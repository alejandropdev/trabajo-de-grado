using System;
using System.Collections.Generic;
using System.IO;
using Nexus.Core;
using Nexus.Core.Datos;
using Nexus.Core.Guardado;
using Nexus.Core.Modelo;
using Nexus.Core.Sesion;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// Salir del juego a mitad de la Fase 1 no obliga a empezarla de cero: la planificacion a medias (el paso, lo
    /// elegido, el backlog armado) se escribe en disco y al abrir la partida vuelve tal cual, sin entregarse al motor.
    /// </summary>
    public class Fase1EnCursoGuardadoTests {
        private static Catalogo _catalogo;

        private static Catalogo Catalogo() {
            if (_catalogo != null) return _catalogo;
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            return _catalogo = CatalogLoader.CargarTodo(new CatalogoDeArchivos(raiz));
        }

        [Test]
        public void La_fase_1_a_medias_vuelve_del_disco_en_el_paso_y_con_lo_elegido() {
            var store = new SaveStore(new AlmacenEnMemoria(), () => new DateTime(2026, 11, 14, 10, 0, 0, DateTimeKind.Utc));
            var save = store.Crear("p1", "A medias", "nivel-01", 77, false, 1);
            var fabrica = new FabricaDeSesion(Catalogo());
            var sesion = store.AbrirSesion(save, "nivel-01", fabrica);

            sesion.R.IntroVista = true;
            sesion.R.Fase1Borrador = new Fase1Borrador {
                Paso = "Calidad", SubPaso = 0, Metodologia = "scrum", RazonMetodologia = "una_razon", DuracionDeUnidad = "dos_semanas",
                Fichas = new Dictionary<string, int> { { "seguridad", 2 } },
                Comprobacion = new Dictionary<string, int> { { "q1", 1 } },
                FalladasAlguna = new List<string> { "q2" },
                Backlog = new List<string> { "h1", "h2" }
            };
            store.Sincronizar(save, sesion);

            var cargada = store.Cargar("p1", save.Id);
            Assert.IsNotNull(cargada.Nivel, "la Fase 1 sin cerrar ya tiene nivel en disco");
            var vuelta = store.AbrirSesion(cargada, "nivel-01", fabrica);

            Assert.IsFalse(vuelta.Fase1Cerrada, "el borrador no se entrega al motor al guardar");
            Assert.IsTrue(vuelta.R.IntroVista, "la intro no se repite");
            var b = vuelta.R.Fase1Borrador;
            Assert.AreEqual("Calidad", b.Paso);
            Assert.AreEqual("scrum", b.Metodologia);
            Assert.AreEqual("dos_semanas", b.DuracionDeUnidad);
            Assert.AreEqual(2, b.Fichas["seguridad"]);
            Assert.AreEqual(1, b.Comprobacion["q1"]);
            CollectionAssert.AreEqual(new[] { "q2" }, b.FalladasAlguna);
            CollectionAssert.AreEqual(new[] { "h1", "h2" }, b.Backlog);
        }

        [Test]
        public void Guardar_varias_veces_la_fase_1_a_medias_no_cambia_el_estado_del_mundo() {
            var store = new SaveStore(new AlmacenEnMemoria(), () => new DateTime(2026, 11, 14, 10, 0, 0, DateTimeKind.Utc));
            var save = store.Crear("p1", "A medias", "nivel-01", 77, false, 1);
            var fabrica = new FabricaDeSesion(Catalogo());
            var sesion = store.AbrirSesion(save, "nivel-01", fabrica);
            sesion.R.IntroVista = true;
            store.Sincronizar(save, sesion);
            var primera = JsonDeGuardado.Serializar(store.Cargar("p1", save.Id).Nivel.W);

            var vuelta = store.AbrirSesion(store.Cargar("p1", save.Id), "nivel-01", fabrica);
            store.Sincronizar(save, vuelta);
            Assert.AreEqual(primera, JsonDeGuardado.Serializar(store.Cargar("p1", save.Id).Nivel.W));
        }
    }
}
