using System;
using System.Collections.Generic;

namespace Nexus.Core.Metodologia {
    /// <summary>Lo que se puede abrir en Monitoreo. Cada metodologia enseña las suyas: la que no esta, es que no existe en ella.</summary>
    public static class VistasDeMonitoreo {
        public const string Tablero = "tablero";
        /// <summary>El product backlog y el sprint backlog (Scrum).</summary>
        public const string Backlog = "backlog";
        public const string Burndown = "burndown";
        /// <summary>El diagrama de flujo acumulado, con lead time y throughput (Kanban).</summary>
        public const string Cfd = "cfd";
        public const string CurvaS = "curvaS";
        /// <summary>SRS, SAD, SDD, PTP, PMP con su completitud (Cascada).</summary>
        public const string Documentos = "documentos";
        public const string Pruebas = "pruebas";

        private static readonly string[] _todas = { Tablero, Backlog, Burndown, Cfd, CurvaS, Documentos, Pruebas };
        public static IReadOnlyList<string> Todas { get { return _todas; } }

        public static bool EsValida(string vista) { return Array.IndexOf(_todas, vista) >= 0; }
    }

    /// <summary>
    /// 9 · Como es el tablero del equipo con esta metodologia, y que se puede (y que NO se puede) hacer con el. Es lo
    /// que hace que elegir Scrum, Kanban o Cascada se note cada dia y no solo en unos coeficientes:
    ///
    ///   Scrum    el sprint es cerrado: se trabaja lo comprometido en la planificacion. Meter algo a mitad cuesta.
    ///            No hay limite de WIP por columna ni documentos que firmar. Se mira el burndown.
    ///   Kanban   no hay sprints ni compromiso: hay un limite de trabajo en curso que el jugador sube o baja, y se
    ///            mira el flujo acumulado. Los cambios entran cuando hay hueco.
    ///   Cascada  el trabajo pasa por etapas con su documento; las pruebas solo existen en su etapa. Se mira la
    ///            curva S contra el plan. No hay reunion diaria ni retrospectiva.
    ///
    /// Sin este bloque, una metodologia nueva recibe la politica por defecto de su tipo de calendario.
    /// </summary>
    public sealed class PoliticaDeTablero {
        /// <summary>Como se llama cada columna de base (porHacer, haciendo, revision, hecho) con esta metodologia.</summary>
        public Dictionary<string, string> NombresDeColumna = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Scrum: solo entra al sprint lo comprometido; añadir a mitad de sprint tiene coste.</summary>
        public bool SprintCerrado;

        /// <summary>Kanban: hay un limite de tarjetas en curso, y el jugador lo puede mover.</summary>
        public bool LimiteDeWip;

        /// <summary>Cascada: las etapas en las que se puede probar. Vacia = se prueba cuando se quiera.</summary>
        public List<string> PruebasSoloEnEtapas = new List<string>();

        /// <summary>Cascada: fuera de la etapa de pruebas, lo que se puede hacer es inspeccionar los documentos.</summary>
        public bool InspeccionDeDocumentos;

        /// <summary>Lo que Monitoreo enseña (VistasDeMonitoreo), en este orden.</summary>
        public List<string> Vistas = new List<string>();

        /// <summary>Lo que esta metodologia NO tiene, dicho al jugador: «En Kanban no hay sprints ni burndown: …».</summary>
        public string LoQueNoHay;

        public string NombreDe(string columna, string porDefecto) {
            string n;
            return NombresDeColumna != null && NombresDeColumna.TryGetValue(columna ?? "", out n) && !string.IsNullOrEmpty(n) ? n : porDefecto;
        }

        public bool Tiene(string vista) { return Vistas != null && Vistas.Contains(vista); }

        public PoliticaDeTablero Clone() {
            var c = (PoliticaDeTablero)MemberwiseClone();
            c.NombresDeColumna = NombresDeColumna == null ? new Dictionary<string, string>(StringComparer.Ordinal)
                                                         : new Dictionary<string, string>(NombresDeColumna, StringComparer.Ordinal);
            c.PruebasSoloEnEtapas = PruebasSoloEnEtapas == null ? new List<string>() : new List<string>(PruebasSoloEnEtapas);
            c.Vistas = Vistas == null ? new List<string>() : new List<string>(Vistas);
            return c;
        }

        /// <summary>La de una metodologia que no trae bloque «tablero»: sale de como trocea el nivel.</summary>
        public static PoliticaDeTablero PorDefecto(string tipoDeCalendario) {
            var p = new PoliticaDeTablero();
            switch ((tipoDeCalendario ?? "").Trim().ToLowerInvariant()) {
                case "continuo":
                    p.LimiteDeWip = true;
                    p.Vistas = new List<string> { VistasDeMonitoreo.Tablero, VistasDeMonitoreo.Cfd, VistasDeMonitoreo.Pruebas };
                    break;
                case "secuencial":
                    p.InspeccionDeDocumentos = true;
                    p.Vistas = new List<string> { VistasDeMonitoreo.Tablero, VistasDeMonitoreo.CurvaS, VistasDeMonitoreo.Documentos, VistasDeMonitoreo.Pruebas };
                    break;
                default:
                    p.SprintCerrado = true;
                    p.Vistas = new List<string> { VistasDeMonitoreo.Tablero, VistasDeMonitoreo.Backlog, VistasDeMonitoreo.Burndown, VistasDeMonitoreo.Pruebas };
                    break;
            }
            return p;
        }
    }

    /// <summary>
    /// Lo que el jugador puede hacer en una ceremonia a la que asiste. Cada opcion es una forma de llevarla: la buena
    /// practica y las tentaciones de siempre (convertir la reunion diaria en un parte al jefe, maquillar el informe).
    /// </summary>
    public sealed class OpcionDeCeremonia {
        public string Id;
        public string Texto;
        /// <summary>Se aplican al elegirla. Valores double o "+10%".</summary>
        public Dictionary<string, object> Efectos = new Dictionary<string, object>(StringComparer.Ordinal);
        /// <summary>Lo que suma (o resta) hoy al ritmo del equipo: 0,04 = un 4 % mas de avance ese dia.</summary>
        public double BonoDeFlujo;
        /// <summary>Lo que queda escrito en el acta: que paso por llevarla asi, y por que.</summary>
        public string Acta;

        public OpcionDeCeremonia Clone() {
            var c = (OpcionDeCeremonia)MemberwiseClone();
            c.Efectos = Efectos == null ? new Dictionary<string, object>(StringComparer.Ordinal)
                                        : new Dictionary<string, object>(Efectos, StringComparer.Ordinal);
            return c;
        }
    }
}
