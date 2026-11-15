using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// (2026-11-15, direccion de arte) La respiracion de una luz: el alfa de un Graphic sube y baja despacio entre Min y Max
    /// (un nucleo que late, el vapor de una taza). Con Parpadeo, en cambio, la luz se queda casi siempre encendida y de vez
    /// en cuando titila, como una bombilla cansada. Usa el tiempo sin escala: los pizarrones pausan el juego, no el reloj.
    /// </summary>
    public sealed class LatidoDeLuz : MonoBehaviour {
        public Graphic Objetivo;
        public float Min = 0.6f, Max = 1f, Periodo = 3f, Fase;
        public bool Parpadeo;

        private float _alfa = -1;

        public static LatidoDeLuz En(Graphic g, float min, float max, float periodo, float fase = 0, bool parpadeo = false) {
            if (g == null) return null;
            var l = g.gameObject.AddComponent<LatidoDeLuz>();
            l.Objetivo = g; l.Min = min; l.Max = max; l.Periodo = Mathf.Max(0.1f, periodo); l.Fase = fase; l.Parpadeo = parpadeo;
            return l;
        }

        private void Update() {
            if (Objetivo == null) return;
            if (_alfa < 0) _alfa = Objetivo.color.a;
            var t = Time.unscaledTime / Periodo + Fase;
            float f;
            if (Parpadeo) {
                // Casi siempre arriba; cuando el ruido cae por debajo del umbral, dos o tres golpes de apagon.
                var ruido = Mathf.PerlinNoise(t * 2.3f, Fase * 7.1f);
                f = ruido < 0.32f ? Mathf.Lerp(Min, Max, Mathf.PerlinNoise(t * 31f, 0.5f) * 0.6f) : Max;
            } else {
                f = Mathf.Lerp(Min, Max, 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2));
            }
            var c = Objetivo.color;
            c.a = _alfa * f;
            Objetivo.color = c;
        }
    }
}
