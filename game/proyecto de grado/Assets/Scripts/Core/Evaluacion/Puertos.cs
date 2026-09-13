using System.Collections.Generic;

namespace Nexus.Core.Evaluacion {
    /// <summary>
    /// Lo que MethodologyReport necesita del nivel para calcular sus metricas (§4.8.3).
    /// Evaluacion no depende de ningun otro paquete: GameSession adapta RuntimeState (C1)
    /// y el LevelProfile a esta vista de solo lectura.
    /// </summary>
    public interface IMetricasDelNivel {
        int DiasConHorasExtra { get; }
        int CambiosAceptados { get; }
        int AccionesRetroElegidas { get; }
        double VolatilidadReal { get; }
        double CoberturaAlCerrarDiseno { get; }
        int VecesExcedioWip { get; }
        IReadOnlyList<double> SerieWip { get; }
        IReadOnlyList<double> SerieLeadTime { get; }
        IReadOnlyList<double> CompromisosPorIteracion { get; }
        IReadOnlyList<double> EntregadoPorIteracion { get; }
    }
}
