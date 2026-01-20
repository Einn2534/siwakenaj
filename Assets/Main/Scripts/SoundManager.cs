// Created: 2025-11-28
// Author: gpt-5.1-codex-max

using UnityEngine;

/// <summary>BGM と効果音の再生を管理する。</summary>
public class SoundManager : MonoBehaviour
{
    private const float VOLUME_ON = 1f;
    private const float VOLUME_OFF = 0f;

    [SerializeField]
    AudioSource bgmSource;

    [SerializeField]
    AudioSource seSource;

    [SerializeField]
    AudioClip correctClip;

    [SerializeField]
    AudioClip missClip;

    [SerializeField]
    AudioClip clearClip;

    [SerializeField]
    AudioClip gameOverClip;

    bool isBgmOn = true;
    bool isSeOn = true;

    /// <summary>起動時に保存設定を反映する。</summary>
    void Awake()
    {
        apply_saved_settings();
    }

    /// <summary>BGM を再生する。</summary>
    public void play_bgm()
    {
        if (!bgmSource || bgmSource.isPlaying)
        {
            return;
        }

        if (!isBgmOn)
        {
            return;
        }

        bgmSource.loop = true;
        bgmSource.Play();
    }

    /// <summary>BGM を停止する。</summary>
    public void stop_bgm()
    {
        if (bgmSource)
        {
            bgmSource.Stop();
        }
    }

    /// <summary>BGM の有効/無効を設定する。</summary>
    /// <param name="isOn">有効なら true。</param>
    public void set_bgm_enabled(bool isOn)
    {
        isBgmOn = isOn;
        if (bgmSource)
        {
            bgmSource.volume = isBgmOn ? VOLUME_ON : VOLUME_OFF;
            if (!isBgmOn)
            {
                bgmSource.Stop();
            }
            else if (!bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
        }
    }

    /// <summary>SE の有効/無効を設定する。</summary>
    /// <param name="isOn">有効なら true。</param>
    public void set_se_enabled(bool isOn)
    {
        isSeOn = isOn;
        if (seSource)
        {
            seSource.volume = isSeOn ? VOLUME_ON : VOLUME_OFF;
        }
    }

    /// <summary>保存された設定を反映する。</summary>
    public void apply_saved_settings()
    {
        set_bgm_enabled(SaveService.get_bgm_on());
        set_se_enabled(SaveService.get_se_on());
    }

    /// <summary>正解時の効果音を再生する。</summary>
    public void play_correct()
    {
        play_se(correctClip);
    }

    /// <summary>ミス時の効果音を再生する。</summary>
    public void play_miss()
    {
        play_se(missClip);
    }

    /// <summary>ステージクリアのジングルを再生する。</summary>
    public void play_clear()
    {
        play_se(clearClip);
    }

    /// <summary>ゲームオーバーの効果音を再生する。</summary>
    public void play_game_over()
    {
        play_se(gameOverClip);
    }

    /// <summary>単発の効果音を再生する。</summary>
    /// <param name="clip">再生するクリップ。</param>
    void play_se(AudioClip clip)
    {
        if (seSource && clip && isSeOn)
        {
            seSource.PlayOneShot(clip);
        }
    }
}
