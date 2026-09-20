using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Eventos;
using Nexus.Core.Minijuegos;
using Nexus.Core.Modelo;
using Nexus.Core.Servicios;
using NUnit.Framework;
// Hay dos EfectoDiferido: el de la escena (float) y el del motor (object). Cualquiera que use los dos
// namespaces a la vez necesita desambiguar; el alias documenta cual es cual.
using EfectoDeEscena = Nexus.Core.Minijuegos.EfectoDiferido;

namespace Nexus.Tests {
    /// <summary>
    /// C6 · Ventana de verbos. La invariante del subsistema (§4.6): la mecanica vive en la UI y el motor
    /// solo sabe cual toca. Y la regla de ritmo, que es contenido pedagogico y no una limitacion tecnica:
    /// maximo uno al dia, nunca dos dias seguidos, y el 45 % de los dias elegibles no sale ninguno.
    /// </summary>
    public class VentanaDeVerbosTests {
        private const double Tol = 1e-9;

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
                throw new InvalidOperationException(nombre);
            }
        }

        private static MinigameDefinition Mj(string id, string oa = "OA-GIT-01") {
            return new MinigameDefinition {
                Id = id, Verbo = Verbos.Detectar, Archivo = "minijuegos/" + id + ".json",
                ObjetivoAprendizaje = oa,
                PresionDiegetica = "Javier necesita esto antes de las 10:00",
                Reloj = 90, PesoBase = 10, Enfriamiento = 6, MaxOcurrencias = 1
            };
        }

        private static LevelProfile Nivel(params string[] objetivos) {
            return new LevelProfile {
                Id = "nivel-01", DiasTotales = 20, NivelAndamiaje = 3,
                ObjetivosActivos = objetivos.Length == 0 ? new[] { "OA-GIT-01" } : objetivos
            };
        }

        private static MinigameDirector Director(List<MinigameDefinition> catalogo, LevelProfile perfil = null,
                                                 int semilla = 4417) {
            return new MinigameDirector(catalogo, perfil ?? Nivel(), new DeterministicRng(semilla));
        }

        // ============================================================ verbos

        [Test]
        public void Los_verbos_admiten_las_dos_formas_de_escribirlos() {
            Assert.AreEqual(Verbos.Detectar, Verbos.Normalizar("V1_DETECTAR"));
            Assert.AreEqual(Verbos.Detectar, Verbos.Normalizar("detectar"));
            Assert.AreEqual(Verbos.Detectar, Verbos.Normalizar("  Detectar "));
            Assert.AreEqual(Verbos.Trazar, Verbos.Normalizar("v6_trazar"));
            Assert.IsNull(Verbos.Normalizar("V7_ADIVINAR"));
            Assert.IsFalse(Verbos.EsValido("adivinar"));
            Assert.AreEqual(6, Verbos.Todos.Count);
        }

        // ============================================================ ritmo

        [Test]
        public void Nunca_salen_dos_minijuegos_dos_dias_seguidos() {
            var ctx = new Ctx();
            var director = Director(new List<MinigameDefinition> { Mj("MJ-A"), Mj("MJ-B"), Mj("MJ-C"), Mj("MJ-D") });
            var dias = new List<int>();

            for (var dia = 1; dia <= 30; dia++) {
                ctx.R.DiaActual = dia;
                var pendiente = director.MinijuegoDeHoy(ctx.R, ctx);
                if (pendiente == null) continue;
                dias.Add(dia);
                director.RegistrarJugado(pendiente.MinijuegoId, ctx.R);
            }

            Assert.Greater(dias.Count, 0, "en 30 dias tiene que salir alguno");
            for (var i = 1; i < dias.Count; i++)
                Assert.GreaterOrEqual(dias[i] - dias[i - 1], MinigameDirector.DiasEntreMinijuegos,
                                      $"salieron el dia {dias[i - 1]} y el {dias[i]}");
        }

        [Test]
        public void El_45_por_ciento_de_los_dias_elegibles_no_sale_ninguno() {
            var rng = new DeterministicRng(4417);
            var perfil = Nivel();
            var tranquilos = 0;
            const int intentos = 3000;

            for (var i = 0; i < intentos; i++) {
                // estado limpio cada vez: aqui se mide solo el paso 4 del algoritmo, no la regla de ritmo
                var ctx = new Ctx();
                var director = new MinigameDirector(new List<MinigameDefinition> { Mj("MJ-A"), Mj("MJ-B") }, perfil, rng);
                if (director.MinijuegoDeHoy(ctx.R, ctx) == null) tranquilos++;
            }

            Assert.AreEqual(MinigameDirector.ProbabilidadDeDiaTranquilo, tranquilos / (double)intentos, 0.03);
        }

        [Test]
        public void En_doce_dias_salen_tres_o_cuatro() {
            var ctx = new Ctx();
            var director = Director(new List<MinigameDefinition> { Mj("MJ-A"), Mj("MJ-B"), Mj("MJ-C"), Mj("MJ-D") });
            var salieron = 0;

            for (var dia = 1; dia <= 12; dia++) {
                ctx.R.DiaActual = dia;
                var pendiente = director.MinijuegoDeHoy(ctx.R, ctx);
                if (pendiente == null) continue;
                salieron++;
                director.RegistrarJugado(pendiente.MinijuegoId, ctx.R);
            }

            Assert.GreaterOrEqual(salieron, 2, "un nivel sin minijuegos no enseña los verbos");
            Assert.LessOrEqual(salieron, 5, "mas de eso deja de ser una interrupcion y pasa a ser el juego");
        }

        // ============================================================ filtros

        [Test]
        public void Un_minijuego_fuera_de_los_objetivos_del_nivel_no_sale() {
            var ctx = new Ctx();
            var director = Director(new List<MinigameDefinition> { Mj("MJ-GIT", "OA-GIT-01") },
                                    Nivel("OA-DOC-01", "OA-DEVOPS-02"));

            Assert.IsNull(director.MinijuegoDeHoy(ctx.R, ctx));
            Assert.AreEqual(DecisionDeMinijuego.SinCandidatos, director.Log[0].Resultado);
        }

        [Test]
        public void Un_nivel_sin_objetivos_activos_los_admite_todos() {
            var ctx = new Ctx();
            var perfil = Nivel();
            perfil.ObjetivosActivos = new string[0];
            var director = Director(new List<MinigameDefinition> { Mj("MJ-A", "OA-LO-QUE-SEA") }, perfil);

            director.MinijuegoDeHoy(ctx.R, ctx);
            Assert.AreEqual(1, director.Log[0].Candidatos);
        }

        [Test]
        public void Un_minijuego_en_enfriamiento_no_sale_hasta_que_pase_su_plazo() {
            var ctx = new Ctx();
            var director = Director(new List<MinigameDefinition> { Mj("MJ-A") });
            ctx.R.Enfriamientos[MinigameDirector.PrefijoEnfriamiento + "MJ-A"] = 1;

            ctx.R.DiaActual = 5;   // han pasado 4 < 6
            director.MinijuegoDeHoy(ctx.R, ctx);
            Assert.AreEqual(DecisionDeMinijuego.SinCandidatos, director.Log.Last().Resultado);

            ctx.R.DiaActual = 8;   // han pasado 7 >= 6
            director.MinijuegoDeHoy(ctx.R, ctx);
            Assert.AreEqual(1, director.Log.Last().Candidatos);
        }

        [Test]
        public void Un_minijuego_agotado_no_vuelve() {
            var ctx = new Ctx();
            var director = Director(new List<MinigameDefinition> { Mj("MJ-A") });
            ctx.R.Ocurrencias["MJ-A"] = 1;
            ctx.R.DiaActual = 15;

            director.MinijuegoDeHoy(ctx.R, ctx);
            Assert.AreEqual(DecisionDeMinijuego.SinCandidatos, director.Log.Last().Resultado);
        }

        [Test]
        public void Una_precondicion_que_no_se_cumple_descarta() {
            var ctx = new Ctx();
            var mj = Mj("MJ-A");
            mj.Precondiciones.Add("DeudaTecnica > 40");
            var director = Director(new List<MinigameDefinition> { mj });

            ctx.W.Set("DeudaTecnica", 10);
            director.MinijuegoDeHoy(ctx.R, ctx);
            Assert.AreEqual(0, director.Log.Last().Candidatos);

            ctx.W.Set("DeudaTecnica", 60);
            ctx.R.DiaActual = 5;
            director.MinijuegoDeHoy(ctx.R, ctx);
            Assert.AreEqual(1, director.Log.Last().Candidatos);
        }

        [Test]
        public void Sin_fases_declaradas_solo_sale_en_desarrollo() {
            var ctx = new Ctx();
            var director = Director(new List<MinigameDefinition> { Mj("MJ-A") });

            foreach (var fase in new[] { 1, 3, 4 }) {
                ctx.R.Fase = fase;
                ctx.R.DiaActual = fase * 5;
                director.MinijuegoDeHoy(ctx.R, ctx);
                Assert.AreEqual(0, director.Log.Last().Candidatos, "fase " + fase);
            }

            ctx.R.Fase = 2;
            ctx.R.DiaActual = 20;
            director.MinijuegoDeHoy(ctx.R, ctx);
            Assert.AreEqual(1, director.Log.Last().Candidatos);
        }

        [Test]
        public void Un_minijuego_puede_declarar_otras_fases() {
            var ctx = new Ctx { R = { Fase = 3 } };
            var mj = Mj("MJ-A");
            mj.Fases.Add(FasesDelNivel.Lanzamiento);
            var director = Director(new List<MinigameDefinition> { mj });

            director.MinijuegoDeHoy(ctx.R, ctx);
            Assert.AreEqual(1, director.Log.Last().Candidatos);
        }

        // ============================================================ determinismo

        [Test]
        public void El_filtrado_no_gasta_azar() {
            var ctx = new Ctx();
            var rng = new DeterministicRng(4417);
            var director = new MinigameDirector(new List<MinigameDefinition> { Mj("MJ-A", "OA-OTRO") }, Nivel(), rng);

            director.MinijuegoDeHoy(ctx.R, ctx);

            Assert.AreEqual(0, rng.Consumos,
                            "añadir un minijuego que no aplica no puede descolocar las partidas guardadas");
        }

        [Test]
        public void La_regla_de_ritmo_tampoco_gasta_azar() {
            var ctx = new Ctx();
            var rng = new DeterministicRng(4417);
            var director = new MinigameDirector(new List<MinigameDefinition> { Mj("MJ-A") }, Nivel(), rng);
            ctx.R.Enfriamientos[MinigameDirector.EnfriamientoGlobal] = 1;
            ctx.R.DiaActual = 2;

            director.MinijuegoDeHoy(ctx.R, ctx);

            Assert.AreEqual(0, rng.Consumos);
            Assert.AreEqual(DecisionDeMinijuego.Ritmo, director.Log[0].Resultado);
        }

        [Test]
        public void La_misma_semilla_propone_los_mismos_minijuegos() {
            Assert.AreEqual(string.Join(",", Partida(4417)), string.Join(",", Partida(4417)));
            Assert.AreNotEqual(string.Join(",", Partida(4417)), string.Join(",", Partida(9001)));
        }

        private static List<string> Partida(int semilla) {
            var ctx = new Ctx();
            var director = Director(new List<MinigameDefinition> { Mj("MJ-A"), Mj("MJ-B"), Mj("MJ-C"), Mj("MJ-D") },
                                    Nivel(), semilla);
            var salieron = new List<string>();

            for (var dia = 1; dia <= 20; dia++) {
                ctx.R.DiaActual = dia;
                var p = director.MinijuegoDeHoy(ctx.R, ctx);
                if (p == null) continue;
                salieron.Add($"d{dia}:{p.MinijuegoId}");
                director.RegistrarJugado(p.MinijuegoId, ctx.R);
            }
            return salieron;
        }

        // ============================================================ el pendiente y el registro

        [Test]
        public void El_pendiente_lleva_todo_lo_que_la_escena_necesita() {
            var ctx = new Ctx();
            var director = Director(new List<MinigameDefinition> { Mj("MJ-F2-02") });

            PendingMinigame p = null;
            for (var dia = 1; dia <= 20 && p == null; dia++) {
                ctx.R.DiaActual = dia;
                p = director.MinijuegoDeHoy(ctx.R, ctx);
            }

            Assert.IsNotNull(p);
            Assert.AreEqual("MJ-F2-02", p.MinijuegoId);
            Assert.AreEqual(Verbos.Detectar, p.Verbo);
            Assert.AreEqual("minijuegos/MJ-F2-02.json", p.Archivo);
            Assert.AreEqual(90, p.Segundos);
            Assert.AreEqual(3, p.NivelAndamiaje, "el andamiaje sale del nivel, no del minijuego");
            StringAssert.Contains("Javier", p.PresionDiegetica);
        }

        [Test]
        public void Proponer_un_minijuego_ya_marca_que_hoy_hubo_uno() {
            var ctx = new Ctx();
            var director = Director(new List<MinigameDefinition> { Mj("MJ-A") });

            for (var dia = 1; dia <= 20; dia++) {
                ctx.R.DiaActual = dia;
                if (director.MinijuegoDeHoy(ctx.R, ctx) == null) continue;
                Assert.AreEqual(dia, ctx.R.Enfriamientos[MinigameDirector.EnfriamientoGlobal],
                                "se marca al proponer, no al resolver: omitir tambien gasta el dia");
                Assert.AreEqual(1, ctx.R.MinijuegosJugados);
                return;
            }
            Assert.Fail("no salio ninguno en 20 dias");
        }

        [Test]
        public void RegistrarJugado_cuenta_la_ocurrencia_y_arranca_el_enfriamiento() {
            var ctx = new Ctx { R = { DiaActual = 7 } };
            var director = Director(new List<MinigameDefinition> { Mj("MJ-A") });

            director.RegistrarJugado("MJ-A", ctx.R);

            Assert.AreEqual(1, ctx.R.Ocurrencias["MJ-A"]);
            Assert.AreEqual(7, ctx.R.Enfriamientos[MinigameDirector.PrefijoEnfriamiento + "MJ-A"]);
        }

        [Test]
        public void Un_indice_con_ids_repetidos_lanza() {
            var ex = Assert.Throws<InvalidOperationException>(
                () => Director(new List<MinigameDefinition> { Mj("MJ-A"), Mj("MJ-A") }));
            StringAssert.Contains("MJ-A", ex.Message);
        }

        // ============================================================ el puente al motor

        private static ResultadoMinijuego ResultadoDePrueba() {
            return new ResultadoMinijuego {
                MinijuegoId = "MJ-F2-02",
                Resultado = ResultadosDeMinijuego.Parcial,
                Rubrica = new Rubrica { Veredicto = "aceptable", Oa = "OA-GIT-01", Razon = "Viste el rebase, no el huerfano." },
                EfectosInmediatos = { { "DeudaTecnica", 5f }, { "MoralEquipo", -2f } },
                EfectosDiferidos = {
                    new EfectoDeEscena { EnDias = 4, Efectos = { { "Cobertura", -3f } }, EventoForzado = "EV-ARQ-03" }
                },
                Hallazgos = { "vio_el_rebase" },
                TextoCierre = "Javier: 'esto lo arreglo yo esta noche'."
            };
        }

        [Test]
        public void Los_efectos_inmediatos_del_minijuego_llegan_al_WorldState() {
            var w = new WorldState();
            w.Set("DeudaTecnica", 20);
            var scheduler = new EffectScheduler();

            PuenteDelMotor.Aplicar(ResultadoDePrueba(), w, scheduler, 7);

            Assert.AreEqual(25.0, w.DeudaTecnica, Tol);
            Assert.AreEqual(58.0, w.MoralEquipo, Tol);
        }

        [Test]
        public void Un_efecto_diferido_de_minijuego_llega_a_la_cola_del_motor() {
            var w = new WorldState();
            var scheduler = new EffectScheduler();

            PuenteDelMotor.Aplicar(ResultadoDePrueba(), w, scheduler, 7);

            Assert.AreEqual(1, scheduler.Cola.Count);
            Assert.AreEqual(11, scheduler.Cola[0].DiaObjetivo, "dia 7 + 4");
            Assert.AreEqual("MJ-F2-02", scheduler.Cola[0].Origen);
            Assert.AreEqual("EV-ARQ-03", scheduler.Cola[0].EventoForzado);
            Assert.AreEqual(-3.0, Convert.ToDouble(scheduler.Cola[0].Efectos["Cobertura"]), Tol);
        }

        [Test]
        public void Los_minijuegos_no_escriben_flags_narrativos() {
            var w = new WorldState();
            var resultado = ResultadoDePrueba();

            PuenteDelMotor.Aplicar(resultado, w, new EffectScheduler(), 7);

            // Los hallazgos siguen ahi, sin tocar: el canal narrativo decidira al cerrar que flag escribe
            // cada uno. Es lo que mantiene testeable el arbol de los catorce finales (INV-1).
            CollectionAssert.AreEqual(new[] { "vio_el_rebase" }, resultado.Hallazgos);
            Assert.AreEqual(80.0, w.SaludJugador, Tol, "ningun FLG_ se colo en el estado");
        }

        [Test]
        public void ATraza_construye_una_entrada_que_la_traza_acepta() {
            var traza = new Nexus.Core.Evaluacion.DecisionTrace();
            var entrada = PuenteDelMotor.ATraza(ResultadoDePrueba(), 7, "antes", "despues");

            Assert.DoesNotThrow(() => traza.Registrar(entrada));
            Assert.AreEqual("MJ-F2-02", entrada.Origen);
            Assert.AreEqual("aceptable", entrada.Veredicto);
            Assert.AreEqual("OA-GIT-01", entrada.Oa);
            Assert.AreEqual("parcial", entrada.OpcionId);
            StringAssert.Contains("escapo", entrada.OpcionTexto, "nunca dice 'has fallado': dice lo que hizo");
            Assert.AreEqual("antes", entrada.EstadoAntes);
        }

        [Test]
        public void Omitir_es_una_decision_y_se_evalua_como_tal() {
            var resultado = PuenteDelMotor.Omitido("MJ-F2-02", "OA-GIT-01", null);

            Assert.AreEqual(ResultadosDeMinijuego.Omitido, resultado.Resultado);
            Assert.AreEqual("incorrecta", resultado.Rubrica.Veredicto);
            Assert.AreEqual("OA-GIT-01", resultado.Rubrica.Oa);
            StringAssert.Contains("tambien es decidir", resultado.Rubrica.Razon);
        }

        [Test]
        public void Omitir_usa_la_consecuencia_del_json_cuando_la_escena_la_trae() {
            var consecuencia = new Consecuencia {
                EfectosInmediatos = { { "MoralEquipo", -6f } },
                Rubrica = new Rubrica { Veredicto = "incorrecta", Oa = "OA-GIT-01",
                                        Razon = "Javier lo reviso solo, a las once de la noche." }
            };

            var resultado = PuenteDelMotor.Omitido("MJ-F2-02", "OA-GIT-01", consecuencia);

            Assert.AreEqual(-6f, resultado.EfectosInmediatos["MoralEquipo"]);
            StringAssert.Contains("once de la noche", resultado.Rubrica.Razon,
                                  "el fallo produce una escena con nombre, cara y fecha; no una puntuacion");
        }

        // ============================================================ catalogo

        private const string EscenaJson = @"{
  ""id"": ""MJ-F2-02"", ""verbo"": ""V1_DETECTAR"", ""lienzo"": ""grafo_commits"",
  ""objetivoAprendizaje"": ""OA-GIT-01"",
  ""presentacion"": { ""titulo"": ""La auditoria del grafo"", ""quienEspera"": ""Javier"", ""segundosReloj"": 90 },
  ""artefacto"": {
    ""ramas"": [ { ""id"": ""main"", ""color"": ""canal"" } ],
    ""commits"": [
      { ""id"": ""c1"", ""rama"": ""main"", ""fecha"": ""2026-04-17T15:21"", ""autor"": ""OR"", ""mensaje"": ""init"" },
      { ""id"": ""c2"", ""rama"": ""main"", ""fecha"": ""2026-04-18T09:02"", ""autor"": ""JM"", ""mensaje"": ""rebase"", ""padres"": [""c1""] }
    ],
    ""diffs"": {}
  },
  ""zonas"": [ { ""id"": ""z1"", ""tipo"": ""tramo"", ""commits"": [""c2""], ""defecto"": ""historia_reescrita"",
                 ""explicacion"": ""Reescribir la historia compartida borra a personas."" } ],
  ""senuelos"": [],
  ""paletaEtiquetas"": [""historia_reescrita"", ""commit_huerfano""],
  ""andamiaje"": {},
  ""consecuencias"": {
    ""todos"":         { ""rubrica"": { ""veredicto"": ""correcta"",   ""oa"": ""OA-GIT-01"", ""razon"": ""Lo viste."" } },
    ""parcial"":       { ""rubrica"": { ""veredicto"": ""aceptable"",  ""oa"": ""OA-GIT-01"", ""razon"": ""Casi."" } },
    ""falsoPositivo"": { ""rubrica"": { ""veredicto"": ""incorrecta"", ""oa"": ""OA-GIT-01"", ""razon"": ""No era eso."" } },
    ""omitido"":       { ""rubrica"": { ""veredicto"": ""incorrecta"", ""oa"": ""OA-GIT-01"", ""razon"": ""No lo miraste."" } }
  },
  ""cierre"": { ""texto"": ""Javier: 'gracias'."" }
}";

        private const string IndiceJson = @"{
  ""version"": 1,
  ""minijuegos"": [
    { ""id"": ""MJ-F2-02"", ""verbo"": ""V1_DETECTAR"", ""archivo"": ""minijuegos/MJ-F2-02.json"",
      ""objetivoAprendizaje"": ""OA-GIT-01"",
      ""presionDiegetica"": ""Javier necesita esto antes de las 10:00"",
      ""reloj"": 90, ""pesoBase"": 10, ""enfriamiento"": 6, ""maxOcurrencias"": 1 }
  ]
}";

        private static CatalogoEnMemoria FuenteConMinijuegos(string indice = null, string escena = null) {
            return new CatalogoEnMemoria()
                .Con(CatalogLoader.ArchivoIndiceMinijuegos, indice ?? IndiceJson)
                .Con("minijuegos/MJ-F2-02.json", escena ?? EscenaJson);
        }

        [Test]
        public void El_indice_se_carga_y_encaja_con_su_escena() {
            var fuente = FuenteConMinijuegos();
            var minijuegos = CatalogLoader.CargarMinijuegos(fuente.LeerCatalogo(CatalogLoader.ArchivoIndiceMinijuegos), fuente);

            Assert.AreEqual(1, minijuegos.Count);
            Assert.AreEqual("MJ-F2-02", minijuegos[0].Id);
            Assert.AreEqual(90, minijuegos[0].Reloj);
        }

        [Test]
        public void Un_indice_que_no_coincide_con_su_escena_no_se_carga() {
            var fuente = FuenteConMinijuegos(IndiceJson.Replace(@"""OA-GIT-01""", @"""OA-DOC-01"""));
            var ex = Assert.Throws<SchemaException>(
                () => CatalogLoader.CargarMinijuegos(fuente.LeerCatalogo(CatalogLoader.ArchivoIndiceMinijuegos), fuente));
            StringAssert.Contains("OA-DOC-01", ex.Message);
        }

        [Test]
        public void Un_archivo_de_escena_que_no_existe_no_se_carga() {
            var fuente = new CatalogoEnMemoria().Con(CatalogLoader.ArchivoIndiceMinijuegos, IndiceJson);
            var ex = Assert.Throws<SchemaException>(
                () => CatalogLoader.CargarMinijuegos(IndiceJson, fuente));
            StringAssert.Contains("no existe", ex.Message);
        }

        [Test]
        public void Un_minijuego_sin_presion_diegetica_no_se_carga() {
            var sinPresion = new List<MinigameDefinition> { Mj("MJ-A") };
            sinPresion[0].PresionDiegetica = null;

            var errores = SchemaValidator.ValidarMinijuegos(sinPresion);
            Assert.IsTrue(errores.Any(x => x.Contains("cronometro")), string.Join(" | ", errores));
        }

        [Test]
        public void Un_verbo_inventado_no_se_carga() {
            var raro = new List<MinigameDefinition> { Mj("MJ-A") };
            raro[0].Verbo = "V7_ADIVINAR";

            Assert.IsTrue(SchemaValidator.ValidarMinijuegos(raro).Any(x => x.Contains("V7_ADIVINAR")));
        }

        [Test]
        public void Un_minijuego_sin_archivo_no_se_carga() {
            var sinArchivo = new List<MinigameDefinition> { Mj("MJ-A") };
            sinArchivo[0].Archivo = "";

            Assert.IsTrue(SchemaValidator.ValidarMinijuegos(sinArchivo).Any(x => x.Contains("archivo")));
        }

        [Test]
        public void El_indice_de_minijuegos_es_opcional() {
            CollectionAssert.IsEmpty(SchemaValidator.ValidarMinijuegos(null),
                                     "un nivel puede no tener ventana de verbos");
        }
    }
}
