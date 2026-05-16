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
        SetBgmEnabled(_isBgmOn);
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
        if (_bgmClip == null)
        {
            _bgmClip = Resources.Load<AudioClip>(BgmResourcePath);
        }

        if (_titleBgmClip == null)
        {
            _titleBgmClip = Resources.Load<AudioClip>(TitleBgmResourcePath);
        }

        if (_stageSelectBgmClip == null)
        {
            _stageSelectBgmClip = Resources.Load<AudioClip>(StageSelectBgmResourcePath);
        }

        if (_correctClip == null)
        {
            _correctClip = Resources.Load<AudioClip>(CorrectResourcePath);
        }

        if (_missClip == null)
        {
            _missClip = Resources.Load<AudioClip>(MissResourcePath);
        }

        if (_clearClip == null)
        {
            _clearClip = Resources.Load<AudioClip>(ClearResourcePath);
        }

        if (_gameOverClip == null)
        {
            _gameOverClip = Resources.Load<AudioClip>(GameOverResourcePath);
        }

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

        LoadAudioDataIfNeeded(clip);
        _seSource.PlayOneShot(clip);
    }
}
