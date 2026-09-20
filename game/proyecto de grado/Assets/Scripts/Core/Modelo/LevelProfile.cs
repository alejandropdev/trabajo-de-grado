using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Nexus.Core.Simulacion;

namespace Nexus.Core.Modelo {
    /// <summary>
    /// C1 · El caracter de un nivel en datos (§4.2.3). Espejo en C# de niveles/nivel-XX.json.
    /// Aqui no hay ni una regla de juego: cambiar estos numeros cambia por completo como se siente
    /// un nivel SIN recompilar, y esa es justo la regla de balanceo del §10.4 —
    /// "las funciones del modelo son contenido y no se tocan; lo que se ajusta son estos coeficientes".
    ///
    /// GameSession trabaja SIEMPRE sobre un Clone(): el reparto de fichas de calidad de la Fase 1
    /// reescribe Director.PesosPorTag, y el catalogo se carga una sola vez al arrancar. Sin el clon,
    /// jugar el mismo nivel dos veces acumularia los pesos de la partida anterior.
    /// </summary>
    public sealed class LevelProfile {
        // --- Identidad ---
        public string Id;
        public string Nombre;

        /// <summary>
        /// Las lineas del briefing, que se revelan de una en una. La respuesta correcta de la arquitectura
        /// esta AQUI, no en el catalogo de patrones: el nivel se gana leyendo, no memorizando.
        /// </summary>
        public string[] Briefing = new string[0];

        // --- Grupo 1 · condiciones iniciales ---
        public int DiasTotales;
        public double PresupuestoInicial;
        public double AlcanceInicial;
        public int EquipoInicial;
        public double DeudaHeredada;
        public double CoberturaHeredada;
        public double DocumentacionHeredada;

        // --- Grupo 2 · el modelo continuo ---
        /// <summary>Puntos por dia con el equipo en condiciones neutras. Lo multiplica todo lo demas.</summary>
        public double VelocidadBase = 3.0;

        public Coeficientes Coef = new Coeficientes();

        // --- Grupo 3 · el director de eventos ---
        public DirectorConfig Director = new DirectorConfig();

        // --- Grupo 4 · pedagogia ---
        /// <summary>OA-* que este nivel evalua. Filtra tanto eventos como minijuegos.</summary>
        public string[] ObjetivosActivos = new string[0];

        public Umbrales Umbrales = new Umbrales();
        public string[] MetodologiasPermitidas = new string[0];

        /// <summary>0 sin ayudas · 1 sin contadores · 2 normal · 3 tutorial (zonas resaltadas).</summary>
        public int NivelAndamiaje = 2;

        /// <summary>
        /// 0-100, OCULTO al jugador. En la Fase 4 se compara contra la familia de la metodologia que eligio:
        /// agil acierta si la volatilidad real era alta, tradicional si era baja. Es la unica forma de
        /// evaluar "elegiste bien" sin preguntarselo.
        /// </summary>
        public double VolatilidadReal;

        public Fase1Config Fase1 = new Fase1Config();

        /// <summary>Copia profunda. Todo lo mutable (arrays, diccionarios, subobjetos) se duplica.</summary>
        public LevelProfile Clone() {
            var c = (LevelProfile)MemberwiseClone();
            c.Briefing = Briefing == null ? null : (string[])Briefing.Clone();
            c.ObjetivosActivos = ObjetivosActivos == null ? null : (string[])ObjetivosActivos.Clone();
            c.MetodologiasPermitidas = MetodologiasPermitidas == null ? null : (string[])MetodologiasPermitidas.Clone();
            c.Coef = Coef == null ? null : Coef.Clone();
            c.Director = Director == null ? null : Director.Clone();
            c.Umbrales = Umbrales == null ? null : Umbrales.Clone();
            c.Fase1 = Fase1 == null ? null : Fase1.Clone();
            return c;
        }
    }

    /// <summary>Grupo 3 · lo que gobierna al EventDirector en este nivel (§4.2.3).</summary>
    public sealed class DirectorConfig {
        /// <summary>
        /// Cuantos eventos puede agendar el director en cada fase: [planificacion, desarrollo, lanzamiento, evaluacion].
        /// Es el presupuesto de drama: limita el caos para que un nivel no se convierta en una loteria.
        /// </summary>
        public int[] PresupuestoDrama = { 1, 3, 2, 1 };

        /// <summary>
        /// tag -> multiplicador de peso. Es el enfoque del nivel, y el reparto de fichas de calidad de la
        /// Fase 1 lo REESCRIBE: no invertir en un atributo sube el peso de su tag. El jugador configura
        /// que crisis va a sufrir, y no lo sabe.
        /// </summary>
        public Dictionary<string, double> PesosPorTag = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Dias minimos entre dos eventos cualesquiera.</summary>
        public int EnfriamientoGlobal = 2;

        public int MaxEventosPorDia = 1;

        /// <summary>Dias de antelacion con que se avisa por el canal diegetico. Sin aviso no hay evento (INV-3).</summary>
        public int VentanaTelegrafiado = 2;

        public double MultiplicadorSeveridad = 1.0;

        /// <summary>Semilla propia del nivel. 0 = la usa la de la partida.</summary>
        public int Semilla;

        public DirectorConfig Clone() {
            var c = (DirectorConfig)MemberwiseClone();
            c.PresupuestoDrama = PresupuestoDrama == null ? null : (int[])PresupuestoDrama.Clone();
            c.PesosPorTag = PesosPorTag == null
                ? null
                : new Dictionary<string, double>(PesosPorTag, StringComparer.OrdinalIgnoreCase);
            return c;
        }

        [OnDeserialized]
        internal void TrasCargar(StreamingContext _) {
            PesosPorTag = Diccionarios.SinMayusculas(PesosPorTag);
        }
    }

    /// <summary>Grupo 4 · cuando el nivel se considera ganado y cuando se considera perdido.</summary>
    public sealed class Umbrales {
        public UmbralExito Exito = new UmbralExito();
        public UmbralFallo Fallo = new UmbralFallo();

        public Umbrales Clone() {
            return new Umbrales {
                Exito = Exito == null ? null : Exito.Clone(),
                Fallo = Fallo == null ? null : Fallo.Clone()
            };
        }
    }

    public sealed class UmbralExito {
        public double AvanceMinimo;
        public double CoberturaMinima;
        public double DeudaMaxima = 100;

        public UmbralExito Clone() { return (UmbralExito)MemberwiseClone(); }
    }

    /// <summary>
    /// Nulo = ese umbral no existe en este nivel. Se usa null y no un numero centinela porque
    /// "sin umbral de dinero" y "umbral de dinero en el minimo representable" no son lo mismo al depurar.
    /// </summary>
    public sealed class UmbralFallo {
        /// <summary>Fallo si DeudaTecnica &gt;= este valor.</summary>
        public double? Deuda;

        /// <summary>Fallo si MoralEquipo &lt;= este valor.</summary>
        public double? Moral;

        /// <summary>Fallo si Dinero &lt;= este valor.</summary>
        public double? Dinero;

        public UmbralFallo Clone() { return (UmbralFallo)MemberwiseClone(); }
    }
}
