using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Metodologia;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// Elegir cuanto dura cada sprint, etapa o revision (ronda 6), y armar el sprint backlog tarjeta a tarjeta.
    /// La opcion de serie deja el calendario como estaba; las demas solo cambian el calendario, y de el salen las
    /// ceremonias y los despliegues.
    /// </summary>
    public class CalendarioElegidoTests {
        private static Catalogo _catalogo;

        private static Catalogo Catalogo() {
            if (_catalogo != null) return _catalogo;
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            return _catalogo = CatalogLoader.CargarTodo(new CatalogoDeArchivos(raiz));
        }

        private static GameSession Nueva(string nivel, string metodologia, string duracion = null) {
            var flags = new FlagStore(new Dictionary<string, double>(StringComparer.Ordinal), Catalogo().Flags);
            flags.Inicializar();
            var s = new GameSession(Catalogo(), nivel, flags, 11);
            s.ElegirMetodologia(metodologia, Catalogo().Metodologias[metodologia].TextosDeRazones.Keys.First());
            if (duracion != null) s.AjustarCalendario(duracion);
            s.RepartirCalidad(new Dictionary<string, int>());
            var arq = Catalogo().Niveles[nivel].Fase1.Arquitecturas[0];
            s.ElegirArquitectura(arq.Id, Catalogo().Niveles[nivel].Fase1.RazonesDe(arq)[0].Id);
            s.CerrarFase1();
            return s;
        }

        [Test]
        public void Cada_metodologia_ofrece_duraciones_que_cubren_todos_los_dias_y_una_es_la_de_serie() {
            foreach (var nivel in Catalogo().Niveles.Values)
                foreach (var id in nivel.MetodologiasPermitidas) {
                    var opciones = OpcionesDeCalendario.Para(Catalogo().Metodologias[id], nivel.DiasTotales);
                    var donde = nivel.Id + "/" + id;
                    Assert.GreaterOrEqual(opciones.Count, 2, donde + ": no hay nada que elegir");
                    Assert.AreEqual(1, opciones.Count(o => o.EsLaDeSerie), donde + ": tiene que haber una y solo una opción de serie");
                    CollectionAssert.AllItemsAreUnique(opciones.Select(o => o.Id).ToList(), donde);
                    foreach (var o in opciones) {
                        Assert.IsFalse(string.IsNullOrEmpty(o.Titulo), donde + "/" + o.Id);
                        Assert.IsFalse(string.IsNullOrEmpty(o.Detalle), donde + "/" + o.Id);
                        Assert.AreEqual(nivel.DiasTotales, o.Unidades.Sum(), $"{donde}/{o.Id}: las unidades no cubren el nivel");
                        Assert.IsTrue(o.Unidades.All(d => d >= 1), $"{donde}/{o.Id}");
                        Assert.AreEqual(nivel.DiasTotales, o.Despliegues.Last(), $"{donde}/{o.Id}: el último día siempre despliega");
                    }
                }
        }

        [Test]
        public void La_opcion_de_serie_deja_el_calendario_como_estaba() {
            foreach (var nivel in Catalogo().Niveles.Values)
                foreach (var id in nivel.MetodologiasPermitidas) {
                    var sin = Nueva(nivel.Id, id);
                    var serie = OpcionesDeCalendario.Para(Catalogo().Metodologias[id], nivel.DiasTotales).First(o => o.EsLaDeSerie);
                    var con = Nueva(nivel.Id, id, serie.Id);
                    CollectionAssert.AreEqual(sin.Reglas.Tramos.Select(t => t.DiaFin).ToList(), con.Reglas.Tramos.Select(t => t.DiaFin).ToList(), nivel.Id + "/" + id);
                    CollectionAssert.AreEqual(sin.DiasDeDespliegue(), con.DiasDeDespliegue(), nivel.Id + "/" + id);
                }
        }

        [Test]
        public void Sprints_mas_cortos_son_mas_planificaciones_y_mas_despliegues() {
            var dos = Nueva("nivel-01", "scrum", "sprints-2");
            var cuatro = Nueva("nivel-01", "scrum", "sprints-4");
            Assert.AreEqual(2, dos.Reglas.Tramos.Count);
            Assert.AreEqual(4, cuatro.Reglas.Tramos.Count);
            CollectionAssert.AreEqual(new[] { 5, 10, 15, 20 }, cuatro.DiasDeDespliegue());
            CollectionAssert.AreEqual(new[] { 10, 20 }, dos.DiasDeDespliegue());

            // El tutorial de cinco dias tambien puede partirse en dos sprints.
            var corto = Nueva("nivel-00", "scrum", "sprints-2");
            CollectionAssert.AreEqual(new[] { 3, 5 }, corto.DiasDeDespliegue());
        }

        [Test]
        public void En_kanban_la_cadencia_mueve_la_revision_del_flujo_y_los_despliegues() {
            var s = Nueva("nivel-01", "kanban", "cada-3");
            CollectionAssert.AreEqual(new[] { 3, 6, 9, 12, 15, 18, 20 }, s.DiasDeDespliegue());
            Assert.IsTrue(s.Reglas.PlanFor(3).Ceremonias.Any(c => c.Id == "revision-del-flujo"));
            Assert.IsFalse(s.Reglas.PlanFor(5).Ceremonias.Any(c => c.Id == "revision-del-flujo"));
        }

        [Test]
        public void En_cascada_el_reparto_cambia_los_dias_de_cada_etapa_sin_cambiar_cuantas_hay() {
            var serie = Nueva("nivel-01", "cascada");
            var analisis = Nueva("nivel-01", "cascada", "mas-analisis");
            var construccion = Nueva("nivel-01", "cascada", "mas-construccion");
            Func<GameSession, int, int> dias = (s, i) => s.Reglas.Tramos[i].DiaFin - s.Reglas.Tramos[i].DiaInicio + 1;
            Assert.AreEqual(serie.Reglas.Tramos.Count, analisis.Reglas.Tramos.Count);
            Assert.Greater(dias(analisis, 0) + dias(analisis, 1), dias(serie, 0) + dias(serie, 1));
            Assert.Greater(dias(construccion, 2) + dias(construccion, 3), dias(serie, 2) + dias(serie, 3));
            Assert.AreEqual(20, analisis.Reglas.Tramos.Last().DiaFin);
            CollectionAssert.AreEqual(new[] { 20 }, analisis.DiasDeDespliegue(), "en cascada se despliega una vez, al final");
        }

        [Test]
        public void Una_duracion_que_no_es_de_esa_metodologia_no_se_acepta_y_elegir_dos_no_las_acumula() {
            var flags = new FlagStore(new Dictionary<string, double>(StringComparer.Ordinal), Catalogo().Flags);
            flags.Inicializar();
            var s = new GameSession(Catalogo(), "nivel-01", flags, 3);
            s.ElegirMetodologia("scrum", Catalogo().Metodologias["scrum"].TextosDeRazones.Keys.First());
            Assert.Throws<InvalidOperationException>(() => s.AjustarCalendario("cada-3"));
            s.AjustarCalendario("sprints-4");
            s.AjustarCalendario("sprints-3");
            Assert.AreEqual(3, s.Reglas.Tramos.Count);
            Assert.AreEqual("sprints-3", s.OpcionDeCalendario);
        }

        [Test]
        public void La_duracion_elegida_viaja_en_el_guardado() {
            var s = Nueva("nivel-01", "scrum", "sprints-4");
            s.ComenzarDia();
            var copia = GameSession.Restaurar(Catalogo(), "nivel-01", s.Flags, 11, s.Capturar());
            Assert.AreEqual("sprints-4", copia.OpcionDeCalendario);
            Assert.AreEqual(4, copia.Reglas.Tramos.Count);
            CollectionAssert.AreEqual(s.DiasDeDespliegue(), copia.DiasDeDespliegue());
        }

        // ---------------------------------------------------------------- el sprint backlog

        [Test]
        public void Con_un_solo_sprint_se_planifica_con_un_numero_y_con_varios_se_arma_el_sprint_backlog() {
            var uno = Nueva("nivel-00", "scrum");
            uno.ComenzarDia();
            Assert.IsFalse(uno.SeArmaElSprintBacklog, "el tutorial de serie es un solo sprint");
            var varios = Nueva("nivel-01", "scrum");
            varios.ComenzarDia();
            Assert.IsTrue(varios.SeArmaElSprintBacklog);
            Assert.IsFalse(Nueva("nivel-01", "kanban").SeArmaElSprintBacklog, "en Kanban no hay sprints");
        }

        [Test]
        public void El_compromiso_es_la_suma_de_las_tarjetas_elegidas_y_solo_esas_entran_al_sprint() {
            var s = Nueva("nivel-01", "scrum");
            s.ComenzarDia();
            Assert.IsNotNull(s.PendingPlanning);
            var capacidad = s.PendingPlanning.CapacidadSugerida;
            var candidatas = s.CandidatasAlSprint();
            // No las primeras: la segunda y la cuarta. Elegir es elegir.
            var elegidas = new[] { candidatas[1], candidatas[3] };
            var puntos = elegidas.Sum(c => c.Restante);
            s.ComprometerTarjetas(elegidas.Select(c => c.Id));

            Assert.IsNull(s.PendingPlanning);
            Assert.AreEqual(puntos, s.R.CompromisoActual, 1e-6);
            Assert.AreEqual(Math.Max(0, puntos - capacidad), s.R.SobreCompromiso, 1e-6);
            var enElSprint = s.Tablero.Tarjetas.Where(c => c.Sprint == s.Tablero.SprintActual).Select(c => c.Id).ToList();
            CollectionAssert.AreEquivalent(elegidas.Select(c => c.Id).ToList(), enElSprint);
            Assert.AreEqual(puntos, s.RestanteDelSprint(), 1e-6);
        }

        [Test]
        public void Pasarse_de_la_capacidad_genera_sobrecompromiso_y_un_sprint_vacio_no_vale() {
            var s = Nueva("nivel-01", "scrum");
            s.ComenzarDia();
            Assert.Throws<InvalidOperationException>(() => s.ComprometerTarjetas(new string[0]));
            s.ComprometerTarjetas(s.CandidatasAlSprint().Select(c => c.Id));
            Assert.Greater(s.R.SobreCompromiso, 0, "todo el backlog no cabe en un sprint");
        }
    }
}
