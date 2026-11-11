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
            d.Polilinea(new List<Vector2>(pts), filo, 2.2f);
            // El brillo: una rayita en el lobulo de arriba.
            var bx = cx + (izquierda ? -1 : 1) * 7.5f * escala; var by = cy - 5.6f * escala;
            d.Linea(bx - 2f * escala, by - 1.5f * escala, bx - 1f * escala * (izquierda ? 1 : -1), by + 2f * escala, new Color(1, 1, 1, 0.45f), 2f);
        }

        public static void Corazon(DibujoUI d, float cx, float cy, float escala, Color relleno, Color filo) {
            MitadDeCorazon(d, true, cx, cy, escala, relleno, filo);
            MitadDeCorazon(d, false, cx, cy, escala, relleno, filo);
        }

        // ---------------------------------------------------------------- candado

        public static void Candado(DibujoUI d, float cx, float cy, Color col, float esc) {
            var w = 22f * esc; var h = 17f * esc;
            d.Rect(cx - w / 2, cy - h / 2 + 5 * esc, w, h, col, 3f, new Color(col.r, col.g, col.b, 0.22f), false, 4);
            d.Curva(new Vector2(cx - 6.5f * esc, cy - h / 2 + 5 * esc), new Vector2(cx, cy - h / 2 - 14 * esc), new Vector2(cx + 6.5f * esc, cy - h / 2 + 5 * esc), col, 3f, false);
            d.Circulo(cx, cy + 4 * esc, 2.4f * esc, col, 0, col);
        }

        // ---------------------------------------------------------------- el camion del cliente

        /// <summary>El camion de reparto mirando a la derecha. (x, y) es la esquina de arriba a la izquierda; mide 132×72.</summary>
        public static void Camion(DibujoUI d, float x, float y, Color carroceria, Color luz) {
            var oscuro = Color.Lerp(carroceria, Color.black, 0.55f);
            var claro = Color.Lerp(carroceria, Color.white, 0.25f);
            // La caja de carga.
            d.Rect(x, y + 4, 84, 52, oscuro, 3, carroceria, false, 6);
            d.Linea(x + 6, y + 14, x + 78, y + 14, new Color(1, 1, 1, 0.18f), 2.5f);
            d.Linea(x + 6, y + 30, x + 78, y + 30, new Color(0, 0, 0, 0.25f), 2f);
            // La cabina, con su ventana.
            d.Poligono(new List<Vector2> { new Vector2(x + 86, y + 18), new Vector2(x + 108, y + 18), new Vector2(x + 128, y + 36), new Vector2(x + 128, y + 56), new Vector2(x + 86, y + 56) }, claro);
            d.Polilinea(new List<Vector2> { new Vector2(x + 86, y + 56), new Vector2(x + 86, y + 18), new Vector2(x + 108, y + 18), new Vector2(x + 128, y + 36), new Vector2(x + 128, y + 56), new Vector2(x + 86, y + 56) }, oscuro, 3f);
            d.Poligono(new List<Vector2> { new Vector2(x + 94, y + 24), new Vector2(x + 106, y + 24), new Vector2(x + 119, y + 36), new Vector2(x + 94, y + 36) }, new Color(0.1f, 0.2f, 0.28f, 0.95f));
            // Los faros y las ruedas.
            d.Circulo(x + 127, y + 48, 4.5f, luz, 0, luz);
            d.Circulo(x + 3, y + 48, 3f, new Color(1f, 0.2f, 0.2f), 0, new Color(1f, 0.2f, 0.2f));
            foreach (var rx in new[] { 24f, 62f, 106f }) {
                d.Circulo(x + rx, y + 60, 11, Color.black, 3, new Color(0.08f, 0.1f, 0.13f));
                d.Circulo(x + rx, y + 60, 4.2f, new Color(0.7f, 0.75f, 0.8f), 0, new Color(0.7f, 0.75f, 0.8f));
            }
        }

        // ---------------------------------------------------------------- el fondo lejano

        /// <summary>Edificios lejanos (siluetas con ventanas encendidas) entre x0 y x1, apoyados en baseY.</summary>
        public static void Edificios(DibujoUI d, float x0, float x1, float baseY, float altoMax, int semilla, Color cuerpo, Color ventana) {
            var azar = new System.Random(semilla);
            var x = x0;
            while (x < x1) {
                var w = 46f + (float)azar.NextDouble() * 70f;
                var h = altoMax * (0.35f + 0.65f * (float)azar.NextDouble());
                if (x + w > x1) w = x1 - x;
                if (w < 20) break;
                d.Poligono(new List<Vector2> { new Vector2(x, baseY), new Vector2(x + w, baseY), new Vector2(x + w, baseY - h), new Vector2(x, baseY - h) }, cuerpo);
                for (var fy = baseY - h + 12; fy < baseY - 10; fy += 17)
                    for (var fx = x + 8; fx < x + w - 8; fx += 14)
                        if (azar.NextDouble() < 0.16) d.Rect(fx, fy, 6, 8, new Color(0, 0, 0, 0), 0, ventana, false, 1);
                x += w + 6f + (float)azar.NextDouble() * 18f;
            }
        }

        /// <summary>Una pantalla holografica tenue colgada en el aire: un marco y unas lineas de texto.</summary>
        public static void PantallaHolografica(DibujoUI d, float x, float y, float w, float h, Color color, int semilla) {
            d.Rect(x, y, w, h, new Color(color.r, color.g, color.b, 0.5f), 1.8f, new Color(color.r, color.g, color.b, 0.07f), false, 4);
            var azar = new System.Random(semilla);
            for (var ly = y + 10; ly < y + h - 8; ly += 9) {
                var largo = (w - 20) * (0.35f + 0.6f * (float)azar.NextDouble());
                d.Linea(x + 10, ly, x + 10 + largo, ly, new Color(color.r, color.g, color.b, 0.3f), 2f);
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
                d.Linea(P(ea, ya).x, ya, P(eb, yb).x, yb, new Color(luz.r, luz.g, luz.b, 0.22f), 2f);
                d.Linea(P(ea * 0.55f, ya + 8).x, ya + 8, P(ea * 0.35f, yb - 10).x, yb - 10, new Color(0, 0, 0, 0.25f), 3f);
            }
            // El borde de arriba, con un reborde claro.
            d.Linea(P(0, y0).x, y0, P(ancho + 10, y0).x, y0, new Color(luz.r, luz.g, luz.b, 0.5f), 3f);
        }

        // ---------------------------------------------------------------- la losa del puente

        /// <summary>La celosia que sujeta una losa por debajo: dos cordones y diagonales en zig-zag.</summary>
        public static void Celosia(DibujoUI d, float x0, float x1, float y0, float y1, Color color) {
            d.Linea(x0, y0, x1, y0, color, 3f);
            d.Linea(x0, y1, x1, y1, color, 3f);
            var paso = (y1 - y0) * 1.6f;
            var arriba = true;
            for (var x = x0; x + paso <= x1; x += paso) {
                d.Linea(x, arriba ? y0 : y1, x + paso, arriba ? y1 : y0, new Color(color.r, color.g, color.b, color.a * 0.75f), 2.2f);
                arriba = !arriba;
            }
        }

        /// <summary>Una barandilla: el pasamanos y los postes cada cierto paso.</summary>
        public static void Barandilla(DibujoUI d, float x0, float x1, float y, float alto, Color color) {
            d.Linea(x0, y, x1, y, color, 2.5f);
            d.Linea(x0, y + alto * 0.5f, x1, y + alto * 0.5f, new Color(color.r, color.g, color.b, color.a * 0.6f), 1.8f);
            for (var x = x0; x <= x1; x += 54f) d.Linea(x, y, x, y + alto, color, 2.2f);
        }
    }
}
