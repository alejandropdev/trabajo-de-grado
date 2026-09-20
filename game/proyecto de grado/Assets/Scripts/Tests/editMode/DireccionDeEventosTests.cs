using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Nexus.Core.Eventos;
using Nexus.Core.Guardado;
using Nexus.Core.Metodologia;
using Nexus.Core.Modelo;
using Nexus.Core.Servicios;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// C3 · Direccion de eventos. La invariante que define el subsistema es INV-3: el director elige
    /// y AVISA; no dispara nunca. Todo lo demas de aqui existe para que esa promesa se pueda comprobar.
    /// </summary>
    public class DireccionDeEventosTests {
        private const double Tol = 1e-9;

        /// <summary>Lo mismo que hara GameSession en M9: repartir el nombre entre los dos estados.</summary>
        private sealed class Ctx : IStateContext {
            public readonly WorldState W = new WorldState();
            public readonly RuntimeState R = new RuntimeState { Fase = 2, DiaActual = 1 };

            public bool TryGetValue(string nombre, out double valor) {
                return W.TryGet(nombre, out valor) || R.TryGet(nombre, out valor);
            }

            public double CallFunction(string nombre, string argumento) {
                if (nombre == "diasDesde") {
                    int dia;
                    return R.Enfriamientos.TryGetValue(argumento, out dia) ? R.DiaActual - dia : 999;
                }
                if (nombre == "ocurrencias") {
                    int n;
                    return R.Ocurrencias.TryGetValue(argumento, out n) ? n : 0;
                }
                throw new InvalidOperationException("Funcion desconocida: " + nombre);
            }
        }

        // ------------------------------------------------------------------ fixtures

        private static EventDefinition Ev(string id, params string[] tags) {
            return new EventDefinition {
                Id = id, Nombre = id,
                Tags = new List<string>(tags),
                Fases = new List<string> { FasesDelNivel.Desarrollo },
                Severidad = 3, ObjetivoAprendizaje = "OA-DEVOPS-02",
                PesoBase = 10, Enfriamiento = 5, MaxOcurrencias = 1,
                Telegrafiado = new Telegrafiado { DiasAntes = 2, Canal = "log", Texto = "aviso de " + id },
                Opciones = {
                    new OpcionEvento { Id = "A", Texto = "Parar la linea",
                                       Rubrica = new RubricaOpcion { Veredicto = "correcta", Oa = "OA-DEVOPS-02", Razon = "…" } },
                    new OpcionEvento { Id = "B", Texto = "Compilar en local y seguir",
                                       EfectosInmediatos = { { "DeudaTecnica", 12L } },
                                       EfectosDiferidos = { new EfectoDiferido { EnDias = 9, EventoForzado = "EV-TEC-021" } },
                                       Rubrica = new RubricaOpcion { Veredicto = "incorrecta", Oa = "OA-DEVOPS-02", Razon = "…" } }
                }
            };
        }

        private static LevelProfile Nivel(int enfriamientoGlobal = 0, int maxPorDia = 1) {
            var p = new LevelProfile { Id = "nivel-01", DiasTotales = 20 };
            p.Director.PresupuestoDrama = new[] { 1, 5, 2, 1 };
            p.Director.EnfriamientoGlobal = enfriamientoGlobal;
            p.Director.MaxEventosPorDia = maxPorDia;
            p.Director.VentanaTelegrafiado = 2;
            return p;
        }

        private static MethodologyRules Reglas(double drama = 1.0, params string[] bloqueados) {
            var perfil = new MethodologyProfile {
                Id = "kanban", Nombre = "Kanban", Familia = FamiliasDeMetodologia.Agil,
                Calendario = new Calendario {
                    Tipo = TiposDeCalendario.Continuo, EtiquetaUnidad = "Flujo", LimiteWipInicial = 4
                },
                ModificadoresDirector = new ModificadoresDirector {
                    MultiplicadorDrama = drama, EventosBloqueados = new List<string>(bloqueados)
                },
                TableroPrincipal = TablerosPrincipales.Cfd,
                RubricaCierre = new RubricaCierre {
                    Practicas = { new PracticaEsperada { Id = "x", Metrica = "diasConHorasExtra",
                                                         Comparador = "<=", Objetivo = 4 } }
                }
            };
            return new MethodologyRules(perfil, 20);
        }

        private sealed class Banco {
            public Ctx Contexto;
            public EffectScheduler Scheduler;
            public EventDirector Director;
            public LevelProfile Perfil;
            public MethodologyRules Metodologia;
            public DayPlan Plan;
        }

        private static Banco Montar(List<EventDefinition> catalogo, LevelProfile perfil = null,
                                    MethodologyRules reglas = null, int semilla = 4417) {
            var p = perfil ?? Nivel();
            var m = reglas ?? Reglas();
            var scheduler = new EffectScheduler();
            var contexto = new Ctx();

            // Lo mismo que hara RuntimeState.DesdeNivel: el presupuesto de drama se COPIA del perfil.
            contexto.R.DramaRestante = (int[])p.Director.PresupuestoDrama.Clone();

            return new Banco {
                Contexto = contexto,
                Scheduler = scheduler,
                Perfil = p,
                Metodologia = m,
                Plan = m.PlanFor(1),
                Director = new EventDirector(catalogo, p, m, new DeterministicRng(semilla), scheduler)
            };
        }

        // ============================================================ INV-3

        [Test]
        public void El_director_agenda_y_no_dispara() {
            var b = Montar(new List<EventDefinition> { Ev("EV-TEC-014", "tecnico") });

            b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);

            Assert.IsNull(b.Director.EventoDeHoy(b.Contexto.R), "el mismo dia no puede pasar nada");
            Assert.AreEqual(1, b.Scheduler.Telegrafiados.Count);
            Assert.AreEqual(3, b.Scheduler.Telegrafiados[0].DiaDelEvento, "dia 1 + 2 de antelacion");
            Assert.AreEqual(1, b.Scheduler.Telegrafiados[0].DiaDelAviso, "y el aviso sale hoy");
        }

        [Test]
        public void Ningun_evento_se_dispara_sin_que_su_aviso_haya_salido_antes() {
            var catalogo = new List<EventDefinition> {
                Ev("EV-A", "tecnico"), Ev("EV-B", "equipo"), Ev("EV-C", "cliente"), Ev("EV-D", "calidad")
            };
            var b = Montar(catalogo, Nivel(enfriamientoGlobal: 2));
            var avisados = new HashSet<string>();

            for (var dia = 1; dia <= 20; dia++) {
                b.Contexto.R.DiaActual = dia;

                var evento = b.Director.EventoDeHoy(b.Contexto.R);
                if (evento != null) {
                    Assert.IsTrue(avisados.Contains(evento.Id),
                                  $"'{evento.Id}' se presento el dia {dia} sin aviso previo");
                    b.Director.RegistrarDisparo(evento, b.Contexto.R);
                }

                b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);

                foreach (var aviso in b.Scheduler.AvisosDeHoy(dia)) avisados.Add(aviso.EventoId);
            }

            Assert.Greater(avisados.Count, 0, "el director tiene que haber agendado algo en 20 dias");
        }

        [Test]
        public void EventoDeHoy_lanza_si_el_aviso_no_llego_a_salir() {
            var b = Montar(new List<EventDefinition> { Ev("EV-TEC-014", "tecnico") });
            b.Scheduler.AgendarTelegrafiado("EV-TEC-014", 5, 2, "log", "…");
            b.Contexto.R.DiaActual = 5;   // se salto el dia 3, donde estaba el aviso

            var ex = Assert.Throws<InvalidOperationException>(() => b.Director.EventoDeHoy(b.Contexto.R));
            StringAssert.Contains("INV-3", ex.Message);
            StringAssert.Contains("EV-TEC-014", ex.Message);
        }

        // ============================================================ pesos

        [Test]
        public void La_curva_lineal_sube_el_peso_con_la_variable() {
            var ev = Ev("EV-A", "tecnico");
            ev.ModificadoresPeso.Add(new ModificadorPeso { Variable = "DeudaTecnica", Curva = CurvasDePeso.Lineal, Factor = 0.04 });
            var b = Montar(new List<EventDefinition> { ev });

            b.Contexto.W.Set("DeudaTecnica", 0);
            Assert.AreEqual(10.0, b.Director.CalcularPeso(ev, b.Contexto, b.Plan), Tol);

            b.Contexto.W.Set("DeudaTecnica", 50);
            Assert.AreEqual(10.0 * (1 + 0.04 * 50), b.Director.CalcularPeso(ev, b.Contexto, b.Plan), Tol);
        }

        [Test]
        public void La_curva_inversa_premia_lo_que_falta() {
            var ev = Ev("EV-A", "calidad");
            ev.ModificadoresPeso.Add(new ModificadorPeso { Variable = "Cobertura", Curva = CurvasDePeso.Inversa, Factor = 0.02 });
            var b = Montar(new List<EventDefinition> { ev });

            b.Contexto.W.Set("Cobertura", 100);
            Assert.AreEqual(10.0, b.Director.CalcularPeso(ev, b.Contexto, b.Plan), Tol, "con todo cubierto, nada extra");

            b.Contexto.W.Set("Cobertura", 20);
            Assert.AreEqual(10.0 * (1 + 0.02 * 80), b.Director.CalcularPeso(ev, b.Contexto, b.Plan), Tol);
        }

        [Test]
        public void La_curva_cuadratica_casi_no_se_nota_hasta_pasada_la_mitad() {
            var ev = Ev("EV-A", "equipo");
            ev.ModificadoresPeso.Add(new ModificadorPeso { Variable = "Cansancio", Curva = CurvasDePeso.Cuadratica, Factor = 0.03 });
            var b = Montar(new List<EventDefinition> { ev });

            b.Contexto.W.Set("Cansancio", 25);
            var enCuarto = b.Director.CalcularPeso(ev, b.Contexto, b.Plan);
            b.Contexto.W.Set("Cansancio", 50);
            var enMitad = b.Director.CalcularPeso(ev, b.Contexto, b.Plan);
            b.Contexto.W.Set("Cansancio", 100);
            var alTope = b.Director.CalcularPeso(ev, b.Contexto, b.Plan);

            Assert.AreEqual(10.0 * (1 + 0.03 * 25 * 25 / 100.0), enCuarto, Tol);
            Assert.Greater(alTope - enMitad, (enMitad - enCuarto) * 2, "el ultimo tramo tiene que doler mucho mas");
        }

        [Test]
        public void Los_pesos_por_tag_del_nivel_multiplican_y_se_acumulan_entre_tags() {
            var perfil = Nivel();
            perfil.Director.PesosPorTag["tecnico"] = 1.5;
            perfil.Director.PesosPorTag["devops"] = 2.0;

            var unTag = Ev("EV-A", "tecnico");
            var dosTags = Ev("EV-B", "tecnico", "devops");
            var b = Montar(new List<EventDefinition> { unTag, dosTags }, perfil);

            Assert.AreEqual(15.0, b.Director.CalcularPeso(unTag, b.Contexto, b.Plan), Tol);
            Assert.AreEqual(30.0, b.Director.CalcularPeso(dosTags, b.Contexto, b.Plan), Tol,
                            "los tags se multiplican entre si, no se toma el maximo");
        }

        [Test]
        public void La_metodologia_multiplica_en_cascada_sobre_el_peso_del_nivel() {
            var perfil = Nivel();
            perfil.Director.PesosPorTag["equipo"] = 1.5;

            var metodologia = Reglas();
            metodologia.Profile.ModificadoresDirector.MultiplicadorPesosPorTag["equipo"] = 2.0;

            var ev = Ev("EV-A", "equipo");
            var b = Montar(new List<EventDefinition> { ev }, perfil, metodologia);

            Assert.AreEqual(10.0 * 1.5 * 2.0, b.Director.CalcularPeso(ev, b.Contexto, b.Plan), Tol);
        }

        [Test]
        public void Un_modificador_sobre_una_variable_desconocida_lanza() {
            var ev = Ev("EV-A", "tecnico");
            ev.ModificadoresPeso.Add(new ModificadorPeso { Variable = "DeudaMoral", Curva = CurvasDePeso.Lineal, Factor = 1 });
            var b = Montar(new List<EventDefinition> { ev });

            var ex = Assert.Throws<InvalidOperationException>(() => b.Director.CalcularPeso(ev, b.Contexto, b.Plan));
            StringAssert.Contains("DeudaMoral", ex.Message);
            StringAssert.Contains("EV-A", ex.Message);
        }

        [Test]
        public void Una_curva_de_peso_inventada_lanza() {
            var ev = Ev("EV-A", "tecnico");
            ev.ModificadoresPeso.Add(new ModificadorPeso { Variable = "DeudaTecnica", Curva = "exponencial", Factor = 1 });
            var b = Montar(new List<EventDefinition> { ev });

            var ex = Assert.Throws<InvalidOperationException>(() => b.Director.CalcularPeso(ev, b.Contexto, b.Plan));
            StringAssert.Contains("exponencial", ex.Message);
        }

        // ============================================================ los filtros

        [Test]
        public void Un_evento_de_otra_fase_no_sale() {
            var ev = Ev("EV-A", "tecnico");
            ev.Fases = new List<string> { FasesDelNivel.Lanzamiento };
            var b = Montar(new List<EventDefinition> { ev });

            b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);   // R.Fase = 2 (desarrollo)

            Assert.AreEqual(DecisionDelDirector.SinCandidatos, b.Director.Log[0].Resultado);
            CollectionAssert.IsEmpty(b.Scheduler.Telegrafiados);
        }

        [Test]
        public void Un_evento_en_enfriamiento_no_sale_hasta_que_pase_su_plazo() {
            var b = Montar(new List<EventDefinition> { Ev("EV-A", "tecnico") });
            b.Contexto.R.Enfriamientos["EV-A"] = 1;
            b.Contexto.R.Ocurrencias["EV-A"] = 0;

            b.Contexto.R.DiaActual = 4;   // han pasado 3 < 5
            b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);
            Assert.AreEqual(DecisionDelDirector.SinCandidatos, b.Director.Log.Last().Resultado);

            b.Contexto.R.DiaActual = 7;   // han pasado 6 >= 5
            b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);
            Assert.AreEqual(DecisionDelDirector.Agendado, b.Director.Log.Last().Resultado);
        }

        [Test]
        public void Un_evento_que_agoto_sus_ocurrencias_no_vuelve() {
            var b = Montar(new List<EventDefinition> { Ev("EV-A", "tecnico") });
            b.Contexto.R.Ocurrencias["EV-A"] = 1;   // maxOcurrencias = 1

            b.Contexto.R.DiaActual = 15;
            b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);

            Assert.AreEqual(DecisionDelDirector.SinCandidatos, b.Director.Log.Last().Resultado);
        }

        [Test]
        public void Una_precondicion_que_no_se_cumple_descarta_y_lo_deja_dicho() {
            var ev = Ev("EV-A", "tecnico");
            ev.Precondiciones.Add("DeudaTecnica > 40");
            var b = Montar(new List<EventDefinition> { ev });

            b.Contexto.W.Set("DeudaTecnica", 10);
            b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);

            var log = b.Director.Log[0];
            Assert.AreEqual(DecisionDelDirector.SinCandidatos, log.Resultado);
            Assert.AreEqual(1, log.Descartados.Count);
            StringAssert.Contains("DeudaTecnica > 40", log.Descartados[0]);

            // los riesgos tienen causa: sube la deuda y el evento pasa a ser posible
            b.Contexto.W.Set("DeudaTecnica", 60);
            b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);
            Assert.AreEqual(DecisionDelDirector.Agendado, b.Director.Log.Last().Resultado);
        }

        [Test]
        public void Un_evento_bloqueado_por_la_metodologia_no_sale() {
            var b = Montar(new List<EventDefinition> { Ev("EV-A", "tecnico") }, Nivel(), Reglas(1.0, "EV-A"));

            b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);

            Assert.AreEqual(DecisionDelDirector.SinCandidatos, b.Director.Log[0].Resultado);
        }

        [Test]
        public void Un_evento_ya_agendado_no_se_agenda_dos_veces() {
            var b = Montar(new List<EventDefinition> { Ev("EV-A", "tecnico") });

            b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);
            b.Contexto.R.DiaActual = 2;
            b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);

            Assert.AreEqual(1, b.Scheduler.Telegrafiados.Count);
            Assert.AreEqual(DecisionDelDirector.SinCandidatos, b.Director.Log.Last().Resultado);
        }

        // ============================================================ presupuesto de drama

        [Test]
        public void Sin_drama_no_se_agenda_nada() {
            var perfil = Nivel();
            perfil.Director.PresupuestoDrama = new[] { 1, 0, 2, 1 };
            var b = Montar(new List<EventDefinition> { Ev("EV-A", "tecnico") }, perfil);
            b.Contexto.R.DramaRestante = (int[])perfil.Director.PresupuestoDrama.Clone();

            b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);

            Assert.AreEqual(DecisionDelDirector.SinDrama, b.Director.Log[0].Resultado);
            CollectionAssert.IsEmpty(b.Scheduler.Telegrafiados);
        }

        [Test]
        public void El_presupuesto_de_drama_se_gasta_al_agendar() {
            var b = Montar(new List<EventDefinition> { Ev("EV-A", "tecnico"), Ev("EV-B", "equipo") });
            b.Contexto.R.DramaRestante = new[] { 1, 2, 2, 1 };

            Assert.AreEqual(2, b.Director.DramaRestante(b.Contexto.R));
            b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);
            Assert.AreEqual(1, b.Director.DramaRestante(b.Contexto.R));

            b.Contexto.R.DiaActual = 5;
            b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);
            Assert.AreEqual(0, b.Director.DramaRestante(b.Contexto.R));

            b.Contexto.R.DiaActual = 9;
            b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);
            Assert.AreEqual(DecisionDelDirector.SinDrama, b.Director.Log.Last().Resultado);
        }

        [Test]
        public void La_metodologia_escala_el_presupuesto_de_drama() {
            var b = Montar(new List<EventDefinition> { Ev("EV-A", "tecnico") }, Nivel(), Reglas(drama: 0.8));
            b.Contexto.R.DramaRestante = new[] { 1, 3, 2, 1 };

            Assert.AreEqual(2, b.Director.DramaRestante(b.Contexto.R),
                            "Cascada con multiplicador 0.8 sobre 3 tiene 2: un nivel mas plano");
        }

        [Test]
        public void Un_dia_lleno_no_consume_drama() {
            var b = Montar(new List<EventDefinition> { Ev("EV-A", "tecnico") });
            b.Contexto.R.DramaRestante = new[] { 1, 3, 2, 1 };

            // ya hay otro evento ocupando el dia 3, que es donde caeria este
            b.Scheduler.AgendarTelegrafiado("EV-OTRO", 3, 2, "log", "…");
            var dramaAntes = b.Director.DramaRestante(b.Contexto.R);

            b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);

            Assert.AreEqual(DecisionDelDirector.DiaLleno, b.Director.Log[0].Resultado);
            Assert.AreEqual(dramaAntes, b.Director.DramaRestante(b.Contexto.R),
                            "que la agenda este llena no es una decision dramatica");
        }

        [Test]
        public void El_enfriamiento_global_separa_los_eventos_en_el_calendario() {
            var b = Montar(new List<EventDefinition> { Ev("EV-A", "tecnico"), Ev("EV-B", "equipo") },
                           Nivel(enfriamientoGlobal: 4));
            b.Contexto.R.DramaRestante = new[] { 1, 5, 2, 1 };

            b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);      // agenda para el dia 3
            b.Contexto.R.DiaActual = 2;
            b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);      // querria el dia 4: demasiado cerca

            Assert.AreEqual(DecisionDelDirector.SinHueco, b.Director.Log.Last().Resultado);
            Assert.AreEqual(1, b.Scheduler.Telegrafiados.Count);
        }

        // ============================================================ cadenas

        [Test]
        public void ForzarEvento_encadena_sin_gastar_drama_y_sigue_telegrafiando() {
            var b = Montar(new List<EventDefinition> { Ev("EV-A", "tecnico"), Ev("EV-TEC-021", "tecnico") });
            b.Contexto.R.DramaRestante = new[] { 1, 3, 2, 1 };
            b.Contexto.R.DiaActual = 9;
            var dramaAntes = b.Director.DramaRestante(b.Contexto.R);

            b.Director.ForzarEvento("EV-TEC-021", b.Contexto.R);

            Assert.AreEqual(dramaAntes, b.Director.DramaRestante(b.Contexto.R),
                            "las cadenas las escribe el contenido, no el presupuesto de caos");
            Assert.AreEqual(1, b.Scheduler.Telegrafiados.Count);
            Assert.AreEqual(11, b.Scheduler.Telegrafiados[0].DiaDelEvento);
            Assert.AreEqual(9, b.Scheduler.Telegrafiados[0].DiaDelAviso,
                            "una consecuencia encadenada tampoco cae del cielo");
            Assert.AreEqual(DecisionDelDirector.Forzado, b.Director.Log.Last().Resultado);
        }

        [Test]
        public void ForzarEvento_con_un_id_que_no_existe_lanza() {
            var b = Montar(new List<EventDefinition> { Ev("EV-A", "tecnico") });
            var ex = Assert.Throws<InvalidOperationException>(
                () => b.Director.ForzarEvento("EV-FANTASMA", b.Contexto.R));
            StringAssert.Contains("EV-FANTASMA", ex.Message);
        }

        [Test]
        public void RegistrarDisparo_cuenta_la_ocurrencia_y_arranca_los_dos_enfriamientos() {
            var ev = Ev("EV-A", "tecnico");
            var b = Montar(new List<EventDefinition> { ev });
            b.Contexto.R.DiaActual = 7;
            b.Scheduler.AgendarTelegrafiado("EV-A", 7, 2, "log", "…");

            b.Director.RegistrarDisparo(ev, b.Contexto.R);

            Assert.AreEqual(1, b.Contexto.R.Ocurrencias["EV-A"]);
            Assert.AreEqual(7, b.Contexto.R.Enfriamientos["EV-A"]);
            Assert.AreEqual(7, b.Contexto.R.Enfriamientos[EventDirector.EnfriamientoGlobal]);
            Assert.IsFalse(b.Scheduler.HayEventoAgendado("EV-A"), "resuelto sale de la agenda");
        }

        // ============================================================ determinismo

        [Test]
        public void La_misma_semilla_elige_los_mismos_eventos() {
            Assert.AreEqual(string.Join(",", Partida(4417)), string.Join(",", Partida(4417)));
        }

        [Test]
        public void Semillas_distintas_eligen_partidas_distintas() {
            Assert.AreNotEqual(string.Join(",", Partida(4417)), string.Join(",", Partida(9001)));
        }

        private static List<string> Partida(int semilla) {
            var catalogo = new List<EventDefinition> {
                Ev("EV-A", "tecnico"), Ev("EV-B", "equipo"), Ev("EV-C", "cliente"),
                Ev("EV-D", "calidad"), Ev("EV-E", "devops"), Ev("EV-F", "alcance")
            };
            var b = Montar(catalogo, Nivel(enfriamientoGlobal: 2), Reglas(), semilla);
            b.Contexto.R.DramaRestante = new[] { 1, 9, 2, 1 };
            var elegidos = new List<string>();

            for (var dia = 1; dia <= 20; dia++) {
                b.Contexto.R.DiaActual = dia;
                var evento = b.Director.EventoDeHoy(b.Contexto.R);
                if (evento != null) {
                    elegidos.Add($"d{dia}:{evento.Id}");
                    b.Director.RegistrarDisparo(evento, b.Contexto.R);
                }
                b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);
                b.Scheduler.AvisosDeHoy(dia);
            }
            return elegidos;
        }

        [Test]
        public void En_una_partida_de_veinte_dias_pasan_cosas_pero_no_todos_los_dias() {
            var elegidos = Partida(4417);
            Assert.Greater(elegidos.Count, 1, "un nivel sin eventos no enseña nada");
            Assert.Less(elegidos.Count, 20, "un evento cada dia seria ruido, no drama");
        }

        // ============================================================ traza y datos

        [Test]
        public void El_log_cuenta_por_que_hizo_lo_que_hizo() {
            var ev = Ev("EV-A", "tecnico");
            ev.Precondiciones.Add("DeudaTecnica > 40");
            var b = Montar(new List<EventDefinition> { ev, Ev("EV-B", "equipo") });

            b.Director.TickSeleccion(b.Contexto.R, b.Contexto, b.Plan);

            var log = b.Director.Log[0];
            Assert.AreEqual(1, log.Dia);
            Assert.AreEqual(1, log.Candidatos, "EV-A cae por precondicion, queda EV-B");
            Assert.AreEqual("EV-B", log.EventoId);
            Assert.AreEqual(3, log.DiaDelEvento);
            Assert.Greater(log.Peso, 0);
            StringAssert.Contains("EV-A", log.Descartados[0]);
            StringAssert.Contains("dia 1", log.ToString());
        }

        [Test]
        public void Un_catalogo_con_dos_eventos_del_mismo_id_lanza() {
            var ex = Assert.Throws<InvalidOperationException>(
                () => Montar(new List<EventDefinition> { Ev("EV-A", "tecnico"), Ev("EV-A", "equipo") }));
            StringAssert.Contains("EV-A", ex.Message);
        }

        [Test]
        public void El_evento_sobrevive_a_un_viaje_por_json() {
            var original = Ev("EV-TEC-014", "tecnico", "devops");
            original.ModificadoresPeso.Add(new ModificadorPeso {
                Variable = "DeudaTecnica", Curva = CurvasDePeso.Lineal, Factor = 0.04
            });
            original.Precondiciones.Add("DeudaTecnica > 40");

            var json = JsonConvert.SerializeObject(original, JsonDeGuardado.Settings);
            var vuelta = JsonConvert.DeserializeObject<EventDefinition>(json, JsonDeGuardado.Settings);

            Assert.AreEqual(json, JsonConvert.SerializeObject(vuelta, JsonDeGuardado.Settings));
            Assert.AreEqual(2, vuelta.Tags.Count, "ObjectCreationHandling.Replace: no se duplican");
            Assert.AreEqual(2, vuelta.Opciones.Count);
            Assert.AreEqual("incorrecta", vuelta.Opciones[1].Rubrica.Veredicto);
            Assert.AreEqual(9, vuelta.Opciones[1].EfectosDiferidos[0].EnDias);
            Assert.AreEqual(2, vuelta.Telegrafiado.DiasAntes);
        }

        [Test]
        public void NombreDeFase_traduce_los_cuatro_numeros() {
            Assert.AreEqual("planificacion", EventDirector.NombreDeFase(1));
            Assert.AreEqual("desarrollo", EventDirector.NombreDeFase(2));
            Assert.AreEqual("lanzamiento", EventDirector.NombreDeFase(3));
            Assert.AreEqual("evaluacion", EventDirector.NombreDeFase(4));
            Assert.AreEqual("", EventDirector.NombreDeFase(9));
        }
    }
}
