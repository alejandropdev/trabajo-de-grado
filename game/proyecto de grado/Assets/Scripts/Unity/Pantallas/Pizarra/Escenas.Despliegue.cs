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
                var v = p.Vitrina();
                FondoInterior(v, 475, 120, Nucleo, 0.6f, 231);
                Monitor(v, 110, 200, 150, 104, TintaSuave, A(TintaSuave, 0.7f), A(TintaSuave, 0.7f), A(TintaSuave, 0.7f), A(TintaSuave, 0.7f));
                v.Rotulo(110, 236, "en tu computador", 15, TintaSuave, 0, true);
                FlechaNeon(v, 200, 130, 370, 130, Aviso);
                v.Rotulo(286, 110, "desplegar", 18, Aviso, 0, true);
                PanelUI(v, 390, 56, 170, 120, "producción");
                v.Halo(475, 126, 40, A(Menta, 0.35f)); v.Neon(Pts(458, 126, 472, 140, 496, 110), 2, Menta);
                Credencial(v, 440, 216, Nucleo); Credencial(v, 510, 216, Nucleo);
                v.Rotulo(475, 288, "donde lo usa el cliente", 14, Nucleo, 0, true);
            };
            d["d-despliegue-partes"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 233);
                v.Rotulo(150, 30, "por partes", 18, Nucleo, 0, true);
                for (var i = 0; i < 3; i++) {
                    TarjetaUI(v, 40 + i * 76, 56 + i * 40, 64, 32, Nucleo);
                    v.Neon(Pts(72 + i * 76, 92 + i * 40, 72 + i * 76, 214), 0.8f, Nucleo);
                }
                v.Halo(150, 240, 30, A(Menta, 0.4f)); v.Forma(L.Elipse(150, 240, 9, 9, 16), Color.Lerp(Menta, Color.white, 0.4f));
                v.Rotulo(150, 278, "el cliente lo va viendo", 15, Menta, 0, true);
                v.Forma(Caja(299, 20, 2, 260), A(Tinta, 0.12f));
                v.Rotulo(450, 30, "todo junto", 18, Coral, 0, true);
                CajaDeCarga(v, 380, 70, 140, 96, H(0x5A3A36));
                FlechaNeon(v, 450, 174, 450, 222, Coral);
                v.Forma(L.Elipse(450, 240, 9, 9, 16), H(0x2A3538));
                v.Rotulo(450, 278, "una sola vez, al final", 15, Coral, 0, true);
            };
            d["d-entorno"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 235);
                var nombre = new[] { "desarrollo", "pruebas", "producción" }; var quien = new[] { "tu computador", "un ensayo, sin clientes", "lo de verdad" };
                var c = new[] { TintaSuave, Aviso, Nucleo };
                for (var i = 0; i < 3; i++) {
                    var x = 30 + i * 195;
                    PanelUI(v, x, 30, 150, 140, nombre[i], c[i]);
                    v.Forma(L.Redondeado(x + 18, 78, 114, 74, 8), H(0x0E3340), H(0x05161B));
                    Renglones(v, x + 30, 92, 90, 3, 14);
                    v.Halo(x + 75, 115, 50, A(c[i], 0.15f));
                    v.Rotulo(x + 75, 186, quien[i], 14, Tinta, 0);
                    if (i < 2) FlechaNeon(v, x + 154, 100, x + 190, 100, Tinta);
                }
                Credencial(v, 105, 238, TintaSuave); Credencial(v, 470, 238, Nucleo); Credencial(v, 530, 238, Nucleo);
            };
            d["d-entorno-mezcla"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Coral, 0.5f, 237);
                v.Neon(new RutaDeCurva(222, 130).C(270, 130, 280, 76, 330, 76).Puntos(), 1, A(TintaSuave, 0.5f));
                v.Neon(new RutaDeCurva(222, 150).C(270, 150, 280, 226, 330, 226).Puntos(), 1.3f, Coral);
                PanelUI(v, 40, 90, 180, 110); v.Rotulo(130, 145, "módulo en producción", 15, Nucleo, 0, true);
                Icono(v.En(320, 16, 0.8f), "base-de-datos", Tizador.Cian); v.Rotulo(520, 70, "datos de verdad", 15, Nucleo, 0, true);
                Icono(v.En(320, 168, 0.8f), "base-de-datos", Tizador.Gris); v.Rotulo(520, 222, "datos de prueba", 15, TintaSuave, 0, true);
                Chispa(v, 276, 196, 8, 0.3f);
                v.Rotulo(130, 240, "enchufado donde no era", 15, Coral, 0, true);
            };
            d["d-secretos"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 150, 140, Nucleo, 0.5f, 239);
                v.Forma(L.Redondeado(60, 50, 180, 180, 14), H(0x46555A), H(0x161D20));
                v.Forma(L.Redondeado(72, 62, 156, 156, 10), H(0x2B3335), H(0x101416));
                Valvula(v, 150, 140, 34, Cromo);
                v.Halo(212, 76, 10, A(Menta, 0.6f));
                v.Rotulo(150, 262, "la caja de secretos", 16, Nucleo, 0, true);
                Llave(v, 296, 92, 60, 10); Llave(v, 296, 150, 60, -6);
                FlechaNeon(v, 300, 122, 250, 132, Aviso);
                v.Rotulo(470, 110, "claves y contraseñas", 16, Aviso, 0, true);
                v.Rotulo(470, 190, "el programa las pide", 15, Tinta, 0, true); v.Rotulo(470, 212, "cuando las necesita", 15, Tinta, 0, true);
            };
            d["d-secretos-a-la-vista"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Coral, 0.5f, 241);
                v.Forma(L.Redondeado(66, 36, 220, 240, 6), A(Silueta, 0.5f));
                v.Forma(L.Redondeado(60, 30, 220, 240, 6), H(0xEDE7D6), H(0xCFC7B0));
                v.Rotulo(170, 54, "el código", 15, H(0x1E2A2A), 0, true);
                for (var i = 0; i < 8; i++) if (i != 3) v.Forma(Caja(80, 80 + i * 22, i % 2 == 0 ? 170 : 130, 4), A(H(0x1E2A2A), 0.4f));
                v.Halo(170, 146, 120, 22, A(Coral, 0.35f));
                v.Forma(L.Redondeado(76, 132, 188, 28, 4), Coral);
                v.Rotulo(170, 146, "clave = 1234", 16, H(0x2A0B06), 0, true);
                Credencial(v, 420, 76, Coral);
                Lupa(v, 340, 150, 26, Coral);
                v.Rotulo(470, 206, "cualquiera que vea", 16, Coral, 0, true); v.Rotulo(470, 230, "el código la lee", 16, Coral, 0, true);
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
