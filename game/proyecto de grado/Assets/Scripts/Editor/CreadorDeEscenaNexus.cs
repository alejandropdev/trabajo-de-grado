using System.IO;
using System.Linq;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nexus.EditorTools {
    /// <summary>
    /// Monta la escena del juego: una camara y un AppRoot con el tema asignado. Todo lo demas (lienzo,
    /// EventSystem, pantallas) lo crea AppRoot en tiempo de ejecucion, asi que la escena no hay que tocarla
    /// nunca mas: añadir una pantalla es añadir una clase.
    ///
    /// Menu: Nexus > Crear escena principal
    /// </summary>
    public static class CreadorDeEscenaNexus {
        private const string RutaEscena = "Assets/Scenes/Nexus.unity";
        private const string CarpetaTema = "Assets/Settings";
        private const string RutaTema = CarpetaTema + "/NexusTheme.asset";

        [MenuItem("Nexus/Crear escena principal")]
        public static void Crear() {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (File.Exists(RutaEscena) &&
                !EditorUtility.DisplayDialog("La escena ya existe",
                    RutaEscena + " ya existe. ¿La vuelvo a crear desde cero?\n\n" +
                    "El tema (NexusTheme.asset) no se toca.", "Recrear", "Cancelar"))
                return;

            var tema = BuscarOCrearTema();

            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Con un lienzo en modo overlay no haria falta camara, pero sin ninguna el editor avisa de que
            // no se renderiza nada. Se pone una que solo pinta el fondo del tema.
            var camara = new GameObject("Camara", typeof(Camera), typeof(AudioListener));
            camara.tag = "MainCamera";
            var cam = camara.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = tema.fondo;
            cam.orthographic = true;

            var app = new GameObject("AppRoot");
            var raiz = app.AddComponent<AppRoot>();
            var so = new SerializedObject(raiz);
            so.FindProperty("_tema").objectReferenceValue = tema;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(escena, RutaEscena);

            // Primera en Build Settings: es la que abre el juego. Las demas se conservan detras.
            var escenas = EditorBuildSettings.scenes.Where(s => s.path != RutaEscena).ToList();
            escenas.Insert(0, new EditorBuildSettingsScene(RutaEscena, true));
            EditorBuildSettings.scenes = escenas.ToArray();

            Selection.activeObject = app;
            EditorUtility.DisplayDialog("Escena creada",
                RutaEscena + " está lista y es la primera en Build Settings.\n\n" +
                "Pulsa Play: tiene que salir la pantalla de diagnóstico con el tema a la izquierda y un día " +
                "de verdad del Nivel 0 a la derecha.", "Vale");
        }

        [MenuItem("Nexus/Abrir la carpeta de guardado")]
        public static void AbrirCarpetaDeGuardado() {
            Directory.CreateDirectory(RutasDeGuardado.Raiz);
            EditorUtility.RevealInFinder(RutasDeGuardado.Raiz);
        }

        private static NexusTheme BuscarOCrearTema() {
            var existente = AssetDatabase.FindAssets("t:NexusTheme")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<NexusTheme>)
                .FirstOrDefault(t => t != null);
            if (existente != null) return existente;

            if (!AssetDatabase.IsValidFolder(CarpetaTema)) AssetDatabase.CreateFolder("Assets", "Settings");
            var tema = ScriptableObject.CreateInstance<NexusTheme>();
            AssetDatabase.CreateAsset(tema, RutaTema);
            AssetDatabase.SaveAssets();
            return tema;
        }
    }
}
