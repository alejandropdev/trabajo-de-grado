using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nexus.Unity.Pantallas.Minijuegos.Corregir {
    /// <summary>
    /// Un corredor de animaciones minimo para el diagrama con cartas: cada animacion es un paso (t de 0 a 1) con su
    /// retraso y su duracion, en tiempo sin escalar. No hay ninguno compartido en el proyecto: cada vista anima en su
    /// Update, y aqui hay muchas cosas que se mueven a la vez (cartas que vuelan, marcadores, el revelado).
    /// </summary>
    public sealed class Animador : MonoBehaviour {
        private sealed class Animacion {
            public float Retraso, Duracion, T;
            public Action<float> Paso;
            public Action AlFin;
            public GameObject Dueno;
            public bool TieneDueno;
        }

        private readonly List<Animacion> _vivas = new List<Animacion>();
        private readonly List<Animacion> _nuevas = new List<Animacion>();

        /// <summary>Lanza una animacion. 'dueno': si se destruye antes, la animacion se cancela sin llamar a AlFin.</summary>
        public void Lanzar(float duracion, Action<float> paso, Action alFin = null, float retraso = 0, GameObject dueno = null) {
            _nuevas.Add(new Animacion {
                Duracion = Mathf.Max(0.0001f, duracion), Retraso = retraso, Paso = paso, AlFin = alFin,
                Dueno = dueno, TieneDueno = dueno != null
            });
        }

        /// <summary>Espera y luego hace algo.</summary>
        public void Despues(float segundos, Action accion, GameObject dueno = null) { Lanzar(0.0001f, null, accion, segundos, dueno); }

        private void Update() {
            if (_nuevas.Count > 0) { _vivas.AddRange(_nuevas); _nuevas.Clear(); }
            var dt = Time.unscaledDeltaTime;
            for (var i = _vivas.Count - 1; i >= 0; i--) {
                if (i >= _vivas.Count) continue;
                var a = _vivas[i];
                if (a.TieneDueno && a.Dueno == null) { _vivas.RemoveAt(i); continue; }
                if (a.Retraso > 0) { a.Retraso -= dt; continue; }
                a.T += dt;
                var t = Mathf.Clamp01(a.T / a.Duracion);
                try { a.Paso?.Invoke(t); } catch (Exception e) { Debug.LogException(e); _vivas.Remove(a); continue; }
                if (t < 1f) continue;
                _vivas.Remove(a);
                try { a.AlFin?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
            }
        }
    }

    /// <summary>Curvas de suavizado (t de 0 a 1).</summary>
    public static class Curvas {
        public static float SalidaCubica(float t) { return 1 - Mathf.Pow(1 - Mathf.Clamp01(t), 3); }
        public static float EntradaSalida(float t) { t = Mathf.Clamp01(t); return t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2; }

        /// <summary>Se pasa un poco y vuelve: para lo que «cae» en su sitio.</summary>
        public static float SalidaAtras(float t) {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f, c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
        }

        /// <summary>Rebote elastico: los marcadores que aparecen.</summary>
        public static float Elastica(float t) {
            t = Mathf.Clamp01(t);
            if (t <= 0 || t >= 1) return t;
            return Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - 0.75f) * (2 * Mathf.PI / 3)) + 1;
        }

        /// <summary>Una campana 0 → 1 → 0.</summary>
        public static float Campana(float t) { return Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI); }

        /// <summary>Punto de una curva de Bezier cuadratica.</summary>
        public static Vector3 Bezier(Vector3 a, Vector3 control, Vector3 b, float t) {
            var u = 1 - t;
            return u * u * a + 2 * u * t * control + t * t * b;
        }
    }
}
