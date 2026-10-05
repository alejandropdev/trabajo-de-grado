using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Evaluacion;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Detectar;
using Nexus.Core.Minijuegos.Ordenar;
using Nexus.Core.Minijuegos.Repartir;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// Que las decisiones tengan consecuencias de verdad (feedback beta #13: «jugué la peor partida posible a
    /// propósito y salió bien»). Un robot juega el contenido real dos veces: haciendo todo mal y haciendo todo
    /// bien. La peor partida tiene que salir mal y la mejor, bien, en los dos niveles y con varias semillas.
    /// </summary>
    public class ConsecuenciasRealesTests {
        private static Catalogo _catalogo;

        private static string Raiz() {
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            return !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
        }

        private static Catalogo Catalogo() {
            return _catalogo ?? (_catalogo = CatalogLoader.CargarTodo(new CatalogoDeArchivos(Raiz())));
        }

        private static FlagStore Flags() {
            var store = new FlagStore(new Dictionary<string, double>(StringComparer.Ordinal), Catalogo().Flags);
            store.Inicializar();
            return store;
        }

        // ================================================================ los dos robots

        private static GameSession Empezar(string nivel, int semilla, bool bien) {
            var s = new GameSession(Catalogo(), nivel, Flags(), semilla);
            if (bien) {
                s.ElegirMetodologia("scrum", "el_cliente_cambiara_de_opinion");
                if (nivel == "nivel-00") {
                    s.RepartirCalidad(new Dictionary<string, int> { { "adecuacion-funcional", 2 }, { "usabilidad", 1 }, { "fiabilidad", 1 } });
                    s.ElegirArquitectura("web-sencilla", "nadie_instala_nada");
                } else {
                    s.RepartirCalidad(new Dictionary<string, int> { { "seguridad", 2 }, { "fiabilidad", 2 }, { "adecuacion-funcional", 2 }, { "mantenibilidad", 2 } });
                    s.ElegirArquitectura("monolito-modular", "equipo_pequeno");
                }
            } else {
                s.ElegirMetodologia("cascada", "asi_no_hay_que_hablar_con_el_cliente");
                s.RepartirCalidad(new Dictionary<string, int>());
                if (nivel == "nivel-00") s.ElegirArquitectura("app-movil", "todo_el_mundo_tiene_movil");
                else s.ElegirArquitectura("microservicios", "esta_de_moda");
            }
            s.CerrarFase1();
            return s;
        }

        private static MinijuegoDef Escena(string archivo) {
            return CatalogoMinijuegos.Parsear(File.ReadAllText(Path.Combine(Raiz(), archivo)));
        }

        /// <summary>El mejor resultado posible de una escena, jugado con su evaluador de verdad.</summary>
        private static ResultadoMinijuego JugarBien(string archivo) {
            var def = Escena(archivo);
            switch (Verbos.Normalizar(def.Verbo)) {
                case Verbos.Ordenar: {
                    var mejor = OrdenarEvaluador.MejorOrden(def.Ordenar);
                    var orden = mejor.Concat(def.Ordenar.Tarjetas.Select(t => t.Id).Where(id => !mejor.Contains(id))).ToList();
                    return OrdenarEvaluador.Evaluar(def, orden, "negociar");
                }
                case Verbos.Repartir:
                    return RepartirEvaluador.Evaluar(def, RepartirEvaluador.RepartosGanadores(def.Repartir).First());
                default: {
                    var estado = new DetectarState(def.Presentacion.SegundosReloj);
                    var t = 0;
                    foreach (var z in def.Zonas) {
                        foreach (var pieza in z.Commits) estado.Alternar(pieza);
                        estado.Marcar(z.Defecto, t++);
                    }
                    return DetectarEvaluador.Evaluar(def, estado);
                }
            }
        }

        /// <summary>No hacer nada: lo peor que se puede hacer con un minijuego (sale «omitido»).</summary>
        private static ResultadoMinijuego JugarMal(string archivo) {
            var def = Escena(archivo);
            switch (Verbos.Normalizar(def.Verbo)) {
                case Verbos.Ordenar: return OrdenarEvaluador.Evaluar(def, new List<string>(), null);
                case Verbos.Repartir: return RepartirEvaluador.Evaluar(def, new Dictionary<string, int>());
                default: return DetectarEvaluador.Evaluar(def, new DetectarState(def.Presentacion.SegundosReloj));
            }
        }

        private static string OpcionCon(GameSession s, string veredicto) {
            var ev = Catalogo().Eventos.First(e => e.Id == s.Decision.EventoId);
            var libres = s.Decision.Opciones.Where(o => !o.Bloqueada).Select(o => o.Id).ToList();
            foreach (var v in new[] { veredicto, Veredictos.Aceptable, Veredictos.Correcta, Veredictos.Incorrecta }) {
                var o = ev.Opciones.FirstOrDefault(x => libres.Contains(x.Id) && x.Rubrica != null && x.Rubrica.Veredicto == v);
                if (o != null) return o.Id;
            }
            return libres.First();
        }

        private static LaunchResult Jugar(string nivel, int semilla, bool bien) {
            var s = Empezar(nivel, semilla, bien);
            while (s.R.Fase == 2) {
                s.ComenzarDia();
                if (s.PendingPlanning != null)
                    s.Comprometer(bien ? s.PendingPlanning.CapacidadSugerida : s.PendingPlanning.CapacidadSugerida * 2);
                if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0)
                    s.ElegirAccionRetro(s.PendingRetro.Acciones[0].Id);

                for (var v = 0; v < 60 && !s.SePuedeCerrarLaJornada; v++) {
                    var tramos = new Queue<ResultadoDeAvance>();
                    tramos.Enqueue(s.AvanzarReloj(30));
                    while (tramos.Count > 0)
                        foreach (var a in tramos.Dequeue().AlertasQueSuenan) tramos.Enqueue(s.AtenderAlerta(a.Id));
                    if (s.Decision != null)
                        s.ResolverDecision(OpcionCon(s, bien ? Veredictos.Correcta : Veredictos.Incorrecta));
                    if (s.Minijuego != null)
                        s.ResolverMinijuego(bien ? JugarBien(s.Minijuego.Archivo) : JugarMal(s.Minijuego.Archivo));
                }
                // El buen jugador adelanta trabajo en su escritorio; el malo no.
                if (bien && !s.AlertasDeHoy.Any(a => a.EstaPendiente))
                    foreach (var tarea in s.TareasDeOficina)
                        if (s.PorQueNoSePuedeHacer(tarea.Id) == null) {
                            s.EmpezarTarea(tarea.Id);
                            s.ResolverTarea(JugarBien(tarea.Archivo));
                        }
                s.CerrarJornada();
                s.TerminarDia(!bien);   // el malo se queda todas las noches
            }
            return s.EjecutarLanzamiento();
        }

        /// <summary>
        /// El mejor gestor posible del tablero y de las ceremonias, dentro de una partida por lo demas pesima: asigna
        /// cada tarjeta a quien mejor se le da, asiste a todas las ceremonias llevandolas bien y prueba cada dia que puede.
        /// </summary>
        private static LaunchResult JugarMalPeroGestionandoBien(string nivel, int semilla) {
            var s = Empezar(nivel, semilla, false);
            while (s.R.Fase == 2) {
                s.ComenzarDia();
                if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida * 2);
                if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0) s.ElegirAccionRetro(s.PendingRetro.Acciones[0].Id);
                foreach (var c in s.CeremoniasDeHoy())
                    if (s.PorQueNoSePuedeAsistir(c.Id) == null) s.AsistirACeremonia(c.Id, c.Opciones[0].Id);
                if (s.PorQueNoSePuedeProbar() == null) s.EjecutarPruebas();
                foreach (var c in s.Tablero.Tarjetas.Where(x => x.Empezada && !x.Terminada).ToList()) {
                    var mejor = s.Tablero.Miembros.OrderByDescending(m => m.HabilidadPara(c.Tipo)).First();
                    if (s.PorQueNoSePuedeAsignar(c.Id, mejor.Id) == null) s.AsignarTarjeta(c.Id, mejor.Id);
                }
                for (var v = 0; v < 60 && !s.SePuedeCerrarLaJornada; v++) {
                    var tramos = new Queue<ResultadoDeAvance>();
                    tramos.Enqueue(s.AvanzarReloj(30));
                    while (tramos.Count > 0)
                        foreach (var a in tramos.Dequeue().AlertasQueSuenan) tramos.Enqueue(s.AtenderAlerta(a.Id));
                    if (s.Decision != null) s.ResolverDecision(OpcionCon(s, Veredictos.Incorrecta));
                    if (s.Minijuego != null) s.ResolverMinijuego(JugarMal(s.Minijuego.Archivo));
                }
                if (!s.JornadaLlegoAlCierre) s.CerrarJornada();
                s.TerminarDia(true);
            }
            return s.EjecutarLanzamiento();
        }

        [TestCase("nivel-00", 1)]
        [TestCase("nivel-01", 1)]
        [TestCase("nivel-01", 4417)]
        public void La_mejor_gestion_del_tablero_no_basta_para_salvar_la_peor_partida(string nivel, int semilla) {
            // El tablero y las ceremonias MODULAN al equipo (una banda del 80 al 115 %): ayudan, pero no tapan haber
            // decidido todo mal, haberse quedado todas las noches y no haber hecho ni un reto.
            var l = JugarMalPeroGestionandoBien(nivel, semilla);
            TestContext.WriteLine("peor, bien gestionada: " + Describir(l));
            Assert.AreNotEqual(NivelesDeLanzamiento.Bien, l.Nivel, Describir(l));
        }

        private static string Describir(LaunchResult l) {
            return $"{l.Nivel} ({l.Puntaje:0}) · " + string.Join(" · ", l.Factores.Select(f => $"{f.Nombre}={f.Valor} [{f.Estado}]"));
        }

        // ================================================================ lo que se exige

        [TestCase("nivel-00", 1)]
        [TestCase("nivel-00", 7)]
        [TestCase("nivel-00", 4417)]
        [TestCase("nivel-01", 1)]
        [TestCase("nivel-01", 4417)]
        public void La_peor_partida_posible_sale_mal(string nivel, int semilla) {
            var l = Jugar(nivel, semilla, false);
            TestContext.WriteLine("peor: " + Describir(l));
            Assert.AreEqual(NivelesDeLanzamiento.Mal, l.Nivel, Describir(l));
            Assert.IsFalse(l.Exito);
            Assert.IsNotEmpty(l.DecisionesQuePesaron, "hay que enseñar qué decisiones lo provocaron");
        }

        [TestCase("nivel-00", 1)]
        [TestCase("nivel-00", 7)]
        [TestCase("nivel-00", 4417)]
        [TestCase("nivel-01", 1)]
        [TestCase("nivel-01", 4417)]
        public void La_mejor_partida_sale_bien(string nivel, int semilla) {
            var l = Jugar(nivel, semilla, true);
            TestContext.WriteLine("mejor: " + Describir(l));
            Assert.AreEqual(NivelesDeLanzamiento.Bien, l.Nivel, Describir(l));
            Assert.IsTrue(l.Exito);
        }

        [Test]
        public void La_partida_del_feedback_ya_no_sale_bien() {
            // Lo que vio el tester: 14 de 14 puntos, 9 defectos, riesgo 38, todo decidido mal.
            var traza = new DecisionTrace();
            for (var i = 0; i < 5; i++)
                traza.Registrar(new EntradaTraza { Dia = i + 1, Origen = "EV-X" + i, Titulo = "d" + i, Veredicto = Veredictos.Incorrecta });
            var c = CalculoDeLanzamiento.Calcular(new EntradaDeLanzamiento {
                Riesgo = 38, Defectos = 9, Entregado = 14, Comprometido = 14, Satisfaccion = 40, Traza = traza
            }, Catalogo().Niveles["nivel-00"].Lanzamiento);
            Assert.AreEqual(NivelesDeLanzamiento.Mal, c.Nivel);
            Assert.AreEqual(3, c.DecisionesQuePesaron.Count, "las tres que más pesaron, no todas");
        }

        [Test]
        public void Un_solo_factor_en_rojo_impide_salir_bien_aunque_el_puntaje_sea_alto() {
            var traza = new DecisionTrace();
            traza.Registrar(new EntradaTraza { Origen = "EV-A", Titulo = "a", Veredicto = Veredictos.Correcta });
            var u = new UmbralesDeLanzamiento();
            var c = CalculoDeLanzamiento.Calcular(new EntradaDeLanzamiento {
                Riesgo = 10, Defectos = u.DefectosMal + 1, Entregado = 10, Comprometido = 10, Satisfaccion = 90, Traza = traza
            }, u);
            Assert.AreNotEqual(NivelesDeLanzamiento.Bien, c.Nivel);
            Assert.AreEqual(NivelesDeLanzamiento.Mal, c.Nivel);
        }

        [Test]
        public void Una_barra_mala_trae_su_incidencia_y_la_misma_no_se_repite_si_hay_otra() {
            var n0 = Catalogo().Niveles["nivel-00"];
            var w = new Nexus.Core.Modelo.WorldState { DeudaTecnica = 70, Cobertura = 60, Cansancio = 10, MoralEquipo = 60, SatisfaccionCliente = 60 };
            var r = new Nexus.Core.Modelo.RuntimeState { DiaActual = 3 };
            var hoy = Nexus.Core.Simulacion.IncidenciasDelEstado.Elegir(n0.Incidencias, w, r);
            Assert.AreEqual("INC-DEUDA", hoy.Id);
            StringAssert.Contains("70", hoy.Texto, "dice el valor que la provocó");

            w.Cansancio = 80;
            r.IncidenciasVistas["INC-DEUDA"] = 1;
            Assert.AreEqual("INC-CANSANCIO", Nexus.Core.Simulacion.IncidenciasDelEstado.Elegir(n0.Incidencias, w, r).Id,
                            "con dos barras malas, rota: no siempre la misma");

            var sano = new Nexus.Core.Modelo.WorldState { DeudaTecnica = 10, Cobertura = 60, Cansancio = 10, MoralEquipo = 60, SatisfaccionCliente = 60 };
            Assert.IsNull(Nexus.Core.Simulacion.IncidenciasDelEstado.Elegir(n0.Incidencias, sano, r), "un proyecto sano no tiene incidencias");
        }

        [Test]
        public void El_pronostico_del_ultimo_dia_es_el_lanzamiento() {
            foreach (var bien in new[] { true, false }) {
                var s = Empezar("nivel-00", 7, bien);
                while (s.R.Fase == 2) {
                    s.ComenzarDia();
                    if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
                    if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0) s.ElegirAccionRetro(s.PendingRetro.Acciones[0].Id);
                    for (var v = 0; v < 60 && !s.SePuedeCerrarLaJornada; v++) {
                        var tramos = new Queue<ResultadoDeAvance>();
                        tramos.Enqueue(s.AvanzarReloj(30));
                        while (tramos.Count > 0)
                            foreach (var a in tramos.Dequeue().AlertasQueSuenan) tramos.Enqueue(s.AtenderAlerta(a.Id));
                        if (s.Decision != null) s.ResolverDecision(OpcionCon(s, bien ? Veredictos.Correcta : Veredictos.Incorrecta));
                        if (s.Minijuego != null) s.ResolverMinijuego(bien ? JugarBien(s.Minijuego.Archivo) : JugarMal(s.Minijuego.Archivo));
                    }
                    s.CerrarJornada();
                    s.TerminarDia(!bien);
                }
                var pronostico = s.Pronostico();
                var real = s.EjecutarLanzamiento();
                Assert.AreEqual(real.Nivel, pronostico.Nivel, "lo que se enseña durante el nivel es lo que se cobra al final");
            }
        }

        [Test]
        public void Un_dia_perdido_se_cobra_en_avance() {
            var w = new Nexus.Core.Modelo.WorldState { Avance = 10, VelocidadMod = 1.0 };
            Nexus.Core.Servicios.EffectApplier.Aplicar(w, new Dictionary<string, object> { { "Dias", 1.0 } });
            Assert.AreEqual(10 - Nexus.Core.Servicios.EffectApplier.PuntosPorDia, w.Avance, 1e-9);
            var vista = Nexus.Core.Servicios.EffectApplier.Previsualizar(w, new Dictionary<string, object> { { "Dias", 0.5 } });
            Assert.IsTrue(vista.ContainsKey("Avance"), "la previsualizacion enseña lo que cuesta el tiempo");
        }
    }
}
