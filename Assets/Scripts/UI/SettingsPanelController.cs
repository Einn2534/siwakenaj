using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    private const string ToggleOnResourcePath = "UI/ui_settings_toggle_on";
    private const string ToggleOffResourcePath = "UI/ui_settings_toggle_off";

    private static readonly Color EnabledTextColor = new(0.976f, 0.953f, 0.882f, 1f);
    private static readonly Color DisabledTextColor = new(0.56f, 0.61f, 0.69f, 1f);
    private static readonly Color BgmAccentColor = new(0.49f, 0.43f, 0.93f, 1f);
    private static readonly Color DisabledAccentColor = new(0.35f, 0.29f, 0.25f, 1f);
    private static readonly Color SeAccentColor = new(1f, 0.76f, 0.24f, 1f);
    private static readonly Color VibrationAccentColor = new(1f, 0.43f, 0.31f, 1f);
    private static readonly Color SliderTrackColor = new(0.184f, 0.118f, 0.071f, 1f);
    private static readonly Color SliderHandleColor = new(0.196f, 0.137f, 0.118f, 1f);
    private static readonly Color SwitchOnColor = new(0.431f, 0.89f, 0.616f, 1f);
    private static readonly Color SwitchOffColor = new(0.27f, 0.2f, 0.15f, 1f);
    private static readonly Color SwitchOutlineColor = new(0.196f, 0.137f, 0.118f, 1f);
    private static readonly Color SwitchKnobColor = new(0.976f, 0.953f, 0.882f, 1f);
    private static readonly Vector2 SliderHandleSize = new(64f, 0f);

    [SerializeField, FormerlySerializedAs("bgmToggle")]
    private Toggle _bgmToggle;

    [SerializeField, FormerlySerializedAs("seToggle")]
    private Toggle _seToggle;

    [SerializeField, FormerlySerializedAs("vibrationToggle")]
    private Toggle _vibrationToggle;

    [Header("Visuals")]
    [SerializeField]
    private TMP_Text _bgmStateText;

    [SerializeField]
    private TMP_Text _seStateText;

    [SerializeField]
    private TMP_Text _vibrationStateText;

    [Header("Volume")]
    [SerializeField]
    private Slider _bgmVolumeSlider;

    [SerializeField]
    private Slider _seVolumeSlider;

    [SerializeField]
    private TMP_Text _bgmVolumeValueText;

    [SerializeField]
    private TMP_Text _seVolumeValueText;

    [SerializeField]
    private Image _bgmToggleImage;

    [SerializeField]
    private Image _seToggleImage;

    [SerializeField]
    private Image _vibrationToggleImage;

    [SerializeField]
    private Image _bgmAccentImage;

    [SerializeField]
    private Image _seAccentImage;

    [SerializeField]
    private Image _vibrationAccentImage;

    [SerializeField]
    private Sprite _toggleOnSprite;

    [SerializeField]
    private Sprite _toggleOffSprite;

    private bool _isInitialized;

    private void Awake()
    {
        ResolveSprites();
        EnsureSettingsRowBindings();
        EnsureVibrationRowBindings();
        EnsureVolumeBindings();
    }

    private void Start()
    {
        if (_bgmToggle != null)
        {
            _bgmToggle.onValueChanged.AddListener(OnBgmChanged);
        }

        if (_seToggle != null)
        {
            _seToggle.onValueChanged.AddListener(OnSeChanged);
        }

        if (_vibrationToggle != null)
        {
            _vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
        }

        if (_bgmVolumeSlider != null)
        {
            _bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        }

        if (_seVolumeSlider != null)
        {
            _seVolumeSlider.onValueChanged.AddListener(OnSeVolumeChanged);
        }

        _isInitialized = true;
        RefreshFromSave();
    }

    private void OnEnable()
    {
        if (_isInitialized)
        {
            RefreshFromSave();
        }
    }

    private void OnDestroy()
    {
        if (_bgmToggle != null)
        {
            _bgmToggle.onValueChanged.RemoveListener(OnBgmChanged);
        }

        if (_seToggle != null)
        {
            _seToggle.onValueChanged.RemoveListener(OnSeChanged);
        }

        if (_vibrationToggle != null)
        {
            _vibrationToggle.onValueChanged.RemoveListener(OnVibrationChanged);
        }

        if (_bgmVolumeSlider != null)
        {
            _bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
        }

        if (_seVolumeSlider != null)
        {
            _seVolumeSlider.onValueChanged.RemoveListener(OnSeVolumeChanged);
        }
    }

    private void OnBgmChanged(bool isOn)
    {
        SaveService.SetBgmOn(isOn);
        SaveService.Save();
        SoundManager.Instance?.SetBgmEnabled(isOn);
        RefreshVisualState(_bgmStateText, _bgmToggleImage, _bgmAccentImage, isOn, BgmAccentColor);
    }

    private void OnSeChanged(bool isOn)
    {
        SaveService.SetSeOn(isOn);
        SaveService.Save();
        SoundManager.Instance?.SetSeEnabled(isOn);
        RefreshVisualState(_seStateText, _seToggleImage, _seAccentImage, isOn, SeAccentColor);
    }

    private void OnBgmVolumeChanged(float volume)
    {
        volume = Mathf.Clamp01(volume);
        SaveService.SetBgmVolume(volume);
        SaveService.Save();
        SoundManager.Instance?.SetBgmVolume(volume);
        RefreshVolumeText(_bgmVolumeValueText, volume);
    }

    private void OnSeVolumeChanged(float volume)
    {
        volume = Mathf.Clamp01(volume);
        SaveService.SetSeVolume(volume);
        SaveService.Save();
        SoundManager.Instance?.SetSeVolume(volume);
        RefreshVolumeText(_seVolumeValueText, volume);
    }

    private void OnVibrationChanged(bool isOn)
    {
        SaveService.SetVibrationOn(isOn);
        SaveService.Save();
        if (!isOn)
        {
            VibrationService.Stop();
        }

        RefreshVisualState(_vibrationStateText, _vibrationToggleImage, _vibrationAccentImage, isOn, VibrationAccentColor);
    }

    private void RefreshFromSave()
    {
        bool isBgmOn = SaveService.GetBgmOn();
        bool isSeOn = SaveService.GetSeOn();
        bool isVibrationOn = SaveService.GetVibrationOn();
        float bgmVolume = SaveService.GetBgmVolume();
        float seVolume = SaveService.GetSeVolume();

        if (_bgmToggle != null)
        {
            _bgmToggle.SetIsOnWithoutNotify(isBgmOn);
        }

        if (_seToggle != null)
        {
            _seToggle.SetIsOnWithoutNotify(isSeOn);
        }

        if (_vibrationToggle != null)
        {
            _vibrationToggle.SetIsOnWithoutNotify(isVibrationOn);
        }

        if (_bgmVolumeSlider != null)
        {
            _bgmVolumeSlider.SetValueWithoutNotify(bgmVolume);
        }

        if (_seVolumeSlider != null)
        {
            _seVolumeSlider.SetValueWithoutNotify(seVolume);
        }

        SoundManager.Instance?.SetBgmEnabled(isBgmOn);
        SoundManager.Instance?.SetSeEnabled(isSeOn);
        SoundManager.Instance?.SetBgmVolume(bgmVolume);
        SoundManager.Instance?.SetSeVolume(seVolume);
        RefreshVisualState(_bgmStateText, _bgmToggleImage, _bgmAccentImage, isBgmOn, BgmAccentColor);
        RefreshVisualState(_seStateText, _seToggleImage, _seAccentImage, isSeOn, SeAccentColor);
        RefreshVisualState(_vibrationStateText, _vibrationToggleImage, _vibrationAccentImage, isVibrationOn, VibrationAccentColor);
        RefreshVolumeText(_bgmVolumeValueText, bgmVolume);
        RefreshVolumeText(_seVolumeValueText, seVolume);
    }

    private void ResolveSprites()
    {
        _toggleOnSprite ??= Resources.Load<Sprite>(ToggleOnResourcePath);
        _toggleOffSprite ??= Resources.Load<Sprite>(ToggleOffResourcePath);
    }

    private void EnsureSettingsRowBindings()
    {
        EnsureToggleRowBindings("BGM", ref _bgmToggle, ref _bgmStateText, ref _bgmToggleImage, ref _bgmAccentImage);
        EnsureToggleRowBindings("SE", ref _seToggle, ref _seStateText, ref _seToggleImage, ref _seAccentImage);
        EnsureToggleRowBindings("VIBRATION", ref _vibrationToggle, ref _vibrationStateText, ref _vibrationToggleImage, ref _vibrationAccentImage);
    }

    private void EnsureToggleRowBindings(
        string label,
        ref Toggle toggle,
        ref TMP_Text stateText,
        ref Image toggleImage,
        ref Image accentImage)
    {
        Transform row = transform.Find($"Body/{label}Row");
        if (row == null)
        {
            return;
        }

        if (toggle == null)
        {
            Transform toggleTransform = row.Find($"{label}Toggle");
            toggle = toggleTransform != null
                ? toggleTransform.GetComponent<Toggle>()
                : row.GetComponentInChildren<Toggle>(true);
        }

        if (stateText == null)
        {
            stateText = FindRowText(row, "StateText");
        }

        if (toggleImage == null && toggle != null)
        {
            toggleImage = ResolveToggleImage(toggle);
        }

        if (accentImage == null)
        {
            accentImage = FindRowImage(row, "AccentBar");
        }
    }

    private void EnsureVibrationRowBindings()
    {
        if (_vibrationToggle != null
            && _vibrationStateText != null
            && _vibrationToggleImage != null
            && _vibrationAccentImage != null)
        {
            return;
        }

        bool createdVibrationRow = false;
        Transform vibrationRow = transform.Find("Body/VIBRATIONRow");
        if (vibrationRow == null)
        {
            Transform sourceRow = transform.Find("Body/SERow");
            if (sourceRow != null)
            {
                GameObject rowObject = Instantiate(sourceRow.gameObject, sourceRow.parent);
                rowObject.name = "VIBRATIONRow";
                rowObject.transform.SetSiblingIndex(sourceRow.GetSiblingIndex() + 1);
                vibrationRow = rowObject.transform;
                createdVibrationRow = true;
            }
        }

        if (vibrationRow == null)
        {
            return;
        }

        SetRowText(vibrationRow, "Label", "VIBRATION");
        SetRowText(vibrationRow, "Detail", "JUDGE / RESULT FEEDBACK");
        SetRowText(vibrationRow, "StateText", "ON");

        if (_vibrationToggle == null)
        {
            _vibrationToggle = vibrationRow.GetComponentInChildren<Toggle>(true);
        }

        if (_vibrationToggle != null)
        {
            _vibrationToggle.name = "VIBRATIONToggle";
            if (createdVibrationRow)
            {
                _vibrationToggle.onValueChanged = new Toggle.ToggleEvent();
            }

            _vibrationToggle.onValueChanged.RemoveListener(OnVibrationChanged);
            if (_vibrationToggleImage == null)
            {
                _vibrationToggleImage = ResolveToggleImage(_vibrationToggle);
            }
        }

        if (_vibrationStateText == null)
        {
            _vibrationStateText = FindRowText(vibrationRow, "StateText");
        }

        if (_vibrationAccentImage == null)
        {
            _vibrationAccentImage = FindRowImage(vibrationRow, "AccentBar");
        }
    }

    private void EnsureVolumeBindings()
    {
        Transform bgmRow = transform.Find("Body/BGMRow");
        EnsureVolumeBinding(bgmRow, "BGM", BgmAccentColor, ref _bgmVolumeSlider, ref _bgmVolumeValueText);

        Transform seRow = transform.Find("Body/SERow");
        EnsureVolumeBinding(seRow, "SE", SeAccentColor, ref _seVolumeSlider, ref _seVolumeValueText);
    }

    private static void EnsureVolumeBinding(
        Transform row,
        string label,
        Color accentColor,
        ref Slider slider,
        ref TMP_Text valueText)
    {
        if (row == null)
        {
            return;
        }

        if (slider == null)
        {
            Transform sliderTransform = row.Find($"{label}VolumeSlider");
            slider = sliderTransform != null ? sliderTransform.GetComponent<Slider>() : null;
        }

        if (valueText == null)
        {
            valueText = FindRowText(row, $"{label}VolumeValueText");
        }

        if (slider == null)
        {
            CreateVolumeSlider(row, label, accentColor, out slider, out valueText);
            return;
        }

        ConfigureVolumeSlider(slider);
        if (valueText == null)
        {
            valueText = CreateVolumeValueText(row, label);
        }
    }

    private static void CreateVolumeSlider(
        Transform row,
        string label,
        Color accentColor,
        out Slider slider,
        out TMP_Text valueText)
    {
        RectTransform sliderRect = CreateRuntimeObject($"{label}VolumeSlider", row, typeof(Slider));
        SetAnchored(sliderRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-52f, 44f), new Vector2(-250f, 70f));

        RectTransform background = CreateRuntimePanel("Background", sliderRect, SliderTrackColor, true);
        Stretch(background, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -12f), new Vector2(0f, 12f));

        RectTransform fillArea = CreateRuntimeObject("Fill Area", sliderRect);
        Stretch(fillArea, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -12f), new Vector2(0f, 12f));

        RectTransform fill = CreateRuntimePanel("Fill", fillArea, accentColor, false);
        Stretch(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform handleArea = CreateRuntimeObject("Handle Slide Area", sliderRect);
        Stretch(handleArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform handle = CreateRuntimePanel("Handle", handleArea, SliderHandleColor, true);
        SetAnchored(handle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, SliderHandleSize);

        slider = sliderRect.GetComponent<Slider>();
        ConfigureVolumeSlider(slider);
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();

        valueText = CreateVolumeValueText(row, label);
    }

    private static void ConfigureVolumeSlider(Slider slider)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;

        if (slider.handleRect != null)
        {
            slider.handleRect.sizeDelta = SliderHandleSize;
            slider.handleRect.anchoredPosition = Vector2.zero;
        }
    }

    private static void SetRowText(Transform row, string childName, string value)
    {
        TMP_Text text = FindRowText(row, childName);
        if (text != null)
        {
            text.text = value;
        }
    }

    private static TMP_Text FindRowText(Transform row, string childName)
    {
        Transform child = row.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Image FindRowImage(Transform row, string childName)
    {
        Transform child = row.Find(childName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private static Image ResolveToggleImage(Toggle toggle)
    {
        if (toggle == null)
        {
            return null;
        }

        Image toggleImage = toggle.GetComponent<Image>();
        if (toggleImage != null)
        {
            return toggleImage;
        }

        return toggle.targetGraphic as Image;
    }

    private static RectTransform CreateRuntimeObject(string name, Transform parent, params System.Type[] components)
    {
        System.Type[] allComponents = new System.Type[components.Length + 2];
        allComponents[0] = typeof(RectTransform);
        allComponents[1] = typeof(CanvasRenderer);
        for (int i = 0; i < components.Length; i += 1)
        {
            allComponents[i + 2] = components[i];
        }

        GameObject gameObject = new(name, allComponents);
        gameObject.layer = parent.gameObject.layer;
        gameObject.transform.SetParent(parent, false);
        return (RectTransform)gameObject.transform;
    }

    private static RectTransform CreateRuntimePanel(string name, Transform parent, Color color, bool raycastTarget)
    {
        RectTransform rectTransform = CreateRuntimeObject(name, parent, typeof(Image));
        Image image = rectTransform.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return rectTransform;
    }

    private static TMP_Text CreateRuntimeText(string name, Transform parent, string text, TextAlignmentOptions alignment)
    {
        RectTransform rectTransform = CreateRuntimeObject(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI textComponent = rectTransform.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 26f;
        textComponent.fontSizeMax = 26f;
        textComponent.fontSizeMin = 18f;
        textComponent.enableAutoSizing = true;
        textComponent.alignment = alignment;
        textComponent.color = EnabledTextColor;
        textComponent.raycastTarget = false;
        textComponent.textWrappingMode = TextWrappingModes.NoWrap;
        return textComponent;
    }

    private static TMP_Text CreateVolumeValueText(Transform row, string label)
    {
        TMP_Text valueText = CreateRuntimeText($"{label}VolumeValueText", row, "100", TextAlignmentOptions.Center);
        SetAnchored((RectTransform)valueText.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-42f, 44f), new Vector2(92f, 52f));
        return valueText;
    }

    private void RefreshVisualState(TMP_Text stateText, Image toggleImage, Image accentImage, bool isOn, Color enabledAccentColor)
    {
        if (stateText != null)
        {
            stateText.text = isOn ? "ON" : "OFF";
            stateText.color = isOn ? EnabledTextColor : DisabledTextColor;
        }

        if (toggleImage != null)
        {
            RectTransform knob = toggleImage.transform.Find("Knob") as RectTransform;
            Image trackFace = toggleImage.transform.Find("TrackFace")?.GetComponent<Image>();
            if (knob != null && trackFace != null)
            {
                toggleImage.color = SwitchOutlineColor;
                trackFace.color = isOn ? SwitchOnColor : SwitchOffColor;
                float anchorX = isOn ? 1f : 0f;
                knob.anchorMin = new Vector2(anchorX, 0.5f);
                knob.anchorMax = new Vector2(anchorX, 0.5f);
                knob.pivot = new Vector2(anchorX, 0.5f);
                knob.anchoredPosition = new Vector2(isOn ? -8f : 8f, 0f);
                knob.GetComponent<Image>().color = SwitchOutlineColor;
                Image knobFace = knob.Find("KnobFace")?.GetComponent<Image>();
                if (knobFace != null)
                {
                    knobFace.color = SwitchKnobColor;
                }
            }
            else
            {
                Sprite stateSprite = isOn ? _toggleOnSprite : _toggleOffSprite;
                if (stateSprite != null)
                {
                    toggleImage.sprite = stateSprite;
                    toggleImage.type = Image.Type.Simple;
                }

                toggleImage.color = Color.white;
            }
        }

        if (accentImage != null)
        {
            accentImage.color = isOn ? enabledAccentColor : DisabledAccentColor;
        }
    }

    private static void RefreshVolumeText(TMP_Text valueText, float volume)
    {
        if (valueText != null)
        {
            valueText.text = Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f).ToString();
        }
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
}
