using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nexus.Core.Coleccion;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    public static partial class Hallazgos {
        private static readonly Color Piedra = new Color32(0x5B, 0x62, 0x70, 0xFF);
        private static readonly Color PiedraClara = new Color32(0x8A, 0x92, 0xA0, 0xFF);

        /// <summary>
        /// Una startup que fracaso, contada en fragmentos: una linea de tiempo cuyos hitos se encienden uno a uno mientras
        /// la grafica de arriba sube… hasta el ultimo, donde se desploma. Entonces sale de la tierra su lapida, con los
        /// años y la causa grabada como epitafio. El monograma es inventado (nunca el logo real).
        /// </summary>
        public static void Startup(ContextoDeHallazgo x) {
            var col = x.Col;
            var tema = x.Tema;
            var e = x.Escenario;
            var hitos = col.Hitos != null && col.Hitos.Count > 0 ? col.Hitos
                : new List<HitoDeStartup> { new HitoDeStartup { Fecha = "", Texto = col.Texto } };
            var n = hitos.Count;
            var morado = tema.violet;

            // ---- la cabecera: monograma inventado, nombre y nicho
            var mono = x.Circulo(e, "Monograma", -570, 300, 74, NexusTheme.Alfa(morado, 0.25f));
            x.Ui.Borde((RectTransform)mono.transform, morado, 37, 2);
            x.Texto(mono.transform, string.IsNullOrEmpty(col.Titulo) ? "?" : col.Titulo.Substring(0, 1).ToUpperInvariant(), EstiloTexto.Titulo, tema.ink, 0, 0, 74, 74);
            x.Texto(e, col.Titulo, EstiloTexto.Titulo, tema.ink, -310, 314, 440, 44, TextAlignmentOptions.Left);
            x.Texto(e, col.Nicho ?? "", EstiloTexto.Pequeno, morado, -310, 282, 520, 26, TextAlignmentOptions.Left);
            var gCabecera = x.Grupo(mono);

            // ---- la grafica: sube con cada hito y se desploma en el ultimo
            const float ancho = 760, alto = 210;
            DibujoUI dGrafica;
            var g = x.Lamina(e, -200, 132, ancho, alto, out dGrafica);
            g.Linea(0, alto - 4, ancho, alto - 4, Tizador.Gris, 2);
            for (var k = 1; k < 4; k++) g.Linea(0, alto - 4 - k * 48, ancho, alto - 4 - k * 48, NexusTheme.Alfa(Tizador.Gris, 0.4f), 1, true);
            var xs = new float[n];
            for (var i = 0; i < n; i++) xs[i] = n == 1 ? ancho / 2 : 40 + i * (ancho - 80) / (n - 1);
            var puntos = new List<float> { 0, alto - 14 };
            for (var i = 0; i < n; i++) {
                var ultimo = i == n - 1;
                puntos.Add(xs[i]);
                puntos.Add(ultimo ? alto - 8 : alto - 40 - i * (140f / Mathf.Max(1, n - 1)));
            }
            g.Trazo(morado, puntos.ToArray());
            if (n > 1) g.Flecha(xs[n - 2] + 6, alto - 40 - (n - 2) * (140f / Mathf.Max(1, n - 1)), xs[n - 1], alto - 10, tema.danger, 4);

            // ---- la linea de tiempo
            const float yLinea = -40;
            x.Caja(e, "Linea", -200, yLinea, ancho, 4, NexusTheme.Alfa(morado, 0.5f), 2);
            var grupos = new CanvasGroup[n];
            var nodos = new Image[n];
            for (var i = 0; i < n; i++) {
                var cx = -200 - ancho / 2 + xs[i];
                var ultimo = i == n - 1;
                var nodo = x.Nodo(e, "Hito " + (i + 1), cx, yLinea, 190, 200);
                nodos[i] = x.Circulo(nodo, "Punto", 0, 0, 22, ultimo ? tema.danger : morado);
                x.Texto(nodo, (hitos[i].Fecha ?? "").ToUpperInvariant(), EstiloTexto.Leyenda, ultimo ? tema.danger : PlanoIlustrado.ColorDelDestello, 0, 32, 190, 24);
                x.Texto(nodo, hitos[i].Texto ?? "", EstiloTexto.Pequeno, tema.ink, 0, -62, 180, 96, TextAlignmentOptions.Top);
                grupos[i] = x.Grupo(nodo);
            }

            // ---- la lapida
            // La lapida sale de la tierra: va dentro de una fosa con mascara cuyo borde de abajo es el suelo.
            var fosa = x.Nodo(e, "Fosa", 440, 70, 460, 500);
            fosa.gameObject.AddComponent<RectMask2D>();
            var lapida = x.Nodo(fosa, "Lapida", 0, -100, 330, 420);
            x.Circulo(lapida, "Arco", 0, 70, 300, Piedra);
            x.Caja(lapida, "Cuerpo", 0, -40, 300, 260, Piedra, 10);
            x.Ui.Borde(x.Nodo(lapida, "Grabado", 0, 10, 250, 300), NexusTheme.Alfa(PiedraClara, 0.5f), 18, 2);
            x.Texto(lapida, "AQUÍ YACE", EstiloTexto.Leyenda, PiedraClara, 0, 150, 240, 24).characterSpacing = 6;
            x.Texto(lapida, col.Titulo, EstiloTexto.Subtitulo, tema.ink, 0, 112, 250, 40);
            x.Texto(lapida, Anos(hitos), EstiloTexto.Pequeno, PiedraClara, 0, 80, 250, 26);
            var epitafio = x.Texto(lapida, col.Causa ?? "", EstiloTexto.Pequeno, tema.ink, 0, -40, 240, 170);
            epitafio.fontStyle = FontStyles.Italic;
            epitafio.enableAutoSizing = true;
            epitafio.fontSizeMin = 12;
            epitafio.fontSizeMax = epitafio.fontSize;
            var gLapida = x.Grupo(lapida);
            // La tierra de delante (tapa el pie de la lapida al salir).
            x.Caja(e, "Tierra", 440, -196, 440, 40, new Color32(0x1B, 0x22, 0x1E, 0xFF), 8);
            for (var k = 0; k < 9; k++) x.Caja(e, "Hierba", 250 + k * 48, -178, 4, 14 + (k % 3) * 6, new Color32(0x2F, 0x5D, 0x3A, 0xFF));

            var fuente = x.Texto(e, col.Fuente ?? "", EstiloTexto.Pequeno, tema.inkFaint, 0, -318, 1100, 26);
            fuente.fontSize *= 0.85f;
            var gFuente = x.Grupo(fuente);

            // ---- el ritmo: cabecera, cada hito con su trozo de grafica, el desplome, la lapida
            var s = x.Sec;
            s.En(0f, 0.4f, p => gCabecera.alpha = p);
            const float porHito = 0.9f;
            s.En(0.5f, porHito * n, p => dGrafica.Avance = p);
            for (var i = 0; i < n; i++) {
                var k = i;
                s.En(0.5f + porHito * (k + 1) - 0.35f, 0.35f, p => {
                    grupos[k].alpha = p;
                    nodos[k].transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, ContextoDeHallazgo.Rebote(p));
                });
            }
            var finHitos = 0.5f + porHito * n;
            s.En(finHitos, 0.01f, p => { if (p >= 1) x.Sacudir(10); });
            s.En(finHitos + 0.3f, 0.8f, p => {
                gLapida.alpha = p > 0 ? 1 : 0;
                lapida.anchoredPosition = new Vector2(0, Mathf.Lerp(-540, -100, ContextoDeHallazgo.Suave(p)));
            });
            s.En(finHitos + 1.2f, 0.4f, p => gFuente.alpha = p);
        }

        /// <summary>«1999 - 2001» a partir de los años que aparezcan en las fechas de los hitos.</summary>
        private static string Anos(List<HitoDeStartup> hitos) {
            var anos = hitos.SelectMany(h => Regex.Matches(h.Fecha ?? "", @"\d{4}").Cast<Match>().Select(m => int.Parse(m.Value))).ToList();
            if (anos.Count == 0) return "";
            var a = anos.Min(); var b = anos.Max();
            return a == b ? a.ToString() : $"{a} - {b}";
        }
    }
}
