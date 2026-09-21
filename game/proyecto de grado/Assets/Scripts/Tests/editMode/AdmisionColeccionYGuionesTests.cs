using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core;
using Nexus.Core.Coleccion;
using Nexus.Core.Datos;
using Nexus.Core.Evaluacion;
using Nexus.Core.Modelo;
using Nexus.Core.Narrativa;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// Las tres piezas de contenido que el motor no sabia leer: la entrevista del N0 (el pre-test), los
    /// coleccionables y los guiones de las escenas. Aqui se prueba que el corrector mide bien y que el
    /// validador rechaza lo que rompería una partida o una medicion.
    /// </summary>
    public class AdmisionColeccionYGuionesTests {
        private const double Tol = 1e-9;

        // ================================================================ entrevista

        private static PruebaDeAdmision Prueba() {
            var p = new PruebaDeAdmision {
                Version = 1,
                Entrevistadora = new PersonajeDeEscena { Id = "marisol", Nombre = "Marisol Andrade" }
            };
            for (var n = 1; n <= 15; n++)
                p.Preguntas.Add(new PreguntaDeAdmision {
                    Numero = n, Oa = "OA-INTRO-01", Enunciado = "Pregunta " + n,
                    Opciones = {
                        new OpcionDePregunta { Id = "A", Texto = "a" },
                        new OpcionDePregunta { Id = "B", Texto = "b" },
                        new OpcionDePregunta { Id = "C", Texto = "c" }
                    },
                    Correcta = "B", SusurroSiAcierta = "Anótalo.", SusurroSiFalla = "No."
                });
            return p;
        }

        private static Dictionary<int, string> Todas(string opcion) {
            var r = new Dictionary<int, string>();
            for (var n = 1; n <= 15; n++) r[n] = opcion;
            return r;
        }

        [Test]
        public void La_prueba_de_referencia_valida() {
            Assert.IsEmpty(SchemaValidator.ValidarAdmision(Prueba()));
        }

        [Test]
        public void Acertarlo_todo_son_quince_unos_y_fallarlo_todo_quince_ceros() {
            CollectionAssert.AreEqual(Enumerable.Repeat(1, 15).ToArray(), CorrectorDeAdmision.Corregir(Prueba(), Todas("B")));
            CollectionAssert.AreEqual(Enumerable.Repeat(0, 15).ToArray(), CorrectorDeAdmision.Corregir(Prueba(), Todas("A")));
        }

        [Test]
        public void Una_pregunta_sin_responder_cuenta_como_fallo() {
            var respuestas = Todas("B");
            respuestas.Remove(7);
            var resultado = CorrectorDeAdmision.Corregir(Prueba(), respuestas);
            Assert.AreEqual(0, resultado[6], "la pregunta 7 va al indice 6");
            Assert.AreEqual(14, resultado.Sum());
        }

        [Test]
        public void HH_susurra_segun_aciertes_o_falles() {
            var pregunta = Prueba().PorNumero(1);
            Assert.AreEqual("Anótalo.", CorrectorDeAdmision.Susurro(pregunta, "B"));
            Assert.AreEqual("No.", CorrectorDeAdmision.Susurro(pregunta, "C"));
        }

        [Test]
        public void La_ganancia_de_Hake_mide_la_parte_de_lo_que_faltaba() {
            var pre = new int[15];
            for (var i = 0; i < 6; i++) pre[i] = 1;    // 40 %
            var post = new int[15];
            for (var i = 0; i < 12; i++) post[i] = 1;  // 80 %

            Assert.AreEqual(40.0, CorrectorDeAdmision.Porcentaje(pre), Tol);
            Assert.AreEqual((80.0 - 40.0) / (100.0 - 40.0), CorrectorDeAdmision.GananciaDeHake(pre, post).Value, Tol,
                            "g = (post − pre) / (100 − pre)");
        }

        [Test]
        public void Con_un_pre_test_perfecto_no_hay_ganancia_que_medir() {
            Assert.IsNull(CorrectorDeAdmision.GananciaDeHake(Enumerable.Repeat(1, 15).ToArray(), new int[15]),
                          "dividiria por cero");
        }

        [Test]
        public void El_pre_test_se_guarda_en_el_perfil_como_copia() {
            var perfil = new PlayerProfile();
            var resultado = CorrectorDeAdmision.Corregir(Prueba(), Todas("B"));
            CorrectorDeAdmision.GuardarPreTest(perfil, resultado);

            resultado[0] = 0;
            Assert.AreEqual(1, perfil.resultadoPreTest[0], "tocar el array original no puede cambiar el perfil");
            Assert.Throws<ArgumentException>(() => CorrectorDeAdmision.GuardarPreTest(perfil, new int[14]));
        }

        [Test]
        public void Sin_quince_preguntas_el_pre_test_no_se_puede_comparar_con_el_post_test() {
            var p = Prueba();
            p.Preguntas.RemoveAt(14);
            Assert.IsTrue(SchemaValidator.ValidarAdmision(p).Any(e => e.Contains("14 preguntas")));
        }

        [Test]
        public void Una_respuesta_correcta_que_no_es_ninguna_opcion_se_rechaza() {
            var p = Prueba();
            p.Preguntas[2].Correcta = "D";
            Assert.IsTrue(SchemaValidator.ValidarAdmision(p).Any(e => e.Contains("Pregunta 3") && e.Contains("'D'")));
        }

        [Test]
        public void HH_tiene_que_susurrar_algo_en_los_dos_casos() {
            var p = Prueba();
            p.Preguntas[0].SusurroSiFalla = "";
            Assert.IsTrue(SchemaValidator.ValidarAdmision(p).Any(e => e.Contains("susurrar")));
        }

        // ================================================================ coleccionables

        private static Coleccionable Carta(string id = "COL-DEV-02") {
            return new Coleccionable {
                Id = id, Serie = SeriesDeColeccionables.Carta, Palo = PalosDeCarta.Leyes, Titulo = "Ley de Conway",
                Texto = "…", PreguntaDeAplicacion = "¿…?", Fuente = "Conway (1968)."
            };
        }

        [Test]
        public void Una_carta_bien_hecha_valida() {
            Assert.IsEmpty(SchemaValidator.ValidarColeccionables(new List<Coleccionable> { Carta() }));
        }

        [Test]
        public void Una_carta_sin_fuente_se_rechaza() {
            var c = Carta();
            c.Fuente = null;
            Assert.IsTrue(SchemaValidator.ValidarColeccionables(new List<Coleccionable> { c })
                                         .Any(e => e.Contains("fuente")),
                          "es la nota de produccion del canon: hechos reales, siempre con fuente");
        }

        [Test]
        public void Una_startup_sin_causa_se_rechaza() {
            var c = new Coleccionable { Id = "COL-STARTUP-01", Serie = SeriesDeColeccionables.Startup,
                                        Titulo = "Webvan", Texto = "…", Nicho = "Supermercado", Fuente = "…" };
            Assert.IsTrue(SchemaValidator.ValidarColeccionables(new List<Coleccionable> { c })
                                         .Any(e => e.Contains("causa")));
        }

        [Test]
        public void Un_fragmento_del_usb_fuera_de_1_a_8_se_rechaza() {
            var c = new Coleccionable { Id = "COL-USB-09", Serie = SeriesDeColeccionables.Usb,
                                        Titulo = "?", Texto = "…", Fragmento = 9 };
            Assert.IsTrue(SchemaValidator.ValidarColeccionables(new List<Coleccionable> { c })
                                         .Any(e => e.Contains("fragmento")));
        }

        [Test]
        public void Un_palo_inventado_se_rechaza() {
            var c = Carta();
            c.Palo = "trucos";
            Assert.IsTrue(SchemaValidator.ValidarColeccionables(new List<Coleccionable> { c })
                                         .Any(e => e.Contains("palo")));
        }

        private static Catalogo CatalogoMinimo() {
            var c = new Catalogo();
            c.Niveles["nivel-01"] = new LevelProfile { Id = "nivel-01", Nombre = "N1", DiasTotales = 20, AlcanceInicial = 10 };
            c.Niveles["nivel-00"] = new LevelProfile { Id = "nivel-00", Nombre = "N0", DiasTotales = 5, AlcanceInicial = 10 };
            return c;
        }

        private static List<string> ErroresCruzados(Catalogo c) {
            // Solo interesan los cruces de esta prueba; el catalogo minimo no tiene metodologias ni eventos.
            return SchemaValidator.ValidarCatalogo(c)
                                  .Where(e => !e.Contains("metodologia") && !e.Contains("eventos") &&
                                              !e.Contains("briefing") && !e.Contains("fase1") &&
                                              !e.Contains("arquitectura"))
                                  .ToList();
        }

        [Test]
        public void Una_zona_con_un_coleccionable_que_no_existe_se_rechaza() {
            var c = CatalogoMinimo();
            c.Coleccionables.Add(Carta());
            c.Niveles["nivel-01"].Mapa = new Nexus.Core.Jornada.MapaDeZonas {
                Zonas = { new Nexus.Core.Jornada.ZonaDeNivel { Id = "escritorio", Nombre = "E", EsAncla = true, QueDa = "x",
                                                               Coleccionables = { "COL-DEV-99" } } }
            };
            Assert.IsTrue(ErroresCruzados(c).Any(e => e.Contains("COL-DEV-99")));
        }

        [Test]
        public void Un_coleccionable_no_puede_estar_en_dos_sitios() {
            var c = CatalogoMinimo();
            c.Coleccionables.Add(Carta());
            foreach (var nivel in new[] { "nivel-00", "nivel-01" })
                c.Niveles[nivel].Mapa = new Nexus.Core.Jornada.MapaDeZonas {
                    Zonas = { new Nexus.Core.Jornada.ZonaDeNivel { Id = "escritorio", Nombre = "E", EsAncla = true, QueDa = "x",
                                                                   Coleccionables = { "COL-DEV-02" } } }
                };
            Assert.IsTrue(ErroresCruzados(c).Any(e => e.Contains("a la vez")));
        }

        // ================================================================ guiones

        private static Guion GuionDeDia(string id, params string[] variantes) {
            var g = new Guion { Id = id, Titulo = id, Nivel = "nivel-01", Momento = MomentosDeGuion.Dia };
            foreach (var v in variantes)
                g.Variantes[v] = new List<LineaDeGuion> { new LineaDeGuion { Quien = "narrador", Texto = "…" } };
            return g;
        }

        [Test]
        public void Un_beat_sin_guion_es_una_escena_en_blanco() {
            var c = CatalogoMinimo();
            c.Beats.Add(new NarrativeBeat { Id = "CIN-1.2", Nombre = "Standup" });
            c.Guiones.Add(GuionDeDia("INT-1", "default"));
            Assert.IsTrue(ErroresCruzados(c).Any(e => e.Contains("CIN-1.2") && e.Contains("en blanco")));
        }

        [Test]
        public void Cada_variante_que_el_coloreo_puede_elegir_tiene_que_estar_escrita() {
            var c = CatalogoMinimo();
            c.Beats.Add(new NarrativeBeat {
                Id = "INT-1", Nombre = "El Cuarto",
                Coloreo = { { "vecesQueSeFueACasa >= 5", "cansado" }, { "default", "base" } }
            });
            c.Guiones.Add(GuionDeDia("INT-1", "base"));
            Assert.IsTrue(ErroresCruzados(c).Any(e => e.Contains("'cansado'")));
        }

        [Test]
        public void Sin_coloreo_basta_con_la_variante_default() {
            var c = CatalogoMinimo();
            c.Beats.Add(new NarrativeBeat { Id = "CIN-1.2", Nombre = "Standup" });
            c.Guiones.Add(GuionDeDia("CIN-1.2", "default"));
            Assert.IsFalse(ErroresCruzados(c).Any(e => e.Contains("CIN-1.2")));
        }

        [Test]
        public void Una_opcion_de_guion_solo_puede_escribir_flags_del_censo() {
            var c = CatalogoMinimo();
            c.Flags.Add(new DefinicionDeFlag { Id = "FLG_DEUDA_MORAL", Eje = "integridad", NoBaja = true, Descripcion = "x" });
            var g = GuionDeDia("TUT-0.2", "default");
            g.Opciones.Add(new OpcionDeGuion { Id = "si", Texto = "Sí", Flag = "FLG_MARTA_ALIADISIMA", Valor = 1 });
            g.Opciones.Add(new OpcionDeGuion { Id = "no", Texto = "No" });
            c.Guiones.Add(g);
            Assert.IsTrue(ErroresCruzados(c).Any(e => e.Contains("FLG_MARTA_ALIADISIMA")));
        }

        [Test]
        public void Una_eleccion_de_una_sola_opcion_no_es_una_eleccion() {
            var g = GuionDeDia("TUT-0.2", "default");
            g.Opciones.Add(new OpcionDeGuion { Id = "si", Texto = "Sí" });
            Assert.IsTrue(SchemaValidator.ValidarGuiones(new List<Guion> { g }).Any(e => e.Contains("una sola opcion")));
        }

        [Test]
        public void Un_momento_inventado_se_rechaza() {
            var g = GuionDeDia("X", "default");
            g.Momento = "cuandoSea";
            Assert.IsTrue(SchemaValidator.ValidarGuiones(new List<Guion> { g }).Any(e => e.Contains("momento")));
        }

        // ================================================================ soloNiveles y flags leidos

        [Test]
        public void Una_cinematica_atada_a_un_nivel_que_no_existe_se_rechaza() {
            var c = CatalogoMinimo();
            c.Beats.Add(new NarrativeBeat { Id = "CIN-9.9", Nombre = "?", SoloNiveles = { "nivel-09" } });
            Assert.IsTrue(ErroresCruzados(c).Any(e => e.Contains("nivel-09")));
        }

        [Test]
        public void Una_condicion_que_lee_un_flag_fuera_del_censo_se_rechaza() {
            var c = CatalogoMinimo();
            c.Flags.Add(new DefinicionDeFlag { Id = "FLG_DEUDA_MORAL", Eje = "integridad", NoBaja = true, Descripcion = "x" });
            c.Beats.Add(new NarrativeBeat { Id = "INT-1", Nombre = "El Cuarto",
                                            Coloreo = { { "FLG_ORIGEN_INVENTADO == 1", "otra" } } });
            Assert.IsTrue(ErroresCruzados(c).Any(e => e.Contains("FLG_ORIGEN_INVENTADO")),
                          "si no, fallaria en mitad de la partida al evaluar el coloreo");
        }
    }
}
