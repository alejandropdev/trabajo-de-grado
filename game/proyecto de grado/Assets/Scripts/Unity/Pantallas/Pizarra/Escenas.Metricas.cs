using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using UnityEngine;
using L = Nexus.Unity.Tema.LuzUI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// «Cómo se mide tu proyecto»: por cada barra, que es (m-…-que), que pasa si esta alta o baja (m-…-alto,
    /// m-…-bajo) y como conviene tenerla (m-…-ideal).
    /// </summary>
    public static partial class DibujosDeEscena {
        private static void Metricas(Dictionary<string, Action<Tizador>> d) {
            // ------------------------------------------------------------ avance
            d["m-avance-que"] = p => {
                var v = p.Vitrina();
                FondoExterior(v, 0.15f, 120, 230, 2);
                TorreDelCliente(v, 530, 236, 172, 0.55f, Nucleo);
                Via(v, 24, 486, 166, 10, 6, 236, Nucleo, true);
                Capsula(v, 230, 166, 66, Nucleo, false);
                v.Rotulo(36, 44, "60 %", 34, Tinta, -1, false, true);
                v.Rotulo(38, 76, "de avance", 14, A(Tinta, 0.8f), -1, true);
                v.Rotulo(530, 252, "cliente", 12, A(Sol, 0.85f), 0, true);
                v.Rotulo(300, 276, "6 de 10 puntos prometidos: 60 % de avance", 19, Tinta);
            };
            d["m-avance-alto"] = p => {
                var v = p.Vitrina();
                FondoExterior(v, 0.05f, 90, 226, 4);
                v.Halo(530, 70, 170, A(Nucleo, 0.22f));
                TorreDelCliente(v, 530, 236, 172, 1, Nucleo);
                Via(v, 24, 486, 166, 10, 9.6f, 236, Nucleo);
                Capsula(v, 376, 166, 70, Nucleo, true);
                v.Rotulo(36, 44, "96 %", 34, Tinta, -1, false, true);
                v.Rotulo(38, 76, "casi todo hecho", 14, A(Tinta, 0.8f), -1, true);
                v.Rotulo(120, 262, "se entrega", 14, Tinta, 0, true);
                v.Rotulo(470, 262, "el cliente lo recibe", 14, Sol, 0, true);
            };
            d["m-avance-bajo"] = p => {
                var v = p.Vitrina();
                FondoExterior(v, 0.72f, 470, 238, 6);
                TorreDelCliente(v, 530, 236, 172, 0.3f, Coral);
                // El cartel de neon de la torre: la fecha de entrega, que parpadea.
                var brillo = v.Halo(452, 130, 92, 36, A(Coral, 0.35f));
                v.Cable(Pts(500, 127, 524, 127), 2, 2, SiluetaFuera);
                v.Forma(Pts(380, 116, 508, 106, 504, 146, 386, 154), H(0x12081A), SiluetaFuera);
                v.Neon(Pts(380, 116, 508, 106, 504, 146), 0.9f, Coral);
                v.Neon(Pts(504, 146, 386, 154, 380, 116), 0.9f, Coral);
                var fecha = v.Rotulo(444, 131, "ES MAÑANA", 14, Coral, 0, false, true);
                v.Parpadeo(brillo, 0.2f, 0.4f); v.Parpadeo(fecha, 0.35f, 0.4f);
                Via(v, 24, 486, 166, 10, 2.2f, 236, Nucleo);
                Capsula(v, 34, 166, 62, Coral, false);
                v.Forma(L.Elipse(65, 143, 4, 3.5f, 12, 180, 360), Coral);
                v.Latido(v.Halo(65, 140, 18, A(Coral, 0.8f)), 0.2f, 1, 1.1f);
                v.Rotulo(36, 44, "22 %", 34, Tinta, -1, false, true);
                v.Rotulo(38, 76, "falta casi todo", 14, Coral, -1, true);
                v.Rotulo(300, 276, "lo que no esté hecho no llega al cliente", 19, Tinta);
            };
            d["m-avance-ideal"] = p => {
                var v = p.Vitrina();
                FondoExterior(v, 0.2f, 300, 236, 8);
                v.Forma(Caja(299, 30, 2, 196), A(Tinta, 0.12f));
                Rampa(v, Pts(30, 214, 80, 188, 130, 162, 180, 136, 230, 110, 280, 84), Nucleo, 236, false);
                v.Halo(548, 52, 42, A(NucleoClaro, 0.2f));
                v.Forma(Tizador.Luna(548, 52, 11), BombillaCentro);
                Rampa(v, Pts(320, 214, 370, 211, 420, 207, 470, 202, 520, 196, 572, 80), Coral, 236, true);
                var dias = new[] { "L", "M", "X", "J", "V" };
                for (var i = 0; i < 5; i++) {
                    v.Rotulo(55 + i * 50, 250, dias[i], 12, A(Tinta, 0.6f));
                    v.Rotulo(345 + i * 50, 250, dias[i], 12, A(Tinta, 0.6f));
                }
                v.Rotulo(155, 38, "así", 14, Menta, 0, true);
                v.Rotulo(445, 38, "así no", 14, Coral, 0, true);
                v.Rotulo(155, 276, "un poco cada día", 18, Nucleo);
                v.Rotulo(445, 276, "todo al final, a costa de horas extra", 15, Coral);
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
                var v = p.Vitrina();
                FondoInterior(v, 230, 70, Ambar, 0.85f, 3);
                var b = Guirnalda(v, 16, 26, 584, 34, 22, 8, 5);
                v.Rotulo((b[0].x + b[4].x) / 2, 100, "la energía que te queda", 14, Ambar, 0, true);
                v.Rotulo((b[5].x + b[7].x) / 2, 100, "ya gastada", 13, TintaSuave, 0, true);
                Escritorio(v, 170, 572, 216, Ambar);
                Monitor(v, 330, 216, 136, 84, Nucleo, A(Nucleo, 0.8f), A(Nucleo, 0.8f), A(Nucleo, 0.8f), A(Nucleo, 0.8f));
                Taza(v, 470, 216, 0, 0.35f, Ambar);
                Silla(v, 112, 214, true, Ambar);
                v.Rotulo(40, 108, "tú", 16, Tinta, -1, true);
                v.Rotulo(300, 278, "cada hora de más la <color=#EF7A5E>gasta</color>; descansar la <color=#7ED9AE>repone</color>", 18, Tinta);
            };
            d["m-cansancio-alto"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 320, 150, Nucleo, 0.45f, 5);
                Guirnalda(v, 16, 22, 584, 28, 16, 8, 1, 0);
                OjoDeBuey(v, 70, 90, 40, true);
                Reloj(v, 530, 96, 32, 23, 40, Aviso);
                v.Rotulo(530, 144, "23:40", 18, Aviso, 0, false, true);
                Escritorio(v, 150, 580, 222, Nucleo);
                Monitor(v, 320, 222, 190, 118, Nucleo, Nucleo, Coral, Nucleo, Coral, Coral, Nucleo, Coral);
                Taza(v, 448, 222, 3, 0, Nucleo);
                Taza(v, 480, 222, 0, 0, Nucleo);
                Taza(v, 516, 222, 2, 0, Nucleo);
                Silla(v, 160, 220, true, Nucleo);
                v.Rotulo(300, 278, "con sueño, todo sale peor y con más errores", 18, Coral);
            };
            d["m-cansancio-bajo"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 70, 90, Sol, 1, 7);
                v.Haz(70, 90, 380, 130, 52, A(Sol, 0.07f));
                v.Haz(70, 90, 320, 90, 68, A(Sol, 0.06f));
                Guirnalda(v, 16, 22, 584, 28, 16, 8, 8);
                OjoDeBuey(v, 70, 90, 40, false);
                Escritorio(v, 150, 580, 222, Sol);
                Monitor(v, 320, 222, 190, 118, Menta, Menta, Nucleo, Menta, Nucleo, Nucleo, Menta, Nucleo);
                Taza(v, 470, 222, 0, 1, Sol);
                Silla(v, 160, 220, true, Sol);
                v.Rotulo(300, 278, "descansado, el día rinde y hay menos errores", 18, Menta);
            };
            d["m-cansancio-ideal"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.6f, 9);
                // Cuatro dias: la jornada (banda clara, con su bombilla) y la noche en casa (con su luna).
                var curva = new RutaDeCurva(40, 196);
                for (var dia = 0; dia < 4; dia++) {
                    float x0 = 40 + dia * 130, x1 = x0 + 86, x2 = x1 + 44;
                    v.Forma(L.Redondeado(x0 + 2, 40, 82, 170, 12), A(CostillaLejos, 0.16f), A(CostillaLejos, 0.03f));
                    v.Halo(x0 + 43, 56, 24, A(Ambar, 0.3f));
                    v.Halo(x0 + 43, 56, 8, A(BombillaCentro, 0.9f));
                    v.Forma(Tizador.Luna(x1 + 22, 58, 7), A(BombillaCentro, 0.8f));
                    curva.C(x0 + 40, 196, x1 - 30, 100, x1, 100).C(x1 + 16, 100, x2 - 16, 196, x2, 196);
                }
                v.Neon(curva.Puntos(), 1.6f, Nucleo);
                for (var dia = 0; dia < 4; dia++) {
                    float x1 = 126 + dia * 130, x2 = x1 + 44;
                    v.Halo(x1, 100, 9, A(Aviso, 0.95f));
                    v.Halo(x2, 196, 8, A(NucleoClaro, 0.95f));
                }
                v.Rotulo(126, 24, "sube al trabajar", 13, Aviso, 0, true);
                v.Forma(Caja(125.3f, 33, 1.4f, 56), A(Aviso, 0.5f));
                v.Rotulo(430, 24, "baja al irte a casa", 13, Nucleo, 0, true);
                v.Forma(Caja(429.3f, 33, 1.4f, 152), A(Nucleo, 0.45f));
                v.Rotulo(300, 244, "que suba y baje cada día, sin acumularse", 18, Tinta);
                v.Rotulo(300, 274, "quedarte tarde: solo cuando de verdad haga falta", 15, TintaSuave);
            };

            // ------------------------------------------------------------ moral
            d["m-moral-que"] = p => {
                // Las ganas del equipo, contadas por sus tazas: el vapor de cada una.
                var v = p.Vitrina();
                FondoInterior(v, 300, 70, Ambar, 0.9f, 11);
                Lampara(v, 300, 70, Ambar);
                v.Halo(300, 200, 250, 26, A(Ambar, 0.3f));
                Escritorio(v, 50, 550, 198, Ambar);
                Taza(v, 140, 198, 0, 1, Ambar);
                Taza(v, 245, 198, 2, 0.4f, Ambar);
                Taza(v, 355, 198, 1, 0.9f, Ambar);
                Taza(v, 460, 198, 3, 0, Ambar);
                v.Rotulo(300, 262, "las ganas con las que el equipo viene a trabajar", 19, Tinta);
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
                // Tres valvulas meten presion en la caldera; el manometro es el peligro de entregar.
                var v = p.Vitrina();
                FondoInterior(v, 440, 150, Coral, 0.55f, 13);
                var causas = new[] { "cansancio", "deuda técnica", "pocas pruebas" };
                for (var i = 0; i < 3; i++) {
                    float vy = 62 + i * 82, ty = 112 + i * 38;
                    var tubo = new RutaDeCurva(64, vy).C(200, vy, 250, ty, 372, ty).Puntos();
                    v.Cable(tubo, 13, 13, CostillaMedia);
                    v.Cable(L.Mover(tubo, 0, -3.5f), 3, 3, A(CostillaLejos, 0.55f));
                    for (var k = 0; k < 3; k++) {
                        var q = tubo[(int)(tubo.Count * (0.35f + k * 0.22f))];
                        v.Latido(v.Halo(q.x, q.y, 14, A(Coral, 0.45f)), 0.2f, 1, 1.4f, k * 0.33f + i * 0.2f);
                    }
                }
                for (var i = 0; i < 3; i++) {
                    float vy = 62 + i * 82;
                    Valvula(v, 52, vy, 20, CostillaLejos);
                    v.Rotulo(84, vy - 26, causas[i], 13, Tinta, -1, true);
                }
                v.Forma(L.Redondeado(362, 34, 156, 228, 60), CostillaMedia, PrimerPlano, new Vector2(362, 0), new Vector2(518, 0));
                v.Forma(L.Redondeado(376, 64, 10, 170, 5), A(CostillaLejos, 0.35f));
                foreach (var by in new[] { 96f, 206f }) {
                    v.Forma(Caja(362, by, 156, 5), A(Silueta, 0.6f));
                    for (var k = 0; k < 7; k++) v.Forma(L.Elipse(374 + k * 22, by + 9, 1.8f, 1.8f, 8), A(CostillaLejos, 0.7f));
                }
                Manometro(v, 440, 152, 46, 0.86f);
                v.Forma(L.Elipse(440, 36, 13, 11, 20, 180, 360), Coral, H(0x8A2E1E));
                v.Latido(v.Halo(440, 30, 40, A(Coral, 0.55f)), 0.25f, 1, 1.2f);
                v.Rotulo(440, 282, "el peligro de entregar", 16, Coral, 0, true);
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

        }
    }
}
