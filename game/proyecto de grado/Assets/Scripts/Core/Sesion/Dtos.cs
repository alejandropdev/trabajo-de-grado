using System;
using System.Collections.Generic;
using Nexus.Core.Evaluacion;
using Nexus.Core.Metodologia;
using Nexus.Core.Simulacion;

namespace Nexus.Core.Sesion {
    /// <summary>
    /// Lo que la UI pinta a las 09:00 (§4.9.2). Todo lo que se sabe de hoy, en un objeto.
    /// La UI no consulta el estado: recibe esto y lo dibuja.
    /// </summary>
    public sealed class DayBrief {
        public int Dia;
        public int DiasTotales;

        public string EtiquetaUnidad;      // "Sprint 2", "Etapa: Pruebas", "Flujo"
        public string UnidadId;
        public string TextoDeUnidad;

        public List<string> Ceremonias = new List<string>();

        /// <summary>Los telegrafiados que salen hoy, ya con su canal: "[log] El build tardo 14 min."</summary>
        public List<string> Avisos = new List<string>();

        /// <summary>Lo que se cobro hoy de decisiones viejas. Es P4 hecho texto.</summary>
        public List<string> EfectosQueVencieronHoy = new List<string>();

        public Derived Derivadas;

        /// <summary>burndown | curvaS | cfd. Lo decide la metodologia.</summary>
        public string Tablero;

        public int WipActual;
        public int LimiteWip;
        public double LeadTime;
    }

    /// <summary>La ceremonia de planificacion pide un numero. Es el verbo V5: comprometerse.</summary>
    public sealed class PlanningRequest {
        public string Unidad;

        /// <summary>Lo que el modelo dice que cabe. El jugador puede prometer mas, y ahi empieza el problema.</summary>
        public double CapacidadSugerida;

        public double PuntosPendientes;
        public string Texto;
    }

    /// <summary>La retrospectiva: el unico sitio donde el jugador cambia una constante de su proceso.</summary>
    public sealed class RetroRequest {
        public string CeremoniaId;
        public string Texto;
        public List<AccionRetro> Acciones = new List<AccionRetro>();
    }

    /// <summary>Una opcion de decision, ya masticada para la pantalla.</summary>
    public sealed class OpcionPresentada {
        public string Id;
        public string Texto;

        /// <summary>Lo que cambiaria cada stock. SOLO el efecto inmediato: el diferido no se previsualiza nunca.</summary>
        public Dictionary<string, double> Previsualizacion = new Dictionary<string, double>(StringComparer.Ordinal);

        public bool Bloqueada;
        public string MotivoBloqueo;

        /// <summary>Lo que TU metodologia opina de esta opcion hoy.</summary>
        public string NotaDeMetodologia;
    }

    /// <summary>El evento de las 12:00, listo para pintarse.</summary>
    public sealed class PendingDecision {
        public string EventoId;
        public string Titulo;

        /// <summary>El canal por el que llego el aviso, dias atras. Se repite aqui para dar continuidad.</summary>
        public string Canal;
        public string TextoAviso;

        public int Severidad;
        public string ObjetivoAprendizaje;

        public bool EsCambioDeAlcance;

        /// <summary>Lo que la metodologia dice sobre cambiar el alcance HOY. Null si no es un cambio de alcance.</summary>
        public VeredictoCambio Veredicto;

        public List<OpcionPresentada> Opciones = new List<OpcionPresentada>();
    }

    /// <summary>El resultado del deploy (§4.9.3).</summary>
    public sealed class LaunchResult {
        public bool Exito;
        public double RiesgoDeLanzamiento;
        public double AlcanceEntregado;
        public double AlcanceComprometido;
        public int DefectosEscapados;

        public string Texto;
        public string TextoMetodologia;
    }

    /// <summary>
    /// El Dashboard de Lecciones Aprendidas (§10.3), construido una sola vez dentro de Cerrar().
    /// Las siete secciones de la Fase 4 salen de aqui.
    /// </summary>
    public sealed class DebriefReport {
        public string NivelId;
        public string NivelNombre;
        public int Semilla;

        /// <summary>Como termino ESTE nivel: completado | fallado. Los 14 finales del juego son cosa del N8.</summary>
        public string Final;

        public LaunchResult Lanzamiento;

        public bool CumpleUmbralesDeExito;
        public List<string> UmbralesFallados = new List<string>();

        public MethodologyReport Metodologia;
        public CompetenceProfile Competencia;
        public DecisionTrace Traza;

        public string ArquitecturaElegida;
        public string ArquitecturaVeredicto;
        public string ArquitecturaRazon;

        /// <summary>Las 9 metricas agregadas que las rubricas de cierre consultan por nombre.</summary>
        public Dictionary<string, double> Metricas = new Dictionary<string, double>(StringComparer.Ordinal);

        /// <summary>Lo que se lleva la partida. Copia: el DebriefReport no puede alterar los flags.</summary>
        public Dictionary<string, double> Flags = new Dictionary<string, double>(StringComparer.Ordinal);

        /// <summary>Las 5 decisiones incorrectas mas recientes, en orden inverso hasta la raiz.</summary>
        public List<string> CadenaCausal = new List<string>();

        /// <summary>Beats obligatorios que nunca pudieron salir. Vacio en una partida sana.</summary>
        public List<string> BeatsPerdidos = new List<string>();
    }
}
