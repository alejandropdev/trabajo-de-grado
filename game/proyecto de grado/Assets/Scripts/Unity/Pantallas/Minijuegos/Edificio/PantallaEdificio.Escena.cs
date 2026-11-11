using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Repartir;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Pantallas.Minijuegos.Corregir;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    public sealed partial class PantallaEdificio {
        // ==================================================================== la escena: el edificio del cliente en corte

        private sealed class PisoVista {
            public Deposito Dep;
            public TipoDePrueba Tipo;
            public int Indice;
            public Color Color;
            public float X, Y, W, H;
            public RectTransform Rt;
            public Image Fondo, Borde, Luz;
            public DibujoUI Dib;
            public TMP_Text Horas;
            public Button Menos, Mas;
            /// <summary>Por donde entran los bichos (el borde izquierdo del piso) y por donde salen los que se cuelan (el derecho), en coordenadas de la lamina.</summary>
            public Vector2 Puerto, Salida;
            /// <summary>En el cierre: lo que atrapo y lo que se colo. null mientras se planifica.</summary>
            public RepartirEvaluador.Reparto Resultado;
        }

        private Lamina _lam;
        private RectTransform _visor, _edificio, _bolsa, _capaBichos;
        private Image _vineta, _luzAntena;
        private DibujoUI _clienteDib, _bolsaDib;
        private TMP_Text _bolsaCuenta;
        private FichaDeHoras _ficha;
        private CanvasGroup _monedaGrupo;
        private List<Deposito> _pisos = new List<Deposito>();
        private readonly Dictionary<string, PisoVista> _vistas = new Dictionary<string, PisoVista>();
        private readonly Dictionary<string, RectTransform> _mas = new Dictionary<string, RectTransform>();
        private float _w, _h, _cx, _yBase, _fh, _w0, _wTop;
        private Vector2 _origen, _cliente;
        private float _animo = 1f;
        private int _alertas;

        private Color ColorDe(string clave) {
            switch (clave) {
                case "violet": return Tema.violet;
                case "warning": return Tema.warning;
                case "success": return Tema.success;
                case "ink": return Tema.ink;
                case "naranja": return new Color(1f, 0.62f, 0.3f);
                default: return Tema.cyan;
            }
        }

        /// <summary>
        /// El CanvasGroup de un objeto, creandolo si falta. OJO: nada de «GetComponent() ?? AddComponent()»: en el editor un
        /// GetComponent que no encuentra devuelve un «null falso» que el operador ?? da por bueno, y luego revienta.
        /// </summary>
        private static CanvasGroup Grupo(GameObject go) {
            var g = go.GetComponent<CanvasGroup>();
            if (g == null) g = go.AddComponent<CanvasGroup>();
            return g;
        }

        private DibujoUI CapaDib(RectTransform padre, string nombre) {
            var rt = UiKit.Rellenar(Ui.Nodo(padre, nombre));
            rt.gameObject.AddComponent<CanvasRenderer>();
            var d = rt.gameObject.AddComponent<DibujoUI>();
            d.raycastTarget = false;
            return d;
        }

        /// <summary>
        /// El visor llena todo el cuerpo; la escena se dibuja cuando HuecoMedido sabe lo que mide, a escala 1:1. Encima,
        /// la capa donde flotan los globos. 'cierre': la escena ya resuelta (sin entrada animada).
        /// </summary>
        private void ConstruirEscena(RectTransform area, bool cierre, Action listo) {
            var fondo = area.gameObject.AddComponent<Image>();
            Ui.Fondo(fondo, NexusTheme.Alfa(Tema.bg950, 0.55f), NexusTheme.RadioLg);
            fondo.raycastTarget = false;
            Ui.Borde(area, NexusTheme.Alfa(Tema.cyan, 0.5f), NexusTheme.RadioLg, 1.5f);

            _visor = Ui.Nodo(area, "Visor");
            UiKit.Rellenar(_visor);
            _visor.gameObject.AddComponent<RectMask2D>();
            _capa = Ui.Nodo(area, "Globos");
            UiKit.Rellenar(_capa);

            var visor = _visor;
            var medido = visor.gameObject.AddComponent<HuecoMedido>();
            medido.AlMedirse = (w, h) => {
                if (_lam != null && _lam.Raiz != null) Destroy(_lam.Raiz.gameObject);
                _vistas.Clear(); _mas.Clear();
                _lam = new Lamina(Ui, visor, w, h);
                var r = _lam.Raiz;
                r.GetComponent<LayoutElement>().ignoreLayout = true;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = new Vector2(w, h);
                r.gameObject.AddComponent<ListaDeElementos>();   // sus piezas son contenido, no botones de la pantalla
                PintarEscena(w, h);
                listo?.Invoke();
            };
        }

        private float AnchoDelPiso(int i) {
            var n = _pisos.Count;
            return n <= 1 ? _w0 : Mathf.Lerp(_w0, _wTop, i / (float)(n - 1));
        }

        private void PintarEscena(float w, float h) {
            _w = w; _h = h; _cx = w * 0.5f;
            _pisos = TiposDePrueba.Pisos(Cfg);
            var n = _pisos.Count;
            _yBase = h - 135f;
            _fh = Mathf.Clamp((_yBase - 270f) / Mathf.Max(1, n), 92f, 128f);
            _w0 = Mathf.Clamp(w * 0.5f, 640f, 900f);
            _wTop = n <= 1 ? _w0 : Mathf.Max(600f, _w0 * 0.8f);
            _origen = new Vector2(Mathf.Max(110f, _cx - _w0 / 2f - 230f), _yBase - 40f);
            _cliente = new Vector2(Mathf.Min(w - 150f, _cx + _w0 / 2f + 200f), _yBase - 36f);
            var raiz = _lam.Raiz;

            var fondo = CapaDib(raiz, "Fondo");
            PintarElFondo(fondo);
            MotasFlotantes.Crear(Ui, raiz, w, h, 34, NexusTheme.Alfa(Tema.cyan, 0.9f));
            CrearLaCaja();

            // El edificio: la estructura, la antena y los pisos. Gira un poco sobre su base cuando pesa mas arriba que abajo.
            _edificio = UiKit.Rellenar(Ui.Nodo(raiz, "Edificio"));
            _edificio.pivot = new Vector2(_cx / w, 1f - _yBase / h);
            var estructura = CapaDib(_edificio, "Estructura");
            PintarLaEstructura(estructura);
            for (var i = 0; i < n; i++) _vistas[_pisos[i].Id] = CrearPiso(i, _pisos[i]);
            var luzAntena = Ui.Nodo(_edificio, "Luz de la antena");
            Lamina.Colocar(luzAntena, _cx - _wTop * 0.28f - 9, _yBase - n * _fh - 6 - 44 - 9, 18, 18);
            _luzAntena = luzAntena.gameObject.AddComponent<Image>();
            _luzAntena.sprite = Ui.SpriteCircular();
            _luzAntena.color = Tema.danger;
            _luzAntena.raycastTarget = false;

            _clienteDib = CapaDib(raiz, "Cliente");
            PintarCliente();
            CrearLaBolsa();

            _capaBichos = UiKit.Rellenar(Ui.Nodo(raiz, "Bichos"));
            var vineta = UiKit.Rellenar(Ui.Nodo(raiz, "Vineta"));
            _vineta = vineta.gameObject.AddComponent<Image>();
            _vineta.color = new Color(1f, 0.1f, 0.08f, 0f);
            _vineta.raycastTarget = false;
        }

        // ---------------------------------------------------------------- el fondo: la calle del edificio al atardecer

        private void PintarElFondo(DibujoUI d) {
            const int bandas = 8;
            for (var i = 0; i < bandas; i++) {
                var f = i / (float)(bandas - 1);
                var c = Color.Lerp(new Color(0.035f, 0.065f, 0.11f), new Color(0.15f, 0.13f, 0.19f), f * f);
                var y0 = _yBase * i / bandas; var y1 = _yBase * (i + 1) / bandas + 1f;
                d.Poligono(new List<Vector2> { new Vector2(0, y0), new Vector2(_w, y0), new Vector2(_w, y1), new Vector2(0, y1) }, c);
            }
            // El ultimo resplandor del dia, tras el edificio.
            d.Elipse(_cx, _yBase - 20, _w0 * 0.62f, 150, new Color(0, 0, 0, 0), 0, NexusTheme.Alfa(new Color(1f, 0.62f, 0.38f), 0.07f));
            DibujosDelPuente.Edificios(d, 0, _w, _yBase, 190f, 5, new Color(0.05f, 0.08f, 0.12f), NexusTheme.Alfa(new Color(1f, 0.82f, 0.45f), 0.28f));
            // La calle y lo que hay debajo (la bolsa de horas se apoya ahi).
            d.Poligono(new List<Vector2> { new Vector2(0, _yBase), new Vector2(_w, _yBase), new Vector2(_w, _h), new Vector2(0, _h) }, new Color(0.04f, 0.06f, 0.09f));
            d.Poligono(new List<Vector2> { new Vector2(0, _yBase), new Vector2(_w, _yBase), new Vector2(_w, _yBase + 30), new Vector2(0, _yBase + 30) }, new Color(0.07f, 0.09f, 0.12f));
            d.Linea(0, _yBase, _w, _yBase, NexusTheme.Alfa(Tema.cyan, 0.3f), 2.5f);
            d.Linea(0, _yBase + 15, _w, _yBase + 15, NexusTheme.Alfa(Color.white, 0.22f), 2.5f, true);
        }

        private void PintarLaEstructura(DibujoUI d) {
            var n = _pisos.Count;
            var acero = NexusTheme.Alfa(Tema.ink, 0.38f);
            // El zocalo del edificio.
            d.Rect(_cx - _w0 / 2f - 14, _yBase - 2, _w0 + 28, 16, acero, 2f, new Color(0.08f, 0.1f, 0.13f), false, 4);
            for (var i = 0; i < n; i++) {
                var W = AnchoDelPiso(i);
                var y = _yBase - (i + 1) * _fh;
                // La cornisa de cada piso.
                d.Rect(_cx - W / 2f - 8, y - 2, W + 16, 9, acero, 2f, new Color(0.09f, 0.12f, 0.16f), false, 3);
            }
            DibujosDelEdificio.Antena(d, _cx - _wTop * 0.28f, _yBase - n * _fh - 2, acero, NexusTheme.Alfa(Tema.cyan, 0.85f));
        }

        // ---------------------------------------------------------------- la caja de la version y la gente del cliente

        private void CrearLaCaja() {
            var d = CapaDib(_lam.Raiz, "Caja de la version");
            DibujosDelEdificio.CajaDeVersion(d, _origen.x, _origen.y, Tema.cyan);
            var etiqueta = TextoFijo(_lam.Raiz, "LA VERSIÓN NUEVA", 14, Tema.inkMuted, TextAlignmentOptions.Center, _origen.x - 80, _origen.y + 40, 160, 22);
            etiqueta.characterSpacing = 3;
        }

        private void PintarCliente() {
            _clienteDib.Limpiar();
            var x = _cliente.x - 115; var y = _cliente.y - 62;
            DibujosDelEdificio.GenteDelCliente(_clienteDib, x, y, NexusTheme.Alfa(Tema.ink, 0.9f), new Color(0.27f, 0.5f, 0.82f));
            // La barra de animo del cliente.
            var bx = x + 4; var by = y - 40; const float bw = 210;
            _clienteDib.Rect(bx, by, bw, 14, NexusTheme.Alfa(Tema.ink, 0.5f), 2f, new Color(0, 0, 0, 0.5f), false, 7);
            var fraccion = Mathf.Clamp01(_animo);
            if (fraccion > 0.01f)
                _clienteDib.Rect(bx + 2, by + 2, (bw - 4) * fraccion, 10, new Color(0, 0, 0, 0), 0, Color.Lerp(Tema.danger, Tema.success, fraccion), false, 5);
            // Las alertas de lo que se colo.
            for (var k = 0; k < Mathf.Min(_alertas, 5); k++) DibujosDelEdificio.Alerta(_clienteDib, x + 20 + k * 38, by - 20, 0.85f, Tema.danger);
        }

        // ---------------------------------------------------------------- la bolsa de horas

        private void CrearLaBolsa() {
            var bolsa = Ui.Nodo(_lam.Raiz, "Bolsa de horas");
            Lamina.Colocar(bolsa, _cx - 150, _h - 104, 300, 82);
            _bolsa = bolsa;
            var fondo = bolsa.gameObject.AddComponent<Image>();
            Ui.Fondo(fondo, NexusTheme.Alfa(Tema.bg950, 0.88f), NexusTheme.RadioLg);
            fondo.raycastTarget = true;
            Ui.Halo(bolsa, NexusTheme.Alfa(Tema.warning, 0.14f), NexusTheme.RadioLg);
            Ui.Borde(bolsa, NexusTheme.Alfa(Tema.warning, 0.55f), NexusTheme.RadioLg, 1.5f);
            // La moneda que se arrastra.
            var moneda = Ui.Nodo(bolsa, "Moneda");
            Lamina.Colocar(moneda, 14, 9, 64, 64);
            var imgMoneda = moneda.gameObject.AddComponent<Image>();
            imgMoneda.color = Color.clear;
            imgMoneda.raycastTarget = true;
            _bolsaDib = CapaDib(moneda, "Dibujo");
            DibujosDelEdificio.Moneda(_bolsaDib, 32, 32, 26);
            _monedaGrupo = moneda.gameObject.AddComponent<CanvasGroup>();
            _ficha = moneda.gameObject.AddComponent<FichaDeHoras>();
            _ficha.CrearCopia = CrearCopiaDeFicha;
            _ficha.AlSoltar = SoltarFichaEn;
            _bolsaCuenta = TextoFijo(bolsa, "", 36, Tema.warning, TextAlignmentOptions.MidlineLeft, 92, 6, 200, 46);
            _bolsaCuenta.fontStyle = FontStyles.Bold;
            TextoFijo(bolsa, "horas sin repartir", 14, Tema.inkMuted, TextAlignmentOptions.MidlineLeft, 94, 50, 200, 24);
        }

        // ---------------------------------------------------------------- un piso

        private PisoVista CrearPiso(int i, Deposito d) {
            var tipo = TiposDePrueba.Para(d);
            var pv = new PisoVista { Dep = d, Tipo = tipo, Indice = i, Color = ColorDe(tipo.Color) };
            pv.W = AnchoDelPiso(i); pv.H = _fh - 6f;
            pv.X = _cx - pv.W / 2f; pv.Y = _yBase - (i + 1) * _fh + 3f;
            pv.Puerto = new Vector2(pv.X, pv.Y + pv.H / 2f);
            pv.Salida = new Vector2(pv.X + pv.W, pv.Y + pv.H / 2f);

            var rt = Ui.Nodo(_edificio, "Piso " + d.Id);
            Lamina.Colocar(rt, pv.X, pv.Y, pv.W, pv.H);
            pv.Rt = rt;
            var luz = Ui.Nodo(rt, "Luz");
            luz.anchorMin = luz.anchorMax = luz.pivot = new Vector2(0.5f, 0.5f);
            luz.anchoredPosition = Vector2.zero;
            luz.sizeDelta = new Vector2(pv.W * 1.12f, pv.H * 2.1f);
            pv.Luz = luz.gameObject.AddComponent<Image>();
            pv.Luz.sprite = PlanoIlustrado.Brillo();
            pv.Luz.color = Color.clear;
            pv.Luz.raycastTarget = false;
            var cuerpo = Ui.Nodo(rt, "Cuerpo");
            UiKit.Rellenar(cuerpo);
            pv.Fondo = cuerpo.gameObject.AddComponent<Image>();
            Ui.Fondo(pv.Fondo, NexusTheme.Alfa(new Color(0.05f, 0.08f, 0.12f), 0.93f), 10);
            pv.Fondo.raycastTarget = true;
            pv.Borde = Ui.Borde(cuerpo, d.Critico ? NexusTheme.Alfa(Tema.danger, 0.8f) : NexusTheme.Alfa(pv.Color, 0.6f), 10, d.Critico ? 2.6f : 2f);
            var escucha = cuerpo.gameObject.AddComponent<EscuchaDeRaton>();
            escucha.AlEntrar = () => InfoDePiso(pv);
            escucha.AlPulsar = () => InfoDePiso(pv);

            pv.Dib = CapaDib(cuerpo, "Dibujo");
            var nombre = TextoFijo(rt, tipo.Nombre, 15, pv.Color, TextAlignmentOptions.MidlineLeft, 76, 6, 190, 24);
            nombre.fontStyle = FontStyles.Bold; nombre.characterSpacing = 3;
            nombre.enableAutoSizing = true; nombre.fontSizeMin = 11; nombre.fontSizeMax = 15;
            pv.Horas = TextoFijo(rt, "", 28, Tema.ink, TextAlignmentOptions.MidlineLeft, 76, 32, 120, 36);
            pv.Horas.fontStyle = FontStyles.Bold;
            var coste = TextoFijo(rt, "×" + d.CostePorDefecto + " h", 14, Tema.inkMuted, TextAlignmentOptions.MidlineLeft, 102, pv.H - 30, 90, 22);
            coste.characterSpacing = 1;

            var id = d.Id;
            pv.Menos = Ui.Boton(rt, "−" + Paso, () => Sumar(id, -Paso));
            Lamina.Colocar((RectTransform)pv.Menos.transform, pv.W - 126, pv.H / 2f - 24, 56, 48);
            pv.Mas = Ui.Boton(rt, "+" + Paso, () => Sumar(id, Paso));
            Lamina.Colocar((RectTransform)pv.Mas.transform, pv.W - 64, pv.H / 2f - 24, 56, 48);
            _mas[id] = (RectTransform)pv.Mas.transform;
            if (i == 0) GuiaView.Registrar("mj.repartir.mas", pv.Mas);
            return pv;
        }

        /// <summary>
        /// Lo que se dibuja dentro de un piso: el icono, la moneda del coste, las siluetas de los bichos que cabe esperar
        /// (o, en el cierre, lo que se atrapo y lo que se colo) y las redes que frenan las horas puestas.
        /// </summary>
        private void PintarPiso(PisoVista pv) {
            var d = pv.Dib;
            d.Limpiar();
            var dep = pv.Dep;
            DibujosDelEdificio.IconoDePiso(d, pv.Tipo.Id, 38, pv.H / 2f, pv.Color);
            DibujosDelEdificio.Moneda(d, 90, pv.H - 19, 7);
            if (dep.Critico) DibujosDelEdificio.Escudo(d, pv.W - 24, 20, 0.95f, pv.Resultado != null && pv.Resultado.Escapan > 0 ? Tema.danger : NexusTheme.Alfa(Tema.danger, 0.9f));

            const float x0 = 268, paso = 30;
            var yRadar = pv.H * 0.31f; var yRedes = pv.H * 0.72f;
            // Marcas de lo que enseña cada fila: un bichito (lo que viene) y una red (lo que frena).
            DibujosDelEdificio.Bicho(d, pv.Tipo.Bicho, x0 - 24, yRadar, 0.5f, NexusTheme.Alfa(Tema.inkMuted, 0.8f));
            DibujosDelEdificio.Red(d, x0 - 24, yRedes, 7, NexusTheme.Alfa(Tema.inkMuted, 0.8f), true);

            var horas = _asignacion[dep.Id];
            var alcanza = horas / Mathf.Max(1, dep.CostePorDefecto);
            if (pv.Resultado != null) {
                // El cierre: lo atrapado en el color del piso, lo colado en rojo.
                var r = pv.Resultado;
                var total = Mathf.Min(6, r.Encontrados + r.Escapan);
                for (var k = 0; k < total; k++) {
                    var atrapado = k < r.Encontrados;
                    DibujosDelEdificio.Bicho(d, pv.Tipo.Bicho, x0 + k * paso, yRadar, 0.62f, atrapado ? pv.Color : Tema.danger);
                    if (atrapado) d.Check(x0 + k * paso + 9, yRadar + 11, Tema.success);
                    else d.Cruz(x0 + k * paso + 9, yRadar + 11, Tema.danger, 4);
                }
            } else {
                var max = RepartirEvaluador.MaximoEsperado(dep); var min = RepartirEvaluador.MinimoEsperado(dep);
                if (Revelado) {
                    for (var k = 0; k < Mathf.Min(6, dep.DefectosOcultos); k++) DibujosDelEdificio.Bicho(d, pv.Tipo.Bicho, x0 + k * paso, yRadar, 0.62f, pv.Color);
                } else if (Andamiaje < 2 && dep.EstimadoMax > 0) {
                    // Sin pistas del rango: tres siluetas de duda.
                    for (var k = 0; k < 3; k++) DibujosDelEdificio.Bicho(d, pv.Tipo.Bicho, x0 + k * paso, yRadar, 0.62f, NexusTheme.Alfa(Tema.inkMuted, 0.45f));
                } else {
                    for (var k = 0; k < Mathf.Min(6, max); k++)
                        DibujosDelEdificio.Bicho(d, pv.Tipo.Bicho, x0 + k * paso, yRadar, 0.62f, pv.Color, k < min ? 1f : 0.35f);
                }
            }
            // Las redes: tantas como errores caben esperar (o las que alcanzan, si son mas), encendidas las que dan las horas.
            var huecos = Mathf.Min(6, Mathf.Max(RepartirEvaluador.MaximoEsperado(dep), alcanza));
            for (var k = 0; k < huecos; k++) DibujosDelEdificio.Red(d, x0 + k * paso, yRedes, 10, pv.Color, k < alcanza);
        }

        /// <summary>Al pasar o pulsar un piso: que parte del sistema revisa, que no ve y la incidencia concreta del reto.</summary>
        private void InfoDePiso(PisoVista pv) {
            if (_soltando || (_globo == null && _leccionTexto == null)) return;
            var propia = pv.Dep.Descripcion;
            var primera = string.IsNullOrEmpty(propia) ? null : propia.Split(new[] { ". " }, StringSplitOptions.None)[0].TrimEnd('.');
            var texto = pv.Tipo.Nombre + ": " + char.ToUpperInvariant(pv.Tipo.Defensa[0]) + pv.Tipo.Defensa.Substring(1) + ". No ve " + pv.Tipo.NoVe + "." +
                        (primera != null ? " Aquí: " + primera.Substring(0, 1).ToLowerInvariant() + primera.Substring(1) + "." : "") +
                        (pv.Dep.Critico ? " No puede fallar." : "");
            if (_leccionTexto != null) {
                // En el cierre: la leccion del piso y lo que le paso.
                var r = pv.Resultado;
                _leccionTexto.text = Nexus.Core.Narrativa.Glifos.Seguro(texto + (r == null ? "" : r.Escapan == 0 ? $" Frenó los {r.Encontrados}." : $" Frenó {r.Encontrados} y se {(r.Escapan == 1 ? "coló 1" : "colaron " + r.Escapan)}."));
                return;
            }
            Decir(texto);
        }

        // ---------------------------------------------------------------- la entrada

        /// <summary>
        /// La escena entra: el edificio se levanta piso a piso con rebote, la caja y la gente del cliente aparecen y la
        /// bolsa sube desde abajo. Devuelve cuanto dura todo.
        /// </summary>
        private float EntradaDeLaEscena() {
            var dueno = _lam.Raiz.gameObject;
            var grupo = Grupo(_lam.Raiz.gameObject);
            grupo.alpha = 0;
            _anim.Lanzar(0.45f, t => { if (grupo != null) grupo.alpha = Curvas.SalidaCubica(t); }, () => { if (grupo != null) grupo.alpha = 1; }, 0, dueno);

            var fin = 1.0f;
            for (var i = 0; i < _pisos.Count; i++) {
                var pv = _vistas[_pisos[i].Id];
                var g = Grupo(pv.Rt.gameObject);
                g.alpha = 0;
                var meta = pv.Rt.anchoredPosition;
                var retraso = 0.25f + i * 0.2f;
                _anim.Lanzar(0.55f, t => {
                    if (pv.Rt == null) return;
                    g.alpha = Mathf.Clamp01(t * 2.5f);
                    pv.Rt.anchoredPosition = new Vector2(meta.x, meta.y - 90f * (1f - Curvas.SalidaAtras(t)));
                }, () => { if (pv.Rt != null) { g.alpha = 1; pv.Rt.anchoredPosition = meta; } }, retraso, pv.Rt.gameObject);
                fin = Mathf.Max(fin, retraso + 0.6f);
            }
            // La bolsa sube desde abajo.
            var gb = _bolsa.gameObject.AddComponent<CanvasGroup>();
            gb.alpha = 0;
            var by = _bolsa.anchoredPosition;
            _anim.Lanzar(0.5f, t => { if (_bolsa != null) { gb.alpha = Mathf.Clamp01(t * 2f); _bolsa.anchoredPosition = new Vector2(by.x, by.y - 60f * (1f - Curvas.SalidaAtras(t))); } },
                         () => { if (_bolsa != null) { gb.alpha = 1; _bolsa.anchoredPosition = by; } }, 0.6f, _bolsa.gameObject);
            return Mathf.Max(fin, 1.2f);
        }

        // ==================================================================== efectos del edificio

        private void LateUpdate() {
            if (_edificio != null) {
                // El edificio se inclina si las horas pesan mas arriba que en la base (solo se ve; no cambia el resultado).
                var objetivo = _soltando || Resultado != null ? 0f : -Mathf.Clamp(PesoDeArriba(), -1f, 1f) * 2.6f;
                var z = _edificio.localEulerAngles.z; if (z > 180f) z -= 360f;
                _edificio.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(z, objetivo, 1f - Mathf.Exp(-5f * Time.unscaledDeltaTime)));
            }
            if (_luzAntena != null) _luzAntena.color = NexusTheme.Alfa(Tema.danger, 0.35f + 0.65f * Mathf.Max(0f, Mathf.Sin(Time.unscaledTime * 3.2f)));
            if (_soltar != null) {
                var late = !_soltando && !_bloqueado && Resultado == null && Usado > 0 && !TutorialAbierto;
                _soltar.transform.localScale = Vector3.one * (late ? 1f + 0.03f * Mathf.Sin(Time.unscaledTime * 6f) : 1f);
            }
            // Los pisos criticos laten en rojo.
            foreach (var pv in _vistas.Values)
                if (pv.Dep.Critico && pv.Borde != null) pv.Borde.color = NexusTheme.Alfa(Tema.danger, 0.55f + 0.35f * Mathf.Max(0f, Mathf.Sin(Time.unscaledTime * 3f)));
        }

        /// <summary>De −1 (todo en la base) a +1 (todo en la cima): hacia donde pesan las horas.</summary>
        private float PesoDeArriba() {
            var usado = Usado;
            if (usado <= 0 || _pisos.Count < 2) return 0f;
            var suma = 0f;
            for (var i = 0; i < _pisos.Count; i++) suma += _asignacion[_pisos[i].Id] * (i / (float)(_pisos.Count - 1) * 2f - 1f);
            return suma / usado;
        }
    }
}
