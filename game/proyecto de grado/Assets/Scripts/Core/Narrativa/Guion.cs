using System;
using System.Collections.Generic;

namespace Nexus.Core.Narrativa {
    /// <summary>
    /// Cuando se reproduce un guion. Solo 'dia' lo decide el NarrativeDirector (son los beats de
    /// narrativa.json); los demas los lanza el flujo de pantallas en su momento fijo.
    /// </summary>
    public static class MomentosDeGuion {
        public const string AntesDelNivel = "antesDelNivel";
        public const string Apertura = "apertura";
        public const string Fase1 = "fase1";
        public const string Dia = "dia";
        public const string Lanzamiento = "lanzamiento";
        public const string Cierre = "cierre";
        public const string DespuesDelNivel = "despuesDelNivel";

        private static readonly string[] _todos = { AntesDelNivel, Apertura, Fase1, Dia, Lanzamiento, Cierre, DespuesDelNivel };
        public static IReadOnlyList<string> Todos { get { return _todos; } }

        public static bool EsValido(string momento) {
            foreach (var m in _todos)
                if (string.Equals(m, momento, StringComparison.Ordinal)) return true;
            return false;
        }
    }

    /// <summary>
    /// Lo que se dice en una escena. El beat (NarrativeBeat) decide CUANDO y con que variante; el guion
    /// dice QUE. Separarlos permite reescribir un dialogo sin tocar la agenda narrativa, y comprobar al
    /// cargar que cada variante que el director puede elegir tiene texto escrito.
    /// </summary>
    public sealed class Guion {
        /// <summary>Coincide con el id del beat cuando el momento es 'dia'.</summary>
        public string Id;
        public string Titulo;
        public string Nivel;
        public string Momento;

        /// <summary>Orden entre los guiones del mismo nivel y momento (la entrevista va entre CIN-0.0 y CIN-0.1).</summary>
        public int Orden;

        /// <summary>variante -> lineas. Sin coloreo, la unica variante es "default".</summary>
        public Dictionary<string, List<LineaDeGuion>> Variantes =
            new Dictionary<string, List<LineaDeGuion>>(StringComparer.Ordinal);

        /// <summary>
        /// La eleccion con la que termina la escena, si la hay. No escribe nada al momento: INV-6 solo deja
        /// escribir flags al cerrar el nivel, asi que la eleccion se anota y el puente la vuelca en Cerrar().
        /// </summary>
        public List<OpcionDeGuion> Opciones = new List<OpcionDeGuion>();
    }

    public sealed class LineaDeGuion {
        /// <summary>Quien habla. "narrador" para acotaciones y texto en pantalla.</summary>
        public string Quien;
        public string Texto;
    }

    public sealed class OpcionDeGuion {
        public string Id;
        public string Texto;

        /// <summary>El flag que quedara escrito al cerrar el nivel si se elige esta opcion. Opcional.</summary>
        public string Flag;
        public double Valor;
    }
}
