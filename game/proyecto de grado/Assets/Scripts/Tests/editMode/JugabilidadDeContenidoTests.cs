using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Puente;
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

        // ================================================================ V3 · el puente del backlog (2026-11-10)

        private static IEnumerable<MinijuegoDef> Puentes() { return Escenas(Verbos.Ordenar).Where(PuenteDelBacklog.Aplica); }

        [Test]
        public void Todos_los_backlog_del_contenido_son_puentes() {
            var ordenar = Escenas(Verbos.Ordenar).ToList();
            Assert.AreEqual(13, ordenar.Count);
            foreach (var def in ordenar) Assert.IsTrue(PuenteDelBacklog.Aplica(def), def.Id + ": sigue siendo un backlog de lista plana");
        }

        [Test]
        public void Cada_puente_se_puede_ganar_sin_perder_una_vida() {
            foreach (var def in Puentes()) {
                var estado = new EstadoDelPuente(def.Puente);
                for (var i = 0; i < def.Puente.Pilares.Count; i++) {
                    var solucion = PuenteEvaluador.Solucion(def.Puente.Pilares[i]);
                    for (var k = 0; k < solucion.Count; k++) estado.Mover(i, solucion[k], k);
                }
                var ronda = estado.Verificar();
                Assert.IsTrue(ronda.Completo, def.Id);
                var r = PuenteEvaluador.Evaluar(def, estado);
                Assert.AreEqual(ResultadosDeMinijuego.Todos, r.Resultado, def.Id + ": " + string.Join(" | ", r.Detalle));
            }
        }

        [Test]
        public void Ningun_puente_empieza_ya_resuelto_y_siempre_empieza_igual() {
            foreach (var def in Puentes())
                foreach (var pilar in def.Puente.Pilares)
                    for (var semilla = 0; semilla < 12; semilla++) {
                        var orden = PuenteEvaluador.OrdenInicial(pilar, semilla);
                        CollectionAssert.AreEquivalent(pilar.Historias.Select(h => h.Id), orden, def.Id + ": se pierde o se repite una historia");
                        CollectionAssert.AreEqual(orden, PuenteEvaluador.OrdenInicial(pilar, semilla), def.Id + ": no es determinista");
                        Assert.IsFalse(PuenteEvaluador.RevisarPilar(pilar, orden).Correcto, $"{def.Id}/{pilar.Id} (semilla {semilla}): empieza ya resuelto");
                    }
        }

        [Test]
        public void Dos_errores_en_el_mismo_pilar_pierden_el_puente_y_los_pilares_buenos_se_quedan_hechos() {
            foreach (var def in Puentes()) {
                var estado = new EstadoDelPuente(def.Puente);
                // Pilar 0 bien; el resto, como empieza (mal).
                var solucion = PuenteEvaluador.Solucion(def.Puente.Pilares[0]);
                for (var k = 0; k < solucion.Count; k++) estado.Mover(0, solucion[k], k);
                var uno = estado.Verificar();
                Assert.IsTrue(estado.Hecho[0], def.Id);
                Assert.IsFalse(estado.Perdido, def.Id + ": un error no pierde");
                Assert.IsFalse(uno.Revisiones.First(x => x.Pilar != def.Puente.Pilares[0].Id).Correcto, def.Id);
                var dos = estado.Verificar();
                Assert.IsFalse(dos.Pilares.Contains(0), def.Id + ": un pilar hecho no se vuelve a revisar");
                Assert.IsTrue(estado.Perdido, def.Id + ": el segundo error pierde");
                Assert.AreEqual(ResultadosDeMinijuego.FalsoPositivo, PuenteEvaluador.Evaluar(def, estado).Resultado, def.Id);
            }
        }

        [Test]
        public void El_evento_del_cliente_que_entiende_sale_de_un_puente_perfecto_del_graduado() {
            var def = Escena("MJ-F1-08");
            Assert.IsTrue(def.Consecuencias["todos"].EfectosDiferidos.Any(e => e.EventoForzado == "EV-BUE-02"));
            foreach (var clave in new[] { "parcial", "falsoPositivo", "omitido" })
                Assert.IsFalse(def.Consecuencias[clave].EfectosDiferidos.Any(e => e.EventoForzado == "EV-BUE-02"), clave);
        }
    }
}
