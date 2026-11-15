using System;
using System.Collections.Generic;
using Mundo3D.GeneracionObjetos;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Nexus.Mundo3D {
    /// <summary>
    /// Reparte los NPC por el mundo 3D al cargarse la escena de recoleccion (2026-11-11), sin tocar el archivo de escena.
    ///
    ///   · Toma como moldes los NPC «Npc1»…«Npc6» que hay bajo «Npcs» (los desactiva: los clones salen de ellos);
    ///   · «Npc7» NO se toca ni se repite nunca: es el de la zona del Escondite, y ahi no sale ningun otro;
    ///   · en cada una de las otras cuatro zonas (Oficinas, Bodega, Cafeteria y Museo) y en los pasillos centrales (la parte
    ///     caminable que queda fuera de las cinco zonas) salen cinco NPC distintos de los seis disenos. Cada area omite un
    ///     diseno diferente, asi que cada diseno aparece al menos cuatro veces en el mundo;
    ///   · las posiciones son puntos al azar de la malla de navegacion, al menos a 2 m unos de otros (se relaja si no caben).
    /// Si falta la malla o las zonas, se avisa en la consola y la escena se queda como estaba.
    /// </summary>
    public static class SpawnerDeNpcs {
        private const int NpcsPorArea = 5;
        private const float Separacion = 2f;
        private const int IntentosPorNpc = 60;
        private const string NombreDelContenedor = "Npcs poblados (Nexus)";

        private sealed class Area {
            public string Nombre;
            public Func<Vector3, bool> Contiene;
        }

        public static void Poblar(Scene escena) {
            if (!escena.IsValid() || !escena.isLoaded) return;
            Transform raiz = null;
            var moldes = new SortedDictionary<int, GameObject>();
            var zonas = new List<SpawnZoneBehaviour>();
            foreach (var go in escena.GetRootGameObjects()) {
                foreach (var t in go.GetComponentsInChildren<Transform>(true)) {
                    if (t.name == NombreDelContenedor) { Debug.Log("[Npcs] La escena ya estaba poblada."); return; }
                    if (t.name == "Npcs") raiz = t;
                }
                zonas.AddRange(go.GetComponentsInChildren<SpawnZoneBehaviour>(true));
            }
            if (raiz == null) { Debug.LogWarning("[Npcs] No hay un objeto «Npcs» en la escena: no se reparte nada."); return; }
            foreach (Transform hijo in raiz) {
                int n;
                if (hijo.name.Length == 4 && hijo.name.StartsWith("Npc", StringComparison.Ordinal) && int.TryParse(hijo.name.Substring(3), out n) && n >= 1 && n <= 6)
                    moldes[n] = hijo.gameObject;
            }
            if (moldes.Count == 0) { Debug.LogWarning("[Npcs] No hay moldes Npc1…Npc6: no se reparte nada."); return; }

            var triangulacion = NavMesh.CalculateTriangulation();
            if (triangulacion.indices.Length < 3) { Debug.LogWarning("[Npcs] La escena no tiene malla de navegacion: no se reparte nada."); return; }

            var areas = DefinirAreas(zonas);
            if (areas.Count == 0) { Debug.LogWarning("[Npcs] No se encontraron las zonas: no se reparte nada."); return; }

            // Los moldes, desactivados antes de clonar: asi los clones nacen apagados y se activan ya colocados.
            var disenos = new List<int>(moldes.Keys);
            foreach (var m in moldes.Values) m.SetActive(false);

            var contenedor = new GameObject(NombreDelContenedor);
            SceneManager.MoveGameObjectToScene(contenedor, escena);
            contenedor.transform.SetParent(raiz, false);

            // Cada area omite un diseno distinto (si hay mas disenos que NPC por area): asi salen todos varias veces.
            var omitidos = new List<int>(disenos);
            Barajar(omitidos);
            var puestos = new List<Vector3>();
            var total = 0;
            for (var a = 0; a < areas.Count; a++) {
                var area = areas[a];
                var triangulos = TriangulosDelArea(triangulacion, area);
                if (triangulos.Count == 0) { Debug.LogWarning("[Npcs] El area «" + area.Nombre + "» no tiene malla caminable."); continue; }

                var elegibles = new List<int>(disenos);
                if (elegibles.Count > NpcsPorArea) elegibles.Remove(omitidos[a % omitidos.Count]);
                Barajar(elegibles);
                var cuantos = Mathf.Min(NpcsPorArea, elegibles.Count);
                for (var i = 0; i < cuantos; i++) {
                    Vector3 posicion;
                    if (!BuscarPunto(triangulos, area, puestos, out posicion)) { Debug.LogWarning("[Npcs] No cabe otro NPC en «" + area.Nombre + "»."); break; }
                    puestos.Add(posicion);
                    Crear(moldes[elegibles[i]], contenedor.transform, escena, posicion, area.Nombre + " " + (i + 1), elegibles[i]);
                    total++;
                }
            }
            Debug.Log("[Npcs] Repartidos " + total + " NPC en " + areas.Count + " areas (Npc7 se queda donde estaba).");
        }

        // ---------------------------------------------------------------- las areas

        private static List<Area> DefinirAreas(List<SpawnZoneBehaviour> zonas) {
            var cajas = new List<BoxCollider>();
            BoxCollider escondite = null;
            var propias = new List<KeyValuePair<string, BoxCollider>>();
            foreach (var z in zonas) {
                var caja = z.ZoneBounds != null ? z.ZoneBounds : z.GetComponent<BoxCollider>();
                if (caja == null) continue;
                cajas.Add(caja);
                var nombre = z.name.ToLowerInvariant();
                if (nombre.Contains("escondite")) { escondite = caja; continue; }
                string etiqueta = null;
                if (nombre.Contains("oficina")) etiqueta = "Oficinas";
                else if (nombre.Contains("bodega")) etiqueta = "Bodega";
                else if (nombre.Contains("cafeter")) etiqueta = "Cafeteria";
                else if (nombre.Contains("museo")) etiqueta = "Museo";
                if (etiqueta != null) propias.Add(new KeyValuePair<string, BoxCollider>(etiqueta, caja));
            }
            var areas = new List<Area>();
            foreach (var par in propias) {
                var caja = par.Value; var vedada = escondite;
                // La zona del Escondite es de Npc7: nada mas puede salir en ella, ni siquiera desde otra zona que se le solape.
                areas.Add(new Area { Nombre = par.Key, Contiene = p => Dentro(caja, p) && (vedada == null || !Dentro(vedada, p)) });
            }
            if (areas.Count > 0) {
                var todas = cajas.ToArray();
                areas.Add(new Area { Nombre = "Pasillos", Contiene = p => { foreach (var c in todas) if (Dentro(c, p)) return false; return true; } });
            }
            return areas;
        }

        /// <summary>Dentro de la caja en horizontal (con un margen alto en vertical: la caja no siempre llega al suelo).</summary>
        private static bool Dentro(BoxCollider caja, Vector3 mundo) {
            var local = caja.transform.InverseTransformPoint(mundo) - caja.center;
            var mitad = caja.size * 0.5f;
            return Mathf.Abs(local.x) <= mitad.x && Mathf.Abs(local.z) <= mitad.z && Mathf.Abs(local.y) <= mitad.y + 3f;
        }

        // ---------------------------------------------------------------- puntos de la malla

        private struct Triangulo { public Vector3 A, B, C; public float Superficie; }

        /// <summary>Los triangulos de la malla cuyo centro cae en el area, con su superficie (para elegirlos en proporcion).</summary>
        private static List<Triangulo> TriangulosDelArea(NavMeshTriangulation t, Area area) {
            var lista = new List<Triangulo>();
            for (var i = 0; i + 2 < t.indices.Length; i += 3) {
                var a = t.vertices[t.indices[i]]; var b = t.vertices[t.indices[i + 1]]; var c = t.vertices[t.indices[i + 2]];
                var centro = (a + b + c) / 3f;
                if (!area.Contiene(centro)) continue;
                var superficie = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                if (superficie < 0.05f) continue;
                lista.Add(new Triangulo { A = a, B = b, C = c, Superficie = superficie });
            }
            return lista;
        }

        private static bool BuscarPunto(List<Triangulo> triangulos, Area area, List<Vector3> ocupados, out Vector3 resultado) {
            var total = 0f;
            foreach (var t in triangulos) total += t.Superficie;
            for (var intento = 0; intento < IntentosPorNpc; intento++) {
                // Mientras se puede, a 2 m de los demas; en los ultimos intentos se acepta un poco mas cerca.
                var separacion = intento < IntentosPorNpc * 0.75f ? Separacion : Separacion * 0.5f;
                var tirada = Random.value * total;
                var elegido = triangulos[triangulos.Count - 1];
                foreach (var t in triangulos) { tirada -= t.Superficie; if (tirada <= 0f) { elegido = t; break; } }
                var r1 = Mathf.Sqrt(Random.value); var r2 = Random.value;
                var punto = (1f - r1) * elegido.A + r1 * (1f - r2) * elegido.B + r1 * r2 * elegido.C;
                NavMeshHit hit;
                if (!NavMesh.SamplePosition(punto, out hit, 1f, NavMesh.AllAreas)) continue;
                if (!area.Contiene(hit.position)) continue;
                var lejos = true;
                foreach (var o in ocupados) if ((o - hit.position).sqrMagnitude < separacion * separacion) { lejos = false; break; }
                if (!lejos) continue;
                resultado = hit.position;
                return true;
            }
            resultado = Vector3.zero;
            return false;
        }

        // ---------------------------------------------------------------- un NPC

        private static void Crear(GameObject molde, Transform padre, Scene escena, Vector3 posicion, string lugar, int diseno) {
            var clon = Object.Instantiate(molde);   // el molde esta apagado: el clon nace apagado
            SceneManager.MoveGameObjectToScene(clon, escena);   // un Instantiate suelto cae en la escena activa, no en esta
            clon.name = "Npc" + diseno + " · " + lugar;
            clon.transform.SetParent(padre, true);
            clon.transform.SetPositionAndRotation(posicion, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            clon.SetActive(true);
            var agente = clon.GetComponent<NavMeshAgent>();
            if (agente != null && agente.enabled) agente.Warp(posicion);
        }

        private static void Barajar<T>(IList<T> lista) {
            for (var i = lista.Count - 1; i > 0; i--) {
                var j = Random.Range(0, i + 1);
                var tmp = lista[i]; lista[i] = lista[j]; lista[j] = tmp;
            }
        }
    }
}
