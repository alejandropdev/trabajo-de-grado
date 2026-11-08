using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// Las ventanas de los dialogos (2026-11-08): cristal neon en lugar del marco de metal con remaches. La ventana de
    /// metal (UiKit.Ventana) queda solo para los minijuegos.
    /// </summary>
    public sealed partial class UiKit {
        /// <summary>
        /// Una ventana de dialogo: vidrio oscuro con filo y resplandor del color de acento (cian por defecto), las cuatro
        /// esquinas en L luminosas, una franja de luz arriba y una entrada animada (escala y fundido). Mismo contrato que
        /// Ventana: devuelve una columna de lo que se le meta, con su relleno, lista para un ContentSizeFitter.
        /// </summary>
        public RectTransform Dialogo(Transform padre, string nombre, Color? acento = null, float? relleno = null) {
            var color = acento ?? Tema.cyan;
            var rt = Nodo(padre, nombre);
            var fondo = rt.gameObject.AddComponent<Image>();
            Fondo(fondo, NexusTheme.Alfa(Tema.bg950, 0.94f), NexusTheme.RadioLg);   // casi opaco: el texto se lee sobre cualquier cosa
            var grupo = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            Configurar(grupo, Tema.espacio, relleno ?? Tema.Px(20) + Tema.Espacio(3), TextAnchor.UpperLeft);

            Halo(rt, NexusTheme.Alfa(color, 0.28f), NexusTheme.RadioLg);
            Borde(rt, NexusTheme.Alfa(color, 0.85f), NexusTheme.RadioLg, 2);
            Esquinas(rt, color);

            // La franja de luz de arriba (como el filo de un panel de control).
            var franja = Decoracion(rt, "Franja", 0);
            franja.anchorMin = new Vector2(0.12f, 1); franja.anchorMax = new Vector2(0.88f, 1);
            franja.offsetMin = new Vector2(0, -Tema.Px(3)); franja.offsetMax = Vector2.zero;
            var fi = franja.gameObject.AddComponent<Image>();
            fi.color = NexusTheme.Alfa(color, 0.95f);
            fi.raycastTarget = false;

            rt.gameObject.AddComponent<EntradaDeDialogo>();
            return rt;
        }

        /// <summary>
        /// El lienzo de papel de las pantallas que no son un minijuego (el despliegue): la hoja de papel con un filo de
        /// luz y su resplandor, sin el marco de metal. El papel y su tinta oscura no cambian.
        /// </summary>
        public RectTransform LienzoDeCristal(Transform padre, string nombre, float? relleno = null) {
            var rt = Nodo(padre, nombre);
            var fondo = rt.gameObject.AddComponent<Image>();
            if (!Material(fondo, MaterialesNexus.Papel, 12)) Fondo(fondo, Tema.paperBg, NexusTheme.RadioMd);
            var grupo = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            Configurar(grupo, Tema.espacio, relleno ?? Tema.Espacio(4), TextAnchor.UpperLeft);
            Halo(rt, NexusTheme.Alfa(Tema.cyan, 0.30f), NexusTheme.RadioMd);
            Borde(rt, NexusTheme.Alfa(Tema.cyan, 0.9f), NexusTheme.RadioMd, 2.5f);
            return rt;
        }
    }

    /// <summary>Un dialogo que entra: escala de 0,94 a 1 y se enciende en unos 0,18 s (con el reloj sin escalar).</summary>
    public sealed class EntradaDeDialogo : MonoBehaviour {
        private CanvasGroup _grupo;
        private float _t;

        private void Awake() {
            _grupo = gameObject.GetComponent<CanvasGroup>();
            if (_grupo == null) _grupo = gameObject.AddComponent<CanvasGroup>();
            _grupo.alpha = 0;
            transform.localScale = new Vector3(0.94f, 0.94f, 1);
        }

        private void Update() {
            _t += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            var x = Mathf.Clamp01(_t / 0.18f);
            var e = 1 - Mathf.Pow(1 - x, 3);
            _grupo.alpha = e;
            transform.localScale = new Vector3(0.94f + 0.06f * e, 0.94f + 0.06f * e, 1);
            if (x >= 1) { transform.localScale = Vector3.one; enabled = false; }
        }
    }
}
