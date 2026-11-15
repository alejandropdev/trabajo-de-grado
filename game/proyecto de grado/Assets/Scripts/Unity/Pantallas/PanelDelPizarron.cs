using System.Collections.Generic;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// (2026-11-15, direccion de arte) El material del pizarron de conceptos: una placa de acero esmaltada con juntas,
    /// remaches y rotulacion estarcida, con su textura hecha por codigo (manchas, cepillado, grano, rasguños). La escena se
    /// monta en ella como una pantalla encastrada con bisel cromado. (Se probaron tambien una pagina de manual de 1957 y
    /// una pizarra de piedra; se eligio la chapa. El degradado azul anterior se rechazo por generico.)
    /// </summary>
    public static class PanelDelPizarron {
        private const float W = MarcoDePizarra.W, H = MarcoDePizarra.H;

        private static Color H6(int rgb, float a = 1) { return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, a); }

        // ---------------------------------------------------------------- tintas

        public static readonly Color Marco = H6(0x14181A), Titulo = H6(0xF2EEE2), Texto = H6(0xDAD8CE), Tema = H6(0xE3C24F, 0.85f),
                                     Acento = H6(0xE3C24F), EnNexus = H6(0xE3C24F), TextoDeTarjeta = H6(0xDAD8CE), Contador = H6(0x8A918C);

        // ---------------------------------------------------------------- el panel

        /// <summary>El fondo del panel (debajo de la lamina) con su textura y su decoracion fija.</summary>
        public static void Fondo(UiKit ui, RectTransform tabla) {
            var img = tabla.gameObject.AddComponent<RawImage>();
            img.texture = Textura();
            img.raycastTarget = false;
            tabla.gameObject.AddComponent<RectMask2D>();
            DecorarChapa(ui, tabla);
        }

        private static void DecorarChapa(UiKit ui, RectTransform t) {
            // Juntas entre planchas: una ranura oscura con su filo de luz al lado; remaches a lo largo.
            foreach (var x in new[] { 548f, 1094f }) {
                LuzUI.Forma(ui, t, Caja(x - 2, 0, 3, H), H6(0x0A0C0D, 0.85f));
                LuzUI.Forma(ui, t, Caja(x + 1, 0, 1.5f, H), H6(0x5A615E, 0.35f));
                for (var y = 28f; y < H; y += 74) { Remache(ui, t, x - 14, y); Remache(ui, t, x + 14, y); }
            }
            for (var x = 30f; x < W; x += 92) { Remache(ui, t, x, 18); Remache(ui, t, x, H - 18); }
            for (var y = 92f; y < H - 40; y += 92) { Remache(ui, t, 18, y); Remache(ui, t, W - 18, y); }
            // Rotulacion estarcida, pintada a mano hace tiempo.
            var r = ui.Texto(t, "SECTOR 7  ·  FORMACIÓN  ·  NX-07", EstiloTexto.Cuerpo, H6(0xE3C24F, 0.16f), TextAlignmentOptions.BottomRight);
            r.font = ui.Tema.FuenteDisplay; r.fontSize = 22; r.characterSpacing = 14; r.raycastTarget = false;
            Lamina.Colocar(r.rectTransform, W - 1240, 34, 860, 40);
        }

        private static void Remache(UiKit ui, RectTransform t, float cx, float cy) {
            LuzUI.Forma(ui, t, LuzUI.Elipse(cx + 1, cy + 1.5f, 4.6f, 4.6f, 14), H6(0x050607, 0.7f));
            LuzUI.Forma(ui, t, LuzUI.Elipse(cx, cy, 4.2f, 4.2f, 14), H6(0x5B625F), H6(0x23282A), new Vector2(cx - 3, cy - 3), new Vector2(cx + 3, cy + 3));
            LuzUI.Forma(ui, t, LuzUI.Elipse(cx - 1.4f, cy - 1.4f, 1.3f, 1.1f, 8), H6(0xC9D0CC, 0.6f));
        }

        /// <summary>La placa de la esquina, del material del panel.</summary>
        public static void Placa(UiKit ui, RectTransform marco, string nota) {
            var rt = ui.Nodo(marco, "Nota");
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-54, -44);
            rt.sizeDelta = new Vector2(220, 42);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ui.SpriteRedondeado(); img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            img.color = H6(0xE3C24F);
            var t = ui.Texto(rt, nota, EstiloTexto.Cuerpo, H6(0x1A1608), TextAlignmentOptions.Center);
            t.font = ui.Tema.FuenteInterfazNegrita;
            t.fontSize = 19; t.characterSpacing = 8; t.raycastTarget = false;
            UiKit.Rellenar((RectTransform)t.transform);
        }

        // ---------------------------------------------------------------- la escena montada en el panel

        /// <summary>Lo que se come el bisel alrededor de la escena.</summary>
        public const float Margen = 16;

        /// <summary>Lo que va DETRAS de la escena (llamar antes de dibujarla).</summary>
        public static void Detras(UiKit ui, Transform padre, float x, float y, float w, float h) {
            // Bisel cromado alrededor de la pantalla, con su sombra en la chapa.
            LuzUI.Forma(ui, padre, LuzUI.Redondeado(x - 14, y - 10, w + 28, h + 28, 22), H6(0x000000, 0.45f));
            Grad(ui, padre, LuzUI.Redondeado(x - 16, y - 16, w + 32, h + 32, 20), H6(0xB7C0BE), H6(0x3E4645));
            Grad(ui, padre, LuzUI.Redondeado(x - 9, y - 9, w + 18, h + 18, 14), H6(0x2B3133), H6(0x8E9896));
            LuzUI.Forma(ui, padre, Caja(x - 3, y - 3, w + 6, h + 6), H6(0x050708));
        }

        /// <summary>Lo que va ENCIMA de la escena (llamar despues de dibujarla).</summary>
        public static void Encima(UiKit ui, Transform padre, float x, float y, float w, float h) {
            // Sombra interior del cristal y un reflejo en diagonal.
            LuzUI.Niebla(ui, padre, x, y, w, 26, Color.black, 0.55f, 0);
            LuzUI.Niebla(ui, padre, x, y + h - 18, w, 18, Color.black, 0, 0.45f);
            LuzUI.Forma(ui, padre, new List<Vector2> { new Vector2(x + w * 0.62f, y), new Vector2(x + w * 0.74f, y),
                new Vector2(x + w * 0.56f, y + h), new Vector2(x + w * 0.44f, y + h) }, H6(0xFFFFFF, 0.018f));
        }

        // ---------------------------------------------------------------- tarjetas de las columnas

        public static void Tarjeta(UiKit ui, Transform padre, float x, float y, float w, float h, out Graphic cara, out Graphic borde) {
            borde = Grad(ui, padre, LuzUI.Redondeado(x - 2, y - 2, w + 4, h + 4, 14), H6(0x4A514F), H6(0x101314));
            cara = Grad(ui, padre, LuzUI.Redondeado(x, y, w, h, 12), H6(0x15191A), H6(0x1E2324));
        }

        // ---------------------------------------------------------------- texturas por codigo

        private static Texture2D _textura;

        /// <summary>Acero esmaltado en grafito: manchas grandes, cepillado horizontal, grano, viñeta y algunos rasguños.</summary>
        private static Texture2D Textura() {
            if (_textura != null) return _textura;
            const int w = 1024, h = 592;
            var px = new Color32[w * h];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++) {
                    float u = x / (float)w, v = y / (float)h;
                    var borde = Mathf.Min(Mathf.Min(u, 1 - u) * 6, Mathf.Min(v, 1 - v) * 10);
                    var vin = Mathf.Lerp(0.86f, 1f, Mathf.Clamp01(borde));
                    var manchas = Fbm(x / 170f, y / 170f, 4, 1) - 0.5f;
                    var cepillado = Fbm(x / 420f, y / 2.6f, 2, 2) - 0.5f;
                    var grano = Hash(x, y, 3) - 0.5f;
                    var b = 0.155f + manchas * 0.05f + cepillado * 0.03f + grano * 0.018f;
                    var c = new Color(b * 0.96f, b * 1.04f, b) * vin;
                    c.a = 1;
                    px[(h - 1 - y) * w + x] = c;
                }
            var azar = new System.Random(7);
            for (var i = 0; i < 70; i++) {
                float x0 = (float)azar.NextDouble() * w, y0 = (float)azar.NextDouble() * h;
                var ang = (float)(azar.NextDouble() * Mathf.PI);
                var largo = 16 + (float)azar.NextDouble() * 70;
                for (var s = 0f; s < largo; s += 0.7f) {
                    int x = Mathf.RoundToInt(x0 + Mathf.Cos(ang) * s), y = Mathf.RoundToInt(y0 + Mathf.Sin(ang) * s);
                    if (x < 0 || y < 0 || x >= w || y >= h) break;
                    var idx = (h - 1 - y) * w + x;
                    var c = px[idx];
                    px[idx] = new Color32((byte)Mathf.Min(255, c.r + 9), (byte)Mathf.Min(255, c.g + 9), (byte)Mathf.Min(255, c.b + 9), 255);
                }
            }
            _textura = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, name = "Panel de chapa" };
            _textura.SetPixels32(px); _textura.Apply(true, true);
            return _textura;
        }

        private static float Hash(int x, int y, int s) {
            unchecked {
                var n = x * 374761393 + y * 668265263 + s * 1442695041;
                n = (n ^ (n >> 13)) * 1274126177;
                return ((n ^ (n >> 16)) & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        private static float Ruido(float x, float y, int s) {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            var a = Mathf.Lerp(Hash(xi, yi, s), Hash(xi + 1, yi, s), fx);
            var b = Mathf.Lerp(Hash(xi, yi + 1, s), Hash(xi + 1, yi + 1, s), fx);
            return Mathf.Lerp(a, b, fy);
        }

        private static float Fbm(float x, float y, int octavas, int s) {
            float suma = 0, amp = 0.5f, total = 0;
            for (var i = 0; i < octavas; i++) {
                suma += Ruido(x, y, s + i * 17) * amp; total += amp;
                x *= 2.03f; y *= 2.03f; amp *= 0.5f;
            }
            return suma / total;
        }

        /// <summary>Forma con degradado vertical de arriba (a) a abajo (b).</summary>
        private static FormaDegradada Grad(UiKit ui, Transform padre, List<Vector2> pts, Color a, Color b) {
            float y0 = float.MaxValue, y1 = float.MinValue;
            foreach (var q in pts) { y0 = Mathf.Min(y0, q.y); y1 = Mathf.Max(y1, q.y); }
            return LuzUI.Forma(ui, padre, pts, a, b, new Vector2(0, y0), new Vector2(0, y1 + 0.01f));
        }

        private static List<Vector2> Caja(float x, float y, float w, float h) {
            return new List<Vector2> { new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h) };
        }
    }
}
