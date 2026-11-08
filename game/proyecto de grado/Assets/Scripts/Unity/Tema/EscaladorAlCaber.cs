using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// Que lo de dentro SIEMPRE quepa, sin scroll (2026-11-07): mide lo que el contenido pide (Interior) y, si es mas
    /// alto o mas ancho que el hueco, lo encoge de forma uniforme (escala), dandole un lienzo mas grande para que el
    /// texto se parta menos. Nunca agranda.
    /// Con AltoFijo, este nodo ademas ocupa exactamente ese alto (el visor de un scroll): la pantalla entera cabe.
    /// Lo usan la Calidad y las tres partes de la Metodologia de la Fase 1.
    /// </summary>
    public sealed class EscaladorAlCaber : MonoBehaviour {
        public RectTransform Interior;
        public Func<float> AltoFijo;
        private Vector2 _ultimo;
        private int _pasadas;

        /// <summary>La escala que se aplico la ultima vez (1 = cabia sin encoger).</summary>
        public float Escala { get; private set; } = 1f;

        /// <summary>Vuelve a medir en los proximos fotogramas (cuando el contenido cambia de tamaño).</summary>
        public void Remedir() { _pasadas = 0; }

        private void OnEnable() { _pasadas = 0; }

        private void LateUpdate() {
            var rt = (RectTransform)transform;
            if (AltoFijo != null) {
                var h = AltoFijo();
                var le = GetComponent<LayoutElement>();
                if (h > 1 && le != null && Mathf.Abs(le.preferredHeight - h) > 0.5f) le.preferredHeight = h;
            }
            var ancho = rt.rect.width;
            var alto = rt.rect.height;
            if (ancho <= 1 || alto <= 1 || Interior == null) return;
            var tam = new Vector2(ancho, alto);
            if ((tam - _ultimo).sqrMagnitude > 0.25f) _pasadas = 0;
            _ultimo = tam;
            if (_pasadas++ >= 6) return;   // el layout se asienta en unos fotogramas; despues no se vuelve a medir
            Ajustar(ancho, alto);
        }

        /// <summary>Mide y escala ya (lo usa tambien la herramienta de capturas, fuera del bucle de juego).</summary>
        public void Ajustar() {
            var r = ((RectTransform)transform).rect;
            if (r.width > 1 && r.height > 1 && Interior != null) Ajustar(r.width, r.height);
        }

        private void Ajustar(float ancho, float alto) {
            Interior.anchorMin = Interior.anchorMax = Interior.pivot = new Vector2(0.5f, 0.5f);
            Interior.anchoredPosition = Vector2.zero;
            var escala = 1f;
            for (var i = 0; i < 4; i++) {
                Interior.localScale = Vector3.one;
                Interior.sizeDelta = new Vector2(ancho / escala, alto / escala);
                LayoutRebuilder.ForceRebuildLayoutImmediate(Interior);
                var pideAlto = LayoutUtility.GetPreferredHeight(Interior);
                var pideAncho = LayoutUtility.GetMinWidth(Interior);
                var nueva = Mathf.Min(pideAlto > alto ? alto / pideAlto : 1f, pideAncho > ancho ? ancho / pideAncho : 1f);
                if (nueva >= escala - 0.005f) break;   // ya cabe (solo se encoge: es lo conservador)
                escala = Mathf.Max(0.2f, nueva);
            }
            Interior.sizeDelta = new Vector2(ancho / escala, alto / escala);
            Interior.localScale = Vector3.one * escala;
            Escala = escala;
        }
    }
}
