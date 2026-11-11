using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Corregir;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Pantallas.Minijuegos.Corregir;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>
    /// V1 · corregir el diagrama con cartas (2026-11-09, sustituye al «pincha y etiqueta» en los retos de diagrama).
    ///
    ///   · el diagrama entero, con los puntos que hay que revisar marcados (#1, #2…): unos son problemas de verdad y
    ///     otros parecen raros y estan bien;
    ///   · se elige un punto y se le juega una carta de solucion (un patron o una practica) desde el abanico de abajo,
    ///     o «Está bien así». Una carta jugada no se puede jugar en otro punto hasta quitarla;
    ///   · «Verificar» revela punto por punto (√ o ×) y cierra el reto. Un solo intento, como antes.
    ///
    /// Ronda 3 (mismo dia), estructura: NADA ocupa layout salvo el lienzo.
    ///   · el diagrama es un LIENZO que ocupa toda la pantalla bajo la cabecera: rueda para acercar, arrastrar para
    ///     moverse, con limites invisibles (LienzoNavegable). Se dibuja a escala 1:1 en el hueco medido (HuecoMedido);
    ///   · todo lo demas flota encima como globos de cristal: el dialogo y el contador arriba a la izquierda, pista y
    ///     Verificar arriba a la derecha, el abanico de cartas abajo, el zoom a un lado;
    ///   · el tutorial son globos con un aro sobre la pieza (GloboDeTutorial), sin pantalla aparte;
    ///   · el cierre es el mismo lienzo con un globo junto al punto que se mira (GloboDeRevision).
    ///
    /// La logica vive en Core (PuntosDelDiagrama, CorregirState, CorregirEvaluador, RevisionDePunto); aqui solo se dibuja
    /// y se anima.
    /// </summary>
    public sealed class PantallaCorregirDiagrama : PantallaDeMinijuego {
        private const int SegundosPorPista = 15;
        // Reparto del diagrama sobre el lienzo. Los margenes dejan libres los globos de arriba y el abanico de abajo
        // cuando el lienzo esta entero (zoom 1); al acercar, se puede mover para ver lo que tapan.
        private const float MargenX = 70, MargenSup = 210, MargenInf = 270, HuecoEntreFilas = 70, HuecoEntreColumnas = 100;
        private const float AnchoMaxCaja = 330, AltoMaxCaja = 118, AltoMinCaja = 70;
        private const float AltoDeLosGlobosDeArriba = 150, AltoDeLaTira = 110;

        protected override bool EstiloCristal { get { return true; } }

        private CorregirState _st;
        private List<PuntoDelDiagrama> _puntos;
        /// <summary>Para el capturador del editor: los puntos en su orden.</summary>
        internal IReadOnlyList<PuntoDelDiagrama> Puntos { get { return _puntos; } }
        private readonly Dictionary<string, MarcadorDePunto> _marcadores = new Dictionary<string, MarcadorDePunto>();
        private readonly Dictionary<string, Vector2> _anclas = new Dictionary<string, Vector2>();
        private readonly Dictionary<string, Rect> _cajas = new Dictionary<string, Rect>();
        private readonly Dictionary<string, Rect> _rectsDePieza = new Dictionary<string, Rect>();
        private readonly Dictionary<string, Vector2[]> _flechas = new Dictionary<string, Vector2[]>();
        private float _bw = 300, _bh = 100;
        private Animador _anim;
        private Lamina _lamina;
        private RectTransform _visor, _capa, _zoom;
        private LienzoNavegable _navegable;
        private DibujoUI _resalte;
        private RectTransform _vuelos, _rafaga, _panelVerificar;
        private AbanicoDeSoluciones _abanico;
        private GloboDeTutorial _tutorial;
        private TMP_Text _globo, _contador;
        private Button _verificar, _quitar, _botonDePista;
        private AndamiajeCfg _andamiaje;
        private bool _bloqueado, _verificando;
        private int _pistasPedidas;
        private string _dudaAvisada;
        private float _latidoVerificar;

        // ---- el cierre
        private GloboDeRevision _globoRev;
        private PuntoDelDiagrama _revisado;
        private RectTransform _chips;

        // ==================================================================== construir

        protected override void ConstruirJuego(RectTransform cuerpo) {
            // Tambien al reintentar una practica: todo de nuevo en blanco.
            _marcadores.Clear(); _anclas.Clear(); _cajas.Clear(); _rectsDePieza.Clear(); _flechas.Clear();
            if (_vuelos != null) { Destroy(_vuelos.gameObject); _vuelos = null; }
            _pistasPedidas = 0; _dudaAvisada = null; _verificando = false; _latidoVerificar = 0; _lamina = null; _tutorial = null;
            TutorialAbierto = false;
            _st = new CorregirState(Def.Cartas.Select(c => c.Id));
            _puntos = PuntosDelDiagrama.De(Def);
            AndamiajeCfg cfg;
            _andamiaje = Pendiente != null && Def.Andamiaje != null && Def.Andamiaje.TryGetValue(Pendiente.NivelAndamiaje.ToString(), out cfg) ? cfg : null;
            AsegurarAnimador();
            _bloqueado = true;   // hasta que termine la entrada

            // El lienzo ocupa TODO el cuerpo; el diagrama se dibuja cuando el hueco ya tiene su tamaño.
            var area = Ui.Nodo(cuerpo, "Lienzo");
            UiKit.Tamano(area, flexAncho: 1, flexAlto: 1);
            ConstruirLienzo(area, true, PincharPieza, lamina => { _lamina = lamina; Entrada(); });
            GuiaView.Registrar("mj.corregir.puntos", area);

            // ---- los globos que flotan encima
            ConstruirGlobosDeArriba(_capa);
            ConstruirCartas(_capa);
            ConstruirZoom(_capa);

            // La capa por la que vuelan las cartas, encima de todo y sin bloquear clics.
            _vuelos = Ui.Nodo(Raiz, "Vuelos");
            UiKit.Rellenar(_vuelos);
            _vuelos.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var grupo = _vuelos.gameObject.AddComponent<CanvasGroup>();
            grupo.blocksRaycasts = false;
            grupo.interactable = false;
            _vuelos.SetAsLastSibling();
            _rafaga = Ui.Nodo(_vuelos, "Plantilla de rafaga");
            var img = _rafaga.gameObject.AddComponent<Image>();
            img.sprite = PlanoIlustrado.Brillo();
            img.color = new Color(1, 1, 1, 0);
            img.raycastTarget = false;
        }

        private void AsegurarAnimador() {
            if (_anim != null) return;
            _anim = Raiz.GetComponent<Animador>();
            if (_anim == null) _anim = Raiz.gameObject.AddComponent<Animador>();
        }

        // ---- pequeñas ayudas de layout para los globos (nada de esto entra en un layout: se ancla y ya)

        private static void Anclar(RectTransform rt, Vector2 ancla, Vector2 pos) {
            rt.anchorMin = rt.anchorMax = rt.pivot = ancla;
            rt.anchoredPosition = pos;
        }

        private static void CrecerConElContenido(RectTransform rt, bool aLoAncho) {
            var f = rt.gameObject.AddComponent<ContentSizeFitter>();
            f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            f.horizontalFit = aLoAncho ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
        }

        /// <summary>Un globo de cristal: vidrio oscuro con un filo de luz, como los de la Fase 1.</summary>
        private RectTransform Globo(Transform padre, string nombre, bool fila, Color? filo = null, float? relleno = null) {
            var rt = fila ? Ui.Fila(padre, nombre, Tema.espacio * 0.75f, relleno ?? Tema.espacio, TextAnchor.MiddleLeft)
                          : Ui.Columna(padre, nombre, Tema.espacio * 0.75f, relleno ?? Tema.espacio);
            var fondo = rt.gameObject.AddComponent<Image>();
            Ui.Fondo(fondo, NexusTheme.Alfa(Tema.bg950, 0.86f), NexusTheme.RadioLg);
            fondo.raycastTarget = true;   // el globo no deja pasar el arrastre: no mueve el lienzo por debajo
            Ui.Halo(rt, NexusTheme.Alfa(Tema.cyan, 0.16f), NexusTheme.RadioLg);
            Ui.Borde(rt, filo ?? NexusTheme.Alfa(Tema.cyan, 0.5f), NexusTheme.RadioLg, 1.5f);
            return rt;
        }

        /// <summary>Como en la Fase 1: una linea pequeña en cian y el titulo grande debajo.</summary>
        private RectTransform Titular(Transform padre, string antetitulo, string titulo, Color color) {
            var t = Ui.Columna(padre, "Titular", 2);
            Ui.Texto(t, antetitulo, EstiloTexto.Leyenda, Tema.cyan);
            var tt = Ui.Texto(t, titulo, EstiloTexto.Titulo, color);
            tt.textWrappingMode = TextWrappingModes.NoWrap;
            tt.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        // ==================================================================== el lienzo

        /// <summary>
        /// El lienzo del diagrama: un cristal que ocupa todo 'area', con el visor (que se mueve y se acerca) y, encima, la
        /// capa donde flotan los globos. El diagrama se dibuja cuando HuecoMedido sabe lo que mide el visor, a escala 1:1.
        /// 'animado': las lineas se trazan y las cajas aparecen escalonadas (la entrada); en el cierre, todo de golpe.
        /// </summary>
        private void ConstruirLienzo(RectTransform area, bool animado, Action<string> alPinchar, Action<Lamina> listo) {
            var fondo = area.gameObject.AddComponent<Image>();
            Ui.Fondo(fondo, NexusTheme.Alfa(Tema.bg950, 0.55f), NexusTheme.RadioLg);
            fondo.raycastTarget = false;
            Ui.Borde(area, NexusTheme.Alfa(Tema.cyan, 0.5f), NexusTheme.RadioLg, 1.5f);

            _visor = Ui.Nodo(area, "Visor");
            UiKit.Rellenar(_visor);
            _visor.gameObject.AddComponent<RectMask2D>();
            var tocable = _visor.gameObject.AddComponent<Image>();
            tocable.color = Color.clear;   // sin esto, el arrastre y la rueda no llegan al lienzo
            tocable.raycastTarget = true;

            _capa = Ui.Nodo(area, "Globos");
            UiKit.Rellenar(_capa);

            var visor = _visor;
            var medido = visor.gameObject.AddComponent<HuecoMedido>();
            medido.AlMedirse = (w, h) => {
                _marcadores.Clear(); _anclas.Clear(); _cajas.Clear(); _rectsDePieza.Clear(); _flechas.Clear();
                var lamina = new Lamina(Ui, visor, w, h);
                var r = lamina.Raiz;
                r.GetComponent<LayoutElement>().ignoreLayout = true;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = new Vector2(w, h);
                r.gameObject.AddComponent<ListaDeElementos>();   // sus zonas y marcadores son contenido, no botones de la pantalla
                _navegable = LienzoNavegable.Crear(visor, r, new Vector2(w, h));

                var capa = Ui.Nodo(r, "Resalte");   // el punto elegido, entre el dibujo y los textos
                UiKit.Rellenar(capa);
                capa.gameObject.AddComponent<CanvasRenderer>();
                var resalte = capa.gameObject.AddComponent<DibujoUI>();
                resalte.raycastTarget = false;
                if (animado) _resalte = resalte;

                var textos = PintarDiagrama(lamina, w, h, alPinchar);
                if (animado) {
                    var dibujo = lamina.Dibujo;
                    dibujo.Avance = 0;
                    _anim.Lanzar(1.1f, t => { if (dibujo != null) dibujo.Avance = Curvas.EntradaSalida(t); }, null, 0.1f, r.gameObject);
                    for (var i = 0; i < textos.Count; i++) {
                        var t0 = textos[i];
                        var g = t0.gameObject.GetComponent<CanvasGroup>();
                        if (g == null) g = t0.gameObject.AddComponent<CanvasGroup>();
                        g.alpha = 0;
                        _anim.Lanzar(0.35f, t => {
                            if (g == null) return;
                            g.alpha = t;
                            t0.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, Curvas.SalidaAtras(t));
                        }, null, 0.3f + i * 0.03f, t0.gameObject);
                    }
                }
                listo?.Invoke(lamina);
            };
        }

        private Color ColorDeCapa(Elemento el) {
            switch (!string.IsNullOrEmpty(el.Capa) ? el.Capa : el.Tipo == "externo" ? "externo" : el.Tipo == "baseDeDatos" ? "datos" : "logica") {
                case "interfaz": return Tema.cyan;
                case "logica": return Tema.warning;
                case "datos": return Tema.success;
                case "externo": return Tema.violet;
                default: return Tema.lineStrong;
            }
        }

        /// <summary>
        /// Cajas oscuras con el filo de su capa y su nombre; flechas cian con su texto. Se reparten por el lienzo (columnas
        /// a lo ancho, filas entre los margenes de arriba y abajo), asi que las piezas y la letra salen lo mas grandes que
        /// caben; con el zoom salen mas. Lo que hace cada pieza no se escribe debajo: lo dice el globo al pincharla.
        /// Devuelve los textos, para animar su entrada.
        /// </summary>
        private List<RectTransform> PintarDiagrama(Lamina l, float W, float H, Action<string> alPinchar) {
            var creados = new List<RectTransform>();
            var els = Def.Artefacto.Elementos;
            if (els.Count == 0) return creados;
            var cols = els.Max(e => e.Columna) + 1;
            var filas = els.Max(e => e.Fila) + 1;
            _bw = Mathf.Clamp((W - 2 * MargenX - (cols - 1) * HuecoEntreColumnas) / cols, 150f, AnchoMaxCaja);
            _bh = Mathf.Clamp((H - MargenSup - MargenInf - (filas - 1) * HuecoEntreFilas) / filas, AltoMinCaja, AltoMaxCaja);
            var gx = (W - cols * _bw) / (cols + 1);
            var paso = filas > 1 ? (H - MargenSup - MargenInf - _bh) / (filas - 1) : 0f;
            var y0 = filas > 1 ? MargenSup : (H - _bh) / 2f;
            var pos = els.ToDictionary(e => e.Id, e => new Vector2(gx + e.Columna * (_bw + gx), y0 + e.Fila * paso));
            var ocupados = els.Select(e => new Rect(pos[e.Id].x - 4, pos[e.Id].y - 4, _bw + 8, _bh + 8)).ToList();
            var piezasConPunto = new HashSet<string>(_puntos.Select(p => p.Pieza));
            var flecha = NexusTheme.Alfa(Tema.cyan, 0.6f);

            var etiquetas = new List<Action>();
            foreach (var c in Def.Artefacto.Conexiones) {
                Vector2 pa, pb;
                if (!pos.TryGetValue(c.Desde ?? "", out pa) || !pos.TryGetValue(c.Hasta ?? "", out pb)) continue;
                var a = pa + new Vector2(_bw / 2, _bh / 2); var b = pb + new Vector2(_bw / 2, _bh / 2);
                var p1 = Corte(a, b); var p2 = Corte(b, a);
                l.Dibujo.Flecha(p1.x, p1.y, p2.x, p2.y, flecha, c.Tipo == "escribe" ? 4.5f : 3f, c.Tipo == "lee");
                var conPunto = piezasConPunto.Contains(c.Id);
                var ancho = l.AnchoDePastilla(c.Texto, 21);
                var extra = conPunto ? 2 * MarcadorDePunto.Radio + 14 : 0;
                var hueco = Hueco(p1, p2, ancho + extra, ocupados);
                var con = c;
                _flechas[con.Id] = new[] { p1, p2 };
                etiquetas.Add(() => {
                    var centro = hueco.x - extra / 2;
                    creados.Add(l.Pastilla(centro, hueco.y, con.Texto, 21, NexusTheme.Alfa(Tema.bg950, 0.92f),
                                           conPunto ? Tema.ink : Tema.inkMuted, conPunto ? NexusTheme.Alfa(Tema.warning, 0.8f) : (Color?)null));
                    _anclas[con.Id] = new Vector2(centro + ancho / 2 + MarcadorDePunto.Radio + 8, hueco.y);
                    _rectsDePieza[con.Id] = new Rect(centro - ancho / 2, hueco.y - 19, ancho, 38);
                    if (alPinchar != null) l.Zona(centro - ancho / 2, hueco.y - 19, ancho, 38, () => alPinchar(con.Id), "Flecha " + con.Id);
                });
            }
            foreach (var e in etiquetas) e();

            foreach (var el in els) {
                var p = pos[el.Id];
                var color = ColorDeCapa(el);
                var relleno = NexusTheme.Alfa(Tema.bg900, 0.95f);
                var bd = el.Tipo == "baseDeDatos";
                if (bd) l.Dibujo.Cilindro(p.x, p.y, _bw, _bh, color, 3f, relleno);
                else l.Dibujo.Rect(p.x, p.y, _bw, _bh, color, 3f, relleno, el.Tipo == "externo", 12);
                var texto = l.Texto(p.x + 14, p.y + (bd ? 12 : 0), _bw - 28, _bh - (bd ? 12 : 0), el.Texto, 28, Tema.ink,
                                    TextAlignmentOptions.Center, null, true);
                texto.enableAutoSizing = true; texto.fontSizeMin = 18; texto.fontSizeMax = 28;
                texto.overflowMode = TextOverflowModes.Ellipsis;
                texto.raycastTarget = false;
                creados.Add((RectTransform)texto.transform);
                _cajas[el.Id] = new Rect(p.x, p.y, _bw, _bh);
                _rectsDePieza[el.Id] = _cajas[el.Id];
                _anclas[el.Id] = new Vector2(p.x + _bw - 20, p.y - 10);   // arriba a la derecha, montado sobre el filo
                var id = el.Id;
                if (alPinchar != null) l.Zona(p.x, p.y, _bw, _bh, () => alPinchar(id), "Caja " + id);
            }
            return creados;
        }

        private Vector2 Corte(Vector2 desde, Vector2 hacia) {
            var d = hacia - desde;
            var tx = Mathf.Abs(d.x) > 0.01f ? (_bw / 2 + 8) / Mathf.Abs(d.x) : 1e9f;
            var ty = Mathf.Abs(d.y) > 0.01f ? (_bh / 2 + 8) / Mathf.Abs(d.y) : 1e9f;
            return desde + d * Mathf.Min(tx, ty);
        }

        /// <summary>Donde poner la etiqueta de una flecha (y su marcador) sin pisar cajas ni otras etiquetas.</summary>
        private static Vector2 Hueco(Vector2 a, Vector2 b, float ancho, List<Rect> ocupados) {
            var normal = (b - a).sqrMagnitude > 0 ? new Vector2(-(b - a).y, (b - a).x).normalized : Vector2.up;
            foreach (var lado in new[] { 0f, 30f, -30f, 56f, -56f, 80f, -80f })
                foreach (var t in new[] { .5f, .38f, .62f, .28f, .72f, .2f, .8f }) {
                    var p = Vector2.Lerp(a, b, t) + normal * lado;
                    var r = new Rect(p.x - ancho / 2 - 8, p.y - 30, ancho + 16, 60);
                    if (!ocupados.Any(o => o.Overlaps(r))) { ocupados.Add(r); return p; }
                }
            var m = (a + b) / 2;
            ocupados.Add(new Rect(m.x - ancho / 2, m.y - 19, ancho, 38));
            return m;
        }

        /// <summary>De coordenadas de la lamina (origen arriba a la izquierda, y hacia abajo) a las de la capa de globos.</summary>
        private Vector2 ACapa(Vector2 p) {
            var tam = _lamina.Raiz.sizeDelta;
            var mundo = _lamina.Raiz.TransformPoint(new Vector3(p.x - tam.x / 2f, -(p.y - tam.y / 2f), 0));
            return (Vector2)_capa.InverseTransformPoint(mundo);
        }

        private Rect ACapa(Rect r) {
            var a = ACapa(new Vector2(r.xMin, r.yMin));
            var b = ACapa(new Vector2(r.xMax, r.yMax));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        // ==================================================================== los globos de arriba, las cartas y el zoom

        /// <summary>Arriba a la izquierda, quien espera con su dialogo y el contador; arriba a la derecha, pista y Verificar.</summary>
        private void ConstruirGlobosDeArriba(RectTransform capa) {
            var dialogo = Globo(capa, "Dialogo", false);
            Anclar(dialogo, new Vector2(0, 1), new Vector2(16, -14));
            dialogo.sizeDelta = new Vector2(640, 10);
            CrecerConElContenido(dialogo, false);
            var quien = Def.Presentacion.QuienEspera;
            var fila = Ui.Fila(dialogo, "Retrato", Tema.espacio, alineacion: TextAnchor.MiddleLeft);
            if (!string.IsNullOrEmpty(quien) && quien != "Tú mismo")
                Ui.Ilustracion(fila, MaterialesNexus.IdDePersonaje(quien), null, 72, 84);
            var textos = Ui.Columna(fila, "Texto", 2);
            UiKit.Tamano(textos, flexAncho: 1);
            var quienEs = !string.IsNullOrEmpty(quien) && quien != "Tú mismo" ? quien.ToUpperInvariant() : "TUS NOTAS";
            Ui.Texto(textos, quienEs + " · " + (Def.Presentacion.Titulo ?? Def.Id).ToUpperInvariant(), EstiloTexto.Leyenda, Tema.cyan);
            _globo = Ui.Texto(textos, "", EstiloTexto.Dialogo, Tema.ink);
            var alto = _globo.GetComponent<AltoDeTexto>();
            if (alto != null) Destroy(alto);   // caja fija con letra que se ajusta: que no crezca el globo
            _globo.enableAutoSizing = true; _globo.fontSizeMin = 14; _globo.fontSizeMax = 20;
            UiKit.Tamano(_globo, alto: 78);
            _contador = Ui.Texto(textos, "", EstiloTexto.Pequeno, Tema.inkMuted);

            var acciones = Ui.Fila(capa, "Acciones", Tema.espacio * 0.75f, 0, TextAnchor.MiddleRight);
            Anclar(acciones, new Vector2(1, 1), new Vector2(-16, -14));
            CrecerConElContenido(acciones, true);
            acciones.gameObject.AddComponent<ListaDeElementos>();
            if (!Guiado) {
                _botonDePista = Ui.Boton(acciones, "", PedirPista, VarianteBoton.Secundario);
                PintarBotonDePista();
            }
            _quitar = Ui.Boton(acciones, "Devolver la carta", Quitar, VarianteBoton.Fantasma);
            _quitar.gameObject.SetActive(false);
            _verificar = Ui.Boton(acciones, "Verificar", Verificar, VarianteBoton.Primario);
            UiKit.Tamano(_verificar, alto: 64);
            _panelVerificar = (RectTransform)_verificar.transform;
            GuiaView.Registrar("mj.entregar", _panelVerificar);
        }

        /// <summary>El abanico flota abajo: en reposo asoma por arriba y, al elegir un punto, sube entero.</summary>
        private void ConstruirCartas(RectTransform capa) {
            var sitio = Ui.Nodo(capa, "Sitio de la mano");
            sitio.anchorMin = new Vector2(0, 0);
            sitio.anchorMax = new Vector2(1, 0);
            sitio.pivot = new Vector2(0.5f, 0);
            sitio.anchoredPosition = Vector2.zero;
            sitio.sizeDelta = new Vector2(0, AbanicoDeSoluciones.AltoDeReposo);
            var opciones = Def.Cartas.Select(c => new OpcionDeSolucion { Id = c.Id, Nombre = c.Nombre, Descripcion = c.Descripcion, Icono = c.Icono, Tipo = c.Tipo }).ToList();
            opciones.Add(new OpcionDeSolucion {
                Id = PuntosDelDiagrama.EstaBien, Nombre = "Está bien así", Descripcion = "Aquí no hay nada que arreglar.", EsEstaBien = true
            });
            _abanico = AbanicoDeSoluciones.Crear(Ui, sitio, opciones);
            _abanico.AlPulsar = JugarCarta;
            _abanico.enabled = false;   // reparte cuando el diagrama ya esta dibujado
            GuiaView.Registrar("mj.corregir.cartas", sitio);
        }

        /// <summary>Tres botones pegados al lado derecho: acercar, alejar y verlo entero.</summary>
        private void ConstruirZoom(RectTransform capa) {
            _zoom = Ui.Columna(capa, "Zoom", Tema.espacio * 0.5f, 0, TextAnchor.UpperRight);
            Anclar(_zoom, new Vector2(1, 0.5f), new Vector2(-16, 30));
            CrecerConElContenido(_zoom, true);
            _zoom.gameObject.AddComponent<ListaDeElementos>();
            Ui.Boton(_zoom, "+", () => { if (_navegable != null) _navegable.Acercar(LienzoNavegable.PasoDeBoton); }, VarianteBoton.Secundario);
            Ui.Boton(_zoom, "−", () => { if (_navegable != null) _navegable.Acercar(1f / LienzoNavegable.PasoDeBoton); }, VarianteBoton.Secundario);
            Ui.Boton(_zoom, "Ajustar", () => { if (_navegable != null) _navegable.Ajustar(); }, VarianteBoton.Fantasma);
        }

        // ==================================================================== la entrada y el tutorial

        private void Entrada() {
            Decir(Saludo());
            // Los marcadores caen con rebote, uno tras otro, cuando el dibujo ya esta casi trazado.
            for (var i = 0; i < _puntos.Count; i++) {
                var p = _puntos[i];
                Vector2 centro;
                if (!_anclas.TryGetValue(p.Pieza ?? "", out centro)) continue;
                var id = p.Id;
                var m = MarcadorDePunto.Crear(Ui, _lamina, p.Id, p.Numero, centro, () => ElegirPunto(id));
                _marcadores[p.Id] = m;
                m.Aparecer(_anim, 1.0f + i * 0.14f);
            }
            var fin = 1.0f + _puntos.Count * 0.14f;
            var dueno = _lamina.Raiz.gameObject;
            _anim.Despues(fin, () => { if (_abanico != null) _abanico.enabled = true; }, dueno);   // reparte
            _anim.Despues(fin + 0.5f, () => {
                _bloqueado = false;
                // La ayuda de un compañero: quita una carta que no sirve.
                if (Pendiente != null && Pendiente.TieneAyuda(Nexus.Core.Relaciones.TiposDeAyuda.PistaDetectar)) {
                    var distractor = Distractor();
                    if (distractor != null) {
                        _st.Descartar(distractor);
                        _abanico.Quemar(distractor, _anim);
                        Decir((Pendiente.AyudaDe ?? "Un compañero") + " te echa una mano: quita una carta que aquí no sirve.");
                    }
                }
                Repintar();
                // La primera vez que se ve esta mecanica, el tutorial en globos (despues, tras el «?»).
                // (Con Marisol guiando ya hay un globo que explica cada paso: no se apilan dos.)
                if (TutorialPendiente) { TutorialPendiente = false; if (!Guiado) MostrarTutorial(true); }
            }, dueno);
            Repintar();
        }

        private string Saludo() {
            var n = _puntos.Count;
            var inicio = n == 1 ? "Hay un punto marcado." : $"Hay {n} puntos marcados.";
            return inicio + " Elige uno y juégale la carta que lo arregla, o «Está bien así». Cada carta sirve una vez.";
        }

        /// <summary>El tutorial como globos con un aro sobre la pieza que explican; sin pantalla aparte, sin tocar el layout.</summary>
        protected override void MostrarTutorial(bool primeraVez) {
            if (_capa == null || _anim == null || _abanico == null || (_tutorial != null && _tutorial.Abierto)) return;
            Func<RectTransform> marcador = () => {
                var p = _puntos.FirstOrDefault();
                MarcadorDePunto m;
                return p != null && _marcadores.TryGetValue(p.Id, out m) ? m.Disco : null;
            };
            var pasos = new List<PasoDeTutorial> {
                new PasoDeTutorial { Texto = "Estos círculos con número son los puntos que hay que revisar. Unos son errores de verdad y otros parecen raros, pero están bien.", Objetivo = marcador },
                new PasoDeTutorial { Texto = "Pincha un punto y juégale la carta que lo arregla. Cada carta sirve una sola vez: si la usas aquí, ya no está para otro punto.", Objetivo = () => _abanico.Raiz },
                new PasoDeTutorial { Texto = "«Está bien así» es para los puntos que no hay que tocar. Esa carta no se gasta. Cambiar lo que funciona también cuenta en contra.",
                                     Objetivo = () => { var c = _abanico.Carta(PuntosDelDiagrama.EstaBien); return c != null ? (RectTransform)c.transform : null; } },
                new PasoDeTutorial { Texto = "Arrastra el diagrama para moverte y usa la rueda del ratón (o estos botones) para acercarte.", Objetivo = () => _zoom },
                new PasoDeTutorial { Texto = "Cuando todos los puntos tengan carta, pulsa Verificar. Se revisan uno a uno y el reto termina.", Objetivo = () => _panelVerificar }
            };
            TutorialAbierto = true;
            _tutorial = GloboDeTutorial.Abrir(Ui, _capa, _anim, pasos, () => TutorialAbierto = false);
        }

        // ==================================================================== lo que hace el jugador

        /// <summary>Pinchar una caja o una flecha: si es un punto, se elige; si no, el globo dice que hace.</summary>
        private void PincharPieza(string pieza) {
            if (Resultado != null || _bloqueado || _verificando) return;
            var p = _puntos.FirstOrDefault(x => x.Pieza == pieza);
            if (p != null) { ElegirPunto(p.Id); return; }
            Decir(QueEs(pieza));
        }

        private string QueEs(string pieza) {
            var el = Def.Artefacto.Elementos.FirstOrDefault(e => e.Id == pieza);
            if (el != null) return $"«{el.Texto}»: " + (string.IsNullOrEmpty(el.Detalle) ? "nadie ha escrito qué hace." : el.Detalle);
            var cx = Def.Artefacto.Conexiones.FirstOrDefault(c => c.Id == pieza);
            if (cx == null) return "";
            var de = Def.Artefacto.Elementos.FirstOrDefault(e => e.Id == cx.Desde);
            var a = Def.Artefacto.Elementos.FirstOrDefault(e => e.Id == cx.Hasta);
            return $"De «{de?.Texto}» a «{a?.Texto}»: {cx.Texto}.";
        }

        internal void ElegirPunto(string id) {
            if (Resultado != null || _bloqueado || _verificando) return;
            if (Guiado && !Guia.Permite(AccionGuiada.ElegirPunto, id)) { Guia.Rechazar(); return; }
            _st.PuntoElegido = id;
            var p = _puntos.First(x => x.Id == id);
            var carta = _st.CartaEn(p.Id);
            Decir($"#{p.Numero} · " + QueEs(p.Pieza) +
                  (carta != null ? $" Ahora tiene «{PuntosDelDiagrama.NombreDeCarta(Def, carta)}»." : " ¿Qué carta le pones?"));
            Guia?.Hecho(AccionGuiada.ElegirPunto, id);
            Repintar();
        }

        internal void JugarCarta(string carta) {
            if (Resultado != null || _bloqueado || _verificando) return;
            if (string.IsNullOrEmpty(_st.PuntoElegido)) {
                _abanico.Sacudir(carta);
                Decir("Primero elige un punto del diagrama: los círculos con número.");
                return;
            }
            if (Guiado && !Guia.Permite(AccionGuiada.JugarCarta, carta)) { Guia.Rechazar(); _abanico.Sacudir(carta); return; }
            var punto = _st.PuntoElegido;
            var donde = _st.PuntoDe(carta);
            if (donde != null && donde != punto) {
                _abanico.Sacudir(carta);
                MarcadorDePunto m;
                if (_marcadores.TryGetValue(donde, out m)) m.Menear();
                Decir($"Esa carta ya está en el punto #{NumeroDe(donde)}. Si la quieres aquí, devuélvela primero a la mano.");
                return;
            }
            if (_st.CartaEn(punto) == carta) return;
            // Con andamiaje de tutorial, cambiar algo que estaba bien no pasa en silencio: duda una vez.
            var p = _puntos.First(x => x.Id == punto);
            if (!Guiado && Andamiaje >= 3 && !p.EsReal && carta != PuntosDelDiagrama.EstaBien && _dudaAvisada != punto + "|" + carta) {
                _dudaAvisada = punto + "|" + carta;
                _abanico.Sacudir(carta);
                Decir("Marisol duda: «¿Seguro que aquí hay algo que arreglar?». Pulsa la carta de nuevo para ponerla igual.");
                return;
            }
            _dudaAvisada = null;
            string devuelta;
            if (_st.Jugar(carta, Segundo, out devuelta) != Jugada.Puesta) { _abanico.Sacudir(carta); return; }
            Guia?.Hecho(AccionGuiada.JugarCarta, carta);
            if (devuelta != null) _abanico.Devolver(devuelta, _anim);
            Volar(carta, punto);
        }

        /// <summary>
        /// La carta vuela del abanico al punto por una curva, girando y encogiendose; al llegar cae la chapa con su sello
        /// y un golpe de luz, y la carta vuelve a su sitio en la mano (hundida y con «EN #N» si se gasto).
        /// </summary>
        private void Volar(string carta, string punto) {
            MarcadorDePunto marcador;
            _marcadores.TryGetValue(punto, out marcador);
            var rt = marcador != null ? _abanico.Despegar(carta, _vuelos) : null;
            if (rt == null) { Aterrizar(carta, punto, marcador); return; }
            _bloqueado = true;
            var desde = rt.position;
            var hasta = marcador.Disco.position;
            var control = (desde + hasta) / 2 + Vector3.up * (Vector3.Distance(desde, hasta) * 0.35f);
            var escala0 = rt.localScale.x;
            var giro0 = rt.localEulerAngles.z;
            _anim.Lanzar(0.5f, t => {
                if (rt == null) return;
                var e = Curvas.EntradaSalida(t);
                // El marcador se mueve con el lienzo: se vuelve a leer su sitio en cada fotograma.
                var destino = marcador != null ? marcador.Disco.position : hasta;
                rt.position = Curvas.Bezier(desde, control, destino, e);
                rt.localScale = Vector3.one * Mathf.Lerp(escala0, 0.16f, Curvas.EntradaSalida(Mathf.Clamp01((t - 0.35f) / 0.65f)));
                rt.localEulerAngles = new Vector3(0, 0, giro0 + 360f * e);
            }, () => Aterrizar(carta, punto, marcador), 0, rt.gameObject);
        }

        private void Aterrizar(string carta, string punto, MarcadorDePunto marcador) {
            var esEstaBien = carta == PuntosDelDiagrama.EstaBien;
            _abanico.Aterrizar(carta, esEstaBien ? 0 : NumeroDe(punto));
            if (marcador != null) {
                var c = PuntosDelDiagrama.Carta(Def, carta);
                marcador.PonerCarta(PuntosDelDiagrama.NombreDeCarta(Def, carta), c != null ? c.Icono : null, esEstaBien, _anim, _rafaga);
            }
            _bloqueado = false;
            Decir(esEstaBien ? $"El punto #{NumeroDe(punto)} se queda como está." : $"«{PuntosDelDiagrama.NombreDeCarta(Def, carta)}» puesta en el #{NumeroDe(punto)}.");
            // Sin guia, se pasa solo al siguiente punto sin decidir: menos clics, y se ve lo que falta.
            if (!Guiado) {
                var siguiente = _puntos.FirstOrDefault(x => _st.CartaEn(x.Id) == null);
                if (siguiente != null)
                    _anim.Despues(0.6f, () => { if (!_verificando && Resultado == null && !_bloqueado && _st.CartaEn(siguiente.Id) == null) ElegirPunto(siguiente.Id); }, _lamina.Raiz.gameObject);
                else { _st.PuntoElegido = null; Decir("Todos los puntos tienen su carta. Si lo ves claro, pulsa «Verificar»."); }
            }
            Repintar();
        }

        private void Quitar() {
            if (Resultado != null || _bloqueado || _verificando || string.IsNullOrEmpty(_st.PuntoElegido)) return;
            var punto = _st.PuntoElegido;
            var carta = _st.Quitar(punto);
            if (carta == null) return;
            MarcadorDePunto m;
            if (_marcadores.TryGetValue(punto, out m)) m.QuitarCarta(_anim);
            if (carta != PuntosDelDiagrama.EstaBien) _abanico.Devolver(carta, _anim);
            Decir($"«{PuntosDelDiagrama.NombreDeCarta(Def, carta)}» vuelve a la mano.");
            Repintar();
        }

        private int NumeroDe(string punto) {
            var p = _puntos.FirstOrDefault(x => x.Id == punto);
            return p != null ? p.Numero : 0;
        }

        private string Distractor() {
            var arreglan = new HashSet<string>(Def.Zonas.SelectMany(z => z.Soluciones));
            return Def.Cartas.Select(c => c.Id).FirstOrDefault(c => !arreglan.Contains(c) && !_st.EnUso(c) && !_st.Descartadas.Contains(c));
        }

        // ==================================================================== pistas

        /// <summary>
        /// Primero, la pregunta que hay que hacerse sobre el punto elegido; despues, se quema una carta que no sirve. Las
        /// dos las dice el globo del dialogo (no hay otro texto).
        /// </summary>
        private void PedirPista() {
            if (Resultado != null || _verificando || _bloqueado || _pistasPedidas >= 2) return;
            if (_pistasPedidas == 0) {
                var p = _puntos.FirstOrDefault(x => x.Id == _st.PuntoElegido) ?? _puntos.FirstOrDefault(x => _st.CartaEn(x.Id) == null) ?? _puntos.FirstOrDefault();
                if (p == null) return;
                CobrarSegundos(SegundosPorPista);
                _pistasPedidas++;
                Decir($"Pista · #{p.Numero}: " + (p.EsReal && !string.IsNullOrEmpty(p.Zona.Pista)
                    ? p.Zona.Pista
                    : "¿hace justo lo que pidió el encargo, ni más ni menos? Que parezca raro no quiere decir que esté mal."));
            } else {
                var d = Distractor();
                if (d == null) { Decir("No queda ninguna carta que quitar."); _pistasPedidas = 2; PintarBotonDePista(); return; }
                CobrarSegundos(SegundosPorPista);
                _pistasPedidas++;
                _st.Descartar(d);
                _abanico.Quemar(d, _anim);
                Decir($"Fuera «{PuntosDelDiagrama.NombreDeCarta(Def, d)}»: aquí no arregla nada.");
            }
            PintarBotonDePista();
        }

        private void PintarBotonDePista() {
            if (_botonDePista == null) return;
            _botonDePista.interactable = _pistasPedidas < 2;
            _botonDePista.GetComponentInChildren<TMP_Text>().text =
                _pistasPedidas == 0 ? $"Pista (−{SegundosPorPista} s)"
                : _pistasPedidas == 1 ? $"Quitar una carta (−{SegundosPorPista} s)" : "Sin más pistas";
        }

        // ==================================================================== verificar

        /// <summary>
        /// Revela punto por punto: el marcador gira y enseña √ (con chispas) o × (y el diagrama tiembla); si estaba mal,
        /// la chapa dice lo que tocaba. Despues se entrega y se pinta el cierre.
        /// </summary>
        internal void Verificar() {
            if (Resultado != null || _verificando || _bloqueado) return;
            var faltan = _puntos.Count(p => _st.CartaEn(p.Id) == null);
            if (faltan > 0) {
                Decir(faltan == 1 ? "Te falta un punto por decidir." : $"Te faltan {faltan} puntos por decidir.");
                foreach (var p in _puntos.Where(x => _st.CartaEn(x.Id) == null)) {
                    MarcadorDePunto m;
                    if (_marcadores.TryGetValue(p.Id, out m)) m.Menear();
                }
                return;
            }
            if (Guiado && !Guia.Permite(AccionGuiada.Entregar)) { Guia.Rechazar(); return; }
            _verificando = true;
            _st.PuntoElegido = null;
            if (_navegable != null) _navegable.Ajustar();   // a verlo entero: se revelan todos los puntos
            Repintar();
            Decir("Vamos a ver…");
            const float paso = 0.75f;
            var dueno = _lamina.Raiz.gameObject;
            for (var i = 0; i < _puntos.Count; i++) {
                var p = _puntos[i];
                var carta = _st.CartaEn(p.Id);
                var bien = CorregirEvaluador.Acierta(Def, p, carta);
                var correcta = Correcta(p);
                var cartaCorrecta = PuntosDelDiagrama.Carta(Def, correcta);
                _anim.Despues(0.4f + i * paso, () => {
                    MarcadorDePunto m;
                    if (!_marcadores.TryGetValue(p.Id, out m)) return;
                    m.Revelar(bien, PuntosDelDiagrama.NombreDeCarta(Def, correcta), cartaCorrecta != null ? cartaCorrecta.Icono : null,
                              correcta == PuntosDelDiagrama.EstaBien, _anim, _rafaga);
                    Decir($"#{p.Numero}: " + (bien ? (p.EsReal ? "¡arreglado!" : "bien visto, estaba bien.")
                                                  : (p.EsReal ? "eso no lo arregla." : "ahí no había nada que cambiar.")));
                    if (!bien) Temblar();
                }, dueno);
            }
            _anim.Despues(0.4f + _puntos.Count * paso + 0.8f, () => {
                _verificando = false;
                Entregar();
                // El modo guiado puede no dejar entregar todavia: entonces se sigue jugando.
                if (Resultado == null) Repintar();
            }, dueno);
        }

        private static string Correcta(PuntoDelDiagrama p) {
            return p.EsReal ? p.Zona.Soluciones.FirstOrDefault() : PuntosDelDiagrama.EstaBien;
        }

        private void Temblar() {
            var nav = _navegable;
            if (nav == null) return;
            _anim.Lanzar(0.3f, t => { if (nav != null) nav.Sacudida = new Vector2(Mathf.Sin(t * 50f) * 9f * (1 - t), 0); },
                         () => { if (nav != null) nav.Sacudida = Vector2.zero; }, 0, nav.gameObject);
        }

        // ==================================================================== el dialogo

        /// <summary>El globo escribe la frase letra a letra (con un tope de duracion: nunca hace esperar).</summary>
        private void Decir(string frase) {
            if (_globo == null) return;
            AsegurarAnimador();
            var texto = Nexus.Core.Narrativa.Glifos.Seguro(frase ?? "");
            _globo.text = texto;
            _globo.maxVisibleCharacters = 0;
            var total = texto.Length;
            var globo = _globo;
            _anim.Lanzar(Mathf.Clamp(total * 0.016f, 0.25f, 1.1f), t => {
                if (globo == null || globo.text != texto) return;
                globo.maxVisibleCharacters = Mathf.CeilToInt(total * t);
            }, () => { if (globo != null && globo.text == texto) globo.maxVisibleCharacters = 99999; }, 0, globo.gameObject);
        }

        // ==================================================================== repintar

        public override void Repintar() {
            if (_st == null || Resultado != null || _abanico == null) return;
            foreach (var kv in _marcadores) {
                kv.Value.Elegido = kv.Key == _st.PuntoElegido;
                kv.Value.Pintar();
            }
            _abanico.Ofrecer(!string.IsNullOrEmpty(_st.PuntoElegido) && !_verificando);
            PintarResalte();

            var carta = _st.CartaEn(_st.PuntoElegido);
            _quitar.gameObject.SetActive(carta != null && !Guiado && !_verificando);

            var decididos = _puntos.Count(x => _st.CartaEn(x.Id) != null);
            var contador = $"{decididos} de {_puntos.Count} puntos con carta";
            if (_andamiaje != null && _andamiaje.ContadorRestantes) contador += $" · {Def.Zonas.Count} son problemas";
            _contador.text = contador;
            _verificar.interactable = !_verificando;
            _latidoVerificar = decididos == _puntos.Count ? 1 : 0;
        }

        private void PintarResalte() {
            if (_resalte == null) return;
            _resalte.Limpiar();
            var p = _puntos.FirstOrDefault(x => x.Id == _st.PuntoElegido);
            if (p == null) return;
            Rect caja;
            Vector2[] flecha;
            if (_cajas.TryGetValue(p.Pieza ?? "", out caja)) {
                _resalte.Rect(caja.x - 12, caja.y - 12, caja.width + 24, caja.height + 24, NexusTheme.Alfa(Tema.cyan, 0.25f), 10,
                              NexusTheme.Alfa(Tema.cyan, 0.08f), false, 16);
                _resalte.Rect(caja.x - 6, caja.y - 6, caja.width + 12, caja.height + 12, Tema.cyan, 3, null, false, 12);
            } else if (_flechas.TryGetValue(p.Pieza ?? "", out flecha)) {
                _resalte.Linea(flecha[0].x, flecha[0].y, flecha[1].x, flecha[1].y, NexusTheme.Alfa(Tema.cyan, 0.25f), 16);
                _resalte.Flecha(flecha[0].x, flecha[0].y, flecha[1].x, flecha[1].y, Tema.cyan, 5);
            }
        }

        private void LateUpdate() {
            // «Verificar» late cuando todo tiene carta: es lo siguiente que hay que hacer.
            if (_panelVerificar == null) return;
            var s = _latidoVerificar > 0 && !_verificando && Resultado == null ? 1f + 0.035f * Mathf.Sin(Time.unscaledTime * 6f) : 1f;
            _panelVerificar.localScale = Vector3.one * s;
        }

        // ==================================================================== teclado y guia

        protected override void TeclasDelJuego(UnityEngine.InputSystem.Keyboard teclado) {
            if (_st == null || Resultado != null || TutorialAbierto) return;
            var digitos = new[] { teclado.digit1Key, teclado.digit2Key, teclado.digit3Key, teclado.digit4Key, teclado.digit5Key,
                                  teclado.digit6Key, teclado.digit7Key, teclado.digit8Key, teclado.digit9Key };
            for (var i = 0; i < digitos.Length && i < Def.Cartas.Count; i++)
                if (digitos[i].wasPressedThisFrame) { JugarCarta(Def.Cartas[i].Id); return; }
            if (teclado.digit0Key.wasPressedThisFrame) { JugarCarta(PuntosDelDiagrama.EstaBien); return; }
            if (teclado.tabKey.wasPressedThisFrame || teclado.rightArrowKey.wasPressedThisFrame) { CambiarDePunto(+1); return; }
            if (teclado.leftArrowKey.wasPressedThisFrame) { CambiarDePunto(-1); return; }
            if (teclado.deleteKey.wasPressedThisFrame || teclado.backspaceKey.wasPressedThisFrame) { Quitar(); return; }
            if (teclado.numpadPlusKey.wasPressedThisFrame && _navegable != null) { _navegable.Acercar(LienzoNavegable.PasoDeBoton); return; }
            if (teclado.numpadMinusKey.wasPressedThisFrame && _navegable != null) { _navegable.Acercar(1f / LienzoNavegable.PasoDeBoton); return; }
            if (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame) Verificar();
        }

        private void CambiarDePunto(int paso) {
            if (_puntos.Count == 0) return;
            var i = _puntos.FindIndex(p => p.Id == _st.PuntoElegido);
            i = i < 0 ? (paso > 0 ? 0 : _puntos.Count - 1) : (i + paso + _puntos.Count) % _puntos.Count;
            ElegirPunto(_puntos[i].Id);
        }

        protected override RectTransform PiezaGuiada(PasoGuiado paso) {
            switch (paso.Accion) {
                case AccionGuiada.ElegirPunto: {
                    MarcadorDePunto m;
                    return _marcadores.TryGetValue(paso.Objetivo ?? "", out m) ? m.Disco : null;
                }
                case AccionGuiada.JugarCarta: {
                    var c = _abanico != null ? _abanico.Carta(paso.Objetivo) : null;
                    return c != null ? (RectTransform)c.transform : null;
                }
                case AccionGuiada.Entregar: return _verificar != null ? (RectTransform)_verificar.transform : null;
                default: return null;
            }
        }

        // ==================================================================== evaluar

        protected override ResultadoMinijuego Evaluar() {
            return CorregirEvaluador.Evaluar(Def, _st ?? new CorregirState(Def.Cartas.Select(c => c.Id)));
        }

        protected override IEnumerable<string> SolucionEnTexto() {
            foreach (var p in _puntos ?? PuntosDelDiagrama.De(Def))
                yield return $"#{p.Numero} · {PuntosDelDiagrama.NombreDePieza(Def, p.Pieza)} → «{PuntosDelDiagrama.NombreDeCarta(Def, Correcta(p))}»";
        }

        /// <summary>No se usa (el cierre es propio), pero la base lo exige.</summary>
        protected override void PintarResultado(RectTransform zona) { }

        // ==================================================================== el cierre

        /// <summary>
        /// El cierre: el mismo lienzo (a pantalla completa, con zoom y arrastre) con los marcadores en √ o ×, y encima
        /// globos: el veredicto arriba a la izquierda, un globo junto al punto que se mira (dos mini-cartas y una frase) y
        /// una tira abajo con un chip por punto, «¿Qué aprendiste?» y volver. Empieza en el primer punto que fallo.
        /// </summary>
        protected override bool PintarCierrePropio(RectTransform cuerpo) {
            if (_st == null) _st = new CorregirState(Def.Cartas.Select(c => c.Id));
            if (_puntos == null) _puntos = PuntosDelDiagrama.De(Def);
            AsegurarAnimador();
            if (_vuelos != null) { Destroy(_vuelos.gameObject); _vuelos = null; }
            _resalte = null; _abanico = null; _panelVerificar = null; _globo = null; _lamina = null; _tutorial = null;
            TutorialAbierto = false;
            _revisado = null; _globoRev = null;
            _marcadores.Clear(); _anclas.Clear(); _cajas.Clear(); _rectsDePieza.Clear(); _flechas.Clear();

            var bien = Resultado.Resultado == ResultadosDeMinijuego.Todos;
            var arreglados = _puntos.Count(p => p.EsReal && CorregirEvaluador.Acierta(Def, p, _st.CartaEn(p.Id)));
            var reales = _puntos.Count(p => p.EsReal);
            var deMas = _puntos.Count(p => !p.EsReal && !CorregirEvaluador.Acierta(Def, p, _st.CartaEn(p.Id)));

            var area = Ui.Nodo(cuerpo, "Lienzo");
            UiKit.Tamano(area, flexAncho: 1, flexAlto: 1);
            ConstruirLienzo(area, false, null, DiagramaResuelto);

            // ---- arriba a la izquierda: el veredicto y dos cifras
            var veredicto = Globo(_capa, "Veredicto", false, NexusTheme.Alfa(bien ? Tema.success : Tema.warning, 0.8f));
            Anclar(veredicto, new Vector2(0, 1), new Vector2(16, -14));
            CrecerConElContenido(veredicto, true);
            Titular(veredicto, Guiado ? "RESULTADO · CON LA AYUDA DE MARISOL" : "RESULTADO",
                    TituloDelResultado(Resultado.Resultado, Def.Verbo), bien ? Tema.success : Tema.warning);
            var cifras = Ui.Fila(veredicto, "Cifras", Tema.espacio * 0.75f, alineacion: TextAnchor.MiddleLeft);
            if (reales > 0) Ui.Badge(cifras, $"{arreglados}/{reales} arreglados", arreglados == reales ? Tono.Exito : Tono.Aviso);
            Ui.Badge(cifras, deMas == 0 ? "0 cambios de más" : deMas == 1 ? "1 cambio de más" : $"{deMas} cambios de más", deMas == 0 ? Tono.Exito : Tono.Peligro);
            Entrar(veredicto, 0f);

            // ---- abajo: la tira
            var tira = Globo(_capa, "Tira", true);
            Anclar(tira, new Vector2(0.5f, 0), new Vector2(0, 16));
            CrecerConElContenido(tira, true);
            _chips = Ui.Fila(tira, "Puntos", Tema.espacio * 0.75f, alineacion: TextAnchor.MiddleLeft);
            _chips.gameObject.AddComponent<ListaDeElementos>();   // un chip por punto: contenido, no botones de la pantalla
            Ui.Boton(tira, "¿Qué aprendiste?  ›", AbrirAprendizaje, VarianteBoton.Secundario);
            if (PuedeReintentar) Ui.Boton(tira, "Intentarlo otra vez", ReintentarPractica, VarianteBoton.Secundario);
            var volver = Ui.Boton(tira, "Volver a la jornada", VolverALaJornada, VarianteBoton.Primario);
            UiKit.Tamano(volver, alto: 64);
            PintarChips();
            Entrar(tira, 0.25f);

            // ---- zoom (tambien en el cierre: el diagrama se puede mover y acercar)
            ConstruirZoom(_capa);
            return true;
        }

        /// <summary>Entra con un fundido y una escala de 0,96 a 1.</summary>
        private void Entrar(RectTransform rt, float retraso) {
            var g = rt.gameObject.GetComponent<CanvasGroup>();
            if (g == null) g = rt.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0;
            _anim.Lanzar(0.32f, t => {
                if (g == null) return;
                var e = Curvas.SalidaCubica(t);
                g.alpha = e;
                rt.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, e);
            }, null, retraso, rt.gameObject);
        }

        /// <summary>Los marcadores con su veredicto (√ o ×) y el globo, que se abre en el primer punto que fallo.</summary>
        private void DiagramaResuelto(Lamina lamina) {
            _lamina = lamina;
            _globoRev = GloboDeRevision.Crear(Ui, _capa, _anim, Def, AltoDeLosGlobosDeArriba, AltoDeLaTira);
            for (var i = 0; i < _puntos.Count; i++) {
                var p = _puntos[i];
                Vector2 centro;
                if (!_anclas.TryGetValue(p.Pieza ?? "", out centro)) continue;
                var punto = p;
                var m = MarcadorDePunto.Crear(Ui, lamina, p.Id, p.Numero, centro, () => Revisar(punto, true));
                m.PintarVeredicto(CorregirEvaluador.Acierta(Def, p, _st.CartaEn(p.Id)));
                m.Aparecer(_anim, 0.1f + i * 0.1f);
                _marcadores[p.Id] = m;
            }
            var primero = _puntos.FirstOrDefault(p => !CorregirEvaluador.Acierta(Def, p, _st.CartaEn(p.Id))) ?? _puntos.FirstOrDefault();
            if (primero != null) _anim.Despues(0.5f + _puntos.Count * 0.1f, () => Revisar(primero, true), lamina.Raiz.gameObject);
        }

        /// <summary>Abre el globo de un punto junto a su marcador, y marca ese marcador y su chip.</summary>
        private void Revisar(PuntoDelDiagrama p, bool animar) {
            if (_globoRev == null || _lamina == null) return;
            var pieza = p.Pieza ?? "";
            if (!_anclas.ContainsKey(pieza) || !_rectsDePieza.ContainsKey(pieza)) return;
            _revisado = p;
            foreach (var kv in _marcadores) kv.Value.Destacar(kv.Key == p.Id);
            // Se pide de nuevo en cada fotograma: el lienzo se acerca y se arrastra, y el globo sigue al marcador.
            _globoRev.Mostrar(RevisionDePunto.Para(Def, p, _st.CartaEn(p.Id)), () => ACapa(_anclas[pieza]), () => ACapa(_rectsDePieza[pieza]), animar);
            PintarChips();
        }

        private void PintarChips() {
            if (_chips == null) return;
            UiKit.Vaciar(_chips);
            foreach (var p in _puntos) {
                var punto = p;
                var ok = CorregirEvaluador.Acierta(Def, p, _st.CartaEn(p.Id));
                Ui.Boton(_chips, $"#{p.Numero}  {(ok ? "√" : "×")}", () => Revisar(punto, true),
                         _revisado != null && _revisado.Id == p.Id ? VarianteBoton.Primario : VarianteBoton.Secundario);
            }
        }

        /// <summary>Lo que no cabe en el cierre: la razon, lo que significa para el cliente y, guiado, lo que mirar la proxima vez.</summary>
        private void AbrirAprendizaje() {
            App.Router.Apilar<PantallaDeFicha>(p => {
                p.Titulo = "¿Qué aprendiste?";
                p.Pintar = h => {
                    var razon = Resultado != null && Resultado.Rubrica != null ? Resultado.Rubrica.Razon : null;
                    if (!string.IsNullOrEmpty(razon)) { h.Etiqueta("Lo que importa"); h.Parrafo(razon, Tema.ink); }
                    if (Guiado) {
                        h.Etiqueta("La próxima vez, mira esto");
                        foreach (var leccion in RecorridoGuiado.Lecciones(Guia.Pasos).Take(3)) h.Nota("· " + leccion);
                    }
                    var cliente = QueSignificaParaElCliente();
                    if (cliente != null) { h.Etiqueta("Para el cliente"); h.Nota(cliente); }
                    h.Nota(Practica ? "Era práctica: lo que ganes va al proyecto, pero no cuenta para tu evaluación."
                                    : "Esto queda en tu evaluación. El resumen de todo, en Lecciones al cerrar el nivel.");
                };
            });
        }
    }
}
