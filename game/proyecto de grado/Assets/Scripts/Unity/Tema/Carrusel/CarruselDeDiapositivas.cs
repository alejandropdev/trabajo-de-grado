using System;
using System.Collections.Generic;
using Nexus.Unity.Aplicacion;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// Un carrusel de pantallas completas (2026-11-06, Fase 2): una diapositiva a la vez, flechas grandes a los lados,
    /// puntos con el nombre de cada una abajo, y ←/→ (o A/D) en el teclado.
    ///
    /// La transicion es llamativa a proposito: la que se va se desliza, se encoge y se apaga; la que llega entra desde su
    /// lado con un pequeño rebote, crece de 0,94 a 1 y su titulo destella. Cada diapositiva se pinta la primera vez que
    /// se ve y se repinta solo cuando su Clave cambia (como los paneles del dia), no cada fotograma.
    /// </summary>
    public sealed class CarruselDeDiapositivas : MonoBehaviour {
        public sealed class Diapositiva {
            public string Id;
            public string Nombre;
            /// <summary>Pinta el contenido en el cuerpo (ya vacio).</summary>
            public Action<RectTransform> Pintar;
            /// <summary>Cuando cambia, se repinta. Null = se pinta una vez.</summary>
            public Func<string> Clave;
        }

        private sealed class Pagina {
            public Diapositiva Def;
            public RectTransform Rt, Cuerpo, Titulo;
            public CanvasGroup Grupo;
            public string Clave;
            public bool Pintada;
        }

        private const float Duracion = 0.36f, MargenLateral = 96f;

        /// <summary>Alto de la tira de puntos con el nombre de cada diapositiva (Indicadores, Burndown, Tablero…).</summary>
        public const float AltoDeLosPuntos = 54f;

        /// <summary>Mientras diga true (un momento del dia encima, una bandeja abierta), no se cambia de diapositiva.</summary>
        public Func<bool> Bloqueado;
        /// <summary>Se avisa al llegar a otra diapositiva (por su id).</summary>
        public Action<string> AlCambiar;

        private UiKit _ui;
        private NexusTheme _tema;
        private RectTransform _escena, _puntos;
        private readonly List<Pagina> _paginas = new List<Pagina>();
        private readonly List<Image> _fondosDePunto = new List<Image>();
        private readonly List<TMP_Text> _textosDePunto = new List<TMP_Text>();
        private int _actual, _anterior = -1, _direccion;
        private float _t = 1f, _siguienteRevision;

        public string Actual { get { return _paginas.Count == 0 ? null : _paginas[_actual].Def.Id; } }

        /// <summary>
        /// 'bajoElPadre' > 0: el carrusel se estira esa cantidad POR DEBAJO de su padre (hasta el borde de la pantalla) y
        /// los puntos con nombre se pegan alli, abajo del todo; el contenido sigue acabando donde acaba el padre, y el
        /// hueco entre los puntos y el contenido queda libre para quien lo quiera usar (la Fase 2 pone ahi «M» y «Producto»).
        /// </summary>
        public static CarruselDeDiapositivas Crear(UiKit ui, RectTransform padre, IList<Diapositiva> diapositivas, string inicial = null,
                                                   float bajoElPadre = 0f) {
            var rt = ui.Nodo(padre, "Carrusel");
            UiKit.Rellenar(rt);
            if (bajoElPadre > 0) rt.offsetMin = new Vector2(0, -bajoElPadre);
            var c = rt.gameObject.AddComponent<CarruselDeDiapositivas>();
            c.Construir(ui, rt, diapositivas, inicial, bajoElPadre);
            return c;
        }

        private void Construir(UiKit ui, RectTransform rt, IList<Diapositiva> diapositivas, string inicial, float bajoElPadre) {
            _ui = ui;
            _tema = ui.Tema;

            // Donde empieza el contenido, medido desde el borde de abajo del carrusel.
            var abajo = bajoElPadre > 0 ? bajoElPadre : AltoDeLosPuntos + 6;
            _escena = ui.Nodo(rt, "Escena");
            _escena.anchorMin = Vector2.zero;
            _escena.anchorMax = Vector2.one;
            _escena.offsetMin = new Vector2(MargenLateral, abajo);
            _escena.offsetMax = new Vector2(-MargenLateral, 0);

            foreach (var d in diapositivas) _paginas.Add(CrearPagina(d));

            // Las flechas, grandes y a los lados (a media altura del contenido); y los puntos con nombre, abajo. Son
            // navegacion del contenido: no cuentan para la regla de los siete botones.
            var alturaDeFlechas = bajoElPadre > 0 ? abajo / 2f : AltoDeLosPuntos / 2f;
            Flecha(rt, "<", 0f, () => Mover(-1), alturaDeFlechas).gameObject.AddComponent<ListaDeElementos>();
            Flecha(rt, ">", 1f, () => Mover(+1), alturaDeFlechas).gameObject.AddComponent<ListaDeElementos>();

            _puntos = ui.Fila(rt, "Puntos", 8, alineacion: TextAnchor.MiddleCenter);
            _puntos.anchorMin = new Vector2(0, 0);
            _puntos.anchorMax = new Vector2(1, 0);
            _puntos.pivot = new Vector2(0.5f, 0);
            _puntos.sizeDelta = new Vector2(0, AltoDeLosPuntos);
            _puntos.anchoredPosition = new Vector2(0, bajoElPadre > 0 ? 6 : 0);   // con un poco de aire sobre el borde de la pantalla
            _puntos.gameObject.AddComponent<ListaDeElementos>();
            for (var i = 0; i < _paginas.Count; i++) CrearPunto(i);

            var indice = 0;
            if (inicial != null) {
                var k = _paginas.FindIndex(p => p.Def.Id == inicial);
                if (k >= 0) indice = k;
            }
            _actual = indice;
            for (var i = 0; i < _paginas.Count; i++) _paginas[i].Rt.gameObject.SetActive(i == _actual);
            if (_paginas.Count > 0) PintarSiHaceFalta(_paginas[_actual], true);
            PintarPuntos();
        }

        private Pagina CrearPagina(Diapositiva d) {
            var ui = _ui;
            var rt = ui.Nodo(_escena, "Diapositiva " + d.Id);
            UiKit.Rellenar(rt);
            var p = new Pagina { Def = d, Rt = rt, Grupo = rt.gameObject.AddComponent<CanvasGroup>() };

            // El titulo, grande, arriba a la izquierda (destella al llegar).
            p.Titulo = ui.Nodo(rt, "Titulo");
            p.Titulo.anchorMin = p.Titulo.anchorMax = p.Titulo.pivot = new Vector2(0, 1);
            p.Titulo.anchoredPosition = new Vector2(8, -2);
            p.Titulo.sizeDelta = new Vector2(600, 46);
            var texto = ui.Texto(p.Titulo, (d.Nombre ?? d.Id).ToUpperInvariant(), EstiloTexto.Subtitulo, _tema.cyan);
            texto.characterSpacing = 4;
            texto.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Rellenar(texto.rectTransform);

            p.Cuerpo = ui.Nodo(rt, "Cuerpo");
            p.Cuerpo.anchorMin = Vector2.zero;
            p.Cuerpo.anchorMax = Vector2.one;
            p.Cuerpo.offsetMin = Vector2.zero;
            p.Cuerpo.offsetMax = new Vector2(0, -52);
            // El contenido de una diapositiva es contenido: sus botones no cuentan para la regla de los siete.
            p.Cuerpo.gameObject.AddComponent<ListaDeElementos>();
            return p;
        }

        private RectTransform Flecha(RectTransform padre, string simbolo, float lado, Action alPulsar, float altura) {
            var rt = _ui.Nodo(padre, "Flecha " + simbolo);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(lado, 0.5f);
            rt.sizeDelta = new Vector2(76, 160);
            rt.anchoredPosition = new Vector2(lado < 0.5f ? 8 : -8, altura);
            var img = rt.gameObject.AddComponent<Image>();
            _ui.Fondo(img, NexusTheme.Alfa(_tema.bg950, 0.35f), NexusTheme.RadioLg);
            var boton = rt.gameObject.AddComponent<Button>();
            boton.transition = Selectable.Transition.None;
            boton.onClick.AddListener(() => alPulsar());
            var t = _ui.Texto(rt, simbolo, EstiloTexto.Hero, _tema.ink, TextAlignmentOptions.Center);
            t.fontSize = 72;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            var alto = t.GetComponent<AltoDeTexto>();
            if (alto != null) Destroy(alto);
            UiKit.Rellenar(t.rectTransform);
            rt.gameObject.AddComponent<FlechaViva>().Configurar(t, _tema);
            return rt;
        }

        private void CrearPunto(int i) {
            var p = _paginas[i];
            var rt = _ui.Nodo(_puntos, "Punto " + p.Def.Id);
            var img = rt.gameObject.AddComponent<Image>();
            _ui.Fondo(img, _tema.surface, 14);
            var boton = rt.gameObject.AddComponent<Button>();
            boton.transition = Selectable.Transition.None;
            var indice = i;
            boton.onClick.AddListener(() => IrAIndice(indice));
            var grupo = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            grupo.padding = new RectOffset(14, 14, 6, 6);
            grupo.childAlignment = TextAnchor.MiddleCenter;
            grupo.childControlWidth = grupo.childControlHeight = true;
            var t = _ui.Texto(rt, p.Def.Nombre ?? p.Def.Id, EstiloTexto.Leyenda, _tema.inkMuted, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            _fondosDePunto.Add(img);
            _textosDePunto.Add(t);
        }

        /// <summary>El punto de una diapositiva: la guia lo señala cuando lo que pide esta en otra diapositiva.</summary>
        public RectTransform PuntoDe(string id) {
            var i = _paginas.FindIndex(p => p.Def.Id == id);
            return i < 0 ? null : (RectTransform)_fondosDePunto[i].transform;
        }

        // ==================================================================== moverse

        public void Mover(int direccion) {
            if (_paginas.Count < 2) return;
            IrAIndice((_actual + direccion + _paginas.Count) % _paginas.Count, direccion);
        }

        public void IrA(string id) {
            var i = _paginas.FindIndex(p => p.Def.Id == id);
            if (i >= 0) IrAIndice(i);
        }

        private void IrAIndice(int i, int direccion = 0) {
            if (i == _actual || i < 0 || i >= _paginas.Count) return;
            if (Bloqueado != null && Bloqueado()) return;
            if (_anterior >= 0) {
                // Una transicion a medias se cierra antes de empezar otra.
                var vieja = _paginas[_anterior];
                vieja.Rt.gameObject.SetActive(false);
                vieja.Rt.anchoredPosition = Vector2.zero;
                vieja.Rt.localScale = Vector3.one;
                vieja.Grupo.alpha = 1;
            }
            _anterior = _actual;
            _actual = i;
            _direccion = direccion != 0 ? direccion : (i > _anterior ? 1 : -1);
            _t = 0;
            var p = _paginas[_actual];
            p.Rt.gameObject.SetActive(true);
            p.Rt.SetAsLastSibling();
            PintarSiHaceFalta(p, true);
            PintarPuntos();
            AlCambiar?.Invoke(p.Def.Id);
        }

        /// <summary>Repinta la diapositiva que se ve (aunque su Clave no haya cambiado).</summary>
        public void Repintar() {
            if (_paginas.Count > 0) PintarSiHaceFalta(_paginas[_actual], true);
        }

        private void PintarSiHaceFalta(Pagina p, bool forzar) {
            var clave = p.Def.Clave != null ? p.Def.Clave() : null;
            if (!forzar && p.Pintada && (p.Def.Clave == null || clave == p.Clave)) return;
            p.Clave = clave;
            p.Pintada = true;
            UiKit.Vaciar(p.Cuerpo);
            try {
                p.Def.Pintar?.Invoke(p.Cuerpo);
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        private void PintarPuntos() {
            for (var i = 0; i < _fondosDePunto.Count; i++) {
                var actual = i == _actual;
                _fondosDePunto[i].color = actual ? _tema.cyan : NexusTheme.Alfa(_tema.surface, 0.9f);
                _textosDePunto[i].color = actual ? _tema.bg950 : _tema.inkMuted;
            }
        }

        // ==================================================================== cada fotograma

        private void Update() {
            var dt = Time.unscaledDeltaTime;
            Teclado();
            if (_t < 1f) Animar(dt);
            // La que se ve, se repinta si su clave cambio (cuatro veces por segundo basta).
            if (Time.unscaledTime >= _siguienteRevision && _paginas.Count > 0) {
                _siguienteRevision = Time.unscaledTime + 0.25f;
                PintarSiHaceFalta(_paginas[_actual], false);
            }
        }

        private void Teclado() {
            var teclado = Keyboard.current;
            if (teclado == null || _paginas.Count < 2) return;
            if (ManoDeCartas.PuedeEscuchar != null && !ManoDeCartas.PuedeEscuchar(transform)) return;
            if (Bloqueado != null && Bloqueado()) return;
            // Con una mano de cartas a la vista, las flechas son suyas.
            if (GetComponentInChildren<ManoDeCartas>() != null) return;
            if (teclado.leftArrowKey.wasPressedThisFrame || teclado.aKey.wasPressedThisFrame) Mover(-1);
            else if (teclado.rightArrowKey.wasPressedThisFrame || teclado.dKey.wasPressedThisFrame) Mover(+1);
        }

        private void Animar(float dt) {
            _t = Mathf.Min(1f, _t + dt / Duracion);
            var ancho = Mathf.Max(400f, _escena.rect.width);
            var entra = _paginas[_actual];
            // La que llega: desde su lado, con un pequeño rebote (ease-out-back), creciendo y apareciendo.
            var e = EaseOutBack(_t);
            entra.Rt.anchoredPosition = new Vector2(_direccion * ancho * 0.35f * (1 - e), 0);
            entra.Rt.localScale = Vector3.one * Mathf.LerpUnclamped(0.94f, 1f, e);
            entra.Grupo.alpha = Mathf.Clamp01(_t * 1.6f);
            // El titulo destella al llegar.
            entra.Titulo.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(_t * Mathf.PI));

            if (_anterior >= 0) {
                var sale = _paginas[_anterior];
                var s = 1 - Mathf.Pow(1 - _t, 3);
                sale.Rt.anchoredPosition = new Vector2(-_direccion * ancho * 0.35f * s, 0);
                sale.Rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.92f, s);
                sale.Grupo.alpha = 1 - Mathf.Clamp01(_t * 1.4f);
                if (_t >= 1f) {
                    sale.Rt.gameObject.SetActive(false);
                    sale.Rt.anchoredPosition = Vector2.zero;
                    sale.Rt.localScale = Vector3.one;
                    sale.Grupo.alpha = 1;
                    _anterior = -1;
                }
            }
            if (_t >= 1f) {
                entra.Rt.anchoredPosition = Vector2.zero;
                entra.Rt.localScale = Vector3.one;
                entra.Titulo.localScale = Vector3.one;
                entra.Grupo.alpha = 1;
            }
        }

        private static float EaseOutBack(float x) {
            const float c1 = 1.70158f, c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2);
        }

        /// <summary>La flecha crece y se ilumina al pasar el raton.</summary>
        private sealed class FlechaViva : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
            private TMP_Text _texto;
            private NexusTheme _tema;
            private bool _encima;
            private float _f;

            public void Configurar(TMP_Text texto, NexusTheme tema) { _texto = texto; _tema = tema; }
            public void OnPointerEnter(PointerEventData e) { _encima = true; }
            public void OnPointerExit(PointerEventData e) { _encima = false; }

            private void Update() {
                _f = Mathf.MoveTowards(_f, _encima ? 1 : 0, Time.unscaledDeltaTime / 0.15f);
                transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.12f, _f);
                if (_texto != null) _texto.color = Color.Lerp(_tema.ink, _tema.cyan, _f);
            }
        }
    }

    /// <summary>
    /// Una pieza de una diapositiva que responde: al pasar el raton crece y enseña lo que tenia escondido (el efecto de
    /// un KPI, una ayuda); al pulsarla, hace lo suyo. Es lo que permite que en pantalla haya poco texto: el detalle
    /// aparece solo cuando se mira.
    /// </summary>
    public sealed class PiezaViva : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
        public GameObject Revelar;
        public GameObject Ocultar;
        public Action AlPulsar;
        public float Escala = 1.06f;
        private bool _encima;
        private float _f;

        public void OnPointerEnter(PointerEventData e) { _encima = true; }
        public void OnPointerExit(PointerEventData e) { _encima = false; }
        public void OnPointerClick(PointerEventData e) { AlPulsar?.Invoke(); }

        private void OnDisable() { _encima = false; _f = 0; transform.localScale = Vector3.one; }

        private void Update() {
            _f = Mathf.MoveTowards(_f, _encima ? 1 : 0, Time.unscaledDeltaTime / 0.16f);
            var e = 1 - (1 - _f) * (1 - _f);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, Escala, e);
            var ver = _f > 0.5f;
            if (Revelar != null && Revelar.activeSelf != ver) Revelar.SetActive(ver);
            if (Ocultar != null && Ocultar.activeSelf == ver) Ocultar.SetActive(!ver);
        }
    }
}
