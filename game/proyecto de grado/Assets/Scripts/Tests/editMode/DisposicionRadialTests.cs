using System;
using System.Linq;
using Nexus.Core.Tarjetas;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// La pausa en red (2026-11-07): donde va cada nodo (DisposicionRadial). Es solo geometria; lo que se ve en pantalla
    /// se comprueba abriendo la pausa en el editor.
    /// </summary>
    public class DisposicionRadialTests {
        private const float Rx = 560, Ry = 250, Nodo = 132, MitadDeSeguir = 220;

        [Test]
        public void Sin_nodos_no_hay_puntos() {
            Assert.AreEqual(0, DisposicionRadial.Calcular(0, Rx, Ry).Length);
            Assert.AreEqual(0, DisposicionRadial.Calcular(-3, Rx, Ry).Length);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void Todos_los_puntos_estan_en_la_elipse(int n) {
            foreach (var p in DisposicionRadial.Calcular(n, Rx, Ry)) {
                var ecuacion = (p.X * p.X) / (Rx * Rx) + (p.Y * p.Y) / (Ry * Ry);
                Assert.AreEqual(1.0, ecuacion, 1e-3);
            }
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void La_red_es_simetrica_en_horizontal(int n) {
            var puntos = DisposicionRadial.Calcular(n, Rx, Ry);
            foreach (var p in puntos) {
                var espejo = puntos.Any(q => Math.Abs(q.X + p.X) < 0.01 && Math.Abs(q.Y - p.Y) < 0.01);
                Assert.IsTrue(espejo, $"el punto ({p.X:0.#},{p.Y:0.#}) no tiene su espejo");
            }
        }

        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void Ningun_nodo_cae_sobre_el_boton_de_seguir(int n) {
            // «Seguir jugando» va en la vertical de abajo: ningun nodo por debajo del centro y dentro de su ancho.
            foreach (var p in DisposicionRadial.Calcular(n, Rx, Ry))
                Assert.IsFalse(p.Y < 0 && Math.Abs(p.X) < MitadDeSeguir, $"n={n}: un nodo en ({p.X:0.#},{p.Y:0.#}) taparia «Seguir»");
        }

        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void Los_nodos_no_se_pisan(int n) {
            var puntos = DisposicionRadial.Calcular(n, Rx, Ry);
            for (var i = 0; i < puntos.Length; i++)
                for (var j = i + 1; j < puntos.Length; j++) {
                    var d = Math.Sqrt(Math.Pow(puntos[i].X - puntos[j].X, 2) + Math.Pow(puntos[i].Y - puntos[j].Y, 2));
                    Assert.Greater(d, Nodo, $"n={n}: los nodos {i} y {j} se pisan");
                }
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(5)]
        public void Con_un_numero_impar_el_primer_nodo_esta_arriba_del_todo(int n) {
            var p = DisposicionRadial.Calcular(n, Rx, Ry);
            Assert.AreEqual(0, p[0].X, 0.01);
            Assert.AreEqual(Ry, p[0].Y, 0.01);
            Assert.AreEqual(p.Max(q => q.Y), p[0].Y, 0.01, "es el mas alto");
        }

        [Test]
        public void Los_nodos_recorren_la_elipse_en_sentido_horario() {
            // Con tres nodos: arriba, abajo a la derecha, abajo a la izquierda.
            var p = DisposicionRadial.Calcular(3, Rx, Ry);
            Assert.Greater(p[1].X, 0, "el segundo, a la derecha");
            Assert.Less(p[2].X, 0, "el tercero, a la izquierda");
            Assert.Less(p[1].Y, p[0].Y);
        }

        [Test]
        public void El_angulo_inicial_deja_libre_la_vertical_de_abajo() {
            Assert.AreEqual(90f, DisposicionRadial.AnguloInicial(3), 0.001f);
            Assert.AreEqual(90f, DisposicionRadial.AnguloInicial(5), 0.001f);
            Assert.AreEqual(45f, DisposicionRadial.AnguloInicial(4), 0.001f, "con cuatro, en las diagonales");
            Assert.AreEqual(0f, DisposicionRadial.AnguloInicial(2), 0.001f, "con dos, a los lados");
        }
    }
}
