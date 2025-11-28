// Created: 2025-11-28
// Author: gpt-5.1-codex-max

using UnityEngine;

/// <summary>Controls player character animations for feedback.</summary>
public class PlayerAnimationController : MonoBehaviour
{
    private const string HAPPY_TRIGGER = "Happy";
    private const string CRY_TRIGGER = "Cry";
    private const string WIN_TRIGGER = "Win";

    [SerializeField]
    Animator animator;

    /// <summary>Plays the happy reaction animation.</summary>
    public void play_happy()
    {
        if (animator)
        {
            animator.SetTrigger(HAPPY_TRIGGER);
        }
    }

    /// <summary>Plays the crying reaction animation.</summary>
    public void play_cry()
    {
        if (animator)
        {
            animator.SetTrigger(CRY_TRIGGER);
        }
    }

    /// <summary>Plays the winning pose animation.</summary>
    public void play_win()
    {
        if (animator)
        {
            animator.SetTrigger(WIN_TRIGGER);
        }
    }
}
