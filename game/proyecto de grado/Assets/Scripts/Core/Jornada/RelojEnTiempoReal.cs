using System;

namespace Nexus.Core.Jornada {
    /// <summary>
    /// Convierte segundos de pantalla en minutos de juego. Es la unica pieza del dia continuo que depende del
    /// tiempo real, y vive aqui (no en la capa Unity) para poder probarse: la capa Unity solo le pasa
    /// Time.deltaTime cada fotograma y hace avanzar la sesion los minutos enteros que este le devuelve.
    ///
    /// Acumula la fraccion: a 20 s por hora, un fotograma de 1/60 s son 0,05 minutos de juego. Si se
    /// redondeara cada fotograma, el reloj no avanzaria nunca; si se truncara, perderia tiempo sin que nadie
    /// lo notara.
    /// </summary>
    public sealed class RelojEnTiempoReal {
        public const double VelocidadMinima = 0.25;
        public const double VelocidadMaxima = 4.0;

        private readonly double _segundosRealesPorHora;
        private double _minutosAcumulados;
        private double _velocidad = 1.0;

        public RelojEnTiempoReal(JornadaConfig jornada) {
            if (jornada == null) throw new ArgumentNullException(nameof(jornada));
            if (jornada.SegundosRealesPorHora <= 0)
                throw new ArgumentException("SegundosRealesPorHora tiene que ser positivo.", nameof(jornada));
            _segundosRealesPorHora = jornada.SegundosRealesPorHora;
        }

        /// <summary>
        /// Multiplicador de la simulacion (la variable «velocidadSimulacion» del inventario). El docente puede
        /// comprimir una sesion de laboratorio sin tocar el contenido.
        /// </summary>
        public double Velocidad {
            get { return _velocidad; }
            set { _velocidad = Math.Max(VelocidadMinima, Math.Min(VelocidadMaxima, value)); }
        }

        /// <summary>
        /// Con el reloj en pausa no pasa nada. Se pausa DENTRO de una escena ya atendida, nunca mientras el
        /// jugador decide si ir a atender una alerta: si el mundo se congelara, esa decision no costaria nada.
        /// </summary>
        public bool Pausado { get; set; }

        /// <summary>Lo que falta para completar el siguiente minuto. Solo para depurar y para los tests.</summary>
        public double FraccionPendiente { get { return _minutosAcumulados; } }

        /// <summary>Recibe los segundos reales de este fotograma y devuelve los minutos ENTEROS que avanzan.</summary>
        public int Tick(double segundosReales) {
            if (Pausado || segundosReales <= 0 || double.IsNaN(segundosReales)) return 0;

            _minutosAcumulados += segundosReales * _velocidad * 60.0 / _segundosRealesPorHora;
            var enteros = (int)Math.Floor(_minutosAcumulados);
            _minutosAcumulados -= enteros;
            return enteros;
        }

        /// <summary>Al empezar un dia nuevo, la fraccion del anterior no tiene que regalar un minuto.</summary>
        public void Reiniciar() {
            _minutosAcumulados = 0;
        }
    }
}
