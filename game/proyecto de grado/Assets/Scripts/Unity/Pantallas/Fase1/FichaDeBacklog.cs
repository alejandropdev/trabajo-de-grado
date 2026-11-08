using System;
using Nexus.Core.Proyecto;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Una historia de usuario como ficha compacta de cristal (2026-11-08), con el filo del color de su parte del sistema:
    /// su titulo en una o dos lineas, y «como / quiero / para» al pasar el raton. NO enseña el valor ni si la pidieron
    /// (eso es lo que se decide). Se coge y se suelta con la mecanica del muro (FichaArrastrable); un clic, la mete o la saca.
    /// </summary>
    internal static class FichaDeBacklog {
        public static FichaArrastrable Crear(UiKit ui, RectTransform padre, RectTransform capa, HistoriaCandidata h, Color modulo, int numero,
                                             string origen, Func<TooltipDeCalidad> tip, bool compacta) {
            var t = ui.Tema;
            var rt = ui.Nodo(padre, "Historia " + h.Id);
            var le = UiKit.Tamano(rt, flexAncho: 1);
            le.preferredHeight = t.Px(compacta ? 36 : 54); le.minHeight = t.Px(compacta ? 30 : 44);
            var fondo = rt.gameObject.AddComponent<Image>();
            ui.Fondo(fondo, NexusTheme.Alfa(t.bg950, 0.88f), NexusTheme.RadioMd);
            ui.Borde(rt, NexusTheme.Alfa(modulo, 0.75f), NexusTheme.RadioMd, 1.5f);

            // la franja del color de su parte del sistema
            var franja = ui.Nodo(rt, "Franja");
            franja.anchorMin = Vector2.zero; franja.anchorMax = new Vector2(0, 1);
            franja.offsetMin = new Vector2(t.Px(4), t.Px(5)); franja.offsetMax = new Vector2(t.Px(8), -t.Px(5));
            var fi = franja.gameObject.AddComponent<Image>();
            ui.Fondo(fi, modulo, 2);
            fi.raycastTarget = false;

            var izquierda = t.Px(18);
            if (numero > 0) {
                var lado = t.Px(compacta ? 24 : 26);
                var insignia = ui.Nodo(rt, "Numero");
                insignia.anchorMin = insignia.anchorMax = new Vector2(0, 0.5f);
                insignia.pivot = new Vector2(0, 0.5f);
                insignia.sizeDelta = new Vector2(lado, lado);
                insignia.anchoredPosition = new Vector2(t.Px(14), 0);
                var ii = insignia.gameObject.AddComponent<Image>();
                ii.sprite = ui.SpriteCircular(); ii.color = NexusTheme.Alfa(modulo, 0.25f); ii.raycastTarget = false;
                ui.Borde(insignia, modulo, lado / t.escala / 2, 1.5f);
                TextoEnCaja.Crear(ui, insignia, numero.ToString(), EstiloTexto.Encabezado, modulo, TextAlignmentOptions.Center, lado * 0.62f,
                                  Vector2.zero, Vector2.one, 1).fontStyle = FontStyles.Bold;
                izquierda = t.Px(14) + lado + t.Px(8);
            }
            var titulo = TextoEnCaja.Crear(ui, rt, h.Titulo, EstiloTexto.Pequeno, t.ink, TextAlignmentOptions.MidlineLeft, t.Px(compacta ? 15 : 16),
                                           Vector2.zero, Vector2.one);
            titulo.rectTransform.offsetMin = new Vector2(izquierda, t.Px(3));
            titulo.rectTransform.offsetMax = new Vector2(-t.Px(8), -t.Px(3));
            if (compacta) titulo.textWrappingMode = TextWrappingModes.NoWrap;

            // al pasar el raton: como / quiero / para
            fondo.raycastTarget = true;
            var r = rt.gameObject.AddComponent<RealceDeCalidad>();
            r.Fondo = fondo; r.Normal = NexusTheme.Alfa(t.bg950, 0.88f); r.Resaltado = NexusTheme.Alfa(modulo, 0.22f);
            var texto = string.IsNullOrEmpty(h.Como) ? h.Titulo
                      : "Como " + h.Como + ",\nquiero " + h.Quiero + ",\npara " + h.Para + ".";
            r.Entra = () => { if (FichaArrastrable.EnVuelo == null) tip().Mostrar(h.Titulo, texto, modulo); };
            r.Sale = () => tip().Ocultar();

            var ficha = rt.gameObject.AddComponent<FichaArrastrable>();
            ficha.Id = h.Id; ficha.Origen = origen; ficha.Capa = capa;
            return ficha;
        }
    }
}
