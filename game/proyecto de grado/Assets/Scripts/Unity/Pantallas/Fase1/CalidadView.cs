using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Modelo;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El reparto de fichas de calidad de la Fase 1 (2026-11-07), en DOS vistas del mismo reparto:
    ///   · Tarjetas: una por atributo, con sus subcaracteristicas, y los circulos − / + para repartir.
    ///   · Tabla: la tabla completa de la ISO 25010; se iluminan las columnas que este nivel permite y cada una lleva
    ///     un circulo con sus fichas (tambien se reparte desde ahi). Pasar el raton por una casilla dice que significa.
    /// Pasar de una a otra es un movimiento (las tarjetas suben y se apagan, la tabla entra desde abajo y sus columnas
    /// se encienden en cascada). Las fichas viven en el diccionario que entrega la pantalla: aqui solo se pintan y se
    /// cambian, sin repintar la pantalla entera (si no, se perderia la animacion).
    /// </summary>
    public sealed class CalidadView {
        private readonly UiKit _ui;
        private readonly NexusTheme _t;
        private readonly RectTransform _padre, _raizDeTooltip;
        private readonly CalidadConfig _cfg;
        private readonly IDictionary<string, int> _fichas;
        private readonly Action _alCambiar;
        private readonly Action<Transform, string> _chip;
        private readonly Action<bool> _alCambiarVista;
        private bool _tabla;

        private TMP_Text _contador, _leyendaDelContador, _textoDelConmutador;
        private ReboteDeCalidad _reboteDelContador;
        private AnimadorDeCalidad _animador;
        private TooltipDeCalidad _tip;
        private readonly List<Tarjeta> _tarjetas = new List<Tarjeta>();
        private readonly List<Columna> _columnas = new List<Columna>();

        private sealed class Tarjeta {
            public AtributoCalidad Atributo;
            public TMP_Text Numero;
            public GameObject Aviso, Pips;
            public Button Menos, Mas;
            public TarjetaViva Viva;
        }

        private sealed class Columna {
            public AtributoCalidad Atributo;   // null = esta columna de la ISO no se reparte en este nivel
            public ColumnaViva Viva;
            public TMP_Text Numero;
            public Button Menos, Mas;
        }

        public CalidadView(UiKit ui, RectTransform padre, RectTransform raizDeTooltip, CalidadConfig cfg,
                           IDictionary<string, int> fichas, Action alCambiar, Action<Transform, string> chip,
                           bool enTabla, Action<bool> alCambiarVista) {
            _ui = ui; _t = ui.Tema; _padre = padre; _raizDeTooltip = raizDeTooltip; _cfg = cfg; _fichas = fichas;
            _alCambiar = alCambiar; _chip = chip; _tabla = enTabla; _alCambiarVista = alCambiarVista;
        }

        private int Libres { get { return _cfg.Fichas - _fichas.Values.Sum(); } }
        private int De(string id) { int v; return _fichas.TryGetValue(id, out v) ? v : 0; }

        // ==================================================================== construir

        public void Construir() {
            _tip = TooltipDeCalidad.Obtener(_ui, _raizDeTooltip);
            _tip.OcultarYa();

            // ---- la barra: titulo de la seccion · cuantas fichas quedan · cambiar de vista
            var barra = _ui.Fila(_padre, "BarraDeCalidad", _t.espacio, 0, TextAnchor.MiddleLeft);
            var rotulo = _ui.Columna(barra, "Rotulo", 2);
            UiKit.Tamano(rotulo, flexAncho: 1);
            _ui.Texto(rotulo, "Atributos de calidad", EstiloTexto.Subtitulo);
            _ui.Texto(rotulo, "Reparte con + y −. Las fichas sin usar no se guardan.", EstiloTexto.Pequeno);

            var cuenta = _ui.Columna(barra, "Cuenta", 0, 0, TextAnchor.MiddleCenter);
            _contador = _ui.Texto(cuenta, "", EstiloTexto.Titulo, _t.cyan, TextAlignmentOptions.Center);
            _contador.fontSize = _t.Px(32);
            _contador.textWrappingMode = TextWrappingModes.NoWrap;
            _reboteDelContador = _contador.gameObject.AddComponent<ReboteDeCalidad>();
            _leyendaDelContador = _ui.Texto(cuenta, "", EstiloTexto.Leyenda, _t.inkMuted, TextAlignmentOptions.Center);
            UiKit.Tamano(cuenta, ancho: _t.Px(210));

            var conmutador = _ui.Boton(barra, "Ver tabla completa", () => Ir(!_tabla), VarianteBoton.Secundario);
            _textoDelConmutador = conmutador.GetComponentInChildren<TMP_Text>();

            // ---- el escenario: las dos vistas, una encima de la otra
            var alto = _t.Px(324);
            var escenario = _ui.Nodo(_padre, "Escenario");
            UiKit.Tamano(escenario, alto: alto, flexAncho: 1);
            escenario.gameObject.AddComponent<ListaDeElementos>();   // los +/− son contenido, no botones de la pantalla
            escenario.gameObject.AddComponent<RectMask2D>();
            _animador = escenario.gameObject.AddComponent<AnimadorDeCalidad>();
            _animador.Alto = alto;

            CanvasGroup grupoTarjetas, grupoTabla;
            var vistaTabla = Vista(escenario, "Tabla ISO 25010", out grupoTabla);
            var vistaTarjetas = Vista(escenario, "Tarjetas", out grupoTarjetas);
            _animador.VistaTabla = vistaTabla; _animador.GrupoTabla = grupoTabla;
            _animador.VistaTarjetas = vistaTarjetas; _animador.GrupoTarjetas = grupoTarjetas;

            ConstruirTarjetas(vistaTarjetas);
            ConstruirTabla(vistaTabla);

            _animador.Tarjetas.AddRange(_tarjetas.Select(x => x.Viva));
            _animador.Columnas.AddRange(_columnas.Select(x => x.Viva));
            _animador.Fijar(_tabla, true);
            Actualizar();
            ActualizarConmutador();
        }

        private RectTransform Vista(RectTransform padre, string nombre, out CanvasGroup grupo) {
            var rt = _ui.Nodo(padre, nombre);
            UiKit.Rellenar(rt, _t.Px(12));
            grupo = rt.gameObject.AddComponent<CanvasGroup>();
            return rt;
        }

        private void Ir(bool tabla) {
            if (_tabla == tabla) return;
            _tabla = tabla;
            _tip.OcultarYa();
            _animador.Fijar(tabla, false);
            ActualizarConmutador();
            if (_alCambiarVista != null) _alCambiarVista(tabla);
        }

        private void ActualizarConmutador() {
            if (_textoDelConmutador != null) _textoDelConmutador.text = _tabla ? "Ver tarjetas" : "Ver tabla completa";
        }

        // ==================================================================== vista A · tarjetas

        private void ConstruirTarjetas(RectTransform vista) {
            var fila = _ui.Fila(vista, "FilaDeTarjetas", _t.Espacio(4), 0, TextAnchor.MiddleCenter);
            vista.gameObject.AddComponent<EscaladorAlCaber>().Interior = fila;   // si no caben, se encogen: nunca hay scroll
            for (var i = 0; i < _cfg.Atributos.Count; i++) _tarjetas.Add(CrearTarjeta(fila, _cfg.Atributos[i], i));
        }

        private Tarjeta CrearTarjeta(Transform padre, AtributoCalidad a, int indice) {
            var tj = new Tarjeta { Atributo = a };
            var iso = TablaIso25010.DeAtributo(a.Id);
            var lado = _t.Px(16);

            var raiz = _ui.Nodo(padre, "Tarjeta " + a.Nombre);
            var le = raiz.gameObject.AddComponent<LayoutElement>();
            le.minWidth = _t.Px(150); le.preferredWidth = _t.Px(230); le.flexibleWidth = 1; le.flexibleHeight = 1;
            le.preferredHeight = _t.Px(300);   // la altura de diseño: el escalador la encoge si el espacio es menor

            var cuerpo = _ui.Nodo(raiz, "Cuerpo");
            UiKit.Rellenar(cuerpo);
            var fondo = cuerpo.gameObject.AddComponent<Image>();
            _ui.Fondo(fondo, NexusTheme.Alfa(_t.bg950, 0.62f), NexusTheme.RadioLg);
            var halo = _ui.Halo(cuerpo, NexusTheme.Alfa(_t.cyan, 0), NexusTheme.RadioLg);
            var borde = _ui.Borde(cuerpo, _t.lineStrong, NexusTheme.RadioLg, 1.5f);
            var grupo = cuerpo.gameObject.AddComponent<CanvasGroup>();

            // cabecera: el nombre (grande) y el «?» del glosario; al pasar el raton, que significa
            var altoCab = _t.Px(52);
            var cab = _ui.Fila(cuerpo, "Cabecera", _t.Espacio(2), 0, TextAnchor.UpperLeft);
            FranjaSuperior(cab, lado, altoCab, lado);
            var area = cab.gameObject.AddComponent<Image>();
            area.color = Color.clear;
            var nombre = _ui.Texto(cab, a.Nombre, EstiloTexto.Encabezado, _t.ink);
            nombre.fontSize = _t.Px(21);
            nombre.fontStyle = FontStyles.Bold;
            UiKit.Tamano(nombre, flexAncho: 1);
            if (_chip != null) _chip(cab, a.Id);
            var realceCab = cab.gameObject.AddComponent<RealceDeCalidad>();
            realceCab.Entra = () => _tip.Mostrar(a.Nombre, De(a.Id) == 0 && !string.IsNullOrEmpty(a.TextoSinInversion)
                                                       ? "Sin fichas: " + a.TextoSinInversion
                                                       : iso != null ? iso.Resumen : a.Nombre, _t.cyan);
            realceCab.Sale = _tip.Ocultar;

            // controles: − · fichas · +
            var altoCtl = _t.Px(40);
            var ctl = _ui.Fila(cuerpo, "Controles", 0, 0, TextAnchor.MiddleCenter);
            FranjaInferior(ctl, lado, altoCtl, lado);
            tj.Menos = Redondo(ctl, "−", altoCtl, () => Cambiar(a.Id, -1));
            _ui.Resorte(ctl);
            tj.Numero = _ui.Texto(ctl, "0", EstiloTexto.Titulo, _t.ink, TextAlignmentOptions.Center);
            tj.Numero.fontSize = _t.Px(26);
            _ui.Resorte(ctl);
            tj.Mas = Redondo(ctl, "+", altoCtl, () => Cambiar(a.Id, +1));

            // pips: una marca por ficha, que se enciende con un pequeño salto
            var altoPip = _t.Px(14);
            var yPip = lado + altoCtl + _t.Px(8);
            var pipsFila = _ui.Fila(cuerpo, "Pips", _t.Px(5), 0, TextAnchor.MiddleCenter);
            FranjaInferior(pipsFila, yPip, altoPip, lado);
            tj.Pips = pipsFila.gameObject;
            var pips = new Image[Mathf.Clamp(_cfg.Fichas, 3, 8)];
            for (var i = 0; i < pips.Length; i++) {
                var p = _ui.Nodo(pipsFila, "Pip");
                UiKit.Tamano(p, ancho: _t.Px(12), alto: _t.Px(12));
                pips[i] = p.gameObject.AddComponent<Image>();
                pips[i].sprite = _ui.SpriteCircular();
                pips[i].raycastTarget = false;
            }

            // aviso corto cuando no hay fichas (el motivo completo, al pasar el raton por la cabecera)
            // (ocupa el sitio de los pips: cuando no hay fichas, en vez de pips apagados, el aviso)
            var aviso = _ui.Texto(cuerpo, "SIN FICHAS", EstiloTexto.Leyenda, _t.warning, TextAlignmentOptions.Center);
            TextoEnCaja.Ajustar(aviso, _t.Px(11));
            FranjaInferior(aviso.rectTransform, yPip - _t.Px(3), altoPip + _t.Px(6), lado);
            tj.Aviso = aviso.gameObject;

            // subcaracteristicas de muestra
            var lista = _ui.Columna(cuerpo, "Subcaracteristicas", 2, 0, TextAnchor.UpperLeft);
            FranjaMedia(lista, yPip + altoPip + _t.Px(8), lado + altoCab + _t.Px(6), lado);
            if (iso != null) {
                const int visibles = 3;
                foreach (var s in iso.Subcaracteristicas.Take(visibles)) Casilla(lista, s, _t.cyan, true, 13.5f);
                var resto = iso.Subcaracteristicas.Count - visibles;
                if (resto > 0) {
                    var mas = _ui.Texto(lista, "+ " + resto + " más en la tabla", EstiloTexto.Leyenda, _t.inkMuted);
                    mas.fontSize = _t.Px(12);
                }
            }

            var viva = cuerpo.gameObject.AddComponent<TarjetaViva>();
            viva.Grupo = grupo; viva.Halo = halo; viva.Borde = borde; viva.Pips = pips;
            viva.Cian = _t.cyan; viva.Apagado = NexusTheme.Alfa(_t.inkMuted, 0.35f);
            viva.BordeNormal = _t.lineStrong; viva.BordeActivo = _t.cyan;
            viva.Desplazamiento = _t.Px(40); viva.Elevacion = _t.Px(6);
            viva.Reiniciar(0.08f * indice);
            tj.Viva = viva;
            return tj;
        }

        // ==================================================================== vista B · tabla ISO 25010

        private void ConstruirTabla(RectTransform vista) {
            var fila = _ui.Fila(vista, "FilaDeColumnas", _t.Px(6), 0, TextAnchor.UpperCenter);
            vista.gameObject.AddComponent<EscaladorAlCaber>().Interior = fila;   // la tabla entera cabe, a la escala que haga falta
            var orden = 0;
            foreach (var iso in TablaIso25010.Columnas) {
                var disponible = _cfg.Atributos.FirstOrDefault(x => TablaIso25010.IdDeColumna(x.Id) == iso.Id);
                _columnas.Add(CrearColumna(fila, iso, disponible, disponible != null ? orden++ : 0));
            }
        }

        private Columna CrearColumna(Transform padre, CaracteristicaIso iso, AtributoCalidad a, int orden) {
            var col = new Columna { Atributo = a };
            var disponible = a != null;

            var raiz = _ui.Columna(padre, "Columna " + iso.Nombre, _t.Px(2), _t.Px(5), TextAnchor.UpperLeft);
            var le = raiz.gameObject.AddComponent<LayoutElement>();
            le.minWidth = _t.Px(96); le.preferredWidth = _t.Px(150); le.flexibleWidth = 1; le.flexibleHeight = 1;
            var fondo = raiz.gameObject.AddComponent<Image>();
            _ui.Fondo(fondo, NexusTheme.Alfa(_t.bg950, 0.5f), NexusTheme.RadioMd);
            fondo.raycastTarget = false;
            var grupo = raiz.gameObject.AddComponent<CanvasGroup>();
            var halo = _ui.Halo(raiz, NexusTheme.Alfa(_t.cyan, 0), NexusTheme.RadioMd);
            var borde = _ui.Borde(raiz, _t.line, NexusTheme.RadioMd, 1.5f);

            // titulo de la columna, con la ficha (el circulo) abajo a la derecha
            var cab = _ui.Nodo(raiz, "Titulo");
            UiKit.Tamano(cab, alto: _t.Px(46), flexAncho: 1);
            var cabFondo = cab.gameObject.AddComponent<Image>();
            _ui.Fondo(cabFondo, NexusTheme.Alfa(_t.ink, 0.06f), NexusTheme.RadioSm);
            cabFondo.raycastTarget = true;
            var titulo = _ui.Texto(cab, iso.Nombre, EstiloTexto.Encabezado, _t.ink, TextAlignmentOptions.Center);
            titulo.fontSize = _t.Px(13.5f);
            titulo.fontStyle = FontStyles.Bold;
            TextoEnCaja.Ajustar(titulo, _t.Px(13.5f));
            UiKit.Rellenar(titulo.rectTransform, _t.Px(4));
            // a la derecha queda el sitio del circulo de fichas: el titulo no pasa por debajo
            if (disponible) titulo.rectTransform.offsetMax = new Vector2(-_t.Px(34), -_t.Px(4));
            var realceCab = cab.gameObject.AddComponent<RealceDeCalidad>();
            realceCab.Entra = () => _tip.Mostrar(iso.Nombre, iso.Resumen, _t.cyan);
            realceCab.Sale = _tip.Ocultar;

            RectTransform ficha = null;
            if (disponible) {
                // dentro de la cabecera, abajo a la derecha (antes se salia por abajo y tapaba la primera casilla)
                var lado = _t.Px(28);
                ficha = _ui.Nodo(cab, "Ficha");
                ficha.anchorMin = ficha.anchorMax = new Vector2(1, 0);
                ficha.pivot = new Vector2(0.5f, 0.5f);
                ficha.sizeDelta = new Vector2(lado, lado);
                ficha.anchoredPosition = new Vector2(-lado * 0.5f - _t.Px(3), lado * 0.5f + _t.Px(3));
                var fi = ficha.gameObject.AddComponent<Image>();
                _ui.Fondo(fi, _t.bg950, 14);
                fi.raycastTarget = false;
                _ui.Borde(ficha, _t.cyan, 14, 2);
                col.Numero = _ui.Texto(ficha, "0", EstiloTexto.Encabezado, _t.cyan, TextAlignmentOptions.Center);
                col.Numero.fontStyle = FontStyles.Bold;
                TextoEnCaja.Ajustar(col.Numero, _t.Px(15));
                UiKit.Rellenar(col.Numero.rectTransform);
            }

            foreach (var s in iso.Subcaracteristicas) Casilla(raiz, s, _t.cyan, false, 12.5f);
            _ui.Resorte(raiz);

            CanvasGroup controles = null;
            if (disponible) {
                var lado = _t.Px(34);
                var ctl = _ui.Fila(raiz, "Controles", _t.Px(10), 0, TextAnchor.MiddleCenter);
                controles = ctl.gameObject.AddComponent<CanvasGroup>();
                col.Menos = Redondo(ctl, "−", lado, () => Cambiar(a.Id, -1));
                col.Mas = Redondo(ctl, "+", lado, () => Cambiar(a.Id, +1));
            }

            var viva = raiz.gameObject.AddComponent<ColumnaViva>();
            viva.Grupo = grupo; viva.Controles = controles; viva.Borde = borde; viva.Halo = halo; viva.Cabecera = cabFondo;
            viva.Titulo = titulo; viva.Ficha = ficha; viva.Disponible = disponible; viva.Orden = orden;
            viva.Cian = _t.cyan; viva.BordeBase = _t.line; viva.TextoBase = _t.ink;
            viva.CabeceraBase = NexusTheme.Alfa(_t.ink, 0.06f);
            viva.CabeceraEncendida = NexusTheme.Alfa(_t.cyan, 0.22f);
            col.Viva = viva;
            return col;
        }

        // ==================================================================== piezas compartidas

        /// <summary>Una casilla de la tabla (o una linea de una tarjeta): su nombre; al pasar el raton, que significa.</summary>
        private void Casilla(Transform padre, SubcaracteristicaIso s, Color acento, bool vineta, float tam) {
            var rt = _ui.Columna(padre, "Casilla " + s.Nombre, 0, _t.Px(3), TextAnchor.MiddleLeft);
            var img = rt.gameObject.AddComponent<Image>();
            _ui.Fondo(img, NexusTheme.Alfa(acento, 0), NexusTheme.RadioSm);
            img.raycastTarget = true;
            var tx = _ui.Texto(rt, (vineta ? "•  " : "") + s.Nombre, EstiloTexto.Pequeno, _t.ink);
            tx.fontSize = _t.Px(tam);
            var r = rt.gameObject.AddComponent<RealceDeCalidad>();
            r.Fondo = img; r.Normal = NexusTheme.Alfa(acento, 0); r.Resaltado = NexusTheme.Alfa(acento, 0.24f);
            r.Entra = () => _tip.Mostrar(s.Nombre, s.Significado, acento);
            r.Sale = _tip.Ocultar;
        }

        /// <summary>Un boton circular (− o +) con borde neon: crece al pasar el raton y se apaga cuando no se puede.</summary>
        private Button Redondo(Transform padre, string signo, float lado, Action alPulsar) {
            var rt = _ui.Nodo(padre, "Boton " + signo);
            UiKit.Tamano(rt, ancho: lado, alto: lado);
            var img = rt.gameObject.AddComponent<Image>();
            var radio = lado / _t.escala * 0.5f;   // en px de web: el radio es la mitad del lado
            _ui.Fondo(img, NexusTheme.Alfa(_t.bg950, 0.85f), radio);
            _ui.Borde(rt, _t.cyan, radio, 2);
            var boton = rt.gameObject.AddComponent<Button>();
            boton.targetGraphic = img;
            boton.transition = Selectable.Transition.None;
            boton.navigation = new Navigation { mode = Navigation.Mode.None };
            boton.onClick.AddListener(() => alPulsar());
            var t = _ui.Texto(rt, signo, EstiloTexto.Encabezado, _t.cyan, TextAlignmentOptions.Center);
            t.fontSize = lado * 0.55f;
            t.fontStyle = FontStyles.Bold;
            UiKit.Rellenar(t.rectTransform);
            var g = rt.gameObject.AddComponent<CanvasGroup>();
            var r = rt.gameObject.AddComponent<RealceDeCalidad>();
            r.Fondo = img; r.Normal = NexusTheme.Alfa(_t.bg950, 0.85f); r.Resaltado = NexusTheme.Alfa(_t.cyan, 0.30f);
            r.Escala = 1.14f; r.Boton = boton; r.Opacidad = g;
            return boton;
        }

        private static void FranjaSuperior(RectTransform rt, float y, float alto, float lado) {
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(lado, -(y + alto)); rt.offsetMax = new Vector2(-lado, -y);
        }

        private static void FranjaInferior(RectTransform rt, float y, float alto, float lado) {
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(0.5f, 0);
            rt.offsetMin = new Vector2(lado, y); rt.offsetMax = new Vector2(-lado, y + alto);
        }

        private static void FranjaMedia(RectTransform rt, float desdeAbajo, float desdeArriba, float lado) {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(lado, desdeAbajo); rt.offsetMax = new Vector2(-lado, -desdeArriba);
        }

        // ==================================================================== cambiar y refrescar

        private void Cambiar(string id, int delta) {
            var actual = De(id);
            if (actual + delta < 0 || delta > Libres) return;
            _fichas[id] = actual + delta;
            if (_alCambiar != null) _alCambiar();
            Actualizar();
            foreach (var tj in _tarjetas) if (tj.Atributo.Id == id) tj.Viva.Pulso();
            foreach (var c in _columnas) if (c.Atributo != null && c.Atributo.Id == id) c.Viva.Pulso();
            _reboteDelContador.Lanzar();
        }

        private void Actualizar() {
            var libres = Libres;
            _contador.text = libres + " de " + _cfg.Fichas;
            _contador.color = libres == 0 ? _t.success : _t.cyan;
            _leyendaDelContador.text = libres == 0 ? "TODAS REPARTIDAS" : "FICHAS SIN REPARTIR";
            foreach (var tj in _tarjetas) {
                var v = De(tj.Atributo.Id);
                tj.Numero.text = v.ToString();
                tj.Numero.color = v > 0 ? _t.cyan : _t.inkMuted;
                tj.Aviso.SetActive(v == 0);
                tj.Pips.SetActive(v > 0);
                tj.Menos.interactable = v > 0;
                tj.Mas.interactable = libres > 0;
                tj.Viva.Valor = v;
            }
            foreach (var c in _columnas) {
                if (c.Atributo == null) continue;
                var v = De(c.Atributo.Id);
                c.Numero.text = v.ToString();
                c.Menos.interactable = v > 0;
                c.Mas.interactable = libres > 0;
            }
        }
    }

    // ======================================================================== animaciones y tooltip

    /// <summary>
    /// El movimiento entre las dos vistas. P va de 0 (tarjetas) a 1 (tabla) y se puede invertir a medias. Las tarjetas
    /// suben y se apagan; la tabla entra desde abajo; cuando casi ha llegado, sus columnas se encienden en cascada.
    /// </summary>
    internal sealed class AnimadorDeCalidad : MonoBehaviour {
        public RectTransform VistaTarjetas, VistaTabla;
        public CanvasGroup GrupoTarjetas, GrupoTabla;
        public float Alto;
        public readonly List<TarjetaViva> Tarjetas = new List<TarjetaViva>();
        public readonly List<ColumnaViva> Columnas = new List<ColumnaViva>();
        public float P;
        private bool _tabla;
        private float _luz;

        public void Fijar(bool tabla, bool ya) {
            _tabla = tabla;
            if (ya) { P = tabla ? 1 : 0; _luz = tabla ? 10 : 0; }
            else if (!tabla) for (var i = 0; i < Tarjetas.Count; i++) Tarjetas[i].Reiniciar(0.18f + 0.07f * i);
            Aplicar();
        }

        private static float Suave(float x) { x = Mathf.Clamp01(x); return x * x * (3 - 2 * x); }

        private void Update() {
            var dt = Time.unscaledDeltaTime;
            P = Mathf.MoveTowards(P, _tabla ? 1 : 0, dt / 0.6f);
            if (P >= 0.65f && _tabla) _luz += dt; else if (P < 0.65f) _luz = 0;
            Aplicar();
        }

        private void Aplicar() {
            var e = Suave(P);
            GrupoTarjetas.alpha = 1 - Suave(P / 0.55f);
            VistaTarjetas.anchoredPosition = new Vector2(0, e * Alto * 0.18f);
            VistaTarjetas.localScale = Vector3.one * (1 - 0.06f * e);
            GrupoTarjetas.blocksRaycasts = GrupoTarjetas.interactable = P < 0.4f;

            GrupoTabla.alpha = Suave((P - 0.3f) / 0.7f);
            VistaTabla.anchoredPosition = new Vector2(0, -(1 - e) * Alto * 0.22f);
            VistaTabla.localScale = Vector3.one * (0.95f + 0.05f * e);
            GrupoTabla.blocksRaycasts = GrupoTabla.interactable = P > 0.6f;

            for (var i = 0; i < Columnas.Count; i++) {
                var c = Columnas[i];
                c.Fijar(Mathf.Clamp01((_luz - c.Orden * 0.09f) / 0.35f), Time.unscaledTime);
            }
        }
    }

    /// <summary>Una tarjeta de atributo: entra con retraso, se eleva al pasar el raton, rebota al cambiar y enciende sus pips.</summary>
    internal sealed class TarjetaViva : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        public CanvasGroup Grupo;
        public Image Halo, Borde;
        public Image[] Pips;
        public Color Cian, Apagado, BordeNormal, BordeActivo;
        public float Desplazamiento, Elevacion;
        public int Valor;
        private RectTransform _rt;
        private float _entrada, _hover, _pulso = 1;
        private float[] _pip;
        private bool _dentro;

        public void Reiniciar(float retraso) { _entrada = -retraso; }
        public void Pulso() { _pulso = 0; }
        public void OnPointerEnter(PointerEventData e) { _dentro = true; }
        public void OnPointerExit(PointerEventData e) { _dentro = false; }
        private void OnDisable() { _dentro = false; }

        private void Update() {
            if (_rt == null) _rt = (RectTransform)transform;
            if (_pip == null) _pip = new float[Pips.Length];
            var dt = Time.unscaledDeltaTime;
            _entrada += dt;
            var k = Mathf.Clamp01(_entrada / 0.45f);
            var e = 1 - Mathf.Pow(1 - k, 3);
            Grupo.alpha = e;
            _hover = Mathf.MoveTowards(_hover, _dentro ? 1 : 0, dt / 0.15f);
            _pulso = Mathf.Min(1, _pulso + dt / 0.3f);

            _rt.anchoredPosition = new Vector2(0, -(1 - e) * Desplazamiento + _hover * Elevacion);
            var escala = (0.96f + 0.04f * e) * (1 + 0.03f * _hover) * (1 + 0.05f * Mathf.Sin(Mathf.PI * _pulso));
            _rt.localScale = Vector3.one * escala;

            var activa = Valor > 0;
            Borde.color = Color.Lerp(activa ? BordeActivo : BordeNormal, BordeActivo, _hover);
            Halo.color = NexusTheme.Alfa(Cian, Mathf.Max(activa ? 0.16f : 0, _hover * 0.3f));

            for (var i = 0; i < Pips.Length; i++) {
                _pip[i] = Mathf.MoveTowards(_pip[i], i < Valor ? 1 : 0, dt / 0.2f);
                Pips[i].color = Color.Lerp(Apagado, Cian, _pip[i]);
                Pips[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.3f, _pip[i]);
            }
        }
    }

    /// <summary>
    /// Una columna de la tabla. Con la luz en 0 esta apagada (todas iguales); al subir, las que este nivel permite
    /// brillan (borde, respiracion del halo, titulo en cian) y sacan su circulo con un rebote; las demas se atenuan.
    /// </summary>
    internal sealed class ColumnaViva : MonoBehaviour {
        public CanvasGroup Grupo, Controles;
        public Image Borde, Halo, Cabecera;
        public TMP_Text Titulo;
        public RectTransform Ficha;
        public bool Disponible;
        public int Orden;
        public Color Cian, BordeBase, TextoBase, CabeceraBase, CabeceraEncendida;
        private float _pulso = 1;

        public void Pulso() { _pulso = 0; }

        private static float RebotaAlLlegar(float t) {
            const float c1 = 1.70158f, c3 = c1 + 1;
            var x = t - 1;
            return 1 + c3 * x * x * x + c1 * x * x;
        }

        public void Fijar(float t, float tiempo) {
            _pulso = Mathf.Min(1, _pulso + Time.unscaledDeltaTime / 0.3f);
            Grupo.alpha = Mathf.Lerp(0.9f, Disponible ? 1f : 0.3f, t);
            if (!Disponible) return;
            Borde.color = Color.Lerp(BordeBase, Cian, t);
            Halo.color = NexusTheme.Alfa(Cian, t * 0.3f * (0.7f + 0.3f * Mathf.Sin(tiempo * 2.2f)));
            Cabecera.color = Color.Lerp(CabeceraBase, CabeceraEncendida, t);
            Titulo.color = Color.Lerp(TextoBase, Cian, t);
            if (Ficha != null) Ficha.localScale = Vector3.one * (RebotaAlLlegar(t) * (1 + 0.25f * Mathf.Sin(Mathf.PI * _pulso)));
            if (Controles != null) { Controles.alpha = t; Controles.blocksRaycasts = t > 0.9f; Controles.interactable = t > 0.9f; }
        }
    }

    /// <summary>Resalte al pasar el raton (color y escala) con avisos de entrada y salida; sirve a casillas, cabeceras y botones.</summary>
    internal sealed class RealceDeCalidad : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        public Action Entra, Sale;
        public Graphic Fondo;
        public Color Normal, Resaltado;
        public float Escala = 1;
        public Button Boton;
        public CanvasGroup Opacidad;
        private float _f;
        private bool _dentro;

        public void OnPointerEnter(PointerEventData e) { _dentro = true; if (Entra != null) Entra(); }
        public void OnPointerExit(PointerEventData e) { Salir(); }
        private void OnDisable() { Salir(); _f = 0; }

        private void Salir() {
            if (!_dentro) return;
            _dentro = false;
            if (Sale != null) Sale();
        }

        private void Update() {
            var activo = Boton == null || Boton.interactable;
            _f = Mathf.MoveTowards(_f, _dentro && activo ? 1 : 0, Time.unscaledDeltaTime / 0.12f);
            if (Fondo != null) Fondo.color = Color.Lerp(Normal, Resaltado, _f);
            if (Escala != 1) transform.localScale = Vector3.one * Mathf.Lerp(1, Escala, _f);
            if (Opacidad != null && Boton != null) Opacidad.alpha = Boton.interactable ? 1 : 0.35f;
        }
    }

    /// <summary>Un pequeño salto de escala (el contador cuando cambia).</summary>
    internal sealed class ReboteDeCalidad : MonoBehaviour {
        private float _t = 1;
        public void Lanzar() { _t = 0; }
        private void Update() {
            if (_t >= 1) return;
            _t = Mathf.Min(1, _t + Time.unscaledDeltaTime / 0.32f);
            transform.localScale = Vector3.one * (_t >= 1 ? 1 : 1 + 0.22f * Mathf.Sin(Mathf.PI * _t));
        }
    }

    /// <summary>
    /// El cuadro que sale al pasar el raton por una casilla: su nombre y que significa. Vive en la raiz de la
    /// pantalla (no dentro del scroll, que lo recortaria), sigue al raton y se queda dentro de la pantalla.
    /// </summary>
    internal sealed class TooltipDeCalidad : MonoBehaviour {
        private RectTransform _rt, _raiz;
        private CanvasGroup _g;
        private TMP_Text _titulo, _cuerpo;
        private float _f;
        private bool _visible;

        public static TooltipDeCalidad Obtener(UiKit ui, RectTransform raiz) {
            var previo = raiz.Find("TooltipCalidad");
            if (previo != null) {
                var viejo = previo.GetComponent<TooltipDeCalidad>();
                if (viejo != null) { previo.SetAsLastSibling(); return viejo; }
            }
            var t = ui.Tema;
            var rt = ui.Columna(raiz, "TooltipCalidad", t.Px(4), t.Px(14), TextAnchor.UpperLeft);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(t.Px(340), 0);
            rt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var fit = rt.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            var fondo = rt.gameObject.AddComponent<Image>();
            ui.Fondo(fondo, NexusTheme.Alfa(t.bg950, 0.97f), NexusTheme.RadioMd);
            fondo.raycastTarget = false;
            ui.Halo(rt, NexusTheme.Alfa(t.cyan, 0.25f), NexusTheme.RadioMd);
            ui.Borde(rt, NexusTheme.Alfa(t.cyan, 0.85f), NexusTheme.RadioMd, 1.5f);
            var g = rt.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0; g.blocksRaycasts = false; g.interactable = false;

            var vista = rt.gameObject.AddComponent<TooltipDeCalidad>();
            vista._rt = rt; vista._raiz = raiz; vista._g = g;
            vista._titulo = ui.Texto(rt, "", EstiloTexto.Encabezado, t.cyan);
            vista._titulo.fontStyle = FontStyles.Bold;
            vista._cuerpo = ui.Texto(rt, "", EstiloTexto.Cuerpo, t.ink);
            vista._cuerpo.fontSize = t.Px(14);
            rt.SetAsLastSibling();
            return vista;
        }

        public void Mostrar(string titulo, string cuerpo, Color acento) {
            _titulo.text = titulo;
            _titulo.color = acento;
            _cuerpo.text = cuerpo;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rt);
            _visible = true;
            _rt.SetAsLastSibling();
            Seguir();
        }

        public void Ocultar() { _visible = false; }
        public void OcultarYa() { _visible = false; _f = 0; if (_g != null) _g.alpha = 0; }

        private void Update() {
            _f = Mathf.MoveTowards(_f, _visible ? 1 : 0, Time.unscaledDeltaTime / 0.12f);
            _g.alpha = _f;
            if (_f <= 0) return;
            _rt.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, _f);
            if (_visible) Seguir();
        }

        private void Seguir() {
            var raton = Mouse.current;
            if (raton == null) { _visible = false; return; }
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_raiz, raton.position.ReadValue(), null, out local)) return;
            var tam = _rt.rect.size;
            var margen = 12f;
            var desplazamiento = 18f;
            var x = local.x + desplazamiento;
            if (x + tam.x > _raiz.rect.xMax - margen) x = local.x - desplazamiento - tam.x;
            var y = local.y - desplazamiento;
            if (y - tam.y < _raiz.rect.yMin + margen) y = local.y + desplazamiento + tam.y;
            _rt.localPosition = new Vector3(x, y, 0);
        }
    }
}
