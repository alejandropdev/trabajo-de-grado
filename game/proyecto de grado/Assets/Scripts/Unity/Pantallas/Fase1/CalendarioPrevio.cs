using System.Collections.Generic;
using Nexus.Core.Metodologia;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// La vista previa del calendario de una opcion de duracion (2026-11-08): los dias del nivel como una tira a escala,
    /// un bloque por sprint/etapa/revision que entra animado, una banderita en cada dia de despliegue y, debajo, la
    /// frase que lo resume. Se vuelve a pintar al mirar o elegir otra opcion.
    /// </summary>
    public sealed class CalendarioPrevio : MonoBehaviour {
        private UiKit _ui;
        private NexusTheme _t;
        private RectTransform _raiz, _contenido;
        private OpcionDeCalendario _mostrada;

        public static CalendarioPrevio Crear(UiKit ui, Transform padre) {
            var raiz = ui.Nodo(padre, "Calendario previo");
            var c = raiz.gameObject.AddComponent<CalendarioPrevio>();
            c._ui = ui; c._t = ui.Tema; c._raiz = raiz;
            var fondo = raiz.gameObject.AddComponent<Image>();
            ui.Fondo(fondo, NexusTheme.Alfa(c._t.bg950, 0.6f), NexusTheme.RadioLg);
            fondo.raycastTarget = false;
            ui.Borde(raiz, NexusTheme.Alfa(c._t.cyan, 0.45f), NexusTheme.RadioLg, 1.5f);
            return c;
        }

        public void Mostrar(OpcionDeCalendario o, int diasTotales, string unidad) {
            if (o == null || o == _mostrada) return;
            _mostrada = o;
            if (_contenido != null) { _contenido.gameObject.SetActive(false); Destroy(_contenido.gameObject); }
            _contenido = _ui.Nodo(_raiz, "Contenido");
            UiKit.Rellenar(_contenido, 18);
            var dias = Mathf.Max(1, diasTotales);

            TextoEnCaja.Crear(_ui, _contenido, o.Titulo, EstiloTexto.Subtitulo, _t.cyan, TextAlignmentOptions.TopLeft, _t.Px(20),
                              new Vector2(0, 0.84f), new Vector2(1, 1));
            TextoEnCaja.Crear(_ui, _contenido, "Tu calendario de " + dias + " días · las banderas son los días en que se entrega",
                              EstiloTexto.Pequeno, _t.inkMuted, TextAlignmentOptions.TopLeft, _t.Px(14),
                              new Vector2(0, 0.76f), new Vector2(1, 0.84f));

            // la tira: un bloque por unidad, a escala de sus dias
            var tira = _ui.Nodo(_contenido, "Tira");
            tira.anchorMin = new Vector2(0, 0.34f); tira.anchorMax = new Vector2(1, 0.62f);
            tira.offsetMin = tira.offsetMax = Vector2.zero;
            var acumulado = 0;
            for (var i = 0; i < o.Unidades.Count; i++) {
                var u = o.Unidades[i];
                var bloque = _ui.Nodo(tira, "Unidad " + (i + 1));
                bloque.anchorMin = new Vector2((float)acumulado / dias, 0);
                bloque.anchorMax = new Vector2((float)(acumulado + u) / dias, 1);
                bloque.offsetMin = new Vector2(2, 0); bloque.offsetMax = new Vector2(-2, 0);
                bloque.pivot = new Vector2(0.5f, 0);
                var img = bloque.gameObject.AddComponent<Image>();
                _ui.Fondo(img, NexusTheme.Alfa(_t.cyan, i % 2 == 0 ? 0.30f : 0.18f), NexusTheme.RadioSm);
                img.raycastTarget = false;
                _ui.Borde(bloque, _t.cyan, NexusTheme.RadioSm, 1.5f);
                var nombre = i < o.Nombres.Count ? o.Nombres[i] : unidad + " " + (i + 1);
                TextoEnCaja.Crear(_ui, bloque, nombre + "\n" + u + " d", EstiloTexto.Pequeno, _t.ink, TextAlignmentOptions.Center,
                                  _t.Px(14), Vector2.zero, Vector2.one, 4);
                Aparecer.En(bloque, 0.08f * i, true);
                acumulado += u;
            }

            // los despliegues: una banderita sobre el dia en que se entrega
            foreach (var d in o.Despliegues) {
                var x = Mathf.Clamp01((float)d / dias);
                var marca = _ui.Nodo(_contenido, "Despliegue " + d);
                marca.anchorMin = marca.anchorMax = new Vector2(x, 0.62f);
                marca.pivot = new Vector2(x > 0.92f ? 1 : x < 0.08f ? 0 : 0.5f, 0);
                marca.sizeDelta = new Vector2(_t.Px(64), _t.Px(34));
                marca.anchoredPosition = new Vector2(0, 4);
                var punto = _ui.Nodo(marca, "Bandera");
                punto.anchorMin = punto.anchorMax = new Vector2(marca.pivot.x, 0);
                punto.pivot = new Vector2(marca.pivot.x, 0);   // en los extremos, el punto no se sale de la tira
                punto.sizeDelta = new Vector2(_t.Px(12), _t.Px(12));
                var pi = punto.gameObject.AddComponent<Image>();
                pi.sprite = _ui.SpriteCircular(); pi.color = _t.warning; pi.raycastTarget = false;
                TextoEnCaja.Crear(_ui, marca, "día " + d, EstiloTexto.Leyenda, _t.warning, TextAlignmentOptions.Bottom, _t.Px(12),
                                  new Vector2(0, 0.40f), Vector2.one);
                Aparecer.En(marca, 0.08f * o.Unidades.Count + 0.1f, false);
            }

            // el eje y el resumen
            TextoEnCaja.Crear(_ui, _contenido, "día 1", EstiloTexto.Leyenda, _t.inkMuted, TextAlignmentOptions.TopLeft, _t.Px(12),
                              new Vector2(0, 0.24f), new Vector2(0.3f, 0.33f));
            TextoEnCaja.Crear(_ui, _contenido, "día " + dias, EstiloTexto.Leyenda, _t.inkMuted, TextAlignmentOptions.TopRight, _t.Px(12),
                              new Vector2(0.7f, 0.24f), new Vector2(1, 0.33f));
            var entregas = o.Despliegues.Count == 1 ? "Un solo despliegue, el día " + o.Despliegues[0] + "."
                         : o.Despliegues.Count + " despliegues: los días " + string.Join(", ", o.Despliegues) + ".";
            TextoEnCaja.Crear(_ui, _contenido, entregas + (string.IsNullOrEmpty(o.Detalle) ? "" : "  " + o.Detalle),
                              EstiloTexto.Cuerpo, _t.ink, TextAlignmentOptions.TopLeft, _t.Px(15), new Vector2(0, 0), new Vector2(1, 0.22f));
        }
    }

    /// <summary>Una pieza que entra con un retraso: crece desde abajo (o desde su centro) y se enciende.</summary>
    public sealed class Aparecer : MonoBehaviour {
        private float _t, _retraso;
        private bool _vertical;
        private CanvasGroup _g;

        public static void En(RectTransform rt, float retraso, bool vertical) {
            var a = rt.gameObject.AddComponent<Aparecer>();
            a._retraso = retraso; a._vertical = vertical; a._t = -retraso;
            a._g = rt.gameObject.AddComponent<CanvasGroup>();
            a._g.alpha = 0;
            a.Aplicar();
        }

        private void Update() {
            if (_t > 0.4f) return;
            _t += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            Aplicar();
        }

        private void Aplicar() {
            var x = Mathf.Clamp01(_t / 0.35f);
            var e = ActoresDeEscena.Rebote(x);
            _g.alpha = Mathf.Clamp01(x * 2);
            transform.localScale = _vertical ? new Vector3(1, Mathf.Max(0.001f, e), 1) : new Vector3(e, e, 1);
        }

        /// <summary>Termina ya (herramienta de capturas).</summary>
        public void Terminar() { _t = 1; Aplicar(); }
    }
}
