using UnityEngine;
using UnityEngine.Serialization;

public class PlayerAnimationController : MonoBehaviour
{
    private const string HappyTrigger = "Attack";
    private const string CryTrigger = "Damage";
    private const string WinTrigger = "Win";

    [SerializeField, FormerlySerializedAs("animator")]
    private Animator _animator;

    // Main uses immediate authored states. Existing UI animators retain their trigger contract.
    [SerializeField] private bool _useImmediateStates;
    private bool _winLocked;
    private int _reactionState;
    private bool _paused;
    private float _speedBeforePause = 1f;

    public bool IsReactionPlaying
    {
        get
        {
            if (!_useImmediateStates || _reactionState == 0 || !CanAnimate)
                return false;
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            return state.shortNameHash == _reactionState && state.normalizedTime < 1f;
        }
    }

    private bool CanAnimate => _animator != null && _animator.isActiveAndEnabled
        && _animator.runtimeAnimatorController != null;

    private void OnEnable()
    {
        _winLocked = false;
        _reactionState = 0;
    }

    public void SetPaused(bool paused)
    {
        if (_paused == paused || _animator == null) return;
        if (paused)
        {
            _speedBeforePause = _animator.speed;
            _animator.speed = 0;
        }
        else _animator.speed = _speedBeforePause;
        _paused = paused;
    }

    public void PlayHappy()
    {
        PlayReaction(HappyTrigger);
    }

    public void PlayCry()
    {
        PlayReaction(CryTrigger);
    }

    public void PlayWin()
    {
        PlayReaction(WinTrigger);
        _winLocked = _useImmediateStates;
    }

    private void PlayReaction(string stateName)
    {
        if (!CanAnimate || _winLocked)
            return;

        if (_useImmediateStates)
        {
            int state = Animator.StringToHash(stateName);
            if (!_animator.HasState(0, state))
                return;
            _reactionState = state;
            // Replace the current reaction, including repeats, without queued triggers.
            _animator.Play(state, 0, 0f);
            _animator.Update(0f);
        }
        else
        {
            _animator.ResetTrigger(HappyTrigger);
            _animator.ResetTrigger(CryTrigger);
            _animator.ResetTrigger(WinTrigger);
            _animator.SetTrigger(stateName);
        }
    }
}
