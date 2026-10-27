using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Mundo3D.Inventario;
using Mundo3D.UI;
using Nexus.Core.Fase1;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Nexus.Mundo3D {
    /// <summary>
    /// Lo que el juego pone ENCIMA del mundo 3D mientras dura el recorrido: un cartel con la fase y los controles, y la
    /// pausa (MenuDePausa3D). Y es tambien quien trae y se lleva ese mundo:
    ///
    ///   al abrirse     carga la escena del recorrido JUNTO a la del juego (aditiva: AppRoot, la sesion y la pila
    ///                  de pantallas no se tocan), apaga la camara del juego, quita el fondo opaco del lienzo y pone
    ///                  en marcha el reloj del recorrido
    ///   mientras dura  la pausa (Esc) y el inventario (I) congelan el mundo y paran el reloj
    ///   al terminar    cuando el reloj llega a cero (o el jugador lo termina desde la pausa) lee el inventario,
    ///                  lo traduce a un ResultadoDeRecoleccion, descarga la escena, deja todo como estaba, se cierra
    ///                  y entrega el resultado
    ///
    /// La pantalla de la Fase 1 queda debajo, apagada, y vuelve a encenderse al cerrar esta.
    /// </summary>
    public sealed class PantallaDeRecoleccion3D : Pantalla {
        /// <summary>
        /// El nivel de calidad con el que se pinta el mundo 3D: su propio perfil de URP, mas barato que el del juego
        /// (Nexus > Mundo 3D > 4 · Configurar el render del 3D). Si no existe, se pinta con el del juego.
        /// </summary>
        public const string NivelDeCalidad = "Mundo 3D";

        /// <summary>Los objetos de la escena que traian su propia pausa (Esc → un cartel): ahora la pone esta pantalla.</summary>
        private static readonly string[] PausaDeLaEscena = { "NKey (Pause)", "Msg (Pausa)" };

        private const string MotivoPausa = "pausa", MotivoInventario = "inventario";

        public EntradaDeRecoleccion Entrada;
        public Action<ResultadoDeRecoleccion> AlTerminar;

        /// <summary>Escape aqui no vuelve atras: en el mundo 3D abre la pausa.</summary>
        public override bool PuedeVolver { get { return false; } }

        private readonly List<Behaviour> _apagados = new List<Behaviour>();
        private readonly MedidorDeRendimiento _medidor = new MedidorDeRendimiento();
        private readonly HashSet<string> _congelado = new HashSet<string>();
        private readonly List<KeyValuePair<CinemachineFreeLook, Vector2>> _camaras = new List<KeyValuePair<CinemachineFreeLook, Vector2>>();
        private RectTransform _cartel;
        private TMP_Text _estado, _fps;
        private MenuDePausa3D _menu;
        private GameTimerUI _reloj;
        private InventoryController _inventario;
        // La escena trae MAS de un InventoryPanelUI (uno en el boton que abre, otro en el que cierra): se escuchan todos.
        private readonly List<InventoryPanelUI> _panelesDeInventario = new List<InventoryPanelUI>();
        private GameObject _jugador;
        private bool _escenaCargada, _fondoQuitado, _terminando, _menuTrasInventario;
        private bool _cursorCapturadoAntes;
        private float _escalaDeTiempoAntes = 1f;
        private float _volumenDelJuego = -1;   // el volumen general al entrar; -1 = no se ha tocado
        private int _calidadAnterior = -1;
        private const float DuracionDeLaGuia = 10f;
        private float _cartelVisible = DuracionDeLaGuia;
        private CanvasGroup _guia;
        private float _guiaEnPantalla;
        private BarraView _cuentaDeLaGuia;

        protected override void Construir() {
            // La raiz no tiene imagen: no tapa el mundo ni se queda con sus clics.
            // La guia va ABAJO A LA IZQUIERDA, el unico rincon libre del HUD del 3D (arriba esta el reloj, arriba a la
            // derecha el inventario y abajo en el centro la barra de objetos). Teclas dibujadas como teclas y una
            // linea de objetivo: se lee de un vistazo. Se va sola; la lista entera esta en Pausa → Controles.
            _cartel = Ui.PanelColumna(Raiz, "Guia del recorrido", Tema.Espacio(4), Tema.Espacio(2), NexusTheme.Alfa(Tema.bg950, 0.78f));
            _cartel.anchorMin = _cartel.anchorMax = _cartel.pivot = Vector2.zero;
            _cartel.anchoredPosition = new Vector2(Tema.margen, Tema.margen);
            _cartel.sizeDelta = new Vector2(360, 0);
            _cartel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _guia = _cartel.gameObject.AddComponent<CanvasGroup>();
            _guia.blocksRaycasts = false;   // solo informa: no se queda con clics
            _guia.alpha = 0;

            Ui.Texto(_cartel, "FASE 1 · RECORRIDO", EstiloTexto.Leyenda, Tema.cyan);
            _estado = Ui.Texto(_cartel, "Cargando el recorrido…", EstiloTexto.Pequeno, Tema.ink);
            var teclas = Ui.Columna(_cartel, "Teclas", Tema.Espacio(1));
            Tecla(teclas, "W A S D", "Moverte");
            Tecla(teclas, "Espacio", "Saltar");
            Tecla(teclas, "E", "Abrir puertas");
            Tecla(teclas, "I", "Inventario");
            Tecla(teclas, "Esc", "Pausa y controles");
            // Cuanto le queda a la guia en pantalla: se ve que se va a ir, y no sorprende.
            _cuentaDeLaGuia = Ui.Barra(_cartel, 1f, Tema.cyan);
            UiKit.Tamano(_cuentaDeLaGuia, alto: 3);
            _fps = Ui.Texto(Raiz, "", EstiloTexto.Mono, Tema.inkMuted);
            var fps = _fps.rectTransform;
            fps.anchorMin = fps.anchorMax = fps.pivot = new Vector2(1, 0);
            fps.anchoredPosition = new Vector2(-Tema.margen, Tema.margen);
            fps.sizeDelta = new Vector2(320, 40);
            _fps.alignment = TextAlignmentOptions.BottomRight;
            _fps.gameObject.SetActive(false);

            _menu = MenuDePausa3D.Crear(Ui, Raiz);
            _menu.AlContinuar = CerrarPausa;
            _menu.AlInventario = InventarioDesdeLaPausa;
            _menu.AlTerminar = () => Terminar(terminoAntes: true);
            _menu.AlCambiarSensibilidad = AplicarSensibilidad;
            _menu.AlCambiarVolumen = AplicarVolumen;
            _menu.Estado = EstadoDelRecorrido;
        }

        private void Start() {
            StartCoroutine(Cargar());
        }

        // ==================================================================== entrar al mundo

        private IEnumerator Cargar() {
            if (!Application.CanStreamedLevelBeLoaded(ModuloDeRecoleccion3D.Escena)) {
                // Falta en Build Settings. Se dice, y se deja seguir: el nivel no se puede quedar atascado aqui.
                Debug.LogError($"[Nexus] La escena «{ModuloDeRecoleccion3D.Escena}» no está en Build Settings. " +
                               "Menú Nexus > Mundo 3D > Preparar escena de recolección.");
                yield return Avisar("No se encontró el mundo 3D en esta versión: el recorrido se simula.");
                yield break;
            }

            // Lo que hoy pinta y escucha es del juego; en cuanto llegue el mundo, pinta y escucha el.
            var delJuego = new List<Behaviour>();
            delJuego.AddRange(FindObjectsByType<Camera>(FindObjectsSortMode.None));
            delJuego.AddRange(FindObjectsByType<AudioListener>(FindObjectsSortMode.None));

            SceneManager.sceneLoaded += AlCargarLaEscena;
            var carga = SceneManager.LoadSceneAsync(ModuloDeRecoleccion3D.Escena, LoadSceneMode.Additive);
            while (carga != null && !carga.isDone) yield return null;
            SceneManager.sceneLoaded -= AlCargarLaEscena;

            var escena = SceneManager.GetSceneByName(ModuloDeRecoleccion3D.Escena);
            if (!escena.IsValid() || !escena.isLoaded) {
                yield return Avisar("El mundo 3D no se pudo cargar: el recorrido se simula.");
                yield break;
            }
            _escenaCargada = true;

            foreach (var b in delJuego) {
                if (b == null || !b.enabled) continue;
                b.enabled = false;
                _apagados.Add(b);
            }
            SceneManager.SetActiveScene(escena);   // su cielo y su iluminacion, no los de la escena del juego
            var calidad = Array.IndexOf(QualitySettings.names, NivelDeCalidad);
            if (calidad >= 0 && calidad != QualitySettings.GetQualityLevel()) {
                _calidadAnterior = QualitySettings.GetQualityLevel();
                QualitySettings.SetQualityLevel(calidad, true);
            }
            App.MostrarFondo(false);
            _fondoQuitado = true;

            ConectarConLaEscena(escena);
            _volumenDelJuego = AudioListener.volume;
            AplicarVolumen(MenuDePausa3D.Volumen);
            AplicarSensibilidad(MenuDePausa3D.Sensibilidad);

            var minutos = _reloj != null ? Mathf.RoundToInt(_reloj.TotalTime / 60f) : 0;
            _estado.text = "Recoge <b>hojas de vida</b> (tu equipo) y <b>suministros</b>" +
                           (minutos > 0 ? $" antes de que acaben los <b>{minutos} minutos</b>." : ".");
        }

        /// <summary>Busca en el mundo recien cargado lo que hay que escuchar: el reloj, el inventario y el jugador.</summary>
        private void ConectarConLaEscena(Scene escena) {
            foreach (var raiz in escena.GetRootGameObjects()) {
                if (_reloj == null) _reloj = raiz.GetComponentInChildren<GameTimerUI>(true);
                if (_inventario == null) _inventario = raiz.GetComponentInChildren<InventoryController>(true);
                _panelesDeInventario.AddRange(raiz.GetComponentsInChildren<InventoryPanelUI>(true));
                foreach (var camara in raiz.GetComponentsInChildren<CinemachineFreeLook>(true))
                    _camaras.Add(new KeyValuePair<CinemachineFreeLook, Vector2>(camara, new Vector2(camara.m_XAxis.m_MaxSpeed, camara.m_YAxis.m_MaxSpeed)));
            }
            if (_inventario != null) _jugador = _inventario.gameObject;

            if (_reloj != null) {
                _reloj.Exhausted += AlAgotarseElTiempo;
                _reloj.StartTimer();
            } else {
                Debug.LogWarning("[Nexus] El recorrido no tiene reloj (GameTimerUI): solo se puede terminar desde la pausa.");
            }
            foreach (var panel in _panelesDeInventario) {
                panel.Opened += AlAbrirElInventario;
                panel.Closed += AlCerrarElInventario;
            }
            if (_inventario == null) Debug.LogWarning("[Nexus] El recorrido no tiene inventario (InventoryController): no se traerá nada.");
        }

        /// <summary>
        /// Sin mundo 3D no hay recorrido: se dice un momento y se vuelve con las manos vacias (el nivel no se puede quedar
        /// atascado aqui).
        /// </summary>
        private IEnumerator Avisar(string texto) {
            _estado.text = texto;
            _estado.color = Tema.warning;
            yield return new WaitForSecondsRealtime(3f);
            Terminar(terminoAntes: true);
        }

        /// <summary>
        /// Justo al cargar, antes del primer Update de la escena: su EventSystem sobra (manda el del juego, y dos
        /// a la vez se pisan), y su pausa tambien (la pone esta pantalla, que ademas para el reloj).
        /// </summary>
        private static void AlCargarLaEscena(Scene escena, LoadSceneMode modo) {
            if (escena.name != ModuloDeRecoleccion3D.Escena) return;
            foreach (var raiz in escena.GetRootGameObjects()) {
                foreach (var eventos in raiz.GetComponentsInChildren<EventSystem>(true))
                    eventos.gameObject.SetActive(false);
                foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
                    if (Array.IndexOf(PausaDeLaEscena, t.name) >= 0) t.gameObject.SetActive(false);
            }
        }

        private void Update() {
            var teclado = Keyboard.current;
            if (_terminando || teclado == null) return;

            _medidor.Anotar(Time.unscaledDeltaTime);
            if (teclado.f3Key.wasPressedThisFrame) _fps.gameObject.SetActive(!_fps.gameObject.activeSelf);
            if (_fps.gameObject.activeSelf) _fps.text = $"{_medidor.FpsAhora:0} FPS · {_medidor.MsAhora:0.0} ms";
            AnimarLaGuia();
            if (!_escenaCargada) return;

            VigilarElInventario();

            if (teclado.escapeKey.wasPressedThisFrame) {
                if (_menu.Abierto) _menu.Atras();
                else if (InventarioAbierto) CerrarInventario();
                else AbrirPausa();
                return;
            }
            if (teclado.iKey.wasPressedThisFrame && !_menu.Abierto && _panelesDeInventario.Count > 0) {
                if (InventarioAbierto) CerrarInventario();
                else _panelesDeInventario[0].OpenInventory();
                return;
            }

            // El mundo captura el raton para mirar. Tab lo suelta (y lo vuelve a capturar) mientras se juega.
            if (_congelado.Count > 0 || !teclado.tabKey.wasPressedThisFrame) return;
            var capturado = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = capturado ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = capturado;
        }

        // ==================================================================== la guia de teclas

        private void Tecla(Transform padre, string tecla, string que) {
            var fila = Ui.Fila(padre, tecla, Tema.Espacio(3));
            UiKit.Tamano(Ui.Chip(fila, tecla, Tema.surfaceRaised, Tema.cyan), ancho: 118);
            UiKit.Tamano(Ui.Texto(fila, que, EstiloTexto.Pequeno, Tema.ink), flexAncho: 1);
        }

        /// <summary>
        /// Entra deslizandose desde la izquierda, se queda DuracionDeLaGuia segundos (la cuenta no corre en pausa ni con
        /// el inventario abierto) y se desvanece. Tiempo no escalado: con el mundo congelado sigue respondiendo.
        /// </summary>
        private void AnimarLaGuia() {
            if (_cartelVisible <= 0) return;
            _guiaEnPantalla += Time.unscaledDeltaTime;
            // La cuenta empieza con el mundo ya cargado (mientras carga, la guia dice «Cargando…»).
            if (_escenaCargada && _congelado.Count == 0) _cartelVisible -= Time.unscaledDeltaTime;
            var entrada = Mathf.SmoothStep(0, 1, Mathf.Clamp01(_guiaEnPantalla / 0.35f));
            var salida = Mathf.Clamp01(_cartelVisible / 0.6f);
            _guia.alpha = entrada * salida;
            _cartel.anchoredPosition = new Vector2(Tema.margen - 40 * (1 - entrada), Tema.margen);
            _cuentaDeLaGuia.Valor = Mathf.Clamp01(_cartelVisible / DuracionDeLaGuia);
            if (_cartelVisible <= 0) _cartel.gameObject.SetActive(false);
        }

        // ==================================================================== la pausa y el inventario

        private void AbrirPausa() {
            Congelar(MotivoPausa);
            _menu.Abrir();
        }

        private void CerrarPausa() {
            _menu.Cerrar();
            Descongelar(MotivoPausa);
        }

        /// <summary>Desde la pausa: el inventario se ve sin el velo encima y, al cerrarlo, se vuelve a la pausa.</summary>
        private void InventarioDesdeLaPausa() {
            if (_panelesDeInventario.Count == 0) return;
            _menu.Cerrar();
            _menuTrasInventario = true;
            _panelesDeInventario[0].OpenInventory();
        }

        private bool InventarioAbierto {
            get {
                foreach (var panel in _panelesDeInventario) if (panel != null && panel.IsOpen) return true;
                return false;
            }
        }

        private void CerrarInventario() {
            foreach (var panel in _panelesDeInventario) if (panel != null && panel.IsOpen) panel.CloseInventory();
        }

        /// <summary>
        /// Red de seguridad: lo que se ve manda. Si el inventario se abrio o se cerro por un camino que no avisa (un
        /// boton de la escena que lo apaga directamente), el mundo no se puede quedar congelado sin nada encima.
        /// </summary>
        private void VigilarElInventario() {
            var abierto = InventarioAbierto;
            if (abierto && !_congelado.Contains(MotivoInventario)) AlAbrirElInventario();
            else if (!abierto && _congelado.Contains(MotivoInventario)) AlCerrarElInventario();
            // En pausa, sin inventario delante, la pausa siempre se ve.
            if (!abierto && _congelado.Contains(MotivoPausa) && !_menu.Abierto) {
                _menuTrasInventario = false;
                _menu.Abrir();
            }
        }

        private void AlAbrirElInventario() {
            Congelar(MotivoInventario);
        }

        private void AlCerrarElInventario() {
            Descongelar(MotivoInventario);
            if (!_menuTrasInventario) return;
            _menuTrasInventario = false;
            if (_congelado.Contains(MotivoPausa)) _menu.Abrir();
        }

        /// <summary>
        /// Para el mundo por 'motivo' (la pausa, el inventario): el reloj, el tiempo de la fisica y las animaciones, el
        /// sonido y al jugador. Con varios motivos a la vez, el mundo sigue parado hasta que se quita el ultimo.
        /// </summary>
        private void Congelar(string motivo) {
            if (_reloj != null) _reloj.Pause(motivo);
            if (!_congelado.Add(motivo) || _congelado.Count > 1) return;
            _cursorCapturadoAntes = Cursor.lockState == CursorLockMode.Locked;
            _escalaDeTiempoAntes = Time.timeScale > 0 ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            // NThirdPersonPlayer.nPause: deja de leer el movimiento y apaga la camara libre. Por mensaje, para no atar
            // este ensamblado al del controlador del equipo 3D.
            if (_jugador != null) _jugador.SendMessage("nPause", SendMessageOptions.DontRequireReceiver);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Descongelar(string motivo) {
            if (_reloj != null) _reloj.Resume(motivo);
            if (!_congelado.Remove(motivo) || _congelado.Count > 0) return;
            Time.timeScale = _escalaDeTiempoAntes;
            AudioListener.pause = false;
            if (_jugador != null) _jugador.SendMessage("nUnPause", SendMessageOptions.DontRequireReceiver);
            Cursor.lockState = _cursorCapturadoAntes ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !_cursorCapturadoAntes;
        }

        /// <summary>El volumen del recorrido es una parte del general del juego (Opciones), que se respeta.</summary>
        private void AplicarVolumen(float factor) {
            if (_volumenDelJuego >= 0) AudioListener.volume = _volumenDelJuego * Mathf.Clamp01(factor);
        }

        private void AplicarSensibilidad(float factor) {
            foreach (var kv in _camaras) {
                if (kv.Key == null) continue;
                kv.Key.m_XAxis.m_MaxSpeed = kv.Value.x * factor;
                kv.Key.m_YAxis.m_MaxSpeed = kv.Value.y * factor;
            }
        }

        /// <summary>Para la pausa: el tiempo que queda y lo que se lleva, contado por lo que significa.</summary>
        private string EstadoDelRecorrido() {
            var tiempo = "";
            if (_reloj != null) {
                var s = Mathf.CeilToInt(_reloj.RemainingTime);
                tiempo = $"Quedan {s / 60:00}:{s % 60:00}. ";
            }
            int cvs = 0, suministros = 0, otros = 0;
            var catalogo = App.Catalogo.Objetos3D;
            foreach (var kv in Inventario()) {
                var o = catalogo.Buscar(kv.Key);
                if (o == null || o.Oculto) continue;
                if (o.Tipo == TiposDeObjeto3D.Candidato) cvs++;
                else if (o.Tipo == TiposDeObjeto3D.Suministro) suministros += kv.Value;
                else otros += kv.Value;
            }
            return tiempo + $"Llevas {cvs} {(cvs == 1 ? "hoja de vida" : "hojas de vida")}, {suministros} " +
                   $"{(suministros == 1 ? "suministro" : "suministros")} y {otros} {(otros == 1 ? "hallazgo más" : "hallazgos más")}.";
        }

        /// <summary>Lo que lleva el jugador: id del objeto -> cuantos.</summary>
        private Dictionary<string, int> Inventario() {
            var d = new Dictionary<string, int>(StringComparer.Ordinal);
            if (_inventario == null) return d;
            foreach (var def in _inventario.UniqueCollectedItems)
                if (def != null && !string.IsNullOrEmpty(def.Id)) d[def.Id] = _inventario.GetQuantity(def.Id);
            return d;
        }

        // ==================================================================== salir del mundo

        private void AlAgotarseElTiempo() {
            Terminar(terminoAntes: false);
        }

        private void Terminar(bool terminoAntes) {
            if (_terminando) return;
            _terminando = true;
            _menu.Cerrar();
            StartCoroutine(Salir(terminoAntes));
        }

        private IEnumerator Salir(bool terminoAntes) {
            // Lo recogido se lee ANTES de descargar la escena: con ella se va el inventario.
            var descartados = new List<string>();
            var fraccion = _reloj != null ? _reloj.UsedFraction : 1f;
            // Sin mundo (no esta en la build o no cargo) no se castiga al jugador dejandolo sin equipo: el recorrido se
            // simula, como cuando el 3D no existia.
            var resultado = _escenaCargada
                ? TraductorDeRecoleccion3D.Traducir(Entrada, App.Catalogo.Objetos3D, Inventario(), fraccion, terminoAntes, descartados)
                : SimuladorDeRecoleccion.Simular(Entrada, IntensidadesDeSimulacion.Normal);
            if (descartados.Count > 0)
                Debug.LogWarning("[Nexus] Objetos del 3D que no están en objetos-3d.json (no cuentan): " + string.Join(", ", descartados));

            if (_escenaCargada) {
                _medidor.Guardar(ModuloDeRecoleccion3D.Escena);
                Desconectar();
                RecogerLoQueSeQuedoFuera();
                var descarga = SceneManager.UnloadSceneAsync(ModuloDeRecoleccion3D.Escena);
                while (descarga != null && !descarga.isDone) yield return null;
                _escenaCargada = false;
            }
            Restaurar();

            // Primero se cierra esta pantalla (la Fase 1 vuelve a encenderse) y despues se entrega el resultado.
            var alTerminar = AlTerminar;
            AlTerminar = null;
            if (App.Router.Actual == this) App.Router.Volver();
            alTerminar?.Invoke(resultado);
        }

        /// <summary>
        /// Lo del recorrido que nacio fuera de su escena (Instantiate sin padre cae en la escena activa, y al cargar puede
        /// ser todavia la del juego) se mete en ella, para que se vaya al descargarla: si no, los objetos y sus luces se
        /// quedarian flotando sobre las pantallas 2D.
        /// </summary>
        private static void RecogerLoQueSeQuedoFuera() {
            var escena = SceneManager.GetSceneByName(ModuloDeRecoleccion3D.Escena);
            if (!escena.IsValid() || !escena.isLoaded) return;
            foreach (var recogible in FindObjectsByType<global::Mundo3D.Coleccionables.CollectiblePickup>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (recogible == null) continue;
                var raiz = recogible.transform.root.gameObject;
                if (raiz.scene != escena) SceneManager.MoveGameObjectToScene(raiz, escena);
            }
        }

        private void Desconectar() {
            if (_reloj != null) _reloj.Exhausted -= AlAgotarseElTiempo;
            foreach (var panel in _panelesDeInventario) {
                if (panel == null) continue;
                panel.Opened -= AlAbrirElInventario;
                panel.Closed -= AlCerrarElInventario;
            }
            _reloj = null;
            _panelesDeInventario.Clear();
            _inventario = null;
            _jugador = null;
            _camaras.Clear();
        }

        /// <summary>Deja el juego como estaba antes de entrar. Se puede llamar mas de una vez.</summary>
        private void Restaurar() {
            foreach (var b in _apagados) if (b != null) b.enabled = true;
            _apagados.Clear();
            if (_fondoQuitado) {
                App.MostrarFondo(true);
                _fondoQuitado = false;
            }
            if (_calidadAnterior >= 0) {
                QualitySettings.SetQualityLevel(_calidadAnterior, true);
                _calidadAnterior = -1;
            }
            // Lo que la pausa pudo dejar puesto: el juego 2D no puede quedarse con el tiempo parado ni mudo.
            _congelado.Clear();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (_volumenDelJuego >= 0) {
                AudioListener.volume = _volumenDelJuego;
                _volumenDelJuego = -1;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDestroy() {
            // Si la pantalla se va sin terminar el recorrido (cerrar el juego, por ejemplo), el mundo no se puede quedar
            // puesto debajo de los menus.
            SceneManager.sceneLoaded -= AlCargarLaEscena;
            Desconectar();
            if (_escenaCargada) {
                _escenaCargada = false;
                var escena = SceneManager.GetSceneByName(ModuloDeRecoleccion3D.Escena);
                if (escena.IsValid() && escena.isLoaded) SceneManager.UnloadSceneAsync(escena);
            }
            if (App != null) Restaurar();
        }
    }
}
