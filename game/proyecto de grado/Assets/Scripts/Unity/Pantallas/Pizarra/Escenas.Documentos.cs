using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;
using L = Nexus.Unity.Tema.LuzUI;
using Nexus.Unity.Tema;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Los documentos de la asignatura (doc-srs-…, doc-sad-…, doc-ptp-…, doc-pmp-…): que es cada uno, que secciones
    /// lleva, por que se escribe y cuando sobra.
    /// </summary>
    public static partial class DibujosDeEscena {
        /// <summary>El documento abierto por su indice: una hoja grande con sus secciones numeradas.</summary>
        /// <summary>
        /// (2026-11-15, direccion de arte) El indice de un documento: la carpeta impresa con su pestaña del color del
        /// documento y las secciones numeradas, en una vitrina del bunker.
        /// </summary>
        private static void Indice(Tizador p, string titulo, Color c, params string[] secciones) {
            var v = p.Vitrina();
            var col = Traducir(c);
            FondoInterior(v, 300, 120, col, 0.5f, titulo.Length * 13);
            v.Forma(L.Redondeado(118, 14, 372, 284, 6), A(Silueta, 0.5f));
            v.Forma(L.Redondeado(110, 6, 372, 284, 6), H(0xEDE7D6), H(0xCFC7B0));
            v.Forma(L.Redondeado(110, 6, 10, 284, 3), col);
            v.Forma(L.Redondeado(380, -6, 90, 30, 6), col);
            v.Rotulo(425, 10, titulo, 16, H(0x10181A), 0, false, true);
            v.Rotulo(300, 40, "índice", 13, A(H(0x1E2A2A), 0.7f), 0, true);
            v.Forma(Caja(140, 54, 320, 1.4f), A(H(0x1E2A2A), 0.4f));
            var paso = Mathf.Min(38f, 228f / secciones.Length);
            for (var k = 0; k < secciones.Length; k++) {
                var y = 80 + k * paso;
                v.Rotulo(150, y, (k + 1).ToString(), 16, Color.Lerp(col, H(0x10181A), 0.35f), 0, false, true);
                v.Rotulo(172, y, secciones[k], 16, H(0x1E2A2A), -1);
            }
        }

        private static void Documentos(Dictionary<string, Action<Tizador>> d) {
            // ------------------------------------------------------------ SRS
            d["doc-srs-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 601);
                Credencial(v, 80, 80, Aviso, "cliente");
                Bocadillo(v, 20, 6, 170, 38, "«necesito que…»", Aviso, 80, 44, 14);
                FlechaNeon(v, 124, 120, 210, 120, Aviso);
                Papel(v, 230, 40, 140, 190, "SRS", H(0x1E4A5A));
                FlechaNeon(v, 386, 120, 466, 120, Nucleo);
                Credencial(v, 500, 80, Tinta); Credencial(v, 548, 108, Tinta);
                v.Rotulo(520, 206, "el equipo", 16, Tinta, 0, true);
                v.Rotulo(300, 272, "lo que el sistema debe hacer, escrito y acordado", 18, Nucleo);
            };
            d["doc-srs-secciones"] = p => Indice(p, "SRS", Tizador.Cian, "Para qué es y hasta dónde llega", "Quién lo usará y con qué límites",
                "Requisitos funcionales: lo que hace", "Requisitos de calidad: qué tan bien", "Con qué otros sistemas habla", "Glosario y criterios de aceptación");
            d["doc-srs-porque"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 100, Coral, 0.5f, 603);
                Bocadillo(v, 200, 8, 200, 36, "«un registro de visitas»", Aviso, 300, 50, 14);
                var formas = new[] { 0, 1, 2 }; var cs = new[] { Nucleo, Aviso, Coral };
                for (var i = 0; i < 3; i++) {
                    var x = 100 + i * 200;
                    Bocadillo(v, x - 44, 62, 88, 66, "", cs[i], x, 150, 14);
                    if (i == 0) v.Forma(L.Elipse(x, 92, 18, 18, 24), A(cs[i], 0.9f));
                    if (i == 1) v.Forma(L.Redondeado(x - 18, 76, 36, 32, 4), A(cs[i], 0.9f));
                    if (i == 2) v.Forma(Pts(x, 74, x + 20, 110, x - 20, 110), A(cs[i], 0.9f));
                    Credencial(v, x, 166, Tinta);
                }
                v.Rotulo(300, 252, "sin SRS, cada quien se imagina una cosa distinta", 18, Coral);
                v.Rotulo(300, 280, "y nadie lo descubre hasta la entrega", 16, Tinta);
            };
            d["doc-srs-cuando-no"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 605);
                Credencial(v, 80, 80, Nucleo, "una persona");
                for (var i = 0; i < 3; i++) TarjetaUI(v, 150, 56 + i * 54, 110, 44, Nucleo);
                NeonCheck(v, 205, 240, 14);
                v.Rotulo(170, 278, "basta una lista corta", 16, Nucleo);
                Papel(v, 416, 56, 120, 170, null, null, 4); Papel(v, 400, 40, 120, 170, "SRS");
                Sello(v, 466, 128, 44);
                v.Rotulo(468, 262, "cien páginas para tres días", 15, Coral);
            };

            // ------------------------------------------------------------ SAD
            d["doc-sad-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 210, 130, Aviso, 0.5f, 607);
                PanelUI(v, 60, 20, 300, 250, "SAD", Aviso);
                Pieza(v, 86, 72, 104, 44, "pantallas", Nucleo, 14); Pieza(v, 230, 72, 104, 44, "reglas", Nucleo, 14);
                FlechaNeon(v, 192, 94, 226, 94, Tinta); FlechaNeon(v, 270, 120, 236, 156, Tinta);
                Icono(v.En(144, 140, 0.6f), "base-de-datos", Tizador.Cian);
                v.Rotulo(210, 250, "las piezas y cómo se hablan", 15, Tinta);
                Bocadillo(v, 394, 74, 184, 60, "«y se eligió así porque…»", Aviso, 380, 130, 14);
                v.Rotulo(485, 196, "la forma del sistema", 16, Aviso, 0, true); v.Rotulo(485, 220, "y su porqué", 16, Aviso, 0, true);
            };
            d["doc-sad-secciones"] = p => Indice(p, "SAD", Tizador.Mostaza, "Contexto: qué hay alrededor", "Qué calidad se busca y qué límites hay",
                "Las piezas y cómo se conectan", "Dónde corre cada pieza", "Las decisiones y su porqué", "Riesgos y atajos conocidos");
            d["doc-sad-porque"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 430, 140, Nucleo, 0.5f, 609);
                Credencial(v, 66, 90, Nucleo, "alguien nuevo");
                Papel(v, 130, 80, 80, 100, "SAD", H(0x6A4A12), -6);
                FlechaNeon(v, 222, 130, 290, 130, Nucleo);
                v.Forma(L.Redondeado(310, 40, 250, 200, 6), H(0x24507A), H(0x173A5C));
                foreach (var t in new[] { Pts(310, 40, 400, 40, 400, 110, 310, 110), Pts(440, 40, 560, 40, 560, 120, 440, 120), Pts(310, 160, 480, 160, 480, 240, 310, 240) }) Contorno(v, t, 1.4f, A(Color.white, 0.7f));
                v.Neon(Pts(310, 130, 370, 130, 370, 80, 440, 80, 440, 180, 510, 180, 510, 130, 560, 130), 1.4f, Aviso);
                v.Rotulo(300, 274, "con el plano en la mano, no se pierde en el sistema", 18, Nucleo);
            };
            d["doc-sad-cuando-no"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 611);
                Pieza(v, 70, 80, 180, 110, "una sola pieza", Nucleo, 18);
                NeonCheck(v, 160, 222, 16);
                v.Rotulo(160, 264, "basta un dibujo en la pared", 16, Nucleo, 0, true);
                Papel(v, 400, 40, 120, 170, "SAD");
                Sello(v, 460, 125, 44);
                v.Rotulo(460, 244, "documentar lo obvio", 16, Coral, 0, true); v.Rotulo(460, 268, "es tiempo perdido", 16, Coral, 0, true);
            };

            // ------------------------------------------------------------ PTP
            d["doc-ptp-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 613);
                PanelUI(v, 90, 18, 420, 206, "PTP · plan de pruebas", Aviso);
                v.Forma(Caja(250, 60, 1.2f, 156), A(Borde, 0.9f)); v.Forma(Caja(400, 60, 1.2f, 156), A(Borde, 0.9f));
                v.Rotulo(170, 82, "qué", 14, Aviso, 0, true); v.Rotulo(325, 82, "cómo", 14, Aviso, 0, true); v.Rotulo(455, 82, "horas", 14, Aviso, 0, true);
                var que = new[] { "registro", "aviso", "informes" }; var como = new[] { "unitarias", "integración", "no se prueba" }; var horas = new[] { "6", "4", "0" };
                for (var i = 0; i < 3; i++) {
                    var c = i == 2 ? Coral : Tinta; var y = 120 + i * 36;
                    if (i == 2) v.Forma(L.Redondeado(100, y - 14, 400, 28, 6), A(Coral, 0.1f));
                    v.Rotulo(170, y, que[i], 16, c); v.Rotulo(325, y, como[i], 16, c); v.Rotulo(455, y, horas[i], 18, c);
                }
                v.Rotulo(300, 262, "también dice lo que se deja fuera, a propósito", 18, Coral);
            };
            d["doc-ptp-secciones"] = p => Indice(p, "PTP", Tizador.Roja, "Qué se prueba y qué no", "Qué tipos de prueba y con qué orden",
                "Dónde se prueba y con qué datos", "Calendario y horas", "Cuándo se da por probado", "Qué puede salir mal al probar");
            d["doc-ptp-porque"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 615);
                Reloj(v, 80, 100, 44, 4, 0, Aviso);
                v.Rotulo(80, 172, "las horas", 16, Aviso, 0, true); v.Rotulo(80, 194, "no alcanzan", 16, Aviso, 0, true);
                FlechaNeon(v, 146, 110, 206, 110, Aviso);
                PanelUI(v, 220, 24, 340, 192);
                var alt = new[] { 0.95f, 0.6f, 0.3f, 0.1f };
                for (var i = 0; i < 4; i++) { var h = 150 * alt[i]; v.Forma(L.Redondeado(250 + i * 78, 200 - h, 50, h, 6), Color.Lerp(Nucleo, Color.white, 0.2f), A(Nucleo, 0.6f)); }
                v.Rotulo(275, 232, "lo que más duele", 14, Nucleo); v.Rotulo(509, 232, "lo que menos", 14, TintaSuave);
                v.Rotulo(300, 280, "el plan las pone donde más importa, no donde es más fácil", 16, Tinta);
            };
            d["doc-ptp-cuando-no"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 617);
                Ventana(v, 60, 60, 180, 130, null, TintaSuave);
                v.Rotulo(150, 136, "prototipo", 18, TintaSuave, 0, true);
                v.Rotulo(150, 222, "se va a desechar", 16, TintaSuave, 0, true);
                PanelUI(v, 340, 46, 220, 150);
                for (var i = 0; i < 4; i++) { v.Forma(L.Redondeado(358, 68 + i * 30, 140, 4, 2), A(Nucleo, 0.7f)); NeonCheck(v, 528, 70 + i * 30, 9); }
                v.Rotulo(450, 226, "o las pruebas ya corren solas:", 15, Nucleo, 0); v.Rotulo(450, 250, "su lista es el plan", 15, Nucleo, 0);
            };

            // ------------------------------------------------------------ PMP
            d["doc-pmp-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 619);
                Papel(v, 236, 10, 128, 64, "PMP", H(0x6A4A12));
                v.Neon(Pts(300, 80, 300, 106), 1, Aviso);
                v.Neon(Pts(100, 106, 500, 106), 1, Aviso);
                foreach (var x in new[] { 100f, 300, 500 }) v.Neon(Pts(x, 106, x, 128), 1, Aviso);
                Calendario(v, 60, 136, 80, 80, Aviso); v.Rotulo(100, 240, "cuándo", 18, Tinta, 0, true);
                Credencial(v, 278, 150, Nucleo); Credencial(v, 324, 150, Nucleo); v.Rotulo(300, 240, "quién", 18, Nucleo, 0, true);
                Monedas(v, 500, 212, 6); v.Rotulo(500, 240, "con cuánto", 18, Aviso, 0, true);
                v.Rotulo(300, 280, "cómo se va a llevar el proyecto de principio a fin", 17, Tinta);
            };
            d["doc-pmp-secciones"] = p => Indice(p, "PMP", Tizador.Mostaza, "Qué se entrega y qué no", "Calendario y fechas clave",
                "Costos y recursos", "Quién hace qué y cómo se avisa", "Riesgos y qué hacer si pasan", "Cómo se aprueba un cambio");
            d["doc-pmp-porque"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 621);
                PanelUI(v, 40, 20, 540, 216);
                var x = new[] { 70f, 160, 250, 380 }; var w = new[] { 110f, 120, 150, 120 }; var n = new[] { "requisitos", "diseño", "construcción", "pruebas" };
                for (var i = 0; i < 4; i++) {
                    var c = i == 2 ? Aviso : Nucleo;
                    v.Forma(L.Redondeado(x[i], 40 + i * 42, w[i], 28, 6), Color.Lerp(c, Color.white, 0.15f), A(c, 0.6f));
                    v.Rotulo(x[i] + w[i] + 10, 54 + i * 42, n[i], 15, Tinta, -1);
                }
                for (var y = 30f; y < 226; y += 14) v.Forma(Caja(499, y, 2.4f, 8), Coral);
                v.Rotulo(500, 248, "entrega", 16, Coral, 0, true);
                v.Rotulo(260, 280, "todos saben qué toca ahora y qué viene después", 17, Tinta);
            };
            d["doc-pmp-cuando-no"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 623);
                PanelUI(v, 50, 50, 230, 160);
                v.Rotulo(108, 72, "hacer", 13, Nucleo, 0, true); v.Rotulo(222, 72, "hecho", 13, Menta, 0, true);
                v.Forma(Caja(165, 62, 1.2f, 140), A(Borde, 0.9f));
                TarjetaUI(v, 62, 92, 92, 34, Nucleo); TarjetaUI(v, 62, 136, 92, 34, Nucleo); TarjetaUI(v, 176, 92, 92, 34, Menta);
                v.Rotulo(165, 238, "cinco días y dos personas:", 16, Nucleo); v.Rotulo(165, 262, "basta un tablero", 16, Nucleo);
                Papel(v, 400, 40, 120, 170, "PMP");
                Sello(v, 460, 125, 44);
                v.Rotulo(460, 252, "más plan que proyecto", 16, Coral, 0, true);
            };
        }
    }
}
