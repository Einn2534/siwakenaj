using UnityEngine;
using UnityEngine.Serialization;

public class SoundManager : MonoBehaviour
{
    private static SoundManager _currentInstance;

    [SerializeField, FormerlySerializedAs("bgmSource")]
    private AudioSource _bgmSource;

    [SerializeField, FormerlySerializedAs("seSource")]
    private AudioSource _seSource;

    [SerializeField, FormerlySerializedAs("bgmClip")]
    private AudioClip _bgmClip;

    [SerializeField, FormerlySerializedAs("correctClip")]
    private AudioClip _correctClip;

    [SerializeField, FormerlySerializedAs("missClip")]
    private AudioClip _missClip;

    [SerializeField, FormerlySerializedAs("clearClip")]
    private AudioClip _clearClip;

    [SerializeField, FormerlySerializedAs("gameOverClip")]
    private AudioClip _gameOverClip;

    private bool _isBgmOn = true;
    private bool _isSeOn = true;

    public static SoundManager Instance => _currentInstance;

    private void Awake()
    {
        if (_currentInstance != null && _currentInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        _currentInstance = this;
        DontDestroyOnLoad(gameObject);
        _isBgmOn = SaveService.GetBgmOn();
        _isSeOn = SaveService.GetSeOn();
    }

    private void OnDestroy()
    {
        if (_currentInstance == this)
        {
            _currentInstance = null;
        }
    }

    public void SetBgmEnabled(bool isOn)
    {
        _isBgmOn = isOn;
        if (_bgmSource != null)
        {
            _bgmSource.mute = !_isBgmOn;
        }
    }

    public void SetSeEnabled(bool isOn)
    {
        _isSeOn = isOn;
    }

    public void PlayBgm()
    {
        if (_bgmSource == null || _bgmClip == null)
        {
            return;
        }

        _bgmSource.clip = _bgmClip;
        _bgmSource.loop = true;
        _bgmSource.mute = !_isBgmOn;
        _bgmSource.Play();
    }

    public void StopBgm()
    {
        if (_bgmSource != null)
        {
            _bgmSource.Stop();
        }
    }

    public void PlayCorrect()
    {
        PlaySe(_correctClip);
    }

    public void PlayMiss()
    {
        PlaySe(_missClip);
    }

    public void PlayClear()
    {
        PlaySe(_clearClip);
    }

    public void PlayGameOver()
    {
        PlaySe(_gameOverClip);
    }

    private void PlaySe(AudioClip clip)
    {
        if (!_isSeOn || _seSource == null || clip == null)
        {
            return;
        }

        _seSource.PlayOneShot(clip);
    }
}
