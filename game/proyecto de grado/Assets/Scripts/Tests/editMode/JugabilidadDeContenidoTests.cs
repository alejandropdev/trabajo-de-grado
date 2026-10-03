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
        public void Las_horas_de_pruebas_tienen_una_sola_combinacion_ganadora(string id) {
            // Una sola: el reto es encontrarla razonando con las pistas, no probar al azar.
            Assert.AreEqual(1, RepartirEvaluador.RepartosGanadores(Escena(id).Repartir).Count);
        }

        [Test]
        public void El_validador_rechaza_un_reparto_imposible() {
            var def = Escena("MJ-OF-N1-PRUEBAS");
            def.Repartir.Presupuesto = 30;   // lo que habia antes: 37 h hacian falta con tolerancia 1
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
