using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// Un grafico de series en el tiempo, sobre DibujoUI: ejes con su rejilla, lineas (continuas o discontinuas),
    /// areas apiladas, una marca vertical («hoy», «fin del sprint») y la leyenda. Es lo que dibuja la evolucion del
    /// proyecto y, por metodologia, el burndown, el flujo acumulado y la curva S.
    ///
    /// El eje X son dias (u otra cuenta entera) de 'minX' a 'maxX'; una serie es una lista de valores, uno por
    /// paso, empezando en 'primerX'. Las series pueden ser mas cortas que el eje: se dibuja hasta donde llegan.
    /// </summary>
    public sealed class GraficoDeSeries {
        private readonly UiKit _ui;
        private readonly Lamina _lamina;
        private readonly float _ancho, _alto;
        private const float Izq = 64, Der = 20, Arriba = 16, Abajo = 44;
        private int _minX, _maxX = 1;
        private double _maxY = 1;

        public RectTransform Raiz { get { return _lamina.Raiz; } }

        public GraficoDeSeries(UiKit ui, Transform padre, float ancho, float alto) {
            _ui = ui;
            _ancho = ancho;
            _alto = alto;
            _lamina = new Lamina(ui, padre, ancho, alto, "Grafico");
        }

        private NexusTheme Tema { get { return _ui.Tema; } }

        private float X(double x) { return Izq + (float)((x - _minX) / System.Math.Max(1, _maxX - _minX)) * (_ancho - Izq - Der); }
        private float Y(double y) { return _alto - Abajo - (float)(System.Math.Max(0, y) / _maxY) * (_alto - Arriba - Abajo); }

        /// <summary>
        /// De cuanto en cuanto van las marcas del eje Y: de 5 en 5 con pocos puntos, de 10 en 10 con mas (y de 20 o de
        /// 50 si hiciera falta). Numeros redondos: una rejilla en 8,25 · 16,5 · 24,75 no se lee.
        /// </summary>
        public static double PasoDelEjeY(double maxY) {
            if (maxY <= 5) return 1;
            if (maxY <= 30) return 5;
            if (maxY <= 60) return 10;
            if (maxY <= 120) return 20;
            return 50;
        }

        /// <summary>
        /// Fija la escala y dibuja los ejes. Se llama antes que las series. El techo del eje Y se redondea hacia arriba
        /// al siguiente multiplo del paso, para que la ultima marca tambien sea un numero redondo.
        /// </summary>
        public void Ejes(string ejeX, string ejeY, int minX, int maxX, double maxY) {
            _minX = minX;
            _maxX = Mathf.Max(minX + 1, maxX);
            var paso = PasoDelEjeY(maxY <= 0 ? 1 : maxY);
            _maxY = System.Math.Max(paso, System.Math.Ceiling((maxY <= 0 ? 1 : maxY) / paso - 1e-9) * paso);

            // La rejilla horizontal, con su valor a la izquierda.
            for (var v = 0.0; v <= _maxY + 1e-9; v += paso) {
                var y = Y(v);
                _lamina.Dibujo.Linea(Izq, y, _ancho - Der, y, v == 0 ? Tema.lineStrong : Tema.line, v == 0 ? 2 : 1);
                _lamina.Texto(0, y - 11, Izq - 8, 22, v.ToString("0"), 14, Tema.inkFaint, TextAlignmentOptions.MidlineRight);
            }
            _lamina.Dibujo.Linea(Izq, Arriba, Izq, _alto - Abajo, Tema.lineStrong, 2);

            // Las marcas del eje X: todas si caben, o una de cada tantas.
            var pasos = _maxX - _minX;
            var cada = Mathf.Max(1, Mathf.CeilToInt(pasos / 10f));
            for (var x = _minX; x <= _maxX; x += cada) {
                _lamina.Dibujo.Linea(X(x), _alto - Abajo, X(x), _alto - Abajo + 5, Tema.lineStrong, 2);
                _lamina.Texto(X(x) - 30, _alto - Abajo + 6, 60, 18, x.ToString(), 14, Tema.inkFaint, TextAlignmentOptions.Center);
            }
            if (!string.IsNullOrEmpty(ejeX))
                _lamina.Texto(Izq, _alto - 20, _ancho - Izq - Der, 18, ejeX, 13, Tema.inkFaint, TextAlignmentOptions.MidlineRight);
            if (!string.IsNullOrEmpty(ejeY))
                _lamina.Texto(Izq + 8, Arriba - 4, 400, 18, ejeY, 13, Tema.inkFaint);
        }

        /// <summary>Una serie como linea. 'primerX' es el valor de X de su primer punto (por defecto, el minimo del eje).</summary>
        public void Linea(IList<double> ys, Color color, bool discontinua = false, float grosor = 3, int? primerX = null) {
            if (ys == null || ys.Count == 0) return;
            var x0 = primerX ?? _minX;
            var puntos = new List<Vector2>();
            for (var i = 0; i < ys.Count; i++) puntos.Add(new Vector2(X(x0 + i), Y(ys[i])));
            if (puntos.Count == 1) { _lamina.Dibujo.Circulo(puntos[0].x, puntos[0].y, grosor * 1.5f, color, 0, color); return; }
            _lamina.Dibujo.Polilinea(puntos, color, grosor, discontinua);
            var ultimo = puntos[puntos.Count - 1];
            _lamina.Dibujo.Circulo(ultimo.x, ultimo.y, grosor * 1.5f, color, 0, color);
        }

        /// <summary>
        /// Capas apiladas de abajo arriba (la primera, pegada al eje): cada una ocupa la banda entre lo acumulado
        /// hasta ella y lo acumulado con ella. Se rellena tramo a tramo, con un cuadrilatero por cada dos puntos.
        /// </summary>
        public void AreasApiladas(IList<IList<double>> capas, IList<Color> colores, int? primerX = null) {
            if (capas == null || capas.Count == 0) return;
            var x0 = primerX ?? _minX;
            var n = int.MaxValue;
            foreach (var c in capas) n = Mathf.Min(n, c.Count);
            if (n < 2 || n == int.MaxValue) return;

            var debajo = new double[n];
            for (var k = 0; k < capas.Count; k++) {
                var color = colores[k % colores.Count];
                var encima = new double[n];
                for (var i = 0; i < n; i++) encima[i] = debajo[i] + System.Math.Max(0, capas[k][i]);
                for (var i = 0; i < n - 1; i++) {
                    _lamina.Dibujo.Poligono(new List<Vector2> {
                        new Vector2(X(x0 + i), Y(debajo[i])), new Vector2(X(x0 + i), Y(encima[i])),
                        new Vector2(X(x0 + i + 1), Y(encima[i + 1])), new Vector2(X(x0 + i + 1), Y(debajo[i + 1]))
                    }, NexusTheme.Alfa(color, 0.55f));
                }
                var borde = new List<Vector2>();
                for (var i = 0; i < n; i++) borde.Add(new Vector2(X(x0 + i), Y(encima[i])));
                _lamina.Dibujo.Polilinea(borde, color, 2);
                debajo = encima;
            }
        }

        /// <summary>Una linea vertical con su rotulo: «hoy», el final de un sprint, una compuerta.</summary>
        public void MarcaVertical(int x, string texto, Color color) {
            if (x < _minX || x > _maxX) return;
            _lamina.Dibujo.Linea(X(x), Arriba, X(x), _alto - Abajo, color, 2, true);
            if (!string.IsNullOrEmpty(texto))
                _lamina.Texto(X(x) + 6, Arriba, 200, 18, texto, 13, color);
        }

        /// <summary>La leyenda, en una fila aparte debajo del grafico (no encima: taparia las series).</summary>
        public static void Leyenda(UiKit ui, Transform padre, IList<KeyValuePair<string, Color>> series) {
            var fila = ui.Fila(padre, "Leyenda", ui.Tema.Espacio(4));
            foreach (var s in series) {
                var item = ui.Fila(fila, "Serie", ui.Tema.Espacio(2));
                ui.Punto(item, s.Value, 10);
                ui.Texto(item, s.Key, EstiloTexto.Pequeno, ui.Tema.ink);
            }
        }
    }
}
