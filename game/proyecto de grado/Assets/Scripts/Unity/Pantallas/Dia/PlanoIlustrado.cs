using System;
using System.Collections.Generic;
using Nexus.Core.Coleccion;
using Nexus.Core.Jornada;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El mapa de la oficina dibujado: la ilustracion del nivel (MapaDeZonas.Imagen) con cada zona recortada encima de
    /// su sala (ZonaDeNivel.Area). Inspirado en el mapa de Ori — lo que se puede visitar brilla, lo demas queda en
    /// penumbra — y con tres cosas propias:
    ///
    ///   - Al pasar por una sala, SOLO esa sala crece (1,07) y se ilumina, su nombre crece (1,35) y las demas se
    ///     hunden en niebla. Una linea de luz va de «Estas aqui» a la sala con lo que cuesta ir: el viaje se ve antes
    ///     de decidirlo.
    ///   - Las salas cerradas se ven, pero bajo niebla espesa: la progresion se nota en el plano (C11).
    ///   - Los coleccionables de la sala donde estas son destellos que hay que atrapar (Destello + HuidaDelDestello).
    ///
    /// Es solo dibujo: lo que significa cada cosa (que zona esta abierta, cuanto cuesta, que pasa al atrapar) lo
    /// decide quien lo crea, a traves de DatosDelPlano.
    /// </summary>
    public sealed class PlanoIlustrado : MonoBehaviour {
        /// <summary>El color de «tu» en los mapas (tambien el punto del recorrido 3D): un magenta que no usa nada mas.</summary>
        public static readonly Color ColorDelJugador = new Color32(0xFF, 0x3D, 0xAE, 0xFF);

        /// <summary>El color de los destellos: luz calida, que destaca sobre una ilustracion fria y nocturna.</summary>
        public static readonly Color ColorDelDestello = new Color32(0xFF, 0xE3, 0x8A, 0xFF);

        public sealed class DatosDelPlano {
            public string ZonaActual;
            public bool PuedeMoverse;
            public Func<string, bool> Abierta;
            /// <summary>Lo que se lee bajo el nombre: «Estas aqui», «20 min», «Cerrada por ahora».</summary>
            public Func<ZonaDeNivel, string> Detalle;
            /// <summary>Lo que cuesta ir, para la linea de viaje («20 min»). Null o vacio: no se dibuja la cifra.</summary>
            public Func<string, string> Coste;
            public Action<string> AlViajar;
            /// <summary>La sala que se esta mirando cambio (null = ninguna: vuelve a la actual).</summary>
            public Action<string> AlEnfocar;
            /// <summary>Los coleccionables que ya brillan en la sala actual: id y dificultad.</summary>
            public List<KeyValuePair<string, string>> Destellos = new List<KeyValuePair<string, string>>();
            /// <summary>Los que todavia no han aparecido aqui: solo un parpadeo del color de su dificultad.</summary>
            public List<Color> PorAparecer = new List<Color>();
            public Action<string> AlAtrapar;
            /// <summary>Para la guia: cada sala se registra como «mapa.&lt;id&gt;».</summary>
            public Action<string, Component> Registrar;
        }

        private UiKit _ui;
        private NexusTheme _tema;
        private DatosDelPlano _datos;
        private RectTransform _raiz;
        private readonly List<Seccion> _secciones = new List<Seccion>();
        private Seccion _encima, _actual, _viajando;
        private float _tiempoDeViaje;
        private RectTransform _ruta;
        private readonly List<Image> _puntosDeRuta = new List<Image>();
        private RectTransform _placaDeCoste;
        private TMP_Text _textoDeCoste;

        public RectTransform Raiz { get { return _raiz; } }

        /// <summary>Crea el plano con 'ancho' de ancho (el alto sale de la proporcion de la imagen).</summary>
        public static PlanoIlustrado Crear(UiKit ui, Transform padre, Texture2D imagen, MapaDeZonas mapa,
                                           DatosDelPlano datos, float ancho) {
            var rt = ui.Nodo(padre, "Plano ilustrado");
            var alto = ancho * imagen.height / Mathf.Max(1f, imagen.width);
            UiKit.Tamano(rt, ancho, alto);
            rt.sizeDelta = new Vector2(ancho, alto);
            var plano = rt.gameObject.AddComponent<PlanoIlustrado>();
            // Las salas son botones, pero son el contenido del mapa: no cuentan para la regla de los 7 botones.
            rt.gameObject.AddComponent<ListaDeElementos>();
            plano.Construir(ui, rt, imagen, mapa, datos);
            return plano;
        }

        private void Construir(UiKit ui, RectTransform raiz, Texture2D imagen, MapaDeZonas mapa, DatosDelPlano datos) {
            _ui = ui;
            _tema = ui.Tema;
            _datos = datos;
            _raiz = raiz;

            // El fondo: la ilustracion entera, en penumbra. Lo que no es una sala (pasillos, la calle) se queda ahi.
            var fondo = raiz.gameObject.AddComponent<RawImage>();
            fondo.texture = imagen;
            fondo.raycastTarget = false;
            var penumbra = ui.Nodo(raiz, "Penumbra");
            UiKit.Rellenar(penumbra);
            var imgPenumbra = penumbra.gameObject.AddComponent<Image>();
            imgPenumbra.color = NexusTheme.Alfa(_tema.bg950, 0.55f);
            imgPenumbra.raycastTarget = false;
            ui.Borde(raiz, NexusTheme.Alfa(_tema.cyan, 0.35f), 6, 1);

            foreach (var zona in mapa.Zonas) {
                if (zona == null || !zona.TieneArea) continue;
                var s = Seccion.Crear(this, zona, imagen);
                _secciones.Add(s);
                if (zona.Id == datos.ZonaActual) _actual = s;
                datos.Registrar?.Invoke("mapa." + zona.Id, s);
            }

            // La linea de viaje, encima de todo: puntos que avanzan de donde estas a donde miras.
            _ruta = ui.Nodo(raiz, "Ruta");
            UiKit.Rellenar(_ruta);
            for (var i = 0; i < 16; i++) {
                var p = ui.Nodo(_ruta, "Paso");
                p.anchorMin = p.anchorMax = Vector2.zero;
                p.sizeDelta = new Vector2(7, 7);
                var img = p.gameObject.AddComponent<Image>();
                img.sprite = ui.SpriteCircular();
                img.color = _tema.cyan;
                img.raycastTarget = false;
                _puntosDeRuta.Add(img);
            }
            _placaDeCoste = ui.PanelColumna(_ruta, "Coste", 6, 0, NexusTheme.Alfa(_tema.bg950, 0.9f));
            _placaDeCoste.anchorMin = _placaDeCoste.anchorMax = Vector2.zero;
            _placaDeCoste.pivot = new Vector2(0.5f, 0.5f);
            var ajusteCoste = _placaDeCoste.gameObject.AddComponent<ContentSizeFitter>();
            ajusteCoste.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            ajusteCoste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ui.Borde(_placaDeCoste, _tema.cyan, 4, 1);
            _textoDeCoste = ui.Texto(_placaDeCoste, "", EstiloTexto.Mono, _tema.cyan, TextAlignmentOptions.Center);
            _textoDeCoste.textWrappingMode = TextWrappingModes.NoWrap;
            _ruta.gameObject.SetActive(false);

            // Los coleccionables de aqui: destellos dentro de la sala actual (crecen y se mueven con ella).
            if (_actual != null) {
                var rnd = new System.Random(datos.ZonaActual.GetHashCode());
                foreach (var d in datos.Destellos) Destello.Crear(this, _actual, d.Key, d.Value, rnd);
                foreach (var c in datos.PorAparecer) Parpadeo.Crear(this, _actual, c, rnd);
            }
        }

        // ==================================================================== estado compartido por las salas

        internal UiKit Ui { get { return _ui; } }
        internal NexusTheme Tema { get { return _tema; } }
        internal DatosDelPlano Datos { get { return _datos; } }
        internal Seccion Encima { get { return _encima; } }
        internal Seccion Viajando { get { return _viajando; } }
        internal float ProgresoDelViaje { get { return Mathf.Clamp01(_tiempoDeViaje / DuracionDelViaje); } }
        private const float DuracionDelViaje = 0.28f;

        internal void Entrar(Seccion s) {
            if (_viajando != null || _encima == s) return;
            _encima = s;
            s.transform.SetAsLastSibling();
            _ruta.SetAsLastSibling();
            _datos.AlEnfocar?.Invoke(s.Zona.Id);
        }

        internal void Salir(Seccion s) {
            if (_encima != s || _viajando != null) return;
            _encima = null;
            _datos.AlEnfocar?.Invoke(null);
        }

        internal void Pulsar(Seccion s) {
            if (_viajando != null) return;
            var id = s.Zona.Id;
            var puede = _datos.PuedeMoverse && id != _datos.ZonaActual && (_datos.Abierta == null || _datos.Abierta(id));
            if (!puede) { s.Sacudir(); return; }
            _viajando = s;
            _tiempoDeViaje = 0;
            _ruta.gameObject.SetActive(false);
        }

        private void Update() {
            var dt = Time.unscaledDeltaTime;
            if (_viajando != null) {
                _tiempoDeViaje += dt;
                if (_tiempoDeViaje >= DuracionDelViaje) {
                    var id = _viajando.Zona.Id;
                    var accion = _datos.AlViajar;
                    _viajando = null;
                    _encima = null;
                    accion?.Invoke(id);   // normalmente repinta el panel y este plano se destruye
                }
                return;
            }
            PintarRuta();
        }

        /// <summary>La linea de luz de la sala actual a la que se mira, con su coste en medio.</summary>
        private void PintarRuta() {
            var mostrar = _encima != null && _actual != null && _encima != _actual &&
                          (_datos.Abierta == null || _datos.Abierta(_encima.Zona.Id));
            if (_ruta.gameObject.activeSelf != mostrar) _ruta.gameObject.SetActive(mostrar);
            if (!mostrar) return;
            var tam = _raiz.rect.size;
            var a = Vector2.Scale(_actual.CentroNormalizado, tam);
            var b = Vector2.Scale(_encima.CentroNormalizado, tam);
            var arco = Mathf.Min(60f, (b - a).magnitude * 0.18f);
            var n = _puntosDeRuta.Count;
            var t = Time.unscaledTime * 1.6f;
            for (var i = 0; i < n; i++) {
                var f = (i + 0.5f) / n;
                // Un arco suave hacia arriba: se lee como un viaje, no como una regla.
                var pos = Vector2.Lerp(a, b, f) + Vector2.up * Mathf.Sin(f * Mathf.PI) * arco;
                var rt = _puntosDeRuta[i].rectTransform;
                rt.anchoredPosition = pos;
                var brillo = Mathf.Pow(1 - Mathf.Repeat(t - f, 1f), 3);
                _puntosDeRuta[i].color = NexusTheme.Alfa(_tema.cyan, 0.25f + 0.75f * brillo);
                rt.localScale = Vector3.one * (0.8f + 0.6f * brillo);
            }
            var coste = _datos.Coste != null ? _datos.Coste(_encima.Zona.Id) : null;
            var hayCoste = !string.IsNullOrEmpty(coste);
            if (_placaDeCoste.gameObject.activeSelf != hayCoste) _placaDeCoste.gameObject.SetActive(hayCoste);
            if (!hayCoste) return;
            _textoDeCoste.text = coste;
            _placaDeCoste.anchoredPosition = Vector2.Lerp(a, b, 0.5f) + Vector2.up * (arco + 22);
        }

        /// <summary>Donde esta el raton, en coordenadas locales de 'rt'. False si no hay raton.</summary>
        internal static bool Raton(RectTransform rt, out Vector2 local) {
            local = Vector2.zero;
            var raton = Mouse.current;
            if (raton == null) return false;
            var lienzo = rt.GetComponentInParent<Canvas>();
            var raizDelLienzo = lienzo != null ? lienzo.rootCanvas : null;
            var camara = raizDelLienzo != null && raizDelLienzo.renderMode != RenderMode.ScreenSpaceOverlay ? raizDelLienzo.worldCamera : null;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, raton.position.ReadValue(), camara, out local);
        }

        private static Sprite _brillo;

        /// <summary>Un punto de luz difuso (gaussiano), generado en memoria: el cuerpo de los destellos.</summary>
        internal static Sprite Brillo() {
            if (_brillo != null) return _brillo;
            const int lado = 64;
            var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false) { name = "brillo", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[lado * lado];
            var c = lado / 2f;
            for (var y = 0; y < lado; y++)
                for (var x = 0; x < lado; x++) {
                    float dx = (x + 0.5f - c) / c, dy = (y + 0.5f - c) / c;
                    var a = Mathf.Exp(-(dx * dx + dy * dy) * 4.5f);
                    px[y * lado + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _brillo = Sprite.Create(tex, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), 100);
            return _brillo;
        }

        // ==================================================================== una sala

        /// <summary>
        /// Una zona del nivel encima de su sala del dibujo: su recorte de la ilustracion a plena luz, su nombre y lo que
        /// cuesta ir. Es un Selectable (Button) para que tambien se recorra con Tab y Enter.
        /// </summary>
        internal sealed class Seccion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler {
            public ZonaDeNivel Zona { get; private set; }
            public Vector2 CentroNormalizado { get; private set; }

            private PlanoIlustrado _plano;
            private RectTransform _rt, _placa;
            private Image _velo, _halo;
            private RectTransform _anilloAqui;
            private Image _imgAnilloAqui;
            private bool _abierta, _aqui;
            private float _foco;       // 0..1, suavizado: cuanto se esta mirando esta sala
            private float _sacudida;

            public static Seccion Crear(PlanoIlustrado plano, ZonaDeNivel zona, Texture2D imagen) {
                var ui = plano.Ui;
                var tema = plano.Tema;
                var rt = ui.Nodo(plano.Raiz, "Sala " + zona.Id);
                float x = zona.Area[0], y = zona.Area[1], w = zona.Area[2], h = zona.Area[3];
                rt.anchorMin = new Vector2(x, 1 - (y + h));
                rt.anchorMax = new Vector2(x + w, 1 - y);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);

                var s = rt.gameObject.AddComponent<Seccion>();
                s._plano = plano;
                s._rt = rt;
                s.Zona = zona;
                s.CentroNormalizado = new Vector2(x + w / 2, 1 - (y + h / 2));
                s._aqui = zona.Id == plano.Datos.ZonaActual;
                s._abierta = plano.Datos.Abierta == null || plano.Datos.Abierta(zona.Id);

                // El recorte de la ilustracion, a plena luz: es lo que «se enciende» sobre la penumbra.
                var recorte = rt.gameObject.AddComponent<RawImage>();
                recorte.texture = imagen;
                recorte.uvRect = new Rect(x, 1 - (y + h), w, h);
                recorte.raycastTarget = true;

                s._halo = ui.Halo(rt, NexusTheme.Alfa(tema.cyan, 0f), 4);
                ui.Borde(rt, NexusTheme.Alfa(s._aqui ? ColorDelJugador : tema.cyan, s._aqui ? 0.8f : 0.3f), 4, s._aqui ? 2 : 1);

                // La niebla: espesa en las cerradas, y la que cae sobre las demas cuando se mira otra.
                var velo = ui.Nodo(rt, "Niebla");
                UiKit.Rellenar(velo);
                s._velo = velo.gameObject.AddComponent<Image>();
                s._velo.color = NexusTheme.Alfa(tema.bg950, s._abierta ? 0f : 0.62f);
                s._velo.raycastTarget = false;

                // La placa con el nombre, en la esquina de arriba que mira al centro del plano (las salas de la derecha
                // la llevan a la derecha): asi, al crecer, no se sale del dibujo.
                var aLaDerecha = x + w / 2 > 0.6f;
                var esquina = new Vector2(aLaDerecha ? 1 : 0, 1);
                s._placa = ui.PanelColumna(rt, "Placa", 8, 0, NexusTheme.Alfa(tema.bg950, 0.72f));
                s._placa.anchorMin = s._placa.anchorMax = s._placa.pivot = esquina;
                s._placa.anchoredPosition = new Vector2(aLaDerecha ? -12 : 12, -10);
                var ajuste = s._placa.gameObject.AddComponent<ContentSizeFitter>();
                ajuste.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var nombre = ui.Texto(s._placa, zona.Nombre, EstiloTexto.Encabezado, s._abierta ? tema.ink : tema.inkMuted,
                                      aLaDerecha ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft);
                nombre.textWrappingMode = TextWrappingModes.NoWrap;
                // En una sala estrecha el nombre parte en dos lineas en vez de salirse (y aun le queda sitio para crecer).
                var disponible = w * plano.Raiz.sizeDelta.x / 1.35f - 40;
                if (disponible > 60 && nombre.GetPreferredValues(zona.Nombre).x > disponible) {
                    nombre.textWrappingMode = TextWrappingModes.Normal;
                    UiKit.Tamano(nombre, ancho: disponible);
                }
                var detalle = plano.Datos.Detalle != null ? plano.Datos.Detalle(zona) : "";
                if (!string.IsNullOrEmpty(detalle)) {
                    var t = ui.Texto(s._placa, detalle, EstiloTexto.Leyenda,
                                     s._aqui ? ColorDelJugador : s._abierta ? tema.cyan : tema.inkFaint,
                                     aLaDerecha ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft);
                    t.textWrappingMode = TextWrappingModes.NoWrap;
                }

                // «Estas aqui»: una luz que late, en la esquina de arriba contraria a la placa.
                if (s._aqui) {
                    var marca = ui.Nodo(rt, "Estas aqui");
                    marca.anchorMin = marca.anchorMax = new Vector2(aLaDerecha ? 0 : 1, 1);
                    marca.anchoredPosition = new Vector2(aLaDerecha ? 22 : -22, -22);
                    s._anilloAqui = Punto(ui, marca, 20, ColorDelJugador);
                    s._imgAnilloAqui = s._anilloAqui.GetComponent<Image>();
                    Punto(ui, marca, 16, Color.white);
                    Punto(ui, marca, 11, ColorDelJugador);
                }

                var boton = rt.gameObject.AddComponent<Button>();
                boton.transition = Selectable.Transition.None;
                boton.targetGraphic = recorte;
                boton.onClick.AddListener(() => plano.Pulsar(s));
                return s;
            }

            private static RectTransform Punto(UiKit ui, Transform padre, float lado, Color color) {
                var p = ui.Nodo(padre, "Punto");
                p.sizeDelta = new Vector2(lado, lado);
                var img = p.gameObject.AddComponent<Image>();
                img.sprite = ui.SpriteCircular();
                img.color = color;
                img.raycastTarget = false;
                return p;
            }

            public void OnPointerEnter(PointerEventData e) { _plano.Entrar(this); }
            public void OnPointerExit(PointerEventData e) { _plano.Salir(this); }
            public void OnSelect(BaseEventData e) { _plano.Entrar(this); }
            public void OnDeselect(BaseEventData e) { _plano.Salir(this); }

            public void Sacudir() { _sacudida = 0.32f; }

            /// <summary>El interior de la sala en sus coordenadas locales, con un margen (para que los destellos no se salgan).</summary>
            public Rect Interior(float margen) {
                var r = _rt.rect;
                return new Rect(r.xMin + margen, r.yMin + margen, r.width - 2 * margen, r.height - 2 * margen);
            }

            private void Update() {
                var dt = Time.unscaledDeltaTime;
                var tema = _plano.Tema;
                var viajando = _plano.Viajando;
                var encima = _plano.Encima;

                // Ease-out de unos 180 ms hacia el foco.
                var objetivo = viajando == this || (viajando == null && encima == this) ? 1f : 0f;
                _foco = Mathf.MoveTowards(_foco, objetivo, dt / 0.18f);
                var e = 1 - (1 - _foco) * (1 - _foco);

                var escala = Mathf.Lerp(1f, 1.07f, e);
                if (viajando == this) escala = Mathf.Lerp(1.07f, 1.16f, _plano.ProgresoDelViaje);
                _rt.localScale = Vector3.one * escala;
                _placa.localScale = Vector3.one * Mathf.Lerp(1f, 1.35f, e);
                _halo.color = NexusTheme.Alfa(tema.cyan, 0.85f * e);

                // Niebla: espesa si esta cerrada; media si se mira otra sala; casi total para las demas durante el viaje.
                var niebla = _abierta ? 0f : 0.62f;
                if (viajando != null && viajando != this) niebla = Mathf.Max(niebla, 0.7f * _plano.ProgresoDelViaje);
                else if (viajando == null && encima != null && encima != this) niebla = Mathf.Max(niebla, 0.45f);
                _velo.color = NexusTheme.Alfa(tema.bg950, Mathf.MoveTowards(_velo.color.a, niebla, dt / 0.18f));

                if (_sacudida > 0) {
                    _sacudida -= dt;
                    var fuerza = Mathf.Max(0, _sacudida) / 0.32f;
                    _rt.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 70f) * 7f * fuerza, 0);
                } else if (_rt.anchoredPosition != Vector2.zero) {
                    _rt.anchoredPosition = Vector2.zero;
                }

                if (_anilloAqui != null) {
                    var t = (Time.unscaledTime * 1.1f) % 1f;
                    _anilloAqui.localScale = Vector3.one * Mathf.Lerp(1f, 2.5f, t);
                    _imgAnilloAqui.color = NexusTheme.Alfa(ColorDelJugador, 0.8f * (1 - t));
                }
            }
        }

        // ==================================================================== un destello (un coleccionable)

        /// <summary>
        /// Un coleccionable que ya aparecio en la sala donde estas: una luz que flota, huye del cursor unas veces
        /// (segun su dificultad, HuidaDelDestello) y despues se cansa y se deja atrapar con un clic.
        /// </summary>
        internal sealed class Destello : MonoBehaviour, IPointerClickHandler {
            private PlanoIlustrado _plano;
            private Seccion _sala;
            private string _id;
            private HuidaDelDestello _huida;
            private RectTransform _rt, _halo, _nucleo;
            private Image _imgHalo, _imgNucleo;
            private readonly List<RectTransform> _estela = new List<RectTransform>();
            private Vector2 _ancla, _pos, _azar;
            private float _semilla, _rapidez = 6f, _atrapado = -1f, _susto;
            private bool _colocado;

            public static void Crear(PlanoIlustrado plano, Seccion sala, string id, string dificultad, System.Random rnd) {
                var ui = plano.Ui;
                // La estela va detras de la luz: se crea antes, como hermana.
                var estela = new List<RectTransform>();
                for (var i = 0; i < 4; i++) {
                    var e = ui.Nodo(sala.transform, "Estela");
                    e.sizeDelta = new Vector2(14 - i * 2.5f, 14 - i * 2.5f);
                    var img = e.gameObject.AddComponent<Image>();
                    img.sprite = Brillo();
                    img.color = NexusTheme.Alfa(ColorDelDestello, 0.35f - i * 0.07f);
                    img.raycastTarget = false;
                    estela.Add(e);
                }

                var rt = ui.Nodo(sala.transform, "Destello " + id);
                rt.sizeDelta = new Vector2(64, 64);
                var d = rt.gameObject.AddComponent<Destello>();
                d._plano = plano;
                d._sala = sala;
                d._id = id;
                d._rt = rt;
                d._estela.AddRange(estela);
                d._huida = new HuidaDelDestello(dificultad);
                d._semilla = (float)rnd.NextDouble() * 100f;
                d._azar = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());

                // La zona de clic: transparente y mas grande que la luz (atraparlo no es un examen de punteria).
                var hit = rt.gameObject.AddComponent<Image>();
                hit.color = new Color(0, 0, 0, 0);
                hit.raycastTarget = true;

                d._halo = ui.Nodo(rt, "Halo");
                d._halo.sizeDelta = new Vector2(64, 64);
                d._imgHalo = d._halo.gameObject.AddComponent<Image>();
                d._imgHalo.sprite = Brillo();
                d._imgHalo.color = NexusTheme.Alfa(ColorDelDestello, 0.75f);
                d._imgHalo.raycastTarget = false;
                d._nucleo = ui.Nodo(rt, "Nucleo");
                d._nucleo.sizeDelta = new Vector2(14, 14);
                d._imgNucleo = d._nucleo.gameObject.AddComponent<Image>();
                d._imgNucleo.sprite = ui.SpriteCircular();
                d._imgNucleo.color = Color.white;
                d._imgNucleo.raycastTarget = false;
            }

            private bool Activo { get { return _plano.Datos.PuedeMoverse && _plano.Viajando == null; } }

            private void Update() {
                var dt = Time.unscaledDeltaTime;
                var r = _sala.Interior(26);
                if (r.width <= 0 || r.height <= 0) return;
                if (!_colocado) {
                    // Con la sala ya maquetada: un sitio al azar, lejos de la placa del nombre (arriba a la izquierda).
                    _colocado = true;
                    _ancla = _pos = new Vector2(Mathf.Lerp(r.xMin + r.width * 0.3f, r.xMax, _azar.x),
                                                Mathf.Lerp(r.yMin, r.yMax - r.height * 0.3f, _azar.y));
                    foreach (var e in _estela) e.anchoredPosition = _pos;
                }

                if (_atrapado >= 0) { Atrapando(dt); return; }

                // Asustarse: si el raton se acerca y aun le quedan escapes, salta lejos (dentro de la sala).
                Vector2 raton;
                if (Activo && Raton((RectTransform)_sala.transform, out raton)) {
                    float nx, ny;
                    if (_huida.Asustar(_pos.x, _pos.y, raton.x, raton.y, r.xMin, r.yMin, r.xMax, r.yMax, out nx, out ny)) {
                        _ancla = new Vector2(nx, ny);
                        _rapidez = 16f;
                        _susto = 0.25f;
                    }
                }

                // Flotar: un vaiven de ruido alrededor del ancla (mas lento si esta cansado), y llegar con suavidad.
                var t = Time.unscaledTime * 0.45f * _huida.Ritmo + _semilla;
                var vaiven = new Vector2(Mathf.PerlinNoise(t, _semilla) - 0.5f, Mathf.PerlinNoise(_semilla, t) - 0.5f) * 36f;
                var objetivo = new Vector2(Mathf.Clamp(_ancla.x + vaiven.x, r.xMin, r.xMax), Mathf.Clamp(_ancla.y + vaiven.y, r.yMin, r.yMax));
                _pos = Vector2.Lerp(_pos, objetivo, 1 - Mathf.Exp(-_rapidez * dt));
                _rapidez = Mathf.MoveTowards(_rapidez, 6f, dt * 20f);
                _rt.anchoredPosition = _pos;

                // La estela sigue con retraso.
                var previo = _pos;
                for (var i = 0; i < _estela.Count; i++) {
                    var e = _estela[i];
                    e.anchoredPosition = Vector2.Lerp(e.anchoredPosition, previo, 1 - Mathf.Exp(-(14f - i * 2.5f) * dt));
                    previo = e.anchoredPosition;
                }

                // Latido: cansado late mas rapido, mas grande y mas brillante («ahora si»); inactivo, apagado.
                var latido = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (_huida.Cansado ? 5f : 2.6f) + _semilla);
                var tam = (_huida.Cansado ? 1.25f : 1f) * Mathf.Lerp(0.85f, 1.1f, latido) * (1 + _susto * 1.2f);
                _susto = Mathf.Max(0, _susto - dt);
                _halo.localScale = Vector3.one * tam;
                var alfa = Activo ? (_huida.Cansado ? 0.95f : 0.7f) : 0.3f;
                _imgHalo.color = NexusTheme.Alfa(ColorDelDestello, alfa * Mathf.Lerp(0.75f, 1f, latido));
                _imgNucleo.color = NexusTheme.Alfa(Color.white, Activo ? 1f : 0.45f);
            }

            public void OnPointerClick(PointerEventData e) {
                if (!Activo || _atrapado >= 0) return;
                _atrapado = 0;
            }

            /// <summary>Atrapado: la luz estalla en un anillo y se apaga; entonces se recoge (el diario, la escena).</summary>
            private void Atrapando(float dt) {
                _atrapado += dt;
                var f = Mathf.Clamp01(_atrapado / 0.38f);
                _halo.localScale = Vector3.one * Mathf.Lerp(1.2f, 3.4f, f);
                _imgHalo.color = NexusTheme.Alfa(ColorDelDestello, 0.95f * (1 - f));
                _nucleo.localScale = Vector3.one * Mathf.Lerp(1.4f, 0f, f);
                foreach (var s in _estela) if (s.gameObject.activeSelf) s.gameObject.SetActive(false);
                if (f < 1) return;
                var accion = _plano.Datos.AlAtrapar;
                var id = _id;
                gameObject.SetActive(false);
                accion?.Invoke(id);
            }
        }

        /// <summary>Un coleccionable que todavia no aparece: un parpadeo debil del color de su dificultad. No se toca.</summary>
        internal sealed class Parpadeo : MonoBehaviour {
            private Image _img;
            private Color _color;
            private float _semilla;
            private Vector2 _azar;
            private Seccion _sala;
            private bool _colocado;

            public static void Crear(PlanoIlustrado plano, Seccion sala, Color color, System.Random rnd) {
                var rt = plano.Ui.Nodo(sala.transform, "Por aparecer");
                rt.sizeDelta = new Vector2(22, 22);
                var p = rt.gameObject.AddComponent<Parpadeo>();
                p._img = rt.gameObject.AddComponent<Image>();
                p._img.sprite = Brillo();
                p._img.raycastTarget = false;
                p._img.color = new Color(0, 0, 0, 0);
                p._color = color;
                p._sala = sala;
                p._semilla = (float)rnd.NextDouble() * 50f;
                p._azar = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());
            }

            private void Update() {
                if (!_colocado) {
                    var r = _sala.Interior(30);
                    if (r.width <= 0 || r.height <= 0) return;
                    _colocado = true;
                    ((RectTransform)transform).anchoredPosition = new Vector2(Mathf.Lerp(r.xMin, r.xMax, _azar.x),
                                                                              Mathf.Lerp(r.yMin, r.yMax, _azar.y));
                }
                var n = Mathf.PerlinNoise(Time.unscaledTime * 1.7f, _semilla);
                _img.color = NexusTheme.Alfa(_color, Mathf.Clamp01((n - 0.45f) * 1.6f) * 0.55f);
            }
        }
    }
}
