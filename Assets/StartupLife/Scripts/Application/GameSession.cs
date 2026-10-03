#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StartupLife.Core;
using StartupLife.Simulation;

namespace StartupLife.Application
{
    public sealed class GameSession : IGameCommands
    {
        // Historical callers and pre-PR6 children were capped at 256 characters. PR6 children began "$batch/".
        // Starting with 257 '$' characters excludes both namespaces, including PR6 children longer than 256.
        private static readonly string InternalBatchPrefix = BatchReceiptIdentity.InternalPrefix;
        private GameState state;
        private readonly ContentCatalog content;
        private readonly ISaveSerializer serializer;
        private readonly ISaveStore store;
        private readonly SimulationEngine simulation;
        private readonly object gate = new object();
        private readonly Dictionary<string, string> legacyBatchOwners;
        private bool recoveryRequired;
        // Bindings must come from trusted save provenance, never from the caller's retry request.
        // Key: ambiguous persisted child prefix (e.g. "$batch/3:abc/"); value: its actual caller root.
        public GameSession(GameState initial, ContentCatalog catalog, ISaveSerializer saveSerializer, ISaveStore saveStore,
            IReadOnlyDictionary<string, string>? legacyBatchOwners = null)
        {
            content = catalog; serializer = saveSerializer; store = saveStore; simulation = new SimulationEngine(catalog);
            this.legacyBatchOwners = CopyLegacyOwners(legacyBatchOwners);
            state = Clone(initial);
        }
        private static Dictionary<string, string> CopyLegacyOwners(IReadOnlyDictionary<string, string>? bindings)
        {
            var copy = new Dictionary<string, string>(StringComparer.Ordinal);
            if (bindings != null)
                foreach (var binding in bindings)
                {
                    if (!TryParseEncodedChild(binding.Key + "0", "$batch/", out var encodedRoot, out _, out var prefix) ||
                        prefix != binding.Key || !ValidCallerCommandId(binding.Value) ||
                        (binding.Value != encodedRoot && binding.Value != prefix.Substring(0, prefix.Length - 1)))
                        throw new ArgumentException("Invalid legacy batch owner binding.", nameof(bindings));
                    copy.Add(binding.Key, binding.Value);
                }
            return copy;
        }
        public static bool TryRestore(ContentCatalog catalog, ISaveSerializer serializer, ISaveStore store,
            out GameSession? session, out LoadResult result, IReadOnlyDictionary<string, string>? legacyBatchOwners = null)
        {
            session = null;
            result = store.Read();
            if (result.Status != LoadStatus.Valid && result.Status != LoadStatus.RecoveredBackup) return false;
            var restored = new GameSession(result.State!, catalog, serializer, store, legacyBatchOwners);
            var ownershipFailure = restored.PrepareReceiptOwnership(result.Reason == BatchReceiptIdentity.BackupRepairReason);
            if (ownershipFailure.Length > 0)
            { result = new LoadResult(LoadStatus.RecoveryRequired, reason: ownershipFailure); return false; }
            result = new LoadResult(result.Status, restored.Clone(restored.state),
                result.Reason == BatchReceiptIdentity.BackupRepairReason ? "" : result.Reason);
            session = restored;
            return true;
        }
        public static GameState NewState(ContentCatalog catalog, string runId, ulong seed, SimDate start)
        {
            var scheduler = DeterministicRng.Seed(seed, "scheduler"); var events = DeterministicRng.Seed(seed, "events");
            if (scheduler == events) { events = unchecked(events + 1); if (events == 0) events = 1; }
            return new GameState { ContentVersion = catalog.Version, RunId = runId, Seed = seed, DateIso = start.ToString(), SchedulerRng = scheduler, EventRng = events };
        }
        public static IRestoreStateValidator CreateRestoreValidator() => new SimulationRestoreValidator();
        public GameSnapshot Snapshot() { lock (gate) return new GameSnapshot(state, content); }
        public byte[] ExportCheckpoint() { lock (gate) return serializer.Serialize(state); }
        public CommandResult Execute(CommandEnvelope command)
        {
            lock (gate)
            {
                if (!ValidCallerCommandId(command.CommandId) || command.RunId != state.RunId) return Result(CommandStatus.Rejected, "command.invalid_id");
                var ownershipFailure = PrepareReceiptOwnership();
                if (ownershipFailure.Length > 0) return Result(CommandStatus.RecoveryRequired, ownershipFailure);
                if (GetBatchReceipts(command.CommandId).Count > 0) return Result(CommandStatus.Rejected, "command.id_conflict");
                return ExecuteInternal(command);
            }
        }
        private CommandResult ExecuteInternal(CommandEnvelope envelope, string advanceTarget = "", string parentPayload = "")
        {
            if (recoveryRequired) return Result(CommandStatus.RecoveryRequired, "save.recovery_required");
            if (string.IsNullOrWhiteSpace(envelope.CommandId) || envelope.RunId != state.RunId) return Result(CommandStatus.Rejected, "command.invalid_id");
            var prior = state.Receipts.SingleOrDefault(x => x.CommandId == envelope.CommandId);
            if (prior != null)
                return prior.Payload == envelope.Command.CanonicalPayload && prior.ParentPayload == parentPayload
                    ? FromReceipt(prior)
                    : Result(CommandStatus.Rejected, "command.id_conflict");
            if (envelope.ExpectedRevision != state.Revision) return Result(CommandStatus.Rejected, "command.stale_state");
            GameState candidate;
            CommandReceipt receipt;
            SimulationOutcome outcome;
            var baseline = CaptureOutcome(state);
            try
            {
                candidate = Clone(state);
                NormalizeBatchReceipts(candidate);
                var operation = candidate.NewOperation();
                var minutes = simulation.Evaluate(candidate, envelope.Command, operation);
                candidate.Revision = checked(state.Revision + 1);
                if (envelope.Command.Kind != CommandKind.AdvanceBoundary && envelope.Command.Kind != CommandKind.AcknowledgePlayback)
                { candidate.CurrentActivity = operation; candidate.PlaybackCursor = 0; }
                receipt = new CommandReceipt { CommandId = envelope.CommandId, Payload = envelope.Command.CanonicalPayload, OperationId = operation,
                    Revision = candidate.Revision, Cue = candidate.CurrentCue, MinutesConsumed = minutes, AdvanceTargetIso = advanceTarget, ParentPayload = parentPayload };
                candidate.Receipts.Add(receipt); StateValidation.Validate(candidate, content);
                outcome = BuildOutcome(baseline, candidate, receipt);
            }
            catch (RuleFailure e) { return Result(CommandStatus.Rejected, e.ReasonKey); }
            catch (OverflowException) { return Result(CommandStatus.Rejected, "arithmetic.overflow"); }
            catch (ArgumentException) { return Result(CommandStatus.Rejected, "state.invalid"); }
            var write = store.Commit(serializer.Serialize(candidate), state.Revision);
            if (write == WriteStatus.Failed) return Result(CommandStatus.PersistenceFailed, "save.write_failed");
            if (write == WriteStatus.Ambiguous) { recoveryRequired = true; return Result(CommandStatus.RecoveryRequired, "save.recovery_required"); }
            var verified = store.Read();
            if ((verified.Status != LoadStatus.Valid && verified.Status != LoadStatus.RecoveredBackup) || verified.State!.Revision != candidate.Revision ||
                !verified.State.Receipts.Any(x => x.CommandId == receipt.CommandId && x.OperationId == receipt.OperationId && x.Payload == receipt.Payload))
            { recoveryRequired = true; return Result(CommandStatus.RecoveryRequired, "save.recovery_required"); }
            state = candidate; return new CommandResult(CommandStatus.Committed, "", state.Revision, receipt.OperationId, receipt.MinutesConsumed, outcome);
        }
        public LoadResult Recover(IReadOnlyDictionary<string, string>? legacyBatchOwners = null)
        {
            lock (gate)
            {
                var bindings = legacyBatchOwners == null ? null : CopyLegacyOwners(legacyBatchOwners);
                var restored = store.Read();
                if (restored.Status == LoadStatus.Valid || restored.Status == LoadStatus.RecoveredBackup)
                {
                    if (bindings == null && restored.State!.RunId != state.RunId) this.legacyBatchOwners.Clear();
                    state = Clone(restored.State!); recoveryRequired = false;
                    if (bindings != null)
                    {
                        this.legacyBatchOwners.Clear();
                        foreach (var binding in bindings) this.legacyBatchOwners.Add(binding.Key, binding.Value);
                    }
                    var ownershipFailure = PrepareReceiptOwnership(restored.Reason == BatchReceiptIdentity.BackupRepairReason);
                    if (ownershipFailure.Length > 0) return new LoadResult(LoadStatus.RecoveryRequired, reason: ownershipFailure);
                    return new LoadResult(restored.Status, Clone(state),
                        restored.Reason == BatchReceiptIdentity.BackupRepairReason ? "" : restored.Reason);
                }
                return restored;
            }
        }
        public AdvanceResult AdvanceDay(CommandEnvelope request) => Advance(request, false);
        public AdvanceResult AdvanceMonth(CommandEnvelope request) => Advance(request, true);
        private AdvanceResult Advance(CommandEnvelope request, bool month)
        {
            lock (gate)
            {
                var outcomes = new List<CommandResult>();
                if (request.RunId != state.RunId || !ValidCallerCommandId(request.CommandId) || request.Command.Kind != CommandKind.AdvanceBoundary)
                    return new AdvanceResult(outcomes.AsReadOnly(), Instant(state), "InvalidRequest");
                if (PrepareReceiptOwnership().Length > 0) return new AdvanceResult(outcomes.AsReadOnly(), Instant(state), "RecoveryRequired");
                var direct = state.Receipts.SingleOrDefault(x => x.CommandId == request.CommandId);
                if (direct != null) return new AdvanceResult(outcomes.AsReadOnly(), Instant(state), "InvalidRequest");

                var payload = (month ? "month:" : "day:") + request.Command.CanonicalPayload;
                var existing = GetBatchReceipts(request.CommandId);
                string target;
                if (existing.Count > 0)
                {
                    if (existing.Select(x => x.Index).Distinct().Count() != existing.Count ||
                        existing.Where((x, i) => x.Index != i).Any() || existing.Any(x => x.Receipt.ParentPayload != payload) ||
                        existing.Any(x => string.IsNullOrWhiteSpace(x.Receipt.AdvanceTargetIso)) ||
                        existing.Select(x => x.Receipt.AdvanceTargetIso).Distinct(StringComparer.Ordinal).Count() != 1)
                        return new AdvanceResult(outcomes.AsReadOnly(), Instant(state), "InvalidState");
                    target = existing[0].Receipt.AdvanceTargetIso;
                    outcomes.AddRange(existing.Select(x => FromReceipt(x.Receipt)));
                }
                else
                {
                    if (request.ExpectedRevision != state.Revision) return new AdvanceResult(outcomes.AsReadOnly(), Instant(state), "InvalidRequest");
                    if (month)
                    {
                        var next = new DateTime(state.Date.Year, state.Date.Month, 1).AddMonths(1);
                        target = next.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                    else target = state.Date.AddDays(1).ToString();
                }

                for (var index = existing.Count; string.CompareOrdinal(state.DateIso, target) < 0; index++)
                {
                    if (state.PendingChoiceId.Length > 0) return new AdvanceResult(outcomes.AsReadOnly(), Instant(state), "PlayerChoice");
                    if (index > 10000) return new AdvanceResult(outcomes.AsReadOnly(), Instant(state), "InvalidState");
                    var childId = BatchChildCommandId(request.CommandId, index);
                    var result = ExecuteInternal(new CommandEnvelope(state.RunId, childId, state.Revision,
                        new GameCommand(CommandKind.AdvanceBoundary)), target, payload);
                    outcomes.Add(result);
                    if (result.Status != CommandStatus.Committed && result.Status != CommandStatus.AlreadyCommitted) return new AdvanceResult(outcomes.AsReadOnly(), Instant(state), "PersistenceOrRuleFailure");
                }
                return new AdvanceResult(outcomes.AsReadOnly(), Instant(state), "TargetReached");
            }
        }
        private List<BatchReceipt> GetBatchReceipts(string rootCommandId)
        {
            var result = new List<BatchReceipt>();
            foreach (var receipt in state.Receipts)
            {
                if (string.IsNullOrEmpty(receipt.ParentPayload)) continue;
                if (TryGetBatchOwner(receipt, out var owner, out var index) && owner == rootCommandId)
                    result.Add(new BatchReceipt(index, receipt));
            }
            result.Sort((a, b) => a.Index.CompareTo(b.Index));
            return result;
        }
        private static bool ValidCallerCommandId(string commandId) =>
            !string.IsNullOrWhiteSpace(commandId) && commandId.Length <= 256;
        private static string BatchChildPrefix(string rootCommandId) => InternalBatchPrefix + rootCommandId.Length.ToString(CultureInfo.InvariantCulture) + ":" + rootCommandId + "/";
        private static string BatchChildCommandId(string rootCommandId, int index) => BatchChildPrefix(rootCommandId) + index.ToString(CultureInfo.InvariantCulture);
        private bool HasUnresolvedBatchOwners() => state.Receipts.Any(receipt =>
            !string.IsNullOrEmpty(receipt.ParentPayload) && !TryGetBatchOwner(receipt, out _, out _));
        private string PrepareReceiptOwnership(bool repairBackup = false)
        {
            if (recoveryRequired) return "save.recovery_required";
            if (HasUnresolvedBatchOwners()) return "save.batch_owner_required";
            if (!repairBackup && !state.Receipts.Any(receipt => receipt.ParentPayload.Length > 0 &&
                !receipt.CommandId.StartsWith(InternalBatchPrefix, StringComparison.Ordinal))) return "";
            if (!(store is ISaveCompatibilityStore compatibilityStore)) return "save.compatibility_store_required";
            var expected = serializer.Serialize(state);
            GameState candidate;
            byte[] normalized;
            try
            {
                candidate = Clone(state);
                NormalizeBatchReceipts(candidate);
                normalized = serializer.Serialize(candidate);
            }
            catch (ArgumentException) { return "save.batch_identity_invalid"; }
            var write = compatibilityStore.CommitReceiptCompatibility(expected, normalized);
            if (write == WriteStatus.Failed) return "save.compatibility_write_failed";
            if (write == WriteStatus.Ambiguous) { recoveryRequired = true; return "save.recovery_required"; }
            var verified = store.Read();
            if ((verified.Status != LoadStatus.Valid && verified.Status != LoadStatus.RecoveredBackup) ||
                !normalized.SequenceEqual(serializer.Serialize(verified.State!)))
            { recoveryRequired = true; return "save.recovery_required"; }
            state = candidate;
            legacyBatchOwners.Clear();
            return "";
        }
        private void NormalizeBatchReceipts(GameState candidate)
        {
            foreach (var receipt in candidate.Receipts)
            {
                if (string.IsNullOrEmpty(receipt.ParentPayload)) continue;
                if (!TryGetBatchOwner(receipt, out var owner, out var index)) throw new ArgumentException("Unresolved batch owner.");
                receipt.CommandId = BatchChildCommandId(owner, index);
            }
        }
        private bool TryGetBatchOwner(CommandReceipt receipt, out string owner, out int index)
        {
            if (TryParseEncodedChild(receipt.CommandId, InternalBatchPrefix, out owner, out index, out _)) return true;
            if (TryParseEncodedChild(receipt.CommandId, "$batch/", out var encodedOwner, out index, out var ambiguousPrefix))
            {
                // An alternative raw root longer than the caller limit could never have existed.
                if (ambiguousPrefix.Length - 1 > 256) { owner = encodedOwner; return true; }
                return legacyBatchOwners.TryGetValue(ambiguousPrefix, out owner!);
            }
            var slash = receipt.CommandId.LastIndexOf('/');
            owner = slash > 0 ? receipt.CommandId.Substring(0, slash) : "";
            return ValidCallerCommandId(owner) && TryParseBatchIndex(receipt.CommandId, owner + "/", out index);
        }
        private static bool TryParseEncodedChild(string commandId, string formatPrefix, out string owner, out int index, out string childPrefix)
        {
            owner = ""; index = -1; childPrefix = "";
            if (!commandId.StartsWith(formatPrefix, StringComparison.Ordinal)) return false;
            var colon = commandId.IndexOf(':', formatPrefix.Length);
            if (colon < 0 || !int.TryParse(commandId.Substring(formatPrefix.Length, colon - formatPrefix.Length),
                NumberStyles.None, CultureInfo.InvariantCulture, out var length) || length < 1 || length > 256 ||
                commandId.Substring(formatPrefix.Length, colon - formatPrefix.Length) != length.ToString(CultureInfo.InvariantCulture) ||
                colon + 1 + length >= commandId.Length || commandId[colon + 1 + length] != '/') return false;
            owner = commandId.Substring(colon + 1, length);
            childPrefix = commandId.Substring(0, colon + 2 + length);
            return ValidCallerCommandId(owner) && TryParseBatchIndex(commandId, childPrefix, out index);
        }
        private static bool TryParseBatchIndex(string commandId, string prefix, out int index)
        {
            index = -1;
            if (!commandId.StartsWith(prefix, StringComparison.Ordinal)) return false;
            var suffix = commandId.Substring(prefix.Length);
            return suffix.Length > 0 && int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out index) && index >= 0;
        }
        private CommandResult FromReceipt(CommandReceipt receipt) =>
            new CommandResult(CommandStatus.AlreadyCommitted, "", receipt.Revision, receipt.OperationId, receipt.MinutesConsumed, OutcomeForReceipt(receipt));

        private SimulationOutcome OutcomeForReceipt(CommandReceipt target)
        {
            var replay = NewState(content, state.RunId, state.Seed, StateValidation.ReceiptOrigin(state));
            foreach (var receipt in state.Receipts)
            {
                var baseline = CaptureOutcome(replay);
                var operation = replay.NewOperation();
                if (operation != receipt.OperationId) throw new InvalidOperationException("Committed operation identity cannot be replayed.");
                var command = GameCommand.ParseCanonicalPayload(receipt.Payload);
                int minutes;
                try { minutes = simulation.Evaluate(replay, command, operation); }
                catch (RuleFailure error) { throw new InvalidOperationException("Committed receipt cannot be replayed: " + error.ReasonKey, error); }
                if (minutes != receipt.MinutesConsumed) throw new InvalidOperationException("Committed duration cannot be replayed.");
                replay.Revision = receipt.Revision;
                if (command.Kind != CommandKind.AdvanceBoundary && command.Kind != CommandKind.AcknowledgePlayback)
                { replay.CurrentActivity = operation; replay.PlaybackCursor = 0; }
                if (receipt.Revision == target.Revision && receipt.OperationId == target.OperationId)
                    return BuildOutcome(baseline, replay, receipt);
            }
            throw new InvalidOperationException("Committed receipt was not found in authoritative history.");
        }

        private OutcomeBaseline CaptureOutcome(GameState source)
        {
            var skills = new Dictionary<string, SkillBaseline>(StringComparer.Ordinal);
            foreach (var skill in source.Skills)
                skills.Add(skill.Id, new SkillBaseline(skill.Exposure, skill.GrantedLevel, GameSnapshot.Level(skill, content.Skills[skill.Id])));
            return new OutcomeBaseline(Instant(source), source.Revision, source.Cash, source.Employment, skills,
                source.Course?.InstanceId ?? "", source.Course?.ProgressUnits ?? 0, source.Grants.Count, source.History.Count);
        }

        private SimulationOutcome BuildOutcome(OutcomeBaseline before, GameState after, CommandReceipt receipt)
        {
            var ledger = after.Ledger.Where(x => x.OperationId == receipt.OperationId)
                .Select(x => new OutcomeLedgerEntry(x.Id, x.Category, x.CashDelta, x.Amount, x.AttributionId)).ToArray();
            var skillDeltas = new List<SkillProgressDelta>();
            foreach (var skill in after.Skills)
            {
                before.Skills.TryGetValue(skill.Id, out var prior);
                var priorExposure = prior?.Exposure ?? 0;
                var priorGranted = prior?.GrantedLevel ?? 0;
                var priorLevel = prior?.Level ?? 0;
                var nextLevel = GameSnapshot.Level(skill, content.Skills[skill.Id]);
                var exposureDelta = checked(skill.Exposure - priorExposure);
                if (exposureDelta != 0 || priorGranted != skill.GrantedLevel || priorLevel != nextLevel)
                    skillDeltas.Add(new SkillProgressDelta(skill.Id, exposureDelta, priorLevel, nextLevel, priorGranted, skill.GrantedLevel));
            }

            var employmentId = before.EmploymentId;
            var careerXpDelta = 0L;
            var priorRank = before.Rank;
            var newRank = before.Rank;
            if (after.Employment != null && before.EmploymentId.Length == 0)
            {
                employmentId = after.Employment.InstanceId; careerXpDelta = after.Employment.Xp; priorRank = 0; newRank = after.Employment.Rank;
            }
            else if (after.Employment != null && after.Employment.InstanceId == before.EmploymentId)
            {
                careerXpDelta = checked(after.Employment.Xp - before.CareerXp); newRank = after.Employment.Rank;
            }

            var courseId = before.CourseInstanceId;
            var priorStudy = before.CourseProgressUnits;
            if (courseId.Length == 0 && after.Course != null) courseId = after.Course.InstanceId;
            var studyDelta = courseId.Length == 0 ? 0 : checked(CourseProgress(after, courseId) - priorStudy);
            var grants = after.Grants.Skip(before.GrantCount).ToArray();
            var history = after.History.Skip(before.HistoryCount).ToArray();
            return new SimulationOutcome(receipt.OperationId, receipt.CommandId, after.CurrentActivity, before.Revision, receipt.Revision,
                before.Instant, Instant(after), receipt.MinutesConsumed, receipt.Cue, after.PlaybackCursor, checked(after.Cash - before.Cash),
                employmentId, careerXpDelta, priorRank, newRank, courseId, studyDelta, ledger, skillDeltas.ToArray(), grants, history);
        }

        private static long CourseProgress(GameState source, string instanceId)
        {
            if (source.Course != null && source.Course.InstanceId == instanceId) return source.Course.ProgressUnits;
            var completed = source.CompletedCourses.SingleOrDefault(x => x.InstanceId == instanceId);
            return completed?.ProgressUnits ?? 0;
        }

        private static SimInstant Instant(GameState source) => new SimInstant(source.Date, source.Minute);

        private sealed class SkillBaseline
        {
            public long Exposure { get; }
            public int GrantedLevel { get; }
            public int Level { get; }
            public SkillBaseline(long exposure, int grantedLevel, int level)
            { Exposure = exposure; GrantedLevel = grantedLevel; Level = level; }
        }
        private sealed class OutcomeBaseline
        {
            public SimInstant Instant { get; }
            public long Revision { get; }
            public long Cash { get; }
            public string EmploymentId { get; }
            public long CareerXp { get; }
            public int Rank { get; }
            public IReadOnlyDictionary<string, SkillBaseline> Skills { get; }
            public string CourseInstanceId { get; }
            public long CourseProgressUnits { get; }
            public int GrantCount { get; }
            public int HistoryCount { get; }
            public OutcomeBaseline(SimInstant instant, long revision, long cash, EmploymentState? employment,
                IReadOnlyDictionary<string, SkillBaseline> skills, string courseInstanceId, long courseProgressUnits, int grantCount, int historyCount)
            {
                Instant = instant; Revision = revision; Cash = cash; EmploymentId = employment?.InstanceId ?? "";
                CareerXp = employment?.Xp ?? 0; Rank = employment?.Rank ?? 0; Skills = skills;
                CourseInstanceId = courseInstanceId; CourseProgressUnits = courseProgressUnits;
                GrantCount = grantCount; HistoryCount = historyCount;
            }
        }
        private sealed class BatchReceipt
        {
            public int Index { get; }
            public CommandReceipt Receipt { get; }
            public BatchReceipt(int index, CommandReceipt receipt) { Index = index; Receipt = receipt; }
        }
        private GameState Clone(GameState source)
        {
            var result = serializer.DeserializeAndValidate(serializer.Serialize(source));
            if (result.Status != LoadStatus.Valid) throw new ArgumentException("Invalid checkpoint: " + result.Reason);
            return result.State!;
        }
        private CommandResult Result(CommandStatus status, string key) => new CommandResult(status, key, state.Revision);
    }
}
