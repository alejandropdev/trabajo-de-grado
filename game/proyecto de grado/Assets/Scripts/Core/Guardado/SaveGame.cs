using System.Collections.Generic;

namespace Nexus.Core.Guardado {
    /// <summary>
    /// Una partida en disco (§4.10.2): tres bloques con tres duraciones.
    /// Partida y Flags viven TODA la partida; Nivel vive UN nivel y es null entre niveles.
    /// Ruta: perfiles/&lt;perfilId&gt;/partidas/&lt;id&gt;.json
    /// </summary>
    public sealed class SaveGame {
        public int VersionEsquema = SaveStore.VersionActual;
        public string Id;
        public string PerfilId;
        public DatosDePartida Partida = new DatosDePartida();

        /// <summary>Los FLG_*. Persisten entre niveles: los proyectos se olvidan, las decisiones no.</summary>
        public Dictionary<string, double> Flags = new Dictionary<string, double>();

        public NivelEnCurso Nivel;
    }

    public sealed class DatosDePartida {
        public string Nombre;
        public int Semilla;
        public bool ModoAula;
        public string FechaCreacion;          // ISO 8601: DateTime.UtcNow.ToString("o")
        public string FechaUltimoGuardado;
        public string NivelActualId;
        public int NivelAndamiaje;
        public double SegundosJugados;
        public List<string> NivelesCompletados = new List<string>();
    }

    /// <summary>Lo que pinta la pantalla de "cargar partida", incluidas las dañadas.</summary>
    public sealed class ResumenDePartida {
        public string Id;
        public string Nombre;
        public string NivelActualId;
        public string FechaUltimoGuardado;
        public bool EntreNiveles;
        /// <summary>True si no se pudo leer. Se muestra "(partida dañada)" y se puede borrar, sin tumbar la lista.</summary>
        public bool Danada;
        public string Error;
    }
}
