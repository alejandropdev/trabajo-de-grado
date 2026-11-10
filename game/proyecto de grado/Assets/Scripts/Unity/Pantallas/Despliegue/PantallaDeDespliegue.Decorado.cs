using System.Collections.Generic;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    public sealed partial class PantallaDeDespliegue {
        // ==================================================================== el decorado de los cuartos
        //
        // Todo dibujado por codigo (2026-11-09, ronda 2): el cuarto deja de ser un pasillo plano con dos cuadros. Hay tres
        // temas (sala de servidores, tuberias y archivo), elegidos de forma estable por el modulo, cada uno con su paleta
        // y sus props. Lo que se mueve (camara, paralaje, polvo, LEDs, bombillas) vive en AmbienteDeLosCuartos.

        /// <summary>La geometria del pasillo: la pared de fondo es el rectangulo (Fx0,Fy0)-(Fx1,Fy1) y el resto se abre en perspectiva.</summary>
        private sealed class Geo {
            public float W, H, Fx0, Fx1, Fy0, Fy1;
            /// <summary>La y de una pared en la distancia x desde el borde de la pantalla; f = 0 es el techo y f = 1 el suelo.</summary>
            public float Y(float x, float f) {
                var t = Fy0 * (x / Fx0);
                var b = H + (Fy1 - H) * (x / Fx0);
                return t + (b - t) * f;
            }
            public Vector2 P(int lado, float x, float f) { return new Vector2(lado == 0 ? x : W - x, Y(x, f)); }
            /// <summary>Cuanto mide algo a esa distancia respecto a lo que mediria pegado a la camara (1 → ~0,4).</summary>
            public float Esc(float x) { return (Y(x, 1f) - Y(x, 0f)) / H; }
        }

        private struct Paleta {
            public Color Suelo, Techo, Pared, Fondo, Panel, Acento, Tubo;
        }

        private static Color Rgb(float r, float g, float b, float a = 1f) { return new Color(r, g, b, a); }

        /// <summary>0 = sala de servidores, 1 = tuberias, 2 = archivo. Siempre el mismo para el mismo modulo.</summary>
        private static int TemaDelCuarto(string modulo) { return (int)(HashDe(modulo ?? "") % 3u); }

        private Paleta PaletaDe(int tema) {
            if (tema == 1)
                return new Paleta {
                    Suelo = Rgb(0.11f, 0.10f, 0.08f), Techo = Rgb(0.07f, 0.065f, 0.055f), Pared = Rgb(0.21f, 0.17f, 0.12f),
                    Fondo = Rgb(0.27f, 0.22f, 0.15f), Panel = Rgb(0.31f, 0.25f, 0.18f), Acento = Tema.warning, Tubo = Rgb(0.36f, 0.29f, 0.19f)
                };
            if (tema == 2)
                return new Paleta {
                    Suelo = Rgb(0.10f, 0.09f, 0.15f), Techo = Rgb(0.07f, 0.06f, 0.11f), Pared = Rgb(0.20f, 0.17f, 0.28f),
                    Fondo = Rgb(0.25f, 0.22f, 0.35f), Panel = Rgb(0.28f, 0.25f, 0.39f), Acento = Tema.violet, Tubo = Rgb(0.33f, 0.29f, 0.45f)
                };
            return new Paleta {
                Suelo = Rgb(0.075f, 0.125f, 0.185f), Techo = Rgb(0.05f, 0.085f, 0.135f), Pared = Rgb(0.125f, 0.21f, 0.30f),
                Fondo = Rgb(0.16f, 0.27f, 0.38f), Panel = Rgb(0.17f, 0.27f, 0.38f), Acento = Tema.cyan, Tubo = Rgb(0.20f, 0.33f, 0.46f)
            };
        }

        private static List<Vector2> Cuadro(Vector2 a, Vector2 b, Vector2 c, Vector2 d) { return new List<Vector2> { a, b, c, d }; }

        /// <summary>Un DibujoUI del tamaño de la sala, centrado en el padre.</summary>
        private DibujoUI CapaFija(RectTransform padre, string nombre, float w, float h, out RectTransform rt) {
            rt = Ui.Nodo(padre, nombre);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
            rt.gameObject.AddComponent<CanvasRenderer>();
            var d = rt.gameObject.AddComponent<DibujoUI>();
            d.raycastTarget = false;
            return d;
        }

        private RectTransform NodoLibre(RectTransform padre, string nombre, float w, float h) {
            var rt = Ui.Nodo(padre, nombre);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        // ---------------------------------------------------------------- el fondo: paredes, suelo, techo, puerta

        private void DecorarElFondo(Sala s, DibujoUI d, Geo g, Paleta p, int tema) {
            float w = g.W, h = g.H;
            var arista = NexusTheme.Alfa(p.Acento, 0.28f);
            var negro = Rgb(0, 0, 0);

            // ---- los cinco planos
            d.Poligono(Cuadro(new Vector2(0, h), new Vector2(w, h), new Vector2(g.Fx1, g.Fy1), new Vector2(g.Fx0, g.Fy1)), p.Suelo);
            d.Poligono(Cuadro(new Vector2(0, 0), new Vector2(w, 0), new Vector2(g.Fx1, g.Fy0), new Vector2(g.Fx0, g.Fy0)), p.Techo);
            d.Poligono(Cuadro(new Vector2(0, 0), new Vector2(g.Fx0, g.Fy0), new Vector2(g.Fx0, g.Fy1), new Vector2(0, h)), p.Pared);
            d.Poligono(Cuadro(new Vector2(w, 0), new Vector2(g.Fx1, g.Fy0), new Vector2(g.Fx1, g.Fy1), new Vector2(w, h)), p.Pared);
            d.Poligono(Cuadro(new Vector2(g.Fx0, g.Fy0), new Vector2(g.Fx1, g.Fy0), new Vector2(g.Fx1, g.Fy1), new Vector2(g.Fx0, g.Fy1)), p.Fondo);
            // Un degradado falso: la pared se oscurece hacia el techo y hacia el suelo.
            for (var lado = 0; lado < 2; lado++) {
                d.Poligono(Cuadro(g.P(lado, 0, 0f), g.P(lado, g.Fx0, 0f), g.P(lado, g.Fx0, 0.22f), g.P(lado, 0, 0.22f)), NexusTheme.Alfa(negro, 0.22f));
                d.Poligono(Cuadro(g.P(lado, 0, 0.78f), g.P(lado, g.Fx0, 0.78f), g.P(lado, g.Fx0, 1f), g.P(lado, 0, 1f)), NexusTheme.Alfa(negro, 0.2f));
            }
            d.Linea(0, 0, g.Fx0, g.Fy0, arista, 2); d.Linea(w, 0, g.Fx1, g.Fy0, arista, 2);
            d.Linea(0, h, g.Fx0, g.Fy1, arista, 2); d.Linea(w, h, g.Fx1, g.Fy1, arista, 2);
            d.Rect(g.Fx0, g.Fy0, g.Fx1 - g.Fx0, g.Fy1 - g.Fy0, arista, 2, null, false, 2);

            // ---- el suelo: baldosas con sus juntas (en perspectiva), charcos y su brillo
            for (var k = 1; k < 7; k++) d.Linea(g.Fx0 + k * (g.Fx1 - g.Fx0) / 7f, g.Fy1, w * k / 7f, h, NexusTheme.Alfa(p.Acento, 0.10f), 2);
            for (var k = 1; k < 6; k++) {
                var u = Mathf.Pow(k / 6f, 1.7f);
                var y = g.Fy1 + (h - g.Fy1) * u;
                var xl = g.Fx0 * (1f - u); var xr = w - xl;
                d.Linea(xl, y, xr, y, NexusTheme.Alfa(negro, 0.30f), 2);
            }
            Charco(d, w * 0.27f, h * 0.90f, 150, 26, p);
            Charco(d, w * 0.73f, h * 0.80f, 96, 15, p);

            // ---- las paredes: tuberias, cables, zocalo con franjas de peligro, manchas y vetas de suciedad
            var azar = new System.Random(tema * 977 + _iCuarto * 31 + 5);
            for (var lado = 0; lado < 2; lado++) {
                for (var k = 0; k < 8; k++) {
                    var x = g.Fx0 * (0.06f + 0.85f * (float)azar.NextDouble());
                    var f0 = 0.03f + 0.4f * (float)azar.NextDouble();
                    var a = g.P(lado, x, f0); var b = g.P(lado, x, f0 + 0.18f + 0.3f * (float)azar.NextDouble());
                    d.Linea(a.x, a.y, b.x, b.y, NexusTheme.Alfa(negro, 0.07f), (4f + 9f * (float)azar.NextDouble()) * g.Esc(x));
                }
                for (var k = 0; k < 3; k++) {
                    var x = g.Fx0 * (0.15f + 0.7f * (float)azar.NextDouble());
                    var q = g.P(lado, x, 0.45f + 0.35f * (float)azar.NextDouble());
                    var e = g.Esc(x);
                    d.Elipse(q.x, q.y, (26f + 30f * (float)azar.NextDouble()) * e, (46f + 60f * (float)azar.NextDouble()) * e, NexusTheme.Alfa(negro, 0f), 0, NexusTheme.Alfa(negro, 0.09f));
                }
                // El zocalo con sus franjas amarillas y negras.
                d.Poligono(Cuadro(g.P(lado, 0, 0.935f), g.P(lado, g.Fx0, 0.935f), g.P(lado, g.Fx0, 1f), g.P(lado, 0, 1f)), NexusTheme.Alfa(negro, 0.55f));
                for (var k = 0; k < 14; k += 2) {
                    var x0 = g.Fx0 * k / 14f; var x1 = g.Fx0 * (k + 1) / 14f;
                    d.Poligono(Cuadro(g.P(lado, x0, 0.94f), g.P(lado, x1, 0.94f), g.P(lado, x1, 0.995f), g.P(lado, x0, 0.995f)), NexusTheme.Alfa(Tema.warning, 0.5f));
                }
                // Dos tuberias que corren hacia el fondo, con juntas, y (una) con valvula.
                TuboDePared(d, g, lado, 0.075f, 0.022f, p, false);
                TuboDePared(d, g, lado, 0.15f, 0.016f, p, tema != 0 || lado == 0);
                // Mangueras de cables que cuelgan en comba.
                Manguera(d, g, lado, 0.04f, 0.58f, 1f, p);
                Manguera(d, g, lado, 0.50f, 0.95f, 0.7f, p);
            }

            // ---- el techo: lamparas apagadas con su cadena (la bombilla se enciende con el haz y parpadea)
            Lampara(s, d, g, w * 0.36f, g.Fy0 * 0.28f, 1.0f);
            Lampara(s, d, g, w * 0.64f, g.Fy0 * 0.28f, 1.0f);
            Lampara(s, d, g, w * 0.43f, g.Fy0 * 0.74f, 0.55f);
            Lampara(s, d, g, w * 0.57f, g.Fy0 * 0.74f, 0.55f);

            // ---- el fondo con el tema del modulo
            DecorarElFondoDelTema(s, d, g, p, tema);
        }

        private void Charco(DibujoUI d, float x, float y, float rx, float ry, Paleta p) {
            d.Elipse(x, y, rx, ry, NexusTheme.Alfa(p.Acento, 0.20f), 2, NexusTheme.Alfa(p.Acento, 0.06f));
            d.Curva(new Vector2(x - rx * 0.5f, y - ry * 0.25f), new Vector2(x, y - ry * 0.7f), new Vector2(x + rx * 0.35f, y - ry * 0.3f), NexusTheme.Alfa(Color.white, 0.22f), 2, false);
        }

        private void TuboDePared(DibujoUI d, Geo g, int lado, float f, float semi, Paleta p, bool valvula) {
            var col = NexusTheme.Alfa(p.Tubo, 0.95f);
            d.Poligono(Cuadro(g.P(lado, 0, f - semi), g.P(lado, g.Fx0, f - semi), g.P(lado, g.Fx0, f + semi), g.P(lado, 0, f + semi)), col);
            var a = g.P(lado, 0, f - semi * 0.45f); var b = g.P(lado, g.Fx0, f - semi * 0.45f);
            d.Linea(a.x, a.y, b.x, b.y, NexusTheme.Alfa(Color.white, 0.20f), 3);
            a = g.P(lado, 0, f + semi * 0.8f); b = g.P(lado, g.Fx0, f + semi * 0.8f);
            d.Linea(a.x, a.y, b.x, b.y, NexusTheme.Alfa(Color.black, 0.35f), 4);
            foreach (var t in new[] { 0.16f, 0.43f, 0.7f, 0.9f }) {
                var x = g.Fx0 * t; var dx = g.Fx0 * 0.016f;
                d.Poligono(Cuadro(g.P(lado, x - dx, f - semi * 1.5f), g.P(lado, x + dx, f - semi * 1.5f), g.P(lado, x + dx, f + semi * 1.5f), g.P(lado, x - dx, f + semi * 1.5f)), Color.Lerp(col, Color.black, 0.35f));
                var tor = g.P(lado, x, f - semi * 1.1f);
                d.Circulo(tor.x, tor.y, 2.2f * g.Esc(x) + 0.8f, NexusTheme.Alfa(Color.white, 0f), 0, NexusTheme.Alfa(Color.white, 0.35f));
            }
            if (valvula) {
                var x = g.Fx0 * 0.43f;
                var baseV = g.P(lado, x, f - semi * 1.4f);
                var cima = g.P(lado, x, f - semi * 1.4f - 0.07f);
                var e = g.Esc(x);
                d.Linea(baseV.x, baseV.y, cima.x, cima.y, Color.Lerp(col, Color.black, 0.3f), 6 * e);
                d.Elipse(cima.x, cima.y, 26 * e, 9 * e, p.Acento, 3, NexusTheme.Alfa(Color.black, 0.35f));
                d.Linea(cima.x - 26 * e, cima.y, cima.x + 26 * e, cima.y, p.Acento, 2.5f);
            }
        }

        private void Manguera(DibujoUI d, Geo g, int lado, float x0, float x1, float caida, Paleta p) {
            var a = g.P(lado, g.Fx0 * x0, 0.012f); var b = g.P(lado, g.Fx0 * x1, 0.012f);
            var e = g.Esc(g.Fx0 * (x0 + x1) / 2f);
            var ctl = (a + b) / 2f + new Vector2(0, 120f * e * caida);
            d.Curva(a, ctl, b, Rgb(0.03f, 0.04f, 0.05f, 0.9f), 7 * e + 1.5f, false);
            d.Curva(a, ctl + new Vector2(0, -3f), b, NexusTheme.Alfa(p.Acento, 0.18f), 1.8f, false);
            d.Circulo(a.x, a.y, 5 * e + 1.5f, Color.black, 0, Rgb(0.02f, 0.03f, 0.04f));
            d.Circulo(b.x, b.y, 5 * e + 1.5f, Color.black, 0, Rgb(0.02f, 0.03f, 0.04f));
        }

        private void Lampara(Sala s, DibujoUI d, Geo g, float x, float y, float sz) {
            d.Linea(x, y - 40 * sz, x, y - 6 * sz, NexusTheme.Alfa(Tema.ink, 0.35f), 2);
            d.Rect(x - 36 * sz, y - 6 * sz, 72 * sz, 12 * sz, NexusTheme.Alfa(Tema.ink, 0.45f), 2, Rgb(0.10f, 0.13f, 0.17f), false, 3);
            d.Linea(x - 30 * sz, y + 7 * sz, x + 30 * sz, y + 7 * sz, NexusTheme.Alfa(Color.white, 0.22f), 2);
            var rt = NodoLibre(s.Fondo, "Bombilla", 190 * sz, 190 * sz);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, -(y + 10 * sz));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = PlanoIlustrado.Brillo();
            img.raycastTarget = false;
            img.color = new Color(1f, 0.9f, 0.62f, 0.4f);
            s.Ambiente.Bombillas.Add(img);
            s.Ambiente.BrilloDeBombillas.Add(0.42f * sz + 0.1f);
        }

        // ---------------------------------------------------------------- el fondo segun el tema

        private void DecorarElFondoDelTema(Sala s, DibujoUI d, Geo g, Paleta p, int tema) {
            var w = g.W;
            float dw = (g.Fx1 - g.Fx0) * 0.2f;
            float izq0 = g.Fx0 + 14, izq1 = w / 2 - dw / 2 - 16, der0 = w / 2 + dw / 2 + 16, der1 = g.Fx1 - 14;
            float y0 = g.Fy0 + 16, y1 = g.Fy1 - 10;
            if (tema == 0) {            // sala de servidores: dos racks con LEDs
                Rack(s, d, izq0, y0 + 14, izq1 - izq0, y1 - y0 - 14, 1);
                Rack(s, d, der0, y0 + 14, der1 - der0, y1 - y0 - 14, 4);
            } else if (tema == 1) {     // tuberias: columnas con bridas y manometros
                Columna(d, p, izq0 + (izq1 - izq0) * 0.5f, y0, y1, (izq1 - izq0) * 0.28f, true);
                Columna(d, p, der0 + (der1 - der0) * 0.5f, y0, y1, (der1 - der0) * 0.28f, false);
            } else {                    // archivo: estanterias con cajas
                Estanteria(d, p, izq0, y0 + 10, izq1 - izq0, y1 - y0 - 10, 3);
                Estanteria(d, p, der0, y0 + 10, der1 - der0, y1 - y0 - 10, 8);
            }
        }

        private void Rack(Sala s, DibujoUI d, float x, float y, float wd, float ht, int semilla) {
            d.Rect(x, y, wd, ht, NexusTheme.Alfa(Tema.ink, 0.40f), 2, Rgb(0.05f, 0.08f, 0.12f, 0.96f), false, 4);
            var filas = 9; var hf = ht / filas;
            for (var r = 0; r < filas; r++) {
                var yy = y + r * hf;
                d.Rect(x + 6, yy + 4, wd - 12, hf - 8, NexusTheme.Alfa(Tema.ink, 0.16f), 1.5f, NexusTheme.Alfa(Color.black, 0.28f), false, 2);
                var verde = (r + semilla) % 4 != 0;
                s.Ambiente.Leds.Add(new AmbienteDeLosCuartos.Led { Pos = new Vector2(x + 18, yy + hf / 2), Color = verde ? Tema.success : Tema.warning, Radio = 3.2f, Fase = r * 1.37f + semilla });
                s.Ambiente.Leds.Add(new AmbienteDeLosCuartos.Led { Pos = new Vector2(x + 31, yy + hf / 2), Color = Tema.cyan, Radio = 2.6f, Fase = r * 2.11f + semilla + 3 });
                for (var k = 0; k < 5; k++) d.Linea(x + wd - 54 + k * 8, yy + hf * 0.28f, x + wd - 54 + k * 8, yy + hf * 0.72f, NexusTheme.Alfa(Tema.ink, 0.22f), 1.6f);
            }
        }

        private void Columna(DibujoUI d, Paleta p, float cx, float y0, float y1, float semi, bool manometro) {
            var col = NexusTheme.Alfa(p.Tubo, 0.95f);
            d.Poligono(Cuadro(new Vector2(cx - semi, y0), new Vector2(cx + semi, y0), new Vector2(cx + semi, y1), new Vector2(cx - semi, y1)), col);
            d.Linea(cx - semi * 0.55f, y0, cx - semi * 0.55f, y1, NexusTheme.Alfa(Color.white, 0.2f), 4);
            d.Linea(cx + semi * 0.7f, y0, cx + semi * 0.7f, y1, NexusTheme.Alfa(Color.black, 0.35f), 5);
            foreach (var t in new[] { 0.06f, 0.37f, 0.7f, 0.94f }) {
                var yy = Mathf.Lerp(y0, y1, t);
                d.Rect(cx - semi * 1.25f, yy - 7, semi * 2.5f, 14, NexusTheme.Alfa(Tema.ink, 0.35f), 2, Color.Lerp(col, Color.black, 0.4f), false, 3);
                d.Circulo(cx - semi * 1.0f, yy, 2.4f, Color.white, 0, NexusTheme.Alfa(Color.white, 0.4f));
                d.Circulo(cx + semi * 1.0f, yy, 2.4f, Color.white, 0, NexusTheme.Alfa(Color.white, 0.4f));
            }
            var m = Mathf.Lerp(y0, y1, 0.5f);
            var r = Mathf.Min(semi * 1.5f, 42f);
            d.Circulo(cx, m, r, p.Acento, 3, Rgb(0.04f, 0.06f, 0.08f));
            for (var k = 0; k < 9; k++) {
                var a = Mathf.Lerp(-2.4f, -0.74f, k / 8f) ;
                d.Linea(cx + Mathf.Cos(a) * r * 0.78f, m + Mathf.Sin(a) * r * 0.78f, cx + Mathf.Cos(a) * r * 0.95f, m + Mathf.Sin(a) * r * 0.95f, NexusTheme.Alfa(Color.white, 0.6f), 1.5f);
            }
            var aguja = manometro ? -1.25f : -1.85f;
            d.Linea(cx, m, cx + Mathf.Cos(aguja) * r * 0.78f, m + Mathf.Sin(aguja) * r * 0.78f, Tema.danger, 2.5f);
            d.Circulo(cx, m, 3.4f, Color.white, 0, Tema.ink);
        }

        private void Estanteria(DibujoUI d, Paleta p, float x, float y, float wd, float ht, int semilla) {
            d.Rect(x, y, wd, ht, NexusTheme.Alfa(Tema.ink, 0.38f), 2, Rgb(0.07f, 0.06f, 0.11f, 0.95f), false, 3);
            var baldas = 4; var hb = ht / baldas;
            var azar = new System.Random(semilla * 53 + 1);
            for (var b = 0; b < baldas; b++) {
                var yb = y + (b + 1) * hb;
                d.Linea(x, yb, x + wd, yb, NexusTheme.Alfa(Tema.ink, 0.45f), 4);
                var cx = x + 8;
                while (cx < x + wd - 28) {
                    var bw = 26f + 34f * (float)azar.NextDouble();
                    var bh = hb * (0.45f + 0.4f * (float)azar.NextDouble());
                    if (cx + bw > x + wd - 8) break;
                    var tono = Color.Lerp(p.Panel, p.Acento, 0.25f * (float)azar.NextDouble());
                    d.Rect(cx, yb - 4 - bh, bw, bh, NexusTheme.Alfa(Tema.ink, 0.30f), 1.6f, tono, false, 2);
                    d.Rect(cx + bw * 0.18f, yb - 4 - bh * 0.62f, bw * 0.64f, bh * 0.2f, NexusTheme.Alfa(Tema.ink, 0.25f), 1, NexusTheme.Alfa(Color.white, 0.5f), false, 1);
                    cx += bw + 4 + 6 * (float)azar.NextDouble();
                }
            }
        }

        // ---------------------------------------------------------------- la puerta del fondo

        /// <summary>El marco, la rueda de valvula, el rotulo de SALIDA y el numero de cuarto de la puerta.</summary>
        private void DecorarLaPuerta(Sala s, DibujoUI d, Geo g, Paleta p, float dw, float dh) {
            float w = g.W;
            var x = w / 2 - dw / 2; var y = g.Fy1 - dh;
            d.Rect(x - 10, y - 10, dw + 20, dh + 10, NexusTheme.Alfa(Tema.ink, 0.4f), 3, Rgb(0.04f, 0.06f, 0.09f), false, 5);
            d.Linea(x - 10, g.Fy1, x + dw + 10, g.Fy1, NexusTheme.Alfa(Tema.warning, 0.55f), 4);
            // El rotulo luminoso de arriba.
            var rotulo = Etiqueta(s.Fondo, "SALIDA", 15, Tema.success, dw + 20, 22, 0, true);
            Lamina.Colocar(rotulo.rectTransform, x - 10, y - 38, dw + 20, 22);
            rotulo.characterSpacing = 6; rotulo.fontStyle = FontStyles.Bold;
            rotulo.color = NexusTheme.Alfa(Tema.success, 0.85f);
            s.Ambiente.Leds.Add(new AmbienteDeLosCuartos.Led { Pos = new Vector2(x - 2, y - 27), Color = Tema.success, Radio = 3.6f, Fijo = true });
            s.Ambiente.Leds.Add(new AmbienteDeLosCuartos.Led { Pos = new Vector2(x + dw + 2, y - 27), Color = Tema.success, Radio = 3.6f, Fijo = true });

            // La rueda de valvula y el numero, hijos de la puerta (se abren con ella).
            RectTransform lienzoRt;
            var pd = CapaFija(s.Puerta, "Decorado de la puerta", dw, dh, out lienzoRt);
            var r = Mathf.Min(dw, dh) * 0.24f; var cx = dw / 2; var cy = dh * 0.5f;
            pd.Circulo(cx, cy, r, NexusTheme.Alfa(Tema.ink, 0.55f), 4, Rgb(0.05f, 0.08f, 0.12f));
            pd.Circulo(cx, cy, r * 0.22f, NexusTheme.Alfa(Tema.ink, 0.6f), 3, Rgb(0.10f, 0.13f, 0.17f));
            for (var k = 0; k < 5; k++) {
                var a = k * Mathf.PI * 2 / 5 - Mathf.PI / 2;
                pd.Linea(cx + Mathf.Cos(a) * r * 0.22f, cy + Mathf.Sin(a) * r * 0.22f, cx + Mathf.Cos(a) * r * 0.98f, cy + Mathf.Sin(a) * r * 0.98f, NexusTheme.Alfa(Tema.ink, 0.5f), 4);
            }
            pd.Linea(dw * 0.08f, dh * 0.08f, dw * 0.92f, dh * 0.08f, NexusTheme.Alfa(Tema.warning, 0.5f), 3);
            pd.Linea(dw * 0.08f, dh * 0.92f, dw * 0.92f, dh * 0.92f, NexusTheme.Alfa(Tema.warning, 0.5f), 3);
            var num = Etiqueta(s.Puerta, "CUARTO " + (_iCuarto + 1), 13, NexusTheme.Alfa(Tema.ink, 0.6f), dw, 20, 0, true);
            Lamina.Colocar(num.rectTransform, 0, dh * 0.13f, dw, 20);
            num.characterSpacing = 3;
        }

        // ---------------------------------------------------------------- una toma, vista de cerca

        /// <summary>La placa metalica de una toma: tornillos, el enchufe con su hueco, un LED, la chapa donde se lee el nombre.</summary>
        private void DibujarLaToma(Sala s, DibujoUI dib, float ancho, float alto, Vector2 esquina, Paleta p) {
            dib.Rect(4, 4, ancho - 8, alto - 8, NexusTheme.Alfa(Tema.ink, 0.5f), 2.5f, Rgb(0.20f, 0.26f, 0.33f, 0.95f), false, 12);
            foreach (var q in new[] { new Vector2(16, 16), new Vector2(ancho - 16, 16), new Vector2(16, alto - 16), new Vector2(ancho - 16, alto - 16) }) {
                dib.Circulo(q.x, q.y, 4.5f, NexusTheme.Alfa(Color.black, 0.6f), 1.5f, NexusTheme.Alfa(Tema.ink, 0.55f));
                dib.Linea(q.x - 2.8f, q.y - 1.5f, q.x + 2.8f, q.y + 1.5f, NexusTheme.Alfa(Color.black, 0.7f), 1.4f);
            }
            var mx = ancho / 2; var my = alto * 0.26f; var rr = Mathf.Min(ancho, alto) * 0.12f;
            dib.Circulo(mx, my, rr * 1.45f, NexusTheme.Alfa(Tema.ink, 0.3f), 2, Rgb(0.12f, 0.16f, 0.21f));
            dib.Circulo(mx, my, rr, Tema.ink, 3.5f, Rgb(0.02f, 0.03f, 0.05f, 0.95f));
            dib.Circulo(mx - 11, my, 4, Tema.ink, 0, Tema.ink); dib.Circulo(mx + 11, my, 4, Tema.ink, 0, Tema.ink);
            dib.Linea(mx - 5, my + rr * 0.6f, mx + 5, my + rr * 0.6f, NexusTheme.Alfa(Tema.ink, 0.7f), 2.5f);
            // La chapa donde se lee el nombre (las letras son de verdad solo con luz).
            dib.Rect(ancho * 0.06f, alto * 0.40f, ancho * 0.88f, alto * 0.52f, NexusTheme.Alfa(Tema.ink, 0.28f), 1.5f, NexusTheme.Alfa(Color.black, 0.32f), false, 6);
            s.Ambiente.Leds.Add(new AmbienteDeLosCuartos.Led { Pos = esquina + new Vector2(ancho - 30, 22), Color = Tema.success, Radio = 3.4f, Fase = esquina.x * 0.013f + 1f, EnProps = true });
        }

        /// <summary>Las notas del tablero de la palanca: una franja de aviso, tornillos, un manometro y el puerto de salida del cable.</summary>
        private void DibujarElTablero(DibujoUI dib, float bw, float bh) {
            for (var k = 0; k < 16; k += 2) {
                var x0 = 14 + (bw - 28) * k / 16f; var x1 = 14 + (bw - 28) * (k + 1) / 16f;
                dib.Poligono(Cuadro(new Vector2(x0, 5), new Vector2(x1, 5), new Vector2(x1 - 4, 12), new Vector2(x0 - 4, 12)), NexusTheme.Alfa(Tema.warning, 0.55f));
            }
            foreach (var q in new[] { new Vector2(12, 22), new Vector2(bw - 12, 22), new Vector2(12, bh - 12), new Vector2(bw - 12, bh - 12) }) {
                dib.Circulo(q.x, q.y, 4, NexusTheme.Alfa(Color.black, 0.6f), 1.4f, NexusTheme.Alfa(Tema.ink, 0.5f));
                dib.Linea(q.x - 2.4f, q.y - 1.5f, q.x + 2.4f, q.y + 1.5f, NexusTheme.Alfa(Color.black, 0.7f), 1.3f);
            }
            // El manometro (bajo la lampara, a la izquierda de la palanca).
            var gx = bw - 112; var gy = 74f; var r = 17f;
            dib.Circulo(gx, gy, r, NexusTheme.Alfa(Tema.ink, 0.6f), 2.5f, Rgb(0.03f, 0.05f, 0.07f));
            for (var k = 0; k < 7; k++) {
                var a = Mathf.Lerp(-2.5f, -0.64f, k / 6f);
                dib.Linea(gx + Mathf.Cos(a) * r * 0.72f, gy + Mathf.Sin(a) * r * 0.72f, gx + Mathf.Cos(a) * r * 0.92f, gy + Mathf.Sin(a) * r * 0.92f, NexusTheme.Alfa(Color.white, 0.55f), 1.3f);
            }
            dib.Linea(gx, gy, gx + Mathf.Cos(-1.6f) * r * 0.75f, gy + Mathf.Sin(-1.6f) * r * 0.75f, Tema.danger, 2f);
            dib.Circulo(gx, gy, 2.4f, Color.white, 0, Tema.ink);
            // El puerto de salida del cable: en el borde de arriba, al centro.
            dib.Circulo(bw / 2, 0, 12, NexusTheme.Alfa(Tema.ink, 0.6f), 3, Rgb(0.10f, 0.13f, 0.17f));
            dib.Circulo(bw / 2, 0, 5, Color.black, 0, Rgb(0.01f, 0.02f, 0.03f));
        }
    }
}
