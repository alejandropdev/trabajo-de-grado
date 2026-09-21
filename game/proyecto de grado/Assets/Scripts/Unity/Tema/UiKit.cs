using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nexus.Unity.Tema {
    public enum EstiloTexto { Titulo, Subtitulo, Cuerpo, Pequeno, Mono }

    public enum VarianteBoton {
        /// <summary>La accion principal de la pantalla. Una, como mucho dos.</summary>
        Primario,
        /// <summary>El resto de acciones.</summary>
        Secundario,
        /// <summary>Lo que tiene consecuencias: borrar, quedarse hasta las 20:00. Cuenta para el 12 % de mostaza.</summary>
        Peligro,
        /// <summary>Solo texto: «volver», «cancelar».</summary>
        Fantasma
    }

    /// <summary>
    /// La fabrica de piezas de interfaz. Ninguna pantalla crea un Image o un TextMeshProUGUI a mano: se los
    /// pide a UiKit, y UiKit los viste con el NexusTheme. Asi el tema es de verdad el unico sitio donde vive
    /// el aspecto del juego.
    ///
    /// Todo se construye con uGUI y layout automatico (VerticalLayoutGroup / HorizontalLayoutGroup). Las
    /// pantallas describen QUE hay y en que orden; UiKit decide como se ve.
    /// </summary>
    public sealed class UiKit {
        public NexusTheme Tema { get; }

        private Sprite _circulo;

        public UiKit(NexusTheme tema) {
            Tema = tema != null ? tema : NexusTheme.PorDefecto();
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
            Configurar(grupo, espacio ?? Tema.espacio, relleno ?? Tema.espacio, TextAnchor.UpperLeft);
            return rt;
        }

        /// <summary>Apila hijos de arriba a abajo. Es el contenedor por defecto de casi todas las pantallas.</summary>
        public RectTransform Columna(Transform padre, string nombre = "Columna", float? espacio = null,
                                    float relleno = 0, TextAnchor alineacion = TextAnchor.UpperLeft) {
            var rt = Nodo(padre, nombre);
            var grupo = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            Configurar(grupo, espacio ?? Tema.espacio, relleno, alineacion);
            return rt;
        }

        /// <summary>Pone hijos uno al lado del otro.</summary>
        public RectTransform Fila(Transform padre, string nombre = "Fila", float? espacio = null,
                                 float relleno = 0, TextAnchor alineacion = TextAnchor.MiddleLeft) {
            var rt = Nodo(padre, nombre);
            var grupo = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            Configurar(grupo, espacio ?? Tema.espacio, relleno, alineacion);
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

            scroll.viewport = visor;
            scroll.content = contenido;
            return scroll;
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
            le.flexibleWidth = le.flexibleHeight = 1;
        }

        /// <summary>Una linea fina de separacion.</summary>
        public void Separador(Transform padre) {
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
            return le;
        }

        /// <summary>Destruye todos los hijos. Para repintar una lista.</summary>
        public static void Vaciar(Transform contenedor) {
            for (var i = contenedor.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(contenedor.GetChild(i).gameObject);
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
            if (color.HasValue) tmp.color = color.Value;
            return tmp;
        }

        public void Estilizar(TMP_Text tmp, EstiloTexto estilo) {
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
            etiqueta.margin = new Vector4(Tema.espacio * 1.5f, 0, Tema.espacio * 1.5f, 0);
            Rellenar((RectTransform)etiqueta.transform);

            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = Tema.altoBoton;
            le.preferredWidth = Mathf.Max(160, etiqueta.GetPreferredValues(texto).x + Tema.espacio * 3);

            if (alPulsar != null) boton.onClick.AddListener(new UnityAction(alPulsar));
            return boton;
        }

        /// <summary>
        /// Un boton para frases largas (las opciones de una decision). Su alto sale de su texto, que se parte
        /// en varias lineas: un Boton normal tiene alto fijo, y una frase de dos lineas se sale de el. Debajo
        /// del texto puede llevar una segunda linea tenue (lo que va a cambiar, por que esta bloqueado…).
        /// </summary>
        public Button BotonDeOpcion(Transform padre, string texto, string detalle, Action alPulsar) {
            var rt = Nodo(padre, "Opcion");
            var img = rt.gameObject.AddComponent<Image>();
            Vestir(img, Tema.spriteBoton, Color.white);
            var boton = rt.gameObject.AddComponent<Button>();
            boton.targetGraphic = img;
            boton.colors = Colores(Tema.pared);

            var grupo = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            Configurar(grupo, 4, Tema.espacio * 1.5f, TextAnchor.MiddleLeft);
            Tamano(rt, alto: null).minHeight = Tema.altoBoton;

            var t = Texto(rt, texto, EstiloTexto.Cuerpo);
            t.fontStyle = FontStyles.Bold;
            if (!string.IsNullOrEmpty(detalle)) Texto(rt, detalle, EstiloTexto.Pequeno);

            if (alPulsar != null) boton.onClick.AddListener(new UnityAction(alPulsar));
            return boton;
        }

        /// <summary>Una tarjeta: un panel con titulo cuyo alto sale de su contenido. Devuelve donde meter las cosas.</summary>
        public RectTransform Tarjeta(Transform padre, string titulo, Color? colorTitulo = null) {
            var panel = PanelColumna(padre, "Tarjeta " + titulo, Tema.margen * 0.75f, Tema.espacio);
            if (!string.IsNullOrEmpty(titulo)) Texto(panel, titulo.ToUpperInvariant(), EstiloTexto.Pequeno, colorTitulo ?? Tema.cian);
            return panel;
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
            var rt = Nodo(padre, nombre);
            Vestir(rt.gameObject.AddComponent<Image>(), Tema.spriteBarraFondo, Tema.hormigon);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = Tema.altoBarra;
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

            var radar = rt.gameObject.AddComponent<GraficoRadar>();
            radar.raycastTarget = false;
            radar.ColorRejilla = Tema.hormigon;
            radar.ColorValor = new Color(Tema.cian.r, Tema.cian.g, Tema.cian.b, 0.45f);
            radar.ColorBorde = Tema.cian;
            radar.Valores = valores01;

            if (etiquetas != null)
                for (var i = 0; i < etiquetas.Length; i++) {
                    var t = Texto(rt, etiquetas[i], EstiloTexto.Pequeno, null, TextAlignmentOptions.Center);
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
