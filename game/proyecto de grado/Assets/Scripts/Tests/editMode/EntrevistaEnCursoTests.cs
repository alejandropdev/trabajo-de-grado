using Newtonsoft.Json;
using NUnit.Framework;
using Nexus.Core;
using Nexus.Core.Evaluacion;

namespace Nexus.Tests {
    public class EntrevistaEnCursoTests {
        [Test]
        public void Quien_cierra_en_la_pregunta_siete_vuelve_a_la_octava() {
            EntrevistaEnCurso e = null;
            for (var i = 0; i < 7; i++) e = EntrevistaEnCurso.Anotar(e, i + 1, "a", i + 1);

            var perfil = new PlayerProfile { entrevistaEnCurso = e };
            var recargado = JsonConvert.DeserializeObject<PlayerProfile>(JsonConvert.SerializeObject(perfil));

            Assert.AreEqual(7, EntrevistaEnCurso.IndiceDeInicio(recargado.entrevistaEnCurso, 15), "la octava, en base 0");
            Assert.AreEqual(7, recargado.entrevistaEnCurso.respuestas.Count);
        }

        [Test]
        public void No_lo_se_tambien_se_recuerda() {
            var e = EntrevistaEnCurso.Anotar(null, 3, null, 3);
            var recargada = JsonConvert.DeserializeObject<EntrevistaEnCurso>(JsonConvert.SerializeObject(e));
            Assert.IsTrue(recargada.respuestas.ContainsKey(3));
            Assert.IsNull(recargada.respuestas[3]);
        }

        [Test]
        public void Sin_entrevista_guardada_se_empieza_por_la_primera() {
            Assert.AreEqual(0, EntrevistaEnCurso.IndiceDeInicio(null, 15));
            Assert.IsFalse(EntrevistaEnCurso.HayUnaAMedias(null));
            Assert.IsFalse(EntrevistaEnCurso.HayUnaAMedias(new EntrevistaEnCurso()));
        }

        [Test]
        public void Un_indice_fuera_de_la_prueba_se_acota() {
            Assert.AreEqual(14, EntrevistaEnCurso.IndiceDeInicio(new EntrevistaEnCurso { indice = 40 }, 15));
            Assert.AreEqual(0, EntrevistaEnCurso.IndiceDeInicio(new EntrevistaEnCurso { indice = -2 }, 15));
        }

        [Test]
        public void Un_perfil_viejo_sin_el_campo_se_abre_igual() {
            var perfil = JsonConvert.DeserializeObject<PlayerProfile>("{\"idPerfil\":\"p1\",\"preTestHecho\":false}");
            Assert.IsNull(perfil.entrevistaEnCurso);
            Assert.IsNotNull(perfil.recetasVistas);
        }
    }
}
