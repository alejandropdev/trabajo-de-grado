using System;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>Como se ve un bloque del pilar.</summary>
    public enum VistaDeBloque {
        /// <summary>Esperando: cristal oscuro con el filo del color de su modulo.</summary>
        Normal,
        /// <summary>El que se ha pulsado (se mueve con ▲▼).</summary>
        Elegido,
        /// <summary>La luz de «Verificar» ha pasado por el: bien.</summary>
        Bien,
        /// <summary>La primera historia que falla: roja y agrietada.</summary>
        Mal,
        /// <summary>Por encima de un fallo: la luz no llego hasta aqui.</summary>
        Apagado,
        /// <summary>Su pilar ya esta verificado: verde y con candado, ya no se mueve.</summary>
        Hecho
    }

    /// <summary>
    /// Un bloque de historia del puente (2026-11-10): una tarjeta de cristal con el titulo, la franja de color de su
    /// modulo y un icono a la derecha (el agarre, el check, la cruz o el candado). Se arrastra solo en vertical; el
    /// pilar decide donde cae (AlSoltar) y recoloca a los demas. Los colores cambian suavemente hacia el estado
    /// pedido, y un destello de luz verde o roja recorre el bloque al verificar.
    /// </summary>
    public sealed class BloqueDeHistoria : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {
        public string Id;
        public Color ColorDelModulo = Color.cyan;
        public bool PuedeArrastrar = true;
        /// <summary>Un clic sin arrastrar: el bloque queda elegido y se mueve con ▲▼.</summary>
        public Action<BloqueDeHistoria> AlPulsar;
        public Action<BloqueDeHistoria> AlEmpezar;
        /// <summary>El bloque se arrastra: la y (hacia abajo, coordenadas de la lamina) de su centro.</summary>
        public Action<BloqueDeHistoria, float> AlArrastrar;
        public Action<BloqueDeHistoria, float> AlSoltar;
        public Action<BloqueDeHistoria, bool> AlPasarElRaton;

        public RectTransform Rt { get; private set; }
        public VistaDeBloque Vista { get; private set; } = VistaDeBloque.Normal;
        public bool Arrastrando { get; private set; }
        /// <summary>Cada movimiento anima con el numero de version: si llega otro, el anterior se detiene.</summary>
        public int Version;

        private NexusTheme _t;
        private Image _fondo, _borde, _franja, _resplandor;
        private TMP_Text _titulo, _necesita;
        private DibujoUI _icono, _grietas;
        private Color _cFondo, _cBorde, _cFranja;
        private Color _tFondo, _tBorde, _tFranja;
        private float _destello;
        private Color _colorDestello;
        private float _alzado, _alzadoMeta;
        private float _arrastreY0, _punteroY0;

        // ---------------------------------------------------------------- creacion

        public static BloqueDeHistoria Crear(UiKit ui, RectTransform padre, string id, string titulo, Color modulo, float ancho, float alto) {
            var rt = ui.Nodo(padre, "Bloque " + id);
            Lamina.Colocar(rt, 0, 0, ancho, alto);
            var t = ui.Tema;
            var b = rt.gameObject.AddComponent<BloqueDeHistoria>();
            b.Id = id; b.ColorDelModulo = modulo; b.Rt = rt; b._t = t;

            // El resplandor (detras): un brillo suave del tamaño del bloque y un poco mas.
            var res = ui.Nodo(rt, "Resplandor");
            res.anchorMin = res.anchorMax = res.pivot = new Vector2(0.5f, 0.5f);
            res.anchoredPosition = Vector2.zero;
            res.sizeDelta = new Vector2(ancho * 1.35f, alto * 1.9f);
            b._resplandor = res.gameObject.AddComponent<Image>();
            b._resplandor.sprite = PlanoIlustrado.Brillo();
            b._resplandor.color = Color.clear;
            b._resplandor.raycastTarget = false;

            // El cuerpo de cristal.
            var fondo = ui.Nodo(rt, "Cuerpo");
            UiKit.Rellenar(fondo);
            b._fondo = fondo.gameObject.AddComponent<Image>();
            ui.Fondo(b._fondo, NexusTheme.Alfa(NexusTheme.Alfa(Color.black, 1f), 0.9f), 12);
            b._fondo.raycastTarget = true;
            b._borde = ui.Borde(fondo, modulo, 12, 2f);

            // Un reflejo arriba (le da volumen de cubo) y la franja de color a la izquierda.
            var reflejo = ui.Nodo(fondo, "Reflejo");
            reflejo.anchorMin = new Vector2(0, 1); reflejo.anchorMax = new Vector2(1, 1); reflejo.pivot = new Vector2(0.5f, 1);
            reflejo.offsetMin = new Vector2(10, -alto * 0.34f); reflejo.offsetMax = new Vector2(-10, -4);
            var rImg = reflejo.gameObject.AddComponent<Image>();
            rImg.color = new Color(1, 1, 1, 0.045f);
            rImg.raycastTarget = false;
            var franja = ui.Nodo(fondo, "Franja");
            franja.anchorMin = new Vector2(0, 0); franja.anchorMax = new Vector2(0, 1); franja.pivot = new Vector2(0, 0.5f);
            franja.sizeDelta = new Vector2(9, -18);
            franja.anchoredPosition = new Vector2(8, 0);
            b._franja = franja.gameObject.AddComponent<Image>();
            b._franja.raycastTarget = false;

            // El titulo (con letra que se ajusta) y, debajo, lo que necesita (a veces).
            b._titulo = ui.Texto(fondo, titulo, EstiloTexto.Cuerpo, t.ink, TextAlignmentOptions.MidlineLeft);
            var at = b._titulo.GetComponent<AltoDeTexto>();
            if (at != null) UnityEngine.Object.Destroy(at);
            b._titulo.enableAutoSizing = true; b._titulo.fontSizeMin = 14; b._titulo.fontSizeMax = 23;
            b._titulo.textWrappingMode = TextWrappingModes.Normal;
            b._titulo.fontStyle = FontStyles.Bold;
            b._titulo.raycastTarget = false;
            var tr = b._titulo.rectTransform;
            tr.anchorMin = new Vector2(0, 0); tr.anchorMax = new Vector2(1, 1);
            tr.offsetMin = new Vector2(28, alto * 0.30f); tr.offsetMax = new Vector2(-64, -6);
            b._necesita = ui.Texto(fondo, "", EstiloTexto.Pequeno, t.warning, TextAlignmentOptions.MidlineLeft);
            var an = b._necesita.GetComponent<AltoDeTexto>();
            if (an != null) UnityEngine.Object.Destroy(an);
            b._necesita.enableAutoSizing = true; b._necesita.fontSizeMin = 11; b._necesita.fontSizeMax = 15;
            b._necesita.textWrappingMode = TextWrappingModes.NoWrap;
            b._necesita.overflowMode = TextOverflowModes.Ellipsis;
            b._necesita.raycastTarget = false;
            var nr = b._necesita.rectTransform;
            nr.anchorMin = new Vector2(0, 0); nr.anchorMax = new Vector2(1, 0);
            nr.pivot = new Vector2(0.5f, 0);
            nr.offsetMin = new Vector2(28, 5); nr.offsetMax = new Vector2(-64, alto * 0.30f);

            // El icono de la derecha y las grietas (encima de todo).
            var ico = ui.Nodo(fondo, "Icono");
            ico.anchorMin = ico.anchorMax = new Vector2(1, 0.5f);
            ico.pivot = new Vector2(1, 0.5f);
            ico.sizeDelta = new Vector2(48, 48);
            ico.anchoredPosition = new Vector2(-8, 0);
            ico.gameObject.AddComponent<CanvasRenderer>();
            b._icono = ico.gameObject.AddComponent<DibujoUI>();
            b._icono.raycastTarget = false;
            var gri = ui.Nodo(fondo, "Grietas");
            UiKit.Rellenar(gri);
            gri.gameObject.AddComponent<CanvasRenderer>();
            b._grietas = gri.gameObject.AddComponent<DibujoUI>();
            b._grietas.raycastTarget = false;

            b._cFondo = b._tFondo = b.ColorDeFondo(VistaDeBloque.Normal, modulo);
            b._cBorde = b._tBorde = b.ColorDeBorde(VistaDeBloque.Normal, modulo);
            b._cFranja = b._tFranja = modulo;
            b.Aplicar();
            b.PintarIcono();
            return b;
        }

        // ---------------------------------------------------------------- estados

        private Color ColorDeFondo(VistaDeBloque v, Color modulo) {
            switch (v) {
                case VistaDeBloque.Elegido: return Color.Lerp(new Color(0.03f, 0.06f, 0.09f, 0.94f), modulo, 0.16f);
                case VistaDeBloque.Bien: return Color.Lerp(new Color(0.03f, 0.08f, 0.07f, 0.94f), _t.success, 0.2f);
                case VistaDeBloque.Hecho: return Color.Lerp(new Color(0.03f, 0.08f, 0.07f, 0.94f), _t.success, 0.14f);
                case VistaDeBloque.Mal: return Color.Lerp(new Color(0.09f, 0.03f, 0.04f, 0.95f), _t.danger, 0.24f);
                case VistaDeBloque.Apagado: return new Color(0.02f, 0.03f, 0.05f, 0.82f);
                default: return new Color(0.03f, 0.06f, 0.09f, 0.9f);
            }
        }

        private Color ColorDeBorde(VistaDeBloque v, Color modulo) {
            switch (v) {
                case VistaDeBloque.Elegido: return Color.Lerp(modulo, Color.white, 0.35f);
                case VistaDeBloque.Bien: case VistaDeBloque.Hecho: return _t.success;
                case VistaDeBloque.Mal: return _t.danger;
                case VistaDeBloque.Apagado: return NexusTheme.Alfa(modulo, 0.22f);
                default: return NexusTheme.Alfa(modulo, 0.62f);
            }
        }

        public void Poner(VistaDeBloque vista, bool inmediato = false) {
            Vista = vista;
            _tFondo = ColorDeFondo(vista, ColorDelModulo);
            _tBorde = ColorDeBorde(vista, ColorDelModulo);
            _tFranja = vista == VistaDeBloque.Bien || vista == VistaDeBloque.Hecho ? _t.success : vista == VistaDeBloque.Mal ? _t.danger
                     : vista == VistaDeBloque.Apagado ? NexusTheme.Alfa(ColorDelModulo, 0.3f) : ColorDelModulo;
            if (inmediato) { _cFondo = _tFondo; _cBorde = _tBorde; _cFranja = _tFranja; Aplicar(); }
            _titulo.color = vista == VistaDeBloque.Apagado ? NexusTheme.Alfa(_t.ink, 0.45f) : _t.ink;
            PintarIcono();
            if (vista != VistaDeBloque.Mal) { _grietas.Limpiar(); _grietas.Avance = 1f; }
        }

        /// <summary>Lo que necesita este bloque, en una linea pequeña («necesita: Formulario de visita»). Vacio = nada.</summary>
        public void MostrarNecesidades(string texto) {
            _necesita.text = texto ?? "";
        }

        /// <summary>Un destello de luz de ese color que se apaga solo (la luz de «Verificar» al pasar).</summary>
        public void Destello(Color color) { _destello = 1f; _colorDestello = color; }

        public DibujoUI Grietas { get { return _grietas; } }

        /// <summary>Dibuja las grietas (empiezan sin trazar: se animan con Grietas.Avance de 0 a 1).</summary>
        public void DibujarGrietas() {
            var tam = Rt.sizeDelta;
            _grietas.Limpiar();
            var rojo = NexusTheme.Alfa(Color.Lerp(_t.danger, Color.white, 0.35f), 0.95f);
            _grietas.Polilinea(new System.Collections.Generic.List<Vector2> {
                new Vector2(tam.x * 0.52f, 2), new Vector2(tam.x * 0.47f, tam.y * 0.22f), new Vector2(tam.x * 0.56f, tam.y * 0.4f),
                new Vector2(tam.x * 0.45f, tam.y * 0.62f), new Vector2(tam.x * 0.53f, tam.y * 0.8f), new Vector2(tam.x * 0.49f, tam.y - 2) }, rojo, 3.2f);
            _grietas.Polilinea(new System.Collections.Generic.List<Vector2> {
                new Vector2(tam.x * 0.56f, tam.y * 0.4f), new Vector2(tam.x * 0.68f, tam.y * 0.46f), new Vector2(tam.x * 0.76f, tam.y * 0.38f) }, rojo, 2.4f);
            _grietas.Polilinea(new System.Collections.Generic.List<Vector2> {
                new Vector2(tam.x * 0.45f, tam.y * 0.62f), new Vector2(tam.x * 0.33f, tam.y * 0.7f), new Vector2(tam.x * 0.27f, tam.y * 0.6f) }, rojo, 2.4f);
            _grietas.Avance = 0f;
        }

        private void PintarIcono() {
            _icono.Limpiar();
            switch (Vista) {
                case VistaDeBloque.Bien: _icono.Check(24, 26, _t.success); break;
                case VistaDeBloque.Mal: _icono.Cruz(24, 24, _t.danger, 12); break;
                case VistaDeBloque.Hecho: DibujosDelPuente.Candado(_icono, 24, 24, _t.success, 1f); break;
                case VistaDeBloque.Apagado: break;
                default:
                    // El agarre: tres rayitas (se puede arrastrar).
                    if (PuedeArrastrar) for (var k = 0; k < 3; k++) _icono.Linea(10, 15 + k * 9, 38, 15 + k * 9, NexusTheme.Alfa(_t.ink, Vista == VistaDeBloque.Elegido ? 0.85f : 0.4f), 3f);
                    break;
            }
        }

        public void RefrescarIcono() { PintarIcono(); }

        // ---------------------------------------------------------------- vida propia

        private void Aplicar() {
            _fondo.color = _cFondo; _borde.color = _cBorde; _franja.color = _cFranja;
        }

        private void Update() {
            var dt = Time.unscaledDeltaTime;
            var k = 1f - Mathf.Exp(-14f * dt);
            _cFondo = Color.Lerp(_cFondo, _tFondo, k);
            _cBorde = Color.Lerp(_cBorde, _tBorde, k);
            _cFranja = Color.Lerp(_cFranja, _tFranja, k);
            Aplicar();
            _destello = Mathf.MoveTowards(_destello, 0f, dt * 1.7f);
            var luz = _destello * 0.55f;
            if (Vista == VistaDeBloque.Elegido) luz = Mathf.Max(luz, 0.16f + 0.05f * Mathf.Sin(Time.unscaledTime * 5f));
            _resplandor.color = new Color(_colorDestello.r, _colorDestello.g, _colorDestello.b, luz);
            if (Vista == VistaDeBloque.Elegido && _destello <= 0f)
                _resplandor.color = NexusTheme.Alfa(Color.Lerp(ColorDelModulo, Color.white, 0.2f), luz);
            _alzado = Mathf.Lerp(_alzado, Arrastrando ? 1f : _alzadoMeta, 1f - Mathf.Exp(-16f * dt));
            Rt.localScale = Vector3.one * (1f + 0.035f * _alzado);
        }

        // ---------------------------------------------------------------- raton

        public void OnPointerClick(PointerEventData e) {
            if (!e.dragging) AlPulsar?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData e) { _alzadoMeta = PuedeArrastrar ? 0.5f : 0f; AlPasarElRaton?.Invoke(this, true); }
        public void OnPointerExit(PointerEventData e) { _alzadoMeta = 0f; AlPasarElRaton?.Invoke(this, false); }

        public void OnBeginDrag(PointerEventData e) {
            if (!PuedeArrastrar) return;
            Arrastrando = true;
            Rt.SetAsLastSibling();
            Rt.localRotation = Quaternion.Euler(0, 0, 1.4f);
            _arrastreY0 = Rt.anchoredPosition.y;
            var padre = (RectTransform)Rt.parent;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, e.position, e.pressEventCamera, out local);
            _punteroY0 = local.y;
            AlEmpezar?.Invoke(this);
        }

        public void OnDrag(PointerEventData e) {
            if (!Arrastrando) return;
            var padre = (RectTransform)Rt.parent;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, e.position, e.pressEventCamera, out local);
            var p = Rt.anchoredPosition;
            p.y = _arrastreY0 + (local.y - _punteroY0);
            Rt.anchoredPosition = p;
            AlArrastrar?.Invoke(this, -p.y + Rt.sizeDelta.y / 2f);
        }

        public void OnEndDrag(PointerEventData e) {
            if (!Arrastrando) return;
            Arrastrando = false;
            Rt.localRotation = Quaternion.identity;
            AlSoltar?.Invoke(this, -Rt.anchoredPosition.y + Rt.sizeDelta.y / 2f);
        }

        /// <summary>Cancela un arrastre en curso (por ejemplo, si el pilar se bloquea a mitad).</summary>
        public void SoltarYa() { Arrastrando = false; if (Rt != null) Rt.localRotation = Quaternion.identity; }
    }
}
