using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// Las piezas de luz de la direccion de arte (2026-11-15, docs/08-arte): halos y haces que SUMAN luz (shader
    /// «Nexus/UI Aditivo», en Resources/Shaders), cielos y velos de niebla con degradado de verdad, formas organicas con
    /// degradado (FormaDegradada) y el trazado de curvas. Coordenadas «de SVG» (arriba a la izquierda) dentro del padre;
    /// el orden de creacion es el orden de pintado. Las texturas se generan una vez y se comparten.
    /// </summary>
    public static class LuzUI {
        private static Material _aditivo;
        private static bool _buscado;
        private static Texture2D _radial, _haz;
        private static readonly Dictionary<string, Texture2D> _verticales = new Dictionary<string, Texture2D>();

        /// <summary>El material aditivo, o null si el shader no esta (entonces se pinta con mezcla normal).</summary>
        public static Material Aditivo {
            get {
                if (_aditivo != null || _buscado) return _aditivo;
                _buscado = true;
                var s = Resources.Load<Shader>("Shaders/UIAditivo");
                if (s == null) s = Shader.Find("Nexus/UI Aditivo");
                if (s == null) { Debug.LogWarning("[LuzUI] falta Resources/Shaders/UIAditivo: los halos se pintan con mezcla normal"); return null; }
                return _aditivo = new Material(s) { name = "UI Aditivo (Nexus)" };
            }
        }

        /// <summary>Halo: caida suave casi gaussiana, blanca, para teñir.</summary>
        public static Texture2D TexturaRadial {
            get {
                if (_radial != null) return _radial;
                const int n = 128;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Halo (LuzUI)" };
                var px = new Color[n * n];
                for (var y = 0; y < n; y++)
                    for (var x = 0; x < n; x++) {
                        var d = new Vector2(x - n / 2f + 0.5f, y - n / 2f + 0.5f).magnitude / (n / 2f);
                        var a = Mathf.Clamp01(1 - d);
                        px[y * n + x] = new Color(1, 1, 1, a * a * a);
                    }
                t.SetPixels(px); t.Apply(false, true);
                return _radial = t;
            }
        }

        /// <summary>Haz de luz: suave a los lados, fuerte en el origen (arriba) y desvanecido al final.</summary>
        public static Texture2D TexturaHaz {
            get {
                if (_haz != null) return _haz;
                const int w = 64, h = 256;
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Haz (LuzUI)" };
                var px = new Color[w * h];
                for (var y = 0; y < h; y++)
                    for (var x = 0; x < w; x++) {
                        var lado = 1 - Mathf.Abs((x + 0.5f) / w * 2 - 1);
                        var largo = y / (float)(h - 1);
                        px[y * w + x] = new Color(1, 1, 1, Mathf.SmoothStep(0, 1, lado) * largo * largo);
                    }
                t.SetPixels(px); t.Apply(false, true);
                return _haz = t;
            }
        }

        /// <summary>Degradado vertical de varias paradas (posicion 0 = arriba … 1 = abajo). Se guarda por paradas.</summary>
        public static Texture2D TexturaVertical(params KeyValuePair<float, Color>[] paradas) {
            var clave = new System.Text.StringBuilder();
            foreach (var p in paradas) clave.Append(p.Key).Append(':').Append(ColorUtility.ToHtmlStringRGBA(p.Value)).Append(';');
            Texture2D t;
            if (_verticales.TryGetValue(clave.ToString(), out t) && t != null) return t;
            const int h = 256;
            t = new Texture2D(2, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Degradado (LuzUI)" };
            var px = new Color[2 * h];
            for (var y = 0; y < h; y++) {
                var p = 1 - y / (float)(h - 1);
                var c = paradas[0].Value;
                for (var i = 0; i + 1 < paradas.Length; i++) {
                    if (p >= paradas[i].Key && p <= paradas[i + 1].Key) {
                        c = Color.Lerp(paradas[i].Value, paradas[i + 1].Value, Mathf.InverseLerp(paradas[i].Key, paradas[i + 1].Key, p));
                        break;
                    }
                    if (p > paradas[i + 1].Key) c = paradas[i + 1].Value;
                }
                px[y * 2] = px[y * 2 + 1] = c;
            }
            t.SetPixels(px); t.Apply(false, true);
            _verticales[clave.ToString()] = t;
            return t;
        }

        public static KeyValuePair<float, Color> Parada(float posicion, Color color) { return new KeyValuePair<float, Color>(posicion, color); }

        // ---------------------------------------------------------------- piezas

        public static RawImage Imagen(UiKit ui, Transform padre, Texture tex, float x, float y, float w, float h, Color c, bool suma, string nombre = "Luz") {
            var r = ui.Nodo(padre, nombre);
            Lamina.Colocar(r, x, y, w, h);
            var img = r.gameObject.AddComponent<RawImage>();
            img.texture = tex;
            img.color = c;
            img.raycastTarget = false;
            if (suma && Aditivo != null) img.material = Aditivo;
            return img;
        }

        /// <summary>Un cielo (o cualquier fondo) con degradado vertical de varias paradas.</summary>
        public static RawImage Cielo(UiKit ui, Transform padre, float x, float y, float w, float h, params KeyValuePair<float, Color>[] paradas) {
            return Imagen(ui, padre, TexturaVertical(paradas), x, y, w, h, Color.white, false, "Cielo");
        }

        /// <summary>Velo de niebla: su color con el alfa de arriba a abajo. Va encima de lo que tapa.</summary>
        public static RawImage Niebla(UiKit ui, Transform padre, float x, float y, float w, float h, Color c, float alfaArriba, float alfaAbajo) {
            return Imagen(ui, padre, TexturaVertical(Parada(0, NexusTheme.Alfa(c, alfaArriba)), Parada(1, NexusTheme.Alfa(c, alfaAbajo))), x, y, w, h, Color.white, false, "Niebla");
        }

        /// <summary>Halo aditivo centrado en (cx, cy).</summary>
        public static RawImage Brillo(UiKit ui, Transform padre, float cx, float cy, float rx, float ry, Color c) {
            return Imagen(ui, padre, TexturaRadial, cx - rx, cy - ry, rx * 2, ry * 2, c, true, "Brillo");
        }

        /// <summary>Haz de luz desde (x, y) hacia abajo, girado 'grados' (positivo = hacia la derecha).</summary>
        public static RawImage Haz(UiKit ui, Transform padre, float x, float y, float largo, float ancho, float grados, Color c) {
            var img = Imagen(ui, padre, TexturaHaz, 0, 0, ancho, largo, c, true, "Haz");
            var r = img.rectTransform;
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(x, -y);
            r.localRotation = Quaternion.Euler(0, 0, grados);
            return img;
        }

        /// <summary>Forma con degradado lineal (de 'a' en 'desde' a 'b' en 'hasta'), estirada sobre todo el padre.</summary>
        public static FormaDegradada Forma(UiKit ui, Transform padre, List<Vector2> pts, Color a, Color b, Vector2 desde, Vector2 hasta, string nombre = "Forma") {
            var r = ui.Nodo(padre, nombre);
            UiKit.Rellenar(r);
            // El CanvasRenderer primero: AddComponent de un Graphic propio no lo trae solo (sin el, no se pinta nunca).
            r.gameObject.AddComponent<CanvasRenderer>();
            var f = r.gameObject.AddComponent<FormaDegradada>();
            f.raycastTarget = false;
            f.Puntos = pts; f.ColorA = a; f.ColorB = b; f.Desde = desde; f.Hasta = hasta;
            f.SetAllDirty();
            return f;
        }

        public static FormaDegradada Forma(UiKit ui, Transform padre, List<Vector2> pts, Color c, string nombre = "Forma") {
            return Forma(ui, padre, pts, c, c, Vector2.zero, Vector2.up, nombre);
        }

        // ---------------------------------------------------------------- geometria

        public static List<Vector2> Elipse(float cx, float cy, float rx, float ry, int n = 48, float a0 = 0, float a1 = 360) {
            var p = new List<Vector2>();
            var lleno = Mathf.Approximately(a1 - a0, 360);
            var pasos = lleno ? n : n + 1;
            for (var i = 0; i < pasos; i++) {
                var a = Mathf.Deg2Rad * Mathf.Lerp(a0, a1, i / (float)n);
                p.Add(new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry));
            }
            return p;
        }

        public static List<Vector2> Redondeado(float x, float y, float w, float h, float r, int pasos = 8) {
            var p = new List<Vector2>();
            r = Mathf.Min(r, Mathf.Min(w, h) / 2);
            void Esquina(float cx, float cy, float desde) {
                for (var i = 0; i <= pasos; i++) {
                    var a = Mathf.Deg2Rad * (desde + 90f * i / pasos);
                    p.Add(new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r));
                }
            }
            Esquina(x + w - r, y + r, 270); Esquina(x + w - r, y + h - r, 0); Esquina(x + r, y + h - r, 90); Esquina(x + r, y + r, 180);
            return p;
        }

        /// <summary>Banda en arco (costilla, puente): arco exterior y arco interior. 'bamboleo' evita el circulo perfecto.</summary>
        public static List<Vector2> Arco(float cx, float cy, float rx, float ry, float grueso, float a0, float a1, int n = 40, float bamboleo = 0, int semilla = 0) {
            var fuera = new List<Vector2>();
            var dentro = new List<Vector2>();
            for (var i = 0; i <= n; i++) {
                var t = i / (float)n;
                var a = Mathf.Deg2Rad * Mathf.Lerp(a0, a1, t);
                var w = 1 + bamboleo * Mathf.Sin(t * 9.3f + semilla) * Mathf.Sin(t * 3.1f + semilla * 0.7f);
                fuera.Add(new Vector2(cx + Mathf.Cos(a) * rx * w, cy + Mathf.Sin(a) * ry * w));
                dentro.Add(new Vector2(cx + Mathf.Cos(a) * (rx - grueso) * w, cy + Mathf.Sin(a) * (ry - grueso) * w));
            }
            dentro.Reverse();
            fuera.AddRange(dentro);
            return fuera;
        }

        /// <summary>Tubo de grosor variable a lo largo de una polilinea (cable, tuberia, voluta).</summary>
        public static List<Vector2> Tubo(List<Vector2> eje, float g0, float g1) {
            var izq = new List<Vector2>();
            var der = new List<Vector2>();
            for (var i = 0; i < eje.Count; i++) {
                var a = eje[Mathf.Max(0, i - 1)];
                var b = eje[Mathf.Min(eje.Count - 1, i + 1)];
                var n = new Vector2(-(b - a).y, (b - a).x).normalized;
                var g = Mathf.Lerp(g0, g1, i / (float)Mathf.Max(1, eje.Count - 1)) * 0.5f;
                izq.Add(eje[i] + n * g);
                der.Add(eje[i] - n * g);
            }
            der.Reverse();
            izq.AddRange(der);
            return izq;
        }

        public static List<Vector2> Mover(List<Vector2> p, float dx, float dy, float k = 1f, float cx = 0, float cy = 0) {
            var r = new List<Vector2>(p.Count);
            foreach (var v in p) r.Add(new Vector2(cx + (v.x - cx) * k + dx, cy + (v.y - cy) * k + dy));
            return r;
        }
    }

    /// <summary>Trazador de contornos: rectas y curvas de Bezier muestreadas, como un path de SVG.</summary>
    public sealed class RutaDeCurva {
        public readonly List<Vector2> P = new List<Vector2>();
        public RutaDeCurva(float x, float y) { P.Add(new Vector2(x, y)); }
        public RutaDeCurva L(float x, float y) { P.Add(new Vector2(x, y)); return this; }

        public RutaDeCurva C(float c1x, float c1y, float c2x, float c2y, float x, float y, int pasos = 18) {
            var a = P[P.Count - 1];
            Vector2 c1 = new Vector2(c1x, c1y), c2 = new Vector2(c2x, c2y), b = new Vector2(x, y);
            for (var i = 1; i <= pasos; i++) {
                var t = i / (float)pasos; var u = 1 - t;
                P.Add(u * u * u * a + 3 * u * u * t * c1 + 3 * u * t * t * c2 + t * t * t * b);
            }
            return this;
        }

        /// <summary>Curva cuadratica (un solo punto de control).</summary>
        public RutaDeCurva Q(float cx, float cy, float x, float y, int pasos = 14) {
            var a = P[P.Count - 1];
            Vector2 c = new Vector2(cx, cy), b = new Vector2(x, y);
            for (var i = 1; i <= pasos; i++) {
                var t = i / (float)pasos; var u = 1 - t;
                P.Add(u * u * a + 2 * u * t * c + t * t * b);
            }
            return this;
        }

        public List<Vector2> Puntos() { return P; }
    }
}
