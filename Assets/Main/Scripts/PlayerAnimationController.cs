// Created: 2025-11-28
// Author: gpt-5.1-codex-max

using UnityEngine;

/// <summary>プレイヤーキャラクターのリアクションアニメを制御する。</summary>
public class PlayerAnimationController : MonoBehaviour
{
    private const string HAPPY_TRIGGER = "Attack";
    private const string CRY_TRIGGER = "Damage";
    private const string WIN_TRIGGER = "Win";

    [SerializeField]
    Animator animator;

    /// <summary>嬉しいリアクションアニメを再生する。</summary>
    public void play_happy()
    {
        if (animator)
        {
            animator.SetTrigger(HAPPY_TRIGGER);
        }
    }

    /// <summary>泣きリアクションのアニメを再生する。</summary>
    public void play_cry()
    {
        if (animator)
        {
            animator.SetTrigger(CRY_TRIGGER);
        }
    }

    /// <summary>勝利ポーズのアニメを再生する。</summary>
    public void play_win()
    {
        if (animator)
        {
            animator.SetTrigger(WIN_TRIGGER);
        }
    }
}
