using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using UnityEngine;
using L = Nexus.Unity.Tema.LuzUI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El curso de Scrum (sc-…), contado en una cocina: el Dueño del menú es el Product Owner, el Coach es el Scrum
    /// Master, los cocineros son los Developers y cada plato servido es un incremento.
    /// (2026-11-15, direccion de arte) La cocina del bunker: los cocineros son su gorro, el Dueño del menú y el Coach sus
    /// credenciales (mostaza y cian), los pedidos sus comandas (PiezasDeEscena.Cocina).
    /// </summary>
    public static partial class DibujosDeEscena {
        private static void Scrum(Dictionary<string, Action<Tizador>> d) {
            d["sc-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 400, 130, Ambar, 0.6f, 801);
                Papel(v, 50, 20, 130, 150, "Banquete", Coral);
                Gorro(v, 115, 230, Tinta);
                v.Rotulo(115, 274, "un pedido enorme", 15, Coral);
                FlechaNeon(v, 200, 150, 266, 150, Aviso);
                for (var i = 0; i < 4; i++) PlatoTapado(v, 300 + i * 56, 140 + i * 22, 22, Nucleo);
                FlechaNeon(v, 506, 210, 540, 210, Nucleo);
                NeonCheck(v, 566, 196, 20);
                v.Rotulo(400, 66, "cuatro bandejas pequeñas,", 15, Nucleo); v.Rotulo(400, 88, "una por una", 15, Nucleo);
                v.Rotulo(450, 274, "algo útil en cada ciclo", 15, Nucleo);
            };
            d["sc-origen"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Ambar, 0.6f, 803);
                for (var i = 0; i < 3; i++) Credencial(v, 66 + i * 40, 90 + i * 14, Tinta);
                v.Forma(L.Elipse(200, 190, 24, 15, 24), H(0x8A5A2E), H(0x4A2E14)); v.Forma(Caja(186, 188, 28, 1.6f), A(Color.white, 0.6f));
                v.Rotulo(130, 250, "la melé del rugby", 16, Tinta); v.Rotulo(130, 272, "(scrum, en inglés)", 14, TintaSuave);
                FlechaNeon(v, 250, 150, 318, 150, Aviso);
                for (var i = 0; i < 3; i++) Gorro(v, 350 + i * 34, 140 + i * 14, Nucleo, 0.9f);
                Olla(v, 466, 210, 90, 64, 0.8f, Aviso);
                v.Rotulo(440, 260, "todos empujan juntos", 16, Nucleo);
            };
            d["sc-manifiesto"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 805);
                var mas = new[] { "personas", "plato servido", "colaborar", "responder al cambio" }; var menos = new[] { "procesos", "la receta en papel", "el contrato", "seguir el plan" };
                for (var i = 0; i < 4; i++) {
                    float x = 150 + (i % 2) * 300, y = 30 + (i / 2) * 140;
                    Balanza(v.En(x - 150, y - 40, 0.5f), 300, 70, 150, 236, -10);
                    v.Rotulo(x - 75, y + 64, mas[i], 15, Nucleo); v.Rotulo(x + 75, y + 46, menos[i], 13, TintaSuave);
                    v.Halo(x - 75, y + 34, 16, A(Nucleo, 0.6f)); v.Forma(L.Elipse(x - 75, y + 34, 9, 9, 16), Color.Lerp(Nucleo, Color.white, 0.3f), Nucleo);
                    v.Forma(L.Elipse(x + 75, y + 6, 5, 5, 12), A(TintaSuave, 0.8f));
                }
            };
            d["sc-pilares"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Aviso, 0.5f, 807);
                v.Forma(Pts(40, 80, 300, 14, 560, 80), Cromo, CromoOscuro);
                v.Forma(L.Redondeado(36, 246, 528, 12, 4), Cromo, CromoOscuro);
                var nombres = new[] { "transparencia", "inspección", "adaptación" };
                for (var i = 0; i < 3; i++) { v.Forma(Caja(80 + i * 170, 80, 100, 166), H(0x2B4F57), H(0x163038)); v.Rotulo(130 + i * 170, 280, nombres[i], 16, Aviso); }
                Espejo(v, 130, 140, 60, 70);
                Olla(v, 266, 214, 60, 44, 0.5f, Aviso, false); Lupa(v, 300, 132, 18, Aviso);
                Gorro(v, 470, 140, Nucleo, 0.9f); Olla(v, 440, 222, 60, 40, 0.8f, Nucleo, false);
            };
            d["sc-lean"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Coral, 0.5f, 809);
                for (var i = 0; i < 5; i++) Plato(v, 50 + (i % 3) * 60, 66 + (i / 3) * 48, 24, A(TintaSuave, 0.8f));
                FlechaNeon(v, 110, 140, 110, 182, Coral);
                v.Forma(Pts(80, 196, 140, 196, 134, 256, 86, 256), H(0x46555A), H(0x1A2428)); v.Forma(L.Redondeado(74, 188, 72, 10, 3), Cromo, CromoOscuro);
                Plato(v, 110, 196, 18, Coral);
                v.Rotulo(110, 282, "platos que nadie pidió", 15, Coral);
                FlechaNeon(v, 228, 150, 298, 150, Aviso);
                PlatoTapado(v, 380, 110, 24, Nucleo); PlatoTapado(v, 450, 110, 24, Nucleo); NeonCheck(v, 520, 90, 14);
                v.Forma(Pts(400, 196, 460, 196, 454, 256, 406, 256), H(0x46555A), H(0x1A2428)); v.Forma(L.Redondeado(394, 188, 72, 10, 3), Cromo, CromoOscuro);
                v.Rotulo(430, 282, "solo lo pedido: basura vacía", 15, Nucleo);
            };
            d["sc-valores"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 811);
                var nombres = new[] { "compromiso", "foco", "apertura", "respeto", "coraje" };
                for (var i = 0; i < 5; i++) { Gorro(v, 60 + i * 120, 196, Tinta, 0.9f); v.Rotulo(60 + i * 120, 236, nombres[i], 16, Aviso); }
                v.Neon(Pts(46, 96, 60, 112, 78, 92), 1.6f, Nucleo);
                Anillo(v, 180, 100, 20, 3, Nucleo); Anillo(v, 180, 100, 10, 3, Nucleo); v.Halo(180, 100, 10, A(Coral, 0.8f));
                v.Forma(L.Redondeado(282, 76, 16, 50, 3), H(0x46555A)); v.Forma(Pts(298, 76, 326, 86, 326, 116, 298, 126), A(Ambar, 0.7f)); v.Halo(306, 100, 28, A(Ambar, 0.35f));
                v.Forma(Tizador.Rotar(L.Redondeado(402, 76, 10, 48, 4), 407, 124, 20), Cromo); v.Forma(Tizador.Rotar(L.Redondeado(428, 76, 10, 48, 4), 433, 124, -20), Cromo);
                Bocadillo(v, 478, 72, 112, 40, "«¡se quemó!»", Coral, 540, 150, 14);
            };
            d["sc-equipo"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 150, Ambar, 0.6f, 813);
                v.Forma(L.Elipse(300, 172, 150, 60, 48), H(0x2B4F57), H(0x0B1E23));
                v.Forma(L.Arco(300, 172, 150, 60, 2, 190, 350, 28), A(Ambar, 0.5f));
                for (var i = 0; i < 7; i++) {
                    var a = Mathf.PI * 2 / 7 * i - Mathf.PI / 2; var x = 300 + Mathf.Cos(a) * 190; var y = 158 + Mathf.Sin(a) * 92;
                    if (i == 0) Credencial(v, x, y - 30, Aviso); else if (i == 1) Credencial(v, x, y - 30, Nucleo); else Gorro(v, x, y + 16, Tinta, 0.8f);
                }
                v.Rotulo(300, 160, "un solo equipo", 18, Tinta); v.Rotulo(300, 186, "nadie es jefe de nadie", 16, Aviso);
                v.Rotulo(264, 64, "Dueño del menú", 13, Aviso, 1, true); v.Rotulo(484, 100, "Coach", 13, Nucleo, -1, true); v.Rotulo(110, 282, "cocineros", 14, Tinta, 0, true);
            };
            d["sc-po"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 815);
                var pide = new[] { "sopa", "postre", "pizza" };
                for (var i = 0; i < 3; i++) { Credencial(v, 50, 20 + i * 90, Tinta); Bocadillo(v, 86, 16 + i * 90, 90, 32, pide[i], Tinta, 76, 50 + i * 90, 15); }
                Credencial(v, 290, 92, Aviso, "Dueño del menú");
                FlechaNeon(v, 326, 140, 392, 140, Aviso);
                for (var i = 0; i < 4; i++) TarjetaUI(v, 410, 30 + i * 58, 120, 46, i == 0 ? Aviso : Borde);
                FlechaNeon(v, 560, 246, 560, 40, Aviso);
                v.Rotulo(470, 284, "de más a menos importante", 14, Aviso);
            };
            d["sc-sm"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 380, 130, Nucleo, 0.5f, 817);
                Gorro(v, 60, 150, Tinta);
                v.Forma(Tizador.Rotar(L.Redondeado(110, 136, 110, 26, 12), 165, 149, -6), H(0x6B4B33), H(0x3A2A1E));
                Chispa(v, 160, 124, 6, 0.2f);
                v.Rotulo(160, 196, "un impedimento", 15, Coral);
                FlechaNeon(v, 252, 150, 318, 150, Aviso);
                Credencial(v, 380, 40, Nucleo, "Coach");
                v.Forma(Tizador.Rotar(L.Redondeado(330, 220, 100, 24, 12), 380, 232, 8), A(H(0x6B4B33), 0.6f), A(H(0x3A2A1E), 0.6f));
                v.Neon(Tizador.Bezier(new Vector2(390, 140), new Vector2(420, 190), new Vector2(400, 214), 16), 1, Nucleo);
                Gorro(v, 480, 150, Tinta);
                FlechaNeon(v, 510, 132, 580, 132, Nucleo);
                v.Rotulo(520, 196, "paso libre", 15, Nucleo);
            };
            d["sc-dev"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Ambar, 0.6f, 819);
                var hace = new[] { "corta", "cocina", "emplata" };
                for (var i = 0; i < 3; i++) { Gorro(v, 80 + i * 140, 100, Tinta); v.Rotulo(80 + i * 140, 128, hace[i], 16, Tinta); if (i < 2) FlechaNeon(v, 116 + i * 140, 80, 176 + i * 140, 80, TintaSuave); }
                v.Forma(L.Redondeado(46, 208, 70, 16, 3), H(0x6B4B33), H(0x3A2A1E));
                v.Forma(Pts(58, 200, 100, 200, 112, 208, 58, 208), Cromo, CromoOscuro);
                Olla(v, 190, 222, 60, 44, 0.8f, Aviso);
                Plato(v, 360, 214, 34, Aviso);
                FlechaNeon(v, 410, 110, 470, 110, Nucleo);
                PlatoTapado(v, 520, 150, 34, Nucleo);
                NeonCheck(v, 520, 184, 16);
                v.Rotulo(520, 232, "ellos deciden cómo", 15, Nucleo);
            };
            d["sc-ciclo"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 330, 150, Nucleo, 0.6f, 821);
                Comandas(v, 30, 60, 100, Aviso, Tinta, Tinta);
                FlechaNeon(v, 140, 110, 226, 136, Aviso);
                var anillo = new List<Vector2>();
                for (var k = 0; k < 40; k++) { var a = k / 40f * Mathf.PI * 2; anillo.Add(new Vector2(330 + Mathf.Cos(a) * 96, 150 + Mathf.Sin(a) * 96)); }
                Contorno(v, anillo, 1.4f, Nucleo, true);
                var nombres = new[] { "Planning", "Daily", "Review", "Retro" };
                for (var i = 0; i < 4; i++) {
                    var a = Mathf.PI / 2 * i - Mathf.PI / 2; var x = 330 + Mathf.Cos(a) * 96; var y = 150 + Mathf.Sin(a) * 96;
                    v.Halo(x, y, 28, A(Nucleo, 0.4f));
                    v.Forma(L.Elipse(x, y, 18, 18, 24), SuperficieAlta, Superficie); Anillo(v, x, y, 18, 2.4f, Nucleo);
                    v.Rotulo(x + (i == 1 ? 64 : i == 3 ? -64 : 0), y + (i == 0 ? -30 : i == 2 ? 32 : 0), nombres[i], 15, Nucleo);
                }
                PlatoTapado(v, 330, 172, 30, Aviso);
                v.Rotulo(520, 282, "y vuelta a empezar", 15, Tinta);
            };
            d["sc-backlog"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 280, 140, Aviso, 0.5f, 823);
                v.Forma(L.Redondeado(186, 6, 188, 8, 4), Cromo, CromoOscuro);
                v.Forma(L.Redondeado(192, 14, 176, 270, 4), H(0xF2EAD2), H(0xCFC5A5));
                TarjetaUI(v, 204, 24, 152, 56, Nucleo); TarjetaUI(v, 204, 88, 152, 48, Nucleo); TarjetaUI(v, 204, 144, 152, 34, Borde);
                foreach (var (y, h) in new[] { (188f, 24f), (218f, 18f), (242f, 14f) }) v.Forma(L.Redondeado(204, y, 152, h, 4), A(H(0x1E2A2A), 0.18f));
                Credencial(v, 70, 66, Aviso, "Dueño del menú");
                v.Forma(L.Redondeado(40, 190, 80, 40, 4), H(0xF2EAD2), H(0xD9CFB0)); v.Forma(Caja(40, 190, 80, 6), Aviso); v.Rotulo(80, 214, "nueva", 13, H(0x1E2A2A));
                v.Neon(Tizador.Bezier(new Vector2(122, 200), new Vector2(160, 160), new Vector2(196, 170), 16), 1, Aviso);
                v.Rotulo(480, 60, "arriba: grande y clara", 15, Nucleo); v.Rotulo(480, 230, "abajo: borrosa", 15, TintaSuave); v.Rotulo(480, 150, "nunca se termina", 16, Tinta);
            };
            d["sc-goal"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 460, 110, Sol, 0.6f, 825);
                Comandas(v, 50, 60, 110, Tinta, Tinta, Tinta);
                FlechaNeon(v, 180, 140, 330, 140, Aviso);
                v.Forma(Caja(340, 198, 240, 1.4f), A(TintaSuave, 0.6f));
                v.Halo(460, 110, 70, A(Sol, 0.4f)); v.FormaDeLuz(Estrella(460, 110, 50), A(Sol, 0.95f)); v.Forma(Estrella(460, 110, 20), BombillaCentro);
                v.Rotulo(460, 236, "«Restaurante con 5 estrellas»", 16, Aviso);
                v.Rotulo(105, 270, "la lista", 15, Tinta); v.Rotulo(460, 270, "a dónde quiere llegar", 15, TintaSuave);
            };
            d["sc-historia"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 827);
                PanelUI(v, 60, 10, 480, 186);
                var rotulo = new[] { "Como…", "quiero…", "para…" }; var pie = new[] { "quién", "qué", "por qué" };
                for (var i = 0; i < 3; i++) { v.Forma(L.Redondeado(80 + i * 154, 50, 132, 110, 8), Pozo); v.Rotulo(146 + i * 154, 36, rotulo[i], 16, Aviso); v.Rotulo(146 + i * 154, 178, pie[i], 14, TintaSuave); }
                Credencial(v, 146, 92, Nucleo);
                Olla(v, 248, 140, 52, 40, 0.6f, Nucleo, false);
                Corazon(v, 454, 104, 22, Coral);
                var criterios = new[] { "caliente", "sin sal", "en 10 min" };
                for (var i = 0; i < 3; i++) { NeonCheck(v, 120 + i * 170, 238, 12); v.Rotulo(186 + i * 170, 240, criterios[i], 16, Nucleo); }
                v.Rotulo(300, 280, "criterios de aceptación", 14, TintaSuave);
            };
            d["sc-refinar"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 829);
                Papel(v, 40, 80, 150, 130, "Banquete", Coral);
                FlechaNeon(v, 196, 145, 250, 145, Aviso);
                v.Forma(L.Redondeado(264, 140, 26, 14, 3), H(0x6B4B33), H(0x3A2A1E)); v.Forma(Pts(290, 138, 340, 138, 352, 152, 290, 152), Cromo, CromoOscuro);
                FlechaNeon(v, 364, 145, 404, 145, Aviso);
                var trozos = new[] { "entrada", "sopa", "plato fuerte", "postre" };
                for (var i = 0; i < 4; i++) Pieza(v, 420, 30 + i * 62, 150, 48, trozos[i], Nucleo, 15);
                v.Rotulo(115, 240, "demasiado grande", 15, Coral);
            };
            d["sc-estimar"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 831);
                var antes = new[] { "3", "5", "5", "13" };
                for (var i = 0; i < 4; i++) {
                    Gorro(v, 50 + i * 68, 100, Tinta, 0.8f);
                    v.Forma(L.Redondeado(30 + i * 68, 140, 40, 56, 5), H(0xF2EAD2), H(0xD9CFB0)); v.Rotulo(50 + i * 68, 168, antes[i], 20, i == 3 ? Coral : H(0x1E2A2A), 0, false, true);
                }
                Bocadillo(v, 204, 8, 120, 34, "«falta el horno»", Coral, 254, 76, 12);
                FlechaNeon(v, 306, 168, 362, 168, Aviso);
                for (var i = 0; i < 4; i++) { v.Halo(400 + i * 52, 138, 26, A(Nucleo, 0.25f)); v.Forma(L.Redondeado(380 + i * 52, 110, 40, 56, 5), H(0xF2EAD2), H(0xD9CFB0)); v.Rotulo(400 + i * 52, 138, "5", 20, H(0x1E4A5A), 0, false, true); }
                v.Rotulo(150, 236, "todos muestran a la vez", 15, Tinta);
                v.Rotulo(480, 200, "hablan, y se ponen", 15, Nucleo); v.Rotulo(480, 222, "de acuerdo", 15, Nucleo);
            };
            d["sc-sprint"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 250, 150, Nucleo, 0.6f, 833);
                var anillo = new List<Vector2>();
                for (var k = 0; k < 40; k++) { var a = k / 40f * Mathf.PI * 2; anillo.Add(new Vector2(250 + Mathf.Cos(a) * 120, 150 + Mathf.Sin(a) * 120)); }
                Contorno(v, anillo, 1.2f, A(Nucleo, 0.6f), true);
                RelojDeArena(v, 250, 92, 116, Aviso, 0.5f);
                Papel(v, 140, 70, 30, 40); Reloj(v, 360, 90, 14, 9, 0, Nucleo); Plato(v, 360, 214, 20, Nucleo); Espejo(v, 150, 208, 24, 32);
                Bandera(v, 470, 196, 90, Aviso);
                Icono(v.En(468, 60, 0.45f), "candado", Tizador.Roja);
                v.Rotulo(500, 222, "el objetivo", 16, Aviso); v.Rotulo(500, 244, "no se toca", 16, Coral);
                v.Rotulo(250, 290, "un mes o menos, siempre igual de largo", 15, Tinta);
            };
            d["sc-cancelar"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Coral, 0.5f, 835);
                Bocadillo(v, 20, 14, 270, 42, "«el cliente ya no quiere pizza»", Coral, 150, 90, 14);
                Credencial(v, 150, 100, Aviso, "Dueño del menú");
                v.Halo(243, 115, 40, A(Coral, 0.4f));
                v.Forma(L.Redondeado(220, 92, 46, 46, 10), Coral); v.Rotulo(243, 115, "ALTO", 12, H(0x2A0B06), 0, true);
                RelojDeArena(v, 460, 66, 130, TintaSuave, 0.5f);
                NeonX(v, 460, 132, 44);
                v.Rotulo(300, 272, "solo él puede cancelarlo, si el objetivo ya no sirve", 15, Tinta);
            };
            d["sc-planning"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Ambar, 0.7f, 837);
                v.Forma(L.Elipse(300, 160, 270, 100, 48), CostillaCerca, PrimerPlano);
                v.Forma(L.Arco(300, 160, 270, 100, 2, 190, 350, 28), A(Ambar, 0.5f));
                for (var y = 76f; y < 248; y += 14) { v.Forma(Caja(209, y, 2, 7), A(Borde, 0.9f)); v.Forma(Caja(389, y, 2, 7), A(Borde, 0.9f)); }
                Corazon(v, 120, 136, 26, Coral); v.Rotulo(120, 210, "¿por qué?", 17, Coral);
                TarjetaUI(v, 250, 104, 100, 34, Nucleo); TarjetaUI(v, 250, 146, 100, 34, Nucleo); v.Rotulo(300, 210, "¿qué?", 17, Nucleo);
                for (var i = 0; i < 3; i++) { v.Halo(420, 112 + i * 26, 8, A(Aviso, 0.7f)); v.Forma(L.Redondeado(434, 110 + i * 26, 76, 4, 2), A(Aviso, 0.8f)); }
                v.Rotulo(470, 210, "¿cómo?", 17, Aviso);
                v.Rotulo(300, 24, "una mesa, tres preguntas", 16, Tinta);
            };
            d["sc-sprint-goal"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 160, Aviso, 0.6f, 839);
                Plato(v, 300, 214, 72, new Color(0, 0, 0, 0));
                Bandera(v, 300, 210, 120, Aviso);
                v.Rotulo(300, 40, "«Menú de desayunos listo»", 17, Aviso);
                Gorro(v, 60, 130, Tinta, 0.8f); FlechaNeon(v, 90, 130, 230, 176, Nucleo);
                Gorro(v, 540, 130, Tinta, 0.8f); FlechaNeon(v, 510, 130, 374, 176, Nucleo);
                Gorro(v, 110, 256, Tinta, 0.8f); FlechaNeon(v, 140, 240, 226, 220, Nucleo);
                Gorro(v, 500, 256, Tinta, 0.8f); FlechaNeon(v, 470, 240, 380, 220, Nucleo);
            };
            d["sc-sprint-backlog"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Nucleo, 0.5f, 841);
                PanelUI(v, 150, 10, 300, 280);
                Bandera(v, 270, 70, 50, Aviso); v.Rotulo(380, 50, "el objetivo", 15, Aviso);
                v.Forma(Caja(166, 82, 268, 1.2f), A(Borde, 0.9f));
                for (var i = 0; i < 3; i++) {
                    TarjetaUI(v, 166, 96 + i * 62, 100, 50, Nucleo);
                    for (var k = 0; k < 3; k++) v.Forma(L.Redondeado(282 + k * 52, 102 + i * 62, 44, 38, 4), H(0xF2EAD2), H(0xD9CFB0));
                }
                v.Rotulo(75, 130, "lo elegido", 15, Nucleo); v.Rotulo(525, 130, "y el plan,", 15, Aviso); v.Rotulo(525, 152, "en tareas", 15, Aviso);
            };
            d["sc-daily"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 330, 120, Ambar, 0.6f, 843);
                float ancho; TableroUI(v, 40, 30, 160, 130, new[] { "", "", "" }, out ancho);
                TarjetaUI(v, 46, 56, 44, 26, Nucleo); TarjetaUI(v, 100, 56, 44, 26, Aviso);
                for (var i = 0; i < 4; i++) Gorro(v, 260 + i * 64, 176 + (i % 2) * 24, Tinta, 0.9f);
                Bocadillo(v, 330, 30, 190, 38, "«me bloquea el horno»", Coral, 360, 120, 14);
                Reloj(v, 110, 222, 34, 0, 15, Aviso);
                v.Rotulo(350, 262, "de pie, 15 minutos, todos los días", 16, Tinta);
            };
            d["sc-incremento"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 220, 130, Nucleo, 0.6f, 845);
                for (var i = 0; i < 3; i++) {
                    var c = i == 2 ? Aviso : Nucleo;
                    Plato(v, 220, 230 - i * 56, 90 - i * 14, c);
                    v.Rotulo(400, 230 - i * 56, "Sprint " + (i + 1), 18, c, -1);
                }
                FlechaNeon(v, 540, 250, 540, 90, Tinta);
                v.Rotulo(300, 40, "cada plato encaja sobre el anterior", 16, Tinta);
                NeonCheck(v, 90, 150, 22);
            };
            d["sc-dod"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Nucleo, 0.5f, 847);
                Plato(v, 70, 160, 44, Aviso);
                FlechaNeon(v, 124, 150, 180, 150, Aviso);
                v.Forma(L.Redondeado(196, 36, 220, 240, 4), A(Silueta, 0.45f)); v.Forma(L.Redondeado(190, 30, 220, 240, 4), H(0xEDE7D6), H(0xCFC7B0));
                var puntos = new[] { "temperatura", "presentación", "probado", "limpio" };
                for (var i = 0; i < 4; i++) { v.Rotulo(214, 80 + i * 50, puntos[i], 18, H(0x1E2A2A), -1); NeonCheck(v, 384, 80 + i * 50, 12, H(0x2E7A5A)); }
                FlechaNeon(v, 418, 150, 456, 150, Aviso);
                SelloTexto(v, 528, 150, "HECHO", Nucleo, 104);
            };
            d["sc-review"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 849);
                for (var i = 0; i < 2; i++) { Gorro(v, 50 + i * 70, 110, Tinta, 0.8f); PlatoTapado(v, 50 + i * 70, 190, 24, Nucleo); }
                FlechaNeon(v, 170, 150, 236, 150, Nucleo);
                Credencial(v, 290, 74, Menta); Credencial(v, 350, 74, TintaSuave); Credencial(v, 320, 140, Coral);
                v.Rotulo(320, 230, "opinan", 16, Tinta);
                FlechaNeon(v, 390, 150, 436, 150, Aviso);
                Credencial(v, 490, 30, Aviso);
                TarjetaUI(v, 450, 118, 110, 34, Aviso); TarjetaUI(v, 450, 160, 110, 34, Borde);
                v.Neon(Tizador.Bezier(new Vector2(572, 178), new Vector2(594, 156), new Vector2(572, 134), 12), 1, Aviso);
                v.Rotulo(500, 230, "y reordena la lista", 15, Aviso);
            };
            d["sc-retro"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 200, 140, Nucleo, 0.5f, 851);
                Espejo(v, 96, 120, 100, 150);
                for (var i = 0; i < 3; i++) Gorro(v, 194 + i * 40, 190 + (i % 2) * 20, Tinta, 0.8f);
                float ancho; TableroUI(v, 330, 30, 240, 220, new[] { "funcionó", "no funcionó", "a probar" }, out ancho, new[] { Menta, Coral, Sol });
                for (var k = 0; k < 3; k++) for (var i = 0; i < 2; i++) TarjetaUI(v, 338 + k * 80, 70 + i * 60, 64, 46, k == 0 ? Menta : k == 1 ? Coral : Sol, 0.5f);
            };
            d["sc-tablero"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Nucleo, 0.5f, 853);
                float ancho; TableroUI(v, 50, 30, 500, 240, new[] { "por hacer", "en curso", "hecho" }, out ancho, new[] { Nucleo, Aviso, Menta });
                TarjetaUI(v, 66, 68, 136, 44, Nucleo); TarjetaUI(v, 66, 122, 136, 44, Nucleo); TarjetaUI(v, 232, 68, 136, 44, Aviso); TarjetaUI(v, 398, 68, 136, 44, Menta); TarjetaUI(v, 398, 122, 136, 44, Menta);
                v.Neon(Tizador.Bezier(new Vector2(140, 200), new Vector2(220, 250), new Vector2(290, 200), 20), 1, Aviso);
                v.Neon(Tizador.Bezier(new Vector2(310, 200), new Vector2(390, 250), new Vector2(460, 200), 20), 1, Menta);
            };
            d["sc-burndown"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 855);
                PanelUI(v, 40, 30, 240, 190);
                var linea = new List<Vector2>(); var ys = new[] { 1f, 0.85f, 0.7f, 0.5f, 0.35f, 0.15f, 0f };
                for (var i = 0; i < ys.Length; i++) linea.Add(new Vector2(60 + i * 34, 200 - ys[i] * 150));
                v.Neon(linea, 1.6f, Nucleo);
                v.Rotulo(160, 244, "burndown:", 16, Nucleo); v.Rotulo(160, 266, "lo que queda, día a día", 15, Tinta);
                PanelUI(v, 320, 30, 240, 190);
                var bs = new[] { 0.5f, 0.75f, 0.62f };
                for (var i = 0; i < 3; i++) v.Forma(L.Redondeado(356 + i * 66, 200 - bs[i] * 150, 44, bs[i] * 150, 5), Color.Lerp(Aviso, Color.white, 0.2f), A(Aviso, 0.6f));
                for (var x = 330f; x < 552; x += 14) v.Forma(Caja(x, 108, 7, 2.4f), Coral);
                v.Rotulo(440, 244, "velocidad:", 16, Aviso); v.Rotulo(440, 266, "lo terminado por Sprint", 15, Tinta);
            };
            d["sc-errores"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Coral, 0.5f, 857);
                v.Forma(Caja(299, 20, 2, 260), A(Tinta, 0.12f)); v.Forma(Caja(30, 149, 540, 2), A(Tinta, 0.12f));
                Credencial(v, 66, 34, Coral); Gorro(v, 120, 100, Tinta, 0.8f);
                v.Rotulo(150, 132, "rendir cuentas al jefe", 14, Tinta); NeonX(v, 240, 70, 26);
                RelojDeArena(v, 400, 26, 80, TintaSuave); v.Rotulo(450, 132, "un Sprint sin objetivo", 14, Tinta); NeonX(v, 520, 70, 26);
                Espejo(v, 100, 214, 50, 60); v.Forma(Tizador.Rotar(L.Redondeado(70, 210, 64, 8, 3), 102, 214, 34), A(Coral, 0.8f));
                v.Rotulo(150, 282, "saltarse la Retro", 14, Tinta); NeonX(v, 240, 214, 26);
                RelojDeArena(v, 460, 170, 70, TintaSuave); TarjetaUI(v, 352, 190, 60, 34, Aviso); FlechaNeon(v, 416, 206, 440, 206, Aviso);
                v.Rotulo(450, 282, "cambios a mitad del Sprint", 14, Tinta); NeonX(v, 540, 214, 26);
            };
            d["sc-escalar"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 160, Aviso, 0.6f, 859);
                var x = new[] { 40f, 240, 440 };
                for (var i = 0; i < 3; i++) {
                    PanelUI(v, x[i], 30, 120, 80);
                    Gorro(v, x[i] + 36, 96, Tinta, 0.75f); Gorro(v, x[i] + 84, 96, Nucleo, 0.75f);
                    FlechaNeon(v, x[i] + 84, 116, 300 + (i - 1) * 30, 156, Nucleo);
                }
                v.Halo(300, 180, 40, A(Nucleo, 0.4f)); Anillo(v, 300, 180, 28, 3, Nucleo); v.Rotulo(300, 180, "juntos", 14, Nucleo, 0, true);
                FlechaNeon(v, 334, 190, 430, 216, Aviso);
                TorreDelCliente(v, 500, 290, 120, 0.9f, Aviso);
                v.Rotulo(150, 240, "varias cocinas,", 16, Tinta); v.Rotulo(150, 264, "un solo restaurante", 16, Aviso);
            };
            d["sc-resumen"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 861);
                Credencial(v, 50, 26, Aviso); Credencial(v, 110, 26, Nucleo); Gorro(v, 170, 80, Tinta, 0.9f);
                v.Rotulo(110, 118, "3 roles", 18, Aviso);
                Reloj(v, 300, 64, 28, 10, 10, Tinta);
                for (var i = 0; i < 5; i++) { var a = Mathf.PI * 2 / 5 * i - Mathf.PI / 2; v.Halo(300 + Mathf.Cos(a) * 52, 64 + Mathf.Sin(a) * 46, 10, A(Nucleo, 0.7f)); v.Forma(L.Elipse(300 + Mathf.Cos(a) * 52, 64 + Mathf.Sin(a) * 46, 5, 5, 12), NucleoClaro); }
                v.Rotulo(300, 140, "5 eventos", 18, Nucleo);
                Comandas(v, 440, 30, 100, Tinta, Tinta, Aviso);
                v.Rotulo(490, 128, "3 artefactos", 18, Tinta);
                for (var x = 40f; x < 560; x += 14) v.Forma(Caja(x, 159, 7, 1.6f), A(TintaSuave, 0.6f));
                v.Rotulo(300, 182, "cada artefacto, con su compromiso", 15, TintaSuave);
                Comandas(v, 60, 212, 80, Tinta, Tinta); FlechaNeon(v, 152, 232, 186, 232, Tinta);
                v.FormaDeLuz(Estrella(212, 232, 16), A(Sol, 0.95f));
                PanelUI(v, 254, 210, 60, 44); FlechaNeon(v, 320, 232, 352, 232, Tinta); Bandera(v, 368, 260, 46, Aviso);
                PlatoTapado(v, 456, 250, 22, Nucleo); FlechaNeon(v, 484, 232, 504, 232, Tinta);
                SelloTexto(v, 548, 232, "OK", Nucleo, 60);
            };
        }
    }
}
