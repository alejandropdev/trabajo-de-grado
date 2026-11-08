using System.Collections.Generic;
using Nexus.Core.Minijuegos;

namespace Nexus.Core.Tablero {
    /// <summary>
    /// Los numeros de linea de un diff, como en el margen de un IDE: a la izquierda el del archivo de antes, a la derecha
    /// el de despues. El contexto avanza los dos; lo que se quita, solo el de antes; lo que se añade, solo el de despues.
    /// 0 = esa columna va vacia en esa linea.
    /// </summary>
    public static class NumeracionDeDiff {
        public struct Numeros {
            public int Antes, Despues;
        }

        public static List<Numeros> Numerar(IList<LineaDiff> lineas, int primera = 1) {
            var r = new List<Numeros>();
            var antes = primera;
            var despues = primera;
            if (lineas == null) return r;
            foreach (var l in lineas) {
                switch (l == null ? "ctx" : l.Tipo) {
                    case "add": r.Add(new Numeros { Antes = 0, Despues = despues++ }); break;
                    case "del": r.Add(new Numeros { Antes = antes++, Despues = 0 }); break;
                    default: r.Add(new Numeros { Antes = antes++, Despues = despues++ }); break;
                }
            }
            return r;
        }

        /// <summary>Cuantas lineas añade y cuantas quita («+N −M» de la cabecera del cambio).</summary>
        public static void Contar(IList<LineaDiff> lineas, out int anadidas, out int quitadas) {
            anadidas = quitadas = 0;
            if (lineas == null) return;
            foreach (var l in lineas) {
                if (l == null) continue;
                if (l.Tipo == "add") anadidas++;
                else if (l.Tipo == "del") quitadas++;
            }
        }
    }
}
