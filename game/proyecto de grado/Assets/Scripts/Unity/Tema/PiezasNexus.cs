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

        private Selectable _selectable;
        private CanvasGroup _grupo;
        private bool _encima, _pulsado, _elegido;

        public bool Elegido {
            get { return _elegido; }
            set { _elegido = value; Aplicar(); }
        }

        private void Awake() {
            _selectable = GetComponent<Selectable>();
            _grupo = GetComponent<CanvasGroup>();
            if (_grupo == null) _grupo = gameObject.AddComponent<CanvasGroup>();
        }

        private void OnEnable() { _encima = _pulsado = false; Aplicar(); }

        private void LateUpdate() {
            var activo = _selectable == null || _selectable.IsInteractable();
            var alfa = activo ? 1f : 0.45f;
            if (_grupo != null && !Mathf.Approximately(_grupo.alpha, alfa)) _grupo.alpha = alfa;
        }

        public void OnPointerEnter(PointerEventData e) { _encima = true; Aplicar(); }
        public void OnPointerExit(PointerEventData e) { _encima = _pulsado = false; Aplicar(); }
        public void OnPointerDown(PointerEventData e) { _pulsado = true; Aplicar(); }
        public void OnPointerUp(PointerEventData e) { _pulsado = false; Aplicar(); }

        public void Aplicar() {
            var interactivo = _selectable == null || _selectable.IsInteractable();
            var e = _elegido && TieneElegido ? ElegidoEstado : _encima && interactivo ? Encima : Normal;
            var fondo = _pulsado && interactivo ? Color.Lerp(e.Fondo, Color.black, 0.18f) : e.Fondo;
            if (Fondo != null) Fondo.color = fondo;
            if (Borde != null) Borde.color = e.Borde;
            if (Etiqueta != null) Etiqueta.color = e.Texto;
            if (Halo != null && Halo.gameObject.activeSelf != e.Halo) Halo.gameObject.SetActive(e.Halo);
        }
    }
}
