using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using UnityEngine;
using L = Nexus.Unity.Tema.LuzUI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// (2026-11-15, direccion de arte) Mas objetos del mundo para el resto de metricas:
    ///   alcance       → el vagon de carga: cada caja lleva su tarjeta pegada; el cliente habla por un altavoz de poste;
    ///   deuda tecnica → las tuberias del bunker: parches de cinta, fugas de vapor y chispas (los errores);
    ///   cobertura     → modulos con su lampara de inspeccion y la rejilla que para las chispas;
    ///   satisfaccion  → el cartel de estrellas de neon de la torre del cliente.
    /// </summary>
    public static partial class DibujosDeEscena {
        // ================================================================ alcance

        /// <summary>Caja de carga con su tarjeta pegada (lo prometido). 'giro' la ladea (cuando ya no cabe).</summary>
        private static void CajaDeCarga(Tizador v, float x, float y, float w, float h, Color tinte, float giro = 0) {
            float cx = x + w / 2, cy = y + h / 2;
            List<Vector2> R(List<Vector2> p) { return giro == 0 ? p : Tizador.Rotar(p, cx, cy, giro); }
            v.Forma(R(L.Redondeado(x, y, w, h, 2.5f)), Color.Lerp(tinte, Color.white, 0.12f), Color.Lerp(tinte, Silueta, 0.5f));
            v.Forma(R(Caja(x + 1, y + 1, w - 2, 2)), A(Color.white, 0.25f));
            v.Forma(R(Caja(x + w * 0.3f, y + h * 0.26f, w * 0.4f, h * 0.42f)), A(Tinta, 0.85f));
            v.Forma(R(Caja(x + w * 0.35f, y + h * 0.4f, w * 0.3f, 1.4f)), A(Silueta, 0.55f));
            v.Forma(R(Caja(x + w * 0.35f, y + h * 0.52f, w * 0.2f, 1.4f)), A(Silueta, 0.55f));
        }

        /// <summary>Plataforma de carga del monorriel sobre la via (yRiel = cara de arriba de la via). Devuelve la y de la cama.</summary>
        private static float Plataforma(Tizador v, float x, float yRiel, float w) {
            foreach (var bx in new[] { x + 26, x + w - 26 }) v.Forma(L.Elipse(bx, yRiel - 3, 12, 5, 16), H(0x14181A));
            v.Forma(L.Redondeado(x, yRiel - 18, w, 13, 4), H(0x5A666B), H(0x232B30));
            v.Forma(Caja(x + 10, yRiel - 12, w - 20, 2.4f), Aviso);
            v.Forma(Caja(x + 4, yRiel - 18, w - 8, 1.6f), A(Color.white, 0.3f));
            v.Forma(L.Redondeado(x + 2, yRiel - 58, 6, 42, 2), Cromo, CromoOscuro);
            v.Forma(L.Redondeado(x + w - 8, yRiel - 58, 6, 42, 2), Cromo, CromoOscuro);
            return yRiel - 18;
        }

        /// <summary>Altavoz de bocina en su poste (la voz del cliente): la bocina mira a la izquierda.</summary>
        private static void Altavoz(Tizador v, float cx, float cy, float suelo, Color voz) {
            v.Cable(Pts(cx + 4, cy, cx + 4, suelo), 3, 4, H(0x1A0C1E));
            v.Forma(Pts(cx + 8, cy - 6, cx + 8, cy + 6, cx - 24, cy + 16, cx - 24, cy - 16), Cromo, CromoOscuro);
            v.Forma(L.Elipse(cx - 24, cy, 4, 16, 16), H(0x14181A));
            v.Forma(L.Arco(cx - 28, cy, 14, 18, 2, 140, 220, 12), A(voz, 0.9f));
            v.Forma(L.Arco(cx - 28, cy, 24, 28, 2, 145, 215, 12), A(voz, 0.6f));
            v.Halo(cx - 34, cy, 24, A(voz, 0.25f));
        }

        /// <summary>Bocadillo de cristal oscuro con su cola hacia (colaX, colaY).</summary>
        private static void Bocadillo(Tizador v, float x, float y, float w, float h, string texto, Color c, float colaX, float colaY, float tam = 16) {
            var bx = Mathf.Clamp(colaX, x + 16, x + w - 16);
            v.Forma(Pts(bx - 9, y + h - 2, bx + 9, y + h - 2, colaX, colaY), A(c, 0.8f));
            v.Forma(L.Redondeado(x - 1.5f, y - 1.5f, w + 3, h + 3, 11), A(c, 0.8f));
            v.Forma(L.Redondeado(x, y, w, h, 10), H(0x10202A, 0.95f), H(0x08121A, 0.95f));
            v.Forma(Pts(bx - 7, y + h - 1, bx + 7, y + h - 1, colaX + (bx - colaX) * 0.25f, colaY - 5), H(0x08121A, 0.95f));
            v.Rotulo(x + w / 2, y + h / 2, texto, tam, c);
        }

        /// <summary>Balanza de platillos: devuelve el centro de cada platillo (izquierdo, derecho).</summary>
        private static Vector2[] Balanza(Tizador v, float cx, float yEje, float brazo, float suelo, float giro = 0) {
            v.Forma(Pts(cx - 5, yEje, cx + 5, yEje, cx + 16, suelo - 8, cx - 16, suelo - 8), H(0x46555A), H(0x1A2428));
            v.Forma(L.Redondeado(cx - 40, suelo - 10, 80, 10, 4), Cromo, CromoOscuro);
            var izq = new Vector2(cx - brazo, yEje - Mathf.Sin(giro * Mathf.Deg2Rad) * brazo);
            var der = new Vector2(cx + brazo, yEje + Mathf.Sin(giro * Mathf.Deg2Rad) * brazo);
            v.Forma(Tizador.Rotar(L.Redondeado(cx - brazo - 6, yEje - 4, brazo * 2 + 12, 8, 4), cx, yEje, giro), Cromo, CromoOscuro);
            v.Forma(L.Elipse(cx, yEje, 8, 8, 16), Aviso, H(0x8A6A1A));
            var platos = new Vector2[2];
            var k = 0;
            foreach (var e in new[] { izq, der }) {
                var plato = new Vector2(e.x, e.y + 62);
                v.Cable(Pts(e.x, e.y, plato.x - 38, plato.y), 1.4f, 1.4f, CromoOscuro);
                v.Cable(Pts(e.x, e.y, plato.x + 38, plato.y), 1.4f, 1.4f, CromoOscuro);
                v.Forma(L.Elipse(plato.x, plato.y, 44, 13, 24, 0, 180), Cromo, CromoOscuro);
                v.Forma(L.Elipse(plato.x, plato.y, 44, 3, 20), A(Color.white, 0.35f));
                platos[k++] = plato;
            }
            return platos;
        }

        // ================================================================ deuda tecnica: tuberias

        /// <summary>Tuberia con volumen: cuerpo, brillo arriba y sombra abajo.</summary>
        private static void Tubo(Tizador v, List<Vector2> eje, float g, Color c) {
            v.Cable(L.Mover(eje, 0, g * 0.2f), g * 1.05f, g * 1.05f, A(Silueta, 0.5f));
            v.Cable(eje, g, g, c);
            v.Cable(L.Mover(eje, 0, -g * 0.26f), g * 0.22f, g * 0.22f, A(Color.Lerp(c, Color.white, 0.5f), 0.55f));
        }

        /// <summary>Brida: el anillo que une dos tramos (vertical: el tubo va en horizontal).</summary>
        private static void Brida(Tizador v, float x, float y, float g) {
            v.Forma(L.Redondeado(x - 3.5f, y - g * 0.78f, 7, g * 1.56f, 2), Cromo, CromoOscuro);
        }

        /// <summary>Parche de cinta sobre un tubo; 'apuntado' le cuelga su etiqueta de papel.</summary>
        private static void Parche(Tizador v, float x, float y, float g, bool apuntado = false) {
            v.Forma(Tizador.Rotar(L.Redondeado(x - 10, y - g * 0.82f, 20, g * 1.64f, 2), x, y, 14), H(0xC9C1A2), H(0x8C846B));
            v.Forma(Tizador.Rotar(L.Redondeado(x - 3, y - g * 0.86f, 12, g * 1.72f, 2), x + 3, y, -10), H(0xD8D0B0, 0.9f), H(0x9A9276, 0.9f));
            if (!apuntado) return;
            v.Cable(Pts(x + 4, y + g * 0.6f, x + 18, y + g * 1.9f), 1.2f, 1.2f, H(0xD8D0B0));
            v.Forma(Tizador.Rotar(L.Redondeado(x + 8, y + g * 1.9f, 64, 22, 3), x + 18, y + g * 1.9f, 6), H(0xF2EAD2), H(0xD9CFB0));
            v.Rotulo(x + 40, y + g * 1.9f + 13, "apuntado", 12, H(0x1E2A2A));
        }

        /// <summary>Volutas de vapor que suben desde (x, y). 'fuerza' de 0 a 1.</summary>
        private static void Vapor(Tizador v, float x, float y, float fuerza, int semilla = 0) {
            var volutas = fuerza > 0.7f ? 3 : fuerza > 0.3f ? 2 : 1;
            for (var i = 0; i < volutas; i++) {
                var eje = new List<Vector2>();
                var x0 = x - 5 + i * 5;
                var alto = 22 + fuerza * 34 - i * 4;
                for (var k = 0; k <= 14; k++) {
                    var t = k / 14f;
                    eje.Add(new Vector2(x0 + Mathf.Sin(t * 5.2f + i * 1.7f + semilla) * (2 + 5 * t), y - t * alto));
                }
                var hilo = v.Cable(eje, 3.5f + 3 * fuerza, 0.4f, A(Tinta, 0.16f + 0.14f * fuerza));
                v.Latido(hilo, 0.45f, 1, 2.4f + i * 0.6f, i * 0.3f + semilla);
            }
        }

        /// <summary>Una chispa coral: un error que salta (aqui no hay bichos de clip-art).</summary>
        private static void Chispa(Tizador v, float x, float y, float r, float fase = 0) {
            v.Latido(v.Halo(x, y, r * 3, A(Coral, 0.45f)), 0.35f, 1, 0.9f, fase);
            var q = r * 0.28f;
            v.Forma(Pts(x, y - r, x + q, y - q, x + r, y, x + q, y + q, x, y + r, x - q, y + q, x - r, y, x - q, y - q), BombillaCentro, Coral);
        }

        /// <summary>Llave inglesa (la herramienta con que se paga la deuda).</summary>
        private static void Llave(Tizador v, float x, float y, float largo, float giro) {
            v.Forma(Tizador.Rotar(L.Redondeado(x, y - 4, largo, 8, 4), x, y, giro), Cromo, CromoOscuro);
            var a = giro * Mathf.Deg2Rad;
            var cabeza = new Vector2(x + Mathf.Cos(a) * largo, y + Mathf.Sin(a) * largo);
            v.Forma(Tizador.Rotar(L.Arco(cabeza.x, cabeza.y, 13, 13, 7, 40, 320, 20), cabeza.x, cabeza.y, giro), Cromo, CromoOscuro);
        }

        /// <summary>Pila de monedas (los intereses de la deuda).</summary>
        private static void Monedas(Tizador v, float x, float yBase, int n) {
            for (var i = 0; i < n; i++) {
                var y = yBase - 4 - i * 5;
                v.Forma(L.Elipse(x, y + 2, 14, 4.5f, 18), H(0x6E5414));
                v.Forma(L.Elipse(x, y, 14, 4.5f, 18), H(0xF2D56A), H(0xB08A22));
            }
            v.Halo(x, yBase - n * 5, 20, A(Aviso, 0.25f));
        }

        // ================================================================ cobertura

        /// <summary>Modulo de la maquina con su lampara de inspeccion: menta si alguien lo comprobo; apagado y con «?» si no.</summary>
        private static void Modulo(Tizador v, float x, float y, float w, float h, bool probado) {
            v.Forma(L.Redondeado(x, y, w, h, 6), probado ? H(0x2B4F57) : H(0x1E2A2E), probado ? H(0x11262C) : H(0x0C1417));
            v.Forma(Caja(x + 4, y + 2, w - 8, 1.6f), A(Color.white, probado ? 0.25f : 0.08f));
            float lx = x + w - 12, ly = y + 12;
            if (probado) {
                v.Halo(lx, ly, 18, A(Menta, 0.5f));
                v.Forma(L.Elipse(lx, ly, 4.5f, 4.5f, 12), H(0xE6FFF3), Menta);
                v.Forma(Caja(x + 8, y + h - 12, w * 0.5f, 3), A(Menta, 0.7f));
            } else {
                v.Forma(L.Elipse(lx, ly, 4.5f, 4.5f, 12), H(0x2A3538));
                v.Rotulo(x + w * 0.4f, y + h * 0.55f, "?", 20, A(TintaSuave, 0.7f));
            }
        }

        /// <summary>Rejilla de inspeccion: barrotes entre dos largueros. 'rota' le arranca dos barrotes.</summary>
        private static void Rejilla(Tizador v, float x, float y, float w, float h, bool rota, Color luz) {
            if (!rota) v.Halo(x + w / 2, y + h / 2, w * 1.4f, h * 0.7f, A(luz, 0.22f));
            var n = Mathf.Max(3, Mathf.RoundToInt(w / 11));
            for (var i = 0; i < n; i++) {
                var bx = x + 6 + i * (w - 12) / (n - 1);
                if (rota && (i == n / 2 || i == n / 2 - 1)) {
                    v.Forma(L.Redondeado(bx - 2, y + 4, 4, h * 0.22f, 2), CromoOscuro, Silueta);
                    v.Forma(L.Redondeado(bx - 2, y + h - h * 0.18f - 4, 4, h * 0.18f, 2), CromoOscuro, Silueta);
                } else v.Forma(L.Redondeado(bx - 2, y + 4, 4, h - 8, 2), Cromo, CromoOscuro);
            }
            v.Forma(L.Redondeado(x, y, w, 8, 3), Cromo, CromoOscuro);
            v.Forma(L.Redondeado(x, y + h - 8, w, 8, 3), Cromo, CromoOscuro);
            if (!rota) v.Neon(Pts(x + 4, y + 1, x + w - 4, y + 1), 0.9f, luz);
        }

        // ================================================================ satisfaccion: el cartel de estrellas

        private static List<Vector2> Estrella(float cx, float cy, float r) {
            var p = new List<Vector2>();
            for (var i = 0; i < 10; i++) {
                var a = Mathf.Deg2Rad * (-90 + i * 36);
                var rr = i % 2 == 0 ? r : r * 0.45f;
                p.Add(new Vector2(cx + Mathf.Cos(a) * rr, cy + Mathf.Sin(a) * rr));
            }
            return p;
        }

        /// <summary>Cartel Googie de cinco estrellas de neon: 'encendidas' de 0 a 5.</summary>
        private static void CartelDeEstrellas(Tizador v, float x, float y, int encendidas, bool parpadea = false) {
            v.Forma(new RutaDeCurva(x, y + 6).Q(x, y, x + 8, y).L(x + 214, y - 8).Q(x + 222, y - 8, x + 222, y).L(x + 218, y + 44)
                .Q(x + 217, y + 50, x + 210, y + 50).L(x + 6, y + 54).Q(x, y + 54, x, y + 48).Puntos(), H(0x12081A), SiluetaFuera);
            for (var i = 0; i < 5; i++) {
                float cx = x + 26 + i * 42, cy = y + 24 - i * 1.5f;
                if (i < encendidas) {
                    var h = v.Halo(cx, cy, 30, A(Sol, 0.45f));
                    var e = v.FormaDeLuz(Estrella(cx, cy, 14), A(Sol, 0.95f));
                    v.Forma(Estrella(cx, cy, 6), BombillaCentro);
                    if (parpadea) { v.Parpadeo(h, 0.1f, i * 0.4f); v.Parpadeo(e, 0.25f, i * 0.4f); }
                } else v.Forma(Estrella(cx, cy, 14), H(0x2A1830));
            }
        }

        // ================================================================ moral: decisiones y la puerta

        /// <summary>Placa remachada con una decision y su lampara (menta = asi; coral = asi no).</summary>
        private static void PlacaDeDecision(Tizador v, float x, float y, float w, float h, string texto, bool bien) {
            var c = bien ? Menta : Coral;
            v.Forma(L.Redondeado(x, y, w, h, 6), H(0x2B3335), H(0x161B1D));
            foreach (var px in new[] { x + 7, x + w - 7 }) foreach (var py in new[] { y + 7, y + h - 7 }) v.Forma(L.Elipse(px, py, 2, 2, 8), H(0x5B625F));
            v.Rotulo(x + 20, y + h / 2, texto, 15, bien ? Tinta : A(Tinta, 0.75f), -1);
            v.Halo(x + w - 26, y + h / 2, 20, A(c, 0.5f));
            v.Forma(L.Elipse(x + w - 26, y + h / 2, 6, 6, 14), BombillaCentro, c);
        }

        /// <summary>Puerta del bunker entreabierta: la luz de fuera entra en cuña por el suelo.</summary>
        private static void PuertaEntreabierta(Tizador v, float x, float yTop, float w, float suelo) {
            var hueco = new RutaDeCurva(x, suelo).L(x, yTop + w / 2).C(x, yTop, x + w, yTop, x + w, yTop + w / 2).L(x + w, suelo).Puntos();
            v.Forma(L.Mover(hueco, 0, 0, 1.12f, x + w / 2, suelo), H(0x46555A), H(0x1A2428));
            v.FormaDeLuz(hueco, A(Ambar, 0.55f));
            v.Halo(x + w / 2, (yTop + suelo) / 2, w, (suelo - yTop) * 0.6f, A(Ambar, 0.3f));
            v.FormaDeLuz(Pts(x, suelo, x + w, suelo, x + w - 70, 300, x - 30, 300), A(Ambar, 0.18f));
            // La hoja abierta hacia dentro, en escorzo.
            v.Forma(Pts(x + w, yTop + w * 0.3f, x + w + 26, yTop + w * 0.1f, x + w + 26, suelo + 6, x + w, suelo), H(0x3A464B), H(0x161D20));
            v.Forma(L.Elipse(x + w + 20, (yTop + suelo) / 2 + 10, 2.5f, 4, 10), Cromo);
        }
    }
}
