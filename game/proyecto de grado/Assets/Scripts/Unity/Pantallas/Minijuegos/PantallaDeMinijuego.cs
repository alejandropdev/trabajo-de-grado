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
        private bool _reintentado;

        private const float AnchoDelBriefing = 380, AnchoDelBriefingPlegado = 96;
        private UnityEngine.UI.LayoutElement _anchoDelBriefing;
        private GameObject _contenidoDelBriefing;
        private UnityEngine.UI.Button _plegar;
        private bool _briefingPlegado;
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
            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", Tema.espacio, Tema.margen * 0.75f));

            // Cabecera fina: de quien es el ticket, las herramientas y el reloj (LCD). El titulo va en el briefing.
            var cabecera = Ui.Fila(marco);
            var quien = Practica ? "PRÁCTICA EN TU ESCRITORIO · NO CUENTA PARA TU EVALUACIÓN"
                                 : $"TICKET · {(Def.Presentacion.QuienEspera ?? "").ToUpperInvariant()} · {Def.Presentacion.TextoPresion}";
            UiKit.Tamano(Ui.Texto(cabecera, quien, EstiloTexto.Leyenda, Practica ? Tema.cyan : Tema.warning), flexAncho: 1);
            Ui.Boton(cabecera, "Receta", () => AbrirReceta(false));
            GuiaView.BotonDeAyuda(App, cabecera);
            Ui.Boton(cabecera, "Menú", AbrirPausa, VarianteBoton.Fantasma);

            _restante = Math.Max(10, Def.Presentacion.SegundosReloj);
            _total = _restante;
            // El reloj del reto: cyan con tiempo, warning al final, danger cuando agotarlo entrega el reto.
            _reloj = Ui.Temporizador(cabecera, Reloj(_total), Tono.Cyan);
            GuiaView.Registrar("mj.reloj", _reloj);

            var area = Ui.Fila(marco, "Area", Tema.espacio * 1.5f, alineacion: TextAnchor.UpperLeft);
            UiKit.Tamano(area, flexAncho: 1, flexAlto: 1);
            ConstruirBriefing(area);
            _cuerpo = Ui.Fila(area, "Cuerpo", Tema.espacio * 1.5f, alineacion: TextAnchor.UpperLeft);
            UiKit.Tamano(_cuerpo, flexAncho: 1, flexAlto: 1);
            GuiaView.Registrar("mj.tablero", _cuerpo);

            // El panel de Marisol va entre la cabecera y el tablero: se ve a la vez que la pieza que señala.
            if (Pendiente != null && Pendiente.Guiado) {
                Guia = new GuiaDelMinijuego(App, marco, area.GetSiblingIndex(), PiezaGuiada, _ => Repintar());
                _reloj.gameObject.SetActive(false);   // guiado = sin prisa: el reloj no corre
            }

            // Detras de la receta: quien espera el trabajo y, por si alguien la cierra sin empezar, volver a verla.
            var espera = Ui.Ventana(_cuerpo, "Espera");
            UiKit.Tamano(espera, flexAncho: 1, flexAlto: 1);
            var personaje = Def.Presentacion.QuienEspera;
            if (!string.IsNullOrEmpty(personaje) && personaje != "Tú mismo")
                Ui.Ilustracion(espera, Nexus.Unity.Tema.MaterialesNexus.IdDePersonaje(personaje), personaje, alto: 420);
            var fila = Ui.Fila(espera);
            Ui.Boton(fila, "Ver la receta", () => AbrirReceta(true));
            Ui.Boton(fila, "¡A jugar!", Empezar, VarianteBoton.Primario);
        }

        /// <summary>
        /// La columna de papel de la izquierda (las referencias de los minijuegos): el titulo del reto, donde estamos en
        /// el proyecto, que hay que hacer y quien lo espera. Todo sale del JSON del reto; no se inventa nada.
        /// </summary>
        private void ConstruirBriefing(Transform padre) {
            var hoja = Ui.Hoja(padre, "Briefing", Tema.Espacio(5));
            _anchoDelBriefing = UiKit.Tamano(hoja, ancho: AnchoDelBriefing, flexAlto: 1);   // +25 %: el briefing se lee de corrido, no a pedazos
            // Mientras se juega estorba mas de lo que ayuda (ya se leyo, y la receta sigue a un boton): se pliega a
            // un riel al empezar, y se despliega cuando se quiera releer.
            _plegar = Ui.Boton(hoja, "◄", () => PlegarBriefing(!_briefingPlegado), VarianteBoton.Fantasma);
            RectTransform contenido;
            var scroll = Ui.Desplazable(hoja, out contenido, "Briefing (scroll)");
            _contenidoDelBriefing = scroll.gameObject;
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            Ui.Texto(contenido, Def.Presentacion.Titulo ?? Def.Id, EstiloTexto.Titulo, Tema.paperInk);
            var renglones = new List<RenglonDeBriefing>();
            var dondeEstamos = DondeEstamos();
            if (dondeEstamos != null) renglones.Add(new RenglonDeBriefing("•", "Dónde estamos", dondeEstamos));
            if (!string.IsNullOrEmpty(Def.Presentacion.ComoSeJuega))
                renglones.Add(new RenglonDeBriefing("→", "Lo que haces", Def.Presentacion.ComoSeJuega));
            if (!Practica && !string.IsNullOrEmpty(Def.Presentacion.TextoPresion))
                renglones.Add(new RenglonDeBriefing("!", "Quién lo espera", Def.Presentacion.TextoPresion));
            var briefing = Ui.Briefing(contenido, renglones, true);
            GuiaView.Registrar("mj.proyecto", briefing);
        }

        private void PlegarBriefing(bool plegado) {
            _briefingPlegado = plegado;
            _contenidoDelBriefing.SetActive(!plegado);
            _anchoDelBriefing.minWidth = _anchoDelBriefing.preferredWidth = plegado ? AnchoDelBriefingPlegado : AnchoDelBriefing;
            _plegar.GetComponentInChildren<TMP_Text>().text = plegado ? "►" : "◄";
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
            PlegarBriefing(true);
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
            // Guiado, solo se llega aqui con el recorrido completo: esta mecanica ya no se vuelve a guiar.
            if (Guia != null) App.AnotarMecanicaGuiada(Def.Verbo);
            Guia?.Cerrar();
            PintarCierre();
        }

        /// <summary>
        /// Una practica que no salio se puede repetir una vez, con el tablero de nuevo en blanco y ya sabiendo lo
        /// que explico el cierre: es practica, y fallar sin poder corregir no enseña nada. El tiempo ya se pago.
        /// </summary>
        private void Reintentar() {
            if (_fase != Fase.Cierre || _reintentado) return;
            _reintentado = true;
            Resultado = null;
            _restante = _total;
            _fase = Fase.Receta;
            Empezar();
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
            if (Practica && Guia == null && !_reintentado && Resultado.Resultado != ResultadosDeMinijuego.Todos)
                Ui.Boton(lado, "Intentarlo otra vez", Reintentar);
            Ui.Boton(lado, "Volver a la jornada", () => AlTerminar?.Invoke(Resultado), VarianteBoton.Primario);
            GuiaView.Avisar(App, "minijuego.cierre");
        }

        private static string Minuscula(string s) {
            return string.IsNullOrEmpty(s) ? "" : char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        /// <summary>
        /// La columna de la derecha de un tablero: su contenido va en un scroll (con la letra del design system no
        /// siempre cabe en 1080 de alto, y una columna sin sitio montaba un texto sobre otro) y debajo, fijo, el pie
        /// para el boton de entregar, que asi nunca queda fuera de la pantalla.
        /// </summary>
        protected RectTransform ColumnaLateral(RectTransform cuerpo, string nombre, float ancho, out RectTransform pie) {
            var columna = Ui.Columna(cuerpo, nombre, Tema.espacio);
            UiKit.Tamano(columna, ancho: ancho, flexAlto: 1);
            TarjetaDeQuienEspera(columna);
            RectTransform contenido;
            var scroll = Ui.Desplazable(columna, out contenido, nombre + " (scroll)");
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            contenido.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().padding = new RectOffset(4, 18, 4, 4);   // aire para la barra y las esquinas
            pie = Ui.Columna(columna, "Pie", Tema.espacio);
            return contenido;
        }

        /// <summary>La ficha de quien espera el trabajo (Javier, la ministra…): su retrato y lo que te ha dicho.</summary>
        protected void TarjetaDeQuienEspera(Transform padre) {
            var personaje = Def.Presentacion.QuienEspera;
            if (string.IsNullOrEmpty(personaje) || personaje == "Tú mismo") return;
            var ficha = Ui.PanelColumna(padre, "Quien espera", Tema.espacio, Tema.espacio);
            var fila = Ui.Fila(ficha, "Retrato", Tema.espacio, alineacion: TextAnchor.UpperLeft);
            Ui.Ilustracion(fila, Nexus.Unity.Tema.MaterialesNexus.IdDePersonaje(personaje), null, 110, 130);
            var textos = Ui.Columna(fila, "Texto", 4);
            UiKit.Tamano(textos, flexAncho: 1);
            Ui.Texto(textos, personaje, EstiloTexto.Subtitulo);
            if (!string.IsNullOrEmpty(Def.Presentacion.TextoPresion))
                Ui.Texto(textos, "«" + Def.Presentacion.TextoPresion + "»", EstiloTexto.Dialogo).fontStyle = TMPro.FontStyles.Italic;
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
            // El tablero es un documento: una hoja de papel dentro de una ventana de metal, con su titulo en tinta.
            var panel = Ui.Ventana(padre, "Lienzo", true);
            if (anchoFijo.HasValue) UiKit.Tamano(panel, ancho: anchoFijo.Value, flexAlto: 1);
            else UiKit.Tamano(panel, flexAncho: 1, flexAlto: 1);
            if (!string.IsNullOrEmpty(rotulo)) Ui.Texto(panel, rotulo, EstiloTexto.Subtitulo, Tema.paperInk);
            RectTransform contenido;
            var scroll = Ui.Desplazable(panel, out contenido);
            scroll.horizontal = true;
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            contenido.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().childForceExpandWidth = false;
            contenido.anchorMax = new Vector2(0, 1);   // el ancho lo decide la lamina, no el visor
            contenido.pivot = new Vector2(0, 1);       // y empieza a la izquierda, no centrada
            var ajuste = contenido.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            ajuste.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            // La lamina mide lo que su dibujo (1330 px en un diagrama); la ventana, lo que deje la pantalla. Se escala
            // para caber de ancho, sin bajar del 55 % (por debajo ya no se lee: entonces, scroll horizontal).
            var caja = Ui.Nodo(contenido, "Caja de la lamina");
            var lamina = new Lamina(Ui, caja, ancho, alto);
            var rt = lamina.Raiz;
            rt.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(ancho, alto);
            var ajustar = caja.gameObject.AddComponent<AjustarLaminaAlVisor>();
            ajustar.Visor = scroll.viewport;
            ajustar.Lamina = rt;
            ajustar.Ancho = ancho;
            ajustar.Alto = alto;
            return lamina;
        }
    }

    /// <summary>Escala una lamina para que quepa de ancho en su visor (entre el 55 % y el 100 %) y reserva su sitio.</summary>
    public sealed class AjustarLaminaAlVisor : MonoBehaviour {
        public RectTransform Visor, Lamina;
        public float Ancho, Alto;
        private UnityEngine.UI.LayoutElement _le;
        private float _ultimo = -1;

        private void LateUpdate() {
            if (Visor == null || Lamina == null) return;
            var disponible = Visor.rect.width - 24;   // lo que ocupa la barra de scroll vertical
            if (disponible <= 1 || Mathf.Abs(disponible - _ultimo) < 0.5f) return;
            _ultimo = disponible;
            var k = Mathf.Clamp(disponible / Ancho, 0.55f, 1f);
            Lamina.localScale = new Vector3(k, k, 1);
            if (_le == null) _le = UiKit.Tamano(transform);
            _le.minWidth = _le.preferredWidth = Ancho * k;
            _le.minHeight = _le.preferredHeight = Alto * k;
        }
    }
}
