using TMPro;
using UnityEngine;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// Todo lo visual de la interfaz, en un solo asset. UiKit no conoce ningun color ni ninguna fuente:
    /// los pide aqui. Hoy los sprites estan vacios y UiKit pinta rectangulos de color; el dia que se
    /// arrastre un sprite a un campo, TODAS las pantallas que lo usan cambian a la vez, sin abrir un prefab.
    ///
    /// La paleta es la de la Biblia §11.4.1, «Bunker Corporativo v2». La regla que mas se rompe sin querer:
    /// el mostaza es el color del peligro y no puede pasar del 12 % del encuadre. Si todo es urgente,
    /// nada lo es.
    ///
    /// Crear: Assets > Create > Nexus > Tema, o el menu Nexus > Crear escena principal (lo crea si falta).
    /// </summary>
    [CreateAssetMenu(fileName = "NexusTheme", menuName = "Nexus/Tema")]
    public sealed class NexusTheme : ScriptableObject {
        [Header("Fondos")]
        public Color fondo = Hex(0x1B2A31);
        public Color fondoSecundario = Hex(0x233B42);
        public Color pared = Hex(0x2E4A52);
        public Color hormigon = Hex(0x3A4A4E);

        [Header("Informacion (cian)")]
        public Color cian = Hex(0x5FD8F5);
        public Color cianClaro = Hex(0x8FEAFF);

        [Header("Peligro (mostaza) — max 12 % del encuadre")]
        public Color mostaza = Hex(0xC9A227);
        public Color mostazaClara = Hex(0xE3C24F);
        public Color mostazaEnvejecida = Hex(0x8F6F14);

        [Header("Acentos calidos")]
        public Color naranja = Hex(0xE8703A);
        public Color rojo = Hex(0xC94F3D);
        public Color amarillo = Hex(0xF2C14E);

        [Header("Texto")]
        public Color texto = Hex(0xDDE6E8);
        public Color textoTenue = Hex(0x8FA3AA);
        public Color textoSobreCian = Hex(0x10191D);

        [Header("Fuentes (vacias = la de TextMesh Pro por defecto)")]
        public TMP_FontAsset fuenteTitulo;
        public TMP_FontAsset fuenteCuerpo;
        public TMP_FontAsset fuenteMonoespaciada;

        [Header("Tamaños de texto")]
        public float tamTitulo = 46;
        public float tamSubtitulo = 30;
        public float tamCuerpo = 22;
        public float tamPequeno = 17;

        [Header("Sprites (vacios = rectangulo de color)")]
        [Tooltip("Se usan con Image.Type.Sliced: configura los bordes en el Sprite Editor.")]
        public Sprite spritePanel;
        public Sprite spriteBoton;
        public Sprite spriteBarraFondo;
        public Sprite spriteBarraRelleno;
        public Sprite spriteDial;

        [Header("Medidas")]
        public float margen = 24;
        public float espacio = 12;
        public float altoBoton = 56;
        public float altoBarra = 18;

        /// <summary>
        /// El tema que se usa si no hay ninguno asignado. Asi la aplicacion arranca aunque nadie haya creado
        /// el asset todavia: es el mismo tema, pero en memoria.
        /// </summary>
        public static NexusTheme PorDefecto() {
            var tema = CreateInstance<NexusTheme>();
            tema.name = "NexusTheme (por defecto)";
            return tema;
        }

        public TMP_FontAsset FuenteTitulo { get { return fuenteTitulo != null ? fuenteTitulo : TMP_Settings.defaultFontAsset; } }
        public TMP_FontAsset FuenteCuerpo { get { return fuenteCuerpo != null ? fuenteCuerpo : TMP_Settings.defaultFontAsset; } }
        public TMP_FontAsset FuenteMono { get { return fuenteMonoespaciada != null ? fuenteMonoespaciada : FuenteCuerpo; } }

        private static Color Hex(int rgb) {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }
    }
}
