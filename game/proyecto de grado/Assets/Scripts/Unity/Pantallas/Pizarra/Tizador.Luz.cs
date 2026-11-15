using System.Collections.Generic;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>
    /// (2026-11-15, direccion de arte, docs/08-arte) El lenguaje nuevo de las escenas de pizarra: nada de tiza ni de
    /// monigotes, sino una «vitrina» de 600×300 con su luz, su niebla y sus objetos del mundo, pintados con formas con
    /// degradado (FormaDegradada), halos aditivos (LuzUI) y rotulos en Rajdhani u Orbitron.
    ///
    /// Una escena nueva empieza con <see cref="Vitrina"/>: devuelve otro Tizador que dibuja DENTRO de un contenedor recortado
    /// del tamaño de la escena, donde cada pieza es su propio objeto y el orden de creacion es el orden de pintado (al
    /// contrario que DibujoUI, que pinta todos los trazos encima de todos los rellenos). Dentro de la vitrina solo se usan
    /// los metodos de este archivo: los de tiza dibujan en la lamina de debajo.
    /// </summary>
    public sealed partial class Tizador {
        private RectTransform _capa;

        private Tizador(Lamina l, TMP_FontAsset fuente, RectTransform capa, float k) : this(l, fuente, 0, 0, k) { _capa = capa; }

        private UiKit Ui { get { return _l.Ui; } }
        private Transform Capa { get { return _capa != null ? (Transform)_capa : _l.Raiz; } }

        /// <summary>El cuadro de la escena (600×300 de prototipo), recortado. Lo que se dibuje con el Tizador devuelto vive dentro.</summary>
        public Tizador Vitrina(float ancho = 600, float alto = 300) {
            var rt = Ui.Nodo(Capa, "Vitrina");
            Lamina.Colocar(rt, X(0), Y(0), ancho * _k, alto * _k);
            rt.gameObject.AddComponent<RectMask2D>();
            return new Tizador(_l, _fuente, rt, _k);
        }

        // ---------------------------------------------------------------- formas

        /// <summary>
        /// A coordenadas de la capa, sin puntos repetidos (ni seguidos ni el ultimo igual al primero): el recorte de orejas
        /// de FormaDegradada no sabe cortar un vertice de area cero y acaba abanicando lo que quede.
        /// </summary>
        private List<Vector2> T(List<Vector2> pts) {
            var r = new List<Vector2>(pts.Count);
            foreach (var p in pts) {
                var q = new Vector2(X(p.x), Y(p.y));
                if (r.Count > 0 && (r[r.Count - 1] - q).sqrMagnitude < 0.0001f) continue;
                r.Add(q);
            }
            while (r.Count > 3 && (r[r.Count - 1] - r[0]).sqrMagnitude < 0.0001f) r.RemoveAt(r.Count - 1);
            return r;
        }

        /// <summary>Forma de un color.</summary>
        public FormaDegradada Forma(List<Vector2> pts, Color c) { return LuzUI.Forma(Ui, Capa, T(pts), c); }

        /// <summary>Forma con degradado vertical: 'arriba' en lo mas alto de la forma, 'abajo' en lo mas bajo.</summary>
        public FormaDegradada Forma(List<Vector2> pts, Color arriba, Color abajo) {
            float y0 = float.MaxValue, y1 = float.MinValue;
            foreach (var p in pts) { y0 = Mathf.Min(y0, p.y); y1 = Mathf.Max(y1, p.y); }
            return Forma(pts, arriba, abajo, new Vector2(0, y0), new Vector2(0, y1 + 0.01f));
        }

        /// <summary>Forma con degradado a lo largo de un eje cualquiera (en coordenadas de la escena).</summary>
        public FormaDegradada Forma(List<Vector2> pts, Color a, Color b, Vector2 desde, Vector2 hasta) {
            return LuzUI.Forma(Ui, Capa, T(pts), a, b, new Vector2(X(desde.x), Y(desde.y)), new Vector2(X(hasta.x), Y(hasta.y)));
        }

        /// <summary>Una forma que SUMA luz (cristal encendido, filo de neon).</summary>
        public FormaDegradada FormaDeLuz(List<Vector2> pts, Color c) {
            var f = Forma(pts, c);
            if (LuzUI.Aditivo != null) f.material = LuzUI.Aditivo;
            return f;
        }

        /// <summary>Tubo a lo largo de una polilinea: cable, tuberia, voluta de vapor.</summary>
        public FormaDegradada Cable(List<Vector2> eje, float g0, float g1, Color c) { return Forma(LuzUI.Tubo(eje, g0, g1), c); }

        /// <summary>Un tubo de neon: el halo ancho que suma luz y el nucleo casi blanco.</summary>
        public void Neon(List<Vector2> eje, float g, Color c) {
            FormaDeLuz(LuzUI.Tubo(eje, g * 6, g * 6), NexusTheme.Alfa(c, 0.10f));
            FormaDeLuz(LuzUI.Tubo(eje, g * 2.6f, g * 2.6f), NexusTheme.Alfa(c, 0.28f));
            Forma(LuzUI.Tubo(eje, g, g), Color.Lerp(c, Color.white, 0.55f));
        }

        // ---------------------------------------------------------------- luz y atmosfera

        public RawImage Halo(float cx, float cy, float rx, float ry, Color c) { return LuzUI.Brillo(Ui, Capa, X(cx), Y(cy), rx * _k, ry * _k, c); }
        public RawImage Halo(float cx, float cy, float r, Color c) { return Halo(cx, cy, r, r, c); }

        public RawImage Haz(float x, float y, float largo, float ancho, float grados, Color c) {
            return LuzUI.Haz(Ui, Capa, X(x), Y(y), largo * _k, ancho * _k, grados, c);
        }

        /// <summary>Velo de niebla: transparente arriba (aArriba) y mas denso abajo (aAbajo).</summary>
        public RawImage Velo(float x, float y, float w, float h, Color c, float aArriba, float aAbajo) {
            return LuzUI.Niebla(Ui, Capa, X(x), Y(y), w * _k, h * _k, c, aArriba, aAbajo);
        }

        public RawImage Cielo(float x, float y, float w, float h, params KeyValuePair<float, Color>[] paradas) {
            return LuzUI.Cielo(Ui, Capa, X(x), Y(y), w * _k, h * _k, paradas);
        }

        /// <summary>Motas de luz alrededor de una fuente, mas densas cerca. Siempre las mismas para la misma semilla.</summary>
        public void Motas(float cx, float cy, float rx, float ry, int n, int semilla, Color c) {
            var azar = new System.Random(semilla);
            for (var i = 0; i < n; i++) {
                var a = (float)(azar.NextDouble() * Mathf.PI * 2);
                var d = Mathf.Pow((float)azar.NextDouble(), 0.7f);
                var x = cx + Mathf.Cos(a) * rx * d;
                var y = cy + Mathf.Sin(a) * ry * d;
                var r = 1.5f + (float)azar.NextDouble() * 3f;
                Halo(x, y, r * 2.2f, NexusTheme.Alfa(c, 0.25f + 0.5f * (1 - d)));
            }
        }

        public LatidoDeLuz Latido(Graphic g, float min, float max, float periodo, float fase = 0) { return LatidoDeLuz.En(g, min, max, periodo, fase); }
        public LatidoDeLuz Parpadeo(Graphic g, float min, float fase = 0) { return LatidoDeLuz.En(g, min, 1, 1.6f, fase, true); }

        // ---------------------------------------------------------------- rotulos

        /// <summary>
        /// Rotulo de maqueta (Rajdhani Bold; Orbitron si 'display'). (x, y) es el centro vertical de la linea; 'alinear' -1 a la
        /// izquierda de x, 0 centrado, 1 a la derecha. 'mayus' lo pasa a mayusculas espaciadas. Admite <color> de TMP.
        /// </summary>
        public TMP_Text Rotulo(float x, float y, string texto, float tam, Color c, int alinear = 0, bool mayus = false, bool display = false, float ancho = 0) {
            var al = alinear < 0 ? TextAlignmentOptions.MidlineLeft : alinear > 0 ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.Midline;
            var t = Ui.Texto(Capa, mayus ? texto.ToUpperInvariant() : texto, EstiloTexto.Cuerpo, c, al);
            t.font = display ? Ui.Tema.FuenteDisplay : Ui.Tema.FuenteInterfazNegrita;
            t.fontSize = tam * _k;
            t.fontStyle = FontStyles.Normal;
            t.characterSpacing = mayus ? 6 : 0;
            t.textWrappingMode = ancho > 0 ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            var w = (ancho > 0 ? ancho : 600) * _k;
            var h = tam * (ancho > 0 ? 3.2f : 1.6f) * _k;
            var x0 = alinear < 0 ? X(x) : alinear > 0 ? X(x) - w : X(x) - w / 2;
            Lamina.Colocar(t.rectTransform, x0, Y(y) - h / 2, w, h);
            return t;
        }

        // ---------------------------------------------------------------- geometria de apoyo

        /// <summary>Gira unos puntos 'grados' alrededor de (cx, cy) (positivo = como las agujas del reloj, con y hacia abajo).</summary>
        public static List<Vector2> Rotar(List<Vector2> pts, float cx, float cy, float grados) {
            var a = grados * Mathf.Deg2Rad;
            float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
            var r = new List<Vector2>(pts.Count);
            foreach (var p in pts) {
                float dx = p.x - cx, dy = p.y - cy;
                r.Add(new Vector2(cx + dx * cos - dy * sin, cy + dx * sin + dy * cos));
            }
            return r;
        }

        /// <summary>Puntos a lo largo de una curva de Bezier cuadratica.</summary>
        public static List<Vector2> Bezier(Vector2 a, Vector2 c, Vector2 b, int pasos = 24) {
            var r = new List<Vector2>(pasos + 1);
            for (var i = 0; i <= pasos; i++) {
                var t = i / (float)pasos; var u = 1 - t;
                r.Add(u * u * a + 2 * u * t * c + t * t * b);
            }
            return r;
        }

        /// <summary>Un cuarto creciente iluminado por la izquierda: medio disco y un arco interior que se mete hacia la izquierda.</summary>
        public static List<Vector2> Luna(float cx, float cy, float r, float grueso = 0.45f) {
            var p = new List<Vector2>();
            const int n = 24;
            for (var i = 0; i <= n; i++) {
                var a = Mathf.Deg2Rad * Mathf.Lerp(-90, -270, i / (float)n);
                p.Add(new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r));
            }
            for (var i = 1; i < n; i++) {
                var a = Mathf.Deg2Rad * Mathf.Lerp(90, -90, i / (float)n);
                p.Add(new Vector2(cx - Mathf.Cos(a) * r * (1 - grueso), cy + Mathf.Sin(a) * r));
            }
            return p;
        }
    }
}
