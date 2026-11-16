using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;
using L = Nexus.Unity.Tema.LuzUI;
using Nexus.Unity.Tema;

namespace Nexus.Unity.Pantallas {
    /// <summary>La arquitectura (q-…): que es, como se elige, y como es por dentro un monolito, unos microservicios o un parche.</summary>
    public static partial class DibujosDeEscena {
        private static void Arquitectura(Dictionary<string, Action<Tizador>> d) {
            // ---------------------------------------------------------------- que es
            d["q-plano"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Nucleo, 0.5f, 331);
                v.Forma(L.Redondeado(40, 50, 210, 170, 4), H(0x24507A), H(0x173A5C));
                foreach (var t in new[] { Caja(56, 64, 84, 82), Caja(140, 64, 96, 82), Caja(56, 146, 180, 60) }) Contorno(v, t, 1.6f, A(Color.white, 0.8f));
                v.Rotulo(98, 105, "sala", 14, A(Color.white, 0.85f), 0, true); v.Rotulo(188, 105, "cocina", 14, A(Color.white, 0.85f), 0, true); v.Rotulo(146, 176, "pasillo", 14, A(Color.white, 0.85f), 0, true);
                v.Rotulo(145, 250, "el plano de una casa", 17, TintaSuave);
                v.Rotulo(300, 140, "=", 40, Aviso, 0, false, true);
                PanelUI(v, 350, 50, 210, 170);
                Pieza(v, 364, 66, 86, 40, "pantallas", Nucleo, 13); Pieza(v, 460, 66, 86, 40, "reglas", Nucleo, 13); Pieza(v, 412, 160, 86, 40, "datos", Aviso, 13);
                FlechaNeon(v, 410, 110, 440, 156, Tinta); FlechaNeon(v, 504, 110, 474, 156, Tinta);
                v.Rotulo(455, 250, "el plano de un sistema", 17, Nucleo);
            };
            d["q-piezas"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Nucleo, 0.5f, 333);
                v.Rotulo(300, 36, "cada pieza hace una cosa, y se hablan por las flechas", 17, TintaSuave);
                Monitor(v, 95, 200, 130, 96, Nucleo, A(Nucleo, 0.8f), A(Nucleo, 0.8f), A(Nucleo, 0.8f));
                v.Rotulo(95, 230, "lo que se ve", 16, Nucleo, 0, true);
                FlechaNeon(v, 168, 120, 238, 120, Tinta); v.Rotulo(203, 102, "pide", 14, Tinta, 0, true);
                FlechaNeon(v, 238, 152, 168, 152, TintaSuave); v.Rotulo(203, 176, "responde", 14, TintaSuave, 0, true);
                Icono(v.En(220, 68, 1f), "servidor", Tizador.Tiza);
                v.Rotulo(300, 230, "lo que decide", 16, Aviso, 0, true);
                FlechaNeon(v, 360, 136, 432, 136, Tinta); v.Rotulo(396, 118, "guarda", 14, Tinta, 0, true);
                Icono(v.En(422, 72, 0.95f), "base-de-datos", Tizador.Tiza);
                v.Rotulo(500, 230, "lo que recuerda", 16, Tinta, 0, true);
            };

            // ---------------------------------------------------------------- monolito modular
            d["q-mono-dentro"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Nucleo, 0.6f, 335);
                PanelUI(v, 110, 34, 380, 196);
                for (var y = 48f; y < 218; y += 14) { v.Forma(Caja(239, y, 2, 7), A(Borde, 0.9f)); v.Forma(Caja(359, y, 2, 7), A(Borde, 0.9f)); }
                Pieza(v, 128, 54, 100, 52, "reservas", Nucleo); Pieza(v, 250, 54, 100, 52, "turnos", Nucleo); Pieza(v, 372, 54, 100, 52, "acceso", Nucleo);
                Pieza(v, 188, 150, 100, 52, "panel", Nucleo); Pieza(v, 312, 150, 100, 52, "informes", Nucleo);
                v.Rotulo(300, 256, "un solo programa", 19, Nucleo, 0, true);
                v.Rotulo(300, 282, "con paredes por dentro: cada módulo, lo suyo", 16, TintaSuave);
            };
            d["q-mono-viaje"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 290, 130, Nucleo, 0.5f, 337);
                Credencial(v, 44, 92, Tinta);
                FlechaNeon(v, 76, 130, 140, 130, Aviso); v.Rotulo(108, 112, "pide", 14, Aviso, 0, true);
                PanelUI(v, 150, 56, 280, 150);
                v.Halo(156, 130, 20, 40, A(Aviso, 0.5f));
                v.Forma(L.Redondeado(146, 100, 14, 60, 4), Aviso);
                Pieza(v, 180, 80, 92, 44, "reservas", Nucleo, 14); Pieza(v, 312, 80, 92, 44, "turnos", Nucleo, 14);
                FlechaNeon(v, 276, 102, 308, 102, Tinta);
                v.Rotulo(291, 150, "se hablan por dentro,", 14, TintaSuave, 0); v.Rotulo(291, 170, "sin red de por medio", 14, TintaSuave, 0);
                FlechaNeon(v, 436, 130, 482, 130, Tinta);
                Icono(v.En(474, 76, 0.7f), "base-de-datos", Tizador.Tiza);
                v.Rotulo(530, 200, "una base", 14, Tinta, 0, true);
                v.Rotulo(300, 252, "una puerta de entrada, un camino corto", 17, Nucleo);
            };
            d["q-mono-despliegue"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 435, 140, Nucleo, 0.5f, 339);
                PanelUI(v, 60, 86, 150, 116);
                for (var i = 0; i < 4; i++) Pieza(v, 74 + (i % 2) * 66, 100 + (i / 2) * 48, 56, 38, null, Nucleo);
                FlechaNeon(v, 222, 144, 360, 144, Aviso); v.Rotulo(291, 124, "un despliegue", 17, Aviso, 0, true);
                Icono(v.En(358, 72, 1.1f), "servidor", Tizador.Tiza);
                NeonCheck(v, 538, 140, 18);
                v.Rotulo(135, 232, "sale todo junto", 16, Nucleo, 0, true); v.Rotulo(445, 244, "una sola cosa que vigilar", 16, Nucleo, 0, true);
            };
            d["q-mono-sin-modulos"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Nucleo, 0.5f, 341);
                v.Forma(L.Redondeado(38, 48, 214, 164, 13), A(Coral, 0.8f));
                v.Forma(L.Redondeado(40, 50, 210, 160, 12), SuperficieAlta, Superficie);
                Maraña(v, 56, 64, 178, 132, Coral, 11);
                v.Rotulo(145, 240, "sin paredes:", 16, Coral, 0, true); v.Rotulo(145, 264, "todo toca todo", 16, Coral, 0, true);
                v.Forma(Caja(299, 30, 2, 250), A(Tinta, 0.12f));
                PanelUI(v, 350, 50, 210, 160);
                Pieza(v, 364, 66, 84, 50, "A", Nucleo, 18); Pieza(v, 462, 66, 84, 50, "B", Nucleo, 18); Pieza(v, 412, 142, 84, 50, "C", Nucleo, 18);
                v.Rotulo(455, 240, "con módulos:", 16, Nucleo, 0, true); v.Rotulo(455, 264, "cambias uno sin romper otro", 16, Nucleo, 0, true);
            };

            // ---------------------------------------------------------------- microservicios
            d["q-micro-dentro"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 343);
                var nombres = new[] { "reservas", "turnos", "acceso", "informes" };
                for (var i = 0; i < 4; i++) {
                    var x = 40 + i * 140;
                    Pieza(v, x, 46, 110, 56, nombres[i], Aviso);
                    v.Neon(Pts(x + 55, 104, x + 55, 136), 0.8f, Aviso);
                    Icono(v.En(x + 15, 118, 0.5f), "base-de-datos", Tizador.Tiza);
                }
                v.Rotulo(300, 240, "cada servicio es un programa aparte, con su propia base", 17, Aviso);
                v.Rotulo(300, 266, "se despliegan y se caen por separado", 16, TintaSuave);
            };
            d["q-micro-viaje"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Aviso, 0.5f, 345);
                Credencial(v, 40, 92, Tinta);
                FlechaNeon(v, 70, 130, 132, 130, Aviso);
                Pieza(v, 138, 104, 100, 52, "reservas", Aviso);
                FlechaNeon(v, 242, 130, 276, 130, Tinta); Rele(v, 300, 136, Menta); FlechaNeon(v, 324, 130, 356, 130, Tinta);
                Pieza(v, 360, 104, 100, 52, "turnos", Aviso);
                v.Neon(Pts(464, 130, 482, 130), 1, A(Coral, 0.7f)); Rele(v, 506, 136, Coral); Chispa(v, 506, 112, 7, 0.3f);
                v.Forma(L.Redondeado(536, 108, 56, 48, 8), A(Coral, 0.08f)); v.Rotulo(564, 132, "…", 18, A(Coral, 0.6f), 0, true);
                v.Rotulo(300, 196, "entre servicio y servicio hay red", 17, Tinta);
                v.Rotulo(300, 222, "y la red puede fallar a mitad de camino", 17, Coral);
                v.Rotulo(300, 264, "cada salto hay que vigilarlo y reintentarlo", 16, TintaSuave);
            };
            d["q-micro-despliegue"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Aviso, 0.5f, 347);
                for (var i = 0; i < 4; i++) {
                    var y = 40 + i * 54;
                    Pieza(v, 40, y, 90, 40, null, Aviso);
                    for (var k = 0; k < 4; k++) { v.Forma(L.Redondeado(150 + k * 52, y + 12, 46, 16, 5), Color.Lerp(Superficie, Aviso, 0.18f)); v.Neon(Pts(154 + k * 52, y + 12, 192 + k * 52, y + 12), 0.6f, Aviso); }
                    FlechaNeon(v, 362, y + 20, 412, y + 20, Aviso);
                    Icono(v.En(400, y - 6, 0.38f), "servidor", Tizador.Tiza);
                }
                v.Rotulo(300, 280, "cuatro servicios, cuatro despliegues, cuatro cosas que vigilar", 16, Aviso);
            };
            d["q-micro-equipo"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 349);
                for (var i = 0; i < 3; i++) {
                    var x = 50 + i * 130;
                    Pieza(v, x, 30, 100, 44, null, Aviso);
                    Credencial(v, x + 28, 120, Nucleo); Credencial(v, x + 74, 120, Nucleo);
                }
                v.Rotulo(230, 206, "un equipo por servicio", 17, Nucleo, 0, true);
                Baliza(v, 500, 64);
                Credencial(v, 500, 120, Coral);
                v.Rotulo(500, 206, "y alguien de guardia", 17, Coral, 0, true);
                v.Rotulo(300, 250, "sirve cuando hay mucha gente y mucho uso;", 16, Tinta);
                v.Rotulo(300, 274, "con cuatro personas, se come al equipo", 16, Aviso);
            };

            // ---------------------------------------------------------------- parche sobre lo existente
            d["q-parche-dentro"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Aviso, 0.5f, 351);
                v.Forma(L.Redondeado(70, 70, 230, 150, 10), H(0x2B3335), H(0x101416));
                Icono(v.En(105, 76, 1f), "candado", Tizador.Gris);
                v.Rotulo(185, 248, "lo que ya había:", 16, TintaSuave, 0, true); v.Rotulo(185, 272, "nadie tiene el código", 16, Coral, 0, true);
                Pieza(v, 250, 40, 150, 80, "lo nuevo", Aviso, 18);
                Parche(v, 262, 112, 14); Parche(v, 386, 112, 14);
                FlechaNeon(v, 410, 80, 466, 80, Aviso);
                v.Rotulo(520, 68, "se pega", 16, Aviso, 0, true); v.Rotulo(520, 92, "encima", 16, Aviso, 0, true);
                v.Rotulo(500, 196, "arranca rápido:", 16, Nucleo, 0, true); v.Rotulo(500, 220, "algo ya funciona", 16, Nucleo, 0, true);
            };
            d["q-parche-riesgo"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 200, 140, Coral, 0.5f, 353);
                v.Forma(L.Redondeado(60, 250, 260, 6, 3), Cromo, CromoOscuro);
                v.Forma(L.Redondeado(90, 190, 180, 60, 6), H(0x2B3335), H(0x101416)); v.Rotulo(180, 220, "?", 26, TintaSuave, 0, false, true);
                CajaDeCarga(v, 110, 144, 150, 46, H(0x8A6A1A), -2);
                CajaDeCarga(v, 132, 98, 130, 42, H(0x8A6A1A), 3);
                CajaDeCarga(v, 166, 54, 110, 38, Coral, 9);
                Parche(v, 180, 140, 10); Parche(v, 230, 96, 10);
                Chispa(v, 290, 52, 8, 0.4f);
                v.Rotulo(450, 90, "parche sobre parche:", 17, Aviso, 0, true);
                v.Rotulo(450, 116, "cada uno se apoya en algo", 15, Tinta, 0, true); v.Rotulo(450, 138, "que nadie entiende", 15, Tinta, 0, true);
                Chispa(v, 420, 200, 7, 0.2f);
                v.Rotulo(440, 206, "y un día se cae", 17, Coral, -1);
            };
            d["q-parche-cuando"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 355);
                RelojDeArena(v, 90, 50, 90, Aviso, 0.6f);
                NeonCheck(v, 170, 100, 20);
                v.Rotulo(130, 190, "para salir del paso,", 16, Nucleo, 0, true); v.Rotulo(130, 214, "con fecha de caducidad", 16, Nucleo, 0, true);
                v.Forma(Caja(299, 30, 2, 250), A(Tinta, 0.12f));
                Calendario(v, 390, 46, 110, 96, CromoOscuro);
                NeonX(v, 528, 96, 20);
                v.Rotulo(450, 190, "para quedarse años,", 16, Coral, 0, true); v.Rotulo(450, 214, "heredas lo que no controlas", 16, Coral, 0, true);
            };
        }
    }
}
