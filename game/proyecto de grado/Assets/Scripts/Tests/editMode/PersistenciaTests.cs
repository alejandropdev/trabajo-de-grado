using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Nexus.Core;
using Nexus.Core.Evaluacion;
using Nexus.Core.Eventos;
using Nexus.Core.Guardado;
using Nexus.Core.Modelo;
using Nexus.Core.Simulacion;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// C9 · Persistencia. INV-7: recargar no puede cambiar la partida.
    /// Aqui se blinda la mitad unitaria (lo que entra sale identico); el recorrido completo
    /// 10 dias vs 5 + guardar + recargar + 5 llegara cuando exista GameSession.
    /// </summary>
    public class PersistenciaTests {
        private static readonly DateTime Instante = new DateTime(2026, 9, 6, 14, 22, 9, DateTimeKind.Utc).AddTicks(1234567);

        private string _carpetaTemporal;

        [TearDown]
        public void Limpiar() {
            if (_carpetaTemporal != null && Directory.Exists(_carpetaTemporal)) Directory.Delete(_carpetaTemporal, true);
            _carpetaTemporal = null;
        }

        /// <summary>
        /// Antes de que existiera C1 esto era una clase falsa con cuatro campos. Ahora es el WorldState
        /// de verdad, asi que el round-trip prueba el tipo que se va a guardar en produccion.
        /// </summary>
        private static WorldState EstadoDePrueba() {
            var w = new WorldState();
            w.Set("Dias", 4);
            w.Set("Dinero", -1200.5);
            w.Set("DeudaTecnica", 47.25);
            w.Set("SaludJugador", 62);
            return w;
        }

        private sealed class SesionFalsa : ISesionPersistible {
            public string NivelId { get; set; } = "nivel-02";
            public bool NivelTerminado { get; set; }
            public NivelEnCurso Nivel = NivelDePrueba();
            public Dictionary<string, double> Flags = new Dictionary<string, double> { { "FLG_DEUDA_TECNICA", 47 }, { "FLG_SALUD", 62 } };
            public NivelEnCurso Capturar() { return Nivel; }
            public Dictionary<string, double> CapturarFlags() { return Flags; }
        }

        private sealed class FabricaFalsa : IFabricaDeSesion<string> {
            public string Nueva(SaveGame partida) { return "nueva:" + partida.Partida.NivelActualId; }
            public string Restaurar(SaveGame partida, NivelEnCurso nivel) { return "restaurada:" + nivel.PerfilDeNivelId + ":" + nivel.ConsumosDelRng; }
        }

        private static NivelEnCurso NivelDePrueba() {
            var coef = new Coeficientes();
            coef.MultiplicarUno("kappa", 0.85);

            var traza = new DecisionTrace();
            traza.Registrar(new EntradaTraza {
                Dia = 2, Origen = "EV-TEC-02", Titulo = "Lecturas duplicadas", OpcionId = "B",
                OpcionTexto = "Parchear el síntoma", Veredicto = Veredictos.Incorrecta, Oa = "OA-DIS-01", Razon = "…",
                EstadoAntes = "Deuda=10", EstadoDespues = "Deuda=22"
            });

            var competencia = new CompetenceProfile();
            competencia.Acumular("OA-DIS-01", Veredictos.Incorrecta);

            return new NivelEnCurso {
                PerfilDeNivelId = "nivel-02",
                W = EstadoDePrueba(),
                R = new RuntimeState { DiaActual = 4, DiasSeguidosTrabajando = 2 },
                Coef = coef,
                Traza = traza,
                Competencia = competencia,
                Fase1Cerrada = true,
                MetodologiaId = "scrum",
                RazonMetodologia = "el_cliente_cambiara_de_opinion",
                ArquitecturaId = "offline_first",
                RazonArquitectura = "conectividad_intermitente",
                FichasDeCalidad = new Dictionary<string, int> { { "rendimiento", 2 }, { "seguridad", 0 } },
                VelocidadBaseMult = 1.05,
                ThroughputAcumulado = 7.3,
                AvanceAlEmpezarUnidad = 24.0,
                UnidadAnterior = "it1",
                ColaDeEfectos = new List<EfectoEnCola> {
                    new EfectoEnCola { DiaObjetivo = 11, Origen = "EV-TEC-02", EventoForzado = "EV-TEC-05" }
                },
                Telegrafiados = new List<TelegrafiadoPendiente> {
                    new TelegrafiadoPendiente { EventoId = "EV-CLI-01", DiaDelEvento = 9, DiaDelAviso = 7, Emitido = true }
                },
                ConsumosDelRng = 37
            };
        }

        private static SaveStore Store(IAlmacen almacen) {
            return new SaveStore(almacen, () => Instante);
        }

        [Test]
        public void Guardar_y_cargar_devuelve_exactamente_la_misma_partida() {
            var store = Store(new AlmacenEnMemoria());
            var save = store.Crear("p1", "Primer intento", "nivel-02", 4417, true, 2);
            store.Sincronizar(save, new SesionFalsa());

            var cargada = store.Cargar("p1", save.Id);

            Assert.AreEqual(JsonDeGuardado.Serializar(save), JsonDeGuardado.Serializar(cargada));
            Assert.AreEqual(37, cargada.Nivel.ConsumosDelRng);
            Assert.AreEqual("EV-TEC-05", cargada.Nivel.ColaDeEfectos[0].EventoForzado);
            Assert.AreEqual(11, cargada.Nivel.ColaDeEfectos[0].DiaObjetivo);
            Assert.AreEqual(true, cargada.Nivel.Telegrafiados[0].Emitido);
            Assert.AreEqual(0.34, cargada.Nivel.Coef.Kappa, 1e-9);
            Assert.AreEqual(4, cargada.Nivel.Coef.W.Length, "ObjectCreationHandling.Replace: no se duplican los pesos");
            Assert.AreEqual(1, cargada.Nivel.Competencia.PorObjetivo["OA-DIS-01"].Incorrectas);
            Assert.AreEqual(-1200.5, cargada.Nivel.W.Dinero, 1e-9);
            Assert.AreEqual(47.25, cargada.Nivel.W.DeudaTecnica, 1e-9);
            Assert.AreEqual(4, cargada.Nivel.R.DiaActual);
            Assert.AreEqual(2, cargada.Nivel.R.DiasSeguidosTrabajando);
        }

        [Test]
        public void Las_fechas_se_conservan_como_texto_iso() {
            var store = Store(new AlmacenEnMemoria());
            var save = store.Crear("p1", "x", "nivel-01", 1, false, 3);
            var cargada = store.Cargar("p1", save.Id);
            Assert.AreEqual("2026-09-06T14:22:09.1234567Z", cargada.Partida.FechaCreacion);
            Assert.AreEqual("2026-09-06T14:22:09.1234567Z", cargada.Partida.FechaUltimoGuardado);
        }

        [Test]
        public void Los_flags_y_los_OA_conservan_sus_mayusculas_en_el_json() {
            var almacen = new AlmacenEnMemoria();
            var store = Store(almacen);
            var save = store.Crear("p1", "x", "nivel-02", 1, false, 1);
            store.Sincronizar(save, new SesionFalsa());

            var json = almacen.Cargar(SaveStore.ClaveDePartida("p1", save.Id));
            StringAssert.Contains("\"FLG_DEUDA_TECNICA\"", json);
            StringAssert.Contains("\"OA-DIS-01\"", json);
            StringAssert.Contains("\"versionEsquema\"", json);
            StringAssert.Contains("\"consumosDelRng\": 37", json);
            StringAssert.Contains("\"deudaTecnica\": 47.25", json,
                                  "el esquema no cambio al tipar los bloques: el JSON es el mismo de antes");
        }

        [Test]
        public void Una_version_futura_se_rechaza_con_mensaje_claro() {
            var almacen = new AlmacenEnMemoria();
            var store = Store(almacen);
            var save = store.Crear("p1", "x", "nivel-01", 1, false, 1);
            var clave = SaveStore.ClaveDePartida("p1", save.Id);
            var arbol = JObject.Parse(almacen.Cargar(clave));
            arbol["versionEsquema"] = SaveStore.VersionActual + 1;
            arbol["partida"] = "otra forma que este juego no entiende";
            almacen.Guardar(clave, arbol.ToString());

            var ex = Assert.Throws<SaveException>(() => store.Cargar("p1", save.Id));
            StringAssert.Contains("más nueva", ex.Message);
        }

        [Test]
        public void Un_json_corrupto_lanza_SaveException() {
            var almacen = new AlmacenEnMemoria();
            var store = Store(almacen);
            almacen.Guardar(SaveStore.ClaveDePartida("p1", "rota"), "{ \"versionEsquema\": 1, \"id\": ");

            var ex = Assert.Throws<SaveException>(() => store.Cargar("p1", "rota"));
            StringAssert.Contains("dañada", ex.Message);
        }

        [Test]
        public void Un_nivel_sin_cola_de_efectos_no_se_carga() {
            var almacen = new AlmacenEnMemoria();
            var store = Store(almacen);
            var save = store.Crear("p1", "x", "nivel-02", 1, false, 1);
            store.Sincronizar(save, new SesionFalsa());
            var clave = SaveStore.ClaveDePartida("p1", save.Id);
            var arbol = JObject.Parse(almacen.Cargar(clave));
            ((JObject)arbol["nivel"]).Remove("w");
            almacen.Guardar(clave, arbol.ToString());

            var ex = Assert.Throws<SaveException>(() => store.Cargar("p1", save.Id));
            StringAssert.Contains("nivel.w", ex.Message);
        }

        [Test]
        public void Nunca_se_escribe_una_partida_invalida() {
            var almacen = new AlmacenEnMemoria();
            var store = Store(almacen);
            var save = store.Crear("p1", "x", "nivel-02", 1, false, 1);
            var antes = almacen.Cargar(SaveStore.ClaveDePartida("p1", save.Id));

            var sesion = new SesionFalsa();
            sesion.Nivel.Coef = null;
            sesion.Flags["DEUDA"] = 3;

            var ex = Assert.Throws<SaveException>(() => store.Sincronizar(save, sesion));
            StringAssert.Contains("coef", ex.Message);
            StringAssert.Contains("FLG_", ex.Message);
            Assert.AreEqual(antes, almacen.Cargar(SaveStore.ClaveDePartida("p1", save.Id)));
        }

        [Test]
        public void Listar_muestra_la_partida_danada_sin_tumbar_la_lista() {
            var almacen = new AlmacenEnMemoria();
            var store = Store(almacen);
            store.Crear("p1", "A", "nivel-01", 1, false, 1);
            store.Crear("p1", "B", "nivel-01", 2, false, 1);
            almacen.Guardar(SaveStore.ClaveDePartida("p1", "rota"), "no es json");

            var lista = store.Listar("p1");

            Assert.AreEqual(3, lista.Count);
            Assert.IsTrue(lista.Last().Danada);
            Assert.AreEqual("rota", lista.Last().Id);
            Assert.IsTrue(lista.Take(2).All(r => !r.Danada && r.EntreNiveles));
        }

        [Test]
        public void Un_perfil_no_ve_las_partidas_de_otro() {
            var almacen = new AlmacenEnMemoria();
            var store = Store(almacen);
            var deA = store.Crear("A", "de A", "nivel-01", 1, false, 1);
            var deB = store.Crear("B", "de B", "nivel-01", 1, false, 1);

            CollectionAssert.AreEqual(new[] { deA.Id }, store.Listar("A").Select(r => r.Id));

            // Copiar a mano el archivo de B dentro de la carpeta de A no sirve para leerlo.
            almacen.Guardar(SaveStore.ClaveDePartida("A", deB.Id), almacen.Cargar(SaveStore.ClaveDePartida("B", deB.Id)));
            Assert.Throws<SaveException>(() => store.Cargar("A", deB.Id));
        }

        [Test]
        public void Un_id_que_intenta_salir_de_su_carpeta_lanza() {
            var store = Store(new AlmacenEnMemoria());
            Assert.Throws<ArgumentException>(() => store.Cargar("../otro", "x"));
            Assert.Throws<ArgumentException>(() => store.Cargar("p1", "a/b"));
        }

        [Test]
        public void Sin_perfil_se_usa_el_perfil_por_defecto() {
            var store = Store(new AlmacenEnMemoria());
            Assert.AreEqual(SaveStore.PerfilPorDefecto, store.Crear(null, "x", "nivel-01", 1, false, 1).PerfilId);
        }

        [Test]
        public void AbrirSesion_sin_nivel_crea_una_sesion_nueva() {
            var store = Store(new AlmacenEnMemoria());
            var save = store.Crear("p1", "x", "nivel-01", 1, false, 1);
            Assert.AreEqual("nueva:nivel-01", store.AbrirSesion(save, "nivel-01", new FabricaFalsa()));
        }

        [Test]
        public void AbrirSesion_con_nivel_a_medias_restaura() {
            var store = Store(new AlmacenEnMemoria());
            var save = store.Crear("p1", "x", "nivel-02", 1, false, 1);
            store.Sincronizar(save, new SesionFalsa());
            var cargada = store.Cargar("p1", save.Id);
            Assert.AreEqual("restaurada:nivel-02:37", store.AbrirSesion(cargada, "nivel-02", new FabricaFalsa()));
        }

        [Test]
        public void AbrirSesion_con_el_perfil_de_otro_nivel_lanza() {
            var store = Store(new AlmacenEnMemoria());
            var save = store.Crear("p1", "x", "nivel-02", 1, false, 1);
            store.Sincronizar(save, new SesionFalsa());
            var ex = Assert.Throws<SaveException>(() => store.AbrirSesion(save, "nivel-03", new FabricaFalsa()));
            StringAssert.Contains("otro nivel", ex.Message);
        }

        [Test]
        public void Al_terminar_el_nivel_se_tira_el_estado_y_se_quedan_los_flags() {
            var store = Store(new AlmacenEnMemoria());
            var save = store.Crear("p1", "x", "nivel-02", 1, false, 1);
            store.Sincronizar(save, new SesionFalsa());

            var terminada = new SesionFalsa { NivelTerminado = true };
            terminada.Flags["FLG_VIDA_EXTERNA"] = 15;
            store.Sincronizar(save, terminada);

            var cargada = store.Cargar("p1", save.Id);
            Assert.IsNull(cargada.Nivel);
            CollectionAssert.AreEqual(new[] { "nivel-02" }, cargada.Partida.NivelesCompletados);
            Assert.AreEqual(47, cargada.Flags["FLG_DEUDA_TECNICA"]);
            Assert.AreEqual(15, cargada.Flags["FLG_VIDA_EXTERNA"]);

            store.Sincronizar(save, terminada);
            Assert.AreEqual(1, store.Cargar("p1", save.Id).Partida.NivelesCompletados.Count, "no se duplica el nivel completado");
        }

        [Test]
        public void Borrar_un_perfil_borra_solo_sus_partidas() {
            var almacen = new AlmacenEnMemoria();
            var store = Store(almacen);
            store.Crear("A", "1", "nivel-01", 1, false, 1);
            store.Crear("A", "2", "nivel-01", 1, false, 1);
            var deB = store.Crear("B", "3", "nivel-01", 1, false, 1);

            Assert.AreEqual(2, store.BorrarTodasDelPerfil("A"));
            CollectionAssert.IsEmpty(store.Listar("A"));
            Assert.AreEqual(deB.Id, store.Listar("B").Single().Id);
        }

        [Test]
        public void El_almacen_en_memoria_lista_solo_los_hijos_directos() {
            var almacen = new AlmacenEnMemoria();
            almacen.Guardar("perfiles/p1", "{}");
            almacen.Guardar("perfiles/p1/partidas/x", "{}");
            CollectionAssert.AreEqual(new[] { "perfiles/p1" }, almacen.ListarClaves("perfiles/"));
            CollectionAssert.AreEqual(new[] { "perfiles/p1/partidas/x" }, almacen.ListarClaves("perfiles/p1/partidas/"));
        }

        [Test]
        public void La_escritura_atomica_sobrescribe_y_no_deja_temporales() {
            _carpetaTemporal = Path.Combine(Path.GetTempPath(), "nexus-tests-" + Guid.NewGuid().ToString("N"));
            var almacen = new AlmacenDeArchivosAtomico(_carpetaTemporal);
            var store = Store(almacen);

            var save = store.Crear("p1", "x", "nivel-02", 4417, true, 2);
            store.Sincronizar(save, new SesionFalsa());
            store.Sincronizar(save, new SesionFalsa());

            Assert.AreEqual(JsonDeGuardado.Serializar(save), JsonDeGuardado.Serializar(store.Cargar("p1", save.Id)));
            CollectionAssert.IsEmpty(Directory.GetFiles(_carpetaTemporal, "*.tmp", SearchOption.AllDirectories));
            CollectionAssert.AreEqual(new[] { SaveStore.ClaveDePartida("p1", save.Id) },
                                      almacen.ListarClaves(SaveStore.PrefijoDePartidas("p1")));
            Assert.Throws<ArgumentException>(() => almacen.Cargar("../fuera"));
        }

        [Test]
        public void El_autoguardado_cuenta_y_avisa_si_supera_el_umbral() {
            var store = Store(new AlmacenEnMemoria());
            var save = store.Crear("p1", "x", "nivel-02", 1, false, 1);
            var avisos = new List<string>();

            var lento = new AutoGuardado(store, save, avisos.Add, umbralMs: -1);
            lento.Guardar(new SesionFalsa(), AutoGuardado.Motivos.FinDeJornada);
            lento.Guardar(new SesionFalsa(), AutoGuardado.Motivos.CierreFase1);

            Assert.AreEqual(2, lento.Guardados);
            Assert.AreEqual(AutoGuardado.Motivos.CierreFase1, lento.UltimoMotivo);
            Assert.AreEqual(2, avisos.Count);
            StringAssert.Contains("fin de la jornada", avisos[0]);

            var rapido = new AutoGuardado(store, save, avisos.Add);
            rapido.Guardar(new SesionFalsa(), AutoGuardado.Motivos.PausaYSalida);
            Assert.AreEqual(2, avisos.Count, "un guardado de milisegundos no avisa");
            Assert.IsNotNull(store.Cargar("p1", save.Id).Nivel);
        }
    }
}
