using Nexus.Unity.Aplicacion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nexus.EditorTools {
    /// <summary>
    /// Galeria de dibujos hechos por codigo (2026-11-14, auditoria de arte): entra en Play con la escena del juego, deja
    /// que CapturadorDeGaleria pinte cada dibujo fuera de su pantalla y lo fotografie, y vuelve. Los PNG y el informe
    /// quedan en Library/capturas-galeria/.
    ///
    ///   Nexus > Capturar Galeria 2D
    ///
    /// Sin abrir el editor (con el editor CERRADO; sin -quit, porque tiene que entrar en Play):
    ///   Unity.exe -batchmode -projectPath "game/proyecto de grado" -executeMethod Nexus.EditorTools.CapturasDeGaleria.Capturar -logFile galeria.log
    /// </summary>
    [InitializeOnLoad]
    public static class CapturasDeGaleria {
        private const string Clave = "Nexus.CapturasDeGaleria.Activo";
        private const string Escena = "Assets/Scenes/Nexus.unity";

        static CapturasDeGaleria() {
            EditorApplication.playModeStateChanged += AlCambiar;
        }

        [MenuItem("Nexus/Capturar Galeria 2D")]
        public static void Capturar() {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Capturas galeria] Sal de Play antes."); return; }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool(Clave, true);
            EditorSceneManager.OpenScene(Escena);
            EditorApplication.EnterPlaymode();
        }

        private static void AlCambiar(PlayModeStateChange cambio) {
            if (!SessionState.GetBool(Clave, false)) return;
            if (cambio == PlayModeStateChange.EnteredPlayMode) {
                new GameObject("Capturador de la galeria").AddComponent<CapturadorDeGaleria>();
            } else if (cambio == PlayModeStateChange.EnteredEditMode) {
                SessionState.SetBool(Clave, false);
                Debug.Log("[Capturas galeria] Listas en " + CapturadorDeGaleria.Carpeta + " · fallos: " + CapturadorDeGaleria.Fallos);
                if (Application.isBatchMode) EditorApplication.Exit(CapturadorDeGaleria.Fallos > 0 ? 1 : 0);
                else EditorUtility.RevealInFinder(CapturadorDeGaleria.Carpeta);
            }
        }
    }
}
