using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Despliegue;
using Nexus.Unity.Guia;
using Nexus.Unity.Pantallas.Minijuegos.Corregir;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    public sealed partial class PantallaDeDespliegue {
        // ==================================================================== 2 · los cuartos a oscuras

        /// <summary>Un cuarto = un cable de un modulo (o, sin cables, solo la palanca; o, una dependencia, un candado).</summary>
        private sealed class Cuarto {
            public string Modulo;
            public CableDef Cable;
            public List<string> Opciones = new List<string>();
            public bool Dependencia;
            public string Necesita;
            /// <summary>El ultimo cuarto de su modulo: al resolverlo, el modulo se enciende (o no).</summary>
            public bool Fin;
            public int NumeroEnElModulo, TotalDelModulo;
        }

        private sealed class ItemDeSala {
            public string Tipo;            // "opcion", "candado", "palanca"
            public int Lado;               // 0 izquierda, 1 derecha (solo las opciones)
            public string Toma;            // la toma que ofrece (solo las opciones)
            public RectTransform Rt;
            public CanvasGroup Etiquetas;
            public Image Borde;
            public Vector2 Centro;         // en coordenadas de la lamina (origen arriba a la izquierda)
            public Vector2 Enchufe;        // el agujero de la toma, donde acaba el cable (mismas coordenadas)
            public float Intensidad;
            public bool Visto;
        }

        private sealed class Sala {
            public Lamina Lam;
            public RectTransform Raiz, Fondo, Props, Puerta, LuzDePuerta, Palanca, Mango, Cono;
            public Image LamparaDeLaPalanca;
            public DibujoUI Cable, Rayo;
            public AmbienteDeLosCuartos Ambiente;
            public Paleta Paleta;
            public Vector2 Puerto, BaseDeLaPalanca;   // el puerto del tablero (donde nace el cable) y el pie de la palanca
            public Cuarto Cuarto;
            public readonly List<ItemDeSala> Items = new List<ItemDeSala>();
            public ItemDeSala Palanquita;
            public int Elegida = -1;
            public bool Resuelta;
            public Vector2 CableInicio, CableFin;   // el puerto del tablero y el agujero de la toma, en coordenadas de la lamina de los props
        }

        private List<Cuarto> _cuartos = new List<Cuarto>();
        private int _iCuarto;
        private Sala _sala;
        private LinternaView _linterna;
        private RectTransform _salaCont, _chispasCapa, _progreso, _flash;
        private Image _flashImg, _emergencia;
        private float _cw, _ch, _sinLuz;
        private bool _ocupado, _luciernaga;
        private char[] _estadoDeCuartos = new char[0];   // ' ' sin jugar, b bien, m mal enchufado, f no enciende
        private HashSet<string> _encendidos = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _fallaDura = new HashSet<string>(StringComparer.Ordinal);

        private const string TextoOscuro = "Está muy oscuro para tocarlo: ilumínalo primero con la linterna.";

        private void LimpiarCuartos() {
            if (_progreso != null) Destroy(_progreso.gameObject);
            _progreso = null; _sala = null; _linterna = null; _salaCont = null; _chispasCapa = null; _flash = null; _flashImg = null; _emergencia = null;
            _ocupado = false; _luciernaga = false;
        }

        /// <summary>Los modulos de hoy con lo que necesitan antes primero: asi un candado no puede estar cerrado por culpa del orden.</summary>
        private List<string> ModulosEnOrden() {
            var orden = new List<string>();
            var pendientes = _plan.Modulos.ToList();
            var hechos = new HashSet<string>(_plan.YaDesplegados, StringComparer.Ordinal);
            while (pendientes.Count > 0) {
                var siguiente = pendientes.FirstOrDefault(m => _def.CablesDe(m).Where(c => c.EsDependencia)
                                    .All(c => hechos.Contains(c.ModuloNecesario) || !pendientes.Contains(c.ModuloNecesario))) ?? pendientes[0];
                orden.Add(siguiente);
                hechos.Add(siguiente);
                pendientes.Remove(siguiente);
            }
            return orden;
        }

        private void ArmarLosCuartos() {
            _cuartos = new List<Cuarto>();
            foreach (var m in ModulosEnOrden()) {
                var cables = _def.CablesDe(m);
                if (cables.Count == 0) { _cuartos.Add(new Cuarto { Modulo = m, Fin = true, NumeroEnElModulo = 1, TotalDelModulo = 1 }); continue; }
                for (var k = 0; k < cables.Count; k++) {
                    var cable = cables[k];
                    _cuartos.Add(new Cuarto {
                        Modulo = m, Cable = cable, Dependencia = cable.EsDependencia, Necesita = cable.ModuloNecesario,
                        Opciones = _def.OpcionesDeCable(m, cable), Fin = k == cables.Count - 1, NumeroEnElModulo = k + 1, TotalDelModulo = cables.Count
                    });
                }
            }
            _estadoDeCuartos = Enumerable.Repeat(' ', _cuartos.Count).ToArray();
            _encendidos = new HashSet<string>(_plan.YaDesplegados, StringComparer.Ordinal);
            _fallaDura.Clear();
        }

        private void PintarCuartos() {
            _paso = Paso.Cuartos;
            PonerAntetitulo("2 de 2 · los cuartos a oscuras");
            PonerIntentos();
            ArmarLosCuartos();
            _iCuarto = 0; _ocupado = false; _luciernaga = false; _sinLuz = 0;
            var area = UiKit.Rellenar(Ui.Nodo(_escena, "Cuartos"));
            var medido = area.gameObject.AddComponent<HuecoMedido>();
            medido.AlMedirse = (w, h) => { _cw = w; _ch = h; ConstruirLosCuartos(area); };
            GuiaView.Avisar(App, "despliegue.modulos");
        }

        /// <summary>Las capas (la sala, la oscuridad, el destello, las chispas), el indicador de progreso y el primer cuarto.</summary>
        private void ConstruirLosCuartos(RectTransform area) {
            _salaCont = UiKit.Rellenar(Ui.Nodo(area, "Sala"));
            var g = _salaCont.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0;
            _linterna = LinternaView.Crear(Ui, area, 520);
            _flash = UiKit.Rellenar(Ui.Nodo(area, "Destello"));
            _flashImg = _flash.gameObject.AddComponent<Image>();
            _flashImg.color = new Color(1f, 0.86f, 0.55f, 0f);
            _flashImg.raycastTarget = false;
            _chispasCapa = UiKit.Rellenar(Ui.Nodo(area, "Chispas"));
            // El grano de pelicula (por encima de la oscuridad) y la luz roja de emergencia que pulsa al fallar.
            GranoDePelicula.Crear(Ui, area, 0.055f, true);
            var em = UiKit.Rellenar(Ui.Nodo(area, "Emergencia"));
            _emergencia = em.gameObject.AddComponent<Image>();
            _emergencia.color = new Color(1f, 0.1f, 0.08f, 0f);
            _emergencia.raycastTarget = false;

            _progreso = Globo(_hud, "Progreso", false);
            Anclar(_progreso, new Vector2(0, 1), new Vector2(16, -150));
            Crecer(_progreso, true);
            RefrescarProgreso();
            IrAlCuarto(0);
            _anim.Despues(1.3f, AbrirElTutorialDeLosCuartos, _salaCont.gameObject);
        }

        // ---------------------------------------------------------------- el indicador de progreso

        private void RefrescarProgreso() {
            if (_progreso == null || _cuartos.Count == 0) return;
            UiKit.Vaciar(_progreso);
            var c = _cuartos[Mathf.Min(_iCuarto, _cuartos.Count - 1)];
            Ui.Texto(_progreso, ("Módulo " + NombreDe(c.Modulo) + " · cuarto " + c.NumeroEnElModulo + " de " + c.TotalDelModulo).ToUpperInvariant(), EstiloTexto.Leyenda, Tema.cyan);
            var fila = Ui.Fila(_progreso, "Lamparas", 6, 0, TextAnchor.MiddleLeft);
            for (var i = 0; i < _cuartos.Count; i++) {
                var punto = Ui.Nodo(fila, "Lampara " + (i + 1));
                UiKit.Tamano(punto, 22, 22);
                var img = punto.gameObject.AddComponent<Image>();
                img.sprite = Ui.SpriteCircular();
                img.raycastTarget = false;
                var e = _estadoDeCuartos[i];
                img.color = e == 'b' ? Tema.success : e == 'm' ? Tema.warning : e == 'f' ? Tema.danger
                          : i == _iCuarto ? Tema.cyan : NexusTheme.Alfa(Tema.lineStrong, 0.7f);
                if (i == _iCuarto && e == ' ') {
                    var rt = punto;
                    _anim.Lanzar(0.5f, t => { if (rt != null) rt.localScale = Vector3.one * (1f + 0.3f * Curvas.Campana(t)); }, null, 0, punto.gameObject);
                }
            }
        }

        // ---------------------------------------------------------------- un cuarto

        private void IrAlCuarto(int i) {
            _iCuarto = i;
            var c = _cuartos[i];
            _sala = ConstruirSala(c);
            if (_sala.Ambiente != null) _sala.Ambiente.Caminar();
            _linterna.Encendida = true;
            _linterna.Oscuridad = 1;
            _sinLuz = 0; _luciernaga = false; _ocupado = false;
            // Entra desde un poco mas lejos, como si se acabara de cruzar la puerta.
            var g = _salaCont.GetComponent<CanvasGroup>();
            _anim.Lanzar(0.5f, t => {
                if (g == null) return;
                g.alpha = Mathf.Clamp01(t * 1.8f);
                _salaCont.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, Curvas.SalidaAtras(t));
            }, () => { if (g != null) { g.alpha = 1; _salaCont.localScale = Vector3.one; } }, 0, _salaCont.gameObject);
            RefrescarProgreso();
            if (c.Dependencia) Decir("«" + NombreDe(c.Modulo) + "» necesita a «" + NombreDe(c.Necesita) + "». Ilumina el candado y despliega.");
            else if (c.Cable == null) Decir("«" + NombreDe(c.Modulo) + "» no necesita enchufar nada. Ilumina la palanca y despliega.");
            else Decir("«" + NombreDe(c.Modulo) + "»: conecta «" + c.Cable.Nombre.ToLowerInvariant() + "». Ilumina las dos tomas, elige una y despliega.");
        }

        /// <summary>
        /// El cuarto en primera persona, en capas (de atras hacia delante): el fondo (paredes, suelo, techo, puerta y el
        /// tema del modulo), los props (tomas, tablero con la palanca y el cable) y el polvo. Cada capa se desplaza un poco
        /// distinto con el haz (paralaje) y toda la sala se balancea como una camara (AmbienteDeLosCuartos).
        /// El cable vive en la capa de los props, igual que las tomas y el tablero: asi nunca se separan de ellos.
        /// </summary>
        private Sala ConstruirSala(Cuarto c) {
            var s = new Sala { Cuarto = c };
            float w = _cw, h = _ch;
            s.Lam = new Lamina(Ui, _salaCont, w, h);
            s.Raiz = s.Lam.Raiz;
            s.Raiz.GetComponent<LayoutElement>().ignoreLayout = true;
            s.Raiz.anchorMin = s.Raiz.anchorMax = s.Raiz.pivot = new Vector2(0.5f, 0.5f);
            s.Raiz.anchoredPosition = Vector2.zero;
            s.Raiz.sizeDelta = new Vector2(w, h);

            var tema = TemaDelCuarto(c.Modulo);
            var pal = PaletaDe(tema);
            s.Paleta = pal;
            var g = new Geo { W = w, H = h, Fx0 = w * 0.31f, Fx1 = w * 0.69f, Fy0 = h * 0.20f, Fy1 = h * 0.60f };
            float fx0 = g.Fx0, fx1 = g.Fx1, fy0 = g.Fy0, fy1 = g.Fy1;

            // ---- el ambiente (la camara, el paralaje, el polvo, los LEDs y las bombillas)
            s.Ambiente = s.Raiz.gameObject.AddComponent<AmbienteDeLosCuartos>();
            s.Ambiente.Camara = s.Raiz;
            s.Ambiente.Ancho = w; s.Ambiente.Alto = h;
            var linterna = _linterna;
            s.Ambiente.Haz = () => linterna != null ? linterna.Haz : Vector2.zero;

            // ---- el fondo: un poco mas grande que la pantalla, para que el paralaje nunca deje ver el borde
            RectTransform fondoRt;
            var d = CapaFija(s.Raiz, "Fondo", w, h, out fondoRt);
            fondoRt.localScale = Vector3.one * 1.07f;
            s.Fondo = fondoRt;
            s.Ambiente.Fondo = fondoRt;
            RectTransform lucesRt;
            s.Ambiente.Luces = CapaFija(fondoRt, "LEDs del fondo", w, h, out lucesRt);   // se mueve el fondo: los LEDs, con el
            // (los LEDs se dibujan encima del fondo, por eso la capa va despues: se reordena al final)
            DecorarElFondo(s, d, g, pal, tema);
            GranoDePelicula.Crear(Ui, fondoRt, 0.10f, false);

            // ---- la puerta del fondo (se abre al acertar), con la luz que hay detras
            float dw = (fx1 - fx0) * 0.2f, dh = (fy1 - fy0) * 0.82f;
            s.LuzDePuerta = Ui.Nodo(fondoRt, "Luz de la puerta");
            Lamina.Colocar(s.LuzDePuerta, w / 2 - dw / 2, fy1 - dh, dw, dh);
            var luzImg = s.LuzDePuerta.gameObject.AddComponent<Image>();
            luzImg.color = new Color(1f, 0.88f, 0.6f, 0f);
            luzImg.raycastTarget = false;
            s.Puerta = Ui.Nodo(fondoRt, "Puerta");
            Lamina.Colocar(s.Puerta, w / 2 - dw / 2, fy1 - dh, dw, dh);
            var puertaImg = s.Puerta.gameObject.AddComponent<Image>();
            Ui.Fondo(puertaImg, Color.Lerp(new Color(0.07f, 0.12f, 0.18f), pal.Fondo, 0.18f), 6);
            puertaImg.raycastTarget = false;
            Ui.Borde(s.Puerta, NexusTheme.Alfa(pal.Acento, 0.5f), 6, 2);
            DecorarLaPuerta(s, d, g, pal, dw, dh);
            // El cono de luz que sale de la puerta al acertir (invisible hasta entonces).
            RectTransform conoRt;
            var cono = CapaFija(fondoRt, "Cono de luz", w, h, out conoRt);
            cono.Poligono(new List<Vector2> { new Vector2(w / 2 - dw / 2, fy1 - dh), new Vector2(w / 2 + dw / 2, fy1 - dh), new Vector2(w / 2 + dw * 2.9f, h), new Vector2(w / 2 - dw * 2.9f, h) }, new Color(1f, 0.88f, 0.55f, 0.14f));
            cono.Poligono(new List<Vector2> { new Vector2(w / 2 - dw * 0.3f, fy1 - dh * 0.8f), new Vector2(w / 2 + dw * 0.3f, fy1 - dh * 0.8f), new Vector2(w / 2 + dw * 1.3f, h), new Vector2(w / 2 - dw * 1.3f, h) }, new Color(1f, 0.92f, 0.65f, 0.12f));
            var conoGrupo = conoRt.gameObject.AddComponent<CanvasGroup>();
            conoGrupo.alpha = 0; conoGrupo.blocksRaycasts = false;
            s.Cono = conoRt;
            lucesRt.SetAsLastSibling();   // los LEDs encima de lo que decora el fondo (no del cono)
            conoRt.SetAsLastSibling();

            // ---- los props: las tomas (o el candado), el tablero con la palanca y el cable
            RectTransform propsRt;
            var dp = CapaFija(s.Raiz, "Props", w, h, out propsRt);
            s.Props = propsRt;
            s.Ambiente.Props = propsRt;
            RectTransform lucesPropsRt;
            s.Ambiente.LucesProps = CapaFija(propsRt, "LEDs de los props", w, h, out lucesPropsRt);

            float xa = fx0 * 0.12f, xb = fx0 * 0.90f;
            var panel = pal.Panel;
            for (var lado = 0; lado < 2; lado++) {
                Func<float, float> X = x => lado == 0 ? x : w - x;
                var quad = new List<Vector2> { new Vector2(X(xa), g.Y(xa, 0.26f)), new Vector2(X(xb), g.Y(xb, 0.26f)), new Vector2(X(xb), g.Y(xb, 0.72f)), new Vector2(X(xa), g.Y(xa, 0.72f)) };
                dp.Poligono(quad, panel);
                for (var k = 0; k < 4; k++) dp.Linea(quad[k].x, quad[k].y, quad[(k + 1) % 4].x, quad[(k + 1) % 4].y, NexusTheme.Alfa(Tema.ink, 0.35f), 3);
                var cx = quad.Average(p => p.x); var cy = quad.Average(p => p.y);
                var ancho = Mathf.Abs(X(xb) - X(xa)) * 0.94f;
                var alto = ((g.Y(xa, 0.72f) - g.Y(xa, 0.26f)) + (g.Y(xb, 0.72f) - g.Y(xb, 0.26f))) / 2f * 0.9f;
                if (c.Dependencia) { if (lado == 0) s.Items.Add(Candado(s, cx, cy, ancho, alto)); }
                else if (c.Opciones.Count == 2) s.Items.Add(Opcion(s, lado, c.Opciones[lado], cx, cy, ancho, alto, pal));
            }
            lucesPropsRt.SetAsLastSibling();

            s.Palanquita = Palanca(s, w, h, c);
            s.Items.Add(s.Palanquita);

            // El cable que se sostiene (se dibuja al enchufarlo): encima de las tomas y del tablero, en su misma capa.
            s.Cable = CapaFija(propsRt, "Cable", w, h, out lucesPropsRt);
            s.CableInicio = s.Puerto;
            // El rayo de las averias va por encima de la oscuridad (no en la sala).
            s.Rayo = CapaDeDibujo(_chispasCapa, "Rayo");

            // ---- el polvo: bajo la oscuridad, flota solo dentro del haz
            RectTransform polvo = NodoLibre(s.Raiz, "Polvo", w, h);
            s.Ambiente.Polvo = polvo;
            s.Ambiente.PrepararPolvo(Ui, Ui.SpriteCircular(), 46);
            return s;
        }

        private DibujoUI CapaDeDibujo(RectTransform padre, string nombre) {
            var rt = UiKit.Rellenar(Ui.Nodo(padre, nombre));
            rt.gameObject.AddComponent<CanvasRenderer>();
            var d = rt.gameObject.AddComponent<DibujoUI>();
            d.raycastTarget = false;
            return d;
        }

        private ItemDeSala Opcion(Sala s, int lado, string toma, float cx, float cy, float ancho, float alto, Paleta pal) {
            var def = _def.Toma(toma);
            var rt = Ui.Nodo(s.Props, "Toma " + (lado == 0 ? "izquierda" : "derecha"));
            Lamina.Colocar(rt, cx - ancho / 2, cy - alto / 2, ancho, alto);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = true;
            var borde = Ui.Borde(rt, NexusTheme.Alfa(Tema.cyan, 0f), 10, 3.5f);
            var esquina = new Vector2(cx - ancho / 2, cy - alto / 2);
            var item = new ItemDeSala {
                Tipo = "opcion", Lado = lado, Toma = toma, Rt = rt, Borde = borde, Centro = new Vector2(cx, cy),
                Enchufe = new Vector2(cx, esquina.y + alto * 0.26f)   // el agujero que se dibuja en DibujarLaToma
            };
            var boton = rt.gameObject.AddComponent<Button>();
            boton.targetGraphic = img;
            boton.transition = Selectable.Transition.None;
            boton.onClick.AddListener(() => ElegirToma(s, item));

            // La placa y el enchufe, dibujados (siempre ahi) y, solo con luz, su nombre y su entorno.
            var lienzo = UiKit.Rellenar(Ui.Nodo(rt, "Enchufe"));
            lienzo.gameObject.AddComponent<CanvasRenderer>();
            var dib = lienzo.gameObject.AddComponent<DibujoUI>();
            dib.raycastTarget = false;
            DibujarLaToma(s, dib, ancho, alto, esquina, pal);
            var etiquetas = Ui.Nodo(rt, "Etiquetas");
            UiKit.Rellenar(etiquetas);
            item.Etiquetas = etiquetas.gameObject.AddComponent<CanvasGroup>();
            item.Etiquetas.alpha = 0;
            var nombre = Etiqueta(etiquetas, def == null ? toma : def.Nombre, 27, Tema.ink, ancho, alto * 0.42f, alto * 0.38f, true);
            nombre.enableAutoSizing = true; nombre.fontSizeMin = 16; nombre.fontSizeMax = 27;
            var produccion = def == null || def.Entorno == "produccion";
            var tag = Etiqueta(etiquetas, produccion ? "PRODUCCIÓN" : "PRUEBAS", 19, produccion ? Tema.warning : Tema.violet, ancho, 28, alto * 0.74f, true);
            tag.characterSpacing = 4;
            return item;
        }

        private ItemDeSala Candado(Sala s, float cx, float cy, float ancho, float alto) {
            var abierto = _encendidos.Contains(s.Cuarto.Necesita);
            var rt = Ui.Nodo(s.Props, "Candado");
            Lamina.Colocar(rt, cx - ancho / 2, cy - alto / 2, ancho, alto);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = false;
            var item = new ItemDeSala { Tipo = "candado", Rt = rt, Centro = new Vector2(cx, cy) };
            var lienzo = UiKit.Rellenar(Ui.Nodo(rt, "Candado dibujado"));
            lienzo.gameObject.AddComponent<CanvasRenderer>();
            var dib = lienzo.gameObject.AddComponent<DibujoUI>();
            dib.raycastTarget = false;
            var col = abierto ? Tema.success : Tema.danger;
            // La misma placa metalica de las tomas, con sus tornillos.
            dib.Rect(4, 4, ancho - 8, alto - 8, NexusTheme.Alfa(Tema.ink, 0.5f), 2.5f, Rgb(0.20f, 0.26f, 0.33f, 0.95f), false, 12);
            foreach (var q in new[] { new Vector2(16, 16), new Vector2(ancho - 16, 16), new Vector2(16, alto - 16), new Vector2(ancho - 16, alto - 16) }) {
                dib.Circulo(q.x, q.y, 4.5f, NexusTheme.Alfa(Color.black, 0.6f), 1.5f, NexusTheme.Alfa(Tema.ink, 0.55f));
                dib.Linea(q.x - 2.8f, q.y - 1.5f, q.x + 2.8f, q.y + 1.5f, NexusTheme.Alfa(Color.black, 0.7f), 1.4f);
            }
            var mx = ancho / 2; var top = alto * 0.12f; var cw = Mathf.Min(ancho, alto) * 0.34f;
            dib.Rect(mx - cw / 2, top + cw * 0.55f, cw, cw * 0.8f, col, 4, NexusTheme.Alfa(col, 0.18f), false, 6);
            if (abierto) dib.Curva(new Vector2(mx - cw * 0.3f, top + cw * 0.55f), new Vector2(mx - cw * 0.3f, top - cw * 0.2f), new Vector2(mx + cw * 0.05f, top - cw * 0.05f), col, 4, false);
            else dib.Curva(new Vector2(mx - cw * 0.3f, top + cw * 0.55f), new Vector2(mx, top - cw * 0.35f), new Vector2(mx + cw * 0.3f, top + cw * 0.55f), col, 4, false);
            dib.Circulo(mx, top + cw * 0.95f, 5, col, 0, col);
            dib.Rect(ancho * 0.06f, alto * 0.50f, ancho * 0.88f, alto * 0.42f, NexusTheme.Alfa(Tema.ink, 0.28f), 1.5f, NexusTheme.Alfa(Color.black, 0.32f), false, 6);
            var etiquetas = Ui.Nodo(rt, "Etiquetas");
            UiKit.Rellenar(etiquetas);
            item.Etiquetas = etiquetas.gameObject.AddComponent<CanvasGroup>();
            item.Etiquetas.alpha = 0;
            var nombre = Etiqueta(etiquetas, "Necesita «" + NombreDe(s.Cuarto.Necesita) + "»", 25, Tema.ink, ancho, alto * 0.3f, alto * 0.56f, true);
            nombre.enableAutoSizing = true; nombre.fontSizeMin = 15; nombre.fontSizeMax = 25;
            var estado = Etiqueta(etiquetas, abierto ? "ENCENDIDO" : "APAGADO", 20, col, ancho, 28, alto * 0.82f, true);
            estado.characterSpacing = 4;
            return item;
        }

        /// <summary>El tablero del fondo: «DESPLEGAR», la palanca que enciende el modulo y el puerto de donde sale el cable.</summary>
        private ItemDeSala Palanca(Sala s, float w, float h, Cuarto c) {
            float bw = 380, bh = 132;
            var rt = Ui.Nodo(s.Props, "Tablero de despliegue");
            Lamina.Colocar(rt, w / 2 - bw / 2, h * 0.725f, bw, bh);
            var fondo = rt.gameObject.AddComponent<Image>();
            Ui.Fondo(fondo, new Color(0.11f, 0.17f, 0.24f), 14);
            fondo.raycastTarget = true;
            var borde = Ui.Borde(rt, Tema.warning, 14, 3);
            var item = new ItemDeSala { Tipo = "palanca", Rt = rt, Borde = borde, Centro = new Vector2(w / 2, h * 0.725f + bh / 2) };
            var boton = rt.gameObject.AddComponent<Button>();
            boton.targetGraphic = fondo;
            boton.transition = Selectable.Transition.None;
            boton.onClick.AddListener(() => TirarPalanca(s));
            // Donde nace el cable (el puerto del borde de arriba) y el pie de la palanca, en coordenadas de la lamina.
            s.Puerto = new Vector2(w / 2, h * 0.725f);
            s.BaseDeLaPalanca = new Vector2(w / 2 - bw / 2 + bw - 35, h * 0.725f + bh - 22);

            var detalle = UiKit.Rellenar(Ui.Nodo(rt, "Detalle del tablero"));
            detalle.gameObject.AddComponent<CanvasRenderer>();
            var dt = detalle.gameObject.AddComponent<DibujoUI>();
            dt.raycastTarget = false;
            DibujarElTablero(dt, bw, bh);

            var etiquetas = Ui.Nodo(rt, "Etiquetas");
            UiKit.Rellenar(etiquetas);
            item.Etiquetas = etiquetas.gameObject.AddComponent<CanvasGroup>();
            item.Etiquetas.alpha = 0;
            var titulo = Etiqueta(etiquetas, "DESPLEGAR", 28, Tema.warning, 230, 36, 22, false);
            titulo.fontStyle = FontStyles.Bold; titulo.characterSpacing = 4;
            var modulo = Etiqueta(etiquetas, "«" + NombreDe(c.Modulo) + "»", 20, Tema.ink, 200, 52, 64, false);
            modulo.enableAutoSizing = true; modulo.fontSizeMin = 13; modulo.fontSizeMax = 20;
            Lamina.Colocar(titulo.rectTransform, 22, 20, 230, 36);
            Lamina.Colocar(modulo.rectTransform, 22, 62, 190, 52);

            // La lampara (se enciende en verde o en rojo) y la palanca, que baja.
            var lampara = Ui.Nodo(rt, "Lampara");
            Lamina.Colocar(lampara, bw - 46, 14, 22, 22);
            s.LamparaDeLaPalanca = lampara.gameObject.AddComponent<Image>();
            s.LamparaDeLaPalanca.sprite = Ui.SpriteCircular();
            s.LamparaDeLaPalanca.color = NexusTheme.Alfa(Tema.lineStrong, 0.8f);
            s.LamparaDeLaPalanca.raycastTarget = false;
            var base_ = Ui.Nodo(rt, "Base de la palanca");
            Lamina.Colocar(base_, bw - 64, bh - 34, 58, 20);
            var baseImg = base_.gameObject.AddComponent<Image>();
            Ui.Fondo(baseImg, new Color(0.06f, 0.1f, 0.15f), 8);
            baseImg.raycastTarget = false;
            s.Palanca = Ui.Nodo(rt, "Palanca");
            s.Palanca.anchorMin = s.Palanca.anchorMax = new Vector2(0, 1);
            s.Palanca.pivot = new Vector2(0.5f, 0f);
            s.Palanca.sizeDelta = new Vector2(12, 76);
            s.Palanca.anchoredPosition = new Vector2(bw - 35, -(bh - 22));
            s.Palanca.localEulerAngles = new Vector3(0, 0, 38f);
            var barra = s.Palanca.gameObject.AddComponent<Image>();
            barra.color = Tema.ink;
            barra.raycastTarget = false;
            s.Mango = Ui.Nodo(s.Palanca, "Mango");
            s.Mango.anchorMin = s.Mango.anchorMax = new Vector2(0.5f, 1f);
            s.Mango.pivot = new Vector2(0.5f, 0.5f);
            s.Mango.sizeDelta = new Vector2(30, 30);
            s.Mango.anchoredPosition = Vector2.zero;
            var mango = s.Mango.gameObject.AddComponent<Image>();
            mango.sprite = Ui.SpriteCircular();
            mango.color = Tema.warning;
            mango.raycastTarget = false;
            return item;
        }

        private TMP_Text Etiqueta(RectTransform padre, string texto, float tam, Color color, float ancho, float alto, float y, bool centradoEnElPanel) {
            var t = Ui.Texto(padre, texto, EstiloTexto.Cuerpo, color, TextAlignmentOptions.Center);
            var at = t.GetComponent<AltoDeTexto>();
            if (at != null) Destroy(at);
            t.fontSize = tam;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            if (centradoEnElPanel) { Lamina.Colocar(t.rectTransform, 0, y, ancho, alto); }
            else { Lamina.Colocar(t.rectTransform, 14, y, ancho, alto); t.alignment = TextAlignmentOptions.MidlineLeft; }
            return t;
        }

        // ---------------------------------------------------------------- la luz: que se ve y que se puede tocar

        /// <summary>Cuanta luz le llega a cada cosa: revela sus etiquetas, habilita los clics y avisa si el jugador se pierde.</summary>
        private void ActualizarLuzDeLosCuartos() {
            if (_sala == null || _linterna == null) return;
            var maxI = 0f;
            foreach (var it in _sala.Items) {
                var local = new Vector2(it.Centro.x - _cw / 2f, _ch / 2f - it.Centro.y);
                it.Intensidad = _linterna.Intensidad(local);
                if (it.Intensidad > 0.5f) it.Visto = true;
                if (it.Etiquetas != null) it.Etiquetas.alpha = Mathf.Clamp01((it.Intensidad - 0.12f) / 0.5f);
                maxI = Mathf.Max(maxI, it.Intensidad);
            }
            if (maxI > 0.2f || _sala.Resuelta || _ocupado || _tutorialAbierto) { _sinLuz = 0; return; }
            _sinLuz += Time.unscaledDeltaTime;
            // Si lleva rato sin iluminar nada, una luciernaga flota hacia lo que aun no ha visto.
            if (_sinLuz > 8f && !_luciernaga) Luciernaga();
        }

        private bool Iluminado(ItemDeSala it) { return it.Intensidad > 0.3f; }

        private void Luciernaga() {
            if (_sala == null) return;
            var objetivo = _sala.Items.FirstOrDefault(i => !i.Visto);
            if (objetivo == null) return;
            _luciernaga = true;
            var rt = Ui.Nodo(_chispasCapa, "Luciernaga");
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(46, 46);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = PlanoIlustrado.Brillo();
            img.color = new Color(1f, 0.9f, 0.55f, 0f);
            img.raycastTarget = false;
            var desde = _linterna.Haz;
            var hasta = new Vector2(objetivo.Centro.x - _cw / 2f, _ch / 2f - objetivo.Centro.y);
            _anim.Lanzar(3.2f, t => {
                if (rt == null) return;
                var e = Curvas.EntradaSalida(t);
                var p = Vector2.Lerp(desde, hasta, e) + new Vector2(Mathf.Sin(t * 14f) * 14f, Mathf.Cos(t * 11f) * 10f) * (1 - t);
                rt.anchoredPosition = p;
                img.color = new Color(1f, 0.9f, 0.55f, Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 0.9f);
            }, () => { if (rt != null) Destroy(rt.gameObject); _sinLuz = 0; _luciernaga = false; }, 0, rt.gameObject);
        }

        // ---------------------------------------------------------------- elegir una toma y bajar la palanca

        private void ElegirToma(Sala s, ItemDeSala it) {
            if (_ocupado || s.Resuelta || _tutorialAbierto || _transicionando || s != _sala) return;
            if (!Iluminado(it)) { Decir(TextoOscuro); return; }
            s.Elegida = it.Lado;
            foreach (var o in s.Items.Where(x => x.Tipo == "opcion")) o.Borde.color = o == it ? Tema.cyan : NexusTheme.Alfa(Tema.cyan, 0f);
            Enchufar(s, it, Tema.cyan);
            var def = _def.Toma(it.Toma);
            Decir("Enchufado en «" + (def == null ? it.Toma : def.Nombre) + "». Si estás seguro, baja la palanca.");
        }

        /// <summary>
        /// El cable sale del puerto del tablero y llega al agujero de la toma (los dos puntos reales, en la lamina de los
        /// props: la misma capa que lleva las tomas y el tablero, asi se mueven juntos), con comba, y encaja con un saltito.
        /// </summary>
        private void Enchufar(Sala s, ItemDeSala it, Color color) {
            s.CableInicio = s.Puerto;
            s.CableFin = it.Enchufe;
            var a = s.CableInicio; var b = s.CableFin;
            var control = ControlDelCable(a, b);
            _anim.Lanzar(0.5f, t => {
                if (s.Cable == null) return;
                DibujarCable(s, a, control, b, Curvas.EntradaSalida(t), color);
            }, () => { if (s.Cable != null) DibujarCable(s, a, control, b, 1f, color); }, 0, s.Raiz.gameObject);
        }

        private Vector2 ControlDelCable(Vector2 a, Vector2 b) {
            return new Vector2((a.x + b.x) / 2f, Mathf.Max(a.y, b.y) + _ch * 0.07f);
        }

        private void DibujarCable(Sala s, Vector2 a, Vector2 control, Vector2 b, float hasta, Color color) {
            s.Cable.Limpiar();
            var pts = new List<Vector2>();
            var n = Mathf.Max(2, Mathf.CeilToInt(26 * hasta));
            for (var i = 0; i <= n; i++) {
                var t = hasta * i / n;
                pts.Add(Vector2.Lerp(Vector2.Lerp(a, control, t), Vector2.Lerp(control, b, t), t));
            }
            s.Cable.Polilinea(pts, NexusTheme.Alfa(color, 0.28f), 16);
            s.Cable.Polilinea(pts, Color.Lerp(color, Color.black, 0.5f), 9);
            s.Cable.Polilinea(pts, color, 5);
            // La punta: un enchufe pequeño que tapa el agujero (y el puerto del tablero, al otro lado).
            var punta = pts[pts.Count - 1];
            s.Cable.Circulo(a.x, a.y, 8, NexusTheme.Alfa(Tema.ink, 0.9f), 2.5f, color);
            if (hasta >= 0.999f) {
                s.Cable.Rect(punta.x - 10, punta.y - 12, 20, 24, Tema.ink, 2.5f, Color.Lerp(color, Color.black, 0.35f), false, 4);
                s.Cable.Circulo(punta.x, punta.y, 3.2f, Color.black, 0, Color.black);
            } else {
                s.Cable.Circulo(punta.x, punta.y, 9, color, 3, NexusTheme.Alfa(Tema.bg950, 0.9f));
            }
        }

        private void TirarPalanca(Sala s) {
            if (_ocupado || s.Resuelta || _tutorialAbierto || _transicionando || s != _sala) return;
            if (!Iluminado(s.Palanquita)) { Decir(TextoOscuro); return; }
            var c = s.Cuarto;
            if (!c.Dependencia && c.Cable != null && s.Elegida < 0) {
                Decir("Primero enchufa el cable en una de las dos tomas.");
                Sacudir(s.Palanquita.Rt);
                return;
            }
            _ocupado = true;
            s.Resuelta = true;
            // La palanca baja.
            _anim.Lanzar(0.28f, t => { if (s.Palanca != null) s.Palanca.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(38f, -38f, Curvas.SalidaCubica(t))); },
                         () => Resolver(s), 0, s.Raiz.gameObject);
        }

        /// <summary>Que paso al bajar la palanca: lo que dicen las reglas, el porque de cada cable y el efecto en el cuarto.</summary>
        private void Resolver(Sala s) {
            var c = s.Cuarto;
            bool ok; string porque; var estado = 'b';
            string tomaElegida = null;
            if (c.Dependencia) {
                ok = _encendidos.Contains(c.Necesita);
                porque = ok ? "«" + NombreDe(c.Necesita) + "» ya estaba encendido. " + c.Cable.Porque
                            : "No enciende: «" + NombreDe(c.Necesita) + "» no está desplegado. " + c.Cable.Porque;
                if (!ok) { estado = 'f'; _fallaDura.Add(c.Modulo); }
            } else if (c.Cable == null) {
                ok = true;
                porque = "«" + NombreDe(c.Modulo) + "» no necesitaba enchufar nada: enciende.";
            } else {
                tomaElegida = c.Opciones[s.Elegida];
                ok = tomaElegida == c.Cable.Toma;
                var dondeFue = _def.Toma(tomaElegida);
                porque = ok ? c.Cable.Porque : (dondeFue != null && !string.IsNullOrEmpty(dondeFue.Porque) ? dondeFue.Porque : c.Cable.Porque);
                if (!_enchufes.ContainsKey(c.Modulo)) _enchufes[c.Modulo] = new Dictionary<string, string>(StringComparer.Ordinal);
                _enchufes[c.Modulo][c.Cable.Id] = tomaElegida;
                if (!ok) {
                    var trampa = dondeFue != null && dondeFue.Trampa;
                    estado = trampa ? 'm' : 'f';
                    if (!trampa) _fallaDura.Add(c.Modulo);
                }
            }
            _estadoDeCuartos[_iCuarto] = ok ? 'b' : estado;
            // Un modulo se enciende si ninguno de sus cuartos fallo del todo (igual que en EvaluadorDeDespliegue).
            if (c.Fin && !_fallaDura.Contains(c.Modulo)) _encendidos.Add(c.Modulo);
            RefrescarProgreso();
            if (ok) Exito(s, porque); else Fallo(s, porque);
        }

        /// <summary>Acierto: se hace la luz (el negro se disipa con un destello calido), el cable brilla y la puerta se abre.</summary>
        private void Exito(Sala s, string porque) {
            Decir("✔ " + porque);
            s.LamparaDeLaPalanca.color = Tema.success;
            var elegido = s.Items.FirstOrDefault(i => i.Tipo == "opcion" && i.Lado == s.Elegida);
            if (elegido != null && s.Cable != null) DibujarCable(s, s.CableInicio, ControlDelCable(s.CableInicio, s.CableFin), s.CableFin, 1f, Tema.success);
            var luz = s.LuzDePuerta.GetComponent<Image>();
            var puerta = s.Puerta;
            var flash = _flashImg;
            _anim.Lanzar(0.7f, t => {
                _linterna.Oscuridad = 1f - Curvas.SalidaCubica(t);
                if (flash != null) flash.color = new Color(1f, 0.86f, 0.55f, 0.5f * Curvas.Campana(t));
            }, null, 0, s.Raiz.gameObject);
            _anim.Lanzar(0.8f, t => {
                if (puerta == null) return;
                puerta.localScale = new Vector3(Mathf.Lerp(1f, 0.06f, Curvas.EntradaSalida(t)), 1, 1);
                luz.color = new Color(1f, 0.88f, 0.6f, Mathf.Clamp01(t * 1.4f));
            }, null, 0.25f, s.Raiz.gameObject);
            // El cono de luz que sale de la puerta que se abre.
            var cono = s.Cono != null ? s.Cono.GetComponent<CanvasGroup>() : null;
            if (cono != null) _anim.Lanzar(0.9f, t => { if (cono != null) cono.alpha = Curvas.SalidaCubica(t); }, null, 0.3f, s.Raiz.gameObject);
            var p = AChispas(s, s.BaseDeLaPalanca);
            Chispas(_chispasCapa, p, Tema.success, 18, 110, false);
            Chispas(_chispasCapa, p, Tema.warning, 10, 150, false);
            _anim.Despues(1.9f, () => Avanzar(true), s.Raiz.gameObject);
        }

        /// <summary>
        /// Un punto de la lamina de los props, llevado al espacio de las chispas (que esta por encima de la oscuridad y no
        /// se balancea). Se vuelve a calcular en cada fotograma, asi el arco sigue a la sala aunque se mueva.
        /// </summary>
        private Vector2 AChispas(Sala s, Vector2 p) {
            if (s.Props == null || _chispasCapa == null) return new Vector2(p.x - _cw / 2f, _ch / 2f - p.y);
            var mundo = s.Props.TransformPoint(new Vector3(p.x - _cw / 2f, _ch / 2f - p.y, 0f));
            var l = _chispasCapa.InverseTransformPoint(mundo);
            return new Vector2(l.x, l.y);
        }

        /// <summary>
        /// Fallo: salta una chispa del interruptor a la otra toma (un arco que titila), el haz se cierra y el cuarto se
        /// queda totalmente a oscuras, con las chispas encendidas y una luz roja de emergencia que pulsa. Aqui no se
        /// repite: se sigue.
        /// </summary>
        private void Fallo(Sala s, string porque) {
            Decir("✖ " + porque);
            s.LamparaDeLaPalanca.color = Tema.danger;
            _linterna.Parpadear();
            _linterna.Encendida = false;
            // Hacia la otra opcion (la que tocaba) o, sin opciones, hacia el candado.
            var destinoItem = s.Items.FirstOrDefault(i => i.Tipo == "opcion" && i.Toma == s.Cuarto.Cable?.Toma) ?? s.Items.FirstOrDefault(i => i.Tipo == "candado");
            var origenP = s.BaseDeLaPalanca;
            var destinoP = destinoItem != null ? (destinoItem.Tipo == "opcion" ? destinoItem.Enchufe : destinoItem.Centro) : new Vector2(_cw / 2f, _ch * 0.4f);
            var rayo = s.Rayo;
            _anim.Lanzar(1.5f, t => {
                if (rayo == null) return;
                rayo.Limpiar();
                if (t > 0.9f) return;
                if (UnityEngine.Random.value < 0.25f) return;   // titila
                var o = AChispas(s, origenP); var dd = AChispas(s, destinoP);
                var pts = new List<Vector2>();
                const int n = 12;
                var amp = 34f * (1f - t * 0.7f);
                for (var i = 0; i <= n; i++) {
                    var q = Vector2.Lerp(o, dd, i / (float)n);
                    if (i > 0 && i < n) q += new Vector2(UnityEngine.Random.Range(-amp, amp), UnityEngine.Random.Range(-amp, amp));
                    pts.Add(new Vector2(q.x + _cw / 2f, _ch / 2f - q.y));   // el rayo se dibuja con el origen arriba a la izquierda
                }
                rayo.Polilinea(pts, NexusTheme.Alfa(Tema.warning, 0.35f), 16);
                rayo.Polilinea(pts, Color.white, 4);
            }, () => { if (rayo != null) rayo.Limpiar(); }, 0, s.Raiz.gameObject);
            var po = AChispas(s, origenP);
            var pd = AChispas(s, destinoP);
            Chispas(_chispasCapa, po, Tema.warning, 20, 120, false);
            Chispas(_chispasCapa, pd, Tema.danger, 14, 90, false);
            _anim.Despues(0.35f, () => Chispas(_chispasCapa, pd, Color.white, 10, 70, false), s.Raiz.gameObject);
            _anim.Despues(0.7f, () => Chispas(_chispasCapa, po, Tema.danger, 12, 100, false), s.Raiz.gameObject);
            // La luz roja de emergencia: pulsa tres veces y se apaga.
            var em = _emergencia;
            if (em != null) _anim.Lanzar(1.5f, t => { if (em != null) em.color = new Color(1f, 0.08f, 0.06f, 0.24f * Mathf.Max(0f, Mathf.Sin(t * Mathf.PI * 3f)) * (1f - t)); },
                                         () => { if (em != null) em.color = new Color(1f, 0.08f, 0.06f, 0f); }, 0.05f, s.Raiz.gameObject);
            // Un temblor de la sala, como un cortocircuito.
            var raiz = _salaCont;
            _anim.Lanzar(0.45f, t => { if (raiz != null) raiz.anchoredPosition = new Vector2(Mathf.Sin(t * 70f) * 10f * (1 - t), Mathf.Cos(t * 53f) * 6f * (1 - t)); },
                         () => { if (raiz != null) raiz.anchoredPosition = Vector2.zero; }, 0, s.Raiz.gameObject);
            _anim.Despues(2.3f, () => Avanzar(false), s.Raiz.gameObject);
        }

        /// <summary>La camara avanza hacia el fondo del cuarto y se entra en el siguiente, otra vez a oscuras.</summary>
        private void Avanzar(bool bien) {
            if (_sala == null) return;
            var siguiente = _iCuarto + 1;
            if (siguiente >= _cuartos.Count) { Entregar(false); return; }
            var g = _salaCont.GetComponent<CanvasGroup>();
            _anim.Lanzar(0.55f, t => {
                if (g == null) return;
                _salaCont.localScale = Vector3.one * Mathf.Lerp(1f, 1.55f, Curvas.EntradaSalida(t));
                g.alpha = 1f - t;
                if (bien) _linterna.Oscuridad = Mathf.Lerp(0f, 1f, t);
            }, () => {
                if (_sala == null) return;
                Destroy(_sala.Raiz.gameObject);
                if (_sala.Rayo != null) Destroy(_sala.Rayo.gameObject);
                for (var i = _chispasCapa.childCount - 1; i >= 0; i--) Destroy(_chispasCapa.GetChild(i).gameObject);   // chispas y luciernagas que quedaran sueltas
                IrAlCuarto(siguiente);
            }, 0, _salaCont.gameObject);
        }

        private void AbrirElTutorialDeLosCuartos() {
            if (_sala == null) return;
            TutorialDelPaso("despliegue-cuartos", new List<PasoDeTutorial> {
                new PasoDeTutorial { Texto = "Aquí todo está a oscuras. Mueve la linterna con el ratón (o con las flechas) para ver lo que hay.", Objetivo = null },
                new PasoDeTutorial { Texto = "En cada cuarto hay dos tomas, una en cada pared. Ilumínalas para leer cuál es la de producción y pulsa la que creas correcta.", Objetivo = null },
                new PasoDeTutorial { Texto = "Después baja la palanca «DESPLEGAR» del tablero de abajo. Si acertaste, se hace la luz; si no, saltan chispas, el cuarto se queda a oscuras y sigues.",
                                     Objetivo = () => _sala != null ? _sala.Palanquita.Rt : null }
            });
        }
    }
}
