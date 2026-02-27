// Created: 2025-02-14
// Updated: 2026-02-26
// Author: Einn

using UnityEngine;
using UnityEngine.UI;

/// <summary>設定パネルのBGM/SEトグル操作を管理する。</summary>
public class SettingsPanelController : MonoBehaviour
{
    [SerializeField]
    Toggle bgmToggle;

    [SerializeField]
    Toggle seToggle;

    [SerializeField]
    SoundManager soundManager;

    /// <summary>初期化時に保存済み設定をトグルへ反映し、リスナーを登録する。</summary>
    void Start()
    {
        if (bgmToggle)
        {
            bgmToggle.isOn = SaveService.get_bgm_on();
            bgmToggle.onValueChanged.AddListener(on_bgm_changed);
        }

        if (seToggle)
        {
            seToggle.isOn = SaveService.get_se_on();
            seToggle.onValueChanged.AddListener(on_se_changed);
        }
    }

    /// <summary>BGM トグルの値が変わったときの処理。</summary>
    /// <param name="isOn">有効なら true。</param>
    void on_bgm_changed(bool isOn)
    {
        SaveService.set_bgm_on(isOn);
        SaveService.save();

        if (soundManager)
        {
            soundManager.set_bgm_enabled(isOn);
        }
    }

    /// <summary>SE トグルの値が変わったときの処理。</summary>
    /// <param name="isOn">有効なら true。</param>
    void on_se_changed(bool isOn)
    {
        SaveService.set_se_on(isOn);
        SaveService.save();

        if (soundManager)
        {
            soundManager.set_se_enabled(isOn);
        }
    }
}
