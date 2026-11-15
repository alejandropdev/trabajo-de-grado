using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;
using L = Nexus.Unity.Tema.LuzUI;
using Nexus.Unity.Tema;

namespace Nexus.Unity.Pantallas {
    /// <summary>El producto y lo que falta por hacer: el MVP y sus parientes (mvp-…) y el backlog (bl-…).</summary>
    public static partial class DibujosDeEscena {
        private static void Patineta(Tizador p, float x, float y, Color c) { p.Linea(x, y, x + 70, y, c, 5); p.Circulo(x + 14, y + 10, 7, c); p.Circulo(x + 56, y + 10, 7, c); }
        private static void Bici(Tizador p, float x, float y, Color c) {
            p.Circulo(x + 16, y + 30, 16, c); p.Circulo(x + 74, y + 30, 16, c); p.Trazo(c, x + 16, y + 30, x + 40, y + 6, x + 62, y + 6, x + 74, y + 30); p.Trazo(c, x + 40, y + 6, x + 46, y + 30, x + 62, y + 6);
        }

        private static void Producto(Dictionary<string, Action<Tizador>> d) {
            // ------------------------------------------------------------ MVP
            d["mvp-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Nucleo, 0.5f, 501);
                var y0 = Ventana(v, 60, 50, 220, 180);
                var partes = new[] { "registrar", "avisar", "quién sigue" };
                for (var i = 0; i < 3; i++) Pieza(v, 80, y0 + 16 + i * 44, 180, 34, partes[i], Nucleo);
                FlechaNeon(v, 296, 140, 366, 140, Tinta);
                Credencial(v, 430, 80, Aviso, "lo usa de verdad");
                NeonCheck(v, 500, 92, 16);
                v.Rotulo(300, 272, "pequeño, pero completo: ya resuelve el problema", 18, Tinta);
            };
            d["mvp-si"] = p => {
                var v = p.Vitrina();
                FondoExterior(v, 0.2f, 300, 236, 503);
                Via(v, -20, 620, 160, 8, 8, 236, Nucleo);
                Plataforma(v, 40, 160, 90);
                Capsula(v, 222, 160, 76, Nucleo, false);
                Capsula(v, 400, 160, 140, Nucleo, true);
                FlechaNeon(v, 148, 104, 204, 104, A(Tinta, 0.7f)); FlechaNeon(v, 320, 104, 376, 104, A(Tinta, 0.7f));
                foreach (var x in new[] { 85f, 260f, 470f }) NeonCheck(v, x, 56, 18);
                v.Rotulo(300, 272, "cada entrega ya sirve para moverse", 20, Nucleo);
            };
            d["mvp-no"] = p => {
                var v = p.Vitrina();
                FondoExterior(v, 0.35f, 300, 236, 505);
                Via(v, -20, 620, 160, 8, 8, 236, Nucleo);
                Anillo(v, 85, 136, 18, 6, Cromo); v.Forma(L.Elipse(85, 136, 4, 4, 10), CromoOscuro);
                v.Forma(L.Redondeado(222, 140, 90, 6, 3), Cromo, CromoOscuro);
                Anillo(v, 230, 146, 8, 4, Cromo); Anillo(v, 304, 146, 8, 4, Cromo);
                Capsula(v, 400, 160, 140, Nucleo, false);
                FlechaNeon(v, 148, 104, 204, 104, A(Tinta, 0.7f)); FlechaNeon(v, 320, 104, 376, 104, A(Tinta, 0.7f));
                NeonX(v, 85, 56, 18); NeonX(v, 265, 56, 18); NeonCheck(v, 470, 56, 18);
                v.Rotulo(300, 272, "nada sirve hasta el final", 20, Coral);
            };
            d["mvp-escalera"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 380, 120, Aviso, 0.5f, 507);
                var pasos = new[] { "boceto", "mockup", "prototipo", "prototipo", "MVP", "beta", "producto" };
                var sub = new[] { "", "", "sin función", "con función", "", "", "completo" };
                for (var i = 0; i < 7; i++) {
                    float x = 20 + i * 80, y = 250 - i * 30;
                    var c = i == 4 ? Aviso : CostillaMedia;
                    if (i == 4) v.Halo(x + 40, y + 10, 70, 30, A(Aviso, 0.35f));
                    v.Forma(L.Redondeado(x, y, 78, 300 - y, 4), Color.Lerp(c, Color.white, 0.12f), Color.Lerp(c, Silueta, 0.5f));
                    v.Forma(Caja(x + 2, y, 74, 2), A(Color.white, 0.3f));
                    v.Rotulo(x + 40, y - 12, pasos[i], 15, i == 4 ? Aviso : Tinta);
                    if (sub[i].Length > 0) v.Rotulo(x + 40, y + 16, sub[i], 12, TintaSuave);
                }
                FlechaNeon(v, 40, 120, 296, 34, Nucleo);
                v.Rotulo(130, 50, "cada vez más real", 17, Nucleo, 0, true);
            };
            d["mvp-boceto"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Aviso, 0.5f, 509);
                Papel(v, 170, 20, 260, 240, null, null, -2);
                foreach (var r in new[] { Caja(194, 44, 212, 36), Caja(194, 96, 100, 90), Caja(306, 96, 100, 40), Caja(306, 146, 100, 40) }) Contorno(v, r, 1.6f, A(H(0x1E2A2A), 0.6f));
                v.Forma(Caja(194, 206, 186, 2), A(H(0x1E2A2A), 0.5f)); v.Forma(Caja(194, 226, 136, 2), A(H(0x1E2A2A), 0.5f));
                v.Forma(Tizador.Rotar(L.Redondeado(470, 52, 90, 10, 3), 470, 57, -30), Aviso, H(0xB0922E));
                v.Forma(Tizador.Rotar(Pts(458, 57, 470, 52, 470, 62), 470, 57, -30), H(0x3A2A1E));
                v.Rotulo(510, 110, "a lápiz,", 17, Aviso, 0, true); v.Rotulo(510, 134, "en minutos", 17, Aviso, 0, true);
                v.Rotulo(90, 140, "la idea", 17, Tinta, 0, true); v.Rotulo(90, 164, "en rayas", 17, Tinta, 0, true);
            };
            d["mvp-mockup"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Coral, 0.4f, 511);
                Ventana(v, 150, 20, 300, 240);
                v.Forma(L.Redondeado(170, 60, 260, 36, 6), Nucleo, A(Nucleo, 0.6f));
                v.Forma(L.Redondeado(170, 108, 120, 90, 6), Aviso, A(Aviso, 0.6f));
                Pieza(v, 304, 108, 126, 40, null, Tinta); Pieza(v, 304, 158, 126, 40, null, Tinta);
                v.Forma(L.Redondeado(170, 212, 110, 30, 15), Coral);
                v.Forma(Pts(490, 168, 490, 200, 498, 192, 506, 208, 512, 205, 504, 190, 516, 190), Tinta, TintaSuave);
                NeonX(v, 530, 150, 18);
                v.Rotulo(520, 250, "se ve, no se toca", 16, Coral, 0, true);
                v.Rotulo(75, 130, "una imagen", 16, Tinta, 0, true); v.Rotulo(75, 154, "con el aspecto", 16, Tinta, 0, true); v.Rotulo(75, 178, "final", 16, Tinta, 0, true);
            };
            d["mvp-proto-nf"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 513);
                for (var i = 0; i < 3; i++) { Ventana(v, 30 + i * 195, 40, 150, 130); Renglones(v, 46 + i * 195, 78, 110, 2, 14); }
                BotonUI(v, 104, 128, 64, 28, "ir", Aviso); BotonUI(v, 299, 128, 64, 28, "ir", Aviso);
                v.Neon(Tizador.Bezier(new Vector2(168, 142), new Vector2(200, 100), new Vector2(222, 112), 16), 1, Aviso);
                v.Neon(Tizador.Bezier(new Vector2(363, 142), new Vector2(395, 100), new Vector2(417, 112), 16), 1, Aviso);
                Icono(v.En(256, 178, 0.6f), "base-de-datos", Tizador.Gris);
                NeonX(v, 340, 220, 20);
                v.Rotulo(110, 230, "se puede hacer clic,", 16, Aviso, 0, true); v.Rotulo(110, 254, "pasa de pantalla", 16, Aviso, 0, true);
                v.Rotulo(480, 236, "pero no guarda nada", 16, Coral, 0, true);
            };
            d["mvp-proto-f"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.6f, 515);
                Ventana(v, 40, 50, 160, 140);
                BotonUI(v, 56, 96, 128, 34, "una función", Aviso, true);
                v.Forma(L.Redondeado(56, 142, 128, 30, 15), A(TintaSuave, 0.12f));
                FlechaNeon(v, 206, 120, 262, 120, Nucleo);
                Icono(v.En(238, 56, 0.95f), "servidor", Tizador.Tiza);
                FlechaNeon(v, 360, 120, 416, 120, Nucleo);
                Icono(v.En(404, 56, 0.85f), "base-de-datos", Tizador.Cian);
                v.Rotulo(300, 230, "una sola cosa funciona de punta a punta", 18, Nucleo);
                v.Rotulo(300, 258, "sirve para probar una idea; después se suele desechar", 16, TintaSuave);
            };
            d["mvp-minimo"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 517);
                var y0 = Ventana(v, 200, 30, 200, 160, null, Aviso);
                for (var i = 0; i < 3; i++) { Pieza(v, 218, y0 + 14 + i * 38, 130, 28, null, Nucleo); NeonCheck(v, 374, y0 + 28 + i * 38, 11); }
                Credencial(v, 100, 74, Tinta); Credencial(v, 56, 120, Tinta); Credencial(v, 144, 120, Tinta);
                FlechaNeon(v, 168, 110, 194, 110, Tinta);
                v.Rotulo(500, 70, "clientes reales", 17, Tinta, 0, true); v.Rotulo(500, 94, "lo usan", 17, Tinta, 0, true);
                Bocadillo(v, 434, 130, 136, 40, "«falta esto»", Aviso, 450, 190, 15);
                v.Rotulo(300, 264, "lo esencial, bien hecho, y a aprender de quien lo usa", 17, Aviso);
            };
            d["mvp-beta"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 519);
                var y0 = Ventana(v, 180, 30, 240, 180);
                for (var f = 0; f < 3; f++) for (var k = 0; k < 2; k++) {
                    if (f == 2 && k == 1) v.Forma(L.Redondeado(196 + k * 106, y0 + 14 + f * 44, 96, 34, 8), A(TintaSuave, 0.12f));
                    else Pieza(v, 196 + k * 106, y0 + 14 + f * 44, 96, 34, null, Nucleo);
                }
                Chispa(v, 100, 78, 9, 0.2f); Lupa(v, 110, 146, 26, Aviso);
                v.Rotulo(100, 220, "se cazan errores", 16, Aviso, 0, true);
                Credencial(v, 480, 66, Tinta); Credencial(v, 530, 96, Tinta);
                v.Rotulo(500, 200, "con un grupo", 16, Tinta, 0, true); v.Rotulo(500, 222, "pequeño", 16, Tinta, 0, true);
                v.Rotulo(300, 270, "casi todo está, y se pule antes de abrirlo a todos", 17, Nucleo);
            };
            d["mvp-completo"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Menta, 0.5f, 521);
                var y0 = Ventana(v, 60, 40, 240, 180);
                for (var f = 0; f < 3; f++) for (var k = 0; k < 2; k++) Pieza(v, 76 + k * 106, y0 + 16 + f * 44, 96, 34, null, Nucleo);
                v.Halo(370, 70, 30, A(Sol, 0.45f)); v.FormaDeLuz(Estrella(370, 70, 18), A(Sol, 0.95f)); v.Forma(Estrella(370, 70, 7), BombillaCentro);
                v.Rotulo(400, 72, "todo lo planeado", 17, Aviso, -1, true);
                Llave(v, 344, 132, 44, -20); v.Rotulo(400, 132, "con mantenimiento", 17, Tinta, -1, true);
                Credencial(v, 370, 170, Nucleo); v.Rotulo(400, 200, "y con soporte", 17, Nucleo, -1, true);
                v.Rotulo(180, 272, "abierto a todos los usuarios", 17, Tinta);
            };

            // ------------------------------------------------------------ backlog
            d["bl-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 523);
                var filas = new[] { "registrar la entrada", "avisar al anfitrión", "quién sigue dentro", "exportar a Excel", "cambiar colores" };
                for (var i = 0; i < 5; i++) {
                    v.Rotulo(150, 50 + i * 50, (i + 1).ToString(), 22, i < 3 ? Aviso : TintaSuave, 0, false, true);
                    Pieza(v, 180, 30 + i * 50, 250, 40, filas[i], i < 3 ? Nucleo : TintaSuave);
                }
                FlechaNeon(v, 480, 250, 480, 40, Aviso);
                v.Rotulo(540, 60, "lo más", 16, Aviso, 0, true); v.Rotulo(540, 82, "valioso", 16, Aviso, 0, true);
                v.Rotulo(540, 240, "puede esperar", 15, TintaSuave);
                v.Rotulo(70, 150, "todo lo", 17, Tinta, 0, true); v.Rotulo(70, 174, "que falta", 17, Tinta, 0, true);
            };
            d["bl-detalle"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 525);
                PanelUI(v, 60, 18, 230, 90, "registrar la entrada"); Renglones(v, 78, 64, 194, 3, 12);
                PanelUI(v, 60, 118, 230, 60); Renglones(v, 78, 138, 194, 2, 14);
                v.Forma(L.Redondeado(60, 188, 230, 36, 8), A(TintaSuave, 0.14f));
                v.Forma(L.Redondeado(60, 232, 230, 26, 8), A(TintaSuave, 0.1f));
                v.Forma(L.Redondeado(60, 266, 230, 18, 8), A(TintaSuave, 0.07f));
                v.Rotulo(440, 60, "arriba: claro y detallado,", 16, Nucleo, 0, true); v.Rotulo(440, 84, "listo para empezar", 16, Nucleo, 0, true);
                v.Rotulo(440, 230, "abajo: una idea suelta,", 16, TintaSuave, 0, true); v.Rotulo(440, 254, "ya se detallará", 16, TintaSuave, 0, true);
            };
            d["bl-dueno"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 280, 120, Aviso, 0.5f, 527);
                for (var i = 0; i < 3; i++) {
                    Credencial(v, 50 + i * 56, 64 + (i % 2) * 70, Tinta);
                    v.Neon(Tizador.Bezier(new Vector2(74 + i * 56, 96 + (i % 2) * 70), new Vector2(200, 170), new Vector2(246, 134), 16), 0.8f, A(TintaSuave, 0.8f));
                }
                Credencial(v, 280, 96, Aviso, "ordena la lista");
                FlechaNeon(v, 316, 130, 392, 130, Aviso);
                for (var i = 0; i < 4; i++) TarjetaUI(v, 410, 40 + i * 54, 150, 42, i == 0 ? Aviso : Borde);
                v.Rotulo(130, 256, "cliente y usuarios piden", 15, Tinta, 0);
            };
            d["bl-vivo"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 529);
                Pieza(v, 26, 108, 116, 48, "idea nueva", Aviso);
                FlechaNeon(v, 150, 132, 222, 132, Aviso);
                for (var i = 0; i < 4; i++) TarjetaUI(v, 235, 50 + i * 52, 140, 40, Borde);
                FlechaNeon(v, 388, 70, 450, 70, Nucleo);
                Pieza(v, 462, 46, 116, 48, "hecha", Menta); NeonCheck(v, 520, 120, 14);
                FlechaNeon(v, 388, 226, 434, 226, Coral);
                Pieza(v, 444, 202, 144, 48, "ya no hace falta", Coral, 14);
                v.Rotulo(305, 284, "nunca está terminado", 18, Tinta);
            };
            d["bl-producto"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Aviso, 0.5f, 531);
                PanelUI(v, 170, 18, 260, 264, "todo el producto", Aviso);
                for (var i = 0; i < 6; i++) TarjetaUI(v, 188, 66 + i * 34, 224, 28, i < 2 ? Nucleo : Borde);
            };
            d["bl-sprint"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Nucleo, 0.5f, 533);
                PanelUI(v, 150, 60, 270, 170, "solo este sprint");
                TarjetaUI(v, 170, 112, 230, 40, Nucleo); TarjetaUI(v, 170, 166, 230, 40, Nucleo);
                RelojDeArena(v, 500, 96, 100, Aviso, 0.55f);
            };
            d["bl-sin-orden"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Coral, 0.4f, 535);
                var xy = new[] { 60f, 40, 250, 70, 130, 150, 330, 180, 90, 220, 400, 50, 440, 130 };
                for (var i = 0; i < 7; i++) TarjetaUI(v, xy[i * 2], xy[i * 2 + 1], 110, 46, Borde);
                v.Halo(300, 140, 60, A(Coral, 0.3f));
                v.Rotulo(300, 140, "?", 80, Coral, 0, false, true);
            };
            d["bl-todo-urgente"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 470, 150, Coral, 0.6f, 537);
                for (var i = 0; i < 4; i++) {
                    v.Rotulo(140, 55 + i * 62, "1", 24, Coral, 0, false, true);
                    Pieza(v, 170, 30 + i * 62, 220, 50, "URGENTE", Coral, 16);
                }
                Baliza(v, 480, 170);
            };
            d["bl-infinito"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 100, TintaSuave, 0.4f, 539);
                for (var i = 0; i < 9; i++) {
                    var t = i / 8f;
                    v.Forma(L.Redondeado(210, 10 + i * 31, 180, 24, 6), A(Color.Lerp(H(0x1D3F4A), Superficie, t), 1 - t * 0.5f));
                    v.Forma(L.Redondeado(210, 10 + i * 31, 4, 24, 2), A(i < 2 ? Nucleo : TintaSuave, 1 - t * 0.7f));
                    v.Forma(L.Redondeado(224, 18 + i * 31, 110 - i * 4, 3.5f, 1.7f), A(Tinta, 0.6f - t * 0.45f));
                }
                Maraña(v, 360, 140, 140, 110, A(TintaSuave, 0.35f), 13);
                v.Rotulo(110, 240, "ideas de", 17, TintaSuave, 0, true); v.Rotulo(110, 264, "hace un año", 17, TintaSuave, 0, true);
            };
        }
    }
}
