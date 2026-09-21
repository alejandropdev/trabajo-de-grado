using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Evaluacion;
using Nexus.Core.Eventos;
using Nexus.Core.Guardado;
using Nexus.Core.Metodologia;
using Nexus.Core.Minijuegos;
using Nexus.Core.Modelo;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using NUnit.Framework;
// Hay dos EfectoDiferido: el de la escena (float) y el del motor (object). Aqui se usa el del motor.
using EfectoDelMotor = Nexus.Core.Eventos.EfectoDiferido;

namespace Nexus.Tests {
    /// <summary>
    /// El hub. Aqui se comprueba que los nueve subsistemas juegan juntos, y sobre todo INV-7:
    /// recargar no puede cambiar la partida. Es la invariante que impide que el jugador esquive
    /// una consecuencia que no le gusto, y la que hace que el "modo aula" sea un instrumento fiable.
    /// </summary>
    public class GameSessionTests {
        private const double Tol = 1e-9;

        // ================================================================ catalogo de prueba

        private static EventDefinition Ev(string id, string tag, params string[] soloMetodologias) {
            return new EventDefinition {
                Id = id, Nombre = "Evento " + id,
                Tags = new List<string> { tag },
                Fases = new List<string> { FasesDelNivel.Desarrollo },
                Severidad = 3, ObjetivoAprendizaje = "OA-DEVOPS-02",
                PesoBase = 10, Enfriamiento = 5, MaxOcurrencias = 1,
                SoloMetodologias = new List<string>(soloMetodologias),
                Telegrafiado = new Telegrafiado { DiasAntes = 2, Canal = "log", Texto = "aviso de " + id },
                Opciones = {
                    new OpcionEvento {
                        Id = "A", Texto = "Pararlo y arreglarlo",
                        EfectosInmediatos = { { "Dias", 1 }, { "DeudaTecnica", -4 } },
                        EfectosDiferidos = { new EfectoDelMotor { EnDias = 4, Efectos = { { "Cobertura", 3 } } } },
                        Rubrica = new RubricaOpcion { Veredicto = "correcta", Oa = "OA-DEVOPS-02", Razon = "Es lo canonico." }
                    },
                    new OpcionEvento {
                        Id = "B", Texto = "Seguir y ya lo vemos",
                        EfectosInmediatos = { { "DeudaTecnica", 10 } },
                        EfectosDiferidos = { new EfectoDelMotor { EnDias = 5, Efectos = { { "MoralEquipo", -5 } } } },
                        Rubrica = new RubricaOpcion { Veredicto = "incorrecta", Oa = "OA-DEVOPS-02", Razon = "Lo esconde." }
                    }
                }
            };
        }

        private static EventDefinition EventoDeAlcance() {
            var ev = Ev("EV-ALC-01", "alcance");
            ev.EsCambioDeAlcance = true;
            ev.Opciones[0] = new OpcionEvento {
                Id = "A", Texto = "Aceptar el cambio", AmpliaAlcance = true,
                EfectosInmediatos = { { "Alcance", 6 }, { "Dias", 1 } },
                EfectosDiferidos = { new EfectoDelMotor { EnDias = 3, Efectos = { { "DeudaTecnica", 4 } } } },
                Rubrica = new RubricaOpcion { Veredicto = "aceptable", Oa = "OA-ALC-01", Razon = "Depende de cuando." }
            };
            return ev;
        }

        private static LevelProfile Nivel() {
            var p = new LevelProfile {
                Id = "nivel-01", Nombre = "Cradle Lifts",
                Briefing = new[] { "Veinte dias." },
                DiasTotales = 20, PresupuestoInicial = 18000, AlcanceInicial = 34, EquipoInicial = 3,
                CoberturaHeredada = 40, DocumentacionHeredada = 40,
                VelocidadBase = 3.0, VolatilidadReal = 25,
                ObjetivosActivos = new[] { "OA-GIT-01", "OA-DEVOPS-02", "OA-ALC-01" },
                MetodologiasPermitidas = new[] { "scrum", "kanban" },
                NivelAndamiaje = 3
            };
            p.Director.PresupuestoDrama = new[] { 1, 5, 2, 1 };
            p.Director.PesosPorTag["tecnico"] = 1.0;
            p.Director.PesosPorTag["seguridad"] = 1.0;
            p.Director.EnfriamientoGlobal = 2;
            p.Umbrales.Exito.AvanceMinimo = 20;

            p.Fase1.Calidad.Fichas = 8;
            p.Fase1.Calidad.Atributos.Add(new AtributoCalidad {
                Id = "seguridad", Nombre = "Seguridad", TagAfectado = "seguridad"
            });
            p.Fase1.Calidad.Atributos.Add(new AtributoCalidad {
                Id = "fiabilidad", Nombre = "Fiabilidad", TagAfectado = "tecnico", CoeficienteAfectado = "alpha"
            });
            p.Fase1.RazonesDisponibles.Add(new RazonOpcion { Id = "dominio_pequeno", Texto = "El dominio es pequeno" });
            p.Fase1.RazonesDisponibles.Add(new RazonOpcion { Id = "es_lo_moderno", Texto = "Es lo que se usa ahora" });
            p.Fase1.Arquitecturas.Add(new ArquitecturaOpcion {
                Id = "monolito", Nombre = "Monolito", EsLaAdecuada = true,
                Veredicto = "correcta", Razon = "Tres patios no necesitan microservicios.",
                Efectos = { { "Documentacion", 5 } }, ModificadoresModelo = { { "alpha", 0.9 } },
                RazonesValidas = { "dominio_pequeno" }, RazonesTrampa = { "es_lo_moderno" }
            });
            p.Fase1.Arquitecturas.Add(new ArquitecturaOpcion {
                Id = "micro", Nombre = "Microservicios", EsLaAdecuada = false,
                Veredicto = "incorrecta", Razon = "Confunde moda con criterio.",
                Efectos = { { "DeudaTecnica", 10 } }, ModificadoresModelo = { { "alpha", 1.3 } },
                RazonesTrampa = { "es_lo_moderno" }
            });
            return p;
        }

        private static MethodologyProfile Scrum() {
            return new MethodologyProfile {
                Id = "scrum", Nombre = "Scrum", Familia = FamiliasDeMetodologia.Agil,
                Resumen = "Iteraciones cortas.",
                RazonesValidas = { "dominio_pequeno" }, RazonesTrampa = { "es_lo_moderno" },
                Calendario = new Calendario { Tipo = TiposDeCalendario.Iterativo, EtiquetaUnidad = "Sprint",
                                              LongitudIteracion = 10, Iteraciones = 2 },
                Ceremonias = {
                    new Ceremonia { Id = "planning", Nombre = "Planning", Cuando = CuandoAplica.InicioIteracion,
                                    CosteDias = 0.5, Verbo = "V5", AbreVentanaDeCambio = true },
                    new Ceremonia { Id = "retro", Nombre = "Retrospectiva", Cuando = CuandoAplica.FinIteracion,
                                    CosteDias = 0.5, AjustaCoeficiente = true,
                                    Acciones = { new AccionRetro { Id = "documentar", Coeficiente = "kappa",
                                                                   Multiplicador = 0.85, Texto = "Documentar",
                                                                   Explicacion = "La documentacion se diluye mas despacio." } } }
                },
                ReglasDeCambio = new ReglasDeCambio {
                    Ventanas = new List<string> { VentanasDeCambio.EntreIteraciones },
                    TextoEnVentana = "Entra en el sprint que viene.",
                    PenalizacionFueraDeVentana = new PenalizacionFueraDeVentana {
                        Permitido = true, CosteMultiplicador = 2.0,
                        EfectosExtra = { { "MoralEquipo", -4 } }, Texto = "Romper el sprint se paga."
                    }
                },
                ModificadoresModelo = { { "kappa", 0.9 } },
                TableroPrincipal = TablerosPrincipales.Burndown,
                Lanzamiento = new LanzamientoConfig { FactorRiesgo = 0.9, EntregaIncremental = true },
                RubricaCierre = { Practicas = { new PracticaEsperada {
                    Id = "ritmo", Descripcion = "Ritmo sostenible", Metrica = "diasConHorasExtra",
                    Comparador = "<=", Objetivo = 6,
                    RazonSiCumple = "Mantuviste el ritmo.", RazonSiFalla = "Quemaste al equipo." } } }
            };
        }

        private static MethodologyProfile Kanban() {
            return new MethodologyProfile {
                Id = "kanban", Nombre = "Kanban", Familia = FamiliasDeMetodologia.Agil,
                Resumen = "Flujo continuo.", RazonesValidas = { "dominio_pequeno" },
                Calendario = new Calendario { Tipo = TiposDeCalendario.Continuo, EtiquetaUnidad = "Flujo",
                                              LimiteWipInicial = 4 },
                ReglasDeCambio = new ReglasDeCambio {
                    Ventanas = new List<string> { VentanasDeCambio.Siempre }, ConsumeWip = true,
                    TextoEnVentana = "Entra, pero algo sale del tablero."
                },
                TableroPrincipal = TablerosPrincipales.Cfd,
                Lanzamiento = new LanzamientoConfig { FactorRiesgo = 1.0, EntregaIncremental = true },
                RubricaCierre = { Practicas = { new PracticaEsperada {
                    Id = "wip", Descripcion = "Respetar el WIP", Metrica = "vecesExcedioWip",
                    Comparador = "<=", Objetivo = 2,
                    RazonSiCumple = "Respetaste el tablero.", RazonSiFalla = "Metiste de mas." } } }
            };
        }

        private static List<DefinicionDeFlag> Censo() {
            return new List<DefinicionDeFlag> {
                D("FLG_DEUDA_MORAL", EjesDeFlag.Integridad, 0, 0, 20, noBaja: true),
                D("FLG_DEUDA_TECNICA", EjesDeFlag.Competencia, 0, 0, 100),
                D("FLG_MORAL_EQUIPO", EjesDeFlag.Relaciones, 60, 0, 100),
                D("FLG_SALUD", EjesDeFlag.Persona, 80, 0, 100),
                D("FLG_VIDA_EXTERNA", EjesDeFlag.Persona, 0, 0, 10),
                D("FLG_CALIDAD_ACUM", EjesDeFlag.Competencia, 0, 0, 100),
                D("FLG_REPUTACION", EjesDeFlag.Estado, 50, 0, 100),
                new DefinicionDeFlag { Id = "FLG_HORAS_EXTRA", Eje = EjesDeFlag.Estado, SinTecho = true,
                                       Descripcion = "Contador acumulado." }
            };
        }

        private static DefinicionDeFlag D(string id, string eje, double ini, double min, double max, bool noBaja = false) {
            return new DefinicionDeFlag { Id = id, Eje = eje, Inicial = ini, Min = min, Max = max,
                                          NoBaja = noBaja, Descripcion = id };
        }

        private static Catalogo Catalogo() {
            var c = new Catalogo {
                Eventos = { Ev("EV-TEC-01", "tecnico"), Ev("EV-SEG-01", "seguridad"),
                            Ev("EV-EQ-01", "equipo"), EventoDeAlcance() },
                Minijuegos = {
                    new MinigameDefinition { Id = "MJ-A", Verbo = Verbos.Detectar, Archivo = "minijuegos/MJ-A.json",
                                             ObjetivoAprendizaje = "OA-GIT-01", PresionDiegetica = "Javier espera",
                                             Reloj = 90, PesoBase = 10, Enfriamiento = 6 }
                },
                Beats = {
                    new NarrativeBeat { Id = "CIN-1.4", Nombre = "El Primer Glitch",
                                        Prioridad = PrioridadDeBeat.Obligatorio,
                                        Ventana = new VentanaDeBeat { DiaMin = 18, DiaMax = 20 } }
                },
                Flags = Censo()
            };
            c.Niveles["nivel-01"] = Nivel();
            c.Metodologias["scrum"] = Scrum();
            c.Metodologias["kanban"] = Kanban();
            return c;
        }

        private static FlagStore Flags(Dictionary<string, double> respaldo = null) {
            var store = new FlagStore(respaldo ?? new Dictionary<string, double>(StringComparer.Ordinal), Censo());
            store.Inicializar();
            return store;
        }

        /// <summary>Una sesion con la Fase 1 ya cerrada, lista para el bucle diario.</summary>
        private static GameSession Empezada(string metodologia = "scrum", int semilla = 4417,
                                            Catalogo catalogo = null, FlagStore flags = null) {
            var s = new GameSession(catalogo ?? Catalogo(), "nivel-01", flags ?? Flags(), semilla);
            s.ElegirMetodologia(metodologia, "dominio_pequeno");
            s.RepartirCalidad(new Dictionary<string, int> { { "seguridad", 0 }, { "fiabilidad", 8 } });
            s.ElegirArquitectura("monolito", "dominio_pequeno");
            s.CerrarFase1();
            return s;
        }

        /// <summary>
        /// Juega N dias siempre igual. Es el "jugador robot" con el que se comparan dos partidas:
        /// sin un jugador determinista, INV-7 no se puede comprobar.
        /// </summary>
        private static void Jugar(GameSession s, int dias) {
            for (var i = 0; i < dias; i++) {
                s.ComenzarDia();

                if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida + 4);

                if (s.Decision != null) {
                    var elegible = s.Decision.Opciones.FirstOrDefault(o => !o.Bloqueada);
                    if (elegible != null) s.ResolverDecision(elegible.Id);
                }

                if (s.Minijuego != null)
                    s.ResolverMinijuego(new ResultadoMinijuego {
                        MinijuegoId = s.Minijuego.MinijuegoId,
                        Resultado = ResultadosDeMinijuego.Parcial,
                        Rubrica = new Rubrica { Veredicto = "aceptable", Oa = "OA-GIT-01", Razon = "Casi." },
                        EfectosInmediatos = { { "DeudaTecnica", 3f } }
                    });

                if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0)
                    s.ElegirAccionRetro(s.PendingRetro.Acciones[0].Id);

                s.TerminarDia(s.R.DiaActual % 3 == 0);
            }
        }

        // ================================================================ FASE 1

        [Test]
        public void Elegir_metodologia_reescribe_los_coeficientes() {
            var s = new GameSession(Catalogo(), "nivel-01", Flags(), 4417);
            var kappaAntes = s.Coef.Kappa;

            s.ElegirMetodologia("scrum", "dominio_pequeno");

            Assert.AreEqual(kappaAntes * 0.9, s.Coef.Kappa, Tol);
            Assert.AreEqual("Scrum", s.Metodologia.Nombre);
            Assert.AreEqual(2, s.Reglas.NumeroDeUnidades);
        }

        [Test]
        public void Una_metodologia_que_el_nivel_no_permite_se_rechaza() {
            var s = new GameSession(Catalogo(), "nivel-01", Flags(), 4417);
            var ex = Assert.Throws<InvalidOperationException>(() => s.ElegirMetodologia("cascada", "dominio_pequeno"));
            StringAssert.Contains("cascada", ex.Message);
            CollectionAssert.AreEqual(new[] { "scrum", "kanban" }, s.MetodologiasDisponibles().Select(m => m.Id));
        }

        [Test]
        public void No_invertir_en_un_atributo_sube_el_peso_de_su_tag() {
            var s = new GameSession(Catalogo(), "nivel-01", Flags(), 4417);
            s.ElegirMetodologia("scrum", "dominio_pequeno");

            s.RepartirCalidad(new Dictionary<string, int> { { "seguridad", 0 }, { "fiabilidad", 8 } });

            Assert.AreEqual(2.0, s.Perfil.Director.PesosPorTag["seguridad"], Tol,
                            "cero fichas duplica el riesgo de ese tag: el jugador acaba de elegir que crisis sufrir");
            Assert.AreEqual(0.6, s.Perfil.Director.PesosPorTag["tecnico"], Tol, "y ocho fichas casi lo apagan");
        }

        [Test]
        public void Repartir_mas_fichas_de_las_que_hay_se_rechaza() {
            var s = new GameSession(Catalogo(), "nivel-01", Flags(), 4417);
            s.ElegirMetodologia("scrum", "dominio_pequeno");

            var ex = Assert.Throws<InvalidOperationException>(
                () => s.RepartirCalidad(new Dictionary<string, int> { { "seguridad", 5 }, { "fiabilidad", 9 } }));
            StringAssert.Contains("8", ex.Message);
        }

        [Test]
        public void Las_tres_decisiones_de_fase_1_quedan_en_la_traza_con_su_veredicto() {
            var s = new GameSession(Catalogo(), "nivel-01", Flags(), 4417);
            s.ElegirMetodologia("scrum", "es_lo_moderno");          // razon trampa
            s.RepartirCalidad(new Dictionary<string, int> { { "seguridad", 4 }, { "fiabilidad", 4 } });
            s.ElegirArquitectura("monolito", "dominio_pequeno");    // razon valida

            Assert.AreEqual(3, s.Traza.Entradas.Count);
            Assert.AreEqual(Veredictos.Incorrecta, s.Traza.Entradas[0].Veredicto,
                            "acertar por el motivo equivocado no puntua igual");
            Assert.AreEqual(Veredictos.Correcta, s.Traza.Entradas[1].Veredicto, "repartio las 8 fichas");
            Assert.AreEqual(Veredictos.Correcta, s.Traza.Entradas[2].Veredicto);
            Assert.AreEqual(45.0, s.W.Documentacion, Tol, "la arquitectura aplico su efecto");
        }

        [Test]
        public void CerrarFase1_sin_las_tres_decisiones_se_rechaza() {
            var s = new GameSession(Catalogo(), "nivel-01", Flags(), 4417);
            Assert.Throws<InvalidOperationException>(() => s.CerrarFase1());

            s.ElegirMetodologia("scrum", "dominio_pequeno");
            Assert.Throws<InvalidOperationException>(() => s.CerrarFase1());

            s.ElegirArquitectura("monolito", "dominio_pequeno");
            Assert.DoesNotThrow(() => s.CerrarFase1());
            Assert.Throws<InvalidOperationException>(() => s.ElegirArquitectura("micro", "es_lo_moderno"));
        }

        [Test]
        public void Al_cerrar_la_fase_1_se_anota_la_cobertura_del_diseno() {
            var s = Empezada();
            Assert.AreEqual(s.W.Cobertura, s.R.CoberturaAlCerrarDiseno, Tol);
            Assert.AreEqual(2, s.R.Fase);
        }

        // ================================================================ FASE 2

        [Test]
        public void ComenzarDia_avanza_el_dia_y_devuelve_el_brief() {
            var s = Empezada();
            var brief = s.ComenzarDia();

            Assert.AreEqual(1, brief.Dia);
            Assert.AreEqual(20, brief.DiasTotales);
            Assert.AreEqual("Sprint 1", brief.EtiquetaUnidad);
            Assert.AreEqual("burndown", brief.Tablero);
            Assert.IsNotNull(brief.Derivadas);
            Assert.Contains("Planning", brief.Ceremonias);
            Assert.IsNotNull(s.PendingPlanning, "el planning del dia 1 abre el verbo V5");
        }

        [Test]
        public void Las_ceremonias_cuestan_dias_de_proyecto() {
            var s = Empezada();
            var diasAntes = s.W.Dias;
            s.ComenzarDia();
            Assert.AreEqual(diasAntes + 0.5, s.W.Dias, Tol, "medio dia de planning es medio dia que no se programa");
        }

        [Test]
        public void El_sobrecompromiso_genera_deuda_todos_los_dias() {
            var conservador = Empezada();
            conservador.ComenzarDia();
            conservador.Comprometer(conservador.PendingPlanning.CapacidadSugerida);
            Assert.AreEqual(0.0, conservador.R.SobreCompromiso, Tol);
            conservador.TerminarDia(false);

            var optimista = Empezada();
            optimista.ComenzarDia();
            optimista.Comprometer(optimista.PendingPlanning.CapacidadSugerida + 10);
            Assert.AreEqual(10.0, optimista.R.SobreCompromiso, Tol);
            optimista.TerminarDia(false);

            Assert.Greater(optimista.W.DeudaTecnica, conservador.W.DeudaTecnica,
                           "prometer de mas no da error: da deuda, todos los dias hasta cerrar el sprint");
        }

        [Test]
        public void La_retro_cambia_un_coeficiente_de_verdad() {
            var s = Empezada();
            Jugar(s, 9);

            s.ComenzarDia();   // dia 10: fin de sprint, cae la retro
            Assert.IsNotNull(s.PendingRetro);

            var kappaAntes = s.Coef.Kappa;
            s.ElegirAccionRetro("documentar");

            Assert.AreEqual(kappaAntes * 0.85, s.Coef.Kappa, Tol,
                            "a partir de hoy la documentacion se diluye mas despacio, y para siempre");
            Assert.AreEqual(1, s.R.AccionesRetroElegidas);
        }

        [Test]
        public void TerminarDia_es_el_unico_sitio_donde_avanza_el_tiempo() {
            var s = Empezada();
            s.ComenzarDia();
            var avanceTrasComenzar = s.W.Avance;

            Assert.AreEqual(0.0, avanceTrasComenzar, Tol, "empezar el dia no produce nada");

            s.TerminarDia(false);
            Assert.Greater(s.W.Avance, 0, "solo las 18:00 mueven el proyecto");
        }

        [Test]
        public void Quedarse_avanza_mas_hoy_y_lo_cobra_durante_el_resto_de_la_partida() {
            var aCasa = Empezada();
            aCasa.ComenzarDia();
            aCasa.TerminarDia(false);

            var seQueda = Empezada();
            seQueda.ComenzarDia();
            seQueda.TerminarDia(true);

            Assert.Greater(seQueda.W.Avance, aCasa.W.Avance, "el +25 % es el señuelo");
            Assert.Greater(seQueda.W.DeudaTecnica, aCasa.W.DeudaTecnica);
            Assert.Greater(seQueda.W.Cansancio, aCasa.W.Cansancio);
            Assert.Less(seQueda.W.SaludJugador, aCasa.W.SaludJugador);

            Assert.AreEqual(1, seQueda.R.DiasConHorasExtra);
            Assert.AreEqual(1, aCasa.R.VecesQueSeFueACasa, "irse a casa desbloquea el interludio de esa noche");
            Assert.AreEqual(0, aCasa.R.DiasSeguidosTrabajando);
        }

        // ================================================================ decisiones

        private static GameSession HastaLaPrimeraDecision(out int dia, string metodologia = "scrum") {
            var s = Empezada(metodologia);
            for (dia = 1; dia <= 20; dia++) {
                s.ComenzarDia();
                if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
                if (s.Decision != null) return s;
                s.TerminarDia(false);
            }
            Assert.Fail("no salio ninguna decision en 20 dias");
            return null;
        }

        [Test]
        public void La_decision_llega_con_su_aviso_y_su_previsualizacion() {
            int dia;
            var s = HastaLaPrimeraDecision(out dia);

            Assert.IsNotNull(s.Decision.TextoAviso, "el evento recuerda el aviso que llego dias antes");
            Assert.AreEqual("log", s.Decision.Canal);
            Assert.AreEqual(2, s.Decision.Opciones.Count);

            var estadoAntes = s.W.ToString();
            var previsualizacion = s.Decision.Opciones[1].Previsualizacion;
            Assert.AreEqual(10.0, previsualizacion["DeudaTecnica"], Tol);
            Assert.AreEqual(estadoAntes, s.W.ToString(), "previsualizar no toca el estado");
        }

        [Test]
        public void Resolver_una_decision_aplica_lo_inmediato_y_encola_lo_diferido() {
            int dia;
            var s = HastaLaPrimeraDecision(out dia);
            var deudaAntes = s.W.DeudaTecnica;
            var eventoId = s.Decision.EventoId;

            s.ResolverDecision("B");

            Assert.AreEqual(deudaAntes + 10, s.W.DeudaTecnica, Tol);
            Assert.IsNull(s.Decision, "una decision al dia, y ya se tomo");
            Assert.AreEqual(1, s.R.Ocurrencias[eventoId]);

            var entrada = s.Traza.Entradas.Last();
            Assert.AreEqual(eventoId, entrada.Origen);
            Assert.AreEqual(Veredictos.Incorrecta, entrada.Veredicto);
            Assert.AreNotEqual(entrada.EstadoAntes, entrada.EstadoDespues, "la traza es cierta gracias a INV-2");
        }

        [Test]
        public void Un_cambio_de_alcance_pasa_por_la_metodologia() {
            // Scrum fuera de ventana: permitido, pero al doble de coste y con moral extra perdida.
            var catalogo = Catalogo();
            catalogo.Eventos = new List<EventDefinition> { EventoDeAlcance() };
            var s = Empezada("scrum", 4417, catalogo);

            for (var dia = 1; dia <= 20 && s.Decision == null; dia++) {
                s.ComenzarDia();
                if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
                if (s.Decision != null) break;
                s.TerminarDia(false);
            }

            Assert.IsNotNull(s.Decision, "el evento de alcance tenia que salir");
            Assert.IsTrue(s.Decision.EsCambioDeAlcance);
            Assert.IsNotNull(s.Decision.Veredicto, "la metodologia opina sobre cambiar el alcance hoy");

            var alcanceAntes = s.W.Alcance;
            var multiplicador = s.Decision.Veredicto.CosteMultiplicador;
            s.ResolverDecision("A");

            Assert.AreEqual(alcanceAntes + 6 * multiplicador, s.W.Alcance, Tol,
                            "el coste del cambio lo escala la metodologia");
        }

        [Test]
        public void Una_opcion_bloqueada_no_se_puede_elegir() {
            var catalogo = Catalogo();
            var evento = Ev("EV-REQ-01", "tecnico");
            evento.Opciones[0].Requisitos.Add("Cobertura > 90");
            catalogo.Eventos = new List<EventDefinition> { evento };

            var s = Empezada("scrum", 4417, catalogo);
            for (var dia = 1; dia <= 20 && s.Decision == null; dia++) {
                s.ComenzarDia();
                if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
                if (s.Decision != null) break;
                s.TerminarDia(false);
            }

            var bloqueada = s.Decision.Opciones.First(o => o.Id == "A");
            Assert.IsTrue(bloqueada.Bloqueada);
            Assert.IsNotNull(bloqueada.MotivoBloqueo);
            Assert.Throws<InvalidOperationException>(() => s.ResolverDecision("A"));
        }

        [Test]
        public void Resolver_un_minijuego_llega_al_estado_y_a_la_traza() {
            var s = Empezada();
            for (var dia = 1; dia <= 20 && s.Minijuego == null; dia++) {
                s.ComenzarDia();
                if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
                if (s.Minijuego != null) break;
                if (s.Decision != null) s.ResolverDecision("A");
                s.TerminarDia(false);
            }

            Assert.IsNotNull(s.Minijuego, "la ventana de las 15:00 tenia que abrirse alguna vez");
            Assert.AreEqual(3, s.Minijuego.NivelAndamiaje, "el andamiaje sale del nivel");

            var deudaAntes = s.W.DeudaTecnica;
            s.ResolverMinijuego(new ResultadoMinijuego {
                MinijuegoId = s.Minijuego.MinijuegoId, Resultado = ResultadosDeMinijuego.Parcial,
                Rubrica = new Rubrica { Veredicto = "aceptable", Oa = "OA-GIT-01", Razon = "Casi." },
                EfectosInmediatos = { { "DeudaTecnica", 5f } }
            });

            Assert.AreEqual(deudaAntes + 5, s.W.DeudaTecnica, Tol);
            Assert.AreEqual("MJ-A", s.Traza.Entradas.Last().Origen);
            Assert.AreEqual(1, s.Competencia.PorObjetivo["OA-GIT-01"].Aceptables);
        }

        // ================================================================ IStateContext

        [Test]
        public void El_contexto_expone_las_variables_de_sesion() {
            var s = Empezada();
            s.ComenzarDia();
            double v;

            Assert.IsTrue(s.TryGetValue("diasTotales", out v));
            Assert.AreEqual(20.0, v, Tol);
            Assert.IsTrue(s.TryGetValue("volatilidadReal", out v));
            Assert.AreEqual(25.0, v, Tol);
            Assert.IsTrue(s.TryGetValue("riesgoLatente", out v));
            Assert.Greater(v, 0);
            Assert.IsTrue(s.TryGetValue("retrasoRelativo", out v));
            Assert.IsTrue(s.TryGetValue("DeudaTecnica", out v), "y tambien los stocks");
            Assert.IsTrue(s.TryGetValue("diaActual", out v), "y los diez consultables del RuntimeState");
            Assert.IsFalse(s.TryGetValue("sprintActual", out v), "pero no lo interno");
        }

        [Test]
        public void diasDesde_de_un_evento_que_nunca_ocurrio_vale_999() {
            var s = Empezada();
            Assert.AreEqual(999.0, s.CallFunction("diasDesde", "EV-NUNCA"), Tol);
            Assert.AreEqual(0.0, s.CallFunction("ocurrencias", "EV-NUNCA"), Tol);
            Assert.Throws<InvalidOperationException>(() => s.CallFunction("inventada", "x"));
        }

        // ================================================================ FASE 3 y 4

        [Test]
        public void El_lanzamiento_sigue_la_formula_de_la_especificacion() {
            var s = Empezada();
            Jugar(s, 20);

            var resultado = s.EjecutarLanzamiento();

            Assert.AreEqual(Math.Round(Math.Min(100, s.W.Avance >= 0 ? resultado.RiesgoDeLanzamiento : 0), 1),
                            resultado.RiesgoDeLanzamiento);
            Assert.AreEqual(Math.Round(Math.Min(s.W.Avance, s.W.Alcance), 1), resultado.AlcanceEntregado, Tol);
            Assert.GreaterOrEqual(resultado.DefectosEscapados, 0);
            Assert.AreEqual(4, s.R.Fase);
        }

        [Test]
        public void Un_lanzamiento_fallido_cuesta_moral() {
            var catalogo = Catalogo();
            catalogo.Niveles["nivel-01"].Umbrales.Exito.AvanceMinimo = 9999;   // imposible
            var s = Empezada("scrum", 4417, catalogo);
            Jugar(s, 5);

            var moralAntes = s.W.MoralEquipo;
            var resultado = s.EjecutarLanzamiento();

            Assert.IsFalse(resultado.Exito);
            Assert.AreEqual(moralAntes - 8, s.W.MoralEquipo, Tol);
        }

        [Test]
        public void Cerrar_construye_el_dashboard_completo() {
            var s = Empezada();
            Jugar(s, 20);
            var reporte = s.Cerrar();

            Assert.AreEqual("nivel-01", reporte.NivelId);
            Assert.AreEqual(4417, reporte.Semilla);
            Assert.IsNotNull(reporte.Lanzamiento);
            Assert.IsNotNull(reporte.Metodologia);
            Assert.AreEqual("scrum", reporte.Metodologia.MetodologiaId);
            Assert.IsTrue(reporte.Metodologia.RazonValida, "eligio Scrum por una razon que lo sostiene");
            Assert.IsFalse(reporte.Metodologia.EraAdecuada, "pero la volatilidad real era 25: no encajaba");
            Assert.AreEqual(1, reporte.Metodologia.Practicas.Count);
            Assert.AreEqual("monolito", reporte.ArquitecturaElegida);
            Assert.AreEqual("correcta", reporte.ArquitecturaVeredicto);
            Assert.AreEqual(9, reporte.Metricas.Count, "las nueve metricas agregadas");
            Assert.Greater(reporte.Traza.Entradas.Count, 3);
            Assert.IsTrue(s.NivelTerminado);
            Assert.Throws<InvalidOperationException>(() => s.Cerrar());
        }

        [Test]
        public void El_puente_a_flags_ocurre_solo_dentro_de_Cerrar() {
            var respaldo = new Dictionary<string, double>(StringComparer.Ordinal);
            var s = Empezada("scrum", 4417, null, Flags(respaldo));

            Jugar(s, 20);

            Assert.AreEqual(0.0, respaldo["FLG_HORAS_EXTRA"], Tol,
                            "jugar veinte dias no toca los flags: INV-6 dice que el puente es uno solo");
            Assert.AreEqual(0.0, respaldo["FLG_DEUDA_TECNICA"], Tol);

            var reporte = s.Cerrar();

            Assert.AreEqual(Math.Round(s.W.DeudaTecnica), respaldo["FLG_DEUDA_TECNICA"], Tol);
            Assert.AreEqual(s.R.DiasConHorasExtra, respaldo["FLG_HORAS_EXTRA"], Tol);
            Assert.AreEqual(respaldo["FLG_SALUD"], reporte.Flags["FLG_SALUD"], Tol);
        }

        // ================================================================ ★ INV-7

        [Test]
        public void Diez_dias_seguidos_dan_lo_mismo_que_cinco_guardar_recargar_y_cinco() {
            // --- de un tiron ---
            var deUnTiron = Empezada();
            Jugar(deUnTiron, 10);

            // --- con parada tecnica: cinco dias, guardar, recargar y cinco mas ---
            var respaldo = new Dictionary<string, double>(StringComparer.Ordinal);
            var antes = Empezada("scrum", 4417, null, Flags(respaldo));
            Jugar(antes, 5);

            var partida = new SaveGame {
                Id = "p", PerfilId = "perfil",
                Partida = new DatosDePartida { Nombre = "x", Semilla = 4417, NivelActualId = "nivel-01" },
                Flags = respaldo,
                Nivel = antes.Capturar()
            };

            // el viaje completo por disco: serializar y volver
            var json = JsonDeGuardado.Serializar(partida);
            var recargada = JsonDeGuardado.Deserializar<SaveGame>(json);

            var despues = new FabricaDeSesion(Catalogo()).Restaurar(recargada, recargada.Nivel);
            Jugar(despues, 5);

            Assert.AreEqual(deUnTiron.W.ToString(), despues.W.ToString(),
                            "recargar no puede servir para esquivar una consecuencia");
            Assert.AreEqual(deUnTiron.R.DiaActual, despues.R.DiaActual);
            Assert.AreEqual(deUnTiron.R.DiasConHorasExtra, despues.R.DiasConHorasExtra);
            Assert.AreEqual(deUnTiron.Coef.Kappa, despues.Coef.Kappa, Tol, "las retros elegidas tambien viajan");
            Assert.AreEqual(deUnTiron.Traza.Entradas.Count, despues.Traza.Entradas.Count);

            var unTiron = deUnTiron.Cerrar();
            var conParada = despues.Cerrar();
            Assert.AreEqual(JsonDeGuardado.Serializar(unTiron), JsonDeGuardado.Serializar(conParada),
                            "el Dashboard de Lecciones tiene que salir identico");
        }

        [Test]
        public void Restaurar_no_reaplica_las_decisiones_de_la_fase_1() {
            var s = Empezada();
            Jugar(s, 3);
            var coberturaAntes = s.W.Cobertura;
            var documentacionAntes = s.W.Documentacion;

            var partida = new SaveGame {
                Id = "p", PerfilId = "perfil",
                Partida = new DatosDePartida { Nombre = "x", Semilla = 4417, NivelActualId = "nivel-01" },
                Flags = new Dictionary<string, double>(StringComparer.Ordinal),
                Nivel = s.Capturar()
            };
            var restaurada = new FabricaDeSesion(Catalogo()).Restaurar(partida, partida.Nivel);

            Assert.AreEqual(coberturaAntes, restaurada.W.Cobertura, Tol);
            Assert.AreEqual(documentacionAntes, restaurada.W.Documentacion, Tol,
                            "el +5 de documentacion del monolito ya estaba dentro de W: reaplicarlo " +
                            "daria documentacion gratis en cada recarga, sin dar error");
        }

        [Test]
        public void Restaurar_si_reaplica_los_pesos_de_calidad() {
            var s = Empezada();
            Jugar(s, 3);

            var partida = new SaveGame {
                Id = "p", PerfilId = "perfil",
                Partida = new DatosDePartida { Nombre = "x", Semilla = 4417, NivelActualId = "nivel-01" },
                Flags = new Dictionary<string, double>(StringComparer.Ordinal),
                Nivel = s.Capturar()
            };
            var restaurada = new FabricaDeSesion(Catalogo()).Restaurar(partida, partida.Nivel);

            Assert.AreEqual(2.0, restaurada.Perfil.Director.PesosPorTag["seguridad"], Tol,
                            "estos viven en el LevelProfile, que se recarga limpio del catalogo");
        }

        // ================================================================ determinismo

        [Test]
        public void La_misma_semilla_produce_la_misma_partida() {
            var a = Empezada("scrum", 4417);
            var b = Empezada("scrum", 4417);
            Jugar(a, 20);
            Jugar(b, 20);

            Assert.AreEqual(a.W.ToString(), b.W.ToString());
            Assert.AreEqual(JsonDeGuardado.Serializar(a.Cerrar()), JsonDeGuardado.Serializar(b.Cerrar()));
        }

        /// <summary>
        /// Un catalogo con margen de verdad: ocho eventos de peso parecido, sin enfriamiento global y
        /// con drama de sobra. Con el catalogo pequeño de los demas tests el calendario sale casi
        /// forzado — se agenda uno cada cuatro dias y acaban saliendo todos — y entonces la semilla
        /// no tiene nada que decidir.
        /// </summary>
        private static Catalogo CatalogoAmplio() {
            var c = Catalogo();
            c.Eventos = new List<EventDefinition>();

            var tags = new[] { "equipo", "cliente", "alcance", "calidad",
                               "devops", "integracion", "legacy", "gestion" };

            for (var i = 0; i < tags.Length; i++) {
                var ev = Ev("EV-" + tags[i].ToUpperInvariant(), tags[i]);
                // ★ Cada evento cuesta algo DISTINTO. Si todos costaran lo mismo, daria igual cual
                // saliera y el estado final seria identico con cualquier semilla: el test no podria
                // distinguir nada, por mucho que el azar si estuviera decidiendo.
                ev.Opciones[0].EfectosInmediatos["DeudaTecnica"] = -2 - i;
                ev.Opciones[0].EfectosInmediatos["MoralEquipo"] = 1 + i;
                ev.Opciones[1].EfectosInmediatos["DeudaTecnica"] = 4 + i * 3;
                c.Eventos.Add(ev);
            }

            c.Niveles["nivel-01"].Director.EnfriamientoGlobal = 0;
            c.Niveles["nivel-01"].Director.PresupuestoDrama = new[] { 1, 9, 2, 1 };
            return c;
        }

        [Test]
        public void Semillas_distintas_producen_partidas_distintas() {
            var a = Empezada("scrum", 4417, CatalogoAmplio());
            var b = Empezada("scrum", 9001, CatalogoAmplio());
            Jugar(a, 20);
            Jugar(b, 20);

            Assert.AreNotEqual(a.W.ToString(), b.W.ToString(),
                               "la semilla decide que crisis te toca: es lo que hace comparable el modo aula");
        }

        [Test]
        public void Con_el_mismo_catalogo_amplio_la_misma_semilla_sigue_dando_lo_mismo() {
            var a = Empezada("scrum", 4417, CatalogoAmplio());
            var b = Empezada("scrum", 4417, CatalogoAmplio());
            Jugar(a, 20);
            Jugar(b, 20);

            Assert.AreEqual(a.W.ToString(), b.W.ToString());
        }

        [Test]
        public void Dos_metodologias_producen_partidas_distintas() {
            var scrum = Empezada("scrum", 4417);
            var kanban = Empezada("kanban", 4417);
            Jugar(scrum, 20);
            Jugar(kanban, 20);

            Assert.AreNotEqual(scrum.W.ToString(), kanban.W.ToString(),
                               "si dos metodologias dieran lo mismo, elegir no significaria nada");

            var deScrum = scrum.Cerrar();
            var deKanban = kanban.Cerrar();
            Assert.AreEqual("burndown", scrum.Metodologia.TableroPrincipal);
            Assert.AreEqual("cfd", kanban.Metodologia.TableroPrincipal);
            Assert.AreNotEqual(deScrum.Metodologia.Practicas[0].Id, deKanban.Metodologia.Practicas[0].Id,
                               "y cada una se juzga con su propia rubrica");
        }

        [Test]
        public void Una_partida_completa_deja_una_traza_auditable() {
            var s = Empezada();
            Jugar(s, 20);
            var reporte = s.Cerrar();

            foreach (var entrada in reporte.Traza.Entradas) {
                Assert.IsTrue(Veredictos.EsValido(entrada.Veredicto), entrada.Origen);
                Assert.IsNotNull(entrada.EstadoAntes, entrada.Origen);
                Assert.IsNotNull(entrada.EstadoDespues, entrada.Origen);
            }

            Assert.Greater(reporte.Competencia.PorObjetivo.Count, 0, "algo se evaluo");
            CollectionAssert.IsEmpty(reporte.BeatsPerdidos, "el unico beat obligatorio no tenia precondiciones");
        }
    }
}
