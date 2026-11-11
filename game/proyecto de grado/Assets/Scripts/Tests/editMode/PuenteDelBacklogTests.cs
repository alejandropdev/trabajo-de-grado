using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Puente;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// El puente del backlog (2026-11-10): la luz sube por cada pilar y se detiene en el primer fallo; un pilar bien
    /// se bloquea; un pilar mal pierde una vida; sin vidas, se pierde el puente.
    /// </summary>
    public class PuenteDelBacklogTests {
        private static HistoriaDePilar H(string id, params string[] deps) {
            return new HistoriaDePilar { Id = id, Titulo = "Historia " + id, DependeDe = deps.ToList() };
        }

        /// <summary>Dos pilares: a ← b ← c (cadena) y x, y ← z (z necesita las dos) ← w.</summary>
        private static MinijuegoDef Def(int vidas = 2) {
            var def = new MinijuegoDef {
                Id = "MJ-T-PUENTE", Verbo = Verbos.Ordenar, Lienzo = "puente", ObjetivoAprendizaje = "OA-SCRUM-01",
                Puente = new PuenteCfg {
                    Vidas = vidas,
                    Pilares = {
                        new PilarDef { Id = "uno", Nombre = "Uno", Historias = { H("a"), H("b", "a"), H("c", "b") } },
                        new PilarDef { Id = "dos", Nombre = "Dos", Historias = { H("x"), H("y"), H("z", "x", "y"), H("w", "z") } }
                    }
                }
            };
            foreach (var clave in new[] { "todos", "parcial", "falsoPositivo", "omitido" })
                def.Consecuencias[clave] = new Consecuencia { Rubrica = new Rubrica { Veredicto = "correcta", Oa = "OA-SCRUM-01", Razon = clave } };
            return def;
        }

        private static void Resolver(EstadoDelPuente e, int pilar) {
            var solucion = PuenteEvaluador.Solucion(e.Cfg.Pilares[pilar]);
            for (var k = 0; k < solucion.Count; k++) e.Mover(pilar, solucion[k], k);
        }

        // ------------------------------------------------------------ la luz

        [Test]
        public void Una_historia_esta_bien_si_lo_que_necesita_esta_mas_abajo() {
            var p = Def().Puente.Pilares[0];
            Assert.IsTrue(PuenteEvaluador.RevisarPilar(p, new[] { "a", "b", "c" }).Correcto);
            Assert.IsFalse(PuenteEvaluador.RevisarPilar(p, new[] { "b", "a", "c" }).Correcto);
        }

        [Test]
        public void La_luz_se_detiene_en_el_primer_fallo_y_lo_de_arriba_queda_sin_revisar_aunque_este_bien() {
            var p = Def().Puente.Pilares[0];
            var r = PuenteEvaluador.RevisarPilar(p, new[] { "a", "c", "b" });   // c necesita b, que esta encima
            CollectionAssert.AreEqual(new[] { EstadoDeBloque.Bien, EstadoDeBloque.Mal, EstadoDeBloque.SinRevisar }, r.Estados);
            Assert.AreEqual(1, r.PrimerFallo);
            Assert.AreEqual("c", r.HistoriaQueFalla);
            Assert.AreEqual("b", r.LaQueFalta);
            StringAssert.Contains("necesita", r.Motivo);
        }

        [Test]
        public void Una_historia_sin_dependencias_puede_ir_en_cualquier_sitio_y_el_orden_no_unico_vale() {
            var p = Def().Puente.Pilares[1];
            Assert.IsTrue(PuenteEvaluador.RevisarPilar(p, new[] { "x", "y", "z", "w" }).Correcto);
            Assert.IsTrue(PuenteEvaluador.RevisarPilar(p, new[] { "y", "x", "z", "w" }).Correcto);
            Assert.IsFalse(PuenteEvaluador.RevisarPilar(p, new[] { "x", "z", "y", "w" }).Correcto);
        }

        [Test]
        public void El_tablero_no_empieza_resuelto_ni_cambia_entre_partidas() {
            foreach (var p in Def().Puente.Pilares)
                for (var semilla = 0; semilla < 20; semilla++) {
                    var o = PuenteEvaluador.OrdenInicial(p, semilla);
                    CollectionAssert.AreEquivalent(p.Historias.Select(h => h.Id), o);
                    CollectionAssert.AreEqual(o, PuenteEvaluador.OrdenInicial(p, semilla));
                    Assert.IsFalse(PuenteEvaluador.RevisarPilar(p, o).Correcto, p.Id + "/" + semilla);
                }
        }

        // ------------------------------------------------------------ vidas y pilares hechos

        [Test]
        public void Un_pilar_bien_puesto_se_da_por_hecho_se_bloquea_y_no_se_vuelve_a_revisar() {
            var e = new EstadoDelPuente(Def().Puente);
            Resolver(e, 0);
            var ronda = e.Verificar();
            Assert.IsTrue(e.Hecho[0]);
            Assert.IsFalse(e.SePuedeMover(0), "un pilar hecho queda fijo");
            Assert.IsFalse(e.Mover(0, "a", 2));
            Assert.AreEqual(2, ronda.Pilares.Count, "la primera vez se revisan los dos");
            Assert.AreEqual(1, e.Verificar().Pilares.Count, "despues, solo el que falta");
        }

        [Test]
        public void Un_error_cuesta_una_vida_y_el_pilar_sigue_editable() {
            var e = new EstadoDelPuente(Def().Puente);
            e.Verificar();
            CollectionAssert.AreEqual(new[] { 1, 1 }, e.Vidas);
            Assert.IsFalse(e.Perdido);
            Assert.IsTrue(e.SePuedeMover(0) && e.SePuedeMover(1));
            Assert.AreEqual(2, e.VidasPerdidas);
        }

        [Test]
        public void Dos_errores_en_el_mismo_pilar_pierden_el_puente() {
            var e = new EstadoDelPuente(Def().Puente);
            Resolver(e, 0);
            e.Verificar();
            var segunda = e.Verificar();
            Assert.IsTrue(segunda.SePerdio);
            CollectionAssert.AreEqual(new[] { 1 }, segunda.Caidos);
            Assert.IsTrue(e.Perdido);
            Assert.IsTrue(e.Hecho[0], "el pilar que salio bien se queda hecho");
            Assert.IsFalse(e.Mover(1, "x", 0), "con el puente perdido no se mueve nada");
            Assert.AreEqual(0, e.Verificar().Pilares.Count);
        }

        [Test]
        public void Las_vidas_se_leen_del_contenido() {
            var e = new EstadoDelPuente(Def(vidas: 3).Puente);
            e.Verificar(); e.Verificar();
            Assert.IsFalse(e.Perdido);
            e.Verificar();
            Assert.IsTrue(e.Perdido);
        }

        // ------------------------------------------------------------ resultado

        [Test]
        public void Los_cuatro_resultados() {
            var def = Def();

            var perfecto = new EstadoDelPuente(def.Puente);
            Resolver(perfecto, 0); Resolver(perfecto, 1); perfecto.Verificar();
            Assert.AreEqual(ResultadosDeMinijuego.Todos, PuenteEvaluador.Evaluar(def, perfecto).Resultado);

            var conTropiezo = new EstadoDelPuente(def.Puente);
            Resolver(conTropiezo, 0); conTropiezo.Verificar();   // el pilar 2 pierde una vida
            Resolver(conTropiezo, 1); conTropiezo.Verificar();
            Assert.IsTrue(conTropiezo.Completo);
            Assert.AreEqual(ResultadosDeMinijuego.Parcial, PuenteEvaluador.Evaluar(def, conTropiezo).Resultado);

            var aMedias = new EstadoDelPuente(def.Puente);
            Resolver(aMedias, 0); aMedias.Verificar();   // se acaba el tiempo con un pilar hecho
            Assert.AreEqual(ResultadosDeMinijuego.Parcial, PuenteEvaluador.Evaluar(def, aMedias).Resultado);

            var caido = new EstadoDelPuente(def.Puente);
            caido.Verificar(); caido.Verificar();
            Assert.AreEqual(ResultadosDeMinijuego.FalsoPositivo, PuenteEvaluador.Evaluar(def, caido).Resultado);

            Assert.AreEqual(ResultadosDeMinijuego.Omitido, PuenteEvaluador.Evaluar(def, new EstadoDelPuente(def.Puente)).Resultado);
        }

        [Test]
        public void El_detalle_dice_por_que_se_cayo_el_pilar() {
            var def = Def();
            var e = new EstadoDelPuente(def.Puente);
            Resolver(e, 0);
            e.Verificar(); e.Verificar();
            var detalle = PuenteEvaluador.Evaluar(def, e).Detalle;
            Assert.IsTrue(detalle.Any(l => l.Contains("«Uno»") && l.Contains("bien a la primera")));
            Assert.IsTrue(detalle.Any(l => l.Contains("«Dos»") && l.Contains("se cayó") && l.Contains("necesita")));
        }

        // ------------------------------------------------------------ el recorrido guiado

        [Test]
        public void Seguir_a_Marisol_en_un_puente_da_todos_y_termina_entregando() {
            var def = Def();
            var pasos = RecorridoGuiado.Para(def);
            Assert.AreEqual(AccionGuiada.Entregar, pasos.Last().Accion);
            Assert.AreEqual(7, pasos.Count(p => p.Accion == AccionGuiada.Mover), "una por historia");
            Assert.AreEqual(ResultadosDeMinijuego.Todos, RecorridoGuiado.Aplicar(def, pasos).Resultado);
        }

        // ------------------------------------------------------------ el validador

        [Test]
        public void El_validador_acepta_el_puente_bueno() {
            CollectionAssert.IsEmpty(CatalogoMinijuegos.Validar(Def()));
        }

        [Test]
        public void El_validador_rechaza_dependencias_de_otro_pilar_ciclos_y_pilares_planos() {
            var cruzada = Def();
            cruzada.Puente.Pilares[0].Historias[1].DependeDe.Add("x");
            Assert.IsTrue(CatalogoMinijuegos.Validar(cruzada).Any(m => m.Contains("no esta en su pilar")));

            var ciclo = Def();
            ciclo.Puente.Pilares[0].Historias[0].DependeDe.Add("c");
            Assert.IsTrue(CatalogoMinijuegos.Validar(ciclo).Any(m => m.Contains("ciclo")));

            var plano = Def();
            foreach (var h in plano.Puente.Pilares[1].Historias) h.DependeDe.Clear();
            Assert.IsTrue(CatalogoMinijuegos.Validar(plano).Any(m => m.Contains("no ensena nada")));

            var repetida = Def();
            repetida.Puente.Pilares[1].Historias[0].Id = "a";
            Assert.IsTrue(CatalogoMinijuegos.Validar(repetida).Any(m => m.Contains("repetida")));

            var pocos = Def();
            pocos.Puente.Pilares.RemoveAt(1);
            Assert.IsTrue(CatalogoMinijuegos.Validar(pocos).Any(m => m.Contains("dos a cuatro pilares")));
        }
    }
}
