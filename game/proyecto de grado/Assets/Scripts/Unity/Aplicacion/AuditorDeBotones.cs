using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// Regla de usabilidad de la ronda con usuarios (nov. 2026): ninguna pantalla muestra mas de siete botones a la vez.
    /// En el editor y en builds de desarrollo, un fotograma despues de cada cambio de pantalla cuenta los botones
    /// visibles y pulsables de la de arriba, y avisa en la consola si pasan de siete. No cuenta las zonas invisibles de
    /// los diagramas ni lo que vaya dentro de una lista marcada con ListaDeElementos (las partidas guardadas, las
    /// opciones de una pregunta): eso es contenido, no botones de la pantalla.
    /// </summary>
    public static class AuditorDeBotones {
        public const int Maximo = 7;

        public static void Vigilar(AppRoot app) {
            if (!Debug.isDebugBuild) return;
            app.Router.AlCambiar += pantalla => {
                if (pantalla != null) app.StartCoroutine(Contar(pantalla));
            };
        }

        private static IEnumerator Contar(Pantalla pantalla) {
            yield return null;   // la pantalla termina de construirse en este fotograma
            if (pantalla == null) yield break;
            var tipo = pantalla.GetType().Name;
            if (tipo == "PantallaDeDiagnostico" || tipo == "PantallaDelTema") yield break;
            var botones = pantalla.GetComponentsInChildren<Button>(false)
                .Where(b => b.interactable && b.GetComponentInParent<ListaDeElementos>() == null && !EsZonaInvisible(b))
                .ToList();
            if (botones.Count > Maximo)
                Debug.LogWarning($"[Usabilidad] {tipo} muestra {botones.Count} botones (máximo {Maximo}): " +
                                 string.Join(", ", botones.Select(Rotulo)));
        }

        private static bool EsZonaInvisible(Button b) {
            var img = b.targetGraphic as Image;
            return img != null && img.color.a < 0.01f && b.GetComponentInChildren<TMPro.TMP_Text>() == null;
        }

        private static string Rotulo(Button b) {
            var t = b.GetComponentInChildren<TMPro.TMP_Text>();
            return t != null && !string.IsNullOrEmpty(t.text) ? "«" + t.text + "»" : b.name;
        }
    }

    /// <summary>Marca un contenedor cuyos botones son contenido (una lista), no acciones de la pantalla.</summary>
    public sealed class ListaDeElementos : MonoBehaviour { }
}
