using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Proyecto {
    /// <summary>Una pregunta de «Comprueba que lo entendiste»: una sola opcion correcta.</summary>
    public sealed class PreguntaDelProyecto {
        public string Id;
        public string Texto;
        public List<string> Opciones = new List<string>();
        public int Correcta;
        /// <summary>Lo que se enseña al fallar (y al acertar): el porque, con las palabras del proyecto.</summary>
        public string Explicacion;
    }

    /// <summary>
    /// Las tres preguntas de comprobacion del paso «El proyecto» de la Fase 1, generadas de la ficha:
    ///   1 · una palabra del sector y lo que significa;
    ///   2 · una necesidad de un usuario y el modulo que la resuelve;
    ///   3 · lo que le duele al cliente.
    /// Deterministas (la misma ficha da siempre las mismas preguntas, con las opciones en el mismo orden): no
    /// gastan azar del motor y se pueden probar. Si a la ficha le falta material para una, esa no sale.
    /// </summary>
    public static class PreguntasDelProyecto {
        public const int Opciones = 3;

        public static List<PreguntaDelProyecto> Para(FichaDelProyecto f, string semilla) {
            var r = new List<PreguntaDelProyecto>();
            if (f == null) return r;
            var h = Hash(semilla ?? f.Nombre ?? "");

            // 1 · vocabulario
            var voc = f.Vocabulario.Where(t => !string.IsNullOrEmpty(t.Termino) && !string.IsNullOrEmpty(t.Definicion)).ToList();
            if (voc.Count >= Opciones) {
                var i = h % voc.Count;
                var t = voc[i];
                var otras = Enumerable.Range(1, voc.Count - 1).Select(k => voc[(i + k) % voc.Count].Definicion).Take(Opciones - 1);
                r.Add(Montar("vocabulario", $"En este proyecto, ¿qué es «{t.Termino}»?", t.Definicion, otras, h,
                             $"«{t.Termino}»: {t.Definicion}" + (string.IsNullOrEmpty(t.DondeLoVeras) ? "" : " Lo verás en " + t.DondeLoVeras)));
            }

            // 2 · necesidad -> modulo
            var usuarios = f.Usuarios.Where(u => f.Modulo(u.Modulo) != null && !string.IsNullOrEmpty(u.QueNecesita)).ToList();
            if (usuarios.Count > 0 && f.Modulos.Count >= Opciones) {
                var u = usuarios[(h / 7) % usuarios.Count];
                var m = f.Modulo(u.Modulo);
                var i = f.Modulos.IndexOf(m);
                var otros = Enumerable.Range(1, f.Modulos.Count - 1).Select(k => f.Modulos[(i + k) % f.Modulos.Count].Nombre).Take(Opciones - 1);
                r.Add(Montar("modulo", $"{u.Nombre} necesita {Minuscula(u.QueNecesita)} ¿Qué parte del sistema lo resuelve?", m.Nombre, otros, h / 3,
                             $"{m.Nombre}: {m.QueHace}"));
            }

            // 3 · el cliente
            var c = f.Cliente;
            if (c != null && !string.IsNullOrEmpty(c.QueLeDuele) && c.Distractores.Count >= Opciones - 1)
                r.Add(Montar("cliente", $"¿Qué problema tiene {c.Nombre} hoy, el que el proyecto tiene que resolver?", c.QueLeDuele,
                             c.Distractores.Take(Opciones - 1), h / 11,
                             $"{c.Nombre} {Minuscula(c.QueHace)} Hoy: {c.QueLeDuele}"));
            return r;
        }

        private static PreguntaDelProyecto Montar(string id, string texto, string buena, IEnumerable<string> malas, int giro, string explicacion) {
            var opciones = new List<string> { buena };
            opciones.AddRange(malas);
            var g = giro % opciones.Count;
            var giradas = opciones.Skip(g).Concat(opciones.Take(g)).ToList();
            return new PreguntaDelProyecto { Id = id, Texto = texto, Opciones = giradas, Correcta = giradas.IndexOf(buena), Explicacion = explicacion };
        }

        private static string Minuscula(string s) {
            return string.IsNullOrEmpty(s) ? "" : char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        /// <summary>Un hash estable (string.GetHashCode cambia entre ejecuciones en .NET moderno).</summary>
        private static int Hash(string s) {
            unchecked {
                var h = 17;
                foreach (var ch in s) h = h * 31 + ch;
                return h & 0x7fffffff;
            }
        }
    }

    /// <summary>
    /// Cuanto lleva cada modulo, a partir del avance global: el trabajo que queda se reparte entre los modulos segun
    /// su peso y lo que les falta, y se va completando en el orden de construccion. Es una lectura del avance, no una
    /// simulacion nueva: con el avance a 0 cada modulo esta en su avance inicial, con el avance igual al alcance
    /// todos estan al 100 %, y nunca baja si el avance no baja.
    /// </summary>
    public static class AvanceDeModulos {
        public static List<KeyValuePair<ModuloDelProyecto, double>> Calcular(FichaDelProyecto f, double avance, double alcance) {
            var r = new List<KeyValuePair<ModuloDelProyecto, double>>();
            if (f == null || f.Modulos.Count == 0) return r;
            var fraccion = alcance <= 0 ? 0 : Math.Max(0, Math.Min(1, avance / alcance));
            var faltan = f.Modulos.Select(m => Math.Max(0, m.Peso) * (100 - Limitar(m.AvanceInicial)) / 100).ToList();
            var hecho = faltan.Sum() * fraccion;
            for (var i = 0; i < f.Modulos.Count; i++) {
                var m = f.Modulos[i];
                var inicial = Limitar(m.AvanceInicial);
                var toca = Math.Min(hecho, faltan[i]);
                hecho -= toca;
                var pct = faltan[i] <= 0 ? inicial : inicial + (100 - inicial) * (toca / faltan[i]);
                r.Add(new KeyValuePair<ModuloDelProyecto, double>(m, Limitar(pct)));
            }
            return r;
        }

        private static double Limitar(double v) { return Math.Max(0, Math.Min(100, v)); }
    }
}
