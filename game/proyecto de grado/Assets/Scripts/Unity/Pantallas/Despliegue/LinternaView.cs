using Nexus.Unity.Tema;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// La oscuridad de los cuartos de la Fase 3 y la linterna que la rompe (2026-11-09). Sin shaders: un circulo con el
    /// borde difuminado (un sprite generado por codigo, transparente por dentro y negro por fuera) sigue al puntero, y
    /// cuatro paneles negros cubren todo lo que queda fuera de el. Lo que hay debajo (el cuarto) solo se ve dentro del haz.
    ///
    ///   · el haz sigue al raton, o a las flechas del teclado, con un poco de retraso (se siente como una linterna en la mano);
    ///   · Oscuridad (0 a 1) es lo oscuro que esta todo: a 0 se hizo la luz;
    ///   · Encendida = false cierra el haz (el cuarto se queda a oscuras del todo);
    ///   · Intensidad(p) dice cuanto ilumina el haz un punto: es lo que revela las etiquetas y habilita los clics.
    /// Las coordenadas son las del area (origen en el centro, y hacia arriba). Nunca captura clics.
    /// </summary>
    public sealed class LinternaView : MonoBehaviour {
        public float Radio = 520;
        public bool Encendida = true;

        private RectTransform _area, _agujero, _izq, _der, _arr, _aba, _brillo;
        private Image _brilloImg;
        private float _tParpadeo = 7f, _falla;
        private CanvasGroup _grupo;
        private Vector2 _meta, _actual, _ultimoRaton;
        private float _r;
        private Canvas _canvas;
        private static Sprite _sprite;

        public Vector2 Haz { get { return _actual; } }

        /// <summary>Lo oscuro que esta todo: 1 = a oscuras; 0 = con la luz encendida.</summary>
        public float Oscuridad {
            get { return _grupo.alpha; }
            set { _grupo.alpha = Mathf.Clamp01(value); }
        }

        public static LinternaView Crear(UiKit ui, RectTransform area, float radio) {
            var raiz = ui.Nodo(area, "Oscuridad");
            UiKit.Rellenar(raiz);
            var l = raiz.gameObject.AddComponent<LinternaView>();
            l._area = area;
            l.Radio = radio;
            l._grupo = raiz.gameObject.AddComponent<CanvasGroup>();
            l._grupo.blocksRaycasts = false;
            l._grupo.interactable = false;
            l._izq = Panel(ui, raiz, "Izquierda");
            l._der = Panel(ui, raiz, "Derecha");
            l._arr = Panel(ui, raiz, "Arriba");
            l._aba = Panel(ui, raiz, "Abajo");
            l._agujero = ui.Nodo(raiz, "Haz");
            l._agujero.anchorMin = l._agujero.anchorMax = l._agujero.pivot = new Vector2(0.5f, 0.5f);
            var img = l._agujero.gameObject.AddComponent<Image>();
            img.sprite = SpriteDelHaz();
            img.color = Color.black;
            img.raycastTarget = false;
            // El brillo calido: una luz suave que sigue al haz, para que se vea un cono de luz y no solo un agujero.
            l._brillo = ui.Nodo(raiz, "Brillo");
            l._brillo.anchorMin = l._brillo.anchorMax = l._brillo.pivot = new Vector2(0.5f, 0.5f);
            l._brilloImg = l._brillo.gameObject.AddComponent<Image>();
            l._brilloImg.sprite = PlanoIlustrado.Brillo();
            l._brilloImg.color = new Color(1f, 0.86f, 0.55f, 0.16f);
            l._brilloImg.raycastTarget = false;
            l._r = radio;
            var raton = Mouse.current;
            l._ultimoRaton = raton != null ? raton.position.ReadValue() : Vector2.zero;
            l.Disponer();
            return l;
        }

        private static RectTransform Panel(UiKit ui, RectTransform padre, string nombre) {
            var rt = ui.Nodo(padre, nombre);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;
            return rt;
        }

        /// <summary>Transparente en el centro (hasta la mitad del radio) y negro en el borde: el haz de una linterna.</summary>
        private static Sprite SpriteDelHaz() {
            if (_sprite != null) return _sprite;
            const int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++) {
                    var nx = (x + 0.5f) / n * 2f - 1f;
                    var ny = (y + 0.5f) / n * 2f - 1f;
                    var r = Mathf.Sqrt(nx * nx + ny * ny);
                    var t = Mathf.Clamp01((r - 0.45f) / 0.55f);
                    var alfa = t * t * (3f - 2f * t);
                    px[y * n + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(alfa * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            return _sprite;
        }

        private Camera Camara() {
            if (_canvas == null) _canvas = _area.GetComponentInParent<Canvas>();
            return _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
        }

        private Vector2 Dentro(Vector2 p) {
            var r = _area.rect;
            return new Vector2(Mathf.Clamp(p.x, r.xMin, r.xMax), Mathf.Clamp(p.y, r.yMin, r.yMax));
        }

        private void LateUpdate() {
            var dt = Time.unscaledDeltaTime;
            var raton = Mouse.current;
            if (raton != null) {
                var pos = raton.position.ReadValue();
                if ((pos - _ultimoRaton).sqrMagnitude > 0.5f) {
                    _ultimoRaton = pos;
                    Vector2 local;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, pos, Camara(), out local)) _meta = Dentro(local);
                }
            }
            var teclado = Keyboard.current;
            if (teclado != null) {
                var dir = Vector2.zero;
                if (teclado.leftArrowKey.isPressed) dir.x -= 1;
                if (teclado.rightArrowKey.isPressed) dir.x += 1;
                if (teclado.upArrowKey.isPressed) dir.y += 1;
                if (teclado.downArrowKey.isPressed) dir.y -= 1;
                if (dir != Vector2.zero) _meta = Dentro(_meta + dir.normalized * 700f * dt);
            }
            _actual = Vector2.Lerp(_actual, _meta, 1f - Mathf.Exp(-20f * dt));
            _r = Mathf.MoveTowards(_r, Encendida ? Radio : 0f, dt * 2200f);
            // La bombilla vive: el radio late con ruido y, de vez en cuando, falla un instante.
            _tParpadeo -= dt;
            if (_tParpadeo <= 0f) { _tParpadeo = Random.Range(6f, 12f); _falla = 0.15f; }
            if (_falla > 0f) _falla -= dt;
            _viva = (1f + 0.03f * (Mathf.PerlinNoise(Time.unscaledTime * 3.1f, 0.5f) - 0.5f) * 2f) * (_falla > 0f ? 0.55f : 1f);
            Disponer();
        }

        private float _viva = 1f;

        /// <summary>La linterna falla un instante (por ejemplo, con un cortocircuito).</summary>
        public void Parpadear() { _falla = 0.15f; }

        /// <summary>Coloca el haz y estira los cuatro paneles para tapar todo lo demas.</summary>
        private void Disponer() {
            var rect = _area.rect;
            float W = rect.width, H = rect.height;
            var c = _actual; var r = _r * _viva;
            _agujero.anchoredPosition = c;
            _agujero.sizeDelta = Vector2.one * Mathf.Max(0.01f, 2f * r);
            _agujero.gameObject.SetActive(r > 0.5f);
            if (_brillo != null) {
                _brillo.anchoredPosition = c;
                _brillo.sizeDelta = Vector2.one * Mathf.Max(0.01f, 2.1f * r);
                _brillo.gameObject.SetActive(r > 0.5f);
            }
            Poner(_izq, -W / 2, c.x - r, -H / 2, H / 2);
            Poner(_der, c.x + r, W / 2, -H / 2, H / 2);
            Poner(_arr, Mathf.Max(-W / 2, c.x - r), Mathf.Min(W / 2, c.x + r), c.y + r, H / 2);
            Poner(_aba, Mathf.Max(-W / 2, c.x - r), Mathf.Min(W / 2, c.x + r), -H / 2, c.y - r);
        }

        private static void Poner(RectTransform rt, float x0, float x1, float y0, float y1) {
            var w = Mathf.Max(0f, x1 - x0); var h = Mathf.Max(0f, y1 - y0);
            rt.gameObject.SetActive(w > 0.5f && h > 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2((x0 + x1) / 2f, (y0 + y1) / 2f);
        }

        /// <summary>Cuanto ilumina el haz un punto (de 0 a 1): 1 cerca del centro, 0 fuera de el.</summary>
        public float Intensidad(Vector2 p) {
            var d = Vector2.Distance(p, _actual);
            var a = 0.30f * Radio; var b = 0.95f * Radio;
            var t = Mathf.Clamp01((d - a) / (b - a));
            var luz = 1f - t * t * (3f - 2f * t);
            return luz * Mathf.Clamp01(_r / Mathf.Max(1f, Radio));
        }
    }
}
