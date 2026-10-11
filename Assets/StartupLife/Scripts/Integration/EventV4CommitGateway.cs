#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Core;
using StartupLife.Infrastructure;
using StartupLife.Simulation;

namespace StartupLife.Integration
{
    public enum EventCommitStatus
    {
        Committed, AlreadyCommitted, NoEligibleEvent, Rejected, PersistenceFailed, RecoveryRequired
    }

    public sealed class EventCommitResult
    {
        public EventCommitStatus Status { get; }
        public string ReasonKey { get; }
        public long Revision { get; }
        public string TicketId { get; }
        public string OutcomeId { get; }

        public EventCommitResult(EventCommitStatus status, string reasonKey, long revision,
            string ticketId = "", string outcomeId = "")
        {
            Status = status; ReasonKey = reasonKey; Revision = revision;
            TicketId = ticketId; OutcomeId = outcomeId;
        }
    }

    // Stateless staged transaction entrypoint; NOT an independent gameplay engine.
    // Uses the same event journal planner and the existing ISaveStore CAS authority.
    // Gameplay commands and applied reward receipts still require GameSession routing.
    public sealed class EventV4CommitGateway
    {
        private readonly EventV4CompatibilitySerializer serializer;
        private readonly ISaveStore store;
        private readonly IReadOnlyList<GameEventDefinition> definitions;
        private readonly IReadOnlyList<EventCategoryPolicy> policies;
        private readonly EventV4CoreCommandRunner? coreRunner;

        public EventV4CommitGateway(EventV4CompatibilitySerializer serializer, ISaveStore store,
            IEnumerable<GameEventDefinition> definitions, IEnumerable<EventCategoryPolicy> policies,
            EventV4CoreCommandRunner? coreRunner = null)
        {
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.coreRunner = coreRunner;
            if (definitions == null || policies == null)
                throw new ArgumentNullException("event catalog");
            this.definitions = Array.AsReadOnly(definitions.ToArray());
            this.policies = Array.AsReadOnly(policies.ToArray());
            EventDecisionJournal.Validate(EventDecisionJournal.New("catalog-admission", 1),
                this.definitions, this.policies);
        }

        public LoadResult Read() => store.Read();

        // Production API: the client sends only command identity and global CAS
        // revision. All event eligibility inputs come from the verified V3 save.
        public EventCommitResult Offer(string runId, string commandId, long expectedRevision) =>
            OfferInternal(runId, commandId, expectedRevision, null);

        // Compatibility API for source-only planner callers: supplied context is
        // ASSERTED against authoritative state; it is never trusted as input.
        public EventCommitResult Offer(string runId, string commandId, long expectedRevision,
            GameEventContext context) =>
            OfferInternal(runId, commandId, expectedRevision, context);

        private EventCommitResult OfferInternal(string runId, string commandId,
            long expectedRevision, GameEventContext? supplied)
        {
            var read = Read();
            if (!Valid(read)) return LoadFailure(read);
            var current = read.State!;
            if (!ValidIdentity(runId, commandId, current))
                return Reject(current, "event.invalid_command");
            if (current.Receipts.Any(x => x.CommandId == commandId))
                return Reject(current, "event.command_id_conflict");

            var journal = current.EventV4!.Journal!;
            var prior = journal.Tickets.FirstOrDefault(t => t.OfferCommandId == commandId);
            if (prior != null)
            {
                if (supplied != null && (prior.OpenedDateIso != supplied.Date.ToString() ||
                    prior.OriginDateIso != supplied.OriginDate.ToString() ||
                    prior.Stage != supplied.Stage || !prior.Tags.SequenceEqual(supplied.Tags)))
                    return Reject(current, "event.command_id_conflict");
                return new EventCommitResult(EventCommitStatus.AlreadyCommitted, "",
                    current.Revision, prior.Id);
            }
            // Reject before evaluating any author-controlled tag, stage or origin.
            var context = AuthoritativeEventContext.FromCore(current, journal);
            if (supplied != null)
            {
                if (supplied.Date != current.Date)
                    return Reject(current, "event.date_mismatch");
                if (context.OriginDate != supplied.OriginDate ||
                    context.Stage != supplied.Stage ||
                    !context.Tags.SequenceEqual(supplied.Tags))
                    return Reject(current, "event.context_not_authoritative");
            }
            if (current.Revision != expectedRevision)
                return Reject(current, "event.stale_revision");
            var innerRevision = checked(current.Revision - journal.Revision);
            var result = EventDecisionJournal.Offer(journal, definitions, policies,
                context, journal.Revision, commandId, innerRevision);
            if (result.Status != EventJournalStatus.Offered)
                return FromPlanner(result, current.Revision);
            return Commit(current, result);
        }

        public EventCommitResult Resolve(string runId, string commandId, long expectedRevision,
            string ticketId, string choiceId)
        {
            var read = Read();
            if (!Valid(read)) return LoadFailure(read);
            var current = read.State!;
            if (!ValidIdentity(runId, commandId, current))
                return Reject(current, "event.invalid_command");
            if (current.Receipts.Any(x => x.CommandId == commandId))
                return Reject(current, "event.command_id_conflict");
            var payload = current.EventV4!;
            var result = EventDecisionJournal.Resolve(payload.Journal!, definitions, policies,
                payload.Journal!.Revision, commandId, ticketId, choiceId);
            if (result.Status == EventJournalStatus.AlreadyCommitted)
                return FromPlanner(result, current.Revision);
            if (current.Revision != expectedRevision)
                return Reject(current, "event.stale_revision");
            if (result.Status != EventJournalStatus.Resolved)
                return FromPlanner(result, current.Revision);
            return Commit(current, result);
        }

        // The same V4 CAS frontier now covers all ordinary V3 GameSession receipts
        // and event decisions. V2/V3 replay stays authoritative for the inner state.
        public EventCommitResult ExecuteCore(CommandEnvelope command)
        {
            var read = Read();
            if (!Valid(read)) return LoadFailure(read);
            var current = read.State!;
            if (command == null || command.Command == null ||
                !ValidIdentity(command.RunId, command.CommandId, current))
                return Reject(current, "command.invalid_id");
            var journal = current.EventV4!.Journal!;
            if (journal.Tickets.Any(x => x.OfferCommandId == command.CommandId ||
                x.ResolutionCommandId == command.CommandId))
                return Reject(current, "command.id_conflict");
            var prior = current.Receipts.FirstOrDefault(x => x.CommandId == command.CommandId);
            if (prior != null)
                return prior.Payload == command.Command.CanonicalPayload
                    ? new EventCommitResult(EventCommitStatus.AlreadyCommitted, "", current.Revision)
                    : Reject(current, "command.id_conflict");
            if (current.Revision != command.ExpectedRevision)
                return Reject(current, "command.stale_state");
            if (journal.PendingTicketId.Length > 0 &&
                command.Command.Kind == CommandKind.AdvanceBoundary)
                return Reject(current, "event.choice_required");
            if (coreRunner == null)
                return Reject(current, "save.v4_core_evaluator_unavailable");
            try
            {
                var oldV3 = Convert.FromBase64String(current.EventV4.EmbeddedV3EnvelopeBase64);
                var outcome = coreRunner.Evaluate(oldV3, command, out var nextV3);
                if (outcome.Status != CommandStatus.Committed || nextV3 == null)
                    return new EventCommitResult(MapCoreStatus(outcome.Status),
                        outcome.ReasonKey, current.Revision);
                // One verified V3 replay yields BOTH the payload and its core state.
                // Avoid replaying the same entire receipt suffix a second time.
                var updated = serializer.ReplaceCore(current.EventV4, nextV3, out var inner);
                return CommitCandidate(current, inner, updated);
            }
            catch (Exception error) when (error is ArgumentException ||
                error is InvalidOperationException || error is OverflowException)
            { return Reject(current, "save.v4_invalid_core_candidate"); }
        }

        private EventCommitResult Commit(GameState current, EventJournalResult result)
        {
            var payload = JsonSaveSerializer.ReadObject<EventV4Payload>(
                JsonSaveSerializer.WriteObject(current.EventV4!));
            payload.Journal = result.Next;
            var core = EventV4CompatibilitySerializer.RestoreCoreProjection(current);
            return CommitCandidate(current, core, payload, result.TicketId, result.OutcomeId);
        }

        private EventCommitResult CommitCandidate(GameState current, GameState nextCore,
            EventV4Payload payload, string ticketId = "", string outcomeId = "")
        {
            byte[] candidateBytes;
            try
            {
                var candidate = EventV4CompatibilitySerializer.Project(nextCore, payload);
                if (candidate.Revision != checked(current.Revision + 1))
                    return Reject(current, "save.v4_revision_gap");
                candidateBytes = serializer.Serialize(candidate);
            }
            catch (Exception error) when (error is ArgumentException ||
                error is InvalidOperationException || error is OverflowException)
            { return Reject(current, "event.invalid_candidate"); }

            var written = store.Commit(candidateBytes, current.Revision);
            if (written == WriteStatus.Failed)
                return new EventCommitResult(EventCommitStatus.PersistenceFailed,
                    "save.write_failed", current.Revision);
            if (written == WriteStatus.Ambiguous)
                return new EventCommitResult(EventCommitStatus.RecoveryRequired,
                    "save.recovery_required", current.Revision);
            var verified = Read();
            if (!Valid(verified) || verified.State!.Revision != checked(current.Revision + 1) ||
                !JsonSaveSerializer.WriteObject(verified.State.EventV4!)
                    .SequenceEqual(JsonSaveSerializer.WriteObject(payload)))
                return new EventCommitResult(EventCommitStatus.RecoveryRequired,
                    "save.recovery_required", current.Revision);
            return new EventCommitResult(EventCommitStatus.Committed, "",
                verified.State.Revision, ticketId, outcomeId);
        }

        private static EventCommitStatus MapCoreStatus(CommandStatus status)
        {
            switch (status)
            {
                case CommandStatus.AlreadyCommitted: return EventCommitStatus.AlreadyCommitted;
                case CommandStatus.PersistenceFailed: return EventCommitStatus.PersistenceFailed;
                case CommandStatus.RecoveryRequired: return EventCommitStatus.RecoveryRequired;
                default: return EventCommitStatus.Rejected;
            }
        }

        private static bool Valid(LoadResult read) =>
            (read.Status == LoadStatus.Valid || read.Status == LoadStatus.RecoveredBackup) &&
            read.State != null && read.State.SaveVersion == 4 && read.State.EventV4?.Journal != null;

        private static bool ValidIdentity(string runId, string commandId, GameState state) =>
            runId == state.RunId && !string.IsNullOrWhiteSpace(commandId) &&
            commandId.Length <= 256;

        private static EventCommitResult Reject(GameState state, string reason) =>
            new EventCommitResult(EventCommitStatus.Rejected, reason, state.Revision);

        private static EventCommitResult FromPlanner(EventJournalResult result, long revision) =>
            new EventCommitResult(result.Status == EventJournalStatus.AlreadyCommitted
                    ? EventCommitStatus.AlreadyCommitted
                    : result.Status == EventJournalStatus.NoEligibleEvent
                        ? EventCommitStatus.NoEligibleEvent : EventCommitStatus.Rejected,
                result.ReasonKey, revision, result.TicketId, result.OutcomeId);

        private static EventCommitResult LoadFailure(LoadResult load) =>
            new EventCommitResult(EventCommitStatus.RecoveryRequired,
                load.Reason.Length == 0 ? "save.recovery_required" : load.Reason, -1);
    }
}
