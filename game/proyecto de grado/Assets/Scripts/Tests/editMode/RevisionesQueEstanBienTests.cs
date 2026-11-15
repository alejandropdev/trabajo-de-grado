using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Evaluacion;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Detectar;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using Nexus.Core.Tablero;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// No todo lo que se revisa esta mal (ronda 6: «quiero que haya algun caso donde si pueda aceptarlo y este bien»).
    /// Cada diagrama tiene un punto que parece raro y esta bien, hay documentos de etapa correctos y decisiones en las
    /// que lo bueno es dar por bueno el trabajo de un compañero. (Los diagramas enteramente limpios se quitaron el
    /// 2026-11-14: con «Está bien así» de un solo uso, cada diagrama trae al menos un problema de verdad.)
    /// </summary>
    public class RevisionesQueEstanBienTests {
        private static Catalogo _catalogo;

        private static string Raiz() {
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            return !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
        }

        private static Catalogo Catalogo() { return _catalogo ?? (_catalogo = CatalogLoader.CargarTodo(new CatalogoDeArchivos(Raiz()))); }

        private static MinijuegoDef Minijuego(string id) {
            return CatalogoMinijuegos.Parsear(File.ReadAllText(Path.Combine(Raiz(), "minijuegos", id + ".json")));
        }

        // ---------------------------------------------------------------- diagramas

        [Test]
        public void Aprobar_un_diagrama_que_si_tenia_defectos_los_deja_escapar() {
            var def = Minijuego("MJ-F0-01");
            var r = DetectarEvaluador.Evaluar(def, new DetectarState(def.Presentacion.SegundosReloj) { Aprobado = true });
            Assert.AreEqual(DetectarEvaluador.PARCIAL, r.Resultado);
        }

        // ---------------------------------------------------------------- documentos de etapa

        [Test]
        public void Cada_documento_de_cascada_tiene_extractos_que_estan_bien_y_extractos_con_un_problema() {
            var docs = Catalogo().Metodologias["cascada"].Calendario.Etapas.SelectMany(e => e.Documentos ?? new List<string>()).Distinct().ToList();
            Assert.IsNotEmpty(docs);
            foreach (var d in docs) {
                var suyos = Catalogo().Extractos.Where(x => x.Documento == d).ToList();
                Assert.IsTrue(suyos.Any(x => !x.TieneDefecto), d + ": ningún extracto correcto, firmar nunca sería lo bueno");
                Assert.IsTrue(suyos.Any(x => x.TieneDefecto), d + ": ningún extracto con problema");
                foreach (var x in suyos) Assert.That(x.Secciones.Count, Is.InRange(2, 5), x.Id);
            }
        }

        private static GameSession EnCascada() {
            var flags = new FlagStore(new Dictionary<string, double>(StringComparer.Ordinal), Catalogo().Flags);
            flags.Inicializar();
            var s = new GameSession(Catalogo(), "nivel-01", flags, 9);
            s.ElegirMetodologia("cascada", Catalogo().Metodologias["cascada"].TextosDeRazones.Keys.First());
            s.RepartirCalidad(new Dictionary<string, int>());
            var arq = Catalogo().Niveles["nivel-01"].Fase1.Arquitecturas[0];
            s.ElegirArquitectura(arq.Id, Catalogo().Niveles["nivel-01"].Fase1.RazonesDe(arq)[0].Id);
            s.CerrarFase1();
            s.ComenzarDia();
            return s;
        }

        [Test]
        public void Firmar_un_documento_correcto_acierta_y_pedirle_cambios_no() {
            var s = EnCascada();
            var doc = s.DocumentosDeHoy()[0];
            var extracto = s.ExtractoDeHoy(doc);
            Assert.IsNotNull(extracto);
            ResultadoDeAvance avance;
            var bueno = extracto.TieneDefecto ? VeredictosDePr.PedirCambios : VeredictosDePr.Aprobar;
            var r = s.InspeccionarDocumento(doc, bueno, out avance);
            Assert.IsTrue(r.Acierto);
            Assert.AreEqual(extracto.Explicacion, r.Explicacion);
            Assert.IsNotNull(s.PorQueNoSePuedeInspeccionar(doc), "una inspección al día");

            var otra = EnCascada();
            var malo = extracto.TieneDefecto ? VeredictosDePr.Aprobar : VeredictosDePr.PedirCambios;
            var r2 = otra.InspeccionarDocumento(doc, malo, out avance);
            Assert.IsFalse(r2.Acierto);
            Assert.Less(otra.R.Artefactos[doc].Calidad, s.R.Artefactos[doc].Calidad, "equivocarse deja el documento peor que acertar");
        }

        // ---------------------------------------------------------------- decisiones del dia

        [Test]
        public void Hay_decisiones_en_las_que_lo_correcto_es_dar_por_bueno_lo_revisado() {
            var eventos = Catalogo().Eventos.Where(e => e.Id.StartsWith("EV-OK-", StringComparison.Ordinal)).ToList();
            Assert.GreaterOrEqual(eventos.Count, 2);
            foreach (var e in eventos) {
                var correcta = e.Opciones.Single(o => o.Rubrica.Veredicto == Veredictos.Correcta);
                Assert.IsTrue(e.Opciones.Any(o => o.Rubrica.Veredicto == Veredictos.Incorrecta), e.Id + ": desconfiar sin motivo tiene que salir mal");
                // La buena no pide nada mas: acepta. Y no le cuesta nada al proyecto.
                Assert.IsTrue(correcta.EfectosInmediatos.Values.All(v => Convert.ToDouble(v) > 0), e.Id + ": aceptar lo que está bien no debería costar");
            }
        }
    }
}
