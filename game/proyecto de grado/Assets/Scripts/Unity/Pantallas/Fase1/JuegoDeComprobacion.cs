using System;
using System.Collections.Generic;
using Nexus.Core.Proyecto;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// «Comprueba que lo entendiste» como un JUEGO (2026-11-08): la pregunta es una ficha que se ARRASTRA hasta su
    /// respuesta (o se pulsa la respuesta, que hace lo mismo). Si acierta, la ficha vuela a su casilla y se estampa
    /// «¡CORRECTO!»; si no, la casilla se sacude y se apaga, la ficha vuelve y sale la pista. Mismas preguntas y mismo
    /// registro que el mini-quiz de cartas; la pantalla decide si la respuesta vale (contestar) y que viene despues.
    /// </summary>
    internal sealed class JuegoDeComprobacion : MonoBehaviour {
        private UiKit _ui;
        private NexusTheme _t;
        private RectTransform _ficha, _sello;
        private CanvasGroup _grupoFicha;
        private TMP_Text _pista;
        private readonly List<RectTransform> _casillas = new List<RectTransform>();
        private readonly List<Image> _fondos = new List<Image>(), _bordes = new List<Image>();
        private readonly List<CanvasGroup> _grupos = new List<CanvasGroup>();
        private readonly List<bool> _falladas = new List<bool>();
        private readonly List<float> _sacudida = new List<float>();
        private Func<int, bool> _contestar;
        private Action _alAcertar;
        private string _textoDePista;
        private bool _arrastrando, _resuelta, _volviendo;
        private int _encima = -1, _acierto = -1;
        private float _tAcierto = -1;
        private Vector3 _posInicial;

        public static JuegoDeComprobacion Crear(UiKit ui, RectTransform padre, PreguntaDelProyecto p, Func<int, bool> contestar,
                                                Func<int, bool> yaFallada, Action alAcertar) {
            var raiz = ui.Nodo(padre, "Juego");
            UiKit.Rellenar(raiz);
            raiz.gameObject.AddComponent<ListaDeElementos>();   // las casillas son contenido, no botones de la pantalla
            var j = raiz.gameObject.AddComponent<JuegoDeComprobacion>();
            j.Construir(ui, raiz, p, contestar, yaFallada, alAcertar);
            return j;
        }

        private void Construir(UiKit ui, RectTransform raiz, PreguntaDelProyecto p, Func<int, bool> contestar, Func<int, bool> yaFallada, Action alAcertar) {
            _ui = ui; _t = ui.Tema; _contestar = contestar; _alAcertar = alAcertar; _textoDePista = p.Explicacion;

            // ---- las casillas de respuesta (a la derecha)
            const float alto = 0.27f, hueco = 0.035f;
            for (var j = 0; j < p.Opciones.Count; j++) {
                var yMax = 1 - j * (alto + hueco);
                var c = ui.Nodo(raiz, "Casilla " + (j + 1));
                c.anchorMin = new Vector2(0.40f, yMax - alto); c.anchorMax = new Vector2(1, yMax);
                c.offsetMin = c.offsetMax = Vector2.zero;
                var fondo = c.gameObject.AddComponent<Image>();
                ui.Fondo(fondo, NexusTheme.Alfa(_t.bg950, 0.7f), NexusTheme.RadioLg);
                var borde = ui.Borde(c, _t.lineStrong, NexusTheme.RadioLg, 1.5f);
                var g = c.gameObject.AddComponent<CanvasGroup>();
                // la letra de la casilla
                var letra = ui.Nodo(c, "Letra");
                letra.anchorMin = letra.anchorMax = new Vector2(0, 0.5f);
                letra.pivot = new Vector2(0, 0.5f);
                letra.sizeDelta = new Vector2(_t.Px(36), _t.Px(36));
                letra.anchoredPosition = new Vector2(_t.Px(12), 0);
                var li = letra.gameObject.AddComponent<Image>();
                ui.Fondo(li, NexusTheme.Alfa(_t.cyan, 0.15f), _t.Px(18) / _t.escala);
                li.raycastTarget = false;
                TextoEnCaja.Crear(ui, letra, ((char)('A' + j)).ToString(), EstiloTexto.Encabezado, _t.cyan, TextAlignmentOptions.Center, _t.Px(20),
                                  Vector2.zero, Vector2.one).fontStyle = FontStyles.Bold;
                var texto = TextoEnCaja.Crear(ui, c, p.Opciones[j], EstiloTexto.Cuerpo, _t.ink, TextAlignmentOptions.MidlineLeft, _t.Px(17),
                                              Vector2.zero, Vector2.one);
                texto.rectTransform.offsetMin = new Vector2(_t.Px(58), _t.Px(6));
                texto.rectTransform.offsetMax = new Vector2(-_t.Px(12), -_t.Px(6));
                var indice = j;
                var boton = c.gameObject.AddComponent<Button>();
                boton.targetGraphic = fondo;
                boton.transition = Selectable.Transition.None;
                boton.navigation = new Navigation { mode = Navigation.Mode.None };
                boton.onClick.AddListener(() => Responder(indice));
                _casillas.Add(c); _fondos.Add(fondo); _bordes.Add(borde); _grupos.Add(g);
                var fallada = yaFallada != null && yaFallada(j);
                _falladas.Add(fallada); _sacudida.Add(9);
                if (fallada) g.alpha = 0.4f;
            }

            // ---- la ficha: la pregunta, para arrastrar
            _ficha = ui.Nodo(raiz, "Ficha");
            _ficha.anchorMin = _ficha.anchorMax = new Vector2(0.19f, 0.54f);
            _ficha.pivot = new Vector2(0.5f, 0.5f);
            _ficha.sizeDelta = new Vector2(_t.Px(330), _t.Px(210));
            var ff = _ficha.gameObject.AddComponent<Image>();
            ui.Fondo(ff, NexusTheme.Alfa(_t.bg950, 0.92f), NexusTheme.RadioLg);
            ui.Halo(_ficha, NexusTheme.Alfa(_t.cyan, 0.3f), NexusTheme.RadioLg);
            ui.Borde(_ficha, _t.cyan, NexusTheme.RadioLg, 2);
            _grupoFicha = _ficha.gameObject.AddComponent<CanvasGroup>();
            TextoEnCaja.Crear(ui, _ficha, "ARRÁSTRAME", EstiloTexto.Leyenda, _t.cyan, TextAlignmentOptions.Center, _t.Px(12),
                              new Vector2(0, 0.84f), Vector2.one, 4);
            TextoEnCaja.Crear(ui, _ficha, p.Texto, EstiloTexto.Encabezado, _t.ink, TextAlignmentOptions.Center, _t.Px(22),
                              new Vector2(0, 0), new Vector2(1, 0.84f), _t.Px(16)).fontStyle = FontStyles.Bold;
            var arrastre = _ficha.gameObject.AddComponent<FichaDelJuego>();
            arrastre.Juego = this;
            _posInicial = _ficha.anchoredPosition;

            // ---- el sello de acierto (oculto hasta que haga falta)
            _sello = ui.Nodo(raiz, "Sello");
            _sello.sizeDelta = new Vector2(_t.Px(190), _t.Px(40));
            var sf = _sello.gameObject.AddComponent<Image>();
            ui.Fondo(sf, NexusTheme.Alfa(_t.bg950, 0.95f), 6);
            sf.raycastTarget = false;
            ui.Borde(_sello, _t.success, 6, 3);
            TextoEnCaja.Crear(ui, _sello, "¡CORRECTO!", EstiloTexto.Subtitulo, _t.success, TextAlignmentOptions.Center, _t.Px(22),
                              Vector2.zero, Vector2.one, 4).fontStyle = FontStyles.Bold;
            _sello.localEulerAngles = new Vector3(0, 0, 8);
            _sello.localScale = Vector3.zero;

            // ---- la pista, debajo
            _pista = TextoEnCaja.Crear(ui, raiz, "Arrastra la ficha hasta su respuesta (o pulsa la respuesta).", EstiloTexto.Pequeno, _t.inkMuted,
                                       TextAlignmentOptions.MidlineLeft, _t.Px(15), new Vector2(0, -0.01f), new Vector2(0.62f, 0.10f));
        }

        // ==================================================================== arrastrar

        internal void Empezar() {
            if (_resuelta) return;
            _arrastrando = true; _volviendo = false;
            _ficha.SetAsLastSibling();
        }

        internal void Arrastrar(PointerEventData e) {
            if (_resuelta || !_arrastrando) return;
            Vector3 mundo;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)_ficha.parent, e.position, e.pressEventCamera, out mundo))
                _ficha.position = mundo;
            _encima = -1;
            for (var j = 0; j < _casillas.Count; j++)
                if (RectTransformUtility.RectangleContainsScreenPoint(_casillas[j], e.position, e.pressEventCamera)) { _encima = j; break; }
        }

        internal void Soltar(PointerEventData e) {
            _arrastrando = false;
            var j = _encima;
            _encima = -1;
            if (_resuelta) return;
            if (j >= 0 && !_falladas[j]) Responder(j);
            else _volviendo = true;
        }

        private void Responder(int j) {
            if (_resuelta || _falladas[j]) return;
            if (_contestar(j)) {
                _resuelta = true; _acierto = j; _tAcierto = 0; _volviendo = false;
                _posInicial = _ficha.position;
                _pista.text = "";
            } else {
                _falladas[j] = true; _sacudida[j] = 0; _volviendo = true;
                _pista.text = Nexus.Core.Narrativa.Glifos.Seguro("No es esa. Pista: " + _textoDePista);
                _pista.color = _t.warning;
            }
        }

        // ==================================================================== animacion

        private void Update() {
            var dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            for (var j = 0; j < _casillas.Count; j++) {
                _sacudida[j] += dt;
                var sacude = _sacudida[j] < 0.4f ? Mathf.Sin(_sacudida[j] * 55f) * 12f * (1 - _sacudida[j] / 0.4f) : 0f;
                _casillas[j].anchoredPosition = new Vector2(sacude, 0);
                var objetivo = j == _acierto ? 0.3f : (_arrastrando && j == _encima && !_falladas[j]) ? 0.25f : 0f;
                var acento = j == _acierto ? _t.success : _falladas[j] ? _t.danger : _t.cyan;
                _fondos[j].color = Color.Lerp(_fondos[j].color, NexusTheme.Alfa(objetivo > 0 ? acento : _t.bg950, objetivo > 0 ? objetivo : 0.7f), dt * 14f);
                _bordes[j].color = j == _acierto ? _t.success : _falladas[j] ? NexusTheme.Alfa(_t.danger, 0.8f) : (_arrastrando && j == _encima ? _t.cyan : _t.lineStrong);
                _grupos[j].alpha = _falladas[j] ? 0.4f : (_resuelta && j != _acierto ? 0.45f : 1f);
            }
            if (_volviendo) {
                _ficha.anchoredPosition = Vector2.Lerp(_ficha.anchoredPosition, Vector2.zero, dt * 14f);
                if (_ficha.anchoredPosition.sqrMagnitude < 1f) { _ficha.anchoredPosition = Vector2.zero; _volviendo = false; }
            }
            if (_tAcierto >= 0) {
                _tAcierto += dt;
                var x = Mathf.Clamp01(_tAcierto / 0.4f);
                var meta = _casillas[_acierto].TransformPoint(_casillas[_acierto].rect.center);
                _ficha.position = Vector3.Lerp(_posInicial, meta, 1 - Mathf.Pow(1 - x, 3));
                _ficha.localScale = Vector3.one * Mathf.Lerp(1f, 0.45f, x);
                _grupoFicha.alpha = 1 - Mathf.Clamp01((_tAcierto - 0.15f) / 0.3f);
                var s = ActoresDeEscena.Rebote(Mathf.Clamp01((_tAcierto - 0.3f) / 0.4f));
                _sello.position = _casillas[_acierto].TransformPoint(new Vector3(_casillas[_acierto].rect.xMax - _t.Px(120), 0, 0));
                _sello.localScale = new Vector3(s, s, 1);
                if (_tAcierto > 1.15f) { _tAcierto = -1; if (_alAcertar != null) _alAcertar(); }
            }
        }

        /// <summary>La ficha de la pregunta: avisa al juego de cuando se arrastra y se suelta.</summary>
        private sealed class FichaDelJuego : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
            public JuegoDeComprobacion Juego;
            public void OnBeginDrag(PointerEventData e) { Juego.Empezar(); }
            public void OnDrag(PointerEventData e) { Juego.Arrastrar(e); }
            public void OnEndDrag(PointerEventData e) { Juego.Soltar(e); }
        }
    }
}
