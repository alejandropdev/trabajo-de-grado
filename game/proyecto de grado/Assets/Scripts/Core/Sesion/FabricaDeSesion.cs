using System;
using System.Collections.Generic;
using Nexus.Core.Datos;
using Nexus.Core.Guardado;
using Nexus.Core.Narrativa;

namespace Nexus.Core.Sesion {
    /// <summary>
    /// Como se construye una sesion al abrir una partida (§5.7). Implementa el puerto que C9 declaro
    /// antes de que GameSession existiera, y por eso SaveStore.AbrirSesion nunca ha necesitado
    /// conocer esta clase.
    ///
    /// Sin nivel en curso, empieza el nivel por la Fase 1; con un nivel a medias, rehidrata.
    /// </summary>
    public sealed class FabricaDeSesion : IFabricaDeSesion<GameSession> {
        private readonly Catalogo _catalogo;

        public FabricaDeSesion(Catalogo catalogo) {
            _catalogo = catalogo ?? throw new ArgumentNullException(nameof(catalogo));
        }

        public GameSession Nueva(SaveGame partida) {
            if (partida == null) throw new ArgumentNullException(nameof(partida));

            var flags = Flags(partida);
            flags.Inicializar();
            return new GameSession(_catalogo, partida.Partida.NivelActualId, flags, partida.Partida.Semilla);
        }

        public GameSession Restaurar(SaveGame partida, NivelEnCurso nivel) {
            if (partida == null) throw new ArgumentNullException(nameof(partida));
            if (nivel == null) throw new ArgumentNullException(nameof(nivel));

            return GameSession.Restaurar(_catalogo, nivel.PerfilDeNivelId, Flags(partida),
                                         partida.Partida.Semilla, nivel);
        }

        /// <summary>
        /// El FlagStore envuelve el diccionario de la partida, no una copia: asi lo que el motor escriba
        /// ya esta dentro de SaveGame cuando AutoGuardado lo serialice.
        /// </summary>
        private FlagStore Flags(SaveGame partida) {
            if (partida.Flags == null) partida.Flags = new Dictionary<string, double>(StringComparer.Ordinal);
            return new FlagStore(partida.Flags, _catalogo.Flags);
        }
    }
}
