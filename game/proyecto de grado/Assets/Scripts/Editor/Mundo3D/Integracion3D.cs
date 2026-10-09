using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cinemachine;
using Nexus.Mundo3D;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Nexus.EditorTools.Mundo3D {
    /// <summary>
    /// Lo que hay que hacerle al proyecto 3D para que viva dentro de este (Unity 6, URP, Input System nuevo). El
    /// 3D llego de un proyecto de Unity 2022 con el pipeline integrado y la entrada antigua, copiado TAL CUAL a
    /// Assets/Mundo3D/Externo. Tres pasos, que se pueden repetir las veces que haga falta (no deshacen nada):
    ///
    ///   Nexus > Mundo 3D > 1 · Convertir materiales a URP
    ///   Nexus > Mundo 3D > 2 · Preparar escena de recolección
    ///   Nexus > Mundo 3D > 3 · Verificar integración        → tools/integracion-3d/informe-unity.txt
    ///
    /// Sin abrir el editor:  Unity -batchmode -projectPath … -executeMethod Nexus.EditorTools.Mundo3D.Integracion3D.TodoEnLote
    /// </summary>
    public static class Integracion3D {
        private const string Carpeta = "Assets/Mundo3D";
        // El nivel que construyo el equipo de 3D (las zonas, los recolectables, el jugador). ThirdPerson.unity, en
        // el mismo paquete, es solo la demo del controlador.
        private const string EscenaOriginal = Carpeta + "/Externo/malfarie/Levels/Level1.unity";
        private const string CarpetaDeEscenas = Carpeta + "/Escenas";
        private const string EscenaDeRecoleccion = CarpetaDeEscenas + "/" + ModuloDeRecoleccion3D.Escena + ".unity";
        private const string Acciones = "Assets/InputSystem_Actions.inputactions";
        private const string ShaderLit = "Universal Render Pipeline/Lit";

        // ==================================================================== 1 · materiales

        [MenuItem("Nexus/Mundo 3D/1 · Convertir materiales a URP")]
        public static void ConvertirMateriales() {
            Debug.Log("[Mundo3D] " + ConvertirMaterialesYContar());
        }

        /// <summary>
        /// Solo toca los materiales de Assets/Mundo3D (el conversor de Unity recorre el proyecto entero). Los del
        /// shader Standard pasan por el conversor oficial; los de shaders del pipeline integrado que URP no pinta
        /// (Legacy, Mobile y los «surface shaders» propios de algun pack) pasan a URP/Lit con su textura y su color.
        /// </summary>
        private static string ConvertirMaterialesYContar() {
            var lit = Shader.Find(ShaderLit);
            if (lit == null) throw new InvalidOperationException("No se encuentra el shader " + ShaderLit + ": ¿el proyecto usa URP?");

            var oficiales = new List<MaterialUpgrader> {
                new StandardUpgrader("Standard"), new StandardUpgrader("Standard (Specular setup)")
            };
            int standard = 0, aLit = 0, yaValian = 0;
            var sinTocar = new SortedDictionary<string, int>();

            foreach (var ruta in RutasDe("t:Material").Where(r => r.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))) {
                var material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (material == null || material.shader == null) continue;
                var nombre = material.shader.name;

                if (nombre == "Standard" || nombre == "Standard (Specular setup)") {
                    MaterialUpgrader.Upgrade(material, oficiales, MaterialUpgrader.UpgradeFlags.None);
                    standard++;
                } else if (NecesitaLit(material.shader)) {
                    PasarALit(material, lit);
                    aLit++;
                } else {
                    yaValian++;
                    sinTocar[nombre] = sinTocar.TryGetValue(nombre, out var n) ? n + 1 : 1;
                    continue;
                }
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
            return $"Materiales: {standard} de Standard convertidos, {aLit} pasados a URP/Lit, {yaValian} sin tocar (" +
                   string.Join(", ", sinTocar.Select(kv => $"{kv.Value} × {kv.Key}")) + ").";
        }

        /// <summary>Un shader que URP no pinta: de las familias antiguas, roto, o un surface shader de un pack.</summary>
        private static bool NecesitaLit(Shader shader) {
            var nombre = shader.name;
            if (nombre == "Hidden/InternalErrorShader" || nombre.StartsWith("Legacy Shaders/") || nombre.StartsWith("Mobile/"))
                return true;
            var ruta = AssetDatabase.GetAssetPath(shader);
            if (string.IsNullOrEmpty(ruta) || !ruta.EndsWith(".shader", StringComparison.OrdinalIgnoreCase) || !File.Exists(ruta))
                return false;
            return File.ReadAllText(ruta).Contains("#pragma surface");
        }

        private static void PasarALit(Material material, Shader lit) {
            var textura = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            var escala = textura != null ? material.GetTextureScale("_MainTex") : Vector2.one;
            var desplazamiento = textura != null ? material.GetTextureOffset("_MainTex") : Vector2.zero;
            var color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            var suavidad = material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : 0.5f;
            var metal = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0f;

            material.shader = lit;
            material.SetTexture("_BaseMap", textura);
            material.SetTextureScale("_BaseMap", escala);
            material.SetTextureOffset("_BaseMap", desplazamiento);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", suavidad);
            material.SetFloat("_Metallic", metal);
        }

        // ==================================================================== 2 · la escena del recorrido

        [MenuItem("Nexus/Mundo 3D/2 · Preparar escena de recolección")]
        public static void PrepararEscena() {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Debug.Log("[Mundo3D] " + PrepararEscenaYContar());
        }

        /// <summary>
        /// La escena del recorrido es una COPIA de la del equipo de 3D (la suya no se toca). A la copia se le cambia
        /// lo que el Input System nuevo exige: el modulo de entrada de su EventSystem y de donde lee el raton la camara.
        /// </summary>
        private static string PrepararEscenaYContar() {
            var hecho = new List<string>();
            if (!File.Exists(EscenaDeRecoleccion)) {
                if (!File.Exists(EscenaOriginal)) throw new FileNotFoundException("Falta la escena del 3D.", EscenaOriginal);
                if (!AssetDatabase.IsValidFolder(CarpetaDeEscenas)) AssetDatabase.CreateFolder(Carpeta, "Escenas");
                if (!AssetDatabase.CopyAsset(EscenaOriginal, EscenaDeRecoleccion))
                    throw new IOException("No se pudo copiar " + EscenaOriginal);
                hecho.Add("copiada de " + EscenaOriginal);
            }

            var escena = EditorSceneManager.OpenScene(EscenaDeRecoleccion, OpenSceneMode.Single);
            var todos = escena.GetRootGameObjects();

            foreach (var antiguo in todos.SelectMany(r => r.GetComponentsInChildren<StandaloneInputModule>(true)).ToList()) {
                var objeto = antiguo.gameObject;
                UnityEngine.Object.DestroyImmediate(antiguo);
                if (objeto.GetComponent<InputSystemUIInputModule>() == null) objeto.AddComponent<InputSystemUIInputModule>();
                hecho.Add("EventSystem con InputSystemUIInputModule");
            }

            var mirar = AssetDatabase.LoadAllAssetsAtPath(Acciones).OfType<InputActionReference>()
                .FirstOrDefault(r => r.action != null && r.action.actionMap.name == "Player" && r.action.name == "Look");
            if (mirar == null) throw new InvalidOperationException("No se encuentra la accion Player/Look en " + Acciones);
            foreach (var camara in todos.SelectMany(r => r.GetComponentsInChildren<CinemachineFreeLook>(true))) {
                var proveedor = camara.GetComponent<CinemachineInputProvider>();
                if (proveedor == null) proveedor = camara.gameObject.AddComponent<CinemachineInputProvider>();
                if (proveedor.XYAxis == mirar) continue;
                proveedor.XYAxis = mirar;
                EditorUtility.SetDirty(proveedor);
                hecho.Add("la camara lee Player/Look");
            }

            if (hecho.Count > 0) {
                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
            }

            // En Build Settings, detras de las que ya hay: la primera sigue siendo la del juego.
            if (EditorBuildSettings.scenes.All(s => s.path != EscenaDeRecoleccion)) {
                EditorBuildSettings.scenes = EditorBuildSettings.scenes
                    .Append(new EditorBuildSettingsScene(EscenaDeRecoleccion, true)).ToArray();
                hecho.Add("añadida a Build Settings");
            }
            return EscenaDeRecoleccion + ": " + (hecho.Count == 0 ? "ya estaba preparada." : string.Join("; ", hecho) + ".");
        }

        // ==================================================================== 3 · verificar

        [MenuItem("Nexus/Mundo 3D/3 · Verificar integración")]
        public static void Verificar() {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var problemas = VerificarYEscribir(out var ruta);
            Debug.Log($"[Mundo3D] Verificación: {problemas} problema(s) en lo que usa el juego. Informe: {ruta}");
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(ruta);
        }

        /// <summary>
        /// Recorre todo lo que llego del 3D y escribe lo que encuentra. Lo que impide jugar (scripts perdidos o
        /// materiales sin shader en la escena del recorrido, o que no este en Build Settings) cuenta como problema;
        /// lo de las escenas de demostracion de los packs se anota, y nada mas: ya venian asi, o son de otro pipeline.
        /// </summary>
        private static int VerificarYEscribir(out string rutaDelInforme) {
            var informe = new StringBuilder();
            var problemas = 0;
            informe.AppendLine("Verificación de Assets/Mundo3D · Unity " + Application.unityVersion);
            informe.AppendLine();

            // --- materiales ---
            var porShader = new SortedDictionary<string, int>();
            var rotos = new List<string>();
            foreach (var ruta in RutasDe("t:Material").Where(r => r.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))) {
                var material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (material == null) continue;
                var nombre = material.shader != null ? material.shader.name : "(sin shader)";
                porShader[nombre] = porShader.TryGetValue(nombre, out var n) ? n + 1 : 1;
                if (material.shader == null || nombre == "Hidden/InternalErrorShader" || NecesitaLit(material.shader)) rotos.Add(ruta);
            }
            informe.AppendLine("MATERIALES (.mat) por shader");
            foreach (var kv in porShader) informe.AppendLine($"  {kv.Value,5}  {kv.Key}");
            informe.AppendLine($"  Sin shader valido para URP: {rotos.Count}");
            foreach (var r in rotos) informe.AppendLine("    " + r);
            informe.AppendLine();

            // --- prefabs ---
            var prefabs = RutasDe("t:Prefab").Where(r => r.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)).ToList();
            var prefabsRotos = new List<string>();
            foreach (var ruta in prefabs) {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (prefab == null) { prefabsRotos.Add(ruta + "  (no carga)"); continue; }
                var perdidos = prefab.GetComponentsInChildren<Transform>(true)
                    .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                if (perdidos > 0) prefabsRotos.Add($"{ruta}  ({perdidos} script(s) perdido(s))");
            }
            informe.AppendLine($"PREFABS: {prefabs.Count} · con scripts perdidos: {prefabsRotos.Count}");
            foreach (var r in prefabsRotos) informe.AppendLine("    " + r);
            informe.AppendLine();

            // --- escenas ---
            informe.AppendLine("ESCENAS");
            foreach (var ruta in RutasDe("t:Scene").OrderBy(r => r)) {
                var esLaDelJuego = ruta == EscenaDeRecoleccion;
                try {
                    var escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
                    var objetos = escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToList();
                    var perdidos = objetos.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                    var sinMaterial = objetos.SelectMany(t => t.GetComponents<Renderer>())
                        .Count(r => r.sharedMaterials.Any(m => m == null || m.shader == null || m.shader.name == "Hidden/InternalErrorShader"));
                    var entradaAntigua = objetos.Count(t => t.GetComponent<StandaloneInputModule>() != null);
                    informe.AppendLine($"  {(esLaDelJuego ? "★" : " ")} {ruta}");
                    informe.AppendLine($"      {objetos.Count} objetos · scripts perdidos: {perdidos} · renderers con material roto: {sinMaterial}" +
                                       $" · StandaloneInputModule: {entradaAntigua}");
                    if (esLaDelJuego) problemas += perdidos + sinMaterial + entradaAntigua;
                } catch (Exception e) {
                    informe.AppendLine($"    {ruta}\n      NO ABRE: {e.Message}");
                    if (esLaDelJuego) problemas++;
                }
            }
            informe.AppendLine();

            // --- lo que el juego necesita ---
            var existe = File.Exists(EscenaDeRecoleccion);
            var enBuild = EditorBuildSettings.scenes.Any(s => s.enabled && s.path == EscenaDeRecoleccion);
            if (!existe || !enBuild) problemas++;
            informe.AppendLine("EL JUEGO");
            informe.AppendLine($"  Escena del recorrido ({EscenaDeRecoleccion}): {(existe ? "existe" : "FALTA")} · " +
                               (enBuild ? "en Build Settings" : "NO esta en Build Settings"));
            informe.AppendLine("  Build Settings: " + string.Join(" → ", EditorBuildSettings.scenes.Select(s => Path.GetFileNameWithoutExtension(s.path))));
            informe.AppendLine();
            informe.AppendLine($"PROBLEMAS EN LO QUE USA EL JUEGO (★): {problemas}");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            rutaDelInforme = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../tools/integracion-3d/informe-unity.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(rutaDelInforme));
            File.WriteAllText(rutaDelInforme, informe.ToString(), new UTF8Encoding(false));
            return problemas;
        }

        // ==================================================================== sin abrir el editor

        /// <summary>Los tres pasos seguidos. Sale con 0 si lo que usa el juego esta bien, y con 1 si no.</summary>
        public static void TodoEnLote() {
            try {
                Debug.Log("[Mundo3D] " + ConvertirMaterialesYContar());
                Debug.Log("[Mundo3D] " + PrepararEscenaYContar());
                var problemas = VerificarYEscribir(out var ruta);
                Debug.Log($"[Mundo3D] Verificación: {problemas} problema(s). Informe: {ruta}");
                AssetDatabase.SaveAssets();
                EditorApplication.Exit(problemas == 0 ? 0 : 1);
            } catch (Exception e) {
                Debug.LogException(e);
                EditorApplication.Exit(2);
            }
        }

        private static IEnumerable<string> RutasDe(string filtro) {
            return AssetDatabase.FindAssets(filtro, new[] { Carpeta }).Select(AssetDatabase.GUIDToAssetPath).Distinct();
        }
    }
}
