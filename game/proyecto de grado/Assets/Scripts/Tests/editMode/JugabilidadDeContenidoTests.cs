using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Ordenar;
using Nexus.Core.Minijuegos.Repartir;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// Que cada minijuego del contenido real se pueda ganar (feedback beta: «en Estimación de pruebas es imposible
    /// tener una combinación ganadora»), y que la respuesta al cliente del backlog se vea en el tablero.
    /// </summary>
    public class JugabilidadDeContenidoTests {
        private static string Carpeta() {
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            return Path.Combine(raiz, "minijuegos");
        }

        private static IEnumerable<MinijuegoDef> Escenas(string verbo) {
            foreach (var archivo in Directory.GetFiles(Carpeta(), "MJ-*.json")) {
                var def = CatalogoMinijuegos.Parsear(File.ReadAllText(archivo));
                if (Verbos.Normalizar(def.Verbo) == verbo) yield return def;
            }
        }

        private static MinijuegoDef Escena(string id) {
            return CatalogoMinijuegos.Parsear(File.ReadAllText(Path.Combine(Carpeta(), id + ".json")));
        }

        // ================================================================ V2 · repartir

        [Test]
        public void Cada_reparto_de_horas_se_puede_ganar_con_los_botones_de_verdad() {
            var escenas = Escenas(Verbos.Repartir).ToList();
            Assert.IsNotEmpty(escenas);
            foreach (var def in escenas) {
                var ganadores = RepartirEvaluador.RepartosGanadores(def.Repartir);
                Assert.IsNotEmpty(ganadores, $"{def.Id}: con {def.Repartir.Presupuesto} h de {def.Repartir.Paso} en {def.Repartir.Paso} no se puede ganar");
                foreach (var g in ganadores) {
                    Assert.IsTrue(g.Values.All(h => h % Math.Max(1, def.Repartir.Paso) == 0), def.Id + ": un ganador fuera de la rejilla de los botones");
                    Assert.AreEqual(ResultadosDeMinijuego.Todos, RepartirEvaluador.Evaluar(def, g).Resultado, def.Id);
                }
            }
        }

        [TestCase("MJ-OF-N0-PRUEBAS")]
        [TestCase("MJ-OF-N1-PRUEBAS")]
        [TestCase("MJ-F2-08")]
        public void Las_horas_de_pruebas_tienen_varias_combinaciones_ganadoras_y_la_prudente_gana(string id) {
            // Antes habia UNA sola entre miles y con los errores escondidos: salia bien por suerte (feedback de la
            // beta). Ahora hay varias, y quien cubre lo maximo que cabe esperar segun las tarjetas gana seguro.
            var def = Escena(id);
            var ganadores = RepartirEvaluador.RepartosGanadores(def.Repartir);
            Assert.GreaterOrEqual(ganadores.Count, 3, id + ": una combinación ganadora casi única es adivinar");

            var prudente = RepartirEvaluador.RepartoPrudente(def.Repartir);
            Assert.LessOrEqual(prudente.Values.Sum(), def.Repartir.Presupuesto, id + ": el reparto prudente no cabe");
            Assert.IsTrue(prudente.Values.All(h => h % Math.Max(1, def.Repartir.Paso) == 0), id + ": el prudente no se puede hacer con los botones");
            Assert.LessOrEqual(RepartirEvaluador.EscapesEsperados(def.Repartir, prudente, true), def.Repartir.ToleranciaDeEscapes,
                               id + ": ni en el peor caso de las pistas puede perder");
            Assert.AreEqual(ResultadosDeMinijuego.Todos, RepartirEvaluador.Evaluar(def, prudente).Resultado, id);
        }

        [Test]
        public void La_pista_de_cada_tipo_de_prueba_es_un_rango_que_no_miente() {
            foreach (var def in Escenas(Verbos.Repartir))
                foreach (var d in def.Repartir.Depositos) {
                    Assert.Greater(d.EstimadoMax, 0, $"{def.Id}/{d.Id}: sin rango a la vista, el reparto no se puede razonar");
                    Assert.That(d.DefectosOcultos, Is.InRange(d.EstimadoMin, d.EstimadoMax), $"{def.Id}/{d.Id}");
                }
        }

        [Test]
        public void El_validador_rechaza_una_pista_falsa_y_un_reparto_que_dependa_de_la_suerte() {
            var def = Escena("MJ-OF-N1-PRUEBAS");
            def.Repartir.Depositos[0].DefectosOcultos = def.Repartir.Depositos[0].EstimadoMax + 1;
            Assert.IsTrue(CatalogoMinijuegos.Validar(def).Any(e => e.Contains("pista seria falsa")));

            def = Escena("MJ-OF-N1-PRUEBAS");
            def.Repartir.Presupuesto -= 2 * def.Repartir.Paso;   // ya no alcanza para ir sobre seguro
            Assert.IsTrue(CatalogoMinijuegos.Validar(def).Any(e => e.Contains("cuestion de suerte")));
        }

        [Test]
        public void Redondear_a_los_botones_no_cuenta_como_horas_de_mas() {
            // Regresion cuesta 3 h por error y los botones van de 2 en 2: cubrir 3 errores obliga a poner 10 h.
            // Antes esa hora de redondeo contaba como «sobrecompra» y podia dar falsoPositivo.
            var def = Escena("MJ-F2-08");
            var r = RepartirEvaluador.Evaluar(def, new Dictionary<string, int> { { "regresion", 10 }, { "unitarias", 10 } });
            Assert.AreEqual(ResultadosDeMinijuego.Parcial, r.Resultado);
        }

        [Test]
        public void El_validador_rechaza_un_reparto_imposible() {
            var def = Escena("MJ-OF-N1-PRUEBAS");
            def.Repartir.Presupuesto = 12;   // ni de lejos: hacen falta 26 h para que escape solo 1
            Assert.IsTrue(CatalogoMinijuegos.Validar(def).Any(e => e.Contains("imposible")));
        }

        [Test]
        public void El_detalle_distingue_no_invertir_de_invertir_poco() {
            var def = Escena("MJ-OF-N0-PRUEBAS");
            var r = RepartirEvaluador.Evaluar(def, new Dictionary<string, int> { { "unitarias", 2 }, { "integracion", 0 }, { "e2e", 4 } });
            Assert.IsTrue(r.Detalle.Any(d => d.Contains("No pusiste horas") && d.Contains("integración")));
            Assert.IsTrue(r.Detalle.Any(d => d.Contains("no las suficientes") && d.Contains("unitarias")));
        }

        // ================================================================ V3 · ordenar

        [Test]
        public void Cada_backlog_se_puede_ganar_con_su_mejor_orden_y_negociando() {
            foreach (var def in Escenas(Verbos.Ordenar)) {
                var mejor = OrdenarEvaluador.MejorOrden(def.Ordenar);
                var orden = mejor.Concat(def.Ordenar.Tarjetas.Select(t => t.Id).Where(id => !mejor.Contains(id))).ToList();
                var r = OrdenarEvaluador.Evaluar(def, orden, "negociar");
                Assert.AreEqual(ResultadosDeMinijuego.Todos, r.Resultado, def.Id + ": " + string.Join(" | ", r.Detalle));
            }
        }

        [Test]
        public void El_backlog_no_empieza_ya_resuelto_y_siempre_empieza_igual() {
            // El JSON trae las tarjetas en el orden bueno: quien no tocaba nada ganaba, y quien ordenaba «por valor»
            // lo rompia. El tablero empieza barajado, en un orden que no gana tal cual.
            foreach (var def in Escenas(Verbos.Ordenar)) {
                var cfg = def.Ordenar;
                var porId = cfg.Tarjetas.ToDictionary(t => t.Id);
                for (var semilla = 0; semilla < 12; semilla++) {
                    var orden = OrdenarEvaluador.OrdenInicial(cfg, semilla);
                    CollectionAssert.AreEquivalent(cfg.Tarjetas.Select(t => t.Id), orden, def.Id + ": se pierde o se repite una tarjeta");
                    CollectionAssert.AreEqual(orden, OrdenarEvaluador.OrdenInicial(cfg, semilla), def.Id + ": no es determinista");
                    var capturado = OrdenarEvaluador.Entran(cfg, orden).Sum(id => porId[id].Valor);
                    var gana = OrdenarEvaluador.Rotas(cfg, orden).Count == 0 &&
                               capturado >= cfg.UmbralDeValor * OrdenarEvaluador.MejorValorPosible(cfg);
                    Assert.IsFalse(gana, $"{def.Id} (semilla {semilla}): el tablero empieza ya resuelto");
                }
            }
        }

        [Test]
        public void Las_dependencias_rotas_que_se_avisan_son_las_que_luego_se_castigan() {
            foreach (var def in Escenas(Verbos.Ordenar)) {
                var mejor = OrdenarEvaluador.MejorOrden(def.Ordenar);
                var orden = mejor.Concat(def.Ordenar.Tarjetas.Select(t => t.Id).Where(id => !mejor.Contains(id))).ToList();
                CollectionAssert.IsEmpty(OrdenarEvaluador.Rotas(def.Ordenar, orden), def.Id + ": el mejor orden no avisa de nada");

                var conDependencia = def.Ordenar.Tarjetas.First(t => t.DependeDe.Count > 0 && mejor.Contains(t.Id));
                orden.Remove(conDependencia.Id);
                orden.Insert(0, conDependencia.Id);   // arriba del todo: antes de lo que necesita
                Assert.IsTrue(OrdenarEvaluador.Rotas(def.Ordenar, orden).Any(p => p.Key == conDependencia.Id), def.Id);
                Assert.AreEqual(ResultadosDeMinijuego.FalsoPositivo, OrdenarEvaluador.Evaluar(def, orden, "rechazar").Resultado, def.Id);
            }
        }

        [Test]
        public void Cada_backlog_dice_que_tarjeta_pide_el_cliente_y_que_le_pasa() {
            foreach (var def in Escenas(Verbos.Ordenar)) {
                Assert.IsNotNull(def.Ordenar.TarjetaPedida, def.Id);
                foreach (var kv in def.Ordenar.Respuestas)
                    Assert.IsFalse(string.IsNullOrEmpty(kv.Value.Replica), $"{def.Id}/{kv.Key}: sin réplica no se ve el efecto de contestar");
            }
        }

        [Test]
        public void Obedecer_sube_la_tarjeta_del_cliente_al_tablero_y_no_puede_salir_todos() {
            var def = Escena("MJ-OF-N0-BACKLOG");
            var mejor = OrdenarEvaluador.MejorOrden(def.Ordenar);
            var orden = mejor.Concat(def.Ordenar.Tarjetas.Select(t => t.Id).Where(id => !mejor.Contains(id))).ToList();

            var tras = OrdenarEvaluador.AplicarRespuesta(def.Ordenar, orden, def.Ordenar.Respuestas["obedecer"]);
            Assert.AreEqual(def.Ordenar.TarjetaPedida, tras[0], "la pedida sube arriba del todo");
            Assert.IsFalse(OrdenarEvaluador.Entran(def.Ordenar, orden).SetEquals(OrdenarEvaluador.Entran(def.Ordenar, tras)),
                           "lo que entra cambia: se ve el efecto");

            var r = OrdenarEvaluador.Evaluar(def, orden, "obedecer");
            Assert.AreNotEqual(ResultadosDeMinijuego.Todos, r.Resultado, "decir que sí a todo nunca es el mejor resultado");
        }

        [Test]
        public void Negociar_en_el_graduado_mete_la_tarjeta_pedida_a_cambio_de_otra() {
            var def = Escena("MJ-F1-08");
            var mejor = OrdenarEvaluador.MejorOrden(def.Ordenar);
            var orden = mejor.Concat(def.Ordenar.Tarjetas.Select(t => t.Id).Where(id => !mejor.Contains(id))).ToList();
            Assert.IsFalse(OrdenarEvaluador.Entran(def.Ordenar, orden).Contains(def.Ordenar.TarjetaPedida));

            var tras = OrdenarEvaluador.AplicarRespuesta(def.Ordenar, orden, def.Ordenar.Respuestas["negociar"]);
            var entran = OrdenarEvaluador.Entran(def.Ordenar, tras);
            Assert.IsTrue(entran.Contains(def.Ordenar.TarjetaPedida), "lo pedido entra");
            Assert.AreEqual(orden.Count, tras.Count, "no se pierde ni se duplica ninguna tarjeta");
            Assert.AreEqual(ResultadosDeMinijuego.Todos, OrdenarEvaluador.Evaluar(def, orden, "negociar").Resultado,
                            "y el orden sigue siendo construible");
        }

        [Test]
        public void El_mejor_orden_respeta_las_dependencias() {
            foreach (var def in Escenas(Verbos.Ordenar)) {
                var mejor = OrdenarEvaluador.MejorOrden(def.Ordenar);
                var porId = def.Ordenar.Tarjetas.ToDictionary(t => t.Id);
                for (var i = 0; i < mejor.Count; i++)
                    foreach (var dep in porId[mejor[i]].DependeDe)
                        Assert.Less(mejor.IndexOf(dep), i, $"{def.Id}: {mejor[i]} va antes que {dep}");
            }
        }
    }
}
