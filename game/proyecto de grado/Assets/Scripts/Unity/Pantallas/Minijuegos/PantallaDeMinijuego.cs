using System;
using Nexus.Core.Minijuegos;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>
    /// Lo comun a los tres verbos: la presentacion (quien espera y por que hay prisa), el reloj que corre
    /// mientras se juega y, al acabar, el cierre con lo que se encontro y lo que no. La escena no aplica nada
    /// al mundo: devuelve el ResultadoMinijuego y el motor lo aplica (AlTerminar → ResolverMinijuego).
    ///
    /// Si el reloj llega a cero, se entrega lo que haya: quien espera no espera mas.
    /// </summary>
    public abstract class PantallaDeMinijuego : Pantalla {
        public MinijuegoDef Def;
        public PendingMinigame Pendiente;
        public Action<ResultadoMinijuego> AlTerminar;

        private enum Fase { Presentacion, Jugando, Cierre }

        private Fase _fase = Fase.Presentacion;
        private float _restante;
        private TMP_Text _reloj;
        private RectTransform _cuerpo;
        private ResultadoMinijuego _resultado;

        public override bool PuedeVolver { get { return false; } }

        protected float Segundo { get { return Def.Presentacion.SegundosReloj - _restante; } }

        protected override void Construir() {
            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", Tema.margen, Tema.margen * 0.75f));
            var cabecera = Ui.Fila(marco);
            var titulos = Ui.Columna(cabecera, espacio: 0);
            UiKit.Tamano(titulos, flexAncho: 1);
            Ui.Texto(titulos, "TICKET · " + (Def.Presentacion.QuienEspera ?? "").ToUpperInvariant(), EstiloTexto.Pequeno, Tema.mostaza);
            Ui.Texto(titulos, Def.Presentacion.Titulo ?? Def.Id, EstiloTexto.Titulo);
            _reloj = Ui.Texto(cabecera, "", EstiloTexto.Titulo, Tema.cian, TextAlignmentOptions.Right);
            UiKit.Tamano(_reloj, ancho: 200);

            _cuerpo = Ui.Columna(marco, "Cuerpo", Tema.espacio);
            UiKit.Tamano(_cuerpo, flexAncho: 1, flexAlto: 1);
            _restante = Math.Max(10, Def.Presentacion.SegundosReloj);
            PintarPresentacion();
        }

        private void PintarPresentacion() {
            UiKit.Vaciar(_cuerpo);
            var panel = Ui.PanelColumna(_cuerpo, "Presentacion", Tema.margen * 1.5f, Tema.espacio);
            var hoja = new Hoja(Ui, panel);
            if (!string.IsNullOrEmpty(Def.Presentacion.TextoPresion)) hoja.Subtitulo(Def.Presentacion.TextoPresion, Tema.amarillo);
            if (!string.IsNullOrEmpty(Def.Presentacion.ComoSeJuega)) hoja.Parrafo(Def.Presentacion.ComoSeJuega);
            hoja.Nota($"Tienes {Def.Presentacion.SegundosReloj} segundos. El reloj empieza cuando pulses «Empezar»; si llega a cero, se entrega lo que tengas.");
            hoja.Accion("Empezar", () => {
                _fase = Fase.Jugando;
                UiKit.Vaciar(_cuerpo);
                ConstruirJuego(_cuerpo);
            });
        }

        private void Update() {
            if (_fase == Fase.Presentacion) { _reloj.text = FormatoReloj(_restante); return; }
            if (_fase != Fase.Jugando) return;
            _restante -= Time.unscaledDeltaTime;
            _reloj.text = FormatoReloj(Mathf.Max(0, _restante));
            _reloj.color = _restante < 15 ? Tema.rojo : _restante < 30 ? Tema.amarillo : Tema.cian;
            if (_restante <= 0) Entregar();
        }

        private static string FormatoReloj(float s) {
            var t = Mathf.CeilToInt(s);
            return $"{t / 60}:{t % 60:00}";
        }

        /// <summary>Lo llaman las subclases (boton «Entregar») y el reloj al llegar a cero.</summary>
        protected void Entregar() {
            if (_fase != Fase.Jugando) return;
            _fase = Fase.Cierre;
            _resultado = Evaluar();
            PintarCierre();
        }

        private void PintarCierre() {
            UiKit.Vaciar(_cuerpo);
            _reloj.text = "";
            var panel = Ui.PanelColumna(_cuerpo, "Cierre", Tema.margen * 1.25f, Tema.espacio);
            UiKit.Tamano(panel, flexAlto: 1);
            RectTransform contenido;
            var scroll = Ui.Desplazable(panel, out contenido);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            var hoja = new Hoja(Ui, contenido);

            hoja.Etiqueta("Resultado");
            hoja.Titulo(TituloDelResultado(_resultado.Resultado));
            foreach (var linea in _resultado.Detalle) hoja.Parrafo("· " + linea);
            if (!string.IsNullOrEmpty(_resultado.TextoCierre)) {
                hoja.Espacio();
                hoja.Parrafo(_resultado.TextoCierre, Tema.cianClaro);
            }
            hoja.Nota("Cómo se valora lo que hiciste, lo verás en el Dashboard de Lecciones al cerrar el nivel.");
            hoja.Accion("Volver a la jornada", () => AlTerminar?.Invoke(_resultado));
        }

        public static string TituloDelResultado(string resultado) {
            switch (resultado) {
                case ResultadosDeMinijuego.Todos: return "Lo encontraste";
                case ResultadosDeMinijuego.Parcial: return "Se te escapó algo";
                case ResultadosDeMinijuego.FalsoPositivo: return "Señalaste lo que no era";
                default: return "No entregaste nada";
            }
        }

        /// <summary>Construye el tablero del verbo en 'cuerpo'. Se llama al pulsar «Empezar».</summary>
        protected abstract void ConstruirJuego(RectTransform cuerpo);

        /// <summary>Evalua lo que hay en el tablero ahora mismo, con el evaluador puro del verbo.</summary>
        protected abstract ResultadoMinijuego Evaluar();
    }
}
