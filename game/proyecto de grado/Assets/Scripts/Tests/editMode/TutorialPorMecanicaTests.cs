using System.Collections.Generic;
using Nexus.Core.Minijuegos;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// El modo guiado sale la PRIMERA vez de cada mecanica, no de cada escena ni siempre en el tutorial (feedback de
    /// la beta: «solo la primera vez que se enfrenta al minijuego»). Tres recorridos por perfil.
    /// </summary>
    public class TutorialPorMecanicaTests {
        [Test]
        public void Un_perfil_nuevo_se_guia_en_las_tres_mecanicas() {
            foreach (var verbo in new[] { Verbos.Detectar, Verbos.Ordenar, Verbos.Repartir }) {
                Assert.IsTrue(TutorialPorMecanica.DebeGuiarse(new List<string>(), verbo), verbo);
                Assert.IsTrue(TutorialPorMecanica.DebeGuiarse(null, verbo), verbo);
            }
        }

        [Test]
        public void Cada_mecanica_se_guia_una_sola_vez_y_no_arrastra_a_las_demas() {
            var guiadas = new List<string> { Verbos.Detectar };
            Assert.IsFalse(TutorialPorMecanica.DebeGuiarse(guiadas, Verbos.Detectar));
            Assert.IsFalse(TutorialPorMecanica.DebeGuiarse(guiadas, "detectar"), "da igual como se escriba el verbo");
            Assert.IsTrue(TutorialPorMecanica.DebeGuiarse(guiadas, Verbos.Ordenar));
            Assert.IsTrue(TutorialPorMecanica.DebeGuiarse(guiadas, Verbos.Repartir));
        }

        [Test]
        public void Un_verbo_desconocido_no_se_guia() {
            Assert.IsFalse(TutorialPorMecanica.DebeGuiarse(new List<string>(), "V9_BAILAR"));
            Assert.IsFalse(TutorialPorMecanica.DebeGuiarse(new List<string>(), null));
        }

        [Test]
        public void Un_perfil_de_antes_no_repite_las_mecanicas_que_ya_jugo() {
            var escenas = new Dictionary<string, string> {
                { "MJ-OF-N0-DIAGRAMA", Verbos.Detectar }, { "MJ-F0-01", Verbos.Detectar }, { "MJ-OF-N0-BACKLOG", "ordenar" }
            };
            var jugados = new List<string> { "MJ-OF-N0-DIAGRAMA", "MJ-F0-01", "MJ-OF-N0-BACKLOG", "MJ-QUE-YA-NO-EXISTE" };
            var mecanicas = TutorialPorMecanica.Migrar(jugados, id => { string v; return escenas.TryGetValue(id, out v) ? v : null; });

            CollectionAssert.AreEqual(new[] { Verbos.Detectar, Verbos.Ordenar }, mecanicas);
            Assert.IsFalse(TutorialPorMecanica.DebeGuiarse(mecanicas, Verbos.Detectar));
            Assert.IsTrue(TutorialPorMecanica.DebeGuiarse(mecanicas, Verbos.Repartir), "la que nunca jugo, si");
        }

        [Test]
        public void Migrar_sin_historial_da_una_lista_vacia_no_nula() {
            Assert.IsEmpty(TutorialPorMecanica.Migrar(null, id => Verbos.Detectar));
            Assert.IsEmpty(TutorialPorMecanica.Migrar(new List<string>(), id => Verbos.Detectar));
        }
    }
}
