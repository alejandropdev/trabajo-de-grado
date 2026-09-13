using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Exporta cada sprite individual (recortado dentro de una textura "Multiple")
/// como un archivo PNG separado, usando el nombre del sprite como nombre de archivo.
///
/// CÓMO USARLO:
/// 1. Coloca este script dentro de una carpeta llamada "Editor" en tu proyecto
///    (por ejemplo: Assets/Editor/SpriteExporter.cs). Debe estar en una carpeta
///    "Editor" o Unity no lo va a reconocer como herramienta de editor.
/// 2. En el Project window, selecciona la textura (el PNG grande) que ya tiene
///    los sprites recortados en modo "Multiple".
/// 3. Ve al menú superior: Tools > Export Sprites To PNG
/// 4. Elige la carpeta de destino cuando se te pida.
/// 5. Cada sprite se exportará como un PNG independiente, nombrado igual que
///    aparece en el Sprite Editor (ej: "A.png", "B.png", "icon_settings.png").
/// </summary>
public class SpriteExporter {
    [MenuItem("Tools/Export Sprites To PNG")]
    static void ExportSelectedSprites() {
        // Verifica que haya algo seleccionado
        Object[] selection = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);

        if (selection.Length == 0) {
            EditorUtility.DisplayDialog(
                "Nada seleccionado",
                "Selecciona primero la textura (PNG) en el Project window antes de usar esta herramienta.",
                "OK"
            );
            return;
        }

        // Pide carpeta de destino
        string outputFolder = EditorUtility.SaveFolderPanel(
            "Elige carpeta de destino para los PNG",
            Application.dataPath,
            ""
        );

        if (string.IsNullOrEmpty(outputFolder)) {
            return; // Usuario canceló
        }

        int totalExported = 0;

        foreach (Object obj in selection) {
            string path = AssetDatabase.GetAssetPath(obj);

            // Carga TODOS los sub-assets (los sprites individuales) de esa textura
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);

            foreach (Object asset in allAssets) {
                if (asset is Sprite sprite) {
                    Texture2D croppedTexture = ExtractSpriteTexture(sprite);

                    if (croppedTexture != null) {
                        byte[] pngData = croppedTexture.EncodeToPNG();
                        string fileName = SanitizeFileName(sprite.name) + ".png";
                        string fullPath = Path.Combine(outputFolder, fileName);

                        File.WriteAllBytes(fullPath, pngData);
                        totalExported++;

                        // Limpieza de memoria (evita acumular texturas temporales)
                        Object.DestroyImmediate(croppedTexture);
                    }
                }
            }
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Exportación completa",
            $"Se exportaron {totalExported} sprites como PNG individuales en:\n{outputFolder}",
            "OK"
        );

        Debug.Log($"[SpriteExporter] Exportados {totalExported} sprites a: {outputFolder}");
    }

    /// <summary>
    /// Extrae el área exacta de un sprite (según su rect) como una textura nueva,
    /// respetando la transparencia original.
    /// </summary>
    static Texture2D ExtractSpriteTexture(Sprite sprite) {
        Texture2D sourceTexture = sprite.texture;

        // Si la textura no es legible (Read/Write no está activado), avisamos y saltamos
        if (!IsTextureReadable(sourceTexture)) {
            Debug.LogWarning(
                $"[SpriteExporter] La textura '{sourceTexture.name}' no tiene 'Read/Write Enabled' activado. " +
                $"Actívalo en el Inspector (Advanced > Read/Write Enabled) y vuelve a intentar."
            );
            return null;
        }

        Rect rect = sprite.rect;
        int x = Mathf.RoundToInt(rect.x);
        int y = Mathf.RoundToInt(rect.y);
        int width = Mathf.RoundToInt(rect.width);
        int height = Mathf.RoundToInt(rect.height);

        Color[] pixels = sourceTexture.GetPixels(x, y, width, height);

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.SetPixels(pixels);
        result.Apply();

        return result;
    }

    static bool IsTextureReadable(Texture2D texture) {
        try {
            texture.GetPixel(0, 0);
            return true;
        }
        catch {
            return false;
        }
    }

    static string SanitizeFileName(string name) {
        // Reemplaza caracteres inválidos para nombres de archivo (por si algún
        // sprite quedó nombrado con símbolos raros, ej: "?" o "/")
        foreach (char invalidChar in Path.GetInvalidFileNameChars()) {
            name = name.Replace(invalidChar, '_');
        }
        return name;
    }
}
