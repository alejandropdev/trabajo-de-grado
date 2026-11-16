using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nexus.EditorTools.Mundo3D {
    /// <summary>
    /// Inventario de los materiales del recorrido 3D (2026-11-16, direccion de arte). Abre Recoleccion3D en el editor
    /// (sin Play, sin guardar nada) y lista cada material que se usa: shader, textura, color, si emite luz, si es de un
    /// personaje y cuantos renderers lo llevan, con su matiz/saturacion/valor. Sirve para decidir como unificar las
    /// familias de materiales sin tocar los originales. Salida: Library/inventario-materiales-3d.txt.
    ///
    ///   Nexus > Mundo 3D > Inventario de materiales
    ///   Unity.exe -batchmode -quit -projectPath "game/proyecto de grado" -executeMethod Nexus.EditorTools.Mundo3D.InventarioDeMateriales3D.Generar -logFile inventario.log
    /// </summary>
    public static class InventarioDeMateriales3D {
        private const string Escena = "Assets/Mundo3D/Escenas/Recoleccion3D.unity";

        private sealed class Ficha {
            public Material Mat; public int Renderers; public int Personajes;
            public string UsadoEn = "";
        }

        [MenuItem("Nexus/Mundo 3D/Inventario de materiales")]
        public static void Generar() {
            EditorSceneManager.OpenScene(Escena, OpenSceneMode.Single);
            var fichas = new Dictionary<Material, Ficha>();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                var personaje = r is SkinnedMeshRenderer;
                foreach (var m in r.sharedMaterials) {
                    if (m == null) continue;
                    Ficha f;
                    if (!fichas.TryGetValue(m, out f)) { f = new Ficha { Mat = m }; fichas[m] = f; }
                    f.Renderers++;
                    if (personaje) f.Personajes++;
                    if (f.UsadoEn.Length < 60 && !f.UsadoEn.Contains(r.name)) f.UsadoEn += r.name + "; ";
                }
            }
            var sb = new StringBuilder();
            sb.AppendLine("materiales distintos: " + fichas.Count);
            sb.AppendLine("nombre | shader | textura | H S V | emision | renderers (personaje) | usado en");
            foreach (var f in fichas.Values.OrderByDescending(x => x.Renderers)) {
                var m = f.Mat;
                var color = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                float h, s, v; Color.RGBToHSV(color, out h, out s, out v);
                var tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                var emite = m.IsKeywordEnabled("_EMISSION") && m.HasProperty("_EmissionColor") && m.GetColor("_EmissionColor").maxColorComponent > 0.05f;
                sb.AppendLine(string.Format("{0} | {1} | {2} | {3:0} {4:0.00} {5:0.00} | {6} | {7} ({8}) | {9}",
                    m.name, m.shader != null ? m.shader.name : "?", tex != null ? tex.name : "-", h * 360, s, v, emite ? "EMITE" : "-", f.Renderers, f.Personajes, f.UsadoEn));
            }
            var ruta = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library", "inventario-materiales-3d.txt");
            File.WriteAllText(ruta, sb.ToString());
            Debug.Log("[Inventario 3D] " + fichas.Count + " materiales -> " + ruta);
        }
    }
}
