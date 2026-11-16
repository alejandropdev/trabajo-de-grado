using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;
using L = Nexus.Unity.Tema.LuzUI;
using Nexus.Unity.Tema;

namespace Nexus.Unity.Pantallas {
    /// <summary>Donde corre el software: la aplicacion web (web-…) y la app movil (mov-…), por fuera y por dentro.</summary>
    public static partial class DibujosDeEscena {
        /// <summary>Un camino de pasos numerados: cada uno, su circulo y su rotulo debajo.</summary>
        private static void Pasos(Tizador p, Color c, params string[] pasos) {
            var n = pasos.Length; var ancho = 560f / n;
            for (var i = 0; i < n; i++) {
                var x = 20 + ancho * i + ancho / 2; var y = i % 2 == 0 ? 90 : 170;
                p.Circulo(x, y, 26, c); p.Texto(x, y + 10, (i + 1).ToString(), 26, c, true);
                var lineas = pasos[i].Split('|');
                for (var k = 0; k < lineas.Length; k++) p.Texto(x, y + (i % 2 == 0 ? -62 + k * 20 : 54 + k * 20), lineas[k], 15, Tizador.Tiza, true);
                if (i < n - 1) p.Flecha(x + 28, y + (i % 2 == 0 ? 12 : -12), x + ancho - 28, (i % 2 == 0 ? 170 : 90) + (i % 2 == 0 ? -12 : 12), Tizador.Gris);
            }
        }

        private static void Plataformas(Dictionary<string, Action<Tizador>> d) {
            // ------------------------------------------------------------ aplicacion web
            d["web-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 470, 120, Nucleo, 0.5f, 401);
                var y0 = Ventana(v, 40, 60, 220, 160, "visitas.nexus.com");
                Pieza(v, 60, y0 + 18, 180, 32, null, Tinta); BotonUI(v, 60, y0 + 66, 100, 32, "Guardar", Aviso);
                v.Neon(Tizador.Bezier(new Vector2(270, 110), new Vector2(340, 60), new Vector2(404, 96), 20), 1.1f, Aviso);
                v.Neon(Tizador.Bezier(new Vector2(404, 170), new Vector2(340, 220), new Vector2(270, 170), 20), 1.1f, Aviso);
                Antena(v, 440, 220, 140, Nucleo);
                Icono(v.En(470, 90, 0.75f), "servidor", Tizador.Tiza);
                v.Rotulo(150, 250, "en tu equipo: solo el navegador", 16, Nucleo);
                v.Rotulo(480, 262, "el programa vive en internet", 16, Tinta);
            };
            d["web-cliente"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 350, 130, Nucleo, 0.5f, 403);
                Credencial(v, 70, 96, Aviso, "el usuario");
                var y0 = Ventana(v, 150, 30, 400, 210, "visitas.nexus.com");
                v.Rotulo(350, y0 + 40, "Registrar visita", 20, Tinta);
                v.Forma(L.Redondeado(180, y0 + 76, 160, 36, 6), Pozo);
                BotonUI(v, 360, y0 + 76, 160, 36, "Guardar", Aviso, true);
                v.Rotulo(400, 16, "escribe la dirección y entra", 14, Aviso);
                v.Rotulo(350, 272, "no instala nada: abre el navegador y ya", 18, Tinta);
            };
            d["web-dentro"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 405);
                Ventana(v, 20, 70, 150, 110);
                Renglones(v, 36, 112, 118, 3, 16);
                v.Rotulo(95, 206, "navegador", 17, Nucleo, 0, true); v.Rotulo(95, 230, "muestra y recoge", 14, TintaSuave);
                FlechaNeon(v, 178, 110, 238, 110, Tinta); FlechaNeon(v, 238, 140, 178, 140, TintaSuave);
                Icono(v.En(220, 50, 1.15f), "servidor", Tizador.Tiza);
                v.Rotulo(300, 222, "servidor", 17, Aviso, 0, true); v.Rotulo(300, 246, "decide y calcula", 14, TintaSuave);
                FlechaNeon(v, 362, 110, 422, 110, Tinta); FlechaNeon(v, 422, 140, 362, 140, TintaSuave);
                Icono(v.En(416, 64, 1f), "base-de-datos", Tizador.Tiza);
                v.Rotulo(495, 206, "base de datos", 17, Tinta, 0, true); v.Rotulo(495, 230, "guarda", 14, TintaSuave);
                v.Rotulo(300, 282, "«sencilla»: el servidor es una sola pieza", 16, Aviso);
            };
            d["web-http"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Nucleo, 0.5f, 407);
                Ventana(v, 20, 80, 150, 120); Renglones(v, 36, 122, 118, 4, 16);
                Icono(v.En(420, 60, 1.25f), "servidor", Tizador.Tiza);
                FlechaNeon(v, 180, 120, 436, 120, Nucleo);
                BotonUI(v, 220, 72, 180, 36, "GET /visitas", Nucleo); v.Rotulo(310, 50, "1. petición: «dame la lista»", 15, Nucleo);
                FlechaNeon(v, 436, 176, 180, 176, Aviso);
                BotonUI(v, 210, 188, 200, 36, "200 OK + la página", Aviso); v.Rotulo(310, 244, "2. respuesta: «aquí está»", 15, Aviso);
                v.Rotulo(300, 284, "HTTP es el idioma en que se piden y se contestan las cosas", 15, Tinta);
            };
            d["web-flujo"] = p => {
                var v = p.Vitrina();
                PasosLuz(v, Nucleo, "haces clic|en Guardar", "el navegador|manda la petición", "el servidor|revisa los datos", "la base de datos|guarda la visita", "el servidor|responde «listo»", "el navegador|pinta la pantalla");
            };
            d["web-html"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Nucleo, 0.5f, 409);
                Ventana(v, 150, 30, 300, 240);
                foreach (var r in new[] { Caja(170, 70, 260, 40), Caja(170, 122, 120, 130), Caja(304, 122, 126, 60), Caja(304, 192, 126, 60) }) Contorno(v, r, 1.6f, A(TintaSuave, 0.8f));
            };
            d["web-css"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 411);
                Ventana(v, 150, 30, 300, 240);
                v.Forma(L.Redondeado(170, 70, 260, 40, 6), Nucleo, A(Nucleo, 0.6f));
                v.Forma(L.Redondeado(170, 122, 120, 130, 6), Aviso, A(Aviso, 0.6f));
                v.Forma(L.Redondeado(304, 122, 126, 60, 6), Coral, A(Coral, 0.6f));
                v.Forma(L.Redondeado(304, 192, 126, 60, 6), Menta, A(Menta, 0.6f));
            };
            d["web-js"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 413);
                Ventana(v, 150, 30, 300, 240);
                BotonUI(v, 210, 104, 180, 64, "Guardar", Aviso, true);
                v.Forma(Pts(330, 150, 330, 182, 338, 174, 346, 190, 352, 187, 344, 172, 356, 172), Tinta, TintaSuave);
                Chispa(v, 404, 96, 8, 0.2f);
                NeonCheck(v, 300, 226, 20);
            };
            d["web-escritorio"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 415);
                Monitor(v, 280, 236, 260, 190, Nucleo);
                Pieza(v, 190, 64, 110, 70, "programa", Nucleo);
                FlechaNeon(v, 384, 60, 384, 150, Aviso);
                v.Forma(L.Redondeado(350, 158, 68, 6, 3), Aviso);
                v.Rotulo(300, 280, "se instala en cada computador", 18, Tinta);
            };
            d["web-pwa"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 430, 140, Nucleo, 0.5f, 417);
                Ventana(v, 40, 60, 200, 160); Renglones(v, 58, 104, 164, 4, 18);
                FlechaNeon(v, 252, 140, 340, 140, Aviso);
                var pant = Movil(v, 360, 20, 150, 260);
                AppIcono(v, pant.center.x - 35, pant.y + 50, 70, Nucleo, true);
                v.Rotulo(435, 200, "como una app", 17, Nucleo, 0, true);
                v.Rotulo(140, 256, "una web…", 18, Nucleo);
            };

            // ------------------------------------------------------------ app movil
            d["mov-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 419);
                var pant = Movil(v, 210, 10, 180, 280);
                for (var f = 0; f < 4; f++) for (var k = 0; k < 3; k++) AppIcono(v, pant.x + 14 + k * 50, pant.y + 14 + f * 52, 36, f == 1 && k == 1 ? Aviso : CostillaMedia, f == 1 && k == 1);
                v.Neon(Tizador.Bezier(new Vector2(470, 100), new Vector2(400, 96), new Vector2(334, 112), 20), 1.1f, Aviso);
                v.Rotulo(500, 50, "tu app, instalada", 17, Aviso, 0, true); v.Rotulo(500, 74, "en el teléfono", 17, Aviso, 0, true);
                v.Rotulo(100, 150, "vive en el", 17, Tinta); v.Rotulo(100, 174, "aparato de", 17, Tinta); v.Rotulo(100, 198, "cada usuario", 17, Tinta);
            };
            d["mov-cliente"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Aviso, 0.5f, 421);
                PanelUI(v, 30, 60, 150, 150, "tienda", Nucleo);
                AppIcono(v, 75, 116, 60, Aviso, true);
                v.Rotulo(105, 240, "1. la busca", 17, Nucleo, 0, true);
                FlechaNeon(v, 188, 135, 238, 135, Tinta);
                FlechaNeon(v, 300, 70, 300, 172, Aviso);
                v.Forma(L.Redondeado(260, 182, 80, 6, 3), Aviso);
                v.Rotulo(300, 240, "2. la instala", 17, Aviso, 0, true);
                FlechaNeon(v, 362, 135, 412, 135, Tinta);
                var pant = Movil(v, 430, 30, 120, 190);
                AppIcono(v, pant.center.x - 28, pant.center.y - 28, 56, Aviso, true);
                v.Rotulo(490, 250, "3. la abre", 17, Tinta, 0, true);
            };
            d["mov-nativa"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Nucleo, 0.5f, 423);
                Papel(v, 60, 26, 110, 90, "Kotlin", H(0x1E4A5A));
                FlechaNeon(v, 115, 124, 115, 164, Nucleo);
                Movil(v, 70, 170, 90, 124, Nucleo); v.Rotulo(210, 240, "Android", 18, Nucleo, 0, true);
                Papel(v, 380, 26, 110, 90, "Swift", H(0x6A4A12));
                FlechaNeon(v, 435, 124, 435, 164, Aviso);
                Movil(v, 390, 170, 90, 124, Aviso); v.Rotulo(530, 240, "iOS", 18, Aviso, 0, true);
                v.Rotulo(300, 60, "dos apps,", 18, Tinta, 0, true); v.Rotulo(300, 86, "dos códigos", 18, Tinta, 0, true);
            };
            d["mov-multi"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 425);
                Papel(v, 245, 8, 110, 90, "un código");
                FlechaNeon(v, 280, 104, 172, 162, Nucleo); FlechaNeon(v, 320, 104, 428, 162, Aviso);
                Movil(v, 110, 170, 90, 124, Nucleo); v.Rotulo(60, 240, "Android", 17, Nucleo, 0, true);
                Movil(v, 400, 170, 90, 124, Aviso); v.Rotulo(530, 240, "iOS", 17, Aviso, 0, true);
                v.Rotulo(300, 220, "se escribe", 16, Tinta, 0, true); v.Rotulo(300, 244, "una vez", 16, Tinta, 0, true);
            };
            d["mov-hibrida"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 427);
                var pant = Movil(v, 200, 10, 200, 280);
                Ventana(v, pant.x + 8, pant.y + 20, pant.width - 16, pant.height - 40);
                Pieza(v, pant.x + 24, pant.y + 70, pant.width - 48, 30, null, Nucleo); Pieza(v, pant.x + 24, pant.y + 112, pant.width - 48, 30, null, Nucleo);
                v.Neon(Tizador.Bezier(new Vector2(480, 120), new Vector2(430, 116), new Vector2(380, 140), 16), 1.1f, Aviso);
                v.Rotulo(500, 80, "por dentro", 17, Aviso, 0, true); v.Rotulo(500, 104, "es una web", 17, Aviso, 0, true);
                v.Rotulo(100, 130, "por fuera", 17, Tinta, 0, true); v.Rotulo(100, 154, "parece una app", 17, Tinta, 0, true);
            };
            d["mov-dentro"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 429);
                var pant = Movil(v, 20, 40, 110, 200);
                Icono(v.En(pant.x + 10, pant.y + 80, 0.45f), "base-de-datos", Tizador.Gris);
                v.Rotulo(75, 270, "la app", 17, Nucleo, 0, true);
                v.Rotulo(75, pant.y + 62, "guarda algo aquí", 11, TintaSuave);
                FlechaNeon(v, 140, 120, 200, 120, Tinta); FlechaNeon(v, 200, 150, 140, 150, TintaSuave);
                Antena(v, 270, 200, 120, Nucleo); v.Rotulo(270, 222, "internet", 16, Tinta, 0, true);
                FlechaNeon(v, 330, 120, 386, 120, Tinta); FlechaNeon(v, 386, 150, 330, 150, TintaSuave);
                Icono(v.En(390, 70, 0.75f), "servidor", Tizador.Tiza); v.Rotulo(450, 230, "servidor", 16, Aviso, 0, true);
                FlechaNeon(v, 488, 130, 510, 130, Tinta);
                Icono(v.En(496, 86, 0.6f), "base-de-datos", Tizador.Tiza); v.Rotulo(545, 200, "datos", 16, Tinta, 0, true);
            };
            d["mov-flujo"] = p => {
                var v = p.Vitrina();
                PasosLuz(v, Aviso, "tocas|un botón", "la app arma|la petición", "viaja por|internet", "el servidor|contesta", "la app pinta|el resultado");
            };
            d["mov-camara"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 431);
                var pant = Movil(v, 210, 20, 180, 260);
                v.Forma(L.Elipse(300, 110, 40, 40, 32), Cromo, CromoOscuro);
                v.Forma(L.Elipse(300, 110, 28, 28, 28), H(0x0B1E23), H(0x02090C));
                v.Halo(300, 110, 30, A(Aviso, 0.4f)); v.Forma(L.Elipse(292, 102, 7, 7, 12), A(Color.white, 0.4f));
                v.Haz(300, 140, 60, 90, 0, A(Aviso, 0.25f));
                BotonUI(v, 250, 184, 100, 38, "ABC 123", Nucleo);
            };
            d["mov-avisos"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 433);
                var pant = Movil(v, 210, 20, 180, 260);
                v.Halo(300, 88, 90, 40, A(Aviso, 0.35f));
                v.Forma(L.Redondeado(pant.x + 8, 60, pant.width - 16, 54, 10), Color.Lerp(Superficie, Aviso, 0.25f), Superficie);
                v.Forma(L.Redondeado(pant.x + 8, 60, 5, 54, 2.5f), Aviso);
                v.Rotulo(306, 87, "tu visita llegó", 15, Aviso, 0, true);
                foreach (var s in new[] { -1, 1 }) for (var k = 0; k < 2; k++) v.Latido(v.Forma(L.Arco(300 + s * (110 + k * 14), 87, 12 + k * 8, 22 + k * 8, 2.5f, s < 0 ? 150 : -30, s < 0 ? 210 : 30, 12), A(Coral, 0.85f - k * 0.3f)), 0.3f, 1, 0.6f, k * 0.2f);
            };
            d["mov-sin-conexion"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Menta, 0.5f, 435);
                var pant = Movil(v, 210, 20, 180, 260);
                Antena(v, 300, 160, 90, TintaSuave);
                NeonX(v, 300, 110, 34);
                NeonCheck(v, 300, 210, 26);
            };
        }
    }
}
