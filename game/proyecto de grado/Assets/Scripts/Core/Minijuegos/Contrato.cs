using System.Collections.Generic;

namespace Nexus.Core.Minijuegos
{
    /// <summary>
    /// Todo lo que la escena necesita saber del mundo. Lo construye QUIEN ABRE
    /// la escena (el EventDirector en M8, el banco de pruebas hoy), nunca la escena.
    /// Es de solo lectura: un minijuego no toca WorldState.
    /// </summary>
    public sealed class MinijuegoContext
    {
        public int NivelAndamiaje = 1;                    // 0..3, viene del LevelProfile
        public IReadOnlyDictionary<string, float> WorldState;
        public IReadOnlyDictionary<string, int> PoblacionDefectos;
        public string QuienEspera;                        // sobreescribe el del JSON si no es null
        public int Semilla;                               // para DeterministicRng

        public static MinijuegoContext Basico(int andamiaje)
        {
            return new MinijuegoContext
            {
                NivelAndamiaje = andamiaje,
                WorldState = new Dictionary<string, float>(),
                PoblacionDefectos = new Dictionary<string, int>(),
                Semilla = 4417
            };
        }
    }

    /// <summary>
    /// El contrato con el motor, identico para los 37 minijuegos.
    /// La escena NO aplica nada: devuelve esto y el motor lo aplica.
    /// </summary>
    public sealed class ResultadoMinijuego
    {
        public string MinijuegoId;
        public string Resultado;                          // todos | parcial | falsoPositivo | omitido
        public Rubrica Rubrica = new Rubrica();
        public Dictionary<string, float> EfectosInmediatos = new Dictionary<string, float>();
        public List<EfectoDiferido> EfectosDiferidos = new List<EfectoDiferido>();

        /// <summary>
        /// Los minijuegos NO escriben FLG_*. Devuelven hallazgos y el canal
        /// narrativo decide, al cerrar la fase, que flag escribe cada uno.
        /// Es lo que mantiene testeable el arbol de finales.
        /// </summary>
        public List<string> Hallazgos = new List<string>();

        /// <summary>Lo que se le ensena al jugador al cerrar, sin puntuacion.</summary>
        public List<string> Detalle = new List<string>();

        /// <summary>Para trazaDecisiones y el post-mortem de 5 porques.</summary>
        public List<MarcaRegistrada> Traza = new List<MarcaRegistrada>();

        public string TextoCierre;
    }

    public sealed class MarcaRegistrada
    {
        public List<string> Commits = new List<string>();
        public string Etiqueta;
        public string ZonaAcertada;      // null si no acerto ninguna
        public string SenueloTocado;     // null si no toco ninguno
        public int SegundoDeLaPartida;
    }
}
