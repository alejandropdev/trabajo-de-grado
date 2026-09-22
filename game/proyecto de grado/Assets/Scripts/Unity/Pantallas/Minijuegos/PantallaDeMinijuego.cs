using System;
using Nexus.Core.Minijuegos;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>
    /// Lo comun a los tres verbos:
    ///   · la RECETA (una pizarra con dibujos, al estilo de Overcooked) sale sola al empezar, con quien espera y por
    ///     que hay prisa, y se puede volver a abrir en cualquier momento con «Receta». Mientras esta abierta, el
    ///     reloj del reto espera;
    ///   · el reloj circular, que corre mientras se juega. Si llega a cero, se entrega lo que haya;
    ///   · el cierre: el lienzo se vuelve a pintar ENSEÑANDO la solucion (lo que acertaste, lo que se te escapo),
    ///     con la explicacion al lado.
    ///
    /// La escena no aplica nada al mundo: devuelve el ResultadoMinijuego y el motor lo aplica.
    /// </summary>
    public abstract class PantallaDeMinijuego : Pantalla {
        public MinijuegoDef Def;
        public PendingMinigame Pendiente;
        public Action<ResultadoMinijuego> AlTerminar;
        /// <summary>Trabajo de oficina: se juega igual, pero no cuenta para la evaluacion, y la pantalla lo dice.</summary>
        public bool Practica;

        private enum Fase { Receta, Jugando, Cierre }

        private Fase _fase = Fase.Receta;
        private float _restante;
        private DialView _dial;
        private RectTransform _cuerpo;
        private bool _recetaInicialPendiente = true;
        private bool _recetaAbierta;
        protected ResultadoMinijuego Resultado { get; private set; }

        public override bool PuedeVolver { get { return false; } }

        protected float Segundo { get { return Def.Presentacion.SegundosReloj - _restante; } }

        protected override void Construir() {
            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", Tema.margen, Tema.margen * 0.75f));
            var cabecera = Ui.Fila(marco);
            var titulos = Ui.Columna(cabecera, espacio: 0);
            UiKit.Tamano(titulos, flexAncho: 1);
            var quien = Practica ? "PRÁCTICA EN TU ESCRITORIO · NO CUENTA PARA TU EVALUACIÓN"
                                 : $"TICKET · {(Def.Presentacion.QuienEspera ?? "").ToUpperInvariant()} · {Def.Presentacion.TextoPresion}";
            Ui.Texto(titulos, quien, EstiloTexto.Pequeno, Practica ? Tema.cianClaro : Tema.mostaza);
            Ui.Texto(titulos, Def.Presentacion.Titulo ?? Def.Id, EstiloTexto.Titulo);
            Ui.Boton(cabecera, "Receta", () => AbrirReceta(false));
            GuiaView.BotonDeAyuda(App, cabecera);

            _restante = Math.Max(10, Def.Presentacion.SegundosReloj);
            _dial = Ui.Dial(cabecera, 1, "", Tema.cian, 112);
            var total = _restante;
            _dial.Formato = v => { var s = Mathf.CeilToInt(v * total); return $"{s / 60}:{s % 60:00}"; };
            _dial.Valor = 1;
            GuiaView.Registrar("mj.reloj", _dial);

            _cuerpo = Ui.Fila(marco, "Cuerpo", Tema.espacio * 1.5f, alineacion: TextAnchor.UpperLeft);
            UiKit.Tamano(_cuerpo, flexAncho: 1, flexAlto: 1);
            GuiaView.Registrar("mj.tablero", _cuerpo);

            // Detras de la receta: por si alguien la cierra sin empezar, un boton para verla otra vez.
            var espera = Ui.PanelColumna(_cuerpo, "Espera", Tema.margen * 1.5f, Tema.espacio);
            UiKit.Tamano(espera, flexAncho: 1);
            var hoja = new Hoja(Ui, espera);
            if (!string.IsNullOrEmpty(Def.Presentacion.ComoSeJuega)) hoja.Parrafo(Def.Presentacion.ComoSeJuega);
            var fila = hoja.Fila();
            Ui.Boton(fila, "Ver la receta", () => AbrirReceta(true));
            Ui.Boton(fila, "Empezar", Empezar, VarianteBoton.Primario);
        }

        private void AbrirReceta(bool inicio) {
            if (_recetaAbierta) return;
            _recetaAbierta = true;
            App.Router.Apilar<PantallaDeReceta>(p => {
                p.Def = Def;
                p.Inicio = inicio && _fase == Fase.Receta;
                p.AlCerrar = () => {
                    _recetaAbierta = false;
                    if (p.Inicio) Empezar();
                };
            });
        }

        private void Empezar() {
            if (_fase != Fase.Receta) return;
            _fase = Fase.Jugando;
            UiKit.Vaciar(_cuerpo);
            ConstruirJuego(_cuerpo);
            GuiaView.Avisar(App, "minijuego.jugando");
        }

        private void Update() {
            if (_recetaInicialPendiente) {
                // La receta se apila encima en cuanto la pantalla existe (en Construir el router aun no la tiene).
                _recetaInicialPendiente = false;
                AbrirReceta(true);
                GuiaView.Avisar(App, "minijuego.presentacion");
                return;
            }
            if (_fase != Fase.Jugando || _recetaAbierta || GuiaView.PausaActiva) return;
            _restante -= Time.unscaledDeltaTime;
            var f = Mathf.Max(0, _restante) / Math.Max(10, Def.Presentacion.SegundosReloj);
            _dial.Valor = f;
            _dial.Color = f < 0.15f ? Tema.rojo : f < 0.3f ? Tema.amarillo : Tema.cian;
            if (_restante <= 0) Entregar();
        }

        /// <summary>Lo llaman las subclases (boton «Entregar») y el reloj al llegar a cero.</summary>
        protected void Entregar() {
            if (_fase != Fase.Jugando) return;
            _fase = Fase.Cierre;
            Resultado = Evaluar();
            PintarCierre();
        }

        private void PintarCierre() {
            UiKit.Vaciar(_cuerpo);

            var visual = Ui.Columna(_cuerpo, "Solucion", Tema.espacio);
            UiKit.Tamano(visual, flexAncho: 1, flexAlto: 1);
            PintarResultado(visual);

            var lado = Ui.Columna(_cuerpo, "Explicacion", Tema.espacio);
            UiKit.Tamano(lado, ancho: 480, flexAlto: 1);
            var panel = Ui.PanelColumna(lado, "Resultado", Tema.margen, Tema.espacio,
                                        null);
            UiKit.Tamano(panel, flexAlto: 1);
            RectTransform contenido;
            var scroll = Ui.Desplazable(panel, out contenido);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            var hoja = new Hoja(Ui, contenido);
            hoja.Etiqueta("Resultado");
            hoja.Subtitulo(TituloDelResultado(Resultado.Resultado),
                           Resultado.Resultado == ResultadosDeMinijuego.Todos ? Tema.cian : Tema.amarillo);
            foreach (var linea in Resultado.Detalle) hoja.Nota("· " + linea, Tema.texto);
            if (!string.IsNullOrEmpty(Resultado.TextoCierre)) hoja.Parrafo(Resultado.TextoCierre, Tema.cianClaro);
            hoja.Nota(Practica ? "Era práctica: lo que ganes va al proyecto, pero no cuenta para tu evaluación."
                               : "Cómo se valora lo que hiciste, lo verás en el Dashboard de Lecciones al cerrar el nivel.");
            Ui.Boton(lado, "Volver a la jornada", () => AlTerminar?.Invoke(Resultado), VarianteBoton.Primario);
            GuiaView.Avisar(App, "minijuego.cierre");
        }

        public static string TituloDelResultado(string resultado) {
            switch (resultado) {
                case ResultadosDeMinijuego.Todos: return "Lo encontraste";
                case ResultadosDeMinijuego.Parcial: return "Se te escapó algo";
                case ResultadosDeMinijuego.FalsoPositivo: return "Señalaste lo que no era";
                default: return "No entregaste nada";
            }
        }

        /// <summary>Construye el tablero del verbo en 'cuerpo' (una fila). Se llama al empezar.</summary>
        protected abstract void ConstruirJuego(RectTransform cuerpo);

        /// <summary>Evalua lo que hay en el tablero ahora mismo, con el evaluador puro del verbo.</summary>
        protected abstract ResultadoMinijuego Evaluar();

        /// <summary>Vuelve a pintar el tablero enseñando la solucion. La explicacion en texto la pone la base.</summary>
        protected abstract void PintarResultado(RectTransform zona);

        /// <summary>Un panel de lienzo que ocupa lo que le den, con una lamina de tamaño fijo dentro (con scroll si no cabe).</summary>
        protected Lamina NuevoLienzo(Transform padre, string rotulo, float ancho, float alto, float? anchoFijo = null) {
            var panel = Ui.PanelColumna(padre, "Lienzo", 18, 8);
            if (anchoFijo.HasValue) UiKit.Tamano(panel, ancho: anchoFijo.Value, flexAlto: 1);
            else UiKit.Tamano(panel, flexAncho: 1, flexAlto: 1);
            if (!string.IsNullOrEmpty(rotulo)) Ui.Texto(panel, rotulo.ToUpperInvariant(), EstiloTexto.Pequeno, Tema.cian);
            RectTransform contenido;
            var scroll = Ui.Desplazable(panel, out contenido);
            scroll.horizontal = true;
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            contenido.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().childForceExpandWidth = false;
            contenido.anchorMax = new Vector2(0, 1);   // el ancho lo decide la lamina, no el visor
            contenido.pivot = new Vector2(0, 1);       // y empieza a la izquierda, no centrada
            var ajuste = contenido.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            ajuste.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            return new Lamina(Ui, contenido, ancho, alto);
        }
    }
}
