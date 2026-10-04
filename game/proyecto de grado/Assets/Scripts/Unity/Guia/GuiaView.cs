using System;
using System.Collections.Generic;
using Nexus.Core.Narrativa;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Pantallas;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Guia {
    /// <summary>
    /// La guia del tutorial: una burbuja de Marisol, siempre encima de todo, que dice QUE hacer y POR QUE, y
    /// señala con un marco la pieza de la pantalla de la que habla.
    ///
    /// Las pantallas no saben que pasos hay: solo avisan de lo que pasa (Avisar("alerta.suena")) y de lo que
    /// el jugador hizo (Hecho("ir-a-zona:pasillo")), y registran las piezas señalables (Registrar("dia.mapa",
    /// rt)). Que decir y cuando lo decide el contenido (narrativa/guia-tutorial.json), con GuiaDelTutorial.
    ///
    /// Las reglas que la mantienen en su sitio (antes se quedaba colgada o llegaba tarde):
    ///   · Un paso sin accion es MODAL: un velo tapa la pantalla hasta «¡Entendido!». Asi el jugador no puede
    ///     adelantarse por debajo y dejar a la guia explicando una pantalla que ya no esta.
    ///   · Un paso con accion («Pulse el pasillo») no lleva velo ni para el reloj, y se cierra cuando se hace.
    ///     Si la accion YA estaba hecha (ya estas en el pasillo), el paso no sale.
    ///   · Un disparador CONTEXTUAL nuevo (otra pantalla, otro paso de la Fase 1, otro dia) descarta lo que
    ///     quedaba pendiente del contexto anterior: la guia sigue al jugador, no al reves.
    ///   · Si algo hace imposible la accion (el aviso caduco, elegiste quedarte), la pantalla llama a Cancelar.
    ///   · Un paso se marca visto al CERRARLO. «¿Qué hago ahora?» relee lo de la pantalla actual, visto o no, y
    ///     si no hay nada propio, la ayuda generica de esa pantalla. «Apuntes del proyecto» resume todo el nivel.
    /// </summary>
    public sealed class GuiaView : MonoBehaviour {
        private static GuiaView _instancia;
        private static readonly Dictionary<string, RectTransform> _resaltables = new Dictionary<string, RectTransform>();
        private static Func<string, bool> _yaCumplida;

        private AppRoot _app;
        private GuiaDelTutorial _guia;
        private string _nivelDeLaGuia;
        private PasoDeGuia _actual;
        private string _disparadorActual, _contexto;
        private bool _releyendo, _colocarPendiente;
        private readonly List<string> _enEspera = new List<string>();
        private readonly List<PasoDeGuia> _repaso = new List<PasoDeGuia>();
        private readonly HashSet<string> _hechas = new HashSet<string>();

        private RectTransform _burbuja, _marco, _textoMas, _velo, _retrato;
        private TMP_Text _quien, _titulo, _texto, _mas;
        private Button _entendido, _botonMas;
        private GameObject _esperando;
        private readonly List<Image> _bordes = new List<Image>();

        /// <summary>Hay un paso abierto que para el reloj (el del dia y el de los minijuegos).</summary>
        public static bool PausaActiva {
            get {
                return _instancia != null && _instancia._actual != null && _instancia._actual.PausaElReloj;
            }
        }

        // ==================================================================== API de las pantallas

        public static void Registrar(string clave, Component pieza) {
            if (string.IsNullOrEmpty(clave) || pieza == null) return;
            _resaltables[clave] = pieza.transform as RectTransform;
        }

        /// <summary>
        /// La pantalla del dia dice como saber si una accion ya esta hecha («ir-a-zona:pasillo» si ya estas en el
        /// pasillo). Un paso que pide algo ya hecho no sale: si saliera, se quedaria esperando para siempre.
        /// </summary>
        public static void RegistrarComprobador(Func<string, bool> yaCumplida) { _yaCumplida = yaCumplida; }

        public static void Avisar(AppRoot app, string disparador) {
            var g = Obtener(app);
            if (g == null) return;
            g.AlAvisar(disparador);
        }

        public static void Hecho(AppRoot app, string accion) {
            if (_instancia == null || string.IsNullOrEmpty(accion)) return;
            _instancia._hechas.Add(accion);
            if (_instancia._actual != null && AccionesDeGuia.Cumple(_instancia._actual.EsperaAccion, accion)) _instancia.Cerrar(true);
        }

        /// <summary>
        /// La accion que un paso esperaba ya no se puede hacer (el aviso caduco, el jugador eligio quedarse). El
        /// paso se cierra como visto: el momento paso, y volver a pedirlo despues no tendria sentido.
        /// </summary>
        public static void Cancelar(string accion) {
            if (_instancia == null || _instancia._actual == null || string.IsNullOrEmpty(accion)) return;
            if (AccionesDeGuia.Cumple(_instancia._actual.EsperaAccion, accion) || _instancia._actual.EsperaAccion == accion)
                _instancia.Cerrar(true);
        }

        /// <summary>Los botones «¿Qué hago ahora?» y «Apuntes del proyecto». Solo en niveles que tienen guia.</summary>
        public static void BotonDeAyuda(AppRoot app, Transform padre) {
            var g = Obtener(app);
            if (g == null || g._guia == null || !g._guia.Activa) return;
            app.Ui.Boton(padre, "¿Qué hago ahora?", () => g.Repetir(), VarianteBoton.Fantasma);
            // Rotulo corto: las cabeceras ya llevan varios botones y uno largo empujaba el resto fuera de la pantalla.
            app.Ui.Boton(padre, "Apuntes", () => ApuntesDelProyecto.Abrir(app), VarianteBoton.Fantasma);
        }

        /// <summary>Al salir de la partida: la burbuja no se queda flotando sobre el menu.</summary>
        public static void Reiniciar() {
            _yaCumplida = null;
            _señalado = null;
            if (_instancia == null) return;
            _instancia._guia = null;
            _instancia._nivelDeLaGuia = null;
            _instancia._actual = null;
            _instancia._contexto = null;
            _instancia._enEspera.Clear();
            _instancia._repaso.Clear();
            _instancia._hechas.Clear();
            _instancia.Ocultar();
        }

        // ==================================================================== ciclo

        private static GuiaView Obtener(AppRoot app) {
            if (app == null || app.CapaGuia == null) return null;
            if (_instancia == null) {
                _instancia = app.CapaGuia.gameObject.AddComponent<GuiaView>();
                _instancia._app = app;
                _instancia.Construir();
            }
            _instancia.PrepararNivel();
            return _instancia;
        }

        /// <summary>La guia es de un nivel: al cambiar de nivel (o de partida) se rehace con los pasos del nuevo.</summary>
        private void PrepararNivel() {
            var nivel = _app.Sesion != null ? _app.Sesion.NivelId : null;
            if (nivel == _nivelDeLaGuia && _guia != null) return;
            _nivelDeLaGuia = nivel;
            var vistos = _app.PerfilActivo != null
                ? (_app.PerfilActivo.guiaVista ?? (_app.PerfilActivo.guiaVista = new List<string>()))
                : new List<string>();
            _guia = new GuiaDelTutorial(_app.Catalogo.Guia, nivel, vistos);
            _actual = null;
            _contexto = null;
            _enEspera.Clear();
            _repaso.Clear();
            _hechas.Clear();
            Ocultar();
        }

        private void AlAvisar(string disparador) {
            if (_guia == null || !_guia.Activa || string.IsNullOrEmpty(disparador)) return;

            if (DisparadoresDeGuia.EsContextual(disparador) && disparador != _contexto) {
                // Otra pantalla, otro momento: lo pendiente del anterior ya no sirve.
                _contexto = disparador;
                _enEspera.RemoveAll(DisparadoresDeGuia.EsContextual);
                if (DisparadoresDeGuia.EsDia(disparador) || disparador == "dia.antes") _hechas.Clear();
                if (_releyendo) {
                    _repaso.Clear();
                    if (_actual != null) Cerrar(false, false);
                    _releyendo = false;
                }
                if (_actual != null && _disparadorActual != disparador && !EsDeEvento(_actual)) Cerrar(false, false);
            }

            if (_actual != null) {
                if (!_enEspera.Contains(disparador)) _enEspera.Add(disparador);
                return;
            }
            MostrarSiguiente(disparador);
        }

        /// <summary>
        /// Un paso de evento (un aviso, un hallazgo) sobrevive al cambio de contexto: pasa encima de cualquier
        /// pantalla. Si esperaba algo que deja de ser posible, la pantalla lo cancela (Cancelar).
        /// </summary>
        private static bool EsDeEvento(PasoDeGuia p) {
            return p != null && !DisparadoresDeGuia.EsContextual(p.Disparador);
        }

        /// <summary>El siguiente paso sin ver de ese disparador, saltando los que piden algo que ya esta hecho.</summary>
        private void MostrarSiguiente(string disparador) {
            PasoDeGuia paso;
            while ((paso = _guia.Siguiente(disparador)) != null) {
                if (!YaHecho(paso.EsperaAccion)) break;
                _guia.MarcarVisto(paso);   // pedia algo que el jugador ya hizo: no hace falta decirlo
            }
            if (paso == null) return;
            _disparadorActual = disparador;
            _releyendo = false;
            Mostrar(paso);
        }

        private bool YaHecho(string accion) {
            if (string.IsNullOrEmpty(accion)) return false;
            foreach (var h in _hechas) if (AccionesDeGuia.Cumple(accion, h)) return true;
            return _yaCumplida != null && _yaCumplida(accion);
        }

        /// <param name="visto">Se marca como visto (lo leyo o lo hizo). Al cambiar de pantalla a la fuerza, no: volvera a salir.</param>
        /// <param name="seguir">Sale lo que venia detras. Al cerrarlo por un cambio de contexto, no: ya lo decide el nuevo.</param>
        private void Cerrar(bool visto, bool seguir = true) {
            var cerrado = _actual;
            _actual = null;
            Ocultar();
            if (cerrado != null && visto && !_releyendo) {
                _guia.MarcarVisto(cerrado);
                _app.GuardarPerfil();
            }
            if (!seguir) { _disparadorActual = null; return; }

            if (_releyendo) {
                if (_repaso.Count > 0) { var p = _repaso[0]; _repaso.RemoveAt(0); Mostrar(p); return; }
                _releyendo = false;
            }

            // Lo que se explica en varias burbujas sale seguido; despues, lo que se aviso mientras tanto.
            var mismo = _disparadorActual;
            _disparadorActual = null;
            if (mismo != null && _guia.Siguiente(mismo) != null) { MostrarSiguiente(mismo); if (_actual != null) return; }
            while (_enEspera.Count > 0 && _actual == null) {
                var d = _enEspera[0];
                _enEspera.RemoveAt(0);
                MostrarSiguiente(d);
            }
        }

        private void Entendido() { Cerrar(true); }

        /// <summary>«¿Qué hago ahora?»: relee lo de la pantalla en la que estas, y si no hay, la ayuda de esa pantalla.</summary>
        private void Repetir() {
            if (_actual != null) return;
            var lista = _contexto != null ? _guia.Pasos(_contexto) : new List<PasoDeGuia>();
            if (lista.Count == 0) lista = _guia.Pasos(DisparadoresDeGuia.AyudaDe(_contexto) ?? "ayuda.dia");
            if (lista.Count == 0) lista.Add(new PasoDeGuia {
                Id = "AYUDA-GENERICA", Titulo = "¿Perdido?",
                Texto = "Mira el panel del centro: siempre dice qué puedes hacer ahora. Si algo brilla en amarillo, es por ahí."
            });
            _repaso.Clear();
            foreach (var p in lista) _repaso.Add(p.ParaReleer());
            _releyendo = true;
            var primero = _repaso[0];
            _repaso.RemoveAt(0);
            Mostrar(primero);
        }

        // ==================================================================== la burbuja

        private void Construir() {
            var ui = _app.Ui;
            var tema = ui.Tema;

            // El velo: tapa la pantalla mientras hay una burbuja que leer. Sin el, se podia pulsar por debajo.
            _velo = ui.Nodo(_app.CapaGuia, "Velo");
            UiKit.Rellenar(_velo);
            var velo = _velo.gameObject.AddComponent<Image>();
            velo.color = NexusTheme.Alfa(tema.bg950, 0.45f);
            velo.raycastTarget = true;

            _marco = ui.Nodo(_app.CapaGuia, "Marco");
            _marco.anchorMin = _marco.anchorMax = new Vector2(0.5f, 0.5f);
            foreach (var nombre in new[] { "arriba", "abajo", "izquierda", "derecha" }) {
                var borde = ui.Nodo(_marco, nombre).gameObject.AddComponent<Image>();
                borde.color = tema.warning;   // lo que la guia señala brilla en amarillo (asi lo dicen sus textos)
                borde.raycastTarget = false;
                _bordes.Add(borde);
            }
            UbicarBordes(5);

            _burbuja = ui.PanelColumna(_app.CapaGuia, "Burbuja", tema.margen, tema.espacio * 0.75f, tema.surfaceRaised);
            UiKit.ColorDeBorde(_burbuja, tema.warning);
            ui.Halo(_burbuja, tema.warning, NexusTheme.RadioLg);
            _burbuja.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _burbuja.sizeDelta = new Vector2(1040, 0);

            // Habla una persona: su retrato a la izquierda, como en una viñeta.
            var viñeta = ui.Fila(_burbuja, "Viñeta", tema.espacio, alineacion: TextAnchor.UpperLeft);
            _retrato = ui.Columna(viñeta, "Retrato", 0);
            UiKit.Tamano(_retrato, ancho: 120);
            var textos = ui.Columna(viñeta, "Textos", tema.espacio * 0.75f);
            UiKit.Tamano(textos, flexAncho: 1);
            _quien = ui.Texto(textos, "", EstiloTexto.Leyenda, tema.warning);
            _titulo = ui.Texto(textos, "", EstiloTexto.Encabezado, tema.ink);
            _texto = ui.Texto(textos, "", EstiloTexto.Dialogo, tema.ink);
            _textoMas = ui.PanelColumna(textos, "Mas", tema.espacio, 4, tema.surfaceSunken);
            _mas = ui.Texto(_textoMas, "", EstiloTexto.Pequeno, tema.inkMuted);

            var fila = ui.Fila(_burbuja);
            _botonMas = ui.Boton(fila, "Quiero saber más", () => _textoMas.gameObject.SetActive(!_textoMas.gameObject.activeSelf),
                                 VarianteBoton.Fantasma);
            ui.Resorte(fila);
            var esperando = ui.Fila(fila);
            ui.Texto(esperando, "Hazlo y la guía sigue…", EstiloTexto.Pequeno, tema.warning);
            ui.Boton(esperando, "Saltar", () => Cerrar(true), VarianteBoton.Fantasma);
            _esperando = esperando.gameObject;
            _entendido = ui.Boton(fila, "¡Entendido!", Entendido, VarianteBoton.Primario);

            Ocultar();
        }

        private void Mostrar(PasoDeGuia paso) {
            _actual = paso;
            _quien.text = (paso.Quien ?? "Guía").ToUpperInvariant() + " · GUÍA" + (_releyendo ? " · REPASO" : "");
            UiKit.Vaciar(_retrato);
            _app.Ui.Ilustracion(_retrato, MaterialesNexus.IdDePersonaje(paso.Quien ?? "Marisol Andrade"), null, 120, 140);
            _titulo.text = paso.Titulo ?? "";
            _titulo.gameObject.SetActive(!string.IsNullOrEmpty(paso.Titulo));
            _texto.text = paso.Texto ?? "";
            _mas.text = paso.Mas ?? "";
            _textoMas.gameObject.SetActive(false);
            _botonMas.gameObject.SetActive(!string.IsNullOrEmpty(paso.Mas));
            var espera = !string.IsNullOrEmpty(paso.EsperaAccion);
            _esperando.SetActive(espera);
            _entendido.gameObject.SetActive(!espera);
            _entendido.GetComponentInChildren<TMP_Text>().text = _releyendo && _repaso.Count > 0 ? "Siguiente" : "¡Entendido!";
            // Sin accion: modal. Con accion: el jugador tiene que poder pulsar lo que se le pide.
            _velo.gameObject.SetActive(!espera);
            _velo.SetAsLastSibling();
            _marco.SetAsLastSibling();
            _burbuja.gameObject.SetActive(true);
            _burbuja.SetAsLastSibling();
            // Se coloca en el LateUpdate: la pantalla que acaba de avisar aun no ha recalculado su layout.
            _colocarPendiente = true;
            _burbuja.anchoredPosition = new Vector2(0, -10000);
        }

        private void Ocultar() {
            if (_burbuja != null) _burbuja.gameObject.SetActive(false);
            if (_marco != null) _marco.gameObject.SetActive(false);
            if (_velo != null) _velo.gameObject.SetActive(false);
        }

        private static Func<RectTransform> _señalado;

        /// <summary>
        /// Señala una pieza con el marco parpadeante SIN abrir burbuja: lo usa el modo guiado de los minijuegos,
        /// que habla en su propio panel. La funcion se consulta cada frame (las piezas se rehacen al repintar).
        /// null deja de señalar.
        /// </summary>
        public static void Señalar(AppRoot app, Func<RectTransform> pieza) {
            _señalado = pieza;
            var g = Obtener(app);
            if (g != null && pieza == null && g._actual == null && g._marco != null) g._marco.gameObject.SetActive(false);
        }

        private RectTransform Objetivo() {
            RectTransform rt;
            if (_actual == null) {
                var libre = _señalado != null ? _señalado() : null;
                return libre != null && libre.gameObject.activeInHierarchy ? libre : null;
            }
            if (string.IsNullOrEmpty(_actual.Resaltar) || !_resaltables.TryGetValue(_actual.Resaltar, out rt)) return null;
            return rt != null && rt.gameObject.activeInHierarchy ? rt : null;
        }

        /// <summary>Abajo en el centro, salvo que lo señalado este abajo: entonces arriba, para no taparlo.</summary>
        private void Colocar(RectTransform objetivo) {
            var abajo = true;
            if (objetivo != null) {
                var centro = _app.CapaGuia.InverseTransformPoint(objetivo.TransformPoint(objetivo.rect.center));
                abajo = centro.y > 0;
            }
            _burbuja.anchorMin = _burbuja.anchorMax = new Vector2(0.5f, abajo ? 0 : 1);
            _burbuja.pivot = new Vector2(0.5f, abajo ? 0 : 1);
            _burbuja.anchoredPosition = new Vector2(0, abajo ? 36 : -36);
        }

        private void LateUpdate() {
            if (_actual == null && _señalado == null) return;
            if (_actual != null && _colocarPendiente) {
                _colocarPendiente = false;
                Canvas.ForceUpdateCanvases();
                Colocar(Objetivo());
            }
            var objetivo = Objetivo();
            _marco.gameObject.SetActive(objetivo != null);
            if (objetivo == null) return;

            var esquinas = new Vector3[4];
            objetivo.GetWorldCorners(esquinas);
            Vector2 min = _app.CapaGuia.InverseTransformPoint(esquinas[0]);
            Vector2 max = _app.CapaGuia.InverseTransformPoint(esquinas[2]);
            const float holgura = 8;
            _marco.sizeDelta = (max - min) + Vector2.one * holgura * 2;
            _marco.anchoredPosition = (min + max) * 0.5f;
            var alfa = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);
            foreach (var b in _bordes) { var c = b.color; c.a = alfa; b.color = c; }
        }

        private void UbicarBordes(float grosor) {
            // arriba, abajo, izquierda, derecha: cada uno pegado a su lado del marco
            Pegar(_bordes[0].rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -grosor), Vector2.zero);
            Pegar(_bordes[1].rectTransform, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, grosor));
            Pegar(_bordes[2].rectTransform, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(grosor, 0));
            Pegar(_bordes[3].rectTransform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-grosor, 0), Vector2.zero);
        }

        private static void Pegar(RectTransform rt, Vector2 anclaMin, Vector2 anclaMax, Vector2 offMin, Vector2 offMax) {
            rt.anchorMin = anclaMin;
            rt.anchorMax = anclaMax;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
        }

        private void OnDestroy() {
            if (_instancia == this) _instancia = null;
        }
    }
}
