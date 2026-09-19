using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Nexus.Core.Evaluacion;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// C8 · Evaluacion pedagogica. Local, determinista y sin IA: todo se puede comprobar en una terminal.
    /// </summary>
    public class EvaluacionPedagogicaTests {
        private const double Tol = 1e-9;

        private sealed class MetricasFalsas : IMetricasDelNivel {
            public int DiasConHorasExtra { get; set; }
            public int CambiosAceptados { get; set; }
            public int AccionesRetroElegidas { get; set; }
            public double VolatilidadReal { get; set; }
            public double CoberturaAlCerrarDiseno { get; set; }
            public int VecesExcedioWip { get; set; }
            public IReadOnlyList<double> SerieWip { get; set; } = new List<double>();
            public IReadOnlyList<double> SerieLeadTime { get; set; } = new List<double>();
            public IReadOnlyList<double> CompromisosPorIteracion { get; set; } = new List<double>();
            public IReadOnlyList<double> EntregadoPorIteracion { get; set; } = new List<double>();
        }

        private static EntradaTraza Entrada(int dia, string veredicto, string titulo = "Evento", string opcion = "Opción") {
            return new EntradaTraza {
                Dia = dia, Origen = "EV-TEST", Titulo = titulo, OpcionTexto = opcion,
                Veredicto = veredicto, Oa = "OA-TEST-01", Razon = "porque sí"
            };
        }

        private static DatosDeMetodologia Scrum() {
            return new DatosDeMetodologia {
                Id = "scrum", Nombre = "Scrum", Familia = "agil",
                RazonesValidas = new List<string> { "el_cliente_cambiara_de_opinion" },
                RazonesTrampa = new List<string> { "es_la_de_moda" },
                Practicas = new List<PracticaAEvaluar> {
                    new PracticaAEvaluar { Id = "retro", Metrica = "accionesRetroElegidas", Comparador = ">=", Objetivo = 2,
                                           RazonSiCumple = "mejoraste el proceso", RazonSiFalla = "no mejoraste nada" },
                    new PracticaAEvaluar { Id = "ritmo", Metrica = "diasConHorasExtra", Comparador = "<=", Objetivo = 1,
                                           RazonSiCumple = "ritmo sostenible", RazonSiFalla = "quemaste al equipo" }
                }
            };
        }

        [Test]
        public void La_escala_es_uno_medio_y_cero() {
            var p = new CompetenceProfile();
            p.Acumular("OA-A", Veredictos.Correcta);
            p.Acumular("OA-B", Veredictos.Aceptable);
            p.Acumular("OA-C", Veredictos.Incorrecta);
            p.Acumular("OA-D", Veredictos.Correcta);
            p.Acumular("OA-D", Veredictos.Aceptable);

            Assert.AreEqual(100.0, p.PuntuacionDe("OA-A"), Tol);
            Assert.AreEqual(50.0, p.PuntuacionDe("OA-B"), Tol);
            Assert.AreEqual(0.0, p.PuntuacionDe("OA-C"), Tol);
            Assert.AreEqual(75.0, p.PuntuacionDe("OA-D"), Tol);
        }

        [Test]
        public void Sin_decisiones_la_puntuacion_es_cero() {
            Assert.AreEqual(0.0, new CompetenceProfile.Conteo().Puntuacion, Tol);
            Assert.AreEqual(0.0, new CompetenceProfile().PuntuacionDe("OA-NUNCA"), Tol);
        }

        [Test]
        public void Un_veredicto_fuera_del_vocabulario_lanza() {
            Assert.Throws<ArgumentException>(() => new CompetenceProfile().Acumular("OA-A", "regular"));
            Assert.Throws<ArgumentException>(() => new DecisionTrace().Registrar(Entrada(1, "Correcta")));
        }

        [Test]
        public void Una_entrada_sin_origen_lanza() {
            var e = Entrada(1, Veredictos.Correcta);
            e.Origen = null;
            Assert.Throws<ArgumentException>(() => new DecisionTrace().Registrar(e));
        }

        [Test]
        public void La_traza_conserva_el_orden_de_las_decisiones() {
            var t = new DecisionTrace();
            t.Registrar(Entrada(1, Veredictos.Correcta, "A"));
            t.Registrar(Entrada(3, Veredictos.Incorrecta, "B"));
            t.Registrar(Entrada(2, Veredictos.Aceptable, "C"));
            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, t.Entradas.Select(e => e.Titulo));
        }

        [Test]
        public void Fusionar_suma_los_conteos_de_otra_partida() {
            var perfil = new CompetenceProfile();
            perfil.Acumular("OA-A", Veredictos.Correcta);
            var partida = new CompetenceProfile();
            partida.Acumular("OA-A", Veredictos.Incorrecta);
            partida.Acumular("OA-B", Veredictos.Aceptable);

            perfil.Fusionar(partida);

            Assert.AreEqual(1, perfil.PorObjetivo["OA-A"].Correctas);
            Assert.AreEqual(1, perfil.PorObjetivo["OA-A"].Incorrectas);
            Assert.AreEqual(1, perfil.PorObjetivo["OA-B"].Aceptables);
            Assert.AreEqual(1, partida.PorObjetivo["OA-A"].Total, "Fusionar no modifica la partida de origen");
        }

        [Test]
        public void Sin_decisiones_incorrectas_la_culpa_es_del_sistema() {
            var t = new DecisionTrace();
            t.Registrar(Entrada(1, Veredictos.Correcta));
            CollectionAssert.AreEqual(new[] { CadenaCausal.SinDecisionesIncorrectas }, CadenaCausal.Construir(t));
        }

        [Test]
        public void La_cadena_causal_toma_las_cinco_incorrectas_mas_recientes_en_orden_inverso() {
            var t = new DecisionTrace();
            for (var dia = 1; dia <= 7; dia++) t.Registrar(Entrada(dia, Veredictos.Incorrecta, "E" + dia));
            t.Registrar(Entrada(8, Veredictos.Correcta, "bien"));

            var cadena = CadenaCausal.Construir(t);

            StringAssert.Contains("E7", cadena[0]);
            StringAssert.Contains("E3", cadena[4]);
            Assert.IsFalse(cadena.Any(l => l.Contains("E2") || l.Contains("bien")));
            StringAssert.Contains("en el día 3, 4 días antes de «E7»", cadena[5]);
        }

        [Test]
        public void Con_una_sola_incorrecta_la_raiz_es_ella_misma() {
            var t = new DecisionTrace();
            t.Registrar(Entrada(2, Veredictos.Incorrecta, "Lecturas duplicadas", "Parchear el síntoma"));
            var cadena = CadenaCausal.Construir(t);
            Assert.AreEqual(2, cadena.Count);
            StringAssert.Contains("«Parchear el síntoma»", cadena[0]);
            StringAssert.Contains("en el día 2.", cadena[1]);
        }

        [Test]
        public void Se_puede_acertar_por_el_motivo_equivocado() {
            var trampa = MethodologyReport.Construir(Scrum(), "es_la_de_moda", 80, new Dictionary<string, double>());
            Assert.IsTrue(trampa.EraAdecuada);
            Assert.IsTrue(trampa.RazonTrampa);
            Assert.IsFalse(trampa.RazonValida);

            var buena = MethodologyReport.Construir(Scrum(), "el_cliente_cambiara_de_opinion", 80, new Dictionary<string, double>());
            Assert.IsTrue(buena.RazonValida);
            Assert.IsFalse(buena.RazonTrampa);
        }

        [TestCase("agil", 80, true)]
        [TestCase("agil", 20, false)]
        [TestCase("tradicional", 20, true)]
        [TestCase("tradicional", 80, false)]
        [TestCase("agil", 50, true)]
        public void No_hay_modelo_mejor_hay_modelo_adecuado(string familia, double volatilidad, bool adecuada) {
            var met = Scrum();
            met.Familia = familia;
            Assert.AreEqual(adecuada, MethodologyReport.Construir(met, null, volatilidad, null).EraAdecuada);
        }

        [Test]
        public void Las_practicas_se_evaluan_contra_las_metricas_reales() {
            var metricas = MethodologyReport.CalcularMetricas(new MetricasFalsas { AccionesRetroElegidas = 3, DiasConHorasExtra = 4 });
            var rep = MethodologyReport.Construir(Scrum(), "el_cliente_cambiara_de_opinion", 80, metricas);

            var retro = rep.Practicas.Single(p => p.Id == "retro");
            Assert.IsTrue(retro.Evaluable);
            Assert.IsTrue(retro.Cumple);
            Assert.AreEqual("mejoraste el proceso", retro.Razon);

            var ritmo = rep.Practicas.Single(p => p.Id == "ritmo");
            Assert.IsFalse(ritmo.Cumple);
            Assert.AreEqual("quemaste al equipo", ritmo.Razon);
        }

        [Test]
        public void Una_metrica_o_comparador_desconocido_no_tumba_el_cierre() {
            var met = Scrum();
            met.Practicas.Add(new PracticaAEvaluar { Id = "x", Metrica = "noExiste", Comparador = ">=" });
            met.Practicas.Add(new PracticaAEvaluar { Id = "y", Metrica = "cambiosAceptados", Comparador = "=>" });
            var rep = MethodologyReport.Construir(met, null, 10, MethodologyReport.CalcularMetricas(new MetricasFalsas()));

            Assert.IsFalse(rep.Practicas.Single(p => p.Id == "x").Evaluable);
            Assert.IsFalse(rep.Practicas.Single(p => p.Id == "y").Evaluable);
        }

        [Test]
        public void Las_nueve_metricas_se_calculan_como_dice_la_especificacion() {
            var m = MethodologyReport.CalcularMetricas(new MetricasFalsas {
                DiasConHorasExtra = 2, CambiosAceptados = 1, AccionesRetroElegidas = 3, VolatilidadReal = 70,
                CoberturaAlCerrarDiseno = 45, VecesExcedioWip = 4,
                SerieWip = new List<double> { 2, 4, 6 },
                SerieLeadTime = new List<double> { 1, 3 },
                CompromisosPorIteracion = new List<double> { 10, 0, 20 },
                EntregadoPorIteracion = new List<double> { 8, 5, 25 }
            });

            Assert.AreEqual(9, m.Count);
            Assert.AreEqual(4.0, m["wipMedio"], Tol);
            Assert.AreEqual(2.0, m["leadTimeMedio"], Tol);
            Assert.AreEqual((20.0 + 25.0) / 2.0, m["desviacionCompromisoPct"], Tol, "la iteración sin compromiso se salta");
            Assert.AreEqual(70.0, m["volatilidadReal"], Tol);
        }

        [Test]
        public void Sin_series_las_medias_son_cero() {
            var m = MethodologyReport.CalcularMetricas(new MetricasFalsas());
            Assert.AreEqual(0.0, m["wipMedio"], Tol);
            Assert.AreEqual(0.0, m["leadTimeMedio"], Tol);
            Assert.AreEqual(0.0, m["desviacionCompromisoPct"], Tol);
        }

        [Test]
        public void La_traza_y_la_competencia_sobreviven_a_un_viaje_por_json() {
            var t = new DecisionTrace();
            t.Registrar(Entrada(4, Veredictos.Aceptable));
            var p = new CompetenceProfile();
            p.Acumular("OA-DIS-01", Veredictos.Correcta);

            var settings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
            var tj = JsonConvert.SerializeObject(t);
            var pj = JsonConvert.SerializeObject(p);

            Assert.AreEqual(tj, JsonConvert.SerializeObject(JsonConvert.DeserializeObject<DecisionTrace>(tj, settings)));
            Assert.AreEqual(pj, JsonConvert.SerializeObject(JsonConvert.DeserializeObject<CompetenceProfile>(pj, settings)));
            StringAssert.DoesNotContain("Puntuacion", pj, "las derivadas no se guardan");
        }
    }
}
