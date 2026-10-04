using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// Marca los hijos que solo visten una pieza (borde, resplandor, esquinas): el layout no los cuenta y
    /// UiKit.Vaciar no los borra al repintar el contenido.
    /// </summary>
    public sealed class DecoracionNexus : MonoBehaviour { }

    /// <summary>
    /// Las piezas de UiKit segun el design system del juego (bundle.css del design system, mismas medidas, llevadas al
    /// lienzo con NexusTheme.Px). UiKit.cs decide entre esto y el aspecto clasico; las pantallas no se enteran.
    /// </summary>
    public sealed partial class UiKit {
        // ================================================================ vestir

        /// <summary>Le da a una Image la caja redondeada del design system (radio en px de web: NexusTheme.RadioMd…).</summary>
        public void Fondo(Image img, Color color, float radioWeb) {
            img.sprite = Formas.Caja(Tema.Px(radioWeb));
            img.type = Image.Type.Sliced;
            img.color = color;
        }

        /// <summary>El borde de 1px (en px de web) por dentro de la pieza. Va encima del contenido pero solo ocupa el filo.</summary>
        public Image Borde(RectTransform rt, Color color, float radioWeb, float grosorWeb = 1) {
            var img = Decoracion(rt, "Borde", 0).gameObject.AddComponent<Image>();
            img.sprite = Formas.Borde(Tema.Px(radioWeb), Mathf.Max(1, Tema.Px(grosorWeb)));
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>El resplandor de seleccion (glow-cyan) o de alerta (glow-danger), por fuera de la pieza.</summary>
        public Image Halo(RectTransform rt, Color color, float radioWeb, bool visible = true) {
            var ext = Tema.Px(14);
            var nodo = Decoracion(rt, "Resplandor", ext);
            var img = nodo.gameObject.AddComponent<Image>();
            img.sprite = Formas.Resplandor(Tema.Px(radioWeb), ext);
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            nodo.gameObject.SetActive(visible);
            return img;
        }

        /// <summary>
        /// Las cuatro esquinas en L del Panel: la firma visual del design system. Cyan normalmente; danger solo en un
        /// panel que reporta una condicion critica real. No anidar paneles con esquinas.
        /// </summary>
        public Image[] Esquinas(RectTransform rt, Color? color = null) {
            var c = color ?? Tema.cyan;
            var lado = Tema.Px(14);
            var grosor = Tema.Px(2);
            var piezas = new Image[8];
            for (var i = 0; i < 4; i++) {
                var derecha = i == 1 || i == 3;
                var abajo = i >= 2;
                var esquina = Decoracion(rt, "Esquina", 0);
                var ancla = new Vector2(derecha ? 1 : 0, abajo ? 0 : 1);
                esquina.anchorMin = esquina.anchorMax = esquina.pivot = ancla;
                esquina.sizeDelta = new Vector2(lado, lado);
                esquina.anchoredPosition = new Vector2(derecha ? 1 : -1, abajo ? -1 : 1);
                piezas[i * 2] = Barrita(esquina, ancla, new Vector2(lado, grosor), c);
                piezas[i * 2 + 1] = Barrita(esquina, ancla, new Vector2(grosor, lado), c);
            }
            return piezas;
        }

        /// <summary>Cambia el color del borde que UiKit le puso a una pieza (un panel que pasa a ser de aviso).</summary>
        public static void ColorDeBorde(RectTransform rt, Color color) {
            foreach (Transform hijo in rt)
                if (hijo.name == "Borde" && hijo.GetComponent<DecoracionNexus>() != null)
                    hijo.GetComponent<Image>().color = color;
        }

        private Image Barrita(RectTransform esquina, Vector2 ancla, Vector2 tamano, Color c) {
            var rt = Nodo(esquina, "Trazo");
            rt.anchorMin = rt.anchorMax = rt.pivot = ancla;
            rt.sizeDelta = tamano;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Un hijo que no cuenta para el layout, estirado sobre la pieza (con 'fuera' de margen hacia afuera).</summary>
        private RectTransform Decoracion(RectTransform rt, string nombre, float fuera) {
            var d = Nodo(rt, nombre);
            d.anchorMin = Vector2.zero;
            d.anchorMax = Vector2.one;
            d.offsetMin = new Vector2(-fuera, -fuera);
            d.offsetMax = new Vector2(fuera, fuera);
            d.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            d.gameObject.AddComponent<DecoracionNexus>();
            return d;
        }

        // ================================================================ texto

        private void EstilizarNexus(TMP_Text tmp, EstiloTexto estilo) {
            tmp.fontStyle = FontStyles.Normal;
            tmp.fontWeight = FontWeight.Regular;
            tmp.characterSpacing = 0;
            tmp.lineSpacing = 0;
            switch (estilo) {
                case EstiloTexto.Hero:
                    Fuente(tmp, Tema.FuenteDisplay, 64, Tema.ink, 2); tmp.fontWeight = FontWeight.Heavy; break;
                case EstiloTexto.Titulo:   // phase, un punto menos: aqui titula pantallas, no transiciones a pantalla completa
                    Fuente(tmp, Tema.FuenteDisplay, 30, Tema.ink, 1); break;
                case EstiloTexto.Subtitulo:   // panel-title, ajustado a las columnas estrechas del HUD
                    Fuente(tmp, Tema.FuenteDisplay, 17, Tema.ink, 4); tmp.fontStyle = FontStyles.UpperCase; break;
                case EstiloTexto.Encabezado:
                    Fuente(tmp, Tema.FuenteInterfaz, 17, Tema.ink); tmp.fontWeight = FontWeight.SemiBold; break;
                case EstiloTexto.Dialogo:
                    Fuente(tmp, Tema.FuenteInterfaz, 16, Tema.ink); tmp.lineSpacing = 12; break;
                case EstiloTexto.Etiqueta:
                    Fuente(tmp, Tema.FuenteInterfazNegrita, 13, Tema.ink, 6); tmp.fontStyle = FontStyles.UpperCase; break;
                case EstiloTexto.Leyenda:
                    Fuente(tmp, Tema.FuenteInterfaz, 11, Tema.inkMuted, 5); tmp.fontWeight = FontWeight.SemiBold; break;
                case EstiloTexto.Pequeno:   // texto secundario: body en ink-muted, un paso mas pequeño
                    Fuente(tmp, Tema.FuenteInterfaz, 13, Tema.inkMuted); break;
                case EstiloTexto.Mono:   // code
                    Fuente(tmp, Tema.FuenteCodigo, 13, Tema.ink); break;
                case EstiloTexto.CodigoEtiqueta:
                    Fuente(tmp, Tema.FuenteCodigo, 11, Tema.inkMuted, 4); tmp.fontWeight = FontWeight.SemiBold; break;
                case EstiloTexto.DocTitulo:
                    Fuente(tmp, Tema.FuenteDocumento, 22, Tema.paperInk); tmp.fontWeight = FontWeight.Bold; break;
                case EstiloTexto.DocCuerpo:
                    Fuente(tmp, Tema.FuenteDocumento, 14, Tema.paperInk); tmp.lineSpacing = 18; break;
                case EstiloTexto.DocLeyenda:
                    Fuente(tmp, Tema.FuenteDocumento, 11, Tema.paperMuted, 2); tmp.fontStyle = FontStyles.Italic; break;
                default:   // Cuerpo = body
                    Fuente(tmp, Tema.FuenteInterfaz, 15, Tema.ink); break;
            }
        }

        private void Fuente(TMP_Text tmp, TMP_FontAsset fuente, float tamWeb, Color color, float espaciadoEm100 = 0) {
            tmp.font = fuente;
            tmp.fontSize = Tema.Px(tamWeb);
            tmp.color = color;
            tmp.characterSpacing = espaciadoEm100;
        }

        // ================================================================ contenedores

        private RectTransform PanelNexus(Transform padre, string nombre, Color? color) {
            var rt = Nodo(padre, nombre);
            Fondo(rt.gameObject.AddComponent<Image>(), color ?? Tema.surface, NexusTheme.RadioLg);
            Borde(rt, Tema.line, NexusTheme.RadioLg);
            return rt;
        }

        private RectTransform TarjetaNexus(Transform padre, string titulo, Color acento, bool alerta) {
            var panel = Nodo(padre, "Tarjeta " + titulo);
            Fondo(panel.gameObject.AddComponent<Image>(), Tema.surface, NexusTheme.RadioLg);
            var grupo = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            Configurar(grupo, Tema.espacio, Tema.Espacio(5), TextAnchor.UpperLeft);
            if (!string.IsNullOrEmpty(titulo)) {
                var cabecera = Fila(panel, "Cabecera", Tema.Px(8));
                var barra = Nodo(cabecera, "Barra");
                barra.gameObject.AddComponent<Image>().color = alerta ? Tema.danger : acento;
                Tamano(barra, Tema.Px(3), Tema.Px(16));
                var t = Texto(cabecera, titulo, EstiloTexto.Subtitulo);
                Tamano(t, flexAncho: 1);
            }
            if (alerta) Halo(panel, Tema.danger, NexusTheme.RadioLg);
            Borde(panel, alerta ? Tema.danger : Tema.line, NexusTheme.RadioLg);
            Esquinas(panel, alerta ? Tema.danger : Tema.cyan);
            return panel;
        }

        /// <summary>
        /// Una tarjeta con tono. Peligro la convierte en panel de alerta (esquinas y borde danger, glow-danger): solo para
        /// una condicion critica real. Aviso/Exito/Violeta solo tiñen la barra del titulo.
        /// </summary>
        public RectTransform Tarjeta(Transform padre, string titulo, Tono tono) {
            if (Clasico) return Tarjeta(padre, titulo, (Color?)null);
            var acento = tono == Tono.Neutro ? Tema.cyan : Tema.ColorDe(tono);
            return TarjetaNexus(padre, titulo, acento, tono == Tono.Peligro);
        }

        private void SeparadorNexus(Transform padre) {
            var rt = Nodo(padre, "Separador");
            rt.gameObject.AddComponent<Image>().color = Tema.line;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = Mathf.Max(1, Tema.Px(1));
            le.flexibleWidth = 1;
        }

        // ================================================================ botones

        private Button BotonNexus(Transform padre, string texto, Action alPulsar, VarianteBoton variante) {
            var rt = Nodo(padre, "Boton " + texto);
            var fondo = rt.gameObject.AddComponent<Image>();
            Fondo(fondo, Color.clear, NexusTheme.RadioMd);
            var boton = rt.gameObject.AddComponent<Button>();
            boton.targetGraphic = fondo;
            boton.transition = Selectable.Transition.None;

            var menu = variante == VarianteBoton.Menu;
            var fantasma = variante == VarianteBoton.Fantasma;
            var relleno = fantasma ? Tema.Espacio(3) : Tema.Espacio(5);
            var etiqueta = Texto(rt, texto, EstiloTexto.Etiqueta, null,
                                 menu ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center);
            etiqueta.textWrappingMode = TextWrappingModes.NoWrap;
            etiqueta.margin = new Vector4(relleno, 0, relleno, 0);
            Rellenar((RectTransform)etiqueta.transform);

            var colorHalo = variante == VarianteBoton.Peligro ? Tema.danger : Tema.cyan;
            var halo = fantasma ? null : Halo(rt, colorHalo, NexusTheme.RadioMd, false);
            var borde = Borde(rt, Color.clear, NexusTheme.RadioMd);

            var v = rt.gameObject.AddComponent<VisualDeBoton>();
            v.Fondo = fondo; v.Borde = borde; v.Halo = halo; v.Etiqueta = etiqueta;
            var elegido = Estado(Tema.cyanSoft, Tema.cyan, Tema.ink, true);
            switch (variante) {
                case VarianteBoton.Primario:
                    v.Normal = Estado(Tema.cyan, Tema.cyan, Tema.onCyan);
                    v.Encima = Estado(Color.Lerp(Tema.cyan, Color.white, 0.1f), Tema.cyan, Tema.onCyan, true);
                    elegido = v.Encima;
                    break;
                case VarianteBoton.Peligro:
                    v.Normal = Estado(Color.clear, Tema.danger, Tema.danger);
                    v.Encima = Estado(Tema.dangerSoft, Tema.danger, Tema.danger, true);
                    elegido = v.Encima;
                    break;
                case VarianteBoton.Fantasma:
                    v.Normal = Estado(Color.clear, Color.clear, Tema.inkMuted);
                    v.Encima = Estado(Tema.surface, Color.clear, Tema.ink);
                    elegido = Estado(Tema.cyanSoft, Color.clear, Tema.cyan);
                    break;
                case VarianteBoton.Menu:
                    v.Normal = Estado(Tema.surface, Tema.line, Tema.ink);
                    v.Encima = Estado(Tema.surface, Tema.cyan, Tema.cyan, true);
                    elegido = v.Encima;
                    break;
                default:
                    v.Normal = Estado(Tema.surfaceRaised, Tema.line, Tema.ink);
                    v.Encima = Estado(Color.Lerp(Tema.surfaceRaised, Color.white, 0.04f), Tema.lineStrong, Tema.ink);
                    break;
            }
            v.ElegidoEstado = elegido;
            v.Aplicar();

            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = menu ? Tema.altoBoton + 8 : fantasma ? Tema.altoBoton * 0.8f : Tema.altoBoton;
            le.preferredWidth = Mathf.Max(fantasma ? 100 : 160, etiqueta.GetPreferredValues(texto).x + relleno * 2);

            if (alPulsar != null) boton.onClick.AddListener(new UnityAction(alPulsar));
            return boton;
        }

        private static VisualDeBoton.Estado Estado(Color fondo, Color borde, Color texto, bool halo = false) {
            return new VisualDeBoton.Estado { Fondo = fondo, Borde = borde, Texto = texto, Halo = halo };
        }

        /// <summary>SelectionCard: titulo en heading y una linea tenue debajo. Se elige con Resaltar.</summary>
        private Button BotonDeOpcionNexus(Transform padre, string texto, string detalle, Action alPulsar) {
            var rt = Nodo(padre, "Opcion");
            var fondo = rt.gameObject.AddComponent<Image>();
            Fondo(fondo, Tema.surface, NexusTheme.RadioLg);
            var boton = rt.gameObject.AddComponent<Button>();
            boton.targetGraphic = fondo;
            boton.transition = Selectable.Transition.None;

            var grupo = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            Configurar(grupo, Tema.Px(4), Tema.Espacio(4), TextAnchor.MiddleLeft);
            // Sin alto minimo fijo: el del grupo ya suma el de sus textos. Fijarlo dejaba que una columna apretada
            // encogiera la opcion por debajo de su texto (las respuestas al cliente se montaban unas sobre otras).

            var t = Texto(rt, texto, EstiloTexto.Encabezado);
            if (!string.IsNullOrEmpty(detalle)) Texto(rt, detalle, EstiloTexto.Pequeno);

            var v = rt.gameObject.AddComponent<VisualDeBoton>();
            v.Fondo = fondo;
            v.Halo = Halo(rt, Tema.cyan, NexusTheme.RadioLg, false);
            v.Borde = Borde(rt, Tema.line, NexusTheme.RadioLg);
            v.Etiqueta = t;
            v.Normal = Estado(Tema.surface, Tema.line, Tema.ink);
            v.Encima = Estado(Tema.surface, Tema.lineStrong, Tema.ink);
            v.ElegidoEstado = Estado(Tema.cyanSoft, Tema.cyan, Tema.ink, true);
            v.Aplicar();

            if (alPulsar != null) boton.onClick.AddListener(new UnityAction(alPulsar));
            return boton;
        }

        /// <summary>
        /// TagOption: un diagnostico que se marca sobre la evidencia (auditorias). El punto de color dice el tono aunque
        /// no este marcada; marcada lleva borde, fondo entintado y, en cyan y danger, resplandor. Se marca con Resaltar.
        /// </summary>
        public Button OpcionDeEtiqueta(Transform padre, string texto, Tono tono, Action alPulsar, string detalle = null) {
            if (Clasico) return BotonDeOpcion(padre, texto, detalle, alPulsar);
            var rt = Nodo(padre, "Etiqueta " + texto);
            var fondo = rt.gameObject.AddComponent<Image>();
            Fondo(fondo, Tema.surfaceRaised, NexusTheme.RadioSm);
            var boton = rt.gameObject.AddComponent<Button>();
            boton.targetGraphic = fondo;
            boton.transition = Selectable.Transition.None;
            var fila = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            Configurar(fila, Tema.Espacio(2), 0, TextAnchor.MiddleLeft);
            fila.padding = new RectOffset((int)Tema.Espacio(3), (int)Tema.Espacio(3), (int)Tema.Espacio(2), (int)Tema.Espacio(2));
            Punto(rt, Tema.ColorDe(tono), 8);
            var col = Columna(rt, "Texto", 2);
            Tamano(col, flexAncho: 1);
            var t = Texto(col, texto, EstiloTexto.Cuerpo, Tema.inkMuted);
            if (!string.IsNullOrEmpty(detalle)) Texto(col, detalle, EstiloTexto.Leyenda, Tema.inkFaint);

            var v = rt.gameObject.AddComponent<VisualDeBoton>();
            v.Fondo = fondo;
            var resplandor = tono == Tono.Cyan || tono == Tono.Peligro;
            v.Halo = resplandor ? Halo(rt, Tema.ColorDe(tono), NexusTheme.RadioSm, false) : null;
            v.Borde = Borde(rt, Tema.line, NexusTheme.RadioSm);
            v.Etiqueta = t;
            v.Normal = Estado(Tema.surfaceRaised, Tema.line, Tema.inkMuted);
            v.Encima = Estado(Tema.surfaceRaised, Tema.lineStrong, Tema.ink);
            v.ElegidoEstado = Estado(Tema.SuaveDe(tono), Tema.ColorDe(tono), Tema.ink, resplandor);
            v.Aplicar();
            if (alPulsar != null) boton.onClick.AddListener(new UnityAction(alPulsar));
            return boton;
        }

        /// <summary>Un punto de estado redondo (badge, roster, notificacion, etiqueta).</summary>
        public Image Punto(Transform padre, Color color, float diametroWeb) {
            var rt = Nodo(padre, "Punto");
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = SpriteCircular();
            img.color = color;
            img.raycastTarget = false;
            Tamano(rt, Tema.Px(diametroWeb), Tema.Px(diametroWeb));
            return img;
        }

        private bool ResaltarNexus(Button boton, bool elegido, Color? normal) {
            if (boton == null) return true;
            var v = boton.GetComponent<VisualDeBoton>();
            if (v == null) return false;
            if (normal.HasValue) v.Normal.Fondo = normal.Value;
            v.Elegido = elegido;
            return true;
        }

        /// <summary>Marca una pieza elegida con el tono de su significado (una etiqueta danger, un aviso…).</summary>
        public void Resaltar(Button boton, bool elegido, Tono tono) {
            var v = boton != null ? boton.GetComponent<VisualDeBoton>() : null;
            if (Clasico || v == null) { Resaltar(boton, elegido); return; }
            var color = tono == Tono.Neutro ? Tema.cyan : Tema.ColorDe(tono);
            v.ElegidoEstado = Estado(tono == Tono.Neutro ? Tema.cyanSoft : Tema.SuaveDe(tono), color, Tema.ink,
                                     v.Halo != null && (tono == Tono.Cyan || tono == Tono.Neutro || tono == Tono.Peligro));
            if (v.Halo != null) v.Halo.color = color;
            v.Elegido = elegido;
        }

        // ================================================================ campos y chips

        private TMP_InputField CampoDeTextoNexus(Transform padre, string indicacion, string valor, float ancho) {
            var rt = Nodo(padre, "Campo");
            var fondo = rt.gameObject.AddComponent<Image>();
            Fondo(fondo, Tema.surfaceSunken, NexusTheme.RadioSm);
            var campo = rt.gameObject.AddComponent<TMP_InputField>();
            campo.targetGraphic = fondo;
            campo.transition = Selectable.Transition.None;
            Tamano(rt, ancho, Tema.altoBoton);

            var area = Rellenar(Nodo(rt, "Area"));
            area.offsetMin = new Vector2(Tema.Espacio(3), 6);
            area.offsetMax = new Vector2(-Tema.Espacio(3), -6);
            area.gameObject.AddComponent<RectMask2D>();

            var marcador = Texto(area, indicacion, EstiloTexto.Cuerpo, Tema.inkFaint, TextAlignmentOptions.MidlineLeft);
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
            // Sin customCaretColor, TMP ignora caretColor: el cursor salia casi invisible (feedback beta #2).
            campo.customCaretColor = true;
            campo.caretColor = Tema.cyan;
            campo.caretWidth = 3;
            campo.caretBlinkRate = 0.85f;
            campo.selectionColor = NexusTheme.Alfa(Tema.cyan, 0.35f);
            // Foco = borde cyan con resplandor (focus-ring del design system).
            var halo = Halo(rt, Tema.cyan, NexusTheme.RadioSm, false);
            var borde = Borde(rt, Tema.lineStrong, NexusTheme.RadioSm);
            campo.onSelect.AddListener(_ => { marcador.alpha = 0; borde.color = Tema.cyan; halo.gameObject.SetActive(true); });
            campo.onDeselect.AddListener(_ => { marcador.alpha = 1; borde.color = Tema.lineStrong; halo.gameObject.SetActive(false); });
            campo.text = valor ?? "";
            return campo;
        }

        private RectTransform ChipNexus(Transform padre, string texto, Color fondo, Color colorTexto) {
            var chip = Nodo(padre, "Chip " + texto);
            Fondo(chip.gameObject.AddComponent<Image>(), fondo, NexusTheme.RadioXs);
            var fila = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
            Configurar(fila, Tema.Espacio(1), 0, TextAnchor.MiddleCenter);
            fila.padding = new RectOffset((int)Tema.Espacio(2), (int)Tema.Espacio(2), (int)Tema.Px(2), (int)Tema.Px(2));
            var t = Texto(chip, texto, EstiloTexto.Leyenda, colorTexto, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return chip;
        }

        /// <summary>
        /// Badge: una o dos palabras de estado, siempre punto de color + texto (nunca color solo). Danger = urgente,
        /// Aviso = riesgo o deuda, Exito = hecho, Cyan = info, Neutro = roles y metadatos. 'solido' solo para la fila
        /// aislada que debe ganar sobre el resto.
        /// </summary>
        public RectTransform Badge(Transform padre, string texto, Tono tono = Tono.Neutro, bool solido = false) {
            if (Clasico) return Chip(padre, texto, Tema.cian);
            var color = Tema.ColorDe(tono);
            var fondo = solido && tono != Tono.Neutro ? color : Tema.SuaveDe(tono);
            var tinta = solido && tono != Tono.Neutro ? Tema.SobreDe(tono) : color;
            var chip = ChipNexus(padre, texto, fondo, tinta);
            Punto(chip, tinta, 6).transform.SetAsFirstSibling();
            return chip;
        }

        // ================================================================ indicadores

        private BarraView BarraNexus(Transform padre, float valor01, Color color, string nombre) {
            var rt = Nodo(padre, nombre);
            Fondo(rt.gameObject.AddComponent<Image>(), Tema.surfaceSunken, NexusTheme.RadioXs);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = Tema.Px(8);
            le.flexibleWidth = 1;

            var relleno = Nodo(rt, "Relleno");
            relleno.anchorMin = Vector2.zero;
            relleno.anchorMax = new Vector2(0, 1);
            relleno.offsetMin = relleno.offsetMax = Vector2.zero;
            var img = relleno.gameObject.AddComponent<Image>();
            Fondo(img, color, NexusTheme.RadioXs);
            Borde(rt, Tema.line, NexusTheme.RadioXs);

            var barra = rt.gameObject.AddComponent<BarraView>();
            barra.Configurar(relleno, img);
            barra.Valor = valor01;
            return barra;
        }

        /// <summary>Gauge: un aro fino que se llena desde arriba, el valor dentro y la etiqueta debajo.</summary>
        private DialView DialNexus(Transform padre, float valor01, string etiqueta, Color color, float diametro) {
            var rt = Nodo(padre, "Dial " + etiqueta);
            var altoEtiqueta = Tema.Px(11) * 1.8f;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = diametro;
            le.minHeight = le.preferredHeight = diametro + altoEtiqueta;

            var aro = Nodo(rt, "Aro");
            aro.anchorMin = aro.anchorMax = new Vector2(0.5f, 1);
            aro.pivot = new Vector2(0.5f, 1);
            aro.sizeDelta = new Vector2(diametro, diametro);
            var anillo = Formas.Borde(diametro / 2f, Mathf.Max(4, diametro * 6f / 88f));
            var pista = aro.gameObject.AddComponent<Image>();
            pista.sprite = anillo;
            pista.color = Tema.surfaceSunken;
            pista.raycastTarget = false;

            var arco = Rellenar(Nodo(aro, "Arco"));
            var img = arco.gameObject.AddComponent<Image>();
            img.sprite = anillo;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillOrigin = (int)Image.Origin360.Top;
            img.fillClockwise = true;
            img.color = color;
            img.raycastTarget = false;

            var valor = Texto(aro, "", EstiloTexto.Cuerpo, Tema.ink, TextAlignmentOptions.Center);
            valor.font = Tema.FuenteInterfazNegrita;
            valor.fontSize = diametro * 0.2f;
            Rellenar((RectTransform)valor.transform);

            var nombre = Texto(rt, etiqueta, EstiloTexto.Leyenda, null, TextAlignmentOptions.Bottom);
            nombre.fontStyle = FontStyles.UpperCase;
            var rtNombre = (RectTransform)nombre.transform;
            rtNombre.anchorMin = Vector2.zero;
            rtNombre.anchorMax = new Vector2(1, 0);
            rtNombre.pivot = new Vector2(0.5f, 0);
            rtNombre.sizeDelta = new Vector2(0, altoEtiqueta);

            var dial = rt.gameObject.AddComponent<DialView>();
            dial.Configurar(img, valor);
            dial.Valor = valor01;
            return dial;
        }
    }
}
