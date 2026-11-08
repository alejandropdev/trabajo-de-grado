using Nexus.Unity.Aplicacion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nexus.EditorTools {
    /// <summary>
    /// Capturas de la Fase 1 (2026-11-08): entra en Play con la escena del juego, deja que CapturadorDeFase1 recorra
    /// la Metodologia y la Calidad, y vuelve. Los PNG y el informe quedan en Library/capturas-fase1/.
    ///
    ///   Nexus > Capturar Fase 1
    ///
    /// Sin abrir el editor (con el editor CERRADO; sin -quit, porque tiene que entrar en Play):
    ///   Unity.exe -batchmode -projectPath "game/proyecto de grado" -executeMethod Nexus.EditorTools.CapturasDeFase1.Capturar -logFile capturas.log
    /// Sale con 0 si no hubo desbordes ni scrolls, con 1 si los hubo.
    /// </summary>
    [InitializeOnLoad]
    public static class CapturasDeFase1 {
        private const string Clave = "Nexus.CapturasDeFase1.Activo";
        private const string Escena = "Assets/Scenes/Nexus.unity";

        static CapturasDeFase1() {
            EditorApplication.playModeStateChanged += AlCambiar;
        }

        [MenuItem("Nexus/Capturar Fase 1")]
        public static void Capturar() {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Capturas] Sal de Play antes."); return; }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool(Clave, true);
            EditorSceneManager.OpenScene(Escena);
            EditorApplication.EnterPlaymode();
        }

        private static void AlCambiar(PlayModeStateChange cambio) {
            if (!SessionState.GetBool(Clave, false)) return;
            if (cambio == PlayModeStateChange.EnteredPlayMode) {
                new GameObject("Capturador de la Fase 1").AddComponent<CapturadorDeFase1>();
            } else if (cambio == PlayModeStateChange.EnteredEditMode) {
                SessionState.SetBool(Clave, false);
                Debug.Log("[Capturas] Listas en " + CapturadorDeFase1.Carpeta + " · fallos: " + CapturadorDeFase1.Fallos);
                if (Application.isBatchMode) EditorApplication.Exit(CapturadorDeFase1.Fallos > 0 ? 1 : 0);
                else EditorUtility.RevealInFinder(CapturadorDeFase1.Carpeta);
            }
        }
    }
}
