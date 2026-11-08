using System;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Evaluacion;
using Nexus.Core.Narrativa;
using Nexus.Core.Tarjetas;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// La entrevista de admision rediseñada (2026-11-07): cada pregunta dibuja su tema en una pizarra y las cartas llegan
    /// de una forma distinta en cada una, para que las quince no se vean iguales.
    /// </summary>
    public class EntrevistaConPizarraTests {
        private static PruebaDeAdmision PruebaReal() {
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            return CatalogLoader.CargarTodo(new CatalogoDeArchivos(raiz)).Admision;
        }

        [Test]
        public void Cada_pregunta_dibuja_un_tema_distinto_que_existe() {
            var prueba = PruebaReal();
            Assert.That(prueba.Preguntas.Count, Is.EqualTo(CorrectorDeAdmision.NumeroDePreguntas));
            foreach (var p in prueba.Preguntas)
                Assert.IsTrue(EscenasDePizarra.Existe(p.Escena), $"Pregunta {p.Numero}: escena '{p.Escena}' sin dibujo.");
            var escenas = prueba.Preguntas.Select(p => p.Escena).ToList();
            Assert.AreEqual(escenas.Count, escenas.Distinct().Count(), "dos preguntas con el mismo dibujo");
        }

        [Test]
        public void Una_escena_que_no_existe_no_pasa_la_validacion() {
            var prueba = PruebaReal();
            prueba.Preguntas[0].Escena = "no-existe";
            Assert.IsTrue(SchemaValidator.ValidarAdmision(prueba).Any(e => e.Contains("no-existe")));
            prueba.Preguntas[0].Escena = null;
            Assert.IsFalse(SchemaValidator.ValidarAdmision(prueba).Any(e => e.Contains("escena")), "sin escena tambien vale");
        }

        [Test]
        public void Al_llegar_cada_carta_queda_en_su_sitio_con_cualquier_entrada() {
            foreach (EntradaDeMano entrada in Enum.GetValues(typeof(EntradaDeMano)))
                for (var i = 0; i < 4; i++) {
                    var d = DisposicionDeMano.Desfase(entrada, i, 4, 1f);
                    Assert.AreEqual(0f, d.X, 1e-4); Assert.AreEqual(0f, d.Y, 1e-4); Assert.AreEqual(0f, d.Angulo, 1e-4);
                }
        }

        [Test]
        public void Las_cuatro_entradas_salen_de_sitios_distintos_y_rotan_por_pregunta() {
            var salidas = Enum.GetValues(typeof(EntradaDeMano)).Cast<EntradaDeMano>()
                              .Select(e => DisposicionDeMano.Desfase(e, 0, 4, 0f))
                              .Select(l => $"{l.X:0}|{l.Y:0}|{l.Angulo:0}").ToList();
            Assert.AreEqual(4, salidas.Distinct().Count());
            var porPregunta = Enumerable.Range(1, 15).Select(DisposicionDeMano.EntradaPara).ToList();
            Assert.AreEqual(4, porPregunta.Distinct().Count(), "en quince preguntas salen las cuatro");
            for (var n = 1; n < 15; n++) Assert.AreNotEqual(porPregunta[n - 1], porPregunta[n], "dos seguidas iguales");
            Assert.AreEqual(EntradaDeMano.DesdeAbajo, default(EntradaDeMano), "la de siempre es la de por defecto");
        }

        [Test]
        public void Desde_los_lados_las_cartas_de_la_izquierda_llegan_por_la_izquierda() {
            Assert.Less(DisposicionDeMano.Desfase(EntradaDeMano.DesdeLosLados, 0, 4, 0f).X, 0f);
            Assert.Greater(DisposicionDeMano.Desfase(EntradaDeMano.DesdeLosLados, 3, 4, 0f).X, 0f);
        }
    }
}
