using System;
using System.Collections;
using System.Collections.Generic;
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
    /// Lo que el juego pone ENCIMA del mundo 3D mientras dura el recorrido: el boton «Dejar de recolectar». Y es
    /// tambien quien trae y se lleva ese mundo:
    ///
    ///   al abrirse   carga la escena del recorrido JUNTO a la del juego (aditiva: AppRoot, la sesion y la pila
    ///                de pantallas no se tocan), apaga la camara del juego y quita el fondo opaco del lienzo
    ///   al terminar  descarga la escena, deja todo como estaba, se cierra y entrega el resultado
    ///
    /// La pantalla de la Fase 1 queda debajo, apagada, y vuelve a encenderse al cerrar esta.
    /// </summary>
    public sealed class PantallaDeRecoleccion3D : Pantalla {
        /// <summary>
        /// El nivel de calidad con el que se pinta el mundo 3D: su propio perfil de URP, mas barato que el del juego
        /// (Nexus > Mundo 3D > 4 · Configurar el render del 3D). Si no existe, se pinta con el del juego.
        /// </summary>
        public const string NivelDeCalidad = "Mundo 3D";

        public EntradaDeRecoleccion Entrada;
        public Action<ResultadoDeRecoleccion> AlTerminar;

        /// <summary>Escape aqui no vuelve atras: en el mundo 3D pausa y suelta el raton.</summary>
        public override bool PuedeVolver { get { return false; } }

        private readonly List<Behaviour> _apagados = new List<Behaviour>();
        private readonly MedidorDeRendimiento _medidor = new MedidorDeRendimiento();
        private TMP_Text _estado, _fps;
        private Button _salir;
        private bool _escenaCargada, _fondoQuitado, _terminando;
        private int _calidadAnterior = -1;

        protected override void Construir() {
            // La raiz no tiene imagen: no tapa el mundo ni se queda con sus clics. Solo el panel los recibe.
            var panel = Ui.PanelColumna(Raiz, "Recorrido", Tema.espacio, Tema.Espacio(2), NexusTheme.Alfa(Tema.bg950, 0.82f));
            panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.one;
            panel.anchoredPosition = new Vector2(-Tema.margen, -Tema.margen);
            panel.sizeDelta = new Vector2(380, 0);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Ui.Texto(panel, "FASE 1 · RECORRIDO", EstiloTexto.Leyenda, Tema.cyan);
            _estado = Ui.Texto(panel, "Cargando el recorrido…", EstiloTexto.Pequeno, Tema.ink);
            _salir = Ui.Boton(panel, "Dejar de recolectar", Terminar, VarianteBoton.Primario);
            _salir.interactable = false;
            _fps = Ui.Texto(panel, "", EstiloTexto.Mono, Tema.inkMuted);
            _fps.gameObject.SetActive(false);
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
                Avisar("No se encontró el mundo 3D en esta versión. Puedes seguir con el nivel.");
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
                Avisar("El mundo 3D no se pudo cargar. Puedes seguir con el nivel.");
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

            _estado.text = "W A S D para moverte · ratón para mirar · Espacio para saltar.\n" +
                           "<b>Esc</b> o <b>Tab</b> sueltan el ratón para pulsar el botón. <b>F3</b>: rendimiento.";
            _salir.interactable = true;
        }

        private void Avisar(string texto) {
            _estado.text = texto;
            _estado.color = Tema.warning;
            _salir.interactable = true;
        }

        /// <summary>
        /// Justo al cargar, antes del primer Update de la escena: su EventSystem sobra (manda el del juego, y dos
        /// a la vez se pisan).
        /// </summary>
        private static void AlCargarLaEscena(Scene escena, LoadSceneMode modo) {
            if (escena.name != ModuloDeRecoleccion3D.Escena) return;
            foreach (var raiz in escena.GetRootGameObjects())
                foreach (var eventos in raiz.GetComponentsInChildren<EventSystem>(true))
                    eventos.gameObject.SetActive(false);
        }

        private void Update() {
            var teclado = Keyboard.current;
            if (!_escenaCargada || _terminando || teclado == null) return;

            _medidor.Anotar(Time.unscaledDeltaTime);
            if (teclado.f3Key.wasPressedThisFrame) _fps.gameObject.SetActive(!_fps.gameObject.activeSelf);
            if (_fps.gameObject.activeSelf) _fps.text = $"{_medidor.FpsAhora:0} FPS · {_medidor.MsAhora:0.0} ms";

            // El mundo captura el raton para mirar. Tab lo suelta (y lo vuelve a capturar) sin depender de la
            // pausa de la escena, para que el boton se pueda pulsar siempre.
            if (!teclado.tabKey.wasPressedThisFrame) return;
            var capturado = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = capturado ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = capturado;
        }

        // ==================================================================== salir del mundo

        private void Terminar() {
            if (_terminando) return;
            _terminando = true;
            _salir.interactable = false;
            StartCoroutine(Salir());
        }

        private IEnumerator Salir() {
            var resultado = ResultadoProvisional.Para(Entrada);

            if (_escenaCargada) {
                _medidor.Guardar(ModuloDeRecoleccion3D.Escena);
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
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDestroy() {
            // Si la pantalla se va sin pasar por «Dejar de recolectar» (cerrar el juego, por ejemplo), el mundo no
            // se puede quedar puesto debajo de los menus.
            SceneManager.sceneLoaded -= AlCargarLaEscena;
            if (_escenaCargada) {
                _escenaCargada = false;
                var escena = SceneManager.GetSceneByName(ModuloDeRecoleccion3D.Escena);
                if (escena.IsValid() && escena.isLoaded) SceneManager.UnloadSceneAsync(escena);
            }
            if (App != null) Restaurar();
        }
    }
}
