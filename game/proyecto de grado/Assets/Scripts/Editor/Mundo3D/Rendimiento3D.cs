using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Nexus.Mundo3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Nexus.EditorTools.Mundo3D {
    /// <summary>
    /// El rendimiento del recorrido 3D. El nivel llego con unos 14 millones de triangulos, 6.000 piezas de malla
    /// y 5.000 MeshColliders; aqui esta lo que se le hace a la COPIA que usa el juego (el nivel del equipo de 3D
    /// y sus modelos no se tocan) y la forma de medirlo:
    ///
    ///   Nexus > Mundo 3D > 4 · Configurar el render del 3D        un nivel de calidad propio, mas barato
    ///   Nexus > Mundo 3D > 5 · Rehacer y optimizar la escena      copia nueva del nivel + modelos reducidos, cajas, sombras
    ///   Nexus > Mundo 3D > 6 · Ajustar importación                texturas y mallas de lo que usa la escena
    ///   Nexus > Mundo 3D > 7 · Medir rendimiento                  → tools/integracion-3d/informe-rendimiento.txt
    ///
    /// Los modelos reducidos los hace Blender (tools/integracion-3d/reducir_modelos.py), que deja escrito en
    /// informe-reduccion.csv que modelo sustituye a cual.
    /// </summary>
    public static class Rendimiento3D {
        private const string Carpeta = "Assets/Mundo3D";
        private const string Externo = Carpeta + "/Externo/";
        private const string NivelOriginal = Externo + "malfarie/Levels/Level1.unity";
        private const string Escena = Carpeta + "/Escenas/" + ModuloDeRecoleccion3D.Escena + ".unity";
        private const string PerfilDeRender = "Assets/Settings/Mundo3D_RPAsset.asset";

        /// <summary>Lo que no es un mueble: el nivel. Su forma es su colision, y no se sustituye ni se encaja.</summary>
        private static readonly string[] EsElNivel = { "Mapa3D" };

        private const float LejosDeLaCamara = 150f;
        /// <summary>Por debajo de este tamaño (metros), un objeto no proyecta sombra: no se echa de menos.</summary>
        private const float SinSombraPorDebajoDe = 0.6f;
        /// <summary>Un mueble con mas colliders de malla que esto pasa a tener una sola caja.</summary>
        private const int CollidersParaEncajar = 2;
        /// <summary>
        /// Una caja solo vale para lo que se rodea. Lo que mide mas que esto en planta (una recepcion, un mostrador en
        /// U, una fila de percheros) se atraviesa o se entra en ello: una caja seria un muro invisible.
        /// </summary>
        private const float CajaHastaMetros = 2.5f;

        private static string CarpetaDeInformes {
            get { return Path.GetFullPath(Path.Combine(Application.dataPath, "../../../tools/integracion-3d")); }
        }

        // ==================================================================== 4 · el render del 3D

        [MenuItem("Nexus/Mundo 3D/4 · Configurar el render del 3D")]
        public static void ConfigurarRender() { Debug.Log("[Mundo3D] " + ConfigurarRenderYContar()); }

        /// <summary>
        /// El 3D se pinta con su propio nivel de calidad («Mundo 3D»), que el juego activa al entrar al recorrido y
        /// suelta al salir. Un nivel de calidad, y no un asset suelto, para que la build incluya sus variantes de shader.
        /// El GPU Resident Drawer que usa necesita, ademas, dos ajustes del proyecto.
        /// </summary>
        private static string ConfigurarRenderYContar() {
            var hecho = new List<string>();
            var perfil = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PerfilDeRender);
            if (perfil == null) throw new FileNotFoundException("Falta el perfil de render del 3D.", PerfilDeRender);

            var calidad = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            var niveles = calidad.FindProperty("m_QualitySettings");
            var indice = -1;
            for (var i = 0; i < niveles.arraySize; i++)
                if (niveles.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == PantallaDeRecoleccion3D.NivelDeCalidad) indice = i;
            if (indice < 0) {
                // Copia del nivel en uso (el del juego), al final: los indices de los que ya hay no cambian.
                var origen = QualitySettings.GetQualityLevel();
                niveles.InsertArrayElementAtIndex(niveles.arraySize);
                indice = niveles.arraySize - 1;
                CopiarNivel(niveles.GetArrayElementAtIndex(origen), niveles.GetArrayElementAtIndex(indice));
                hecho.Add("nivel de calidad «" + PantallaDeRecoleccion3D.NivelDeCalidad + "» creado");
            }
            var nivel = niveles.GetArrayElementAtIndex(indice);
            nivel.FindPropertyRelative("name").stringValue = PantallaDeRecoleccion3D.NivelDeCalidad;
            nivel.FindPropertyRelative("customRenderPipeline").objectReferenceValue = perfil;
            nivel.FindPropertyRelative("vSyncCount").intValue = 1;      // sin tope, la GPU va al 100 % siempre
            nivel.FindPropertyRelative("antiAliasing").intValue = 0;
            if (calidad.ApplyModifiedPropertiesWithoutUndo()) hecho.Add("nivel de calidad ajustado");

            // GPU Resident Drawer: necesita las variantes BatchRendererGroup en la build y el static batching apagado.
            var graficos = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var brg = graficos.FindProperty("m_BrgStripping");
            if (brg != null && brg.intValue != 2) {
                brg.intValue = 2;   // Keep All
                graficos.ApplyModifiedPropertiesWithoutUndo();
                hecho.Add("variantes BatchRendererGroup: Keep All");
            }
            if (ApagarStaticBatching()) hecho.Add("static batching apagado");

            AssetDatabase.SaveAssets();
            return "Render del 3D: " + (hecho.Count == 0 ? "ya estaba configurado." : string.Join("; ", hecho) + ".");
        }

        private static void CopiarNivel(SerializedProperty origen, SerializedProperty destino) {
            var fin = origen.GetEndProperty();
            var o = origen.Copy();
            var entra = true;
            while (o.Next(entra) && !SerializedProperty.EqualContents(o, fin)) {
                entra = false;
                var d = destino.FindPropertyRelative(o.name);
                if (d == null || d.propertyType != o.propertyType) continue;
                switch (o.propertyType) {
                    case SerializedPropertyType.Integer: d.intValue = o.intValue; break;
                    case SerializedPropertyType.Boolean: d.boolValue = o.boolValue; break;
                    case SerializedPropertyType.Float: d.floatValue = o.floatValue; break;
                    case SerializedPropertyType.String: d.stringValue = o.stringValue; break;
                    case SerializedPropertyType.Enum: d.enumValueIndex = o.enumValueIndex; break;
                    case SerializedPropertyType.ObjectReference: d.objectReferenceValue = o.objectReferenceValue; break;
                    case SerializedPropertyType.Vector3: d.vector3Value = o.vector3Value; break;
                }
            }
        }

        /// <summary>PlayerSettings no expone el static batching: es el mismo metodo interno que usa su inspector.</summary>
        private static bool ApagarStaticBatching() {
            const BindingFlags interno = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            var leer = typeof(PlayerSettings).GetMethod("GetBatchingForPlatform", interno);
            var poner = typeof(PlayerSettings).GetMethod("SetBatchingForPlatform", interno);
            if (leer == null || poner == null) return false;
            var objetivo = EditorUserBuildSettings.activeBuildTarget;
            var args = new object[] { objetivo, 0, 0 };
            leer.Invoke(null, args);
            if ((int)args[1] == 0) return false;
            poner.Invoke(null, new object[] { objetivo, 0, (int)args[2] });
            return true;
        }

        // ==================================================================== 5 · la escena

        [MenuItem("Nexus/Mundo 3D/5 · Rehacer y optimizar la escena de recolección")]
        public static void RehacerEscena() {
            if (!Application.isBatchMode &&
                !EditorUtility.DisplayDialog("Rehacer la escena del recorrido",
                    Escena + " se sustituye por una copia nueva de\n" + NivelOriginal + "\ny se vuelve a preparar y a optimizar.\n\n" +
                    "Lo que se haya cambiado a mano en la copia se pierde.", "Rehacer", "Cancelar")) return;
            Debug.Log("[Mundo3D] " + RehacerEscenaYContar());
        }

        /// <summary>Copia el nivel encima de la escena del juego (mismo .meta, mismo GUID) y repite los dos pasos.</summary>
        private static string RehacerEscenaYContar() {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            File.Copy(NivelOriginal, Escena, true);
            AssetDatabase.ImportAsset(Escena, ImportAssetOptions.ForceUpdate);
            Integracion3D.PrepararEscena();
            return "Escena rehecha. " + OptimizarEscenaYContar();
        }

        /// <summary>
        /// Solo sobre la copia, recien rehecha desde el nivel. Cada mueble pesado se cambia por su version reducida:
        ///   · entero, por el modelo de una sola malla, si en el nivel esta tal como es el modelo;
        ///   · pieza a pieza (misma jerarquia, mallas reducidas) si el nivel le escondio, movio o colgo algo.
        /// Antes de aceptar un cambio se comprueba que lo nuevo ocupa el mismo sitio que lo que habia.
        /// </summary>
        private static string OptimizarEscenaYContar() {
            var sustitutos = LeerSustitutos();
            var escena = EditorSceneManager.OpenScene(Escena, OpenSceneMode.Single);
            var notas = new List<string>();
            var noEnteros = new List<string>();
            int enteros = 0, mallasCambiadas = 0, encajados = 0, collidersQuitados = 0, sinSombra = 0, animadores = 0;
            var escalas = AjustarEscalaDeLasPiezas(sustitutos);

            foreach (var camara in Todos<Camera>(escena)) {
                if (camara.farClipPlane > LejosDeLaCamara) camara.farClipPlane = LejosDeLaCamara;
                camara.allowMSAA = false;
                camara.allowHDR = false;
            }

            // Un prefab del equipo de 3D (una oficina entera) trae sus muebles dentro, y dentro de una instancia no se
            // puede quitar ni poner nada: se desempaqueta (en la copia) hasta que cada mueble queda suelto.
            for (var vuelta = 0; vuelta < 8; vuelta++) {
                var contenedores = Todos<Transform>(escena).Select(t => t.gameObject)
                    .Where(g => PrefabUtility.IsOutermostPrefabInstanceRoot(g) && !EsUnModelo(g) &&
                                g.GetComponentsInChildren<Transform>(true).Any(t => t.gameObject != g && PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)))
                    .ToList();
                if (contenedores.Count == 0) break;
                foreach (var c in contenedores)
                    PrefabUtility.UnpackPrefabInstance(c, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            }

            // Se copian a una lista: el bucle destruye objetos.
            var instancias = Todos<Transform>(escena).Select(t => t.gameObject)
                .Where(g => PrefabUtility.IsOutermostPrefabInstanceRoot(g) && EsUnModelo(g)).ToList();
            foreach (var instancia in instancias) {
                if (instancia == null) continue;
                var modelo = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromOriginalSource(instancia));
                if (EsElNivel.Any(modelo.Contains)) continue;
                var motivo = PorQueNoSeToca(instancia);
                if (motivo == "tiene esqueleto") continue;

                Sustituto sustituto;
                if (sustitutos.TryGetValue(modelo, out sustituto)) {
                    var fallo = motivo;
                    int quitados;
                    if (motivo == null && Sustituir(instancia, sustituto.Entero, out quitados, out fallo)) {
                        enteros++;
                        collidersQuitados += quitados;
                        continue;
                    }
                    noEnteros.Add($"no se cambió entero · {Ruta(instancia)}: {fallo}");
                }

                // Un collider de malla por pieza → una caja. Solo en lo que es un mueble y nada mas.
                if (motivo != null) continue;
                var deMalla = instancia.GetComponentsInChildren<MeshCollider>(true);
                if (deMalla.Length <= CollidersParaEncajar) continue;
                Bounds caja;
                var visibles = Visibles(instancia);
                if (!CabeEnUnaCaja(visibles) || !CajaEn(instancia.transform.worldToLocalMatrix, visibles, out caja)) continue;
                foreach (var c in deMalla) UnityEngine.Object.DestroyImmediate(c);
                PonerCaja(instancia, caja);
                collidersQuitados += deMalla.Length;
                encajados++;
            }

            // Lo que no se pudo cambiar entero (y lo que ni siquiera es una instancia de modelo: los prefabs de los
            // packs usan las mallas de un FBX sin instanciarlo) se reduce malla a malla.
            mallasCambiadas = CambiarMallas(escena, sustitutos, notas);

            foreach (var r in Todos<MeshRenderer>(escena)) {
                if (r.shadowCastingMode == ShadowCastingMode.Off) continue;
                var t = r.bounds.size;
                if (Mathf.Max(t.x, t.y, t.z) >= SinSombraPorDebajoDe) continue;
                r.shadowCastingMode = ShadowCastingMode.Off;
                sinSombra++;
            }
            foreach (var a in Todos<Animator>(escena)) {
                if (a.cullingMode != AnimatorCullingMode.AlwaysAnimate) continue;
                // El del jugador no: su Animator mueve el esqueleto del que cuelga la camara.
                if (a.GetComponentInParent<CharacterController>() != null) continue;
                a.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                animadores++;
            }

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);

            Directory.CreateDirectory(CarpetaDeInformes);
            File.WriteAllLines(Path.Combine(CarpetaDeInformes, "informe-optimizacion.txt"),
                new[] { $"Optimización de {Escena}", "", "MALLAS QUE SIGUEN SIN REDUCIR (lo que hay que mirar)" }.Concat(notas.OrderBy(n => n))
                    .Concat(new[] { "", "MUEBLES QUE NO SE CAMBIARON ENTEROS (se redujeron malla a malla, si arriba no dice lo contrario)" })
                    .Concat(noEnteros.OrderBy(n => n)), new UTF8Encoding(false));
            return $"Escena optimizada: {enteros} muebles sustituidos por su modelo reducido, {mallasCambiadas} mallas cambiadas por su versión reducida, " +
                   $"{encajados} muebles con una caja de colisión, {collidersQuitados} MeshColliders quitados, {sinSombra} objetos pequeños sin sombra, " +
                   $"{animadores} Animators con culling" + (escalas > 0 ? $", {escalas} modelos por piezas con la escala corregida" : "") +
                   $". {notas.Count} mallas sin reducir (informe-optimizacion.txt).";
        }

        private struct Sustituto { public string Entero, PorPiezas; }

        /// <summary>Lo que dejo escrito Blender: {modelo original → sus dos versiones reducidas}, con rutas de asset.</summary>
        private static Dictionary<string, Sustituto> LeerSustitutos() {
            var mapa = new Dictionary<string, Sustituto>();
            var csv = Path.Combine(CarpetaDeInformes, "informe-reduccion.csv");
            if (!File.Exists(csv)) return mapa;
            var lineas = File.ReadAllLines(csv);
            var cabecera = lineas[0].Split(',').ToList();
            int iModelo = cabecera.IndexOf("modelo"), iEstado = cabecera.IndexOf("estado"), iOpt = cabecera.IndexOf("optimizado"),
                iPartes = cabecera.IndexOf("por_piezas");
            foreach (var linea in lineas.Skip(1)) {
                var c = linea.Split(',');
                if (c.Length <= iOpt || c[iEstado] != "reducido" || AssetDatabase.LoadAssetAtPath<GameObject>(c[iOpt]) == null) continue;
                var partes = iPartes >= 0 && c.Length > iPartes && AssetDatabase.LoadAssetAtPath<GameObject>(c[iPartes]) != null ? c[iPartes] : null;
                mapa[Externo + c[iModelo]] = new Sustituto { Entero = c[iOpt], PorPiezas = partes };
            }
            return mapa;
        }

        private static bool EsUnModelo(GameObject instancia) {
            var ruta = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromOriginalSource(instancia));
            return !string.IsNullOrEmpty(ruta) && AssetImporter.GetAtPath(ruta) is ModelImporter;
        }

        /// <summary>
        /// Un mueble es geometria y nada mas. Si alguien le colgo un script, un trigger, una animacion o un objeto
        /// de la escena, deja de ser «solo un mueble»: no se cambia entero ni se le toca la colision.
        /// </summary>
        private static string PorQueNoSeToca(GameObject instancia) {
            if (instancia.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0) return "tiene esqueleto";
            if (instancia.GetComponentsInChildren<MonoBehaviour>(true).Length > 0) return "tiene scripts";
            if (instancia.GetComponentsInChildren<Animator>(true).Any(a => a.runtimeAnimatorController != null)) return "tiene animación";
            if (instancia.GetComponentsInChildren<Rigidbody>(true).Length > 0) return "tiene Rigidbody";
            if (instancia.GetComponentsInChildren<Collider>(true).Any(c => c.isTrigger)) return "tiene un trigger";
            if (instancia.GetComponentsInChildren<Transform>(true)
                .Any(t => PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject) != instancia)) return "tiene objetos de la escena dentro";
            return null;
        }

        /// <summary>Los renderers que de verdad se ven: el nivel puede haber apagado piezas de un modelo.</summary>
        private static List<MeshRenderer> Visibles(GameObject objeto) {
            return objeto.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && ActivoHasta(r.transform, objeto.transform)).ToList();
        }

        private static bool ActivoHasta(Transform t, Transform raiz) {
            for (; t != null && t != raiz; t = t.parent) if (!t.gameObject.activeSelf) return false;
            return true;
        }

        private static bool Sustituir(GameObject original, string rutaDelOptimizado, out int collidersQuitados, out string fallo) {
            collidersQuitados = 0;
            fallo = null;
            var padre = original.transform.parent;
            var alPadre = padre != null ? padre.worldToLocalMatrix : Matrix4x4.identity;
            var renderers = Visibles(original);
            if (renderers.Count < original.GetComponentsInChildren<MeshRenderer>(true).Length) { fallo = "el nivel le apagó piezas"; return false; }
            Bounds cajaOriginal;
            if (!CajaEn(alPadre, renderers, out cajaOriginal)) { fallo = "no tiene mallas"; return false; }

            // El material de cada sub-malla del modelo reducido se busca por el nombre que tenia en el FBX. Se toma el
            // que hay AHORA en la escena, asi que un material cambiado a mano en el nivel se conserva.
            var materiales = new Dictionary<string, Material>();
            foreach (var r in renderers) {
                var enElModelo = PrefabUtility.GetCorrespondingObjectFromOriginalSource(r);
                if (enElModelo == null) continue;
                var deOrigen = enElModelo.sharedMaterials;
                var deAhora = r.sharedMaterials;
                for (var i = 0; i < deOrigen.Length && i < deAhora.Length; i++)
                    if (deOrigen[i] != null && deAhora[i] != null && !materiales.ContainsKey(deOrigen[i].name))
                        materiales[deOrigen[i].name] = deAhora[i];
            }

            // Donde va el modelo nuevo. Los dos modelos guardan la misma geometria vista desde fuera, pero cada FBX
            // trae en su raiz su propio giro y su propia escala de importacion: se quita la del original y se pone la
            // del reducido, y encima, lo que el nivel le hizo a la instancia.
            var modeloOriginal = PrefabUtility.GetCorrespondingObjectFromOriginalSource(original).transform;
            var prefabNuevo = AssetDatabase.LoadAssetAtPath<GameObject>(rutaDelOptimizado);
            var local = Matrix4x4.TRS(original.transform.localPosition, original.transform.localRotation, original.transform.localScale)
                        * Matrix4x4.TRS(modeloOriginal.localPosition, modeloOriginal.localRotation, modeloOriginal.localScale).inverse
                        * Matrix4x4.TRS(prefabNuevo.transform.localPosition, prefabNuevo.transform.localRotation, prefabNuevo.transform.localScale);
            if (!local.ValidTRS()) { fallo = "su giro y su escala no se pueden trasladar al modelo reducido"; return false; }

            var nuevo = (GameObject)PrefabUtility.InstantiatePrefab(prefabNuevo, original.scene);
            nuevo.transform.SetParent(padre, false);
            nuevo.transform.localPosition = local.GetColumn(3);
            nuevo.transform.localRotation = local.rotation;
            nuevo.transform.localScale = local.lossyScale;
            nuevo.SetActive(true);

            // El modelo reducido salio de Blender: se comprueba que ocupa el mismo sitio que el original antes de fiarse.
            Bounds cajaNueva;
            var nuevos = nuevo.GetComponentsInChildren<MeshRenderer>(true).ToList();
            if (!CajaEn(alPadre, nuevos, out cajaNueva) || !Coinciden(cajaOriginal, cajaNueva)) {
                fallo = $"el modelo reducido no ocupa el mismo sitio (original {cajaOriginal.center:F2} {cajaOriginal.size:F2} · reducido {cajaNueva.center:F2} {cajaNueva.size:F2})";
                UnityEngine.Object.DestroyImmediate(nuevo);
                return false;
            }

            foreach (var r in nuevos) {
                var slots = r.sharedMaterials;
                for (var i = 0; i < slots.Length; i++) {
                    Material m;
                    if (slots[i] != null && materiales.TryGetValue(slots[i].name, out m)) slots[i] = m;
                }
                r.sharedMaterials = slots;
            }

            nuevo.name = original.name;
            nuevo.layer = original.layer;
            nuevo.tag = original.tag;
            nuevo.SetActive(original.activeSelf);
            nuevo.transform.SetSiblingIndex(original.transform.GetSiblingIndex());

            var teniaColision = original.GetComponentsInChildren<Collider>(true);
            Bounds cajaLocal;
            if (teniaColision.Length > 0) {
                if (CabeEnUnaCaja(nuevos) && CajaEn(nuevo.transform.worldToLocalMatrix, nuevos, out cajaLocal)) PonerCaja(nuevo, cajaLocal);
                else
                    // Demasiado grande para una caja: su forma, pero la de la malla reducida y en un solo collider.
                    foreach (var r in nuevos) r.gameObject.AddComponent<MeshCollider>().sharedMesh = r.GetComponent<MeshFilter>().sharedMesh;
            }
            collidersQuitados = teniaColision.Length;

            UnityEngine.Object.DestroyImmediate(original);
            return true;
        }

        /// <summary>
        /// Los objetos se quedan como estan (jerarquia, materiales, piezas apagadas, scripts) y solo cambia cada malla
        /// pesada por la reducida del mismo nombre, si mide lo mismo y tiene las mismas sub-mallas. Tambien en los
        /// MeshColliders que usaban esa malla.
        /// </summary>
        private static int CambiarMallas(Scene escena, Dictionary<string, Sustituto> sustitutos, List<string> notas) {
            var reducidas = new Dictionary<string, Dictionary<string, List<Mesh>>>();
            var cambio = new Dictionary<Mesh, Mesh>();
            var sinCambio = new Dictionary<Mesh, string>();

            Func<Mesh, Mesh> reducida = original => {
                Mesh hecha;
                if (original == null || sinCambio.ContainsKey(original)) return null;
                if (cambio.TryGetValue(original, out hecha)) return hecha;
                var modelo = AssetDatabase.GetAssetPath(original);
                Sustituto sustituto;
                if (!sustitutos.TryGetValue(modelo, out sustituto)) { sinCambio[original] = null; return null; }
                if (sustituto.PorPiezas == null) { sinCambio[original] = "no hay versión por piezas"; return null; }

                Dictionary<string, List<Mesh>> porNombre;
                if (!reducidas.TryGetValue(sustituto.PorPiezas, out porNombre)) {
                    reducidas[sustituto.PorPiezas] = porNombre = new Dictionary<string, List<Mesh>>();
                    foreach (var malla in AssetDatabase.LoadAllAssetsAtPath(sustituto.PorPiezas).OfType<Mesh>()) {
                        List<Mesh> lista;
                        if (!porNombre.TryGetValue(malla.name, out lista)) porNombre[malla.name] = lista = new List<Mesh>();
                        lista.Add(malla);
                    }
                }
                // Por nombre; y si Blender lo cambio (los repetidos acaban en .001), cualquiera que mida lo mismo.
                List<Mesh> candidatas;
                if (!porNombre.TryGetValue(original.name, out candidatas)) candidatas = porNombre.Values.SelectMany(l => l).ToList();
                var elegida = candidatas.FirstOrDefault(m => m.subMeshCount == original.subMeshCount && Coinciden(original.bounds, m.bounds));
                if (elegida == null) {
                    var c = candidatas.FirstOrDefault();
                    sinCambio[original] = c == null ? "el modelo por piezas no tiene mallas"
                        : $"ninguna pieza mide lo mismo (original {original.bounds.size:F3} · {original.subMeshCount} sub · la más parecida {c.bounds.size:F3} · {c.subMeshCount} sub)";
                    return null;
                }
                if (TriangulosDe(elegida) >= TriangulosDe(original)) { sinCambio[original] = null; return null; }
                cambio[original] = elegida;
                return elegida;
            };

            var cambiadas = 0;
            foreach (var filtro in Todos<MeshFilter>(escena)) {
                var nueva = reducida(filtro.sharedMesh);
                if (nueva == null) continue;
                filtro.sharedMesh = nueva;
                cambiadas++;
            }
            foreach (var collider in Todos<MeshCollider>(escena)) {
                Mesh nueva;
                if (collider.sharedMesh != null && cambio.TryGetValue(collider.sharedMesh, out nueva)) collider.sharedMesh = nueva;
            }

            foreach (var grupo in sinCambio.Where(kv => kv.Value != null && TriangulosDe(kv.Key) > 2000)
                                           .GroupBy(kv => AssetDatabase.GetAssetPath(kv.Key).Replace(Externo, "")))
                notas.Add($"{grupo.Sum(kv => TriangulosDe(kv.Key)),9:N0} triángulos · {grupo.Key}: {grupo.Count()} mallas · {grupo.First().Value}");
            return cambiadas;
        }

        /// <summary>
        /// El modelo por piezas guarda cada malla en las unidades del archivo, y Unity puede leerlas a otra escala que
        /// las del original (lo normal: 100 veces mayores, centimetros por metros). Se mide y se corrige en su
        /// importador, que es donde Unity lo resuelve.
        /// </summary>
        private static int AjustarEscalaDeLasPiezas(Dictionary<string, Sustituto> sustitutos) {
            var corregidos = 0;
            foreach (var par in sustitutos.Where(kv => kv.Value.PorPiezas != null)) {
                var originales = AssetDatabase.LoadAllAssetsAtPath(par.Key).OfType<Mesh>().ToDictionary(m => m, m => m.bounds.size.magnitude);
                var piezas = AssetDatabase.LoadAllAssetsAtPath(par.Value.PorPiezas).OfType<Mesh>().ToList();
                var razones = new List<float>();
                foreach (var o in originales.Where(kv => kv.Value > 0)) {
                    var pieza = piezas.FirstOrDefault(m => m.name == o.Key.name && m.bounds.size.magnitude > 0);
                    if (pieza != null) razones.Add(o.Value / pieza.bounds.size.magnitude);
                }
                if (razones.Count == 0) continue;
                razones.Sort();
                var k = razones[razones.Count / 2];
                if (Mathf.Abs(k - 1f) < 0.02f) continue;
                var importador = AssetImporter.GetAtPath(par.Value.PorPiezas) as ModelImporter;
                if (importador == null) continue;
                importador.globalScale *= k;
                importador.SaveAndReimport();
                corregidos++;
            }
            return corregidos;
        }

        private static long TriangulosDe(Mesh malla) {
            long t = 0;
            for (var i = 0; i < malla.subMeshCount; i++) t += malla.GetIndexCount(i) / 3;
            return t;
        }

        /// <summary>La caja que envuelve a esos renderers, vista desde un espacio cualquiera (mundo → ese espacio).</summary>
        private static bool CajaEn(Matrix4x4 alEspacio, IEnumerable<MeshRenderer> renderers, out Bounds caja) {
            caja = new Bounds();
            var hay = false;
            foreach (var r in renderers) {
                var filtro = r.GetComponent<MeshFilter>();
                if (filtro == null || filtro.sharedMesh == null) continue;
                var b = filtro.sharedMesh.bounds;
                var m = alEspacio * r.transform.localToWorldMatrix;
                for (var i = 0; i < 8; i++) {
                    var esquina = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = m.MultiplyPoint3x4(esquina);
                    if (!hay) { caja = new Bounds(p, Vector3.zero); hay = true; } else caja.Encapsulate(p);
                }
            }
            return hay;
        }

        /// <summary>Mismo centro y mismo tamaño, con un margen: reducir una malla mueve un poco sus bordes.</summary>
        private static bool Coinciden(Bounds a, Bounds b) {
            var margen = Mathf.Max(0.0005f, a.size.magnitude * 0.06f);
            return (a.center - b.center).magnitude <= margen && (a.size - b.size).magnitude <= margen * 2;
        }

        private static bool CabeEnUnaCaja(List<MeshRenderer> renderers) {
            if (renderers.Count == 0) return false;
            var mundo = renderers[0].bounds;
            foreach (var r in renderers) mundo.Encapsulate(r.bounds);
            return Mathf.Max(mundo.size.x, mundo.size.z) <= CajaHastaMetros;
        }

        private static void PonerCaja(GameObject objeto, Bounds caja) {
            var c = objeto.GetComponent<BoxCollider>();
            if (c == null) c = objeto.AddComponent<BoxCollider>();
            c.center = caja.center;
            c.size = caja.size;
        }

        // ==================================================================== 6 · importación

        [MenuItem("Nexus/Mundo 3D/6 · Ajustar importación")]
        public static void AjustarImportacion() {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Debug.Log("[Mundo3D] " + AjustarImportacionYContar());
        }

        /// <summary>
        /// Solo lo que la escena del juego usa de verdad: sus texturas (1024 como mucho, comprimidas, con mipmaps que se
        /// cargan segun hacen falta) y los modelos reducidos (comprimidos, sin copia en memoria para scripts).
        /// </summary>
        private static string AjustarImportacionYContar() {
            int texturas = 0, modelos = 0;
            foreach (var ruta in AssetDatabase.FindAssets("t:Model", new[] { Carpeta + "/Optimizado" }).Select(AssetDatabase.GUIDToAssetPath)) {
                var importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (importador == null) continue;
                if (importador.meshCompression == ModelImporterMeshCompression.Low && importador.isReadable &&
                    importador.importNormals == ModelImporterNormals.Calculate &&
                    !importador.importBlendShapes && !importador.importCameras && !importador.importLights && !importador.importAnimation) continue;
                importador.meshCompression = ModelImporterMeshCompression.Low;
                // Legibles: un MeshCollider con escala no uniforme necesita leer la malla en la build.
                importador.isReadable = true;
                // Al quitar triangulos las normales del archivo ya no valen: recalculadas y suaves, o se ve a facetas.
                importador.importNormals = ModelImporterNormals.Calculate;
                importador.normalCalculationMode = ModelImporterNormalCalculationMode.AreaAndAngleWeighted;
                importador.normalSmoothingAngle = 70;
                importador.importBlendShapes = false;
                importador.importCameras = false;
                importador.importLights = false;
                importador.importAnimation = false;
                importador.animationType = ModelImporterAnimationType.None;
                importador.SaveAndReimport();
                modelos++;
            }

            var escena = EditorSceneManager.OpenScene(Escena, OpenSceneMode.Single);
            var usadas = new HashSet<string>();
            foreach (var material in Todos<Renderer>(escena).SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct())
                foreach (var propiedad in material.GetTexturePropertyNames()) {
                    var ruta = AssetDatabase.GetAssetPath(material.GetTexture(propiedad));
                    if (ruta.StartsWith(Carpeta + "/")) usadas.Add(ruta);
                }
            foreach (var ruta in usadas) {
                var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
                if (importador == null) continue;
                if (importador.maxTextureSize <= 1024 && importador.mipmapEnabled && importador.streamingMipmaps &&
                    importador.textureCompression != TextureImporterCompression.Uncompressed) continue;
                importador.maxTextureSize = Mathf.Min(importador.maxTextureSize, 1024);
                importador.mipmapEnabled = true;
                importador.streamingMipmaps = true;
                if (importador.textureCompression == TextureImporterCompression.Uncompressed)
                    importador.textureCompression = TextureImporterCompression.Compressed;
                importador.SaveAndReimport();
                texturas++;
            }
            return $"Importación: {modelos} modelos reducidos y {texturas} texturas ajustados ({usadas.Count} texturas en uso).";
        }

        // ==================================================================== 7 · medir

        [MenuItem("Nexus/Mundo 3D/7 · Medir rendimiento")]
        public static void Medir() {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var ruta = MedirYEscribir();
            Debug.Log("[Mundo3D] Medición: " + ruta);
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(ruta);
        }

        /// <summary>El nivel tal como llego y la escena del juego, lado a lado: lo que cuesta dibujar y simular cada una.</summary>
        private static string MedirYEscribir() {
            var informe = new StringBuilder();
            informe.AppendLine("Coste de las escenas del recorrido 3D · Unity " + Application.unityVersion);
            informe.AppendLine("(lo que hay en la escena: sin culling, que es lo que decide cuanto se dibuja en cada fotograma)");
            informe.AppendLine();
            foreach (var ruta in new[] { NivelOriginal, Escena }) MedirEscena(ruta, informe);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Directory.CreateDirectory(CarpetaDeInformes);
            var salida = Path.Combine(CarpetaDeInformes, "informe-rendimiento.txt");
            File.WriteAllText(salida, informe.ToString(), new UTF8Encoding(false));
            return salida;
        }

        private static void MedirEscena(string ruta, StringBuilder informe) {
            var escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            long triangulos = 0, vertices = 0;
            int piezas = 0, conSombra = 0;
            var mallas = new HashSet<Mesh>();
            var materiales = new HashSet<Material>();
            var porObjeto = new Dictionary<string, long>();

            foreach (var r in Todos<Renderer>(escena)) {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                Mesh malla = null;
                var filtro = r.GetComponent<MeshFilter>();
                if (filtro != null) malla = filtro.sharedMesh;
                var conHuesos = r as SkinnedMeshRenderer;
                if (conHuesos != null) malla = conHuesos.sharedMesh;
                if (malla == null) continue;

                long t = 0;
                for (var i = 0; i < malla.subMeshCount; i++) t += malla.GetIndexCount(i) / 3;
                triangulos += t;
                vertices += malla.vertexCount;
                piezas += Mathf.Max(1, malla.subMeshCount);
                if (r.shadowCastingMode != ShadowCastingMode.Off) conSombra++;
                mallas.Add(malla);
                foreach (var m in r.sharedMaterials) if (m != null) materiales.Add(m);

                var raiz = PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject);
                var modelo = raiz != null ? AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromOriginalSource(raiz)) : "(objetos de la escena)";
                porObjeto[modelo] = (porObjeto.TryGetValue(modelo, out var suma) ? suma : 0) + t;
            }

            var texturas = new HashSet<Texture>();
            foreach (var m in materiales)
                foreach (var p in m.GetTexturePropertyNames()) { var tex = m.GetTexture(p); if (tex != null) texturas.Add(tex); }

            var colliders = Todos<Collider>(escena).Where(c => c.enabled && c.gameObject.activeInHierarchy).ToList();
            informe.AppendLine("== " + ruta);
            informe.AppendLine($"  Triángulos            {triangulos,12:N0}");
            informe.AppendLine($"  Vértices              {vertices,12:N0}");
            informe.AppendLine($"  Piezas que se dibujan {piezas,12:N0}   (de ellas, {conSombra:N0} renderers proyectan sombra)");
            informe.AppendLine($"  Mallas distintas      {mallas.Count,12:N0}   {Megas(mallas.Sum(m => Profiler.GetRuntimeMemorySizeLong(m)))} en memoria");
            informe.AppendLine($"  Materiales distintos  {materiales.Count,12:N0}");
            informe.AppendLine($"  Texturas distintas    {texturas.Count,12:N0}   {Megas(texturas.Sum(t => Profiler.GetRuntimeMemorySizeLong(t)))} en memoria");
            informe.AppendLine($"  Colliders             {colliders.Count,12:N0}   " +
                               string.Join(" · ", colliders.GroupBy(c => c.GetType().Name).OrderByDescending(g => g.Count()).Select(g => $"{g.Count():N0} {g.Key}")));
            informe.AppendLine($"  Luces {Todos<Light>(escena).Count()} · Animators {Todos<Animator>(escena).Count()} · " +
                               $"Rigidbodies {Todos<Rigidbody>(escena).Count()} · cámara hasta {Todos<Camera>(escena).Select(c => c.farClipPlane).DefaultIfEmpty(0).Max():N0} m");
            informe.AppendLine("  Lo que más pesa:");
            foreach (var kv in porObjeto.OrderByDescending(kv => kv.Value).Take(15))
                informe.AppendLine($"    {kv.Value,11:N0}  {kv.Key.Replace(Externo, "")}");
            informe.AppendLine();
        }

        private static string Megas(long bytes) { return (bytes / 1048576f).ToString("N0") + " MB"; }

        // ==================================================================== sin abrir el editor

        /// <summary>Todo el rendimiento de una vez, despues de que Blender haya dejado los modelos reducidos.</summary>
        public static void TodoEnLote() {
            try {
                Debug.Log("[Mundo3D] " + ConfigurarRenderYContar());
                Debug.Log("[Mundo3D] " + RehacerEscenaYContar());
                Debug.Log("[Mundo3D] " + AjustarImportacionYContar());
                Debug.Log("[Mundo3D] Medición: " + MedirYEscribir());
                AssetDatabase.SaveAssets();
                EditorApplication.Exit(0);
            } catch (Exception e) {
                Debug.LogException(e);
                EditorApplication.Exit(2);
            }
        }

        /// <summary>Cuando el equipo de 3D cambia su nivel: copia nueva, preparada, optimizada y medida.</summary>
        public static void RehacerEnLote() {
            try {
                Debug.Log("[Mundo3D] " + ConfigurarRenderYContar());
                Debug.Log("[Mundo3D] " + RehacerEscenaYContar());
                Debug.Log("[Mundo3D] " + AjustarImportacionYContar());
                Debug.Log("[Mundo3D] Medición: " + MedirYEscribir());
                AssetDatabase.SaveAssets();
                EditorApplication.Exit(0);
            } catch (Exception e) {
                Debug.LogException(e);
                EditorApplication.Exit(2);
            }
        }

        private static IEnumerable<T> Todos<T>(Scene escena) where T : Component {
            return escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true));
        }

        private static string Ruta(GameObject objeto) {
            var t = objeto.transform;
            var ruta = t.name;
            while (t.parent != null) { t = t.parent; ruta = t.name + "/" + ruta; }
            return ruta;
        }
    }
}
