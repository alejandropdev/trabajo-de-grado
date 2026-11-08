using System;
using System.Collections.Generic;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>Una fase de una escena animada: su pestaña, su pie (una linea), cuanto dura y como se dibuja y se mueve.</summary>
    public sealed class FaseDeEscena {
        public string Pestana;
        public string Pie;
        public float Duracion = 5f;
        /// <summary>Dibuja la base (con tiza, se traza sola) y crea los actores; devuelve lo que los mueve: f(t en segundos).</summary>
        public Func<Tizador, ActoresDeEscena, Action<float>> Preparar;
    }

    /// <summary>
    /// Las piezas que se MUEVEN sobre la lamina (fichas, gotas, tarjetas, rotulos que cambian). El dibujo de tiza es
    /// estatico (se traza una vez); lo que va encima son piezas de interfaz, colocadas en las coordenadas de la lamina
    /// (600×300, desde arriba a la izquierda), que se recolocan cada fotograma.
    /// </summary>
    public sealed class ActoresDeEscena {
        private readonly UiKit _ui;
        private readonly Lamina _lamina;
        internal ActoresDeEscena(UiKit ui, Lamina lamina) { _ui = ui; _lamina = lamina; }

        public NexusTheme Tema { get { return _ui.Tema; } }

        /// <summary>Una ficha (redonda o rectangular) de w×h, con texto opcional.</summary>
        public RectTransform Ficha(float w, float h, Color color, bool redonda = false, string texto = null, Color? colorTexto = null) {
            var rt = _ui.Nodo(_lamina.Raiz, "Ficha");
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            var img = rt.gameObject.AddComponent<Image>();
            if (redonda) img.sprite = _ui.SpriteCircular();
            else { img.sprite = Formas.Caja(Mathf.Min(w, h) * 0.22f); img.type = Image.Type.Sliced; }
            img.color = color;
            img.raycastTarget = false;
            if (!string.IsNullOrEmpty(texto)) {
                var t = _ui.Texto(rt, texto, EstiloTexto.Pequeno, colorTexto ?? Tizador.Pizarra, TextAlignmentOptions.Center);
                t.font = _ui.FuenteTiza;
                t.fontStyle = FontStyles.Bold;
                QueCabe(t, h * 0.62f);
                UiKit.Rellenar(t.rectTransform, 2);
            }
            rt.gameObject.AddComponent<CanvasGroup>();
            return rt;
        }

        /// <summary>Un rotulo que cambia (un contador, «Día 3/10»), centrado en (x, y).</summary>
        public TMP_Text Rotulo(float x, float y, float w, float h, string texto, Color color, float tam) {
            var t = _ui.Texto(_lamina.Raiz, texto, EstiloTexto.Cuerpo, color, TextAlignmentOptions.Center);
            t.font = _ui.FuenteTiza;
            QueCabe(t, tam);
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            Poner(rt, x, y);
            t.gameObject.AddComponent<CanvasGroup>();
            return t;
        }

        /// <summary>Coloca el centro de una pieza en (x, y) de la lamina.</summary>
        public static void Poner(RectTransform rt, float x, float y) { rt.anchoredPosition = new Vector2(x, -y); }
        public static void Poner(RectTransform rt, Vector2 p) { Poner(rt, p.x, p.y); }

        public static void Alfa(Component pieza, float a) {
            var g = pieza.GetComponent<CanvasGroup>();
            if (g != null) g.alpha = Mathf.Clamp01(a);
        }

        public static void Escala(RectTransform rt, float s) { rt.localScale = new Vector3(s, s, 1); }

        /// <summary>Texto que se encoge para caber en su caja (y, si ni asi, puntos suspensivos): nunca se sale.</summary>
        public static void QueCabe(TMP_Text t, float tamMaximo) {
            t.enableAutoSizing = true;
            t.fontSizeMax = tamMaximo;
            t.fontSizeMin = Mathf.Max(6, tamMaximo * 0.5f);
            t.overflowMode = TextOverflowModes.Ellipsis;
        }

        // ---- curvas de movimiento
        public static float Tramo(float t, float desde, float dura) { return Mathf.Clamp01((t - desde) / Mathf.Max(0.0001f, dura)); }
        public static float Suave(float x) { x = Mathf.Clamp01(x); return x * x * (3 - 2 * x); }
        public static float Salida(float x) { x = Mathf.Clamp01(x); return 1 - Mathf.Pow(1 - x, 3); }
        public static float Rebote(float x) { x = Mathf.Clamp01(x); const float c1 = 1.70158f, c3 = c1 + 1; var y = x - 1; return 1 + c3 * y * y * y + c1 * y * y; }
        public static Vector2 Entre(Vector2 a, Vector2 b, float x) { return Vector2.LerpUnclamped(a, b, Suave(x)); }
    }

    /// <summary>
    /// Una pizarra animada por FASES (2026-11-08): la escena de cada fase se traza sola con tiza y sus piezas se mueven
    /// encima; debajo, las pestañas (que se llenan como una barra de progreso y se pueden pulsar) y el pie de la fase,
    /// una sola linea. Se repite en bucle. La lamina de 600×300 se escala para caber en el hueco sin deformarse.
    /// La usan la explicacion general de las metodologias (≤ 15 s) y la detallada de la elegida.
    /// </summary>
    public sealed class EscenaDeMetodologia : MonoBehaviour {
        private const float Trazo = 0.9f;
        /// <summary>El cuadro de la lamina: 600×300 las de metodologia; las de arquitectura, apaisadas (1000×300).</summary>
        private float Ancho = 600, Alto = 300;

        private UiKit _ui;
        private NexusTheme _t;
        private IList<FaseDeEscena> _fases;
        private RectTransform _tabla;
        private Lamina _lamina;
        private TMP_Text _pie;
        private readonly List<Image> _rellenos = new List<Image>();
        private readonly List<TMP_Text> _rotulosDePestana = new List<TMP_Text>();
        private readonly List<CanvasGroup> _textosDeTiza = new List<CanvasGroup>();
        private Action<float> _animar;
        private int _fase = -1;
        private float _tiempo;

        /// <summary>La fase que se esta viendo.</summary>
        public int Fase { get { return _fase; } }

        public static EscenaDeMetodologia Crear(UiKit ui, Transform padre, IList<FaseDeEscena> fases, string nombre = "Escena animada",
                                                float ancho = 600, float alto = 300) {
            var raiz = ui.Nodo(padre, nombre);
            raiz.gameObject.AddComponent<ListaDeElementos>();   // las pestañas son contenido, no botones de la pantalla
            var e = raiz.gameObject.AddComponent<EscenaDeMetodologia>();
            e.Ancho = ancho; e.Alto = alto;
            e.Construir(ui, raiz, fases);
            return e;
        }

        private void Construir(UiKit ui, RectTransform raiz, IList<FaseDeEscena> fases) {
            _ui = ui; _t = ui.Tema; _fases = fases;
            var fondo = raiz.gameObject.AddComponent<Image>();
            ui.Fondo(fondo, NexusTheme.Alfa(_t.bg950, 0.6f), NexusTheme.RadioLg);
            fondo.raycastTarget = false;
            ui.Borde(raiz, NexusTheme.Alfa(_t.cyan, 0.45f), NexusTheme.RadioLg, 1.5f);

            // 85 % arriba: la pizarra (recorta lo que salga). 15 % abajo: pestañas + pie.
            _tabla = ui.Nodo(raiz, "Pizarra");
            _tabla.anchorMin = new Vector2(0, 0.17f); _tabla.anchorMax = Vector2.one;
            _tabla.offsetMin = new Vector2(12, 4); _tabla.offsetMax = new Vector2(-12, -10);
            _tabla.gameObject.AddComponent<RectMask2D>();
            var capa = ui.Nodo(_tabla, "Capa");
            UiKit.Rellenar(capa);
            _lamina = new Lamina(ui, capa, Ancho, Alto);
            _lamina.Raiz.anchorMin = _lamina.Raiz.anchorMax = _lamina.Raiz.pivot = new Vector2(0.5f, 0.5f);
            _lamina.Raiz.sizeDelta = new Vector2(Ancho, Alto);
            _lamina.Dibujo.Tiza = true;

            var banda = ui.Nodo(raiz, "Banda");
            banda.anchorMin = Vector2.zero; banda.anchorMax = new Vector2(1, 0.17f);
            banda.offsetMin = new Vector2(14, 8); banda.offsetMax = new Vector2(-14, -2);
            var fila = ui.Fila(banda, "Fila", 10, 0, TextAnchor.MiddleLeft);
            UiKit.Rellenar(fila);
            fila.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;

            if (fases.Count > 1)
                for (var i = 0; i < fases.Count; i++) Pestana(fila, i);
            _pie = ui.Texto(fila, "", EstiloTexto.Cuerpo, _t.ink, TextAlignmentOptions.MidlineLeft);
            ActoresDeEscena.QueCabe(_pie, _t.Px(15));
            var alto = _pie.GetComponent<AltoDeTexto>();
            if (alto != null) DestroyImmediate(alto);   // el alto lo da la banda; el texto se ajusta a ella
            var le = UiKit.Tamano(_pie, flexAncho: 1, flexAlto: 1);
            le.minHeight = 0; le.preferredHeight = 0;

            Ir(0);
        }

        private void Pestana(RectTransform fila, int i) {
            var rt = _ui.Nodo(fila, "Pestana " + _fases[i].Pestana);
            UiKit.Tamano(rt, ancho: _t.Px(108), flexAlto: 1);
            var img = rt.gameObject.AddComponent<Image>();
            _ui.Fondo(img, NexusTheme.Alfa(_t.ink, 0.06f), NexusTheme.RadioSm);
            var relleno = _ui.Nodo(rt, "Progreso");
            relleno.anchorMin = Vector2.zero; relleno.anchorMax = new Vector2(0, 1);
            relleno.offsetMin = relleno.offsetMax = Vector2.zero;
            var ri = relleno.gameObject.AddComponent<Image>();
            _ui.Fondo(ri, NexusTheme.Alfa(_t.cyan, 0.28f), NexusTheme.RadioSm);
            ri.raycastTarget = false;
            _rellenos.Add(ri);
            var t = _ui.Texto(rt, _fases[i].Pestana, EstiloTexto.Etiqueta, _t.inkMuted, TextAlignmentOptions.Center);
            ActoresDeEscena.QueCabe(t, _t.Px(12));
            var altoT = t.GetComponent<AltoDeTexto>();
            if (altoT != null) DestroyImmediate(altoT);
            UiKit.Rellenar(t.rectTransform, 4);
            _rotulosDePestana.Add(t);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.None;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            var indice = i;
            b.onClick.AddListener(() => Ir(indice));
        }

        /// <summary>Salta a una fase (la pestaña pulsada, la carta que se mira…). La misma fase no se reinicia.</summary>
        public void Saltar(int fase) {
            if (fase < 0 || fase >= _fases.Count || fase == _fase) return;
            Ir(fase);
        }

        private void Ir(int fase) {
            _fase = fase;
            _tiempo = 0;
            // Limpiar la lamina: el dibujo y todo lo que se le puso encima.
            _lamina.Dibujo.Limpiar();
            for (var i = _lamina.Raiz.childCount - 1; i >= 0; i--) {
                var hijo = _lamina.Raiz.GetChild(i).gameObject;
                if (hijo == _lamina.Dibujo.gameObject) continue;
                hijo.SetActive(false);
                Destroy(hijo);
            }
            _textosDeTiza.Clear();
            var f = _fases[fase];
            var tiza = new Tizador(_lamina, _ui.FuenteTiza);
            var actores = new ActoresDeEscena(_ui, _lamina);
            _animar = f.Preparar != null ? f.Preparar(tiza, actores) : null;
            // Los textos de tiza (los del dibujo, sin CanvasGroup) aparecen al terminar el trazo; los actores, a su aire.
            for (var i = 0; i < _lamina.Raiz.childCount; i++) {
                var hijo = _lamina.Raiz.GetChild(i);
                if (!hijo.gameObject.activeSelf || hijo.gameObject == _lamina.Dibujo.gameObject) continue;
                if (hijo.GetComponent<TMP_Text>() == null || hijo.GetComponent<CanvasGroup>() != null) continue;
                var g = hijo.gameObject.AddComponent<CanvasGroup>();
                g.alpha = 0;
                _textosDeTiza.Add(g);
            }
            _lamina.Dibujo.Avance = 0;
            _pie.text = Nexus.Core.Narrativa.Glifos.Seguro(f.Pie ?? "");
            for (var i = 0; i < _rotulosDePestana.Count; i++) _rotulosDePestana[i].color = i == fase ? _t.cyan : _t.inkMuted;
            if (_animar != null) _animar(0);
        }

        private void Update() { Paso(Mathf.Min(Time.unscaledDeltaTime, 0.1f)); }

        /// <summary>Avanza el tiempo a mano (la herramienta de capturas, que no tiene bucle de juego).</summary>
        public void Avanzar(float segundos) {
            for (var s = 0f; s < segundos; s += 0.05f) Paso(0.05f);
        }

        private void Paso(float dt) {
            if (_fases == null || _fases.Count == 0) return;
            var r = _tabla.rect;
            if (r.width > 1 && r.height > 1) {
                var k = Mathf.Min(r.width / Ancho, r.height / Alto);
                _lamina.Raiz.localScale = new Vector3(k, k, 1);
            }
            _tiempo += dt;
            var f = _fases[_fase];
            if (_tiempo >= f.Duracion) { Ir((_fase + 1) % _fases.Count); return; }

            _lamina.Dibujo.Avance = ActoresDeEscena.Salida(_tiempo / Trazo);
            var alfa = Mathf.Clamp01((_tiempo - Trazo * 0.6f) / 0.3f);
            foreach (var g in _textosDeTiza) if (g != null) g.alpha = alfa;
            if (_animar != null) _animar(_tiempo);
            for (var i = 0; i < _rellenos.Count; i++) {
                var lleno = i < _fase ? 1f : i == _fase ? Mathf.Clamp01(_tiempo / f.Duracion) : 0f;
                _rellenos[i].rectTransform.anchorMax = new Vector2(lleno, 1);
            }
        }
    }
}
