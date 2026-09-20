using System;
using System.Collections.Generic;

namespace Nexus.Core.Servicios {
    /// <summary>
    /// El vocabulario que IStateContext expone por encima de los stocks y del RuntimeState (§4.4.1).
    /// Son magnitudes que solo GameSession sabe calcular, pero que el contenido puede consultar.
    ///
    /// Vive aqui, junto al puerto, y no dentro de GameSession, porque el validador de catalogos (C10b)
    /// necesita conocer la lista ANTES de que exista ninguna sesion: es lo que le permite cazar una
    /// precondicion con una variable mal escrita al cargar el juego, y no el dia 14 de una partida.
    /// </summary>
    public static class VariablesDeSesion {
        /// <summary>Dias totales del nivel, del LevelProfile.</summary>
        public const string DiasTotales = "diasTotales";

        /// <summary>0-100, oculta al jugador. Contra ella se juzga si la metodologia encajaba.</summary>
        public const string VolatilidadReal = "volatilidadReal";

        /// <summary>La derivada del modelo, recalculada cada dia.</summary>
        public const string RiesgoLatente = "riesgoLatente";

        /// <summary>Cuanto se va el proyecto del plan: (esperado − avance) / esperado.</summary>
        public const string RetrasoRelativo = "retrasoRelativo";

        /// <summary>Las dos funciones que una precondicion puede llamar.</summary>
        public const string FuncionDiasDesde = "diasDesde";
        public const string FuncionOcurrencias = "ocurrencias";

        private static readonly string[] _nombres = {
            DiasTotales, VolatilidadReal, RiesgoLatente, RetrasoRelativo
        };

        private static readonly string[] _funciones = { FuncionDiasDesde, FuncionOcurrencias };

        public static IReadOnlyList<string> Nombres { get { return _nombres; } }
        public static IReadOnlyList<string> Funciones { get { return _funciones; } }

        public static bool EsVariable(string nombre) {
            foreach (var n in _nombres)
                if (string.Equals(n, nombre, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static bool EsFuncion(string nombre) {
            foreach (var f in _funciones)
                if (string.Equals(f, nombre, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
