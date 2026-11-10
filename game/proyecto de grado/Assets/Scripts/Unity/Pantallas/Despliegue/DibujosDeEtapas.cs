using System.Collections.Generic;
using Nexus.Unity.Tema;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Los iconos de las etapas de la tuberia de la Fase 3 (2026-11-09), dibujados por codigo con las primitivas de
    /// DibujoUI, como el resto de dibujos del juego. La clave es el id del tramo (compilar, unitarias, produccion…);
    /// 'Dibujar' devuelve false si no hay icono para esa clave (quien llama pone el suyo de reserva). Cada icono cabe en
    /// un cuadrado (x, y, lado) con origen arriba a la izquierda; 'c' es el trazo y 'a' el acento.
    /// </summary>
    public static class DibujosDeEtapas {
        public static bool Dibujar(DibujoUI d, string id, float x, float y, float s, Color c, Color a) {
            if (string.IsNullOrEmpty(id)) return false;
            var g = Mathf.Max(2f, s * 0.045f);
            System.Func<float, float, Vector2> P = (fx, fy) => new Vector2(x + fx * s, y + fy * s);
            System.Action<float, float, float, float, Color> L = (ax, ay, bx, by, col) => d.Linea(x + ax * s, y + ay * s, x + bx * s, y + by * s, col, g);
            System.Action<float, float, float, float, Color> R = (rx, ry, rw, rh, col) =>
                d.Rect(x + rx * s, y + ry * s, rw * s, rh * s, col, g, NexusTheme.Alfa(col, 0.16f), false, s * 0.05f);
            System.Action<float, float, float, Color> C = (cx, cy, r, col) => d.Circulo(x + cx * s, y + cy * s, r * s, col, g);

            switch (id) {
                case "compilar": {   // un engranaje
                    C(0.5f, 0.5f, 0.22f, c);
                    C(0.5f, 0.5f, 0.08f, a);
                    for (var i = 0; i < 8; i++) {
                        var ang = i * Mathf.PI / 4f;
                        var dx = Mathf.Cos(ang); var dy = Mathf.Sin(ang);
                        L(0.5f + dx * 0.27f, 0.5f + dy * 0.27f, 0.5f + dx * 0.37f, 0.5f + dy * 0.37f, c);
                    }
                    return true;
                }
                case "unitarias":
                case "pruebas": {   // un matraz con un visto
                    d.Polilinea(new List<Vector2> { P(0.4f, 0.1f), P(0.4f, 0.42f), P(0.16f, 0.86f), P(0.84f, 0.86f), P(0.6f, 0.42f), P(0.6f, 0.1f) }, c, g);
                    L(0.34f, 0.1f, 0.66f, 0.1f, c);
                    d.Polilinea(new List<Vector2> { P(0.36f, 0.66f), P(0.46f, 0.76f), P(0.66f, 0.56f) }, a, g * 1.2f);
                    return true;
                }
                case "integracion": {   // dos piezas que encajan
                    R(0.06f, 0.3f, 0.34f, 0.38f, c);
                    R(0.6f, 0.3f, 0.34f, 0.38f, a);
                    L(0.4f, 0.42f, 0.6f, 0.42f, c);
                    L(0.4f, 0.56f, 0.6f, 0.56f, a);
                    return true;
                }
                case "seguridad": {   // un escudo con cerradura
                    d.Polilinea(new List<Vector2> { P(0.5f, 0.06f), P(0.86f, 0.2f), P(0.8f, 0.6f), P(0.5f, 0.94f), P(0.2f, 0.6f), P(0.14f, 0.2f), P(0.5f, 0.06f) }, c, g);
                    C(0.5f, 0.42f, 0.09f, a);
                    L(0.5f, 0.5f, 0.5f, 0.66f, a);
                    return true;
                }
                case "empaquetar": {   // una caja cerrada con cinta
                    R(0.12f, 0.34f, 0.76f, 0.5f, c);
                    L(0.12f, 0.5f, 0.88f, 0.5f, c);
                    L(0.5f, 0.34f, 0.5f, 0.84f, a);
                    d.Polilinea(new List<Vector2> { P(0.12f, 0.34f), P(0.26f, 0.16f), P(0.74f, 0.16f), P(0.88f, 0.34f) }, c, g);
                    return true;
                }
                case "entorno-de-pruebas": {   // un escenario de ensayo: pantalla con «play»
                    R(0.08f, 0.16f, 0.84f, 0.54f, c);
                    L(0.5f, 0.7f, 0.5f, 0.84f, c);
                    L(0.3f, 0.86f, 0.7f, 0.86f, c);
                    d.Polilinea(new List<Vector2> { P(0.42f, 0.28f), P(0.62f, 0.43f), P(0.42f, 0.58f), P(0.42f, 0.28f) }, a, g);
                    return true;
                }
                case "visto-bueno": {   // un sello con un visto
                    C(0.5f, 0.5f, 0.34f, c);
                    d.Polilinea(new List<Vector2> { P(0.34f, 0.52f), P(0.46f, 0.64f), P(0.68f, 0.38f) }, a, g * 1.3f);
                    return true;
                }
                case "produccion": {   // un cohete
                    d.Polilinea(new List<Vector2> { P(0.5f, 0.06f), P(0.66f, 0.38f), P(0.66f, 0.68f), P(0.34f, 0.68f), P(0.34f, 0.38f), P(0.5f, 0.06f) }, c, g);
                    C(0.5f, 0.36f, 0.07f, a);
                    L(0.34f, 0.56f, 0.2f, 0.74f, c);
                    L(0.66f, 0.56f, 0.8f, 0.74f, c);
                    L(0.42f, 0.74f, 0.5f, 0.94f, a);
                    L(0.58f, 0.74f, 0.5f, 0.94f, a);
                    return true;
                }
                case "vigilancia": {   // un ojo que vigila
                    d.Elipse(x + 0.5f * s, y + 0.5f * s, 0.4f * s, 0.22f * s, c, g);
                    C(0.5f, 0.5f, 0.1f, a);
                    d.Curva(P(0.2f, 0.2f), P(0.5f, 0.0f), P(0.8f, 0.2f), a, g * 0.8f, false);
                    return true;
                }
                case "directo": {   // un atajo: una flecha que salta por encima de todo
                    d.Curva(P(0.08f, 0.66f), P(0.5f, 0.0f), P(0.92f, 0.66f), a, g * 1.2f, true);
                    L(0.1f, 0.84f, 0.9f, 0.84f, c);
                    d.Polilinea(new List<Vector2> { P(0.5f, 0.36f), P(0.58f, 0.52f), P(0.42f, 0.52f), P(0.5f, 0.36f) }, a, g);
                    return true;
                }
                case "probar-en-produccion": {   // un dado: se apuesta con los clientes dentro
                    R(0.2f, 0.2f, 0.6f, 0.6f, c);
                    C(0.36f, 0.36f, 0.04f, a); C(0.5f, 0.5f, 0.04f, a); C(0.64f, 0.64f, 0.04f, a);
                    return true;
                }
                case "a-mano": {   // un archivo que alguien arrastra
                    d.Polilinea(new List<Vector2> { P(0.2f, 0.1f), P(0.2f, 0.7f), P(0.6f, 0.7f), P(0.6f, 0.26f), P(0.44f, 0.1f), P(0.2f, 0.1f) }, c, g);
                    d.Flecha(x + 0.4f * s, y + 0.9f * s, x + 0.92f * s, y + 0.9f * s, a, g);
                    return true;
                }
            }
            return false;
        }
    }
}
