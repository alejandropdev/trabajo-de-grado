#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Nexus.Mundo3D {
    /// <summary>
    /// SOLO EN EL EDITOR (2026-11-14, auditoria de arte). Lo pone en marcha «Nexus > Mundo 3D > Capturar recorrido»:
    /// con Recoleccion3D abierta en Play, aplica la misma ambientacion que el juego (AjustesDelRecorrido.Ambientar y el
    /// nivel de calidad «Mundo 3D»), apaga las camaras de la escena y fotografia con una camara propia: una vista
    /// general, dos por zona (a la altura de los ojos y en tres cuartos) y primeros planos de un NPC, un CV y un
    /// suministro si los encuentra. No cambia nada del nivel: todo es de esta sesion de Play.
    /// Todo va a Library/capturas-3d/.
    /// </summary>
    public sealed class CapturadorDelRecorrido3D : MonoBehaviour {
        public static int Fallos { get; private set; }
        public static string Carpeta { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "capturas-3d")); } }

        private readonly StringBuilder _informe = new StringBuilder();
        private Camera _camara;
        private RenderTexture _rt;

        private IEnumerator Start() {
            Fallos = 0;
            Directory.CreateDirectory(Carpeta);
            foreach (var viejo in Directory.GetFiles(Carpeta, "*.png")) File.Delete(viejo);
            yield return null; yield return null;

            var escena = SceneManager.GetActiveScene();
            var calidad = Array.IndexOf(QualitySettings.names, PantallaDeRecoleccion3D.NivelDeCalidad);
            if (calidad >= 0) QualitySettings.SetQualityLevel(calidad, true);
            AjustesDelRecorrido.Ambientar(escena);

            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) c.enabled = false;
            _rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            _camara = new GameObject("Camara de capturas 3D").AddComponent<Camera>();
            _camara.targetTexture = _rt;
            _camara.fieldOfView = 60;
            _camara.nearClipPlane = 0.05f;
            _camara.farClipPlane = 400;
            var datos = _camara.GetUniversalAdditionalCameraData();
            datos.renderPostProcessing = true;
            Anotar("escena: " + escena.name + " · calidad: " + QualitySettings.names[QualitySettings.GetQualityLevel()]);
            yield return new WaitForSecondsRealtime(2f);

            var todo = Limites(escena.GetRootGameObjects().Select(g => g.transform));
            if (todo.size == Vector3.zero) { Anotar("ERROR: la escena no tiene nada que dibujar"); Fallos++; Terminar(); yield break; }
            yield return Foto("00_general_cenital", todo.center + new Vector3(0, todo.extents.y + Mathf.Max(todo.extents.x, todo.extents.z) * 1.1f, -todo.extents.z * 0.6f), todo.center);
            yield return Foto("01_general_tres_cuartos", todo.center + new Vector3(-todo.extents.x * 0.9f, todo.extents.y + 25f, -todo.extents.z * 0.9f), todo.center);

            var zonas = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Where(t => t.name.StartsWith("Zona", StringComparison.Ordinal) && !t.name.StartsWith("ZonaSpawn", StringComparison.Ordinal))
                .GroupBy(t => t.name).Select(g => g.First()).ToList();
            var n = 2;
            foreach (var zona in zonas) {
                // Las zonas son marcadores (colliders y puntos de aparicion), no geometria: su extension sale de sus
                // colliders o, si no tienen, de la posicion de sus hijos.
                var b = LimitesDeZona(zona);
                if (b.size.x < 1f && b.size.z < 1f) { Anotar("zona sin extension: " + zona.name + " en " + zona.position.ToString("0.0")); b = new Bounds(zona.position, new Vector3(12, 3, 12)); }
                var suelo = b.min.y;
                var ojo = new Vector3(b.center.x - b.extents.x * 0.7f, suelo + 1.6f, b.center.z - b.extents.z * 0.7f);
                yield return Foto((n++).ToString("00") + "_" + zona.name + "_ojos", ojo, new Vector3(b.center.x, suelo + 1.3f, b.center.z));
                var alto = new Vector3(b.center.x - b.extents.x * 0.8f, suelo + Mathf.Max(8f, b.extents.magnitude * 0.6f), b.center.z - b.extents.z * 0.8f);
                yield return Foto((n++).ToString("00") + "_" + zona.name + "_tres_cuartos", alto, new Vector3(b.center.x, suelo, b.center.z));
            }

            // Los recolectables los reparte el spawner al empezar: se espera un poco y se fotografian los primeros de
            // cada nombre distinto (CV, pizza, cafe, tarjeta...).
            yield return new WaitForSecondsRealtime(1.5f);
            var recolectables = FindObjectsByType<global::Mundo3D.Coleccionables.CollectiblePickup>(FindObjectsSortMode.None)
                .GroupBy(p => NombreBase(p.name)).Select(g => g.First()).Take(6).ToList();
            if (recolectables.Count == 0) Anotar("primer plano: no hay recolectables en la escena");
            foreach (var p in recolectables) {
                var rs = p.GetComponentsInChildren<Renderer>(false);
                if (rs.Length == 0) continue;
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                var dist = Mathf.Max(1.0f, b.extents.magnitude * 3f);
                yield return Foto((n++).ToString("00") + "_recolectable_" + NombreBase(p.name), b.center + new Vector3(0.6f, 0.45f, -1f).normalized * dist, b.center);
            }

            foreach (var clave in new[] { "Npc", "ybot", "PanelLuz" }) {
                var objeto = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(false))
                    .FirstOrDefault(r => r.name.IndexOf(clave, StringComparison.OrdinalIgnoreCase) >= 0
                                      || r.transform.root.name.IndexOf(clave, StringComparison.OrdinalIgnoreCase) >= 0);
                if (objeto == null) { Anotar("primer plano: no hay ningun «" + clave + "» en la escena"); continue; }
                var b = objeto.bounds;
                var dist = Mathf.Max(1.2f, b.extents.magnitude * 2.6f);
                var desde = b.center + new Vector3(0.6f, 0.35f, -1f).normalized * dist;
                yield return Foto((n++).ToString("00") + "_primer_plano_" + clave, desde, b.center);
            }
            Terminar();
        }

        private static Bounds Limites(IEnumerable<Transform> raices) {
            var b = new Bounds();
            var hay = false;
            foreach (var raiz in raices)
                foreach (var r in raiz.GetComponentsInChildren<Renderer>(false)) {
                    if (r is ParticleSystemRenderer) continue;
                    if (!hay) { b = r.bounds; hay = true; } else b.Encapsulate(r.bounds);
                }
            return b;
        }

        private static string NombreBase(string nombre) { var i = nombre.IndexOf('('); return (i > 0 ? nombre.Substring(0, i) : nombre).Trim(); }

        private static Bounds LimitesDeZona(Transform zona) {
            var b = new Bounds(zona.position, Vector3.zero);
            foreach (var c in zona.GetComponentsInChildren<Collider>(true)) b.Encapsulate(c.bounds);
            foreach (var t in zona.GetComponentsInChildren<Transform>(true)) b.Encapsulate(t.position);
            return b;
        }

        private IEnumerator Foto(string nombre, Vector3 desde, Vector3 hacia) {
            _camara.transform.position = desde;
            _camara.transform.LookAt(hacia);
            yield return null;
            _camara.Render();   // la primera vuelta tras mover la camara puede salir sin sombras: se descarta
            _camara.Render();
            var antes = RenderTexture.active;
            RenderTexture.active = _rt;
            var tex = new Texture2D(_rt.width, _rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = antes;
            File.WriteAllBytes(Path.Combine(Carpeta, nombre + ".png"), tex.EncodeToPNG());
            Destroy(tex);
            Anotar("ok " + nombre + " · desde " + desde.ToString("0.0") + " hacia " + hacia.ToString("0.0"));
        }

        private void Terminar() {
            Anotar("FALLOS: " + Fallos);
            File.WriteAllText(Path.Combine(Carpeta, "informe.txt"), _informe.ToString());
            Debug.Log("[Capturas 3D] " + Carpeta + "\n" + _informe);
            UnityEditor.EditorApplication.ExitPlaymode();
        }

        private void Anotar(string linea) { _informe.AppendLine(linea); }
    }
}
#endif
