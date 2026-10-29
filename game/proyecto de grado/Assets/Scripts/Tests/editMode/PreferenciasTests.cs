using Nexus.Core;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>Las opciones del juego (SRS CU-05): se guardan, se recuperan y nunca salen de su rango.</summary>
    public class PreferenciasTests {
        [NUnit.Framework.Test]
        public void El_modo_tarjetas_viene_activo_y_se_guarda_aunque_se_desactive() {
            var almacen = new AlmacenEnMemoria();
            Assert.IsTrue(new PreferenciasStore(almacen).Cargar().modoTarjetas, "por defecto, tarjetas y frases cortas");
            new PreferenciasStore(almacen).Guardar(new Preferencias { modoTarjetas = false });
            Assert.IsFalse(new PreferenciasStore(almacen).Cargar().modoTarjetas, "los textos completos se recuerdan");
        }

        [Test]
        public void Sin_archivo_devuelve_los_valores_por_defecto() {
            var p = new PreferenciasStore(new AlmacenEnMemoria()).Cargar();
            Assert.AreEqual(1.0, p.velocidadDeSimulacion);
            Assert.AreEqual(1.0, p.volumenGeneral);
            Assert.IsTrue(p.pantallaCompleta);
        }

        [Test]
        public void Lo_guardado_es_lo_que_se_carga() {
            var almacen = new AlmacenEnMemoria();
            new PreferenciasStore(almacen).Guardar(new Preferencias {
                velocidadDeSimulacion = 2, volumenGeneral = 0.5, pantallaCompleta = false
            });
            var p = new PreferenciasStore(almacen).Cargar();
            Assert.AreEqual(2.0, p.velocidadDeSimulacion);
            Assert.AreEqual(0.5, p.volumenGeneral);
            Assert.IsFalse(p.pantallaCompleta);
        }

        [Test]
        public void Los_valores_fuera_de_rango_se_acotan() {
            var p = new Preferencias { velocidadDeSimulacion = 10, volumenGeneral = -3 }.Acotadas();
            Assert.AreEqual(Preferencias.VelocidadMaxima, p.velocidadDeSimulacion);
            Assert.AreEqual(0.0, p.volumenGeneral);
            Assert.AreEqual(Preferencias.VelocidadMinima, new Preferencias { velocidadDeSimulacion = 0 }.Acotadas().velocidadDeSimulacion);
        }

        [Test]
        public void Un_archivo_roto_no_rompe_el_juego() {
            var almacen = new AlmacenEnMemoria();
            almacen.Guardar(PreferenciasStore.Clave, "{ esto no es json");
            Assert.AreEqual(1.0, new PreferenciasStore(almacen).Cargar().velocidadDeSimulacion);
        }
    }
}
