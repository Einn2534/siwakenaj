using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    private static readonly Color EnabledTextColor = new(0.137f, 0.184f, 0.275f, 1f);
    private static readonly Color DisabledTextColor = new(0.56f, 0.61f, 0.69f, 1f);
    private static readonly Color EnabledAccentColor = new(0.345f, 0.784f, 0.541f, 1f);
    private static readonly Color DisabledAccentColor = new(0.824f, 0.855f, 0.902f, 1f);

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
        EnsureVibrationRowBindings();
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
    }

    private void OnBgmChanged(bool isOn)
    {
        SaveService.SetBgmOn(isOn);
        SaveService.Save();
        SoundManager.Instance?.SetBgmEnabled(isOn);
        RefreshVisualState(_bgmStateText, _bgmToggleImage, _bgmAccentImage, isOn);
    }

    private void OnSeChanged(bool isOn)
    {
        SaveService.SetSeOn(isOn);
        SaveService.Save();
        SoundManager.Instance?.SetSeEnabled(isOn);
        RefreshVisualState(_seStateText, _seToggleImage, _seAccentImage, isOn);
    }

    private void OnVibrationChanged(bool isOn)
    {
        SaveService.SetVibrationOn(isOn);
        SaveService.Save();
        if (!isOn)
        {
            VibrationService.Stop();
        }

        RefreshVisualState(_vibrationStateText, _vibrationToggleImage, _vibrationAccentImage, isOn);
    }

    private void RefreshFromSave()
    {
        bool isBgmOn = SaveService.GetBgmOn();
        bool isSeOn = SaveService.GetSeOn();
        bool isVibrationOn = SaveService.GetVibrationOn();

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

        RefreshVisualState(_bgmStateText, _bgmToggleImage, _bgmAccentImage, isBgmOn);
        RefreshVisualState(_seStateText, _seToggleImage, _seAccentImage, isSeOn);
        RefreshVisualState(_vibrationStateText, _vibrationToggleImage, _vibrationAccentImage, isVibrationOn);
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

    private void RefreshVisualState(TMP_Text stateText, Image toggleImage, Image accentImage, bool isOn)
    {
        if (stateText != null)
        {
            stateText.text = isOn ? "ON" : "OFF";
            stateText.color = isOn ? EnabledTextColor : DisabledTextColor;
        }

        if (toggleImage != null)
        {
            Sprite stateSprite = isOn ? _toggleOnSprite : _toggleOffSprite;
            if (stateSprite != null)
            {
                toggleImage.sprite = stateSprite;
                toggleImage.type = Image.Type.Simple;
            }

            toggleImage.color = Color.white;
        }

        if (accentImage != null)
        {
            accentImage.color = isOn ? EnabledAccentColor : DisabledAccentColor;
        }
    }
}
