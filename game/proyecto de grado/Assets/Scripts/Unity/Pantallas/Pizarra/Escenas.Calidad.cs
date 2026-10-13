using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>«¿A qué le dedicas tu cuidado?»: cada atributo de calidad, cuando esta (q-…-que) y cuando falta (q-…-falta).</summary>
    public static partial class DibujosDeEscena {
        private static void Calidad(Dictionary<string, Action<Tizador>> d) {
            d["q-calidad-que"] = p => {
                var nombres = new[] { "usable", "fiable", "seguro", "fácil de cambiar", "rápido", "hace lo pedido" };
                var valor = new[] { 0.9f, 0.5f, 0.7f, 0.35f, 0.6f, 0.95f };
                var xy = new float[14];
                for (var i = 0; i < 6; i++) {
                    var a = Mathf.PI / 3 * i - Mathf.PI / 2; var cx = Mathf.Cos(a); var sy = Mathf.Sin(a);
                    p.Linea(300, 150, 300 + cx * 110, 150 + sy * 110, Tizador.Gris, 2);
                    p.Texto(300 + cx * 170, 156 + sy * 128, nombres[i], 16, Tizador.Tiza, true);
                    xy[i * 2] = 300 + cx * 110 * valor[i]; xy[i * 2 + 1] = 150 + sy * 110 * valor[i];
                }
                xy[12] = xy[0]; xy[13] = xy[1];
                p.Hexagono(300, 150, 110, Tizador.Gris); p.Trazo(Tizador.Cian, xy);
            };
            d["q-calidad-falta"] = p => {
                for (var i = 0; i < 4; i++) p.Punto(90 + i * 56, 80, 22, Tizador.Mostaza);
                p.Texto(174, 136, "tus fichas", 17, Tizador.Mostaza, true); p.Flecha(310, 80, 360, 80, Tizador.Tiza);
                for (var i = 0; i < 6; i++) p.Circulo(400 + (i % 3) * 62, 56 + (i / 3) * 62, 22, Tizador.Gris, true);
                p.Texto(462, 176, "seis cosas que cuidar", 17, Tizador.Gris, true);
                p.Texto(300, 240, "no alcanza para todo: lo que dejes sin ficha es lo que falla", 18, Tizador.Roja, true);
            };

            d["q-adecuacion-que"] = p => {
                p.Caja(50, 40, 200, 200, null, Tizador.Mostaza); p.Texto(150, 30, "lo que se pidió", 17, Tizador.Mostaza, true);
                p.Navegador(350, 40, 200, 200, Tizador.Cian); p.Texto(450, 30, "lo que hace", 17, Tizador.Cian, true);
                var pedido = new[] { "registrar", "avisar", "quién sigue" };
                for (var i = 0; i < 3; i++) {
                    p.Texto(70, 96 + i * 52, pedido[i], 17, Tizador.Tiza); p.Flecha(255, 90 + i * 52, 345, 90 + i * 52, Tizador.Gris);
                    p.Caja(370, 72 + i * 52, 120, 36, null, Tizador.Cian); p.Si(520, 90 + i * 52, null, 11);
                }
                p.Texto(300, 276, "todo lo pedido, completo y con las cuentas bien", 16, Tizador.Tiza, true);
            };
            d["q-adecuacion-falta"] = p => {
                p.Navegador(60, 50, 230, 170, Tizador.Cian); p.Caja(84, 96, 180, 38, "con cita", Tizador.Cian, false, 17); p.Caja(84, 150, 180, 38, "sin cita", Tizador.Roja, true, 17); p.No(280, 168, null, 10);
                p.Figura(430, 110, Tizador.Tiza, "llega sin cita"); p.Texto(430, 60, "?", 40, Tizador.Roja, true);
                p.Texto(300, 264, "muy bonito, pero no hace lo que se necesitaba", 17, Tizador.Roja, true);
            };

            d["q-usabilidad-que"] = p => {
                p.Navegador(80, 40, 260, 190, Tizador.Cian); p.Caja(130, 110, 160, 70, "Registrar", Tizador.Mostaza, false, 24); p.Mano(250, 150);
                p.Carita(460, 110, 40, 1, Tizador.Mostaza); p.Texto(460, 180, "lo usa a la primera,", 17, Tizador.Mostaza, true); p.Texto(460, 204, "sin manual", 17, Tizador.Mostaza, true);
            };
            d["q-usabilidad-falta"] = p => {
                p.Navegador(40, 40, 250, 200, Tizador.Gris);
                for (var f = 0; f < 4; f++) for (var k = 0; k < 6; k++) p.Caja(56 + k * 38, 80 + f * 38, 30, 26, null, Tizador.Gris);
                p.Figura(350, 120, Tizador.Roja); p.Texto(350, 70, "?", 40, Tizador.Roja, true);
                for (var i = 0; i < 4; i++) p.Figura(420 + i * 44, 130, Tizador.Tiza);
                p.Texto(490, 210, "y la fila creciendo", 17, Tizador.Tiza, true); p.Texto(165, 270, "¿cuál era el botón?", 17, Tizador.Roja, true);
            };

            d["q-fiabilidad-que"] = p => {
                p.Servidor(80, 60, 110, 150, Tizador.Cian); p.Si(135, 240, null, 14);
                p.Grafica(250, 60, 290, 130, Tizador.Cian, 0.8f, 0.8f, 0.8f, 0.2f, 0.8f, 0.8f, 0.8f);
                p.Texto(395, 46, "funciona siempre", 17, Tizador.Cian, true); p.Texto(395, 224, "y si se cae, se levanta solo y rápido", 16, Tizador.Mostaza, true);
            };
            d["q-fiabilidad-falta"] = p => {
                p.Servidor(70, 60, 110, 150, Tizador.Roja); p.No(125, 135, null, 30); p.Esfera(270, 90, 40, Tizador.Roja); p.Texto(270, 160, "a las 9:00,", 17, Tizador.Roja, true); p.Texto(270, 184, "con la fila llena", 17, Tizador.Roja, true);
                p.Flecha(350, 130, 410, 130, Tizador.Tiza); p.Hoja(430, 60, 110, 150, Tizador.Tiza, "cuaderno"); p.Texto(485, 240, "vuelve el papel", 17, Tizador.Tiza, true);
            };

            d["q-seguridad-que"] = p => {
                p.Caja(250, 40, 100, 200, null, Tizador.Tiza); p.Candado(300, 120, 20, Tizador.Mostaza);
                p.Figura(110, 80, Tizador.Cian); p.Llave(150, 100, Tizador.Cian); p.Si(110, 150, null, 12); p.Texto(110, 190, "quien debe, entra", 16, Tizador.Cian, true);
                p.Figura(490, 80, Tizador.Roja); p.No(490, 150, null, 11); p.Texto(490, 190, "quien no, no", 16, Tizador.Roja, true);
                p.Texto(300, 276, "y los datos de adentro no salen", 17, Tizador.Tiza, true);
            };
            d["q-seguridad-falta"] = p => {
                p.Caja(230, 40, 100, 200, null, Tizador.Tiza); p.Caja(260, 120, 40, 34, null, Tizador.Roja); p.Curva(272, 120, 300, 70, 320, 106, Tizador.Roja, false);
                p.Figura(110, 110, Tizador.Roja, "cualquiera"); p.Flecha(150, 130, 222, 130, Tizador.Roja);
                p.Flecha(340, 130, 420, 130, Tizador.Roja); p.Hoja(440, 80, 90, 110, Tizador.Roja, "datos"); p.Texto(485, 220, "se filtran", 17, Tizador.Roja, true);
                p.Texto(300, 276, "no avisa: un día alguien ya entró", 17, Tizador.Roja, true);
            };

            d["q-mantenibilidad-que"] = p => {
                var nombres = new[] { "registro", "aviso", "lista" };
                for (var i = 0; i < 3; i++) p.Caja(60 + i * 150, 60, 120, 70, nombres[i], i == 1 ? Tizador.Mostaza : Tizador.Cian, false, 18);
                p.LlaveInglesa(232, 140, Tizador.Mostaza); p.Texto(270, 220, "se cambia una pieza", 17, Tizador.Mostaza, true); p.Texto(270, 244, "sin tocar las demás", 17, Tizador.Mostaza, true);
                p.Figura(520, 110, Tizador.Tiza, "otra persona"); p.Si(520, 60, null, 12);
            };
            d["q-mantenibilidad-falta"] = p => {
                p.Caja(60, 40, 90, 50, null, Tizador.Tiza); p.Caja(250, 30, 90, 50, null, Tizador.Tiza); p.Caja(90, 190, 90, 50, null, Tizador.Tiza); p.Caja(270, 200, 90, 50, null, Tizador.Tiza);
                p.Curva(150, 65, 300, 150, 135, 190, Tizador.Roja, false); p.Curva(295, 80, 100, 130, 315, 200, Tizador.Roja, false);
                p.Curva(105, 90, 240, 120, 180, 215, Tizador.Roja, false); p.Curva(250, 55, 200, 180, 270, 225, Tizador.Roja, false);
                p.Figura(480, 110, Tizador.Tiza); p.Texto(480, 60, "?", 40, Tizador.Roja, true); p.Texto(480, 200, "nadie se atreve", 17, Tizador.Roja, true); p.Texto(480, 224, "a tocarlo", 17, Tizador.Roja, true);
            };

            d["q-rendimiento-que"] = p => {
                p.Linea(40, 200, 560, 200, Tizador.Gris, 3); p.Caja(280, 110, 24, 90, null, Tizador.Tiza); p.Linea(296, 118, 380, 50, Tizador.Cian, 5);
                p.Coche(120, 150, Tizador.Cian); p.Flecha(230, 176, 270, 176, Tizador.Cian); p.Coche(400, 150, Tizador.Cian); p.Flecha(510, 176, 560, 176, Tizador.Cian);
                p.Esfera(110, 70, 30, Tizador.Mostaza); p.Texto(190, 76, "1 segundo", 18, Tizador.Mostaza); p.Texto(300, 250, "responde rápido, también con mucha carga", 17, Tizador.Tiza, true);
            };
            d["q-rendimiento-falta"] = p => {
                p.Linea(40, 200, 560, 200, Tizador.Gris, 3); p.Caja(470, 110, 24, 90, null, Tizador.Tiza); p.Linea(482, 124, 380, 124, Tizador.Roja, 5);
                for (var i = 0; i < 4; i++) p.Coche(30 + i * 108, 150, i == 3 ? Tizador.Roja : Tizador.Tiza);
                p.RelojDeArena(530, 60, 70, Tizador.Roja); p.Texto(400, 70, "10 segundos cada uno", 18, Tizador.Roja, true);
                p.Texto(300, 250, "lento en la hora punta es como roto", 17, Tizador.Roja, true);
            };
        }
    }
}
