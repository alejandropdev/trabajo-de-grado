using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El progreso de la entrevista de admision como un ASCENSOR (2026-11-05): el concurso es el del ascensor de
    /// Cradle Lifts, y cada respuesta sube la cabina un piso, con un destello al llegar. Del vestibulo al piso 15 se ve
    /// la meta entera; antes era un «Pregunta n de 15» en texto.
    ///
    /// Es solo presentacion: sube igual con cualquier respuesta (tambien con «No lo sé»), sin decir si acertaste. El
    /// pre-test no cambia (mismas preguntas, mismo orden, misma correccion).
    /// </summary>
    public sealed class AscensorDeLaEntrevista : MonoBehaviour {
        private const float AltoDePiso = 26f, AnchoDelHueco = 120f;

        private UiKit _ui;
        private int _pisos;
        private RectTransform _cabina, _destello;
        private Image _imgDestello;
        private readonly Image[] _marcas = new Image[32];
        private TMP_Text _rotulo;
        private float _piso, _objetivo, _tDestello = 1f;

        public static AscensorDeLaEntrevista Crear(UiKit ui, Transform padre, int pisos, int pisoInicial) {
            var tema = ui.Tema;
            var columna = ui.Columna(padre, "Ascensor", tema.Espacio(2), 0, TextAnchor.UpperCenter);
            var a = columna.gameObject.AddComponent<AscensorDeLaEntrevista>();
            a._ui = ui;
            a._pisos = Mathf.Min(pisos, a._marcas.Length - 1);
            a._piso = a._objetivo = Mathf.Clamp(pisoInicial, 0, a._pisos);

            a._rotulo = ui.Texto(columna, "", EstiloTexto.Leyenda, tema.cyan, TextAlignmentOptions.Center);

            // El hueco: una caja alta con una marca por piso y la cabina, que sube.
            var hueco = ui.Nodo(columna, "Hueco");
            var alto = (a._pisos + 1) * AltoDePiso + 8;
            UiKit.Tamano(hueco, AnchoDelHueco, alto);
            var fondo = hueco.gameObject.AddComponent<Image>();
            ui.Fondo(fondo, tema.bg950, NexusTheme.RadioSm);
            fondo.raycastTarget = false;
            ui.Borde(hueco, tema.lineStrong, NexusTheme.RadioSm, 1);

            for (var p = 0; p <= a._pisos; p++) {
                var marca = ui.Nodo(hueco, "Piso " + p);
                marca.anchorMin = marca.anchorMax = new Vector2(0, 0);
                marca.pivot = new Vector2(0, 0.5f);
                marca.anchoredPosition = new Vector2(8, 4 + p * AltoDePiso + AltoDePiso / 2);
                marca.sizeDelta = new Vector2(AnchoDelHueco - 16, 2);
                var img = marca.gameObject.AddComponent<Image>();
                img.color = tema.line;
                img.raycastTarget = false;
                a._marcas[p] = img;
                if (p > 0 && (p % 5 == 0 || p == a._pisos)) {
                    var n = ui.Texto(marca, p.ToString(), EstiloTexto.Leyenda, tema.inkFaint, TextAlignmentOptions.Right);
                    var alto2 = n.GetComponent<AltoDeTexto>();
                    if (alto2 != null) Destroy(alto2);
                    var rn = n.rectTransform;
                    rn.anchorMin = rn.anchorMax = new Vector2(1, 0.5f);
                    rn.pivot = new Vector2(1, 0);
                    rn.sizeDelta = new Vector2(30, 18);
                    rn.anchoredPosition = new Vector2(0, 2);
                }
            }

            a._destello = ui.Nodo(hueco, "Destello");
            a._destello.anchorMin = a._destello.anchorMax = new Vector2(0.5f, 0);
            a._destello.sizeDelta = new Vector2(90, 90);
            a._imgDestello = a._destello.gameObject.AddComponent<Image>();
            a._imgDestello.sprite = PlanoIlustrado.Brillo();
            a._imgDestello.color = new Color(1, 1, 1, 0);
            a._imgDestello.raycastTarget = false;

            a._cabina = ui.Nodo(hueco, "Cabina");
            a._cabina.anchorMin = a._cabina.anchorMax = new Vector2(0.5f, 0);
            a._cabina.sizeDelta = new Vector2(46, AltoDePiso - 4);
            var imgCabina = a._cabina.gameObject.AddComponent<Image>();
            ui.Fondo(imgCabina, tema.surfaceRaised, 3);
            imgCabina.raycastTarget = false;
            ui.Borde(a._cabina, tema.cyan, 3, 2);
            ui.Halo(a._cabina, NexusTheme.Alfa(tema.cyan, 0.5f), 3);

            a.Pintar();
            return a;
        }

        /// <summary>Sube (o se queda) hasta 'piso'. Con un piso nuevo, al llegar, un destello: «ding».</summary>
        public void IrA(int piso) {
            piso = Mathf.Clamp(piso, 0, _pisos);
            if (Mathf.Approximately(piso, _objetivo)) return;
            _objetivo = piso;
            _tDestello = -1;   // se enciende al llegar
            Pintar();
        }

        private void Update() {
            var dt = Time.unscaledDeltaTime;
            if (Mathf.Abs(_piso - _objetivo) > 0.001f) {
                _piso = Mathf.MoveTowards(_piso, _objetivo, dt * 2.2f);
                if (Mathf.Abs(_piso - _objetivo) <= 0.001f) { _piso = _objetivo; _tDestello = 0; }
                Pintar();
            }
            if (_tDestello >= 0 && _tDestello < 1) {
                _tDestello += dt / 0.5f;
                var f = Mathf.Clamp01(_tDestello);
                _destello.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.8f, f);
                _imgDestello.color = NexusTheme.Alfa(_ui.Tema.cyan, 0.8f * (1 - f));
            }
        }

        private void Pintar() {
            var tema = _ui.Tema;
            var y = 4 + _piso * AltoDePiso + AltoDePiso / 2;
            _cabina.anchoredPosition = new Vector2(0, y);
            _destello.anchoredPosition = new Vector2(0, y);
            for (var p = 0; p <= _pisos; p++)
                if (_marcas[p] != null) _marcas[p].color = p > 0 && p <= _piso + 0.01f ? NexusTheme.Alfa(tema.cyan, 0.8f) : tema.line;
            var actual = Mathf.RoundToInt(_objetivo);
            _rotulo.text = actual >= _pisos ? $"PISO {_pisos} · ARRIBA" : actual == 0 ? "VESTÍBULO" : $"PISO {actual} DE {_pisos}";
        }
    }
}
