using System;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Un nodo de la pausa en red (2026-11-07): un circulo con su pictograma, su rotulo debajo y su linea hacia el
    /// nucleo.
    ///
    ///   ENTRAR   sale del nucleo (posicion, escala con rebote) y su linea crece.
    ///   ENFOCAR  al pasar el raton (o con el foco del teclado) sale del nucleo un circulito que RECORRE la linea, que se
    ///            ilumina a su paso; cuando llega, el nodo se agranda de golpe (con rebote), enciende el borde y el icono.
    ///   SALIR    al cerrar la pausa el nodo vuelve al nucleo en remolino, encogiendose (con un pequeño tiron atras
    ///            antes), apagandose, mientras su linea se recoge.
    ///
    /// Un solo componente manda en la escala (entrada x foco x salida): PiezaViva tambien la escribe cada fotograma, y
    /// las dos se pisarian. Todo con unscaledDeltaTime: en la pausa el reloj del juego esta parado.
    /// </summary>
    public sealed class NodoDePausa : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        public const float Diametro = 132f;
        private const float EscalaAlEnfocar = 1.20f;
        /// <summary>Lo que tarda el circulito en ir del nucleo al nodo.</summary>
        private const float SegundosDelPulso = 0.30f;
        private const float DiametroDelPulso = 26f;

        public int Indice;
        /// <summary>El raton entra o sale del nodo (la pantalla mueve el foco).</summary>
        public Action<int> AlEntrar, AlSalir;
        public Button Boton { get; private set; }

        private NexusTheme _tema;
        private RectTransform _rt, _pulsoRt;
        private CanvasGroup _grupo;
        private Image _fondo, _borde, _halo;
        private DibujoUI _icono, _linea, _lineaViva;
        private TMP_Text _rotulo;
        private PictogramasDePausa.Tipo _tipo;
        // En el sistema de la red (centro en 0,0, Y arriba): donde nace y a donde va el nodo, y los extremos de su linea.
        private Vector2 _origen, _destino, _pulsoDesde, _pulsoHasta;
        // En el sistema del dibujo (desde arriba a la izquierda).
        private Vector2 _lineaDesde, _lineaHasta;
        private float _entrada, _salida, _giro, _foco, _pulso;
        private bool _enfocado, _encendido;

        /// <summary>
        /// Crea el nodo en 'padre' (la red). 'origen' y 'destino' van en el sistema de la red: centro en (0,0), Y hacia
        /// arriba. 'centroNucleo' y 'radioNucleo' dicen donde empieza su linea; 'tamanoRed' es el tamaño de la red, para
        /// llevar esos puntos al dibujo (que cuenta desde arriba a la izquierda).
        /// </summary>
        public static NodoDePausa Crear(UiKit ui, RectTransform padre, PictogramasDePausa.Tipo tipo, string rotulo, int indice,
                                        Vector2 origen, Vector2 destino, Vector2 centroNucleo, float radioNucleo, Vector2 tamanoRed,
                                        Action alPulsar) {
            var tema = ui.Tema;

            // Las lineas van DEBAJO de todo lo demas de la red. Se crea primero la que se ilumina (cian, encima) y luego la
            // base: cada SetAsFirstSibling manda a la ultima al fondo.
            var rv = ui.Nodo(padre, "Linea viva " + rotulo);
            Centrar(rv, tamanoRed, Vector2.zero);
            var lineaViva = rv.gameObject.AddComponent<DibujoUI>();
            lineaViva.raycastTarget = false;
            rv.SetAsFirstSibling();
            var rl = ui.Nodo(padre, "Linea " + rotulo);
            Centrar(rl, tamanoRed, Vector2.zero);
            var linea = rl.gameObject.AddComponent<DibujoUI>();
            linea.raycastTarget = false;
            rl.SetAsFirstSibling();

            // El circulito que recorre la linea al enfocar.
            var rp = ui.Nodo(padre, "Pulso " + rotulo);
            Centrar(rp, new Vector2(DiametroDelPulso, DiametroDelPulso), origen);
            var pulso = rp.gameObject.AddComponent<Image>();
            pulso.sprite = ui.SpriteCircular();
            pulso.color = tema.cyan;
            pulso.raycastTarget = false;
            ui.Halo(rp, NexusTheme.Alfa(tema.cyan, 0.7f), DiametroDelPulso / 2f / tema.escala);
            rp.gameObject.SetActive(false);

            var rt = ui.Nodo(padre, "Nodo " + rotulo);
            Centrar(rt, new Vector2(Diametro, Diametro), origen);
            var nodo = rt.gameObject.AddComponent<NodoDePausa>();
            nodo._tema = tema;
            nodo._rt = rt;
            nodo._pulsoRt = rp;
            nodo._tipo = tipo;
            nodo._origen = origen;
            nodo._destino = destino;
            nodo.Indice = indice;
            nodo._linea = linea;
            nodo._lineaViva = lineaViva;
            nodo._grupo = rt.gameObject.AddComponent<CanvasGroup>();

            var dir = (destino - centroNucleo).normalized;
            nodo._pulsoDesde = centroNucleo + dir * (radioNucleo + 6f);
            nodo._pulsoHasta = destino - dir * (Diametro / 2f + 6f);
            nodo._lineaDesde = ALinea(nodo._pulsoDesde, tamanoRed);
            nodo._lineaHasta = ALinea(nodo._pulsoHasta, tamanoRed);
            lineaViva.Linea(nodo._lineaDesde.x, nodo._lineaDesde.y, nodo._lineaHasta.x, nodo._lineaHasta.y, tema.cyan, 4f);
            lineaViva.Avance = 0f;

            nodo._fondo = rt.gameObject.AddComponent<Image>();
            nodo._fondo.sprite = ui.SpriteCircular();
            nodo._fondo.color = tema.surfaceRaised;

            var boton = rt.gameObject.AddComponent<Button>();
            boton.transition = Selectable.Transition.None;
            boton.navigation = new Navigation { mode = Navigation.Mode.None };   // Tab/Enter los lleva la pantalla, no uGUI
            boton.targetGraphic = nodo._fondo;
            boton.onClick.AddListener(() => alPulsar?.Invoke());
            nodo.Boton = boton;

            var radioWeb = Diametro / 2f / tema.escala;
            nodo._halo = ui.Halo(rt, NexusTheme.Alfa(tema.cyan, 0f), radioWeb);
            nodo._borde = ui.Borde(rt, tema.lineStrong, radioWeb, 2f);

            var ri = ui.Nodo(rt, "Icono");
            Centrar(ri, new Vector2(PictogramasDePausa.Lado, PictogramasDePausa.Lado), Vector2.zero);
            nodo._icono = ri.gameObject.AddComponent<DibujoUI>();
            nodo._icono.raycastTarget = false;

            nodo._rotulo = ui.Texto(rt, rotulo, EstiloTexto.Etiqueta, tema.inkMuted, TextAlignmentOptions.Top);
            nodo._rotulo.raycastTarget = false;
            var rr = nodo._rotulo.rectTransform;
            rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.5f);
            rr.pivot = new Vector2(0.5f, 1f);
            rr.sizeDelta = new Vector2(220f, 56f);
            rr.anchoredPosition = new Vector2(0f, -(Diametro / 2f + 10f));

            nodo.Pintar(false);
            nodo.Aplicar();
            return nodo;
        }

        // ================================================================== desde fuera

        /// <summary>0 = dentro del nucleo, sin verse; 1 = en su sitio. Lo escalona la pantalla.</summary>
        public void Entrar(float t) { _entrada = Mathf.Clamp01(t); }

        /// <summary>
        /// La salida: 0 = en su sitio; 1 = dentro del nucleo, apagado. 'giroGrados' es lo que gira alrededor del nucleo
        /// mientras vuelve (el remolino).
        /// </summary>
        public void Salir(float p, float giroGrados) {
            _salida = Mathf.Clamp01(p);
            _giro = giroGrados;
        }

        public void Enfocar(bool enfocado) { _enfocado = enfocado; }

        public void Activar() {
            if (Boton != null && Boton.IsInteractable()) Boton.onClick.Invoke();
        }

        public void OnPointerEnter(PointerEventData e) { if (AlEntrar != null) AlEntrar(Indice); }
        public void OnPointerExit(PointerEventData e) { if (AlSalir != null) AlSalir(Indice); }

        // ================================================================== cada fotograma

        private void Update() {
            var dt = Time.unscaledDeltaTime;
            var activo = _enfocado && _salida <= 0f;
            // El circulito va hasta el nodo (SegundosDelPulso); al soltar, la linea se recoge mas deprisa.
            _pulso = Mathf.MoveTowards(_pulso, activo ? 1f : 0f, dt / (activo ? SegundosDelPulso : 0.15f));
            // El nodo SOLO se agranda cuando el circulito ha llegado.
            var llegado = activo && _pulso >= 1f;
            _foco = Mathf.MoveTowards(_foco, llegado ? 1f : 0f, dt / (llegado ? 0.22f : 0.15f));
            Aplicar();
        }

        private void Aplicar() {
            var s = _salida;
            var f = _foco * _foco * (3f - 2f * _foco);       // smoothstep: el color y el brillo

            Vector2 posicion;
            float escala;
            if (s > 0f) {
                // La salida: vuelve al nucleo girando, con ease-in; antes de irse se hincha un poco (anticipacion).
                var desdeElNucleo = Vector2.Lerp(_destino, _origen, s * s * s) - _origen;
                var a = _giro * s * Mathf.Deg2Rad;
                var c = Mathf.Cos(a);
                var sn = Mathf.Sin(a);
                posicion = _origen + new Vector2(desdeElNucleo.x * c - desdeElNucleo.y * sn, desdeElNucleo.x * sn + desdeElNucleo.y * c);
                escala = Mathf.Max(0f, 1f - EaseInBack(s));
            } else {
                var e = 1f - Mathf.Pow(1f - _entrada, 3f);   // ease-out cubico
                posicion = Vector2.Lerp(_origen, _destino, e);
                escala = EaseOutBack(_entrada);
            }
            _rt.anchoredPosition = posicion;
            // El foco entra con rebote (se agranda de golpe y se asienta), no con una rampa.
            _rt.localScale = Vector3.one * (escala * Mathf.LerpUnclamped(1f, EscalaAlEnfocar, EaseOutBack(_foco)));
            _grupo.alpha = s > 0f ? 1f - Mathf.Clamp01((s - 0.3f) / 0.7f) : 1f;

            _fondo.color = Color.Lerp(_tema.surfaceRaised, _tema.cyanSoft, f);
            _borde.color = Color.Lerp(_tema.lineStrong, _tema.cyan, f);
            _halo.color = NexusTheme.Alfa(_tema.cyan, 0.6f * f);
            _rotulo.color = Color.Lerp(_tema.inkMuted, _tema.ink, f);

            // La linea: crece al entrar y se recoge al salir; la iluminada la sigue el circulito.
            _linea.Avance = _entrada * (1f - Mathf.Clamp01(s * 1.8f));
            var p = _pulso * _pulso * (3f - 2f * _pulso);
            _lineaViva.Avance = s > 0f ? 0f : p;

            var viaja = _enfocado && s <= 0f && _pulso > 0.001f && _pulso < 0.999f;
            if (_pulsoRt.gameObject.activeSelf != viaja) _pulsoRt.gameObject.SetActive(viaja);
            if (viaja) {
                _pulsoRt.anchoredPosition = Vector2.Lerp(_pulsoDesde, _pulsoHasta, p);
                _pulsoRt.localScale = Vector3.one * Mathf.Lerp(0.75f, 1.2f, p);
            }

            var encendido = f > 0.5f;
            if (encendido != _encendido) Pintar(encendido);
        }

        /// <summary>El pictograma y la linea base, en cian si el nodo esta enfocado.</summary>
        private void Pintar(bool encendido) {
            _encendido = encendido;
            PictogramasDePausa.Dibujar(_icono, _tipo, encendido ? _tema.cyan : _tema.ink);
            _linea.Limpiar();
            _linea.Linea(_lineaDesde.x, _lineaDesde.y, _lineaHasta.x, _lineaHasta.y,
                         encendido ? _tema.cyan : NexusTheme.Alfa(_tema.cyan, 0.45f), 3f);
        }

        // ================================================================== ayudas

        private static void Centrar(RectTransform rt, Vector2 tamano, Vector2 posicion) {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = tamano;
            rt.anchoredPosition = posicion;
        }

        /// <summary>De un punto de la red (centro en 0,0 y Y hacia arriba) al del dibujo (desde arriba a la izquierda).</summary>
        private static Vector2 ALinea(Vector2 p, Vector2 tamano) {
            return new Vector2(p.x + tamano.x / 2f, tamano.y / 2f - p.y);
        }

        private static float EaseOutBack(float x) {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        /// <summary>Empieza yendo un poco hacia atras (el tiron antes de irse) y acaba en 1.</summary>
        private static float EaseInBack(float x) {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return c3 * x * x * x - c1 * x * x;
        }
    }
}
