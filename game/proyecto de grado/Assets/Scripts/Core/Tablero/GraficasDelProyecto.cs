using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Tablero {
    /// <summary>Lo que el flujo del tablero dice de si mismo: cuanto tarda una tarjeta, cuantas salen, cual lleva mas tiempo.</summary>
    public sealed class MetricasDeFlujo {
        /// <summary>Dias desde que una tarjeta se empieza hasta que se termina, de media (las terminadas).</summary>
        public double TiempoDeCiclo;
        /// <summary>Tarjetas terminadas por dia, en los ultimos dias.</summary>
        public double Rendimiento;
        public int EnCurso;
        public int Terminadas;
        /// <summary>La tarjeta a medias que mas dias lleva, y cuantos (null si no hay ninguna a medias).</summary>
        public string MasVieja;
        public int DiasDeLaMasVieja;
    }

    /// <summary>
    /// Las cuentas de las tres graficas, una por familia de metodologia. Son funciones puras sobre lo que el motor ya
    /// guarda: la pantalla solo dibuja.
    ///   Burndown  lo que queda del sprint, dia a dia, contra la linea ideal (Scrum)
    ///   CFD       las bandas por hacer / en curso / terminado estan en RuntimeState; aqui, las metricas de flujo (Kanban)
    ///   Curva S   lo planificado, con forma de S, contra lo hecho (Cascada)
    /// </summary>
    public static class GraficasDelProyecto {
        /// <summary>La linea ideal de un sprint: de lo comprometido a cero, a ritmo constante. Un punto por cierre de dia, mas el inicial.</summary>
        public static List<double> BurndownIdeal(double comprometido, int diasDelSprint) {
            var ideal = new List<double>();
            var dias = Math.Max(1, diasDelSprint);
            for (var d = 0; d <= dias; d++) ideal.Add(Math.Round(comprometido * (1 - d / (double)dias), 2));
            return ideal;
        }

        /// <summary>
        /// El avance planificado al empezar cada dia, con forma de S: se arranca despacio (analisis y diseño dan poco
        /// avance visible), se acelera en la construccion y se frena al cerrar. Un punto por dia, mas el del final.
        /// </summary>
        public static List<double> CurvaSPlanificada(double alcance, int diasTotales) {
            var plan = new List<double>();
            var dias = Math.Max(1, diasTotales);
            for (var d = 0; d <= dias; d++) {
                var x = d / (double)dias;
                plan.Add(Math.Round(alcance * (3 * x * x - 2 * x * x * x), 2));
            }
            return plan;
        }

        /// <summary>El retraso (o adelanto) de hoy respecto al plan, en puntos: positivo = se va por detras.</summary>
        public static double DesvioRespectoAlPlan(double avance, double alcance, int dia, int diasTotales) {
            var plan = CurvaSPlanificada(alcance, diasTotales);
            var i = Math.Max(0, Math.Min(plan.Count - 1, dia - 1));
            return Math.Round(plan[i] - avance, 2);
        }

        public static MetricasDeFlujo Flujo(TableroDelEquipo t, int diaActual, int ventana = 5) {
            var m = new MetricasDeFlujo();
            if (t == null) return m;
            var tarjetas = t.Tarjetas.Where(c => !c.EsBug).ToList();
            var hechas = tarjetas.Where(c => c.Terminada && c.DiaInicio >= 0 && c.DiaFin >= 0).ToList();
            m.Terminadas = tarjetas.Count(c => c.Terminada);
            m.TiempoDeCiclo = hechas.Count == 0 ? 0 : Math.Round(hechas.Average(c => c.DiaFin - c.DiaInicio + 1.0), 1);
            var dias = Math.Max(1, Math.Min(ventana, diaActual));
            m.Rendimiento = Math.Round(hechas.Count(c => c.DiaFin > diaActual - dias) / (double)dias, 2);
            var aMedias = tarjetas.Where(c => c.Empezada && !c.Terminada).ToList();
            m.EnCurso = aMedias.Count;
            var vieja = aMedias.Where(c => c.DiaInicio >= 0).OrderBy(c => c.DiaInicio).FirstOrDefault();
            if (vieja != null) { m.MasVieja = vieja.Titulo; m.DiasDeLaMasVieja = Math.Max(1, diaActual - vieja.DiaInicio + 1); }
            return m;
        }
    }
}
