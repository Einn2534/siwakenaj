using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class ReadableTypographySetup
{
    private const string FontPath = "Assets/Resources/Fonts/NotoSansCJKjp-Regular SDF.asset";

    [MenuItem("Tools/Shiwa Kenja/Apply Readable Typography")]
    public static void Apply()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansCJKjp-Regular.otf");
            font = TMP_FontAsset.CreateFontAsset(source, 48, 6, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            font.name = "NotoSansCJKjp-Regular SDF";
            AssetDatabase.CreateAsset(font, FontPath);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var texture in font.atlasTextures)
                AssetDatabase.AddObjectToAsset(texture, font);
        }

        var settings = new SerializedObject(TMP_Settings.instance);
        settings.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
        settings.ApplyModifiedPropertiesWithoutUndo();

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (ApplyTo(root, font)) PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            {
                var scene = EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guid));
                bool changed = false;
                foreach (var root in scene.GetRootGameObjects()) changed |= ApplyTo(root, font);
                if (changed) EditorSceneManager.SaveScene(scene);
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        Debug.Log("Readable typography applied to UI scenes, prefabs and default font.");
    }

    private static bool ApplyTo(GameObject root, TMP_FontAsset font)
    {
        var texts = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (var text in texts)
        {
            float outline = Mathf.Min(text.outlineWidth, 0.045f);
            Color outlineColor = text.outlineColor;
            text.font = font;
            text.fontSharedMaterial = font.material;
            text.fontStyle &= ~FontStyles.Bold;
            text.fontWeight = FontWeight.Regular;
            // Keep contrasting outlines only where the existing design uses them.
            if (outline > 0f)
            {
                var material = new Material(font.material);
                material.name = "Readable UI Outline";
                string path = "Assets/Resources/Fonts/NotoSansCJKjp-Outline.mat";
                var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (existing == null)
                {
                    material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.045f);
                    material.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
                    AssetDatabase.CreateAsset(material, path);
                    existing = material;
                }
                else Object.DestroyImmediate(material);
                text.fontSharedMaterial = existing;
            }
            EditorUtility.SetDirty(text);
        }
        return texts.Length > 0;
    }
}
