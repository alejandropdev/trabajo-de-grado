using System.Collections.Generic;
using Nexus.Unity.Tema;
using UnityEngine;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>
    /// Los dibujos del edificio del cliente (2026-11-11), todos por codigo con DibujoUI (origen arriba a la izquierda, y
    /// hacia abajo): los bichos de cada tipo de incidencia (chinche con chispa, bicho con cable suelto, bicho que
    /// bloquea el paso, fantasma, bicho con ganzua, enjambre), los iconos de cada piso, las redes que los frenan, las
    /// monedas de horas, el escudo de lo critico, la caja de «la version nueva» y la gente del cliente.
    /// Estaticos y sin tema: los colores llegan por parametro. Todo se dibuja centrado en (cx, cy).
    /// </summary>
    public static class DibujosDelEdificio {
        // ---------------------------------------------------------------- los bichos

        /// <summary>Un bicho de la forma pedida. 'esc' = 1 mide unos 30 px.</summary>
        public static void Bicho(DibujoUI d, string forma, float cx, float cy, float esc, Color c, float alfa = 1f) {
            var relleno = new Color(c.r, c.g, c.b, 0.55f * alfa);
            var filo = new Color(c.r, c.g, c.b, alfa);
            switch (forma) {
                case "cable": {
                    Cuerpo(d, cx, cy, esc, relleno, filo);
                    // El cable suelto: una cola que serpentea y acaba en un enchufe.
                    var cola = new List<Vector2>();
                    for (var i = 0; i <= 8; i++) cola.Add(new Vector2(cx - 8 * esc - i * 3.2f * esc, cy + 6 * esc + Mathf.Sin(i * 1.3f) * 4.5f * esc));
                    d.Polilinea(cola, filo, 2.2f * Mathf.Max(0.7f, esc));
                    var fin = cola[cola.Count - 1];
                    d.Rect(fin.x - 5 * esc, fin.y - 3.5f * esc, 7 * esc, 7 * esc, filo, 1.6f, relleno, false, 1);
                    break;
                }
                case "bloqueo": {
                    Cuerpo(d, cx, cy, esc * 1.35f, relleno, filo);
                    // La barrera que atraviesa el paso.
                    d.Linea(cx - 18 * esc, cy - 20 * esc, cx + 18 * esc, cy - 20 * esc, filo, 3f * Mathf.Max(0.8f, esc));
                    for (var k = -2; k <= 2; k++) d.Linea(cx + k * 7 * esc - 3 * esc, cy - 17 * esc, cx + k * 7 * esc + 3 * esc, cy - 23 * esc, new Color(1, 1, 1, 0.7f * alfa), 1.8f);
                    break;
                }
                case "fantasma": {
                    var f = new Color(c.r, c.g, c.b, 0.45f * alfa);
                    d.Poligono(new List<Vector2> { new Vector2(cx - 10 * esc, cy + 12 * esc), new Vector2(cx - 12 * esc, cy - 2 * esc), new Vector2(cx - 6 * esc, cy - 12 * esc), new Vector2(cx, cy - 15 * esc),
                                                   new Vector2(cx + 6 * esc, cy - 12 * esc), new Vector2(cx + 12 * esc, cy - 2 * esc), new Vector2(cx + 10 * esc, cy + 12 * esc) }, f);
                    var onda = new List<Vector2>();
                    for (var i = 0; i <= 6; i++) onda.Add(new Vector2(cx - 10 * esc + i * 20f * esc / 6f, cy + 12 * esc + (i % 2 == 0 ? 3f : -2f) * esc));
                    d.Polilinea(onda, filo, 2f * Mathf.Max(0.7f, esc));
                    d.Linea(cx - 10 * esc, cy + 12 * esc, cx - 12 * esc, cy - 2 * esc, filo, 2f * Mathf.Max(0.7f, esc));
                    d.Linea(cx + 10 * esc, cy + 12 * esc, cx + 12 * esc, cy - 2 * esc, filo, 2f * Mathf.Max(0.7f, esc));
                    d.Circulo(cx - 4 * esc, cy - 3 * esc, 2.3f * esc, Color.white, 0, Color.white);
                    d.Circulo(cx + 4 * esc, cy - 3 * esc, 2.3f * esc, Color.white, 0, Color.white);
                    break;
                }
                case "ganzua": {
                    Cuerpo(d, cx, cy, esc, relleno, filo);
                    // El antifaz y la ganzua.
                    d.Rect(cx - 8 * esc, cy - 9 * esc, 16 * esc, 5 * esc, Color.black, 1.5f, new Color(0, 0, 0, 0.85f * alfa), false, 2);
                    d.Linea(cx + 8 * esc, cy + 4 * esc, cx + 17 * esc, cy - 4 * esc, filo, 2f * Mathf.Max(0.8f, esc));
                    d.Linea(cx + 17 * esc, cy - 4 * esc, cx + 20 * esc, cy - 1 * esc, filo, 2f * Mathf.Max(0.8f, esc));
                    break;
                }
                case "enjambre": {
                    for (var k = 0; k < 3; k++) Cuerpo(d, cx + (k - 1) * 12 * esc, cy + (k % 2 == 0 ? 4 : -5) * esc, esc * 0.62f, relleno, filo);
                    break;
                }
                default: {   // chinche
                    Cuerpo(d, cx, cy, esc, relleno, filo);
                    // La chispa del cortocircuito.
                    d.Polilinea(new List<Vector2> { new Vector2(cx + 4 * esc, cy - 14 * esc), new Vector2(cx + 8 * esc, cy - 19 * esc), new Vector2(cx + 5 * esc, cy - 19 * esc), new Vector2(cx + 10 * esc, cy - 26 * esc) },
                                new Color(1f, 0.9f, 0.4f, alfa), 2.2f * Mathf.Max(0.8f, esc));
                    break;
                }
            }
        }

        private static void Cuerpo(DibujoUI d, float cx, float cy, float esc, Color relleno, Color filo) {
            d.Elipse(cx, cy + 2 * esc, 9 * esc, 11 * esc, filo, 2f * Mathf.Max(0.7f, esc), relleno);
            d.Circulo(cx, cy - 11 * esc, 5 * esc, filo, 2f * Mathf.Max(0.7f, esc), relleno);
            foreach (var s in new[] { -1, 1 }) {
                foreach (var dy in new[] { -3, 3, 9 }) d.Linea(cx + s * 8 * esc, cy + dy * esc, cx + s * 15 * esc, cy + (dy - 3) * esc, filo, 1.8f * Mathf.Max(0.7f, esc));
                d.Linea(cx + s * 2.5f * esc, cy - 15 * esc, cx + s * 6 * esc, cy - 21 * esc, filo, 1.6f * Mathf.Max(0.7f, esc));
            }
            d.Linea(cx, cy - 6 * esc, cx, cy + 12 * esc, new Color(0, 0, 0, 0.35f), 1.4f);
        }

        // ---------------------------------------------------------------- redes, monedas y escudos

        /// <summary>Una red: un circulo con su telaraña. Encendida, de color; apagada, tenue.</summary>
        public static void Red(DibujoUI d, float cx, float cy, float r, Color c, bool encendida) {
            var a = encendida ? 1f : 0.25f;
            var col = new Color(c.r, c.g, c.b, a);
            d.Circulo(cx, cy, r, col, 2f, encendida ? new Color(c.r, c.g, c.b, 0.18f) : (Color?)null);
            d.Circulo(cx, cy, r * 0.5f, col, 1.4f);
            for (var k = 0; k < 4; k++) {
                var ang = k * Mathf.PI / 4f;
                d.Linea(cx - Mathf.Cos(ang) * r, cy - Mathf.Sin(ang) * r, cx + Mathf.Cos(ang) * r, cy + Mathf.Sin(ang) * r, new Color(c.r, c.g, c.b, a * 0.8f), 1.3f);
            }
        }

        public static void Moneda(DibujoUI d, float cx, float cy, float r) {
            var oro = new Color(1f, 0.8f, 0.25f);
            d.Circulo(cx, cy, r, new Color(0.62f, 0.42f, 0.05f), 2f, oro);
            d.Circulo(cx, cy, r * 0.62f, new Color(0.62f, 0.42f, 0.05f, 0.8f), 1.4f);
            d.Linea(cx - r * 0.2f, cy - r * 0.35f, cx - r * 0.2f, cy + r * 0.35f, new Color(0.62f, 0.42f, 0.05f), 1.6f);
        }

        public static void Escudo(DibujoUI d, float cx, float cy, float esc, Color c) {
            var p = new List<Vector2> { new Vector2(cx - 9 * esc, cy - 10 * esc), new Vector2(cx + 9 * esc, cy - 10 * esc), new Vector2(cx + 9 * esc, cy + 2 * esc), new Vector2(cx, cy + 12 * esc), new Vector2(cx - 9 * esc, cy + 2 * esc) };
            d.Poligono(p, new Color(c.r, c.g, c.b, 0.3f));
            var cerrado = new List<Vector2>(p) { p[0] };
            d.Polilinea(cerrado, c, 2.2f);
            d.Linea(cx, cy - 5 * esc, cx, cy + 3 * esc, c, 2.2f);
            d.Circulo(cx, cy + 6.5f * esc, 1.3f * esc, c, 0, c);
        }

        public static void Alerta(DibujoUI d, float cx, float cy, float esc, Color c) {
            var p = new List<Vector2> { new Vector2(cx, cy - 13 * esc), new Vector2(cx + 13 * esc, cy + 10 * esc), new Vector2(cx - 13 * esc, cy + 10 * esc) };
            d.Poligono(p, new Color(c.r, c.g, c.b, 0.9f));
            d.Linea(cx, cy - 4 * esc, cx, cy + 3 * esc, Color.white, 2.6f);
            d.Circulo(cx, cy + 6.5f * esc, 1.4f * esc, Color.white, 0, Color.white);
        }

        // ---------------------------------------------------------------- los iconos de cada piso

        /// <summary>El icono del tipo de prueba, en un disco de unos 56 px.</summary>
        public static void IconoDePiso(DibujoUI d, string tipoId, float cx, float cy, Color c) {
            d.Circulo(cx, cy, 27, new Color(c.r, c.g, c.b, 0.7f), 2f, new Color(c.r, c.g, c.b, 0.14f));
            switch (tipoId) {
                case "integracion":   // dos piezas unidas por un enlace con chispa
                    d.Rect(cx - 18, cy - 7, 11, 14, c, 2f, null, false, 2);
                    d.Rect(cx + 7, cy - 7, 11, 14, c, 2f, null, false, 2);
                    d.Linea(cx - 7, cy, cx + 7, cy, c, 2.4f);
                    d.Polilinea(new List<Vector2> { new Vector2(cx - 3, cy - 6), new Vector2(cx + 1, cy - 1), new Vector2(cx - 1, cy - 1), new Vector2(cx + 3, cy + 6) }, new Color(1f, 0.9f, 0.4f), 2f);
                    break;
                case "seguridad":
                    DibujosDelPuente.Candado(d, cx, cy, c, 1.05f);
                    break;
                case "rendimiento": {   // un reloj de carga: arco y aguja
                    var arco = new List<Vector2>();
                    for (var i = 0; i <= 14; i++) { var a = Mathf.Lerp(Mathf.PI * 0.85f, Mathf.PI * 2.15f, i / 14f); arco.Add(new Vector2(cx + Mathf.Cos(a) * 15, cy + 4 + Mathf.Sin(a) * 15)); }
                    d.Polilinea(arco, c, 2.4f);
                    d.Linea(cx, cy + 4, cx + 9, cy - 6, new Color(1f, 0.9f, 0.4f), 2.6f);
                    d.Circulo(cx, cy + 4, 2.2f, c, 0, c);
                    break;
                }
                case "regresion": {   // una flecha circular que vuelve
                    var arco = new List<Vector2>();
                    for (var i = 0; i <= 14; i++) { var a = Mathf.Lerp(Mathf.PI * 0.3f, Mathf.PI * 2.1f, i / 14f); arco.Add(new Vector2(cx + Mathf.Cos(a) * 13, cy + Mathf.Sin(a) * 13)); }
                    d.Polilinea(arco, c, 2.6f);
                    var f = arco[arco.Count - 1];
                    d.Polilinea(new List<Vector2> { new Vector2(f.x - 5, f.y - 6), f, new Vector2(f.x + 6, f.y - 3) }, c, 2.6f);
                    break;
                }
                case "e2e": {   // el recorrido de punta a punta, con su bandera
                    d.Circulo(cx - 16, cy + 10, 3, c, 0, c);
                    d.Polilinea(new List<Vector2> { new Vector2(cx - 16, cy + 10), new Vector2(cx - 5, cy + 2), new Vector2(cx + 2, cy + 9), new Vector2(cx + 12, cy - 6) }, c, 2.6f);
                    d.Linea(cx + 12, cy - 6, cx + 12, cy - 17, c, 2.2f);
                    d.Poligono(new List<Vector2> { new Vector2(cx + 12, cy - 17), new Vector2(cx + 20, cy - 14), new Vector2(cx + 12, cy - 11) }, c);
                    break;
                }
                default: {   // unitarias: un chip
                    d.Rect(cx - 10, cy - 10, 20, 20, c, 2.2f, new Color(c.r, c.g, c.b, 0.2f), false, 3);
                    for (var k = -1; k <= 1; k++) {
                        d.Linea(cx + k * 7, cy - 10, cx + k * 7, cy - 17, c, 2f); d.Linea(cx + k * 7, cy + 10, cx + k * 7, cy + 17, c, 2f);
                        d.Linea(cx - 10, cy + k * 7, cx - 17, cy + k * 7, c, 2f); d.Linea(cx + 10, cy + k * 7, cx + 17, cy + k * 7, c, 2f);
                    }
                    break;
                }
            }
        }

        // ---------------------------------------------------------------- el entorno

        /// <summary>La caja de «la version nueva» de la que salen los bichos: una caja con cinta y un lazo. Mide 92×70, centrada.</summary>
        public static void CajaDeVersion(DibujoUI d, float cx, float cy, Color c) {
            var x = cx - 46; var y = cy - 35;
            d.Rect(x, y + 12, 92, 58, new Color(0.45f, 0.3f, 0.15f), 3f, new Color(0.62f, 0.44f, 0.22f), false, 4);
            d.Rect(x - 4, y + 4, 100, 14, new Color(0.45f, 0.3f, 0.15f), 3f, new Color(0.7f, 0.5f, 0.26f), false, 3);
            d.Linea(cx, y + 4, cx, y + 70, new Color(c.r, c.g, c.b, 0.9f), 8f);
            d.Linea(x - 4, y + 11, x + 96, y + 11, new Color(c.r, c.g, c.b, 0.9f), 3f);
            for (var k = 0; k < 4; k++) d.Linea(x + 10 + k * 22, y + 62, x + 18 + k * 22, y + 70, new Color(0, 0, 0, 0.3f), 2f);
        }

        /// <summary>La gente del cliente: dos personas y, a su lado, el camion de la garita. Mide unos 230×90 (x, y = esquina de arriba a la izquierda).</summary>
        public static void GenteDelCliente(DibujoUI d, float x, float y, Color persona, Color camion) {
            d.Persona(x + 20, y + 22, persona, 3f);
            d.Persona(x + 62, y + 28, new Color(persona.r, persona.g, persona.b, 0.8f), 3f);
            DibujosDelPuente.Camion(d, x + 98, y + 14, camion, new Color(1f, 0.93f, 0.6f));
        }

        /// <summary>La antena y la bandera de la azotea (la luz de la antena la anima la pantalla).</summary>
        public static void Antena(DibujoUI d, float x, float yBase, Color c, Color bandera) {
            d.Linea(x, yBase, x, yBase - 44, c, 3f);
            d.Linea(x - 10, yBase - 30, x + 10, yBase - 30, c, 2.2f);
            d.Linea(x - 6, yBase - 38, x + 6, yBase - 38, c, 2f);
            d.Linea(x + 70, yBase, x + 70, yBase - 34, c, 2.4f);
            d.Poligono(new List<Vector2> { new Vector2(x + 71, yBase - 34), new Vector2(x + 98, yBase - 27), new Vector2(x + 71, yBase - 19) }, bandera);
        }
    }
}
