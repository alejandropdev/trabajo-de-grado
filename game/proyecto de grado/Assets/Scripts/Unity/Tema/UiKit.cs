using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// Los estilos de texto. Los cinco primeros son los de siempre (todas las pantallas los usan); con el design system
    /// cada uno cae en su estilo equivalente: Titulo → phase, Subtitulo → panel-title, Cuerpo → body, Pequeno → texto
    /// secundario en ink-muted, Mono → code. Los demas son los estilos del design system tal cual.
    /// </summary>
    public enum EstiloTexto {
        Titulo, Subtitulo, Cuerpo, Pequeno, Mono,
        /// <summary>Orbitron 64 — el logotipo; una sola vez por pantalla.</summary>
        Hero,
        /// <summary>Rajdhani 17 semibold — encabezado de tarjeta o de seccion.</summary>
        Encabezado,
        /// <summary>Rajdhani 16 — las lineas de un personaje. Nunca en mayusculas.</summary>
        Dialogo,
        /// <summary>Rajdhani 13 bold, mayusculas via estilo — botones y navegacion.</summary>
        Etiqueta,
        /// <summary>Rajdhani 11 semibold — horas, ids, contadores.</summary>
        Leyenda,
        /// <summary>JetBrains Mono 11 semibold — prompts y etiquetas de codigo.</summary>
        CodigoEtiqueta,
        /// <summary>Spectral — solo documentos del mundo (papel).</summary>
        DocTitulo, DocCuerpo, DocLeyenda
    }

    public enum VarianteBoton {
        /// <summary>La accion principal de la pantalla. Una, como mucho dos.</summary>
        Primario,
        /// <summary>El resto de acciones.</summary>
        Secundario,
        /// <summary>Lo que tiene consecuencias: borrar, quedarse hasta las 20:00. Cuenta para el 12 % de mostaza.</summary>
        Peligro,
        /// <summary>Solo texto: «volver», «cancelar».</summary>
        Fantasma,
        /// <summary>Opcion de menu: ancho completo, icono a la izquierda en cyan, se marca con Resaltar.</summary>
        Menu
    }

    /// <summary>
    /// La fabrica de piezas de interfaz. Ninguna pantalla crea un Image o un TextMeshProUGUI a mano: se los
    /// pide a UiKit, y UiKit los viste con el NexusTheme. Asi el tema es de verdad el unico sitio donde vive
    /// el aspecto del juego.
    ///
    /// Todo se construye con uGUI y layout automatico (VerticalLayoutGroup / HorizontalLayoutGroup). Las
    /// pantallas describen QUE hay y en que orden; UiKit decide como se ve.
    /// </summary>
    public sealed partial class UiKit {
        public NexusTheme Tema { get; }

        /// <summary>
        /// El kit de antes del design system. Lo usan solo la receta de los minijuegos y las pizarras de tiza, que se
        /// quedan tal cual (AppRoot.UiPara): con Clasico, botones, textos y paneles se pintan como antes.
        /// </summary>
        public bool Clasico { get; }

        private Sprite _circulo;

        // Las medidas de antes del design system, fijas: la receta y las pizarras no se mueven aunque el tema cambie
        // sus espacios. El kit del design system usa las del tema.
        private float EspacioBase { get { return Clasico ? 12 : Tema.espacio; } }
        private float MargenBase { get { return Clasico ? 24 : Tema.margen; } }
        private float AltoBotonBase { get { return Clasico ? 56 : Tema.altoBoton; } }
        private float AltoBarraBase { get { return Clasico ? 18 : Tema.altoBarra; } }

        public UiKit(NexusTheme tema, bool clasico = false) {
            Tema = tema != null ? tema : NexusTheme.PorDefecto();
            Clasico = clasico;
        }

        // ================================================================ contenedores

        /// <summary>Un RectTransform vacio, hijo de 'padre'.</summary>
        public RectTransform Nodo(Transform padre, string nombre) {
            var go = new GameObject(nombre, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(padre, false);
            return rt;
        }

        /// <summary>Estira un RectTransform para que ocupe todo su padre, con un margen opcional.</summary>
        public static RectTransform Rellenar(RectTransform rt, float margen = 0) {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margen, margen);
            rt.offsetMax = new Vector2(-margen, -margen);
            return rt;
        }

        /// <summary>Un panel con el fondo del tema (sprite si lo hay, color si no).</summary>
        public RectTransform Panel(Transform padre, string nombre = "Panel", Color? color = null) {
            return Clasico ? PanelClasico(padre, nombre, color) : PanelNexus(padre, nombre, color);
        }

        private RectTransform PanelClasico(Transform padre, string nombre, Color? color) {
            var rt = Nodo(padre, nombre);
            var img = rt.gameObject.AddComponent<Image>();
            Vestir(img, Tema.spritePanel, color ?? Tema.fondoSecundario);
            return rt;
        }

        /// <summary>
        /// Un panel que es a la vez columna: su alto sale de su contenido. Es lo que hay que usar dentro de una
        /// fila o de otra columna. Un Panel con una Columna estirada dentro NO sirve ahi: en uGUI un hijo
        /// estirado no le da tamaño a su padre, y el panel mediria 0 de alto.
        /// </summary>
        public RectTransform PanelColumna(Transform padre, string nombre = "Panel", float? relleno = null,
                                          float? espacio = null, Color? color = null) {
            var rt = Panel(padre, nombre, color);
            var grupo = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            Configurar(grupo, espacio ?? EspacioBase, relleno ?? EspacioBase, TextAnchor.UpperLeft);
            return rt;
        }

        /// <summary>Apila hijos de arriba a abajo. Es el contenedor por defecto de casi todas las pantallas.</summary>
        public RectTransform Columna(Transform padre, string nombre = "Columna", float? espacio = null,
                                    float relleno = 0, TextAnchor alineacion = TextAnchor.UpperLeft) {
            var rt = Nodo(padre, nombre);
            var grupo = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            Configurar(grupo, espacio ?? EspacioBase, relleno, alineacion);
            return rt;
        }

        /// <summary>Pone hijos uno al lado del otro.</summary>
        public RectTransform Fila(Transform padre, string nombre = "Fila", float? espacio = null,
                                 float relleno = 0, TextAnchor alineacion = TextAnchor.MiddleLeft) {
            var rt = Nodo(padre, nombre);
            var grupo = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            Configurar(grupo, espacio ?? EspacioBase, relleno, alineacion);
            return rt;
        }

        /// <summary>
        /// Una lista que se desplaza en vertical. Devuelve el ScrollRect; los hijos van en 'contenido', que
        /// crece solo con lo que se le meta.
        /// </summary>
        public ScrollRect Desplazable(Transform padre, out RectTransform contenido, string nombre = "Desplazable") {
            var rt = Nodo(padre, nombre);
            var scroll = rt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;

            var visor = Rellenar(Nodo(rt, "Visor"));
            visor.gameObject.AddComponent<RectMask2D>();

            contenido = Columna(visor, "Contenido");
            contenido.anchorMin = new Vector2(0, 1);
            contenido.anchorMax = new Vector2(1, 1);
            contenido.pivot = new Vector2(0.5f, 1);
            contenido.offsetMin = contenido.offsetMax = Vector2.zero;
            contenido.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            // Con el design system, un poco de aire dentro del visor: las esquinas en L y el resplandor de las piezas
            // salen unos pixeles por fuera y el recorte del scroll se los comia.
            if (!Clasico) contenido.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(4, 4, 4, 4);

            scroll.viewport = visor;
            scroll.content = contenido;
            IndicarScroll(scroll, rt);
            return scroll;
        }

        /// <summary>
        /// Que se vea que se puede desplazar (feedback: «el jugador nunca supo que se podía»): una barra de scroll visible
        /// cuando hay mas contenido del que cabe, y un aviso «▼ Hay más abajo» (o «Hay más ►») que late suave y que,
        /// al pulsarlo, baja un trozo. Ambos desaparecen solos cuando todo cabe o se llega al final.
        /// </summary>
        private void IndicarScroll(ScrollRect scroll, RectTransform raiz) {
            scroll.verticalScrollbar = Barra(raiz, true);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.verticalScrollbarSpacing = 4;
            scroll.horizontalScrollbar = Barra(raiz, false);
            scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.horizontalScrollbarSpacing = 4;

            var indicador = raiz.gameObject.AddComponent<IndicadorDeScroll>();
            indicador.Scroll = scroll;
            indicador.Abajo = Aviso(raiz, "▼  Hay más abajo", new Vector2(0.5f, 0), new Vector2(0, 14), () => indicador.Avanzar(true));
            indicador.Derecha = Aviso(raiz, "Hay más  ►", new Vector2(1, 0.5f), new Vector2(-22, 0), () => indicador.Avanzar(false));
        }

        private Scrollbar Barra(RectTransform raiz, bool vertical) {
            const float Grosor = 10;
            var rt = Nodo(raiz, vertical ? "Barra vertical" : "Barra horizontal");
            if (vertical) {
                rt.anchorMin = new Vector2(1, 0); rt.anchorMax = Vector2.one; rt.pivot = new Vector2(1, 1);
                rt.sizeDelta = new Vector2(Grosor, 0);
            } else {
                rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(1, 0); rt.pivot = Vector2.zero;
                rt.sizeDelta = new Vector2(0, Grosor);
            }
            var pista = rt.gameObject.AddComponent<Image>();
            pista.sprite = SpriteRedondeado(); pista.type = Image.Type.Sliced;
            pista.color = Clasico ? new Color(1, 1, 1, 0.08f) : NexusTheme.Alfa(Tema.ink, 0.06f);
            var mango = Nodo(rt, "Mango");
            Rellenar(mango);
            var img = mango.gameObject.AddComponent<Image>();
            img.sprite = SpriteRedondeado(); img.type = Image.Type.Sliced;
            var mango_ = Clasico ? Tema.cian : Tema.cyan;
            img.color = new Color(mango_.r, mango_.g, mango_.b, 0.75f);
            var barra = rt.gameObject.AddComponent<Scrollbar>();
            barra.handleRect = mango;
            barra.targetGraphic = img;
            barra.direction = vertical ? Scrollbar.Direction.BottomToTop : Scrollbar.Direction.LeftToRight;
            var colores = ColorBlock.defaultColorBlock;
            colores.highlightedColor = new Color(1, 1, 1, 1);
            colores.normalColor = new Color(0.9f, 0.9f, 0.9f, 1);
            barra.colors = colores;
            return barra;
        }

        /// <summary>La pastilla de «hay más»: pequeña, flotando sobre el borde del area, pulsable.</summary>
        private RectTransform Aviso(RectTransform raiz, string texto, Vector2 ancla, Vector2 posicion, Action alPulsar) {
            var rt = Nodo(raiz, "Hay mas");
            rt.anchorMin = rt.anchorMax = rt.pivot = ancla;
            rt.anchoredPosition = posicion;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = SpriteRedondeado(); img.type = Image.Type.Sliced;
            var fondoAviso = Clasico ? Tema.fondoSecundario : Tema.surfaceRaised;
            img.color = new Color(fondoAviso.r, fondoAviso.g, fondoAviso.b, 0.94f);
            if (Clasico) {
                var borde = rt.gameObject.AddComponent<Outline>();
                borde.effectColor = Tema.cian;
                borde.effectDistance = new Vector2(1.5f, -1.5f);
            } else {
                Borde(rt, Tema.cyan, 30);
            }
            var boton = rt.gameObject.AddComponent<Button>();
            boton.targetGraphic = img;
            boton.onClick.AddListener(new UnityAction(alPulsar));
            var t = Texto(rt, texto, Clasico ? EstiloTexto.Pequeno : EstiloTexto.Leyenda,
                          Clasico ? Tema.cianClaro : Tema.cyan, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            if (Clasico) t.fontStyle = FontStyles.Bold;
            Rellenar((RectTransform)t.transform);
            rt.sizeDelta = new Vector2(t.GetPreferredValues(texto).x + 28, t.GetPreferredValues(texto).y + 10);
            rt.gameObject.SetActive(false);
            return rt;
        }

        /// <summary>Hueco fijo dentro de una fila o columna.</summary>
        public void Espaciador(Transform padre, float tamano) {
            var le = Nodo(padre, "Espaciador").gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = tamano;
            le.minWidth = le.preferredWidth = tamano;
        }

        /// <summary>Ocupa todo el espacio sobrante de una fila o columna: empuja lo siguiente al otro extremo.</summary>
        public void Resorte(Transform padre) {
            var le = Nodo(padre, "Resorte").gameObject.AddComponent<LayoutElement>();
            // ★ Solo en el eje de su contenedor. Con los dos, un resorte dentro de una FILA le daba a la fila
            // flexibilidad vertical, y esa fila se comia la mitad del alto de la columna (la lista de «¿Por qué?»
            // de la Fase 1 quedaba cortada a media pantalla).
            var enFila = padre.GetComponent<HorizontalLayoutGroup>() != null;
            le.flexibleWidth = enFila ? 1 : 0;
            le.flexibleHeight = enFila ? 0 : 1;
        }

        /// <summary>Una linea fina de separacion.</summary>
        public void Separador(Transform padre) {
            if (!Clasico) { SeparadorNexus(padre); return; }
            var rt = Nodo(padre, "Separador");
            rt.gameObject.AddComponent<Image>().color = Tema.hormigon;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = 2;
            le.flexibleWidth = 1;
        }

        /// <summary>Fija el tamaño de una pieza dentro del layout. null = no tocar esa dimension.</summary>
        public static LayoutElement Tamano(Component pieza, float? ancho = null, float? alto = null,
                                          float? flexAncho = null, float? flexAlto = null) {
            // Nada de '??' con objetos de Unity: en el editor GetComponent puede devolver un «falso null».
            var le = pieza.GetComponent<LayoutElement>();
            if (le == null) le = pieza.gameObject.AddComponent<LayoutElement>();
            if (ancho.HasValue) le.minWidth = le.preferredWidth = ancho.Value;
            if (alto.HasValue) le.minHeight = le.preferredHeight = alto.Value;
            if (flexAncho.HasValue) le.flexibleWidth = flexAncho.Value;
            if (flexAlto.HasValue) le.flexibleHeight = flexAlto.Value;
            // ★ Un tamaño fijo es fijo: sin esto, una columna de 420 con una barra dentro (que es flexible)
            // heredaba esa flexibilidad y se repartia el sobrante con el centro, y el centro quedaba estrecho.
            if (ancho.HasValue && !flexAncho.HasValue) le.flexibleWidth = 0;
            if (alto.HasValue && !flexAlto.HasValue) le.flexibleHeight = 0;
            return le;
        }

        /// <summary>Destruye todos los hijos. Para repintar una lista.</summary>
        public static void Vaciar(Transform contenedor) {
            for (var i = contenedor.childCount - 1; i >= 0; i--) {
                var hijo = contenedor.GetChild(i).gameObject;
                if (hijo.GetComponent<DecoracionNexus>() != null) continue;   // el borde o las esquinas del propio panel
                // Destroy espera al final del frame: apagado ya, para que el layout no lo cuente mientras tanto
                // (si no, la lista vieja y la nueva se suman un frame y la pantalla «crece» y salta).
                hijo.SetActive(false);
                UnityEngine.Object.Destroy(hijo);
            }
        }

        // ================================================================ texto

        public TextMeshProUGUI Texto(Transform padre, string texto, EstiloTexto estilo = EstiloTexto.Cuerpo,
                                   Color? color = null, TextAlignmentOptions alineacion = TextAlignmentOptions.TopLeft) {
            var rt = Nodo(padre, "Texto");
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = texto ?? "";
            tmp.alignment = alineacion;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.richText = true;
            tmp.raycastTarget = false;
            Estilizar(tmp, estilo);
            // Un texto nunca se aplasta por debajo de su alto: si no cabe, la columna crece (o hace scroll) en vez de
            // montar un texto encima de otro.
            if (!Clasico) rt.gameObject.AddComponent<AltoDeTexto>();
            if (color.HasValue) tmp.color = color.Value;
            return tmp;
        }

        public void Estilizar(TMP_Text tmp, EstiloTexto estilo) {
            if (Clasico) EstilizarClasico(tmp, estilo); else EstilizarNexus(tmp, estilo);
        }

        private void EstilizarClasico(TMP_Text tmp, EstiloTexto estilo) {
            switch (estilo) {
                case EstiloTexto.Titulo:
                    tmp.font = Tema.FuenteTitulo; tmp.fontSize = Tema.tamTitulo; tmp.fontStyle = FontStyles.Bold;
                    tmp.color = Tema.texto; break;
                case EstiloTexto.Subtitulo:
                    tmp.font = Tema.FuenteTitulo; tmp.fontSize = Tema.tamSubtitulo; tmp.fontStyle = FontStyles.Bold;
                    tmp.color = Tema.cianClaro; break;
                case EstiloTexto.Pequeno:
                    tmp.font = Tema.FuenteCuerpo; tmp.fontSize = Tema.tamPequeno; tmp.fontStyle = FontStyles.Normal;
                    tmp.color = Tema.textoTenue; break;
                case EstiloTexto.Mono:
                    tmp.font = Tema.FuenteMono; tmp.fontSize = Tema.tamCuerpo; tmp.fontStyle = FontStyles.Normal;
                    tmp.color = Tema.cian; break;
                default:
                    tmp.font = Tema.FuenteCuerpo; tmp.fontSize = Tema.tamCuerpo; tmp.fontStyle = FontStyles.Normal;
                    tmp.color = Tema.texto; break;
            }
        }

        // ================================================================ botones

        public Button Boton(Transform padre, string texto, Action alPulsar, VarianteBoton variante = VarianteBoton.Secundario) {
            return Clasico ? BotonClasico(padre, texto, alPulsar, variante) : BotonNexus(padre, texto, alPulsar, variante);
        }

        private Button BotonClasico(Transform padre, string texto, Action alPulsar, VarianteBoton variante) {
            var rt = Nodo(padre, "Boton " + texto);
            var img = rt.gameObject.AddComponent<Image>();
            var boton = rt.gameObject.AddComponent<Button>();

            Color fondo, colorTexto;
            switch (variante) {
                case VarianteBoton.Primario: fondo = Tema.cian; colorTexto = Tema.textoSobreCian; break;
                case VarianteBoton.Peligro: fondo = Tema.mostaza; colorTexto = Tema.textoSobreCian; break;
                case VarianteBoton.Fantasma: fondo = new Color(0, 0, 0, 0); colorTexto = Tema.cianClaro; break;
                default: fondo = Tema.pared; colorTexto = Tema.texto; break;
            }

            Vestir(img, variante == VarianteBoton.Fantasma ? null : Tema.spriteBoton, Color.white);
            boton.targetGraphic = img;
            boton.colors = Colores(fondo);

            var etiqueta = Texto(rt, texto, EstiloTexto.Cuerpo, colorTexto, TextAlignmentOptions.Center);
            etiqueta.fontStyle = FontStyles.Bold;
            etiqueta.textWrappingMode = TextWrappingModes.NoWrap;
            etiqueta.margin = new Vector4(EspacioBase * 1.5f, 0, EspacioBase * 1.5f, 0);
            Rellenar((RectTransform)etiqueta.transform);

            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = AltoBotonBase;
            le.preferredWidth = Mathf.Max(160, etiqueta.GetPreferredValues(texto).x + EspacioBase * 3);

            if (alPulsar != null) boton.onClick.AddListener(new UnityAction(alPulsar));
            return boton;
        }

        /// <summary>
        /// Un boton para frases largas (las opciones de una decision). Su alto sale de su texto, que se parte
        /// en varias lineas: un Boton normal tiene alto fijo, y una frase de dos lineas se sale de el. Debajo
        /// del texto puede llevar una segunda linea tenue (lo que va a cambiar, por que esta bloqueado…).
        /// </summary>
        public Button BotonDeOpcion(Transform padre, string texto, string detalle, Action alPulsar) {
            return Clasico ? BotonDeOpcionClasico(padre, texto, detalle, alPulsar) : BotonDeOpcionNexus(padre, texto, detalle, alPulsar);
        }

        private Button BotonDeOpcionClasico(Transform padre, string texto, string detalle, Action alPulsar) {
            var rt = Nodo(padre, "Opcion");
            var img = rt.gameObject.AddComponent<Image>();
            Vestir(img, Tema.spriteBoton, Color.white);
            var boton = rt.gameObject.AddComponent<Button>();
            boton.targetGraphic = img;
            boton.colors = Colores(Tema.pared);

            var grupo = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            Configurar(grupo, 4, EspacioBase * 1.5f, TextAnchor.MiddleLeft);
            Tamano(rt, alto: null).minHeight = AltoBotonBase;

            var t = Texto(rt, texto, EstiloTexto.Cuerpo);
            t.fontStyle = FontStyles.Bold;
            if (!string.IsNullOrEmpty(detalle)) Texto(rt, detalle, EstiloTexto.Pequeno);

            if (alPulsar != null) boton.onClick.AddListener(new UnityAction(alPulsar));
            return boton;
        }

        /// <summary>Un campo de texto de una linea (el nombre de un perfil, de una partida, una semilla).</summary>
        public TMP_InputField CampoDeTexto(Transform padre, string indicacion, string valor = "", float ancho = 420) {
            return Clasico ? CampoDeTextoClasico(padre, indicacion, valor, ancho) : CampoDeTextoNexus(padre, indicacion, valor, ancho);
        }

        private TMP_InputField CampoDeTextoClasico(Transform padre, string indicacion, string valor, float ancho) {
            var rt = Nodo(padre, "Campo");
            var fondo = rt.gameObject.AddComponent<Image>();
            Vestir(fondo, Tema.spriteBoton, Tema.fondo);
            var campo = rt.gameObject.AddComponent<TMP_InputField>();
            campo.targetGraphic = fondo;
            campo.colors = Colores(Tema.fondo);
            Tamano(rt, ancho, AltoBotonBase);

            var area = Rellenar(Nodo(rt, "Area"));
            area.offsetMin = new Vector2(EspacioBase, 6);
            area.offsetMax = new Vector2(-EspacioBase, -6);
            area.gameObject.AddComponent<RectMask2D>();

            var marcador = Texto(area, indicacion, EstiloTexto.Cuerpo, Tema.textoTenue, TextAlignmentOptions.MidlineLeft);
            marcador.fontStyle = FontStyles.Italic;
            marcador.textWrappingMode = TextWrappingModes.NoWrap;
            Rellenar((RectTransform)marcador.transform);

            var texto = Texto(area, "", EstiloTexto.Cuerpo, null, TextAlignmentOptions.MidlineLeft);
            texto.textWrappingMode = TextWrappingModes.NoWrap;
            Rellenar((RectTransform)texto.transform);

            campo.textViewport = area;
            campo.textComponent = texto;
            campo.placeholder = marcador;
            campo.fontAsset = texto.font;
            campo.pointSize = texto.fontSize;
            // Sin customCaretColor, TMP ignora caretColor: el cursor salia casi invisible y nadie sabia que el campo
            // ya estaba escribiendo (feedback beta #2). Cursor ancho, que parpadea, y un borde al enfocar.
            campo.customCaretColor = true;
            campo.caretColor = Tema.cian;
            campo.caretWidth = 3;
            campo.caretBlinkRate = 0.85f;
            campo.selectionColor = new Color(Tema.cian.r, Tema.cian.g, Tema.cian.b, 0.35f);
            var borde = rt.gameObject.AddComponent<Outline>();
            borde.effectColor = Tema.cian;
            borde.effectDistance = new Vector2(2, -2);
            borde.enabled = false;
            // La indicacion se va en cuanto se pulsa el campo, aunque todavia no se haya escrito nada.
            campo.onSelect.AddListener(_ => { marcador.alpha = 0; borde.enabled = true; });
            campo.onDeselect.AddListener(_ => { marcador.alpha = 1; borde.enabled = false; });
            campo.text = valor ?? "";
            return campo;
        }

        /// <summary>Una etiqueta pequeña con fondo de color: el estado de algo (aquí estás, cerrada, 3 h).</summary>
        public RectTransform Chip(Transform padre, string texto, Color fondo, Color? colorTexto = null) {
            if (!Clasico) return ChipNexus(padre, texto, fondo, colorTexto ?? Tema.onCyan);
            var chip = PanelColumna(padre, "Chip " + texto, 6, 0, fondo);
            var t = Texto(chip, texto, EstiloTexto.Pequeno, colorTexto ?? Tema.textoSobreCian, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return chip;
        }

        /// <summary>Una tarjeta: un panel con titulo cuyo alto sale de su contenido. Devuelve donde meter las cosas.</summary>
        public RectTransform Tarjeta(Transform padre, string titulo, Color? colorTitulo = null) {
            if (!Clasico) return TarjetaNexus(padre, titulo, colorTitulo ?? Tema.cyan, false);
            var panel = PanelColumna(padre, "Tarjeta " + titulo, MargenBase * 0.75f, EspacioBase);
            if (!string.IsNullOrEmpty(titulo)) Texto(panel, titulo.ToUpperInvariant(), EstiloTexto.Pequeno, colorTitulo ?? Tema.cian);
            return panel;
        }

        /// <summary>
        /// Pinta un boton ya creado como elegido (cian) o normal. Para las piezas que se seleccionan y deseleccionan:
        /// las piezas de un diagrama, la respuesta al cliente, el atributo de calidad.
        /// </summary>
        public void Resaltar(Button boton, bool elegido, Color? normal = null) {
            if (!Clasico && ResaltarNexus(boton, elegido, normal)) return;
            var fondo = elegido ? Tema.cian : normal ?? Tema.pared;
            boton.colors = Colores(fondo);
            foreach (var t in boton.GetComponentsInChildren<TMP_Text>())
                t.color = elegido ? Tema.textoSobreCian : (t.fontStyle & FontStyles.Bold) != 0 ? Tema.texto : Tema.textoTenue;
        }

        private static ColorBlock Colores(Color baseColor) {
            var bloque = ColorBlock.defaultColorBlock;
            bloque.normalColor = baseColor;
            bloque.highlightedColor = Color.Lerp(baseColor, Color.white, 0.18f);
            bloque.pressedColor = Color.Lerp(baseColor, Color.black, 0.2f);
            bloque.selectedColor = Color.Lerp(baseColor, Color.white, 0.1f);
            bloque.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.35f);
            bloque.fadeDuration = 0.08f;
            return bloque;
        }

        // ================================================================ indicadores

        /// <summary>Una barra horizontal de 0 a 1. El color por defecto es el cian informativo.</summary>
        public BarraView Barra(Transform padre, float valor01, Color? color = null, string nombre = "Barra") {
            if (!Clasico) return BarraNexus(padre, valor01, color ?? Tema.cyan, nombre);
            var rt = Nodo(padre, nombre);
            Vestir(rt.gameObject.AddComponent<Image>(), Tema.spriteBarraFondo, Tema.hormigon);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = AltoBarraBase;
            le.flexibleWidth = 1;

            var relleno = Nodo(rt, "Relleno");
            relleno.anchorMin = Vector2.zero;
            relleno.anchorMax = new Vector2(0, 1);
            relleno.offsetMin = relleno.offsetMax = Vector2.zero;
            var img = relleno.gameObject.AddComponent<Image>();
            Vestir(img, Tema.spriteBarraRelleno, color ?? Tema.cian);

            var barra = rt.gameObject.AddComponent<BarraView>();
            barra.Configurar(relleno, img);
            barra.Valor = valor01;
            return barra;
        }

        /// <summary>
        /// Un indicador circular. Una Image sin sprite ignora el modo «rellenado» de uGUI y el dial no se
        /// dibujaria nunca, asi que si el tema no trae sprite se usa un circulo generado en memoria.
        /// </summary>
        public DialView Dial(Transform padre, float valor01, string etiqueta, Color? color = null, float diametro = 120) {
            if (!Clasico) return DialNexus(padre, valor01, etiqueta, color ?? Tema.cyan, diametro);
            var rt = Nodo(padre, "Dial " + etiqueta);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = diametro;
            le.minHeight = le.preferredHeight = diametro + Tema.tamPequeno * 1.6f;

            var aro = Nodo(rt, "Aro");
            aro.anchorMin = aro.anchorMax = new Vector2(0.5f, 1);
            aro.pivot = new Vector2(0.5f, 1);
            aro.sizeDelta = new Vector2(diametro, diametro);
            var fondo = aro.gameObject.AddComponent<Image>();
            fondo.sprite = SpriteCircular();
            fondo.color = Tema.hormigon;

            var arco = Rellenar(Nodo(aro, "Arco"));
            var img = arco.gameObject.AddComponent<Image>();
            img.sprite = Tema.spriteDial != null ? Tema.spriteDial : SpriteCircular();
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillOrigin = (int)Image.Origin360.Top;
            img.fillClockwise = true;
            img.color = color ?? Tema.cian;

            var centro = Nodo(aro, "Centro");
            centro.anchorMin = new Vector2(0.18f, 0.18f);
            centro.anchorMax = new Vector2(0.82f, 0.82f);
            centro.offsetMin = centro.offsetMax = Vector2.zero;
            var hueco = centro.gameObject.AddComponent<Image>();
            hueco.sprite = SpriteCircular();
            hueco.color = Tema.fondoSecundario;

            var valor = Texto(centro, "", EstiloTexto.Subtitulo, Tema.texto, TextAlignmentOptions.Center);
            Rellenar((RectTransform)valor.transform);

            var nombre = Texto(rt, etiqueta, EstiloTexto.Pequeno, null, TextAlignmentOptions.Bottom);
            var rtNombre = (RectTransform)nombre.transform;
            rtNombre.anchorMin = Vector2.zero;
            rtNombre.anchorMax = new Vector2(1, 0);
            rtNombre.pivot = new Vector2(0.5f, 0);
            rtNombre.sizeDelta = new Vector2(0, Tema.tamPequeno * 1.6f);

            var dial = rt.gameObject.AddComponent<DialView>();
            dial.Configurar(img, valor);
            dial.Valor = valor01;
            return dial;
        }

        /// <summary>El radar de competencias del Dashboard. Valores de 0 a 1, uno por eje.</summary>
        public GraficoRadar Radar(Transform padre, float[] valores01, string[] etiquetas, float lado = 320) {
            var rt = Nodo(padre, "Radar");
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = lado;
            le.minHeight = le.preferredHeight = lado;

            rt.gameObject.AddComponent<CanvasRenderer>();   // antes que el Graphic: ver GraficoRadar
            var radar = rt.gameObject.AddComponent<GraficoRadar>();
            radar.raycastTarget = false;
            var acento = Clasico ? Tema.cian : Tema.cyan;
            radar.ColorRejilla = Clasico ? Tema.hormigon : Tema.lineStrong;
            radar.ColorValor = new Color(acento.r, acento.g, acento.b, Clasico ? 0.45f : 0.3f);
            radar.ColorBorde = acento;
            radar.Valores = valores01;

            if (etiquetas != null)
                for (var i = 0; i < etiquetas.Length; i++) {
                    var t = Texto(rt, etiquetas[i], Clasico ? EstiloTexto.Pequeno : EstiloTexto.Leyenda, null, TextAlignmentOptions.Center);
                    var rtT = (RectTransform)t.transform;
                    rtT.sizeDelta = new Vector2(150, 40);
                    var angulo = Mathf.PI / 2 - i * 2 * Mathf.PI / etiquetas.Length;
                    rtT.anchoredPosition = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * (lado * 0.5f + 18);
                }
            return radar;
        }

        // ================================================================ interno

        private void Vestir(Image img, Sprite sprite, Color color) {
            img.color = color;
            if (sprite == null) return;
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
        }

        private static void Configurar(HorizontalOrVerticalLayoutGroup grupo, float espacio, float relleno, TextAnchor alineacion) {
            grupo.spacing = espacio;
            var p = Mathf.RoundToInt(relleno);
            grupo.padding = new RectOffset(p, p, p, p);
            grupo.childAlignment = alineacion;
            grupo.childControlWidth = true;
            grupo.childControlHeight = true;
            // ★ Una columna da a cada hijo TODO su ancho. Sin esto, un texto mide lo que ocupa en una sola linea:
            // no se parte, y un briefing o una rubrica larga se salen de la pantalla. Una fila, en cambio,
            // respeta el ancho natural de cada pieza (y el que la quiera estirar pide flexAncho).
            grupo.childForceExpandWidth = grupo is VerticalLayoutGroup;
            grupo.childForceExpandHeight = false;
        }

        private Sprite _redondeado;
        private TMP_FontAsset _tiza;
        private bool _tizaIntentada;

        /// <summary>
        /// Un rectangulo de esquinas redondeadas, blanco y «9-slice»: estirado a cualquier tamaño conserva las
        /// esquinas. Para pastillas (etiquetas de flecha, chips) y cajas de los lienzos.
        /// </summary>
        public Sprite SpriteRedondeado() {
            if (_redondeado != null) return _redondeado;
            const int lado = 64, r = 30;
            var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false) { name = "RedondeadoNexus", wrapMode = TextureWrapMode.Clamp };
            var pixeles = new Color32[lado * lado];
            for (var y = 0; y < lado; y++)
                for (var x = 0; x < lado; x++) {
                    var cx = Mathf.Clamp(x + 0.5f, r, lado - r);
                    var cy = Mathf.Clamp(y + 0.5f, r, lado - r);
                    var d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    pixeles[y * lado + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(r - d) * 255));
                }
            tex.SetPixels32(pixeles);
            tex.Apply(false, true);
            _redondeado = Sprite.Create(tex, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), 100, 0,
                                        SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            _redondeado.name = "RedondeadoNexus";
            return _redondeado;
        }

        /// <summary>
        /// La letra de tiza de las recetas (Gochi Hand, licencia OFL, en Resources/Fuentes). Se crea en tiempo de
        /// ejecucion desde el TTF, con la fuente normal de reserva para cualquier glifo que no tenga. Si no esta,
        /// se usa la fuente del cuerpo: las recetas se leen igual, solo pierden el aire de pizarra.
        /// </summary>
        public TMP_FontAsset FuenteTiza {
            get {
                if (_tizaIntentada) return _tiza != null ? _tiza : Tema.FuenteCuerpo;
                _tizaIntentada = true;
                var ttf = Resources.Load<Font>("Fuentes/GochiHand-Regular");
                if (ttf != null) {
                    _tiza = TMP_FontAsset.CreateFontAsset(ttf);
                    if (_tiza != null && Tema.FuenteCuerpo != null)
                        _tiza.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { Tema.FuenteCuerpo };
                }
                return _tiza != null ? _tiza : Tema.FuenteCuerpo;
            }
        }

        /// <summary>Un circulo blanco de 128 px, generado una vez. Lo necesitan el dial y los puntos de estado.</summary>
        public Sprite SpriteCircular() {
            if (_circulo != null) return _circulo;

            const int lado = 128;
            var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false) { name = "CirculoNexus" };
            tex.wrapMode = TextureWrapMode.Clamp;
            var r = lado / 2f;
            var pixeles = new Color32[lado * lado];
            for (var y = 0; y < lado; y++)
                for (var x = 0; x < lado; x++) {
                    var d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                    var alfa = Mathf.Clamp01(r - d);   // borde suavizado de un pixel
                    pixeles[y * lado + x] = new Color32(255, 255, 255, (byte)(alfa * 255));
                }
            tex.SetPixels32(pixeles);
            tex.Apply(false, true);
            _circulo = Sprite.Create(tex, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), 100);
            _circulo.name = "CirculoNexus";
            return _circulo;
        }
    }
}
