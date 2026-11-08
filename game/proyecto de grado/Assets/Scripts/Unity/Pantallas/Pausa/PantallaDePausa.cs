using System;
using System.Collections.Generic;
using Nexus.Core.Tarjetas;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// La pausa, en cualquier momento de la partida (el dia, la Fase 1, un minijuego). Mientras esta abierta el reloj no
    /// corre, y desde aqui se puede leer el diario o guardar y salir: la partida se guarda tal cual y se retoma en el
    /// mismo sitio y con el mismo perfil (INV-7 garantiza que se recupera igual).
    ///
    /// Rediseñada el 2026-11-07 como una RED DE NODOS, como el logo del juego: en el centro, un nucleo con la hora y el
    /// dia; alrededor, un nodo por accion unido a el por una linea. «Seguir jugando» es el boton grande de abajo y
    /// «Reiniciar el nivel», un enlace discreto en la esquina.
    ///
    ///   ENTRADA  los nodos salen del nucleo, escalonados, y sus lineas crecen.
    ///   HOVER    del nucleo sale un circulito que recorre la linea; al llegar, el nodo se agranda (ver NodoDePausa).
    ///   SALIDA   al cerrar (Seguir, Escape, una ayuda o «Guardar y salir») los nodos vuelven al nucleo en remolino, el
    ///            nucleo se hincha y se apaga lanzando una onda, y el pie y el velo se desvanecen. SOLO ENTONCES se cierra
    ///            la pantalla y se hace lo que se pidio: lo que abre una pantalla encima (los apuntes de la guia) tiene que
    ///            esperar, o al volver se cerraria la equivocada.
    ///
    /// Teclado: Tab o ←/→/↑/↓ mueven el foco entre nodos, 1..5 los activan, Enter o Espacio activan el enfocado (o siguen
    /// jugando si no hay ninguno) y Escape cierra. Escape lo gestiona esta pantalla, no el router, para poder animar la
    /// salida. Con el foco, un nodo se ve igual que con el raton encima.
    /// </summary>
    public sealed class PantallaDePausa : Pantalla {
        /// <summary>Lo que la pantalla de debajo tiene que guardar antes de salir (el borrador de la Fase 1).</summary>
        public Action AntesDeSalir;
        /// <summary>Dentro de un minijuego: salir lo deja sin hacer (cuenta como no entregado).</summary>
        public string Aviso;

        public static bool Abierta { get; private set; }

        public override bool EsModal { get { return true; } }

        /// <summary>Escape lo trata la propia pausa (Teclado): el router cerraria sin la animacion de salida.</summary>
        public override bool PuedeVolver { get { return false; } }

        // Todo en unidades del lienzo (1920 x 1080), con el origen en el centro de la pantalla y la Y hacia arriba.
        private static readonly Vector2 TamanoDeLaRed = new Vector2(1400f, 900f);
        private const float CentroY = 40f, RadioDelNucleo = 160f, RadioX = 560f, RadioY = 250f;
        // AlfaDelVelo: cuanto tapa el juego de debajo. Con 0,84 las letras de los nodos se perdian sobre fondos claros.
        private const float Entrada = 0.4f, Escalon = 0.05f, AlfaDelVelo = 0.95f;
        /// <summary>Lo que se sube, ademas de la elipse, el nodo que cae en la vertical de arriba (con 3 o 5 nodos).</summary>
        private const float SubidaDelNodoDeArriba = 60f;
        // La salida: lo que tarda cada nodo, lo que se escalonan (el ultimo se va primero) y el remolino al volver.
        private const float SalidaDelNodo = 0.42f, EscalonDeSalida = 0.045f, GiroDeSalida = -75f;

        private sealed class Def {
            public PictogramasDePausa.Tipo Tipo;
            public string Rotulo, Nota;
            public Action Accion;
        }

        private readonly List<NodoDePausa> _nodos = new List<NodoDePausa>();
        private readonly List<Def> _defs = new List<Def>();
        private Image _velo, _haloDelNucleo;
        private RectTransform _pie, _nucleo, _haloRt, _onda;
        private CanvasGroup _grupoRed, _grupoPie, _grupoNucleo, _grupoOnda;
        private TMP_Text _nota;
        private int _foco = -1;
        private float _t, _ts, _alfaDelNucleo = 1f;
        private bool _lista, _cerrando, _eraLaDeArriba;
        private Action _alTerminar;

        protected override void Construir() {
            Abierta = true;
            if (App.Runner != null) App.Runner.Pausado = true;
            var s = App.Sesion;

            // El velo: oscurece lo de debajo (que se sigue viendo, y no se puede pulsar). Entra y sale con fundido.
            _velo = Raiz.gameObject.AddComponent<Image>();
            _velo.color = NexusTheme.Alfa(Tema.bg950, 0f);

            var red = Ui.Nodo(Raiz, "Red");
            Centrar(red, TamanoDeLaRed, Vector2.zero);
            _grupoRed = red.gameObject.AddComponent<CanvasGroup>();
            _grupoRed.alpha = 0f;
            _grupoRed.blocksRaycasts = false;   // hasta que termine de entrar: nada se pulsa a medio moverse

            ConstruirNucleo(red, s);
            ConstruirNodos(red);

            _pie = Ui.Nodo(Raiz, "Pie");
            UiKit.Rellenar(_pie);
            _grupoPie = _pie.gameObject.AddComponent<CanvasGroup>();
            _grupoPie.alpha = 0f;
            _grupoPie.blocksRaycasts = false;
            ConstruirPie(_pie, s);
        }

        // ==================================================================== el nucleo

        private void ConstruirNucleo(RectTransform red, Nexus.Core.Sesion.GameSession s) {
            // El halo que late va ANTES del nucleo, como hermano: en uGUI un hijo se dibuja encima de su padre y teñiria el disco.
            _haloRt = Ui.Nodo(red, "Halo del nucleo");
            Centrar(_haloRt, new Vector2(440f, 440f), new Vector2(0f, CentroY));
            _haloDelNucleo = _haloRt.gameObject.AddComponent<Image>();
            _haloDelNucleo.sprite = Ui.SpriteCircular();
            _haloDelNucleo.color = NexusTheme.Alfa(Tema.cyan, 0.15f);
            _haloDelNucleo.raycastTarget = false;

            // La onda que sale del nucleo al cerrar: un aro cian que se expande y se apaga. Oculta hasta entonces.
            _onda = Ui.Nodo(red, "Onda");
            Centrar(_onda, new Vector2(RadioDelNucleo * 2f, RadioDelNucleo * 2f), new Vector2(0f, CentroY));
            Ui.Borde(_onda, Tema.cyan, RadioDelNucleo / Tema.escala, 3f);
            _grupoOnda = _onda.gameObject.AddComponent<CanvasGroup>();
            _grupoOnda.alpha = 0f;
            _grupoOnda.blocksRaycasts = false;
            _onda.gameObject.SetActive(false);

            _nucleo = Ui.Nodo(red, "Nucleo");
            Centrar(_nucleo, new Vector2(RadioDelNucleo * 2f, RadioDelNucleo * 2f), new Vector2(0f, CentroY));
            _grupoNucleo = _nucleo.gameObject.AddComponent<CanvasGroup>();

            var disco = _nucleo.gameObject.AddComponent<Image>();
            disco.sprite = Ui.SpriteCircular();
            disco.color = NexusTheme.Alfa(Tema.surface, 0.96f);
            Ui.Borde(_nucleo, Tema.cyan, RadioDelNucleo / Tema.escala, 2.5f);

            Fijo(Ui.Texto(_nucleo, "PAUSA", EstiloTexto.Subtitulo, null, TextAlignmentOptions.Center), 260f, 36f, s == null ? 0f : 92f);
            if (s == null) return;

            if (s.Fase1Cerrada) {
                var hora = Fijo(Ui.Texto(_nucleo, s.HoraActual, EstiloTexto.Hero, Tema.ink, TextAlignmentOptions.Center), 280f, 72f, 34f);
                hora.fontSize = 56f;
                var mapa = s.Perfil.Mapa;
                var zona = mapa == null || mapa.Vacio ? null : mapa.PorId(s.ZonaActual);
                var dia = s.R.DiaActual == 0 ? "Antes del día 1" : "Día " + s.R.DiaActual;
                Fijo(Ui.Texto(_nucleo, dia + (zona != null ? " · " + zona.Nombre : ""), EstiloTexto.Etiqueta, Tema.ink, TextAlignmentOptions.Center),
                     250f, 28f, -34f);
                Fijo(Ui.Texto(_nucleo, "reloj parado", EstiloTexto.Leyenda, Tema.inkMuted, TextAlignmentOptions.Center), 250f, 24f, -68f);
            } else {
                var fase = Fijo(Ui.Texto(_nucleo, "FASE 1", EstiloTexto.Hero, Tema.ink, TextAlignmentOptions.Center), 280f, 72f, 34f);
                fase.fontSize = 48f;
                Fijo(Ui.Texto(_nucleo, "planificando", EstiloTexto.Etiqueta, Tema.ink, TextAlignmentOptions.Center), 250f, 28f, -34f);
                Fijo(Ui.Texto(_nucleo, "lo elegido se guarda", EstiloTexto.Leyenda, Tema.inkMuted, TextAlignmentOptions.Center), 250f, 24f, -68f);
            }
        }

        // ==================================================================== los nodos

        private void ConstruirNodos(RectTransform red) {
            foreach (var o in GuiaView.OpcionesDeAyuda(App)) {
                var opcion = o;
                var apuntes = opcion.Texto != null && opcion.Texto.StartsWith("Apuntes", StringComparison.Ordinal);
                _defs.Add(new Def {
                    Tipo = apuntes ? PictogramasDePausa.Tipo.Apuntes : PictogramasDePausa.Tipo.Ayuda,
                    Rotulo = apuntes ? "Apuntes" : "¿Qué hago?",
                    // La ayuda de la guia (si esta activa): se cierra la pausa (con su salida) y luego se hace lo suyo.
                    Accion = () => Cerrar(opcion.AlPulsar)
                });
            }
            _defs.Add(new Def {
                Tipo = PictogramasDePausa.Tipo.Diario, Rotulo = "Diario",
                Accion = () => App.Router.Apilar<PantallaDelDiario>()
            });
            _defs.Add(new Def {
                Tipo = PictogramasDePausa.Tipo.Opciones, Rotulo = "Opciones",
                Accion = () => App.Router.Apilar<PantallaDeOpciones>()
            });
            _defs.Add(new Def {
                Tipo = PictogramasDePausa.Tipo.Guardar, Rotulo = "Guardar y salir", Accion = () => Cerrar(Salir, false),
                Nota = "Al volver, elige tu perfil y pulsa «Continuar» en esta partida: sigues justo donde lo dejaste."
            });

            var puntos = DisposicionRadial.Calcular(_defs.Count, RadioX, RadioY);
            var centro = new Vector2(0f, CentroY);
            for (var i = 0; i < _defs.Count; i++) {
                var def = _defs[i];
                var destino = new Vector2(puntos[i].X, puntos[i].Y + CentroY);
                if (Mathf.Abs(puntos[i].X) < 1f && puntos[i].Y > 0f) destino.y += SubidaDelNodoDeArriba;
                var nodo = NodoDePausa.Crear(Ui, red, def.Tipo, def.Rotulo, i, centro, destino, centro, RadioDelNucleo,
                                             TamanoDeLaRed, def.Accion);
                nodo.AlEntrar = ponerFoco => { if (_cerrando) return; _foco = ponerFoco; AplicarFoco(); };
                nodo.AlSalir = quitarFoco => { if (_cerrando) return; if (_foco == quitarFoco) { _foco = -1; AplicarFoco(); } };
                _nodos.Add(nodo);
            }
        }

        // ==================================================================== el pie

        private void ConstruirPie(RectTransform pie, Nexus.Core.Sesion.GameSession s) {
            // El aviso de un minijuego («si sales ahora, el reto se entrega como este»): arriba, sobre la red.
            if (!string.IsNullOrEmpty(Aviso)) {
                var chip = Ui.Nodo(pie, "Aviso");
                Centrar(chip, new Vector2(900f, 56f), new Vector2(0f, 470f));   // sobre el nodo de arriba (su borde llega a y = 416)
                var fondo = chip.gameObject.AddComponent<Image>();
                Ui.Fondo(fondo, Tema.warningSoft, NexusTheme.RadioMd);
                fondo.raycastTarget = false;
                Ui.Borde(chip, Tema.warning, NexusTheme.RadioMd, 1f);
                var texto = Ui.Texto(chip, Aviso, EstiloTexto.Pequeno, Tema.warning, TextAlignmentOptions.Center);
                UiKit.Rellenar(texto.rectTransform, 12f);
                texto.overflowMode = TextOverflowModes.Ellipsis;
                texto.maxVisibleLines = 2;
            }

            // «Seguir jugando»: la accion principal, grande y dorada, abajo en el centro.
            var seguir = Ui.Boton(pie, "Seguir jugando", Seguir, VarianteBoton.Primario);
            Centrar((RectTransform)seguir.transform, new Vector2(440f, 88f), new Vector2(0f, -360f));
            var etiquetaSeguir = seguir.GetComponentInChildren<TMP_Text>();
            if (etiquetaSeguir != null) etiquetaSeguir.fontSize *= 1.25f;

            // La nota de «Guardar y salir»: en un hueco fijo, para que al aparecer no salte nada.
            _nota = Ui.Texto(pie, "", EstiloTexto.Leyenda, Tema.inkMuted, TextAlignmentOptions.Center);
            Fijo(_nota, 1000f, 30f, -445f);

            // «Reiniciar el nivel»: apartado, pequeño y en rojo; sigue pidiendo confirmacion.
            if (s == null) return;
            var reiniciar = Ui.Boton(pie, "Reiniciar el nivel", () => App.Router.Apilar<PantallaDeConfirmacion>(c => {
                c.Titulo = "¿Reiniciar el nivel?";
                c.Texto = "Vuelves a la Fase 1 de este nivel y se pierde lo jugado en él desde entonces. La entrevista y las escenas de apertura no se repiten.";
                c.TextoSi = "Reiniciar";
                c.AlConfirmar = () => FlujoDelNivel.ReiniciarNivel(App);
            }), VarianteBoton.Fantasma);
            var rr = (RectTransform)reiniciar.transform;
            rr.anchorMin = rr.anchorMax = new Vector2(1f, 0f);
            rr.pivot = new Vector2(1f, 0f);
            rr.sizeDelta = new Vector2(260f, 44f);
            rr.anchoredPosition = new Vector2(-48f, 40f);
            // El boton recolorea su etiqueta en cada estado: hay que cambiar los estados, no el color suelto.
            var visual = reiniciar.GetComponent<VisualDeBoton>();
            if (visual != null) {
                var normal = visual.Normal;
                normal.Texto = Tema.danger;
                visual.Normal = normal;
                // El hover del fantasma es cian: aqui, todo en rojo (fondo, borde y resplandor), que es un enlace de peligro.
                var encima = visual.Encima;
                encima.Texto = Color.Lerp(Tema.danger, Color.white, 0.3f);
                encima.Fondo = NexusTheme.Alfa(Tema.dangerSoft, 0.8f);
                encima.Borde = NexusTheme.Alfa(Tema.danger, 0.55f);
                visual.Encima = encima;
                if (visual.Halo != null) visual.Halo.color = Tema.danger;
                visual.Aplicar();
            }
        }

        // ==================================================================== acciones

        /// <summary>Seguir jugando: la salida animada, y al acabar se reanuda el reloj y se cierra la pausa.</summary>
        private void Seguir() { Cerrar(null); }

        /// <summary>
        /// Empieza la animacion de salida. Al acabar: si 'volver', se reanuda el reloj y se cierra esta pantalla; y despues
        /// se hace 'despues' (que puede abrir otra pantalla: por eso va DESPUES de cerrar). Se ignora mientras entra o si ya
        /// se esta cerrando.
        /// </summary>
        private void Cerrar(Action despues, bool volver = true) {
            if (_cerrando || !_lista) return;
            _cerrando = true;
            _ts = 0f;
            _foco = -1;
            AplicarFoco();
            _grupoRed.blocksRaycasts = false;
            _grupoPie.blocksRaycasts = false;
            _onda.gameObject.SetActive(true);
            _alTerminar = () => {
                if (volver) {
                    if (App.Runner != null) App.Runner.Pausado = false;
                    App.Router.Volver();
                }
                if (despues != null) despues();
            };
        }

        private void Salir() {
            if (AntesDeSalir != null) AntesDeSalir();
            App.CerrarPartida();
            App.MostrarInicio();
        }

        private void OnDestroy() {
            Abierta = false;
            if (App != null && App.Runner != null) App.Runner.Pausado = false;
        }

        // ==================================================================== foco

        /// <summary>Vuelve del diario o de las opciones: el foco empieza de nuevo.</summary>
        public override void AlMostrar() {
            base.AlMostrar();
            _foco = -1;
            AplicarFoco();
        }

        private void AplicarFoco() {
            for (var i = 0; i < _nodos.Count; i++) _nodos[i].Enfocar(i == _foco);
            if (_nota != null)
                _nota.text = _foco >= 0 && _foco < _defs.Count && !string.IsNullOrEmpty(_defs[_foco].Nota)
                    ? Nexus.Core.Narrativa.Glifos.Seguro(_defs[_foco].Nota) : "";
        }

        private void MoverFoco(int paso) {
            if (_nodos.Count == 0) return;
            _foco = _foco < 0 ? (paso > 0 ? 0 : _nodos.Count - 1) : (_foco + paso + _nodos.Count) % _nodos.Count;
            AplicarFoco();
        }

        // ==================================================================== cada fotograma

        private void Update() {
            var dt = Time.unscaledDeltaTime;

            if (_cerrando) AnimarSalida(dt);
            else AnimarEntrada(dt);

            // El halo del nucleo late despacio (y se apaga con el nucleo al cerrar).
            if (_haloDelNucleo != null)
                _haloDelNucleo.color = NexusTheme.Alfa(Tema.cyan, _alfaDelNucleo *
                    Mathf.Lerp(0.10f, 0.22f, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / 2.4f)));

            if (_lista && !_cerrando) Teclado();
            // Para Escape: solo cuenta si la pausa YA era la de arriba el fotograma anterior. Si no, el Escape que acaba
            // de cerrar el diario (que lo gestiona AppRoot) cerraria tambien la pausa en el mismo fotograma.
            _eraLaDeArriba = App.Router != null && App.Router.Actual == this;
        }

        /// <summary>Los nodos salen del nucleo escalonados; luego sube el pie.</summary>
        private void AnimarEntrada(float dt) {
            var total = Entrada + Escalon * Mathf.Max(0, _nodos.Count - 1);
            if (_t >= total) return;
            _t = Mathf.Min(total, _t + dt);
            for (var i = 0; i < _nodos.Count; i++) _nodos[i].Entrar((_t - i * Escalon) / Entrada);
            _velo.color = NexusTheme.Alfa(Tema.bg950, AlfaDelVelo * Mathf.Clamp01(_t / 0.2f));
            _grupoRed.alpha = Mathf.Clamp01(_t / 0.15f);
            _grupoPie.alpha = Mathf.Clamp01((_t - 0.25f) / 0.2f);
            if (_t < total) return;
            _lista = true;
            _velo.color = NexusTheme.Alfa(Tema.bg950, AlfaDelVelo);
            _grupoRed.alpha = _grupoPie.alpha = 1f;
            _grupoRed.blocksRaycasts = _grupoPie.blocksRaycasts = true;
        }

        /// <summary>
        /// La salida: los nodos vuelven al nucleo girando (el ultimo se va primero), el nucleo se hincha y se apaga lanzando
        /// una onda, el pie baja y se desvanece y el velo se aclara. Al acabar se hace lo pedido (Cerrar).
        /// </summary>
        private void AnimarSalida(float dt) {
            var n = _nodos.Count;
            var total = SalidaDelNodo + EscalonDeSalida * Mathf.Max(0, n - 1);
            _ts = Mathf.Min(total, _ts + dt);

            for (var i = 0; i < n; i++) {
                var inicio = (n - 1 - i) * EscalonDeSalida;
                _nodos[i].Salir(Mathf.Clamp01((_ts - inicio) / SalidaDelNodo), GiroDeSalida);
            }

            // El nucleo: se hincha un poco (tiron), luego se encoge hasta nada y se apaga a la mitad.
            var cp = Mathf.Clamp01((_ts - 0.2f) / Mathf.Max(0.01f, total - 0.2f));
            var escala = Mathf.Max(0f, 1f - EaseInBack(cp));
            _nucleo.localScale = _haloRt.localScale = Vector3.one * escala;
            _alfaDelNucleo = 1f - Mathf.Clamp01((cp - 0.45f) / 0.55f);
            _grupoNucleo.alpha = _alfaDelNucleo;

            // La onda: sale del nucleo nada mas empezar, crece y se apaga.
            var w = Mathf.Clamp01(_ts / 0.5f);
            _onda.localScale = Vector3.one * Mathf.Lerp(1f, 2.7f, 1f - Mathf.Pow(1f - w, 3f));
            _grupoOnda.alpha = 0.9f * (1f - w);

            // El pie baja y se desvanece enseguida; el velo se aclara hasta dejar ver el juego.
            var pp = Mathf.Clamp01(_ts / 0.3f);
            _grupoPie.alpha = 1f - pp;
            _pie.anchoredPosition = new Vector2(0f, -36f * pp);
            _velo.color = NexusTheme.Alfa(Tema.bg950, AlfaDelVelo * (1f - Mathf.Clamp01((_ts - 0.12f) / Mathf.Max(0.01f, total - 0.12f))));

            if (_ts < total || _alTerminar == null) return;
            var accion = _alTerminar;
            _alTerminar = null;
            accion();
        }

        private void Teclado() {
            var k = Keyboard.current;
            if (k == null) return;
            // Con el diario, las opciones o la confirmacion encima, las teclas son suyas.
            if (App.Router == null || App.Router.Actual != this) return;

            if (k.escapeKey.wasPressedThisFrame && _eraLaDeArriba) { Seguir(); return; }

            if (k.tabKey.wasPressedThisFrame) MoverFoco(k.shiftKey.isPressed ? -1 : +1);
            else if (k.rightArrowKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame) MoverFoco(+1);
            else if (k.leftArrowKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame) MoverFoco(-1);

            var numeros = new[] { k.digit1Key, k.digit2Key, k.digit3Key, k.digit4Key, k.digit5Key };
            for (var i = 0; i < numeros.Length && i < _nodos.Count; i++)
                if (numeros[i].wasPressedThisFrame) { _nodos[i].Activar(); return; }

            if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) {
                // Sin foco, Enter es «seguir»: nadie guarda y sale por accidente.
                if (_foco >= 0 && _foco < _nodos.Count) _nodos[_foco].Activar();
                else Seguir();
            }
        }

        // ==================================================================== ayudas

        private static void Centrar(RectTransform rt, Vector2 tamano, Vector2 posicion) {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = tamano;
            rt.anchoredPosition = posicion;
        }

        /// <summary>Un texto de una sola linea con tamaño y sitio fijos (sin layout): el nucleo y el pie no se recolocan.</summary>
        private static TMP_Text Fijo(TMP_Text texto, float ancho, float alto, float y) {
            texto.textWrappingMode = TextWrappingModes.NoWrap;
            texto.overflowMode = TextOverflowModes.Ellipsis;
            Centrar(texto.rectTransform, new Vector2(ancho, alto), new Vector2(0f, y));
            return texto;
        }

        /// <summary>Empieza yendo un poco hacia atras (el tiron antes de irse) y acaba en 1.</summary>
        private static float EaseInBack(float x) {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return c3 * x * x * x - c1 * x * x;
        }
    }
}
