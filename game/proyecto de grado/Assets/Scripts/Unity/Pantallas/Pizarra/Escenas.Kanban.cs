using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using UnityEngine;
using L = Nexus.Unity.Tema.LuzUI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El curso de Kanban (kb-…), en la misma cocina: los pedidos son tarjetas que cruzan el tablero hasta servirse.
    /// (2026-11-15, direccion de arte) La cocina del bunker (PiezasDeEscena.Cocina) con el tablero de la interfaz neon.
    /// </summary>
    public static partial class DibujosDeEscena {
        private static void Kanban(Dictionary<string, Action<Tizador>> d) {
            d["kb-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 440, 140, Nucleo, 0.5f, 901);
                var xy = new[] { 40f, 60, 90, 90, 50, 130, 110, 150, 70, 190 };
                for (var i = 0; i < 5; i++) v.Forma(Tizador.Rotar(L.Redondeado(xy[i * 2], xy[i * 2 + 1], 90, 40, 4), xy[i * 2] + 45, xy[i * 2 + 1] + 20, (i % 2 == 0 ? -8 : 6)), H(0xF2EAD2), H(0xC9BE9C));
                v.Halo(110, 150, 90, A(Coral, 0.2f));
                v.Rotulo(110, 264, "pedidos amontonados", 15, Coral);
                FlechaNeon(v, 228, 150, 292, 150, Aviso);
                float ancho; TableroUI(v, 310, 50, 260, 180, new[] { "", "", "" }, out ancho);
                TarjetaUI(v, 318, 74, 70, 30, Nucleo); TarjetaUI(v, 318, 112, 70, 30, Nucleo); TarjetaUI(v, 405, 74, 70, 30, Aviso); TarjetaUI(v, 492, 74, 70, 30, Menta);
                FlechaNeon(v, 330, 206, 550, 206, Nucleo);
                v.Rotulo(440, 264, "el trabajo, a la vista y fluyendo", 15, Nucleo);
            };
            d["kb-origen"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 903);
                v.Forma(L.Redondeado(30, 60, 150, 150, 6), H(0x2B3335), H(0x101416));
                for (var f = 0; f < 3; f++) {
                    v.Forma(Caja(30, 106 + f * 50, 150, 4), Cromo);
                    for (var k = 0; k < 4; k++) if (!(f == 1 && k == 2)) CajaDeCarga(v, 40 + k * 34, 72 + f * 50, 26, 32, H(0x3F7F88));
                }
                v.Halo(120, 138, 18, A(Aviso, 0.5f)); Contorno(v, Caja(108, 122, 24, 32), 1.4f, Aviso);
                v.Rotulo(105, 232, "el hueco avisa:", 14, Aviso); v.Rotulo(105, 252, "hay que reponer", 14, Aviso);
                FlechaNeon(v, 190, 135, 234, 135, Tinta);
                Capsula(v, 240, 160, 110, TintaSuave, false);
                v.Forma(L.Redondeado(268, 74, 64, 34, 3), H(0xF2EAD2), H(0xD9CFB0)); v.Rotulo(300, 91, "kanban", 13, H(0x1E2A2A));
                v.Rotulo(296, 190, "Toyota", 15, Tinta);
                FlechaNeon(v, 362, 135, 404, 135, Tinta);
                float ancho; TableroUI(v, 420, 60, 160, 150, new[] { "", "", "" }, out ancho);
                TarjetaUI(v, 426, 84, 44, 26, Nucleo); TarjetaUI(v, 479, 84, 44, 26, Nucleo);
                v.Rotulo(500, 236, "software", 15, Nucleo);
            };
            d["kb-diferencia"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 905);
                v.Rotulo(150, 36, "Scrum", 20, Aviso, 0, false, true);
                for (var i = 0; i < 3; i++) { v.Forma(L.Redondeado(40 + i * 80, 70, 64, 64, 6), H(0x2B3335), H(0x101416)); Plato(v, 72 + i * 80, 96, 18, Nucleo); Plato(v, 72 + i * 80, 118, 18, Nucleo); }
                v.Rotulo(150, 170, "por lotes,", 16, Tinta); v.Rotulo(150, 192, "cada dos semanas", 16, Tinta);
                v.Forma(Caja(299, 20, 2, 260), A(Tinta, 0.12f));
                v.Rotulo(450, 36, "Kanban", 20, Aviso, 0, false, true);
                Cinta(v, 340, 110, 220);
                for (var i = 0; i < 4; i++) PlatoTapado(v, 366 + i * 56, 110, 18, Nucleo);
                FlechaNeon(v, 350, 156, 550, 156, Nucleo);
                v.Rotulo(450, 196, "uno a uno, sin parar", 16, Nucleo);
            };
            d["kb-cambio"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 130, 140, Ambar, 0.6f, 907);
                PanelUI(v, 30, 90, 200, 150);
                Olla(v, 50, 220, 50, 40, 0.6f, Tinta); Gorro(v, 136, 210, Tinta, 0.8f);
                float ancho; TableroUI(v, 160, 104, 62, 44, new[] { "", "" }, out ancho, null);
                v.Rotulo(130, 266, "tu cocina de hoy + un tablero", 15, Aviso);
                v.Neon(Pts(270, 240, 320, 240, 320, 200, 370, 200, 370, 160, 420, 160, 420, 120, 470, 120), 1.4f, Nucleo);
                PanelUI(v, 480, 40, 100, 80);
                v.Halo(530, 80, 30, A(Sol, 0.45f)); v.FormaDeLuz(Estrella(530, 80, 18), A(Sol, 0.95f));
                v.Rotulo(400, 272, "se mejora de a poco", 15, Nucleo);
            };
            d["kb-servicio"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 160, Aviso, 0.6f, 909);
                Credencial(v, 300, 120, Aviso, "el comensal");
                for (var i = 0; i < 5; i++) {
                    var a = Mathf.PI * 2 / 5 * i - Mathf.PI / 2; var x = 300 + Mathf.Cos(a) * 220; var y = 150 + Mathf.Sin(a) * 104;
                    Gorro(v, x, y + 16, Tinta, 0.8f);
                    FlechaNeon(v, 300 + Mathf.Cos(a) * 180, 160 + Mathf.Sin(a) * 80, 300 + Mathf.Cos(a) * 70, 160 + Mathf.Sin(a) * 46, Nucleo);
                }
            };
            d["kb-practicas"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Nucleo, 0.5f, 911);
                var nombres = new[] { "visualizar", "limitar", "fluir", "reglas claras", "feedback", "mejorar" };
                for (var i = 0; i < 6; i++) { float x = 100 + (i % 3) * 200, y = 60 + (i / 3) * 140; v.Rotulo(x, y + 66, (i + 1) + ". " + nombres[i], 17, Aviso); }
                Ojo(v, 100, 60, 34, Nucleo);
                Vaso(v, 300, 60, 30, Nucleo);
                v.Neon(Tizador.Bezier(new Vector2(460, 44), new Vector2(500, 20), new Vector2(540, 50), 16), 1.4f, Nucleo);
                v.Neon(Tizador.Bezier(new Vector2(460, 74), new Vector2(500, 50), new Vector2(540, 80), 16), 1.4f, Nucleo);
                Papel(v, 74, 168, 52, 64);
                Bocadillo(v, 264, 176, 72, 40, "···", Nucleo, 300, 236, 22);
                v.Neon(Pts(470, 236, 494, 236, 494, 212, 518, 212, 518, 188, 542, 188), 1.4f, Nucleo);
            };
            d["kb-tablero"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Nucleo, 0.5f, 913);
                float ancho; var xs = TableroUI(v, 20, 30, 560, 240, new[] { "pedido", "preparar", "cocinar", "emplatar", "servido" }, out ancho,
                    new[] { TintaSuave, Aviso, Aviso, Aviso, Menta });
                var n = new[] { 3, 2, 2, 1, 3 };
                for (var k = 0; k < 5; k++) for (var i = 0; i < n[k]; i++) TarjetaUI(v, xs[k] + 8, 60 + i * 46, ancho - 16, 36, k == 4 ? Menta : k == 0 ? Borde : Aviso);
            };
            d["kb-tarjeta"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 915);
                v.Forma(L.Redondeado(196, 56, 220, 200, 8), A(Silueta, 0.5f));
                v.Forma(L.Redondeado(190, 50, 220, 200, 8), H(0x1D3F4A), H(0x15313C));
                v.Forma(L.Redondeado(190, 50, 220, 14, 6), Aviso);
                v.Rotulo(300, 104, "Sopa", 30, Tinta, 0, false, true);
                Credencial(v, 232, 172, Nucleo); Reloj(v, 300, 204, 18, 2, 0, Tinta);
                Icono(v.En(332, 168, 0.45f), "candado", Tizador.Roja);
                v.Neon(Tizador.Bezier(new Vector2(110, 96), new Vector2(160, 84), new Vector2(240, 100), 14), 0.8f, A(TintaSuave, 0.8f)); v.Rotulo(70, 96, "título", 16, Tinta);
                v.Neon(Tizador.Bezier(new Vector2(110, 46), new Vector2(160, 36), new Vector2(200, 56), 14), 0.8f, A(TintaSuave, 0.8f)); v.Rotulo(70, 46, "tipo", 16, Aviso);
                v.Neon(Tizador.Bezier(new Vector2(110, 222), new Vector2(160, 232), new Vector2(208, 210), 14), 0.8f, A(TintaSuave, 0.8f)); v.Rotulo(60, 222, "responsable", 16, Nucleo);
                v.Neon(Tizador.Bezier(new Vector2(490, 256), new Vector2(400, 272), new Vector2(304, 226), 14), 0.8f, A(TintaSuave, 0.8f)); v.Rotulo(530, 256, "fecha", 16, Tinta);
                v.Neon(Tizador.Bezier(new Vector2(490, 176), new Vector2(440, 166), new Vector2(392, 190), 14), 0.8f, A(TintaSuave, 0.8f)); v.Rotulo(540, 176, "bloqueo", 16, Coral);
            };
            d["kb-visualizar"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 460, 120, Ambar, 0.6f, 917);
                v.Forma(L.Redondeado(30, 50, 220, 190, 8), H(0x02090C), H(0x05141A));
                foreach (var (x, y) in new[] { (60f, 90f), (150f, 130f), (80f, 180f) }) v.Forma(L.Redondeado(x, y, 60, 36, 4), A(TintaSuave, 0.08f));
                v.Rotulo(140, 266, "a oscuras: ¿qué hay pendiente?", 14, TintaSuave);
                FlechaNeon(v, 262, 145, 326, 145, Aviso);
                Lampara(v, 460, 44, Ambar, 0.8f);
                float ancho; TableroUI(v, 350, 90, 220, 150, new[] { "", "", "" }, out ancho);
                TarjetaUI(v, 356, 114, 60, 30, Nucleo); TarjetaUI(v, 429, 114, 60, 30, Aviso); TarjetaUI(v, 502, 114, 60, 30, Menta);
                v.Rotulo(460, 266, "con el tablero, se ve todo", 14, Nucleo);
            };
            d["kb-wip"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 160, Ambar, 0.6f, 919);
                PanelUI(v, 160, 20, 280, 260, "cocinando", Tinta);
                v.Halo(404, 42, 24, A(Aviso, 0.5f)); Anillo(v, 404, 42, 18, 2.4f, Aviso); v.Rotulo(404, 42, "3", 20, Aviso, 0, false, true);
                v.Forma(L.Redondeado(180, 206, 240, 44, 6), H(0x2B3335), H(0x101416));
                for (var i = 0; i < 3; i++) { v.Halo(220 + i * 80, 222, 20, A(Coral, 0.6f)); Anillo(v, 220 + i * 80, 222, 14, 3, Coral); Olla(v, 196 + i * 80, 200, 48, 44, 0.7f, Nucleo, false); }
                Olla(v, 40, 200, 48, 44, 0, TintaSuave, false); v.Rotulo(64, 236, "espera", 16, TintaSuave);
                FlechaNeon(v, 98, 176, 150, 176, Coral); NeonX(v, 124, 150, 12);
                v.Rotulo(520, 150, "tres fogones,", 15, Aviso); v.Rotulo(520, 172, "tres ollas", 15, Aviso);
            };
            d["kb-porque-wip"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Ambar, 0.5f, 921);
                Gorro(v, 140, 66, Coral, 0.9f);
                for (var i = 0; i < 8; i++) { float x = 24 + (i % 4) * 62, y = 160 + (i / 4) * 66; Olla(v, x, y, 40, 34, i % 2 == 0 ? 0 : 0.4f, i % 2 == 0 ? Coral : Nucleo, false); if (i % 2 == 0) Chispa(v, x + 20, y - 40, 6, i * 0.1f); }
                v.Rotulo(140, 276, "ocho a la vez: la mitad se quema", 14, Coral);
                v.Forma(Caja(299, 20, 2, 250), A(Tinta, 0.12f));
                Gorro(v, 450, 66, Nucleo, 0.9f);
                Olla(v, 380, 176, 56, 46, 0.8f, Nucleo); Olla(v, 470, 176, 56, 46, 0.8f, Nucleo);
                NeonCheck(v, 408, 214, 12); NeonCheck(v, 498, 214, 12);
                v.Rotulo(450, 276, "dos a la vez: perfectas y antes", 14, Nucleo);
            };
            d["kb-pull"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Nucleo, 0.5f, 923);
                Credencial(v, 50, 40, Coral, "jefe");
                for (var i = 0; i < 3; i++) CajaDeCarga(v, 100 + i * 36, 60 + i * 30, 50, 40, Coral, (i - 1) * 8);
                Gorro(v, 230, 210, Tinta, 0.9f);
                FlechaNeon(v, 90, 86, 200, 160, Coral);
                NeonX(v, 160, 128, 30);
                v.Rotulo(140, 266, "empujar (push)", 16, Coral);
                v.Forma(Caja(299, 20, 2, 250), A(Tinta, 0.12f));
                float ancho; TableroUI(v, 330, 50, 150, 150, new[] { "", "" }, out ancho);
                TarjetaUI(v, 336, 76, 62, 34, Nucleo); TarjetaUI(v, 336, 120, 62, 34, Nucleo);
                Gorro(v, 540, 140, Nucleo, 0.9f);
                v.Neon(Tizador.Bezier(new Vector2(520, 150), new Vector2(460, 180), new Vector2(404, 120), 16), 1.2f, Nucleo);
                v.Rotulo(450, 240, "jalar (pull):", 16, Nucleo); v.Rotulo(450, 262, "tomo otra cuando termino", 14, Nucleo);
            };
            d["kb-flujo"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Nucleo, 0.5f, 925);
                v.Neon(new RutaDeCurva(30, 60).Q(150, 10, 250, 70).Q(360, 130, 420, 50).Q(500, 0, 570, 80).Puntos(), 1.2f, Coral);
                Contorno(v, L.Elipse(250, 96, 14, 14, 16), 1.2f, A(Coral, 0.7f)); Contorno(v, L.Elipse(430, 90, 12, 12, 16), 1.2f, A(Coral, 0.7f));
                foreach (var (x, y) in new[] { (60f, 30f), (110f, 44f), (340f, 76f) }) TarjetaUI(v, x, y, 40, 24, Coral);
                v.Rotulo(300, 140, "a saltos y con remolinos", 15, Coral);
                v.Forma(L.Redondeado(30, 200, 540, 50, 8), A(Nucleo, 0.08f));
                v.Neon(Pts(30, 200, 570, 200), 1, Nucleo); v.Neon(Pts(30, 250, 570, 250), 1, Nucleo);
                for (var i = 0; i < 5; i++) TarjetaUI(v, 60 + i * 104, 212, 50, 26, Nucleo);
                FlechaNeon(v, 520, 225, 566, 225, Nucleo);
                v.Rotulo(300, 282, "suave y parejo: se puede predecir", 15, Nucleo);
            };
            d["kb-cuello"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 230, 160, Coral, 0.5f, 927);
                float ancho; TableroUI(v, 20, 60, 560, 200, new[] { "preparar", "cocinar", "emplatar", "servido" }, out ancho, new[] { Aviso, Coral, Aviso, Menta });
                for (var i = 0; i < 2; i++) TarjetaUI(v, 34, 96 + i * 44, 110, 34, Borde);
                for (var i = 0; i < 4; i++) TarjetaUI(v, 164 + (i % 2) * 18, 90 + i * 38, 104, 30, Coral);
                v.Halo(230, 160, 90, 80, A(Coral, 0.25f));
                TarjetaUI(v, 304, 96, 110, 34, Aviso);
                v.Neon(Pts(150, 36, 300, 36, 330, 22, 420, 22), 1, Aviso); v.Neon(Pts(150, 280, 300, 280, 330, 292, 420, 292), 1, Aviso);
                v.Rotulo(230, 46, "se amontona antes", 14, Coral, 0, true); v.Rotulo(500, 180, "y después, vacío", 14, TintaSuave);
            };
            d["kb-bloqueo"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Coral, 0.5f, 929);
                v.Forma(L.Redondeado(220, 90, 160, 100, 8), H(0x1D3F4A), H(0x15313C)); v.Forma(L.Redondeado(220, 90, 6, 100, 3), Aviso);
                v.Rotulo(300, 160, "Pizza", 22, Tinta, 0, false, true);
                v.Halo(355, 92, 70, 26, A(Coral, 0.4f));
                v.Forma(L.Redondeado(300, 70, 110, 44, 6), Coral); v.Rotulo(355, 92, "falta queso", 15, H(0x2A0B06));
                Gorro(v, 90, 130, Tinta, 0.9f); Gorro(v, 140, 210, Tinta, 0.9f); Gorro(v, 490, 130, Tinta, 0.9f); Gorro(v, 450, 220, Nucleo, 0.9f);
                FlechaNeon(v, 120, 130, 212, 130, Nucleo); FlechaNeon(v, 470, 130, 388, 130, Nucleo);
                v.Rotulo(300, 266, "se marca a la vista y todos ayudan a destrabarlo", 15, Tinta);
            };
            d["kb-politicas"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 931);
                float ancho; TableroUI(v, 40, 30, 520, 150, new[] { "preparar", "cocinar" }, out ancho, new[] { Aviso, Aviso });
                Pieza(v, 140, 80, 110, 44, "Sopa", Aviso, 15);
                FlechaNeon(v, 256, 102, 286, 102, Aviso);
                v.Halo(300, 110, 20, 60, A(Coral, 0.4f)); v.Forma(L.Redondeado(296, 54, 8, 120, 4), Coral);
                v.Forma(L.Redondeado(176, 202, 260, 80, 4), A(Silueta, 0.45f)); v.Forma(L.Redondeado(170, 196, 260, 80, 4), H(0xEDE7D6), H(0xCFC7B0));
                v.Rotulo(300, 220, "Pasa a Cocinar si:", 16, H(0x6A4A12)); v.Rotulo(286, 250, "ingredientes listos", 16, H(0x1E2A2A)); NeonCheck(v, 404, 250, 10, H(0x2E7A5A));
            };
            d["kb-clases"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 933);
                var nombre = new[] { "Urgente", "Fecha fija", "Estándar", "Intangible" }; var ej = new[] { "«¡alergia!»", "«boda sábado»", "un pedido normal", "«limpiar campana»" };
                var c = new[] { Coral, Aviso, TintaSuave, Nucleo };
                for (var i = 0; i < 4; i++) { Pieza(v, 20 + i * 144, 40, 128, 170, null, c[i]); v.Rotulo(84 + i * 144, 236, nombre[i], 17, c[i]); v.Rotulo(84 + i * 144, 262, ej[i], 13, Tinta); }
                Baliza(v, 84, 160);
                Calendario(v, 194, 90, 68, 70, Aviso);
                TarjetaUI(v, 336, 100, 72, 50, Borde);
                Llave(v, 450, 150, 50, -40);
            };
            d["kb-carriles"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 340, 140, Nucleo, 0.5f, 935);
                PanelUI(v, 120, 30, 460, 240);
                v.Forma(Caja(126, 110, 448, 1.2f), A(Borde, 0.9f)); v.Forma(Caja(126, 190, 448, 1.2f), A(Borde, 0.9f));
                for (var y = 40f; y < 262; y += 14) { v.Forma(Caja(272, y, 1.6f, 7), A(Borde, 0.9f)); v.Forma(Caja(425, y, 1.6f, 7), A(Borde, 0.9f)); }
                var carril = new[] { "Desayunos", "Almuerzos", "Urgente" };
                for (var i = 0; i < 3; i++) v.Rotulo(60, 70 + i * 80, carril[i], 16, i == 2 ? Coral : Tinta);
                TarjetaUI(v, 140, 50, 100, 40, Nucleo); TarjetaUI(v, 300, 50, 100, 40, Nucleo); TarjetaUI(v, 140, 130, 100, 40, Aviso); TarjetaUI(v, 450, 130, 100, 40, Aviso);
                v.Halo(350, 230, 80, 26, A(Coral, 0.3f));
                TarjetaUI(v, 300, 210, 100, 40, Coral); FlechaNeon(v, 140, 230, 286, 230, Coral); FlechaNeon(v, 410, 230, 560, 230, Coral);
            };
            d["kb-metricas"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 240, 120, Nucleo, 0.5f, 937);
                v.Forma(L.Redondeado(40, 106, 400, 8, 4), Pozo);
                Credencial(v, 60, 40, Aviso); v.Rotulo(88, 74, "pide", 14, Aviso, -1, true);
                Gorro(v, 220, 90, Tinta, 0.8f); v.Rotulo(220, 20, "se empieza", 14, Tinta, 0, true);
                PlatoTapado(v, 420, 96, 22, Nucleo); v.Rotulo(420, 20, "se sirve", 14, Nucleo, 0, true);
                v.Neon(Pts(60, 150, 60, 166, 420, 166, 420, 150), 1.2f, Aviso); v.Rotulo(240, 190, "lead time: desde que se pide", 15, Aviso);
                v.Neon(Pts(220, 210, 220, 226, 420, 226, 420, 210), 1.2f, Nucleo); v.Rotulo(320, 250, "cycle time: desde que se empieza", 15, Nucleo);
                PanelUI(v, 470, 90, 110, 110);
                v.Rotulo(525, 140, "12", 34, Tinta, 0, false, true); v.Rotulo(525, 176, "por semana", 13, TintaSuave);
                v.Rotulo(525, 226, "throughput", 15, Tinta, 0, true);
            };
            d["kb-cfd"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Nucleo, 0.5f, 939);
                PanelUI(v, 50, 14, 500, 236);
                var top = Pts(60, 220, 180, 180, 300, 130, 420, 80, 540, 30);
                var mid = Pts(60, 236, 180, 216, 300, 196, 420, 176, 540, 150);
                var bot = Pts(60, 240, 180, 232, 300, 222, 420, 210, 540, 196);
                var f1 = new List<Vector2>(top); var r = new List<Vector2>(mid); r.Reverse(); f1.AddRange(r); v.Forma(f1, A(Nucleo, 0.35f));
                var f2 = new List<Vector2>(mid); r = new List<Vector2>(bot); r.Reverse(); f2.AddRange(r); v.Forma(f2, A(Aviso, 0.45f));
                var f3 = new List<Vector2>(bot); f3.Add(new Vector2(540, 244)); f3.Add(new Vector2(60, 244)); v.Forma(f3, A(Menta, 0.5f));
                v.Neon(top, 1, Nucleo); v.Neon(mid, 1, Aviso); v.Neon(bot, 1, Menta);
                v.Rotulo(150, 150, "por hacer", 15, Nucleo); v.Rotulo(470, 120, "en curso", 15, Aviso); v.Rotulo(490, 222, "hecho", 15, Menta);
                Lupa(v, 400, 140, 26, Coral);
                v.Rotulo(300, 44, "la banda que se ensancha:", 14, Coral); v.Rotulo(300, 64, "ahí está el cuello de botella", 14, Coral);
                v.Rotulo(300, 272, "el tiempo", 14, TintaSuave);
            };
            d["kb-little"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Nucleo, 0.5f, 941);
                Embudo(v, 60, 40, 160, 180, Nucleo);
                foreach (var (x, y) in new[] { (120f, 34f), (156f, 40f), (140f, 248f), (140f, 276f) }) { v.Halo(x, y, 12, A(Nucleo, 0.6f)); v.Forma(L.Elipse(x, y, 7, 7, 12), NucleoClaro); }
                v.Rotulo(140, 14, "poco a la vez", 16, Nucleo); v.Rotulo(250, 240, "sale rápido", 15, Nucleo);
                Embudo(v, 380, 40, 160, 180, Coral);
                for (var i = 0; i < 9; i++) { float x = 410 + (i % 4) * 32 + (i / 4) * 12, y = 30 + (i / 4) * 18; v.Forma(L.Elipse(x, y, 8, 8, 12), Coral); }
                v.Rotulo(460, 14, "mucho a la vez", 16, Coral); v.Rotulo(460, 250, "se atasca", 15, Coral);
                v.Rotulo(300, 130, "tiempo =", 15, Tinta); v.Rotulo(300, 152, "WIP ÷ ritmo", 15, Tinta);
            };
            d["kb-cadencias"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 943);
                Reloj(v, 300, 146, 50, 10, 10, Aviso);
                var nombre = new[] { "reabastecer", "diaria", "entrega", "servicio", "operaciones", "riesgos", "estrategia" };
                for (var i = 0; i < 7; i++) {
                    var a = Mathf.PI * 2 / 7 * i - Mathf.PI / 2; var x = 300 + Mathf.Cos(a) * 214; var y = 132 + Mathf.Sin(a) * 100;
                    v.Rotulo(x, y + 38, nombre[i], 13, Aviso);
                    switch (i) {
                        case 0: CajaDeCarga(v, x - 20, y - 18, 40, 30, H(0x3F7F88)); break;
                        case 1: Taza(v, x, y + 12, 0, 0.6f, Nucleo); break;
                        case 2: Capsula(v, x - 30, y + 12, 60, Nucleo, false); break;
                        case 3: Lupa(v, x - 4, y - 4, 14, Nucleo); break;
                        case 4: Engranaje(v, x, y, 16, Nucleo); break;
                        case 5: Manometro(v, x, y, 16, 0.7f); break;
                        default: v.FormaDeLuz(Estrella(x, y, 16), A(Sol, 0.95f)); break;
                    }
                }
            };
            d["kb-roles"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 945);
                Credencial(v, 70, 66, Aviso); Pieza(v, 40, 150, 60, 26, "pedidos", Aviso, 11);
                Credencial(v, 190, 66, Nucleo); Pieza(v, 160, 150, 60, 26, "flujo", Nucleo, 11);
                v.Rotulo(130, 216, "dos roles habituales,", 15, Tinta); v.Rotulo(130, 238, "ninguno obligatorio", 15, Tinta);
                v.Forma(Caja(289, 20, 2, 250), A(Tinta, 0.12f));
                RelojDeArena(v, 350, 40, 60, Aviso); v.Rotulo(350, 120, "Sprints", 14, Aviso);
                Vaso(v, 540, 70, 26, Nucleo); v.Rotulo(540, 120, "límite WIP", 14, Nucleo);
                FlechaNeon(v, 380, 100, 414, 140, Aviso); FlechaNeon(v, 514, 100, 478, 140, Nucleo);
                v.Forma(Pts(416, 150, 476, 150, 466, 230, 426, 230), A(NucleoClaro, 0.2f)); Contorno(v, Pts(416, 150, 476, 150, 466, 230, 426, 230), 1.4f, Cromo);
                v.Forma(Pts(422, 190, 470, 190, 466, 228, 426, 228), Color.Lerp(Aviso, Nucleo, 0.5f));
                v.Forma(L.Redondeado(420, 230, 52, 18, 4), H(0x46555A), H(0x1A2428));
                v.Rotulo(446, 272, "Scrumban", 18, Tinta, 0, false, true);
            };
            d["kb-resumen"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Nucleo, 0.5f, 947);
                float ancho; var xs = TableroUI(v, 20, 20, 380, 130, new[] { "", "", "", "" }, out ancho);
                for (var k = 0; k < 4; k++) TarjetaUI(v, xs[k] + 8, 46 + (k % 2) * 36, ancho - 16, 28, k == 3 ? Menta : Aviso);
                Cinta(v, 40, 214, 360);
                for (var i = 0; i < 5; i++) PlatoTapado(v, 70 + i * 72, 214, 16, Nucleo);
                FlechaNeon(v, 416, 226, 466, 226, Nucleo);
                Credencial(v, 530, 186, Aviso);
                var pasos = new[] { "visualiza", "limita", "fluye", "mide", "mejora" };
                for (var i = 0; i < 5; i++) v.Rotulo(500, 36 + i * 26, pasos[i], 17, Aviso);
            };
        }
    }
}
