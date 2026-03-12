using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("bgmToggle")]
    private Toggle _bgmToggle;

    [SerializeField, FormerlySerializedAs("seToggle")]
    private Toggle _seToggle;

    private void Start()
    {
        if (_bgmToggle != null)
        {
            _bgmToggle.isOn = SaveService.GetBgmOn();
            _bgmToggle.onValueChanged.AddListener(OnBgmChanged);
        }

        if (_seToggle != null)
        {
            _seToggle.isOn = SaveService.GetSeOn();
            _seToggle.onValueChanged.AddListener(OnSeChanged);
        }
    }

    private void OnBgmChanged(bool isOn)
    {
        SaveService.SetBgmOn(isOn);
        SaveService.Save();
        SoundManager.Instance?.SetBgmEnabled(isOn);
    }

    private void OnSeChanged(bool isOn)
    {
        SaveService.SetSeOn(isOn);
        SaveService.Save();
        SoundManager.Instance?.SetSeEnabled(isOn);
    }
}
