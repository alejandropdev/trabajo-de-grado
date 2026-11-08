using System.Text;

namespace Nexus.Core.Narrativa {
    /// <summary>
    /// Los simbolos que la letra del juego (Rajdhani) NO tiene se pintaban como cuadritos (2026-11-06): ✓ √ ▸ ► ▾ ▲ ▼ ＋
    /// ✱ ★ ○ ● ← → ↵ ⏸ ▶. Antes de pintar un texto se cambian por algo que si tiene (o se quitan). Lo comprobo fontTools
    /// sobre Resources/Fuentes/Rajdhani-Medium.ttf.
    /// </summary>
    public static class Glifos {
        public static string Seguro(string texto) {
            if (string.IsNullOrEmpty(texto)) return texto ?? "";
            StringBuilder sb = null;
            for (var i = 0; i < texto.Length; i++) {
                var c = texto[i];
                var cambio = Cambio(c);
                if (cambio == null) { if (sb != null) sb.Append(c); continue; }
                if (sb == null) { sb = new StringBuilder(texto.Length); sb.Append(texto, 0, i); }
                sb.Append(cambio);
            }
            if (sb == null) return texto;
            return sb.ToString().Replace("  ", " ").Trim();
        }

        /// <summary>null = el caracter se queda; si no, por que se cambia ("" = se quita).</summary>
        public static string Cambio(char c) {
            switch (c) {
                case '►': case '▸': case '▶': return "»";
                case '▾': case '▼': case '▲': case '↵': case '⏸': return "";
                case '√': case '✓': return "";
                case '＋': return "+";
                case '✱': case '★': return "*";
                case '○': return "o";
                case '●': return "•";
                case '←': return "<";
                case '→': return ">";
                default: return null;
            }
        }
    }

    /// <summary>
    /// Un orden barajado pero fijo: la misma clave da siempre el mismo numero (FNV-1a), en cualquier maquina. Sirve para
    /// barajar las respuestas de una charla sin que cambien cada vez que se repinta la pantalla.
    /// </summary>
    public static class Barajar {
        public static int Clave(string texto) {
            unchecked {
                var h = 2166136261u;
                foreach (var c in texto ?? "") { h ^= c; h *= 16777619u; }
                return (int)(h & 0x7FFFFFFF);
            }
        }
    }
}
