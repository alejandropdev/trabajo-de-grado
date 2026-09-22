using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Tema {
    /// <summary>Una barra de 0 a 1. La crea UiKit.Barra; las pantallas solo le cambian el valor y el color.</summary>
    public sealed class BarraView : MonoBehaviour {
        private RectTransform _relleno;
        private Image _imagen;
        private float _valor;

        internal void Configurar(RectTransform relleno, Image imagen) {
            _relleno = relleno;
            _imagen = imagen;
        }

        public float Valor {
            get { return _valor; }
            set {
                _valor = Mathf.Clamp01(value);
                if (_relleno != null) _relleno.anchorMax = new Vector2(_valor, 1);
            }
        }

        public Color Color {
            get { return _imagen != null ? _imagen.color : Color.white; }
            set { if (_imagen != null) _imagen.color = value; }
        }
    }

    /// <summary>Un indicador circular de 0 a 1 con el valor en el centro.</summary>
    public sealed class DialView : MonoBehaviour {
        private Image _arco;
        private TMP_Text _texto;
        private float _valor;

        /// <summary>Como se escribe el valor en el centro. Por defecto, porcentaje.</summary>
        public System.Func<float, string> Formato = v => Mathf.RoundToInt(v * 100) + "%";

        internal void Configurar(Image arco, TMP_Text texto) {
            _arco = arco;
            _texto = texto;
        }

        public float Valor {
            get { return _valor; }
            set {
                _valor = Mathf.Clamp01(value);
                if (_arco != null) _arco.fillAmount = _valor;
                if (_texto != null) _texto.text = Formato(_valor);
            }
        }

        public Color Color {
            get { return _arco != null ? _arco.color : Color.white; }
            set { if (_arco != null) _arco.color = value; }
        }
    }

    /// <summary>
    /// Un grafico de radar dibujado con la malla de uGUI (sin texturas): anillos de referencia, un eje por
    /// valor y el poligono de los valores. Es el radar de competencias del Dashboard de Lecciones.
    ///
    /// ★ RequireComponent(CanvasRenderer) no es decorativo. Image y TMP lo traen de serie; un Graphic propio no.
    /// Sin el, en el editor GetComponent devuelve un «falso null» que Graphic cachea como si fuera un
    /// CanvasRenderer, y al destruir la pantalla (RectMask2D.OnDisable) salta MissingComponentException y corta
    /// el flujo a medias: la pantalla vacia al cerrar el N0.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GraficoRadar : MaskableGraphic {
        public Color ColorRejilla = Color.gray;
        public Color ColorValor = new Color(0.37f, 0.85f, 0.96f, 0.45f);
        public Color ColorBorde = new Color(0.37f, 0.85f, 0.96f, 1f);
        public int Anillos = 4;
        public float GrosorLinea = 2f;

        private float[] _valores = new float[0];

        public float[] Valores {
            get { return _valores; }
            set {
                _valores = value ?? new float[0];
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
            var n = _valores.Length;
            if (n < 3) return;   // con menos de tres ejes no hay poligono

            var r = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
            var centro = rectTransform.rect.center;

            for (var a = 1; a <= Anillos; a++) {
                var radio = r * a / Anillos;
                for (var i = 0; i < n; i++)
                    Linea(vh, Punto(centro, radio, i, n), Punto(centro, radio, (i + 1) % n, n), ColorRejilla);
            }
            for (var i = 0; i < n; i++) Linea(vh, centro, Punto(centro, r, i, n), ColorRejilla);

            // El poligono de los valores: un abanico desde el centro.
            var inicio = vh.currentVertCount;
            vh.AddVert(centro, ColorValor, Vector2.zero);
            for (var i = 0; i < n; i++)
                vh.AddVert(Punto(centro, r * Mathf.Clamp01(_valores[i]), i, n), ColorValor, Vector2.zero);
            for (var i = 0; i < n; i++)
                vh.AddTriangle(inicio, inicio + 1 + i, inicio + 1 + (i + 1) % n);

            for (var i = 0; i < n; i++)
                Linea(vh, Punto(centro, r * Mathf.Clamp01(_valores[i]), i, n),
                      Punto(centro, r * Mathf.Clamp01(_valores[(i + 1) % n]), (i + 1) % n, n), ColorBorde);
        }

        private static Vector2 Punto(Vector2 centro, float radio, int i, int n) {
            var angulo = Mathf.PI / 2 - i * 2 * Mathf.PI / n;   // el primer eje apunta hacia arriba
            return centro + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * radio;
        }

        private void Linea(VertexHelper vh, Vector2 a, Vector2 b, Color c) {
            var normal = new Vector2(-(b - a).y, (b - a).x).normalized * (GrosorLinea * 0.5f);
            var i = vh.currentVertCount;
            vh.AddVert(a - normal, c, Vector2.zero);
            vh.AddVert(a + normal, c, Vector2.zero);
            vh.AddVert(b + normal, c, Vector2.zero);
            vh.AddVert(b - normal, c, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
