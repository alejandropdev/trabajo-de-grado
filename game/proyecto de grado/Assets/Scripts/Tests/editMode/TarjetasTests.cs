using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Eventos;
using Nexus.Core.Evaluacion;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using Nexus.Core.Tarjetas;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// La tarjeta de decision: lo que se ve al arrastrar es lo que pasa al soltar, el diferido no se enseña, y la
    /// traza queda con el Origen, el Oa y el veredicto de la pantalla que la tarjeta sustituye.
    /// </summary>
    public class TarjetasTests {
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

        private static GameSession Sesion() {
            var flags = new FlagStore(new Dictionary<string, double>(StringComparer.Ordinal), Catalogo().Flags);
            flags.Inicializar();
            return new GameSession(Catalogo(), "nivel-00", flags, 7);
        }

        private static TarjetaDef Tarjeta() {
            return new TarjetaDef {
                Id = "TJ-PRUEBA", Personaje = "Marta", Frase = "El cliente quiere un boton nuevo hoy.",
                Ayuda = "Aceptar sin medir el impacto mete deuda.", Momento = "dia", Orden = 1,
                Origen = "EV-PRUEBA", Titulo = "Cambio de ultima hora", Oa = "OA-ALCANCE-01",
                Opciones = new List<OpcionTarjeta> {
                    new OpcionTarjeta {
                        Id = "A", Etiqueta = "Aceptar", Veredicto = Veredictos.Incorrecta, Razon = "Sin medir el impacto.",
                        Efectos = new Dictionary<string, object> { { "DeudaTecnica", 12 } },
                        Diferidos = new List<EfectoDiferido> {
                            new EfectoDiferido { EnDias = 2, Efectos = new Dictionary<string, object> { { "DeudaTecnica", 8 } } }
                        }
                    },
                    new OpcionTarjeta {
                        Id = "B", Etiqueta = "Negociar", Veredicto = Veredictos.Correcta, Razon = "Se pacta para el siguiente sprint.",
                        Efectos = new Dictionary<string, object> { { "Cobertura", 5 } }
                    }
                }
            };
        }

        // ------------------------------------------------------------ resolver

        [Test]
        public void Lo_que_se_previsualiza_es_lo_que_pasa_al_resolver() {
            var s = Sesion();
            var tarjeta = Tarjeta();
            var previsto = ResolutorDeTarjeta.Previsualizar(s.W, tarjeta, "A");

            double antes, despues;
            s.W.TryGet("DeudaTecnica", out antes);
            s.ResolverTarjeta(tarjeta, "A");
            s.W.TryGet("DeudaTecnica", out despues);

            Assert.AreEqual(despues - antes, previsto["DeudaTecnica"], 1e-9);
        }

        [Test]
        public void Previsualizar_no_ensena_los_diferidos() {
            var s = Sesion();
            var previsto = ResolutorDeTarjeta.Previsualizar(s.W, Tarjeta(), "A");
            Assert.AreEqual(1, previsto.Count, "solo el efecto inmediato");
            Assert.AreEqual(12, previsto["DeudaTecnica"], 1e-9, "no suma los 8 del diferido");
        }

        [Test]
        public void Previsualizar_no_toca_el_estado() {
            var s = Sesion();
            var antes = s.W.ToString();
            ResolutorDeTarjeta.Previsualizar(s.W, Tarjeta(), "A");
            Assert.AreEqual(antes, s.W.ToString());
        }

        [Test]
        public void Resolver_escribe_en_la_traza_con_el_origen_el_oa_y_el_veredicto_de_la_tarjeta() {
            var s = Sesion();
            var previas = s.Traza.Entradas.Count;
            s.ResolverTarjeta(Tarjeta(), "B");

            Assert.AreEqual(previas + 1, s.Traza.Entradas.Count);
            var entrada = s.Traza.Entradas.Last();
            Assert.AreEqual("EV-PRUEBA", entrada.Origen);
            Assert.AreEqual("Cambio de ultima hora", entrada.Titulo);
            Assert.AreEqual("B", entrada.OpcionId);
            Assert.AreEqual("OA-ALCANCE-01", entrada.Oa);
            Assert.AreEqual(Veredictos.Correcta, entrada.Veredicto);
            Assert.AreEqual("Se pacta para el siguiente sprint.", entrada.Razon);
            Assert.AreNotEqual(entrada.EstadoAntes, entrada.EstadoDespues, "la traza guarda el antes y el despues");
        }

        [Test]
        public void Una_opcion_que_no_existe_no_se_resuelve() {
            var s = Sesion();
            Assert.Throws<InvalidOperationException>(() => s.ResolverTarjeta(Tarjeta(), "Z"));
            Assert.Throws<InvalidOperationException>(() => ResolutorDeTarjeta.Previsualizar(s.W, Tarjeta(), "Z"));
        }

        // ------------------------------------------------------------ validacion

        [Test]
        public void Una_tarjeta_bien_formada_no_da_errores() {
            Assert.IsEmpty(CatalogoDeTarjetas.Validar(new[] { Tarjeta() }));
        }

        [Test]
        public void Una_frase_larga_se_rechaza() {
            var t = Tarjeta();
            t.Frase = string.Join(" ", Enumerable.Repeat("palabra", LimitesDeTarjeta.PalabrasDeFrase + 1));
            Assert.That(CatalogoDeTarjetas.Validar(new[] { t }).Any(m => m.Contains("frase")));
        }

        [Test]
        public void Una_etiqueta_larga_se_rechaza() {
            var t = Tarjeta();
            t.Opciones[0].Etiqueta = "una etiqueta demasiado larga aqui";
            Assert.That(CatalogoDeTarjetas.Validar(new[] { t }).Any(m => m.Contains("etiqueta")));
        }

        [Test]
        public void Sin_opcion_correcta_se_rechaza() {
            var t = Tarjeta();
            t.Opciones[1].Veredicto = Veredictos.Aceptable;
            Assert.That(CatalogoDeTarjetas.Validar(new[] { t }).Any(m => m.Contains("correcta")));
        }

        [Test]
        public void Una_sola_opcion_no_es_un_dilema() {
            var t = Tarjeta();
            t.Opciones.RemoveAt(1);
            Assert.That(CatalogoDeTarjetas.Validar(new[] { t }).Any(m => m.Contains("opciones")));
        }

        [Test]
        public void Un_stock_que_no_existe_se_rechaza() {
            var t = Tarjeta();
            t.Opciones[0].Efectos["Inventado"] = 3;
            Assert.That(CatalogoDeTarjetas.Validar(new[] { t }).Any(m => m.Contains("Inventado")));
        }

        [Test]
        public void Una_pizarra_que_no_existe_se_rechaza() {
            var t = Tarjeta();
            t.Pizarra = "no-existe";
            Assert.That(CatalogoDeTarjetas.Validar(new[] { t }).Any(m => m.Contains("pizarra")));
        }

        [Test]
        public void Un_veredicto_invalido_se_rechaza() {
            var t = Tarjeta();
            t.Opciones[0].Veredicto = "regular";
            Assert.That(CatalogoDeTarjetas.Validar(new[] { t }).Any(m => m.Contains("veredicto")));
        }

        // ------------------------------------------------------------ ceremonias del contenido real

        private static IEnumerable<KeyValuePair<string, Nexus.Core.Metodologia.Ceremonia>> CeremoniasConOpciones() {
            foreach (var m in Catalogo().Metodologias.Values)
                foreach (var c in m.Ceremonias.Where(x => x.Opciones != null && x.Opciones.Count > 0))
                    yield return new KeyValuePair<string, Nexus.Core.Metodologia.Ceremonia>(m.Id, c);
        }

        [Test]
        public void Toda_ceremonia_jugable_cabe_en_una_tarjeta() {
            var vistas = 0;
            foreach (var par in CeremoniasConOpciones()) {
                var donde = par.Key + "/" + par.Value.Id;
                var hoy = new CeremoniaDeHoy {
                    Id = par.Value.Id, Nombre = par.Value.Nombre, Frase = par.Value.Frase, Ayuda = par.Value.Ayuda,
                    Opciones = par.Value.Opciones
                };
                var tarjeta = TarjetaDeCeremonia.De(hoy);
                Assert.IsNotNull(tarjeta, donde + ": falta 'frase' o alguna 'etiqueta'.");
                Assert.LessOrEqual(CatalogoDeTarjetas.Palabras(tarjeta.Frase), LimitesDeTarjeta.PalabrasDeFrase, donde + ": frase");
                Assert.LessOrEqual(CatalogoDeTarjetas.Palabras(tarjeta.Ayuda), LimitesDeTarjeta.PalabrasDeAyuda, donde + ": ayuda");
                Assert.LessOrEqual(tarjeta.Opciones.Count, LimitesDeTarjeta.MaximoDeOpciones, donde + ": opciones");
                foreach (var o in tarjeta.Opciones)
                    Assert.LessOrEqual(CatalogoDeTarjetas.Palabras(o.Etiqueta), LimitesDeTarjeta.PalabrasDeEtiqueta, donde + "/" + o.Id + ": etiqueta");
                vistas++;
            }
            Assert.AreEqual(8, vistas, "las 8 ceremonias jugables: 2 de Scrum, 2 de Kanban y 4 de Cascada");
        }

        [Test]
        public void La_tarjeta_de_una_ceremonia_previsualiza_lo_que_la_ceremonia_aplica() {
            var s = Sesion();
            var par = CeremoniasConOpciones().First(p => p.Value.Opciones.Any(o => o.Efectos != null && o.Efectos.Count > 0));
            var opcion = par.Value.Opciones.First(o => o.Efectos != null && o.Efectos.Count > 0);
            var tarjeta = TarjetaDeCeremonia.De(new CeremoniaDeHoy {
                Id = par.Value.Id, Nombre = par.Value.Nombre, Frase = par.Value.Frase, Ayuda = par.Value.Ayuda, Opciones = par.Value.Opciones
            });
            var previsto = ResolutorDeTarjeta.Previsualizar(s.W, tarjeta, opcion.Id);
            Assert.AreEqual(opcion.Efectos.Count, previsto.Keys.Count(k => opcion.Efectos.ContainsKey(k)), "toca los mismos stocks que la ceremonia");
        }

        // ------------------------------------------------------------ decisiones del tutorial

        [Test]
        public void Los_eventos_del_tutorial_son_tarjetas_que_respetan_los_limites() {
            var tutoriales = Catalogo().Eventos.Where(e => e.Id.StartsWith("EV-TUT", StringComparison.Ordinal)).ToList();
            Assert.AreEqual(2, tutoriales.Count);
            var s = Sesion();
            foreach (var ev in tutoriales) {
                var decision = new PendingDecision {
                    EventoId = ev.Id, Titulo = ev.Nombre, Frase = ev.Frase,
                    Opciones = ev.Opciones.Select(o => new OpcionPresentada {
                        Id = o.Id, Texto = o.Texto, Etiqueta = o.Etiqueta,
                        Previsualizacion = Nexus.Core.Servicios.EffectApplier.Previsualizar(s.W, o.EfectosInmediatos)
                    }).ToList()
                };
                var tarjeta = TarjetaDeDecision.De(decision);
                Assert.IsNotNull(tarjeta, ev.Id + ": falta 'frase' o alguna 'etiqueta'.");
                Assert.LessOrEqual(CatalogoDeTarjetas.Palabras(tarjeta.Frase), LimitesDeTarjeta.PalabrasDeFrase, ev.Id + ": frase");
                foreach (var o in tarjeta.Opciones)
                    Assert.LessOrEqual(CatalogoDeTarjetas.Palabras(o.Etiqueta), LimitesDeTarjeta.PalabrasDeEtiqueta, ev.Id + "/" + o.Id);
                // Lo que la tarjeta enseña es lo que el motor previsualizo: mismos stocks, mismos deltas.
                foreach (var o in ev.Opciones) {
                    var enseñado = ResolutorDeTarjeta.Previsualizar(s.W, tarjeta, o.Id);
                    foreach (var kv in Nexus.Core.Servicios.EffectApplier.Previsualizar(s.W, o.EfectosInmediatos))
                        Assert.AreEqual(kv.Value, enseñado[kv.Key], 1e-9, ev.Id + "/" + o.Id + "/" + kv.Key);
                }
            }
        }

        [Test]
        public void Una_decision_bloqueada_o_sin_frase_no_sale_como_tarjeta() {
            var opcion = new OpcionPresentada { Id = "a", Etiqueta = "Si" };
            var otra = new OpcionPresentada { Id = "b", Etiqueta = "No" };
            Assert.IsNull(TarjetaDeDecision.De(new PendingDecision { Titulo = "T", Opciones = { opcion, otra } }), "sin frase");
            otra.Bloqueada = true;
            Assert.IsNull(TarjetaDeDecision.De(new PendingDecision { Titulo = "T", Frase = "Una frase.", Opciones = { opcion, otra } }), "con una opcion bloqueada");
        }

        // ------------------------------------------------------------ retrospectiva y planificacion

        [Test]
        public void Toda_retrospectiva_con_acciones_cabe_en_una_tarjeta() {
            var vistas = 0;
            foreach (var m in Catalogo().Metodologias.Values)
                foreach (var c in m.Ceremonias.Where(x => x.Acciones != null && x.Acciones.Count > 0)) {
                    var donde = m.Id + "/" + c.Id;
                    var retro = new RetroRequest { CeremoniaId = c.Id, Frase = c.Frase, Acciones = c.Acciones };
                    var tarjeta = TarjetaDeProceso.DeRetro(retro, c.Nombre);
                    Assert.IsNotNull(tarjeta, donde + ": falta 'frase' o alguna 'etiqueta' en sus acciones.");
                    Assert.LessOrEqual(CatalogoDeTarjetas.Palabras(tarjeta.Frase), LimitesDeTarjeta.PalabrasDeFrase, donde + ": frase");
                    Assert.AreEqual(c.Acciones.Count, tarjeta.Opciones.Count, donde + ": no se pierde ninguna accion");
                    foreach (var o in tarjeta.Opciones)
                        Assert.LessOrEqual(CatalogoDeTarjetas.Palabras(o.Etiqueta), LimitesDeTarjeta.PalabrasDeEtiqueta, donde + "/" + o.Id);
                    Assert.IsTrue(c.Acciones.Select(a => a.Id).SequenceEqual(tarjeta.Opciones.Select(o => o.Id)), donde + ": mismos ids, mismo orden");
                    vistas++;
                }
            Assert.AreEqual(2, vistas, "la retro de Scrum y la revision del flujo de Kanban");
        }

        [Test]
        public void Una_retro_sin_etiquetas_sigue_la_vista_larga() {
            var retro = new RetroRequest {
                CeremoniaId = "retro",
                Acciones = { new Nexus.Core.Metodologia.AccionRetro { Id = "a", Texto = "Algo largo" },
                             new Nexus.Core.Metodologia.AccionRetro { Id = "b", Texto = "Otra cosa larga", Etiqueta = "Otra" } }
            };
            Assert.IsNull(TarjetaDeProceso.DeRetro(retro, "Retro"));
        }

        [Test]
        public void La_planificacion_ofrece_menos_lo_que_cabe_y_mas_y_cada_opcion_lleva_sus_puntos() {
            var plan = new PlanningRequest { Unidad = "sprint", CapacidadSugerida = 20.4, PuntosPendientes = 60 };
            var tarjeta = TarjetaDeProceso.DePlanificacion(plan, false);
            Assert.AreEqual(3, tarjeta.Opciones.Count);
            CollectionAssert.AreEqual(new[] { 14.0, 20.0, 26.0 }, tarjeta.Opciones.Select(TarjetaDeProceso.PuntosDe).ToArray());
            Assert.LessOrEqual(CatalogoDeTarjetas.Palabras(tarjeta.Frase), LimitesDeTarjeta.PalabrasDeFrase);
            Assert.LessOrEqual(CatalogoDeTarjetas.Palabras(tarjeta.Ayuda), LimitesDeTarjeta.PalabrasDeAyuda);
            foreach (var o in tarjeta.Opciones) Assert.LessOrEqual(CatalogoDeTarjetas.Palabras(o.Etiqueta), LimitesDeTarjeta.PalabrasDeEtiqueta);
            Assert.IsEmpty(tarjeta.Opciones.SelectMany(o => o.Efectos), "el sobrecompromiso no se previsualiza");
        }

        [Test]
        public void Una_capacidad_diminuta_no_da_compromisos_negativos() {
            var tarjeta = TarjetaDeProceso.DePlanificacion(new PlanningRequest { Unidad = "sprint", CapacidadSugerida = 0.2 }, true);
            Assert.That(tarjeta.Opciones.All(o => TarjetaDeProceso.PuntosDe(o) >= 0));
        }

        // ------------------------------------------------------------ prueba de admision

        [Test]
        public void Las_quince_preguntas_tienen_vista_corta_y_el_instrumento_original_sigue_intacto() {
            var prueba = Catalogo().Admision;
            Assert.AreEqual(15, prueba.Preguntas.Count);
            foreach (var p in prueba.Preguntas) {
                var donde = "pregunta " + p.Numero;
                Assert.IsNotEmpty(p.Frase, donde + ": falta la frase");
                Assert.LessOrEqual(CatalogoDeTarjetas.Palabras(p.Frase), LimitesDeTarjeta.PalabrasDeFrase, donde);
                Assert.Less(CatalogoDeTarjetas.Palabras(p.Frase), CatalogoDeTarjetas.Palabras(p.Enunciado) + 6, donde + ": la frase no es mas corta que el enunciado");
                foreach (var o in p.Opciones) {
                    Assert.IsNotEmpty(o.Etiqueta, donde + "/" + o.Id + ": falta la etiqueta");
                    Assert.LessOrEqual(CatalogoDeTarjetas.Palabras(o.Etiqueta), LimitesDeTarjeta.PalabrasDeEtiqueta, donde + "/" + o.Id);
                    Assert.IsNotEmpty(o.Texto, donde + "/" + o.Id + ": el texto original no puede desaparecer");
                }
                Assert.IsNotEmpty(p.Enunciado, donde + ": el enunciado original no puede desaparecer");
                Assert.IsTrue(p.Opciones.Any(o => o.Id == p.Correcta), donde + ": la correcta tiene que seguir siendo una opcion");
            }
        }

        // ------------------------------------------------------------ carga

        private const string Json =
            "{ \"version\": 1, \"tarjetas\": [" +
            "{ \"id\": \"TJ-1\", \"personaje\": \"Marta\", \"frase\": \"Una frase corta.\", \"momento\": \"dia\", \"orden\": 2," +
            "  \"origen\": \"EV-X\", \"titulo\": \"T\", \"oa\": \"OA-1\", \"soloMetodologias\": [\"scrum\"]," +
            "  \"opciones\": [" +
            "   { \"id\": \"A\", \"etiqueta\": \"Si\", \"veredicto\": \"correcta\", \"razon\": \"r\", \"efectos\": { \"DeudaTecnica\": -2 } }," +
            "   { \"id\": \"B\", \"etiqueta\": \"No\", \"veredicto\": \"incorrecta\", \"razon\": \"r\", \"efectos\": { \"DeudaTecnica\": 5 }," +
            "     \"diferidos\": [ { \"enDias\": 1, \"efectos\": { \"DeudaTecnica\": 3 } } ] } ] }," +
            "{ \"id\": \"TJ-0\", \"personaje\": \"Ivan\", \"frase\": \"Otra.\", \"momento\": \"dia\", \"orden\": 1," +
            "  \"origen\": \"EV-Y\", \"titulo\": \"T\", \"oa\": \"OA-1\"," +
            "  \"opciones\": [" +
            "   { \"id\": \"A\", \"etiqueta\": \"Si\", \"veredicto\": \"correcta\", \"razon\": \"r\" }," +
            "   { \"id\": \"B\", \"etiqueta\": \"No\", \"veredicto\": \"aceptable\", \"razon\": \"r\" } ] } ] }";

        [Test]
        public void El_json_se_lee_con_efectos_y_diferidos() {
            var catalogo = CatalogoDeTarjetas.Cargar(new CatalogoEnMemoria().Con("tarjetas/n0-dia.json", Json));
            Assert.AreEqual(2, catalogo.Tarjetas.Count);
            var t = catalogo.Buscar("TJ-1");
            Assert.AreEqual(-2, Convert.ToDouble(t.Opcion("A").Efectos["DeudaTecnica"]));
            Assert.AreEqual(1, t.Opcion("B").Diferidos.Count);
            Assert.AreEqual(1, t.Opcion("B").Diferidos[0].EnDias);
        }

        [Test]
        public void Las_tarjetas_de_un_momento_salen_en_orden_y_filtradas_por_metodologia() {
            var catalogo = CatalogoDeTarjetas.Cargar(new CatalogoEnMemoria().Con("tarjetas/n0-dia.json", Json));
            CollectionAssert.AreEqual(new[] { "TJ-0", "TJ-1" }, catalogo.De("dia", "scrum").Select(t => t.Id).ToArray());
            CollectionAssert.AreEqual(new[] { "TJ-0" }, catalogo.De("dia", "kanban").Select(t => t.Id).ToArray());
        }

        [Test]
        public void Sin_carpeta_de_tarjetas_el_catalogo_queda_vacio() {
            Assert.IsEmpty(CatalogoDeTarjetas.Cargar(new CatalogoEnMemoria()).Tarjetas);
        }

        [Test]
        public void Un_id_repetido_entre_archivos_no_carga() {
            var fuente = new CatalogoEnMemoria().Con("tarjetas/a.json", Json).Con("tarjetas/b.json", Json);
            Assert.Throws<SchemaException>(() => CatalogoDeTarjetas.Cargar(fuente));
        }

        [Test]
        public void Un_json_roto_no_carga_a_medias() {
            var fuente = new CatalogoEnMemoria().Con("tarjetas/a.json", "{ esto no es json");
            Assert.Throws<SchemaException>(() => CatalogoDeTarjetas.Cargar(fuente));
        }
    }
}
