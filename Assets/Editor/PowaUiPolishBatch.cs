using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PowaUiPolishBatch
{
    private const string MainScenePath = "Assets/Scenes/Main.unity";

    public static void ApplyAllFromBatchMode()
    {
        StageCardPrefabUpgrader.UpgradeStageCardPrefab();
        TitleSceneLayoutBuilder.BuildFromBatchMode();
        ResultSceneLayoutBuilder.BuildFromBatchMode();
        PolishMainScene();
        AssetDatabase.SaveAssets();
        Debug.Log("[PowaUiPolishBatch] UI polish applied.");
    }

    private static void PolishMainScene()
    {
        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        RectTransform topZone = FindRect("TopZone");
        if (topZone != null)
        {
            SetTopAnchoredFullWidth(topZone, 54f, 640f);
            SetImageAlphaAndRaycast(topZone, 1f, true);
        }

        RectTransform frameImage = FindRect("FrameImage");
        if (frameImage != null)
        {
            Stretch(frameImage, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -160f));
            SetImageRaycast(frameImage, true);
        }

        RectTransform buttonZone = FindRect("ButtonZone");
        if (buttonZone != null)
        {
            SetAnchored(buttonZone, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 468f));
            HorizontalLayoutGroup layout = buttonZone.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.padding = new RectOffset(34, 34, 12, 34);
                layout.spacing = 20f;
                layout.childAlignment = TextAnchor.LowerCenter;
            }
        }

        RectTransform playZone = FindRect("PlayZone");
        if (playZone != null)
        {
            Stretch(playZone, Vector2.zero, Vector2.one, new Vector2(0f, 540f), new Vector2(0f, -694f));
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static RectTransform FindRect(string name)
    {
        foreach (RectTransform rectTransform in UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (rectTransform != null && rectTransform.name == name)
            {
                return rectTransform;
            }
        }

        return null;
    }

    private static void SetImageAlphaAndRaycast(RectTransform rectTransform, float alpha, bool raycastTarget)
    {
        Image image = rectTransform.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = alpha;
        image.color = color;
        image.raycastTarget = raycastTarget;
    }

    private static void SetImageRaycast(RectTransform rectTransform, bool raycastTarget)
    {
        Image image = rectTransform.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        image.raycastTarget = raycastTarget;
    }

    private static void SetAnchored(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;
    }

    private static void SetTopAnchoredFullWidth(RectTransform rectTransform, float topInset, float height)
    {
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.offsetMin = new Vector2(0f, -topInset - height);
        rectTransform.offsetMax = new Vector2(0f, -topInset);
        rectTransform.localScale = Vector3.one;
    }
}
