using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nexus.Core.Coleccion;
using Nexus.Core.Tablero;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Revisar un cambio de codigo (2026-11-07) como en un IDE: barra de titulo, barra de actividad, explorador con el
    /// archivo, pestaña con «DIFF», el diff con sus dos margenes de numeros (antes / despues), marcas + y -, fondos verde
    /// y rojo y el codigo coloreado; a la derecha, el pull request (quien, que dice, +N -M); abajo, la barra de estado
    /// con la rama. Las dos decisiones son una mano de cartas (reparto, sello y giro) que llega cuando el codigo ya esta.
    ///
    /// Al decidir: si se aprueba, lo quitado se tacha y se cierra, lo añadido pierde sus marcas y la barra de estado se
    /// pone verde («fusionado»); si se piden cambios, la barra se pone ambar. En los dos casos llega al pull request el
    /// resultado de la revision. Un clic salta la animacion; Escape cierra (sin decidir: lo que ya pasaba).
    /// </summary>
    public sealed class PantallaDeRevisionDeCodigo : Pantalla, IPointerClickHandler {
        public MicroPr Pr;
        public bool TrabajasSolo;
        /// <summary>Hace la revision en el motor ("aprobar" o "cambios") y devuelve lo que salio (o null si no se pudo).</summary>
        public Func<string, ResultadoDeRevision> Decidir;
        public Action AlCerrar;

        public override bool EsModal { get { return true; } }

        private static readonly Color FondoIde = new Color32(0x1B, 0x1D, 0x22, 0xFF);
        private static readonly Color BarraOscura = new Color32(0x14, 0x15, 0x19, 0xFF);
        private static readonly Color Lateral = new Color32(0x17, 0x19, 0x1D, 0xFF);
        private static readonly Color Linea = new Color32(0x2A, 0x2E, 0x36, 0xFF);

        /// <summary>El IDE ocupa el 95 % de la pantalla (2026-11-07); se mide al construir.</summary>
        private float AnchoIde = 1824, AltoIde = 1026;
        private const float AnchoActividad = 0, AnchoExplorador = 250, AnchoPr = 460, AltoTitulo = 38, AltoPestanas = 40, AltoEstado = 28;
        /// <summary>La capa de la decision: delante del editor, abajo.</summary>
        private const float AltoCapa = 400;

        private ContextoDeHallazgo _x;
        private Secuencia _sec, _despues;
        private RectTransform _ventana, _panelPr, _sitioMano, _capa;
        private CanvasGroup _grupoCapa;
        private GameObject _cerrarSinDecidir;
        private TMP_Text _aviso;
        private Image _estado;
        private TMP_Text _textoEstadoIzq, _textoEstadoDer;
        private readonly List<FilaDeDiff> _filas = new List<FilaDeDiff>();
        private ManoDeCartas _mano;
        private bool _cerrado, _decidido, _cerrarAlFinal;
        private string _lenguaje;

        private sealed class FilaDeDiff {
            public RectTransform Rt;
            public CanvasGroup Grupo;
            public Image Fondo, Tachon;
            public TMP_Text Marca;
            public string Tipo;
            public float Y;
        }

        protected override void Construir() {
            var fondo = Raiz.gameObject.AddComponent<Image>();
            fondo.color = NexusTheme.Alfa(Tema.bg950, 0.9f);
            _x = new ContextoDeHallazgo { Ui = Ui, Tema = Tema };
            _sec = Raiz.gameObject.AddComponent<Secuencia>();
            _x.Sec = _sec;

            var lineas = Pr.Lineas ?? new List<Nexus.Core.Minijuegos.LineaDiff>();
            _lenguaje = ResaltadoDeCodigo.Adivinar(string.Join("\n", lineas.Select(l => l.Texto)));
            var slug = Slug(Pr.Titulo);
            var archivo = slug + ResaltadoDeCodigo.Extension(_lenguaje);

            var lienzo = Raiz.rect;
            if (lienzo.width > 100 && lienzo.height > 100) { AnchoIde = lienzo.width * 0.95f; AltoIde = lienzo.height * 0.95f; }
            _ventana = _x.Nodo(Raiz, "IDE", 0, 0, AnchoIde, AltoIde);
            Ui.Fondo(_ventana.gameObject.AddComponent<Image>(), FondoIde, 12);
            _ventana.gameObject.AddComponent<RectMask2D>();
            Ui.Borde(_ventana, Linea, 12, 1.5f);
            var gVentana = _ventana.gameObject.AddComponent<CanvasGroup>();
            _sec.En(0f, 0.25f, p => {
                gVentana.alpha = p;
                _ventana.localScale = Vector3.one * Mathf.Lerp(0.97f, 1f, ContextoDeHallazgo.Suave(p));
            });

            BarraDeTitulo(archivo);
            Explorador(archivo);
            Pestanas(archivo);
            var finDelCodigo = Diff(lineas);
            PanelDelPr(slug);
            BarraDeEstado(slug, lineas.Count);
            CapaDeDecision(finDelCodigo);
        }

        // ==================================================================== la ventana

        private float Izq(float ancho) { return -AnchoIde / 2 + ancho; }

        private void BarraDeTitulo(string archivo) {
            var y = AltoIde / 2 - AltoTitulo / 2;
            _x.Caja(_ventana, "Titulo", 0, y, AnchoIde, AltoTitulo, BarraOscura);
            _x.Circulo(_ventana, "Cerrar", Izq(22), y, 13, new Color32(0xF8, 0x71, 0x71, 0xFF));
            _x.Circulo(_ventana, "Minimizar", Izq(42), y, 13, new Color32(0xFB, 0xBF, 0x24, 0xFF));
            _x.Circulo(_ventana, "Ampliar", Izq(62), y, 13, new Color32(0x34, 0xD3, 0x99, 0xFF));
            _x.Texto(_ventana, archivo + " - nexus - revisión de código", EstiloTexto.Mono, Tema.inkMuted, 0, y, 800, AltoTitulo).fontSize *= 0.9f;
            var ayuda = Ui.Boton(_ventana, "¿Qué es revisar código?", () => {
                var e = Nexus.Core.Narrativa.Glosario.Buscar(App.Catalogo.Glosario, "revision-de-codigo");
                if (e != null) PantallaDePizarra.AbrirConcepto(App, e);
            }, VarianteBoton.Fantasma);
            var ra = (RectTransform)ayuda.transform;
            UiKit.Tamano(ayuda).ignoreLayout = true;
            ra.anchorMin = ra.anchorMax = ra.pivot = new Vector2(0.5f, 0.5f);
            ra.sizeDelta = new Vector2(280, AltoTitulo - 6);
            ra.anchoredPosition = new Vector2(AnchoIde / 2 - 150, y);
        }

        private void Explorador(string archivo) {
            var alto = AltoIde - AltoTitulo - AltoEstado;
            var cx = Izq(AnchoActividad + AnchoExplorador / 2);
            var arriba = AltoIde / 2 - AltoTitulo;
            _x.Caja(_ventana, "Explorador", cx, arriba - alto / 2, AnchoExplorador, alto, Lateral);
            _x.Caja(_ventana, "Raya", Izq(AnchoActividad + AnchoExplorador), arriba - alto / 2, 1, alto, Linea);
            var x0 = Izq(AnchoActividad + 18);
            _x.Texto(_ventana, "EXPLORADOR", EstiloTexto.Leyenda, Tema.inkMuted, x0 + 100, arriba - 22, 200, 24, TextAlignmentOptions.Left);
            var arbol = new[] { "src/", "  api/", "  modelos/", "  " + archivo, "  vistas/", "pruebas/", "README.md" };
            for (var i = 0; i < arbol.Length; i++) {
                var y = arriba - 60 - i * 28;
                var elegido = i == 3;
                if (elegido) {
                    _x.Caja(_ventana, "Elegido", cx, y, AnchoExplorador, 26, NexusTheme.Alfa(Tema.cyan, 0.14f));
                    _x.Caja(_ventana, "Raya", Izq(1.5f), y, 3, 26, Tema.cyan);
                }
                var t = _x.Texto(_ventana, arbol[i], EstiloTexto.Mono, elegido ? Tema.ink : Tema.inkMuted, x0 + 100, y, 200, 26, TextAlignmentOptions.Left);
                t.fontSize *= 0.85f;
                t.overflowMode = TextOverflowModes.Ellipsis;
                if (elegido) _x.Texto(_ventana, "M", EstiloTexto.Mono, Tema.warning, Izq(AnchoActividad + AnchoExplorador - 16), y, 20, 26).fontSize *= 0.85f;
            }
        }

        private float EditorIzq { get { return Izq(AnchoActividad + AnchoExplorador); } }
        private float EditorAncho { get { return AnchoIde - AnchoActividad - AnchoExplorador - AnchoPr; } }

        private void Pestanas(string archivo) {
            var y = AltoIde / 2 - AltoTitulo - AltoPestanas / 2;
            _x.Caja(_ventana, "Pestañas", EditorIzq + EditorAncho / 2, y, EditorAncho, AltoPestanas, Lateral);
            var pestana = _x.Caja(_ventana, "Pestaña", EditorIzq + 140, y, 280, AltoPestanas, FondoIde);
            _x.Caja(pestana.transform, "Raya", 0, AltoPestanas / 2 - 1.5f, 280, 3, Tema.cyan);
            var t = _x.Texto(pestana.transform, archivo, EstiloTexto.Mono, Tema.ink, -24, 0, 210, AltoPestanas, TextAlignmentOptions.Left);
            t.fontSize *= 0.85f;
            t.overflowMode = TextOverflowModes.Ellipsis;
            var chip = _x.Caja(pestana.transform, "Diff", 108, 0, 52, 20, NexusTheme.Alfa(Tema.warning, 0.2f), 4);
            _x.Texto(chip.transform, "DIFF", EstiloTexto.Leyenda, Tema.warning, 0, 0, 52, 20).fontSize *= 0.85f;
        }

        // ==================================================================== el diff

        /// <summary>Pinta las lineas del diff y programa su entrada. Devuelve cuando termina de entrar la ultima.</summary>
        private float Diff(List<Nexus.Core.Minijuegos.LineaDiff> lineas) {
            var numeros = NumeracionDeDiff.Numerar(lineas, 12);
            var arriba = AltoIde / 2 - AltoTitulo - AltoPestanas;
            var cabecera = _x.Texto(_ventana, "@@ -12 +12 @@  " + Pr.Titulo, EstiloTexto.Mono, NexusTheme.Alfa(Tema.cyan, 0.7f),
                                    EditorIzq + EditorAncho / 2 + 10, arriba - 20, EditorAncho - 20, 24, TextAlignmentOptions.Left);
            cabecera.fontSize *= 0.85f;
            cabecera.overflowMode = TextOverflowModes.Ellipsis;

            var disponible = AltoIde - AltoTitulo - AltoPestanas - AltoEstado - 70 - AltoCapa;
            var alto = Mathf.Clamp(disponible / Mathf.Max(1, lineas.Count), 20, 34);
            var y0 = arriba - 48;
            const float t0 = 0.3f;
            for (var i = 0; i < lineas.Count; i++) {
                var l = lineas[i];
                var fila = FilaDelDiff(l, numeros[i], y0 - i * alto - alto / 2, alto);
                _filas.Add(fila);
                var retraso = t0 + Mathf.Min(i, 14) * 0.045f;
                _sec.En(retraso, 0.22f, p => {
                    fila.Grupo.alpha = p;
                    fila.Rt.anchoredPosition = new Vector2(Mathf.Lerp(-10, 0, ContextoDeHallazgo.Suave(p)), fila.Y);
                });
                if (l.Tipo == "add")
                    _sec.En(retraso + 0.15f, 0.45f, p => fila.Fondo.color = NexusTheme.Alfa(Tema.success, Mathf.Lerp(0.38f, 0.13f, p)));
            }
            var fin = t0 + Mathf.Min(lineas.Count, 14) * 0.045f + 0.22f;
            // El cursor, al final, parpadeando.
            var cursor = _x.Caja(_ventana, "Cursor", EditorIzq + 128, y0 - lineas.Count * alto - alto / 2, 2, alto - 8, Tema.ink);
            cursor.enabled = false;
            _sec.Siempre(t => { if (cursor != null) cursor.enabled = t > fin && Mathf.Repeat(t, 1f) < 0.55f; });
            return fin;
        }

        private FilaDeDiff FilaDelDiff(Nexus.Core.Minijuegos.LineaDiff l, NumeracionDeDiff.Numeros n, float y, float alto) {
            var fila = new FilaDeDiff { Tipo = l.Tipo, Y = y };
            var rt = Ui.Nodo(_ventana, "Linea");
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(AnchoIde, alto);
            rt.anchoredPosition = new Vector2(0, y);
            fila.Rt = rt;
            fila.Grupo = rt.gameObject.AddComponent<CanvasGroup>();
            fila.Grupo.alpha = 0;
            fila.Grupo.blocksRaycasts = false;
            var color = l.Tipo == "add" ? Tema.success : l.Tipo == "del" ? Tema.danger : Color.clear;
            fila.Fondo = _x.Caja(rt, "Fondo", EditorIzq + EditorAncho / 2, 0, EditorAncho, alto, l.Tipo == "ctx" ? Color.clear : NexusTheme.Alfa(color, 0.13f));
            if (l.Tipo != "ctx") _x.Caja(rt, "Borde", EditorIzq + 1.5f, 0, 3, alto, color);
            var gris = NexusTheme.Alfa(Tema.inkFaint, 0.9f);
            _x.Texto(rt, n.Antes > 0 ? n.Antes.ToString() : "", EstiloTexto.Mono, gris, EditorIzq + 26, 0, 34, alto, TextAlignmentOptions.Right).fontSize *= 0.8f;
            _x.Texto(rt, n.Despues > 0 ? n.Despues.ToString() : "", EstiloTexto.Mono, gris, EditorIzq + 64, 0, 34, alto, TextAlignmentOptions.Right).fontSize *= 0.8f;
            fila.Marca = _x.Texto(rt, l.Tipo == "add" ? "+" : l.Tipo == "del" ? "-" : "", EstiloTexto.Mono, color, EditorIzq + 98, 0, 16, alto);
            var codigo = _x.Texto(rt, ResaltadoDeCodigo.Colorear(l.Texto ?? "", _lenguaje), EstiloTexto.Mono, Tema.ink,
                                  EditorIzq + 112 + (EditorAncho - 124) / 2, 0, EditorAncho - 124, alto, TextAlignmentOptions.Left);
            codigo.textWrappingMode = TextWrappingModes.NoWrap;
            codigo.overflowMode = TextOverflowModes.Ellipsis;
            if (l.Tipo == "del") {
                codigo.alpha = 0.8f;
                // El tachon, para cuando se apruebe: empieza sin ancho.
                fila.Tachon = _x.Caja(rt, "Tachon", EditorIzq + 112, 0, 0, 2, Tema.danger);
                fila.Tachon.rectTransform.pivot = new Vector2(0, 0.5f);
                fila.Tachon.rectTransform.anchoredPosition = new Vector2(EditorIzq + 112, 0);
            }
            return fila;
        }

        // ==================================================================== el pull request

        private void PanelDelPr(string slug) {
            var alto = AltoIde - AltoTitulo - AltoEstado;
            var cx = AnchoIde / 2 - AnchoPr / 2;
            var arriba = AltoIde / 2 - AltoTitulo;
            _x.Caja(_ventana, "Panel", cx, arriba - alto / 2, AnchoPr, alto, Lateral);
            _x.Caja(_ventana, "Raya", AnchoIde / 2 - AnchoPr, arriba - alto / 2, 1, alto, Linea);
            _panelPr = _x.Nodo(_ventana, "Pull request", cx, arriba - alto / 2, AnchoPr, alto);
            var top = alto / 2;

            // El esqueleto mientras «carga», y despues lo de verdad.
            var esqueleto = _x.Nodo(_panelPr, "Esqueleto", 0, 0, AnchoPr, alto);
            for (var i = 0; i < 4; i++)
                _x.Caja(esqueleto, "Barra", -AnchoPr / 2 + 24 + (i == 0 ? 120 : 170), top - 70 - i * 34, i == 0 ? 240 : 340, 14, NexusTheme.Alfa(Tema.inkFaint, 0.25f), 7);
            var gEsqueleto = esqueleto.gameObject.AddComponent<CanvasGroup>();
            var contenido = _x.Nodo(_panelPr, "Contenido", 0, 0, AnchoPr, alto);
            var gContenido = contenido.gameObject.AddComponent<CanvasGroup>();
            gContenido.alpha = 0;
            _sec.En(0.1f, 0.3f, p => gEsqueleto.alpha = 0.55f + 0.45f * Mathf.Cos(p * Mathf.PI * 2));
            _sec.En(0.4f, 0.2f, p => { gEsqueleto.alpha = 1 - p; gContenido.alpha = p; });

            var izq = -AnchoPr / 2 + 20;
            _x.Texto(contenido, "PULL REQUEST", EstiloTexto.Leyenda, Tema.inkMuted, izq + 150, top - 22, 300, 22, TextAlignmentOptions.Left).characterSpacing = 3;
            var abierto = _x.Caja(contenido, "Abierto", AnchoPr / 2 - 60, top - 22, 84, 22, NexusTheme.Alfa(Tema.success, 0.2f), 11);
            _x.Texto(abierto.transform, "ABIERTO", EstiloTexto.Leyenda, Tema.success, 0, 0, 84, 22).fontSize *= 0.85f;

            // Quien lo escribio.
            var avatar = _x.Circulo(contenido, "Avatar", izq + 18, top - 62, 36, NexusTheme.Alfa(TrabajasSolo ? Tema.cyan : Tema.violet, 0.85f));
            _x.Texto(avatar.transform, TrabajasSolo ? "T" : "E", EstiloTexto.Encabezado, Tema.bg950, 0, 0, 36, 36);
            _x.Texto(contenido, TrabajasSolo ? "tú" : "alguien del equipo", EstiloTexto.Pequeno, Tema.ink, izq + 160, top - 54, 260, 22, TextAlignmentOptions.Left);
            _x.Texto(contenido, "quiere fusionar feature/" + slug, EstiloTexto.Leyenda, Tema.inkFaint, izq + 160, top - 74, 260, 20, TextAlignmentOptions.Left)
              .overflowMode = TextOverflowModes.Ellipsis;

            var titulo = _x.Texto(contenido, Pr.Titulo, EstiloTexto.Encabezado, Tema.ink, 0, top - 118, AnchoPr - 40, 52, TextAlignmentOptions.TopLeft);
            titulo.enableAutoSizing = true;
            titulo.fontSizeMin = 14;
            titulo.fontSizeMax = titulo.fontSize;

            // Lo que dice, como un comentario.
            if (!string.IsNullOrEmpty(Pr.Descripcion)) {
                var burbuja = _x.Caja(contenido, "Descripcion", 0, top - 192, AnchoPr - 40, 78, new Color32(0x23, 0x26, 0x2D, 0xFF), 8);
                Ui.Borde(burbuja.rectTransform, Linea, 8, 1);
                var d = _x.Texto(burbuja.transform, "«" + Pr.Descripcion + "»", EstiloTexto.Pequeno, Tema.ink, 0, 0, AnchoPr - 64, 70, TextAlignmentOptions.Left);
                d.enableAutoSizing = true;
                d.fontSizeMin = 11;
                d.fontSizeMax = d.fontSize;
            }

            // +N -M
            int mas, menos;
            NumeracionDeDiff.Contar(Pr.Lineas, out mas, out menos);
            var y = top - 252;
            _x.Texto(contenido, "1 archivo", EstiloTexto.Pequeno, Tema.inkMuted, izq + 45, y, 90, 24, TextAlignmentOptions.Left);
            var cMas = _x.Caja(contenido, "Mas", izq + 130, y, 56, 24, NexusTheme.Alfa(Tema.success, 0.2f), 6);
            _x.Texto(cMas.transform, "+" + mas, EstiloTexto.Mono, Tema.success, 0, 0, 56, 24);
            var cMenos = _x.Caja(contenido, "Menos", izq + 194, y, 56, 24, NexusTheme.Alfa(Tema.danger, 0.2f), 6);
            _x.Texto(cMenos.transform, "-" + menos, EstiloTexto.Mono, Tema.danger, 0, 0, 56, 24);
            _x.Texto(contenido, "revisarlo: " + Nexus.Core.Sesion.GameSession.MinutosDeRevisarPr + " min", EstiloTexto.Leyenda, Tema.inkFaint, izq + 300, y, 160, 24, TextAlignmentOptions.Right);
        }

        private void BarraDeEstado(string slug, int lineas) {
            var y = -AltoIde / 2 + AltoEstado / 2;
            _estado = _x.Caja(_ventana, "Estado", 0, y, AnchoIde, AltoEstado, Tema.cyan);
            _textoEstadoIzq = _x.Texto(_ventana, "feature/" + slug + "   revisión pendiente", EstiloTexto.Mono, Tema.onCyan, -AnchoIde / 2 + 320, y, 600, AltoEstado, TextAlignmentOptions.Left);
            _textoEstadoIzq.fontSize *= 0.8f;
            _textoEstadoDer = _x.Texto(_ventana, $"Ln {Math.Max(1, lineas)}, Col 1    UTF-8    {NombreDelLenguaje(_lenguaje)}", EstiloTexto.Mono, Tema.onCyan,
                                       AnchoIde / 2 - 320, y, 600, AltoEstado, TextAlignmentOptions.Right);
            _textoEstadoDer.fontSize *= 0.8f;
        }

        /// <summary>
        /// La capa de la decision, DENTRO del IDE y por delante del editor (2026-11-07): sube desde abajo cuando el codigo
        /// ya esta, con las dos cartas, lo que pasa si se cierra sin decidir y, al decidir, «Seguir».
        /// </summary>
        private void CapaDeDecision(float finDelCodigo) {
            var ancho = EditorAncho - 40;
            var cx = EditorIzq + EditorAncho / 2;
            var y = -AltoIde / 2 + AltoEstado + 16 + AltoCapa / 2;
            // Una sombra que oscurece el editor por debajo de la capa: la capa va delante.
            var sombra = _x.Caja(_ventana, "Sombra", cx, y + 30, EditorAncho, AltoCapa + 100, new Color(0, 0, 0, 0));
            sombra.sprite = PlanoIlustrado.Brillo();
            var capa = _x.Caja(_ventana, "Capa de decision", cx, y, ancho, AltoCapa, NexusTheme.Alfa(new Color32(0x12, 0x14, 0x18, 0xFF), 0.97f), 14);
            capa.raycastTarget = true;
            Ui.Borde(capa.rectTransform, NexusTheme.Alfa(Tema.cyan, 0.6f), 14, 1.5f);
            Ui.Halo(capa.rectTransform, NexusTheme.Alfa(Tema.cyan, 0.25f), 14);
            _capa = capa.rectTransform;
            _grupoCapa = capa.gameObject.AddComponent<CanvasGroup>();
            _grupoCapa.alpha = 0;
            var t = _capa.transform;
            _x.Texto(t, "TU REVISIÓN", EstiloTexto.Leyenda, Tema.cyan, -ancho / 2 + 120, AltoCapa / 2 - 24, 200, 22, TextAlignmentOptions.Left).characterSpacing = 3;
            var cerrar = Ui.Boton(t, "Cerrar sin decidir (Esc)", Cerrar, VarianteBoton.Fantasma);
            var rc = (RectTransform)cerrar.transform;
            UiKit.Tamano(cerrar).ignoreLayout = true;
            rc.anchorMin = rc.anchorMax = rc.pivot = new Vector2(0.5f, 0.5f);
            rc.sizeDelta = new Vector2(280, 40);
            rc.anchoredPosition = new Vector2(ancho / 2 - 160, AltoCapa / 2 - 26);
            _cerrarSinDecidir = cerrar.gameObject;
            _aviso = _x.Texto(t, TrabajasSolo ? "Si lo cierras, nadie más lo revisa: si tiene un fallo, entra tal cual."
                                              : "Si lo cierras, el equipo se lo revisa entre ellos.",
                              EstiloTexto.Pequeno, Tema.inkFaint, 0, -AltoCapa / 2 + 20, ancho - 40, 24);
            _sitioMano = _x.Nodo(t, "Cartas", 0, -10, ancho - 40, AltoCapa - 90);

            // Sube desde abajo (ease-out, 250 ms) cuando el codigo ya esta; entonces se reparten las cartas.
            var yFinal = _capa.anchoredPosition.y;
            _sec.En(finDelCodigo, 0.25f, p => {
                _grupoCapa.alpha = p;
                _capa.anchoredPosition = new Vector2(cx, Mathf.Lerp(yFinal - 60, yFinal, ContextoDeHallazgo.Suave(p)));
                sombra.color = new Color(0, 0, 0, 0.55f * p);
            });
            _sec.En(finDelCodigo + 0.2f, 0.01f, p => { if (p >= 1 && _mano == null) CrearMano(); });
        }

        // ==================================================================== decidir

        private void CrearMano() {
            var minutos = Nexus.Core.Sesion.GameSession.MinutosDeRevisarPr + " min";
            var opciones = new List<OpcionDeCarta> {
                new OpcionDeCarta { Titulo = "Aprobar", Detalle = "Está bien: se fusiona", Pie = minutos, AlElegir = () => AlDecidir(VeredictosDePr.Aprobar) },
                new OpcionDeCarta { Titulo = "Pedir cambios", Detalle = "Algo falla: vuelve a quien lo escribió", Pie = minutos, AlElegir = () => AlDecidir(VeredictosDePr.PedirCambios) }
            };
            _mano = ManoDeCartas.Crear(Ui, _sitioMano, opciones, new ManoDeCartas.Ajustes { Sello = "REVISADO", AnchoDeCarta = 300, AnchoMaximo = Mathf.Min(760, _sitioMano.rect.width) });
        }

        private void AlDecidir(string veredicto) {
            if (_decidido) return;
            _decidido = true;
            var r = Decidir == null ? null : Decidir(veredicto);
            if (_cerrarSinDecidir != null) _cerrarSinDecidir.SetActive(false);
            if (_aviso != null) _aviso.text = "Enter para seguir.";
            _despues = _ventana.gameObject.AddComponent<Secuencia>();
            var aprobado = veredicto == VeredictosDePr.Aprobar;

            if (aprobado) {
                // Lo quitado se tacha y se cierra; lo de debajo sube; lo añadido pierde sus marcas.
                var alto = _filas.Count > 0 ? _filas[0].Rt.sizeDelta.y : 28;
                var desplazamiento = 0f;
                foreach (var f in _filas) {
                    var fila = f;
                    if (fila.Tipo == "del") {
                        desplazamiento += alto;
                        _despues.En(0.1f, 0.25f, p => { if (fila.Tachon != null) fila.Tachon.rectTransform.sizeDelta = new Vector2((EditorAncho - 140) * ContextoDeHallazgo.Suave(p), 2); });
                        _despues.En(0.4f, 0.2f, p => fila.Grupo.alpha = 1 - p);
                        continue;
                    }
                    var desde = fila.Y;
                    var hasta = fila.Y + desplazamiento;
                    _despues.En(0.55f, 0.25f, p => {
                        var e = p < 0.5f ? 4 * p * p * p : 1 - Mathf.Pow(-2 * p + 2, 3) / 2;
                        fila.Rt.anchoredPosition = new Vector2(0, Mathf.Lerp(desde, hasta, e));
                    });
                    if (fila.Tipo == "add")
                        _despues.En(0.55f, 0.3f, p => {
                            fila.Fondo.color = NexusTheme.Alfa(Tema.success, 0.13f * (1 - p));
                            fila.Marca.alpha = 1 - p;
                        });
                }
            }
            var colorEstado = aprobado ? Tema.success : Tema.warning;
            _despues.En(0.3f, 0.25f, p => _estado.color = Color.Lerp(Tema.cyan, colorEstado, p));
            _textoEstadoIzq.text = aprobado ? "main   fusionado: el cambio entra" : "feature/" + Slug(Pr.Titulo) + "   cambios pedidos";
            _textoEstadoIzq.color = Tema.bg950;
            _textoEstadoDer.color = Tema.bg950;

            if (r != null) Resultado(r, aprobado);
            // Seguir, cuando la carta ya se ha sellado.
            var seguir = Ui.Boton(_capa, "Seguir (Enter)", Cerrar, VarianteBoton.Primario);
            var rs = (RectTransform)seguir.transform;
            UiKit.Tamano(seguir).ignoreLayout = true;
            rs.anchorMin = rs.anchorMax = rs.pivot = new Vector2(1, 1);
            rs.anchoredPosition = new Vector2(-20, -12);
            rs.sizeDelta = new Vector2(240, 46);
            var gSeguir = seguir.gameObject.AddComponent<CanvasGroup>();
            _despues.En(0.9f, 0.2f, p => gSeguir.alpha = p);
        }

        /// <summary>El resultado de la revision, como un comentario que llega al pull request: marca, titulo, por que y que cambio.</summary>
        private void Resultado(ResultadoDeRevision r, bool aprobado) {
            var color = r.Acierto ? Tema.success : Tema.warning;
            var alto = AltoIde - AltoTitulo - AltoEstado;
            var tarjeta = _x.Caja(_panelPr, "Resultado", 0, -alto / 2 + 150, AnchoPr - 32, 260, new Color32(0x20, 0x23, 0x29, 0xFF), 10);
            Ui.Borde(tarjeta.rectTransform, color, 10, 1.5f);
            var t = tarjeta.transform;
            DibujoUI dibujo;
            var marca = _x.Lamina(t, -(AnchoPr - 32) / 2 + 30, 104, 32, 32, out dibujo);
            if (r.Acierto) marca.Si(16, 18, color, 10);
            else marca.No(16, 16, color, 9);
            _x.Texto(t, aprobado ? "APROBASTE" : "PEDISTE CAMBIOS", EstiloTexto.Leyenda, Tema.inkMuted, 40, 112, AnchoPr - 140, 20, TextAlignmentOptions.Left).characterSpacing = 3;
            var titulo = _x.Texto(t, r.Titulo ?? "", EstiloTexto.Encabezado, color, 40, 88, AnchoPr - 140, 30, TextAlignmentOptions.Left);
            titulo.enableAutoSizing = true;
            titulo.fontSizeMin = 13;
            titulo.fontSizeMax = titulo.fontSize;
            var porque = _x.Texto(t, r.Explicacion ?? "", EstiloTexto.Pequeno, Tema.ink, 0, 4, AnchoPr - 64, 130, TextAlignmentOptions.TopLeft);
            porque.enableAutoSizing = true;
            porque.fontSizeMin = 10;
            porque.fontSizeMax = porque.fontSize;
            if (r.Cambios != null && r.Cambios.Count > 0) {
                var cambios = _x.Texto(t, Textos.Previsualizar(r.Cambios), EstiloTexto.Pequeno, Tema.warning, 0, -104, AnchoPr - 64, 40, TextAlignmentOptions.Left);
                cambios.enableAutoSizing = true;
                cambios.fontSizeMin = 10;
                cambios.fontSizeMax = cambios.fontSize;
            }
            var g = tarjeta.gameObject.AddComponent<CanvasGroup>();
            var rt = tarjeta.rectTransform;
            var y = rt.anchoredPosition.y;
            _despues.En(0.7f, 0.25f, p => {
                g.alpha = p;
                rt.anchoredPosition = new Vector2(Mathf.Lerp(24, 0, ContextoDeHallazgo.Suave(p)), y);
            });
            _despues.En(0.85f, 0.35f, p => dibujo.Avance = p);
        }

        // ==================================================================== entrada y salida

        public void OnPointerClick(PointerEventData e) {
            if (_sec != null && !_sec.Terminada) _sec.Saltar();
            else if (_despues != null && !_despues.Terminada) _despues.Saltar();
        }

        private void Update() {
            var teclado = UnityEngine.InputSystem.Keyboard.current;
            if (teclado == null || !_decidido || _despues == null || !_despues.Terminada) return;
            if (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame) _cerrarAlFinal = true;
        }

        private void LateUpdate() {
            if (!_cerrarAlFinal) return;
            _cerrarAlFinal = false;
            Cerrar();
        }

        private void Cerrar() {
            if (_cerrado) return;
            App.Router.Volver();
        }

        /// <summary>Al quitarse de encima (Cerrar, Seguir o Escape): quien la abrio suelta el reloj.</summary>
        public override void AlOcultar() {
            if (_cerrado) return;
            _cerrado = true;
            AlCerrar?.Invoke();
        }

        // ==================================================================== nombres

        /// <summary>El titulo en minusculas, sin acentos ni palabras vacias, con guiones: «Ordenar las reservas por hora» -&gt; ordenar-reservas-hora.</summary>
        public static string Slug(string titulo) {
            var vacias = new HashSet<string> { "el", "la", "los", "las", "de", "del", "por", "para", "un", "una", "y", "a", "en", "al", "con" };
            var sb = new StringBuilder();
            foreach (var palabra in (titulo ?? "cambio").ToLowerInvariant().Split(new[] { ' ', ',', '.', ':' }, StringSplitOptions.RemoveEmptyEntries)) {
                if (vacias.Contains(palabra)) continue;
                var limpia = new StringBuilder();
                foreach (var ch in palabra.Normalize(NormalizationForm.FormD))
                    if (char.IsLetterOrDigit(ch) && ch < 128) limpia.Append(ch);
                if (limpia.Length == 0) continue;
                if (sb.Length > 0) sb.Append('-');
                sb.Append(limpia);
                if (sb.Length > 28) break;
            }
            return sb.Length == 0 ? "cambio" : sb.ToString();
        }

        private static string NombreDelLenguaje(string lenguaje) {
            switch (lenguaje) {
                case "python": return "Python";
                case "csharp": return "C#";
                case "sql": return "SQL";
                default: return "JavaScript";
            }
        }
    }
}
