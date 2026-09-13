using System.Collections.Generic;

namespace Nexus.Core.Guardado {
    /// <summary>
    /// Lo que la persistencia necesita de una sesion para guardarla. Lo implementara GameSession
    /// (Capturar() ya esta en su API publica, §4.9.1): C9 no conoce la sesion, solo este puerto.
    /// </summary>
    public interface ISesionPersistible {
        /// <summary>Id del LevelProfile que se esta jugando.</summary>
        string NivelId { get; }

        /// <summary>True tras Cerrar(): el WorldState se tira y la partida queda entre niveles.</summary>
        bool NivelTerminado { get; }

        NivelEnCurso Capturar();

        /// <summary>Los FLG_* de la partida (FlagStore, C7).</summary>
        Dictionary<string, double> CapturarFlags();
    }

    /// <summary>
    /// Como se construye la sesion al abrir una partida (§5.7). La implementara quien conoce
    /// los catalogos: Nueva = new GameSession(...) y se juega la Fase 1;
    /// Restaurar = ReaplicarPesosDeCalidad + GameSession.Restaurar(...), en el orden de §7.6.
    /// </summary>
    public interface IFabricaDeSesion<TSesion> {
        TSesion Nueva(SaveGame partida);
        TSesion Restaurar(SaveGame partida, NivelEnCurso nivel);
    }
}
