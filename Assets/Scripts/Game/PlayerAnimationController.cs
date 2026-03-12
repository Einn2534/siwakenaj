using UnityEngine;
using UnityEngine.Serialization;

public class PlayerAnimationController : MonoBehaviour
{
    private const string HappyTrigger = "Attack";
    private const string CryTrigger = "Damage";
    private const string WinTrigger = "Win";

    [SerializeField, FormerlySerializedAs("animator")]
    private Animator _animator;

    public void PlayHappy()
    {
        if (_animator != null)
        {
            _animator.SetTrigger(HappyTrigger);
        }
    }

    public void PlayCry()
    {
        if (_animator != null)
        {
            _animator.SetTrigger(CryTrigger);
        }
    }

    public void PlayWin()
    {
        if (_animator != null)
        {
            _animator.SetTrigger(WinTrigger);
        }
    }
}
