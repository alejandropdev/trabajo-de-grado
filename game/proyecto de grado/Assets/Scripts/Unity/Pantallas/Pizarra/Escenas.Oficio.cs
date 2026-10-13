using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>Las palabras del oficio de cada nivel: que son (of-…) y donde se ven en el juego (of-…-donde).</summary>
    public static partial class DibujosDeEscena {
        private static void Oficio(Dictionary<string, Action<Tizador>> d) {
            // Quien usa la palabra: la gente del sector y tu cliente, hablando entre ellos.
            d["of-quien-lo-usa"] = p => {
                p.Figura(170, 120, Tizador.Cian, "quien trabaja ahí"); p.Figura(430, 120, Tizador.Mostaza, "tu cliente");
                p.Bocadillo(200, 30, 90, 44, "···", Tizador.Cian, 24); p.Bocadillo(330, 30, 90, 44, "···", Tizador.Mostaza, 24);
                p.Curva(245, 120, 300, 150, 355, 120, Tizador.Gris); p.Curva(355, 140, 300, 172, 245, 140, Tizador.Gris);
                p.Texto(300, 260, "la misma palabra, todos los días", 18, Tizador.Tiza, true);
            };

            // ============================================================ nivel 0 · recepcion y control de visitas

            d["of-visitante"] = p => {
                p.Hexagono(300, 146, 104, Tizador.Gris);
                p.Edificio(266, 102, 68, 88, Tizador.Cian);
                var nombres = new[] { "proveedor", "candidata", "mensajero", "cliente", "técnico", "familiar" };
                for (var i = 0; i < 6; i++) {
                    var a = Mathf.PI / 3 * i + Mathf.PI / 6;
                    var x = 300 + Mathf.Cos(a) * 104; var y = 146 + Mathf.Sin(a) * 104;
                    p.Linea(300 + Mathf.Cos(a) * 54, 146 + Mathf.Sin(a) * 54, 300 + Mathf.Cos(a) * 84, 146 + Mathf.Sin(a) * 84, Tizador.Gris, 2, true);
                    p.Figura(x, y - 16);
                    var alLado = Mathf.Abs(Mathf.Cos(a)) > 0.1f;
                    p.Texto(x + (!alLado ? 70 : Mathf.Cos(a) > 0 ? 66 : -66), y + (alLado ? 0 : -10), nombres[i], 15, Tizador.Tiza, true);
                }
            };
            d["of-visitante-donde"] = p => {
                p.Formulario(60, 40, 190, 200, Tizador.Cian); p.Texto(155, 268, "formulario de visita", 17, Tizador.Cian, true);
                p.Flecha(265, 140, 335, 140);
                p.Caja(350, 40, 200, 200, null, Tizador.Mostaza); p.Texto(450, 268, "registro", 17, Tizador.Mostaza, true);
                var filas = new[] { "09:02  Ana R.", "09:15  Luis M.", "09:40  Eva T.", "10:05  Raúl P." };
                for (var i = 0; i < 4; i++) Renglon(p, 362, 78 + i * 42, 176, filas[i]);
            };

            d["of-anfitrion"] = p => {
                p.Figura(110, 120, Tizador.Tiza, "visitante");
                p.Flecha(160, 140, 420, 140, Tizador.Mostaza); p.Texto(290, 126, "viene a ver a", 18, Tizador.Mostaza, true);
                p.Figura(480, 120, Tizador.Cian, "anfitrión");
                p.Bocadillo(410, 20, 170, 46, "«tu visita ha llegado»", Tizador.Cian, 15);
                p.Texto(300, 250, "el anfitrión trabaja dentro: es quien recibe el aviso", 17, Tizador.Tiza, true);
            };
            d["of-anfitrion-donde"] = p => {
                p.Caja(40, 100, 180, 70, "Aviso al anfitrión", Tizador.Cian, false, 16);
                p.Flecha(225, 135, 290, 135); Sobre(p, 300, 108, 80, 54, Tizador.Mostaza);
                p.Flecha(390, 135, 460, 135); p.Figura(510, 115, Tizador.Cian, "anfitrión");
                p.Texto(130, 200, "un módulo", 16, Tizador.Gris, true); p.Texto(340, 200, "un correo", 16, Tizador.Gris, true);
                p.Texto(300, 60, "en el diagrama de tu propuesta", 17, Tizador.Tiza, true);
            };

            d["of-cita"] = p => {
                p.Calendario(40, 95, 110, 110, Tizador.Mostaza); p.Circulo(95, 163, 14, Tizador.Cian);
                p.Texto(95, 232, "día y hora acordados", 15, Tizador.Mostaza, true);
                p.Curva(160, 130, 230, 60, 300, 70, Tizador.Cian); p.Curva(160, 170, 230, 240, 300, 230, Tizador.Gris);
                p.Figura(335, 50, Tizador.Cian); p.Texto(335, 108, "con cita", 16, Tizador.Cian, true);
                p.Flecha(370, 70, 450, 70, Tizador.Cian); p.Si(480, 70); p.Texto(545, 76, "lo esperan", 16, Tizador.Cian, true);
                p.Figura(335, 210, Tizador.Tiza); p.Texto(335, 268, "sin cita", 16, Tizador.Tiza, true);
                p.Flecha(370, 230, 450, 230); p.Texto(480, 242, "?", 34, Tizador.Mostaza, true); p.Texto(545, 236, "¿qué se hace?", 16, Tizador.Mostaza, true);
            };
            d["of-cita-donde"] = p => {
                p.Caja(40, 40, 250, 200, null, Tizador.Mostaza); p.Texto(165, 268, "registro de visitas", 17, Tizador.Mostaza, true);
                p.Texto(60, 66, "visita", 15, Tizador.Gris); p.Texto(210, 66, "cita", 15, Tizador.Gris);
                var filas = new[] { "Ana R.", "Luis M.", "Eva T." };
                for (var i = 0; i < 3; i++) { Renglon(p, 52, 110 + i * 44, 226, filas[i]); if (i == 1) p.No(228, 104 + i * 44, null, 9); else p.Si(228, 104 + i * 44, null, 9); }
                p.Hoja(350, 30, 110, 110, Tizador.Cian, "ticket"); p.Bocadillo(390, 166, 180, 46, "¿y si viene sin cita?", Tizador.Tiza, 14);
                p.Texto(455, 268, "comentarios del ticket", 17, Tizador.Cian, true);
            };

            d["of-registro"] = p => {
                p.Caja(240, 50, 120, 190, null, Tizador.Tiza); p.Punto(340, 150, 5); p.Texto(300, 268, "la puerta", 16, Tizador.Gris, true);
                p.Figura(90, 90, Tizador.Cian); p.Flecha(130, 110, 230, 110, Tizador.Cian); p.Texto(180, 96, "entra 09:02", 17, Tizador.Cian, true);
                p.Figura(510, 170, Tizador.Mostaza); p.Flecha(370, 190, 470, 190, Tizador.Mostaza); p.Texto(420, 176, "sale 09:47", 17, Tizador.Mostaza, true);
                p.Esfera(90, 220, 24, Tizador.Gris); p.Esfera(510, 70, 24, Tizador.Gris);
            };
            d["of-registro-donde"] = p => {
                p.Tablero(30, 50, 230, 170, Tizador.Cian, "backlog");
                p.Tarjeta(46, 90, 198, 34, Tizador.Cian, "registrar la entrada"); p.Tarjeta(46, 134, 198, 34, Tizador.Cian, "registrar la salida");
                p.Caja(330, 50, 240, 170, null, Tizador.Mostaza); p.Texto(450, 76, "Quién sigue dentro", 16, Tizador.Mostaza, true);
                Renglon(p, 342, 116, 216, "Ana R.   entró 09:02"); Renglon(p, 342, 156, 216, "Eva T.   entró 09:40"); Renglon(p, 342, 196, 216, "Raúl P.  entró 10:05");
            };

            d["of-credencial"] = p => {
                p.Curva(250, 20, 300, 80, 350, 20, Tizador.Gris, false); p.Linea(300, 50, 300, 78, Tizador.Gris);
                p.Caja(215, 78, 170, 190, null, Tizador.Cian); p.Caja(240, 100, 120, 34, "VISITA", Tizador.Mostaza, false, 20);
                p.Figura(300, 165, Tizador.Tiza); p.Linea(240, 228, 360, 228, Tizador.Tiza, 2); p.Linea(260, 246, 340, 246, Tizador.Tiza, 2);
                p.Si(470, 150, null, 22); p.Texto(470, 200, "autorizado", 17, Tizador.Cian, true);
                p.Texto(110, 150, "se lleva a la vista", 17, Tizador.Tiza, true);
            };
            d["of-credencial-donde"] = p => {
                p.Linea(60, 200, 540, 200, Tizador.Tiza, 4); p.Texto(300, 232, "el mostrador de la recepción", 17, Tizador.Gris, true);
                p.Figura(180, 140, Tizador.Cian, null); p.Figura(420, 140, Tizador.Tiza, null);
                p.Bocadillo(200, 40, 200, 50, "«póngase la credencial»", Tizador.Cian, 15);
                p.Caja(290, 150, 34, 44, null, Tizador.Mostaza); p.Texto(300, 268, "en las charlas con la gente de recepción", 16, Tizador.Tiza, true);
            };

            d["of-aforo"] = p => {
                p.Caja(60, 60, 330, 190, null, Tizador.Tiza); p.Trazo(null, 60, 60, 225, 14, 390, 60);
                for (var f = 0; f < 2; f++) for (var k = 0; k < 5; k++) p.Figura(100 + k * 62, 110 + f * 76, f == 1 && k > 2 ? Tizador.Gris : Tizador.Cian);
                p.Texto(490, 130, "38 / 40", 40, Tizador.Mostaza, true); p.Texto(490, 170, "personas dentro", 17, Tizador.Mostaza, true);
                p.Texto(490, 220, "el máximo es 40", 16, Tizador.Roja, true);
            };
            d["of-aforo-donde"] = p => {
                p.Caja(150, 30, 300, 240, null, Tizador.Mostaza); p.Texto(300, 58, "Quién sigue dentro", 17, Tizador.Mostaza, true);
                for (var i = 0; i < 4; i++) { p.Circulo(184, 92 + i * 40, 9, Tizador.Cian); p.Linea(210, 96 + i * 40, 420, 96 + i * 40, Tizador.Gris, 2); }
                p.Bloque(150, 238, 300, 3, Tizador.Mostaza); p.Texto(300, 262, "total: 4 dentro", 18, Tizador.Tiza, true);
            };

            d["of-mvp"] = p => {
                p.Texto(170, 40, "lo mínimo que ya sirve", 18, Tizador.Cian, true);
                var si = new[] { "registrar", "avisar", "quién sigue dentro" };
                for (var i = 0; i < 3; i++) { p.Caja(50, 60 + i * 66, 240, 50, si[i], Tizador.Cian, false, 17); p.Si(318, 85 + i * 66, null, 12); }
                p.Texto(470, 40, "después, si hace falta", 18, Tizador.Gris, true);
                var no = new[] { "informes", "colores", "app móvil" };
                for (var i = 0; i < 3; i++) p.Caja(370, 60 + i * 66, 200, 50, no[i], Tizador.Gris, true, 17);
            };
            d["of-mvp-donde"] = p => {
                p.Figura(110, 130, Tizador.Mostaza, "Marisol"); p.Bocadillo(150, 50, 240, 56, "«que funcione el viernes»", Tizador.Mostaza, 17);
                p.Calendario(440, 80, 120, 120, Tizador.Cian); p.Circulo(530, 161, 15, Tizador.Mostaza); p.Texto(500, 230, "viernes", 17, Tizador.Mostaza, true);
            };

            // ============================================================ nivel 1 · gestion de instalaciones

            d["of-garita"] = p => {
                p.Caja(70, 90, 130, 150, null, Tizador.Tiza); p.Trazo(null, 55, 90, 135, 44, 215, 90); p.Caja(95, 120, 80, 56, null, Tizador.Gris);
                p.Figura(135, 136, Tizador.Cian); p.Texto(135, 268, "el guardia", 16, Tizador.Cian, true);
                p.Camara(215, 96, Tizador.Mostaza); p.Texto(262, 84, "la cámara", 16, Tizador.Mostaza, true);
                p.Linea(300, 114, 400, 190, Tizador.Mostaza, 2, true);
                p.Coche(400, 180, Tizador.Tiza); p.Linea(40, 240, 560, 240, Tizador.Gris, 3); p.Texto(450, 268, "entrada al patio", 16, Tizador.Gris, true);
            };
            d["of-garita-donde"] = p => {
                p.Caja(40, 50, 120, 54, "Garita", Tizador.Cian, false, 16); p.Flecha(165, 77, 215, 77); p.Caja(220, 50, 120, 54, "Acceso", Tizador.Tiza, false, 16);
                p.Texto(190, 132, "el diagrama del sistema", 16, Tizador.Gris, true);
                p.Caja(40, 160, 520, 110, null, Tizador.Mostaza); p.Texto(60, 186, "08:41:02  garita: carro detectado", 16, Tizador.Tiza);
                p.Texto(60, 214, "08:41:03  garita: lectura ABC123", 16, Tizador.Tiza); p.Texto(60, 242, "08:41:04  garita: permiso OK", 16, Tizador.Cian);
                p.Texto(470, 148, "la traza de la garita", 16, Tizador.Mostaza, true);
            };

            d["of-barrera"] = p => {
                p.Caja(90, 150, 30, 90, null, Tizador.Tiza); p.Linea(110, 160, 290, 160, Tizador.Roja, 6); p.Texto(200, 144, "cerrada", 16, Tizador.Roja, true);
                p.Coche(160, 190, Tizador.Gris);
                p.Caja(360, 150, 30, 90, null, Tizador.Tiza); p.Linea(380, 160, 480, 40, Tizador.Cian, 6); p.Texto(500, 70, "abierta", 16, Tizador.Cian, true);
                p.Coche(440, 190, Tizador.Cian); p.Flecha(550, 214, 590, 214, Tizador.Cian);
                p.Linea(40, 242, 580, 242, Tizador.Gris, 3); p.Texto(300, 274, "obedece cualquier orden que le llegue", 16, Tizador.Mostaza, true);
            };
            d["of-barrera-donde"] = p => {
                p.Caja(30, 110, 140, 60, "Acceso", Tizador.Cian, false, 17); p.Flecha(175, 140, 235, 140); p.Texto(205, 126, "abrir", 14, Tizador.Tiza, true);
                p.Caja(240, 100, 160, 80, null, Tizador.Mostaza, true); p.Texto(320, 136, "controlador", 16, Tizador.Mostaza, true); p.Texto(320, 160, "de la barrera", 16, Tizador.Mostaza, true);
                p.Flecha(405, 140, 455, 140); p.Caja(470, 130, 20, 70, null, Tizador.Tiza); p.Linea(482, 138, 570, 80, Tizador.Tiza, 5);
                p.Texto(320, 220, "del fabricante", 16, Tizador.Gris, true);
            };

            d["of-lpr"] = p => {
                p.Camara(40, 110, Tizador.Mostaza); p.Linea(125, 128, 210, 128, Tizador.Mostaza, 2, true);
                p.Caja(215, 100, 150, 60, "ABC 123", Tizador.Tiza, false, 28); p.Texto(290, 186, "la placa del carro", 16, Tizador.Gris, true);
                p.Flecha(375, 130, 440, 130, Tizador.Cian);
                p.Texto(510, 140, "\"ABC123\"", 26, Tizador.Cian, true); p.Texto(510, 186, "texto que el sistema lee", 16, Tizador.Cian, true);
                p.Texto(300, 250, "LPR: lectura de matrículas, por sus siglas en inglés", 16, Tizador.Tiza, true);
            };
            d["of-lpr-donde"] = p => {
                p.Tablero(30, 50, 240, 150, Tizador.Cian, "backlog"); p.Tarjeta(46, 96, 208, 44, Tizador.Cian, "Leer matrículas en la garita");
                p.Caja(320, 50, 250, 150, null, Tizador.Mostaza); p.Texto(445, 76, "la traza del log", 16, Tizador.Mostaza, true);
                p.Texto(334, 112, "lectura: ABC123", 16); p.Texto(334, 142, "lectura: ??C12?", 16, Tizador.Roja); p.Texto(334, 172, "lectura: XYZ789", 16);
            };

            d["of-permiso"] = p => {
                p.Caja(120, 30, 360, 240, null, Tizador.Tiza); p.Texto(300, 58, "pueden entrar al patio", 17, Tizador.Mostaza, true);
                p.Texto(140, 92, "matrícula", 15, Tizador.Gris); p.Texto(290, 92, "horario", 15, Tizador.Gris);
                var filas = new[] { "ABC123     06:00 a 18:00", "XYZ789     todo el día", "JKL456     solo sábados" };
                for (var i = 0; i < 3; i++) { Renglon(p, 132, 132 + i * 46, 300, filas[i]); p.Si(452, 126 + i * 46, null, 9); }
                p.Texto(300, 258, "quien no está en la lista, no pasa", 16, Tizador.Roja, true);
            };
            d["of-permiso-donde"] = p => {
                p.Caja(60, 70, 480, 150, null, Tizador.Mostaza); p.Texto(300, 56, "la traza de la garita", 17, Tizador.Mostaza, true);
                p.Texto(80, 108, "08:41:03  lectura MNO321", 18); p.Texto(80, 148, "08:41:04  permiso: DENEGADO", 18, Tizador.Roja);
                p.Texto(80, 188, "08:41:04  barrera: no abrir", 18); p.No(500, 142, null, 12);
            };

            d["of-ascensor"] = p => {
                p.Caja(210, 30, 180, 240, null, Tizador.Tiza); p.Linea(300, 30, 300, 4, Tizador.Gris); p.Linea(300, 270, 300, 296, Tizador.Gris);
                p.Caja(250, 150, 100, 90, null, Tizador.Mostaza); p.Linea(250, 195, 350, 195, Tizador.Mostaza, 2); p.Linea(300, 150, 300, 240, Tizador.Mostaza, 2);
                p.Flecha(430, 220, 430, 80, Tizador.Cian); p.Texto(500, 150, "sube mercancía", 16, Tizador.Cian, true);
                p.Texto(100, 140, "solo cabe", 17, Tizador.Tiza, true); p.Texto(100, 164, "un envío a la vez", 17, Tizador.Tiza, true);
            };
            d["of-ascensor-donde"] = p => {
                p.Caja(40, 100, 180, 70, "Reservas", Tizador.Cian, false, 20); p.Texto(130, 200, "el módulo", 16, Tizador.Gris, true);
                p.Tablero(310, 50, 250, 190, Tizador.Mostaza, "backlog");
                p.Tarjeta(326, 92, 218, 36, Tizador.Mostaza, "reservar el ascensor"); p.Tarjeta(326, 138, 218, 36, Tizador.Mostaza, "ver las reservas del día");
                p.Tarjeta(326, 184, 218, 36, Tizador.Mostaza, "cancelar una reserva");
            };

            d["of-franja"] = p => {
                p.Linea(40, 170, 560, 170, Tizador.Tiza, 3);
                var horas = new[] { "09:30", "10:00", "10:30", "11:00", "11:30" };
                for (var i = 0; i < 5; i++) { p.Linea(60 + i * 120, 160, 60 + i * 120, 180, Tizador.Tiza); p.Texto(60 + i * 120, 206, horas[i], 16, Tizador.Gris, true); }
                p.Bloque(180, 110, 120, 50, Tizador.Cian); p.Texto(240, 96, "tu franja", 18, Tizador.Cian, true);
                p.Figura(240, 40, Tizador.Cian); p.Texto(430, 130, "media hora con el", 16, Tizador.Tiza, true); p.Texto(430, 152, "ascensor para ti", 16, Tizador.Tiza, true);
            };
            d["of-franja-donde"] = p => {
                p.Caja(40, 50, 240, 180, null, Tizador.Cian); p.Texto(160, 76, "reservas", 17, Tizador.Cian, true);
                Renglon(p, 52, 116, 216, "10:00 a 10:30  Bodega"); Renglon(p, 52, 156, 216, "10:30 a 11:00  Obras"); Renglon(p, 52, 196, 216, "11:00 a 11:30  libre", Tizador.Gris);
                p.Caja(340, 50, 220, 180, null, Tizador.Mostaza); p.Texto(450, 76, "sus pruebas", 17, Tizador.Mostaza, true);
                var pruebas = new[] { "franja libre", "franja ocupada", "franja pasada" };
                for (var i = 0; i < 3; i++) { p.Texto(360, 122 + i * 40, pruebas[i], 16); p.Si(530, 116 + i * 40, null, 9); }
            };

            d["of-choque"] = p => {
                p.Linea(40, 200, 560, 200, Tizador.Tiza, 3);
                var horas = new[] { "10:00", "10:15", "10:30", "10:45" };
                for (var i = 0; i < 4; i++) { p.Linea(90 + i * 140, 190, 90 + i * 140, 210, Tizador.Tiza); p.Texto(90 + i * 140, 236, horas[i], 16, Tizador.Gris, true); }
                p.Caja(90, 70, 280, 46, "Bodega  10:00 a 10:30", Tizador.Cian, false, 16);
                p.Caja(230, 130, 280, 46, "Obras  10:15 a 10:45", Tizador.Mostaza, false, 16);
                p.Caja(230, 60, 140, 126, null, Tizador.Roja, true); p.Texto(300, 44, "se pisan", 18, Tizador.Roja, true);
                p.Texto(300, 276, "una regla escrita decide cuál gana", 16, Tizador.Tiza, true);
            };
            d["of-choque-donde"] = p => {
                p.Caja(120, 40, 360, 210, null, Tizador.Tiza); p.Texto(300, 30, "el calendario de turnos", 17, Tizador.Mostaza, true);
                for (var i = 1; i < 4; i++) p.Linea(120 + i * 90, 40, 120 + i * 90, 250, Tizador.Gris, 2);
                for (var i = 1; i < 4; i++) p.Linea(120, 40 + i * 52, 480, 40 + i * 52, Tizador.Gris, 2);
                p.Bloque(216, 98, 78, 40, Tizador.Cian); p.Bloque(306, 150, 78, 40, Tizador.Cian); p.Bloque(396, 46, 78, 40, Tizador.Cian);
                p.Bloque(126, 150, 78, 40, Tizador.Roja); p.Bloque(140, 162, 78, 40, Tizador.Mostaza); p.No(170, 176, Tizador.Pizarra, 10);
            };

            d["of-hora-punta"] = p => {
                p.Barras(40, 30, 280, 180, Tizador.Cian, 0.15f, 0.3f, 0.95f, 0.5f, 0.25f, 0.35f, 0.2f);
                p.Texto(151, 22, "el pico", 16, Tizador.Mostaza, true); p.Texto(180, 236, "carros por hora, a lo largo del día", 15, Tizador.Gris, true);
                for (var i = 0; i < 4; i++) p.En(336 + i * 64, 130, 0.58f).Coche(0, 0, i == 0 ? Tizador.Mostaza : Tizador.Tiza);
                p.Texto(460, 100, "hasta 40 esperando", 17, Tizador.Mostaza, true); p.Texto(460, 196, "en la garita", 16, Tizador.Gris, true);
            };
            d["of-hora-punta-donde"] = p => {
                p.Hoja(60, 40, 130, 180, Tizador.Cian, "el encargo"); p.Texto(125, 250, "lo pide el cliente", 16, Tizador.Cian, true);
                p.Flecha(210, 130, 290, 130);
                p.Caja(310, 60, 110, 56, "A", Tizador.Gris, true, 20); p.Caja(450, 60, 110, 56, "B", Tizador.Mostaza, false, 20); p.Caja(380, 150, 110, 56, "C", Tizador.Gris, true, 20);
                p.Texto(435, 250, "pesa al elegir la arquitectura", 16, Tizador.Mostaza, true);
            };

            d["of-controlador"] = p => {
                p.Caja(200, 60, 200, 170, null, Tizador.Tiza); p.Candado(300, 120, 22, Tizador.Roja); p.Texto(300, 206, "su código no es tuyo", 16, Tizador.Roja, true);
                p.Caja(388, 126, 24, 40, null, Tizador.Cian); p.Flecha(540, 146, 420, 146, Tizador.Cian);
                p.Texto(500, 120, "solo por la puerta", 16, Tizador.Cian, true); p.Texto(500, 180, "que él ofrece", 16, Tizador.Cian, true);
                p.Texto(100, 150, "viene de fábrica", 17, Tizador.Mostaza, true); p.Engranaje(100, 90, 18, Tizador.Mostaza);
            };
            d["of-controlador-donde"] = p => {
                p.Caja(40, 115, 140, 70, "tu sistema", Tizador.Cian, false, 18);
                p.Flecha(185, 135, 370, 80, Tizador.Mostaza); p.Flecha(185, 165, 370, 220, Tizador.Mostaza);
                p.Caja(380, 50, 180, 60, "barrera", Tizador.Tiza, true, 18); p.Caja(380, 190, 180, 60, "ascensor", Tizador.Tiza, true, 18);
                p.Texto(260, 150, "estas flechas", 16, Tizador.Mostaza, true);
            };
        }
    }
}
