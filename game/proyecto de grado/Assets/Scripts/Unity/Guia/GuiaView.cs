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
    ///   · Un paso sin accion se cierra con «¡Entendido!» y para el reloj mientras se lee.
    ///   · Un paso con accion («Pulse el pasillo») no para el reloj y se cierra solo cuando se hace.
    ///   · Cada paso sale una vez por perfil. «¿Qué hago ahora?» repite el ultimo.
    /// </summary>
    public sealed class GuiaView : MonoBehaviour {
        private static GuiaView _instancia;
        private static readonly Dictionary<string, RectTransform> _resaltables = new Dictionary<string, RectTransform>();

        private AppRoot _app;
        private GuiaDelTutorial _guia;
        private string _nivelDeLaGuia;
        private PasoDeGuia _actual, _ultimo;
        private string _disparadorActual;
        private readonly List<string> _enEspera = new List<string>();

        private RectTransform _burbuja, _marco, _textoMas;
        private TMP_Text _quien, _titulo, _texto, _mas;
        private Button _entendido, _botonMas;
        private GameObject _esperando;
        private readonly List<Image> _bordes = new List<Image>();

        /// <summary>Hay un paso abierto que para el reloj (el del dia y el de los minijuegos).</summary>
        public static bool PausaActiva {
            get { return _instancia != null && _instancia._actual != null && _instancia._actual.PausaElReloj; }
        }

        // ==================================================================== API de las pantallas

        public static void Registrar(string clave, Component pieza) {
            if (string.IsNullOrEmpty(clave) || pieza == null) return;
            _resaltables[clave] = pieza.transform as RectTransform;
        }

        public static void Avisar(AppRoot app, string disparador) {
            var g = Obtener(app);
            if (g == null) return;
            g.AlAvisar(disparador);
        }

        public static void Hecho(AppRoot app, string accion) {
            if (_instancia == null || _instancia._actual == null) return;
            if (AccionesDeGuia.Cumple(_instancia._actual.EsperaAccion, accion)) _instancia.Cerrar();
        }

        /// <summary>El boton «¿Qué hago ahora?». Solo aparece en niveles que tienen guia.</summary>
        public static void BotonDeAyuda(AppRoot app, Transform padre) {
            var g = Obtener(app);
            if (g == null || g._guia == null || !g._guia.Activa) return;
            app.Ui.Boton(padre, "¿Qué hago ahora?", () => g.Repetir(), VarianteBoton.Fantasma);
        }

        /// <summary>Al salir de la partida: la burbuja no se queda flotando sobre el menu.</summary>
        public static void Reiniciar() {
            if (_instancia == null) return;
            _instancia._guia = null;
            _instancia._nivelDeLaGuia = null;
            _instancia._actual = null;
            _instancia._enEspera.Clear();
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
            _ultimo = null;
            _enEspera.Clear();
            Ocultar();
        }

        private void AlAvisar(string disparador) {
            if (_guia == null || !_guia.Activa) return;
            if (_actual != null) {
                if (!_enEspera.Contains(disparador)) _enEspera.Add(disparador);
                return;
            }
            var paso = _guia.Siguiente(disparador);
            if (paso == null) return;
            _guia.MarcarVisto(paso);
            _app.GuardarPerfil();
            _disparadorActual = disparador;
            Mostrar(paso);
        }

        private void Cerrar() {
            _actual = null;
            Ocultar();
            // Lo que se explica en varias burbujas sale seguido; despues, lo que se aviso mientras tanto.
            var mismo = _disparadorActual;
            _disparadorActual = null;
            if (mismo != null && _guia.Siguiente(mismo) != null) { AlAvisar(mismo); return; }
            while (_enEspera.Count > 0 && _actual == null) {
                var d = _enEspera[0];
                _enEspera.RemoveAt(0);
                AlAvisar(d);
            }
        }

        private void Repetir() {
            if (_actual != null || _ultimo == null) return;
            Mostrar(_ultimo);
        }

        // ==================================================================== la burbuja

        private void Construir() {
            var ui = _app.Ui;
            var tema = ui.Tema;

            _marco = ui.Nodo(_app.CapaGuia, "Marco");
            _marco.anchorMin = _marco.anchorMax = new Vector2(0.5f, 0.5f);
            foreach (var nombre in new[] { "arriba", "abajo", "izquierda", "derecha" }) {
                var borde = ui.Nodo(_marco, nombre).gameObject.AddComponent<Image>();
                borde.color = tema.mostaza;
                borde.raycastTarget = false;
                _bordes.Add(borde);
            }
            UbicarBordes(4);

            _burbuja = ui.PanelColumna(_app.CapaGuia, "Burbuja", tema.margen, tema.espacio * 0.75f, tema.fondoSecundario);
            _burbuja.gameObject.AddComponent<Outline>().effectColor = tema.mostaza;
            _burbuja.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _burbuja.sizeDelta = new Vector2(1040, 0);

            _quien = ui.Texto(_burbuja, "", EstiloTexto.Pequeno, tema.mostazaClara);
            _titulo = ui.Texto(_burbuja, "", EstiloTexto.Subtitulo, tema.texto);
            _texto = ui.Texto(_burbuja, "", EstiloTexto.Cuerpo, tema.texto);
            _texto.fontSize = tema.tamCuerpo * 1.1f;
            _textoMas = ui.PanelColumna(_burbuja, "Mas", tema.espacio, 4, tema.pared);
            _mas = ui.Texto(_textoMas, "", EstiloTexto.Pequeno, tema.texto);

            var fila = ui.Fila(_burbuja);
            _botonMas = ui.Boton(fila, "Quiero saber más", () => _textoMas.gameObject.SetActive(!_textoMas.gameObject.activeSelf),
                                 VarianteBoton.Fantasma);
            ui.Resorte(fila);
            var esperando = ui.Fila(fila);
            ui.Texto(esperando, "Hágalo y la guía sigue…", EstiloTexto.Pequeno, tema.mostazaClara);
            ui.Boton(esperando, "Saltar", Cerrar, VarianteBoton.Fantasma);
            _esperando = esperando.gameObject;
            _entendido = ui.Boton(fila, "¡Entendido!", Cerrar, VarianteBoton.Primario);

            Ocultar();
        }

        private void Mostrar(PasoDeGuia paso) {
            _actual = paso;
            _ultimo = paso;
            _quien.text = (paso.Quien ?? "Guía").ToUpperInvariant() + " · GUÍA";
            _titulo.text = paso.Titulo ?? "";
            _titulo.gameObject.SetActive(!string.IsNullOrEmpty(paso.Titulo));
            _texto.text = paso.Texto ?? "";
            _mas.text = paso.Mas ?? "";
            _textoMas.gameObject.SetActive(false);
            _botonMas.gameObject.SetActive(!string.IsNullOrEmpty(paso.Mas));
            var espera = !string.IsNullOrEmpty(paso.EsperaAccion);
            _esperando.SetActive(espera);
            _entendido.gameObject.SetActive(!espera);
            _burbuja.gameObject.SetActive(true);
            _burbuja.SetAsLastSibling();
            Colocar(Objetivo());
        }

        private void Ocultar() {
            if (_burbuja != null) _burbuja.gameObject.SetActive(false);
            if (_marco != null) _marco.gameObject.SetActive(false);
        }

        private RectTransform Objetivo() {
            RectTransform rt;
            if (_actual == null || string.IsNullOrEmpty(_actual.Resaltar) || !_resaltables.TryGetValue(_actual.Resaltar, out rt)) return null;
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
            if (_actual == null) return;
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
