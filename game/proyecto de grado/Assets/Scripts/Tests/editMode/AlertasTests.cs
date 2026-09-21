using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Eventos;
using Nexus.Core.Guardado;
using Nexus.Core.Jornada;
using Nexus.Core.Modelo;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// C11 · Alertas. La citación del momento del §3.3.
    ///
    /// ★ La alerta NO es el telegrafiado. El telegrafiado llega días antes por un canal diegético y
    /// convierte el azar en gestión de riesgo; la alerta es el «ven ahora». Confundirlos destruye el
    /// argumento pedagógico del §7.7, así que hay un test para cada uno.
    /// </summary>
    public class AlertasTests {
        private const int H8 = 8 * 60, H9 = 9 * 60, H11 = 11 * 60, H14 = 14 * 60, H15 = 15 * 60;
        private const int H16 = 16 * 60, H17 = 17 * 60, H18 = 18 * 60;

        private static Alerta Al(string id, int suena, int expira, string tipo = null) {
            return new Alerta {
                Id = id, Tipo = tipo ?? TiposDeAlerta.Decision,
                MinutoDeLaAlerta = suena, MinutoDeExpiracion = expira,
                Canal = "chat", Texto = "Te necesitan en tu escritorio"
            };
        }

        private static ColaDeAlertas Cola(params Alerta[] alertas) {
            var c = new ColaDeAlertas();
            foreach (var a in alertas) c.Encolar(a);
            return c;
        }

        // ============================================================ el paso del tiempo

        [Test]
        public void Una_alerta_no_existe_hasta_que_suena() {
            var c = Cola(Al("EV-TEC-014", H15, H18));

            Assert.IsFalse(c.PorId("EV-TEC-014").YaSono(H11));
            CollectionAssert.IsEmpty(c.Pendientes(H11), "a las 11:00 esa alerta todavia no ha pasado");
            Assert.IsTrue(c.PorId("EV-TEC-014").YaSono(H15));
            Assert.AreEqual(1, c.Pendientes(H15).Count);
        }

        [Test]
        public void SuenanEntre_anuncia_cada_alerta_una_sola_vez() {
            var c = Cola(Al("EV-A", H9, H14), Al("EV-B", H15, H18));

            CollectionAssert.IsEmpty(c.SuenanEntre(H8, H8 + 30));
            CollectionAssert.AreEqual(new[] { "EV-A" }, c.SuenanEntre(H8 + 30, H11).Select(a => a.Id));
            CollectionAssert.IsEmpty(c.SuenanEntre(H11, H14),
                                     "avanzar el reloj otra vez no vuelve a anunciar la misma alerta");
            CollectionAssert.AreEqual(new[] { "EV-B" }, c.SuenanEntre(H14, H16).Select(a => a.Id));
        }

        [Test]
        public void El_intervalo_es_abierto_por_la_izquierda_y_cerrado_por_la_derecha() {
            var c = Cola(Al("EV-A", H15, H18));

            CollectionAssert.IsEmpty(c.SuenanEntre(H15, H16), "justo en H15 ya sono en el tramo anterior");
            CollectionAssert.AreEqual(new[] { "EV-A" }, c.SuenanEntre(H14, H15).Select(a => a.Id));
        }

        // ============================================================ atender

        [Test]
        public void Atender_a_tiempo_la_marca_y_anota_donde_estabas() {
            var c = Cola(Al("EV-TEC-014", H14, H17));

            var alerta = c.Atender("EV-TEC-014", H15, "escritorio");

            Assert.AreEqual(EstadosDeAlerta.Atendida, alerta.Estado);
            Assert.AreEqual(H15, alerta.MinutoDeResolucion);
            Assert.AreEqual("escritorio", alerta.ZonaDelJugador);
            Assert.IsFalse(alerta.EstaPendiente);
            CollectionAssert.IsEmpty(c.Pendientes(H16));
        }

        [Test]
        public void No_se_puede_atender_dos_veces() {
            var c = Cola(Al("EV-A", H14, H17));
            c.Atender("EV-A", H15, "escritorio");

            var ex = Assert.Throws<InvalidOperationException>(() => c.Atender("EV-A", H16, "escritorio"));
            StringAssert.Contains("atendida", ex.Message);
        }

        [Test]
        public void No_se_puede_atender_una_alerta_que_aun_no_sono() {
            var c = Cola(Al("EV-A", H15, H18));
            var ex = Assert.Throws<InvalidOperationException>(() => c.Atender("EV-A", H11, "escritorio"));
            StringAssert.Contains("15:00", ex.Message);
        }

        [Test]
        public void Atender_algo_que_no_existe_lanza() {
            var ex = Assert.Throws<InvalidOperationException>(
                () => Cola().Atender("EV-FANTASMA", H15, "escritorio"));
            StringAssert.Contains("EV-FANTASMA", ex.Message);
        }

        // ============================================================ expirar

        [Test]
        public void Una_alerta_expira_al_pasarse_de_plazo_y_deja_constancia_de_donde_estabas() {
            var c = Cola(Al("EV-TEC-014", H14, H17));

            CollectionAssert.IsEmpty(c.Expirar(H16, "cafeteria"), "a las 16:00 todavia queda una hora");

            var expiradas = c.Expirar(H17, "cafeteria");

            Assert.AreEqual(1, expiradas.Count);
            Assert.AreEqual(EstadosDeAlerta.Expirada, expiradas[0].Estado);
            Assert.AreEqual("cafeteria", expiradas[0].ZonaDelJugador,
                            "el §8 pide esa evidencia: que estaba haciendo el jugador en ese momento");
            Assert.AreEqual(H17, expiradas[0].MinutoDeResolucion);
        }

        [Test]
        public void Expirar_no_toca_lo_ya_resuelto() {
            var c = Cola(Al("EV-A", H9, H14), Al("EV-B", H9, H14));
            c.Atender("EV-A", H11, "escritorio");

            var expiradas = c.Expirar(H15, "bullpen");

            CollectionAssert.AreEqual(new[] { "EV-B" }, expiradas.Select(a => a.Id));
            Assert.AreEqual(EstadosDeAlerta.Atendida, c.PorId("EV-A").Estado);
            CollectionAssert.IsEmpty(c.Expirar(H16, "bullpen"), "expirar dos veces no la expira dos veces");
        }

        [Test]
        public void Una_alerta_expirada_ya_no_se_puede_atender() {
            var c = Cola(Al("EV-A", H9, H14));
            c.Expirar(H15, "bullpen");

            var ex = Assert.Throws<InvalidOperationException>(() => c.Atender("EV-A", H16, "escritorio"));
            StringAssert.Contains("expirada", ex.Message);
        }

        [Test]
        public void La_ventana_de_atencion_se_puede_consultar_para_pintar_la_cuenta_atras() {
            var alerta = Al("EV-A", H14, H17);

            Assert.AreEqual(180, alerta.MinutosParaExpirar(H14), "tres horas de ventana");
            Assert.AreEqual(60, alerta.MinutosParaExpirar(H16));
            Assert.AreEqual(0, alerta.MinutosParaExpirar(H18));
        }

        // ============================================================ cerrar la jornada

        [Test]
        public void No_se_puede_cerrar_la_jornada_con_algo_pendiente() {
            var c = Cola(Al("EV-A", H9, H14));

            Assert.IsFalse(c.SePuedeCerrarLaJornada(H11), "hay una alerta esperando respuesta");
            c.Atender("EV-A", H11, "escritorio");
            Assert.IsTrue(c.SePuedeCerrarLaJornada(H11));
        }

        [Test]
        public void No_se_puede_cerrar_la_jornada_si_queda_algo_por_sonar() {
            var c = Cola(Al("EV-A", H16, H18));

            Assert.IsFalse(c.SePuedeCerrarLaJornada(H11),
                           "saltar al cierre no puede comerse una consecuencia que aun no ha llegado");
            Assert.IsTrue(c.QuedaAlgoPorSonar(H11));
        }

        [Test]
        public void Un_dia_sin_alertas_se_puede_cerrar_desde_el_principio() {
            Assert.IsTrue(Cola().SePuedeCerrarLaJornada(H9),
                          "lo unico que cuesta cerrar pronto es perderse la exploracion de la tarde");
        }

        [Test]
        public void Una_alerta_expirada_tampoco_bloquea_el_cierre() {
            var c = Cola(Al("EV-A", H9, H14));
            c.Expirar(H15, "bullpen");
            Assert.IsTrue(c.SePuedeCerrarLaJornada(H15));
        }

        // ============================================================ el dia siguiente

        [Test]
        public void Las_alertas_de_ayer_no_se_heredan() {
            var c = Cola(Al("EV-A", H9, H14), Al("EV-B", H15, H18));
            c.VaciarDelDia();

            CollectionAssert.IsEmpty(c.Alertas, "una alerta pertenece a su dia");
            Assert.IsTrue(c.SePuedeCerrarLaJornada(H9));
        }

        [Test]
        public void Una_alerta_que_expira_antes_de_sonar_se_rechaza() {
            var ex = Assert.Throws<ArgumentException>(() => Cola().Encolar(Al("EV-A", H15, H14)));
            StringAssert.Contains("expira antes de sonar", ex.Message);
        }

        [Test]
        public void Una_alerta_sin_id_se_rechaza() {
            Assert.Throws<ArgumentException>(() => Cola().Encolar(Al(null, H9, H14)));
            Assert.Throws<ArgumentNullException>(() => Cola().Encolar(null));
        }

        // ============================================================ el telegrafiado NO es la alerta

        [Test]
        public void El_telegrafiado_llega_dias_antes_y_la_alerta_el_mismo_dia() {
            var scheduler = new EffectScheduler();

            // el director elige EV-TEC-014 para el dia 9 a las 14:00, avisando 2 dias antes
            scheduler.AgendarTelegrafiado("EV-TEC-014", 9, 2, "log", "El build tardo 14 min. Ayer tardaba 6.", H14);

            var agendado = scheduler.Telegrafiados[0];
            Assert.AreEqual(7, agendado.DiaDelAviso, "el telegrafiado sale el dia 7…");
            Assert.AreEqual(9, agendado.DiaDelEvento, "…y el evento es el 9");
            Assert.AreEqual(H14, agendado.MinutoDelEvento, "a las 14:00 de ese dia");

            // el dia 7 sale el aviso, y no hay ninguna alerta
            Assert.AreEqual(1, scheduler.AvisosDeHoy(7).Count);
            CollectionAssert.IsEmpty(scheduler.EventosQueDisparanHoy(7));

            // el dia 9 se dispara, y de ahi nace la alerta
            var deHoy = scheduler.EventosQueDisparanHoy(9);
            Assert.AreEqual(1, deHoy.Count);
            Assert.AreEqual(H14, deHoy[0].MinutoDelEvento);
        }

        [Test]
        public void Un_telegrafiado_sin_minuto_lo_deja_en_menos_uno() {
            var scheduler = new EffectScheduler();
            scheduler.AgendarTelegrafiado("EV-A", 5, 2, "log", "…");

            Assert.AreEqual(-1, scheduler.Telegrafiados[0].MinutoDelEvento,
                            "quien construya la alerta decidira la hora");
        }

        // ============================================================ guardado

        [Test]
        public void La_cola_sobrevive_a_guardar_y_recargar_con_su_estado() {
            var antes = Cola(Al("EV-A", H9, H14), Al("EV-B", H15, H18));
            antes.Atender("EV-A", H11, "data-hub");

            var nivel = new NivelEnCurso {
                PerfilDeNivelId = "nivel-01",
                W = new WorldState(), R = new RuntimeState { MinutoDelDia = H14 },
                Alertas = antes.Copia()
            };

            var json = JsonDeGuardado.Serializar(nivel);
            var vuelta = JsonDeGuardado.Deserializar<NivelEnCurso>(json);

            var despues = new ColaDeAlertas();
            despues.Restaurar(vuelta.Alertas);

            Assert.AreEqual(EstadosDeAlerta.Atendida, despues.PorId("EV-A").Estado);
            Assert.AreEqual("data-hub", despues.PorId("EV-A").ZonaDelJugador);
            Assert.IsTrue(despues.PorId("EV-B").EstaPendiente,
                          "guardar no puede servir para esquivar una alerta viva");
            Assert.AreEqual(H18, despues.PorId("EV-B").MinutoDeExpiracion);
        }

        [Test]
        public void Copia_y_Restaurar_no_comparten_objetos() {
            var cola = Cola(Al("EV-A", H9, H14));
            var copia = cola.Copia();
            copia[0].Estado = EstadosDeAlerta.Expirada;

            Assert.IsTrue(cola.PorId("EV-A").EstaPendiente);

            var otra = new ColaDeAlertas();
            otra.Restaurar(copia);
            copia[0].MinutoDeExpiracion = 0;
            Assert.AreEqual(H14, otra.PorId("EV-A").MinutoDeExpiracion);
        }

        [Test]
        public void Un_nivel_sin_bloque_de_alertas_no_se_carga() {
            var save = new SaveGame {
                Id = "p", PerfilId = "perfil",
                Partida = new DatosDePartida { Nombre = "x", NivelActualId = "nivel-01" },
                Nivel = new NivelEnCurso {
                    PerfilDeNivelId = "nivel-01", W = new WorldState(), R = new RuntimeState(),
                    Alertas = null
                }
            };

            var errores = SaveStore.Validar(save);
            Assert.IsTrue(errores.Exists(x => x.Contains("nivel.alertas")), string.Join(" | ", errores));
        }

        // ============================================================ un dia completo

        [Test]
        public void Un_dia_con_dos_alertas_una_atendida_y_otra_perdida() {
            var reloj = new RelojDeJornada(new JornadaConfig());
            var cola = Cola(Al("EV-TEC-014", H11, H14), Al("MJ-F2-02", H15, H18, TiposDeAlerta.Minijuego));
            var zona = "escritorio";
            var bitacora = new List<string>();

            while (!reloj.Terminada) {
                var desde = reloj.Minuto;
                reloj.Avanzar(30);

                foreach (var a in cola.SuenanEntre(desde, reloj.Minuto))
                    bitacora.Add($"{RelojDeJornada.Formatear(a.MinutoDeLaAlerta)} suena {a.Id}");

                foreach (var a in cola.Expirar(reloj.Minuto, zona))
                    bitacora.Add($"{reloj} EXPIRA {a.Id} (estabas en {a.ZonaDelJugador})");

                // el jugador atiende la primera, y para la segunda se va a la cafeteria
                if (reloj.Minuto == H11 + 30 && cola.PorId("EV-TEC-014").EstaPendiente) {
                    cola.Atender("EV-TEC-014", reloj.Minuto, zona);
                    bitacora.Add($"{reloj} atiende EV-TEC-014");
                }
                if (reloj.Minuto == H15) zona = "cafeteria";
            }

            CollectionAssert.AreEqual(new[] {
                "11:00 suena EV-TEC-014",
                "11:30 atiende EV-TEC-014",
                "15:00 suena MJ-F2-02",
                "18:00 EXPIRA MJ-F2-02 (estabas en cafeteria)"
            }, bitacora);

            Assert.AreEqual(EstadosDeAlerta.Atendida, cola.PorId("EV-TEC-014").Estado);
            Assert.AreEqual(EstadosDeAlerta.Expirada, cola.PorId("MJ-F2-02").Estado,
                            "estar lejos cuando expira es un riesgo real, y queda registrado");
        }
    }
}
