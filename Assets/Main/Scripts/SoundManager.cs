// Created: 2025-11-28
// Author: gpt-5.1-codex-max

using UnityEngine;

/// <summary>BGM と効果音の再生を管理する。</summary>
public class SoundManager : MonoBehaviour
{
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

    /// <summary>BGM を再生する。</summary>
    public void play_bgm()
    {
        if (!bgmSource || bgmSource.isPlaying)
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
        if (seSource && clip)
        {
            seSource.PlayOneShot(clip);
        }
    }
}
