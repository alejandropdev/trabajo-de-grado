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

        public double velocidadDeSimulacion = 1.0;
        /// <summary>0 = silencio, 1 = volumen completo.</summary>
        public double volumenGeneral = 1.0;
        public bool pantallaCompleta = true;
        /// <summary>
        /// Tarjetas y frases cortas (true) o los textos completos de siempre (false). Existe para poder comparar las dos
        /// versiones con usuarios y para volver atras si algo falla.
        /// </summary>
        public bool modoTarjetas = true;

        /// <summary>Devuelve los valores dentro de su rango: un JSON editado a mano no puede romper el juego.</summary>
        public Preferencias Acotadas() {
            return new Preferencias {
                velocidadDeSimulacion = Acotar(velocidadDeSimulacion, VelocidadMinima, VelocidadMaxima, 1.0),
                volumenGeneral = Acotar(volumenGeneral, 0, 1, 1.0),
                pantallaCompleta = pantallaCompleta,
                modoTarjetas = modoTarjetas
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
