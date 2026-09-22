using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// Un lienzo de dibujo vectorial sobre la malla de uGUI: lineas (continuas o discontinuas), flechas, curvas,
    /// rectangulos, circulos, elipses y cilindros. Es lo que dibuja el grafo de commits, las flechas de los
    /// diagramas, las lineas de vida de la secuencia y las pizarras de las recetas.
    ///
    /// Las coordenadas son como en un SVG: origen arriba a la izquierda, y hacia abajo. Asi los lienzos se
    /// escriben igual que se prototiparon.
    ///
    /// Con Tiza = true cada trazo se dibuja dos veces con un temblor minimo, que es lo que le da el aspecto de
    /// tiza a las recetas sin necesitar texturas.
    ///
    /// ★ RequireComponent(CanvasRenderer): ver GraficoRadar. Sin el, un Graphic propio revienta al destruirse.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DibujoUI : MaskableGraphic {
        private struct Trazo {
            public List<Vector2> Puntos;
            public Color Color;
            public float Grosor;
            public bool Discontinuo;
        }

        private struct Relleno {
            public List<Vector2> Poligono;   // convexo, en abanico
            public Color Color;
        }

        private readonly List<Trazo> _trazos = new List<Trazo>();
        private readonly List<Relleno> _rellenos = new List<Relleno>();
        public bool Tiza;

        public void Limpiar() { _trazos.Clear(); _rellenos.Clear(); SetVerticesDirty(); }

        // ================================================================ primitivas

        public void Linea(float x1, float y1, float x2, float y2, Color c, float grosor = 3, bool discontinua = false) {
            Polilinea(new List<Vector2> { new Vector2(x1, y1), new Vector2(x2, y2) }, c, grosor, discontinua);
        }

        public void Polilinea(List<Vector2> puntos, Color c, float grosor = 3, bool discontinua = false) {
            _trazos.Add(new Trazo { Puntos = puntos, Color = c, Grosor = grosor, Discontinuo = discontinua });
            SetVerticesDirty();
        }

        public void Flecha(float x1, float y1, float x2, float y2, Color c, float grosor = 3, bool discontinua = false) {
            Linea(x1, y1, x2, y2, c, grosor, discontinua);
            Punta(new Vector2(x1, y1), new Vector2(x2, y2), c, grosor);
        }

        /// <summary>Curva cuadratica de a hasta b pasando cerca de control, con punta de flecha opcional.</summary>
        public void Curva(Vector2 a, Vector2 control, Vector2 b, Color c, float grosor = 3, bool conPunta = true, bool discontinua = false) {
            var pts = new List<Vector2>();
            for (var i = 0; i <= 20; i++) {
                var t = i / 20f;
                pts.Add((1 - t) * (1 - t) * a + 2 * (1 - t) * t * control + t * t * b);
            }
            Polilinea(pts, c, grosor, discontinua);
            if (conPunta) Punta(pts[pts.Count - 2], b, c, grosor);
        }

        /// <summary>Curva cubica (para las aristas diagonales del grafo).</summary>
        public void CurvaCubica(Vector2 a, Vector2 c1, Vector2 c2, Vector2 b, Color c, float grosor = 3) {
            var pts = new List<Vector2>();
            for (var i = 0; i <= 24; i++) {
                var t = i / 24f; var u = 1 - t;
                pts.Add(u * u * u * a + 3 * u * u * t * c1 + 3 * u * t * t * c2 + t * t * t * b);
            }
            Polilinea(pts, c, grosor);
        }

        public void Punta(Vector2 desde, Vector2 hasta, Color c, float grosor = 3, float largo = 13) {
            var dir = (hasta - desde).normalized;
            var ang = Mathf.Atan2(dir.y, dir.x);
            foreach (var d in new[] { 2.6f, -2.6f }) {
                var p = hasta + new Vector2(Mathf.Cos(ang + d), Mathf.Sin(ang + d)) * largo;
                Linea(hasta.x, hasta.y, p.x, p.y, c, grosor);
            }
        }

        public void Rect(float x, float y, float w, float h, Color borde, float grosor = 3, Color? relleno = null, bool discontinuo = false, float radio = 8) {
            var pts = RectRedondeado(x, y, w, h, radio);
            if (relleno.HasValue) _rellenos.Add(new Relleno { Poligono = pts, Color = relleno.Value });
            var cerrado = new List<Vector2>(pts) { pts[0] };
            if (grosor > 0) Polilinea(cerrado, borde, grosor, discontinuo);
            SetVerticesDirty();
        }

        public void Circulo(float cx, float cy, float r, Color borde, float grosor = 3, Color? relleno = null, bool discontinuo = false) {
            Elipse(cx, cy, r, r, borde, grosor, relleno, discontinuo);
        }

        public void Elipse(float cx, float cy, float rx, float ry, Color borde, float grosor = 3, Color? relleno = null, bool discontinuo = false) {
            var pts = new List<Vector2>();
            for (var i = 0; i < 40; i++) {
                var a = i * Mathf.PI * 2 / 40;
                pts.Add(new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry));
            }
            if (relleno.HasValue) _rellenos.Add(new Relleno { Poligono = pts, Color = relleno.Value });
            if (grosor > 0) Polilinea(new List<Vector2>(pts) { pts[0] }, borde, grosor, discontinuo);
            SetVerticesDirty();
        }

        /// <summary>El simbolo de base de datos: un cilindro.</summary>
        public void Cilindro(float x, float y, float w, float h, Color borde, float grosor = 3, Color? relleno = null) {
            if (relleno.HasValue) {
                var cuerpo = new List<Vector2> { new Vector2(x, y + 10), new Vector2(x + w, y + 10), new Vector2(x + w, y + h - 10), new Vector2(x, y + h - 10) };
                _rellenos.Add(new Relleno { Poligono = cuerpo, Color = relleno.Value });
            }
            Elipse(x + w / 2, y + 10, w / 2, 10, borde, grosor, relleno);
            Linea(x, y + 10, x, y + h - 10, borde, grosor);
            Linea(x + w, y + 10, x + w, y + h - 10, borde, grosor);
            var abajo = new List<Vector2>();
            for (var i = 0; i <= 20; i++) {
                var a = Mathf.PI * i / 20;
                abajo.Add(new Vector2(x + w / 2 - Mathf.Cos(a) * w / 2, y + h - 10 + Mathf.Sin(a) * 10));
            }
            Polilinea(abajo, borde, grosor);
        }

        // ---------------------------------------------------------------- iconos de pizarra

        public void Bicho(float x, float y, Color c, float grosor = 3) {
            Elipse(x, y, 9, 12, c, grosor);
            foreach (var s in new[] { -1, 1 })
                foreach (var dy in new[] { -5, 2, 8 })
                    Linea(x + s * 8, y + dy, x + s * 16, y + dy - 3, c, 2);
        }

        public void Persona(float x, float y, Color c, float grosor = 3) {
            Circulo(x, y, 11, c, grosor);
            var hombros = new List<Vector2>();
            for (var i = 0; i <= 12; i++) {
                var a = Mathf.PI + Mathf.PI * i / 12;
                hombros.Add(new Vector2(x + Mathf.Cos(a) * 16, y + 34 + Mathf.Sin(a) * 14));
            }
            Polilinea(hombros, c, grosor);
        }

        public void Cruz(float x, float y, Color c, float r = 14) { Linea(x - r, y - r, x + r, y + r, c, 5); Linea(x + r, y - r, x - r, y + r, c, 5); }
        public void Check(float x, float y, Color c) { Polilinea(new List<Vector2> { new Vector2(x - 14, y), new Vector2(x - 4, y + 11), new Vector2(x + 16, y - 13) }, c, 5); }
        public void Reloj(float x, float y, Color c) { Circulo(x, y, 26, c); Linea(x, y, x, y - 17, c); Linea(x, y, x + 12, y + 6, c); }

        public void Mano(float x, float y, Color c) {
            var pts = new List<Vector2> { new Vector2(x, y), new Vector2(x, y + 34), new Vector2(x + 9, y + 26), new Vector2(x + 16, y + 41),
                                          new Vector2(x + 23, y + 38), new Vector2(x + 16, y + 23), new Vector2(x + 28, y + 22) };
            _rellenos.Add(new Relleno { Poligono = pts, Color = c });
            SetVerticesDirty();
        }

        // ================================================================ malla

        private static List<Vector2> RectRedondeado(float x, float y, float w, float h, float r) {
            r = Mathf.Min(r, Mathf.Min(w, h) / 2);
            var pts = new List<Vector2>();
            var esquinas = new[] { new Vector2(x + w - r, y + r), new Vector2(x + w - r, y + h - r), new Vector2(x + r, y + h - r), new Vector2(x + r, y + r) };
            var inicio = new[] { -90f, 0f, 90f, 180f };
            for (var k = 0; k < 4; k++)
                for (var i = 0; i <= 5; i++) {
                    var a = (inicio[k] + 90f * i / 5) * Mathf.Deg2Rad;
                    pts.Add(esquinas[k] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                }
            return pts;
        }

        private Vector2 AMalla(Vector2 p) {
            var r = rectTransform.rect;
            return new Vector2(r.xMin + p.x, r.yMax - p.y);
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
            foreach (var f in _rellenos) {
                var inicio = vh.currentVertCount;
                var c = Centro(f.Poligono);
                vh.AddVert(AMalla(c), f.Color, Vector2.zero);
                foreach (var p in f.Poligono) vh.AddVert(AMalla(p), f.Color, Vector2.zero);
                for (var i = 0; i < f.Poligono.Count; i++)
                    vh.AddTriangle(inicio, inicio + 1 + i, inicio + 1 + (i + 1) % f.Poligono.Count);
            }
            var semilla = 7;
            foreach (var t in _trazos) {
                DibujarTrazo(vh, t, Vector2.zero, t.Color);
                if (Tiza) {
                    // un segundo trazo, un pelo movido y mas tenue: el borde irregular de la tiza
                    semilla = semilla * 31 + 17;
                    var off = new Vector2((semilla % 5 - 2) * 0.5f, ((semilla / 5) % 5 - 2) * 0.5f);
                    var c = t.Color; c.a *= 0.45f;
                    DibujarTrazo(vh, t, off, c);
                }
            }
        }

        private static Vector2 Centro(List<Vector2> pts) {
            var s = Vector2.zero;
            foreach (var p in pts) s += p;
            return s / Mathf.Max(1, pts.Count);
        }

        private void DibujarTrazo(VertexHelper vh, Trazo t, Vector2 off, Color c) {
            for (var i = 0; i < t.Puntos.Count - 1; i++) {
                var a = t.Puntos[i] + off; var b = t.Puntos[i + 1] + off;
                if (!t.Discontinuo) { Segmento(vh, a, b, c, t.Grosor); continue; }
                var largo = Vector2.Distance(a, b);
                var dir = (b - a) / Mathf.Max(0.001f, largo);
                for (var d = 0f; d < largo; d += 14f)
                    Segmento(vh, a + dir * d, a + dir * Mathf.Min(d + 8f, largo), c, t.Grosor);
            }
        }

        private void Segmento(VertexHelper vh, Vector2 a, Vector2 b, Color c, float grosor) {
            var ma = AMalla(a); var mb = AMalla(b);
            var n = new Vector2(-(mb - ma).y, (mb - ma).x).normalized * (grosor * 0.5f);
            var e = (mb - ma).normalized * (grosor * 0.5f);   // extremos un poco alargados: uniones sin huecos
            var i = vh.currentVertCount;
            vh.AddVert(ma - e - n, c, Vector2.zero);
            vh.AddVert(ma - e + n, c, Vector2.zero);
            vh.AddVert(mb + e + n, c, Vector2.zero);
            vh.AddVert(mb + e - n, c, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }

    /// <summary>
    /// Un lienzo de posicion libre: un DibujoUI de fondo, y encima textos y zonas pulsables colocados en
    /// coordenadas «de SVG» (arriba a la izquierda). Es lo que usan los lienzos de los minijuegos y las recetas.
    /// </summary>
    public sealed class Lamina {
        private readonly UiKit _ui;
        public RectTransform Raiz { get; private set; }
        public DibujoUI Dibujo { get; private set; }

        public Lamina(UiKit ui, Transform padre, float ancho, float alto, string nombre = "Lamina") {
            _ui = ui;
            Raiz = ui.Nodo(padre, nombre);
            UiKit.Tamano(Raiz, ancho, alto);
            var fondo = ui.Nodo(Raiz, "Dibujo");
            UiKit.Rellenar(fondo);
            fondo.gameObject.AddComponent<CanvasRenderer>();
            Dibujo = fondo.gameObject.AddComponent<DibujoUI>();
            Dibujo.raycastTarget = false;
        }

        /// <summary>Coloca un RectTransform en (x, y) con su tamaño, medido desde arriba a la izquierda.</summary>
        public static void Colocar(RectTransform rt, float x, float y, float w, float h) {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        public TMP_Text Texto(float x, float y, float w, float h, string texto, float tam, Color color,
                              TextAlignmentOptions alineacion = TextAlignmentOptions.MidlineLeft, TMP_FontAsset fuente = null, bool negrita = false) {
            var t = _ui.Texto(Raiz, texto, EstiloTexto.Cuerpo, color, alineacion);
            if (fuente != null) t.font = fuente;
            t.fontSize = tam;
            t.fontStyle = negrita ? FontStyles.Bold : FontStyles.Normal;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            Colocar((RectTransform)t.transform, x, y, w, h);
            return t;
        }

        /// <summary>Una zona invisible pulsable (una caja del diagrama, una fila del grafo).</summary>
        public Button Zona(float x, float y, float w, float h, System.Action alPulsar, string nombre = "Zona") {
            var rt = _ui.Nodo(Raiz, nombre);
            Colocar(rt, x, y, w, h);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colores = ColorBlock.defaultColorBlock;
            colores.normalColor = new Color(1, 1, 1, 0);
            colores.highlightedColor = new Color(1, 1, 1, 0.06f);
            colores.pressedColor = new Color(1, 1, 1, 0.12f);
            colores.selectedColor = new Color(1, 1, 1, 0);
            b.colors = colores;
            if (alPulsar != null) b.onClick.AddListener(() => alPulsar());
            return b;
        }

        /// <summary>Una pastilla de color con texto (etiqueta de flecha, chip de valor, marca).</summary>
        public RectTransform Pastilla(float cx, float cy, string texto, float tam, Color fondo, Color colorTexto, Color? borde = null) {
            var ancho = texto.Length * tam * 0.55f + 26;
            var rt = _ui.Nodo(Raiz, "Pastilla");
            Colocar(rt, cx - ancho / 2, cy - (tam + 12) / 2, ancho, tam + 12);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = _ui.SpriteRedondeado();
            img.type = Image.Type.Sliced;
            img.color = fondo;
            if (borde.HasValue) { var o = rt.gameObject.AddComponent<Outline>(); o.effectColor = borde.Value; o.effectDistance = new Vector2(2, -2); }
            var t = _ui.Texto(rt, texto, EstiloTexto.Pequeno, colorTexto, TextAlignmentOptions.Center);
            t.fontSize = tam;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Rellenar((RectTransform)t.transform);
            return rt;
        }
    }
}
