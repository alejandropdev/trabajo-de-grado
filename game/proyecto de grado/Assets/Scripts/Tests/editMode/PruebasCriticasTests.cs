using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Repartir;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// Las pruebas que no se pueden saltar (ronda 6: «parece como si hacer pruebas de extremo a extremo fuese
    /// inútil»). Una pila critica pierde con un solo error escapado, por cara que sea; y hay un reto en el que el
    /// reparto que trae el equipo ya esta bien y lo correcto es entregarlo sin tocar.
    /// </summary>
    public class PruebasCriticasTests {
        private static string Raiz() {
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            return !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
        }

        private static MinijuegoDef Minijuego(string id) {
            return CatalogoMinijuegos.Parsear(File.ReadAllText(Path.Combine(Raiz(), "minijuegos", id + ".json")));
        }

        private static readonly string[] Repartos = { "MJ-F2-08", "MJ-F2-09", "MJ-OF-N0-PRUEBAS", "MJ-OF-N1-PRUEBAS" };

        [Test]
        public void Hay_retos_con_una_pila_que_no_puede_fallar_y_otros_sin_ella() {
            var conCritica = Repartos.Where(id => Minijuego(id).Repartir.Depositos.Any(d => d.Critico)).ToList();
            Assert.GreaterOrEqual(conCritica.Count, 2, "las pruebas caras tienen que ser imprescindibles en algún reto");
            Assert.Less(conCritica.Count, Repartos.Length, "y en alguno tienen que poder dejarse: la respuesta no es siempre la misma");
            foreach (var id in conCritica)
                foreach (var d in Minijuego(id).Repartir.Depositos.Where(x => x.Critico)) {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(d.PorQueEsCritico), id + "/" + d.Id);
                    Assert.GreaterOrEqual(d.DefectosOcultos, 1, id + "/" + d.Id + ": si no hay nada que atrapar, no se nota que era crítica");
                    Assert.GreaterOrEqual(d.EstimadoMin, 1, id + "/" + d.Id + ": el jugador tiene que ver que ahí hay al menos uno");
                }
        }

        [TestCase("MJ-F2-08")]
        [TestCase("MJ-OF-N1-PRUEBAS")]
        [TestCase("MJ-F2-09")]
        public void Dejar_sin_horas_la_pila_critica_pierde_aunque_se_escape_un_solo_error(string id) {
            var def = Minijuego(id);
            var cfg = def.Repartir;
            var critica = cfg.Depositos.First(d => d.Critico);
            // Lo de antes: cubrir lo barato y renunciar a lo mas caro. Ahora eso pierde.
            var reparto = cfg.Depositos.ToDictionary(d => d.Id, d => 0);
            var libres = cfg.Presupuesto;
            foreach (var d in cfg.Depositos.Where(x => !x.Critico).OrderBy(x => x.CostePorDefecto)) {
                var horas = Math.Min(libres, RepartirEvaluador.HorasParaCubrir(cfg, d, RepartirEvaluador.MaximoEsperado(d)));
                reparto[d.Id] = horas - horas % Math.Max(1, cfg.Paso);
                libres -= reparto[d.Id];
            }
            Assert.AreEqual(0, reparto[critica.Id]);
            var r = RepartirEvaluador.Evaluar(def, reparto);
            Assert.AreNotEqual(ResultadosDeMinijuego.Todos, r.Resultado, id);
            Assert.IsTrue(r.Detalle.Any(l => l.Contains("no podía fallar")), id + ": tiene que decir por qué se perdió");
        }

        [TestCase("MJ-F2-08")]
        [TestCase("MJ-OF-N1-PRUEBAS")]
        [TestCase("MJ-F2-09")]
        public void El_reparto_prudente_cubre_la_pila_critica_y_gana(string id) {
            var def = Minijuego(id);
            var cfg = def.Repartir;
            var prudente = RepartirEvaluador.RepartoPrudente(cfg);
            Assert.LessOrEqual(prudente.Values.Sum(), cfg.Presupuesto);
            foreach (var d in cfg.Depositos.Where(x => x.Critico))
                Assert.AreEqual(RepartirEvaluador.HorasParaCubrir(cfg, d, RepartirEvaluador.MaximoEsperado(d)), prudente[d.Id], id + ": a lo crítico no se renuncia");
            Assert.IsFalse(RepartirEvaluador.PuedeEscaparAlgoCritico(cfg, prudente, true));
            Assert.AreEqual(ResultadosDeMinijuego.Todos, RepartirEvaluador.Evaluar(def, prudente).Resultado, id);
            foreach (var ganador in RepartirEvaluador.RepartosGanadores(cfg))
                foreach (var d in cfg.Depositos.Where(x => x.Critico))
                    Assert.GreaterOrEqual(ganador[d.Id], d.CostePorDefecto * d.DefectosOcultos, id + ": ningún reparto gana dejando escapar lo crítico");
        }

        [Test]
        public void Hay_un_reto_en_que_el_reparto_que_trae_el_equipo_esta_bien_y_se_entrega_tal_cual() {
            var def = Minijuego("MJ-F2-09");
            var cfg = def.Repartir;
            Assert.IsNotNull(cfg.Propuesta, "el reto empieza con el reparto de Camila ya puesto");
            Assert.IsFalse(string.IsNullOrEmpty(cfg.QuienPropone));
            Assert.LessOrEqual(cfg.Propuesta.Values.Sum(), cfg.Presupuesto);
            Assert.AreEqual(ResultadosDeMinijuego.Todos, RepartirEvaluador.Evaluar(def, cfg.Propuesta).Resultado, "aceptarlo sin tocar es lo correcto");
            Assert.LessOrEqual(RepartirEvaluador.EscapesEsperados(cfg, cfg.Propuesta, true), cfg.ToleranciaDeEscapes, "y gana aunque haya lo máximo esperado");

            // Y «arreglarlo» quitándole las horas a lo caro lo estropea: revisar no siempre es cambiar.
            var tocado = new Dictionary<string, int>(cfg.Propuesta);
            var critica = cfg.Depositos.First(d => d.Critico);
            var barata = cfg.Depositos.OrderBy(d => d.CostePorDefecto).First();
            tocado[barata.Id] += tocado[critica.Id];
            tocado[critica.Id] = 0;
            Assert.AreNotEqual(ResultadosDeMinijuego.Todos, RepartirEvaluador.Evaluar(def, tocado).Resultado);
        }

        [Test]
        public void El_reto_del_reparto_propuesto_esta_en_el_indice_del_nivel_1() {
            var indice = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(Raiz(), "minijuegos", "indice.json")));
            var entrada = indice["minijuegos"].FirstOrDefault(m => (string)m["id"] == "MJ-F2-09");
            Assert.IsNotNull(entrada);
            CollectionAssert.Contains(entrada["soloNiveles"].Values<string>().ToList(), "nivel-01");
        }
    }
}
