using System.Collections.Generic;
using Nexus.Unity.Tema;
using UnityEngine;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>
    /// Los dibujos del edificio del cliente, por codigo con DibujoUI (origen arriba a la izquierda, y hacia abajo): los
    /// bichos de cada tipo de incidencia, los iconos de cada piso, las redes que los frenan, las monedas de horas, el
    /// escudo de lo critico, la caja de «la version nueva» y la gente del cliente.
    /// Rehechos en 2026-11-15 con el lenguaje del bunker: siluetas macizas en dos tonos (cara clara y cara de sombra), trazo
    /// grueso y parejo, el mostaza solo para avisar y el cian solo para informar. Estaticos y sin tema: los colores llegan
    /// por parametro. Todo se dibuja centrado en (cx, cy).
    /// </summary>
    public static class DibujosDelEdificio {
        private static readonly Color Mostaza = new Color(0.86f, 0.68f, 0.18f);
        private static readonly Color Nada = new Color(0, 0, 0, 0);

        private static float G(float esc, float basePx) { return basePx * Mathf.Max(0.8f, esc); }

        // ---------------------------------------------------------------- los bichos

        /// <summary>Un bicho de la forma pedida. 'esc' = 1 mide unos 30 px.</summary>
        public static void Bicho(DibujoUI d, string forma, float cx, float cy, float esc, Color c, float alfa = 1f) {
            var claro = new Color(c.r, c.g, c.b, alfa);
            var oscuro = new Color(c.r * 0.4f, c.g * 0.4f, c.b * 0.4f, alfa);
            switch (forma) {
                case "cable": {
                    Cuerpo(d, cx, cy, esc, claro, oscuro);
                    // El cable suelto: una cola gruesa que serpentea y acaba en un enchufe.
                    var cola = new List<Vector2>();
                    for (var i = 0; i <= 8; i++) cola.Add(new Vector2(cx - 8 * esc - i * 3.2f * esc, cy + 6 * esc + Mathf.Sin(i * 1.3f) * 4.5f * esc));
                    d.Polilinea(cola, oscuro, G(esc, 3.6f));
                    var fin = cola[cola.Count - 1];
                    d.Rect(fin.x - 6 * esc, fin.y - 4 * esc, 8 * esc, 8 * esc, oscuro, G(esc, 2.4f), Mostaza, false, 1);
                    break;
                }
                case "bloqueo": {
                    Cuerpo(d, cx, cy, esc * 1.35f, claro, oscuro);
                    // La barrera de obra que atraviesa el paso: barra con franjas de peligro.
                    var by = cy - 21 * esc;
                    d.Rect(cx - 20 * esc, by - 3.5f * esc, 40 * esc, 7 * esc, oscuro, G(esc, 2.4f), Color.white, false, 1);
                    for (var k = -2; k <= 2; k++)
                        d.Poligono(new List<Vector2> { new Vector2(cx + k * 8 * esc - 3 * esc, by - 3 * esc), new Vector2(cx + k * 8 * esc + 1.5f * esc, by - 3 * esc), new Vector2(cx + k * 8 * esc - 1.5f * esc, by + 3 * esc), new Vector2(cx + k * 8 * esc - 6 * esc, by + 3 * esc) }, Mostaza);
                    break;
                }
                case "fantasma": {
                    var f = new Color(c.r, c.g, c.b, 0.85f * alfa);
                    var sombra = new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, 0.85f * alfa);
                    var cuerpo = new List<Vector2> { new Vector2(cx - 10 * esc, cy + 12 * esc), new Vector2(cx - 12 * esc, cy - 2 * esc), new Vector2(cx - 6 * esc, cy - 12 * esc), new Vector2(cx, cy - 15 * esc),
                                                   new Vector2(cx + 6 * esc, cy - 12 * esc), new Vector2(cx + 12 * esc, cy - 2 * esc), new Vector2(cx + 10 * esc, cy + 12 * esc) };
                    d.Poligono(cuerpo, f);
                    d.Poligono(new List<Vector2> { new Vector2(cx + 3 * esc, cy - 13 * esc), new Vector2(cx + 6 * esc, cy - 12 * esc), new Vector2(cx + 12 * esc, cy - 2 * esc), new Vector2(cx + 10 * esc, cy + 12 * esc), new Vector2(cx + 3 * esc, cy + 12 * esc) }, sombra);
                    // El dobladillo en dientes de sierra (macizo, no una onda fina).
                    for (var i = 0; i < 4; i++) {
                        var x0 = cx - 10 * esc + i * 5f * esc;
                        d.Poligono(new List<Vector2> { new Vector2(x0, cy + 11 * esc), new Vector2(x0 + 5 * esc, cy + 11 * esc), new Vector2(x0 + 2.5f * esc, cy + 17 * esc) }, i >= 3 ? sombra : f);
                    }
                    d.Circulo(cx - 4 * esc, cy - 4 * esc, 2.8f * esc, Color.white, 0, Color.white);
                    d.Circulo(cx + 4 * esc, cy - 4 * esc, 2.8f * esc, Color.white, 0, Color.white);
                    d.Circulo(cx - 4 * esc, cy - 3.5f * esc, 1.1f * esc, oscuro, 0, oscuro);
                    d.Circulo(cx + 4 * esc, cy - 3.5f * esc, 1.1f * esc, oscuro, 0, oscuro);
                    break;
                }
                case "ganzua": {
                    Cuerpo(d, cx, cy, esc, claro, oscuro);
                    // El antifaz y la ganzua.
                    d.Rect(cx - 8.5f * esc, cy - 9.5f * esc, 17 * esc, 5.5f * esc, Color.black, 0, new Color(0.03f, 0.04f, 0.05f, alfa), false, 2);
                    d.Linea(cx + 8 * esc, cy + 4 * esc, cx + 17 * esc, cy - 4 * esc, Mostaza, G(esc, 3.2f));
                    d.Linea(cx + 17 * esc, cy - 4 * esc, cx + 20 * esc, cy - 1 * esc, Mostaza, G(esc, 3.2f));
                    break;
                }
                case "enjambre": {
                    for (var k = 0; k < 3; k++) Cuerpo(d, cx + (k - 1) * 12 * esc, cy + (k % 2 == 0 ? 4 : -5) * esc, esc * 0.62f, claro, oscuro);
                    break;
                }
                default: {   // chinche
                    Cuerpo(d, cx, cy, esc, claro, oscuro);
                    // La chispa del cortocircuito, en mostaza (avisa).
                    d.Polilinea(new List<Vector2> { new Vector2(cx + 4 * esc, cy - 14 * esc), new Vector2(cx + 8 * esc, cy - 19 * esc), new Vector2(cx + 5 * esc, cy - 19 * esc), new Vector2(cx + 10 * esc, cy - 26 * esc) },
                                new Color(Mostaza.r, Mostaza.g, Mostaza.b, alfa), G(esc, 3.4f));
                    break;
                }
            }
        }

        /// <summary>El cuerpo del bicho: caparazon de dos tonos (la mitad de la sombra a la derecha), cabeza, ojos y patas gruesas.</summary>
        private static void Cuerpo(DibujoUI d, float cx, float cy, float esc, Color claro, Color oscuro) {
            var g = G(esc, 3.2f);
            // Las patas, detras del caparazon.
            foreach (var s in new[] { -1, 1 }) {
                foreach (var dy in new[] { -3, 3, 9 }) d.Linea(cx + s * 8 * esc, cy + dy * esc, cx + s * 15.5f * esc, cy + (dy - 4) * esc, oscuro, g);
                d.Linea(cx + s * 2.5f * esc, cy - 15 * esc, cx + s * 7 * esc, cy - 23 * esc, oscuro, G(esc, 2.6f));
            }
            d.Elipse(cx, cy + 2 * esc, 9.5f * esc, 11.5f * esc, oscuro, g, claro);
            // La cara de sombra: la mitad derecha del caparazon.
            var mitad = new List<Vector2>();
            for (var i = 0; i <= 10; i++) { var a = Mathf.Lerp(-Mathf.PI / 2, Mathf.PI / 2, i / 10f); mitad.Add(new Vector2(cx + Mathf.Cos(a) * 8.5f * esc, cy + 2 * esc + Mathf.Sin(a) * 10.5f * esc)); }
            d.Poligono(mitad, new Color(oscuro.r, oscuro.g, oscuro.b, oscuro.a * 0.55f));
            d.Circulo(cx, cy - 11 * esc, 5.2f * esc, oscuro, g, claro);
            d.Circulo(cx - 2 * esc, cy - 12 * esc, 1.5f * esc, Color.white, 0, Color.white);
            d.Circulo(cx + 2 * esc, cy - 12 * esc, 1.5f * esc, Color.white, 0, Color.white);
            d.Linea(cx, cy - 6 * esc, cx, cy + 12 * esc, oscuro, G(esc, 2.2f));
        }

        // ---------------------------------------------------------------- redes, monedas y escudos

        /// <summary>Una red: un disco con su malla. Encendida, de color con la cara de sombra; apagada, solo el contorno tenue.</summary>
        public static void Red(DibujoUI d, float cx, float cy, float r, Color c, bool encendida) {
            var a = encendida ? 1f : 0.28f;
            var col = new Color(c.r, c.g, c.b, a);
            d.Circulo(cx, cy, r, col, 3.2f, encendida ? new Color(c.r, c.g, c.b, 0.3f) : (Color?)null);
            if (encendida) d.Elipse(cx + r * 0.3f, cy + r * 0.25f, r * 0.62f, r * 0.62f, Nada, 0, new Color(0, 0, 0, 0.16f));
            d.Circulo(cx, cy, r * 0.5f, new Color(c.r, c.g, c.b, a * 0.9f), 2.4f);
            for (var k = 0; k < 4; k++) {
                var ang = k * Mathf.PI / 4f;
                d.Linea(cx - Mathf.Cos(ang) * r, cy - Mathf.Sin(ang) * r, cx + Mathf.Cos(ang) * r, cy + Mathf.Sin(ang) * r, new Color(c.r, c.g, c.b, a * 0.8f), 2.2f);
            }
            if (encendida) d.Circulo(cx, cy, 3f, col, 0, col);   // el nudo central
        }

        /// <summary>La moneda de horas: reborde grueso, cara con tapa y una «H» estarcida.</summary>
        public static void Moneda(DibujoUI d, float cx, float cy, float r) {
            var oro = new Color(1f, 0.8f, 0.25f);
            var borde = new Color(0.5f, 0.33f, 0.04f);
            d.Circulo(cx, cy, r, borde, 3f, oro);
            d.Elipse(cx + r * 0.22f, cy + r * 0.22f, r * 0.7f, r * 0.7f, Nada, 0, new Color(0.8f, 0.58f, 0.1f, 0.55f));
            d.Circulo(cx, cy, r * 0.66f, new Color(borde.r, borde.g, borde.b, 0.85f), 2.2f);
            var h = r * 0.3f; var g = Mathf.Max(2f, r * 0.14f);
            d.Linea(cx - h, cy - h, cx - h, cy + h, borde, g); d.Linea(cx + h, cy - h, cx + h, cy + h, borde, g); d.Linea(cx - h, cy, cx + h, cy, borde, g);
        }

        public static void Escudo(DibujoUI d, float cx, float cy, float esc, Color c) {
            var p = new List<Vector2> { new Vector2(cx - 10 * esc, cy - 11 * esc), new Vector2(cx + 10 * esc, cy - 11 * esc), new Vector2(cx + 10 * esc, cy + 2 * esc), new Vector2(cx, cy + 13 * esc), new Vector2(cx - 10 * esc, cy + 2 * esc) };
            d.Poligono(p, new Color(c.r, c.g, c.b, 0.55f));
            d.Poligono(new List<Vector2> { new Vector2(cx, cy - 11 * esc), new Vector2(cx + 10 * esc, cy - 11 * esc), new Vector2(cx + 10 * esc, cy + 2 * esc), new Vector2(cx, cy + 13 * esc) }, new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, 0.8f));
            var cerrado = new List<Vector2>(p) { p[0] };
            d.Polilinea(cerrado, c, 3.4f);
            d.Linea(cx, cy - 6 * esc, cx, cy + 2 * esc, Color.white, 3f);
            d.Circulo(cx, cy + 6.5f * esc, 1.8f * esc, Color.white, 0, Color.white);
        }

        /// <summary>El triangulo de alerta: borde oscuro grueso, cara mostaza y el signo en negro (cartel de obra).</summary>
        public static void Alerta(DibujoUI d, float cx, float cy, float esc, Color c) {
            var p = new List<Vector2> { new Vector2(cx, cy - 14 * esc), new Vector2(cx + 14 * esc, cy + 11 * esc), new Vector2(cx - 14 * esc, cy + 11 * esc) };
            d.Poligono(p, new Color(c.r, c.g, c.b, 0.95f));
            var cerrado = new List<Vector2>(p) { p[0] };
            d.Polilinea(cerrado, new Color(0.08f, 0.07f, 0.04f), 3.6f);
            var tinta = new Color(0.08f, 0.07f, 0.04f);
            d.Linea(cx, cy - 4 * esc, cx, cy + 3 * esc, tinta, 3.2f);
            d.Circulo(cx, cy + 7 * esc, 1.7f * esc, tinta, 0, tinta);
        }

        // ---------------------------------------------------------------- los iconos de cada piso

        /// <summary>El icono del tipo de prueba, en un disco de unos 56 px (placa remachada, trazo grueso).</summary>
        public static void IconoDePiso(DibujoUI d, string tipoId, float cx, float cy, Color c) {
            d.Circulo(cx, cy, 27, new Color(c.r, c.g, c.b, 0.85f), 3.2f, new Color(c.r * 0.22f, c.g * 0.22f, c.b * 0.22f, 0.95f));
            switch (tipoId) {
                case "integracion":   // dos piezas unidas por un enlace con chispa
                    d.Rect(cx - 19, cy - 8, 12, 16, c, 3f, new Color(c.r, c.g, c.b, 0.35f), false, 2);
                    d.Rect(cx + 7, cy - 8, 12, 16, c, 3f, new Color(c.r, c.g, c.b, 0.35f), false, 2);
                    d.Linea(cx - 7, cy, cx + 7, cy, c, 3.2f);
                    d.Polilinea(new List<Vector2> { new Vector2(cx - 3, cy - 7), new Vector2(cx + 1, cy - 1), new Vector2(cx - 1, cy - 1), new Vector2(cx + 3, cy + 7) }, Mostaza, 3f);
                    break;
                case "seguridad":
                    DibujosDelPuente.Candado(d, cx, cy, c, 1.05f);
                    break;
                case "rendimiento": {   // un manometro: arco con marcas y aguja
                    var arco = new List<Vector2>();
                    for (var i = 0; i <= 14; i++) { var a = Mathf.Lerp(Mathf.PI * 0.85f, Mathf.PI * 2.15f, i / 14f); arco.Add(new Vector2(cx + Mathf.Cos(a) * 15, cy + 4 + Mathf.Sin(a) * 15)); }
                    d.Polilinea(arco, c, 3.6f);
                    d.Linea(cx, cy + 4, cx + 9, cy - 7, Mostaza, 3.6f);
                    d.Circulo(cx, cy + 4, 3.2f, c, 0, c);
                    break;
                }
                case "regresion": {   // una flecha circular que vuelve
                    var arco = new List<Vector2>();
                    for (var i = 0; i <= 14; i++) { var a = Mathf.Lerp(Mathf.PI * 0.3f, Mathf.PI * 2.1f, i / 14f); arco.Add(new Vector2(cx + Mathf.Cos(a) * 13, cy + Mathf.Sin(a) * 13)); }
                    d.Polilinea(arco, c, 3.8f);
                    var f = arco[arco.Count - 1];
                    d.Poligono(new List<Vector2> { new Vector2(f.x - 7, f.y - 7), new Vector2(f.x + 7, f.y - 3), new Vector2(f.x - 1, f.y + 6) }, c);
                    break;
                }
                case "e2e": {   // el recorrido de punta a punta, con su bandera
                    d.Circulo(cx - 16, cy + 10, 3.6f, c, 0, c);
                    d.Polilinea(new List<Vector2> { new Vector2(cx - 16, cy + 10), new Vector2(cx - 5, cy + 2), new Vector2(cx + 2, cy + 9), new Vector2(cx + 12, cy - 6) }, c, 3.6f);
                    d.Linea(cx + 12, cy - 6, cx + 12, cy - 18, c, 3f);
                    d.Poligono(new List<Vector2> { new Vector2(cx + 12, cy - 18), new Vector2(cx + 21, cy - 14), new Vector2(cx + 12, cy - 10) }, Mostaza);
                    break;
                }
                default: {   // unitarias: un chip
                    d.Rect(cx - 10, cy - 10, 20, 20, c, 3f, new Color(c.r, c.g, c.b, 0.4f), false, 3);
                    d.Rect(cx - 4, cy - 4, 8, 8, c, 0, c, false, 1);
                    for (var k = -1; k <= 1; k++) {
                        d.Linea(cx + k * 7, cy - 10, cx + k * 7, cy - 17, c, 3f); d.Linea(cx + k * 7, cy + 10, cx + k * 7, cy + 17, c, 3f);
                        d.Linea(cx - 10, cy + k * 7, cx - 17, cy + k * 7, c, 3f); d.Linea(cx + 10, cy + k * 7, cx + 17, cy + k * 7, c, 3f);
                    }
                    break;
                }
            }
        }

        // ---------------------------------------------------------------- el entorno

        /// <summary>La caja de «la version nueva» de la que salen los bichos: carton en dos tonos, cinta y un rotulo de «arriba». Mide 92×70, centrada.</summary>
        public static void CajaDeVersion(DibujoUI d, float cx, float cy, Color c) {
            var x = cx - 46; var y = cy - 35;
            var borde = new Color(0.3f, 0.19f, 0.08f);
            d.Rect(x, y + 12, 92, 58, borde, 3.6f, new Color(0.66f, 0.47f, 0.24f), false, 3);
            d.Poligono(new List<Vector2> { new Vector2(x + 58, y + 14), new Vector2(x + 90, y + 14), new Vector2(x + 90, y + 68), new Vector2(x + 58, y + 68) }, new Color(0.5f, 0.34f, 0.16f));
            d.Rect(x - 5, y + 3, 102, 15, borde, 3.6f, new Color(0.76f, 0.55f, 0.28f), false, 2);
            d.Linea(cx, y + 3, cx, y + 70, new Color(c.r, c.g, c.b, 0.95f), 9f);
            // El rotulo de la caja: un cuadro con flecha hacia arriba.
            d.Rect(x + 8, y + 32, 24, 22, borde, 2.4f, new Color(0.95f, 0.9f, 0.75f), false, 1);
            d.Polilinea(new List<Vector2> { new Vector2(x + 14, y + 45), new Vector2(x + 20, y + 37), new Vector2(x + 26, y + 45) }, borde, 3f);
            d.Linea(x + 20, y + 37, x + 20, y + 51, borde, 3f);
        }

        /// <summary>La gente del cliente: dos personas y, a su lado, el camion de la garita. Mide unos 230×90 (x, y = esquina de arriba a la izquierda).</summary>
        public static void GenteDelCliente(DibujoUI d, float x, float y, Color persona, Color camion) {
            // Siluetas macizas con credencial al cuello (opcion B: se cuenta por el objeto, sin cara).
            Silueta(d, x + 20, y + 24, persona, Mostaza);
            Silueta(d, x + 62, y + 30, new Color(persona.r * 0.8f, persona.g * 0.8f, persona.b * 0.8f, persona.a), new Color(0.35f, 0.82f, 0.95f));
            DibujosDelPuente.Camion(d, x + 98, y + 14, camion, new Color(1f, 0.93f, 0.6f));
        }

        private static void Silueta(DibujoUI d, float cx, float cy, Color c, Color credencial) {
            var oscuro = new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, c.a);
            d.Circulo(cx, cy - 18, 8, oscuro, 3f, c);
            d.Poligono(new List<Vector2> { new Vector2(cx - 15, cy + 22), new Vector2(cx - 12, cy - 6), new Vector2(cx + 12, cy - 6), new Vector2(cx + 15, cy + 22) }, c);
            d.Poligono(new List<Vector2> { new Vector2(cx + 3, cy - 6), new Vector2(cx + 12, cy - 6), new Vector2(cx + 15, cy + 22), new Vector2(cx + 3, cy + 22) }, oscuro);
            d.Polilinea(new List<Vector2> { new Vector2(cx - 12, cy + 22), new Vector2(cx - 12, cy - 6), new Vector2(cx + 12, cy - 6), new Vector2(cx + 15, cy + 22) }, oscuro, 3f);
            d.Rect(cx - 5, cy + 1, 10, 12, oscuro, 1.6f, credencial, false, 1);
        }

        /// <summary>La antena y la bandera de la azotea (la luz de la antena la anima la pantalla).</summary>
        public static void Antena(DibujoUI d, float x, float yBase, Color c, Color bandera) {
            d.Linea(x, yBase, x, yBase - 44, c, 4.4f);
            d.Linea(x - 10, yBase - 30, x + 10, yBase - 30, c, 3.4f);
            d.Linea(x - 6, yBase - 38, x + 6, yBase - 38, c, 3f);
            d.Linea(x + 70, yBase, x + 70, yBase - 34, c, 3.6f);
            d.Poligono(new List<Vector2> { new Vector2(x + 71, yBase - 34), new Vector2(x + 98, yBase - 27), new Vector2(x + 71, yBase - 19) }, bandera);
            d.Poligono(new List<Vector2> { new Vector2(x + 71, yBase - 27), new Vector2(x + 98, yBase - 27), new Vector2(x + 71, yBase - 19) }, new Color(0, 0, 0, 0.22f));
        }
    }
}
