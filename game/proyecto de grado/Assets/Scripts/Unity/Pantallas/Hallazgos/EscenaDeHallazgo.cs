using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Coleccion;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El hallazgo de un coleccionable, en escena (2026-11-06). Sustituye al dialogo de texto de antes: cada serie
    /// tiene su propia puesta en escena (Hallazgos.Carta, .Pendrive, .Codigo, .Startup, .Nota, .Advertencia) y aqui solo
    /// vive lo comun: el fondo oscuro con polvo flotando, el rotulo de arriba («NUEVO EN TU DIARIO · Cartas 4/15»), los
    /// botones de abajo y el ritmo (una Secuencia). Un clic salta la animacion a su final; Enter salta y luego sigue;
    /// Escape cierra. El diario la abre otra vez con Repeticion = true.
    /// </summary>
    public sealed class EscenaDeHallazgo : Pantalla, IPointerClickHandler {
        public Coleccionable Coleccionable;
        /// <summary>Abierta desde el diario: no es nuevo, y el boton vuelve al diario.</summary>
        public bool Repeticion;
        public Action AlCerrar;

        public override bool EsModal { get { return true; } }
        public override bool PuedeVolver { get { return false; } }

        private Secuencia _secuencia;
        private RectTransform _escenario;
        private float _sacudida;
        private bool _cerrada;

        protected override void Construir() {
            var fondo = Raiz.gameObject.AddComponent<Image>();
            fondo.color = NexusTheme.Alfa(Tema.bg950, 0.95f);
            var col = Coleccionable;
            var acento = ColorDeSerie(Tema, col.Serie);

            // ---- el entorno comun: un halo del color de la serie y polvo que flota
            var halo = Ui.Nodo(Raiz, "Halo");
            halo.sizeDelta = new Vector2(1500, 1100);
            var imgHalo = halo.gameObject.AddComponent<Image>();
            imgHalo.sprite = PlanoIlustrado.Brillo();
            imgHalo.color = NexusTheme.Alfa(acento, 0.13f);
            imgHalo.raycastTarget = false;
            var polvo = Ui.Nodo(Raiz, "Polvo");
            UiKit.Rellenar(polvo);
            polvo.gameObject.AddComponent<PolvoFlotante>().Configurar(Ui, acento);

            _escenario = Ui.Nodo(Raiz, "Escenario");
            _escenario.sizeDelta = new Vector2(1240, 720);
            _escenario.anchoredPosition = new Vector2(0, 10);

            // ---- arriba: que es y cuantos llevas de la serie
            var encontrados = PantallaDelDiario.Encontrados(App);
            var deLaSerie = App.Catalogo.Coleccionables.Where(c => c.Serie == col.Serie).ToList();
            var llevas = deLaSerie.Count(c => encontrados.Contains(c.Id));
            var rotulo = Ui.Texto(Raiz, (Repeticion ? "DE TU DIARIO" : "NUEVO EN TU DIARIO") + "  ·  " +
                                        PantallaDelDiario.NombreDeSerie(col.Serie, false).ToUpperInvariant() + $"  {llevas}/{deLaSerie.Count}",
                                  EstiloTexto.Leyenda, acento, TextAlignmentOptions.Center);
            var rr = rotulo.rectTransform;
            rr.anchorMin = rr.anchorMax = rr.pivot = new Vector2(0.5f, 1);
            rr.sizeDelta = new Vector2(900, 30);
            rr.anchoredPosition = new Vector2(0, -26);
            rotulo.characterSpacing = 6;

            // ---- abajo: seguir y el diario
            var botones = Ui.Fila(Raiz, "Botones", Tema.espacio, 0, TextAnchor.MiddleCenter);
            botones.anchorMin = botones.anchorMax = botones.pivot = new Vector2(0.5f, 0);
            botones.sizeDelta = new Vector2(700, 60);
            botones.anchoredPosition = new Vector2(0, 24);
            if (Repeticion) {
                Ui.Boton(botones, "Volver al diario (Esc)", Cerrar, VarianteBoton.Primario);
            } else {
                Ui.Boton(botones, "Seguir (Enter)", Cerrar, VarianteBoton.Primario);
                Ui.Boton(botones, "Ver en el diario", () => {
                    Cerrar();
                    App.Router.Apilar<PantallaDelDiario>(p => p.AlCerrar = AlCerrar);
                }, VarianteBoton.Fantasma);
            }
            var pista = Ui.Texto(Raiz, "Clic para saltar la animación", EstiloTexto.Pequeno, Tema.inkFaint, TextAlignmentOptions.Center);
            var rp = pista.rectTransform;
            rp.anchorMin = rp.anchorMax = rp.pivot = new Vector2(0.5f, 0);
            rp.sizeDelta = new Vector2(600, 24);
            rp.anchoredPosition = new Vector2(0, 92);

            // ---- la escena de su serie
            _secuencia = _escenario.gameObject.AddComponent<Secuencia>();
            var ctx = new ContextoDeHallazgo {
                Ui = Ui, Tema = Tema, Escenario = _escenario, Sec = _secuencia, Col = col, Repeticion = Repeticion,
                Encontrados = encontrados, Catalogo = App.Catalogo.Coleccionables, Acento = acento,
                Sacudir = fuerza => _sacudida = Mathf.Max(_sacudida, fuerza)
            };
            switch (col.Serie) {
                case SeriesDeColeccionables.Carta: Hallazgos.Carta(ctx); break;
                case SeriesDeColeccionables.Usb: Hallazgos.Pendrive(ctx); break;
                case SeriesDeColeccionables.Codigo: Hallazgos.Codigo(ctx); break;
                case SeriesDeColeccionables.Startup: Hallazgos.Startup(ctx); break;
                case SeriesDeColeccionables.Nota: Hallazgos.Nota(ctx); break;
                default: Hallazgos.Advertencia(ctx); break;
            }
        }

        public void OnPointerClick(PointerEventData e) {
            if (_secuencia != null && !_secuencia.Terminada) _secuencia.Saltar();
        }

        private void Update() {
            var teclado = Keyboard.current;
            if (teclado != null) {
                // Se cierra en LateUpdate: si no, el Escape global (AppRoot) veria ya el diario debajo y lo cerraria tambien.
                if (teclado.escapeKey.wasPressedThisFrame) { _cerrarAlFinal = true; return; }
                var enter = teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame || teclado.spaceKey.wasPressedThisFrame;
                if (enter) {
                    if (_secuencia != null && !_secuencia.Terminada) _secuencia.Saltar();
                    else { _cerrarAlFinal = true; return; }
                }
            }
            // La sacudida de pantalla (el golpe de una advertencia): se apaga sola.
            if (_escenario != null) {
                _sacudida = Mathf.MoveTowards(_sacudida, 0, Time.unscaledDeltaTime * 40f);
                _escenario.anchoredPosition = new Vector2(0, 10) +
                    (_sacudida > 0.01f ? new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)) * _sacudida : Vector2.zero);
            }
        }

        private bool _cerrarAlFinal;
        private void LateUpdate() { if (_cerrarAlFinal) Cerrar(); }

        private void Cerrar() {
            if (_cerrada) return;
            _cerrada = true;
            App.Router.Volver();
            if (!Repeticion) AlCerrar?.Invoke();
        }

        /// <summary>El color de cada serie: el halo del fondo, el rotulo y lo que la escena quiera usar.</summary>
        public static Color ColorDeSerie(NexusTheme tema, string serie) {
            switch (serie) {
                case SeriesDeColeccionables.Carta: return tema.cyan;
                case SeriesDeColeccionables.Usb: return new Color32(0x6B, 0xF1, 0x8A, 0xFF);
                case SeriesDeColeccionables.Codigo: return tema.success;
                case SeriesDeColeccionables.Startup: return tema.violet;
                case SeriesDeColeccionables.Nota: return PlanoIlustrado.ColorDelDestello;
                default: return tema.danger;
            }
        }

        /// <summary>Motas de polvo que suben despacio: el entorno comun de todas las escenas.</summary>
        private sealed class PolvoFlotante : MonoBehaviour {
            private RectTransform[] _motas;
            private Vector2[] _base;
            private float[] _velocidad;

            public void Configurar(UiKit ui, Color color) {
                var rnd = new System.Random(7);
                _motas = new RectTransform[28];
                _base = new Vector2[_motas.Length];
                _velocidad = new float[_motas.Length];
                for (var i = 0; i < _motas.Length; i++) {
                    var m = ui.Nodo(transform, "Mota");
                    var lado = 2 + rnd.Next(4);
                    m.sizeDelta = new Vector2(lado, lado);
                    var img = m.gameObject.AddComponent<Image>();
                    img.sprite = ui.SpriteCircular();
                    img.color = NexusTheme.Alfa(i % 3 == 0 ? color : Color.white, 0.08f + (float)rnd.NextDouble() * 0.15f);
                    img.raycastTarget = false;
                    _base[i] = new Vector2(rnd.Next(-960, 960), rnd.Next(-540, 540));
                    _velocidad[i] = 6 + (float)rnd.NextDouble() * 14;
                    _motas[i] = m;
                }
            }

            private void Update() {
                if (_motas == null) return;
                var t = Time.unscaledTime;
                for (var i = 0; i < _motas.Length; i++) {
                    var y = Mathf.Repeat(_base[i].y + 540 + t * _velocidad[i], 1080) - 540;
                    _motas[i].anchoredPosition = new Vector2(_base[i].x + Mathf.Sin(t * 0.4f + i) * 18, y);
                }
            }
        }
    }

    /// <summary>Lo que una escena de hallazgo necesita para montarse: donde, con que, y como sacudir la pantalla.</summary>
    public sealed class ContextoDeHallazgo {
        public UiKit Ui;
        public NexusTheme Tema;
        public RectTransform Escenario;
        public Secuencia Sec;
        public Coleccionable Col;
        public bool Repeticion;
        public HashSet<string> Encontrados;
        public List<Coleccionable> Catalogo;
        public Color Acento;
        public Action<float> Sacudir;

        /// <summary>Un nodo centrado en (x, y) (y hacia arriba, desde el centro del padre) con su tamaño.</summary>
        public RectTransform Nodo(Transform padre, string nombre, float x, float y, float w, float h) {
            var rt = Ui.Nodo(padre, nombre);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            return rt;
        }

        /// <summary>Un rectangulo de color (redondeado si 'radio' &gt; 0).</summary>
        public Image Caja(Transform padre, string nombre, float x, float y, float w, float h, Color color, float radio = 0) {
            var rt = Nodo(padre, nombre, x, y, w, h);
            var img = rt.gameObject.AddComponent<Image>();
            if (radio > 0) Ui.Fondo(img, color, radio);
            else img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public Image Circulo(Transform padre, string nombre, float x, float y, float diametro, Color color) {
            var img = Caja(padre, nombre, x, y, diametro, diametro, color);
            img.sprite = Ui.SpriteCircular();
            return img;
        }

        public TMP_Text Texto(Transform padre, string texto, EstiloTexto estilo, Color color, float x, float y, float w, float h,
                              TextAlignmentOptions alineacion = TextAlignmentOptions.Center) {
            var t = Ui.Texto(padre, texto, estilo, color, alineacion);
            var al = t.GetComponent<LayoutElement>();
            if (al != null) al.ignoreLayout = true;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            return t;
        }

        /// <summary>Un grupo para fundir: empieza invisible.</summary>
        public CanvasGroup Grupo(Component c) {
            var g = c.GetComponent<CanvasGroup>();
            if (g == null) g = c.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0;
            g.blocksRaycasts = false;
            return g;
        }

        /// <summary>Una lamina de dibujo (y su tizador) de w×h, centrada en (x, y). El dibujo empieza sin trazar.</summary>
        public Tizador Lamina(Transform padre, float x, float y, float w, float h, out DibujoUI dibujo) {
            var lamina = new Lamina(Ui, padre, w, h, "Dibujo");
            var rt = lamina.Raiz;
            var le = rt.GetComponent<LayoutElement>();
            if (le != null) le.ignoreLayout = true;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            dibujo = lamina.Dibujo;
            return new Tizador(lamina, Ui.FuenteTiza);
        }

        // ---- curvas de animacion
        public static float Suave(float p) { return 1 - Mathf.Pow(1 - Mathf.Clamp01(p), 3); }
        public static float Rebote(float p) {
            p = Mathf.Clamp01(p);
            const float c1 = 1.70158f, c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(p - 1, 3) + c1 * Mathf.Pow(p - 1, 2);
        }
        public static float Caida(float p) { p = Mathf.Clamp01(p); return p * p * p; }

        /// <summary>Escribe un texto letra a letra: 'p' de 0 a 1.</summary>
        public static void Escribir(TMP_Text t, float p) {
            if (p >= 1) { t.maxVisibleCharacters = 99999; return; }
            if (t.textInfo == null || t.textInfo.characterCount == 0) t.ForceMeshUpdate();
            var total = t.textInfo == null ? t.text.Length : Mathf.Max(1, t.textInfo.characterCount);
            t.maxVisibleCharacters = Mathf.FloorToInt(total * Mathf.Clamp01(p));
        }
    }

    /// <summary>
    /// El ritmo de una escena: pasos que empiezan en su segundo y duran lo suyo, mas efectos que siempre corren.
    /// Cada paso se llama con su progreso de 0 a 1 (y con 0 nada mas registrarse, para dejar su estado inicial).
    /// Saltar() lleva todo a su final.
    /// </summary>
    public sealed class Secuencia : MonoBehaviour {
        private sealed class Paso { public float Inicio, Duracion; public Action<float> Hacer; public bool Hecho; }
        private readonly List<Paso> _pasos = new List<Paso>();
        private readonly List<Action<float>> _siempre = new List<Action<float>>();
        private float _t, _fin;

        public bool Terminada { get { return _t >= _fin; } }
        public float Tiempo { get { return _t; } }

        public void En(float inicio, float duracion, Action<float> hacer) {
            var p = new Paso { Inicio = inicio, Duracion = Mathf.Max(0.0001f, duracion), Hacer = hacer };
            _pasos.Add(p);
            _fin = Mathf.Max(_fin, inicio + duracion);
            hacer(0);
        }

        /// <summary>Algo que corre siempre (un cursor que parpadea, un borde que late): recibe el tiempo de la escena.</summary>
        public void Siempre(Action<float> cada) { _siempre.Add(cada); }

        public void Saltar() { _t = Mathf.Max(_t, _fin + 0.01f); }

        private void Update() {
            _t += Time.unscaledDeltaTime;
            foreach (var p in _pasos) {
                if (p.Hecho || _t < p.Inicio) continue;
                var f = Mathf.Clamp01((_t - p.Inicio) / p.Duracion);
                p.Hacer(f);
                if (f >= 1) p.Hecho = true;
            }
            foreach (var s in _siempre) s(_t);
        }
    }

    /// <summary>Las escenas de cada serie (un archivo por serie: Hallazgo.Carta.cs, Hallazgo.Pendrive.cs…).</summary>
    public static partial class Hallazgos { }
}
