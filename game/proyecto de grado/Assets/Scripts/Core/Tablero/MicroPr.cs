using System.Collections.Generic;
using Nexus.Core.Minijuegos;

namespace Nexus.Core.Tablero {
    /// <summary>
    /// Un cambio de codigo pequeño que un compañero deja en «Revisión»: tres a seis lineas, con o sin un defecto
    /// dentro. El jugador lo aprueba o pide cambios. Es contenido (tablero/micro-pr.json), no logica.
    /// </summary>
    public sealed class MicroPr {
        public string Id;
        public string Titulo;
        /// <summary>Lo que el autor dice que hace el cambio.</summary>
        public string Descripcion;
        public List<LineaDiff> Lineas = new List<LineaDiff>();
        public bool TieneDefecto;
        /// <summary>Lo que habia que ver (o por que estaba bien). Se enseña despues de decidir.</summary>
        public string Explicacion;
        /// <summary>En que niveles sale (es codigo de ese proyecto: visitas, reservas, accesos…). Vacio = en todos.</summary>
        public List<string> Niveles = new List<string>();
    }

    /// <summary>
    /// Un trozo del documento de una etapa (el SRS, el SAD…) que se lee al inspeccionarlo: tres o cuatro renglones,
    /// con un problema dentro o sin el. Se firma o se piden cambios. Contenido: tablero/micro-doc.json.
    /// </summary>
    public sealed class ExtractoDeDocumento {
        public string Id;
        /// <summary>El documento del que sale: «SRS», «SAD», «SDD», «PTP».</summary>
        public string Documento;
        public string Titulo;
        public List<string> Secciones = new List<string>();
        public bool TieneDefecto;
        public string Explicacion;
    }

    public static class VeredictosDePr {
        public const string Aprobar = "aprobar";
        public const string PedirCambios = "cambios";
    }

    /// <summary>Lo que paso al revisar: si se acerto, lo que cambio y la explicacion.</summary>
    public sealed class ResultadoDeRevision {
        public bool Acierto;
        public string Titulo;
        public string Explicacion;
        public Dictionary<string, double> Cambios = new Dictionary<string, double>();
    }
}
