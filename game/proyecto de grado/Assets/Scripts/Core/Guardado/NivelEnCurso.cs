using System.Collections.Generic;
using Nexus.Core.Evaluacion;
using Nexus.Core.Eventos;
using Nexus.Core.Modelo;
using Nexus.Core.Simulacion;

namespace Nexus.Core.Guardado {
    /// <summary>
    /// Lo que hace falta para rehidratar un nivel a medias (§4.10.3). Un guardado incompleto no da error:
    /// da una partida sutilmente distinta (INV-7). Por eso cada campo de aqui existe.
    ///
    /// W, R, ColaDeEfectos y Telegrafiados fueron bloques opacos (JToken) hasta que existieron C1 y C4.
    /// Ya estan tipados, y el JSON en disco es el mismo que producian los bloques opacos: el esquema
    /// no cambio, solo dejo de hacer falta escribirlo a mano.
    /// </summary>
    public sealed class NivelEnCurso {
        public string PerfilDeNivelId;

        // --- lo publico de GameSession ---
        public WorldState W;                              // C1
        public RuntimeState R;                            // C1
        public Coeficientes Coef;                         // YA multiplicados: no se reaplican al cargar
        public DecisionTrace Traza;
        public CompetenceProfile Competencia;
        public bool Fase1Cerrada;

        // --- las decisiones de Fase 1, que reconfiguran el motor entero ---
        public string MetodologiaId;
        public string RazonMetodologia;
        public string ArquitecturaId;
        public string RazonArquitectura;

        /// <summary>Hay que REAPLICAR los pesos por tag al cargar: el reparto escribio en el LevelProfile, que se recarga.</summary>
        public Dictionary<string, int> FichasDeCalidad = new Dictionary<string, int>();

        // --- lo privado de GameSession que no se puede recalcular ---
        public double VelocidadBaseMult = 1.0;
        public double ThroughputAcumulado;
        public double AvanceAlEmpezarUnidad;
        public string UnidadAnterior;

        // --- la agenda del tiempo: SIN ESTO el jugador esquiva consecuencias recargando ---
        public List<EfectoEnCola> ColaDeEfectos = new List<EfectoEnCola>();              // C4
        public List<TelegrafiadoPendiente> Telegrafiados = new List<TelegrafiadoPendiente>();   // C4

        // --- el estado del azar: se queman estas tiradas al restaurar DeterministicRng ---
        public int ConsumosDelRng;
    }
}
