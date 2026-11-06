using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Nexus.EditorTools {
    /// <summary>
    /// La identidad del ejecutable y la compilacion de entrega.
    ///
    ///   Nexus > Build > Aplicar marca y ajustes      nombre, icono, pantalla de arranque, ventana
    ///   Nexus > Build > Compilar para Windows        lo anterior + build de release en builds/
    ///
    /// Los ajustes se aplican con la API de PlayerSettings (no editando ProjectSettings.asset a mano) para que
    /// Unity los guarde aunque el editor este abierto. Al recargar scripts se comprueban solos: si alguien vuelve
    /// a dejar el nombre de la plantilla o pierde el icono, se restauran.
    ///
    /// Las imagenes salen de tools/marca/generar_marca.py a partir de Assets/Art/Backgrounds/logo.png.
    ///
    /// Desde la linea de comandos (con el editor cerrado):
    ///   Unity.exe -batchmode -quit -projectPath "game/proyecto de grado"
    ///             -executeMethod Nexus.EditorTools.ConfiguracionDeMarca.CompilarWindowsLote
    /// </summary>
    [InitializeOnLoad]
    public static class ConfiguracionDeMarca {
        public const string Empresa = "Nexus Protocol Team";
        public const string Producto = "Nexus Protocol";
        public const string Identificador = "com.nexusprotocolteam.nexusprotocol";
        public const string NombreEjecutable = "NexusProtocol";

        private const string RutaIcono = "Assets/Art/Branding/IconoNexus.png";
        private const string RutaLogoArranque = "Assets/Art/Branding/LogoArranque.png";
        private const float SegundosDelLogo = 2.5f;
        private static readonly Color FondoArranque = new Color32(0x0A, 0x0D, 0x12, 0xFF);   // Tema.bg950

        static ConfiguracionDeMarca() {
            EditorApplication.delayCall += () => {
                if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!EstaAplicada()) Aplicar(silencioso: true);
            };
        }

        // ================================================================== marca

        [MenuItem("Nexus/Build/Aplicar marca y ajustes", priority = 200)]
        public static void AplicarDesdeMenu() {
            Aplicar(silencioso: false);
        }

        public static bool EstaAplicada() {
            var icono = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaIcono);
            var iconos = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
            return PlayerSettings.productName == Producto
                && PlayerSettings.companyName == Empresa
                && icono != null && iconos.Length > 0 && iconos[0] == icono
                && !PlayerSettings.SplashScreen.showUnityLogo
                && PlayerSettings.SplashScreen.logos.Length == 1;
        }

        public static bool Aplicar(bool silencioso) {
            PrepararTextura(RutaIcono, esSprite: false, tamanoMaximo: 1024);
            PrepararTextura(RutaLogoArranque, esSprite: true, tamanoMaximo: 2048);

            var icono = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaIcono);
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(RutaLogoArranque);
            if (icono == null || logo == null) {
                Debug.LogError("[Nexus] Faltan " + RutaIcono + " o " + RutaLogoArranque +
                               ". Generalos con: python tools/marca/generar_marca.py");
                return false;
            }

            // --- identidad: titulo de la ventana, propiedades del .exe y carpeta de guardado (ver MigracionDeGuardado)
            PlayerSettings.companyName = Empresa;
            PlayerSettings.productName = Producto;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, Identificador);

            // --- icono: el predeterminado; Unity saca de el todos los tamaños del .ico de Windows
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icono }, IconKind.Any);

            // --- pantalla de arranque: solo el logo del juego sobre el fondo del tema
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            PlayerSettings.SplashScreen.backgroundColor = FondoArranque;
            PlayerSettings.SplashScreen.background = null;
            PlayerSettings.SplashScreen.overlayOpacity = 0f;
            PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(SegundosDelLogo, logo) };

            // --- ventana de escritorio
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;   // pantalla completa sin bordes
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.allowFullscreenSwitch = true;                          // Alt+Enter
            PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultScreenWidth = 1920;                             // si se pasa a ventana
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.runInBackground = false;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.forceSingleInstance = true;                            // dos instancias pisarian el guardado
            PlayerSettings.usePlayerLog = true;                                   // Player.log para diagnosticar fallos

            AssetDatabase.SaveAssets();

            if (PlayerSettings.SplashScreen.showUnityLogo)
                Debug.LogWarning("[Nexus] La licencia de este editor no permite ocultar el logo de Unity en el arranque.");
            if (!silencioso) Debug.Log("[Nexus] Marca aplicada: " + Producto + " · icono y pantalla de arranque propios.");
            else Debug.Log("[Nexus] Se restauraron la marca y los ajustes del ejecutable (Nexus > Build).");
            return true;
        }

        private static void PrepararTextura(string ruta, bool esSprite, int tamanoMaximo) {
            var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (imp == null) return;
            var tipo = esSprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            var cambia = imp.textureType != tipo || imp.mipmapEnabled || !imp.alphaIsTransparency
                         || imp.textureCompression != TextureImporterCompression.Uncompressed
                         || imp.maxTextureSize != tamanoMaximo || imp.npotScale != TextureImporterNPOTScale.None
                         || (esSprite && imp.spriteImportMode != SpriteImportMode.Single);
            if (!cambia) return;
            imp.textureType = tipo;
            if (esSprite) imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize = tamanoMaximo;
            imp.SaveAndReimport();
        }

        // ================================================================== build

        [MenuItem("Nexus/Build/Compilar para Windows", priority = 201)]
        public static void CompilarWindowsDesdeMenu() {
            var resultado = CompilarWindows();
            if (resultado != null) EditorUtility.RevealInFinder(resultado);
        }

        /// <summary>Para -executeMethod: sale con codigo 0 si compilo, 1 si no.</summary>
        public static void CompilarWindowsLote() {
            EditorApplication.Exit(CompilarWindows() != null ? 0 : 1);
        }

        /// <summary>Devuelve la ruta del .exe, o null si fallo.</summary>
        public static string CompilarWindows() {
            if (!Aplicar(silencioso: true)) return null;

            var escenas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (escenas.Length == 0) {
                Debug.LogError("[Nexus] No hay escenas activas en Build Settings.");
                return null;
            }

            // <repo>/builds/NexusProtocol-<version>/NexusProtocol.exe  (builds/ no se versiona)
            var proyecto = Directory.GetParent(Application.dataPath);           // game/proyecto de grado
            var repo = proyecto.Parent.Parent.FullName;                          // <repo>
            var carpeta = Path.Combine(repo, "builds", NombreEjecutable + "-" + PlayerSettings.bundleVersion);
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);     // sin restos de compilaciones viejas
            var exe = Path.Combine(carpeta, NombreEjecutable + ".exe");

            var opciones = new BuildPlayerOptions {
                scenes = escenas,
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,   // release: sin Development Build, sin depurador, sin consola
            };

            var informe = BuildPipeline.BuildPlayer(opciones);
            var resumen = informe.summary;
            if (resumen.result != BuildResult.Succeeded) {
                Debug.LogError("[Nexus] La compilacion fallo (" + resumen.result + ", " + resumen.totalErrors + " errores).");
                return null;
            }

            // Carpetas que Unity deja junto al .exe y que no se entregan.
            foreach (var sobra in new[] {
                         NombreEjecutable + "_BurstDebugInformation_DoNotShip",
                         NombreEjecutable + "_BackUpThisFolder_ButDontShipItWithYourGame" }) {
                var ruta = Path.Combine(carpeta, sobra);
                if (Directory.Exists(ruta)) Directory.Delete(ruta, true);
            }

            Debug.Log("[Nexus] Build lista: " + exe + " · " + (resumen.totalSize / (1024f * 1024f)).ToString("0.0") +
                      " MB · " + resumen.totalTime.TotalSeconds.ToString("0") + " s");
            return exe;
        }
    }
}
