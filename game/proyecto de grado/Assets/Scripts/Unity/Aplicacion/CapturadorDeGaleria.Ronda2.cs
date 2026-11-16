#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ruta = Nexus.Unity.Tema.RutaDeCurva;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// SOLO EN EL EDITOR (2026-11-15). Ronda 2 de la direccion de arte, tras rechazar las tres de la ronda 1 por
    /// «sentirse generadas con IA». Referencia: Ori (luz y brillo, profundidad por capas, formas organicas) + Fallout
    /// (retrofuturismo de los 50, propaganda con mascota: ARQUI). Seis opciones, cada una a pantalla completa:
    ///   1 · Bioluminiscencia corporativa   2 · Streamline atomico   3 · Diorama (render de Blender)
    ///   4 · Silueta y linterna             5 · Dibujo animado de los 50 sobre luz   6 · Vidrio y valvula (la UI)
    /// Todo se dibuja aqui desde cero (FormaDegradada + piezas de luz de CapturadorDeGaleria.Luz) salvo la 3, que carga
    /// docs/08-arte/ronda2/diorama_sector7.png (tools/arte/diorama_sector7.py).
    /// </summary>
    public sealed partial class CapturadorDeGaleria {
        private static Color X(int rgb, float a = 1f) { var c = H(rgb); c.a = a; return c; }

        // ================================================================ marco comun

        private void Marco(RectTransform capa, int n, string titulo, string lema, Color acento, Color[] paleta) {
            capa.GetComponent<Image>().color = Color.black;
            // Oscurecido arriba para que el titulo se lea sin tapar la escena.
            Niebla(capa, 0, 0, 1920, 230, Color.black, 0.78f, 0f);
            Niebla(capa, 0, 930, 1920, 150, Color.black, 0f, 0.7f);
            Rotulo2(capa, 48, 30, 600, 26, "RONDA 2 · OPCIÓN " + n, 18, acento, FuentesNexus.InterfazNegrita, TextAlignmentOptions.TopLeft, 6);
            Rotulo2(capa, 48, 56, 1100, 60, titulo, 46, X(0xF4F1E8), FuentesNexus.Display);
            Rotulo2(capa, 48, 118, 1000, 60, lema, 19, X(0xF4F1E8, 0.82f), FuentesNexus.Interfaz);
            for (var i = 0; i < paleta.Length; i++) {
                Forma(capa, Redondeado(48 + i * 58, 1010, 46, 46, 23), paleta[i]);
            }
        }

        /// <summary>La muestra de UI de cada opcion: un boton en reposo y otro activo, abajo a la derecha.</summary>
        private void MuestraBotones(RectTransform capa, Action<RectTransform, float, float, bool, string> boton) {
            boton(capa, 1300, 940, false, "REPOSO");
            boton(capa, 1600, 940, true, "ACTIVO");
        }

        // ================================================================ 1 · bioluminiscencia corporativa

        private void Ronda2_1(RectTransform capa) {
            Cielo(capa, 0, 0, 1920, 1080, S(0, X(0x03101A)), S(0.42f, X(0x0A3442)), S(0.6f, X(0x1B6F7C)), S(0.74f, X(0x0B2F3A)), S(1, X(0x02080C)));
            // El corazon de luz: el nucleo del Sector 7 al fondo de la nave.
            Brillo(capa, 1180, 560, 760, 560, X(0x5FD8F5, 0.5f));
            for (var i = 0; i < 11; i++) Haz(capa, 1180, 560, 980, 150, i * 32.7f + 8, X(0x8FEAFF, 0.06f));
            Brillo(capa, 1180, 560, 280, 240, X(0xBFF6FF, 0.75f));
            Brillo(capa, 1180, 560, 70, 70, X(0xFFFFFF, 0.95f));
            // Costillas como huesos de ballena: cada capa mas cerca, mas grande y mas oscura; niebla entre ellas.
            Forma(capa, Arco(1180, 800, 250, 300, 24, 180, 360, 48, 0.02f, 1), X(0x3FA3B0), X(0x2A8A96), new Vector2(0, 500), new Vector2(0, 800));
            Particulas(capa, new Rect(900, 320, 560, 420), 40, 11, X(0x8FEAFF), 1.2f, 3.5f);
            Niebla(capa, 0, 420, 1920, 520, X(0x2A8C98), 0f, 0.28f);
            Forma(capa, Arco(1180, 840, 430, 500, 38, 180, 360, 48, 0.025f, 2), X(0x1E6470), X(0x15505A), new Vector2(0, 340), new Vector2(0, 840));
            Niebla(capa, 0, 380, 1920, 600, X(0x1B6F7C), 0f, 0.22f);
            Forma(capa, Arco(1180, 900, 660, 760, 62, 180, 360, 56, 0.03f, 3), X(0x123F4A), X(0x0B2A32), new Vector2(0, 140), new Vector2(0, 900));
            // Cables colgando como lianas, con bombillas ambar.
            var d = Trazos(capa);
            for (var k = 0; k < 4; k++) {
                float x0 = 420 + k * 330, x1 = x0 + 360, caida = 260 + 60 * (k % 2);
                var cable = new Ruta(x0, 120).Q((x0 + x1) / 2, 120 + caida * 2, x1, 120).Puntos();
                d.Polilinea(cable, X(0x0A2129), 5);
                for (var j = 1; j < 4; j++) {
                    var p = cable[j * cable.Count / 4];
                    Brillo(capa, p.x, p.y + 12, 46, 46, X(0xF2C14E, 0.55f));
                    Brillo(capa, p.x, p.y + 12, 9, 9, X(0xFFF1C4, 1f));
                }
            }
            Forma(capa, Arco(1180, 1000, 940, 1060, 110, 180, 360, 64, 0.03f, 4), X(0x07222A), X(0x041218), new Vector2(0, 0), new Vector2(0, 1000));
            // La pasarela hacia el nucleo y el suelo organico del primer plano.
            Forma(capa, new Ruta(560, 1080).L(1110, 790).L(1250, 790).L(1520, 1080).Puntos(), X(0x1A4E58), X(0x08202A), new Vector2(0, 790), new Vector2(0, 1080));
            d = Trazos(capa);
            d.Linea(1110, 790, 560, 1080, X(0x5FD8F5, 0.7f), 3);
            d.Linea(1250, 790, 1520, 1080, X(0x5FD8F5, 0.45f), 3);
            for (var i = 0; i < 6; i++) {
                var t = i / 6f;
                float xi = Mathf.Lerp(1110, 560, t), xd = Mathf.Lerp(1250, 1520, t), y = Mathf.Lerp(790, 1080, t);
                var alto = Mathf.Lerp(24, 70, t);
                d.Linea(xi, y, xi, y - alto, X(0x0A2129), Mathf.Lerp(3, 7, t));
                d.Linea(xd, y, xd, y - alto, X(0x0A2129), Mathf.Lerp(3, 7, t));
                Brillo(capa, xi, y - alto, 18, 18, X(0x5FD8F5, 0.6f));
            }
            Forma(capa, new Ruta(0, 880).C(260, 840, 420, 900, 600, 930).C(720, 950, 760, 1000, 740, 1080).L(0, 1080).Puntos(), X(0x05141A), X(0x010507), new Vector2(0, 860), new Vector2(0, 1080));
            Forma(capa, new Ruta(1920, 860).C(1700, 880, 1560, 940, 1500, 1080).L(1920, 1080).Puntos(), X(0x05141A), X(0x010507), new Vector2(0, 860), new Vector2(0, 1080));
            // La trabajadora en la pasarela, a contraluz: silueta con el filo de luz del nucleo.
            Silueta(capa, 1015, 900, 1.15f, X(0x02090C), X(0x8FEAFF), true);
            // Tuberia gigante en primer plano, arriba a la izquierda, curvandose fuera de cuadro.
            var tubo = Tubo(new Ruta(-60, 380).C(160, 250, 300, 60, 520, -40).Puntos(), 150, 110);
            Forma(capa, tubo, X(0x03090C));
            d = Trazos(capa);
            d.Polilinea(Mover(new Ruta(-60, 380).C(160, 250, 300, 60, 520, -40).Puntos(), 34, 30), X(0x2A8C98, 0.6f), 3);
            Particulas(capa, new Rect(0, 200, 1920, 760), 70, 7, X(0x8FEAFF), 1f, 4f);
            Particulas(capa, new Rect(300, 300, 1100, 600), 18, 3, X(0xF2C14E), 1.5f, 3f);
            Marco(capa, 1, "BIOLUMINISCENCIA CORPORATIVA", "El búnker como una nave viva: costillas que se pierden en la niebla, un núcleo que respira luz y la gente a contraluz. El neón es luz viva, no un borde.",
                X(0x5FD8F5), new[] { X(0x03101A), X(0x0B2A32), X(0x1E6470), X(0x2A8C98), X(0x5FD8F5), X(0xBFF6FF), X(0xF2C14E) });
            MuestraBotones(capa, (c, x, y, activo, texto) => {
                if (activo) Brillo(c, x + 130, y + 32, 200, 80, X(0x5FD8F5, 0.45f));
                Forma(c, Redondeado(x, y, 260, 64, 32), activo ? X(0x1B6F7C, 0.95f) : X(0x0B2F3A, 0.85f), activo ? X(0x0B3440, 0.95f) : X(0x061A20, 0.85f), new Vector2(0, y), new Vector2(0, y + 64));
                Forma(c, Redondeado(x + 10, y + 6, 240, 22, 11), X(0xFFFFFF, activo ? 0.18f : 0.07f), X(0xFFFFFF, 0f), new Vector2(0, y + 6), new Vector2(0, y + 28));
                Rotulo2(c, x, y, 260, 64, texto, 22, activo ? X(0xDFFBFF) : X(0x8FB8C0), FuentesNexus.InterfazNegrita, TextAlignmentOptions.Center, 4);
            });
        }

        /// <summary>Una persona en silueta (overol, gorra), de pie, a escala k con los pies en (x, y). Filo de luz a la derecha o a la izquierda.</summary>
        private void Silueta(RectTransform capa, float x, float y, float k, Color cuerpo, Color filo, bool filoDerecha, bool señala = false) {
            Func<float, float, Vector2> P = (px, py) => new Vector2(x + px * k, y + py * k);
            var pierna1 = new List<Vector2> { P(-26, -110), P(-4, -110), P(-6, -6), P(-30, -4) };
            var pierna2 = new List<Vector2> { P(4, -110), P(26, -110), P(30, -4), P(6, -6) };
            var torso = new Ruta(x - 34 * k, y - 112 * k).C(x - 40 * k, y - 160 * k, x - 36 * k, y - 196 * k, x - 20 * k, y - 204 * k)
                .L(x + 20 * k, y - 204 * k).C(x + 36 * k, y - 196 * k, x + 40 * k, y - 160 * k, x + 34 * k, y - 112 * k).Puntos();
            var cabeza = Elipse(x, y - 224 * k, 17 * k, 20 * k, 28);
            var gorra = new Ruta(x - 19 * k, y - 230 * k).C(x - 18 * k, y - 252 * k, x + 18 * k, y - 252 * k, x + 19 * k, y - 230 * k).L(x + 34 * k, y - 228 * k).L(x - 19 * k, y - 226 * k).Puntos();
            var brazoI = Tubo(new Ruta(x - 32 * k, y - 196 * k).Q(x - 48 * k, y - 150 * k, x - 40 * k, y - 104 * k).Puntos(), 16 * k, 13 * k);
            var brazoD = señala
                ? Tubo(new Ruta(x + 30 * k, y - 196 * k).Q(x + 80 * k, y - 210 * k, x + 120 * k, y - 236 * k).Puntos(), 16 * k, 12 * k)
                : Tubo(new Ruta(x + 32 * k, y - 196 * k).Q(x + 48 * k, y - 150 * k, x + 40 * k, y - 104 * k).Puntos(), 16 * k, 13 * k);
            foreach (var p in new[] { pierna1, pierna2, torso, cabeza, gorra, brazoI, brazoD }) Forma(capa, p, cuerpo);
            var d = Trazos(capa);
            var s = filoDerecha ? 1 : -1;
            d.Polilinea(new Ruta(x + s * 22 * k, y - 236 * k).Q(x + s * 22 * k, y - 216 * k, x + s * 12 * k, y - 206 * k).Puntos(), filo, 2.5f);
            d.Polilinea(new Ruta(x + s * 36 * k, y - 190 * k).Q(x + s * 42 * k, y - 150 * k, x + s * 36 * k, y - 112 * k).Puntos(), filo, 2.5f);
            d.Linea(x + s * 28 * k, y - 108 * k, x + s * 30 * k, y - 8 * k, filo, 2.5f);
            Brillo(capa, x + s * 30 * k, y - 160 * k, 40 * k, 120 * k, A(filo, 0.18f));
        }

        // ================================================================ 2 · streamline atomico

        private void Ronda2_2(RectTransform capa) {
            Cielo(capa, 0, 0, 1920, 1080, S(0, X(0x140C2E)), S(0.38f, X(0x4A2150)), S(0.6f, X(0xC2504A)), S(0.72f, X(0xF08A3C)), S(0.8f, X(0xF7D07A)), S(1, X(0xF7D07A)));
            Particulas(capa, new Rect(0, 0, 1920, 300), 40, 21, X(0xFFF2D8), 0.6f, 1.6f);
            // Sol aerodinamico: disco, halo grande y rayos.
            Brillo(capa, 760, 830, 1100, 640, X(0xF2A24E, 0.55f));
            for (var i = 0; i < 9; i++) Haz(capa, 760, 830, 900, 110, 120 + i * 15, X(0xFFD9A0, 0.07f));
            Forma(capa, Elipse(760, 830, 150, 150, 64), X(0xFFF0C0), X(0xF29A4E), new Vector2(0, 680), new Vector2(0, 980));
            // Nubes en tiras aerodinamicas.
            // Nubes en huso: afiladas en las puntas, como estela de avion a reaccion.
            for (var i = 0; i < 6; i++) {
                float cy = 250 + i * 70, cw = 380 + (i * 137) % 300, cx = (i * 311) % 1500;
                var huso = new Ruta(cx, cy).C(cx + cw * 0.3f, cy - 16 - i % 2 * 6, cx + cw * 0.75f, cy - 12, cx + cw, cy + 2)
                    .C(cx + cw * 0.7f, cy + 10, cx + cw * 0.3f, cy + 12, cx, cy).Puntos();
                Forma(capa, huso, X(0xF8B27A, 0.6f - i * 0.05f), X(0x7A2C50, 0.3f), new Vector2(0, cy - 16), new Vector2(0, cy + 12));
            }
            // Capa lejana: torres con remate redondeado y la torre Nexus con su platillo y el atomo.
            var lejos = X(0xA24A5A); var lejos2 = X(0x8A3A50);
            float[] torres = { 80, 230, 70, 300, 360, 50, 980, 120, 1460, 90, 1640, 60, 1780, 110 };
            for (var i = 0; i + 1 < torres.Length; i += 2) {
                float tx = torres[i], th = 220 + (i * 53) % 200;
                Forma(capa, Redondeado(tx, 900 - th, torres[i + 1], th + 200, torres[i + 1] / 2), lejos, lejos2, new Vector2(0, 900 - th), new Vector2(0, 1000));
            }
            Niebla(capa, 0, 560, 1920, 400, X(0xF08A3C), 0f, 0.35f);
            // Torre Nexus.
            var torre = new Ruta(1160, 1080).L(1170, 520).C(1180, 430, 1260, 430, 1270, 520).L(1280, 1080).Puntos();
            Forma(capa, torre, X(0x5A2440), X(0x3A1834), new Vector2(1160, 0), new Vector2(1280, 0));
            Forma(capa, Elipse(1220, 520, 210, 30, 64), X(0x6E2C4A), X(0x2E1430), new Vector2(0, 490), new Vector2(0, 550));
            Forma(capa, Elipse(1220, 512, 180, 12, 48), X(0xFFD9A0, 0.5f));
            var d = Trazos(capa);
            d.Linea(1220, 440, 1220, 300, X(0x3A1834), 6);
            // El atomo en la aguja, en tubos de neon ambar.
            for (var i = 0; i < 3; i++) {
                var orbita = new List<Vector2>();
                for (var j = 0; j <= 48; j++) {
                    var a = j * Mathf.PI * 2 / 48;
                    var p = new Vector2(Mathf.Cos(a) * 62, Mathf.Sin(a) * 20);
                    var rot = Quaternion.Euler(0, 0, i * 60) * p;
                    orbita.Add(new Vector2(1220 + rot.x, 280 + rot.y));
                }
                TuboPoli(d, orbita, X(0xF2C14E), 3);
            }
            Brillo(capa, 1220, 280, 120, 120, X(0xF2C14E, 0.55f));
            Brillo(capa, 1220, 280, 14, 14, X(0xFFFFFF));
            // Monorriel en arco con sus pilares.
            var via = new Ruta(-40, 700).C(500, 600, 1300, 620, 1960, 660).Puntos();
            Forma(capa, Tubo(via, 26, 26), X(0x3C1A34));
            for (var i = 1; i < 6; i++) {
                var p = via[i * via.Count / 6];
                Forma(capa, new List<Vector2> { new Vector2(p.x - 10, p.y), new Vector2(p.x + 10, p.y), new Vector2(p.x + 26, 1080), new Vector2(p.x - 26, 1080) }, X(0x3C1A34), X(0x24102A), new Vector2(0, p.y), new Vector2(0, 1080));
            }
            Forma(capa, Redondeado(820, 640, 260, 40, 20), X(0xE8E2D6), X(0x8A7A80), new Vector2(0, 640), new Vector2(0, 680));
            Forma(capa, Redondeado(840, 648, 220, 12, 6), X(0x5FD8F5, 0.85f));
            // Capa media: tejados en boomerang y arcos parabolicos.
            var medio = X(0x2C1430);
            Forma(capa, new Ruta(0, 1080).L(0, 860).L(120, 820).Q(260, 900, 420, 800).L(440, 880).L(440, 1080).Puntos(), medio);
            Forma(capa, new Ruta(1400, 1080).L(1400, 880).Q(1520, 700, 1640, 880).L(1660, 860).Q(1760, 760, 1860, 860).L(1920, 860).L(1920, 1080).Puntos(), medio);
            // Primer plano: el suelo, un coche en gota de agua y la trabajadora señalando la torre.
            Forma(capa, new Ruta(0, 960).C(500, 930, 1300, 950, 1920, 940).L(1920, 1080).L(0, 1080).Puntos(), X(0x1A0C1E), X(0x0A050C), new Vector2(0, 940), new Vector2(0, 1080));
            // Coche aerodinamico de perfil: carroceria en gota, burbuja de cristal, aleta trasera y ruedas carenadas.
            var coche = new Ruta(250, 968).C(250, 940, 300, 925, 380, 918).C(420, 880, 520, 872, 570, 905)
                .C(640, 912, 700, 925, 720, 948).L(722, 968).Puntos();
            Forma(capa, coche, X(0x1C0E1E), X(0x0E060F), new Vector2(0, 880), new Vector2(0, 968));
            Forma(capa, new Ruta(640, 915).L(700, 868).L(712, 872).L(690, 930).Puntos(), X(0x1C0E1E));
            Forma(capa, new Ruta(410, 902).C(440, 880, 520, 878, 552, 904).Puntos(), X(0xF7D07A, 0.35f));
            Forma(capa, Elipse(330, 968, 34, 34, 32), X(0x050205));
            Forma(capa, Elipse(640, 968, 34, 34, 32), X(0x050205));
            Forma(capa, Elipse(330, 968, 14, 14, 24), X(0xC8B8C0), X(0x6A5A62), new Vector2(0, 954), new Vector2(0, 982));
            Forma(capa, Elipse(640, 968, 14, 14, 24), X(0xC8B8C0), X(0x6A5A62), new Vector2(0, 954), new Vector2(0, 982));
            d = Trazos(capa);
            d.Polilinea(new Ruta(262, 945).C(300, 928, 340, 922, 382, 918).C(420, 882, 520, 874, 568, 904).Puntos(), X(0xF7D07A, 0.85f), 3);
            Silueta(capa, 1500, 960, 1.05f, X(0x120812), X(0xF7C27A), false, true);
            // Rotulo googie: boomerang con tubo de neon cian y estrella ambar.
            var boom = new Ruta(1560, 240).Q(1700, 200, 1860, 160).Q(1760, 260, 1820, 330).Q(1700, 290, 1560, 240).Puntos();
            Forma(capa, boom, X(0x2A1430, 0.92f));
            d = Trazos(capa);
            TuboPoli(d, new List<Vector2>(boom) { boom[0] }, X(0x5FD8F5), 3.5f);
            d.Linea(1690, 280, 1690, 960, X(0x1A0C1E), 8);
            for (var i = 0; i < 8; i++) {
                var a = i * Mathf.PI / 4;
                TuboLinea(d, 1600, 180, 1600 + Mathf.Cos(a) * 36, 180 + Mathf.Sin(a) * 36, X(0xF2C14E), 3);
            }
            Rotulo2(capa, 1600, 222, 240, 40, "SECTOR 7", 30, X(0xDFFBFF), FuentesNexus.Display, TextAlignmentOptions.Center, 3);
            Marco(capa, 2, "STREAMLINE ATÓMICO", "El futuro que prometía Nexus en 1957: torres con platillo, monorriel, átomos de neón y un atardecer de cromo. Contraluz cálido, curvas aerodinámicas, nada recto.",
                X(0xF2C14E), new[] { X(0x140C2E), X(0x4A2150), X(0xC2504A), X(0xF08A3C), X(0xF7D07A), X(0x5FD8F5), X(0x1A0C1E) });
            MuestraBotones(capa, (c, x, y, activo, texto) => {
                if (activo) Brillo(c, x + 130, y + 32, 190, 70, X(0xF2C14E, 0.4f));
                Forma(c, Redondeado(x, y, 260, 64, 32), X(0xF4EEE6), X(0x8A7A80), new Vector2(0, y), new Vector2(0, y + 64));
                Forma(c, Redondeado(x + 8, y + 8, 244, 48, 24), activo ? X(0x3A1834) : X(0x2A1430), activo ? X(0x1A0C1E) : X(0x140A18), new Vector2(0, y + 8), new Vector2(0, y + 56));
                Rotulo2(c, x, y, 260, 64, texto, 22, activo ? X(0xF7D07A) : X(0xB89AA8), FuentesNexus.Display, TextAlignmentOptions.Center, 3);
            });
        }

        // ================================================================ 3 · diorama de Blender

        private void Ronda2_3(RectTransform capa) {
            var ruta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "docs", "08-arte", "ronda2", "diorama_sector7.png"));
            if (File.Exists(ruta)) {
                var tex = new Texture2D(2, 2);
                tex.LoadImage(File.ReadAllBytes(ruta));
                Img(capa, tex, 0, 0, 1920, 1080, Color.white, false);
            } else { Anotar("AVISO: falta " + ruta + " (tools/arte/diorama_sector7.py)"); Fallos++; }
            Particulas(capa, new Rect(300, 150, 1300, 600), 50, 5, X(0x8FEAFF), 1f, 3f);
            Marco(capa, 3, "DIORAMA DEL SECTOR 7", "Escenas modeladas en Blender por script y renderizadas con sombreado de dos bandas, niebla volumétrica y neón. El mismo lenguaje sirve para el 2D y para el recorrido 3D.",
                X(0x8FEAFF), new[] { X(0x06121A), X(0x1B2A31), X(0x2E4A52), X(0xDDE6E8), X(0xB5843A), X(0x5FD8F5), X(0xC9A227) });
            MuestraBotones(capa, (c, x, y, activo, texto) => {
                if (activo) Brillo(c, x + 130, y + 32, 180, 70, X(0x8FEAFF, 0.35f));
                Forma(c, Redondeado(x, y, 260, 64, 14), X(0xC99A4A), X(0x5A3B16), new Vector2(0, y), new Vector2(0, y + 64));
                Forma(c, Redondeado(x + 5, y + 5, 250, 54, 10), activo ? X(0xF4F6F6) : X(0xDDE6E8), activo ? X(0xBFD0D6) : X(0x9AAAB0), new Vector2(0, y + 5), new Vector2(0, y + 59));
                Rotulo2(c, x, y, 260, 64, texto, 22, X(0x16262D), FuentesNexus.Display, TextAlignmentOptions.Center, 3);
            });
        }

        // ================================================================ 4 · silueta y linterna

        private void Ronda2_4(RectTransform capa) {
            Cielo(capa, 0, 0, 1920, 1080, S(0, X(0x02070A)), S(0.5f, X(0x0A1C24)), S(0.72f, X(0x0D2830)), S(1, X(0x010406)));
            // Fondo: estanterias y arcos del archivo, perdidos en la niebla.
            for (var i = 0; i < 7; i++) {
                float ax = 200 + i * 260;
                Forma(capa, Arco(ax, 760, 110, 300, 14, 180, 360, 30), X(0x163640));
                for (var f = 0; f < 5; f++) Forma(capa, Redondeado(ax - 80, 520 + f * 48, 160, 10, 4), X(0x12303A));
            }
            Niebla(capa, 0, 300, 1920, 700, X(0x0D2830), 0f, 0.65f);
            Forma(capa, Arco(1500, 1000, 420, 760, 46, 180, 360, 48, 0.02f, 9), X(0x0A1E25));
            Forma(capa, Arco(260, 1060, 380, 820, 52, 180, 360, 48, 0.02f, 4), X(0x08181E));
            // La puerta de la boveda a la derecha, solo un anillo cian: es interactiva.
            var d = Trazos(capa);
            Forma(capa, Elipse(1560, 690, 120, 120, 56), X(0x0A1E25));
            TuboCirculo(d, 1560, 690, 104, X(0x5FD8F5), 3);
            Brillo(capa, 1560, 690, 200, 200, X(0x5FD8F5, 0.18f));
            // La linterna: la unica luz calida de la escena; lo que toca se enciende por ese lado.
            float lx = 760, ly = 700;
            Brillo(capa, lx, ly, 680, 560, X(0xF2C14E, 0.42f));
            Brillo(capa, lx, ly + 230, 520, 90, X(0xF2C14E, 0.35f));
            Brillo(capa, lx, ly, 190, 170, X(0xFFD98A, 0.7f));
            // Cajas de raciones junto a la luz: degradado de la cara iluminada a la oscura.
            Forma(capa, Redondeado(900, 800, 150, 130, 10), X(0x8A6A3A), X(0x0B1418), new Vector2(900, 0), new Vector2(1050, 0));
            Forma(capa, Redondeado(940, 690, 120, 112, 10), X(0x7A5C32), X(0x0B1418), new Vector2(940, 0), new Vector2(1060, 0));
            Forma(capa, Redondeado(470, 830, 170, 100, 10), X(0x0B1418), X(0x7A5C32), new Vector2(470, 0), new Vector2(640, 0));
            // Suelo.
            Forma(capa, new Ruta(0, 930).C(600, 910, 1300, 925, 1920, 915).L(1920, 1080).L(0, 1080).Puntos(), X(0x0A1A1F), X(0x010305), new Vector2(0, 915), new Vector2(0, 1080));
            // La trabajadora con la linterna en alto.
            Silueta(capa, 690, 935, 1.1f, X(0x010407), X(0xFFD98A), true, true);
            Forma(capa, Redondeado(lx - 16, ly - 22, 32, 44, 10), X(0x2A1E10));
            Brillo(capa, lx, ly, 36, 36, X(0xFFF4D6));
            // Un CV flotando (recolectable): lo interactivo es cian.
            Forma(capa, Redondeado(1210, 600, 90, 116, 6), X(0xDFFBFF, 0.9f), X(0x8FEAFF, 0.6f), new Vector2(0, 600), new Vector2(0, 716));
            Brillo(capa, 1255, 658, 150, 150, X(0x5FD8F5, 0.45f));
            // Primer plano: cables y una tuberia casi negros que enmarcan.
            d = Trazos(capa);
            d.Polilinea(new Ruta(0, 140).Q(300, 420, 640, 120).Puntos(), X(0x010305), 9);
            d.Polilinea(new Ruta(1180, 0).Q(1300, 320, 1500, 60).Puntos(), X(0x010305), 7);
            Forma(capa, Tubo(new Ruta(1920, 420).C(1800, 500, 1760, 760, 1820, 1080).Puntos(), 120, 160), X(0x010305));
            Particulas(capa, new Rect(lx - 360, ly - 300, 720, 520), 55, 13, X(0xFFD98A), 0.8f, 2.6f);
            Marco(capa, 4, "SILUETA Y LINTERNA", "Casi todo es noche y niebla; la luz es escasa y cuenta la historia. Lo que toca la linterna se enciende por ese lado. Lo interactivo brilla en cian.",
                X(0xF2C14E), new[] { X(0x010406), X(0x0A1C24), X(0x163640), X(0x7A5C32), X(0xF2C14E), X(0xFFD98A), X(0x5FD8F5) });
            MuestraBotones(capa, (c, x, y, activo, texto) => {
                Forma(c, Redondeado(x, y, 260, 64, 12), activo ? X(0x6A4E26) : X(0x1A262B), X(0x0A1216), new Vector2(x, 0), new Vector2(x + 260, 0));
                if (activo) { Brillo(c, x + 24, y + 32, 70, 50, X(0xF2C14E, 0.6f)); Forma(c, Elipse(x + 24, y + 32, 6, 6, 16), X(0xFFF4D6)); }
                else Forma(c, Elipse(x + 24, y + 32, 5, 5, 16), X(0x5FD8F5, 0.8f));
                Rotulo2(c, x + 30, y, 230, 64, texto, 22, activo ? X(0xFFE7B0) : X(0x9AB0B6), FuentesNexus.InterfazNegrita, TextAlignmentOptions.Center, 4);
            });
        }

        // ================================================================ 5 · dibujo animado de los 50 sobre luz

        private void Ronda2_5(RectTransform capa) {
            Cielo(capa, 0, 0, 1920, 1080, S(0, X(0x0A2A30)), S(0.55f, X(0x1E6A6A)), S(0.78f, X(0x6A8A5A)), S(1, X(0x0A1A1C)));
            Brillo(capa, 1250, 520, 900, 620, X(0xF7D07A, 0.4f));
            for (var i = 0; i < 9; i++) Haz(capa, 1250, 300, 1000, 160, -40 + i * 10, X(0xFFF0C0, 0.05f));
            Forma(capa, Arco(1300, 900, 520, 640, 46, 180, 360, 48, 0.03f, 7), X(0x2C7A78), X(0x1E5A5A), new Vector2(0, 260), new Vector2(0, 900));
            Niebla(capa, 0, 400, 1920, 600, X(0x6A9A80), 0f, 0.3f);
            Forma(capa, Arco(1300, 1000, 760, 900, 70, 180, 360, 56, 0.03f, 2), X(0x144A4C), X(0x0C3234), new Vector2(0, 100), new Vector2(0, 1000));
            Forma(capa, new Ruta(0, 960).C(500, 920, 1300, 980, 1920, 940).L(1920, 1080).L(0, 1080).Puntos(), X(0x0F3A3A), X(0x061818), new Vector2(0, 930), new Vector2(0, 1080));
            Particulas(capa, new Rect(900, 200, 900, 600), 50, 17, X(0xFFF0C0), 1f, 3f);
            // Cartel de propaganda colgado: banderola con el eslogan del canon.
            var banda = new Ruta(1080, 250).Q(1420, 300, 1820, 240).L(1830, 360).Q(1420, 420, 1070, 370).Puntos();
            Forma(capa, Mover(banda, 8, 10), X(0x000000, 0.35f));
            Forma(capa, banda, X(0x24343B));
            Rotulo2(capa, 1110, 270, 700, 100, "SU ARQUITECTURA\nES SU REFUGIO", 40, X(0xF2C14E), FuentesNexus.Display, TextAlignmentOptions.Center, 2);
            // ARQUI, la mascota: contorno de tinta por «calco» (cada pieza, primero en tinta y algo mayor).
            Brillo(capa, 760, 640, 520, 520, X(0xFFF0C0, 0.35f));
            Forma(capa, Elipse(770, 960, 230, 34, 40), X(0x000000, 0.35f));
            Arqui(capa, 760, 0);
            Marco(capa, 5, "DIBUJO ANIMADO DE LOS 50 SOBRE LUZ", "Personajes y mascota con la línea gruesa y viva del cartoon de los 50 (ARQUI, del canon), encima de fondos luminosos por capas. El humor del cartel de seguridad, la atmósfera de Ori.",
                X(0xF2C14E), new[] { X(0x0A2A30), X(0x2C7A78), X(0xF7D07A), X(0xF3E3C3), X(0xC9A227), X(0x3E5158), X(0x161616) });
            MuestraBotones(capa, (c, x, y, activo, texto) => {
                Forma(c, Redondeado(x + 6, y + 8, 260, 64, 16), X(0x000000, 0.5f));
                Forma(c, Redondeado(x - 4, y - 4, 268, 72, 20), X(0x161616));
                Forma(c, Redondeado(x, y, 260, 64, 16), activo ? X(0xF2C14E) : X(0xF3E3C3), activo ? X(0xC9A227) : X(0xD8C8A6), new Vector2(0, y), new Vector2(0, y + 64));
                Forma(c, Redondeado(x + 12, y + 8, 120, 14, 7), X(0xFFFFFF, 0.5f));
                Rotulo2(c, x, y + 2, 260, 64, texto, 26, X(0x161616), FuentesNexus.Display, TextAlignmentOptions.Center, 2);
            });
        }

        /// <summary>ARQUI (canon §11.4.5): figura redondeada con casco, guantes blancos y sonrisa fija; aqui con su tablilla, señalando arriba.</summary>
        private void Arqui(RectTransform capa, float ox, float oy) {
            var tinta = X(0x161616);
            void Pieza(List<Vector2> p, Color a, Color b, float grosor = 9) {
                var c = Vector2.zero; foreach (var v in p) c += v; c /= p.Count;
                float r = 0; foreach (var v in p) r += (v - c).magnitude; r /= p.Count;
                Forma(capa, Mover(p, 0, 0, 1 + grosor / Mathf.Max(8, r), c.x, c.y), tinta);
                Forma(capa, p, a, b, new Vector2(c.x - r, c.y - r), new Vector2(c.x + r, c.y + r));
            }
            void Brazo(List<Vector2> eje, Color a) {
                Forma(capa, Tubo(eje, 58, 50), tinta);
                Forma(capa, Tubo(eje, 40, 34), a);
            }
            var overol = X(0x4E6670); var overol2 = X(0x2F3E52);
            var crema = X(0xF3E3C3); var crema2 = X(0xD8BE96);
            var blanco = X(0xFFFFFF); var blanco2 = X(0xD6DCDE);
            var mostaza = X(0xE3C24F); var mostaza2 = X(0xB08A1A);
            // Piernas y botas.
            Pieza(Redondeado(ox - 82, oy + 790, 58, 130, 26), overol, overol2);
            Pieza(Redondeado(ox + 28, oy + 790, 58, 130, 26), overol, overol2);
            Pieza(Elipse(ox - 60, oy + 930, 66, 30, 32), X(0x2A2A2A), X(0x0A0A0A));
            Pieza(Elipse(ox + 70, oy + 930, 66, 30, 32), X(0x2A2A2A), X(0x0A0A0A));
            // Brazo que sostiene la tablilla (detras del cuerpo).
            Brazo(new Ruta(ox - 120, oy + 620).Q(ox - 220, oy + 690, ox - 180, oy + 780).Puntos(), overol);
            Pieza(Redondeado(ox - 300, oy + 680, 150, 190, 12), X(0x8A6038), X(0x5A3A1E));
            Pieza(Redondeado(ox - 286, oy + 702, 122, 154, 6), crema, crema2, 3);
            Pieza(Redondeado(ox - 250, oy + 668, 50, 26, 6), mostaza, mostaza2, 4);
            Pieza(Elipse(ox - 180, oy + 790, 40, 34, 28), blanco, blanco2);
            // Cuerpo: torso de overol en forma de pera, con cinturon mostaza.
            var torso = new Ruta(ox - 110, oy + 580).C(ox - 175, oy + 650, ox - 165, oy + 800, ox - 60, oy + 820)
                .L(ox + 60, oy + 820).C(ox + 165, oy + 800, ox + 175, oy + 650, ox + 110, oy + 580).C(ox + 60, oy + 548, ox - 60, oy + 548, ox - 110, oy + 580).Puntos();
            Pieza(torso, overol, overol2, 10);
            Pieza(Redondeado(ox - 150, oy + 740, 300, 30, 14), mostaza, mostaza2, 5);
            Pieza(Redondeado(ox - 22, oy + 736, 44, 38, 8), X(0xC8D2D6), X(0x7A8A90), 4);
            // Brazo que señala arriba, con su guante de cuatro dedos (indice estirado).
            Brazo(new Ruta(ox + 120, oy + 610).Q(ox + 230, oy + 560, ox + 240, oy + 440).Puntos(), overol);
            Pieza(Redondeado(ox + 208, oy + 420, 64, 26, 12), blanco, blanco2, 6);
            Pieza(Elipse(ox + 240, oy + 392, 40, 34, 28), blanco, blanco2);
            Pieza(Redondeado(ox + 226, oy + 280, 28, 110, 14), blanco, blanco2, 6);
            Pieza(Elipse(ox + 210, oy + 380, 14, 22, 20), blanco, blanco2, 5);
            // Cabeza, casco con lampara de neon (la fusion), cara de sonrisa fija.
            Pieza(Elipse(ox, oy + 440, 132, 122, 56), crema, crema2, 10);
            var casco = new Ruta(ox - 140, oy + 400).C(ox - 140, oy + 250, ox + 140, oy + 250, ox + 140, oy + 400).Puntos();
            Pieza(casco, mostaza, mostaza2, 10);
            Pieza(Redondeado(ox - 172, oy + 388, 344, 32, 16), mostaza, mostaza2, 7);
            Forma(capa, Elipse(ox - 60, oy + 320, 34, 16, 24), X(0xFFFFFF, 0.45f));
            Pieza(Elipse(ox, oy + 300, 30, 26, 32), X(0x8FEAFF), X(0x5FD8F5), 7);
            Brillo(capa, ox, oy + 300, 120, 120, X(0x5FD8F5, 0.6f));
            Forma(capa, Elipse(ox - 44, oy + 448, 15, 26, 24), tinta);
            Forma(capa, Elipse(ox + 44, oy + 448, 15, 26, 24), tinta);
            Forma(capa, Elipse(ox - 40, oy + 438, 5, 7, 12), blanco);
            Forma(capa, Elipse(ox + 48, oy + 438, 5, 7, 12), blanco);
            Forma(capa, Elipse(ox - 92, oy + 498, 22, 13, 20), X(0xE8846A, 0.55f));
            Forma(capa, Elipse(ox + 92, oy + 498, 22, 13, 20), X(0xE8846A, 0.55f));
            Forma(capa, new Ruta(ox - 72, oy + 492).Q(ox, oy + 590, ox + 72, oy + 492).Q(ox, oy + 540, ox - 72, oy + 492).Puntos(), tinta);
            Forma(capa, Elipse(ox, oy + 486, 12, 9, 16), X(0xD8A07A));
        }

        // ================================================================ 6 · vidrio y valvula (la interfaz)

        private void Ronda2_6(RectTransform capa) {
            Cielo(capa, 0, 0, 1920, 1080, S(0, X(0x0B151A)), S(1, X(0x04080A)));
            Brillo(capa, 960, 200, 1200, 600, X(0x2E4A52, 0.6f));
            // Panel esmaltado con su canto.
            Forma(capa, Redondeado(140, 220, 1640, 800, 60), X(0x1C2C33), X(0x0C1519), new Vector2(0, 220), new Vector2(0, 1020));
            var d = Trazos(capa);
            d.Polilinea(new List<Vector2>(Redondeado(146, 226, 1628, 788, 56)) { Redondeado(146, 226, 1628, 788, 56)[0] }, X(0x5E7A84, 0.5f), 2);
            // Tres relojes con bisel de cromo: tiempo, costo, calidad.
            string[] nombres = { "TIEMPO", "COSTO", "CALIDAD" };
            float[] valores = { 0.35f, 0.72f, 0.58f };
            Color[] colores = { X(0x5FD8F5), X(0xC9A227), X(0x5FD8F5) };
            for (var i = 0; i < 3; i++) {
                float cx = 480 + i * 420, cy = 470, r = 170;
                Forma(capa, Elipse(cx, cy + 10, r + 12, r + 12, 64), X(0x000000, 0.4f));
                Forma(capa, Elipse(cx, cy, r, r, 64), X(0xF2F6F7), X(0x3E4C54), new Vector2(0, cy - r), new Vector2(0, cy + r));
                Forma(capa, Elipse(cx, cy, r - 16, r - 16, 64), X(0x2A363C), X(0xD4DEE2), new Vector2(0, cy - r), new Vector2(0, cy + r));
                Forma(capa, Elipse(cx, cy, r - 26, r - 26, 64), X(0x12222A), X(0x060C10), new Vector2(0, cy - r), new Vector2(0, cy + r));
                var a0 = 135f; var a1 = 405f;
                Forma(capa, Arco(cx, cy, r - 40, r - 40, 10, a0, Mathf.Lerp(a0, a1, valores[i]), 40), A(colores[i], 0.95f));
                Brillo(capa, cx, cy, r, r, A(colores[i], 0.22f));
                d = Trazos(capa);
                for (var t = 0; t <= 20; t++) {
                    var a = Mathf.Deg2Rad * Mathf.Lerp(a0, a1, t / 20f);
                    var largo = t % 5 == 0 ? 18 : 9;
                    d.Linea(cx + Mathf.Cos(a) * (r - 52), cy + Mathf.Sin(a) * (r - 52), cx + Mathf.Cos(a) * (r - 52 - largo), cy + Mathf.Sin(a) * (r - 52 - largo), X(0xDFE8EA, 0.8f), t % 5 == 0 ? 3 : 1.5f);
                }
                var an = Mathf.Deg2Rad * Mathf.Lerp(a0, a1, valores[i]);
                var punta = new Vector2(cx + Mathf.Cos(an) * (r - 60), cy + Mathf.Sin(an) * (r - 60));
                var perp = new Vector2(-Mathf.Sin(an), Mathf.Cos(an)) * 7;
                Forma(capa, new List<Vector2> { new Vector2(cx, cy) + perp, punta, new Vector2(cx, cy) - perp }, X(0xFF7A3C));
                Forma(capa, Elipse(cx, cy, 18, 18, 24), X(0xF2F6F7), X(0x5A6870), new Vector2(0, cy - 18), new Vector2(0, cy + 18));
                // El cristal: un reflejo curvo arriba.
                Forma(capa, new Ruta(cx - r + 40, cy - 40).C(cx - r + 60, cy - r + 30, cx + r - 60, cy - r + 30, cx + r - 40, cy - 40).Q(cx, cy - r + 70, cx - r + 40, cy - 40).Puntos(), X(0xFFFFFF, 0.14f), X(0xFFFFFF, 0f), new Vector2(0, cy - r), new Vector2(0, cy - 40));
                Rotulo2(capa, cx - 120, cy + 70, 240, 30, nombres[i], 20, X(0xDFE8EA, 0.85f), FuentesNexus.InterfazNegrita, TextAlignmentOptions.Center, 6);
            }
            // Visor nixie: la hora en tubos de neon naranja.
            Forma(capa, Redondeado(700, 700, 520, 140, 24), X(0x0A1418), X(0x020507), new Vector2(0, 700), new Vector2(0, 840));
            Forma(capa, Redondeado(700, 700, 520, 140, 24), X(0x8FEAFF, 0.06f), X(0x8FEAFF, 0f), new Vector2(0, 700), new Vector2(0, 760));
            var digitos = "14:00";
            for (var i = 0; i < digitos.Length; i++) {
                var x = 760 + i * 92;
                if (digitos[i] != ':') Forma(capa, Redondeado(x - 4, 716, 72, 108, 30), X(0x2A363C, 0.6f));
                Brillo(capa, x + 32, 770, 70, 80, X(0xFF7A2C, 0.55f));
                Rotulo2(capa, x, 712, 64, 110, digitos[i].ToString(), 92, X(0xFFB066), FuentesNexus.Interfaz, TextAlignmentOptions.Center);
            }
            Rotulo2(capa, 700, 846, 520, 30, "DÍA 3 · SECTOR 7", 18, X(0xDFE8EA, 0.6f), FuentesNexus.InterfazNegrita, TextAlignmentOptions.Center, 8);
            // Valvulas encendidas.
            for (var i = 0; i < 4; i++) {
                float x = 260 + i * 100;
                Brillo(capa, x + 30, 800, 70, 110, X(0xFF9A3C, 0.4f));
                Forma(capa, Redondeado(x, 720, 60, 150, 30), X(0xBFE6EE, 0.22f), X(0xBFE6EE, 0.05f), new Vector2(x, 0), new Vector2(x + 60, 0));
                Brillo(capa, x + 30, 800, 14, 40, X(0xFFD7A0, 0.9f));
                Forma(capa, Redondeado(x - 6, 866, 72, 34, 8), X(0xE6EEF0), X(0x4A5A62), new Vector2(0, 866), new Vector2(0, 900));
            }
            // Interruptores de cromo con la etiqueta retroiluminada.
            string[] et = { "REPOSO", "ACTIVO", "PELIGRO" };
            for (var i = 0; i < 3; i++) {
                float x = 1340, y = 712 + i * 96;
                Forma(capa, Redondeado(x, y, 320, 72, 36), X(0xF2F6F7), X(0x4A5A62), new Vector2(0, y), new Vector2(0, y + 72));
                var luz = i == 1 ? X(0x5FD8F5) : i == 2 ? X(0xC9A227) : X(0x2A363C);
                if (i > 0) Brillo(capa, x + 160, y + 36, 200, 60, A(luz, 0.4f));
                Forma(capa, Redondeado(x + 10, y + 10, 300, 52, 26), i == 0 ? X(0x14222A) : A(luz, 0.85f), i == 0 ? X(0x0A1418) : A(luz * 0.55f, 1f), new Vector2(0, y + 10), new Vector2(0, y + 62));
                Rotulo2(capa, x, y, 320, 72, et[i], 24, i == 0 ? X(0x9AB0B6) : X(0x0A1418), FuentesNexus.Display, TextAlignmentOptions.Center, 4);
            }
            Marco(capa, 6, "VIDRIO Y VÁLVULA", "La interfaz como instrumental de 1957: biseles de cromo, cristal con reflejo, agujas, válvulas encendidas y visor nixie. La luz sale de dentro de los aparatos; el panel no brilla.",
                X(0xFF9A3C), new[] { X(0x04080A), X(0x1C2C33), X(0xF2F6F7), X(0x4A5A62), X(0x5FD8F5), X(0xC9A227), X(0xFF7A2C) });
        }
    }
}
#endif
