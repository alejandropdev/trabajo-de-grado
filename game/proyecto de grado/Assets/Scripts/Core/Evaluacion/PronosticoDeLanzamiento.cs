using System;

namespace Nexus.Core.Evaluacion {
    /// <summary>
    /// Lo que cuesta cada barra del proyecto, en numeros que el jugador entiende: cuanto mas lento va el equipo
    /// por la deuda, cuantos errores le van a llegar al cliente por la cobertura… Lo usan el lanzamiento real y
    /// el pronostico del dia, asi que lo que se enseña durante la Fase 2 es exactamente lo que se cobrara al final.
    /// </summary>
    public static class PronosticoDeLanzamiento {
        /// <summary>Errores que escapan al cliente. Es LA formula del lanzamiento.</summary>
        public static int Defectos(double cobertura, double deuda, bool entregaIncremental) {
            return Math.Max(0, (int)Math.Round(DefectosPorCobertura(cobertura, entregaIncremental) + DefectosPorDeuda(deuda, entregaIncremental)));
        }

        public static double DefectosPorCobertura(double cobertura, bool entregaIncremental) {
            return (100.0 - cobertura) / 12.0 * (entregaIncremental ? 1.0 : 1.3);
        }

        public static double DefectosPorDeuda(double deuda, bool entregaIncremental) {
            return deuda / 15.0 * (entregaIncremental ? 1.0 : 1.3);
        }

        /// <summary>
        /// El lanzamiento si el proyecto siguiera como va: el avance de hoy mas lo que el equipo hace al ritmo de hoy
        /// en los dias que quedan, con los errores y el riesgo de ahora.
        /// </summary>
        public static CalificacionDeLanzamiento Calcular(double avance, double alcance, double velocidad, int diasRestantes,
                                                         double cobertura, double deuda, double riesgoLatente, double factorRiesgo,
                                                         bool entregaIncremental, double satisfaccion, DecisionTrace traza,
                                                         UmbralesDeLanzamiento umbrales) {
            var proyectado = Math.Min(alcance, avance + Math.Max(0, velocidad) * Math.Max(0, diasRestantes));
            return CalculoDeLanzamiento.Calcular(new EntradaDeLanzamiento {
                Riesgo = Math.Min(100.0, riesgoLatente * factorRiesgo),
                Defectos = Defectos(cobertura, deuda, entregaIncremental),
                Entregado = proyectado, Comprometido = alcance, Satisfaccion = satisfaccion, Traza = traza
            }, umbrales);
        }
    }
}
