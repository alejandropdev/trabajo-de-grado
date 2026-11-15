using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Capturador = global::Nexus.Mundo3D.CapturadorDelRecorrido3D;

namespace Nexus.EditorTools.Mundo3D {
    /// <summary>
    /// Capturas del recorrido 3D (2026-11-14, auditoria de arte): abre Recoleccion3D, entra en Play, deja que
    /// CapturadorDelRecorrido3D la ambiente como el juego y la fotografie desde camaras fijas, y vuelve. Los PNG y el
    /// informe quedan en Library/capturas-3d/. No guarda nada en la escena.
    ///
    ///   Nexus > Mundo 3D > Capturar recorrido
    ///
    /// Sin abrir el editor (con el editor CERRADO; sin -quit, porque tiene que entrar en Play):
    ///   Unity.exe -batchmode -projectPath "game/proyecto de grado" -executeMethod Nexus.EditorTools.Mundo3D.CapturasDelRecorrido3D.Capturar -logFile capturas3d.log
    /// </summary>
    [InitializeOnLoad]
    public static class CapturasDelRecorrido3D {
        private const string Clave = "Nexus.CapturasDelRecorrido3D.Activo";
        private const string Escena = "Assets/Mundo3D/Escenas/Recoleccion3D.unity";

        static CapturasDelRecorrido3D() {
            EditorApplication.playModeStateChanged += AlCambiar;
        }

        [MenuItem("Nexus/Mundo 3D/Capturar recorrido")]
        public static void Capturar() {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Capturas 3D] Sal de Play antes."); return; }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool(Clave, true);
            EditorSceneManager.OpenScene(Escena);
            EditorApplication.EnterPlaymode();
        }

        private static void AlCambiar(PlayModeStateChange cambio) {
            if (!SessionState.GetBool(Clave, false)) return;
            if (cambio == PlayModeStateChange.EnteredPlayMode) {
                new GameObject("Capturador del recorrido 3D").AddComponent<Capturador>();
            } else if (cambio == PlayModeStateChange.EnteredEditMode) {
                SessionState.SetBool(Clave, false);
                Debug.Log("[Capturas 3D] Listas en " + Capturador.Carpeta + " · fallos: " + Capturador.Fallos);
                if (Application.isBatchMode) EditorApplication.Exit(Capturador.Fallos > 0 ? 1 : 0);
                else EditorUtility.RevealInFinder(Capturador.Carpeta);
            }
        }
    }
}
