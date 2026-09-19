using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Nexus.Core.Evaluacion;
using Nexus.Core.Simulacion;

namespace Nexus.Core.Guardado {
    /// <summary>
    /// Lo que hace falta para rehidratar un nivel a medias (§4.10.3). Un guardado incompleto no da error:
    /// da una partida sutilmente distinta (INV-7). Por eso cada campo de aqui existe.
    ///
    /// W, R, ColaDeEfectos y Telegrafiados son bloques opacos (JToken) mientras C1 y C4 no existan.
    /// Se escriben con JsonDeGuardado.ABloque(), asi que el JSON en disco es identico al que producira
    /// la clase tipada: cambiar JToken por WorldState/RuntimeState/List&lt;EfectoEnCola&gt; no cambia el esquema.
    /// </summary>
    public sealed class NivelEnCurso {
        public string PerfilDeNivelId;

        // --- lo publico de GameSession ---
        public JToken W;                                  // TODO(C1): WorldState
        public JToken R;                                  // TODO(C1): RuntimeState
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
        public JArray ColaDeEfectos = new JArray();       // TODO(C4): List<EfectoEnCola>
        public JArray Telegrafiados = new JArray();       // TODO(C4): List<TelegrafiadoPendiente>

        // --- el estado del azar: se queman estas tiradas al restaurar DeterministicRng ---
        public int ConsumosDelRng;
    }
}
