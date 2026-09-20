using System;
using System.Collections.Generic;

namespace Nexus.Core.Modelo {
    /// <summary>
    /// Newtonsoft reconstruye los diccionarios con el comparador POR DEFECTO: un
    /// Dictionary(StringComparer.OrdinalIgnoreCase) deja de ignorar mayusculas en cuanto pasa por el guardado.
    ///
    /// No da error. Da una busqueda fallida silenciosa — "Equipo" no encuentra "equipo" — y un peso de evento
    /// que se queda en 1.0 sin que nadie se entere. Es exactamente la clase de bug que INV-7 persigue:
    /// recargar no puede cambiar la partida. Por eso las clases que llevan diccionarios insensibles
    /// los reconstruyen con [OnDeserialized].
    /// </summary>
    internal static class Diccionarios {
        public static Dictionary<string, TValor> SinMayusculas<TValor>(Dictionary<string, TValor> origen) {
            if (origen == null) return null;
            if (ReferenceEquals(origen.Comparer, StringComparer.OrdinalIgnoreCase)) return origen;

            try {
                return new Dictionary<string, TValor>(origen, StringComparer.OrdinalIgnoreCase);
            } catch (ArgumentException ex) {
                throw new InvalidOperationException(
                    "Hay dos claves que solo se diferencian en las mayusculas en un bloque que no las distingue " +
                    "(por ejemplo 'Equipo' y 'equipo' en pesosPorTag). Deja solo una.", ex);
            }
        }
    }
}
