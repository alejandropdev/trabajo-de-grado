using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// (2026-11-15, direccion de arte; nacio en la exploracion de la ronda 2). Una forma cualquiera (tambien concava: curvas,
    /// siluetas organicas) rellena con un degradado lineal de verdad, interpolado por vertice. Coordenadas «de SVG»
    /// (origen arriba a la izquierda del rect). Es lo que le falta a DibujoUI, que solo rellena convexos y en un color.
    /// </summary>
    public sealed class FormaDegradada : MaskableGraphic {
        public List<Vector2> Puntos = new List<Vector2>();
        public Color ColorA = Color.white, ColorB = Color.white;
        /// <summary>El eje del degradado: en Desde vale ColorA y en Hasta, ColorB.</summary>
        public Vector2 Desde, Hasta = new Vector2(0, 1);

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
            if (Puntos == null || Puntos.Count < 3) return;
            var r = rectTransform.rect;
            var eje = Hasta - Desde;
            var largo2 = Mathf.Max(0.0001f, eje.sqrMagnitude);
            foreach (var p in Puntos) {
                var t = Mathf.Clamp01(Vector2.Dot(p - Desde, eje) / largo2);
                vh.AddVert(new Vector3(r.xMin + p.x, r.yMax - p.y), Color.Lerp(ColorA, ColorB, t) * color, Vector2.zero);
            }
            foreach (var tri in Triangular(Puntos)) vh.AddTriangle(tri[0], tri[1], tri[2]);
        }

        /// <summary>Recorte de orejas: indices de triangulos para un poligono simple (sin cruces), concavo o no.</summary>
        public static List<int[]> Triangular(List<Vector2> p) {
            var res = new List<int[]>();
            var idx = new List<int>();
            for (var i = 0; i < p.Count; i++) idx.Add(i);
            float area = 0;
            for (var i = 0; i < p.Count; i++) { var a = p[i]; var b = p[(i + 1) % p.Count]; area += a.x * b.y - b.x * a.y; }
            var signo = Mathf.Sign(area);
            var guarda = 0;
            while (idx.Count > 3 && guarda++ < 5000) {
                var cortado = false;
                for (var k = 0; k < idx.Count; k++) {
                    int ia = idx[(k + idx.Count - 1) % idx.Count], ib = idx[k], ic = idx[(k + 1) % idx.Count];
                    Vector2 a = p[ia], b = p[ib], c = p[ic];
                    if (Cruz(a, b, c) * signo <= 0) continue;
                    var dentro = false;
                    foreach (var j in idx) {
                        if (j == ia || j == ib || j == ic) continue;
                        var q = p[j];
                        if (Cruz(a, b, q) * signo > 0 && Cruz(b, c, q) * signo > 0 && Cruz(c, a, q) * signo > 0) { dentro = true; break; }
                    }
                    if (dentro) continue;
                    res.Add(new[] { ia, ib, ic });
                    idx.RemoveAt(k);
                    cortado = true;
                    break;
                }
                if (!cortado) break;   // poligono degenerado: lo que quede se abanica
            }
            for (var k = 1; k + 1 < idx.Count; k++) res.Add(new[] { idx[0], idx[k], idx[k + 1] });
            return res;
        }

        private static float Cruz(Vector2 a, Vector2 b, Vector2 c) { return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x); }
    }
}
