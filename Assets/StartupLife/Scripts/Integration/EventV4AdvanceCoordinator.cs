#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Core;
using StartupLife.Infrastructure;
using StartupLife.Simulation;
using static StartupLife.Integration.EventV4CommandGuard;

namespace StartupLife.Integration
{
    // The coordinator can only request a fully validated V4 intent commit or
    // ordinary V3 boundary child. It never sees a writer or physical CAS API.
    internal interface IEventV4AdvancePort
    {
        LoadResult Read();
        EventCommitResult CheckCurrentDay(string runId, long expectedRevision);
        // Read-only scheduling gate. No CAS, time or RNG mutation.
        bool ShouldCheckCurrentDay(GameState state);
        EventCommitResult CommitBatchIntent(GameState current, EventV4Payload payload);
        EventCommitResult ExecuteBatchChild(CommandEnvelope command);
    }

    internal static class EventV4AdvanceCoordinator
    {
        // Day/month sequencing is a V4 application workflow, not a second
        // scheduler: selection, step IDs, V3 command evaluation, and save writes
        // still belong to the existing authoritatively tested services.
        internal static AdvanceResult Execute(IEventV4AdvancePort port,
            CommandEnvelope request, string mode, bool automaticDailyChecks)
        {
            var results = new List<CommandResult>();
            LoadResult read = port.Read();
            if (!Valid(read)) return BatchStop(results, read.State, "RecoveryRequired");
            var state = read.State!;
            if (request == null || request.Command == null ||
                request.Command.Kind != CommandKind.AdvanceBoundary ||
                !ValidIdentity(request.RunId, request.CommandId, state) ||
                (request.CommandId.StartsWith(EventAdvanceBatchHistory.ReservedChildPrefix,
                     StringComparison.Ordinal) ||
                 request.CommandId.StartsWith(EventDecisionJournal.DailyCheckPrefix,
                     StringComparison.Ordinal)))
                return BatchStop(results, state, "InvalidRequest");
            var journal = state.EventV4!.Journal!;
            if (state.Receipts.Any(x => x.CommandId == request.CommandId) ||
                journal.Tickets.Any(x => x.OfferCommandId == request.CommandId ||
                    x.ResolutionCommandId == request.CommandId))
                return BatchStop(results, state, "InvalidRequest");
            var batch = journal.AdvanceBatches.FirstOrDefault(
                x => x.RootCommandId == request.CommandId);
            if (batch == null)
            {
                if (request.ExpectedRevision != state.Revision ||
                    string.IsNullOrEmpty(state.Name) ||
                    journal.PendingTicketId.Length > 0 ||
                    journal.AdvanceBatches.Any(x => string.CompareOrdinal(
                        state.DateIso, x.TargetDateIso) < 0))
                    return BatchStop(results, state, journal.PendingTicketId.Length > 0
                        ? "PlayerChoice" : "InvalidRequest");
                var next = JsonSaveSerializer.ReadObject<EventV4Payload>(
                    JsonSaveSerializer.WriteObject(state.EventV4));
                next.Journal!.AdvanceBatches.Add(new EventAdvanceBatchRecord
                {
                    RootCommandId = request.CommandId,
                    Mode = mode,
                    OpenedDateIso = state.DateIso,
                    TargetDateIso = EventAdvanceBatchHistory.Target(state.DateIso, mode),
                    CoreRevisionAtStart = checked(state.Revision - journal.Revision)
                });
                next.Journal.Revision = checked(next.Journal.Revision + 1);
                var registered = port.CommitBatchIntent(state, next);
                if (registered.Status != EventCommitStatus.Committed)
                    return BatchStop(results, state, registered.Status ==
                        EventCommitStatus.RecoveryRequired ? "RecoveryRequired" :
                        registered.Status == EventCommitStatus.PersistenceFailed
                            ? "PersistenceOrRuleFailure" : "InvalidState");
                read = port.Read();
                if (!Valid(read)) return BatchStop(results, state, "RecoveryRequired");
                state = read.State!;
                journal = state.EventV4!.Journal!;
                batch = journal.AdvanceBatches.Last();
            }
            else if (batch.Mode != mode)
                return BatchStop(results, state, "InvalidRequest");

            for (var guard = 0; string.CompareOrdinal(state.DateIso, batch.TargetDateIso) < 0; guard++)
            {
                if (guard > 10000) return BatchStop(results, state, "InvalidState");
                if (journal.PendingTicketId.Length > 0)
                    return BatchStop(results, state, "PlayerChoice");
                if (automaticDailyChecks && !HasCheckedDay(journal, state.DateIso) &&
                    port.ShouldCheckCurrentDay(state))
                {
                    // Every date, even an empty one, is a real V4 CAS frontier.
                    // Re-read after promotion before taking a V3 time step.
                    var checkedDay = port.CheckCurrentDay(state.RunId, state.Revision);
                    if (checkedDay.Status != EventCommitStatus.Committed &&
                        checkedDay.Status != EventCommitStatus.NoEligibleEvent &&
                        checkedDay.Status != EventCommitStatus.AlreadyCommitted)
                        return BatchStop(results, state,
                            checkedDay.Status == EventCommitStatus.RecoveryRequired
                                ? "RecoveryRequired"
                                : checkedDay.Status == EventCommitStatus.PersistenceFailed
                                    ? "PersistenceOrRuleFailure" : "InvalidState");
                    read = port.Read();
                    if (!Valid(read)) return BatchStop(results, state, "RecoveryRequired");
                    state = read.State!;
                    journal = state.EventV4!.Journal!;
                    if (journal.PendingTicketId.Length > 0)
                        return BatchStop(results, state, "PlayerChoice");
                }
                var counts = EventAdvanceBatchHistory.CountSteps(journal, state.Receipts);
                var childId = EventAdvanceBatchHistory.ChildId(
                    state.RunId, batch.RootCommandId, counts[batch.RootCommandId]);
                var step = port.ExecuteBatchChild(new CommandEnvelope(state.RunId, childId,
                    state.Revision, new GameCommand(CommandKind.AdvanceBoundary)));
                if (step.Status != EventCommitStatus.Committed)
                    return BatchStop(results, state, step.Status ==
                        EventCommitStatus.RecoveryRequired ? "RecoveryRequired" :
                        step.Status == EventCommitStatus.PersistenceFailed
                            ? "PersistenceOrRuleFailure" :
                            step.ReasonKey == "event.choice_required"
                                ? "PlayerChoice" : "PersistenceOrRuleFailure");
                results.Add(EventV4CommandResultAdapter.ToGameplayResult(step));
                read = port.Read();
                if (!Valid(read)) return BatchStop(results, state, "RecoveryRequired");
                state = read.State!;
                journal = state.EventV4!.Journal!;
            }
            return BatchStop(results, state, "TargetReached");
        }

        private static AdvanceResult BatchStop(List<CommandResult> results,
            GameState? state, string reason)
        {
            // A failed cold read has no authoritative date. Return the
            // default/unavailable instant, never a fabricated run date.
            var instant = state == null ? default(SimInstant) :
                new SimInstant(state.Date, state.Minute);
            return new AdvanceResult(results.AsReadOnly(), instant, reason);
        }

    }

    // The single presentation mapping serves both standalone V3 commands and
    // day/month batch child outcomes.
    internal static class EventV4CommandResultAdapter
    {
        internal static CommandResult ToGameplayResult(EventCommitResult result)
        {
            var status = result.Status switch
            {
                EventCommitStatus.Committed => CommandStatus.Committed,
                EventCommitStatus.AlreadyCommitted => CommandStatus.AlreadyCommitted,
                EventCommitStatus.PersistenceFailed => CommandStatus.PersistenceFailed,
                EventCommitStatus.RecoveryRequired => CommandStatus.RecoveryRequired,
                _ => CommandStatus.Rejected
            };
            var core = result.CoreCommand;
            return new CommandResult(status, result.ReasonKey, result.Revision,
                core?.OperationId ?? "", core?.MinutesConsumed ?? 0,
                status == CommandStatus.Committed ? core?.Outcome : null);
        }

    }
}
