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
        public CommandResult Execute(CommandEnvelope command) { lock (gate) return ExecuteInternal(command); }
        private CommandResult ExecuteInternal(CommandEnvelope envelope, string advanceTarget = "", string parentPayload = "")
        {
            if (recoveryRequired) return Result(CommandStatus.RecoveryRequired, "save.recovery_required");
            if (string.IsNullOrWhiteSpace(envelope.CommandId) || envelope.CommandId.Length > 256 || envelope.RunId != state.RunId) return Result(CommandStatus.Rejected, "command.invalid_id");
            var prior = state.Receipts.SingleOrDefault(x => x.CommandId == envelope.CommandId);
            if (prior != null)
                return prior.Payload == envelope.Command.CanonicalPayload && prior.ParentPayload == parentPayload
                    ? new CommandResult(CommandStatus.AlreadyCommitted, "", prior.Revision, prior.OperationId, prior.MinutesConsumed)
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
                var payload = (month ? "month:" : "day:") + request.Command.CanonicalPayload;
                var first = state.Receipts.SingleOrDefault(x => x.CommandId == request.CommandId + "/0");
                if (request.RunId != state.RunId || string.IsNullOrWhiteSpace(request.CommandId) || request.Command.Kind != CommandKind.AdvanceBoundary ||
                    (first != null && first.ParentPayload != payload) || (first == null && request.ExpectedRevision != state.Revision))
                    return new AdvanceResult(outcomes.AsReadOnly(), "InvalidRequest");
                string target;
                if (first != null) target = first.AdvanceTargetIso;
                else if (month)
                {
                    var next = new DateTime(state.Date.Year, state.Date.Month, 1).AddMonths(1);
                    target = next.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                else target = state.Date.AddDays(1).ToString();
                for (var index = 0; string.CompareOrdinal(state.DateIso, target) < 0; index++)
                {
                    if (state.PendingChoiceId.Length > 0) return new AdvanceResult(outcomes.AsReadOnly(), "PlayerChoice");
                    if (index > 10000) return new AdvanceResult(outcomes.AsReadOnly(), "InvalidState");
                    var result = ExecuteInternal(new CommandEnvelope(state.RunId, request.CommandId + "/" + index.ToString(CultureInfo.InvariantCulture), state.Revision,
                        new GameCommand(CommandKind.AdvanceBoundary)), target, payload);
                    outcomes.Add(result);
                    if (result.Status != CommandStatus.Committed && result.Status != CommandStatus.AlreadyCommitted) return new AdvanceResult(outcomes.AsReadOnly(), "PersistenceOrRuleFailure");
                }
                return new AdvanceResult(outcomes.AsReadOnly(), "TargetReached");
            }
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
