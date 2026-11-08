using System;
using Nexus.Core.Metodologia;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// La carta de una metodologia (2026-11-08): arriba su imagen de tiza (el ciclo, la escalera, el tablero), abajo el
    /// nombre. Dos modos:
    ///   · Para ELEGIR (parte 1 de la Metodologia): al pasar el raton sube y brilla; al pulsar, se estampa «ELEGIDA».
    ///   · VOLTEABLE (la elegida, en la parte del «¿Por qué?»): al pasar el raton se da la vuelta y enseña
    ///     «✕ Cambiar metodología»; al pulsar el reverso, se deselecciona.
    /// </summary>
    public sealed class CartaDeMetodologia : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
        private const float AnchoLamina = 600, AltoLamina = 300;

        private NexusTheme _t;
        private RectTransform _cuerpo, _anverso, _reverso, _hueco, _sello;
        private Lamina _lamina;
        private CanvasGroup _grupo;
        private Image _borde, _halo;
        private bool _volteable, _dentro;
        private float _hover, _giro, _entrada, _tSello = 9, _retraso;
        private Action _alPulsar;

        public bool Elegida { get; set; }
        /// <summary>Otra carta esta elegida: esta se atenua (sigue pudiendose elegir).</summary>
        public bool Apagada { get; set; }
        /// <summary>Avisa al pasar el raton (true) y al salir (false).</summary>
        public Action<bool> AlEnfocar;

        public static CartaDeMetodologia Crear(UiKit ui, Transform padre, MethodologyProfile m, bool volteable,
                                               Action alPulsar, float retraso = 0) {
            return Crear(ui, padre, m.Nombre, p => DibujosDeMetodologia.Icono(p, m), volteable, alPulsar, retraso, "Cambiar metodología");
        }

        /// <summary>
        /// Una carta cualquiera con imagen de tiza (la usa tambien la arquitectura, 2026-11-08): 'icono' dibuja en una
        /// lamina de 600×300; 'textoDelReverso' es lo que enseña al voltearse.
        /// </summary>
        public static CartaDeMetodologia Crear(UiKit ui, Transform padre, string nombre, Action<Tizador> icono, bool volteable,
                                               Action alPulsar, float retraso, string textoDelReverso) {
            var raiz = ui.Nodo(padre, "Carta " + nombre);
            var c = raiz.gameObject.AddComponent<CartaDeMetodologia>();
            c.Construir(ui, raiz, nombre, icono, volteable, alPulsar, retraso, textoDelReverso);
            return c;
        }

        private void Construir(UiKit ui, RectTransform raiz, string nombreDeLaCarta, Action<Tizador> icono, bool volteable, Action alPulsar,
                               float retraso, string textoDelReverso) {
            _t = ui.Tema; _volteable = volteable; _alPulsar = alPulsar; _retraso = retraso; _entrada = -retraso;
            // La raiz es la zona de clic (transparente, quieta); el cuerpo es lo que se ve y se mueve.
            var zona = raiz.gameObject.AddComponent<Image>();
            zona.color = Color.clear;

            _cuerpo = ui.Nodo(raiz, "Cuerpo");
            UiKit.Rellenar(_cuerpo);
            var fondo = _cuerpo.gameObject.AddComponent<Image>();
            ui.Fondo(fondo, NexusTheme.Alfa(_t.bg950, 0.7f), NexusTheme.RadioLg);
            fondo.raycastTarget = false;
            _halo = ui.Halo(_cuerpo, NexusTheme.Alfa(_t.cyan, 0), NexusTheme.RadioLg);
            _borde = ui.Borde(_cuerpo, _t.lineStrong, NexusTheme.RadioLg, 1.5f);
            _grupo = _cuerpo.gameObject.AddComponent<CanvasGroup>();
            _grupo.blocksRaycasts = false;

            // ---- anverso: imagen + nombre
            _anverso = ui.Nodo(_cuerpo, "Anverso");
            UiKit.Rellenar(_anverso);
            _hueco = ui.Nodo(_anverso, "Imagen");
            _hueco.anchorMin = new Vector2(0.06f, 0.30f); _hueco.anchorMax = new Vector2(0.94f, 0.95f);
            _hueco.offsetMin = _hueco.offsetMax = Vector2.zero;
            _hueco.gameObject.AddComponent<RectMask2D>();
            _lamina = new Lamina(ui, _hueco, AnchoLamina, AltoLamina);
            _lamina.Raiz.anchorMin = _lamina.Raiz.anchorMax = _lamina.Raiz.pivot = new Vector2(0.5f, 0.5f);
            _lamina.Raiz.sizeDelta = new Vector2(AnchoLamina, AltoLamina);
            _lamina.Dibujo.Tiza = true;
            if (icono != null) icono(new Tizador(_lamina, ui.FuenteTiza));
            var nombre = TextoEnCaja.Crear(ui, _anverso, nombreDeLaCarta, EstiloTexto.Encabezado, _t.ink, TextAlignmentOptions.Center,
                                           _t.Px(22), new Vector2(0, 0.04f), new Vector2(1, 0.28f), 8);
            nombre.fontStyle = FontStyles.Bold;

            // el sello «ELEGIDA», arriba a la derecha
            _sello = ui.Nodo(_anverso, "Sello");
            _sello.anchorMin = _sello.anchorMax = new Vector2(1, 1);
            _sello.pivot = new Vector2(1, 1);
            _sello.sizeDelta = new Vector2(_t.Px(78), _t.Px(22));
            _sello.anchoredPosition = new Vector2(-10, -10);
            _sello.localEulerAngles = new Vector3(0, 0, -8);
            var fs = _sello.gameObject.AddComponent<Image>();
            ui.Fondo(fs, NexusTheme.Alfa(_t.cyan, 0.15f), 4);
            fs.raycastTarget = false;
            ui.Borde(_sello, _t.cyan, 4, 2);
            TextoEnCaja.Crear(ui, _sello, "ELEGIDA", EstiloTexto.Leyenda, _t.cyan, TextAlignmentOptions.Center, _t.Px(11),
                              Vector2.zero, Vector2.one, 2).fontStyle = FontStyles.Bold;
            _sello.localScale = Vector3.zero;

            // ---- reverso: deseleccionar
            _reverso = ui.Nodo(_cuerpo, "Reverso");
            UiKit.Rellenar(_reverso);
            if (volteable) {
                var circulo = ui.Nodo(_reverso, "Icono");
                circulo.anchorMin = circulo.anchorMax = new Vector2(0.5f, 0.62f);
                circulo.sizeDelta = new Vector2(_t.Px(64), _t.Px(64));
                var ci = circulo.gameObject.AddComponent<Image>();
                ui.Fondo(ci, NexusTheme.Alfa(_t.danger, 0.15f), 32);
                ci.raycastTarget = false;
                ui.Borde(circulo, _t.danger, 32, 2);
                TextoEnCaja.Crear(ui, circulo, "✕", EstiloTexto.Titulo, _t.danger, TextAlignmentOptions.Center, _t.Px(30),
                                  Vector2.zero, Vector2.one);
                TextoEnCaja.Crear(ui, _reverso, textoDelReverso, EstiloTexto.Encabezado, _t.ink, TextAlignmentOptions.Center,
                                  _t.Px(18), new Vector2(0, 0.10f), new Vector2(1, 0.34f), 10);
            }
            _reverso.gameObject.SetActive(false);
        }

        /// <summary>Estampa el sello con su golpe (al elegir).</summary>
        public void Sellar() { Elegida = true; _tSello = 0; }

        public void OnPointerEnter(PointerEventData e) { _dentro = true; if (AlEnfocar != null) AlEnfocar(true); }
        public void OnPointerExit(PointerEventData e) { _dentro = false; if (AlEnfocar != null) AlEnfocar(false); }
        private void OnDisable() { _dentro = false; }

        public void OnPointerClick(PointerEventData e) {
            if (_entrada < 0.3f) return;
            if (_volteable && _giro < 0.5f) { _dentro = true; return; }   // primero se da la vuelta; el clic es en el reverso
            if (_alPulsar != null) _alPulsar();
        }

        private void Update() {
            var dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            Paso(dt);
        }

        /// <summary>Avanza la animacion a mano (herramienta de capturas).</summary>
        public void Avanzar(float s) { for (var x = 0f; x < s; x += 0.05f) Paso(0.05f); }

        private void Paso(float dt) {
            var r = _hueco.rect;
            if (r.width > 1 && r.height > 1) {
                var k = Mathf.Min(r.width / AnchoLamina, r.height / AltoLamina);
                _lamina.Raiz.localScale = new Vector3(k, k, 1);
            }
            _entrada += dt;
            var e = 1 - Mathf.Pow(1 - Mathf.Clamp01(_entrada / 0.45f), 3);
            _hover = Mathf.MoveTowards(_hover, _dentro ? 1 : 0, dt / 0.15f);
            if (_volteable) _giro = Mathf.MoveTowards(_giro, _dentro ? 1 : 0, dt / 0.28f);
            _tSello += dt;

            var aplasta = _volteable ? Mathf.Max(0.02f, Mathf.Abs(Mathf.Cos(_giro * Mathf.PI))) : 1f;
            var crece = (0.96f + 0.04f * e) * (1 + 0.04f * _hover);
            _cuerpo.localScale = new Vector3(crece * aplasta, crece, 1);
            _cuerpo.anchoredPosition = new Vector2(0, -(1 - e) * 40 + _hover * 8);
            var reverso = _volteable && _giro >= 0.5f;
            if (_anverso.gameObject.activeSelf == reverso) { _anverso.gameObject.SetActive(!reverso); _reverso.gameObject.SetActive(reverso); }

            _grupo.alpha = e * (Apagada && _hover < 0.5f ? 0.55f : 1f);
            _borde.color = reverso ? _t.danger : Elegida ? _t.cyan : Color.Lerp(_t.lineStrong, _t.cyan, _hover);
            _halo.color = NexusTheme.Alfa(reverso ? _t.danger : _t.cyan, Mathf.Max(Elegida ? 0.22f : 0, _hover * 0.32f));
            var s = Elegida ? (_tSello < 0.4f ? ActoresDeEscena.Rebote(_tSello / 0.4f) : 1f) : 0f;
            _sello.localScale = new Vector3(s, s, 1);
        }
    }
}
