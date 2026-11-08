using System;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Fase1;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// Las tarjetas de suministros (2026-11-07) usan la misma imagen que el inventario del 3D, copiada en
    /// Resources/Ilustraciones/suministros/&lt;id&gt;.png para poder cargarla en la interfaz. Un suministro nuevo sin su
    /// imagen saldria con el dibujo de respaldo: este test lo avisa.
    /// </summary>
    public class SuministrosConImagenTests {
        [Test]
        public void Cada_suministro_tiene_su_imagen_del_3D_en_Resources() {
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            var catalogo = CatalogLoader.CargarTodo(new CatalogoDeArchivos(raiz));
            var suministros = catalogo.Objetos3D.Objetos.Where(o => o.Tipo == TiposDeObjeto3D.Suministro).ToList();
            Assert.That(suministros.Count, Is.GreaterThan(0));
            var carpeta = Path.Combine(raiz, "..", "Resources", "Ilustraciones", "suministros");
            foreach (var s in suministros)
                Assert.IsTrue(File.Exists(Path.Combine(carpeta, s.Id + ".png")), $"{s.Id} («{s.Nombre}») no tiene su imagen en Resources/Ilustraciones/suministros.");
        }

        /// <summary>
        /// Una PNG que no mide potencia de dos (el cafe es 106×186) Unity la reescala al importarla si su .meta no dice
        /// nPOTScale: 0, y en la tarjeta salia estirada (2026-11-07). Se importan como Sprite, sin reescalar.
        /// </summary>
        [Test]
        public void Las_imagenes_de_suministros_se_importan_sin_reescalar() {
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            var carpeta = Path.Combine(raiz, "..", "Resources", "Ilustraciones", "suministros");
            foreach (var meta in Directory.GetFiles(carpeta, "*.png.meta")) {
                var texto = File.ReadAllText(meta);
                StringAssert.Contains("nPOTScale: 0", texto, Path.GetFileName(meta) + ": se reescalaría y saldría estirada");
                StringAssert.Contains("textureType: 8", texto, Path.GetFileName(meta) + ": debe importarse como Sprite");
            }
        }
    }
}
