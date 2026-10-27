using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Fase1;
using Nexus.Core.Narrativa;
using Nexus.Core.Servicios;
using Nexus.Core.Sesion;
using Nexus.Core.Tablero;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// Trabajar SOLO (ronda 8: «¿tiene sentido hacer ceremonias, hablarme a mí mismo 15 min o trabajar en pareja si no
    /// hay nadie?»). Sin hojas de vida el equipo es «Tú»: lo de grupo se cambia por lo individual, las ceremonias y la
    /// revision se cuentan en primera persona, los eventos con compañeros no salen y hay otros propios de estar solo.
    /// </summary>
    public class ModoSoloTests {
        private static Catalogo _catalogo;

        private static Catalogo Catalogo() {
            if (_catalogo != null) return _catalogo;
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            return _catalogo = CatalogLoader.CargarTodo(new CatalogoDeArchivos(raiz));
        }

        /// <summary>Nivel 1 con el recorrido ya hecho y el dia 1 empezado. Sin hojas de vida, trabajas solo.</summary>
        private static GameSession EnElDia(string metodologia, Dictionary<string, int> inventario) {
            var flags = new FlagStore(new Dictionary<string, double>(StringComparer.Ordinal), Catalogo().Flags);
            flags.Inicializar();
            var s = new GameSession(Catalogo(), "nivel-01", flags, 7);
            s.AplicarRecoleccion(TraductorDeRecoleccion3D.Traducir(s.EntradaDeRecoleccion(), Catalogo().Objetos3D, inventario, 0.5, false));
            s.ElegirMetodologia(metodologia, Catalogo().Metodologias[metodologia].TextosDeRazones.Keys.First());
            s.RepartirCalidad(new Dictionary<string, int>());
            var arq = s.Perfil.Fase1.Arquitecturas[0];
            s.ElegirArquitectura(arq.Id, s.Perfil.Fase1.RazonesDe(arq)[0].Id);
            s.CerrarFase1();
            s.ComenzarDia();
            if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
            return s;
        }

        private static GameSession Solo(string metodologia = "kanban") {
            return EnElDia(metodologia, new Dictionary<string, int> { { "consumable_pizza", 1 } });
        }

        private static void TerminarElDia(GameSession s) {
            for (var v = 0; v < 60 && !s.SePuedeCerrarLaJornada; v++) {
                foreach (var a in s.AvanzarReloj(30).AlertasQueSuenan) s.AtenderAlerta(a.Id);
                if (s.Decision != null) s.ResolverDecision(s.Decision.Opciones.First(o => !o.Bloqueada).Id);
                if (s.Minijuego != null) s.ResolverMinijuego(Nexus.Core.Minijuegos.PuenteDelMotor.Omitido(s.Minijuego.MinijuegoId, s.Minijuego.ObjetivoAprendizaje, null));
            }
            s.CerrarJornada();
            s.TerminarDia(false);
        }

        private static void PasarUnDia(GameSession s) {
            TerminarElDia(s);
            s.ComenzarDia();
            if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
            if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0) s.ElegirAccionRetro(s.PendingRetro.Acciones[0].Id);
        }

        [Test]
        public void Sin_hojas_de_vida_se_trabaja_solo_y_el_contenido_lo_sabe() {
            var s = Solo();
            Assert.IsTrue(s.TrabajasSolo);
            Assert.IsTrue(ConditionEvaluator.Evaluar("tamanoEquipo == 1", s));

            var conEquipo = EnElDia("kanban", new Dictionary<string, int> { { "cv_marta", 1 }, { "cv_kenji", 1 } });
            Assert.IsFalse(conEquipo.TrabajasSolo);
            Assert.IsTrue(ConditionEvaluator.Evaluar("tamanoEquipo == 2", conEquipo));
        }

        [Test]
        public void Las_tres_acciones_individuales_cuestan_tiempo_y_son_una_vez_al_dia() {
            var s = Solo();
            var doc = s.W.Documentacion;
            var minuto = s.MinutoDelDia;
            s.OrdenarTusIdeas();
            Assert.AreEqual(doc + 1, s.W.Documentacion, 0.001);
            Assert.AreEqual(minuto + GameSession.MinutosDeOrdenarIdeas, s.MinutoDelDia);
            Assert.AreEqual("Hoy ya lo hiciste.", s.PorQueNoSePuedeHacerSolo(GameSession.AccionOrdenarIdeas));

            var tu = s.Tablero.Miembros[0];
            var desvio = tu.DesvioDeCansancio;
            s.PausaActiva();
            Assert.AreEqual(desvio - 8, tu.DesvioDeCansancio, 0.001);

            var cobertura = s.W.Cobertura;
            s.PatoDeGoma();
            Assert.AreEqual(cobertura + 1.5, s.W.Cobertura, 0.001);

            PasarUnDia(s);
            Assert.IsNull(s.PorQueNoSePuedeHacerSolo(GameSession.AccionOrdenarIdeas), "al dia siguiente se puede otra vez");
        }

        [Test]
        public void Con_equipo_las_acciones_individuales_no_se_ofrecen() {
            var s = EnElDia("kanban", new Dictionary<string, int> { { "cv_marta", 1 } });
            Assert.IsNotNull(s.PorQueNoSePuedeHacerSolo(GameSession.AccionPatoDeGoma));
        }

        [Test]
        public void Solo_se_va_mas_despacio_aunque_no_se_asigne_nada_a_mano() {
            var s = Solo();
            s.AvanzarReloj(60);
            Assert.IsTrue(s.Tablero.Tarjetas.Any(c => c.Empezada && !c.Terminada), "hay algo en curso");
            Assert.Less(MotorDelTablero.FactorInstantaneo(s.Tablero), 1.0, "«Tú» no rinde como un equipo");
        }

        [Test]
        public void En_Kanban_solo_el_limite_de_trabajo_en_curso_es_dos() {
            Assert.AreEqual(2, Solo("kanban").R.LimiteWip);
        }

        [Test]
        public void Las_ceremonias_se_cuentan_en_primera_persona() {
            var s = Solo("kanban");
            var daily = s.CeremoniasDeHoy().FirstOrDefault(c => c.Id == "daily-de-tablero");
            Assert.IsNotNull(daily);
            Assert.AreEqual("Repaso ante el tablero", daily.Nombre);
            Assert.IsTrue(daily.Opciones.Any(o => o.Texto == "Repasar todo lo que llevas entre manos"));
            // Los efectos son los de siempre: solo cambia como se cuenta.
            var original = Catalogo().Metodologias["kanban"].Ceremonias.First(c => c.Id == "daily-de-tablero");
            CollectionAssert.AreEquivalent(original.Opciones.Select(o => o.Id), daily.Opciones.Select(o => o.Id));
        }

        [Test]
        public void La_retro_no_ofrece_programar_en_pareja_a_quien_trabaja_solo() {
            var s = Solo("scrum");
            for (var d = 0; d < 20 && s.PendingRetro == null && s.R.Fase == 2; d++) {
                TerminarElDia(s);
                if (s.R.Fase != 2) break;
                s.ComenzarDia();
                if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
            }
            Assert.IsNotNull(s.PendingRetro, "en Scrum llega una retro");
            Assert.IsFalse(s.PendingRetro.Acciones.Any(a => a.Id == "pareja"));
            Assert.IsNotEmpty(s.PendingRetro.Acciones);
        }

        [Test]
        public void Revisar_tu_propio_cambio_no_le_baja_la_moral_a_nadie() {
            var s = Solo();
            for (var d = 0; d < 12 && (s.PrPendiente == null || s.PrPendiente.TieneDefecto); d++) PasarUnDia(s);
            Assert.IsNotNull(s.PrPendiente, "algun dia sale un cambio correcto que revisar");
            var moral = s.W.MoralEquipo;
            ResultadoDeAvance avance;
            var r = s.RevisarPr(VeredictosDePr.PedirCambios, out avance);
            Assert.IsFalse(r.Acierto);
            Assert.AreEqual(moral, s.W.MoralEquipo, 0.001, "rechazar algo bien hecho cuesta tiempo, no moral");
        }

        [Test]
        public void Los_eventos_de_grupo_exigen_equipo_y_los_de_solo_lo_contrario() {
            var deGrupo = new[] { "EV-EQ-03", "EV-EQ-02", "EV-INT-01", "EV-OK-02", "EV-OK-03", "EV-CAL-02", "EV-BUE-04" };
            foreach (var id in deGrupo)
                CollectionAssert.Contains(Catalogo().Eventos.First(e => e.Id == id).Precondiciones, "tamanoEquipo >= 2", id);
            var deSolo = Catalogo().Eventos.Where(e => e.Id.StartsWith("EV-SOLO-")).ToList();
            Assert.AreEqual(4, deSolo.Count);
            foreach (var e in deSolo) CollectionAssert.Contains(e.Precondiciones, "tamanoEquipo == 1", e.Id);
            foreach (var mj in new[] { "MJ-F0-02", "MJ-F1-10", "MJ-F2-09", "MJ-F2-08", "MJ-F1-07", "MJ-F2-02" })
                CollectionAssert.Contains(Catalogo().Minijuegos.First(m => m.Id == mj).Precondiciones, "tamanoEquipo >= 2", mj);

            var s = Solo();
            Assert.IsFalse(ConditionEvaluator.Evaluar("tamanoEquipo >= 2", s));
        }
    }
}
