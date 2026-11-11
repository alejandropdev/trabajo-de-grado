using NUnit.Framework;
using Nexus.Core.Audio;

namespace Nexus.Tests {
    public class MezclaDeMusicaTests {
        [TestCase("PantallaDePerfiles", MezclaDeMusica.Nexus, MezclaDeMusica.EnMenus)]
        [TestCase("PantallaDePartidas", MezclaDeMusica.Nexus, MezclaDeMusica.EnMenus)]
        [TestCase("PantallaDeFase1", MezclaDeMusica.Nexus, MezclaDeMusica.EnLaPartida)]
        [TestCase("PantallaDelDia", MezclaDeMusica.Nexus, MezclaDeMusica.EnLaPartida)]
        [TestCase("PantallaDeLecciones", MezclaDeMusica.Nexus, MezclaDeMusica.EnLaPartida)]
        [TestCase("PantallaDeRecoleccion3D", MezclaDeMusica.Accion, MezclaDeMusica.EnElRecorrido)]
        [TestCase("PantallaDeDespliegue", MezclaDeMusica.Accion, MezclaDeMusica.EnElDespliegue)]
        [TestCase("PantallaDeLanzamiento", MezclaDeMusica.Accion, MezclaDeMusica.EnElLanzamiento)]
        public void Cada_momento_tiene_su_tema_y_su_nivel(string pantalla, string pista, float nivel) {
            var m = MezclaDeMusica.Para(pantalla);
            Assert.AreEqual(pista, m.Pista);
            Assert.AreEqual(nivel, m.Nivel, 1e-6);
        }

        [TestCase("PantallaDetectar")]
        [TestCase("PantallaCorregirDiagrama")]
        [TestCase("PantallaOrdenar")]
        [TestCase("PantallaEdificio")]
        [TestCase("PantallaPuente")]
        [TestCase("PantallaRepartir")]
        public void En_los_minijuegos_no_suena_nada_ni_con_apremio(string pantalla) {
            var m = MezclaDeMusica.Para(pantalla, apremio: true);
            Assert.IsNull(m.Pista);
            Assert.AreEqual(0f, m.Nivel);
        }

        [Test]
        public void Con_apremio_el_tema_sube_y_nunca_pasa_de_uno() {
            Assert.AreEqual(MezclaDeMusica.EnElRecorrido + MezclaDeMusica.SubidaPorApremio,
                            MezclaDeMusica.Para("PantallaDeRecoleccion3D", true).Nivel, 1e-6);
            Assert.AreEqual(1f, MezclaDeMusica.Para("PantallaDePartidas", true).Nivel, 1e-6);
        }

        [Test]
        public void Una_pantalla_desconocida_suena_como_la_partida() {
            var m = MezclaDeMusica.Para("PantallaQueNoExiste");
            Assert.AreEqual(MezclaDeMusica.Nexus, m.Pista);
            Assert.AreEqual(MezclaDeMusica.EnLaPartida, m.Nivel, 1e-6);
            Assert.AreEqual(MezclaDeMusica.Nexus, MezclaDeMusica.Para(null).Pista);
        }
    }
}
