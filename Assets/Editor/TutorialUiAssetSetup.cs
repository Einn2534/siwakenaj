#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class TutorialUiAssetSetup
{
    private const string TutorialResourceFolder = "Assets/Resources/UI/Tutorial";

    [MenuItem("Tools/Shiwa Kenja/Configure Tutorial UI Assets")]
    public static void Configure()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TutorialResourceFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;

            string fileName = Path.GetFileNameWithoutExtension(path);
            importer.spriteBorder = fileName is "hud_wood_panel" or "speech_panel"
                ? new Vector4(60f, 60f, 60f, 60f)
                : Vector4.zero;

            importer.SaveAndReimport();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[TutorialUiAssetSetup] Tutorial UI assets configured.");
    }
}
#endif
