using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Detectar;
using Nexus.Core.Minijuegos.Grafo;
using NUnit.Framework;

namespace Nexus.Tests
{
    /// <summary>
    /// Estos tests no abren Unity. Es la razon de que Nexus.Core no referencie
    /// UnityEngine: la logica pedagogica se puede defender ante el jurado
    /// ejecutandola en una terminal.
    /// </summary>
    public class MinijuegoDetectarTests
    {
        private MinijuegoDef Cargar()
        {
            var ruta = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "..", "..", "Assets", "StreamingAssets", "minijuegos", "MJ-F2-02.json");
            ruta = Path.GetFullPath(ruta);
            Assert.IsTrue(File.Exists(ruta), "No encuentro el JSON en " + ruta);
            return CatalogoMinijuegos.Parsear(File.ReadAllText(ruta));
        }

        private static DetectarState ConMarcas(MinijuegoDef def, params (string[] commits, string etiqueta)[] marcas)
        {
            var st = new DetectarState(def.Presentacion.SegundosReloj);
            foreach (var m in marcas)
            {
                foreach (var c in m.commits) st.Alternar(c);
                st.Marcar(m.etiqueta, 10f);
            }
            return st;
        }

        [Test]
        public void El_json_del_catalogo_es_valido()
        {
            var def = Cargar();
            CollectionAssert.IsEmpty(CatalogoMinijuegos.Validar(def));
        }

        [Test]
        public void Sin_marcas_el_resultado_es_omitido()
        {
            var def = Cargar();
            var res = DetectarEvaluador.Evaluar(def, new DetectarState(90));
            Assert.AreEqual(DetectarEvaluador.OMITIDO, res.Resultado);
            Assert.AreEqual("incorrecta", res.Rubrica.Veredicto);
            CollectionAssert.IsEmpty(res.Hallazgos);
        }

        [Test]
        public void Las_dos_zonas_con_su_etiqueta_dan_todos()
        {
            var def = Cargar();
            var st = ConMarcas(def,
                (new[] { "x03", "x05", "x06" }, "historia_reescrita"),
                (new[] { "x04" }, "autoria_perdida"));
            var res = DetectarEvaluador.Evaluar(def, st);
            Assert.AreEqual(DetectarEvaluador.TODOS, res.Resultado);
            CollectionAssert.Contains(res.Hallazgos, "HAL-AUTORIA-N3");
        }

        [Test]
        public void La_etiqueta_equivocada_sobre_la_zona_correcta_es_parcial()
        {
            var def = Cargar();
            var st = ConMarcas(def, (new[] { "x03", "x04" }, "rama_huerfana"));
            var res = DetectarEvaluador.Evaluar(def, st);
            Assert.AreEqual(DetectarEvaluador.PARCIAL, res.Resultado);
        }

        [Test]
        public void Tocar_un_senuelo_manda_sobre_haber_acertado_todo()
        {
            var def = Cargar();
            var st = ConMarcas(def,
                (new[] { "x03", "x04", "x05", "x06" }, "historia_reescrita"),
                (new[] { "x04" }, "autoria_perdida"),
                (new[] { "mrg" }, "fusion_sin_revisar"));
            var res = DetectarEvaluador.Evaluar(def, st);
            Assert.AreEqual(DetectarEvaluador.FALSO_POSITIVO, res.Resultado);
            Assert.IsTrue(res.EfectosDiferidos.Any(d => d.EventoForzado == "EV-EQ-07"));
        }

        [Test]
        public void La_traza_registra_que_se_marco_y_cuando()
        {
            var def = Cargar();
            var st = ConMarcas(def, (new[] { "x04" }, "autoria_perdida"));
            var res = DetectarEvaluador.Evaluar(def, st);
            Assert.AreEqual(1, res.Traza.Count);
            Assert.AreEqual("Z2", res.Traza[0].ZonaAcertada);
        }

        [Test]
        public void El_layout_es_determinista_y_no_solapa_carriles()
        {
            var def = Cargar();
            var a = GrafoLayout.Calcular(def.Artefacto);
            var b = GrafoLayout.Calcular(def.Artefacto);
            CollectionAssert.AreEqual(
                a.Nodos.Select(n => n.CommitId + n.Carril + n.Fila),
                b.Nodos.Select(n => n.CommitId + n.Carril + n.Fila));

            var ocupadas = new HashSet<string>();
            foreach (var n in a.Nodos)
                Assert.IsTrue(ocupadas.Add(n.Carril + ":" + n.Fila), "Dos commits en la misma celda");
        }

        [Test]
        public void El_tramo_reescrito_comparte_fecha_y_mensaje_con_el_original()
        {
            var def = Cargar();
            var dups = GrafoLayout.FechasDuplicadas(def.Artefacto);
            Assert.IsTrue(dups.Count >= 3, "La pista visual del tramo reescrito no esta sembrada");
        }

        [Test]
        public void Una_zona_dentro_de_otra_se_acredita_por_separado() {
            var def = Cargar();
            var st = ConMarcas(def,
                (new[] { "x03", "x04", "x05", "x06" }, "historia_reescrita"),  // incluye x04
                (new[] { "x04" }, "autoria_perdida"));
            var res = DetectarEvaluador.Evaluar(def, st);
            Assert.AreEqual(DetectarEvaluador.TODOS, res.Resultado);
        }
    }
}
