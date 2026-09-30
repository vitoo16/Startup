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
        // Pre-PR callers and generated legacy child IDs were capped at 256 characters by ExecuteInternal.
        // New internal child IDs deliberately begin beyond that historical namespace so persisted caller IDs
        // such as "$batch/3:abc/0" can never alias a newly generated child ID.
        private static readonly string InternalBatchPrefix = new string('$', 257) + "batch/";
        private GameState state;
        private readonly ContentCatalog content;
        private readonly ISaveSerializer serializer;
        private readonly ISaveStore store;
        private readonly SimulationEngine simulation;
        private readonly object gate = new object();
        private bool recoveryRequired;
        public GameSession(GameState initial, ContentCatalog catalog, ISaveSerializer saveSerializer, ISaveStore saveStore)
        {
            content = catalog; serializer = saveSerializer; store = saveStore; simulation = new SimulationEngine(catalog);
            state = Clone(initial);
        }
        public static GameState NewState(ContentCatalog catalog, string runId, ulong seed, SimDate start)
        {
            var scheduler = DeterministicRng.Seed(seed, "scheduler"); var events = DeterministicRng.Seed(seed, "events");
            if (scheduler == events) { events = unchecked(events + 1); if (events == 0) events = 1; }
            return new GameState { ContentVersion = catalog.Version, RunId = runId, Seed = seed, DateIso = start.ToString(), SchedulerRng = scheduler, EventRng = events };
        }
        public GameSnapshot Snapshot() { lock (gate) return new GameSnapshot(state, content); }
        public byte[] ExportCheckpoint() { lock (gate) return serializer.Serialize(state); }
        public CommandResult Execute(CommandEnvelope command)
        {
            lock (gate)
            {
                if (!ValidCallerCommandId(command.CommandId) || command.RunId != state.RunId) return Result(CommandStatus.Rejected, "command.invalid_id");
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
            try
            {
                candidate = Clone(state); var operation = candidate.NewOperation();
                var minutes = simulation.Evaluate(candidate, envelope.Command, operation);
                candidate.Revision = checked(state.Revision + 1);
                if (envelope.Command.Kind != CommandKind.AdvanceBoundary && envelope.Command.Kind != CommandKind.AcknowledgePlayback)
                { candidate.CurrentActivity = operation; candidate.PlaybackCursor = 0; }
                receipt = new CommandReceipt { CommandId = envelope.CommandId, Payload = envelope.Command.CanonicalPayload, OperationId = operation,
                    Revision = candidate.Revision, Cue = candidate.CurrentCue, MinutesConsumed = minutes, AdvanceTargetIso = advanceTarget, ParentPayload = parentPayload };
                candidate.Receipts.Add(receipt); StateValidation.Validate(candidate, content);
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
            state = candidate; return new CommandResult(CommandStatus.Committed, "", state.Revision, receipt.OperationId, receipt.MinutesConsumed);
        }
        public LoadResult Recover()
        {
            lock (gate)
            {
                var restored = store.Read();
                if (restored.Status == LoadStatus.Valid || restored.Status == LoadStatus.RecoveredBackup)
                { state = Clone(restored.State!); recoveryRequired = false; }
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
                    return new AdvanceResult(outcomes.AsReadOnly(), "InvalidRequest");
                var direct = state.Receipts.SingleOrDefault(x => x.CommandId == request.CommandId);
                if (direct != null) return new AdvanceResult(outcomes.AsReadOnly(), "InvalidRequest");

                var payload = (month ? "month:" : "day:") + request.Command.CanonicalPayload;
                var existing = GetBatchReceipts(request.CommandId);
                string target;
                var legacy = false;
                if (existing.Count > 0)
                {
                    if (existing.Select(x => x.Index).Distinct().Count() != existing.Count || existing.Select(x => x.Legacy).Distinct().Count() != 1 ||
                        existing.Where((x, i) => x.Index != i).Any() || existing.Any(x => x.Receipt.ParentPayload != payload) ||
                        existing.Any(x => string.IsNullOrWhiteSpace(x.Receipt.AdvanceTargetIso)) ||
                        existing.Select(x => x.Receipt.AdvanceTargetIso).Distinct(StringComparer.Ordinal).Count() != 1)
                        return new AdvanceResult(outcomes.AsReadOnly(), "InvalidState");
                    target = existing[0].Receipt.AdvanceTargetIso;
                    legacy = existing[0].Legacy;
                    outcomes.AddRange(existing.Select(x => FromReceipt(x.Receipt)));
                }
                else
                {
                    if (request.ExpectedRevision != state.Revision) return new AdvanceResult(outcomes.AsReadOnly(), "InvalidRequest");
                    if (month)
                    {
                        var next = new DateTime(state.Date.Year, state.Date.Month, 1).AddMonths(1);
                        target = next.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                    else target = state.Date.AddDays(1).ToString();
                }

                for (var index = existing.Count; string.CompareOrdinal(state.DateIso, target) < 0; index++)
                {
                    if (state.PendingChoiceId.Length > 0) return new AdvanceResult(outcomes.AsReadOnly(), "PlayerChoice");
                    if (index > 10000) return new AdvanceResult(outcomes.AsReadOnly(), "InvalidState");
                    var childId = legacy ? LegacyBatchChildCommandId(request.CommandId, index) : BatchChildCommandId(request.CommandId, index);
                    var result = ExecuteInternal(new CommandEnvelope(state.RunId, childId, state.Revision,
                        new GameCommand(CommandKind.AdvanceBoundary)), target, payload);
                    outcomes.Add(result);
                    if (result.Status != CommandStatus.Committed && result.Status != CommandStatus.AlreadyCommitted) return new AdvanceResult(outcomes.AsReadOnly(), "PersistenceOrRuleFailure");
                }
                return new AdvanceResult(outcomes.AsReadOnly(), "TargetReached");
            }
        }
        private List<BatchReceipt> GetBatchReceipts(string rootCommandId)
        {
            var result = new List<BatchReceipt>();
            var internalPrefix = BatchChildPrefix(rootCommandId);
            var legacyPrefix = rootCommandId + "/";
            foreach (var receipt in state.Receipts)
            {
                if (string.IsNullOrEmpty(receipt.ParentPayload)) continue;
                int index;
                if (TryParseBatchIndex(receipt.CommandId, internalPrefix, out index)) result.Add(new BatchReceipt(index, receipt, false));
                else if (TryParseBatchIndex(receipt.CommandId, legacyPrefix, out index)) result.Add(new BatchReceipt(index, receipt, true));
            }
            result.Sort((a, b) => a.Index.CompareTo(b.Index));
            return result;
        }
        private static bool ValidCallerCommandId(string commandId) =>
            !string.IsNullOrWhiteSpace(commandId) && commandId.Length <= 256;
        private static string BatchChildPrefix(string rootCommandId) => InternalBatchPrefix + rootCommandId.Length.ToString(CultureInfo.InvariantCulture) + ":" + rootCommandId + "/";
        private static string BatchChildCommandId(string rootCommandId, int index) => BatchChildPrefix(rootCommandId) + index.ToString(CultureInfo.InvariantCulture);
        private static string LegacyBatchChildCommandId(string rootCommandId, int index) => rootCommandId + "/" + index.ToString(CultureInfo.InvariantCulture);
        private static bool TryParseBatchIndex(string commandId, string prefix, out int index)
        {
            index = -1;
            if (!commandId.StartsWith(prefix, StringComparison.Ordinal)) return false;
            var suffix = commandId.Substring(prefix.Length);
            return suffix.Length > 0 && int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out index) && index >= 0;
        }
        private static CommandResult FromReceipt(CommandReceipt receipt) => new CommandResult(CommandStatus.AlreadyCommitted, "", receipt.Revision, receipt.OperationId, receipt.MinutesConsumed);
        private sealed class BatchReceipt
        {
            public int Index { get; }
            public CommandReceipt Receipt { get; }
            public bool Legacy { get; }
            public BatchReceipt(int index, CommandReceipt receipt, bool legacy) { Index = index; Receipt = receipt; Legacy = legacy; }
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
