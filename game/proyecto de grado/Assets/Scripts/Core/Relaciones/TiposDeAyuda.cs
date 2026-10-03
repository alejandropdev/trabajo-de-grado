using System;
using System.Collections.Generic;

namespace Nexus.Core.Relaciones {
    /// <summary>
    /// Las ayudas que da un compañero cuando confia en ti. Son puntuales y concretas: no suben el andamiaje (los
    /// niveles del tutorial ya estan en el maximo), cambian UNA cosa de UN minijuego o del dia.
    /// </summary>
    public static class TiposDeAyuda {
        /// <summary>Repartir: cada pila dice cuantos errores hay de verdad.</summary>
        public const string RevelarDefectos = "revelar-defectos";
        /// <summary>Ordenar: cada tarjeta dice que necesita, y las dependencias rotas se ven en vivo.</summary>
        public const string MostrarDependencias = "mostrar-dependencias";
        /// <summary>Detectar: una pieza con un problema de verdad se marca en amarillo.</summary>
        public const string PistaDetectar = "pista-detectar";
        /// <summary>Cualquier minijuego: 45 segundos mas de reloj.</summary>
        public const string MinutosExtra = "minutos-extra";
        /// <summary>Del dia: el compañero te cubre un rato y baja el cansancio.</summary>
        public const string BajarCansancio = "bajar-cansancio";

        private static readonly string[] _todos = { RevelarDefectos, MostrarDependencias, PistaDetectar, MinutosExtra, BajarCansancio };

        public static IReadOnlyList<string> Todos { get { return _todos; } }

        public static bool EsValido(string tipo) { return Array.IndexOf(_todos, tipo) >= 0; }

        /// <summary>Las que se gastan dentro de un minijuego (las otras se usan desde la jornada).</summary>
        public static bool EsDeMinijuego(string tipo) { return tipo != BajarCansancio; }

        /// <summary>¿Sirve esta ayuda para este verbo?</summary>
        public static bool SirvePara(string tipo, string verbo) {
            var v = Nexus.Core.Minijuegos.Verbos.Normalizar(verbo);
            switch (tipo) {
                case RevelarDefectos: return v == Nexus.Core.Minijuegos.Verbos.Repartir;
                case MostrarDependencias: return v == Nexus.Core.Minijuegos.Verbos.Ordenar;
                case PistaDetectar: return v == Nexus.Core.Minijuegos.Verbos.Detectar;
                case MinutosExtra: return true;
                default: return false;
            }
        }
    }
}
