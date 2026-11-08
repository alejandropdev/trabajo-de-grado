using System;
using Newtonsoft.Json;

namespace Nexus.Core {
    /// <summary>
    /// Las opciones del juego que no son de un perfil ni de una partida: valen para el equipo en el que se juega
    /// (la velocidad de la simulacion con la que el docente ajusta la sesion, el volumen y el modo de ventana).
    /// </summary>
    public sealed class Preferencias {
        public const double VelocidadMinima = 0.5;
        public const double VelocidadMaxima = 4.0;
        public const double SensibilidadMinima = 0.3;
        public const double SensibilidadMaxima = 2.0;

        public const string Facil = "facil";
        public const string Normal = "normal";
        public const string Dificil = "dificil";

        public double velocidadDeSimulacion = 1.0;
        /// <summary>0 = silencio, 1 = volumen completo.</summary>
        public double volumenGeneral = 1.0;
        public bool pantallaCompleta = true;
        /// <summary>
        /// Tarjetas y frases cortas (true) o los textos completos de siempre (false). Existe para poder comparar las dos
        /// versiones con usuarios y para volver atras si algo falla.
        /// </summary>
        public bool modoTarjetas = true;
        /// <summary>Cuanto reloj dan los minijuegos: «facil», «normal» o «dificil».</summary>
        public string dificultad = Normal;
        /// <summary>La musica de fondo, aparte del volumen general. 0..1.</summary>
        public double volumenMusica = 0.7;
        /// <summary>El raton al mirar en el recorrido 3D. 1 = la de fabrica.</summary>
        public double sensibilidadRaton = 1.0;
        /// <summary>Mover el raton hacia arriba mira hacia abajo (como en un avion).</summary>
        public bool invertirY;

        /// <summary>Devuelve los valores dentro de su rango: un JSON editado a mano no puede romper el juego.</summary>
        public Preferencias Acotadas() {
            return new Preferencias {
                velocidadDeSimulacion = Acotar(velocidadDeSimulacion, VelocidadMinima, VelocidadMaxima, 1.0),
                volumenGeneral = Acotar(volumenGeneral, 0, 1, 1.0),
                pantallaCompleta = pantallaCompleta,
                modoTarjetas = modoTarjetas,
                dificultad = dificultad == Facil || dificultad == Dificil ? dificultad : Normal,
                volumenMusica = Acotar(volumenMusica, 0, 1, 0.7),
                sensibilidadRaton = Acotar(sensibilidadRaton, SensibilidadMinima, SensibilidadMaxima, 1.0),
                invertirY = invertirY
            };
        }

        private static double Acotar(double v, double min, double max, double siNoEsNumero) {
            if (double.IsNaN(v) || double.IsInfinity(v)) return siNoEsNumero;
            return Math.Max(min, Math.Min(max, v));
        }
    }

    /// <summary>Guarda y carga las Preferencias en un unico archivo JSON del almacen.</summary>
    public sealed class PreferenciasStore {
        public const string Clave = "preferencias.json";
        private readonly IAlmacen _almacen;

        public PreferenciasStore(IAlmacen almacen) {
            _almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
        }

        /// <summary>Las guardadas, o las de por defecto si no hay archivo o esta roto.</summary>
        public Preferencias Cargar() {
            if (!_almacen.Existe(Clave)) return new Preferencias();
            try {
                var p = JsonConvert.DeserializeObject<Preferencias>(_almacen.Cargar(Clave));
                return (p ?? new Preferencias()).Acotadas();
            } catch (JsonException) {
                return new Preferencias();
            }
        }

        public void Guardar(Preferencias preferencias) {
            if (preferencias == null) throw new ArgumentNullException(nameof(preferencias));
            _almacen.Guardar(Clave, JsonConvert.SerializeObject(preferencias.Acotadas(), Formatting.Indented));
        }
    }
}
