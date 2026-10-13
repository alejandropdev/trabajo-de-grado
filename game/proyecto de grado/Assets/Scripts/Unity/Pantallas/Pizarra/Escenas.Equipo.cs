using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Lo que el juego antes daba por sabido (g-…): el ticket y la gente de un proyecto, el tablero y sus graficas,
    /// los diagramas y las palabras tecnicas del dia a dia.
    /// </summary>
    public static partial class DibujosDeEscena {
        private static void Equipo(Dictionary<string, Action<Tizador>> d) {
            // ------------------------------------------------------------ el ticket y la gente
            d["g-ticket"] = p => {
                p.Figura(70, 110, Tizador.Mostaza, "quien lo pide"); p.Flecha(110, 130, 190, 130, Tizador.Mostaza);
                p.Caja(210, 30, 180, 220, null, Tizador.Tiza); p.Caja(210, 30, 180, 40, "TICKET  #1042", Tizador.Mostaza, false, 17);
                for (var i = 0; i < 5; i++) p.Linea(226, 96 + i * 30, 374 - (i % 2) * 40, 96 + i * 30, Tizador.Gris, 3);
                p.Flecha(410, 130, 490, 130, Tizador.Cian); p.Figura(520, 90, Tizador.Cian); p.Figura(556, 130, Tizador.Cian); p.Texto(536, 200, "quien lo hace", 16, Tizador.Cian, true);
                p.Texto(300, 280, "un encargo por escrito, con su número", 16, Tizador.Tiza, true);
            };
            d["g-ticket-vida"] = p => {
                var estado = new[] { "abierto", "en curso", "resuelto", "cerrado" }; var c = new[] { Tizador.Mostaza, Tizador.Cian, Tizador.Cian, Tizador.Gris };
                for (var i = 0; i < 4; i++) { p.Caja(20 + i * 146, 100, 120, 60, estado[i], c[i], false, 18); if (i < 3) p.Flecha(142 + i * 146, 130, 164 + i * 146, 130, Tizador.Tiza); }
                p.Curva(372, 96, 300, 30, 226, 96, Tizador.Roja); p.Texto(300, 36, "«no era eso»: se reabre", 15, Tizador.Roja, true);
                p.Texto(80, 200, "alguien lo pide", 14, Tizador.Gris, true); p.Texto(226, 200, "alguien lo hace", 14, Tizador.Gris, true); p.Texto(372, 200, "se comprueba", 14, Tizador.Gris, true); p.Texto(518, 200, "queda el registro", 14, Tizador.Gris, true);
            };
            d["g-cliente"] = p => {
                p.Figura(300, 80, Tizador.Mostaza); p.Texto(300, 150, "el cliente", 20, Tizador.Mostaza, true);
                p.Texto(110, 70, "?", 50, Tizador.Roja, true); p.Texto(110, 120, "tiene un problema", 15, Tizador.Roja, true); p.Flecha(180, 80, 260, 80, Tizador.Roja);
                p.Moneda(490, 66, 26, Tizador.Mostaza); p.Texto(490, 120, "paga por resolverlo", 15, Tizador.Mostaza, true); p.Flecha(340, 80, 450, 70, Tizador.Mostaza);
                p.Si(300, 220, null, 18); p.Texto(300, 266, "y decide si lo entregado le sirve", 16, Tizador.Cian, true);
            };
            d["g-interesados"] = p => {
                p.Caja(230, 110, 140, 70, "el proyecto", Tizador.Cian, false, 18);
                var quien = new[] { "cliente", "usuarios", "tu jefe", "el equipo", "quien lo mantiene" };
                var x = new[] { 90f, 300, 510, 150, 450 }; var y = new[] { 60f, 30, 60, 230, 230 };
                for (var i = 0; i < 5; i++) {
                    p.Figura(x[i], y[i], Tizador.Mostaza); p.Texto(x[i], y[i] + 58, quien[i], 14, Tizador.Mostaza, true);
                    p.Linea(x[i] + (x[i] < 300 ? 30 : x[i] > 300 ? -30 : 0), y[i] + (y[i] < 100 ? 34 : -14), 300 + (x[i] - 300) * 0.3f, y[i] < 100 ? 108 : 182, Tizador.Gris, 2, true);
                }
            };
            d["g-scrum-master"] = p => {
                p.Circulo(380, 150, 110, Tizador.Gris, true); for (var i = 0; i < 3; i++) p.Figura(330 + i * 50, 130 + (i % 2) * 30, Tizador.Tiza);
                p.Texto(380, 230, "el equipo, concentrado", 14, Tizador.Tiza, true);
                p.Figura(220, 130, Tizador.Cian, "Scrum Master"); p.Escudo(220, 60, 14, Tizador.Cian);
                for (var i = 0; i < 3; i++) { p.Flecha(30, 70 + i * 70, 170, 110 + i * 26, Tizador.Roja); }
                p.Texto(70, 40, "interrupciones", 14, Tizador.Roja, true); p.No(190, 140, null, 12);
            };
            d["g-desarrollador"] = p => {
                p.Figura(110, 130, Tizador.Cian, "desarrollador"); p.Navegador(210, 40, 330, 210, Tizador.Tiza);
                var ancho = new[] { 120f, 200, 160, 220, 90, 180 };
                for (var i = 0; i < 6; i++) p.Linea(232 + (i % 3 == 0 ? 0 : 24), 90 + i * 26, 232 + (i % 3 == 0 ? 0 : 24) + ancho[i], 90 + i * 26, i == 3 ? Tizador.Mostaza : Tizador.Cian, 4);
                p.Texto(380, 280, "escribe el código que hace funcionar el sistema", 15, Tizador.Tiza, true);
            };
            d["g-tester"] = p => {
                p.Figura(90, 130, Tizador.Mostaza, "tester"); p.Navegador(190, 40, 220, 190, Tizador.Tiza); p.Caja(210, 90, 180, 34, null, Tizador.Cian); p.Caja(210, 136, 180, 34, null, Tizador.Roja);
                p.Lupa(330, 150, 24, Tizador.Mostaza); p.Bicho(330, 150, Tizador.Roja);
                var caso = new[] { "con cita", "sin cita", "nombre vacío" };
                for (var i = 0; i < 3; i++) { p.Texto(440, 90 + i * 44, caso[i], 16, Tizador.Tiza); if (i == 1) p.No(570, 84 + i * 44, null, 8); else p.Si(570, 84 + i * 44, null, 9); }
                p.Texto(300, 270, "busca los fallos antes de que los encuentre el cliente", 15, Tizador.Tiza, true);
            };
            d["g-rol-interfaz"] = p => {
                p.Navegador(150, 20, 300, 220, Tizador.Cian); p.Caja(176, 70, 248, 40, null, Tizador.Mostaza); p.Caja(176, 124, 116, 40, null, Tizador.Mostaza); p.Caja(308, 124, 116, 40, "Guardar", Tizador.Mostaza, false, 16);
                p.Mano(370, 150); p.Texto(300, 276, "lo que se ve y se toca", 18, Tizador.Mostaza, true);
            };
            d["g-rol-datos"] = p => {
                p.Cilindro(60, 60, 150, 170, Tizador.Cian); p.Caja(290, 50, 270, 190, null, Tizador.Tiza); p.Linea(290, 90, 560, 90, Tizador.Tiza, 2); p.Linea(380, 50, 380, 240, Tizador.Gris, 2); p.Linea(470, 50, 470, 240, Tizador.Gris, 2);
                p.Texto(335, 78, "visita", 14, Tizador.Mostaza, true); p.Texto(425, 78, "entra", 14, Tizador.Mostaza, true); p.Texto(515, 78, "sale", 14, Tizador.Mostaza, true);
                for (var i = 0; i < 3; i++) p.Linea(290, 140 + i * 50, 560, 140 + i * 50, Tizador.Gris, 2);
                p.Flecha(220, 145, 282, 145, Tizador.Cian); p.Texto(300, 276, "lo que se guarda, y con qué forma", 17, Tizador.Cian, true);
            };
            d["g-rol-servidor"] = p => {
                p.Servidor(60, 40, 120, 200, Tizador.Tiza); p.Engranaje(300, 110, 36, Tizador.Mostaza); p.Engranaje(366, 160, 24, Tizador.Mostaza);
                p.Caja(440, 60, 140, 44, "¿tiene cita?", Tizador.Cian, false, 15); p.Caja(440, 120, 140, 44, "avisar", Tizador.Cian, false, 15); p.Caja(440, 180, 140, 44, "guardar", Tizador.Cian, false, 15);
                p.Flecha(190, 140, 250, 130, Tizador.Tiza); p.Texto(300, 276, "las reglas: lo que el sistema decide", 17, Tizador.Mostaza, true);
            };

            // ------------------------------------------------------------ el tablero y sus graficas
            d["g-historia"] = p => {
                p.Caja(90, 20, 420, 190, null, Tizador.Mostaza); p.Bloque(90, 20, 420, 12, Tizador.Mostaza);
                p.Texto(110, 74, "Como recepcionista,", 20, Tizador.Tiza); p.Texto(110, 114, "quiero registrar una visita", 20, Tizador.Cian); p.Texto(110, 154, "para saber quién está dentro.", 20, Tizador.Mostaza);
                p.Circulo(466, 176, 22, Tizador.Tiza); p.Texto(466, 186, "3", 26, Tizador.Tiza, true);
                p.Texto(150, 250, "quién", 16, Tizador.Tiza, true); p.Texto(300, 250, "qué", 16, Tizador.Cian, true); p.Texto(430, 250, "para qué", 16, Tizador.Mostaza, true); p.Texto(540, 182, "puntos", 14, Tizador.Gris, true);
            };
            d["g-columnas"] = p => {
                p.Tablero(20, 30, 560, 220, Tizador.Tiza, "Por hacer", "En curso", "Revisión", "Hecho");
                p.Tarjeta(32, 70, 116, 40, Tizador.Gris); p.Tarjeta(32, 120, 116, 40, Tizador.Gris); p.Tarjeta(172, 70, 116, 40, Tizador.Mostaza); p.Tarjeta(312, 70, 116, 40, Tizador.Cian); p.Tarjeta(452, 70, 116, 40, Tizador.Tiza);
                p.Curva(90, 190, 300, 250, 510, 190, Tizador.Mostaza); p.Texto(300, 280, "cada tarjeta viaja de izquierda a derecha", 16, Tizador.Mostaza, true);
            };
            d["g-pr"] = p => {
                p.Caja(30, 30, 230, 190, null, Tizador.Tiza); p.Texto(145, 56, "el cambio", 16, Tizador.Tiza, true);
                var signo = new[] { "+", "+", "-", "+" }; for (var i = 0; i < 4; i++) { p.Texto(50, 96 + i * 32, signo[i], 22, signo[i] == "+" ? Tizador.Cian : Tizador.Roja); p.Linea(76, 90 + i * 32, 230 - i * 20, 90 + i * 32, signo[i] == "+" ? Tizador.Cian : Tizador.Roja, 4); }
                p.Flecha(270, 125, 320, 125, Tizador.Tiza); p.Figura(370, 90, Tizador.Mostaza, "otra persona"); p.Lupa(400, 110, 14, Tizador.Mostaza);
                p.Flecha(430, 125, 480, 125, Tizador.Cian); p.Si(530, 120, null, 22); p.Texto(530, 180, "aprobado", 16, Tizador.Cian, true);
                p.Texto(300, 268, "nadie mezcla su código sin que otro lo mire", 16, Tizador.Tiza, true);
            };
            d["g-burndown"] = p => {
                p.Ejes(80, 20, 440, 210, Tizador.Gris); p.Linea(80, 30, 520, 230, Tizador.Gris, 2, true); p.Trazo(Tizador.Cian, 80, 30, 170, 50, 260, 110, 350, 130, 440, 200, 520, 230);
                p.Texto(430, 110, "lo ideal", 15, Tizador.Gris, true); p.Texto(250, 70, "lo real", 15, Tizador.Cian, true);
                p.Texto(30, 30, "todo", 13, Tizador.Gris, true); p.Texto(30, 232, "nada", 13, Tizador.Gris, true); p.Texto(300, 264, "días del sprint", 14, Tizador.Gris, true); p.Texto(300, 290, "lo que queda por hacer, bajando", 16, Tizador.Tiza, true);
            };
            d["g-cfd"] = p => {
                p.Ejes(60, 20, 480, 220, Tizador.Gris);
                var hecho = new[] { 0.05f, 0.15f, 0.25f, 0.3f, 0.4f, 0.5f }; var curso = new[] { 0.15f, 0.15f, 0.2f, 0.35f, 0.35f, 0.3f }; var porHacer = new[] { 0.6f, 0.55f, 0.45f, 0.3f, 0.2f, 0.15f };
                for (var i = 0; i < 6; i++) {
                    var x = 80 + i * 76; var b = 240f;
                    p.Bloque(x, b - 220 * hecho[i], 56, 220 * hecho[i], Tizador.Tiza); b -= 220 * hecho[i];
                    p.Bloque(x, b - 220 * curso[i], 56, 220 * curso[i], Tizador.Mostaza); b -= 220 * curso[i];
                    p.Bloque(x, b - 220 * porHacer[i], 56, 220 * porHacer[i], Tizador.Cian);
                }
                p.Texto(300, 268, "cada día, cuánto hay en cada columna del tablero", 15, Tizador.Tiza, true);
            };
            d["g-curva-s"] = p => {
                p.Ejes(80, 20, 440, 210, Tizador.Gris);
                p.Curva(80, 226, 260, 226, 300, 130, Tizador.Cian, false); p.Curva(300, 130, 340, 34, 520, 30, Tizador.Cian, false);
                p.Texto(170, 200, "arranca lento", 15, Tizador.Tiza, true); p.Texto(380, 130, "acelera", 15, Tizador.Mostaza, true); p.Texto(450, 92, "se frena al cerrar", 15, Tizador.Tiza, true);
                p.Texto(300, 270, "el avance acumulado dibuja una S", 16, Tizador.Cian, true);
            };
            d["g-velocidad"] = p => {
                p.Barras(80, 30, 330, 190, Tizador.Cian, 0.6f, 0.8f, 0.7f, 0.75f); p.Linea(80, 84, 410, 84, Tizador.Mostaza, 3, true);
                var pts = new[] { "8", "11", "9", "10" }; for (var i = 0; i < 4; i++) p.Texto(132 + i * 76, 244, "S" + (i + 1) + ": " + pts[i], 15, Tizador.Tiza, true);
                p.Texto(500, 80, "promedio:", 16, Tizador.Mostaza, true); p.Texto(500, 110, "9,5 puntos", 20, Tizador.Mostaza, true); p.Texto(500, 150, "por sprint", 16, Tizador.Mostaza, true);
                p.Texto(300, 284, "sirve para saber cuánto cabe en el siguiente", 15, Tizador.Tiza, true);
            };
            d["g-daily"] = p => {
                var dice = new[] { "«ayer hice…»", "«hoy haré…»", "«me bloquea…»" }; var c = new[] { Tizador.Tiza, Tizador.Cian, Tizador.Roja };
                for (var i = 0; i < 3; i++) { p.Figura(100 + i * 200, 150, c[i]); p.Bocadillo(30 + i * 200, 50, 150, 50, dice[i], c[i], 16); }
                p.Esfera(300, 250, 24, Tizador.Mostaza); p.Texto(430, 258, "15 minutos, de pie", 16, Tizador.Mostaza, true);
            };
            d["g-planning"] = p => {
                for (var i = 0; i < 5; i++) p.Tarjeta(30, 30 + i * 50, 150, 40, i < 3 ? Tizador.Cian : Tizador.Gris);
                p.Texto(105, 290, "lo más importante, arriba", 14, Tizador.Tiza, true); p.Flecha(190, 90, 280, 120, Tizador.Cian);
                p.Trazo(Tizador.Tiza, 300, 40, 320, 250, 500, 250, 520, 40); p.Linea(296, 90, 524, 90, Tizador.Roja, 3, true); p.Texto(410, 76, "hasta aquí cabe", 15, Tizador.Roja, true);
                for (var i = 0; i < 3; i++) p.Tarjeta(336, 104 + i * 46, 148, 38, Tizador.Cian);
                p.Texto(410, 284, "lo que el equipo puede hacer", 14, Tizador.Mostaza, true);
            };

            // ------------------------------------------------------------ diagramas y tecnica
            d["g-componentes"] = p => {
                p.Caja(40, 40, 150, 70, "Recepción", Tizador.Cian, false, 16); p.Caja(240, 40, 150, 70, "Registro", Tizador.Cian, false, 16); p.Caja(240, 180, 150, 70, "Aviso", Tizador.Cian, false, 16); p.Cilindro(450, 40, 110, 80, Tizador.Tiza);
                p.Flecha(194, 75, 236, 75, Tizador.Mostaza); p.Flecha(394, 75, 446, 75, Tizador.Mostaza); p.Flecha(315, 114, 315, 176, Tizador.Mostaza);
                p.Texto(215, 60, "usa", 13, Tizador.Mostaza, true); p.Texto(110, 200, "cajas: las piezas", 16, Tizador.Cian, true); p.Texto(110, 226, "flechas: quién usa a quién", 14, Tizador.Mostaza, true);
            };
            d["g-secuencia"] = p => {
                var quien = new[] { "Recepción", "Registro", "Aviso" };
                for (var i = 0; i < 3; i++) { p.Caja(50 + i * 200, 10, 110, 40, quien[i], Tizador.Cian, false, 15); p.Linea(105 + i * 200, 50, 105 + i * 200, 250, Tizador.Gris, 2, true); }
                p.Flecha(105, 90, 300, 90, Tizador.Tiza); p.Texto(203, 80, "1. guardar visita", 14, Tizador.Tiza, true);
                p.Flecha(305, 140, 500, 140, Tizador.Tiza); p.Texto(403, 130, "2. avisar", 14, Tizador.Tiza, true);
                p.Flecha(305, 200, 110, 200, Tizador.Mostaza); p.Texto(207, 190, "3. listo", 14, Tizador.Mostaza, true);
                p.Flecha(560, 60, 560, 240, Tizador.Gris); p.Texto(560, 266, "el tiempo", 13, Tizador.Gris, true); p.Texto(280, 284, "quién le dice qué a quién, y en qué orden", 15, Tizador.Tiza, true);
            };
            d["g-traza"] = p => {
                var paso = new[] { "garita", "lectura", "permiso", "barrera" };
                for (var i = 0; i < 4; i++) {
                    p.Caja(20 + i * 146, 40, 120, 54, paso[i], Tizador.Tiza, false, 16); p.Texto(80 + i * 146, 126, "08:41:0" + (2 + i), 15, Tizador.Mostaza, true);
                    if (i < 3) { p.Punto(148 + i * 146, 67, 4, Tizador.Cian); p.Punto(158 + i * 146, 67, 4, Tizador.Cian); }
                }
                p.Curva(80, 150, 300, 230, 520, 150, Tizador.Cian); p.Texto(300, 250, "el rastro de UN pedido, paso a paso, con su hora", 16, Tizador.Cian, true);
            };
            d["g-log"] = p => {
                p.Caja(60, 20, 480, 220, null, Tizador.Tiza);
                var linea = new[] { "08:41:02  INFO   carro detectado", "08:41:03  INFO   lectura ABC123", "08:41:04  ERROR  permiso sin respuesta", "08:41:09  INFO   reintento", "08:41:10  INFO   barrera abierta" };
                for (var i = 0; i < 5; i++) p.Texto(80, 60 + i * 40, linea[i], 17, i == 2 ? Tizador.Roja : Tizador.Tiza);
                p.Texto(300, 272, "el diario del sistema: apunta todo lo que hace", 16, Tizador.Mostaza, true);
            };
            d["g-defecto"] = p => {
                p.Caja(40, 60, 220, 110, null, Tizador.Cian); p.Texto(150, 96, "lo esperado", 16, Tizador.Cian, true); p.Texto(150, 144, "aforo: 40", 26, Tizador.Cian, true);
                p.Caja(340, 60, 220, 110, null, Tizador.Roja); p.Texto(450, 96, "lo que pasó", 16, Tizador.Roja, true); p.Texto(450, 144, "aforo: 4", 26, Tizador.Roja, true);
                p.Texto(300, 124, "≠", 44, Tizador.Mostaza, true); p.Bicho(300, 210, Tizador.Roja); p.Texto(300, 264, "esa diferencia es el defecto", 17, Tizador.Tiza, true);
            };
            d["g-dependencia"] = p => {
                p.Caja(40, 100, 140, 70, "guardar", Tizador.Cian, false, 18); p.Caja(230, 100, 140, 70, "avisar", Tizador.Tiza, false, 18); p.Caja(420, 100, 140, 70, "informe", Tizador.Tiza, false, 18);
                p.Flecha(226, 135, 186, 135, Tizador.Mostaza); p.Flecha(416, 135, 376, 135, Tizador.Mostaza); p.Texto(206, 86, "necesita", 13, Tizador.Mostaza, true); p.Texto(396, 86, "necesita", 13, Tizador.Mostaza, true);
                p.Texto(110, 210, "1.º", 22, Tizador.Cian, true); p.Texto(300, 210, "2.º", 22, Tizador.Tiza, true); p.Texto(490, 210, "3.º", 22, Tizador.Tiza, true);
                p.Texto(300, 268, "lo que otros necesitan se hace antes", 16, Tizador.Tiza, true);
            };
            d["g-presupuesto"] = p => {
                p.Circulo(110, 130, 70, Tizador.Mostaza); p.Texto(110, 146, "$", 70, Tizador.Mostaza, true);
                var parte = new[] { "gente", "tiempo", "herramientas" }; var w = new[] { 220f, 140, 70 };
                for (var i = 0; i < 3; i++) { p.Flecha(190, 130, 250, 76 + i * 60, Tizador.Gris); p.Bloque(260, 62 + i * 60, w[i], 28, i == 0 ? Tizador.Cian : Tizador.Mostaza); p.Texto(270 + w[i], 84 + i * 60, parte[i], 15, Tizador.Tiza); }
                p.Texto(300, 268, "lo que se puede gastar, y en qué", 17, Tizador.Tiza, true);
            };
            d["g-incidencia"] = p => {
                p.Servidor(80, 70, 120, 150, Tizador.Tiza); p.Trazo(Tizador.Roja, 120, 70, 150, 120, 124, 140, 160, 220); p.Sirena(140, 52, Tizador.Roja);
                p.Si(280, 90, Tizador.Gris, 16); p.Texto(380, 96, "ayer funcionaba", 17, Tizador.Gris, true); p.No(280, 160, null, 14); p.Texto(380, 166, "hoy está caído", 17, Tizador.Roja, true);
                p.Texto(350, 250, "algo que ya funcionaba deja de hacerlo: hay que atenderlo ya", 14, Tizador.Tiza, true);
            };
        }
    }
}
