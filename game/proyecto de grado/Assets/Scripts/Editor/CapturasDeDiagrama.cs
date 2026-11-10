using Nexus.Unity.Aplicacion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nexus.EditorTools {
    /// <summary>
    /// Capturas del diagrama con cartas (2026-11-09): entra en Play con la escena del juego, deja que
    /// CapturadorDeDiagrama juegue MJ-F1-07 y MJ-F0-02 de principio a fin, y vuelve. Los PNG y el informe quedan en
    /// Library/capturas-diagrama/.
    ///
    ///   Nexus > Capturar minijuego de diagrama
    ///
    /// Sin abrir el editor (con el editor CERRADO; sin -quit, porque tiene que entrar en Play):
    ///   Unity.exe -batchmode -projectPath "game/proyecto de grado" -executeMethod Nexus.EditorTools.CapturasDeDiagrama.Capturar -logFile capturas.log
    /// </summary>
    [InitializeOnLoad]
    public static class CapturasDeDiagrama {
        private const string Clave = "Nexus.CapturasDeDiagrama.Activo";
        private const string Escena = "Assets/Scenes/Nexus.unity";

        static CapturasDeDiagrama() {
            EditorApplication.playModeStateChanged += AlCambiar;
        }

        [MenuItem("Nexus/Capturar minijuego de diagrama")]
        public static void Capturar() {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Capturas diagrama] Sal de Play antes."); return; }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool(Clave, true);
            EditorSceneManager.OpenScene(Escena);
            EditorApplication.EnterPlaymode();
        }

        private static void AlCambiar(PlayModeStateChange cambio) {
            if (!SessionState.GetBool(Clave, false)) return;
            if (cambio == PlayModeStateChange.EnteredPlayMode) {
                new GameObject("Capturador del diagrama").AddComponent<CapturadorDeDiagrama>();
            } else if (cambio == PlayModeStateChange.EnteredEditMode) {
                SessionState.SetBool(Clave, false);
                Debug.Log("[Capturas diagrama] Listas en " + CapturadorDeDiagrama.Carpeta + " · fallos: " + CapturadorDeDiagrama.Fallos);
                if (Application.isBatchMode) EditorApplication.Exit(CapturadorDeDiagrama.Fallos > 0 ? 1 : 0);
                else EditorUtility.RevealInFinder(CapturadorDeDiagrama.Carpeta);
            }
        }
    }
}
