using System;
using System.Collections.Generic;
using Nexus.Core.Proyecto;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using UnityEngine;
using L = Nexus.Unity.Tema.LuzUI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// (2026-11-15, direccion de arte) «Cómo está armado» en el lenguaje nuevo: el plano de la arquitectura elegida con los
    /// modulos de ESTE proyecto como piezas de neon (un programa con paredes, servicios sueltos con su base, un servidor tras
    /// un navegador o un movil, o lo nuevo pegado a una barrera vieja). Cuadro de 1400×560, como siempre.
    /// </summary>
    public static partial class DibujosDeEscena {
        private const float AnchoPlano = 1400, AltoPlano = 560;

        public static void PlanoDeArquitectura(Tizador t, FichaDelProyecto f, string id, string nombre) {
            var modulos = f == null ? new List<ModuloDelProyecto>() : f.Modulos;
            var v = t.Vitrina(AnchoPlano, AltoPlano);
            v.Rotulo(AnchoPlano / 2, 26, nombre ?? "", 24, Aviso, 0, true);
            switch (id) {
                case "microservicios": PlanoMicro(v, modulos); break;
                case "parche-sobre-la-barrera": PlanoParche(v, modulos); break;
                case "app-movil": PlanoCliente(v, modulos, true); break;
                case "web-sencilla": PlanoCliente(v, modulos, false); break;
                default: PlanoMono(v, modulos); break;
            }
        }

        /// <summary>Los modulos en rejilla dentro de un rectangulo; 'paredes' las separa con lineas a trazos.</summary>
        private static void PlanoRejilla(Tizador v, List<ModuloDelProyecto> modulos, float x, float y, float w, float h, Color c, bool paredes) {
            var n = Math.Max(1, modulos.Count);
            var cols = n <= 3 ? n : n <= 4 ? 2 : 3;
            var filas = (int)Math.Ceiling(n / (double)cols);
            float cw = w / cols, ch = h / filas;
            for (var i = 0; i < modulos.Count; i++) {
                float cx = x + (i % cols) * cw, cy = y + (i / cols) * ch;
                Pieza(v, cx + 14, cy + 14, cw - 28, ch - 28, modulos[i].Nombre, c, 18);
            }
            if (!paredes) return;
            for (var k = 1; k < cols; k++) for (var yy = y + 6; yy < y + h - 12; yy += 16) v.Forma(Caja(x + k * cw - 1, yy, 2, 8), A(TintaSuave, 0.5f));
            for (var k = 1; k < filas; k++) for (var xx = x + 6; xx < x + w - 12; xx += 16) v.Forma(Caja(xx, y + k * ch - 1, 8, 2), A(TintaSuave, 0.5f));
        }

        private static void PlanoMono(Tizador v, List<ModuloDelProyecto> modulos) {
            Credencial(v, 70, 180, Tinta, "quien lo usa");
            FlechaNeon(v, 120, 250, 210, 250, Aviso);
            PanelUI(v, 220, 70, 800, 400);
            v.Halo(228, 250, 24, 70, A(Aviso, 0.4f)); v.Forma(L.Redondeado(214, 210, 16, 80, 4), Aviso);   // la unica puerta
            PlanoRejilla(v, modulos, 250, 84, 756, 372, Nucleo, true);
            FlechaNeon(v, 1030, 250, 1110, 250, Tinta);
            Icono(v.En(1100, 170, 1.6f), "base-de-datos", Tizador.Tiza);
            v.Rotulo(1195, 398, "una base de datos", 18, Tinta);
            v.Rotulo(620, 500, "UN SOLO PROGRAMA, con paredes por dentro · una puerta · un despliegue", 22, Nucleo);
            v.Rotulo(620, 536, "los módulos se hablan dentro del programa, sin red de por medio", 18, TintaSuave);
        }

        private static void PlanoMicro(Tizador v, List<ModuloDelProyecto> modulos) {
            var n = Math.Max(1, modulos.Count);
            var w = Mathf.Min(220, (AnchoPlano - 80) / n);
            var x0 = (AnchoPlano - w * n) / 2;
            v.Neon(Pts(x0 + 20, 96, x0 + w * n - 20, 96), 1.1f, A(TintaSuave, 0.8f));
            Rele(v, AnchoPlano / 2, 66, Nucleo); v.Rotulo(AnchoPlano / 2 + 170, 76, "se llaman por la red", 17, TintaSuave);
            for (var i = 0; i < modulos.Count; i++) {
                var x = x0 + i * w;
                v.Neon(Pts(x + w / 2, 96, x + w / 2, 130), 0.8f, A(TintaSuave, 0.8f));
                Pieza(v, x + 12, 130, w - 24, 110, modulos[i].Nombre, Aviso, 18);
                v.Neon(Pts(x + w / 2, 240, x + w / 2, 276), 0.8f, A(TintaSuave, 0.8f));
                Icono(v.En(x + w / 2 - 66, 270, 1.5f), "base-de-datos", Tizador.Tiza);
                Icono(v.En(x + w / 2 - 36, 376, 0.75f), "servidor", Tizador.Tiza);
            }
            v.Rotulo(AnchoPlano / 2, 480, $"{modulos.Count} SERVICIOS · {modulos.Count} bases de datos · {modulos.Count} despliegues · {modulos.Count} cosas que vigilar", 22, Aviso);
            v.Rotulo(AnchoPlano / 2, 516, "cada uno se cambia solo, pero entre ellos hay red, y alguien tiene que operarlos todos", 18, TintaSuave);
        }

        private static void PlanoParche(Tizador v, List<ModuloDelProyecto> modulos) {
            v.Forma(L.Redondeado(120, 300, 520, 180, 10), H(0x2B3335), H(0x101416));
            Icono(v.En(300, 316, 1.1f), "candado", Tizador.Gris);
            v.Rotulo(380, 456, "LA BARRERA ACTUAL · nadie tiene su código", 20, Coral);
            PanelUI(v, 240, 80, 900, 200, null, Aviso);
            PlanoRejilla(v, modulos, 250, 86, 880, 188, Aviso, false);
            v.Rotulo(980, 330, "lo nuevo va pegado encima", 20, Aviso); v.Rotulo(980, 362, "de lo que ya funcionaba", 20, Aviso);
            Chispa(v, 760, 400, 14, 0.3f); v.Rotulo(980, 410, "lo que estaba mal debajo, sigue ahí", 18, Coral);
            v.Rotulo(AnchoPlano / 2, 520, "arranca con ventaja, pero se apoya en algo que no se controla · sale todo junto, viejo y nuevo", 20, TintaSuave);
        }

        private static void PlanoCliente(Tizador v, List<ModuloDelProyecto> modulos, bool movil) {
            if (movil) {
                for (var i = 0; i < 3; i++) Movil(v, 40 + i * 70, 150 + (i % 2) * 40, 56, 100, Aviso);
                v.Rotulo(140, 330, "una app en cada teléfono", 18, Aviso); v.Rotulo(140, 356, "hay que instalarla", 16, Coral);
            } else {
                Ventana(v, 40, 150, 200, 150); v.Rotulo(140, 330, "un navegador", 18, Nucleo); v.Rotulo(140, 356, "nadie instala nada", 16, TintaSuave);
            }
            FlechaNeon(v, 260, 220, 340, 220, Tinta); v.Rotulo(300, 200, "pide", 16, Tinta);
            FlechaNeon(v, 340, 250, 260, 250, TintaSuave); v.Rotulo(300, 278, "responde", 16, TintaSuave);
            PanelUI(v, 350, 70, 700, 400, "el servidor");
            PlanoRejilla(v, modulos, 370, 116, 660, 340, Nucleo, false);
            FlechaNeon(v, 1060, 250, 1130, 250, Tinta);
            Icono(v.En(1130, 170, 1.6f), "base-de-datos", Tizador.Tiza); v.Rotulo(1215, 398, "la base de datos", 18, Tinta);
            v.Rotulo(AnchoPlano / 2, 500, movil ? "DOS COSAS QUE CONSTRUIR: la app y el servidor" : "UNA SOLA COSA QUE CONSTRUIR Y PUBLICAR", 22, movil ? Aviso : Nucleo);
            v.Rotulo(AnchoPlano / 2, 536, movil ? "cada cambio hay que publicarlo en la tienda y esperar a que se actualice" : "se cambia en el servidor y todos ven la versión nueva al momento", 18, TintaSuave);
        }
    }
}
