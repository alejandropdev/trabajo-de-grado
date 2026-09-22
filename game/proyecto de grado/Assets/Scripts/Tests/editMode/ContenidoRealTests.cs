using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Eventos;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Detectar;
using Nexus.Core.Minijuegos.Ordenar;
using Nexus.Core.Minijuegos.Repartir;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// El contenido de verdad, el de StreamingAssets, jugado entero muchas veces. Los demas tests prueban el
    /// motor con catalogos pequeños; estos prueban que el contenido real hace lo que el diseño promete.
    /// Fueron estas partidas las que encontraron las cadenas que se perdian y la alerta de las 08:00.
    ///
    /// En Unity el contenido esta dos carpetas por encima del ensamblado de tests. Fuera de Unity, la
    /// variable de entorno NEXUS_STREAMINGASSETS dice donde esta.
    /// </summary>
    public class ContenidoRealTests {
        private static Catalogo _catalogo;

        private static string Raiz() {
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            Assert.IsTrue(Directory.Exists(raiz), "No encuentro el contenido en " + raiz);
            return raiz;
        }

        private static Catalogo Catalogo() {
            return _catalogo ?? (_catalogo = CatalogLoader.CargarTodo(new CatalogoDeArchivos(Raiz())));
        }

        private static FlagStore Flags(Dictionary<string, double> respaldo = null) {
            var store = new FlagStore(respaldo ?? new Dictionary<string, double>(StringComparer.Ordinal), Catalogo().Flags);
            store.Inicializar();
            return store;
        }

        // ================================================================ el jugador robot

        private sealed class Partida {
            public GameSession Sesion;
            public readonly List<string> Decisiones = new List<string>();   // "EV-X:opcion@dia"
            public readonly List<string> Beats = new List<string>();        // "CIN-X@dia"
            public readonly List<string> Variantes = new List<string>();    // "CIN-X:variante"
            public readonly List<string> Expiradas = new List<string>();
            public readonly List<int> DiasConPlanificacion = new List<int>();
            public readonly List<string> Minijuegos = new List<string>();  // "MJ-X:resultado:respuesta@dia"
        }

        private static GameSession Empezar(string nivel, string metodologia, int semilla, FlagStore flags = null) {
            var s = new GameSession(Catalogo(), nivel, flags ?? Flags(), semilla);
            s.ElegirMetodologia(metodologia, "el_cliente_cambiara_de_opinion");
            if (nivel == "nivel-00") {
                s.RepartirCalidad(new Dictionary<string, int> { { "adecuacion-funcional", 2 }, { "usabilidad", 1 }, { "fiabilidad", 1 } });
                s.ElegirArquitectura("web-sencilla", "un_solo_equipo");
            } else {
                s.RepartirCalidad(new Dictionary<string, int> { { "seguridad", 2 }, { "fiabilidad", 2 }, { "mantenibilidad", 2 }, { "usabilidad", 2 } });
                s.ElegirArquitectura("monolito-modular", "equipo_pequeno");
            }
            s.CerrarFase1();
            return s;
        }

        /// <summary>Juega un nivel entero eligiendo al azar (con su propio azar, no el del motor) y lo cierra.</summary>
        /// <summary>
        /// Juega un minijuego DE VERDAD, con su escena y su evaluador, eligiendo al azar: marcas en el V1, un orden
        /// y una respuesta en el V3, un reparto en el V2. Asi las consecuencias (y las cadenas que agendan) son las
        /// reales, no un resultado inventado.
        /// </summary>
        private static ResultadoMinijuego JugarMinijuego(PendingMinigame pendiente, Random azar) {
            var def = CatalogoMinijuegos.Parsear(File.ReadAllText(Path.Combine(Raiz(), pendiente.Archivo)));
            switch (Verbos.Normalizar(def.Verbo)) {
                case Verbos.Ordenar: {
                    var orden = def.Ordenar.Tarjetas.Select(t => t.Id).OrderBy(_ => azar.Next()).ToList();
                    var respuestas = new[] { "obedecer", "rechazar", "negociar" };
                    return OrdenarEvaluador.Evaluar(def, orden, respuestas[azar.Next(3)]);
                }
                case Verbos.Repartir: {
                    var asignacion = new Dictionary<string, int>();
                    var restante = def.Repartir.Presupuesto;
                    foreach (var d in def.Repartir.Depositos.OrderBy(_ => azar.Next())) {
                        var horas = azar.Next(0, restante + 1);
                        asignacion[d.Id] = horas;
                        restante -= horas;
                    }
                    return RepartirEvaluador.Evaluar(def, asignacion);
                }
                default: {
                    var estado = new DetectarState(def.Presentacion.SegundosReloj);
                    var piezas = def.Artefacto.Commits.Select(c => c.Id)
                        .Concat(def.Artefacto.Elementos.Select(e => e.Id))
                        .Concat(def.Artefacto.Conexiones.Select(c => c.Id)).ToList();
                    var marcas = azar.Next(0, 4);
                    for (var i = 0; i < marcas; i++) {
                        estado.Alternar(piezas[azar.Next(piezas.Count)]);
                        estado.Marcar(def.PaletaEtiquetas[azar.Next(def.PaletaEtiquetas.Count)], i);
                    }
                    return DetectarEvaluador.Evaluar(def, estado);
                }
            }
        }

        private static Partida Jugar(string nivel, string metodologia, int semilla, FlagStore flags = null,
                                     bool dejarCaducarLosMinijuegos = false) {
            var p = new Partida { Sesion = Empezar(nivel, metodologia, semilla, flags) };
            var s = p.Sesion;
            var azar = new Random(semilla * 31 + metodologia.Length);

            while (s.R.Fase == 2) {
                s.ComenzarDia();
                if (s.Beat != null) {
                    p.Beats.Add(s.Beat.BeatId + "@" + s.R.DiaActual);
                    p.Variantes.Add(s.Beat.BeatId + ":" + s.Beat.Variante);
                }
                if (s.PendingPlanning != null) {
                    p.DiasConPlanificacion.Add(s.R.DiaActual);
                    s.Comprometer(s.PendingPlanning.CapacidadSugerida);
                }
                if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0)
                    s.ElegirAccionRetro(s.PendingRetro.Acciones[azar.Next(s.PendingRetro.Acciones.Count)].Id);

                for (var v = 0; v < 60 && !s.SePuedeCerrarLaJornada; v++) {
                    // Atender tambien mueve el reloj, y en ese rato puede sonar (o caducar) otra alerta:
                    // se procesan todas, como haria la pantalla, hasta que no quede ninguna por atender.
                    var tramos = new Queue<ResultadoDeAvance>();
                    tramos.Enqueue(s.AvanzarReloj(30));
                    var sonaron = new List<Nexus.Core.Jornada.Alerta>();
                    var caducaron = new List<Nexus.Core.Jornada.Alerta>();
                    while (tramos.Count > 0) {
                        var tramo = tramos.Dequeue();
                        caducaron.AddRange(tramo.AlertasQueExpiraron);
                        foreach (var a in tramo.AlertasQueSuenan)
                            if (!(dejarCaducarLosMinijuegos && a.Tipo == Nexus.Core.Jornada.TiposDeAlerta.Minijuego))
                                tramos.Enqueue(s.AtenderAlerta(a.Id));
                    }
                    var avance = new { AlertasQueExpiraron = caducaron };
                    foreach (var a in avance.AlertasQueExpiraron) p.Expiradas.Add(a.Id + "@" + s.R.DiaActual);

                    if (s.Decision != null) {
                        var libres = s.Decision.Opciones.Where(o => !o.Bloqueada).ToList();
                        Assert.IsNotEmpty(libres, $"{s.Decision.EventoId} ({metodologia}): todas las opciones bloqueadas");
                        var elegida = libres[azar.Next(libres.Count)];
                        p.Decisiones.Add($"{s.Decision.EventoId}:{elegida.Id}@{s.R.DiaActual}");
                        s.ResolverDecision(elegida.Id);
                    }
                    if (s.Minijuego != null) {
                        var resultado = JugarMinijuego(s.Minijuego, azar);
                        var respuesta = resultado.Hallazgos.FirstOrDefault(h => h.StartsWith("respuesta:"));
                        p.Minijuegos.Add($"{resultado.MinijuegoId}:{resultado.Resultado}:{respuesta}@{s.R.DiaActual}");
                        s.ResolverMinijuego(resultado);
                    }
                    foreach (var a in avance.AlertasQueExpiraron)
                        if (a.Tipo == Nexus.Core.Jornada.TiposDeAlerta.Minijuego)
                            p.Minijuegos.Add($"{a.Id}:caducado:@{s.R.DiaActual}");
                }
                s.CerrarJornada();
                s.TerminarDia(false);
            }

            s.EjecutarLanzamiento();
            s.Cerrar();
            return p;
        }

        private static IEnumerable<string> Ids(Partida p) { return p.Decisiones.Select(d => d.Substring(0, d.IndexOf(':'))); }

        // ================================================================ el catalogo

        [Test]
        public void El_catalogo_entero_carga_sin_un_solo_error() {
            var c = Catalogo();
            TestContext.WriteLine(c.ToString());
            Assert.IsNotNull(c.Admision, "falta la entrevista del N0");
            CollectionAssert.IsSubsetOf(new[] { "nivel-00", "nivel-01" }, c.Niveles.Keys);
        }

        [Test]
        public void Los_siete_flags_del_puente_estan_en_el_censo_y_Oscar_empieza_vivo() {
            var flags = Flags();
            foreach (var id in new[] { "FLG_DEUDA_TECNICA", "FLG_MORAL_EQUIPO", "FLG_SALUD", "FLG_CALIDAD_ACUM",
                                       "FLG_REPUTACION", "FLG_HORAS_EXTRA", "FLG_VIDA_EXTERNA" })
                Assert.DoesNotThrow(() => flags.Get(id), id);
            Assert.AreEqual(1, flags.Get("FLG_OSCAR_VIVO"));
        }

        [Test]
        public void Cada_coleccionable_implementado_esta_en_algun_sitio_del_mapa() {
            var c = Catalogo();
            var colocados = new HashSet<string>(c.Niveles.Values
                .SelectMany(n => n.Mapa == null ? Enumerable.Empty<Nexus.Core.Jornada.ZonaDeNivel>() : n.Mapa.Zonas)
                .SelectMany(z => z.Coleccionables));
            var huerfanos = c.Coleccionables.Where(col => !colocados.Contains(col.Id)).Select(col => col.Id).ToList();
            Assert.IsEmpty(huerfanos, "un coleccionable que no esta en ninguna zona no se puede encontrar nunca");
        }

        // ================================================================ la entrevista

        [Test]
        public void La_entrevista_no_se_puede_aprobar_contestando_siempre_la_misma_letra() {
            var porLetra = Catalogo().Admision.Preguntas.GroupBy(p => p.Correcta).ToDictionary(g => g.Key, g => g.Count());
            foreach (var kv in porLetra) TestContext.WriteLine($"{kv.Key}: {kv.Value}");
            Assert.LessOrEqual(porLetra.Values.Max(), 6,
                               "si una letra concentrara las respuestas, el pre-test mediria la estrategia, no lo que se sabe");
        }

        [Test]
        public void Contestarlo_todo_bien_da_quince_de_quince() {
            var prueba = Catalogo().Admision;
            var respuestas = prueba.Preguntas.ToDictionary(p => p.Numero, p => p.Correcta);
            Assert.AreEqual(100.0, Nexus.Core.Evaluacion.CorrectorDeAdmision.Porcentaje(
                Nexus.Core.Evaluacion.CorrectorDeAdmision.Corregir(prueba, respuestas)), 1e-9);
        }

        // ================================================================ N0

        [Test]
        public void N0_se_juega_entero_y_cada_dia_trae_su_guia() {
            var vistos = new Dictionary<string, int>();
            foreach (var metodologia in new[] { "scrum", "kanban" })
                for (var semilla = 1; semilla <= 30; semilla++) {
                    var p = Jugar("nivel-00", metodologia, semilla);
                    Assert.IsTrue(p.Sesion.NivelTerminado);

                    CollectionAssert.AreEqual(new[] { "TUT-0.1@1", "TUT-0.2@2", "TUT-0.3@3", "TUT-0.4@4", "TUT-0.5@5" },
                                              p.Beats, $"{metodologia}/{semilla}: el tutorial tiene una guia por dia, en orden");

                    Assert.IsTrue(p.Minijuegos.Any(m => m.StartsWith("MJ-F0-01")),
                                  $"{metodologia}/{semilla}: el tutorial tiene que enseñar un minijuego");
                    Assert.IsTrue(Ids(p).All(id => id.StartsWith("EV-TUT")),
                                  $"{metodologia}/{semilla}: salio un evento que no es del concurso");
                    foreach (var id in Ids(p)) { int n; vistos.TryGetValue(id, out n); vistos[id] = n + 1; }
                }
            foreach (var kv in vistos) TestContext.WriteLine($"{kv.Key} salio en {kv.Value} de 60 partidas");
            Assert.AreEqual(60, vistos.ContainsKey("EV-TUT-01") ? vistos["EV-TUT-01"] : 0,
                            "el tutorial tiene que enseñar una decision en todas las partidas");
            Assert.AreEqual(60, vistos.ContainsKey("EV-TUT-02") ? vistos["EV-TUT-02"] : 0,
                            "y las dos, tambien con Kanban: su multiplicador de drama (x0.9) redondea hacia abajo");
        }

        // ================================================================ N1

        [Test]
        public void N1_se_juega_entero_y_las_cadenas_se_respetan_y_se_cierran() {
            int parcheos = 0, volvio = 0, heroes = 0, seFue = 0, alcances = 0, clientes = 0, minijuegos = 0;
            foreach (var metodologia in new[] { "scrum", "kanban", "cascada" })
                for (var semilla = 1; semilla <= 60; semilla++) {
                    var p = Jugar("nivel-01", metodologia, semilla);
                    var ids = Ids(p).ToList();
                    var donde = $"{metodologia}/{semilla}";

                    Assert.IsTrue(p.Sesion.NivelTerminado, donde);
                    Assert.AreEqual(ids.Count, ids.Distinct().Count(), donde + ": ningun evento sale dos veces");
                    Assert.IsFalse(ids.Any(id => id.StartsWith("EV-TUT")), donde + ": un evento del concurso salio en N1");
                    var fallaDiagrama = p.Minijuegos.Any(m => m.StartsWith("MJ-F1-07:parcial") || m.StartsWith("MJ-F1-07:omitido"));
                    var negocio = p.Minijuegos.Any(m => m.StartsWith("MJ-F1-08:") && m.Contains("respuesta:negociar"));
                    Assert.AreEqual(fallaDiagrama, ids.Contains("EV-ALC-01"),
                                    donde + ": «Ah, y también…» sale si y solo si se falló la revisión del diagrama");
                    Assert.AreEqual(negocio, ids.Contains("EV-BUE-02"),
                                    donde + ": «El cliente que entiende» sale si y solo si se negoció el backlog " + string.Join(",", p.Minijuegos) + " / " + string.Join(",", p.Decisiones) + " / exp " + string.Join(",", p.Expiradas));
                    if (fallaDiagrama) alcances++;
                    if (negocio) clientes++;
                    minijuegos += p.Minijuegos.Count;

                    var parcheo = p.Decisiones.FindIndex(d => d.StartsWith("EV-TEC-02:parchear"));
                    if (ids.Contains("EV-TEC-05")) Assert.GreaterOrEqual(parcheo, 0, donde + ": la deuda volvio sin causa");
                    if (parcheo >= 0) { parcheos++; if (p.Decisiones.Skip(parcheo).Any(d => d.StartsWith("EV-TEC-05"))) volvio++; }

                    var heroe = p.Decisiones.FindIndex(d => d.StartsWith("EV-EQ-03:dejarlo"));
                    if (ids.Contains("EV-EQ-02")) Assert.GreaterOrEqual(heroe, 0, donde + ": Oscar se fue sin causa");
                    if (heroe >= 0) { heroes++; if (p.Decisiones.Skip(heroe).Any(d => d.StartsWith("EV-EQ-02"))) seFue++; }

                    CollectionAssert.Contains(p.Beats, "CIN-1.2@6", donde + ": el standup de Oscar es el dia 6");
                }

            TestContext.WriteLine($"parcheo {parcheos} veces y la deuda volvio {volvio}; dejaron solo a Oscar {heroes} y se fue {seFue}");
            TestContext.WriteLine($"minijuegos jugados: {minijuegos} en 180 partidas; fallaron el diagrama {alcances}; negociaron {clientes}");
            Assert.Greater(alcances, 0, "la cadena del requisito ambiguo tiene que haberse visto alguna vez");
            Assert.Greater(clientes, 0, "y la del cliente que entiende, tambien");
            Assert.Greater(parcheos, 0);
            Assert.AreEqual(parcheos, volvio, "una cadena empezada tiene que terminar dentro del nivel");
            Assert.AreEqual(heroes, seFue, "la cadena del bus factor tambien");
        }

        [Test]
        public void Dejar_caducar_la_revision_del_diagrama_tambien_trae_el_requisito_ambiguo() {
            // Si la alerta caduca, se aplica el 'omitido' de la escena, que encadena EV-ALC-01. Con el omitido
            // generico de antes, no mirar el diagrama salia gratis.
            var vistas = 0;
            for (var semilla = 1; semilla <= 40; semilla++) {
                var p = Jugar("nivel-01", "scrum", semilla, dejarCaducarLosMinijuegos: true);
                if (!p.Minijuegos.Any(m => m.StartsWith("MJ-F1-07:caducado"))) continue;
                vistas++;
                CollectionAssert.Contains(Ids(p).ToList(), "EV-ALC-01", $"semilla {semilla}");
            }
            Assert.Greater(vistas, 0);
        }

        [Test]
        public void Con_Scrum_la_planificacion_de_N1_se_abre_al_empezar_cada_sprint() {
            CollectionAssert.AreEqual(new[] { 1, 11 }, Jugar("nivel-01", "scrum", 7).DiasConPlanificacion);
        }

        [Test]
        public void El_sotano_de_N1_no_se_abre_hasta_que_Oscar_se_presenta() {
            var s = Empezar("nivel-01", "scrum", 11);
            for (var dia = 1; dia <= 7; dia++) {
                s.ComenzarDia();
                if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);

                if (dia < 6)
                    Assert.Throws<InvalidOperationException>(() => s.IrAZona("sotano-de-patios"), $"dia {dia}");
                else if (dia == 7)
                    Assert.DoesNotThrow(() => s.IrAZona("sotano-de-patios"), "tras CIN-1.2 el sotano esta abierto");

                if (s.ZonaActual != "escritorio") s.IrAZona("escritorio");
                for (var v = 0; v < 60 && !s.SePuedeCerrarLaJornada; v++) {
                    foreach (var a in s.AvanzarReloj(30).AlertasQueSuenan) s.AtenderAlerta(a.Id);
                    if (s.Decision != null) s.ResolverDecision(s.Decision.Opciones.First(o => !o.Bloqueada).Id);
                }
                s.CerrarJornada();
                s.TerminarDia(false);
            }
        }

        [Test]
        public void En_N1_la_llamada_a_casa_depende_de_donde_vienes() {
            // INT-1 exige haberse ido a casa tres veces; el robot se va siempre. Y el coloreo lee FLG_ORIGEN,
            // que la entrevista del N0 dejo escrito: es el primer flag de color de dialogo con lector.
            var delPueblo = Jugar("nivel-01", "scrum", 3,
                                  Flags(new Dictionary<string, double>(StringComparer.Ordinal) { { "FLG_ORIGEN", 0 } }));
            var universitario = Jugar("nivel-01", "scrum", 3,
                                      Flags(new Dictionary<string, double>(StringComparer.Ordinal) { { "FLG_ORIGEN", 1 } }));

            CollectionAssert.Contains(delPueblo.Variantes, "INT-1:pueblo");
            CollectionAssert.Contains(universitario.Variantes, "INT-1:universitario");
        }
    }
}
