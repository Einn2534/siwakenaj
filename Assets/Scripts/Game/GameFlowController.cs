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
    private GimmickHudController _gimmickHudController;
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
            _carSpawner.CarSpawned += OnCarSpawned;
            _carSpawner.RushWarning += OnRushWarning;
            _carSpawner.RushStarted += OnRushStarted;
        }
    }

    private void OnDisable()
    {
        if (_carSpawner != null)
        {
            _carSpawner.CarMissed -= OnCarMissed;
            _carSpawner.CarSpawned -= OnCarSpawned;
            _carSpawner.RushWarning -= OnRushWarning;
            _carSpawner.RushStarted -= OnRushStarted;
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
        if (_gimmickHudController != null)
        {
            _gimmickHudController.StopEffects();
        }
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
        _gimmickHudController?.SetGameplayActive(false);
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
        _gimmickHudController?.SetGameplayActive(_currentState == GameState.Playing);
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
        _gimmickHudController = GimmickHudController.EnsureInstalled();
        _gimmickHudController.Initialize(_currentStageDefinition, ResolveLaneInputController());
        _gimmickHudController.UpdateState(_scoreManager.State);
        _gimmickHudController.SetGameplayActive(false);
        SoundManager.EnsureInstance().PlayBgm();

        if (!SessionState.IsEndlessMode && TutorialLaunchService.ShouldStartTutorial(_currentStageDefinition.StageNumber))
        {
            _currentState = GameState.Playing;
            _gimmickHudController.SetGameplayActive(true);
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
        _gimmickHudController.SetGameplayActive(true);
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
        if (activeCar != null && activeCar.RequiresRepair)
        {
            ApplyMiss("修理が必要！");
            ResolveLaneInputController()?.PlayNoCarFeedback(laneType);
            EvaluateCompletion();
            return;
        }

        if (activeCar != null && activeCar.IsCovered && !activeCar.IsRevealed)
        {
            ApplyMiss("まだ見えない！");
            ResolveLaneInputController()?.PlayNoCarFeedback(laneType);
            EvaluateCompletion();
            return;
        }

        JudgeResult result = _judgeController != null
            ? _judgeController.Evaluate(activeCar, laneType)
            : JudgeEvaluator.Evaluate(activeCar != null ? activeCar.CarType : null, laneType);

        bool wasCorrect = false;
        switch (result)
        {
            case JudgeResult.Correct:
                ApplyCorrect(activeCar);
                wasCorrect = true;
                break;
            case JudgeResult.WrongLane:
                ApplyMiss("ちがう車!");
                ResolveLaneInputController()?.PlayWrongLaneFeedback(laneType, activeCar.CarType);
                break;
            case JudgeResult.NoCar:
                ApplyMiss("車がいない!");
                ResolveLaneInputController()?.PlayNoCarFeedback(laneType);
                break;
        }

        EvaluateCompletion();
        if (wasCorrect && IsPlaying())
        {
            TryTriggerRush();
        }
    }

    public void HandleRepairInput()
    {
        if (!IsPlaying())
        {
            return;
        }

        if (_tutorialController != null && _tutorialController.IsRunning)
        {
            return;
        }

        CarController activeCar = _carSpawner != null ? _carSpawner.GetActiveCar() : null;
        bool wasCorrect = activeCar != null && activeCar.RequiresRepair;
        if (wasCorrect)
        {
            ApplyCorrect(activeCar);
        }
        else
        {
            ApplyMiss(activeCar == null ? "車がいない!" : "修理は不要！");
        }

        EvaluateCompletion();
        if (wasCorrect && IsPlaying())
        {
            TryTriggerRush();
        }
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

        ApplyMiss("見逃し!");
        EvaluateCompletion();
    }

    private void ApplyCorrect(CarController car)
    {
        if (car == null || _scoreManager == null)
        {
            return;
        }

        _hudEffectsController?.ShowCorrectJudge();
        _scoreManager.ApplySuccess(car.CarType, car.ScoreMultiplier);
        _gimmickHudController?.UpdateState(_scoreManager.State);
        ResolveStageProgressHudController()?.HighlightGoal();
        _playerAnimationController?.PlayHappy();
        SoundManager.EnsureInstance().PlayCorrect();
        _carSpawner?.DespawnCar(car);
        if (!HasReachedTerminalCondition())
        {
            VibrationService.PlayCorrect();
        }
    }

    private void ApplyMiss(string label)
    {
        _scoreManager?.ApplyMiss();
        _gimmickHudController?.UpdateState(_scoreManager?.State);
        _hudEffectsController?.ShowMissJudge(label);
        ResolveStageProgressHudController()?.HighlightMiss();
        _playerAnimationController?.PlayCry();
        SoundManager.EnsureInstance().PlayMiss();
        if (!HasReachedTerminalCondition())
        {
            VibrationService.PlayMiss();
        }
    }

    private void TryTriggerRush()
    {
        if (_currentStageDefinition == null
            || _currentStageDefinition.RushEveryCorrect <= 0
            || _scoreManager == null
            || _scoreManager.TotalCorrectCount <= 0
            || _scoreManager.TotalCorrectCount % _currentStageDefinition.RushEveryCorrect != 0)
        {
            return;
        }

        _carSpawner?.TryStartRush();
    }

    private void OnCarSpawned(CarController car)
    {
        if (car != null)
        {
            _gimmickHudController?.ShowModifierHint(car.Modifier);
        }
    }

    private void OnRushWarning()
    {
        _gimmickHudController?.ShowRushWarning();
    }

    private void OnRushStarted()
    {
        _gimmickHudController?.ShowRushStarted();
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
        _gimmickHudController?.Initialize(_currentStageDefinition, ResolveLaneInputController());
        _gimmickHudController?.UpdateState(_scoreManager.State);
        _gimmickHudController?.SetGameplayActive(false);
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
        _gimmickHudController?.SetGameplayActive(true);
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
        _gimmickHudController?.UpdateState(_scoreManager?.State);
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
        _gimmickHudController?.SetGameplayActive(false);

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
        _gimmickHudController?.SetGameplayActive(true);
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
        _gimmickHudController?.SetGameplayActive(false);
        _gimmickHudController?.StopEffects();
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
