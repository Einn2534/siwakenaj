using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Siwakenja.Experiments.InputUIComparison
{
    public static class InputUIExperimentSchema
    {
        public const string CurrentSchemaVersion = "2";
        public const string CurrentExperimentVersion = "2.1.0";
    }

    public enum ExperimentType
    {
        Placement = 0,
        StateAssist = 1,
        RushPackage = 2,
    }

    public enum PlacementCondition
    {
        UpperFixed = 0,
        BottomFixed = 1,
    }

    public enum RepairAnchorMode
    {
        UpperFixed = 0,
        BottomFixed = 1,
    }

    public enum RepairAssistMode
    {
        AlwaysActive = 0,
        DisabledWhenNormal = 1,
        BrokenOnly = 2,
    }

    public enum VehicleButtonLayoutMode
    {
        StandardThree = 0,
        FixedFour = 1,
    }

    public enum RushPackagePreset
    {
        CurrentEquivalent = 0,
        FixedFourEquivalent = 1,
        ContextualRepairEquivalent = 2,
    }

    public static class ExperimentConditionIds
    {
        public const string PlacementUpperFixed = "Placement_UpperFixed";
        public const string PlacementBottomFixed = "Placement_BottomFixed";
        public const string AssistAlwaysActive = "Assist_AlwaysActive";
        public const string AssistDisabledWhenNormal = "Assist_DisabledWhenNormal";
        public const string AssistBrokenOnly = "Assist_BrokenOnly";
        public const string RushCurrentEquivalent = "Rush_CurrentEquivalent";
        public const string RushFixedFourEquivalent = "Rush_FixedFourEquivalent";
        public const string RushContextualRepairEquivalent = "Rush_ContextualRepairEquivalent";
    }

    public static class ExperimentIgnoredReasons
    {
        public const string None = "none";
        public const string Disabled = "disabled";
        public const string HiddenOrEmpty = "hidden_or_empty";
        public const string Cooldown = "cooldown";
        public const string SameFrameOverwritten = "same_frame_overwritten";
        public const string TrialNotActionable = "trial_not_actionable";
        public const string CoveredNotRevealed = "covered_not_revealed";
        public const string StaleTarget = "stale_target";
        public const string SessionNotRunning = "session_not_running";
    }

    public static class ExperimentSurveyQuestionIds
    {
        public const string TargetClarity = "target_clarity";
        public const string SearchedForButton = "searched_for_button";
        public const string JudgedRepairNeedFromVehicle = "judged_repair_need_from_vehicle";
        public const string UiStateRevealedAnswer = "ui_state_revealed_answer";
        public const string CanExplainMistake = "can_explain_mistake";
        public const string MeaningChanged = "meaning_changed";
        public const string MostConfusingMoment = "most_confusing_moment";
        public const string BrokenTooEasy = "broken_too_easy";
    }

    public static class ExperimentMetricIds
    {
        public const string FirstRawTouchErrorRate = "first_raw_touch_error_rate";
        public const string FirstAcceptedInputErrorRate = "first_accepted_input_error_rate";
        public const string DisabledTapEventRate = "disabled_tap_event_rate";
        public const string TrialWithDisabledTapRate = "trial_with_disabled_tap_rate";
        public const string TrialWithDisabledRepairTapRate = "trial_with_disabled_repair_tap_rate";
        public const string EmptyAreaTapEventRate = "empty_area_tap_event_rate";
        public const string TrialWithEmptyAreaTapRate = "trial_with_empty_area_tap_rate";
        public const string HiddenRepairAreaTapEventRate = "hidden_repair_area_tap_event_rate";
        public const string TrialWithHiddenRepairAreaTapRate = "trial_with_hidden_repair_area_tap_rate";
        public const string HiddenRepairAreaTapRatePerTrial = "hidden_repair_area_tap_rate_per_trial";
        public const string GenericEmptyAreaTapRatePerTrial = "generic_empty_area_tap_rate_per_trial";
        public const string StaleRepairAreaTapEventRate = "stale_repair_area_tap_event_rate";
        public const string StaleRepairAreaTapRatePerTrial = "stale_repair_area_tap_rate_per_trial";
        public const string TrialWithStaleRepairAreaTapRate = "trial_with_stale_repair_area_tap_rate";
        public const string CooldownTapEventRate = "cooldown_tap_event_rate";
        public const string TrialWithCooldownTapRate = "trial_with_cooldown_tap_rate";
        public const string SameFrameOverwrittenTapEventRate = "same_frame_overwritten_tap_event_rate";
        public const string TrialWithSameFrameOverwrittenTapRate = "trial_with_same_frame_overwritten_tap_rate";
        public const string TimeToFirstRawTouchMs = "time_to_first_raw_touch_ms";
        public const string TimeToFirstAcceptedInputMs = "time_to_first_accepted_input_ms";
        public const string TimeToCorrectResolutionMs = "time_to_correct_resolution_ms";
        public const string CorrectionCostMs = "correction_cost_ms";

        // V2.0 diagnostic metric ids retained so older exported summaries remain readable.
        public const string DisabledButtonTapRate = "disabled_button_tap_rate";
        public const string EmptyAreaTapRate = "empty_area_tap_rate";
        public const string StaleRepairAreaTapRate = "stale_repair_area_tap_rate";
        public const string CooldownRejectedTapRate = "cooldown_rejected_tap_rate";
        public const string SameFrameOverwrittenTapRate = "same_frame_overwritten_tap_rate";
        public const string TimeoutRate = "timeout_rate";
        public const string EventualCorrectRate = "eventual_correct_rate";
        public const string MedianActionableResponseTimeMs = "median_actionable_response_time_ms";
        public const string BrokenFirstAcceptedErrorRate = "broken_first_accepted_error_rate";
        public const string BrokenTimeoutRate = "broken_timeout_rate";
        public const string BrokenVehicleButtonTapRate = "broken_vehicle_button_tap_rate";
        public const string BrokenRepairSuccessRate = "broken_repair_success_rate";
        public const string CoveredPrematureRawTouchRate = "covered_premature_raw_touch_rate";
        public const string CoveredPrematureAcceptedInputRate = "covered_premature_accepted_input_rate";
        public const string CoveredPostRevealErrorRate = "covered_post_reveal_error_rate";
        public const string CoveredPostRevealTimeoutRate = "covered_post_reveal_timeout_rate";
        public const string ExpressAccuracy = "express_accuracy";
        public const string ExpressResponseTimeMs = "express_response_time_ms";
        public const string ExpressTimeoutRate = "express_timeout_rate";
        public const string SurveyTargetClarityMedian = "survey_target_clarity_median";
        public const string SurveySearchedForButtonMedian = "survey_searched_for_button_median";
        public const string SurveyJudgedRepairNeedFromVehicleMedian = "survey_judged_repair_need_from_vehicle_median";
        public const string SurveyUiStateRevealedAnswerMedian = "survey_ui_state_revealed_answer_median";
        public const string SurveyCanExplainMistakeMedian = "survey_can_explain_mistake_median";
        public const string SurveyMeaningChangedMedian = "survey_meaning_changed_median";
        public const string SurveyMostConfusingMomentMedian = "survey_most_confusing_moment_median";
        public const string SurveyBrokenTooEasyMedian = "survey_broken_too_easy_median";
    }

    public static class ExperimentMetricRoles
    {
        public const string Primary = "primary";
        public const string Diagnostic = "diagnostic";
    }

    [Serializable]
    public sealed class HumanRushCandidateDefinition
    {
        public HumanRushCandidateDefinition()
        {
        }

        public HumanRushCandidateDefinition(
            string conditionId,
            RepairAnchorMode repairAnchorMode,
            RepairAssistMode repairAssistMode,
            VehicleButtonLayoutMode vehicleButtonLayoutMode)
        {
            ConditionId = conditionId ?? string.Empty;
            RepairAnchorMode = repairAnchorMode;
            RepairAssistMode = repairAssistMode;
            VehicleButtonLayoutMode = vehicleButtonLayoutMode;
        }

        public string ConditionId;
        public RepairAnchorMode RepairAnchorMode;
        public RepairAssistMode RepairAssistMode;
        public VehicleButtonLayoutMode VehicleButtonLayoutMode;

        public HumanRushCandidateDefinition Clone()
        {
            return new HumanRushCandidateDefinition(
                ConditionId,
                RepairAnchorMode,
                RepairAssistMode,
                VehicleButtonLayoutMode);
        }

        public bool HasSamePackageAs(HumanRushCandidateDefinition other)
        {
            return other != null
                && RepairAnchorMode == other.RepairAnchorMode
                && RepairAssistMode == other.RepairAssistMode
                && VehicleButtonLayoutMode == other.VehicleButtonLayoutMode;
        }
    }

    [Serializable]
    public sealed class HumanRushRegistration
    {
        public HumanRushRegistration()
        {
        }

        public HumanRushRegistration(
            string protocolId,
            string referenceConditionId,
            params HumanRushCandidateDefinition[] candidates)
        {
            ProtocolId = protocolId ?? string.Empty;
            ReferenceConditionId = referenceConditionId ?? string.Empty;
            Candidates = candidates == null
                ? Array.Empty<HumanRushCandidateDefinition>()
                : Array.ConvertAll(candidates, candidate => candidate?.Clone());
        }

        public string ProtocolId;
        public string ReferenceConditionId;
        public HumanRushCandidateDefinition[] Candidates = Array.Empty<HumanRushCandidateDefinition>();

        public bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(ProtocolId))
            {
                error = "Human Rush requires a non-empty protocol id.";
                return false;
            }
            if (Candidates == null || Candidates.Length != 2)
            {
                error = "Human Rush requires exactly two preregistered candidates.";
                return false;
            }
            if (Candidates[0] == null || Candidates[1] == null)
            {
                error = "Human Rush candidate definitions cannot be null.";
                return false;
            }

            for (int i = 0; i < Candidates.Length; i++)
            {
                HumanRushCandidateDefinition candidate = Candidates[i];
                if (string.IsNullOrWhiteSpace(candidate.ConditionId))
                {
                    error = "Human Rush candidate ids cannot be empty.";
                    return false;
                }
                if (!candidate.ConditionId.StartsWith("Rush_", StringComparison.Ordinal))
                {
                    error = "Human Rush candidate ids must use the Rush_ prefix.";
                    return false;
                }
                if (!Enum.IsDefined(typeof(RepairAnchorMode), candidate.RepairAnchorMode)
                    || !Enum.IsDefined(typeof(RepairAssistMode), candidate.RepairAssistMode)
                    || !Enum.IsDefined(typeof(VehicleButtonLayoutMode), candidate.VehicleButtonLayoutMode))
                {
                    error = "Human Rush candidate definitions contain an unknown package value.";
                    return false;
                }
            }

            if (string.Equals(Candidates[0].ConditionId, Candidates[1].ConditionId, StringComparison.Ordinal))
            {
                error = "Human Rush candidate ids must be unique.";
                return false;
            }
            if (Candidates[0].HasSamePackageAs(Candidates[1]))
            {
                error = "Human Rush candidates must describe two different packages.";
                return false;
            }
            if (!string.Equals(ReferenceConditionId, Candidates[0].ConditionId, StringComparison.Ordinal)
                && !string.Equals(ReferenceConditionId, Candidates[1].ConditionId, StringComparison.Ordinal))
            {
                error = "Human Rush reference_condition_id must name one of the two candidates.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }

    public sealed class ExperimentConditionAssignment
    {
        public ExperimentConditionAssignment(
            ExperimentType experimentType,
            string condition,
            int conditionOrder,
            string trialSetId,
            int seed,
            RepairAnchorMode repairAnchorMode,
            RepairAssistMode repairAssistMode,
            VehicleButtonLayoutMode vehicleButtonLayoutMode)
        {
            ExperimentType = experimentType;
            Condition = condition ?? string.Empty;
            ConditionOrder = conditionOrder;
            TrialSetId = trialSetId ?? string.Empty;
            Seed = seed;
            RepairAnchorMode = repairAnchorMode;
            RepairAssistMode = repairAssistMode;
            VehicleButtonLayoutMode = vehicleButtonLayoutMode;
        }

        public ExperimentType ExperimentType { get; }
        public string Condition { get; }
        public int ConditionOrder { get; }
        public string TrialSetId { get; }
        public int Seed { get; }
        public RepairAnchorMode RepairAnchorMode { get; }
        public RepairAssistMode RepairAssistMode { get; }
        public VehicleButtonLayoutMode VehicleButtonLayoutMode { get; }
    }

    public sealed class ExperimentSchedule
    {
        public ExperimentSchedule(
            string participantId,
            string experimentVersion,
            bool isPilot,
            IReadOnlyList<ExperimentConditionAssignment> assignments,
            string protocolId = "",
            string referenceConditionId = "",
            IReadOnlyList<HumanRushCandidateDefinition> conditionDefinitions = null)
        {
            ParticipantId = participantId ?? string.Empty;
            ExperimentVersion = experimentVersion ?? string.Empty;
            IsPilot = isPilot;
            var copy = new ExperimentConditionAssignment[assignments == null ? 0 : assignments.Count];
            if (assignments != null)
            {
                for (int i = 0; i < assignments.Count; i++) copy[i] = assignments[i];
            }
            Assignments = Array.AsReadOnly(copy);
            ProtocolId = protocolId ?? string.Empty;
            ReferenceConditionId = referenceConditionId ?? string.Empty;
            var definitionCopy = new HumanRushCandidateDefinition[conditionDefinitions == null
                ? 0
                : conditionDefinitions.Count];
            if (conditionDefinitions != null)
            {
                for (int i = 0; i < conditionDefinitions.Count; i++)
                {
                    definitionCopy[i] = conditionDefinitions[i]?.Clone();
                }
            }
            ConditionDefinitions = Array.AsReadOnly(definitionCopy);
        }

        public string ParticipantId { get; }
        public string ExperimentVersion { get; }
        public bool IsPilot { get; }
        public IReadOnlyList<ExperimentConditionAssignment> Assignments { get; }
        public string ProtocolId { get; }
        public string ReferenceConditionId { get; }
        public IReadOnlyList<HumanRushCandidateDefinition> ConditionDefinitions { get; }
    }

    [Serializable]
    public sealed class ExperimentConditionSeed
    {
        public string Condition;
        public string TrialSetId;
        public int Seed;
    }

    [Serializable]
    public sealed class ExperimentSessionManifest
    {
        public string SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion;
        public string ExperimentVersion = InputUIExperimentSchema.CurrentExperimentVersion;
        public string ParticipantId;
        public string SessionId;
        public bool IsPilot;
        public string DeviceModel;
        public string Platform;
        public string ScreenResolution;
        public string SafeArea;
        public string UnityVersion;
        public string BuildVersion;
        public string GitCommit = "unknown";
        public ExperimentType ExperimentType;
        public string[] ConditionOrder;
        public string[] TrialSetOrder;
        public ExperimentConditionSeed[] SeedByCondition;
        public RepairAnchorMode RepairAnchorMode;
        public RepairAssistMode RepairAssistMode;
        public VehicleButtonLayoutMode VehicleButtonLayoutMode;
        public string RushProtocolId;
        public string ReferenceConditionId;
        public HumanRushCandidateDefinition[] ConditionDefinitions = Array.Empty<HumanRushCandidateDefinition>();
        public string StartedAt;
        public string CompletedAt;
        public string CompletionStatus;
    }

    public enum ExperimentVariant
    {
        Baseline = 0,
        FixedFour = 1,
        SeparatedStateAction = 2,
    }

    public enum ExperimentVehicle
    {
        Sports = 0,
        Compact = 1,
        LightTruck = 2,
    }

    public enum ExperimentSpecialState
    {
        Normal = 0,
        Express = 1,
        Covered = 2,
        Broken = 3,
    }

    public enum ExperimentSpeed
    {
        Low = 0,
        Normal = 1,
        High = 2,
    }

    public enum ExperimentInput
    {
        None = 0,
        Sports = 1,
        Compact = 2,
        LightTruck = 3,
        Repair = 4,
    }

    [Serializable]
    public sealed class ExperimentTrial
    {
        public int TrialNumber;
        public int ProblemSet;
        public string TrialSetId;
        public bool IsPractice;
        public ExperimentVehicle Vehicle;
        public ExperimentSpecialState SpecialState;
        public ExperimentSpeed Speed;
        public float CoveredRevealProgress;
        public float GapAfterSeconds;
        public string PaceContext;
        public string SequenceContext;
        public int RepeatGroup;

        public ExperimentInput ExpectedInput => InputUIComparisonRules.ExpectedInput(Vehicle, SpecialState);
    }

    public readonly struct InputEvaluation
    {
        public InputEvaluation(bool isCorrect, bool isSpecialMiss, string reason)
        {
            IsCorrect = isCorrect;
            IsSpecialMiss = isSpecialMiss;
            Reason = reason;
        }

        public bool IsCorrect { get; }
        public bool IsSpecialMiss { get; }
        public string Reason { get; }
    }

    [Serializable]
    public sealed class ExperimentTrialRecord
    {
        public string SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion;
        public string ExperimentVersion = InputUIExperimentSchema.CurrentExperimentVersion;
        public string ParticipantId;
        public string SessionId;
        public ExperimentType ExperimentType;
        public string Condition;
        public int ConditionOrder;
        public string TrialSetId;
        public bool IsPilot;
        public ExperimentVariant Variant;
        public int VariantOrder;
        public int ProblemSet;
        public int Seed;
        public bool IsPractice;
        public int TrialNumber;
        public ExperimentVehicle Vehicle;
        public ExperimentSpecialState SpecialState;
        public ExperimentSpeed Speed;
        public float CoveredRevealProgress;
        public float GapAfterSeconds;
        public float EffectiveTravelSeconds;
        public string PaceContext;
        public string SequenceContext;
        public ExperimentInput ExpectedInput;
        public ExperimentInput ActualInput;
        public ExperimentInput FinalInput;
        public bool IsCorrect;
        public bool EventualCorrect;
        public bool IsTimeout;
        public double InformationAvailableAtMs = -1d;
        public double TimeToFirstRawTouchMs = -1d;
        public double TimeToFirstAcceptedInputMs = -1d;
        public double TimeToCorrectResolutionMs = -1d;
        public double CorrectionCostMs = -1d;
        public double ResponseTimeMs;
        public double ActionableResponseTimeMs;
        public double FinalResponseTimeMs;
        public bool? SpecialStateMissed;
        public bool SpecialMissApplicable;
        public string ButtonPosition;
        public string WrongButtonType;
        public float DistanceToCorrectButtonPx;
        public float DistanceToCorrectButtonNormalized;
        public bool LayoutChangedBeforeInput;
        public int InputCount;
        public int RetryCount;
        public int SameSpotRepeatCount;
        public int SameFrameCoalescedCount;
        public int CooldownSuppressedCount;
        public int InvalidButtonInputCount;
        public bool PostErrorConsecutiveMiss;
        public string Timestamp;

        public IEnumerable<string> ToCsvFields()
        {
            yield return SchemaVersion;
            yield return ExperimentVersion;
            yield return ParticipantId;
            yield return SessionId;
            yield return InputUIComparisonNaming.ExperimentTypeId(ExperimentType);
            yield return Condition;
            yield return ConditionOrder.ToString(CultureInfo.InvariantCulture);
            yield return TrialSetId;
            yield return IsPilot ? "true" : "false";
            yield return InputUIComparisonNaming.VariantId(Variant);
            yield return VariantOrder.ToString(CultureInfo.InvariantCulture);
            yield return ProblemSet.ToString(CultureInfo.InvariantCulture);
            yield return Seed.ToString(CultureInfo.InvariantCulture);
            yield return IsPractice ? "true" : "false";
            yield return TrialNumber.ToString(CultureInfo.InvariantCulture);
            yield return Vehicle.ToString();
            yield return SpecialState.ToString();
            yield return Speed.ToString();
            yield return CoveredRevealProgress < 0f
                ? string.Empty
                : CoveredRevealProgress.ToString("0.000", CultureInfo.InvariantCulture);
            yield return GapAfterSeconds < 0f
                ? string.Empty
                : GapAfterSeconds.ToString("0.000", CultureInfo.InvariantCulture);
            yield return EffectiveTravelSeconds.ToString("0.000", CultureInfo.InvariantCulture);
            yield return PaceContext ?? string.Empty;
            yield return SequenceContext ?? string.Empty;
            yield return ExpectedInput.ToString();
            yield return ActualInput.ToString();
            yield return FinalInput.ToString();
            yield return IsCorrect ? "true" : "false";
            yield return EventualCorrect ? "true" : "false";
            yield return IsTimeout ? "true" : "false";
            yield return InformationAvailableAtMs < 0d ? string.Empty : InformationAvailableAtMs.ToString("0.0", CultureInfo.InvariantCulture);
            yield return TimeToFirstRawTouchMs < 0d ? string.Empty : TimeToFirstRawTouchMs.ToString("0.0", CultureInfo.InvariantCulture);
            yield return TimeToFirstAcceptedInputMs < 0d ? string.Empty : TimeToFirstAcceptedInputMs.ToString("0.0", CultureInfo.InvariantCulture);
            yield return TimeToCorrectResolutionMs < 0d ? string.Empty : TimeToCorrectResolutionMs.ToString("0.0", CultureInfo.InvariantCulture);
            yield return CorrectionCostMs < 0d ? string.Empty : CorrectionCostMs.ToString("0.0", CultureInfo.InvariantCulture);
            yield return ResponseTimeMs < 0d ? string.Empty : ResponseTimeMs.ToString("0.0", CultureInfo.InvariantCulture);
            yield return ActionableResponseTimeMs < 0d
                ? string.Empty
                : ActionableResponseTimeMs.ToString("0.0", CultureInfo.InvariantCulture);
            yield return FinalResponseTimeMs < 0d ? string.Empty : FinalResponseTimeMs.ToString("0.0", CultureInfo.InvariantCulture);
            yield return SpecialStateMissed.HasValue ? (SpecialStateMissed.Value ? "true" : "false") : string.Empty;
            yield return SpecialMissApplicable ? "true" : "false";
            yield return ButtonPosition ?? string.Empty;
            yield return WrongButtonType ?? string.Empty;
            yield return DistanceToCorrectButtonPx < 0f ? string.Empty : DistanceToCorrectButtonPx.ToString("0.0", CultureInfo.InvariantCulture);
            yield return DistanceToCorrectButtonNormalized < 0f ? string.Empty : DistanceToCorrectButtonNormalized.ToString("0.0000", CultureInfo.InvariantCulture);
            yield return LayoutChangedBeforeInput ? "true" : "false";
            yield return InputCount.ToString(CultureInfo.InvariantCulture);
            yield return RetryCount.ToString(CultureInfo.InvariantCulture);
            yield return SameSpotRepeatCount.ToString(CultureInfo.InvariantCulture);
            yield return SameFrameCoalescedCount.ToString(CultureInfo.InvariantCulture);
            yield return CooldownSuppressedCount.ToString(CultureInfo.InvariantCulture);
            yield return InvalidButtonInputCount.ToString(CultureInfo.InvariantCulture);
            yield return PostErrorConsecutiveMiss ? "true" : "false";
            yield return Timestamp;
        }
    }

    [Serializable]
    public sealed class ExperimentInputEventRecord
    {
        public string SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion;
        public string ExperimentVersion = InputUIExperimentSchema.CurrentExperimentVersion;
        public string ParticipantId;
        public string SessionId;
        public ExperimentType ExperimentType;
        public string Condition;
        public int ConditionOrder;
        public string TrialSetId;
        public bool IsPilot;
        public ExperimentVariant Variant;
        public int VariantOrder;
        public int ProblemSet;
        public bool IsPractice;
        public int TrialNumber;
        public int EventIndex;
        public int Frame;
        public ExperimentInput Input;
        public string PointerPosition;
        public string ButtonCenterPosition;
        public bool VisuallyEnabled;
        public string Disposition;
        public double StimulusTimeMs;
        public double ActionableTimeMs;
        public float DistanceToExpectedButtonPx;
        public int PointerId;
        public double EventTimeMs;
        public float ScreenXPx;
        public float ScreenYPx;
        public float ScreenXNormalized;
        public float ScreenYNormalized;
        public int ScreenWidth;
        public int ScreenHeight;
        public float SafeAreaX;
        public float SafeAreaY;
        public float SafeAreaWidth;
        public float SafeAreaHeight;
        public float UiScaleFactor;
        public string HitUiElement;
        public string HitButtonId;
        public bool WasButtonVisible;
        public bool WasButtonInteractable;
        public bool WasInputAccepted;
        public ExperimentInput AcceptedInput;
        public string IgnoredReason;
        public ExperimentInput ExpectedInput;
        public string TargetVehicleIdAtTouch;
        public string TargetVehicleIdAtAccept;
        public string ResolvedVehicleId;
        public bool TargetChangedBetweenTouchAndAccept;
        public bool IsInsideCurrentRepairRect;
        public bool IsInsidePreviousRepairRect;
        public double PreviousRepairRectAgeMs;
        public string Timestamp;
    }

    [Serializable]
    public sealed class ExperimentSurveyRecord
    {
        public string SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion;
        public string ExperimentVersion = InputUIExperimentSchema.CurrentExperimentVersion;
        public string ParticipantId;
        public string SessionId;
        public ExperimentType ExperimentType;
        public string Condition;
        public int ConditionOrder;
        public string TrialSetId;
        public bool IsPilot;
        public ExperimentVariant Variant;
        public int VariantOrder;
        public int ProblemSet;
        public int TargetClarity;
        public int SearchedForButton;
        // Legacy V1 rating retained so old checkpoints remain readable.
        public int NoticedSpecialState;
        public int JudgedRepairNeedFromVehicle;
        public int UiStateRevealedAnswer;
        public int CanExplainMistake;
        public int MeaningChanged;
        public int MostConfusingMomentRating;
        public int BrokenTooEasy;
        public string TargetClarityExplanation;
        public string ButtonSearchExplanation;
        public string RepairJudgmentExplanation;
        public string UiStateExplanation;
        public string MostConfusingMoment;
        public string MistakeExplanation;
        public string MeaningChangedExplanation;
        public string BrokenDifficultyExplanation;
        public string AnalystExplanationCode;
        public string Timestamp;
    }

    public sealed class ExperimentParticipantConditionMetric
    {
        public string SchemaVersion;
        public string ExperimentVersion;
        public ExperimentType ExperimentType;
        public string ParticipantId;
        public string SessionId;
        public string Condition;
        public int ConditionOrder;
        public string TrialSetId;
        public string Metric;
        public string MetricRole;
        public double Numerator;
        public double Denominator;
        public double Value;
    }

    public sealed class ExperimentParticipantDifferenceMetric
    {
        public string SchemaVersion;
        public string ExperimentVersion;
        public ExperimentType ExperimentType;
        public string ParticipantId;
        public string SessionId;
        public string ReferenceCondition;
        public string ComparisonCondition;
        public string Metric;
        public string MetricRole;
        public double ReferenceValue;
        public double ComparisonValue;
        public double Difference;
        public bool Improved;
        public bool Worsened;
        public bool Unchanged;
    }

    public sealed class ExperimentGroupMetricSummary
    {
        public string SchemaVersion;
        public string ExperimentVersion;
        public ExperimentType ExperimentType;
        public string ReferenceCondition;
        public string Condition;
        public string Metric;
        public string MetricRole;
        public int ParticipantCount;
        public double ParticipantMetricMedian;
        public double ParticipantMetricMin;
        public double ParticipantMetricMax;
        public int ImprovedParticipantCount;
        public int WorsenedParticipantCount;
        public int UnchangedParticipantCount;
    }

    public sealed class ExperimentAnalysisTables
    {
        public ExperimentAnalysisTables(
            IReadOnlyList<ExperimentParticipantConditionMetric> participantConditions,
            IReadOnlyList<ExperimentParticipantDifferenceMetric> participantDifferences,
            IReadOnlyList<ExperimentGroupMetricSummary> groups)
        {
            ParticipantConditions = participantConditions ?? Array.Empty<ExperimentParticipantConditionMetric>();
            ParticipantDifferences = participantDifferences ?? Array.Empty<ExperimentParticipantDifferenceMetric>();
            Groups = groups ?? Array.Empty<ExperimentGroupMetricSummary>();
        }

        public IReadOnlyList<ExperimentParticipantConditionMetric> ParticipantConditions { get; }
        public IReadOnlyList<ExperimentParticipantDifferenceMetric> ParticipantDifferences { get; }
        public IReadOnlyList<ExperimentGroupMetricSummary> Groups { get; }
    }

    public sealed class ExperimentVariantSummary
    {
        public ExperimentVariant Variant;
        public int Trials;
        public int Errors;
        public int SpecialTrials;
        public int SpecialMisses;
        public int Timeouts;
        public int InvalidAttempts;
        public int ConsecutiveMisses;
        public double ErrorRate;
        public double SpecialMissRate;
        public double TimeoutRate;
        public double InvalidAttemptRate;
        public double ConsecutiveMissRate;
        public double MedianResponseMs;
        public double MaxResponseMs;
        public double MedianAllFirstInputMs;
        public double MedianFinalResolutionMs;
        public double HighSpeedErrorRate;
        public int CanExplainMistakeRating;
        public int SearchedForButtonRating;
    }

    public static class InputUIComparisonNaming
    {
        public static string ExperimentTypeId(ExperimentType experimentType)
        {
            switch (experimentType)
            {
                case ExperimentType.Placement: return "placement";
                case ExperimentType.StateAssist: return "state_assist";
                case ExperimentType.RushPackage: return "rush_package";
                default: return experimentType.ToString();
            }
        }

        public static string VariantId(ExperimentVariant variant)
        {
            switch (variant)
            {
                case ExperimentVariant.Baseline: return "baseline_current";
                case ExperimentVariant.FixedFour: return "a_fixed_four";
                case ExperimentVariant.SeparatedStateAction: return "b_separated_state_action";
                default: return variant.ToString();
            }
        }

        public static string VehicleLabel(ExperimentVehicle vehicle)
        {
            switch (vehicle)
            {
                case ExperimentVehicle.Sports: return "スポーツ";
                case ExperimentVehicle.Compact: return "コンパクト";
                case ExperimentVehicle.LightTruck: return "トラック";
                default: return vehicle.ToString();
            }
        }

        public static string StateLabel(ExperimentSpecialState state)
        {
            switch (state)
            {
                case ExperimentSpecialState.Normal: return "通常";
                case ExperimentSpecialState.Express: return "急送";
                case ExperimentSpecialState.Covered: return "覆面";
                case ExperimentSpecialState.Broken: return "故障";
                default: return state.ToString();
            }
        }
    }
}
