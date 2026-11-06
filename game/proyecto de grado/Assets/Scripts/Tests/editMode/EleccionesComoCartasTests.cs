using System;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Evaluacion;
using Nexus.Core.Tarjetas;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// Las elecciones como cartas (2026-11-05): la geometria de la mano (DisposicionDeMano), las teclas que eligen
    /// (TeclasDeEleccion) y lo que la entrevista-ascensor NO puede cambiar del pre-test.
    /// </summary>
    public class EleccionesComoCartasTests {
        // ================================================================ la mano

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void En_abanico_las_cartas_van_de_izquierda_a_derecha_simetricas_y_las_de_los_lados_giran_hacia_fuera(int n) {
            var l = DisposicionDeMano.Calcular(n, 200, 280, 18);
            Assert.IsFalse(DisposicionDeMano.EnRejilla(n));
            for (var i = 1; i < n; i++) Assert.Greater(l[i].X, l[i - 1].X, "en orden");
            for (var i = 0; i < n; i++) {
                var espejo = l[n - 1 - i];
                Assert.AreEqual(-l[i].X, espejo.X, 0.001, "simetria en X");
                Assert.AreEqual(l[i].Y, espejo.Y, 0.001, "el arco es simetrico");
                Assert.AreEqual(-l[i].Angulo, espejo.Angulo, 0.001, "el giro es simetrico");
            }
            Assert.Greater(l[0].Angulo, 0, "la de la izquierda gira a la izquierda (antihorario en Unity)");
            Assert.LessOrEqual(l[0].Y, l[n / 2].Y, "las de los lados quedan mas bajas");
        }

        [Test]
        public void La_carta_que_se_mira_se_endereza_sube_y_sus_vecinas_se_apartan() {
            var sin = DisposicionDeMano.Calcular(3, 200, 280, 18);
            var con = DisposicionDeMano.Calcular(3, 200, 280, 18, encima: 0);
            Assert.AreEqual(0, con[0].Angulo, 0.001);
            Assert.Greater(con[0].Y, sin[0].Y);
            Assert.AreEqual(sin[0].X, con[0].X, 0.001, "la mirada no se desplaza de lado");
            Assert.Greater(con[1].X, sin[1].X, "la de la derecha se aparta a la derecha");
            Assert.Greater(con[2].X, sin[2].X);
        }

        [Test]
        public void Con_mas_de_cuatro_opciones_es_una_rejilla_sin_giro_y_sin_solaparse() {
            const float ancho = 200, alto = 150, hueco = 18;
            var l = DisposicionDeMano.Calcular(7, ancho, alto, hueco);
            Assert.IsTrue(DisposicionDeMano.EnRejilla(7));
            Assert.IsTrue(l.All(x => x.Angulo == 0));
            for (var i = 0; i < l.Length; i++)
                for (var j = i + 1; j < l.Length; j++) {
                    var separadas = Math.Abs(l[i].X - l[j].X) >= ancho - 0.001 || Math.Abs(l[i].Y - l[j].Y) >= alto - 0.001;
                    Assert.IsTrue(separadas, $"las cartas {i} y {j} se pisan");
                }
            float w, h;
            DisposicionDeMano.Tamano(7, ancho, alto, hueco, out w, out h);
            Assert.AreEqual(3 * ancho + 2 * hueco, w, 0.001, "tres columnas");
            Assert.AreEqual(3 * alto + 2 * hueco, h, 0.001, "tres filas");
        }

        [Test]
        public void La_mano_reserva_sitio_para_toda_la_carta_aunque_se_abra() {
            float w, h;
            DisposicionDeMano.Tamano(3, 200, 280, 18, out w, out h);
            var abierta = DisposicionDeMano.Calcular(3, 200, 280, 18, encima: 1);
            foreach (var l in abierta) {
                Assert.LessOrEqual(Math.Abs(l.X) + 100, w / 2 + 0.001, "cabe a lo ancho");
                Assert.LessOrEqual(Math.Abs(l.Y) + 140, h / 2 + DisposicionDeMano.Subida + 0.001, "cabe a lo alto");
            }
        }

        // ================================================================ las teclas

        [TestCase("1", 3, 0)]
        [TestCase("3", 3, 2)]
        [TestCase("4", 3, -1)]
        [TestCase("a", 4, 0)]
        [TestCase("D", 4, 3)]
        [TestCase("e", 4, -1)]
        [TestCase("x", 3, -1)]
        [TestCase("", 3, -1)]
        [TestCase("12", 3, -1)]
        public void Una_tecla_elige_la_carta_que_le_toca_o_ninguna(string tecla, int n, int esperado) {
            Assert.AreEqual(esperado, TeclasDeEleccion.Indice(tecla, n));
        }

        [Test]
        public void El_rotulo_es_un_numero_o_la_letra_de_la_entrevista() {
            Assert.AreEqual("1", TeclasDeEleccion.Rotulo(0, false));
            Assert.AreEqual("3", TeclasDeEleccion.Rotulo(2, false));
            Assert.AreEqual("A", TeclasDeEleccion.Rotulo(0, true));
            Assert.AreEqual("D", TeclasDeEleccion.Rotulo(3, true));
        }

        // ================================================================ el pre-test no cambia

        private static string Raiz() {
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            Assert.IsTrue(Directory.Exists(raiz), "No encuentro el contenido en " + raiz);
            return raiz;
        }

        /// <summary>
        /// La entrevista pinta las opciones en el orden del archivo con la letra A, B, C… y su tecla. Para que la carta
        /// «A» sea la opcion «a» del instrumento, los ids tienen que ir en orden alfabetico desde la «a».
        /// </summary>
        [Test]
        public void Las_opciones_del_pre_test_vienen_en_orden_a_b_c_para_que_la_letra_de_la_carta_sea_la_del_instrumento() {
            var prueba = CatalogLoader.CargarTodo(new CatalogoDeArchivos(Raiz())).Admision;
            Assert.AreEqual(CorrectorDeAdmision.NumeroDePreguntas, prueba.Preguntas.Count);
            foreach (var p in prueba.Preguntas)
                for (var i = 0; i < p.Opciones.Count; i++)
                    Assert.AreEqual(TeclasDeEleccion.Rotulo(i, true).ToLowerInvariant(), p.Opciones[i].Id.ToLowerInvariant(),
                                    $"pregunta {p.Numero}, opcion {i}");
        }

        [Test]
        public void No_lo_se_sigue_contando_como_fallo() {
            var prueba = CatalogLoader.CargarTodo(new CatalogoDeArchivos(Raiz())).Admision;
            var respuestas = prueba.Preguntas.ToDictionary(p => p.Numero, p => (string)null);
            var resultado = CorrectorDeAdmision.Corregir(prueba, respuestas);
            Assert.AreEqual(0, resultado.Sum());
        }
    }
}
