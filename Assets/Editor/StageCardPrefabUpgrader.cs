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
        "Pin",
        "StageThumbnail",
        "VehiclePreview",
        "LockOverlay",
        "InfoPanel",
        "StageNumberText",
        "StageNameText",
        "TargetScoreText",
        "BestScoreText",
        "StatusText",
        "ProgressTrack",
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

        rootRect.sizeDelta = new Vector2(840f, 950f);

        Image rootImage = root.GetComponent<Image>() ?? root.AddComponent<Image>();
        rootImage.sprite = null;
        rootImage.color = Color.clear;
        rootImage.raycastTarget = true;

        LayoutElement layoutElement = root.GetComponent<LayoutElement>() ?? root.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 840f;
        layoutElement.preferredHeight = 950f;
        layoutElement.layoutPriority = 1;
        PreferredSizeByParentWidth responsiveSizer = root.GetComponent<PreferredSizeByParentWidth>();
        if (responsiveSizer != null)
        {
            Object.DestroyImmediate(responsiveSizer);
        }

        PruneChildren(root.transform, VisualChildOrder);

        Sprite plainSprite = GetBuiltInUiSprite();
        Sprite frameSprite = LoadSprite("Assets/Resources/UI/Tutorial/speech_panel.png") ?? plainSprite;
        Sprite selectionGlowSprite = GetBuiltInUiSprite();
        Sprite lockOverlaySprite = LoadSprite(StageSelectSpriteRoot + "ui_stageselect_sheet15_item07.png");
        Sprite woodPanelSprite = LoadSprite("Assets/Resources/UI/Tutorial/hud_wood_panel.png");
        Sprite magicShopSprite = LoadSprite("Assets/Art/Sprites/Backgrounds/magic_shop_background.png");
        Sprite vehicleSprite = LoadSprite("Assets/Art/Sprites/Vehicles/truck.png");
        Sprite filledStarSprite = LoadSprite(StageSelectSpriteRoot + "ui_stage_star_filled.png");
        Sprite emptyStarSprite = LoadSprite(StageSelectSpriteRoot + "ui_stage_star_empty.png");
        Sprite[] thumbnailSprites =
        {
            magicShopSprite,
            magicShopSprite,
            magicShopSprite
        };

        Image selectionGlow = UpsertImage(root.transform, "SelectionGlow", selectionGlowSprite, new Vector2(0f, -8f), new Vector2(860f, 970f), false);
        selectionGlow.type = Image.Type.Sliced;
        selectionGlow.color = Color.clear;
        selectionGlow.gameObject.SetActive(false);

        Image frame = UpsertImage(root.transform, "Frame", frameSprite, Vector2.zero, new Vector2(840f, 950f), false);
        frame.type = Image.Type.Sliced;
        frame.color = new Color(1f, 0.992f, 0.965f, 1f);
        Shadow paperShadow = frame.GetComponent<Shadow>() ?? frame.gameObject.AddComponent<Shadow>();
        paperShadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        paperShadow.effectDistance = new Vector2(0f, -24f);
        paperShadow.useGraphicAlpha = true;

        Image pin = UpsertImage(root.transform, "Pin", plainSprite, new Vector2(0f, 458f), new Vector2(60f, 60f), false);
        pin.type = Image.Type.Sliced;
        pin.color = new Color(1f, 0.851f, 0.29f, 1f);

        Image thumbnail = UpsertImage(root.transform, "StageThumbnail", thumbnailSprites[0], new Vector2(0f, 105f), new Vector2(720f, 420f), false);
        Outline thumbnailOutline = thumbnail.GetComponent<Outline>() ?? thumbnail.gameObject.AddComponent<Outline>();
        thumbnailOutline.effectColor = new Color(0.169f, 0.145f, 0.188f, 1f);
        thumbnailOutline.effectDistance = new Vector2(6f, -6f);
        Image vehiclePreview = UpsertImage(root.transform, "VehiclePreview", vehicleSprite, new Vector2(0f, 60f), new Vector2(260f, 170f), true);
        Image lockOverlay = UpsertImage(root.transform, "LockOverlay", lockOverlaySprite, new Vector2(0f, 105f), new Vector2(720f, 420f), false);
        lockOverlay.color = new Color(1f, 1f, 1f, 0.48f);
        lockOverlay.gameObject.SetActive(false);

        Image infoPanel = UpsertImage(root.transform, "InfoPanel", frameSprite, new Vector2(0f, -205f), new Vector2(720f, 155f), false);
        infoPanel.type = Image.Type.Sliced;
        infoPanel.color = new Color(0.43f, 0.25f, 0.13f, 1f);

        TMP_FontAsset headlineFont = LoadFont("Assets/Fonts/Y1YomiyasuWide-Bold SDF.asset");
        TMP_FontAsset bodyFont = LoadFont("Assets/Fonts/DotGothic16-Regular SDF.asset");

        TMP_Text stageNumberText = UpsertText(
            root.transform,
            "StageNumberText",
            "おしごと 1",
            new Vector2(-150f, 382f),
            new Vector2(320f, 70f),
            headlineFont,
            48f,
            32f,
            new Color(0.169f, 0.145f, 0.188f, 1f));
        stageNumberText.alignment = TextAlignmentOptions.MidlineLeft;

        TMP_Text stageNameText = UpsertText(
            root.transform,
            "StageNameText",
            "はじまりの街",
            new Vector2(170f, 382f),
            new Vector2(280f, 60f),
            bodyFont,
            30f,
            20f,
            new Color(0.169f, 0.145f, 0.188f, 0.45f));
        stageNameText.alignment = TextAlignmentOptions.MidlineRight;

        TMP_Text targetScoreText = UpsertText(
            root.transform,
            "TargetScoreText",
            "目標 60台",
            new Vector2(-150f, -180f),
            new Vector2(280f, 54f),
            bodyFont,
            38f,
            26f,
            new Color(1f, 0.968f, 0.918f, 1f));
        targetScoreText.alignment = TextAlignmentOptions.MidlineLeft;

        TMP_Text bestScoreText = UpsertText(
            root.transform,
            "BestScoreText",
            "ベスト -",
            new Vector2(150f, -180f),
            new Vector2(280f, 54f),
            bodyFont,
            35f,
            24f,
            new Color(1f, 0.902f, 0.412f, 1f));
        bestScoreText.alignment = TextAlignmentOptions.MidlineRight;

        TMP_Text statusText = UpsertText(
            root.transform,
            "StatusText",
            "LOCKED",
            new Vector2(0f, -65f),
            new Vector2(560f, 70f),
            headlineFont,
            30f,
            22f,
            new Color(0.25f, 0.22f, 0.20f, 0.78f));
        statusText.gameObject.SetActive(false);

        TMP_Text starBadgeText = UpsertText(
            root.transform,
            "StarBadgeText",
            string.Empty,
            new Vector2(0f, -370f),
            new Vector2(280f, 60f),
            bodyFont,
            28f,
            18f,
            Color.white);
        starBadgeText.gameObject.SetActive(false);

        Image[] starImages =
        {
            UpsertImage(root.transform, "Star01", emptyStarSprite, new Vector2(-140f, -370f), new Vector2(86f, 86f), true),
            UpsertImage(root.transform, "Star02", emptyStarSprite, new Vector2(0f, -370f), new Vector2(86f, 86f), true),
            UpsertImage(root.transform, "Star03", emptyStarSprite, new Vector2(140f, -370f), new Vector2(86f, 86f), true)
        };

        Image progressTrack = UpsertImage(root.transform, "ProgressTrack", plainSprite, new Vector2(0f, -235f), new Vector2(650f, 20f), false);
        progressTrack.type = Image.Type.Sliced;
        progressTrack.color = new Color(0.20f, 0.13f, 0.08f, 0.45f);
        GameObject fillObject = GetOrCreateChild(progressTrack.transform, "Fill");
        EnsureComponent<CanvasRenderer>(fillObject);
        Image progressFill = EnsureComponent<Image>(fillObject);
        RectTransform fillRect = EnsureRectTransform(fillObject);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);
        progressFill.sprite = plainSprite;
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 1f;
        progressFill.color = new Color(0.48f, 0.91f, 0.61f, 1f);
        progressFill.raycastTarget = false;
        SetSiblingOrder(root.transform, VisualChildOrder);

        SerializedObject serializedCard = new SerializedObject(cardView);
        SetObjectReference(serializedCard, "_stageNumberText", stageNumberText);
        SetObjectReference(serializedCard, "_targetScoreText", targetScoreText);
        SetObjectReference(serializedCard, "_bestScoreText", bestScoreText);
        SetObjectReference(serializedCard, "_statusText", statusText);
        SetObjectReference(serializedCard, "_starBadgeText", starBadgeText);
        SetObjectReference(serializedCard, "_stageNameText", stageNameText);
        SetObjectReference(serializedCard, "_frameImage", frame);
        SetObjectReference(serializedCard, "_thumbnailImage", thumbnail);
        SetObjectReference(serializedCard, "_lockOverlayImage", lockOverlay);
        SetObjectReference(serializedCard, "_selectionGlowImage", selectionGlow);
        SetObjectReference(serializedCard, "_vehiclePreviewImage", vehiclePreview);
        SetObjectReference(serializedCard, "_progressFillImage", progressFill);
        SetObjectReference(serializedCard, "_infoPanelImage", infoPanel);
        SetObjectReference(serializedCard, "_pinImage", pin);
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
