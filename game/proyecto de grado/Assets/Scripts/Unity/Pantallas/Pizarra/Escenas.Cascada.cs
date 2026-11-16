using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using UnityEngine;
using L = Nexus.Unity.Tema.LuzUI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El curso de Cascada (ca-…): una obra que baja escalon a escalon, del plano al edificio entregado.
    /// (2026-11-15, direccion de arte) Las terrazas de hormigon del bunker con el agua de neon que nunca sube
    /// (PiezasDeEscena.Cocina: Terrazas), planos azules, carpetas y firmas.
    /// </summary>
    public static partial class DibujosDeEscena {
        /// <summary>Balde de cromo con su asa.</summary>
        private static void Balde(Tizador v, float cx, float yBase, Color c) {
            v.Forma(L.Arco(cx, yBase - 22, 14, 18, 2, 180, 360, 16), CromoOscuro);
            v.Forma(Pts(cx - 14, yBase - 22, cx + 14, yBase - 22, cx + 10, yBase, cx - 10, yBase), Cromo, CromoOscuro);
            v.Forma(L.Elipse(cx, yBase - 22, 14, 3.5f, 16), Color.Lerp(c, Silueta, 0.3f));
            v.Halo(cx, yBase - 22, 14, A(c, 0.5f));
        }

        /// <summary>Carpeta de papeles con su pestaña.</summary>
        private static void Carpeta(Tizador v, float x, float y, Color c) {
            v.Forma(Pts(x, y, x + 20, y, x + 26, y + 8, x + 54, y + 8, x + 54, y + 40, x, y + 40), Color.Lerp(c, Color.white, 0.1f), Color.Lerp(c, Silueta, 0.4f));
            v.Forma(Caja(x + 6, y + 4, 42, 30), H(0xF2EAD2));
            v.Forma(Pts(x, y + 14, x + 54, y + 14, x + 54, y + 40, x, y + 40), Color.Lerp(c, Color.white, 0.2f), Color.Lerp(c, Silueta, 0.3f));
        }

        /// <summary>Plano azul con sus estancias (x, y, w, h relativos a su origen, de cuatro en cuatro).</summary>
        private static void PlanoAzul(Tizador v, float x, float y, float w, float h, params float[] estancias) {
            v.Forma(L.Redondeado(x, y, w, h, 4), H(0x24507A), H(0x173A5C));
            for (var i = 0; i + 3 < estancias.Length; i += 4) Contorno(v, Caja(x + estancias[i], y + estancias[i + 1], estancias[i + 2], estancias[i + 3]), 1.4f, A(Color.white, 0.75f));
        }

        private static void Cascada(Dictionary<string, Action<Tizador>> d) {
            d["ca-que"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 1001);
                Terrazas(v, 30, 50, 6, 86, 38, Nucleo);
                Balde(v, 76, 50, Aviso); Balde(v, 334, 164, Aviso); Balde(v, 506, 240, Aviso);
                FlechaNeon(v, 150, 40, 250, 86, Aviso);
                v.Rotulo(400, 54, "siempre hacia abajo,", 17, Aviso); v.Rotulo(400, 78, "nunca de vuelta", 17, Aviso);
            };
            d["ca-origen"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Aviso, 0.5f, 1003);
                Credencial(v, 100, 70, Tinta, "Winston Royce");
                v.Rotulo(100, 190, "1970", 22, Aviso, 0, false, true);
                Papel(v, 230, 40, 220, 200);
                v.Forma(Caja(240, 60, 200, 170), H(0xF2EAD2));
                Terrazas(v.En(254, 70, 0.4f), 0, 0, 4, 110, 90, Nucleo);
                v.Halo(520, 112, 40, A(Coral, 0.4f));
                v.Forma(Pts(520, 70, 560, 140, 480, 140), Coral, H(0x8A2E1E)); v.Rotulo(520, 118, "!", 28, H(0x2A0B06), 0, false, true);
                Bocadillo(v, 440, 160, 150, 50, "«así es arriesgado»", Coral, 470, 140, 14);
                v.Rotulo(340, 272, "él mismo recomendaba volver atrás e iterar", 15, Tinta);
            };
            d["ca-idea"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 420, 140, Aviso, 0.5f, 1005);
                PlanoAzul(v, 20, 50, 170, 190, 30, 30, 110, 130, 30, 80, 110, 1, 85, 30, 1, 130);
                v.Rotulo(105, 262, "todo el plano, al inicio", 15, Nucleo);
                FlechaNeon(v, 204, 145, 346, 145, Tinta); v.Rotulo(276, 128, "meses", 15, TintaSuave);
                TorreDelCliente(v, 420, 250, 200, 0.8f, Aviso);
                FlechaNeon(v, 560, 216, 494, 216, Tinta);
                Credencial(v, 548, 150, Tinta);
                v.Rotulo(470, 276, "y solo entonces entra el cliente", 15, Aviso);
            };
            d["ca-fases"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 1007);
                Terrazas(v, 10, 44, 7, 82, 32, Nucleo);
                var n = new[] { "requisitos", "análisis", "diseño", "construcción", "pruebas", "despliegue", "mantenimiento" };
                for (var i = 0; i < 7; i++) v.Rotulo(51 + i * 82, 62 + i * 32, n[i], 12, Aviso);
                Papel(v, 34, 0, 30, 36); Lupa(v, 128, 22, 10, Nucleo); PlanoAzul(v, 196, 44, 34, 30);
                CajaDeCarga(v, 268, 76, 46, 28, H(0x8A5A2E)); Manometro(v, 400, 122, 16, 0.3f); Capsula(v, 436, 172, 70, Nucleo, false); Llave(v, 520, 200, 40, -20);
            };
            d["ca-requisitos"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Aviso, 0.5f, 1009);
                Credencial(v, 70, 60, Tinta);
                Bocadillo(v, 104, 24, 150, 40, "«y además…»", Tinta, 90, 70, 15);
                v.Rotulo(110, 150, "el cliente lo dicta todo", 14, Tinta);
                v.Forma(L.Redondeado(286, 26, 130, 250, 4), A(Silueta, 0.5f));
                v.Forma(L.Redondeado(280, 20, 130, 250, 4), H(0xEDE7D6), H(0xCFC7B0));
                v.Rotulo(345, 40, "SRS", 15, H(0x6A4A12), 0, false, true);
                for (var y = 64f; y < 228; y += 12) v.Forma(Caja(292, y, 106 - (y % 36), 2.4f), A(H(0x1E2A2A), 0.4f));
                v.Neon(Tizador.Bezier(new Vector2(300, 250), new Vector2(320, 236), new Vector2(340, 250), 10), 0.8f, H(0x1E2A2A));
                v.Neon(Tizador.Bezier(new Vector2(356, 250), new Vector2(372, 236), new Vector2(392, 250), 10), 0.8f, H(0x1E4A5A));
                Credencial(v, 510, 60, Nucleo, "Jefe de obra");
                v.Neon(Tizador.Bezier(new Vector2(470, 110), new Vector2(440, 130), new Vector2(416, 120), 12), 1, Nucleo);
                v.Rotulo(510, 240, "firman los dos", 15, Tinta);
            };
            d["ca-analisis"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Aviso, 0.5f, 1011);
                Papel(v, 200, 20, 150, 240);
                Credencial(v, 90, 80, Nucleo, "analista");
                Lupa(v, 220, 110, 34, Aviso);
                v.Rotulo(420, 76, "?", 30, Aviso, 0, false, true); v.Rotulo(500, 76, "¿se puede hacer?", 15, Aviso);
                v.Forma(Pts(420, 120, 440, 156, 400, 156), Coral); v.Rotulo(500, 146, "¿qué puede fallar?", 15, Coral);
                Papel(v, 404, 190, 32, 40); v.Rotulo(500, 214, "¿qué reglas hay?", 15, Nucleo);
            };
            d["ca-diseno"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Nucleo, 0.5f, 1013);
                PlanoAzul(v, 20, 50, 200, 180, 16, 20, 70, 60, 104, 20, 80, 60, 16, 100, 168, 60);
                v.Rotulo(120, 258, "arquitectura: el plano general", 14, Nucleo);
                FlechaNeon(v, 232, 140, 292, 140, Tinta);
                for (var i = 0; i < 3; i++) PlanoAzul(v, 310 + i * 94, 60 + i * 16, 86, 120, 10, 14, 66, 30, 10, 54, 30, 50, 46, 54, 30, 50);
                v.Rotulo(450, 258, "y el detalle de cada parte", 14, Aviso);
            };
            d["ca-implementacion"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Ambar, 0.6f, 1015);
                Papel(v, 240, 10, 120, 100, "receta", H(0x6A4A12));
                v.Rotulo(300, 128, "se sigue al pie de la letra", 14, Aviso);
                for (var i = 0; i < 3; i++) {
                    v.Forma(L.Redondeado(30 + i * 190, 150, 160, 130, 8), A(Superficie, 0.8f));
                    Gorro(v, 80 + i * 190, 224, Tinta, 0.9f);
                    Olla(v, 120 + i * 190, 262, 46, 38, 0.5f, Nucleo);
                    v.Neon(Tizador.Bezier(new Vector2(80 + i * 190, 172), new Vector2(180 + i * 100 - 10, 110), new Vector2(250 + i * 40, 92), 16), 0.7f, A(TintaSuave, 0.7f));
                }
            };
            d["ca-pruebas"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 250, 140, Nucleo, 0.5f, 1017);
                var n = new[] { "el cliente aprueba", "el plato completo", "dos ingredientes juntos", "un ingrediente" };
                var real = new[] { "aceptación", "sistema", "integración", "unitarias" };
                for (var i = 0; i < 4; i++) {
                    var w = 170 + i * 70;
                    Pieza(v, 250 - w / 2, 20 + i * 62, w, 54, n[i], i == 0 ? Aviso : Nucleo, 15);
                    v.Rotulo(250 + w / 2 + 20, 47 + i * 62, real[i], 15, TintaSuave, -1);
                }
                v.Rotulo(300, 290, "todo junto y al final, de abajo hacia arriba", 14, Tinta);
            };
            d["ca-despliegue"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 450, 120, Aviso, 0.6f, 1019);
                Capsula(v, 30, 180, 140, Nucleo, true);
                for (var i = 0; i < 3; i++) PlatoTapado(v, 70 + i * 30, 152 - i * 6, 12, Nucleo);
                FlechaNeon(v, 180, 160, 330, 160, Nucleo);
                v.Forma(L.Elipse(450, 170, 110, 30, 40), CostillaCerca, PrimerPlano); v.Forma(L.Redondeado(446, 196, 8, 56, 3), H(0x2B3335));
                for (var i = 0; i < 4; i++) PlatoTapado(v, 394 + i * 38, 166, 14, Aviso);
                Credencial(v, 450, 40, Aviso);
                v.Rotulo(300, 280, "el producto completo, de una sola vez", 16, Tinta);
            };
            d["ca-mantenimiento"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 150, Nucleo, 0.5f, 1021);
                v.Forma(Caja(299, 10, 2, 280), A(Tinta, 0.12f)); v.Forma(Caja(20, 149, 560, 2), A(Tinta, 0.12f));
                Tubo(v, Pts(60, 60, 240, 60), 16, CostillaMedia); Parche(v, 150, 60, 16); Chispa(v, 150, 92, 6, 0.2f);
                v.Rotulo(150, 122, "correctivo: arreglar", 15, Coral);
                CajaDeCarga(v, 410, 40, 60, 46, H(0x3F7F88)); v.Neon(Tizador.Bezier(new Vector2(480, 60), new Vector2(520, 40), new Vector2(540, 70), 12), 1, Aviso);
                v.Rotulo(450, 122, "adaptativo: ajustarse", 15, Aviso);
                PanelUI(v, 110, 176, 80, 60); v.FormaDeLuz(Estrella(150, 206, 18), A(Sol, 0.95f));
                v.Rotulo(150, 270, "perfectivo: mejorar", 15, Nucleo);
                Manometro(v, 430, 210, 30, 0.25f); Lupa(v, 494, 196, 14, Tinta);
                v.Rotulo(450, 270, "preventivo: evitar fallos", 15, Tinta);
            };
            d["ca-documentacion"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 1023);
                Terrazas(v, 40, 80, 4, 130, 50, Nucleo);
                for (var i = 0; i < 4; i++) {
                    Credencial(v, 90 + i * 130, 22 + i * 50, Tinta);
                    if (i < 3) { Carpeta(v, 130 + i * 130, 34 + i * 50, Aviso); FlechaNeon(v, 188 + i * 130, 64 + i * 50, 212 + i * 130, 82 + i * 50, Aviso); }
                }
                v.Rotulo(170, 276, "cada fase entrega su documento a la siguiente", 15, Aviso);
            };
            d["ca-puertas"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 330, 120, Aviso, 0.5f, 1025);
                v.Forma(Pts(0, 120, 240, 120, 240, 300, 0, 300), H(0x2B4F57), H(0x07222A));
                v.Forma(Pts(240, 220, 600, 220, 600, 300, 240, 300), H(0x2B4F57), H(0x07222A));
                v.Forma(L.Redondeado(228, 20, 24, 200, 4), Cromo, CromoOscuro);
                v.Forma(L.Redondeado(252, 20, 168, 8, 3), Cromo, CromoOscuro);
                for (var x = 270f; x < 420; x += 30) v.Forma(L.Redondeado(x, 28, 6, 192, 3), Cromo, CromoOscuro);
                Icono(v.En(270, 64, 0.75f), "candado", Tizador.Roja);
                Credencial(v, 110, 20, Tinta, "el cliente");
                Papel(v, 150, 52, 44, 56);
                v.Neon(Tizador.Bezier(new Vector2(156, 96), new Vector2(170, 84), new Vector2(188, 96), 10), 0.8f, H(0x1E4A5A));
                v.Rotulo(510, 120, "firma, y se abre", 15, Nucleo); v.Rotulo(510, 144, "la fase siguiente", 15, Nucleo);
            };
            d["ca-roles"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 1027);
                Terrazas(v, 20, 100, 5, 100, 36, Nucleo);
                var rol = new[] { "analista", "arquitecto", "programadores", "testers", "cliente" };
                for (var i = 0; i < 5; i++) { Credencial(v, 70 + i * 100, 40 + i * 36, i == 4 ? Aviso : Tinta); v.Rotulo(70 + i * 100, 116 + i * 36, rol[i], 12, Tinta, 0, true); }
                Credencial(v, 450, 0, Nucleo); Papel(v, 480, 10, 30, 40);
                v.Rotulo(520, 80, "jefe de proyecto", 15, Nucleo);
                v.Neon(Tizador.Bezier(new Vector2(430, 30), new Vector2(300, 20), new Vector2(200, 60), 18), 1, Nucleo);
            };
            d["ca-planificacion"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 1029);
                Pieza(v, 70, 14, 130, 40, "Restaurante", Aviso, 15);
                v.Neon(Pts(135, 54, 135, 76), 0.8f, Aviso); v.Neon(Pts(50, 76, 220, 76), 0.8f, Aviso);
                var rama = new[] { "cocina", "salón", "carta" };
                for (var i = 0; i < 3; i++) { v.Neon(Pts(50 + i * 85, 76, 50 + i * 85, 96), 0.8f, Aviso); Pieza(v, 14 + i * 85, 96, 72, 34, rama[i], Tinta, 13); }
                v.Rotulo(135, 164, "EDT: partir el trabajo", 14, Aviso);
                PanelUI(v, 280, 16, 300, 210);
                var x = new[] { 300f, 350, 420, 470 }; var w = new[] { 60f, 80, 60, 80 };
                for (var i = 0; i < 4; i++) v.Forma(L.Redondeado(x[i], 36 + i * 44, w[i], 28, 5), i == 2 ? A(TintaSuave, 0.5f) : Color.Lerp(Nucleo, Color.white, 0.15f), i == 2 ? A(TintaSuave, 0.3f) : A(Nucleo, 0.6f));
                v.Neon(Pts(300, 70, 360, 70, 360, 114, 470, 114, 470, 200, 550, 200), 1.2f, Coral);
                v.Rotulo(430, 250, "Gantt, y en rojo la ruta crítica", 14, Coral);
            };
            d["ca-cambios"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Coral, 0.5f, 1031);
                Credencial(v, 60, 80, Tinta);
                Bocadillo(v, 14, 14, 214, 40, "«¡quiero pasta, no arroz!»", Tinta, 70, 76, 13);
                FlechaNeon(v, 96, 120, 168, 120, Aviso);
                for (var i = 0; i < 4; i++) Papel(v, 186 + i * 8, 74 - i * 10, 80, 100, null, null, -3 + i * 2);
                v.Rotulo(240, 214, "solicitud", 15, Aviso);
                FlechaNeon(v, 306, 120, 356, 120, Aviso);
                SelloTexto(v, 410, 120, "EVAL.", Tinta, 90);
                FlechaNeon(v, 460, 120, 488, 120, Aviso);
                for (var i = 0; i < 3; i++) Credencial(v, 510 + i * 30, 80 + (i % 2) * 30, Coral);
                v.Rotulo(540, 214, "comité", 15, Coral);
                v.Rotulo(300, 268, "cambiar algo es un trámite formal: pedir, evaluar, aprobar", 15, Tinta);
            };
            d["ca-costo"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Coral, 0.5f, 1033);
                PanelUI(v, 50, 14, 500, 224);
                v.Neon(Pts(60, 220, 180, 212, 300, 190, 400, 150, 480, 90, 540, 26), 1.8f, Coral);
                v.Rotulo(170, 176, "borrar una línea del plano", 14, Nucleo); Monedas(v, 170, 156, 1);
                v.Rotulo(320, 60, "derribar una pared", 14, Coral); Monedas(v, 444, 110, 3); Monedas(v, 480, 110, 6);
                v.Rotulo(300, 256, "cuanto más tarde se descubre un error, más cuesta", 16, Tinta);
                v.Rotulo(80, 282, "inicio", 13, TintaSuave); v.Rotulo(520, 282, "final", 13, TintaSuave);
            };
            d["ca-v"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 160, Nucleo, 0.5f, 1035);
                v.Neon(Pts(60, 30, 300, 270), 1.6f, Nucleo); v.Neon(Pts(300, 270, 540, 30), 1.6f, Aviso);
                var baja = new[] { "requisitos", "diseño", "detalle" }; var sube = new[] { "aceptación", "sistema", "unitarias" };
                for (var i = 0; i < 3; i++) {
                    float xl = 100 + i * 70, y = 60 + i * 70, xr = 500 - i * 70;
                    v.Halo(xl - 10, y, 12, A(Nucleo, 0.6f)); v.Forma(L.Elipse(xl - 10, y, 6, 6, 12), NucleoClaro);
                    v.Halo(xr + 10, y, 12, A(Aviso, 0.6f)); v.Forma(L.Elipse(xr + 10, y, 6, 6, 12), BombillaCentro);
                    for (var x = xl + 8; x < xr - 4; x += 14) v.Forma(Caja(x, y - 1, 7, 2), A(TintaSuave, 0.6f));
                    var dx = i < 2 ? 60 : -56;
                    v.Rotulo(xl + dx, y - 14, baja[i], 14, Nucleo); v.Rotulo(xr - dx, y - 14, sube[i], 14, Aviso);
                }
                v.Rotulo(300, 290, "construir", 14, Tinta); v.Rotulo(80, 270, "se define", 14, Nucleo); v.Rotulo(520, 270, "se prueba", 14, Aviso);
            };
            d["ca-variantes"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Nucleo, 0.5f, 1037);
                Terrazas(v, 20, 60, 3, 50, 40, Nucleo);
                for (var i = 0; i < 2; i++) v.Neon(Tizador.Bezier(new Vector2(100 + i * 50, 110 + i * 40), new Vector2(70 + i * 50, 100 + i * 40), new Vector2(80 + i * 50, 76 + i * 40), 12), 1, Aviso);
                v.Rotulo(100, 226, "con vuelta atrás", 14, Nucleo);
                for (var i = 0; i < 3; i++) v.Forma(L.Redondeado(230 + i * 36, 60 + i * 40, 70, 30, 5), i == 1 ? Aviso : Color.Lerp(Nucleo, Color.white, 0.2f), A(i == 1 ? Aviso : Nucleo, 0.6f));
                v.Rotulo(300, 226, "Sashimi:", 14, Aviso); v.Rotulo(300, 246, "fases que se solapan", 14, Aviso);
                for (var k = 0; k < 3; k++) Terrazas(v, 420 + k * 54, 70 + k * 12, 3, 16, 26, Nucleo);
                v.Rotulo(500, 226, "incremental:", 14, Tinta); v.Rotulo(500, 246, "varias pequeñas", 14, Tinta);
            };
            d["ca-ventajas"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Nucleo, 0.5f, 1039);
                Credencial(v, 90, 50, Nucleo, "Jefe de obra");
                Calendario(v, 200, 60, 90, 90, Aviso); NeonCheck(v, 245, 120, 14);
                v.Rotulo(245, 178, "fechas claras", 15, Aviso);
                v.Forma(L.Redondeado(330, 60, 100, 90, 10), H(0x46555A), H(0x161D20)); Monedas(v, 380, 136, 5);
                v.Rotulo(380, 178, "presupuesto cerrado", 15, Tinta);
                for (var i = 0; i < 3; i++) Carpeta(v, 470 + i * 14, 70 + i * 16, Nucleo);
                v.Rotulo(520, 178, "todo documentado", 15, Nucleo);
                v.Rotulo(300, 250, "fácil de entender y de controlar", 18, Tinta);
            };
            d["ca-desventajas"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 130, Coral, 0.5f, 1041);
                Credencial(v, 110, 90, Tinta);
                Maraña(v, 12, 12, 60, 50, A(TintaSuave, 0.5f), 3);
                Reloj(v, 210, 70, 26, 11, 50, TintaSuave);
                v.Rotulo(130, 200, "meses esperando", 15, TintaSuave);
                FlechaNeon(v, 266, 130, 336, 130, Tinta);
                PlatoTapado(v, 410, 170, 40, Aviso);
                Credencial(v, 520, 90, Coral);
                Bocadillo(v, 356, 14, 230, 40, "«esto no era lo que quería»", Coral, 520, 80, 14);
                v.Rotulo(300, 260, "ve el producto muy tarde, y cambiarlo ya es difícil", 16, Coral);
            };
            d["ca-cuando"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 120, Aviso, 0.5f, 1043);
                var pl = Balanza(v, 300, 110, 160, 236, -6);
                v.Forma(L.Arco(pl[0].x, pl[0].y - 10, 40, 30, 4, 180, 360, 20), Nucleo); v.Forma(L.Redondeado(pl[0].x - 44, pl[0].y - 12, 88, 6, 3), Nucleo);
                v.Rotulo(140, 40, "Cascada", 22, Nucleo, 0, false, true);
                v.Rotulo(140, 222, "un puente, un avión,", 15, Nucleo); v.Rotulo(140, 244, "software médico", 15, Nucleo);
                v.Rotulo(140, 276, "requisitos fijos y regulados", 14, Tinta);
                Movil(v, pl[1].x - 16, pl[1].y - 54, 32, 50, Aviso);
                v.Rotulo(470, 40, "Ágil", 22, Aviso, 0, false, true);
                v.Rotulo(460, 222, "una app de moda:", 15, Aviso); v.Rotulo(460, 244, "lo que se pide cambia", 15, Aviso);
            };
            d["ca-resumen"] = p => {
                var v = p.Vitrina();
                FondoInterior(v, 300, 140, Aviso, 0.5f, 1045);
                var n = new[] { "Cascada", "Scrum", "Kanban" };
                for (var i = 0; i < 3; i++) { v.Rotulo(70, 60 + i * 90, n[i], 18, Aviso, 0, false, true); v.Forma(Caja(140, 80 + i * 90, 430, 1.2f), A(Borde, 0.9f)); }
                PlatoTapado(v, 520, 70, 40, Nucleo); v.Rotulo(330, 56, "una entrega grande, al final", 14, Tinta);
                for (var i = 0; i < 4; i++) PlatoTapado(v, 196 + i * 104, 166, 22, Nucleo);
                v.Rotulo(355, 118, "entregas en ciclos", 14, Tinta);
                Cinta(v, 150, 244, 420);
                for (var i = 0; i < 9; i++) Plato(v, 170 + i * 46, 244, 14, Nucleo);
                v.Rotulo(355, 210, "flujo continuo", 14, Tinta);
            };
        }
    }
}
