using System;
using Nexus.Core.Jornada;

namespace Nexus.Core.Servicios {
    /// <summary>
    /// C10 · A qué hora del día suena una alerta (§7.5, paso 7).
    ///
    /// Es **lo único que cambió** al pasar del día de cuatro ventanas al día continuo: antes el
    /// director agendaba un *día*, ahora agenda un *(día, minuto)*. Los otros siete pasos del ciclo
    /// son idénticos.
    ///
    /// ★ El minuto sale del **mismo `DeterministicRng`** que eligió el evento, así que la
    /// reproducibilidad del Modo Aula se mantiene **al minuto**: dos estudiantes con la misma semilla
    /// no solo sufren las mismas crisis, las sufren a la misma hora.
    /// </summary>
    public static class SorteoDeMinuto {
        /// <summary>
        /// Elige el minuto en que sonará una alerta.
        ///
        /// Solo dentro del tramo donde **la ventana de atención entera cabe antes del cierre**. Si una
        /// alerta pudiera sonar a las 17:00 con tres horas de plazo, tendría de hecho una hora, y el
        /// jugador sería castigado por la hora a la que el azar decidió llamarle. Aquí todas las
        /// alertas valen lo mismo.
        /// </summary>
        public static int Elegir(JornadaConfig jornada, DeterministicRng rng) {
            if (jornada == null) throw new ArgumentNullException(nameof(jornada));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            var primero = jornada.HoraInicio * RelojDeJornada.MinutosPorHora;
            var ultimo = jornada.HoraCierre * RelojDeJornada.MinutosPorHora - jornada.VentanaDeAtencionMinutos;

            // Jornada tan corta que no cabe ni una ventana entera: suena al empezar y se acorta sola.
            if (ultimo <= primero) return primero;

            var paso = Math.Max(1, jornada.GranularidadDeAlertas);
            var huecos = (ultimo - primero) / paso + 1;

            return primero + rng.Next(huecos) * paso;
        }

        /// <summary>Cuándo se pierde: la alerta más su ventana, sin pasar nunca del cierre.</summary>
        public static int Expiracion(JornadaConfig jornada, int minutoDeLaAlerta) {
            var cierre = jornada.HoraCierre * RelojDeJornada.MinutosPorHora;
            var expira = minutoDeLaAlerta + jornada.VentanaDeAtencionMinutos;
            return expira > cierre ? cierre : expira;
        }
    }
}
