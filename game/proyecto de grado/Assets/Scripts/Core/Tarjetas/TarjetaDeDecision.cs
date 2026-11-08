using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Evaluacion;
using Nexus.Core.Sesion;

namespace Nexus.Core.Tarjetas {
    /// <summary>
    /// La decision de las 12:00 vista como tarjeta. La tarjeta solo la presenta: quien la muestra resuelve con
    /// GameSession.ResolverDecision, que ya aplica el multiplicador de la metodologia, agenda los diferidos y escribe
    /// la traza con la rubrica del evento. Por eso lo que se evalua no cambia ni una coma.
    ///
    /// Los efectos de cada respuesta son los deltas que el motor ya calculo para la decision de hoy (con el
    /// multiplicador puesto), asi que las barras enseñan lo mismo que pasara.
    /// </summary>
    public static class TarjetaDeDecision {
        public const string Momento = "decision";

        /// <summary>La tarjeta, o null si el evento no trae frase/etiquetas, una opcion esta bloqueada o toca el reloj (Dias).</summary>
        public static TarjetaDef De(PendingDecision d) {
            if (d == null || string.IsNullOrEmpty(d.Frase) || d.Opciones == null || d.Opciones.Count == 0) return null;
            if (d.Opciones.Count < LimitesDeTarjeta.MinimoDeOpciones || d.Opciones.Count > LimitesDeTarjeta.MaximoDeOpciones) return null;
            if (d.Opciones.Any(o => o == null || o.Bloqueada || string.IsNullOrEmpty(o.Etiqueta))) return null;
            // "Dias" se cobra en avance dentro de Previsualizar: volver a aplicarlo como delta lo contaria dos veces.
            if (d.Opciones.Any(o => o.Previsualizacion != null && o.Previsualizacion.Keys.Any(k => string.Equals(k, "Dias", StringComparison.OrdinalIgnoreCase)))) return null;

            return new TarjetaDef {
                Id = "DEC-" + d.EventoId,
                Personaje = d.Titulo,
                Frase = d.Frase,
                Momento = Momento,
                Origen = d.EventoId,
                Titulo = d.Titulo,
                Opciones = d.Opciones.Select(o => new OpcionTarjeta {
                    Id = o.Id,
                    Etiqueta = o.Etiqueta,
                    Veredicto = Veredictos.Aceptable,   // lo evalua la rubrica del evento, no la tarjeta
                    Razon = "",
                    Efectos = (o.Previsualizacion ?? new Dictionary<string, double>()).ToDictionary(kv => kv.Key, kv => (object)kv.Value)
                }).ToList()
            };
        }
    }
}
