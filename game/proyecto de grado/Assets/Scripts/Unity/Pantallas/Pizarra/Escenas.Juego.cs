using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using UnityEngine;
using L = Nexus.Unity.Tema.LuzUI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Los sitios del propio juego (j-…): las barras del dia, el tablero, un aviso, el lanzamiento… Son los dibujos
    /// de las columnas de «Cómo lo verás en el juego», para que el jugador reconozca la pantalla de la que se habla.
    /// (2026-11-15, direccion de arte) Se dibujan con la interfaz neon en miniatura (PiezasDeEscena.Interfaz) dentro del
    /// bunker; los sitios con gente (la oficina, la charla, el jurado, el equipo) se cuentan por sus objetos.
    /// </summary>
    public static partial class DibujosDeEscena {
        private static void Juego(Dictionary<string, Action<Tizador>> d) {
            d["j-barras"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Nucleo, 0.6f, 61);
                PanelUI(v, 110, 22, 380, 252, "tu proyecto");
                var nombres = new[] { "avance", "deuda", "cobertura", "cansancio", "moral" };
                var valores = new[] { 0.6f, 0.3f, 0.5f, 0.4f, 0.7f };
                var colores = new[] { Nucleo, Aviso, Menta, Coral, Ambar };
                for (var i = 0; i < 5; i++) BarraUI(v, 250, 82 + i * 36, 210, valores[i], colores[i], nombres[i]);
            };
            d["j-horas-extra"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 150, 120, Aviso, 0.5f, 63);
                OjoDeBuey(v, 150, 118, 62, true);
                v.Rotulo(150, 226, "ya es de noche", 18, TintaSuave, 0, true);
                BotonUI(v, 370, 92, 170, 56, "Quedarte", Aviso, true);
                v.Rotulo(455, 182, "una hora más", 18, Aviso);
            };
            d["j-lanzamiento"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Menta, 0.6f, 65);
                PanelUI(v, 150, 28, 300, 236, "lanzamiento", Aviso);
                v.Halo(300, 128, 120, 40, A(Menta, 0.3f));
                v.Rotulo(300, 128, "BIEN", 52, Menta, 0, false, true);
                BarraUI(v, 190, 200, 220, 0.8f, Menta);
            };
            d["j-cliente-pide"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 470, 100, Sol, 0.5f, 67);
                Altavoz(v, 540, 100, 256, Sol);
                Bocadillo(v, 250, 28, 230, 52, "«¿y si además…?»", Sol, 500, 96, 19);
                BotonUI(v, 50, 186, 120, 52, "Sí", Nucleo);
                BotonUI(v, 190, 186, 120, 52, "No", Coral);
                BotonUI(v, 330, 186, 160, 52, "Negociar", Tinta);
            };
            d["j-tablero"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Nucleo, 0.5f, 69);
                PanelUI(v, 50, 22, 500, 256);
                var columnas = new[] { "por hacer", "en curso", "hecho" };
                for (var c = 0; c < 3; c++) {
                    var x = 50 + c * 166.7f;
                    v.Rotulo(x + 83, 48, columnas[c], 14, c == 1 ? Aviso : c == 2 ? Menta : Nucleo, 0, true);
                    if (c > 0) v.Forma(Caja(x, 36, 1.2f, 228), A(Borde, 0.8f));
                }
                for (var i = 0; i < 3; i++) TarjetaUI(v, 66, 72 + i * 56, 134, 46, Nucleo);
                TarjetaUI(v, 233, 72, 134, 46, Aviso);
                TarjetaUI(v, 400, 72, 134, 46, Menta); TarjetaUI(v, 400, 128, 134, 46, Menta);
            };
            d["j-decision"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Aviso, 0.5f, 71);
                PanelUI(v, 90, 22, 420, 256, "un aviso", Aviso);
                Renglones(v, 116, 82, 368, 4, 20);
                BotonUI(v, 116, 196, 170, 50, "Rápido", Coral);
                BotonUI(v, 314, 196, 170, 50, "Bien hecho", Nucleo, true);
            };
            d["j-incidencia"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 160, Coral, 0.5f, 73);
                Baliza(v, 300, 196);
                Chispa(v, 190, 150, 8, 0.1f); Chispa(v, 420, 128, 7, 0.6f); Chispa(v, 360, 220, 5, 0.3f);
                v.Rotulo(300, 262, "algo se rompe", 20, Coral, 0, true);
            };
            d["j-oficina"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 80, Ambar, 0.8f, 75);
                Lampara(v, 330, 46, Ambar, 0.8f);
                Escritorio(v, 60, 560, 214, Ambar);
                Monitor(v, 230, 214, 170, 104, Nucleo, A(Nucleo, 0.8f), A(Nucleo, 0.8f), A(Nucleo, 0.8f), A(Nucleo, 0.8f), A(Nucleo, 0.8f));
                for (var i = 0; i < 3; i++) v.Forma(Tizador.Rotar(Caja(360, 200 - i * 4, 90, 12), 405, 206 - i * 4, -4 + i * 5), H(0xE9E4D4), H(0xBDB59E));
                Taza(v, 500, 214, 0, 0.5f, Ambar);
                v.Rotulo(300, 266, "tu escritorio", 20, Ambar, 0, true);
            };
            d["j-reto-pruebas"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Nucleo, 0.5f, 77);
                PanelUI(v, 60, 22, 320, 244, "tipos de prueba");
                var alturas = new[] { 0.9f, 0.6f, 0.4f, 0.25f };
                for (var i = 0; i < 4; i++) {
                    var h = 160 * alturas[i];
                    v.Forma(L.Redondeado(100 + i * 70, 246 - h, 44, h, 6), Color.Lerp(Nucleo, Color.white, 0.2f), A(Nucleo, 0.6f));
                }
                Reloj(v, 490, 112, 54, 4, 0, Aviso);
                v.Rotulo(490, 196, "horas a repartir", 18, Aviso, 0, true);
                v.Rotulo(220, 286, "entre tipos de prueba", 17, Tinta);
            };
            d["j-irse-a-casa"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 430, 150, Ambar, 0.5f, 79);
                PuertaEntreabierta(v, 380, 50, 100, 236);
                Reloj(v, 170, 110, 48, 17, 0, Menta);
                v.Rotulo(170, 196, "a su hora", 20, Menta, 0, true);
            };
            d["j-charla"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 80, Ambar, 0.8f, 81);
                Escritorio(v, 110, 490, 200, Ambar);
                Taza(v, 230, 200, 0, 0.8f, Ambar);
                Taza(v, 370, 200, 2, 0.8f, Ambar);
                Bocadillo(v, 150, 46, 110, 46, "···", Nucleo, 222, 140, 26);
                Bocadillo(v, 340, 46, 110, 46, "···", Aviso, 372, 140, 26);
                v.Rotulo(300, 262, "en cada sala hay alguien", 19, Tinta);
            };
            d["j-reunion"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 90, Coral, 0.5f, 83);
                v.Forma(L.Elipse(300, 186, 200, 56, 48), CostillaCerca, PrimerPlano);
                v.Forma(L.Arco(300, 186, 200, 56, 2, 190, 350, 28), A(Ambar, 0.5f));
                for (var i = 0; i < 4; i++) Taza(v, 200 + i * 70, 176, i % 4, 0, Ambar);
                for (var i = 0; i < 3; i++) v.Forma(Tizador.Rotar(Caja(150 + i * 120, 196, 60, 10), 180 + i * 120, 201, -6 + i * 6), H(0xE9E4D4), H(0xBDB59E));
                Reloj(v, 520, 70, 34, 10, 50, Coral);
                v.Rotulo(300, 272, "una reunión más", 20, Coral, 0, true);
            };
            d["j-pronostico"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Aviso, 0.5f, 85);
                PanelUI(v, 100, 28, 400, 240, "pronóstico del lanzamiento", Aviso);
                v.Rotulo(300, 140, "saldría <color=#E3C24F>REGULAR</color>", 30, Tinta);
                BarraUI(v, 150, 204, 300, 0.55f, Aviso);
            };
            d["j-fichas"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Aviso, 0.5f, 87);
                PanelUI(v, 50, 22, 500, 256, "tus fichas", Aviso);
                var nombres = new[] { "funcional", "usabilidad", "fiabilidad" }; var fichas = new[] { 2, 1, 1 };
                for (var i = 0; i < 3; i++) {
                    var y = 98 + i * 62;
                    v.Rotulo(84, y, nombres[i], 22, Tinta, -1);
                    for (var k = 0; k < 3; k++) {
                        var x = 340 + k * 70;
                        if (k < fichas[i]) { v.Halo(x, y, 34, A(Aviso, 0.35f)); v.Forma(L.Elipse(x, y, 20, 20, 28), H(0xF2D56A), H(0xB08A22)); v.Forma(L.Elipse(x, y, 13, 13, 24), A(H(0x6E5414), 0.5f)); }
                        else Anillo(v, x, y, 19, 3, A(TintaSuave, 0.5f));
                    }
                }
            };
            d["j-jurado"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 100, Aviso, 0.6f, 89);
                for (var i = 0; i < 3; i++) Credencial(v, 170 + i * 130, 54, Aviso);
                PanelUI(v, 150, 160, 300, 74);
                Renglones(v, 172, 182, 200, 3, 14);
                Lupa(v, 420, 196, 30, Nucleo);
                v.Rotulo(300, 272, "lo prueban ellos", 19, Aviso, 0, true);
            };
            d["j-reto-detectar"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 250, 140, Nucleo, 0.5f, 91);
                FlechaNeon(v, 194, 90, 256, 90, Nucleo);
                FlechaNeon(v, 316, 124, 252, 186, Coral);
                PanelUI(v, 60, 60, 130, 60); v.Rotulo(125, 90, "A", 26, Tinta, 0, false, true);
                PanelUI(v, 260, 60, 130, 60); v.Rotulo(325, 90, "B", 26, Tinta, 0, false, true);
                v.Halo(225, 220, 90, 40, A(Coral, 0.3f));
                PanelUI(v, 160, 190, 130, 60); v.Rotulo(225, 220, "C", 26, Coral, 0, false, true);
                Chispa(v, 288, 192, 8, 0.2f);
                Lupa(v, 460, 160, 48, Aviso);
                v.Rotulo(460, 268, "¿dónde está el fallo?", 18, Aviso);
            };
            d["j-reto-ordenar"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Nucleo, 0.5f, 93);
                for (var i = 0; i < 4; i++) {
                    TarjetaUI(v, 150, 30 + i * 62, 250, 50, i == 0 ? Nucleo : Borde);
                    v.Rotulo(116, 55 + i * 62, (i + 1).ToString(), 24, Aviso, 0, false, true);
                }
                FlechaNeon(v, 460, 236, 460, 46, Aviso);
                v.Rotulo(520, 140, "primero", 19, Aviso, 0, true);
            };
            d["j-ticket"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Aviso, 0.5f, 95);
                PanelUI(v, 150, 16, 300, 268);
                v.Forma(L.Redondeado(150, 16, 300, 44, 12), Aviso, H(0xB0922E));
                v.Rotulo(300, 38, "TICKET  #1042", 18, H(0x241C02), 0, true);
                var campos = new[] { "resumen", "descripción", "restricciones" };
                for (var i = 0; i < 3; i++) {
                    v.Rotulo(172, 88 + i * 66, campos[i], 13, Aviso, -1, true);
                    Renglones(v, 172, 104 + i * 66, 256, 2, 14);
                }
            };
            d["j-eleccion"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 97);
                var op = new[] { "A", "B", "C" };
                for (var i = 0; i < 3; i++) {
                    var x = 60 + i * 170;
                    if (i == 1) { v.Halo(x + 70, 108, 120, 90, A(Aviso, 0.3f)); v.Forma(L.Redondeado(x - 3, 37, 146, 136, 15), Aviso); }
                    PanelUI(v, x, 40, 140, 130);
                    v.Rotulo(x + 70, 105, op[i], 44, i == 1 ? Aviso : A(TintaSuave, 0.7f), 0, false, true);
                }
                BotonUI(v, 150, 206, 300, 50, "porque…", Nucleo, true);
            };
            d["j-planificacion"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Nucleo, 0.5f, 99);
                PanelUI(v, 40, 34, 200, 232, "backlog");
                for (var i = 0; i < 4; i++) TarjetaUI(v, 56, 82 + i * 44, 168, 36, i < 2 ? Nucleo : Borde);
                FlechaNeon(v, 252, 120, 340, 120, Nucleo);
                PanelUI(v, 350, 60, 210, 136, "este sprint");
                v.Rotulo(455, 140, "¿cuánto cabe?", 21, Aviso);
            };
            d["j-retro"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Nucleo, 0.5f, 101);
                PanelUI(v, 40, 26, 520, 248);
                var cabezas = new[] { "bien", "mal", "probar" }; var cs = new[] { Menta, Coral, Sol };
                for (var k = 0; k < 3; k++) {
                    var x = 40 + k * 173.3f;
                    if (k > 0) v.Forma(Caja(x, 40, 1.2f, 220), A(Borde, 0.8f));
                    v.Halo(x + 34, 62, 18, A(cs[k], 0.5f));
                    v.Forma(L.Elipse(x + 34, 62, 6, 6, 14), BombillaCentro, cs[k]);
                    v.Rotulo(x + 50, 62, cabezas[k], 15, cs[k], -1, true);
                    for (var i = 0; i < 3; i++) TarjetaUI(v, x + 18, 96 + i * 54, 138, 42, cs[k], 0.6f);
                }
            };
            d["j-diagrama"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Nucleo, 0.5f, 103);
                Credencial(v, 80, 96, Aviso);
                FlechaNeon(v, 112, 128, 196, 128, Nucleo);
                PanelUI(v, 200, 98, 130, 60);
                FlechaNeon(v, 334, 116, 404, 88, Nucleo);
                FlechaNeon(v, 334, 142, 404, 180, Nucleo);
                PanelUI(v, 410, 58, 130, 56);
                for (var i = 2; i >= 0; i--) {
                    var y = 186 + i * 18;
                    v.Forma(L.Redondeado(430, y - 8, 90, 18, 4), H(0x2B4F57), H(0x11262C));
                    v.Forma(L.Elipse(475, y - 8, 45, 9, 24), H(0x3FA3B0), H(0x1E6470));
                }
                v.Rotulo(300, 276, "el diagrama de tu propuesta", 19, Tinta);
            };
            d["j-alerta"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.6f, 105);
                Campana(v, 300, 54, 62, Aviso);
                v.Rotulo(300, 266, "suena una alerta", 20, Aviso, 0, true);
            };
            d["j-graficas"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Nucleo, 0.5f, 107);
                PanelUI(v, 40, 26, 250, 210);
                var linea = new List<Vector2>();
                var ys = new[] { 1f, 0.8f, 0.7f, 0.45f, 0.3f, 0.1f };
                for (var i = 0; i < ys.Length; i++) linea.Add(new Vector2(64 + i * 40, 210 - ys[i] * 150));
                v.Neon(linea, 1.6f, Nucleo);
                PanelUI(v, 310, 26, 250, 210);
                var bs = new[] { 0.5f, 0.7f, 0.6f };
                for (var i = 0; i < 3; i++) v.Forma(L.Redondeado(346 + i * 66, 214 - bs[i] * 160, 44, bs[i] * 160, 6), Color.Lerp(Aviso, Color.white, 0.2f), A(Aviso, 0.6f));
                v.Rotulo(300, 270, "las gráficas del tablero", 19, Tinta);
            };
            d["j-equipo"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 109);
                var roles = new[] { "Interfaz", "Datos", "Pruebas" };
                for (var i = 0; i < 3; i++) {
                    PanelUI(v, 50 + i * 175, 30, 150, 236);
                    Credencial(v, 125 + i * 175, 90, Nucleo);
                    BotonUI(v, 70 + i * 175, 200, 110, 40, roles[i], Aviso);
                }
            };
            d["j-lecciones"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Ambar, 0.6f, 111);
                v.Forma(L.Redondeado(176, 26, 260, 262, 6), A(Silueta, 0.5f));
                v.Forma(L.Redondeado(170, 20, 260, 260, 6), H(0xEDE7D6), H(0xCFC7B0));
                v.Rotulo(300, 52, "Lecciones", 22, H(0x1E2A2A), 0, false, true);
                v.Forma(Caja(196, 72, 208, 1.4f), A(H(0x1E2A2A), 0.5f));
                var cs = new[] { Menta, Coral, Menta };
                for (var i = 0; i < 3; i++) {
                    var y = 112 + i * 56;
                    v.Halo(206, y, 16, A(cs[i], 0.5f));
                    v.Forma(L.Elipse(206, y, 7, 7, 14), Color.Lerp(cs[i], Color.white, 0.3f), Color.Lerp(cs[i], Silueta, 0.3f));
                    v.Forma(L.Redondeado(228, y - 8, 170, 4, 2), A(H(0x1E2A2A), 0.55f));
                    v.Forma(L.Redondeado(228, y + 4, 120, 4, 2), A(H(0x1E2A2A), 0.35f));
                }
            };
        }
    }
}
