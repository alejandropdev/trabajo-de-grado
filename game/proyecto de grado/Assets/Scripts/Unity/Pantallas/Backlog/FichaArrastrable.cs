using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Una ficha del muro del backlog (2026-11-06) que se coge y se suelta. Al cogerla se despega de su lista (deja su
    /// hueco), se inclina y echa sombra, y sigue al puntero por encima de todo (la capa de arrastre). Al soltarla le
    /// pregunta al destino que tiene debajo (un carril, la cara de alguien): si no se puede, vuelve a su sitio con un
    /// meneo y se dice por que en una linea; si se puede, el destino hace lo suyo.
    /// Un clic sin arrastrar es AlPulsar (abrir su hoja). Una ficha Fija no se coge: deja que su lista se desplace.
    /// </summary>
    public sealed class FichaArrastrable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler {
        public string Id;
        /// <summary>El tipo de trabajo: con el, las caras dicen si es su fuerte o le cuesta.</summary>
        public string Tipo;
        /// <summary>El carril del que sale (lo usan los destinos para saber si es reordenar o pasar de un carril a otro).</summary>
        public string Origen;
        public bool Fija;
        public string PorQueFija;
        public RectTransform Capa;
        public Action AlPulsar;
        public Action<string> AlFallar;

        /// <summary>La ficha que va por el aire ahora mismo (solo una a la vez).</summary>
        public static FichaArrastrable EnVuelo { get; private set; }
        /// <summary>La ultima que se solto bien: al repintar, esa se asienta con un rebote.</summary>
        public static string UltimaSoltada;
        /// <summary>Donde se solto (en el mundo): para la marca «−5 min».</summary>
        public Vector3 DondeSeSolto { get; private set; }

        private RectTransform _rt, _hueco;
        private Transform _padre;
        private ScrollRect _scroll;
        private bool _alScroll;
        private Vector3 _agarre;
        private Vector2 _inicio;
        private DestinoDeFicha _sobre;
        private CanvasGroup _grupo;
        private Shadow _sombra;
        private Quaternion _giro;

        public void OnPointerClick(PointerEventData e) {
            if (!e.dragging) AlPulsar?.Invoke();
        }

        public void OnBeginDrag(PointerEventData e) {
            _inicio = e.position;
            if (Fija || Capa == null || EnVuelo != null) {
                // Lo que no se coge deja que su lista se desplace, como si no estuviera.
                _scroll = GetComponentInParent<ScrollRect>();
                _alScroll = _scroll != null;
                if (_alScroll) _scroll.OnBeginDrag(e);
                return;
            }
            _rt = (RectTransform)transform;
            _padre = _rt.parent;
            var tam = _rt.rect.size;
            var centro = _rt.TransformPoint(_rt.rect.center);

            // El hueco que deja: del mismo alto, para que la lista no salte.
            _hueco = new GameObject("Hueco", typeof(RectTransform)).GetComponent<RectTransform>();
            _hueco.SetParent(_padre, false);
            _hueco.SetSiblingIndex(_rt.GetSiblingIndex());
            var le = _hueco.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = tam.y;
            le.preferredWidth = tam.x;
            var marca = _hueco.gameObject.AddComponent<Image>();
            marca.color = new Color(1, 1, 1, 0.05f);
            marca.raycastTarget = false;

            _giro = _rt.localRotation;
            _rt.SetParent(Capa, false);
            _rt.SetAsLastSibling();
            _rt.anchorMin = _rt.anchorMax = _rt.pivot = new Vector2(0.5f, 0.5f);
            _rt.sizeDelta = tam;
            _rt.position = centro;
            _rt.localRotation = Quaternion.Euler(0, 0, -3f);
            _rt.localScale = Vector3.one * 1.05f;

            _grupo = GetComponent<CanvasGroup>();
            if (_grupo == null) _grupo = gameObject.AddComponent<CanvasGroup>();
            _grupo.blocksRaycasts = false;
            _grupo.alpha = 0.96f;
            if (GetComponent<Graphic>() != null) {
                _sombra = gameObject.AddComponent<Shadow>();
                _sombra.effectDistance = new Vector2(10, -12);
                _sombra.effectColor = new Color(0, 0, 0, 0.45f);
            }

            Vector3 mundo;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(Capa, e.position, e.pressEventCamera, out mundo);
            _agarre = _rt.position - mundo;
            EnVuelo = this;
            DestinoDeFicha.EmpezarVuelo(this);
        }

        public void OnDrag(PointerEventData e) {
            if (_alScroll) { _scroll.OnDrag(e); return; }
            if (EnVuelo != this) return;
            Vector3 mundo;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(Capa, e.position, e.pressEventCamera, out mundo))
                _rt.position = mundo + _agarre;
            var bajo = e.pointerCurrentRaycast.gameObject;
            var destino = bajo == null ? null : bajo.GetComponentInParent<DestinoDeFicha>();
            if (destino != _sobre) {
                if (_sobre != null) _sobre.Salir();
                _sobre = destino;
                if (_sobre != null) _sobre.Entrar(this);
            }
            if (_sobre != null) _sobre.Sobrevolar(this, e.position, e.pressEventCamera);
        }

        public void OnEndDrag(PointerEventData e) {
            if (_alScroll) {
                _scroll.OnEndDrag(e);
                _alScroll = false;
                // Quien intento sacar una ficha fija (no un simple desplazamiento): que sepa por que no se mueve.
                if (Fija && !string.IsNullOrEmpty(PorQueFija) && Mathf.Abs(e.position.x - _inicio.x) > 60) {
                    Menear();
                    AlFallar?.Invoke(PorQueFija);
                }
                return;
            }
            if (EnVuelo != this) return;
            var destino = _sobre;
            var puesto = destino == null ? -1 : destino.Puesto(this, e.position, e.pressEventCamera);
            if (_sobre != null) _sobre.Salir();
            _sobre = null;
            DondeSeSolto = _rt.position;
            Volver();
            if (destino == null) return;
            var motivo = destino.Motivo == null ? null : destino.Motivo(this, puesto);
            if (motivo != null) {
                Menear();
                AlFallar?.Invoke(motivo);
                return;
            }
            UltimaSoltada = Id;
            destino.AlRecibir?.Invoke(this, puesto);
        }

        /// <summary>De vuelta a su hueco, como estaba.</summary>
        private void Volver() {
            EnVuelo = null;
            DestinoDeFicha.TerminarVuelo(this);
            if (_rt == null || _hueco == null) return;
            _rt.SetParent(_padre, false);
            _rt.SetSiblingIndex(_hueco.GetSiblingIndex());
            Destroy(_hueco.gameObject);
            _hueco = null;
            _rt.localRotation = _giro;
            _rt.localScale = Vector3.one;
            if (_grupo != null) { _grupo.alpha = 1; _grupo.blocksRaycasts = true; }
            if (_sombra != null) Destroy(_sombra);
        }

        public void Menear() {
            var m = GetComponent<MeneoDeFicha>();
            if (m == null) m = gameObject.AddComponent<MeneoDeFicha>();
            m.Empezar();
        }

        private void OnDisable() {
            if (EnVuelo != this) return;
            // Se repinto la pantalla con la ficha en el aire: que nada se quede a medias.
            EnVuelo = null;
            DestinoDeFicha.TerminarVuelo(this);
            if (_hueco != null) Destroy(_hueco.gameObject);
        }
    }

    /// <summary>
    /// Donde se puede soltar una ficha: un carril (con 'Lista', y entonces importa el puesto) o la cara de alguien.
    /// Mientras una ficha va por el aire, los destinos que no la aceptan se apagan; al pasar por encima, el que si
    /// la acepta se ilumina, enseña lo que costaria («−5 min») y, en un carril, la raya de donde caeria.
    /// </summary>
    public sealed class DestinoDeFicha : MonoBehaviour {
        public UiKit Ui;
        /// <summary>Las fichas del carril. null = no hay puesto (una cara).</summary>
        public RectTransform Lista;
        /// <summary>Por que no (null = si). 'puesto' es -1 cuando aun no se sabe donde caeria.</summary>
        public Func<FichaArrastrable, int, string> Motivo;
        public Func<FichaArrastrable, string> Coste;
        public Action<FichaArrastrable, int> AlRecibir;
        public Action<FichaArrastrable> AlEmpezarVuelo, AlTerminarVuelo;
        public Color Acento = Color.white;
        public float Radio = 14;
        /// <summary>Donde sale la etiqueta del coste: respecto al centro de arriba del destino.</summary>
        public Vector2 SitioDelChip = new Vector2(0, -6);

        private static readonly List<DestinoDeFicha> Activos = new List<DestinoDeFicha>();
        private CanvasGroup _grupo;
        private Image _resalte;
        private RectTransform _linea, _chip;
        private TMP_Text _textoChip;
        private Image _fondoChip;
        private bool _valido;

        private void OnEnable() { Activos.Add(this); }
        private void OnDisable() { Activos.Remove(this); }

        public static void EmpezarVuelo(FichaArrastrable f) { foreach (var d in Activos.ToList()) if (d != null) d.Empezar(f); }
        public static void TerminarVuelo(FichaArrastrable f) { foreach (var d in Activos.ToList()) if (d != null) d.Terminar(f); }

        private void Empezar(FichaArrastrable f) {
            _valido = Motivo == null || Motivo(f, -1) == null;
            if (_grupo == null) {
                _grupo = gameObject.GetComponent<CanvasGroup>();
                if (_grupo == null) _grupo = gameObject.AddComponent<CanvasGroup>();
            }
            _grupo.alpha = _valido ? 1f : 0.45f;
            Resaltar(_valido ? 0.35f : 0f);
            AlEmpezarVuelo?.Invoke(f);
        }

        private void Terminar(FichaArrastrable f) {
            if (_grupo != null) _grupo.alpha = 1;
            Resaltar(0);
            Ocultar();
            AlTerminarVuelo?.Invoke(f);
        }

        public void Entrar(FichaArrastrable f) {
            Resaltar(_valido ? 1f : 0.2f);
        }

        public void Salir() {
            Resaltar(_valido ? 0.35f : 0f);
            Ocultar();
        }

        public void Sobrevolar(FichaArrastrable f, Vector2 pantalla, Camera camara) {
            var puesto = Puesto(f, pantalla, camara);
            var motivo = Motivo == null ? null : Motivo(f, puesto);
            if (motivo == null) {
                MostrarChip(Coste == null ? null : Coste(f), Acento, Ui == null ? Color.black : Ui.Tema.bg950);
                if (Lista != null) MostrarLinea(f, puesto);
            } else {
                MostrarChip(motivo, Ui == null ? Color.red : Ui.Tema.danger, Color.white);
                if (_linea != null) _linea.gameObject.SetActive(false);
            }
        }

        /// <summary>En que puesto caeria: cuantas fichas (sin contar la que vuela) tienen el centro por encima del puntero.</summary>
        public int Puesto(FichaArrastrable f, Vector2 pantalla, Camera camara) {
            if (Lista == null) return -1;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Lista, pantalla, camara, out local);
            var n = 0;
            foreach (var hijo in Fichas(f)) if (Centro(hijo) > local.y) n++;
            return n;
        }

        private IEnumerable<RectTransform> Fichas(FichaArrastrable f) {
            for (var i = 0; i < Lista.childCount; i++) {
                var h = Lista.GetChild(i) as RectTransform;
                if (h == null || !h.gameObject.activeSelf) continue;
                var ficha = h.GetComponent<FichaArrastrable>();
                if (ficha == null || ficha == f) continue;
                yield return h;
            }
        }

        private float Centro(RectTransform hijo) { return Lista.InverseTransformPoint(hijo.TransformPoint(hijo.rect.center)).y; }

        private void MostrarLinea(FichaArrastrable f, int puesto) {
            if (_linea == null) {
                _linea = new GameObject("Raya", typeof(RectTransform)).GetComponent<RectTransform>();
                _linea.SetParent(Lista, false);
                _linea.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var img = _linea.gameObject.AddComponent<Image>();
                img.color = Acento;
                img.raycastTarget = false;
                _linea.anchorMin = new Vector2(0, 1);
                _linea.anchorMax = new Vector2(1, 1);
                _linea.pivot = new Vector2(0.5f, 0.5f);
                _linea.sizeDelta = new Vector2(-16, 4);
            }
            _linea.gameObject.SetActive(true);
            _linea.SetAsLastSibling();
            var fichas = Fichas(f).ToList();
            float y;
            if (fichas.Count == 0) y = Lista.rect.yMax - 10;
            else if (puesto < fichas.Count) {
                var h = fichas[puesto];
                y = Lista.InverseTransformPoint(h.TransformPoint(new Vector3(0, h.rect.yMax))).y + 4;
            } else {
                var h = fichas[fichas.Count - 1];
                y = Lista.InverseTransformPoint(h.TransformPoint(new Vector3(0, h.rect.yMin))).y - 4;
            }
            _linea.anchoredPosition = new Vector2(0, y - Lista.rect.yMax);
        }

        private void MostrarChip(string texto, Color fondo, Color tinta) {
            if (string.IsNullOrEmpty(texto) || Ui == null) { if (_chip != null) _chip.gameObject.SetActive(false); return; }
            if (_chip == null) {
                _chip = Ui.Chip(transform, texto, fondo, tinta);
                UiKit.Tamano(_chip).ignoreLayout = true;
                _chip.anchorMin = _chip.anchorMax = new Vector2(0.5f, 1);
                _chip.pivot = new Vector2(0.5f, 0);
                _chip.anchoredPosition = SitioDelChip;
                _textoChip = _chip.GetComponentInChildren<TMP_Text>();
                _fondoChip = _chip.GetComponent<Image>();
                var ajuste = _chip.gameObject.GetComponent<ContentSizeFitter>();
                if (ajuste == null) ajuste = _chip.gameObject.AddComponent<ContentSizeFitter>();
                ajuste.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            _chip.gameObject.SetActive(true);
            _chip.SetAsLastSibling();
            if (_textoChip != null) { _textoChip.text = texto; _textoChip.color = tinta; }
            if (_fondoChip != null) _fondoChip.color = fondo;
        }

        private void Ocultar() {
            if (_linea != null) _linea.gameObject.SetActive(false);
            if (_chip != null) _chip.gameObject.SetActive(false);
        }

        private void Resaltar(float fuerza) {
            if (_resalte == null) {
                if (fuerza <= 0 || Ui == null) return;
                _resalte = Ui.Borde((RectTransform)transform, Acento, Radio, 2.5f);
                _resalte.raycastTarget = false;
            }
            _resalte.color = NexusTheme.Alfa(Acento, 0.9f * fuerza);
        }
    }

    /// <summary>Un «no» con la ficha: unos grados a un lado y a otro, y vuelve a como estaba.</summary>
    public sealed class MeneoDeFicha : MonoBehaviour {
        private float _t = -1;
        private Quaternion _base;

        public void Empezar() {
            if (_t < 0) _base = transform.localRotation;
            _t = 0;
        }

        private void Update() {
            if (_t < 0) return;
            _t += Time.unscaledDeltaTime;
            const float dura = 0.42f;
            if (_t >= dura) { transform.localRotation = _base; _t = -1; return; }
            var amortigua = 1 - _t / dura;
            transform.localRotation = _base * Quaternion.Euler(0, 0, Mathf.Sin(_t * 48f) * 5f * amortigua);
        }
    }

    /// <summary>Una ficha que acaba de caer en su sitio: un poco grande, y se asienta con un rebote.</summary>
    public sealed class AsientoDeFicha : MonoBehaviour {
        private float _t;

        private void Update() {
            _t += Time.unscaledDeltaTime;
            var p = Mathf.Clamp01(_t / 0.35f);
            transform.localScale = Vector3.one * Mathf.LerpUnclamped(1.1f, 1f, ContextoDeHallazgo.Rebote(p));
            if (p >= 1) { transform.localScale = Vector3.one; Destroy(this); }
        }
    }
}
