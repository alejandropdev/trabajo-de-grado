using System;
using System.IO;
using Newtonsoft.Json;
using Nexus.Core.Fase1;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Mundo3D {
    /// <summary>
    /// El mapa del recorrido 3D (tecla M o el boton «Mapa» del HUD): la ilustracion del edificio y, encima, un punto
    /// magenta donde esta el jugador, con un abanico hacia donde mira.
    ///
    ///   - NO para el reloj: mirar el mapa es parte del recorrido y cuesta su tiempo (decision del 2026-11-05). Pero
    ///     con el abierto el jugador no anda (AlAbrir/AlCerrar: la pantalla del recorrido lo para con nPause).
    ///   - La posicion se lleva al dibujo con PlanoDelRecorrido (una homografia: el dibujo esta en perspectiva y
    ///     girado respecto del mundo). Los numeros viven en StreamingAssets/mapa-3d.json.
    ///   - Si falta el JSON o la imagen, el mapa no existe: el boton no aparece y la M no hace nada (se avisa en el log).
    /// </summary>
    public sealed class MapaDelRecorrido : MonoBehaviour {
        /// <summary>El color del jugador en el mapa: el mismo magenta que «Estas aqui» en el mapa de la oficina.</summary>
        public static readonly Color ColorDelJugador = Nexus.Unity.Pantallas.PlanoIlustrado.ColorDelJugador;

        private const string Archivo = "mapa-3d.json";
        private const float Duracion = 0.16f;

        /// <summary>A quien seguir: el jugador del mundo 3D (lo pone la pantalla del recorrido al conectar).</summary>
        public Transform Jugador;

        public bool Disponible { get; private set; }
        public bool Abierto { get; private set; }

        private PlanoDelRecorrido _plano;
        private RectTransform _capa, _dibujo, _marca, _anillo, _abanico;
        private Image _imgAnillo;
        private CanvasGroup _grupo;
        private float _visible;   // 0 = cerrado, 1 = abierto (para el fundido)
        private RectTransform _botonDelHud;

        public static MapaDelRecorrido Crear(UiKit ui, RectTransform padre) {
            var go = new GameObject("Mapa del recorrido", typeof(RectTransform));
            go.transform.SetParent(padre, false);
            var mapa = go.AddComponent<MapaDelRecorrido>();
            mapa.Construir(ui, (RectTransform)go.transform);
            return mapa;
        }

        private void Construir(UiKit ui, RectTransform raiz) {
            UiKit.Rellenar(raiz);
            var tema = ui.Tema;

            _plano = Leer();
            var textura = _plano != null && !string.IsNullOrEmpty(_plano.Imagen) ? Resources.Load<Texture2D>(_plano.Imagen) : null;
            if (_plano == null || textura == null || !_plano.EsValido) {
                Debug.LogWarning($"[Nexus] El recorrido no tiene mapa: falta o no es valido StreamingAssets/{Archivo} " +
                                 $"o la imagen «{_plano?.Imagen}» en Resources.");
                Disponible = false;
                raiz.gameObject.SetActive(false);
                return;
            }
            Disponible = true;

            // Lienzo propio, por encima del HUD de la escena 3D y por debajo de la pausa (500).
            var lienzo = raiz.gameObject.AddComponent<Canvas>();
            lienzo.overrideSorting = true;
            lienzo.sortingOrder = 400;
            _grupo = raiz.gameObject.AddComponent<CanvasGroup>();
            _grupo.blocksRaycasts = false;   // no se queda con clics: se sigue jugando con el abierto
            _grupo.interactable = false;
            _grupo.alpha = 0;
            _capa = raiz;

            // Un velo suave: el mundo se sigue viendo detras, el mapa se lee.
            var velo = ui.Nodo(raiz, "Velo");
            UiKit.Rellenar(velo);
            var imgVelo = velo.gameObject.AddComponent<Image>();
            imgVelo.color = NexusTheme.Alfa(tema.bg950, 0.55f);
            imgVelo.raycastTarget = false;

            // El marco: el 84 % del ancho y el 80 % del alto, y dentro el dibujo con su proporcion.
            var hueco = ui.Nodo(raiz, "Hueco");
            hueco.anchorMin = new Vector2(0.08f, 0.07f);
            hueco.anchorMax = new Vector2(0.92f, 0.87f);
            hueco.offsetMin = hueco.offsetMax = Vector2.zero;

            _dibujo = ui.Nodo(hueco, "Plano");
            var ajuste = _dibujo.gameObject.AddComponent<AspectRatioFitter>();
            ajuste.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            ajuste.aspectRatio = _plano.AnchoImagen / _plano.AltoImagen;
            var imagen = _dibujo.gameObject.AddComponent<RawImage>();
            imagen.texture = textura;
            imagen.raycastTarget = false;
            ui.Borde(_dibujo, NexusTheme.Alfa(tema.cyan, 0.55f), 4, 1.5f);
            ui.Halo(_dibujo, NexusTheme.Alfa(tema.cyan, 0.35f), 4);

            // El titulo, encima del marco.
            var titulo = ui.Fila(raiz, "Titulo del mapa", tema.Espacio(3));
            titulo.anchorMin = new Vector2(0.08f, 0.88f);
            titulo.anchorMax = new Vector2(0.92f, 0.95f);
            titulo.offsetMin = titulo.offsetMax = Vector2.zero;
            ui.Texto(titulo, "MAPA DEL EDIFICIO", EstiloTexto.Leyenda, tema.cyan);
            UiKit.Tamano(ui.Punto(titulo, ColorDelJugador, 10), 14, 14);
            ui.Texto(titulo, "Tú estás aquí · el reloj sigue corriendo · <b>M</b> o <b>Esc</b> para volver a andar",
                     EstiloTexto.Pequeno, tema.inkMuted);

            // La marca del jugador: el abanico de hacia donde mira, un anillo que late y el punto, con «Tú».
            _marca = ui.Nodo(_dibujo, "Jugador");
            _marca.sizeDelta = Vector2.zero;

            _abanico = Circulo(ui, _marca, "Abanico", 84, NexusTheme.Alfa(ColorDelJugador, 0.55f));
            _abanico.GetComponent<Image>().sprite = Abanico();
            _anillo = Circulo(ui, _marca, "Anillo", 26, ColorDelJugador);
            _imgAnillo = _anillo.GetComponent<Image>();
            Circulo(ui, _marca, "Borde", 24, Color.white);
            Circulo(ui, _marca, "Punto", 17, ColorDelJugador);

            var tu = ui.Texto(_marca, "Tú", EstiloTexto.Etiqueta, Color.white, TextAlignmentOptions.Center);
            tu.textWrappingMode = TextWrappingModes.NoWrap;
            tu.outlineWidth = 0.25f;
            tu.outlineColor = new Color32(0x3A, 0x00, 0x22, 0xFF);
            var rtTu = tu.rectTransform;
            rtTu.sizeDelta = new Vector2(60, 24);
            rtTu.anchoredPosition = new Vector2(0, -26);

            raiz.gameObject.SetActive(false);
        }

        private static RectTransform Circulo(UiKit ui, Transform padre, string nombre, float lado, Color color) {
            var rt = ui.Nodo(padre, nombre);
            rt.sizeDelta = new Vector2(lado, lado);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ui.SpriteCircular();
            img.color = color;
            img.raycastTarget = false;
            return rt;
        }

        // ==================================================================== el boton del HUD

        private RectTransform _hud;

        /// <summary>
        /// Lo del HUD que pone el juego (el boton del mapa y las teclas dibujadas) va en un lienzo PROPIO, encima del de
        /// la escena. Antes la «E» se metia dentro del boton del inventario de la escena, y su lienzo, con otra escala,
        /// la pintaba enorme tapando el boton (2026-11-05).
        /// </summary>
        private RectTransform Hud(UiKit ui, RectTransform padre) {
            if (_hud != null) return _hud;
            _hud = ui.Nodo(padre, "HUD del juego");
            UiKit.Rellenar(_hud);
            var lienzo = _hud.gameObject.AddComponent<Canvas>();
            lienzo.overrideSorting = true;
            lienzo.sortingOrder = 300;   // encima del HUD de la escena, debajo del mapa (400) y la pausa (500)
            _hud.gameObject.AddComponent<GraphicRaycaster>();
            return _hud;
        }

        /// <summary>
        /// El boton explicito «Mapa», pequeño, con su tecla M en la esquina. Arriba a la derecha; en cuanto se sabe donde
        /// esta el boton del inventario de la escena, se queda justo debajo de el (SeguirDebajoDe).
        /// </summary>
        public void CrearBoton(UiKit ui, RectTransform padre) {
            if (!Disponible || _botonDelHud != null) return;
            var boton = ui.Boton(Hud(ui, padre), "Mapa", Alternar, VarianteBoton.Secundario);
            _botonDelHud = (RectTransform)boton.transform;
            _botonDelHud.anchorMin = _botonDelHud.anchorMax = _botonDelHud.pivot = new Vector2(1, 1);
            _botonDelHud.sizeDelta = new Vector2(120, 40);
            _botonDelHud.anchoredPosition = new Vector2(-ui.Tema.margen, -150);
            var tecla = Tecla(ui, _botonDelHud, "M");
            tecla.anchorMin = tecla.anchorMax = new Vector2(1, 0);
            tecla.anchoredPosition = new Vector2(-2, 2);
        }

        /// <summary>El boton del mapa se queda debajo de 'referencia' (el boton del inventario de la escena 3D).</summary>
        public void SeguirDebajoDe(RectTransform referencia) {
            if (_botonDelHud == null || referencia == null) return;
            _botonDelHud.anchorMin = _botonDelHud.anchorMax = new Vector2(0.5f, 0.5f);
            var s = _botonDelHud.gameObject.AddComponent<SeguirEsquina>();
            s.Objetivo = Visible(referencia, out s.Punto);
            s.Desplazamiento = new Vector2(0, -10);
            s.OcultarSinObjetivo = false;
        }

        /// <summary>Una tecla pequeña en la esquina de abajo a la derecha de un boton de la escena («E» en el inventario).</summary>
        public void MarcarTecla(UiKit ui, RectTransform padre, RectTransform boton, string tecla) {
            if (boton == null) return;
            var rt = Tecla(ui, Hud(ui, padre), tecla);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            var s = rt.gameObject.AddComponent<SeguirEsquina>();
            s.Objetivo = Visible(boton, out s.Punto);
            s.Desplazamiento = new Vector2(-4, 4);   // un poco hacia dentro: se lee como parte del boton
        }

        /// <summary>
        /// Lo que de verdad se ve de un boton de la escena, y donde cae su esquina de abajo a la derecha (en fracciones de
        /// ese rect). El «BotonInventario» de la escena es un rect de 12 x 5 escalado x45 que se sale por la derecha de
        /// la pantalla; lo que se ve es su hijo «ImageInventario», y su sprite tiene un margen transparente abajo (lo
        /// pintado va de x 19..687 y de y 59..289 en 707 x 353). Seguir el rect del boton dejaba la «E» fuera de la
        /// pantalla (2026-11-05).
        /// </summary>
        private static RectTransform Visible(RectTransform boton, out Vector2 punto) {
            punto = new Vector2(1, 0);
            Image mejor = null;
            var area = 0f;
            foreach (var img in boton.GetComponentsInChildren<Image>(true)) {
                if (img.sprite == null) continue;
                var r = img.rectTransform.rect;
                var a = Mathf.Abs(r.width * img.rectTransform.lossyScale.x * r.height * img.rectTransform.lossyScale.y);
                if (a > area) { area = a; mejor = img; }
            }
            if (mejor == null) return boton;
            if (mejor.sprite.name.Contains("BotonInventario")) punto = new Vector2(687f / 707f, (353f - 289f) / 353f);
            return mejor.rectTransform;
        }

        /// <summary>
        /// Pone una pieza de NUESTRO lienzo sobre un punto (Punto, en fracciones del rect) de una pieza de OTRO lienzo (el
        /// de la escena), cada frame: sigue al boton aunque cambie la resolucion, se oculta si el boton se oculta, y nunca
        /// se sale de la pantalla.
        /// </summary>
        private sealed class SeguirEsquina : MonoBehaviour {
            public RectTransform Objetivo;
            public Vector2 Punto = new Vector2(1, 0);
            public Vector2 Desplazamiento;
            public bool OcultarSinObjetivo = true;
            private CanvasGroup _grupo;
            private readonly Vector3[] _esquinas = new Vector3[4];

            private void Awake() {
                _grupo = GetComponent<CanvasGroup>();
                if (_grupo == null) _grupo = gameObject.AddComponent<CanvasGroup>();
            }

            private void LateUpdate() {
                var visible = Objetivo != null && Objetivo.gameObject.activeInHierarchy;
                if (OcultarSinObjetivo) {
                    _grupo.alpha = visible ? 1 : 0;
                    _grupo.blocksRaycasts = visible;
                }
                if (!visible) return;
                var suLienzo = Objetivo.GetComponentInParent<Canvas>();
                var padre = transform.parent as RectTransform;
                var miLienzo = padre != null ? padre.GetComponentInParent<Canvas>() : null;
                if (suLienzo == null || miLienzo == null) return;
                var suRaiz = suLienzo.rootCanvas;
                var miRaiz = miLienzo.rootCanvas;
                var suCamara = suRaiz.renderMode == RenderMode.ScreenSpaceOverlay ? null : suRaiz.worldCamera;
                var miCamara = miRaiz.renderMode == RenderMode.ScreenSpaceOverlay ? null : miRaiz.worldCamera;
                Objetivo.GetWorldCorners(_esquinas);   // 0 abajo-izq, 1 arriba-izq, 2 arriba-der, 3 abajo-der
                var abajo = Vector3.Lerp(_esquinas[0], _esquinas[3], Punto.x);
                var arriba = Vector3.Lerp(_esquinas[1], _esquinas[2], Punto.x);
                var mundo = Vector3.Lerp(abajo, arriba, Punto.y);
                var enPantalla = RectTransformUtility.WorldToScreenPoint(suCamara, mundo);
                Vector2 local;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, enPantalla, miCamara, out local)) return;

                // Dentro de la pantalla siempre, con un margen: la pieza entera se ve, aunque el boton se salga.
                var rt = (RectTransform)transform;
                var r = padre.rect;
                var tam = rt.rect.size;
                const float margen = 8f;
                var p = local + Desplazamiento;
                p.x = Mathf.Clamp(p.x, r.xMin + margen + tam.x * rt.pivot.x, r.xMax - margen - tam.x * (1 - rt.pivot.x));
                p.y = Mathf.Clamp(p.y, r.yMin + margen + tam.y * rt.pivot.y, r.yMax - margen - tam.y * (1 - rt.pivot.y));
                // Con anclas en el centro del padre, la posicion anclada es la local menos el centro del rect del padre.
                rt.anchoredPosition = p - r.center;
            }
        }

        /// <summary>La tecla dibujada como tecla (26 x 26): un recuadro oscuro con borde cyan y la letra.</summary>
        public static RectTransform Tecla(UiKit ui, Transform padre, string tecla) {
            var tema = ui.Tema;
            var rt = ui.Nodo(padre, "Tecla " + tecla);
            rt.pivot = new Vector2(1, 0);
            rt.sizeDelta = new Vector2(26, 26);
            rt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var fondo = rt.gameObject.AddComponent<Image>();
            fondo.sprite = Formas.Caja(6);
            fondo.type = Image.Type.Sliced;
            fondo.color = tema.surfaceRaised;
            fondo.raycastTarget = false;
            ui.Borde(rt, tema.cyan, 4, 1.5f);
            var t = ui.Texto(rt, tecla, EstiloTexto.Mono, tema.cyan, TextAlignmentOptions.Center);
            t.fontStyle = FontStyles.Bold;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Rellenar(t.rectTransform);
            return rt;
        }

        // ==================================================================== abrir y cerrar

        public void Alternar() {
            if (Abierto) Cerrar(); else Abrir();
        }

        /// <summary>
        /// Al abrir y al cerrar: quien maneja el recorrido para y suelta al jugador (mirando el mapa no se anda), pero el
        /// reloj sigue corriendo.
        /// </summary>
        public Action AlAbrir, AlCerrar;

        public void Abrir() {
            if (!Disponible || Abierto) return;
            Abierto = true;
            _capa.gameObject.SetActive(true);
            Seguir();
            AlAbrir?.Invoke();
        }

        public void Cerrar() {
            if (!Abierto) return;
            Abierto = false;
            AlCerrar?.Invoke();
        }

        private void LateUpdate() {
            if (!Disponible || _capa == null) return;
            var dt = Time.unscaledDeltaTime;
            _visible = Mathf.MoveTowards(_visible, Abierto ? 1 : 0, dt / Duracion);
            var e = Mathf.SmoothStep(0, 1, _visible);
            _grupo.alpha = e;
            _dibujo.localScale = Vector3.one * Mathf.Lerp(0.97f, 1f, e);
            if (_visible <= 0 && !Abierto) {
                if (_capa.gameObject.activeSelf) _capa.gameObject.SetActive(false);
                return;
            }
            Seguir();
            // El anillo late (tiempo no escalado: si el mundo se congela, el mapa sigue vivo).
            var t = (Time.unscaledTime * 1.1f) % 1f;
            _anillo.localScale = Vector3.one * Mathf.Lerp(1f, 2.6f, t);
            _imgAnillo.color = NexusTheme.Alfa(ColorDelJugador, 0.75f * (1 - t));
        }

        /// <summary>Lleva la marca a donde esta el jugador en el dibujo, y el abanico a hacia donde mira.</summary>
        private void Seguir() {
            if (_marca == null) return;
            if (Jugador == null || _plano == null) { _marca.gameObject.SetActive(false); return; }
            var p = Jugador.position;
            float u, v;
            if (!_plano.Proyectar(p.x, p.z, out u, out v)) { _marca.gameObject.SetActive(false); return; }
            _marca.gameObject.SetActive(true);
            _marca.anchorMin = _marca.anchorMax = new Vector2(Mathf.Clamp01(u), 1 - Mathf.Clamp01(v));
            _marca.anchoredPosition = Vector2.zero;

            // Hacia donde mira: la camara (lo que se ve), aplanada al suelo; si no hay, hacia donde mira el cuerpo.
            var camara = Camera.main;
            var adelante = camara != null ? camara.transform.forward : Jugador.forward;
            float grados;
            if (_plano.Rumbo(p.x, p.z, adelante.x, adelante.z, out grados)) {
                _abanico.gameObject.SetActive(true);
                // Rumbo: 0 = derecha, creciendo hacia ABAJO de la imagen. En la UI la Y crece hacia arriba: se invierte.
                _abanico.localEulerAngles = new Vector3(0, 0, -grados);
            } else {
                _abanico.gameObject.SetActive(false);
            }
        }

        // ==================================================================== datos

        private static PlanoDelRecorrido Leer() {
            try {
                var ruta = Path.Combine(Application.streamingAssetsPath, Archivo);
                if (!File.Exists(ruta)) return null;
                return JsonConvert.DeserializeObject<PlanoDelRecorrido>(File.ReadAllText(ruta));
            } catch (Exception ex) {
                Debug.LogWarning($"[Nexus] No se pudo leer {Archivo}: {ex.Message}");
                return null;
            }
        }

        private static Sprite _abanicoCache;

        /// <summary>
        /// El abanico de vision: una cuña de ±32° que apunta a la derecha (+X) desde el centro y se desvanece con la
        /// distancia. Generado en memoria, como las formas del design system.
        /// </summary>
        private static Sprite Abanico() {
            if (_abanicoCache != null) return _abanicoCache;
            const int lado = 96;
            var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false) { name = "abanico", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[lado * lado];
            var c = lado / 2f;
            for (var y = 0; y < lado; y++)
                for (var x = 0; x < lado; x++) {
                    float dx = x + 0.5f - c, dy = y + 0.5f - c;
                    var r = Mathf.Sqrt(dx * dx + dy * dy) / c;
                    var ang = Mathf.Abs(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
                    var a = 0f;
                    if (r > 0.12f && r < 1f && ang < 32f)
                        a = Mathf.Clamp01((32f - ang) / 8f) * (1 - r) * 1.4f;
                    px[y * lado + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _abanicoCache = Sprite.Create(tex, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), 100);
            return _abanicoCache;
        }
    }
}
