using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Los documentos de la asignatura (doc-srs-…, doc-sad-…, doc-ptp-…, doc-pmp-…): que es cada uno, que secciones
    /// lleva, por que se escribe y cuando sobra.
    /// </summary>
    public static partial class DibujosDeEscena {
        /// <summary>El documento abierto por su indice: una hoja grande con sus secciones numeradas.</summary>
        private static void Indice(Tizador p, string titulo, Color c, params string[] secciones) {
            p.Caja(110, 6, 380, 290, null, c); p.Linea(110, 6, 110, 296, c, 8);
            p.Texto(300, 36, titulo, 20, c, true); p.Linea(140, 46, 460, 46, Tizador.Gris, 2);
            var paso = Mathf.Min(40f, 240f / secciones.Length);
            for (var i = 0; i < secciones.Length; i++) {
                p.Texto(146, 76 + i * paso, (i + 1) + ".", 17, Tizador.Mostaza); p.Texto(176, 76 + i * paso, secciones[i], 17, Tizador.Tiza);
            }
        }

        private static void Documentos(Dictionary<string, Action<Tizador>> d) {
            // ------------------------------------------------------------ SRS
            d["doc-srs-que"] = p => {
                p.Figura(80, 100, Tizador.Mostaza, "cliente"); p.Bocadillo(30, 20, 150, 40, "«necesito que…»", Tizador.Mostaza, 14); p.Flecha(130, 120, 210, 120, Tizador.Mostaza);
                p.Hoja(230, 40, 140, 190, Tizador.Cian, "SRS"); p.Flecha(390, 120, 470, 120, Tizador.Cian);
                p.Figura(500, 90, Tizador.Tiza); p.Figura(540, 130, Tizador.Tiza); p.Texto(520, 200, "el equipo", 16, Tizador.Tiza, true);
                p.Texto(300, 268, "lo que el sistema debe hacer, escrito y acordado", 17, Tizador.Cian, true);
            };
            d["doc-srs-secciones"] = p => Indice(p, "SRS", Tizador.Cian, "Para qué es y hasta dónde llega", "Quién lo usará y con qué límites",
                "Requisitos funcionales: lo que hace", "Requisitos de calidad: qué tan bien", "Con qué otros sistemas habla", "Glosario y criterios de aceptación");
            d["doc-srs-porque"] = p => {
                p.Bocadillo(210, 10, 180, 40, "«un registro de visitas»", Tizador.Mostaza, 14);
                p.Figura(100, 130, Tizador.Tiza); p.Circulo(100, 80, 24, Tizador.Cian, true); p.Figura(300, 130, Tizador.Tiza); p.Caja(276, 58, 48, 44, null, Tizador.Mostaza, true);
                p.Figura(500, 130, Tizador.Tiza); p.Trazo(Tizador.Roja, 500, 56, 528, 102, 472, 102, 500, 56);
                p.Texto(300, 220, "sin SRS, cada quien se imagina una cosa distinta", 18, Tizador.Roja, true); p.Texto(300, 252, "y nadie lo descubre hasta la entrega", 16, Tizador.Tiza, true);
            };
            d["doc-srs-cuando-no"] = p => {
                p.Figura(90, 100, Tizador.Cian, "una persona"); p.Tarjeta(170, 60, 90, 44, Tizador.Cian); p.Tarjeta(170, 114, 90, 44, Tizador.Cian); p.Tarjeta(170, 168, 90, 44, Tizador.Cian); p.Si(215, 244, null, 12);
                p.Texto(170, 284, "basta una lista corta", 16, Tizador.Cian, true);
                p.Hoja(400, 40, 120, 170, Tizador.Gris, "SRS"); p.Hoja(416, 56, 120, 170, Tizador.Gris); p.No(470, 130, null, 34); p.Texto(470, 264, "cien páginas para tres días", 15, Tizador.Roja, true);
            };

            // ------------------------------------------------------------ SAD
            d["doc-sad-que"] = p => {
                p.Caja(60, 20, 300, 250, null, Tizador.Mostaza); p.Texto(210, 44, "SAD", 20, Tizador.Mostaza, true);
                p.Caja(90, 70, 100, 44, "pantallas", Tizador.Cian, false, 14); p.Caja(230, 70, 100, 44, "reglas", Tizador.Cian, false, 14); p.Cilindro(170, 160, 80, 60, Tizador.Cian);
                p.Flecha(192, 92, 228, 92); p.Flecha(280, 116, 240, 160); p.Texto(210, 250, "las piezas y cómo se hablan", 15, Tizador.Tiza, true);
                p.Bocadillo(400, 80, 170, 60, "«y se eligió así porque…»", Tizador.Mostaza, 14); p.Texto(485, 200, "la forma del sistema", 16, Tizador.Mostaza, true); p.Texto(485, 224, "y su porqué", 16, Tizador.Mostaza, true);
            };
            d["doc-sad-secciones"] = p => Indice(p, "SAD", Tizador.Mostaza, "Contexto: qué hay alrededor", "Qué calidad se busca y qué límites hay",
                "Las piezas y cómo se conectan", "Dónde corre cada pieza", "Las decisiones y su porqué", "Riesgos y atajos conocidos");
            d["doc-sad-porque"] = p => {
                p.Figura(70, 120, Tizador.Cian, "alguien nuevo"); p.Hoja(130, 80, 80, 100, Tizador.Mostaza, "SAD"); p.Flecha(225, 130, 290, 130, Tizador.Cian);
                p.Caja(310, 40, 250, 200, null, Tizador.Tiza);
                p.Trazo(Tizador.Cian, 310, 130, 370, 130, 370, 80, 440, 80, 440, 180, 510, 180, 510, 130, 560, 130);
                p.Linea(400, 40, 400, 110, Tizador.Gris, 3); p.Linea(480, 120, 480, 240, Tizador.Gris, 3); p.Linea(340, 180, 400, 180, Tizador.Gris, 3);
                p.Texto(300, 274, "con el plano en la mano, no se pierde en el sistema", 17, Tizador.Cian, true);
            };
            d["doc-sad-cuando-no"] = p => {
                p.Caja(70, 80, 180, 110, "una sola pieza", Tizador.Cian, false, 18); p.Si(160, 226, null, 14); p.Texto(160, 270, "basta un dibujo en la pared", 16, Tizador.Cian, true);
                p.Hoja(400, 40, 120, 170, Tizador.Gris, "SAD"); p.No(460, 125, null, 34); p.Texto(460, 250, "documentar lo obvio", 16, Tizador.Roja, true); p.Texto(460, 274, "es tiempo perdido", 16, Tizador.Roja, true);
            };

            // ------------------------------------------------------------ PTP
            d["doc-ptp-que"] = p => {
                p.Caja(90, 20, 420, 200, null, Tizador.Tiza); p.Texto(300, 46, "PTP · plan de pruebas", 18, Tizador.Mostaza, true);
                p.Linea(90, 58, 510, 58, Tizador.Gris, 2); p.Linea(250, 58, 250, 220, Tizador.Gris, 2); p.Linea(400, 58, 400, 220, Tizador.Gris, 2);
                p.Texto(170, 82, "qué", 16, Tizador.Mostaza, true); p.Texto(325, 82, "cómo", 16, Tizador.Mostaza, true); p.Texto(455, 82, "horas", 16, Tizador.Mostaza, true);
                var que = new[] { "registro", "aviso", "informes" }; var como = new[] { "unitarias", "integración", "no se prueba" }; var horas = new[] { "6", "4", "0" };
                for (var i = 0; i < 3; i++) {
                    var c = i == 2 ? Tizador.Roja : Tizador.Tiza;
                    p.Texto(170, 120 + i * 40, que[i], 16, c, true); p.Texto(325, 120 + i * 40, como[i], 16, c, true); p.Texto(455, 120 + i * 40, horas[i], 16, c, true);
                }
                p.Texto(300, 262, "también dice lo que se deja fuera, a propósito", 17, Tizador.Roja, true);
            };
            d["doc-ptp-secciones"] = p => Indice(p, "PTP", Tizador.Tiza, "Qué se prueba y qué no", "Qué tipos de prueba y con qué orden",
                "Dónde se prueba y con qué datos", "Calendario y horas", "Cuándo se da por probado", "Qué puede salir mal al probar");
            d["doc-ptp-porque"] = p => {
                p.Esfera(80, 110, 46, Tizador.Mostaza); p.Texto(80, 190, "las horas", 16, Tizador.Mostaza, true); p.Texto(80, 212, "no alcanzan", 16, Tizador.Mostaza, true);
                p.Flecha(150, 110, 210, 110, Tizador.Mostaza); p.Barras(230, 30, 320, 170, Tizador.Cian, 0.95f, 0.6f, 0.3f, 0.1f);
                p.Texto(268, 226, "lo que más", 14, Tizador.Cian, true); p.Texto(268, 244, "duele", 14, Tizador.Cian, true); p.Texto(504, 226, "lo que", 14, Tizador.Gris, true); p.Texto(504, 244, "menos", 14, Tizador.Gris, true);
                p.Texto(300, 284, "el plan las pone donde más importa, no donde es más fácil", 16, Tizador.Tiza, true);
            };
            d["doc-ptp-cuando-no"] = p => {
                p.Navegador(60, 60, 180, 130, Tizador.Gris); p.Texto(150, 136, "prototipo", 18, Tizador.Gris, true); p.Texto(150, 226, "se va a desechar", 16, Tizador.Gris, true);
                p.Caja(350, 50, 200, 150, null, Tizador.Cian);
                for (var i = 0; i < 4; i++) { p.Linea(366, 80 + i * 30, 500, 80 + i * 30, Tizador.Cian, 3); p.Si(526, 78 + i * 30, null, 8); }
                p.Texto(450, 226, "o las pruebas ya corren solas:", 15, Tizador.Cian, true); p.Texto(450, 248, "su lista es el plan", 15, Tizador.Cian, true);
            };

            // ------------------------------------------------------------ PMP
            d["doc-pmp-que"] = p => {
                p.Hoja(230, 20, 140, 60, Tizador.Mostaza, "PMP"); p.Linea(300, 80, 300, 110, Tizador.Mostaza);
                p.Linea(100, 110, 500, 110, Tizador.Mostaza); foreach (var x in new[] { 100f, 300, 500 }) p.Linea(x, 110, x, 130, Tizador.Mostaza);
                p.Calendario(60, 136, 80, 80, Tizador.Tiza); p.Texto(100, 244, "cuándo", 18, Tizador.Tiza, true);
                p.Figura(280, 150, Tizador.Cian); p.Figura(320, 150, Tizador.Cian); p.Texto(300, 244, "quién", 18, Tizador.Cian, true);
                p.Moneda(500, 176, 30, Tizador.Mostaza); p.Texto(500, 244, "con cuánto", 18, Tizador.Mostaza, true);
                p.Texto(300, 284, "cómo se va a llevar el proyecto de principio a fin", 16, Tizador.Tiza, true);
            };
            d["doc-pmp-secciones"] = p => Indice(p, "PMP", Tizador.Mostaza, "Qué se entrega y qué no", "Calendario y fechas clave",
                "Costos y recursos", "Quién hace qué y cómo se avisa", "Riesgos y qué hacer si pasan", "Cómo se aprueba un cambio");
            d["doc-pmp-porque"] = p => {
                p.Linea(60, 30, 60, 220, Tizador.Gris, 2); p.Linea(60, 220, 560, 220, Tizador.Gris, 2);
                var x = new[] { 70f, 160, 250, 380 }; var w = new[] { 110f, 120, 150, 120 }; var n = new[] { "requisitos", "diseño", "construcción", "pruebas" };
                for (var i = 0; i < 4; i++) { p.Bloque(x[i], 44 + i * 42, w[i], 28, i == 2 ? Tizador.Mostaza : Tizador.Cian); p.Texto(x[i] + w[i] + 10, 64 + i * 42, n[i], 15, Tizador.Tiza); }
                p.Linea(500, 30, 500, 220, Tizador.Roja, 3, true); p.Texto(500, 244, "entrega", 16, Tizador.Roja, true);
                p.Texto(260, 280, "todos saben qué toca ahora y qué viene después", 17, Tizador.Tiza, true);
            };
            d["doc-pmp-cuando-no"] = p => {
                p.Tablero(50, 60, 230, 150, Tizador.Cian, "hacer", "hecho"); p.Tarjeta(62, 100, 92, 34, Tizador.Cian); p.Tarjeta(62, 144, 92, 34, Tizador.Cian); p.Tarjeta(176, 100, 92, 34, Tizador.Tiza);
                p.Texto(165, 240, "cinco días y dos personas:", 16, Tizador.Cian, true); p.Texto(165, 262, "basta un tablero", 16, Tizador.Cian, true);
                p.Hoja(400, 40, 120, 170, Tizador.Gris, "PMP"); p.No(460, 125, null, 34); p.Texto(460, 250, "más plan que proyecto", 16, Tizador.Roja, true);
            };
        }
    }
}
