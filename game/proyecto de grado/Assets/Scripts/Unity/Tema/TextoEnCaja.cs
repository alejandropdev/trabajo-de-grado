using TMPro;
using UnityEngine;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// Un texto que vive en una CAJA de tamaño fijo (anclada dentro de su padre) y que nunca se sale de ella
    /// (2026-11-08): se encoge hasta la mitad de su tamaño y, si ni asi cabe, termina en puntos suspensivos. Sin el
    /// AltoDeTexto del kit, que haria crecer la caja en vez de encoger el texto.
    /// </summary>
    public static class TextoEnCaja {
        public static TextMeshProUGUI Crear(UiKit ui, Transform padre, string texto, EstiloTexto estilo, Color color,
                                           TextAlignmentOptions alineacion, float tamMaximo,
                                           Vector2 anclaMin, Vector2 anclaMax, float margen = 0) {
            var t = ui.Texto(padre, texto, estilo, color, alineacion);
            Ajustar(t, tamMaximo);
            var rt = t.rectTransform;
            rt.anchorMin = anclaMin; rt.anchorMax = anclaMax;
            rt.offsetMin = new Vector2(margen, margen); rt.offsetMax = new Vector2(-margen, -margen);
            return t;
        }

        /// <summary>Convierte un texto ya creado en uno que cabe en su caja.</summary>
        public static void Ajustar(TMP_Text t, float tamMaximo) {
            var alto = t.GetComponent<AltoDeTexto>();
            if (alto != null) Object.DestroyImmediate(alto);
            t.enableAutoSizing = true;
            t.fontSizeMax = tamMaximo;
            t.fontSizeMin = Mathf.Max(6, tamMaximo * 0.5f);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Ellipsis;
        }
    }
}
