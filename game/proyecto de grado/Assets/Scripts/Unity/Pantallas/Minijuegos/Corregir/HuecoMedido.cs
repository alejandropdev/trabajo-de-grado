using System;
using UnityEngine;

namespace Nexus.Unity.Pantallas.Minijuegos.Corregir {
    /// <summary>
    /// Avisa cuando el hueco en el que esta ya tiene su tamaño de verdad. Un hueco de un layout mide cero hasta que uGUI
    /// reparte el sitio (y puede cambiar durante los primeros fotogramas), asi que espera a que el tamaño se repita dos
    /// fotogramas seguidos. Lo usa el diagrama para dibujarse a escala 1:1 en lo que le toca, en vez de dibujarse en una
    /// lamina fija y encogerla (2026-11-09, ronda 3: asi se leia muy pequeño).
    /// </summary>
    public sealed class HuecoMedido : MonoBehaviour {
        public Action<float, float> AlMedirse;
        private Vector2 _ultimo;
        private int _repetido;
        private bool _hecho;

        private void LateUpdate() {
            if (_hecho) return;
            var r = ((RectTransform)transform).rect;
            if (r.width <= 1 || r.height <= 1) { _repetido = 0; return; }
            var tam = new Vector2(r.width, r.height);
            _repetido = (tam - _ultimo).sqrMagnitude < 0.25f ? _repetido + 1 : 0;
            _ultimo = tam;
            if (_repetido < 2) return;
            _hecho = true;
            AlMedirse?.Invoke(tam.x, tam.y);
        }
    }
}
