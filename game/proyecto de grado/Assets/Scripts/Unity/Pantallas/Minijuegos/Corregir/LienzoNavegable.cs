using UnityEngine;
using UnityEngine.EventSystems;

namespace Nexus.Unity.Pantallas.Minijuegos.Corregir {
    /// <summary>
    /// Un lienzo que se puede acercar con la rueda y arrastrar (2026-11-09, ronda 3): el diagrama ocupa toda la pantalla
    /// y lo demas (dialogo, cartas, botones) flota encima como globos. El contenido nunca se sale del cuadro del visor:
    /// con el zoom al minimo (1) cabe entero y no se mueve; al acercar, solo se puede arrastrar hasta que el borde del
    /// contenido toca el borde del visor (los «limites invisibles»). Los cambios se suavizan.
    ///
    /// Va en el visor (que tiene que tener un Image con raycast para recibir el arrastre y la rueda). Las zonas
    /// pulsables de dentro siguen funcionando: arrastrar desde una no la pulsa.
    /// </summary>
    public sealed class LienzoNavegable : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler {
        public const float ZoomMinimo = 1f, ZoomMaximo = 2.6f, PasoDeRueda = 1.18f, PasoDeBoton = 1.35f;

        private RectTransform _visor, _contenido;
        private Vector2 _tam;
        private float _k = 1, _kMeta = 1;
        private Vector2 _pos, _posMeta;
        private Vector2 _ultimoLocal;

        public float Zoom { get { return _k; } }
        /// <summary>Un empujon temporal sobre la posicion (el temblor al fallar un punto).</summary>
        public Vector2 Sacudida;

        public static LienzoNavegable Crear(RectTransform visor, RectTransform contenido, Vector2 tamBase) {
            var l = visor.gameObject.AddComponent<LienzoNavegable>();
            l._visor = visor;
            l._contenido = contenido;
            l._tam = tamBase;
            return l;
        }

        /// <summary>Acerca (&gt; 1) o aleja (&lt; 1) alrededor de un punto del visor (null = el centro).</summary>
        public void Acercar(float factor, Vector2? centro = null) {
            var c = centro ?? Vector2.zero;
            var nueva = Mathf.Clamp(_kMeta * factor, ZoomMinimo, ZoomMaximo);
            if (Mathf.Approximately(nueva, _kMeta)) return;
            // El punto bajo el cursor se queda donde esta.
            _posMeta = c - (c - _posMeta) * (nueva / _kMeta);
            _kMeta = nueva;
            _posMeta = Limitar(_posMeta, _kMeta);
        }

        /// <summary>Vuelve a verlo entero.</summary>
        public void Ajustar() { _kMeta = 1; _posMeta = Vector2.zero; }

        private Vector2 Limitar(Vector2 p, float k) {
            var v = _visor.rect.size;
            var mx = Mathf.Max(0f, (_tam.x * k - v.x) / 2f);
            var my = Mathf.Max(0f, (_tam.y * k - v.y) / 2f);
            return new Vector2(Mathf.Clamp(p.x, -mx, mx), Mathf.Clamp(p.y, -my, my));
        }

        private void Update() {
            var f = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            _k = Mathf.Lerp(_k, _kMeta, f);
            _pos = Vector2.Lerp(_pos, _posMeta, f);
            if (Mathf.Abs(_k - _kMeta) < 0.0005f) _k = _kMeta;
            if (_contenido == null) return;
            _contenido.localScale = new Vector3(_k, _k, 1);
            _contenido.anchoredPosition = Limitar(_pos, _k) + Sacudida;
        }

        private bool Local(PointerEventData e, out Vector2 local) {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_visor, e.position, e.pressEventCamera, out local);
        }

        public void OnScroll(PointerEventData e) {
            if (Mathf.Approximately(e.scrollDelta.y, 0f)) return;
            Vector2 c;
            if (!Local(e, out c)) c = Vector2.zero;
            Acercar(e.scrollDelta.y > 0 ? PasoDeRueda : 1f / PasoDeRueda, c);
        }

        public void OnBeginDrag(PointerEventData e) { Local(e, out _ultimoLocal); }

        public void OnDrag(PointerEventData e) {
            Vector2 actual;
            if (!Local(e, out actual)) return;
            _posMeta = Limitar(_posMeta + (actual - _ultimoLocal), _kMeta);
            _pos = _posMeta;   // al arrastrar, sigue al raton sin retraso
            _ultimoLocal = actual;
        }
    }
}
