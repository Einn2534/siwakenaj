using System.IO;
using UnityEditor;
using UnityEngine;

public static class UnityAdsSettingsAssetCreator
{
    private const string ResourcesDirectory = "Assets/Resources";
    private const string SettingsAssetPath = ResourcesDirectory + "/UnityAdsSettings.asset";

    [MenuItem("Siwakenja/Ads/Create Unity Ads Settings")]
    public static void CreateSettingsAsset()
    {
        if (!Directory.Exists(ResourcesDirectory))
        {
            Directory.CreateDirectory(ResourcesDirectory);
        }

        UnityAdsSettings settings = AssetDatabase.LoadAssetAtPath<UnityAdsSettings>(SettingsAssetPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<UnityAdsSettings>();
            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            AssetDatabase.SaveAssets();
        }

        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }
}
