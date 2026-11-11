using System;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Repartir;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// El glosario de tipos de prueba (2026-11-11) que usa el edificio del cliente: cada pila del contenido real tiene el
    /// suyo, y los pisos salen de la base (lo barato y pequeño) a la cima (el recorrido completo).
    /// </summary>
    public class TiposDePruebaTests {
        private static System.Collections.Generic.List<MinijuegoDef> Escenas() {
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            return Directory.GetFiles(Path.Combine(raiz, "minijuegos"), "MJ-*.json")
                            .Select(f => CatalogoMinijuegos.Parsear(File.ReadAllText(f)))
                            .Where(d => Verbos.Normalizar(d.Verbo) == Verbos.Repartir).ToList();
        }

        [Test]
        public void Cada_pila_del_contenido_real_tiene_su_tipo_conocido() {
            var escenas = Escenas();
            Assert.AreEqual(14, escenas.Count);
            foreach (var def in escenas)
                foreach (var d in def.Repartir.Depositos)
                    Assert.IsTrue(TiposDePrueba.EsConocido(d.Id), def.Id + "/" + d.Id + ": sin glosario");
        }

        [Test]
        public void Todo_tipo_dice_que_revisa_que_frena_y_que_no_ve() {
            foreach (var t in TiposDePrueba.Conocidos) {
                Assert.IsFalse(string.IsNullOrEmpty(t.Nombre), t.Id);
                Assert.IsFalse(string.IsNullOrEmpty(t.Revisa), t.Id);
                Assert.IsFalse(string.IsNullOrEmpty(t.Incidencia), t.Id);
                Assert.IsFalse(string.IsNullOrEmpty(t.Defensa), t.Id);
                Assert.IsFalse(string.IsNullOrEmpty(t.NoVe), t.Id);
                Assert.IsFalse(string.IsNullOrEmpty(t.Bicho), t.Id);
                Assert.IsFalse(string.IsNullOrEmpty(t.Color), t.Id);
            }
        }

        [Test]
        public void Los_pisos_van_de_la_base_a_la_cima_y_caben_en_cuatro() {
            foreach (var def in Escenas()) {
                var pisos = TiposDePrueba.Pisos(def.Repartir);
                Assert.AreEqual(def.Repartir.Depositos.Count, pisos.Count, def.Id);
                Assert.LessOrEqual(pisos.Count, 4, def.Id + ": el edificio tiene como mucho cuatro pisos");
                var alturas = pisos.Select(d => TiposDePrueba.Para(d).Piso).ToList();
                CollectionAssert.AreEqual(alturas.OrderBy(a => a).ToList(), alturas, def.Id + ": los pisos no van de abajo arriba");
                if (def.Repartir.Depositos.Any(d => d.Id == "unitarias")) Assert.AreEqual("unitarias", pisos.First().Id, def.Id + ": las unitarias son la base");
                if (def.Repartir.Depositos.Any(d => d.Id == "e2e")) Assert.AreEqual("e2e", pisos.Last().Id, def.Id + ": el recorrido completo es la cima");
            }
        }

        [Test]
        public void Una_pila_desconocida_se_reconoce_por_su_nombre_o_cae_en_un_tipo_generico() {
            var porNombre = TiposDePrueba.Para(new Deposito { Id = "x1", Nombre = "Pruebas unitarias de fechas", CostePorDefecto = 1 });
            Assert.AreEqual("unitarias", porNombre.Id);
            var rara = TiposDePrueba.Para(new Deposito { Id = "magia", Nombre = "Magia negra", CostePorDefecto = 3 });
            Assert.AreEqual("magia", rara.Id);
            Assert.AreEqual("MAGIA NEGRA", rara.Nombre);
            Assert.IsFalse(string.IsNullOrEmpty(rara.Defensa));
        }
    }
}
