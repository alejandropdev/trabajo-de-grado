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
                var v = p.Vitrina();
                FondoExterior(v, 0.15f, 520, 232, 12);
                Via(v, -20, 620, 214, 8, 8, 236, Nucleo);
                var cama = Plataforma(v, 140, 214, 320);
                for (var f = 0; f < 3; f++) for (var k = 0; k < 4; k++) CajaDeCarga(v, 152 + k * 75, cama - 34 - f * 36, 70, 33, H(0x3F7F88));
                v.Rotulo(300, 46, "lo prometido al cliente", 17, Sol, 0, true);
                v.Rotulo(300, 276, "12 tarjetas: ese es el alcance", 19, Tinta);
            };
            d["m-alcance-alto"] = p => {
                var v = p.Vitrina();
                FondoExterior(v, 0.25f, 300, 236, 14);
                Via(v, -20, 620, 214, 8, 8, 236, Nucleo);
                var cama = Plataforma(v, 40, 214, 250);
                for (var k = 0; k < 3; k++) CajaDeCarga(v, 52 + k * 76, cama - 34, 72, 33, H(0x3F7F88));
                for (var k = 0; k < 3; k++) CajaDeCarga(v, 60 + k * 76, cama - 70, 70, 33, H(0x3F7F88), k == 2 ? 6 : 0);
                CajaDeCarga(v, 80, cama - 108, 70, 33, Coral, -8);
                CajaDeCarga(v, 160, cama - 112, 70, 33, Coral, 10);
                CajaDeCarga(v, 120, cama - 150, 70, 33, Coral, -4);
                CajaDeCarga(v, 250, cama - 40, 66, 31, Coral, 28);
                v.Rotulo(165, 262, "no cabe todo", 17, Coral, 0, true);
                Altavoz(v, 520, 96, 236, Sol);
                Bocadillo(v, 352, 34, 160, 42, "«y además…»", Sol, 470, 92, 17);
                v.Rotulo(410, 168, "cada sí es más trabajo", 16, Tinta);
                v.Rotulo(410, 192, "con el mismo tiempo", 16, Tinta);
            };
            d["m-alcance-bajo"] = p => {
                var v = p.Vitrina();
                FondoExterior(v, 0.15f, 120, 232, 16);
                Via(v, -20, 620, 214, 8, 8, 236, Nucleo);
                var cama = Plataforma(v, 40, 214, 250);
                CajaDeCarga(v, 82, cama - 34, 72, 33, Menta);
                CajaDeCarga(v, 164, cama - 34, 72, 33, Menta);
                v.Halo(160, cama - 18, 120, 30, A(Menta, 0.2f));
                v.Rotulo(165, 262, "se termina sin apuros", 16, Menta, 0, true);
                Altavoz(v, 520, 110, 236, Sol);
                Bocadillo(v, 352, 44, 160, 42, "«¿solo esto?»", Sol, 470, 104, 17);
                v.Rotulo(450, 276, "puede que no le resuelva el problema", 15, Tinta);
            };
            d["m-alcance-ideal"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 70, Ambar, 0.8f, 17);
                var platos = Balanza(v, 300, 70, 150, 236);
                for (var k = 0; k < 2; k++) CajaDeCarga(v, platos[0].x - 40 + k * 41, platos[0].y - 25, 38, 24, H(0x3F7F88));
                CajaDeCarga(v, platos[0].x - 20, platos[0].y - 50, 38, 24, H(0x3F7F88));
                Reloj(v, platos[1].x - 16, platos[1].y - 22, 18, 5, 0, Nucleo);
                Taza(v, platos[1].x + 22, platos[1].y - 2, 0, 0.6f, Ambar);
                v.Rotulo(platos[0].x, platos[0].y + 36, "lo prometido", 16, Ambar, 0, true);
                v.Rotulo(platos[1].x, platos[1].y + 36, "el tiempo y el equipo", 16, Nucleo, 0, true);
                v.Rotulo(300, 278, "prometer lo que cabe; lo demás, se negocia", 18, Tinta);
            };

            // ------------------------------------------------------------ deuda tecnica
            d["m-deuda-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 220, 110, Ambar, 0.7f, 19);
                Tubo(v, Pts(-10, 110, 380, 110), 22, CostillaMedia);
                Brida(v, 110, 110, 22); Brida(v, 330, 110, 22);
                Parche(v, 220, 110, 22);
                for (var i = 0; i < 3; i++) v.Latido(v.Halo(222, 134 + i * 26, 4, A(NucleoClaro, 0.8f)), 0.2f, 1, 1.6f, i * 0.33f);
                Vapor(v, 214, 96, 0.5f, 2);
                v.Rotulo(220, 196, "un atajo: hoy funciona", 17, Coral, 0, true);
                v.Rotulo(220, 222, "cinta", 13, A(Tinta, 0.6f), 0, true);
                Monedas(v, 440, 200, 3); Monedas(v, 476, 200, 6); Monedas(v, 512, 200, 10);
                v.Rotulo(476, 230, "se paga después,", 16, Aviso, 0, true);
                v.Rotulo(476, 254, "y con intereses", 16, Aviso, 0, true);
            };
            d["m-deuda-alto"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 260, 140, Coral, 0.5f, 21);
                Tubo(v, new RutaDeCurva(-10, 70).C(120, 70, 140, 150, 260, 150).C(330, 150, 340, 90, 420, 90).Puntos(), 18, CostillaMedia);
                Tubo(v, new RutaDeCurva(-10, 200).C(80, 200, 120, 120, 200, 120).L(250, 120).C(300, 120, 300, 210, 380, 210).Puntos(), 16, CostillaMedia);
                Tubo(v, new RutaDeCurva(100, 300).C(100, 240, 180, 230, 240, 190).Puntos(), 14, CostillaMedia);
                Parche(v, 80, 82, 18); Parche(v, 200, 140, 18); Parche(v, 330, 98, 18); Parche(v, 150, 160, 16); Parche(v, 330, 196, 16);
                Vapor(v, 200, 128, 0.8f, 1); Vapor(v, 330, 84, 0.6f, 3);
                Chispa(v, 160, 200, 7, 0.1f); Chispa(v, 360, 70, 6, 0.5f); Chispa(v, 60, 120, 6, 0.8f);
                Llave(v, 50, 230, 60, -30);
                v.Rotulo(470, 60, "«toco aquí y se rompe allá»", 15, Tinta);
                v.Rotulo(470, 196, "todo va más lento", 17, Coral);
                v.Rotulo(470, 222, "y salen más errores", 17, Coral);
            };
            d["m-deuda-bajo"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.7f, 23);
                for (var i = 0; i < 4; i++) {
                    var y = 70 + i * 40;
                    Tubo(v, Pts(-10, y, 610, y), 16, CostillaMedia);
                    Brida(v, 160, y, 16); Brida(v, 420, y, 16);
                    for (var k = 0; k < 3; k++) v.Latido(v.Halo(220 + k * 70 + i * 18, y, 10, A(Nucleo, 0.6f)), 0.3f, 1, 1.8f, k * 0.3f + i * 0.15f);
                }
                Valvula(v, 290, 230, 18, CostillaLejos);
                v.Forma(L.Arco(290, 230, 28, 28, 2.5f, 200, 330, 16), A(Nucleo, 0.9f));
                v.Rotulo(150, 270, "piezas ordenadas", 16, Tinta, 0, true);
                v.Rotulo(450, 270, "cambiar algo es rápido", 16, Nucleo, 0, true);
            };
            d["m-deuda-ideal"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 90, Ambar, 0.7f, 25);
                Llave(v, 70, 52, 54, -12);
                v.Rotulo(150, 44, "poca, y bajando", 15, Aviso, -1, true);
                v.Rotulo(470, 44, "pagarla un poco cada día", 16, Nucleo);
                Tubo(v, Pts(30, 138, 280, 138), 20, CostillaMedia);
                Parche(v, 150, 138, 20, true);
                v.Rotulo(155, 226, "un atajo a propósito", 16, Menta, 0, true);
                v.Rotulo(155, 250, "y apuntado: vale", 16, Menta, 0, true);
                Tubo(v, Pts(320, 138, 570, 138), 20, CostillaMedia);
                Parche(v, 380, 138, 20); Parche(v, 450, 138, 20); Parche(v, 520, 138, 20);
                Vapor(v, 450, 124, 0.6f, 4); Chispa(v, 520, 170, 6, 0.3f);
                v.Rotulo(445, 226, "atajos sin apuntar", 16, Coral, 0, true);
                v.Rotulo(445, 250, "no", 14, A(Coral, 0.8f), 0, true);
            };

            // ------------------------------------------------------------ cobertura de pruebas
            d["m-cobertura-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 220, 120, Menta, 0.6f, 27);
                Tubo(v, new RutaDeCurva(-10, 96).C(120, 70, 260, 70, 400, 96).Puntos(), 10, CostillaCerca);
                Tubo(v, new RutaDeCurva(-10, 196).C(120, 170, 260, 170, 400, 196).Puntos(), 10, CostillaCerca);
                for (var i = 0; i < 12; i++) {
                    var fila = i / 6; var col = i % 6;
                    var y = (fila == 0 ? 60 : 160) - Mathf.Sin((col + 0.5f) / 6 * Mathf.PI) * 14;
                    Modulo(v, 20 + col * 62, y, 54, 50, i % 3 != 2);
                }
                v.Rotulo(500, 116, "8 de 12", 32, Menta, 0, false, true);
                v.Rotulo(500, 152, "partes probadas", 16, Menta, 0, true);
                v.Rotulo(300, 272, "lo gris nadie lo ha comprobado", 17, TintaSuave);
            };
            d["m-cobertura-alto"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 250, 140, Nucleo, 0.6f, 29);
                Tubo(v, Pts(-10, 150, 610, 150), 26, CostillaCerca);
                Chispa(v, 50, 120, 7, 0.0f); Chispa(v, 90, 176, 6, 0.4f); Chispa(v, 140, 128, 7, 0.7f); Chispa(v, 196, 160, 6, 0.2f); Chispa(v, 218, 130, 5, 0.5f);
                Rejilla(v, 236, 96, 44, 110, false, Nucleo);
                v.Rotulo(258, 236, "las pruebas los paran", 16, Nucleo, 0, true);
                for (var k = 0; k < 3; k++) v.Latido(v.Halo(330 + k * 50, 150, 10, A(Nucleo, 0.6f)), 0.3f, 1, 1.6f, k * 0.3f);
                Capsula(v, 470, 168, 82, Nucleo, false);
                v.Rotulo(470, 214, "al cliente no le llegan", 15, Sol, 0, true);
            };
            d["m-cobertura-bajo"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 250, 140, Coral, 0.5f, 31);
                Tubo(v, Pts(-10, 150, 610, 150), 26, CostillaCerca);
                Chispa(v, 60, 130, 6, 0.0f); Chispa(v, 130, 170, 6, 0.4f);
                Rejilla(v, 236, 96, 44, 110, true, Nucleo);
                v.Rotulo(258, 236, "pocas pruebas", 16, TintaSuave, 0, true);
                Chispa(v, 330, 140, 7, 0.2f); Chispa(v, 380, 166, 6, 0.6f); Chispa(v, 430, 132, 7, 0.9f);
                Capsula(v, 470, 168, 82, Coral, false);
                Chispa(v, 520, 140, 8, 0.3f);
                v.Rotulo(470, 214, "los encuentra el cliente", 15, Coral, 0, true);
            };
            d["m-cobertura-ideal"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 170, 140, Nucleo, 0.6f, 33);
                // Tres repisas de lamparas: muchas pequeñas abajo, algunas en medio, pocas grandes arriba.
                var ys = new[] { 216f, 160f, 104f }; var ns = new[] { 9, 5, 2 }; var rs = new[] { 5f, 8f, 12f };
                var anchos = new[] { 280f, 190f, 100f }; var cs = new[] { Nucleo, Aviso, Coral };
                for (var f = 0; f < 3; f++) {
                    v.Forma(L.Redondeado(170 - anchos[f] / 2 - 10, ys[f] + 16, anchos[f] + 20, 7, 3), Cromo, CromoOscuro);
                    for (var i = 0; i < ns[f]; i++) {
                        var x = 170 - anchos[f] / 2 + i * anchos[f] / (ns[f] - 1);
                        v.Halo(x, ys[f], rs[f] * 3.4f, A(cs[f], 0.4f));
                        v.Forma(L.Elipse(x, ys[f], rs[f], rs[f] * 1.2f, 16), BombillaCentro, cs[f]);
                    }
                }
                v.Rotulo(340, 222, "muchas pequeñas y rápidas", 16, Nucleo, -1);
                v.Rotulo(340, 166, "algunas que unen piezas", 16, Aviso, -1);
                v.Rotulo(340, 110, "pocas de punta a punta", 16, Coral, -1);
                v.Rotulo(300, 274, "y primero, lo que más duele si falla", 18, Tinta);
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
                var v = p.Vitrina();
                FondoInterior(v, 230, 70, Ambar, 1, 35);
                Lampara(v, 230, 70, Ambar);
                v.Halo(230, 200, 220, 24, A(Ambar, 0.32f));
                Escritorio(v, 40, 420, 198, Ambar);
                Taza(v, 100, 198, 0, 1, Ambar);
                Taza(v, 170, 198, 2, 0.9f, Ambar);
                Taza(v, 250, 198, 1, 1, Ambar);
                Taza(v, 330, 198, 3, 0.8f, Ambar);
                v.Rotulo(230, 258, "«esto lo sacamos entre todos»", 17, Tinta);
                v.Rotulo(510, 120, "trabajan mejor", 18, Nucleo, 0, true);
                v.Rotulo(510, 148, "y se ayudan", 18, Nucleo, 0, true);
            };
            d["m-moral-bajo"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 470, 150, Ambar, 0.45f, 37);
                PuertaEntreabierta(v, 430, 60, 80, 236);
                Escritorio(v, 30, 340, 200, CostillaLejos);
                Taza(v, 100, 200, 3, 0, CostillaLejos);
                Taza(v, 170, 200, 0, 0, CostillaLejos);
                Silla(v, 280, 198, false, CostillaLejos);
                v.Rotulo(150, 246, "sin ganas se trabaja", 16, Coral, 0, true);
                v.Rotulo(150, 270, "despacio y con descuido", 16, Coral, 0, true);
                v.Rotulo(470, 270, "alguien se quiere ir", 16, Tinta, 0, true);
            };
            d["m-moral-ideal"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 470, 110, Ambar, 0.8f, 39);
                PlacaDeDecision(v, 30, 40, 310, 54, "promesas que se pueden cumplir", true);
                PlacaDeDecision(v, 30, 112, 310, 54, "escuchar al equipo", true);
                PlacaDeDecision(v, 30, 184, 310, 54, "noches largas una tras otra", false);
                v.Halo(470, 150, 120, 60, A(Ambar, 0.25f));
                Taza(v, 440, 160, 0, 1, Ambar); Taza(v, 500, 160, 1, 0.9f, Ambar);
                v.Forma(L.Redondeado(400, 160, 140, 6, 3), Cromo, CromoOscuro);
                v.Rotulo(470, 212, "se cuida con decisiones,", 16, Aviso, 0, true);
                v.Rotulo(470, 236, "no con discursos", 16, Aviso, 0, true);
            };

            // ------------------------------------------------------------ satisfaccion del cliente
            d["m-satisfaccion-que"] = p => {
                var v = p.Vitrina();
                FondoExterior(v, 0.3f, 120, 232, 41);
                TorreDelCliente(v, 470, 236, 190, 0.8f, Nucleo);
                CartelDeEstrellas(v, 120, 92, 4);
                v.Cable(Pts(342, 112, 464, 112), 2, 2, SiluetaFuera);
                v.Rotulo(470, 252, "tu cliente", 13, A(Sol, 0.9f), 0, true);
                v.Rotulo(300, 278, "qué tan contento está con lo que le vas entregando", 18, Tinta);
            };
            d["m-satisfaccion-alto"] = p => {
                var v = p.Vitrina();
                FondoExterior(v, 0.1f, 80, 228, 43);
                v.Halo(470, 80, 160, A(Sol, 0.2f));
                TorreDelCliente(v, 470, 236, 190, 1, Nucleo);
                CartelDeEstrellas(v, 120, 44, 5);
                v.Cable(Pts(342, 64, 464, 64), 2, 2, SiluetaFuera);
                Via(v, -20, 440, 190, 6, 6, 236, Nucleo);
                var cama = Plataforma(v, 160, 190, 140);
                CajaDeCarga(v, 190, cama - 34, 80, 33, Aviso);
                v.Rotulo(230, cama - 46, "otro", 13, Aviso, 0, true);
                v.Rotulo(230, 254, "vuelve con más trabajo", 15, Aviso, 0, true);
                v.Rotulo(300, 280, "confía en ti y perdona un tropiezo", 17, Tinta);
            };
            d["m-satisfaccion-bajo"] = p => {
                var v = p.Vitrina();
                FondoExterior(v, 0.7f, 470, 240, 45);
                TorreDelCliente(v, 470, 236, 190, 0.15f, Coral);
                CartelDeEstrellas(v, 120, 92, 1, true);
                v.Cable(Pts(342, 112, 464, 112), 2, 2, SiluetaFuera);
                Via(v, -20, 300, 200, 4, 4, 236, Nucleo);
                Capsula(v, 40, 200, 74, Coral, false);
                Chispa(v, 120, 176, 9, 0.2f);
                v.Rotulo(110, 262, "una sorpresa", 15, Coral, 0, true);
                v.Rotulo(420, 266, "desconfía, revisa todo y no repite", 17, Tinta);
            };
            d["m-satisfaccion-ideal"] = p => {
                var v = p.Vitrina();
                FondoExterior(v, 0.2f, 300, 236, 47);
                Via(v, -20, 620, 170, 8, 8, 236, Nucleo);
                // Tres estaciones en la via: escuchar, explicar, cumplir.
                Altavoz(v, 130, 110, 166, Sol);
                v.Forma(Tizador.Rotar(Caja(268, 86, 64, 46), 300, 109, -6), H(0x2E6E8E), H(0x1A4A63));
                for (var i = 0; i < 3; i++) v.Forma(Tizador.Rotar(Caja(276, 96 + i * 10, 40 - i * 8, 2), 300, 109, -6), A(Tinta, 0.8f));
                var cama = Plataforma(v, 420, 170, 120);
                CajaDeCarga(v, 444, cama - 34, 72, 33, Menta);
                v.Halo(480, cama - 18, 70, A(Menta, 0.25f));
                var pasos = new[] { "escuchar", "explicar", "cumplir" };
                for (var i = 0; i < 3; i++) v.Rotulo(110 + i * 190, 52, pasos[i], 19, Sol, 0, true);
                v.Rotulo(300, 274, "y avisar pronto cuando algo no va a salir", 18, Tinta);
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
                var v = p.Vitrina();
                FondoInterior(v, 440, 140, Coral, 0.9f, 49);
                Tubo(v, Pts(130, 150, 372, 150), 14, CostillaMedia);
                Capsula(v, 30, 170, 90, Nucleo, false);
                v.Rotulo(75, 196, "terminado", 15, Tinta, 0, true);
                v.Forma(L.Redondeado(362, 34, 156, 228, 60), CostillaMedia, PrimerPlano, new Vector2(362, 0), new Vector2(518, 0));
                Manometro(v, 440, 150, 46, 1);
                // La caldera revienta: vapor y un fogonazo coral.
                v.Latido(v.Halo(440, 40, 160, 90, A(Coral, 0.45f)), 0.5f, 1, 0.8f);
                Vapor(v, 400, 40, 1, 1); Vapor(v, 470, 44, 1, 5); Vapor(v, 520, 120, 0.8f, 7);
                Chispa(v, 380, 70, 9, 0.2f); Chispa(v, 520, 60, 8, 0.6f);
                v.Rotulo(300, 282, "el lanzamiento sale mal aunque esté todo hecho", 17, Coral);
            };
            d["m-riesgo-bajo"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 440, 150, Menta, 0.6f, 51);
                Tubo(v, Pts(130, 150, 372, 150), 14, CostillaMedia);
                Capsula(v, 40, 170, 90, Nucleo, true);
                v.Forma(L.Redondeado(362, 34, 156, 228, 60), CostillaMedia, PrimerPlano, new Vector2(362, 0), new Vector2(518, 0));
                Manometro(v, 440, 150, 46, 0.18f);
                v.Forma(L.Elipse(440, 36, 13, 11, 20, 180, 360), Menta, H(0x2E7A5A));
                v.Halo(440, 30, 30, A(Menta, 0.5f));
                v.Rotulo(300, 282, "se entrega con calma: nada revienta el último día", 17, Menta);
            };
            d["m-riesgo-ideal"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Coral, 0.4f, 53);
                Manometro(v, 300, 150, 92, 0.25f);
                // El limite del lanzamiento: una marca en lo alto de la esfera.
                v.Forma(Caja(298.5f, 50, 3, 26), Coral);
                v.Halo(300, 60, 20, A(Coral, 0.6f));
                v.Rotulo(300, 30, "el límite del lanzamiento", 15, Coral, 0, true);
                v.Rotulo(120, 120, "por debajo: sale bien", 15, Menta);
                v.Rotulo(480, 120, "por encima: sale mal", 15, Coral);
                v.Rotulo(300, 262, "no se baja directamente:", 16, Tinta);
                v.Rotulo(300, 286, "se baja descansando, pagando deuda y probando", 16, Aviso);
            };

        }
    }
}
