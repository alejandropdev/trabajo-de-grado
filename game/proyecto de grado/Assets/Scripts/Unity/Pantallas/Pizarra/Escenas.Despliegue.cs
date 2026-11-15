using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;
using Nexus.Unity.Tema;
using L = Nexus.Unity.Tema.LuzUI;

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

            // ------------------------------------------------------------ como se ve en el juego
            d["j-tuberia"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Nucleo, 0.5f, 113);
                PanelUI(v, 14, 104, 72, 80); v.Rotulo(50, 144, "código", 14, TintaSuave, 0, true);
                v.Halo(550, 144, 60, A(Aviso, 0.3f));
                PanelUI(v, 514, 104, 72, 80, null); v.Forma(L.Redondeado(512, 102, 76, 84, 13), A(Aviso, 0.9f)); v.Forma(L.Redondeado(514, 104, 72, 80, 12), SuperficieAlta, Superficie);
                v.Rotulo(550, 144, "cliente", 14, Aviso, 0, true);
                var n = new[] { "compilar", "probar", "ensayar", "producir" };
                for (var i = 0; i < 4; i++) {
                    var x = 96 + i * 104; var c = i == 2 ? Aviso : Nucleo;
                    v.Forma(L.Redondeado(x, 126, 96, 36, 8), Color.Lerp(Superficie, c, 0.18f), Superficie);
                    v.Neon(Pts(x + 6, 127, x + 90, 127), 0.9f, c);
                    v.Forma(L.Redondeado(x - 4, 120, 8, 48, 3), Cromo, CromoOscuro);
                    v.Rotulo(x + 48, 145, n[i], 14, c, 0, true);
                }
                v.Forma(L.Redondeado(508, 120, 8, 48, 3), Cromo, CromoOscuro);
                v.Rotulo(300, 252, "arrastra cada tramo a su hueco", 19, Tinta);
            };
            d["j-enchufes"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Nucleo, 0.5f, 115);
                PanelUI(v, 30, 56, 196, 188, "módulo", Tinta);
                v.Forma(L.Redondeado(48, 116, 160, 40, 8), Color.Lerp(Superficie, Aviso, 0.18f), Superficie); v.Rotulo(128, 136, "datos", 15, Aviso, 0, true);
                v.Forma(L.Redondeado(48, 174, 160, 40, 8), Color.Lerp(Superficie, Aviso, 0.18f), Superficie); v.Rotulo(128, 194, "clave", 15, Aviso, 0, true);
                v.Neon(new RutaDeCurva(210, 136).C(290, 136, 300, 76, 374, 76).Puntos(), 1.2f, Nucleo);
                v.Neon(new RutaDeCurva(210, 194).C(290, 194, 300, 228, 374, 228).Puntos(), 1.2f, Nucleo);
                foreach (var y in new[] { 136f, 194f }) v.Forma(L.Elipse(210, y, 5, 5, 12), Cromo, CromoOscuro);
                PanelUI(v, 380, 50, 190, 52); v.Rotulo(475, 76, "producción", 15, Nucleo, 0, true);
                v.Forma(L.Redondeado(380, 126, 190, 52, 12), A(Superficie, 0.5f)); v.Rotulo(475, 152, "pruebas", 15, A(TintaSuave, 0.6f), 0, true);
                PanelUI(v, 380, 202, 190, 52); v.Rotulo(475, 228, "caja de secretos", 15, Nucleo, 0, true);
                foreach (var y in new[] { 76f, 228f }) { v.Halo(378, y, 14, A(Nucleo, 0.6f)); v.Forma(L.Elipse(378, y, 5, 5, 12), NucleoClaro, Nucleo); }
            };
            d["j-cuenta-atras"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Coral, 0.6f, 117);
                v.Latido(v.Halo(300, 130, 220, 90, A(Coral, 0.3f)), 0.5f, 1, 1f);
                v.Forma(L.Redondeado(162, 62, 276, 136, 18), Cromo, CromoOscuro);
                v.Forma(L.Redondeado(172, 72, 256, 116, 12), H(0x1A0806), H(0x0A0302));
                v.Rotulo(300, 130, "4:59", 72, Coral, 0, false, true);
                v.Rotulo(300, 244, "una sola cuenta atrás para todo", 19, Tinta);
            };
        }
    }
}
