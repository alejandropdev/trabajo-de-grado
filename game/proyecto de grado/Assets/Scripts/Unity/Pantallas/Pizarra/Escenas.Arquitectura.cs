using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>La arquitectura (q-…): que es, como se elige, y como es por dentro un monolito, unos microservicios o un parche.</summary>
    public static partial class DibujosDeEscena {
        /// <summary>Un modulo pequeño con su nombre.</summary>
        private static void Modulito(Tizador p, float x, float y, string t, Color? c = null) {
            p.Caja(x, y, 92, 46, t, c ?? Tizador.Cian, false, 15);
        }

        private static void Arquitectura(Dictionary<string, Action<Tizador>> d) {
            // ---------------------------------------------------------------- que es
            d["q-plano"] = p => {
                p.Caja(40, 50, 210, 170, null, Tizador.Gris); p.Linea(140, 50, 140, 150, Tizador.Gris); p.Linea(40, 150, 250, 150, Tizador.Gris);
                p.Texto(90, 106, "sala", 15, Tizador.Gris, true); p.Texto(195, 106, "cocina", 15, Tizador.Gris, true); p.Texto(145, 192, "pasillo", 15, Tizador.Gris, true);
                p.Texto(145, 256, "el plano de una casa", 17, Tizador.Gris, true);
                p.Texto(300, 140, "=", 40, Tizador.Mostaza, true);
                p.Caja(350, 50, 210, 170, null, Tizador.Cian); Modulito(p, 364, 66, "pantallas"); Modulito(p, 458, 66, "reglas"); Modulito(p, 411, 150, "datos", Tizador.Mostaza);
                p.Flecha(410, 112, 440, 148, Tizador.Tiza, 2); p.Flecha(504, 112, 474, 148, Tizador.Tiza, 2);
                p.Texto(455, 256, "el plano de un sistema", 17, Tizador.Cian, true);
            };
            d["q-piezas"] = p => {
                p.Navegador(30, 90, 130, 100, Tizador.Cian); p.Texto(95, 222, "lo que se ve", 16, Tizador.Cian, true);
                p.Flecha(170, 126, 240, 126, Tizador.Tiza); p.Texto(205, 110, "pide", 14, Tizador.Tiza, true);
                p.Flecha(240, 156, 170, 156, Tizador.Gris); p.Texto(205, 180, "responde", 14, Tizador.Gris, true);
                p.Servidor(250, 80, 100, 120, Tizador.Mostaza); p.Texto(300, 232, "lo que decide", 16, Tizador.Mostaza, true);
                p.Flecha(360, 140, 440, 140, Tizador.Tiza); p.Texto(400, 124, "guarda", 14, Tizador.Tiza, true);
                p.Cilindro(450, 90, 100, 100, Tizador.Tiza); p.Texto(500, 222, "lo que recuerda", 16, Tizador.Tiza, true);
                p.Texto(300, 40, "cada pieza hace una cosa, y se hablan por las flechas", 17, Tizador.Gris, true);
            };
            d["q-donde-corre"] = p => {
                p.Servidor(60, 70, 110, 130, Tizador.Cian); p.Texto(115, 232, "todo en una máquina", 16, Tizador.Cian, true); p.Texto(115, 256, "simple de cuidar", 14, Tizador.Gris, true);
                p.Linea(300, 30, 300, 280, Tizador.Gris, 2, true);
                p.Servidor(350, 50, 60, 76, Tizador.Mostaza); p.Servidor(440, 50, 60, 76, Tizador.Mostaza); p.Servidor(395, 150, 60, 76, Tizador.Mostaza);
                p.Linea(410, 90, 440, 90, Tizador.Tiza, 2); p.Linea(385, 128, 410, 150, Tizador.Tiza, 2); p.Linea(465, 128, 440, 150, Tizador.Tiza, 2);
                p.Texto(450, 256, "repartido en varias", 16, Tizador.Mostaza, true); p.Texto(450, 280, "aguanta más, cuesta más cuidarlo", 14, Tizador.Gris, true);
            };
            d["q-elegir"] = p => {
                p.Figura(60, 40, Tizador.Cian); p.Figura(100, 40, Tizador.Cian); p.Texto(80, 116, "el equipo", 16, Tizador.Cian, true);
                p.RelojDeArena(66, 150, 44, Tizador.Mostaza); p.Texto(80, 222, "el plazo", 16, Tizador.Mostaza, true);
                p.Hoja(230, 196, 56, 70, Tizador.Tiza); p.Texto(258, 290, "el encargo", 16, Tizador.Tiza, true);
                p.Flecha(130, 76, 400, 136, Tizador.Gris, 2); p.Flecha(130, 172, 400, 156, Tizador.Gris, 2); p.Flecha(292, 226, 400, 176, Tizador.Gris, 2);
                p.Caja(410, 110, 160, 90, "la arquitectura", Tizador.Mostaza, false, 18);
                p.Texto(490, 230, "sale de esas tres cosas,", 15, Tizador.Tiza, true); p.Texto(490, 252, "no de la moda", 15, Tizador.Roja, true);
            };
            d["q-mal-elegida"] = p => {
                for (var i = 0; i < 5; i++) { p.Caja(40 + (i % 3) * 90, 40 + (i / 3) * 80, 70, 54, null, Tizador.Roja); p.Engranaje(75 + (i % 3) * 90, 67 + (i / 3) * 80, 14, Tizador.Roja); }
                p.Sirena(250, 140, Tizador.Roja);
                p.Carita(450, 110, 30, -1, Tizador.Roja);
                p.Texto(450, 200, "tres personas para", 16, Tizador.Tiza, true); p.Texto(450, 224, "cinco cosas que vigilar", 16, Tizador.Roja, true);
                p.Texto(300, 276, "no era mala: era demasiado para este equipo", 16, Tizador.Mostaza, true);
            };
            d["q-cambiar-cuesta"] = p => {
                p.Edificio(60, 50, 120, 170, Tizador.Cian); p.Texto(120, 250, "todo lo construido", 15, Tizador.Cian, true); p.Texto(120, 272, "se apoya en ella", 15, Tizador.Cian, true);
                p.Flecha(210, 140, 330, 140, Tizador.Roja); p.Texto(270, 122, "cambiarla", 16, Tizador.Roja, true);
                p.Edificio(360, 50, 120, 170, Tizador.Gris); p.No(420, 130, null, 34); p.LlaveInglesa(510, 120, Tizador.Roja);
                p.Texto(440, 262, "es rehacer los cimientos:", 15, Tizador.Tiza, true); p.Texto(440, 284, "por eso se piensa al principio", 15, Tizador.Mostaza, true);
            };

            // ---------------------------------------------------------------- monolito modular
            d["q-mono-dentro"] = p => {
                p.Caja(110, 40, 380, 190, null, Tizador.Cian);
                Modulito(p, 130, 60, "reservas"); Modulito(p, 250, 60, "turnos"); Modulito(p, 370, 60, "acceso"); Modulito(p, 190, 150, "panel"); Modulito(p, 310, 150, "informes");
                p.Linea(240, 50, 240, 130, Tizador.Gris, 2, true); p.Linea(360, 50, 360, 130, Tizador.Gris, 2, true); p.Linea(120, 130, 480, 130, Tizador.Gris, 2, true);
                p.Texto(300, 258, "un solo programa", 18, Tizador.Cian, true); p.Texto(300, 284, "con paredes por dentro: cada módulo, lo suyo", 15, Tizador.Gris, true);
            };
            d["q-mono-viaje"] = p => {
                p.Figura(40, 100, Tizador.Tiza); p.Flecha(80, 130, 140, 130, Tizador.Mostaza); p.Texto(110, 112, "pide", 14, Tizador.Mostaza, true);
                p.Caja(150, 60, 280, 140, null, Tizador.Cian); p.Caja(150, 100, 14, 60, null, Tizador.Mostaza);   // la unica puerta
                Modulito(p, 180, 80, "reservas"); Modulito(p, 310, 80, "turnos");
                p.Flecha(274, 103, 308, 103, Tizador.Tiza, 2); p.Texto(291, 150, "se hablan por dentro,", 14, Tizador.Gris, true); p.Texto(291, 170, "sin red de por medio", 14, Tizador.Gris, true);
                p.Flecha(440, 130, 490, 130, Tizador.Tiza); p.Cilindro(500, 90, 80, 84, Tizador.Tiza); p.Texto(540, 200, "una base", 14, Tizador.Tiza, true);
                p.Texto(300, 250, "una puerta de entrada, un camino corto", 16, Tizador.Cian, true);
            };
            d["q-mono-despliegue"] = p => {
                p.Caja(60, 90, 150, 110, null, Tizador.Cian); for (var i = 0; i < 4; i++) p.Caja(76 + (i % 2) * 64, 104 + (i / 2) * 46, 54, 36, null, Tizador.Tiza);
                p.Flecha(230, 145, 360, 145, Tizador.Mostaza); p.Texto(295, 126, "un despliegue", 17, Tizador.Mostaza, true);
                p.Servidor(380, 80, 110, 130, Tizador.Cian); p.Si(530, 140, null, 18);
                p.Texto(135, 232, "sale todo junto", 16, Tizador.Cian, true); p.Texto(435, 240, "una sola cosa que vigilar", 16, Tizador.Cian, true);
            };
            d["q-mono-sin-modulos"] = p => {
                p.Caja(40, 50, 210, 160, null, Tizador.Roja); p.Telarana(145, 130, 60, Tizador.Roja);
                p.Texto(145, 240, "sin paredes:", 16, Tizador.Roja, true); p.Texto(145, 264, "todo toca todo", 16, Tizador.Roja, true);
                p.Linea(300, 30, 300, 280, Tizador.Gris, 2, true);
                p.Caja(350, 50, 210, 160, null, Tizador.Cian); Modulito(p, 362, 66, "A"); Modulito(p, 458, 66, "B"); Modulito(p, 410, 144, "C");
                p.Texto(455, 240, "con módulos:", 16, Tizador.Cian, true); p.Texto(455, 264, "cambias uno sin romper otro", 16, Tizador.Cian, true);
            };

            // ---------------------------------------------------------------- microservicios
            d["q-micro-dentro"] = p => {
                var nombres = new[] { "reservas", "turnos", "acceso", "informes" };
                for (var i = 0; i < 4; i++) {
                    var x = 40 + i * 140;
                    p.Caja(x, 50, 110, 60, nombres[i], Tizador.Mostaza, false, 15);
                    p.Linea(x + 55, 110, x + 55, 140, Tizador.Gris, 2); p.Cilindro(x + 25, 140, 60, 60, Tizador.Tiza);
                }
                p.Texto(300, 236, "cada servicio es un programa aparte, con su propia base", 16, Tizador.Mostaza, true);
                p.Texto(300, 262, "se despliegan y se caen por separado", 15, Tizador.Gris, true);
            };
            d["q-micro-viaje"] = p => {
                p.Figura(40, 100, Tizador.Tiza); p.Flecha(80, 130, 130, 130, Tizador.Mostaza);
                p.Caja(140, 100, 100, 60, "reservas", Tizador.Mostaza, false, 15);
                p.Nube(300, 130, 26, Tizador.Gris); p.Flecha(244, 130, 272, 130, Tizador.Tiza, 2); p.Flecha(330, 130, 358, 130, Tizador.Tiza, 2);
                p.Caja(362, 100, 100, 60, "turnos", Tizador.Mostaza, false, 15);
                p.Nube(500, 130, 22, Tizador.Roja); p.No(500, 130, null, 12); p.Caja(530, 104, 60, 52, "…", Tizador.Roja, true, 18);
                p.Texto(300, 196, "entre servicio y servicio hay red", 16, Tizador.Tiza, true);
                p.Texto(300, 222, "y la red puede fallar a mitad de camino", 16, Tizador.Roja, true);
                p.Texto(300, 264, "cada salto hay que vigilarlo y reintentarlo", 15, Tizador.Gris, true);
            };
            d["q-micro-despliegue"] = p => {
                for (var i = 0; i < 4; i++) {
                    var y = 40 + i * 56;
                    p.Caja(40, y, 90, 40, null, Tizador.Mostaza); Tramo(p, 160, y + 20, 200, null, Tizador.Mostaza); p.Flecha(370, y + 20, 420, y + 20, Tizador.Mostaza, 2);
                    p.Servidor(430, y - 2, 40, 44, Tizador.Cian);
                }
                p.Texto(260, 284, "cuatro servicios, cuatro despliegues, cuatro cosas que vigilar", 16, Tizador.Mostaza, true);
            };
            d["q-micro-equipo"] = p => {
                for (var i = 0; i < 3; i++) {
                    var x = 50 + i * 130;
                    p.Caja(x, 40, 100, 50, null, Tizador.Mostaza); p.Figura(x + 30, 110, Tizador.Cian); p.Figura(x + 70, 110, Tizador.Cian);
                }
                p.Texto(230, 200, "un equipo por servicio", 17, Tizador.Cian, true);
                p.Sirena(500, 60, Tizador.Roja); p.Figura(500, 120, Tizador.Roja); p.Texto(500, 200, "y alguien de guardia", 17, Tizador.Roja, true);
                p.Texto(300, 252, "sirve cuando hay mucha gente y mucho uso;", 16, Tizador.Tiza, true); p.Texto(300, 276, "con cuatro personas, se come al equipo", 16, Tizador.Mostaza, true);
            };

            // ---------------------------------------------------------------- parche sobre lo existente
            d["q-parche-dentro"] = p => {
                p.Caja(70, 70, 230, 150, null, Tizador.Gris); p.Candado(185, 130, 24, Tizador.Gris); p.Texto(185, 250, "lo que ya había:", 16, Tizador.Gris, true); p.Texto(185, 274, "nadie tiene el código", 16, Tizador.Roja, true);
                p.Caja(250, 40, 150, 80, "lo nuevo", Tizador.Mostaza, false, 18);
                p.Flecha(410, 80, 470, 80, Tizador.Mostaza); p.Texto(520, 70, "se pega", 16, Tizador.Mostaza, true); p.Texto(520, 94, "encima", 16, Tizador.Mostaza, true);
                p.Texto(500, 200, "arranca rápido:", 16, Tizador.Cian, true); p.Texto(500, 224, "algo ya funciona", 16, Tizador.Cian, true);
            };
            d["q-parche-riesgo"] = p => {
                p.Caja(90, 190, 180, 60, null, Tizador.Gris); p.Texto(180, 226, "?", 30, Tizador.Gris, true);
                p.Caja(110, 140, 150, 46, null, Tizador.Mostaza); p.Caja(130, 94, 130, 42, null, Tizador.Mostaza); p.Caja(160, 52, 110, 38, null, Tizador.Roja);
                p.Linea(60, 252, 300, 252, Tizador.Tiza); p.Curva(280, 60, 320, 40, 340, 70, Tizador.Roja);
                p.Texto(450, 90, "parche sobre parche:", 17, Tizador.Mostaza, true); p.Texto(450, 116, "cada uno se apoya en algo", 15, Tizador.Tiza, true); p.Texto(450, 138, "que nadie entiende", 15, Tizador.Tiza, true);
                p.Bicho(420, 200, Tizador.Roja); p.Texto(490, 206, "y un día se cae", 16, Tizador.Roja);
            };
            d["q-parche-cuando"] = p => {
                p.RelojDeArena(70, 60, 80, Tizador.Mostaza); p.Si(170, 100, null, 18);
                p.Texto(130, 190, "para salir del paso,", 16, Tizador.Cian, true); p.Texto(130, 214, "con fecha de caducidad", 16, Tizador.Cian, true);
                p.Linea(300, 30, 300, 280, Tizador.Gris, 2, true);
                p.Calendario(390, 50, 110, 90, Tizador.Gris); p.No(520, 96, null, 18);
                p.Texto(450, 190, "para quedarse años,", 16, Tizador.Roja, true); p.Texto(450, 214, "heredas lo que no controlas", 16, Tizador.Roja, true);
            };
        }
    }
}
