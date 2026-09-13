namespace Nexus.Core.Simulacion {
    /// <summary>
    /// Las 5 magnitudes calculadas (§4.3.1). NO se guardan: se recalculan cada tick.
    /// Asi el guardado pesa menos y el modelo y el estado nunca se desincronizan.
    /// </summary>
    public sealed class Derived {
        /// <summary>Puntos por dia: V_base · mult · VelocidadMod · fComp · fMoral · fFatiga · fDeuda.</summary>
        public double Velocidad;

        /// <summary>0-100. Lo que se enseña al jugador y lo que alimenta los pesos del director.</summary>
        public double RiesgoLatente;

        /// <summary>(k1·Cobertura + k2·Documentacion) · fDeuda · fMoral.</summary>
        public double CalidadEntregada;

        /// <summary>Ley de Little: WipActual / max(0.1, Velocidad/6). Enseña Kanban.</summary>
        public double LeadTime;

        /// <summary>Alcance − Avance de hoy. La serie por dia la guarda RuntimeState.</summary>
        public double Burndown;

        // Desglose del riesgo: las 4 barras del dashboard.
        public double RiesgoPorCansancio;
        public double RiesgoPorDeuda;
        public double RiesgoPorCobertura;
        public double RiesgoPorDocumentacion;
    }
}
