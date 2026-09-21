using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Evaluacion;
using Nexus.Core.Eventos;
using Nexus.Core.Guardado;
using Nexus.Core.Jornada;
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
                                       Descripcion = "Contador acumulado." },
                D("FLG_MARTA_ALIADA", EjesDeFlag.Estado, 0, 0, 1)
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
        /// Juega N dias siempre igual, respetando el dia continuo: avanza el reloj de 30 en 30,
        /// atiende cada alerta en cuanto suena (los fixtures de este archivo no declaran mapa, asi que
        /// el jugador esta siempre en su escritorio) y cierra la jornada en cuanto no queda nada.
        ///
        /// Es el "jugador robot" con el que se comparan dos partidas: sin un jugador determinista,
        /// INV-7 no se puede comprobar.
        /// </summary>
        private static void Jugar(GameSession s, int dias) {
            for (var i = 0; i < dias; i++) {
                s.ComenzarDia();

                if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida + 4);
                if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0)
                    s.ElegirAccionRetro(s.PendingRetro.Acciones[0].Id);

                RecorrerElDia(s);

                s.CerrarJornada();
                s.TerminarDia(s.R.DiaActual % 3 == 0);
            }
        }

        /// <summary>
        /// Avanza el reloj hasta que no quede nada pendiente hoy, atendiendo cada alerta apenas suena
        /// y resolviendo lo que se abra. El tope de 40 iteraciones (20h/30min) es una red de seguridad
        /// de test, no algo que el motor necesite: SePuedeCerrarLaJornada siempre se vuelve cierto
        /// porque toda alerta expira, como muy tarde, en el cierre.
        /// </summary>
        private static void RecorrerElDia(GameSession s) {
            for (var vueltas = 0; vueltas < 40 && !s.SePuedeCerrarLaJornada; vueltas++) {
                var avance = s.AvanzarReloj(30);

                foreach (var alerta in avance.AlertasQueSuenan)
                    s.AtenderAlerta(alerta.Id);

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
            RecorrerElDia(conservador);
            conservador.CerrarJornada();
            conservador.TerminarDia(false);

            var optimista = Empezada();
            optimista.ComenzarDia();
            optimista.Comprometer(optimista.PendingPlanning.CapacidadSugerida + 10);
            Assert.AreEqual(10.0, optimista.R.SobreCompromiso, Tol);
            RecorrerElDia(optimista);
            optimista.CerrarJornada();
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

            RecorrerElDia(s);
            Assert.AreEqual(0.0, s.W.Avance, Tol, "recorrer el dia y resolver alertas tampoco: solo el cierre mueve el proyecto");

            s.CerrarJornada();
            s.TerminarDia(false);
            Assert.Greater(s.W.Avance, 0, "solo las 18:00 mueven el proyecto");
        }

        [Test]
        public void No_se_puede_terminar_el_dia_antes_de_llegar_al_cierre() {
            var s = Empezada();
            s.ComenzarDia();
            var ex = Assert.Throws<InvalidOperationException>(() => s.TerminarDia(false));
            StringAssert.Contains("18:00", ex.Message);
        }

        [Test]
        public void Quedarse_avanza_mas_hoy_y_lo_cobra_durante_el_resto_de_la_partida() {
            var aCasa = Empezada();
            aCasa.ComenzarDia();
            RecorrerElDia(aCasa);
            aCasa.CerrarJornada();
            aCasa.TerminarDia(false);

            var seQueda = Empezada();
            seQueda.ComenzarDia();
            RecorrerElDia(seQueda);
            seQueda.CerrarJornada();
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

        /// <summary>
        /// Avanza dias hasta que se abra una decision. El dia queda A MEDIAS, con la decision viva, tal
        /// como hacia el helper original — solo que ahora "abrirse" significa que sono la alerta Y el
        /// jugador la atendio, no que ComenzarDia la presentara de golpe. Cualquier minijuego que
        /// aparezca por el camino se resuelve, para que no bloquee el cierre de un dia sin decision.
        /// </summary>
        private static void JugarHastaQueHayaUnaDecision(GameSession s, out int dia, int maxDias = 20) {
            for (dia = 1; dia <= maxDias; dia++) {
                s.ComenzarDia();
                if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
                if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0)
                    s.ElegirAccionRetro(s.PendingRetro.Acciones[0].Id);

                for (var vueltas = 0; vueltas < 40 && !s.SePuedeCerrarLaJornada; vueltas++) {
                    var avance = s.AvanzarReloj(30);
                    foreach (var alerta in avance.AlertasQueSuenan) s.AtenderAlerta(alerta.Id);

                    if (s.Decision != null) return;
                    if (s.Minijuego != null)
                        s.ResolverMinijuego(new ResultadoMinijuego {
                            MinijuegoId = s.Minijuego.MinijuegoId, Resultado = ResultadosDeMinijuego.Parcial,
                            Rubrica = new Rubrica { Veredicto = "aceptable", Oa = "OA-GIT-01", Razon = "Casi." },
                            EfectosInmediatos = { { "DeudaTecnica", 3f } }
                        });
                }

                s.CerrarJornada();
                s.TerminarDia(false);
            }
        }

        /// <summary>El espejo: avanza hasta que se abra un minijuego, resolviendo cualquier decision que salga antes.</summary>
        private static void JugarHastaQueHayaUnMinijuego(GameSession s, out int dia, int maxDias = 20) {
            for (dia = 1; dia <= maxDias; dia++) {
                s.ComenzarDia();
                if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
                if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0)
                    s.ElegirAccionRetro(s.PendingRetro.Acciones[0].Id);

                for (var vueltas = 0; vueltas < 40 && !s.SePuedeCerrarLaJornada; vueltas++) {
                    var avance = s.AvanzarReloj(30);
                    foreach (var alerta in avance.AlertasQueSuenan) s.AtenderAlerta(alerta.Id);

                    if (s.Minijuego != null) return;
                    if (s.Decision != null) {
                        var elegible = s.Decision.Opciones.FirstOrDefault(o => !o.Bloqueada);
                        if (elegible != null) s.ResolverDecision(elegible.Id);
                    }
                }

                s.CerrarJornada();
                s.TerminarDia(false);
            }
        }

        private static GameSession HastaLaPrimeraDecision(out int dia, string metodologia = "scrum") {
            var s = Empezada(metodologia);
            JugarHastaQueHayaUnaDecision(s, out dia);
            if (s.Decision == null) Assert.Fail("no salio ninguna decision en 20 dias");
            return s;
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

            int dia;
            JugarHastaQueHayaUnaDecision(s, out dia);

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
            int dia;
            JugarHastaQueHayaUnaDecision(s, out dia);

            var bloqueada = s.Decision.Opciones.First(o => o.Id == "A");
            Assert.IsTrue(bloqueada.Bloqueada);
            Assert.IsNotNull(bloqueada.MotivoBloqueo);
            Assert.Throws<InvalidOperationException>(() => s.ResolverDecision("A"));
        }

        [Test]
        public void Resolver_un_minijuego_llega_al_estado_y_a_la_traza() {
            var s = Empezada();
            int dia;
            JugarHastaQueHayaUnMinijuego(s, out dia);

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

        // ================================================================ ★ A'4: el dia continuo

        /// <summary>
        /// Avanza dias, jugando con normalidad, hasta que un dia empiece con al menos una alerta que
        /// cumpla 'filtro' — y deja la sesion justo tras ComenzarDia() de ese dia, sin tocar el reloj.
        ///
        /// Hace falta porque el DIA 1 nunca trae una decision: EventoDeHoy solo devuelve algo si un
        /// dia ANTERIOR ya lo agendo (§7.5), y el dia 1 no tiene anterior. Probar solo "el dia 1" con
        /// Assert.Ignore como salida no verificaria nada la mayoria de las veces; esto lo hace real.
        /// </summary>
        private static void HastaUnDiaConAlerta(GameSession s, out int dia, Func<Alerta, bool> filtro = null) {
            var pasa = filtro ?? (a => true);
            for (dia = 1; dia <= 20; dia++) {
                s.ComenzarDia();
                if (s.AlertasDeHoy.Any(pasa)) return;

                if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
                if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0)
                    s.ElegirAccionRetro(s.PendingRetro.Acciones[0].Id);
                RecorrerElDia(s);
                s.CerrarJornada();
                s.TerminarDia(false);
            }
            Assert.Fail("no salio ninguna alerta que cumpliera el filtro en 20 dias");
        }

        /// <summary>
        /// Una jornada de 08:00 a 11:00 con 3 h de ventana: el UNICO minuto en que cabe una alerta entera
        /// es el de entrada. Asi se prueba el borde sin depender de que el sorteo caiga ahi por suerte.
        /// </summary>
        private static Catalogo CatalogoConAlertasALasOcho() {
            var c = Catalogo();
            var jornada = c.Niveles["nivel-01"].Jornada;
            jornada.HoraInicio = 8;
            jornada.HoraCierre = 11;
            jornada.HoraLimite = 13;
            jornada.VentanaDeAtencionMinutos = 180;
            return c;
        }

        [Test]
        public void Una_alerta_a_la_hora_de_entrada_suena_en_el_primer_tramo_y_solo_una_vez() {
            // Antes del arreglo, el intervalo (desde, hasta] nunca contenia las 08:00: la alerta sonaba sin
            // avisar y expiraba como omitida (scrum, semilla 16, del contenido real de N1).
            var s = Empezada(catalogo: CatalogoConAlertasALasOcho());
            int dia;
            HastaUnDiaConAlerta(s, out dia);
            var alerta = s.AlertasDeHoy.First();
            Assert.AreEqual(8 * 60, alerta.MinutoDeLaAlerta, "con esta jornada no cabe en ningun otro minuto");

            var primerTramo = s.AvanzarReloj(15);
            CollectionAssert.Contains(primerTramo.AlertasQueSuenan, alerta, "la de las 08:00 tiene que sonar");

            var segundoTramo = s.AvanzarReloj(15);
            CollectionAssert.DoesNotContain(segundoTramo.AlertasQueSuenan, alerta, "suena una vez, no en cada tramo");
        }

        [Test]
        public void Avanzar_cero_minutos_a_las_ocho_no_gasta_el_aviso() {
            var s = Empezada(catalogo: CatalogoConAlertasALasOcho());
            int dia;
            HastaUnDiaConAlerta(s, out dia);
            var alerta = s.AlertasDeHoy.First();

            Assert.IsEmpty(s.AvanzarReloj(0).AlertasQueSuenan, "sin mover el reloj no suena nada");
            CollectionAssert.Contains(s.AvanzarReloj(15).AlertasQueSuenan, alerta,
                                      "y el aviso sigue ahi para el primer tramo que de verdad avanza");
        }

        [Test]
        public void Una_alerta_de_las_ocho_atendida_sin_mover_el_reloj_no_vuelve_a_sonar() {
            var s = Empezada(catalogo: CatalogoConAlertasALasOcho());
            int dia;
            HastaUnDiaConAlerta(s, out dia);
            var alerta = s.AlertasDeHoy.First();

            // AtenderAlerta cobra su tiempo con AvanzarReloj, que arranca justo a las 08:00.
            var resultado = s.AtenderAlerta(alerta.Id);

            CollectionAssert.DoesNotContain(resultado.AlertasQueSuenan, alerta,
                                            "ya estaba atendida: anunciarla ahora seria mentirle a la pantalla");
        }

        // ================================================================ contenido por nivel

        /// <summary>Juega los dias apuntando que cinematica sale cada uno.</summary>
        private static List<string> BeatsEmitidos(GameSession s, int dias) {
            var beats = new List<string>();
            for (var i = 0; i < dias; i++) {
                s.ComenzarDia();
                if (s.Beat != null) beats.Add(s.Beat.BeatId + "@" + s.R.DiaActual);
                if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
                if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0)
                    s.ElegirAccionRetro(s.PendingRetro.Acciones[0].Id);
                RecorrerElDia(s);
                s.CerrarJornada();
                s.TerminarDia(false);
            }
            return beats;
        }

        [Test]
        public void Un_evento_atado_a_otro_nivel_no_sale_nunca() {
            var ajeno = Catalogo();
            foreach (var ev in ajeno.Eventos) ev.SoloNiveles = new List<string> { "nivel-00" };
            var s = Empezada(catalogo: ajeno);
            Jugar(s, 20);
            Assert.IsFalse(s.LogDeEventos.Any(d => d.Resultado == DecisionDelDirector.Agendado),
                           "el catalogo de eventos es global, pero N1 no puede ver los del concurso");

            var propio = Catalogo();
            foreach (var ev in propio.Eventos) ev.SoloNiveles = new List<string> { "nivel-01" };
            var t = Empezada(catalogo: propio);
            Jugar(t, 20);
            Assert.IsTrue(t.LogDeEventos.Any(d => d.Resultado == DecisionDelDirector.Agendado),
                          "y los suyos si: si no, la prueba de arriba pasaria por no hacer nada");
        }

        [Test]
        public void Una_cinematica_de_otro_nivel_no_sale_y_la_suya_si() {
            var c = Catalogo();
            c.Beats.Add(new NarrativeBeat { Id = "TUT-0.2", Nombre = "Del concurso", Prioridad = PrioridadDeBeat.Obligatorio,
                                            Ventana = new VentanaDeBeat { DiaMin = 2, DiaMax = 2 },
                                            SoloNiveles = new List<string> { "nivel-00" } });
            c.Beats.Add(new NarrativeBeat { Id = "CIN-1.2", Nombre = "De N1", Prioridad = PrioridadDeBeat.Obligatorio,
                                            Ventana = new VentanaDeBeat { DiaMin = 3, DiaMax = 3 },
                                            SoloNiveles = new List<string> { "nivel-01" } });

            var beats = BeatsEmitidos(Empezada(catalogo: c), 5);

            CollectionAssert.Contains(beats, "CIN-1.2@3");
            Assert.IsFalse(beats.Any(b => b.StartsWith("TUT-0.2")), "una cinematica del N0 no puede salir en N1");
        }

        // ================================================================ elecciones de escena

        private static Catalogo CatalogoConEscena() {
            var c = Catalogo();
            c.Guiones.Add(new Guion {
                Id = "TUT-0.2", Titulo = "Marta", Nivel = "nivel-01", Momento = MomentosDeGuion.Dia,
                Variantes = { { "default", new List<LineaDeGuion> { new LineaDeGuion { Quien = "Marta", Texto = "¿Aliados?" } } } },
                Opciones = {
                    new OpcionDeGuion { Id = "aceptar", Texto = "Sí", Flag = "FLG_MARTA_ALIADA", Valor = 1 },
                    new OpcionDeGuion { Id = "rechazar", Texto = "No", Flag = "FLG_MARTA_ALIADA", Valor = 0 }
                }
            });
            return c;
        }

        [Test]
        public void Una_eleccion_de_escena_se_escribe_al_cerrar_y_no_antes() {
            var s = Empezada(catalogo: CatalogoConEscena());
            s.RegistrarEleccion("TUT-0.2", "aceptar");

            Jugar(s, 20);
            Assert.AreEqual(0, s.Flags.Get("FLG_MARTA_ALIADA"), "INV-6: ningun flag se escribe antes de Cerrar()");

            s.Cerrar();
            Assert.AreEqual(1, s.Flags.Get("FLG_MARTA_ALIADA"));
        }

        [Test]
        public void Una_eleccion_de_escena_no_se_deshace() {
            var s = Empezada(catalogo: CatalogoConEscena());
            s.RegistrarEleccion("TUT-0.2", "aceptar");
            Assert.Throws<InvalidOperationException>(() => s.RegistrarEleccion("TUT-0.2", "rechazar"));
        }

        [Test]
        public void Una_opcion_o_un_guion_que_no_existen_se_rechazan() {
            var s = Empezada(catalogo: CatalogoConEscena());
            Assert.Throws<InvalidOperationException>(() => s.RegistrarEleccion("TUT-0.2", "quizas"));
            Assert.Throws<InvalidOperationException>(() => s.RegistrarEleccion("CIN-9.9", "aceptar"));
        }

        [Test]
        public void La_eleccion_de_escena_sobrevive_a_guardar_y_recargar() {
            var respaldo = new Dictionary<string, double>(StringComparer.Ordinal);
            var s = Empezada("scrum", 4417, CatalogoConEscena(), Flags(respaldo));
            s.RegistrarEleccion("TUT-0.2", "aceptar");
            Jugar(s, 5);

            var partida = new SaveGame {
                Id = "p", PerfilId = "perfil",
                Partida = new DatosDePartida { Nombre = "x", Semilla = 4417, NivelActualId = "nivel-01" },
                Flags = respaldo,
                Nivel = s.Capturar()
            };
            var recargada = JsonDeGuardado.Deserializar<SaveGame>(JsonDeGuardado.Serializar(partida));
            var despues = new FabricaDeSesion(CatalogoConEscena()).Restaurar(recargada, recargada.Nivel);

            Assert.Throws<InvalidOperationException>(() => despues.RegistrarEleccion("TUT-0.2", "rechazar"),
                                                     "recargar no puede servir para cambiar lo que ya decidiste");
            Jugar(despues, 15);
            despues.Cerrar();
            Assert.AreEqual(1, despues.Flags.Get("FLG_MARTA_ALIADA"));
        }

        [Test]
        public void Una_condicion_puede_leer_un_flag_pero_nunca_escribirlo() {
            var s = Empezada(flags: Flags(new Dictionary<string, double>(StringComparer.Ordinal) { { "FLG_MARTA_ALIADA", 1 } }));

            double valor;
            Assert.IsTrue(s.TryGetValue("FLG_MARTA_ALIADA", out valor));
            Assert.AreEqual(1, valor);
            Assert.IsTrue(Nexus.Core.Servicios.ConditionEvaluator.Evaluar("FLG_MARTA_ALIADA == 1", s),
                          "es lo que da lector a los flags de color de dialogo");
        }

        /// <summary>Un mapa de dos zonas, con una puerta que solo se abre desde el dia 3.</summary>
        private static Catalogo CatalogoConMapa() {
            var c = Catalogo();
            c.Niveles["nivel-01"].Mapa = new MapaDeZonas {
                CosteBaseDeViaje = 20,
                Zonas = {
                    new ZonaDeNivel { Id = "escritorio", Nombre = "Tu escritorio", EsAncla = true,
                                      MinutosDeVisita = 10, QueDa = "Aqui llegan las alertas." },
                    new ZonaDeNivel { Id = "bullpen", Nombre = "Bullpen", MinutosDeVisita = 20,
                                      QueDa = "El equipo." },
                    new ZonaDeNivel { Id = "sala-comite", Nombre = "Sala del Comite", MinutosDeVisita = 20,
                                      QueDa = "Un miembro del Comite.",
                                      Puerta = new PuertaDeZona { Precondiciones = { "diaActual >= 3" } } }
                }
            };
            return c;
        }

        [Test]
        public void Sin_mapa_las_alertas_se_atienden_desde_cualquier_sitio() {
            var s = Empezada();   // Catalogo() no declara Mapa
            int dia;
            JugarHastaQueHayaUnaDecision(s, out dia);

            Assert.IsNull(s.ZonaActual, "sin mapa no hay 'donde estas': todo ocurre en el escritorio");
            Assert.IsNotNull(s.Decision, "AtenderAlerta no exigio ir a ningun sitio para llegar hasta aqui");
        }

        [Test]
        public void Con_mapa_las_alertas_solo_se_atienden_desde_el_escritorio() {
            var s = Empezada("scrum", 4417, CatalogoConMapa());
            int dia;
            HastaUnDiaConAlerta(s, out dia);
            s.IrAZona("bullpen");

            // Se avanza en pasos pequeños y se para en cuanto algo suena — saltar de golpe haria que
            // la alerta sonara Y expirara en la misma llamada, y entonces el mensaje que se comprueba
            // seria "ya expiro", no "solo se atienden desde tu escritorio".
            Alerta sonando = null;
            for (var i = 0; i < 40 && !s.SePuedeCerrarLaJornada && sonando == null; i++)
                sonando = s.AvanzarReloj(15).AlertasQueSuenan.FirstOrDefault();

            Assert.IsNotNull(sonando, "HastaUnDiaConAlerta garantiza que hoy suena algo");
            var ex = Assert.Throws<InvalidOperationException>(() => s.AtenderAlerta(sonando.Id));
            StringAssert.Contains("escritorio", ex.Message);
        }

        [Test]
        public void Ir_a_una_zona_cuesta_el_reloj_y_queda_registrado() {
            var s = Empezada("scrum", 4417, CatalogoConMapa());
            s.ComenzarDia();
            var minutoAntes = s.MinutoDelDia;

            s.IrAZona("bullpen");

            Assert.AreEqual(minutoAntes + 20 + 20, s.MinutoDelDia, "20 de viaje + 20 de la micro-escena");
            Assert.AreEqual("bullpen", s.ZonaActual);
            Assert.AreEqual(1, s.R.ZonasVisitadas["bullpen"]);
        }

        [Test]
        public void Una_zona_con_puerta_cerrada_no_se_puede_visitar() {
            var s = Empezada("scrum", 4417, CatalogoConMapa());
            s.ComenzarDia();   // dia 1: la puerta pide diaActual >= 3

            var ex = Assert.Throws<InvalidOperationException>(() => s.IrAZona("sala-comite"));
            StringAssert.Contains("sala-comite", ex.Message);
        }

        [Test]
        public void Una_zona_con_puerta_se_abre_cuando_se_cumple_la_condicion() {
            var s = Empezada("scrum", 4417, CatalogoConMapa());

            for (var dia = 1; dia < 3; dia++) {
                s.ComenzarDia();
                if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
                RecorrerElDia(s);
                s.CerrarJornada();
                s.TerminarDia(false);
            }
            s.ComenzarDia();   // dia 3: ahora "diaActual >= 3" se cumple

            Assert.DoesNotThrow(() => s.IrAZona("sala-comite"), "desde el dia 3 la puerta esta abierta");
        }

        [Test]
        public void No_se_puede_ir_a_una_zona_si_el_nivel_no_tiene_mapa() {
            var s = Empezada();   // Catalogo() sin Mapa
            s.ComenzarDia();
            var ex = Assert.Throws<InvalidOperationException>(() => s.IrAZona("lo-que-sea"));
            StringAssert.Contains("mapa", ex.Message);
        }

        [Test]
        public void Una_decision_que_expira_se_evalua_como_incorrecta_y_no_como_si_no_hubiera_pasado_nada() {
            var s = Empezada();
            int dia;
            HastaUnDiaConAlerta(s, out dia, a => a.Tipo == TiposDeAlerta.Decision);

            var expiro = false;
            for (var i = 0; i < 40 && !s.SePuedeCerrarLaJornada && !expiro; i++) {
                var avance = s.AvanzarReloj(30);   // nunca se atiende nada: se deja expirar todo
                if (avance.AlertasQueExpiraron.Any(a => a.Tipo == TiposDeAlerta.Decision)) expiro = true;
            }

            Assert.IsTrue(expiro, "HastaUnDiaConAlerta garantiza una decision hoy, y nunca se atendio");

            var entrada = s.Traza.Entradas.Last();
            Assert.AreEqual(Veredictos.Incorrecta, entrada.Veredicto);
            Assert.AreEqual("omitido", entrada.OpcionId);
            StringAssert.Contains("Alguien decidio por ti", entrada.Razon);
            Assert.IsNull(s.Decision, "la decision expirada no puede quedar abierta");
        }

        [Test]
        public void Un_minijuego_que_expira_tambien_produce_una_escena_no_una_puntuacion() {
            var s = Empezada();
            int dia;
            HastaUnDiaConAlerta(s, out dia, a => a.Tipo == TiposDeAlerta.Minijuego);

            Alerta expirada = null;
            for (var i = 0; i < 40 && !s.SePuedeCerrarLaJornada && expirada == null; i++) {
                var avance = s.AvanzarReloj(30);
                expirada = avance.AlertasQueExpiraron.FirstOrDefault(a => a.Tipo == TiposDeAlerta.Minijuego);
            }

            Assert.IsNotNull(expirada, "HastaUnDiaConAlerta garantiza un minijuego hoy, y nunca se atendio");

            var entrada = s.Traza.Entradas.Last();
            Assert.AreEqual(expirada.Id, entrada.Origen);
            Assert.AreEqual(Veredictos.Incorrecta, entrada.Veredicto);
            StringAssert.Contains("tambien es decidir", entrada.Razon);
            Assert.IsNull(s.Minijuego);
        }

        [Test]
        public void CerrarJornada_lanza_si_queda_algo_pendiente() {
            var s = Empezada();
            int dia;
            HastaUnDiaConAlerta(s, out dia);

            Assert.IsFalse(s.SePuedeCerrarLaJornada, "hay una alerta agendada para hoy que aun no sono ni se atendio");
            var ex = Assert.Throws<InvalidOperationException>(() => s.CerrarJornada());
            StringAssert.Contains("pendiente", ex.Message);
        }

        [Test]
        public void Guardar_y_recargar_a_mitad_del_dia_con_una_alerta_viva_da_lo_mismo() {
            // La version mas dura de INV-7: no entre dos dias, sino a MITAD de uno, con una alerta
            // sonada y sin atender todavia. Es exactamente lo que A4 añade sobre lo que M9 ya probaba.
            var deUnTiron = Empezada();
            deUnTiron.ComenzarDia();
            var avance = deUnTiron.AvanzarReloj(600);   // agota el dia entero de un tiron

            var respaldo = new Dictionary<string, double>(StringComparer.Ordinal);
            var conParada = Empezada("scrum", 4417, null, Flags(respaldo));
            conParada.ComenzarDia();
            conParada.AvanzarReloj(150);   // solo una parte del dia: dos horas y media

            var partida = new SaveGame {
                Id = "p", PerfilId = "perfil",
                Partida = new DatosDePartida { Nombre = "x", Semilla = 4417, NivelActualId = "nivel-01" },
                Flags = respaldo, Nivel = conParada.Capturar()
            };
            var recargada = JsonDeGuardado.Deserializar<SaveGame>(JsonDeGuardado.Serializar(partida));
            var restaurada = new FabricaDeSesion(Catalogo()).Restaurar(recargada, recargada.Nivel);

            Assert.AreEqual(conParada.MinutoDelDia, restaurada.MinutoDelDia,
                            "recargar no puede adelantar ni atrasar el reloj del dia");

            restaurada.AvanzarReloj(600 - 150);   // el resto del mismo dia

            Assert.AreEqual(deUnTiron.MinutoDelDia, restaurada.MinutoDelDia);
            Assert.AreEqual(deUnTiron.W.ToString(), restaurada.W.ToString());
            Assert.AreEqual(deUnTiron.Traza.Entradas.Count, restaurada.Traza.Entradas.Count,
                            "recargar a mitad de una alerta no puede borrarla ni servir para esquivarla");
        }

        [Test]
        public void Restaurar_recupera_el_evento_de_una_alerta_todavia_pendiente() {
            var s = Empezada();
            int dia;
            HastaUnDiaConAlerta(s, out dia);

            // Pasos pequeños: saltar de golpe arriesga sonar-y-expirar en la misma llamada, y entonces
            // no quedaria ninguna alerta VIVA que restaurar. Y "pendiente" no basta: una alerta agendada
            // para dentro de dos horas ya es Pendiente aunque todavia no haya SONADO.
            for (var i = 0; i < 40 && !s.SePuedeCerrarLaJornada &&
                            !s.AlertasDeHoy.Any(a => a.EstaPendiente && a.YaSono(s.MinutoDelDia)); i++)
                s.AvanzarReloj(15);

            var sonando = s.AlertasDeHoy.FirstOrDefault(a => a.EstaPendiente && a.YaSono(s.MinutoDelDia));
            Assert.IsNotNull(sonando, "HastaUnDiaConAlerta garantiza una alerta hoy, y suena antes del cierre");

            var partida = new SaveGame {
                Id = "p", PerfilId = "perfil",
                Partida = new DatosDePartida { Nombre = "x", Semilla = 4417, NivelActualId = "nivel-01" },
                Flags = new Dictionary<string, double>(StringComparer.Ordinal), Nivel = s.Capturar()
            };
            var restaurada = new FabricaDeSesion(Catalogo()).Restaurar(partida, partida.Nivel);

            // tras restaurar, atender la misma alerta tiene que seguir funcionando
            Assert.DoesNotThrow(() => restaurada.AtenderAlerta(sonando.Id));
        }
    }
}
