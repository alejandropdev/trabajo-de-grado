using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El curso de Scrum (sc-…), contado en una cocina: el Dueño del menú es el Product Owner, el Coach es el Scrum
    /// Master, los cocineros son los Developers y cada plato servido es un incremento.
    /// </summary>
    public static partial class DibujosDeEscena {
        private static void Corazon(Tizador p, float x, float y, float s, Color? c = null) {
            p.Curva(x, y + s * 0.3f, x - s * 1.2f, y - s * 0.9f, x - s, y + s * 0.2f, c ?? Tizador.Roja, false);
            p.Curva(x, y + s * 0.3f, x + s * 1.2f, y - s * 0.9f, x + s, y + s * 0.2f, c ?? Tizador.Roja, false);
            p.Trazo(c ?? Tizador.Roja, x - s, y + s * 0.2f, x, y + s * 1.2f, x + s, y + s * 0.2f);
        }

        private static void Basura(Tizador p, float x, float y, Color? c = null) {
            p.Trazo(c, x, y, x + 8, y + 60, x + 52, y + 60, x + 60, y); p.Linea(x - 6, y, x + 66, y, c, 4); p.Linea(x + 20, y - 8, x + 40, y - 8, c, 4);
        }

        private static void Cuchillo(Tizador p, float x, float y, Color? c = null) {
            p.Trazo(c, x, y, x + 46, y, x + 60, y + 14, x, y + 14, x, y); p.Linea(x - 26, y + 7, x, y + 7, c, 6);
        }

        /// <summary>El rollo de pedidos: una tira larga de tarjetas que cuelga y se enrolla al final.</summary>
        private static void Rollo(Tizador p, float x, float y, float alto, Color? c = null) {
            p.Linea(x, y, x, y + alto, c); p.Linea(x + 90, y, x + 90, y + alto, c); p.Linea(x - 6, y, x + 96, y, c, 5);
            p.Curva(x, y + alto, x + 45, y + alto + 26, x + 90, y + alto, c, false);
            for (var fy = y + 16; fy < y + alto - 6; fy += 22) p.Linea(x + 10, fy, x + 80, fy, Tizador.Gris, 2);
        }

        private static void Scrum(Dictionary<string, Action<Tizador>> d) {
            d["sc-que"] = p => {
                p.Hoja(50, 20, 130, 150, Tizador.Roja, "Banquete"); p.Cocinero(115, 210, Tizador.Tiza); p.Texto(115, 284, "un pedido enorme", 15, Tizador.Roja, true);
                p.Flecha(210, 150, 270, 150, Tizador.Mostaza);
                for (var i = 0; i < 4; i++) p.Bandeja(290 + i * 56, 110 + i * 22, 46, Tizador.Cian);
                p.Flecha(500, 190, 530, 190, Tizador.Cian); p.Carita(560, 150, 26, 1, Tizador.Mostaza); p.Texto(400, 70, "cuatro bandejas pequeñas,", 15, Tizador.Cian, true); p.Texto(400, 90, "una por una", 15, Tizador.Cian, true);
                p.Texto(450, 284, "algo útil en cada ciclo", 15, Tizador.Cian, true);
            };
            d["sc-origen"] = p => {
                for (var i = 0; i < 3; i++) { p.Figura(60 + i * 36, 110 + i * 14, Tizador.Tiza); p.Linea(78 + i * 36, 124 + i * 14, 110 + i * 36, 140 + i * 14, Tizador.Tiza); }
                p.Elipse(200, 180, 26, 16, Tizador.Mostaza); p.Texto(130, 250, "la melé del rugby", 16, Tizador.Tiza, true); p.Texto(130, 272, "(scrum, en inglés)", 14, Tizador.Gris, true);
                p.Flecha(250, 150, 320, 150, Tizador.Mostaza);
                for (var i = 0; i < 3; i++) { p.Cocinero(350 + i * 36, 110 + i * 14, Tizador.Cian); p.Linea(368 + i * 36, 124 + i * 14, 400 + i * 36, 140 + i * 14, Tizador.Cian); }
                p.Olla(480, 130, 80, 70, Tizador.Mostaza); p.Texto(440, 250, "todos empujan juntos", 16, Tizador.Cian, true);
            };
            d["sc-manifiesto"] = p => {
                var mas = new[] { "personas", "plato servido", "colaborar", "responder al cambio" }; var menos = new[] { "procesos", "la receta en papel", "el contrato", "seguir el plan" };
                for (var i = 0; i < 4; i++) {
                    var x = 150 + (i % 2) * 300; var y = 30 + (i / 2) * 150;
                    p.Balanza(x, y + 10, 150, -1, Tizador.Tiza);
                    p.Punto(x - 75, y + 8, 10, Tizador.Cian); p.Punto(x + 75, y - 14, 5, Tizador.Gris);
                    p.Texto(x - 75, y + 60, mas[i], 15, Tizador.Cian, true); p.Texto(x + 80, y + 30, menos[i], 14, Tizador.Gris, true);
                }
            };
            d["sc-pilares"] = p => {
                p.Trazo(Tizador.Tiza, 40, 80, 300, 10, 560, 80, 40, 80); p.Linea(40, 250, 560, 250, Tizador.Tiza, 5);
                var nombres = new[] { "transparencia", "inspección", "adaptación" };
                for (var i = 0; i < 3; i++) { p.Caja(80 + i * 170, 80, 100, 170, null, Tizador.Tiza); p.Texto(130 + i * 170, 276, nombres[i], 16, Tizador.Mostaza, true); }
                p.Caja(96, 110, 68, 68, null, Tizador.Cian); p.Linea(130, 110, 130, 178, Tizador.Cian, 2); p.Linea(96, 144, 164, 144, Tizador.Cian, 2);
                p.Olla(274, 170, 52, 40, Tizador.Tiza); p.Lupa(292, 130, 16, Tizador.Mostaza);
                p.Cocinero(470, 130, Tizador.Cian); p.Olla(444, 190, 52, 40, Tizador.Tiza); p.Punto(456, 180, 2); p.Punto(466, 174, 2); p.Punto(478, 182, 2);
            };
            d["sc-lean"] = p => {
                for (var i = 0; i < 5; i++) p.PlatoServido(50 + (i % 3) * 60, 60 + (i / 3) * 50, 24, Tizador.Gris);
                p.Flecha(110, 140, 110, 180, Tizador.Roja); Basura(p, 80, 200, Tizador.Roja); p.Plato(110, 196, 18, Tizador.Roja); p.Texto(110, 290, "platos que nadie pidió", 15, Tizador.Roja, true);
                p.Flecha(230, 150, 300, 150, Tizador.Mostaza);
                p.PlatoServido(380, 80, 26, Tizador.Cian); p.PlatoServido(450, 80, 26, Tizador.Cian); p.Si(520, 76, null, 12); Basura(p, 400, 180, Tizador.Gris);
                p.Texto(430, 290, "solo lo pedido: basura vacía", 15, Tizador.Cian, true);
            };
            d["sc-valores"] = p => {
                var nombres = new[] { "compromiso", "foco", "apertura", "respeto", "coraje" };
                for (var i = 0; i < 5; i++) { p.Cocinero(60 + i * 120, 170, Tizador.Tiza); p.Texto(60 + i * 120, 250, nombres[i], 16, Tizador.Mostaza, true); }
                p.Curva(40, 100, 60, 120, 80, 100, Tizador.Cian, false); p.Linea(46, 96, 74, 96, Tizador.Cian, 5);
                p.Circulo(180, 96, 22, Tizador.Cian); p.Circulo(180, 96, 12, Tizador.Cian); p.Punto(180, 96, 4, Tizador.Roja);
                p.Caja(282, 70, 16, 56, null, Tizador.Cian); p.Trazo(Tizador.Cian, 298, 70, 326, 82, 326, 118, 298, 126);
                p.Linea(402, 120, 416, 80, Tizador.Cian, 5); p.Linea(438, 120, 424, 80, Tizador.Cian, 5);
                p.Bocadillo(486, 60, 100, 40, "«¡se quemó!»", Tizador.Roja, 14);
            };
            d["sc-equipo"] = p => {
                p.Elipse(300, 172, 120, 100, Tizador.Gris, true);
                for (var i = 0; i < 7; i++) {
                    var a = Mathf.PI * 2 / 7 * i - Mathf.PI / 2; var x = 300 + Mathf.Cos(a) * 120; var y = 160 + Mathf.Sin(a) * 100;
                    if (i == 0) p.Figura(x, y, Tizador.Mostaza); else if (i == 1) p.Figura(x, y, Tizador.Cian); else p.Cocinero(x, y, Tizador.Tiza);
                }
                p.Texto(300, 160, "un solo equipo", 18, Tizador.Tiza, true); p.Texto(300, 186, "nadie es jefe de nadie", 16, Tizador.Mostaza, true);
                p.Texto(300, 30, "Dueño del menú", 14, Tizador.Mostaza, true); p.Texto(450, 96, "Coach", 14, Tizador.Cian, true); p.Texto(110, 270, "cocineros", 14, Tizador.Tiza, true);
            };
            d["sc-po"] = p => {
                for (var i = 0; i < 3; i++) { p.Figura(50, 40 + i * 90, Tizador.Tiza); p.Bocadillo(84, 22 + i * 90, 90, 34, new[] { "sopa", "postre", "pizza" }[i], Tizador.Tiza, 15); }
                p.Figura(290, 120, Tizador.Mostaza, "Dueño del menú"); p.Flecha(330, 140, 390, 140, Tizador.Mostaza);
                for (var i = 0; i < 4; i++) p.Tarjeta(410, 30 + i * 60, 120, 46, i == 0 ? Tizador.Mostaza : Tizador.Tiza);
                p.Flecha(560, 250, 560, 40, Tizador.Mostaza); p.Texto(470, 290, "de más a menos importante", 14, Tizador.Mostaza, true);
            };
            d["sc-sm"] = p => {
                p.Cocinero(60, 110, Tizador.Tiza); p.Tronco(120, 130, 100, Tizador.Roja); p.Texto(140, 200, "un impedimento", 15, Tizador.Roja, true);
                p.Flecha(260, 150, 320, 150, Tizador.Mostaza);
                p.Figura(380, 80, Tizador.Cian, "Coach"); p.Tronco(360, 220, 90, Tizador.Gris); p.Curva(400, 150, 420, 190, 410, 214, Tizador.Cian);
                p.Cocinero(480, 110, Tizador.Tiza); p.Flecha(510, 130, 580, 130, Tizador.Cian); p.Texto(520, 200, "paso libre", 15, Tizador.Cian, true);
            };
            d["sc-dev"] = p => {
                var hace = new[] { "corta", "cocina", "emplata" };
                for (var i = 0; i < 3; i++) { p.Cocinero(80 + i * 140, 90, Tizador.Tiza); p.Texto(80 + i * 140, 160, hace[i], 16, Tizador.Tiza, true); if (i < 2) p.Flecha(120 + i * 140, 110, 180 + i * 140, 110, Tizador.Gris); }
                Cuchillo(p, 70, 190, Tizador.Tiza); p.Olla(190, 180, 60, 44, Tizador.Tiza); p.Vapor(220, 172, Tizador.Gris); p.Plato(360, 206, 30, Tizador.Tiza);
                p.Flecha(410, 110, 470, 110, Tizador.Cian); p.PlatoServido(520, 120, 36, Tizador.Cian); p.Si(520, 180, null, 16); p.Texto(520, 230, "ellos deciden cómo", 15, Tizador.Cian, true);
            };
            d["sc-ciclo"] = p => {
                p.Circulo(330, 150, 96, Tizador.Gris, true); Rollo(p, 30, 60, 150, Tizador.Mostaza); p.Flecha(130, 120, 222, 140, Tizador.Mostaza);
                var nombres = new[] { "Planning", "Daily", "Review", "Retro" };
                for (var i = 0; i < 4; i++) {
                    var a = Mathf.PI / 2 * i - Mathf.PI / 2; var x = 330 + Mathf.Cos(a) * 96; var y = 150 + Mathf.Sin(a) * 96;
                    p.Punto(x, y, 20, Tizador.Pizarra); p.Circulo(x, y, 20, Tizador.Cian);
                    p.Texto(x + (i == 1 ? 70 : i == 3 ? -30 : 0), y + (i == 0 ? -26 : i == 2 ? 44 : i == 3 ? 46 : 6), nombres[i], 15, Tizador.Cian, true);
                    var b = a + Mathf.PI / 4; p.Curva(330 + Mathf.Cos(b - 0.3f) * 96, 150 + Mathf.Sin(b - 0.3f) * 96, 330 + Mathf.Cos(b) * 104, 150 + Mathf.Sin(b) * 104, 330 + Mathf.Cos(b + 0.3f) * 96, 150 + Mathf.Sin(b + 0.3f) * 96, Tizador.Tiza);
                }
                p.PlatoServido(330, 156, 30, Tizador.Mostaza); p.Si(330, 196, null, 10); p.Texto(520, 280, "y vuelta a empezar", 15, Tizador.Tiza, true);
            };
            d["sc-backlog"] = p => {
                p.Linea(180, 10, 380, 10, Tizador.Tiza, 5); p.Linea(190, 10, 190, 270, Tizador.Tiza); p.Linea(370, 10, 370, 270, Tizador.Tiza); p.Curva(190, 270, 280, 300, 370, 270, Tizador.Tiza, false);
                p.Tarjeta(204, 22, 152, 56, Tizador.Cian); p.Tarjeta(204, 86, 152, 50, Tizador.Cian); p.Tarjeta(204, 144, 152, 36, Tizador.Tiza);
                p.Caja(204, 188, 152, 24, null, Tizador.Gris, true); p.Caja(204, 218, 152, 18, null, Tizador.Gris, true); p.Caja(204, 242, 152, 14, null, Tizador.Gris, true);
                p.Figura(70, 100, Tizador.Mostaza, "Dueño del menú"); p.Tarjeta(40, 190, 80, 40, Tizador.Mostaza, "nueva"); p.Curva(120, 200, 160, 160, 196, 170, Tizador.Mostaza);
                p.Texto(480, 60, "arriba: grande y clara", 15, Tizador.Cian, true); p.Texto(480, 230, "abajo: borrosa", 15, Tizador.Gris, true); p.Texto(480, 150, "nunca se termina", 16, Tizador.Tiza, true);
            };
            d["sc-goal"] = p => {
                Rollo(p, 60, 50, 160, Tizador.Tiza); p.Flecha(180, 140, 330, 140, Tizador.Mostaza); p.Linea(340, 200, 580, 200, Tizador.Gris, 2);
                p.Estrella(460, 110, 54, Tizador.Mostaza); p.Texto(460, 240, "«Restaurante con 5 estrellas»", 16, Tizador.Mostaza, true); p.Texto(105, 270, "la lista", 15, Tizador.Tiza, true); p.Texto(460, 270, "a dónde quiere llegar", 15, Tizador.Gris, true);
            };
            d["sc-historia"] = p => {
                p.Caja(60, 10, 480, 180, null, Tizador.Tiza);
                var rotulo = new[] { "Como…", "quiero…", "para…" }; var pie = new[] { "quién", "qué", "por qué" };
                for (var i = 0; i < 3; i++) { p.Caja(80 + i * 154, 50, 132, 110, null, Tizador.Gris); p.Texto(146 + i * 154, 38, rotulo[i], 16, Tizador.Mostaza, true); p.Texto(146 + i * 154, 182, pie[i], 14, Tizador.Gris, true); }
                p.Figura(146, 90, Tizador.Cian); p.Olla(274, 96, 52, 40, Tizador.Cian); p.Vapor(300, 90, Tizador.Gris); Corazon(p, 454, 96, 22, Tizador.Roja);
                var criterios = new[] { "caliente", "sin sal", "en 10 min" };
                for (var i = 0; i < 3; i++) { p.Si(120 + i * 170, 240, null, 10); p.Texto(190 + i * 170, 246, criterios[i], 16, Tizador.Cian, true); }
                p.Texto(300, 284, "criterios de aceptación", 14, Tizador.Gris, true);
            };
            d["sc-refinar"] = p => {
                p.Caja(40, 80, 150, 130, "Banquete", Tizador.Roja, false, 22); p.Flecha(200, 145, 250, 145, Tizador.Mostaza); Cuchillo(p, 290, 138, Tizador.Mostaza); p.Flecha(360, 145, 400, 145, Tizador.Mostaza);
                var trozos = new[] { "entrada", "sopa", "plato fuerte", "postre" };
                for (var i = 0; i < 4; i++) p.Tarjeta(420, 30 + i * 62, 150, 48, Tizador.Cian, trozos[i]);
                p.Texto(115, 244, "demasiado grande", 15, Tizador.Roja, true);
            };
            d["sc-estimar"] = p => {
                var antes = new[] { "3", "5", "5", "13" };
                for (var i = 0; i < 4; i++) { p.Cocinero(50 + i * 68, 80, Tizador.Tiza); p.Caja(30 + i * 68, 140, 40, 56, antes[i], i == 3 ? Tizador.Roja : Tizador.Tiza, false, 22); }
                p.Bocadillo(210, 10, 110, 36, "«falta el horno»", Tizador.Roja, 12); p.Flecha(310, 150, 360, 150, Tizador.Mostaza);
                for (var i = 0; i < 4; i++) p.Caja(380 + i * 52, 110, 40, 56, "5", Tizador.Cian, false, 22);
                p.Texto(150, 240, "todos muestran a la vez", 15, Tizador.Tiza, true); p.Texto(480, 200, "hablan, y se ponen", 15, Tizador.Cian, true); p.Texto(480, 222, "de acuerdo", 15, Tizador.Cian, true);
            };
            d["sc-sprint"] = p => {
                p.Circulo(250, 150, 130, Tizador.Gris, true); p.RelojDeArena(250, 150, 150, Tizador.Tiza);
                p.Hoja(140, 80, 30, 40, Tizador.Cian); p.Esfera(350, 90, 14, Tizador.Cian); p.Plato(350, 210, 18, Tizador.Cian); p.Espejo(140, 190, 26, 34, Tizador.Cian);
                p.Bandera(470, 170, 80, Tizador.Mostaza); p.Candado(520, 120, 16, Tizador.Roja); p.Texto(500, 210, "el objetivo", 16, Tizador.Mostaza, true); p.Texto(500, 232, "no se toca", 16, Tizador.Roja, true);
                p.Texto(250, 296, "un mes o menos, siempre igual de largo", 15, Tizador.Tiza, true);
            };
            d["sc-cancelar"] = p => {
                p.Bocadillo(30, 20, 250, 44, "«el cliente ya no quiere pizza»", Tizador.Roja, 14); p.Figura(150, 130, Tizador.Mostaza, "Dueño del menú");
                p.Mano(250, 110); p.Texto(280, 100, "¡alto!", 22, Tizador.Roja); p.RelojDeArena(460, 140, 150, Tizador.Gris); p.No(460, 140, null, 44);
                p.Texto(300, 270, "solo él puede cancelarlo, si el objetivo ya no sirve", 15, Tizador.Tiza, true);
            };
            d["sc-planning"] = p => {
                p.Elipse(300, 160, 270, 110, Tizador.Tiza); p.Linea(210, 70, 210, 250, Tizador.Gris, 2, true); p.Linea(390, 70, 390, 250, Tizador.Gris, 2, true);
                Corazon(p, 120, 130, 26, Tizador.Roja); p.Texto(120, 210, "¿por qué?", 17, Tizador.Roja, true);
                p.Tarjeta(250, 100, 100, 34, Tizador.Cian); p.Tarjeta(250, 142, 100, 34, Tizador.Cian); p.Texto(300, 210, "¿qué?", 17, Tizador.Cian, true);
                for (var i = 0; i < 3; i++) { p.Punto(420, 112 + i * 26, 4, Tizador.Mostaza); p.Linea(434, 112 + i * 26, 510, 112 + i * 26, Tizador.Mostaza, 3); }
                p.Texto(470, 210, "¿cómo?", 17, Tizador.Mostaza, true); p.Texto(300, 22, "una mesa, tres preguntas", 16, Tizador.Tiza, true);
            };
            d["sc-sprint-goal"] = p => {
                p.Plato(300, 210, 70, Tizador.Tiza); p.Bandera(300, 200, 130, Tizador.Mostaza); p.Texto(300, 40, "«Menú de desayunos listo»", 17, Tizador.Mostaza, true);
                p.Cocinero(60, 110, Tizador.Tiza); p.Flecha(90, 130, 230, 170, Tizador.Cian); p.Cocinero(540, 110, Tizador.Tiza); p.Flecha(510, 130, 380, 170, Tizador.Cian);
                p.Cocinero(110, 240, Tizador.Tiza); p.Flecha(140, 240, 220, 220, Tizador.Cian); p.Cocinero(500, 240, Tizador.Tiza); p.Flecha(470, 240, 390, 220, Tizador.Cian);
            };
            d["sc-sprint-backlog"] = p => {
                p.Caja(150, 10, 300, 280, null, Tizador.Tiza); p.Bandera(280, 70, 46, Tizador.Mostaza); p.Texto(350, 50, "el objetivo", 15, Tizador.Mostaza, true); p.Linea(150, 80, 450, 80, Tizador.Gris, 2);
                for (var i = 0; i < 3; i++) {
                    p.Tarjeta(166, 94 + i * 64, 100, 50, Tizador.Cian);
                    for (var k = 0; k < 3; k++) p.Caja(282 + k * 54, 100 + i * 64, 44, 38, null, Tizador.Mostaza);
                }
                p.Texto(75, 130, "lo elegido", 15, Tizador.Cian, true); p.Texto(525, 130, "y el plan,", 15, Tizador.Mostaza, true); p.Texto(525, 152, "en tareas", 15, Tizador.Mostaza, true);
            };
            d["sc-daily"] = p => {
                p.Tablero(40, 30, 160, 130, Tizador.Gris, "", "", ""); p.Tarjeta(50, 66, 36, 26, Tizador.Cian); p.Tarjeta(102, 66, 36, 26, Tizador.Mostaza);
                for (var i = 0; i < 4; i++) p.Cocinero(250 + i * 64, 130 + (i % 2) * 30, Tizador.Tiza);
                p.Bocadillo(330, 30, 180, 40, "«me bloquea el horno»", Tizador.Roja, 14);
                p.Circulo(110, 230, 36, Tizador.Mostaza); p.Linea(110, 194, 110, 184, Tizador.Mostaza, 5); p.Texto(110, 240, "15", 26, Tizador.Mostaza, true); p.Texto(350, 250, "de pie, 15 minutos, todos los días", 16, Tizador.Tiza, true);
            };
            d["sc-incremento"] = p => {
                for (var i = 0; i < 3; i++) { p.Plato(220, 230 - i * 60, 90 - i * 14, i == 2 ? Tizador.Mostaza : Tizador.Cian); p.Texto(400, 236 - i * 60, "Sprint " + (i + 1), 18, i == 2 ? Tizador.Mostaza : Tizador.Cian); }
                p.Flecha(520, 250, 520, 90, Tizador.Tiza); p.Texto(300, 40, "cada plato encaja sobre el anterior", 16, Tizador.Tiza, true); p.Si(100, 150, null, 20);
            };
            d["sc-dod"] = p => {
                p.PlatoServido(70, 150, 44, Tizador.Tiza); p.Flecha(130, 150, 180, 150, Tizador.Mostaza); p.Caja(190, 30, 220, 240, null, Tizador.Tiza);
                var puntos = new[] { "temperatura", "presentación", "probado", "limpio" };
                for (var i = 0; i < 4; i++) { p.Texto(210, 80 + i * 52, puntos[i], 18, Tizador.Tiza); p.Si(380, 74 + i * 52, null, 10); }
                p.Flecha(420, 150, 460, 150, Tizador.Mostaza); p.Sello(530, 150, "HECHO", Tizador.Cian);
            };
            d["sc-review"] = p => {
                for (var i = 0; i < 2; i++) { p.Cocinero(50 + i * 70, 90, Tizador.Tiza); p.PlatoServido(50 + i * 70, 170, 26, Tizador.Cian); }
                p.Flecha(170, 150, 240, 150, Tizador.Cian);
                p.Carita(290, 100, 24, 1, Tizador.Mostaza); p.Carita(350, 100, 24, 0, Tizador.Tiza); p.Carita(290, 170, 24, -1, Tizador.Roja); p.Texto(320, 230, "opinan", 16, Tizador.Tiza, true);
                p.Flecha(390, 150, 440, 150, Tizador.Mostaza); p.Figura(480, 60, Tizador.Mostaza);
                p.Tarjeta(450, 120, 110, 34, Tizador.Mostaza); p.Tarjeta(450, 162, 110, 34, Tizador.Tiza); p.Curva(574, 180, 596, 158, 574, 136, Tizador.Mostaza); p.Texto(500, 230, "y reordena la lista", 15, Tizador.Mostaza, true);
            };
            d["sc-retro"] = p => {
                p.Espejo(40, 40, 110, 160, Tizador.Tiza); for (var i = 0; i < 3; i++) p.Cocinero(190 + i * 40, 150 + (i % 2) * 20, Tizador.Tiza);
                p.Caja(330, 30, 240, 220, null, Tizador.Tiza); p.Linea(410, 30, 410, 250, Tizador.Gris, 2); p.Linea(490, 30, 490, 250, Tizador.Gris, 2);
                p.Carita(370, 66, 20, 1, Tizador.Cian); p.Carita(450, 66, 20, -1, Tizador.Roja); p.Estrella(530, 66, 20, Tizador.Mostaza);
                p.Texto(370, 276, "funcionó", 14, Tizador.Cian, true); p.Texto(450, 276, "no funcionó", 14, Tizador.Roja, true); p.Texto(530, 276, "a probar", 14, Tizador.Mostaza, true);
                for (var k = 0; k < 3; k++) for (var i = 0; i < 2; i++) p.Caja(342 + k * 80, 110 + i * 56, 56, 40, null, Tizador.Gris);
            };
            d["sc-tablero"] = p => {
                p.Tablero(50, 30, 500, 240, Tizador.Tiza, "Por hacer", "En curso", "Hecho");
                p.Tarjeta(70, 76, 126, 44, Tizador.Cian); p.Tarjeta(70, 132, 126, 44, Tizador.Cian); p.Tarjeta(236, 76, 126, 44, Tizador.Mostaza); p.Tarjeta(404, 76, 126, 44, Tizador.Tiza); p.Tarjeta(404, 132, 126, 44, Tizador.Tiza);
                p.Curva(140, 200, 220, 250, 290, 200, Tizador.Mostaza); p.Curva(310, 200, 390, 250, 460, 200, Tizador.Tiza);
            };
            d["sc-burndown"] = p => {
                p.Grafica(40, 40, 230, 170, Tizador.Cian, 1f, 0.85f, 0.7f, 0.5f, 0.35f, 0.15f, 0f); p.Texto(155, 240, "burndown:", 16, Tizador.Cian, true); p.Texto(155, 262, "lo que queda, día a día", 15, Tizador.Tiza, true);
                p.Barras(330, 40, 230, 170, Tizador.Mostaza, 0.5f, 0.75f, 0.62f); p.Linea(330, 104, 560, 104, Tizador.Roja, 2, true);
                p.Texto(445, 240, "velocidad:", 16, Tizador.Mostaza, true); p.Texto(445, 262, "lo terminado por Sprint", 15, Tizador.Tiza, true);
            };
            d["sc-errores"] = p => {
                p.Figura(60, 50, Tizador.Roja); p.Cocinero(120, 70, Tizador.Tiza); p.Flecha(104, 80, 78, 70, Tizador.Tiza); p.Texto(100, 132, "rendir cuentas al jefe", 14, Tizador.Tiza, true); p.No(100, 70, null, 40);
                p.RelojDeArena(440, 70, 80, Tizador.Tiza); p.Texto(440, 132, "un Sprint sin objetivo", 14, Tizador.Tiza, true); p.No(440, 70, null, 40);
                p.Espejo(74, 170, 50, 60, Tizador.Tiza); p.Linea(70, 180, 130, 220, Tizador.Gris, 8); p.Texto(100, 282, "saltarse la Retro", 14, Tizador.Tiza, true); p.No(100, 204, null, 40);
                p.RelojDeArena(460, 204, 70, Tizador.Tiza); p.Tarjeta(360, 180, 60, 34, Tizador.Mostaza); p.Flecha(424, 198, 446, 200, Tizador.Mostaza); p.Texto(440, 282, "cambios a mitad del Sprint", 14, Tizador.Tiza, true); p.No(440, 204, null, 40);
                p.Linea(300, 20, 300, 280, Tizador.Gris, 2, true); p.Linea(30, 150, 570, 150, Tizador.Gris, 2, true);
            };
            d["sc-escalar"] = p => {
                var x = new[] { 40f, 240, 440 };
                for (var i = 0; i < 3; i++) { p.Caja(x[i], 30, 120, 80, null, Tizador.Tiza); p.Cocinero(x[i] + 36, 60, Tizador.Tiza); p.Cocinero(x[i] + 84, 60, Tizador.Cian); p.Flecha(x[i] + 84, 114, 300 + (i - 1) * 30, 158, Tizador.Cian); }
                p.Circulo(300, 180, 30, Tizador.Cian); p.Texto(300, 186, "juntos", 14, Tizador.Cian, true); p.Flecha(340, 190, 430, 220, Tizador.Mostaza);
                p.Edificio(440, 170, 110, 110, Tizador.Mostaza); p.Texto(150, 240, "varias cocinas,", 16, Tizador.Tiza, true); p.Texto(150, 264, "un solo restaurante", 16, Tizador.Mostaza, true);
            };
            d["sc-resumen"] = p => {
                p.Figura(50, 40, Tizador.Mostaza); p.Figura(110, 40, Tizador.Cian); p.Cocinero(170, 44, Tizador.Tiza); p.Texto(110, 110, "3 roles", 18, Tizador.Mostaza, true);
                p.Esfera(300, 60, 30, Tizador.Tiza);
                for (var i = 0; i < 5; i++) { var a = Mathf.PI * 2 / 5 * i - Mathf.PI / 2; p.Punto(300 + Mathf.Cos(a) * 52, 60 + Mathf.Sin(a) * 46, 7, Tizador.Cian); }
                p.Texto(300, 140, "5 eventos", 18, Tizador.Cian, true);
                Rollo(p, 440, 14, 60, Tizador.Tiza); p.Texto(490, 126, "3 artefactos", 18, Tizador.Tiza, true);
                p.Linea(40, 160, 560, 160, Tizador.Gris, 2, true); p.Texto(300, 184, "cada artefacto, con su compromiso", 15, Tizador.Gris, true);
                Rollo(p, 60, 200, 46, Tizador.Tiza); p.Flecha(160, 230, 190, 230); p.Estrella(216, 230, 18, Tizador.Mostaza);
                p.Tablero(260, 204, 60, 52, Tizador.Tiza, "", ""); p.Flecha(326, 230, 356, 230); p.Bandera(372, 252, 44, Tizador.Mostaza);
                p.PlatoServido(450, 236, 28, Tizador.Tiza); p.Flecha(484, 230, 506, 230); p.Sello(550, 230, "OK", Tizador.Cian);
            };
        }
    }
}
