using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// «Cómo se mide tu proyecto»: por cada barra, que es (m-…-que), que pasa si esta alta o baja (m-…-alto,
    /// m-…-bajo) y como conviene tenerla (m-…-ideal).
    /// </summary>
    public static partial class DibujosDeEscena {
        private static void Metricas(Dictionary<string, Action<Tizador>> d) {
            // ------------------------------------------------------------ avance
            d["m-avance-que"] = p => {
                for (var i = 0; i < 10; i++) {
                    p.Tarjeta(40 + i * 53, 60, 44, 56, i < 6 ? Tizador.Cian : Tizador.Gris);
                    if (i < 6) p.Si(62 + i * 53, 136, null, 9);
                }
                p.Medidor(40, 200, 520, 0.6f, Tizador.Cian); p.Texto(300, 260, "6 de 10 puntos prometidos: 60 % de avance", 18, Tizador.Tiza, true);
            };
            d["m-avance-alto"] = p => {
                p.Medidor(60, 70, 330, 0.96f, Tizador.Cian, "casi todo hecho"); p.Bandera(440, 110, 70, Tizador.Mostaza);
                p.Camion(90, 160, Tizador.Tiza); p.Flecha(220, 190, 400, 190, Tizador.Cian); p.Carita(470, 190, 34, 1, Tizador.Mostaza);
                p.Texto(470, 252, "el cliente lo recibe", 16, Tizador.Mostaza, true); p.Texto(150, 252, "se entrega", 16, Tizador.Tiza, true);
            };
            d["m-avance-bajo"] = p => {
                p.Medidor(60, 70, 330, 0.22f, Tizador.Roja, "falta casi todo"); p.Calendario(440, 40, 100, 100, Tizador.Mostaza); p.Circulo(516, 109, 13, Tizador.Roja);
                p.Texto(490, 166, "y es mañana", 16, Tizador.Roja, true);
                for (var i = 0; i < 5; i++) p.Tarjeta(70 + i * 62, 170, 50, 50, Tizador.Gris);
                p.Texto(220, 252, "lo que no esté hecho no llega al cliente", 16, Tizador.Tiza, true); p.Carita(490, 230, 28, -1, Tizador.Roja);
            };
            d["m-avance-ideal"] = p => {
                p.Grafica(60, 40, 220, 170, Tizador.Cian, 0.05f, 0.25f, 0.45f, 0.65f, 0.85f, 1f); p.Texto(170, 244, "un poco cada día", 17, Tizador.Cian, true); p.Si(170, 270, null, 10);
                p.Grafica(330, 40, 220, 170, Tizador.Roja, 0.05f, 0.1f, 0.12f, 0.15f, 0.2f, 1f); p.Texto(440, 244, "todo al final, a costa de horas extra", 15, Tizador.Roja, true); p.No(440, 272, null, 9);
            };

            // ------------------------------------------------------------ alcance
            d["m-alcance-que"] = p => {
                p.Caja(150, 40, 300, 200, null, Tizador.Mostaza); p.Texto(300, 30, "lo prometido al cliente", 18, Tizador.Mostaza, true);
                for (var f = 0; f < 3; f++) for (var k = 0; k < 4; k++) p.Tarjeta(168 + k * 68, 60 + f * 58, 58, 44, Tizador.Tiza);
                p.Texto(300, 270, "12 tarjetas: ese es el alcance", 18, Tizador.Tiza, true);
            };
            d["m-alcance-alto"] = p => {
                p.Caja(60, 110, 200, 150, null, Tizador.Mostaza);
                for (var i = 0; i < 9; i++) p.Tarjeta(70 + (i % 3) * 62, 20 + (i / 3) * 62 + (i % 3) * 8, 54, 44, i < 4 ? Tizador.Roja : Tizador.Tiza);
                p.Texto(160, 286, "no cabe todo", 17, Tizador.Roja, true);
                p.Figura(400, 120, Tizador.Mostaza, "cliente"); p.Bocadillo(430, 30, 150, 50, "«y además…»", Tizador.Mostaza, 16);
                p.Texto(450, 240, "cada sí es más trabajo", 16, Tizador.Tiza, true); p.Texto(450, 262, "con el mismo tiempo", 16, Tizador.Tiza, true);
            };
            d["m-alcance-bajo"] = p => {
                p.Caja(60, 80, 200, 150, null, Tizador.Mostaza); p.Tarjeta(80, 100, 70, 50, Tizador.Cian); p.Tarjeta(170, 100, 70, 50, Tizador.Cian);
                p.Si(160, 190, null, 16); p.Texto(160, 262, "se termina sin apuros", 16, Tizador.Cian, true);
                p.Figura(430, 130, Tizador.Mostaza, "cliente"); p.Bocadillo(400, 40, 170, 50, "«¿solo esto?»", Tizador.Mostaza, 16);
                p.Texto(430, 250, "puede que no le resuelva el problema", 15, Tizador.Tiza, true);
            };
            d["m-alcance-ideal"] = p => {
                p.Balanza(300, 90, 300, 0, Tizador.Tiza);
                p.Tarjeta(126, 40, 48, 30, Tizador.Mostaza); p.Texto(150, 130, "lo prometido", 17, Tizador.Mostaza, true);
                p.Esfera(450, 54, 17, Tizador.Cian); p.Texto(450, 130, "el tiempo y el equipo", 17, Tizador.Cian, true);
                p.Texto(300, 250, "prometer lo que cabe; lo demás, se negocia", 17, Tizador.Tiza, true);
            };

            // ------------------------------------------------------------ deuda tecnica
            d["m-deuda-que"] = p => {
                for (var f = 0; f < 5; f++) for (var k = 0; k < 6 - f % 2; k++) {
                    var roto = f == 2 && (k == 2 || k == 3);
                    if (!roto) p.Caja(60 + k * 52 + (f % 2) * 26, 50 + f * 34, 46, 28, null, Tizador.Tiza);
                }
                p.Caja(186, 112, 106, 40, "cinta", Tizador.Roja, true, 16); p.Texto(240, 250, "un atajo: hoy funciona", 17, Tizador.Roja, true);
                p.Moneda(480, 110, 30, Tizador.Mostaza); p.Texto(480, 176, "se paga después,", 16, Tizador.Mostaza, true); p.Texto(480, 198, "y con intereses", 16, Tizador.Mostaza, true);
            };
            d["m-deuda-alto"] = p => {
                var xs = new[] { 150f, 172, 140, 178, 150 };
                for (var i = 0; i < 5; i++) p.Caja(xs[i], 216 - i * 44, 110, 40, null, i > 1 ? Tizador.Roja : Tizador.Tiza);
                p.Bicho(120, 110, Tizador.Roja); p.Bicho(310, 70, Tizador.Roja); p.Bicho(300, 200, Tizador.Roja);
                p.Figura(450, 110, Tizador.Tiza); p.Bocadillo(410, 20, 170, 46, "«toco aquí y se rompe allá»", Tizador.Tiza, 13);
                p.Texto(450, 220, "todo va más lento", 17, Tizador.Roja, true); p.Texto(450, 244, "y salen más errores", 17, Tizador.Roja, true);
            };
            d["m-deuda-bajo"] = p => {
                for (var i = 0; i < 4; i++) p.Caja(150, 200 - i * 46, 120, 40, null, Tizador.Cian);
                p.Si(310, 130, null, 18); p.Figura(430, 110, Tizador.Cian); p.Flecha(460, 130, 560, 130, Tizador.Cian);
                p.Texto(470, 200, "cambiar algo es rápido", 16, Tizador.Cian, true); p.Texto(210, 276, "piezas ordenadas", 16, Tizador.Tiza, true);
            };
            d["m-deuda-ideal"] = p => {
                p.Medidor(60, 60, 300, 0.18f, Tizador.Mostaza, "poca, y bajando"); p.Escoba(420, 30, Tizador.Cian);
                p.Texto(450, 130, "pagarla un poco cada día", 16, Tizador.Cian, true);
                p.Caja(60, 150, 220, 90, null, Tizador.Roja, true); p.Texto(170, 186, "un atajo a propósito", 16, Tizador.Roja, true); p.Texto(170, 212, "y apuntado: vale", 16, Tizador.Roja, true);
                p.Caja(320, 150, 220, 90, null, Tizador.Gris, true); p.Texto(430, 186, "atajos sin apuntar", 16, Tizador.Gris, true); p.No(430, 216, null, 10);
            };

            // ------------------------------------------------------------ cobertura de pruebas
            d["m-cobertura-que"] = p => {
                for (var f = 0; f < 3; f++) for (var k = 0; k < 4; k++) {
                    var probado = f * 4 + k < 8;
                    p.Caja(60 + k * 88, 30 + f * 70, 76, 56, null, probado ? Tizador.Cian : Tizador.Gris, !probado);
                    if (probado) p.Si(98 + k * 88, 60 + f * 70, null, 12); else p.Texto(98 + k * 88, 70 + f * 70, "?", 28, Tizador.Gris, true);
                }
                p.Texto(500, 120, "8 de 12", 30, Tizador.Cian, true); p.Texto(500, 156, "partes probadas", 16, Tizador.Cian, true); p.Texto(300, 270, "lo gris nadie lo ha comprobado", 16, Tizador.Gris, true);
            };
            d["m-cobertura-alto"] = p => {
                p.Bicho(60, 90, Tizador.Roja); p.Bicho(90, 160, Tizador.Roja); p.Bicho(50, 220, Tizador.Roja);
                p.Flecha(120, 150, 210, 150, Tizador.Roja); p.Escudo(280, 130, 44, Tizador.Cian); p.Texto(280, 236, "las pruebas los paran", 16, Tizador.Cian, true);
                p.Flecha(350, 150, 440, 150, Tizador.Cian); p.Carita(510, 140, 36, 1, Tizador.Mostaza); p.Texto(510, 210, "al cliente no le llegan", 15, Tizador.Mostaza, true);
            };
            d["m-cobertura-bajo"] = p => {
                p.Escudo(170, 110, 44, Tizador.Gris); p.Linea(140, 90, 200, 170, Tizador.Pizarra, 14); p.Texto(170, 226, "pocas pruebas", 16, Tizador.Gris, true);
                p.Bicho(60, 140, Tizador.Roja); p.Flecha(90, 150, 400, 150, Tizador.Roja); p.Bicho(330, 120, Tizador.Roja); p.Bicho(360, 180, Tizador.Roja);
                p.Carita(490, 140, 36, -1, Tizador.Roja); p.Texto(490, 210, "los encuentra el cliente", 15, Tizador.Roja, true);
            };
            d["m-cobertura-ideal"] = p => {
                p.Bloque(30, 190, 280, 40, Tizador.Cian); p.Bloque(80, 140, 180, 40, Tizador.Mostaza); p.Bloque(130, 90, 80, 40, Tizador.Roja);
                p.Texto(340, 216, "muchas pequeñas y rápidas", 16, Tizador.Cian); p.Texto(340, 166, "algunas que unen piezas", 16, Tizador.Mostaza); p.Texto(340, 116, "pocas de punta a punta", 16, Tizador.Roja);
                p.Texto(300, 276, "y primero, lo que más duele si falla", 17, Tizador.Tiza, true);
            };

            // ------------------------------------------------------------ cansancio
            d["m-cansancio-que"] = p => {
                p.Figura(200, 100, Tizador.Tiza, "tú"); p.Caja(330, 70, 150, 70, null, Tizador.Tiza); p.Caja(480, 92, 12, 26, null, Tizador.Tiza);
                p.Bloque(338, 78, 40, 54, Tizador.Roja); p.Texto(405, 176, "la energía que te queda", 16, Tizador.Tiza, true);
                p.Texto(300, 250, "cada hora de más la gasta; descansar la repone", 17, Tizador.Mostaza, true);
            };
            d["m-cansancio-alto"] = p => {
                p.Curva(80, 30, 50, 60, 80, 90, Tizador.Mostaza, false); p.Curva(80, 30, 96, 60, 80, 90, Tizador.Mostaza, false); p.Texto(80, 120, "23:40", 17, Tizador.Mostaza, true);
                p.Linea(160, 210, 440, 210, Tizador.Tiza, 4); p.Navegador(260, 110, 130, 90, Tizador.Gris); p.Figura(200, 140, Tizador.Roja);
                p.Bicho(300, 160, Tizador.Roja); p.Bicho(350, 170, Tizador.Roja); p.Bicho(480, 120, Tizador.Roja);
                p.Texto(300, 250, "con sueño, todo sale peor y con más errores", 17, Tizador.Roja, true);
            };
            d["m-cansancio-bajo"] = p => {
                p.Circulo(90, 70, 26, Tizador.Mostaza);
                for (var i = 0; i < 8; i++) { var a = Mathf.PI / 4 * i; p.Linea(90 + Mathf.Cos(a) * 34, 70 + Mathf.Sin(a) * 34, 90 + Mathf.Cos(a) * 46, 70 + Mathf.Sin(a) * 46, Tizador.Mostaza, 3); }
                p.Figura(260, 110, Tizador.Cian); p.Caja(330, 90, 150, 60, null, Tizador.Tiza); p.Bloque(338, 98, 134, 44, Tizador.Cian); p.Si(530, 120, null, 18);
                p.Texto(300, 240, "descansado, el día rinde y hay menos errores", 17, Tizador.Cian, true);
            };
            d["m-cansancio-ideal"] = p => {
                p.Grafica(70, 40, 460, 150, Tizador.Mostaza, 0.2f, 0.55f, 0.25f, 0.6f, 0.3f, 0.6f, 0.25f, 0.55f, 0.2f);
                p.Texto(180, 30, "sube al trabajar", 16, Tizador.Mostaza, true); p.Texto(420, 30, "baja al irte a casa", 16, Tizador.Cian, true);
                p.Texto(300, 230, "que suba y baje cada día, sin acumularse", 17, Tizador.Tiza, true); p.Texto(300, 262, "quedarte tarde: solo cuando de verdad haga falta", 16, Tizador.Gris, true);
            };

            // ------------------------------------------------------------ moral
            d["m-moral-que"] = p => {
                var animo = new[] { 1, 0, 1, -1 };
                for (var i = 0; i < 4; i++) { p.Carita(120 + i * 120, 110, 34, animo[i], animo[i] > 0 ? Tizador.Cian : animo[i] < 0 ? Tizador.Roja : Tizador.Tiza); }
                p.Texto(300, 200, "las ganas con las que el equipo viene a trabajar", 18, Tizador.Tiza, true);
                p.Medidor(150, 236, 300, 0.6f, Tizador.Cian);
            };
            d["m-moral-alto"] = p => {
                for (var i = 0; i < 3; i++) p.Carita(90 + i * 90, 110, 30, 1, Tizador.Cian);
                p.Flecha(330, 180, 540, 60, Tizador.Cian); p.Texto(480, 200, "trabajan mejor", 17, Tizador.Cian, true); p.Texto(480, 224, "y se ayudan", 17, Tizador.Cian, true);
                p.Bocadillo(60, 190, 250, 46, "«esto lo sacamos entre todos»", Tizador.Tiza, 15);
            };
            d["m-moral-bajo"] = p => {
                p.Carita(100, 110, 30, -1, Tizador.Roja); p.Carita(190, 110, 30, -1, Tizador.Roja);
                p.Caja(440, 50, 90, 170, null, Tizador.Tiza); p.Punto(456, 136, 4); p.Figura(360, 120, Tizador.Gris); p.Flecha(385, 140, 432, 140, Tizador.Gris);
                p.Texto(450, 250, "alguien se quiere ir", 16, Tizador.Gris, true); p.Texto(145, 190, "sin ganas se trabaja", 16, Tizador.Roja, true); p.Texto(145, 212, "despacio y con descuido", 16, Tizador.Roja, true);
            };
            d["m-moral-ideal"] = p => {
                p.Caja(40, 40, 250, 60, "promesas que se pueden cumplir", Tizador.Cian, false, 15); p.Si(310, 70, null, 12);
                p.Caja(40, 120, 250, 60, "escuchar al equipo", Tizador.Cian, false, 15); p.Si(310, 150, null, 12);
                p.Caja(40, 200, 250, 60, "noches largas una tras otra", Tizador.Roja, true, 15); p.No(310, 230, null, 11);
                p.Carita(470, 120, 46, 1, Tizador.Mostaza); p.Texto(470, 210, "se cuida con decisiones,", 16, Tizador.Mostaza, true); p.Texto(470, 232, "no con discursos", 16, Tizador.Mostaza, true);
            };

            // ------------------------------------------------------------ satisfaccion del cliente
            d["m-satisfaccion-que"] = p => {
                p.Figura(160, 110, Tizador.Mostaza, "tu cliente"); p.Carita(300, 120, 30, 1, Tizador.Mostaza);
                for (var i = 0; i < 5; i++) p.Estrella(390 + i * 40, 120, 16, i < 4 ? Tizador.Mostaza : Tizador.Gris);
                p.Texto(300, 230, "qué tan contento está con lo que le vas entregando", 18, Tizador.Tiza, true);
            };
            d["m-satisfaccion-alto"] = p => {
                p.Figura(150, 100, Tizador.Cian, "tú"); p.Figura(330, 100, Tizador.Mostaza, "cliente"); p.Curva(180, 140, 240, 170, 300, 140, Tizador.Tiza, false); p.Si(240, 110, null, 12);
                p.Flecha(380, 120, 450, 120, Tizador.Mostaza); p.Hoja(470, 70, 90, 110, Tizador.Mostaza, "otro"); p.Texto(515, 210, "vuelve con más trabajo", 15, Tizador.Mostaza, true);
                p.Texto(240, 250, "confía en ti y perdona un tropiezo", 16, Tizador.Tiza, true);
            };
            d["m-satisfaccion-bajo"] = p => {
                p.Caja(80, 90, 110, 100, "!", Tizador.Roja, false, 50); p.Texto(135, 220, "una sorpresa", 16, Tizador.Roja, true);
                p.Flecha(210, 140, 290, 140, Tizador.Roja); p.Carita(360, 140, 40, -1, Tizador.Roja);
                p.Texto(500, 120, "desconfía,", 17, Tizador.Tiza, true); p.Texto(500, 144, "revisa todo", 17, Tizador.Tiza, true); p.Texto(500, 168, "y no repite", 17, Tizador.Tiza, true);
            };
            d["m-satisfaccion-ideal"] = p => {
                var pasos = new[] { "escuchar", "explicar", "cumplir" };
                for (var i = 0; i < 3; i++) {
                    p.Circulo(110 + i * 190, 110, 50, Tizador.Mostaza); p.Texto(110 + i * 190, 200, pasos[i], 20, Tizador.Mostaza, true);
                    if (i < 2) p.Flecha(170 + i * 190, 110, 240 + i * 190, 110, Tizador.Tiza);
                }
                p.Bocadillo(80, 90, 60, 34, "···", Tizador.Tiza, 20); p.Hoja(282, 82, 40, 54, Tizador.Tiza); p.Si(490, 110, null, 20);
                p.Texto(300, 260, "y avisar pronto cuando algo no va a salir", 17, Tizador.Tiza, true);
            };

            // ------------------------------------------------------------ riesgo latente
            d["m-riesgo-que"] = p => {
                var causas = new[] { "cansancio", "deuda técnica", "pocas pruebas" };
                for (var i = 0; i < 3; i++) { p.Caja(40, 40 + i * 80, 190, 56, causas[i], Tizador.Tiza, false, 17); p.Flecha(236, 68 + i * 80, 340, 140 + (i - 1) * 12, Tizador.Roja); }
                p.Trazo(Tizador.Roja, 430, 80, 520, 220, 340, 220, 430, 80); p.Texto(430, 198, "!", 50, Tizador.Roja, true);
                p.Texto(430, 262, "el peligro de entregar", 17, Tizador.Roja, true);
            };
            d["m-riesgo-alto"] = p => {
                p.Camion(60, 100, Tizador.Tiza); p.Si(100, 80, null, 10); p.Texto(120, 190, "terminado", 16, Tizador.Tiza, true);
                p.Flecha(190, 130, 290, 130, Tizador.Roja);
                p.Trazo(Tizador.Roja, 300, 110, 320, 90, 330, 114, 356, 96, 352, 126, 380, 134, 352, 150, 362, 178, 334, 164, 318, 188, 310, 160, 284, 166, 300, 140, 280, 122, 300, 110);
                p.Carita(480, 130, 38, -1, Tizador.Roja); p.Texto(400, 250, "el lanzamiento sale mal aunque esté todo hecho", 16, Tizador.Roja, true);
            };
            d["m-riesgo-bajo"] = p => {
                p.Camion(60, 100, Tizador.Cian); p.Flecha(190, 130, 290, 130, Tizador.Cian); p.Escudo(340, 110, 36, Tizador.Cian); p.Si(340, 126, null, 14);
                p.Carita(480, 130, 38, 1, Tizador.Mostaza); p.Texto(300, 250, "se entrega con calma: nada revienta el último día", 16, Tizador.Cian, true);
            };
            d["m-riesgo-ideal"] = p => {
                p.Medidor(70, 70, 460, 0.25f, Tizador.Cian); p.Linea(300, 50, 300, 110, Tizador.Roja, 3, true); p.Texto(300, 40, "el límite del lanzamiento", 16, Tizador.Roja, true);
                p.Texto(185, 136, "por debajo: sale bien", 16, Tizador.Cian, true); p.Texto(430, 136, "por encima: sale mal", 16, Tizador.Roja, true);
                p.Texto(300, 210, "no se baja directamente:", 17, Tizador.Tiza, true); p.Texto(300, 238, "se baja descansando, pagando deuda y probando", 17, Tizador.Mostaza, true);
            };

            // ------------------------------------------------------------ documentacion
            d["m-documentacion-que"] = p => {
                p.Hoja(110, 60, 110, 150, Tizador.Cian, "SRS"); p.Hoja(245, 60, 110, 150, Tizador.Mostaza, "SAD"); p.Hoja(380, 60, 110, 150, Tizador.Tiza, "PTP");
                p.Texto(300, 250, "lo que queda escrito del proyecto: qué hace, cómo es y cómo se prueba", 16, Tizador.Tiza, true);
            };
            d["m-documentacion-alto"] = p => {
                p.Figura(120, 110, Tizador.Cian, "alguien nuevo"); p.Hoja(200, 70, 100, 130, Tizador.Tiza); p.Flecha(320, 130, 400, 130, Tizador.Cian);
                p.Si(450, 126, null, 22); p.Texto(470, 190, "lo entiende solo", 17, Tizador.Cian, true); p.Texto(300, 260, "nadie tiene que adivinar ni preguntar dos veces", 16, Tizador.Tiza, true);
            };
            d["m-documentacion-bajo"] = p => {
                p.Figura(150, 110, Tizador.Tiza, "tú"); p.Texto(150, 60, "?", 44, Tizador.Mostaza, true);
                p.Figura(460, 110, Tizador.Gris, "se fue"); p.Bocadillo(360, 20, 200, 46, "solo lo sabía él", Tizador.Gris, 16); p.Flecha(500, 130, 570, 130, Tizador.Gris);
                p.Texto(300, 250, "lo que no está escrito se pierde, y cada uno lo supone a su manera", 16, Tizador.Roja, true);
            };
            d["m-documentacion-ideal"] = p => {
                p.Balanza(300, 80, 300, 0, Tizador.Tiza); p.Hoja(130, 8, 40, 50, Tizador.Cian); p.Esfera(450, 42, 18, Tizador.Mostaza);
                p.Texto(150, 120, "escribir lo que importa", 16, Tizador.Cian, true); p.Texto(450, 120, "sin quitarle tiempo a construir", 16, Tizador.Mostaza, true);
                p.Texto(300, 250, "lo justo: decisiones, reglas y lo que alguien nuevo necesita", 16, Tizador.Tiza, true);
            };
        }
    }
}
