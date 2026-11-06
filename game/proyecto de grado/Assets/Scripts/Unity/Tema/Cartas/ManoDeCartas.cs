using System;
using System.Collections.Generic;
using Nexus.Core.Tarjetas;
using Nexus.Unity.Aplicacion;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nexus.Unity.Tema {
    /// <summary>Una opcion de una mano de cartas.</summary>
    public sealed class OpcionDeCarta {
        public string Titulo;
        /// <summary>Una linea debajo del titulo (tenue).</summary>
        public string Detalle;
        /// <summary>Al pie de la carta, en cyan: el efecto que se adelanta, el coste, la confianza…</summary>
        public string Pie;
        /// <summary>Si no es null, la opcion esta bloqueada y esto dice por que.</summary>
        public string Bloqueo;
        /// <summary>Ya se probo y no era (el reto de comprension de la Fase 1): gris, sin poder elegirse.</summary>
        public bool Descartada;
        /// <summary>Una carta menor, mas apagada: «No lo sé».</summary>
        public bool Discreta;
        public Action AlElegir;
        /// <summary>
        /// Opcional: si dice false, la carta no se sella; se sacude y se apaga (y se llama AlElegir igual, que es quien
        /// anota el fallo). Para los retos que corrigen al momento.
        /// </summary>
        public Func<bool> EsCorrecta;
    }

    /// <summary>
    /// Las elecciones del juego como una MANO DE CARTAS (2026-11-05): antes eran listas de botones iguales, y el
    /// jugador las sentia aburridas y genericas.
    ///
    ///   repartir   las cartas llegan desde abajo una tras otra (60 ms de diferencia)
    ///   mirar      con el raton o el teclado, la carta se endereza, sube, crece (1,08) y brilla; las vecinas se apartan
    ///   elegir     «sello + giro»: la carta sube al centro, gira y enseña el reverso con un sello (DECIDIDO, DICHO,
    ///              ANOTADO…) que cae con un golpe de luz; las demas caen y se apagan. Un clic o Enter lo acortan.
    ///   teclado    1..9 (o A..D si la mano usa letras) eligen; ←/→ mueven el foco y Enter confirma
    ///
    /// La geometria vive en Core (DisposicionDeMano), probada; aqui solo se anima.
    /// </summary>
    public sealed class ManoDeCartas : MonoBehaviour {
        /// <summary>
        /// Quien decide si esta mano puede escuchar el teclado: la pone AppRoot (solo la pantalla de arriba). Sin ella,
        /// escucha siempre que este activa.
        /// </summary>
        public static Func<Transform, bool> PuedeEscuchar;

        public sealed class Ajustes {
            public string Sello = "DECIDIDO";
            public Color? ColorDelSello;
            /// <summary>A, B, C en vez de 1, 2, 3 (la entrevista).</summary>
            public bool ConLetras;
            /// <summary>Ancho maximo que puede ocupar la mano; las cartas se estrechan para caber.</summary>
            public float AnchoMaximo = 1040;
            public float AnchoDeCarta = 260;
            /// <summary>
            /// Si ya se eligio (se vuelve a pintar la pantalla despues de elegir), la mano se pinta directamente con esa
            /// carta sellada en el centro, sin animar y sin poder elegir otra.
            /// </summary>
            public int Elegida = -1;

            /// <summary>
            /// Una eleccion que se puede cambiar antes de seguir (la metodologia, la arquitectura, los motivos de la Fase 1):
            /// la carta elegida no se lleva la mano; se queda en su sitio con un sello pequeño en la esquina («ELEGIDA») y
            /// un golpe de luz, y las demas se atenuan. Otro clic en otra la cambia.
            /// </summary>
            public bool Seleccionable;
            /// <summary>En una mano Seleccionable: la que ya esta elegida (se pinta sin repartir de nuevo).</summary>
            public int Seleccionada = -1;
        }

        private const float Hueco = 18f, Rapidez = 1f / 0.18f;
        private const float DuracionDeEleccion = 0.85f;

        private UiKit _ui;
        private NexusTheme _tema;
        private Ajustes _ajustes;
        private readonly List<Carta> _cartas = new List<Carta>();
        private RectTransform _rt, _rafaga;
        private Image _imgRafaga;
        private float _anchoCarta, _altoCarta;
        private int _encima = -1, _elegida = -1;
        private float _t;              // tiempo desde que se reparte
        private float _tEleccion = -1; // tiempo desde que se eligio; -1 = no se ha elegido
        private bool _avisado;

        public bool Eligiendo { get { return _tEleccion >= 0; } }
        public RectTransform Raiz { get { return _rt; } }

        public static ManoDeCartas Crear(UiKit ui, Transform padre, IList<OpcionDeCarta> opciones, Ajustes ajustes = null) {
            var rt = ui.Nodo(padre, "Mano de cartas");
            var mano = rt.gameObject.AddComponent<ManoDeCartas>();
            // Son el contenido de la pantalla, no botones de navegacion: no cuentan para la regla de los 7 botones.
            rt.gameObject.AddComponent<ListaDeElementos>();
            mano.Construir(ui, rt, opciones, ajustes ?? new Ajustes());
            return mano;
        }

        private void Construir(UiKit ui, RectTransform rt, IList<OpcionDeCarta> opciones, Ajustes ajustes) {
            _ui = ui;
            _tema = ui.Tema;
            _rt = rt;
            _ajustes = ajustes;
            var n = opciones.Count;
            for (var i = 0; i < n; i++) _cartas.Add(Carta.Crear(this, i, opciones[i]));
            var yaElegida = ajustes.Elegida >= 0 && ajustes.Elegida < n;
            Dimensionar(ajustes.AnchoMaximo);

            _rafaga = ui.Nodo(rt, "Rafaga");
            _rafaga.sizeDelta = new Vector2(_anchoCarta, _anchoCarta);
            _imgRafaga = _rafaga.gameObject.AddComponent<Image>();
            _imgRafaga.sprite = Nexus.Unity.Pantallas.PlanoIlustrado.Brillo();
            _imgRafaga.raycastTarget = false;
            _imgRafaga.color = new Color(1, 1, 1, 0);

            if (ajustes.Seleccionable && ajustes.Seleccionada >= 0 && ajustes.Seleccionada < n) {
                // Ya repartida antes (la pantalla se repinta al elegir): sin volver a repartir, con su sello puesto.
                _t = 10f;
                _cartas[ajustes.Seleccionada].Marcar(1f);
            }
            if (yaElegida) {
                _elegida = ajustes.Elegida;
                _tEleccion = DuracionDeEleccion + 1;
                _avisado = true;
                for (var i = 0; i < n; i++) {
                    if (i == _elegida) _cartas[i].Sellada(Vector2.zero);
                    else _cartas[i].gameObject.SetActive(false);
                }
                return;
            }
            Colocar(true);
        }

        /// <summary>
        /// El tamaño de las cartas y de la mano para 'anchoMaximo': las cartas se estrechan hasta caber (minimo 150) y
        /// todas son igual de altas (manda la de mas texto).
        /// </summary>
        private void Dimensionar(float anchoMaximo) {
            var n = _cartas.Count;
            var enRejilla = DisposicionDeMano.EnRejilla(n);
            var porFila = enRejilla ? DisposicionDeMano.Columnas(n) : n;
            var margen = enRejilla ? 0 : 2 * DisposicionDeMano.Apertura + 24;
            _anchoCarta = Mathf.Min(_ajustes.AnchoDeCarta, (anchoMaximo - margen - (porFila - 1) * Hueco) / Mathf.Max(1, porFila));
            _anchoCarta = Mathf.Max(150, _anchoCarta);

            _altoCarta = 0;
            foreach (var c in _cartas) _altoCarta = Mathf.Max(_altoCarta, c.AltoNecesario(_anchoCarta));
            _altoCarta = Mathf.Clamp(_altoCarta, 170, 420);
            foreach (var c in _cartas) c.Medir(_anchoCarta, _altoCarta);

            float ancho, alto;
            DisposicionDeMano.Tamano(n, _anchoCarta, _altoCarta, Hueco, out ancho, out alto);
            if (_ajustes.Elegida >= 0 && _ajustes.Elegida < n) {
                // Elegida de antes: solo la carta sellada, al centro.
                ancho = _anchoCarta + 40;
                alto = _altoCarta + 30;
            }
            UiKit.Tamano(_rt, ancho, alto);
            _rt.sizeDelta = new Vector2(ancho, alto);
            if (_rafaga != null) _rafaga.sizeDelta = new Vector2(_anchoCarta, _anchoCarta);
        }

        /// <summary>
        /// En el primer fotograma ya maquetado se sabe cuanto mide de verdad el sitio donde va la mano: si es menos de
        /// lo supuesto, las cartas se estrechan para no salirse.
        /// </summary>
        private bool _ajustado;
        private void AjustarAlSitio() {
            if (_ajustado) return;
            var padre = _rt.parent as RectTransform;
            if (padre == null || padre.rect.width < 10) return;
            _ajustado = true;
            if (padre.rect.width < _rt.sizeDelta.x - 1) Dimensionar(padre.rect.width);
        }

        // ==================================================================== lo que hace el jugador

        internal void Entrar(int i) { if (!Eligiendo) Mirar(i); }
        internal void Salir(int i) { if (!Eligiendo && _encima == i) Mirar(-1); }

        private void Mirar(int i) {
            if (_encima == i) return;
            _encima = i;
            // La que se mira se pinta encima de las demas; las otras, en su orden.
            for (var k = 0; k < _cartas.Count; k++) _cartas[k].transform.SetSiblingIndex(k);
            if (i >= 0) _cartas[i].transform.SetAsLastSibling();
            _rafaga.SetAsLastSibling();
        }

        internal void Pulsar(int i) {
            if (Eligiendo) { Acortar(); return; }
            var c = _cartas[i];
            if (!c.Elegible) { c.Sacudir(); return; }
            var opcion = c.Opcion;
            if (_ajustes.Seleccionable) {
                if (_marcando >= 0 || i == _ajustes.Seleccionada) return;
                // El sello pequeño cae sobre la carta, con su golpe de luz; despues avisa (y la pantalla se repinta).
                _marcando = i;
                _tMarca = 0;
                return;
            }
            if (opcion.EsCorrecta != null && !opcion.EsCorrecta()) {
                // Un fallo en un reto que corrige: no se sella. Se sacude, se apaga, y quien corrige anota el fallo cuando
                // ya se ha visto la sacudida (si repintara enseguida, no se veria).
                c.Descartar();
                _alFallar = opcion.AlElegir;
                _esperaDelFallo = 0.36f;
                return;
            }
            _elegida = i;
            _tEleccion = 0;
            Mirar(i);
        }

        private void Acortar() {
            if (_tEleccion >= 0 && _tEleccion < DuracionDeEleccion) _tEleccion = DuracionDeEleccion;
        }

        private void Update() {
            var dt = Time.unscaledDeltaTime;
            AjustarAlSitio();
            _t += dt;
            Teclado();
            if (_avisado) return;
            if (Eligiendo) Elegir(dt);
            else Colocar(false);
            if (_marcando >= 0) Marcando(dt);
            if (_alFallar != null) {
                _esperaDelFallo -= dt;
                if (_esperaDelFallo <= 0) {
                    var avisar = _alFallar;
                    _alFallar = null;
                    avisar();
                }
            }
        }

        private Action _alFallar;
        private float _esperaDelFallo;

        private int _marcando = -1;
        private float _tMarca;
        private const float DuracionDeMarca = 0.38f;

        /// <summary>En una mano Seleccionable: el sello de la esquina cae, la luz estalla, y se avisa.</summary>
        private void Marcando(float dt) {
            _tMarca += dt;
            var c = _cartas[_marcando];
            c.Marcar(Mathf.Clamp01(_tMarca / 0.2f));
            var r = Mathf.Clamp01((_tMarca - 0.12f) / 0.3f);
            if (r > 0 && r < 1) {
                _rafaga.SetAsLastSibling();
                _rafaga.anchoredPosition = c.Posicion;
                _rafaga.localScale = Vector3.one * Mathf.Lerp(0.5f, 2.6f, r);
                _imgRafaga.color = NexusTheme.Alfa(_ajustes.ColorDelSello ?? _tema.cyan, 0.7f * (1 - r));
            }
            if (_tMarca < DuracionDeMarca) return;
            _imgRafaga.color = new Color(1, 1, 1, 0);
            var i = _marcando;
            _marcando = -1;
            _ajustes.Seleccionada = i;
            _cartas[i].Opcion.AlElegir?.Invoke();
        }

        private static readonly Key[] Digitos = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        private static readonly Key[] Numericos = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };
        private static readonly Key[] Letras = { Key.A, Key.B, Key.C, Key.D };

        private void Teclado() {
            var teclado = Keyboard.current;
            if (teclado == null || _cartas.Count == 0 || _avisado) return;
            if (PuedeEscuchar != null && !PuedeEscuchar(transform)) return;
            var enter = teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame;
            if (Eligiendo) {
                if (enter || teclado.spaceKey.wasPressedThisFrame) Acortar();
                return;
            }
            for (var i = 0; i < _cartas.Count && i < Digitos.Length; i++) {
                var letra = _ajustes.ConLetras && i < Letras.Length && teclado[Letras[i]].wasPressedThisFrame;
                if (teclado[Digitos[i]].wasPressedThisFrame || teclado[Numericos[i]].wasPressedThisFrame || letra) { Pulsar(i); return; }
            }
            if (teclado.rightArrowKey.wasPressedThisFrame) Mirar(Mathf.Min(_cartas.Count - 1, _encima + 1));
            else if (teclado.leftArrowKey.wasPressedThisFrame) Mirar(_encima <= 0 ? 0 : _encima - 1);
            else if (enter && _encima >= 0) Pulsar(_encima);
        }

        /// <summary>Las cartas van hacia su sitio en la mano (y, al principio, llegan repartidas).</summary>
        private void Colocar(bool deGolpe) {
            var lugares = DisposicionDeMano.Calcular(_cartas.Count, _anchoCarta, _altoCarta, Hueco, _encima);
            var enRejilla = DisposicionDeMano.EnRejilla(_cartas.Count);
            for (var i = 0; i < _cartas.Count; i++) {
                var l = lugares[i];
                var llegada = Mathf.Clamp01((_t - i * 0.06f) / 0.35f);
                var e = 1 - Mathf.Pow(1 - llegada, 3);
                var destino = new Vector2(l.X, l.Y - (1 - e) * 260f);
                var escala = i == _encima ? 1.08f : 1f;
                if (enRejilla && i == _encima) destino.y += 6;
                _cartas[i].Ir(destino, l.Angulo * e, escala, e, deGolpe || llegada < 1);
            }
        }

        /// <summary>«Sello + giro»: sube, gira, cae el sello con un golpe de luz; las demas se van.</summary>
        private void Elegir(float dt) {
            _tEleccion += dt;
            var t = _tEleccion;
            for (var i = 0; i < _cartas.Count; i++) {
                if (i == _elegida) continue;
                // Las demas caen, se inclinan hacia fuera y se apagan.
                _cartas[i].Caer(Mathf.Clamp01(t / 0.45f), Mathf.Sign(i - _elegida));
            }
            var elegida = _cartas[_elegida];
            var subir = Mathf.Clamp01(t / 0.2f);
            var giro = Mathf.Clamp01((t - 0.2f) / 0.3f);
            elegida.Girar(Vector2.Lerp(elegida.Posicion, new Vector2(0, 10), 1 - Mathf.Pow(1 - subir, 2)), giro);

            // El sello cae cuando ya se ve el reverso.
            elegida.Sello(Mathf.Clamp01((t - 0.5f) / 0.15f));

            // El golpe de luz.
            var r = Mathf.Clamp01((t - 0.58f) / 0.4f);
            if (r > 0 && r < 1) {
                _rafaga.anchoredPosition = elegida.Posicion;
                _rafaga.localScale = Vector3.one * Mathf.Lerp(0.6f, 3.6f, r);
                _imgRafaga.color = NexusTheme.Alfa(_ajustes.ColorDelSello ?? _tema.cyan, 0.85f * (1 - r));
            } else if (_imgRafaga.color.a != 0) {
                _imgRafaga.color = new Color(1, 1, 1, 0);
            }

            if (t >= DuracionDeEleccion && !_avisado) {
                _avisado = true;
                _imgRafaga.color = new Color(1, 1, 1, 0);
                for (var i = 0; i < _cartas.Count; i++) if (i != _elegida) _cartas[i].gameObject.SetActive(false);
                elegida.Sellada(new Vector2(0, 10));
                // Lo ultimo: quien llama suele repintar, y esta mano desaparece con ello.
                elegida.Opcion.AlElegir?.Invoke();
            }
        }

        // ==================================================================== una carta

        internal sealed class Carta : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
            public OpcionDeCarta Opcion { get; private set; }
            public Vector2 Posicion { get { return _rt.anchoredPosition; } }
            public bool Elegible { get { return Opcion.Bloqueo == null && !Opcion.Descartada && !_descartada; } }

            private ManoDeCartas _mano;
            private int _indice;
            private RectTransform _rt, _anverso, _reverso, _sello;
            private CanvasGroup _grupo, _grupoSello;
            private Image _fondo, _halo, _borde;
            private TMP_Text _titulo, _detalle, _pie;
            private float _angulo, _escala = 1, _foco, _sacudida;
            private bool _descartada, _golpeado, _marcada;
            private RectTransform _marca;
            private CanvasGroup _grupoMarca;

            /// <summary>El sello pequeño de la esquina (manos Seleccionables): f 0→1 cae de grande a su sitio.</summary>
            public void Marcar(float f) {
                if (_marca == null) return;
                _marcada = true;
                var e = 1 - Mathf.Pow(1 - f, 3);
                _marca.localScale = Vector3.one * Mathf.Lerp(1.9f, 1f, e);
                _grupoMarca.alpha = e;
            }

            private const float Relleno = 16, BandaDeTecla = 52;

            public static Carta Crear(ManoDeCartas mano, int indice, OpcionDeCarta o) {
                var ui = mano._ui;
                var tema = mano._tema;
                var rt = ui.Nodo(mano._rt, "Carta " + (indice + 1));
                rt.pivot = new Vector2(0.5f, 0.5f);
                var c = rt.gameObject.AddComponent<Carta>();
                c._mano = mano;
                c._indice = indice;
                c._rt = rt;
                c.Opcion = o;
                c._grupo = rt.gameObject.AddComponent<CanvasGroup>();
                c._grupo.alpha = 0;

                c._fondo = rt.gameObject.AddComponent<Image>();
                ui.Fondo(c._fondo, o.Discreta ? tema.bg900 : tema.surface, NexusTheme.RadioLg);
                c._fondo.raycastTarget = true;
                c._halo = ui.Halo(rt, NexusTheme.Alfa(tema.cyan, 0), NexusTheme.RadioLg);
                c._borde = ui.Borde(rt, o.Discreta ? tema.line : tema.lineStrong, NexusTheme.RadioLg, o.Discreta ? 1 : 1.5f);

                // ---- el anverso
                c._anverso = ui.Nodo(rt, "Anverso");
                UiKit.Rellenar(c._anverso);
                var tecla = ui.Nodo(c._anverso, "Tecla");
                tecla.anchorMin = tecla.anchorMax = tecla.pivot = new Vector2(0, 1);
                tecla.anchoredPosition = new Vector2(14, -14);
                tecla.sizeDelta = new Vector2(30, 30);
                var fondoTecla = tecla.gameObject.AddComponent<Image>();
                ui.Fondo(fondoTecla, tema.bg950, 6);
                fondoTecla.raycastTarget = false;
                ui.Borde(tecla, NexusTheme.Alfa(tema.cyan, 0.7f), 6, 1);
                var rotulo = Texto(ui, tecla, TeclasDeEleccion.Rotulo(indice, mano._ajustes.ConLetras), EstiloTexto.Mono, tema.cyan, TextAlignmentOptions.Center);
                rotulo.textWrappingMode = TextWrappingModes.NoWrap;
                UiKit.Rellenar(rotulo.rectTransform);

                c._titulo = Texto(ui, c._anverso, o.Titulo ?? "", o.Discreta ? EstiloTexto.Cuerpo : EstiloTexto.Encabezado,
                                  o.Discreta ? tema.inkMuted : tema.ink, TextAlignmentOptions.TopLeft);
                if (!string.IsNullOrEmpty(o.Detalle))
                    c._detalle = Texto(ui, c._anverso, o.Detalle, EstiloTexto.Pequeno, tema.inkMuted, TextAlignmentOptions.TopLeft);
                var pie = o.Bloqueo != null ? "Bloqueada · " + o.Bloqueo : o.Pie;
                if (!string.IsNullOrEmpty(pie))
                    c._pie = Texto(ui, c._anverso, pie, EstiloTexto.Pequeno, o.Bloqueo != null ? tema.warning : tema.cyan, TextAlignmentOptions.BottomLeft);

                // ---- el reverso (con el sello), oculto hasta el giro
                c._reverso = ui.Nodo(rt, "Reverso");
                UiKit.Rellenar(c._reverso);
                var fondoReverso = c._reverso.gameObject.AddComponent<Image>();
                ui.Fondo(fondoReverso, tema.bg900, NexusTheme.RadioLg);
                fondoReverso.raycastTarget = false;
                var resumen = Texto(ui, c._reverso, o.Titulo ?? "", EstiloTexto.Pequeno, tema.inkMuted, TextAlignmentOptions.Bottom);
                var rr = resumen.rectTransform;
                rr.anchorMin = new Vector2(0, 0);
                rr.anchorMax = new Vector2(1, 0.4f);
                rr.offsetMin = new Vector2(Relleno, 14);
                rr.offsetMax = new Vector2(-Relleno, 0);
                resumen.overflowMode = TextOverflowModes.Ellipsis;

                var colorSello = mano._ajustes.ColorDelSello ?? tema.cyan;
                c._sello = ui.Nodo(c._reverso, "Sello");
                c._sello.anchorMin = c._sello.anchorMax = new Vector2(0.5f, 0.62f);
                c._sello.localEulerAngles = new Vector3(0, 0, 12);
                var marco = c._sello.gameObject.AddComponent<Image>();
                ui.Fondo(marco, NexusTheme.Alfa(colorSello, 0.12f), 6);
                marco.raycastTarget = false;
                ui.Borde(c._sello, colorSello, 6, 3);
                var textoSello = Texto(ui, c._sello, mano._ajustes.Sello, EstiloTexto.Subtitulo, colorSello, TextAlignmentOptions.Center);
                textoSello.fontStyle = FontStyles.Bold;
                textoSello.characterSpacing = 6;
                textoSello.textWrappingMode = TextWrappingModes.NoWrap;
                UiKit.Rellenar(textoSello.rectTransform);
                c._sello.sizeDelta = new Vector2(textoSello.GetPreferredValues(mano._ajustes.Sello).x + 40, 54);
                c._grupoSello = c._sello.gameObject.AddComponent<CanvasGroup>();
                c._grupoSello.alpha = 0;
                c._reverso.gameObject.SetActive(false);

                // El sello pequeño de las manos Seleccionables, en la esquina de arriba a la derecha del anverso.
                if (mano._ajustes.Seleccionable) {
                    c._marca = ui.Nodo(c._anverso, "Elegida");
                    c._marca.anchorMin = c._marca.anchorMax = new Vector2(1, 1);
                    c._marca.pivot = new Vector2(1, 1);
                    c._marca.anchoredPosition = new Vector2(-10, -12);
                    c._marca.localEulerAngles = new Vector3(0, 0, -8);
                    var fondoMarca = c._marca.gameObject.AddComponent<Image>();
                    ui.Fondo(fondoMarca, NexusTheme.Alfa(colorSello, 0.15f), 4);
                    fondoMarca.raycastTarget = false;
                    ui.Borde(c._marca, colorSello, 4, 2);
                    var textoMarca = Texto(ui, c._marca, "ELEGIDA", EstiloTexto.Leyenda, colorSello, TextAlignmentOptions.Center);
                    textoMarca.fontStyle = FontStyles.Bold;
                    textoMarca.characterSpacing = 4;
                    textoMarca.textWrappingMode = TextWrappingModes.NoWrap;
                    UiKit.Rellenar(textoMarca.rectTransform);
                    c._marca.sizeDelta = new Vector2(textoMarca.GetPreferredValues("ELEGIDA").x + 22, 28);
                    c._grupoMarca = c._marca.gameObject.AddComponent<CanvasGroup>();
                    c._grupoMarca.alpha = 0;
                }

                if (!c.Elegible) {
                    c._fondo.color = NexusTheme.Alfa(tema.bg900, 0.9f);
                    c._titulo.color = tema.inkFaint;
                }
                return c;
            }

            private static TMP_Text Texto(UiKit ui, Transform padre, string texto, EstiloTexto estilo, Color color, TextAlignmentOptions alineacion) {
                var t = ui.Texto(padre, texto, estilo, color, alineacion);
                // Sin el «alto minimo = alto real» de UiKit: aqui cada texto tiene su caja fija dentro de la carta.
                var alto = t.GetComponent<AltoDeTexto>();
                if (alto != null) Destroy(alto);
                t.textWrappingMode = TextWrappingModes.Normal;
                return t;
            }

            /// <summary>Lo que necesita de alto esta carta con este ancho, para que quepa su texto.</summary>
            public float AltoNecesario(float ancho) {
                var w = ancho - 2 * Relleno;
                var alto = BandaDeTecla + _titulo.GetPreferredValues(_titulo.text, w, 0).y;
                if (_detalle != null) alto += 8 + _detalle.GetPreferredValues(_detalle.text, w, 0).y;
                if (_pie != null) alto += 12 + _pie.GetPreferredValues(_pie.text, w, 0).y;
                return alto + Relleno + 6;
            }

            /// <summary>Con el tamaño definitivo, cada texto a su caja: titulo arriba, detalle debajo, pie abajo del todo.</summary>
            public void Medir(float ancho, float alto) {
                _rt.sizeDelta = new Vector2(ancho, alto);
                var w = ancho - 2 * Relleno;
                var y = BandaDeTecla;
                var hTitulo = _titulo.GetPreferredValues(_titulo.text, w, 0).y;
                Caja(_titulo.rectTransform, y, hTitulo);
                y += hTitulo + 8;
                if (_detalle != null) Caja(_detalle.rectTransform, y, _detalle.GetPreferredValues(_detalle.text, w, 0).y);
                if (_pie != null) {
                    var h = _pie.GetPreferredValues(_pie.text, w, 0).y;
                    var rt = _pie.rectTransform;
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(1, 0);
                    rt.pivot = new Vector2(0.5f, 0);
                    rt.offsetMin = new Vector2(Relleno, Relleno);
                    rt.offsetMax = new Vector2(-Relleno, Relleno + h);
                }
            }

            private static void Caja(RectTransform rt, float desdeArriba, float alto) {
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.offsetMin = new Vector2(Relleno, -desdeArriba - alto);
                rt.offsetMax = new Vector2(-Relleno, -desdeArriba);
            }

            public void OnPointerEnter(PointerEventData e) { _mano.Entrar(_indice); }
            public void OnPointerExit(PointerEventData e) { _mano.Salir(_indice); }
            public void OnPointerClick(PointerEventData e) { _mano.Pulsar(_indice); }

            public void Sacudir() { _sacudida = 0.32f; }

            public void Descartar() {
                _descartada = true;
                Sacudir();
                var tema = _mano._tema;
                _fondo.color = NexusTheme.Alfa(tema.bg900, 0.9f);
                _titulo.color = tema.inkFaint;
                _borde.color = NexusTheme.Alfa(tema.danger, 0.6f);
            }

            /// <summary>Hacia su sitio en la mano: posicion, giro, escala y opacidad, suavizado (o de golpe).</summary>
            public void Ir(Vector2 destino, float angulo, float escala, float alfa, bool deGolpe) {
                var dt = Time.unscaledDeltaTime;
                var k = deGolpe ? 1f : 1 - Mathf.Exp(-Rapidez * 1.6f * dt);
                var pos = Vector2.Lerp(_rt.anchoredPosition, destino, k);
                _angulo = Mathf.Lerp(_angulo, angulo, k);
                _escala = Mathf.Lerp(_escala, escala, k);
                var mirando = _mano._encima == _indice;
                _foco = Mathf.MoveTowards(_foco, mirando ? 1 : 0, dt * Rapidez);
                if (_sacudida > 0) {
                    _sacudida -= dt;
                    pos.x += Mathf.Sin(Time.unscaledTime * 70f) * 8f * Mathf.Max(0, _sacudida) / 0.32f;
                }
                _rt.anchoredPosition = pos;
                _rt.localEulerAngles = new Vector3(0, 0, _angulo);
                _rt.localScale = Vector3.one * _escala;
                var tema = _mano._tema;
                // En una mano Seleccionable con una ya elegida: la elegida brilla siempre; las demas, atenuadas.
                var hayElegida = _mano._ajustes.Seleccionable && _mano._ajustes.Seleccionada >= 0;
                var brillo = Mathf.Max(_foco, _marcada ? 0.75f : 0f);
                _halo.color = NexusTheme.Alfa(Elegible ? tema.cyan : tema.inkFaint, 0.8f * brillo);
                var atenuada = hayElegida && !_marcada ? Mathf.Lerp(0.6f, 1f, _foco) : 1f;
                _grupo.alpha = alfa * (Elegible ? 1f : 0.6f) * atenuada;
                if (!_descartada && Elegible) _borde.color = Color.Lerp(Opcion.Discreta ? tema.line : tema.lineStrong, tema.cyan, _foco);
            }

            /// <summary>Las que no se eligieron: caen, se inclinan hacia fuera y se apagan.</summary>
            public void Caer(float f, float lado) {
                var e = f * f;
                var dt = Time.unscaledDeltaTime;
                var p = _rt.anchoredPosition;
                _rt.anchoredPosition = new Vector2(p.x + lado * 40f * dt, p.y - 600f * e * dt);
                _rt.localEulerAngles = new Vector3(0, 0, _angulo - lado * 18f * e);
                _grupo.alpha = 1 - f;
                _grupo.blocksRaycasts = false;
            }

            /// <summary>La elegida: hacia el centro, y girando sobre su eje vertical (a mitad de giro cambia de cara).</summary>
            public void Girar(Vector2 pos, float giro) {
                _rt.anchoredPosition = pos;
                var angulo = Mathf.Lerp(_angulo, 0, giro);
                _rt.localEulerAngles = new Vector3(0, 0, angulo);
                var escala = Mathf.Lerp(_escala, 1.12f, Mathf.Clamp01(giro * 2));
                var ancho = Mathf.Abs(Mathf.Cos(giro * Mathf.PI));
                _rt.localScale = new Vector3(escala * Mathf.Max(0.02f, ancho), escala, 1);
                var reverso = giro >= 0.5f;
                if (_reverso.gameObject.activeSelf != reverso) {
                    _reverso.gameObject.SetActive(reverso);
                    _anverso.gameObject.SetActive(!reverso);
                }
                _halo.color = NexusTheme.Alfa(_mano._ajustes.ColorDelSello ?? _mano._tema.cyan, 0.9f);
                _grupo.alpha = 1;
            }

            /// <summary>El sello cae: grande y transparente → en su sitio, con un pequeño temblor de la carta al llegar.</summary>
            public void Sello(float f) {
                if (f <= 0) return;
                var e = 1 - Mathf.Pow(1 - f, 3);
                _sello.localScale = Vector3.one * Mathf.Lerp(1.7f, 1f, e);
                _grupoSello.alpha = e;
                if (f >= 1 && !_golpeado) {
                    _golpeado = true;
                    _sacudida = 0.18f;
                }
                if (_sacudida > 0) {
                    _sacudida -= Time.unscaledDeltaTime;
                    var p = _rt.anchoredPosition;
                    _rt.anchoredPosition = new Vector2(p.x + Mathf.Sin(Time.unscaledTime * 90f) * 3f, p.y);
                }
            }

            /// <summary>Directamente en su estado final: reverso con el sello, en el centro.</summary>
            public void Sellada(Vector2 pos) {
                _rt.anchoredPosition = pos;
                _rt.localEulerAngles = Vector3.zero;
                _rt.localScale = Vector3.one * 1.04f;
                _anverso.gameObject.SetActive(false);
                _reverso.gameObject.SetActive(true);
                _sello.localScale = Vector3.one;
                _grupoSello.alpha = 1;
                _grupo.alpha = 1;
                _grupo.blocksRaycasts = false;
                _halo.color = NexusTheme.Alfa(_mano._ajustes.ColorDelSello ?? _mano._tema.cyan, 0.5f);
            }
        }
    }
}
