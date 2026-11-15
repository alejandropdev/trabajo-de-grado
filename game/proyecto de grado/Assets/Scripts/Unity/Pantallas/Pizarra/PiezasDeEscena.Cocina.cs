using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using UnityEngine;
using L = Nexus.Unity.Tema.LuzUI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// (2026-11-15, direccion de arte) La cocina del bunker, la metafora de los cursos de Scrum, Kanban y Cascada: los
    /// cocineros se cuentan por su gorro (con la cinta del color de su papel), los pedidos por sus comandas, el trabajo por
    /// ollas y platos. Cascada añade sus terrazas de hormigon con el agua de neon que cae de una a otra.
    /// </summary>
    public static partial class DibujosDeEscena {
        /// <summary>Gorro de cocinero sobre (cx, yBase); 'cinta' es el color de su papel. 's' escala.</summary>
        private static void Gorro(Tizador v, float cx, float yBase, Color cinta, float s = 1, string rotulo = null) {
            float w = 36 * s, b = 12 * s;
            v.Forma(L.Elipse(cx, yBase + 2, w * 0.6f, 4 * s, 16), A(Silueta, 0.4f));
            foreach (var (dx, dy, r) in new[] { (-10f, -24f, 12f), (10f, -24f, 12f), (0f, -32f, 14f) })
                v.Forma(L.Elipse(cx + dx * s, yBase - b + dy * s + 12 * s, r * s, r * s, 20), H(0xF4F1E8), H(0xC9C4B4));
            v.Forma(L.Redondeado(cx - w / 2, yBase - b, w, b, 3 * s), H(0xF4F1E8), H(0xD8D3C3));
            v.Forma(Caja(cx - w / 2, yBase - b * 0.62f, w, b * 0.34f), cinta);
            if (rotulo != null) v.Rotulo(cx, yBase + 16, rotulo, 13, cinta, 0, true);
        }

        /// <summary>Olla de acero sobre su fogon; 'vapor' de 0 a 1; 'c' tiñe el caldo (o el borde si se quema).</summary>
        private static void Olla(Tizador v, float x, float yBase, float w, float h, float vapor, Color c, bool fuego = true) {
            if (fuego) { v.Halo(x + w / 2, yBase + 2, w * 0.7f, 10, A(Ambar, 0.6f)); v.Forma(L.Redondeado(x - 4, yBase, w + 8, 5, 2), H(0x2B3335)); }
            v.Forma(L.Redondeado(x - 7, yBase - h * 0.7f, 8, 5, 2), CromoOscuro); v.Forma(L.Redondeado(x + w - 1, yBase - h * 0.7f, 8, 5, 2), CromoOscuro);
            v.Forma(L.Redondeado(x, yBase - h, w, h, 5), Cromo, CromoOscuro, new Vector2(x, 0), new Vector2(x + w, 0));
            v.Forma(L.Elipse(x + w / 2, yBase - h, w / 2, 4.5f, 20), Color.Lerp(c, Silueta, 0.25f), Color.Lerp(c, Silueta, 0.5f));
            v.Forma(L.Redondeado(x + w * 0.16f, yBase - h + 6, 3, h - 12, 1.5f), A(Color.white, 0.4f));
            if (vapor > 0) Vapor(v, x + w / 2, yBase - h - 4, vapor, (int)x);
        }

        /// <summary>Plato visto en escorzo con su comida (si 'comida' no es transparente).</summary>
        private static void Plato(Tizador v, float cx, float cy, float r, Color comida) {
            v.Forma(L.Elipse(cx, cy + 2, r, r * 0.32f, 28), A(Silueta, 0.45f));
            v.Forma(L.Elipse(cx, cy, r, r * 0.32f, 28), H(0xF4F1E8), H(0xBDB7A6));
            v.Forma(L.Elipse(cx, cy - r * 0.02f, r * 0.68f, r * 0.2f, 24), H(0xD8D3C3));
            if (comida.a > 0) {
                v.Forma(L.Elipse(cx, cy - r * 0.1f, r * 0.46f, r * 0.2f, 20), Color.Lerp(comida, Color.white, 0.2f), Color.Lerp(comida, Silueta, 0.3f));
                v.Forma(L.Elipse(cx - r * 0.12f, cy - r * 0.18f, r * 0.12f, r * 0.05f, 10), A(Color.white, 0.5f));
            }
        }

        /// <summary>Plato servido: con su campana de cromo y el brillo de lo terminado.</summary>
        private static void PlatoTapado(Tizador v, float cx, float yBase, float r, Color brillo) {
            v.Halo(cx, yBase - r * 0.4f, r * 1.4f, r * 0.9f, A(brillo, 0.25f));
            Plato(v, cx, yBase, r * 1.15f, new Color(0, 0, 0, 0));
            v.Forma(L.Elipse(cx, yBase - 2, r, r * 0.8f, 28, 180, 360), Cromo, CromoOscuro);
            v.Forma(L.Arco(cx, yBase - 2, r * 0.82f, r * 0.64f, 2.4f, 205, 255, 12), A(Color.white, 0.6f));
            v.Forma(L.Elipse(cx, yBase - r * 0.84f, r * 0.13f, r * 0.1f, 12), Cromo, CromoOscuro);
        }

        /// <summary>Riel de comandas: barra de cromo y los papelitos colgando. 'colores' marca cada comanda.</summary>
        private static void Comandas(Tizador v, float x, float y, float w, params Color[] colores) {
            v.Forma(L.Redondeado(x - 6, y - 4, w + 12, 8, 4), Cromo, CromoOscuro);
            var paso = w / Mathf.Max(1, colores.Length);
            for (var i = 0; i < colores.Length; i++) {
                var px = x + i * paso + paso * 0.12f; var pw = paso * 0.76f;
                v.Forma(Tizador.Rotar(L.Redondeado(px, y + 2, pw, pw * 1.3f, 2), px + pw / 2, y + 2, (i % 2 == 0 ? -4 : 3)), H(0xF2EAD2), H(0xD9CFB0));
                v.Forma(Caja(px + 3, y + 6, pw - 6, 4), colores[i]);
                for (var k = 0; k < 3; k++) v.Forma(Caja(px + 4, y + 16 + k * pw * 0.26f, pw * (k == 2 ? 0.4f : 0.66f), 2), A(H(0x1E2A2A), 0.5f));
            }
        }

        private static void Bandera(Tizador v, float x, float yBase, float alto, Color c) {
            v.Cable(Pts(x, yBase, x, yBase - alto), 3, 2.4f, Cromo);
            var top = yBase - alto;
            v.Halo(x + 26, top + 16, 44, A(c, 0.3f));
            v.Forma(new RutaDeCurva(x, top).C(x + 18, top - 8, x + 34, top + 8, x + 54, top + 2).L(x + 50, top + 30)
                .C(x + 32, top + 36, x + 18, top + 22, x, top + 30).Puntos(), Color.Lerp(c, Color.white, 0.2f), Color.Lerp(c, Silueta, 0.3f));
        }

        private static void Espejo(Tizador v, float cx, float cy, float w, float h) {
            v.Forma(L.Elipse(cx, cy, w / 2 + 6, h / 2 + 6, 36), Cromo, CromoOscuro);
            v.Forma(L.Elipse(cx, cy, w / 2, h / 2, 36), H(0x2B6A7A), H(0x0B2A33));
            v.Halo(cx, cy, w * 0.5f, h * 0.4f, A(NucleoClaro, 0.18f));
            v.Forma(Tizador.Rotar(L.Redondeado(cx - w * 0.25f, cy - h * 0.3f, 6, h * 0.5f, 3), cx, cy, 25), A(Color.white, 0.35f));
        }

        private static void Corazon(Tizador v, float cx, float cy, float s, Color c) {
            v.Halo(cx, cy, s * 1.8f, A(c, 0.35f));
            v.Forma(new RutaDeCurva(cx, cy + s).C(cx - s * 1.4f, cy, cx - s * 1.1f, cy - s * 1.2f, cx, cy - s * 0.45f)
                .C(cx + s * 1.1f, cy - s * 1.2f, cx + s * 1.4f, cy, cx, cy + s).Puntos(), Color.Lerp(c, Color.white, 0.25f), Color.Lerp(c, Silueta, 0.3f));
        }

        /// <summary>Sello de tinta: doble marco y la palabra.</summary>
        private static void SelloTexto(Tizador v, float cx, float cy, string texto, Color c, float w = 96) {
            v.Halo(cx, cy, w * 0.7f, 30, A(c, 0.25f));
            Contorno(v, Caja(cx - w / 2, cy - 20, w, 40), 3, c);
            Contorno(v, Caja(cx - w / 2 + 5, cy - 15, w - 10, 30), 1.2f, A(c, 0.8f));
            v.Rotulo(cx, cy, texto, 16, c, 0, false, true);
        }

        /// <summary>Terrazas de la cascada: n escalones de hormigon y el agua de neon cayendo de uno al siguiente.</summary>
        private static void Terrazas(Tizador v, float x, float y, int n, float w, float h, Color agua) {
            for (var i = 0; i < n; i++) {
                float tx = x + i * w, ty = y + i * h;
                v.Forma(Caja(tx, ty, w + 1, 300 - ty), Color.Lerp(H(0x2B4F57), H(0x163038), i / (float)n), H(0x07222A));
                v.Forma(Caja(tx, ty, w + 1, 3), A(CostillaLejos, 0.7f));
                if (i < n - 1) {
                    v.Neon(new RutaDeCurva(tx + w - 2, ty + 1).C(tx + w + 6, ty + 2, tx + w + 4, ty + h * 0.6f, tx + w + 5, ty + h).Puntos(), 1.4f, agua);
                    v.Halo(tx + w + 6, ty + h, 14, 6, A(agua, 0.5f));
                }
            }
        }

        /// <summary>Cinta transportadora con sus rodillos.</summary>
        private static void Cinta(Tizador v, float x, float y, float w) {
            v.Forma(L.Redondeado(x, y, w, 22, 11), H(0x2B3335), H(0x101416));
            v.Forma(Caja(x + 10, y + 1, w - 20, 2), A(Cromo, 0.5f));
            for (var cx = x + 14; cx < x + w; cx += 34) v.Forma(L.Elipse(cx, y + 11, 6, 6, 12), Cromo, CromoOscuro);
        }

        /// <summary>Vaso medidor con su tope marcado en coral: hasta ahi, y no mas.</summary>
        private static void Vaso(Tizador v, float cx, float cy, float s, Color c) {
            var cuerpo = Pts(cx - s * 0.6f, cy - s, cx + s * 0.6f, cy - s, cx + s * 0.45f, cy + s, cx - s * 0.45f, cy + s);
            v.FormaDeLuz(cuerpo, A(NucleoClaro, 0.12f));
            v.Forma(Pts(cx - s * 0.52f, cy - s * 0.2f, cx + s * 0.52f, cy - s * 0.2f, cx + s * 0.45f, cy + s, cx - s * 0.45f, cy + s), A(c, 0.75f));
            Contorno(v, cuerpo, 1.6f, A(Cromo, 0.8f));
            for (var x = cx - s * 0.8f; x < cx + s * 0.8f; x += 8) v.Forma(Caja(x, cy - s * 0.32f, 4, 2.4f), Coral);
        }

        /// <summary>Un ojo: almendra clara con el iris que brilla.</summary>
        private static void Ojo(Tizador v, float cx, float cy, float s, Color c) {
            v.Forma(new RutaDeCurva(cx - s, cy).Q(cx, cy - s * 1.1f, cx + s, cy).Q(cx, cy + s * 1.1f, cx - s, cy).Puntos(), H(0xE9E4D4), H(0xBDB7A6));
            v.Halo(cx, cy, s * 0.6f, A(c, 0.6f));
            v.Forma(L.Elipse(cx, cy, s * 0.36f, s * 0.36f, 20), Color.Lerp(c, Color.white, 0.2f), Color.Lerp(c, Silueta, 0.4f));
            v.Forma(L.Elipse(cx, cy, s * 0.15f, s * 0.15f, 12), H(0x05090A));
        }

        /// <summary>Embudo de cromo de (x, y) con boca w y alto h.</summary>
        private static void Embudo(Tizador v, float x, float y, float w, float h, Color c) {
            v.Forma(Pts(x, y, x + w, y, x + w * 0.58f, y + h * 0.7f, x + w * 0.58f, y + h, x + w * 0.42f, y + h, x + w * 0.42f, y + h * 0.7f), Cromo, CromoOscuro);
            v.Forma(L.Elipse(x + w / 2, y, w / 2, 7, 24), Color.Lerp(c, Silueta, 0.4f));
        }

        /// <summary>Tablero de la interfaz con sus columnas; devuelve la x de cada columna (y su ancho, en 'ancho').</summary>
        private static float[] TableroUI(Tizador v, float x, float y, float w, float h, string[] cols, out float ancho, Color[] cs = null) {
            PanelUI(v, x, y, w, h);
            ancho = w / Mathf.Max(1, cols.Length);
            var xs = new float[cols.Length];
            for (var i = 0; i < cols.Length; i++) {
                xs[i] = x + i * ancho;
                if (i > 0) v.Forma(Caja(xs[i], y + 10, 1.2f, h - 20), A(Borde, 0.9f));
                if (!string.IsNullOrEmpty(cols[i])) v.Rotulo(xs[i] + ancho / 2, y + 20, cols[i], 12, cs != null ? cs[i] : Nucleo, 0, true);
            }
            return xs;
        }
    }
}
