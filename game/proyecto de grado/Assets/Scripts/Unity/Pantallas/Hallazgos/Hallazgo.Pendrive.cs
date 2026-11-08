using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nexus.Core.Coleccion;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    public static partial class Hallazgos {
        private static readonly Color Beige = new Color32(0xCF, 0xC6, 0xB0, 0xFF);
        private static readonly Color BeigeOscuro = new Color32(0xA8, 0x9F, 0x88, 0xFF);
        private static readonly Color Fosforo = new Color32(0x6B, 0xF1, 0x8A, 0xFF);
        private static readonly Color FosforoTenue = new Color32(0x2C, 0x6B, 0x3B, 0xFF);
        private static readonly Color Pantalla = new Color32(0x06, 0x14, 0x0B, 0xFF);

        /// <summary>
        /// Un fragmento del pendrive: un ordenador viejo de fosforo verde. El pendrive entra por el lateral y parpadea su
        /// luz; la pantalla monta la unidad con su barra, abre la carpeta con los ocho fragmentos (los encontrados se
        /// encienden, los demas esperan con «?»), hace doble clic en el nuevo, vuelca su hex, lo descifra y escribe el
        /// mensaje letra a letra.
        /// </summary>
        public static void Pendrive(ContextoDeHallazgo x) {
            var col = x.Col;
            var tema = x.Tema;
            var e = x.Escenario;
            var tengo = new HashSet<int>(x.Catalogo.Where(c => c.Serie == SeriesDeColeccionables.Usb && (x.Encontrados.Contains(c.Id) || c.Id == col.Id))
                                                   .Select(c => c.Fragmento));

            // ---- el ordenador
            x.Caja(e, "Pie", 0, -214, 300, 18, BeigeOscuro, 6);
            x.Caja(e, "Cuello", 0, -192, 160, 34, BeigeOscuro, 4);
            var monitor = x.Caja(e, "Monitor", -40, 56, 740, 500, Beige, 28);
            x.Caja(monitor.transform, "Sombra", 0, -236, 700, 10, NexusTheme.Alfa(BeigeOscuro, 0.8f), 5);
            x.Caja(monitor.transform, "Marco", 0, 14, 670, 430, new Color32(0x2A, 0x2A, 0x26, 0xFF), 20);
            var pantalla = x.Caja(monitor.transform, "Pantalla", 0, 16, 630, 392, Pantalla, 12);
            pantalla.gameObject.AddComponent<RectMask2D>();
            var gPantalla = pantalla.gameObject.AddComponent<CanvasGroup>();
            var led = x.Circulo(monitor.transform, "Luz", 300, -222, 12, FosforoTenue);
            x.Texto(monitor.transform, "NX-286", EstiloTexto.Leyenda, BeigeOscuro, -270, -222, 160, 20, TextAlignmentOptions.Left).characterSpacing = 6;
            x.Caja(monitor.transform, "Ranura", 372, -120, 10, 40, new Color32(0x22, 0x22, 0x20, 0xFF), 3);
            // El teclado.
            var teclado = x.Caja(e, "Teclado", -40, -284, 660, 58, BeigeOscuro, 10);
            for (var fila = 0; fila < 3; fila++)
                for (var k = 0; k < 14; k++)
                    x.Caja(teclado.transform, "Tecla", -286 + k * 44, 16 - fila * 16, 36, 11, Beige, 2);

            // El pendrive: cuerpo y conector.
            var pendrive = x.Nodo(e, "Pendrive", 640, -64, 110, 40);
            x.Caja(pendrive, "Cuerpo", 18, 0, 76, 36, new Color32(0x3A, 0x40, 0x4A, 0xFF), 8);
            x.Caja(pendrive, "Conector", -38, 0, 34, 24, new Color32(0xC9, 0xCF, 0xD6, 0xFF), 2);
            var luzPendrive = x.Circulo(pendrive, "Luz", 44, 0, 8, FosforoTenue);

            // ---- dentro de la pantalla: rayas de barrido
            DibujoUI dRayas;
            var rayas = x.Lamina(pantalla.transform, 0, 0, 630, 392, out dRayas);
            for (var y = 2; y < 392; y += 4) rayas.Linea(0, y, 630, y, new Color(0, 0, 0, 0.35f), 1);

            // a) el montaje
            var consola = x.Texto(pantalla.transform, "", EstiloTexto.Mono, Fosforo, 0, 110, 580, 150, TextAlignmentOptions.TopLeft);

            // b) la carpeta con los ocho fragmentos
            var carpeta = x.Nodo(pantalla.transform, "Carpeta", 0, -40, 600, 300);
            var gCarpeta = x.Grupo(carpeta);
            x.Caja(carpeta, "Barra", 0, 132, 600, 30, NexusTheme.Alfa(Fosforo, 0.15f));
            x.Texto(carpeta, $"/mnt/usb   ·   {tengo.Count} de 8 fragmentos", EstiloTexto.Mono, Fosforo, 0, 132, 580, 26, TextAlignmentOptions.Left);
            Image iconoNuevo = null;
            for (var i = 1; i <= 8; i++) {
                var cx = -225 + ((i - 1) % 4) * 150;
                var cy = (i <= 4 ? 52 : -72);
                var lo = tengo.Contains(i);
                var nuevo = i == col.Fragmento;
                var hoja = x.Caja(carpeta, "Fragmento " + i, cx, cy + 10, 50, 62, lo ? (nuevo ? Fosforo : FosforoTenue) : new Color(1, 1, 1, 0.06f), 4);
                if (nuevo) iconoNuevo = hoja;
                x.Caja(hoja.transform, "Esquina", 17, 23, 16, 16, Pantalla);
                x.Texto(hoja.transform, lo ? "BIN" : "?", EstiloTexto.Mono, lo ? Pantalla : new Color(1, 1, 1, 0.3f), 0, -6, 50, 26);
                x.Texto(carpeta, $"fragmento_{i}.bin", EstiloTexto.Mono, lo ? Fosforo : new Color(1, 1, 1, 0.25f), cx, cy - 40, 150, 22).fontSize *= 0.75f;
            }

            // c) el volcado hexadecimal y d) el mensaje
            var hex = x.Caja(pantalla.transform, "Hex", 0, 0, 630, 392, Pantalla);
            var gHex = x.Grupo(hex);
            var lineasHex = x.Texto(hex.transform, "", EstiloTexto.Mono, FosforoTenue, 0, 20, 580, 330, TextAlignmentOptions.TopLeft);
            var estadoHex = x.Texto(hex.transform, "", EstiloTexto.Mono, Fosforo, 0, -166, 580, 30, TextAlignmentOptions.Left);
            var mensaje = x.Caja(pantalla.transform, "Mensaje", 0, 0, 630, 392, Pantalla);
            var gMensaje = x.Grupo(mensaje);
            x.Caja(mensaje.transform, "Cabecera", 0, 176, 630, 40, NexusTheme.Alfa(Fosforo, 0.15f));
            x.Texto(mensaje.transform, $"fragmento_{col.Fragmento}.txt   [descifrado]", EstiloTexto.Mono, Fosforo, 0, 176, 590, 30, TextAlignmentOptions.Left);
            var cuerpo = x.Texto(mensaje.transform, col.Texto, EstiloTexto.Mono, Fosforo, 0, 10, 570, 280, TextAlignmentOptions.TopLeft);
            cuerpo.fontSize *= 1.15f;
            cuerpo.lineSpacing = 12;

            // ---- el contador de la esquina: ocho casillas
            var contador = x.Nodo(e, "Contador", 520, 300, 220, 60);
            x.Texto(contador, "FRAGMENTOS", EstiloTexto.Leyenda, Fosforo, 0, 16, 220, 20).characterSpacing = 5;
            for (var i = 1; i <= 8; i++) {
                var casilla = x.Caja(contador, "Casilla", -91 + (i - 1) * 26, -10, 20, 20, tengo.Contains(i) ? Fosforo : new Color(1, 1, 1, 0.08f), 3);
                if (i == col.Fragmento) x.Sec.Siempre(t => casilla.transform.localScale = Vector3.one * (1 + 0.15f * Mathf.Max(0, Mathf.Sin(t * 5))));
            }

            // ---- el ritmo
            var s = x.Sec;
            s.En(0f, 0.7f, p => pendrive.anchoredPosition = new Vector2(Mathf.Lerp(700, 376, ContextoDeHallazgo.Suave(p)), -64));
            s.En(0.7f, 0.5f, p => {
                var encendida = p > 0 && Mathf.Repeat(p * 6, 1) < 0.5f || p >= 1;
                luzPendrive.color = encendida ? Fosforo : FosforoTenue;
                led.color = p > 0 ? Fosforo : FosforoTenue;
            });
            var barra = 24;
            s.En(1.2f, 1.1f, p => {
                if (p <= 0) { consola.text = ""; return; }
                var n = Mathf.RoundToInt(barra * ContextoDeHallazgo.Suave(p));
                consola.text = "$ mount /dev/sdb1 /mnt/usb\nMontando la unidad...\n[" + new string('#', n) + new string('.', barra - n) + $"]  {Mathf.RoundToInt(p * 100)}%" +
                               (p >= 1 ? "\nOK. 8 elementos." : "");
            });
            s.En(2.4f, 0.3f, p => { gCarpeta.alpha = p; if (p >= 1) consola.gameObject.SetActive(false); });
            s.En(2.8f, 0.5f, p => {
                // El doble clic: el icono nuevo destella dos veces.
                if (iconoNuevo == null) return;
                var destello = p > 0 && p < 1 && Mathf.Repeat(p * 4, 1) < 0.5f;
                iconoNuevo.color = destello ? Color.white : Fosforo;
                iconoNuevo.transform.localScale = Vector3.one * (destello ? 1.12f : 1f);
            });
            var rnd = new System.Random(col.Fragmento * 97);
            s.En(3.3f, 1.0f, p => {
                gHex.alpha = p > 0 ? 1 : 0;
                if (p <= 0) return;
                var sb = new StringBuilder();
                var desde = (int)(p * 40);
                for (var l = 0; l < 11; l++) {
                    sb.Append(((desde + l) * 16).ToString("X6")).Append("  ");
                    for (var b = 0; b < 12; b++) sb.Append(rnd.Next(256).ToString("X2")).Append(' ');
                    sb.Append('\n');
                }
                lineasHex.text = sb.ToString();
                estadoHex.text = p < 1 ? "descifrando" + new string('.', 1 + (int)(p * 9) % 3) : "descifrado. OK";
            });
            s.En(4.3f, 0.25f, p => gMensaje.alpha = p);
            s.En(4.5f, Mathf.Clamp((col.Texto ?? "").Length / 45f, 1.2f, 3.5f), p => ContextoDeHallazgo.Escribir(cuerpo, p));
            s.Siempre(t => {
                // El fosforo parpadea un pelo, como las pantallas de antes.
                gPantalla.alpha = 0.94f + 0.06f * Mathf.PerlinNoise(t * 8f, 0.3f);
                if (t > 1.2f) led.color = Mathf.Repeat(t * 3.3f, 1) < 0.15f ? FosforoTenue : Fosforo;
            });
        }
    }
}
