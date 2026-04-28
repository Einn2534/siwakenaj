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

    [Header("Visuals")]
    [SerializeField]
    private TMP_Text _bgmStateText;

    [SerializeField]
    private TMP_Text _seStateText;

    [SerializeField]
    private Image _bgmToggleImage;

    [SerializeField]
    private Image _seToggleImage;

    [SerializeField]
    private Image _bgmAccentImage;

    [SerializeField]
    private Image _seAccentImage;

    [SerializeField]
    private Sprite _toggleOnSprite;

    [SerializeField]
    private Sprite _toggleOffSprite;

    private bool _isInitialized;

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

    private void RefreshFromSave()
    {
        bool isBgmOn = SaveService.GetBgmOn();
        bool isSeOn = SaveService.GetSeOn();

        if (_bgmToggle != null)
        {
            _bgmToggle.SetIsOnWithoutNotify(isBgmOn);
        }

        if (_seToggle != null)
        {
            _seToggle.SetIsOnWithoutNotify(isSeOn);
        }

        RefreshVisualState(_bgmStateText, _bgmToggleImage, _bgmAccentImage, isBgmOn);
        RefreshVisualState(_seStateText, _seToggleImage, _seAccentImage, isSeOn);
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
