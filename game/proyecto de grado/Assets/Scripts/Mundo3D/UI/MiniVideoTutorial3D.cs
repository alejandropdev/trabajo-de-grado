using System;
using System.Collections;
using System.Collections.Generic;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Nexus.Mundo3D {
    /// <summary>
    /// Los mini-videos del tutorial del recorrido 3D (2026-11-20): una ventana ABAJO A LA DERECHA que reproduce, uno tras
    /// otro y DE CORRIDO, los videos de Resources/Tutorial3D. El mundo sigue vivo y el jugador se mueve mientras pasan.
    /// Los videos NO llevan sonido (audioOutputMode = None). Enter salta el que se esta viendo.
    ///
    /// De corrido: hay DOS reproductores que se turnan. Mientras uno muestra su video, el otro ya prepara el siguiente,
    /// asi que al acabar uno empieza el otro sin parpadeo ni hueco negro.
    ///
    /// Con el mundo congelado (la pausa o el inventario: Time.timeScale = 0) el video se pausa solo, y sigue donde iba
    /// al soltar el mundo.
    /// </summary>
    public sealed class MiniVideoTutorial3D : MonoBehaviour {
        /// <summary>Id para el perfil (videosVistos), titulo y ruta en Resources, en el orden en que se ven.</summary>
        public static readonly string[] Ids = { "3d.introduccion", "3d.mapa", "3d.inventario", "3d.recursos" };
        public static readonly string[] Titulos = { "Introducción", "Exploración y mapa", "Inventario", "Recursos" };
        private static readonly string[] Rutas = {
            "Tutorial3D/1-introduccion", "Tutorial3D/2-exploracion-y-mapa", "Tutorial3D/3-inventario", "Tutorial3D/4-recursos"
        };

        /// <summary>Se llama al terminar (o saltar) cada video que SE VIO, con su id: para anotarlo en el perfil.</summary>
        public Action<string> AlTerminarUno;
        /// <summary>Se llama cuando ya no queda ninguno de la cola.</summary>
        public Action AlTerminarTodo;

        /// <summary>La ventana esta a la vista (para apartar de su rincon lo que estorbe).</summary>
        public bool Visible { get; private set; }

        /// <summary>El alto aproximado de la ventana con su cabecera, para apartar lo que estorbe de ese rincon.</summary>
        public float AltoAproximado { get { return _panel != null ? _panel.rect.height : 0f; } }

        private const float AnchoDeReferencia = 560f;

        private CanvasGroup _grupo;
        private RectTransform _panel;
        private RawImage _imagen;
        private LayoutElement _hueco;
        private float _ancho;
        private readonly VideoPlayer[] _vp = new VideoPlayer[2];
        private readonly RenderTexture[] _tx = new RenderTexture[2];
        private readonly bool[] _fin = new bool[2];
        private readonly bool[] _fallo = new bool[2];
        private TMP_Text _contador, _titulo;
        private BarraView _avance;
        private Coroutine _cola;
        private bool _saltar, _pausadoPorElJuego;
        private int _actual;
        private float _alfa;

        public static MiniVideoTutorial3D Crear(UiKit ui, Transform padre) {
            var raiz = ui.Nodo(padre, "Mini-video del tutorial");
            UiKit.Rellenar(raiz);
            // Lienzo propio, por encima del HUD de la escena 3D pero por debajo de la pausa (500).
            var lienzo = raiz.gameObject.AddComponent<Canvas>();
            lienzo.overrideSorting = true;
            lienzo.sortingOrder = 250;
            raiz.gameObject.AddComponent<GraphicRaycaster>();
            var m = raiz.gameObject.AddComponent<MiniVideoTutorial3D>();
            m.Construir(ui, raiz);
            return m;
        }

        private void Construir(UiKit ui, RectTransform raiz) {
            var tema = ui.Tema;
            _ancho = tema.Px(AnchoDeReferencia);
            _grupo = raiz.gameObject.AddComponent<CanvasGroup>();
            _grupo.alpha = 0;
            _grupo.blocksRaycasts = false;   // solo informa; el boton se activa mientras se ve

            _panel = ui.PanelColumna(raiz, "Ventana", tema.Espacio(2), tema.Espacio(1), NexusTheme.Alfa(tema.bg950, 0.92f));
            _panel.anchorMin = _panel.anchorMax = _panel.pivot = new Vector2(1, 0);
            _panel.anchoredPosition = new Vector2(-tema.margen, tema.margen);
            _panel.sizeDelta = new Vector2(_ancho, 0);
            _panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ui.Borde(_panel, NexusTheme.Alfa(tema.cyan, 0.8f), NexusTheme.RadioMd, 1.5f);

            var cabecera = ui.Fila(_panel, "Cabecera", tema.Espacio(2), 0, TextAnchor.MiddleLeft);
            var textos = ui.Columna(cabecera, "Titulos", 0);
            UiKit.Tamano(textos, flexAncho: 1);
            _contador = ui.Texto(textos, "TUTORIAL", EstiloTexto.Leyenda, tema.cyan);
            _titulo = ui.Texto(textos, "", EstiloTexto.Cuerpo, tema.ink);
            var saltar = ui.Boton(cabecera, "Saltar (Enter)", Saltar, VarianteBoton.Fantasma);
            UiKit.Tamano(saltar, ancho: tema.Px(170));

            var pantalla = ui.Nodo(_panel, "Video");
            _hueco = UiKit.Tamano(pantalla, alto: _ancho * 9f / 16f, flexAncho: 1);
            _imagen = pantalla.gameObject.AddComponent<RawImage>();
            _imagen.color = Color.black;
            _imagen.raycastTarget = false;

            _avance = ui.Barra(_panel, 0f, tema.cyan);
            UiKit.Tamano(_avance, alto: 4);

            for (var k = 0; k < 2; k++) {
                var indice = k;
                var vp = gameObject.AddComponent<VideoPlayer>();
                vp.playOnAwake = false;
                vp.isLooping = false;
                vp.audioOutputMode = VideoAudioOutputMode.None;   // sin sonido, de ninguna manera
                vp.renderMode = VideoRenderMode.RenderTexture;
                vp.timeUpdateMode = VideoTimeUpdateMode.GameTime;   // con timeScale 0 se queda quieto, como el juego
                vp.skipOnDrop = true;
                vp.loopPointReached += _ => _fin[indice] = true;
                vp.errorReceived += (_, msg) => { Debug.LogWarning("[Nexus] Video del tutorial: " + msg); _fallo[indice] = true; };
                _vp[k] = vp;
            }
            _panel.gameObject.SetActive(false);
        }

        /// <summary>Pone en cola esos videos (indices de Ids) y los pasa de corrido. Si ya habia cola, la cambia.</summary>
        public void Reproducir(IList<int> indices) {
            Detener();
            if (indices == null || indices.Count == 0) return;
            _cola = StartCoroutine(PasarLaCola(new List<int>(indices)));
        }

        /// <summary>Salta el video que se esta viendo (el siguiente de la cola empieza solo).</summary>
        public void Saltar() { _saltar = true; }

        /// <summary>Corta lo que haya, sin avisar a nadie, y quita la ventana.</summary>
        public void Detener() {
            if (_cola != null) StopCoroutine(_cola);
            _cola = null;
            for (var k = 0; k < 2; k++) if (_vp[k] != null) _vp[k].Stop();
            Visible = false;
            _saltar = _pausadoPorElJuego = false;
            if (_panel != null) _panel.gameObject.SetActive(false);
            if (_grupo != null) { _grupo.alpha = 0; _grupo.blocksRaycasts = false; }
            _alfa = 0;
        }

        /// <summary>Deja listo el video «i» en el reproductor «k»: se carga y se prepara mientras el otro sigue a lo suyo.</summary>
        private void Preparar(int k, int i) {
            _fin[k] = _fallo[k] = false;
            var clip = i >= 0 && i < Rutas.Length ? Resources.Load<VideoClip>(Rutas[i]) : null;
            if (clip == null) {
                Debug.LogWarning("[Nexus] Falta el video del tutorial: " + (i >= 0 && i < Rutas.Length ? Rutas[i] : "#" + i));
                _fallo[k] = true;
                return;
            }
            var ancho = (int)Mathf.Clamp(clip.width, 320, 1280);
            var alto = (int)Mathf.Clamp(clip.height, 180, 720);
            if (_tx[k] == null || _tx[k].width != ancho || _tx[k].height != alto) {
                if (_tx[k] != null) { _vp[k].targetTexture = null; _tx[k].Release(); Destroy(_tx[k]); }
                _tx[k] = new RenderTexture(ancho, alto, 0, RenderTextureFormat.ARGB32) { name = "Tutorial 3D " + k };
            }
            _vp[k].Stop();
            _vp[k].clip = clip;
            _vp[k].targetTexture = _tx[k];
            _vp[k].Prepare();
        }

        private IEnumerator PasarLaCola(List<int> cola) {
            var k = 0;
            Preparar(k, cola[0]);
            for (var n = 0; n < cola.Count; n++) {
                var i = cola[n];
                var espera = 0f;
                while (!_vp[k].isPrepared && !_fallo[k] && espera < 8f && !_saltar) { espera += Time.unscaledDeltaTime; yield return null; }
                var visto = false;
                if (_vp[k].isPrepared && !_fallo[k] && !_saltar) {
                    Mostrar(k, n, cola.Count, i);
                    visto = true;
                    _vp[k].Play();
                    // El siguiente se va preparando ya, para que entre sin hueco.
                    if (n + 1 < cola.Count) Preparar(1 - k, cola[n + 1]);
                    yield return null;
                    while (!_saltar && !_fin[k] && !_fallo[k]) {
                        if (_vp[k].length > 0) _avance.Valor = Mathf.Clamp01((float)(_vp[k].time / _vp[k].length));
                        yield return null;
                    }
                } else if (n + 1 < cola.Count) {
                    Preparar(1 - k, cola[n + 1]);
                }
                _vp[k].Stop();
                if (visto) AlTerminarUno?.Invoke(Ids[i]);
                _saltar = false;
                k = 1 - k;
            }
            Visible = false;
            _cola = null;
            AlTerminarTodo?.Invoke();
        }

        /// <summary>Pone el reproductor «k» en la ventana: su imagen, su proporcion y sus textos.</summary>
        private void Mostrar(int k, int n, int total, int i) {
            _actual = k;
            _imagen.texture = _tx[k];
            _imagen.color = Color.white;
            _hueco.preferredHeight = Mathf.Round(_ancho * _tx[k].height / _tx[k].width);
            _contador.text = "TUTORIAL · " + (n + 1) + " DE " + total;
            _titulo.text = Titulos[i];
            _avance.Valor = 0;
            _panel.gameObject.SetActive(true);
            Visible = true;
            _grupo.blocksRaycasts = true;
        }

        private void Update() {
            var objetivo = Visible ? 1f : 0f;
            _alfa = Mathf.MoveTowards(_alfa, objetivo, Time.unscaledDeltaTime * 5f);
            _grupo.alpha = _alfa;
            if (!Visible && _alfa <= 0f && _panel.gameObject.activeSelf && _cola == null) {
                _panel.gameObject.SetActive(false);
                _grupo.blocksRaycasts = false;
            }
            if (!Visible) return;

            // El juego congelado (pausa, inventario) congela el video; al soltarlo, sigue donde iba.
            var congelado = Time.timeScale <= 0f;
            var vp = _vp[_actual];
            if (congelado && vp.isPlaying) { vp.Pause(); _pausadoPorElJuego = true; }
            else if (!congelado && _pausadoPorElJuego) { _pausadoPorElJuego = false; if (vp.isPaused) vp.Play(); }

            // Enter salta, pero no con el juego congelado (ahi Enter es de los botones del menu).
            var teclado = Keyboard.current;
            if (!congelado && teclado != null && teclado.enterKey.wasPressedThisFrame) _saltar = true;
        }

        private void OnDestroy() {
            for (var k = 0; k < 2; k++) {
                if (_vp[k] != null) _vp[k].targetTexture = null;
                if (_tx[k] != null) { _tx[k].Release(); Destroy(_tx[k]); }
            }
        }
    }
}
