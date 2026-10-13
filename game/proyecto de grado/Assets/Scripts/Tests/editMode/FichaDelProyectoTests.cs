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

        // ---------------------------------------------------------------- el producto y los papeles del cliente

        [Test]
        public void Cada_modulo_trae_la_pantalla_con_que_se_dibuja_y_sus_datos_de_ejemplo() {
            var pantallas = new[] { "formulario", "lista", "aviso", "lector", "calendario", "grafica" };
            foreach (var id in Niveles)
                foreach (var m in Nivel(id).Proyecto.Modulos) {
                    CollectionAssert.Contains(pantallas, m.Pantalla, $"{id}/{m.Id}: pantalla desconocida");
                    Assert.GreaterOrEqual(m.Datos.Count, 2, $"{id}/{m.Id}: sin datos de ejemplo, el MVP se veria vacio");
                }
        }

        [Test]
        public void En_cada_nivel_hay_al_menos_un_modulo_que_se_puede_usar_en_la_maqueta() {
            foreach (var id in Niveles)
                Assert.IsTrue(Nivel(id).Proyecto.Modulos.Any(m => !string.IsNullOrEmpty(m.Accion)), id + ": ningun modulo tiene accion");
        }

        [Test]
        public void La_etapa_de_un_modulo_nunca_retrocede_cuando_el_proyecto_avanza() {
            foreach (var id in Niveles) {
                var nivel = Nivel(id);
                var f = nivel.Proyecto;
                var antes = MaquetaDelProducto.Calcular(f, 0, 100, null);
                // Sin avance, cada parte esta donde la dejaron (en el nivel 1 se hereda trabajo ya empezado).
                foreach (var m in antes)
                    Assert.AreEqual(EtapasDelProducto.De(m.Modulo.AvanceInicial, false), m.Etapa, $"{id}/{m.Modulo.Id}: etapa de partida");
                for (var avance = 5; avance <= 100; avance += 5) {
                    var ahora = MaquetaDelProducto.Calcular(f, avance, 100, null);
                    for (var i = 0; i < ahora.Count; i++)
                        Assert.GreaterOrEqual(EtapasDelProducto.Orden(ahora[i].Etapa), EtapasDelProducto.Orden(antes[i].Etapa),
                                              $"{id}/{ahora[i].Modulo.Id}: retrocedio al pasar a {avance}");
                    antes = ahora;
                }
                Assert.IsTrue(antes.All(m => m.Etapa == EtapasDelProducto.Mvp), id + ": con todo hecho, todo es un MVP");
                Assert.AreEqual(EtapasDelProducto.Mvp, MaquetaDelProducto.EtapaGeneral(antes));
            }
        }

        [Test]
        public void Un_modulo_desplegado_esta_en_produccion_y_lo_que_sube_de_etapa_se_anuncia() {
            var f = Nivel("nivel-00").Proyecto;
            var primero = f.Modulos[0].Id;
            var conUno = MaquetaDelProducto.Calcular(f, 100, 100, new[] { primero });
            Assert.AreEqual(EtapasDelProducto.Desplegado, conUno.First(m => m.Modulo.Id == primero).Etapa);
            Assert.AreNotEqual(EtapasDelProducto.Desplegado, MaquetaDelProducto.EtapaGeneral(conUno), "falta desplegar el resto");
            Assert.AreEqual(EtapasDelProducto.Desplegado, MaquetaDelProducto.EtapaGeneral(MaquetaDelProducto.Calcular(f, 100, 100, f.Modulos.Select(m => m.Id))));

            Assert.IsEmpty(MaquetaDelProducto.QueSubieron(f, 40, 40, 100, null));
            Assert.IsNotEmpty(MaquetaDelProducto.QueSubieron(f, 0, 100, 100, null));
        }

        [Test]
        public void Cada_usuario_del_cliente_dice_algo_distinto_segun_la_etapa_de_su_modulo() {
            foreach (var id in Niveles) {
                var f = Nivel(id).Proyecto;
                var boceto = VocesDelCliente.Para(f, MaquetaDelProducto.Calcular(f, 0, 100, null), 50);
                var mvp = VocesDelCliente.Para(f, MaquetaDelProducto.Calcular(f, 100, 100, null), 50);
                Assert.AreEqual(f.Usuarios.Count, boceto.Count, id + ": un usuario habla de un modulo que no existe");
                for (var i = 0; i < boceto.Count; i++) {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(boceto[i].Frase));
                    Assert.AreNotEqual(boceto[i].Frase, mvp[i].Frase, $"{id}/{boceto[i].Quien}: dice lo mismo con un boceto que con un MVP");
                }
            }
        }

        [Test]
        public void Los_papeles_del_cliente_llegan_dentro_del_nivel_y_hablan_de_modulos_que_existen() {
            foreach (var id in Niveles) {
                var nivel = Nivel(id);
                var f = nivel.Proyecto;
                Assert.GreaterOrEqual(f.Papeles.Count, 5, id + ": pocos papeles del cliente");
                CollectionAssert.AllItemsAreUnique(f.Papeles.Select(p => p.Id).ToList(), id);
                Assert.IsTrue(f.Papeles.Any(p => p.Dia == 1), id + ": el primer dia no llega ningun papel");
                foreach (var p in f.Papeles) {
                    Assert.That(p.Dia, Is.InRange(1, nivel.DiasTotales), $"{id}/{p.Id}: llega fuera del nivel");
                    CollectionAssert.Contains(TiposDePapel.Todos, p.Tipo, $"{id}/{p.Id}");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(p.Titulo), $"{id}/{p.Id}: sin titulo");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(p.QueTeDice), $"{id}/{p.Id}: no dice que ensena");
                    Assert.That(p.Lineas.Count, Is.InRange(2, 5), $"{id}/{p.Id}: renglones");
                    // Lo que cabe en el papel dibujado (DibujosDelProducto.Papel).
                    foreach (var l in p.Lineas) Assert.LessOrEqual(l.Length, 62, $"{id}/{p.Id}: «{l}» no cabe en el papel");
                    if (!string.IsNullOrEmpty(p.Modulo)) Assert.IsTrue(f.Modulos.Any(m => m.Id == p.Modulo), $"{id}/{p.Id}: modulo «{p.Modulo}»");
                }
                Assert.AreEqual(f.Papeles.Count(p => p.Dia <= 2), f.PapelesHasta(2).Count);
            }
        }
    }
}
