using Nexus.Unity.Pantallas.Minijuegos.Corregir;
using Nexus.Unity.Tema;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>
    /// Efectos de interfaz sueltos que comparten varias pantallas (2026-11-10): chispas que salen disparadas y caen, y
    /// motas de luz que flotan. Sin shaders ni texturas: Images con el circulo del UiKit y tiempo sin escalar (Animador).
    /// </summary>
    public static class EfectosUI {
        /// <summary>
        /// Chispas que salen disparadas en circulo y caen. 'centro' esta en coordenadas de lamina (origen arriba a la
        /// izquierda, y hacia abajo) si 'arribaIzquierda'; si no, en las del padre centrado.
        /// </summary>
        public static void Chispas(UiKit ui, Animador anim, Transform padre, Vector2 centro, Color color, int n, float alcance, bool arribaIzquierda, float gravedad = 50f) {
            for (var i = 0; i < n; i++) {
                var p = ui.Nodo(padre, "Chispa");
                if (arribaIzquierda) { p.anchorMin = p.anchorMax = new Vector2(0, 1); } else { p.anchorMin = p.anchorMax = new Vector2(0.5f, 0.5f); }
                p.pivot = new Vector2(0.5f, 0.5f);
                p.sizeDelta = new Vector2(8 + (i % 3) * 3, 8 + (i % 3) * 3);
                var img = p.gameObject.AddComponent<Image>();
                img.sprite = ui.SpriteCircular();
                img.raycastTarget = false;
                var ang = (i / (float)n) * Mathf.PI * 2 + (i % 2) * 0.4f;
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                var lejos = alcance * (0.55f + (i % 4) * 0.18f);
                var origen = arribaIzquierda ? new Vector2(centro.x, -centro.y) : centro;
                p.anchoredPosition = origen;
                anim.Lanzar(0.75f, t => {
                    if (p == null) return;
                    var e = Curvas.SalidaCubica(t);
                    p.anchoredPosition = origen + dir * lejos * e + new Vector2(0, -gravedad * t * t);
                    img.color = NexusTheme.Alfa(color, 1 - t);
                    p.localScale = Vector3.one * (1 - 0.6f * t);
                }, () => { if (p != null) Object.Destroy(p.gameObject); }, 0.02f * (i % 5), p.gameObject);
            }
        }

        /// <summary>Un aro que se abre y se desvanece (un latido de luz) sobre 'centro' (coordenadas de lamina).</summary>
        public static void Aro(UiKit ui, Animador anim, Transform padre, Vector2 centro, Color color, float radioFinal, float duracion = 0.55f) {
            var rt = ui.Nodo(padre, "Aro");
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(centro.x, -centro.y);
            rt.sizeDelta = Vector2.one * radioFinal * 2f;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = PlanoIlustrado.Brillo();
            img.raycastTarget = false;
            anim.Lanzar(duracion, t => {
                if (rt == null) return;
                rt.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, Curvas.SalidaCubica(t));
                img.color = NexusTheme.Alfa(color, 0.7f * (1 - t));
            }, () => { if (rt != null) Object.Destroy(rt.gameObject); }, 0, rt.gameObject);
        }
    }

    /// <summary>Motas de luz que suben despacio por un rectangulo: da vida a un fondo quieto. Tiempo sin escalar.</summary>
    public sealed class MotasFlotantes : MonoBehaviour {
        private struct Mota { public RectTransform Rt; public float Velocidad, Fase, Amplitud; }
        private readonly System.Collections.Generic.List<Mota> _motas = new System.Collections.Generic.List<Mota>();
        private float _ancho, _alto;

        public static MotasFlotantes Crear(UiKit ui, RectTransform padre, float ancho, float alto, int cuantas, Color color) {
            var raiz = ui.Nodo(padre, "Motas");
            raiz.anchorMin = raiz.anchorMax = raiz.pivot = new Vector2(0.5f, 0.5f);
            raiz.anchoredPosition = Vector2.zero;
            raiz.sizeDelta = new Vector2(ancho, alto);
            var m = raiz.gameObject.AddComponent<MotasFlotantes>();
            m._ancho = ancho; m._alto = alto;
            for (var i = 0; i < cuantas; i++) {
                var rt = ui.Nodo(raiz, "Mota");
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                var tam = Random.Range(2.5f, 6f);
                rt.sizeDelta = new Vector2(tam, tam);
                rt.anchoredPosition = new Vector2(Random.Range(-ancho / 2, ancho / 2), Random.Range(-alto / 2, alto / 2));
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = ui.SpriteCircular();
                img.color = NexusTheme.Alfa(color, Random.Range(0.12f, 0.4f));
                img.raycastTarget = false;
                m._motas.Add(new Mota { Rt = rt, Velocidad = Random.Range(6f, 20f), Fase = Random.Range(0f, 6.28f), Amplitud = Random.Range(6f, 26f) });
            }
            return m;
        }

        private void Update() {
            var dt = Time.unscaledDeltaTime; var t = Time.unscaledTime;
            foreach (var m in _motas) {
                if (m.Rt == null) continue;
                var p = m.Rt.anchoredPosition;
                p.y += m.Velocidad * dt;
                p.x += Mathf.Sin(t * 0.6f + m.Fase) * m.Amplitud * dt * 0.5f;
                if (p.y > _alto / 2f + 8f) { p.y = -_alto / 2f - 8f; p.x = Random.Range(-_ancho / 2, _ancho / 2); }
                m.Rt.anchoredPosition = p;
            }
        }
    }
}
