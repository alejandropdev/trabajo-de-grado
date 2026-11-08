using System.Collections.Generic;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El edificio de Cradle Lifts en corte (2026-11-07), a la izquierda de la entrevista: el vestibulo abajo con su
    /// cartel, quince pisos con ventanas a los dos lados del hueco, el hueco con sus cables y el contrapeso, y arriba la
    /// azotea con la antena (su luz parpadea) y la bandera de la meta. Sustituye al ascensor pequeño de 2026-11-05.
    ///
    /// Cada respuesta sube la cabina un piso (ease-in-out); al llegar, un destello, las puertas se abren un momento y las
    /// ventanas de ese piso se encienden. Se enciende igual con cualquier respuesta (tambien con «No lo sé»): no dice si
    /// acertaste, y el pre-test no cambia. El piso de la pregunta que toca brilla en cian.
    /// Dibujado en codigo en un lienzo de 300×780 que se escala para caber.
    /// </summary>
    public sealed class EdificioDeLaEntrevista : MonoBehaviour {
        private const float Ancho = 300, Alto = 780;
        private const float X0 = 30, X1 = 270, HuecoX0 = 128, HuecoX1 = 172;
        private const float Techo = 92, AltoDePiso = 40, Suelo = 692, Calle = 762;

        private NexusTheme _tema;
        private int _pisos;
        private RectTransform _lienzo, _cabina, _contrapeso, _cable, _cableContrapeso, _destello, _puertaIzq, _puertaDer;
        private Image _imgDestello, _luzDeAntena;
        private readonly List<Image[]> _ventanas = new List<Image[]>();
        private TMP_Text _rotulo;
        private float _piso, _desde, _objetivo, _tViaje = 1, _tLlegada = 9, _t;
        private Color _encendida, _apagada;

        public static EdificioDeLaEntrevista Crear(UiKit ui, Transform padre, int pisos, int pisoInicial) {
            var hueco = ui.Nodo(padre, "Edificio");
            var le = UiKit.Tamano(hueco, alto: 300, flexAncho: 1, flexAlto: 1);
            le.preferredHeight = Alto * 0.8f;
            var e = hueco.gameObject.AddComponent<EdificioDeLaEntrevista>();
            e.Construir(ui, Mathf.Clamp(pisos, 1, 15), pisoInicial);
            return e;
        }

        private void Construir(UiKit ui, int pisos, int pisoInicial) {
            var tema = _tema = ui.Tema;
            _pisos = pisos;
            _piso = _desde = _objetivo = Mathf.Clamp(pisoInicial, 0, pisos);
            _encendida = NexusTheme.Alfa(VentanaDelAtardecer.Hex(0xF5C46B), 0.9f);
            _apagada = VentanaDelAtardecer.Hex(0x1E2633);

            _lienzo = ui.Nodo(transform, "Lienzo");
            _lienzo.anchorMin = _lienzo.anchorMax = _lienzo.pivot = new Vector2(0.5f, 0.5f);
            _lienzo.sizeDelta = new Vector2(Ancho, Alto);

            // ---- el cuerpo, la azotea y la calle
            var cuerpo = VentanaDelAtardecer.Capa(ui, _lienzo, "Cuerpo");
            cuerpo.Rect(X0, Techo - 10, X1 - X0, Calle - 6 - (Techo - 10), tema.lineStrong, 3, NexusTheme.Alfa(tema.surfaceRaised, 0.92f), false, 6);
            cuerpo.Linea(X0 - 18, Calle - 4, X1 + 18, Calle - 4, tema.lineStrong, 3);
            // Antena y bandera.
            cuerpo.Linea(90, Techo - 10, 90, 30, tema.inkMuted, 3);
            cuerpo.Linea(78, 52, 102, 52, tema.inkMuted, 2);
            cuerpo.Linea(210, Techo - 10, 210, 42, tema.inkMuted, 2);
            cuerpo.Poligono(new List<Vector2> { P(211, 42), P(246, 50), P(211, 60) }, tema.cyan);
            Texto(ui, 150, 60, 140, 18, "META", 12, tema.cyan, TextAlignmentOptions.Center).characterSpacing = 4;
            _luzDeAntena = VentanaDelAtardecer.Imagen(VentanaDelAtardecer.Colocado(ui, _lienzo, "Luz de antena", 84, 22, 12, 12), ui.SpriteCircular(), tema.danger);

            // ---- los pisos: forjado, ventanas a los dos lados y el numero cada cinco
            var xs = new[] { 46f, 86f, 188f, 228f };
            for (var p = 1; p <= _pisos; p++) {
                var y = YDePiso(p);
                cuerpo.Linea(X0, y + AltoDePiso, X1, y + AltoDePiso, tema.line, 2);
                var fila = new Image[4];
                for (var k = 0; k < 4; k++)
                    fila[k] = VentanaDelAtardecer.Imagen(VentanaDelAtardecer.Colocado(ui, _lienzo, "Ventana", xs[k], y + 10, 26, 20), null, _apagada);
                _ventanas.Add(fila);
                if (p % 5 == 0) Texto(ui, 0, y + 10, 26, 20, p.ToString(), 13, tema.inkFaint, TextAlignmentOptions.Right);
            }
            cuerpo.Linea(X0, Techo, X1, Techo, tema.lineStrong, 3);

            // ---- el vestibulo: puertas de cristal y el cartel
            cuerpo.Rect(52, Suelo + 22, 56, 44, tema.inkFaint, 2, NexusTheme.Alfa(tema.cyan, 0.08f), false, 3);
            cuerpo.Linea(80, Suelo + 22, 80, Suelo + 66, tema.inkFaint, 2);
            cuerpo.Rect(190, Suelo + 22, 60, 44, tema.inkFaint, 2, NexusTheme.Alfa(tema.cyan, 0.08f), false, 3);
            Texto(ui, X0, Suelo + 2, X1 - X0, 18, "CRADLE LIFTS · CONCURSO", 11, tema.warning, TextAlignmentOptions.Center).characterSpacing = 2;

            // ---- el hueco del ascensor, los cables y el contrapeso
            cuerpo.Rect(HuecoX0, Techo, HuecoX1 - HuecoX0, Calle - 6 - Techo, tema.line, 2, tema.bg950, false, 2);
            cuerpo.Circulo((HuecoX0 + HuecoX1) / 2, Techo + 8, 6, tema.inkMuted, 2);
            _cable = Varilla(ui, "Cable", tema.inkFaint);
            _cableContrapeso = Varilla(ui, "Cable del contrapeso", tema.inkFaint);
            _contrapeso = VentanaDelAtardecer.Colocado(ui, _lienzo, "Contrapeso", HuecoX1 - 12, Techo + 20, 8, 26);
            VentanaDelAtardecer.Imagen(_contrapeso, null, tema.lineStrong);

            // ---- el destello y la cabina con sus puertas
            _destello = VentanaDelAtardecer.Colocado(ui, _lienzo, "Destello", 0, 0, 120, 120);
            _destello.pivot = new Vector2(0.5f, 0.5f);
            _imgDestello = VentanaDelAtardecer.Imagen(_destello, PlanoIlustrado.Brillo(), NexusTheme.Alfa(tema.cyan, 0));

            _cabina = VentanaDelAtardecer.Colocado(ui, _lienzo, "Cabina", HuecoX0 + 7, 0, HuecoX1 - HuecoX0 - 14, AltoDePiso - 6);
            var fondoCabina = VentanaDelAtardecer.Imagen(_cabina, null, Color.white);
            ui.Fondo(fondoCabina, NexusTheme.Alfa(VentanaDelAtardecer.Hex(0xFFE2A8), 0.55f), NexusTheme.RadioXs);
            ui.Borde(_cabina, tema.cyan, NexusTheme.RadioXs, 2);
            ui.Halo(_cabina, NexusTheme.Alfa(tema.cyan, 0.45f), NexusTheme.RadioXs);
            _puertaIzq = Puerta(ui, 0f);
            _puertaDer = Puerta(ui, 0.5f);

            _rotulo = Texto(ui, 0, 0, Ancho, 20, "", 14, tema.cyan, TextAlignmentOptions.Center);
            _rotulo.characterSpacing = 3;

            Pintar();
        }

        /// <summary>Sube (o baja) hasta 'piso'. Al llegar a un piso nuevo: destello, puertas y ventanas.</summary>
        public void IrA(int piso) {
            piso = Mathf.Clamp(piso, 0, _pisos);
            if (Mathf.Approximately(piso, _objetivo)) return;
            _desde = _piso;
            _objetivo = piso;
            _tViaje = 0;
        }

        private void Update() {
            var dt = Time.unscaledDeltaTime;
            _t += dt;
            var r = ((RectTransform)transform).rect;
            if (r.width > 1 && r.height > 1) {
                var s = Mathf.Min(r.width / Ancho, r.height / Alto, 1.2f);
                _lienzo.localScale = new Vector3(s, s, 1);
            }

            if (_tViaje < 1) {
                // Ease-in-out: arranca y frena como un ascensor de verdad (~0,5 s por piso, entre 0,6 y 1,6 s).
                var duracion = Mathf.Clamp(Mathf.Abs(_objetivo - _desde) * 0.5f, 0.6f, 1.6f);
                _tViaje = Mathf.Min(1, _tViaje + dt / duracion);
                var e = _tViaje < 0.5f ? 4 * _tViaje * _tViaje * _tViaje : 1 - Mathf.Pow(-2 * _tViaje + 2, 3) / 2;
                _piso = Mathf.Lerp(_desde, _objetivo, e);
                if (_tViaje >= 1) { _piso = _objetivo; _tLlegada = 0; }
            }
            _tLlegada += dt;
            Pintar();
        }

        private void Pintar() {
            var tema = _tema;
            var yCabina = YDeCabina(_piso);
            Lamina.Colocar(_cabina, HuecoX0 + 7, yCabina, HuecoX1 - HuecoX0 - 14, AltoDePiso - 6);
            _destello.anchoredPosition = new Vector2((HuecoX0 + HuecoX1) / 2, -(yCabina + AltoDePiso / 2));
            var p = Mathf.Clamp01(_tLlegada / 0.5f);
            _destello.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.8f, 1 - Mathf.Pow(1 - p, 3));
            _imgDestello.color = NexusTheme.Alfa(tema.cyan, 0.75f * (1 - p));

            // Las puertas: abiertas un momento al llegar (se abren en 180 ms, esperan, se cierran en 180 ms).
            var abierta = _tLlegada < 0.18f ? _tLlegada / 0.18f : _tLlegada < 0.9f ? 1 : Mathf.Clamp01(1 - (_tLlegada - 0.9f) / 0.18f);
            abierta = 1 - Mathf.Pow(1 - abierta, 3);
            _puertaIzq.anchorMax = new Vector2(0.5f - 0.45f * abierta, 1);
            _puertaDer.anchorMin = new Vector2(0.5f + 0.45f * abierta, 0);

            // Los cables y el contrapeso (va al reves que la cabina).
            Lamina.Colocar(_cable, (HuecoX0 + HuecoX1) / 2 - 1, Techo + 14, 2, Mathf.Max(0, yCabina - Techo - 14));
            var yContrapeso = Techo + 20 + (YDeCabina(0) - yCabina);
            Lamina.Colocar(_contrapeso, HuecoX1 - 12, yContrapeso, 8, 26);
            Lamina.Colocar(_cableContrapeso, HuecoX1 - 9, Techo + 14, 2, Mathf.Max(0, yContrapeso - Techo - 14));

            // Las ventanas: encendidas las de los pisos ya subidos; en cian, latiendo, las del piso que toca.
            var subidos = Mathf.FloorToInt(_piso + 0.01f);
            var toca = Mathf.RoundToInt(_objetivo) + 1;
            var latido = 0.45f + 0.25f * Mathf.Sin(_t * 3f);
            for (var piso = 1; piso <= _pisos; piso++) {
                Color c;
                if (piso <= subidos) c = _encendida;
                else if (piso == toca && _tViaje >= 1) c = NexusTheme.Alfa(tema.cyan, latido);
                else c = _apagada;
                foreach (var v in _ventanas[piso - 1]) v.color = c;
            }
            _luzDeAntena.color = NexusTheme.Alfa(tema.danger, Mathf.Repeat(_t, 1.4f) < 0.25f ? 1f : 0.2f);

            var actual = Mathf.RoundToInt(_objetivo);
            _rotulo.text = actual >= _pisos ? $"PISO {_pisos} · ARRIBA" : actual == 0 ? "VESTÍBULO" : $"PISO {actual} DE {_pisos}";
        }

        /// <summary>Lo alto (desde arriba del lienzo) del techo del piso p.</summary>
        private static float YDePiso(int p) { return Suelo - p * AltoDePiso; }

        /// <summary>Donde va la cabina: en el vestibulo un poco mas abajo (es mas alto que un piso); luego, piso a piso.</summary>
        private static float YDeCabina(float piso) {
            var enVestibulo = Suelo + 18;
            if (piso <= 1) return Mathf.Lerp(enVestibulo, YDePiso(1) + 3, Mathf.Max(0, piso));
            return YDePiso(1) + 3 - (piso - 1) * AltoDePiso;
        }

        private RectTransform Varilla(UiKit ui, string nombre, Color color) {
            var rt = VentanaDelAtardecer.Colocado(ui, _lienzo, nombre, 0, Techo, 2, 10);
            VentanaDelAtardecer.Imagen(rt, null, color);
            return rt;
        }

        private RectTransform Puerta(UiKit ui, float desde) {
            var rt = ui.Nodo(_cabina, "Puerta");
            rt.anchorMin = new Vector2(desde, 0);
            rt.anchorMax = new Vector2(desde + 0.5f, 1);
            rt.offsetMin = new Vector2(2, 2);
            rt.offsetMax = new Vector2(-2, -2);
            VentanaDelAtardecer.Imagen(rt, null, NexusTheme.Alfa(_tema.inkMuted, 0.85f));
            return rt;
        }

        private TMP_Text Texto(UiKit ui, float x, float y, float w, float h, string texto, float tam, Color color, TextAlignmentOptions alineacion) {
            var t = ui.Texto(_lienzo, texto, EstiloTexto.Pequeno, color, alineacion);
            t.fontSize = tam;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            var alto = t.GetComponent<AltoDeTexto>();
            if (alto != null) Destroy(alto);
            Lamina.Colocar(t.rectTransform, x, y, w, h);
            return t;
        }

        private static Vector2 P(float x, float y) { return new Vector2(x, y); }
    }
}
