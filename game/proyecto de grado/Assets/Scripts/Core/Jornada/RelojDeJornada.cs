using System;
using System.Globalization;

namespace Nexus.Core.Jornada {
    /// <summary>
    /// C11 · Como es un dia de trabajo en este nivel (§3.3). Vive en el LevelProfile, asi que un nivel
    /// puede tener jornadas mas cortas que otro sin tocar codigo — el prologo, por ejemplo.
    /// </summary>
    public sealed class JornadaConfig {
        /// <summary>Hora a la que empieza la jornada.</summary>
        public int HoraInicio = 8;

        /// <summary>
        /// La hora del cierre: aqui aparece la decision mas importante del juego, quedarse o irse.
        /// Es el unico momento fijo que sobrevivio al dia continuo.
        /// </summary>
        public int HoraCierre = 18;

        /// <summary>Hasta donde llega el dia si el jugador decide quedarse.</summary>
        public int HoraLimite = 20;

        /// <summary>
        /// Cuantos segundos REALES dura una hora del juego. El motor no lo usa nunca: es la escala que
        /// lee la capa de presentacion para hacer correr el reloj.
        ///
        /// El §M8 avisa de que este numero y la ventana de atencion "son los dos que definen si el dia
        /// se siente tenso o agobiante, y hay que jugarlos, no razonarlos". Por eso esta en datos.
        /// </summary>
        public double SegundosRealesPorHora = 20.0;

        /// <summary>
        /// Minutos del juego que el jugador tiene para atender una alerta antes de que expire.
        /// Tres horas por defecto: suficiente para terminar lo que estaba haciendo, no para olvidarlo.
        /// </summary>
        public int VentanaDeAtencionMinutos = 180;

        public JornadaConfig Clone() { return (JornadaConfig)MemberwiseClone(); }
    }

    /// <summary>
    /// C11 · El reloj de la jornada (§3.3). **El dia de la Fase 2 no tiene turnos.**
    ///
    /// Sustituye a las cuatro ventanas fijas (09:00 / 12:00 / 15:00 / 18:00), que el propio Documento
    /// Maestro marca como documentacion obsoleta en su §13.2. Ahora el tiempo pasa de verdad y en
    /// cualquier instante el trabajo puede reclamar al jugador.
    ///
    /// ★ Este reloj NO sabe que existe el tiempo real. Solo cuenta minutos del juego y los hace
    /// avanzar cuando alguien se lo pide. Quien decide cada cuanto pedirselo es la capa de
    /// presentacion, leyendo SegundosRealesPorHora. Asi el motor sigue siendo determinista y
    /// comprobable sin abrir Unity — que es lo que sostiene INV-7.
    /// </summary>
    public sealed class RelojDeJornada {
        public const int MinutosPorHora = 60;

        public JornadaConfig Config { get; private set; }

        /// <summary>Minutos desde medianoche. 480 = 08:00.</summary>
        public int Minuto { get; private set; }

        /// <summary>True si el jugador eligio quedarse y el dia sigue mas alla del cierre.</summary>
        public bool Prorrogada { get; private set; }

        public RelojDeJornada(JornadaConfig config) {
            Config = config ?? new JornadaConfig();
            Comprobar();
            Reiniciar();
        }

        public int Hora { get { return Minuto / MinutosPorHora; } }
        public int MinutoDeLaHora { get { return Minuto % MinutosPorHora; } }

        public int MinutoDeInicio { get { return Config.HoraInicio * MinutosPorHora; } }
        public int MinutoDeCierre { get { return Config.HoraCierre * MinutosPorHora; } }
        public int MinutoLimite {
            get { return (Prorrogada ? Config.HoraLimite : Config.HoraCierre) * MinutosPorHora; }
        }

        /// <summary>Llego la hora de decidir si se queda o se va.</summary>
        public bool LlegoElCierre { get { return Minuto >= MinutoDeCierre; } }

        /// <summary>Ya no queda dia: o son las 18:00 sin prorrogar, o son las 20:00.</summary>
        public bool Terminada { get { return Minuto >= MinutoLimite; } }

        public int MinutosRestantes { get { return Math.Max(0, MinutoLimite - Minuto); } }

        /// <summary>Deja el reloj en la hora de inicio y sin prorroga. Se llama al empezar cada dia.</summary>
        public void Reiniciar() {
            Minuto = MinutoDeInicio;
            Prorrogada = false;
        }

        /// <summary>Rehidrata el reloj desde el guardado. No valida contra el cierre: el guardado manda.</summary>
        public void Restaurar(int minuto, bool prorrogada) {
            Prorrogada = prorrogada;
            Minuto = minuto < MinutoDeInicio ? MinutoDeInicio : minuto;
        }

        /// <summary>
        /// Hace correr el reloj. Topa en el limite del dia y devuelve los minutos que AVANZO DE VERDAD,
        /// que puede ser menos de los pedidos: quien llama necesita saberlo para cobrar solo el tiempo
        /// que existio (un desplazamiento de 30 min a las 17:50 no cuesta 30 min, cuesta 10).
        /// </summary>
        public int Avanzar(int minutos) {
            if (minutos <= 0) return 0;

            var destino = Minuto + minutos;
            if (destino > MinutoLimite) destino = MinutoLimite;

            var avanzados = destino - Minuto;
            Minuto = destino;
            return avanzados;
        }

        /// <summary>
        /// Saltar directo a una hora del dia, sin pasar por el tiempo intermedio. Lo usa el boton de
        /// "cerrar jornada", que solo se ofrece cuando no queda nada pendiente. Devuelve los minutos saltados.
        /// </summary>
        public int SaltarHasta(int minutoDestino) {
            return Avanzar(minutoDestino - Minuto);
        }

        /// <summary>
        /// Quedarse a trabajar. Solo tiene sentido en el cierre: antes no hay nada que prorrogar.
        /// Devuelve false si se pidio fuera de tiempo o si el nivel no tiene horas extra configuradas.
        /// </summary>
        public bool Prorrogar() {
            if (Prorrogada || !LlegoElCierre) return false;
            if (Config.HoraLimite <= Config.HoraCierre) return false;

            Prorrogada = true;
            return true;
        }

        public override string ToString() { return Formatear(Minuto); }

        /// <summary>"08:00". Siempre en cultura invariante, como todo lo que se audita.</summary>
        public static string Formatear(int minuto) {
            var h = minuto / MinutosPorHora;
            var m = minuto % MinutosPorHora;
            return h.ToString("00", CultureInfo.InvariantCulture) + ":" +
                   m.ToString("00", CultureInfo.InvariantCulture);
        }

        private void Comprobar() {
            if (Config.HoraInicio < 0 || Config.HoraInicio > 23)
                throw new InvalidOperationException($"'horaInicio' vale {Config.HoraInicio}; va de 0 a 23.");
            if (Config.HoraCierre <= Config.HoraInicio)
                throw new InvalidOperationException(
                    $"La jornada cierra a las {Config.HoraCierre} y empieza a las {Config.HoraInicio}: " +
                    "no habria dia que jugar.");
            if (Config.HoraLimite < Config.HoraCierre)
                throw new InvalidOperationException(
                    $"'horaLimite' ({Config.HoraLimite}) es anterior al cierre ({Config.HoraCierre}).");
            if (Config.HoraLimite > 24)
                throw new InvalidOperationException($"'horaLimite' vale {Config.HoraLimite}; el dia acaba a las 24.");
            if (Config.VentanaDeAtencionMinutos < 1)
                throw new InvalidOperationException(
                    $"'ventanaDeAtencionMinutos' vale {Config.VentanaDeAtencionMinutos}: " +
                    "una alerta que expira al instante no es una decision.");
            if (Config.SegundosRealesPorHora <= 0)
                throw new InvalidOperationException(
                    $"'segundosRealesPorHora' vale {Config.SegundosRealesPorHora}; el reloj no correria.");
        }
    }
}
