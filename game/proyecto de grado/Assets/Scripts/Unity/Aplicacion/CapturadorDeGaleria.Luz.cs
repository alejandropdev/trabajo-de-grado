#if UNITY_EDITOR
using System.Collections.Generic;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// SOLO EN EL EDITOR (2026-11-15). Atajos de las laminas de la galeria sobre las piezas de luz del juego (LuzUI,
    /// FormaDegradada, RutaDeCurva, en Unity/Tema): misma luz en la exploracion y en el juego.
    /// </summary>
    public sealed partial class CapturadorDeGaleria {
        private static KeyValuePair<float, Color> S(float p, Color c) { return LuzUI.Parada(p, c); }

        private RawImage Img(RectTransform padre, Texture tex, float x, float y, float w, float h, Color c, bool suma) {
            return LuzUI.Imagen(_ui, padre, tex, x, y, w, h, c, suma);
        }

        private void Cielo(RectTransform padre, float x, float y, float w, float h, params KeyValuePair<float, Color>[] paradas) {
            LuzUI.Cielo(_ui, padre, x, y, w, h, paradas);
        }

        private void Niebla(RectTransform padre, float x, float y, float w, float h, Color c, float alfaArriba, float alfaAbajo) {
            LuzUI.Niebla(_ui, padre, x, y, w, h, c, alfaArriba, alfaAbajo);
        }

        private void Brillo(RectTransform padre, float cx, float cy, float rx, float ry, Color c, float fuerza = 1f) {
            LuzUI.Brillo(_ui, padre, cx, cy, rx, ry, A(c, c.a * fuerza));
        }

        private void Haz(RectTransform padre, float x, float y, float largo, float ancho, float grados, Color c) {
            LuzUI.Haz(_ui, padre, x, y, largo, ancho, grados, c);
        }

        /// <summary>Motas de luz: n puntitos con su halo, tamaño y brillo al azar (semilla fija) dentro de un rect.</summary>
        private void Particulas(RectTransform padre, Rect zona, int n, int semilla, Color c, float rMin, float rMax) {
            var azar = new System.Random(semilla);
            for (var i = 0; i < n; i++) {
                var x = zona.x + (float)azar.NextDouble() * zona.width;
                var y = zona.y + (float)azar.NextDouble() * zona.height;
                var r = Mathf.Lerp(rMin, rMax, Mathf.Pow((float)azar.NextDouble(), 2.2f));
                var a = 0.35f + 0.65f * (float)azar.NextDouble();
                Brillo(padre, x, y, r * 4, r * 4, A(c, a * 0.5f));
                Brillo(padre, x, y, r, r, A(Color.Lerp(c, Color.white, 0.6f), a));
            }
        }

        private FormaDegradada Forma(RectTransform padre, List<Vector2> pts, Color a, Color b, Vector2 desde, Vector2 hasta) {
            return LuzUI.Forma(_ui, padre, pts, a, b, desde, hasta);
        }

        private FormaDegradada Forma(RectTransform padre, List<Vector2> pts, Color c) { return LuzUI.Forma(_ui, padre, pts, c); }

        /// <summary>Una capa de trazos (DibujoUI) a pantalla completa encima de lo ya pintado.</summary>
        private DibujoUI Trazos(RectTransform capa) { return Lamina(capa, 0, 0, 1920, 1080).Dibujo; }

        private void Rotulo2(RectTransform capa, float x, float y, float w, float h, string texto, float tam, Color c, TMP_FontAsset f,
                             TextAlignmentOptions al = TextAlignmentOptions.TopLeft, float espaciado = 0) {
            var t = _ui.Texto(capa, texto, EstiloTexto.Cuerpo, c, al);
            t.font = f; t.fontSize = tam; t.characterSpacing = espaciado;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            Colocar(t.rectTransform, x, y, w, h);
        }

        private static List<Vector2> Elipse(float cx, float cy, float rx, float ry, int n = 48, float a0 = 0, float a1 = 360) { return LuzUI.Elipse(cx, cy, rx, ry, n, a0, a1); }
        private static List<Vector2> Redondeado(float x, float y, float w, float h, float r, int pasos = 8) { return LuzUI.Redondeado(x, y, w, h, r, pasos); }
        private static List<Vector2> Arco(float cx, float cy, float rx, float ry, float grueso, float a0, float a1, int n = 40, float bamboleo = 0, int semilla = 0) {
            return LuzUI.Arco(cx, cy, rx, ry, grueso, a0, a1, n, bamboleo, semilla);
        }
        private static List<Vector2> Tubo(List<Vector2> eje, float g0, float g1) { return LuzUI.Tubo(eje, g0, g1); }
        private static List<Vector2> Mover(List<Vector2> p, float dx, float dy, float k = 1f, float cx = 0, float cy = 0) { return LuzUI.Mover(p, dx, dy, k, cx, cy); }
    }
}
#endif
