using System;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus.Mundo3D {
    /// <summary>
    /// La pausa del recorrido 3D, con el aspecto del resto del juego (UiKit y el tema Nexus): oscurece el mundo, que se
    /// queda congelado debajo, y pone una ventana con lo importante:
    ///
    ///   Continuar          vuelve al recorrido (tambien Esc)
    ///   Inventario         lo recogido, sin que corra el reloj
    ///   Controles          las teclas, por si se olvidan
    ///   Opciones           volumen y sensibilidad del raton (se recuerdan entre partidas)
    ///   Terminar recorrido vuelve a la planificacion con lo que se lleve, tras confirmarlo
    ///
    /// No congela nada por si mismo: dice que quiere el jugador y quien lo crea (PantallaDeRecoleccion3D) para el reloj y
    /// el mundo. Todo lo que anima usa tiempo no escalado, porque con la pausa Time.timeScale es 0.
    /// </summary>
    public sealed class MenuDePausa3D : MonoBehaviour {
        public const string ClaveVolumen = "nexus.3d.volumen";
        public const string ClaveSensibilidad = "nexus.3d.sensibilidad";
        public const float SensibilidadMinima = 0.3f, SensibilidadMaxima = 2f;

        private enum Vista { Principal, Controles, Opciones, Confirmar }

        public Action AlContinuar, AlInventario, AlTerminar;
        /// <summary>La sensibilidad del raton cambio (1 = la de serie).</summary>
        public Action<float> AlCambiarSensibilidad;
        /// <summary>El volumen del recorrido cambio: de 0 a 1, sobre el volumen general del juego.</summary>
        public Action<float> AlCambiarVolumen;
        /// <summary>Lo que se enseña bajo el titulo: el tiempo que queda y lo que se lleva.</summary>
        public Func<string> Estado;

        public bool Abierto { get; private set; }

        private UiKit _ui;
        private NexusTheme _tema;
        private CanvasGroup _grupo;
        private RectTransform _ventana;
        private TMP_Text _estado;
        private Vista _vista;
        private float _alfa;

        public static float Volumen { get { return PlayerPrefs.GetFloat(ClaveVolumen, 1f); } }
        public static float Sensibilidad { get { return PlayerPrefs.GetFloat(ClaveSensibilidad, 1f); } }

        /// <summary>Crea el menu, cerrado, encima de todo lo demas (tambien del HUD de la escena 3D).</summary>
        public static MenuDePausa3D Crear(UiKit ui, Transform padre) {
            var rt = ui.Nodo(padre, "Pausa del recorrido");
            UiKit.Rellenar(rt);
            // Un lienzo propio con orden alto: el HUD del 3D es otro lienzo y no puede quedar por encima de la pausa.
            var lienzo = rt.gameObject.AddComponent<Canvas>();
            lienzo.overrideSorting = true;
            lienzo.sortingOrder = 500;
            rt.gameObject.AddComponent<GraphicRaycaster>();
            var velo = rt.gameObject.AddComponent<Image>();
            velo.color = NexusTheme.Alfa(ui.Tema.bg950, 0.8f);   // bloquea los clics al mundo

            var m = rt.gameObject.AddComponent<MenuDePausa3D>();
            m._ui = ui;
            m._tema = ui.Tema;
            m._grupo = rt.gameObject.AddComponent<CanvasGroup>();
            m._ventana = ui.Ventana(rt, "Ventana");
            m._ventana.anchorMin = m._ventana.anchorMax = m._ventana.pivot = new Vector2(0.5f, 0.5f);
            m._ventana.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            m._ventana.sizeDelta = new Vector2(640, 0);
            rt.gameObject.SetActive(false);
            return m;
        }

        public void Abrir() {
            Abierto = true;
            _vista = Vista.Principal;
            gameObject.SetActive(true);
            _grupo.interactable = _grupo.blocksRaycasts = true;
            Pintar();
        }

        /// <summary>Se quita de en medio con un fundido. No avisa a nadie: quien lo cierra ya sabe por que.</summary>
        public void Cerrar() {
            Abierto = false;
            _grupo.interactable = _grupo.blocksRaycasts = false;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        /// <summary>Esc: desde una sub-vista vuelve a la principal; desde la principal, continua.</summary>
        public void Atras() {
            if (!Abierto) return;
            if (_vista != Vista.Principal) { IrA(Vista.Principal); return; }
            AlContinuar?.Invoke();
        }

        private void Update() {
            var objetivo = Abierto ? 1f : 0f;
            _alfa = Mathf.MoveTowards(_alfa, objetivo, Time.unscaledDeltaTime * 7f);
            _grupo.alpha = _alfa;
            // Entra con un leve acercamiento (0,96 → 1) y sale igual: se nota que llega, sin estorbar.
            var escala = Mathf.Lerp(0.96f, 1f, Mathf.SmoothStep(0, 1, _alfa));
            _ventana.localScale = new Vector3(escala, escala, 1);
            if (!Abierto && _alfa <= 0f) { gameObject.SetActive(false); return; }
            if (Abierto && _vista == Vista.Principal && _estado != null && Estado != null) _estado.text = Estado();
        }

        // ==================================================================== las vistas

        private void Pintar() {
            UiKit.Vaciar(_ventana);
            _estado = null;
            Button primero;
            switch (_vista) {
                case Vista.Controles: primero = PintarControles(); break;
                case Vista.Opciones: primero = PintarOpciones(); break;
                case Vista.Confirmar: primero = PintarConfirmar(); break;
                default: primero = PintarPrincipal(); break;
            }
            // Con teclado o mando se navega desde el primer boton.
            if (primero != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(primero.gameObject);
        }

        private Button PintarPrincipal() {
            _ui.Texto(_ventana, "FASE 1 · RECORRIDO", EstiloTexto.Leyenda, _tema.cyan);
            _ui.Texto(_ventana, "Recorrido en pausa", EstiloTexto.Titulo, _tema.ink);
            _estado = _ui.Texto(_ventana, Estado != null ? Estado() : "", EstiloTexto.Pequeno, _tema.inkMuted);
            _ui.Separador(_ventana);

            var col = _ui.Columna(_ventana, "Acciones", _tema.Espacio(2));
            var continuar = Boton(col, "Continuar", () => AlContinuar?.Invoke(), VarianteBoton.Primario);
            Boton(col, "Inventario", () => AlInventario?.Invoke(), VarianteBoton.Secundario);
            Boton(col, "Controles", () => IrA(Vista.Controles), VarianteBoton.Secundario);
            Boton(col, "Opciones", () => IrA(Vista.Opciones), VarianteBoton.Secundario);
            _ui.Espaciador(col, _tema.Espacio(2));
            Boton(col, "Terminar el recorrido", () => IrA(Vista.Confirmar), VarianteBoton.Peligro);

            _ui.Texto(_ventana, "El reloj no corre mientras estás aquí.  <b>Esc</b> para continuar.", EstiloTexto.Leyenda, _tema.inkFaint);
            return continuar;
        }

        private Button PintarControles() {
            _ui.Texto(_ventana, "Controles", EstiloTexto.Subtitulo, _tema.ink);
            var lista = _ui.Columna(_ventana, "Teclas", _tema.Espacio(2));
            Tecla(lista, "W A S D", "Moverte");
            Tecla(lista, "Ratón", "Mirar alrededor");
            Tecla(lista, "Espacio", "Saltar");
            Tecla(lista, "Shift", "Caminar despacio");
            Tecla(lista, "E", "Abrir una puerta (con la tarjeta de acceso)");
            Tecla(lista, "I", "Abrir o cerrar el inventario (el reloj se para)");
            Tecla(lista, "Tab", "Soltar o capturar el ratón");
            Tecla(lista, "Esc", "Pausa");
            Tecla(lista, "F3", "Ver el rendimiento (FPS)");
            _ui.Texto(_ventana, "Acércate a un objeto para recogerlo. Las hojas de vida forman tu equipo; la pizza, el café y la " +
                                "Guepardex son suministros para el desarrollo.", EstiloTexto.Pequeno, _tema.inkMuted);

            // El color y el ritmo del brillo dicen qué es cada cosa antes de verla de cerca (CollectibleFeedback).
            _ui.Texto(_ventana, "QUÉ SIGNIFICA CADA LUZ", EstiloTexto.Leyenda, _tema.cyan);
            var luces = _ui.Columna(_ventana, "Luces", _tema.Espacio(1));
            Luz(luces, "#22D3EE", "Cian que respira despacio: una hoja de vida (alguien para tu equipo)");
            Luz(luces, "#34D399", "Verde que late: una ayuda (pizza, café, Guepardex)");
            Luz(luces, "#FBBF24", "Ámbar que parpadea doble: una tarjeta de acceso");
            Luz(luces, "#C084FC", "Violeta que centellea: algo para tu diario (cartas, notas)");
            Luz(luces, "#F87171", "Coral que pulsa rápido: una evidencia (el fragmento de USB)");
            Luz(luces, "#E3C24F", "Dorado con halo lento: un certificado");
            return Boton(_ventana, "Volver", () => IrA(Vista.Principal), VarianteBoton.Fantasma);
        }

        private Button PintarOpciones() {
            _ui.Texto(_ventana, "Opciones", EstiloTexto.Subtitulo, _tema.ink);
            var primero = Paso(_ventana, "Volumen del recorrido", Volumen, 0f, 1f, 0.1f, v => {
                PlayerPrefs.SetFloat(ClaveVolumen, v);
                AlCambiarVolumen?.Invoke(v);
            }, v => Mathf.RoundToInt(v * 100) + " %");
            Paso(_ventana, "Sensibilidad del ratón", Sensibilidad, SensibilidadMinima, SensibilidadMaxima, 0.1f, v => {
                PlayerPrefs.SetFloat(ClaveSensibilidad, v);
                AlCambiarSensibilidad?.Invoke(v);
            }, v => "×" + v.ToString("0.0"));
            _ui.Texto(_ventana, "Se guardan para las próximas partidas.", EstiloTexto.Leyenda, _tema.inkFaint);
            Boton(_ventana, "Volver", () => { PlayerPrefs.Save(); IrA(Vista.Principal); }, VarianteBoton.Fantasma);
            return primero;
        }

        private Button PintarConfirmar() {
            _ui.Texto(_ventana, "¿Terminar el recorrido ya?", EstiloTexto.Subtitulo, _tema.ink);
            _ui.Texto(_ventana, "Vuelves a la planificación con lo que hayas recogido hasta ahora. El tiempo que te quede se pierde, " +
                                "y lo que no recogiste se queda en el edificio.", EstiloTexto.Cuerpo, _tema.ink);
            if (Estado != null) _ui.Texto(_ventana, Estado(), EstiloTexto.Pequeno, _tema.inkMuted);
            var fila = _ui.Fila(_ventana, "Respuesta", _tema.Espacio(2));
            var seguir = Boton(fila, "Seguir recorriendo", () => IrA(Vista.Principal), VarianteBoton.Primario);
            Boton(fila, "Terminar y volver", () => AlTerminar?.Invoke(), VarianteBoton.Peligro);
            return seguir;
        }

        private void IrA(Vista vista) {
            _vista = vista;
            Pintar();
        }

        // ==================================================================== piezas

        private Button Boton(Transform padre, string texto, Action accion, VarianteBoton variante) {
            var b = _ui.Boton(padre, texto, accion, variante);
            UiKit.Tamano(b, flexAncho: 1);
            return b;
        }

        private void Luz(Transform padre, string hex, string que) {
            var fila = _ui.Fila(padre, que, _tema.Espacio(3));
            ColorUtility.TryParseHtmlString(hex, out Color color);
            _ui.Punto(fila, color, 14);
            UiKit.Tamano(_ui.Texto(fila, que, EstiloTexto.Pequeno, _tema.ink), flexAncho: 1);
        }

        private void Tecla(Transform padre, string tecla, string que) {
            var fila = _ui.Fila(padre, tecla, _tema.Espacio(3));
            var chip = _ui.Chip(fila, tecla, _tema.surfaceRaised, _tema.cyan);
            UiKit.Tamano(chip, ancho: 150);
            UiKit.Tamano(_ui.Texto(fila, que, EstiloTexto.Pequeno, _tema.ink), flexAncho: 1);
        }

        /// <summary>Un valor con [−] barra [+]: se maneja igual con raton, teclado o mando. Devuelve el boton «−».</summary>
        private Button Paso(Transform padre, string titulo, float valor, float min, float max, float paso,
                            Action<float> alCambiar, Func<float, string> formato) {
            _ui.Texto(padre, titulo.ToUpperInvariant(), EstiloTexto.Leyenda, _tema.cyan);
            var fila = _ui.Fila(padre, titulo, _tema.Espacio(3));
            var actual = Mathf.Clamp(valor, min, max);
            BarraView barra = null;
            TMP_Text texto = null;
            Action<float> poner = v => {
                actual = Mathf.Clamp(Mathf.Round(v / paso) * paso, min, max);
                barra.Valor = Mathf.InverseLerp(min, max, actual);
                texto.text = formato(actual);
                alCambiar(actual);
            };
            var menos = _ui.Boton(fila, "−", () => poner(actual - paso), VarianteBoton.Secundario);
            UiKit.Tamano(menos, ancho: 56);
            barra = _ui.Barra(fila, Mathf.InverseLerp(min, max, actual), _tema.cyan);
            var mas = _ui.Boton(fila, "+", () => poner(actual + paso), VarianteBoton.Secundario);
            UiKit.Tamano(mas, ancho: 56);
            texto = _ui.Texto(fila, formato(actual), EstiloTexto.Mono, _tema.ink);
            UiKit.Tamano(texto, ancho: 90);
            return menos;
        }
    }
}
