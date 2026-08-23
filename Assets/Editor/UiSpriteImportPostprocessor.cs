using UnityEditor;
using UnityEngine;

public sealed class UiSpriteImportPostprocessor : AssetPostprocessor
{
    private const string UiSpriteRoot = "Assets/Art/UI/Sprites/";
    private const string RuntimePauseSpriteRoot = "Assets/Resources/UI/Pause/";
    private const float SpritePixelsPerUnit = 100f;
    private const float DefaultNineSliceBorder = 24f;

    private void OnPreprocessTexture()
    {
        string normalizedPath = assetPath.Replace("\\", "/");
        if (!normalizedPath.StartsWith(UiSpriteRoot)
            && !normalizedPath.StartsWith(RuntimePauseSpriteRoot))
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = SpritePixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Compressed;

        if (ShouldUseNineSlice(normalizedPath))
        {
            float border = GetNineSliceBorder(normalizedPath);
            importer.spriteBorder = new Vector4(
                border,
                border,
                border,
                border);
        }
    }

    private static float GetNineSliceBorder(string normalizedPath)
    {
        if (normalizedPath.EndsWith("Settings/ui_settings_panel_frame.png", System.StringComparison.OrdinalIgnoreCase))
        {
            return 52f;
        }

        if (normalizedPath.EndsWith("Settings/ui_settings_back_button.png", System.StringComparison.OrdinalIgnoreCase))
        {
            return 64f;
        }

        if (normalizedPath.EndsWith("Pause/ui_pause_primary_button.png", System.StringComparison.OrdinalIgnoreCase))
        {
            return 64f;
        }

        if (normalizedPath.EndsWith("Pause/ui_pause_secondary_button.png", System.StringComparison.OrdinalIgnoreCase))
        {
            return 48f;
        }

        if (normalizedPath.EndsWith("Pause/ui_pause_card.png", System.StringComparison.OrdinalIgnoreCase))
        {
            return 52f;
        }

        if (normalizedPath.EndsWith("Result/Legacy/card_bg_soft.png", System.StringComparison.OrdinalIgnoreCase))
        {
            return 42f;
        }

        return DefaultNineSliceBorder;
    }

    private static bool ShouldUseNineSlice(string normalizedPath)
    {
        string fileName = System.IO.Path.GetFileNameWithoutExtension(normalizedPath).ToLowerInvariant();
        return fileName.Contains("button")
            || fileName.Contains("panel")
            || fileName.Contains("card")
            || fileName.Contains("frame")
            || fileName.Contains("bubble")
            || fileName.Contains("bg");
    }
}

public static class UiSpriteBatchImporter
{
    private const string UiAssetRoot = "Assets/Art/UI";

    public static void ImportUiSprites()
    {
        AssetDatabase.ImportAsset(
            UiAssetRoot,
            ImportAssetOptions.ImportRecursive
                | ImportAssetOptions.ForceUpdate
                | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.SaveAssets();
        Debug.Log($"[UiSpriteBatchImporter] Imported {UiAssetRoot}");
    }
}
