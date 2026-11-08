using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Evaluacion;
using Nexus.Core.Sesion;

namespace Nexus.Core.Tarjetas {
    /// <summary>
    /// Una ceremonia de hoy, vista como tarjeta. Las ceremonias ya tienen sus formas de llevarlas (opciones con efectos
    /// y acta) y no evaluan en la traza, asi que NO pasan por GameSession.ResolverTarjeta: la tarjeta solo las presenta
    /// y quien la muestra llama a AsistirACeremonia con la opcion elegida. Lo que cambia es la forma: una frase y
    /// etiquetas de tres palabras, en vez de un parrafo y frases largas.
    /// </summary>
    public static class TarjetaDeCeremonia {
        public const string Momento = "ceremonia";

        /// <summary>La tarjeta de la ceremonia, o null si no trae frase o alguna opcion no trae etiqueta (sigue la vista larga).</summary>
        public static TarjetaDef De(CeremoniaDeHoy c) {
            if (c == null || string.IsNullOrEmpty(c.Frase) || c.Opciones == null || c.Opciones.Count == 0) return null;
            if (c.Opciones.Any(o => o == null || string.IsNullOrEmpty(o.Etiqueta))) return null;

            return new TarjetaDef {
                Id = "CER-" + c.Id,
                Personaje = c.Nombre,
                Frase = c.Frase,
                Ayuda = c.Ayuda,
                Momento = Momento,
                Origen = "CEREMONIA",
                Titulo = c.Nombre,
                Opciones = c.Opciones.Select(o => new OpcionTarjeta {
                    Id = o.Id,
                    Etiqueta = o.Etiqueta,
                    Veredicto = Veredictos.Aceptable,   // las ceremonias no evaluan; lo cuenta el acta, no la traza
                    Razon = o.Acta,
                    Efectos = o.Efectos == null ? new Dictionary<string, object>() : new Dictionary<string, object>(o.Efectos)
                }).ToList()
            };
        }
    }
}
