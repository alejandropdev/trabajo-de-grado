using System.Linq;
using Nexus.Core.Coleccion;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    public static partial class Hallazgos {
        private static readonly Color FondoTerminal = new Color32(0x0D, 0x11, 0x17, 0xFF);
        private static readonly Color BarraTerminal = new Color32(0x1C, 0x21, 0x28, 0xFF);

        /// <summary>
        /// Un codigo: si es un comando, una terminal bash donde se teclea letra a letra con su cursor, responde linea a
        /// linea y deja la nota del diario como comentario; si es codigo de otro lenguaje, un IDE con pestaña, numeros
        /// de linea, colores, minimapa y barra de estado, que se ejecuta solo y abre su panel de salida.
        /// </summary>
        public static void Codigo(ContextoDeHallazgo x) {
            if (x.Col.EsDeTerminal) Terminal(x);
            else Ide(x);
        }

        private static void Terminal(ContextoDeHallazgo x) {
            var col = x.Col;
            var tema = x.Tema;
            var ventana = x.Caja(x.Escenario, "Terminal", 0, 20, 980, 560, FondoTerminal, 14);
            x.Ui.Borde((RectTransform)ventana.transform, new Color32(0x30, 0x36, 0x3D, 0xFF), 14, 1.5f);
            ventana.gameObject.AddComponent<RectMask2D>();
            var v = ventana.transform;
            x.Caja(v, "Barra", 0, 262, 980, 36, BarraTerminal);
            x.Circulo(v, "Cerrar", -462, 262, 14, new Color32(0xF8, 0x71, 0x71, 0xFF));
            x.Circulo(v, "Minimizar", -440, 262, 14, new Color32(0xFB, 0xBF, 0x24, 0xFF));
            x.Circulo(v, "Ampliar", -418, 262, 14, new Color32(0x34, 0xD3, 0x99, 0xFF));
            x.Texto(v, "nobody@nexus: ~", EstiloTexto.Mono, tema.inkMuted, 0, 262, 400, 28);

            const string prompt = "<color=#34D399>nobody@nexus</color><color=#94A1B2>:</color><color=#22D3EE>~</color><color=#94A1B2>$</color> ";
            var pantalla = x.Texto(v, "", EstiloTexto.Mono, tema.ink, 0, -20, 920, 500, TextAlignmentOptions.TopLeft);
            pantalla.fontSize *= 1.2f;
            pantalla.lineSpacing = 18;
            var salida = (col.Revela ?? "").Split('\n');
            var comentario = "<color=" + ResaltadoDeCodigo.ColorComentario + "># " + ResaltadoDeCodigo.Proteger(col.Texto ?? "") + "</color>";
            var comando = col.Comando ?? "";

            var tecleado = 0;
            var lineas = 0;
            var conComentario = false;
            var s = x.Sec;
            s.En(0.4f, Mathf.Clamp(comando.Length * 0.07f, 0.6f, 2.2f), p => tecleado = Mathf.RoundToInt(comando.Length * p));
            var finTecleo = 0.4f + Mathf.Clamp(comando.Length * 0.07f, 0.6f, 2.2f);
            s.En(finTecleo + 0.35f, 0.15f * salida.Length + 0.01f, p => lineas = Mathf.CeilToInt(salida.Length * p));
            s.En(finTecleo + 0.6f + 0.15f * salida.Length, 0.4f, p => conComentario = p > 0);
            var gVentana = x.Grupo(ventana);
            s.En(0f, 0.3f, p => { gVentana.alpha = p; ventana.transform.localScale = Vector3.one * Mathf.Lerp(0.96f, 1, ContextoDeHallazgo.Suave(p)); });
            s.Siempre(t => {
                // Se repinta cada fotograma: lo tecleado (con sus colores), la salida, el comentario y el cursor.
                var cursor = Mathf.Repeat(t, 1f) < 0.55f ? "<color=#EEF3F7>_</color>" : " ";
                var texto = prompt + ResaltadoDeCodigo.Colorear(comando.Substring(0, tecleado), "bash");
                var termino = tecleado >= comando.Length && lineas > 0;
                if (!termino) { pantalla.text = texto + cursor; return; }
                texto += "\n";
                for (var i = 0; i < lineas && i < salida.Length; i++) texto += "<color=#EEF3F7>" + ResaltadoDeCodigo.Proteger(salida[i]) + "</color>\n";
                if (conComentario) texto += "\n" + comentario + "\n";
                pantalla.text = texto + "\n" + prompt + cursor;
            });
        }

        private static void Ide(ContextoDeHallazgo x) {
            var col = x.Col;
            var tema = x.Tema;
            var lenguaje = col.Lenguaje ?? "";
            var ventana = x.Caja(x.Escenario, "IDE", 0, 20, 1120, 620, new Color32(0x1E, 0x1F, 0x24, 0xFF), 10);
            ventana.gameObject.AddComponent<RectMask2D>();
            var v = ventana.transform;
            // La barra de actividad, el explorador estrecho y la pestaña del archivo.
            x.Caja(v, "Actividad", -538, 0, 44, 620, new Color32(0x16, 0x17, 0x1B, 0xFF));
            for (var i = 0; i < 4; i++) x.Caja(v, "Icono", -538, 270 - i * 50, 22, 22, NexusTheme.Alfa(tema.inkMuted, i == 0 ? 0.9f : 0.35f), 4);
            x.Caja(v, "Pestañas", 22, 290, 1076, 40, new Color32(0x18, 0x19, 0x1D, 0xFF));
            var pestana = x.Caja(v, "Pestaña", -400, 290, 200, 40, new Color32(0x1E, 0x1F, 0x24, 0xFF));
            x.Caja(pestana.transform, "Raya", 0, 18, 200, 3, tema.cyan);
            x.Texto(pestana.transform, "hallazgo" + Extension(lenguaje), EstiloTexto.Mono, tema.ink, 0, 0, 190, 30);
            // El boton de ejecutar.
            var ejecutar = x.Caja(v, "Ejecutar", 500, 290, 90, 30, tema.success, 6);
            x.Texto(ejecutar.transform, "RUN", EstiloTexto.Leyenda, tema.onSuccess, 0, 0, 90, 30);

            var codigo = (col.Comando ?? "").Replace("\r", "");
            var nLineas = Mathf.Max(1, codigo.Split('\n').Length);
            var gutter = x.Texto(v, string.Join("\n", Enumerable.Range(1, nLineas)), EstiloTexto.Mono, tema.inkFaint, -470, -10, 50, 520, TextAlignmentOptions.TopRight);
            gutter.lineSpacing = 10;
            var editor = x.Texto(v, ResaltadoDeCodigo.Colorear(codigo, lenguaje), EstiloTexto.Mono, tema.ink, 10, -10, 860, 520, TextAlignmentOptions.TopLeft);
            editor.lineSpacing = 10;
            // El minimapa: una raya por linea, del largo de la linea.
            var lineas = codigo.Split('\n');
            for (var i = 0; i < lineas.Length && i < 60; i++)
                x.Caja(v, "Mini", 490 + Mathf.Min(60, lineas[i].Length) / 2f, 250 - i * 4, Mathf.Min(60, lineas[i].Length) + 1, 2, NexusTheme.Alfa(tema.inkMuted, 0.5f));
            // La barra de estado.
            var estado = x.Caja(v, "Estado", 0, -298, 1120, 24, tema.cyan);
            x.Texto(estado.transform, $"Ln {nLineas}, Col {lineas[lineas.Length - 1].Length + 1}   UTF-8   {NombreDeLenguaje(lenguaje)}", EstiloTexto.Mono, tema.onCyan,
                    300, 0, 500, 22, TextAlignmentOptions.Right).fontSize *= 0.8f;
            // El panel de salida, que sube al ejecutar.
            var panel = x.Caja(v, "Salida", 22, -400, 1076, 200, new Color32(0x16, 0x17, 0x1B, 0xFF));
            x.Caja(panel.transform, "Raya", 0, 99, 1076, 1, new Color32(0x30, 0x36, 0x3D, 0xFF));
            x.Texto(panel.transform, "SALIDA", EstiloTexto.Leyenda, tema.inkMuted, -440, 80, 160, 22, TextAlignmentOptions.Left);
            var resultado = x.Texto(panel.transform, ResaltadoDeCodigo.Proteger(col.Revela ?? "") + "\n\n<color=" + ResaltadoDeCodigo.ColorComentario + ">" +
                                    ResaltadoDeCodigo.Proteger(col.Texto ?? "") + "</color>", EstiloTexto.Mono, tema.ink, 0, -10, 1020, 150, TextAlignmentOptions.TopLeft);

            var s = x.Sec;
            var gVentana = x.Grupo(ventana);
            s.En(0f, 0.3f, p => gVentana.alpha = p);
            s.En(0.3f, Mathf.Clamp(codigo.Length / 60f, 0.8f, 2.5f), p => ContextoDeHallazgo.Escribir(editor, p));
            var fin = 0.3f + Mathf.Clamp(codigo.Length / 60f, 0.8f, 2.5f);
            s.En(fin + 0.2f, 0.3f, p => ejecutar.transform.localScale = Vector3.one * (p > 0 && p < 1 ? 0.88f : 1f));
            s.En(fin + 0.5f, 0.4f, p => ((RectTransform)panel.transform).anchoredPosition = new Vector2(22, Mathf.Lerp(-400, -186, ContextoDeHallazgo.Suave(p))));
            s.En(fin + 0.9f, 1.2f, p => ContextoDeHallazgo.Escribir(resultado, p));
        }

        private static string Extension(string lenguaje) {
            switch ((lenguaje ?? "").ToLowerInvariant()) {
                case "csharp": case "c#": return ".cs";
                case "python": case "py": return ".py";
                case "javascript": case "js": return ".js";
                case "typescript": case "ts": return ".ts";
                case "java": return ".java";
                case "sql": return ".sql";
                case "go": return ".go";
                default: return ".txt";
            }
        }

        private static string NombreDeLenguaje(string lenguaje) {
            switch ((lenguaje ?? "").ToLowerInvariant()) {
                case "csharp": case "c#": return "C#";
                case "python": case "py": return "Python";
                case "javascript": case "js": return "JavaScript";
                case "typescript": case "ts": return "TypeScript";
                case "sql": return "SQL";
                default: return string.IsNullOrEmpty(lenguaje) ? "Texto" : lenguaje;
            }
        }
    }
}
