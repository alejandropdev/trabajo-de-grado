using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>La Fase 3 (d-…): que es desplegar, los entornos, los secretos y la vuelta atras.</summary>
    public static partial class DibujosDeEscena {
        /// <summary>Un tramo de tuberia con su nombre: dos lineas y dos bridas.</summary>
        private static void Tramo(Tizador p, float x, float y, float w, string t, Color? c = null) {
            p.Linea(x, y - 16, x + w, y - 16, c); p.Linea(x, y + 16, x + w, y + 16, c);
            p.Linea(x, y - 24, x, y + 24, c, 5); p.Linea(x + w, y - 24, x + w, y + 24, c, 5);
            if (t != null) p.Texto(x + w / 2, y + 6, t, 14, c ?? Tizador.Tiza, true);
        }

        private static void Despliegue(Dictionary<string, Action<Tizador>> d) {
            d["d-despliegue"] = p => {
                p.Navegador(30, 70, 150, 110, Tizador.Gris); p.Figura(105, 220, Tizador.Tiza); p.Texto(105, 284, "en tu computador", 15, Tizador.Gris, true);
                p.Flecha(200, 125, 380, 125, Tizador.Mostaza); p.Texto(290, 108, "desplegar", 18, Tizador.Mostaza, true);
                p.Navegador(400, 70, 150, 110, Tizador.Cian); p.Figura(440, 220, Tizador.Cian); p.Figura(510, 220, Tizador.Cian); p.Si(475, 124, null, 18);
                p.Texto(475, 284, "donde lo usa el cliente", 15, Tizador.Cian, true);
            };
            d["d-despliegue-partes"] = p => {
                p.Texto(150, 30, "por partes", 18, Tizador.Cian, true);
                for (var i = 0; i < 3; i++) { p.Caja(40 + i * 76, 60 + i * 44, 64, 34, null, Tizador.Cian); p.Flecha(72 + i * 76, 98 + i * 44, 72 + i * 76, 220, Tizador.Cian); }
                p.Carita(150, 250, 20, 1, Tizador.Mostaza); p.Texto(150, 290, "el cliente lo va viendo", 14, Tizador.Mostaza, true);
                p.Linea(300, 20, 300, 280, Tizador.Gris, 2, true);
                p.Texto(450, 30, "todo junto", 18, Tizador.Roja, true);
                p.Caja(370, 60, 160, 110, null, Tizador.Roja); for (var i = 0; i < 3; i++) p.Caja(384 + i * 46, 80 + (i % 2) * 30, 38, 28, null, Tizador.Tiza);
                p.Flecha(450, 176, 450, 220, Tizador.Roja); p.Carita(450, 250, 20, 0, Tizador.Tiza); p.Texto(450, 290, "una sola vez, al final", 14, Tizador.Roja, true);
            };
            d["d-entorno"] = p => {
                var nombre = new[] { "desarrollo", "pruebas", "producción" }; var quien = new[] { "tu computador", "un ensayo, sin clientes", "lo de verdad" };
                var c = new[] { Tizador.Gris, Tizador.Mostaza, Tizador.Cian };
                for (var i = 0; i < 3; i++) {
                    p.Caja(30 + i * 195, 60, 150, 130, null, c[i]); p.Texto(105 + i * 195, 44, nombre[i], 18, c[i], true);
                    p.Navegador(52 + i * 195, 80, 106, 70, c[i]); p.Texto(105 + i * 195, 222, quien[i], 14, Tizador.Tiza, true);
                    if (i < 2) p.Flecha(184 + i * 195, 125, 220 + i * 195, 125, Tizador.Tiza);
                }
                p.Figura(105, 250, Tizador.Gris); p.Figura(450, 250, Tizador.Cian); p.Figura(520, 250, Tizador.Cian);
            };
            d["d-entorno-mezcla"] = p => {
                p.Caja(40, 90, 180, 110, "módulo en producción", Tizador.Cian, false, 14);
                p.Cilindro(330, 30, 110, 90, Tizador.Cian); p.Texto(520, 80, "datos de verdad", 15, Tizador.Cian, true);
                p.Cilindro(330, 180, 110, 90, Tizador.Gris); p.Texto(520, 230, "datos de prueba", 15, Tizador.Gris, true);
                p.Curva(222, 150, 280, 230, 326, 226, Tizador.Roja); p.No(276, 196, null, 12);
                p.Curva(222, 130, 280, 70, 326, 76, Tizador.Gris, false); p.Texto(130, 240, "enchufado donde no era", 15, Tizador.Roja, true);
            };
            d["d-secretos"] = p => {
                p.Caja(60, 60, 180, 170, null, Tizador.Cian); p.Candado(150, 140, 30, Tizador.Cian);
                p.Texto(150, 262, "la caja de secretos", 16, Tizador.Cian, true);
                p.Llave(330, 90, Tizador.Mostaza); p.Llave(330, 150, Tizador.Mostaza); p.Flecha(320, 120, 250, 130, Tizador.Mostaza);
                p.Texto(470, 110, "claves y contraseñas", 16, Tizador.Mostaza, true); p.Texto(470, 190, "el programa las pide", 15, Tizador.Tiza, true); p.Texto(470, 212, "cuando las necesita", 15, Tizador.Tiza, true);
            };
            d["d-secretos-a-la-vista"] = p => {
                p.Hoja(60, 30, 220, 240, Tizador.Gris, "el código"); p.Bloque(80, 130, 180, 30, Tizador.Roja); p.Texto(170, 152, "clave = 1234", 16, Tizador.Pizarra, true);
                p.Figura(400, 110, Tizador.Roja); p.Lupa(350, 150, 18, Tizador.Roja); p.Texto(470, 200, "cualquiera que vea", 15, Tizador.Roja, true); p.Texto(470, 222, "el código la lee", 15, Tizador.Roja, true);
            };
            d["d-vuelta-atras"] = p => {
                p.Caja(60, 100, 130, 80, "versión 1", Tizador.Cian, false, 18); p.Si(125, 210, null, 12); p.Texto(125, 244, "funcionaba", 15, Tizador.Cian, true);
                p.Flecha(200, 140, 300, 140, Tizador.Tiza);
                p.Caja(310, 100, 130, 80, "versión 2", Tizador.Roja, false, 18); p.Bicho(375, 214, Tizador.Roja); p.Texto(375, 250, "falla", 15, Tizador.Roja, true);
                p.Curva(375, 96, 250, 20, 125, 96, Tizador.Mostaza); p.Texto(250, 30, "vuelta atrás", 18, Tizador.Mostaza, true);
                p.Texto(520, 140, "en minutos,", 15, Tizador.Tiza, true); p.Texto(520, 162, "no en horas", 15, Tizador.Tiza, true);
            };
            d["d-vuelta-sin-paquete"] = p => {
                p.Caja(70, 80, 150, 100, null, Tizador.Gris, true); p.Texto(145, 140, "?", 50, Tizador.Roja, true); p.Texto(145, 214, "¿cuál era la anterior?", 15, Tizador.Roja, true);
                p.Figura(360, 110, Tizador.Tiza); p.Texto(360, 60, "?", 34, Tizador.Roja, true);
                p.Texto(480, 130, "sin versiones", 15, Tizador.Tiza, true); p.Texto(480, 152, "guardadas, no hay", 15, Tizador.Tiza, true); p.Texto(480, 174, "a dónde volver", 15, Tizador.Tiza, true);
            };

            // ------------------------------------------------------------ como se ve en el juego
            d["j-tuberia"] = p => {
                p.Caja(10, 100, 70, 80, "código", Tizador.Gris, false, 14); p.Caja(520, 100, 70, 80, "cliente", Tizador.Mostaza, false, 14);
                var n = new[] { "compilar", "probar", "ensayar", "producir" };
                for (var i = 0; i < 4; i++) Tramo(p, 90 + i * 106, 140, 96, n[i], i == 2 ? Tizador.Mostaza : Tizador.Cian);
                p.Texto(300, 250, "arrastra cada tramo a su hueco", 20, Tizador.Tiza, true);
            };
            d["j-enchufes"] = p => {
                p.Caja(30, 60, 190, 180, null, Tizador.Tiza); p.Texto(125, 90, "módulo", 18, Tizador.Tiza, true);
                p.Caja(50, 120, 150, 40, "datos", Tizador.Mostaza, false, 16); p.Caja(50, 176, 150, 40, "clave", Tizador.Mostaza, false, 16);
                p.Caja(380, 50, 190, 50, "producción", Tizador.Cian, false, 16); p.Caja(380, 126, 190, 50, "pruebas", Tizador.Gris, true, 16); p.Caja(380, 202, 190, 50, "caja de secretos", Tizador.Cian, false, 16);
                p.Curva(204, 140, 290, 80, 376, 76, Tizador.Cian); p.Curva(204, 196, 290, 240, 376, 228, Tizador.Cian);
            };
            d["j-cuenta-atras"] = p => {
                p.Caja(170, 70, 260, 120, null, Tizador.Roja); p.Texto(300, 156, "4:59", 70, Tizador.Roja, true);
                p.Texto(300, 240, "una sola cuenta atrás para todo", 20, Tizador.Tiza, true);
            };
        }
    }
}
