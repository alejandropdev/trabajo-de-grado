using System;
using System.Collections.Generic;
using Nexus.Core.Proyecto;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// «Cómo está armado»: el plano de la arquitectura elegida, dibujado con los modulos de ESTE proyecto. La misma
    /// lista de modulos se ve como un solo programa con paredes, como servicios sueltos cada uno con su base, detras
    /// de un navegador o pegada encima de un sistema viejo. Es lo que hace visible la tercera decision de la Fase 1.
    /// Todo en un cuadro de 1400×560, para ponerlo a pagina completa o a escala.
    /// </summary>
    public static class DibujosDeArquitectura {
        public const float Ancho = 1400, Alto = 560;

        /// <summary>Lo que la arquitectura le hace al despliegue y al dia a dia, en una frase cada cosa.</summary>
        public static string AlDesplegar(string id) {
            switch (id) {
                case "microservicios": return "Cada módulo es un servicio aparte: cada uno se enciende por separado, con sus propios cables.";
                case "parche-sobre-la-barrera": return "Lo nuevo sale pegado a la barrera vieja: si lo viejo falla al encender, falla todo.";
                case "app-movil": return "Además del servidor, hay que publicar la app y esperar a que cada persona la instale.";
                case "web-sencilla": return "Se publica en un servidor y ya está: quien abre el navegador ve la versión nueva.";
                default: return "Sale todo junto, en un solo despliegue: una cosa que encender y una cosa que vigilar.";
            }
        }

        public static string EnElDia(string id) {
            switch (id) {
                case "microservicios": return "El equipo gasta parte de cada día en operar servicios: la deuda crece más deprisa.";
                case "parche-sobre-la-barrera": return "Se trabaja encima de código que nadie entiende: la deuda heredada pesa cada día.";
                case "app-movil": return "Hay dos cosas que construir (la app y el servidor): el equipo se cansa antes.";
                case "web-sencilla": return "Una sola cosa que construir y que mantener: no le añade trabajo al equipo.";
                default: return "Un solo programa bien separado por dentro: no le añade trabajo de operación al equipo.";
            }
        }

        public static void Plano(Tizador t, FichaDelProyecto f, string id, string nombre) {
            var modulos = f == null ? new List<ModuloDelProyecto>() : f.Modulos;
            t.Texto(Ancho / 2, 26, (nombre ?? "").ToUpperInvariant(), 26, Tizador.Mostaza, true);
            switch (id) {
                case "microservicios": Micro(t, modulos); break;
                case "parche-sobre-la-barrera": Parche(t, modulos); break;
                case "app-movil": Cliente(t, modulos, true); break;
                case "web-sencilla": Cliente(t, modulos, false); break;
                default: Mono(t, modulos); break;
            }
        }

        /// <summary>Los modulos en rejilla dentro de un rectangulo. Devuelve nada: solo dibuja.</summary>
        private static void Rejilla(Tizador t, List<ModuloDelProyecto> modulos, float x, float y, float w, float h, Color c, bool paredes) {
            var n = Math.Max(1, modulos.Count);
            var cols = n <= 3 ? n : n <= 4 ? 2 : 3;
            var filas = (int)Math.Ceiling(n / (double)cols);
            float cw = w / cols, ch = h / filas;
            for (var i = 0; i < modulos.Count; i++) {
                float cx = x + (i % cols) * cw, cy = y + (i / cols) * ch;
                t.Caja(cx + 14, cy + 14, cw - 28, ch - 28, null, c);
                t.Texto(cx + cw / 2, cy + ch / 2 + 6, modulos[i].Nombre, 20, Tizador.Tiza, true, cw - 48);
            }
            if (!paredes) return;
            for (var k = 1; k < cols; k++) t.Linea(x + k * cw, y + 6, x + k * cw, y + h - 6, Tizador.Gris, 2, true);
            for (var k = 1; k < filas; k++) t.Linea(x + 6, y + k * ch, x + w - 6, y + k * ch, Tizador.Gris, 2, true);
        }

        private static void Mono(Tizador t, List<ModuloDelProyecto> modulos) {
            t.Figura(70, 220, Tizador.Tiza); t.Texto(70, 300, "quien lo usa", 18, Tizador.Gris, true);
            t.Flecha(120, 250, 210, 250, Tizador.Mostaza);
            t.Caja(220, 70, 800, 400, null, Tizador.Cian);
            t.Caja(220, 210, 16, 80, null, Tizador.Mostaza);   // la unica puerta
            Rejilla(t, modulos, 250, 84, 756, 372, Tizador.Cian, true);
            t.Flecha(1030, 250, 1110, 250, Tizador.Tiza); t.Cilindro(1120, 180, 150, 140, Tizador.Tiza); t.Texto(1195, 350, "una base de datos", 18, Tizador.Tiza, true);
            t.Texto(620, 500, "UN SOLO PROGRAMA, con paredes por dentro · una puerta · un despliegue", 22, Tizador.Cian, true);
            t.Texto(620, 536, "los módulos se hablan dentro del programa, sin red de por medio", 18, Tizador.Gris, true);
        }

        private static void Micro(Tizador t, List<ModuloDelProyecto> modulos) {
            var n = Math.Max(1, modulos.Count);
            var w = Mathf.Min(220, (Ancho - 80) / n);
            var x0 = (Ancho - w * n) / 2;
            t.Linea(x0 + 20, 96, x0 + w * n - 20, 96, Tizador.Gris, 2, true);
            t.Nube(Ancho / 2, 70, 24, Tizador.Gris); t.Texto(Ancho / 2 + 160, 76, "se llaman por la red", 17, Tizador.Gris, true);
            for (var i = 0; i < modulos.Count; i++) {
                var x = x0 + i * w;
                t.Linea(x + w / 2, 96, x + w / 2, 130, Tizador.Gris, 2);
                t.Caja(x + 12, 130, w - 24, 110, null, Tizador.Mostaza);
                t.Texto(x + w / 2, 190, modulos[i].Nombre, 18, Tizador.Tiza, true, w - 44);
                t.Linea(x + w / 2, 240, x + w / 2, 276, Tizador.Gris, 2);
                t.Cilindro(x + w / 2 - 44, 276, 88, 84, Tizador.Tiza);
                t.Servidor(x + w / 2 - 24, 384, 48, 56, Tizador.Cian);
            }
            t.Texto(Ancho / 2, 480, $"{modulos.Count} SERVICIOS · {modulos.Count} bases de datos · {modulos.Count} despliegues · {modulos.Count} cosas que vigilar", 22, Tizador.Mostaza, true);
            t.Texto(Ancho / 2, 516, "cada uno se cambia solo, pero entre ellos hay red, y alguien tiene que operarlos todos", 18, Tizador.Gris, true);
        }

        private static void Parche(Tizador t, List<ModuloDelProyecto> modulos) {
            t.Caja(120, 300, 520, 180, null, Tizador.Gris); t.Candado(380, 360, 30, Tizador.Gris);
            t.Texto(380, 456, "LA BARRERA ACTUAL · nadie tiene su código", 20, Tizador.Roja, true);
            t.Caja(240, 80, 900, 200, null, Tizador.Mostaza, true);
            Rejilla(t, modulos, 250, 86, 880, 188, Tizador.Mostaza, false);
            t.Texto(980, 330, "lo nuevo va pegado encima", 20, Tizador.Mostaza, true);
            t.Texto(980, 362, "de lo que ya funcionaba", 20, Tizador.Mostaza, true);
            t.Bicho(760, 400, Tizador.Roja); t.Texto(980, 410, "lo que estaba mal debajo, sigue ahí", 18, Tizador.Roja, true);
            t.Texto(Ancho / 2, 520, "arranca con ventaja, pero se apoya en algo que no se controla · sale todo junto, viejo y nuevo", 20, Tizador.Gris, true);
        }

        private static void Cliente(Tizador t, List<ModuloDelProyecto> modulos, bool movil) {
            if (movil) {
                for (var i = 0; i < 3; i++) t.Telefono(40 + i * 70, 150 + (i % 2) * 40, 56, 100, Tizador.Mostaza);
                t.Texto(140, 330, "una app en cada teléfono", 18, Tizador.Mostaza, true); t.Texto(140, 356, "hay que instalarla", 16, Tizador.Roja, true);
            } else {
                t.Navegador(40, 150, 200, 150, Tizador.Cian); t.Texto(140, 330, "un navegador", 18, Tizador.Cian, true); t.Texto(140, 356, "nadie instala nada", 16, Tizador.Gris, true);
            }
            t.Flecha(260, 220, 340, 220, Tizador.Tiza); t.Texto(300, 200, "pide", 16, Tizador.Tiza, true);
            t.Flecha(340, 250, 260, 250, Tizador.Gris); t.Texto(300, 278, "responde", 16, Tizador.Gris, true);
            t.Caja(350, 70, 700, 400, null, Tizador.Cian); t.Texto(700, 100, "EL SERVIDOR", 20, Tizador.Cian, true);
            Rejilla(t, modulos, 370, 116, 660, 340, Tizador.Cian, false);
            t.Flecha(1060, 250, 1130, 250, Tizador.Tiza); t.Cilindro(1140, 180, 150, 140, Tizador.Tiza); t.Texto(1215, 350, "la base de datos", 18, Tizador.Tiza, true);
            t.Texto(Ancho / 2, 500, movil ? "DOS COSAS QUE CONSTRUIR: la app y el servidor" : "UNA SOLA COSA QUE CONSTRUIR Y PUBLICAR", 22, movil ? Tizador.Mostaza : Tizador.Cian, true);
            t.Texto(Ancho / 2, 536, movil ? "cada cambio hay que publicarlo en la tienda y esperar a que se actualice" : "se cambia en el servidor y todos ven la versión nueva al momento", 18, Tizador.Gris, true);
        }
    }
}
