using System;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos.Corregir {
    /// <summary>
    /// Un punto marcado del diagrama: un disco con su numero que late como un sonar. Elegido, se vuelve cyan y su aro
    /// crece; con una carta jugada lleva debajo una chapa con el dibujo y el nombre de la carta. Al verificar gira y
    /// enseña √ o ×, con chispas si estaba bien. Vive dentro de la lamina, asi que escala con ella.
    /// </summary>
    public sealed class MarcadorDePunto : MonoBehaviour {
        public const float Radio = 28;
        private const float AnchoChapa = 250, AltoChapa = 44;

        public string Punto { get; private set; }
        public int Numero { get; private set; }
        public RectTransform Disco { get { return _disco; } }
        public bool Elegido;
        public bool TieneCarta { get; private set; }
        public bool Revelado { get; private set; }

        private UiKit _ui;
        private NexusTheme _tema;
        private RectTransform _rt, _disco, _aro, _chapa, _sello;
        private Image _imgDisco, _imgAro, _fondoChapa, _bordeChapa;
        private TMP_Text _texto, _nombreChapa, _textoSello;
        private DibujoUI _dibujoChapa;
        private CanvasGroup _grupoSello;
        private float _t, _entrada, _meneo;
        private bool _girando, _destacado;
        private Color _color;

        public static MarcadorDePunto Crear(UiKit ui, Lamina lamina, string punto, int numero, Vector2 centro, Action alPulsar) {
            var tema = ui.Tema;
            var rt = ui.Nodo(lamina.Raiz, "Punto #" + numero);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(centro.x, -centro.y);
            rt.sizeDelta = new Vector2(Radio * 2, Radio * 2);
            var m = rt.gameObject.AddComponent<MarcadorDePunto>();
            m._ui = ui; m._tema = tema; m._rt = rt; m.Punto = punto; m.Numero = numero;
            m._color = tema.warning;
            // La chapa se queda dentro de la lamina: un marcador pegado a un borde la empujaria fuera (y la mascara la cortaria).
            var anchoLamina = lamina.Raiz.sizeDelta.x;
            var chapaX = anchoLamina > 1 ? Mathf.Clamp(centro.x, AnchoChapa / 2 + 6, anchoLamina - AnchoChapa / 2 - 6) - centro.x : 0f;

            m._aro = ui.Nodo(rt, "Aro");
            m._aro.anchorMin = m._aro.anchorMax = m._aro.pivot = new Vector2(0.5f, 0.5f);
            m._aro.sizeDelta = new Vector2(Radio * 2, Radio * 2);
            m._imgAro = m._aro.gameObject.AddComponent<Image>();
            m._imgAro.sprite = ui.SpriteCircular();
            m._imgAro.raycastTarget = false;

            m._disco = ui.Nodo(rt, "Disco");
            m._disco.anchorMin = m._disco.anchorMax = m._disco.pivot = new Vector2(0.5f, 0.5f);
            m._disco.sizeDelta = new Vector2(Radio * 2, Radio * 2);
            m._imgDisco = m._disco.gameObject.AddComponent<Image>();
            m._imgDisco.sprite = ui.SpriteCircular();
            var borde = m._disco.gameObject.AddComponent<Outline>();
            borde.effectColor = NexusTheme.Alfa(tema.bg950, 0.9f);
            borde.effectDistance = new Vector2(2, -2);
            var boton = m._disco.gameObject.AddComponent<Button>();
            boton.targetGraphic = m._imgDisco;
            var colores = ColorBlock.defaultColorBlock;
            colores.highlightedColor = new Color(1, 1, 1, 1);
            colores.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1);
            boton.colors = colores;
            if (alPulsar != null) boton.onClick.AddListener(() => alPulsar());
            m._texto = Texto(ui, m._disco, "#" + numero, 22, Color.white, TextAlignmentOptions.Center);
            m._texto.fontStyle = FontStyles.Bold;
            UiKit.Rellenar(m._texto.rectTransform);

            // La chapa de la carta jugada, debajo del disco.
            m._chapa = ui.Nodo(rt, "Chapa");
            m._chapa.anchorMin = m._chapa.anchorMax = new Vector2(0.5f, 0.5f);
            m._chapa.pivot = new Vector2(0.5f, 0f);
            m._chapa.anchoredPosition = new Vector2(chapaX, Radio + 8);   // encima: en el hueco entre filas, sin tapar la caja
            m._chapa.sizeDelta = new Vector2(AnchoChapa, AltoChapa);
            m._fondoChapa = m._chapa.gameObject.AddComponent<Image>();
            ui.Fondo(m._fondoChapa, tema.bg900, 8);
            m._fondoChapa.raycastTarget = false;
            m._bordeChapa = ui.Borde(m._chapa, tema.cyan, 8, 2);
            var icono = ui.Nodo(m._chapa, "Icono");
            Lamina.Colocar(icono, 7, 6, 32, 32);
            icono.gameObject.AddComponent<CanvasRenderer>();
            m._dibujoChapa = icono.gameObject.AddComponent<DibujoUI>();
            m._dibujoChapa.raycastTarget = false;
            m._nombreChapa = Texto(ui, m._chapa, "", 18, tema.ink, TextAlignmentOptions.MidlineLeft);
            m._nombreChapa.enableAutoSizing = true;
            m._nombreChapa.fontSizeMin = 12;
            m._nombreChapa.fontSizeMax = 18;
            Lamina.Colocar(m._nombreChapa.rectTransform, 46, 2, AnchoChapa - 54, AltoChapa - 4);
            m._chapa.localScale = Vector3.zero;
            m._chapa.gameObject.SetActive(false);

            // El sello que cae al poner una carta.
            m._sello = ui.Nodo(rt, "Sello");
            m._sello.anchorMin = m._sello.anchorMax = m._sello.pivot = new Vector2(0.5f, 0.5f);
            m._sello.anchoredPosition = new Vector2(chapaX, Radio + 30);
            m._sello.sizeDelta = new Vector2(130, 40);
            m._sello.localEulerAngles = new Vector3(0, 0, 10);
            var fondoSello = m._sello.gameObject.AddComponent<Image>();
            ui.Fondo(fondoSello, NexusTheme.Alfa(tema.cyan, 0.15f), 6);
            fondoSello.raycastTarget = false;
            ui.Borde(m._sello, tema.cyan, 6, 3);
            m._textoSello = Texto(ui, m._sello, "PUESTA", 19, tema.cyan, TextAlignmentOptions.Center);
            m._textoSello.fontStyle = FontStyles.Bold;
            m._textoSello.characterSpacing = 5;
            UiKit.Rellenar(m._textoSello.rectTransform);
            m._grupoSello = m._sello.gameObject.AddComponent<CanvasGroup>();
            m._grupoSello.alpha = 0;
            m._grupoSello.blocksRaycasts = false;

            rt.localScale = Vector3.zero;
            m.Pintar();
            return m;
        }

        private static TMP_Text Texto(UiKit ui, Transform padre, string texto, float tam, Color color, TextAlignmentOptions al) {
            var t = ui.Texto(padre, texto, EstiloTexto.Cuerpo, color, al);
            var alto = t.GetComponent<AltoDeTexto>();
            if (alto != null) Destroy(alto);
            t.fontSize = tam;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Entra con un rebote elastico tras 'retraso' segundos.</summary>
        public void Aparecer(Animador anim, float retraso) {
            anim.Lanzar(0.6f, t => { _entrada = t; _rt.localScale = Vector3.one * Curvas.Elastica(t); }, null, retraso, gameObject);
        }

        /// <summary>Sin animar (el cierre, que se vuelve a pintar ya resuelto).</summary>
        public void Mostrar() { _entrada = 1; _rt.localScale = Vector3.one; }

        private void Update() {
            if (_entrada <= 0) return;
            _t += Time.unscaledDeltaTime;
            if (_meneo > 0) {
                _meneo = Mathf.Max(0, _meneo - Time.unscaledDeltaTime * 2f);
                _disco.localEulerAngles = new Vector3(0, 0, Mathf.Sin(_meneo * 40f) * 18f * _meneo);
            }
            // El sonar: un aro que crece y se apaga. Mas rapido y cyan si esta elegido; quieto si ya esta revelado.
            if (Revelado) {
                _imgAro.color = NexusTheme.Alfa(_destacado ? _tema.cyan : _color, _destacado ? 0.8f : 0.25f);
                _aro.localScale = Vector3.one * (_destacado ? 1.35f + 0.1f * Mathf.Sin(_t * 6f) : 1.45f);
                return;
            }
            var periodo = Elegido ? 0.9f : 1.8f;
            var f = (_t % periodo) / periodo;
            _aro.localScale = Vector3.one * Mathf.Lerp(1f, Elegido ? 2.1f : 1.7f, Curvas.SalidaCubica(f));
            var alfa = (TieneCarta && !Elegido ? 0.25f : 0.55f) * (1 - f);
            _imgAro.color = NexusTheme.Alfa(Elegido ? _tema.cyan : _color, alfa);
            var latido = Elegido ? 1f + 0.06f * Mathf.Sin(_t * 8f) : 1f;
            if (!_girando) _disco.localScale = Vector3.one * latido;
        }

        public void Pintar() {
            if (Revelado) return;
            _imgDisco.color = Elegido ? _tema.cyan : TieneCarta ? _tema.bg900 : _color;
            _texto.color = TieneCarta && !Elegido ? _tema.ink : _tema.bg950;   // tinta oscura sobre el neon
        }

        /// <summary>«Esa carta ya está aquí»: se menea para que se vea dónde.</summary>
        public void Menear() { _meneo = 1f; }

        public void PonerCarta(string nombre, string icono, bool esEstaBien, Animador anim, RectTransform rafaga) {
            TieneCarta = true;
            _chapa.gameObject.SetActive(true);
            _nombreChapa.text = nombre;
            var acento = esEstaBien ? _tema.success : _tema.cyan;
            _bordeChapa.color = acento;
            _fondoChapa.color = _tema.bg900;
            _nombreChapa.color = _tema.ink;
            _dibujoChapa.Limpiar();
            DibujosDeSoluciones.Dibujar(_dibujoChapa, esEstaBien ? DibujosDeSoluciones.EstaBien : icono, 0, 0, 32, _tema.ink, acento);
            _textoSello.text = esEstaBien ? "BIEN ASÍ" : "PUESTA";
            _textoSello.color = acento;
            Pintar();
            anim.Lanzar(0.45f, t => { if (_chapa != null) _chapa.localScale = Vector3.one * Curvas.SalidaAtras(t); }, null, 0, gameObject);
            // El sello cae grande y se asienta; luego se desvanece.
            anim.Lanzar(0.9f, t => {
                if (_sello == null) return;
                var cae = Curvas.SalidaCubica(Mathf.Clamp01(t / 0.25f));
                _sello.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, cae);
                _grupoSello.alpha = t < 0.25f ? cae : 1 - Mathf.Clamp01((t - 0.55f) / 0.45f);
            }, null, 0.05f, gameObject);
            if (rafaga != null) Rafaga(anim, rafaga, acento);
        }

        public void QuitarCarta(Animador anim) {
            TieneCarta = false;
            Pintar();
            anim.Lanzar(0.22f, t => { if (_chapa != null) _chapa.localScale = Vector3.one * (1 - Curvas.SalidaCubica(t)); },
                        () => { if (_chapa != null && !TieneCarta) _chapa.gameObject.SetActive(false); }, 0, gameObject);
        }

        /// <summary>
        /// El veredicto de este punto: el disco gira de canto, cambia a √ o ×, y vuelve con un rebote. Si estaba mal, la
        /// chapa enseña lo que tocaba ('correcta').
        /// </summary>
        public void Revelar(bool bien, string correcta, string iconoCorrecto, bool correctaEsEstaBien, Animador anim, RectTransform rafaga) {
            var cambiado = false;
            _girando = true;
            anim.Lanzar(0.55f, t => {
                if (_disco == null) return;
                var giro = t < 0.4f ? 1 - Curvas.EntradaSalida(t / 0.4f) : Curvas.SalidaAtras((t - 0.4f) / 0.6f);
                _disco.localScale = new Vector3(Mathf.Max(0.02f, giro), 1, 1) * (1 + 0.15f * Curvas.Campana(t));
                if (t < 0.4f || cambiado) return;
                cambiado = true;
                Revelado = true;
                _color = bien ? _tema.success : _tema.danger;
                _imgDisco.color = _color;
                _texto.color = bien ? _tema.bg950 : Color.white;
                _texto.text = bien ? "√" : "×";
                _texto.fontSize = 32;
                if (_chapa.gameObject.activeSelf || !bien) {
                    _chapa.gameObject.SetActive(true);
                    _chapa.localScale = Vector3.one;
                    _bordeChapa.color = _color;
                    if (!bien) {
                        _nombreChapa.text = "Tocaba: " + correcta;
                        _nombreChapa.color = _tema.ink;
                        _dibujoChapa.Limpiar();
                        DibujosDeSoluciones.Dibujar(_dibujoChapa, correctaEsEstaBien ? DibujosDeSoluciones.EstaBien : iconoCorrecto, 0, 0, 32, _tema.ink, _tema.success);
                    }
                }
            }, null, 0, gameObject);
            if (bien && rafaga != null) { Rafaga(anim, rafaga, _tema.success); Chispas(anim); }
        }

        /// <summary>El cierre: ya revelado, sin animar y sin chapa (lo que se puso y lo que tocaba lo cuenta el globo).</summary>
        public void PintarVeredicto(bool bien) {
            Revelado = true;
            _color = bien ? _tema.success : _tema.danger;
            _imgDisco.color = _color;
            _texto.color = bien ? _tema.bg950 : Color.white;
            _texto.text = bien ? "√" : "×";
            _texto.fontSize = 32;
            _chapa.gameObject.SetActive(false);
        }

        /// <summary>El marcador elegido en el cierre: un aro cian, sin cambiar su √ o ×.</summary>
        public void Destacar(bool si) { _destacado = si; }

        /// <summary>El golpe de luz (el sprite de brillo del juego) que estalla sobre el disco.</summary>
        private void Rafaga(Animador anim, RectTransform plantilla, Color color) {
            var r = _ui.Nodo(_rt, "Rafaga");
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(120, 120);
            r.SetAsFirstSibling();
            var img = r.gameObject.AddComponent<Image>();
            var fuente = plantilla != null ? plantilla.GetComponent<Image>() : null;
            img.sprite = fuente != null ? fuente.sprite : _ui.SpriteCircular();
            img.raycastTarget = false;
            anim.Lanzar(0.5f, t => {
                if (r == null) return;
                r.localScale = Vector3.one * Mathf.Lerp(0.4f, 2.6f, Curvas.SalidaCubica(t));
                img.color = NexusTheme.Alfa(color, 0.8f * (1 - t));
            }, () => { if (r != null) Destroy(r.gameObject); }, 0, gameObject);
        }

        /// <summary>Chispas que salen disparadas en circulo y caen.</summary>
        private void Chispas(Animador anim) {
            const int n = 10;
            for (var i = 0; i < n; i++) {
                var ch = _ui.Nodo(_rt, "Chispa");
                ch.anchorMin = ch.anchorMax = ch.pivot = new Vector2(0.5f, 0.5f);
                ch.sizeDelta = new Vector2(9, 9);
                var img = ch.gameObject.AddComponent<Image>();
                img.sprite = _ui.SpriteCircular();
                img.raycastTarget = false;
                var color = i % 2 == 0 ? _tema.success : _tema.cyan;
                var ang = (i / (float)n) * Mathf.PI * 2 + 0.3f;
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                var lejos = 60f + (i % 3) * 18f;
                anim.Lanzar(0.7f, t => {
                    if (ch == null) return;
                    var e = Curvas.SalidaCubica(t);
                    ch.anchoredPosition = dir * lejos * e + new Vector2(0, -40f * t * t);
                    img.color = NexusTheme.Alfa(color, 1 - t);
                    ch.localScale = Vector3.one * (1 - 0.5f * t);
                }, () => { if (ch != null) Destroy(ch.gameObject); }, 0.12f, gameObject);
            }
        }
    }
}
