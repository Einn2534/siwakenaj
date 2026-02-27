// Created: 2025-11-28
// Updated: 2026-02-26
// Author: Einn

using UnityEngine;

/// <summary>BGM およびサウンドエフェクトの再生を管理する。</summary>
public class SoundManager : MonoBehaviour
{
    [SerializeField]
    AudioSource bgmSource;

    [SerializeField]
    AudioSource seSource;

    [SerializeField]
    AudioClip bgmClip;

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

    /// <summary>初期化時に保存された設定を読み込む。</summary>
    void Awake()
    {
        isBgmOn = SaveService.get_bgm_on();
        isSeOn = SaveService.get_se_on();
    }

    /// <summary>BGM の有効・無効を切り替える。</summary>
    /// <param name="isOn">有効にする場合 true。</param>
    public void set_bgm_enabled(bool isOn)
    {
        isBgmOn = isOn;

        if (bgmSource)
        {
            bgmSource.mute = !isBgmOn;
        }
    }

    /// <summary>SE の有効・無効を切り替える。</summary>
    /// <param name="isOn">有効にする場合 true。</param>
    public void set_se_enabled(bool isOn)
    {
        isSeOn = isOn;
    }

    /// <summary>BGM を再生する。</summary>
    public void play_bgm()
    {
        if (!bgmSource || !bgmClip)
        {
            return;
        }

        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        bgmSource.mute = !isBgmOn;
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

    /// <summary>正解 SE を再生する。</summary>
    public void play_correct()
    {
        play_se(correctClip);
    }

    /// <summary>ミス SE を再生する。</summary>
    public void play_miss()
    {
        play_se(missClip);
    }

    /// <summary>クリア SE を再生する。</summary>
    public void play_clear()
    {
        play_se(clearClip);
    }

    /// <summary>ゲームオーバー SE を再生する。</summary>
    public void play_game_over()
    {
        play_se(gameOverClip);
    }

    /// <summary>SE を1回だけ再生する。</summary>
    /// <param name="clip">再生するオーディオクリップ。</param>
    void play_se(AudioClip clip)
    {
        if (!isSeOn || !seSource || !clip)
        {
            return;
        }

        seSource.PlayOneShot(clip);
    }
}
