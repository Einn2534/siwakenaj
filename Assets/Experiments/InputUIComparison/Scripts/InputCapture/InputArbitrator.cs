using System;
using System.Collections.Generic;

namespace Siwakenja.Experiments.InputUIComparison
{
    /// <summary>
    /// Immutable-by-convention input to the pure frame arbitrator. The caller builds
    /// one candidate for every PhysicalPointerDown, including empty-area touches.
    /// </summary>
    public sealed class InputArbitrationCandidate
    {
        public long EventSequence;
        public double EventTimeMs;
        public ExperimentInput Input;
        public string HitButtonId;
        public bool WasButtonVisible;
        public bool WasButtonInteractable;
        public string TargetVehicleIdAtTouch;
        public bool WasTrialRunningAtTouch = true;
        public bool WasCoveredRevealedAtTouch = true;

        public static InputArbitrationCandidate FromRecord(ExperimentInputEventRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            return new InputArbitrationCandidate
            {
                EventSequence = record.EventIndex,
                EventTimeMs = record.EventTimeMs,
                Input = record.Input,
                HitButtonId = record.HitButtonId,
                WasButtonVisible = record.WasButtonVisible,
                WasButtonInteractable = record.WasButtonInteractable,
                TargetVehicleIdAtTouch = record.TargetVehicleIdAtTouch,
                WasTrialRunningAtTouch = record.TrialNumber > 0,
                WasCoveredRevealedAtTouch = record.ActionableTimeMs >= 0d,
            };
        }
    }

    /// <summary>
    /// State sampled once when a frame's raw downs are ready for gameplay arbitration.
    /// All time values use the same millisecond clock as the candidates.
    /// </summary>
    public sealed class InputArbitrationContext
    {
        public bool SessionRunning = true;
        public bool TrialActionable = true;
        public bool CoveredNotRevealed;
        public bool AcceptCoveredPrematureInput;
        public string TargetVehicleIdAtAccept;
        public double LastAcceptedEventTimeMs = -1d;
        public double CooldownDurationMs = InputUIComparisonRules.GlobalInputCooldownSeconds * 1000d;
    }

    public sealed class InputArbitrationResult
    {
        public long EventSequence;
        public bool WasInputAccepted;
        public ExperimentInput AcceptedInput;
        public string IgnoredReason;
        public string TargetVehicleIdAtTouch;
        public string TargetVehicleIdAtAccept;
        public string ResolvedVehicleId;
        public bool TargetChangedBetweenTouchAndAccept;
    }

    public sealed class InputArbitrationFrameResult
    {
        public InputArbitrationFrameResult(
            IReadOnlyList<InputArbitrationResult> results,
            long acceptedEventSequence,
            double lastAcceptedEventTimeMs)
        {
            Results = results ?? throw new ArgumentNullException(nameof(results));
            AcceptedEventSequence = acceptedEventSequence;
            LastAcceptedEventTimeMs = lastAcceptedEventTimeMs;
        }

        public IReadOnlyList<InputArbitrationResult> Results { get; }
        public long AcceptedEventSequence { get; }
        public double LastAcceptedEventTimeMs { get; }
        public bool HasAcceptedInput => AcceptedEventSequence >= 0;
    }

    /// <summary>
    /// Pure last-physical-input-wins arbitration. It never reads Time, scene objects, or
    /// mutable controller state; target identity and cooldown state are explicit input.
    /// </summary>
    public static class InputArbitrator
    {
        public static InputArbitrationFrameResult ArbitrateFrame(
            IReadOnlyList<InputArbitrationCandidate> candidates,
            InputArbitrationContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            int candidateCount = candidates?.Count ?? 0;
            var results = new InputArbitrationResult[candidateCount];

            for (int i = 0; i < candidateCount; i++)
            {
                InputArbitrationCandidate candidate = candidates[i]
                    ?? throw new ArgumentException("Candidate entries cannot be null.", nameof(candidates));
                string targetAtTouch = candidate.TargetVehicleIdAtTouch ?? string.Empty;
                string targetAtAccept = context.TargetVehicleIdAtAccept ?? string.Empty;
                bool targetChanged = !string.Equals(targetAtTouch, targetAtAccept, StringComparison.Ordinal);
                var result = new InputArbitrationResult
                {
                    EventSequence = candidate.EventSequence,
                    WasInputAccepted = false,
                    AcceptedInput = ExperimentInput.None,
                    IgnoredReason = ExperimentIgnoredReasons.None,
                    TargetVehicleIdAtTouch = targetAtTouch,
                    TargetVehicleIdAtAccept = targetAtAccept,
                    ResolvedVehicleId = string.Empty,
                    TargetChangedBetweenTouchAndAccept = targetChanged,
                };
                results[i] = result;
            }

            if (candidateCount == 0)
            {
                return new InputArbitrationFrameResult(results, -1L, context.LastAcceptedEventTimeMs);
            }

            // Every physical down is retained as Raw, but only the final physical down
            // in this frame is eligible for gameplay evaluation. Earlier disabled or
            // empty rows are overwritten just like earlier valid button rows.
            for (int i = 0; i < candidateCount - 1; i++)
            {
                results[i].IgnoredReason = ExperimentIgnoredReasons.SameFrameOverwritten;
            }

            int acceptedIndex = candidateCount - 1;
            InputArbitrationCandidate acceptedCandidate = candidates[acceptedIndex];
            InputArbitrationResult acceptedResult = results[acceptedIndex];
            string fixedRejection = FixedRejection(
                acceptedCandidate,
                context,
                acceptedResult.TargetChangedBetweenTouchAndAccept);
            if (fixedRejection != ExperimentIgnoredReasons.None)
            {
                acceptedResult.IgnoredReason = fixedRejection;
                return new InputArbitrationFrameResult(results, -1L, context.LastAcceptedEventTimeMs);
            }

            acceptedResult.WasInputAccepted = true;
            acceptedResult.AcceptedInput = acceptedCandidate.Input;
            acceptedResult.IgnoredReason = ExperimentIgnoredReasons.None;
            acceptedResult.ResolvedVehicleId = context.TargetVehicleIdAtAccept ?? string.Empty;

            return new InputArbitrationFrameResult(
                results,
                acceptedResult.EventSequence,
                acceptedCandidate.EventTimeMs);
        }

        public static void ApplyToRecord(
            ExperimentInputEventRecord record,
            InputArbitrationResult result)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (record.EventIndex != result.EventSequence)
            {
                throw new ArgumentException("The arbitration result belongs to a different raw event.", nameof(result));
            }

            record.WasInputAccepted = result.WasInputAccepted;
            record.AcceptedInput = result.AcceptedInput;
            record.IgnoredReason = result.IgnoredReason ?? ExperimentIgnoredReasons.None;
            record.TargetVehicleIdAtTouch = result.TargetVehicleIdAtTouch ?? string.Empty;
            record.TargetVehicleIdAtAccept = result.TargetVehicleIdAtAccept ?? string.Empty;
            record.ResolvedVehicleId = result.ResolvedVehicleId ?? string.Empty;
            record.TargetChangedBetweenTouchAndAccept = result.TargetChangedBetweenTouchAndAccept;
        }

        private static string FixedRejection(
            InputArbitrationCandidate candidate,
            InputArbitrationContext context,
            bool targetChanged)
        {
            if (!context.SessionRunning || !candidate.WasTrialRunningAtTouch)
            {
                return ExperimentIgnoredReasons.SessionNotRunning;
            }
            if (string.IsNullOrEmpty(candidate.HitButtonId)
                || candidate.Input == ExperimentInput.None
                || !candidate.WasButtonVisible)
            {
                return ExperimentIgnoredReasons.HiddenOrEmpty;
            }

            if (!candidate.WasButtonInteractable) return ExperimentIgnoredReasons.Disabled;
            if (!context.TrialActionable)
            {
                if (!candidate.WasCoveredRevealedAtTouch && !context.AcceptCoveredPrematureInput)
                {
                    return ExperimentIgnoredReasons.CoveredNotRevealed;
                }

                return ExperimentIgnoredReasons.TrialNotActionable;
            }
            if (!candidate.WasCoveredRevealedAtTouch && !context.AcceptCoveredPrematureInput)
            {
                return ExperimentIgnoredReasons.CoveredNotRevealed;
            }

            // A down captured for one Rush target can never fall through to whichever
            // target is current in LateUpdate.
            if (targetChanged) return ExperimentIgnoredReasons.StaleTarget;

            double elapsedSinceAccepted = candidate.EventTimeMs - context.LastAcceptedEventTimeMs;
            if (context.LastAcceptedEventTimeMs >= 0d
                && elapsedSinceAccepted >= 0d
                && elapsedSinceAccepted < Math.Max(0d, context.CooldownDurationMs))
            {
                return ExperimentIgnoredReasons.Cooldown;
            }

            return ExperimentIgnoredReasons.None;
        }
    }
}
