using System;
using System.Collections.Generic;

namespace Nexus.Core.Metodologia {
    /// <summary>
    /// C5 · Lo que la metodologia dice de UN dia concreto (§4.5.3). Es un DTO: lo produce MethodologyRules
    /// y lo consumen GameSession y el EventDirector. No tiene logica.
    ///
    /// Es la pieza que hace que el dia 7 de Scrum y el dia 7 de Cascada no se parezcan en nada,
    /// sin que nadie escriba un if con el nombre de la metodologia dentro.
    /// </summary>
    public sealed class DayPlan {
        public int Dia;

        /// <summary>"Sprint", "Etapa", "Semana"... lo que se pinta en la cabecera.</summary>
        public string EtiquetaUnidad;

        public string UnidadId;

        /// <summary>0 para la primera unidad. Indexa CompromisosPorIteracion y EntregadoPorIteracion.</summary>
        public int IndiceDeUnidad;

        /// <summary>1 el primer dia de la unidad.</summary>
        public int DiaDentroDeUnidad;

        public bool EsPrimerDiaDeUnidad;
        public bool EsUltimoDiaDeUnidad;

        /// <summary>Las que tocan hoy, en el orden del perfil. Puede estar vacia.</summary>
        public List<Ceremonia> Ceremonias = new List<Ceremonia>();

        /// <summary>
        /// El enfoque de la etapa actual, que se multiplica sobre los pesosPorTag del nivel.
        /// En Cascada cambia entre Analisis y Pruebas; en Scrum es el mismo todo el nivel.
        /// </summary>
        public Dictionary<string, double> MultiplicadorTagsDeEtapa =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Lo que la etapa cobra cada dia, pase lo que pase. Lo aplica GameSession al empezar el dia.</summary>
        public Dictionary<string, object> EfectosDeEtapa =
            new Dictionary<string, object>(StringComparer.Ordinal);

        public string TextoDeUnidad;
    }

    /// <summary>
    /// C5 · Que pasa si hoy el cliente quiere cambiar el alcance (§4.5.3).
    ///
    /// Las tres metodologias contestan distinto a la misma pregunta, y esa diferencia es el nucleo
    /// pedagogico del bloque: Kanban dice "vale, pero algo sale del tablero"; Scrum dice "al sprint
    /// que viene"; Cascada dice "fuera de un hito, no, y si insistes te va a costar".
    /// </summary>
    public sealed class VeredictoCambio {
        public bool Permitido;

        /// <summary>True si hoy la metodologia tiene abierta su ventana de cambio.</summary>
        public bool EnVentana;

        /// <summary>Multiplica el coste del cambio. Fuera de ventana se acumula la penalizacion.</summary>
        public double CosteMultiplicador = 1.0;

        /// <summary>Kanban: aceptar algo nuevo obliga a sacar algo del tablero.</summary>
        public bool ConsumeWip;

        /// <summary>Lo que ademas se cobra por hacerlo fuera de ventana.</summary>
        public Dictionary<string, object> EfectosExtra =
            new Dictionary<string, object>(StringComparer.Ordinal);

        /// <summary>Lo que se le dice al jugador, con la voz de su metodologia.</summary>
        public string Texto;
    }
}
