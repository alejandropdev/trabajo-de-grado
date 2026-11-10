using System;
using System.Collections.Generic;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Corregir;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos.Corregir {
    /// <summary>
    /// El globo del cierre (2026-11-09, ronda 3): en vez de una columna de texto a la derecha, un globo de cristal pegado
    /// al marcador del punto que se mira, con casi nada de texto:
    ///   · dos MINI-CARTAS unidas por una flecha: lo que pusiste → lo que tocaba (con «Está bien así» tambien como carta;
    ///     si acertaste, una sola con su √);
    ///   · el nombre de lo que habia (en el color del veredicto) y su definicion en una linea;
    ///   · «Ver por qué ›», que cambia las cartas por la explicacion completa (una vista a la vez, sin scroll).
    /// Se coloca a la derecha, la izquierda, debajo o encima del marcador, donde quepa y sin tapar la pieza del punto.
    /// Vive en una capa FIJA encima del lienzo (no se agranda con el zoom) y sigue al marcador cuando el lienzo se
    /// acerca o se arrastra: sus coordenadas son las de esa capa (origen en el centro, y hacia arriba).
    /// </summary>
    public sealed class GloboDeRevision : MonoBehaviour {
        public const float Ancho = 740, Alto = 236;
        private const float AnchoMini = 128, AltoMini = 176, Relleno = 18;

        private UiKit _ui;
        private NexusTheme _tema;
        private RectTransform _capa;
        private float _margenArriba, _margenAbajo;
        private Func<Vector2> _ancla;
        private Func<Rect> _pieza;
        private Animador _anim;
        private MinijuegoDef _def;
        private RectTransform _caja, _enlace;
        private RevisionDePunto _rev;
        private Color _acento;

        /// <summary>'capa': donde flota el globo. Los margenes son lo que ocupan arriba y abajo los globos de la pantalla (no se tapan).</summary>
        public static GloboDeRevision Crear(UiKit ui, RectTransform capa, Animador anim, MinijuegoDef def, float margenArriba, float margenAbajo) {
            var g = capa.gameObject.AddComponent<GloboDeRevision>();
            g._ui = ui; g._tema = ui.Tema; g._capa = capa; g._anim = anim; g._def = def;
            g._margenArriba = margenArriba; g._margenAbajo = margenAbajo;
            return g;
        }

        public void Cerrar() {
            if (_caja != null) Destroy(_caja.gameObject);
            if (_enlace != null) Destroy(_enlace.gameObject);
            _caja = null; _enlace = null; _ancla = null; _pieza = null;
        }

        /// <summary>
        /// Abre el globo de 'r' junto al marcador. 'ancla' da el centro del marcador y 'pieza' el rectangulo de su pieza (que no
        /// se tapa), los dos en coordenadas de la capa y pedidos de nuevo en cada fotograma: el lienzo se mueve.
        /// </summary>
        public void Mostrar(RevisionDePunto r, Func<Vector2> ancla, Func<Rect> pieza, bool animar) {
            Cerrar();
            _rev = r;
            _ancla = ancla; _pieza = pieza;
            _acento = r.Acierta ? _tema.success : _tema.danger;

            _enlace = _ui.Nodo(_capa, "Hilo");
            _enlace.anchorMin = _enlace.anchorMax = _enlace.pivot = new Vector2(0.5f, 0.5f);
            var hilo = _enlace.gameObject.AddComponent<Image>();
            hilo.color = NexusTheme.Alfa(_acento, 0.85f);
            hilo.raycastTarget = false;

            _caja = _ui.Nodo(_capa, "Globo de revision");
            _caja.anchorMin = _caja.anchorMax = _caja.pivot = new Vector2(0.5f, 0.5f);
            _caja.sizeDelta = new Vector2(Ancho, Alto);
            var fondo = _caja.gameObject.AddComponent<Image>();
            _ui.Fondo(fondo, NexusTheme.Alfa(_tema.bg950, 0.97f), NexusTheme.RadioLg);
            _ui.Halo(_caja, NexusTheme.Alfa(_acento, 0.3f), NexusTheme.RadioLg);
            _ui.Borde(_caja, NexusTheme.Alfa(_acento, 0.9f), NexusTheme.RadioLg, 2);

            Contenido(false, animar);
            Colocar();
            if (!animar) return;
            var g = _caja.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0;
            var caja = _caja;
            _caja.localScale = Vector3.one * 0.8f;
            _anim.Lanzar(0.3f, t => {
                if (caja == null) return;
                caja.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, Curvas.SalidaAtras(t));
                g.alpha = Mathf.Clamp01(t * 3f);
            }, null, 0, caja.gameObject);
        }

        private void LateUpdate() { if (_caja != null) Colocar(); }

        /// <summary>Elige donde va (derecha, izquierda, debajo o encima del marcador; sin tapar su pieza) y tiende el hilo.</summary>
        private void Colocar() {
            if (_ancla == null || _pieza == null) return;
            var ancla = _ancla();
            var p = _pieza();
            var libre = new Rect(p.x - 10, p.y - 10, p.width + 20, p.height + 20);
            var area = _capa.rect;
            var zona = Rect.MinMaxRect(area.xMin + 8, area.yMin + _margenAbajo, area.xMax - 8, area.yMax - _margenArriba);
            var candidatos = new[] {
                new Rect(ancla.x + 48, ancla.y - Alto / 2, Ancho, Alto),               // a la derecha
                new Rect(ancla.x - 48 - Ancho, ancla.y - Alto / 2, Ancho, Alto),       // a la izquierda
                new Rect(ancla.x - Ancho / 2, ancla.y - 48 - Alto, Ancho, Alto),       // debajo
                new Rect(ancla.x - Ancho / 2, ancla.y + 48, Ancho, Alto)               // encima
            };
            var elegido = Dentro(candidatos[0], zona);
            foreach (var c in candidatos) {
                var f = Dentro(c, zona);
                if (f.Overlaps(libre) || f.Contains(ancla)) continue;
                elegido = f;
                break;
            }
            _caja.anchoredPosition = elegido.center;
            // El hilo: del marcador al punto mas cercano del globo.
            var union = new Vector2(Mathf.Clamp(ancla.x, elegido.xMin, elegido.xMax), Mathf.Clamp(ancla.y, elegido.yMin, elegido.yMax));
            var largo = Vector2.Distance(ancla, union);
            _enlace.gameObject.SetActive(largo > 4f);
            _enlace.anchoredPosition = (ancla + union) / 2f;
            _enlace.sizeDelta = new Vector2(Mathf.Max(2f, largo), 3);
            _enlace.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(union.y - ancla.y, union.x - ancla.x) * Mathf.Rad2Deg);
        }

        private static Rect Dentro(Rect r, Rect zona) {
            r.x = Mathf.Clamp(r.x, zona.xMin, Mathf.Max(zona.xMin, zona.xMax - r.width));
            r.y = Mathf.Clamp(r.y, zona.yMin, Mathf.Max(zona.yMin, zona.yMax - r.height));
            return r;
        }

        // ==================================================================== el contenido

        /// <summary>La vista de las cartas (con su frase) o la explicacion completa.</summary>
        private void Contenido(bool explicacion, bool animar) {
            for (var i = _caja.childCount - 1; i >= 0; i--) {
                var h = _caja.GetChild(i).gameObject;
                if (h.GetComponent<DecoracionNexus>() != null) continue;   // el borde y el halo del propio globo
                h.SetActive(false);
                Destroy(h);
            }
            var fila = _ui.Fila(_caja, "Contenido", _tema.espacio * 2, Relleno, TextAnchor.MiddleLeft);
            UiKit.Rellenar(fila);
            if (explicacion) VistaDeExplicacion(fila);
            else VistaDeCartas(fila, animar);
        }

        private void VistaDeCartas(RectTransform fila, bool animar) {
            var cartas = _ui.Fila(fila, "Cartas", 10, 0, TextAnchor.MiddleCenter);
            var hijos = new List<RectTransform>();
            if (_rev.Acierta) {
                hijos.Add(MiniCarta(cartas, _rev.CartaJugada == PuntosDelDiagrama.EstaBien ? "BIEN VISTO" : "PUSISTE", _tema.success, _rev.CartaJugada, true));
            } else {
                hijos.Add(MiniCarta(cartas, "PUSISTE", _tema.danger, _rev.CartaJugada, false));
                var flecha = T(cartas, "→", 40, _tema.inkMuted, TextAlignmentOptions.Center);
                UiKit.Tamano(flecha, ancho: 36);
                hijos.Add(flecha.rectTransform);
                hijos.Add(MiniCarta(cartas, "TOCABA", _tema.success, _rev.CartaCorrecta, true));
            }

            var texto = _ui.Columna(fila, "Frase", 6, 0, TextAnchor.MiddleLeft);
            UiKit.Tamano(texto, flexAncho: 1);
            var titular = T(texto, _rev.Titular, 25, _acento, TextAlignmentOptions.TopLeft);
            titular.fontStyle = FontStyles.Bold;
            var def = T(texto, _rev.Definicion, 18, _tema.inkMuted, TextAlignmentOptions.TopLeft);
            def.enableAutoSizing = true; def.fontSizeMin = 14; def.fontSizeMax = 18;
            UiKit.Tamano(def, alto: 74);
            _ui.Boton(texto, "Ver por qué  ›", () => Contenido(true, false), VarianteBoton.Fantasma);

            if (!animar) return;
            // Las cartas entran una tras otra, y la flecha se desliza entre ellas.
            for (var i = 0; i < hijos.Count; i++) {
                var rt = hijos[i];
                var g = rt.gameObject.GetComponent<CanvasGroup>();
                if (g == null) g = rt.gameObject.AddComponent<CanvasGroup>();
                g.alpha = 0;
                var esFlecha = rt.GetComponent<TMP_Text>() != null;
                var esSegunda = i == hijos.Count - 1 && hijos.Count > 1;
                _anim.Lanzar(esSegunda ? 0.4f : 0.32f, t => {
                    if (rt == null) return;
                    g.alpha = Mathf.Clamp01(t * 2.5f);
                    if (esFlecha) rt.localScale = new Vector3(Mathf.Lerp(0.3f, 1f, Curvas.SalidaCubica(t)), 1, 1);
                    else if (esSegunda) rt.localScale = new Vector3(Mathf.Max(0.02f, Curvas.SalidaAtras(t)), 1, 1);   // la que tocaba se «voltea»
                    else rt.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, Curvas.SalidaAtras(t));
                }, null, 0.14f + i * 0.12f, rt.gameObject);
            }
        }

        private void VistaDeExplicacion(RectTransform fila) {
            var col = _ui.Columna(fila, "Explicacion", 8, 0, TextAnchor.UpperLeft);
            UiKit.Tamano(col, flexAncho: 1, flexAlto: 1);
            T(col, "POR QUÉ · " + _rev.Titular.ToUpperInvariant(), 13, _acento, TextAlignmentOptions.TopLeft).characterSpacing = 3;
            var t = T(col, _rev.ExplicacionCompleta, 19, _tema.ink, TextAlignmentOptions.TopLeft);
            t.enableAutoSizing = true; t.fontSizeMin = 13; t.fontSizeMax = 19;
            UiKit.Tamano(t, flexAncho: 1, flexAlto: 1);
            t.maxVisibleCharacters = 0;
            var total = t.text.Length;
            _anim.Lanzar(Mathf.Clamp(total * 0.006f, 0.2f, 0.7f), p => { if (t != null) t.maxVisibleCharacters = Mathf.CeilToInt(total * p); },
                         () => { if (t != null) t.maxVisibleCharacters = 99999; }, 0, t.gameObject);
            _ui.Boton(col, "‹  Volver", () => Contenido(false, false), VarianteBoton.Fantasma);
        }

        // ==================================================================== una mini-carta

        /// <summary>Una carta pequeña: un rotulo («PUSISTE», «TOCABA»), su dibujo y su nombre. 'id' null = sin decidir.</summary>
        private RectTransform MiniCarta(Transform padre, string rotulo, Color color, string id, bool correcta) {
            var vacia = id == null;
            var esEstaBien = id == PuntosDelDiagrama.EstaBien;
            var carta = vacia || esEstaBien ? null : PuntosDelDiagrama.Carta(_def, id);
            var nombre = vacia ? "Sin decidir" : PuntosDelDiagrama.NombreDeCarta(_def, id);
            var icono = vacia ? "?" : esEstaBien ? DibujosDeSoluciones.EstaBien : (carta != null ? carta.Icono : "?");

            var rt = _ui.Nodo(padre, "Mini " + rotulo);
            UiKit.Tamano(rt, ancho: AnchoMini, alto: AltoMini);
            var fondo = rt.gameObject.AddComponent<Image>();
            _ui.Fondo(fondo, _tema.bg900, NexusTheme.RadioMd);
            fondo.raycastTarget = false;
            _ui.Borde(rt, NexusTheme.Alfa(color, 0.95f), NexusTheme.RadioMd, 2);

            var lab = T(rt, rotulo, 12, color, TextAlignmentOptions.Center);
            lab.characterSpacing = 3;
            Lamina.Colocar(lab.rectTransform, 6, 8, AnchoMini - 12, 18);

            var placa = _ui.Nodo(rt, "Dibujo");
            Lamina.Colocar(placa, 14, 30, AnchoMini - 28, 82);
            placa.gameObject.AddComponent<CanvasRenderer>();
            var d = placa.gameObject.AddComponent<DibujoUI>();
            d.raycastTarget = false;
            DibujosDeSoluciones.Dibujar(d, icono, (AnchoMini - 28 - 72) / 2f, 5, 72, vacia ? _tema.inkMuted : _tema.ink, color);

            var nom = T(rt, nombre, 15, vacia ? _tema.inkMuted : _tema.ink, TextAlignmentOptions.Top);
            nom.fontStyle = FontStyles.Bold;
            nom.enableAutoSizing = true; nom.fontSizeMin = 11; nom.fontSizeMax = 15;
            Lamina.Colocar(nom.rectTransform, 8, 116, AnchoMini - 16, 54);

            // El veredicto de la carta, en una esquina: √ si es la buena, × si es la que no servia.
            if (!vacia) {
                var marca = T(rt, correcta ? "√" : "×", 22, correcta ? _tema.success : _tema.danger, TextAlignmentOptions.TopRight);
                marca.fontStyle = FontStyles.Bold;
                Lamina.Colocar(marca.rectTransform, AnchoMini - 34, 4, 28, 26);
            }
            return rt;
        }

        private TMP_Text T(Transform padre, string texto, float tam, Color color, TextAlignmentOptions al) {
            var t = _ui.Texto(padre, texto, EstiloTexto.Cuerpo, color, al);
            var alto = t.GetComponent<AltoDeTexto>();
            if (alto != null && padre.GetComponent<LayoutGroup>() == null) Destroy(alto);   // en una caja fija, sin «alto minimo»
            t.fontSize = tam;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }
    }
}
