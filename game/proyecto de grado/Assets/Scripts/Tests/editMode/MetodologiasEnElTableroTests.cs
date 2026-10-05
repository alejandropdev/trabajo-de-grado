using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Metodologia;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using Nexus.Core.Tablero;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// Que elegir metodologia se note en la Fase 2 (feedback de la beta: «sigo viendo todo igual y nunca importó
    /// seleccionar la metodología»). Cada una trae su tablero, sus vistas, sus ceremonias y sus acciones, y —tan
    /// importante— lo que NO tiene: Kanban no tiene sprints ni burndown, Cascada no tiene reunion diaria ni prueba
    /// antes de su etapa, Scrum no tiene limite de WIP ni documentos que firmar.
    /// </summary>
    public class MetodologiasEnElTableroTests {
        private static Catalogo _catalogo;

        private static Catalogo Catalogo() {
            if (_catalogo != null) return _catalogo;
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            return _catalogo = CatalogLoader.CargarTodo(new CatalogoDeArchivos(raiz));
        }

        private static FlagStore Flags() {
            var store = new FlagStore(new Dictionary<string, double>(StringComparer.Ordinal), Catalogo().Flags);
            store.Inicializar();
            return store;
        }

        private static GameSession Empezar(string nivel, string metodologia, int semilla = 3) {
            var s = new GameSession(Catalogo(), nivel, Flags(), semilla);
            s.ElegirMetodologia(metodologia, Catalogo().Metodologias[metodologia].TextosDeRazones.Keys.First());
            s.RepartirCalidad(new Dictionary<string, int>());
            var arq = Catalogo().Niveles[nivel].Fase1.Arquitecturas[0];
            s.ElegirArquitectura(arq.Id, Catalogo().Niveles[nivel].Fase1.RazonesDe(arq)[0].Id);
            s.CerrarFase1();
            return s;
        }

        /// <summary>Empieza un dia y deja resueltas la planificacion y la retro, si las hay.</summary>
        private static void EmpezarDia(GameSession s) {
            s.ComenzarDia();
            if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
            if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0) s.ElegirAccionRetro(s.PendingRetro.Acciones[0].Id);
        }

        private static void CerrarDia(GameSession s) {
            for (var v = 0; v < 80 && !s.SePuedeCerrarLaJornada; v++) {
                foreach (var a in s.AvanzarReloj(30).AlertasQueSuenan) s.AtenderAlerta(a.Id);
                if (s.Decision != null) s.ResolverDecision(s.Decision.Opciones.First(o => !o.Bloqueada).Id);
                if (s.Minijuego != null) s.ResolverMinijuego(Nexus.Core.Minijuegos.PuenteDelMotor.Omitido(s.Minijuego.MinijuegoId, s.Minijuego.ObjetivoAprendizaje, null));
            }
            if (!s.JornadaLlegoAlCierre) s.CerrarJornada();
            s.TerminarDia(false);
        }

        private static HashSet<string> CeremoniasDelNivel(GameSession s) {
            return new HashSet<string>(s.AgendaDeCeremonias().Select(c => c.Id));
        }

        // ================================================================ lo que tiene y lo que no tiene cada una

        [Test]
        public void Cada_metodologia_trae_su_propio_tablero_sus_vistas_y_dice_lo_que_no_tiene() {
            var scrum = Catalogo().Metodologias["scrum"].PoliticaDeTablero;
            var kanban = Catalogo().Metodologias["kanban"].PoliticaDeTablero;
            var cascada = Catalogo().Metodologias["cascada"].PoliticaDeTablero;

            Assert.IsTrue(scrum.SprintCerrado); Assert.IsFalse(scrum.LimiteDeWip); Assert.IsFalse(scrum.InspeccionDeDocumentos);
            Assert.IsTrue(kanban.LimiteDeWip); Assert.IsFalse(kanban.SprintCerrado); Assert.IsFalse(kanban.InspeccionDeDocumentos);
            Assert.IsTrue(cascada.InspeccionDeDocumentos); Assert.IsFalse(cascada.SprintCerrado); Assert.IsFalse(cascada.LimiteDeWip);

            // El diagrama propio de cada una, y ninguno de los ajenos.
            Assert.IsTrue(scrum.Tiene(VistasDeMonitoreo.Burndown) && scrum.Tiene(VistasDeMonitoreo.Backlog));
            Assert.IsFalse(scrum.Tiene(VistasDeMonitoreo.Cfd) || scrum.Tiene(VistasDeMonitoreo.CurvaS) || scrum.Tiene(VistasDeMonitoreo.Documentos));
            Assert.IsTrue(kanban.Tiene(VistasDeMonitoreo.Cfd));
            Assert.IsFalse(kanban.Tiene(VistasDeMonitoreo.Burndown) || kanban.Tiene(VistasDeMonitoreo.Backlog) || kanban.Tiene(VistasDeMonitoreo.CurvaS));
            Assert.IsTrue(cascada.Tiene(VistasDeMonitoreo.CurvaS) && cascada.Tiene(VistasDeMonitoreo.Documentos));
            Assert.IsFalse(cascada.Tiene(VistasDeMonitoreo.Burndown) || cascada.Tiene(VistasDeMonitoreo.Cfd));

            foreach (var p in new[] { scrum, kanban, cascada }) {
                Assert.IsFalse(string.IsNullOrEmpty(p.LoQueNoHay), "tiene que decirle al jugador lo que esta metodología no hace");
                foreach (var v in p.Vistas) Assert.IsTrue(VistasDeMonitoreo.EsValida(v), v);
                Assert.AreNotEqual(p.NombreDe(ColumnasDeBase.PorHacer, "x"), "x", "sus columnas tienen su propio nombre");
            }
        }

        [Test]
        public void Las_ceremonias_son_las_de_cada_metodologia_y_ninguna_de_las_otras() {
            var scrum = CeremoniasDelNivel(Empezar("nivel-01", "scrum"));
            var kanban = CeremoniasDelNivel(Empezar("nivel-01", "kanban"));
            var cascada = CeremoniasDelNivel(Empezar("nivel-01", "cascada"));

            CollectionAssert.IsSubsetOf(new[] { "planning", "daily", "review", "retro" }, scrum);
            CollectionAssert.IsSubsetOf(new[] { "daily-de-tablero", "reposicion", "revision-del-flujo" }, kanban);
            CollectionAssert.IsSubsetOf(new[] { "kickoff", "comite-de-seguimiento", "firma-de-etapa", "lecciones-aprendidas" }, cascada);

            Assert.IsFalse(kanban.Contains("planning") || kanban.Contains("review") || kanban.Contains("retro"), "Kanban no tiene sprints: ni planificación ni revisión de sprint");
            Assert.IsFalse(cascada.Contains("daily") || cascada.Contains("retro") || cascada.Contains("planning"), "Cascada no tiene reunión diaria ni retrospectiva");
            Assert.IsFalse(scrum.Contains("firma-de-etapa") || scrum.Contains("kickoff") || scrum.Contains("reposicion"), "Scrum no tiene compuertas de etapa");
        }

        [Test]
        public void Toda_ceremonia_jugable_explica_para_que_sirve_y_tiene_una_forma_buena_y_una_tentacion() {
            foreach (var m in Catalogo().Metodologias.Values)
                foreach (var c in m.Ceremonias) {
                    Assert.IsFalse(string.IsNullOrEmpty(c.ParaQueSirve), $"{m.Id}/{c.Id}: no dice para qué sirve");
                    if (c.Opciones.Count == 0) continue;
                    Assert.GreaterOrEqual(c.Opciones.Count, 2, $"{m.Id}/{c.Id}");
                    foreach (var o in c.Opciones) Assert.Greater(o.Acta.Length, 40, $"{m.Id}/{c.Id}/{o.Id}: el acta tiene que explicar el porqué");
                }
        }

        // ================================================================ ceremonias

        [Test]
        public void Asistir_a_una_ceremonia_cuesta_minutos_aplica_su_efecto_y_deja_acta() {
            var s = Empezar("nivel-01", "scrum");
            EmpezarDia(s);
            var daily = s.CeremoniasDeHoy().First(c => c.Id == "daily");
            Assert.IsNull(s.PorQueNoSePuedeAsistir("daily"));
            var minuto = s.MinutoDelDia;
            var moral = s.W.MoralEquipo;

            s.AsistirACeremonia("daily", "bloqueos");
            Assert.AreEqual(minuto + daily.DuracionMinutos, s.MinutoDelDia, "asistir cuesta su tiempo");
            Assert.Greater(s.W.MoralEquipo, moral);
            Assert.Greater(MotorDelTablero.FactorDelDia(s.Tablero), 1.0, "llevarla bien se nota hoy en el ritmo del equipo");
            Assert.IsNotNull(s.PorQueNoSePuedeAsistir("daily"), "una vez al día");
            var acta = s.R.ActasDeCeremonias.Last();
            Assert.IsTrue(acta.Asistio);
            Assert.AreEqual("daily", acta.CeremoniaId);
            Assert.IsNotEmpty(acta.Texto);
        }

        [Test]
        public void No_asistir_no_castiga_pero_queda_en_el_acta_y_el_dia_siguiente_vuelve_a_haber_reunion() {
            var s = Empezar("nivel-01", "kanban");
            EmpezarDia(s);
            Assert.IsTrue(s.CeremoniasDeHoy().Any(c => c.Id == "daily-de-tablero"));
            s.AvanzarReloj(120);
            Assert.AreEqual(1.0, MotorDelTablero.FactorDelDia(s.Tablero), "sin asistir, ni bono ni castigo");
            CerrarDia(s);
            Assert.IsTrue(s.R.ActasDeCeremonias.Any(a => a.Dia == 1 && a.CeremoniaId == "daily-de-tablero" && !a.Asistio));
            EmpezarDia(s);
            Assert.IsFalse(s.CeremoniasDeHoy().First(c => c.Id == "daily-de-tablero").Hecha);
        }

        [Test]
        public void Llevar_mal_una_ceremonia_cuesta() {
            var s = Empezar("nivel-01", "scrum");
            EmpezarDia(s);
            s.AsistirACeremonia("daily", "diseno");
            Assert.Less(MotorDelTablero.FactorDelDia(s.Tablero), 1.0, "una reunión diaria de 45 minutos frena al equipo");
        }

        [Test]
        public void La_agenda_dice_que_ceremonias_hay_cada_dia_del_nivel() {
            var s = Empezar("nivel-01", "cascada");
            var agenda = s.AgendaDeCeremonias();
            Assert.AreEqual(1, agenda.Count(c => c.Id == "kickoff"));
            Assert.AreEqual(1, agenda.First(c => c.Id == "kickoff").Dia, "el kickoff es el primer día");
            Assert.AreEqual(s.Perfil.DiasTotales, agenda.First(c => c.Id == "lecciones-aprendidas").Dia, "y las lecciones, el último");
            Assert.AreEqual(4, agenda.Count(c => c.Id == "firma-de-etapa"), "una compuerta por etapa");
        }

        // ================================================================ Scrum

        [Test]
        public void En_scrum_lo_comprometido_son_las_primeras_tarjetas_del_backlog_y_el_burndown_baja_al_terminarlas() {
            var s = Empezar("nivel-01", "scrum");
            EmpezarDia(s);
            var sprint = s.Tablero.Tarjetas.Where(c => c.Sprint == 0).ToList();
            Assert.IsNotEmpty(sprint, "planificar llena el sprint backlog");
            Assert.Less(sprint.Count, s.Tablero.Tarjetas.Count, "y deja el resto en el product backlog");
            Assert.AreEqual(sprint.Max(c => c.Orden) + 1, sprint.Count, "son las primeras, en su orden");
            Assert.AreEqual(1, s.R.BurndownDelSprint.Count);
            var comprometido = s.R.BurndownDelSprint[0];
            Assert.That(comprometido, Is.GreaterThanOrEqualTo(s.R.CompromisoActual - 0.01), "lo marcado cubre lo prometido");

            CerrarDia(s);
            EmpezarDia(s);
            CerrarDia(s);
            Assert.AreEqual(3, s.R.BurndownDelSprint.Count, "un punto por día cerrado");
            Assert.Less(s.R.BurndownDelSprint.Last(), comprometido, "lo que queda del sprint baja");
        }

        [Test]
        public void Priorizar_antes_de_planificar_decide_que_entra_al_sprint() {
            var s = Empezar("nivel-01", "scrum");
            s.ComenzarDia();
            var ultima = s.Tablero.Tarjetas.OrderBy(c => c.Orden).Last();
            Assert.IsNull(s.PorQueNoSePuedePriorizar(ultima.Id, true));
            var minuto = s.MinutoDelDia;
            for (var i = 0; i < 40 && s.PorQueNoSePuedePriorizar(ultima.Id, true) == null; i++) s.PriorizarTarjeta(ultima.Id, true);
            // Sube hasta quedar la primera de lo que aun no se ha empezado: lo que el equipo ya tiene entre manos no se desplaza.
            Assert.IsTrue(s.Tablero.Tarjetas.Where(x => !x.Empezada).All(x => x.Orden >= ultima.Orden), "es la primera de lo no empezado");
            Assert.LessOrEqual(ultima.Orden, s.Tablero.Miembros.Count);
            Assert.Greater(s.MinutoDelDia, minuto, "priorizar cuesta minutos");
            s.Comprometer(s.PendingPlanning.CapacidadSugerida);
            Assert.AreEqual(0, ultima.Sprint, "y por eso entra en el sprint");
        }

        [Test]
        public void Meter_algo_a_mitad_de_sprint_se_puede_y_cuesta_moral_y_sobrecompromiso() {
            var s = Empezar("nivel-01", "scrum");
            EmpezarDia(s);
            var fuera = s.Tablero.Tarjetas.First(c => c.Sprint != 0);
            var moral = s.W.MoralEquipo;
            var sobre = s.R.SobreCompromiso;
            s.MeterAlSprint(fuera.Id);
            Assert.AreEqual(0, fuera.Sprint);
            Assert.Less(s.W.MoralEquipo, moral);
            Assert.Greater(s.R.SobreCompromiso, sobre, "y el sobrecompromiso genera deuda cada día hasta cerrar el sprint");
            Assert.IsNotNull(s.PorQueNoSePuedeAjustarElWip(1), "en Scrum no hay límite de WIP que mover");
            Assert.IsNotNull(s.PorQueNoSePuedeInspeccionar("SRS"), "ni documentos de etapa");
        }

        // ================================================================ Kanban

        [Test]
        public void En_kanban_el_limite_de_wip_es_del_jugador_y_moverlo_cambia_el_flujo() {
            var s = Empezar("nivel-01", "kanban");
            EmpezarDia(s);
            var limite = s.R.LimiteWip;
            Assert.AreEqual(limite, MotorDelTablero.EnCurso(s.Tablero), "el límite es cuántas tarjetas se llevan a la vez");
            Assert.AreEqual(1.0, MotorDelTablero.FactorDelWip(s.Tablero), "sin tocarlo, neutro");
            Assert.AreEqual(-1, s.Tablero.Tarjetas.Max(c => c.Sprint), "no hay sprint");
            Assert.IsNotNull(s.PorQueNoSePuedeMeterAlSprint(s.Tablero.Tarjetas[0].Id), "ni forma de meter nada en uno");

            s.AjustarLimiteWip(-1);
            Assert.AreEqual(limite - 1, s.R.LimiteWip);
            Assert.Greater(MotorDelTablero.FactorDelWip(s.Tablero), 1.0, "un paso menos enfoca al equipo");

            s.AjustarLimiteWip(1);
            s.AjustarLimiteWip(1);
            s.AjustarLimiteWip(1);
            Assert.Less(MotorDelTablero.FactorDelWip(s.Tablero), 1.0, "más cosas a medias, todo más lento");
            var veces = s.R.VecesExcedioWip;
            CerrarDia(s);
            Assert.AreEqual(veces + 1, s.R.VecesExcedioWip, "y cuenta como haber excedido el límite ese día");
        }

        [Test]
        public void La_revision_del_flujo_que_baja_el_wip_lo_baja_de_verdad() {
            var s = Empezar("nivel-01", "kanban");
            var limite = s.R.LimiteWip;
            for (var d = 0; d < 5; d++) {
                s.ComenzarDia();
                if (s.PendingRetro != null) {
                    s.ElegirAccionRetro("bajar-wip");
                    Assert.AreEqual(limite - 1, s.R.LimiteWip);
                    Assert.AreEqual(limite - 1, s.Tablero.LimiteEnCurso);
                    return;
                }
                CerrarDia(s);
            }
            Assert.Fail("en cinco días tenía que haber una revisión del flujo");
        }

        // ================================================================ Cascada

        [Test]
        public void En_cascada_no_se_prueba_hasta_la_etapa_de_pruebas_y_antes_se_inspecciona_el_documento() {
            var s = Empezar("nivel-01", "cascada");
            EmpezarDia(s);
            Assert.AreEqual("analisis", s.EtapaDeHoy().Id);
            StringAssert.Contains("etapa", s.PorQueNoSePuedeProbar());
            CollectionAssert.AreEqual(new[] { "SRS" }, s.DocumentosDeHoy());
            Assert.Greater(s.R.Artefactos["SRS"].Completitud, 0, "el documento de la etapa avanza cada día");

            var doc = s.W.Documentacion;
            var calidad = s.R.Artefactos["SRS"].Calidad;
            s.InspeccionarDocumento("SRS");
            Assert.Greater(s.W.Documentacion, doc);
            Assert.Greater(s.R.Artefactos["SRS"].Calidad, calidad);
            Assert.IsNotNull(s.PorQueNoSePuedeInspeccionar("SRS"), "una inspección al día");
            Assert.IsNotNull(s.PorQueNoSePuedeInspeccionar("PTP"), "y solo el documento de esta etapa");

            while (s.EtapaDeHoy().Id != "pruebas") { CerrarDia(s); EmpezarDia(s); }
            Assert.AreEqual(100, s.R.Artefactos["SRS"].Completitud, 0.5, "al cerrar su etapa el documento está completo");
            Assert.IsNull(s.PorQueNoSePuedeProbar(), "en su etapa, sí");
            var cobertura = s.W.Cobertura;
            s.EjecutarPruebas();
            Assert.Greater(s.W.Cobertura, cobertura);
        }

        [Test]
        public void Las_etapas_de_cascada_se_ajustan_a_un_nivel_corto() {
            // Antes, un nivel de cinco dias con etapas que suman veinte no salia nunca de la primera.
            var s = Empezar("nivel-00", "cascada");
            var etapas = new List<string>();
            while (s.R.Fase == 2) { EmpezarDia(s); etapas.Add(s.EtapaDeHoy().Id); CerrarDia(s); }
            CollectionAssert.AreEquivalent(new[] { "analisis", "diseno", "codificacion", "pruebas" }, etapas.Distinct().ToList());
            Assert.AreEqual("pruebas", etapas.Last());
        }

        [Test]
        public void En_scrum_y_en_kanban_se_prueba_cuando_se_quiera_una_vez_al_dia() {
            foreach (var m in new[] { "scrum", "kanban" }) {
                var s = Empezar("nivel-01", m);
                EmpezarDia(s);
                Assert.IsNull(s.PorQueNoSePuedeProbar(), m);
                var minuto = s.MinutoDelDia;
                var cobertura = s.W.Cobertura;
                s.EjecutarPruebas();
                Assert.AreEqual(minuto + GameSession.MinutosDeProbar, s.MinutoDelDia, m);
                Assert.Greater(s.W.Cobertura, cobertura, m);
                Assert.IsNotNull(s.PorQueNoSePuedeProbar(), m + ": una ronda al día");
            }
        }

        // ================================================================ asignar y revisar

        [Test]
        public void Asignar_a_quien_se_le_da_bien_hace_avanzar_mas_ese_dia() {
            Func<bool, double> avanceDelDia = bien => {
                var s = Empezar("nivel-01", "kanban", 5);
                EmpezarDia(s);
                s.AvanzarReloj(30);
                foreach (var c in s.Tablero.Tarjetas.Where(x => x.Columna == ColumnasDeBase.Haciendo).ToList()) {
                    var quien = bien ? s.Tablero.Miembros.OrderByDescending(m => m.HabilidadPara(c.Tipo)).First()
                                     : s.Tablero.Miembros.OrderBy(m => m.HabilidadPara(c.Tipo)).First();
                    if (s.PorQueNoSePuedeAsignar(c.Id, quien.Id) == null) s.AsignarTarjeta(c.Id, quien.Id);
                }
                var antes = s.W.Avance;
                CerrarDia(s);
                return s.W.Avance - antes;
            };
            var bueno = avanceDelDia(true);
            var malo = avanceDelDia(false);
            Assert.Greater(bueno, malo, "asignar importa");
            Assert.LessOrEqual(bueno / malo, MotorDelTablero.FactorMaximo / MotorDelTablero.FactorMinimo + 0.01, "pero dentro de la banda: no sustituye al motor");
        }

        [Test]
        public void Revisar_un_cambio_con_defecto_y_aprobarlo_mete_deuda_y_cazarlo_la_baja() {
            Assert.GreaterOrEqual(Catalogo().MicroPrs.Count, 10);
            Assert.IsTrue(Catalogo().MicroPrs.Any(p => p.TieneDefecto) && Catalogo().MicroPrs.Any(p => !p.TieneDefecto), "si todos tienen defecto, «pedir cambios» siempre acierta");

            foreach (var veredicto in new[] { VeredictosDePr.Aprobar, VeredictosDePr.PedirCambios }) {
                var s = Empezar("nivel-01", "scrum", 8);
                EmpezarDia(s); CerrarDia(s); EmpezarDia(s);   // el dia 2 hay cambio que revisar
                var pr = s.PrPendiente;
                Assert.IsNotNull(pr, "un día de cada dos hay un cambio esperando revisión");
                var deuda = s.W.DeudaTecnica;
                var minuto = s.MinutoDelDia;
                ResultadoDeAvance avance;
                var r = s.RevisarPr(veredicto, out avance);
                Assert.AreEqual(minuto + GameSession.MinutosDeRevisarPr, s.MinutoDelDia);
                Assert.AreEqual(pr.TieneDefecto != (veredicto == VeredictosDePr.Aprobar), r.Acierto);
                if (pr.TieneDefecto && veredicto == VeredictosDePr.Aprobar) Assert.Greater(s.W.DeudaTecnica, deuda);
                if (pr.TieneDefecto && veredicto == VeredictosDePr.PedirCambios) Assert.LessOrEqual(s.W.DeudaTecnica, deuda);
                Assert.IsNull(s.PrPendiente, "revisado");
                Assert.IsNotEmpty(r.Explicacion);
            }
        }

        // ================================================================ las graficas

        [Test]
        public void Las_bandas_del_flujo_acumulado_suman_siempre_el_alcance() {
            var s = Empezar("nivel-01", "kanban");
            for (var d = 0; d < 6; d++) { EmpezarDia(s); CerrarDia(s); }
            Assert.AreEqual(6, s.R.SerieTerminado.Count);
            for (var i = 0; i < 6; i++)
                Assert.AreEqual(s.R.SerieAlcance[i], s.R.SeriePorHacer[i] + s.R.SerieEnCurso[i] + s.R.SerieTerminado[i], 0.05, "día " + (i + 1));
            Assert.Greater(s.R.SerieTerminado.Last(), s.R.SerieTerminado.First(), "lo terminado crece");

            var flujo = GraficasDelProyecto.Flujo(s.Tablero, s.R.DiaActual);
            Assert.Greater(flujo.Terminadas, 0);
            Assert.Greater(flujo.TiempoDeCiclo, 0);
        }

        [Test]
        public void La_curva_S_planificada_empieza_en_cero_termina_en_el_alcance_y_arranca_despacio() {
            var plan = GraficasDelProyecto.CurvaSPlanificada(40, 20);
            Assert.AreEqual(21, plan.Count);
            Assert.AreEqual(0, plan[0]);
            Assert.AreEqual(40, plan[20]);
            Assert.Less(plan[5], 40 * 5 / 20.0, "al principio va por debajo de la recta");
            Assert.Greater(plan[15], 40 * 15 / 20.0, "y al final, por encima");
            for (var i = 1; i < plan.Count; i++) Assert.GreaterOrEqual(plan[i], plan[i - 1]);
            Assert.Greater(GraficasDelProyecto.DesvioRespectoAlPlan(5, 40, 11, 20), 0, "ir por detrás del plan es un desvío positivo");
        }

        [TestCase("nivel-00", 5)]
        [TestCase("nivel-01", 10)]
        public void El_burndown_dura_lo_que_dura_el_sprint_en_ese_nivel_y_no_lo_que_declara_la_metodologia(string nivel, int dias) {
            // Scrum declara sprints de 10 dias; el tutorial dura 5. El eje del burndown llegaba hasta 10 en un nivel de 5.
            var s = Empezar(nivel, "scrum");
            EmpezarDia(s);
            Assert.AreEqual(dias, s.DiasDeLaUnidadActual());
            Assert.LessOrEqual(s.DiasDeLaUnidadActual(), s.Perfil.DiasTotales);
            while (s.R.Fase == 2) { CerrarDia(s); if (s.R.Fase == 2) EmpezarDia(s); }
            Assert.LessOrEqual(s.R.BurndownDelSprint.Count - 1, dias, "un punto por día del sprint, ni uno más");
        }

        [Test]
        public void El_burndown_ideal_va_de_lo_comprometido_a_cero() {
            var ideal = GraficasDelProyecto.BurndownIdeal(30, 10);
            Assert.AreEqual(11, ideal.Count);
            Assert.AreEqual(30, ideal[0]);
            Assert.AreEqual(0, ideal[10]);
            Assert.AreEqual(15, ideal[5]);
        }

        // ================================================================ no se rompe lo de antes

        [Test]
        public void Una_metodologia_sin_bloque_de_tablero_recibe_la_politica_de_su_calendario() {
            foreach (var tipo in new[] { TiposDeCalendario.Iterativo, TiposDeCalendario.Continuo, TiposDeCalendario.Secuencial }) {
                var m = new MethodologyProfile { Id = "inventada", Calendario = new Calendario { Tipo = tipo } };
                Assert.IsNull(m.Tablero);
                Assert.IsNotEmpty(m.PoliticaDeTablero.Vistas, tipo);
                Assert.IsTrue(m.PoliticaDeTablero.Tiene(VistasDeMonitoreo.Tablero), tipo);
            }
            Assert.IsTrue(PoliticaDeTablero.PorDefecto("continuo").LimiteDeWip);
            Assert.IsTrue(PoliticaDeTablero.PorDefecto("iterativo").SprintCerrado);
            Assert.IsTrue(PoliticaDeTablero.PorDefecto("secuencial").InspeccionDeDocumentos);
        }

        [Test]
        public void Lo_que_el_jugador_hace_con_el_tablero_viaja_en_el_guardado() {
            var s = Empezar("nivel-01", "scrum", 6);
            EmpezarDia(s);
            s.AsistirACeremonia("daily", "bloqueos");
            s.EjecutarPruebas();
            var nivel = Nexus.Core.Guardado.JsonDeGuardado.Deserializar<Nexus.Core.Guardado.NivelEnCurso>(
                Nexus.Core.Guardado.JsonDeGuardado.Serializar(s.Capturar()));
            var r = GameSession.Restaurar(Catalogo(), "nivel-01", Flags(), 6, nivel);

            Assert.IsTrue(r.CeremoniasDeHoy().First(c => c.Id == "daily").Hecha);
            Assert.IsNotNull(r.PorQueNoSePuedeProbar());
            Assert.AreEqual(s.R.ActasDeCeremonias.Count, r.R.ActasDeCeremonias.Count);
            Assert.AreEqual(s.Tablero.BonoDeFlujoHoy, r.Tablero.BonoDeFlujoHoy, 1e-9);
            Assert.AreEqual(s.Tablero.SprintActual, r.Tablero.SprintActual);
            CollectionAssert.AreEqual(s.R.BurndownDelSprint, r.R.BurndownDelSprint);
            CollectionAssert.AreEqual(s.Tablero.Tarjetas.Select(c => c.Sprint), r.Tablero.Tarjetas.Select(c => c.Sprint));
        }
    }
}
