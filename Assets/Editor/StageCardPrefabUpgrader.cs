using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class StageCardPrefabUpgrader
{
    private const string PrefabPath = "Assets/Prefabs/StageCard.prefab";
    private const string StageSelectSpriteRoot = "Assets/Art/UI/Sprites/StageSelect/";
    private static readonly string[] VisualChildOrder =
    {
        "SelectionGlow",
        "Frame",
        "StageThumbnail",
        "LockOverlay",
        "InfoPanel",
        "StageNumberText",
        "TargetScoreText",
        "BestScoreText",
        "StatusText",
        "StarBadgeText",
        "Star01",
        "Star02",
        "Star03"
    };

    [MenuItem("Tools/UI/Upgrade Stage Card Prefab")]
    public static void UpgradeStageCardPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            Debug.LogError($"[StageCardPrefabUpgrader] Missing prefab: {PrefabPath}");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"[StageCardPrefabUpgrader] Failed to load prefab contents: {PrefabPath}");
            return;
        }

        try
        {
            if (!Upgrade(prefabRoot))
            {
                return;
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[StageCardPrefabUpgrader] Upgraded {PrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static bool Upgrade(GameObject root)
    {
        StageCardView cardView = root.GetComponent<StageCardView>() ?? root.AddComponent<StageCardView>();
        RectTransform rootRect = root.GetComponent<RectTransform>();
        if (rootRect == null)
        {
            Debug.LogError($"[StageCardPrefabUpgrader] Prefab root must have a RectTransform: {PrefabPath}");
            return false;
        }

        rootRect.sizeDelta = new Vector2(760f, 980f);

        Image rootImage = root.GetComponent<Image>() ?? root.AddComponent<Image>();
        rootImage.sprite = null;
        rootImage.color = Color.clear;
        rootImage.raycastTarget = true;

        LayoutElement layoutElement = root.GetComponent<LayoutElement>() ?? root.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 720f;
        layoutElement.preferredHeight = 928f;
        layoutElement.layoutPriority = 1;

        PruneChildren(root.transform, VisualChildOrder);

        Sprite frameSprite = LoadSprite(StageSelectSpriteRoot + "ui_stageselect_sheet15_item01.png");
        Sprite selectionGlowSprite = LoadSprite(StageSelectSpriteRoot + "ui_stageselect_sheet15_item08.png");
        Sprite lockOverlaySprite = LoadSprite(StageSelectSpriteRoot + "ui_stageselect_sheet15_item07.png");
        Sprite filledStarSprite = LoadSprite(StageSelectSpriteRoot + "ui_stage_star_filled.png");
        Sprite emptyStarSprite = LoadSprite(StageSelectSpriteRoot + "ui_stage_star_empty.png");
        Sprite[] thumbnailSprites =
        {
            LoadSprite(StageSelectSpriteRoot + "ui_stage_thumb_city.png"),
            LoadSprite(StageSelectSpriteRoot + "ui_stage_thumb_overpass.png"),
            LoadSprite(StageSelectSpriteRoot + "ui_stage_thumb_crane.png")
        };

        Image selectionGlow = UpsertImage(root.transform, "SelectionGlow", selectionGlowSprite, Vector2.zero, new Vector2(650f, 820f), false);
        selectionGlow.gameObject.SetActive(false);

        Image frame = UpsertImage(root.transform, "Frame", frameSprite, Vector2.zero, new Vector2(640f, 920f), false);
        Image thumbnail = UpsertImage(root.transform, "StageThumbnail", thumbnailSprites[0], new Vector2(0f, 145f), new Vector2(500f, 500f), true);
        Image lockOverlay = UpsertImage(root.transform, "LockOverlay", lockOverlaySprite, new Vector2(0f, 80f), new Vector2(500f, 610f), true);
        lockOverlay.gameObject.SetActive(false);

        Image infoPanel = UpsertImage(root.transform, "InfoPanel", GetBuiltInUiSprite(), new Vector2(0f, -260f), new Vector2(560f, 260f), false);
        infoPanel.type = Image.Type.Sliced;
        infoPanel.color = new Color(0.015f, 0.09f, 0.15f, 0.88f);

        TMP_FontAsset headlineFont = LoadFont("Assets/Fonts/Y1BroadBlack SDF.asset");
        TMP_FontAsset bodyFont = LoadFont("Assets/Fonts/Y1YomiyasuWide-Bold SDF.asset");

        TMP_Text stageNumberText = UpsertText(
            root.transform,
            "StageNumberText",
            "STAGE <color=#35D7FF>01</color>",
            new Vector2(0f, -165f),
            new Vector2(520f, 82f),
            headlineFont,
            56f,
            36f,
            Color.white);

        TMP_Text targetScoreText = UpsertText(
            root.transform,
            "TargetScoreText",
            "<color=#FFD84D>TARGET</color>  0",
            new Vector2(0f, -240f),
            new Vector2(500f, 56f),
            bodyFont,
            34f,
            22f,
            Color.white);

        TMP_Text bestScoreText = UpsertText(
            root.transform,
            "BestScoreText",
            "<color=#FFE05D>BEST</color>  -",
            new Vector2(0f, -296f),
            new Vector2(500f, 56f),
            bodyFont,
            34f,
            22f,
            Color.white);

        TMP_Text statusText = UpsertText(
            root.transform,
            "StatusText",
            "LOCKED",
            new Vector2(0f, 105f),
            new Vector2(500f, 110f),
            headlineFont,
            54f,
            30f,
            new Color(1f, 0.93f, 0.62f, 1f));
        statusText.gameObject.SetActive(false);

        TMP_Text starBadgeText = UpsertText(
            root.transform,
            "StarBadgeText",
            string.Empty,
            new Vector2(0f, -390f),
            new Vector2(280f, 60f),
            bodyFont,
            28f,
            18f,
            Color.white);
        starBadgeText.gameObject.SetActive(false);

        Image[] starImages =
        {
            UpsertImage(root.transform, "Star01", emptyStarSprite, new Vector2(-120f, -404f), new Vector2(88f, 88f), true),
            UpsertImage(root.transform, "Star02", emptyStarSprite, new Vector2(0f, -404f), new Vector2(88f, 88f), true),
            UpsertImage(root.transform, "Star03", emptyStarSprite, new Vector2(120f, -404f), new Vector2(88f, 88f), true)
        };
        SetSiblingOrder(root.transform, VisualChildOrder);

        SerializedObject serializedCard = new SerializedObject(cardView);
        SetObjectReference(serializedCard, "_stageNumberText", stageNumberText);
        SetObjectReference(serializedCard, "_targetScoreText", targetScoreText);
        SetObjectReference(serializedCard, "_bestScoreText", bestScoreText);
        SetObjectReference(serializedCard, "_statusText", statusText);
        SetObjectReference(serializedCard, "_starBadgeText", starBadgeText);
        SetObjectReference(serializedCard, "_frameImage", frame);
        SetObjectReference(serializedCard, "_thumbnailImage", thumbnail);
        SetObjectReference(serializedCard, "_lockOverlayImage", lockOverlay);
        SetObjectReference(serializedCard, "_selectionGlowImage", selectionGlow);
        SetObjectReference(serializedCard, "_filledStarSprite", filledStarSprite);
        SetObjectReference(serializedCard, "_emptyStarSprite", emptyStarSprite);
        SetObjectArray(serializedCard.FindProperty("_starImages"), starImages);
        SetObjectArray(serializedCard.FindProperty("_stageThumbnailSprites"), thumbnailSprites);
        serializedCard.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    private static Image UpsertImage(Transform parent, string name, Sprite sprite, Vector2 anchoredPosition, Vector2 size, bool preserveAspect)
    {
        GameObject gameObject = GetOrCreateChild(parent, name);
        EnsureComponent<CanvasRenderer>(gameObject);
        Image image = EnsureComponent<Image>(gameObject);
        gameObject.SetActive(true);
        gameObject.layer = parent.gameObject.layer;

        RectTransform rectTransform = EnsureRectTransform(gameObject);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        image.color = Color.white;
        return image;
    }

    private static TMP_Text UpsertText(
        Transform parent,
        string name,
        string text,
        Vector2 anchoredPosition,
        Vector2 size,
        TMP_FontAsset font,
        float fontSizeMax,
        float fontSizeMin,
        Color color)
    {
        GameObject gameObject = GetOrCreateChild(parent, name);
        EnsureComponent<CanvasRenderer>(gameObject);
        TextMeshProUGUI textComponent = EnsureComponent<TextMeshProUGUI>(gameObject);
        gameObject.SetActive(true);
        gameObject.layer = parent.gameObject.layer;

        RectTransform rectTransform = EnsureRectTransform(gameObject);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        textComponent.text = text;
        textComponent.font = font != null ? font : TMP_Settings.defaultFontAsset;
        textComponent.fontSize = fontSizeMax;
        textComponent.fontSizeMax = fontSizeMax;
        textComponent.fontSizeMin = fontSizeMin;
        textComponent.enableAutoSizing = true;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = color;
        textComponent.raycastTarget = false;
        textComponent.richText = true;
        textComponent.textWrappingMode = TextWrappingModes.NoWrap;
        return textComponent;
    }

    private static GameObject GetOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child.gameObject;
        }

        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static RectTransform EnsureRectTransform(GameObject gameObject)
    {
        RectTransform rectTransform = gameObject.transform as RectTransform;
        if (rectTransform != null)
        {
            return rectTransform;
        }

        Debug.LogError($"[StageCardPrefabUpgrader] Child must use RectTransform: {gameObject.name}");
        return gameObject.AddComponent<RectTransform>();
    }

    private static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
    }

    private static void PruneChildren(Transform parent, IReadOnlyCollection<string> expectedNames)
    {
        HashSet<string> expectedNameSet = new HashSet<string>(expectedNames);
        HashSet<string> seenNames = new HashSet<string>();
        List<GameObject> childrenToRemove = new List<GameObject>();
        for (int i = 0; i < parent.childCount; i += 1)
        {
            Transform child = parent.GetChild(i);
            if (!expectedNameSet.Contains(child.name) || !seenNames.Add(child.name))
            {
                childrenToRemove.Add(child.gameObject);
            }
        }

        foreach (GameObject child in childrenToRemove)
        {
            Object.DestroyImmediate(child);
        }
    }

    private static void SetSiblingOrder(Transform parent, IReadOnlyList<string> orderedNames)
    {
        for (int i = 0; i < orderedNames.Count; i += 1)
        {
            Transform child = parent.Find(orderedNames[i]);
            if (child != null)
            {
                child.SetSiblingIndex(i);
            }
        }
    }

    private static void SetObjectArray(SerializedProperty property, Object[] values)
    {
        if (property == null)
        {
            Debug.LogWarning("[StageCardPrefabUpgrader] Missing serialized array property.");
            return;
        }

        values ??= new Object[0];
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i += 1)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"[StageCardPrefabUpgrader] Missing serialized property: {propertyName}");
            return;
        }

        property.objectReferenceValue = value;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogWarning($"[StageCardPrefabUpgrader] Missing sprite: {path}");
        }

        return sprite;
    }

    private static TMP_FontAsset LoadFont(string path)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font == null)
        {
            Debug.LogWarning($"[StageCardPrefabUpgrader] Missing font: {path}");
        }

        return font;
    }

    private static Sprite GetBuiltInUiSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }
}
