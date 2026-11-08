using System;
using System.Collections.Generic;

namespace Nexus.Core.Jornada {
    /// <summary>
    /// Los minijuegos de zona de la Fase 2 (2026-11-06): cosas pequeñas que solo se pueden hacer en un sitio del mapa,
    /// una vez al dia cada una. Hacen que ir a la cafeteria o al Data Hub sea algo mas que leer una ficha.
    ///
    ///   cafe      Cafeteria · llenar la taza hasta la linea (soltar a tiempo). Bien: el equipo descansa un poco.
    ///   logs      Data Hub · cazar la linea roja entre las de log antes de que se vaya. Bien: sube la cobertura.
    ///   rumores   Bullpen · pasar el raton por las mesas y escuchar. Una frase adelanta el proximo aviso.
    ///   tablon    Pasillo (N0) · voltear las notas del corcho: pistas de coleccionables por aparecer.
    ///
    /// Aqui solo las reglas (que hay, cuanto cuesta, cuando se acierta); la sesion aplica el efecto y cobra los minutos.
    /// </summary>
    public static class TiposDeMinijuegoDeZona {
        public const string Cafe = "cafe";
        public const string Logs = "logs";
        public const string Rumores = "rumores";
        public const string Tablon = "tablon";

        private static readonly string[] _todos = { Cafe, Logs, Rumores, Tablon };
        public static IReadOnlyList<string> Todos { get { return _todos; } }
        public static bool EsValido(string tipo) { return Array.IndexOf(_todos, tipo) >= 0; }
    }

    public static class MinijuegosDeZona {
        /// <summary>Lo que cuesta en minutos de la jornada.</summary>
        public static int Minutos(string tipo) {
            switch (tipo) {
                case TiposDeMinijuegoDeZona.Cafe: return 10;
                case TiposDeMinijuegoDeZona.Logs: return 15;
                case TiposDeMinijuegoDeZona.Rumores: return 5;
                default: return 0;
            }
        }

        /// <summary>El nombre corto, para el boton de la escena.</summary>
        public static string Nombre(string tipo) {
            switch (tipo) {
                case TiposDeMinijuegoDeZona.Cafe: return "Llenar la taza";
                case TiposDeMinijuegoDeZona.Logs: return "Cazar el error";
                case TiposDeMinijuegoDeZona.Rumores: return "Escuchar rumores";
                case TiposDeMinijuegoDeZona.Tablon: return "Mirar el tablón";
                default: return tipo;
            }
        }

        // ---- la taza

        /// <summary>Lo que sube la taza por segundo apretado (fraccion de la taza). En ~2,4 s esta en la linea.</summary>
        public const float VelocidadDeLaTaza = 0.33f;
        /// <summary>Donde esta la linea (fraccion de la taza) y cuanto se puede pasar o quedar corto.</summary>
        public const float LineaDeLaTaza = 0.78f, ToleranciaDeLaTaza = 0.07f;

        public static float NivelDeLaTaza(float segundosApretado) {
            return Math.Max(0f, Math.Min(1.15f, segundosApretado * VelocidadDeLaTaza));
        }

        public static bool TazaBienLlena(float nivel) {
            return Math.Abs(nivel - LineaDeLaTaza) <= ToleranciaDeLaTaza;
        }

        // ---- los logs

        /// <summary>Cuanto dura la caza y cuantas lineas pasan.</summary>
        public const float SegundosDeLogs = 10f;
        public const int LineasDeLog = 26;

        /// <summary>En que linea sale la roja: siempre en la segunda mitad, para que haya que estar atento.</summary>
        public static int IndiceDeLaRoja(int semilla) {
            var r = new Random(semilla);
            return LineasDeLog / 2 + r.Next(LineasDeLog / 3);
        }
    }

    /// <summary>
    /// Las diapositivas del escritorio en la Fase 2 (el carrusel), en su orden. Las fijas para todos, y las que pone la
    /// metodologia: Scrum mira el burndown, Kanban el flujo acumulado, Cascada la curva S y sus documentos. La del
    /// cliente, solo si el proyecto trae papeles del cliente.
    /// </summary>
    public static class DiapositivasDelDia {
        public const string Kpis = "kpis";
        public const string Burndown = "burndown";
        public const string Flujo = "cfd";
        public const string CurvaS = "curvaS";
        public const string Evolucion = "evolucion";
        public const string Documentos = "documentos";
        public const string Tablero = "tablero";
        public const string Equipo = "equipo";
        public const string Escritorio = "escritorio";
        public const string Cliente = "cliente";

        public static List<string> Para(IEnumerable<string> vistasDeLaMetodologia, bool hayCliente) {
            var vistas = new HashSet<string>(vistasDeLaMetodologia ?? new string[0], StringComparer.Ordinal);
            var lista = new List<string> { Kpis };
            // La grafica de la metodologia (una sola, la que la define); si no trae ninguna, la evolucion del proyecto.
            if (vistas.Contains(Nexus.Core.Metodologia.VistasDeMonitoreo.Burndown)) lista.Add(Burndown);
            else if (vistas.Contains(Nexus.Core.Metodologia.VistasDeMonitoreo.Cfd)) lista.Add(Flujo);
            else if (vistas.Contains(Nexus.Core.Metodologia.VistasDeMonitoreo.CurvaS)) lista.Add(CurvaS);
            else lista.Add(Evolucion);
            if (vistas.Contains(Nexus.Core.Metodologia.VistasDeMonitoreo.Documentos)) lista.Add(Documentos);
            lista.Add(Tablero);
            lista.Add(Equipo);
            lista.Add(Escritorio);
            if (hayCliente) lista.Add(Cliente);
            return lista;
        }

        public static string Nombre(string id) {
            switch (id) {
                case Kpis: return "Indicadores";
                case Burndown: return "Burndown";
                case Flujo: return "Flujo";
                case CurvaS: return "Curva S";
                case Evolucion: return "Evolución";
                case Documentos: return "Documentos";
                case Tablero: return "Tablero";
                case Equipo: return "Equipo";
                case Escritorio: return "Escritorio";
                case Cliente: return "Cliente";
                default: return id;
            }
        }
    }
}
