// Created: 2025-11-28
// Author: gpt-5.1-codex-max

using UnityEngine;

/// <summary>Handles BGM and sound effect playback.</summary>
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

    /// <summary>Plays the background music.</summary>
    public void play_bgm()
    {
        if (!bgmSource || bgmSource.isPlaying)
        {
            return;
        }

        bgmSource.loop = true;
        bgmSource.Play();
    }

    /// <summary>Stops the background music.</summary>
    public void stop_bgm()
    {
        if (bgmSource)
        {
            bgmSource.Stop();
        }
    }

    /// <summary>Plays the correct answer sound.</summary>
    public void play_correct()
    {
        play_se(correctClip);
    }

    /// <summary>Plays the miss sound effect.</summary>
    public void play_miss()
    {
        play_se(missClip);
    }

    /// <summary>Plays the stage clear jingle.</summary>
    public void play_clear()
    {
        play_se(clearClip);
    }

    /// <summary>Plays the game over sound.</summary>
    public void play_game_over()
    {
        play_se(gameOverClip);
    }

    /// <summary>Plays a one-shot sound effect.</summary>
    /// <param name="clip">Clip to play.</param>
    void play_se(AudioClip clip)
    {
        if (seSource && clip)
        {
            seSource.PlayOneShot(clip);
        }
    }
}
