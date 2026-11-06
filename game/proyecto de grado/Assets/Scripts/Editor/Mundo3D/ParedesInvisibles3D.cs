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
    /// Paredes invisibles del recorrido: sitios donde el jugador choca y no se ve nada, y su contrario, paredes que se
    /// ven y se atraviesan. Se miden comparando dos mundos fisicos sobre la misma escena:
    ///   · el VISUAL: un MeshCollider por cada malla que se ve (capa 31), es decir, «lo que el jugador cree que hay»;
    ///   · el REAL: los colliders que de verdad le paran (sin triggers ni el propio jugador).
    /// Desde donde aparece el jugador se recorre, en una cuadricula, todo lo que en el mundo visual se puede andar
    /// (suelo debajo, escalones de hasta 45 cm, la capsula cabe con holgura). En cada casilla andable se prueba la
    /// capsula contra el mundo real: si choca, ahi hay una pared invisible, y se apunta quien. En el borde de lo andable,
    /// donde empieza algo alto que se ve, se prueba al reves: si el mundo real deja pasar, es una pared atravesable.
    ///
    /// «Auditar» solo mide y escribe el informe y el mapa. «Quitar» arregla lo que la auditoria señala en la escena del
    /// juego (nunca en el nivel original del equipo 3D) y vuelve a medir, hasta que no quede nada o deje de mejorar.
    /// Rendimiento3D lo encadena al rehacer la escena, para que una optimizacion futura no las vuelva a meter.
    /// </summary>
    public static class ParedesInvisibles3D {
        private const string Escena = "Assets/Mundo3D/Escenas/Recoleccion3D.unity";
        private const int CapaVisual = 31;

        // El jugador: CharacterController de radio 0,2 m y alto 1,81 m (NThirdPerson del nivel).
        private const float Radio = 0.2f, Alto = 1.81f;
        /// <summary>Holgura: algo real que sobresale menos que esto de lo que se ve no cuenta como pared invisible.</summary>
        private const float Holgura = 0.12f;
        // Escalon = stepOffset del CharacterController del nivel (0,3 m): lo que sube sin saltar. Antes se usaba 0,45 y
        // la auditoria no veia lo bajo (patas, reposapies de taburetes), que es justo donde el jugador se atascaba.
        private const float Paso = 0.25f, Escalon = 0.3f, Bajada = 1.2f, PendienteMinima = 0.7f;
        /// <summary>Por debajo de esta altura, lo que se ve es un mueble o un adorno: atravesarlo no saca del mapa.</summary>
        private const float AltoDeUnaPared = 1.5f;
        private const int MaximoDeCasillas = 600000;

        private static string Carpeta {
            get { return Path.GetFullPath(Path.Combine(Application.dataPath, "../../../tools/integracion-3d")); }
        }

        [MenuItem("Nexus/Mundo 3D/8 · Auditar paredes invisibles")]
        public static void Auditar() {
            var escena = EditorSceneManager.OpenScene(Escena, OpenSceneMode.Single);
            Debug.Log("[Mundo3D] " + Medir(escena, true).Resumen);
        }

        [MenuItem("Nexus/Mundo 3D/9 · Quitar paredes invisibles")]
        public static void Quitar() {
            var escena = EditorSceneManager.OpenScene(Escena, OpenSceneMode.Single);
            Debug.Log("[Mundo3D] " + QuitarEnEscena(escena));
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }

        public static void AuditarEnLote() { EnLote(Auditar); }
        public static void QuitarEnLote() { EnLote(Quitar); }

        private static void EnLote(Action accion) {
            try {
                accion();
                EditorApplication.Exit(0);
            } catch (Exception e) {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        // ==================================================================== arreglar

        /// <summary>
        /// Arregla lo que mide la auditoria, en rondas, sobre la escena abierta (no la guarda: lo hace quien llama).
        /// Devuelve el resumen de antes y despues.
        /// </summary>
        public static string QuitarEnEscena(Scene escena) {
            var antes = Medir(escena, false);
            var informe = new StringBuilder();
            informe.AppendLine("Antes: " + antes.Resumen);
            var actual = antes;
            for (var ronda = 1; ronda <= 6 && !actual.Limpio; ronda++) {
                var cambios = Arreglar(actual, informe);
                var despues = Medir(escena, false);
                informe.AppendLine($"Ronda {ronda}: {cambios} cambios · {despues.Resumen}");
                var mejoro = despues.Total < actual.Total;
                actual = despues;
                if (cambios == 0 || !mejoro) break;
            }
            // El informe y el mapa del estado final.
            actual = Medir(escena, true, informe.ToString());
            return "Paredes invisibles: " + antes.Resumen + " → " + actual.Resumen;
        }

        private static int Arreglar(Medida m, StringBuilder informe) {
            var cambios = 0;
            foreach (var culpable in m.Invisibles.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToList()) {
                if (culpable == null || !culpable.enabled) continue;
                var go = culpable.gameObject;
                var ruta = Ruta(go);
                var tipo = culpable.GetType().Name;
                var visibles = Visibles(go);
                if (visibles.Count == 0) {
                    // Nada de lo que cuelga de el se ve: un bloqueador invisible. Se apaga (no se borra: se puede revisar).
                    culpable.enabled = false;
                    informe.AppendLine($"  apagado (no se ve nada): {ruta} · {tipo}");
                    cambios++;
                    continue;
                }
                var malla = culpable as MeshCollider;
                var filtro = go.GetComponent<MeshFilter>();
                if (malla != null && filtro != null && filtro.sharedMesh != null) {
                    if (malla.sharedMesh != filtro.sharedMesh) {
                        malla.sharedMesh = filtro.sharedMesh;
                        informe.AppendLine($"  malla de colisión = la visible: {ruta}");
                        cambios++;
                    } else if (malla.convex) {
                        // Convexa, la colision rellena los huecos de la forma (un arco, una U): se deja con su forma.
                        malla.convex = false;
                        informe.AppendLine($"  colisión no convexa (tapaba sus huecos): {ruta}");
                        cambios++;
                    }
                    // Si ya es la malla que se ve, lo que choca se ve: la casilla la marco otra pieza o la holgura.
                    continue;
                }
                // Una caja (o una capsula, o un MeshCollider sin malla propia) envolviendo un mueble: su forma real,
                // en la capa del collider que sustituye (la de sus mallas puede ser una que el jugador no toca).
                var capa = go.layer;
                UnityEngine.Object.DestroyImmediate(culpable);
                foreach (var r in visibles) PonerForma(r, capa);
                informe.AppendLine($"  forma real en vez de {tipo}: {ruta} ({visibles.Count} mallas)");
                cambios++;
            }
            foreach (var r in m.Atravesables.Keys.ToList()) {
                if (r == null) continue;
                var capa = Physics.GetIgnoreLayerCollision(m.CapaDelJugador, r.gameObject.layer) ? 0 : r.gameObject.layer;
                if (!PonerForma(r, capa)) continue;
                informe.AppendLine($"  colisión para una pared que se atravesaba: {Ruta(r.gameObject)}");
                cambios++;
            }
            return cambios;
        }

        private const string NombreDeLaForma = "Colision (forma real)";

        /// <summary>
        /// La colision con la forma exacta de una malla visible: un hijo con transform identidad (hereda su matriz) y la
        /// capa que se pida. Si ya tiene uno de estos hijos, se le pone esa capa. Devuelve si hizo algo.
        /// </summary>
        private static bool PonerForma(MeshRenderer r, int capa) {
            var f = r.GetComponent<MeshFilter>();
            if (f == null || f.sharedMesh == null) return false;
            var hijo = r.transform.Find(NombreDeLaForma);
            if (hijo != null) {
                if (hijo.gameObject.layer == capa) return false;
                hijo.gameObject.layer = capa;
                return true;
            }
            var propio = r.GetComponent<MeshCollider>();
            if (propio != null && propio.enabled && propio.sharedMesh == f.sharedMesh && r.gameObject.layer == capa) return false;
            var go = new GameObject(NombreDeLaForma) { layer = capa };
            go.transform.SetParent(r.transform, false);
            go.AddComponent<MeshCollider>().sharedMesh = f.sharedMesh;
            return true;
        }

        // ==================================================================== medir

        private sealed class Medida {
            public int Andables, CasillasInvisibles, CasillasAtravesables, Huecos, CapaDelJugador;
            /// <summary>Casillas a las que se llega de verdad pero desde las que ya no se puede volver: trampas.</summary>
            public int Trampas, Alcanzables;
            public readonly Dictionary<Collider, int> Encierran = new Dictionary<Collider, int>();
            public readonly List<Vector3> SitiosDeTrampa = new List<Vector3>();
            public readonly Dictionary<Collider, int> Invisibles = new Dictionary<Collider, int>();
            public readonly Dictionary<MeshRenderer, int> Atravesables = new Dictionary<MeshRenderer, int>();
            public int Total { get { return CasillasInvisibles + CasillasAtravesables + Huecos + Trampas; } }
            public bool Limpio { get { return Total == 0; } }
            public string Resumen {
                get {
                    return $"{Andables} casillas andables · {CasillasInvisibles} con pared invisible ({Invisibles.Count} colliders) · " +
                           $"{CasillasAtravesables} junto a paredes atravesables ({Atravesables.Count}) · {Huecos} sin suelo real · " +
                           $"{Trampas} trampas (se entra y no se sale) de {Alcanzables} alcanzables";
                }
            }
        }

        private struct Casilla { public int X, Z; public float Y; }

        private static Medida Medir(Scene escena, bool escribir, string historia = null) {
            var m = new Medida();
            var jugador = BuscarJugador(escena);
            if (jugador == null) throw new InvalidOperationException("No hay jugador (CharacterController) en " + Escena);
            m.CapaDelJugador = jugador.gameObject.layer;

            // El jugador no cuenta como obstaculo: se apagan sus colliders mientras se mide.
            var delJugador = jugador.GetComponentsInChildren<Collider>(true).Where(c => c.enabled).ToList();
            foreach (var c in delJugador) c.enabled = false;
            // Las puertas, abiertas: cerradas se ven y cortan el paso, y lo de detras quedaria sin medir.
            var puertas = AbrirPuertas(escena);
            var raizVisual = ConstruirMundoVisual(escena, jugador.transform);
            Physics.SyncTransforms();

            var visual = 1 << CapaVisual;
            var real = MascaraReal(jugador.gameObject.layer);
            try {
                var andables = new Dictionary<long, Casilla>();
                var porVer = new Queue<Casilla>();
                Casilla inicio;
                if (!Suelo(jugador.transform.position + Vector3.up * 1f, 3f, visual, out inicio))
                    throw new InvalidOperationException("No hay suelo visible debajo del jugador.");
                andables[Clave(inicio)] = inicio;
                porVer.Enqueue(inicio);
                var dirs = new[] { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } };
                var invisibles = new HashSet<long>();
                var bordes = new Dictionary<long, Vector3>();

                while (porVer.Count > 0 && andables.Count < MaximoDeCasillas) {
                    var c = porVer.Dequeue();
                    foreach (var d in dirs) {
                        var arriba = new Vector3((c.X + d[0]) * Paso, c.Y + Escalon + 0.05f, (c.Z + d[1]) * Paso);
                        Casilla n;
                        if (!Suelo(arriba, Escalon + Bajada, visual, out n)) continue;
                        var clave = Clave(n);
                        if (andables.ContainsKey(clave) || bordes.ContainsKey(clave)) continue;
                        if (Choca(n, Radio + Holgura, visual)) {
                            // Algo que se ve corta el paso. Si con la capsula justa tambien choca con lo que se ve pero
                            // no con lo real, ahi hay algo visible sin colision: puede ser una pared atravesable.
                            if (Choca(n, Radio, visual) && !Choca(n, Radio, real)) bordes[clave] = Centro(n);
                            continue;
                        }
                        andables[clave] = n;
                        porVer.Enqueue(n);
                    }
                }
                m.Andables = andables.Count;

                foreach (var kv in andables) {
                    var c = kv.Value;
                    var p1 = new Vector3(c.X * Paso, c.Y + Escalon + Radio, c.Z * Paso);
                    var p2 = new Vector3(c.X * Paso, c.Y + Alto - Radio, c.Z * Paso);
                    var choca = Physics.OverlapCapsule(p1, p2, Radio, real, QueryTriggerInteraction.Ignore);
                    if (choca.Length > 0) {
                        m.CasillasInvisibles++;
                        invisibles.Add(kv.Key);
                        foreach (var col in choca) { int k; m.Invisibles.TryGetValue(col, out k); m.Invisibles[col] = k + 1; }
                    }
                    // Suelo que se ve pero no sostiene: por ahi se cae uno del mapa.
                    if (!Physics.Raycast(new Vector3(c.X * Paso, c.Y + 0.5f, c.Z * Paso), Vector3.down, 1.2f, real, QueryTriggerInteraction.Ignore))
                        m.Huecos++;
                }
                var atravesables = new HashSet<long>();
                foreach (var b in bordes) {
                    var p = b.Value;
                    var hits = Physics.OverlapCapsule(p + Vector3.up * (Escalon + Radio), p + Vector3.up * (Alto - Radio), Radio, visual,
                                                      QueryTriggerInteraction.Collide);
                    foreach (var h in hits) {
                        var r = Origen(h);
                        if (r == null || r.bounds.size.y < AltoDeUnaPared) continue;
                        int k; m.Atravesables.TryGetValue(r, out k); m.Atravesables[r] = k + 1;
                        m.CasillasAtravesables++;
                        atravesables.Add(b.Key);
                        break;
                    }
                }

                BuscarTrampas(m, inicio, real);

                if (escribir) Escribir(m, andables, invisibles, bordes.Where(b => atravesables.Contains(b.Key)).Select(b => b.Value).ToList(), historia);
            } finally {
                foreach (var g in raizVisual) if (g != null) UnityEngine.Object.DestroyImmediate(g);
                foreach (var c in delJugador) if (c != null) c.enabled = true;
                foreach (var p in puertas) if (p.Key != null) p.Key.localPosition = p.Value;
                Physics.SyncTransforms();
            }
            return m;
        }

        /// <summary>
        /// Lleva la parte movil de cada DoorController a su posicion de abierta (la de Awake: cerrada + openOffset) y
        /// devuelve donde estaba cada una, para dejarlas como estaban al terminar.
        /// </summary>
        private static List<KeyValuePair<Transform, Vector3>> AbrirPuertas(Scene escena) {
            var movidas = new List<KeyValuePair<Transform, Vector3>>();
            foreach (var mb in Todos<MonoBehaviour>(escena)) {
                if (mb == null || mb.GetType().Name != "DoorController") continue;
                var so = new SerializedObject(mb);
                var parte = so.FindProperty("movingPart");
                var desplazamiento = so.FindProperty("openOffset");
                var t = parte != null ? parte.objectReferenceValue as Transform : null;
                if (t == null || desplazamiento == null) continue;
                var abrirAlEmpezar = so.FindProperty("startOpen");
                if (abrirAlEmpezar != null && abrirAlEmpezar.boolValue) continue;   // ya esta abierta
                movidas.Add(new KeyValuePair<Transform, Vector3>(t, t.localPosition));
                t.localPosition += desplazamiento.vector3Value;
            }
            return movidas;
        }

        /// <summary>
        /// Recorre el mundo REAL como lo haria el CharacterController (sube hasta 'Escalon', baja lo que sea, pendiente
        /// de 45° como mucho, la capsula no puede tocar nada) y apunta cada paso como una flecha de A a B. Despues mira,
        /// hacia atras, desde que casillas se puede volver al inicio. Las que se alcanzan y no vuelven son trampas: el
        /// jugador entra y se queda atascado. Se apunta que colliders las rodean.
        /// </summary>
        private static void BuscarTrampas(Medida m, Casilla inicio, int real) {
            var radio = Radio * 0.95f;   // la piel del CharacterController: tocar no es chocar
            Casilla desde;
            if (!Suelo(Centro(inicio) + Vector3.up * 1f, 3f, real, out desde)) return;
            var nodos = new Dictionary<long, Casilla> { { Clave(desde), desde } };
            var entran = new Dictionary<long, List<long>>();
            var cola = new Queue<Casilla>();
            cola.Enqueue(desde);
            var dirs = new[] { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } };
            while (cola.Count > 0 && nodos.Count < MaximoDeCasillas) {
                var c = cola.Dequeue();
                var kc = Clave(c);
                foreach (var d in dirs) {
                    Casilla n;
                    if (!Suelo(new Vector3((c.X + d[0]) * Paso, c.Y + Escalon + 0.05f, (c.Z + d[1]) * Paso), Escalon + Bajada + 0.05f, real, out n)) continue;
                    if (Choca(n, radio, real)) continue;
                    // El tramo entre las dos casillas tambien: que no haya nada en medio (una pata entre dos huecos).
                    if (Physics.CheckCapsule(Centro(c) + Vector3.up * (Escalon + radio), Centro(n) + Vector3.up * (Escalon + radio), radio * 0.8f,
                                             real, QueryTriggerInteraction.Ignore)) continue;
                    var kn = Clave(n);
                    List<long> lista;
                    if (!entran.TryGetValue(kn, out lista)) entran[kn] = lista = new List<long>();
                    lista.Add(kc);
                    if (nodos.ContainsKey(kn)) continue;
                    nodos[kn] = n;
                    cola.Enqueue(n);
                }
            }
            m.Alcanzables = nodos.Count;

            // Hacia atras: desde el inicio, siguiendo las flechas al reves.
            var vuelven = new HashSet<long> { Clave(desde) };
            var atras = new Queue<long>();
            atras.Enqueue(Clave(desde));
            var salen = new Dictionary<long, List<long>>();
            foreach (var kv in entran)
                foreach (var origen in kv.Value) {
                    List<long> l;
                    if (!salen.TryGetValue(origen, out l)) salen[origen] = l = new List<long>();
                    l.Add(kv.Key);
                }
            // vuelve(A) si existe A→B con vuelve(B): se propaga desde el inicio por las flechas que ENTRAN.
            while (atras.Count > 0) {
                var k = atras.Dequeue();
                List<long> origenes;
                if (!entran.TryGetValue(k, out origenes)) continue;
                foreach (var o in origenes) if (vuelven.Add(o)) atras.Enqueue(o);
            }
            foreach (var kv in nodos) {
                if (vuelven.Contains(kv.Key)) continue;
                m.Trampas++;
                var p = Centro(kv.Value);
                m.SitiosDeTrampa.Add(p);
                // Lo que la rodea, a ras de suelo incluido: es lo que cierra la salida.
                foreach (var col in Physics.OverlapCapsule(p + Vector3.up * 0.05f, p + Vector3.up * (Alto - Radio), Radio + Paso * 1.5f, real,
                                                           QueryTriggerInteraction.Ignore)) {
                    int n;
                    m.Encierran.TryGetValue(col, out n);
                    m.Encierran[col] = n + 1;
                }
            }
        }

        private static long Clave(Casilla c) {
            // Pisos distintos en la misma x,z: el alto, en tramos de 1,5 m.
            var piso = Mathf.FloorToInt(c.Y / 1.5f) + 512;
            return ((long)(c.X + 1_000_000) << 32) | ((long)(c.Z + 32768) << 10) | (long)(piso & 1023);
        }

        private static Vector3 Centro(Casilla c) { return new Vector3(c.X * Paso, c.Y, c.Z * Paso); }

        private static bool Suelo(Vector3 desde, float largo, int mascara, out Casilla c) {
            c = new Casilla { X = Mathf.RoundToInt(desde.x / Paso), Z = Mathf.RoundToInt(desde.z / Paso) };
            RaycastHit hit;
            var origen = new Vector3(c.X * Paso, desde.y, c.Z * Paso);
            if (!Physics.Raycast(origen, Vector3.down, out hit, largo, mascara, QueryTriggerInteraction.Collide) || hit.normal.y < PendienteMinima)
                return false;
            c.Y = hit.point.y;
            return true;
        }

        private static bool Choca(Casilla c, float radio, int mascara) {
            var p1 = new Vector3(c.X * Paso, c.Y + Escalon + radio, c.Z * Paso);
            var p2 = new Vector3(c.X * Paso, c.Y + Alto - radio, c.Z * Paso);
            if (p2.y < p1.y) p2.y = p1.y;
            return Physics.CheckCapsule(p1, p2, radio, mascara, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Las capas con las que choca el jugador (la matriz de colisiones), sin la visual.</summary>
        private static int MascaraReal(int capaDelJugador) {
            var m = 0;
            for (var l = 0; l < 32; l++)
                if (l != CapaVisual && !Physics.GetIgnoreLayerCollision(capaDelJugador, l)) m |= 1 << l;
            return m;
        }

        private static CharacterController BuscarJugador(Scene escena) {
            foreach (var raiz in escena.GetRootGameObjects()) {
                var cc = raiz.GetComponentInChildren<CharacterController>(true);
                if (cc != null) return cc;
            }
            return null;
        }

        // ==================================================================== el mundo visual

        /// <summary>De que renderer salio cada collider del mundo visual.</summary>
        private static readonly Dictionary<Collider, MeshRenderer> _origenes = new Dictionary<Collider, MeshRenderer>();

        private static MeshRenderer Origen(Collider c) {
            MeshRenderer r;
            return _origenes.TryGetValue(c, out r) ? r : null;
        }

        /// <summary>
        /// Un MeshCollider en la capa 31 por cada malla que se ve, colgado como hijo del propio renderer con transform
        /// identidad: asi hereda su matriz exacta (con padres girados y escalados de forma no uniforme, copiar
        /// posicion, giro y lossyScale pierde el cizallamiento y el espejo, y el gemelo quedaba desplazado).
        /// Se destruyen todos al terminar de medir (y no se guardan nunca: DontSave).
        /// </summary>
        private static List<GameObject> ConstruirMundoVisual(Scene escena, Transform jugador) {
            _origenes.Clear();
            var gemelos = new List<GameObject>();
            foreach (var r in Todos<MeshRenderer>(escena).ToList()) {
                if (!r.enabled || !r.gameObject.activeInHierarchy || r.transform.IsChildOf(jugador)) continue;
                var f = r.GetComponent<MeshFilter>();
                if (f == null || f.sharedMesh == null) continue;
                var go = new GameObject("~ gemelo visual") { hideFlags = HideFlags.HideAndDontSave, layer = CapaVisual };
                go.transform.SetParent(r.transform, false);
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = f.sharedMesh;
                _origenes[mc] = r;
                gemelos.Add(go);
            }
            return gemelos;
        }

        private static List<MeshRenderer> Visibles(GameObject go) {
            return go.GetComponentsInChildren<MeshRenderer>(false).Where(r => r.enabled).ToList();
        }

        private static IEnumerable<T> Todos<T>(Scene escena) where T : Component {
            return escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true));
        }

        private static string Ruta(GameObject go) {
            var partes = new List<string>();
            for (var t = go.transform; t != null; t = t.parent) partes.Insert(0, t.name);
            return string.Join("/", partes);
        }

        // ==================================================================== informe y mapa

        private static void Escribir(Medida m, Dictionary<long, Casilla> andables, HashSet<long> invisibles,
                                     List<Vector3> atravesables, string historia) {
            Directory.CreateDirectory(Carpeta);
            var t = new StringBuilder();
            t.AppendLine("Paredes invisibles del recorrido 3D · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            t.AppendLine(m.Resumen);
            // Los limites del suelo andable, en metros: son los que pide StreamingAssets/mapa-3d.json ('mundo') para
            // poner al jugador sobre el plano ilustrado (tecla M). El PNG de abajo empieza 4 casillas antes de minX/minZ.
            if (andables.Count > 0)
                t.AppendLine($"Suelo andable (m): minX {andables.Values.Min(c => c.X) * Paso:F2} · maxX {andables.Values.Max(c => c.X) * Paso:F2} · " +
                             $"minZ {andables.Values.Min(c => c.Z) * Paso:F2} · maxZ {andables.Values.Max(c => c.Z) * Paso:F2}");
            t.AppendLine();
            if (!string.IsNullOrEmpty(historia)) { t.AppendLine(historia); t.AppendLine(); }
            t.AppendLine("CHOCA Y NO SE VE (casillas andables que tapa · collider)");
            foreach (var kv in m.Invisibles.OrderByDescending(kv => kv.Value))
                if (kv.Key != null) t.AppendLine($"{kv.Value,6} · {kv.Key.GetType().Name,-15} · {Ruta(kv.Key.gameObject)}");
            t.AppendLine();
            t.AppendLine("TRAMPAS: SE ENTRA Y NO SE SALE (casillas · collider que las rodea)");
            foreach (var kv in m.Encierran.OrderByDescending(kv => kv.Value))
                if (kv.Key != null) t.AppendLine($"{kv.Value,6} · {kv.Key.GetType().Name,-15} · {Ruta(kv.Key.gameObject)}");
            foreach (var p in m.SitiosDeTrampa.Take(40)) t.AppendLine($"         en ({p.x:F2}, {p.y:F2}, {p.z:F2})");
            t.AppendLine();
            t.AppendLine("SE VE Y SE ATRAVIESA (casillas de borde · renderer de más de 1,5 m)");
            foreach (var kv in m.Atravesables.OrderByDescending(kv => kv.Value))
                if (kv.Key != null) t.AppendLine($"{kv.Value,6} · {Ruta(kv.Key.gameObject)}");
            File.WriteAllText(Path.Combine(Carpeta, "informe-paredes-invisibles.txt"), t.ToString(), new UTF8Encoding(false));

            // El mapa, visto desde arriba: gris = andable, rojo = pared invisible, naranja = pared atravesable.
            if (andables.Count == 0) return;
            int minX = andables.Values.Min(c => c.X) - 4, maxX = andables.Values.Max(c => c.X) + 4;
            int minZ = andables.Values.Min(c => c.Z) - 4, maxZ = andables.Values.Max(c => c.Z) + 4;
            int w = maxX - minX + 1, h = maxZ - minZ + 1, k = Mathf.Clamp(1600 / Mathf.Max(w, h), 1, 4);
            var tex = new Texture2D(w * k, h * k, TextureFormat.RGB24, false);
            var px = Enumerable.Repeat(new Color32(18, 22, 28, 255), w * k * h * k).ToArray();
            Action<int, int, Color32> pintar = (x, z, col) => {
                if (x < minX || x > maxX || z < minZ || z > maxZ) return;
                for (var i = 0; i < k; i++)
                    for (var j = 0; j < k; j++) px[((z - minZ) * k + j) * w * k + (x - minX) * k + i] = col;
            };
            foreach (var c in andables.Values) pintar(c.X, c.Z, new Color32(150, 160, 170, 255));
            foreach (var p in atravesables) pintar(Mathf.RoundToInt(p.x / Paso), Mathf.RoundToInt(p.z / Paso), new Color32(255, 150, 40, 255));
            foreach (var kv in andables) if (invisibles.Contains(kv.Key)) pintar(kv.Value.X, kv.Value.Z, new Color32(235, 40, 60, 255));
            foreach (var p in m.SitiosDeTrampa) pintar(Mathf.RoundToInt(p.x / Paso), Mathf.RoundToInt(p.z / Paso), new Color32(190, 80, 255, 255));
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Carpeta, "mapa-paredes-invisibles.png"), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
