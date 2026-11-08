using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Metodologia;
using Nexus.Core.Modelo;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Los instrumentos de la cabina de lanzamiento del Resumen (2026-11-08): cada decision se ve como un dibujo y no como
    /// una frase. El radar dice como repartiste las fichas de calidad; la mini tira, cuando se entrega.
    /// </summary>
    internal static class RadarDeCalidad {
        private const float W = 440, H = 300, CX = 220, CY = 150, R = 100;

        /// <summary>
        /// Un radar con un eje por atributo; el mas cargado llega al borde. Cada vertice lleva el nombre y sus fichas
        /// (en aviso si no tiene ninguna). Con menos de tres atributos no hay radar: se pintan sus fichas sueltas.
        /// </summary>
        public static void Crear(UiKit ui, RectTransform hueco, IList<AtributoCalidad> atributos, Func<string, int> fichas) {
            var t = ui.Tema;
            var lamina = PiezasDeProyecto.NuevaLamina(ui, hueco, W, H);
            var n = atributos.Count;
            if (n < 3) {
                TextoEnCaja.Crear(ui, hueco, string.Join(" · ", atributos.Select(a => a.Nombre + " " + fichas(a.Id))), EstiloTexto.Cuerpo, t.ink,
                                  TextAlignmentOptions.Center, t.Px(16), Vector2.zero, Vector2.one, 4);
                return;
            }
            var d = lamina.Dibujo;
            var max = Mathf.Max(1, atributos.Max(a => fichas(a.Id)));
            Func<int, float, Vector2> punto = (i, k) => {
                var ang = (-90 + 360f * (i % n) / n) * Mathf.Deg2Rad;
                return new Vector2(CX + Mathf.Cos(ang) * R * k, CY + Mathf.Sin(ang) * R * k);
            };
            for (var anillo = 1; anillo <= 3; anillo++) {
                var pts = new List<Vector2>();
                for (var i = 0; i <= n; i++) pts.Add(punto(i, anillo / 3f));
                d.Polilinea(pts, NexusTheme.Alfa(t.ink, 0.2f), 1.5f);
            }
            for (var i = 0; i < n; i++) { var p = punto(i, 1); d.Linea(CX, CY, p.x, p.y, NexusTheme.Alfa(t.ink, 0.2f), 1.5f); }
            var valores = new List<Vector2>();
            for (var i = 0; i < n; i++) valores.Add(punto(i, Mathf.Max(0.07f, fichas(atributos[i].Id) / (float)max)));
            d.Poligono(valores, NexusTheme.Alfa(t.cyan, 0.3f));
            var cierre = new List<Vector2>(valores) { valores[0] };
            d.Polilinea(cierre, t.cyan, 3);
            for (var i = 0; i < n; i++) d.Circulo(valores[i].x, valores[i].y, 6, t.cyan, 0, t.cyan);
            for (var i = 0; i < n; i++) {
                var p = punto(i, 1.30f);
                var v = fichas(atributos[i].Id);
                var etiqueta = ui.Nodo(lamina.Raiz, "Eje " + atributos[i].Nombre);
                Lamina.Colocar(etiqueta, p.x - 96, p.y - 16, 192, 32);
                TextoEnCaja.Crear(ui, etiqueta, atributos[i].Nombre + " · " + v, EstiloTexto.Pequeno, v > 0 ? t.ink : t.warning,
                                  TextAlignmentOptions.Center, 25, Vector2.zero, Vector2.one);
            }
        }
    }

    internal static class MiniTira {
        /// <summary>
        /// El calendario del nivel en una tira: un bloque por sprint/etapa/revision (a escala de sus dias) y una banderita
        /// amarilla en cada dia de despliegue.
        /// </summary>
        public static void Crear(UiKit ui, RectTransform padre, OpcionDeCalendario o, int dias) {
            var t = ui.Tema;
            if (o == null || o.Unidades.Count == 0) return;
            dias = Mathf.Max(1, dias);
            var acumulado = 0;
            for (var i = 0; i < o.Unidades.Count; i++) {
                var u = o.Unidades[i];
                var bloque = ui.Nodo(padre, "Unidad " + (i + 1));
                bloque.anchorMin = new Vector2((float)acumulado / dias, 0);
                bloque.anchorMax = new Vector2((float)(acumulado + u) / dias, 0.5f);
                bloque.offsetMin = new Vector2(1.5f, 0); bloque.offsetMax = new Vector2(-1.5f, 0);
                var img = bloque.gameObject.AddComponent<Image>();
                ui.Fondo(img, NexusTheme.Alfa(t.cyan, i % 2 == 0 ? 0.40f : 0.24f), 3);
                img.raycastTarget = false;
                acumulado += u;
            }
            foreach (var d in o.Despliegues) {
                var x = Mathf.Clamp01((float)d / dias);
                var marca = ui.Nodo(padre, "Despliegue " + d);
                marca.anchorMin = marca.anchorMax = new Vector2(x, 0.74f);
                marca.pivot = new Vector2(x > 0.9f ? 1 : 0.5f, 0.5f);
                marca.sizeDelta = new Vector2(t.Px(11), t.Px(11));
                var im = marca.gameObject.AddComponent<Image>();
                im.sprite = ui.SpriteCircular(); im.color = t.warning; im.raycastTarget = false;
            }
        }
    }
}
