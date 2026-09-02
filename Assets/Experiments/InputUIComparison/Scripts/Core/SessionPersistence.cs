using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Siwakenja.Experiments.InputUIComparison
{
    [Serializable]
    public sealed class ExperimentVehicleCheckpoint
    {
        public string VehicleId;
        public ExperimentTrial Trial;
        public double SpawnedAtSessionMs;
        public float NormalizedProgress;
        public bool CoveredRevealed;
        public bool IsCurrentTarget;
        public bool IsResolved;
    }

    [Serializable]
    public sealed class ExperimentRectCheckpoint
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public static ExperimentRectCheckpoint FromRect(Rect rect)
        {
            return new ExperimentRectCheckpoint
            {
                X = rect.x,
                Y = rect.y,
                Width = rect.width,
                Height = rect.height,
            };
        }

        public Rect ToRect()
        {
            return new Rect(X, Y, Width, Height);
        }
    }

    /// <summary>
    /// Complete resumable runtime state. Controllers should finalize the current
    /// frame's raw inputs before saving when possible; PendingInputRecords exists for
    /// lifecycle callbacks that happen between physical capture and arbitration.
    /// </summary>
    [Serializable]
    public sealed class ExperimentSessionCheckpoint
    {
        public string SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion;
        public string ExperimentVersion = InputUIExperimentSchema.CurrentExperimentVersion;
        public string ParticipantId;
        public string SessionId;
        public bool IsPilot;
        public long Revision;
        public string SavedAt;
        public string CompletionStatus = SessionPersistence.StatusInProgress;
        public ExperimentSessionManifest Manifest;

        public string Phase;
        public int ConditionIndex;
        public int TrialIndex;
        public int EventIndex;
        public int Score;
        public int Misses;
        public double SessionElapsedMs;
        public double TrialElapsedMs;
        public double ActionableElapsedMs;
        public double RemainingGapMs;
        public double LastAcceptedEventTimeMs = -1d;
        public string RandomState;
        public string CurrentTargetVehicleId;
        public bool CoveredRevealed;
        public bool TrialResolving;
        public ExperimentRectCheckpoint CurrentRepairRect;
        public ExperimentRectCheckpoint PreviousRepairRect;
        public double RepairRectChangedAgeMs = -1d;

        public List<ExperimentTrial> ActiveTrials = new List<ExperimentTrial>();
        public List<ExperimentVehicleCheckpoint> ActiveVehicles = new List<ExperimentVehicleCheckpoint>();
        public ExperimentTrialRecord CurrentTrialRecord;
        public List<ExperimentTrialRecord> TrialRecords = new List<ExperimentTrialRecord>();
        public List<ExperimentInputEventRecord> InputEventRecords = new List<ExperimentInputEventRecord>();
        public List<ExperimentInputEventRecord> PendingInputRecords = new List<ExperimentInputEventRecord>();
        public List<ExperimentSurveyRecord> SurveyRecords = new List<ExperimentSurveyRecord>();
    }

    /// <summary>
    /// Versioned, crash-tolerant persistence for one experiment session. Manifest JSON
    /// is human-readable; checkpoints alternate between two checksum-protected slots,
    /// so a torn write never destroys the last valid resume point.
    /// </summary>
    public sealed class SessionPersistence
    {
        public const string StatusInProgress = "in_progress";
        public const string StatusInterrupted = "interrupted";
        public const string StatusCompleted = "completed";

        private const string ProductDirectoryName = "InputUIComparison";
        private const string SchemaDirectoryName = "v2";
        private const string ManifestFileName = "session_manifest.json";
        private const string CheckpointAFileName = "checkpoint.a.json";
        private const string CheckpointBFileName = "checkpoint.b.json";
        private const string RawEventWalFileName = "raw_events.wal.jsonl";

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private static readonly JsonSerializerSettings JsonSettings = CreateJsonSettings(false);
        private static readonly JsonSerializerSettings CheckpointJsonSettings = CreateJsonSettings(true);

        private readonly object _gate = new object();
        private readonly string _participantId;
        private readonly string _sessionId;
        private readonly string _experimentVersion;

        public SessionPersistence(
            string participantId,
            string sessionId,
            string experimentVersion = null,
            string persistentDataPathOverride = null)
        {
            _participantId = RequireIdentity(participantId, nameof(participantId));
            _sessionId = RequireIdentity(sessionId, nameof(sessionId));
            _experimentVersion = string.IsNullOrWhiteSpace(experimentVersion)
                ? InputUIExperimentSchema.CurrentExperimentVersion
                : experimentVersion.Trim();

            string persistentRoot = string.IsNullOrWhiteSpace(persistentDataPathOverride)
                ? Application.persistentDataPath
                : Path.GetFullPath(persistentDataPathOverride);
            VersionDirectory = Path.Combine(
                persistentRoot,
                ProductDirectoryName,
                SchemaDirectoryName,
                SanitizePathSegment(_experimentVersion));
            SessionDirectory = Path.Combine(VersionDirectory, SanitizePathSegment(_sessionId));
            ManifestPath = Path.Combine(SessionDirectory, ManifestFileName);
            CheckpointAPath = Path.Combine(SessionDirectory, CheckpointAFileName);
            CheckpointBPath = Path.Combine(SessionDirectory, CheckpointBFileName);
            RawEventWalPath = Path.Combine(SessionDirectory, RawEventWalFileName);
        }

        public string VersionDirectory { get; }
        public string SessionDirectory { get; }
        public string ManifestPath { get; }
        public string CheckpointAPath { get; }
        public string CheckpointBPath { get; }
        public string RawEventWalPath { get; }

        public static bool TryFindLatestIncompleteSession(
            string participantId,
            out ExperimentSessionManifest manifest,
            string experimentVersion = null,
            string persistentDataPathOverride = null)
        {
            string requiredParticipant = RequireIdentity(participantId, nameof(participantId));
            string requiredVersion = string.IsNullOrWhiteSpace(experimentVersion)
                ? InputUIExperimentSchema.CurrentExperimentVersion
                : experimentVersion.Trim();
            string persistentRoot = string.IsNullOrWhiteSpace(persistentDataPathOverride)
                ? Application.persistentDataPath
                : Path.GetFullPath(persistentDataPathOverride);
            string versionDirectory = Path.Combine(
                persistentRoot,
                ProductDirectoryName,
                SchemaDirectoryName,
                SanitizePathSegment(requiredVersion));
            manifest = null;
            if (!Directory.Exists(versionDirectory)) return false;

            DateTime latestWrite = DateTime.MinValue;
            foreach (string path in Directory.GetFiles(versionDirectory, ManifestFileName, SearchOption.AllDirectories))
            {
                bool read = TryReadJson(path, out ExperimentSessionManifest candidate);
                if (!read) read = TryReadJson(path + ".backup", out candidate);
                if (!read
                    || candidate == null
                    || !string.Equals(candidate.SchemaVersion, InputUIExperimentSchema.CurrentSchemaVersion, StringComparison.Ordinal)
                    || !string.Equals(candidate.ExperimentVersion, requiredVersion, StringComparison.Ordinal)
                    || !string.Equals(candidate.ParticipantId, requiredParticipant, StringComparison.Ordinal)
                    || string.IsNullOrWhiteSpace(candidate.SessionId))
                {
                    continue;
                }

                if (string.Equals(candidate.CompletionStatus, StatusCompleted, StringComparison.Ordinal)) continue;

                // Completion is committed checkpoint-first and manifest-second. If
                // the process dies between those two durable writes, repair only the
                // completion fields on the original manifest and never offer the
                // already-completed session as resumable.
                var persistence = new SessionPersistence(
                    candidate.ParticipantId,
                    candidate.SessionId,
                    candidate.ExperimentVersion,
                    persistentRoot);
                if (persistence.TryLoadLatestCheckpoint(out ExperimentSessionCheckpoint checkpoint)
                    && IsCompletedCheckpoint(checkpoint))
                {
                    candidate.CompletionStatus = StatusCompleted;
                    candidate.CompletedAt = string.IsNullOrWhiteSpace(checkpoint.SavedAt)
                        ? UtcNow()
                        : checkpoint.SavedAt;
                    persistence.SaveManifest(candidate);
                    continue;
                }

                DateTime writeTime = File.GetLastWriteTimeUtc(path);
                if (manifest != null && writeTime <= latestWrite) continue;
                latestWrite = writeTime;
                manifest = candidate;
            }

            return manifest != null;
        }

        public static ExperimentSessionManifest CreateManifestSnapshot(
            string participantId,
            string sessionId,
            ExperimentType experimentType,
            IEnumerable<ExperimentConditionAssignment> assignments,
            RepairAnchorMode repairAnchorMode,
            RepairAssistMode repairAssistMode,
            VehicleButtonLayoutMode vehicleButtonLayoutMode,
            bool isPilot,
            string gitCommit = "unknown",
            string experimentVersion = null,
            string rushProtocolId = null,
            string referenceConditionId = null,
            IEnumerable<HumanRushCandidateDefinition> conditionDefinitions = null)
        {
            ExperimentConditionAssignment[] assignmentArray = assignments?.ToArray()
                ?? Array.Empty<ExperimentConditionAssignment>();
            HumanRushCandidateDefinition[] definitionArray = conditionDefinitions == null
                ? assignmentArray.Select(value => new HumanRushCandidateDefinition(
                    value.Condition,
                    value.RepairAnchorMode,
                    value.RepairAssistMode,
                    value.VehicleButtonLayoutMode)).ToArray()
                : conditionDefinitions.Select(value => value?.Clone()).ToArray();
            string resolvedReference = string.IsNullOrWhiteSpace(referenceConditionId)
                ? DefaultReferenceCondition(experimentType)
                : referenceConditionId.Trim();
            string resolvedRushProtocol = experimentType == ExperimentType.RushPackage
                ? string.IsNullOrWhiteSpace(rushProtocolId)
                    ? isPilot ? InputUITrialPlan.P00RushProtocolId : string.Empty
                    : rushProtocolId.Trim()
                : string.Empty;
            if (experimentType == ExperimentType.RushPackage && !isPilot)
            {
                var registration = new HumanRushRegistration(
                    resolvedRushProtocol,
                    resolvedReference,
                    definitionArray);
                if (!registration.TryValidate(out string registrationError))
                {
                    throw new InvalidOperationException(
                        "Cannot create a non-pilot Rush manifest without a valid preregistration: "
                        + registrationError);
                }

                string[] assignedConditions = assignmentArray.Select(value => value.Condition)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
                string[] registeredConditions = definitionArray.Select(value => value.ConditionId)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
                if (!assignedConditions.SequenceEqual(registeredConditions, StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Human Rush manifest assignments do not match the preregistered candidates.");
                }
            }
            Rect safeArea = Screen.safeArea;
            return new ExperimentSessionManifest
            {
                SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion,
                ExperimentVersion = string.IsNullOrWhiteSpace(experimentVersion)
                    ? InputUIExperimentSchema.CurrentExperimentVersion
                    : experimentVersion.Trim(),
                ParticipantId = participantId ?? string.Empty,
                SessionId = sessionId ?? string.Empty,
                IsPilot = isPilot,
                DeviceModel = SystemInfo.deviceModel ?? string.Empty,
                Platform = Application.platform.ToString(),
                ScreenResolution = Screen.width.ToString(CultureInfo.InvariantCulture)
                    + "x" + Screen.height.ToString(CultureInfo.InvariantCulture),
                SafeArea = RectValue(safeArea),
                UnityVersion = Application.unityVersion ?? string.Empty,
                BuildVersion = Application.version ?? string.Empty,
                GitCommit = string.IsNullOrWhiteSpace(gitCommit) ? "unknown" : gitCommit.Trim(),
                ExperimentType = experimentType,
                ConditionOrder = assignmentArray.Select(value => value.Condition).ToArray(),
                TrialSetOrder = assignmentArray.Select(value => value.TrialSetId).ToArray(),
                SeedByCondition = assignmentArray.Select(value => new ExperimentConditionSeed
                {
                    Condition = value.Condition,
                    TrialSetId = value.TrialSetId,
                    Seed = value.Seed,
                }).ToArray(),
                RepairAnchorMode = repairAnchorMode,
                RepairAssistMode = repairAssistMode,
                VehicleButtonLayoutMode = vehicleButtonLayoutMode,
                RushProtocolId = resolvedRushProtocol,
                ReferenceConditionId = resolvedReference,
                ConditionDefinitions = definitionArray,
                StartedAt = UtcNow(),
                CompletedAt = string.Empty,
                CompletionStatus = StatusInProgress,
            };
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

        public void BeginSession(ExperimentSessionManifest manifest)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            NormalizeManifestIdentity(manifest);
            manifest.CompletionStatus = StatusInProgress;
            manifest.CompletedAt = string.Empty;
            if (string.IsNullOrWhiteSpace(manifest.StartedAt)) manifest.StartedAt = UtcNow();
            SaveManifest(manifest);
        }

        public void SaveManifest(ExperimentSessionManifest manifest)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            lock (_gate)
            {
                NormalizeManifestIdentity(manifest);
                Directory.CreateDirectory(SessionDirectory);
                WriteTextAtomic(ManifestPath, Serialize(manifest), true);
            }
        }

        public void MarkInterrupted(ExperimentSessionManifest manifest)
        {
            MarkFinished(manifest, StatusInterrupted);
        }

        public void MarkCompleted(ExperimentSessionManifest manifest)
        {
            MarkFinished(manifest, StatusCompleted);
        }

        public long MarkInterrupted(
            ExperimentSessionManifest manifest,
            ExperimentSessionCheckpoint checkpoint)
        {
            return SaveFinishedState(manifest, checkpoint, StatusInterrupted);
        }

        public long MarkCompleted(
            ExperimentSessionManifest manifest,
            ExperimentSessionCheckpoint checkpoint)
        {
            return SaveFinishedState(manifest, checkpoint, StatusCompleted);
        }

        public bool TryLoadManifest(out ExperimentSessionManifest manifest)
        {
            lock (_gate)
            {
                if (TryReadJson(ManifestPath, out manifest) && IsMatchingManifest(manifest)) return true;
                if (TryReadJson(ManifestPath + ".backup", out manifest) && IsMatchingManifest(manifest)) return true;
                manifest = null;
                return false;
            }
        }

        public long SaveCheckpoint(ExperimentSessionCheckpoint checkpoint)
        {
            if (checkpoint == null) throw new ArgumentNullException(nameof(checkpoint));
            lock (_gate)
            {
                ValidateIdentityField(checkpoint.ParticipantId, _participantId, "participant", allowEmpty: true);
                ValidateIdentityField(checkpoint.SessionId, _sessionId, "session", allowEmpty: true);
                if (checkpoint.Manifest != null) NormalizeManifestIdentity(checkpoint.Manifest);
                NormalizeCheckpointCollections(checkpoint);
                Directory.CreateDirectory(SessionDirectory);
                long latestRevision = ReadValidCheckpointEnvelopes()
                    .Select(value => value.Envelope.Revision)
                    .DefaultIfEmpty(0L)
                    .Max();
                checkpoint.SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion;
                checkpoint.ExperimentVersion = _experimentVersion;
                checkpoint.ParticipantId = _participantId;
                checkpoint.SessionId = _sessionId;
                checkpoint.Revision = latestRevision + 1L;
                checkpoint.SavedAt = UtcNow();
                if (string.IsNullOrWhiteSpace(checkpoint.CompletionStatus))
                {
                    checkpoint.CompletionStatus = StatusInProgress;
                }

                string payload = SerializeCheckpoint(checkpoint);
                var envelope = new CheckpointEnvelope
                {
                    Revision = checkpoint.Revision,
                    SchemaVersion = checkpoint.SchemaVersion,
                    ExperimentVersion = checkpoint.ExperimentVersion,
                    ParticipantId = checkpoint.ParticipantId,
                    SessionId = checkpoint.SessionId,
                    PayloadSha256 = Sha256(payload),
                    PayloadJson = payload,
                };
                string destination = checkpoint.Revision % 2L == 1L
                    ? CheckpointAPath
                    : CheckpointBPath;
                WriteTextAtomic(destination, Serialize(envelope), false);
                return checkpoint.Revision;
            }
        }

        public bool TryLoadLatestCheckpoint(out ExperimentSessionCheckpoint checkpoint)
        {
            lock (_gate)
            {
                ValidCheckpoint latest = ReadValidCheckpointEnvelopes()
                    .OrderByDescending(value => value.Envelope.Revision)
                    .FirstOrDefault();
                if (latest == null)
                {
                    checkpoint = null;
                    return false;
                }

                checkpoint = latest.Checkpoint;
                NormalizeCheckpointCollections(checkpoint);
                return true;
            }
        }

        /// <summary>
        /// Synchronously appends one capture-time Raw row before gameplay arbitration.
        /// The line-oriented checksum envelope is a write-ahead log: a force-stop
        /// after Pointer Down but before LateUpdate/checkpoint cannot erase the tap.
        /// </summary>
        public void AppendRawEventJournal(ExperimentInputEventRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            lock (_gate)
            {
                ValidateRawEventIdentity(record);
                if (record.EventIndex <= 0)
                {
                    throw new InvalidDataException("Raw event_index must be positive before journaling.");
                }

                Directory.CreateDirectory(SessionDirectory);
                string payload = SerializeCompact(record);
                var envelope = new RawEventWalEnvelope
                {
                    SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion,
                    ExperimentVersion = _experimentVersion,
                    ParticipantId = _participantId,
                    SessionId = _sessionId,
                    EventIndex = record.EventIndex,
                    PayloadSha256 = Sha256(payload),
                    PayloadJson = payload,
                };
                byte[] bytes = Utf8NoBom.GetBytes(SerializeCompact(envelope) + "\n");
                using (var stream = new FileStream(
                    RawEventWalPath,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.Read,
                    4096,
                    FileOptions.WriteThrough))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
            }
        }

        /// <summary>
        /// Returns valid WAL rows in session order. A torn final line, corrupt line,
        /// wrong identity, or duplicate event index is ignored fail-closed.
        /// </summary>
        public IReadOnlyList<ExperimentInputEventRecord> ReadRawEventJournal()
        {
            lock (_gate)
            {
                var byEventIndex = new Dictionary<int, ExperimentInputEventRecord>();
                if (!File.Exists(RawEventWalPath)) return Array.Empty<ExperimentInputEventRecord>();

                try
                {
                    using (var stream = new FileStream(
                        RawEventWalPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite))
                    using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            try
                            {
                                RawEventWalEnvelope envelope = Deserialize<RawEventWalEnvelope>(line);
                                if (envelope == null
                                    || envelope.EventIndex <= 0
                                    || byEventIndex.ContainsKey(envelope.EventIndex)
                                    || !string.Equals(envelope.SchemaVersion, InputUIExperimentSchema.CurrentSchemaVersion, StringComparison.Ordinal)
                                    || !string.Equals(envelope.ExperimentVersion, _experimentVersion, StringComparison.Ordinal)
                                    || !string.Equals(envelope.ParticipantId, _participantId, StringComparison.Ordinal)
                                    || !string.Equals(envelope.SessionId, _sessionId, StringComparison.Ordinal)
                                    || string.IsNullOrEmpty(envelope.PayloadJson)
                                    || !FixedTimeEquals(envelope.PayloadSha256, Sha256(envelope.PayloadJson)))
                                {
                                    continue;
                                }

                                ExperimentInputEventRecord record = Deserialize<ExperimentInputEventRecord>(envelope.PayloadJson);
                                ValidateRawEventIdentity(record);
                                if (record.EventIndex != envelope.EventIndex) continue;
                                byEventIndex.Add(record.EventIndex, record);
                            }
                            catch (Exception exception) when (IsRecoverableReadException(exception))
                            {
                                // Includes a partial trailing line after force-stop.
                            }
                        }
                    }
                }
                catch (Exception exception) when (IsRecoverableReadException(exception))
                {
                    return Array.Empty<ExperimentInputEventRecord>();
                }

                return byEventIndex
                    .OrderBy(pair => pair.Key)
                    .Select(pair => pair.Value)
                    .ToArray();
            }
        }

        private void MarkFinished(ExperimentSessionManifest manifest, string status)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            manifest.CompletionStatus = status;
            manifest.CompletedAt = string.Equals(status, StatusCompleted, StringComparison.Ordinal)
                ? UtcNow()
                : string.Empty;
            SaveManifest(manifest);
        }

        private long SaveFinishedState(
            ExperimentSessionManifest manifest,
            ExperimentSessionCheckpoint checkpoint,
            string status)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            if (checkpoint == null) throw new ArgumentNullException(nameof(checkpoint));
            checkpoint.CompletionStatus = status;
            long revision = SaveCheckpoint(checkpoint);
            MarkFinished(manifest, status);
            return revision;
        }

        private IEnumerable<ValidCheckpoint> ReadValidCheckpointEnvelopes()
        {
            string[] paths =
            {
                CheckpointAPath,
                CheckpointBPath,
            };
            for (int i = 0; i < paths.Length; i++)
            {
                if (!TryReadJson(paths[i], out CheckpointEnvelope envelope)
                    || envelope == null
                    || string.IsNullOrEmpty(envelope.PayloadJson)
                    || !FixedTimeEquals(envelope.PayloadSha256, Sha256(envelope.PayloadJson)))
                {
                    continue;
                }

                ExperimentSessionCheckpoint parsed;
                try
                {
                    parsed = DeserializeCheckpoint(envelope.PayloadJson);
                    ValidateCheckpointIdentity(parsed, allowEmpty: false);
                }
                catch (Exception exception) when (IsRecoverableReadException(exception))
                {
                    continue;
                }

                if (parsed == null || parsed.Revision != envelope.Revision) continue;
                yield return new ValidCheckpoint(envelope, parsed);
            }
        }

        private void NormalizeManifestIdentity(ExperimentSessionManifest manifest)
        {
            if (!string.IsNullOrWhiteSpace(manifest.ParticipantId)
                && !string.Equals(manifest.ParticipantId, _participantId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Manifest participant does not match this persistence instance.");
            }

            if (!string.IsNullOrWhiteSpace(manifest.SessionId)
                && !string.Equals(manifest.SessionId, _sessionId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Manifest session does not match this persistence instance.");
            }

            if (!string.IsNullOrWhiteSpace(manifest.ExperimentVersion)
                && !string.Equals(manifest.ExperimentVersion, _experimentVersion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Manifest experiment version does not match this persistence instance.");
            }

            manifest.SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion;
            manifest.ExperimentVersion = _experimentVersion;
            manifest.ParticipantId = _participantId;
            manifest.SessionId = _sessionId;
            manifest.GitCommit = string.IsNullOrWhiteSpace(manifest.GitCommit) ? "unknown" : manifest.GitCommit;
        }

        private bool IsMatchingManifest(ExperimentSessionManifest manifest)
        {
            return manifest != null
                && string.Equals(manifest.SchemaVersion, InputUIExperimentSchema.CurrentSchemaVersion, StringComparison.Ordinal)
                && string.Equals(manifest.ExperimentVersion, _experimentVersion, StringComparison.Ordinal)
                && string.Equals(manifest.ParticipantId, _participantId, StringComparison.Ordinal)
                && string.Equals(manifest.SessionId, _sessionId, StringComparison.Ordinal);
        }

        private void ValidateCheckpointIdentity(ExperimentSessionCheckpoint checkpoint, bool allowEmpty)
        {
            if (checkpoint == null) throw new InvalidDataException("Checkpoint payload is null.");
            ValidateIdentityField(checkpoint.SchemaVersion, InputUIExperimentSchema.CurrentSchemaVersion, "schema", allowEmpty);
            ValidateIdentityField(checkpoint.ExperimentVersion, _experimentVersion, "experiment version", allowEmpty);
            ValidateIdentityField(checkpoint.ParticipantId, _participantId, "participant", allowEmpty);
            ValidateIdentityField(checkpoint.SessionId, _sessionId, "session", allowEmpty);
        }

        private void ValidateRawEventIdentity(ExperimentInputEventRecord record)
        {
            if (record == null) throw new InvalidDataException("Raw event payload is null.");
            ValidateIdentityField(record.SchemaVersion, InputUIExperimentSchema.CurrentSchemaVersion, "raw event schema", false);
            ValidateIdentityField(record.ExperimentVersion, _experimentVersion, "raw event experiment version", false);
            ValidateIdentityField(record.ParticipantId, _participantId, "raw event participant", false);
            ValidateIdentityField(record.SessionId, _sessionId, "raw event session", false);
        }

        private static bool IsCompletedCheckpoint(ExperimentSessionCheckpoint checkpoint)
        {
            return checkpoint != null
                && (string.Equals(checkpoint.CompletionStatus, StatusCompleted, StringComparison.Ordinal)
                    || string.Equals(checkpoint.Phase, "Complete", StringComparison.Ordinal));
        }

        private static void NormalizeCheckpointCollections(ExperimentSessionCheckpoint checkpoint)
        {
            checkpoint.ActiveTrials = checkpoint.ActiveTrials ?? new List<ExperimentTrial>();
            checkpoint.ActiveVehicles = checkpoint.ActiveVehicles ?? new List<ExperimentVehicleCheckpoint>();
            checkpoint.TrialRecords = checkpoint.TrialRecords ?? new List<ExperimentTrialRecord>();
            checkpoint.InputEventRecords = checkpoint.InputEventRecords ?? new List<ExperimentInputEventRecord>();
            checkpoint.PendingInputRecords = checkpoint.PendingInputRecords ?? new List<ExperimentInputEventRecord>();
            checkpoint.SurveyRecords = checkpoint.SurveyRecords ?? new List<ExperimentSurveyRecord>();
        }

        private static void ValidateIdentityField(string actual, string expected, string label, bool allowEmpty)
        {
            if (allowEmpty && string.IsNullOrWhiteSpace(actual)) return;
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Checkpoint " + label + " does not match the requested session.");
            }
        }

        private static bool TryReadJson<T>(string path, out T value) where T : class
        {
            value = null;
            if (!File.Exists(path)) return false;
            try
            {
                value = Deserialize<T>(File.ReadAllText(path, Encoding.UTF8));
                return value != null;
            }
            catch (Exception exception) when (IsRecoverableReadException(exception))
            {
                value = null;
                return false;
            }
        }

        private static bool IsRecoverableReadException(Exception exception)
        {
            return exception is IOException
                || exception is UnauthorizedAccessException
                || exception is JsonException
                || exception is InvalidDataException;
        }

        private static void WriteTextAtomic(string path, string text, bool retainBackup)
        {
            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory)) throw new InvalidOperationException("Persistence path has no directory.");
            Directory.CreateDirectory(directory);
            string temporaryPath = path + ".tmp-" + Guid.NewGuid().ToString("N");
            string backupPath = path + ".backup";
            try
            {
                byte[] bytes = Utf8NoBom.GetBytes(text ?? string.Empty);
                using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    FileOptions.WriteThrough))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                if (!File.Exists(path))
                {
                    File.Move(temporaryPath, path);
                    return;
                }

                try
                {
                    File.Replace(temporaryPath, path, retainBackup ? backupPath : null, true);
                }
                catch (Exception exception) when (exception is PlatformNotSupportedException
                    || exception is NotSupportedException
                    || exception is IOException)
                {
                    ReplaceWithPortableRename(temporaryPath, path, backupPath, retainBackup);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }

        private static void ReplaceWithPortableRename(
            string temporaryPath,
            string path,
            string backupPath,
            bool retainBackup)
        {
            if (File.Exists(backupPath)) File.Delete(backupPath);
            File.Move(path, backupPath);
            try
            {
                File.Move(temporaryPath, path);
                if (!retainBackup && File.Exists(backupPath)) File.Delete(backupPath);
            }
            catch
            {
                if (!File.Exists(path) && File.Exists(backupPath)) File.Move(backupPath, path);
                throw;
            }
        }

        private static JsonSerializerSettings CreateJsonSettings(bool preserveObjectReferences)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy(),
                },
                Culture = CultureInfo.InvariantCulture,
                DateParseHandling = DateParseHandling.None,
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                PreserveReferencesHandling = preserveObjectReferences
                    ? PreserveReferencesHandling.Objects
                    : PreserveReferencesHandling.None,
            };
            settings.Converters.Add(new StringEnumConverter());
            return settings;
        }

        private static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, JsonSettings);
        }

        private static string SerializeCompact(object value)
        {
            return JsonConvert.SerializeObject(value, Formatting.None, JsonSettings);
        }

        private static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, JsonSettings);
        }

        private static string SerializeCheckpoint(ExperimentSessionCheckpoint checkpoint)
        {
            return JsonConvert.SerializeObject(checkpoint, CheckpointJsonSettings);
        }

        private static ExperimentSessionCheckpoint DeserializeCheckpoint(string json)
        {
            return JsonConvert.DeserializeObject<ExperimentSessionCheckpoint>(json, CheckpointJsonSettings);
        }

        private static string Sha256(string value)
        {
            using (SHA256 algorithm = SHA256.Create())
            {
                byte[] hash = algorithm.ComputeHash(Utf8NoBom.GetBytes(value ?? string.Empty));
                var builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            int difference = 0;
            for (int i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
            return difference == 0;
        }

        private static string RequireIdentity(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A non-empty value is required.", parameterName);
            return value.Trim();
        }

        private static string SanitizePathSegment(string value)
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

        private static string RectValue(Rect rect)
        {
            return rect.x.ToString("0.###", CultureInfo.InvariantCulture)
                + "," + rect.y.ToString("0.###", CultureInfo.InvariantCulture)
                + "," + rect.width.ToString("0.###", CultureInfo.InvariantCulture)
                + "," + rect.height.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string UtcNow()
        {
            return DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        }

        [Serializable]
        private sealed class CheckpointEnvelope
        {
            public long Revision;
            public string SchemaVersion;
            public string ExperimentVersion;
            public string ParticipantId;
            public string SessionId;
            public string PayloadSha256;
            public string PayloadJson;
        }

        [Serializable]
        private sealed class RawEventWalEnvelope
        {
            public string SchemaVersion;
            public string ExperimentVersion;
            public string ParticipantId;
            public string SessionId;
            public int EventIndex;
            public string PayloadSha256;
            public string PayloadJson;
        }

        private sealed class ValidCheckpoint
        {
            public ValidCheckpoint(CheckpointEnvelope envelope, ExperimentSessionCheckpoint checkpoint)
            {
                Envelope = envelope;
                Checkpoint = checkpoint;
            }

            public CheckpointEnvelope Envelope { get; }
            public ExperimentSessionCheckpoint Checkpoint { get; }
        }
    }
}
