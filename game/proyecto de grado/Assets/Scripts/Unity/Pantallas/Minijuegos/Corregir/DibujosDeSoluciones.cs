using System.Collections.Generic;
using Nexus.Unity.Tema;
using UnityEngine;

namespace Nexus.Unity.Pantallas.Minijuegos.Corregir {
    /// <summary>
    /// La ilustracion de cada carta de solucion, dibujada por codigo con las primitivas de DibujoUI (sin texturas, como
    /// el resto de dibujos del juego). Cada una cabe en un cuadrado (x, y, lado) con origen arriba a la izquierda. La
    /// clave es el «icono» de la carta en el JSON; una clave que no se conoce dibuja un «?».
    /// </summary>
    public static class DibujosDeSoluciones {
        public const string EstaBien = "esta_bien";

        public static void Dibujar(DibujoUI d, string icono, float x, float y, float s, Color c, Color acento) {
            System.Func<float, float, Vector2> P = (fx, fy) => new Vector2(x + fx * s, y + fy * s);
            var g = Mathf.Max(2f, s * 0.045f);   // grosor de trazo
            System.Action<float, float, float, float> L = (ax, ay, bx, by) => d.Linea(x + ax * s, y + ay * s, x + bx * s, y + by * s, c, g);
            System.Action<float, float, float, float, Color, bool> R = (rx, ry, rw, rh, col, lleno) =>
                d.Rect(x + rx * s, y + ry * s, rw * s, rh * s, col, g, lleno ? (Color?)NexusTheme.Alfa(col, 0.18f) : null, false, s * 0.05f);
            System.Action<float, float, float, Color> C = (cx, cy, r, col) => d.Circulo(x + cx * s, y + cy * s, r * s, col, g);

            switch (icono) {
                case "acuerdo":   // dos bocadillos que se tocan y un check
                    R(0.08f, 0.14f, 0.46f, 0.32f, c, true);
                    R(0.46f, 0.40f, 0.46f, 0.32f, acento, true);
                    L(0.18f, 0.46f, 0.14f, 0.58f);
                    d.Polilinea(new List<Vector2> { P(0.58f, 0.56f), P(0.66f, 0.64f), P(0.82f, 0.48f) }, acento, g * 1.2f);
                    break;
                case "lista":   // una hoja con renglones y uno nuevo «+»
                    R(0.18f, 0.08f, 0.6f, 0.84f, c, true);
                    for (var i = 0; i < 3; i++) L(0.28f, 0.24f + i * 0.14f, 0.66f, 0.24f + i * 0.14f);
                    C(0.72f, 0.74f, 0.15f, acento);
                    d.Linea(x + 0.72f * s, y + 0.65f * s, x + 0.72f * s, y + 0.83f * s, acento, g);
                    d.Linea(x + 0.63f * s, y + 0.74f * s, x + 0.81f * s, y + 0.74f * s, acento, g);
                    break;
                case "puerta":   // una caja con una sola puerta y una flecha que entra por ella
                    R(0.34f, 0.12f, 0.58f, 0.76f, c, true);
                    R(0.34f, 0.38f, 0.1f, 0.24f, acento, true);
                    d.Flecha(x + 0.04f * s, y + 0.5f * s, x + 0.32f * s, y + 0.5f * s, acento, g);
                    break;
                case "adaptador":   // dos piezas de enchufe distintas con un traductor en medio
                    R(0.04f, 0.34f, 0.24f, 0.32f, c, false);
                    R(0.38f, 0.26f, 0.24f, 0.48f, acento, true);
                    d.Circulo(x + 0.84f * s, y + 0.5f * s, 0.14f * s, c, g);
                    L(0.28f, 0.5f, 0.38f, 0.5f);
                    L(0.62f, 0.5f, 0.70f, 0.5f);
                    break;
                case "candado":
                    R(0.2f, 0.44f, 0.6f, 0.46f, c, true);
                    d.Curva(P(0.32f, 0.44f), P(0.5f, -0.02f), P(0.68f, 0.44f), c, g, false);
                    C(0.5f, 0.62f, 0.06f, acento);
                    d.Linea(x + 0.5f * s, y + 0.66f * s, x + 0.5f * s, y + 0.78f * s, acento, g);
                    break;
                case "escudo":
                    d.Polilinea(new List<Vector2> { P(0.5f, 0.06f), P(0.86f, 0.2f), P(0.8f, 0.6f), P(0.5f, 0.94f), P(0.2f, 0.6f), P(0.14f, 0.2f), P(0.5f, 0.06f) }, c, g);
                    d.Polilinea(new List<Vector2> { P(0.34f, 0.5f), P(0.46f, 0.62f), P(0.68f, 0.38f) }, acento, g * 1.2f);
                    break;
                case "bitacora":   // un libro con un reloj
                    R(0.1f, 0.12f, 0.56f, 0.76f, c, true);
                    for (var i = 0; i < 3; i++) L(0.2f, 0.3f + i * 0.14f, 0.54f, 0.3f + i * 0.14f);
                    C(0.72f, 0.7f, 0.18f, acento);
                    d.Linea(x + 0.72f * s, y + 0.7f * s, x + 0.72f * s, y + 0.6f * s, acento, g);
                    d.Linea(x + 0.72f * s, y + 0.7f * s, x + 0.8f * s, y + 0.74f * s, acento, g);
                    break;
                case "ojo":   // un ojo tachado
                    d.Elipse(x + 0.5f * s, y + 0.5f * s, 0.4f * s, 0.22f * s, c, g);
                    C(0.5f, 0.5f, 0.1f, c);
                    d.Linea(x + 0.14f * s, y + 0.84f * s, x + 0.86f * s, y + 0.16f * s, acento, g * 1.3f);
                    break;
                case "ciclo":   // un circulo de flechas cortado por una tijera de luz
                    d.Curva(P(0.2f, 0.5f), P(0.2f, 0.12f), P(0.56f, 0.16f), c, g, true);
                    d.Curva(P(0.8f, 0.5f), P(0.8f, 0.88f), P(0.44f, 0.84f), c, g, true);
                    d.Linea(x + 0.2f * s, y + 0.8f * s, x + 0.8f * s, y + 0.2f * s, acento, g * 1.3f, true);
                    break;
                case "antena":   // una antena que emite ondas
                    L(0.5f, 0.5f, 0.5f, 0.92f);
                    L(0.32f, 0.92f, 0.68f, 0.92f);
                    C(0.5f, 0.44f, 0.07f, acento);
                    d.Curva(P(0.3f, 0.26f), P(0.22f, 0.44f), P(0.3f, 0.62f), acento, g, false);
                    d.Curva(P(0.7f, 0.26f), P(0.78f, 0.44f), P(0.7f, 0.62f), acento, g, false);
                    d.Curva(P(0.16f, 0.14f), P(0.02f, 0.44f), P(0.16f, 0.74f), c, g, false);
                    d.Curva(P(0.84f, 0.14f), P(0.98f, 0.44f), P(0.84f, 0.74f), c, g, false);
                    break;
                case "plano":   // un plano con cotas y un lapiz
                    R(0.08f, 0.14f, 0.66f, 0.72f, c, true);
                    R(0.18f, 0.26f, 0.22f, 0.18f, c, false);
                    R(0.46f, 0.52f, 0.18f, 0.2f, c, false);
                    d.Linea(x + 0.18f * s, y + 0.8f * s, x + 0.64f * s, y + 0.8f * s, c, g * 0.6f, true);
                    d.Linea(x + 0.92f * s, y + 0.1f * s, x + 0.62f * s, y + 0.62f * s, acento, g * 1.6f);
                    break;
                case "rayo":
                    d.Polilinea(new List<Vector2> { P(0.6f, 0.06f), P(0.26f, 0.54f), P(0.5f, 0.54f), P(0.4f, 0.94f), P(0.76f, 0.42f), P(0.52f, 0.42f), P(0.6f, 0.06f) }, acento, g);
                    break;
                case "reintento":   // flecha circular
                    d.Curva(P(0.78f, 0.36f), P(0.5f, -0.06f), P(0.2f, 0.4f), c, g, false);
                    d.Curva(P(0.2f, 0.4f), P(0.2f, 0.88f), P(0.62f, 0.8f), c, g, false);
                    d.Punta(P(0.6f, 0.2f), P(0.8f, 0.38f), acento, g);
                    break;
                case "uno":   // una sola caja con un 1
                    R(0.22f, 0.14f, 0.56f, 0.72f, c, true);
                    d.Polilinea(new List<Vector2> { P(0.42f, 0.34f), P(0.54f, 0.26f), P(0.54f, 0.74f) }, acento, g * 1.4f);
                    break;
                case "cubos":   // muchas cajitas separadas
                    for (var i = 0; i < 3; i++)
                        for (var j = 0; j < 2; j++)
                            R(0.06f + i * 0.32f, 0.2f + j * 0.34f, 0.24f, 0.26f, (i + j) % 2 == 0 ? c : acento, true);
                    break;
                case "servidores":
                    for (var i = 0; i < 3; i++) {
                        R(0.16f, 0.1f + i * 0.28f, 0.68f, 0.22f, c, true);
                        C(0.28f, 0.21f + i * 0.28f, 0.035f, acento);
                    }
                    break;
                case "llave":
                    C(0.3f, 0.5f, 0.18f, c);
                    L(0.48f, 0.5f, 0.92f, 0.5f);
                    L(0.8f, 0.5f, 0.8f, 0.66f);
                    L(0.68f, 0.5f, 0.68f, 0.62f);
                    break;
                case "comentario":   // un bocadillo con «//»
                    R(0.08f, 0.16f, 0.84f, 0.52f, c, true);
                    L(0.22f, 0.68f, 0.18f, 0.86f);
                    d.Linea(x + 0.38f * s, y + 0.56f * s, x + 0.48f * s, y + 0.28f * s, acento, g);
                    d.Linea(x + 0.52f * s, y + 0.56f * s, x + 0.62f * s, y + 0.28f * s, acento, g);
                    break;
                case "bifurcacion":   // un camino que se abre en dos
                    L(0.5f, 0.92f, 0.5f, 0.54f);
                    d.Curva(P(0.5f, 0.54f), P(0.5f, 0.3f), P(0.2f, 0.14f), c, g, true);
                    d.Curva(P(0.5f, 0.54f), P(0.5f, 0.3f), P(0.8f, 0.14f), acento, g, true);
                    break;
                case "cola":   // sobres en fila hacia una caja
                    for (var i = 0; i < 3; i++) R(0.04f + i * 0.2f, 0.36f, 0.16f, 0.24f, c, true);
                    d.Flecha(x + 0.66f * s, y + 0.48f * s, x + 0.78f * s, y + 0.48f * s, acento, g);
                    R(0.8f, 0.3f, 0.16f, 0.36f, acento, true);
                    break;
                case EstaBien:   // un check grande dentro de un circulo
                    C(0.5f, 0.5f, 0.4f, c);
                    d.Polilinea(new List<Vector2> { P(0.3f, 0.52f), P(0.44f, 0.66f), P(0.72f, 0.36f) }, acento, g * 1.5f);
                    break;
                default:
                    // Las etapas de la tuberia de la Fase 3 tienen sus propios iconos.
                    if (DibujosDeEtapas.Dibujar(d, icono, x, y, s, c, acento)) break;
                    C(0.5f, 0.5f, 0.38f, c);
                    d.Curva(P(0.38f, 0.38f), P(0.5f, 0.18f), P(0.62f, 0.4f), acento, g, false);
                    L(0.62f, 0.4f, 0.5f, 0.56f);
                    C(0.5f, 0.72f, 0.03f, acento);
                    break;
            }
        }
    }
}
