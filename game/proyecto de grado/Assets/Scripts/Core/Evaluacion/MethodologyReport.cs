using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Evaluacion {
    /// <summary>
    /// Lo que Evaluacion necesita saber de la metodologia elegida. Lo construye GameSession
    /// a partir del MethodologyProfile (C5), para que este paquete no dependa de Core.Method.
    /// </summary>
    public sealed class DatosDeMetodologia {
        public string Id;
        public string Nombre;
        public string Familia;                                    // agil | tradicional
        public List<string> RazonesValidas = new List<string>();
        public List<string> RazonesTrampa = new List<string>();
        public List<PracticaAEvaluar> Practicas = new List<PracticaAEvaluar>();   // RubricaCierre.Practicas
    }

    /// <summary>Espejo de PracticaEsperada (§4.5.1).</summary>
    public sealed class PracticaAEvaluar {
        public string Id;
        public string Descripcion;
        public string Metrica;                                    // nombre de MethodologyReport.Metricas
        public string Comparador;                                 // >= | <= | > | < | ==
        public double Objetivo;
        public string RazonSiCumple;
        public string RazonSiFalla;
    }

    public sealed class PracticaEvaluada {
        public string Id;
        public string Descripcion;
        public string Metrica;
        public string Comparador;
        public double Objetivo;
        public double Valor;
        /// <summary>False si la metrica o el comparador no existen: se informa, no se tumba el cierre.</summary>
        public bool Evaluable;
        public bool Cumple;
        public string Razon;
    }

    /// <summary>
    /// El cierre pedagogico (§4.8.3), construido una vez dentro de Cerrar(). Responde tres preguntas:
    /// ¿acertaste por buen motivo?, ¿era la metodologia adecuada?, ¿aplicaste las practicas de TU metodologia?
    /// </summary>
    public sealed class MethodologyReport {
        public const string FamiliaAgil = "agil";
        public const double UmbralVolatilidad = 50.0;

        public static class Metricas {
            public const string DiasConHorasExtra = "diasConHorasExtra";
            public const string CambiosAceptados = "cambiosAceptados";
            public const string AccionesRetroElegidas = "accionesRetroElegidas";
            public const string VolatilidadReal = "volatilidadReal";
            public const string CoberturaAlCerrarDiseno = "coberturaAlCerrarDiseno";
            public const string VecesExcedioWip = "vecesExcedioWip";
            public const string WipMedio = "wipMedio";
            public const string LeadTimeMedio = "leadTimeMedio";
            public const string DesviacionCompromisoPct = "desviacionCompromisoPct";
        }

        public string MetodologiaId;
        public string MetodologiaNombre;
        public string Familia;
        public string RazonElegida;

        /// <summary>1 · La razon esta entre las validas.</summary>
        public bool RazonValida;
        /// <summary>1 · La razon es una trampa: se puede acertar por el motivo equivocado, y el juego lo distingue.</summary>
        public bool RazonTrampa;

        /// <summary>2 · "No hay modelo mejor, hay modelo adecuado": agil si y solo si volatilidad >= 50.</summary>
        public double VolatilidadReal;
        public bool EraAdecuada;

        /// <summary>3 · Las practicas de su metodologia contra las metricas reales del nivel.</summary>
        public List<PracticaEvaluada> Practicas = new List<PracticaEvaluada>();
        public Dictionary<string, double> ValoresDeMetricas = new Dictionary<string, double>();

        /// <summary>Las 9 metricas agregadas que las rubricas de cierre consultan por nombre.</summary>
        public static Dictionary<string, double> CalcularMetricas(IMetricasDelNivel m) {
            if (m == null) throw new ArgumentNullException(nameof(m));
            return new Dictionary<string, double> {
                { Metricas.DiasConHorasExtra, m.DiasConHorasExtra },
                { Metricas.CambiosAceptados, m.CambiosAceptados },
                { Metricas.AccionesRetroElegidas, m.AccionesRetroElegidas },
                { Metricas.VolatilidadReal, m.VolatilidadReal },
                { Metricas.CoberturaAlCerrarDiseno, m.CoberturaAlCerrarDiseno },
                { Metricas.VecesExcedioWip, m.VecesExcedioWip },
                { Metricas.WipMedio, Media(m.SerieWip) },
                { Metricas.LeadTimeMedio, Media(m.SerieLeadTime) },
                { Metricas.DesviacionCompromisoPct, DesviacionCompromiso(m.CompromisosPorIteracion, m.EntregadoPorIteracion) }
            };
        }

        public static MethodologyReport Construir(DatosDeMetodologia met, string razonElegida,
                                                  double volatilidadReal, IReadOnlyDictionary<string, double> metricas) {
            if (met == null) throw new ArgumentNullException(nameof(met));

            var rep = new MethodologyReport {
                MetodologiaId = met.Id,
                MetodologiaNombre = met.Nombre,
                Familia = met.Familia,
                RazonElegida = razonElegida,
                RazonValida = razonElegida != null && met.RazonesValidas != null && met.RazonesValidas.Contains(razonElegida),
                RazonTrampa = razonElegida != null && met.RazonesTrampa != null && met.RazonesTrampa.Contains(razonElegida),
                VolatilidadReal = volatilidadReal,
                EraAdecuada = (met.Familia == FamiliaAgil) == (volatilidadReal >= UmbralVolatilidad)
            };

            if (metricas != null)
                foreach (var kv in metricas) rep.ValoresDeMetricas[kv.Key] = kv.Value;

            if (met.Practicas != null)
                foreach (var p in met.Practicas)
                    rep.Practicas.Add(Evaluar(p, metricas));

            return rep;
        }

        /// <summary>null si el comparador no pertenece al vocabulario.</summary>
        public static bool? Comparar(double valor, string comparador, double objetivo) {
            switch (comparador) {
                case ">=": return valor >= objetivo;
                case "<=": return valor <= objetivo;
                case ">": return valor > objetivo;
                case "<": return valor < objetivo;
                case "==": return Math.Abs(valor - objetivo) < 1e-9;
                default: return null;
            }
        }

        private static PracticaEvaluada Evaluar(PracticaAEvaluar p, IReadOnlyDictionary<string, double> metricas) {
            var ev = new PracticaEvaluada {
                Id = p.Id,
                Descripcion = p.Descripcion,
                Metrica = p.Metrica,
                Comparador = p.Comparador,
                Objetivo = p.Objetivo
            };

            double valor;
            if (metricas == null || p.Metrica == null || !metricas.TryGetValue(p.Metrica, out valor)) {
                ev.Razon = $"No se pudo evaluar: la métrica '{p.Metrica}' no existe.";
                return ev;
            }
            ev.Valor = valor;

            var cumple = Comparar(valor, p.Comparador, p.Objetivo);
            if (cumple == null) {
                ev.Razon = $"No se pudo evaluar: el comparador '{p.Comparador}' no es válido.";
                return ev;
            }

            ev.Evaluable = true;
            ev.Cumple = cumple.Value;
            ev.Razon = ev.Cumple ? p.RazonSiCumple : p.RazonSiFalla;
            return ev;
        }

        private static double Media(IReadOnlyList<double> serie) {
            return serie == null || serie.Count == 0 ? 0.0 : serie.Average();
        }

        /// <summary>Media de |comprometido − entregado| / comprometido × 100; se saltan las iteraciones sin compromiso.</summary>
        private static double DesviacionCompromiso(IReadOnlyList<double> comprometido, IReadOnlyList<double> entregado) {
            if (comprometido == null || entregado == null) return 0.0;
            var n = Math.Min(comprometido.Count, entregado.Count);
            var desviaciones = new List<double>();
            for (var i = 0; i < n; i++) {
                if (comprometido[i] <= 0) continue;
                desviaciones.Add(Math.Abs(comprometido[i] - entregado[i]) / comprometido[i] * 100.0);
            }
            return desviaciones.Count == 0 ? 0.0 : desviaciones.Average();
        }
    }
}
