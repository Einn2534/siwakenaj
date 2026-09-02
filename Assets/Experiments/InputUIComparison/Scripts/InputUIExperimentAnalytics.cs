using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Siwakenja.Experiments.InputUIComparison
{
    public static class InputUIExperimentAnalytics
    {
        private const double EqualityTolerance = 0.000001d;

        public static List<ExperimentVariantSummary> Summarize(
            IEnumerable<ExperimentTrialRecord> trials,
            IEnumerable<ExperimentSurveyRecord> surveys)
        {
            var trialList = trials.Where(row => !row.IsPractice).ToList();
            var surveyList = surveys.ToList();
            var result = new List<ExperimentVariantSummary>();
            foreach (ExperimentVariant variant in Enum.GetValues(typeof(ExperimentVariant)))
            {
                List<ExperimentTrialRecord> rows = trialList.Where(row => row.Variant == variant).ToList();
                if (rows.Count == 0) continue;

                List<double> allResponseTimes = rows
                    .Select(row => row.TimeToFirstAcceptedInputMs >= 0d
                        ? row.TimeToFirstAcceptedInputMs
                        : row.ActionableResponseTimeMs >= 0d ? row.ActionableResponseTimeMs : row.ResponseTimeMs)
                    .Where(value => value >= 0d)
                    .OrderBy(value => value)
                    .ToList();
                List<double> correctFirstResponseTimes = rows
                    .Where(row => row.IsCorrect)
                    .Select(row => row.TimeToFirstAcceptedInputMs >= 0d
                        ? row.TimeToFirstAcceptedInputMs
                        : row.ActionableResponseTimeMs >= 0d ? row.ActionableResponseTimeMs : row.ResponseTimeMs)
                    .Where(value => value >= 0d)
                    .OrderBy(value => value)
                    .ToList();
                List<double> finalResolutionTimes = rows
                    .Where(row => row.EventualCorrect
                        && (row.TimeToCorrectResolutionMs >= 0d || row.FinalResponseTimeMs >= 0d))
                    .Select(row => row.TimeToCorrectResolutionMs >= 0d
                        ? row.TimeToCorrectResolutionMs
                        : row.FinalResponseTimeMs)
                    .OrderBy(value => value)
                    .ToList();
                List<ExperimentTrialRecord> special = rows.Where(row => row.SpecialMissApplicable).ToList();
                List<ExperimentTrialRecord> high = rows.Where(row => row.Speed == ExperimentSpeed.High).ToList();
                ExperimentSurveyRecord survey = surveyList.LastOrDefault(row => row.Variant == variant);
                int errors = rows.Count(row => !row.IsCorrect && !row.IsTimeout);
                int specialMisses = special.Count(row => row.SpecialStateMissed == true);
                int timeouts = rows.Count(row => row.IsTimeout);
                int invalidAttempts = rows.Sum(row => row.InvalidButtonInputCount);
                int consecutiveMisses = rows.Count(row => row.PostErrorConsecutiveMiss);

                result.Add(new ExperimentVariantSummary
                {
                    Variant = variant,
                    Trials = rows.Count,
                    Errors = errors,
                    SpecialTrials = special.Count,
                    SpecialMisses = specialMisses,
                    Timeouts = timeouts,
                    InvalidAttempts = invalidAttempts,
                    ConsecutiveMisses = consecutiveMisses,
                    ErrorRate = Rate(errors, rows.Count),
                    SpecialMissRate = Rate(specialMisses, special.Count),
                    TimeoutRate = Rate(timeouts, rows.Count),
                    InvalidAttemptRate = Rate(invalidAttempts, rows.Count),
                    ConsecutiveMissRate = Rate(consecutiveMisses, rows.Count),
                    MedianResponseMs = Median(correctFirstResponseTimes),
                    MaxResponseMs = correctFirstResponseTimes.Count == 0 ? double.NaN : correctFirstResponseTimes[correctFirstResponseTimes.Count - 1],
                    MedianAllFirstInputMs = Median(allResponseTimes),
                    MedianFinalResolutionMs = Median(finalResolutionTimes),
                    HighSpeedErrorRate = Rate(high.Count(row => !row.IsCorrect && !row.IsTimeout), high.Count),
                    CanExplainMistakeRating = survey?.CanExplainMistake ?? 0,
                    SearchedForButtonRating = survey?.SearchedForButton ?? 0,
                });
            }

            return result;
        }

        public static List<ExperimentParticipantConditionMetric> SummarizeParticipantConditions(
            IEnumerable<ExperimentTrialRecord> trials,
            IEnumerable<ExperimentInputEventRecord> events,
            IEnumerable<ExperimentSurveyRecord> surveys,
            string experimentVersion,
            ExperimentType experimentType)
        {
            if (string.IsNullOrWhiteSpace(experimentVersion))
            {
                throw new ArgumentException("An explicit experiment version is required.", nameof(experimentVersion));
            }

            List<ExperimentTrialRecord> trialList = (trials ?? Enumerable.Empty<ExperimentTrialRecord>())
                .Where(row => !row.IsPractice
                    && !row.IsPilot
                    && row.ExperimentType == experimentType
                    && string.Equals(row.ExperimentVersion, experimentVersion, StringComparison.Ordinal)
                    && !string.IsNullOrWhiteSpace(row.Condition))
                .ToList();
            List<ExperimentInputEventRecord> eventList = (events ?? Enumerable.Empty<ExperimentInputEventRecord>())
                .Where(row => !row.IsPractice
                    && !row.IsPilot
                    && row.ExperimentType == experimentType
                    && string.Equals(row.ExperimentVersion, experimentVersion, StringComparison.Ordinal)
                    && !string.IsNullOrWhiteSpace(row.Condition))
                .ToList();
            List<ExperimentSurveyRecord> surveyList = (surveys ?? Enumerable.Empty<ExperimentSurveyRecord>())
                .Where(row => !row.IsPilot
                    && row.ExperimentType == experimentType
                    && string.Equals(row.ExperimentVersion, experimentVersion, StringComparison.Ordinal)
                    && !string.IsNullOrWhiteSpace(row.Condition))
                .ToList();

            var result = new List<ExperimentParticipantConditionMetric>();
            var groups = trialList.GroupBy(row => new
            {
                row.SchemaVersion,
                row.ExperimentVersion,
                row.ExperimentType,
                row.ParticipantId,
                row.SessionId,
                row.Condition,
                row.ConditionOrder,
                row.TrialSetId,
            });
            foreach (var group in groups)
            {
                List<ExperimentTrialRecord> conditionTrials = group.ToList();
                var measuredTrialNumbers = new HashSet<int>(conditionTrials.Select(row => row.TrialNumber));
                List<ExperimentInputEventRecord> conditionEvents = eventList
                    .Where(row => string.Equals(row.ParticipantId, group.Key.ParticipantId, StringComparison.Ordinal)
                        && string.Equals(row.SessionId, group.Key.SessionId, StringComparison.Ordinal)
                        && string.Equals(row.Condition, group.Key.Condition, StringComparison.Ordinal)
                        && string.Equals(row.TrialSetId, group.Key.TrialSetId, StringComparison.Ordinal)
                        && !IsReason(row, ExperimentIgnoredReasons.SessionNotRunning)
                        && !IsReason(row, ExperimentIgnoredReasons.TrialNotActionable)
                        && measuredTrialNumbers.Contains(row.TrialNumber))
                    .OrderBy(EventOrder)
                    .ThenBy(row => row.EventIndex)
                    .ToList();
                List<ExperimentSurveyRecord> conditionSurveys = surveyList
                    .Where(row => string.Equals(row.ParticipantId, group.Key.ParticipantId, StringComparison.Ordinal)
                        && string.Equals(row.SessionId, group.Key.SessionId, StringComparison.Ordinal)
                        && string.Equals(row.Condition, group.Key.Condition, StringComparison.Ordinal))
                    .ToList();

                Func<string, double, double, double, ExperimentParticipantConditionMetric> metric =
                    (name, numerator, denominator, value) => new ExperimentParticipantConditionMetric
                    {
                        SchemaVersion = string.IsNullOrEmpty(group.Key.SchemaVersion)
                            ? InputUIExperimentSchema.CurrentSchemaVersion
                            : group.Key.SchemaVersion,
                        ExperimentVersion = group.Key.ExperimentVersion,
                        ExperimentType = group.Key.ExperimentType,
                        ParticipantId = group.Key.ParticipantId,
                        SessionId = group.Key.SessionId,
                        Condition = group.Key.Condition,
                        ConditionOrder = group.Key.ConditionOrder,
                        TrialSetId = group.Key.TrialSetId,
                        Metric = name,
                        MetricRole = MetricRole(group.Key.ExperimentType, name),
                        Numerator = numerator,
                        Denominator = denominator,
                        Value = value,
                    };

                var eventsByTrial = conditionEvents
                    .GroupBy(row => row.TrialNumber)
                    .ToDictionary(rows => rows.Key, rows => rows.OrderBy(EventOrder).ThenBy(row => row.EventIndex).ToList());
                var firstRaw = new List<(ExperimentTrialRecord trial, ExperimentInputEventRecord input)>();
                var firstAccepted = new List<(ExperimentTrialRecord trial, ExperimentInputEventRecord input)>();
                foreach (ExperimentTrialRecord trial in conditionTrials)
                {
                    if (!eventsByTrial.TryGetValue(trial.TrialNumber, out List<ExperimentInputEventRecord> trialEvents)) continue;
                    List<ExperimentInputEventRecord> measurementEvents = MeasurementEvents(trial, trialEvents).ToList();
                    ExperimentInputEventRecord raw = measurementEvents.FirstOrDefault();
                    if (raw != null) firstRaw.Add((trial, raw));
                    ExperimentInputEventRecord accepted = measurementEvents.FirstOrDefault(row => row.WasInputAccepted);
                    if (accepted != null) firstAccepted.Add((trial, accepted));
                }

                List<ExperimentInputEventRecord> disabledEvents = conditionEvents.Where(IsDisabledTap).ToList();
                List<ExperimentInputEventRecord> hiddenRepairEvents = conditionEvents.Where(IsHiddenRepairAreaTap).ToList();
                List<ExperimentInputEventRecord> genericEmptyEvents = conditionEvents.Where(IsGenericEmptyAreaTap).ToList();
                List<ExperimentInputEventRecord> emptyAreaEvents = conditionEvents
                    .Where(row => IsReason(row, ExperimentIgnoredReasons.HiddenOrEmpty)).ToList();
                HashSet<int> trialsImmediatelyAfterVisibleRepair =
                    TrialsImmediatelyAfterVisibleRepair(conditionTrials);
                List<ExperimentInputEventRecord> staleRepairEvents = conditionEvents
                    .Where(row => IsSpatialOrTargetStaleRepairAreaTap(row)
                        || (trialsImmediatelyAfterVisibleRepair.Contains(row.TrialNumber)
                            && IsHiddenRepairAreaTap(row)))
                    .ToList();
                List<ExperimentInputEventRecord> cooldownEvents = conditionEvents.Where(IsCooldownTap).ToList();
                List<ExperimentInputEventRecord> sameFrameEvents = conditionEvents.Where(IsSameFrameOverwrittenTap).ToList();

                AddRate(result, metric, ExperimentMetricIds.FirstRawTouchErrorRate,
                    firstRaw.Count(pair => RawInput(pair.input) != pair.trial.ExpectedInput), firstRaw.Count);
                AddRate(result, metric, ExperimentMetricIds.FirstAcceptedInputErrorRate,
                    firstAccepted.Count(pair => AcceptedInput(pair.input) != pair.trial.ExpectedInput), firstAccepted.Count);

                AddRate(result, metric, ExperimentMetricIds.DisabledTapEventRate,
                    disabledEvents.Count, conditionEvents.Count);
                AddRate(result, metric, ExperimentMetricIds.TrialWithDisabledTapRate,
                    TrialCount(disabledEvents), conditionTrials.Count);
                AddRate(result, metric, ExperimentMetricIds.TrialWithDisabledRepairTapRate,
                    TrialCount(disabledEvents.Where(IsRepairAreaTap)), conditionTrials.Count);
                AddRate(result, metric, ExperimentMetricIds.EmptyAreaTapEventRate,
                    genericEmptyEvents.Count, conditionEvents.Count);
                AddRate(result, metric, ExperimentMetricIds.TrialWithEmptyAreaTapRate,
                    TrialCount(genericEmptyEvents), conditionTrials.Count);
                AddRate(result, metric, ExperimentMetricIds.HiddenRepairAreaTapEventRate,
                    hiddenRepairEvents.Count, conditionEvents.Count);
                AddRate(result, metric, ExperimentMetricIds.TrialWithHiddenRepairAreaTapRate,
                    TrialCount(hiddenRepairEvents), conditionTrials.Count);
                AddRate(result, metric, ExperimentMetricIds.HiddenRepairAreaTapRatePerTrial,
                    TrialCount(hiddenRepairEvents), conditionTrials.Count);
                AddRate(result, metric, ExperimentMetricIds.GenericEmptyAreaTapRatePerTrial,
                    TrialCount(genericEmptyEvents), conditionTrials.Count);
                AddRate(result, metric, ExperimentMetricIds.StaleRepairAreaTapEventRate,
                    staleRepairEvents.Count, conditionEvents.Count);
                AddRate(result, metric, ExperimentMetricIds.StaleRepairAreaTapRatePerTrial,
                    TrialCount(staleRepairEvents), conditionTrials.Count);
                AddRate(result, metric, ExperimentMetricIds.TrialWithStaleRepairAreaTapRate,
                    TrialCount(staleRepairEvents), conditionTrials.Count);
                AddRate(result, metric, ExperimentMetricIds.CooldownTapEventRate,
                    cooldownEvents.Count, conditionEvents.Count);
                AddRate(result, metric, ExperimentMetricIds.TrialWithCooldownTapRate,
                    TrialCount(cooldownEvents), conditionTrials.Count);
                AddRate(result, metric, ExperimentMetricIds.SameFrameOverwrittenTapEventRate,
                    sameFrameEvents.Count, conditionEvents.Count);
                AddRate(result, metric, ExperimentMetricIds.TrialWithSameFrameOverwrittenTapRate,
                    TrialCount(sameFrameEvents), conditionTrials.Count);

                AddMedian(result, metric, ExperimentMetricIds.TimeToFirstRawTouchMs,
                    conditionTrials.Select(row => TimeToFirstRawTouch(row, eventsByTrial)).Where(value => value >= 0d));
                AddMedian(result, metric, ExperimentMetricIds.TimeToFirstAcceptedInputMs,
                    conditionTrials.Select(row => TimeToFirstAcceptedInput(row, eventsByTrial)).Where(value => value >= 0d));
                AddMedian(result, metric, ExperimentMetricIds.TimeToCorrectResolutionMs,
                    conditionTrials.Select(row => TimeToCorrectResolution(row, eventsByTrial)).Where(value => value >= 0d));
                AddMedian(result, metric, ExperimentMetricIds.CorrectionCostMs,
                    conditionTrials.Select(row => CorrectionCost(row, eventsByTrial)).Where(value => value >= 0d));

                // V2.0 event-denominator diagnostics are intentionally retained for backward comparison only.
                AddRate(result, metric, ExperimentMetricIds.DisabledButtonTapRate,
                    disabledEvents.Count, conditionEvents.Count);
                AddRate(result, metric, ExperimentMetricIds.EmptyAreaTapRate,
                    emptyAreaEvents.Count, conditionEvents.Count);
                AddRate(result, metric, ExperimentMetricIds.StaleRepairAreaTapRate,
                    staleRepairEvents.Count, conditionEvents.Count);
                AddRate(result, metric, ExperimentMetricIds.CooldownRejectedTapRate,
                    cooldownEvents.Count, conditionEvents.Count);
                AddRate(result, metric, ExperimentMetricIds.SameFrameOverwrittenTapRate,
                    sameFrameEvents.Count, conditionEvents.Count);
                AddRate(result, metric, ExperimentMetricIds.TimeoutRate,
                    conditionTrials.Count(row => row.IsTimeout), conditionTrials.Count);
                AddRate(result, metric, ExperimentMetricIds.EventualCorrectRate,
                    conditionTrials.Count(row => row.EventualCorrect), conditionTrials.Count);
                AddMedian(result, metric, ExperimentMetricIds.MedianActionableResponseTimeMs,
                    conditionTrials.Select(row => row.ActionableResponseTimeMs).Where(value => value >= 0d));

                List<ExperimentTrialRecord> brokenTrials = conditionTrials
                    .Where(row => row.SpecialState == ExperimentSpecialState.Broken).ToList();
                var brokenFirstAccepted = firstAccepted
                    .Where(pair => pair.trial.SpecialState == ExperimentSpecialState.Broken).ToList();
                List<ExperimentInputEventRecord> brokenEvents = EventsForTrials(conditionEvents, brokenTrials);
                AddRate(result, metric, ExperimentMetricIds.BrokenFirstAcceptedErrorRate,
                    brokenFirstAccepted.Count(pair => AcceptedInput(pair.input) != ExperimentInput.Repair), brokenFirstAccepted.Count);
                AddRate(result, metric, ExperimentMetricIds.BrokenTimeoutRate,
                    brokenTrials.Count(row => row.IsTimeout), brokenTrials.Count);
                AddRate(result, metric, ExperimentMetricIds.BrokenVehicleButtonTapRate,
                    brokenEvents.Count(row => IsVehicleInput(RawInput(row))), brokenEvents.Count);
                AddRate(result, metric, ExperimentMetricIds.BrokenRepairSuccessRate,
                    brokenTrials.Count(row => row.EventualCorrect && row.FinalInput == ExperimentInput.Repair), brokenTrials.Count);

                List<ExperimentTrialRecord> coveredTrials = conditionTrials
                    .Where(row => row.SpecialState == ExperimentSpecialState.Covered).ToList();
                var coveredTrialNumbers = new HashSet<int>(coveredTrials.Select(row => row.TrialNumber));
                List<ExperimentInputEventRecord> coveredEvents = conditionEvents
                    .Where(row => coveredTrialNumbers.Contains(row.TrialNumber)).ToList();
                var prematureByTrial = new HashSet<int>(coveredEvents.Where(IsBeforeReveal).Select(row => row.TrialNumber));
                var prematureAcceptedByTrial = new HashSet<int>(coveredEvents
                    .Where(row => IsBeforeReveal(row) && row.WasInputAccepted).Select(row => row.TrialNumber));
                var coveredPostRevealFirstAccepted = coveredEvents
                    .Where(row => !IsBeforeReveal(row) && row.WasInputAccepted)
                    .GroupBy(row => row.TrialNumber)
                    .Select(rows => rows.OrderBy(EventOrder).ThenBy(row => row.EventIndex).First())
                    .ToList();
                AddRate(result, metric, ExperimentMetricIds.CoveredPrematureRawTouchRate,
                    prematureByTrial.Count, coveredTrials.Count);
                AddRate(result, metric, ExperimentMetricIds.CoveredPrematureAcceptedInputRate,
                    prematureAcceptedByTrial.Count, coveredTrials.Count);
                AddRate(result, metric, ExperimentMetricIds.CoveredPostRevealErrorRate,
                    coveredPostRevealFirstAccepted.Count(row => AcceptedInput(row) != row.ExpectedInput),
                    coveredPostRevealFirstAccepted.Count);
                AddRate(result, metric, ExperimentMetricIds.CoveredPostRevealTimeoutRate,
                    coveredTrials.Count(row => row.IsTimeout), coveredTrials.Count);

                List<ExperimentTrialRecord> expressTrials = conditionTrials
                    .Where(row => row.SpecialState == ExperimentSpecialState.Express).ToList();
                var expressFirstAccepted = firstAccepted
                    .Where(pair => pair.trial.SpecialState == ExperimentSpecialState.Express).ToList();
                AddRate(result, metric, ExperimentMetricIds.ExpressAccuracy,
                    expressFirstAccepted.Count(pair => AcceptedInput(pair.input) == pair.trial.ExpectedInput), expressFirstAccepted.Count);
                AddMedian(result, metric, ExperimentMetricIds.ExpressResponseTimeMs,
                    expressTrials.Where(row => row.IsCorrect)
                        .Select(row => row.ActionableResponseTimeMs >= 0d ? row.ActionableResponseTimeMs : row.ResponseTimeMs)
                        .Where(value => value >= 0d));
                AddRate(result, metric, ExperimentMetricIds.ExpressTimeoutRate,
                    expressTrials.Count(row => row.IsTimeout), expressTrials.Count);

                AddSurveyMedian(result, metric, ExperimentMetricIds.SurveyTargetClarityMedian,
                    conditionSurveys.Select(row => row.TargetClarity));
                AddSurveyMedian(result, metric, ExperimentMetricIds.SurveySearchedForButtonMedian,
                    conditionSurveys.Select(row => row.SearchedForButton));
                AddSurveyMedian(result, metric, ExperimentMetricIds.SurveyJudgedRepairNeedFromVehicleMedian,
                    conditionSurveys.Select(row => row.JudgedRepairNeedFromVehicle));
                AddSurveyMedian(result, metric, ExperimentMetricIds.SurveyUiStateRevealedAnswerMedian,
                    conditionSurveys.Select(row => row.UiStateRevealedAnswer));
                AddSurveyMedian(result, metric, ExperimentMetricIds.SurveyCanExplainMistakeMedian,
                    conditionSurveys.Select(row => row.CanExplainMistake));
                AddSurveyMedian(result, metric, ExperimentMetricIds.SurveyMeaningChangedMedian,
                    conditionSurveys.Select(row => row.MeaningChanged));
                AddSurveyMedian(result, metric, ExperimentMetricIds.SurveyMostConfusingMomentMedian,
                    conditionSurveys.Select(row => row.MostConfusingMomentRating));
                AddSurveyMedian(result, metric, ExperimentMetricIds.SurveyBrokenTooEasyMedian,
                    conditionSurveys.Select(row => row.BrokenTooEasy));
            }

            return result;
        }

        public static ExperimentAnalysisTables BuildAnalysisTables(
            IEnumerable<ExperimentTrialRecord> trials,
            IEnumerable<ExperimentInputEventRecord> events,
            IEnumerable<ExperimentSurveyRecord> surveys,
            string experimentVersion,
            ExperimentType experimentType)
        {
            return BuildAnalysisTables(
                trials,
                events,
                surveys,
                experimentVersion,
                experimentType,
                ReferenceCondition(experimentType));
        }

        public static ExperimentAnalysisTables BuildAnalysisTables(
            IEnumerable<ExperimentTrialRecord> trials,
            IEnumerable<ExperimentInputEventRecord> events,
            IEnumerable<ExperimentSurveyRecord> surveys,
            string experimentVersion,
            ExperimentType experimentType,
            string referenceConditionId)
        {
            referenceConditionId = RequireReferenceCondition(referenceConditionId);
            List<ExperimentParticipantConditionMetric> participantConditions = SummarizeParticipantConditions(
                trials, events, surveys, experimentVersion, experimentType);
            List<ExperimentParticipantDifferenceMetric> participantDifferences =
                CalculateParticipantDifferences(participantConditions, referenceConditionId);
            List<ExperimentGroupMetricSummary> groups = SummarizeGroups(
                participantConditions,
                participantDifferences,
                referenceConditionId);
            return new ExperimentAnalysisTables(participantConditions, participantDifferences, groups);
        }

        public static string ExportAnalysisTables(
            string rootDirectory,
            ExperimentAnalysisTables tables,
            string experimentVersion,
            ExperimentType experimentType)
        {
            if (tables == null) throw new ArgumentNullException(nameof(tables));
            if (string.IsNullOrWhiteSpace(rootDirectory)) throw new ArgumentException("An output root is required.", nameof(rootDirectory));
            if (string.IsNullOrWhiteSpace(experimentVersion)) throw new ArgumentException("An experiment version is required.", nameof(experimentVersion));

            EnsureSingleVersionAndType(tables.ParticipantConditions, experimentVersion, experimentType);
            EnsureSingleVersionAndType(tables.ParticipantDifferences, experimentVersion, experimentType);
            EnsureSingleVersionAndType(tables.Groups, experimentVersion, experimentType);

            string directory = Path.Combine(
                rootDirectory,
                SanitizePathComponent(experimentVersion),
                InputUIComparisonNaming.ExperimentTypeId(experimentType));
            Directory.CreateDirectory(directory);
            WriteUtf8BomAtomic(
                Path.Combine(directory, "summary_participant_condition.csv"),
                ParticipantConditionLines(tables.ParticipantConditions));
            WriteUtf8BomAtomic(
                Path.Combine(directory, "summary_participant_difference.csv"),
                ParticipantDifferenceLines(tables.ParticipantDifferences));
            WriteUtf8BomAtomic(
                Path.Combine(directory, "summary_group.csv"),
                GroupSummaryLines(tables.Groups));
            return directory;
        }

        public static List<ExperimentParticipantDifferenceMetric> CalculateParticipantDifferences(
            IEnumerable<ExperimentParticipantConditionMetric> participantConditionMetrics)
        {
            return CalculateParticipantDifferencesCore(participantConditionMetrics, null);
        }

        public static List<ExperimentParticipantDifferenceMetric> CalculateParticipantDifferences(
            IEnumerable<ExperimentParticipantConditionMetric> participantConditionMetrics,
            string referenceConditionId)
        {
            return CalculateParticipantDifferencesCore(
                participantConditionMetrics,
                RequireReferenceCondition(referenceConditionId));
        }

        private static List<ExperimentParticipantDifferenceMetric> CalculateParticipantDifferencesCore(
            IEnumerable<ExperimentParticipantConditionMetric> participantConditionMetrics,
            string explicitReferenceConditionId)
        {
            var metrics = (participantConditionMetrics ?? Enumerable.Empty<ExperimentParticipantConditionMetric>())
                .Where(row => IsFinite(row.Value)).ToList();
            var result = new List<ExperimentParticipantDifferenceMetric>();
            foreach (var session in metrics.GroupBy(row => new
            {
                row.SchemaVersion,
                row.ExperimentVersion,
                row.ExperimentType,
                row.ParticipantId,
                row.SessionId,
            }))
            {
                string referenceCondition = string.IsNullOrEmpty(explicitReferenceConditionId)
                    ? ReferenceCondition(session.Key.ExperimentType)
                    : explicitReferenceConditionId;
                List<ExperimentParticipantConditionMetric> referenceRows = session
                    .Where(row => string.Equals(row.Condition, referenceCondition, StringComparison.Ordinal)).ToList();
                if (referenceRows.Count == 0) continue;
                var referenceByMetric = referenceRows.ToDictionary(row => row.Metric, row => row);
                foreach (IGrouping<string, ExperimentParticipantConditionMetric> comparison in session
                    .Where(row => !string.Equals(row.Condition, referenceCondition, StringComparison.Ordinal))
                    .GroupBy(row => row.Condition))
                {
                    foreach (ExperimentParticipantConditionMetric comparisonRow in comparison)
                    {
                        if (!referenceByMetric.TryGetValue(comparisonRow.Metric, out ExperimentParticipantConditionMetric referenceRow)) continue;
                        double difference = comparisonRow.Value - referenceRow.Value;
                        bool unchanged = Math.Abs(difference) <= EqualityTolerance;
                        bool improved = !unchanged && ((difference < 0d) == LowerIsBetter(comparisonRow.Metric));
                        result.Add(new ExperimentParticipantDifferenceMetric
                        {
                            SchemaVersion = session.Key.SchemaVersion,
                            ExperimentVersion = session.Key.ExperimentVersion,
                            ExperimentType = session.Key.ExperimentType,
                            ParticipantId = session.Key.ParticipantId,
                            SessionId = session.Key.SessionId,
                            ReferenceCondition = referenceCondition,
                            ComparisonCondition = comparison.Key,
                            Metric = comparisonRow.Metric,
                            MetricRole = MetricRole(session.Key.ExperimentType, comparisonRow.Metric),
                            ReferenceValue = referenceRow.Value,
                            ComparisonValue = comparisonRow.Value,
                            Difference = difference,
                            Improved = improved,
                            Worsened = !unchanged && !improved,
                            Unchanged = unchanged,
                        });
                    }
                }
            }
            return result;
        }

        public static List<ExperimentGroupMetricSummary> SummarizeGroups(
            IEnumerable<ExperimentParticipantConditionMetric> participantConditionMetrics,
            IEnumerable<ExperimentParticipantDifferenceMetric> differences)
        {
            return SummarizeGroupsCore(participantConditionMetrics, differences, null);
        }

        public static List<ExperimentGroupMetricSummary> SummarizeGroups(
            IEnumerable<ExperimentParticipantConditionMetric> participantConditionMetrics,
            IEnumerable<ExperimentParticipantDifferenceMetric> differences,
            string referenceConditionId)
        {
            return SummarizeGroupsCore(
                participantConditionMetrics,
                differences,
                RequireReferenceCondition(referenceConditionId));
        }

        private static List<ExperimentGroupMetricSummary> SummarizeGroupsCore(
            IEnumerable<ExperimentParticipantConditionMetric> participantConditionMetrics,
            IEnumerable<ExperimentParticipantDifferenceMetric> differences,
            string explicitReferenceConditionId)
        {
            List<ExperimentParticipantConditionMetric> metricRows =
                (participantConditionMetrics ?? Enumerable.Empty<ExperimentParticipantConditionMetric>())
                .Where(row => IsFinite(row.Value)).ToList();
            List<ExperimentParticipantDifferenceMetric> differenceRows =
                (differences ?? Enumerable.Empty<ExperimentParticipantDifferenceMetric>()).ToList();
            var result = new List<ExperimentGroupMetricSummary>();
            foreach (var group in metricRows.GroupBy(row => new
            {
                row.SchemaVersion,
                row.ExperimentVersion,
                row.ExperimentType,
                row.Condition,
                row.Metric,
            }))
            {
                var participantGroups = group.GroupBy(row => row.ParticipantId).ToList();
                if (participantGroups.Any(rows => rows.Count() != 1))
                {
                    throw new InvalidOperationException(
                        "Select one complete session per participant before group aggregation: "
                        + group.Key.ExperimentVersion + "/" + group.Key.Condition + "/" + group.Key.Metric);
                }
                List<double> values = participantGroups.Select(rows => rows.Single().Value).OrderBy(value => value).ToList();
                string referenceCondition = string.IsNullOrEmpty(explicitReferenceConditionId)
                    ? ReferenceCondition(group.Key.ExperimentType)
                    : explicitReferenceConditionId;
                List<ExperimentParticipantDifferenceMetric> directions = differenceRows.Where(row =>
                        row.ExperimentType == group.Key.ExperimentType
                        && string.Equals(row.ExperimentVersion, group.Key.ExperimentVersion, StringComparison.Ordinal)
                        && string.Equals(row.ComparisonCondition, group.Key.Condition, StringComparison.Ordinal)
                        && string.Equals(row.Metric, group.Key.Metric, StringComparison.Ordinal))
                    .ToList();
                result.Add(new ExperimentGroupMetricSummary
                {
                    SchemaVersion = group.Key.SchemaVersion,
                    ExperimentVersion = group.Key.ExperimentVersion,
                    ExperimentType = group.Key.ExperimentType,
                    ReferenceCondition = referenceCondition,
                    Condition = group.Key.Condition,
                    Metric = group.Key.Metric,
                    MetricRole = MetricRole(group.Key.ExperimentType, group.Key.Metric),
                    ParticipantCount = values.Count,
                    ParticipantMetricMedian = Median(values),
                    ParticipantMetricMin = values.First(),
                    ParticipantMetricMax = values.Last(),
                    ImprovedParticipantCount = directions.Count(row => row.Improved),
                    WorsenedParticipantCount = directions.Count(row => row.Worsened),
                    UnchangedParticipantCount = string.Equals(group.Key.Condition, referenceCondition, StringComparison.Ordinal)
                        ? values.Count
                        : directions.Count(row => row.Unchanged),
                });
            }
            return result;
        }

        public static string ReferenceCondition(ExperimentType experimentType)
        {
            switch (experimentType)
            {
                case ExperimentType.Placement: return ExperimentConditionIds.PlacementUpperFixed;
                case ExperimentType.StateAssist: return ExperimentConditionIds.AssistAlwaysActive;
                case ExperimentType.RushPackage: return ExperimentConditionIds.RushCurrentEquivalent;
                default: throw new ArgumentOutOfRangeException(nameof(experimentType), experimentType, null);
            }
        }

        private static string RequireReferenceCondition(string referenceConditionId)
        {
            if (string.IsNullOrWhiteSpace(referenceConditionId))
            {
                throw new ArgumentException(
                    "An explicit non-empty reference condition is required.",
                    nameof(referenceConditionId));
            }
            return referenceConditionId.Trim();
        }

        public static bool LowerIsBetter(string metric)
        {
            switch (metric)
            {
                case ExperimentMetricIds.EventualCorrectRate:
                case ExperimentMetricIds.BrokenRepairSuccessRate:
                case ExperimentMetricIds.ExpressAccuracy:
                case ExperimentMetricIds.SurveyTargetClarityMedian:
                case ExperimentMetricIds.SurveyJudgedRepairNeedFromVehicleMedian:
                case ExperimentMetricIds.SurveyCanExplainMistakeMedian:
                    return false;
                default:
                    return true;
            }
        }

        public static string MetricRole(ExperimentType experimentType, string metric)
        {
            if (string.Equals(metric, ExperimentMetricIds.FirstRawTouchErrorRate, StringComparison.Ordinal)
                || string.Equals(metric, ExperimentMetricIds.TimeToCorrectResolutionMs, StringComparison.Ordinal)
                || string.Equals(metric, ExperimentMetricIds.CorrectionCostMs, StringComparison.Ordinal)
                || string.Equals(metric, ExperimentMetricIds.TimeoutRate, StringComparison.Ordinal)
                || string.Equals(metric, ExperimentMetricIds.EventualCorrectRate, StringComparison.Ordinal))
            {
                return ExperimentMetricRoles.Primary;
            }

            if (experimentType == ExperimentType.StateAssist
                && (string.Equals(metric, ExperimentMetricIds.TrialWithDisabledRepairTapRate, StringComparison.Ordinal)
                    || string.Equals(metric, ExperimentMetricIds.TrialWithHiddenRepairAreaTapRate, StringComparison.Ordinal)))
            {
                return ExperimentMetricRoles.Primary;
            }

            return ExperimentMetricRoles.Diagnostic;
        }

        private static void AddRate(
            ICollection<ExperimentParticipantConditionMetric> destination,
            Func<string, double, double, double, ExperimentParticipantConditionMetric> factory,
            string metric,
            int numerator,
            int denominator)
        {
            destination.Add(factory(metric, numerator, denominator, Rate(numerator, denominator)));
        }

        private static void AddMedian(
            ICollection<ExperimentParticipantConditionMetric> destination,
            Func<string, double, double, double, ExperimentParticipantConditionMetric> factory,
            string metric,
            IEnumerable<double> values)
        {
            List<double> sorted = values.OrderBy(value => value).ToList();
            destination.Add(factory(metric, double.NaN, sorted.Count, Median(sorted)));
        }

        private static void AddSurveyMedian(
            ICollection<ExperimentParticipantConditionMetric> destination,
            Func<string, double, double, double, ExperimentParticipantConditionMetric> factory,
            string metric,
            IEnumerable<int> values)
        {
            AddMedian(destination, factory, metric, values.Where(value => value >= 1 && value <= 5).Select(value => (double)value));
        }

        private static List<ExperimentInputEventRecord> EventsForTrials(
            IEnumerable<ExperimentInputEventRecord> events,
            IEnumerable<ExperimentTrialRecord> trials)
        {
            var numbers = new HashSet<int>(trials.Select(row => row.TrialNumber));
            return events.Where(row => numbers.Contains(row.TrialNumber)).ToList();
        }

        private static bool IsReason(ExperimentInputEventRecord row, string reason)
        {
            return string.Equals(row.IgnoredReason, reason, StringComparison.Ordinal);
        }

        private static IEnumerable<ExperimentInputEventRecord> MeasurementEvents(
            ExperimentTrialRecord trial,
            IEnumerable<ExperimentInputEventRecord> events)
        {
            return trial.SpecialState == ExperimentSpecialState.Covered
                ? events.Where(row => !IsBeforeReveal(row))
                : events;
        }

        private static bool IsDisabledTap(ExperimentInputEventRecord row)
        {
            return IsReason(row, ExperimentIgnoredReasons.Disabled)
                || string.Equals(row.Disposition, "disabled_control_attempt", StringComparison.Ordinal);
        }

        private static bool IsRepairAreaTap(ExperimentInputEventRecord row)
        {
            return RawInput(row) == ExperimentInput.Repair
                || row.IsInsideCurrentRepairRect
                || row.IsInsidePreviousRepairRect
                || string.Equals(row.HitButtonId, "repair", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSpatialOrTargetStaleRepairAreaTap(ExperimentInputEventRecord row)
        {
            return (IsReason(row, ExperimentIgnoredReasons.StaleTarget) && IsRepairAreaTap(row))
                || (row.IsInsidePreviousRepairRect && !row.IsInsideCurrentRepairRect);
        }

        private static bool IsHiddenRepairAreaTap(ExperimentInputEventRecord row)
        {
            return IsReason(row, ExperimentIgnoredReasons.HiddenOrEmpty)
                && !IsSpatialOrTargetStaleRepairAreaTap(row)
                && !row.WasButtonVisible
                && IsRepairAreaTap(row);
        }

        private static bool IsGenericEmptyAreaTap(ExperimentInputEventRecord row)
        {
            return IsReason(row, ExperimentIgnoredReasons.HiddenOrEmpty)
                && !IsSpatialOrTargetStaleRepairAreaTap(row)
                && !IsHiddenRepairAreaTap(row)
                && string.IsNullOrEmpty(row.HitUiElement);
        }

        private static HashSet<int> TrialsImmediatelyAfterVisibleRepair(
            IEnumerable<ExperimentTrialRecord> trials)
        {
            List<ExperimentTrialRecord> ordered = trials
                .OrderBy(row => row.TrialNumber)
                .ToList();
            var result = new HashSet<int>();
            for (int i = 1; i < ordered.Count; i++)
            {
                // Broken is the only state whose correct control is Repair. In
                // BrokenOnly/contextual layouts that control then disappears on a
                // following non-Broken trial, so a hidden-area tap there is the
                // temporal "kept pressing where Repair used to be" stale signal.
                if (ordered[i - 1].ExpectedInput == ExperimentInput.Repair
                    && ordered[i].ExpectedInput != ExperimentInput.Repair)
                {
                    result.Add(ordered[i].TrialNumber);
                }
            }

            return result;
        }

        private static bool IsCooldownTap(ExperimentInputEventRecord row)
        {
            return IsReason(row, ExperimentIgnoredReasons.Cooldown)
                || string.Equals(row.Disposition, "cooldown_suppressed", StringComparison.Ordinal);
        }

        private static bool IsSameFrameOverwrittenTap(ExperimentInputEventRecord row)
        {
            return IsReason(row, ExperimentIgnoredReasons.SameFrameOverwritten)
                || string.Equals(row.Disposition, "same_frame_overwritten", StringComparison.Ordinal);
        }

        private static int TrialCount(IEnumerable<ExperimentInputEventRecord> rows)
        {
            return rows.Select(row => row.TrialNumber).Distinct().Count();
        }

        private static double TimeToFirstRawTouch(
            ExperimentTrialRecord trial,
            IReadOnlyDictionary<int, List<ExperimentInputEventRecord>> eventsByTrial)
        {
            if (trial.TimeToFirstRawTouchMs >= 0d) return trial.TimeToFirstRawTouchMs;
            if (!eventsByTrial.TryGetValue(trial.TrialNumber, out List<ExperimentInputEventRecord> events)) return -1d;
            ExperimentInputEventRecord first = MeasurementEvents(trial, events).FirstOrDefault();
            return EventMeasurementTime(trial, first);
        }

        private static double TimeToFirstAcceptedInput(
            ExperimentTrialRecord trial,
            IReadOnlyDictionary<int, List<ExperimentInputEventRecord>> eventsByTrial)
        {
            if (trial.TimeToFirstAcceptedInputMs >= 0d) return trial.TimeToFirstAcceptedInputMs;
            if (trial.ActionableResponseTimeMs >= 0d) return trial.ActionableResponseTimeMs;
            if (!eventsByTrial.TryGetValue(trial.TrialNumber, out List<ExperimentInputEventRecord> events)) return -1d;
            ExperimentInputEventRecord first = MeasurementEvents(trial, events).FirstOrDefault(row => row.WasInputAccepted);
            return EventMeasurementTime(trial, first);
        }

        private static double TimeToCorrectResolution(
            ExperimentTrialRecord trial,
            IReadOnlyDictionary<int, List<ExperimentInputEventRecord>> eventsByTrial)
        {
            if (trial.TimeToCorrectResolutionMs >= 0d) return trial.TimeToCorrectResolutionMs;
            if (!trial.EventualCorrect) return -1d;
            if (trial.FinalResponseTimeMs >= 0d)
            {
                return trial.SpecialState == ExperimentSpecialState.Covered && trial.InformationAvailableAtMs >= 0d
                    ? Math.Max(0d, trial.FinalResponseTimeMs - trial.InformationAvailableAtMs)
                    : trial.FinalResponseTimeMs;
            }
            if (!eventsByTrial.TryGetValue(trial.TrialNumber, out List<ExperimentInputEventRecord> events)) return -1d;
            ExperimentInputEventRecord correct = MeasurementEvents(trial, events)
                .FirstOrDefault(row => row.WasInputAccepted && AcceptedInput(row) == trial.ExpectedInput);
            return EventMeasurementTime(trial, correct);
        }

        private static double CorrectionCost(
            ExperimentTrialRecord trial,
            IReadOnlyDictionary<int, List<ExperimentInputEventRecord>> eventsByTrial)
        {
            if (trial.CorrectionCostMs >= 0d) return trial.CorrectionCostMs;
            double firstRaw = TimeToFirstRawTouch(trial, eventsByTrial);
            double resolution = TimeToCorrectResolution(trial, eventsByTrial);
            return firstRaw >= 0d && resolution >= firstRaw ? resolution - firstRaw : -1d;
        }

        private static double EventMeasurementTime(
            ExperimentTrialRecord trial,
            ExperimentInputEventRecord row)
        {
            if (row == null) return -1d;
            if (row.ActionableTimeMs >= 0d) return row.ActionableTimeMs;
            return trial.SpecialState == ExperimentSpecialState.Covered ? -1d : row.StimulusTimeMs;
        }

        private static bool IsBeforeReveal(ExperimentInputEventRecord row)
        {
            return row.ActionableTimeMs < 0d
                || IsReason(row, ExperimentIgnoredReasons.CoveredNotRevealed)
                || IsReason(row, ExperimentIgnoredReasons.TrialNotActionable);
        }

        private static bool IsVehicleInput(ExperimentInput input)
        {
            return input == ExperimentInput.Sports
                || input == ExperimentInput.Compact
                || input == ExperimentInput.LightTruck;
        }

        private static ExperimentInput RawInput(ExperimentInputEventRecord row)
        {
            return row.Input != ExperimentInput.None ? row.Input : row.AcceptedInput;
        }

        private static ExperimentInput AcceptedInput(ExperimentInputEventRecord row)
        {
            return row.AcceptedInput != ExperimentInput.None ? row.AcceptedInput : row.Input;
        }

        private static double EventOrder(ExperimentInputEventRecord row)
        {
            // event_time_ms is monotonic only inside one app process. event_index is
            // restored from checkpoint and therefore remains the authoritative
            // session order across force-stop/relaunch epochs.
            return row.EventIndex > 0 ? row.EventIndex : row.EventTimeMs;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static void EnsureSingleVersionAndType(
            IEnumerable<ExperimentParticipantConditionMetric> rows,
            string experimentVersion,
            ExperimentType experimentType)
        {
            if (rows.Any(row => row.ExperimentType != experimentType
                || !string.Equals(row.ExperimentVersion, experimentVersion, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("Participant-condition output cannot mix experiment versions or types.");
            }
        }

        private static void EnsureSingleVersionAndType(
            IEnumerable<ExperimentParticipantDifferenceMetric> rows,
            string experimentVersion,
            ExperimentType experimentType)
        {
            if (rows.Any(row => row.ExperimentType != experimentType
                || !string.Equals(row.ExperimentVersion, experimentVersion, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("Participant-difference output cannot mix experiment versions or types.");
            }
        }

        private static void EnsureSingleVersionAndType(
            IEnumerable<ExperimentGroupMetricSummary> rows,
            string experimentVersion,
            ExperimentType experimentType)
        {
            if (rows.Any(row => row.ExperimentType != experimentType
                || !string.Equals(row.ExperimentVersion, experimentVersion, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("Group output cannot mix experiment versions or types.");
            }
        }

        internal static IEnumerable<string> ParticipantConditionLines(
            IEnumerable<ExperimentParticipantConditionMetric> rows)
        {
            yield return "schema_version,experiment_version,experiment_type,participant_id,session_id,condition,condition_order,trial_set_id,metric,metric_role,numerator,denominator,value";
            foreach (ExperimentParticipantConditionMetric row in rows
                .OrderBy(value => value.ParticipantId)
                .ThenBy(value => value.ConditionOrder)
                .ThenBy(value => value.Metric))
            {
                yield return CsvJoin(new[]
                {
                    row.SchemaVersion,
                    row.ExperimentVersion,
                    InputUIComparisonNaming.ExperimentTypeId(row.ExperimentType),
                    row.ParticipantId,
                    row.SessionId,
                    row.Condition,
                    row.ConditionOrder.ToString(CultureInfo.InvariantCulture),
                    row.TrialSetId,
                    row.Metric,
                    MetricRole(row.ExperimentType, row.Metric),
                    NumberOrBlank(row.Numerator),
                    NumberOrBlank(row.Denominator),
                    NumberOrBlank(row.Value),
                });
            }
        }

        internal static IEnumerable<string> ParticipantDifferenceLines(
            IEnumerable<ExperimentParticipantDifferenceMetric> rows)
        {
            yield return "schema_version,experiment_version,experiment_type,participant_id,session_id,reference_condition,comparison_condition,metric,metric_role,reference_value,comparison_value,difference,improved,worsened,unchanged";
            foreach (ExperimentParticipantDifferenceMetric row in rows
                .OrderBy(value => value.ParticipantId)
                .ThenBy(value => value.ComparisonCondition)
                .ThenBy(value => value.Metric))
            {
                yield return CsvJoin(new[]
                {
                    row.SchemaVersion,
                    row.ExperimentVersion,
                    InputUIComparisonNaming.ExperimentTypeId(row.ExperimentType),
                    row.ParticipantId,
                    row.SessionId,
                    row.ReferenceCondition,
                    row.ComparisonCondition,
                    row.Metric,
                    MetricRole(row.ExperimentType, row.Metric),
                    NumberOrBlank(row.ReferenceValue),
                    NumberOrBlank(row.ComparisonValue),
                    NumberOrBlank(row.Difference),
                    row.Improved ? "true" : "false",
                    row.Worsened ? "true" : "false",
                    row.Unchanged ? "true" : "false",
                });
            }
        }

        internal static IEnumerable<string> GroupSummaryLines(IEnumerable<ExperimentGroupMetricSummary> rows)
        {
            yield return "schema_version,experiment_version,experiment_type,reference_condition,condition,metric,metric_role,participant_count,participant_metric_median,participant_metric_min,participant_metric_max,improved_participant_count,worsened_participant_count,unchanged_participant_count";
            foreach (ExperimentGroupMetricSummary row in rows
                .OrderBy(value => value.Condition)
                .ThenBy(value => value.Metric))
            {
                yield return CsvJoin(new[]
                {
                    row.SchemaVersion,
                    row.ExperimentVersion,
                    InputUIComparisonNaming.ExperimentTypeId(row.ExperimentType),
                    row.ReferenceCondition,
                    row.Condition,
                    row.Metric,
                    MetricRole(row.ExperimentType, row.Metric),
                    row.ParticipantCount.ToString(CultureInfo.InvariantCulture),
                    NumberOrBlank(row.ParticipantMetricMedian),
                    NumberOrBlank(row.ParticipantMetricMin),
                    NumberOrBlank(row.ParticipantMetricMax),
                    row.ImprovedParticipantCount.ToString(CultureInfo.InvariantCulture),
                    row.WorsenedParticipantCount.ToString(CultureInfo.InvariantCulture),
                    row.UnchangedParticipantCount.ToString(CultureInfo.InvariantCulture),
                });
            }
        }

        private static void WriteUtf8BomAtomic(string path, IEnumerable<string> lines)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string partialPath = path + ".partial-" + Guid.NewGuid().ToString("N");
            try
            {
                File.WriteAllLines(partialPath, lines, new UTF8Encoding(true));
                if (!File.Exists(path))
                {
                    File.Move(partialPath, path);
                    return;
                }

                try
                {
                    File.Replace(partialPath, path, null, true);
                }
                catch (Exception exception) when (exception is PlatformNotSupportedException
                    || exception is NotSupportedException
                    || exception is IOException)
                {
                    File.Delete(path);
                    File.Move(partialPath, path);
                }
            }
            finally
            {
                if (File.Exists(partialPath)) File.Delete(partialPath);
            }
        }

        private static string CsvJoin(IEnumerable<string> values)
        {
            return string.Join(",", values.Select(CsvField));
        }

        private static string CsvField(string value)
        {
            value = value ?? string.Empty;
            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? value
                : "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string NumberOrBlank(double value)
        {
            return IsFinite(value) ? value.ToString("0.####", CultureInfo.InvariantCulture) : string.Empty;
        }

        private static string SanitizePathComponent(string value)
        {
            var builder = new StringBuilder();
            foreach (char character in value ?? string.Empty)
            {
                if (char.IsLetterOrDigit(character) || character == '-' || character == '_' || character == '.')
                {
                    builder.Append(character);
                }
            }
            return builder.Length == 0 ? "unknown" : builder.ToString();
        }

        public static double Median(IReadOnlyList<double> sortedValues)
        {
            if (sortedValues == null || sortedValues.Count == 0) return double.NaN;
            int middle = sortedValues.Count / 2;
            if ((sortedValues.Count & 1) == 1) return sortedValues[middle];
            return (sortedValues[middle - 1] + sortedValues[middle]) * 0.5d;
        }

        private static double Rate(int numerator, int denominator)
        {
            return denominator <= 0 ? double.NaN : (double)numerator / denominator;
        }
    }

    public sealed class InputUIExperimentCsvWriter
    {
        private static readonly UTF8Encoding Utf8Bom = new UTF8Encoding(true);

        private readonly string _participantId;
        private readonly string _sessionId;
        private readonly string _experimentVersion;
        private readonly ExperimentType _experimentType;
        private readonly string _referenceConditionId;

        public InputUIExperimentCsvWriter(string participantId, string sessionId)
            : this(participantId, sessionId, InputUIExperimentSchema.CurrentExperimentVersion, ExperimentType.Placement)
        {
        }

        public InputUIExperimentCsvWriter(
            string participantId,
            string sessionId,
            string experimentVersion,
            ExperimentType experimentType)
            : this(participantId, sessionId, experimentVersion, experimentType, null)
        {
        }

        public InputUIExperimentCsvWriter(
            string participantId,
            string sessionId,
            string experimentVersion,
            ExperimentType experimentType,
            string referenceConditionId)
        {
            _participantId = participantId;
            _sessionId = sessionId;
            _experimentVersion = string.IsNullOrWhiteSpace(experimentVersion)
                ? InputUIExperimentSchema.CurrentExperimentVersion
                : experimentVersion.Trim();
            _experimentType = experimentType;
            _referenceConditionId = string.IsNullOrWhiteSpace(referenceConditionId)
                ? string.Empty
                : referenceConditionId.Trim();
        }

        public string ExportDirectory
        {
            get
            {
                return Path.Combine(
                    Application.persistentDataPath,
                    "InputUIComparison",
                    "v2",
                    Sanitize(_experimentVersion),
                    Sanitize(_sessionId));
            }
        }

        public string Export(
            IReadOnlyList<ExperimentTrialRecord> trials,
            IReadOnlyList<ExperimentInputEventRecord> events,
            IReadOnlyList<ExperimentSurveyRecord> surveys)
        {
            Directory.CreateDirectory(ExportDirectory);
            string stem = Sanitize(_participantId) + "_" + Sanitize(_sessionId);
            WriteTrials(Path.Combine(ExportDirectory, stem + "_trials.csv"), trials);
            WriteEvents(Path.Combine(ExportDirectory, stem + "_input_events.csv"), events);
            WriteSurveys(Path.Combine(ExportDirectory, stem + "_survey.csv"), surveys);
            WriteSummary(Path.Combine(ExportDirectory, stem + "_summary.csv"), InputUIExperimentAnalytics.Summarize(trials, surveys));
            if (trials.Any(row => !string.IsNullOrWhiteSpace(row.Condition)
                && row.ExperimentType == _experimentType
                && string.Equals(row.ExperimentVersion, _experimentVersion, StringComparison.Ordinal)))
            {
                string referenceConditionId = ResolveReferenceCondition(trials);
                ExperimentAnalysisTables tables = InputUIExperimentAnalytics.BuildAnalysisTables(
                    trials,
                    events,
                    surveys,
                    _experimentVersion,
                    _experimentType,
                    referenceConditionId);
                WriteAllLinesAtomic(
                    Path.Combine(ExportDirectory, stem + "_summary_participant_condition.csv"),
                    InputUIExperimentAnalytics.ParticipantConditionLines(tables.ParticipantConditions));
                WriteAllLinesAtomic(
                    Path.Combine(ExportDirectory, stem + "_summary_participant_difference.csv"),
                    InputUIExperimentAnalytics.ParticipantDifferenceLines(tables.ParticipantDifferences));
                WriteAllLinesAtomic(
                    Path.Combine(ExportDirectory, "summary_participant_condition.csv"),
                    InputUIExperimentAnalytics.ParticipantConditionLines(tables.ParticipantConditions));
                WriteAllLinesAtomic(
                    Path.Combine(ExportDirectory, "summary_participant_difference.csv"),
                    InputUIExperimentAnalytics.ParticipantDifferenceLines(tables.ParticipantDifferences));
                WriteAllLinesAtomic(
                    Path.Combine(ExportDirectory, "summary_group.csv"),
                    InputUIExperimentAnalytics.GroupSummaryLines(tables.Groups));
            }
            return ExportDirectory;
        }

        private string ResolveReferenceCondition(IReadOnlyList<ExperimentTrialRecord> trials)
        {
            if (!string.IsNullOrEmpty(_referenceConditionId)) return _referenceConditionId;
            bool containsHumanRush = _experimentType == ExperimentType.RushPackage
                && trials.Any(row => row.ExperimentType == ExperimentType.RushPackage && !row.IsPilot);
            if (containsHumanRush)
            {
                throw new InvalidOperationException(
                    "Non-pilot Rush export requires the preregistered reference condition explicitly.");
            }
            return InputUIExperimentAnalytics.ReferenceCondition(_experimentType);
        }

        private static void WriteTrials(string path, IReadOnlyList<ExperimentTrialRecord> rows)
        {
            var lines = new List<string>(rows.Count + 1)
            {
                "schema_version,experiment_version,participant_id,session_id,experiment_type,condition,condition_order,trial_set_id,is_pilot,legacy_variant,legacy_variant_order,problem_set,seed,is_practice,trial_number,vehicle_type,special_state,speed_level,covered_reveal_progress,gap_after_seconds,effective_travel_seconds,pace_context,sequence_context,expected_input,actual_input,final_input,is_correct,eventual_correct,is_timeout,information_available_at_ms,time_to_first_raw_touch_ms,time_to_first_accepted_input_ms,time_to_correct_resolution_ms,correction_cost_ms,response_time_ms,actionable_response_time_ms,final_response_time_ms,special_state_missed,special_miss_applicable,button_position,wrong_button_type,distance_to_correct_button_px,distance_to_correct_button_normalized,layout_changed_before_input,input_count,retry_count,same_spot_repeat_count,same_frame_coalesced_count,cooldown_suppressed_count,invalid_button_input_count,post_error_consecutive_miss,timestamp"
            };
            lines.AddRange(rows.Select(row => Join(row.ToCsvFields())));
            WriteAllLinesAtomic(path, lines);
        }

        private static void WriteEvents(string path, IReadOnlyList<ExperimentInputEventRecord> rows)
        {
            var lines = new List<string>(rows.Count + 1)
            {
                "schema_version,experiment_version,participant_id,session_id,experiment_type,condition,condition_order,trial_set_id,trial_number,pointer_id,event_time_ms,screen_x_px,screen_y_px,screen_x_normalized,screen_y_normalized,screen_width,screen_height,safe_area_x,safe_area_y,safe_area_width,safe_area_height,ui_scale_factor,hit_ui_element,hit_button_id,was_button_visible,was_button_interactable,was_input_accepted,accepted_input,ignored_reason,expected_input,target_vehicle_id_at_touch,target_vehicle_id_at_accept,resolved_vehicle_id,target_changed_between_touch_and_accept,is_inside_current_repair_rect,is_inside_previous_repair_rect,previous_repair_rect_age_ms,is_pilot,is_practice,event_index,frame,raw_input,pointer_position,button_center_position,visually_enabled,disposition,stimulus_time_ms,actionable_time_ms,distance_to_expected_button_px,timestamp"
            };
            foreach (ExperimentInputEventRecord row in rows)
            {
                lines.Add(Join(new[]
                {
                    row.SchemaVersion,
                    row.ExperimentVersion,
                    row.ParticipantId,
                    row.SessionId,
                    InputUIComparisonNaming.ExperimentTypeId(row.ExperimentType),
                    row.Condition,
                    row.ConditionOrder.ToString(CultureInfo.InvariantCulture),
                    row.TrialSetId,
                    row.TrialNumber.ToString(CultureInfo.InvariantCulture),
                    row.PointerId.ToString(CultureInfo.InvariantCulture),
                    row.EventTimeMs.ToString("0.0", CultureInfo.InvariantCulture),
                    row.ScreenXPx.ToString("0.0", CultureInfo.InvariantCulture),
                    row.ScreenYPx.ToString("0.0", CultureInfo.InvariantCulture),
                    row.ScreenXNormalized.ToString("0.0000", CultureInfo.InvariantCulture),
                    row.ScreenYNormalized.ToString("0.0000", CultureInfo.InvariantCulture),
                    row.ScreenWidth.ToString(CultureInfo.InvariantCulture),
                    row.ScreenHeight.ToString(CultureInfo.InvariantCulture),
                    row.SafeAreaX.ToString("0.0", CultureInfo.InvariantCulture),
                    row.SafeAreaY.ToString("0.0", CultureInfo.InvariantCulture),
                    row.SafeAreaWidth.ToString("0.0", CultureInfo.InvariantCulture),
                    row.SafeAreaHeight.ToString("0.0", CultureInfo.InvariantCulture),
                    row.UiScaleFactor.ToString("0.0000", CultureInfo.InvariantCulture),
                    row.HitUiElement,
                    row.HitButtonId,
                    row.WasButtonVisible ? "true" : "false",
                    row.WasButtonInteractable ? "true" : "false",
                    row.WasInputAccepted ? "true" : "false",
                    row.AcceptedInput.ToString(),
                    string.IsNullOrEmpty(row.IgnoredReason) ? ExperimentIgnoredReasons.None : row.IgnoredReason,
                    row.ExpectedInput.ToString(),
                    row.TargetVehicleIdAtTouch,
                    row.TargetVehicleIdAtAccept,
                    row.ResolvedVehicleId,
                    row.TargetChangedBetweenTouchAndAccept ? "true" : "false",
                    row.IsInsideCurrentRepairRect ? "true" : "false",
                    row.IsInsidePreviousRepairRect ? "true" : "false",
                    row.PreviousRepairRectAgeMs < 0d ? string.Empty : row.PreviousRepairRectAgeMs.ToString("0.0", CultureInfo.InvariantCulture),
                    row.IsPilot ? "true" : "false",
                    row.IsPractice ? "true" : "false",
                    row.EventIndex.ToString(CultureInfo.InvariantCulture),
                    row.Frame.ToString(CultureInfo.InvariantCulture),
                    row.Input.ToString(),
                    row.PointerPosition,
                    row.ButtonCenterPosition,
                    row.VisuallyEnabled ? "true" : "false",
                    row.Disposition,
                    row.StimulusTimeMs.ToString("0.0", CultureInfo.InvariantCulture),
                    row.ActionableTimeMs < 0d ? string.Empty : row.ActionableTimeMs.ToString("0.0", CultureInfo.InvariantCulture),
                    row.DistanceToExpectedButtonPx.ToString("0.0", CultureInfo.InvariantCulture),
                    row.Timestamp,
                }));
            }

            WriteAllLinesAtomic(path, lines);
        }

        private static void WriteSurveys(string path, IReadOnlyList<ExperimentSurveyRecord> rows)
        {
            var lines = new List<string>(rows.Count + 1)
            {
                "schema_version,experiment_version,participant_id,session_id,experiment_type,condition,condition_order,trial_set_id,is_pilot,q1_target_clarity_rating,q1_target_clarity_short_answer,q2_searched_for_button_rating,q2_searched_for_button_short_answer,q3_judged_repair_need_from_vehicle_rating,q3_judged_repair_need_from_vehicle_short_answer,q4_ui_state_revealed_answer_rating,q4_ui_state_revealed_answer_short_answer,q5_can_explain_mistake_rating,q5_can_explain_mistake_short_answer,q6_meaning_changed_rating,q6_meaning_changed_short_answer,q7_most_confusing_moment_rating,q7_most_confusing_moment_short_answer,q8_broken_too_easy_rating,q8_broken_too_easy_short_answer,analyst_explanation_code,timestamp,legacy_variant,legacy_problem_set,legacy_noticed_special_state"
            };
            foreach (ExperimentSurveyRecord row in rows)
            {
                lines.Add(Join(new[]
                {
                    row.SchemaVersion,
                    row.ExperimentVersion,
                    row.ParticipantId,
                    row.SessionId,
                    InputUIComparisonNaming.ExperimentTypeId(row.ExperimentType),
                    row.Condition,
                    row.ConditionOrder.ToString(CultureInfo.InvariantCulture),
                    row.TrialSetId,
                    row.IsPilot ? "true" : "false",
                    row.TargetClarity.ToString(CultureInfo.InvariantCulture),
                    row.TargetClarityExplanation,
                    row.SearchedForButton.ToString(CultureInfo.InvariantCulture),
                    row.ButtonSearchExplanation,
                    row.JudgedRepairNeedFromVehicle.ToString(CultureInfo.InvariantCulture),
                    row.RepairJudgmentExplanation,
                    row.UiStateRevealedAnswer.ToString(CultureInfo.InvariantCulture),
                    row.UiStateExplanation,
                    row.CanExplainMistake.ToString(CultureInfo.InvariantCulture),
                    row.MistakeExplanation,
                    row.MeaningChanged.ToString(CultureInfo.InvariantCulture),
                    row.MeaningChangedExplanation,
                    row.MostConfusingMomentRating.ToString(CultureInfo.InvariantCulture),
                    row.MostConfusingMoment,
                    row.BrokenTooEasy.ToString(CultureInfo.InvariantCulture),
                    row.BrokenDifficultyExplanation,
                    row.AnalystExplanationCode ?? string.Empty,
                    row.Timestamp,
                    InputUIComparisonNaming.VariantId(row.Variant),
                    row.ProblemSet.ToString(CultureInfo.InvariantCulture),
                    row.NoticedSpecialState.ToString(CultureInfo.InvariantCulture),
                }));
            }

            WriteAllLinesAtomic(path, lines);
        }

        private void WriteSummary(string path, IReadOnlyList<ExperimentVariantSummary> rows)
        {
            var lines = new List<string>(rows.Count + 1)
            {
                "participant_id,session_id,variant,trials,wrong_input_trials,wrong_input_rate,special_trials,special_misses,special_miss_rate,median_correct_first_response_ms,max_correct_first_response_ms,median_all_first_input_ms,median_final_resolution_ms,timeout_rate,invalid_attempts_per_trial,consecutive_miss_rate,high_speed_wrong_input_rate,q2_searched_for_button,q4_can_explain_mistake"
            };
            foreach (ExperimentVariantSummary row in rows)
            {
                lines.Add(Join(new[]
                {
                    _participantId,
                    _sessionId,
                    InputUIComparisonNaming.VariantId(row.Variant), row.Trials.ToString(CultureInfo.InvariantCulture),
                    row.Errors.ToString(CultureInfo.InvariantCulture), row.ErrorRate.ToString("0.0000", CultureInfo.InvariantCulture),
                    row.SpecialTrials.ToString(CultureInfo.InvariantCulture), row.SpecialMisses.ToString(CultureInfo.InvariantCulture),
                    NumberOrBlank(row.SpecialMissRate, "0.0000"),
                    NumberOrBlank(row.MedianResponseMs, "0.0"), NumberOrBlank(row.MaxResponseMs, "0.0"),
                    NumberOrBlank(row.MedianAllFirstInputMs, "0.0"), NumberOrBlank(row.MedianFinalResolutionMs, "0.0"),
                    NumberOrBlank(row.TimeoutRate, "0.0000"), NumberOrBlank(row.InvalidAttemptRate, "0.0000"),
                    NumberOrBlank(row.ConsecutiveMissRate, "0.0000"), NumberOrBlank(row.HighSpeedErrorRate, "0.0000"),
                    row.SearchedForButtonRating.ToString(CultureInfo.InvariantCulture), row.CanExplainMistakeRating.ToString(CultureInfo.InvariantCulture),
                }));
            }

            WriteAllLinesAtomic(path, lines);
        }

        private static string Join(IEnumerable<string> fields)
        {
            return string.Join(",", fields.Select(Csv));
        }

        private static string NumberOrBlank(double value, string format)
        {
            return double.IsNaN(value) || double.IsInfinity(value)
                ? string.Empty
                : value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static void WriteAllLinesAtomic(string path, IEnumerable<string> lines)
        {
            WriteUtf8BomAtomicPortable(path, lines);
        }

        private static void WriteUtf8BomAtomicPortable(string path, IEnumerable<string> lines)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string partialPath = path + ".partial-" + Guid.NewGuid().ToString("N");
            string backupPath = path + ".backup";
            try
            {
                File.WriteAllLines(partialPath, lines, Utf8Bom);
                if (!File.Exists(path))
                {
                    File.Move(partialPath, path);
                    return;
                }

                try
                {
                    File.Replace(partialPath, path, null, true);
                }
                catch (Exception exception) when (exception is PlatformNotSupportedException
                    || exception is NotSupportedException
                    || exception is IOException)
                {
                    if (File.Exists(backupPath)) File.Delete(backupPath);
                    File.Move(path, backupPath);
                    try
                    {
                        File.Move(partialPath, path);
                        File.Delete(backupPath);
                    }
                    catch
                    {
                        if (!File.Exists(path) && File.Exists(backupPath)) File.Move(backupPath, path);
                        throw;
                    }
                }
            }
            finally
            {
                if (File.Exists(partialPath)) File.Delete(partialPath);
            }
        }

        private static string Csv(string value)
        {
            value = value ?? string.Empty;
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string Sanitize(string value)
        {
            var builder = new StringBuilder();
            foreach (char c in value ?? string.Empty)
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.') builder.Append(c);
            }

            return builder.Length == 0 ? "anonymous" : builder.ToString();
        }
    }
}
