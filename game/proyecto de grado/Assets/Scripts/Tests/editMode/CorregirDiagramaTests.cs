using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Corregir;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// El diagrama con cartas de solucion (2026-11-09): los puntos se ven, a cada uno se le juega una carta, y una carta
    /// en uso no se puede repetir. «Está bien así» no se gasta. Se evalua con las claves de siempre.
    /// </summary>
    public class CorregirDiagramaTests {
        private static string Carpeta() {
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            return Path.Combine(raiz, "minijuegos");
        }

        private static MinijuegoDef Cargar(string id) {
            return CatalogoMinijuegos.Parsear(File.ReadAllText(Path.Combine(Carpeta(), id + ".json")));
        }

        private static IEnumerable<MinijuegoDef> Diagramas() {
            return Directory.GetFiles(Carpeta(), "MJ-*.json").Select(f => CatalogoMinijuegos.Parsear(File.ReadAllText(f)))
                            .Where(d => d.Lienzo == "diagrama");
        }

        private static CorregirState Tablero(MinijuegoDef def) { return new CorregirState(def.Cartas.Select(c => c.Id)); }

        private static Jugada Jugar(CorregirState st, string punto, string carta) {
            st.PuntoElegido = punto;
            string devuelta;
            return st.Jugar(carta, 1, out devuelta);
        }

        private static PuntoDelDiagrama Real(MinijuegoDef def, int i = 0) { return PuntosDelDiagrama.De(def).Where(p => p.EsReal).ElementAt(i); }
        private static PuntoDelDiagrama Senuelo(MinijuegoDef def, int i = 0) { return PuntosDelDiagrama.De(def).Where(p => !p.EsReal).ElementAt(i); }
        private static string Distractor(MinijuegoDef def) {
            var arreglan = new HashSet<string>(def.Zonas.SelectMany(z => z.Soluciones));
            return def.Cartas.First(c => !arreglan.Contains(c.Id)).Id;
        }

        [Test]
        public void Todos_los_diagramas_se_juegan_con_cartas_y_se_pueden_ganar() {
            var n = 0;
            foreach (var def in Diagramas()) {
                n++;
                Assert.IsTrue(PuntosDelDiagrama.Aplica(def), def.Id);
                var ganadora = PuntosDelDiagrama.SolucionGanadora(def);
                Assert.IsNotNull(ganadora, def.Id);
                var st = Tablero(def);
                foreach (var kv in ganadora) Assert.AreEqual(Jugada.Puesta, Jugar(st, kv.Key, kv.Value), def.Id);
                Assert.AreEqual(ResultadosDeMinijuego.Todos, CorregirEvaluador.Evaluar(def, st).Resultado, def.Id);
            }
            Assert.AreEqual(16, n, "los 16 retos de diagrama");
        }

        [Test]
        public void Los_puntos_mezclan_zonas_y_senuelos_en_un_orden_estable() {
            foreach (var def in Diagramas()) {
                var a = PuntosDelDiagrama.De(def).Select(p => p.Id).ToList();
                var b = PuntosDelDiagrama.De(def).Select(p => p.Id).ToList();
                CollectionAssert.AreEqual(a, b, def.Id);
                Assert.AreEqual(def.Zonas.Count + def.Senuelos.Count, a.Count, def.Id);
                CollectionAssert.AreEqual(Enumerable.Range(1, a.Count), PuntosDelDiagrama.De(def).Select(p => p.Numero), def.Id);
            }
        }

        [Test]
        public void Una_carta_en_uso_no_se_puede_jugar_en_otro_punto() {
            var def = Cargar("MJ-F1-07");
            var st = Tablero(def);
            var p1 = Real(def, 0); var p2 = Real(def, 1);
            Assert.AreEqual(Jugada.Puesta, Jugar(st, p1.Id, "cache"));
            Assert.AreEqual(Jugada.EnUso, Jugar(st, p2.Id, "cache"));
            Assert.IsNull(st.CartaEn(p2.Id));
            Assert.AreEqual(p1.Id, st.PuntoDe("cache"));
        }

        [Test]
        public void Esta_bien_asi_no_se_gasta() {
            var def = Cargar("MJ-F1-07");
            var st = Tablero(def);
            Assert.AreEqual(Jugada.Puesta, Jugar(st, Senuelo(def, 0).Id, PuntosDelDiagrama.EstaBien));
            Assert.AreEqual(Jugada.Puesta, Jugar(st, Senuelo(def, 1).Id, PuntosDelDiagrama.EstaBien));
            Assert.IsFalse(st.EnUso(PuntosDelDiagrama.EstaBien));
        }

        [Test]
        public void Cambiar_la_carta_de_un_punto_devuelve_la_anterior_y_quitarla_la_libera() {
            var def = Cargar("MJ-F1-07");
            var st = Tablero(def);
            var p = Real(def);
            Jugar(st, p.Id, "cache");
            st.PuntoElegido = p.Id;
            string devuelta;
            Assert.AreEqual(Jugada.Puesta, st.Jugar("estrategia", 2, out devuelta));
            Assert.AreEqual("cache", devuelta);
            Assert.IsFalse(st.EnUso("cache"));
            Assert.AreEqual("estrategia", st.Quitar(p.Id));
            Assert.IsFalse(st.EnUso("estrategia"));
            Assert.AreEqual(Jugada.Puesta, Jugar(st, Real(def, 1).Id, "estrategia"));
        }

        [Test]
        public void Sin_punto_elegido_o_con_una_carta_quemada_no_se_juega() {
            var def = Cargar("MJ-F1-07");
            var st = Tablero(def);
            string devuelta;
            Assert.AreEqual(Jugada.SinPunto, st.Jugar("cache", 0, out devuelta));
            Assert.IsTrue(st.Descartar("cache"));
            Assert.AreEqual(Jugada.NoDisponible, Jugar(st, Real(def).Id, "cache"));
            Assert.AreEqual(Jugada.NoDisponible, Jugar(st, Real(def).Id, "no-existe"));
        }

        [Test]
        public void Nada_jugado_es_omitido_y_arreglar_solo_una_parte_es_parcial() {
            var def = Cargar("MJ-F1-07");
            var st = Tablero(def);
            Assert.AreEqual(ResultadosDeMinijuego.Omitido, CorregirEvaluador.Evaluar(def, st).Resultado);
            var p = Real(def);
            Jugar(st, p.Id, p.Zona.Soluciones[0]);
            var r = CorregirEvaluador.Evaluar(def, st);
            Assert.AreEqual(ResultadosDeMinijuego.Parcial, r.Resultado);
            Assert.AreEqual(p.Zona.Id, r.Traza.Single().ZonaAcertada);
            Assert.AreEqual(def.Zonas.Count + def.Senuelos.Count, r.Detalle.Count, "una linea por punto");
        }

        [Test]
        public void Dar_por_bueno_un_problema_no_gana() {
            var def = Cargar("MJ-F1-07");
            var st = Tablero(def);
            foreach (var p in PuntosDelDiagrama.De(def)) Jugar(st, p.Id, PuntosDelDiagrama.EstaBien);
            var r = CorregirEvaluador.Evaluar(def, st);
            Assert.AreEqual(ResultadosDeMinijuego.Parcial, r.Resultado);
            Assert.IsTrue(r.Detalle.Any(d => d.Contains("lo diste por bueno")));
        }

        [Test]
        public void Cambiar_lo_que_estaba_bien_cuenta_en_contra_segun_la_tolerancia() {
            var def = Cargar("MJ-F1-07");   // tolerancia 1, dos señuelos
            var ganadora = PuntosDelDiagrama.SolucionGanadora(def);
            var st = Tablero(def);
            foreach (var kv in ganadora) Jugar(st, kv.Key, kv.Value);
            var libres = def.Cartas.Select(c => c.Id).Where(c => !st.EnUso(c)).ToList();
            Jugar(st, Senuelo(def, 0).Id, libres[0]);
            var r = CorregirEvaluador.Evaluar(def, st);
            Assert.AreEqual(ResultadosDeMinijuego.Parcial, r.Resultado, "uno dentro de la tolerancia: nunca «todos»");
            Assert.IsTrue(r.Detalle.Any(d => d.Contains("a algo que estaba bien")));
            Jugar(st, Senuelo(def, 1).Id, libres[1]);
            Assert.AreEqual(ResultadosDeMinijuego.FalsoPositivo, CorregirEvaluador.Evaluar(def, st).Resultado);
        }

        [Test]
        public void Un_diagrama_sin_defectos_se_gana_dandolo_todo_por_bueno() {
            var def = Cargar("MJ-F0-02");
            Assert.IsTrue(def.SinDefectos);
            var st = Tablero(def);
            var puntos = PuntosDelDiagrama.De(def);
            Jugar(st, puntos[0].Id, PuntosDelDiagrama.EstaBien);
            Assert.AreEqual(ResultadosDeMinijuego.Parcial, CorregirEvaluador.Evaluar(def, st).Resultado, "sin revisarlo entero");
            foreach (var p in puntos) Jugar(st, p.Id, PuntosDelDiagrama.EstaBien);
            Assert.AreEqual(ResultadosDeMinijuego.Todos, CorregirEvaluador.Evaluar(def, st).Resultado);
            Jugar(st, puntos[1].Id, def.Cartas[0].Id);
            Assert.AreEqual(ResultadosDeMinijuego.FalsoPositivo, CorregirEvaluador.Evaluar(def, st).Resultado);
        }

        [Test]
        public void Una_carta_equivocada_explica_por_que_no_sirve_y_cual_si() {
            var def = Cargar("MJ-F1-07");
            var st = Tablero(def);
            var p = Real(def);
            var mala = Distractor(def);
            Jugar(st, p.Id, mala);
            var linea = CorregirEvaluador.Evaluar(def, st).Detalle.First(d => d.StartsWith("Punto #" + p.Numero + " "));
            StringAssert.Contains(PuntosDelDiagrama.NombreDeCarta(def, mala), linea);
            StringAssert.Contains(PuntosDelDiagrama.Carta(def, mala).PorQueNo, linea);
            StringAssert.Contains(PuntosDelDiagrama.NombreDeCarta(def, p.Zona.Soluciones[0]), linea);
        }

        // ---------------------------------------------------------------- la revision de cada punto (el globo del cierre)

        [Test]
        public void La_revision_de_una_zona_acertada_lleva_su_defecto_y_una_sola_carta() {
            var def = Cargar("MJ-F1-07");
            var p = Real(def);
            var r = RevisionDePunto.Para(def, p, p.Zona.Soluciones[0]);
            Assert.IsTrue(r.Acierta);
            Assert.AreEqual(p.Zona.Soluciones[0], r.CartaCorrecta);
            Assert.IsNull(r.PorQueNo);
            StringAssert.DoesNotContain("Lo arreglaba", r.ExplicacionCompleta);
            Assert.IsNotEmpty(r.Titular);
        }

        [Test]
        public void La_revision_de_una_zona_fallada_dice_que_tocaba_y_por_que_no_servia_lo_puesto() {
            var def = Cargar("MJ-F1-07");
            var p = Real(def);
            var mala = Distractor(def);
            var r = RevisionDePunto.Para(def, p, mala);
            Assert.IsFalse(r.Acierta);
            Assert.AreEqual(PuntosDelDiagrama.Carta(def, mala).PorQueNo, r.PorQueNo);
            StringAssert.Contains("Lo arreglaba", r.ExplicacionCompleta);
            StringAssert.Contains(PuntosDelDiagrama.NombreDeCarta(def, p.Zona.Soluciones[0]), r.ExplicacionCompleta);
        }

        [Test]
        public void La_revision_de_un_senuelo_dice_que_estaba_bien_y_cambiarlo_cuenta_en_contra() {
            var def = Cargar("MJ-F1-07");
            var s = Senuelo(def);
            var bien = RevisionDePunto.Para(def, s, PuntosDelDiagrama.EstaBien);
            Assert.IsTrue(bien.Acierta);
            Assert.AreEqual("Estaba bien", bien.Titular);
            Assert.AreEqual(PuntosDelDiagrama.EstaBien, bien.CartaCorrecta);
            var mal = RevisionDePunto.Para(def, s, Distractor(def));
            Assert.IsFalse(mal.Acierta);
            StringAssert.Contains("también cuenta en contra", mal.ExplicacionCompleta);
        }

        [Test]
        public void Un_punto_sin_decidir_nunca_acierta_en_una_zona() {
            var def = Cargar("MJ-F1-07");
            var r = RevisionDePunto.Para(def, Real(def), null);
            Assert.IsFalse(r.Acierta);
            Assert.IsNull(r.CartaJugada);
            Assert.IsNull(r.PorQueNo);
        }

        [Test]
        public void Todos_los_puntos_de_todos_los_diagramas_tienen_titular_y_una_definicion_corta() {
            foreach (var def in Diagramas())
                foreach (var p in PuntosDelDiagrama.De(def)) {
                    var r = RevisionDePunto.Para(def, p, null);
                    Assert.IsNotEmpty(r.Titular, def.Id + "/" + p.Id);
                    Assert.IsNotEmpty(r.Definicion, def.Id + "/" + p.Id);
                    Assert.LessOrEqual(r.Definicion.Length, RevisionDePunto.MaximoDeDefinicion + 1, def.Id + "/" + p.Id);
                    Assert.IsNotEmpty(r.ExplicacionCompleta, def.Id + "/" + p.Id);
                }
        }

        [Test]
        public void El_validador_rechaza_un_diagrama_imposible_o_sin_distractores() {
            var def = Cargar("MJ-F1-07");
            def.Zonas[1].Soluciones = def.Zonas[0].Soluciones.ToList();   // dos zonas que solo arregla la misma carta
            Assert.IsTrue(CatalogoMinijuegos.Validar(def).Any(e => e.Contains("imposible")));

            var otro = Cargar("MJ-F0-01");
            otro.Cartas = otro.Cartas.Where(c => otro.Zonas.Any(z => z.Soluciones.Contains(c.Id))).ToList();
            Assert.IsTrue(CatalogoMinijuegos.Validar(otro).Any(e => e.Contains("descartar")));

            var sinCartas = Cargar("MJ-F0-01");
            sinCartas.Cartas.Clear();
            Assert.IsTrue(CatalogoMinijuegos.Validar(sinCartas).Any(e => e.Contains("cartas")));
        }
    }
}
