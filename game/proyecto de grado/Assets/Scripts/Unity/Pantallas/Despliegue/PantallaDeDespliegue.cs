using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Despliegue;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Pantallas.Minijuegos.Corregir;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Una pieza que se arrastra por la lamina en las dos direcciones. Al soltarla dice donde quedo su centro, en
    /// coordenadas de la lamina; quien la creo decide que pasa y vuelve a pintar. Un clic sin arrastre tambien cuenta.
    /// (La Fase 3 ya no la usa desde 2026-11-09, pero otras pantallas la nombran.)
    /// </summary>
    public sealed class PiezaArrastrable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler {
        public Action<Vector2> AlSoltar;
        public Action AlPulsar;
        private RectTransform _rt;

        public void OnBeginDrag(PointerEventData e) {
            _rt = (RectTransform)transform;
            _rt.SetAsLastSibling();
            _rt.localRotation = Quaternion.Euler(0, 0, 1.5f);
        }

        public void OnDrag(PointerEventData e) {
            if (_rt == null) return;
            var padre = (RectTransform)_rt.parent;
            Vector2 local, anterior;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, e.position, e.pressEventCamera, out local);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, e.position - e.delta, e.pressEventCamera, out anterior);
            _rt.anchoredPosition += local - anterior;
        }

        public void OnEndDrag(PointerEventData e) {
            if (_rt == null) return;
            _rt.localRotation = Quaternion.identity;
            var centro = new Vector2(_rt.anchoredPosition.x + _rt.sizeDelta.x / 2, -_rt.anchoredPosition.y + _rt.sizeDelta.y / 2);
            AlSoltar?.Invoke(centro);
        }

        public void OnPointerClick(PointerEventData e) {
            if (!e.dragging) AlPulsar?.Invoke();
        }
    }

    /// <summary>Avisa de cuando el puntero entra o sale (un globo con lo que hace una pieza, sin tener que pulsarla).</summary>
    public sealed class AlPasarElRaton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        public Action AlEntrar, AlSalir;
        public void OnPointerEnter(PointerEventData e) { if (AlEntrar != null) AlEntrar(); }
        public void OnPointerExit(PointerEventData e) { if (AlSalir != null) AlSalir(); }
    }

    /// <summary>
    /// La Fase 3: desplegar (2026-11-09, rediseñada). Una sola cuenta atras para todo, y cuatro pasos que se suceden con
    /// una transicion de camara (zoom y fundido), sin scroll ni columnas: todo ocupa la pantalla y lo demas flota como
    /// globos de cristal.
    ///   0 · Intro   — tres tarjetas: lo que sale hoy, tu arquitectura y como se juega.
    ///   1 · Tuberia — se arman las etapas del camino del codigo al cliente y se PRUEBA: una luz recorre la tuberia y se
    ///                 detiene en la primera que falla. No se pasa al siguiente juego hasta tenerla perfecta.
    ///   2 · Cuartos — los modulos, a oscuras y con una linterna: en cada cuarto, una toma buena y un señuelo y la palanca
    ///                 de despliegue. Acertar enciende la luz; fallar salta en chispas y deja el cuarto a oscuras.
    ///   3 · Resultado — tarjetas y barras, no parrafos.
    /// Al agotarse el tiempo se entrega lo que haya. Las reglas estan en Core (RevisorDeTuberia, EvaluadorDeDespliegue);
    /// esto solo pinta, anima y recoge la respuesta. Cada paso vive en su archivo parcial (Intro, Tuberia, Cuartos,
    /// Resultado).
    /// </summary>
    public sealed partial class PantallaDeDespliegue : Pantalla {
        public Action AlTerminar;

        public override bool PuedeVolver { get { return false; } }

        private enum Paso { Intro, Tuberia, Cuartos, Resultado }

        private DespliegueDef _def;
        private PlanDeDespliegue _plan;
        private Nexus.Core.Proyecto.FichaDelProyecto _ficha;
        private Paso _paso = Paso.Intro;
        private string[] _huecos;
        private readonly Dictionary<string, Dictionary<string, string>> _enchufes = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        private int _intentos;
        private float _restante, _total;
        private ResultadoDeDespliegue _resultado;

        private Animador _anim;
        private RectTransform _escena, _hud, _capa, _accion;
        private TemporizadorView _reloj;
        private TMP_Text _antetitulo, _dialogo;
        private RectTransform _chipIntentos;
        private bool _tutorialAbierto, _transicionando;

        protected override void Construir() {
            var s = App.Sesion;
            _def = s.Perfil.Despliegue;
            _ficha = s.Perfil.Proyecto;
            _plan = s.PlanDelDespliegue();
            _huecos = new string[Math.Max(1, _def.Tuberia.Huecos)];
            foreach (var m in _plan.Modulos) _enchufes[m] = new Dictionary<string, string>(StringComparer.Ordinal);
            _total = _restante = _plan.Segundos;
            _anim = Raiz.GetComponent<Animador>();
            if (_anim == null) _anim = Raiz.gameObject.AddComponent<Animador>();

            // Tres capas a pantalla completa: la escena del paso (que cambia), los globos fijos y, encima, los avisos.
            var marco = UiKit.Rellenar(Ui.Nodo(Raiz, "Marco"));
            _escena = UiKit.Rellenar(Ui.Nodo(marco, "Escena"));
            _hud = UiKit.Rellenar(Ui.Nodo(marco, "Globos fijos"));
            _capa = UiKit.Rellenar(Ui.Nodo(marco, "Avisos"));
            ConstruirGlobosFijos();
            PintarIntro();
        }

        // ==================================================================== los globos fijos

        /// <summary>Arriba a la izquierda, el dialogo; arriba a la derecha, intentos, la accion del paso, la ayuda y el reloj.</summary>
        private void ConstruirGlobosFijos() {
            var dialogo = Globo(_hud, "Dialogo", false);
            Anclar(dialogo, new Vector2(0, 1), new Vector2(16, -14));
            dialogo.sizeDelta = new Vector2(700, 10);
            Crecer(dialogo, false);
            _antetitulo = Ui.Texto(dialogo, "", EstiloTexto.Leyenda, Tema.warning);
            _dialogo = Ui.Texto(dialogo, "", EstiloTexto.Dialogo, Tema.ink);
            var alto = _dialogo.GetComponent<AltoDeTexto>();
            if (alto != null) Destroy(alto);   // caja fija con letra que se ajusta: que no crezca el globo
            _dialogo.enableAutoSizing = true; _dialogo.fontSizeMin = 14; _dialogo.fontSizeMax = 21;
            UiKit.Tamano(_dialogo, alto: 78);

            var fila = Ui.Fila(_hud, "Acciones", Tema.espacio * 0.75f, 0, TextAnchor.MiddleRight);
            Anclar(fila, new Vector2(1, 1), new Vector2(-16, -14));
            Crecer(fila, true);
            fila.gameObject.AddComponent<ListaDeElementos>();
            _chipIntentos = Ui.Chip(fila, "", Tema.warningSoft, Tema.warning);
            _chipIntentos.gameObject.SetActive(false);
            _accion = Ui.Fila(fila, "Accion del paso", Tema.espacio * 0.75f, 0, TextAnchor.MiddleRight);
            GuiaView.BotonDeAyuda(App, fila);
            _reloj = Ui.Temporizador(fila, Reloj(_total), Tono.Cyan);
        }

        /// <summary>Un globo de cristal: vidrio oscuro con un filo de luz, como los de la Fase 1 y los del diagrama.</summary>
        private RectTransform Globo(Transform padre, string nombre, bool fila, Color? filo = null, float? relleno = null) {
            var rt = fila ? Ui.Fila(padre, nombre, Tema.espacio * 0.75f, relleno ?? Tema.espacio, TextAnchor.MiddleLeft)
                          : Ui.Columna(padre, nombre, Tema.espacio * 0.75f, relleno ?? Tema.espacio);
            var fondo = rt.gameObject.AddComponent<Image>();
            Ui.Fondo(fondo, NexusTheme.Alfa(Tema.bg950, 0.86f), NexusTheme.RadioLg);
            fondo.raycastTarget = true;
            Ui.Halo(rt, NexusTheme.Alfa(Tema.cyan, 0.16f), NexusTheme.RadioLg);
            Ui.Borde(rt, filo ?? NexusTheme.Alfa(Tema.cyan, 0.5f), NexusTheme.RadioLg, 1.5f);
            return rt;
        }

        private static void Anclar(RectTransform rt, Vector2 ancla, Vector2 pos) {
            rt.anchorMin = rt.anchorMax = rt.pivot = ancla;
            rt.anchoredPosition = pos;
        }

        private static void Crecer(RectTransform rt, bool aLoAncho) {
            var f = rt.gameObject.AddComponent<ContentSizeFitter>();
            f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            f.horizontalFit = aLoAncho ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
        }

        private static string Reloj(float segundos) {
            var s = Mathf.CeilToInt(Mathf.Max(0, segundos));
            return $"{s / 60}:{s % 60:00}";
        }

        private string NombreDe(string modulo) {
            var m = _ficha == null ? null : _ficha.Modulo(modulo);
            return m == null ? modulo : m.Nombre;
        }

        /// <summary>El dialogo escribe la frase letra a letra (con un tope: nunca hace esperar).</summary>
        private void Decir(string frase) {
            if (_dialogo == null) return;
            var texto = Nexus.Core.Narrativa.Glifos.Seguro(frase ?? "");
            _dialogo.text = texto;
            _dialogo.maxVisibleCharacters = 0;
            var total = texto.Length;
            var d = _dialogo;
            _anim.Lanzar(Mathf.Clamp(total * 0.016f, 0.25f, 1.1f), t => {
                if (d == null || d.text != texto) return;
                d.maxVisibleCharacters = Mathf.CeilToInt(total * t);
            }, () => { if (d != null && d.text == texto) d.maxVisibleCharacters = 99999; }, 0, d.gameObject);
        }

        private void PonerAntetitulo(string paso) {
            _antetitulo.text = ("Fase 3 · despliegue · día " + _plan.Dia + (_plan.EsFinal ? " · el último" : "") + " · " + paso).ToUpperInvariant();
        }

        private void PonerIntentos() {
            if (_chipIntentos == null) return;
            _chipIntentos.gameObject.SetActive(_intentos > 0 && _paso == Paso.Tuberia);
            var t = _chipIntentos.GetComponentInChildren<TMP_Text>();
            if (t != null) t.text = _intentos == 1 ? "1 prueba fallida" : _intentos + " pruebas fallidas";
        }

        /// <summary>Entra con un fundido y una escala de 0,88 a 1 tras 'retraso' segundos.</summary>
        private void Aparecer(RectTransform rt, float retraso) {
            var g = rt.gameObject.GetComponent<CanvasGroup>();
            if (g == null) g = rt.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0;
            _anim.Lanzar(0.38f, t => {
                if (g == null) return;
                var e = Curvas.SalidaAtras(t);
                g.alpha = Mathf.Clamp01(t * 2.5f);
                rt.localScale = Vector3.one * Mathf.LerpUnclamped(0.88f, 1f, e);
            }, () => { if (g != null) g.alpha = 1; }, retraso, rt.gameObject);
        }

        /// <summary>
        /// Chispas que salen disparadas en circulo y caen. 'centro' esta en coordenadas de lamina (origen arriba a la
        /// izquierda, y hacia abajo) si 'arribaIzquierda'; si no, en las del padre centrado.
        /// </summary>
        private void Chispas(Transform padre, Vector2 centro, Color color, int n, float alcance, bool arribaIzquierda) {
            for (var i = 0; i < n; i++) {
                var p = Ui.Nodo(padre, "Chispa");
                if (arribaIzquierda) { p.anchorMin = p.anchorMax = new Vector2(0, 1); } else { p.anchorMin = p.anchorMax = new Vector2(0.5f, 0.5f); }
                p.pivot = new Vector2(0.5f, 0.5f);
                p.sizeDelta = new Vector2(8 + (i % 3) * 3, 8 + (i % 3) * 3);
                var img = p.gameObject.AddComponent<Image>();
                img.sprite = Ui.SpriteCircular();
                img.raycastTarget = false;
                var ang = (i / (float)n) * Mathf.PI * 2 + (i % 2) * 0.4f;
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                var lejos = alcance * (0.55f + (i % 4) * 0.18f);
                var origen = arribaIzquierda ? new Vector2(centro.x, -centro.y) : centro;
                p.anchoredPosition = origen;
                _anim.Lanzar(0.75f, t => {
                    if (p == null) return;
                    var e = Curvas.SalidaCubica(t);
                    p.anchoredPosition = origen + dir * lejos * e + new Vector2(0, -50f * t * t);
                    img.color = NexusTheme.Alfa(color, 1 - t);
                    p.localScale = Vector3.one * (1 - 0.6f * t);
                }, () => { if (p != null) Destroy(p.gameObject); }, 0.02f * (i % 5), p.gameObject);
            }
        }

        // ==================================================================== de un paso al siguiente

        /// <summary>
        /// La camara avanza: lo que habia se acerca y se funde, se monta lo siguiente y entra desde un poco mas lejos.
        /// 'cambiar' pinta el paso nuevo (la escena ya esta vacia).
        /// </summary>
        private void Transicion(Action cambiar) {
            if (_transicionando) { UiKit.Vaciar(_escena); cambiar(); return; }
            _transicionando = true;
            var g = _escena.GetComponent<CanvasGroup>();
            if (g == null) g = _escena.gameObject.AddComponent<CanvasGroup>();
            g.interactable = false;
            _anim.Lanzar(0.28f, t => {
                if (g == null) return;
                g.alpha = 1 - t;
                _escena.localScale = Vector3.one * Mathf.Lerp(1f, 1.08f, Curvas.EntradaSalida(t));
            }, () => {
                UiKit.Vaciar(_escena);
                cambiar();
                _anim.Lanzar(0.4f, t => {
                    if (g == null) return;
                    g.alpha = Mathf.Clamp01(t * 1.6f);
                    _escena.localScale = Vector3.one * Mathf.LerpUnclamped(0.93f, 1f, Curvas.SalidaAtras(t));
                }, () => {
                    if (g == null) return;
                    g.alpha = 1; g.interactable = true; _escena.localScale = Vector3.one; _transicionando = false;
                }, 0, gameObject);
            }, 0, gameObject);
        }

        /// <summary>Un paso no deja nada del anterior: ni su boton de accion, ni sus avisos, ni su tutorial.</summary>
        private void LimpiarPaso() {
            if (_accion != null) UiKit.Vaciar(_accion);
            for (var i = _capa.childCount - 1; i >= 0; i--) Destroy(_capa.GetChild(i).gameObject);
            _tutorialAbierto = false;
            LimpiarTuberia();
            LimpiarCuartos();
        }

        /// <summary>El tutorial en globos de un paso, solo la primera vez (despues, el «?» de la guia).</summary>
        private void TutorialDelPaso(string clave, List<PasoDeTutorial> pasos) {
            if (App.YaVioLaReceta(clave)) return;
            App.AnotarRecetaVista(clave);
            _tutorialAbierto = true;
            GloboDeTutorial.Abrir(Ui, _capa, _anim, pasos, () => _tutorialAbierto = false);
        }

        // ==================================================================== reloj, teclado y entrega

        private void Update() {
            var jugando = _paso == Paso.Tuberia || _paso == Paso.Cuartos;
            var corriendo = jugando && !_tutorialAbierto && !GuiaView.PausaActiva && !PantallaDePausa.Abierta;
            // La musica sube en el ultimo cuarto de la cuenta atras (y baja si el reloj se para).
            if (App.Musica != null)
                App.Musica.Apremio(corriendo && _restante / Mathf.Max(10f, _total) <= 0.25f);
            if (_paso == Paso.Cuartos) ActualizarLuzDeLosCuartos();
            if (_paso == Paso.Tuberia) TeclasDeLaTuberia();
            if (!corriendo) return;
            _restante -= Time.unscaledDeltaTime;
            var f = Mathf.Max(0, _restante) / Mathf.Max(10f, _total);
            _reloj.Mostrar(Reloj(_restante), f < 0.15f ? Tono.Peligro : f < 0.3f ? Tono.Aviso : Tono.Cyan);
            if (_restante <= 0) Entregar(true);
        }

        private RespuestaDeDespliegue Respuesta(bool sinTiempo) {
            return new RespuestaDeDespliegue {
                Tuberia = _huecos.ToList(),
                Enchufes = _enchufes.ToDictionary(kv => kv.Key, kv => new Dictionary<string, string>(kv.Value, StringComparer.Ordinal), StringComparer.Ordinal),
                SegundosUsados = _total - Mathf.Max(0, _restante),
                SeAcaboElTiempo = sinTiempo,
                IntentosFallidosDeTuberia = _intentos
            };
        }

        private void Entregar(bool sinTiempo) {
            if (_paso == Paso.Resultado) return;
            _paso = Paso.Resultado;
            _reloj.Mostrar(Reloj(_restante), sinTiempo ? Tono.Peligro : Tono.Cyan);
            _resultado = App.Sesion.ResolverDespliegue(Respuesta(sinTiempo));
            PonerIntentos();
            LimpiarPaso();
            Transicion(PintarResultado);
        }

        public override void Repintar() { }
    }
}
