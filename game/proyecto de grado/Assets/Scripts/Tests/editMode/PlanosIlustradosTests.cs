using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Nexus.Core.Coleccion;
using Nexus.Core.Datos;
using Nexus.Core.Fase1;
using Nexus.Core.Jornada;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// Los mapas ilustrados (2026-11-05): el plano del recorrido 3D con el jugador encima (PlanoDelRecorrido), el mapa
    /// de la oficina dibujado (ZonaDeNivel.Area) y los destellos que hay que atrapar (HuidaDelDestello).
    /// </summary>
    public class PlanosIlustradosTests {
        private static string Raiz() {
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            Assert.IsTrue(Directory.Exists(raiz), "No encuentro el contenido en " + raiz);
            return raiz;
        }

        // ================================================================ el plano del recorrido 3D

        /// <summary>Un suelo de 10 x 20 m dibujado como un trapecio (mas estrecho arriba) y girado como el de Nexus.</summary>
        private static PlanoDelRecorrido Trapecio() {
            return new PlanoDelRecorrido {
                Imagen = "Mapas/Prueba",
                AnchoImagen = 1000, AltoImagen = 600,
                Mundo = new PlanoDelRecorrido.LimitesDelMundo { MinX = -5, MaxX = 5, MinZ = 0, MaxZ = 20 },
                // (minX,minZ) arriba-izq, (maxX,minZ) abajo-izq, (maxX,maxZ) abajo-der, (minX,maxZ) arriba-der
                Esquinas = new[] {
                    new[] { 200f, 50f }, new[] { 20f, 550f }, new[] { 980f, 550f }, new[] { 800f, 50f }
                }
            };
        }

        [Test]
        public void Las_cuatro_esquinas_del_suelo_caen_en_sus_esquinas_del_dibujo() {
            var p = Trapecio();
            Assert.IsTrue(p.EsValido);
            AssertEn(p, -5, 0, 200, 50);
            AssertEn(p, 5, 0, 20, 550);
            AssertEn(p, 5, 20, 980, 550);
            AssertEn(p, -5, 20, 800, 50);
        }

        private static void AssertEn(PlanoDelRecorrido p, float x, float z, float px, float py) {
            float u, v;
            Assert.IsTrue(p.Proyectar(x, z, out u, out v));
            Assert.AreEqual(px, u * p.AnchoImagen, 0.01, $"x de ({x},{z})");
            Assert.AreEqual(py, v * p.AltoImagen, 0.01, $"y de ({x},{z})");
        }

        [Test]
        public void El_centro_del_suelo_cae_dentro_del_trapecio_y_mas_arriba_que_una_media_lineal() {
            // En perspectiva, la mitad del suelo queda por encima de la mitad del dibujo (lo lejano se ve mas pequeño).
            var p = Trapecio();
            float u, v;
            Assert.IsTrue(p.Proyectar(0, 10, out u, out v));
            Assert.AreEqual(0.5, u, 0.01, "centrado a lo ancho");
            Assert.Less(v * p.AltoImagen, 300f, "la perspectiva sube el centro");
            Assert.Greater(v * p.AltoImagen, 50f);
        }

        [Test]
        public void El_rumbo_respeta_el_giro_del_dibujo() {
            // En el dibujo, «arriba» es −X del mundo y «derecha» es +Z.
            var p = Trapecio();
            float g;
            Assert.IsTrue(p.Rumbo(0, 10, 0, 1, out g));     // mirar hacia +Z: hacia la derecha del dibujo
            Assert.AreEqual(0, g, 2);
            Assert.IsTrue(p.Rumbo(0, 10, -1, 0, out g));    // mirar hacia −X: hacia arriba (angulo negativo: la Y baja)
            Assert.AreEqual(-90, g, 5);
            Assert.IsFalse(p.Rumbo(0, 10, 0, 0, out g), "sin direccion no hay rumbo");
        }

        [Test]
        public void Unas_esquinas_alineadas_no_dan_un_plano_valido() {
            var p = Trapecio();
            p.Esquinas = new[] { new[] { 0f, 0f }, new[] { 10f, 0f }, new[] { 20f, 0f }, new[] { 30f, 0f } };
            Assert.IsFalse(p.EsValido);
            p = Trapecio();
            p.Mundo.MaxX = p.Mundo.MinX;
            Assert.IsFalse(p.EsValido, "un suelo sin ancho no se puede proyectar");
        }

        [Test]
        public void El_plano_real_del_recorrido_es_valido_y_el_punto_de_salida_cae_en_el_vestibulo() {
            var json = File.ReadAllText(Path.Combine(Raiz(), "mapa-3d.json"));
            var p = JsonConvert.DeserializeObject<PlanoDelRecorrido>(json);
            Assert.IsTrue(p.EsValido, "mapa-3d.json");
            Assert.AreEqual("Mapas/Mapa3D", p.Imagen);
            // El jugador aparece en (-6,1; -12,26): la estrella del vestibulo central del dibujo (~790, 520).
            float u, v;
            Assert.IsTrue(p.Proyectar(-6.1f, -12.262f, out u, out v));
            Assert.AreEqual(790, u * p.AnchoImagen, 60);
            Assert.AreEqual(520, v * p.AltoImagen, 60);
        }

        // ================================================================ el mapa de la oficina dibujado

        private static Nexus.Core.Modelo.LevelProfile Nivel(string id) {
            return CatalogLoader.CargarPerfil(File.ReadAllText(Path.Combine(Raiz(), "niveles", id + ".json")));
        }

        [TestCase("nivel-00", "Mapas/MapaNivel0")]
        [TestCase("nivel-01", "Mapas/MapaNivel1")]
        public void Los_niveles_con_mapa_tienen_plano_y_cada_zona_su_sala(string nivel, string imagen) {
            var p = Nivel(nivel);
            Assert.AreEqual(imagen, p.Mapa.Imagen);
            foreach (var z in p.Mapa.Zonas) Assert.IsTrue(z.TieneArea, z.Id + " no tiene area");
            var errores = SchemaValidator.ValidarNivel(p);
            Assert.AreEqual(0, errores.Count, string.Join(" | ", errores));
        }

        private static List<string> Errores(Action<MapaDeZonas> romper) {
            var p = Nivel("nivel-01");
            romper(p.Mapa);
            return SchemaValidator.ValidarNivel(p);
        }

        [Test]
        public void Con_plano_una_zona_sin_area_no_se_carga() {
            var e = Errores(m => m.PorId("bullpen").Area = null);
            Assert.IsTrue(e.Any(x => x.Contains("bullpen") && x.Contains("area")), string.Join(" | ", e));
        }

        [Test]
        public void Un_area_fuera_del_dibujo_no_se_carga() {
            var e = Errores(m => m.PorId("bullpen").Area = new[] { 0.9f, 0.1f, 0.3f, 0.2f });
            Assert.IsTrue(e.Any(x => x.Contains("se sale del dibujo")), string.Join(" | ", e));
        }

        [Test]
        public void Dos_salas_que_se_pisan_no_se_cargan() {
            var e = Errores(m => m.PorId("cafeteria").Area = (float[])m.PorId("archivo-de-voss").Area.Clone());
            Assert.IsTrue(e.Any(x => x.Contains("se pisan")), string.Join(" | ", e));
        }

        [Test]
        public void Zonas_con_area_sin_imagen_no_se_cargan() {
            var e = Errores(m => m.Imagen = null);
            Assert.IsTrue(e.Any(x => x.Contains("no dice su 'imagen'")), string.Join(" | ", e));
        }

        [Test]
        public void Un_mapa_sin_plano_ni_areas_sigue_valiendo_como_lista() {
            var e = Errores(m => {
                m.Imagen = null;
                foreach (var z in m.Zonas) z.Area = null;
            });
            Assert.AreEqual(0, e.Count, string.Join(" | ", e));
        }

        [Test]
        public void Clonar_una_zona_copia_su_area() {
            var z = new ZonaDeNivel { Id = "a", Area = new[] { 0.1f, 0.2f, 0.3f, 0.4f } };
            var c = z.Clone();
            c.Area[0] = 0.9f;
            Assert.AreEqual(0.1f, z.Area[0]);
        }

        // ================================================================ los destellos

        [TestCase(DificultadesDeColeccionable.Facil, 1)]
        [TestCase(DificultadesDeColeccionable.Media, 2)]
        [TestCase(DificultadesDeColeccionable.Dificil, 3)]
        [TestCase(null, 1)]
        public void Un_destello_se_escapa_segun_lo_dificil_que_fue_que_apareciera(string dificultad, int escapes) {
            var h = new HuidaDelDestello(dificultad);
            var huidas = 0;
            float x = 100, y = 100;
            for (var i = 0; i < 10; i++) {
                float nx, ny;
                // El cursor siempre encima: mientras le queden escapes, salta.
                if (h.Asustar(x, y, x + 5, y, 0, 0, 400, 300, out nx, out ny)) { huidas++; x = nx; y = ny; }
            }
            Assert.AreEqual(escapes, huidas);
            Assert.IsTrue(h.Cansado);
            Assert.Less(h.Ritmo, 1f, "cansado flota despacio: se deja coger");
        }

        [Test]
        public void Un_cursor_lejano_no_lo_asusta() {
            var h = new HuidaDelDestello(DificultadesDeColeccionable.Media);
            float nx, ny;
            Assert.IsFalse(h.Asustar(100, 100, 100 + HuidaDelDestello.RadioDeAlarma + 1, 100, 0, 0, 400, 300, out nx, out ny));
            Assert.AreEqual(2, h.Quedan);
        }

        [Test]
        public void Al_huir_se_aleja_del_cursor_sin_salirse_de_la_sala() {
            float nx, ny;
            // Acorralado en la esquina de abajo a la izquierda, con el cursor encima: se escurre, no atraviesa la pared.
            HuidaDelDestello.PuntoDeHuida(5, 5, 20, 20, HuidaDelDestello.SaltoDeHuida, 0, 0, 400, 300, out nx, out ny);
            Assert.That(nx, Is.InRange(0f, 400f));
            Assert.That(ny, Is.InRange(0f, 300f));
            var antes = Math.Sqrt(15 * 15 * 2);
            var despues = Math.Sqrt((nx - 20) * (nx - 20) + (ny - 20) * (ny - 20));
            Assert.Greater(despues, antes + 50, "se aleja de verdad");
        }
    }
}
