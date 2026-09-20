using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Eventos;
using Nexus.Core.Guardado;
using Nexus.Core.Modelo;
using Nexus.Core.Servicios;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// C4 · Cola temporal. Aqui vive el principio P4: el coste siempre es diferido.
    /// Lo que se blinda es que una consecuencia programada NO se pueda perder — ni saltandose un dia,
    /// ni guardando y recargando (INV-7) — y que ningun evento se dispare sin aviso previo (INV-3).
    /// </summary>
    public class ColaTemporalTests {
        private const double Tol = 1e-9;

        private static EfectoDiferido Diferido(int enDias, string clave = "DeudaTecnica", object valor = null,
                                               string eventoForzado = null) {
            return new EfectoDiferido {
                EnDias = enDias,
                Efectos = new Dictionary<string, object> { { clave, valor ?? (object)8 } },
                EventoForzado = eventoForzado
            };
        }

        // ------------------------------------------------------------------ efectos diferidos

        [Test]
        public void Encolar_programa_el_efecto_en_su_dia_absoluto() {
            var s = new EffectScheduler();
            s.Encolar(4, Diferido(5), "EV-TEC-02");

            Assert.AreEqual(1, s.Cola.Count);
            Assert.AreEqual(9, s.Cola[0].DiaObjetivo, "dia 4 + 5 dias");
            Assert.AreEqual("EV-TEC-02", s.Cola[0].Origen);
        }

        [Test]
        public void Encolar_copia_los_efectos_y_no_comparte_el_catalogo() {
            var s = new EffectScheduler();
            var delCatalogo = Diferido(2);

            s.Encolar(1, delCatalogo, "EV-TEC-02");
            delCatalogo.Efectos["DeudaTecnica"] = 999;
            delCatalogo.Efectos["Cobertura"] = -50;

            Assert.AreEqual(8, Convert.ToInt32(s.Cola[0].Efectos["DeudaTecnica"]),
                            "el catalogo se carga una vez: una partida no puede reescribirlo");
            Assert.AreEqual(1, s.Cola[0].Efectos.Count);
        }

        [Test]
        public void Vencidos_saca_lo_que_toca_y_lo_quita_de_la_cola() {
            var s = new EffectScheduler();
            s.Encolar(0, Diferido(3), "A");    // dia 3
            s.Encolar(0, Diferido(7), "B");    // dia 7

            var hoy = s.Vencidos(3);

            Assert.AreEqual(1, hoy.Count);
            Assert.AreEqual("A", hoy[0].Origen);
            Assert.AreEqual(1, s.Cola.Count, "lo que vence se retira");
            Assert.AreEqual("B", s.Cola[0].Origen);

            CollectionAssert.IsEmpty(s.Vencidos(3), "llamarlo dos veces el mismo dia no lo aplica dos veces");
        }

        [Test]
        public void Vencidos_respeta_el_orden_de_encolado() {
            var s = new EffectScheduler();
            s.Encolar(0, Diferido(5), "primero");
            s.Encolar(0, Diferido(2), "segundo");
            s.Encolar(0, Diferido(5), "tercero");

            var hoy = s.Vencidos(5);

            CollectionAssert.AreEqual(new[] { "primero", "segundo", "tercero" }, hoy.Select(e => e.Origen).ToArray(),
                                      "el orden tiene que ser estable o la partida deja de ser reproducible");
        }

        [Test]
        public void Vencidos_recoge_tambien_lo_que_se_quedo_atras() {
            var s = new EffectScheduler();
            s.Encolar(0, Diferido(3), "A");

            var hoy = s.Vencidos(10);

            Assert.AreEqual(1, hoy.Count, "un efecto perdido seria una consecuencia esquivada sin saberlo");
        }

        [Test]
        public void Un_diferido_que_vence_en_el_pasado_lanza() {
            var s = new EffectScheduler();
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => s.Encolar(4, Diferido(-1), "EV-MAL"));
            StringAssert.Contains("EV-MAL", ex.Message);
        }

        [Test]
        public void Encolar_null_no_hace_nada() {
            var s = new EffectScheduler();
            s.Encolar(1, null, "X");
            CollectionAssert.IsEmpty(s.Cola);
        }

        // ------------------------------------------------------------------ telegrafiados

        [Test]
        public void AgendarTelegrafiado_calcula_el_dia_del_aviso() {
            var s = new EffectScheduler();
            s.AgendarTelegrafiado("EV-TEC-014", 9, 2, "log", "El build tardo 14 min. Ayer tardaba 6.");

            Assert.AreEqual(9, s.Telegrafiados[0].DiaDelEvento);
            Assert.AreEqual(7, s.Telegrafiados[0].DiaDelAviso);
            Assert.AreEqual("log", s.Telegrafiados[0].Canal);
            Assert.IsFalse(s.Telegrafiados[0].Emitido);
        }

        [Test]
        public void El_aviso_nunca_cae_antes_del_dia_uno() {
            var s = new EffectScheduler();
            s.AgendarTelegrafiado("EV-PRONTO", 2, 5, "chat", "…");
            Assert.AreEqual(1, s.Telegrafiados[0].DiaDelAviso,
                            "un aviso en el dia -3 no lo veria nadie y el evento saldria a bocajarro");
        }

        [Test]
        public void El_aviso_sale_una_sola_vez() {
            var s = new EffectScheduler();
            s.AgendarTelegrafiado("EV-TEC-014", 9, 2, "log", "…");

            CollectionAssert.IsEmpty(s.AvisosDeHoy(6), "todavia no toca");
            Assert.AreEqual(1, s.AvisosDeHoy(7).Count);
            Assert.IsTrue(s.Telegrafiados[0].Emitido);
            CollectionAssert.IsEmpty(s.AvisosDeHoy(8), "si se repitiera, el jugador dejaria de leerlo");
        }

        [Test]
        public void Un_aviso_atrasado_sale_igualmente_porque_ningun_evento_va_sin_aviso() {
            var s = new EffectScheduler();
            s.AgendarTelegrafiado("EV-TEC-014", 9, 2, "log", "…");

            Assert.AreEqual(1, s.AvisosDeHoy(8).Count, "INV-3: perder el aviso dispararia el evento a ciegas");
        }

        [Test]
        public void La_agenda_sabe_que_hay_y_cuando() {
            var s = new EffectScheduler();
            s.AgendarTelegrafiado("EV-A", 9, 2, "log", "…");
            s.AgendarTelegrafiado("EV-B", 9, 1, "correo", "…");
            s.AgendarTelegrafiado("EV-C", 12, 3, "chat", "…");

            Assert.IsTrue(s.HayEventoAgendado("EV-B"));
            Assert.IsFalse(s.HayEventoAgendado("EV-Z"));
            Assert.AreEqual(2, s.EventosAgendadosEn(9), "es lo que hace cumplir maxEventosPorDia");
            Assert.AreEqual(1, s.EventosAgendadosEn(12));
            Assert.AreEqual(0, s.EventosAgendadosEn(5));

            CollectionAssert.AreEqual(new[] { "EV-A", "EV-B" },
                                      s.EventosQueDisparanHoy(9).Select(t => t.EventoId).ToArray());
        }

        [Test]
        public void OlvidarAgenda_retira_el_evento() {
            var s = new EffectScheduler();
            s.AgendarTelegrafiado("EV-A", 9, 2, "log", "…");
            s.AgendarTelegrafiado("EV-B", 9, 2, "log", "…");

            s.OlvidarAgenda("EV-A");

            Assert.IsFalse(s.HayEventoAgendado("EV-A"));
            Assert.AreEqual(1, s.EventosAgendadosEn(9));
        }

        [Test]
        public void Agendar_sin_id_lanza() {
            var s = new EffectScheduler();
            Assert.Throws<ArgumentException>(() => s.AgendarTelegrafiado(null, 9, 2, "log", "…"));
            Assert.Throws<ArgumentException>(() => s.AgendarTelegrafiado("", 9, 2, "log", "…"));
        }

        // ------------------------------------------------------------------ el ciclo completo

        [Test]
        public void El_ciclo_de_un_evento_telegrafiado_de_principio_a_fin() {
            var s = new EffectScheduler();

            // dia 5: el director elige EV-TEC-014 para el dia 9 y lo telegrafia con 2 dias de antelacion
            s.AgendarTelegrafiado("EV-TEC-014", 9, 2, "log", "El build tardo 14 min.");

            for (var dia = 5; dia <= 6; dia++)
                CollectionAssert.IsEmpty(s.AvisosDeHoy(dia), "dia " + dia + ": todavia nada");

            var aviso = s.AvisosDeHoy(7).Single();
            Assert.AreEqual("El build tardo 14 min.", aviso.Texto, "dia 7: llega el aviso por el log");

            CollectionAssert.IsEmpty(s.EventosQueDisparanHoy(8), "dia 8: silencio");

            var evento = s.EventosQueDisparanHoy(9).Single();
            Assert.AreEqual("EV-TEC-014", evento.EventoId, "dia 9: el evento se presenta, ya avisado");

            // el jugador elige la opcion B, que tiene un coste diferido a 9 dias
            s.OlvidarAgenda("EV-TEC-014");
            s.Encolar(9, Diferido(9, "DeudaTecnica", 12, "EV-TEC-021"), "EV-TEC-014");

            CollectionAssert.IsEmpty(s.Vencidos(17), "dia 17: aun no");
            var cobro = s.Vencidos(18).Single();
            Assert.AreEqual("EV-TEC-021", cobro.EventoForzado, "dia 18: se cobra, y encadena otro evento");
            Assert.AreEqual("EV-TEC-014", cobro.Origen, "la traza sabe de donde venia");
        }

        [Test]
        public void Un_efecto_vencido_llega_al_WorldState_por_la_puerta_de_siempre() {
            var s = new EffectScheduler();
            var w = new WorldState();
            w.Set("DeudaTecnica", 20);
            w.Set("Cobertura", 50);

            s.Encolar(1, new EfectoDiferido {
                EnDias = 4,
                Efectos = new Dictionary<string, object> { { "DeudaTecnica", 12L }, { "Cobertura", "-15%" } }
            }, "EV-TEC-02");

            foreach (var vencido in s.Vencidos(5))
                EffectApplier.Aplicar(w, vencido.Efectos);

            Assert.AreEqual(32.0, w.DeudaTecnica, Tol);
            Assert.AreEqual(42.5, w.Cobertura, Tol);
        }

        // ------------------------------------------------------------------ guardado

        [Test]
        public void Restaurar_reconstruye_la_agenda_sin_compartir_las_listas() {
            var original = new EffectScheduler();
            original.Encolar(0, Diferido(11), "EV-TEC-02");
            original.AgendarTelegrafiado("EV-CLI-01", 9, 2, "correo", "…");
            original.AvisosDeHoy(7);

            var cola = original.CopiaDeCola();
            var avisos = original.CopiaDeTelegrafiados();

            var restaurado = new EffectScheduler();
            restaurado.Restaurar(cola, avisos);

            Assert.AreEqual(1, restaurado.Cola.Count);
            Assert.AreEqual(11, restaurado.Cola[0].DiaObjetivo);
            Assert.IsTrue(restaurado.Telegrafiados[0].Emitido, "un aviso ya emitido no vuelve a salir tras recargar");

            // tocar las listas de entrada no puede alterar el scheduler restaurado
            cola[0].DiaObjetivo = 999;
            avisos[0].Emitido = false;
            Assert.AreEqual(11, restaurado.Cola[0].DiaObjetivo);
            Assert.IsTrue(restaurado.Telegrafiados[0].Emitido);
        }

        [Test]
        public void CopiaDeCola_es_profunda() {
            var s = new EffectScheduler();
            s.Encolar(0, Diferido(3), "A");

            var copia = s.CopiaDeCola();
            copia[0].Efectos["DeudaTecnica"] = 999;
            copia[0].DiaObjetivo = 999;

            Assert.AreEqual(8, Convert.ToInt32(s.Cola[0].Efectos["DeudaTecnica"]));
            Assert.AreEqual(3, s.Cola[0].DiaObjetivo);
        }

        [Test]
        public void La_agenda_sobrevive_a_un_viaje_por_el_json_del_guardado() {
            var s = new EffectScheduler();
            s.Encolar(2, new EfectoDiferido {
                EnDias = 9,
                Efectos = new Dictionary<string, object> { { "DeudaTecnica", 12L }, { "VelocidadMod", "-25%" } },
                EventoForzado = "EV-TEC-021"
            }, "EV-TEC-014");
            s.AgendarTelegrafiado("EV-CLI-01", 9, 2, "correo", "La Ministra pregunta por el panel.");
            s.AvisosDeHoy(7);

            var nivel = new NivelEnCurso {
                PerfilDeNivelId = "nivel-01",
                W = new WorldState(),
                R = new RuntimeState(),
                ColaDeEfectos = s.CopiaDeCola(),
                Telegrafiados = s.CopiaDeTelegrafiados()
            };

            var json = JsonDeGuardado.Serializar(nivel);
            var vuelta = JsonDeGuardado.Deserializar<NivelEnCurso>(json);

            Assert.AreEqual(11, vuelta.ColaDeEfectos[0].DiaObjetivo);
            Assert.AreEqual("EV-TEC-014", vuelta.ColaDeEfectos[0].Origen);
            Assert.AreEqual("EV-TEC-021", vuelta.ColaDeEfectos[0].EventoForzado);
            Assert.AreEqual("-25%", vuelta.ColaDeEfectos[0].Efectos["VelocidadMod"].ToString(),
                            "los porcentajes siguen siendo texto tras el viaje");
            Assert.AreEqual(7, vuelta.Telegrafiados[0].DiaDelAviso);
            Assert.IsTrue(vuelta.Telegrafiados[0].Emitido);
            Assert.AreEqual(json, JsonDeGuardado.Serializar(vuelta));
        }

        [Test]
        public void Un_efecto_diferido_sobrevive_a_guardar_y_recargar_y_se_cobra_igual() {
            var antes = new EffectScheduler();
            antes.Encolar(2, Diferido(9, "DeudaTecnica", 12), "EV-TEC-014");

            // se guarda el dia 5 y se recarga
            var json = JsonDeGuardado.Serializar(antes.CopiaDeCola());
            var despues = new EffectScheduler();
            despues.Restaurar(JsonDeGuardado.Deserializar<List<EfectoEnCola>>(json), null);

            var wSinRecargar = new WorldState();
            var wRecargado = new WorldState();
            foreach (var e in antes.Vencidos(11)) EffectApplier.Aplicar(wSinRecargar, e.Efectos);
            foreach (var e in despues.Vencidos(11)) EffectApplier.Aplicar(wRecargado, e.Efectos);

            Assert.AreEqual(wSinRecargar.ToString(), wRecargado.ToString(),
                            "INV-7: recargar no puede servir para esquivar una consecuencia");
            Assert.AreEqual(12.0, wRecargado.DeudaTecnica, Tol);
        }
    }
}
