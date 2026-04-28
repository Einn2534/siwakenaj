using UnityEditor;
using UnityEngine;

public sealed class UiSpriteImportPostprocessor : AssetPostprocessor
{
    private const string UiSpriteRoot = "Assets/Art/UI/Sprites/";
    private const float SpritePixelsPerUnit = 100f;
    private const float DefaultNineSliceBorder = 24f;

    private void OnPreprocessTexture()
    {
        string normalizedPath = assetPath.Replace("\\", "/");
        if (!normalizedPath.StartsWith(UiSpriteRoot))
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
            importer.spriteBorder = new Vector4(
                DefaultNineSliceBorder,
                DefaultNineSliceBorder,
                DefaultNineSliceBorder,
                DefaultNineSliceBorder);
        }
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
