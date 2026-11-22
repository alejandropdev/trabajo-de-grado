using System.Linq;
using Nexus.Core.Fase1;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>La lista de objetos del 3D: completa, sin pistas de dónde están, y sin spoilers de lo secreto.</summary>
    public class GuiaDeObjetos3DTests {
        private static GrupoDeObjeto3D Grupo(string id) { return GuiaDeObjetos3D.Grupos.Single(g => g.Id == id); }

        [Test]
        public void Estan_todos_los_tipos_de_objeto_del_recorrido() {
            var ids = GuiaDeObjetos3D.Grupos.Select(g => g.Id).ToList();
            CollectionAssert.AreEquivalent(
                new[] { "cv", "pizza", "cafe", "guepardex", "certificado", "acceso", "cartas", "notas", "usb" }, ids);
            Assert.AreEqual(7, Grupo("cv").Cantidad);
            Assert.AreEqual(4, Grupo("acceso").Cantidad);
            Assert.AreEqual(7, Grupo("cartas").Cantidad);
            Assert.AreEqual(6, Grupo("notas").Cantidad);
        }

        [Test]
        public void Ningun_texto_dice_donde_aparece_un_objeto() {
            var prohibidas = new[] { "museo", "bodega", "oficinas", "cafetería", "cafeteria", "escondite", "zona", "piso", "aparece" };
            foreach (var g in GuiaDeObjetos3D.Grupos) {
                var texto = ((g.Nombre ?? "") + " " + (g.ParaQue ?? "") + " " + (g.PistaSecreta ?? "")).ToLowerInvariant();
                foreach (var p in prohibidas) StringAssert.DoesNotContain(p, texto, g.Id);
            }
        }

        [Test]
        public void Cada_grupo_tiene_nombre_para_que_sirve_y_glifo() {
            foreach (var g in GuiaDeObjetos3D.Grupos) {
                Assert.IsNotEmpty(g.Nombre, g.Id);
                Assert.IsNotEmpty(g.ParaQue, g.Id);
                Assert.IsNotEmpty(g.Glifo, g.Id);
            }
        }

        [Test]
        public void El_usb_es_un_signo_de_interrogacion_hasta_encontrarlo() {
            var usb = Grupo("usb");
            Assert.IsTrue(GuiaDeObjetos3D.EstaOculto(usb, new string[0]));
            Assert.IsTrue(GuiaDeObjetos3D.EstaOculto(usb, null));
            Assert.IsFalse(GuiaDeObjetos3D.EstaOculto(usb, new[] { "COL-USB-02" }));
        }

        [Test]
        public void Los_grupos_normales_nunca_se_ocultan() {
            Assert.IsFalse(GuiaDeObjetos3D.EstaOculto(Grupo("pizza"), new string[0]));
            Assert.IsFalse(GuiaDeObjetos3D.EstaOculto(Grupo("notas"), new string[0]));
        }

        [Test]
        public void Cuenta_las_piezas_del_diario_sin_repetir_y_por_prefijo() {
            var diario = new[] { "COL-NOTA-01", "COL-NOTA-01", "COL-NOTA-03", "COL-DEV-10", null };
            Assert.AreEqual(2, GuiaDeObjetos3D.Encontradas(Grupo("notas"), diario));
            Assert.AreEqual(1, GuiaDeObjetos3D.Encontradas(Grupo("cartas"), diario));
            Assert.AreEqual(0, GuiaDeObjetos3D.Encontradas(Grupo("pizza"), diario));
        }
    }
}
