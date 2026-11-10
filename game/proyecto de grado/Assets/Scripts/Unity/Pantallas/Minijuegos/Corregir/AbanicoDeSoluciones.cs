using System;
using System.Collections.Generic;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos.Corregir {
    /// <summary>Lo que necesita el abanico de cada carta.</summary>
    public sealed class OpcionDeSolucion {
        public string Id;
        public string Nombre;
        public string Descripcion;
        public string Icono;
        /// <summary>"patron" | "practica" | null.</summary>
        public string Tipo;
        /// <summary>«Está bien así»: va aparte, no se gasta.</summary>
        public bool EsEstaBien;
    }

    /// <summary>
    /// La mano de cartas de solucion del diagrama (2026-11-09), en abanico como en el boceto: las cartas se reparten
    /// desde abajo, la que se mira sube, se endereza y crece, y las vecinas se apartan. Cuando hay un punto elegido la
    /// mano «se ofrece» (sube un poco). Una carta jugada sigue en la mano, hundida, apagada y con la chapa «EN #2»: no
    /// se puede repetir. La carta «Está bien así» va al final, separada, y nunca se gasta.
    /// </summary>
    public sealed class AbanicoDeSoluciones : MonoBehaviour {
        public const float AnchoCarta = 228, AltoCarta = 316;
        /// <summary>
        /// El alto del sitio donde vive la mano. En reposo las cartas asoman solo por arriba (se ven la tecla, el dibujo y
        /// el nombre) para dejarle alto al diagrama; al elegir un punto o pasar el raton, suben y se ven enteras.
        /// </summary>
        public const float AltoDeReposo = 250;

        /// <summary>El tamaño de las cartas (1 = el de siempre). La tuberia de la Fase 3 trae 12: se usan mas pequeñas.</summary>
        public float Escala = 1f;
        /// <summary>Si las cartas se pueden arrastrar fuera de la mano (AlSoltar dice donde se soltaron).</summary>
        public bool PermiteArrastrar;
        public Action<string, PointerEventData> AlSoltar;
        private const float Solape = -26, SeparacionEstaBien = 40, Rapidez = 14f;

        public Action<string> AlPulsar;
        public Action<string> AlEnfocar;

        private UiKit _ui;
        private NexusTheme _tema;
        private RectTransform _rt;
        private readonly List<CartaDeSolucionView> _cartas = new List<CartaDeSolucionView>();
        private int _encima = -1;
        private bool _ofrecida;
        private float _t, _subida;

        public RectTransform Raiz { get { return _rt; } }
        public IReadOnlyList<CartaDeSolucionView> Cartas { get { return _cartas; } }

        public static AbanicoDeSoluciones Crear(UiKit ui, RectTransform sitio, IList<OpcionDeSolucion> opciones) {
            var rt = ui.Nodo(sitio, "Abanico de soluciones");
            UiKit.Rellenar(rt);
            rt.gameObject.AddComponent<ListaDeElementos>();   // contenido, no botones de navegacion (regla de los 7 botones)
            var a = rt.gameObject.AddComponent<AbanicoDeSoluciones>();
            a._ui = ui;
            a._tema = ui.Tema;
            a._rt = rt;
            for (var i = 0; i < opciones.Count; i++) a._cartas.Add(CartaDeSolucionView.Crear(ui, rt, a, i, opciones[i]));
            foreach (var c in a._cartas) c.Colocar(new Vector2(0, -420), 0, 1, true);
            return a;
        }

        public CartaDeSolucionView Carta(string id) { return _cartas.Find(c => c.Opcion.Id == id); }

        /// <summary>Hay un punto elegido: la mano sube un poco, invitando a jugar.</summary>
        public void Ofrecer(bool si) { _ofrecida = si; }

        internal void Entrar(CartaDeSolucionView c) { _encima = _cartas.IndexOf(c); AlEnfocar?.Invoke(c.Opcion.Id); Ordenar(); }
        internal void Salir(CartaDeSolucionView c) { if (_cartas.IndexOf(c) == _encima) { _encima = -1; AlEnfocar?.Invoke(null); Ordenar(); } }
        internal void Pulsar(CartaDeSolucionView c) { AlPulsar?.Invoke(c.Opcion.Id); }

        /// <summary>Mira una carta con el teclado (o ninguna con null).</summary>
        public void Mirar(string id) {
            var c = id == null ? null : Carta(id);
            _encima = c == null ? -1 : _cartas.IndexOf(c);
            Ordenar();
        }

        private void Ordenar() {
            for (var i = 0; i < _cartas.Count; i++) _cartas[i].transform.SetSiblingIndex(i);
            if (_encima >= 0 && _encima < _cartas.Count) _cartas[_encima].transform.SetAsLastSibling();
        }

        private void Update() {
            var dt = Time.unscaledDeltaTime;
            _t += dt;
            var visibles = _cartas.FindAll(c => !c.Quemada && !c.Volando && !c.Oculta);
            var normales = visibles.FindAll(c => !c.Opcion.EsEstaBien);
            var estaBien = visibles.Find(c => c.Opcion.EsEstaBien);
            // El ancho que cabe: si no, el solape crece.
            var n = normales.Count + (estaBien != null ? 1 : 0);
            var paso = (AnchoCarta + Solape) * Escala;
            var anchoTotal = n * paso + (estaBien != null ? SeparacionEstaBien * Escala : 0);
            var disponible = Mathf.Max(400, _rt.rect.width - 40);
            if (anchoTotal > disponible) paso -= (anchoTotal - disponible) / Mathf.Max(1, n);
            var x0 = -(n - 1) * paso / 2f - (estaBien != null ? SeparacionEstaBien / 2f : 0);
            // Lo que asoma: la carta pegada arriba del sitio y el resto, por debajo. Ofrecida, sube lo que le falta para verse entera.
            var oculto = Mathf.Max(0f, AltoCarta * Escala - _rt.rect.height + 8f);
            _subida = Mathf.MoveTowards(_subida, _ofrecida ? oculto : 0f, dt * 900f);
            var baseY = _rt.rect.height / 2f - AltoCarta * Escala / 2f - 8f + _subida;
            var medio = (n - 1) / 2f;
            var encima = _encima >= 0 && _encima < _cartas.Count ? _cartas[_encima] : null;
            var iEncima = encima != null ? visibles.IndexOf(encima) : -1;

            for (var k = 0; k < visibles.Count; k++) {
                var c = visibles[k];
                var i = c.Opcion.EsEstaBien ? n - 1 : normales.IndexOf(c);
                var d = i - medio;
                var x = x0 + i * paso + (c.Opcion.EsEstaBien ? SeparacionEstaBien : 0);
                var y = baseY - d * d * 5f;
                var ang = -d * 3.2f;
                var escala = Escala;
                if (c == encima) { y += Mathf.Max(0f, oculto - _subida) + 40f; ang = 0; escala = 1.1f * Escala; }
                else if (iEncima >= 0) x += Math.Sign(k - iEncima) * 26;
                if (c.EnUso > 0) { y -= 70; escala *= 0.92f; ang *= 0.5f; }
                // El reparto: cada carta llega desde abajo, una tras otra.
                var llegada = Curvas.SalidaAtras(Mathf.Clamp01((_t - 0.05f - _cartas.IndexOf(c) * 0.07f) / 0.45f));
                if (llegada < 1f) { y = Mathf.LerpUnclamped(y - 360, y, llegada); ang += (1 - llegada) * (d * 14f); }
                c.Colocar(new Vector2(x, y), ang, escala, llegada < 1f, dt * Rapidez);
            }
        }

        // ====================================================================== lo que pide la pantalla

        public void Sacudir(string id) { var c = Carta(id); if (c != null) c.Sacudir(); }

        internal void Soltar(CartaDeSolucionView c, PointerEventData e) { if (AlSoltar != null) AlSoltar(c.Opcion.Id, e); }

        /// <summary>
        /// La carta se queda PUESTA en otro sitio (una estacion de la tuberia): sale de la mano hasta que se devuelva con
        /// Mostrar. Se usa despues de un vuelo (Despegar) o de un arrastre.
        /// </summary>
        public void Posar(string id) {
            var c = Carta(id);
            if (c == null) return;
            if (_encima == _cartas.IndexOf(c)) _encima = -1;
            c.transform.SetParent(_rt, false);
            c.Volando = false;
            c.Oculta = true;
            c.gameObject.SetActive(false);
        }

        /// <summary>La carta que estaba puesta vuelve a la mano, con un saltito.</summary>
        public void Mostrar(string id) {
            var c = Carta(id);
            if (c == null) return;
            c.Oculta = false;
            c.gameObject.SetActive(true);
            c.Saltar();
        }

        public bool EstaPuesta(string id) { var c = Carta(id); return c != null && c.Oculta; }

        /// <summary>La carta sale de la mano para volar (la mueve quien la anima, en la capa de vuelos).</summary>
        public RectTransform Despegar(string id, Transform capa) {
            var c = Carta(id);
            if (c == null) return null;
            c.Volando = true;
            if (_encima == _cartas.IndexOf(c)) _encima = -1;
            c.transform.SetParent(capa, true);
            return (RectTransform)c.transform;
        }

        /// <summary>
        /// Vuelve a la mano: hundida y con su chapa si quedo jugada ('numero' &gt; 0), o normal. Entra desde abajo de su
        /// sitio, para que se vea que «queda» ahi.
        /// </summary>
        public void Aterrizar(string id, int numero) {
            var c = Carta(id);
            if (c == null) return;
            c.transform.SetParent(_rt, false);
            c.Volando = false;
            c.MarcarEnUso(numero);
            c.Colocar(new Vector2(c.Posicion.x, -_rt.rect.height / 2f - AltoCarta), 0, 0.6f, true);
            Ordenar();
        }

        /// <summary>Una carta que estaba jugada vuelve a estar libre: un saltito para que se note.</summary>
        public void Devolver(string id, Animador anim) {
            var c = Carta(id);
            if (c == null) return;
            c.MarcarEnUso(0);
            c.Saltar();
        }

        /// <summary>Una pista quema esta carta: se pone naranja, tiembla, se encoge y desaparece.</summary>
        public void Quemar(string id, Animador anim, Action alFin = null) {
            var c = Carta(id);
            if (c == null) { alFin?.Invoke(); return; }
            c.Quemar(anim, alFin);
        }
    }

    /// <summary>Una carta de solucion: ilustracion arriba, nombre, y para que sirve.</summary>
    public sealed class CartaDeSolucionView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
                                              IBeginDragHandler, IDragHandler, IEndDragHandler {
        public OpcionDeSolucion Opcion { get; private set; }
        public bool Volando;
        /// <summary>Puesta en otro sitio (una estacion): no esta en la mano.</summary>
        public bool Oculta;
        private bool _arrastrando;
        public bool Quemada { get; private set; }
        /// <summary>El numero del punto en el que esta (0 = en la mano).</summary>
        public int EnUso { get; private set; }
        public Vector2 Posicion { get { return _pos; } }

        private AbanicoDeSoluciones _abanico;
        private NexusTheme _tema;
        private RectTransform _rt, _chapa;
        private CanvasGroup _grupo;
        private Image _fondo, _halo, _borde;
        private TMP_Text _titulo, _textoChapa;
        private Vector2 _pos;
        private float _ang, _escala = 1, _sacudida, _brillo, _salto;

        public static CartaDeSolucionView Crear(UiKit ui, RectTransform padre, AbanicoDeSoluciones abanico, int indice, OpcionDeSolucion o) {
            var tema = ui.Tema;
            var rt = ui.Nodo(padre, "Carta " + o.Id);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(AbanicoDeSoluciones.AnchoCarta, AbanicoDeSoluciones.AltoCarta);
            var c = rt.gameObject.AddComponent<CartaDeSolucionView>();
            c.Opcion = o;
            c._abanico = abanico;
            c._tema = tema;
            c._rt = rt;
            c._grupo = rt.gameObject.AddComponent<CanvasGroup>();

            c._fondo = rt.gameObject.AddComponent<Image>();
            ui.Fondo(c._fondo, o.EsEstaBien ? NexusTheme.Alfa(tema.bg950, 0.95f) : NexusTheme.Alfa(tema.bg900, 0.97f), NexusTheme.RadioLg);
            c._fondo.raycastTarget = true;
            var acento = o.EsEstaBien ? tema.success : tema.cyan;
            c._halo = ui.Halo(rt, NexusTheme.Alfa(acento, 0), NexusTheme.RadioLg);
            c._borde = ui.Borde(rt, o.EsEstaBien ? NexusTheme.Alfa(tema.success, 0.7f) : tema.lineStrong, NexusTheme.RadioLg, 1.5f);

            const float relleno = 16;
            const float alto = AbanicoDeSoluciones.AltoCarta, ancho = AbanicoDeSoluciones.AnchoCarta;

            // La tecla (1, 2, 3… y 0 para «Está bien así») y el tipo.
            var tecla = ui.Nodo(rt, "Tecla");
            Lamina.Colocar(tecla, relleno, relleno, 30, 30);
            var fondoTecla = tecla.gameObject.AddComponent<Image>();
            ui.Fondo(fondoTecla, tema.bg950, 6);
            fondoTecla.raycastTarget = false;
            ui.Borde(tecla, NexusTheme.Alfa(acento, 0.7f), 6, 1);
            var rotulo = Texto(ui, tecla, o.EsEstaBien ? "0" : (indice + 1).ToString(), 17, acento, TextAlignmentOptions.Center);
            UiKit.Rellenar(rotulo.rectTransform);
            if (!string.IsNullOrEmpty(o.Tipo) || o.EsEstaBien) {
                var tipo = Texto(ui, rt, o.EsEstaBien ? "NO SE GASTA" : o.Tipo == "patron" ? "PATRÓN" : "PRÁCTICA", 13,
                                 NexusTheme.Alfa(acento, 0.85f), TextAlignmentOptions.MidlineRight);
                tipo.characterSpacing = 3;
                Lamina.Colocar(tipo.rectTransform, 52, relleno, ancho - 52 - relleno, 30);
            }

            // La ilustracion, sobre su placa.
            var placa = ui.Nodo(rt, "Ilustracion");
            Lamina.Colocar(placa, relleno, 56, ancho - 2 * relleno, 118);
            var fondoPlaca = placa.gameObject.AddComponent<Image>();
            ui.Fondo(fondoPlaca, NexusTheme.Alfa(tema.bg950, 0.7f), 8);
            fondoPlaca.raycastTarget = false;
            var lienzo = ui.Nodo(placa, "Dibujo");
            UiKit.Rellenar(lienzo);
            lienzo.gameObject.AddComponent<CanvasRenderer>();
            var dibujo = lienzo.gameObject.AddComponent<DibujoUI>();
            dibujo.raycastTarget = false;
            var lado = 100f;
            DibujosDeSoluciones.Dibujar(dibujo, o.EsEstaBien ? DibujosDeSoluciones.EstaBien : o.Icono,
                                        (ancho - 2 * relleno - lado) / 2f, 9, lado, tema.ink, acento);

            // El nombre y para que sirve.
            c._titulo = Texto(ui, rt, o.Nombre ?? "", 21, tema.ink, TextAlignmentOptions.TopLeft);
            c._titulo.fontStyle = FontStyles.Bold;
            c._titulo.enableAutoSizing = true;
            c._titulo.fontSizeMin = 15;
            c._titulo.fontSizeMax = 21;
            Lamina.Colocar(c._titulo.rectTransform, relleno, 186, ancho - 2 * relleno, 54);
            if (!string.IsNullOrEmpty(o.Descripcion)) {
                var desc = Texto(ui, rt, o.Descripcion, 16, tema.inkMuted, TextAlignmentOptions.TopLeft);
                desc.enableAutoSizing = true;
                desc.fontSizeMin = 12;
                desc.fontSizeMax = 16;
                Lamina.Colocar(desc.rectTransform, relleno, 242, ancho - 2 * relleno, alto - 242 - relleno);
            }

            // La chapa «EN #2», escondida hasta que se juega.
            c._chapa = ui.Nodo(rt, "En uso");
            c._chapa.anchorMin = c._chapa.anchorMax = new Vector2(0.5f, 1f);
            c._chapa.pivot = new Vector2(0.5f, 0.5f);
            c._chapa.anchoredPosition = new Vector2(0, -26);
            c._chapa.sizeDelta = new Vector2(110, 34);
            c._chapa.localEulerAngles = new Vector3(0, 0, -6);
            var fondoChapa = c._chapa.gameObject.AddComponent<Image>();
            ui.Fondo(fondoChapa, tema.warning, 6);
            fondoChapa.raycastTarget = false;
            c._textoChapa = Texto(ui, c._chapa, "EN #1", 16, tema.onWarning, TextAlignmentOptions.Center);
            c._textoChapa.fontStyle = FontStyles.Bold;
            UiKit.Rellenar(c._textoChapa.rectTransform);
            c._chapa.gameObject.SetActive(false);
            return c;
        }

        private static TMP_Text Texto(UiKit ui, Transform padre, string texto, float tam, Color color, TextAlignmentOptions alineacion) {
            var t = ui.Texto(padre, texto, EstiloTexto.Cuerpo, color, alineacion);
            var alto = t.GetComponent<AltoDeTexto>();
            if (alto != null) Destroy(alto);
            t.fontSize = tam;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Lleva la carta hacia su sitio (suave) o la pone de golpe (factor &gt;= 1).</summary>
        public void Colocar(Vector2 destino, float angulo, float escala, bool deGolpe, float factor = 1) {
            var k = deGolpe ? 1f : Mathf.Clamp01(factor);
            _pos = Vector2.Lerp(_pos, destino, k);
            _ang = Mathf.LerpAngle(_ang, angulo, k);
            _escala = Mathf.Lerp(_escala, escala, k);
            Aplicar();
        }

        private void Aplicar() {
            if (Volando) return;
            var temblor = _sacudida > 0 ? Mathf.Sin(_sacudida * 60f) * 10f * _sacudida : 0;
            _rt.anchoredPosition = _pos + new Vector2(temblor, 0);
            _rt.localEulerAngles = new Vector3(0, 0, _ang);
            _rt.localScale = Vector3.one * (_escala + (_salto > 0 ? 0.16f * Curvas.Campana(1 - _salto) : 0));
        }

        private void Update() {
            var dt = Time.unscaledDeltaTime;
            if (_sacudida > 0) _sacudida = Mathf.Max(0, _sacudida - dt * 2.4f);
            if (_salto > 0) { _salto = Mathf.Max(0, _salto - dt / 0.42f); if (_salto == 0 && _brillo > 0.6f) _brillo = 0; }
            // El brillo del halo sigue a la que se mira; una carta jugada se apaga.
            var objetivo = EnUso > 0 ? 0.55f : 1f;
            _grupo.alpha = Mathf.MoveTowards(_grupo.alpha, Quemada ? 0 : objetivo, dt * 4f);
            var acento = Opcion.EsEstaBien ? _tema.success : _tema.cyan;
            _halo.color = NexusTheme.Alfa(acento, Mathf.MoveTowards(_halo.color.a, _brillo, dt * 4f));
        }

        public void OnPointerEnter(PointerEventData e) { if (!Quemada) { _brillo = 0.55f; _abanico.Entrar(this); } }
        public void OnPointerExit(PointerEventData e) { _brillo = 0; _abanico.Salir(this); }
        public void OnPointerClick(PointerEventData e) { if (!Quemada && !Volando) _abanico.Pulsar(this); }

        // Arrastrar la carta fuera de la mano (solo si el abanico lo permite): sigue al raton y, al soltarla, quien la juega decide.
        public void OnBeginDrag(PointerEventData e) {
            if (!_abanico.PermiteArrastrar || Quemada || Volando || Oculta) return;
            _arrastrando = true;
            Volando = true;   // la mano la deja en paz mientras se arrastra
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData e) {
            if (!_arrastrando) return;
            Vector3 mundo;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_rt, e.position, e.pressEventCamera, out mundo)) _rt.position = mundo;
            _pos = _rt.anchoredPosition;   // si no se juega, vuelve a su sitio deslizandose desde aqui
            _rt.localEulerAngles = Vector3.zero;
        }

        public void OnEndDrag(PointerEventData e) {
            if (!_arrastrando) return;
            _arrastrando = false;
            Volando = false;
            _abanico.Soltar(this, e);
        }

        /// <summary>No se puede (sin punto, o ya en uso): tiembla.</summary>
        public void Sacudir() { _sacudida = 1f; }

        public void MarcarEnUso(int numero) {
            EnUso = Opcion.EsEstaBien ? 0 : numero;
            _chapa.gameObject.SetActive(EnUso > 0);
            if (EnUso > 0) _textoChapa.text = "EN #" + EnUso;
            _borde.color = EnUso > 0 ? NexusTheme.Alfa(_tema.warning, 0.8f)
                         : Opcion.EsEstaBien ? NexusTheme.Alfa(_tema.success, 0.7f) : _tema.lineStrong;
        }

        /// <summary>Un saltito con giro: vuelve a estar libre.</summary>
        public void Saltar() {
            _salto = 1f;
            _brillo = 0.8f;
        }

        public void Quemar(Animador anim, Action alFin) {
            Volando = true;   // fuera del abanico: lo mueve la animacion
            var inicio = _rt.anchoredPosition;
            var color = _fondo.color;
            anim.Lanzar(0.8f, t => {
                if (this == null) return;
                _fondo.color = Color.Lerp(color, _tema.warning, Mathf.Clamp01(t * 2.5f));
                _halo.color = NexusTheme.Alfa(_tema.danger, 0.8f * Curvas.Campana(t));
                var temblor = (1 - t) * 6f * Mathf.Sin(t * 70f);
                _rt.anchoredPosition = inicio + new Vector2(temblor, t * 60f);
                _rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, Curvas.EntradaSalida(t));
                _rt.localEulerAngles = new Vector3(0, 0, t * 25f);
                _grupo.alpha = 1 - Curvas.EntradaSalida(Mathf.Clamp01((t - 0.3f) / 0.7f));
            }, () => {
                if (this == null) return;
                Quemada = true;
                gameObject.SetActive(false);
                alFin?.Invoke();
            }, 0, gameObject);
        }
    }
}
