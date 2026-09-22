using System;
using Nexus.Core;
using Nexus.Core.Datos;
using Nexus.Core.Jornada;
using Nexus.Core.Modelo;
using Nexus.Core.Sesion;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// La costura entre el tiempo de la pantalla y el tiempo del juego, y el orden de los niveles. Son las dos
    /// piezas de la capa Unity con logica propia; por eso viven en el Core, donde se pueden probar.
    /// </summary>
    public class RelojEnTiempoRealTests {
        private static RelojEnTiempoReal Reloj(double segundosPorHora = 20) {
            return new RelojEnTiempoReal(new JornadaConfig { SegundosRealesPorHora = segundosPorHora });
        }

        [Test]
        public void Veinte_segundos_reales_son_una_hora_de_juego() {
            Assert.AreEqual(60, Reloj(20).Tick(20.0));
        }

        [Test]
        public void Los_fotogramas_cortos_acumulan_la_fraccion_y_no_pierden_tiempo() {
            var reloj = Reloj(20);
            var total = 0;
            for (var i = 0; i < 60 * 20; i++) total += reloj.Tick(1.0 / 60.0);   // 20 s a 60 fps
            Assert.AreEqual(60, total, "redondeando cada fotograma el reloj no avanzaria nunca");
        }

        [Test]
        public void En_pausa_no_pasa_el_tiempo_ni_se_acumula() {
            var reloj = Reloj(20);
            reloj.Pausado = true;
            Assert.AreEqual(0, reloj.Tick(100));
            reloj.Pausado = false;
            Assert.AreEqual(0, reloj.FraccionPendiente, 1e-9, "la pausa no puede guardarse minutos para luego");
        }

        [Test]
        public void La_velocidad_multiplica_y_tiene_limites() {
            var reloj = Reloj(20);
            reloj.Velocidad = 2.0;
            Assert.AreEqual(120, reloj.Tick(20.0));

            reloj.Velocidad = 100;
            Assert.AreEqual(RelojEnTiempoReal.VelocidadMaxima, reloj.Velocidad);
            reloj.Velocidad = 0;
            Assert.AreEqual(RelojEnTiempoReal.VelocidadMinima, reloj.Velocidad);
        }

        [Test]
        public void Reiniciar_descarta_la_fraccion_del_dia_anterior() {
            var reloj = Reloj(20);
            reloj.Tick(0.2);   // 0,6 minutos
            Assert.Greater(reloj.FraccionPendiente, 0);
            reloj.Reiniciar();
            Assert.AreEqual(0, reloj.FraccionPendiente, 1e-9);
        }

        [Test]
        public void Un_tiempo_negativo_o_raro_no_hace_retroceder_el_reloj() {
            var reloj = Reloj(20);
            Assert.AreEqual(0, reloj.Tick(-5));
            Assert.AreEqual(0, reloj.Tick(double.NaN));
        }

        // ================================================================ progresion

        private static Catalogo ConNiveles(params string[] ids) {
            var c = new Catalogo();
            foreach (var id in ids) c.Niveles[id] = new LevelProfile { Id = id };
            return c;
        }

        [Test]
        public void El_orden_de_los_niveles_es_el_de_sus_ids() {
            var c = ConNiveles("nivel-01", "nivel-00", "nivel-02");
            Assert.AreEqual("nivel-00", ProgresionDeNiveles.Primero(c));
            Assert.AreEqual("nivel-01", ProgresionDeNiveles.Siguiente(c, "nivel-00"));
            Assert.AreEqual("nivel-02", ProgresionDeNiveles.Siguiente(c, "nivel-01"));
        }

        [Test]
        public void Despues_del_ultimo_no_hay_nada() {
            Assert.IsNull(ProgresionDeNiveles.Siguiente(ConNiveles("nivel-00", "nivel-01"), "nivel-01"));
        }

        [Test]
        public void El_pre_test_se_guarda_en_el_perfil_y_sobrevive_a_recargarlo() {
            var store = new ProfileStore(new AlmacenEnMemoria());
            var perfil = store.CrearPerfil("Ada");
            perfil.resultadoPreTest[3] = 1;
            perfil.coleccionablesGlobales.Add("COL-DEV-02");
            store.Guardar(perfil);

            var recargado = store.CargarPorId(perfil.idPerfil);
            Assert.AreEqual(1, recargado.resultadoPreTest[3]);
            CollectionAssert.Contains(recargado.coleccionablesGlobales, "COL-DEV-02");
        }

        [Test]
        public void Un_nivel_que_no_existe_se_rechaza() {
            Assert.Throws<InvalidOperationException>(() => ProgresionDeNiveles.Siguiente(ConNiveles("nivel-00"), "nivel-07"));
        }
    }
}
