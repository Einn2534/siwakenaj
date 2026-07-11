using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameFlowController : MonoBehaviour
{
    private const float ContinueFailureDelaySeconds = 0.7f;
    private const string ResultSceneName = "Result";
    private const string StageDatabaseResourcePath = "StageDatabase";
    private const int ReadabilityOverlaySortingOrder = 5;
    private const float ReadabilityOverlayAlpha = 0.30f;

    [SerializeField, FormerlySerializedAs("scoreManager")]
    private ScoreManager _scoreManager;

    [SerializeField, FormerlySerializedAs("carSpawner")]
    private CarSpawner _carSpawner;

    [SerializeField, FormerlySerializedAs("playerAnimationController")]
    private PlayerAnimationController _playerAnimationController;

    [SerializeField]
    private MainHudEffectsController _hudEffectsController;

    [SerializeField, FormerlySerializedAs("currentState")]
    private GameState _currentState = GameState.Ready;

    private JudgeController _judgeController;
    private LaneInputController _laneInputController;
    private StageDatabase _stageDatabase;
    private StageDefinition _currentStageDefinition;
    private TutorialController _tutorialController;
    private StageProgressHudController _stageProgressHudController;
    private ContinuePromptController _continuePromptController;
    private Coroutine _startGameRoutine;
    private Coroutine _continueRoutine;
    private GameState _stateBeforePause = GameState.Ready;
    private float _timeScaleBeforePause = 1f;
    private bool _hasUsedContinue;
    private SpriteRenderer _readabilityOverlayRenderer;
    private static Sprite s_ReadabilityOverlaySprite;

    private void Awake()
    {
        _judgeController = FindAnyObjectByType<JudgeController>();
        _laneInputController = FindAnyObjectByType<LaneInputController>(FindObjectsInactive.Include);
        _hudEffectsController ??= FindAnyObjectByType<MainHudEffectsController>();
        _stageProgressHudController = FindAnyObjectByType<StageProgressHudController>(FindObjectsInactive.Include);
        _tutorialController = FindAnyObjectByType<TutorialController>(FindObjectsInactive.Include);
        _stageDatabase = Resources.Load<StageDatabase>(StageDatabaseResourcePath);
        EnsureReadabilityOverlay();
    }

    private void OnEnable()
    {
        if (_carSpawner != null)
        {
            _carSpawner.CarMissed += OnCarMissed;
        }
    }

    private void OnDisable()
    {
        if (_carSpawner != null)
        {
            _carSpawner.CarMissed -= OnCarMissed;
        }

        if (_startGameRoutine != null)
        {
            StopCoroutine(_startGameRoutine);
            _startGameRoutine = null;
        }

        if (_currentState == GameState.Paused)
        {
            _currentState = _stateBeforePause;
            RestoreTimeScale();
        }

        if (_continueRoutine != null)
        {
            StopCoroutine(_continueRoutine);
            _continueRoutine = null;
        }

        _continuePromptController?.Hide();

        _hudEffectsController?.StopEffects();
    }

    private void Start()
    {
        StartGame();
    }

    public bool IsPlaying()
    {
        return _currentState == GameState.Playing;
    }

    public bool IsPaused()
    {
        return _currentState == GameState.Paused;
    }

    public bool CanPauseGame()
    {
        return _currentState == GameState.Ready || _currentState == GameState.Playing;
    }

    public bool PauseGame()
    {
        if (!CanPauseGame())
        {
            return false;
        }

        _stateBeforePause = _currentState;
        _timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
        _currentState = GameState.Paused;
        Time.timeScale = 0f;
        return true;
    }

    public void ResumeGame()
    {
        if (_currentState != GameState.Paused)
        {
            return;
        }

        _currentState = _stateBeforePause;
        RestoreTimeScale();
    }

    public void StartGame()
    {
        if (_currentState != GameState.Ready)
        {
            return;
        }

        if (_startGameRoutine != null)
        {
            return;
        }

        _startGameRoutine = StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        if (_scoreManager == null || _carSpawner == null || _stageDatabase == null)
        {
            _startGameRoutine = null;
            yield break;
        }

        RefreshReadabilityOverlay();

        int selectedStageNumber = StageNumberUtility.Normalize(SessionState.SelectedStageNumber);
        _hasUsedContinue = false;
        if (SessionState.IsEndlessMode)
        {
            _currentStageDefinition = _stageDatabase.GetEndlessStageDefinition(selectedStageNumber);
            SessionState.SelectEndless(_currentStageDefinition.StageNumber);
        }
        else
        {
            _currentStageDefinition = _stageDatabase.GetStageDefinition(selectedStageNumber);
            SessionState.SelectStage(_currentStageDefinition.StageNumber);
        }

        _scoreManager.Initialize(_currentStageDefinition);
        _carSpawner.Initialize(_currentStageDefinition);
        SoundManager.EnsureInstance().PlayBgm();

        if (!SessionState.IsEndlessMode && TutorialLaunchService.ShouldStartTutorial(_currentStageDefinition.StageNumber))
        {
            _currentState = GameState.Playing;
            _tutorialController = TutorialController.EnsureInstalled();
            _tutorialController.Begin(this, _scoreManager, _carSpawner, _currentStageDefinition);
            _startGameRoutine = null;
            yield break;
        }

        if (_hudEffectsController != null)
        {
            yield return _hudEffectsController.PlayReadyCountdown();
        }

        if (_currentState != GameState.Ready)
        {
            _startGameRoutine = null;
            yield break;
        }

        _carSpawner.StartSpawning();
        _currentState = GameState.Playing;
        _startGameRoutine = null;
    }

    public void HandleLaneInput(CarType laneType)
    {
        if (!IsPlaying())
        {
            return;
        }

        if (_tutorialController != null && _tutorialController.IsRunning)
        {
            _tutorialController.HandleLaneInput(laneType);
            return;
        }

        CarController activeCar = _carSpawner != null ? _carSpawner.GetActiveCar() : null;
        JudgeResult result = _judgeController != null
            ? _judgeController.Evaluate(activeCar, laneType)
            : JudgeEvaluator.Evaluate(activeCar != null ? activeCar.CarType : null, laneType);

        switch (result)
        {
            case JudgeResult.Correct:
                _hudEffectsController?.ShowCorrectJudge();
                _scoreManager.ApplySuccess(laneType);
                ResolveStageProgressHudController()?.HighlightGoal();
                _playerAnimationController?.PlayHappy();
                SoundManager.EnsureInstance().PlayCorrect();
                _carSpawner?.DespawnCar(activeCar);
                if (!HasReachedTerminalCondition())
                {
                    VibrationService.PlayCorrect();
                }
                break;
            case JudgeResult.WrongLane:
                _hudEffectsController?.ShowMissJudge("\u3061\u304c\u3046\u8eca!");
                ResolveLaneInputController()?.PlayWrongLaneFeedback(laneType, activeCar.CarType);
                _scoreManager.ApplyMiss();
                ResolveStageProgressHudController()?.HighlightMiss();
                _playerAnimationController?.PlayCry();
                SoundManager.EnsureInstance().PlayMiss();
                if (!HasReachedTerminalCondition())
                {
                    VibrationService.PlayMiss();
                }
                break;
            case JudgeResult.NoCar:
                _hudEffectsController?.ShowMissJudge("\u8eca\u304c\u3044\u306a\u3044!");
                ResolveLaneInputController()?.PlayNoCarFeedback(laneType);
                _scoreManager.ApplyMiss();
                ResolveStageProgressHudController()?.HighlightMiss();
                _playerAnimationController?.PlayCry();
                SoundManager.EnsureInstance().PlayMiss();
                if (!HasReachedTerminalCondition())
                {
                    VibrationService.PlayMiss();
                }
                break;
        }

        EvaluateCompletion();
    }

    private void OnCarMissed(CarController car)
    {
        if (!IsPlaying())
        {
            return;
        }

        if (_tutorialController != null && _tutorialController.IsRunning)
        {
            _tutorialController.HandleCarMissed(car);
            return;
        }

        _scoreManager.ApplyMiss();
        _hudEffectsController?.ShowMissJudge("\u898b\u9003\u3057!");
        ResolveStageProgressHudController()?.HighlightMiss();
        _playerAnimationController?.PlayCry();
        SoundManager.EnsureInstance().PlayMiss();
        if (!HasReachedTerminalCondition())
        {
            VibrationService.PlayMiss();
        }
        EvaluateCompletion();
    }

    public void PlayTutorialCorrectFeedback()
    {
        _hudEffectsController?.ShowCorrectJudge();
        _playerAnimationController?.PlayHappy();
        SoundManager.EnsureInstance().PlayCorrect();
        VibrationService.PlayCorrect();
    }

    public void PlayTutorialMissFeedback()
    {
        _hudEffectsController?.ShowMissJudge();
        _playerAnimationController?.PlayCry();
        SoundManager.EnsureInstance().PlayMiss();
        VibrationService.PlayMiss();
    }

    public void StartRegularStageAfterTutorial()
    {
        if (_scoreManager == null || _carSpawner == null || _currentStageDefinition == null)
        {
            return;
        }

        StopGameplay();
        _carSpawner.DespawnAllCars();
        _hudEffectsController?.StopEffects();
        _scoreManager.Initialize(_currentStageDefinition);
        _carSpawner.Initialize(_currentStageDefinition);
        _currentState = GameState.Ready;

        if (_startGameRoutine != null)
        {
            StopCoroutine(_startGameRoutine);
        }

        _startGameRoutine = StartCoroutine(StartRegularStageAfterTutorialRoutine());
    }

    private IEnumerator StartRegularStageAfterTutorialRoutine()
    {
        yield return null;

        if (_currentState != GameState.Ready)
        {
            _startGameRoutine = null;
            yield break;
        }

        _carSpawner?.StartSpawning();
        _currentState = GameState.Playing;
        _startGameRoutine = null;
    }

    private bool HasReachedTerminalCondition()
    {
        return _scoreManager != null &&
            (_scoreManager.HasReachedMissLimit || _scoreManager.HasReachedTargetScore);
    }

    private StageProgressHudController ResolveStageProgressHudController()
    {
        if (_stageProgressHudController == null)
        {
            _stageProgressHudController = FindAnyObjectByType<StageProgressHudController>(FindObjectsInactive.Include);
        }

        return _stageProgressHudController;
    }

    private LaneInputController ResolveLaneInputController()
    {
        if (_laneInputController == null)
        {
            _laneInputController = FindAnyObjectByType<LaneInputController>(FindObjectsInactive.Include);
        }

        return _laneInputController;
    }

    private void EnsureReadabilityOverlay()
    {
        if (_readabilityOverlayRenderer != null)
        {
            RefreshReadabilityOverlay();
            return;
        }

        GameObject overlayObject = new("GameplayReadabilityBackdrop");
        _readabilityOverlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
        _readabilityOverlayRenderer.sprite = GetReadabilityOverlaySprite();
        _readabilityOverlayRenderer.color = new Color(0f, 0f, 0f, ReadabilityOverlayAlpha);
        _readabilityOverlayRenderer.sortingOrder = ReadabilityOverlaySortingOrder;
        RefreshReadabilityOverlay();
    }

    private void RefreshReadabilityOverlay()
    {
        if (_readabilityOverlayRenderer == null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null || !mainCamera.orthographic)
        {
            _readabilityOverlayRenderer.enabled = false;
            return;
        }

        float height = mainCamera.orthographicSize * 2f;
        float width = height * mainCamera.aspect;
        Vector3 cameraPosition = mainCamera.transform.position;

        _readabilityOverlayRenderer.enabled = true;
        _readabilityOverlayRenderer.transform.position = new Vector3(cameraPosition.x, cameraPosition.y, 0f);
        _readabilityOverlayRenderer.transform.localScale = new Vector3(width, height, 1f);
    }

    private static Sprite GetReadabilityOverlaySprite()
    {
        if (s_ReadabilityOverlaySprite != null)
        {
            return s_ReadabilityOverlaySprite;
        }

        Texture2D texture = new(1, 1, TextureFormat.RGBA32, false)
        {
            name = "GameplayReadabilityBackdropTexture",
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, true);

        s_ReadabilityOverlaySprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        s_ReadabilityOverlaySprite.name = "GameplayReadabilityBackdropSprite";
        s_ReadabilityOverlaySprite.hideFlags = HideFlags.HideAndDontSave;
        return s_ReadabilityOverlaySprite;
    }

    private void EvaluateCompletion()
    {
        if (_scoreManager == null)
        {
            return;
        }

        if (_scoreManager.HasReachedMissLimit)
        {
            if (CanOfferContinue())
            {
                ShowContinuePrompt();
            }
            else
            {
                HandleGameOver();
            }

            return;
        }

        if (_scoreManager.HasReachedTargetScore)
        {
            FinishStage();
        }
    }

    private void HandleGameOver()
    {
        if (_currentState != GameState.Playing)
        {
            return;
        }

        FinalizeGameOver();
    }

    private void FinalizeGameOver()
    {
        _currentState = GameState.GameOver;
        StopGameplay();
        SoundManager.EnsureInstance().PlayGameOver();
        VibrationService.PlayGameOver();
        _playerAnimationController?.PlayCry();
        StoreResult(false);
        StartResultSceneLoad();
    }

    private bool CanOfferContinue()
    {
        return _currentState == GameState.Playing && !_hasUsedContinue;
    }

    private void ShowContinuePrompt()
    {
        if (_currentState != GameState.Playing)
        {
            return;
        }

        _hasUsedContinue = true;
        _currentState = GameState.ContinuePrompt;
        StopGameplay();

        _continuePromptController = ContinuePromptController.EnsureInstalled();
        _continuePromptController.Show(
            _scoreManager != null ? _scoreManager.CurrentScore : 0,
            OnContinueAdRequested,
            OnContinueDeclined);
    }

    private void OnContinueAdRequested()
    {
        if (_currentState != GameState.ContinuePrompt)
        {
            return;
        }

        _continuePromptController?.ShowAdWaiting();
        UnityAdsManager.Instance.ShowRewardedThenContinue(OnRewardedContinueCompleted);
    }

    private void OnContinueDeclined()
    {
        if (_currentState != GameState.ContinuePrompt)
        {
            return;
        }

        _continuePromptController?.Hide();
        FinalizeGameOver();
    }

    private void OnRewardedContinueCompleted(bool wasCompleted)
    {
        if (_currentState != GameState.ContinuePrompt)
        {
            return;
        }

        if (!wasCompleted)
        {
            _continuePromptController?.ShowAdUnavailable();
            if (_continueRoutine != null)
            {
                StopCoroutine(_continueRoutine);
            }

            _continueRoutine = StartCoroutine(FailContinueAfterDelay());
            return;
        }

        _continuePromptController?.Hide();
        _scoreManager?.ReviveFromContinue();
        _carSpawner?.DespawnAllCars();
        _playerAnimationController?.PlayHappy();

        if (_continueRoutine != null)
        {
            StopCoroutine(_continueRoutine);
        }

        _continueRoutine = StartCoroutine(RestartAfterContinueRoutine());
    }

    private IEnumerator FailContinueAfterDelay()
    {
        yield return new WaitForSecondsRealtime(ContinueFailureDelaySeconds);
        _continueRoutine = null;
        _continuePromptController?.Hide();
        FinalizeGameOver();
    }

    private IEnumerator RestartAfterContinueRoutine()
    {
        _currentState = GameState.Ready;

        if (_hudEffectsController != null)
        {
            yield return _hudEffectsController.PlayReadyCountdown();
        }

        if (_currentState != GameState.Ready)
        {
            _continueRoutine = null;
            yield break;
        }

        _carSpawner?.StartSpawning();
        _currentState = GameState.Playing;
        _continueRoutine = null;
    }

    private void FinishStage()
    {
        if (_currentState != GameState.Playing)
        {
            return;
        }

        _currentState = GameState.Result;
        StopGameplay();
        SoundManager.EnsureInstance().PlayClear();
        VibrationService.PlayClear();
        _playerAnimationController?.PlayWin();
        StoreResult(true);
        StartResultSceneLoad();
    }

    private void StopGameplay()
    {
        if (_startGameRoutine != null)
        {
            StopCoroutine(_startGameRoutine);
            _startGameRoutine = null;
        }

        _carSpawner?.StopSpawning();
        _carSpawner?.StopAllCars();
    }

    private void StoreResult(bool isClear)
    {
        int stageNumber = _currentStageDefinition != null ? _currentStageDefinition.StageNumber : SessionState.SelectedStageNumber;
        SessionState.StoreResult(GameResultData.FromScoreState(SessionState.SelectedGameMode, stageNumber, isClear, _scoreManager?.State));
    }

    private void StartResultSceneLoad()
    {
        LoadResultScene();
    }

    private static void LoadResultScene()
    {
        SceneManager.LoadScene(ResultSceneName);
    }

    private void RestoreTimeScale()
    {
        Time.timeScale = Mathf.Approximately(_timeScaleBeforePause, 0f)
            ? 1f
            : _timeScaleBeforePause;
    }
}
