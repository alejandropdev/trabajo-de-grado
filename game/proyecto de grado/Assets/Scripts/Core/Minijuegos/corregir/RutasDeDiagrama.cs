using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Minijuegos.Corregir
{
    public struct PuntoDeRuta
    {
        public float X, Y;
        public PuntoDeRuta(float x, float y) { X = x; Y = y; }
    }

    public sealed class CajaDeRuta
    {
        public string Id;
        public float X, Y, Ancho, Alto;
    }

    public sealed class ConexionDeRuta
    {
        public string Id, Desde, Hasta;
    }

    /// <summary>
    /// Las flechas del diagrama (2026-11-20): rutas ortogonales que esquivan las cajas y se cruzan lo menos posible.
    /// Antes eran rectas de centro a centro y atravesaban cajas y otras flechas. Aqui cada flecha sale de un lado de su
    /// caja, va por los pasillos entre cajas (con carriles distintos para que no se monten) y entra por un lado de la
    /// otra. Es un camino de coste minimo sobre una rejilla de pasillos: largo + giros + cruces con las ya trazadas.
    /// Sin dependencias de Unity: se prueba con dotnet.
    /// </summary>
    public static class RutasDeDiagrama
    {
        /// <summary>Lo que sobresale la flecha de la caja antes de girar, y la holgura con la que se esquivan las cajas.</summary>
        public const float Margen = 22f;

        private const float CosteGiro = 35f, CosteCruce = 420f, CosteSolape = 7f, CosteSolapeFijo = 260f, CostePuertoUsado = 240f;
        private static readonly float[] Ranuras = { 0.5f, 0.28f, 0.72f };
        // Direcciones: 0 = derecha, 1 = abajo, 2 = izquierda, 3 = arriba.
        private static readonly int[] Dx = { 1, 0, -1, 0 };
        private static readonly int[] Dy = { 0, 1, 0, -1 };

        private struct Puerto
        {
            public PuntoDeRuta Borde, Cabo;
            public int Salida;      // la direccion en la que se aleja de la caja
            public string Clave;
        }

        private struct Tramo
        {
            public float X1, Y1, X2, Y2;
            public bool Horizontal { get { return Math.Abs(Y1 - Y2) < 0.01f; } }
        }

        /// <summary>Una ruta por conexion (por su Id). Las que no tienen sus dos cajas, o van de una caja a si misma, se omiten.</summary>
        public static Dictionary<string, List<PuntoDeRuta>> Calcular(IList<CajaDeRuta> cajas, IList<ConexionDeRuta> conexiones, float margen = Margen)
        {
            var resultado = new Dictionary<string, List<PuntoDeRuta>>();
            if (cajas == null || cajas.Count == 0 || conexiones == null) return resultado;
            var porId = cajas.GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First());

            // ---- la rejilla de pasillos
            var xs = new List<float>(); var ys = new List<float>();
            foreach (var c in cajas)
            {
                xs.Add(c.X - margen); xs.Add(c.X + c.Ancho + margen);
                ys.Add(c.Y - margen); ys.Add(c.Y + c.Alto + margen);
                foreach (var f in Ranuras) { xs.Add(c.X + c.Ancho * f); ys.Add(c.Y + c.Alto * f); }
            }
            AnadirPasillos(xs, cajas.Select(c => new[] { c.X, c.X + c.Ancho }).ToList(), margen);
            AnadirPasillos(ys, cajas.Select(c => new[] { c.Y, c.Y + c.Alto }).ToList(), margen);
            var gx = Unicos(xs); var gy = Unicos(ys);
            var nx = gx.Count; var ny = gy.Count;

            // Las cajas, algo infladas: nada de la ruta entra en ellas.
            var obstaculos = cajas.Select(c => new[] { c.X - margen + 0.5f, c.Y - margen + 0.5f, c.X + c.Ancho + margen - 0.5f, c.Y + c.Alto + margen - 0.5f }).ToList();
            var libre = new bool[nx * ny];
            for (var j = 0; j < ny; j++)
                for (var i = 0; i < nx; i++)
                    libre[j * nx + i] = !obstaculos.Any(o => gx[i] > o[0] + 0.01f && gx[i] < o[2] - 0.01f && gy[j] > o[1] + 0.01f && gy[j] < o[3] - 0.01f);

            var trazadas = new List<Tramo>();
            var puertosUsados = new HashSet<string>();
            var orden = Enumerable.Range(0, conexiones.Count)
                .Where(k => conexiones[k].Desde != conexiones[k].Hasta && porId.ContainsKey(conexiones[k].Desde ?? "") && porId.ContainsKey(conexiones[k].Hasta ?? ""))
                .OrderByDescending(k => Distancia(porId[conexiones[k].Desde], porId[conexiones[k].Hasta]))
                .ThenBy(k => k).ToList();

            foreach (var k in orden)
            {
                var con = conexiones[k];
                var ruta = Trazar(porId[con.Desde], porId[con.Hasta], gx, gy, libre, obstaculos, trazadas, puertosUsados, margen);
                if (ruta == null) ruta = Recta(porId[con.Desde], porId[con.Hasta]);
                resultado[con.Id] = ruta;
                for (var s = 0; s + 1 < ruta.Count; s++) trazadas.Add(new Tramo { X1 = ruta[s].X, Y1 = ruta[s].Y, X2 = ruta[s + 1].X, Y2 = ruta[s + 1].Y });
            }
            return resultado;
        }

        // ====================================================================== el camino

        private static List<PuntoDeRuta> Trazar(CajaDeRuta a, CajaDeRuta b, List<float> gx, List<float> gy, bool[] libre, List<float[]> obstaculos,
                                               List<Tramo> trazadas, HashSet<string> puertosUsados, float margen)
        {
            var nx = gx.Count; var ny = gy.Count;
            var salidas = Puertos(a, margen); var entradas = Puertos(b, margen);
            var estados = nx * ny * 4;
            var coste = new float[estados];
            var previo = new int[estados];
            var inicio = new int[estados];   // el puerto de salida con el que se empezo (por estado)
            for (var s = 0; s < estados; s++) { coste[s] = float.MaxValue; previo[s] = -1; inicio[s] = -1; }
            var monton = new Monton();
            for (var p = 0; p < salidas.Count; p++)
            {
                int ci, cj;
                if (!Buscar(gx, salidas[p].Cabo.X, out ci) || !Buscar(gy, salidas[p].Cabo.Y, out cj) || !libre[cj * nx + ci]) continue;
                var estado = (cj * nx + ci) * 4 + salidas[p].Salida;
                var c0 = puertosUsados.Contains(salidas[p].Clave) ? CostePuertoUsado : 0f;
                if (c0 < coste[estado]) { coste[estado] = c0; inicio[estado] = p; monton.Meter(c0, estado); }
            }

            // Los cabos de entrada: donde se acaba, con el giro que cuesta entrar de frente.
            var final = new Dictionary<int, List<int>>();
            for (var p = 0; p < entradas.Count; p++)
            {
                int ci, cj;
                if (!Buscar(gx, entradas[p].Cabo.X, out ci) || !Buscar(gy, entradas[p].Cabo.Y, out cj) || !libre[cj * nx + ci]) continue;
                List<int> lista;
                if (!final.TryGetValue(cj * nx + ci, out lista)) final[cj * nx + ci] = lista = new List<int>();
                lista.Add(p);
            }

            var mejor = float.MaxValue; var estadoFinal = -1; var puertoFinal = -1;
            float c1; int e1;
            while (monton.Sacar(out c1, out e1))
            {
                if (c1 > coste[e1] + 0.001f || c1 >= mejor) continue;
                var nodo = e1 / 4; var dir = e1 % 4;
                List<int> destinos;
                if (final.TryGetValue(nodo, out destinos))
                {
                    foreach (var p in destinos)
                    {
                        var entra = (entradas[p].Salida + 2) % 4;   // entrar a la caja es ir al reves de salir
                        var total = c1 + (dir != entra ? CosteGiro : 0f) + (puertosUsados.Contains(entradas[p].Clave) ? CostePuertoUsado : 0f);
                        if (total < mejor) { mejor = total; estadoFinal = e1; puertoFinal = p; }
                    }
                }
                var i = nodo % nx; var j = nodo / nx;
                for (var d = 0; d < 4; d++)
                {
                    if (d == (dir + 2) % 4) continue;   // sin dar la vuelta
                    var i2 = i + Dx[d]; var j2 = j + Dy[d];
                    if (i2 < 0 || j2 < 0 || i2 >= nx || j2 >= ny || !libre[j2 * nx + i2]) continue;
                    if (ChocaConCaja(gx[i], gy[j], gx[i2], gy[j2], obstaculos)) continue;
                    var largo = Math.Abs(gx[i2] - gx[i]) + Math.Abs(gy[j2] - gy[j]);
                    var nuevo = c1 + largo + (d != dir ? CosteGiro : 0f) + Penalizacion(gx[i], gy[j], gx[i2], gy[j2], trazadas);
                    var e2 = (j2 * nx + i2) * 4 + d;
                    if (nuevo < coste[e2]) { coste[e2] = nuevo; previo[e2] = e1; inicio[e2] = inicio[e1]; monton.Meter(nuevo, e2); }
                }
            }
            if (estadoFinal < 0) return null;

            // ---- de vuelta: los nodos del camino
            var nodos = new List<PuntoDeRuta>();
            for (var s = estadoFinal; s >= 0; s = previo[s]) { var n = s / 4; nodos.Add(new PuntoDeRuta(gx[n % nx], gy[n / nx])); }
            nodos.Reverse();
            var salida = salidas[inicio[estadoFinal]]; var entrada = entradas[puertoFinal];
            puertosUsados.Add(salida.Clave); puertosUsados.Add(entrada.Clave);
            var ruta = new List<PuntoDeRuta> { salida.Borde };
            ruta.AddRange(nodos);
            ruta.Add(entrada.Borde);
            return SinPuntosSobrantes(ruta);
        }

        /// <summary>Lo que cuesta pasar por un tramo de la rejilla: cruzar otra flecha, o ir montado encima de ella.</summary>
        private static float Penalizacion(float x1, float y1, float x2, float y2, List<Tramo> trazadas)
        {
            var coste = 0f;
            var horizontal = Math.Abs(y1 - y2) < 0.01f;
            foreach (var t in trazadas)
            {
                if (horizontal == t.Horizontal)
                {
                    // Paralelos: si van por el mismo pasillo y se tocan, se montan.
                    if (horizontal && Math.Abs(t.Y1 - y1) < 6f) coste += Solape(x1, x2, t.X1, t.X2);
                    else if (!horizontal && Math.Abs(t.X1 - x1) < 6f) coste += Solape(y1, y2, t.Y1, t.Y2);
                }
                else if (horizontal)
                {
                    // Este es horizontal y t vertical: se cruzan si t.X esta dentro de [x1,x2] y y1 dentro de [t.Y1,t.Y2], sin ser un extremo.
                    if (Dentro(t.X1, x1, x2) && Dentro(y1, t.Y1, t.Y2)) coste += CosteCruce;
                }
                else
                {
                    if (Dentro(t.Y1, y1, y2) && Dentro(x1, t.X1, t.X2)) coste += CosteCruce;
                }
            }
            return coste;
        }

        private static float Solape(float a1, float a2, float b1, float b2)
        {
            var lo = Math.Max(Math.Min(a1, a2), Math.Min(b1, b2));
            var hi = Math.Min(Math.Max(a1, a2), Math.Max(b1, b2));
            return hi - lo > 1f ? CosteSolapeFijo + (hi - lo) * CosteSolape : 0f;
        }

        /// <summary>v esta estrictamente entre a y b (sea cual sea el orden).</summary>
        private static bool Dentro(float v, float a, float b)
        {
            return v > Math.Min(a, b) + 0.5f && v < Math.Max(a, b) - 0.5f;
        }

        // ====================================================================== piezas

        private static List<Puerto> Puertos(CajaDeRuta c, float margen)
        {
            var lista = new List<Puerto>();
            for (var r = 0; r < Ranuras.Length; r++)
            {
                var f = Ranuras[r];
                var cx = c.X + c.Ancho * f; var cy = c.Y + c.Alto * f;
                lista.Add(new Puerto { Borde = new PuntoDeRuta(cx, c.Y), Cabo = new PuntoDeRuta(cx, c.Y - margen), Salida = 3, Clave = c.Id + "|arriba|" + r });
                lista.Add(new Puerto { Borde = new PuntoDeRuta(cx, c.Y + c.Alto), Cabo = new PuntoDeRuta(cx, c.Y + c.Alto + margen), Salida = 1, Clave = c.Id + "|abajo|" + r });
                lista.Add(new Puerto { Borde = new PuntoDeRuta(c.X, cy), Cabo = new PuntoDeRuta(c.X - margen, cy), Salida = 2, Clave = c.Id + "|izquierda|" + r });
                lista.Add(new Puerto { Borde = new PuntoDeRuta(c.X + c.Ancho, cy), Cabo = new PuntoDeRuta(c.X + c.Ancho + margen, cy), Salida = 0, Clave = c.Id + "|derecha|" + r });
            }
            return lista;
        }

        /// <summary>Los pasillos entre las cajas: el medio de cada hueco, y un carril a cada lado, si el hueco es ancho.</summary>
        private static void AnadirPasillos(List<float> coordenadas, List<float[]> intervalos, float margen)
        {
            var orden = intervalos.OrderBy(v => v[0]).ToList();
            var finActual = float.NaN;
            foreach (var iv in orden)
            {
                if (float.IsNaN(finActual)) { finActual = iv[1]; continue; }
                if (iv[0] > finActual)
                {
                    var hueco = iv[0] - finActual; var medio = (iv[0] + finActual) / 2f;
                    coordenadas.Add(medio);
                    if (hueco > 2 * margen + 40f) { coordenadas.Add(medio - 17f); coordenadas.Add(medio + 17f); }
                }
                finActual = Math.Max(finActual, iv[1]);
            }
        }

        private static List<float> Unicos(List<float> v)
        {
            var orden = v.OrderBy(x => x).ToList();
            var res = new List<float>();
            foreach (var x in orden) if (res.Count == 0 || x - res[res.Count - 1] > 0.5f) res.Add(x);
            return res;
        }

        private static bool Buscar(List<float> v, float x, out int indice)
        {
            indice = -1;
            for (var i = 0; i < v.Count; i++) if (Math.Abs(v[i] - x) < 0.6f) { indice = i; return true; }
            return false;
        }

        private static bool ChocaConCaja(float x1, float y1, float x2, float y2, List<float[]> obstaculos)
        {
            foreach (var o in obstaculos)
            {
                if (Math.Abs(y1 - y2) < 0.01f)
                {
                    if (y1 > o[1] + 0.01f && y1 < o[3] - 0.01f && Math.Max(x1, x2) > o[0] + 0.01f && Math.Min(x1, x2) < o[2] - 0.01f) return true;
                }
                else if (x1 > o[0] + 0.01f && x1 < o[2] - 0.01f && Math.Max(y1, y2) > o[1] + 0.01f && Math.Min(y1, y2) < o[3] - 0.01f) return true;
            }
            return false;
        }

        private static float Distancia(CajaDeRuta a, CajaDeRuta b)
        {
            return Math.Abs(a.X + a.Ancho / 2 - b.X - b.Ancho / 2) + Math.Abs(a.Y + a.Alto / 2 - b.Y - b.Alto / 2);
        }

        /// <summary>Si no hay camino (no deberia): la recta de siempre, de borde a borde.</summary>
        private static List<PuntoDeRuta> Recta(CajaDeRuta a, CajaDeRuta b)
        {
            var ca = new PuntoDeRuta(a.X + a.Ancho / 2, a.Y + a.Alto / 2); var cb = new PuntoDeRuta(b.X + b.Ancho / 2, b.Y + b.Alto / 2);
            return new List<PuntoDeRuta> { Corte(a, ca, cb), Corte(b, cb, ca) };
        }

        private static PuntoDeRuta Corte(CajaDeRuta c, PuntoDeRuta desde, PuntoDeRuta hacia)
        {
            var dx = hacia.X - desde.X; var dy = hacia.Y - desde.Y;
            var tx = Math.Abs(dx) > 0.01f ? (c.Ancho / 2) / Math.Abs(dx) : 1e9f;
            var ty = Math.Abs(dy) > 0.01f ? (c.Alto / 2) / Math.Abs(dy) : 1e9f;
            var t = Math.Min(tx, ty);
            return new PuntoDeRuta(desde.X + dx * t, desde.Y + dy * t);
        }

        private static List<PuntoDeRuta> SinPuntosSobrantes(List<PuntoDeRuta> ruta)
        {
            var res = new List<PuntoDeRuta>();
            foreach (var p in ruta)
            {
                if (res.Count > 0 && Math.Abs(res[res.Count - 1].X - p.X) < 0.01f && Math.Abs(res[res.Count - 1].Y - p.Y) < 0.01f) continue;
                if (res.Count >= 2)
                {
                    var a = res[res.Count - 2]; var b = res[res.Count - 1];
                    var mismaX = Math.Abs(a.X - b.X) < 0.01f && Math.Abs(b.X - p.X) < 0.01f;
                    var mismaY = Math.Abs(a.Y - b.Y) < 0.01f && Math.Abs(b.Y - p.Y) < 0.01f;
                    if (mismaX || mismaY) res.RemoveAt(res.Count - 1);
                }
                res.Add(p);
            }
            return res;
        }

        // ====================================================================== las etiquetas

        /// <summary>
        /// Donde poner la etiqueta de una flecha (su centro): sobre la propia flecha, en el tramo mas largo que quede libre
        /// de cajas, de otras etiquetas y de otras flechas. 'ocupados' son rectangulos (x, y, ancho, alto); se le añade el
        /// elegido. Si nada cabe, junto a la flecha, y en el peor caso en el medio del tramo mas largo.
        /// </summary>
        public static PuntoDeRuta PuestoDeEtiqueta(IList<PuntoDeRuta> ruta, float ancho, float alto, List<float[]> ocupados, IEnumerable<IList<PuntoDeRuta>> otras)
        {
            var propios = Tramos(ruta).OrderByDescending(t => Math.Abs(t.X2 - t.X1) + Math.Abs(t.Y2 - t.Y1)).ToList();
            var ajenos = otras == null ? new List<Tramo>() : otras.SelectMany(r => Tramos(r)).ToList();
            var fracciones = new[] { 0.5f, 0.38f, 0.62f, 0.26f, 0.74f, 0.15f, 0.85f };
            var lados = new[] { 0f, -(alto / 2 + 14), alto / 2 + 14 };
            foreach (var lado in lados)
                foreach (var t in propios)
                    foreach (var f in fracciones)
                    {
                        var px = t.X1 + (t.X2 - t.X1) * f; var py = t.Y1 + (t.Y2 - t.Y1) * f;
                        if (lado != 0f) { if (t.Horizontal) py += lado; else px += lado; }
                        var r = new[] { px - ancho / 2 - 6, py - alto / 2 - 4, ancho + 12, alto + 8 };
                        if (ocupados.Any(o => Solapa(o, r))) continue;
                        if (ajenos.Any(x => TocaRect(x, r))) continue;
                        ocupados.Add(r);
                        return new PuntoDeRuta(px, py);
                    }
            var mayor = propios.Count > 0 ? propios[0] : new Tramo();
            var p = new PuntoDeRuta((mayor.X1 + mayor.X2) / 2, (mayor.Y1 + mayor.Y2) / 2);
            ocupados.Add(new[] { p.X - ancho / 2 - 6, p.Y - alto / 2 - 4, ancho + 12, alto + 8 });
            return p;
        }

        private static IEnumerable<Tramo> Tramos(IList<PuntoDeRuta> r)
        {
            for (var i = 0; i + 1 < r.Count; i++) yield return new Tramo { X1 = r[i].X, Y1 = r[i].Y, X2 = r[i + 1].X, Y2 = r[i + 1].Y };
        }

        private static bool Solapa(float[] a, float[] b)
        {
            return a[0] < b[0] + b[2] && b[0] < a[0] + a[2] && a[1] < b[1] + b[3] && b[1] < a[1] + a[3];
        }

        private static bool TocaRect(Tramo t, float[] r)
        {
            var minX = Math.Min(t.X1, t.X2); var maxX = Math.Max(t.X1, t.X2);
            var minY = Math.Min(t.Y1, t.Y2); var maxY = Math.Max(t.Y1, t.Y2);
            return maxX > r[0] && minX < r[0] + r[2] && maxY > r[1] && minY < r[1] + r[3];
        }

        // ====================================================================== para las pruebas

        /// <summary>Cuantas veces se cruzan dos rutas (sin contar tocarse en un extremo).</summary>
        public static int Cruces(IList<PuntoDeRuta> a, IList<PuntoDeRuta> b)
        {
            var n = 0;
            foreach (var s in Tramos(a))
                foreach (var t in Tramos(b))
                {
                    if (s.Horizontal == t.Horizontal) continue;
                    var h = s.Horizontal ? s : t; var v = s.Horizontal ? t : s;
                    if (Dentro(v.X1, h.X1, h.X2) && Dentro(h.Y1, v.Y1, v.Y2)) n++;
                }
            return n;
        }

        /// <summary>Cuantas de estas cajas atraviesa la ruta (se pasa la lista sin la de salida ni la de entrada).</summary>
        public static int CajasAtravesadas(IList<PuntoDeRuta> ruta, IEnumerable<CajaDeRuta> cajas)
        {
            var n = 0;
            foreach (var c in cajas)
            {
                var r = new[] { c.X + 1f, c.Y + 1f, c.Ancho - 2f, c.Alto - 2f };
                if (Tramos(ruta).Any(t => TocaRect(t, r))) n++;
            }
            return n;
        }

        // ====================================================================== un monton minimo
        private sealed class Monton
        {
            private readonly List<KeyValuePair<float, int>> _v = new List<KeyValuePair<float, int>>();

            public void Meter(float coste, int estado)
            {
                _v.Add(new KeyValuePair<float, int>(coste, estado));
                var i = _v.Count - 1;
                while (i > 0)
                {
                    var p = (i - 1) / 2;
                    if (_v[p].Key <= _v[i].Key) break;
                    var t = _v[p]; _v[p] = _v[i]; _v[i] = t; i = p;
                }
            }

            public bool Sacar(out float coste, out int estado)
            {
                coste = 0; estado = -1;
                if (_v.Count == 0) return false;
                coste = _v[0].Key; estado = _v[0].Value;
                var ultimo = _v[_v.Count - 1]; _v.RemoveAt(_v.Count - 1);
                if (_v.Count == 0) return true;
                _v[0] = ultimo;
                var i = 0;
                while (true)
                {
                    var l = 2 * i + 1; var r = l + 1; var m = i;
                    if (l < _v.Count && _v[l].Key < _v[m].Key) m = l;
                    if (r < _v.Count && _v[r].Key < _v[m].Key) m = r;
                    if (m == i) break;
                    var t = _v[m]; _v[m] = _v[i]; _v[i] = t; i = m;
                }
                return true;
            }
        }
    }
}
