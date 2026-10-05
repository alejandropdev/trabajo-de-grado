using System;
using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Datos;
using Nexus.Core.Guardado;
using Nexus.Core.Sesion;
using Nexus.Unity.Juego;
using Nexus.Unity.Pantallas;
using Nexus.Unity.Tema;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// El unico objeto que hace falta en la escena. Al arrancar:
    ///   1 · carga y valida TODO el catalogo (INV-5: si el contenido esta roto, el juego no arranca y dice por que)
    ///   2 · crea el lienzo, el EventSystem y el ScreenRouter
    ///   3 · abre la pantalla inicial
    ///
    /// Despues es el dueño de lo que dura mas que una pantalla: el perfil activo, la partida, la sesion del
    /// motor y el autoguardado. Las pantallas se lo piden a el; nunca se lo pasan entre ellas.
    ///
    /// Sustituye al ProfileManager (y el ScreenRouter al MenuController). Los dos siguen en el proyecto
    /// solo porque la escena antigua MenuInicial los referencia; la escena Nexus ya no los usa.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class AppRoot : MonoBehaviour {
        [SerializeField, Tooltip("Vacio = el tema por defecto, en memoria.")]
        private NexusTheme _tema;

        public static AppRoot Instancia { get; private set; }

        public UiKit Ui { get; private set; }

        /// <summary>El kit de antes del design system, con el mismo tema. Ver UiPara.</summary>
        public UiKit UiClasico { get; private set; }
        public ScreenRouter Router { get; private set; }
        public LevelRunner Runner { get; private set; }

        // --- contenido ---
        public Catalogo Catalogo { get; private set; }
        public IReadOnlyList<string> ErroresDeCarga { get; private set; } = new List<string>();

        // --- jugador ---
        public ProfileStore Perfiles { get; private set; }
        public SaveStore Partidas { get; private set; }
        public PlayerProfile PerfilActivo { get; private set; }
        public SaveGame PartidaActiva { get; private set; }
        public GameSession Sesion { get; private set; }

        private AutoGuardado _autoGuardado;

        // ==================================================================== arranque

        private void Awake() {
            if (Instancia != null && Instancia != this) {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
            DontDestroyOnLoad(gameObject);

            Ui = new UiKit(_tema);
            UiClasico = new UiKit(Ui.Tema, clasico: true);

            var almacen = new AlmacenDeArchivosAtomico(RutasDeGuardado.Raiz);
            Perfiles = new ProfileStore(almacen);
            Partidas = new SaveStore(almacen);

            CargarCatalogo();
            Router = new ScreenRouter(this, CrearLienzo());
            CrearEventSystem();
            Runner = gameObject.AddComponent<LevelRunner>();
        }

        private void Start() {
            if (Catalogo == null) {
                Router.IrA<PantallaDeError>(p => p.Errores = ErroresDeCarga);
                return;
            }
            MostrarInicio();
        }

        /// <summary>La primera pantalla: elegir quien juega. Con un perfil ya elegido, su menu de partidas.</summary>
        public void MostrarInicio() {
            if (PerfilActivo != null) Router.IrA<PantallaDePartidas>();
            else Router.IrA<PantallaDePerfiles>();
        }

        /// <summary>Vuelve a leer el contenido del disco. Para la pantalla de error: corregir el JSON y reintentar.</summary>
        public bool RecargarCatalogo() {
            Catalogo = null;
            ErroresDeCarga = new List<string>();
            CargarCatalogo();
            return Catalogo != null;
        }

        private void CargarCatalogo() {
            try {
                Catalogo = CatalogLoader.CargarTodo(new CatalogoDeArchivos(RutasDeGuardado.Contenido));
                Debug.Log("[Nexus] Catalogo cargado: " + Catalogo);
            } catch (SchemaException ex) {
                ErroresDeCarga = ex.Errores != null && ex.Errores.Count > 0
                    ? ex.Errores
                    : new List<string> { ex.Message };
                Debug.LogError("[Nexus] El catalogo no es valido:\n" + string.Join("\n", ErroresDeCarga));
            } catch (Exception ex) {
                ErroresDeCarga = new List<string> { ex.GetType().Name + ": " + ex.Message };
                Debug.LogException(ex);
            }
        }

        /// <summary>Un lienzo a 1920×1080 que escala con la pantalla, con el fondo del tema y la capa de pantallas.</summary>
        private RectTransform CrearLienzo() {
            var go = new GameObject("Lienzo", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var escala = go.GetComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920, 1080);
            escala.matchWidthOrHeight = 0.5f;

            var fondo = UiKit.Rellenar(Ui.Nodo(go.transform, "Fondo"));
            var img = fondo.gameObject.AddComponent<Image>();
            img.color = Ui.Tema.bg900;
            img.raycastTarget = false;
            // Detras de todo, la oficina ilustrada, cubriendo la pantalla y muy oscurecida: el juego ocurre en un
            // sitio, no sobre un fondo plano.
            var oficina = Tema.MaterialesNexus.Sprite(Tema.MaterialesNexus.FondoOficina);
            if (oficina != null) {
                var foto = Ui.Nodo(fondo, "Oficina");
                foto.anchorMin = foto.anchorMax = new Vector2(0.5f, 0.5f);
                var ajuste = foto.gameObject.AddComponent<AspectRatioFitter>();
                ajuste.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                ajuste.aspectRatio = oficina.rect.width / oficina.rect.height;
                var fi = foto.gameObject.AddComponent<Image>();
                fi.sprite = oficina;
                fi.raycastTarget = false;
                var velo = UiKit.Rellenar(Ui.Nodo(fondo, "Velo")).gameObject.AddComponent<Image>();
                velo.color = NexusTheme.Alfa(Ui.Tema.bg950, 0.72f);
                velo.raycastTarget = false;
            }

            var pantallas = UiKit.Rellenar(Ui.Nodo(go.transform, "Pantallas"));
            // La guia va en su propia capa, por encima de TODAS las pantallas (tambien de los modales).
            // Un RectTransform sin Graphic no captura clics: solo la burbuja los captura.
            CapaGuia = UiKit.Rellenar(Ui.Nodo(go.transform, "Guia"));
            return pantallas;
        }

        /// <summary>
        /// Las pantallas que NO pasan al design system: la receta de apoyo de los minijuegos y las pizarras de tiza
        /// (conceptos y apuntes). Se quedan como estaban, botones incluidos.
        /// </summary>
        private static readonly HashSet<Type> ConKitClasico = new HashSet<Type> {
            typeof(Pantallas.Minijuegos.PantallaDeReceta),
            typeof(PantallaDePizarra)
        };

        /// <summary>El kit con el que se construye una pantalla.</summary>
        public UiKit UiPara(Pantalla pantalla) {
            return pantalla != null && ConKitClasico.Contains(pantalla.GetType()) ? UiClasico : Ui;
        }

        /// <summary>La capa de la guia del tutorial, encima de todo. La usa GuiaView.</summary>
        public RectTransform CapaGuia { get; private set; }

        /// <summary>
        /// El proyecto usa el Input System nuevo en exclusiva (activeInputHandler = 1), y con el el
        /// StandaloneInputModule clasico lanza excepciones. Es el mismo modulo que ya usa la escena MenuInicial.
        /// </summary>
        private void CrearEventSystem() {
            if (EventSystem.current != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetParent(transform, false);
        }

        // ==================================================================== perfil y partida

        public void SeleccionarPerfil(PlayerProfile perfil) {
            PerfilActivo = perfil ?? throw new ArgumentNullException(nameof(perfil));
        }

        /// <summary>Guarda el perfil activo: el pre-test, los coleccionables globales.</summary>
        public void GuardarPerfil() {
            if (PerfilActivo != null) Perfiles.Guardar(PerfilActivo);
        }

        /// <summary>Crea una partida nueva del perfil activo, empezando en el primer nivel del catalogo.</summary>
        public SaveGame CrearPartida(string nombre, int? semilla = null, bool modoAula = false) {
            if (PerfilActivo == null) throw new InvalidOperationException("No hay ningun perfil seleccionado.");

            var primero = ProgresionDeNiveles.Primero(Catalogo);
            var andamiaje = Catalogo.Niveles[primero].NivelAndamiaje;
            // Sin semilla de aula, una al azar: el modo aula es el unico que necesita partidas identicas.
            var s = semilla ?? UnityEngine.Random.Range(1, int.MaxValue);
            return Partidas.Crear(PerfilActivo.idPerfil, nombre, primero, s, modoAula, andamiaje);
        }

        /// <summary>
        /// Abre una partida: si esta entre niveles, empieza el nivel que toca; si tiene un nivel a medias, lo
        /// rehidrata tal y como se guardo (INV-7).
        /// </summary>
        public GameSession AbrirPartida(SaveGame partida) {
            PartidaActiva = partida ?? throw new ArgumentNullException(nameof(partida));
            _autoGuardado = new AutoGuardado(Partidas, partida, Debug.LogWarning);
            UltimoCierre = null;

            // ★ Una partida que se cerro justo despues de terminar un nivel (antes de pasar al siguiente) se
            // guardo sin nivel en curso y con el nivel ya en NivelesCompletados: abrirla tal cual empezaria
            // otra vez el nivel terminado. Se avanza al primero que falte.
            if (partida.Nivel == null) {
                var datos = partida.Partida;
                while (datos.NivelActualId != null && datos.NivelesCompletados.Contains(datos.NivelActualId))
                    datos.NivelActualId = ProgresionDeNiveles.Siguiente(Catalogo, datos.NivelActualId);
                if (datos.NivelActualId == null) {
                    Sesion = null;   // ya no quedan niveles: la partida esta terminada
                    return null;
                }
                datos.NivelAndamiaje = Catalogo.Niveles[datos.NivelActualId].NivelAndamiaje;
                FotografiarFlags();
            }

            Sesion = Partidas.AbrirSesion(partida, null, new FabricaDeSesion(Catalogo));
            return Sesion;
        }

        /// <summary>Lo que valian los flags al empezar este nivel: a esto vuelve «Repetir el nivel».</summary>
        private void FotografiarFlags() {
            if (PartidaActiva == null) return;
            PartidaActiva.FlagsAlEmpezarNivel = PartidaActiva.Flags == null
                ? new Dictionary<string, double>()
                : new Dictionary<string, double>(PartidaActiva.Flags);
            PartidaActiva.RelacionesAlEmpezarNivel = PartidaActiva.Relaciones == null
                ? new Dictionary<string, int>()
                : new Dictionary<string, int>(PartidaActiva.Relaciones);
        }

        /// <summary>
        /// Tras cerrar un nivel que no salio bien: se vuelve a jugar desde su Fase 1, con los flags como estaban al
        /// empezarlo (lo que escribio el cierre se deshace) y sin repetir la entrevista ni las escenas de apertura.
        /// </summary>
        public void RepetirNivel() {
            if (Sesion == null || !Sesion.NivelTerminado)
                throw new InvalidOperationException("Solo se puede repetir un nivel ya cerrado.");
            var nivel = Sesion.NivelId;
            if (PartidaActiva.FlagsAlEmpezarNivel != null)
                PartidaActiva.Flags = new Dictionary<string, double>(PartidaActiva.FlagsAlEmpezarNivel);
            if (PartidaActiva.RelacionesAlEmpezarNivel != null)
                PartidaActiva.Relaciones = new Dictionary<string, int>(PartidaActiva.RelacionesAlEmpezarNivel);
            PartidaActiva.Partida.NivelesCompletados.Remove(nivel);
            PartidaActiva.Partida.NivelActualId = nivel;
            PartidaActiva.Nivel = null;
            Nexus.Unity.Guia.GuiaView.Reiniciar();
            Sesion = new FabricaDeSesion(Catalogo).Nueva(PartidaActiva);
            Sesion.R.IntroVista = true;
            Guardar("repetir nivel");
        }

        /// <summary>El informe del ultimo nivel cerrado, para el Dashboard de Lecciones. Solo vive en memoria.</summary>
        public DebriefReport UltimoCierre { get; set; }

        /// <summary>
        /// Un coleccionable encontrado pasa a la coleccion del PERFIL en el momento, no al cerrar el nivel: la
        /// coleccion es del estudiante y sobrevive a las partidas. Lo que se escribe en Cerrar() son los flags.
        /// </summary>
        public void AnotarColeccionable(string id) {
            if (PerfilActivo == null || string.IsNullOrEmpty(id)) return;
            if (PerfilActivo.coleccionablesGlobales == null) PerfilActivo.coleccionablesGlobales = new List<string>();
            if (PerfilActivo.coleccionablesGlobales.Contains(id)) return;
            PerfilActivo.coleccionablesGlobales.Add(id);
            GuardarPerfil();
        }

        /// <summary>
        /// Si Marisol ya guio a este perfil, de principio a fin, por esa mecanica (detectar, ordenar, repartir). La
        /// primera vez de cada una se juega guiada; despues, libre, sea el reto que sea.
        /// </summary>
        public bool YaSeGuioLaMecanica(string verbo) {
            return !Nexus.Core.Minijuegos.TutorialPorMecanica.DebeGuiarse(MecanicasGuiadas(), verbo);
        }

        public void AnotarMecanicaGuiada(string verbo) {
            var guiadas = MecanicasGuiadas();
            var canonico = Nexus.Core.Minijuegos.Verbos.Normalizar(verbo);
            if (guiadas == null || canonico == null || guiadas.Contains(canonico)) return;
            guiadas.Add(canonico);
            GuardarPerfil();
        }

        /// <summary>La lista del perfil; un perfil de antes (null) la deduce de las escenas que ya jugo.</summary>
        private List<string> MecanicasGuiadas() {
            if (PerfilActivo == null) return null;
            if (PerfilActivo.mecanicasGuiadas == null) {
                PerfilActivo.mecanicasGuiadas = Nexus.Core.Minijuegos.TutorialPorMecanica.Migrar(PerfilActivo.minijuegosJugados, VerboDelMinijuego);
                GuardarPerfil();
            }
            return PerfilActivo.mecanicasGuiadas;
        }

        /// <summary>El verbo de una escena por su id: del indice si es un reto, o leyendo su JSON si es una practica.</summary>
        private string VerboDelMinijuego(string id) {
            if (Catalogo == null || string.IsNullOrEmpty(id)) return null;
            foreach (var m in Catalogo.Minijuegos)
                if (m.Id == id) return m.Verbo;
            foreach (var nivel in Catalogo.Niveles.Values)
                foreach (var tarea in nivel.Oficina.Tareas) {
                    if (tarea.Minijuego != id) continue;
                    try {
                        return CatalogoMinijuegos.Parsear(new CatalogoDeArchivos(RutasDeGuardado.Contenido).LeerCatalogo(tarea.Archivo)).Verbo;
                    } catch (Exception e) {
                        Debug.LogException(e);
                        return null;
                    }
                }
            return null;
        }

        /// <summary>Si este perfil ya termino alguna vez ese minijuego.</summary>
        public bool YaJugoElMinijuego(string id) {
            return PerfilActivo != null && PerfilActivo.minijuegosJugados != null && !string.IsNullOrEmpty(id) &&
                   PerfilActivo.minijuegosJugados.Contains(id);
        }

        public void AnotarMinijuegoJugado(string id) {
            if (PerfilActivo == null || string.IsNullOrEmpty(id)) return;
            if (PerfilActivo.minijuegosJugados == null) PerfilActivo.minijuegosJugados = new List<string>();
            if (PerfilActivo.minijuegosJugados.Contains(id)) return;
            PerfilActivo.minijuegosJugados.Add(id);
            GuardarPerfil();
        }

        /// <summary>
        /// Guarda la partida ahora. Los cinco puntos del autoguardado (§5.7) los decide quien llama: fin de
        /// Fase 1, fin de dia, lanzamiento, cierre de nivel y salir.
        /// </summary>
        public void Guardar(string motivo) {
            if (Sesion == null || _autoGuardado == null) return;
            if (Runner != null) PartidaActiva.Partida.SegundosJugados += Runner.ConsumirSegundosJugados();
            _autoGuardado.Guardar(Sesion, motivo);
        }

        /// <summary>
        /// Tras cerrar un nivel, empieza el siguiente. Devuelve su id, o null si ya no hay mas: el juego (o
        /// esta version del juego) termina ahi.
        /// </summary>
        public string PasarAlSiguienteNivel() {
            if (Sesion == null || !Sesion.NivelTerminado)
                throw new InvalidOperationException("El nivel actual todavia no se ha cerrado.");

            Guardar("cierre de nivel");
            var siguiente = ProgresionDeNiveles.Siguiente(Catalogo, Sesion.NivelId);
            if (siguiente == null) return null;

            PartidaActiva.Partida.NivelActualId = siguiente;
            PartidaActiva.Partida.NivelAndamiaje = Catalogo.Niveles[siguiente].NivelAndamiaje;
            FotografiarFlags();
            Sesion = new FabricaDeSesion(Catalogo).Nueva(PartidaActiva);
            Guardar("inicio de nivel");
            return siguiente;
        }

        /// <summary>Guarda y suelta la partida. Vuelve a dejar la aplicacion en el menu.</summary>
        public void CerrarPartida() {
            Guardar("salir");
            if (Runner != null) Runner.Detener();
            Nexus.Unity.Guia.GuiaView.Reiniciar();
            Sesion = null;
            PartidaActiva = null;
            _autoGuardado = null;
        }

        private void OnApplicationQuit() {
            // Cerrar la ventana a mitad de dia no puede perder el dia: INV-7 dice que recargar da lo mismo.
            if (Sesion != null && !Sesion.NivelTerminado) Guardar("cerrar la aplicacion");
        }

        private void Update() {
            // Escape vuelve atras, salvo en las pantallas que no lo permiten (una decision a medias).
            var teclado = Keyboard.current;
            if (teclado != null && teclado.escapeKey.wasPressedThisFrame &&
                Router?.Actual != null && Router.Actual.PuedeVolver)
                Router.Volver();
        }
    }
}
