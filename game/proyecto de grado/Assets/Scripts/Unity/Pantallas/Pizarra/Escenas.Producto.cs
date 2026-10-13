using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;

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
                p.Navegador(60, 50, 220, 180, Tizador.Cian);
                var partes = new[] { "registrar", "avisar", "quién sigue" };
                for (var i = 0; i < 3; i++) p.Caja(84, 92 + i * 44, 172, 34, partes[i], Tizador.Cian, false, 16);
                p.Flecha(300, 140, 370, 140, Tizador.Tiza); p.Figura(430, 110, Tizador.Mostaza, "lo usa de verdad"); p.Si(500, 120, null, 16);
                p.Texto(300, 270, "pequeño, pero completo: ya resuelve el problema", 17, Tizador.Tiza, true);
            };
            d["mvp-si"] = p => {
                Patineta(p, 50, 110, Tizador.Cian); p.Flecha(140, 110, 200, 110, Tizador.Gris); Bici(p, 220, 80, Tizador.Cian); p.Flecha(330, 110, 390, 110, Tizador.Gris); p.Coche(410, 86, Tizador.Cian);
                p.Carita(85, 190, 22, 0, Tizador.Mostaza); p.Carita(265, 190, 22, 1, Tizador.Mostaza); p.Carita(460, 190, 22, 1, Tizador.Mostaza);
                p.Texto(300, 260, "cada entrega ya sirve para moverse", 20, Tizador.Cian, true);
            };
            d["mvp-no"] = p => {
                p.Circulo(85, 110, 20, Tizador.Gris); p.Flecha(140, 110, 200, 110, Tizador.Gris); p.Linea(220, 120, 310, 120, Tizador.Gris, 5); p.Circulo(238, 130, 9, Tizador.Gris); p.Circulo(292, 130, 9, Tizador.Gris);
                p.Flecha(330, 110, 390, 110, Tizador.Gris); p.Coche(410, 86, Tizador.Tiza);
                p.Carita(85, 190, 22, -1, Tizador.Roja); p.Carita(265, 190, 22, -1, Tizador.Roja); p.Carita(460, 190, 22, 1, Tizador.Mostaza);
                p.Texto(300, 260, "nada sirve hasta el final", 20, Tizador.Roja, true);
            };
            d["mvp-escalera"] = p => {
                var pasos = new[] { "boceto", "mockup", "prototipo", "prototipo", "MVP", "beta", "producto" };
                var sub = new[] { "", "", "sin función", "con función", "", "", "completo" };
                for (var i = 0; i < 7; i++) {
                    var x = 20 + i * 80; var y = 250 - i * 30;
                    p.Trazo(i == 4 ? Tizador.Mostaza : Tizador.Tiza, x, y, x + 80, y, x + 80, y - 30);
                    p.Texto(x + 40, y - 12, pasos[i], 15, i == 4 ? Tizador.Mostaza : Tizador.Tiza, true);
                    if (sub[i].Length > 0) p.Texto(x + 40, y + 20, sub[i], 12, Tizador.Gris, true);
                }
                p.Flecha(40, 120, 300, 30, Tizador.Cian); p.Texto(130, 50, "cada vez más real", 17, Tizador.Cian, true);
            };
            d["mvp-boceto"] = p => {
                p.Caja(170, 20, 260, 240, null, Tizador.Tiza, true);
                p.Caja(194, 44, 212, 36, null, Tizador.Gris, true); p.Caja(194, 96, 100, 90, null, Tizador.Gris, true); p.Caja(306, 96, 100, 40, null, Tizador.Gris, true); p.Caja(306, 146, 100, 40, null, Tizador.Gris, true);
                p.Linea(194, 206, 380, 206, Tizador.Gris, 2); p.Linea(194, 226, 330, 226, Tizador.Gris, 2);
                p.Linea(470, 60, 540, 20, Tizador.Mostaza, 6); p.Trazo(Tizador.Mostaza, 470, 60, 462, 74, 478, 68); p.Texto(510, 110, "a lápiz,", 17, Tizador.Mostaza, true); p.Texto(510, 134, "en minutos", 17, Tizador.Mostaza, true);
                p.Texto(90, 140, "la idea", 17, Tizador.Tiza, true); p.Texto(90, 164, "en rayas", 17, Tizador.Tiza, true);
            };
            d["mvp-mockup"] = p => {
                p.Navegador(150, 20, 300, 240, Tizador.Cian); p.Bloque(170, 60, 260, 36, Tizador.Cian); p.Bloque(170, 108, 120, 90, Tizador.Mostaza); p.Caja(304, 108, 126, 40, null, Tizador.Tiza); p.Caja(304, 158, 126, 40, null, Tizador.Tiza);
                p.Bloque(170, 212, 110, 30, Tizador.Roja); p.Mano(490, 180); p.No(520, 150, null, 14);
                p.Texto(520, 250, "se ve, no se toca", 16, Tizador.Roja, true); p.Texto(75, 130, "una imagen", 16, Tizador.Tiza, true); p.Texto(75, 154, "con el aspecto", 16, Tizador.Tiza, true); p.Texto(75, 178, "final", 16, Tizador.Tiza, true);
            };
            d["mvp-proto-nf"] = p => {
                p.Navegador(30, 40, 150, 130, Tizador.Cian); p.Navegador(225, 40, 150, 130, Tizador.Cian); p.Navegador(420, 40, 150, 130, Tizador.Cian);
                p.Caja(110, 130, 56, 26, null, Tizador.Mostaza); p.Curva(166, 143, 200, 100, 222, 110, Tizador.Mostaza); p.Caja(305, 130, 56, 26, null, Tizador.Mostaza); p.Curva(361, 143, 395, 100, 417, 110, Tizador.Mostaza);
                p.Cilindro(250, 200, 90, 60, Tizador.Gris); p.No(295, 232, null, 22);
                p.Texto(110, 230, "se puede hacer clic,", 16, Tizador.Mostaza, true); p.Texto(110, 254, "pasa de pantalla", 16, Tizador.Mostaza, true); p.Texto(480, 236, "pero no guarda nada", 16, Tizador.Roja, true);
            };
            d["mvp-proto-f"] = p => {
                p.Navegador(40, 50, 160, 140, Tizador.Cian); p.Flecha(205, 120, 265, 120, Tizador.Cian); p.Servidor(275, 60, 80, 120, Tizador.Cian); p.Flecha(360, 120, 420, 120, Tizador.Cian); p.Cilindro(430, 80, 90, 80, Tizador.Cian);
                p.Caja(60, 100, 120, 34, "una función", Tizador.Mostaza, false, 14); p.Caja(60, 146, 120, 30, null, Tizador.Gris, true);
                p.Texto(300, 230, "una sola cosa funciona de punta a punta", 17, Tizador.Cian, true); p.Texto(300, 260, "sirve para probar una idea; después se suele desechar", 16, Tizador.Gris, true);
            };
            d["mvp-minimo"] = p => {
                p.Navegador(200, 30, 200, 160, Tizador.Mostaza);
                for (var i = 0; i < 3; i++) { p.Caja(220, 70 + i * 38, 130, 28, null, Tizador.Cian); p.Si(374, 84 + i * 38, null, 9); }
                p.Figura(100, 90, Tizador.Tiza); p.Figura(60, 150, Tizador.Tiza); p.Figura(140, 150, Tizador.Tiza); p.Flecha(160, 110, 195, 110, Tizador.Tiza);
                p.Texto(500, 90, "clientes reales", 17, Tizador.Tiza, true); p.Texto(500, 114, "lo usan", 17, Tizador.Tiza, true); p.Bocadillo(440, 150, 130, 40, "«falta esto»", Tizador.Mostaza, 15);
                p.Texto(300, 264, "lo esencial, bien hecho, y a aprender de quien lo usa", 16, Tizador.Mostaza, true);
            };
            d["mvp-beta"] = p => {
                p.Navegador(180, 30, 240, 180, Tizador.Cian);
                for (var f = 0; f < 3; f++) for (var k = 0; k < 2; k++) p.Caja(200 + k * 104, 70 + f * 42, 94, 32, null, f == 2 && k == 1 ? Tizador.Gris : Tizador.Cian, f == 2 && k == 1);
                p.Bicho(120, 80, Tizador.Roja); p.Lupa(110, 150, 22, Tizador.Mostaza); p.Texto(100, 220, "se cazan errores", 16, Tizador.Mostaza, true);
                p.Figura(480, 80, Tizador.Tiza); p.Figura(530, 130, Tizador.Tiza); p.Texto(500, 210, "con un grupo", 16, Tizador.Tiza, true); p.Texto(500, 232, "pequeño", 16, Tizador.Tiza, true);
                p.Texto(300, 270, "casi todo está, y se pule antes de abrirlo a todos", 16, Tizador.Cian, true);
            };
            d["mvp-completo"] = p => {
                p.Navegador(60, 40, 240, 180, Tizador.Cian);
                for (var f = 0; f < 3; f++) for (var k = 0; k < 2; k++) p.Caja(80 + k * 104, 80 + f * 42, 94, 32, null, Tizador.Cian);
                p.Estrella(370, 70, 22, Tizador.Mostaza); p.Texto(470, 78, "todo lo planeado", 17, Tizador.Mostaza, true);
                p.LlaveInglesa(350, 110, Tizador.Tiza); p.Texto(480, 144, "con mantenimiento", 17, Tizador.Tiza, true);
                p.Figura(370, 190, Tizador.Cian); p.Texto(470, 216, "y con soporte", 17, Tizador.Cian, true);
                p.Texto(300, 272, "abierto a todos los usuarios", 16, Tizador.Tiza, true);
            };

            // ------------------------------------------------------------ backlog
            d["bl-que"] = p => {
                var filas = new[] { "registrar la entrada", "avisar al anfitrión", "quién sigue dentro", "exportar a Excel", "cambiar colores" };
                for (var i = 0; i < 5; i++) { p.Texto(150, 56 + i * 50, (i + 1).ToString(), 24, Tizador.Mostaza, true); p.Tarjeta(180, 30 + i * 50, 250, 40, i < 3 ? Tizador.Cian : Tizador.Gris, filas[i]); }
                p.Flecha(480, 250, 480, 40, Tizador.Mostaza); p.Texto(540, 60, "lo más", 16, Tizador.Mostaza, true); p.Texto(540, 82, "valioso", 16, Tizador.Mostaza, true); p.Texto(540, 240, "puede esperar", 15, Tizador.Gris, true);
                p.Texto(70, 150, "todo lo", 17, Tizador.Tiza, true); p.Texto(70, 174, "que falta", 17, Tizador.Tiza, true);
            };
            d["bl-detalle"] = p => {
                p.Caja(60, 20, 230, 86, null, Tizador.Cian); p.Texto(175, 44, "registrar la entrada", 15, Tizador.Cian, true); p.Linea(76, 58, 274, 58, Tizador.Cian, 2); p.Linea(76, 74, 250, 74, Tizador.Cian, 2); p.Linea(76, 90, 220, 90, Tizador.Cian, 2);
                p.Caja(60, 116, 230, 60, null, Tizador.Tiza); p.Linea(76, 138, 274, 138, Tizador.Tiza, 2); p.Linea(76, 156, 220, 156, Tizador.Tiza, 2);
                p.Caja(60, 186, 230, 36, null, Tizador.Gris, true); p.Caja(60, 232, 230, 26, null, Tizador.Gris, true); p.Caja(60, 266, 230, 18, null, Tizador.Gris, true);
                p.Texto(440, 60, "arriba: claro y detallado,", 16, Tizador.Cian, true); p.Texto(440, 84, "listo para empezar", 16, Tizador.Cian, true);
                p.Texto(440, 230, "abajo: una idea suelta,", 16, Tizador.Gris, true); p.Texto(440, 254, "ya se detallará", 16, Tizador.Gris, true);
            };
            d["bl-dueno"] = p => {
                for (var i = 0; i < 3; i++) { p.Figura(60, 50 + i * 86, Tizador.Tiza); p.Curva(90, 66 + i * 86, 170, 100 + i * 20, 240, 130, Tizador.Gris); }
                p.Figura(280, 110, Tizador.Mostaza, "ordena la lista"); p.Flecha(320, 130, 390, 130, Tizador.Mostaza);
                for (var i = 0; i < 4; i++) p.Tarjeta(410, 40 + i * 54, 150, 42, i == 0 ? Tizador.Mostaza : Tizador.Tiza);
                p.Texto(130, 286, "cliente y usuarios piden", 15, Tizador.Tiza, true);
            };
            d["bl-vivo"] = p => {
                p.Tarjeta(30, 110, 110, 46, Tizador.Mostaza, "idea nueva"); p.Flecha(150, 133, 220, 133, Tizador.Mostaza);
                for (var i = 0; i < 4; i++) p.Tarjeta(235, 50 + i * 52, 140, 40, Tizador.Tiza);
                p.Flecha(390, 70, 450, 70, Tizador.Cian); p.Tarjeta(465, 48, 110, 46, Tizador.Cian, "hecha"); p.Si(520, 120, null, 12);
                p.Flecha(390, 226, 436, 226, Tizador.Roja); p.Tarjeta(446, 204, 140, 46, Tizador.Roja, "ya no hace falta"); p.Texto(305, 284, "nunca está terminado", 17, Tizador.Tiza, true);
            };
            d["bl-producto"] = p => {
                p.Caja(170, 20, 260, 260, null, Tizador.Tiza); p.Texto(300, 46, "todo el producto", 18, Tizador.Mostaza, true);
                for (var i = 0; i < 6; i++) p.Tarjeta(190, 60 + i * 36, 220, 28, i < 2 ? Tizador.Cian : Tizador.Gris);
            };
            d["bl-sprint"] = p => {
                p.Caja(170, 60, 260, 170, null, Tizador.Cian); p.Texto(300, 90, "solo este sprint", 18, Tizador.Cian, true);
                p.Tarjeta(190, 110, 220, 40, Tizador.Cian); p.Tarjeta(190, 164, 220, 40, Tizador.Cian); p.RelojDeArena(500, 140, 90, Tizador.Mostaza);
            };
            d["bl-sin-orden"] = p => {
                var xy = new[] { 60f, 40, 250, 70, 130, 150, 330, 180, 90, 220, 400, 50, 440, 130 };
                for (var i = 0; i < 7; i++) p.Tarjeta(xy[i * 2], xy[i * 2 + 1], 110, 46, Tizador.Gris);
                p.Texto(300, 140, "?", 80, Tizador.Roja, true);
            };
            d["bl-todo-urgente"] = p => {
                for (var i = 0; i < 4; i++) { p.Tarjeta(170, 30 + i * 62, 220, 50, Tizador.Roja, "URGENTE"); p.Texto(140, 64 + i * 62, "1", 26, Tizador.Roja, true); }
                p.Sirena(470, 150, Tizador.Roja);
            };
            d["bl-infinito"] = p => {
                for (var i = 0; i < 9; i++) p.Tarjeta(210, 10 + i * 31, 180, 24, i < 2 ? Tizador.Tiza : Tizador.Gris);
                p.Telarana(390, 190, 80, Tizador.Gris); p.Texto(110, 240, "ideas de", 17, Tizador.Gris, true); p.Texto(110, 264, "hace un año", 17, Tizador.Gris, true);
            };
        }
    }
}
