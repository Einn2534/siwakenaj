using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class TutorialController : MonoBehaviour
{
    public enum TutorialState
    {
        Introduction,
        GuidedTruck,
        GuidedCompactCar,
        GuidedSportsCar,
        IndependentPractice,
        OnFirstMiss,
        MissExplanation,
        ClearExplanation,
        Graduation,
        Completed
    }

    private const float ReferenceWidth = 1080f;
    private const float ReferenceHeight = 1920f;
    private const float TutorialSpeedRatio = 0.7f;
    private const float TutorialSpawnIntervalSeconds = 2f;
    private const float IdleReinforceSeconds = 5f;
    private const float TruckMessageDelaySeconds = 1.15f;
    private const float FirstMissCutInSeconds = 2f;
    private const float GraduationAutoAdvanceSeconds = 1.2f;
    private const int FirstIndependentIndex = 3;
    private const int RequiredIndependentSuccessCount = 3;

    private static readonly CarType[] TutorialCars =
    {
        CarType.LightTruck,
        CarType.CompactCar,
        CarType.SportsCar,
        CarType.CompactCar,
        CarType.LightTruck,
        CarType.SportsCar
    };

    private static readonly CarType[] LaneTypes =
    {
        CarType.LightTruck,
        CarType.CompactCar,
        CarType.SportsCar
    };

    private static readonly Color ScrimColor = new(20f / 255f, 14f / 255f, 28f / 255f, 0.38f);
    private static readonly Color TextColor = new(43f / 255f, 37f / 255f, 48f / 255f, 1f);
    private static readonly Color SkipButtonColor = new(1f, 0.97f, 0.84f, 0.96f);
    private static readonly Color SkipTextColor = new(0.08f, 0.105f, 0.13f, 1f);

    private GameFlowController _gameFlowController;
    private ScoreManager _scoreManager;
    private CarSpawner _carSpawner;
    private StageDefinition _stageDefinition;
    private LaneInputController _laneInputController;
    private StageProgressHudController _progressHudController;
    private TutorialFocusGuideView _focusGuideView;
    private Canvas _canvas;
    private Image _instructionScrimImage;
    private RectTransform _speechRoot;
    private RectTransform _powaRect;
    private TMP_Text _instructionText;
    private Button _skipButton;
    private RectTransform _firstMissCutIn;
    private RectTransform _graduationRoot;
    private RectTransform _graduationCard;
    private readonly List<RectTransform> _graduationStars = new();
    private Coroutine _tutorialRoutine;
    private Coroutine _blinkRoutine;
    private Coroutine _respawnRoutine;
    private Coroutine _delayedTruckMessageRoutine;
    private Coroutine _messagePopRoutine;
    private Coroutine _powaBobRoutine;
    private readonly Dictionary<CanvasGroup, float> _originalGroupAlpha = new();
    private readonly Dictionary<RectTransform, Vector3> _originalScales = new();
    private readonly Dictionary<Button, bool> _originalInteractable = new();

    private TutorialState _state = TutorialState.Introduction;
    private CarController _activeCar;
    private CarType _expectedType;
    private float _currentCarSpeed;
    private float _lastActionTime;
    private bool _isRunning;
    private bool _isAwaitingInput;
    private bool _stepSucceeded;
    private bool _firstMissExplained;
    private bool _graduationTapped;
    private int _independentSuccessCount;
    private int _completedCarCount;
    private int _tutorialMissCount;

    public bool IsRunning => _isRunning;
    public TutorialState State => _state;

    public static TutorialController EnsureInstalled()
    {
        TutorialController controller = FindAnyObjectByType<TutorialController>(FindObjectsInactive.Include);
        if (controller != null)
        {
            return controller;
        }

        GameObject controllerObject = new("TutorialController");
        return controllerObject.AddComponent<TutorialController>();
    }

    private void Update()
    {
        if (!_isRunning || !_isAwaitingInput)
        {
            return;
        }

        if (Time.time - _lastActionTime >= IdleReinforceSeconds)
        {
            ReinforceCorrectButton();
            _lastActionTime = Time.time;
        }
    }

    private void OnDisable()
    {
        ClearButtonGuide();
    }

    public void Begin(
        GameFlowController gameFlowController,
        ScoreManager scoreManager,
        CarSpawner carSpawner,
        StageDefinition stageDefinition)
    {
        _gameFlowController = gameFlowController;
        _scoreManager = scoreManager;
        _carSpawner = carSpawner;
        _stageDefinition = stageDefinition;
        _laneInputController = FindAnyObjectByType<LaneInputController>(FindObjectsInactive.Include);
        _progressHudController = FindAnyObjectByType<StageProgressHudController>(FindObjectsInactive.Include);

        EventSystemInputModuleUtility.EnsureCompatibleEventSystem();
        BuildInterface();
        gameObject.SetActive(true);
        _canvas.gameObject.SetActive(true);
        _skipButton.gameObject.SetActive(true);
        _skipButton.interactable = true;
        _speechRoot.gameObject.SetActive(true);
        _powaRect.gameObject.SetActive(true);
        _firstMissCutIn.gameObject.SetActive(false);
        _graduationRoot.gameObject.SetActive(false);

        _completedCarCount = 0;
        _tutorialMissCount = 0;
        _firstMissExplained = false;
        _isRunning = true;
        _progressHudController?.SetTutorialMode(true);
        _progressHudController?.SetTutorialProgress(0, TutorialCars.Length, 0);

        if (_powaBobRoutine != null)
        {
            StopCoroutine(_powaBobRoutine);
        }

        _powaBobRoutine = StartCoroutine(BobPowa());

        if (_tutorialRoutine != null)
        {
            StopCoroutine(_tutorialRoutine);
        }

        _tutorialRoutine = StartCoroutine(RunTutorial());
    }

    public void HandleLaneInput(CarType laneType)
    {
        if (!_isRunning || !_isAwaitingInput)
        {
            return;
        }

        _lastActionTime = Time.time;
        if (_activeCar == null)
        {
            ReinforceCorrectButton();
            return;
        }

        if (_activeCar.CarType == laneType)
        {
            CompleteCurrentCar();
            return;
        }

        HandleTutorialMiss();
    }

    public void HandleCarMissed(CarController car)
    {
        if (!_isRunning || !_isAwaitingInput || car == null || car != _activeCar)
        {
            return;
        }

        _activeCar = null;
        _lastActionTime = Time.time;
        HandleTutorialMiss();
    }

    private IEnumerator RunTutorial()
    {
        _isRunning = true;
        _state = TutorialState.Introduction;
        _independentSuccessCount = 0;
        _carSpawner?.StopSpawning();
        _carSpawner?.DespawnAllCars();
        _scoreManager?.Initialize(_stageDefinition);

        SetInstruction("\u9b54\u6cd5\u306e\u8eca\u304c\u6765\u308b\u3088!\u540c\u3058\u30dc\u30bf\u30f3\u3092\u30bf\u30c3\u30d7\u3060\u3088\u3063\u3002");
        yield return WaitTutorialSeconds(0.6f);
        yield return RunGuidedStep(0, TutorialState.GuidedTruck, true);
        yield return WaitTutorialSeconds(TutorialSpawnIntervalSeconds);
        yield return RunGuidedStep(1, TutorialState.GuidedCompactCar, false);
        yield return WaitTutorialSeconds(TutorialSpawnIntervalSeconds);
        yield return RunGuidedStep(2, TutorialState.GuidedSportsCar, false);

        SetInstruction("\u3084\u3063\u305f\u306d!\u305d\u306e\u8abf\u5b50!");
        yield return WaitTutorialSeconds(1f);
        yield return WaitTutorialSeconds(Mathf.Max(0f, TutorialSpawnIntervalSeconds - 1f));

        _state = TutorialState.IndependentPractice;
        SetInstruction("\u0033\u53f0\u9023\u7d9a\u3067\u6210\u529f\u3057\u3088\u3046");
        for (int i = FirstIndependentIndex; i < TutorialCars.Length; i += 1)
        {
            yield return RunPracticeStep(i);
            if (i < TutorialCars.Length - 1)
            {
                yield return WaitTutorialSeconds(TutorialSpawnIntervalSeconds);
            }
        }

        if (_independentSuccessCount >= RequiredIndependentSuccessCount)
        {
            yield return RunExplanationsAndComplete();
        }
    }

    private IEnumerator RunGuidedStep(int carIndex, TutorialState state, bool isFirstTruck)
    {
        _state = state;
        if (isFirstTruck)
        {
            SetInstruction("\u6765\u305f\u8eca\u3068\u540c\u3058\u30dc\u30bf\u30f3\u3092\u62bc\u305d\u3046");
        }
        else
        {
            SetInstruction("\u6765\u305f\u8eca\u3068\u540c\u3058\u30dc\u30bf\u30f3\u3092\u62bc\u305d\u3046");
        }

        yield return RunStep(carIndex, true, isFirstTruck);
    }

    private IEnumerator RunPracticeStep(int carIndex)
    {
        yield return RunStep(carIndex, false, false);
    }

    private IEnumerator RunStep(int carIndex, bool showGuideImmediately, bool isFirstTruck)
    {
        _expectedType = TutorialCars[carIndex];
        _currentCarSpeed = isFirstTruck
            ? 0f
            : GetTutorialCarSpeed();
        _stepSucceeded = false;
        _isAwaitingInput = false;
        ClearButtonGuide();

        yield return SpawnCurrentCar();
        _isAwaitingInput = true;
        _lastActionTime = Time.time;

        if (showGuideImmediately)
        {
            ApplyButtonGuide(_expectedType);
        }

        if (isFirstTruck)
        {
            StopDelayedTruckMessage();
            _delayedTruckMessageRoutine = StartCoroutine(ShowTruckMessageAfterDelay());
        }

        while (_isRunning && !_stepSucceeded)
        {
            yield return null;
        }

        StopDelayedTruckMessage();
        ClearButtonGuide();
        _isAwaitingInput = false;
    }

    private IEnumerator RunExplanationsAndComplete()
    {
        _carSpawner?.DespawnAllCars();
        ClearButtonGuide();

        _state = TutorialState.Graduation;
        TutorialLaunchService.MarkCompleted();
        yield return ShowGraduation();
        _state = TutorialState.Completed;
        FinishAndStartRegularStage();
    }

    private void CompleteCurrentCar()
    {
        _gameFlowController?.PlayTutorialCorrectFeedback();
        if (_activeCar != null)
        {
            _carSpawner?.DespawnCar(_activeCar);
            _activeCar = null;
        }

        if (_state == TutorialState.IndependentPractice)
        {
            _independentSuccessCount += 1;
        }

        _completedCarCount += 1;
        _progressHudController?.SetTutorialProgress(_completedCarCount, TutorialCars.Length, _tutorialMissCount);
        SetInstruction("\u3084\u3063\u305f\u306d!\u305d\u306e\u8abf\u5b50!");

        _stepSucceeded = true;
    }

    private void HandleTutorialMiss()
    {
        _tutorialMissCount = Mathf.Min(3, _tutorialMissCount + 1);
        _progressHudController?.SetTutorialProgress(_completedCarCount, TutorialCars.Length, _tutorialMissCount);
        _progressHudController?.HighlightMiss();
        _gameFlowController?.PlayTutorialMissFeedback();

        if (_state == TutorialState.IndependentPractice && !_firstMissExplained)
        {
            _firstMissExplained = true;
            if (_respawnRoutine != null)
            {
                StopCoroutine(_respawnRoutine);
            }

            _respawnRoutine = StartCoroutine(ShowFirstMissAndRestart());
            return;
        }

        RestartCurrentCar();
    }

    private IEnumerator ShowFirstMissAndRestart()
    {
        _isAwaitingInput = false;
        _state = TutorialState.OnFirstMiss;
        ClearButtonGuide();
        if (_activeCar != null)
        {
            _carSpawner?.DespawnCar(_activeCar);
            _activeCar = null;
        }

        _firstMissCutIn.gameObject.SetActive(true);
        _firstMissCutIn.localScale = Vector3.one * 0.92f;
        SetInstruction("\u308f\u308f\u3063...\u30df\u30b9\u304c3\u56de\u305f\u307e\u308b\u3068\u30b2\u30fc\u30e0\u30aa\u30fc\u30d0\u30fc\u3060\u3088!");
        float elapsed = 0f;
        while (elapsed < FirstMissCutInSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.2f);
            _firstMissCutIn.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, EaseOutBack(t));
            yield return null;
        }

        _firstMissCutIn.gameObject.SetActive(false);
        _state = TutorialState.IndependentPractice;
        yield return SpawnCurrentCar();
        _isAwaitingInput = true;
        _lastActionTime = Time.time;
        _respawnRoutine = null;
    }

    private void RestartCurrentCar()
    {
        if (!_isRunning)
        {
            return;
        }

        if (_respawnRoutine != null)
        {
            StopCoroutine(_respawnRoutine);
        }

        _respawnRoutine = StartCoroutine(RestartCurrentCarRoutine());
    }

    private IEnumerator RestartCurrentCarRoutine()
    {
        _isAwaitingInput = false;
        if (_activeCar != null)
        {
            _carSpawner?.DespawnCar(_activeCar);
            _activeCar = null;
        }

        yield return null;
        yield return SpawnCurrentCar();
        _isAwaitingInput = true;
        if (ShouldShowFocusGuide())
        {
            ReinforceCorrectButton();
        }
        _respawnRoutine = null;
    }

    private IEnumerator SpawnCurrentCar()
    {
        while (_isRunning && _activeCar == null && _carSpawner != null)
        {
            _activeCar = _carSpawner.SpawnFixedCar(_expectedType, _currentCarSpeed);
            if (_activeCar != null)
            {
                if (Mathf.Approximately(_currentCarSpeed, 0f))
                {
                    PlaceStoppedCarInView(_activeCar);
                }

                yield break;
            }

            yield return null;
        }
    }

    private void PlaceStoppedCarInView(CarController car)
    {
        if (car == null || !BoundsHelper.TryGetBounds(car.gameObject, out Bounds bounds))
        {
            return;
        }

        car.transform.position += Vector3.left * (bounds.size.x * 0.95f);
    }

    private float GetTutorialCarSpeed()
    {
        float stageSpeed = _stageDefinition != null ? _stageDefinition.CarSpeed : 0.62f;
        return Mathf.Max(0f, stageSpeed * TutorialSpeedRatio);
    }

    private void ReinforceCorrectButton()
    {
        if (!_isRunning || !_isAwaitingInput)
        {
            return;
        }

        ApplyButtonGuide(_expectedType, true);
    }

    private void ApplyButtonGuide(CarType expectedType, bool reinforceOnly = false)
    {
        if (_laneInputController == null)
        {
            _laneInputController = FindAnyObjectByType<LaneInputController>(FindObjectsInactive.Include);
        }

        ClearButtonGuide();
        if (_laneInputController == null)
        {
            return;
        }

        foreach (CarType laneType in LaneTypes)
        {
            if (!_laneInputController.TryGetButtonForLane(laneType, out Button button) || button == null)
            {
                continue;
            }

            CanvasGroup group = button.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = button.gameObject.AddComponent<CanvasGroup>();
            }

            _originalGroupAlpha[group] = group.alpha;
            bool isGuided = ShouldShowFocusGuide();
            group.alpha = isGuided && laneType != expectedType ? 0.55f : 1f;
            _originalInteractable[button] = button.interactable;
            button.interactable = !isGuided || laneType == expectedType;

            if (button.transform is RectTransform rectTransform && !_originalScales.ContainsKey(rectTransform))
            {
                _originalScales[rectTransform] = rectTransform.localScale;
            }
        }

        StartBlink(expectedType);
        bool shouldShowFocusGuide = ShouldShowFocusGuide() || reinforceOnly;
        SetInstructionScrimVisible(!shouldShowFocusGuide);
        if (shouldShowFocusGuide)
        {
            _focusGuideView?.ShowFocus(expectedType, _laneInputController, _activeCar);
        }
        else
        {
            _focusGuideView?.HideFocus();
        }
    }

    private void StartBlink(CarType expectedType)
    {
        StopBlink();
        if (_laneInputController == null
            || !_laneInputController.TryGetButtonForLane(expectedType, out RectTransform rectTransform)
            || rectTransform == null)
        {
            return;
        }

        if (!_originalScales.ContainsKey(rectTransform))
        {
            _originalScales[rectTransform] = rectTransform.localScale;
        }

        _blinkRoutine = StartCoroutine(BlinkButton(rectTransform, _originalScales[rectTransform]));
    }

    private IEnumerator BlinkButton(RectTransform target, Vector3 baseScale)
    {
        while (_isRunning && target != null)
        {
            float wave = Mathf.PingPong(Time.unscaledTime / 0.3f, 1f);
            target.localScale = baseScale * Mathf.Lerp(1f, 1.06f, wave);
            yield return null;
        }
    }

    private void ClearButtonGuide()
    {
        StopBlink();
        _focusGuideView?.HideFocus();
        SetInstructionScrimVisible(true);

        foreach (KeyValuePair<CanvasGroup, float> entry in _originalGroupAlpha)
        {
            if (entry.Key != null)
            {
                entry.Key.alpha = entry.Value;
            }
        }

        foreach (KeyValuePair<RectTransform, Vector3> entry in _originalScales)
        {
            if (entry.Key != null)
            {
                entry.Key.localScale = entry.Value;
            }
        }

        foreach (KeyValuePair<Button, bool> entry in _originalInteractable)
        {
            if (entry.Key != null)
            {
                entry.Key.interactable = entry.Value;
            }
        }

        _originalGroupAlpha.Clear();
        _originalScales.Clear();
        _originalInteractable.Clear();
    }

    private bool ShouldShowFocusGuide()
    {
        return _state == TutorialState.GuidedTruck
            || _state == TutorialState.GuidedCompactCar
            || _state == TutorialState.GuidedSportsCar;
    }

    private void SetInstructionScrimVisible(bool isVisible)
    {
        if (_instructionScrimImage != null)
        {
            _instructionScrimImage.enabled = isVisible;
        }
    }

    private void StopBlink()
    {
        if (_blinkRoutine != null)
        {
            StopCoroutine(_blinkRoutine);
            _blinkRoutine = null;
        }
    }

    private IEnumerator ShowTruckMessageAfterDelay()
    {
        yield return WaitTutorialSeconds(TruckMessageDelaySeconds);
        if (_isRunning && _state == TutorialState.GuidedTruck && !_stepSucceeded)
        {
            SetInstruction("\u30c8\u30e9\u30c3\u30af\u3060\u3002\u30c8\u30e9\u30c3\u30af\u3092\u30bf\u30c3\u30d7");
        }

        _delayedTruckMessageRoutine = null;
    }

    private void StopDelayedTruckMessage()
    {
        if (_delayedTruckMessageRoutine != null)
        {
            StopCoroutine(_delayedTruckMessageRoutine);
            _delayedTruckMessageRoutine = null;
        }
    }

    private void SkipTutorial()
    {
        if (!_isRunning)
        {
            return;
        }

        TutorialLaunchService.MarkSkipped();
        if (_tutorialRoutine != null)
        {
            StopCoroutine(_tutorialRoutine);
            _tutorialRoutine = null;
        }

        StartCoroutine(SkipTutorialRoutine());
    }

    private IEnumerator SkipTutorialRoutine()
    {
        _isAwaitingInput = false;
        _skipButton.interactable = false;
        StopDelayedTruckMessage();
        ClearButtonGuide();
        _carSpawner?.DespawnAllCars();
        _scoreManager?.Initialize(_stageDefinition);
        SetInstruction("\u672c\u756a\u30b9\u30bf\u30fc\u30c8");
        yield return WaitTutorialSeconds(0.45f);
        FinishAndStartRegularStage();
    }

    private void FinishAndStartRegularStage()
    {
        _isAwaitingInput = false;
        _isRunning = false;
        StopDelayedTruckMessage();
        ClearButtonGuide();
        _carSpawner?.DespawnAllCars();
        _progressHudController?.SetTutorialMode(false);
        if (_powaBobRoutine != null)
        {
            StopCoroutine(_powaBobRoutine);
            _powaBobRoutine = null;
        }
        _canvas.gameObject.SetActive(false);
        _gameFlowController?.StartRegularStageAfterTutorial();
    }

    private IEnumerator WaitTutorialSeconds(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void SetInstruction(string message)
    {
        if (_instructionText != null)
        {
            _instructionText.text = message;
            if (_messagePopRoutine != null)
            {
                StopCoroutine(_messagePopRoutine);
            }

            _messagePopRoutine = StartCoroutine(PopMessage());
        }
    }

    private void BuildInterface()
    {
        if (_canvas != null)
        {
            return;
        }

        GameObject canvasObject = new("TutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = LayerMask.NameToLayer("UI");
        canvasObject.transform.SetParent(transform, false);

        _canvas = canvasObject.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 90;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform safeRoot = CreateUiObject("SafeAreaRoot", canvasObject.transform);
        Stretch(safeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safeRoot.gameObject.AddComponent<SafeAreaFitter>();

        RectTransform scrim = CreatePanel("InstructionScrim", safeRoot, ScrimColor);
        Stretch(scrim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _instructionScrimImage = scrim.GetComponent<Image>();
        _instructionScrimImage.raycastTarget = false;

        _focusGuideView = gameObject.GetComponent<TutorialFocusGuideView>();
        if (_focusGuideView == null)
        {
            _focusGuideView = gameObject.AddComponent<TutorialFocusGuideView>();
        }

        _focusGuideView.Initialize(_canvas, safeRoot);

        _powaRect = CreateSpriteImage("Powa", safeRoot, LoadSprite("UI/Tutorial/powa_idle"), true);
        SetAnchored(_powaRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(6f, 343f), new Vector2(330f, 470f));

        _speechRoot = CreateSpriteImage("SpeechPanel", safeRoot, LoadSprite("UI/Tutorial/speech_panel"), false);
        SetAnchored(_speechRoot, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(121f, 654f), new Vector2(-318f, 214f));
        Image speechImage = _speechRoot.GetComponent<Image>();
        speechImage.type = Image.Type.Sliced;

        RectTransform speechTail = CreateSpriteImage("SpeechTail", safeRoot, LoadSprite("UI/Tutorial/speech_tail"), true);
        SetAnchored(speechTail, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(282f, 664f), new Vector2(128f, 128f));
        speechTail.SetSiblingIndex(_speechRoot.GetSiblingIndex() + 1);

        _instructionText = CreateText("InstructionText", _speechRoot, string.Empty, 43f, 31f, FontStyles.Bold, TextAlignmentOptions.Center, TextColor);
        _instructionText.font = LoadFont("UI/Tutorial/YomiyasuWide-Bold SDF");
        Stretch((RectTransform)_instructionText.transform, Vector2.zero, Vector2.one, new Vector2(38f, 28f), new Vector2(-38f, -28f));
        _instructionText.textWrappingMode = TextWrappingModes.Normal;

        _skipButton = CreateButton("SkipButton", safeRoot, "SKIP", new Vector2(32f, -32f), new Vector2(152f, 72f));
        _skipButton.onClick.AddListener(SkipTutorial);

        BuildFirstMissCutIn(safeRoot);
        BuildGraduation(safeRoot);
    }

    private void BuildFirstMissCutIn(RectTransform parent)
    {
        _firstMissCutIn = CreatePanel("FirstMissCutIn", parent, ScrimColor);
        Stretch(_firstMissCutIn, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _firstMissCutIn.GetComponent<Image>().raycastTarget = false;

        RectTransform glow = CreatePanel("MissGlow", _firstMissCutIn, new Color(1f, 217f / 255f, 74f / 255f, 0.32f));
        SetAnchored(glow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 440f), new Vector2(650f, 190f));
        AddOutline(glow.gameObject, new Color(1f, 217f / 255f, 74f / 255f, 0.85f), new Vector2(8f, 8f));

        RectTransform judge = CreateSpriteImage("MissJudge", _firstMissCutIn, LoadSprite("UI/Tutorial/judge_miss"), true);
        SetAnchored(judge, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 440f), new Vector2(540f, 170f));
        _firstMissCutIn.gameObject.SetActive(false);
    }

    private void BuildGraduation(RectTransform parent)
    {
        _graduationRoot = CreatePanel("Graduation", parent, new Color(20f / 255f, 14f / 255f, 28f / 255f, 0.55f), typeof(Button));
        Stretch(_graduationRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Button backdropButton = _graduationRoot.GetComponent<Button>();
        backdropButton.targetGraphic = _graduationRoot.GetComponent<Image>();
        backdropButton.onClick.AddListener(() => _graduationTapped = true);

        RectTransform graduatePowa = CreateSpriteImage("GraduatePowa", _graduationRoot, LoadSprite("UI/Tutorial/powa_idle"), true);
        SetAnchored(graduatePowa, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -326f), new Vector2(476f, 520f));

        _graduationCard = CreateSpriteImage("GraduationCard", _graduationRoot, LoadSprite("UI/Tutorial/speech_panel"), false);
        SetAnchored(_graduationCard, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -690f), new Vector2(914f, 620f));
        _graduationCard.GetComponent<Image>().type = Image.Type.Sliced;

        for (int i = 0; i < 3; i += 1)
        {
            RectTransform star = CreateSpriteImage($"Star{i + 1}", _graduationCard, LoadSprite("UI/Tutorial/star_filled"), true);
            SetAnchored(star, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2((i - 1) * 114f, -74f), new Vector2(95f, 95f));
            _graduationStars.Add(star);
        }

        TMP_Text title = CreateText("Title", _graduationCard, "\u898b\u7fd2\u3044\u5352\u696d\u3060\u3088!", 67f, 48f, FontStyles.Bold, TextAlignmentOptions.Center, TextColor);
        title.font = LoadFont("UI/Tutorial/YomiyasuWide-Bold SDF");
        SetAnchored((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -204f), new Vector2(820f, 100f));

        TMP_Text subtitle = CreateText("Subtitle", _graduationCard, "TUTORIAL COMPLETE", 41f, 30f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(43f / 255f, 37f / 255f, 48f / 255f, 0.55f));
        subtitle.font = LoadFont("UI/Tutorial/DotGothic16-Regular SDF");
        SetAnchored((RectTransform)subtitle.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -298f), new Vector2(760f, 70f));

        RectTransform cta = CreatePanel("StageOneButton", _graduationCard, new Color(1f, 197f / 255f, 66f / 255f, 1f));
        SetAnchored(cta, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(780f, 170f));
        AddOutline(cta.gameObject, TextColor, new Vector2(10f, 10f));
        TMP_Text ctaLabel = CreateText("Label", cta, "\u30b9\u30c6\u30fc\u30b81\u3078\n<TAP TO PLAY>", 52f, 28f, FontStyles.Bold, TextAlignmentOptions.Center, TextColor);
        ctaLabel.font = LoadFont("UI/Tutorial/YomiyasuWide-Bold SDF");
        Stretch((RectTransform)ctaLabel.transform, Vector2.zero, Vector2.one, new Vector2(24f, 14f), new Vector2(-24f, -14f));
        _graduationRoot.gameObject.SetActive(false);
    }

    private IEnumerator ShowGraduation()
    {
        _speechRoot.gameObject.SetActive(false);
        _powaRect.gameObject.SetActive(false);
        _skipButton.gameObject.SetActive(false);
        _graduationTapped = false;
        _graduationRoot.gameObject.SetActive(true);
        _graduationCard.localScale = Vector3.one * 0.92f;
        foreach (RectTransform star in _graduationStars)
        {
            star.localScale = Vector3.zero;
        }

        float introElapsed = 0f;
        while (introElapsed < 0.25f)
        {
            introElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(introElapsed / 0.25f);
            _graduationCard.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, EaseOutBack(t));
            yield return null;
        }

        for (int i = 0; i < _graduationStars.Count; i += 1)
        {
            float elapsed = 0f;
            while (elapsed < 0.12f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.12f);
                _graduationStars[i].localScale = Vector3.one * EaseOutBack(t);
                yield return null;
            }
        }

        float wait = 0f;
        while (!_graduationTapped && wait < GraduationAutoAdvanceSeconds)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator BobPowa()
    {
        Vector2 basePosition = _powaRect.anchoredPosition;
        while (_isRunning && _powaRect != null)
        {
            float y = Mathf.Sin(Time.unscaledTime * 2.2f) * 7f;
            _powaRect.anchoredPosition = basePosition + new Vector2(0f, y);
            yield return null;
        }

        if (_powaRect != null)
        {
            _powaRect.anchoredPosition = basePosition;
        }
    }

    private IEnumerator PopMessage()
    {
        if (_speechRoot == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.15f);
            _speechRoot.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, EaseOutBack(t));
            yield return null;
        }

        _speechRoot.localScale = Vector3.one;
        _messagePopRoutine = null;
    }

    private static float EaseOutBack(float value)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float t = value - 1f;
        return 1f + (c3 * t * t * t) + (c1 * t * t);
    }

    private static RectTransform CreateSpriteImage(string name, Transform parent, Sprite sprite, bool preserveAspect)
    {
        RectTransform rect = CreateUiObject(name, parent, typeof(Image));
        Image image = rect.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        image.color = Color.white;
        return rect;
    }

    private static Sprite LoadSprite(string path)
    {
        return Resources.Load<Sprite>(path);
    }

    private static TMP_FontAsset LoadFont(string path)
    {
        return Resources.Load<TMP_FontAsset>(path) ?? TMP_Settings.defaultFontAsset;
    }

    private static void AddOutline(GameObject target, Color color, Vector2 distance)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline == null)
        {
            outline = target.AddComponent<Outline>();
        }

        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreatePanel(name, parent, SkipButtonColor, typeof(Button));
        SetAnchored(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);

        Button button = rect.GetComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        ApplyButtonColors(button);

        TMP_Text text = CreateText("Label", rect, label, 30f, 18f, FontStyles.Bold, TextAlignmentOptions.Center, SkipTextColor);
        Stretch((RectTransform)text.transform, Vector2.zero, Vector2.one, new Vector2(18f, 8f), new Vector2(-18f, -8f));
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return button;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color, params System.Type[] extraComponents)
    {
        System.Type[] components = new System.Type[extraComponents.Length + 1];
        components[0] = typeof(Image);
        for (int i = 0; i < extraComponents.Length; i += 1)
        {
            components[i + 1] = extraComponents[i];
        }

        RectTransform rect = CreateUiObject(name, parent, components);
        Image image = rect.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return rect;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string value,
        float fontSizeMax,
        float fontSizeMin,
        FontStyles fontStyle,
        TextAlignmentOptions alignment,
        Color color)
    {
        RectTransform rect = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSizeMax;
        text.fontSizeMax = fontSizeMax;
        text.fontSizeMin = fontSizeMin;
        text.enableAutoSizing = true;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static RectTransform CreateUiObject(string name, Transform parent, params System.Type[] components)
    {
        System.Type[] allComponents = new System.Type[components.Length + 2];
        allComponents[0] = typeof(RectTransform);
        allComponents[1] = typeof(CanvasRenderer);
        for (int i = 0; i < components.Length; i += 1)
        {
            allComponents[i + 2] = components[i];
        }

        GameObject gameObject = new(name, allComponents);
        gameObject.layer = LayerMask.NameToLayer("UI");
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        return (RectTransform)gameObject.transform;
    }

    private static void ApplyButtonColors(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.selectedColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.82f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.42f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    private static void SetAnchored(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;
    }
}
