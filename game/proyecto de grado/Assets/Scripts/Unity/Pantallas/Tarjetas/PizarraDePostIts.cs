using System;
using System.Collections.Generic;
using Nexus.Core.Tarjetas;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Las ceremonias (retrospectiva, planificacion, daily, revision…) como una PIZARRA DE POST-ITS (2026-11-05): las
    /// formas de llevarla son post-its pegados en «Propuestas», y se elige ARRASTRANDO uno a «Lo hacemos» (o con un
    /// clic, o con su numero: el post-it vuela solo). Al llegar se pega con un golpe de tiza — la pizarra tiembla, sale
    /// polvo, aparece «¡Acordado!» — y los demas se despegan y caen.
    ///
    /// Es otra forma de elegir que la mano de cartas de las decisiones, a proposito: una ceremonia es algo que se
    /// acuerda en equipo delante de una pizarra, no una carta que se juega a solas. Lo que se elige y como se evalua no
    /// cambia: quien la abre recibe el indice y llama al mismo metodo de la sesion que antes.
    /// </summary>
    public sealed class PizarraDePostIts : MonoBehaviour {
        private static readonly Color[] ColoresDePapel = {
            new Color32(0xF6, 0xE2, 0x7F, 0xFF), new Color32(0xF5, 0xA9, 0xC6, 0xFF),
            new Color32(0x9C, 0xD7, 0xEE, 0xFF), new Color32(0xB9, 0xE3, 0xA2, 0xFF)
        };
        private static readonly Color Tiza = new Color(0.93f, 0.93f, 0.88f, 1f);
        private static readonly Color Verde = new Color32(0x1C, 0x2A, 0x23, 0xFF);

        private const float AnchoPostIt = 230, AltoPostIt = 150;
        private const float DuracionDelVuelo = 0.32f, DuracionTotal = 1.05f;

        private UiKit _ui;
        private RectTransform _pizarra, _zona;
        private CanvasGroup _grupoAcordado;
        private readonly List<PostIt> _postIts = new List<PostIt>();
        private readonly List<Image> _polvo = new List<Image>();
        private Action<int> _alPrevisualizar, _alElegir;
        private int _elegido = -1;
        private float _t;
        private bool _avisado;

        public static PizarraDePostIts Crear(UiKit ui, RectTransform padre, TarjetaDef tarjeta,
                                             Action<int> alPrevisualizar, Action<int> alElegir) {
            var rt = ui.Nodo(padre, "Pizarra");
            UiKit.Rellenar(rt);
            var p = rt.gameObject.AddComponent<PizarraDePostIts>();
            p._ui = ui;
            p._alPrevisualizar = alPrevisualizar;
            p._alElegir = alElegir;
            p.Construir(rt, tarjeta);
            return p;
        }

        private void Construir(RectTransform raiz, TarjetaDef tarjeta) {
            var ui = _ui;
            _pizarra = ui.Nodo(raiz, "Tablero");
            UiKit.Rellenar(_pizarra, 8);
            var fondo = _pizarra.gameObject.AddComponent<Image>();
            ui.Fondo(fondo, Verde, NexusTheme.RadioMd);
            fondo.raycastTarget = false;
            ui.Borde(_pizarra, new Color32(0x6B, 0x4A, 0x2E, 0xFF), NexusTheme.RadioMd, 6);   // el marco de madera

            // Quien la plantea y la pregunta, escritas con tiza arriba.
            var cabecera = TextoDeTiza(_pizarra, (string.IsNullOrEmpty(tarjeta.Personaje) ? "" : tarjeta.Personaje + ":  ") + tarjeta.Frase, 30,
                                       TextAlignmentOptions.TopLeft);
            var rc = cabecera.rectTransform;
            rc.anchorMin = new Vector2(0, 1);
            rc.anchorMax = new Vector2(1, 1);
            rc.pivot = new Vector2(0.5f, 1);
            rc.offsetMin = new Vector2(32, -96);
            rc.offsetMax = new Vector2(-32, -22);

            Colocar(TextoDeTiza(_pizarra, "Propuestas", 24, TextAlignmentOptions.TopLeft).rectTransform,
                    new Vector2(0.03f, 0.74f), new Vector2(0.5f, 0.82f));
            Colocar(TextoDeTiza(_pizarra, "Lo hacemos", 24, TextAlignmentOptions.TopLeft).rectTransform,
                    new Vector2(0.64f, 0.74f), new Vector2(0.97f, 0.82f));

            // La columna de destino: un recuadro de tiza.
            _zona = ui.Nodo(_pizarra, "Lo hacemos");
            Colocar(_zona, new Vector2(0.64f, 0.08f), new Vector2(0.97f, 0.73f));
            ui.Borde(_zona, NexusTheme.Alfa(Tiza, 0.55f), NexusTheme.RadioMd, 2);
            var pista = TextoDeTiza(_zona, "Arrastra aquí lo que hacéis", 20, TextAlignmentOptions.Center);
            pista.color = NexusTheme.Alfa(Tiza, 0.35f);
            UiKit.Rellenar(pista.rectTransform, 16);
            var acordado = TextoDeTiza(_zona, "¡Acordado!", 34, TextAlignmentOptions.Top);
            UiKit.Rellenar(acordado.rectTransform, 14);
            _grupoAcordado = acordado.gameObject.AddComponent<CanvasGroup>();
            _grupoAcordado.alpha = 0;

            var rnd = new System.Random((tarjeta.Id ?? "").GetHashCode());
            for (var i = 0; i < tarjeta.Opciones.Count; i++)
                _postIts.Add(PostIt.Crear(this, i, tarjeta.Opciones[i].Etiqueta, ColoresDePapel[i % ColoresDePapel.Length], rnd));

            for (var i = 0; i < 14; i++) {
                var d = ui.Nodo(_pizarra, "Polvo");
                d.sizeDelta = new Vector2(10, 10);
                var img = d.gameObject.AddComponent<Image>();
                img.sprite = PlanoIlustrado.Brillo();
                img.color = new Color(1, 1, 1, 0);
                img.raycastTarget = false;
                _polvo.Add(img);
            }
        }

        private static void Colocar(RectTransform rt, Vector2 min, Vector2 max) {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private TMP_Text TextoDeTiza(Transform padre, string texto, float tam, TextAlignmentOptions alineacion) {
            var t = _ui.Texto(padre, texto, EstiloTexto.Cuerpo, Tiza, alineacion);
            var alto = t.GetComponent<AltoDeTexto>();
            if (alto != null) Destroy(alto);
            t.font = _ui.FuenteTiza;
            t.fontSize = tam;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        // ==================================================================== donde va cada post-it

        /// <summary>Su sitio en «Propuestas»: una rejilla de dos columnas, con un poco de desorden.</summary>
        private Vector2 Casa(int i) {
            var r = _pizarra.rect;
            var x = r.xMin + r.width * 0.05f + AnchoPostIt / 2 + (i % 2) * (AnchoPostIt + 34);
            var y = r.yMin + r.height * 0.72f - AltoPostIt / 2 - (i / 2) * (AltoPostIt + 26);
            return new Vector2(x, y);
        }

        private Vector2 CentroDeLaZona {
            get {
                var r = _pizarra.rect;
                return new Vector2(Mathf.Lerp(r.xMin, r.xMax, (0.64f + 0.97f) / 2), Mathf.Lerp(r.yMin, r.yMax, (0.08f + 0.73f) / 2));
            }
        }

        internal bool Bloqueada { get { return _elegido >= 0; } }
        internal RectTransform Tablero { get { return _pizarra; } }
        internal UiKit Ui { get { return _ui; } }

        internal void Mirar(int i) { if (!Bloqueada) _alPrevisualizar?.Invoke(i); }

        /// <summary>¿Se solto el post-it encima de «Lo hacemos»?</summary>
        internal bool SobreLaZona(Vector2 pantalla, Camera camara) {
            return RectTransformUtility.RectangleContainsScreenPoint(_zona, pantalla, camara);
        }

        internal void Elegir(int i) {
            if (Bloqueada) { if (_t < DuracionTotal) _t = DuracionTotal; return; }   // un segundo clic lo acorta
            _elegido = i;
            _t = 0;
            _alPrevisualizar?.Invoke(i);
            _postIts[i].transform.SetAsLastSibling();
        }

        private void Update() {
            var dt = Time.unscaledDeltaTime;
            Teclado();
            if (_pizarra.rect.width < 10) return;

            if (!Bloqueada) {
                for (var i = 0; i < _postIts.Count; i++) _postIts[i].Reposar(Casa(i), dt);
                return;
            }
            if (_avisado) return;
            _t += dt;
            var t = _t;

            // El elegido vuela a «Lo hacemos» y se aplasta al pegarse.
            var vuelo = Mathf.Clamp01(t / DuracionDelVuelo);
            var e = 1 - Mathf.Pow(1 - vuelo, 3);
            var aplastar = Mathf.Clamp01((t - DuracionDelVuelo) / 0.12f);
            var escala = vuelo < 1 ? Mathf.Lerp(1.05f, 1.18f, e) : Mathf.Lerp(1.18f, 1f, aplastar);
            _postIts[_elegido].Volar(CentroDeLaZona, e, escala, -3f);

            // El golpe de tiza: la pizarra tiembla, sale polvo y se escribe «¡Acordado!».
            var golpe = t - DuracionDelVuelo;
            if (golpe >= 0 && golpe < 0.25f)
                _pizarra.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 80f) * 4f * (1 - golpe / 0.25f), 0);
            else if (golpe >= 0.25f && _pizarra.anchoredPosition != Vector2.zero) _pizarra.anchoredPosition = Vector2.zero;
            Polvo(golpe);
            _grupoAcordado.alpha = Mathf.Clamp01((t - DuracionDelVuelo - 0.1f) / 0.25f);

            // Los demas se despegan y caen.
            for (var i = 0; i < _postIts.Count; i++)
                if (i != _elegido) _postIts[i].Caer(Mathf.Clamp01((t - 0.15f) / 0.6f), i < _elegido ? -1 : 1);

            if (t >= DuracionTotal) {
                _avisado = true;
                _pizarra.anchoredPosition = Vector2.zero;
                _alElegir?.Invoke(_elegido);
            }
        }

        private void Polvo(float golpe) {
            var centro = CentroDeLaZona;
            for (var i = 0; i < _polvo.Count; i++) {
                var img = _polvo[i];
                if (golpe < 0 || golpe > 0.6f) { if (img.color.a != 0) img.color = new Color(1, 1, 1, 0); continue; }
                var f = golpe / 0.6f;
                var ang = i * Mathf.PI * 2 / _polvo.Count + i * 0.37f;
                var dist = Mathf.Lerp(AnchoPostIt * 0.45f, AnchoPostIt * 0.95f, 1 - Mathf.Pow(1 - f, 2));
                img.rectTransform.anchoredPosition = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.7f) * dist - new Vector2(0, f * 20f);
                img.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.4f, 0.6f, f);
                img.color = NexusTheme.Alfa(Tiza, 0.8f * (1 - f));
            }
        }

        private static readonly Key[] Digitos = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
        private static readonly Key[] Numericos = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4 };

        private void Teclado() {
            var teclado = Keyboard.current;
            if (teclado == null) return;
            if (ManoDeCartas.PuedeEscuchar != null && !ManoDeCartas.PuedeEscuchar(transform)) return;
            if (Bloqueada) {
                if (teclado.enterKey.wasPressedThisFrame || teclado.spaceKey.wasPressedThisFrame) Elegir(_elegido);
                return;
            }
            for (var i = 0; i < _postIts.Count && i < Digitos.Length; i++)
                if (teclado[Digitos[i]].wasPressedThisFrame || teclado[Numericos[i]].wasPressedThisFrame) { Elegir(i); return; }
        }

        // ==================================================================== un post-it

        internal sealed class PostIt : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
                                       IBeginDragHandler, IDragHandler, IEndDragHandler {
            private PizarraDePostIts _pizarra;
            private int _indice;
            private RectTransform _rt;
            private CanvasGroup _grupo;
            private float _giro, _escala = 1, _encima;
            private bool _arrastrando, _colocado, _dentro;
            private Vector2 _agarre;

            public static PostIt Crear(PizarraDePostIts pizarra, int indice, string texto, Color papel, System.Random rnd) {
                var ui = pizarra.Ui;
                var rt = ui.Nodo(pizarra.Tablero, "Post-it " + (indice + 1));
                rt.sizeDelta = new Vector2(AnchoPostIt, AltoPostIt);
                var p = rt.gameObject.AddComponent<PostIt>();
                p._pizarra = pizarra;
                p._indice = indice;
                p._rt = rt;
                p._giro = (float)(rnd.NextDouble() * 10 - 5);
                p._grupo = rt.gameObject.AddComponent<CanvasGroup>();

                // Sombra, papel, cinta y el texto a mano.
                var sombra = ui.Nodo(rt, "Sombra");
                UiKit.Rellenar(sombra);
                sombra.offsetMin = new Vector2(6, -8);
                sombra.offsetMax = new Vector2(6, -8);
                var imgSombra = sombra.gameObject.AddComponent<Image>();
                ui.Fondo(imgSombra, new Color(0, 0, 0, 0.35f), 4);
                imgSombra.raycastTarget = false;

                var hoja = ui.Nodo(rt, "Papel");
                UiKit.Rellenar(hoja);
                var imgHoja = hoja.gameObject.AddComponent<Image>();
                ui.Fondo(imgHoja, papel, 3);
                imgHoja.raycastTarget = true;

                var cinta = ui.Nodo(hoja, "Cinta");
                cinta.anchorMin = cinta.anchorMax = new Vector2(0.5f, 1);
                cinta.sizeDelta = new Vector2(70, 18);
                cinta.localEulerAngles = new Vector3(0, 0, -4);
                var imgCinta = cinta.gameObject.AddComponent<Image>();
                imgCinta.color = new Color(1, 1, 1, 0.35f);
                imgCinta.raycastTarget = false;

                var numero = SinAltoMinimo(ui.Texto(hoja, (indice + 1).ToString(), EstiloTexto.Mono, new Color(0, 0, 0, 0.45f), TextAlignmentOptions.TopRight));
                UiKit.Rellenar(numero.rectTransform, 10);

                var t = SinAltoMinimo(ui.Texto(hoja, texto ?? "", EstiloTexto.Cuerpo, new Color32(0x26, 0x26, 0x2A, 0xFF), TextAlignmentOptions.Center));
                t.font = ui.FuenteTiza;
                t.enableAutoSizing = true;
                t.fontSizeMin = 16;
                t.fontSizeMax = 28;
                t.textWrappingMode = TextWrappingModes.Normal;
                UiKit.Rellenar(t.rectTransform, 18);
                return p;
            }

            private static TMP_Text SinAltoMinimo(TMP_Text t) {
                var alto = t.GetComponent<AltoDeTexto>();
                if (alto != null) Destroy(alto);
                return t;
            }

            /// <summary>En su sitio de «Propuestas»: llega a el, se mece un poco, y crece si se le pasa el raton.</summary>
            public void Reposar(Vector2 casa, float dt) {
                if (_arrastrando) return;
                var k = _colocado ? 1 - Mathf.Exp(-12f * dt) : 1f;
                _colocado = true;
                _rt.anchoredPosition = Vector2.Lerp(_rt.anchoredPosition, casa + Vector2.up * 8f * _encima, k);
                _escala = Mathf.Lerp(_escala, 1f + 0.07f * _encima, k);
                _rt.localScale = Vector3.one * _escala;
                var meceo = Mathf.Sin(Time.unscaledTime * 1.3f + _indice) * 0.6f;
                _rt.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(_giro + meceo, 0, _encima));
            }

            public void Volar(Vector2 destino, float f, float escala, float giro) {
                _rt.anchoredPosition = Vector2.Lerp(_rt.anchoredPosition, destino, Mathf.Max(f, 0.18f));
                _rt.localScale = Vector3.one * escala;
                var z = _rt.localEulerAngles.z;
                if (z > 180) z -= 360;
                _rt.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(z, giro, f));
                _grupo.blocksRaycasts = false;
            }

            /// <summary>Se despega por una esquina (gira) y cae, apagandose.</summary>
            public void Caer(float f, int lado) {
                var e = f * f;
                var dt = Time.unscaledDeltaTime;
                var p = _rt.anchoredPosition;
                _rt.anchoredPosition = new Vector2(p.x + lado * 30f * dt, p.y - 700f * e * dt);
                _rt.localEulerAngles = new Vector3(0, 0, _giro + lado * 40f * e);
                _grupo.alpha = 1 - f;
                _grupo.blocksRaycasts = false;
            }

            private void Update() {
                var objetivo = !_pizarra.Bloqueada && (_arrastrando || _dentro) ? 1f : 0f;
                _encima = Mathf.MoveTowards(_encima, objetivo, Time.unscaledDeltaTime / 0.18f);
            }

            public void OnPointerEnter(PointerEventData e) { _dentro = true; _pizarra.Mirar(_indice); }
            public void OnPointerExit(PointerEventData e) { _dentro = false; if (!_arrastrando) _pizarra.Mirar(-1); }
            public void OnPointerClick(PointerEventData e) { if (!_arrastrando) _pizarra.Elegir(_indice); }

            public void OnBeginDrag(PointerEventData e) {
                if (_pizarra.Bloqueada) return;
                _arrastrando = true;
                transform.SetAsLastSibling();
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_pizarra.Tablero, e.position, e.pressEventCamera, out local);
                _agarre = _rt.anchoredPosition - local;
            }

            public void OnDrag(PointerEventData e) {
                if (!_arrastrando) return;
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_pizarra.Tablero, e.position, e.pressEventCamera, out local))
                    _rt.anchoredPosition = local + _agarre;
                _rt.localScale = Vector3.one * 1.1f;
                _rt.localEulerAngles = new Vector3(0, 0, Mathf.Clamp(e.delta.x * -0.6f, -10, 10));
            }

            public void OnEndDrag(PointerEventData e) {
                if (!_arrastrando) return;
                _arrastrando = false;
                if (_pizarra.SobreLaZona(e.position, e.pressEventCamera)) _pizarra.Elegir(_indice);
                else _pizarra.Mirar(-1);   // vuelve solo a su sitio (Reposar)
            }
        }
    }
}
