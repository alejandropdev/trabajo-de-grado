using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;
using L = Nexus.Unity.Tema.LuzUI;
using Nexus.Unity.Tema;

namespace Nexus.Unity.Pantallas {
    /// <summary>Las palabras del oficio de cada nivel: que son (of-…) y donde se ven en el juego (of-…-donde).</summary>
    public static partial class DibujosDeEscena {
        // ---------------------------------------------------------------- piezas del oficio (2026-11-15, direccion de arte)

        /// <summary>Un renglon de registro en la interfaz: texto y su raya de separacion.</summary>
        private static void RenglonUI(Tizador v, float x, float y, float w, string texto, Color? c = null) {
            v.Rotulo(x + 8, y, texto, 15, c ?? Tinta, -1);
            v.Forma(Caja(x, y + 12, w, 1.2f), A(Borde, 0.9f));
        }

        /// <summary>Camara de vigilancia en su brazo, mirando abajo a la derecha, con su luz de grabacion.</summary>
        private static void CamaraVigilancia(Tizador v, float x, float y, Color luz) {
            v.Forma(L.Redondeado(x - 6, y - 20, 12, 24, 3), H(0x46555A), H(0x1A2428));
            v.Forma(Tizador.Rotar(L.Redondeado(x - 4, y - 4, 70, 30, 8), x, y + 10, 20), Cromo, CromoOscuro);
            v.Forma(Tizador.Rotar(L.Redondeado(x + 58, y, 14, 22, 4), x, y + 10, 20), H(0x14181A));
            v.Latido(v.Halo(x + 10, y + 6, 8, A(Coral, 0.8f)), 0.3f, 1, 1.2f);
            v.Haz(x + 60, y + 34, 160, 90, 52, A(luz, 0.10f));
        }

        /// <summary>Engranaje de ocho dientes.</summary>
        private static void Engranaje(Tizador v, float cx, float cy, float r, Color c) {
            var pts = new List<Vector2>();
            for (var i = 0; i < 32; i++) {
                var a = i * Mathf.PI * 2 / 32;
                var rr = (i % 4 < 2) ? r : r * 0.78f;
                pts.Add(new Vector2(cx + Mathf.Cos(a) * rr, cy + Mathf.Sin(a) * rr));
            }
            v.Forma(pts, Color.Lerp(c, Color.white, 0.2f), Color.Lerp(c, Silueta, 0.4f));
            v.Forma(L.Elipse(cx, cy, r * 0.32f, r * 0.32f, 16), H(0x14181A));
        }

        private static void Oficio(Dictionary<string, Action<Tizador>> d) {
            // Quien usa la palabra: la gente del sector y tu cliente, hablando entre ellos.
            d["of-quien-lo-usa"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 90, Ambar, 0.8f, 701);
                Escritorio(v, 120, 480, 210, Ambar);
                Credencial(v, 170, 110, Nucleo, "quien trabaja ahí"); Credencial(v, 430, 110, Aviso, "tu cliente");
                Bocadillo(v, 200, 22, 90, 42, "···", Nucleo, 190, 70, 22); Bocadillo(v, 320, 22, 90, 42, "···", Aviso, 410, 70, 22);
                v.Rotulo(300, 270, "la misma palabra, todos los días", 19, Tinta);
            };

            // ============================================================ nivel 0 · recepcion y control de visitas

            d["of-visitante"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 200, Nucleo, 0.6f, 703);
                PuertaEntreabierta(v, 266, 150, 68, 236);
                var nombres = new[] { "proveedor", "candidata", "mensajero", "cliente", "técnico", "familiar" };
                var cs = new[] { Aviso, Nucleo, Menta, Sol, CromoOscuro, Coral };
                var xs = new[] { 50f, 140, 230, 370, 460, 550 }; var ys = new[] { 120f, 72, 44, 44, 72, 120 };
                for (var i = 0; i < 6; i++) {
                    v.Neon(Pts(xs[i], ys[i] + 92, 300 + (xs[i] - 300) * 0.25f, 190), 0.7f, A(cs[i], 0.6f));
                    Credencial(v, xs[i], ys[i], cs[i]);
                    v.Rotulo(xs[i], ys[i] + 74, nombres[i], 14, Tinta);
                }
            };
            d["of-visitante-donde"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 705);
                PanelUI(v, 60, 30, 190, 210, "formulario", Nucleo);
                for (var i = 0; i < 3; i++) { v.Forma(L.Redondeado(80, 86 + i * 42, 150, 26, 6), Pozo); }
                BotonUI(v, 110, 206, 90, 26, "Guardar", Aviso, true);
                v.Rotulo(155, 270, "formulario de visita", 17, Nucleo);
                FlechaNeon(v, 262, 140, 336, 140, Tinta);
                PanelUI(v, 350, 30, 200, 210, "registro", Aviso);
                var filas = new[] { "09:02  Ana R.", "09:15  Luis M.", "09:40  Eva T.", "10:05  Raúl P." };
                for (var i = 0; i < 4; i++) RenglonUI(v, 362, 92 + i * 36, 176, filas[i]);
                v.Rotulo(450, 270, "registro", 17, Aviso);
            };

            d["of-anfitrion"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 480, 120, Nucleo, 0.5f, 707);
                Credencial(v, 110, 90, Tinta, "visitante");
                FlechaNeon(v, 150, 140, 420, 140, Aviso); v.Rotulo(290, 122, "viene a ver a", 18, Aviso);
                Credencial(v, 480, 90, Nucleo, "anfitrión");
                Bocadillo(v, 384, 8, 200, 40, "«tu visita ha llegado»", Nucleo, 480, 46, 15);
                v.Rotulo(300, 262, "el anfitrión trabaja dentro: es quien recibe el aviso", 18, Tinta);
            };
            d["of-anfitrion-donde"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 709);
                v.Rotulo(300, 50, "en el diagrama de tu propuesta", 18, Tinta);
                Pieza(v, 40, 100, 180, 70, "Aviso al anfitrión", Nucleo, 15);
                FlechaNeon(v, 224, 135, 292, 135, Tinta);
                v.Forma(L.Redondeado(300, 108, 80, 54, 4), H(0xEDE7D6), H(0xCFC7B0));
                v.Forma(Pts(300, 108, 340, 140, 380, 108), H(0xCFC7B0), H(0xB8AF95));
                FlechaNeon(v, 388, 135, 456, 135, Tinta);
                Credencial(v, 510, 96, Nucleo, "anfitrión");
                v.Rotulo(130, 200, "un módulo", 16, TintaSuave); v.Rotulo(340, 200, "un correo", 16, TintaSuave);
            };

            d["of-cita"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Nucleo, 0.5f, 711);
                Calendario(v, 40, 95, 110, 110, Aviso);
                v.Halo(95, 168, 18, A(Nucleo, 0.6f)); Anillo(v, 95, 168, 12, 2.4f, Nucleo);
                v.Rotulo(95, 232, "día y hora acordados", 15, Aviso);
                v.Neon(Tizador.Bezier(new Vector2(160, 130), new Vector2(230, 60), new Vector2(300, 70), 20), 1, Nucleo);
                v.Neon(Tizador.Bezier(new Vector2(160, 170), new Vector2(230, 240), new Vector2(300, 228), 20), 1, A(TintaSuave, 0.7f));
                Credencial(v, 335, 30, Nucleo); v.Rotulo(335, 112, "con cita", 15, Nucleo);
                FlechaNeon(v, 372, 70, 448, 70, Nucleo); NeonCheck(v, 480, 70, 14); v.Rotulo(548, 70, "lo esperan", 16, Nucleo);
                Credencial(v, 335, 190, TintaSuave); v.Rotulo(335, 274, "sin cita", 15, Tinta);
                FlechaNeon(v, 372, 230, 448, 230, Tinta); v.Rotulo(480, 230, "?", 30, Aviso, 0, false, true); v.Rotulo(548, 230, "¿qué se hace?", 16, Aviso);
            };
            d["of-cita-donde"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 713);
                PanelUI(v, 40, 30, 250, 210, "registro de visitas", Aviso);
                v.Rotulo(60, 84, "visita", 13, TintaSuave, -1, true); v.Rotulo(210, 84, "cita", 13, TintaSuave, -1, true);
                var filas = new[] { "Ana R.", "Luis M.", "Eva T." };
                for (var i = 0; i < 3; i++) { RenglonUI(v, 52, 120 + i * 40, 226, filas[i]); if (i == 1) NeonX(v, 230, 120 + i * 40, 12); else NeonCheck(v, 230, 120 + i * 40, 12); }
                Bocadillo(v, 370, 22, 200, 44, "¿y si viene sin cita?", Tinta, 420, 116, 14);
                Papel(v, 360, 124, 120, 110, "ticket", H(0x6A4A12));
                v.Rotulo(455, 266, "comentarios del ticket", 17, Nucleo);
            };

            d["of-registro"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 150, Ambar, 0.5f, 715);
                PuertaEntreabierta(v, 250, 60, 100, 236);
                Credencial(v, 90, 70, Nucleo); FlechaNeon(v, 124, 120, 232, 120, Nucleo); v.Rotulo(178, 100, "entra 09:02", 16, Nucleo);
                Credencial(v, 510, 150, Aviso); FlechaNeon(v, 382, 200, 476, 200, Aviso); v.Rotulo(430, 180, "sale 09:47", 16, Aviso);
                Reloj(v, 60, 216, 22, 9, 2, Nucleo); Reloj(v, 556, 246, 22, 9, 47, Aviso);
                v.Rotulo(300, 276, "la puerta", 16, TintaSuave, 0, true);
            };
            d["of-registro-donde"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 717);
                PanelUI(v, 30, 40, 240, 180, "backlog");
                Pieza(v, 46, 92, 208, 38, "registrar la entrada", Nucleo, 14); Pieza(v, 46, 140, 208, 38, "registrar la salida", Nucleo, 14);
                PanelUI(v, 320, 40, 250, 180, "quién sigue dentro", Aviso);
                RenglonUI(v, 332, 108, 226, "Ana R.   entró 09:02"); RenglonUI(v, 332, 146, 226, "Eva T.   entró 09:40"); RenglonUI(v, 332, 184, 226, "Raúl P.  entró 10:05");
            };

            d["of-credencial"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Nucleo, 0.6f, 719);
                v.Cable(Pts(260, 10, 288, 78), 4, 4, Aviso); v.Cable(Pts(340, 10, 312, 78), 4, 4, Aviso);
                v.Forma(L.Redondeado(222, 84, 170, 190, 10), A(Silueta, 0.5f));
                v.Forma(L.Redondeado(215, 78, 170, 190, 10), H(0xEDE7D6), H(0xCFC7B0));
                v.Forma(L.Redondeado(215, 78, 170, 46, 10), Aviso);
                v.Rotulo(300, 101, "VISITA", 20, H(0x241C02), 0, false, true);
                v.Forma(L.Redondeado(266, 138, 68, 64, 8), H(0x2E4A52), H(0x1A2E34));
                v.Forma(Caja(240, 220, 120, 3), A(H(0x1E2A2A), 0.6f)); v.Forma(Caja(260, 238, 80, 3), A(H(0x1E2A2A), 0.4f));
                NeonCheck(v, 470, 150, 22); v.Rotulo(470, 200, "autorizado", 17, Menta, 0, true);
                v.Rotulo(110, 150, "se lleva a la vista", 17, Tinta);
            };
            d["of-credencial-donde"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 90, Ambar, 0.8f, 721);
                Lampara(v, 300, 30, Ambar, 0.7f);
                Escritorio(v, 60, 540, 196, Ambar);
                Credencial(v, 300, 132, Aviso);
                Bocadillo(v, 180, 50, 240, 46, "«póngase la credencial»", Nucleo, 160, 140, 15);
                Taza(v, 120, 196, 0, 0.5f, Ambar);
                v.Rotulo(300, 236, "el mostrador de la recepción", 16, TintaSuave, 0, true);
                v.Rotulo(300, 272, "en las charlas con la gente de recepción", 17, Tinta);
            };

            d["of-aforo"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 225, 140, Nucleo, 0.5f, 723);
                PanelUI(v, 60, 40, 330, 210);
                // La fila de atras primero: asi la de delante tapa sus cordones.
                for (var f = 1; f >= 0; f--) for (var k = 0; k < 5; k++) Credencial(v, 100 + k * 62, 96 + f * 80, f == 1 && k > 2 ? CromoOscuro : Nucleo);
                v.Rotulo(490, 120, "38 / 40", 38, Aviso, 0, false, true);
                v.Rotulo(490, 166, "personas dentro", 17, Aviso);
                BarraUI(v, 420, 196, 140, 0.95f, Coral);
                v.Rotulo(490, 236, "el máximo es 40", 16, Coral);
            };
            d["of-aforo-donde"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 725);
                PanelUI(v, 150, 24, 300, 252, "quién sigue dentro", Aviso);
                for (var i = 0; i < 4; i++) {
                    v.Halo(184, 92 + i * 38, 10, A(Nucleo, 0.6f)); v.Forma(L.Elipse(184, 92 + i * 38, 5, 5, 12), NucleoClaro);
                    v.Forma(L.Redondeado(206, 90 + i * 38, 200 - i * 20, 4, 2), A(Tinta, 0.6f));
                }
                v.Forma(Caja(170, 238, 260, 1.4f), A(Aviso, 0.8f));
                v.Rotulo(300, 258, "total: 4 dentro", 18, Tinta);
            };

            d["of-mvp"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 170, 140, Nucleo, 0.6f, 727);
                v.Rotulo(170, 36, "lo mínimo que ya sirve", 18, Nucleo);
                var si = new[] { "registrar", "avisar", "quién sigue dentro" };
                for (var i = 0; i < 3; i++) { Pieza(v, 50, 60 + i * 66, 240, 50, si[i], Nucleo, 17); NeonCheck(v, 318, 85 + i * 66, 14); }
                v.Rotulo(470, 36, "después, si hace falta", 18, TintaSuave);
                var no = new[] { "informes", "colores", "app móvil" };
                for (var i = 0; i < 3; i++) { v.Forma(L.Redondeado(370, 60 + i * 66, 200, 50, 8), A(TintaSuave, 0.12f)); v.Rotulo(470, 85 + i * 66, no[i], 17, TintaSuave, 0, true); }
            };
            d["of-mvp-donde"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 729);
                Credencial(v, 110, 96, Aviso, "Marisol");
                Bocadillo(v, 150, 36, 250, 54, "«que funcione el viernes»", Aviso, 130, 96, 17);
                Calendario(v, 440, 80, 120, 120, Nucleo);
                v.Halo(530, 168, 20, A(Aviso, 0.6f)); Anillo(v, 530, 168, 14, 3, Aviso);
                v.Rotulo(500, 230, "viernes", 17, Aviso, 0, true);
            };

            // ============================================================ nivel 1 · gestion de instalaciones

            d["of-garita"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 135, 150, Ambar, 0.6f, 731);
                v.Forma(new RutaDeCurva(56, 96).Q(135, 30, 214, 96).Puntos(), H(0x46555A), H(0x1A2428));
                v.Forma(L.Redondeado(70, 92, 130, 150, 8), H(0x2B3335), H(0x101416));
                v.Forma(L.Redondeado(88, 116, 94, 56, 6), A(Ambar, 0.35f)); v.Halo(135, 144, 60, A(Ambar, 0.35f));
                Credencial(v, 135, 120, Nucleo);
                v.Rotulo(135, 262, "el guardia", 16, Nucleo, 0, true);
                CamaraVigilancia(v, 226, 90, Aviso);
                v.Rotulo(300, 70, "la cámara", 16, Aviso, 0, true);
                v.Forma(L.Redondeado(0, 238, 600, 6, 3), H(0x46555A));
                Capsula(v, 380, 238, 110, TintaSuave, false);
                v.Rotulo(450, 268, "entrada al patio", 16, TintaSuave, 0, true);
            };
            d["of-garita-donde"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 733);
                Pieza(v, 40, 40, 120, 54, "Garita", Nucleo, 16); FlechaNeon(v, 164, 67, 214, 67, Tinta); Pieza(v, 220, 40, 120, 54, "Acceso", Tinta, 16);
                v.Rotulo(190, 122, "el diagrama del sistema", 16, TintaSuave);
                v.Rotulo(470, 140, "la traza de la garita", 16, Aviso, 0, true);
                v.Forma(L.Redondeado(40, 156, 520, 116, 8), H(0x05161B), H(0x020B0E));
                v.Rotulo(60, 180, "08:41:02  garita: carro detectado", 16, A(Menta, 0.9f), -1);
                v.Rotulo(60, 208, "08:41:03  garita: lectura ABC123", 16, A(Menta, 0.9f), -1);
                v.Rotulo(60, 236, "08:41:04  garita: permiso OK", 16, Nucleo, -1);
            };

            d["of-barrera"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Nucleo, 0.5f, 735);
                v.Forma(L.Redondeado(0, 238, 600, 6, 3), H(0x46555A));
                Barrera(v, 100, 238, false, Coral);
                v.Rotulo(160, 150, "cerrada", 16, Coral, 0, true);
                Capsula(v, 140, 238, 100, TintaSuave, false);
                Barrera(v, 370, 238, true, Menta);
                v.Rotulo(510, 70, "abierta", 16, Menta, 0, true);
                Capsula(v, 420, 238, 110, Nucleo, true);
                v.Rotulo(300, 274, "obedece cualquier orden que le llegue", 17, Aviso);
            };
            d["of-barrera-donde"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 737);
                Pieza(v, 30, 110, 140, 60, "Acceso", Nucleo, 17);
                FlechaNeon(v, 174, 140, 234, 140, Tinta); v.Rotulo(204, 122, "abrir", 14, Tinta, 0, true);
                v.Forma(L.Redondeado(240, 100, 160, 80, 8), H(0x2B3335), H(0x101416));
                Engranaje(v, 270, 124, 14, Aviso);
                v.Rotulo(336, 128, "controlador", 15, Aviso, 0, true); v.Rotulo(336, 152, "de la barrera", 15, Aviso, 0, true);
                FlechaNeon(v, 404, 140, 452, 140, Tinta);
                Barrera(v, 480, 210, true, Menta);
                v.Rotulo(320, 220, "del fabricante", 16, TintaSuave, 0, true);
            };

            d["of-lpr"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 290, 130, Aviso, 0.5f, 739);
                CamaraVigilancia(v, 60, 100, Aviso);
                v.Forma(L.Redondeado(205, 100, 160, 60, 6), H(0xEDE7D6), H(0xCFC7B0));
                Contorno(v, Caja(210, 105, 150, 50), 1.6f, H(0x1E2A2A));
                v.Rotulo(285, 130, "ABC 123", 26, H(0x1E2A2A), 0, false, true);
                v.Rotulo(285, 186, "la placa del carro", 16, TintaSuave);
                FlechaNeon(v, 374, 130, 438, 130, Nucleo);
                PanelUI(v, 448, 102, 130, 56); v.Rotulo(513, 130, "\"ABC123\"", 18, Nucleo, 0, true);
                v.Rotulo(510, 186, "texto que el sistema lee", 15, Nucleo);
                v.Rotulo(300, 256, "LPR: lectura de matrículas, por sus siglas en inglés", 17, Tinta);
            };
            d["of-lpr-donde"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 741);
                PanelUI(v, 30, 50, 240, 150, "backlog");
                Pieza(v, 46, 104, 208, 44, "Leer matrículas en la garita", Nucleo, 13);
                v.Forma(L.Redondeado(320, 50, 250, 150, 8), H(0x05161B), H(0x020B0E));
                v.Rotulo(445, 74, "la traza del log", 15, Aviso, 0, true);
                v.Rotulo(336, 110, "lectura: ABC123", 16, A(Menta, 0.9f), -1);
                v.Halo(410, 140, 90, 14, A(Coral, 0.3f));
                v.Rotulo(336, 140, "lectura: ??C12?", 16, Coral, -1);
                v.Rotulo(336, 170, "lectura: XYZ789", 16, A(Menta, 0.9f), -1);
            };

            d["of-permiso"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 743);
                PanelUI(v, 120, 20, 360, 250, "pueden entrar al patio", Aviso);
                v.Rotulo(140, 84, "matrícula", 13, TintaSuave, -1, true); v.Rotulo(290, 84, "horario", 13, TintaSuave, -1, true);
                var filas = new[] { "ABC123     06:00 a 18:00", "XYZ789     todo el día", "JKL456     solo sábados" };
                for (var i = 0; i < 3; i++) { RenglonUI(v, 132, 120 + i * 40, 300, filas[i]); NeonCheck(v, 452, 120 + i * 40, 11); }
                v.Rotulo(300, 252, "quien no está en la lista, no pasa", 17, Coral);
            };
            d["of-permiso-donde"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Coral, 0.5f, 745);
                v.Rotulo(300, 46, "la traza de la garita", 17, Aviso, 0, true);
                v.Forma(L.Redondeado(60, 66, 480, 156, 8), H(0x05161B), H(0x020B0E));
                v.Rotulo(80, 104, "08:41:03  lectura MNO321", 18, A(Menta, 0.9f), -1);
                v.Halo(250, 144, 200, 18, A(Coral, 0.25f));
                v.Rotulo(80, 144, "08:41:04  permiso: DENEGADO", 18, Coral, -1);
                v.Rotulo(80, 184, "08:41:04  barrera: no abrir", 18, A(Menta, 0.9f), -1);
                NeonX(v, 500, 144, 18);
            };

            d["of-ascensor"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 150, Ambar, 0.6f, 747);
                v.Cable(Pts(300, 0, 300, 140), 2, 2, CromoOscuro);
                v.Forma(L.Redondeado(206, 24, 188, 250, 8), H(0x46555A), H(0x1A2428));
                v.Forma(L.Redondeado(220, 38, 160, 222, 4), H(0x0B1E23), H(0x05141A));
                for (var y = 46f; y < 256; y += 18) v.Forma(Caja(222, y, 156, 1.5f), A(Cromo, 0.25f));
                v.Halo(300, 190, 80, A(Ambar, 0.3f));
                v.Forma(L.Redondeado(240, 140, 120, 110, 4), H(0x2B3335), H(0x14181A));
                CajaDeCarga(v, 254, 196, 92, 50, H(0x8A6A1A));
                FlechaNeon(v, 440, 220, 440, 80, Nucleo); v.Rotulo(510, 150, "sube mercancía", 16, Nucleo, 0, true);
                v.Rotulo(100, 140, "solo cabe", 17, Tinta, 0, true); v.Rotulo(100, 164, "un envío a la vez", 17, Tinta, 0, true);
            };
            d["of-ascensor-donde"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 749);
                Pieza(v, 40, 100, 180, 70, "Reservas", Nucleo, 20); v.Rotulo(130, 200, "el módulo", 16, TintaSuave);
                PanelUI(v, 300, 40, 260, 210, "backlog", Aviso);
                Pieza(v, 316, 88, 228, 40, "reservar el ascensor", Aviso, 14);
                Pieza(v, 316, 136, 228, 40, "ver las reservas del día", Aviso, 14);
                Pieza(v, 316, 184, 228, 40, "cancelar una reserva", Aviso, 14);
            };

            d["of-franja"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 240, 120, Nucleo, 0.5f, 751);
                v.Forma(L.Redondeado(40, 166, 520, 8, 4), Pozo);
                var horas = new[] { "09:30", "10:00", "10:30", "11:00", "11:30" };
                for (var i = 0; i < 5; i++) { v.Forma(Caja(59 + i * 120, 158, 2, 24), A(Tinta, 0.6f)); v.Rotulo(60 + i * 120, 202, horas[i], 15, TintaSuave); }
                v.Halo(240, 140, 90, 40, A(Nucleo, 0.35f));
                v.Forma(L.Redondeado(180, 110, 120, 50, 8), Color.Lerp(Nucleo, Color.white, 0.2f), A(Nucleo, 0.65f));
                v.Rotulo(240, 135, "tu franja", 16, H(0x03141A), 0, true);
                Credencial(v, 240, 26, Nucleo);
                v.Rotulo(440, 120, "media hora con el", 16, Tinta); v.Rotulo(440, 144, "ascensor para ti", 16, Tinta);
            };
            d["of-franja-donde"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 753);
                PanelUI(v, 30, 40, 260, 200, "reservas");
                RenglonUI(v, 42, 104, 236, "10:00 a 10:30  Bodega"); RenglonUI(v, 42, 144, 236, "10:30 a 11:00  Obras"); RenglonUI(v, 42, 184, 236, "11:00 a 11:30  libre", TintaSuave);
                PanelUI(v, 330, 40, 240, 200, "sus pruebas", Aviso);
                var pruebas = new[] { "franja libre", "franja ocupada", "franja pasada" };
                for (var i = 0; i < 3; i++) { v.Rotulo(350, 108 + i * 40, pruebas[i], 16, Tinta, -1); NeonCheck(v, 540, 108 + i * 40, 11); }
            };

            d["of-choque"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Coral, 0.5f, 755);
                v.Forma(L.Redondeado(40, 196, 520, 8, 4), Pozo);
                var horas = new[] { "10:00", "10:15", "10:30", "10:45" };
                for (var i = 0; i < 4; i++) { v.Forma(Caja(89 + i * 140, 188, 2, 24), A(Tinta, 0.6f)); v.Rotulo(90 + i * 140, 232, horas[i], 15, TintaSuave); }
                v.Forma(L.Redondeado(90, 70, 280, 46, 8), Color.Lerp(Nucleo, Color.white, 0.1f), A(Nucleo, 0.6f)); v.Rotulo(160, 93, "Bodega", 15, H(0x03141A), 0, true); v.Rotulo(160, 110, "10:00 a 10:30", 11, H(0x03141A));
                v.Forma(L.Redondeado(230, 128, 280, 46, 8), Color.Lerp(Aviso, Color.white, 0.1f), A(Aviso, 0.6f)); v.Rotulo(440, 145, "Obras", 15, H(0x241C02), 0, true); v.Rotulo(440, 162, "10:15 a 10:45", 11, H(0x241C02));
                v.Latido(v.FormaDeLuz(L.Redondeado(230, 64, 140, 116, 8), A(Coral, 0.3f)), 0.4f, 1, 1f);
                Contorno(v, Caja(230, 64, 140, 116), 2, Coral, true);
                v.Rotulo(300, 46, "se pisan", 18, Coral, 0, true);
                v.Rotulo(300, 276, "una regla escrita decide cuál gana", 17, Tinta);
            };
            d["of-choque-donde"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 757);
                v.Rotulo(300, 22, "el calendario de turnos", 17, Aviso, 0, true);
                PanelUI(v, 120, 40, 360, 210);
                for (var i = 1; i < 4; i++) v.Forma(Caja(120 + i * 90, 46, 1.2f, 198), A(Borde, 0.9f));
                for (var i = 1; i < 4; i++) v.Forma(Caja(126, 40 + i * 52, 348, 1.2f), A(Borde, 0.9f));
                foreach (var c in new[] { new Vector2(216, 98), new Vector2(306, 150), new Vector2(396, 46) }) v.Forma(L.Redondeado(c.x, c.y, 78, 40, 6), Color.Lerp(Nucleo, Color.white, 0.1f), A(Nucleo, 0.6f));
                v.Forma(L.Redondeado(126, 150, 78, 40, 6), A(Coral, 0.85f));
                v.Forma(L.Redondeado(140, 162, 78, 40, 6), A(Aviso, 0.85f));
                Sello(v, 179, 182, 22, H(0x2A0B06));
            };

            d["of-hora-punta"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 180, 120, Aviso, 0.5f, 759);
                PanelUI(v, 30, 22, 300, 200);
                var alt = new[] { 0.15f, 0.3f, 0.95f, 0.5f, 0.25f, 0.35f, 0.2f };
                for (var i = 0; i < 7; i++) { var h = 150 * alt[i]; var c = i == 2 ? Aviso : Nucleo; v.Forma(L.Redondeado(48 + i * 40, 206 - h, 28, h, 5), Color.Lerp(c, Color.white, 0.2f), A(c, 0.6f)); }
                v.Rotulo(146, 40, "el pico", 15, Aviso, 0, true);
                v.Rotulo(180, 244, "carros por hora, a lo largo del día", 15, TintaSuave);
                v.Forma(L.Redondeado(340, 170, 260, 6, 3), H(0x46555A));
                for (var i = 0; i < 3; i++) Capsula(v, 350 + i * 82, 170, 74, i == 0 ? Aviso : TintaSuave, false);
                v.Rotulo(460, 100, "hasta 40 esperando", 17, Aviso, 0, true); v.Rotulo(460, 206, "en la garita", 16, TintaSuave, 0, true);
            };
            d["of-hora-punta-donde"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 400, 120, Aviso, 0.5f, 761);
                Papel(v, 60, 40, 130, 180, "el encargo", H(0x1E4A5A));
                v.Rotulo(125, 250, "lo pide el cliente", 16, Nucleo, 0, true);
                FlechaNeon(v, 206, 130, 290, 130, Tinta);
                v.Forma(L.Redondeado(310, 60, 110, 56, 8), A(TintaSuave, 0.12f)); v.Rotulo(365, 88, "A", 20, TintaSuave, 0, false, true);
                v.Halo(505, 88, 70, 40, A(Aviso, 0.3f)); Pieza(v, 450, 60, 110, 56, "B", Aviso, 22);
                v.Forma(L.Redondeado(380, 150, 110, 56, 8), A(TintaSuave, 0.12f)); v.Rotulo(435, 178, "C", 20, TintaSuave, 0, false, true);
                v.Rotulo(435, 250, "pesa al elegir la arquitectura", 16, Aviso, 0, true);
            };

            d["of-controlador"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Coral, 0.4f, 763);
                v.Forma(L.Redondeado(200, 60, 200, 170, 10), H(0x46555A), H(0x161D20));
                v.Forma(L.Redondeado(212, 72, 176, 146, 6), H(0x2B3335), H(0x101416));
                Icono(v.En(220, 64, 0.8f), "candado", Tizador.Roja);
                v.Rotulo(300, 202, "su código no es tuyo", 15, Coral, 0, true);
                v.Halo(400, 146, 20, A(Nucleo, 0.6f));
                v.Forma(L.Redondeado(388, 126, 24, 40, 4), Nucleo, A(Nucleo, 0.6f));
                FlechaNeon(v, 540, 146, 418, 146, Nucleo);
                v.Rotulo(500, 120, "solo por la puerta", 16, Nucleo, 0, true); v.Rotulo(500, 180, "que él ofrece", 16, Nucleo, 0, true);
                Engranaje(v, 100, 90, 26, Aviso);
                v.Rotulo(100, 150, "viene de fábrica", 17, Aviso, 0, true);
            };
            d["of-controlador-donde"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 765);
                Pieza(v, 40, 115, 140, 70, "tu sistema", Nucleo, 18);
                v.Halo(278, 150, 70, 60, A(Aviso, 0.25f));
                FlechaNeon(v, 184, 135, 370, 82, Aviso); FlechaNeon(v, 184, 165, 370, 218, Aviso);
                v.Forma(L.Redondeado(380, 50, 180, 60, 8), H(0x2B3335), H(0x101416)); v.Rotulo(470, 80, "barrera", 18, TintaSuave, 0, true);
                v.Forma(L.Redondeado(380, 190, 180, 60, 8), H(0x2B3335), H(0x101416)); v.Rotulo(470, 220, "ascensor", 18, TintaSuave, 0, true);
                v.Rotulo(270, 150, "estas flechas", 16, Aviso, 0, true);
            };
        }
    }
}
