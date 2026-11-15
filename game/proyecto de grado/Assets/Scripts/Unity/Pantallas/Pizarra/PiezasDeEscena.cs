using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using UnityEngine;
using UnityEngine.UI;
using L = Nexus.Unity.Tema.LuzUI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// (2026-11-15, direccion de arte) Los objetos del mundo con que se cuentan las escenas nuevas de pizarra, dibujados
    /// dentro de una vitrina (Tizador.Vitrina). Cada metrica tiene SU objeto y lo repite en sus cuatro escenas:
    ///   avance     → la via del monorriel que se tiende hacia la torre del cliente (exterior, 1957);
    ///   cansancio  → tu puesto: la guirnalda de bombillas es la energia que te queda;
    ///   moral      → las tazas del equipo y su vapor;
    ///   riesgo     → la caldera y su manometro.
    /// Las personas no se dibujan: se cuentan por sus cosas (silla, abrigo, taza). Paleta: docs/08-arte/STYLE.md §2.
    /// </summary>
    public static partial class DibujosDeEscena {
        private static Color H(int rgb, float a = 1) { return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, a); }
        private static Color A(Color c, float a) { return NexusTheme.Alfa(c, a); }

        // Interior (bioluminiscencia)
        private static readonly Color Abismo = H(0x03101A), Nave = H(0x0A3442), Horizonte = H(0x1B6F7C), Niebla = H(0x2A8C98);
        private static readonly Color CostillaLejos = H(0x3FA3B0), CostillaMedia = H(0x1E6470), CostillaCerca = H(0x123F4A);
        private static readonly Color PrimerPlano = H(0x07222A), Silueta = H(0x02090C);
        private static readonly Color Nucleo = H(0x5FD8F5), NucleoClaro = H(0xBFF6FF), Ambar = H(0xF2C14E), BombillaCentro = H(0xFFF1C4);
        // Exterior (streamline atomico)
        private static readonly Color Noche = H(0x140C2E), Ciruela = H(0x4A2150), Terracota = H(0xC2504A), Atardecer = H(0xF08A3C);
        private static readonly Color Sol = H(0xF7D07A), SiluetaFuera = H(0x1A0C1E);
        // Lo que informa
        private static readonly Color Tinta = H(0xE8F4F2), TintaSuave = H(0x8DB2B6), Menta = H(0x7ED9AE), Coral = H(0xEF7A5E), Aviso = H(0xE3C24F);
        private static readonly Color Cromo = H(0xC9D6D8), CromoOscuro = H(0x4E6B72);

        private static List<Vector2> Caja(float x, float y, float w, float h) {
            return new List<Vector2> { new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h) };
        }

        private static List<Vector2> Pts(params float[] xy) {
            var r = new List<Vector2>(xy.Length / 2);
            for (var i = 0; i + 1 < xy.Length; i += 2) r.Add(new Vector2(xy[i], xy[i + 1]));
            return r;
        }

        /// <summary>Un aro (dos medias bandas: un aro cerrado en un solo poligono no se triangula bien).</summary>
        private static void Anillo(Tizador v, float cx, float cy, float r, float g, Color c, float giro = 0, float achatado = 1, bool luz = false) {
            for (var m = 0; m < 2; m++) {
                var banda = L.Arco(cx, cy, r + g / 2, (r + g / 2) * achatado, g, m * 180, m * 180 + 180, 28);
                if (giro != 0) banda = Tizador.Rotar(banda, cx, cy, giro);
                if (luz) v.FormaDeLuz(banda, c); else v.Forma(banda, c);
            }
        }

        // ================================================================ fondos

        /// <summary>
        /// El interior del bunker: nave verdeazulada que cae al abismo, costillas en arco perdidas en la niebla, un velo de
        /// niebla abajo, el suelo en sombra y motas alrededor de la fuente de luz (lx, ly).
        /// </summary>
        private static void FondoInterior(Tizador v, float lx, float ly, Color luz, float intensidad = 1, int semilla = 1) {
            v.Forma(Caja(0, 0, 600, 300), Nave, Abismo);
            v.Halo(lx, ly, 340, 230, A(Horizonte, 0.55f * intensidad));
            var dx = (semilla % 3 - 1) * 70;
            v.Forma(L.Arco(300 + dx, 340, 390, 350, 14, 180, 360, 48, 0.025f, semilla), A(CostillaLejos, 0.20f));
            v.Forma(L.Arco(300 + dx, 340, 300, 280, 10, 180, 360, 48, 0.03f, semilla + 3), A(CostillaLejos, 0.12f));
            v.Velo(0, 120, 600, 180, Niebla, 0, 0.26f);
            v.Forma(new RutaDeCurva(0, 266).C(200, 258, 420, 262, 600, 256).L(600, 300).L(0, 300).Puntos(), PrimerPlano, Silueta);
            v.Halo(lx, ly, 120, 90, A(luz, 0.16f * intensidad));
            v.Motas(lx, ly, 230, 120, 16, semilla, A(Color.Lerp(luz, Color.white, 0.4f), intensidad));
        }

        /// <summary>
        /// El exterior de 1957: cielo de atardecer (noche 0) a anochecido (noche 1), el sol en (sx, sy), una ciudad de
        /// capsulas al fondo velada por la bruma y el suelo en silueta desde y = 232.
        /// </summary>
        private static void FondoExterior(Tizador v, float noche, float sx, float sy, int semilla = 1) {
            Color C(Color tarde, Color oscuro) { return Color.Lerp(tarde, oscuro, noche); }
            v.Cielo(0, 0, 600, 300,
                    L.Parada(0, C(Noche, H(0x07051A))), L.Parada(0.30f, C(Ciruela, Noche)), L.Parada(0.55f, C(Terracota, Ciruela)),
                    L.Parada(0.70f, C(Atardecer, Terracota)), L.Parada(0.79f, C(Sol, H(0xE07040))), L.Parada(1, C(Atardecer, Ciruela)));
            if (noche > 0.25f) {
                var azar = new System.Random(semilla * 31);
                for (var i = 0; i < 22; i++)
                    v.Halo((float)azar.NextDouble() * 600, (float)azar.NextDouble() * 110, 2 + (float)azar.NextDouble() * 3, A(Tinta, 0.6f * noche));
            }
            var luzSol = 1 - noche * 0.8f;
            v.Halo(sx, sy, 260, 150, A(Atardecer, 0.55f * luzSol));
            v.Halo(sx, sy, 80, A(Sol, 0.85f * luzSol));
            v.Forma(L.Elipse(sx, sy, 24, 24), A(BombillaCentro, luzSol), A(Sol, luzSol));

            // La ciudad del fondo: capsulas y cupulas, del color del cielo bajo, sin cuadricula.
            var lejos = A(Color.Lerp(Ciruela, Terracota, 0.35f), 0.75f);
            var az = new System.Random(semilla * 7 + 3);
            var x = 8f;
            while (x < 600) {
                var w = 10 + (float)az.NextDouble() * 14;
                var h = 18 + (float)az.NextDouble() * 46;
                if (az.NextDouble() < 0.3) v.Forma(L.Elipse(x + w, 232, w * 1.4f, h * 0.45f, 24, 180, 360), lejos);
                else v.Forma(L.Redondeado(x, 232 - h, w, h + 4, w / 2), lejos);
                x += w + 6 + (float)az.NextDouble() * 26;
            }
            v.Velo(0, 160, 600, 80, C(Atardecer, Ciruela), 0, 0.45f);
            v.Forma(new RutaDeCurva(0, 236).C(150, 226, 380, 240, 600, 228).L(600, 300).L(0, 300).Puntos(), SiluetaFuera, H(0x0B0610));
        }

        // ================================================================ exterior: la torre del cliente y la via

        /// <summary>La torre del cliente: fuste que se afina, platillo con ventanas, cupula de cristal y un atomo de neon arriba.</summary>
        private static void TorreDelCliente(Tizador v, float cx, float yBase, float alto, float luz, Color baliza) {
            var top = yBase - alto;
            var fuste = new RutaDeCurva(cx - 24, yBase).C(cx - 12, yBase - 22, cx - 9, top + 60, cx - 7, top + 18)
                .L(cx + 7, top + 18).C(cx + 9, top + 60, cx + 12, yBase - 22, cx + 24, yBase).Puntos();
            v.Forma(fuste, Color.Lerp(SiluetaFuera, Terracota, 0.45f), SiluetaFuera, new Vector2(cx - 24, 0), new Vector2(cx + 20, 0));
            // Aletas en boomerang a los lados de la base.
            v.Forma(new RutaDeCurva(cx - 14, yBase - 40).Q(cx - 40, yBase - 22, cx - 44, yBase).L(cx - 20, yBase).Puntos(), SiluetaFuera);
            v.Forma(new RutaDeCurva(cx + 14, yBase - 40).Q(cx + 40, yBase - 22, cx + 44, yBase).L(cx + 20, yBase).Puntos(), H(0x12081A));
            // Ventanas en rendija, por grupos (nunca en cuadricula).
            var ys = new[] { top + 44, top + 62, top + 98, yBase - 60 };
            for (var i = 0; i < ys.Length; i++) {
                var on = luz > i * 0.22f;
                v.Forma(L.Redondeado(cx - 2, ys[i], 4, 12, 2), on ? A(Ambar, 0.95f) : A(Ciruela, 0.8f));
                if (on) v.Halo(cx, ys[i] + 6, 10, A(Ambar, 0.35f));
            }
            // Platillo, ventanas del platillo y cupula.
            v.Forma(L.Elipse(cx, top + 18, 36, 8), Color.Lerp(SiluetaFuera, Ciruela, 0.9f), SiluetaFuera);
            v.Forma(L.Elipse(cx, top + 16, 34, 3), A(Sol, 0.35f));
            for (var i = 0; i < 5; i++) {
                var wx = cx - 22 + i * 11;
                var on = luz > 0.15f + i * 0.12f;
                v.Forma(L.Elipse(wx, top + 21, 2.2f, 1.6f, 10), on ? Ambar : A(Ciruela, 0.9f));
                if (on) v.Halo(wx, top + 21, 7, A(Ambar, 0.4f));
            }
            v.Forma(L.Elipse(cx, top + 12, 15, 13, 24, 180, 360), A(Nucleo, 0.30f), A(Nucleo, 0.10f));
            v.Forma(L.Arco(cx, top + 12, 12, 10, 2, 200, 250, 10), A(Color.white, 0.45f));
            // Antena con atomo.
            v.Cable(Pts(cx, top, cx, top - 22), 2, 1.2f, Color.Lerp(SiluetaFuera, Ciruela, 0.6f));
            var ay = top - 32;
            v.Halo(cx, ay, 46, A(baliza, 0.18f + 0.45f * luz));
            for (var i = 0; i < 3; i++) Anillo(v, cx, ay, 12, 1.6f, A(baliza, 0.55f + 0.45f * luz), i * 60, 0.36f, true);
            v.Halo(cx, ay, 6, A(NucleoClaro, 0.9f));
        }

        /// <summary>
        /// La via del monorriel de x0 a x1 a la altura y, en n tramos con pilares en boomerang hasta el suelo. 'hechos' tramos
        /// estan tendidos y encendidos (puede ser fraccion); los demas son solo el trazo de obra, discontinuo y apagado.
        /// </summary>
        private static void Via(Tizador v, float x0, float x1, float y, int n, float hechos, float suelo, Color luz, bool numeros = false) {
            var tramo = (x1 - x0) / n;
            var obra = A(Sol, 0.30f);
            for (var i = 0; i <= n; i++) {
                var px = x0 + i * tramo;
                if (i <= hechos + 0.001f) {
                    v.Forma(new RutaDeCurva(px - 4, y + 7).C(px - 4, y + 30, px - 11, suelo - 16, px - 14, suelo)
                        .L(px + 14, suelo).C(px + 11, suelo - 16, px + 4, y + 30, px + 4, y + 7).Puntos(),
                        Color.Lerp(SiluetaFuera, Ciruela, 0.7f), SiluetaFuera);
                } else {
                    for (var yy = y + 10; yy < suelo - 4; yy += 11) v.Forma(Caja(px - 1, yy, 2, 6), A(Sol, 0.18f));
                }
            }
            for (var i = 0; i < n; i++) {
                var xa = x0 + i * tramo;
                var f = Mathf.Clamp01(hechos - i);
                if (f > 0) {
                    var largo = tramo * f;
                    v.Forma(L.Redondeado(xa + 1, y, largo - 2, 8, 3), Color.Lerp(SiluetaFuera, Ciruela, 0.6f), SiluetaFuera);
                    v.Neon(Pts(xa + 3, y + 0.5f, xa + largo - 3, y + 0.5f), 1.1f, luz);
                }
                if (f < 1) {
                    for (var xx = xa + tramo * f + 3; xx < xa + tramo - 3; xx += 12) {
                        var w = Mathf.Min(6, xa + tramo - 3 - xx);
                        v.Forma(Caja(xx, y, w, 1.6f), obra);
                        v.Forma(Caja(xx, y + 6.4f, w, 1.6f), obra);
                    }
                }
                if (numeros) v.Rotulo(xa + tramo / 2, y + 20, (i + 1).ToString(), 11, f >= 1 ? A(Tinta, 0.85f) : A(Sol, 0.5f));
            }
        }

        /// <summary>La capsula del monorriel sobre la via (y = cara de arriba de la via), con el morro a la derecha.</summary>
        private static void Capsula(Tizador v, float x, float y, float largo, Color ventana, bool estela) {
            if (estela)
                for (var i = 0; i < 3; i++) {
                    var ey = y - 18 + i * 6;
                    v.Cable(Pts(x - 46 + i * 8, ey, x - 4, ey), 0.6f, 2.4f, A(Sol, 0.45f - i * 0.1f));
                }
            var cuerpo = new RutaDeCurva(x, y - 10).Q(x + 4, y - 22, x + largo * 0.35f, y - 24)
                .C(x + largo * 0.8f, y - 25, x + largo, y - 17, x + largo, y - 10)
                .C(x + largo, y - 4, x + largo * 0.85f, y - 2, x + largo * 0.7f, y - 2)
                .L(x + 8, y - 2).Q(x, y - 3, x, y - 10).Puntos();
            v.Halo(x + largo * 0.5f, y - 12, largo * 0.7f, 22, A(ventana, 0.18f));
            v.Forma(cuerpo, Cromo, CromoOscuro);
            v.Cable(new RutaDeCurva(x + 6, y - 17).Q(x + largo * 0.35f, y - 25, x + largo * 0.8f, y - 21).Puntos(), 0.8f, 1.6f, A(Color.white, 0.55f));
            v.FormaDeLuz(L.Redondeado(x + largo * 0.32f, y - 19, largo * 0.48f, 5.5f, 2.7f), A(ventana, 0.95f));
            v.Halo(x + largo * 0.56f, y - 16, largo * 0.35f, 9, A(ventana, 0.35f));
            v.Forma(L.Redondeado(x + 9, y - 8, largo * 0.66f, 2.4f, 1.2f), Aviso);
        }

        /// <summary>
        /// La via vista como grafica: un tramo por dia, que sube lo que se avanzo ese dia, con sus pilares hasta el suelo.
        /// 'torcido' inclina el ultimo pilar (la obra hecha deprisa, a ultima hora).
        /// </summary>
        private static void Rampa(Tizador v, List<Vector2> puntos, Color luz, float suelo, bool torcido) {
            for (var i = 1; i < puntos.Count; i++) {
                var q = puntos[i];
                var pie = torcido && i == puntos.Count - 1 ? -12f : 0f;
                v.Forma(Pts(q.x - 3, q.y + 6, q.x + 3, q.y + 6, q.x + 9 + pie, suelo, q.x - 9 + pie, suelo),
                        Color.Lerp(SiluetaFuera, Ciruela, 0.7f), SiluetaFuera);
            }
            for (var i = 1; i < puntos.Count; i++) {
                Vector2 a = puntos[i - 1], b = puntos[i];
                v.Cable(Pts(a.x, a.y + 3.5f, b.x, b.y + 3.5f), 7, 7, Color.Lerp(SiluetaFuera, Ciruela, 0.6f));
                v.Neon(Pts(a.x, a.y - 0.5f, b.x, b.y - 0.5f), 1.1f, luz);
            }
        }

        // ================================================================ interior: el puesto de trabajo

        /// <summary>Guirnalda de bombillas colgada en catenaria. Devuelve donde cuelga cada bombilla (centro del cristal).</summary>
        private static List<Vector2> Guirnalda(Tizador v, float x0, float y0, float x1, float y1, float cuelga, int n, int encendidas, int parpadea = -1) {
            var a = new Vector2(x0, y0); var b = new Vector2(x1, y1); var c = new Vector2((x0 + x1) / 2, Mathf.Max(y0, y1) + cuelga * 2);
            v.Cable(Tizador.Bezier(a, c, b, 40), 1.8f, 1.8f, H(0x0C2A30));
            var donde = new List<Vector2>();
            for (var i = 0; i < n; i++) {
                var t = (i + 0.5f) / n; var u = 1 - t;
                var p = u * u * a + 2 * u * t * c + t * t * b;
                var g = new Vector2(p.x, p.y + 13);
                donde.Add(g);
                v.Forma(L.Redondeado(p.x - 3, p.y - 1, 6, 7, 2), H(0x2B3A3E));
                if (i < encendidas) {
                    var halo = v.Halo(g.x, g.y, 38, A(Ambar, 0.42f));
                    var nucleo = v.Halo(g.x, g.y, 13, A(BombillaCentro, 0.95f));
                    var vidrio = v.Forma(L.Elipse(g.x, g.y, 6, 8, 20), BombillaCentro, Ambar);
                    if (i == parpadea) { v.Parpadeo(halo, 0.1f, i * 0.37f); v.Parpadeo(nucleo, 0.15f, i * 0.37f); v.Parpadeo(vidrio, 0.35f, i * 0.37f); }
                } else {
                    v.Forma(L.Elipse(g.x, g.y, 6, 8, 20), H(0x1E3A40), H(0x0B1E23));
                    v.Forma(L.Elipse(g.x - 2, g.y - 3, 1.4f, 2.4f, 10), A(Color.white, 0.22f));
                }
            }
            return donde;
        }

        /// <summary>Tablero de escritorio de x0 a x1 con su filo de luz y patas en boomerang.</summary>
        private static void Escritorio(Tizador v, float x0, float x1, float y, Color filo) {
            foreach (var lx in new[] { x0 + 34, x1 - 34 }) {
                // La pata se abre hacia fuera: el borde izquierdo baja al pie izquierdo y el derecho, al derecho.
                var fuera = lx < (x0 + x1) / 2;
                float pieI = lx + (fuera ? -20 : 8), pieD = lx + (fuera ? -8 : 20);
                v.Forma(new RutaDeCurva(lx - 5, y + 9).C(lx - 5, y + 40, pieI + 2, 286, pieI, 300)
                    .L(pieD, 300).C(pieD - 2, 286, lx + 5, y + 40, lx + 5, y + 9).Puntos(), PrimerPlano, Silueta);
            }
            v.Forma(L.Redondeado(x0, y, x1 - x0, 11, 5), CostillaCerca, PrimerPlano);
            v.Forma(L.Redondeado(x0 + 8, y, x1 - x0 - 16, 2, 1), A(filo, 0.75f));
        }

        /// <summary>
        /// Silla de oficina de perfil, mirando a la mesa (a la derecha); y = altura del asiento. Con 'abrigo', el abrigo de
        /// trabajo colgado del respaldo con su cinta mostaza: la persona se cuenta por sus cosas, sin dibujarla.
        /// </summary>
        private static void Silla(Tizador v, float x, float y, bool abrigo, Color filo) {
            // Pie: columna, cruceta y dos ruedas.
            v.Forma(L.Redondeado(x - 3, y + 4, 6, 284 - y, 3), Silueta);
            v.Forma(L.Redondeado(x - 28, 282, 56, 5, 2.5f), PrimerPlano, Silueta);
            v.Forma(L.Elipse(x - 25, 291, 4.5f, 4.5f, 12), H(0x0B1E23));
            v.Forma(L.Elipse(x + 25, 291, 4.5f, 4.5f, 12), H(0x0B1E23));
            // Asiento y brazo.
            v.Forma(L.Redondeado(x - 32, y - 5, 64, 11, 5), CostillaCerca, PrimerPlano);
            v.Forma(L.Redondeado(x - 26, y - 5, 54, 2, 1), A(filo, 0.6f));
            v.Forma(new RutaDeCurva(x - 18, y - 4).L(x - 16, y - 26).L(x + 16, y - 26).Q(x + 22, y - 26, x + 22, y - 21)
                .L(x - 11, y - 21).L(x - 13, y - 4).Puntos(), PrimerPlano);
            // Respaldo: una placa alta y estrecha, inclinada hacia atras.
            v.Forma(new RutaDeCurva(x - 30, y - 2).Q(x - 30, y - 30, x - 40, y - 54).L(x - 34, y - 56).Q(x - 22, y - 30, x - 22, y - 2).Puntos(), Silueta);
            v.Forma(Tizador.Rotar(L.Redondeado(x - 50, y - 92, 16, 52, 7), x - 42, y - 66, -8), CostillaCerca, PrimerPlano);
            if (!abrigo) return;
            // El abrigo, doblado sobre el respaldo: corto por delante, largo por detras.
            var tela = new RutaDeCurva(x - 56, y - 86).Q(x - 46, y - 100, x - 32, y - 88)
                .C(x - 30, y - 74, x - 30, y - 62, x - 28, y - 52).L(x - 36, y - 50)
                .C(x - 42, y - 40, x - 52, y - 10, x - 56, y + 18).L(x - 68, y + 14)
                .C(x - 66, y - 20, x - 62, y - 60, x - 56, y - 86).Puntos();
            v.Forma(tela, H(0x2B4F57), H(0x112A31));
            v.Forma(Pts(x - 66, y - 6, x - 50, y - 9, x - 51, y - 3, x - 67, y), Aviso);
            v.Forma(L.Arco(x - 44, y - 82, 12, 10, 1.6f, 200, 290, 12), A(filo, 0.45f));
        }

        /// <summary>
        /// Una taza sobre la mesa (yBase = la mesa). estilo 0: esmaltada; 1: vaso termico; 2: taza con plato; 3: taza oscura
        /// desportillada con la cucharilla dentro. 'vapor' de 0 (fria) a 1 (recien hecha).
        /// </summary>
        private static void Taza(Tizador v, float x, float yBase, int estilo, float vapor, Color luz) {
            float arriba;
            switch (estilo) {
                case 1:
                    arriba = yBase - 40;
                    v.Forma(Pts(x - 10, arriba, x + 10, arriba, x + 8, yBase, x - 8, yBase), CostillaLejos, CostillaMedia);
                    v.Forma(L.Redondeado(x - 11, arriba - 6, 22, 7, 3), H(0x1E3A40));
                    v.Forma(Caja(x - 9.4f, arriba + 14, 18.8f, 4), A(Nucleo, 0.8f));
                    break;
                case 2:
                    arriba = yBase - 20;
                    v.Forma(L.Elipse(x, yBase - 1, 24, 4.5f, 24), H(0xD8EAE6), H(0x8FB0AE));
                    v.Forma(new RutaDeCurva(x - 16, arriba).L(x + 16, arriba).C(x + 16, yBase - 6, x + 8, yBase - 3, x, yBase - 3)
                        .C(x - 8, yBase - 3, x - 16, yBase - 6, x - 16, arriba).Puntos(), H(0xE6F1EE), H(0x9DBDB9));
                    v.Forma(L.Arco(x + 16, arriba + 7, 6, 5, 2.4f, -90, 90, 12), H(0xBFD8D4));
                    v.Forma(L.Elipse(x, arriba, 16, 3, 20), H(0x3A2A1E));
                    break;
                case 3:
                    arriba = yBase - 26;
                    v.Forma(L.Redondeado(x - 13, arriba, 26, 26, 4), H(0x5B6B70), H(0x2A3538));
                    v.Forma(L.Arco(x + 13, arriba + 12, 8, 7, 3, -90, 90, 12), H(0x46555A));
                    v.Forma(L.Elipse(x, arriba, 13, 3.2f, 20), H(0x161E20));
                    v.Forma(Pts(x - 6, arriba - 2.5f, x - 1, arriba - 3, x - 3, arriba + 3), H(0x161E20));
                    v.Cable(Pts(x + 3, arriba + 1, x + 12, arriba - 16), 2.2f, 1.6f, Cromo);
                    break;
                default:
                    arriba = yBase - 26;
                    v.Forma(L.Redondeado(x - 13, arriba, 26, 26, 4), H(0xDCEDE9), H(0x8CAEAA));
                    v.Forma(L.Arco(x + 13, arriba + 12, 8, 7, 3, -90, 90, 12), H(0xC3DBD7));
                    v.Forma(Caja(x - 13, arriba + 1, 26, 2.4f), H(0x1F4652));
                    v.Forma(L.Elipse(x, arriba, 13, 3.2f, 20), H(0x2A3F44));
                    v.Forma(L.Elipse(x - 7, arriba + 18, 1.6f, 1.2f, 8), H(0x1F4652));
                    break;
            }
            v.Halo(x - 6, arriba + 8, 14, A(luz, 0.18f));
            if (vapor <= 0) return;
            var volutas = vapor > 0.7f ? 3 : vapor > 0.3f ? 2 : 1;
            for (var i = 0; i < volutas; i++) {
                var eje = new List<Vector2>();
                var x0 = x - 5 + i * 5;
                var alto = 22 + vapor * 34 - i * 4;
                for (var k = 0; k <= 14; k++) {
                    var t = k / 14f;
                    eje.Add(new Vector2(x0 + Mathf.Sin(t * 5.2f + i * 1.7f) * (2 + 5 * t), arriba - 4 - t * alto));
                }
                var hilo = v.Cable(eje, 3.5f + 3 * vapor, 0.4f, A(Tinta, 0.16f + 0.14f * vapor));
                v.Latido(hilo, 0.45f, 1, 2.4f + i * 0.6f, i * 0.3f);
            }
        }

        /// <summary>Monitor de los 50 sobre la mesa: carcasa redondeada, pantalla con renglones de codigo (uno por color).</summary>
        private static void Monitor(Tizador v, float cx, float yBase, float w, float h, Color brillo, params Color[] renglones) {
            v.Forma(Pts(cx - 10, yBase - 14, cx + 10, yBase - 14, cx + 16, yBase - 2, cx - 16, yBase - 2), H(0x1D3237));
            v.Forma(L.Elipse(cx, yBase - 1.5f, 34, 3.5f, 20), H(0x2E4A50), H(0x13262B));
            var top = yBase - 14 - h;
            v.Halo(cx, top + h * 0.45f, w * 0.8f, h * 0.75f, A(brillo, 0.16f));
            v.Forma(L.Redondeado(cx - w / 2, top, w, h, 18), H(0x2E4A50), H(0x10232A));
            v.Forma(L.Redondeado(cx - w / 2 + 3, top + 2, w - 6, 3, 1.5f), A(Color.white, 0.12f));
            var px = cx - w / 2 + 11; var py = top + 10; var pw = w - 22; var ph = h - 30;
            v.Forma(L.Redondeado(px, py, pw, ph, 12), H(0x0E3340), H(0x05161B));
            v.Halo(cx, py + ph / 2, pw * 0.55f, ph * 0.55f, A(brillo, 0.18f));
            var anchos = new[] { 0.62f, 0.44f, 0.78f, 0.36f, 0.58f, 0.70f, 0.40f, 0.52f };
            var paso = Mathf.Min(11, (ph - 14) / Mathf.Max(1, renglones.Length));
            for (var i = 0; i < renglones.Length; i++) {
                var sangria = (i % 3 == 1 ? 10 : i % 3 == 2 ? 18 : 0);
                var rw = (pw - 26 - sangria) * anchos[i % anchos.Length];
                var ry = py + 9 + i * paso;
                v.Forma(L.Redondeado(px + 10 + sangria, ry, rw, 3.6f, 1.8f), A(renglones[i], 0.9f));
                if (renglones[i] == Coral) {
                    v.Halo(px + 10 + sangria + rw + 6, ry + 2, 9, A(Coral, 0.7f));
                    v.Halo(px + 10 + sangria + rw / 2, ry + 2, rw * 0.6f, 6, A(Coral, 0.22f));
                }
            }
            v.Forma(L.Elipse(cx + w / 2 - 16, top + h - 10, 3.2f, 3.2f, 12), Cromo, CromoOscuro);
            v.Forma(L.Elipse(cx + w / 2 - 28, top + h - 10, 3.2f, 3.2f, 12), Cromo, CromoOscuro);
            v.Halo(cx - w / 2 + 16, top + h - 10, 5, A(brillo, 0.8f));
        }

        /// <summary>Ojo de buey con aro cromado: dentro, la noche con luna o el amanecer.</summary>
        private static void OjoDeBuey(Tizador v, float cx, float cy, float r, bool noche) {
            v.Forma(L.Elipse(cx, cy, r, r, 40), Cromo, CromoOscuro);
            var ri = r * 0.8f;
            if (noche) {
                v.Forma(L.Elipse(cx, cy, ri, ri, 40), H(0x07051A), Noche);
                var az = new System.Random(5);
                for (var i = 0; i < 9; i++) {
                    var a = (float)az.NextDouble() * Mathf.PI * 2; var d = (float)az.NextDouble() * ri * 0.8f;
                    v.Halo(cx + Mathf.Cos(a) * d, cy + Mathf.Sin(a) * d, 2.5f, A(Tinta, 0.7f));
                }
                v.Halo(cx + ri * 0.25f, cy - ri * 0.2f, ri * 0.8f, A(NucleoClaro, 0.2f));
                v.Forma(Tizador.Luna(cx + ri * 0.25f, cy - ri * 0.2f, ri * 0.32f), BombillaCentro);
            } else {
                v.Forma(L.Elipse(cx, cy, ri, ri, 40), Terracota, Sol);
                v.Halo(cx - ri * 0.1f, cy + ri * 0.35f, ri * 1.1f, A(Sol, 0.9f));
                v.Forma(L.Elipse(cx - ri * 0.1f, cy + ri * 0.45f, ri * 0.3f, ri * 0.3f, 24), BombillaCentro);
                v.Forma(L.Redondeado(cx - ri * 0.8f, cy + ri * 0.55f, ri * 1.6f, ri * 0.5f, 2), A(SiluetaFuera, 0.85f));
            }
            v.Forma(L.Arco(cx, cy, ri * 0.9f, ri * 0.9f, 2.4f, 200, 250, 12), A(Color.white, 0.3f));
            for (var i = 0; i < 8; i++) {
                var a = i * Mathf.PI / 4 + 0.39f;
                v.Forma(L.Elipse(cx + Mathf.Cos(a) * r * 0.9f, cy + Mathf.Sin(a) * r * 0.9f, 1.8f, 1.8f, 8), H(0x2B3A3E));
            }
        }

        /// <summary>Reloj de pared con bisel cromado; las agujas brillan porque informan.</summary>
        private static void Reloj(Tizador v, float cx, float cy, float r, int horas, int minutos, Color agujas) {
            v.Forma(L.Elipse(cx, cy, r, r, 40), Cromo, CromoOscuro);
            v.Forma(L.Elipse(cx, cy, r * 0.84f, r * 0.84f, 40), H(0x123F4A), H(0x071C22));
            for (var i = 0; i < 12; i++) {
                var largo = i % 3 == 0 ? 6f : 3.5f;
                v.Forma(Tizador.Rotar(Caja(cx - 1, cy - r * 0.78f, 2, largo), cx, cy, i * 30), A(Tinta, 0.75f));
            }
            var am = minutos * 6f; var ah = (horas % 12 + minutos / 60f) * 30f;
            Vector2 Punta(float grados, float largo) { var a = grados * Mathf.Deg2Rad; return new Vector2(cx + Mathf.Sin(a) * largo, cy - Mathf.Cos(a) * largo); }
            var m = Punta(am, r * 0.7f); var h = Punta(ah, r * 0.46f);
            v.Neon(Pts(cx, cy, h.x, h.y), 1.6f, agujas);
            v.Neon(Pts(cx, cy, m.x, m.y), 1.0f, agujas);
            v.Forma(L.Elipse(cx, cy, 2.6f, 2.6f, 10), Cromo);
            v.Forma(L.Arco(cx, cy, r * 0.8f, r * 0.8f, 2, 200, 245, 10), A(Color.white, 0.25f));
        }

        /// <summary>Lampara de techo con pantalla esmaltada: halo, cono de luz hacia abajo.</summary>
        private static Graphic Lampara(Tizador v, float cx, float y, Color luz, float intensidad = 1) {
            v.Haz(cx, y + 6, 240, 300, 0, A(luz, 0.10f * intensidad));
            v.Cable(Pts(cx, -4, cx, y - 16), 2, 2, H(0x0C2A30));
            var halo = v.Halo(cx, y + 8, 110, 70, A(luz, 0.45f * intensidad));
            v.Forma(L.Elipse(cx, y, 34, 22, 32, 180, 360), H(0x2E4A50), H(0x0B1E23));
            v.Forma(L.Elipse(cx, y, 34, 4.5f, 24), A(luz, 0.9f), A(BombillaCentro, 0.9f));
            v.Halo(cx, y + 4, 22, A(BombillaCentro, 0.95f * intensidad));
            return halo;
        }

        // ================================================================ la caldera del riesgo

        /// <summary>Volante de valvula: aro, cuatro radios y cubo.</summary>
        private static void Valvula(Tizador v, float cx, float cy, float r, Color c) {
            for (var i = 0; i < 4; i++) {
                var a = i * Mathf.PI / 2 + 0.4f;
                v.Cable(Pts(cx, cy, cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r), 3.2f, 2.4f, Color.Lerp(c, Silueta, 0.25f));
            }
            Anillo(v, cx, cy, r, 4.5f, c);
            v.Forma(L.Arco(cx, cy, r + 2, r + 2, 1.6f, 200, 260, 10), A(Color.white, 0.35f));
            v.Forma(L.Elipse(cx, cy, 4.5f, 4.5f, 12), Cromo, CromoOscuro);
        }

        /// <summary>Manometro con esfera crema y zonas menta, mostaza y coral; 'valor' de 0 a 1.</summary>
        private static void Manometro(Tizador v, float cx, float cy, float r, float valor) {
            v.Halo(cx, cy, r * 1.6f, A(Coral, 0.18f + 0.3f * valor));
            v.Forma(L.Elipse(cx, cy, r, r, 48), Cromo, CromoOscuro);
            v.Forma(L.Elipse(cx, cy, r * 0.86f, r * 0.86f, 48), H(0xEFE6CC), H(0xB9AE90));
            v.Forma(L.Arco(cx, cy, r * 0.78f, r * 0.78f, r * 0.12f, 150, 250, 24), Menta);
            v.Forma(L.Arco(cx, cy, r * 0.78f, r * 0.78f, r * 0.12f, 250, 310, 16), Aviso);
            v.Forma(L.Arco(cx, cy, r * 0.78f, r * 0.78f, r * 0.12f, 310, 390, 18), Coral);
            for (var i = 0; i <= 8; i++) {
                var g = 150 + i * 30 + 90;   // Rotar mide desde las 12; el arco, desde las 3
                v.Forma(Tizador.Rotar(Caja(cx - 0.9f, cy - r * 0.62f, 1.8f, 5), cx, cy, g), H(0x2A2620));
            }
            var ang = (150 + valor * 240) * Mathf.Deg2Rad;
            var d = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)); var n = new Vector2(-d.y, d.x);
            var c0 = new Vector2(cx, cy);
            var aguja = new List<Vector2> { c0 - d * r * 0.14f + n * 3.2f, c0 + d * r * 0.7f, c0 - d * r * 0.14f - n * 3.2f };
            v.Forma(aguja, H(0x14100C));
            v.Forma(L.Elipse(cx, cy, 5, 5, 14), H(0x2A2620), H(0x0B0806));
            v.Forma(L.Arco(cx, cy, r * 0.82f, r * 0.82f, 3, 195, 250, 14), A(Color.white, 0.45f));
        }
    }
}
