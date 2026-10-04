using System;
using System.Collections.Generic;
using System.Linq;
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
        private float _restante, _total;
        private TemporizadorView _reloj;
        private RectTransform _cuerpo;
        private bool _recetaInicialPendiente = true;
        private bool _recetaAbierta;
        protected ResultadoMinijuego Resultado { get; private set; }

        public override bool PuedeVolver { get { return false; } }

        protected float Segundo { get { return Def.Presentacion.SegundosReloj - _restante; } }

        /// <summary>
        /// El modo guiado: Marisol lleva paso a paso por todo el reto (null si se juega libre). Las subclases le
        /// preguntan Permite() antes de cada accion del jugador y le avisan con Hecho() cuando se hizo.
        /// </summary>
        protected GuiaDelMinijuego Guia { get; private set; }

        protected bool Guiado { get { return Guia != null; } }

        /// <summary>El andamiaje del nivel; sin pendiente (banco de pruebas), el de tutorial.</summary>
        protected int Andamiaje { get { return Pendiente != null ? Pendiente.NivelAndamiaje : 3; } }

        /// <summary>"detectar", "ordenar", "repartir": la parte del disparador de la guia que cambia con el verbo.</summary>
        private string VerboCorto {
            get {
                var v = Verbos.Normalizar(Def.Verbo) ?? Verbos.Detectar;
                return v.Substring(v.IndexOf('_') + 1).ToLowerInvariant();
            }
        }

        protected override void Construir() {
            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", Tema.margen, Tema.margen * 0.75f));
            var cabecera = Ui.Fila(marco);
            var titulos = Ui.Columna(cabecera, espacio: 0);
            UiKit.Tamano(titulos, flexAncho: 1);
            var quien = Practica ? "PRÁCTICA EN TU ESCRITORIO · NO CUENTA PARA TU EVALUACIÓN"
                                 : $"TICKET · {(Def.Presentacion.QuienEspera ?? "").ToUpperInvariant()} · {Def.Presentacion.TextoPresion}";
            Ui.Texto(titulos, quien, EstiloTexto.Leyenda, Practica ? Tema.cyan : Tema.warning);
            Ui.Texto(titulos, Def.Presentacion.Titulo ?? Def.Id, EstiloTexto.Titulo);
            // Donde estamos en el proyecto: sin esto, las piezas del reto son nombres sueltos (ronda 4).
            var dondeEstamos = DondeEstamos();
            if (dondeEstamos != null) {
                var linea = Ui.Texto(titulos, dondeEstamos, EstiloTexto.Pequeno, Tema.cyan);
                GuiaView.Registrar("mj.proyecto", linea);
            }
            Ui.Boton(cabecera, "Receta", () => AbrirReceta(false));
            GuiaView.BotonDeAyuda(App, cabecera);
            Ui.Boton(cabecera, "Menú", AbrirPausa, VarianteBoton.Fantasma);

            _restante = Math.Max(10, Def.Presentacion.SegundosReloj);
            _total = _restante;
            // TimerChip: cyan con tiempo, warning al final, danger (con resplandor) cuando agotarlo entrega el reto.
            _reloj = Ui.Temporizador(cabecera, Reloj(_total), Tono.Cyan);
            GuiaView.Registrar("mj.reloj", _reloj);

            _cuerpo = Ui.Fila(marco, "Cuerpo", Tema.espacio * 1.5f, alineacion: TextAnchor.UpperLeft);
            UiKit.Tamano(_cuerpo, flexAncho: 1, flexAlto: 1);
            GuiaView.Registrar("mj.tablero", _cuerpo);

            // El panel de Marisol va entre la cabecera y el tablero: se ve a la vez que la pieza que señala.
            if (Pendiente != null && Pendiente.Guiado) {
                Guia = new GuiaDelMinijuego(App, marco, _cuerpo.GetSiblingIndex(), PiezaGuiada, _ => Repintar());
                _reloj.gameObject.SetActive(false);   // guiado = sin prisa: el reloj no corre
            }

            // Detras de la receta: por si alguien la cierra sin empezar, un boton para verla otra vez.
            var espera = Ui.PanelColumna(_cuerpo, "Espera", Tema.margen * 1.5f, Tema.espacio);
            Ui.Esquinas(espera);
            UiKit.Tamano(espera, flexAncho: 1);
            var hoja = new Hoja(Ui, espera);
            if (!string.IsNullOrEmpty(Def.Presentacion.ComoSeJuega)) hoja.Parrafo(Def.Presentacion.ComoSeJuega);
            var fila = hoja.Fila();
            Ui.Boton(fila, "Ver la receta", () => AbrirReceta(true));
            Ui.Boton(fila, "¡A jugar!", Empezar, VarianteBoton.Primario);
        }

        private static string Reloj(float segundos) {
            var s = Mathf.CeilToInt(segundos);
            return $"{s / 60}:{s % 60:00}";
        }

        /// <summary>El expediente del proyecto del nivel en curso (null en el banco de pruebas o si el nivel no lo tiene).</summary>
        protected Nexus.Core.Proyecto.FichaDelProyecto Proyecto {
            get { return App.Sesion != null && App.Sesion.Perfil != null ? App.Sesion.Perfil.Proyecto : null; }
        }

        /// <summary>«MÓDULO: Reservas · Es el diagrama del módulo…». null si no hay nada que decir.</summary>
        private string DondeEstamos() {
            var modulo = Proyecto == null ? null : Proyecto.Modulo(Def.Modulo);
            if (modulo == null && string.IsNullOrEmpty(Def.EnElProyecto)) return null;
            return (modulo != null ? $"<b>{Proyecto.Nombre.ToUpperInvariant()} · MÓDULO: {modulo.Nombre.ToUpperInvariant()}</b>" : "") +
                   (modulo != null && !string.IsNullOrEmpty(Def.EnElProyecto) ? "  ·  " : "") + (Def.EnElProyecto ?? "");
        }

        private void AbrirReceta(bool inicio) {
            if (_recetaAbierta) return;
            _recetaAbierta = true;
            App.Router.Apilar<PantallaDeReceta>(p => {
                p.Def = Def;
                p.Inicio = inicio && _fase == Fase.Receta;
                p.Ayudas = p.Inicio ? AyudasUtiles() : new List<string>();
                p.AlUsarAyuda = UsarAyuda;
                p.AlCerrar = () => {
                    _recetaAbierta = false;
                    if (p.Inicio) Empezar();
                };
            });
        }

        /// <summary>
        /// La pausa tambien dentro de un reto. El reloj del reto espera. Salir entrega lo que haya en el tablero
        /// (como si se acabara el tiempo): un reto no puede quedarse a medias en el guardado.
        /// </summary>
        private void AbrirPausa() {
            App.Router.Apilar<PantallaDePausa>(p => {
                p.Aviso = _fase == Fase.Cierre ? null : "Si sales ahora, el reto se entrega tal y como esté.";
                p.AntesDeSalir = () => {
                    if (_fase == Fase.Cierre) { AlTerminar?.Invoke(Resultado); return; }
                    _fase = Fase.Cierre;
                    Resultado = Evaluar();
                    AlTerminar?.Invoke(Resultado);
                };
            });
        }

        /// <summary>Las ayudas de compañeros que sirven para ESTE reto y que el jugador todavia tiene.</summary>
        private List<string> AyudasUtiles() {
            var s = App.Sesion;
            if (s == null || Pendiente == null || !s.Fase1Cerrada) return new List<string>();
            return s.AyudasDisponibles().Keys
                    .Where(t => Nexus.Core.Relaciones.TiposDeAyuda.EsDeMinijuego(t) &&
                                Nexus.Core.Relaciones.TiposDeAyuda.SirvePara(t, Def.Verbo) && !Pendiente.TieneAyuda(t))
                    .ToList();
        }

        /// <summary>Gasta una ayuda en este reto. Las que cambian el tablero se aplican al construirlo, al empezar.</summary>
        private bool UsarAyuda(string tipo) {
            if (Pendiente == null || _fase != Fase.Receta || App.Sesion == null || !App.Sesion.UsarAyuda(tipo)) return false;
            Pendiente.Ayudas.Add(tipo);
            if (tipo == Nexus.Core.Relaciones.TiposDeAyuda.MinutosExtra) { _restante += 45; _total += 45; }
            return true;
        }

        private void Empezar() {
            if (_fase != Fase.Receta) return;
            _fase = Fase.Jugando;
            UiKit.Vaciar(_cuerpo);
            try {
                ConstruirJuego(_cuerpo);
                if (Guia != null) {
                    Guia.Empezar(RecorridoGuiado.Para(Def));
                    Repintar();
                }
            } catch (Exception e) {
                // Un tablero a medio construir deja la pantalla colgada y sin clics: mejor cerrarlo con orden.
                Debug.LogException(e);
                AbandonarPorFallo();
                return;
            }
            GuiaView.Avisar(App, "minijuego.jugando." + VerboCorto);
        }

        /// <summary>El reto no se pudo montar: se entrega como omitido (o con lo que haya) y se vuelve a la jornada.</summary>
        private void AbandonarPorFallo() {
            _fase = Fase.Cierre;
            Guia?.Cerrar();
            try { Resultado = Evaluar(); } catch (Exception e) { Debug.LogException(e); Resultado = null; }
            if (Resultado == null)
                Resultado = PuenteDelMotor.Omitido(Def.Id, Pendiente != null ? Pendiente.ObjetivoAprendizaje : null, null);
            UiKit.Vaciar(_cuerpo);
            var panel = Ui.PanelColumna(_cuerpo, "Fallo", Tema.margen * 1.5f, Tema.espacio);
            UiKit.Tamano(panel, flexAncho: 1);
            Ui.Texto(panel, "No se pudo abrir el reto", EstiloTexto.Subtitulo, Tema.danger);
            Ui.Texto(panel, "Algo falló al montar el tablero. El reto se da por omitido y la jornada sigue.", EstiloTexto.Cuerpo);
            Ui.Boton(panel, "Volver a la jornada", () => AlTerminar?.Invoke(Resultado), VarianteBoton.Primario);
        }

        /// <summary>La pieza de la pantalla de la que habla un paso guiado (para señalarla). null = ninguna.</summary>
        protected virtual RectTransform PiezaGuiada(PasoGuiado paso) { return null; }

        private void Update() {
            if (_recetaInicialPendiente) {
                // La receta se apila encima en cuanto la pantalla existe (en Construir el router aun no la tiene).
                _recetaInicialPendiente = false;
                AbrirReceta(true);
                GuiaView.Avisar(App, "minijuego.presentacion");
                return;
            }
            if (_fase != Fase.Jugando || _recetaAbierta || GuiaView.PausaActiva || PantallaDePausa.Abierta || Guia != null) return;
            _restante -= Time.unscaledDeltaTime;
            var f = Mathf.Max(0, _restante) / Math.Max(10f, _total);
            _reloj.Mostrar(Reloj(Mathf.Max(0, _restante)), f < 0.15f ? Tono.Peligro : f < 0.3f ? Tono.Aviso : Tono.Cyan);
            if (_restante <= 0) Entregar();
        }

        /// <summary>Lo llaman las subclases (boton «Entregar») y el reloj al llegar a cero.</summary>
        protected void Entregar() {
            if (_fase != Fase.Jugando) return;
            if (Guia != null && !Guia.Permite(AccionGuiada.Entregar)) { Guia.Rechazar(); return; }
            _fase = Fase.Cierre;
            Resultado = Evaluar();
            Guia?.Cerrar();
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
            hoja.Etiqueta(Guia != null ? "Resultado · hecho con la ayuda de Marisol" : "Resultado");
            hoja.Subtitulo(TituloDelResultado(Resultado.Resultado, Def.Verbo),
                           Resultado.Resultado == ResultadosDeMinijuego.Todos ? Tema.success : Tema.warning);
            // Lo que deja el modo guiado: que mirar para hacerlo solo la proxima vez.
            if (Guia != null) {
                var t = hoja.Tarjeta("La próxima vez lo harás tú: esto es lo que tienes que mirar");
                foreach (var leccion in RecorridoGuiado.Lecciones(Guia.Pasos)) Ui.Texto(t, "· " + leccion, EstiloTexto.Pequeno, Tema.ink);
            }
            // El porque, primero: es lo que se aprende. Antes solo lo veia el docente en el Dashboard.
            if (Resultado.Rubrica != null && !string.IsNullOrEmpty(Resultado.Rubrica.Razon))
                hoja.Parrafo(Resultado.Rubrica.Razon, Tema.ink);
            if (Resultado.Detalle.Count > 0) {
                hoja.Etiqueta("Qué pasó, pieza a pieza");
                foreach (var linea in Resultado.Detalle) hoja.Nota("· " + linea, Tema.ink);
            }
            var solucion = Guia != null ? new List<string>() : SolucionEnTexto().ToList();
            if (solucion.Count > 0 && Resultado.Resultado != ResultadosDeMinijuego.Todos) {
                hoja.Etiqueta("Cómo se podía hacer");
                foreach (var linea in solucion) hoja.Nota("· " + linea, Tema.cyan);
            }
            if (!string.IsNullOrEmpty(Resultado.TextoCierre)) hoja.Parrafo(Resultado.TextoCierre, Tema.cyan);
            // Lo que el resultado significa para el proyecto y su cliente.
            var modulo = Proyecto == null ? null : Proyecto.Modulo(Def.Modulo);
            if (modulo != null) {
                var bien = Resultado.Resultado == ResultadosDeMinijuego.Todos;
                var t = hoja.Tarjeta("Qué significa para " + Proyecto.Cliente.Nombre);
                Ui.Texto(t, bien
                    ? $"«{modulo.Nombre}» sigue adelante sin sorpresas: {Minuscula(modulo.QueHace)}"
                    : $"Lo que se escapó aquí lo acabará notando {Proyecto.Cliente.Nombre} en «{modulo.Nombre}», la parte que {Minuscula(modulo.QueHace)}",
                    EstiloTexto.Pequeno, Tema.ink);
            }
            hoja.Nota(Practica ? "Era práctica: lo que ganes va al proyecto, pero no cuenta para tu evaluación."
                               : "Esto queda en tu evaluación. El resumen de todo, en Lecciones al cerrar el nivel.");
            Ui.Boton(lado, "Volver a la jornada", () => AlTerminar?.Invoke(Resultado), VarianteBoton.Primario);
            GuiaView.Avisar(App, "minijuego.cierre");
        }

        private static string Minuscula(string s) {
            return string.IsNullOrEmpty(s) ? "" : char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        /// <summary>Lineas que explican una solucion buena, para enseñarlas si no se acerto. Cada verbo la suya.</summary>
        protected virtual IEnumerable<string> SolucionEnTexto() { yield break; }

        /// <summary>El titulo del cierre, en el idioma del verbo: en un backlog no se «señala» nada.</summary>
        public static string TituloDelResultado(string resultado, string verbo = null) {
            switch (Verbos.Normalizar(verbo)) {
                case Verbos.Ordenar:
                    switch (resultado) {
                        case ResultadosDeMinijuego.Todos: return "Buen orden";
                        case ResultadosDeMinijuego.Parcial: return "Quedó fuera algo valioso";
                        case ResultadosDeMinijuego.FalsoPositivo: return "Algo va antes de lo que necesita";
                        default: return "No entregaste el orden";
                    }
                case Verbos.Repartir:
                    switch (resultado) {
                        case ResultadosDeMinijuego.Todos: return "Buen reparto";
                        case ResultadosDeMinijuego.Parcial: return "Se escaparon errores";
                        case ResultadosDeMinijuego.FalsoPositivo: return "Horas de más en un tipo, cero en otro";
                        default: return "No repartiste nada";
                    }
                default:
                    switch (resultado) {
                        case ResultadosDeMinijuego.Todos: return "Lo encontraste";
                        case ResultadosDeMinijuego.Parcial: return "Se te escapó algo";
                        case ResultadosDeMinijuego.FalsoPositivo: return "Señalaste lo que no era";
                        default: return "No entregaste nada";
                    }
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
            if (!string.IsNullOrEmpty(rotulo)) Ui.Texto(panel, rotulo.ToUpperInvariant(), EstiloTexto.Pequeno, Tema.cyan);
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
