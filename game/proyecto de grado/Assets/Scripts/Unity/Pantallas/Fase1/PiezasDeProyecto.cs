using System;
using System.Linq;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>Escala una lamina de tamaño fijo (ancho×alto) para que quepa en su hueco sin deformarse.</summary>
    internal sealed class AjusteDeLamina : MonoBehaviour {
        public RectTransform Lamina;
        public float Ancho, Alto;
        private void LateUpdate() {
            var r = ((RectTransform)transform).rect;
            if (r.width <= 1 || r.height <= 1 || Lamina == null) return;
            var k = Mathf.Min(r.width / Ancho, r.height / Alto);
            Lamina.localScale = new Vector3(k, k, 1);
        }
    }

    /// <summary>El arco del anillo de progreso: crece despacio hasta su meta.</summary>
    internal sealed class AnilloVivo : MonoBehaviour {
        public Image Relleno;
        public float Meta;
        private float _v;
        private void Update() {
            _v = Mathf.MoveTowards(_v, Meta, Mathf.Min(Time.unscaledDeltaTime, 0.1f) / 0.9f);
            Relleno.fillAmount = _v;
        }
    }

    /// <summary>Una luz que late (el borde de lo que falta por decidir).</summary>
    internal sealed class LatidoDeBorde : MonoBehaviour {
        public Image Borde, Halo;
        public Color Color;
        private void Update() {
            var s = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
            if (Borde != null) Borde.color = NexusTheme.Alfa(Color, 0.55f + 0.45f * s);
            if (Halo != null) Halo.color = NexusTheme.Alfa(Color, 0.10f + 0.22f * s);
        }
    }

    /// <summary>Piezas pequeñas que comparten «El proyecto» y el «Resumen» de la Fase 1 (2026-11-08).</summary>
    internal static class PiezasDeProyecto {
        /// <summary>Una lamina de ancho×alto centrada en 'hueco' y escalada para caber en el.</summary>
        public static Lamina NuevaLamina(UiKit ui, RectTransform hueco, float ancho, float alto) {
            var lamina = new Lamina(ui, hueco, ancho, alto);
            var r = lamina.Raiz;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(ancho, alto);
            var ajuste = hueco.gameObject.AddComponent<AjusteDeLamina>();
            ajuste.Lamina = r; ajuste.Ancho = ancho; ajuste.Alto = alto;
            return lamina;
        }

        /// <summary>«Garita. Es donde…» → «Garita.»; si aun es larga, se corta con puntos suspensivos.</summary>
        public static string PrimeraFrase(string texto, int max) {
            if (string.IsNullOrEmpty(texto)) return "";
            var t = texto.Trim();
            var punto = t.IndexOf(". ", StringComparison.Ordinal);
            if (punto > 8) t = t.Substring(0, punto + 1);
            if (t.Length > max) t = t.Substring(0, max - 1).TrimEnd() + "…";
            return t;
        }

        /// <summary>«Gestión de instalaciones (facility management)» → «Gestión de instalaciones»: lo justo para un dato.</summary>
        public static string Titular(string texto, int max) {
            if (string.IsNullOrEmpty(texto)) return "";
            var t = texto;
            var parentesis = t.IndexOf(" (", StringComparison.Ordinal);
            if (parentesis > 4) t = t.Substring(0, parentesis);
            if (t.Length <= max) return t;
            var palabras = t.Split(' ');
            var salida = "";
            foreach (var p in palabras) {
                var siguiente = salida.Length == 0 ? p : salida + " " + p;
                if (siguiente.Length > max) break;
                salida = siguiente;
            }
            // sin dejar colgando un conector («Recepción corporativa y»)
            var cola = salida.Split(' ');
            var n = cola.Length;
            while (n > 1 && (cola[n - 1] == "y" || cola[n - 1] == "de" || cola[n - 1] == "la" || cola[n - 1] == "el" || cola[n - 1] == "en" || cola[n - 1] == "del" || cola[n - 1] == "con")) n--;
            return string.Join(" ", cola.Take(n));
        }

        /// <summary>Un cristal con filo neon (el fondo de una tarjeta de estas pantallas). Devuelve el borde.</summary>
        public static Image Cristal(UiKit ui, RectTransform rt, Color borde, float alfa = 0.62f) {
            var img = rt.GetComponent<Image>();
            if (img == null) img = rt.gameObject.AddComponent<Image>();
            ui.Fondo(img, NexusTheme.Alfa(ui.Tema.bg950, alfa), NexusTheme.RadioLg);
            img.raycastTarget = false;
            return ui.Borde(rt, borde, NexusTheme.RadioLg, 1.5f);
        }

        /// <summary>
        /// Una cifra con su etiqueta (un «dato» del proyecto): el valor grande y, al pasar el raton, un cuadro que lo explica.
        /// </summary>
        public static RectTransform Dato(UiKit ui, Transform padre, string valor, string etiqueta, Color acento, string titulo, string texto,
                                         Func<TooltipDeCalidad> tip, float ancho, float alto) {
            var t = ui.Tema;
            var rt = ui.Nodo(padre, "Dato " + etiqueta);
            var le = UiKit.Tamano(rt, alto: alto, flexAncho: 1);
            le.minWidth = ancho * 0.6f; le.preferredWidth = ancho;
            var fondo = rt.gameObject.AddComponent<Image>();
            ui.Fondo(fondo, NexusTheme.Alfa(t.bg950, 0.7f), NexusTheme.RadioMd);
            ui.Borde(rt, NexusTheme.Alfa(acento, 0.6f), NexusTheme.RadioMd, 1.5f);
            TextoEnCaja.Crear(ui, rt, valor, EstiloTexto.Titulo, acento, TextAlignmentOptions.Center, alto * 0.42f,
                              new Vector2(0, 0.36f), Vector2.one, 6).fontStyle = FontStyles.Bold;
            TextoEnCaja.Crear(ui, rt, etiqueta, EstiloTexto.Leyenda, t.inkMuted, TextAlignmentOptions.Center, t.Px(11),
                              Vector2.zero, new Vector2(1, 0.40f), 4);
            if (!string.IsNullOrEmpty(texto) && tip != null) {
                fondo.raycastTarget = true;
                var r = rt.gameObject.AddComponent<RealceDeCalidad>();
                r.Fondo = fondo; r.Normal = NexusTheme.Alfa(t.bg950, 0.7f); r.Resaltado = NexusTheme.Alfa(acento, 0.22f); r.Escala = 1.04f;
                r.Entra = () => tip().Mostrar(titulo, texto, acento);
                r.Sale = () => tip().Ocultar();
            }
            return rt;
        }

        /// <summary>Un anillo de progreso (un disco tenue con un arco que se llena y un hueco con su texto).</summary>
        public static RectTransform Anillo(UiKit ui, Transform padre, float lado, float valor, Color color, string centro) {
            var t = ui.Tema;
            var rt = ui.Nodo(padre, "Anillo");
            UiKit.Tamano(rt, lado, lado);
            var baseImg = rt.gameObject.AddComponent<Image>();
            baseImg.sprite = ui.SpriteCircular(); baseImg.color = NexusTheme.Alfa(color, 0.2f); baseImg.raycastTarget = false;
            var arco = ui.Nodo(rt, "Arco");
            UiKit.Rellenar(arco);
            var ai = arco.gameObject.AddComponent<Image>();
            ai.sprite = ui.SpriteCircular(); ai.type = Image.Type.Filled; ai.fillMethod = Image.FillMethod.Radial360;
            ai.fillOrigin = (int)Image.Origin360.Top; ai.fillClockwise = true; ai.color = color; ai.fillAmount = 0; ai.raycastTarget = false;
            var hueco = ui.Nodo(rt, "Centro");
            UiKit.Rellenar(hueco, lado * 0.13f);
            var hi = hueco.gameObject.AddComponent<Image>();
            hi.sprite = ui.SpriteCircular(); hi.color = t.bg950; hi.raycastTarget = false;
            TextoEnCaja.Crear(ui, hueco, centro, EstiloTexto.Titulo, t.ink, TextAlignmentOptions.Center, lado * 0.28f,
                              Vector2.zero, Vector2.one, 2).fontStyle = FontStyles.Bold;
            var v = rt.gameObject.AddComponent<AnilloVivo>();
            v.Relleno = ai; v.Meta = Mathf.Clamp01(valor);
            return rt;
        }
    }
}
