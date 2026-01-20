// Created: 2025-02-14
// Author: gpt-5.2-codex

using UnityEngine;
using UnityEngine.UI;

/// <summary>設定パネルのトグルと保存を管理する。</summary>
public class SettingsPanelController : MonoBehaviour
{
    [SerializeField]
    Toggle bgmToggle;

    [SerializeField]
    Toggle seToggle;

    [SerializeField]
    SoundManager soundManager;

    /// <summary>表示時に保存設定を反映する。</summary>
    void OnEnable()
    {
        if (bgmToggle)
        {
            bgmToggle.isOn = SaveService.get_bgm_on();
        }

        if (seToggle)
        {
            seToggle.isOn = SaveService.get_se_on();
        }
    }

    /// <summary>BGM トグル変更時の処理。</summary>
    /// <param name="isOn">有効なら true。</param>
    public void on_bgm_toggle_changed(bool isOn)
    {
        SaveService.set_bgm_on(isOn);
        SaveService.save();
        if (soundManager)
        {
            soundManager.set_bgm_enabled(isOn);
        }
    }

    /// <summary>SE トグル変更時の処理。</summary>
    /// <param name="isOn">有効なら true。</param>
    public void on_se_toggle_changed(bool isOn)
    {
        SaveService.set_se_on(isOn);
        SaveService.save();
        if (soundManager)
        {
            soundManager.set_se_enabled(isOn);
        }
    }
}
