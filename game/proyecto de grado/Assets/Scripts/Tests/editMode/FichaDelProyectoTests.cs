using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Minijuegos;
using Nexus.Core.Modelo;
using Nexus.Core.Proyecto;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// El expediente del proyecto (feedback beta, ronda 4: «se puede jugar bien el nivel sin entender de qué era el
    /// proyecto»). Cada nivel trae su ficha, cada minijuego dice a qué módulo pertenece, y las preguntas y el avance
    /// por módulo se calculan bien.
    /// </summary>
    public class FichaDelProyectoTests {
        private static readonly string[] Niveles = { "nivel-00", "nivel-01" };

        private static string Raiz() {
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            return !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
        }

        private static LevelProfile Nivel(string id) {
            return CatalogLoader.CargarPerfil(File.ReadAllText(Path.Combine(Raiz(), "niveles", id + ".json")));
        }

        private static MinijuegoDef Minijuego(string id) {
            return CatalogoMinijuegos.Parsear(File.ReadAllText(Path.Combine(Raiz(), "minijuegos", id + ".json")));
        }

        /// <summary>Los minijuegos de un nivel: los de alerta (indice, soloNiveles) y los de la oficina.</summary>
        private static List<string> MinijuegosDe(LevelProfile nivel) {
            var indice = JObject.Parse(File.ReadAllText(Path.Combine(Raiz(), "minijuegos", "indice.json")));
            var ids = indice["minijuegos"]
                .Where(m => m["soloNiveles"] != null && m["soloNiveles"].Values<string>().Contains(nivel.Id))
                .Select(m => (string)m["id"]).ToList();
            if (nivel.Oficina != null) ids.AddRange(nivel.Oficina.Tareas.Select(t => t.Minijuego));
            return ids.Where(i => !string.IsNullOrEmpty(i)).Distinct().ToList();
        }

        [Test]
        public void Cada_nivel_trae_su_expediente_completo_y_coherente() {
            foreach (var id in Niveles) {
                var n = Nivel(id);
                var f = n.Proyecto;
                Assert.IsNotNull(f, id + ": sin 'proyecto'");
                Assert.IsNotEmpty(f.Nombre, id);
                Assert.IsNotEmpty(f.EnUnaFrase, id);
                Assert.IsNotEmpty(f.Sector.Nombre, id);
                Assert.IsNotEmpty(f.Cliente.QueLeDuele, id);
                Assert.GreaterOrEqual(f.Modulos.Count, 3, id + ": pocos módulos");
                Assert.GreaterOrEqual(f.Vocabulario.Count, 6, id + ": poco vocabulario del sector");
                Assert.IsNotEmpty(f.LoNuevo, id + ": no dice qué tiene de distinto");
                Assert.IsNotEmpty(f.Artefactos, id);
                Assert.IsNotEmpty(f.Contexto.Flujos, id + ": sin diagrama de contexto");
                CollectionAssert.IsEmpty(SchemaValidator.ValidarNivel(n).Where(e => e.Contains("proyecto") || e.Contains("modulo") || e.Contains("flujo")), id);
                // Todo modulo aparece en el diagrama de contexto: si no, no se ve donde encaja.
                foreach (var m in f.Modulos)
                    Assert.IsTrue(f.Contexto.Flujos.Any(x => x.Desde == m.Id || x.Hasta == m.Id), $"{id}: el módulo {m.Id} no está en el diagrama");
            }
        }

        [Test]
        public void Todo_minijuego_del_nivel_dice_a_que_modulo_pertenece() {
            foreach (var id in Niveles) {
                var n = Nivel(id);
                var mjs = MinijuegosDe(n);
                Assert.IsNotEmpty(mjs, id);
                foreach (var mj in mjs) {
                    var def = Minijuego(mj);
                    Assert.IsNotNull(n.Proyecto.Modulo(def.Modulo), $"{id}/{mj}: el módulo '{def.Modulo}' no existe en la ficha");
                    Assert.IsNotEmpty(def.EnElProyecto, $"{id}/{mj}: sin 'enElProyecto'");
                }
            }
        }

        [Test]
        public void Las_practicas_hablan_del_proyecto_del_nivel() {
            // Antes, las prácticas de N0 eran de «concursantes» y un «jurado»: otro sistema distinto al del encargo.
            foreach (var mj in new[] { "MJ-OF-N0-DIAGRAMA", "MJ-OF-N0-BACKLOG" }) {
                var t = File.ReadAllText(Path.Combine(Raiz(), "minijuegos", mj + ".json")).ToLowerInvariant();
                StringAssert.DoesNotContain("concursante", t, mj);
                StringAssert.DoesNotContain("jurado", t, mj);
                StringAssert.Contains("visita", t, mj);
            }
            StringAssert.DoesNotContain("sucursal", File.ReadAllText(Path.Combine(Raiz(), "minijuegos", "MJ-F1-08.json")).ToLowerInvariant());
        }

        [Test]
        public void Las_preguntas_tienen_una_sola_respuesta_y_son_siempre_las_mismas() {
            foreach (var id in Niveles) {
                var f = Nivel(id).Proyecto;
                var a = PreguntasDelProyecto.Para(f, id);
                var b = PreguntasDelProyecto.Para(f, id);
                Assert.AreEqual(3, a.Count, id);
                for (var i = 0; i < a.Count; i++) {
                    var p = a[i];
                    Assert.AreEqual(PreguntasDelProyecto.Opciones, p.Opciones.Count, $"{id}/{p.Id}");
                    Assert.AreEqual(p.Opciones.Count, p.Opciones.Distinct().Count(), $"{id}/{p.Id}: opciones repetidas");
                    Assert.That(p.Correcta, Is.InRange(0, p.Opciones.Count - 1));
                    Assert.IsNotEmpty(p.Explicacion);
                    CollectionAssert.AreEqual(p.Opciones, b[i].Opciones, "no es determinista");
                    Assert.AreEqual(p.Correcta, b[i].Correcta);
                }
                Assert.AreEqual(f.Cliente.QueLeDuele, a.Single(p => p.Id == "cliente").Opciones[a.Single(p => p.Id == "cliente").Correcta]);
            }
        }

        [Test]
        public void El_avance_por_modulo_parte_del_inicial_llega_al_cien_y_nunca_baja() {
            var f = Nivel("nivel-01").Proyecto;
            const double alcance = 34;
            var inicio = AvanceDeModulos.Calcular(f, 0, alcance);
            for (var i = 0; i < f.Modulos.Count; i++) Assert.AreEqual(f.Modulos[i].AvanceInicial, inicio[i].Value, 1e-9);
            CollectionAssert.AreEqual(Enumerable.Repeat(100.0, f.Modulos.Count), AvanceDeModulos.Calcular(f, alcance, alcance).Select(x => x.Value));
            CollectionAssert.AreEqual(Enumerable.Repeat(100.0, f.Modulos.Count), AvanceDeModulos.Calcular(f, alcance * 2, alcance).Select(x => x.Value));

            var anterior = inicio.Select(x => x.Value).ToList();
            for (double avance = 0; avance <= alcance; avance += 0.5) {
                var ahora = AvanceDeModulos.Calcular(f, avance, alcance).Select(x => x.Value).ToList();
                for (var i = 0; i < ahora.Count; i++) {
                    Assert.GreaterOrEqual(ahora[i], anterior[i] - 1e-9, $"el módulo {i} bajó con avance {avance}");
                    Assert.That(ahora[i], Is.InRange(0, 100));
                }
                anterior = ahora;
            }
        }

        [Test]
        public void El_modo_guiado_empieza_diciendo_donde_estamos_en_el_proyecto() {
            var def = Minijuego("MJ-OF-N1-DIAGRAMA");
            var primero = RecorridoGuiado.Para(def).First();
            Assert.AreEqual(AccionGuiada.Leer, primero.Accion);
            StringAssert.Contains(def.EnElProyecto, primero.Porque);
        }
    }
}
