using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El gesto de la tarjeta: se arrastra de lado, se inclina como un naipe y, si se suelta lo bastante lejos, elige
    /// ese lado. Si no, vuelve al centro. Mientras se arrastra avisa de hacia donde se inclina (-1..1) para que las
    /// barras enseñen la consecuencia antes de soltar.
    ///
    /// Igual que PiezaArrastrable, no es la unica via: PantallaDeTarjetas ofrece siempre botones y teclas ←/→.
    /// </summary>
    public sealed class DeslizableDeTarjeta : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
        /// <summary>Cuanto hay que arrastrar, como fraccion del ancho de la tarjeta, para que cuente como elegir.</summary>
        public const float UmbralDeElegir = 0.28f;
        private const float GiroMaximo = 9f;
        private const float VelocidadDeVuelta = 14f;

        /// <summary>-1 (izquierda) .. 1 (derecha) mientras se arrastra; 0 al volver al centro.</summary>
        public Action<float> AlInclinar;
        /// <summary>-1 o 1: el lado elegido al soltar pasado el umbral.</summary>
        public Action<int> AlSoltar;

        private RectTransform _rt;
        private float _x;
        private bool _arrastrando;
        private bool _bloqueada;

        public void Bloquear(bool bloqueada) { _bloqueada = bloqueada; if (bloqueada) Volver(); }

        private float Ancho { get { return Mathf.Max(1f, ((RectTransform)transform).rect.width); } }

        public void OnBeginDrag(PointerEventData e) {
            if (_bloqueada) return;
            _rt = (RectTransform)transform;
            _arrastrando = true;
        }

        public void OnDrag(PointerEventData e) {
            if (!_arrastrando || _rt == null) return;
            var padre = (RectTransform)_rt.parent;
            Vector2 local, anterior;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, e.position, e.pressEventCamera, out local);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, e.position - e.delta, e.pressEventCamera, out anterior);
            _x += local.x - anterior.x;
            Aplicar();
            AlInclinar?.Invoke(Mathf.Clamp(_x / (Ancho * UmbralDeElegir), -1f, 1f));
        }

        public void OnEndDrag(PointerEventData e) {
            if (!_arrastrando) return;
            _arrastrando = false;
            if (Mathf.Abs(_x) >= Ancho * UmbralDeElegir) {
                var lado = _x < 0 ? -1 : 1;
                AlSoltar?.Invoke(lado);
            } else {
                AlInclinar?.Invoke(0f);
            }
        }

        private void Update() {
            if (_arrastrando || _rt == null || Mathf.Approximately(_x, 0f)) return;
            _x = Mathf.Lerp(_x, 0f, Time.unscaledDeltaTime * VelocidadDeVuelta);
            if (Mathf.Abs(_x) < 0.5f) _x = 0f;
            Aplicar();
        }

        /// <summary>Vuelve al centro sin animar: para cuando llega una tarjeta nueva.</summary>
        public void Volver() {
            _arrastrando = false;
            _x = 0f;
            if (_rt == null) _rt = (RectTransform)transform;
            Aplicar();
        }

        private void Aplicar() {
            _rt.anchoredPosition = new Vector2(_x, _rt.anchoredPosition.y);
            _rt.localRotation = Quaternion.Euler(0, 0, -GiroMaximo * Mathf.Clamp(_x / (Ancho * UmbralDeElegir), -1f, 1f));
        }
    }
}
