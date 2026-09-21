using System;
using System.Collections.Generic;
using Nexus.Core.Datos;

namespace Nexus.Core.Sesion {
    /// <summary>
    /// Que nivel viene despues de otro. Los ids siguen la forma "nivel-00", "nivel-01"…, asi que el orden
    /// natural de los ids es el orden del juego. Hoy es lineal; si algun dia hay niveles opcionales, este es
    /// el unico sitio que cambia.
    /// </summary>
    public static class ProgresionDeNiveles {
        public static IList<string> EnOrden(Catalogo catalogo) {
            if (catalogo == null) throw new ArgumentNullException(nameof(catalogo));
            var ids = new List<string>(catalogo.Niveles.Keys);
            ids.Sort(StringComparer.Ordinal);
            return ids;
        }

        public static string Primero(Catalogo catalogo) {
            var ids = EnOrden(catalogo);
            return ids.Count == 0 ? null : ids[0];
        }

        /// <summary>Null si no hay siguiente: el juego (o esta version del juego) termina ahi.</summary>
        public static string Siguiente(Catalogo catalogo, string nivelActual) {
            var ids = EnOrden(catalogo);
            var i = ids.IndexOf(nivelActual);
            if (i < 0) throw new InvalidOperationException($"'{nivelActual}' no es un nivel del catalogo.");
            return i + 1 < ids.Count ? ids[i + 1] : null;
        }
    }
}
