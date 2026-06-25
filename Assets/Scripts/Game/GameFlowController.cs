using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameFlowController : MonoBehaviour
{
    private const float ResultSceneDelaySeconds = 0.5f;
    private const string ResultSceneName = "Result";
    private const string StageDatabaseResourcePath = "StageDatabase";

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
    private StageDatabase _stageDatabase;
    private StageDefinition _currentStageDefinition;
    private Coroutine _startGameRoutine;
    private Coroutine _resultLoadRoutine;
    private GameState _stateBeforePause = GameState.Ready;
    private float _timeScaleBeforePause = 1f;

    private void Awake()
    {
        _judgeController = FindAnyObjectByType<JudgeController>();
        _hudEffectsController ??= FindAnyObjectByType<MainHudEffectsController>();
        _stageDatabase = Resources.Load<StageDatabase>(StageDatabaseResourcePath);
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

        int selectedStageNumber = StageNumberUtility.Normalize(SessionState.SelectedStageNumber);
        _currentStageDefinition = _stageDatabase.GetStageDefinition(selectedStageNumber);
        SessionState.SelectStage(_currentStageDefinition.StageNumber);

        _scoreManager.Initialize(_currentStageDefinition);
        _carSpawner.Initialize(_currentStageDefinition);
        SoundManager.EnsureInstance().PlayBgm();

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

        CarController activeCar = _carSpawner != null ? _carSpawner.GetActiveCar() : null;
        JudgeResult result = _judgeController != null
            ? _judgeController.Evaluate(activeCar, laneType)
            : JudgeEvaluator.Evaluate(activeCar != null ? activeCar.CarType : null, laneType);

        switch (result)
        {
            case JudgeResult.Correct:
                _hudEffectsController?.ShowCorrectJudge();
                _scoreManager.ApplySuccess(laneType);
                _playerAnimationController?.PlayHappy();
                SoundManager.EnsureInstance().PlayCorrect();
                _carSpawner?.DespawnCar(activeCar);
                if (!HasReachedTerminalCondition())
                {
                    VibrationService.PlayCorrect();
                }
                break;
            case JudgeResult.WrongLane:
            case JudgeResult.NoCar:
                _hudEffectsController?.ShowMissJudge();
                _scoreManager.ApplyMiss();
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

        _scoreManager.ApplyMiss();
        _hudEffectsController?.ShowMissJudge();
        _playerAnimationController?.PlayCry();
        SoundManager.EnsureInstance().PlayMiss();
        if (!HasReachedTerminalCondition())
        {
            VibrationService.PlayMiss();
        }
        EvaluateCompletion();
    }

    private bool HasReachedTerminalCondition()
    {
        return _scoreManager != null &&
            (_scoreManager.HasReachedMissLimit || _scoreManager.HasReachedTargetScore);
    }

    private void EvaluateCompletion()
    {
        if (_scoreManager == null)
        {
            return;
        }

        if (_scoreManager.HasReachedMissLimit)
        {
            HandleGameOver();
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

        _currentState = GameState.GameOver;
        StopGameplay();
        SoundManager.EnsureInstance().PlayGameOver();
        VibrationService.PlayGameOver();
        _playerAnimationController?.PlayCry();
        StoreResult(false);
        StartResultSceneLoad();
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
        SessionState.StoreResult(GameResultData.FromScoreState(stageNumber, isClear, _scoreManager?.State));
    }

    private void StartResultSceneLoad()
    {
        if (_resultLoadRoutine != null)
        {
            return;
        }

        _resultLoadRoutine = StartCoroutine(LoadResultAfterDelay());
    }

    private IEnumerator LoadResultAfterDelay()
    {
        yield return new WaitForSeconds(ResultSceneDelaySeconds);
        _resultLoadRoutine = null;
        UnityAdsManager.Instance.ShowInterstitialThenContinue(LoadResultScene);
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
