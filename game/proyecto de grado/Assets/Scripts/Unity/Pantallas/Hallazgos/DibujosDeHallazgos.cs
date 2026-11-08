using System;
using System.Collections.Generic;
using Nexus.Core.Coleccion;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Los dibujos de los hallazgos, a mano con el Tizador (los mismos trazos que las pizarras del juego): el emblema de
    /// cada carta (caja de 240×220), el glifo de cada palo (caja de 60×60), el icono de cada advertencia (120×120) y los
    /// pictogramas del diario (120×90). Coordenadas desde arriba a la izquierda, y hacia abajo. Sin textos dentro: los
    /// textos del Tizador no se «trazan» con DibujoUI.Avance.
    /// Las claves vienen de Core (EmblemasDeColeccionables): un test exige que el catalogo tenga todas y sin repetir.
    /// </summary>
    public static class DibujosDeHallazgos {
        private static readonly Color Tiza = Tizador.Tiza, Cian = Tizador.Cian, Mostaza = Tizador.Mostaza, Roja = Tizador.Roja, Gris = Tizador.Gris;

        // ==================================================================== emblemas de las cartas (240×220)

        public static readonly Dictionary<string, Action<Tizador, Color>> Emblemas = new Dictionary<string, Action<Tizador, Color>> {
            // Ley de Conway: dos equipos (izquierda) y dos modulos (derecha) con la MISMA frontera entre ellos.
            { "organigrama-espejo", (t, c) => {
                t.Circulo(60, 40, 13, c); t.Linea(60, 53, 60, 70, c); t.Linea(30, 70, 90, 70, c);
                t.Linea(30, 70, 30, 88, c); t.Linea(90, 70, 90, 88, c);
                t.Circulo(30, 102, 13, c); t.Circulo(90, 102, 13, c);
                t.Linea(120, 20, 120, 200, Gris, 2, true);
                t.Caja(158, 28, 44, 26, null, Tiza); t.Linea(180, 54, 180, 70, Tiza); t.Linea(150, 70, 210, 70, Tiza);
                t.Caja(128, 90, 44, 26, null, Tiza); t.Caja(188, 90, 44, 26, null, Tiza);
                t.Linea(172, 103, 188, 103, Roja, 4);
                t.Curva(40, 150, 120, 205, 200, 150, Mostaza, true);
            } },
            // Therac-25: el trebol de radiacion y una dosis que se sale de la escala.
            { "radiacion", (t, c) => {
                t.Punto(120, 86, 12, Mostaza);
                for (var k = 0; k < 3; k++) {
                    var a0 = (-90 + k * 120 - 30) * Mathf.Deg2Rad; var a1 = (-90 + k * 120 + 30) * Mathf.Deg2Rad;
                    var xy = new List<float>();
                    for (var i = 0; i <= 8; i++) { var a = Mathf.Lerp(a0, a1, i / 8f); xy.Add(120 + Mathf.Cos(a) * 66); xy.Add(86 + Mathf.Sin(a) * 66); }
                    for (var i = 8; i >= 0; i--) { var a = Mathf.Lerp(a0, a1, i / 8f); xy.Add(120 + Mathf.Cos(a) * 24); xy.Add(86 + Mathf.Sin(a) * 24); }
                    xy.Add(xy[0]); xy.Add(xy[1]);
                    t.Trazo(Mostaza, xy.ToArray());
                }
                t.Caja(30, 176, 180, 18, null, Tiza);
                t.Linea(36, 185, 150, 185, c, 8);
                t.Linea(150, 185, 228, 185, Roja, 8);
                t.Linea(204, 168, 204, 202, Tiza, 2);
            } },
            // Ley de Parkinson: el reloj de arena, y la tarea que se estira hasta llenar el plazo.
            { "reloj-que-crece", (t, c) => {
                t.Linea(84, 20, 156, 20, Tiza, 4); t.Linea(84, 140, 156, 140, Tiza, 4);
                t.Trazo(Tiza, 92, 20, 148, 140); t.Trazo(Tiza, 148, 20, 92, 140);
                t.Linea(108, 130, 132, 130, Mostaza, 6); t.Linea(114, 36, 126, 36, Mostaza, 4);
                t.Caja(80, 168, 80, 22, null, c);
                t.Flecha(76, 179, 22, 179, c); t.Flecha(164, 179, 218, 179, c);
            } },
            // Knight Capital: siete servidores bien y uno con el codigo viejo; la grafica que se desploma.
            { "desplome", (t, c) => {
                for (var i = 0; i < 8; i++) {
                    var mala = i == 5;
                    t.Caja(14 + i * 28, 168, 22, 34, null, mala ? Roja : Tiza);
                    t.Punto(25 + i * 28, 196, 3, mala ? Roja : Cian);
                }
                t.Trazo(c, 16, 120, 50, 100, 80, 108, 110, 70, 140, 52);
                t.Flecha(140, 52, 196, 150, Roja, 4);
                t.Linea(14, 150, 226, 150, Gris, 2, true);
            } },
            // ADR: el documento con sus lineas y el sello de «decidido».
            { "pergamino", (t, c) => {
                t.Trazo(Tiza, 64, 18, 154, 18, 176, 40, 176, 196, 64, 196, 64, 18);
                t.Trazo(Tiza, 154, 18, 154, 40, 176, 40);
                for (var i = 0; i < 6; i++) t.Linea(80, 58 + i * 18, i % 3 == 2 ? 130 : 160, 58 + i * 18, Gris, 2);
                t.Circulo(160, 168, 28, c, false, 4); t.Circulo(160, 168, 20, c, false, 2);
                t.Trazo(c, 148, 168, 156, 177, 172, 158);
            } },
            // Bus factor: el autobus, y la unica persona que sabe como funciona, sola.
            { "autobus", (t, c) => {
                t.Caja(76, 64, 154, 84, null, Tiza);
                for (var i = 0; i < 4; i++) t.Caja(86 + i * 34, 76, 26, 26, null, Cian);
                t.Circulo(110, 152, 15, Tiza); t.Circulo(196, 152, 15, Tiza);
                t.Linea(76, 120, 230, 120, Gris, 2);
                t.Persona(34, 100, c);
                t.Circulo(34, 100, 34, Mostaza, true, 2);
            } },
            // Deuda tecnica: monedas que crecen con los intereses.
            { "moneda-interes", (t, c) => {
                t.Circulo(46, 170, 18, Mostaza); t.Circulo(46, 170, 11, Mostaza, false, 2);
                t.Circulo(110, 140, 28, Mostaza); t.Circulo(110, 140, 19, Mostaza, false, 2);
                t.Circulo(186, 92, 42, Mostaza); t.Circulo(186, 92, 30, Mostaza, false, 2);
                t.Curva(30, 120, 70, 40, 150, 34, c, true);
                t.Linea(20, 204, 226, 204, Gris, 2);
            } },
            // Burnout: la bateria casi vacia, con su raya roja.
            { "bateria", (t, c) => {
                t.Caja(46, 70, 136, 76, null, Tiza);
                t.Bloque(182, 92, 12, 32, Tiza);
                t.Bloque(56, 80, 22, 56, Roja);
                t.Linea(84, 80, 84, 136, Gris, 2, true); t.Linea(116, 80, 116, 136, Gris, 2, true); t.Linea(148, 80, 148, 136, Gris, 2, true);
                t.Trazo(c, 120, 168, 108, 188, 124, 188, 112, 210);
            } },
            // La regla del Boy Scout: la escoba y el codigo que queda mas limpio que antes.
            { "escoba", (t, c) => {
                t.Linea(60, 24, 130, 130, Mostaza, 5);
                t.Trazo(Tiza, 112, 128, 150, 116, 172, 168, 128, 186, 112, 128);
                for (var i = 0; i < 4; i++) t.Linea(130 + i * 8, 140, 140 + i * 9, 180, Gris, 2);
                t.Estrella(196, 60, 14, c); t.Estrella(214, 110, 9, c); t.Estrella(170, 34, 7, c);
                t.Punto(40, 196, 3, Gris); t.Punto(58, 204, 2, Gris); t.Punto(30, 182, 2, Gris);
            } },
            // Commits pequeños: la rama que sale, hace dos commits y vuelve a la principal.
            { "ramas", (t, c) => {
                t.Linea(16, 150, 224, 150, Tiza, 4);
                t.Curva(60, 150, 70, 80, 110, 80, c, false); t.Linea(110, 80, 160, 80, c, 4);
                t.Curva(160, 80, 200, 80, 200, 150, c, false);
                foreach (var x in new[] { 30f, 60f, 200f, 224f }) t.Punto(x, 150, 9, Tiza);
                t.Punto(110, 80, 9, c); t.Punto(160, 80, 9, c);
            } },
            // Cobertura: el escudo de las pruebas con su marca.
            { "escudo", (t, c) => {
                t.Trazo(c, 120, 18, 196, 46, 192, 118, 120, 202, 48, 118, 44, 46, 120, 18);
                t.Trazo(Gris, 120, 40, 176, 60, 172, 114, 120, 176, 68, 114, 64, 60, 120, 40);
                t.Trazo(Tiza, 88, 108, 112, 134, 156, 80);
            } },
            // Historias de usuario: la tarjeta con sus criterios de aceptacion, marcados.
            { "criterios", (t, c) => {
                t.Caja(48, 22, 144, 180, null, Tiza);
                t.Linea(64, 50, 176, 50, c, 5);
                for (var i = 0; i < 3; i++) {
                    var y = 86 + i * 38;
                    t.Caja(64, y - 12, 22, 22, null, Gris);
                    if (i < 2) t.Trazo(c, 68, y, 74, y + 7, 86, y - 10);
                    t.Linea(98, y, i == 2 ? 140 : 172, y, Tiza, 2);
                }
            } },
            // Diagramas: el edificio dibujado antes de construir, con sus cotas.
            { "plano", (t, c) => {
                t.Caja(60, 80, 120, 110, null, c, true);
                t.Trazo(c, 52, 84, 120, 28, 188, 84);
                t.Caja(80, 110, 26, 30, null, Tiza); t.Caja(134, 110, 26, 30, null, Tiza); t.Caja(106, 150, 28, 40, null, Tiza);
                t.Flecha(60, 206, 180, 206, Gris, 2); t.Flecha(180, 206, 60, 206, Gris, 2);
                t.Flecha(206, 80, 206, 190, Gris, 2); t.Flecha(206, 190, 206, 80, Gris, 2);
            } },
            // Pausas activas: la taza humeante y el reloj que marca el descanso.
            { "taza-pausa", (t, c) => {
                t.Trazo(Tiza, 46, 96, 54, 186, 130, 186, 138, 96, 46, 96);
                t.Curva(138, 112, 178, 120, 136, 160, Tiza, false);
                t.Curva(70, 84, 56, 62, 76, 40, Gris, false); t.Curva(96, 84, 82, 60, 102, 34, Gris, false);
                t.Circulo(188, 54, 30, c); t.Linea(188, 54, 188, 34, c); t.Linea(188, 54, 204, 62, c);
                t.Linea(30, 192, 160, 192, Gris, 2);
            } },
            // Code review: la lupa sobre el codigo, y un bocadillo amable al lado.
            { "lupa-dialogo", (t, c) => {
                var largos = new[] { 120f, 90f, 140f, 70f, 110f };
                for (var i = 0; i < largos.Length; i++) t.Linea(20 + (i % 2) * 14, 100 + i * 20, 20 + largos[i], 100 + i * 20, Gris, 3);
                t.Circulo(96, 132, 36, c, false, 4); t.Linea(122, 158, 152, 190, c, 7);
                t.Trazo(Tiza, 140, 20, 226, 20, 226, 72, 168, 72, 152, 90, 156, 72, 140, 72, 140, 20);
                t.Carita(183, 46, 15, 1, Mostaza);
            } },
        };

        // ==================================================================== glifo de cada palo (60×60)

        public static void GlifoDePalo(Tizador t, string palo, Color c) {
            switch (palo) {
                case PalosDeCarta.Leyes:   // una balanza
                    t.Linea(30, 10, 30, 52, c); t.Linea(18, 52, 42, 52, c);
                    t.Linea(8, 18, 52, 18, c);
                    t.Trazo(c, 8, 18, 2, 34, 14, 34, 8, 18); t.Trazo(c, 52, 18, 46, 34, 58, 34, 52, 18);
                    break;
                case PalosDeCarta.Desastres:   // un triangulo de peligro
                    t.Trazo(c, 30, 6, 56, 52, 4, 52, 30, 6);
                    t.Linea(30, 22, 30, 38, c, 4); t.Punto(30, 45, 2.5f, c);
                    break;
                case PalosDeCarta.Practicas:   // un engranaje
                    t.Circulo(30, 30, 14, c);
                    for (var k = 0; k < 8; k++) {
                        var a = k * Mathf.PI / 4;
                        t.Linea(30 + Mathf.Cos(a) * 15, 30 + Mathf.Sin(a) * 15, 30 + Mathf.Cos(a) * 24, 30 + Mathf.Sin(a) * 24, c, 5);
                    }
                    t.Circulo(30, 30, 5, c);
                    break;
                default:   // oficio: una llave inglesa
                    t.Linea(14, 48, 38, 24, c, 6);
                    t.Trazo(c, 34, 14, 40, 8, 52, 12, 46, 20, 52, 26, 44, 30, 34, 20);
                    break;
            }
        }

        // ==================================================================== iconos de las advertencias (120×120)

        public static readonly Dictionary<string, Action<Tizador, Color>> IconosDeAdvertencia = new Dictionary<string, Action<Tizador, Color>> {
            { "commit", (t, c) => { t.Linea(10, 60, 110, 60, c, 4); t.Punto(30, 60, 8, c); t.Circulo(60, 60, 16, c, false, 5); t.Punto(90, 60, 8, c); } },
            { "documento", (t, c) => { t.Trazo(c, 30, 14, 76, 14, 92, 30, 92, 106, 30, 106, 30, 14); for (var i = 0; i < 4; i++) t.Linea(42, 44 + i * 16, 80, 44 + i * 16, c, 3); } },
            { "termometro", (t, c) => { t.Trazo(c, 52, 90, 52, 18, 68, 18, 68, 90); t.Circulo(60, 98, 14, c); t.Punto(60, 98, 9, c); t.Linea(60, 88, 60, 34, c, 6); } },
            { "embudo", (t, c) => { t.Trazo(c, 14, 18, 106, 18, 68, 64, 68, 104, 52, 96, 52, 64, 14, 18); t.Punto(40, 34, 5, c); t.Punto(60, 30, 5, c); t.Punto(80, 34, 5, c); } },
            { "persona", (t, c) => { t.Circulo(60, 38, 18, c); t.Trazo(c, 26, 104, 30, 74, 60, 62, 90, 74, 94, 104); t.Linea(98, 20, 112, 34, c, 4); t.Linea(112, 20, 98, 34, c, 4); } },
            { "reloj-de-arena", (t, c) => { t.Linea(30, 14, 90, 14, c, 5); t.Linea(30, 106, 90, 106, c, 5); t.Trazo(c, 38, 14, 82, 106); t.Trazo(c, 82, 14, 38, 106); t.Linea(50, 98, 70, 98, c, 6); } },
            { "bocadillo", (t, c) => { t.Trazo(c, 14, 20, 106, 20, 106, 78, 52, 78, 30, 102, 34, 78, 14, 78, 14, 20); t.Linea(30, 40, 90, 40, c, 3); t.Linea(30, 58, 70, 58, c, 3); } },
            { "balanza", (t, c) => { t.Linea(60, 16, 60, 100, c, 4); t.Linea(36, 102, 84, 102, c, 5); t.Linea(18, 34, 102, 34, c, 4);
                                     t.Trazo(c, 18, 34, 6, 64, 30, 64, 18, 34); t.Trazo(c, 102, 34, 90, 64, 114, 64, 102, 34); } },
            { "calendario", (t, c) => { t.Caja(16, 24, 88, 80, null, c); t.Linea(16, 44, 104, 44, c, 4); t.Linea(36, 14, 36, 32, c, 4); t.Linea(84, 14, 84, 32, c, 4);
                                        t.Linea(64, 62, 92, 92, c, 5); t.Linea(92, 62, 64, 92, c, 5); } },
            { "lupa", (t, c) => { t.Circulo(50, 50, 30, c, false, 5); t.Linea(72, 72, 106, 106, c, 8); t.Linea(36, 50, 64, 50, c, 3); } },
            { "brujula", (t, c) => { t.Circulo(60, 60, 44, c, false, 4); t.Trazo(c, 60, 22, 70, 60, 60, 98, 50, 60, 60, 22); t.Punto(60, 60, 4, c); } },
            { "sirena", (t, c) => { t.Curva(34, 84, 60, 10, 86, 84, c, false); t.Linea(24, 86, 96, 86, c, 6); t.Linea(30, 100, 90, 100, c, 4);
                                    t.Linea(14, 40, 26, 50, c, 3); t.Linea(106, 40, 94, 50, c, 3); t.Linea(60, 6, 60, 18, c, 3); } },
        };

        // ==================================================================== pictogramas del diario (120×90)

        public static void Pictograma(Tizador t, string serie, Color c) {
            switch (serie) {
                case SeriesDeColeccionables.Usb:
                    t.Caja(22, 30, 62, 32, null, c); t.Caja(84, 36, 20, 20, null, Tiza); t.Punto(34, 46, 3, Mostaza);
                    break;
                case SeriesDeColeccionables.Codigo:
                    t.Caja(14, 14, 92, 64, null, c); t.Linea(14, 28, 106, 28, c, 2);
                    t.Trazo(Tiza, 26, 42, 36, 50, 26, 58); t.Linea(42, 60, 58, 60, Tiza, 3);
                    break;
                case SeriesDeColeccionables.Startup:
                    t.Trazo(c, 34, 84, 34, 34, 60, 12, 86, 34, 86, 84); t.Linea(24, 84, 96, 84, Gris, 3);
                    t.Linea(50, 46, 70, 46, Tiza, 2); t.Linea(60, 36, 60, 64, Tiza, 2);
                    break;
                case SeriesDeColeccionables.Nota:
                    t.Trazo(c, 30, 12, 90, 12, 90, 64, 72, 82, 30, 82, 30, 12); t.Trazo(c, 90, 64, 72, 64, 72, 82);
                    t.Linea(40, 32, 78, 32, Tiza, 2); t.Linea(40, 46, 70, 46, Tiza, 2);
                    break;
                case SeriesDeColeccionables.Advertencia:
                    t.Trazo(c, 60, 8, 104, 82, 16, 82, 60, 8); t.Linea(60, 34, 60, 58, c, 5); t.Punto(60, 70, 3.5f, c);
                    break;
                default:
                    t.Caja(36, 8, 48, 74, null, c);
                    break;
            }
        }
    }
}
