using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TitleController : MonoBehaviour
{
    private const string StageSelectScene = "StageSelect";
    private const string MainScene = "Main";
    private const string StageDatabaseResourcePath = "StageDatabase";

    [SerializeField, FormerlySerializedAs("howToPanel")]
    private GameObject _howToPanel;

    [SerializeField, FormerlySerializedAs("settingsPanel")]
    private GameObject _settingsPanel;

    [SerializeField]
    private HowToOverlayController _howToOverlayController;

    private StageDatabase _stageDatabase;

    private void Start()
    {
        SoundManager.EnsureInstance().PlayTitleBgm();
        _stageDatabase = Resources.Load<StageDatabase>(StageDatabaseResourcePath);
        SaveService.EnsureProgressMigration(_stageDatabase);
        BuildModeButtons();
    }

    public void OnStartPressed()
    {
        SceneManager.LoadScene(StageSelectScene);
    }

    public void OnDailyPressed()
    {
        if (!SaveService.IsExtendedModeUnlocked())
        {
            return;
        }

        int sourceStage = SaveService.GetHighestClearedStageNumber(_stageDatabase);
        StageSelectionService.SelectDaily(sourceStage);
        SceneManager.LoadScene(MainScene);
    }

    public void OnEndlessPressed()
    {
        if (!SaveService.IsExtendedModeUnlocked())
        {
            return;
        }

        int sourceStage = SaveService.GetHighestClearedStageNumber(_stageDatabase);
        StageSelectionService.SelectEndless(sourceStage);
        SceneManager.LoadScene(MainScene);
    }

    private void BuildModeButtons()
    {
        Button campaignButton = GameObject.Find("StageSelectButton")?.GetComponent<Button>();
        if (campaignButton == null || campaignButton.transform.parent is not RectTransform root)
        {
            return;
        }

        if (root.Find("DailyButton") != null)
        {
            return;
        }

        HorizontalLayoutGroup horizontal = root.GetComponent<HorizontalLayoutGroup>();
        if (horizontal != null)
        {
            horizontal.enabled = false;
        }

        GameObject columnObject = new("ModeButtonColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
        RectTransform column = columnObject.GetComponent<RectTransform>();
        column.SetParent(root, false);
        column.anchorMin = Vector2.zero;
        column.anchorMax = Vector2.one;
        column.offsetMin = Vector2.zero;
        column.offsetMax = Vector2.zero;
        campaignButton.transform.SetParent(column, false);
        VerticalLayoutGroup vertical = columnObject.GetComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(132, 132, 10, 18);
        vertical.spacing = 12f;
        vertical.childAlignment = TextAnchor.LowerCenter;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = false;
        vertical.childForceExpandHeight = false;
        root.sizeDelta = new Vector2(1080f, 526f);
        root.anchoredPosition = new Vector2(0f, 52f);

        Button dailyButton = Instantiate(campaignButton, column, false);
        dailyButton.name = "DailyButton";
        Button endlessButton = Instantiate(campaignButton, column, false);
        endlessButton.name = "EndlessButton";

        ConfigureModeButton(campaignButton, "おしごと", "15 STAGES", true, OnStartPressed);
        bool extendedUnlocked = SaveService.IsExtendedModeUnlocked();
        string lockedDetail = "おしごと5クリアで解放";
        ConfigureModeButton(dailyButton, "デイリー", extendedUnlocked ? "JST / 60 SECONDS" : lockedDetail, extendedUnlocked, OnDailyPressed);
        ConfigureModeButton(endlessButton, "エンドレス", extendedUnlocked ? "BEST CHALLENGE" : lockedDetail, extendedUnlocked, OnEndlessPressed);
    }

    private static void ConfigureModeButton(Button button, string title, string detail, bool interactable, UnityEngine.Events.UnityAction action)
    {
        button.onClick = new Button.ButtonClickedEvent();
        if (interactable)
        {
            button.onClick.AddListener(action);
        }

        button.interactable = interactable;
        RectTransform rect = button.transform as RectTransform;
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(760f, 154f);
        }

        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.minHeight = 154f;
            layout.preferredHeight = 154f;
            layout.preferredWidth = 760f;
        }

        TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);
        if (labels.Length > 0)
        {
            labels[0].text = title;
            labels[0].fontSizeMax = 58f;
            labels[0].fontSizeMin = 38f;
            labels[0].alignment = TextAlignmentOptions.Center;
            RectTransform titleRect = labels[0].rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(1f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 23f);
            titleRect.sizeDelta = new Vector2(-72f, 68f);
        }

        if (labels.Length > 1)
        {
            labels[1].text = detail;
            labels[1].fontSizeMax = 25f;
            labels[1].fontSizeMin = 18f;
            labels[1].alignment = TextAlignmentOptions.Center;
            RectTransform detailRect = labels[1].rectTransform;
            detailRect.anchorMin = new Vector2(0f, 0.5f);
            detailRect.anchorMax = new Vector2(1f, 0.5f);
            detailRect.anchoredPosition = new Vector2(0f, -46f);
            detailRect.sizeDelta = new Vector2(-110f, 36f);
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = interactable ? Color.white : new Color(0.62f, 0.62f, 0.62f, 0.78f);
        }
    }

    public void OnHowToOpen()
    {
        if (_howToOverlayController != null)
        {
            _howToOverlayController.ShowOverlay();
            return;
        }

        SetPanelActive(_howToPanel, true);
    }

    public void OnHowToClose()
    {
        if (_howToOverlayController != null)
        {
            _howToOverlayController.CloseOverlay();
            return;
        }

        SetPanelActive(_howToPanel, false);
    }

    public void OnSettingsOpen()
    {
        SetPanelActive(_settingsPanel, true);
    }

    public void OnSettingsClose()
    {
        SetPanelActive(_settingsPanel, false);
    }

    private static void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }
}
