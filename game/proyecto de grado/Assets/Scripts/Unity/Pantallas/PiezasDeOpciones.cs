using System.Collections.Generic;
using Nexus.Unity.Tema;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Los pictogramas de la pantalla de Opciones (2026-11-07). Se DIBUJAN con trazos, no con texto: la letra del juego
    /// (Rajdhani) no tiene simbolos como ✓ ▸ ★ ← →. Cada uno cabe en un DibujoUI de 64 x 64 (coordenadas desde arriba a la
    /// izquierda, como el resto de dibujos del juego).
    /// </summary>
    public static class PictogramasDeOpciones {
        public enum Tipo { Dificultad, Sonido, Recorrido3D, Partida }

        public const float Lado = 64f;

        public static void Dibujar(DibujoUI d, Tipo tipo, Color c) {
            d.Limpiar();
            const float g = 4f;
            switch (tipo) {
                case Tipo.Dificultad:
                    Barras(d, 2, c, NexusTheme.Alfa(c, 0.35f));
                    break;

                case Tipo.Sonido:   // un altavoz con dos ondas
                    d.Polilinea(new List<Vector2> {
                        new Vector2(8, 25), new Vector2(18, 25), new Vector2(32, 13),
                        new Vector2(32, 51), new Vector2(18, 39), new Vector2(8, 39), new Vector2(8, 25)
                    }, c, g);
                    d.Curva(new Vector2(40, 24), new Vector2(47, 32), new Vector2(40, 40), c, 3.5f, false);
                    d.Curva(new Vector2(45, 15), new Vector2(60, 32), new Vector2(45, 49), c, 3.5f, false);
                    break;

                case Tipo.Recorrido3D:   // un cubo: el hexagono y las tres aristas que salen del centro
                    d.Polilinea(new List<Vector2> {
                        new Vector2(32, 6), new Vector2(56, 19), new Vector2(56, 45),
                        new Vector2(32, 58), new Vector2(8, 45), new Vector2(8, 19), new Vector2(32, 6)
                    }, c, g);
                    d.Linea(32, 32, 8, 19, c, 3);
                    d.Linea(32, 32, 56, 19, c, 3);
                    d.Linea(32, 32, 32, 58, c, 3);
                    break;

                case Tipo.Partida:   // un reloj
                    d.Reloj(32, 32, c);
                    break;
            }
        }

        /// <summary>Tres barras crecientes (como la señal del movil): las 'nivel' primeras encendidas.</summary>
        public static void Barras(DibujoUI d, int nivel, Color encendida, Color apagada) {
            d.Limpiar();
            for (var i = 0; i < 3; i++) {
                var alto = 16f + i * 14f;
                var x = 8f + i * 18f;
                var luce = i < nivel;
                d.Rect(x, 56f - alto, 12f, alto, luce ? encendida : apagada, 3f, luce ? (Color?)encendida : null, false, 3f);
            }
        }
    }

    /// <summary>
    /// Un interruptor de si/no (2026-11-07): una pista redondeada y una perilla que se desliza al cambiar, con el color
    /// que pasa de apagado a cian. Sustituye a los botones que decian su estado («Eje vertical invertido: no»).
    /// </summary>
    public sealed class InterruptorVivo : MonoBehaviour {
        public const float Ancho = 96f, Alto = 48f, Perilla = 36f;

        public Image Pista;
        public RectTransform PerillaRt;
        public Color Apagado, Encendido;
        private float _f, _quiero;

        public void Poner(bool encendido, bool inmediato) {
            _quiero = encendido ? 1f : 0f;
            if (inmediato) _f = _quiero;
            Aplicar();
        }

        private void Update() {
            if (Mathf.Approximately(_f, _quiero)) return;
            _f = Mathf.MoveTowards(_f, _quiero, Time.unscaledDeltaTime / 0.14f);
            Aplicar();
        }

        private void Aplicar() {
            var e = _f * _f * (3f - 2f * _f);
            if (Pista != null) Pista.color = Color.Lerp(Apagado, Encendido, e);
            // La perilla recorre la pista dejando un margen de 6 a cada lado.
            var recorrido = (Ancho - Perilla) / 2f - 6f;
            if (PerillaRt != null) PerillaRt.anchoredPosition = new Vector2(Mathf.Lerp(-recorrido, recorrido, e), 0f);
        }
    }

    /// <summary>
    /// La entrada de una tarjeta de Opciones: aparece con un fundido y un pequeño crecimiento, escalonada. Solo toca el
    /// alfa y la escala (la posicion es del layout) y, al acabar, se quita a si misma.
    /// </summary>
    internal sealed class AparicionDeTarjeta : MonoBehaviour {
        private const float Duracion = 0.35f;
        private float _retraso, _t;
        private CanvasGroup _grupo;

        public static void Poner(RectTransform tarjeta, float retraso) {
            var a = tarjeta.gameObject.AddComponent<AparicionDeTarjeta>();
            a._retraso = retraso;
            a._grupo = tarjeta.gameObject.GetComponent<CanvasGroup>();
            if (a._grupo == null) a._grupo = tarjeta.gameObject.AddComponent<CanvasGroup>();
            a._grupo.alpha = 0f;
            tarjeta.localScale = Vector3.one * 0.94f;
        }

        private void Update() {
            _t += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01((_t - _retraso) / Duracion);
            var e = 1f - Mathf.Pow(1f - u, 3f);
            _grupo.alpha = e;
            transform.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, e);
            if (u < 1f) return;
            _grupo.alpha = 1f;
            transform.localScale = Vector3.one;
            Destroy(this);
        }
    }
}
