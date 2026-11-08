using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using Nexus.Core.Tablero;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// El muro del backlog (2026-11-06): una tarjeta se arrastra a su puesto de una vez. Lo que de eso vive en Core:
    /// mover a un puesto solo toca su fila (su sprint o el product backlog), cuesta lo mismo llegue donde llegue, y
    /// lo empezado no se mueve.
    /// </summary>
    public class MuroDelBacklogTests {
        private static Catalogo _catalogo;

        private static Catalogo Catalogo() {
            if (_catalogo != null) return _catalogo;
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            return _catalogo = CatalogLoader.CargarTodo(new CatalogoDeArchivos(raiz));
        }

        private static GameSession Empezar(string metodologia) {
            var flags = new FlagStore(new Dictionary<string, double>(StringComparer.Ordinal), Catalogo().Flags);
            flags.Inicializar();
            var s = new GameSession(Catalogo(), "nivel-01", flags, 3);
            s.ElegirMetodologia(metodologia, Catalogo().Metodologias[metodologia].TextosDeRazones.Keys.First());
            s.RepartirCalidad(new Dictionary<string, int>());
            var arq = Catalogo().Niveles["nivel-01"].Fase1.Arquitecturas[0];
            s.ElegirArquitectura(arq.Id, Catalogo().Niveles["nivel-01"].Fase1.RazonesDe(arq)[0].Id);
            s.CerrarFase1();
            s.ComenzarDia();
            return s;
        }

        [Test]
        public void Arrastrar_la_ultima_al_primer_puesto_la_pone_primera_y_corre_las_demas() {
            var s = Empezar("kanban");
            var ultima = s.Tablero.Tarjetas.Where(c => !c.Empezada && !c.EsBug).OrderBy(c => c.Orden).Last();
            var fila = s.FilaDeOrden(ultima);
            var antes = fila.Select(c => c.Id).ToList();
            var ordenes = fila.Select(c => c.Orden).OrderBy(o => o).ToList();
            s.MoverTarjetaA(ultima.Id, 0);
            // Al avanzar el reloj el equipo puede empezar la primera: se mira el orden, no la fila de lo no empezado.
            var despues = fila.OrderBy(c => c.Orden).Select(c => c.Id).ToList();
            Assert.AreEqual(ultima.Id, despues[0]);
            CollectionAssert.AreEqual(antes.Take(antes.Count - 1), despues.Skip(1), "las demás conservan su orden entre ellas");
            CollectionAssert.AreEqual(ordenes, fila.Select(c => c.Orden).OrderBy(o => o), "se reparten los mismos valores de orden");
        }

        [Test]
        public void Mover_cuesta_lo_mismo_llegue_donde_llegue() {
            var s = Empezar("kanban");
            var fila = s.FilaDeOrden(s.Tablero.Tarjetas.First(c => !c.Empezada && !c.EsBug));
            Assume.That(fila.Count, Is.GreaterThanOrEqualTo(4));
            var minuto = s.MinutoDelDia;
            s.MoverTarjetaA(fila.Last().Id, 0);
            Assert.AreEqual(minuto + GameSession.MinutosDePriorizar, s.MinutoDelDia);
        }

        [Test]
        public void No_se_mueve_lo_empezado_ni_al_mismo_puesto() {
            var s = Empezar("kanban");
            var c = s.Tablero.Tarjetas.First(x => !x.Empezada && !x.EsBug);
            var fila = s.FilaDeOrden(c);
            Assert.IsNotNull(s.PorQueNoSePuedeMover(c.Id, fila.IndexOf(c)), "ya está en ese puesto");
            c.Trabajo = 0.5;
            Assert.IsNotNull(s.PorQueNoSePuedeMover(c.Id, 0), "empezada");
            Assert.Throws<InvalidOperationException>(() => s.MoverTarjetaA(c.Id, 0));
        }

        [Test]
        public void En_scrum_mover_dentro_del_backlog_no_toca_el_sprint() {
            var s = Empezar("scrum");
            if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
            var fuera = s.Tablero.Tarjetas.Where(c => !c.EsBug && !c.Empezada && c.Sprint != s.Tablero.SprintActual).OrderBy(c => c.Orden).ToList();
            Assume.That(fuera.Count, Is.GreaterThanOrEqualTo(2));
            var sprint = s.Tablero.Tarjetas.Where(c => c.Sprint == s.Tablero.SprintActual).ToDictionary(c => c.Id, c => c.Orden);
            s.MoverTarjetaA(fuera.Last().Id, 0);
            foreach (var kv in sprint) Assert.AreEqual(kv.Value, s.Tablero.Tarjeta(kv.Key).Orden, "el sprint no se entera");
            Assert.AreEqual(fuera.Last().Id, fuera.OrderBy(c => c.Orden).First().Id);
            Assert.AreNotEqual(s.Tablero.SprintActual, fuera.Last().Sprint, "sigue en el product backlog");
        }

        [Test]
        public void En_scrum_dar_una_tarjeta_del_backlog_con_todo_lleno_avisa_que_no_empezaria() {
            var s = Empezar("scrum");
            if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
            var t = s.Tablero;
            var fuera = t.Tarjetas.Where(c => !c.EsBug && !c.Empezada && c.Sprint != t.SprintActual).OrderBy(c => c.Orden).Last();
            Assert.IsNotNull(s.PorQueNoEmpezariaAlAsignar(fuera.Id), "la cola está llena: asignarla sola no la empieza");
            var limite = MotorDelTablero.EnCurso(t);
            s.AsignarSubiendoElLimite(fuera.Id, t.Miembros[0].Id);
            Assert.AreEqual(limite + 1, MotorDelTablero.EnCurso(t), "el límite sube uno");
            Assert.AreEqual(t.SprintActual, fuera.Sprint, "entra en el sprint");
            Assert.IsTrue(MotorDelTablero.EnOrdenDeTrabajo(t).Take(MotorDelTablero.EnCurso(t)).Contains(fuera), "y está entre lo que se trabaja ya");
            Assert.IsTrue(fuera.Empezada, "los minutos de asignar ya la ponen en marcha");
            Assert.IsNull(s.PorQueNoEmpezariaAlAsignar(fuera.Id));
        }

        [Test]
        public void Dar_la_primera_de_la_cola_con_hueco_no_avisa() {
            var s = Empezar("kanban");
            var activa = MotorDelTablero.EnOrdenDeTrabajo(s.Tablero).First();
            Assert.IsNull(s.PorQueNoEmpezariaAlAsignar(activa.Id));
        }

        [Test]
        public void Mover_en_una_lista_corre_los_de_en_medio_y_acota_el_puesto() {
            var l = new List<string> { "a", "b", "c", "d" };
            Assert.IsTrue(OrdenDeLista.Mover(l, 3, 1));
            CollectionAssert.AreEqual(new[] { "a", "d", "b", "c" }, l);
            Assert.IsTrue(OrdenDeLista.Mover(l, 0, 99));
            CollectionAssert.AreEqual(new[] { "d", "b", "c", "a" }, l);
            Assert.IsFalse(OrdenDeLista.Mover(l, 2, 2));
            Assert.IsFalse(OrdenDeLista.Mover(l, 7, 0));
        }

        [Test]
        public void Poner_mete_lo_nuevo_en_su_puesto_o_mueve_lo_que_ya_estaba() {
            var l = new List<string> { "a", "b" };
            Assert.IsTrue(OrdenDeLista.Poner(l, "x", 1));
            CollectionAssert.AreEqual(new[] { "a", "x", "b" }, l);
            Assert.IsTrue(OrdenDeLista.Poner(l, "a", 5));
            CollectionAssert.AreEqual(new[] { "x", "b", "a" }, l);
            Assert.IsTrue(OrdenDeLista.Poner(l, "z", -3));
            Assert.AreEqual("z", l[0]);
        }
    }
}
