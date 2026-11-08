using System.Linq;
using Nexus.Core.Modelo;
using NUnit.Framework;

namespace Nexus.Tests.EditMode {
    /// <summary>La tabla ISO 25010 de la Fase 1: nueve columnas, cada casilla con su significado, y los atributos de los niveles caen en ella.</summary>
    public class TablaIso25010Tests {
        [Test]
        public void La_tabla_tiene_nueve_columnas_con_ids_unicos() {
            Assert.AreEqual(9, TablaIso25010.Columnas.Count);
            Assert.AreEqual(9, TablaIso25010.Columnas.Select(c => c.Id).Distinct().Count());
        }

        [Test]
        public void Cada_casilla_tiene_nombre_y_significado() {
            foreach (var c in TablaIso25010.Columnas) {
                Assert.IsNotEmpty(c.Resumen, c.Id);
                Assert.IsNotEmpty(c.Subcaracteristicas, c.Id);
                foreach (var s in c.Subcaracteristicas) {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(s.Nombre), c.Id);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(s.Significado), c.Id + "/" + s.Nombre);
                }
            }
        }

        [TestCase("adecuacion-funcional", "adecuacion-funcional")]
        [TestCase("fiabilidad", "fiabilidad")]
        [TestCase("seguridad", "seguridad")]
        [TestCase("mantenibilidad", "mantenibilidad")]
        [TestCase("usabilidad", "capacidad-de-interaccion")]
        [TestCase("rendimiento", "eficiencia-de-desempeno")]
        public void Los_atributos_de_los_niveles_son_columnas_de_la_tabla(string atributo, string columna) {
            Assert.AreEqual(columna, TablaIso25010.IdDeColumna(atributo));
            Assert.IsNotNull(TablaIso25010.DeAtributo(atributo));
        }

        [Test]
        public void Un_atributo_desconocido_no_tiene_columna() {
            Assert.IsNull(TablaIso25010.IdDeColumna("inventado"));
            Assert.IsNull(TablaIso25010.IdDeColumna(null));
            Assert.IsNull(TablaIso25010.DeAtributo("inventado"));
        }
    }
}
