using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Modelo;
using Nexus.Core.Narrativa;
using Nexus.Core.Relaciones;
using Nexus.Core.Sesion;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// La guia que ya no se cuelga (lo de Core: contexto, releer, vistos al cerrar) y las relaciones de la Fase 2:
    /// hablar con la gente de cada sala, ganar su confianza y recibir ayudas.
    /// </summary>
    public class GuiaYRelacionesTests {
        private static Catalogo _catalogo;

        private static Catalogo Catalogo() {
            if (_catalogo != null) return _catalogo;
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            return _catalogo = CatalogLoader.CargarTodo(new CatalogoDeArchivos(raiz));
        }

        // ================================================================ la guia

        [Test]
        public void Los_disparadores_de_pantalla_son_contextuales_y_los_de_evento_no() {
            foreach (var d in new[] { "fase1.metodologia", "fase1.calidad", "dia.antes", "dia.3", "minijuego.jugando.ordenar", "lecciones" })
                Assert.IsTrue(DisparadoresDeGuia.EsContextual(d), d);
            foreach (var d in new[] { "alerta.suena", "coleccionable", "oficina.hecha", "conversacion" })
                Assert.IsFalse(DisparadoresDeGuia.EsContextual(d), d);
        }

        [Test]
        public void Que_hago_ahora_siempre_tiene_algo_que_decir_en_cada_pantalla() {
            var guia = new GuiaDelTutorial(Catalogo().Guia, "nivel-01", new List<string>());
            // N1 casi no tiene guia propia: lo que no tenga, lo cubre la ayuda generica de su pantalla.
            foreach (var contexto in new[] { "fase1.calidad", "dia.4", "minijuego.jugando.repartir", "lecciones" }) {
                var propios = guia.Pasos(contexto);
                var ayuda = guia.Pasos(DisparadoresDeGuia.AyudaDe(contexto));
                Assert.IsTrue(propios.Count + ayuda.Count > 0, "nada que releer en " + contexto);
            }
        }

        [Test]
        public void Releer_un_paso_que_esperaba_una_accion_no_vuelve_a_esperarla() {
            var paso = Catalogo().Guia.First(p => !string.IsNullOrEmpty(p.EsperaAccion));
            var copia = paso.ParaReleer();
            Assert.IsNull(copia.EsperaAccion, "releerlo no puede quedarse esperando algo que ya paso");
            Assert.AreEqual(paso.Texto, copia.Texto);
            Assert.IsNotNull(paso.EsperaAccion, "el original no se toca");
        }

        [Test]
        public void Ningun_nivel_fuerza_el_modo_guiado_se_guia_la_primera_vez_de_cada_mecanica() {
            // Los minijuegos no se explican con burbujas: el modo guiado (RecorridoGuiado) los lleva paso a paso. Pero
            // solo la primera vez que el perfil se enfrenta a cada mecanica (TutorialPorMecanica), tambien en el
            // tutorial: antes N0 guiaba cada practica, cada vez que se repetia.
            foreach (var nivel in Catalogo().Niveles.Values)
                Assert.IsFalse(nivel.MinijuegosGuiados, nivel.Id + " fuerza el modo guiado");
        }

        [Test]
        public void La_guia_del_tutorial_tutea() {
            var textos = Catalogo().Guia.Where(p => p.Nivel == "nivel-00").Select(p => p.Texto + " " + p.Titulo).ToList();
            foreach (var formal in new[] { "Pulse ", "Usted", "usted", " Elija ", "Relea ", "Léalo" })
                Assert.IsFalse(textos.Any(t => t.Contains(formal)), "queda un «" + formal.Trim() + "» en la guia: el juego tutea");
        }

        // ================================================================ el glosario

        [Test]
        public void El_glosario_explica_cada_atributo_de_calidad_cada_arquitectura_y_cada_metodologia() {
            var c = Catalogo();
            Func<string, bool> hay = id => Glosario.Buscar(c.Glosario, id) != null;
            foreach (var nivel in c.Niveles.Values) {
                foreach (var a in nivel.Fase1.Calidad.Atributos) Assert.IsTrue(hay(a.Id), "falta en el glosario: " + a.Id);
                foreach (var a in nivel.Fase1.Arquitecturas) Assert.IsTrue(hay(a.Id), "falta en el glosario: " + a.Id);
            }
            foreach (var m in c.Metodologias.Keys) Assert.IsTrue(hay(m), "falta en el glosario: " + m);
            foreach (var t in new[] { "sprint", "wip", "deuda-tecnica", "cobertura", "riesgo", "satisfaccion", "srs", "ptp", "git", "devops", "estimacion", "patron-de-diseno" })
                Assert.IsTrue(hay(t), "falta en el glosario: " + t);
        }

        [Test]
        public void Cada_concepto_tiene_su_pizarron_de_varias_paginas_con_dibujos_y_su_pagina_del_juego() {
            foreach (var t in Catalogo().Glosario) {
                var paginas = Glosario.PaginasDe(t);
                Assert.GreaterOrEqual(paginas.Count, 2, t.Id);
                Assert.IsTrue(paginas.Any(p => p.EnElJuego), t.Id + ": falta cómo se ve en el juego");
                Assert.IsTrue(paginas.Any(p => p.Vinetas.Count > 0), t.Id + ": un pizarrón sin dibujos");
                foreach (var v in paginas.SelectMany(p => p.Vinetas)) Assert.IsTrue(IconosDePizarra.Existe(v.Icono), t.Id + ": " + v.Icono);
            }
        }

        // ================================================================ arquitecturas con sus propias razones

        [Test]
        public void Cada_arquitectura_ofrece_razones_distintas() {
            foreach (var nivel in Catalogo().Niveles.Values) {
                var listas = nivel.Fase1.Arquitecturas.Select(a => string.Join(",", nivel.Fase1.RazonesDe(a).Select(r => r.Id).OrderBy(x => x))).ToList();
                Assert.AreEqual(listas.Count, listas.Distinct().Count(), nivel.Id + ": dos arquitecturas ofrecen exactamente las mismas razones");
            }
        }

        // ================================================================ relaciones

        private static GameSession EnElDia(string nivel, int dia) {
            var flags = new FlagStore(new Dictionary<string, double>(StringComparer.Ordinal), Catalogo().Flags);
            flags.Inicializar();
            var s = new GameSession(Catalogo(), nivel, flags, 11);
            s.ElegirMetodologia("scrum", "el_cliente_cambiara_de_opinion");
            s.RepartirCalidad(new Dictionary<string, int>());
            s.ElegirArquitectura(nivel == "nivel-00" ? "web-sencilla" : "monolito-modular", nivel == "nivel-00" ? "un_solo_equipo" : "equipo_pequeno");
            s.CerrarFase1();
            for (var d = 1; d < dia; d++) {
                s.ComenzarDia();
                if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
                if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0) s.ElegirAccionRetro(s.PendingRetro.Acciones[0].Id);
                s.AvanzarReloj(24 * 60);
                s.TerminarDia(false);
            }
            s.ComenzarDia();
            if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
            if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0) s.ElegirAccionRetro(s.PendingRetro.Acciones[0].Id);
            return s;
        }

        private static string Mejor(Conversacion c) {
            return c.Pregunta.Opciones.OrderByDescending(o => o.Confianza).First().Id;
        }

        [Test]
        public void Se_habla_con_quien_esta_en_la_sala_una_vez_por_persona_y_dia() {
            var s = EnElDia("nivel-00", 1);
            Assert.IsEmpty(s.ConversacionesAqui(), "en tu escritorio no hay nadie con quien charlar");
            s.IrAZona("pasillo");
            var aqui = s.ConversacionesAqui();
            Assert.AreEqual(1, aqui.Count);
            Assert.AreEqual("marta", aqui[0].Personaje);

            var minuto = s.MinutoDelDia;
            var r = s.Hablar(aqui[0].Id, Mejor(aqui[0]));
            Assert.AreEqual(minuto + aqui[0].Minutos, s.MinutoDelDia, "hablar cuesta tiempo");
            Assert.Greater(r.CambioDeConfianza, 0);
            Assert.IsNotEmpty(r.Respuesta, "cada respuesta trae su explicación");
            Assert.IsEmpty(s.ConversacionesAqui(), "con Marta, una vez al dia");
            Assert.Throws<InvalidOperationException>(() => s.Hablar(aqui[0].Id, Mejor(aqui[0])));
        }

        [Test]
        public void La_confianza_desbloquea_una_ayuda_una_sola_vez_y_se_gasta() {
            var c = Catalogo().Relaciones;
            var r = new RuntimeState { DiaActual = 0 };
            var conversaciones = c.Conversaciones.Where(x => x.Personaje == "marta").OrderBy(x => x.Orden).ToList();
            var ganadas = new List<AyudaDePersonaje>();
            foreach (var cv in conversaciones) {
                r.DiaActual = Math.Max(r.DiaActual + 1, cv.DiaDesde);
                ganadas.AddRange(MotorDeRelaciones.Hablar(c, r, "nivel-00", "pasillo", cv.Id, Mejor(cv)).AyudasNuevas);
            }
            var umbrales = c.PersonajePorId("marta").Ayudas.Select(a => a.Umbral).ToList();
            Assert.AreEqual(umbrales.Count(u => r.Confianza["marta"] >= u), ganadas.Count, "cada ayuda, una vez");
            Assert.Greater(ganadas.Count, 0, "contestando bien a Marta se gana al menos una ayuda en el tutorial");

            var tipo = ganadas[0].Tipo;
            var usos = MotorDeRelaciones.UsosDe(r, tipo);
            Assert.IsTrue(MotorDeRelaciones.Gastar(r, tipo));
            Assert.AreEqual(usos - 1, MotorDeRelaciones.UsosDe(r, tipo));
        }

        [Test]
        public void Contestar_mal_no_baja_la_confianza_de_cero() {
            var c = Catalogo().Relaciones;
            var r = new RuntimeState { DiaActual = 1 };
            var cv = c.Conversaciones.First(x => x.Personaje == "ivan");
            var peor = cv.Pregunta.Opciones.OrderBy(o => o.Confianza).First();
            MotorDeRelaciones.Hablar(c, r, "nivel-00", "terminal", cv.Id, peor.Id);
            Assert.AreEqual(0, r.Confianza["ivan"]);
        }

        [Test]
        public void La_confianza_solo_llega_a_los_flags_al_cerrar() {
            var s = EnElDia("nivel-01", 3);
            s.IrAZona("data-hub");
            var cv = s.ConversacionesAqui().First(x => x.Personaje == "javier");
            var antes = s.Flags.Get("FLG_JAVIER_CONFIANZA");
            for (var i = 0; i < 1; i++) s.Hablar(cv.Id, Mejor(cv));
            Assert.AreEqual(antes, s.Flags.Get("FLG_JAVIER_CONFIANZA"), "INV-6: nada de flags a mitad de nivel");
            Assert.AreEqual(2, s.Confianza("javier"));
        }

        [Test]
        public void La_confianza_viaja_en_el_guardado() {
            var s = EnElDia("nivel-00", 1);
            s.IrAZona("pasillo");
            var cv = s.ConversacionesAqui()[0];
            s.Hablar(cv.Id, Mejor(cv));

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(s.Capturar(), CatalogLoader.Settings);
            var nivel = Newtonsoft.Json.JsonConvert.DeserializeObject<Nexus.Core.Guardado.NivelEnCurso>(json, CatalogLoader.Settings);
            var flags = new FlagStore(new Dictionary<string, double>(StringComparer.Ordinal), Catalogo().Flags);
            var r = GameSession.Restaurar(Catalogo(), "nivel-00", flags, 11, nivel);
            Assert.AreEqual(s.Confianza("marta"), r.Confianza("marta"));
            Assert.IsTrue(r.R.ConversacionesHechas.Contains(cv.Id));
        }

        [Test]
        public void La_confianza_de_quien_sigue_pasa_al_nivel_siguiente_y_la_de_quien_no_se_queda() {
            var c = Catalogo().Relaciones;
            Assert.IsTrue(c.PersonajePorId("javier").Persistente);
            Assert.IsFalse(c.PersonajePorId("ivan").Persistente);

            var r = new RuntimeState { DiaActual = 1 };
            r.Confianza["javier"] = 9;
            r.Confianza["ivan"] = 4;
            var guardado = MotorDeRelaciones.Persistentes(c, r);
            Assert.AreEqual(9, guardado["javier"]);
            Assert.IsFalse(guardado.ContainsKey("ivan"), "Iván no sigue: no viaja");

            var siguiente = new RuntimeState { DiaActual = 1 };
            MotorDeRelaciones.Heredar(c, siguiente, guardado);
            Assert.AreEqual(9, MotorDeRelaciones.ConfianzaDe(siguiente, "javier"));
            var primera = c.PersonajePorId("javier").Ayudas.Min(a => a.Umbral);
            Assert.Greater(MotorDeRelaciones.UsosDe(siguiente, c.PersonajePorId("javier").Ayudas.First(a => a.Umbral == primera).Tipo), 0,
                           "lo ganado antes se nota: su ayuda ya está disponible en el nivel nuevo");
        }

        [Test]
        public void Los_que_siguen_piden_mas_confianza_que_los_que_no() {
            foreach (var p in Catalogo().Relaciones.Personajes.Where(x => x.Persistente))
                Assert.GreaterOrEqual(p.Ayudas.Min(a => a.Umbral), 7, p.Id);
            Assert.LessOrEqual(Catalogo().Relaciones.PersonajePorId("ivan").Ayudas.Min(a => a.Umbral), 3);
        }

        [Test]
        public void Cada_dia_hay_con_quien_hablar_en_cada_zona_con_gente() {
            var c = Catalogo().Relaciones;
            var w = new WorldState { Cobertura = 20, DeudaTecnica = 60, Cansancio = 70, MoralEquipo = 30, SatisfaccionCliente = 30, Documentacion = 20 };
            foreach (var caso in new[] { new { nivel = "nivel-00", zona = "pasillo", dias = 5 }, new { nivel = "nivel-01", zona = "bullpen", dias = 20 },
                                         new { nivel = "nivel-01", zona = "cafeteria", dias = 20 } }) {
                var r = new RuntimeState();
                for (var dia = 1; dia <= caso.dias; dia++) {
                    r.DiaActual = dia;
                    var hoy = MotorDeRelaciones.Disponibles(c, r, caso.nivel, caso.zona, w);
                    Assert.IsNotEmpty(hoy, $"{caso.nivel}/{caso.zona} día {dia}: nadie con quien hablar");
                    var cv = hoy[0];
                    var opcion = cv.Pregunta == null ? null : cv.Pregunta.Opciones.OrderByDescending(o => o.Confianza).First().Id;
                    MotorDeRelaciones.Hablar(c, r, caso.nivel, caso.zona, cv.Id, opcion, w);
                }
            }
        }

        [Test]
        public void La_charla_del_estado_dice_el_valor_real_de_la_barra() {
            var javier = Catalogo().Relaciones.PersonajePorId("javier");
            var charla = javier.Charlas.First(ch => ch.Estadistica == "Cobertura");
            var texto = charla.Texto(new WorldState { Cobertura = 18, DeudaTecnica = 10 });
            StringAssert.Contains("18", texto);
            StringAssert.DoesNotContain("{", texto, "no quedan huecos sin rellenar");
        }

        [Test]
        public void Cada_personaje_con_ayudas_se_puede_ganar_en_su_nivel() {
            var c = Catalogo().Relaciones;
            foreach (var p in c.Personajes) {
                var maximo = c.Conversaciones.Where(x => x.Personaje == p.Id)
                              .Sum(x => x.Pregunta == null ? 1 : x.Pregunta.Opciones.Max(o => o.Confianza));
                var primera = p.Ayudas.Min(a => a.Umbral);
                Assert.GreaterOrEqual(maximo, primera, $"{p.Id}: ni contestando todo bien se llega a su primera ayuda");
            }
        }
    }
}
