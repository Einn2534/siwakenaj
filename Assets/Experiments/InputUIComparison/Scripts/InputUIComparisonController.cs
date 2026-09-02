using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Siwakenja.Experiments.InputUIComparison
{
    [DisallowMultipleComponent]
    public sealed class InputUIComparisonController : MonoBehaviour
    {
        [Header("Human Rush preregistration (P00 ignores this and runs all three technical presets)")]
        [SerializeField]
        private HumanRushRegistration _humanRushRegistration = new HumanRushRegistration();

        [Header("Shared vehicle stimuli (identical in every experimental condition)")]
        [SerializeField] private Sprite _sportsVehicleSprite;
        [SerializeField] private Sprite _compactVehicleSprite;
        [SerializeField] private Sprite _lightTruckVehicleSprite;

        private enum RunPhase
        {
            Setup,
            BlockIntro,
            Practice,
            MeasuredIntro,
            Measured,
            Survey,
            Complete,
        }

        private sealed class VehicleView
        {
            public RectTransform Rect;
            public UnityEngine.UI.Image Image;
            public UnityEngine.UI.Image VehicleIcon;
            public UnityEngine.UI.Text VehicleLabel;
            public UnityEngine.UI.Text StateLabel;
            public GameObject TargetBadge;
            public GameObject CoverOverlay;
            public ExperimentTrial Trial;
            public string VehicleId;
            public float StartX;
        }

        private readonly List<ExperimentTrialRecord> _trialRecords = new List<ExperimentTrialRecord>();
        private readonly List<ExperimentInputEventRecord> _eventRecords = new List<ExperimentInputEventRecord>();
        private readonly List<ExperimentSurveyRecord> _surveyRecords = new List<ExperimentSurveyRecord>();
        private readonly List<ExperimentInputEventRecord> _pendingFrameEvents = new List<ExperimentInputEventRecord>();
        private readonly Dictionary<ExperimentInput, ExperimentPointerInput> _controls =
            new Dictionary<ExperimentInput, ExperimentPointerInput>();
        private readonly ExperimentRatingControl[] _ratings = new ExperimentRatingControl[8];
        private readonly UnityEngine.UI.InputField[] _shortAnswers = new UnityEngine.UI.InputField[8];
        private readonly VehicleView[] _vehicleViews = new VehicleView[3];

        private RunPhase _phase;
        private UnityEngine.UI.CanvasScaler _canvasScaler;
        private Canvas _canvas;
        private RectTransform _safeRoot;
        private RectTransform _contentRoot;
        private GameObject _setupPanel;
        private GameObject _runPanel;
        private GameObject _surveyPanel;
        private GameObject _summaryPanel;
        private GameObject _runOverlay;
        private UnityEngine.UI.Text _setupValidation;
        private UnityEngine.UI.InputField _participantField;
        private UnityEngine.UI.Text _experimentTypeValue;
        private UnityEngine.UI.Text _assistAnchorValue;
        private UnityEngine.UI.Button _experimentTypeButton;
        private UnityEngine.UI.Button _assistAnchorButton;
        private UnityEngine.UI.Button _resumeButton;
        private UnityEngine.UI.Text _blockLabel;
        private UnityEngine.UI.Text _progressLabel;
        private UnityEngine.UI.Text _scoreLabel;
        private UnityEngine.UI.Text _feedbackLabel;
        private RectTransform _targetMarker;
        private UnityEngine.UI.Text _overlayTitle;
        private UnityEngine.UI.Text _overlayBody;
        private UnityEngine.UI.Button _overlayButton;
        private UnityEngine.UI.Text _overlayButtonLabel;
        private RectTransform _controlRoot;
        private UnityEngine.UI.Text _surveyHeader;
        private UnityEngine.UI.Text _surveyValidation;
        private UnityEngine.UI.Text _summaryText;
        private UnityEngine.UI.Text _summaryPath;
        private Action _overlayAction;
        private GlobalRawPointerCapture _rawCapture;

        private ExperimentType _selectedExperimentType = ExperimentType.Placement;
        private RepairAnchorMode _selectedAssistAnchor = RepairAnchorMode.UpperFixed;
        private bool _sessionConfigurationLocked;
        private string _participantId;
        private string _sessionId;
        private ExperimentSchedule _schedule;
        private int _blockIndex;
        private ExperimentConditionAssignment _assignment;
        private ExperimentLayoutSpec _currentLayout;
        private List<ExperimentTrial> _activeTrials;
        private int _trialIndex;
        private ExperimentTrial _currentTrial;
        private ExperimentTrialRecord _currentRecord;
        private InputUIExperimentCsvWriter _csvWriter;
        private SessionPersistence _sessionPersistence;
        private ExperimentSessionManifest _manifest;

        private float _trialStartedAt;
        private float _actionableStartedAt;
        private double _trialStartedAtMonotonicMs;
        private double _actionableStartedAtMonotonicMs = double.PositiveInfinity;
        private double _lastAcceptedEventTimeMs = -1d;
        private float _lastPointerAt = -100f;
        private Vector2 _lastPointerPosition;
        private bool _hasLastPointer;
        private bool _coveredRevealed;
        private bool _trialResolving;
        private float _nextTrialAt;
        private int _eventIndex;
        private int _score;
        private int _misses;
        private bool _previousTrialHadError;
        private string _currentTargetVehicleId = string.Empty;
        private Rect _currentRepairRect;
        private Rect _previousRepairRect;
        private bool _hasCurrentRepairRect;
        private bool _hasPreviousRepairRect;
        private double _repairRectChangedAtMs;
        private bool _verificationFreeze;

        public IReadOnlyList<ExperimentInputEventRecord> RawEventsForTests => _eventRecords;
        public IReadOnlyList<ExperimentTrialRecord> TrialRecordsForTests => _trialRecords;
        public ExperimentLayoutSpec CurrentLayoutForTests => _currentLayout;
        public string CurrentTargetVehicleIdForTests => _currentTargetVehicleId;
        public bool SessionConfigurationLockedForTests => _sessionConfigurationLocked;
        public GlobalRawPointerCapture RawCaptureForTests => _rawCapture;
        public Rect CurrentRepairRectForTests => _currentRepairRect;
        public Rect PreviousRepairRectForTests => _previousRepairRect;
        public bool HasCurrentRepairRectForTests => _hasCurrentRepairRect;
        public bool HasPreviousRepairRectForTests => _hasPreviousRepairRect;
        public int TrialIndexForTests => _trialIndex;
        public bool TrialResolvingForTests => _trialResolving;
        public string PhaseForTests => _phase.ToString();
        public int CurrentTrialNumberForTests => _currentTrial == null ? 0 : _currentTrial.TrialNumber;
        public int SurveyRecordCountForTests => _surveyRecords.Count;
        public string CurrentConditionForTests => _assignment == null ? string.Empty : _assignment.Condition;
        public int ActiveVehicleCountForTests => _vehicleViews.Count(view =>
            view != null && view.Rect != null && view.Rect.gameObject.activeSelf);
        public IReadOnlyList<string> ActiveVehicleIdsForTests => _vehicleViews
            .Where(view => view != null && view.Rect != null && view.Rect.gameObject.activeSelf)
            .Select(view => view.VehicleId)
            .ToArray();

        private bool IsTrialRunning => _phase == RunPhase.Practice || _phase == RunPhase.Measured;
        private bool IsRush => _assignment != null && _assignment.ExperimentType == ExperimentType.RushPackage;

        private void Awake()
        {
            // The URP Rendering Debugger can be opened by multi-touch gestures in a
            // Development Build. P00 deliberately exercises multi-touch, so disable
            // the runtime debugger before it can cover the experiment or add overhead.
            UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;
            Application.targetFrameRate = 60;
            BuildInterface();
            _rawCapture = gameObject.GetComponent<GlobalRawPointerCapture>();
            if (_rawCapture == null) _rawCapture = gameObject.AddComponent<GlobalRawPointerCapture>();
            _rawCapture.PointerDownCaptured += OnPhysicalPointerDown;
            ShowSetup();
        }

        private void OnDestroy()
        {
            if (_rawCapture != null) _rawCapture.PointerDownCaptured -= OnPhysicalPointerDown;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _sessionConfigurationLocked && _phase != RunPhase.Complete)
            {
                PersistInterruptedState();
            }
        }

        private void OnApplicationQuit()
        {
            if (_sessionConfigurationLocked && _phase != RunPhase.Complete) PersistInterruptedState();
        }

        private void Update()
        {
            if (_verificationFreeze || !IsTrialRunning || _currentTrial == null || _trialResolving) return;

            float elapsed = Time.unscaledTime - _trialStartedAt;
            float duration = InputUIComparisonRules.TravelSeconds(_currentTrial.Speed, _currentTrial.SpecialState);
            float progress = Mathf.Clamp01(elapsed / duration);
            UpdateVehicleViews(elapsed, progress);

            float revealProgress = _currentTrial.CoveredRevealProgress > 0f
                ? _currentTrial.CoveredRevealProgress
                : InputUIComparisonRules.CoveredRevealProgress;
            if (_currentTrial.SpecialState == ExperimentSpecialState.Covered
                && !_coveredRevealed
                && progress >= revealProgress)
            {
                RevealCoveredCar();
            }

            if (elapsed >= duration) ResolveTimeout();
        }

        private void LateUpdate()
        {
            FlushPendingInputFrame();
        }

        private void BuildInterface()
        {
            GameObject canvasObject = new GameObject(
                "InputUIComparisonCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler),
                typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;
            _canvasScaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            _canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _canvasScaler.referenceResolution = ExperimentLayoutCatalog.ReferenceResolution;
            _canvasScaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _canvasScaler.matchWidthOrHeight = 0f;

            UnityEngine.UI.Image background = ExperimentUIFactory.Image(
                "Background", canvasObject.transform, ExperimentUIFactory.CanvasBackground);
            ExperimentUIFactory.Stretch(background.rectTransform);
            background.raycastTarget = false;

            _safeRoot = ExperimentUIFactory.Rect("SafeArea", canvasObject.transform);
            ExperimentUIFactory.Stretch(_safeRoot);
            _safeRoot.gameObject.AddComponent<ExperimentSafeArea>();

            _contentRoot = ExperimentUIFactory.Rect("ReferenceFrame9x16", _safeRoot);
            ExperimentUIFactory.Stretch(_contentRoot);
            UnityEngine.UI.AspectRatioFitter aspect =
                _contentRoot.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            aspect.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = 9f / 16f;

            EnsureEventSystem();
            BuildSetupPanel();
            BuildRunPanel();
            BuildSurveyPanel();
            BuildSummaryPanel();
        }

        private void BuildSetupPanel()
        {
            UnityEngine.UI.Image panel = ExperimentUIFactory.Image(
                "SetupPanel", _contentRoot, ExperimentUIFactory.Panel);
            ExperimentUIFactory.Stretch(panel.rectTransform, 54f, 70f);
            _setupPanel = panel.gameObject;

            UnityEngine.UI.Text title = ExperimentUIFactory.Text(
                "Title",
                panel.transform,
                "入力UI 比較実験 v" + InputUIExperimentSchema.CurrentExperimentVersion,
                50,
                TextAnchor.MiddleCenter);
            ExperimentUIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(900f, 100f), new Vector2(0f, -105f));

            UnityEngine.UI.Text intro = ExperimentUIFactory.Text(
                "Intro",
                panel.transform,
                "P00：3実験すべての技術予行。人間本試験の同日まとめ実施は禁止。\nP01〜P06：Placementのみ → anchor決定 → State Assistは別session（最低別日）\n→ 2候補を固定 → Rushを最後に反証します。",
                24,
                TextAnchor.MiddleLeft,
                ExperimentUIFactory.MutedInk);
            ExperimentUIFactory.Place(intro.rectTransform, new Vector2(0.5f, 1f), new Vector2(860f, 240f), new Vector2(0f, -280f));

            UnityEngine.UI.Text participantLabel = ExperimentUIFactory.Text(
                "ParticipantLabel", panel.transform, "匿名参加者ID（P00 / P01〜）", 28, TextAnchor.MiddleLeft);
            ExperimentUIFactory.Place(participantLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(760f, 48f), new Vector2(0f, 320f));
            _participantField = ExperimentUIFactory.InputField("ParticipantInput", panel.transform, "例：P00 / P01", 31);
            ExperimentUIFactory.Place(_participantField.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(760f, 82f), new Vector2(0f, 255f));

            _experimentTypeButton = ExperimentUIFactory.Button(
                "ExperimentTypeSelector", panel.transform, string.Empty, 30, CycleExperimentType);
            ExperimentUIFactory.Place(_experimentTypeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(760f, 98f), new Vector2(0f, 130f));
            _experimentTypeValue = _experimentTypeButton.GetComponentInChildren<UnityEngine.UI.Text>();

            _assistAnchorButton = ExperimentUIFactory.Button(
                "AssistAnchorSelector", panel.transform, string.Empty, 28, CycleAssistAnchor);
            ExperimentUIFactory.Place(_assistAnchorButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(760f, 90f), new Vector2(0f, 15f));
            _assistAnchorValue = _assistAnchorButton.GetComponentInChildren<UnityEngine.UI.Text>();

            UnityEngine.UI.Button start = ExperimentUIFactory.Button(
                "StartButton", panel.transform, "説明を確認して開始", 34, TryStartSession);
            ExperimentUIFactory.Place(start.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(700f, 104f), new Vector2(0f, 260f));

            _resumeButton = ExperimentUIFactory.Button(
                "ResumeButton", panel.transform, "P00 / 中断セッションを再開", 27, TryResumeSession);
            ExperimentUIFactory.Place(_resumeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(700f, 82f), new Vector2(0f, 150f));

            _setupValidation = ExperimentUIFactory.Text(
                "Validation", panel.transform, string.Empty, 25, TextAnchor.MiddleCenter, ExperimentUIFactory.Error);
            ExperimentUIFactory.Place(_setupValidation.rectTransform, new Vector2(0.5f, 0f), new Vector2(880f, 62f), new Vector2(0f, 72f));
            RefreshSetupSelectors();
        }

        private void BuildRunPanel()
        {
            RectTransform panel = ExperimentUIFactory.Rect("RunPanel", _contentRoot);
            ExperimentUIFactory.Stretch(panel);
            _runPanel = panel.gameObject;

            _blockLabel = ExperimentUIFactory.Text("BlockLabel", panel, "UI 1 / 2", 36, TextAnchor.MiddleLeft);
            _blockLabel.raycastTarget = true;
            ExperimentUIFactory.Place(_blockLabel.rectTransform, new Vector2(0f, 1f), new Vector2(330f, 78f), new Vector2(185f, -70f));
            _progressLabel = ExperimentUIFactory.Text("ProgressLabel", panel, "練習 1 / 4", 29, TextAnchor.MiddleCenter, ExperimentUIFactory.MutedInk);
            _progressLabel.raycastTarget = true;
            ExperimentUIFactory.Place(_progressLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(380f, 78f), new Vector2(0f, -70f));
            _scoreLabel = ExperimentUIFactory.Text("ScoreLabel", panel, "SCORE 0  MISS 0", 29, TextAnchor.MiddleRight);
            _scoreLabel.raycastTarget = true;
            ExperimentUIFactory.Place(_scoreLabel.rectTransform, new Vector2(1f, 1f), new Vector2(370f, 78f), new Vector2(-210f, -70f));

            UnityEngine.UI.Image playBand = ExperimentUIFactory.Image("PlayBand", panel, new Color32(225, 229, 233, 255));
            playBand.raycastTarget = false;
            ExperimentUIFactory.Place(playBand.rectTransform, new Vector2(0f, 0.5f), new Vector2(1010f, 450f), new Vector2(540f, 160f));

            UnityEngine.UI.Image target = ExperimentUIFactory.Image("TargetMarker", panel, new Color32(205, 211, 217, 110));
            target.raycastTarget = true;
            _targetMarker = target.rectTransform;
            ExperimentUIFactory.Place(target.rectTransform, new Vector2(0f, 0.5f), new Vector2(290f, 250f), new Vector2(150f, 160f));
            UnityEngine.UI.Text targetText = ExperimentUIFactory.Text(
                "TargetText", target.transform, "判定対象", 26, TextAnchor.UpperCenter, ExperimentUIFactory.MutedInk);
            ExperimentUIFactory.Place(targetText.rectTransform, new Vector2(0.5f, 1f), new Vector2(260f, 48f), new Vector2(0f, 54f));

            for (int i = 0; i < _vehicleViews.Length; i++)
            {
                _vehicleViews[i] = CreateVehicleView(panel, i);
            }

            _feedbackLabel = ExperimentUIFactory.Text("Feedback", panel, string.Empty, 36, TextAnchor.MiddleCenter);
            ExperimentUIFactory.Place(_feedbackLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(850f, 82f), new Vector2(0f, -280f));

            _controlRoot = ExperimentUIFactory.Rect("InputControls", panel);
            ExperimentUIFactory.Stretch(_controlRoot);
            EnsureInputControls();

            UnityEngine.UI.Image overlay = ExperimentUIFactory.Image("RunOverlay", panel, new Color32(255, 255, 255, 248));
            ExperimentUIFactory.Stretch(overlay.rectTransform, 46f, 110f);
            _runOverlay = overlay.gameObject;
            _overlayTitle = ExperimentUIFactory.Text("OverlayTitle", overlay.transform, string.Empty, 44);
            ExperimentUIFactory.Place(_overlayTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(880f, 120f), new Vector2(0f, -170f));
            _overlayBody = ExperimentUIFactory.Text("OverlayBody", overlay.transform, string.Empty, 29, TextAnchor.MiddleLeft, ExperimentUIFactory.MutedInk);
            ExperimentUIFactory.Place(_overlayBody.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(820f, 650f), new Vector2(0f, 80f));
            _overlayButton = ExperimentUIFactory.Button("OverlayButton", overlay.transform, "開始", 34, InvokeOverlayAction);
            _overlayButtonLabel = _overlayButton.GetComponentInChildren<UnityEngine.UI.Text>();
            ExperimentUIFactory.Place(_overlayButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(680f, 116f), new Vector2(0f, 230f));
        }

        private VehicleView CreateVehicleView(Transform parent, int index)
        {
            UnityEngine.UI.Image image = ExperimentUIFactory.Image("CarCard" + (index + 1), parent, Color.white);
            image.raycastTarget = true;
            ExperimentUIFactory.AddOutline(image, new Color32(80, 87, 94, 255), new Vector2(3f, -3f));
            ExperimentUIFactory.Place(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(270f, 190f), new Vector2(965f, 160f));

            UnityEngine.UI.Image vehicleIcon = ExperimentUIFactory.Image("VehicleIcon", image.transform, Color.white);
            vehicleIcon.raycastTarget = false;
            vehicleIcon.preserveAspect = true;
            ExperimentUIFactory.Place(vehicleIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(230f, 112f), new Vector2(0f, -3f));

            UnityEngine.UI.Text vehicle = ExperimentUIFactory.Text("Vehicle", image.transform, "スポーツ", 19);
            ExperimentUIFactory.Place(vehicle.rectTransform, new Vector2(0.5f, 0f), new Vector2(240f, 34f), new Vector2(0f, 20f));
            UnityEngine.UI.Text state = ExperimentUIFactory.Text("State", image.transform, "通常", 21, TextAnchor.MiddleRight, ExperimentUIFactory.MutedInk);
            ExperimentUIFactory.Place(state.rectTransform, new Vector2(1f, 1f), new Vector2(150f, 38f), new Vector2(-82f, -23f));

            UnityEngine.UI.Image targetBadge = ExperimentUIFactory.Image("TargetBadge", image.transform, ExperimentUIFactory.Accent);
            targetBadge.raycastTarget = false;
            ExperimentUIFactory.Place(targetBadge.rectTransform, new Vector2(0f, 1f), new Vector2(92f, 38f), new Vector2(50f, -23f));
            UnityEngine.UI.Text targetBadgeText = ExperimentUIFactory.Text("Label", targetBadge.transform, "対象", 20, TextAnchor.MiddleCenter, Color.white);
            ExperimentUIFactory.Stretch(targetBadgeText.rectTransform, 4f, 2f);

            UnityEngine.UI.Image cover = ExperimentUIFactory.Image("CoverOverlay", image.transform, new Color32(95, 101, 108, 245));
            ExperimentUIFactory.Stretch(cover.rectTransform, 5f, 5f);
            UnityEngine.UI.Text coverText = ExperimentUIFactory.Text("CoverText", cover.transform, "？？？\n公開まで待つ", 29, TextAnchor.MiddleCenter, Color.white);
            ExperimentUIFactory.Stretch(coverText.rectTransform, 8f, 8f);
            image.gameObject.SetActive(false);
            return new VehicleView
            {
                Rect = image.rectTransform,
                Image = image,
                VehicleIcon = vehicleIcon,
                VehicleLabel = vehicle,
                StateLabel = state,
                TargetBadge = targetBadge.gameObject,
                CoverOverlay = cover.gameObject,
            };
        }

        private void BuildSurveyPanel()
        {
            UnityEngine.UI.Image panel = ExperimentUIFactory.Image("SurveyPanel", _contentRoot, ExperimentUIFactory.Panel);
            ExperimentUIFactory.Stretch(panel.rectTransform, 30f, 32f);
            _surveyPanel = panel.gameObject;
            _surveyHeader = ExperimentUIFactory.Text("SurveyHeader", panel.transform, "UI終了後アンケート", 36);
            ExperimentUIFactory.Place(_surveyHeader.rectTransform, new Vector2(0.5f, 1f), new Vector2(960f, 76f), new Vector2(0f, -48f));
            UnityEngine.UI.Text scale = ExperimentUIFactory.Text(
                "Scale", panel.transform, "1：全くそう思わない　5：非常にそう思う", 21, TextAnchor.MiddleCenter, ExperimentUIFactory.MutedInk);
            ExperimentUIFactory.Place(scale.rectTransform, new Vector2(0.5f, 1f), new Vector2(920f, 42f), new Vector2(0f, -102f));

            string[] questions =
            {
                "1. 判定対象がすぐに分かった。",
                "2. 押す場所を探した。",
                "3. 整備の要否を車両から判断した。",
                "4. UIの表示・有効状態が答えを教えた。",
                "5. ミスした理由を説明できる。",
                "6. ボタンの意味が途中で変わったと感じた。",
                "7. 迷う場面が多かった。",
                "8. Brokenを見分けるゲーム性が簡単すぎた。",
            };
            for (int i = 0; i < questions.Length; i++)
            {
                _ratings[i] = CreateRatingQuestion(panel.transform, questions[i], 1720f - i * 198f, i);
            }

            UnityEngine.UI.Button submit = ExperimentUIFactory.Button("SubmitSurvey", panel.transform, "回答を保存して次へ", 29, SubmitSurvey);
            ExperimentUIFactory.Place(submit.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(680f, 94f), new Vector2(0f, 150f));
            _surveyValidation = ExperimentUIFactory.Text("SurveyValidation", panel.transform, string.Empty, 22, TextAnchor.MiddleCenter, ExperimentUIFactory.Error);
            ExperimentUIFactory.Place(_surveyValidation.rectTransform, new Vector2(0.5f, 0f), new Vector2(900f, 45f), new Vector2(0f, 66f));
        }

        private ExperimentRatingControl CreateRatingQuestion(Transform parent, string question, float y, int index)
        {
            UnityEngine.UI.Text label = ExperimentUIFactory.Text("Question" + (index + 1), parent, question, 20, TextAnchor.MiddleLeft);
            ExperimentUIFactory.Place(label.rectTransform, new Vector2(0f, 0f), new Vector2(940f, 40f), new Vector2(540f, y));
            var buttons = new UnityEngine.UI.Button[5];
            ExperimentRatingControl rating = null;
            for (int i = 0; i < buttons.Length; i++)
            {
                int value = i + 1;
                buttons[i] = ExperimentUIFactory.Button("Rating" + value, parent, value.ToString(CultureInfo.InvariantCulture), 23, () => rating.Select(value));
                ExperimentUIFactory.Place(buttons[i].GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(86f, 58f), new Vector2(160f + i * 110f, y - 58f));
            }
            _shortAnswers[index] = ExperimentUIFactory.InputField(
                "ShortAnswer" + (index + 1),
                parent,
                "短答（該当なしは「なし」）",
                18);
            ExperimentUIFactory.Place(
                _shortAnswers[index].GetComponent<RectTransform>(),
                new Vector2(0f, 0f),
                new Vector2(350f, 64f),
                new Vector2(835f, y - 58f));
            rating = new ExperimentRatingControl(buttons);
            return rating;
        }

        private void BuildSummaryPanel()
        {
            UnityEngine.UI.Image panel = ExperimentUIFactory.Image("SummaryPanel", _contentRoot, ExperimentUIFactory.Panel);
            ExperimentUIFactory.Stretch(panel.rectTransform, 46f, 65f);
            _summaryPanel = panel.gameObject;
            UnityEngine.UI.Text title = ExperimentUIFactory.Text("SummaryTitle", panel.transform, "セッション完了", 50);
            ExperimentUIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(880f, 100f), new Vector2(0f, -110f));
            _summaryText = ExperimentUIFactory.Text("Summary", panel.transform, string.Empty, 28, TextAnchor.UpperLeft);
            ExperimentUIFactory.Place(_summaryText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900f, 780f), new Vector2(0f, 120f));
            _summaryPath = ExperimentUIFactory.Text("ExportPath", panel.transform, string.Empty, 22, TextAnchor.MiddleLeft, ExperimentUIFactory.MutedInk);
            ExperimentUIFactory.Place(_summaryPath.rectTransform, new Vector2(0.5f, 0f), new Vector2(900f, 190f), new Vector2(0f, 170f));
        }

        private void ShowSetup()
        {
            _phase = RunPhase.Setup;
            _sessionConfigurationLocked = false;
            SetOnlyPanel(_setupPanel);
            RefreshSetupSelectors();
        }

        private void CycleExperimentType()
        {
            if (_sessionConfigurationLocked) return;
            _selectedExperimentType = (ExperimentType)(((int)_selectedExperimentType + 1) % 3);
            RefreshSetupSelectors();
        }

        private void CycleAssistAnchor()
        {
            if (_sessionConfigurationLocked || _selectedExperimentType != ExperimentType.StateAssist) return;
            _selectedAssistAnchor = _selectedAssistAnchor == RepairAnchorMode.UpperFixed
                ? RepairAnchorMode.BottomFixed
                : RepairAnchorMode.UpperFixed;
            RefreshSetupSelectors();
        }

        private void RefreshSetupSelectors()
        {
            if (_experimentTypeValue != null)
            {
                _experimentTypeValue.text = "実験種別：" + ExperimentTypeLabel(_selectedExperimentType) + "（タップで変更）";
            }
            if (_assistAnchorValue != null)
            {
                if (_selectedExperimentType == ExperimentType.StateAssist)
                {
                    _assistAnchorValue.text = "状態アシストの整備位置：" + _selectedAssistAnchor + "（Placement集計後の暫定値）";
                }
                else if (_selectedExperimentType == ExperimentType.RushPackage)
                {
                    bool ready = _humanRushRegistration != null
                        && _humanRushRegistration.TryValidate(out _);
                    _assistAnchorValue.text = ready
                        ? "人間Rush：事前登録2候補あり（P00は技術確認3preset）"
                        : "人間Rush：事前登録2候補が未設定（P00だけ3presetで実行可）";
                }
                else
                {
                    _assistAnchorValue.text = "整備位置の追加設定：この実験では不要";
                }
            }
            if (_assistAnchorButton != null)
            {
                _assistAnchorButton.interactable = !_sessionConfigurationLocked
                    && _selectedExperimentType == ExperimentType.StateAssist;
            }
            if (_experimentTypeButton != null) _experimentTypeButton.interactable = !_sessionConfigurationLocked;
        }

        private void TryStartSession()
        {
            string participant = (_participantField.text ?? string.Empty).Trim().ToUpperInvariant();
            if (participant.Length < 2 || participant.Contains("@") || participant.Contains(" "))
            {
                _setupValidation.text = "個人情報ではなく P00 / P01 のような匿名IDを使用してください。";
                return;
            }

            int participantOrdinal;
            if (!InputUITrialPlan.IsPilotParticipant(participant)
                && !InputUITrialPlan.TryGetParticipantOrdinal(participant, out participantOrdinal))
            {
                _setupValidation.text = "順序を均等化するため、末尾が連番の匿名ID（P01など）を使用してください。";
                return;
            }

            _participantId = participant;
            try
            {
                _schedule = InputUITrialPlan.BuildSchedule(
                    participant,
                    _selectedExperimentType,
                    InputUIExperimentSchema.CurrentExperimentVersion,
                    _selectedAssistAnchor,
                    _humanRushRegistration);
            }
            catch (InvalidOperationException exception)
            {
                _setupValidation.text = "開始できません：" + exception.Message;
                return;
            }
            int identitySeed = _schedule.Assignments.Count == 0 ? 1 : _schedule.Assignments[0].Seed;
            _sessionId = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture)
                + "-" + identitySeed.ToString("X8", CultureInfo.InvariantCulture);
            _csvWriter = new InputUIExperimentCsvWriter(
                _participantId,
                _sessionId,
                InputUIExperimentSchema.CurrentExperimentVersion,
                _selectedExperimentType,
                _schedule.ReferenceConditionId);
            _sessionPersistence = new SessionPersistence(
                _participantId,
                _sessionId,
                InputUIExperimentSchema.CurrentExperimentVersion);
            ExperimentConditionAssignment firstAssignment = _schedule.Assignments[0];
            _manifest = SessionPersistence.CreateManifestSnapshot(
                _participantId,
                _sessionId,
                _selectedExperimentType,
                _schedule.Assignments,
                _selectedExperimentType == ExperimentType.StateAssist
                    ? _selectedAssistAnchor
                    : firstAssignment.RepairAnchorMode,
                firstAssignment.RepairAssistMode,
                firstAssignment.VehicleButtonLayoutMode,
                _schedule.IsPilot,
                "unknown",
                InputUIExperimentSchema.CurrentExperimentVersion,
                _schedule.ProtocolId,
                _schedule.ReferenceConditionId,
                _schedule.ConditionDefinitions);
            _sessionPersistence.BeginSession(_manifest);
            for (int i = 0; i < _eventRecords.Count; i++)
            {
                if (string.IsNullOrEmpty(_eventRecords[i].ParticipantId)) _eventRecords[i].ParticipantId = _participantId;
                if (string.IsNullOrEmpty(_eventRecords[i].SessionId)) _eventRecords[i].SessionId = _sessionId;
            }
            _blockIndex = 0;
            _sessionConfigurationLocked = true;
            _experimentTypeButton.interactable = false;
            _assistAnchorButton.interactable = false;
            _setupValidation.text = string.Empty;
            BeginBlock();
        }

        private void BeginBlock()
        {
            _phase = RunPhase.BlockIntro;
            SetOnlyPanel(_runPanel);
            _assignment = _schedule.Assignments[_blockIndex];
            _currentLayout = LayoutForAssignment(_assignment);
            ApplyInputLayout(_currentLayout);
            _blockLabel.text = "UI " + (_blockIndex + 1) + " / " + _schedule.Assignments.Count;
            _score = 0;
            _misses = 0;
            _previousTrialHadError = false;
            UpdateScoreHud();
            HideAllVehicleViews();
            ShowRunOverlay(
                "UI " + (_blockIndex + 1) + " / " + _schedule.Assignments.Count + "：練習",
                "・通常／急送：車種に合うボタン\n・覆面：車両が公開されたら車種ボタン\n・故障：整備だけで完了\n\n誤入力後も期限内なら入力し直せます。Pointer Downを記録し、同一フレームは最後の物理入力だけを判定します。",
                "練習4試行を開始",
                StartPractice);
            PersistCheckpoint();
        }

        private void StartPractice()
        {
            _phase = RunPhase.Practice;
            _activeTrials = InputUITrialPlan.BuildPracticeSet(_blockIndex, _assignment.Seed);
            _trialIndex = 0;
            _runOverlay.SetActive(false);
            StartCurrentTrial();
        }

        private void ShowMeasuredIntro()
        {
            _phase = RunPhase.MeasuredIntro;
            HideAllVehicleViews();
            ShowRunOverlay(
                "練習終了",
                "ここから24試行を計測します。ルールと問題構成は条件間で同一です。\n成績は全条件終了まで表示しません。",
                "計測を開始",
                StartMeasured);
            PersistCheckpoint();
        }

        private void StartMeasured()
        {
            _phase = RunPhase.Measured;
            _activeTrials = InputUITrialPlan.BuildMeasuredSet(_assignment.TrialSetId, _assignment.Seed);
            _trialIndex = 0;
            _runOverlay.SetActive(false);
            StartCurrentTrial();
        }

        private void StartCurrentTrial()
        {
            _currentTrial = _activeTrials[_trialIndex];
            _trialResolving = false;
            _coveredRevealed = _currentTrial.SpecialState != ExperimentSpecialState.Covered;
            _trialStartedAt = Time.unscaledTime;
            _actionableStartedAt = _coveredRevealed ? _trialStartedAt : float.PositiveInfinity;
            // Reaction-time fields use only realtimeSinceStartupAsDouble. DateTime is
            // retained for audit timestamps but never participates in RT arithmetic.
            _trialStartedAtMonotonicMs = NowMilliseconds();
            _actionableStartedAtMonotonicMs = _coveredRevealed
                ? _trialStartedAtMonotonicMs
                : double.PositiveInfinity;
            _feedbackLabel.text = string.Empty;
            _currentTargetVehicleId = CreateVehicleId(_blockIndex, _currentTrial);
            ConfigureVehicleViews();
            UpdateRepairPresentation();

            float duration = InputUIComparisonRules.TravelSeconds(_currentTrial.Speed, _currentTrial.SpecialState);
            _currentRecord = new ExperimentTrialRecord
            {
                SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion,
                ExperimentVersion = InputUIExperimentSchema.CurrentExperimentVersion,
                ParticipantId = _participantId,
                SessionId = _sessionId,
                ExperimentType = _assignment.ExperimentType,
                Condition = _assignment.Condition,
                ConditionOrder = _assignment.ConditionOrder,
                TrialSetId = _currentTrial.TrialSetId,
                IsPilot = _schedule != null && _schedule.IsPilot,
                Variant = LegacyVariantFor(_assignment.Condition),
                VariantOrder = _assignment.ConditionOrder,
                ProblemSet = _currentTrial.ProblemSet,
                Seed = _assignment.Seed,
                IsPractice = _currentTrial.IsPractice,
                TrialNumber = _currentTrial.TrialNumber,
                Vehicle = _currentTrial.Vehicle,
                SpecialState = _currentTrial.SpecialState,
                Speed = _currentTrial.Speed,
                CoveredRevealProgress = _currentTrial.CoveredRevealProgress,
                GapAfterSeconds = _currentTrial.GapAfterSeconds,
                EffectiveTravelSeconds = duration,
                PaceContext = _currentTrial.PaceContext,
                SequenceContext = _currentTrial.SequenceContext,
                ExpectedInput = _currentTrial.ExpectedInput,
                ActualInput = ExperimentInput.None,
                FinalInput = ExperimentInput.None,
                IsCorrect = false,
                EventualCorrect = false,
                IsTimeout = false,
                InformationAvailableAtMs = _coveredRevealed ? 0d : -1d,
                TimeToFirstRawTouchMs = -1d,
                TimeToFirstAcceptedInputMs = -1d,
                TimeToCorrectResolutionMs = -1d,
                CorrectionCostMs = -1d,
                ResponseTimeMs = -1d,
                ActionableResponseTimeMs = -1d,
                FinalResponseTimeMs = -1d,
                SpecialStateMissed = _currentTrial.SpecialState == ExperimentSpecialState.Broken
                    || _currentTrial.SpecialState == ExperimentSpecialState.Covered
                    ? false
                    : (bool?)null,
                SpecialMissApplicable = _currentTrial.SpecialState == ExperimentSpecialState.Broken
                    || _currentTrial.SpecialState == ExperimentSpecialState.Covered,
                DistanceToCorrectButtonPx = -1f,
                DistanceToCorrectButtonNormalized = -1f,
                Timestamp = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            };
            string prefix = _currentTrial.IsPractice ? "練習" : "計測";
            _progressLabel.text = prefix + " " + (_trialIndex + 1) + " / " + _activeTrials.Count;
            PersistCheckpoint();
        }

        private void ConfigureVehicleViews()
        {
            HideAllVehicleViews();
            int count = IsRush ? Mathf.Min(3, _activeTrials.Count - _trialIndex) : 1;
            for (int i = 0; i < count; i++)
            {
                ExperimentTrial trial = _activeTrials[_trialIndex + i];
                VehicleView view = _vehicleViews[i];
                view.Trial = trial;
                view.VehicleId = CreateVehicleId(_blockIndex, trial);
                view.StartX = IsRush ? 250f + i * 300f : 965f;
                view.Image.color = VehicleCardColor(trial.SpecialState);
                view.VehicleIcon.sprite = VehicleSpriteFor(trial.Vehicle);
                view.VehicleIcon.enabled = view.VehicleIcon.sprite != null;
                view.VehicleLabel.text = InputUIComparisonNaming.VehicleLabel(trial.Vehicle);
                view.StateLabel.text = InputUIComparisonNaming.StateLabel(trial.SpecialState);
                view.StateLabel.color = trial.SpecialState == ExperimentSpecialState.Broken
                    ? ExperimentUIFactory.Error
                    : ExperimentUIFactory.MutedInk;
                view.TargetBadge.SetActive(i == 0);
                view.CoverOverlay.SetActive(trial.SpecialState == ExperimentSpecialState.Covered && (i != 0 || !_coveredRevealed));
                view.Rect.anchoredPosition = new Vector2(view.StartX, 160f);
                view.Rect.gameObject.SetActive(true);
            }
        }

        private void UpdateVehicleViews(float elapsed, float currentProgress)
        {
            if (!IsRush)
            {
                _vehicleViews[0].Rect.anchoredPosition = new Vector2(Mathf.Lerp(965f, 135f, currentProgress), 160f);
                return;
            }

            for (int i = 0; i < _vehicleViews.Length; i++)
            {
                VehicleView view = _vehicleViews[i];
                if (!view.Rect.gameObject.activeSelf || view.Trial == null) continue;
                float duration = InputUIComparisonRules.TravelSeconds(view.Trial.Speed, view.Trial.SpecialState);
                float progress = Mathf.Clamp01(elapsed / duration);
                float travel = 360f * progress;
                view.Rect.anchoredPosition = new Vector2(view.StartX - travel, 160f);
            }
        }

        private void RevealCoveredCar()
        {
            if (_coveredRevealed) return;
            _coveredRevealed = true;
            _actionableStartedAt = Time.unscaledTime;
            _actionableStartedAtMonotonicMs = NowMilliseconds();
            if (_currentRecord != null && _currentRecord.InformationAvailableAtMs < 0d)
            {
                _currentRecord.InformationAvailableAtMs = Math.Max(
                    0d,
                    _actionableStartedAtMonotonicMs - _trialStartedAtMonotonicMs);
            }
            _vehicleViews[0].CoverOverlay.SetActive(false);
            _vehicleViews[0].Image.color = Color.white;
            _vehicleViews[0].StateLabel.text = "公開済み";
            PersistCheckpoint();
        }

        private Sprite VehicleSpriteFor(ExperimentVehicle vehicle)
        {
            switch (vehicle)
            {
                case ExperimentVehicle.Sports: return _sportsVehicleSprite;
                case ExperimentVehicle.Compact: return _compactVehicleSprite;
                case ExperimentVehicle.LightTruck: return _lightTruckVehicleSprite;
                default: return null;
            }
        }

        private static Color VehicleCardColor(ExperimentSpecialState state)
        {
            switch (state)
            {
                case ExperimentSpecialState.Broken: return new Color32(255, 224, 220, 255);
                case ExperimentSpecialState.Covered: return new Color32(225, 229, 233, 255);
                case ExperimentSpecialState.Express: return new Color32(224, 242, 255, 255);
                default: return Color.white;
            }
        }

        private void OnPhysicalPointerDown(PhysicalPointerDown pointer)
        {
            // Capture-time RT uses the same monotonic Unity clock as the trial and
            // reveal origins. pointer.EventTimeMs remains the native event clock for
            // arbitration/cooldown diagnostics; wall-clock Timestamp is audit-only.
            bool trialAtTouch = IsTrialRunning
                && _currentTrial != null
                && _currentRecord != null
                && !_trialResolving;
            double capturedAtMonotonicMs = NowMilliseconds();
            double stimulusTimeMs = !trialAtTouch
                ? -1d
                : Math.Max(0d, capturedAtMonotonicMs - _trialStartedAtMonotonicMs);
            double actionableTimeMs = !trialAtTouch
                || !_coveredRevealed
                || double.IsPositiveInfinity(_actionableStartedAtMonotonicMs)
                    ? -1d
                    : Math.Max(0d, capturedAtMonotonicMs - _actionableStartedAtMonotonicMs);
            RefreshRepairRectHistory(pointer.EventTimeMs);
            ExperimentPointerInput control = FindControlAt(pointer.ScreenPosition);
            string topUiElement = ResolveTopUiElement(pointer.ScreenPosition, pointer.PointerId);
            Rect safe = Screen.safeArea;
            bool insideCurrentRepair = _hasCurrentRepairRect && _currentRepairRect.Contains(pointer.ScreenPosition);
            bool insidePreviousRepair = _hasPreviousRepairRect && _previousRepairRect.Contains(pointer.ScreenPosition);
            double previousAge = _hasPreviousRepairRect
                ? Math.Max(0d, pointer.EventTimeMs - _repairRectChangedAtMs)
                : -1d;
            var row = new ExperimentInputEventRecord
            {
                SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion,
                ExperimentVersion = InputUIExperimentSchema.CurrentExperimentVersion,
                ParticipantId = _participantId ?? string.Empty,
                SessionId = _sessionId ?? string.Empty,
                ExperimentType = _assignment?.ExperimentType ?? _selectedExperimentType,
                Condition = _assignment?.Condition ?? string.Empty,
                ConditionOrder = _assignment?.ConditionOrder ?? 0,
                TrialSetId = trialAtTouch ? _currentTrial.TrialSetId : string.Empty,
                IsPilot = _schedule != null && _schedule.IsPilot,
                Variant = _assignment == null ? ExperimentVariant.Baseline : LegacyVariantFor(_assignment.Condition),
                VariantOrder = _assignment?.ConditionOrder ?? 0,
                ProblemSet = trialAtTouch ? _currentTrial.ProblemSet : -1,
                IsPractice = trialAtTouch && _currentTrial.IsPractice,
                TrialNumber = trialAtTouch ? _currentTrial.TrialNumber : 0,
                EventIndex = ++_eventIndex,
                Frame = pointer.Frame,
                Input = control?.Input ?? ExperimentInput.None,
                PointerId = pointer.PointerId,
                EventTimeMs = pointer.EventTimeMs,
                ScreenXPx = pointer.ScreenPosition.x,
                ScreenYPx = pointer.ScreenPosition.y,
                ScreenXNormalized = pointer.ScreenPosition.x / Mathf.Max(1f, Screen.width),
                ScreenYNormalized = pointer.ScreenPosition.y / Mathf.Max(1f, Screen.height),
                ScreenWidth = Screen.width,
                ScreenHeight = Screen.height,
                SafeAreaX = safe.x,
                SafeAreaY = safe.y,
                SafeAreaWidth = safe.width,
                SafeAreaHeight = safe.height,
                UiScaleFactor = _canvas == null ? 1f : _canvas.scaleFactor,
                HitUiElement = string.IsNullOrEmpty(topUiElement) ? control?.gameObject.name ?? string.Empty : topUiElement,
                HitButtonId = control?.ButtonId ?? string.Empty,
                WasButtonVisible = control != null && control.IsVisible,
                WasButtonInteractable = control != null && control.IsInteractable,
                WasInputAccepted = false,
                AcceptedInput = ExperimentInput.None,
                IgnoredReason = ExperimentIgnoredReasons.None,
                ExpectedInput = trialAtTouch ? _currentTrial.ExpectedInput : ExperimentInput.None,
                TargetVehicleIdAtTouch = trialAtTouch ? _currentTargetVehicleId ?? string.Empty : string.Empty,
                TargetVehicleIdAtAccept = string.Empty,
                ResolvedVehicleId = string.Empty,
                TargetChangedBetweenTouchAndAccept = false,
                IsInsideCurrentRepairRect = insideCurrentRepair,
                IsInsidePreviousRepairRect = insidePreviousRepair,
                PreviousRepairRectAgeMs = previousAge,
                PointerPosition = NormalizedPosition(pointer.ScreenPosition),
                ButtonCenterPosition = control == null ? string.Empty : NormalizedPosition(control.ScreenCenter()),
                VisuallyEnabled = control != null && control.VisuallyEnabled,
                Disposition = "raw",
                StimulusTimeMs = stimulusTimeMs,
                ActionableTimeMs = actionableTimeMs,
                DistanceToExpectedButtonPx = !trialAtTouch || control == null
                    ? -1f
                    : DistanceToExpectedButton(control.ScreenCenter()),
                Timestamp = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            };
            if (_sessionPersistence != null)
            {
                try
                {
                    // Write-ahead before the in-memory queue: a force-stop between
                    // physical capture and LateUpdate cannot erase this Raw event.
                    _sessionPersistence.AppendRawEventJournal(row);
                }
                catch (Exception exception)
                {
                    Debug.LogError("[InputUIComparison] Raw event WAL append failed: " + exception);
                }
            }
            _eventRecords.Add(row);
            _pendingFrameEvents.Add(row);
            if (trialAtTouch)
            {
                if (_currentRecord.TimeToFirstRawTouchMs < 0d && row.ActionableTimeMs >= 0d)
                {
                    _currentRecord.TimeToFirstRawTouchMs = row.ActionableTimeMs;
                }
                TrackSameSpot(pointer.ScreenPosition);
            }
        }

        private void FlushPendingInputFrame()
        {
            if (_pendingFrameEvents.Count == 0) return;

            // InputSystem may deliver a before-render sample after this component's
            // LateUpdate. Preserve the captured frame boundary so a callback carried
            // into the next flush can never overwrite an input from another frame.
            while (_pendingFrameEvents.Count > 0)
            {
                int capturedFrame = _pendingFrameEvents[0].Frame;
                int frameCount = 1;
                while (frameCount < _pendingFrameEvents.Count
                    && _pendingFrameEvents[frameCount].Frame == capturedFrame)
                {
                    frameCount++;
                }

                ExperimentInputEventRecord[] rows = _pendingFrameEvents
                    .Take(frameCount)
                    .ToArray();
                _pendingFrameEvents.RemoveRange(0, frameCount);
                ArbitrateCapturedFrame(rows);
            }

            PersistCheckpoint();
        }

        private void ArbitrateCapturedFrame(IReadOnlyList<ExperimentInputEventRecord> rows)
        {
            if (rows == null || rows.Count == 0) return;
            var candidates = rows.Select(InputArbitrationCandidate.FromRecord).ToArray();
            var context = new InputArbitrationContext
            {
                SessionRunning = IsTrialRunning,
                TrialActionable = _currentRecord != null && !_trialResolving,
                CoveredNotRevealed = _currentTrial != null
                    && _currentTrial.SpecialState == ExperimentSpecialState.Covered
                    && !_coveredRevealed,
                // Production rules accept an early Covered vehicle input as a wrong answer.
                AcceptCoveredPrematureInput = true,
                TargetVehicleIdAtAccept = _currentTargetVehicleId ?? string.Empty,
                LastAcceptedEventTimeMs = _lastAcceptedEventTimeMs,
                CooldownDurationMs = InputUIComparisonRules.GlobalInputCooldownSeconds * 1000d,
            };
            InputArbitrationFrameResult frame = InputArbitrator.ArbitrateFrame(candidates, context);
            ExperimentInputEventRecord accepted = null;
            for (int i = 0; i < frame.Results.Count; i++)
            {
                ExperimentInputEventRecord row = rows[i];
                InputArbitrator.ApplyToRecord(row, frame.Results[i]);
                if (row.IgnoredReason == ExperimentIgnoredReasons.HiddenOrEmpty
                    && context.CoveredNotRevealed
                    && row.HitUiElement.IndexOf("Cover", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    row.IgnoredReason = ExperimentIgnoredReasons.CoveredNotRevealed;
                }
                row.Disposition = row.WasInputAccepted ? "accepted" : row.IgnoredReason;
                if (_currentRecord != null)
                {
                    if (row.IgnoredReason == ExperimentIgnoredReasons.Disabled) _currentRecord.InvalidButtonInputCount++;
                    else if (row.IgnoredReason == ExperimentIgnoredReasons.Cooldown) _currentRecord.CooldownSuppressedCount++;
                    else if (row.IgnoredReason == ExperimentIgnoredReasons.SameFrameOverwritten) _currentRecord.SameFrameCoalescedCount++;
                }
                if (row.WasInputAccepted) accepted = row;
            }
            _lastAcceptedEventTimeMs = frame.LastAcceptedEventTimeMs;
            if (accepted != null) ProcessAcceptedInput(accepted);
        }

        private void ProcessAcceptedInput(ExperimentInputEventRecord inputEvent)
        {
            if (_currentRecord == null || _currentTrial == null || _trialResolving) return;
            // The reveal boundary is sampled with the physical down. A Covered down
            // captured before reveal remains premature even if Update reveals the car
            // before LateUpdate performs arbitration.
            bool coveredRevealedAtTouch = _currentTrial.SpecialState != ExperimentSpecialState.Covered
                || inputEvent.ActionableTimeMs >= 0d;
            bool actionableAtTouch = InputUIComparisonRules.IsActionable(
                _currentTrial.SpecialState,
                coveredRevealedAtTouch);
            InputEvaluation evaluation = InputUIComparisonRules.Evaluate(
                _currentTrial.Vehicle,
                _currentTrial.SpecialState,
                coveredRevealedAtTouch,
                inputEvent.AcceptedInput);
            _currentRecord.InputCount++;
            _currentRecord.RetryCount = Mathf.Max(0, _currentRecord.InputCount - 1);
            _currentRecord.FinalInput = inputEvent.AcceptedInput;
            double capturedAtMonotonicMs = NowMilliseconds();
            double stimulusMs = inputEvent.StimulusTimeMs >= 0d
                ? inputEvent.StimulusTimeMs
                : Math.Max(0d, capturedAtMonotonicMs - _trialStartedAtMonotonicMs);
            double actionableMs = actionableAtTouch
                ? inputEvent.ActionableTimeMs >= 0d
                    ? inputEvent.ActionableTimeMs
                    : Math.Max(0d, capturedAtMonotonicMs - _actionableStartedAtMonotonicMs)
                : -1d;
            if (_currentRecord.InputCount == 1)
            {
                _currentRecord.ActualInput = inputEvent.AcceptedInput;
                _currentRecord.IsCorrect = evaluation.IsCorrect;
                _currentRecord.ResponseTimeMs = stimulusMs;
                ExperimentPointerInput pressed;
                if (_controls.TryGetValue(inputEvent.AcceptedInput, out pressed))
                {
                    _currentRecord.ButtonPosition = NormalizedPosition(pressed.ScreenCenter());
                    _currentRecord.DistanceToCorrectButtonPx = DistanceToExpectedButton(pressed.ScreenCenter());
                    _currentRecord.DistanceToCorrectButtonNormalized = _currentRecord.DistanceToCorrectButtonPx < 0f
                        ? -1f
                        : _currentRecord.DistanceToCorrectButtonPx
                            / Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
                }
                if (!evaluation.IsCorrect)
                {
                    _currentRecord.WrongButtonType = inputEvent.AcceptedInput.ToString();
                    _currentRecord.PostErrorConsecutiveMiss = _previousTrialHadError;
                }
            }
            if (_currentRecord.ActionableResponseTimeMs < 0d && actionableMs >= 0d)
            {
                _currentRecord.ActionableResponseTimeMs = actionableMs;
            }
            if (_currentRecord.TimeToFirstAcceptedInputMs < 0d && actionableMs >= 0d)
            {
                _currentRecord.TimeToFirstAcceptedInputMs = actionableMs;
            }
            if (evaluation.IsSpecialMiss) _currentRecord.SpecialStateMissed = true;

            if (evaluation.IsCorrect)
            {
                _currentRecord.EventualCorrect = true;
                _currentRecord.FinalResponseTimeMs = stimulusMs;
                _currentRecord.TimeToCorrectResolutionMs = actionableMs;
                _currentRecord.CorrectionCostMs = actionableMs >= 0d
                    && _currentRecord.TimeToFirstRawTouchMs >= 0d
                        ? Math.Max(0d, actionableMs - _currentRecord.TimeToFirstRawTouchMs)
                        : -1d;
                _score += _currentTrial.SpecialState == ExperimentSpecialState.Express ? 20 : 10;
                ShowFeedback("正解", ExperimentUIFactory.Correct);
                ResolveTrial();
            }
            else
            {
                _score -= 5;
                _misses++;
                ShowFeedback(evaluation.Reason, ExperimentUIFactory.Error);
                UpdateScoreHud();
            }
        }

        private void ResolveTimeout()
        {
            if (_trialResolving || _currentRecord == null) return;
            _currentRecord.IsTimeout = true;
            _currentRecord.EventualCorrect = false;
            _currentRecord.FinalResponseTimeMs = -1d;
            _currentRecord.TimeToCorrectResolutionMs = -1d;
            _currentRecord.CorrectionCostMs = -1d;
            if (_currentRecord.SpecialMissApplicable) _currentRecord.SpecialStateMissed = true;
            _score -= 5;
            _misses++;
            ShowFeedback("見逃し", ExperimentUIFactory.Error);
            ResolveTrial();
        }

        private void ResolveTrial()
        {
            if (_trialResolving || _currentRecord == null) return;
            _trialResolving = true;
            _currentRecord.RetryCount = Mathf.Max(0, _currentRecord.InputCount - 1);
            _currentRecord.Timestamp = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            _trialRecords.Add(_currentRecord);
            _previousTrialHadError = !_currentRecord.IsCorrect || _currentRecord.IsTimeout;
            _currentRecord = null;
            UpdateScoreHud();

            float gap = _currentTrial.GapAfterSeconds > 0f
                ? _currentTrial.GapAfterSeconds
                : InputUIComparisonRules.NominalGapSeconds(_currentTrial.Speed, false);
            if (IsRush) gap = Mathf.Min(gap, 0.22f);
            _nextTrialAt = Time.unscaledTime + gap;
            _trialIndex++;
            // Commit the resolved record, next trial index, and gap deadline as one
            // state transition. Restore can no longer replay the resolved trial.
            PersistCheckpoint();
            Invoke(nameof(AdvanceAfterGap), gap);
        }

        private void AdvanceAfterGap()
        {
            if (!_trialResolving) return;
            float remaining = _nextTrialAt - Time.unscaledTime;
            if (remaining > 0.005f)
            {
                // Invoke may wake slightly early on some devices. Re-arm instead of
                // leaving the session permanently stuck in a resolving gap.
                Invoke(nameof(AdvanceAfterGap), remaining);
                return;
            }
            if (_trialIndex < _activeTrials.Count)
            {
                StartCurrentTrial();
                return;
            }
            HideAllVehicleViews();
            _currentTrial = null;
            _currentRecord = null;
            _currentTargetVehicleId = string.Empty;
            _trialResolving = false;
            if (_phase == RunPhase.Practice) ShowMeasuredIntro();
            else ShowSurvey();
        }

        private void ShowSurvey()
        {
            _phase = RunPhase.Survey;
            SetOnlyPanel(_surveyPanel);
            _surveyHeader.text = "UI " + (_blockIndex + 1) + " / " + _schedule.Assignments.Count + "：終了後アンケート";
            _surveyValidation.text = string.Empty;
            for (int i = 0; i < _ratings.Length; i++)
            {
                _ratings[i].Select(0);
                _shortAnswers[i].text = string.Empty;
            }
            PersistCheckpoint();
        }

        private void SubmitSurvey()
        {
            if (_ratings.Any(rating => rating.Value < 1 || rating.Value > 5))
            {
                _surveyValidation.text = "8項目すべてに1〜5で回答してください。";
                return;
            }
            if (_shortAnswers.Any(answer => string.IsNullOrWhiteSpace(answer.text)))
            {
                _surveyValidation.text = "8項目すべてに短答を入力してください（該当なしは「なし」）。";
                return;
            }

            if (HasSubmittedSurveyForCurrentAssignment())
            {
                _blockIndex++;
                if (_blockIndex < _schedule.Assignments.Count) BeginBlock();
                else ShowComplete();
                return;
            }

            _surveyRecords.Add(new ExperimentSurveyRecord
            {
                SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion,
                ExperimentVersion = InputUIExperimentSchema.CurrentExperimentVersion,
                ParticipantId = _participantId,
                SessionId = _sessionId,
                ExperimentType = _assignment.ExperimentType,
                Condition = _assignment.Condition,
                ConditionOrder = _assignment.ConditionOrder,
                TrialSetId = _assignment.TrialSetId,
                IsPilot = _schedule.IsPilot,
                Variant = LegacyVariantFor(_assignment.Condition),
                VariantOrder = _assignment.ConditionOrder,
                ProblemSet = TrialSetIndex(_assignment.TrialSetId),
                TargetClarity = _ratings[0].Value,
                SearchedForButton = _ratings[1].Value,
                JudgedRepairNeedFromVehicle = _ratings[2].Value,
                UiStateRevealedAnswer = _ratings[3].Value,
                CanExplainMistake = _ratings[4].Value,
                MeaningChanged = _ratings[5].Value,
                MostConfusingMomentRating = _ratings[6].Value,
                BrokenTooEasy = _ratings[7].Value,
                TargetClarityExplanation = _shortAnswers[0].text.Trim(),
                ButtonSearchExplanation = _shortAnswers[1].text.Trim(),
                RepairJudgmentExplanation = _shortAnswers[2].text.Trim(),
                UiStateExplanation = _shortAnswers[3].text.Trim(),
                MistakeExplanation = _shortAnswers[4].text.Trim(),
                MeaningChangedExplanation = _shortAnswers[5].text.Trim(),
                MostConfusingMoment = _shortAnswers[6].text.Trim(),
                BrokenDifficultyExplanation = _shortAnswers[7].text.Trim(),
                AnalystExplanationCode = string.Empty,
                Timestamp = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            });
            // Commit the response while ConditionIndex still names the survey's
            // assignment. Restore detects this durable row and advances without
            // asking for or writing the same survey twice.
            PersistCheckpoint();
            _blockIndex++;
            if (_blockIndex < _schedule.Assignments.Count) BeginBlock();
            else ShowComplete();
        }

        private void ShowComplete()
        {
            _phase = RunPhase.Complete;
            SetOnlyPanel(_summaryPanel);
            string exportPath = _csvWriter.Export(_trialRecords, _eventRecords, _surveyRecords);
            if (_sessionPersistence != null && _manifest != null)
            {
                _sessionPersistence.MarkCompleted(_manifest, CreateCheckpoint(SessionPersistence.StatusCompleted));
            }
            _summaryText.text = _schedule.IsPilot
                ? "P00技術予行データを保存しました。P00では3実験すべてを確認できますが、人間一般のUX集計からは自動除外されます。\n\nP00の成立はUI案の優位性を示しません。"
                : "この実験データを保存しました。人間本試験は P01〜P06 Placementのみ → 参加者集計 → anchor決定 → State Assist別session（最低別日）→ 参加者集計 → 2候補固定 → Rush最終反証の順です。\n\n同日に3実験をまとめず、State Assistは別cohortを推奨します。";
            _summaryPath.text = "出力先\n" + exportPath;
        }

        private void EnsureInputControls()
        {
            if (_controls.Count > 0) return;
            CreateInputControl(ExperimentInput.Sports, "スポーツ");
            CreateInputControl(ExperimentInput.Compact, "コンパクト");
            CreateInputControl(ExperimentInput.LightTruck, "トラック");
            CreateInputControl(ExperimentInput.Repair, "整備");
        }

        private void CreateInputControl(ExperimentInput input, string label)
        {
            UnityEngine.UI.Image image = ExperimentUIFactory.Image(input + "Button", _controlRoot, ExperimentUIFactory.Control);
            ExperimentUIFactory.AddOutline(image, new Color32(110, 116, 124, 255), new Vector2(2f, -2f));
            UnityEngine.UI.Text text = ExperimentUIFactory.Text("Label", image.transform, label, 27);
            ExperimentUIFactory.Stretch(text.rectTransform, 8f, 6f);
            ExperimentPointerInput pointer = image.gameObject.AddComponent<ExperimentPointerInput>();
            pointer.Configure(this, input, label);
            _controls[input] = pointer;
        }

        private void ApplyInputLayout(ExperimentLayoutSpec layout)
        {
            EnsureInputControls();
            for (int i = 0; i < layout.Controls.Count; i++)
            {
                ExperimentControlSpec spec = layout.Controls[i];
                ExperimentPointerInput pointer = _controls[spec.Input];
                spec.RectTransform.ApplyTo(pointer.GetComponent<RectTransform>());
                UnityEngine.UI.Image image = pointer.GetComponent<UnityEngine.UI.Image>();
                UnityEngine.UI.Text label = pointer.GetComponentInChildren<UnityEngine.UI.Text>(true);
                spec.Style.ApplyColors(image, label, true);
                pointer.SetPresentation(true, true);
            }
            Canvas.ForceUpdateCanvases();
            RefreshRepairRectHistory(NowMilliseconds(), true);
        }

        private void UpdateRepairPresentation()
        {
            ExperimentPointerInput repair = _controls[ExperimentInput.Repair];
            ExperimentRepairPresentation presentation = _currentLayout.GetRepairPresentation(_currentTrial.SpecialState);
            repair.SetPresentation(presentation.Visible, presentation.Interactable);
        }

        private ExperimentPointerInput FindControlAt(Vector2 screenPosition)
        {
            // Repair is evaluated first solely for deterministic overlap diagnostics;
            // valid catalog layouts do not overlap any controls.
            ExperimentInput[] order =
            {
                ExperimentInput.Repair,
                ExperimentInput.LightTruck,
                ExperimentInput.Compact,
                ExperimentInput.Sports,
            };
            for (int i = 0; i < order.Length; i++)
            {
                ExperimentPointerInput control;
                if (_controls.TryGetValue(order[i], out control) && control.ContainsScreenPoint(screenPosition)) return control;
            }
            return null;
        }

        private string ResolveTopUiElement(Vector2 screenPosition, int pointerId)
        {
            EventSystem system = EventSystem.current;
            if (system != null)
            {
                var data = new PointerEventData(system) { position = screenPosition, pointerId = pointerId };
                var results = new List<RaycastResult>();
                system.RaycastAll(data, results);
                if (results.Count > 0 && results[0].gameObject != null) return results[0].gameObject.name;
            }

            // Batchmode/headless EventSystem raycasts can transiently return no
            // Graphic result. Spatial fallback keeps semantic car/target/HUD taps
            // separate from true blank area without treating them as controls.
            for (int i = 0; i < _vehicleViews.Length; i++)
            {
                VehicleView view = _vehicleViews[i];
                if (view != null && view.Rect != null && view.Rect.gameObject.activeInHierarchy
                    && RectTransformUtility.RectangleContainsScreenPoint(view.Rect, screenPosition))
                {
                    return view.Rect.gameObject.name;
                }
            }
            if (_targetMarker != null
                && _targetMarker.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(_targetMarker, screenPosition))
            {
                return _targetMarker.gameObject.name;
            }
            foreach (UnityEngine.UI.Text hud in new[] { _blockLabel, _progressLabel, _scoreLabel })
            {
                if (hud != null && hud.gameObject.activeInHierarchy
                    && RectTransformUtility.RectangleContainsScreenPoint(hud.rectTransform, screenPosition))
                {
                    return hud.gameObject.name;
                }
            }
            return string.Empty;
        }

        private void RefreshRepairRectHistory(double eventTimeMs, bool force = false)
        {
            ExperimentPointerInput repair;
            if (!_controls.TryGetValue(ExperimentInput.Repair, out repair)) return;
            Rect latest = repair.ScreenRect();
            if (!_hasCurrentRepairRect)
            {
                _currentRepairRect = latest;
                _hasCurrentRepairRect = true;
                _repairRectChangedAtMs = eventTimeMs;
                return;
            }
            if (!force && Approximately(_currentRepairRect, latest)) return;
            if (!Approximately(_currentRepairRect, latest))
            {
                _previousRepairRect = _currentRepairRect;
                _hasPreviousRepairRect = true;
                _currentRepairRect = latest;
                _repairRectChangedAtMs = eventTimeMs;
            }
        }

        private static bool Approximately(Rect left, Rect right)
        {
            return Mathf.Abs(left.x - right.x) < 0.1f
                && Mathf.Abs(left.y - right.y) < 0.1f
                && Mathf.Abs(left.width - right.width) < 0.1f
                && Mathf.Abs(left.height - right.height) < 0.1f;
        }

        private void TrackSameSpot(Vector2 pointerPosition)
        {
            if (_hasLastPointer
                && Time.unscaledTime - _lastPointerAt <= 0.30f
                && Vector2.Distance(pointerPosition, _lastPointerPosition) <= 64f)
            {
                _currentRecord.SameSpotRepeatCount++;
            }
            _hasLastPointer = true;
            _lastPointerAt = Time.unscaledTime;
            _lastPointerPosition = pointerPosition;
        }

        private float DistanceToExpectedButton(Vector2 selectedCenter)
        {
            if (_currentTrial == null) return -1f;
            ExperimentPointerInput expected;
            return _controls.TryGetValue(_currentTrial.ExpectedInput, out expected)
                ? Vector2.Distance(selectedCenter, expected.ScreenCenter())
                : -1f;
        }

        private static string NormalizedPosition(Vector2 screenPosition)
        {
            return (screenPosition.x / Mathf.Max(1f, Screen.width)).ToString("0.0000", CultureInfo.InvariantCulture)
                + "|" + (screenPosition.y / Mathf.Max(1f, Screen.height)).ToString("0.0000", CultureInfo.InvariantCulture);
        }

        private void PersistCheckpoint()
        {
            if (_csvWriter == null) return;
            try
            {
                _csvWriter.Export(_trialRecords, _eventRecords, _surveyRecords);
                if (_sessionPersistence != null)
                {
                    _sessionPersistence.SaveCheckpoint(CreateCheckpoint(SessionPersistence.StatusInProgress));
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("[InputUIComparison] checkpoint failed: " + exception);
            }
        }

        private void PersistInterruptedState()
        {
            if (_sessionPersistence == null || _manifest == null) return;
            try
            {
                FlushPendingInputFrame();
                _csvWriter?.Export(_trialRecords, _eventRecords, _surveyRecords);
                _sessionPersistence.MarkInterrupted(
                    _manifest,
                    CreateCheckpoint(SessionPersistence.StatusInterrupted));
            }
            catch (Exception exception)
            {
                Debug.LogError("[InputUIComparison] interrupted checkpoint failed: " + exception);
            }
        }

        private ExperimentSessionCheckpoint CreateCheckpoint(string completionStatus)
        {
            double checkpointNowMonotonicMs = NowMilliseconds();
            var activeVehicles = new List<ExperimentVehicleCheckpoint>();
            for (int i = 0; i < _vehicleViews.Length; i++)
            {
                VehicleView view = _vehicleViews[i];
                if (view == null || view.Trial == null || !view.Rect.gameObject.activeSelf) continue;
                activeVehicles.Add(new ExperimentVehicleCheckpoint
                {
                    VehicleId = view.VehicleId,
                    Trial = view.Trial,
                    SpawnedAtSessionMs = 0d,
                    NormalizedProgress = _currentTrial == null
                        ? 0f
                        : Mathf.Clamp01((Time.unscaledTime - _trialStartedAt)
                            / InputUIComparisonRules.TravelSeconds(view.Trial.Speed, view.Trial.SpecialState)),
                    CoveredRevealed = view == _vehicleViews[0] && _coveredRevealed,
                    IsCurrentTarget = view == _vehicleViews[0],
                    IsResolved = false,
                });
            }

            return new ExperimentSessionCheckpoint
            {
                SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion,
                ExperimentVersion = InputUIExperimentSchema.CurrentExperimentVersion,
                ParticipantId = _participantId,
                SessionId = _sessionId,
                IsPilot = _schedule != null && _schedule.IsPilot,
                CompletionStatus = completionStatus,
                Manifest = _manifest,
                Phase = _phase.ToString(),
                ConditionIndex = _blockIndex,
                TrialIndex = _trialIndex,
                EventIndex = _eventIndex,
                Score = _score,
                Misses = _misses,
                TrialElapsedMs = _currentTrial == null
                    ? 0d
                    : Math.Max(0d, checkpointNowMonotonicMs - _trialStartedAtMonotonicMs),
                ActionableElapsedMs = !_coveredRevealed
                    || double.IsPositiveInfinity(_actionableStartedAtMonotonicMs)
                    ? -1d
                    : Math.Max(0d, checkpointNowMonotonicMs - _actionableStartedAtMonotonicMs),
                RemainingGapMs = _trialResolving ? Math.Max(0d, (_nextTrialAt - Time.unscaledTime) * 1000d) : 0d,
                LastAcceptedEventTimeMs = _lastAcceptedEventTimeMs,
                RandomState = _assignment == null ? string.Empty : _assignment.Seed.ToString(CultureInfo.InvariantCulture),
                CurrentTargetVehicleId = _currentTargetVehicleId,
                CoveredRevealed = _coveredRevealed,
                TrialResolving = _trialResolving,
                CurrentRepairRect = _hasCurrentRepairRect
                    ? ExperimentRectCheckpoint.FromRect(_currentRepairRect)
                    : null,
                PreviousRepairRect = _hasPreviousRepairRect
                    ? ExperimentRectCheckpoint.FromRect(_previousRepairRect)
                    : null,
                RepairRectChangedAgeMs = _hasPreviousRepairRect
                    ? Math.Max(0d, checkpointNowMonotonicMs - _repairRectChangedAtMs)
                    : -1d,
                ActiveTrials = _activeTrials == null ? new List<ExperimentTrial>() : new List<ExperimentTrial>(_activeTrials),
                ActiveVehicles = activeVehicles,
                CurrentTrialRecord = _currentRecord,
                TrialRecords = new List<ExperimentTrialRecord>(_trialRecords),
                InputEventRecords = new List<ExperimentInputEventRecord>(_eventRecords),
                PendingInputRecords = new List<ExperimentInputEventRecord>(_pendingFrameEvents),
                SurveyRecords = new List<ExperimentSurveyRecord>(_surveyRecords),
            };
        }

        private void TryResumeSession()
        {
            string participant = (_participantField.text ?? string.Empty).Trim().ToUpperInvariant();
            if (participant.Length < 2)
            {
                _setupValidation.text = "再開する匿名参加者IDを入力してください。";
                return;
            }

            ExperimentSessionManifest manifest;
            if (!SessionPersistence.TryFindLatestIncompleteSession(
                    participant,
                    out manifest,
                    InputUIExperimentSchema.CurrentExperimentVersion))
            {
                _setupValidation.text = "このIDの未完了v2セッションは見つかりません。";
                return;
            }

            var persistence = new SessionPersistence(
                manifest.ParticipantId,
                manifest.SessionId,
                manifest.ExperimentVersion);
            ExperimentSessionCheckpoint checkpoint;
            if (!persistence.TryLoadLatestCheckpoint(out checkpoint))
            {
                _setupValidation.text = "Manifestはありますが、有効なcheckpointがありません。";
                return;
            }

            RestoreCheckpoint(persistence, manifest, checkpoint);
        }

        private void RestoreCheckpoint(
            SessionPersistence persistence,
            ExperimentSessionManifest manifest,
            ExperimentSessionCheckpoint checkpoint)
        {
            CancelInvoke();
            if (persistence == null) throw new ArgumentNullException(nameof(persistence));
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            if (checkpoint == null) throw new ArgumentNullException(nameof(checkpoint));

            _participantId = manifest.ParticipantId;
            _sessionId = manifest.SessionId;
            _selectedExperimentType = manifest.ExperimentType;
            _selectedAssistAnchor = manifest.RepairAnchorMode;

            // A completed checkpoint is authoritative even if the process died
            // before the manifest's second-phase completion write. Never call
            // BeginSession here: it would reopen completed data as in_progress.
            if (IsCompletedCheckpoint(checkpoint, manifest))
            {
                _sessionPersistence = persistence;
                _manifest = manifest;
                _manifest.CompletionStatus = SessionPersistence.StatusCompleted;
                if (string.IsNullOrWhiteSpace(_manifest.CompletedAt))
                {
                    _manifest.CompletedAt = string.IsNullOrWhiteSpace(checkpoint.SavedAt)
                        ? DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
                        : checkpoint.SavedAt;
                }
                _sessionPersistence.SaveManifest(_manifest);
                _phase = RunPhase.Complete;
                _sessionConfigurationLocked = true;
                SetOnlyPanel(_summaryPanel);
                _summaryText.text = "このsessionは完了済みです。再開や試行の再実行は行いません。";
                _summaryPath.text = "保存先\n" + _sessionPersistence.SessionDirectory;
                _setupValidation.text = string.Empty;
                return;
            }

            HumanRushRegistration rushRegistration = manifest.ExperimentType == ExperimentType.RushPackage
                && !manifest.IsPilot
                ? new HumanRushRegistration(
                    manifest.RushProtocolId,
                    manifest.ReferenceConditionId,
                    manifest.ConditionDefinitions ?? Array.Empty<HumanRushCandidateDefinition>())
                : null;
            _schedule = InputUITrialPlan.BuildSchedule(
                _participantId,
                _selectedExperimentType,
                manifest.ExperimentVersion,
                _selectedAssistAnchor,
                rushRegistration);
            _sessionPersistence = persistence;
            _manifest = manifest;
            _sessionPersistence.BeginSession(_manifest);
            _csvWriter = new InputUIExperimentCsvWriter(
                _participantId,
                _sessionId,
                manifest.ExperimentVersion,
                manifest.ExperimentType,
                _schedule.ReferenceConditionId);
            _sessionConfigurationLocked = true;
            _blockIndex = Mathf.Clamp(checkpoint.ConditionIndex, 0, _schedule.Assignments.Count - 1);
            _assignment = _schedule.Assignments[_blockIndex];
            _currentLayout = LayoutForAssignment(_assignment);
            ApplyInputLayout(_currentLayout);
            RestoreRepairRectHistory(checkpoint);

            _trialRecords.Clear();
            _trialRecords.AddRange(checkpoint.TrialRecords ?? new List<ExperimentTrialRecord>());
            _eventRecords.Clear();
            _eventRecords.AddRange(checkpoint.InputEventRecords ?? new List<ExperimentInputEventRecord>());
            _pendingFrameEvents.Clear();
            MergeRawJournalAndPendingRecovery(
                checkpoint.PendingInputRecords ?? new List<ExperimentInputEventRecord>());
            _surveyRecords.Clear();
            _surveyRecords.AddRange(checkpoint.SurveyRecords ?? new List<ExperimentSurveyRecord>());
            _activeTrials = checkpoint.ActiveTrials ?? new List<ExperimentTrial>();
            _trialIndex = checkpoint.TrialIndex;
            _eventIndex = Math.Max(
                checkpoint.EventIndex,
                _eventRecords.Count == 0 ? 0 : _eventRecords.Max(row => row.EventIndex));
            _score = checkpoint.Score;
            _misses = checkpoint.Misses;
            // realtimeSinceStartup is process-local. A force-stop/relaunch creates a
            // new monotonic epoch, so a persisted cooldown timestamp must not be
            // compared with new input or suppress the first post-resume touch.
            _lastAcceptedEventTimeMs = -1d;
            _currentTargetVehicleId = checkpoint.CurrentTargetVehicleId ?? string.Empty;
            RejectPendingInputsFromInterruptedProcess();
            _coveredRevealed = checkpoint.CoveredRevealed;
            _trialResolving = checkpoint.TrialResolving;
            _currentRecord = checkpoint.CurrentTrialRecord;
            RunPhase parsedPhase;
            _phase = Enum.TryParse(checkpoint.Phase, out parsedPhase) ? parsedPhase : RunPhase.BlockIntro;
            _blockLabel.text = "UI " + (_blockIndex + 1) + " / " + _schedule.Assignments.Count;
            UpdateScoreHud();

            if ((_phase == RunPhase.Practice || _phase == RunPhase.Measured)
                && _trialResolving)
            {
                // TrialIndex already points at the next trial (or exactly Count).
                // The resolved vehicle must never be reconstructed or replayed.
                _currentTrial = null;
                _currentRecord = null;
                SetOnlyPanel(_runPanel);
                _runOverlay.SetActive(false);
                HideAllVehicleViews();
                float remaining = (float)Math.Max(0.01d, checkpoint.RemainingGapMs / 1000d);
                _nextTrialAt = Time.unscaledTime + remaining;
                Invoke(nameof(AdvanceAfterGap), remaining);
                PersistCheckpoint();
            }
            else if ((_phase == RunPhase.Practice || _phase == RunPhase.Measured)
                && _trialIndex >= 0
                && _trialIndex < _activeTrials.Count)
            {
                _currentTrial = _activeTrials[_trialIndex];
                _trialStartedAt = Time.unscaledTime - (float)(Math.Max(0d, checkpoint.TrialElapsedMs) / 1000d);
                _actionableStartedAt = checkpoint.ActionableElapsedMs < 0d
                    ? float.PositiveInfinity
                    : Time.unscaledTime - (float)(checkpoint.ActionableElapsedMs / 1000d);
                double restoredAtMonotonicMs = NowMilliseconds();
                _trialStartedAtMonotonicMs = restoredAtMonotonicMs - Math.Max(0d, checkpoint.TrialElapsedMs);
                _actionableStartedAtMonotonicMs = checkpoint.ActionableElapsedMs < 0d
                    ? double.PositiveInfinity
                    : restoredAtMonotonicMs - Math.Max(0d, checkpoint.ActionableElapsedMs);
                SetOnlyPanel(_runPanel);
                _runOverlay.SetActive(false);
                ConfigureVehicleViews();
                UpdateRepairPresentation();
                PersistCheckpoint();
            }
            else if (_phase == RunPhase.Survey)
            {
                if (HasSubmittedSurveyForCurrentAssignment())
                {
                    _blockIndex++;
                    if (_blockIndex < _schedule.Assignments.Count) BeginBlock();
                    else ShowComplete();
                }
                else
                {
                    ShowSurvey();
                }
            }
            else if (_phase == RunPhase.MeasuredIntro)
            {
                ShowMeasuredIntro();
            }
            else if (_phase == RunPhase.BlockIntro)
            {
                BeginBlock();
            }
            else
            {
                _phase = RunPhase.BlockIntro;
                BeginBlock();
            }
            _setupValidation.text = string.Empty;
        }

        private static bool IsCompletedCheckpoint(
            ExperimentSessionCheckpoint checkpoint,
            ExperimentSessionManifest manifest)
        {
            return string.Equals(checkpoint.CompletionStatus, SessionPersistence.StatusCompleted, StringComparison.Ordinal)
                || string.Equals(checkpoint.Phase, RunPhase.Complete.ToString(), StringComparison.Ordinal)
                || string.Equals(manifest.CompletionStatus, SessionPersistence.StatusCompleted, StringComparison.Ordinal);
        }

        private bool HasSubmittedSurveyForCurrentAssignment()
        {
            return _assignment != null && _surveyRecords.Any(row => row != null
                && string.Equals(row.ParticipantId, _participantId, StringComparison.Ordinal)
                && string.Equals(row.SessionId, _sessionId, StringComparison.Ordinal)
                && row.ExperimentType == _assignment.ExperimentType
                && row.ConditionOrder == _assignment.ConditionOrder
                && string.Equals(row.Condition, _assignment.Condition, StringComparison.Ordinal));
        }

        private void MergeRawJournalAndPendingRecovery(
            IEnumerable<ExperimentInputEventRecord> checkpointPending)
        {
            // Checkpoint-finalized rows are authoritative. WAL contributes only
            // event indexes absent from the checkpoint; unresolved rows become
            // recovery-pending and are rejected before any resumed target appears.
            List<ExperimentInputEventRecord> normalized = _eventRecords
                .Where(row => row != null && row.EventIndex > 0)
                .GroupBy(row => row.EventIndex)
                .Select(group => group.First())
                .OrderBy(row => row.EventIndex)
                .ToList();
            _eventRecords.Clear();
            _eventRecords.AddRange(normalized);
            var byEventIndex = normalized.ToDictionary(row => row.EventIndex);

            foreach (ExperimentInputEventRecord pending in checkpointPending
                ?? Enumerable.Empty<ExperimentInputEventRecord>())
            {
                if (pending == null || pending.EventIndex <= 0) continue;
                if (!byEventIndex.TryGetValue(pending.EventIndex, out ExperimentInputEventRecord persisted))
                {
                    persisted = pending;
                    byEventIndex.Add(persisted.EventIndex, persisted);
                    _eventRecords.Add(persisted);
                }
                if (IsUnresolvedRawEvent(persisted)) _pendingFrameEvents.Add(persisted);
            }

            if (_sessionPersistence != null)
            {
                foreach (ExperimentInputEventRecord journalRow in _sessionPersistence.ReadRawEventJournal())
                {
                    if (journalRow == null || journalRow.EventIndex <= 0
                        || byEventIndex.ContainsKey(journalRow.EventIndex))
                    {
                        continue;
                    }
                    byEventIndex.Add(journalRow.EventIndex, journalRow);
                    _eventRecords.Add(journalRow);
                    _pendingFrameEvents.Add(journalRow);
                }
            }

            _eventRecords.Sort((left, right) => left.EventIndex.CompareTo(right.EventIndex));
        }

        private static bool IsUnresolvedRawEvent(ExperimentInputEventRecord row)
        {
            return row != null
                && !row.WasInputAccepted
                && (string.IsNullOrEmpty(row.IgnoredReason)
                    || string.Equals(row.IgnoredReason, ExperimentIgnoredReasons.None, StringComparison.Ordinal))
                && (string.IsNullOrEmpty(row.Disposition)
                    || string.Equals(row.Disposition, "raw", StringComparison.Ordinal));
        }

        private void RestoreRepairRectHistory(ExperimentSessionCheckpoint checkpoint)
        {
            Rect appliedCurrent = _currentRepairRect;
            bool hadAppliedCurrent = _hasCurrentRepairRect;
            _hasCurrentRepairRect = checkpoint.CurrentRepairRect != null || hadAppliedCurrent;
            _currentRepairRect = checkpoint.CurrentRepairRect != null
                ? checkpoint.CurrentRepairRect.ToRect()
                : appliedCurrent;
            _hasPreviousRepairRect = checkpoint.PreviousRepairRect != null;
            _previousRepairRect = checkpoint.PreviousRepairRect != null
                ? checkpoint.PreviousRepairRect.ToRect()
                : default(Rect);
            double ageMs = checkpoint.PreviousRepairRect != null
                ? Math.Max(0d, checkpoint.RepairRectChangedAgeMs)
                : 0d;
            _repairRectChangedAtMs = NowMilliseconds() - ageMs;
        }

        private void RejectPendingInputsFromInterruptedProcess()
        {
            if (_pendingFrameEvents.Count == 0) return;

            for (int i = 0; i < _pendingFrameEvents.Count; i++)
            {
                ExperimentInputEventRecord pending = _pendingFrameEvents[i];
                ExperimentInputEventRecord persisted = _eventRecords.FirstOrDefault(
                    row => row.EventIndex == pending.EventIndex);
                ExperimentInputEventRecord row = persisted ?? pending;
                row.WasInputAccepted = false;
                row.AcceptedInput = ExperimentInput.None;
                string recoveryReason = row.TrialNumber > 0
                    ? ExperimentIgnoredReasons.StaleTarget
                    : ExperimentIgnoredReasons.SessionNotRunning;
                row.IgnoredReason = recoveryReason;
                row.Disposition = recoveryReason;
                row.TargetVehicleIdAtAccept = _currentTargetVehicleId ?? string.Empty;
                row.ResolvedVehicleId = string.Empty;
                row.TargetChangedBetweenTouchAndAccept = !string.Equals(
                    row.TargetVehicleIdAtTouch,
                    row.TargetVehicleIdAtAccept,
                    StringComparison.Ordinal);
                if (persisted == null) _eventRecords.Add(row);
            }

            _eventIndex = Math.Max(
                _eventIndex,
                _eventRecords.Count == 0 ? 0 : _eventRecords.Max(row => row.EventIndex));
            _pendingFrameEvents.Clear();
        }

        private void HideAllVehicleViews()
        {
            for (int i = 0; i < _vehicleViews.Length; i++)
            {
                if (_vehicleViews[i] == null) continue;
                _vehicleViews[i].Trial = null;
                _vehicleViews[i].VehicleId = string.Empty;
                _vehicleViews[i].Rect.gameObject.SetActive(false);
            }
        }

        private void ShowFeedback(string message, Color color)
        {
            _feedbackLabel.text = message;
            _feedbackLabel.color = color;
        }

        private void UpdateScoreHud()
        {
            _scoreLabel.text = "SCORE " + _score + "  MISS " + _misses;
        }

        private void ShowRunOverlay(string title, string body, string button, Action action)
        {
            SetOnlyPanel(_runPanel);
            _runOverlay.SetActive(true);
            _overlayTitle.text = title;
            _overlayBody.text = body;
            _overlayButtonLabel.text = button;
            _overlayAction = action;
        }

        private void InvokeOverlayAction()
        {
            Action action = _overlayAction;
            _overlayAction = null;
            action?.Invoke();
        }

        private void SetOnlyPanel(GameObject active)
        {
            _setupPanel?.SetActive(active == _setupPanel);
            _runPanel?.SetActive(active == _runPanel);
            _surveyPanel?.SetActive(active == _surveyPanel);
            _summaryPanel?.SetActive(active == _summaryPanel);
        }

        private static ExperimentLayoutSpec LayoutForAssignment(ExperimentConditionAssignment assignment)
        {
            if (assignment.ExperimentType == ExperimentType.Placement)
            {
                return ExperimentLayoutCatalog.CreatePlacement(
                    assignment.Condition == ExperimentConditionIds.PlacementUpperFixed
                        ? PlacementCondition.UpperFixed
                        : PlacementCondition.BottomFixed);
            }
            if (assignment.ExperimentType == ExperimentType.StateAssist)
            {
                return ExperimentLayoutCatalog.CreateStateAssist(
                    assignment.RepairAssistMode,
                    assignment.RepairAnchorMode);
            }
            if (assignment.Condition == ExperimentConditionIds.RushCurrentEquivalent)
            {
                return ExperimentLayoutCatalog.CreateRushPackage(RushPackagePreset.CurrentEquivalent);
            }
            if (assignment.Condition == ExperimentConditionIds.RushFixedFourEquivalent)
            {
                return ExperimentLayoutCatalog.CreateRushPackage(RushPackagePreset.FixedFourEquivalent);
            }
            if (assignment.Condition == ExperimentConditionIds.RushContextualRepairEquivalent)
            {
                return ExperimentLayoutCatalog.CreateRushPackage(RushPackagePreset.ContextualRepairEquivalent);
            }
            return ExperimentLayoutCatalog.CreateRushPackage(
                assignment.RepairAnchorMode,
                assignment.RepairAssistMode,
                assignment.VehicleButtonLayoutMode);
        }

        private static ExperimentVariant LegacyVariantFor(string condition)
        {
            if (condition == ExperimentConditionIds.RushFixedFourEquivalent) return ExperimentVariant.FixedFour;
            if (condition == ExperimentConditionIds.RushContextualRepairEquivalent) return ExperimentVariant.SeparatedStateAction;
            return ExperimentVariant.Baseline;
        }

        private static string CreateVehicleId(int blockIndex, ExperimentTrial trial)
        {
            return "C" + (blockIndex + 1).ToString("00", CultureInfo.InvariantCulture)
                + (trial.IsPractice ? "-P" : "-M")
                + trial.TrialNumber.ToString("000", CultureInfo.InvariantCulture);
        }

        private static int TrialSetIndex(string trialSetId)
        {
            if (string.Equals(trialSetId, InputUITrialPlan.Set02, StringComparison.Ordinal)) return 1;
            if (string.Equals(trialSetId, InputUITrialPlan.Set03, StringComparison.Ordinal)) return 2;
            return 0;
        }

        private static string ExperimentTypeLabel(ExperimentType type)
        {
            switch (type)
            {
                case ExperimentType.Placement: return "1：配置比較";
                case ExperimentType.StateAssist: return "2：状態アシスト比較";
                case ExperimentType.RushPackage: return "3：Rush完成パッケージ比較";
                default: return type.ToString();
            }
        }

        private static double NowMilliseconds()
        {
            return Time.realtimeSinceStartupAsDouble * 1000d;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var eventSystemObject = new GameObject("InputUIComparisonEventSystem", typeof(EventSystem));
            Type inputSystemModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModule != null) eventSystemObject.AddComponent(inputSystemModule);
            else eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        // Legacy preview overload retained for existing screenshot tooling. These values are
        // package candidates only and are never used by Placement or StateAssist schedules.
        public void ShowVerificationPreview(ExperimentVariant variant, ExperimentSpecialState state)
        {
            RushPackagePreset preset = variant == ExperimentVariant.FixedFour
                ? RushPackagePreset.FixedFourEquivalent
                : variant == ExperimentVariant.SeparatedStateAction
                    ? RushPackagePreset.ContextualRepairEquivalent
                    : RushPackagePreset.CurrentEquivalent;
            ExperimentLayoutSpec layout = ExperimentLayoutCatalog.CreateRushPackage(preset);
            BeginSyntheticTrialForTests(layout, state, ExperimentVehicle.LightTruck, "VERIFY-TARGET");
            _verificationFreeze = true;
        }

        public void BeginSyntheticTrialForTests(
            ExperimentLayoutSpec layout,
            ExperimentSpecialState state,
            ExperimentVehicle vehicle,
            string targetVehicleId)
        {
            CancelInvoke();
            _verificationFreeze = false;
            _trialRecords.Clear();
            _eventRecords.Clear();
            _surveyRecords.Clear();
            _pendingFrameEvents.Clear();
            _eventIndex = 0;
            _lastAcceptedEventTimeMs = -1d;
            _lastPointerAt = -100f;
            _lastPointerPosition = Vector2.zero;
            _hasLastPointer = false;
            _score = 0;
            _misses = 0;
            _previousTrialHadError = false;
            _participantId = "TEST";
            _sessionId = "TEST-SESSION";
            _schedule = new ExperimentSchedule(
                _participantId,
                InputUIExperimentSchema.CurrentExperimentVersion,
                false,
                new[]
                {
                    new ExperimentConditionAssignment(
                        layout.ExperimentType,
                        layout.ConditionId,
                        1,
                        InputUITrialPlan.Set01,
                        1,
                        layout.RepairAnchorMode,
                        layout.RepairAssistMode,
                        layout.VehicleButtonLayoutMode),
                });
            _assignment = _schedule.Assignments[0];
            _currentLayout = layout;
            _blockIndex = 0;
            _phase = RunPhase.Measured;
            _sessionConfigurationLocked = true;
            _activeTrials = new List<ExperimentTrial>
            {
                new ExperimentTrial
                {
                    TrialNumber = 1,
                    ProblemSet = 0,
                    TrialSetId = InputUITrialPlan.Set01,
                    IsPractice = false,
                    Vehicle = vehicle,
                    SpecialState = state,
                    Speed = ExperimentSpeed.Normal,
                    CoveredRevealProgress = InputUIComparisonRules.CoveredRevealProgress,
                    GapAfterSeconds = 0.72f,
                    PaceContext = "test",
                    SequenceContext = "test",
                },
            };
            if (layout.ExperimentType == ExperimentType.RushPackage)
            {
                _activeTrials.Add(new ExperimentTrial
                {
                    TrialNumber = 2,
                    ProblemSet = 0,
                    TrialSetId = InputUITrialPlan.Set01,
                    IsPractice = false,
                    Vehicle = (ExperimentVehicle)(((int)vehicle + 1) % 3),
                    SpecialState = state == ExperimentSpecialState.Broken
                        ? ExperimentSpecialState.Normal
                        : ExperimentSpecialState.Broken,
                    Speed = ExperimentSpeed.Low,
                    CoveredRevealProgress = -1f,
                    GapAfterSeconds = 0.22f,
                    PaceContext = "test_short_gap",
                    SequenceContext = "test_adjacent_state_change",
                });
                _activeTrials.Add(new ExperimentTrial
                {
                    TrialNumber = 3,
                    ProblemSet = 0,
                    TrialSetId = InputUITrialPlan.Set01,
                    IsPractice = false,
                    Vehicle = (ExperimentVehicle)(((int)vehicle + 2) % 3),
                    SpecialState = ExperimentSpecialState.Express,
                    Speed = ExperimentSpeed.High,
                    CoveredRevealProgress = -1f,
                    GapAfterSeconds = 0.22f,
                    PaceContext = "test_express_short_gap",
                    SequenceContext = "test_speed_difference",
                });
            }
            _trialIndex = 0;
            ApplyInputLayout(layout);
            SetOnlyPanel(_runPanel);
            _runOverlay.SetActive(false);
            StartCurrentTrial();
            _currentTargetVehicleId = targetVehicleId ?? string.Empty;
            _vehicleViews[0].Rect.anchoredPosition = new Vector2(150f, 160f);
        }

        public void ForceTargetVehicleIdForTests(string targetVehicleId)
        {
            _currentTargetVehicleId = targetVehicleId ?? string.Empty;
        }

        public void SetCoveredRevealedForTests(bool revealed)
        {
            if (revealed)
            {
                RevealCoveredCar();
                return;
            }

            _coveredRevealed = false;
            _actionableStartedAt = float.PositiveInfinity;
            _actionableStartedAtMonotonicMs = double.PositiveInfinity;
            if (_currentRecord != null) _currentRecord.InformationAvailableAtMs = -1d;
            if (_vehicleViews[0] != null) _vehicleViews[0].CoverOverlay.SetActive(!revealed);
        }

        public void FlushInputFrameForTests()
        {
            FlushPendingInputFrame();
        }

        public void ResolveTimeoutForTests()
        {
            ResolveTimeout();
        }

        public ExperimentSessionCheckpoint CreateCheckpointForTests()
        {
            return CreateCheckpoint(SessionPersistence.StatusInProgress);
        }

        public void RestoreCheckpointForTests(
            SessionPersistence persistence,
            ExperimentSessionManifest manifest,
            ExperimentSessionCheckpoint checkpoint)
        {
            RestoreCheckpoint(persistence, manifest, checkpoint);
        }

        public ExperimentPointerInput GetControlForTests(ExperimentInput input)
        {
            return _controls[input];
        }
    }
}
