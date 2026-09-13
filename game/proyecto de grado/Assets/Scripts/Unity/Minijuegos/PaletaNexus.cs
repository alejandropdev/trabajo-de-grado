using System;
using UnityEngine;

namespace Nexus.Unity.Minijuegos {
    /// <summary>
    /// Los colores salen del codigo y pasan a un asset que arte puede tocar
    /// sin abrir un .cs. Crear con: Assets > Create > Nexus > Paleta.
    /// Guardalo en Assets/Settings/PaletaNexus.asset.
    /// </summary>
    [CreateAssetMenu(fileName = "PaletaNexus", menuName = "Nexus/Paleta")]
    public sealed class PaletaNexus : ScriptableObject {
        [Serializable]
        public struct ColorLogico {
            public string nombre;     // "canal", "verde", "mostaza", "gris", "rojo"
            public Color color;
        }

        [Header("Ramas del grafo")]
        public ColorLogico[] coloresRama = new ColorLogico[0];

        [Header("Autores")]
        [Tooltip("Solo se usan con nivelAndamiaje 3. Que sean distintos entre si Y distintos " +
                 "de los colores de rama, o el jugador confunde las dos codificaciones.")]
        public Color[] coloresAutor = new Color[0];

        [Header("Estados de fila")]
        public Color filaNormal = new Color(0, 0, 0, 0);
        public Color filaSeleccionada = new Color(0.31f, 0.70f, 0.79f, 0.18f);
        public Color filaMarcada = new Color(0.66f, 0.29f, 0.26f, 0.22f);
        public Color filaResaltada = new Color(0.78f, 0.58f, 0.17f, 0.14f);

        [Header("Texto")]
        public Color texto = new Color32(0xD8, 0xDE, 0xE2, 0xFF);
        public Color textoTenue = new Color32(0x7A, 0x88, 0x91, 0xFF);
        public Color relojUrgente = new Color32(0xC8, 0x95, 0x2B, 0xFF);

        [Header("Diff")]
        public Color diffAnadido = new Color32(0x6F, 0xA3, 0x6B, 0xFF);
        public Color diffBorrado = new Color32(0xA8, 0x4B, 0x42, 0xFF);
        public Color diffContexto = new Color32(0x7A, 0x88, 0x91, 0xFF);

        [Range(0f, 1f)] public float atenuacionRamaInactiva = 0.35f;

        public Color Rama(string nombreLogico) {
            foreach (var c in coloresRama)
                if (c.nombre == nombreLogico) return c.color;
            return textoTenue;
        }
    }
}
