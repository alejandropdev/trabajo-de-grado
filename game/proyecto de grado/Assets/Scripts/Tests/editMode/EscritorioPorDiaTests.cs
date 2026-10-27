using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Guardado;
using Nexus.Core.Minijuegos;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// El trabajo del escritorio ya no es siempre el mismo caso (ronda 9: «por cada minijuego muchos más casos, uno
    /// diferente por día»). Cada tarea tiene varios casos; cada dia sale otro y no se repite ninguno hasta que han salido
    /// todos. El orden cambia con la semilla de la partida y no gasta el azar de la sesion.
    /// </summary>
    public class EscritorioPorDiaTests {
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

        private static GameSession Nueva(string nivel, int semilla) {
            var flags = new FlagStore(new Dictionary<string, double>(StringComparer.Ordinal), Catalogo().Flags);
            flags.Inicializar();
            var s = new GameSession(Catalogo(), nivel, flags, semilla);
            s.ElegirMetodologia("kanban", Catalogo().Metodologias["kanban"].TextosDeRazones.Keys.First());
            s.RepartirCalidad(new Dictionary<string, int>());
            var arq = s.Perfil.Fase1.Arquitecturas[0];
            s.ElegirArquitectura(arq.Id, s.Perfil.Fase1.RazonesDe(arq)[0].Id);
            s.CerrarFase1();
            return s;
        }

        /// <summary>El caso de una tarea en los dias 1..n (solo se mira el calendario de casos, sin jugar los dias).</summary>
        private static List<string> CasosDeLosDias(GameSession s, string tarea, int dias) {
            var lista = new List<string>();
            for (var d = 1; d <= dias; d++) {
                s.R.DiaActual = d;
                lista.Add(s.CasoDeHoy(tarea).Minijuego);
            }
            return lista;
        }

        [TestCase("nivel-00", 5)]
        [TestCase("nivel-01", 7)]
        public void Cada_tarea_tiene_sus_casos_y_no_se_repite_ninguno_hasta_que_salen_todos(string nivel, int casos) {
            var s = Nueva(nivel, 7);
            foreach (var tarea in s.TareasDeOficina) {
                Assert.AreEqual(casos, tarea.Casos().Count, $"{nivel}/{tarea.Id}");
                var dias = CasosDeLosDias(s, tarea.Id, casos);
                CollectionAssert.AllItemsAreUnique(dias, $"{nivel}/{tarea.Id}: se repitió un caso antes de salir todos");
                CollectionAssert.AreEquivalent(tarea.Casos().Select(c => c.Minijuego), dias, $"{nivel}/{tarea.Id}: quedó algún caso sin salir");
            }
        }

        [Test]
        public void La_misma_semilla_da_el_mismo_orden_y_otra_semilla_otro() {
            var a = CasosDeLosDias(Nueva("nivel-01", 7), "of-diagrama", 7);
            CollectionAssert.AreEqual(a, CasosDeLosDias(Nueva("nivel-01", 7), "of-diagrama", 7));
            Assert.IsTrue(Enumerable.Range(8, 10).Any(sem => !CasosDeLosDias(Nueva("nivel-01", sem), "of-diagrama", 7).SequenceEqual(a)),
                          "con otra semilla el orden de los casos cambia");
        }

        [Test]
        public void Todos_los_casos_de_una_tarea_son_la_misma_practica() {
            foreach (var nivel in Catalogo().Niveles.Values.Where(n => n.Oficina != null))
                foreach (var tarea in nivel.Oficina.Tareas) {
                    var verbos = tarea.Casos()
                        .Select(c => Verbos.Normalizar(CatalogoMinijuegos.Parsear(File.ReadAllText(Path.Combine(Raiz(), c.Archivo))).Verbo))
                        .Distinct().ToList();
                    Assert.AreEqual(1, verbos.Count, $"{nivel.Id}/{tarea.Id}");
                }
        }

        [Test]
        public void Las_revisiones_de_codigo_son_del_proyecto_y_no_se_repiten_en_todo_el_nivel() {
            // Ronda 9: «en las revisiones de código siempre me sale lo mismo». Antes se sorteaba cada día y se repetían.
            var s = Nueva("nivel-01", 7);
            var vistas = new List<string>();
            for (var dia = 0; dia < 20 && s.R.Fase == 2; dia++) {
                s.ComenzarDia();
                if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
                if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0) s.ElegirAccionRetro(s.PendingRetro.Acciones[0].Id);
                if (!string.IsNullOrEmpty(s.R.PrDeHoy)) vistas.Add(s.R.PrDeHoy);
                for (var v = 0; v < 60 && !s.SePuedeCerrarLaJornada; v++) {
                    foreach (var a in s.AvanzarReloj(30).AlertasQueSuenan) s.AtenderAlerta(a.Id);
                    if (s.Decision != null) s.ResolverDecision(s.Decision.Opciones.First(o => !o.Bloqueada).Id);
                    if (s.Minijuego != null) s.ResolverMinijuego(PuenteDelMotor.Omitido(s.Minijuego.MinijuegoId, s.Minijuego.ObjetivoAprendizaje, null));
                }
                s.CerrarJornada();
                s.TerminarDia(false);
            }
            Assert.GreaterOrEqual(vistas.Count, 5, "en veinte días salen bastantes revisiones");
            CollectionAssert.AllItemsAreUnique(vistas, "ninguna revisión se repite en el nivel");
            foreach (var id in vistas) {
                var pr = Catalogo().MicroPrs.First(p => p.Id == id);
                Assert.IsTrue(pr.Niveles.Count == 0 || pr.Niveles.Contains("nivel-01"), id + " es de otro proyecto");
            }
            Assert.GreaterOrEqual(Catalogo().MicroPrs.Count(p => p.Niveles.Contains("nivel-01")), 10, "hay muchas revisiones de Cradle Lifts");
        }

        [Test]
        public void El_caso_abierto_es_el_de_hoy_y_cargar_la_partida_no_lo_cambia() {
            var s = Nueva("nivel-01", 7);
            s.ComenzarDia();
            if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
            var antes = s.CasoDeHoy("of-pruebas").Minijuego;
            s.EmpezarTarea("of-pruebas");
            Assert.AreEqual(antes, s.CasoDeHoy("of-pruebas").Minijuego, "al empezarla, el caso abierto es el que se anunciaba");
            var copia = GameSession.Restaurar(Catalogo(), "nivel-01", s.Flags, 7,
                                              JsonDeGuardado.Deserializar<NivelEnCurso>(JsonDeGuardado.Serializar(s.Capturar())));
            Assert.AreEqual(antes, copia.CasoDeHoy("of-pruebas").Minijuego);
        }
    }
}
