using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>El curso de Kanban (kb-…), en la misma cocina: los pedidos son tarjetas que cruzan el tablero hasta servirse.</summary>
    public static partial class DibujosDeEscena {
        private static void Ojo(Tizador p, float x, float y, float s, Color? c = null) {
            p.Curva(x - s, y, x, y - s * 0.9f, x + s, y, c, false); p.Curva(x - s, y, x, y + s * 0.9f, x + s, y, c, false); p.Circulo(x, y, s * 0.3f, c);
        }

        /// <summary>Un vaso con su tope marcado: hasta ahi, y no mas.</summary>
        private static void Vaso(Tizador p, float x, float y, float s, Color? c = null) {
            p.Trazo(c, x - s * 0.6f, y - s, x - s * 0.45f, y + s, x + s * 0.45f, y + s, x + s * 0.6f, y - s);
            p.Linea(x - s * 0.8f, y - s * 0.3f, x + s * 0.8f, y - s * 0.3f, Tizador.Roja, 3, true);
        }

        private static void Escalera(Tizador p, float x, float y, float s, Color? c = null) {
            p.Trazo(c, x, y, x + s, y, x + s, y - s, x + s * 2, y - s, x + s * 2, y - s * 2, x + s * 3, y - s * 2);
        }

        private static void Cinta(Tizador p, float x, float y, float w, Color? c = null) {
            p.Linea(x, y, x + w, y, c, 4); p.Linea(x, y + 22, x + w, y + 22, c, 4);
            p.Curva(x, y, x - 16, y + 11, x, y + 22, c, false); p.Curva(x + w, y, x + w + 16, y + 11, x + w, y + 22, c, false);
            for (var cx = x + 14; cx < x + w; cx += 34) p.Circulo(cx, y + 11, 7, c ?? Tizador.Gris);
        }

        private static void Bombilla(Tizador p, float x, float y, Color? c = null) {
            p.Circulo(x, y, 16, c ?? Tizador.Mostaza); p.Caja(x - 7, y + 15, 14, 12, null, c ?? Tizador.Mostaza);
            for (var i = 0; i < 5; i++) { var a = Mathf.PI + Mathf.PI / 4 * i; p.Linea(x + Mathf.Cos(a) * 24, y + Mathf.Sin(a) * 24, x + Mathf.Cos(a) * 34, y + Mathf.Sin(a) * 34, c ?? Tizador.Mostaza, 2); }
        }

        private static void Kanban(Dictionary<string, Action<Tizador>> d) {
            d["kb-que"] = p => {
                var xy = new[] { 40f, 60, 90, 90, 50, 130, 110, 150, 70, 190 };
                for (var i = 0; i < 5; i++) p.Tarjeta(xy[i * 2], xy[i * 2 + 1], 90, 40, Tizador.Roja);
                p.Texto(110, 264, "pedidos amontonados", 15, Tizador.Roja, true); p.Flecha(230, 150, 290, 150, Tizador.Mostaza);
                p.Tablero(310, 50, 260, 180, Tizador.Tiza, "", "", ""); p.Tarjeta(320, 90, 66, 30, Tizador.Cian); p.Tarjeta(320, 130, 66, 30, Tizador.Cian); p.Tarjeta(407, 90, 66, 30, Tizador.Mostaza); p.Tarjeta(494, 90, 66, 30, Tizador.Tiza);
                p.Flecha(330, 200, 550, 200, Tizador.Cian); p.Texto(440, 264, "el trabajo, a la vista y fluyendo", 15, Tizador.Cian, true);
            };
            d["kb-origen"] = p => {
                p.Caja(30, 60, 150, 150, null, Tizador.Tiza); p.Linea(30, 110, 180, 110, Tizador.Tiza); p.Linea(30, 160, 180, 160, Tizador.Tiza);
                for (var f = 0; f < 3; f++) for (var k = 0; k < 4; k++) if (!(f == 1 && k == 2)) p.Caja(40 + k * 34, 72 + f * 50, 24, 34, null, Tizador.Cian);
                p.Caja(108, 122, 24, 34, null, Tizador.Mostaza, true); p.Texto(105, 236, "el hueco avisa:", 14, Tizador.Mostaza, true); p.Texto(105, 256, "hay que reponer", 14, Tizador.Mostaza, true);
                p.Flecha(195, 135, 235, 135); p.Coche(250, 110, Tizador.Tiza); p.Tarjeta(270, 70, 60, 30, Tizador.Mostaza, "kanban"); p.Texto(300, 190, "Toyota", 15, Tizador.Tiza, true);
                p.Flecha(365, 135, 405, 135); p.Tablero(420, 60, 160, 150, Tizador.Cian, "", "", ""); p.Tarjeta(428, 96, 40, 26, Tizador.Cian); p.Tarjeta(481, 96, 40, 26, Tizador.Cian); p.Texto(500, 236, "software", 15, Tizador.Cian, true);
            };
            d["kb-diferencia"] = p => {
                for (var i = 0; i < 3; i++) { p.Caja(40 + i * 80, 70, 60, 60, null, Tizador.Tiza); p.Plato(70 + i * 80, 90, 16, Tizador.Tiza); p.Plato(70 + i * 80, 112, 16, Tizador.Tiza); }
                p.Texto(150, 170, "por lotes,", 16, Tizador.Tiza, true); p.Texto(150, 192, "cada dos semanas", 16, Tizador.Tiza, true); p.Texto(150, 40, "Scrum", 20, Tizador.Mostaza, true);
                p.Linea(300, 20, 300, 270, Tizador.Gris, 2, true);
                Cinta(p, 340, 110, 220, Tizador.Tiza); for (var i = 0; i < 4; i++) p.PlatoServido(360 + i * 60, 100, 20, Tizador.Cian);
                p.Flecha(350, 160, 550, 160, Tizador.Cian); p.Texto(450, 200, "uno a uno, sin parar", 16, Tizador.Cian, true); p.Texto(450, 40, "Kanban", 20, Tizador.Mostaza, true);
            };
            d["kb-cambio"] = p => {
                p.Caja(30, 90, 200, 150, null, Tizador.Tiza); p.Olla(50, 180, 50, 40, Tizador.Tiza); p.Cocinero(140, 150, Tizador.Tiza); p.Tablero(160, 104, 60, 44, Tizador.Mostaza, "", "");
                p.Texto(130, 266, "tu cocina de hoy + un tablero", 15, Tizador.Mostaza, true);
                p.Trazo(Tizador.Cian, 270, 240, 320, 240, 320, 200, 370, 200, 370, 160, 420, 160, 420, 120, 470, 120);
                p.Caja(480, 40, 100, 80, null, Tizador.Cian); p.Estrella(530, 80, 20, Tizador.Mostaza); p.Texto(400, 270, "se mejora de a poco", 15, Tizador.Cian, true);
            };
            d["kb-servicio"] = p => {
                p.Carita(300, 160, 32, 1, Tizador.Mostaza); p.Texto(300, 220, "el comensal", 15, Tizador.Mostaza, true);
                for (var i = 0; i < 5; i++) {
                    var a = Mathf.PI * 2 / 5 * i - Mathf.PI / 2; var x = 300 + Mathf.Cos(a) * 200; var y = 150 + Mathf.Sin(a) * 96;
                    p.Cocinero(x, y, Tizador.Tiza); p.Flecha(300 + Mathf.Cos(a) * 160, 160 + Mathf.Sin(a) * 76, 300 + Mathf.Cos(a) * 60, 160 + Mathf.Sin(a) * 44, Tizador.Cian);
                }
            };
            d["kb-practicas"] = p => {
                var nombres = new[] { "visualizar", "limitar", "fluir", "reglas claras", "feedback", "mejorar" };
                for (var i = 0; i < 6; i++) { var x = 60 + (i % 3) * 240; var y = 60 + (i / 3) * 140; p.Texto(x, y + 70, (i + 1) + ". " + nombres[i], 17, Tizador.Mostaza, true); }
                Ojo(p, 60, 60, 34, Tizador.Cian); Vaso(p, 300, 60, 30, Tizador.Cian); p.Curva(500, 40, 540, 20, 580, 50, Tizador.Cian, false); p.Curva(500, 64, 540, 44, 580, 74, Tizador.Cian, false);
                p.Hoja(34, 170, 52, 64, Tizador.Cian); p.Bocadillo(264, 176, 72, 40, "···", Tizador.Cian, 22); Escalera(p, 506, 240, 24, Tizador.Cian);
            };
            d["kb-tablero"] = p => {
                p.Tablero(20, 30, 560, 240, Tizador.Tiza, "Pedido", "Preparar", "Cocinar", "Emplatar", "Servido");
                var n = new[] { 3, 2, 2, 1, 3 };
                for (var k = 0; k < 5; k++) for (var i = 0; i < n[k]; i++) p.Tarjeta(30 + k * 112, 70 + i * 46, 92, 36, k == 4 ? Tizador.Cian : k == 0 ? Tizador.Gris : Tizador.Mostaza);
            };
            d["kb-tarjeta"] = p => {
                p.Caja(190, 50, 220, 200, null, Tizador.Tiza); p.Texto(300, 96, "Sopa", 30, Tizador.Tiza, true); p.Bloque(190, 50, 220, 12, Tizador.Mostaza);
                p.Carita(230, 200, 20, 1, Tizador.Cian); p.Esfera(300, 200, 20, Tizador.Tiza); p.Candado(370, 190, 12, Tizador.Roja);
                p.Curva(110, 90, 160, 80, 250, 90, Tizador.Gris); p.Texto(70, 96, "título", 16, Tizador.Tiza, true);
                p.Curva(110, 40, 160, 30, 200, 52, Tizador.Gris); p.Texto(70, 46, "tipo", 16, Tizador.Mostaza, true);
                p.Curva(110, 220, 160, 230, 206, 206, Tizador.Gris); p.Texto(60, 226, "responsable", 16, Tizador.Cian, true);
                p.Curva(490, 250, 400, 270, 304, 226, Tizador.Gris); p.Texto(530, 256, "fecha", 16, Tizador.Tiza, true);
                p.Curva(490, 170, 440, 160, 392, 190, Tizador.Gris); p.Texto(540, 176, "bloqueo", 16, Tizador.Roja, true);
            };
            d["kb-visualizar"] = p => {
                p.Bloque(30, 50, 220, 190, new Color(0, 0, 0, 0.5f)); p.Caja(30, 50, 220, 190, null, Tizador.Gris);
                p.Caja(60, 90, 60, 36, null, Tizador.Gris, true); p.Caja(150, 130, 60, 36, null, Tizador.Gris, true); p.Caja(80, 180, 60, 36, null, Tizador.Gris, true); p.Texto(140, 266, "a oscuras: ¿qué hay pendiente?", 14, Tizador.Gris, true);
                p.Flecha(265, 145, 325, 145, Tizador.Mostaza); Bombilla(p, 460, 40, Tizador.Mostaza);
                p.Tablero(350, 90, 220, 150, Tizador.Tiza, "", "", ""); p.Tarjeta(358, 126, 58, 30, Tizador.Cian); p.Tarjeta(432, 126, 58, 30, Tizador.Mostaza); p.Tarjeta(505, 126, 58, 30, Tizador.Tiza);
                p.Texto(460, 266, "con el tablero, se ve todo", 14, Tizador.Cian, true);
            };
            d["kb-wip"] = p => {
                p.Caja(160, 20, 280, 260, null, Tizador.Tiza); p.Texto(260, 56, "Cocinando", 22, Tizador.Tiza, true); p.Circulo(400, 48, 24, Tizador.Mostaza); p.Texto(400, 60, "3", 30, Tizador.Mostaza, true);
                p.Linea(160, 80, 440, 80, Tizador.Gris, 2); p.Caja(180, 200, 240, 60, null, Tizador.Gris);
                for (var i = 0; i < 3; i++) { p.Circulo(220 + i * 80, 230, 18, Tizador.Roja); p.Olla(196 + i * 80, 140, 48, 44, Tizador.Cian); p.Vapor(220 + i * 80, 132, Tizador.Gris); }
                p.Olla(40, 150, 48, 44, Tizador.Gris); p.Texto(64, 230, "espera", 16, Tizador.Gris, true); p.Flecha(100, 172, 150, 172, Tizador.Roja); p.No(128, 150, null, 10);
                p.Texto(520, 150, "tres fogones,", 15, Tizador.Mostaza, true); p.Texto(520, 172, "tres ollas", 15, Tizador.Mostaza, true);
            };
            d["kb-porque-wip"] = p => {
                p.Cocinero(140, 50, Tizador.Roja); p.Punto(118, 36, 3, Tizador.Cian); p.Punto(164, 44, 3, Tizador.Cian);
                for (var i = 0; i < 8; i++) { p.Olla(24 + (i % 4) * 62, 120 + (i / 4) * 66, 40, 34, i % 2 == 0 ? Tizador.Roja : Tizador.Tiza); if (i % 2 == 0) p.No(44 + (i % 4) * 62, 138 + (i / 4) * 66, null, 8); }
                p.Texto(140, 276, "ocho a la vez: la mitad se quema", 14, Tizador.Roja, true); p.Linea(300, 20, 300, 270, Tizador.Gris, 2, true);
                p.Cocinero(450, 50, Tizador.Cian); p.Olla(380, 130, 56, 46, Tizador.Cian); p.Olla(470, 130, 56, 46, Tizador.Cian); p.Si(408, 210, null, 12); p.Si(498, 210, null, 12);
                p.Texto(450, 276, "dos a la vez: perfectas y antes", 14, Tizador.Cian, true);
            };
            d["kb-pull"] = p => {
                p.Figura(50, 60, Tizador.Roja, "jefe"); for (var i = 0; i < 3; i++) p.Caja(100 + i * 36, 60 + i * 30, 50, 40, null, Tizador.Roja);
                p.Cocinero(230, 180, Tizador.Tiza); p.Flecha(90, 80, 200, 150, Tizador.Roja); p.Texto(140, 266, "empujar (push)", 16, Tizador.Roja, true); p.No(150, 130, null, 60);
                p.Linea(300, 20, 300, 270, Tizador.Gris, 2, true);
                p.Tablero(330, 50, 150, 150, Tizador.Tiza, "", ""); p.Tarjeta(338, 90, 58, 34, Tizador.Cian); p.Tarjeta(338, 134, 58, 34, Tizador.Cian);
                p.Cocinero(540, 110, Tizador.Cian); p.Curva(520, 140, 460, 170, 402, 110, Tizador.Cian); p.Texto(450, 240, "jalar (pull):", 16, Tizador.Cian, true); p.Texto(450, 262, "tomo otra cuando termino", 14, Tizador.Cian, true);
            };
            d["kb-flujo"] = p => {
                p.Curva(30, 60, 150, 10, 250, 70, Tizador.Roja, false); p.Curva(250, 70, 360, 130, 420, 50, Tizador.Roja, false); p.Curva(420, 50, 500, 0, 570, 80, Tizador.Roja, false);
                p.Circulo(250, 96, 14, Tizador.Roja, true); p.Circulo(430, 90, 12, Tizador.Roja, true);
                p.Tarjeta(60, 30, 40, 24, Tizador.Roja); p.Tarjeta(110, 44, 40, 24, Tizador.Roja); p.Tarjeta(340, 76, 40, 24, Tizador.Roja); p.Texto(300, 140, "a saltos y con remolinos", 15, Tizador.Roja, true);
                p.Linea(30, 200, 570, 200, Tizador.Cian); p.Linea(30, 250, 570, 250, Tizador.Cian);
                for (var i = 0; i < 5; i++) { p.Tarjeta(60 + i * 104, 212, 50, 26, Tizador.Cian); }
                p.Flecha(520, 225, 566, 225, Tizador.Cian); p.Texto(300, 284, "suave y parejo: se puede predecir", 15, Tizador.Cian, true);
            };
            d["kb-cuello"] = p => {
                p.Tablero(20, 60, 560, 200, Tizador.Gris, "Preparar", "Cocinar", "Emplatar", "Servido");
                for (var i = 0; i < 2; i++) p.Tarjeta(34, 100 + i * 44, 110, 34, Tizador.Tiza);
                for (var i = 0; i < 4; i++) p.Tarjeta(174 + (i % 2) * 20, 96 + i * 36, 100, 30, Tizador.Roja);
                p.Tarjeta(314, 100, 110, 34, Tizador.Mostaza);
                p.Trazo(Tizador.Mostaza, 150, 36, 300, 36, 330, 20, 420, 20); p.Trazo(Tizador.Mostaza, 150, 280, 300, 280, 330, 292, 420, 292);
                p.Texto(230, 28, "se amontona antes", 14, Tizador.Roja, true); p.Texto(500, 180, "y después, vacío", 14, Tizador.Gris, true);
            };
            d["kb-bloqueo"] = p => {
                p.Tarjeta(220, 90, 160, 100, Tizador.Tiza, "Pizza"); p.Bloque(300, 70, 110, 44, Tizador.Roja); p.Texto(355, 98, "falta queso", 15, Tizador.Pizarra, true);
                p.Cocinero(90, 110, Tizador.Tiza); p.Cocinero(140, 190, Tizador.Tiza); p.Cocinero(490, 110, Tizador.Tiza); p.Cocinero(450, 200, Tizador.Cian);
                p.Flecha(120, 130, 210, 130, Tizador.Cian); p.Flecha(470, 130, 390, 130, Tizador.Cian); p.Texto(300, 264, "se marca a la vista y todos ayudan a destrabarlo", 15, Tizador.Tiza, true);
            };
            d["kb-politicas"] = p => {
                p.Tablero(40, 30, 520, 150, Tizador.Tiza, "Preparar", "Cocinar"); p.Tarjeta(140, 80, 110, 44, Tizador.Mostaza, "Sopa"); p.Flecha(256, 102, 286, 102, Tizador.Mostaza);
                p.Linea(300, 60, 300, 170, Tizador.Roja, 5); p.Caja(292, 50, 16, 16, null, Tizador.Roja);
                p.Caja(170, 200, 260, 70, null, Tizador.Mostaza); p.Texto(300, 226, "Pasa a Cocinar si:", 16, Tizador.Mostaza, true); p.Texto(280, 254, "ingredientes listos", 16, Tizador.Tiza, true); p.Si(400, 248, null, 10);
            };
            d["kb-clases"] = p => {
                var nombre = new[] { "Urgente", "Fecha fija", "Estándar", "Intangible" }; var ej = new[] { "«¡alergia!»", "«boda sábado»", "un pedido normal", "«limpiar campana»" };
                var c = new[] { Tizador.Roja, Tizador.Mostaza, Tizador.Gris, Tizador.Cian };
                for (var i = 0; i < 4; i++) { p.Caja(20 + i * 144, 40, 128, 170, null, c[i]); p.Texto(84 + i * 144, 240, nombre[i], 17, c[i], true); p.Texto(84 + i * 144, 264, ej[i], 13, Tizador.Tiza, true); }
                p.Sirena(84, 140, Tizador.Roja); p.Calendario(194, 90, 68, 70, Tizador.Mostaza); p.Tarjeta(336, 100, 72, 50, Tizador.Gris); p.Escoba(450, 80, Tizador.Cian);
            };
            d["kb-carriles"] = p => {
                p.Caja(120, 30, 460, 240, null, Tizador.Tiza); p.Linea(120, 110, 580, 110, Tizador.Gris, 2); p.Linea(120, 190, 580, 190, Tizador.Gris, 2);
                p.Linea(273, 30, 273, 270, Tizador.Gris, 2, true); p.Linea(426, 30, 426, 270, Tizador.Gris, 2, true);
                var carril = new[] { "Desayunos", "Almuerzos", "Urgente" };
                for (var i = 0; i < 3; i++) p.Texto(60, 76 + i * 80, carril[i], 16, i == 2 ? Tizador.Roja : Tizador.Tiza, true);
                p.Tarjeta(140, 50, 100, 40, Tizador.Cian); p.Tarjeta(300, 50, 100, 40, Tizador.Cian); p.Tarjeta(140, 130, 100, 40, Tizador.Mostaza); p.Tarjeta(450, 130, 100, 40, Tizador.Mostaza);
                p.Tarjeta(300, 210, 100, 40, Tizador.Roja); p.Flecha(140, 230, 280, 230, Tizador.Roja); p.Flecha(410, 230, 560, 230, Tizador.Roja);
            };
            d["kb-metricas"] = p => {
                p.Linea(40, 110, 440, 110, Tizador.Tiza, 3); p.Carita(60, 60, 20, 0, Tizador.Mostaza); p.Texto(60, 30, "pide", 14, Tizador.Mostaza, true);
                p.Cocinero(220, 64, Tizador.Tiza); p.Texto(220, 20, "se empieza", 14, Tizador.Tiza, true); p.PlatoServido(420, 76, 22, Tizador.Cian); p.Texto(420, 30, "se sirve", 14, Tizador.Cian, true);
                foreach (var x in new[] { 60f, 220, 420 }) p.Linea(x, 100, x, 120, Tizador.Tiza, 3);
                p.Trazo(Tizador.Mostaza, 60, 150, 60, 166, 420, 166, 420, 150); p.Texto(240, 192, "lead time: desde que se pide", 15, Tizador.Mostaza, true);
                p.Trazo(Tizador.Cian, 220, 210, 220, 226, 420, 226, 420, 210); p.Texto(320, 252, "cycle time: desde que se empieza", 15, Tizador.Cian, true);
                p.Caja(470, 90, 110, 110, null, Tizador.Tiza); p.Texto(525, 150, "12", 40, Tizador.Tiza, true); p.Texto(525, 180, "por semana", 14, Tizador.Tiza, true); p.Texto(525, 228, "throughput", 15, Tizador.Tiza, true);
            };
            d["kb-cfd"] = p => {
                p.Ejes(60, 20, 480, 220, Tizador.Gris);
                p.Trazo(Tizador.Cian, 60, 220, 180, 180, 300, 130, 420, 80, 540, 30);
                p.Trazo(Tizador.Mostaza, 60, 236, 180, 216, 300, 196, 420, 176, 540, 150);
                p.Trazo(Tizador.Tiza, 60, 240, 180, 232, 300, 222, 420, 210, 540, 196);
                p.Texto(130, 150, "por hacer", 15, Tizador.Cian, true); p.Texto(470, 110, "en curso", 15, Tizador.Mostaza, true); p.Texto(490, 226, "hecho", 15, Tizador.Tiza, true);
                p.Lupa(400, 130, 28, Tizador.Roja); p.Texto(300, 60, "la banda que se ensancha:", 14, Tizador.Roja, true); p.Texto(300, 80, "ahí está el cuello de botella", 14, Tizador.Roja, true);
                p.Texto(300, 270, "el tiempo", 14, Tizador.Gris, true);
            };
            d["kb-little"] = p => {
                p.Embudo(60, 40, 160, 180, Tizador.Cian); p.Punto(120, 70, 9, Tizador.Cian); p.Punto(156, 80, 9, Tizador.Cian); p.Punto(140, 250, 9, Tizador.Cian); p.Punto(140, 280, 9, Tizador.Cian);
                p.Texto(140, 20, "poco a la vez", 16, Tizador.Cian, true); p.Texto(250, 240, "sale rápido", 15, Tizador.Cian, true);
                p.Embudo(380, 40, 160, 180, Tizador.Roja);
                for (var i = 0; i < 9; i++) p.Punto(410 + (i % 4) * 32 + (i / 4) * 12, 60 + (i / 4) * 26, 9, Tizador.Roja);
                p.Texto(460, 20, "mucho a la vez", 16, Tizador.Roja, true); p.Texto(460, 250, "se atasca", 15, Tizador.Roja, true);
                p.Texto(300, 130, "tiempo =", 15, Tizador.Tiza, true); p.Texto(300, 152, "WIP ÷ ritmo", 15, Tizador.Tiza, true);
            };
            d["kb-cadencias"] = p => {
                p.Circulo(300, 150, 60, Tizador.Tiza); p.Linea(300, 150, 300, 110, Tizador.Tiza); p.Linea(300, 150, 330, 160, Tizador.Tiza);
                var nombre = new[] { "reabastecer", "diaria", "entrega", "servicio", "operaciones", "riesgos", "estrategia" };
                for (var i = 0; i < 7; i++) {
                    var a = Mathf.PI * 2 / 7 * i - Mathf.PI / 2; var x = 300 + Mathf.Cos(a) * 210; var y = 140 + Mathf.Sin(a) * 100;
                    p.Texto(x, y + 44, nombre[i], 13, Tizador.Mostaza, true);
                    var q = p.En(x - 36, y - 30, 0.9f);
                    switch (i) {
                        case 0: q.Carrito(4, 6, Tizador.Cian); break;
                        case 1: q.Taza(16, 10, Tizador.Cian); break;
                        case 2: q.En(0, 6, 0.6f).Camion(0, 0, Tizador.Cian); break;
                        case 3: q.Lupa(30, 24, 14, Tizador.Cian); break;
                        case 4: q.Engranaje(40, 30, 14, Tizador.Cian); break;
                        case 5: q.Escudo(40, 22, 16, Tizador.Cian); break;
                        default: q.Brujula(40, 30, 20, Tizador.Cian); break;
                    }
                }
            };
            d["kb-roles"] = p => {
                p.Figura(70, 90, Tizador.Mostaza); p.Caja(40, 150, 60, 26, "pedidos", Tizador.Mostaza, false, 12); p.Figura(190, 90, Tizador.Cian); p.Caja(160, 150, 60, 26, "flujo", Tizador.Cian, false, 12);
                p.Texto(130, 220, "dos roles habituales,", 15, Tizador.Tiza, true); p.Texto(130, 242, "ninguno obligatorio", 15, Tizador.Tiza, true); p.Linea(290, 20, 290, 270, Tizador.Gris, 2, true);
                p.RelojDeArena(350, 70, 60, Tizador.Mostaza); p.Texto(350, 126, "Sprints", 14, Tizador.Mostaza, true); Vaso(p, 540, 70, 26, Tizador.Cian); p.Texto(540, 126, "límite WIP", 14, Tizador.Cian, true);
                p.Flecha(380, 90, 416, 130, Tizador.Mostaza); p.Flecha(514, 90, 476, 130, Tizador.Cian); p.Licuadora(416, 140, Tizador.Tiza); p.Texto(446, 276, "Scrumban", 18, Tizador.Tiza, true);
            };
            d["kb-resumen"] = p => {
                p.Tablero(20, 20, 380, 130, Tizador.Tiza, "", "", "", ""); for (var k = 0; k < 4; k++) p.Tarjeta(30 + k * 95, 56 + (k % 2) * 36, 74, 28, k == 3 ? Tizador.Cian : Tizador.Mostaza);
                Cinta(p, 40, 220, 360, Tizador.Tiza); for (var i = 0; i < 5; i++) p.PlatoServido(70 + i * 72, 210, 20, Tizador.Cian);
                p.Flecha(420, 230, 470, 230, Tizador.Cian); p.Carita(530, 220, 36, 1, Tizador.Mostaza);
                var pasos = new[] { "visualiza", "limita", "fluye", "mide", "mejora" };
                for (var i = 0; i < 5; i++) p.Texto(500, 36 + i * 26, pasos[i], 17, Tizador.Mostaza, true);
            };
        }
    }
}
