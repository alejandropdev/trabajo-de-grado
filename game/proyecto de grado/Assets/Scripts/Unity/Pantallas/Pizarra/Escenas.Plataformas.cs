using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;

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
                p.Navegador(40, 60, 220, 160, Tizador.Cian, "visitas.nexus.com"); p.Caja(64, 110, 172, 34, null, Tizador.Tiza); p.Caja(64, 156, 100, 34, null, Tizador.Mostaza);
                p.Curva(270, 110, 340, 60, 400, 100, Tizador.Mostaza); p.Curva(400, 170, 340, 220, 270, 170, Tizador.Mostaza);
                p.Nube(480, 130, 70, Tizador.Tiza); p.Servidor(450, 150, 60, 80, Tizador.Cian);
                p.Texto(150, 250, "en tu equipo: solo el navegador", 16, Tizador.Cian, true); p.Texto(480, 262, "el programa vive en internet", 16, Tizador.Tiza, true);
            };
            d["web-cliente"] = p => {
                p.Figura(70, 120, Tizador.Mostaza, "el usuario"); p.Navegador(150, 30, 400, 210, Tizador.Cian, "visitas.nexus.com");
                p.Caja(180, 90, 340, 40, "Registrar visita", Tizador.Tiza, false, 18); p.Caja(180, 146, 160, 36, null, Tizador.Gris); p.Caja(360, 146, 160, 36, "Guardar", Tizador.Mostaza, false, 16);
                p.Curva(300, 12, 250, 0, 250, 30, Tizador.Mostaza); p.Texto(400, 16, "escribe la dirección y entra", 15, Tizador.Mostaza, true);
                p.Texto(350, 272, "no instala nada: abre el navegador y ya", 17, Tizador.Tiza, true);
            };
            d["web-dentro"] = p => {
                p.Navegador(20, 70, 150, 110, Tizador.Cian); p.Texto(95, 210, "navegador", 17, Tizador.Cian, true); p.Texto(95, 232, "muestra y recoge", 14, Tizador.Gris, true);
                p.Flecha(180, 110, 240, 110); p.Flecha(240, 140, 180, 140, Tizador.Gris);
                p.Servidor(250, 50, 100, 150, Tizador.Mostaza); p.Texto(300, 226, "servidor", 17, Tizador.Mostaza, true); p.Texto(300, 248, "decide y calcula", 14, Tizador.Gris, true);
                p.Flecha(360, 110, 420, 110); p.Flecha(420, 140, 360, 140, Tizador.Gris);
                p.Cilindro(430, 70, 130, 110, Tizador.Tiza); p.Texto(495, 210, "base de datos", 17, Tizador.Tiza, true); p.Texto(495, 232, "guarda", 14, Tizador.Gris, true);
                p.Texto(300, 286, "«sencilla»: el servidor es una sola pieza", 16, Tizador.Mostaza, true);
            };
            d["web-http"] = p => {
                p.Navegador(20, 80, 150, 120, Tizador.Cian); p.Servidor(450, 60, 110, 160, Tizador.Mostaza);
                p.Flecha(180, 110, 440, 110, Tizador.Cian); p.Caja(220, 60, 180, 40, "GET /visitas", Tizador.Cian, false, 18); p.Texto(310, 46, "1. petición: «dame la lista»", 15, Tizador.Cian, true);
                p.Flecha(440, 180, 180, 180, Tizador.Mostaza); p.Caja(220, 190, 180, 40, "200 OK + la página", Tizador.Mostaza, false, 16); p.Texto(310, 256, "2. respuesta: «aquí está»", 15, Tizador.Mostaza, true);
                p.Texto(300, 290, "HTTP es el idioma en que se piden y se contestan las cosas", 15, Tizador.Tiza, true);
            };
            d["web-flujo"] = p => Pasos(p, Tizador.Cian, "haces clic|en Guardar", "el navegador|manda la petición", "el servidor|revisa los datos", "la base de datos|guarda la visita",
                                        "el servidor|responde «listo»", "el navegador|pinta la pantalla");
            d["web-html"] = p => {
                p.Navegador(150, 30, 300, 240, Tizador.Tiza); p.Caja(170, 70, 260, 40, null, Tizador.Gris, true); p.Caja(170, 122, 120, 120, null, Tizador.Gris, true); p.Caja(304, 122, 126, 54, null, Tizador.Gris, true); p.Caja(304, 188, 126, 54, null, Tizador.Gris, true);
            };
            d["web-css"] = p => {
                p.Navegador(150, 30, 300, 240, Tizador.Tiza); p.Bloque(170, 70, 260, 40, Tizador.Cian); p.Bloque(170, 122, 120, 120, Tizador.Mostaza); p.Bloque(304, 122, 126, 54, Tizador.Roja); p.Bloque(304, 188, 126, 54, Tizador.Cian);
            };
            d["web-js"] = p => {
                p.Navegador(150, 30, 300, 240, Tizador.Tiza); p.Caja(210, 100, 180, 70, "Guardar", Tizador.Mostaza, false, 26); p.Mano(330, 150);
                p.Curva(400, 110, 440, 80, 420, 60, Tizador.Cian); p.Si(300, 220, null, 16);
            };
            d["web-escritorio"] = p => {
                p.Caja(150, 30, 300, 180, null, Tizador.Tiza); p.Linea(300, 210, 300, 240, Tizador.Tiza, 5); p.Linea(230, 240, 370, 240, Tizador.Tiza, 5);
                p.Caja(180, 60, 110, 80, "programa", Tizador.Cian, false, 15); p.Flecha(380, 70, 380, 150, Tizador.Mostaza); p.Linea(350, 160, 410, 160, Tizador.Mostaza, 5);
                p.Texto(300, 280, "se instala en cada computador", 18, Tizador.Tiza, true);
            };
            d["web-pwa"] = p => {
                p.Navegador(40, 60, 200, 160, Tizador.Cian); p.Flecha(255, 140, 340, 140, Tizador.Mostaza); p.Telefono(360, 20, 150, 260, Tizador.Tiza); p.App(400, 90, 70, Tizador.Cian);
                p.Texto(435, 200, "como una app", 17, Tizador.Cian, true); p.Texto(140, 256, "una web…", 18, Tizador.Cian, true);
            };

            // ------------------------------------------------------------ app movil
            d["mov-que"] = p => {
                p.Telefono(210, 10, 180, 280, Tizador.Tiza);
                for (var f = 0; f < 4; f++) for (var k = 0; k < 3; k++) p.App(232 + k * 50, 50 + f * 54, 36, f == 1 && k == 1 ? Tizador.Mostaza : Tizador.Gris);
                p.Curva(490, 96, 400, 96, 322, 120, Tizador.Mostaza); p.Texto(500, 50, "tu app, instalada", 17, Tizador.Mostaza, true); p.Texto(500, 74, "en el teléfono", 17, Tizador.Mostaza, true);
                p.Texto(100, 150, "vive en el", 17, Tizador.Tiza, true); p.Texto(100, 174, "aparato de", 17, Tizador.Tiza, true); p.Texto(100, 198, "cada usuario", 17, Tizador.Tiza, true);
            };
            d["mov-cliente"] = p => {
                p.Caja(30, 60, 150, 150, null, Tizador.Cian); p.Texto(105, 90, "Tienda", 20, Tizador.Cian, true); p.App(75, 110, 60, Tizador.Mostaza); p.Texto(105, 240, "1. la busca", 17, Tizador.Cian, true);
                p.Flecha(190, 135, 240, 135);
                p.Flecha(300, 70, 300, 170, Tizador.Mostaza); p.Linea(260, 190, 340, 190, Tizador.Mostaza, 5); p.Texto(300, 240, "2. la instala", 17, Tizador.Mostaza, true);
                p.Flecha(360, 135, 410, 135);
                p.Telefono(430, 30, 120, 190, Tizador.Tiza); p.App(462, 90, 56, Tizador.Mostaza); p.Texto(490, 250, "3. la abre", 17, Tizador.Tiza, true);
            };
            d["mov-nativa"] = p => {
                p.Hoja(60, 30, 110, 90, Tizador.Cian, "Kotlin"); p.Flecha(115, 130, 115, 160, Tizador.Cian); p.Telefono(70, 170, 90, 120, Tizador.Cian); p.Texto(210, 240, "Android", 18, Tizador.Cian, true);
                p.Hoja(380, 30, 110, 90, Tizador.Mostaza, "Swift"); p.Flecha(435, 130, 435, 160, Tizador.Mostaza); p.Telefono(390, 170, 90, 120, Tizador.Mostaza); p.Texto(530, 240, "iOS", 18, Tizador.Mostaza, true);
                p.Texto(300, 60, "dos apps,", 18, Tizador.Tiza, true); p.Texto(300, 86, "dos códigos", 18, Tizador.Tiza, true);
            };
            d["mov-multi"] = p => {
                p.Hoja(245, 10, 110, 90, Tizador.Tiza, "un código"); p.Flecha(280, 106, 170, 160, Tizador.Cian); p.Flecha(320, 106, 430, 160, Tizador.Mostaza);
                p.Telefono(110, 170, 90, 120, Tizador.Cian); p.Texto(60, 240, "Android", 17, Tizador.Cian, true); p.Telefono(400, 170, 90, 120, Tizador.Mostaza); p.Texto(530, 240, "iOS", 17, Tizador.Mostaza, true);
                p.Texto(300, 220, "se escribe", 16, Tizador.Tiza, true); p.Texto(300, 242, "una vez", 16, Tizador.Tiza, true);
            };
            d["mov-hibrida"] = p => {
                p.Telefono(200, 10, 200, 280, Tizador.Tiza); p.Navegador(222, 60, 156, 180, Tizador.Cian); p.Caja(240, 110, 120, 30, null, Tizador.Cian); p.Caja(240, 152, 120, 30, null, Tizador.Cian);
                p.Curva(490, 124, 430, 116, 384, 140, Tizador.Mostaza); p.Texto(500, 80, "por dentro", 17, Tizador.Mostaza, true); p.Texto(500, 104, "es una web", 17, Tizador.Mostaza, true);
                p.Texto(100, 130, "por fuera", 17, Tizador.Tiza, true); p.Texto(100, 154, "parece una app", 17, Tizador.Tiza, true);
            };
            d["mov-dentro"] = p => {
                p.Telefono(20, 40, 110, 200, Tizador.Cian); p.Cilindro(50, 150, 50, 50, Tizador.Gris); p.Texto(75, 270, "la app", 17, Tizador.Cian, true); p.Texto(75, 130, "guarda algo aquí", 11, Tizador.Gris, true);
                p.Flecha(140, 120, 200, 120); p.Flecha(200, 150, 140, 150, Tizador.Gris);
                p.Nube(270, 150, 56, Tizador.Tiza); p.Texto(270, 190, "internet", 16, Tizador.Tiza, true);
                p.Flecha(340, 120, 390, 120); p.Flecha(390, 150, 340, 150, Tizador.Gris);
                p.Servidor(400, 60, 70, 140, Tizador.Mostaza); p.Texto(435, 230, "servidor", 16, Tizador.Mostaza, true); p.Flecha(478, 130, 506, 130);
                p.Cilindro(512, 90, 70, 80, Tizador.Tiza); p.Texto(547, 200, "datos", 16, Tizador.Tiza, true);
            };
            d["mov-flujo"] = p => Pasos(p, Tizador.Mostaza, "tocas|un botón", "la app arma|la petición", "viaja por|internet", "el servidor|contesta", "la app pinta|el resultado");
            d["mov-camara"] = p => { p.Telefono(210, 20, 180, 260, Tizador.Tiza); p.Camara(260, 110, Tizador.Mostaza); p.Caja(250, 180, 100, 40, "ABC 123", Tizador.Cian, false, 16); };
            d["mov-avisos"] = p => {
                p.Telefono(210, 20, 180, 260, Tizador.Tiza); p.Caja(226, 60, 148, 54, null, Tizador.Mostaza); p.Texto(300, 94, "tu visita llegó", 15, Tizador.Mostaza, true);
                p.Curva(410, 60, 440, 87, 410, 114, Tizador.Roja, false); p.Curva(190, 60, 160, 87, 190, 114, Tizador.Roja, false);
            };
            d["mov-sin-conexion"] = p => {
                p.Telefono(210, 20, 180, 260, Tizador.Tiza); p.Nube(300, 120, 44, Tizador.Gris); p.No(300, 100, null, 30); p.Si(300, 200, Tizador.Cian, 22);
            };
        }
    }
}
