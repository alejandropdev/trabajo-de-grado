using System;
using System.Collections.Generic;
using Nexus.Core.Jornada;
using Nexus.Core.Servicios;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// C10 · A que hora suena una alerta (§7.5, paso 7). Lo unico que cambio al pasar al dia continuo:
    /// antes el director agendaba un dia, ahora agenda un (dia, minuto), y el minuto sale del mismo
    /// DeterministicRng que ya elegia el evento — por eso el Modo Aula sigue siendo reproducible al minuto.
    /// </summary>
    public class SorteoDeMinutoTests {
        private static JornadaConfig Jornada(int inicio = 8, int cierre = 18, int ventana = 180, int granularidad = 15) {
            return new JornadaConfig {
                HoraInicio = inicio, HoraCierre = cierre,
                VentanaDeAtencionMinutos = ventana, GranularidadDeAlertas = granularidad
            };
        }

        [Test]
        public void La_misma_semilla_elige_el_mismo_minuto() {
            var a = SorteoDeMinuto.Elegir(Jornada(), new DeterministicRng(4417));
            var b = SorteoDeMinuto.Elegir(Jornada(), new DeterministicRng(4417));
            Assert.AreEqual(a, b, "dos alumnos con la misma semilla sufren la misma alerta a la misma hora");
        }

        [Test]
        public void Semillas_distintas_eligen_minutos_distintos() {
            var vistos = new HashSet<int>();
            for (var semilla = 1; semilla <= 20; semilla++)
                vistos.Add(SorteoDeMinuto.Elegir(Jornada(), new DeterministicRng(semilla)));
            Assert.Greater(vistos.Count, 1, "20 semillas no pueden dar todas el mismo minuto");
        }

        [Test]
        public void Gasta_exactamente_una_tirada() {
            var rng = new DeterministicRng(4417);
            SorteoDeMinuto.Elegir(Jornada(), rng);
            Assert.AreEqual(1, rng.Consumos,
                            "si gastara mas de una, la reproducibilidad del Modo Aula se desalinearia " +
                            "con todo lo que el motor sortea despues");
        }

        [Test]
        public void El_minuto_siempre_deja_caber_la_ventana_de_atencion_entera() {
            var jornada = Jornada(inicio: 8, cierre: 18, ventana: 180);
            var rng = new DeterministicRng(4417);

            for (var i = 0; i < 500; i++) {
                var minuto = SorteoDeMinuto.Elegir(jornada, rng);
                Assert.GreaterOrEqual(minuto, 8 * 60);
                Assert.LessOrEqual(minuto + jornada.VentanaDeAtencionMinutos, 18 * 60,
                                   "una alerta a las 17:00 con 3h de ventana tendria de hecho una hora; " +
                                   "todas las alertas tienen que valer lo mismo");
            }
        }

        [Test]
        public void El_minuto_cae_en_los_multiplos_de_la_granularidad() {
            var jornada = Jornada(granularidad: 15);
            var rng = new DeterministicRng(4417);

            for (var i = 0; i < 200; i++) {
                var minuto = SorteoDeMinuto.Elegir(jornada, rng);
                Assert.AreEqual(0, (minuto - jornada.HoraInicio * 60) % 15,
                                "09:37 se lee peor que 09:30, y no le quita nada al azar");
            }
        }

        [Test]
        public void Con_granularidad_de_un_minuto_puede_caer_en_cualquiera() {
            var jornada = Jornada(granularidad: 1, ventana: 60);
            var rng = new DeterministicRng(4417);
            var vistos = new HashSet<int>();

            for (var i = 0; i < 3000; i++) vistos.Add(SorteoDeMinuto.Elegir(jornada, rng));

            Assert.Greater(vistos.Count, 30, "con granularidad 1 el rango deberia cubrirse bien");
        }

        [Test]
        public void Una_jornada_demasiado_corta_para_la_ventana_suena_al_empezar() {
            // 9:00 a 9:30, con una ventana de 3 horas: no cabe ni una vez.
            var jornada = Jornada(inicio: 9, cierre: 10, ventana: 180);
            var minuto = SorteoDeMinuto.Elegir(jornada, new DeterministicRng(4417));
            Assert.AreEqual(9 * 60, minuto, "si no cabe ni una ventana entera, suena al empezar y se acorta sola");
        }

        [Test]
        public void Cubre_todo_el_rango_disponible_con_tiradas_suficientes() {
            var jornada = Jornada(inicio: 8, cierre: 12, ventana: 60, granularidad: 30);   // huecos: 8:00,8:30,...,11:00 = 7
            var rng = new DeterministicRng(4417);
            var vistos = new HashSet<int>();

            for (var i = 0; i < 2000; i++) vistos.Add(SorteoDeMinuto.Elegir(jornada, rng));

            Assert.AreEqual(7, vistos.Count, "los siete huecos tienen que poder salir todos");
        }

        [Test]
        public void Los_argumentos_nulos_se_rechazan() {
            Assert.Throws<ArgumentNullException>(() => SorteoDeMinuto.Elegir(null, new DeterministicRng(1)));
            Assert.Throws<ArgumentNullException>(() => SorteoDeMinuto.Elegir(Jornada(), null));
        }

        // ------------------------------------------------------------------ Expiracion

        [Test]
        public void Expiracion_suma_la_ventana_de_atencion() {
            var jornada = Jornada(ventana: 180);
            Assert.AreEqual(11 * 60, SorteoDeMinuto.Expiracion(jornada, 8 * 60));
        }

        [Test]
        public void Expiracion_no_pasa_nunca_del_cierre() {
            var jornada = Jornada(cierre: 18, ventana: 180);
            Assert.AreEqual(18 * 60, SorteoDeMinuto.Expiracion(jornada, 17 * 60),
                            "una alerta de las 17:00 no puede expirar a las 20:00: el dia ya se cerro");
        }
    }
}
