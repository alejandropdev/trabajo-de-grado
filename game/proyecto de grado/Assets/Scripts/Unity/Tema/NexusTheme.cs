using TMPro;
using UnityEngine;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// Todo lo visual de la interfaz, en un solo asset. UiKit no conoce ningun color ni ninguna fuente:
    /// los pide aqui.
    ///
    /// Los tokens son los del design system del juego (tema oscuro «Núcleo»; el claro «Aula» es solo para
    /// material impreso y no entra en la experiencia jugable). Los nombres son los del design system, tal cual,
    /// para poder buscarlos en los dos lados. Reglas que mas se rompen sin querer:
    ///   · cyan es el acento de marca: seleccion, progreso, foco. No es un color de estado.
    ///   · violet es SOLO narrativo (cinematicas, decisiones con peso de historia). Nunca estado de sistema.
    ///   · danger = urgente/bloqueado, warning = aviso/riesgo/deuda, success = hecho. Si todo es urgente, nada lo es.
    ///
    /// Los campos del bloque «Clasico» son la paleta anterior («Bunker Corporativo v2»). Solo los usan la receta
    /// de los minijuegos y las pizarras de tiza, que se quedan como estaban (ver UiKit.Clasico); ninguna pantalla
    /// nueva debe leerlos.
    ///
    /// Crear: Assets > Create > Nexus > Tema, o el menu Nexus > Crear escena principal (lo crea si falta).
    /// </summary>
    [CreateAssetMenu(fileName = "NexusTheme", menuName = "Nexus/Tema")]
    public sealed class NexusTheme : ScriptableObject {
        // ================================================================ design system

        [Header("Fondos y superficies")]
        public Color bg950 = Hex(0x0A0D12);
        public Color bg900 = Hex(0x10141B);
        public Color surface = Hex(0x161D27);
        public Color surfaceRaised = Hex(0x1C2531);
        public Color surfaceSunken = Hex(0x0D1117);
        public Color line = Hex(0x2A3542);
        public Color lineStrong = Hex(0x3D4B5C);

        [Header("Texto")]
        public Color ink = Hex(0xEEF3F7);
        public Color inkMuted = Hex(0x94A1B2);
        [Tooltip("Deshabilitado o terciario. Nunca para texto accionable ni sobre surfaceSunken.")]
        public Color inkFaint = Hex(0x576375);

        [Header("Acento de marca")]
        public Color cyan = Hex(0x22D3EE);
        public Color cyanSoft = Hex(0x0E2A33);
        public Color onCyan = Hex(0x04161B);

        [Header("Acento narrativo — nunca estado de sistema")]
        public Color violet = Hex(0xC084FC);
        public Color violetSoft = Hex(0x241236);
        public Color onViolet = Hex(0x1A0A2E);

        [Header("Estados")]
        public Color success = Hex(0x34D399);
        public Color successSoft = Hex(0x0C2A1F);
        public Color onSuccess = Hex(0x06231A);
        public Color warning = Hex(0xFBBF24);
        public Color warningSoft = Hex(0x2E2309);
        public Color onWarning = Hex(0x2B1D02);
        public Color danger = Hex(0xF87171);
        public Color dangerSoft = Hex(0x341012);
        public Color onDanger = Hex(0x2B0B0B);

        [Header("Papel — documentos del mundo, no cambian con el tema")]
        public Color paperBg = Hex(0xECE0C6);
        public Color paperInk = Hex(0x2C2013);
        public Color paperMuted = Hex(0x6B5A3E);
        public Color paperLine = Hex(0xC9B78F);
        public Color paperAccent = Hex(0x8A3B24);

        [Header("Fuentes (vacias = las de Resources/Fuentes)")]
        public TMP_FontAsset fuenteDisplay;
        public TMP_FontAsset fuenteInterfaz;
        public TMP_FontAsset fuenteInterfazNegrita;
        public TMP_FontAsset fuenteCodigo;
        public TMP_FontAsset fuenteDocumento;

        [Header("Escala")]
        [Tooltip("El design system mide en px de web; el lienzo es 1920×1080. Todas las medidas del design system " +
                 "(tipografia, espacios, radios) se multiplican por este factor.")]
        public float escala = 1.4f;

        // ================================================================ clasico (receta y pizarras)

        [Header("Clasico — solo receta y pizarras de tiza")]
        public Color fondo = Hex(0x1B2A31);
        public Color fondoSecundario = Hex(0x233B42);
        public Color pared = Hex(0x2E4A52);
        public Color hormigon = Hex(0x3A4A4E);
        public Color cian = Hex(0x5FD8F5);
        public Color cianClaro = Hex(0x8FEAFF);
        public Color mostaza = Hex(0xC9A227);
        public Color mostazaClara = Hex(0xE3C24F);
        public Color mostazaEnvejecida = Hex(0x8F6F14);
        public Color naranja = Hex(0xE8703A);
        public Color rojo = Hex(0xC94F3D);
        public Color amarillo = Hex(0xF2C14E);
        public Color texto = Hex(0xDDE6E8);
        public Color textoTenue = Hex(0x8FA3AA);
        public Color textoSobreCian = Hex(0x10191D);

        [Tooltip("Vacias = la de TextMesh Pro por defecto")]
        public TMP_FontAsset fuenteTitulo;
        public TMP_FontAsset fuenteCuerpo;
        public TMP_FontAsset fuenteMonoespaciada;

        public float tamTitulo = 46;
        public float tamSubtitulo = 30;
        public float tamCuerpo = 22;
        public float tamPequeno = 17;

        [Tooltip("Se usan con Image.Type.Sliced: configura los bordes en el Sprite Editor.")]
        public Sprite spritePanel;
        public Sprite spriteBoton;
        public Sprite spriteBarraFondo;
        public Sprite spriteBarraRelleno;
        public Sprite spriteDial;

        [Header("Medidas de layout (compartidas)")]
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

        // ---- fuentes clasicas
        public TMP_FontAsset FuenteTitulo { get { return fuenteTitulo != null ? fuenteTitulo : TMP_Settings.defaultFontAsset; } }
        public TMP_FontAsset FuenteCuerpo { get { return fuenteCuerpo != null ? fuenteCuerpo : TMP_Settings.defaultFontAsset; } }
        public TMP_FontAsset FuenteMono { get { return fuenteMonoespaciada != null ? fuenteMonoespaciada : FuenteCuerpo; } }

        // ---- fuentes del design system
        public TMP_FontAsset FuenteDisplay { get { return fuenteDisplay != null ? fuenteDisplay : FuentesNexus.Display; } }
        public TMP_FontAsset FuenteInterfaz { get { return fuenteInterfaz != null ? fuenteInterfaz : FuentesNexus.Interfaz; } }
        public TMP_FontAsset FuenteInterfazNegrita { get { return fuenteInterfazNegrita != null ? fuenteInterfazNegrita : FuentesNexus.InterfazNegrita; } }
        public TMP_FontAsset FuenteCodigo { get { return fuenteCodigo != null ? fuenteCodigo : FuentesNexus.Codigo; } }
        public TMP_FontAsset FuenteDocumento { get { return fuenteDocumento != null ? fuenteDocumento : FuentesNexus.Documento; } }

        /// <summary>Una medida del design system (en px de web) llevada al lienzo.</summary>
        public float Px(float pxWeb) { return pxWeb * escala; }

        /// <summary>space-1 … space-12 del design system (base 4px), ya escalado. Espacio(4) = space-4 = 16px web.</summary>
        public float Espacio(int paso) { return Px(paso * 4); }

        // radius-xs/sm/md/lg del design system, en px de web
        public const float RadioXs = 2, RadioSm = 4, RadioMd = 8, RadioLg = 16;

        /// <summary>El color como «#RRGGBB» para texto enriquecido de TMP (&lt;color=…&gt;).</summary>
        public static string Html(Color c) { return "#" + ColorUtility.ToHtmlStringRGB(c); }

        /// <summary>El mismo color con otra opacidad.</summary>
        public static Color Alfa(Color c, float a) { return new Color(c.r, c.g, c.b, a); }

        // ---- tonos: el mismo trio (texto / fondo entintado / texto sobre relleno) para cada estado
        public Color ColorDe(Tono tono) {
            switch (tono) {
                case Tono.Cyan: return cyan;
                case Tono.Exito: return success;
                case Tono.Aviso: return warning;
                case Tono.Peligro: return danger;
                case Tono.Violeta: return violet;
                default: return inkMuted;
            }
        }

        public Color SuaveDe(Tono tono) {
            switch (tono) {
                case Tono.Cyan: return cyanSoft;
                case Tono.Exito: return successSoft;
                case Tono.Aviso: return warningSoft;
                case Tono.Peligro: return dangerSoft;
                case Tono.Violeta: return violetSoft;
                default: return line;
            }
        }

        public Color SobreDe(Tono tono) {
            switch (tono) {
                case Tono.Cyan: return onCyan;
                case Tono.Exito: return onSuccess;
                case Tono.Aviso: return onWarning;
                case Tono.Peligro: return onDanger;
                case Tono.Violeta: return onViolet;
                default: return ink;
            }
        }

        private static Color Hex(int rgb) {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }
    }

    /// <summary>Los tonos semanticos del design system. Neutro = sin carga de estado.</summary>
    public enum Tono { Neutro, Cyan, Exito, Aviso, Peligro, Violeta }
}
