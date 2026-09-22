using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Ordenar;
using Nexus.Core.Minijuegos.Repartir;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// Los dos verbos nuevos (V3 ordenar, V2 repartir) y el V1 generalizado a diagramas. Los evaluadores son
    /// funciones puras: aqui se comprueba que la rubrica sale de lo que el jugador hizo, no de una solucion
    /// escrita a mano.
    /// </summary>
    public class MinijuegosNuevosTests {
        // ================================================================ V3 ordenar

        private static Consecuencia C(string veredicto) {
            return new Consecuencia { Rubrica = new Rubrica { Veredicto = veredicto, Oa = "OA-SCRUM-01", Razon = veredicto } };
        }

        private static MinijuegoDef Backlog() {
            var def = new MinijuegoDef {
                Id = "MJ-T-V3", Verbo = Verbos.Ordenar, ObjetivoAprendizaje = "OA-SCRUM-01",
                Ordenar = new OrdenarCfg {
                    Capacidad = 8, UmbralDeValor = 0.8,
                    Tarjetas = {
                        new Tarjeta { Id = "a", Titulo = "Base", Valor = 5, Esfuerzo = 3 },
                        new Tarjeta { Id = "b", Titulo = "Sobre la base", Valor = 9, Esfuerzo = 3, DependeDe = { "a" } },
                        new Tarjeta { Id = "c", Titulo = "Suelta cara", Valor = 2, Esfuerzo = 5 },
                        new Tarjeta { Id = "d", Titulo = "Suelta barata", Valor = 3, Esfuerzo = 2 }
                    },
                    Respuestas = {
                        { "obedecer", new Respuesta { Texto = "Sí a todo", Consecuencia = C("incorrecta") } },
                        { "rechazar", new Respuesta { Texto = "No", Consecuencia = C("aceptable") } },
                        { "negociar", new Respuesta { Texto = "Elige", Consecuencia = new Consecuencia {
                            Rubrica = new Rubrica { Veredicto = "correcta", Oa = "OA-SCRUM-01", Razon = "negociar" },
                            EfectosDiferidos = { new EfectoDiferido { EnDias = 4, EventoForzado = "EV-BUE-02" } } } } }
                    }
                }
            };
            foreach (var k in new[] { "todos", "parcial", "falsoPositivo", "omitido" }) def.Consecuencias[k] = C(k);
            return def;
        }

        [Test]
        public void El_mejor_valor_posible_respeta_dependencias_y_capacidad() {
            // Cabe 8: a+b (6 de esfuerzo, 14 de valor) + d (2, 3) = 17. b sin a no vale.
            Assert.AreEqual(17, OrdenarEvaluador.MejorValorPosible(Backlog().Ordenar));
        }

        [Test]
        public void Un_buen_orden_es_todos() {
            var r = OrdenarEvaluador.Evaluar(Backlog(), new[] { "a", "b", "d", "c" }, "rechazar");
            Assert.AreEqual(ResultadosDeMinijuego.Todos, r.Resultado);
        }

        [Test]
        public void Poner_algo_antes_de_lo_que_necesita_es_falso_positivo() {
            var r = OrdenarEvaluador.Evaluar(Backlog(), new[] { "b", "a", "d", "c" }, "rechazar");
            Assert.AreEqual(ResultadosDeMinijuego.FalsoPositivo, r.Resultado);
            Assert.IsTrue(r.Detalle.Any(d => d.Contains("depende")), "el detalle tiene que decir cual dependencia se rompio");
        }

        [Test]
        public void Un_orden_valido_que_deja_fuera_el_valor_es_parcial() {
            var r = OrdenarEvaluador.Evaluar(Backlog(), new[] { "c", "a", "b", "d" }, "rechazar");   // entran c y a: 7
            Assert.AreEqual(ResultadosDeMinijuego.Parcial, r.Resultado);
        }

        [Test]
        public void Sin_orden_o_sin_respuesta_es_omitido() {
            Assert.AreEqual(ResultadosDeMinijuego.Omitido, OrdenarEvaluador.Evaluar(Backlog(), new string[0], "negociar").Resultado);
            Assert.AreEqual(ResultadosDeMinijuego.Omitido, OrdenarEvaluador.Evaluar(Backlog(), new[] { "a", "b" }, null).Resultado);
        }

        [Test]
        public void Solo_negociar_agenda_el_evento_del_cliente_que_entiende() {
            var negociar = OrdenarEvaluador.Evaluar(Backlog(), new[] { "a", "b", "d", "c" }, "negociar");
            var obedecer = OrdenarEvaluador.Evaluar(Backlog(), new[] { "a", "b", "d", "c" }, "obedecer");
            Assert.IsTrue(negociar.EfectosDiferidos.Any(e => e.EventoForzado == "EV-BUE-02"));
            Assert.IsFalse(obedecer.EfectosDiferidos.Any(e => e.EventoForzado == "EV-BUE-02"));
            CollectionAssert.Contains(negociar.Hallazgos, "respuesta:negociar");
        }

        [Test]
        public void Las_dependencias_en_ciclo_se_detectan() {
            var t = new List<Tarjeta> {
                new Tarjeta { Id = "x", DependeDe = { "y" } }, new Tarjeta { Id = "y", DependeDe = { "x" } }
            };
            Assert.IsTrue(OrdenarEvaluador.HayCiclo(t));
            Assert.IsFalse(OrdenarEvaluador.HayCiclo(Backlog().Ordenar.Tarjetas));
        }

        // ================================================================ V2 repartir

        private static MinijuegoDef Horas() {
            var def = new MinijuegoDef {
                Id = "MJ-T-V2", Verbo = Verbos.Repartir, ObjetivoAprendizaje = "OA-TEST-01",
                Repartir = new RepartirCfg {
                    Presupuesto = 20, ToleranciaDeEscapes = 1,
                    Depositos = {
                        new Deposito { Id = "u", Nombre = "Unitarias", CostePorDefecto = 2, DefectosOcultos = 4 },
                        new Deposito { Id = "i", Nombre = "Integración", CostePorDefecto = 4, DefectosOcultos = 3 }
                    }
                }
            };
            foreach (var k in new[] { "todos", "parcial", "falsoPositivo", "omitido" }) def.Consecuencias[k] = C(k);
            return def;
        }

        [Test]
        public void Repartir_donde_esta_el_riesgo_es_todos() {
            // unitarias 8 h -> 4 de 4; integracion 12 h -> 3 de 3
            var r = RepartirEvaluador.Evaluar(Horas(), new Dictionary<string, int> { { "u", 8 }, { "i", 12 } });
            Assert.AreEqual(ResultadosDeMinijuego.Todos, r.Resultado);
        }

        [Test]
        public void Sobrecomprar_un_tipo_y_dejar_otro_a_cero_es_falso_positivo() {
            var r = RepartirEvaluador.Evaluar(Horas(), new Dictionary<string, int> { { "u", 20 }, { "i", 0 } });
            Assert.AreEqual(ResultadosDeMinijuego.FalsoPositivo, r.Resultado);
            Assert.IsTrue(r.Detalle.Last().Contains("integración"), "los defectos escapan del tipo en el que no invertiste");
        }

        [Test]
        public void Escapar_demasiados_sin_sobrecomprar_es_parcial() {
            var r = RepartirEvaluador.Evaluar(Horas(), new Dictionary<string, int> { { "u", 4 }, { "i", 4 } });
            Assert.AreEqual(ResultadosDeMinijuego.Parcial, r.Resultado);
        }

        [Test]
        public void No_asignar_nada_es_omitido_y_pasarse_del_presupuesto_se_rechaza() {
            Assert.AreEqual(ResultadosDeMinijuego.Omitido, RepartirEvaluador.Evaluar(Horas(), new Dictionary<string, int>()).Resultado);
            Assert.Throws<InvalidOperationException>(() =>
                RepartirEvaluador.Evaluar(Horas(), new Dictionary<string, int> { { "u", 15 }, { "i", 15 } }));
        }

        // ================================================================ validacion de escenas

        [Test]
        public void Un_backlog_con_ciclo_o_que_cabe_entero_no_valida() {
            var def = Backlog();
            def.Ordenar.Tarjetas[0].DependeDe.Add("b");
            Assert.IsTrue(CatalogoMinijuegos.Validar(def).Any(e => e.Contains("ciclo")));

            var sobrado = Backlog();
            sobrado.Ordenar.Capacidad = 100;
            Assert.IsTrue(CatalogoMinijuegos.Validar(sobrado).Any(e => e.Contains("cabe en la capacidad")));
        }

        [Test]
        public void Un_reparto_que_alcanza_para_todo_no_obliga_a_nada() {
            var def = Horas();
            def.Repartir.Presupuesto = 100;
            Assert.IsTrue(CatalogoMinijuegos.Validar(def).Any(e => e.Contains("alcanza")));
        }

        [Test]
        public void Un_diagrama_cuya_zona_apunta_a_una_pieza_que_no_existe_no_valida() {
            var def = new MinijuegoDef {
                Id = "MJ-T-V1", Verbo = Verbos.Detectar, ObjetivoAprendizaje = "OA-DOC-01",
                PaletaEtiquetas = { "requisito_ambiguo" },
                Zonas = { new Zona { Id = "Z1", Commits = { "fantasma" }, Defecto = "requisito_ambiguo", Explicacion = "…" } }
            };
            def.Artefacto.Elementos.Add(new Elemento { Id = "a", Texto = "A" });
            foreach (var k in new[] { "todos", "parcial", "falsoPositivo", "omitido" }) def.Consecuencias[k] = C(k);
            Assert.IsTrue(CatalogoMinijuegos.Validar(def).Any(e => e.Contains("fantasma")));
        }
    }
}
