using System;
using System.Collections.Generic;
using System.Text;

namespace Nexus.Core.Coleccion {
    /// <summary>
    /// Colorea codigo para TextMeshPro (etiquetas &lt;color&gt;): palabras clave, cadenas, comentarios y numeros; en
    /// bash, el comando, sus opciones y las rutas. Los «&lt;» y «&gt;» del codigo se protegen con &lt;noparse&gt; para que
    /// no se lean como etiquetas. Es una funcion pura: la escena del hallazgo solo pinta lo que devuelve.
    /// </summary>
    public static class ResaltadoDeCodigo {
        public const string ColorClave = "#C084FC";
        public const string ColorCadena = "#FBBF24";
        public const string ColorComentario = "#6B7A8C";
        public const string ColorNumero = "#F87171";
        public const string ColorComando = "#22D3EE";
        public const string ColorOpcion = "#FBBF24";
        public const string ColorRuta = "#34D399";

        private static readonly HashSet<string> Claves = new HashSet<string>(StringComparer.Ordinal) {
            // C#, Java, JS/TS, Python, Go: lo comun de verdad, no un diccionario de cada lenguaje.
            "if", "else", "for", "foreach", "while", "do", "return", "break", "continue", "switch", "case", "default",
            "class", "struct", "interface", "enum", "new", "public", "private", "protected", "internal", "static", "void",
            "var", "let", "const", "function", "def", "import", "from", "using", "namespace", "package", "try", "catch",
            "finally", "throw", "throws", "null", "true", "false", "None", "True", "False", "this", "self", "async", "await",
            "int", "string", "bool", "double", "float", "long", "lambda", "in", "is", "not", "and", "or", "func", "go"
        };

        public static string Colorear(string codigo, string lenguaje) {
            if (string.IsNullOrEmpty(codigo)) return "";
            return EsBash(lenguaje) ? Bash(codigo) : Generico(codigo, ComentarioDeLinea(lenguaje));
        }

        public static bool EsBash(string lenguaje) {
            return string.IsNullOrEmpty(lenguaje) || string.Equals(lenguaje, "bash", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(lenguaje, "sh", StringComparison.OrdinalIgnoreCase);
        }

        private static string ComentarioDeLinea(string lenguaje) {
            switch ((lenguaje ?? "").ToLowerInvariant()) {
                case "python": case "py": case "ruby": case "yaml": return "#";
                case "sql": return "--";
                default: return "//";
            }
        }

        /// <summary>Una linea de bash: la primera palabra es el comando; -x son opciones; lo que lleva / es una ruta.</summary>
        private static string Bash(string linea) {
            var sb = new StringBuilder();
            var palabras = linea.Split(' ');
            for (var i = 0; i < palabras.Length; i++) {
                if (i > 0) sb.Append(' ');
                var p = palabras[i];
                if (p.Length == 0) continue;
                string color = null;
                if (p.StartsWith("#", StringComparison.Ordinal)) {
                    // Un comentario: lo que queda de la linea.
                    sb.Append(Envolver(string.Join(" ", palabras, i, palabras.Length - i), ColorComentario));
                    break;
                }
                if (i == 0 || palabras[i - 1] == "|" || palabras[i - 1] == "&&" || palabras[i - 1] == "sudo") color = ColorComando;
                else if (p.StartsWith("-", StringComparison.Ordinal)) color = ColorOpcion;
                else if (p.IndexOf('/') >= 0 || p.StartsWith("~", StringComparison.Ordinal)) color = ColorRuta;
                else if (p.StartsWith("\"", StringComparison.Ordinal) || p.StartsWith("'", StringComparison.Ordinal)) color = ColorCadena;
                sb.Append(color == null ? Proteger(p) : Envolver(p, color));
            }
            return sb.ToString();
        }

        private static string Generico(string codigo, string comentario) {
            var sb = new StringBuilder();
            var i = 0;
            while (i < codigo.Length) {
                var c = codigo[i];
                // Comentario de linea.
                if (string.CompareOrdinal(codigo, i, comentario, 0, comentario.Length) == 0) {
                    var fin = codigo.IndexOf('\n', i);
                    if (fin < 0) fin = codigo.Length;
                    sb.Append(Envolver(codigo.Substring(i, fin - i), ColorComentario));
                    i = fin;
                    continue;
                }
                // Cadena.
                if (c == '"' || c == '\'') {
                    var j = i + 1;
                    while (j < codigo.Length && codigo[j] != c && codigo[j] != '\n') { if (codigo[j] == '\\') j++; j++; }
                    j = Math.Min(codigo.Length, j + 1);
                    sb.Append(Envolver(codigo.Substring(i, j - i), ColorCadena));
                    i = j;
                    continue;
                }
                // Numero.
                if (char.IsDigit(c)) {
                    var j = i;
                    while (j < codigo.Length && (char.IsDigit(codigo[j]) || codigo[j] == '.')) j++;
                    sb.Append(Envolver(codigo.Substring(i, j - i), ColorNumero));
                    i = j;
                    continue;
                }
                // Palabra.
                if (char.IsLetter(c) || c == '_') {
                    var j = i;
                    while (j < codigo.Length && (char.IsLetterOrDigit(codigo[j]) || codigo[j] == '_')) j++;
                    var palabra = codigo.Substring(i, j - i);
                    sb.Append(Claves.Contains(palabra) ? Envolver(palabra, ColorClave) : palabra);
                    i = j;
                    continue;
                }
                sb.Append(Proteger(c.ToString()));
                i++;
            }
            return sb.ToString();
        }

        private static string Envolver(string texto, string color) {
            return "<color=" + color + ">" + Proteger(texto) + "</color>";
        }

        /// <summary>
        /// El lenguaje de un trozo de codigo, a ojo (para colorearlo y ponerle extension en la revision de codigo):
        /// SELECT… FROM es SQL; «def » o «self.», Python; function, =&gt;, const o let, JavaScript; public o var, C#.
        /// Lo demas, JavaScript.
        /// </summary>
        public static string Adivinar(string codigo) {
            var c = codigo ?? "";
            if (c.IndexOf("SELECT ", StringComparison.OrdinalIgnoreCase) >= 0 && c.IndexOf(" FROM ", StringComparison.OrdinalIgnoreCase) >= 0) return "sql";
            if (c.Contains("def ") || c.Contains("elif ") || c.Contains("self.")) return "python";
            if (c.Contains("function") || c.Contains("=>") || c.Contains("const ") || c.Contains("let ")) return "javascript";
            if (c.Contains("public ") || c.Contains("private ") || c.Contains("var ") || c.Contains("foreach")) return "csharp";
            return "javascript";
        }

        /// <summary>La extension de archivo de un lenguaje.</summary>
        public static string Extension(string lenguaje) {
            switch ((lenguaje ?? "").ToLowerInvariant()) {
                case "python": return ".py";
                case "csharp": return ".cs";
                case "sql": return ".sql";
                default: return ".js";
            }
        }

        /// <summary>Los &lt; y &gt; del codigo no son etiquetas.</summary>
        public static string Proteger(string texto) {
            if (texto.IndexOf('<') < 0 && texto.IndexOf('>') < 0) return texto;
            return "<noparse>" + texto + "</noparse>";
        }
    }
}
