using System.Collections.Generic;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Las flechas del mapa vivo (2026-11-08): rectas ortogonales (con codos) que NUNCA pisan otra caja y que se cruzan lo menos
    /// posible. Para cada flecha se prueban todos los puertos (tres por lado) de las dos cajas y los caminos de uno, dos o tres
    /// tramos por los «pasillos» que dejan las demas cajas; gana el mas corto, con pocos codos y sin cruces. Se reparten los
    /// puertos para que dos flechas no salgan del mismo punto. Coordenadas de la lamina (y crece hacia abajo).
    /// </summary>
    internal static class RutaDeFlechas {
        private const float Margen = 9;

        /// <summary>
        /// 'partes[i]' son los rectangulos que ocupa la caja i (la primera es la que lleva los puertos). 'sinAbajo[i]' impide
        /// salir por debajo (donde una persona lleva su nombre). Devuelve, por cada par, la ruta o null si no hay camino limpio.
        /// </summary>
        public static List<List<Vector2>> Calcular(List<List<Rect>> partes, List<bool> sinAbajo, List<int[]> pares, float ancho, float alto) {
            var canalesX = new List<float> { 10, ancho - 10 };
            var canalesY = new List<float> { 10, alto - 10 };
            foreach (var p in partes) foreach (var r in p) {
                canalesX.Add(r.xMin - Margen); canalesX.Add(r.xMax + Margen);
                canalesY.Add(r.yMin - Margen); canalesY.Add(r.yMax + Margen);
            }
            canalesX = Depurar(canalesX); canalesY = Depurar(canalesY);

            var puertos = new List<List<Vector2>>();
            for (var i = 0; i < partes.Count; i++) puertos.Add(PuertosDe(partes[i][0], sinAbajo[i]));

            var rutas = new List<List<Vector2>>();
            var usados = new HashSet<Vector2>();
            foreach (var _ in pares) rutas.Add(null);

            for (var pasada = 0; pasada < 3; pasada++) {
                for (var k = 0; k < pares.Count; k++) {
                    if (rutas[k] != null) { usados.Remove(rutas[k][0]); usados.Remove(rutas[k][rutas[k].Count - 1]); }
                    var otras = new List<List<Vector2>>();
                    for (var j = 0; j < rutas.Count; j++) if (j != k && rutas[j] != null) otras.Add(rutas[j]);
                    var mejor = Mejor(partes, puertos, pares[k][0], pares[k][1], canalesX, canalesY, otras, usados);
                    rutas[k] = mejor ?? rutas[k];
                    if (rutas[k] != null) { usados.Add(rutas[k][0]); usados.Add(rutas[k][rutas[k].Count - 1]); }
                }
            }
            return rutas;
        }

        private static List<float> Depurar(List<float> v) {
            v.Sort();
            var r = new List<float>();
            foreach (var x in v) if (r.Count == 0 || x - r[r.Count - 1] > 3) r.Add(x);
            var extra = new List<float>();
            for (var i = 1; i < r.Count; i++) if (r[i] - r[i - 1] > 18) extra.Add((r[i] + r[i - 1]) / 2);
            r.AddRange(extra);
            return r;
        }

        private static List<Vector2> PuertosDe(Rect r, bool sinAbajo) {
            var fr = r.width < 70 || r.height < 70 ? new[] { 0.5f } : new[] { 0.25f, 0.5f, 0.75f };
            var l = new List<Vector2>();
            foreach (var f in fr) {
                l.Add(new Vector2(r.xMin + r.width * f, r.yMin));
                if (!sinAbajo) l.Add(new Vector2(r.xMin + r.width * f, r.yMax));
                l.Add(new Vector2(r.xMin, r.yMin + r.height * f));
                l.Add(new Vector2(r.xMax, r.yMin + r.height * f));
            }
            return l;
        }

        private static List<Vector2> Mejor(List<List<Rect>> partes, List<List<Vector2>> puertos, int a, int b, List<float> xs, List<float> ys,
                                           List<List<Vector2>> otras, HashSet<Vector2> usados) {
            List<Vector2> mejor = null; var costeMejor = float.MaxValue;
            foreach (var pa in puertos[a]) foreach (var pb in puertos[b]) {
                var penal = (usados.Contains(pa) ? 5000f : 0) + (usados.Contains(pb) ? 5000f : 0);
                foreach (var ruta in Candidatos(pa, pb, xs, ys)) {
                    if (!Limpia(ruta, partes, a, b)) continue;
                    var c = penal + Coste(ruta, otras);
                    if (c < costeMejor) { costeMejor = c; mejor = ruta; }
                }
            }
            return mejor;
        }

        private static IEnumerable<List<Vector2>> Candidatos(Vector2 a, Vector2 b, List<float> xs, List<float> ys) {
            if (Mathf.Abs(a.x - b.x) < 0.5f || Mathf.Abs(a.y - b.y) < 0.5f) yield return new List<Vector2> { a, b };
            yield return new List<Vector2> { a, new Vector2(a.x, b.y), b };
            yield return new List<Vector2> { a, new Vector2(b.x, a.y), b };
            foreach (var cx in xs) yield return new List<Vector2> { a, new Vector2(cx, a.y), new Vector2(cx, b.y), b };
            foreach (var cy in ys) yield return new List<Vector2> { a, new Vector2(a.x, cy), new Vector2(b.x, cy), b };
        }

        private static bool Limpia(List<Vector2> ruta, List<List<Rect>> partes, int a, int b) {
            for (var i = 0; i + 1 < ruta.Count; i++) {
                var p = ruta[i]; var q = ruta[i + 1];
                if ((p - q).sqrMagnitude < 0.01f) continue;
                var seg = Rect.MinMaxRect(Mathf.Min(p.x, q.x), Mathf.Min(p.y, q.y), Mathf.Max(p.x, q.x), Mathf.Max(p.y, q.y));
                for (var k = 0; k < partes.Count; k++) {
                    var propia = k == a || k == b;
                    foreach (var r in partes[k]) {
                        var caja = propia ? Rect.MinMaxRect(r.xMin + 2, r.yMin + 2, r.xMax - 2, r.yMax - 2)
                                          : Rect.MinMaxRect(r.xMin - Margen, r.yMin - Margen, r.xMax + Margen, r.yMax + Margen);
                        if (seg.xMin < caja.xMax && seg.xMax > caja.xMin && seg.yMin < caja.yMax && seg.yMax > caja.yMin) return false;
                    }
                }
            }
            return true;
        }

        private static float Coste(List<Vector2> ruta, List<List<Vector2>> otras) {
            var largo = 0f;
            for (var i = 0; i + 1 < ruta.Count; i++) largo += Vector2.Distance(ruta[i], ruta[i + 1]);
            var c = largo + 30 * (ruta.Count - 2);
            foreach (var o in otras)
                for (var i = 0; i + 1 < ruta.Count; i++)
                    for (var j = 0; j + 1 < o.Count; j++) c += Choque(ruta[i], ruta[i + 1], o[j], o[j + 1]);
            return c;
        }

        /// <summary>Cuanto pesa que dos tramos se crucen (600) o se solapen yendo juntos (800).</summary>
        private static float Choque(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2) {
            var h1 = Mathf.Abs(p1.y - p2.y) < 0.5f; var h2 = Mathf.Abs(q1.y - q2.y) < 0.5f;
            if (h1 == h2) {
                // paralelos: se solapan si estan en la misma recta (casi) y sus rangos se pisan
                if (h1) return Mathf.Abs(p1.y - q1.y) < 6 && Pisan(p1.x, p2.x, q1.x, q2.x) ? 800 : 0;
                return Mathf.Abs(p1.x - q1.x) < 6 && Pisan(p1.y, p2.y, q1.y, q2.y) ? 800 : 0;
            }
            // uno horizontal y otro vertical: se cruzan si cada uno cae dentro del rango del otro
            var hp1 = h1 ? p1 : q1; var hp2 = h1 ? p2 : q2; var vp1 = h1 ? q1 : p1; var vp2 = h1 ? q2 : p2;
            var dentroX = vp1.x > Mathf.Min(hp1.x, hp2.x) + 1 && vp1.x < Mathf.Max(hp1.x, hp2.x) - 1;
            var dentroY = hp1.y > Mathf.Min(vp1.y, vp2.y) + 1 && hp1.y < Mathf.Max(vp1.y, vp2.y) - 1;
            return dentroX && dentroY ? 600 : 0;
        }

        private static bool Pisan(float a1, float a2, float b1, float b2) {
            return Mathf.Max(Mathf.Min(a1, a2), Mathf.Min(b1, b2)) + 3 < Mathf.Min(Mathf.Max(a1, a2), Mathf.Max(b1, b2));
        }
    }
}
