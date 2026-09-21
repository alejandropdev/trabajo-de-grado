using System;

namespace Nexus.Core.Jornada {
    /// <summary>Qué hay al otro lado de la alerta cuando el jugador llega a su escritorio.</summary>
    public static class TiposDeAlerta {
        public const string Decision = "decision";
        public const string Minijuego = "minijuego";

        public static bool EsValido(string tipo) {
            return string.Equals(tipo, Decision, StringComparison.Ordinal) ||
                   string.Equals(tipo, Minijuego, StringComparison.Ordinal);
        }
    }

    public static class EstadosDeAlerta {
        public const string Pendiente = "pendiente";
        public const string Atendida = "atendida";

        /// <summary>Se acabó la ventana. Produce el resultado 'omitido', que la rúbrica lee como incorrecta.</summary>
        public const string Expirada = "expirada";
    }

    /// <summary>
    /// C11 · La citación del momento (§3.3). «Te necesitan en tu escritorio.»
    ///
    /// ★ **La alerta NO es el telegrafiado**, y confundirlos destruye el argumento pedagógico del §7.7:
    ///
    /// · El **telegrafiado** llega *días antes* por un canal diegético — «el build tardó 14 min, ayer
    ///   tardaba 6» — y es lo que convierte el azar en gestión de riesgo. Lo lleva C4.
    /// · La **alerta** es el «ven ahora». Un evento bien telegrafiado hace días **también** llega como
    ///   alerta el día que toca.
    ///
    /// El nervio del día está en la ventana de atención: el jugador *puede* seguir con lo suyo, pero
    /// volver cuesta el tiempo de desplazamiento desde donde esté. **Estar lejos es un riesgo real.**
    ///
    /// Esta clase no sabe qué es un evento ni qué es un minijuego: solo lleva un id y un tipo. Quien
    /// las construye y quien las resuelve es GameSession, que sí conoce ambos mundos.
    /// </summary>
    public sealed class Alerta {
        /// <summary>El id del evento o del minijuego que hay detrás.</summary>
        public string Id;

        /// <summary>decision | minijuego.</summary>
        public string Tipo = TiposDeAlerta.Decision;

        /// <summary>Minuto del día en que suena.</summary>
        public int MinutoDeLaAlerta;

        /// <summary>Minuto del día en que se pierde. Normalmente la alerta + la ventana de atención.</summary>
        public int MinutoDeExpiracion;

        /// <summary>chat | correo | megafonia | ticket. Nunca un HUD narrativo (principio P2).</summary>
        public string Canal = "chat";

        public string Texto;

        public string Estado = EstadosDeAlerta.Pendiente;

        /// <summary>Minuto en que se resolvió, de una forma o de otra. -1 mientras sigue pendiente.</summary>
        public int MinutoDeResolucion = -1;

        /// <summary>
        /// Dónde estaba el jugador cuando esto se resolvió. El §8 lo pide explícitamente para la traza:
        /// dejar expirar una alerta registra «qué estaba haciendo el jugador en ese momento», que es
        /// evidencia directa de gestión de la atención.
        /// </summary>
        public string ZonaDelJugador;

        public bool EstaPendiente {
            get { return string.Equals(Estado, EstadosDeAlerta.Pendiente, StringComparison.Ordinal); }
        }

        public bool Expiro {
            get { return string.Equals(Estado, EstadosDeAlerta.Expirada, StringComparison.Ordinal); }
        }

        /// <summary>True si ya sonó a esta hora. Una alerta agendada para las 15:00 no existe a las 11:00.</summary>
        public bool YaSono(int minutoActual) { return minutoActual >= MinutoDeLaAlerta; }

        /// <summary>Cuánto queda antes de perderla. 0 si ya se pasó.</summary>
        public int MinutosParaExpirar(int minutoActual) {
            return Math.Max(0, MinutoDeExpiracion - minutoActual);
        }

        public Alerta Clone() { return (Alerta)MemberwiseClone(); }

        public override string ToString() {
            return $"{RelojDeJornada.Formatear(MinutoDeLaAlerta)} [{Canal}] {Id} " +
                   $"(hasta {RelojDeJornada.Formatear(MinutoDeExpiracion)}) · {Estado}";
        }
    }
}
