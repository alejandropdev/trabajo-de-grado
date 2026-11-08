using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nexus.Core.Evaluacion;
using Nexus.Core.Sesion;

namespace Nexus.Core.Tarjetas {
    /// <summary>
    /// La retrospectiva y la planificacion, vistas como tarjeta. Igual que las ceremonias, NO pasan por
    /// GameSession.ResolverTarjeta: la tarjeta solo las presenta y quien la muestra llama a ElegirAccionRetro o
    /// Comprometer con lo elegido. Lo que enseñan sigue siendo lo mismo (cambiar UNA constante del proceso; no
    /// prometer mas de lo que cabe) y las consecuencias siguen sin previsualizarse.
    /// </summary>
    public static class TarjetaDeProceso {
        public const string MomentoRetro = "retro";
        public const string MomentoPlanificacion = "planificacion";
        public const string FraseDeRetroPorDefecto = "¿Qué cambiamos del proceso?";

        /// <summary>
        /// La tarjeta de la retrospectiva, o null si alguna accion no trae etiqueta (sigue la vista larga). La frase
        /// es la de la ceremonia; sin ella, una por defecto.
        /// </summary>
        public static TarjetaDef DeRetro(RetroRequest retro, string nombreDeCeremonia) {
            if (retro == null || retro.Acciones == null || retro.Acciones.Count < LimitesDeTarjeta.MinimoDeOpciones ||
                retro.Acciones.Count > LimitesDeTarjeta.MaximoDeOpciones) return null;
            if (retro.Acciones.Any(a => a == null || string.IsNullOrEmpty(a.Etiqueta))) return null;

            return new TarjetaDef {
                Id = "RETRO-" + retro.CeremoniaId,
                Personaje = string.IsNullOrEmpty(nombreDeCeremonia) ? "Retrospectiva" : nombreDeCeremonia,
                Frase = string.IsNullOrEmpty(retro.Frase) ? FraseDeRetroPorDefecto : retro.Frase,
                Momento = MomentoRetro,
                Origen = "CEREMONIA",
                Titulo = nombreDeCeremonia,
                Opciones = retro.Acciones.Select(a => new OpcionTarjeta {
                    Id = a.Id,
                    Etiqueta = a.Etiqueta,
                    Veredicto = Veredictos.Aceptable,   // la retro no evalua en la traza: cambia una constante
                    Razon = a.Explicacion,
                    Efectos = new Dictionary<string, object>()
                }).ToList()
            };
        }

        /// <summary>
        /// La tarjeta de «¿con cuantos puntos nos comprometemos?»: menos, lo que cabe, o mas. Las tres etiquetas son
        /// la cifra, y la del medio es la sugerida por el modelo; pasarse genera sobrecompromiso, que no se avisa.
        /// El id de cada opcion es el compromiso en puntos (texto invariante), para que quien la muestra llame a
        /// Comprometer con exactamente eso.
        /// </summary>
        public static TarjetaDef DePlanificacion(PlanningRequest plan, bool solo) {
            if (plan == null) return null;
            var justo = Math.Max(1, Math.Round(plan.CapacidadSugerida));
            var holgura = Math.Max(1, Math.Round(justo * 0.3));
            var cifras = new[] { Math.Max(0, justo - holgura), justo, justo + holgura };

            return new TarjetaDef {
                Id = "PLAN-" + plan.Unidad,
                Personaje = "Planificación",
                Frase = solo ? "¿Con cuántos puntos te comprometes?" : "¿Con cuántos puntos nos comprometemos?",
                Ayuda = $"Caben unos {plan.CapacidadSugerida:0.#}. Quedan {plan.PuntosPendientes:0} por hacer.",
                Termino = "planificacion-de-sprint",
                Momento = MomentoPlanificacion,
                Origen = "CEREMONIA",
                Titulo = "Planificación",
                Opciones = cifras.Select(c => new OpcionTarjeta {
                    Id = c.ToString("0.##", CultureInfo.InvariantCulture),
                    Etiqueta = c.ToString("0.##", CultureInfo.InvariantCulture) + " puntos",
                    Veredicto = Veredictos.Aceptable,
                    Efectos = new Dictionary<string, object>()
                }).ToList()
            };
        }

        /// <summary>Los puntos que lleva una opcion de la tarjeta de planificacion (su id).</summary>
        public static double PuntosDe(OpcionTarjeta opcion) {
            double puntos;
            return opcion != null && double.TryParse(opcion.Id, NumberStyles.Float, CultureInfo.InvariantCulture, out puntos) ? puntos : 0;
        }
    }
}
