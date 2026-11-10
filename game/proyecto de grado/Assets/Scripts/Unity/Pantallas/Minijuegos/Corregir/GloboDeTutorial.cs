using System;
using System.Collections.Generic;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos.Corregir {
    /// <summary>Un paso del tutorial: lo que se dice y la pieza real de la pantalla a la que se señala (null = sin señalar).</summary>
    public sealed class PasoDeTutorial {
        public string Texto;
        public Func<RectTransform> Objetivo;
    }

    /// <summary>
    /// El tutorial de un minijuego como GLOBOS (2026-11-09, ronda 3): en vez de una pantalla de pizarra que tapa el juego,
    /// uno o dos globos que flotan encima, uno por paso, con un aro alrededor de la pieza que explican. No ocupan sitio
    /// en el layout (no le quitan ancho ni alto a nada) y se pueden saltar. Mientras estan abiertos, quien los abrio
    /// puede pausar el reloj (alCerrar avisa).
    /// </summary>
    public sealed class GloboDeTutorial : MonoBehaviour {
        private const float AnchoGlobo = 540, Separacion = 30;

        private UiKit _ui;
        private NexusTheme _tema;
        private RectTransform _capa, _caja, _anillo;
        private Animador _anim;
        private IList<PasoDeTutorial> _pasos;
        private int _i;
        private TMP_Text _progreso, _texto;
        private Button _siguiente;
        private Action _alCerrar;
        private bool _abierto;

        public bool Abierto { get { return _abierto; } }

        public static GloboDeTutorial Abrir(UiKit ui, RectTransform capa, Animador anim, IList<PasoDeTutorial> pasos, Action alCerrar) {
            var g = capa.gameObject.AddComponent<GloboDeTutorial>();
            g._ui = ui; g._tema = ui.Tema; g._capa = capa; g._anim = anim; g._pasos = pasos; g._alCerrar = alCerrar;
            g.Construir();
            return g;
        }

        private void Construir() {
            var t = _tema;
            // El aro, debajo del globo.
            _anillo = _ui.Nodo(_capa, "Aro del tutorial");
            _anillo.anchorMin = _anillo.anchorMax = _anillo.pivot = new Vector2(0.5f, 0.5f);
            var img = _anillo.gameObject.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = false;
            _ui.Borde(_anillo, t.warning, 14, 3);

            _caja = _ui.Columna(_capa, "Globo del tutorial", 8, 18, TextAnchor.UpperLeft);
            _caja.anchorMin = _caja.anchorMax = _caja.pivot = new Vector2(0.5f, 0.5f);
            _caja.sizeDelta = new Vector2(AnchoGlobo, 10);
            var fondo = _caja.gameObject.AddComponent<Image>();
            _ui.Fondo(fondo, NexusTheme.Alfa(t.bg950, 0.97f), NexusTheme.RadioLg);
            _ui.Halo(_caja, NexusTheme.Alfa(t.warning, 0.3f), NexusTheme.RadioLg);
            _ui.Borde(_caja, NexusTheme.Alfa(t.warning, 0.9f), NexusTheme.RadioLg, 2);
            var ajuste = _caja.gameObject.AddComponent<ContentSizeFitter>();
            ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ajuste.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            _progreso = _ui.Texto(_caja, "", EstiloTexto.Leyenda, t.warning);
            _texto = _ui.Texto(_caja, "", EstiloTexto.Cuerpo, t.ink);
            _texto.fontSize = 21;
            var fila = _ui.Fila(_caja, "Botones", 10, 0, TextAnchor.MiddleRight);
            _ui.Boton(fila, "Saltar", Cerrar, VarianteBoton.Fantasma);
            UiKit.Tamano(_ui.Nodo(fila, "Resorte"), flexAncho: 1);
            _siguiente = _ui.Boton(fila, "Siguiente  ›", Siguiente, VarianteBoton.Primario);
            _abierto = true;
            Pintar();
        }

        private void Pintar() {
            if (_i >= _pasos.Count) { Cerrar(); return; }
            var ultimo = _i == _pasos.Count - 1;
            _progreso.text = $"TUTORIAL · {_i + 1} DE {_pasos.Count}";
            _texto.text = Nexus.Core.Narrativa.Glifos.Seguro(_pasos[_i].Texto ?? "");
            _siguiente.GetComponentInChildren<TMP_Text>().text = ultimo ? "¡Entendido!" : "Siguiente  ›";
            // Un saltito al cambiar de paso.
            var caja = _caja;
            _anim.Lanzar(0.28f, p => { if (caja != null) caja.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, Curvas.SalidaAtras(p)); }, null, 0, caja.gameObject);
        }

        private void Siguiente() { _i++; Pintar(); }

        public void Cerrar() {
            if (!_abierto) return;
            _abierto = false;
            if (_caja != null) Destroy(_caja.gameObject);
            if (_anillo != null) Destroy(_anillo.gameObject);
            _alCerrar?.Invoke();
            Destroy(this);
        }

        private void Update() {
            var teclado = UnityEngine.InputSystem.Keyboard.current;
            if (_abierto && teclado != null && (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame)) Siguiente();
        }

        /// <summary>El globo y el aro siguen a la pieza señalada (que se mueve con el zoom, o aparece despues).</summary>
        private void LateUpdate() {
            if (!_abierto || _caja == null || _i >= _pasos.Count) return;
            var area = _capa.rect;
            var alto = Mathf.Max(80f, LayoutUtility.GetPreferredHeight(_caja));
            var objetivo = _pasos[_i].Objetivo != null ? _pasos[_i].Objetivo() : null;
            if (objetivo == null || !objetivo.gameObject.activeInHierarchy) {
                _anillo.gameObject.SetActive(false);
                _caja.anchoredPosition = new Vector2(0, area.yMax - alto / 2 - 150);
                return;
            }
            var esquinas = new Vector3[4];
            objetivo.GetWorldCorners(esquinas);
            var a = (Vector2)_capa.InverseTransformPoint(esquinas[0]);
            var b = (Vector2)_capa.InverseTransformPoint(esquinas[2]);
            var r = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            _anillo.gameObject.SetActive(true);
            var pulso = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 6f);
            _anillo.anchoredPosition = r.center;
            _anillo.sizeDelta = (r.size + new Vector2(24, 24)) * pulso;

            // Debajo de la pieza si esta en la mitad de arriba; encima si esta en la de abajo.
            var abajo = r.center.y > area.center.y;
            var y = abajo ? r.yMin - Separacion - alto / 2 : r.yMax + Separacion + alto / 2;
            var x = Mathf.Clamp(r.center.x, area.xMin + AnchoGlobo / 2 + 12, area.xMax - AnchoGlobo / 2 - 12);
            y = Mathf.Clamp(y, area.yMin + alto / 2 + 12, area.yMax - alto / 2 - 12);
            _caja.anchoredPosition = new Vector2(x, y);
        }
    }
}
