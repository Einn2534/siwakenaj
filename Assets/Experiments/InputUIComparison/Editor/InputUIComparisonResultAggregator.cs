using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Siwakenja.Experiments.InputUIComparison.Editor
{
    public static class InputUIComparisonResultAggregator
    {
        private sealed class SessionCandidate
        {
            public string ParticipantId;
            public string SessionId;
            public DateTime CompletedAtUtc;
            public string ProtocolId;
            public string ReferenceConditionId;
            public string[] Conditions;
            public List<ExperimentParticipantConditionMetric> Metrics;
        }

        private sealed class SessionContract
        {
            public string ProtocolId;
            public string ReferenceConditionId;
            public string[] Conditions;
            public Dictionary<string, string> TrialSetByCondition;
        }

        private sealed class MeasuredTrialEvidence
        {
            public string ExperimentVersion;
            public ExperimentType ExperimentType;
            public string ParticipantId;
            public string SessionId;
            public string Condition;
            public string TrialSetId;
            public bool IsPilot;
            public bool IsPractice;
            public int TrialNumber;
        }

        [MenuItem("Tools/しわ賢者/Input UI Comparison/Aggregate V2 Complete Sessions")]
        public static void AggregateAllCompleteSessions()
        {
            string root = Path.Combine(Application.persistentDataPath, "InputUIComparison");
            Directory.CreateDirectory(root);
            var outputDirectories = new List<string>();
            foreach (ExperimentType experimentType in Enum.GetValues(typeof(ExperimentType)))
            {
                string output = AggregateCompleteSessions(
                    root,
                    InputUIExperimentSchema.CurrentExperimentVersion,
                    experimentType);
                if (!string.IsNullOrEmpty(output)) outputDirectories.Add(output);
            }

            if (outputDirectories.Count == 0)
            {
                Debug.LogWarning("[InputUIComparison] No completed non-pilot v2 sessions were found under: " + root);
                EditorUtility.RevealInFinder(root);
                return;
            }

            Debug.Log("[InputUIComparison] Wrote version-separated participant summaries: "
                + string.Join(", ", outputDirectories));
            EditorUtility.RevealInFinder(outputDirectories[0]);
        }

        public static string AggregateCompleteSessions(
            string rootDirectory,
            string experimentVersion,
            ExperimentType experimentType)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("A results root is required.", nameof(rootDirectory));
            }
            if (string.IsNullOrWhiteSpace(experimentVersion))
            {
                throw new ArgumentException("An explicit experiment version is required.", nameof(experimentVersion));
            }
            if (!Directory.Exists(rootDirectory)) return null;

            List<SessionCandidate> candidates = LoadCompletedSessions(
                rootDirectory,
                experimentVersion,
                experimentType);
            if (candidates.Count == 0) return null;

            List<SessionCandidate> selectedCandidates = candidates
                .GroupBy(candidate => candidate.ParticipantId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(candidate => candidate.CompletedAtUtc).First())
                .ToList();
            string referenceConditionId = ValidateSharedContract(selectedCandidates);
            List<ExperimentParticipantConditionMetric> participantConditions = selectedCandidates
                .SelectMany(candidate => candidate.Metrics)
                .ToList();
            List<ExperimentParticipantDifferenceMetric> participantDifferences =
                InputUIExperimentAnalytics.CalculateParticipantDifferences(
                    participantConditions,
                    referenceConditionId);
            List<ExperimentGroupMetricSummary> groups =
                InputUIExperimentAnalytics.SummarizeGroups(
                    participantConditions,
                    participantDifferences,
                    referenceConditionId);
            return InputUIExperimentAnalytics.ExportAnalysisTables(
                rootDirectory,
                new ExperimentAnalysisTables(participantConditions, participantDifferences, groups),
                experimentVersion,
                experimentType);
        }

        private static List<SessionCandidate> LoadCompletedSessions(
            string rootDirectory,
            string experimentVersion,
            ExperimentType experimentType)
        {
            var result = new List<SessionCandidate>();
            foreach (string manifestPath in Directory.GetFiles(
                rootDirectory,
                "session_manifest.json",
                SearchOption.AllDirectories))
            {
                JObject manifest;
                try
                {
                    manifest = JObject.Parse(File.ReadAllText(manifestPath, Encoding.UTF8));
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[InputUIComparison] Ignoring unreadable manifest " + manifestPath + ": " + exception.Message);
                    continue;
                }

                string version = Text(manifest, "experiment_version");
                if (!string.Equals(version, experimentVersion, StringComparison.Ordinal)) continue;
                if (!TryExperimentType(manifest["experiment_type"], out ExperimentType manifestType)
                    || manifestType != experimentType) continue;
                string status = Text(manifest, "completion_status");
                if (!string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase)) continue;

                string participantId = Text(manifest, "participant_id");
                bool isPilot = Bool(manifest, "is_pilot") || InputUITrialPlan.IsPilotParticipant(participantId);
                if (isPilot) continue;
                string sessionId = Text(manifest, "session_id");
                if (string.IsNullOrWhiteSpace(participantId) || string.IsNullOrWhiteSpace(sessionId)) continue;
                if (!TryResolveSessionContract(manifest, experimentType, out SessionContract contract)) continue;

                string directory = Path.GetDirectoryName(manifestPath);
                string sessionDirectory = directory ?? rootDirectory;
                string exactSummaryPath = Path.Combine(sessionDirectory, "summary_participant_condition.csv");
                string summaryPath = File.Exists(exactSummaryPath)
                    ? exactSummaryPath
                    : Directory.GetFiles(
                            sessionDirectory,
                            "*_summary_participant_condition.csv",
                            SearchOption.TopDirectoryOnly)
                        .OrderByDescending(File.GetLastWriteTimeUtc)
                        .FirstOrDefault();
                if (string.IsNullOrEmpty(summaryPath)) continue;

                List<ExperimentParticipantConditionMetric> metrics = ParseParticipantConditionSummary(summaryPath)
                    .Where(row => string.Equals(row.ExperimentVersion, experimentVersion, StringComparison.Ordinal)
                        && row.ExperimentType == experimentType
                        && string.Equals(row.ParticipantId, participantId, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(row.SessionId, sessionId, StringComparison.Ordinal))
                    .ToList();
                string[] metricConditions = metrics.Select(row => row.Condition)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
                if (!metricConditions.SequenceEqual(contract.Conditions, StringComparer.Ordinal)) continue;

                string trialsPath = Directory.GetFiles(
                        sessionDirectory,
                        "*_trials.csv",
                        SearchOption.TopDirectoryOnly)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
                if (string.IsNullOrEmpty(trialsPath)) continue;
                List<MeasuredTrialEvidence> measuredTrials = ParseMeasuredTrialEvidence(trialsPath)
                    .Where(row => string.Equals(row.ExperimentVersion, experimentVersion, StringComparison.Ordinal)
                        && row.ExperimentType == experimentType
                        && string.Equals(row.ParticipantId, participantId, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(row.SessionId, sessionId, StringComparison.Ordinal)
                        && !row.IsPilot
                        && !row.IsPractice)
                    .ToList();
                if (!HasCompleteMeasuredTrials(measuredTrials, contract)) continue;

                DateTime completedAt = ParseUtc(Text(manifest, "completed_at"));
                if (completedAt == DateTime.MinValue) completedAt = File.GetLastWriteTimeUtc(manifestPath);
                result.Add(new SessionCandidate
                {
                    ParticipantId = participantId,
                    SessionId = sessionId,
                    CompletedAtUtc = completedAt,
                    ProtocolId = contract.ProtocolId,
                    ReferenceConditionId = contract.ReferenceConditionId,
                    Conditions = contract.Conditions,
                    Metrics = metrics,
                });
            }
            return result;
        }

        private static string ValidateSharedContract(IReadOnlyList<SessionCandidate> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                throw new InvalidOperationException("No complete session contract was available for aggregation.");
            }

            SessionCandidate first = candidates[0];
            for (int i = 1; i < candidates.Count; i++)
            {
                SessionCandidate candidate = candidates[i];
                if (!string.Equals(candidate.ProtocolId, first.ProtocolId, StringComparison.Ordinal)
                    || !string.Equals(candidate.ReferenceConditionId, first.ReferenceConditionId, StringComparison.Ordinal)
                    || !candidate.Conditions.SequenceEqual(first.Conditions, StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Completed sessions use different preregistered condition/reference contracts and cannot be pooled.");
                }
            }

            return first.ReferenceConditionId;
        }

        private static bool TryResolveSessionContract(
            JObject manifest,
            ExperimentType experimentType,
            out SessionContract contract)
        {
            contract = null;
            string[] orderedConditions = StringArray(manifest["condition_order"]);
            string[] orderedTrialSets = StringArray(manifest["trial_set_order"]);
            if (orderedConditions.Length == 0
                || orderedConditions.Length != orderedTrialSets.Length
                || orderedConditions.Any(string.IsNullOrWhiteSpace)
                || orderedTrialSets.Any(string.IsNullOrWhiteSpace)
                || orderedConditions.Distinct(StringComparer.Ordinal).Count() != orderedConditions.Length)
            {
                return false;
            }

            string[] conditions = orderedConditions
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            string protocolId = string.Empty;
            string referenceConditionId = Text(manifest, "reference_condition_id");
            if (experimentType == ExperimentType.RushPackage)
            {
                protocolId = Text(manifest, "rush_protocol_id");
                HumanRushCandidateDefinition[] definitions = ParseConditionDefinitions(
                    manifest["condition_definitions"]);
                var registration = new HumanRushRegistration(
                    protocolId,
                    referenceConditionId,
                    definitions);
                if (!registration.TryValidate(out _)) return false;
                string[] registeredConditions = definitions.Select(value => value.ConditionId)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
                if (!conditions.SequenceEqual(registeredConditions, StringComparer.Ordinal)) return false;
            }
            else
            {
                string expectedReference = InputUIExperimentAnalytics.ReferenceCondition(experimentType);
                if (string.IsNullOrWhiteSpace(referenceConditionId)) referenceConditionId = expectedReference;
                if (!string.Equals(referenceConditionId, expectedReference, StringComparison.Ordinal)) return false;
                string[] expectedConditions = InputUITrialPlan.GetConditionIds(experimentType)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
                if (!conditions.SequenceEqual(expectedConditions, StringComparer.Ordinal)) return false;
            }

            if (!conditions.Contains(referenceConditionId, StringComparer.Ordinal)) return false;
            var trialSetByCondition = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < orderedConditions.Length; i++)
            {
                trialSetByCondition.Add(orderedConditions[i], orderedTrialSets[i]);
            }
            contract = new SessionContract
            {
                ProtocolId = protocolId,
                ReferenceConditionId = referenceConditionId,
                Conditions = conditions,
                TrialSetByCondition = trialSetByCondition,
            };
            return true;
        }

        private static HumanRushCandidateDefinition[] ParseConditionDefinitions(JToken token)
        {
            if (!(token is JArray array)) return Array.Empty<HumanRushCandidateDefinition>();
            var result = new List<HumanRushCandidateDefinition>(array.Count);
            foreach (JToken item in array)
            {
                if (!(item is JObject definition)
                    || !TryEnumValue(definition["repair_anchor_mode"], out RepairAnchorMode anchor)
                    || !TryEnumValue(definition["repair_assist_mode"], out RepairAssistMode assist)
                    || !TryEnumValue(definition["vehicle_button_layout_mode"], out VehicleButtonLayoutMode layout))
                {
                    return Array.Empty<HumanRushCandidateDefinition>();
                }
                result.Add(new HumanRushCandidateDefinition(
                    Text(definition, "condition_id"),
                    anchor,
                    assist,
                    layout));
            }
            return result.ToArray();
        }

        private static bool TryEnumValue<TEnum>(JToken token, out TEnum value)
            where TEnum : struct
        {
            value = default;
            if (token == null) return false;
            if (!Enum.TryParse(token.ToString(), true, out value)) return false;
            return Enum.IsDefined(typeof(TEnum), value);
        }

        private static string[] StringArray(JToken token)
        {
            return token is JArray array
                ? array.Select(value => value?.ToString() ?? string.Empty).ToArray()
                : Array.Empty<string>();
        }

        private static List<MeasuredTrialEvidence> ParseMeasuredTrialEvidence(string path)
        {
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length < 2) return new List<MeasuredTrialEvidence>();
            List<string> header = ParseCsvLine(lines[0]);
            var index = header.Select((name, position) => (name, position))
                .ToDictionary(pair => pair.name, pair => pair.position, StringComparer.Ordinal);
            var result = new List<MeasuredTrialEvidence>();
            for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                List<string> fields = ParseCsvLine(lines[lineIndex]);
                if (!TryExperimentType(Field(fields, index, "experiment_type"), out ExperimentType type)
                    || !int.TryParse(
                        Field(fields, index, "trial_number"),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int trialNumber)
                    || !TryBoolField(fields, index, "is_pilot", out bool isPilot)
                    || !TryBoolField(fields, index, "is_practice", out bool isPractice))
                {
                    return new List<MeasuredTrialEvidence>();
                }
                result.Add(new MeasuredTrialEvidence
                {
                    ExperimentVersion = Field(fields, index, "experiment_version"),
                    ExperimentType = type,
                    ParticipantId = Field(fields, index, "participant_id"),
                    SessionId = Field(fields, index, "session_id"),
                    Condition = Field(fields, index, "condition"),
                    TrialSetId = Field(fields, index, "trial_set_id"),
                    IsPilot = isPilot,
                    IsPractice = isPractice,
                    TrialNumber = trialNumber,
                });
            }
            return result;
        }

        private static bool HasCompleteMeasuredTrials(
            IReadOnlyList<MeasuredTrialEvidence> measuredTrials,
            SessionContract contract)
        {
            int expectedPerCondition = InputUITrialPlan.MeasuredTrialsPerVariant;
            if (measuredTrials == null
                || measuredTrials.Count != contract.Conditions.Length * expectedPerCondition)
            {
                return false;
            }
            if (measuredTrials.Any(row => !contract.Conditions.Contains(row.Condition, StringComparer.Ordinal)))
            {
                return false;
            }

            int[] expectedNumbers = Enumerable.Range(1, expectedPerCondition).ToArray();
            foreach (string condition in contract.Conditions)
            {
                List<MeasuredTrialEvidence> rows = measuredTrials
                    .Where(row => string.Equals(row.Condition, condition, StringComparison.Ordinal))
                    .ToList();
                if (rows.Count != expectedPerCondition
                    || !rows.Select(row => row.TrialNumber).Distinct().OrderBy(value => value)
                        .SequenceEqual(expectedNumbers)
                    || !contract.TrialSetByCondition.TryGetValue(condition, out string trialSetId)
                    || rows.Any(row => !string.Equals(row.TrialSetId, trialSetId, StringComparison.Ordinal)))
                {
                    return false;
                }
            }
            return true;
        }

        private static List<ExperimentParticipantConditionMetric> ParseParticipantConditionSummary(string path)
        {
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length < 2) return new List<ExperimentParticipantConditionMetric>();
            List<string> header = ParseCsvLine(lines[0]);
            var index = header.Select((name, position) => (name, position))
                .ToDictionary(pair => pair.name, pair => pair.position, StringComparer.Ordinal);
            var result = new List<ExperimentParticipantConditionMetric>();
            for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                List<string> fields = ParseCsvLine(lines[lineIndex]);
                if (!TryExperimentType(Field(fields, index, "experiment_type"), out ExperimentType experimentType)) continue;
                result.Add(new ExperimentParticipantConditionMetric
                {
                    SchemaVersion = Field(fields, index, "schema_version"),
                    ExperimentVersion = Field(fields, index, "experiment_version"),
                    ExperimentType = experimentType,
                    ParticipantId = Field(fields, index, "participant_id"),
                    SessionId = Field(fields, index, "session_id"),
                    Condition = Field(fields, index, "condition"),
                    ConditionOrder = IntField(fields, index, "condition_order"),
                    TrialSetId = Field(fields, index, "trial_set_id"),
                    Metric = Field(fields, index, "metric"),
                    MetricRole = string.IsNullOrWhiteSpace(Field(fields, index, "metric_role"))
                        ? InputUIExperimentAnalytics.MetricRole(experimentType, Field(fields, index, "metric"))
                        : Field(fields, index, "metric_role"),
                    Numerator = DoubleField(fields, index, "numerator"),
                    Denominator = DoubleField(fields, index, "denominator"),
                    Value = DoubleField(fields, index, "value"),
                });
            }
            return result;
        }

        private static bool TryExperimentType(JToken token, out ExperimentType experimentType)
        {
            experimentType = default;
            if (token == null) return false;
            if (token.Type == JTokenType.Integer)
            {
                int value = token.Value<int>();
                if (Enum.IsDefined(typeof(ExperimentType), value))
                {
                    experimentType = (ExperimentType)value;
                    return true;
                }
                return false;
            }
            return TryExperimentType(token.ToString(), out experimentType);
        }

        private static bool TryExperimentType(string value, out ExperimentType experimentType)
        {
            string normalized = (value ?? string.Empty).Trim().Replace("-", string.Empty).Replace("_", string.Empty);
            foreach (ExperimentType candidate in Enum.GetValues(typeof(ExperimentType)))
            {
                if (string.Equals(normalized, candidate.ToString(), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, InputUIComparisonNaming.ExperimentTypeId(candidate), StringComparison.OrdinalIgnoreCase))
                {
                    experimentType = candidate;
                    return true;
                }
            }
            experimentType = default;
            return false;
        }

        private static string Text(JObject value, string name)
        {
            return value[name]?.ToString() ?? string.Empty;
        }

        private static bool Bool(JObject value, string name)
        {
            JToken token = value[name];
            return token != null && (token.Type == JTokenType.Boolean
                ? token.Value<bool>()
                : string.Equals(token.ToString(), "true", StringComparison.OrdinalIgnoreCase));
        }

        private static DateTime ParseUtc(string value)
        {
            return DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime parsed)
                ? parsed
                : DateTime.MinValue;
        }

        private static string Field(
            IReadOnlyList<string> fields,
            IReadOnlyDictionary<string, int> index,
            string name)
        {
            return index.TryGetValue(name, out int position) && position < fields.Count
                ? fields[position]
                : string.Empty;
        }

        private static int IntField(
            IReadOnlyList<string> fields,
            IReadOnlyDictionary<string, int> index,
            string name)
        {
            return int.TryParse(Field(fields, index, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value
                : 0;
        }

        private static bool TryBoolField(
            IReadOnlyList<string> fields,
            IReadOnlyDictionary<string, int> index,
            string name,
            out bool value)
        {
            return bool.TryParse(Field(fields, index, name), out value);
        }

        private static double DoubleField(
            IReadOnlyList<string> fields,
            IReadOnlyDictionary<string, int> index,
            string name)
        {
            return double.TryParse(Field(fields, index, name), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? value
                : double.NaN;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var value = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char character = line[i];
                if (character == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        value.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                }
                else if (character == ',' && !quoted)
                {
                    result.Add(value.ToString());
                    value.Length = 0;
                }
                else
                {
                    value.Append(character);
                }
            }
            result.Add(value.ToString());
            return result;
        }
    }
}
