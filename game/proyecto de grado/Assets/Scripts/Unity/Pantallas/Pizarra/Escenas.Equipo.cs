using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;
using L = Nexus.Unity.Tema.LuzUI;
using Nexus.Unity.Tema;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Lo que el juego antes daba por sabido (g-…): el ticket y la gente de un proyecto, el tablero y sus graficas,
    /// los diagramas y las palabras tecnicas del dia a dia.
    /// </summary>
    public static partial class DibujosDeEscena {
        private static void Equipo(Dictionary<string, Action<Tizador>> d) {
            // ------------------------------------------------------------ el ticket y la gente
            d["g-ticket"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 201);
                Credencial(v, 70, 92, Aviso, "quien lo pide");
                FlechaNeon(v, 104, 130, 196, 130, Aviso);
                PanelUI(v, 210, 26, 180, 218);
                v.Forma(L.Redondeado(210, 26, 180, 40, 12), Aviso, H(0xB0922E));
                v.Rotulo(300, 46, "TICKET  #1042", 16, H(0x241C02), 0, true);
                Renglones(v, 228, 88, 144, 6, 22);
                FlechaNeon(v, 404, 130, 476, 130, Nucleo);
                Credencial(v, 512, 82, Nucleo); Credencial(v, 556, 104, Nucleo);
                v.Rotulo(534, 200, "quien lo hace", 15, Nucleo, 0, true);
                v.Rotulo(300, 282, "un encargo por escrito, con su número", 17, Tinta);
            };
            d["g-ticket-vida"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Nucleo, 0.5f, 203);
                var estado = new[] { "abierto", "en curso", "resuelto", "cerrado" }; var c = new[] { Aviso, Nucleo, Menta, TintaSuave };
                var quien = new[] { "alguien lo pide", "alguien lo hace", "se comprueba", "queda el registro" };
                for (var i = 0; i < 4; i++) {
                    var x = 20 + i * 146;
                    PanelUI(v, x, 100, 120, 60);
                    v.Halo(x + 18, 130, 12, A(c[i], 0.6f)); v.Forma(L.Elipse(x + 18, 130, 5, 5, 12), Color.Lerp(c[i], Color.white, 0.4f));
                    v.Rotulo(x + 66, 130, estado[i], 16, c[i], 0, true);
                    if (i < 3) FlechaNeon(v, x + 124, 130, x + 142, 130, Tinta);
                    v.Rotulo(x + 60, 196, quien[i], 14, TintaSuave);
                }
                v.Neon(Tizador.Bezier(new Vector2(372, 96), new Vector2(300, 22), new Vector2(228, 96), 30), 1.3f, Coral);
                v.Forma(Pts(228, 96, 220, 82, 236, 84), Coral);
                v.Rotulo(300, 40, "«no era eso»: se reabre", 15, Coral);
            };

            // ------------------------------------------------------------ el tablero y sus graficas
            d["g-historia"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 110, Aviso, 0.5f, 205);
                PanelUI(v, 90, 20, 420, 190);
                v.Forma(L.Redondeado(90, 20, 420, 12, 6), Aviso);
                v.Rotulo(112, 74, "Como recepcionista,", 21, Tinta, -1);
                v.Rotulo(112, 112, "quiero registrar una visita", 21, Nucleo, -1);
                v.Rotulo(112, 150, "para saber quién está dentro.", 21, Aviso, -1);
                v.Halo(470, 176, 30, A(Tinta, 0.2f));
                v.Forma(L.Elipse(470, 176, 20, 20, 24), H(0x2B4F57), H(0x11262C)); Anillo(v, 470, 176, 20, 2, Tinta);
                v.Rotulo(470, 176, "3", 22, Tinta, 0, false, true);
                v.Rotulo(150, 246, "quién", 16, Tinta, 0, true); v.Rotulo(300, 246, "qué", 16, Nucleo, 0, true); v.Rotulo(430, 246, "para qué", 16, Aviso, 0, true);
                v.Rotulo(550, 176, "puntos", 13, TintaSuave, 0, true);
            };
            d["g-columnas"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 207);
                PanelUI(v, 20, 24, 560, 206);
                var cols = new[] { "por hacer", "en curso", "revisión", "hecho" }; var cs = new[] { TintaSuave, Aviso, Nucleo, Menta };
                for (var i = 0; i < 4; i++) {
                    var x = 20 + i * 140;
                    if (i > 0) v.Forma(Caja(x, 38, 1.2f, 180), A(Borde, 0.8f));
                    v.Rotulo(x + 70, 46, cols[i], 13, cs[i], 0, true);
                }
                TarjetaUI(v, 32, 70, 116, 40, TintaSuave); TarjetaUI(v, 32, 118, 116, 40, TintaSuave);
                TarjetaUI(v, 172, 70, 116, 40, Aviso); TarjetaUI(v, 312, 70, 116, 40, Nucleo); TarjetaUI(v, 452, 70, 116, 40, Menta);
                v.Neon(Tizador.Bezier(new Vector2(90, 196), new Vector2(300, 250), new Vector2(500, 196), 30), 1.3f, Aviso);
                v.Forma(Pts(510, 190, 494, 192, 502, 206), Aviso);
                v.Rotulo(300, 274, "cada tarjeta viaja de izquierda a derecha", 17, Aviso);
            };
            d["g-pr"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 209);
                PanelUI(v, 26, 26, 236, 196, "el cambio", Tinta);
                var signo = new[] { "+", "+", "-", "+" };
                for (var i = 0; i < 4; i++) {
                    var col = signo[i] == "+" ? Menta : Coral; var y = 84 + i * 32;
                    v.Forma(L.Redondeado(40, y - 12, 208, 24, 4), A(col, 0.12f));
                    v.Rotulo(54, y, signo[i], 20, col, 0, false, true);
                    v.Forma(L.Redondeado(74, y - 3, 150 - i * 18, 5, 2.5f), A(col, 0.9f));
                }
                FlechaNeon(v, 270, 124, 318, 124, Tinta);
                Credencial(v, 368, 74, Aviso, "otra persona");
                Lupa(v, 404, 112, 20, Aviso);
                FlechaNeon(v, 432, 124, 480, 124, Nucleo);
                v.Halo(530, 124, 40, A(Menta, 0.35f));
                v.Neon(Pts(512, 124, 526, 138, 550, 106), 2.2f, Menta);
                v.Rotulo(530, 176, "aprobado", 16, Menta, 0, true);
                v.Rotulo(300, 270, "nadie mezcla su código sin que otro lo mire", 17, Tinta);
            };
            d["g-burndown"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 211);
                PanelUI(v, 60, 16, 480, 228);
                for (var x = 84f; x < 516; x += 18) { var t = (x - 84) / 432; v.Forma(L.Redondeado(x, 34 + t * 196 - 1.2f, 9, 2.4f, 1.2f), A(TintaSuave, 0.6f)); }
                v.Neon(new List<Vector2> { new Vector2(84, 34), new Vector2(170, 54), new Vector2(256, 110), new Vector2(342, 128), new Vector2(428, 196), new Vector2(516, 230) }, 1.6f, Nucleo);
                v.Rotulo(430, 112, "lo ideal", 15, TintaSuave, 0, true); v.Rotulo(240, 72, "lo real", 15, Nucleo, 0, true);
                v.Rotulo(30, 34, "todo", 13, TintaSuave, 0, true); v.Rotulo(30, 230, "nada", 13, TintaSuave, 0, true);
                v.Rotulo(300, 260, "días del sprint", 14, TintaSuave, 0, true);
                v.Rotulo(300, 286, "lo que queda por hacer, bajando", 17, Tinta);
            };
            d["g-cfd"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 213);
                PanelUI(v, 50, 14, 500, 236);
                var hecho = new[] { 0.05f, 0.15f, 0.25f, 0.3f, 0.4f, 0.5f }; var curso = new[] { 0.15f, 0.15f, 0.2f, 0.35f, 0.35f, 0.3f }; var porHacer = new[] { 0.6f, 0.55f, 0.45f, 0.3f, 0.2f, 0.15f };
                var capas = new[] { Menta, Aviso, Nucleo };
                for (var i = 0; i < 6; i++) {
                    var x = 80 + i * 76; var b = 236f;
                    var fr = new[] { hecho[i], curso[i], porHacer[i] };
                    for (var k = 0; k < 3; k++) {
                        var h = 200 * fr[k];
                        v.Forma(Caja(x, b - h, 56, h - 1.5f), Color.Lerp(capas[k], Color.white, 0.15f), A(capas[k], 0.7f));
                        b -= h;
                    }
                }
                v.Rotulo(300, 274, "cada día, cuánto hay en cada columna del tablero", 16, Tinta);
            };
            d["g-curva-s"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 215);
                PanelUI(v, 60, 14, 480, 230);
                v.Neon(new RutaDeCurva(84, 226).C(200, 226, 250, 226, 300, 130).C(350, 34, 400, 32, 516, 32).Puntos(), 1.8f, Nucleo);
                v.Rotulo(170, 206, "arranca lento", 15, Tinta, 0, true); v.Rotulo(380, 130, "acelera", 15, Aviso, 0, true); v.Rotulo(450, 72, "se frena al cerrar", 15, Tinta, 0, true);
                v.Rotulo(300, 272, "el avance acumulado dibuja una S", 17, Nucleo);
            };
            d["g-velocidad"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 217);
                PanelUI(v, 60, 20, 370, 228);
                var alt = new[] { 0.6f, 0.8f, 0.7f, 0.75f }; var pts = new[] { "8", "11", "9", "10" };
                for (var i = 0; i < 4; i++) {
                    var h = 170 * alt[i];
                    v.Forma(L.Redondeado(96 + i * 80, 216 - h, 50, h, 6), Color.Lerp(Nucleo, Color.white, 0.2f), A(Nucleo, 0.6f));
                    v.Rotulo(121 + i * 80, 234, "S" + (i + 1) + ": " + pts[i], 14, Tinta, 0, true);
                }
                for (var x = 76f; x < 414; x += 16) v.Forma(L.Redondeado(x, 88, 9, 2.6f, 1.3f), Aviso);
                v.Rotulo(500, 84, "promedio:", 16, Aviso, 0, true); v.Rotulo(500, 114, "9,5 puntos", 22, Aviso, 0, false, true); v.Rotulo(500, 146, "por sprint", 16, Aviso, 0, true);
                v.Rotulo(300, 280, "sirve para saber cuánto cabe en el siguiente", 16, Tinta);
            };
            d["g-planning"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 219);
                for (var i = 0; i < 5; i++) TarjetaUI(v, 30, 26 + i * 48, 150, 40, i < 3 ? Nucleo : Borde);
                v.Rotulo(105, 280, "lo más importante, arriba", 14, Tinta, 0, true);
                FlechaNeon(v, 190, 90, 286, 118, Nucleo);
                PanelUI(v, 300, 40, 220, 214);
                for (var x = 304f; x < 516; x += 16) v.Forma(L.Redondeado(x, 88, 9, 2.6f, 1.3f), Coral);
                v.Rotulo(410, 72, "hasta aquí cabe", 15, Coral, 0, true);
                for (var i = 0; i < 3; i++) TarjetaUI(v, 336, 104 + i * 46, 148, 38, Nucleo);
                v.Rotulo(410, 280, "lo que el equipo puede hacer", 14, Aviso, 0, true);
            };

            // ------------------------------------------------------------ diagramas y tecnica
            d["g-componentes"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 221);
                FlechaNeon(v, 194, 75, 236, 75, Aviso); FlechaNeon(v, 394, 75, 446, 75, Aviso); FlechaNeon(v, 315, 114, 315, 176, Aviso);
                PanelUI(v, 40, 40, 150, 70); v.Rotulo(115, 75, "Recepción", 16, Nucleo, 0, true);
                PanelUI(v, 240, 40, 150, 70); v.Rotulo(315, 75, "Registro", 16, Nucleo, 0, true);
                PanelUI(v, 240, 180, 150, 70); v.Rotulo(315, 215, "Aviso", 16, Nucleo, 0, true);
                Icono(v.En(426, 22, 0.95f), "base-de-datos", Tizador.Tiza);
                v.Rotulo(215, 58, "usa", 13, Aviso, 0, true);
                v.Rotulo(110, 200, "cajas: las piezas", 16, Nucleo, 0, true);
                v.Rotulo(110, 226, "flechas: quién usa a quién", 15, Aviso, 0, true);
            };
            d["g-incidencia"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 140, 140, Coral, 0.6f, 223);
                Icono(v.En(60, 70, 1.2f), "servidor", Tizador.Tiza);
                Baliza(v, 140, 78);
                Chispa(v, 120, 150, 9, 0.2f); Chispa(v, 170, 196, 6, 0.6f);
                v.Forma(L.Elipse(290, 96, 7, 7, 14), A(Menta, 0.45f)); v.Rotulo(310, 96, "ayer funcionaba", 18, TintaSuave, -1);
                v.Halo(290, 166, 18, A(Coral, 0.6f)); v.Forma(L.Elipse(290, 166, 7, 7, 14), Color.Lerp(Coral, Color.white, 0.4f)); v.Rotulo(310, 166, "hoy está caído", 18, Coral, -1);
                v.Rotulo(340, 262, "algo que ya funcionaba deja de hacerlo: hay que atenderlo ya", 15, Tinta);
            };
        }
    }
}
