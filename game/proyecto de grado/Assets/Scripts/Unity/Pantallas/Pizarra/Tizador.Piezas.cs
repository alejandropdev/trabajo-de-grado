using System.Collections.Generic;
using UnityEngine;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>
    /// Las piezas con las que se arman las escenas de los pizarrones: gente, cocina, oficina, graficas. A diferencia
    /// de Persona, Check, Cruz y Reloj (que tienen tamaño fijo), todas estas crecen con la escala del tizador: una
    /// escena se dibuja una vez en su cuadro de 600×300 y vale igual a pagina completa que dentro de una columna.
    /// </summary>
    public sealed partial class Tizador {
        /// <summary>Una linea quebrada: Trazo(c, x1, y1, x2, y2, x3, y3…).</summary>
        public void Trazo(Color? c, params float[] xy) {
            var pts = new List<Vector2>();
            for (var i = 0; i + 1 < xy.Length; i += 2) pts.Add(P(xy[i], xy[i + 1]));
            D.Polilinea(pts, c ?? Tiza, 3 * _k);
        }

        public void Elipse(float x, float y, float rx, float ry, Color? c = null, bool disc = false) {
            D.Elipse(X(x), Y(y), rx * _k, ry * _k, c ?? Tiza, 3 * _k, null, disc);
        }

        /// <summary>Un circulo macizo (una bola, una moneda, un punto de una grafica).</summary>
        public void Punto(float x, float y, float r, Color? c = null) { D.Circulo(X(x), Y(y), r * _k, c ?? Tiza, 0, c ?? Tiza); }

        /// <summary>Un rectangulo macizo (una barra, una banda de color).</summary>
        public void Bloque(float x, float y, float w, float h, Color? c = null) {
            D.Rect(X(x), Y(y), w * _k, h * _k, c ?? Tiza, 0, c ?? Tiza, false, 3 * _k);
        }

        // ================================================================ gente

        /// <summary>Una persona de cabeza y hombros; (x, y) es el centro de la cabeza. Ocupa 40×50.</summary>
        public void Figura(float x, float y, Color? c = null, string t = null) {
            Circulo(x, y, 11, c);
            Curva(x - 17, y + 36, x, y + 4, x + 17, y + 36, c, false);
            if (t != null) Texto(x, y + 58, t, 16, c ?? Tiza, true);
        }

        /// <summary>Un cocinero: la figura con su gorro.</summary>
        public void Cocinero(float x, float y, Color? c = null, string t = null) {
            Figura(x, y, c, t);
            Trazo(c, x - 10, y - 12, x - 10, y - 20, x + 10, y - 20, x + 10, y - 12);
            Curva(x - 12, y - 20, x, y - 36, x + 12, y - 20, c, false);
        }

        /// <summary>Una cara: animo 1 contenta, 0 seria, -1 disgustada.</summary>
        public void Carita(float x, float y, float r, int animo, Color? c = null) {
            Circulo(x, y, r, c);
            Punto(x - r * 0.35f, y - r * 0.25f, r * 0.1f, c); Punto(x + r * 0.35f, y - r * 0.25f, r * 0.1f, c);
            var b = y + r * 0.35f;
            if (animo == 0) Linea(x - r * 0.4f, b, x + r * 0.4f, b, c);
            else Curva(x - r * 0.45f, b, x, b + animo * r * 0.5f, x + r * 0.45f, b, c, false);
        }

        /// <summary>Lo que alguien dice: una caja con su pico hacia abajo a la izquierda.</summary>
        public void Bocadillo(float x, float y, float w, float h, string t, Color? c = null, float tam = 17) {
            Caja(x, y, w, h, t, c, false, tam);
            Trazo(c, x + 18, y + h, x + 10, y + h + 16, x + 36, y + h);
        }

        public void Si(float x, float y, Color? c = null, float r = 14) { Trazo(c ?? Cian, x - r, y, x - r * 0.3f, y + r * 0.8f, x + r * 1.1f, y - r * 0.9f); }
        public void No(float x, float y, Color? c = null, float r = 14) { Linea(x - r, y - r, x + r, y + r, c ?? Roja, 4); Linea(x + r, y - r, x - r, y + r, c ?? Roja, 4); }

        // ================================================================ cocina

        public void Olla(float x, float y, float w, float h, Color? c = null) {
            Trazo(c, x, y, x, y + h, x + w, y + h, x + w, y);
            Linea(x - 8, y, x + w + 8, y, c);
            Linea(x - 8, y + h * 0.3f, x - 20, y + h * 0.3f, c); Linea(x + w + 8, y + h * 0.3f, x + w + 20, y + h * 0.3f, c);
        }

        /// <summary>El vapor de algo caliente, sobre (x, y).</summary>
        public void Vapor(float x, float y, Color? c = null) {
            for (var i = -1; i <= 1; i++) Curva(x + i * 14, y, x + i * 14 + 8, y - 12, x + i * 14, y - 24, c ?? Gris, false);
        }

        public void Plato(float x, float y, float r, Color? c = null) { Elipse(x, y, r, r * 0.34f, c); Elipse(x, y - 2, r * 0.6f, r * 0.18f, c); }

        /// <summary>Un plato servido: el plato con su comida encima.</summary>
        public void PlatoServido(float x, float y, float r, Color? c = null) {
            Plato(x, y, r, c);
            Curva(x - r * 0.55f, y - 3, x, y - r * 0.9f, x + r * 0.55f, y - 3, c ?? Mostaza, false);
        }

        public void Bandeja(float x, float y, float w, Color? c = null) {
            Linea(x, y, x + w, y, c, 4); Curva(x + w * 0.2f, y, x + w / 2, y - w * 0.45f, x + w * 0.8f, y, c, false);
        }

        // ================================================================ oficina y edificios

        public void Hexagono(float x, float y, float r, Color? c = null) {
            var xy = new float[14];
            for (var i = 0; i <= 6; i++) { var a = Mathf.PI / 3 * i + Mathf.PI / 6; xy[i * 2] = x + Mathf.Cos(a) * r; xy[i * 2 + 1] = y + Mathf.Sin(a) * r; }
            Trazo(c, xy);
        }

        /// <summary>Un edificio de oficinas con sus ventanas y su puerta.</summary>
        public void Edificio(float x, float y, float w, float h, Color? c = null) {
            Caja(x, y, w, h, null, c);
            var cols = Mathf.Max(2, Mathf.RoundToInt(w / 26)); var filas = Mathf.Max(2, Mathf.RoundToInt(h / 34) - 1);
            var pw = w / (cols * 2 + 1); var ph = (h - 30) / (filas * 2 + 1);
            for (var f = 0; f < filas; f++)
                for (var k = 0; k < cols; k++) D.Rect(X(x + pw * (1 + k * 2)), Y(y + ph * (1 + f * 2)), pw * _k, ph * _k, c ?? Tiza, 2 * _k, null, false, 0);
            Trazo(c, x + w / 2 - 9, y + h, x + w / 2 - 9, y + h - 22, x + w / 2 + 9, y + h - 22, x + w / 2 + 9, y + h);
        }

        /// <summary>Una hoja con casillas que rellenar.</summary>
        public void Formulario(float x, float y, float w, float h, Color? c = null) {
            Caja(x, y, w, h, null, c);
            var n = Mathf.Max(2, Mathf.RoundToInt(h / 34));
            for (var i = 0; i < n; i++) {
                var fy = y + 16 + i * (h - 24) / n;
                Linea(x + 10, fy + 6, x + w * 0.32f, fy + 6, c, 2);
                D.Rect(X(x + w * 0.4f), Y(fy - 4), w * 0.5f * _k, 16 * _k, c ?? Tiza, 2 * _k, null, false, 3 * _k);
            }
        }

        /// <summary>Una hoja con sus renglones (un documento, una receta, un contrato).</summary>
        public void Hoja(float x, float y, float w, float h, Color? c = null, string titulo = null) {
            Caja(x, y, w, h, null, c);
            var y0 = y + 18;
            if (titulo != null) { Texto(x + w / 2, y + 24, titulo, 16, c ?? Tiza, true); y0 = y + 38; }
            for (var fy = y0; fy < y + h - 8; fy += 14) Linea(x + 10, fy, x + w - 10, fy, c, 2);
        }

        public void Calendario(float x, float y, float w, float h, Color? c = null) {
            Caja(x, y, w, h, null, c);
            Linea(x, y + h * 0.26f, x + w, y + h * 0.26f, c);
            Linea(x + w * 0.25f, y - 8, x + w * 0.25f, y + 8, c, 4); Linea(x + w * 0.75f, y - 8, x + w * 0.75f, y + 8, c, 4);
            for (var f = 0; f < 2; f++) for (var k = 0; k < 3; k++) Punto(x + w * (0.25f + k * 0.25f), y + h * (0.5f + f * 0.26f), 3.5f, c);
        }

        /// <summary>Una tarjeta del tablero: su titulo y dos renglones.</summary>
        public void Tarjeta(float x, float y, float w, float h, Color? c = null, string t = null) {
            Caja(x, y, w, h, null, c);
            if (t != null) Texto(x + w / 2, y + h / 2 + 6, t, 15, c ?? Tiza, true);
            else { Linea(x + 8, y + h * 0.36f, x + w - 8, y + h * 0.36f, c, 2); Linea(x + 8, y + h * 0.68f, x + w * 0.6f, y + h * 0.68f, c, 2); }
        }

        /// <summary>Un tablero de columnas con su titulo cada una.</summary>
        public void Tablero(float x, float y, float w, float h, Color? c, params string[] columnas) {
            Caja(x, y, w, h, null, c);
            var n = Mathf.Max(1, columnas.Length); var cw = w / n;
            Linea(x, y + 26, x + w, y + 26, c, 2);
            for (var i = 0; i < n; i++) {
                if (i > 0) Linea(x + cw * i, y, x + cw * i, y + h, c, 2);
                if (!string.IsNullOrEmpty(columnas[i])) Texto(x + cw * i + cw / 2, y + 19, columnas[i], 13, c ?? Tiza, true);
            }
        }

        // ================================================================ objetos

        public void Estrella(float x, float y, float r, Color? c = null) {
            var xy = new float[22];
            for (var i = 0; i <= 10; i++) {
                var a = -Mathf.PI / 2 + Mathf.PI / 5 * i; var rr = i % 2 == 0 ? r : r * 0.42f;
                xy[i * 2] = x + Mathf.Cos(a) * rr; xy[i * 2 + 1] = y + Mathf.Sin(a) * rr;
            }
            Trazo(c ?? Mostaza, xy);
        }

        /// <summary>Una bandera clavada en (x, y), que es el pie del asta.</summary>
        public void Bandera(float x, float y, float h, Color? c = null) {
            Linea(x, y, x, y - h, c);
            Trazo(c ?? Mostaza, x, y - h, x + h * 0.55f, y - h * 0.8f, x, y - h * 0.6f);
        }

        /// <summary>Un sello de caucho estampado: su palabra en un marco doble.</summary>
        public void Sello(float x, float y, string t, Color? c = null) {
            var w = t.Length * 13 + 30;
            Caja(x - w / 2, y - 20, w, 40, t, c ?? Cian, false, 20);
            D.Rect(X(x - w / 2 - 6), Y(y - 26), (w + 12) * _k, 52 * _k, c ?? Cian, 2 * _k, null, false, 10 * _k);
        }

        public void Lupa(float x, float y, float r, Color? c = null) {
            Circulo(x, y, r, c); Linea(x + r * 0.7f, y + r * 0.7f, x + r * 1.7f, y + r * 1.7f, c, 5);
        }

        public void Engranaje(float x, float y, float r, Color? c = null) {
            Circulo(x, y, r, c); Circulo(x, y, r * 0.35f, c);
            for (var i = 0; i < 8; i++) {
                var a = Mathf.PI / 4 * i;
                Linea(x + Mathf.Cos(a) * r, y + Mathf.Sin(a) * r, x + Mathf.Cos(a) * r * 1.35f, y + Mathf.Sin(a) * r * 1.35f, c, 6);
            }
        }

        public void Escudo(float x, float y, float s, Color? c = null) {
            Trazo(c, x - s, y - s, x + s, y - s, x + s, y + s * 0.2f);
            Curva(x + s, y + s * 0.2f, x + s * 0.8f, y + s * 1.1f, x, y + s * 1.4f, c, false);
            Curva(x, y + s * 1.4f, x - s * 0.8f, y + s * 1.1f, x - s, y + s * 0.2f, c, false);
            Linea(x - s, y + s * 0.2f, x - s, y - s, c);
        }

        public void Candado(float x, float y, float s, Color? c = null) {
            Caja(x - s, y, s * 2, s * 1.6f, null, c); Curva(x - s * 0.6f, y, x, y - s * 1.7f, x + s * 0.6f, y, c, false);
        }

        public void Llave(float x, float y, Color? c = null) {
            Circulo(x, y, 12, c); Linea(x + 12, y, x + 52, y, c); Linea(x + 40, y, x + 40, y + 10, c); Linea(x + 50, y, x + 50, y + 10, c);
        }

        public void Camion(float x, float y, Color? c = null) {
            Caja(x, y, 80, 46, null, c); Trazo(c, x + 80, y + 14, x + 106, y + 14, x + 118, y + 30, x + 118, y + 46, x + 80, y + 46);
            Circulo(x + 22, y + 52, 10, c); Circulo(x + 96, y + 52, 10, c);
        }

        public void Coche(float x, float y, Color? c = null) {
            Trazo(c, x, y + 22, x + 14, y + 22, x + 26, y, x + 68, y, x + 82, y + 22, x + 100, y + 22, x + 100, y + 40, x, y + 40, x, y + 22);
            Circulo(x + 24, y + 42, 9, c); Circulo(x + 76, y + 42, 9, c);
        }

        public void Botella(float x, float y, float h, Color? c = null) {
            var w = h * 0.36f;
            Trazo(c, x - w * 0.25f, y, x - w * 0.25f, y + h * 0.28f, x - w / 2, y + h * 0.45f, x - w / 2, y + h, x + w / 2, y + h, x + w / 2, y + h * 0.45f,
                  x + w * 0.25f, y + h * 0.28f, x + w * 0.25f, y, x - w * 0.25f, y);
        }

        public void Embudo(float x, float y, float w, float h, Color? c = null) {
            Trazo(c, x, y, x + w, y, x + w * 0.58f, y + h * 0.6f, x + w * 0.58f, y + h, x + w * 0.42f, y + h, x + w * 0.42f, y + h * 0.6f, x, y);
        }

        /// <summary>
        /// Una balanza con centro en x y un plato sobre cada brazo (lo que se pesa se dibuja encima, a unos 20 por
        /// arriba del brazo). Inclinacion -1: pesa mas la izquierda; 1: la derecha; 0: en equilibrio.
        /// </summary>
        public void Balanza(float x, float y, float ancho, int inclinacion, Color? c = null) {
            var d = inclinacion * 14f; var m = ancho / 2;
            Linea(x, y, x, y + 76, c); Linea(x - 26, y + 76, x + 26, y + 76, c, 4);
            Linea(x - m, y - d, x + m, y + d, c);
            foreach (var s in new[] { -1, 1 }) {
                var px = x + s * m; var py = y + s * d;
                Linea(px, py, px, py - 10, c);
                Curva(px - 30, py - 20, px, py, px + 30, py - 20, c, false);
            }
        }

        public void RelojDeArena(float x, float y, float h, Color? c = null) {
            var w = h * 0.3f;
            Linea(x - w - 4, y - h / 2, x + w + 4, y - h / 2, c, 4); Linea(x - w - 4, y + h / 2, x + w + 4, y + h / 2, c, 4);
            Trazo(c, x - w, y - h / 2, x + w, y + h / 2); Trazo(c, x + w, y - h / 2, x - w, y + h / 2);
            Linea(x - w * 0.5f, y + h / 2 - 5, x + w * 0.5f, y + h / 2 - 5, c ?? Mostaza, 5);
        }

        /// <summary>Un reloj de esfera a escala (el del icono no crece).</summary>
        public void Esfera(float x, float y, float r, Color? c = null) {
            Circulo(x, y, r, c); Linea(x, y, x, y - r * 0.65f, c); Linea(x, y, x + r * 0.45f, y + r * 0.25f, c);
        }

        public void Moneda(float x, float y, float r, Color? c = null) {
            Circulo(x, y, r, c ?? Mostaza); Texto(x, y + r * 0.45f, "$", r * 1.3f, c ?? Mostaza, true);
        }

        public void Tronco(float x, float y, float w, Color? c = null) {
            Caja(x, y, w, 26, null, c ?? Roja); Elipse(x + w, y + 13, 7, 13, c ?? Roja);
        }

        public void Espejo(float x, float y, float w, float h, Color? c = null) {
            Elipse(x + w / 2, y + h / 2, w / 2, h / 2, c); Linea(x + w * 0.3f, y + h * 0.3f, x + w * 0.5f, y + h * 0.18f, c ?? Gris, 2);
            Linea(x + w / 2, y + h, x + w / 2, y + h + 14, c); Linea(x + w * 0.25f, y + h + 14, x + w * 0.75f, y + h + 14, c, 4);
        }

        public void Sirena(float x, float y, Color? c = null) {
            Curva(x - 14, y, x, y - 34, x + 14, y, c ?? Roja, false); Linea(x - 20, y, x + 20, y, c ?? Roja, 4);
            foreach (var s in new[] { -1, 1 }) Linea(x + s * 22, y - 20, x + s * 34, y - 28, c ?? Roja, 2);
        }

        public void Escoba(float x, float y, Color? c = null) {
            Linea(x, y, x + 40, y + 50, c); Trazo(c, x + 30, y + 50, x + 52, y + 44, x + 60, y + 66, x + 36, y + 72, x + 30, y + 50);
        }

        public void Carrito(float x, float y, Color? c = null) {
            Trazo(c, x, y, x + 12, y, x + 22, y + 34, x + 62, y + 34, x + 70, y + 8, x + 16, y + 8);
            Circulo(x + 28, y + 44, 6, c); Circulo(x + 56, y + 44, 6, c);
        }

        public void Taza(float x, float y, Color? c = null) {
            Trazo(c, x, y, x + 4, y + 34, x + 36, y + 34, x + 40, y, x, y); Curva(x + 40, y + 6, x + 58, y + 16, x + 38, y + 26, c, false);
        }

        public void Brujula(float x, float y, float r, Color? c = null) {
            Circulo(x, y, r, c); Trazo(c ?? Mostaza, x, y - r * 0.7f, x + r * 0.22f, y, x, y + r * 0.7f, x - r * 0.22f, y, x, y - r * 0.7f);
        }

        public void Licuadora(float x, float y, Color? c = null) {
            Trazo(c, x, y, x + 12, y + 70, x + 48, y + 70, x + 60, y, x, y); Caja(x + 4, y + 70, 52, 30, null, c);
            Linea(x + 18, y + 50, x + 42, y + 62, c ?? Mostaza); Linea(x + 42, y + 50, x + 18, y + 62, c ?? Mostaza);
        }

        public void LlaveInglesa(float x, float y, Color? c = null) {
            Linea(x, y + 46, x + 34, y + 12, c, 6); Curva(x + 28, y + 2, x + 40, y + 12, x + 50, y + 6, c, false); Curva(x + 30, y, x + 46, y - 12, x + 52, y + 2, c, false);
        }

        public void Ladrillos(float x, float y, Color? c = null) {
            for (var f = 0; f < 3; f++) for (var k = 0; k < 3 - f % 2; k++) D.Rect(X(x + k * 34 + (f % 2) * 17), Y(y + f * 18), 30 * _k, 14 * _k, c ?? Tiza, 2.5f * _k, null, false, 2 * _k);
        }

        public void Termometro(float x, float y, float h, Color? c = null) {
            Trazo(c, x - 6, y + h - 14, x - 6, y, x + 6, y, x + 6, y + h - 14); Circulo(x, y + h - 4, 13, c); Linea(x, y + h - 6, x, y + h * 0.35f, c ?? Roja, 5);
        }

        public void Telarana(float x, float y, float r, Color? c = null) {
            for (var i = 0; i < 4; i++) { var a = Mathf.PI / 6 * i; Linea(x, y, x + Mathf.Cos(a) * r, y + Mathf.Sin(a) * r, c ?? Gris, 2); }
            foreach (var f in new[] { 0.5f, 0.9f }) Curva(x + r * f, y, x + r * f * 0.6f, y + r * f * 0.6f, x, y + r * f, c ?? Gris, false);
        }

        // ================================================================ tecnologia

        /// <summary>Una ventana de navegador con su barra de direccion.</summary>
        public void Navegador(float x, float y, float w, float h, Color? c = null, string direccion = null) {
            Caja(x, y, w, h, null, c); Linea(x, y + 26, x + w, y + 26, c, 2);
            for (var i = 0; i < 3; i++) Punto(x + 14 + i * 14, y + 13, 4, c);
            D.Rect(X(x + 60), Y(y + 5), (w - 72) * _k, 16 * _k, c ?? Tiza, 2 * _k, null, false, 8 * _k);
            if (direccion != null) Texto(x + 60 + (w - 72) / 2, y + 19, direccion, 12, c ?? Tiza, true);
        }

        public void Telefono(float x, float y, float w, float h, Color? c = null) {
            D.Rect(X(x), Y(y), w * _k, h * _k, c ?? Tiza, 3 * _k, new Color(1, 1, 1, 0.04f), false, 12 * _k);
            Linea(x + w * 0.35f, y + 10, x + w * 0.65f, y + 10, c, 3); Circulo(x + w / 2, y + h - 12, 4, c, false, 2);
        }

        /// <summary>El icono de una app: un cuadrado redondeado.</summary>
        public void App(float x, float y, float s, Color? c = null) { D.Rect(X(x), Y(y), s * _k, s * _k, c ?? Cian, 3 * _k, null, false, s * 0.28f * _k); }

        public void Servidor(float x, float y, float w, float h, Color? c = null) {
            Caja(x, y, w, h, null, c);
            for (var i = 1; i <= 3; i++) { Linea(x + 10, y + h * i / 4, x + w - 22, y + h * i / 4, c, 2); Punto(x + w - 12, y + h * i / 4, 3, c ?? Cian); }
        }

        public void Nube(float x, float y, float s, Color? c = null) {
            Curva(x - s, y, x - s * 1.1f, y - s * 0.8f, x - s * 0.4f, y - s * 0.7f, c, false);
            Curva(x - s * 0.4f, y - s * 0.7f, x, y - s * 1.4f, x + s * 0.5f, y - s * 0.7f, c, false);
            Curva(x + s * 0.5f, y - s * 0.7f, x + s * 1.2f, y - s * 0.8f, x + s, y, c, false);
            Linea(x - s, y, x + s, y, c);
        }

        public void Camara(float x, float y, Color? c = null) {
            Caja(x, y, 60, 36, null, c); Circulo(x + 30, y + 18, 10, c); Trazo(c, x + 60, y + 10, x + 80, y + 2, x + 80, y + 34, x + 60, y + 26);
        }

        // ================================================================ graficas

        /// <summary>Los dos ejes de una grafica, con el origen abajo a la izquierda del cuadro.</summary>
        public void Ejes(float x, float y, float w, float h, Color? c = null) { Trazo(c ?? Gris, x, y, x, y + h, x + w, y + h); }

        /// <summary>Barras de 0 a 1 dentro del cuadro.</summary>
        public void Barras(float x, float y, float w, float h, Color? c, params float[] valores) {
            Ejes(x, y, w, h);
            var bw = w / (valores.Length * 1.5f + 0.5f);
            for (var i = 0; i < valores.Length; i++) Bloque(x + bw * (0.5f + i * 1.5f), y + h * (1 - valores[i]), bw, h * valores[i] - 2, c ?? Cian);
        }

        /// <summary>Una linea que une valores de 0 a 1 dentro del cuadro.</summary>
        public void Grafica(float x, float y, float w, float h, Color? c, params float[] valores) {
            Ejes(x, y, w, h);
            var xy = new float[valores.Length * 2];
            for (var i = 0; i < valores.Length; i++) { xy[i * 2] = x + w * i / (valores.Length - 1); xy[i * 2 + 1] = y + h * (1 - valores[i]); }
            Trazo(c ?? Cian, xy);
        }

        /// <summary>Una barra de medidor (las del juego), llena de 0 a 1.</summary>
        public void Medidor(float x, float y, float w, float lleno, Color? c = null, string t = null) {
            D.Rect(X(x), Y(y), w * _k, 22 * _k, Tiza, 3 * _k, null, false, 8 * _k);
            if (lleno > 0) Bloque(x + 4, y + 4, (w - 8) * Mathf.Clamp01(lleno), 14, c ?? Cian);
            if (t != null) Texto(x + w / 2, y - 8, t, 15, c ?? Tiza, true);
        }
    }
}
