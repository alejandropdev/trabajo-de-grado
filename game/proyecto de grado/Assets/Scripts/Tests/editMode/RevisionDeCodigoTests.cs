using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Coleccion;
using Nexus.Core.Datos;
using Nexus.Core.Minijuegos;
using Nexus.Core.Tablero;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// La revision de codigo como un IDE (2026-11-07): lo que de ella vive en Core. Los numeros del margen del diff
    /// (antes / despues) y el lenguaje del cambio, para colorearlo y darle extension al archivo.
    /// </summary>
    public class RevisionDeCodigoTests {
        private static Catalogo _catalogo;

        private static Catalogo Catalogo() {
            if (_catalogo != null) return _catalogo;
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            return _catalogo = CatalogLoader.CargarTodo(new CatalogoDeArchivos(raiz));
        }

        private static LineaDiff L(string tipo) { return new LineaDiff { Tipo = tipo, Texto = "x" }; }

        [Test]
        public void El_margen_del_diff_numera_antes_y_despues_por_separado() {
            var n = NumeracionDeDiff.Numerar(new List<LineaDiff> { L("ctx"), L("del"), L("add"), L("add"), L("ctx") });
            Assert.AreEqual(1, n[0].Antes); Assert.AreEqual(1, n[0].Despues);
            Assert.AreEqual(2, n[1].Antes); Assert.AreEqual(0, n[1].Despues, "lo que se quita no tiene numero despues");
            Assert.AreEqual(0, n[2].Antes); Assert.AreEqual(2, n[2].Despues, "lo que se añade no tiene numero antes");
            Assert.AreEqual(3, n[3].Despues);
            Assert.AreEqual(3, n[4].Antes); Assert.AreEqual(4, n[4].Despues);
        }

        [Test]
        public void Se_cuentan_las_lineas_que_anade_y_las_que_quita() {
            int mas, menos;
            NumeracionDeDiff.Contar(new List<LineaDiff> { L("ctx"), L("del"), L("add"), L("add") }, out mas, out menos);
            Assert.AreEqual(2, mas);
            Assert.AreEqual(1, menos);
        }

        [Test]
        public void El_lenguaje_se_adivina_por_sus_marcas() {
            Assert.AreEqual("javascript", ResaltadoDeCodigo.Adivinar("function f(a) { return a => a; }"));
            Assert.AreEqual("python", ResaltadoDeCodigo.Adivinar("def f(self):\n    return self.x"));
            Assert.AreEqual("csharp", ResaltadoDeCodigo.Adivinar("public int Total() { var x = 1; return x; }"));
            Assert.AreEqual("sql", ResaltadoDeCodigo.Adivinar("SELECT nombre FROM visitas"));
            Assert.AreEqual(".py", ResaltadoDeCodigo.Extension("python"));
        }

        [Test]
        public void Todos_los_cambios_reales_se_numeran_y_tienen_lenguaje() {
            var prs = Catalogo().MicroPrs;
            Assert.That(prs.Count, Is.GreaterThan(0));
            foreach (var pr in prs) {
                var n = NumeracionDeDiff.Numerar(pr.Lineas);
                Assert.AreEqual(pr.Lineas.Count, n.Count, pr.Id);
                Assert.IsTrue(n.All(x => x.Antes > 0 || x.Despues > 0), pr.Id + ": cada linea tiene al menos un numero");
                var codigo = string.Join("\n", pr.Lineas.Select(l => l.Texto));
                Assert.IsFalse(string.IsNullOrEmpty(ResaltadoDeCodigo.Adivinar(codigo)), pr.Id);
            }
        }
    }
}
