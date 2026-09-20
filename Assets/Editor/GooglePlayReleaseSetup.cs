using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class GooglePlayReleaseSetup
{
    [MenuItem("Siwakenja/Build/Apply Google Play Identity and Icon")]
    public static void Apply()
    {
        const string iconPath = "Assets/Art/Release/app-icon.png";
        AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(iconPath);
        importer.textureType = TextureImporterType.Default;
        importer.maxTextureSize = 512;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        if (icon == null)
            throw new BuildFailedException("Release app icon could not be loaded.");

        string currentId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        if (string.IsNullOrWhiteSpace(currentId) || currentId.Contains("DefaultCompany"))
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.kibundeseisakujo.siwakenja");
        PlayerSettings.companyName = "気分で製作所";
        PlayerSettings.productName = "しわ賢者";
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
        AssetDatabase.SaveAssets();
        Debug.Log("Google Play identity and default app icon configured. Signing and store verification are still required.");
    }
}
