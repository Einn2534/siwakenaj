using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Siwakenja.Experiments.InputUIComparison.Editor;
using UnityEditor;
using UnityEngine;

namespace Siwakenja.Experiments.InputUIComparison.Tests
{
    public sealed class InputUIComparisonEditModeTests
    {
        [Test]
        public void IsolatedSceneAssetExists()
        {
            SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                "Assets/Experiments/InputUIComparison/Scenes/InputUIComparison.unity");
            Assert.That(scene, Is.Not.Null);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void MeasuredSet_IsFullyStratified(int setId)
        {
            List<ExperimentTrial> trials = InputUITrialPlan.BuildMeasuredSet(setId, InputUITrialPlan.DefaultSeed);
            Assert.That(trials, Has.Count.EqualTo(24));
            Assert.That(trials.Select(trial => TrialKey(trial)).Distinct().Count(), Is.EqualTo(24));

            foreach (ExperimentVehicle vehicle in Enum.GetValues(typeof(ExperimentVehicle)))
            {
                Assert.That(trials.Count(trial => trial.Vehicle == vehicle), Is.EqualTo(8), vehicle.ToString());
            }

            foreach (ExperimentSpecialState state in Enum.GetValues(typeof(ExperimentSpecialState)))
            {
                Assert.That(trials.Count(trial => trial.SpecialState == state), Is.EqualTo(6), state.ToString());
                foreach (ExperimentSpeed speed in Enum.GetValues(typeof(ExperimentSpeed)))
                {
                    Assert.That(
                        trials.Count(trial => trial.SpecialState == state && trial.Speed == speed),
                        Is.EqualTo(2),
                        state + " / " + speed);
                }
            }

            foreach (ExperimentSpeed speed in Enum.GetValues(typeof(ExperimentSpeed)))
            {
                Assert.That(trials.Count(trial => trial.Speed == speed), Is.EqualTo(8), speed.ToString());
            }

            Assert.That(
                Enumerable.Range(1, trials.Count - 1).Any(index => trials[index].Vehicle == trials[index - 1].Vehicle),
                Is.True);
            ExperimentTrial[] rush = trials.Where(trial => trial.PaceContext == "short_gap_0.22s").ToArray();
            Assert.That(rush, Has.Length.EqualTo(3));
            Assert.That(rush.Select(trial => trial.Vehicle), Is.EqualTo(new[]
            {
                ExperimentVehicle.Sports,
                ExperimentVehicle.Compact,
                ExperimentVehicle.LightTruck,
            }));
        }

        [Test]
        public void MeasuredSet_FixedSeedIsRepeatable()
        {
            List<ExperimentTrial> first = InputUITrialPlan.BuildMeasuredSet(2, 123456);
            List<ExperimentTrial> second = InputUITrialPlan.BuildMeasuredSet(2, 123456);
            Assert.That(first.Select(TrialKey), Is.EqualTo(second.Select(TrialKey)));

            List<ExperimentTrial> different = InputUITrialPlan.BuildMeasuredSet(2, 123457);
            Assert.That(first.Select(TrialKey), Is.Not.EqualTo(different.Select(TrialKey)));
        }

        [Test]
        public void MeasuredSet_SequenceConstraintsHoldAcrossManySeeds()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                for (int setId = 0; setId < 3; setId++)
                {
                    List<ExperimentTrial> trials = InputUITrialPlan.BuildMeasuredSet(setId, seed);
                    Assert.That(trials.Select(TrialKey).Distinct().Count(), Is.EqualTo(24), "seed/set " + seed + "/" + setId);
                    Assert.That(
                        Enumerable.Range(1, 23).Any(index => trials[index].Vehicle == trials[index - 1].Vehicle),
                        Is.True,
                        "repeat seed/set " + seed + "/" + setId);
                    ExperimentTrial[] rush = trials.Where(trial => trial.PaceContext == "short_gap_0.22s").ToArray();
                    Assert.That(rush.Select(trial => trial.Vehicle), Is.EqualTo(new[]
                    {
                        ExperimentVehicle.Sports,
                        ExperimentVehicle.Compact,
                        ExperimentVehicle.LightTruck,
                    }), "rush seed/set " + seed + "/" + setId);
                }
            }
        }

        [Test]
        public void Counterbalance_P01ThroughP06CoversAllOrdersAndSets()
        {
            var uniqueOrders = new HashSet<string>();
            var positionCounts = new Dictionary<string, int>();
            var setCounts = new Dictionary<string, int>();
            for (int participant = 1; participant <= 6; participant++)
            {
                string id = "P" + participant.ToString("00");
                ExperimentVariant[] order = InputUITrialPlan.GetCounterbalancedOrder(id, 99);
                uniqueOrders.Add(string.Join(">", order));
                for (int position = 0; position < order.Length; position++)
                {
                    string positionKey = order[position] + ":" + position;
                    positionCounts[positionKey] = positionCounts.TryGetValue(positionKey, out int value) ? value + 1 : 1;
                    int setId = InputUITrialPlan.GetProblemSetForVariant(id, 99, order[position]);
                    string setKey = order[position] + ":" + setId;
                    setCounts[setKey] = setCounts.TryGetValue(setKey, out value) ? value + 1 : 1;
                }
            }

            Assert.That(uniqueOrders, Has.Count.EqualTo(6));
            Assert.That(positionCounts.Values, Has.All.EqualTo(2));
            Assert.That(setCounts.Values, Has.All.EqualTo(2));
        }

        [Test]
        public void V2PlacementSchedule_AlternatesOrderAndBalancesTrialSets()
        {
            var conditionSetCounts = new Dictionary<string, int>();
            for (int participant = 1; participant <= 6; participant++)
            {
                string id = "P" + participant.ToString("00");
                ExperimentSchedule schedule = InputUITrialPlan.BuildSchedule(id, ExperimentType.Placement);
                string[] expected = (participant & 1) == 1
                    ? new[] { ExperimentConditionIds.PlacementUpperFixed, ExperimentConditionIds.PlacementBottomFixed }
                    : new[] { ExperimentConditionIds.PlacementBottomFixed, ExperimentConditionIds.PlacementUpperFixed };
                Assert.That(schedule.Assignments.Select(row => row.Condition), Is.EqualTo(expected));
                Assert.That(schedule.Assignments.Select(row => row.TrialSetId).Distinct().Count(), Is.EqualTo(2));
                foreach (ExperimentConditionAssignment assignment in schedule.Assignments)
                {
                    string key = assignment.Condition + ":" + assignment.TrialSetId;
                    conditionSetCounts[key] = conditionSetCounts.TryGetValue(key, out int count) ? count + 1 : 1;
                    Assert.That(assignment.RepairAssistMode, Is.EqualTo(RepairAssistMode.AlwaysActive));
                    Assert.That(assignment.VehicleButtonLayoutMode, Is.EqualTo(VehicleButtonLayoutMode.StandardThree));
                }
            }

            Assert.That(conditionSetCounts, Has.Count.EqualTo(6));
            Assert.That(conditionSetCounts.Values, Has.All.EqualTo(2));
        }

        [Test]
        public void V2AssistSchedule_CoversAllSixOrdersAndConditionSetPairs()
        {
            var orders = new HashSet<string>();
            var positionCounts = new Dictionary<string, int>();
            var conditionSetCounts = new Dictionary<string, int>();
            for (int participant = 1; participant <= 6; participant++)
            {
                ExperimentSchedule schedule = InputUITrialPlan.BuildSchedule(
                    "P" + participant.ToString("00"),
                    ExperimentType.StateAssist,
                    InputUIExperimentSchema.CurrentExperimentVersion,
                    RepairAnchorMode.BottomFixed);
                orders.Add(string.Join(">", schedule.Assignments.Select(row => row.Condition)));
                Assert.That(schedule.Assignments.Select(row => row.TrialSetId).Distinct().Count(), Is.EqualTo(3));
                for (int position = 0; position < schedule.Assignments.Count; position++)
                {
                    ExperimentConditionAssignment assignment = schedule.Assignments[position];
                    Assert.That(assignment.RepairAnchorMode, Is.EqualTo(RepairAnchorMode.BottomFixed));
                    string positionKey = assignment.Condition + ":" + position;
                    positionCounts[positionKey] = positionCounts.TryGetValue(positionKey, out int count) ? count + 1 : 1;
                    string setKey = assignment.Condition + ":" + assignment.TrialSetId;
                    conditionSetCounts[setKey] = conditionSetCounts.TryGetValue(setKey, out count) ? count + 1 : 1;
                }
            }

            Assert.That(orders, Has.Count.EqualTo(6));
            Assert.That(positionCounts, Has.Count.EqualTo(9));
            Assert.That(positionCounts.Values, Has.All.EqualTo(2));
            Assert.That(conditionSetCounts, Has.Count.EqualTo(9));
            Assert.That(conditionSetCounts.Values, Has.All.EqualTo(2));
        }

        [Test]
        public void V2Schedule_P00IsPermittedAndMarkedPilot()
        {
            ExperimentSchedule schedule = InputUITrialPlan.BuildSchedule("p00", ExperimentType.RushPackage);
            Assert.That(schedule.ParticipantId, Is.EqualTo("P00"));
            Assert.That(schedule.IsPilot, Is.True);
            Assert.That(schedule.Assignments, Has.Count.EqualTo(3));
            Assert.That(schedule.ProtocolId, Is.EqualTo(InputUITrialPlan.P00RushProtocolId));
            Assert.That(schedule.ReferenceConditionId, Is.EqualTo(ExperimentConditionIds.RushCurrentEquivalent));
            Assert.That(schedule.ConditionDefinitions, Has.Count.EqualTo(3));
        }

        [Test]
        public void V21RushSchedule_NonPilotFailsClosedWithoutValidPreregistration()
        {
            Assert.Throws<InvalidOperationException>(() =>
                InputUITrialPlan.BuildSchedule("P01", ExperimentType.RushPackage));

            HumanRushRegistration valid = RegisteredRush();
            var invalid = new HumanRushRegistration(
                valid.ProtocolId,
                valid.ReferenceConditionId,
                valid.Candidates[0]);
            Assert.Throws<InvalidOperationException>(() =>
                InputUITrialPlan.BuildSchedule(
                    "P01",
                    ExperimentType.RushPackage,
                    InputUIExperimentSchema.CurrentExperimentVersion,
                    RepairAnchorMode.UpperFixed,
                    invalid));
        }

        [Test]
        public void V21RushSchedule_UsesExactlyTwoRegisteredCandidatesInAbBaOrder()
        {
            HumanRushRegistration registration = RegisteredRush();
            ExperimentSchedule odd = InputUITrialPlan.BuildSchedule(
                "P01",
                ExperimentType.RushPackage,
                InputUIExperimentSchema.CurrentExperimentVersion,
                RepairAnchorMode.UpperFixed,
                registration);
            ExperimentSchedule even = InputUITrialPlan.BuildSchedule(
                "P02",
                ExperimentType.RushPackage,
                InputUIExperimentSchema.CurrentExperimentVersion,
                RepairAnchorMode.UpperFixed,
                registration);

            Assert.That(odd.IsPilot, Is.False);
            Assert.That(odd.Assignments.Select(row => row.Condition), Is.EqualTo(new[]
            {
                registration.Candidates[0].ConditionId,
                registration.Candidates[1].ConditionId,
            }));
            Assert.That(even.Assignments.Select(row => row.Condition), Is.EqualTo(new[]
            {
                registration.Candidates[1].ConditionId,
                registration.Candidates[0].ConditionId,
            }));
            Assert.That(odd.Assignments, Has.Count.EqualTo(2));
            Assert.That(even.Assignments, Has.Count.EqualTo(2));
            Assert.That(odd.ProtocolId, Is.EqualTo(registration.ProtocolId));
            Assert.That(odd.ReferenceConditionId, Is.EqualTo(registration.ReferenceConditionId));
            Assert.That(odd.ConditionDefinitions.Select(row => row.ConditionId),
                Is.EqualTo(registration.Candidates.Select(row => row.ConditionId)));
        }

        [Test]
        public void V21RushManifest_SnapshotsRegistrationAndRejectsImplicitHumanContract()
        {
            HumanRushRegistration registration = RegisteredRush();
            ExperimentSchedule schedule = InputUITrialPlan.BuildSchedule(
                "P01",
                ExperimentType.RushPackage,
                InputUIExperimentSchema.CurrentExperimentVersion,
                RepairAnchorMode.UpperFixed,
                registration);
            ExperimentSessionManifest manifest = SessionPersistence.CreateManifestSnapshot(
                "P01",
                "S-RUSH-MANIFEST",
                ExperimentType.RushPackage,
                schedule.Assignments,
                RepairAnchorMode.UpperFixed,
                RepairAssistMode.AlwaysActive,
                VehicleButtonLayoutMode.StandardThree,
                false,
                experimentVersion: schedule.ExperimentVersion,
                rushProtocolId: schedule.ProtocolId,
                referenceConditionId: schedule.ReferenceConditionId,
                conditionDefinitions: schedule.ConditionDefinitions);

            Assert.That(manifest.RushProtocolId, Is.EqualTo(registration.ProtocolId));
            Assert.That(manifest.ReferenceConditionId, Is.EqualTo(registration.ReferenceConditionId));
            Assert.That(manifest.ConditionDefinitions.Select(row => row.ConditionId),
                Is.EqualTo(registration.Candidates.Select(row => row.ConditionId)));
            Assert.Throws<InvalidOperationException>(() => SessionPersistence.CreateManifestSnapshot(
                "P01",
                "S-RUSH-IMPLICIT",
                ExperimentType.RushPackage,
                schedule.Assignments,
                RepairAnchorMode.UpperFixed,
                RepairAssistMode.AlwaysActive,
                VehicleButtonLayoutMode.StandardThree,
                false));
        }

        [Test]
        public void StableConditionSeed_UsesEveryRequiredComponentAndNeverGetHashCode()
        {
            const string version = InputUIExperimentSchema.CurrentExperimentVersion;
            int seed = InputUITrialPlan.StableConditionSeed(
                "P01", ExperimentType.StateAssist, ExperimentConditionIds.AssistAlwaysActive,
                InputUITrialPlan.Set01, version);
            Assert.That(InputUITrialPlan.StableConditionSeed(
                " p01 ", ExperimentType.StateAssist, ExperimentConditionIds.AssistAlwaysActive,
                InputUITrialPlan.Set01, version), Is.EqualTo(seed));
            Assert.That(new[]
            {
                InputUITrialPlan.StableConditionSeed("P02", ExperimentType.StateAssist, ExperimentConditionIds.AssistAlwaysActive, InputUITrialPlan.Set01, version),
                InputUITrialPlan.StableConditionSeed("P01", ExperimentType.Placement, ExperimentConditionIds.AssistAlwaysActive, InputUITrialPlan.Set01, version),
                InputUITrialPlan.StableConditionSeed("P01", ExperimentType.StateAssist, ExperimentConditionIds.AssistBrokenOnly, InputUITrialPlan.Set01, version),
                InputUITrialPlan.StableConditionSeed("P01", ExperimentType.StateAssist, ExperimentConditionIds.AssistAlwaysActive, InputUITrialPlan.Set02, version),
                InputUITrialPlan.StableConditionSeed("P01", ExperimentType.StateAssist, ExperimentConditionIds.AssistAlwaysActive, InputUITrialPlan.Set01, "2.1.1"),
            }, Has.None.EqualTo(seed));
            string source = File.ReadAllText("Assets/Experiments/InputUIComparison/Scripts/InputUITrialPlan.cs");
            Assert.That(source, Does.Not.Contain("GetHashCode("));
        }

        [Test]
        public void V2TrialSets_AreEquivalentNonIdenticalAndMeetSequenceConstraints()
        {
            var compositions = new HashSet<string>();
            foreach (string setId in InputUITrialPlan.TrialSetIds)
            {
                List<ExperimentTrial> trials = InputUITrialPlan.BuildMeasuredSet(setId, 918273);
                Assert.That(trials, Has.Count.EqualTo(24));
                Assert.That(trials.Select(row => row.TrialSetId), Has.All.EqualTo(setId));
                Assert.That(trials.Count(row => row.SpecialState == ExperimentSpecialState.Broken), Is.EqualTo(6));
                Assert.That(trials.Count(row => row.SpecialState == ExperimentSpecialState.Covered), Is.EqualTo(6));
                Assert.That(trials.Count(row => row.SpecialState == ExperimentSpecialState.Express), Is.EqualTo(6));
                foreach (ExperimentSpeed speed in Enum.GetValues(typeof(ExperimentSpeed)))
                {
                    Assert.That(trials.Count(row => row.Speed == speed), Is.EqualTo(8));
                    foreach (ExperimentSpecialState state in Enum.GetValues(typeof(ExperimentSpecialState)))
                    {
                        Assert.That(trials.Count(row => row.Speed == speed && row.SpecialState == state), Is.EqualTo(2));
                    }
                }
                Assert.That(trials.Where(row => row.SpecialState == ExperimentSpecialState.Covered)
                    .GroupBy(row => row.CoveredRevealProgress).Select(rows => rows.Count()), Has.All.EqualTo(2));
                Assert.That(AdjacentCount(trials, (left, right) => left.Vehicle == right.Vehicle), Is.EqualTo(4));
                Assert.That(AdjacentCount(trials, (left, right) => left.SpecialState == ExperimentSpecialState.Broken
                    && right.SpecialState == ExperimentSpecialState.Normal), Is.EqualTo(2));
                Assert.That(AdjacentCount(trials, (left, right) => left.SpecialState == ExperimentSpecialState.Normal
                    && right.SpecialState == ExperimentSpecialState.Broken), Is.EqualTo(2));
                ExperimentTrial[] shortGap = trials.Where(row => row.PaceContext == "short_gap_0.22s").ToArray();
                Assert.That(shortGap.Select(row => row.Vehicle), Is.EqualTo(new[]
                {
                    ExperimentVehicle.Sports,
                    ExperimentVehicle.Compact,
                    ExperimentVehicle.LightTruck,
                }));
                compositions.Add(string.Join(",", trials.Select(TrialKey).OrderBy(value => value)));
            }

            Assert.That(compositions, Has.Count.EqualTo(3), "Sets must differ in composition, not only order.");
        }

        [Test]
        public void Rules_MatchAuditedSpecialStateContract()
        {
            InputEvaluation brokenRepair = InputUIComparisonRules.Evaluate(
                ExperimentVehicle.Sports, ExperimentSpecialState.Broken, true, ExperimentInput.Repair);
            Assert.That(brokenRepair.IsCorrect, Is.True);

            InputEvaluation brokenLane = InputUIComparisonRules.Evaluate(
                ExperimentVehicle.Sports, ExperimentSpecialState.Broken, true, ExperimentInput.Sports);
            Assert.That(brokenLane.IsCorrect, Is.False);
            Assert.That(brokenLane.IsSpecialMiss, Is.True);

            InputEvaluation coveredEarly = InputUIComparisonRules.Evaluate(
                ExperimentVehicle.Compact, ExperimentSpecialState.Covered, false, ExperimentInput.Compact);
            Assert.That(coveredEarly.IsCorrect, Is.False);
            Assert.That(coveredEarly.IsSpecialMiss, Is.True);

            InputEvaluation coveredAfterReveal = InputUIComparisonRules.Evaluate(
                ExperimentVehicle.Compact, ExperimentSpecialState.Covered, true, ExperimentInput.Compact);
            Assert.That(coveredAfterReveal.IsCorrect, Is.True);

            Assert.That(
                InputUIComparisonRules.TravelSeconds(ExperimentSpeed.High, ExperimentSpecialState.Express),
                Is.LessThan(InputUIComparisonRules.TravelSeconds(ExperimentSpeed.High, ExperimentSpecialState.Normal)));
            Assert.That(
                InputUIComparisonRules.TravelSeconds(ExperimentSpeed.High, ExperimentSpecialState.Broken),
                Is.GreaterThan(InputUIComparisonRules.TravelSeconds(ExperimentSpeed.High, ExperimentSpecialState.Normal)));
        }

        [Test]
        public void Analytics_SeparatesWrongInputFromTimeoutAndUsesCorrectFirstRt()
        {
            var rows = new[]
            {
                Trial(true, false, 100d),
                Trial(false, false, 20d),
                Trial(false, true, -1d),
            };
            ExperimentVariantSummary summary = InputUIExperimentAnalytics.Summarize(rows, Array.Empty<ExperimentSurveyRecord>()).Single();
            Assert.That(summary.Errors, Is.EqualTo(1));
            Assert.That(summary.Timeouts, Is.EqualTo(1));
            Assert.That(summary.ErrorRate, Is.EqualTo(1d / 3d).Within(0.0001));
            Assert.That(summary.MedianResponseMs, Is.EqualTo(100d));
            Assert.That(summary.MedianAllFirstInputMs, Is.EqualTo(60d));
        }

        [Test]
        public void V2Analytics_SeparatesRawAcceptedBrokenCoveredAndExpressMetrics()
        {
            const string version = InputUIExperimentSchema.CurrentExperimentVersion;
            const string condition = "Assist_AlwaysActive";
            var trials = new[]
            {
                V2Trial(1, ExperimentSpecialState.Normal, ExperimentInput.Sports, false, true, true, ExperimentInput.Sports, 100d),
                V2Trial(2, ExperimentSpecialState.Broken, ExperimentInput.Repair, true, false, false, ExperimentInput.Sports, 200d),
                V2Trial(3, ExperimentSpecialState.Covered, ExperimentInput.Compact, true, false, false, ExperimentInput.Sports, 150d),
                V2Trial(4, ExperimentSpecialState.Express, ExperimentInput.LightTruck, false, true, true, ExperimentInput.LightTruck, 120d),
                V2Trial(99, ExperimentSpecialState.Normal, ExperimentInput.Sports, false, true, true, ExperimentInput.Sports, 50d,
                    participant: "P00", isPilot: true),
                V2Trial(1, ExperimentSpecialState.Normal, ExperimentInput.Sports, false, true, true, ExperimentInput.Sports, 50d,
                    participant: "P02", experimentVersion: "3.0.0"),
            };
            var events = new List<ExperimentInputEventRecord>
            {
                V2Event(1, 1, ExperimentInput.None, false, ExperimentIgnoredReasons.HiddenOrEmpty, -1d),
                V2Event(1, 2, ExperimentInput.Sports, true, ExperimentIgnoredReasons.None, 100d),
                V2Event(2, 3, ExperimentInput.Sports, true, ExperimentIgnoredReasons.None, 200d),
                V2Event(3, 4, ExperimentInput.Repair, true, ExperimentIgnoredReasons.CoveredNotRevealed, -1d),
                V2Event(3, 5, ExperimentInput.Sports, true, ExperimentIgnoredReasons.None, 150d, ExperimentInput.Compact),
                V2Event(4, 6, ExperimentInput.LightTruck, true, ExperimentIgnoredReasons.None, 120d),
                V2Event(1, 7, ExperimentInput.Repair, false, ExperimentIgnoredReasons.Disabled, 210d),
                V2Event(1, 8, ExperimentInput.Sports, false, ExperimentIgnoredReasons.Cooldown, 220d),
                V2Event(1, 9, ExperimentInput.Sports, false, ExperimentIgnoredReasons.SameFrameOverwritten, 230d),
                V2Event(1, 10, ExperimentInput.Repair, false, ExperimentIgnoredReasons.StaleTarget, 240d, insidePrevious: true),
                V2Event(1, 11, ExperimentInput.Repair, false, ExperimentIgnoredReasons.HiddenOrEmpty, 250d),
                V2Event(1, 12, ExperimentInput.Repair, false, ExperimentIgnoredReasons.Disabled, 260d),
            };
            trials[0].TimeToFirstRawTouchMs = 20d;
            trials[0].TimeToFirstAcceptedInputMs = 100d;
            trials[0].TimeToCorrectResolutionMs = 100d;
            trials[0].CorrectionCostMs = 80d;
            trials[1].TimeToFirstRawTouchMs = 200d;
            trials[1].TimeToFirstAcceptedInputMs = 200d;
            trials[2].InformationAvailableAtMs = 500d;
            trials[2].TimeToFirstRawTouchMs = 150d;
            trials[2].TimeToFirstAcceptedInputMs = 150d;
            trials[3].TimeToFirstRawTouchMs = 120d;
            trials[3].TimeToFirstAcceptedInputMs = 120d;
            trials[3].TimeToCorrectResolutionMs = 120d;
            trials[3].CorrectionCostMs = 0d;
            var surveys = new[]
            {
                new ExperimentSurveyRecord
                {
                    ExperimentVersion = version,
                    ExperimentType = ExperimentType.StateAssist,
                    ParticipantId = "P01",
                    SessionId = "S01",
                    Condition = condition,
                    TrialSetId = InputUITrialPlan.Set01,
                    TargetClarity = 4,
                    SearchedForButton = 2,
                    JudgedRepairNeedFromVehicle = 4,
                    UiStateRevealedAnswer = 5,
                    CanExplainMistake = 4,
                    MeaningChanged = 2,
                    MostConfusingMomentRating = 3,
                    BrokenTooEasy = 5,
                },
            };

            List<ExperimentParticipantConditionMetric> metrics = InputUIExperimentAnalytics.SummarizeParticipantConditions(
                trials, events, surveys, version, ExperimentType.StateAssist);
            Assert.That(metrics.Select(row => row.ParticipantId).Distinct(), Is.EqualTo(new[] { "P01" }));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.FirstRawTouchErrorRate), Is.EqualTo(3d / 4d).Within(0.0001));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.FirstAcceptedInputErrorRate), Is.EqualTo(0.5d).Within(0.0001));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.DisabledTapEventRate), Is.EqualTo(2d / 12d).Within(0.0001));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.TrialWithDisabledTapRate), Is.EqualTo(0.25d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.TrialWithDisabledRepairTapRate), Is.EqualTo(0.25d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.EmptyAreaTapEventRate), Is.EqualTo(1d / 12d).Within(0.0001));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.TrialWithEmptyAreaTapRate), Is.EqualTo(0.25d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.HiddenRepairAreaTapEventRate), Is.EqualTo(1d / 12d).Within(0.0001));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.TrialWithHiddenRepairAreaTapRate), Is.EqualTo(0.25d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.HiddenRepairAreaTapRatePerTrial), Is.EqualTo(0.25d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.GenericEmptyAreaTapRatePerTrial), Is.EqualTo(0.25d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.StaleRepairAreaTapEventRate), Is.EqualTo(1d / 12d).Within(0.0001));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.StaleRepairAreaTapRatePerTrial), Is.EqualTo(0.25d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.TrialWithStaleRepairAreaTapRate), Is.EqualTo(0.25d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.CooldownTapEventRate), Is.EqualTo(1d / 12d).Within(0.0001));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.TrialWithCooldownTapRate), Is.EqualTo(0.25d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.SameFrameOverwrittenTapEventRate), Is.EqualTo(1d / 12d).Within(0.0001));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.TrialWithSameFrameOverwrittenTapRate), Is.EqualTo(0.25d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.TimeToFirstRawTouchMs), Is.EqualTo(135d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.TimeToFirstAcceptedInputMs), Is.EqualTo(135d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.TimeToCorrectResolutionMs), Is.EqualTo(110d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.CorrectionCostMs), Is.EqualTo(40d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.BrokenFirstAcceptedErrorRate), Is.EqualTo(1d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.BrokenVehicleButtonTapRate), Is.EqualTo(1d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.CoveredPrematureRawTouchRate), Is.EqualTo(1d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.CoveredPrematureAcceptedInputRate), Is.EqualTo(1d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.CoveredPostRevealErrorRate), Is.EqualTo(1d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.ExpressAccuracy), Is.EqualTo(1d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.ExpressResponseTimeMs), Is.EqualTo(120d));
            string[] expressMetrics = metrics.Select(row => row.Metric).Where(value => value.StartsWith("express_", StringComparison.Ordinal)).ToArray();
            Assert.That(expressMetrics, Is.EquivalentTo(new[]
            {
                ExperimentMetricIds.ExpressAccuracy,
                ExperimentMetricIds.ExpressResponseTimeMs,
                ExperimentMetricIds.ExpressTimeoutRate,
            }));
            Assert.That(metrics.Any(row => row.Metric.IndexOf("miss", StringComparison.OrdinalIgnoreCase) >= 0
                && row.Metric.StartsWith("express_", StringComparison.Ordinal)), Is.False);
            Assert.That(MetricValue(metrics, ExperimentMetricIds.SurveyBrokenTooEasyMedian), Is.EqualTo(5d));
            Assert.That(metrics.Where(row => row.MetricRole == ExperimentMetricRoles.Primary).Select(row => row.Metric),
                Is.EquivalentTo(new[]
                {
                    ExperimentMetricIds.FirstRawTouchErrorRate,
                    ExperimentMetricIds.TrialWithDisabledRepairTapRate,
                    ExperimentMetricIds.TrialWithHiddenRepairAreaTapRate,
                    ExperimentMetricIds.TimeToCorrectResolutionMs,
                    ExperimentMetricIds.CorrectionCostMs,
                    ExperimentMetricIds.TimeoutRate,
                    ExperimentMetricIds.EventualCorrectRate,
                }));
            Assert.That(metrics.Single(row => row.Metric == ExperimentMetricIds.FirstAcceptedInputErrorRate).MetricRole,
                Is.EqualTo(ExperimentMetricRoles.Diagnostic));
        }

        [Test]
        public void V21Analytics_CoveredUsesPostRevealOriginAndKeepsPrematureTouchSeparate()
        {
            ExperimentTrialRecord trial = V2Trial(
                1,
                ExperimentSpecialState.Covered,
                ExperimentInput.Compact,
                false,
                false,
                true,
                ExperimentInput.Compact,
                -1d);
            trial.InformationAvailableAtMs = 500d;
            trial.FinalResponseTimeMs = 580d;
            var events = new[]
            {
                V2Event(1, 1, ExperimentInput.Repair, false, ExperimentIgnoredReasons.CoveredNotRevealed, -1d,
                    ExperimentInput.Compact),
                V2Event(1, 2, ExperimentInput.Sports, false, ExperimentIgnoredReasons.None, 30d,
                    ExperimentInput.Compact),
                V2Event(1, 3, ExperimentInput.Compact, true, ExperimentIgnoredReasons.None, 80d,
                    ExperimentInput.Compact),
            };

            List<ExperimentParticipantConditionMetric> metrics = InputUIExperimentAnalytics.SummarizeParticipantConditions(
                new[] { trial },
                events,
                Array.Empty<ExperimentSurveyRecord>(),
                InputUIExperimentSchema.CurrentExperimentVersion,
                ExperimentType.StateAssist);

            Assert.That(MetricValue(metrics, ExperimentMetricIds.FirstRawTouchErrorRate), Is.EqualTo(1d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.FirstAcceptedInputErrorRate), Is.EqualTo(0d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.CoveredPrematureRawTouchRate), Is.EqualTo(1d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.TimeToFirstRawTouchMs), Is.EqualTo(30d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.TimeToFirstAcceptedInputMs), Is.EqualTo(80d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.TimeToCorrectResolutionMs), Is.EqualTo(80d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.CorrectionCostMs), Is.EqualTo(50d));
        }

        [Test]
        public void V21Analytics_StaleRepairMetricExcludesStaleVehicleInput()
        {
            ExperimentTrialRecord trial = V2Trial(
                1,
                ExperimentSpecialState.Normal,
                ExperimentInput.Sports,
                false,
                false,
                false,
                ExperimentInput.None,
                -1d);
            var events = new[]
            {
                V2Event(1, 1, ExperimentInput.Sports, false, ExperimentIgnoredReasons.StaleTarget, 10d),
                V2Event(1, 2, ExperimentInput.Repair, false, ExperimentIgnoredReasons.StaleTarget, 20d),
                V2Event(1, 3, ExperimentInput.Repair, false, ExperimentIgnoredReasons.StaleTarget, 30d),
            };

            List<ExperimentParticipantConditionMetric> metrics = InputUIExperimentAnalytics.SummarizeParticipantConditions(
                new[] { trial },
                events,
                Array.Empty<ExperimentSurveyRecord>(),
                InputUIExperimentSchema.CurrentExperimentVersion,
                ExperimentType.StateAssist);

            ExperimentParticipantConditionMetric eventRate = metrics.Single(row =>
                row.Metric == ExperimentMetricIds.StaleRepairAreaTapEventRate);
            Assert.That(eventRate.Numerator, Is.EqualTo(2d));
            Assert.That(eventRate.Denominator, Is.EqualTo(3d));
            Assert.That(eventRate.Value, Is.EqualTo(2d / 3d).Within(0.0001d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.TrialWithStaleRepairAreaTapRate), Is.EqualTo(1d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.StaleRepairAreaTapRatePerTrial), Is.EqualTo(1d),
                "Repeated taps by one participant must not inflate the trial-level rate.");
        }

        [Test]
        public void V21Analytics_StaleRepairIncludesHiddenTapImmediatelyAfterRepairDisappears()
        {
            var trials = new[]
            {
                V2Trial(1, ExperimentSpecialState.Broken, ExperimentInput.Repair,
                    false, true, true, ExperimentInput.Repair, 100d),
                V2Trial(2, ExperimentSpecialState.Normal, ExperimentInput.Sports,
                    false, false, false, ExperimentInput.None, -1d),
                V2Trial(3, ExperimentSpecialState.Normal, ExperimentInput.Compact,
                    false, false, false, ExperimentInput.None, -1d),
            };
            var events = new[]
            {
                V2Event(2, 1, ExperimentInput.Repair, false,
                    ExperimentIgnoredReasons.HiddenOrEmpty, 10d, ExperimentInput.Sports),
                V2Event(3, 2, ExperimentInput.Repair, false,
                    ExperimentIgnoredReasons.HiddenOrEmpty, 20d, ExperimentInput.Compact),
            };
            foreach (ExperimentTrialRecord trial in trials)
            {
                trial.Condition = ExperimentConditionIds.AssistBrokenOnly;
            }
            foreach (ExperimentInputEventRecord inputEvent in events)
            {
                inputEvent.Condition = ExperimentConditionIds.AssistBrokenOnly;
                inputEvent.IsInsideCurrentRepairRect = true;
                inputEvent.WasButtonVisible = false;
            }

            List<ExperimentParticipantConditionMetric> metrics = InputUIExperimentAnalytics.SummarizeParticipantConditions(
                trials,
                events,
                Array.Empty<ExperimentSurveyRecord>(),
                InputUIExperimentSchema.CurrentExperimentVersion,
                ExperimentType.StateAssist);

            Assert.That(MetricValue(metrics, ExperimentMetricIds.HiddenRepairAreaTapEventRate), Is.EqualTo(1d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.TrialWithHiddenRepairAreaTapRate), Is.EqualTo(2d / 3d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.StaleRepairAreaTapEventRate), Is.EqualTo(0.5d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.StaleRepairAreaTapRatePerTrial), Is.EqualTo(1d / 3d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.TrialWithStaleRepairAreaTapRate), Is.EqualTo(1d / 3d));
        }

        [Test]
        public void V21Analytics_UsesRestoredEventIndexAcrossMonotonicClockEpochs()
        {
            ExperimentTrialRecord trial = V2Trial(
                1,
                ExperimentSpecialState.Normal,
                ExperimentInput.Sports,
                false,
                true,
                true,
                ExperimentInput.Sports,
                100d);
            ExperimentInputEventRecord beforeForceStop = V2Event(
                1, 1, ExperimentInput.Sports, true, ExperimentIgnoredReasons.None, 100d);
            ExperimentInputEventRecord afterRestart = V2Event(
                1, 2, ExperimentInput.Repair, false, ExperimentIgnoredReasons.Disabled, 200d);
            beforeForceStop.EventTimeMs = 5000d;
            afterRestart.EventTimeMs = 25d;

            List<ExperimentParticipantConditionMetric> metrics = InputUIExperimentAnalytics.SummarizeParticipantConditions(
                new[] { trial },
                new[] { beforeForceStop, afterRestart },
                Array.Empty<ExperimentSurveyRecord>(),
                InputUIExperimentSchema.CurrentExperimentVersion,
                ExperimentType.StateAssist);

            Assert.That(MetricValue(metrics, ExperimentMetricIds.FirstRawTouchErrorRate), Is.EqualTo(0d),
                "Checkpoint-restored event_index, not the new process clock epoch, defines first Raw order.");
        }

        [Test]
        public void V21Analytics_ExcludesEventsOutsideMeasuredTrialIdentity()
        {
            ExperimentTrialRecord trial = V2Trial(
                1,
                ExperimentSpecialState.Normal,
                ExperimentInput.Sports,
                false,
                true,
                true,
                ExperimentInput.Sports,
                100d);
            ExperimentInputEventRecord wrongTrialSet = V2Event(
                1, 1, ExperimentInput.Repair, false, ExperimentIgnoredReasons.Disabled, 10d);
            wrongTrialSet.TrialSetId = InputUITrialPlan.Set02;
            ExperimentInputEventRecord sessionNotRunning = V2Event(
                1, 2, ExperimentInput.Repair, false, ExperimentIgnoredReasons.SessionNotRunning, 20d);
            ExperimentInputEventRecord trialNotActionable = V2Event(
                1, 3, ExperimentInput.Repair, false, ExperimentIgnoredReasons.TrialNotActionable, 30d);
            ExperimentInputEventRecord measured = V2Event(
                1, 4, ExperimentInput.Sports, true, ExperimentIgnoredReasons.None, 100d);

            List<ExperimentParticipantConditionMetric> metrics = InputUIExperimentAnalytics.SummarizeParticipantConditions(
                new[] { trial },
                new[] { wrongTrialSet, sessionNotRunning, trialNotActionable, measured },
                Array.Empty<ExperimentSurveyRecord>(),
                InputUIExperimentSchema.CurrentExperimentVersion,
                ExperimentType.StateAssist);

            Assert.That(MetricValue(metrics, ExperimentMetricIds.FirstRawTouchErrorRate), Is.EqualTo(0d));
            ExperimentParticipantConditionMetric disabledEventRate = metrics.Single(row =>
                row.Metric == ExperimentMetricIds.DisabledTapEventRate);
            Assert.That(disabledEventRate.Numerator, Is.EqualTo(0d));
            Assert.That(disabledEventRate.Denominator, Is.EqualTo(1d),
                "Only the event belonging to this measured trial set is eligible for event-rate denominators.");
        }

        [Test]
        public void V21Analytics_GenericEmptyRequiresNoHitUiElement()
        {
            ExperimentTrialRecord trial = V2Trial(
                1,
                ExperimentSpecialState.Normal,
                ExperimentInput.Sports,
                false,
                false,
                false,
                ExperimentInput.None,
                -1d);
            ExperimentInputEventRecord hitHud = V2Event(
                1, 1, ExperimentInput.None, false, ExperimentIgnoredReasons.HiddenOrEmpty, 10d);
            hitHud.HitUiElement = "HudPanel";

            List<ExperimentParticipantConditionMetric> metrics = InputUIExperimentAnalytics.SummarizeParticipantConditions(
                new[] { trial },
                new[] { hitHud },
                Array.Empty<ExperimentSurveyRecord>(),
                InputUIExperimentSchema.CurrentExperimentVersion,
                ExperimentType.StateAssist);

            Assert.That(MetricValue(metrics, ExperimentMetricIds.EmptyAreaTapEventRate), Is.EqualTo(0d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.TrialWithEmptyAreaTapRate), Is.EqualTo(0d));
            Assert.That(MetricValue(metrics, ExperimentMetricIds.GenericEmptyAreaTapRatePerTrial), Is.EqualTo(0d));
        }

        [Test]
        public void V21Analytics_ExplicitRushReferenceSupportsPairWithoutCurrentPreset()
        {
            HumanRushRegistration registration = RegisteredRush();
            string reference = registration.ReferenceConditionId;
            string comparison = registration.Candidates.Single(row => row.ConditionId != reference).ConditionId;
            var metrics = new List<ExperimentParticipantConditionMetric>
            {
                V2Metric("P01", "S-RUSH", reference, 0.8d, ExperimentType.RushPackage),
                V2Metric("P01", "S-RUSH", comparison, 0.5d, ExperimentType.RushPackage),
            };

            List<ExperimentParticipantDifferenceMetric> differences =
                InputUIExperimentAnalytics.CalculateParticipantDifferences(metrics, reference);
            List<ExperimentGroupMetricSummary> groups =
                InputUIExperimentAnalytics.SummarizeGroups(metrics, differences, reference);

            ExperimentParticipantDifferenceMetric difference = differences.Single();
            Assert.That(difference.ReferenceCondition, Is.EqualTo(reference));
            Assert.That(difference.ComparisonCondition, Is.EqualTo(comparison));
            Assert.That(difference.Difference, Is.EqualTo(-0.3d).Within(0.0001d));
            Assert.That(difference.Improved, Is.True);
            Assert.That(groups.Select(row => row.ReferenceCondition), Has.All.EqualTo(reference));
            Assert.That(groups.Select(row => row.Condition), Is.EquivalentTo(new[] { reference, comparison }));
            Assert.That(groups.Any(row => row.Condition == ExperimentConditionIds.RushCurrentEquivalent), Is.False);
        }

        [Test]
        public void V2GroupSummary_UsesPairedParticipantDirections()
        {
            var metrics = new List<ExperimentParticipantConditionMetric>();
            for (int participant = 1; participant <= 6; participant++)
            {
                string id = "P" + participant.ToString("00");
                metrics.Add(V2Metric(id, "S" + participant, ExperimentConditionIds.PlacementUpperFixed, 0.5d));
                double comparison = participant <= 4 ? 0.4d : participant == 5 ? 0.6d : 0.5d;
                metrics.Add(V2Metric(id, "S" + participant, ExperimentConditionIds.PlacementBottomFixed, comparison));
            }

            List<ExperimentParticipantDifferenceMetric> differences =
                InputUIExperimentAnalytics.CalculateParticipantDifferences(metrics);
            List<ExperimentGroupMetricSummary> groups = InputUIExperimentAnalytics.SummarizeGroups(metrics, differences);
            ExperimentGroupMetricSummary comparisonGroup = groups.Single(row =>
                row.Condition == ExperimentConditionIds.PlacementBottomFixed
                && row.Metric == ExperimentMetricIds.FirstAcceptedInputErrorRate);
            Assert.That(comparisonGroup.ParticipantCount, Is.EqualTo(6));
            Assert.That(comparisonGroup.ImprovedParticipantCount, Is.EqualTo(4));
            Assert.That(comparisonGroup.WorsenedParticipantCount, Is.EqualTo(1));
            Assert.That(comparisonGroup.UnchangedParticipantCount, Is.EqualTo(1));
            Assert.That(comparisonGroup.MetricRole, Is.EqualTo(ExperimentMetricRoles.Diagnostic));
        }

        [Test]
        public void V2SummaryExport_WritesThreeVersionSeparatedUtf8BomFiles()
        {
            string root = Path.Combine(Path.GetTempPath(), "InputUIComparisonV2-" + Guid.NewGuid().ToString("N"));
            try
            {
                var metrics = new List<ExperimentParticipantConditionMetric>
                {
                    V2Metric("P01", "S01", ExperimentConditionIds.PlacementUpperFixed, 0.5d),
                    V2Metric("P01", "S01", ExperimentConditionIds.PlacementBottomFixed, 0.4d),
                };
                List<ExperimentParticipantDifferenceMetric> differences =
                    InputUIExperimentAnalytics.CalculateParticipantDifferences(metrics);
                List<ExperimentGroupMetricSummary> groups = InputUIExperimentAnalytics.SummarizeGroups(metrics, differences);
                string output = InputUIExperimentAnalytics.ExportAnalysisTables(
                    root,
                    new ExperimentAnalysisTables(metrics, differences, groups),
                    InputUIExperimentSchema.CurrentExperimentVersion,
                    ExperimentType.Placement);

                Assert.That(output.Replace('\\', '/'), Does.EndWith("/2.1.0/placement"));
                foreach (string filename in new[]
                {
                    "summary_participant_condition.csv",
                    "summary_participant_difference.csv",
                    "summary_group.csv",
                })
                {
                    string path = Path.Combine(output, filename);
                    Assert.That(File.Exists(path), Is.True, filename);
                    Assert.That(File.ReadAllBytes(path).Take(3), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
                    Assert.That(File.ReadLines(path).First(), Does.Contain("metric_role"));
                }
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void V21Aggregator_UsesManifestRushPairAndExplicitReference()
        {
            string root = Path.Combine(Path.GetTempPath(), "InputUIComparisonRushAggregate-" + Guid.NewGuid().ToString("N"));
            try
            {
                HumanRushRegistration registration = RegisteredRush();
                CreateCompletedRushSession(root, registration, false);

                string output = InputUIComparisonResultAggregator.AggregateCompleteSessions(
                    root,
                    InputUIExperimentSchema.CurrentExperimentVersion,
                    ExperimentType.RushPackage);

                Assert.That(output, Is.Not.Null.And.Not.Empty);
                string differencePath = Path.Combine(output, "summary_participant_difference.csv");
                string[] fields = File.ReadLines(differencePath).Skip(1).Single().Split(',');
                Assert.That(fields[5], Is.EqualTo(registration.ReferenceConditionId));
                Assert.That(fields[6], Is.EqualTo(registration.Candidates[1].ConditionId));
                Assert.That(File.ReadAllText(differencePath), Does.Not.Contain(ExperimentConditionIds.RushCurrentEquivalent));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void V21Aggregator_RejectsDuplicateOrMissingMeasuredTrials()
        {
            string root = Path.Combine(Path.GetTempPath(), "InputUIComparisonRushIncomplete-" + Guid.NewGuid().ToString("N"));
            try
            {
                CreateCompletedRushSession(root, RegisteredRush(), true);

                string output = InputUIComparisonResultAggregator.AggregateCompleteSessions(
                    root,
                    InputUIExperimentSchema.CurrentExperimentVersion,
                    ExperimentType.RushPackage);

                Assert.That(output, Is.Null,
                    "A completed manifest is not aggregatable without 24 unique measured trials per condition.");
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void V21CsvWriter_HumanRushRequiresExplicitReference()
        {
            string session = "UNITTEST-RUSH-" + Guid.NewGuid().ToString("N");
            var writer = new InputUIExperimentCsvWriter(
                "P01",
                session,
                InputUIExperimentSchema.CurrentExperimentVersion,
                ExperimentType.RushPackage);
            ExperimentTrialRecord trial = V2Trial(
                1,
                ExperimentSpecialState.Normal,
                ExperimentInput.Sports,
                false,
                true,
                true,
                ExperimentInput.Sports,
                100d);
            trial.ExperimentType = ExperimentType.RushPackage;
            trial.Condition = RegisteredRush().Candidates[0].ConditionId;
            try
            {
                Assert.Throws<InvalidOperationException>(() => writer.Export(
                    new[] { trial },
                    Array.Empty<ExperimentInputEventRecord>(),
                    Array.Empty<ExperimentSurveyRecord>()));
            }
            finally
            {
                if (Directory.Exists(writer.ExportDirectory)) Directory.Delete(writer.ExportDirectory, true);
            }
        }

        [Test]
        public void RawEventJournal_SurvivesReloadDeduplicatesAndIgnoresMalformedTail()
        {
            string root = Path.Combine(Path.GetTempPath(), "InputUIComparisonWal-" + Guid.NewGuid().ToString("N"));
            try
            {
                var persistence = new SessionPersistence(
                    "P00",
                    "WAL-SESSION",
                    InputUIExperimentSchema.CurrentExperimentVersion,
                    root);
                ExperimentInputEventRecord first = JournalEvent("P00", "WAL-SESSION", 1, 10d);
                ExperimentInputEventRecord duplicate = JournalEvent("P00", "WAL-SESSION", 1, 999d);
                ExperimentInputEventRecord second = JournalEvent("P00", "WAL-SESSION", 2, 20d);

                persistence.AppendRawEventJournal(first);
                persistence.AppendRawEventJournal(duplicate);
                persistence.AppendRawEventJournal(second);
                File.AppendAllText(persistence.RawEventWalPath, "{\"torn\":", new UTF8Encoding(false));

                IReadOnlyList<ExperimentInputEventRecord> restored = persistence.ReadRawEventJournal();
                Assert.That(restored.Select(row => row.EventIndex), Is.EqualTo(new[] { 1, 2 }));
                Assert.That(restored[0].EventTimeMs, Is.EqualTo(10d),
                    "The first durable copy is authoritative for a duplicate event_index.");
                Assert.That(restored[1].EventTimeMs, Is.EqualTo(20d));

                ExperimentInputEventRecord wrongSession = JournalEvent("P00", "OTHER-SESSION", 3, 30d);
                Assert.Throws<InvalidDataException>(() => persistence.AppendRawEventJournal(wrongSession));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void LatestIncompleteSession_CompletedCheckpointRepairsManifestWithoutLosingRushContract()
        {
            string root = Path.Combine(Path.GetTempPath(), "InputUIComparisonCompleteRepair-" + Guid.NewGuid().ToString("N"));
            try
            {
                const string participant = "P01";
                const string session = "COMPLETE-CUT";
                HumanRushRegistration registration = RegisteredRush();
                ExperimentSchedule schedule = InputUITrialPlan.BuildSchedule(
                    participant,
                    ExperimentType.RushPackage,
                    InputUIExperimentSchema.CurrentExperimentVersion,
                    RepairAnchorMode.UpperFixed,
                    registration);
                var persistence = new SessionPersistence(
                    participant,
                    session,
                    InputUIExperimentSchema.CurrentExperimentVersion,
                    root);
                ExperimentConditionAssignment first = schedule.Assignments[0];
                ExperimentSessionManifest manifest = SessionPersistence.CreateManifestSnapshot(
                    participant,
                    session,
                    ExperimentType.RushPackage,
                    schedule.Assignments,
                    first.RepairAnchorMode,
                    first.RepairAssistMode,
                    first.VehicleButtonLayoutMode,
                    false,
                    experimentVersion: schedule.ExperimentVersion,
                    rushProtocolId: schedule.ProtocolId,
                    referenceConditionId: schedule.ReferenceConditionId,
                    conditionDefinitions: schedule.ConditionDefinitions);
                persistence.BeginSession(manifest);
                var completedCheckpoint = new ExperimentSessionCheckpoint
                {
                    Manifest = manifest,
                    Phase = "Complete",
                    CompletionStatus = SessionPersistence.StatusCompleted,
                    ConditionIndex = schedule.Assignments.Count,
                    TrialIndex = InputUITrialPlan.MeasuredTrialsPerVariant,
                };
                persistence.SaveCheckpoint(completedCheckpoint);

                Assert.That(SessionPersistence.TryFindLatestIncompleteSession(
                    participant,
                    out _,
                    InputUIExperimentSchema.CurrentExperimentVersion,
                    root), Is.False);
                Assert.That(persistence.TryLoadManifest(out ExperimentSessionManifest repaired), Is.True);
                Assert.That(repaired.CompletionStatus, Is.EqualTo(SessionPersistence.StatusCompleted));
                Assert.That(repaired.CompletedAt, Is.EqualTo(completedCheckpoint.SavedAt));
                Assert.That(repaired.RushProtocolId, Is.EqualTo(registration.ProtocolId));
                Assert.That(repaired.ReferenceConditionId, Is.EqualTo(registration.ReferenceConditionId));
                Assert.That(repaired.ConditionDefinitions.Select(row => row.ConditionId),
                    Is.EqualTo(registration.Candidates.Select(row => row.ConditionId)));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void CsvWriter_WritesUtf8BomEscapesTextAndCanCheckpointTwice()
        {
            string session = "UNITTEST-" + Guid.NewGuid().ToString("N");
            var writer = new InputUIExperimentCsvWriter("P99", session);
            try
            {
                var trials = new[] { Trial(true, false, 123d) };
                var surveys = new[]
                {
                    new ExperimentSurveyRecord
                    {
                        ParticipantId = "P99",
                        SessionId = session,
                        Variant = ExperimentVariant.Baseline,
                        MistakeExplanation = "comma, quote \" and\nnewline",
                        MostConfusingMoment = "none",
                    },
                };
                writer.Export(trials, Array.Empty<ExperimentInputEventRecord>(), surveys);
                writer.Export(trials, Array.Empty<ExperimentInputEventRecord>(), surveys);

                string trialPath = Directory.GetFiles(writer.ExportDirectory, "*_trials.csv").Single();
                byte[] bytes = File.ReadAllBytes(trialPath);
                Assert.That(bytes.Take(3), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
                string trialHeader = File.ReadLines(trialPath).First();
                Assert.That(trialHeader, Does.Contain("information_available_at_ms"));
                Assert.That(trialHeader, Does.Contain("time_to_first_raw_touch_ms"));
                Assert.That(trialHeader, Does.Contain("time_to_first_accepted_input_ms"));
                Assert.That(trialHeader, Does.Contain("time_to_correct_resolution_ms"));
                Assert.That(trialHeader, Does.Contain("correction_cost_ms"));
                string surveyPath = Directory.GetFiles(writer.ExportDirectory, "*_survey.csv").Single();
                string text = File.ReadAllText(surveyPath);
                Assert.That(text, Does.Contain("\"comma, quote \"\" and\nnewline\""));
            }
            finally
            {
                if (Directory.Exists(writer.ExportDirectory)) Directory.Delete(writer.ExportDirectory, true);
            }
        }

        private static HumanRushRegistration RegisteredRush()
        {
            return new HumanRushRegistration(
                "RUSH-PREREG-UNIT-001",
                "Rush_PreregisteredReference",
                new HumanRushCandidateDefinition(
                    "Rush_PreregisteredReference",
                    RepairAnchorMode.UpperFixed,
                    RepairAssistMode.AlwaysActive,
                    VehicleButtonLayoutMode.StandardThree),
                new HumanRushCandidateDefinition(
                    "Rush_PreregisteredAlternative",
                    RepairAnchorMode.BottomFixed,
                    RepairAssistMode.BrokenOnly,
                    VehicleButtonLayoutMode.FixedFour));
        }

        private static ExperimentInputEventRecord JournalEvent(
            string participantId,
            string sessionId,
            int eventIndex,
            double eventTimeMs)
        {
            return new ExperimentInputEventRecord
            {
                SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion,
                ExperimentVersion = InputUIExperimentSchema.CurrentExperimentVersion,
                ParticipantId = participantId,
                SessionId = sessionId,
                ExperimentType = ExperimentType.Placement,
                Condition = ExperimentConditionIds.PlacementUpperFixed,
                TrialSetId = InputUITrialPlan.Set01,
                TrialNumber = 1,
                EventIndex = eventIndex,
                EventTimeMs = eventTimeMs,
                Input = ExperimentInput.Sports,
                Disposition = "raw",
                IgnoredReason = ExperimentIgnoredReasons.None,
            };
        }

        private static void CreateCompletedRushSession(
            string root,
            HumanRushRegistration registration,
            bool duplicateLastTrial)
        {
            const string participant = "P01";
            const string session = "S-RUSH-AGGREGATE";
            ExperimentSchedule schedule = InputUITrialPlan.BuildSchedule(
                participant,
                ExperimentType.RushPackage,
                InputUIExperimentSchema.CurrentExperimentVersion,
                RepairAnchorMode.UpperFixed,
                registration);
            var persistence = new SessionPersistence(
                participant,
                session,
                InputUIExperimentSchema.CurrentExperimentVersion,
                root);
            ExperimentSessionManifest manifest = SessionPersistence.CreateManifestSnapshot(
                participant,
                session,
                ExperimentType.RushPackage,
                schedule.Assignments,
                RepairAnchorMode.UpperFixed,
                RepairAssistMode.AlwaysActive,
                VehicleButtonLayoutMode.StandardThree,
                false,
                experimentVersion: schedule.ExperimentVersion,
                rushProtocolId: schedule.ProtocolId,
                referenceConditionId: schedule.ReferenceConditionId,
                conditionDefinitions: schedule.ConditionDefinitions);
            persistence.BeginSession(manifest);
            persistence.MarkCompleted(manifest);

            var summaryLines = new List<string>
            {
                "schema_version,experiment_version,experiment_type,participant_id,session_id,condition,condition_order,trial_set_id,metric,metric_role,numerator,denominator,value",
            };
            foreach (ExperimentConditionAssignment assignment in schedule.Assignments)
            {
                string value = string.Equals(
                    assignment.Condition,
                    registration.ReferenceConditionId,
                    StringComparison.Ordinal) ? "0.4" : "0.2";
                summaryLines.Add(string.Join(",", new[]
                {
                    InputUIExperimentSchema.CurrentSchemaVersion,
                    InputUIExperimentSchema.CurrentExperimentVersion,
                    InputUIComparisonNaming.ExperimentTypeId(ExperimentType.RushPackage),
                    participant,
                    session,
                    assignment.Condition,
                    assignment.ConditionOrder.ToString(CultureInfo.InvariantCulture),
                    assignment.TrialSetId,
                    ExperimentMetricIds.FirstRawTouchErrorRate,
                    ExperimentMetricRoles.Primary,
                    value,
                    "1",
                    value,
                }));
            }
            File.WriteAllLines(
                Path.Combine(persistence.SessionDirectory, "summary_participant_condition.csv"),
                summaryLines,
                new UTF8Encoding(true));

            var trialLines = new List<string>
            {
                "experiment_version,experiment_type,participant_id,session_id,condition,trial_set_id,is_pilot,is_practice,trial_number",
            };
            for (int assignmentIndex = 0; assignmentIndex < schedule.Assignments.Count; assignmentIndex++)
            {
                ExperimentConditionAssignment assignment = schedule.Assignments[assignmentIndex];
                for (int trialNumber = 1; trialNumber <= InputUITrialPlan.MeasuredTrialsPerVariant; trialNumber++)
                {
                    int writtenTrialNumber = duplicateLastTrial && assignmentIndex == 0
                        && trialNumber == InputUITrialPlan.MeasuredTrialsPerVariant
                            ? trialNumber - 1
                            : trialNumber;
                    trialLines.Add(string.Join(",", new[]
                    {
                        InputUIExperimentSchema.CurrentExperimentVersion,
                        InputUIComparisonNaming.ExperimentTypeId(ExperimentType.RushPackage),
                        participant,
                        session,
                        assignment.Condition,
                        assignment.TrialSetId,
                        "false",
                        "false",
                        writtenTrialNumber.ToString(CultureInfo.InvariantCulture),
                    }));
                }
            }
            File.WriteAllLines(
                Path.Combine(persistence.SessionDirectory, participant + "_" + session + "_trials.csv"),
                trialLines,
                new UTF8Encoding(true));
        }

        private static ExperimentTrialRecord Trial(bool correct, bool timeout, double rt)
        {
            return new ExperimentTrialRecord
            {
                Variant = ExperimentVariant.Baseline,
                IsPractice = false,
                IsCorrect = correct,
                IsTimeout = timeout,
                InformationAvailableAtMs = 0d,
                TimeToFirstRawTouchMs = rt,
                TimeToFirstAcceptedInputMs = rt,
                TimeToCorrectResolutionMs = correct ? rt : -1d,
                CorrectionCostMs = correct ? 0d : -1d,
                ResponseTimeMs = rt,
                ActionableResponseTimeMs = -1d,
                FinalResponseTimeMs = correct ? rt : -1d,
                EventualCorrect = correct,
            };
        }

        private static ExperimentTrialRecord V2Trial(
            int trialNumber,
            ExperimentSpecialState state,
            ExperimentInput expected,
            bool timeout,
            bool firstCorrect,
            bool eventualCorrect,
            ExperimentInput finalInput,
            double actionableMs,
            string participant = "P01",
            bool isPilot = false,
            string experimentVersion = InputUIExperimentSchema.CurrentExperimentVersion)
        {
            return new ExperimentTrialRecord
            {
                SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion,
                ExperimentVersion = experimentVersion,
                ExperimentType = ExperimentType.StateAssist,
                ParticipantId = participant,
                SessionId = participant == "P01" ? "S01" : "S-" + participant,
                Condition = ExperimentConditionIds.AssistAlwaysActive,
                ConditionOrder = 1,
                TrialSetId = InputUITrialPlan.Set01,
                IsPilot = isPilot,
                TrialNumber = trialNumber,
                SpecialState = state,
                ExpectedInput = expected,
                ActualInput = finalInput,
                FinalInput = finalInput,
                IsCorrect = firstCorrect,
                EventualCorrect = eventualCorrect,
                IsTimeout = timeout,
                ResponseTimeMs = actionableMs,
                ActionableResponseTimeMs = actionableMs,
                FinalResponseTimeMs = eventualCorrect ? actionableMs : -1d,
            };
        }

        private static ExperimentInputEventRecord V2Event(
            int trialNumber,
            int index,
            ExperimentInput input,
            bool accepted,
            string ignoredReason,
            double actionableMs,
            ExperimentInput expected = ExperimentInput.None,
            bool insidePrevious = false)
        {
            ExperimentInput inferredExpected = expected != ExperimentInput.None
                ? expected
                : trialNumber == 1 ? ExperimentInput.Sports
                    : trialNumber == 2 ? ExperimentInput.Repair
                        : trialNumber == 3 ? ExperimentInput.Compact
                            : ExperimentInput.LightTruck;
            return new ExperimentInputEventRecord
            {
                SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion,
                ExperimentVersion = InputUIExperimentSchema.CurrentExperimentVersion,
                ExperimentType = ExperimentType.StateAssist,
                ParticipantId = "P01",
                SessionId = "S01",
                Condition = ExperimentConditionIds.AssistAlwaysActive,
                ConditionOrder = 1,
                TrialSetId = InputUITrialPlan.Set01,
                TrialNumber = trialNumber,
                EventIndex = index,
                EventTimeMs = index,
                Input = input,
                AcceptedInput = accepted ? input : ExperimentInput.None,
                WasInputAccepted = accepted,
                IgnoredReason = ignoredReason,
                ExpectedInput = inferredExpected,
                ActionableTimeMs = actionableMs,
                IsInsidePreviousRepairRect = insidePrevious,
                IsInsideCurrentRepairRect = false,
            };
        }

        private static ExperimentParticipantConditionMetric V2Metric(
            string participant,
            string session,
            string condition,
            double value,
            ExperimentType experimentType = ExperimentType.Placement)
        {
            return new ExperimentParticipantConditionMetric
            {
                SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion,
                ExperimentVersion = InputUIExperimentSchema.CurrentExperimentVersion,
                ExperimentType = experimentType,
                ParticipantId = participant,
                SessionId = session,
                Condition = condition,
                Metric = ExperimentMetricIds.FirstAcceptedInputErrorRate,
                Numerator = value,
                Denominator = 1d,
                Value = value,
            };
        }

        private static double MetricValue(
            IEnumerable<ExperimentParticipantConditionMetric> metrics,
            string metric)
        {
            return metrics.Single(row => row.Metric == metric).Value;
        }

        private static int AdjacentCount(
            IReadOnlyList<ExperimentTrial> trials,
            Func<ExperimentTrial, ExperimentTrial, bool> predicate)
        {
            int count = 0;
            for (int i = 0; i + 1 < trials.Count; i++)
            {
                if (predicate(trials[i], trials[i + 1])) count++;
            }
            return count;
        }

        private static string TrialKey(ExperimentTrial trial)
        {
            return trial.Vehicle + ":" + trial.SpecialState + ":" + trial.Speed;
        }
    }
}
