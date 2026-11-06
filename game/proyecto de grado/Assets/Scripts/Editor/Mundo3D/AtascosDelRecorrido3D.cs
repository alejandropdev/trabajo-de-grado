using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nexus.EditorTools.Mundo3D {
    /// <summary>
    /// La prueba de atascos con el controlador DE VERDAD. ParedesInvisibles3D razona con capsulas sobre una cuadricula;
    /// aqui se pone un CharacterController con los mismos valores que el del jugador (radio, alto, escalon, pendiente,
    /// piel) en cada sitio libre alrededor de los muebles, y se le hace andar con Move() en ocho direcciones, como si se
    /// pulsara W A S D. Un sitio es un ATASCO si desde ahi no consigue alejarse ni medio metro en ninguna direccion.
    ///
    /// Se prueba alrededor de cada objeto con colision propia (lo que mas atrapa: barras, mostradores, taburetes,
    /// estanterias), en una corona de 1,5 m. El informe dice donde y entre que colliders se queda atascado.
    /// </summary>
    public static class AtascosDelRecorrido3D {
        private const string Escena = "Assets/Mundo3D/Escenas/Recoleccion3D.unity";
        private const float Paso = 0.25f, Corona = 1.5f, Velocidad = 3f, Dt = 1f / 60f, Segundos = 1.2f, Alejarse = 0.5f;

        [MenuItem("Nexus/Mundo 3D/10 · Probar atascos con el controlador")]
        public static void Probar() {
            var escena = EditorSceneManager.OpenScene(Escena, OpenSceneMode.Single);
            Debug.Log("[Mundo3D] " + ProbarEnEscena(escena, true).Resumen);
        }

        /// <summary>
        /// Diagnostico: todo lo que hay a menos de 4 m del objeto cuyo nombre diga la variable NEXUS_CERCA_DE (por
        /// defecto «MiniBar»): ruta, componentes, capa, y si tiene Rigidbody (lo que el jugador puede empujar).
        /// </summary>
        public static void DescribirEnLote() {
            try {
                var nombre = Environment.GetEnvironmentVariable("NEXUS_CERCA_DE");
                if (string.IsNullOrEmpty(nombre)) nombre = "MiniBar";
                var escena = EditorSceneManager.OpenScene(Escena, OpenSceneMode.Single);
                var todos = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToList();
                var centro = todos.First(t => t.name == nombre);
                var r0 = centro.GetComponentsInChildren<Renderer>().Select(r => r.bounds).ToList();
                var b = r0.Count > 0 ? r0[0] : new Bounds(centro.position, Vector3.one);
                foreach (var x in r0) b.Encapsulate(x);
                b.Expand(new Vector3(8f, 4f, 8f));
                var t = new StringBuilder();
                t.AppendLine($"Cerca de {Ruta(centro.gameObject)} · caja {b.center:F2} {b.size:F2}");
                foreach (var go in todos.Select(x => x.gameObject)) {
                    var tieneAlgo = go.GetComponent<Collider>() != null || go.GetComponent<Rigidbody>() != null;
                    if (!tieneAlgo) continue;
                    var rr = go.GetComponentInChildren<Renderer>();
                    var c = go.GetComponent<Collider>();
                    var donde = c != null ? c.bounds.center : go.transform.position;
                    if (!b.Contains(donde)) continue;
                    var comps = string.Join(", ", go.GetComponents<Component>().Where(k => !(k is Transform)).Select(k => {
                        var rb = k as Rigidbody;
                        if (rb != null) return $"Rigidbody(kin={rb.isKinematic},masa={rb.mass})";
                        var col = k as Collider;
                        if (col != null) return $"{k.GetType().Name}(on={col.enabled},trig={col.isTrigger},{col.bounds.size:F2})";
                        return k.GetType().Name;
                    }));
                    t.AppendLine($"  {Ruta(go)} · capa {LayerMask.LayerToName(go.layer)} · activo {go.activeInHierarchy} · {comps}");
                }
                var carpeta = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../tools/integracion-3d"));
                File.WriteAllText(Path.Combine(carpeta, "diagnostico-cerca.txt"), t.ToString(), new UTF8Encoding(false));
                EditorApplication.Exit(0);
            } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void ProbarEnLote() {
            try { Probar(); EditorApplication.Exit(0); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public sealed class Resultado {
            public int Probados, Atascos, Falsos;
            public readonly List<string> Sitios = new List<string>();
            public readonly Dictionary<Collider, int> Culpables = new Dictionary<Collider, int>();
            public string Resumen {
                get {
                    return $"{Probados} sitios probados con el controlador · {Atascos} atascos reales ({Culpables.Count} colliders alrededor) · " +
                           $"{Falsos} descartados (no se puede entrar andando)";
                }
            }
        }

        public static Resultado ProbarEnEscena(Scene escena, bool escribir, string soloCerca = null) {
            var res = new Resultado();
            var jugador = escena.GetRootGameObjects().Select(g => g.GetComponentInChildren<CharacterController>(true)).FirstOrDefault(c => c != null);
            if (jugador == null) throw new InvalidOperationException("No hay jugador en la escena.");

            // El controlador de prueba: una copia de los valores del jugador, en su capa. El jugador se aparta.
            var delJugador = jugador.GetComponentsInChildren<Collider>(true).Where(c => c.enabled).ToList();
            foreach (var c in delJugador) c.enabled = false;
            var puertas = AbrirPuertas(escena);
            var robot = new GameObject("~ robot de atascos") { hideFlags = HideFlags.HideAndDontSave, layer = jugador.gameObject.layer };
            SceneManager.MoveGameObjectToScene(robot, escena);
            var cc = robot.AddComponent<CharacterController>();
            cc.radius = jugador.radius; cc.height = jugador.height; cc.center = jugador.center;
            cc.stepOffset = jugador.stepOffset; cc.slopeLimit = jugador.slopeLimit; cc.skinWidth = jugador.skinWidth;
            cc.minMoveDistance = jugador.minMoveDistance;
            var mascara = 0;
            for (var l = 0; l < 32; l++) if (!Physics.GetIgnoreLayerCollision(jugador.gameObject.layer, l)) mascara |= 1 << l;

            try {
                Physics.SyncTransforms();
                // Lo que se alcanza andando desde donde aparece el jugador (sin salto: el juego ya no salta), en una
                // cuadricula de 25 cm; y desde CADA una de esas casillas, el controlador tiene que poder irse.
                foreach (var pie in AlcanzablesAndando(cc, jugador.transform.position, mascara)) {
                    res.Probados++;
                    if (PuedeSalir(cc, pie)) continue;
                    // ¿Se llega de verdad? El robot sale de las casillas de alrededor (a 0,5-0,75 m) y anda hacia aqui; si
                    // en alguna de esas entradas termina en un sitio del que no sale, es una trampa real. Si nunca logra
                    // meterse, la casilla era un falso positivo de la cuadricula (la piel del controlador no cabe).
                    Vector3 dondeQueda;
                    if (!SeEntraYNoSeSale(cc, pie, out dondeQueda)) { res.Falsos++; continue; }
                    res.Atascos++;
                    if (res.Sitios.Count < 80) res.Sitios.Add($"({pie.x:F2}, {pie.y:F2}, {pie.z:F2}) · se queda en ({dondeQueda.x:F2}, {dondeQueda.z:F2})" +
                                                              Perfil(pie, mascara));
                    foreach (var col in Physics.OverlapCapsule(pie + Vector3.up * (cc.stepOffset * 0.5f), pie + Vector3.up * (cc.height - cc.radius),
                                                               cc.radius + 0.2f, mascara, QueryTriggerInteraction.Ignore)) {
                        if (col == cc) continue;
                        int n; res.Culpables.TryGetValue(col, out n); res.Culpables[col] = n + 1;
                    }
                }
                if (escribir) Escribir(res);
            } finally {
                UnityEngine.Object.DestroyImmediate(robot);
                foreach (var c in delJugador) if (c != null) c.enabled = true;
                foreach (var p in puertas) if (p.Key != null) p.Key.localPosition = p.Value;
                Physics.SyncTransforms();
            }
            return res;
        }

        /// <summary>
        /// El salto del juego (NThirdPersonPlayer: velocidad vertical 3,0 al pulsar, gravedad 9,8, corriendo a 5 m/s) en
        /// ocho direcciones. Si alguno aterriza en un sitio del que andando no se puede salir, devuelve ese sitio.
        /// </summary>
        private static bool CaeAtascado(CharacterController cc, Vector3 pie, out Vector3 donde) {
            donde = pie;
            for (var i = 0; i < 8; i++) {
                var dir = Quaternion.Euler(0, i * 45f, 0) * Vector3.forward;
                cc.enabled = false;
                cc.transform.position = pie;
                cc.enabled = true;
                Physics.SyncTransforms();
                var vy = FuerzaDeSalto;
                var enElAire = 0f;
                for (var t = 0f; t < 1.5f; t += Dt) {
                    vy -= 9.8f * Dt;
                    cc.Move((dir * 5f + Vector3.up * vy) * Dt);
                    enElAire += Dt;
                    if (enElAire > 0.1f && cc.isGrounded) break;
                }
                var aterrizaje = cc.transform.position;
                if (!PuedeSalir(cc, aterrizaje)) { donde = aterrizaje; return true; }
            }
            return false;
        }

        private const float FuerzaDeSalto = 3.0f;   // AjustesDelRecorrido.FuerzaDeSalto

        /// <summary>
        /// Las casillas a las que se llega andando: suelo debajo (sube hasta el escalon, baja hasta 1,2 m, pendiente de
        /// 45° como mucho) y la capsula del controlador cabe sin tocar nada.
        /// </summary>
        private static List<Vector3> AlcanzablesAndando(CharacterController cc, Vector3 inicio, int mascara) {
            var res = new List<Vector3>();
            var vistas = new HashSet<long>();
            var cola = new Queue<Vector3>();
            RaycastHit hit;
            if (!Physics.Raycast(inicio + Vector3.up, Vector3.down, out hit, 4f, mascara, QueryTriggerInteraction.Ignore)) return res;
            var p0 = new Vector3(Mathf.Round(hit.point.x / Paso) * Paso, hit.point.y, Mathf.Round(hit.point.z / Paso) * Paso);
            cola.Enqueue(p0);
            vistas.Add(ClaveDe(p0));
            var dirs = new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            while (cola.Count > 0 && res.Count < 400000) {
                var c = cola.Dequeue();
                res.Add(c + Vector3.up * (cc.skinWidth + 0.01f));
                foreach (var d in dirs) {
                    var arriba = c + d * Paso + Vector3.up * (cc.stepOffset + 0.05f);
                    if (!Physics.Raycast(arriba, Vector3.down, out hit, cc.stepOffset + 1.25f, mascara, QueryTriggerInteraction.Ignore)) continue;
                    if (hit.normal.y < Mathf.Cos(cc.slopeLimit * Mathf.Deg2Rad)) continue;
                    var n = new Vector3(arriba.x, hit.point.y, arriba.z);
                    var k = ClaveDe(n);
                    if (vistas.Contains(k)) continue;
                    var r = cc.radius * 0.95f;
                    if (Physics.CheckCapsule(n + Vector3.up * (cc.stepOffset + r), n + Vector3.up * (cc.height - r), r, mascara, QueryTriggerInteraction.Ignore)) continue;
                    vistas.Add(k);
                    cola.Enqueue(n);
                }
            }
            return res;
        }

        private static long ClaveDe(Vector3 p) {
            return ((long)(Mathf.RoundToInt(p.x / Paso) + 100000) << 40) ^ ((long)(Mathf.RoundToInt(p.z / Paso) + 100000) << 16) ^ (long)(Mathf.FloorToInt(p.y / 1.5f) + 1000);
        }

        /// <summary>
        /// Desde ocho puntos alrededor (a 0,5 y 0,75 m) el robot anda 1,5 s hacia 'pie'. Si alguna vez acaba a menos de
        /// 20 cm de el y desde ahi no puede salir, es una trampa de verdad: 'dondeQueda' dice donde se quedo.
        /// </summary>
        private static bool SeEntraYNoSeSale(CharacterController cc, Vector3 pie, out Vector3 dondeQueda) {
            dondeQueda = pie;
            foreach (var distancia in new[] { 0.5f, 0.75f })
                for (var i = 0; i < 8; i++) {
                    cc.enabled = false;   // el robot no puede taparse a si mismo en estas comprobaciones
                    var desde = pie + Quaternion.Euler(0, i * 45f, 0) * Vector3.forward * distancia + Vector3.up * 0.3f;
                    RaycastHit suelo;
                    if (!Physics.Raycast(desde, Vector3.down, out suelo, 1.5f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    var inicio = suelo.point + Vector3.up * (cc.skinWidth + 0.01f);
                    var r = cc.radius;
                    if (Physics.CheckCapsule(inicio + Vector3.up * (cc.stepOffset + r), inicio + Vector3.up * (cc.height - r), r, ~0,
                                             QueryTriggerInteraction.Ignore)) continue;
                    cc.enabled = false;
                    cc.transform.position = inicio;
                    cc.enabled = true;
                    Physics.SyncTransforms();
                    var caida = 0f;
                    for (var t = 0f; t < 1.5f; t += Dt) {
                        var hacia = pie - cc.transform.position;
                        hacia.y = 0;
                        if (hacia.magnitude < 0.05f) break;
                        caida = cc.isGrounded ? -1f : caida - 9.8f * Dt;
                        cc.Move((hacia.normalized * Mathf.Min(Velocidad, hacia.magnitude / Dt) + Vector3.up * caida) * Dt);
                    }
                    var llego = cc.transform.position;
                    var h = llego - pie;
                    h.y = 0;
                    if (h.magnitude > 0.2f) continue;
                    if (!PuedeSalir(cc, llego)) { dondeQueda = llego; return true; }
                }
            return false;
        }

        /// <summary>A que distancia hay algo en ocho direcciones, a cuatro alturas: la forma del hueco donde se atasca.</summary>
        private static string Perfil(Vector3 pie, int mascara) {
            var t = new StringBuilder();
            foreach (var alto in new[] { 0.1f, 0.4f, 0.9f, 1.6f }) {
                t.Append($"{Environment.NewLine}      a {alto:F1} m:");
                for (var i = 0; i < 8; i++) {
                    var dir = Quaternion.Euler(0, i * 45f, 0) * Vector3.forward;
                    RaycastHit hit;
                    var d = Physics.Raycast(pie + Vector3.up * alto, dir, out hit, 1.5f, mascara, QueryTriggerInteraction.Ignore) ? hit.distance : 9f;
                    t.Append(d > 8f ? "   ·  " : $" {d:F2}{(hit.collider != null ? hit.collider.name.Substring(0, Math.Min(3, hit.collider.name.Length)) : "")}");
                }
            }
            return t.ToString();
        }

        /// <summary>Desde 'pie', ¿se aleja medio metro en alguna de las ocho direcciones andando 1,2 s?</summary>
        private static bool PuedeSalir(CharacterController cc, Vector3 pie) {
            for (var i = 0; i < 8; i++) {
                var dir = Quaternion.Euler(0, i * 45f, 0) * Vector3.forward;
                cc.enabled = false;
                cc.transform.position = pie;
                cc.enabled = true;
                Physics.SyncTransforms();
                var caida = 0f;
                for (var t = 0f; t < Segundos; t += Dt) {
                    caida = cc.isGrounded ? -1f : caida - 9.8f * Dt;
                    cc.Move((dir * Velocidad + Vector3.up * caida) * Dt);
                }
                var h = cc.transform.position - pie;
                h.y = 0;
                if (h.magnitude >= Alejarse) return true;
            }
            return false;
        }

        private static bool EsSuelo(Collider c) {
            var b = c.bounds;
            return b.size.y < 0.15f && (b.size.x > 2f || b.size.z > 2f);
        }

        private static List<KeyValuePair<Transform, Vector3>> AbrirPuertas(Scene escena) {
            var movidas = new List<KeyValuePair<Transform, Vector3>>();
            foreach (var mb in escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true))) {
                if (mb == null || mb.GetType().Name != "DoorController") continue;
                var so = new SerializedObject(mb);
                var parte = so.FindProperty("movingPart");
                var t = parte != null ? parte.objectReferenceValue as Transform : null;
                var d = so.FindProperty("openOffset");
                if (t == null || d == null) continue;
                movidas.Add(new KeyValuePair<Transform, Vector3>(t, t.localPosition));
                t.localPosition += d.vector3Value;
            }
            return movidas;
        }

        private static string Ruta(GameObject go) {
            var partes = new List<string>();
            for (var t = go.transform; t != null; t = t.parent) partes.Insert(0, t.name);
            return string.Join("/", partes);
        }

        private static void Escribir(Resultado res) {
            var carpeta = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../tools/integracion-3d"));
            Directory.CreateDirectory(carpeta);
            var t = new StringBuilder();
            t.AppendLine("Atascos del recorrido 3D (con el CharacterController) · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            t.AppendLine(res.Resumen);
            t.AppendLine();
            t.AppendLine("COLLIDERS ALREDEDOR DE LOS ATASCOS (veces · tipo · convexo · ruta)");
            foreach (var kv in res.Culpables.OrderByDescending(kv => kv.Value)) {
                if (kv.Key == null) continue;
                var mc = kv.Key as MeshCollider;
                t.AppendLine($"{kv.Value,6} · {kv.Key.GetType().Name,-15} · {(mc != null ? (mc.convex ? "convexo" : "no convexo") : "-"),-10} · " +
                             $"{Ruta(kv.Key.gameObject)} · caja {kv.Key.bounds.size:F2}");
            }
            t.AppendLine();
            t.AppendLine("SITIOS");
            foreach (var s in res.Sitios) t.AppendLine("  " + s);
            File.WriteAllText(Path.Combine(carpeta, "informe-atascos.txt"), t.ToString(), new UTF8Encoding(false));
        }
    }
}
