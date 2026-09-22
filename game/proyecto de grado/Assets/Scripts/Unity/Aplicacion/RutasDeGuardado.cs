using System.IO;
using UnityEngine;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// Donde viven los datos del jugador (§8.3.7):
    ///   persistentDataPath/NexusProtocol/perfiles/&lt;idPerfil&gt;.json
    ///   persistentDataPath/NexusProtocol/perfiles/&lt;idPerfil&gt;/partidas/&lt;idPartida&gt;.json
    ///
    /// Las claves dentro de esa carpeta las deciden ProfileStore y SaveStore (Core); aqui solo se fija la raiz,
    /// que es lo unico que depende de Unity.
    /// </summary>
    public static class RutasDeGuardado {
        public const string Carpeta = "NexusProtocol";

        public static string Raiz { get { return Path.Combine(Application.persistentDataPath, Carpeta); } }

        /// <summary>Donde esta el contenido del juego. En escritorio se lee directamente del disco.</summary>
        public static string Contenido { get { return Application.streamingAssetsPath; } }
    }
}
