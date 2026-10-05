using System;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Detectar;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// El modo guiado (feedback beta, ronda 2: «colocando todo lo correcto me dice que no lo hice bien»). Seguir
    /// el recorrido de Marisol al pie de la letra tiene que dar SIEMPRE el mejor resultado, en todas las escenas.
    /// </summary>
    public class RecorridoGuiadoTests {
        private static string Carpeta() {
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            return Path.Combine(raiz, "minijuegos");
        }

        private static MinijuegoDef[] Escenas() {
            return Directory.GetFiles(Carpeta(), "MJ-*.json").Select(f => CatalogoMinijuegos.Parsear(File.ReadAllText(f))).ToArray();
        }

        [Test]
        public void Seguir_el_recorrido_guiado_da_siempre_el_mejor_resultado() {
            foreach (var def in Escenas()) {
                var pasos = RecorridoGuiado.Para(def);
                var r = RecorridoGuiado.Aplicar(def, pasos);
                Assert.AreEqual(ResultadosDeMinijuego.Todos, r.Resultado, def.Id + ": " + string.Join(" | ", r.Detalle));
            }
        }

        [Test]
        public void Cada_paso_dice_que_hacer_y_por_que() {
            foreach (var def in Escenas())
                foreach (var p in RecorridoGuiado.Para(def)) {
                    Assert.IsFalse(string.IsNullOrEmpty(p.Texto), def.Id + ": un paso sin texto");
                    Assert.IsFalse(string.IsNullOrEmpty(p.Porque), def.Id + ": el paso «" + p.Texto + "» no explica por qué");
                }
        }

        [Test]
        public void El_recorrido_termina_entregando_y_deja_lecciones_para_la_proxima_vez() {
            foreach (var def in Escenas()) {
                var pasos = RecorridoGuiado.Para(def);
                Assert.AreEqual(AccionGuiada.Entregar, pasos.Last().Accion, def.Id);
                Assert.IsNotEmpty(RecorridoGuiado.Lecciones(pasos), def.Id + ": sin «qué mirar la próxima vez»");
            }
        }

        [Test]
        public void Los_senuelos_se_explican_pero_nunca_se_piden_marcar() {
            foreach (var def in Escenas().Where(d => Verbos.Normalizar(d.Verbo) == Verbos.Detectar)) {
                var pedidas = RecorridoGuiado.Para(def).Where(p => p.Accion == AccionGuiada.Seleccionar).Select(p => p.Objetivo).ToList();
                foreach (var s in def.Senuelos) CollectionAssert.IsEmpty(s.Commits.Intersect(pedidas), def.Id + "/" + s.Id);
            }
        }

        [Test]
        public void Una_marca_que_mezcla_la_zona_y_un_senuelo_nunca_es_el_mejor_resultado() {
            // Lo que le pasaba al tester: pinchar para mirar dejaba seleccionadas dos piezas, y la marca llevaba las dos.
            var def = CatalogoMinijuegos.Parsear(File.ReadAllText(Path.Combine(Carpeta(), "MJ-OF-N0-DIAGRAMA.json")));
            var estado = new DetectarState(90);
            estado.Alternar(def.Zonas[0].Commits[0]);
            estado.Alternar(def.Senuelos[0].Commits[0]);
            estado.Marcar(def.Zonas[0].Defecto, 1);
            // Con la tolerancia de la escena, un señuelo ya no lo anula todo: se queda en parcial, y el cierre lo explica.
            var r = DetectarEvaluador.Evaluar(def, estado);
            Assert.AreEqual(ResultadosDeMinijuego.Parcial, r.Resultado);
            Assert.IsTrue(r.Detalle.Any(d => d.Contains("estaba bien")));

            def.ToleranciaDeSenuelos = 0;
            Assert.AreEqual(ResultadosDeMinijuego.FalsoPositivo, DetectarEvaluador.Evaluar(def, estado).Resultado,
                            "sin tolerancia sigue mandando el señuelo: por eso la pantalla ya no acumula la selección");
        }
    }
}
