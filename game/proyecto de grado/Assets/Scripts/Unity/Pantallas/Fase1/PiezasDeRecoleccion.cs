using System.Collections.Generic;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Las piezas de la Recoleccion de la Fase 1 (2026-11-08), para decir con dibujos lo que antes eran frases:
    ///   · Tecla:  una tecla de teclado (con su sombra) que se «pulsa» sola en secuencia (W A S D).
    ///   · Ficha:  un objeto que se puede encontrar o que se trajo (su imagen, un contador), con su tooltip.
    ///   · Barrido: una linea de luz que recorre el mapa, como un escaner.
    /// </summary>
    internal static class PiezasDeRecoleccion {
        public static TeclaViva Tecla(UiKit ui, Transform padre, string texto, float ancho, float alto) {
            var t = ui.Tema;
            var rt = ui.Nodo(padre, "Tecla " + texto);
            UiKit.Tamano(rt, ancho, alto);
            var sombra = ui.Nodo(rt, "Sombra");
            UiKit.Rellenar(sombra);
            sombra.offsetMax = new Vector2(0, -4);
            var fs = sombra.gameObject.AddComponent<Image>();
            ui.Fondo(fs, NexusTheme.Alfa(t.cyan, 0.35f), NexusTheme.RadioSm);
            fs.raycastTarget = false;
            var cara = ui.Nodo(rt, "Cara");
            UiKit.Rellenar(cara);
            cara.offsetMin = new Vector2(0, 4);
            var fc = cara.gameObject.AddComponent<Image>();
            ui.Fondo(fc, t.bg950, NexusTheme.RadioSm);
            fc.raycastTarget = false;
            var borde = ui.Borde(cara, NexusTheme.Alfa(t.cyan, 0.8f), NexusTheme.RadioSm, 1.5f);
            TextoEnCaja.Crear(ui, cara, texto, EstiloTexto.Encabezado, t.ink, TextAlignmentOptions.Center, t.Px(16),
                              Vector2.zero, Vector2.one, 2).fontStyle = FontStyles.Bold;
            var viva = rt.gameObject.AddComponent<TeclaViva>();
            viva.Cara = cara; viva.Fondo = fc; viva.Borde = borde; viva.Reposo = t.bg950; viva.Luz = NexusTheme.Alfa(t.cyan, 0.45f);
            return viva;
        }

        /// <summary>
        /// Un objeto: su imagen (Ilustraciones/&lt;id&gt;; sin imagen, el glifo), su nombre corto debajo y, si hay mas de uno,
        /// «×N». Al pasar el raton, el tooltip con 'titulo' y 'texto'.
        /// </summary>
        public static RectTransform Ficha(UiKit ui, Transform padre, string ilustracion, string glifo, string etiqueta, int cuantos,
                                          float lado, string titulo, string texto, System.Func<TooltipDeCalidad> tooltip, Color acento) {
            var t = ui.Tema;
            var rt = ui.Nodo(padre, "Ficha " + etiqueta);
            UiKit.Tamano(rt, lado, lado * 1.32f);
            var caja = ui.Nodo(rt, "Caja");
            caja.anchorMin = new Vector2(0, 0.26f); caja.anchorMax = Vector2.one;
            caja.offsetMin = caja.offsetMax = Vector2.zero;
            var fondo = caja.gameObject.AddComponent<Image>();
            ui.Fondo(fondo, NexusTheme.Alfa(t.bg950, 0.7f), NexusTheme.RadioMd);
            ui.Borde(caja, NexusTheme.Alfa(acento, 0.7f), NexusTheme.RadioMd, 1.5f);
            var sprite = string.IsNullOrEmpty(ilustracion) ? null : MaterialesNexus.Ilustracion(ilustracion);
            if (sprite != null) {
                var img = ui.Nodo(caja, "Imagen");
                UiKit.Rellenar(img, lado * 0.1f);
                var im = img.gameObject.AddComponent<Image>();
                im.sprite = sprite; im.preserveAspect = true; im.raycastTarget = false;
                img.gameObject.AddComponent<FlotaEnBucle>();
            } else {
                TextoEnCaja.Crear(ui, caja, glifo ?? "?", EstiloTexto.Titulo, acento, TextAlignmentOptions.Center, lado * 0.42f,
                                  Vector2.zero, Vector2.one, 4);
            }
            if (cuantos > 1) {
                var insignia = ui.Nodo(caja, "Cuantos");
                insignia.anchorMin = insignia.anchorMax = new Vector2(1, 1);
                insignia.pivot = new Vector2(0.7f, 0.7f);
                insignia.sizeDelta = new Vector2(lado * 0.42f, lado * 0.3f);
                var fi = insignia.gameObject.AddComponent<Image>();
                ui.Fondo(fi, acento, 10);
                fi.raycastTarget = false;
                TextoEnCaja.Crear(ui, insignia, "×" + cuantos, EstiloTexto.Pequeno, t.bg950, TextAlignmentOptions.Center, t.Px(13),
                                  Vector2.zero, Vector2.one).fontStyle = FontStyles.Bold;
            }
            // El nombre crece con la ficha (en la franja de «También trajiste» las fichas son grandes).
            TextoEnCaja.Crear(ui, rt, etiqueta, EstiloTexto.Leyenda, t.ink, TextAlignmentOptions.Top, Mathf.Max(t.Px(12), lado * 0.16f),
                              new Vector2(-0.15f, 0), new Vector2(1.15f, 0.24f));
            var r = caja.gameObject.AddComponent<RealceDeCalidad>();
            r.Fondo = fondo; r.Normal = NexusTheme.Alfa(t.bg950, 0.7f); r.Resaltado = NexusTheme.Alfa(acento, 0.22f);
            r.Escala = 1.06f;
            r.Entra = () => tooltip().Mostrar(titulo, texto, acento);
            r.Sale = () => tooltip().Ocultar();
            return rt;
        }

        /// <summary>Una linea de luz que baja y sube despacio por encima de 'padre' (el mapa, «escaneandolo»).</summary>
        public static void Barrido(UiKit ui, RectTransform padre) {
            var rt = ui.Nodo(padre, "Barrido");
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0, 3);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = NexusTheme.Alfa(ui.Tema.cyan, 0.5f);
            img.raycastTarget = false;
            rt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            rt.gameObject.AddComponent<BarridoVivo>();
        }
    }

    /// <summary>Una tecla que se hunde y se ilumina cuando se le dice (Pulsar).</summary>
    public sealed class TeclaViva : MonoBehaviour {
        public RectTransform Cara;
        public Image Fondo, Borde;
        public Color Reposo, Luz;
        private float _t = 9;
        public void Pulsar() { _t = 0; }
        private void Update() {
            _t += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            var x = _t < 0.35f ? Mathf.Sin(Mathf.PI * _t / 0.35f) : 0f;
            Cara.anchoredPosition = new Vector2(0, -3 * x);
            Fondo.color = Color.Lerp(Reposo, Luz, x);
        }
    }

    /// <summary>Pulsa sus teclas una tras otra, en bucle (la demostracion de «W A S D»).</summary>
    public sealed class SecuenciaDeTeclas : MonoBehaviour {
        public readonly List<TeclaViva> Teclas = new List<TeclaViva>();
        private float _t;
        private int _i;
        private void Update() {
            if (Teclas.Count == 0) return;
            _t += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            if (_t < 0.55f) return;
            _t = 0;
            Teclas[_i % Teclas.Count].Pulsar();
            _i++;
        }
    }

    /// <summary>La linea de luz del mapa: baja y sube, y se desvanece en los extremos.</summary>
    public sealed class BarridoVivo : MonoBehaviour {
        private RectTransform _rt, _padre;
        private Image _img;
        private float _t;
        private void Awake() { _rt = (RectTransform)transform; _padre = _rt.parent as RectTransform; _img = GetComponent<Image>(); }
        private void Update() {
            if (_padre == null) return;
            _t += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            var f = Mathf.PingPong(_t / 3.2f, 1f);
            _rt.anchoredPosition = new Vector2(0, _padre.rect.height * f);
            var c = _img.color; c.a = 0.45f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(f));
            _img.color = c;
        }
    }
}
