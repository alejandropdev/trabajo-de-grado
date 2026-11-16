#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// SOLO EN EL EDITOR (2026-11-14). Las laminas de muestra de las tres propuestas de direccion de arte
    /// (docs/08-arte/ART_DIRECTION_PROPOSALS.md). Las tres dibujan el MISMO tema para poder compararlas: la ventana de las
    /// 17:00, una trabajadora del Sector 7 y un boton en tres estados. Todo esta dibujado desde cero con las primitivas de
    /// DibujoUI; no reutiliza ningun dibujo del juego. No es arte del juego: solo existe para la puerta de aprobacion.
    ///   A · Senaletica de bunker iluminada: formas rellenas a dos tonos con sombra dura, contorno de tinta, el neon como
    ///       tubo de rotulo (solo lo que informa brilla), franjas de peligro mostaza.
    ///   B · Plano tecnico de turno nocturno: cianotipo, linea con jerarquia de grosores, sombra con rayado, cotas y
    ///       rotulos de plano; el neon es la mesa de luz y lo que esta encendido.
    ///   C · Cartel serigrafiado fluorescente: tres tintas planas sobre papel, desregistro de 2 px, semitono; el neon
    ///       es una tinta fluorescente.
    /// </summary>
    public sealed partial class CapturadorDeGaleria {
        // ------------------------------------------------ paleta de la fusion (canon Bunker v2 §11.4.1 + neon)
        private static readonly Color Fondo = H(0x1B2A31), Fondo2 = H(0x233B42), Pared = H(0x2E4A52), Hormigon = H(0x3A4A4E);
        private static readonly Color Cian = H(0x5FD8F5), CianClaro = H(0x8FEAFF);
        private static readonly Color Mostaza = H(0xC9A227), MostazaClara = H(0xE3C24F), MostazaVieja = H(0x8F6F14);
        private static readonly Color Naranja = H(0xE8703A), Rojo = H(0xC94F3D), Ambar = H(0xF2C14E);
        private static readonly Color Camisa = H(0xDDE6E8), Overol = H(0x3E5158), Overol2 = H(0x2F3E52), Cuero = H(0x1F2529);
        private static readonly Color Texto = H(0xEAF6FA), Tinta = H(0x10191D), Piel = H(0xC98E6B), PielSombra = H(0x93603F);
        // B: cianotipo
        private static readonly Color Prusia = H(0x0E2A38), LineaPlano = H(0xCDEFF6);
        // C: papel y tintas
        private static readonly Color Papel = H(0xE4D8BD), TintaPizarra = H(0x24343B), Fluor = H(0x3FE6F2);

        private static Color H(int rgb) { return new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255); }
        private static Color A(Color c, float a) { c.a = a; return c; }
        private static Vector2 V(float x, float y) { return new Vector2(x, y); }

        private enum Estilo { Senaletica, Plano, Serigrafia }

        // ================================================================ las tres laminas

        private void PropuestaA(RectTransform capa) {
            Encabezado(capa, Fondo, "PROPUESTA A", "SEÑALÉTICA DE BÚNKER ILUMINADA", Cian,
                "Formas rellenas a dos tonos con sombra dura y contorno de tinta. El neón es un tubo de rótulo: solo brilla lo que informa o se puede tocar.");
            Escena(capa, Estilo.Senaletica);
            Persona(capa, Estilo.Senaletica);
            Botones(capa, Estilo.Senaletica);
            Muestrario(capa, new[] { Fondo, Pared, Hormigon, Overol, Camisa, Cian, Mostaza, Rojo },
                new[] { "fondo 1B2A31", "pared 2E4A52", "hormigón 3A4A4E", "overol 3E5158", "camisa DDE6E8", "neón 5FD8F5", "peligro C9A227", "prohibido C94F3D" },
                "Contorno 5 px fuera · 2 px dentro · luz por la derecha, borde de sombra duro · rim cian a la izquierda · mostaza ≤ 12 % del encuadre");
        }

        private void PropuestaB(RectTransform capa) {
            Encabezado(capa, Prusia, "PROPUESTA B", "PLANO TÉCNICO DE TURNO NOCTURNO", LineaPlano,
                "Cianotipo de delineante: línea con tres grosores, sombra con rayado a 45°, cotas y rótulos. El neón es la mesa de luz y lo que está encendido.");
            Escena(capa, Estilo.Plano);
            Persona(capa, Estilo.Plano);
            Botones(capa, Estilo.Plano);
            Muestrario(capa, new[] { Prusia, H(0x163A4B), LineaPlano, A(LineaPlano, 0.45f), Cian, Mostaza, Rojo, Ambar },
                new[] { "papel 0E2A38", "papel luz 163A4B", "línea CDEFF6", "construcción", "encendido 5FD8F5", "aviso C9A227", "prohibido C94F3D", "lámpara F2C14E" },
                "Contorno 3,5 px · detalle 1,5 px · construcción 1 px discontinua · rayado cada 9 px · solo lo encendido lleva color");
        }

        private void PropuestaC(RectTransform capa) {
            Encabezado(capa, Fondo, "PROPUESTA C", "CARTEL SERIGRAFIADO FLUORESCENTE", Fluor,
                "Tres tintas planas sobre papel, desregistro de 2 px y trama de semitono. Sin contornos: la silueta lo hace todo. El neón es una tinta fluorescente.");
            Escena(capa, Estilo.Serigrafia);
            Persona(capa, Estilo.Serigrafia);
            Botones(capa, Estilo.Serigrafia);
            Muestrario(capa, new[] { Papel, TintaPizarra, Mostaza, Fluor, Rojo, Fondo, Hormigon, Camisa },
                new[] { "papel E4D8BD", "tinta 24343B", "tinta C9A227", "flúor 3FE6F2", "prohibido C94F3D", "muro 1B2A31", "hormigón 3A4A4E", "luz DDE6E8" },
                "Máximo 3 tintas + papel por dibujo · desregistro 2 px a la izquierda · semitono en vez de degradado · sin contorno de línea");
        }

        // ================================================================ maquetacion comun

        private void Encabezado(RectTransform capa, Color fondo, string etiqueta, string titulo, Color acento, string lema) {
            capa.GetComponent<UnityEngine.UI.Image>().color = fondo;
            var l = Lamina(capa, 0, 0, 1920, 90);
            l.Texto(40, 14, 400, 24, etiqueta, 18, acento, TextAlignmentOptions.TopLeft, FuentesNexus.InterfazNegrita, true);
            l.Texto(40, 36, 900, 44, titulo, 34, Texto, TextAlignmentOptions.TopLeft, FuentesNexus.Display, true);
            l.Texto(980, 20, 900, 60, lema, 17, A(Texto, 0.75f), TextAlignmentOptions.TopLeft, FuentesNexus.Interfaz);
        }

        private void Muestrario(RectTransform capa, Color[] colores, string[] nombres, string reglas) {
            var l = Lamina(capa, 0, 960, 1920, 120);
            for (var i = 0; i < colores.Length; i++) {
                float x = 40 + i * 150;
                l.Dibujo.Rect(x, 8, 132, 40, A(Texto, 0.25f), 1, colores[i], false, 2);
                l.Texto(x, 52, 140, 22, nombres[i], 13, A(Texto, 0.7f), TextAlignmentOptions.TopLeft, FuentesNexus.Interfaz);
            }
            l.Texto(1260, 8, 640, 80, reglas, 15, A(Texto, 0.8f), TextAlignmentOptions.TopLeft, FuentesNexus.Interfaz);
        }

        // ================================================================ la escena: la ventana de las 17:00

        // Lienzo de 1200×860 en (40, 96). La ventana va de (150,70) a (930,500); el horizonte en y = 392.
        private const float VX0 = 150, VY0 = 70, VX1 = 930, VY1 = 500, Horizonte = 392;

        // Edificios cercanos: x, ancho, altura del tejado, remate (0 plano, 1 antena, 2 deposito de agua, 3 escalonado).
        private static readonly float[,] Cercanos = {
            { 150, 118, 300, 1 }, { 268, 92, 346, 0 }, { 360, 146, 262, 2 }, { 506, 74, 362, 0 },
            { 642, 70, 330, 0 }, { 712, 112, 284, 3 }, { 824, 106, 318, 1 }
        };
        // Ventanas encendidas: indice del edificio, columna, fila (desde el tejado). Pocas y agrupadas: oficinas, no ruido.
        private static readonly int[,] Encendidas = {
            { 0, 1, 2 }, { 0, 2, 2 }, { 0, 2, 3 }, { 2, 0, 1 }, { 2, 1, 1 }, { 2, 3, 4 }, { 3, 0, 1 },
            { 5, 1, 2 }, { 5, 2, 2 }, { 5, 2, 3 }, { 6, 0, 3 }
        };

        private void Escena(RectTransform capa, Estilo e) {
            // DibujoUI pinta todos los trazos encima de todos los rellenos: lo que va detras (el muro de bloques) va
            // en su propia lamina, debajo.
            if (e == Estilo.Senaletica) MuroDeBloques(Lamina(capa, 40, 96, 1200, 860).Dibujo);
            var l = Lamina(capa, 40, 96, 1200, 860);
            var d = l.Dibujo;
            switch (e) {
                case Estilo.Senaletica: EscenaA(l, d); break;
                case Estilo.Plano: EscenaB(l, d); break;
                default: EscenaC(l, d); break;
            }
        }

        private static List<Vector2> Silueta(int i, float dx = 0, float dy = 0) {
            float x = Cercanos[i, 0] + dx, w = Cercanos[i, 1], top = Cercanos[i, 2] + dy, b = VY1 + dy;
            var p = new List<Vector2>();
            if ((int)Cercanos[i, 3] == 3) {
                p.Add(V(x, b)); p.Add(V(x, top + 40)); p.Add(V(x + w * 0.25f, top + 40)); p.Add(V(x + w * 0.25f, top + 18));
                p.Add(V(x + w * 0.5f, top + 18)); p.Add(V(x + w * 0.5f, top)); p.Add(V(x + w, top)); p.Add(V(x + w, b));
            } else {
                p.Add(V(x, b)); p.Add(V(x, top)); p.Add(V(x + w, top)); p.Add(V(x + w, b));
            }
            return p;
        }

        private static void Remates(DibujoUI d, int i, Color c, float g, float dx = 0, float dy = 0) {
            float x = Cercanos[i, 0] + dx, w = Cercanos[i, 1], top = Cercanos[i, 2] + dy;
            switch ((int)Cercanos[i, 3]) {
                case 1:
                    d.Linea(x + w * 0.62f, top, x + w * 0.62f, top - 58, c, g);
                    d.Linea(x + w * 0.62f - 12, top - 34, x + w * 0.62f + 12, top - 34, c, g * 0.7f);
                    break;
                case 2:   // deposito de agua sobre patas: el remate que identifica la ciudad
                    Rellenar(d, c, x + 34, top, x + 40, top - 26, x + 46, top);
                    Rellenar(d, c, x + 70, top, x + 64, top - 26, x + 58, top);
                    Rellenar(d, c, x + 30, top - 24, x + 74, top - 24, x + 74, top - 60, x + 52, top - 74, x + 30, top - 60);
                    break;
            }
        }

        private static Rect VentanaDe(int i, int col, int fila) {
            float x = Cercanos[i, 0], top = Cercanos[i, 2];
            return new Rect(x + 12 + col * 24, top + 14 + fila * 26, 12, 15);
        }

        // ---------------------------------------------------------------- A · senaletica

        /// <summary>Muro de bloques de hormigon: llagas desfasadas, no ruido.</summary>
        private static void MuroDeBloques(DibujoUI d) {
            d.Poligono(Caja(0, 0, 1200, 860), Pared);
            for (var f = 0; f < 10; f++) {
                float y = f * 86;
                d.Linea(0, y, 1200, y, A(Hormigon, 0.9f), 3);
                for (var x = (f % 2) * 90f; x < 1200; x += 180) d.Linea(x, y, x, y + 86, A(Hormigon, 0.9f), 3);
            }
        }

        private void EscenaA(Lamina l, DibujoUI d) {
            // Cielo en cuatro bandas duras (sin degradado) y el sol medio hundido.
            float[] cortes = { VY0, 180, 270, 340, Horizonte };
            Color[] bandas = { Fondo2, H(0x4A4A57), H(0x8A5A3C), Naranja };
            for (var i = 0; i < 4; i++) d.Poligono(Caja(VX0, cortes[i], VX1 - VX0, cortes[i + 1] - cortes[i]), bandas[i]);
            d.Poligono(Caja(VX0, Horizonte, VX1 - VX0, VY1 - Horizonte), H(0x6B3A2A));
            Disco(d, 560, Horizonte, 64, Ambar, true);
            Disco(d, 560, Horizonte, 46, H(0xF7D77A), true);
            // Plano lejano: una sola masa de tono medio.
            Rellenar(d, Overol2, VX0, VY1, VX0, 360, 230, 360, 230, 330, 330, 330, 330, 372, 470, 372, 470, 348, 620, 348, 620, 366, 760, 366, 760, 340, VX1, 340, VX1, VY1);
            // Plano cercano: tinta, con un filo de luz (rim) en la cara que mira al sol.
            for (var i = 0; i < Cercanos.GetLength(0); i++) {
                Rellenar(d, Tinta, Silueta(i));
                Remates(d, i, Tinta, 6);
                float x = Cercanos[i, 0], w = Cercanos[i, 1], top = Cercanos[i, 2];
                var haciaElSol = x + w < 560;
                var bx = haciaElSol ? x + w - 7 : x;
                d.Poligono(Caja(bx, top + ((int)Cercanos[i, 3] == 3 && !haciaElSol ? 40 : 0), 7, VY1 - top), MostazaVieja);
            }
            for (var k = 0; k < Encendidas.GetLength(0); k++) {
                var r = VentanaDe(Encendidas[k, 0], Encendidas[k, 1], Encendidas[k, 2]);
                d.Poligono(Caja(r.x, r.y, r.width, r.height), Ambar);
            }
            // Marco de la ventana del bunker: tinta gruesa por fuera, chapa con remaches por dentro.
            d.Rect(VX0 - 14, VY0 - 14, VX1 - VX0 + 28, VY1 - VY0 + 28, Tinta, 14, null, false, 6);
            d.Rect(VX0 - 4, VY0 - 4, VX1 - VX0 + 8, VY1 - VY0 + 8, Hormigon, 8, null, false, 2);
            d.Linea(540, VY0, 540, VY1, Hormigon, 12); d.Linea(540, VY0, 540, VY1, Tinta, 3);
            for (var x = VX0 + 20; x < VX1; x += 64) { Disco(d, x, VY0 - 4, 4, Camisa, true); Disco(d, x, VY1 + 4, 4, Camisa, true); }
            // Alfeizar con franja de peligro: la unica mostaza fuerte del encuadre.
            d.Poligono(Caja(VX0 - 40, VY1 + 18, VX1 - VX0 + 80, 30), Tinta);
            for (var x = VX0 - 40; x < VX1 + 30; x += 44) Rellenar(d, Mostaza, x + 16, VY1 + 20, x + 38, VY1 + 20, x + 22, VY1 + 46, x, VY1 + 46);
            // Mesa y taza esmaltada a dos tonos, contorno de 5 px.
            d.Poligono(Caja(0, 640, 1200, 220), H(0x3B2A1E));
            d.Poligono(Caja(0, 640, 1200, 16), H(0x5A402B));
            d.Linea(0, 640, 1200, 640, Tinta, 5);
            Taza(d, 300, 568, Camisa, H(0x9FB0B5), Cian, Tinta, 5);
            // Tablilla portapapeles.
            var tab = new List<Vector2> { V(430, 600), V(560, 590), V(572, 650), V(442, 660) };
            Rellenar(d, H(0x7A5A3A), tab);
            Rellenar(d, Camisa, 444, 606, 552, 598, 562, 646, 454, 654);
            d.Polilinea(new List<Vector2>(tab) { tab[0] }, Tinta, 5);
            d.Poligono(Caja(482, 584, 40, 14), H(0x9FB0B5));
            for (var i = 0; i < 3; i++) d.Linea(462, 618 + i * 10, 540, 612 + i * 10, A(Tinta, 0.6f), 2);
            // El reloj como tubo de neon: es lo que informa (la hora), por eso brilla.
            float cx = 1060, cy = 230;
            Disco(d, cx, cy, 92, Tinta, true);
            TuboCirculo(d, cx, cy, 74, Cian, 5);
            TuboLinea(d, cx, cy, cx, cy - 52, Cian, 5);
            TuboLinea(d, cx, cy, cx - 32, cy + 20, Cian, 5);
            for (var h = 0; h < 12; h++) {
                var a = h * Mathf.PI / 6;
                d.Linea(cx + Mathf.Sin(a) * 60, cy - Mathf.Cos(a) * 60, cx + Mathf.Sin(a) * 68, cy - Mathf.Cos(a) * 68, A(Cian, 0.8f), 3);
            }
            l.Texto(990, 340, 140, 30, "17:00", 28, CianClaro, TextAlignmentOptions.Center, FuentesNexus.Display, true);
            // Cartel de propaganda pegado al muro (papel plano, sin sombreado).
            Rellenar(d, Papel, 990, 420, 1150, 414, 1156, 600, 994, 604);
            d.Poligono(Caja(1004, 428, 140, 26), Mostaza);
            l.Texto(1004, 462, 146, 130, "DOCUMENTE HOY.\nSOBREVIVA MAÑANA.", 20, TintaPizarra, TextAlignmentOptions.TopLeft, FuentesNexus.InterfazNegrita, true);
        }

        // ---------------------------------------------------------------- B · plano

        private void EscenaB(Lamina l, DibujoUI d) {
            var linea = LineaPlano;
            var tenue = A(LineaPlano, 0.45f);
            d.Poligono(Caja(0, 0, 1200, 860), Prusia);
            // Mesa de luz: anillos escalonados, no un degradado.
            for (var i = 6; i >= 1; i--) Disco(d, 560, 330, i * 70, A(H(0x1E5266), 0.12f), true);
            for (var x = 0f; x <= 1200; x += 20) d.Linea(x, 0, x, 860, A(linea, x % 100 == 0 ? 0.10f : 0.04f), 1);
            for (var y = 0f; y <= 860; y += 20) d.Linea(0, y, 1200, y, A(linea, y % 100 == 0 ? 0.10f : 0.04f), 1);
            // Muro en seccion: dos lineas por lado (grueso del muro) y rayado del corte.
            d.Rect(VX0 - 30, VY0 - 30, VX1 - VX0 + 60, VY1 - VY0 + 60, linea, 3.5f, null, false, 0);
            d.Rect(VX0, VY0, VX1 - VX0, VY1 - VY0, linea, 3.5f, null, false, 0);
            Rayar(d, Caja(VX0 - 30, VY0 - 30, VX1 - VX0 + 60, 30), 9, tenue, 1.2f);
            Rayar(d, Caja(VX0 - 30, VY1, VX1 - VX0 + 60, 30), 9, tenue, 1.2f);
            d.Linea(540, VY0, 540, VY1, linea, 2);
            // El sol como construccion: circulo, cruz y su recorrido discontinuo hasta las 17:00.
            d.Circulo(560, Horizonte, 58, linea, 2);
            d.Linea(480, Horizonte, 640, Horizonte, tenue, 1, true);
            d.Linea(560, Horizonte - 80, 560, Horizonte + 10, tenue, 1, true);
            d.Curva(V(260, 170), V(420, 110), V(552, Horizonte - 62), A(Ambar, 0.9f), 1.5f, true, true);
            Cota(l, d, 260, 120, 552, 120, "RECORRIDO 12:00 → 17:00", Ambar);
            d.Linea(VX0, Horizonte, VX1, Horizonte, tenue, 1, true);
            // Edificios: solo contorno; la cara en sombra (la que no da al sol) rayada.
            for (var i = 0; i < Cercanos.GetLength(0); i++) {
                var s = Silueta(i);
                d.Polilinea(new List<Vector2>(s) { s[0] }, linea, 3.5f);
                Remates(d, i, linea, 2);
                float x = Cercanos[i, 0], w = Cercanos[i, 1], top = Cercanos[i, 2];
                var sombra = x + w < 560 ? Caja(x, top, w * 0.35f, VY1 - top) : Caja(x + w * 0.65f, top, w * 0.35f, VY1 - top);
                Rayar(d, sombra, 9, tenue, 1.2f);
            }
            // Lo encendido es lo unico con color: las ventanas como pequeños neones.
            for (var k = 0; k < Encendidas.GetLength(0); k++) {
                var r = VentanaDe(Encendidas[k, 0], Encendidas[k, 1], Encendidas[k, 2]);
                d.Poligono(Caja(r.x - 3, r.y - 3, r.width + 6, r.height + 6), A(Cian, 0.25f));
                d.Poligono(Caja(r.x, r.y, r.width, r.height), Cian);
            }
            Cota(l, d, VX0, VY1 + 70, VX1, VY1 + 70, "VANO 7,80 m", linea);
            // Mesa y taza como despiece con lineas de referencia.
            d.Linea(0, 660, 1200, 660, linea, 3.5f);
            d.Linea(0, 672, 1200, 672, tenue, 1.5f);
            Taza(d, 300, 588, null, null, Cian, linea, 3.5f);
            Rayar(d, Caja(338, 596, 30, 60), 9, tenue, 1.2f);
            Referencia(l, d, 336, 610, 200, 740, "TAZA ESMALTADA · 0,3 L");
            d.Rect(440, 606, 130, 54, linea, 3.5f, null, false, 2);
            Referencia(l, d, 560, 620, 640, 740, "TABLILLA · TURNO 2");
            // El reloj: circunferencia con divisiones; las agujas encendidas.
            float cx = 1060, cy = 230;
            d.Circulo(cx, cy, 80, linea, 3.5f);
            d.Circulo(cx, cy, 70, tenue, 1, null, true);
            for (var h = 0; h < 60; h++) {
                var a = h * Mathf.PI / 30;
                var largo = h % 5 == 0 ? 12 : 5;
                d.Linea(cx + Mathf.Sin(a) * (80 - largo), cy - Mathf.Cos(a) * (80 - largo), cx + Mathf.Sin(a) * 80, cy - Mathf.Cos(a) * 80, linea, h % 5 == 0 ? 2 : 1);
            }
            TuboLinea(d, cx, cy, cx, cy - 56, Cian, 3);
            TuboLinea(d, cx, cy, cx - 34, cy + 20, Cian, 3);
            l.Texto(970, 330, 180, 24, "FIG. 3 · 17:00 h", 16, linea, TextAlignmentOptions.Center, FuentesNexus.InterfazNegrita, true);
            // Cajetin del plano.
            d.Rect(900, 700, 280, 140, linea, 2, null, false, 0);
            d.Linea(900, 740, 1180, 740, linea, 1.5f); d.Linea(1040, 740, 1040, 840, linea, 1.5f);
            l.Texto(912, 708, 260, 28, "NEXUS · SECTOR 7", 18, linea, TextAlignmentOptions.MidlineLeft, FuentesNexus.InterfazNegrita, true);
            l.Texto(912, 750, 120, 80, "VENTANA\nOFICINA 2\nESC 1:50", 14, tenue, TextAlignmentOptions.TopLeft, FuentesNexus.Interfaz);
            l.Texto(1052, 750, 120, 80, "HOJA 03\nREV. B\nTURNO NOCHE", 14, tenue, TextAlignmentOptions.TopLeft, FuentesNexus.Interfaz);
        }

        // ---------------------------------------------------------------- C · serigrafia

        private void EscenaC(Lamina l, DibujoUI d) {
            const float DX = -3, DY = -2;   // el desregistro de la tinta fluorescente
            d.Poligono(Caja(0, 0, 1200, 860), Fondo);
            // El cartel: papel con un borde irregular de imprenta.
            Rellenar(d, Papel, 60, 30, 1150, 24, 1158, 830, 54, 836);
            // Cielo: trama de semitono de tinta pizarra, mas densa arriba (sustituye al degradado).
            Semitono(d, VX0, VY0, VX1 - VX0, Horizonte - VY0, 13, (x, y) => Mathf.Lerp(0.95f, 0.15f, (y - VY0) / (Horizonte - VY0)), TintaPizarra);
            // Sol: disco fluorescente desregistrado y su mitad inferior en mostaza.
            Disco(d, 560 + DX, Horizonte + DY, 74, Fluor, true);
            Disco(d, 560, Horizonte, 70, Mostaza, true);
            Semitono(d, 490, Horizonte - 70, 140, 70, 9, (x, y) => (new Vector2(x - 560, y - Horizonte).magnitude < 66) ? 0.55f : 0f, Papel);
            // Ciudad: una sola tinta, sin contorno; ventanas en reserva (papel).
            for (var i = 0; i < Cercanos.GetLength(0); i++) {
                Rellenar(d, A(Fluor, 0.9f), Silueta(i, DX, DY));
                Remates(d, i, A(Fluor, 0.9f), 6, DX, DY);
            }
            for (var i = 0; i < Cercanos.GetLength(0); i++) { Rellenar(d, TintaPizarra, Silueta(i)); Remates(d, i, TintaPizarra, 6); }
            for (var k = 0; k < Encendidas.GetLength(0); k++) {
                var r = VentanaDe(Encendidas[k, 0], Encendidas[k, 1], Encendidas[k, 2]);
                d.Poligono(Caja(r.x, r.y, r.width, r.height), Papel);
            }
            d.Poligono(Caja(VX0, VY1, VX1 - VX0, 8), TintaPizarra);
            // Faja del eslogan: tinta mostaza con letra en tinta, como un cartel de seguridad.
            d.Poligono(Caja(60, 560, 1098, 120), Mostaza);
            l.Texto(90, 566, 1060, 110, "REVISE ANTES DE DESPLEGAR", 74, TintaPizarra, TextAlignmentOptions.MidlineLeft, FuentesNexus.Display, true);
            // Taza y tablilla: tintas planas, sombra en semitono.
            Taza(d, 300 + DX, 720 + DY, Fluor, null, null, null, 0);
            Taza(d, 300, 720, TintaPizarra, null, Papel, null, 0);
            Semitono(d, 300, 720, 40, 64, 7, (x, y) => 0.7f, Papel);
            Rellenar(d, TintaPizarra, 440, 740, 590, 728, 600, 812, 450, 822);
            Rellenar(d, Papel, 458, 752, 578, 742, 586, 806, 466, 814);
            for (var i = 0; i < 3; i++) d.Linea(476, 768 + i * 12, 566, 760 + i * 12, TintaPizarra, 4);
            // Reloj: anillo de tinta, agujas fluorescentes desregistradas.
            float cx = 1000, cy = 230;
            Disco(d, cx, cy, 96, TintaPizarra, true);
            Disco(d, cx, cy, 78, Papel, true);
            Disco(d, cx + DX, cy + DY, 10, Fluor, true);
            d.Linea(cx + DX, cy + DY, cx + DX, cy - 58 + DY, Fluor, 10);
            d.Linea(cx + DX, cy + DY, cx - 38 + DX, cy + 22 + DY, Fluor, 10);
            d.Linea(cx, cy, cx, cy - 58, TintaPizarra, 4);
            d.Linea(cx, cy, cx - 38, cy + 22, TintaPizarra, 4);
            l.Texto(900, 330, 200, 50, "17:00", 44, TintaPizarra, TextAlignmentOptions.Center, FuentesNexus.Display, true);
            l.Texto(905, 382, 190, 26, "SECTOR 7 · FIN DE TURNO", 14, TintaPizarra, TextAlignmentOptions.Center, FuentesNexus.InterfazNegrita, true);
        }

        // ================================================================ la trabajadora del Sector 7

        private void Persona(RectTransform capa, Estilo e) {
            var fondo = e == Estilo.Plano ? H(0x123444) : e == Estilo.Serigrafia ? Papel : Fondo2;
            var b = Bloque(capa, 1280, 96, 600, 520, fondo);
            var l = Lamina(b, 0, 0, 600, 520);
            var d = l.Dibujo;
            // La figura mide 340 de alto (7 cabezas de ~48) en su caja 0..232 × 0..340; se dibuja a ×1.25.
            const float k = 1.25f, ox = 50, oy = 40;
            if (e == Estilo.Plano) {
                for (var i = 0; i <= 7; i++) {
                    var y = oy + i * 48.6f * k;
                    d.Linea(16, y, 30, y, A(LineaPlano, 0.7f), 1.5f);
                    if (i < 7) l.Texto(0, y + 14, 16, 30, (i + 1).ToString(), 13, A(LineaPlano, 0.7f), TextAlignmentOptions.Center, FuentesNexus.Interfaz);
                }
                d.Linea(23, oy, 23, oy + 340 * k, A(LineaPlano, 0.7f), 1.5f);
            }
            Trabajadora(d, e, ox, oy, k, false, Tinta);
            // A la derecha, la prueba de silueta del canon: rellena en un solo color, pequeña, debe leerse igual.
            var tinta = e == Estilo.Serigrafia ? TintaPizarra : e == Estilo.Plano ? LineaPlano : Tinta;
            Trabajadora(d, e, 430, 200, 0.5f, true, tinta);
            l.Texto(380, 380, 200, 40, "prueba de silueta", 14, e == Estilo.Serigrafia ? TintaPizarra : A(Texto, 0.6f), TextAlignmentOptions.Center, FuentesNexus.Interfaz);
        }

        /// <summary>Supervisora de turno, señalando al frente (pose de cartel del canon §11.4.3). Coordenadas propias 0..232 × 0..340.</summary>
        private static void Trabajadora(DibujoUI d, Estilo e, float ox, float oy, float k, bool silueta, Color colorSilueta) {
            Func<float[], List<Vector2>> P = xy => {
                var r = new List<Vector2>();
                for (var i = 0; i + 1 < xy.Length; i += 2) r.Add(V(ox + xy[i] * k, oy + xy[i + 1] * k));
                return r;
            };
            // Las piezas, de atras hacia delante.
            var brazoAtras = P(new float[] { 60, 74, 74, 80, 70, 162, 54, 160 });
            var piernaI = P(new float[] { 70, 172, 99, 172, 95, 320, 70, 320 });
            var piernaD = P(new float[] { 101, 172, 132, 172, 136, 320, 108, 320 });
            var botaI = P(new float[] { 64, 318, 98, 318, 100, 338, 56, 338 });
            var botaD = P(new float[] { 106, 318, 138, 318, 148, 338, 106, 338 });
            var torso = P(new float[] { 56, 70, 144, 70, 132, 176, 68, 176 });
            var camisa = P(new float[] { 66, 68, 134, 68, 126, 112, 74, 112 });
            var peto = P(new float[] { 78, 104, 122, 104, 130, 176, 70, 176 });
            var brazoDelante = P(new float[] { 134, 70, 146, 64, 208, 76, 206, 92, 140, 90 });
            var antebrazo = P(new float[] { 170, 72, 208, 76, 206, 92, 168, 90 });
            var mano = P(new float[] { 206, 76, 232, 80, 232, 86, 214, 88, 206, 92 });
            var cabeza = new List<Vector2>();
            for (var i = 0; i < 24; i++) { var a = i * Mathf.PI * 2 / 24; cabeza.Add(V(ox + (100 + Mathf.Cos(a) * 21) * k, oy + (30 + Mathf.Sin(a) * 25) * k)); }
            var pelo = P(new float[] { 78, 26, 82, 8, 100, 2, 118, 6, 124, 22, 122, 16, 104, 12, 88, 16 });
            var cuello = P(new float[] { 92, 50, 108, 50, 108, 68, 92, 68 });
            var brazalete = P(new float[] { 152, 66, 162, 68, 162, 88, 152, 88 });
            var credencial = P(new float[] { 104, 120, 120, 120, 120, 140, 104, 140 });
            var piezas = new[] { brazoAtras, piernaI, piernaD, botaI, botaD, torso, camisa, peto, cuello, cabeza, pelo, brazoDelante, antebrazo, mano, brazalete, credencial };

            if (silueta) {
                foreach (var p in piezas) Rellenar(d, colorSilueta, p);
                return;
            }
            switch (e) {
                case Estilo.Senaletica: {
                    Color[] base_ = { Overol2, Overol, Overol, Cuero, Cuero, Overol, Camisa, Overol, Piel, Piel, Cuero, Camisa, Piel, Piel, Mostaza, Camisa };
                    for (var i = 0; i < piezas.Length; i++) Rellenar(d, base_[i], piezas[i]);
                    // Sombra dura (luz por la derecha): el tercio izquierdo de cada volumen, un tono mas oscuro.
                    Rellenar(d, H(0x2B3A48), P(new float[] { 56, 70, 82, 70, 80, 176, 68, 176 }));
                    Rellenar(d, H(0x2B3A48), P(new float[] { 70, 172, 82, 172, 80, 320, 70, 320 }));
                    Rellenar(d, H(0x2B3A48), P(new float[] { 101, 172, 112, 172, 116, 320, 108, 320 }));
                    Rellenar(d, H(0xAFBCC0), P(new float[] { 66, 68, 84, 68, 82, 112, 74, 112 }));
                    Rellenar(d, PielSombra, P(new float[] { 79, 22, 92, 12, 92, 52, 84, 48 }));
                    Rellenar(d, H(0xAFBCC0), P(new float[] { 140, 84, 168, 86, 168, 90, 140, 90 }));
                    // Credencial y cordon mostaza: el elemento mostaza del vestuario (canon §11.4.4).
                    d.Linea(ox + 92 * k, oy + 68 * k, ox + 112 * k, oy + 120 * k, Mostaza, 3);
                    d.Linea(ox + 108 * k, oy + 68 * k, ox + 112 * k, oy + 120 * k, Mostaza, 3);
                    // Contorno de tinta: grueso por fuera, fino dentro.
                    foreach (var p in new[] { brazoAtras, piernaI, piernaD, botaI, botaD, torso, cabeza, brazoDelante, mano })
                        d.Polilinea(new List<Vector2>(p) { p[0] }, Tinta, 5);
                    foreach (var p in new[] { camisa, peto, antebrazo, brazalete, credencial, pelo })
                        d.Polilinea(new List<Vector2>(p) { p[0] }, Tinta, 2);
                    // Rim cian por un solo lado (izquierda): hombro, mandibula, pierna.
                    d.Linea(ox + 58 * k, oy + 74 * k, ox + 66 * k, oy + 170 * k, Cian, 3);
                    d.Linea(ox + 80 * k, oy + 34 * k, ox + 88 * k, oy + 50 * k, Cian, 3);
                    d.Linea(ox + 72 * k, oy + 180 * k, ox + 72 * k, oy + 316 * k, Cian, 3);
                    break;
                }
                case Estilo.Plano: {
                    var linea = LineaPlano;
                    var tenue = A(LineaPlano, 0.5f);
                    // Construccion: eje, circulo de la cabeza, linea de hombros.
                    d.Linea(ox + 100 * k, oy - 6, ox + 100 * k, oy + 344 * k, tenue, 1, true);
                    d.Linea(ox + 40 * k, oy + 70 * k, ox + 160 * k, oy + 70 * k, tenue, 1, true);
                    d.Circulo(ox + 100 * k, oy + 30 * k, 30 * k, tenue, 1, null, true);
                    Rayar(d, P(new float[] { 56, 70, 82, 70, 80, 176, 68, 176 }), 8, tenue, 1.2f);
                    Rayar(d, P(new float[] { 70, 172, 82, 172, 80, 320, 70, 320 }), 8, tenue, 1.2f);
                    Rayar(d, P(new float[] { 101, 172, 112, 172, 116, 320, 108, 320 }), 8, tenue, 1.2f);
                    foreach (var p in new[] { brazoAtras, piernaI, piernaD, botaI, botaD, torso, cabeza, brazoDelante, mano })
                        d.Polilinea(new List<Vector2>(p) { p[0] }, linea, 3.5f);
                    foreach (var p in new[] { camisa, peto, antebrazo, pelo, cuello })
                        d.Polilinea(new List<Vector2>(p) { p[0] }, linea, 1.5f);
                    Rellenar(d, Mostaza, brazalete);
                    Rellenar(d, A(Cian, 0.25f), P(new float[] { 100, 116, 124, 116, 124, 144, 100, 144 }));
                    Rellenar(d, Cian, credencial);
                    break;
                }
                default: {
                    const float DX = -4, DY = -2;
                    foreach (var p in new[] { brazoAtras, piernaI, piernaD, botaI, botaD, torso, cabeza, brazoDelante, mano, pelo }) {
                        var q = new List<Vector2>();
                        foreach (var v in p) q.Add(v + V(DX, DY));
                        Rellenar(d, Fluor, q);
                    }
                    foreach (var p in new[] { brazoAtras, piernaI, piernaD, botaI, botaD, torso, peto, cabeza, pelo, brazoDelante, mano }) Rellenar(d, TintaPizarra, p);
                    Rellenar(d, Papel, camisa); Rellenar(d, Papel, antebrazo);
                    Rellenar(d, Mostaza, brazalete);
                    Rellenar(d, Mostaza, credencial);
                    // La sombra de las piernas en semitono de papel.
                    Semitono(d, ox + 70 * k, oy + 180 * k, 14 * k, 140 * k, 6, (x, y) => 0.6f, Papel);
                    break;
                }
            }
        }

        // ================================================================ el boton en tres estados

        private void Botones(RectTransform capa, Estilo e) {
            var fondo = e == Estilo.Plano ? H(0x123444) : e == Estilo.Serigrafia ? Fondo : Fondo2;
            var b = Bloque(capa, 1280, 636, 600, 310, fondo);
            var l = Lamina(b, 0, 0, 600, 310);
            var d = l.Dibujo;
            string[] textos = { "REPOSO", "ACTIVO · ENFOCADO", "PELIGRO · IRREVERSIBLE" };
            for (var i = 0; i < 3; i++) {
                float x = 40, y = 24 + i * 96, w = 520, h = 70;
                switch (e) {
                    case Estilo.Senaletica: {
                        // Placa metalica con la esquina superior derecha achaflanada (canon §11.4.8) y remaches.
                        var placa = new List<Vector2> { V(x, y), V(x + w - 18, y), V(x + w, y + 18), V(x + w, y + h), V(x, y + h) };
                        Rellenar(d, i == 1 ? H(0x1D4552) : Pared, placa);
                        if (i == 1) TuboPoli(d, new List<Vector2>(placa) { placa[0] }, Cian, 3);
                        else d.Polilinea(new List<Vector2>(placa) { placa[0] }, i == 0 ? Hormigon : Mostaza, 3);
                        if (i == 2) for (var s = 0; s < 3; s++) Rellenar(d, Mostaza, x + 16 + s * 16, y + 4, x + 24 + s * 16, y + 4, x + 12 + s * 16, y + h - 4, x + 4 + s * 16, y + h - 4);
                        Disco(d, x + 66, y + h / 2, 4, A(Camisa, 0.7f), true);
                        Disco(d, x + w - 14, y + h - 12, 4, A(Camisa, 0.7f), true);
                        l.Texto(x + 80, y, w - 100, h, textos[i], 24, i == 1 ? CianClaro : i == 2 ? MostazaClara : Texto, TextAlignmentOptions.MidlineLeft, FuentesNexus.InterfazNegrita, true);
                        break;
                    }
                    case Estilo.Plano: {
                        var c = i == 2 ? Mostaza : LineaPlano;
                        d.Rect(x, y, w, h, c, i == 1 ? 3.5f : 2, null, false, 0);
                        d.Rect(x + 6, y + 6, w - 12, h - 12, A(c, 0.4f), 1, null, false, 0);
                        foreach (var esq in new[] { V(x, y), V(x + w, y), V(x, y + h), V(x + w, y + h) }) {
                            d.Linea(esq.x - 8, esq.y, esq.x + 8, esq.y, c, 1.5f); d.Linea(esq.x, esq.y - 8, esq.x, esq.y + 8, c, 1.5f);
                        }
                        if (i == 1) TuboLinea(d, x + 20, y + h - 14, x + w - 20, y + h - 14, Cian, 3);
                        l.Texto(x + 20, y + 6, 160, 20, "ACCIÓN · 0" + (i + 1), 12, A(c, 0.7f), TextAlignmentOptions.TopLeft, FuentesNexus.Interfaz);
                        l.Texto(x + 20, y + 20, w - 40, h - 26, textos[i], 22, i == 1 ? CianClaro : c, TextAlignmentOptions.MidlineLeft, FuentesNexus.InterfazNegrita, true);
                        if (i == 2) Rayar(d, Caja(x + w - 90, y + 6, 84, h - 12), 8, A(Mostaza, 0.6f), 1.5f);
                        break;
                    }
                    default: {
                        // Etiqueta impresa: la tinta de color desregistrada hace de brillo.
                        var acento = i == 2 ? Rojo : Fluor;
                        if (i > 0) d.Poligono(Caja(x - 5, y - 4, w, h), acento);
                        d.Poligono(Caja(x, y, w, h), i == 2 ? Mostaza : i == 1 ? Papel : H(0x2A3B42));
                        if (i == 1) Semitono(d, x + w - 120, y + 4, 116, h - 8, 8, (px, py) => (px - (x + w - 120)) / 120f, TintaPizarra);
                        l.Texto(x + 24, y, w - 48, h, textos[i], 26, i == 0 ? Papel : TintaPizarra, TextAlignmentOptions.MidlineLeft, FuentesNexus.Display, true);
                        break;
                    }
                }
            }
        }

        // ================================================================ piezas de dibujo

        private static List<Vector2> Caja(float x, float y, float w, float h) {
            return new List<Vector2> { V(x, y), V(x + w, y), V(x + w, y + h), V(x, y + h) };
        }

        private static void Disco(DibujoUI d, float cx, float cy, float r, Color c, bool lleno) {
            var pts = new List<Vector2>();
            var n = Mathf.Clamp((int)(r * 0.8f), 8, 48);
            for (var i = 0; i < n; i++) { var a = i * Mathf.PI * 2 / n; pts.Add(V(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r)); }
            if (lleno) d.Poligono(pts, c); else d.Polilinea(new List<Vector2>(pts) { pts[0] }, c, 2);
        }

        private static void Rellenar(DibujoUI d, Color c, params float[] xy) {
            var p = new List<Vector2>();
            for (var i = 0; i + 1 < xy.Length; i += 2) p.Add(V(xy[i], xy[i + 1]));
            Rellenar(d, c, p);
        }

        /// <summary>Rellena un poligono cualquiera (tambien concavo) partiendolo en triangulos por recorte de orejas.</summary>
        private static void Rellenar(DibujoUI d, Color c, List<Vector2> poligono) {
            var p = new List<Vector2>(poligono);
            if (p.Count < 3) return;
            float area = 0;
            for (var i = 0; i < p.Count; i++) { var a = p[i]; var b = p[(i + 1) % p.Count]; area += a.x * b.y - b.x * a.y; }
            var signo = Mathf.Sign(area);
            var guarda = 0;
            while (p.Count > 3 && guarda++ < 400) {
                var cortado = false;
                for (var i = 0; i < p.Count; i++) {
                    Vector2 a = p[(i + p.Count - 1) % p.Count], b = p[i], cc = p[(i + 1) % p.Count];
                    if (Cruz(a, b, cc) * signo <= 0) continue;
                    var dentro = false;
                    for (var j = 0; j < p.Count && !dentro; j++) {
                        var q = p[j];
                        if (q == a || q == b || q == cc) continue;
                        dentro = Cruz(a, b, q) * signo > 0 && Cruz(b, cc, q) * signo > 0 && Cruz(cc, a, q) * signo > 0;
                    }
                    if (dentro) continue;
                    d.Poligono(new List<Vector2> { a, b, cc }, c);
                    p.RemoveAt(i);
                    cortado = true;
                    break;
                }
                if (!cortado) break;
            }
            if (p.Count >= 3) d.Poligono(p, c);
        }

        private static float Cruz(Vector2 a, Vector2 b, Vector2 c) { return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x); }

        /// <summary>Rayado a 45° dentro de un poligono convexo: la sombra del plano tecnico.</summary>
        private static void Rayar(DibujoUI d, List<Vector2> convexo, float paso, Color c, float grosor) {
            float min = float.MaxValue, max = float.MinValue;
            foreach (var v in convexo) { min = Mathf.Min(min, v.x - v.y); max = Mathf.Max(max, v.x - v.y); }
            for (var k = min + paso * 0.5f; k < max; k += paso) {
                var cortes = new List<Vector2>();
                for (var i = 0; i < convexo.Count; i++) {
                    Vector2 a = convexo[i], b = convexo[(i + 1) % convexo.Count];
                    float fa = a.x - a.y - k, fb = b.x - b.y - k;
                    if ((fa < 0) == (fb < 0) || Mathf.Approximately(fa, fb)) continue;
                    cortes.Add(Vector2.Lerp(a, b, fa / (fa - fb)));
                }
                if (cortes.Count < 2) continue;
                cortes.Sort((p, q) => p.x.CompareTo(q.x));
                d.Linea(cortes[0].x, cortes[0].y, cortes[cortes.Count - 1].x, cortes[cortes.Count - 1].y, c, grosor);
            }
        }

        /// <summary>Trama de semitono: hexagonos en reticula alternada, con un radio de 0 a 1 que dice 'cuanto'.</summary>
        private static void Semitono(DibujoUI d, float x, float y, float w, float h, float paso, Func<float, float, float> cuanto, Color c) {
            var fila = 0;
            for (var py = y + paso * 0.5f; py < y + h; py += paso * 0.866f, fila++) {
                for (var px = x + paso * 0.5f + (fila % 2) * paso * 0.5f; px < x + w; px += paso) {
                    var r = Mathf.Clamp01(cuanto(px, py)) * paso * 0.55f;
                    if (r < 0.7f) continue;
                    var hex = new List<Vector2>();
                    for (var i = 0; i < 6; i++) { var a = i * Mathf.PI / 3; hex.Add(V(px + Mathf.Cos(a) * r, py + Mathf.Sin(a) * r)); }
                    d.Poligono(hex, c);
                }
            }
        }

        /// <summary>Un tubo de neon: dos halos anchos y transparentes, el tubo y un alma casi blanca.</summary>
        private static void TuboPoli(DibujoUI d, List<Vector2> pts, Color c, float g) {
            d.Polilinea(pts, A(c, 0.08f), g * 7);
            d.Polilinea(pts, A(c, 0.20f), g * 3.4f);
            d.Polilinea(pts, c, g);
            d.Polilinea(pts, A(Color.Lerp(c, Color.white, 0.7f), 0.9f), Mathf.Max(1, g * 0.35f));
        }

        private static void TuboLinea(DibujoUI d, float x1, float y1, float x2, float y2, Color c, float g) {
            TuboPoli(d, new List<Vector2> { V(x1, y1), V(x2, y2) }, c, g);
        }

        private static void TuboCirculo(DibujoUI d, float cx, float cy, float r, Color c, float g) {
            var pts = new List<Vector2>();
            for (var i = 0; i <= 48; i++) { var a = i * Mathf.PI * 2 / 48; pts.Add(V(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r)); }
            TuboPoli(d, pts, c, g);
        }

        /// <summary>La taza esmaltada (prop del canon): cuerpo, borde y asa. Nulos = sin relleno.</summary>
        private static void Taza(DibujoUI d, float x, float y, Color? luz, Color? sombra, Color? borde, Color? contorno, float g) {
            var cuerpo = new List<Vector2> { V(x, y), V(x + 72, y), V(x + 66, y + 72), V(x + 6, y + 72) };
            if (luz.HasValue) Rellenar(d, luz.Value, cuerpo);
            if (sombra.HasValue) Rellenar(d, sombra.Value, new List<Vector2> { V(x + 44, y), V(x + 72, y), V(x + 66, y + 72), V(x + 42, y + 72) });
            if (borde.HasValue) d.Poligono(Caja(x - 2, y - 2, 76, 9), borde.Value);
            var asa = new List<Vector2> { V(x + 70, y + 16), V(x + 92, y + 18), V(x + 94, y + 44), V(x + 68, y + 50) };
            if (contorno.HasValue) {
                d.Polilinea(new List<Vector2>(cuerpo) { cuerpo[0] }, contorno.Value, g);
                d.Polilinea(asa, contorno.Value, g);
            } else if (luz.HasValue) d.Polilinea(asa, luz.Value, 10);
        }

        /// <summary>Una cota de plano: linea con marcas a 45° en los extremos y el texto encima.</summary>
        private static void Cota(Lamina l, DibujoUI d, float x1, float y1, float x2, float y2, string texto, Color c) {
            d.Linea(x1, y1, x2, y2, c, 1.5f);
            d.Linea(x1 - 6, y1 + 6, x1 + 6, y1 - 6, c, 2); d.Linea(x2 - 6, y2 + 6, x2 + 6, y2 - 6, c, 2);
            d.Linea(x1, y1 - 12, x1, y1 + 12, A(c, 0.6f), 1); d.Linea(x2, y2 - 12, x2, y2 + 12, A(c, 0.6f), 1);
            l.Texto((x1 + x2) / 2 - 200, y1 - 30, 400, 24, texto, 15, c, TextAlignmentOptions.Center, FuentesNexus.InterfazNegrita, true);
        }

        /// <summary>Linea de referencia con punto en el objeto y rotulo en el otro extremo.</summary>
        private static void Referencia(Lamina l, DibujoUI d, float x1, float y1, float x2, float y2, string texto) {
            Disco(d, x1, y1, 4, LineaPlano, true);
            d.Linea(x1, y1, x2, y2, LineaPlano, 1.2f);
            d.Linea(x2, y2, x2 + 200, y2, LineaPlano, 1.2f);
            l.Texto(x2, y2 - 24, 260, 22, texto, 14, LineaPlano, TextAlignmentOptions.BottomLeft, FuentesNexus.InterfazNegrita, true);
        }
    }
}
#endif
