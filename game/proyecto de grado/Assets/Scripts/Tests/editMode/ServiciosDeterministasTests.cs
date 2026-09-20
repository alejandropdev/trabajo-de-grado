using System;
using System.Collections.Generic;
using System.Globalization;
using Nexus.Core.Modelo;
using Nexus.Core.Servicios;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// C10a · Servicios deterministas. Los tres blindan invariantes distintas:
    /// DeterministicRng -> INV-4 (nadie sortea por su cuenta) y la mitad de INV-7 (recargar no cambia nada).
    /// EffectApplier    -> INV-1 (ningun evento escribe un flag) e INV-2 (una sola puerta de escritura).
    /// ConditionEvaluator -> que un JSON de contenido no pueda ejecutar codigo.
    /// </summary>
    public class ServiciosDeterministasTests {
        private const double Tol = 1e-9;

        private sealed class ContextoFalso : IStateContext {
            public readonly Dictionary<string, double> Valores =
                new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, int> UltimaVez = new Dictionary<string, int>(StringComparer.Ordinal);
            public readonly Dictionary<string, int> Veces = new Dictionary<string, int>(StringComparer.Ordinal);
            public int DiaActual = 10;

            public bool TryGetValue(string nombre, out double valor) {
                return Valores.TryGetValue(nombre, out valor);
            }

            public double CallFunction(string nombre, string argumento) {
                if (nombre == "diasDesde") {
                    int dia;
                    return UltimaVez.TryGetValue(argumento, out dia) ? DiaActual - dia : 999;
                }
                if (nombre == "ocurrencias") {
                    int n;
                    return Veces.TryGetValue(argumento, out n) ? n : 0;
                }
                throw new InvalidOperationException("Funcion desconocida: " + nombre);
            }
        }

        private static ContextoFalso Contexto() {
            var c = new ContextoFalso();
            c.Valores["DeudaTecnica"] = 45;
            c.Valores["Cobertura"] = 60;
            c.Valores["Cansancio"] = 20;
            c.Valores["diaActual"] = 10;
            return c;
        }

        // ============================================================ DeterministicRng

        [Test]
        public void La_misma_semilla_produce_la_misma_secuencia() {
            var a = new DeterministicRng(4417);
            var b = new DeterministicRng(4417);
            for (var i = 0; i < 200; i++)
                Assert.AreEqual(a.NextDouble(), b.NextDouble(), 0.0,
                                "dos alumnos con la misma semilla deben jugar el mismo escenario");
        }

        [Test]
        public void Semillas_contiguas_producen_secuencias_distintas() {
            var a = new DeterministicRng(4417);
            var b = new DeterministicRng(4418);
            var iguales = 0;
            for (var i = 0; i < 50; i++)
                if (Math.Abs(a.NextDouble() - b.NextDouble()) < 1e-12) iguales++;
            Assert.AreEqual(0, iguales, "sin avalancha, 4417 y 4418 darian partidas casi identicas");
        }

        [Test]
        public void Restaurar_quema_las_tiradas_y_sigue_exactamente_por_donde_iba() {
            var original = new DeterministicRng(4417);
            for (var i = 0; i < 5; i++) original.NextDouble();
            original.Next(10);
            original.RuletaPonderada(new double[] { 1, 2, 3 });

            var consumos = original.Consumos;
            var esperado = new List<double>();
            for (var i = 0; i < 5; i++) esperado.Add(original.NextDouble());

            var restaurado = DeterministicRng.Restaurar(4417, consumos);

            Assert.AreEqual(7, consumos, "5 dobles + 1 entero + 1 ruleta = 7 tiradas");
            Assert.AreEqual(consumos, restaurado.Consumos);
            for (var i = 0; i < 5; i++)
                Assert.AreEqual(esperado[i], restaurado.NextDouble(), 0.0,
                                "recargar no puede cambiar la partida (INV-7)");
        }

        [Test]
        public void Next_y_NextDouble_comparten_el_mismo_contador() {
            var rng = new DeterministicRng(1);
            Assert.AreEqual(0, rng.Consumos);
            rng.NextDouble();
            Assert.AreEqual(1, rng.Consumos);
            rng.Next(5);
            Assert.AreEqual(2, rng.Consumos, "si Next(int) tuviera su propio camino, restaurar dejaria de funcionar");
        }

        [Test]
        public void NextDouble_siempre_cae_en_el_intervalo_cero_uno() {
            var rng = new DeterministicRng(99);
            for (var i = 0; i < 5000; i++) {
                var v = rng.NextDouble();
                Assert.GreaterOrEqual(v, 0.0);
                Assert.Less(v, 1.0);
            }
        }

        [Test]
        public void Next_respeta_el_maximo_exclusivo_y_cubre_todo_el_rango() {
            var rng = new DeterministicRng(7);
            var vistos = new HashSet<int>();
            for (var i = 0; i < 3000; i++) {
                var v = rng.Next(6);
                Assert.GreaterOrEqual(v, 0);
                Assert.Less(v, 6);
                vistos.Add(v);
            }
            Assert.AreEqual(6, vistos.Count, "los seis valores deben poder salir");
        }

        [Test]
        public void RuletaPonderada_gasta_una_sola_tirada_sea_cual_sea_el_tamano() {
            var corta = new DeterministicRng(3);
            corta.RuletaPonderada(new double[] { 1, 1 });
            Assert.AreEqual(1, corta.Consumos);

            var larga = new DeterministicRng(3);
            var pesos = new double[200];
            for (var i = 0; i < pesos.Length; i++) pesos[i] = 1;
            larga.RuletaPonderada(pesos);
            Assert.AreEqual(1, larga.Consumos,
                            "si el coste dependiera del catalogo, añadir un evento cambiaria las partidas guardadas");
        }

        [Test]
        public void RuletaPonderada_devuelve_menos_uno_cuando_no_hay_nada_que_elegir() {
            var rng = new DeterministicRng(3);
            Assert.AreEqual(-1, rng.RuletaPonderada(null));
            Assert.AreEqual(-1, rng.RuletaPonderada(new double[0]));
            Assert.AreEqual(-1, rng.RuletaPonderada(new double[] { 0, 0, -5 }));
            Assert.AreEqual(0, rng.Consumos, "descartar sin sortear no debe gastar tirada");
        }

        [Test]
        public void RuletaPonderada_reparte_en_proporcion_a_los_pesos() {
            var rng = new DeterministicRng(4417);
            var cuenta = new int[2];
            const int tiradas = 20000;
            for (var i = 0; i < tiradas; i++) cuenta[rng.RuletaPonderada(new double[] { 1, 3 })]++;

            Assert.AreEqual(0.25, cuenta[0] / (double)tiradas, 0.02);
            Assert.AreEqual(0.75, cuenta[1] / (double)tiradas, 0.02);
        }

        [Test]
        public void RuletaPonderada_ignora_los_pesos_invalidos() {
            var rng = new DeterministicRng(11);
            for (var i = 0; i < 500; i++) {
                var elegido = rng.RuletaPonderada(new[] { 0.0, double.NaN, 2.0, double.PositiveInfinity, -1.0 });
                Assert.AreEqual(2, elegido, "solo el indice 2 tiene un peso valido");
            }
        }

        [Test]
        public void Los_argumentos_imposibles_del_azar_lanzan() {
            Assert.Throws<ArgumentOutOfRangeException>(() => DeterministicRng.Restaurar(1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DeterministicRng(1).Next(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DeterministicRng(1).Next(-3));
        }

        // ============================================================ ConditionEvaluator

        [Test]
        public void Lee_una_comparacion_simple() {
            var ctx = Contexto();
            Assert.IsTrue(ConditionEvaluator.Evaluar("DeudaTecnica > 40", ctx));
            Assert.IsFalse(ConditionEvaluator.Evaluar("DeudaTecnica > 50", ctx));
            Assert.IsTrue(ConditionEvaluator.Evaluar("Cobertura <= 60", ctx));
            Assert.IsTrue(ConditionEvaluator.Evaluar("DeudaTecnica == 45", ctx));
            Assert.IsTrue(ConditionEvaluator.Evaluar("DeudaTecnica != 40", ctx));
        }

        [Test]
        public void Respeta_la_precedencia_y_los_parentesis() {
            var ctx = Contexto();
            Assert.AreEqual(14.0, ConditionEvaluator.EvaluarNumerico("2 + 3 * 4", ctx), Tol);
            Assert.AreEqual(20.0, ConditionEvaluator.EvaluarNumerico("(2 + 3) * 4", ctx), Tol);
            Assert.AreEqual(-5.0, ConditionEvaluator.EvaluarNumerico("-5", ctx), Tol);
            Assert.AreEqual(5.0, ConditionEvaluator.EvaluarNumerico("-(-5)", ctx), Tol);
            Assert.IsTrue(ConditionEvaluator.Evaluar("DeudaTecnica + Cansancio > 60", ctx));
        }

        [Test]
        public void Dividir_por_cero_da_cero_y_no_tumba_la_partida() {
            var ctx = Contexto();
            ctx.Valores["Velocidad"] = 0;
            Assert.AreEqual(0.0, ConditionEvaluator.EvaluarNumerico("10 / 0", ctx), Tol);
            Assert.AreEqual(0.0, ConditionEvaluator.EvaluarNumerico("Cobertura / Velocidad", ctx), Tol);
        }

        [Test]
        public void Las_funciones_reciben_su_argumento_entrecomillado() {
            var ctx = Contexto();
            ctx.UltimaVez["EV-TEC-02"] = 3;
            ctx.Veces["EV-TEC-02"] = 2;

            Assert.AreEqual(7.0, ConditionEvaluator.EvaluarNumerico("diasDesde('EV-TEC-02')", ctx), Tol);
            Assert.AreEqual(2.0, ConditionEvaluator.EvaluarNumerico("ocurrencias('EV-TEC-02')", ctx), Tol);
            Assert.AreEqual(999.0, ConditionEvaluator.EvaluarNumerico("diasDesde('EV-NUNCA')", ctx), Tol,
                            "un evento que nunca ocurrio debe hacer cierto 'diasDesde(x) > N'");
            Assert.IsTrue(ConditionEvaluator.Evaluar("diasDesde('EV-NUNCA') > 10", ctx));
            Assert.AreEqual(7.0, ConditionEvaluator.EvaluarNumerico("diasDesde(\"EV-TEC-02\")", ctx), Tol);
        }

        [Test]
        public void Una_variable_desconocida_lanza_nombrandola() {
            var ex = Assert.Throws<ExpresionInvalidaException>(
                () => ConditionEvaluator.Evaluar("DeudaMoral > 3", Contexto()));
            StringAssert.Contains("DeudaMoral", ex.Message);
        }

        [Test]
        public void Un_igual_simple_avisa_de_que_se_escribe_doble() {
            var ex = Assert.Throws<ExpresionInvalidaException>(
                () => ConditionEvaluator.Evaluar("DeudaTecnica = 45", Contexto()));
            StringAssert.Contains("==", ex.Message);
        }

        [Test]
        public void Un_parentesis_sin_cerrar_lanza_diciendo_donde() {
            var ex = Assert.Throws<ExpresionInvalidaException>(
                () => ConditionEvaluator.Evaluar("(DeudaTecnica > 40", Contexto()));
            StringAssert.Contains("parentesis", ex.Message);
            StringAssert.Contains("posicion", ex.Message);
        }

        [Test]
        public void Los_operadores_booleanos_no_existen_porque_cada_condicion_va_aparte() {
            Assert.Throws<ExpresionInvalidaException>(
                () => ConditionEvaluator.Evaluar("DeudaTecnica > 40 && Cobertura < 70", Contexto()));
        }

        [Test]
        public void El_evaluador_no_puede_ejecutar_codigo() {
            var ctx = Contexto();
            foreach (var intento in new[] {
                "System.IO.File.Delete('x')",
                "typeof(int)",
                "1; DeudaTecnica",
                "$(whoami)"
            }) {
                Assert.Throws<ExpresionInvalidaException>(() => ConditionEvaluator.Evaluar(intento, ctx), intento);
            }
        }

        [Test]
        public void Los_decimales_se_leen_con_punto_en_cualquier_cultura() {
            CultureInfo espanola;
            try {
                espanola = new CultureInfo("es-ES");
            } catch (CultureNotFoundException) {
                Assert.Ignore("Esta maquina no tiene la cultura es-ES instalada.");
                return;
            }

            var previa = CultureInfo.CurrentCulture;
            try {
                CultureInfo.CurrentCulture = espanola;
                Assert.AreEqual(0.85, ConditionEvaluator.EvaluarNumerico("0.85", Contexto()), Tol,
                                "con la cultura del sistema, 0.85 se leeria como 85");
            } finally {
                CultureInfo.CurrentCulture = previa;
            }
        }

        [Test]
        public void EvaluarTodas_exige_que_se_cumplan_todas_y_una_lista_vacia_pasa() {
            var ctx = Contexto();
            Assert.IsTrue(ConditionEvaluator.EvaluarTodas(new[] { "DeudaTecnica > 40", "Cobertura < 70" }, ctx));
            Assert.IsFalse(ConditionEvaluator.EvaluarTodas(new[] { "DeudaTecnica > 40", "Cobertura > 70" }, ctx));
            Assert.IsTrue(ConditionEvaluator.EvaluarTodas(null, ctx), "sin precondiciones no es lo mismo que imposible");
            Assert.IsTrue(ConditionEvaluator.EvaluarTodas(new string[0], ctx));
            Assert.IsTrue(ConditionEvaluator.Evaluar("   ", ctx));
        }

        [Test]
        public void Una_expresion_sin_comparacion_se_lee_como_distinto_de_cero() {
            var ctx = Contexto();
            Assert.IsTrue(ConditionEvaluator.Evaluar("DeudaTecnica", ctx));
            Assert.IsFalse(ConditionEvaluator.Evaluar("0", ctx));
            Assert.IsTrue(ConditionEvaluator.Evaluar("1", ctx));
        }

        // ============================================================ EffectApplier

        private static WorldState Estado() {
            var w = new WorldState();
            w.Set("DeudaTecnica", 20);
            w.Set("Cobertura", 50);
            w.Set("Documentacion", 40);
            w.Set("Dinero", 18000);
            return w;
        }

        [Test]
        public void Un_delta_absoluto_suma_y_un_porcentaje_se_aplica_sobre_el_actual() {
            var w = Estado();
            EffectApplier.Aplicar(w, new Dictionary<string, object> {
                { "DeudaTecnica", 12L },        // Newtonsoft entrega los enteros del JSON como long
                { "Cobertura", "-15%" },
                { "Dinero", -2500.5 }
            });

            Assert.AreEqual(32.0, w.DeudaTecnica, Tol);
            Assert.AreEqual(50.0 * 0.85, w.Cobertura, Tol);
            Assert.AreEqual(15499.5, w.Dinero, Tol);
        }

        [Test]
        public void Los_porcentajes_sobre_VelocidadMod_componen() {
            var w = Estado();
            var mas10 = new Dictionary<string, object> { { "VelocidadMod", "+10%" } };
            EffectApplier.Aplicar(w, mas10);
            EffectApplier.Aplicar(w, mas10);

            Assert.AreEqual(1.21, w.VelocidadMod, Tol, "dos subidas del 10 % dan 1.21, no 1.20");

            EffectApplier.Aplicar(w, new Dictionary<string, object> { { "VelocidadMod", "-25%" } });
            Assert.AreEqual(1.21 * 0.75, w.VelocidadMod, Tol);
        }

        [Test]
        public void El_multiplicador_escala_tanto_el_delta_como_el_porcentaje() {
            var absoluto = Estado();
            EffectApplier.Aplicar(absoluto, new Dictionary<string, object> { { "DeudaTecnica", 10 } }, 2.0);
            Assert.AreEqual(40.0, absoluto.DeudaTecnica, Tol);

            var porcentual = Estado();
            EffectApplier.Aplicar(porcentual, new Dictionary<string, object> { { "Cobertura", "-10%" } }, 2.0);
            Assert.AreEqual(50.0 * 0.8, porcentual.Cobertura, Tol);
        }

        [Test]
        public void Ningun_efecto_puede_escribir_un_flag_narrativo() {
            var w = Estado();
            foreach (var clave in new[] { "FLG_DEUDA_MORAL", "flg_salud", "  FLG_EVIDENCIA" }) {
                var ex = Assert.Throws<InvalidOperationException>(
                    () => EffectApplier.Aplicar(w, new Dictionary<string, object> { { clave, 1 } }));
                StringAssert.Contains("INV-1", ex.Message);
            }

            var validando = Assert.Throws<InvalidOperationException>(
                () => EffectApplier.Validar(new Dictionary<string, object> { { "FLG_PACTO", 1 } }, "EV-TEC-014 opcion B"));
            StringAssert.Contains("EV-TEC-014 opcion B", validando.Message, "el mensaje debe decir donde esta el fallo");
        }

        [Test]
        public void Un_stock_inexistente_se_rechaza_listando_los_validos() {
            var ex = Assert.Throws<InvalidOperationException>(
                () => EffectApplier.Aplicar(Estado(), new Dictionary<string, object> { { "DeudaMoral", 5 } }));
            StringAssert.Contains("DeudaMoral", ex.Message);
            StringAssert.Contains("DeudaTecnica", ex.Message);
        }

        [Test]
        public void La_acotacion_del_WorldState_se_aplica_al_escribir_un_efecto() {
            var w = Estado();
            EffectApplier.Aplicar(w, new Dictionary<string, object> { { "Cobertura", -500 }, { "DeudaTecnica", 500 } });
            Assert.AreEqual(0.0, w.Cobertura, Tol);
            Assert.AreEqual(100.0, w.DeudaTecnica, Tol);
        }

        [Test]
        public void Previsualizar_no_toca_el_estado_y_dice_la_verdad_cuando_el_stock_topa() {
            var w = Estado();
            w.Set("Cobertura", 2);

            var deltas = EffectApplier.Previsualizar(w, new Dictionary<string, object> {
                { "DeudaTecnica", 8 },
                { "Cobertura", -15 }
            });

            Assert.AreEqual(2.0, w.Cobertura, Tol, "previsualizar no puede mutar nada");
            Assert.AreEqual(20.0, w.DeudaTecnica, Tol);
            Assert.AreEqual(8.0, deltas["DeudaTecnica"], Tol);
            Assert.AreEqual(-2.0, deltas["Cobertura"], Tol, "el delta honesto es -2, no -15: la cobertura topa en 0");
        }

        [Test]
        public void Validar_caza_un_valor_ilegible_sin_aplicar_nada() {
            var ex = Assert.Throws<InvalidOperationException>(
                () => EffectApplier.Validar(new Dictionary<string, object> { { "Cobertura", "un poco menos" } },
                                            "MJ-F2-02"));
            StringAssert.Contains("MJ-F2-02", ex.Message);
            StringAssert.Contains("Cobertura", ex.Message);

            Assert.DoesNotThrow(() => EffectApplier.Validar(new Dictionary<string, object> {
                { "DeudaTecnica", 12L }, { "Cobertura", "-15%" }, { "Dinero", -2500.5 }
            }, "ok"));
            Assert.DoesNotThrow(() => EffectApplier.Validar(null, "vacio"));
        }

        [Test]
        public void ToDouble_entiende_lo_que_entrega_Newtonsoft() {
            Assert.AreEqual(12.0, EffectApplier.ToDouble(12L), Tol);
            Assert.AreEqual(12.0, EffectApplier.ToDouble(12), Tol);
            Assert.AreEqual(-8.5, EffectApplier.ToDouble(-8.5), Tol);
            Assert.AreEqual(-8.5, EffectApplier.ToDouble("-8.5"), Tol);
            Assert.AreEqual(3.0, EffectApplier.ToDouble(3.0f), Tol);
            Assert.Throws<InvalidOperationException>(() => EffectApplier.ToDouble(null));
            Assert.Throws<InvalidOperationException>(() => EffectApplier.ToDouble("mucho"));
        }

        [Test]
        public void El_overload_de_float_trata_todo_como_delta_absoluto() {
            var w = Estado();
            EffectApplier.Aplicar(w, new Dictionary<string, float> { { "DeudaTecnica", 5f }, { "MoralEquipo", -3f } });
            Assert.AreEqual(25.0, w.DeudaTecnica, Tol);
            Assert.AreEqual(57.0, w.MoralEquipo, Tol);
        }
    }
}
