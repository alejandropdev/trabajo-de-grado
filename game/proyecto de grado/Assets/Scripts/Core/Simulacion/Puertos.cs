namespace Nexus.Core.Simulacion {
    /// <summary>
    /// Lo que ForresterModel necesita del estado del proyecto: leer y escribir un stock por nombre.
    /// WorldState (C1) ya declara exactamente estos dos metodos en la especificacion (§4.2.1),
    /// asi que solo tiene que añadir ": IEstadoSimulable".
    /// Contrato: Set() aplica la acotacion de §4.2.1 (9 stocks a 0-100, Dias/Alcance/Avance >= 0,
    /// VelocidadMod >= 0.1, Dinero sin acotar). Es el paso 7 de AvanzarUnDia.
    /// </summary>
    public interface IEstadoSimulable {
        bool TryGet(string nombre, out double valor);
        void Set(string nombre, double valor);
    }

    /// <summary>
    /// Vista de solo lectura de RuntimeState (C1). ForresterModel NUNCA escribe contadores:
    /// GameSession los actualiza antes de llamarlo (§5.3, TerminarDia).
    /// </summary>
    public interface IContadoresDeSimulacion {
        int DiasSeguidosTrabajando { get; }
        double SobreCompromiso { get; }
        int WipActual { get; }
    }
}
