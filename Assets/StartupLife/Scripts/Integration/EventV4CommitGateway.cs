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
        // Only populated for an acknowledged V3 gameplay command. Event choices
        // still use TicketId/OutcomeId and have their own effect receipts.
        public CommandResult? CoreCommand { get; }

        public EventCommitResult(EventCommitStatus status, string reasonKey, long revision,
            string ticketId = "", string outcomeId = "", CommandResult? coreCommand = null)
        {
            Status = status; ReasonKey = reasonKey; Revision = revision;
            TicketId = ticketId; OutcomeId = outcomeId; CoreCommand = coreCommand;
        }
    }

    // Read-only V4 choice presentation. Every item is derived from the same
    // verified journal and immutable authored catalog used for command admission.
    public sealed class EventPendingChoiceSnapshot
    {
        public string RunId { get; }
        public long Revision { get; }
        public string TicketId { get; }
        public string EventId { get; }
        public string TitleKey { get; }
        public string BodyKey { get; }
        public IReadOnlyList<EventChoiceDefinition> Choices { get; }

        public EventPendingChoiceSnapshot(string runId, long revision, string ticketId,
            string eventId, IEnumerable<EventChoiceDefinition> choices,
            string titleKey = "", string bodyKey = "")
        {
            RunId = runId; Revision = revision; TicketId = ticketId; EventId = eventId;
            TitleKey = titleKey; BodyKey = bodyKey;
            Choices = Array.AsReadOnly(choices.ToArray());
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

        // A UI can render and resolve the pending choice using this exact global
        // CAS revision; it must never fabricate stage/tags or bypass the journal.
        public EventPendingChoiceSnapshot? ReadPendingChoice()
        {
            var read = Read();
            if (!Valid(read))
                throw new InvalidOperationException("Verified V4 state required for event choices.");
            var state = read.State!;
            var journal = state.EventV4!.Journal!;
            if (journal.PendingTicketId.Length == 0) return null;
            var ticket = journal.Tickets.Single(x => x.Id == journal.PendingTicketId);
            var definition = definitions.Single(x => x.Id == ticket.EventId);
            return new EventPendingChoiceSnapshot(state.RunId, state.Revision,
                ticket.Id, definition.Id, definition.Choices, definition.TitleKey,
                definition.BodyKey);
        }

        // Persist a negative event-day check with the existing V4 CAS writer.
        public EventCommitResult CheckCurrentDay(string runId, long expectedRevision)
        {
            var read = Read();
            if (!Valid(read)) return LoadFailure(read);
            var current = read.State!;
            if (current.RunId != runId || string.IsNullOrEmpty(current.Name))
                return Reject(current, "event.character_required");
            return OfferInternal(runId,
                EventDecisionJournal.DailyCommandId(current.DateIso),
                expectedRevision, null, persistEmpty: true);
        }

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
            long expectedRevision, GameEventContext? supplied, bool persistEmpty = false)
        {
            var read = Read();
            if (!Valid(read)) return LoadFailure(read);
            var current = read.State!;
            if (!ValidIdentity(runId, commandId, current))
                return Reject(current, "event.invalid_command");
            if (commandId.StartsWith(EventDecisionJournal.DailyCheckPrefix,
                    StringComparison.Ordinal) &&
                (!persistEmpty || commandId !=
                    EventDecisionJournal.DailyCommandId(current.DateIso)))
                return Reject(current, "event.command_id_conflict");
            if (ConflictsWithCoreOrBatch(current, commandId))
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
            var previousEmpty = journal.NoEligibleDays.FirstOrDefault(
                x => x.CommandId == commandId);
            if (previousEmpty != null)
                return new EventCommitResult(EventCommitStatus.AlreadyCommitted,
                    "", current.Revision);
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
            var result = persistEmpty
                ? EventDecisionJournal.CheckDay(journal, definitions, policies,
                    context, journal.Revision, commandId, innerRevision)
                : EventDecisionJournal.Offer(journal, definitions, policies,
                    context, journal.Revision, commandId, innerRevision);
            if (result.Status == EventJournalStatus.NoEligibleEvent && persistEmpty)
            {
                var committed = Commit(current, result);
                return committed.Status == EventCommitStatus.Committed
                    ? new EventCommitResult(EventCommitStatus.NoEligibleEvent,
                        "event.none", committed.Revision)
                    : committed;
            }
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
            if (ConflictsWithCoreOrBatch(current, commandId) ||
                commandId.StartsWith(EventDecisionJournal.DailyCheckPrefix,
                    StringComparison.Ordinal))
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

        public AdvanceResult AdvanceDay(CommandEnvelope request) => AdvanceBatch(request, "day");

        public AdvanceResult AdvanceMonth(CommandEnvelope request) => AdvanceBatch(request, "month");

        // A batch root is committed once as a V4 event-journal intent. Each child
        // remains exactly one real V3 GameSession AdvanceBoundary receipt under
        // the original V4 physical CAS. Cold retries derive the next child index
        // from authenticated V3 receipts rather than volatile counters.
        private AdvanceResult AdvanceBatch(CommandEnvelope request, string mode)
        {
            var results = new List<CommandResult>();
            LoadResult read = Read();
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
                var registered = CommitCandidate(state,
                    EventV4CompatibilitySerializer.RestoreCoreProjection(state), next);
                if (registered.Status != EventCommitStatus.Committed)
                    return BatchStop(results, state, registered.Status ==
                        EventCommitStatus.RecoveryRequired ? "RecoveryRequired" :
                        registered.Status == EventCommitStatus.PersistenceFailed
                            ? "PersistenceOrRuleFailure" : "InvalidState");
                read = Read();
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
                var counts = EventAdvanceBatchHistory.CountSteps(journal, state.Receipts);
                var childId = EventAdvanceBatchHistory.ChildId(
                    state.RunId, batch.RootCommandId, counts[batch.RootCommandId]);
                var step = ExecuteCoreInternal(new CommandEnvelope(state.RunId, childId,
                    state.Revision, new GameCommand(CommandKind.AdvanceBoundary)), true);
                if (step.Status != EventCommitStatus.Committed)
                    return BatchStop(results, state, step.Status ==
                        EventCommitStatus.RecoveryRequired ? "RecoveryRequired" :
                        step.Status == EventCommitStatus.PersistenceFailed
                            ? "PersistenceOrRuleFailure" :
                            step.ReasonKey == "event.choice_required"
                                ? "PlayerChoice" : "PersistenceOrRuleFailure");
                results.Add(ToGameplayResult(step));
                read = Read();
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

        // Presentation-compatible single-command API: global revision belongs
        // to V4; operation, duration and outcome are the verified V3 evaluator's.
        // Multi-boundary day/month batches require their own durable intent and
        // are deliberately NOT synthesized here.
        public CommandResult ExecuteGameplay(CommandEnvelope command) =>
            ToGameplayResult(ExecuteCore(command));

        private static CommandResult ToGameplayResult(EventCommitResult result)
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

        // The same V4 CAS frontier now covers all ordinary V3 GameSession receipts
        // and event decisions. V2/V3 replay stays authoritative for the inner state.
        public EventCommitResult ExecuteCore(CommandEnvelope command) =>
            ExecuteCoreInternal(command, false);

        private EventCommitResult ExecuteCoreInternal(CommandEnvelope command, bool batchChild)
        {
            var read = Read();
            if (!Valid(read)) return LoadFailure(read);
            var current = read.State!;
            if (command == null || command.Command == null ||
                !ValidIdentity(command.RunId, command.CommandId, current))
                return Reject(current, "command.invalid_id");
            var journal = current.EventV4!.Journal!;
            if (batchChild)
            {
                var active = journal.AdvanceBatches.LastOrDefault();
                if (active == null ||
                    string.CompareOrdinal(current.DateIso, active.TargetDateIso) >= 0 ||
                    command.Command.Kind != CommandKind.AdvanceBoundary ||
                    command.CommandId != EventAdvanceBatchHistory.ChildId(current.RunId,
                        active.RootCommandId, EventAdvanceBatchHistory.CountSteps(
                            journal, current.Receipts)[active.RootCommandId]))
                    return Reject(current, "batch.invalid_child");
            }
            else if (command.CommandId.StartsWith(
                EventAdvanceBatchHistory.ReservedChildPrefix, StringComparison.Ordinal) ||
                command.CommandId.StartsWith(
                    EventDecisionJournal.DailyCheckPrefix, StringComparison.Ordinal))
                return Reject(current, "command.reserved_id");
            if (!batchChild && journal.AdvanceBatches.Any(x =>
                string.CompareOrdinal(current.DateIso, x.TargetDateIso) < 0) &&
                command.Command.Kind != CommandKind.AcknowledgePlayback)
                return Reject(current, "batch.in_progress");
            if (journal.AdvanceBatches.Any(x => x.RootCommandId == command.CommandId))
                return Reject(current, "command.id_conflict");
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
            // A required choice pauses gameplay, not only the clock. Otherwise
            // a course purchase, resignation or business change could mutate
            // the authenticated V3 world while the player sees a blocking event.
            // Playback acknowledgement carries no game progression and remains
            // allowed so already-committed presentation cues can finish.
            if (journal.PendingTicketId.Length > 0 &&
                command.Command.Kind != CommandKind.AcknowledgePlayback)
                return Reject(current, "event.choice_required");
            if (coreRunner == null)
                return Reject(current, "save.v4_core_evaluator_unavailable");
            try
            {
                var oldV3 = Convert.FromBase64String(current.EventV4.EmbeddedV3EnvelopeBase64);
                var outcome = coreRunner.Evaluate(oldV3, command, out var nextV3);
                if (outcome.Status != CommandStatus.Committed || nextV3 == null)
                    return new EventCommitResult(MapCoreStatus(outcome.Status),
                        outcome.ReasonKey, current.Revision,
                        coreCommand: outcome.Status == CommandStatus.Rejected ? null : outcome);
                // One verified V3 replay yields BOTH the payload and its core state.
                // Avoid replaying the same entire receipt suffix a second time.
                var updated = serializer.ReplaceCore(current.EventV4, nextV3, out var inner);
                return CommitCandidate(current, inner, updated, coreCommand: outcome);
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
            EventV4Payload payload, string ticketId = "", string outcomeId = "",
            CommandResult? coreCommand = null)
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
                verified.State.Revision, ticketId, outcomeId, coreCommand);
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

        // Event IDs share a command namespace with both V3 receipts and durable
        // V4 day/month roots. Reject collisions before planning or consuming RNG;
        // the V4 cold validator enforces the same rule on persisted checkpoints.
        private static bool ConflictsWithCoreOrBatch(GameState state, string commandId) =>
            commandId.StartsWith(EventAdvanceBatchHistory.ReservedChildPrefix,
                StringComparison.Ordinal) ||
            state.Receipts.Any(x => x.CommandId == commandId) ||
            state.EventV4!.Journal!.AdvanceBatches.Any(x => x.RootCommandId == commandId);

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
