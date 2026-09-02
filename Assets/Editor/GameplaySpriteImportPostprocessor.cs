using UnityEditor;
using UnityEngine;

public sealed class GameplaySpriteImportPostprocessor : AssetPostprocessor
{
    private const string CoveredVehiclePath = "Assets/Resources/Gameplay/covered_vehicle.png";
    private const float CoveredVehicleWorldWidth = 9.9f;

    private void OnPreprocessTexture()
    {
        string normalizedPath = assetPath.Replace('\\', '/');
        if (normalizedPath != CoveredVehiclePath)
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.GetSourceTextureWidthAndHeight(out int width, out _);
        if (width > 0)
        {
            importer.spritePixelsPerUnit = width / CoveredVehicleWorldWidth;
        }

        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.maxTextureSize = 2048;
    }
}
