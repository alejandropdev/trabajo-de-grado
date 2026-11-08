using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// Las formas del design system, generadas en memoria y «9-slice»: una caja redondeada maciza, su borde de un
    /// trazo, y el resplandor (glow) que en el design system marca seleccion y foco. Igual que el circulo y el
    /// redondeado de UiKit, no hacen falta texturas en el proyecto. Se miden en unidades del lienzo (1 texel = 1
    /// unidad con 100 px por unidad), asi que un radio de 22 se ve de 22 a cualquier tamaño.
    /// </summary>
    public static class Formas {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>Caja redondeada maciza de radio 'radio'.</summary>
        public static Sprite Caja(float radio) {
            var r = Mathf.Max(1, Mathf.Round(radio));
            return Generar("caja" + r, r, 0, (d, _) => Mathf.Clamp01(0.5f - d));
        }

        /// <summary>Solo el contorno: una banda de 'grosor' por dentro del borde de la caja.</summary>
        public static Sprite Borde(float radio, float grosor) {
            var r = Mathf.Max(1, Mathf.Round(radio));
            var g = Mathf.Max(1, grosor);
            return Generar("borde" + r + "_" + g, r, 0, (d, _) => Mathf.Clamp01(0.5f - d) * Mathf.Clamp01(0.5f + d + g));
        }

        /// <summary>
        /// El resplandor de afuera: transparente dentro de la caja y difuminado hacia fuera a lo largo de 'extension'.
        /// La Image que lo lleva debe ser 'extension' mas grande que la pieza por cada lado (UiKit.Halo).
        /// </summary>
        public static Sprite Resplandor(float radio, float extension) {
            var r = Mathf.Max(1, Mathf.Round(radio));
            var e = Mathf.Max(2, Mathf.Round(extension));
            return Generar("halo" + r + "_" + e, r, e, (d, ext) => {
                if (d <= 0) return 0;
                var t = d / (ext * 0.5f);
                return 0.65f * Mathf.Exp(-t * t);
            });
        }

        private static Sprite Generar(string clave, float r, float ext, System.Func<float, float, float> alfa) {
            if (Cache.TryGetValue(clave, out var hecho) && hecho != null) return hecho;
            var lado = Mathf.CeilToInt(2 * (r + ext) + 4);
            var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false) { name = clave, wrapMode = TextureWrapMode.Clamp };
            var pixeles = new Color32[lado * lado];
            var c = lado / 2f;
            var medio = c - ext - 0.5f;   // media caja interior (sin el resplandor), con medio pixel de aire
            for (var y = 0; y < lado; y++)
                for (var x = 0; x < lado; x++) {
                    var qx = Mathf.Abs(x + 0.5f - c) - (medio - r);
                    var qy = Mathf.Abs(y + 0.5f - c) - (medio - r);
                    var fuera = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
                    var d = fuera + Mathf.Min(Mathf.Max(qx, qy), 0) - r;   // distancia con signo a la caja redondeada
                    pixeles[y * lado + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alfa(d, ext)) * 255));
                }
            tex.SetPixels32(pixeles);
            tex.Apply(false, true);
            var b = r + ext + 1;
            var sprite = Sprite.Create(tex, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), 100, 0,
                                       SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            sprite.name = clave;
            Cache[clave] = sprite;
            return sprite;
        }
    }

    /// <summary>
    /// El aspecto vivo de un boton del design system: en reposo, al pasar el raton (o con foco de teclado), pulsado,
    /// elegido y deshabilitado. uGUI solo sabe teñir UNA imagen; el design system cambia a la vez el fondo, el borde,
    /// el color de la etiqueta y el resplandor, asi que lo hace este componente. UiKit lo crea; las pantallas solo le
    /// cambian Elegido (via UiKit.Resaltar).
    ///
    /// ANIMADO (2026-11-07, los botones neon de UiKit.Boton): los colores se funden hacia el estado en ~0,1 s; el
    /// resplandor solo cambia de intensidad (BrilloEnReposo / BrilloEncendido); lo visible (el Cuerpo) crece unos pixeles
    /// al pasar el raton, se hunde al pulsar y rebota al soltar (un muelle); al hacer click el borde destella y el
    /// resplandor se expande y se apaga; al entrar el raton, una franja de luz lo cruza (Barrido). Todo con
    /// unscaledDeltaTime: tambien anima con el reloj del juego parado. Sin Animado, el comportamiento de siempre.
    ///
    /// ★ Se escala el CUERPO, nunca el boton: el boton es la zona de clic y tiene que medir siempre lo mismo (si se
    /// encogiera al pulsar, el cursor quedaria fuera, saltaria OnPointerExit y el click se perderia), hay botones con el
    /// pivote en una esquina y otros que ya laten por su cuenta (LatidoDeResumen escala el boton).
    /// </summary>
    public sealed class VisualDeBoton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
                                        IPointerDownHandler, IPointerUpHandler {
        public struct Estado {
            public Color Fondo, Borde, Texto;
            public bool Halo;
        }

        public Image Fondo, Borde, Halo;
        public TMP_Text Etiqueta;
        public Estado Normal, Encima, ElegidoEstado;
        public bool TieneElegido = true;

        // ---- el boton neon
        public bool Animado;
        /// <summary>Lo que se escala (fondo, borde, halo, texto): un hijo estirado del boton, con el pivote en el centro.</summary>
        public RectTransform Cuerpo;
        /// <summary>Intensidad del resplandor cuando el estado no lo pide (Estado.Halo = false) y cuando si.</summary>
        public float BrilloEnReposo, BrilloEncendido = 0.55f;
        /// <summary>La franja de luz del hover; su padre es el contenedor recortado, que solo existe mientras pasa.</summary>
        public RectTransform Barrido;
        public Image ImagenBarrido;

        /// <summary>Lo que crece el cuerpo al pasar el raton, en pixeles (no en %: un boton ancho creceria demasiado).</summary>
        private const float CrecerEncima = 5f, HundirAlPulsar = 0.95f;
        private const float DuracionDestello = 0.35f, DuracionBarrido = 0.5f, AlfaBarrido = 0.22f;

        private Selectable _selectable;
        private CanvasGroup _grupo;
        private bool _encima, _pulsado, _elegido;

        // Lo que se ve ahora y hacia donde va (solo Animado).
        private bool _listo, _quieto;
        private Color _fondo, _borde, _texto, _fondoMeta, _bordeMeta, _textoMeta;
        private float _brillo, _brilloMeta, _escala = 1f, _velocidad, _destello, _barrido = -1f;

        public bool Elegido {
            get { return _elegido; }
            set { _elegido = value; Aplicar(); }
        }

        private void Awake() {
            _selectable = GetComponent<Selectable>();
            _grupo = GetComponent<CanvasGroup>();
            if (_grupo == null) _grupo = gameObject.AddComponent<CanvasGroup>();
        }

        private void OnEnable() {
            _encima = _pulsado = false;
            _velocidad = 0f;
            _destello = 0f;
            _barrido = -1f;
            _escala = 1f;
            if (Animado && Cuerpo != null) Cuerpo.localScale = Vector3.one;
            Aplicar();
        }

        private void LateUpdate() {
            var activo = _selectable == null || _selectable.IsInteractable();
            var alfa = activo ? 1f : 0.45f;
            if (_grupo != null && !Mathf.Approximately(_grupo.alpha, alfa)) _grupo.alpha = alfa;
        }

        private bool Interactivo { get { return _selectable == null || _selectable.IsInteractable(); } }

        public void OnPointerEnter(PointerEventData e) {
            _encima = true;
            if (Animado && Interactivo && Barrido != null) _barrido = 0f;
            Aplicar();
        }

        public void OnPointerExit(PointerEventData e) { _encima = _pulsado = false; Aplicar(); }

        public void OnPointerDown(PointerEventData e) { _pulsado = true; Aplicar(); }

        public void OnPointerUp(PointerEventData e) {
            var click = _pulsado && _encima;
            _pulsado = false;
            if (Animado && click && Interactivo) {
                _destello = 1f;
                _velocidad += 2.4f;   // el rebote: sale despedido hacia arriba desde el hundido
            }
            Aplicar();
        }

        public void Aplicar() {
            var interactivo = Interactivo;
            var e = _elegido && TieneElegido ? ElegidoEstado : _encima && interactivo ? Encima : Normal;
            var pulsado = _pulsado && interactivo;
            if (!Animado) {
                var fondo = pulsado ? Color.Lerp(e.Fondo, Color.black, 0.18f) : e.Fondo;
                if (Fondo != null) Fondo.color = fondo;
                if (Borde != null) Borde.color = e.Borde;
                if (Etiqueta != null) Etiqueta.color = e.Texto;
                if (Halo != null && Halo.gameObject.activeSelf != e.Halo) Halo.gameObject.SetActive(e.Halo);
                return;
            }
            _fondoMeta = pulsado ? Color.Lerp(e.Fondo, Color.black, 0.12f) : e.Fondo;
            _bordeMeta = e.Borde;
            _textoMeta = e.Texto;
            _brilloMeta = e.Halo ? BrilloEncendido : BrilloEnReposo;
            if (!_listo) {
                // Hasta el primer fotograma, sin fundido: el boton aparece ya con su aspecto aunque lo retoquen al crearlo.
                _fondo = _fondoMeta;
                _borde = _bordeMeta;
                _texto = _textoMeta;
                _brillo = _brilloMeta;
                Pintar();
            }
            _quieto = false;
        }

        private void Update() {
            if (!Animado) return;
            _listo = true;
            if (_quieto) return;
            var dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);   // un tiron de fotogramas no dispara el muelle
            var k = 1f - Mathf.Exp(-dt * 22f);
            _fondo = Color.Lerp(_fondo, _fondoMeta, k);
            _borde = Color.Lerp(_borde, _bordeMeta, k);
            _texto = Color.Lerp(_texto, _textoMeta, k);
            _brillo = Mathf.Lerp(_brillo, _brilloMeta, k);

            // La escala, con un muelle poco amortiguado: llega, se pasa un poco y vuelve.
            var meta = MetaDeEscala();
            _velocidad += ((meta - _escala) * 520f - _velocidad * 24f) * dt;
            _escala += _velocidad * dt;

            if (_destello > 0f) _destello = Mathf.Max(0f, _destello - dt / DuracionDestello);
            if (_barrido >= 0f) {
                _barrido += dt / DuracionBarrido;
                if (_barrido >= 1f) _barrido = -1f;
            }
            Pintar();

            // Quieto: colores en su sitio, escala parada, sin destello ni barrido. Hasta el proximo cambio, nada.
            _quieto = Mathf.Abs(_escala - meta) < 0.0005f && Mathf.Abs(_velocidad) < 0.01f && _destello <= 0f && _barrido < 0f &&
                      Cerca(_fondo, _fondoMeta) && Cerca(_borde, _bordeMeta) && Cerca(_texto, _textoMeta) &&
                      Mathf.Abs(_brillo - _brilloMeta) < 0.005f;
        }

        private float MetaDeEscala() {
            if (!Interactivo || Cuerpo == null) return 1f;
            if (_pulsado) return HundirAlPulsar;
            if (!_encima) return 1f;
            var ancho = Mathf.Max(40f, Cuerpo.rect.width);
            return Mathf.Min(1.06f, 1f + 2f * CrecerEncima / ancho);
        }

        private void Pintar() {
            if (Fondo != null) Fondo.color = _fondo;
            if (Borde != null) Borde.color = Color.Lerp(_borde, Color.white, 0.8f * _destello);
            if (Etiqueta != null) Etiqueta.color = _texto;
            if (Halo != null) {
                // Solo el alfa: el color (cian, rojo, el tono de Resaltar) lo decide quien lo puso. Apagado del todo, fuera:
                // un resplandor invisible tambien se dibuja.
                var c = Halo.color;
                c.a = Mathf.Max(_brillo, _destello);
                Halo.color = c;
                var visible = c.a > 0.003f;
                if (Halo.gameObject.activeSelf != visible) Halo.gameObject.SetActive(visible);
                // Al hacer click el resplandor se expande mientras se apaga.
                Halo.rectTransform.localScale = Vector3.one * (_destello > 0f ? 1f + 0.15f * (1f - _destello) : 1f);
            }
            if (Cuerpo != null) Cuerpo.localScale = Vector3.one * _escala;
            if (Barrido != null) {
                var activo = _barrido >= 0f;
                var contenedor = Barrido.parent;
                // El contenedor recortado (RectMask2D) solo existe mientras pasa la luz: una mascara por boton rompe el
                // agrupado de dibujo de toda la pantalla.
                if (contenedor != null && contenedor.gameObject.activeSelf != activo) contenedor.gameObject.SetActive(activo);
                if (activo && contenedor != null) {
                    var ancho = ((RectTransform)contenedor).rect.width;
                    var t = _barrido * _barrido * (3f - 2f * _barrido);
                    Barrido.anchoredPosition = new Vector2(Mathf.Lerp(-0.65f * ancho, 0.65f * ancho, t), 0f);
                    if (ImagenBarrido != null) ImagenBarrido.color = new Color(1f, 1f, 1f, AlfaBarrido * Mathf.Sin(Mathf.PI * _barrido));
                }
            }
        }

        private static bool Cerca(Color a, Color b) {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a) < 0.01f;
        }
    }

    /// <summary>
    /// El alto minimo de un texto es su alto real. TMP dice que su minimo es 0, y un VerticalLayoutGroup sin sitio
    /// encoge a sus hijos hacia el minimo: los textos se montaban unos sobre otros. Con esto, la columna crece (o su
    /// scroll se alarga) en vez de aplastar texto.
    /// </summary>
    public sealed class AltoDeTexto : UIBehaviour, ILayoutElement {
        private TMP_Text _texto;
        protected override void Awake() { _texto = GetComponent<TMP_Text>(); }
        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
        public float minWidth { get { return -1; } }
        public float preferredWidth { get { return -1; } }
        public float flexibleWidth { get { return -1; } }
        public float minHeight { get { return _texto != null && isActiveAndEnabled ? _texto.preferredHeight : -1; } }
        public float preferredHeight { get { return -1; } }
        public float flexibleHeight { get { return -1; } }
        public int layoutPriority { get { return 1; } }
    }
}
