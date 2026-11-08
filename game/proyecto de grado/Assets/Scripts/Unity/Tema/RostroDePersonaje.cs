using Nexus.Core.Tablero;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// El ROSTRO de una persona del equipo (2026-11-08, sacado de PantallaDelDia para usarlo tambien en la Fase 1): un
    /// circulo con la parte de arriba de su retrato (los retratos son de medio cuerpo, con la cara arriba al centro) y
    /// borde. Sin retrato, sus iniciales.
    /// </summary>
    public static class RostroDePersonaje {
        /// <summary>El retrato de un miembro: su personaje si lo tiene, si no por su nombre; null = la protagonista.</summary>
        public static string FotoDe(MiembroDelEquipo m) {
            if (m == null) return "personajes/protagonista";
            // Los que tienen retrato con otro nombre de archivo (2026-11-06). Los demas, por su nombre.
            switch (m.Nombre) {
                case "Javier Webb": return "personajes/javier";
                case "Óscar Rondón": return "personajes/oscar-rendon";
            }
            return MaterialesNexus.IdDePersonaje(!string.IsNullOrEmpty(m.Personaje) ? m.Personaje : m.Nombre);
        }

        /// <summary>«El guardia de la garita» → «G»: la inicial de lo importante, sin el articulo.</summary>
        public static string Inicial(string nombre) {
            if (string.IsNullOrWhiteSpace(nombre)) return "?";
            foreach (var palabra in nombre.Replace("Dra. ", "").Replace("Dr. ", "").Split(' ')) {
                var p = palabra.Trim().ToLowerInvariant();
                if (p.Length == 0 || p == "el" || p == "la" || p == "los" || p == "las" || p == "un" || p == "una" || p == "de" || p == "del") continue;
                return char.ToUpperInvariant(palabra.Trim()[0]).ToString();
            }
            return nombre.Trim().Substring(0, 1).ToUpperInvariant();
        }

        /// <summary>El circulo de lado 'lado', hijo de 'padre' (con su LayoutElement). Devuelve el marco.</summary>
        public static RectTransform Crear(UiKit ui, Transform padre, string id, string nombre, float lado, Color? borde = null) {
            var tema = ui.Tema;
            var sprite = MaterialesNexus.Ilustracion(id);
            var marco = ui.Nodo(padre, "Rostro");
            UiKit.Tamano(marco, lado, lado);
            marco.sizeDelta = new Vector2(lado, lado);
            if (sprite == null) {
                var r = ui.Retrato(marco, Inicial(nombre), Tono.Cyan, lado / tema.escala);
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = new Vector2(lado, lado);   // Retrato solo pone su LayoutElement: sin esto, mide 100×100
                return marco;
            }
            var mascara = marco.gameObject.AddComponent<Image>();
            mascara.sprite = ui.SpriteCircular();
            mascara.raycastTarget = false;
            marco.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            mascara.color = tema.bg900;
            var cara = ui.Nodo(marco, "Cara");
            UiKit.Rellenar(cara);
            var img = cara.gameObject.AddComponent<RawImage>();
            img.texture = sprite.texture;
            // Los retratos con gesto (la mano y el papel ocupan medio cuadro) llevan la cara mas pequeña: recorte mas cerrado.
            var conGesto = id == "personajes/kenji-sato" || id == "personajes/derek-hartman" || id == "personajes/elena-gomez";
            img.uvRect = conGesto ? new Rect(0.28f, 0.50f, 0.50f, 0.50f) : new Rect(0.22f, 0.42f, 0.56f, 0.56f);
            img.raycastTarget = false;
            ui.Borde(marco, borde ?? tema.cyan, lado / 2f / tema.escala, 2);
            return marco;
        }
    }
}
