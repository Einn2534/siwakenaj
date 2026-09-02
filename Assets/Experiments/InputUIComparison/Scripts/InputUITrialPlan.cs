using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Siwakenja.Experiments.InputUIComparison
{
    public static class InputUITrialPlan
    {
        public const int MeasuredTrialsPerVariant = 24;
        public const int PracticeTrialsPerVariant = 4;
        public const int DefaultSeed = 4172601;
        public const string Set01 = "Set01";
        public const string Set02 = "Set02";
        public const string Set03 = "Set03";
        public const string P00RushProtocolId = "P00_Rush_LegacyPresets_v2.1";

        private const int RequiredSameVehicleAdjacencies = 4;
        private const int RequiredBrokenThenNormalAdjacencies = 2;
        private const int RequiredNormalThenBrokenAdjacencies = 2;
        private const int MaximumSequenceAttempts = 50000;

        private static readonly string[] TrialSetIdsInternal = { Set01, Set02, Set03 };

        private static readonly int[][] PlacementOrders =
        {
            new[] { 0, 1 },
            new[] { 1, 0 },
        };

        private static readonly int[][] ThreeConditionOrders =
        {
            new[] { 0, 1, 2 },
            new[] { 0, 2, 1 },
            new[] { 1, 0, 2 },
            new[] { 1, 2, 0 },
            new[] { 2, 0, 1 },
            new[] { 2, 1, 0 },
        };

        private static readonly ExperimentVariant[][] Orders =
        {
            new[] { ExperimentVariant.Baseline, ExperimentVariant.FixedFour, ExperimentVariant.SeparatedStateAction },
            new[] { ExperimentVariant.Baseline, ExperimentVariant.SeparatedStateAction, ExperimentVariant.FixedFour },
            new[] { ExperimentVariant.FixedFour, ExperimentVariant.Baseline, ExperimentVariant.SeparatedStateAction },
            new[] { ExperimentVariant.FixedFour, ExperimentVariant.SeparatedStateAction, ExperimentVariant.Baseline },
            new[] { ExperimentVariant.SeparatedStateAction, ExperimentVariant.Baseline, ExperimentVariant.FixedFour },
            new[] { ExperimentVariant.SeparatedStateAction, ExperimentVariant.FixedFour, ExperimentVariant.Baseline },
        };

        public static IReadOnlyList<string> TrialSetIds => Array.AsReadOnly((string[])TrialSetIdsInternal.Clone());

        public static ExperimentSchedule BuildSchedule(
            string participantId,
            ExperimentType experimentType,
            string experimentVersion = InputUIExperimentSchema.CurrentExperimentVersion,
            RepairAnchorMode assistAnchorMode = RepairAnchorMode.UpperFixed,
            HumanRushRegistration humanRushRegistration = null)
        {
            string normalizedParticipant = NormalizeParticipantId(participantId);
            if (normalizedParticipant.Length == 0)
            {
                throw new ArgumentException("A participant id is required.", nameof(participantId));
            }
            if (string.IsNullOrWhiteSpace(experimentVersion))
            {
                throw new ArgumentException("An experiment version is required.", nameof(experimentVersion));
            }

            bool isPilot = IsPilotParticipant(normalizedParticipant);
            int ordinal;
            bool hasOrdinal = TryGetParticipantOrdinal(normalizedParticipant, out ordinal);
            int orderIndex = isPilot
                ? 0
                : hasOrdinal
                    ? ordinal - 1
                    : StableParticipantHash(normalizedParticipant, DefaultSeed);
            int setOffset = isPilot
                ? 0
                : hasOrdinal
                    ? (ordinal - 1) % TrialSetIdsInternal.Length
                    : StableParticipantHash(normalizedParticipant, DefaultSeed) % TrialSetIdsInternal.Length;

            ConditionTemplate[] conditions;
            int[][] orders;
            string protocolId = string.Empty;
            string referenceConditionId;
            if (experimentType == ExperimentType.RushPackage && !isPilot)
            {
                string registrationError;
                if (humanRushRegistration == null)
                {
                    registrationError = "No HumanRushRegistration was supplied.";
                }
                else if (humanRushRegistration.TryValidate(out registrationError))
                {
                    registrationError = string.Empty;
                }

                if (!string.IsNullOrEmpty(registrationError))
                {
                    throw new InvalidOperationException(
                        "Non-pilot Rush is locked until exactly two candidates are preregistered. "
                        + registrationError);
                }

                conditions = humanRushRegistration.Candidates
                    .Select(ToConditionTemplate)
                    .ToArray();
                orders = PlacementOrders;
                protocolId = humanRushRegistration.ProtocolId.Trim();
                referenceConditionId = humanRushRegistration.ReferenceConditionId.Trim();
            }
            else
            {
                conditions = GetConditionTemplates(experimentType, assistAnchorMode);
                orders = experimentType == ExperimentType.Placement ? PlacementOrders : ThreeConditionOrders;
                protocolId = experimentType == ExperimentType.RushPackage ? P00RushProtocolId : string.Empty;
                referenceConditionId = DefaultReferenceCondition(experimentType);
            }
            int[] order = orders[PositiveModulo(orderIndex, orders.Length)];
            var assignments = new List<ExperimentConditionAssignment>(order.Length);
            for (int position = 0; position < order.Length; position++)
            {
                int conditionIndex = order[position];
                ConditionTemplate condition = conditions[conditionIndex];
                string trialSetId = TrialSetIdsInternal[(setOffset + conditionIndex) % TrialSetIdsInternal.Length];
                int conditionSeed = StableConditionSeed(
                    normalizedParticipant,
                    experimentType,
                    condition.Id,
                    trialSetId,
                    experimentVersion);
                assignments.Add(new ExperimentConditionAssignment(
                    experimentType,
                    condition.Id,
                    position + 1,
                    trialSetId,
                    conditionSeed,
                    condition.RepairAnchorMode,
                    condition.RepairAssistMode,
                    condition.VehicleButtonLayoutMode));
            }

            HumanRushCandidateDefinition[] definitions = conditions.Select(template =>
                new HumanRushCandidateDefinition(
                    template.Id,
                    template.RepairAnchorMode,
                    template.RepairAssistMode,
                    template.VehicleButtonLayoutMode)).ToArray();
            return new ExperimentSchedule(
                normalizedParticipant,
                experimentVersion,
                isPilot,
                assignments,
                protocolId,
                referenceConditionId,
                definitions);
        }

        public static IReadOnlyList<string> GetConditionIds(ExperimentType experimentType)
        {
            ConditionTemplate[] templates = GetConditionTemplates(experimentType, RepairAnchorMode.UpperFixed);
            return Array.AsReadOnly(templates.Select(template => template.Id).ToArray());
        }

        private static string DefaultReferenceCondition(ExperimentType experimentType)
        {
            switch (experimentType)
            {
                case ExperimentType.Placement: return ExperimentConditionIds.PlacementUpperFixed;
                case ExperimentType.StateAssist: return ExperimentConditionIds.AssistAlwaysActive;
                case ExperimentType.RushPackage: return ExperimentConditionIds.RushCurrentEquivalent;
                default: throw new ArgumentOutOfRangeException(nameof(experimentType), experimentType, null);
            }
        }

        private static ConditionTemplate ToConditionTemplate(HumanRushCandidateDefinition candidate)
        {
            return new ConditionTemplate(
                candidate.ConditionId.Trim(),
                candidate.RepairAnchorMode,
                candidate.RepairAssistMode,
                candidate.VehicleButtonLayoutMode);
        }

        public static int StableConditionSeed(
            string participantId,
            ExperimentType experimentType,
            string condition,
            string trialSetId,
            string experimentVersion)
        {
            string canonical = NormalizeParticipantId(participantId)
                + "|" + experimentType.ToString()
                + "|" + (condition ?? string.Empty).Trim()
                + "|" + NormalizeTrialSetId(trialSetId)
                + "|" + (experimentVersion ?? string.Empty).Trim();
            byte[] bytes = Encoding.UTF8.GetBytes(canonical);
            unchecked
            {
                uint hash = 2166136261u;
                for (int i = 0; i < bytes.Length; i++)
                {
                    hash ^= bytes[i];
                    hash *= 16777619u;
                }
                int result = (int)(hash & 0x7fffffffu);
                return result == 0 ? 1 : result;
            }
        }

        public static bool IsPilotParticipant(string participantId)
        {
            return string.Equals(NormalizeParticipantId(participantId), "P00", StringComparison.Ordinal);
        }

        public static int StableParticipantHash(string participantId, int seed)
        {
            unchecked
            {
                uint hash = 2166136261;
                string value = (participantId ?? string.Empty).Trim().ToUpperInvariant() + ":" + seed;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= 16777619;
                }

                return (int)(hash & 0x7fffffff);
            }
        }

        public static ExperimentVariant[] GetCounterbalancedOrder(string participantId, int seed)
        {
            int index = GetCounterbalanceIndex(participantId, seed);
            return (ExperimentVariant[])Orders[index].Clone();
        }

        public static int[] GetProblemSetOrder(string participantId, int seed)
        {
            ExperimentVariant[] order = GetCounterbalancedOrder(participantId, seed);
            var result = new int[order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                result[i] = GetProblemSetForVariant(participantId, seed, order[i]);
            }

            return result;
        }

        public static int GetCounterbalanceIndex(string participantId, int seed)
        {
            int ordinal;
            if (TryGetParticipantOrdinal(participantId, out ordinal))
            {
                return (ordinal - 1) % Orders.Length;
            }

            return StableParticipantHash(participantId, seed) % Orders.Length;
        }

        public static int GetProblemSetForVariant(string participantId, int seed, ExperimentVariant variant)
        {
            int ordinal;
            int offset = TryGetParticipantOrdinal(participantId, out ordinal)
                ? (ordinal - 1) % 3
                : (StableParticipantHash(participantId, seed) / Orders.Length) % 3;
            return (offset + (int)variant) % 3;
        }

        public static List<ExperimentTrial> BuildPracticeSet(int blockIndex, int seed)
        {
            ExperimentVehicle[] vehicles =
            {
                ExperimentVehicle.Sports,
                ExperimentVehicle.Compact,
                ExperimentVehicle.LightTruck,
                ExperimentVehicle.Sports,
            };
            ExperimentSpecialState[] states =
            {
                ExperimentSpecialState.Normal,
                ExperimentSpecialState.Broken,
                ExperimentSpecialState.Covered,
                ExperimentSpecialState.Express,
            };

            var result = new List<ExperimentTrial>(PracticeTrialsPerVariant);
            int rotation = Math.Abs(seed + blockIndex) % vehicles.Length;
            for (int i = 0; i < PracticeTrialsPerVariant; i++)
            {
                result.Add(new ExperimentTrial
                {
                    TrialNumber = i + 1,
                    ProblemSet = -1,
                    TrialSetId = "Practice",
                    IsPractice = true,
                    Vehicle = vehicles[(i + rotation) % vehicles.Length],
                    SpecialState = states[i],
                    Speed = i < 2 ? ExperimentSpeed.Low : ExperimentSpeed.Normal,
                    CoveredRevealProgress = states[i] == ExperimentSpecialState.Covered
                        ? InputUIComparisonRules.CoveredRevealProgress
                        : -1f,
                    GapAfterSeconds = InputUIComparisonRules.NominalGapSeconds(
                        i < 2 ? ExperimentSpeed.Low : ExperimentSpeed.Normal,
                        false),
                    PaceContext = "practice",
                    SequenceContext = "practice",
                    RepeatGroup = 0,
                });
            }

            return result;
        }

        public static List<ExperimentTrial> BuildMeasuredSet(int problemSet, int seed)
        {
            if (problemSet < 0 || problemSet > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(problemSet));
            }

            return BuildMeasuredSet(TrialSetIdsInternal[problemSet], seed);
        }

        public static List<ExperimentTrial> BuildMeasuredSet(string trialSetId, int seed)
        {
            int problemSet = TrialSetIndex(trialSetId);
            var baseTrials = new List<ExperimentTrial>(MeasuredTrialsPerVariant);

            foreach (ExperimentVehicle vehicle in Enum.GetValues(typeof(ExperimentVehicle)))
            {
                foreach (ExperimentSpecialState state in Enum.GetValues(typeof(ExperimentSpecialState)))
                {
                    for (int repetition = 0; repetition < 2; repetition++)
                    {
                        baseTrials.Add(new ExperimentTrial
                        {
                            Vehicle = vehicle,
                            SpecialState = state,
                            Speed = (ExperimentSpeed)(((int)vehicle + (int)state + repetition + problemSet) % 3),
                        });
                    }
                }
            }

            int shortGapStart = 4 + problemSet * 6;
            List<ExperimentTrial> trials = null;
            for (int attempt = 0; attempt < MaximumSequenceAttempts; attempt++)
            {
                var candidate = new List<ExperimentTrial>(baseTrials);
                int derivedSeed = unchecked(
                    seed * 397
                    ^ problemSet * 7919
                    ^ attempt * 104729
                    ^ 0x5f3759df);
                Shuffle(candidate, new XorShift32((uint)derivedSeed));
                if (SatisfiesSequenceConstraints(candidate, shortGapStart))
                {
                    trials = candidate;
                    break;
                }
            }

            if (trials == null)
            {
                throw new InvalidOperationException(
                    "Unable to build " + NormalizeTrialSetId(trialSetId)
                    + " with the required adjacency and short-gap constraints.");
            }

            var result = new List<ExperimentTrial>(MeasuredTrialsPerVariant);
            int coveredOrdinal = 0;
            for (int i = 0; i < MeasuredTrialsPerVariant; i++)
            {
                bool shortGap = i >= shortGapStart && i < shortGapStart + 3;
                ExperimentTrial trial = trials[i];
                trial.TrialNumber = i + 1;
                trial.ProblemSet = problemSet;
                trial.TrialSetId = TrialSetIdsInternal[problemSet];
                trial.IsPractice = false;
                if (trial.SpecialState == ExperimentSpecialState.Covered)
                {
                    int revealIndex = (coveredOrdinal + problemSet) % 3;
                    trial.CoveredRevealProgress = revealIndex == 0 ? 0.42f : revealIndex == 1 ? 0.52f : 0.62f;
                    coveredOrdinal++;
                }
                else
                {
                    trial.CoveredRevealProgress = -1f;
                }
                trial.GapAfterSeconds = InputUIComparisonRules.NominalGapSeconds(trial.Speed, shortGap);
                trial.PaceContext = shortGap
                    ? "short_gap_0.22s"
                    : trial.Speed == ExperimentSpeed.Low
                        ? "stage11_gap_1.00s"
                        : trial.Speed == ExperimentSpeed.High
                            ? "stage15_gap_0.56s"
                            : "stage13_gap_0.72s";
                trial.SequenceContext = SequenceContextAt(trials, i, shortGap);
                bool repeatsPrevious = i > 0 && trials[i - 1].Vehicle == trial.Vehicle;
                bool repeatsNext = i + 1 < trials.Count && trials[i + 1].Vehicle == trial.Vehicle;
                trial.RepeatGroup = repeatsPrevious || repeatsNext ? 1 : 0;
                result.Add(trial);
            }

            return result;
        }

        private static void Shuffle<T>(IList<T> items, XorShift32 random)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T temp = items[i];
                items[i] = items[j];
                items[j] = temp;
            }
        }

        private static bool SatisfiesSequenceConstraints(
            IReadOnlyList<ExperimentTrial> trials,
            int shortGapStart)
        {
            int sameVehicle = 0;
            int brokenThenNormal = 0;
            int normalThenBroken = 0;
            for (int i = 0; i + 1 < trials.Count; i++)
            {
                ExperimentTrial current = trials[i];
                ExperimentTrial next = trials[i + 1];
                if (current.Vehicle == next.Vehicle) sameVehicle++;
                if (current.SpecialState == ExperimentSpecialState.Broken
                    && next.SpecialState == ExperimentSpecialState.Normal) brokenThenNormal++;
                if (current.SpecialState == ExperimentSpecialState.Normal
                    && next.SpecialState == ExperimentSpecialState.Broken) normalThenBroken++;
            }

            return sameVehicle == RequiredSameVehicleAdjacencies
                && brokenThenNormal == RequiredBrokenThenNormalAdjacencies
                && normalThenBroken == RequiredNormalThenBrokenAdjacencies
                && trials[shortGapStart].Vehicle == ExperimentVehicle.Sports
                && trials[shortGapStart + 1].Vehicle == ExperimentVehicle.Compact
                && trials[shortGapStart + 2].Vehicle == ExperimentVehicle.LightTruck;
        }

        private static string SequenceContextAt(
            IReadOnlyList<ExperimentTrial> trials,
            int index,
            bool shortGap)
        {
            var labels = new List<string>();
            if (shortGap) labels.Add("mixed_vehicle_short_gap");
            if (index + 1 < trials.Count)
            {
                ExperimentTrial current = trials[index];
                ExperimentTrial next = trials[index + 1];
                if (current.Vehicle == next.Vehicle) labels.Add("same_vehicle_next");
                if (current.SpecialState == ExperimentSpecialState.Broken
                    && next.SpecialState == ExperimentSpecialState.Normal) labels.Add("broken_then_normal");
                if (current.SpecialState == ExperimentSpecialState.Normal
                    && next.SpecialState == ExperimentSpecialState.Broken) labels.Add("normal_then_broken");
            }
            return labels.Count == 0 ? "standard" : string.Join("|", labels);
        }

        private static int TrialSetIndex(string trialSetId)
        {
            string normalized = NormalizeTrialSetId(trialSetId);
            for (int i = 0; i < TrialSetIdsInternal.Length; i++)
            {
                if (string.Equals(TrialSetIdsInternal[i], normalized, StringComparison.Ordinal)) return i;
            }
            throw new ArgumentOutOfRangeException(nameof(trialSetId), trialSetId, "Expected Set01, Set02, or Set03.");
        }

        private static string NormalizeTrialSetId(string trialSetId)
        {
            string value = (trialSetId ?? string.Empty).Trim();
            if (value.Length == 1 && value[0] >= '1' && value[0] <= '3') return "Set0" + value;
            if (value.Length == 2 && value[0] == '0' && value[1] >= '1' && value[1] <= '3') return "Set" + value;
            if (value.StartsWith("Set", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(value.Substring(3), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                && parsed >= 1 && parsed <= 3)
            {
                return "Set" + parsed.ToString("00", CultureInfo.InvariantCulture);
            }
            return value;
        }

        private static ConditionTemplate[] GetConditionTemplates(
            ExperimentType experimentType,
            RepairAnchorMode assistAnchorMode)
        {
            switch (experimentType)
            {
                case ExperimentType.Placement:
                    return new[]
                    {
                        new ConditionTemplate(
                            ExperimentConditionIds.PlacementUpperFixed,
                            RepairAnchorMode.UpperFixed,
                            RepairAssistMode.AlwaysActive,
                            VehicleButtonLayoutMode.StandardThree),
                        new ConditionTemplate(
                            ExperimentConditionIds.PlacementBottomFixed,
                            RepairAnchorMode.BottomFixed,
                            RepairAssistMode.AlwaysActive,
                            VehicleButtonLayoutMode.StandardThree),
                    };
                case ExperimentType.StateAssist:
                    return new[]
                    {
                        new ConditionTemplate(
                            ExperimentConditionIds.AssistAlwaysActive,
                            assistAnchorMode,
                            RepairAssistMode.AlwaysActive,
                            VehicleButtonLayoutMode.StandardThree),
                        new ConditionTemplate(
                            ExperimentConditionIds.AssistDisabledWhenNormal,
                            assistAnchorMode,
                            RepairAssistMode.DisabledWhenNormal,
                            VehicleButtonLayoutMode.StandardThree),
                        new ConditionTemplate(
                            ExperimentConditionIds.AssistBrokenOnly,
                            assistAnchorMode,
                            RepairAssistMode.BrokenOnly,
                            VehicleButtonLayoutMode.StandardThree),
                    };
                case ExperimentType.RushPackage:
                    return new[]
                    {
                        new ConditionTemplate(
                            ExperimentConditionIds.RushCurrentEquivalent,
                            RepairAnchorMode.BottomFixed,
                            RepairAssistMode.AlwaysActive,
                            VehicleButtonLayoutMode.StandardThree),
                        new ConditionTemplate(
                            ExperimentConditionIds.RushFixedFourEquivalent,
                            RepairAnchorMode.BottomFixed,
                            RepairAssistMode.DisabledWhenNormal,
                            VehicleButtonLayoutMode.FixedFour),
                        new ConditionTemplate(
                            ExperimentConditionIds.RushContextualRepairEquivalent,
                            RepairAnchorMode.UpperFixed,
                            RepairAssistMode.BrokenOnly,
                            VehicleButtonLayoutMode.StandardThree),
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(experimentType), experimentType, null);
            }
        }

        private static string NormalizeParticipantId(string participantId)
        {
            return (participantId ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static int PositiveModulo(int value, int divisor)
        {
            int remainder = value % divisor;
            return remainder < 0 ? remainder + divisor : remainder;
        }

        public static bool TryGetParticipantOrdinal(string participantId, out int ordinal)
        {
            ordinal = 0;
            string value = (participantId ?? string.Empty).Trim();
            int start = value.Length;
            while (start > 0 && char.IsDigit(value[start - 1])) start--;
            if (start == value.Length) return false;
            int parsed;
            if (!int.TryParse(value.Substring(start), out parsed) || parsed <= 0) return false;
            ordinal = parsed;
            return true;
        }

        private sealed class XorShift32
        {
            private uint _state;

            public XorShift32(uint seed)
            {
                _state = seed == 0u ? 0x6d2b79f5u : seed;
            }

            public int Next(int exclusiveMaximum)
            {
                uint value = _state;
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                _state = value;
                return (int)(value % (uint)exclusiveMaximum);
            }
        }

        private readonly struct ConditionTemplate
        {
            public ConditionTemplate(
                string id,
                RepairAnchorMode repairAnchorMode,
                RepairAssistMode repairAssistMode,
                VehicleButtonLayoutMode vehicleButtonLayoutMode)
            {
                Id = id;
                RepairAnchorMode = repairAnchorMode;
                RepairAssistMode = repairAssistMode;
                VehicleButtonLayoutMode = vehicleButtonLayoutMode;
            }

            public string Id { get; }
            public RepairAnchorMode RepairAnchorMode { get; }
            public RepairAssistMode RepairAssistMode { get; }
            public VehicleButtonLayoutMode VehicleButtonLayoutMode { get; }
        }
    }
}
