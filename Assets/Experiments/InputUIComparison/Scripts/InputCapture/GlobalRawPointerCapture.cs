using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Siwakenja.Experiments.InputUIComparison
{
    public enum PhysicalPointerKind
    {
        Touch = 0,
        Mouse = 1,
        Pen = 2,
        Injected = 3,
    }

    /// <summary>
    /// Immutable description of one physical pointer-down before UI hit testing or
    /// gameplay acceptance. EventTimeMs uses realtime-since-startup so every device
    /// shares one monotonic clock.
    /// </summary>
    public readonly struct PhysicalPointerDown
    {
        public PhysicalPointerDown(
            long sequence,
            int pointerId,
            int deviceId,
            PhysicalPointerKind kind,
            Vector2 screenPosition,
            double eventTimeMs,
            int frame)
        {
            Sequence = sequence;
            PointerId = pointerId;
            DeviceId = deviceId;
            Kind = kind;
            ScreenPosition = screenPosition;
            EventTimeMs = eventTimeMs;
            Frame = frame;
        }

        public long Sequence { get; }
        public int PointerId { get; }
        public int DeviceId { get; }
        public PhysicalPointerKind Kind { get; }
        public Vector2 ScreenPosition { get; }
        public double EventTimeMs { get; }
        public int Frame { get; }
    }

    /// <summary>
    /// The experiment's single source of physical pointer-down events. It observes
    /// Input System devices below uGUI, so a disabled control, a CanvasGroup-hidden
    /// control, and empty screen space are all visible to the logger.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GlobalRawPointerCapture : MonoBehaviour
    {
        public const int MousePointerId = -1;
        public const int PenPointerId = -2;

        private const float SyntheticDuplicateDistancePx = 2f;
        private const double SyntheticDuplicateWindowMs = 8d;
        // Compatibility mouse callbacks can arrive one rendered frame after the
        // authoritative touch/pen callback. 42 ms covers one frame at 30/60/120 Hz
        // while the one-frame and 2 px constraints keep the suppression conservative.
        private const double DelayedSyntheticDuplicateWindowMs = 42d;

        private static GlobalRawPointerCapture _activeInstance;

        private readonly List<PublishedSample> _recentAuthoritativeSamples = new List<PublishedSample>(4);
        private long _nextSequence;
        private int _lastMouseCaptureFrame = -1;
        private int _lastPenCaptureFrame = -1;
        private bool _subscribed;

        public event Action<PhysicalPointerDown> PointerDownCaptured;

        public static GlobalRawPointerCapture ActiveInstance => _activeInstance;
        public long LastSequence => _nextSequence;

        public void RestoreSequence(long lastPersistedSequence)
        {
            if (lastPersistedSequence < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(lastPersistedSequence));
            }

            if (lastPersistedSequence > _nextSequence) _nextSequence = lastPersistedSequence;
        }

        public static void PopulateRecordPointerFields(
            ExperimentInputEventRecord record,
            PhysicalPointerDown pointerDown,
            double sessionOriginTimeMs,
            Canvas canvas = null)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            Rect safeArea = Screen.safeArea;
            float width = Mathf.Max(1f, Screen.width);
            float height = Mathf.Max(1f, Screen.height);
            record.EventIndex = checked((int)pointerDown.Sequence);
            record.Frame = pointerDown.Frame;
            record.PointerId = pointerDown.PointerId;
            record.EventTimeMs = Math.Max(0d, pointerDown.EventTimeMs - sessionOriginTimeMs);
            record.ScreenXPx = pointerDown.ScreenPosition.x;
            record.ScreenYPx = pointerDown.ScreenPosition.y;
            record.ScreenXNormalized = pointerDown.ScreenPosition.x / width;
            record.ScreenYNormalized = pointerDown.ScreenPosition.y / height;
            record.ScreenWidth = Screen.width;
            record.ScreenHeight = Screen.height;
            record.SafeAreaX = safeArea.x;
            record.SafeAreaY = safeArea.y;
            record.SafeAreaWidth = safeArea.width;
            record.SafeAreaHeight = safeArea.height;
            record.UiScaleFactor = canvas == null ? 1f : canvas.scaleFactor;
            record.PointerPosition = record.ScreenXNormalized.ToString("0.0000", CultureInfo.InvariantCulture)
                + "|" + record.ScreenYNormalized.ToString("0.0000", CultureInfo.InvariantCulture);
            record.WasInputAccepted = false;
            record.AcceptedInput = ExperimentInput.None;
            record.IgnoredReason = ExperimentIgnoredReasons.None;
            record.Timestamp = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        }

        private void OnEnable()
        {
            if (_activeInstance != null && _activeInstance != this)
            {
                Debug.LogError("[InputUIComparison] Only one GlobalRawPointerCapture may be active.", this);
                enabled = false;
                return;
            }

            _activeInstance = this;
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (_activeInstance == this) _activeInstance = null;
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            EnhancedTouchSupport.Enable();
            EnhancedTouch.onFingerDown += OnFingerDown;
            InputSystem.onAfterUpdate += CaptureMouseAndPen;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            InputSystem.onAfterUpdate -= CaptureMouseAndPen;
            EnhancedTouch.onFingerDown -= OnFingerDown;
            EnhancedTouchSupport.Disable();
            _subscribed = false;
        }

        private void OnFingerDown(Finger finger)
        {
            EnhancedTouch touch = finger.currentTouch;
            PublishNative(
                touch.touchId,
                touch.screenPosition,
                Time.realtimeSinceStartupAsDouble * 1000d,
                finger.screen.deviceId,
                PhysicalPointerKind.Touch);
        }

        private void CaptureMouseAndPen()
        {
            // Pen is evaluated first because some desktop platforms also synthesize
            // a mouse press for the same tip-down. PublishNative removes only a
            // cross-device, same-position event in a very small time window.
            Pen pen = Pen.current;
            int frame = Time.frameCount;
            if (pen != null && pen.tip.wasPressedThisFrame && _lastPenCaptureFrame != frame)
            {
                _lastPenCaptureFrame = frame;
                PublishNative(
                    PenPointerId,
                    pen.position.ReadValue(),
                    Time.realtimeSinceStartupAsDouble * 1000d,
                    pen.deviceId,
                    PhysicalPointerKind.Pen);
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && _lastMouseCaptureFrame != frame)
            {
                _lastMouseCaptureFrame = frame;
                PublishNative(
                    MousePointerId,
                    mouse.position.ReadValue(),
                    Time.realtimeSinceStartupAsDouble * 1000d,
                    mouse.deviceId,
                    PhysicalPointerKind.Mouse);
            }
        }

        /// <summary>
        /// Deterministic injection seam for EditMode/PlayMode tests. Injected events
        /// deliberately bypass native cross-device de-duplication so tests can model
        /// several physical pointers at the same position in one frame.
        /// </summary>
        public PhysicalPointerDown InjectPointerDownForTest(
            int pointerId,
            Vector2 screenPosition,
            double eventTimeMs,
            int frame = -1)
        {
            int resolvedFrame = frame >= 0 ? frame : Time.frameCount;
            return Publish(
                pointerId,
                screenPosition,
                eventTimeMs,
                0,
                PhysicalPointerKind.Injected,
                resolvedFrame,
                false);
        }

        /// <summary>
        /// Deterministic native-path seam for PlayMode verification. Unlike
        /// InjectPointerDownForTest, this applies the production cross-device
        /// synthetic-mouse suppression rule. It exists so P00 can prove that a
        /// touch plus its synthesized mouse copy produces one Raw row while two
        /// independent touch samples remain two Raw rows.
        /// </summary>
        public PhysicalPointerDown InjectNativeLikePointerDownForTest(
            int pointerId,
            int deviceId,
            PhysicalPointerKind kind,
            Vector2 screenPosition,
            double eventTimeMs,
            int frame = -1)
        {
            if (kind == PhysicalPointerKind.Injected)
            {
                throw new ArgumentException(
                    "Use InjectPointerDownForTest for injected samples.",
                    nameof(kind));
            }

            int resolvedFrame = frame >= 0 ? frame : Time.frameCount;
            return Publish(
                pointerId,
                screenPosition,
                eventTimeMs,
                deviceId,
                kind,
                resolvedFrame,
                true);
        }

        private void PublishNative(
            int pointerId,
            Vector2 screenPosition,
            double eventTimeMs,
            int deviceId,
            PhysicalPointerKind kind)
        {
            Publish(pointerId, screenPosition, eventTimeMs, deviceId, kind, Time.frameCount, true);
        }

        private PhysicalPointerDown Publish(
            int pointerId,
            Vector2 screenPosition,
            double eventTimeMs,
            int deviceId,
            PhysicalPointerKind kind,
            int frame,
            bool suppressSyntheticDuplicate)
        {
            PruneSyntheticDedupeWindow(eventTimeMs);
            if (suppressSyntheticDuplicate
                && IsSyntheticDuplicate(pointerId, kind, screenPosition, eventTimeMs, frame))
            {
                return default;
            }

            var pointerDown = new PhysicalPointerDown(
                ++_nextSequence,
                pointerId,
                deviceId,
                kind,
                screenPosition,
                eventTimeMs,
                frame);
            if (kind == PhysicalPointerKind.Touch || kind == PhysicalPointerKind.Pen)
            {
                _recentAuthoritativeSamples.Add(new PublishedSample(kind, screenPosition, eventTimeMs, frame));
            }
            PointerDownCaptured?.Invoke(pointerDown);
            return pointerDown;
        }

        private void PruneSyntheticDedupeWindow(double eventTimeMs)
        {
            for (int i = _recentAuthoritativeSamples.Count - 1; i >= 0; i--)
            {
                if (Math.Abs(_recentAuthoritativeSamples[i].EventTimeMs - eventTimeMs)
                    > DelayedSyntheticDuplicateWindowMs)
                {
                    _recentAuthoritativeSamples.RemoveAt(i);
                }
            }
        }

        private bool IsSyntheticDuplicate(
            int pointerId,
            PhysicalPointerKind kind,
            Vector2 screenPosition,
            double eventTimeMs,
            int frame)
        {
            // Touch/Pen downs are authoritative physical samples and must never be
            // removed. Some platforms additionally expose either one as a mouse
            // press, so only that lower-fidelity mouse copy is eligible for removal.
            if (kind != PhysicalPointerKind.Mouse || pointerId != MousePointerId) return false;

            float maximumDistanceSquared = SyntheticDuplicateDistancePx * SyntheticDuplicateDistancePx;
            for (int i = 0; i < _recentAuthoritativeSamples.Count; i++)
            {
                PublishedSample sample = _recentAuthoritativeSamples[i];
                double elapsedMs = Math.Abs(sample.EventTimeMs - eventTimeMs);
                bool insideImmediateWindow = elapsedMs <= SyntheticDuplicateWindowMs;
                bool insideSingleDelayedFrame = frame == sample.Frame + 1
                    && elapsedMs <= DelayedSyntheticDuplicateWindowMs;
                if (!insideImmediateWindow && !insideSingleDelayedFrame) continue;
                if ((sample.ScreenPosition - screenPosition).sqrMagnitude <= maximumDistanceSquared) return true;
            }

            return false;
        }

        private readonly struct PublishedSample
        {
            public PublishedSample(
                PhysicalPointerKind kind,
                Vector2 screenPosition,
                double eventTimeMs,
                int frame)
            {
                Kind = kind;
                ScreenPosition = screenPosition;
                EventTimeMs = eventTimeMs;
                Frame = frame;
            }

            public PhysicalPointerKind Kind { get; }
            public Vector2 ScreenPosition { get; }
            public double EventTimeMs { get; }
            public int Frame { get; }
        }
    }
}
