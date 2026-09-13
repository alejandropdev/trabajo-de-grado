using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Nexus.Core.Simulacion;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// C2 · Simulacion continua. Blindan las no linealidades, que son contenido pedagogico:
    /// si alguien "simplifica" una curva para balancear, estos tests fallan.
    /// </summary>
    public class SimulacionContinuaTests {
        private const double Tol = 1e-9;

        /// <summary>Estado falso con las reglas de acotacion de WorldState (§4.2.1).</summary>
        private sealed class EstadoFalso : IEstadoSimulable {
            private static readonly HashSet<string> Acotados = new HashSet<string> {
                "DeudaTecnica", "Cobertura", "Documentacion", "MoralEquipo", "Cansancio",
                "Competencia", "SatisfaccionCliente", "Reputacion", "SaludJugador"
            };

            public readonly Dictionary<string, double> Valores = new Dictionary<string, double> {
                { "Dias", 0 }, { "Dinero", -500 }, { "Alcance", 34 }, { "Avance", 0 },
                { "DeudaTecnica", 0 }, { "Cobertura", 50 }, { "Documentacion", 50 },
                { "MoralEquipo", 60 }, { "Cansancio", 10 }, { "Competencia", 50 },
                { "SatisfaccionCliente", 50 }, { "Reputacion", 50 }, { "SaludJugador", 80 },
                { "VelocidadMod", 1.0 }
            };

            public bool TryGet(string nombre, out double valor) {
                return Valores.TryGetValue(nombre, out valor);
            }

            public void Set(string nombre, double valor) {
                if (Acotados.Contains(nombre)) valor = Math.Max(0, Math.Min(100, valor));
                else if (nombre == "Dias" || nombre == "Alcance" || nombre == "Avance") valor = Math.Max(0, valor);
                else if (nombre == "VelocidadMod") valor = Math.Max(0.1, valor);
                Valores[nombre] = valor;
            }

            public double this[string nombre] {
                get { return Valores[nombre]; }
                set { Valores[nombre] = value; }
            }
        }

        private sealed class ContadoresFalsos : IContadoresDeSimulacion {
            public int DiasSeguidosTrabajando { get; set; }
            public double SobreCompromiso { get; set; }
            public int WipActual { get; set; }
        }

        [Test]
        public void La_deuda_castiga_de_forma_no_lineal() {
            var caida0a25 = ForresterModel.FDeuda(0) - ForresterModel.FDeuda(25);
            var caida50a75 = ForresterModel.FDeuda(50) - ForresterModel.FDeuda(75);
            Assert.Greater(caida50a75, caida0a25, "El tramo 50→75 debe doler más que el 0→25");
        }

        [Test]
        public void La_curva_de_deuda_coincide_con_la_tabla_de_la_especificacion() {
            Assert.AreEqual(1.00, ForresterModel.FDeuda(0), 0.005);
            Assert.AreEqual(0.89, ForresterModel.FDeuda(25), 0.005);
            Assert.AreEqual(0.67, ForresterModel.FDeuda(50), 0.005);
            Assert.AreEqual(0.47, ForresterModel.FDeuda(75), 0.005);
            Assert.AreEqual(0.33, ForresterModel.FDeuda(100), 0.005);
        }

        [Test]
        public void Con_competencia_50_el_factor_es_exactamente_1() {
            Assert.AreEqual(1.0, ForresterModel.FComp(50), Tol);
            Assert.AreEqual(0.6, ForresterModel.FComp(0), Tol);
            Assert.AreEqual(1.4, ForresterModel.FComp(100), Tol);
        }

        [Test]
        public void Calcular_no_muta_el_estado() {
            var w = new EstadoFalso();
            var antes = new Dictionary<string, double>(w.Valores);
            ForresterModel.Calcular(w, new ContadoresFalsos { WipActual = 3 }, new Coeficientes(), 3.0, 1.0);
            CollectionAssert.AreEquivalent(antes, w.Valores);
        }

        [Test]
        public void Calcular_desglosa_el_riesgo_y_aplica_la_ley_de_little() {
            var w = new EstadoFalso();
            var d = ForresterModel.Calcular(w, new ContadoresFalsos { WipActual = 3 }, new Coeficientes(), 3.0, 1.0);

            Assert.AreEqual(0.30 * 10, d.RiesgoPorCansancio, Tol);
            Assert.AreEqual(0.0, d.RiesgoPorDeuda, Tol);
            Assert.AreEqual(0.20 * 50, d.RiesgoPorCobertura, Tol);
            Assert.AreEqual(0.15 * 50, d.RiesgoPorDocumentacion, Tol);
            Assert.AreEqual(3 + 0 + 10 + 7.5, d.RiesgoLatente, Tol);
            Assert.AreEqual(3.0 / Math.Max(0.1, d.Velocidad / 6.0), d.LeadTime, Tol);
            Assert.AreEqual(34.0, d.Burndown, Tol);
            Assert.AreEqual((0.5 * 50 + 0.5 * 50) * 1.0 * 0.8, d.CalidadEntregada, Tol);
        }

        [Test]
        public void Un_dia_en_casa_coincide_con_el_calculo_a_mano() {
            var w = new EstadoFalso();
            var avanceDia = ForresterModel.AvanzarUnDia(w, new ContadoresFalsos(), new Coeficientes(), 3.0, 1.0, false);

            // V = 3 · 1 · 1 · fComp(50)=1 · fMoral(60)=0.8 · fFatiga(10)=0.994 · fDeuda(0)=1
            const double v = 2.3856;
            Assert.AreEqual(v, avanceDia, Tol);
            Assert.AreEqual(v, w["Avance"], Tol);
            Assert.AreEqual(0.8 * 0.8, w["DeudaTecnica"], Tol);
            Assert.AreEqual(6.0, w["Cansancio"], Tol);
            Assert.AreEqual(82.0, w["SaludJugador"], Tol);
            Assert.AreEqual(60.0 + 0.6 - 0.02 * 6.0, w["MoralEquipo"], Tol);
            Assert.AreEqual(50.0 - 0.5 * (v / 3.0), w["Cobertura"], Tol);
            var doc = 50.0 - 0.4 * (v / 3.0);
            Assert.AreEqual(doc, w["Documentacion"], Tol);
            Assert.AreEqual(50.0 + 0.15 * (doc / 100.0), w["Competencia"], Tol);
            Assert.AreEqual(1.0, w["Dias"], Tol);
            Assert.AreEqual(-500.0, w["Dinero"], Tol, "ForresterModel no toca el dinero");
        }

        [Test]
        public void El_overtime_avanza_un_25_por_ciento_mas() {
            var casa = ForresterModel.AvanzarUnDia(new EstadoFalso(), new ContadoresFalsos(), new Coeficientes(), 3.0, 1.0, false);
            var extra = ForresterModel.AvanzarUnDia(new EstadoFalso(), new ContadoresFalsos { DiasSeguidosTrabajando = 1 },
                                                    new Coeficientes(), 3.0, 1.0, true);
            Assert.AreEqual(casa * 1.25, extra, Tol);
        }

        [Test]
        public void El_sobrecompromiso_genera_mas_deuda() {
            var w = new EstadoFalso();
            ForresterModel.AvanzarUnDia(w, new ContadoresFalsos { SobreCompromiso = 2 }, new Coeficientes(), 3.0, 1.0, false);
            Assert.AreEqual(0.8 * (0.8 + 1.2), w["DeudaTecnica"], Tol);
        }

        [Test]
        public void La_cobertura_y_la_documentacion_se_diluyen_con_el_avance() {
            var lento = new EstadoFalso();
            var rapido = new EstadoFalso();
            ForresterModel.AvanzarUnDia(lento, new ContadoresFalsos(), new Coeficientes(), 1.0, 1.0, false);
            ForresterModel.AvanzarUnDia(rapido, new ContadoresFalsos(), new Coeficientes(), 6.0, 1.0, false);
            Assert.Less(rapido["Cobertura"], lento["Cobertura"]);
            Assert.Less(rapido["Documentacion"], lento["Documentacion"]);
        }

        [Test]
        public void Tres_noches_seguidas_cuestan_mas_que_el_triple() {
            var perdidas = new List<double>();
            for (var seguidos = 1; seguidos <= 3; seguidos++) {
                var w = new EstadoFalso();
                ForresterModel.AvanzarUnDia(w, new ContadoresFalsos { DiasSeguidosTrabajando = seguidos },
                                            new Coeficientes(), 3.0, 1.0, true);
                perdidas.Add(80.0 - w["SaludJugador"]);
            }
            Assert.AreEqual(1.5, perdidas[0], Tol);
            Assert.AreEqual(2.3, perdidas[1], Tol);
            Assert.AreEqual(3.1, perdidas[2], Tol);
            Assert.Greater(perdidas[0] + perdidas[1] + perdidas[2], 3 * perdidas[0]);
        }

        [Test]
        public void Los_stocks_se_acotan_al_final_del_dia() {
            var w = new EstadoFalso();
            w["Cansancio"] = 99;
            w["Cobertura"] = 0.1;
            ForresterModel.AvanzarUnDia(w, new ContadoresFalsos { DiasSeguidosTrabajando = 1 }, new Coeficientes(), 30.0, 1.0, true);
            Assert.AreEqual(100.0, w["Cansancio"], Tol);
            Assert.AreEqual(0.0, w["Cobertura"], Tol);
        }

        [Test]
        public void Un_stock_que_falta_lanza_con_su_nombre() {
            var w = new EstadoFalso();
            w.Valores.Remove("SaludJugador");
            var ex = Assert.Throws<InvalidOperationException>(() =>
                ForresterModel.AvanzarUnDia(w, new ContadoresFalsos(), new Coeficientes(), 3.0, 1.0, false));
            StringAssert.Contains("SaludJugador", ex.Message);
        }

        [Test]
        public void La_misma_entrada_produce_el_mismo_dia() {
            var a = new EstadoFalso();
            var b = new EstadoFalso();
            for (var dia = 0; dia < 10; dia++) {
                var he = dia % 3 == 0;
                ForresterModel.AvanzarUnDia(a, new ContadoresFalsos { DiasSeguidosTrabajando = he ? 1 : 0 }, new Coeficientes(), 3.0, 1.1, he);
                ForresterModel.AvanzarUnDia(b, new ContadoresFalsos { DiasSeguidosTrabajando = he ? 1 : 0 }, new Coeficientes(), 3.0, 1.1, he);
            }
            CollectionAssert.AreEquivalent(a.Valores, b.Valores);
        }

        [Test]
        public void Clone_copia_W_en_profundidad() {
            var original = new Coeficientes();
            var copia = original.Clone();
            copia.W[0] = 0.99;
            copia.Kappa = 0.1;
            Assert.AreEqual(0.30, original.W[0], Tol);
            Assert.AreEqual(0.4, original.Kappa, Tol);
        }

        [Test]
        public void MultiplicarUno_kappa_cambia_solo_kappa() {
            var c = new Coeficientes();
            c.MultiplicarUno("kappa", 0.85);
            Assert.AreEqual(0.34, c.Kappa, Tol);
            Assert.AreEqual(0.5, c.Iota, Tol);
        }

        [Test]
        public void MultiplicarPor_aplica_el_bloque_de_la_metodologia_e_ignora_velocidadBase() {
            var c = new Coeficientes();
            c.MultiplicarPor(new Dictionary<string, double> { { "Alpha", 1.5 }, { "gamma", 0.5 }, { "velocidadBase", 1.2 } });
            Assert.AreEqual(1.2, c.Alpha, Tol);
            Assert.AreEqual(0.3, c.Gamma, Tol);
        }

        [Test]
        public void Un_coeficiente_desconocido_lanza() {
            var ex = Assert.Throws<InvalidOperationException>(() => new Coeficientes().MultiplicarUno("omega", 2));
            StringAssert.Contains("omega", ex.Message);
        }

        [Test]
        public void Los_coeficientes_sobreviven_a_un_viaje_por_json() {
            var c = new Coeficientes();
            c.MultiplicarUno("kappa", 0.85);
            c.W[1] = 0.4;
            var settings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
            var json = JsonConvert.SerializeObject(c);
            var vuelta = JsonConvert.DeserializeObject<Coeficientes>(json, settings);
            Assert.AreEqual(json, JsonConvert.SerializeObject(vuelta));
            Assert.AreEqual(4, vuelta.W.Length);
        }
    }
}
