using System.Collections.Generic;
using Nexus.Unity.Tema;
using UnityEngine;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>
    /// Los dibujos del puente del backlog (2026-11-10), todos por codigo con DibujoUI (origen arriba a la izquierda, y
    /// hacia abajo): el corazon de vida (y sus dos mitades, que se separan al perderlo), el candado, el camion del
    /// cliente, los edificios lejanos con sus pantallas, los riscos de los acantilados y la celosia de la losa.
    /// Estaticos y sin tema: los colores llegan por parametro.
    /// </summary>
    public static class DibujosDelPuente {
        // ---------------------------------------------------------------- el corazon

        /// <summary>El contorno del corazon (curva clasica), de t0 a t1, centrado en (cx, cy) y con la y hacia abajo.</summary>
        private static List<Vector2> ContornoDeCorazon(float cx, float cy, float escala, float t0, float t1, int n) {
            var pts = new List<Vector2>();
            for (var i = 0; i <= n; i++) {
                var t = Mathf.Lerp(t0, t1, i / (float)n);
                var x = 16f * Mathf.Pow(Mathf.Sin(t), 3);
                var y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2 * t) - 2f * Mathf.Cos(3 * t) - Mathf.Cos(4 * t);
                pts.Add(new Vector2(cx + x * escala, cy - y * escala));
            }
            return pts;
        }

        /// <summary>Una mitad del corazon (la izquierda o la derecha), rellena, con su contorno y un brillo.</summary>
        public static void MitadDeCorazon(DibujoUI d, bool izquierda, float cx, float cy, float escala, Color relleno, Color filo) {
            // Con x = 16 sin^3 t, la mitad izquierda es t de π a 2π y la derecha, t de 0 a π.
            var pts = izquierda ? ContornoDeCorazon(cx, cy, escala, Mathf.PI, 2 * Mathf.PI, 28) : ContornoDeCorazon(cx, cy, escala, 0, Mathf.PI, 28);
            d.Poligono(new List<Vector2>(pts), relleno);
            d.Polilinea(new List<Vector2>(pts), filo, 3.2f);
            // Dos tonos, como en el cartel: una mancha clara en el lobulo y una franja de sombra abajo.
            var bx = cx + (izquierda ? -1 : 1) * 7.5f * escala; var by = cy - 5.6f * escala;
            d.Elipse(bx, by, 2.6f * escala, 3.6f * escala, new Color(0, 0, 0, 0), 0, new Color(1, 1, 1, 0.5f));
            d.Elipse(cx + (izquierda ? -1 : 1) * 5f * escala, cy + 7f * escala, 5.5f * escala, 2.2f * escala, new Color(0, 0, 0, 0), 0, new Color(0, 0, 0, 0.18f));
        }

        public static void Corazon(DibujoUI d, float cx, float cy, float escala, Color relleno, Color filo) {
            MitadDeCorazon(d, true, cx, cy, escala, relleno, filo);
            MitadDeCorazon(d, false, cx, cy, escala, relleno, filo);
        }

        // ---------------------------------------------------------------- candado

        public static void Candado(DibujoUI d, float cx, float cy, Color col, float esc) {
            var w = 24f * esc; var h = 18f * esc;
            var oscuro = Color.Lerp(col, Color.black, 0.55f);
            // El arco: trazo grueso (se lee de lejos, como un rotulo de obra).
            d.Curva(new Vector2(cx - 7f * esc, cy - h / 2 + 5 * esc), new Vector2(cx, cy - h / 2 - 17 * esc), new Vector2(cx + 7f * esc, cy - h / 2 + 5 * esc), col, 4.5f, false);
            // El cuerpo en dos tonos: cara clara y zocalo oscuro.
            d.Rect(cx - w / 2, cy - h / 2 + 5 * esc, w, h, oscuro, 3f, new Color(col.r, col.g, col.b, 0.9f), false, 3);
            d.Poligono(new List<Vector2> { new Vector2(cx - w / 2 + 1.5f, cy + h / 2 - 3 * esc), new Vector2(cx + w / 2 - 1.5f, cy + h / 2 - 3 * esc), new Vector2(cx + w / 2 - 1.5f, cy + h / 2 + 3.5f * esc), new Vector2(cx - w / 2 + 1.5f, cy + h / 2 + 3.5f * esc) }, oscuro);
            d.Circulo(cx, cy + 1.5f * esc, 2.6f * esc, oscuro, 0, oscuro);
            d.Linea(cx, cy + 2f * esc, cx, cy + 7f * esc, oscuro, 2.6f * esc);
        }

        // ---------------------------------------------------------------- el camion del cliente

        /// <summary>El camion de reparto mirando a la derecha. (x, y) es la esquina de arriba a la izquierda; mide 132×72.</summary>
        public static void Camion(DibujoUI d, float x, float y, Color carroceria, Color luz) {
            var oscuro = Color.Lerp(carroceria, Color.black, 0.6f);
            var sombra = Color.Lerp(carroceria, Color.black, 0.28f);
            var claro = Color.Lerp(carroceria, Color.white, 0.3f);
            // La caja de carga: cara clara arriba, sombra abajo (dos tonos), franja de rotulo y puertas.
            d.Rect(x, y + 4, 84, 52, oscuro, 3.5f, carroceria, false, 3);
            d.Poligono(new List<Vector2> { new Vector2(x + 2, y + 38), new Vector2(x + 82, y + 38), new Vector2(x + 82, y + 54), new Vector2(x + 2, y + 54) }, sombra);
            d.Poligono(new List<Vector2> { new Vector2(x + 2, y + 24), new Vector2(x + 82, y + 24), new Vector2(x + 82, y + 31), new Vector2(x + 2, y + 31) }, new Color(1, 1, 1, 0.22f));
            d.Linea(x + 42, y + 6, x + 42, y + 54, oscuro, 2.5f);
            // La cabina: parabrisas inclinado, techo, vidrio oscuro con un destello.
            var cab = new List<Vector2> { new Vector2(x + 86, y + 56), new Vector2(x + 86, y + 18), new Vector2(x + 108, y + 18), new Vector2(x + 128, y + 36), new Vector2(x + 128, y + 56) };
            d.Poligono(new List<Vector2>(cab), claro);
            d.Poligono(new List<Vector2> { new Vector2(x + 86, y + 46), new Vector2(x + 128, y + 46), new Vector2(x + 128, y + 56), new Vector2(x + 86, y + 56) }, sombra);
            cab.Add(cab[0]); d.Polilinea(cab, oscuro, 3.5f);
            d.Poligono(new List<Vector2> { new Vector2(x + 93, y + 24), new Vector2(x + 106, y + 24), new Vector2(x + 119, y + 37), new Vector2(x + 93, y + 37) }, new Color(0.06f, 0.14f, 0.2f, 1f));
            d.Linea(x + 97, y + 33, x + 103, y + 27, new Color(1, 1, 1, 0.45f), 2.2f);
            // Parachoques, faro y piloto.
            d.Rect(x + 120, y + 52, 12, 5, oscuro, 0, oscuro, false, 1);
            d.Circulo(x + 126, y + 42, 4.5f, oscuro, 1.5f, luz);
            d.Rect(x - 1, y + 44, 5, 9, oscuro, 0, new Color(0.95f, 0.25f, 0.2f), false, 1);
            // Ruedas con tapacubos.
            foreach (var rx in new[] { 24f, 62f, 106f }) {
                d.Circulo(x + rx, y + 60, 11, new Color(0.04f, 0.05f, 0.06f), 3.5f, new Color(0.1f, 0.12f, 0.15f));
                d.Circulo(x + rx, y + 60, 4.8f, oscuro, 0, new Color(0.78f, 0.82f, 0.85f));
            }
        }

        // ---------------------------------------------------------------- el fondo lejano

        /// <summary>
        /// Torres lejanas entre x0 y x1, apoyadas en baseY: cuerpos escalonados en dos tonos (la cara de la sombra mas oscura),
        /// bandas de luz por pisos en vez de ventanas sueltas y, de cuando en cuando, una antena con su baliza.
        /// </summary>
        public static void Edificios(DibujoUI d, float x0, float x1, float baseY, float altoMax, int semilla, Color cuerpo, Color ventana) {
            var azar = new System.Random(semilla);
            var sombra = Color.Lerp(cuerpo, Color.black, 0.32f);
            var x = x0;
            while (x < x1) {
                var w = 46f + (float)azar.NextDouble() * 70f;
                var h = altoMax * (0.35f + 0.65f * (float)azar.NextDouble());
                if (x + w > x1) w = x1 - x;
                if (w < 20) break;
                var escalon = azar.NextDouble() < 0.55f;
                var wc = escalon ? w * 0.7f : w;              // el cuerpo de arriba, mas estrecho
                var xc = x + (w - wc) * 0.5f;
                var hb = escalon ? h * 0.62f : h;             // la base escalonada
                d.Poligono(new List<Vector2> { new Vector2(x, baseY), new Vector2(x + w, baseY), new Vector2(x + w, baseY - hb), new Vector2(x, baseY - hb) }, cuerpo);
                d.Poligono(new List<Vector2> { new Vector2(x + w * 0.72f, baseY), new Vector2(x + w, baseY), new Vector2(x + w, baseY - hb), new Vector2(x + w * 0.72f, baseY - hb) }, sombra);
                if (escalon) {
                    d.Poligono(new List<Vector2> { new Vector2(xc, baseY - hb), new Vector2(xc + wc, baseY - hb), new Vector2(xc + wc, baseY - h), new Vector2(xc, baseY - h) }, cuerpo);
                    d.Poligono(new List<Vector2> { new Vector2(xc + wc * 0.72f, baseY - hb), new Vector2(xc + wc, baseY - hb), new Vector2(xc + wc, baseY - h), new Vector2(xc + wc * 0.72f, baseY - h) }, sombra);
                }
                // Bandas de luz: una rendija continua por piso, con tramos apagados.
                for (var fy = baseY - h + 14; fy < baseY - 10; fy += 16) {
                    var enBase = fy > baseY - hb;
                    var bx0 = enBase ? x : xc; var bw = enBase ? w : wc;
                    if (azar.NextDouble() < 0.45f) continue;
                    var ini = bx0 + 6 + (float)azar.NextDouble() * bw * 0.2f;
                    var fin = bx0 + bw - 6 - (float)azar.NextDouble() * bw * 0.3f;
                    if (fin - ini > 8) d.Poligono(new List<Vector2> { new Vector2(ini, fy), new Vector2(fin, fy), new Vector2(fin, fy + 5), new Vector2(ini, fy + 5) }, ventana);
                }
                // Antena con baliza en algunas azoteas.
                if (azar.NextDouble() < 0.28f) {
                    var ax = xc + wc * 0.5f;
                    d.Linea(ax, baseY - h, ax, baseY - h - 16, sombra, 3f);
                    d.Circulo(ax, baseY - h - 18, 3f, new Color(0, 0, 0, 0), 0, new Color(1f, 0.8f, 0.3f, 0.85f));
                }
                x += w + 6f + (float)azar.NextDouble() * 18f;
            }
        }

        /// <summary>Una pantalla holografica tenue colgada en el aire: un marco y unas lineas de texto.</summary>
        public static void PantallaHolografica(DibujoUI d, float x, float y, float w, float h, Color color, int semilla) {
            // Un rotulo luminoso: panel tenue, marco grueso con esquineros y barras de texto en vez de hilos.
            d.Rect(x, y, w, h, new Color(color.r, color.g, color.b, 0.55f), 3f, new Color(color.r, color.g, color.b, 0.09f), false, 3);
            var e = Mathf.Min(14f, w * 0.12f); var c = new Color(color.r, color.g, color.b, 0.9f);
            d.Polilinea(new List<Vector2> { new Vector2(x - 3, y + e), new Vector2(x - 3, y - 3), new Vector2(x + e, y - 3) }, c, 4f);
            d.Polilinea(new List<Vector2> { new Vector2(x + w + 3 - e, y + h + 3), new Vector2(x + w + 3, y + h + 3), new Vector2(x + w + 3, y + h + 3 - e) }, c, 4f);
            var azar = new System.Random(semilla);
            for (var ly = y + 12; ly < y + h - 14; ly += 14) {
                var largo = (w - 24) * (0.3f + 0.62f * (float)azar.NextDouble());
                d.Poligono(new List<Vector2> { new Vector2(x + 12, ly), new Vector2(x + 12 + largo, ly), new Vector2(x + 12 + largo, ly + 6), new Vector2(x + 12, ly + 6) }, new Color(color.r, color.g, color.b, 0.34f));
            }
        }

        // ---------------------------------------------------------------- los acantilados

        /// <summary>Un acantilado a un lado: capas de roca en cuadrilateros (convexos) con grietas, desde y0 hasta el fondo.</summary>
        public static void Risco(DibujoUI d, bool izquierda, float ancho, float y0, float alto, float w, Color roca, Color luz) {
            Vector2 P(float fx, float fy) { return new Vector2(izquierda ? fx : w - fx, fy); }
            var capas = 6;
            for (var i = 0; i < capas; i++) {
                var ya = y0 + alto * i / capas; var yb = y0 + alto * (i + 1) / capas;
                var ea = ancho * (1f - 0.22f * i / capas) + (i % 2 == 0 ? 10f : -8f);
                var eb = ancho * (1f - 0.22f * (i + 1) / capas) + ((i + 1) % 2 == 0 ? 12f : -6f);
                var tono = Color.Lerp(roca, Color.black, 0.07f * i);
                d.Poligono(new List<Vector2> { P(0, ya), P(ea, ya), P(eb, yb), P(0, yb) }, tono);
                // Dos tonos por estrato: una cara de sombra hacia el vacio, en vez de grietas finas.
                var ca = ea * 0.72f; var cb = eb * 0.72f;
                d.Poligono(new List<Vector2> { P(ca, ya), P(ea, ya), P(eb, yb), P(cb, yb) }, Color.Lerp(tono, Color.black, 0.3f));
                d.Poligono(new List<Vector2> { P(0, ya), P(ea * 0.5f, ya), P(eb * 0.5f, ya + (yb - ya) * 0.22f), P(0, ya + (yb - ya) * 0.22f) }, Color.Lerp(tono, Color.white, 0.06f));
                d.Linea(P(ea, ya).x, ya, P(eb, yb).x, yb, new Color(luz.r, luz.g, luz.b, 0.35f), 3.5f);
            }
            // El borde de arriba: reborde oscuro con franjas de peligro (mostaza) a lo largo.
            var fin = ancho + 10; var paso = 26f;
            d.Poligono(new List<Vector2> { P(0, y0 - 3), P(fin, y0 - 3), P(fin, y0 + 7), P(0, y0 + 7) }, Color.Lerp(roca, Color.black, 0.45f));
            for (var fx = 0f; fx + paso * 0.5f < fin; fx += paso) {
                var f1 = Mathf.Min(fx + paso * 0.5f, fin);
                d.Poligono(new List<Vector2> { P(fx, y0 - 3), P(f1, y0 - 3), P(Mathf.Max(0, f1 - 4), y0 + 7), P(Mathf.Max(0, fx - 4), y0 + 7) }, new Color(0.86f, 0.68f, 0.18f, 0.9f));
            }
            d.Linea(P(0, y0 - 3).x, y0 - 3, P(fin, y0 - 3).x, y0 - 3, new Color(luz.r, luz.g, luz.b, 0.65f), 3.5f);
        }

        // ---------------------------------------------------------------- la losa del puente

        /// <summary>La celosia que sujeta una losa por debajo: cordones gruesos, diagonales en zig-zag y placas de union remachadas.</summary>
        public static void Celosia(DibujoUI d, float x0, float x1, float y0, float y1, Color color) {
            d.Linea(x0, y0, x1, y0, color, 5f);
            d.Linea(x0, y1, x1, y1, color, 5f);
            var paso = (y1 - y0) * 1.6f;
            var arriba = true;
            var diag = new Color(color.r, color.g, color.b, color.a * 0.85f);
            for (var x = x0; x + paso <= x1; x += paso) {
                d.Linea(x, arriba ? y0 : y1, x + paso, arriba ? y1 : y0, diag, 3.6f);
                // La placa de union en cada nudo, con su remache.
                var ny = arriba ? y1 : y0;
                d.Rect(x + paso - 4, ny - 4, 8, 8, color, 0, color, false, 1);
                d.Circulo(x + paso, ny, 1.6f, new Color(0, 0, 0, 0.6f), 0, new Color(0, 0, 0, 0.6f));
                arriba = !arriba;
            }
        }

        /// <summary>Una barandilla: el pasamanos y los postes cada cierto paso, con su capuchon.</summary>
        public static void Barandilla(DibujoUI d, float x0, float x1, float y, float alto, Color color) {
            d.Linea(x0, y, x1, y, color, 4f);
            d.Linea(x0, y + alto * 0.5f, x1, y + alto * 0.5f, new Color(color.r, color.g, color.b, color.a * 0.6f), 2.6f);
            for (var x = x0; x <= x1; x += 54f) {
                d.Linea(x, y, x, y + alto, color, 3.6f);
                d.Rect(x - 3.5f, y - 3, 7, 5, color, 0, color, false, 1);
            }
        }
    }
}
