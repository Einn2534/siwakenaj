using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class StageCardPrefabUpgrader
{
    private const string PrefabPath = "Assets/Prefabs/StageCard.prefab";
    private const string StageSelectSpriteRoot = "Assets/Art/UI/Sprites/StageSelect/";
    private const string ResultSpriteRoot = "Assets/Art/UI/Sprites/Result/";

    public static void UpgradeStageCardPrefab()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Upgrade(prefabRoot);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[StageCardPrefabUpgrader] Upgraded {PrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void Upgrade(GameObject root)
    {
        StageCardView cardView = root.GetComponent<StageCardView>() ?? root.AddComponent<StageCardView>();
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(760f, 980f);

        Image rootImage = root.GetComponent<Image>() ?? root.AddComponent<Image>();
        rootImage.sprite = null;
        rootImage.color = Color.clear;
        rootImage.raycastTarget = true;

        LayoutElement layoutElement = root.GetComponent<LayoutElement>() ?? root.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 720f;
        layoutElement.preferredHeight = 928f;
        layoutElement.layoutPriority = 1;

        ClearChildren(root.transform);

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

        Image selectionGlow = CreateImage(root.transform, "SelectionGlow", selectionGlowSprite, Vector2.zero, new Vector2(650f, 820f), false);
        selectionGlow.gameObject.SetActive(false);

        Image frame = CreateImage(root.transform, "Frame", frameSprite, Vector2.zero, new Vector2(640f, 920f), false);
        Image thumbnail = CreateImage(root.transform, "StageThumbnail", thumbnailSprites[0], new Vector2(0f, 145f), new Vector2(500f, 500f), true);
        Image lockOverlay = CreateImage(root.transform, "LockOverlay", lockOverlaySprite, new Vector2(0f, 80f), new Vector2(500f, 610f), true);
        lockOverlay.gameObject.SetActive(false);

        Image infoPanel = CreateImage(root.transform, "InfoPanel", GetBuiltInUiSprite(), new Vector2(0f, -260f), new Vector2(560f, 260f), false);
        infoPanel.type = Image.Type.Sliced;
        infoPanel.color = new Color(0.015f, 0.09f, 0.15f, 0.88f);

        TMP_FontAsset headlineFont = LoadFont("Assets/Fonts/Y1BroadBlack SDF.asset");
        TMP_FontAsset bodyFont = LoadFont("Assets/Fonts/Y1YomiyasuWide-Bold SDF.asset");

        TMP_Text stageNumberText = CreateText(
            root.transform,
            "StageNumberText",
            "STAGE <color=#35D7FF>01</color>",
            new Vector2(0f, -165f),
            new Vector2(520f, 82f),
            headlineFont,
            56f,
            36f,
            Color.white);

        TMP_Text targetScoreText = CreateText(
            root.transform,
            "TargetScoreText",
            "<color=#FFD84D>TARGET</color>  0",
            new Vector2(0f, -240f),
            new Vector2(500f, 56f),
            bodyFont,
            34f,
            22f,
            Color.white);

        TMP_Text bestScoreText = CreateText(
            root.transform,
            "BestScoreText",
            "<color=#FFE05D>BEST</color>  -",
            new Vector2(0f, -296f),
            new Vector2(500f, 56f),
            bodyFont,
            34f,
            22f,
            Color.white);

        TMP_Text statusText = CreateText(
            root.transform,
            "StatusText",
            "LOCKED",
            new Vector2(0f, -250f),
            new Vector2(500f, 90f),
            headlineFont,
            46f,
            28f,
            new Color(1f, 0.93f, 0.62f, 1f));
        statusText.gameObject.SetActive(false);

        TMP_Text starBadgeText = CreateText(
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
            CreateImage(root.transform, "Star01", emptyStarSprite, new Vector2(-120f, -404f), new Vector2(88f, 88f), true),
            CreateImage(root.transform, "Star02", emptyStarSprite, new Vector2(0f, -404f), new Vector2(88f, 88f), true),
            CreateImage(root.transform, "Star03", emptyStarSprite, new Vector2(120f, -404f), new Vector2(88f, 88f), true)
        };

        SerializedObject serializedCard = new SerializedObject(cardView);
        serializedCard.FindProperty("_stageNumberText").objectReferenceValue = stageNumberText;
        serializedCard.FindProperty("_targetScoreText").objectReferenceValue = targetScoreText;
        serializedCard.FindProperty("_bestScoreText").objectReferenceValue = bestScoreText;
        serializedCard.FindProperty("_statusText").objectReferenceValue = statusText;
        serializedCard.FindProperty("_starBadgeText").objectReferenceValue = starBadgeText;
        serializedCard.FindProperty("_frameImage").objectReferenceValue = frame;
        serializedCard.FindProperty("_thumbnailImage").objectReferenceValue = thumbnail;
        serializedCard.FindProperty("_lockOverlayImage").objectReferenceValue = lockOverlay;
        serializedCard.FindProperty("_selectionGlowImage").objectReferenceValue = selectionGlow;
        serializedCard.FindProperty("_filledStarSprite").objectReferenceValue = filledStarSprite;
        serializedCard.FindProperty("_emptyStarSprite").objectReferenceValue = emptyStarSprite;
        SetObjectArray(serializedCard.FindProperty("_starImages"), starImages);
        SetObjectArray(serializedCard.FindProperty("_stageThumbnailSprites"), thumbnailSprites);
        serializedCard.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 anchoredPosition, Vector2 size, bool preserveAspect)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameObject.layer = parent.gameObject.layer;
        gameObject.transform.SetParent(parent, false);

        RectTransform rectTransform = (RectTransform)gameObject.transform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = gameObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        image.color = Color.white;
        return image;
    }

    private static TMP_Text CreateText(
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
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        gameObject.layer = parent.gameObject.layer;
        gameObject.transform.SetParent(parent, false);

        RectTransform rectTransform = (RectTransform)gameObject.transform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI textComponent = gameObject.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.font = font;
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

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i -= 1)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    private static void SetObjectArray(SerializedProperty property, Object[] values)
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i += 1)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
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
