using System;
using System.Collections.Generic;
using Nexus.Unity.Tema;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Los pictogramas de los nodos de la pausa (2026-11-07). Se DIBUJAN con trazos, no con texto: la letra del juego
    /// (Rajdhani) no tiene simbolos como ✓ ▸ ★ ← →, y un cuadrito en medio de la pausa se veria fatal.
    ///
    /// Cada uno cabe en un DibujoUI de 64 x 64 (coordenadas desde arriba a la izquierda, como el resto de dibujos del
    /// juego). Se pueden repintar con otro color: el nodo enfocado los pone en cian.
    /// </summary>
    public static class PictogramasDePausa {
        public enum Tipo { Ayuda, Apuntes, Diario, Opciones, Guardar, Borrar }

        public const float Lado = 64f;

        public static void Dibujar(DibujoUI d, Tipo tipo, Color c) {
            d.Limpiar();
            const float g = 4f;   // grosor de los trazos grandes
            const float f = 3f;   // grosor de los detalles
            switch (tipo) {
                case Tipo.Diario:   // un libro: tapa, lomo y tres renglones
                    d.Rect(14, 8, 36, 48, c, g, null, false, 5);
                    d.Linea(23, 8, 23, 56, c, f);
                    d.Linea(30, 22, 43, 22, c, f);
                    d.Linea(30, 32, 43, 32, c, f);
                    d.Linea(30, 42, 39, 42, c, f);
                    break;

                case Tipo.Opciones:   // tres controles deslizantes con su perilla
                    d.Linea(8, 16, 56, 16, c, f);
                    d.Linea(8, 32, 56, 32, c, f);
                    d.Linea(8, 48, 56, 48, c, f);
                    d.Circulo(42, 16, 6, c, f, c);
                    d.Circulo(20, 32, 6, c, f, c);
                    d.Circulo(36, 48, 6, c, f, c);
                    break;

                case Tipo.Guardar:   // un disquete: cuerpo, etiqueta y ranura
                    d.Rect(10, 10, 44, 44, c, g, null, false, 5);
                    d.Rect(20, 10, 24, 14, c, f, null, false, 2);
                    d.Rect(18, 36, 28, 18, c, f, null, false, 2);
                    break;

                case Tipo.Ayuda:   // la «?» dibujada: el gancho en arco, el rabo y el punto
                    var gancho = new List<Vector2>();
                    for (var grados = 200; grados >= -50; grados -= 25) {
                        var a = grados * Mathf.Deg2Rad;
                        gancho.Add(new Vector2(32 + 12 * Mathf.Cos(a), 24 - 12 * Mathf.Sin(a)));
                    }
                    gancho.Add(new Vector2(32, 40));
                    gancho.Add(new Vector2(32, 44));
                    d.Polilinea(gancho, c, g);
                    d.Circulo(32, 55, 3.5f, c, 2, c);
                    break;

                case Tipo.Borrar:   // una papelera: tapa, asa, cubo y tres rayas
                    d.Linea(12, 18, 52, 18, c, g);
                    d.Rect(26, 8, 12, 10, c, f, null, false, 2);
                    d.Rect(18, 22, 28, 34, c, g, null, false, 3);
                    d.Linea(26, 30, 26, 48, c, f);
                    d.Linea(32, 30, 32, 48, c, f);
                    d.Linea(38, 30, 38, 48, c, f);
                    break;

                case Tipo.Apuntes:   // una hoja con la esquina doblada y renglones
                    d.Polilinea(new List<Vector2> {
                        new Vector2(16, 8), new Vector2(40, 8), new Vector2(50, 18),
                        new Vector2(50, 56), new Vector2(16, 56), new Vector2(16, 8)
                    }, c, g);
                    d.Polilinea(new List<Vector2> { new Vector2(40, 8), new Vector2(40, 18), new Vector2(50, 18) }, c, f);
                    d.Linea(24, 30, 42, 30, c, f);
                    d.Linea(24, 40, 42, 40, c, f);
                    d.Linea(24, 48, 34, 48, c, f);
                    break;
            }
        }
    }
}
