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
    }
}
