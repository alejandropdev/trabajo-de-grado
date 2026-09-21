using System;
using System.Globalization;
using Newtonsoft.Json;
using Nexus.Core.Datos;
using Nexus.Core.Guardado;
using Nexus.Core.Jornada;
using Nexus.Core.Modelo;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// C11 · El reloj de la jornada. Sustituye a las cuatro ventanas fijas por el dia continuo (§3.3).
    ///
    /// Lo que se blinda aqui es que el reloj sea PURO: cuenta minutos del juego y no sabe que existe
    /// el tiempo real. Quien decide cada cuanto hacerlo correr es la capa de presentacion. Asi el
    /// motor sigue siendo determinista y comprobable sin abrir Unity, que es lo que sostiene INV-7.
    /// </summary>
    public class RelojDeJornadaTests {
        private static RelojDeJornada Reloj(int inicio = 8, int cierre = 18, int limite = 20) {
            return new RelojDeJornada(new JornadaConfig {
                HoraInicio = inicio, HoraCierre = cierre, HoraLimite = limite
            });
        }

        // ============================================================ el dia

        [Test]
        public void La_jornada_empieza_a_las_ocho() {
            var r = Reloj();
            Assert.AreEqual(480, r.Minuto);
            Assert.AreEqual(8, r.Hora);
            Assert.AreEqual("08:00", r.ToString());
            Assert.IsFalse(r.LlegoElCierre);
            Assert.IsFalse(r.Terminada);
            Assert.IsFalse(r.Prorrogada);
        }

        [Test]
        public void El_reloj_avanza_hora_a_hora() {
            var r = Reloj();
            var horas = new System.Collections.Generic.List<string>();

            for (var i = 0; i < 10; i++) {
                horas.Add(r.ToString());
                r.Avanzar(60);
            }

            CollectionAssert.AreEqual(
                new[] { "08:00", "09:00", "10:00", "11:00", "12:00", "13:00", "14:00", "15:00", "16:00", "17:00" },
                horas);
            Assert.AreEqual("18:00", r.ToString());
        }

        [Test]
        public void El_reloj_tambien_entiende_de_minutos() {
            var r = Reloj();
            r.Avanzar(25);
            Assert.AreEqual("08:25", r.ToString());
            Assert.AreEqual(8, r.Hora);
            Assert.AreEqual(25, r.MinutoDeLaHora);

            r.Avanzar(40);
            Assert.AreEqual("09:05", r.ToString());
        }

        [Test]
        public void Avanzar_devuelve_lo_que_avanzo_DE_VERDAD() {
            var r = Reloj();
            r.SaltarHasta(17 * 60 + 50);          // 17:50

            var avanzados = r.Avanzar(30);        // se piden 30, pero solo quedan 10

            Assert.AreEqual(10, avanzados,
                            "un desplazamiento de 30 min a las 17:50 no cuesta 30 min: cuesta 10");
            Assert.AreEqual("18:00", r.ToString());
        }

        [Test]
        public void El_reloj_topa_en_el_cierre_si_no_se_prorroga() {
            var r = Reloj();
            r.Avanzar(10000);

            Assert.AreEqual("18:00", r.ToString());
            Assert.IsTrue(r.LlegoElCierre);
            Assert.IsTrue(r.Terminada);
            Assert.AreEqual(0, r.MinutosRestantes);
        }

        [Test]
        public void Avanzar_cero_o_negativo_no_mueve_nada() {
            var r = Reloj();
            Assert.AreEqual(0, r.Avanzar(0));
            Assert.AreEqual(0, r.Avanzar(-90));
            Assert.AreEqual("08:00", r.ToString());
        }

        // ============================================================ quedarse o irse

        [Test]
        public void Quedarse_alarga_el_dia_hasta_la_hora_limite() {
            var r = Reloj();
            r.Avanzar(10 * 60);                   // 18:00

            Assert.IsTrue(r.Terminada, "sin prorrogar, a las 18:00 se acabo");
            Assert.IsTrue(r.Prorrogar());
            Assert.IsFalse(r.Terminada, "quedandose, quedan dos horas");
            Assert.AreEqual(120, r.MinutosRestantes);

            r.Avanzar(120);
            Assert.AreEqual("20:00", r.ToString());
            Assert.IsTrue(r.Terminada);
        }

        [Test]
        public void No_se_puede_prorrogar_antes_del_cierre() {
            var r = Reloj();
            r.Avanzar(4 * 60);                    // 12:00

            Assert.IsFalse(r.Prorrogar(), "antes de las 18:00 no hay nada que prorrogar");
            Assert.IsFalse(r.Prorrogada);
        }

        [Test]
        public void No_se_puede_prorrogar_dos_veces() {
            var r = Reloj();
            r.Avanzar(10 * 60);

            Assert.IsTrue(r.Prorrogar());
            Assert.IsFalse(r.Prorrogar(), "las horas extra no se encadenan dentro del mismo dia");
        }

        [Test]
        public void Un_nivel_sin_horas_extra_no_deja_quedarse() {
            var r = Reloj(inicio: 9, cierre: 14, limite: 14);   // el prologo, por ejemplo
            r.Avanzar(5 * 60);

            Assert.IsTrue(r.LlegoElCierre);
            Assert.IsFalse(r.Prorrogar(), "si limite == cierre, no hay horas extra que ofrecer");
        }

        // ============================================================ cerrar la jornada de golpe

        [Test]
        public void SaltarHasta_lleva_el_reloj_al_cierre_sin_pasar_por_el_medio() {
            var r = Reloj();
            r.Avanzar(2 * 60);                    // 10:00

            var saltados = r.SaltarHasta(r.MinutoDeCierre);

            Assert.AreEqual(8 * 60, saltados);
            Assert.AreEqual("18:00", r.ToString());
        }

        [Test]
        public void SaltarHasta_una_hora_ya_pasada_no_retrocede() {
            var r = Reloj();
            r.Avanzar(5 * 60);                    // 13:00
            Assert.AreEqual(0, r.SaltarHasta(9 * 60));
            Assert.AreEqual("13:00", r.ToString());
        }

        // ============================================================ guardado

        [Test]
        public void Reiniciar_deja_el_reloj_listo_para_el_dia_siguiente() {
            var r = Reloj();
            r.Avanzar(10 * 60);
            r.Prorrogar();
            r.Avanzar(60);

            r.Reiniciar();

            Assert.AreEqual("08:00", r.ToString());
            Assert.IsFalse(r.Prorrogada, "la prorroga es de un dia, no se hereda");
        }

        [Test]
        public void Restaurar_devuelve_el_reloj_a_media_jornada() {
            var r = Reloj();
            r.Restaurar(14 * 60 + 30, false);

            Assert.AreEqual("14:30", r.ToString());
            Assert.IsFalse(r.Terminada);

            var prorrogado = Reloj();
            prorrogado.Restaurar(19 * 60, true);
            Assert.AreEqual("19:00", prorrogado.ToString());
            Assert.IsFalse(prorrogado.Terminada, "se habia quedado a trabajar, y eso tambien se guarda");
        }

        [Test]
        public void El_minuto_del_dia_viaja_en_el_estado_de_ejecucion() {
            var r = new RuntimeState { MinutoDelDia = 14 * 60 + 30, ZonaActual = "bullpen", JornadaProrrogada = true };

            var json = JsonConvert.SerializeObject(r, JsonDeGuardado.Settings);
            var vuelta = JsonConvert.DeserializeObject<RuntimeState>(json, JsonDeGuardado.Settings);

            Assert.AreEqual(870, vuelta.MinutoDelDia,
                            "sin esto, recargar a media jornada devolveria al jugador al principio del dia");
            Assert.AreEqual("bullpen", vuelta.ZonaActual);
            Assert.IsTrue(vuelta.JornadaProrrogada);
        }

        [Test]
        public void El_contenido_puede_preguntar_la_hora() {
            var r = new RuntimeState { MinutoDelDia = 15 * 60 };
            double v;

            Assert.IsTrue(r.TryGet("minutoDelDia", out v));
            Assert.AreEqual(900.0, v, 1e-9);
            CollectionAssert.Contains(RuntimeState.Consultables, "minutoDelDia");
        }

        // ============================================================ configuracion

        [Test]
        public void La_jornada_se_configura_por_nivel() {
            var prologo = new RelojDeJornada(new JornadaConfig {
                HoraInicio = 9, HoraCierre = 13, HoraLimite = 13, VentanaDeAtencionMinutos = 90
            });

            Assert.AreEqual("09:00", prologo.ToString());
            Assert.AreEqual(4 * 60, prologo.MinutosRestantes, "el prologo tiene jornadas mas cortas");
            Assert.AreEqual(90, prologo.Config.VentanaDeAtencionMinutos);
        }

        [Test]
        public void Una_jornada_imposible_se_rechaza_al_construirla() {
            Assert.Throws<InvalidOperationException>(
                () => new RelojDeJornada(new JornadaConfig { HoraInicio = 18, HoraCierre = 8 }));

            Assert.Throws<InvalidOperationException>(
                () => new RelojDeJornada(new JornadaConfig { HoraCierre = 18, HoraLimite = 16 }));

            Assert.Throws<InvalidOperationException>(
                () => new RelojDeJornada(new JornadaConfig { VentanaDeAtencionMinutos = 0 }));

            Assert.Throws<InvalidOperationException>(
                () => new RelojDeJornada(new JornadaConfig { SegundosRealesPorHora = 0 }));
        }

        [Test]
        public void El_validador_de_catalogos_caza_una_jornada_imposible() {
            var perfil = CatalogLoader.CargarPerfil(NivelJson);
            perfil.Jornada.HoraCierre = 6;

            var errores = SchemaValidator.ValidarNivel(perfil);
            Assert.IsTrue(errores.Exists(x => x.Contains("no habria dia que jugar")),
                          string.Join(" | ", errores));
        }

        [Test]
        public void Un_nivel_sin_bloque_jornada_usa_la_de_por_defecto() {
            var perfil = CatalogLoader.CargarPerfil(NivelJson);

            Assert.AreEqual(8, perfil.Jornada.HoraInicio);
            Assert.AreEqual(18, perfil.Jornada.HoraCierre);
            Assert.AreEqual(20, perfil.Jornada.HoraLimite);
            Assert.AreEqual(180, perfil.Jornada.VentanaDeAtencionMinutos, "tres horas para atender una alerta");
            CollectionAssert.IsEmpty(SchemaValidator.ValidarNivel(perfil));
        }

        [Test]
        public void La_jornada_se_clona_con_el_perfil() {
            var perfil = CatalogLoader.CargarPerfil(NivelJson);
            var copia = perfil.Clone();

            copia.Jornada.HoraCierre = 16;

            Assert.AreEqual(18, perfil.Jornada.HoraCierre,
                            "el catalogo se carga una vez: una partida no puede reescribirlo");
        }

        [Test]
        public void Formatear_usa_dos_digitos_en_cualquier_cultura() {
            CultureInfo arabe;
            try {
                arabe = new CultureInfo("ar-SA");
            } catch (CultureNotFoundException) {
                Assert.Ignore("Esta maquina no tiene la cultura ar-SA instalada.");
                return;
            }

            var previa = CultureInfo.CurrentCulture;
            try {
                CultureInfo.CurrentCulture = arabe;
                Assert.AreEqual("08:05", RelojDeJornada.Formatear(8 * 60 + 5));
                Assert.AreEqual("20:00", RelojDeJornada.Formatear(20 * 60));
            } finally {
                CultureInfo.CurrentCulture = previa;
            }
        }

        // Un perfil minimo valido, sin bloque 'jornada', para probar los valores por defecto.
        private const string NivelJson = @"{
  ""id"": ""nivel-01"", ""nombre"": ""Cradle Lifts"",
  ""briefing"": [""Veinte dias.""],
  ""diasTotales"": 20, ""alcanceInicial"": 34, ""velocidadBase"": 3.0,
  ""volatilidadReal"": 25, ""nivelAndamiaje"": 3,
  ""metodologiasPermitidas"": [""scrum""],
  ""director"": { ""presupuestoDrama"": [1,3,2,1] },
  ""fase1"": {
    ""calidad"": { ""fichas"": 8, ""atributos"": [
      { ""id"": ""seguridad"", ""nombre"": ""Seguridad"", ""tagAfectado"": ""seguridad"" } ] },
    ""arquitecturas"": [
      { ""id"": ""monolito"", ""nombre"": ""Monolito"", ""esLaAdecuada"": true,
        ""veredicto"": ""correcta"", ""razon"": ""Tres patios no necesitan microservicios."" },
      { ""id"": ""micro"", ""nombre"": ""Microservicios"", ""esLaAdecuada"": false,
        ""veredicto"": ""incorrecta"", ""razon"": ""Confunde moda con criterio."" } ] } }";
    }
}
