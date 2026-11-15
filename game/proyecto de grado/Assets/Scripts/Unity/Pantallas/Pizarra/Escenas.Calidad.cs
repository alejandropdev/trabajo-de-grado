using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;
using L = Nexus.Unity.Tema.LuzUI;
using Nexus.Unity.Tema;

namespace Nexus.Unity.Pantallas {
    /// <summary>«¿A qué le dedicas tu cuidado?»: cada atributo de calidad, cuando esta (q-…-que) y cuando falta (q-…-falta).</summary>
    public static partial class DibujosDeEscena {
        private static void Calidad(Dictionary<string, Action<Tizador>> d) {
            d["q-calidad-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 150, Nucleo, 0.7f, 301);
                var nombres = new[] { "usable", "fiable", "seguro", "fácil de cambiar", "rápido", "hace lo pedido" };
                var valor = new[] { 0.9f, 0.5f, 0.7f, 0.35f, 0.6f, 0.95f };
                var borde = new List<Vector2>(); var forma = new List<Vector2>();
                for (var i = 0; i < 6; i++) {
                    var a = Mathf.PI / 3 * i - Mathf.PI / 2; var dx = Mathf.Cos(a); var dy = Mathf.Sin(a);
                    v.Forma(L.Tubo(Pts(300, 150, 300 + dx * 110, 150 + dy * 110), 1.2f, 1.2f), A(Borde, 0.9f));
                    v.Rotulo(300 + dx * 170, 152 + dy * 128, nombres[i], 16, Tinta);
                    borde.Add(new Vector2(300 + dx * 110, 150 + dy * 110));
                    forma.Add(new Vector2(300 + dx * 110 * valor[i], 150 + dy * 110 * valor[i]));
                }
                Contorno(v, borde, 1.6f, A(Borde, 0.9f));
                v.Forma(forma, A(Nucleo, 0.16f));
                Contorno(v, forma, 1.4f, Nucleo, true);
            };

            d["q-adecuacion-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 303);
                Papel(v, 50, 40, 200, 200, "lo que se pidió");
                PanelUI(v, 350, 40, 200, 200, "lo que hace");
                var pedido = new[] { "registrar", "avisar", "quién sigue" };
                for (var i = 0; i < 3; i++) {
                    var y = 98 + i * 50;
                    v.Forma(Caja(60, y - 12, 180, 26), H(0xEDE7D6));
                    v.Rotulo(72, y, pedido[i], 16, H(0x1E2A2A), -1);
                    FlechaNeon(v, 256, y, 344, y, TintaSuave);
                    Pieza(v, 368, y - 16, 118, 32, null, Nucleo);
                    NeonCheck(v, 520, y, 16);
                }
                v.Rotulo(300, 276, "todo lo pedido, completo y con las cuentas bien", 17, Tinta);
            };
            d["q-adecuacion-falta"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Coral, 0.5f, 305);
                PanelUI(v, 60, 44, 230, 180);
                BotonUI(v, 84, 82, 182, 40, "con cita", Nucleo);
                v.Forma(L.Redondeado(84, 146, 182, 40, 20), A(Coral, 0.08f)); v.Rotulo(175, 166, "sin cita", 16, A(Coral, 0.5f), 0, true);
                NeonX(v, 278, 166, 16);
                Credencial(v, 440, 90, Tinta, "llega sin cita");
                v.Rotulo(440, 34, "?", 34, Coral, 0, false, true);
                v.Rotulo(300, 268, "muy bonito, pero no hace lo que se necesitaba", 18, Coral);
            };

            d["q-usabilidad-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 210, 130, Aviso, 0.5f, 307);
                PanelUI(v, 80, 40, 260, 190);
                BotonUI(v, 120, 108, 180, 60, "Registrar", Aviso, true);
                Credencial(v, 460, 76, Nucleo);
                NeonCheck(v, 520, 70, 18);
                v.Rotulo(460, 180, "lo usa a la primera,", 17, Aviso, 0, true);
                v.Rotulo(460, 204, "sin manual", 17, Aviso, 0, true);
            };
            d["q-usabilidad-falta"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Coral, 0.4f, 309);
                PanelUI(v, 40, 40, 250, 200);
                for (var f = 0; f < 4; f++) for (var k = 0; k < 6; k++) Pieza(v, 54 + k * 38, 64 + f * 40, 30, 26, null, TintaSuave);
                Credencial(v, 350, 86, Coral);
                v.Rotulo(350, 40, "?", 34, Coral, 0, false, true);
                for (var i = 0; i < 4; i++) Credencial(v, 420 + i * 44, 104, TintaSuave);
                v.Rotulo(490, 214, "y la fila creciendo", 17, Tinta, 0, true);
                v.Rotulo(165, 272, "¿cuál era el botón?", 18, Coral);
            };

            d["q-fiabilidad-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 135, 130, Menta, 0.5f, 311);
                Icono(v.En(55, 50, 1.1f), "servidor", Tizador.Tiza);
                NeonCheck(v, 135, 222, 18);
                PanelUI(v, 250, 54, 290, 140);
                v.Neon(Pts(270, 90, 380, 90, 392, 162, 404, 90, 520, 90), 1.5f, Menta);
                v.Rotulo(395, 40, "funciona siempre", 17, Menta, 0, true);
                v.Rotulo(395, 226, "y si se cae, se levanta solo y rápido", 16, Aviso);
            };
            d["q-fiabilidad-falta"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 125, 130, Coral, 0.6f, 313);
                Icono(v.En(45, 56, 1.1f), "servidor", Tizador.Tiza);
                NeonX(v, 125, 126, 34);
                Reloj(v, 270, 90, 38, 9, 0, Coral);
                v.Rotulo(270, 160, "a las 9:00,", 17, Coral, 0, true); v.Rotulo(270, 184, "con la fila llena", 17, Coral, 0, true);
                FlechaNeon(v, 346, 130, 408, 130, Tinta);
                Papel(v, 430, 60, 110, 150, "cuaderno", null, 4);
                v.Rotulo(485, 244, "vuelve el papel", 17, Tinta, 0, true);
            };

            d["q-seguridad-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Aviso, 0.5f, 315);
                v.Forma(L.Redondeado(240, 40, 120, 200, 12), H(0x46555A), H(0x161D20));
                v.Forma(L.Redondeado(250, 50, 100, 180, 8), H(0x2B3335), H(0x101416));
                Icono(v.En(220, 74, 1f), "candado", Tizador.Mostaza);
                Credencial(v, 100, 70, Nucleo); Llave(v, 132, 140, 40, -20);
                NeonCheck(v, 100, 166, 14);
                v.Rotulo(110, 204, "quien debe, entra", 16, Nucleo, 0, true);
                Credencial(v, 490, 70, Coral); NeonX(v, 490, 166, 14);
                v.Rotulo(490, 204, "quien no, no", 16, Coral, 0, true);
                v.Rotulo(300, 276, "y los datos de adentro no salen", 18, Tinta);
            };
            d["q-seguridad-falta"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 290, 140, Coral, 0.6f, 317);
                v.Forma(L.Redondeado(220, 40, 120, 200, 12), H(0x46555A), H(0x161D20));
                v.Forma(L.Redondeado(230, 50, 100, 180, 8), H(0x05090A));
                v.Halo(280, 140, 60, A(Coral, 0.25f));
                v.Forma(Pts(330, 50, 366, 34, 366, 244, 330, 230), H(0x3A464B), H(0x161D20));
                Credencial(v, 110, 92, Coral, "cualquiera");
                FlechaNeon(v, 148, 130, 222, 130, Coral);
                FlechaNeon(v, 372, 130, 424, 130, Coral);
                Papel(v, 440, 80, 90, 110, "datos", Coral, -6);
                Chispa(v, 470, 70, 7, 0.2f); Chispa(v, 540, 120, 6, 0.6f);
                v.Rotulo(485, 222, "se filtran", 17, Coral, 0, true);
                v.Rotulo(300, 276, "no avisa: un día alguien ya entró", 18, Coral);
            };

            d["q-mantenibilidad-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 270, 110, Aviso, 0.5f, 319);
                Pieza(v, 60, 60, 120, 70, "registro", Nucleo, 18);
                v.Forma(L.Redondeado(210, 60, 120, 70, 8), A(Silueta, 0.5f));
                Pieza(v, 216, 30, 120, 70, "aviso", Aviso, 18);
                v.Halo(276, 64, 80, 40, A(Aviso, 0.25f));
                Pieza(v, 360, 60, 120, 70, "lista", Nucleo, 18);
                Llave(v, 230, 150, 50, -30);
                v.Rotulo(270, 220, "se cambia una pieza", 17, Aviso, 0, true);
                v.Rotulo(270, 244, "sin tocar las demás", 17, Aviso, 0, true);
                Credencial(v, 530, 120, Tinta, "otra persona");
                NeonCheck(v, 530, 66, 14);
            };
            d["q-mantenibilidad-falta"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 200, 140, Coral, 0.5f, 321);
                Maraña(v, 90, 60, 230, 170, Coral, 7);
                Pieza(v, 60, 40, 90, 50, null, TintaSuave); Pieza(v, 250, 30, 90, 50, null, TintaSuave);
                Pieza(v, 90, 190, 90, 50, null, TintaSuave); Pieza(v, 270, 200, 90, 50, null, TintaSuave);
                Credencial(v, 480, 96, Tinta);
                v.Rotulo(480, 48, "?", 34, Coral, 0, false, true);
                v.Rotulo(480, 214, "nadie se atreve", 17, Coral, 0, true); v.Rotulo(480, 238, "a tocarlo", 17, Coral, 0, true);
            };

            d["q-rendimiento-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Nucleo, 0.5f, 323);
                v.Forma(L.Redondeado(-10, 198, 620, 8, 3), Color.Lerp(SiluetaFuera, Ciruela, 0.6f), SiluetaFuera);
                v.Neon(Pts(0, 198, 600, 198), 1, Nucleo);
                Barrera(v, 300, 198, true, Menta);
                Capsula(v, 120, 198, 90, Nucleo, true);
                Capsula(v, 420, 198, 90, Nucleo, true);
                Reloj(v, 110, 70, 30, 0, 0, Aviso);
                v.Rotulo(190, 74, "1 segundo", 18, Aviso, -1, true);
                v.Rotulo(300, 252, "responde rápido, también con mucha carga", 18, Tinta);
            };
            d["q-rendimiento-falta"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Coral, 0.5f, 325);
                v.Forma(L.Redondeado(-10, 198, 620, 8, 3), Color.Lerp(SiluetaFuera, Ciruela, 0.6f), SiluetaFuera);
                v.Neon(Pts(0, 198, 600, 198), 1, A(Coral, 0.6f));
                for (var i = 0; i < 4; i++) Capsula(v, 14 + i * 104, 198, 90, i == 3 ? Coral : TintaSuave, false);
                Barrera(v, 460, 198, false, Coral);
                RelojDeArena(v, 530, 40, 80, Coral, 0.35f);
                v.Rotulo(380, 70, "10 segundos cada uno", 18, Coral, 0, true);
                v.Rotulo(300, 252, "lento en la hora punta es como roto", 18, Coral);
            };
        }
    }
}
