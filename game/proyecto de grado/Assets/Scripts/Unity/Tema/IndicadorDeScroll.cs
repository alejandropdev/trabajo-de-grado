using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// El aviso de «hay más» de un area con scroll (UiKit.Desplazable): se enseña solo cuando el contenido no cabe y
    /// aun queda por ver en esa direccion, late suave para que se note, y al pulsarlo baja (o avanza) casi una
    /// pantalla. En areas muy bajas (menos de 150 px) no sale: taparia lo poco que se ve, y basta con la barra.
    /// </summary>
    public sealed class IndicadorDeScroll : MonoBehaviour {
        public ScrollRect Scroll;
        public RectTransform Abajo, Derecha;
        private Vector2 _baseAbajo, _baseDerecha;
        private bool _base;

        private void LateUpdate() {
            if (Scroll == null || Scroll.viewport == null || Scroll.content == null) return;
            if (!_base) {
                _base = true;
                if (Abajo != null) _baseAbajo = Abajo.anchoredPosition;
                if (Derecha != null) _baseDerecha = Derecha.anchoredPosition;
            }
            var visor = Scroll.viewport.rect;
            var contenido = Scroll.content.rect;
            var latido = Mathf.Sin(Time.unscaledTime * 3.2f) * 3f;

            var masAbajo = Scroll.vertical && visor.height >= 150 && contenido.height > visor.height + 8 &&
                           Scroll.verticalNormalizedPosition > 0.02f;
            Mostrar(Abajo, masAbajo, _baseAbajo + new Vector2(0, latido));

            var masDerecha = Scroll.horizontal && visor.width >= 150 && contenido.width > visor.width + 8 &&
                             Scroll.horizontalNormalizedPosition < 0.98f;
            Mostrar(Derecha, masDerecha, _baseDerecha + new Vector2(-latido, 0));
        }

        private static void Mostrar(RectTransform aviso, bool ver, Vector2 posicion) {
            if (aviso == null) return;
            if (aviso.gameObject.activeSelf != ver) aviso.gameObject.SetActive(ver);
            if (ver) aviso.anchoredPosition = posicion;
        }

        /// <summary>Avanza casi una pantalla (el 85 % de lo que se ve) hacia abajo o hacia la derecha.</summary>
        public void Avanzar(bool vertical) {
            var visor = Scroll.viewport.rect;
            var contenido = Scroll.content.rect;
            Scroll.velocity = Vector2.zero;
            if (vertical) {
                var sobra = contenido.height - visor.height;
                if (sobra <= 0) return;
                Scroll.verticalNormalizedPosition = Mathf.Clamp01(Scroll.verticalNormalizedPosition - visor.height * 0.85f / sobra);
            } else {
                var sobra = contenido.width - visor.width;
                if (sobra <= 0) return;
                Scroll.horizontalNormalizedPosition = Mathf.Clamp01(Scroll.horizontalNormalizedPosition + visor.width * 0.85f / sobra);
            }
        }
    }
}
