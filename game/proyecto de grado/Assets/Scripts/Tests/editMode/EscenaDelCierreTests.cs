using System.Linq;
using Nexus.Core.Jornada;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>La ventana del atardecer del cierre de la jornada (2026-11-07): sus cuentas, sin Unity.</summary>
    public class EscenaDelCierreTests {
        [Test]
        public void La_tarde_va_de_0_a_1_y_se_queda_en_los_bordes() {
            Assert.AreEqual(0f, EscenaDelCierre.FraccionDeTarde(9 * 60, 17 * 60, 19 * 60));
            Assert.AreEqual(0.5f, EscenaDelCierre.FraccionDeTarde(18 * 60, 17 * 60, 19 * 60), 1e-4);
            Assert.AreEqual(1f, EscenaDelCierre.FraccionDeTarde(23 * 60, 17 * 60, 19 * 60));
            Assert.AreEqual(1f, EscenaDelCierre.FraccionDeTarde(17 * 60, 17 * 60, 17 * 60), "un tramo vacio ya paso");
        }

        [Test]
        public void El_reloj_marca_las_cinco_y_media() {
            var m = 17 * 60 + 30;
            Assert.AreEqual(165f, EscenaDelCierre.AnguloDeHora(m), 1e-3);
            Assert.AreEqual(180f, EscenaDelCierre.AnguloDeMinuto(m), 1e-3);
            Assert.AreEqual(0f, EscenaDelCierre.AnguloDeHora(12 * 60), 1e-3);
        }

        [Test]
        public void Las_ventanas_se_encienden_en_un_orden_fijo_y_salteado() {
            var a = EscenaDelCierre.OrdenDeEncendido(24);
            var b = EscenaDelCierre.OrdenDeEncendido(24);
            CollectionAssert.AreEqual(a, b);
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 24), a);
            CollectionAssert.AreNotEqual(Enumerable.Range(0, 24).ToArray(), a, "no de izquierda a derecha");
        }

        [Test]
        public void Las_ventanas_se_van_encendiendo_y_vuelven_a_empezar() {
            Assert.AreEqual(0, EscenaDelCierre.VentanasEncendidas(0f, 20));
            var mitad = EscenaDelCierre.VentanasEncendidas(4.5f, 20);
            Assert.That(mitad, Is.InRange(8, 12));
            Assert.AreEqual(20, EscenaDelCierre.VentanasEncendidas(10f, 20), "en la pausa, todas");
            Assert.AreEqual(0, EscenaDelCierre.VentanasEncendidas(12f, 20), "y vuelve a empezar");
            Assert.AreEqual(0, EscenaDelCierre.VentanasEncendidas(5f, 0));
        }

        [Test]
        public void La_racha_avisa_desde_dos_dias_seguidos() {
            Assert.IsFalse(EscenaDelCierre.EsRachaPeligrosa(0));
            Assert.IsFalse(EscenaDelCierre.EsRachaPeligrosa(1));
            Assert.IsTrue(EscenaDelCierre.EsRachaPeligrosa(2));
            StringAssert.Contains("3 días", EscenaDelCierre.Racha(3));
            Assert.AreEqual("Ayer te quedaste", EscenaDelCierre.Racha(1));
        }

        [Test]
        public void El_paquete_recorre_la_tuberia_se_queda_al_final_y_vuelve() {
            Assert.AreEqual(0f, EscenaDelCierre.Recorrido(0f));
            Assert.AreEqual(0.5f, EscenaDelCierre.Recorrido(1.6f), 1e-4);
            Assert.AreEqual(1f, EscenaDelCierre.Recorrido(3.5f), "en la pausa, llegado");
            Assert.AreEqual(0.25f, EscenaDelCierre.Recorrido(4.8f), 1e-4, "y vuelve a salir");
        }

        [Test]
        public void Las_etapas_se_encienden_al_pasar_el_paquete() {
            Assert.AreEqual(0, EscenaDelCierre.EtapaAlcanzada(0f, 4));
            Assert.AreEqual(0, EscenaDelCierre.EtapaAlcanzada(0.3f, 4));
            Assert.AreEqual(1, EscenaDelCierre.EtapaAlcanzada(1f / 3f, 4));
            Assert.AreEqual(3, EscenaDelCierre.EtapaAlcanzada(1f, 4));
            Assert.AreEqual(3, EscenaDelCierre.EtapaAlcanzada(7f, 4));
        }

        [Test]
        public void Lo_construido_se_apila_en_piramide_y_no_se_desborda() {
            CollectionAssert.AreEqual(new[] { 5, 2 }, EscenaDelCierre.FilasDeLaPila(7));
            CollectionAssert.AreEqual(new[] { 5, 4, 3, 2, 1 }, EscenaDelCierre.FilasDeLaPila(40));
            CollectionAssert.IsEmpty(EscenaDelCierre.FilasDeLaPila(0));
        }
    }
}
