using UnityEngine;
using UnityEngine.Serialization;

public class SoundManager : MonoBehaviour
{
    private static SoundManager _currentInstance;
    private const string SoundManagerObjectName = "SoundManager";
    private const string BgmResourcePath = "Audio/Bgm";
    private const string TitleBgmResourcePath = "Audio/TitleBgm";
    private const string StageSelectBgmResourcePath = "Audio/StageSelectBgm";
    private const string CorrectResourcePath = "Audio/Correct";
    private const string MissResourcePath = "Audio/Miss";
    private const string ClearResourcePath = "Audio/Clear";
    private const string GameOverResourcePath = "Audio/GameOver";

    [SerializeField, FormerlySerializedAs("bgmSource")]
    private AudioSource _bgmSource;

    [SerializeField, FormerlySerializedAs("seSource")]
    private AudioSource _seSource;

    [SerializeField, FormerlySerializedAs("bgmClip")]
    private AudioClip _bgmClip;

    [SerializeField]
    private AudioClip _titleBgmClip;

    [SerializeField]
    private AudioClip _stageSelectBgmClip;

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
    private float _bgmVolume = 1f;
    private float _seVolume = 1f;

    public static SoundManager Instance => _currentInstance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        _currentInstance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        EnsureInstance();
    }

    public static SoundManager EnsureInstance()
    {
        if (_currentInstance != null)
        {
            return _currentInstance;
        }

        GameObject soundManagerObject = new GameObject(SoundManagerObjectName);
        return soundManagerObject.AddComponent<SoundManager>();
    }

    private void Awake()
    {
        if (_currentInstance != null && _currentInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        _currentInstance = this;
        DontDestroyOnLoad(gameObject);
        EnsureAudioSources();
        LoadResourceFallbackClips();
        _isBgmOn = SaveService.GetBgmOn();
        _isSeOn = SaveService.GetSeOn();
        _bgmVolume = SaveService.GetBgmVolume();
        _seVolume = SaveService.GetSeVolume();
        ApplyBgmSettings();
        ApplySeSettings();
    }

    private void EnsureAudioSources()
    {
        if (_bgmSource == null)
        {
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
        }

        _bgmSource.spatialBlend = 0f;

        if (_seSource == null)
        {
            _seSource = gameObject.AddComponent<AudioSource>();
            _seSource.playOnAwake = false;
        }

        _seSource.spatialBlend = 0f;
    }

    private void LoadResourceFallbackClips()
    {
        _bgmClip ??= Resources.Load<AudioClip>(BgmResourcePath);
        _titleBgmClip ??= Resources.Load<AudioClip>(TitleBgmResourcePath);
        _stageSelectBgmClip ??= Resources.Load<AudioClip>(StageSelectBgmResourcePath);
        _correctClip ??= Resources.Load<AudioClip>(CorrectResourcePath);
        _missClip ??= Resources.Load<AudioClip>(MissResourcePath);
        _clearClip ??= Resources.Load<AudioClip>(ClearResourcePath);
        _gameOverClip ??= Resources.Load<AudioClip>(GameOverResourcePath);

        LoadSeAudioData();
    }

    private void LoadSeAudioData()
    {
        LoadAudioDataIfNeeded(_correctClip);
        LoadAudioDataIfNeeded(_missClip);
        LoadAudioDataIfNeeded(_clearClip);
        LoadAudioDataIfNeeded(_gameOverClip);
    }

    private static void LoadAudioDataIfNeeded(AudioClip clip)
    {
        if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
        }
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
        ApplyBgmSettings();
    }

    public void SetBgmVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        ApplyBgmSettings();
    }

    private void ApplyBgmSettings()
    {
        if (_bgmSource != null)
        {
            _bgmSource.mute = !_isBgmOn;
            _bgmSource.volume = _bgmVolume;
        }
    }

    public void SetSeEnabled(bool isOn)
    {
        _isSeOn = isOn;
        ApplySeSettings();
    }

    public void SetSeVolume(float volume)
    {
        _seVolume = Mathf.Clamp01(volume);
        ApplySeSettings();
    }

    private void ApplySeSettings()
    {
        if (_seSource != null)
        {
            _seSource.mute = !_isSeOn;
            _seSource.volume = _seVolume;
        }
    }

    public void PlayBgm()
    {
        PlayBgmClip(_bgmClip);
    }

    public void PlayTitleBgm()
    {
        PlayBgmClip(_titleBgmClip);
    }

    public void PlayStageSelectBgm()
    {
        PlayBgmClip(_stageSelectBgmClip);
    }

    private void PlayBgmClip(AudioClip clip)
    {
        if (_bgmSource == null || clip == null)
        {
            return;
        }

        if (_bgmSource.clip == clip && _bgmSource.isPlaying)
        {
            return;
        }

        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        ApplyBgmSettings();
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

        LoadAudioDataIfNeeded(clip);
        _seSource.PlayOneShot(clip);
    }
}
