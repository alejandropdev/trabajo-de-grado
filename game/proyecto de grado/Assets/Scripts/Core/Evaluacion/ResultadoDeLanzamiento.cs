using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Evaluacion {
    /// <summary>Como salio el lanzamiento. Tres niveles, no dos: «salio, pero…» es lo mas comun en la vida real.</summary>
    public static class NivelesDeLanzamiento {
        public const string Bien = "bien";
        public const string ConProblemas = "con-problemas";
        public const string Mal = "mal";

        public static string Titulo(string nivel) {
            switch (nivel) {
                case Bien: return "Salió bien";
                case ConProblemas: return "Salió, pero con problemas";
                default: return "Salió mal";
            }
        }
    }

    /// <summary>
    /// Lo que exige cada nivel para que su lanzamiento salga bien, y lo que ya es salir mal. Vive en el JSON del
    /// nivel (bloque "lanzamiento"): un tutorial de cinco dias no se mide igual que un proyecto de veinte.
    /// "...Bien" es el listón de «salió bien»; "...Mal" es la línea a partir de la cual sale mal sí o sí.
    /// </summary>
    public sealed class UmbralesDeLanzamiento {
        public double RiesgoBien = 45, RiesgoMal = 65;
        public int DefectosBien = 4, DefectosMal = 8;
        /// <summary>Fraccion del alcance comprometido que llego al cliente (0-1).</summary>
        public double EntregaBien = 0.85, EntregaMal = 0.6;
        public double SatisfaccionBien = 50, SatisfaccionMal = 30;
        /// <summary>Fraccion de decisiones bien tomadas (correcta = 1, aceptable = 0,5).</summary>
        public double DecisionesBien = 0.6, DecisionesMal = 0.35;
        /// <summary>El puntaje total (0-100) que hace falta para «bien», y por debajo del cual sale mal.</summary>
        public double PuntajeBien = 70, PuntajeMal = 45;

        public UmbralesDeLanzamiento Clone() { return (UmbralesDeLanzamiento)MemberwiseClone(); }
    }

    /// <summary>Lo que se mide en el lanzamiento, ya calculado por la sesion.</summary>
    public sealed class EntradaDeLanzamiento {
        public double Riesgo;
        public int Defectos;
        public double Entregado;
        public double Comprometido;
        public double Satisfaccion;
        public DecisionTrace Traza;
    }

    /// <summary>Un renglon del desglose: que se midio, cuanto dio, si estuvo bien, y por que importa.</summary>
    public sealed class FactorDeLanzamiento {
        public string Nombre;
        public string Valor;
        /// <summary>bien | regular | mal.</summary>
        public string Estado;
        public string Explicacion;
        /// <summary>Puntos que aporto al puntaje total, y sobre cuantos.</summary>
        public double Puntos;
        public double Maximo;
    }

    public sealed class CalificacionDeLanzamiento {
        public string Nivel;
        public double Puntaje;
        public List<FactorDeLanzamiento> Factores = new List<FactorDeLanzamiento>();
        /// <summary>Las decisiones que mas pesaron (las incorrectas, de la mas reciente a la mas antigua).</summary>
        public List<string> DecisionesQuePesaron = new List<string>();
    }

    /// <summary>
    /// El veredicto del lanzamiento, puro y testeable. Antes solo contaban el riesgo (con un listón de 62 que un
    /// nivel corto no alcanzaba nunca) y el alcance: se podia jugar la peor partida posible y leer «Salió bien».
    /// Ahora cuenta todo lo que el cliente nota:
    ///
    ///   alcance entregado 30 · errores que encontró 20 · riesgo 20 · satisfacción 15 · cómo decidiste 15
    ///
    /// Y dos reglas duras, para que el puntaje no tape un desastre: cualquier factor en su zona «mal» hunde el
    /// resultado a «salió mal»; y para «salió bien» hace falta el puntaje Y que ningun factor quede en «mal»
    /// ni mas de uno en «regular».
    /// </summary>
    public static class CalculoDeLanzamiento {
        public static CalificacionDeLanzamiento Calcular(EntradaDeLanzamiento e, UmbralesDeLanzamiento u) {
            if (e == null) throw new ArgumentNullException(nameof(e));
            u = u ?? new UmbralesDeLanzamiento();
            var c = new CalificacionDeLanzamiento();

            var fraccion = e.Comprometido > 0 ? Math.Max(0, Math.Min(1, e.Entregado / e.Comprometido)) : 0;
            c.Factores.Add(Factor("Alcance entregado", $"{e.Entregado:0} de {e.Comprometido:0} puntos ({fraccion * 100:0} %)",
                Estado(fraccion, u.EntregaBien, u.EntregaMal, true), 30, fraccion,
                "Lo que se prometió y no llegó, el cliente lo nota primero."));

            var limiteDef = Math.Max(1, u.DefectosMal * 1.5);
            c.Factores.Add(Factor("Errores que encontró el cliente", e.Defectos.ToString(),
                Estado(e.Defectos, u.DefectosBien, u.DefectosMal, false), 20, 1 - Math.Min(1, e.Defectos / limiteDef),
                "Salen de lo que no se probó (cobertura baja) y de los atajos (deuda técnica)."));

            c.Factores.Add(Factor("Riesgo con el que se lanzó", $"{e.Riesgo:0} / 100",
                Estado(e.Riesgo, u.RiesgoBien, u.RiesgoMal, false), 20, 1 - Math.Min(1, e.Riesgo / 100.0),
                "Junta cansancio, deuda, lo que falta por probar y por documentar."));

            c.Factores.Add(Factor("Satisfacción del cliente", $"{e.Satisfaccion:0} / 100",
                Estado(e.Satisfaccion, u.SatisfaccionBien, u.SatisfaccionMal, true), 15, Math.Max(0, Math.Min(1, e.Satisfaccion / 100.0)),
                "Sube cuando escuchas, negocias y cumples; baja con cada «no» sin explicar y cada sorpresa."));

            var decisiones = CalidadDeDecisiones(e.Traza);
            var cuantas = ContarDecisiones(e.Traza);
            c.Factores.Add(Factor("Cómo decidiste", cuantas == 0 ? "sin decisiones" : $"{decisiones * 100:0} % bien tomadas ({cuantas})",
                cuantas == 0 ? "regular" : Estado(decisiones, u.DecisionesBien, u.DecisionesMal, true), 15, cuantas == 0 ? 0.5 : decisiones,
                "Cada decisión mal justificada deja su huella en el proyecto, aunque hoy no se note."));

            c.Puntaje = Math.Round(c.Factores.Sum(f => f.Puntos), 1);
            var malos = c.Factores.Count(f => f.Estado == "mal");
            var regulares = c.Factores.Count(f => f.Estado == "regular");

            if (malos > 0 || c.Puntaje < u.PuntajeMal) c.Nivel = NivelesDeLanzamiento.Mal;
            else if (c.Puntaje >= u.PuntajeBien && regulares <= 1) c.Nivel = NivelesDeLanzamiento.Bien;
            else c.Nivel = NivelesDeLanzamiento.ConProblemas;

            if (e.Traza != null)
                c.DecisionesQuePesaron = e.Traza.Entradas
                    .Where(x => x != null && x.Veredicto == Veredictos.Incorrecta && x.Origen != "LANZAMIENTO")
                    .Reverse().Take(3)
                    .Select(x => $"Día {x.Dia} · {x.Titulo}: «{x.OpcionTexto}»" + (string.IsNullOrEmpty(x.Razon) ? "" : " — " + x.Razon))
                    .ToList();
            return c;
        }

        /// <summary>correcta = 1, aceptable = 0,5, incorrecta = 0, sobre las decisiones del jugador (no el propio lanzamiento).</summary>
        public static double CalidadDeDecisiones(DecisionTrace traza) {
            var n = ContarDecisiones(traza);
            if (n == 0) return 0;
            var suma = traza.Entradas.Where(EsDecision)
                .Sum(x => x.Veredicto == Veredictos.Correcta ? 1.0 : x.Veredicto == Veredictos.Aceptable ? 0.5 : 0.0);
            return suma / n;
        }

        private static int ContarDecisiones(DecisionTrace traza) {
            return traza == null ? 0 : traza.Entradas.Count(EsDecision);
        }

        /// <summary>El lanzamiento no se juzga a si mismo, y la comprobacion del expediente no penaliza: no son decisiones.</summary>
        private static bool EsDecision(EntradaTraza x) { return x != null && x.Origen != "LANZAMIENTO" && x.Origen != "COMPRENSION"; }

        /// <param name="masEsMejor">true si un valor alto es bueno (entrega, satisfaccion); false si es malo (riesgo, errores).</param>
        private static string Estado(double valor, double bien, double mal, bool masEsMejor) {
            if (masEsMejor) return valor >= bien ? "bien" : valor < mal ? "mal" : "regular";
            return valor <= bien ? "bien" : valor > mal ? "mal" : "regular";
        }

        private static FactorDeLanzamiento Factor(string nombre, string valor, string estado, double peso, double fraccion01, string porQue) {
            return new FactorDeLanzamiento {
                Nombre = nombre, Valor = valor, Estado = estado, Explicacion = porQue,
                Maximo = peso, Puntos = Math.Round(peso * Math.Max(0, Math.Min(1, fraccion01)), 1)
            };
        }
    }
}
