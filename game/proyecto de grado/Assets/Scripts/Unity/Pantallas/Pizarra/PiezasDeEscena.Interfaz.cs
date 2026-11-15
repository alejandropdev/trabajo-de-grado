using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using UnityEngine;
using L = Nexus.Unity.Tema.LuzUI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// (2026-11-15, direccion de arte) Las escenas «Cómo lo verás en el juego» (j-…) enseñan pantallas del propio juego:
    /// se dibujan con la interfaz neon en miniatura (paneles de cristal oscuro, botones de borde cian, barras) para que el
    /// jugador reconozca la pantalla de la que se habla. Colores de los tokens de NexusTheme (docs/08-arte/STYLE.md §1).
    /// </summary>
    public static partial class DibujosDeEscena {
        private static readonly Color Superficie = H(0x0F2630), SuperficieAlta = H(0x15313C), Borde = H(0x2E6470), Pozo = H(0x071419);

        /// <summary>Panel de la interfaz: cristal oscuro con su borde; 'titulo' en versalitas arriba a la izquierda.</summary>
        private static void PanelUI(Tizador v, float x, float y, float w, float h, string titulo = null, Color? acento = null) {
            v.Forma(L.Redondeado(x - 1.5f, y - 1.5f, w + 3, h + 3, 13), A(Borde, 0.9f));
            v.Forma(L.Redondeado(x, y, w, h, 12), SuperficieAlta, Superficie);
            if (titulo == null) return;
            v.Rotulo(x + 18, y + 22, titulo, 14, acento ?? Nucleo, -1, true);
            v.Forma(Caja(x + 18, y + 36, w - 36, 1.2f), A(Borde, 0.8f));
        }

        /// <summary>Boton neon: borde y texto del color; 'primario' lo rellena y le da su halo (solo lo interactivo brilla).</summary>
        private static void BotonUI(Tizador v, float x, float y, float w, float h, string texto, Color c, bool primario = false) {
            if (primario) v.Halo(x + w / 2, y + h / 2, w * 0.7f, h * 1.1f, A(c, 0.3f));
            v.Forma(L.Redondeado(x - 1.5f, y - 1.5f, w + 3, h + 3, h / 2 + 1), A(c, 0.95f));
            v.Forma(L.Redondeado(x, y, w, h, h / 2), primario ? c : Color.Lerp(Superficie, c, 0.14f));
            v.Rotulo(x + w / 2, y + h / 2, texto, Mathf.Min(18, h * 0.42f), primario ? H(0x03141A) : c, 0, true);
        }

        /// <summary>Barra de valor de la interfaz: pozo oscuro y relleno con degradado; etiqueta a la izquierda si la hay.</summary>
        private static void BarraUI(Tizador v, float x, float y, float w, float valor, Color c, string etiqueta = null, float alto = 12) {
            if (etiqueta != null) v.Rotulo(x - 12, y + alto / 2, etiqueta, 15, A(Tinta, 0.85f), 1);
            v.Forma(L.Redondeado(x, y, w, alto, alto / 2), Pozo);
            if (valor <= 0) return;
            v.Forma(L.Redondeado(x, y, Mathf.Max(alto, w * valor), alto, alto / 2), Color.Lerp(c, Color.white, 0.2f), c,
                    new Vector2(x, 0), new Vector2(x + w * valor, 0));
            v.Halo(x + w * valor, y + alto / 2, alto * 1.4f, A(c, 0.5f));
        }

        /// <summary>Tarjeta del tablero: cristal claro con su franja de color a la izquierda y dos renglones.</summary>
        private static void TarjetaUI(Tizador v, float x, float y, float w, float h, Color franja, float renglon = 0.7f) {
            v.Forma(L.Redondeado(x, y, w, h, 6), H(0x1D3F4A), H(0x15313C));
            v.Forma(L.Redondeado(x, y, 5, h, 2.5f), franja);
            v.Forma(L.Redondeado(x + 14, y + h * 0.32f, (w - 28) * renglon, 4, 2), A(Tinta, 0.75f));
            v.Forma(L.Redondeado(x + 14, y + h * 0.6f, (w - 28) * renglon * 0.6f, 4, 2), A(TintaSuave, 0.6f));
        }

        /// <summary>Renglones de texto de mentira (gris) dentro de un panel.</summary>
        private static void Renglones(Tizador v, float x, float y, float w, int n, float paso = 14) {
            var anchos = new[] { 0.92f, 0.78f, 0.86f, 0.6f, 0.7f };
            for (var i = 0; i < n; i++) v.Forma(L.Redondeado(x, y + i * paso, w * anchos[i % anchos.Length], 4, 2), A(TintaSuave, 0.55f));
        }

        /// <summary>Flecha de neon de (x1, y1) a (x2, y2).</summary>
        private static void FlechaNeon(Tizador v, float x1, float y1, float x2, float y2, Color c) {
            var d = new Vector2(x2 - x1, y2 - y1).normalized; var n = new Vector2(-d.y, d.x);
            var b = new Vector2(x2, y2) - d * 12;
            v.Neon(Pts(x1, y1, b.x, b.y), 1.2f, c);
            v.Forma(new List<Vector2> { new Vector2(x2, y2), b + n * 7, b - n * 7 }, Color.Lerp(c, Color.white, 0.4f));
        }

        // ---------------------------------------------------------------- objetos que acompañan a la interfaz

        /// <summary>Lupa con su cristal que brilla un poco.</summary>
        private static void Lupa(Tizador v, float cx, float cy, float r, Color c) {
            var d = new Vector2(0.7071f, 0.7071f);
            v.Cable(Pts(cx + d.x * r * 1.02f, cy + d.y * r * 1.02f, cx + d.x * r * 2.2f, cy + d.y * r * 2.2f), r * 0.28f, r * 0.34f, H(0x3A2A1E));
            v.FormaDeLuz(L.Elipse(cx, cy, r * 0.82f, r * 0.82f, 32), A(c, 0.12f));
            Anillo(v, cx, cy, r * 0.9f, r * 0.2f, Cromo);
            v.Forma(L.Arco(cx, cy, r * 0.62f, r * 0.62f, 2.4f, 200, 250, 10), A(Color.white, 0.5f));
        }

        /// <summary>Credencial colgada de su cinta: la persona se cuenta por su tarjeta de empleado.</summary>
        private static void Credencial(Tizador v, float cx, float y, Color cinta, string rol = null) {
            v.Cable(Pts(cx - 14, y - 40, cx - 4, y), 3, 3, A(cinta, 0.9f));
            v.Cable(Pts(cx + 14, y - 40, cx + 4, y), 3, 3, A(cinta, 0.9f));
            v.Forma(L.Redondeado(cx - 22, y, 44, 58, 5), H(0xE9E4D4), H(0xC9C2AD));
            v.Forma(L.Redondeado(cx - 22, y, 44, 12, 5), cinta);
            v.Forma(L.Redondeado(cx - 12, y + 18, 24, 22, 4), H(0x2E4A52), H(0x1A2E34));
            v.Forma(Caja(cx - 14, y + 46, 28, 3), A(H(0x1E2A2A), 0.6f));
            if (rol != null) v.Rotulo(cx, y + 74, rol, 14, cinta, 0, true);
        }

        /// <summary>Baliza giratoria de alarma: cupula coral con dos haces que barren.</summary>
        private static void Baliza(Tizador v, float cx, float yBase) {
            v.Latido(v.Haz(cx, yBase - 22, 260, 70, -70, A(Coral, 0.16f)), 0.2f, 1, 0.9f);
            v.Latido(v.Haz(cx, yBase - 22, 260, 70, 70, A(Coral, 0.16f)), 0.2f, 1, 0.9f, 0.5f);
            v.Forma(L.Redondeado(cx - 30, yBase - 10, 60, 12, 4), Cromo, CromoOscuro);
            v.Latido(v.Halo(cx, yBase - 24, 60, A(Coral, 0.55f)), 0.3f, 1, 0.9f);
            v.Forma(L.Elipse(cx, yBase - 10, 22, 24, 28, 180, 360), BombillaCentro, Coral);
            v.Forma(L.Arco(cx, yBase - 12, 15, 17, 2.4f, 200, 250, 10), A(Color.white, 0.6f));
        }

        /// <summary>Campana de aviso con su badajo; las ondas del sonido a los lados.</summary>
        private static void Campana(Tizador v, float cx, float y, float r, Color c) {
            v.Halo(cx, y + r * 0.6f, r * 2.4f, A(c, 0.25f));
            v.Forma(new RutaDeCurva(cx - r * 1.05f, y + r * 1.3f).C(cx - r * 0.8f, y + r * 1.1f, cx - r * 0.9f, y, cx, y)
                .C(cx + r * 0.9f, y, cx + r * 0.8f, y + r * 1.1f, cx + r * 1.05f, y + r * 1.3f).Puntos(), Color.Lerp(c, Color.white, 0.25f), Color.Lerp(c, Silueta, 0.35f));
            v.Forma(L.Redondeado(cx - r * 1.15f, y + r * 1.26f, r * 2.3f, r * 0.16f, r * 0.08f), Color.Lerp(c, Silueta, 0.4f));
            v.Forma(L.Elipse(cx, y + r * 1.5f, r * 0.18f, r * 0.18f, 14), Color.Lerp(c, Silueta, 0.3f));
            v.Forma(L.Elipse(cx, y - r * 0.08f, r * 0.14f, r * 0.14f, 12), Color.Lerp(c, Silueta, 0.3f));
            foreach (var s in new[] { -1, 1 })
                for (var k = 0; k < 2; k++) {
                    var arco = L.Arco(cx + s * (r * 1.4f + k * 14), y + r * 0.7f, 12 + k * 10, 22 + k * 12, 2.5f, s < 0 ? 150 : -30, s < 0 ? 210 : 30, 12);
                    v.Latido(v.Forma(arco, A(Coral, 0.85f - k * 0.25f)), 0.3f, 1, 0.8f, k * 0.25f);
                }
        }

        // ---------------------------------------------------------------- piezas de sistema, papel y paso

        /// <summary>Una pieza de un sistema (modulo, servicio): panel con su nombre del color que toque.</summary>
        private static void Pieza(Tizador v, float x, float y, float w, float h, string texto, Color c, float tam = 15) {
            v.Forma(L.Redondeado(x - 1.5f, y - 1.5f, w + 3, h + 3, 9), A(c, 0.85f));
            v.Forma(L.Redondeado(x, y, w, h, 8), Color.Lerp(SuperficieAlta, c, 0.12f), Superficie);
            if (!string.IsNullOrEmpty(texto)) v.Rotulo(x + w / 2, y + h / 2, texto, tam, c, 0, true);
        }

        /// <summary>Papel impreso con su titulo y renglones (documento, cuaderno, codigo).</summary>
        private static void Papel(Tizador v, float x, float y, float w, float h, string titulo = null, Color? tinta = null, float giro = 0) {
            var t = tinta ?? H(0x1E2A2A);
            List<Vector2> R(List<Vector2> p) { return giro == 0 ? p : Tizador.Rotar(p, x + w / 2, y + h / 2, giro); }
            v.Forma(R(L.Redondeado(x + 5, y + 6, w, h, 4)), A(Silueta, 0.45f));
            v.Forma(R(L.Redondeado(x, y, w, h, 4)), H(0xEDE7D6), H(0xCFC7B0));
            var y0 = y + 16;
            if (titulo != null) { v.Rotulo(x + w / 2, y + 18, titulo, 13, t, 0, true); y0 = y + 36; }
            for (var ry = y0; ry < y + h - 10; ry += 14) v.Forma(R(Caja(x + 12, ry, (ry / 14) % 2 < 1 ? w - 24 : w - 40, 3)), A(t, 0.4f));
        }

        /// <summary>Calendario de pared: hoja con su cabecera y los dias.</summary>
        private static void Calendario(Tizador v, float x, float y, float w, float h, Color c) {
            v.Forma(L.Redondeado(x + 5, y + 6, w, h, 5), A(Silueta, 0.45f));
            v.Forma(L.Redondeado(x, y, w, h, 5), H(0xEDE7D6), H(0xCFC7B0));
            v.Forma(L.Redondeado(x, y, w, h * 0.24f, 5), c);
            for (var f = 0; f < 3; f++) for (var k = 0; k < 5; k++) v.Forma(Caja(x + 10 + k * (w - 20) / 5, y + h * 0.34f + f * (h * 0.2f), (w - 20) / 5 - 5, h * 0.12f), A(H(0x1E2A2A), 0.25f));
        }

        /// <summary>Reloj de arena con su armazon: 'resto' de 0 a 1 es la arena que queda arriba.</summary>
        private static void RelojDeArena(Tizador v, float cx, float y, float h, Color c, float resto = 0.4f) {
            var w = h * 0.55f;
            v.Forma(L.Redondeado(cx - w / 2 - 6, y - 6, w + 12, 8, 3), Cromo, CromoOscuro);
            v.Forma(L.Redondeado(cx - w / 2 - 6, y + h - 2, w + 12, 8, 3), Cromo, CromoOscuro);
            v.FormaDeLuz(Pts(cx - w / 2, y + 2, cx + w / 2, y + 2, cx + 3, y + h / 2, cx + w / 2, y + h - 2, cx - w / 2, y + h - 2, cx - 3, y + h / 2), A(NucleoClaro, 0.12f));
            var a = (1 - resto) * (h / 2 - 6);
            v.Forma(Pts(cx - (w / 2) * resto, y + 4 + a, cx + (w / 2) * resto, y + 4 + a, cx, y + h / 2), Color.Lerp(c, Color.white, 0.2f), c);
            v.Forma(Pts(cx, y + h - 4 - (1 - resto) * (h / 2 - 8), cx + w / 2 - 4, y + h - 4, cx - w / 2 + 4, y + h - 4), Color.Lerp(c, Color.white, 0.2f), c);
            v.Forma(Caja(cx - 0.8f, y + h / 2, 1.6f, h / 2 - 8), A(c, 0.8f));
        }

        /// <summary>Barrera de paso sobre la via: poste y brazo con franjas; 'abierta' lo levanta.</summary>
        private static void Barrera(Tizador v, float x, float yRiel, bool abierta, Color luz) {
            v.Forma(L.Redondeado(x - 6, yRiel - 70, 12, 70, 3), H(0x46555A), H(0x1A2428));
            var brazo = L.Redondeado(x - 4, yRiel - 66, 110, 8, 4);
            var giro = abierta ? -62 : 0;
            v.Forma(Tizador.Rotar(brazo, x, yRiel - 62, giro), Tinta, H(0xBDB59E));
            for (var i = 0; i < 4; i++) v.Forma(Tizador.Rotar(Caja(x + 10 + i * 26, yRiel - 66, 12, 8), x, yRiel - 62, giro), abierta ? Menta : Coral);
            v.Halo(x, yRiel - 76, 14, A(luz, 0.7f));
            v.Forma(L.Elipse(x, yRiel - 76, 5, 5, 12), Color.Lerp(luz, Color.white, 0.4f));
        }

        /// <summary>Rele de red: la caja por la que pasan los mensajes entre servicios (con su antena).</summary>
        private static void Rele(Tizador v, float cx, float cy, Color c) {
            v.Cable(Pts(cx + 8, cy - 18, cx + 14, cy - 40), 1.6f, 1.2f, Cromo);
            v.Halo(cx + 14, cy - 42, 8, A(c, 0.7f));
            v.Forma(L.Redondeado(cx - 22, cy - 18, 44, 36, 6), H(0x2B3335), H(0x101416));
            for (var i = 0; i < 3; i++) v.Forma(Caja(cx - 14, cy - 8 + i * 7, 20, 2), A(TintaSuave, 0.5f));
            v.Forma(L.Elipse(cx + 12, cy, 3.5f, 3.5f, 10), Color.Lerp(c, Color.white, 0.3f));
        }

        private static void NeonCheck(Tizador v, float cx, float cy, float s, Color? c = null) {
            v.Halo(cx, cy, s * 1.6f, A(c ?? Menta, 0.3f));
            v.Neon(Pts(cx - s * 0.6f, cy, cx - s * 0.15f, cy + s * 0.45f, cx + s * 0.7f, cy - s * 0.55f), Mathf.Max(1.2f, s * 0.07f), c ?? Menta);
        }

        private static void NeonX(Tizador v, float cx, float cy, float s, Color? c = null) {
            v.Halo(cx, cy, s * 1.6f, A(c ?? Coral, 0.3f));
            v.Neon(Pts(cx - s * 0.5f, cy - s * 0.5f, cx + s * 0.5f, cy + s * 0.5f), Mathf.Max(1.2f, s * 0.07f), c ?? Coral);
            v.Neon(Pts(cx + s * 0.5f, cy - s * 0.5f, cx - s * 0.5f, cy + s * 0.5f), Mathf.Max(1.2f, s * 0.07f), c ?? Coral);
        }

        /// <summary>
        /// Contorno cerrado lado a lado (un tubo cerrado en un solo poligono se triangularia como forma rellena).
        /// 'neon' lo pinta como tubo de neon; si no, como linea del color.
        /// </summary>
        private static void Contorno(Tizador v, List<Vector2> puntos, float g, Color c, bool neon = false) {
            for (var i = 0; i < puntos.Count; i++) {
                Vector2 a = puntos[i], b = puntos[(i + 1) % puntos.Count];
                if (neon) v.Neon(new List<Vector2> { a, b }, g, c);
                else v.Forma(L.Tubo(new List<Vector2> { a, b }, g, g), c);
            }
        }

        /// <summary>Una X maciza de sello, para encima de papel (el neon aditivo se lava sobre lo claro).</summary>
        private static void Sello(Tizador v, float cx, float cy, float s, Color? c = null) {
            var col = c ?? Coral;
            v.Forma(L.Tubo(Pts(cx - s * 0.5f, cy - s * 0.5f, cx + s * 0.5f, cy + s * 0.5f), s * 0.2f, s * 0.2f), col);
            v.Forma(L.Tubo(Pts(cx + s * 0.5f, cy - s * 0.5f, cx - s * 0.5f, cy + s * 0.5f), s * 0.2f, s * 0.2f), col);
        }

        /// <summary>Maraña de cables entre (x, y, w, h): todo toca todo.</summary>
        private static void Maraña(Tizador v, float x, float y, float w, float h, Color c, int semilla) {
            var az = new System.Random(semilla);
            for (var i = 0; i < 6; i++) {
                Vector2 a = new Vector2(x + (float)az.NextDouble() * w, y + (float)az.NextDouble() * h);
                Vector2 b = new Vector2(x + (float)az.NextDouble() * w, y + (float)az.NextDouble() * h);
                Vector2 m = new Vector2(x + (float)az.NextDouble() * w, y + (float)az.NextDouble() * h);
                v.Neon(Tizador.Bezier(a, m, b, 20), 0.9f, c);
            }
        }

        // ---------------------------------------------------------------- plataformas: ventana, telefono, app, antena, pasos

        /// <summary>Ventana de navegador de la interfaz: barra con sus tres luces y la direccion; devuelve la y del contenido.</summary>
        private static float Ventana(Tizador v, float x, float y, float w, float h, string url = null, Color? c = null) {
            var col = c ?? Nucleo;
            v.Forma(L.Redondeado(x - 1.5f, y - 1.5f, w + 3, h + 3, 11), A(col, 0.8f));
            v.Forma(L.Redondeado(x, y, w, h, 10), SuperficieAlta, Superficie);
            v.Forma(L.Redondeado(x, y, w, 26, 10), H(0x1D3F4A));
            var cs = new[] { Coral, Aviso, Menta };
            for (var i = 0; i < 3; i++) v.Forma(L.Elipse(x + 14 + i * 13, y + 13, 4, 4, 10), cs[i]);
            if (url != null) {
                v.Forma(L.Redondeado(x + 58, y + 6, w - 72, 15, 7), Pozo);
                v.Rotulo(x + 66, y + 13.5f, url, 11, TintaSuave, -1);
            }
            return y + 26;
        }

        /// <summary>Telefono de mano de cromo: cuerpo, pantalla que brilla, altavoz y boton. Devuelve el rect de la pantalla.</summary>
        private static Rect Movil(Tizador v, float x, float y, float w, float h, Color? brillo = null) {
            v.Forma(L.Redondeado(x + 4, y + 6, w, h, w * 0.16f), A(Silueta, 0.5f));
            v.Forma(L.Redondeado(x, y, w, h, w * 0.16f), Cromo, CromoOscuro);
            var p = new Rect(x + w * 0.08f, y + h * 0.1f, w * 0.84f, h * 0.76f);
            v.Forma(L.Redondeado(p.x, p.y, p.width, p.height, 8), H(0x0E3340), H(0x05161B));
            v.Halo(p.center.x, p.center.y, p.width * 0.6f, p.height * 0.5f, A(brillo ?? Nucleo, 0.14f));
            v.Forma(L.Redondeado(x + w / 2 - w * 0.12f, y + h * 0.045f, w * 0.24f, 4, 2), H(0x2B3335));
            v.Forma(L.Elipse(x + w / 2, y + h * 0.93f, w * 0.07f, w * 0.07f, 14), H(0x46555A), H(0x1A2428));
            return p;
        }

        /// <summary>Icono de app: azulejo redondeado que brilla si es la tuya.</summary>
        private static void AppIcono(Tizador v, float x, float y, float s, Color c, bool tuya = false) {
            if (tuya) v.Halo(x + s / 2, y + s / 2, s, A(c, 0.45f));
            v.Forma(L.Redondeado(x, y, s, s, s * 0.26f), Color.Lerp(c, Color.white, tuya ? 0.25f : 0.05f), Color.Lerp(c, Silueta, tuya ? 0.25f : 0.55f));
            v.Forma(L.Elipse(x + s / 2, y + s / 2, s * 0.2f, s * 0.2f, 14), A(Color.white, tuya ? 0.7f : 0.25f));
        }

        /// <summary>Antena de radio: internet, la red por la que viajan las peticiones.</summary>
        private static void Antena(Tizador v, float cx, float suelo, float alto, Color c) {
            var top = suelo - alto;
            v.Forma(Pts(cx - 3, top + 10, cx + 3, top + 10, cx + 20, suelo, cx - 20, suelo), H(0x46555A), H(0x1A2428));
            for (var i = 1; i < 4; i++) { var y = top + 10 + i * (alto - 10) / 4; var w = 3 + 17 * i / 4f; v.Forma(Caja(cx - w, y, w * 2, 2), Cromo); }
            v.Latido(v.Halo(cx, top + 6, 22, A(c, 0.6f)), 0.4f, 1, 1.4f);
            v.Forma(L.Elipse(cx, top + 6, 5, 5, 12), Color.Lerp(c, Color.white, 0.4f));
            for (var k = 0; k < 2; k++) {
                v.Forma(L.Arco(cx, top + 6, 14 + k * 10, 14 + k * 10, 2, 200, 250, 10), A(c, 0.8f - k * 0.25f));
                v.Forma(L.Arco(cx, top + 6, 14 + k * 10, 14 + k * 10, 2, 290, 340, 10), A(c, 0.8f - k * 0.25f));
            }
        }

        /// <summary>Pasos numerados en una linea de neon; cada paso «linea uno|linea dos» con su rotulo debajo.</summary>
        private static void PasosLuz(Tizador v, Color c, params string[] pasos) {
            FondoInterior(v, 300, 120, c, 0.5f, pasos.Length * 7);
            // Los rotulos se alternan arriba y abajo de la linea para que no se pisen entre vecinos.
            var n = pasos.Length; var paso = 470f / (n - 1);
            v.Neon(Pts(50, 140, 550, 140), 1.2f, c);
            for (var i = 0; i < n; i++) {
                var x = 65 + i * paso;
                v.Halo(x, 140, 26, A(c, 0.4f));
                v.Forma(L.Elipse(x, 140, 18, 18, 24), SuperficieAlta, Superficie);
                Anillo(v, x, 140, 18, 2.4f, c);
                v.Rotulo(x, 140, (i + 1).ToString(), 16, c, 0, false, true);
                var t = pasos[i].Split('|');
                var abajo = i % 2 == 0;
                for (var k = 0; k < t.Length; k++) {
                    var y = abajo ? 186 + k * 22 : 94 - (t.Length - 1 - k) * 22;
                    v.Rotulo(x, y, t[k], 15, k == 0 ? Tinta : TintaSuave);
                }
                v.Forma(Caja(x - 0.6f, abajo ? 160 : 104, 1.2f, abajo ? 12 : 14), A(c, 0.5f));
            }
        }
    }
}
