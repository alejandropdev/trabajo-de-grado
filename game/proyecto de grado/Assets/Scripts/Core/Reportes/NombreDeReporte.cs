using System;
using System.Globalization;
using System.Text;

namespace Nexus.Core.Reportes {
    /// <summary>
    /// El nombre del archivo del informe: <c>NexusProtocol_Lecciones_N1_Ana_Perez_2026-10-28_1030.pdf</c>.
    /// Sin tildes, espacios ni caracteres que Windows no admita, para que se pueda adjuntar a un correo tal cual.
    /// Lleva la hora para que repetir un nivel no sobrescriba el informe anterior.
    /// </summary>
    public static class NombreDeReporte {
        private const int MaximoPorParte = 30;

        public static string Para(string nivelId, string estudiante, DateTime fecha) {
            return "NexusProtocol_Lecciones_" + Sanear(nivelId, "nivel") + "_" + Sanear(estudiante, "estudiante") + "_" +
                   fecha.ToString("yyyy-MM-dd_HHmm", CultureInfo.InvariantCulture) + ".pdf";
        }

        /// <summary>Solo letras y cifras ASCII, con «_» entre palabras. Vacio → 'porDefecto'.</summary>
        public static string Sanear(string texto, string porDefecto) {
            var limpio = PdfWriter.QuitarTildes(texto ?? "");
            var sb = new StringBuilder();
            var separador = false;
            foreach (var ch in limpio) {
                if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')) {
                    if (separador && sb.Length > 0) sb.Append('_');
                    sb.Append(ch);
                    separador = false;
                } else {
                    separador = true;
                }
                if (sb.Length >= MaximoPorParte) break;
            }
            return sb.Length == 0 ? porDefecto : sb.ToString();
        }
    }
}
