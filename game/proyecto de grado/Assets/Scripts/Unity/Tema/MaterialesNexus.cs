using System.Collections.Generic;
using UnityEngine;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// Los materiales del juego: el metal remachado de las ventanas, la chapa de los paneles, el papel de los
    /// documentos y los botones de chapa (ambar y pizarra). Son las texturas de Art/minijuegos, copiadas a
    /// Resources/Materiales para poder cargarlas desde codigo. Los bordes 9-slice se fijan AQUI (en pixeles de la
    /// textura), no en el importador: asi el corte vive junto al codigo que lo usa.
    ///
    /// Si falta una textura, se devuelve null y UiKit cae en la caja plana del design system: el juego se ve mas
    /// sobrio, pero sigue funcionando.
    /// </summary>
    public static class MaterialesNexus {
        public const string Marco = "marco_exterior";        // ventana de metal con remaches (Tiled)
        public const string Esquina = "esquina_metal";       // escuadra de metal para las esquinas de una ventana
        public const string Remache = "remache";
        public const string Chapa = "panel_oscuro";          // panel de chapa oscura biselada
        public const string Papel = "panel_pergamino";       // hoja de papel con borde sombreado
        public const string Etiqueta = "boton_etiqueta";     // tarjeta/etiqueta de papel
        public const string BotonAmbar = "boton_mostaza";    // la accion principal
        public const string BotonPizarra = "boton_oscuro";   // el resto de botones
        public const string FondoOficina = "fondo_oficina";  // la oficina ilustrada, detras de todo
        public const string FondoPlacas = "fondo_placas";

        // Borde 9-slice de cada textura, en pixeles de la textura (izquierda, abajo, derecha, arriba).
        private static readonly Dictionary<string, Vector4> Bordes = new Dictionary<string, Vector4> {
            { Marco, new Vector4(112, 112, 112, 112) },
            { Chapa, new Vector4(76, 76, 76, 76) },
            { Papel, new Vector4(52, 52, 52, 52) },
            { Etiqueta, new Vector4(20, 20, 20, 20) },
            { BotonAmbar, new Vector4(36, 36, 36, 36) },
            { BotonPizarra, new Vector4(38, 38, 38, 38) },
        };

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Sprite(string nombre) {
            Sprite s;
            if (Cache.TryGetValue(nombre, out s)) return s;
            var tex = Resources.Load<Texture2D>("Materiales/" + nombre);
            if (tex != null) {
                Vector4 borde;
                Bordes.TryGetValue(nombre, out borde);
                s = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100, 0,
                                              SpriteMeshType.FullRect, borde);
                s.name = nombre;
            } else {
                Debug.LogWarning("[Nexus] Falta el material Resources/Materiales/" + nombre + ".png");
            }
            Cache[nombre] = s;
            return s;
        }

        /// <summary>
        /// Cuanto se encoge el borde de una textura al pintarla: el marco mide 112 px en la textura y debe verse de
        /// unos 28 en el lienzo. Es el pixelsPerUnitMultiplier de la Image.
        /// </summary>
        public static float Reduccion(string nombre, float bordeEnLienzo) {
            Vector4 b;
            return Bordes.TryGetValue(nombre, out b) && b.x > 0 ? b.x / Mathf.Max(1, bordeEnLienzo) : 1;
        }

        // ------------------------------------------------------------------ ilustraciones

        private static readonly Dictionary<string, Sprite> Ilustraciones = new Dictionary<string, Sprite>();

        /// <summary>
        /// Una ilustracion del juego (un personaje, una escena, una viñeta) por su id: Resources/Ilustraciones/&lt;id&gt;.png.
        /// null si el equipo de arte todavia no la ha entregado: quien la pide pone su hueco con silueta.
        /// </summary>
        public static Sprite Ilustracion(string id) {
            if (string.IsNullOrEmpty(id)) return null;
            Sprite s;
            if (Ilustraciones.TryGetValue(id, out s)) return s;
            var tex = Resources.Load<Texture2D>("Ilustraciones/" + id);
            s = tex == null ? null : UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
            Ilustraciones[id] = s;
            return s;
        }

        /// <summary>"Dra. Voss" → "voss", "Marisol Andrade" → "marisol-andrade": el id de archivo de un personaje.</summary>
        public static string IdDePersonaje(string nombre) {
            if (string.IsNullOrEmpty(nombre)) return null;
            var n = nombre.ToLowerInvariant().Replace("dra. ", "").Replace("dr. ", "").Trim();
            var sb = new System.Text.StringBuilder();
            foreach (var c in n.Normalize(System.Text.NormalizationForm.FormD)) {
                if (char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
                sb.Append(char.IsLetterOrDigit(c) ? c : '-');
            }
            return "personajes/" + sb.ToString().Trim('-');
        }
    }
}
