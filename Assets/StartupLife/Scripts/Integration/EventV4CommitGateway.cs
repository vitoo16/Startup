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
        public IReadOnlyDictionary<string, string> ArchivedVietnamese { get; }
        // Per-choice blockers are projected from the verified save and authored
        // rewards, never estimated from an uncommitted client-side wallet.
        public IReadOnlyDictionary<string, string> UnavailableChoiceReasons { get; }

        public EventPendingChoiceSnapshot(string runId, long revision, string ticketId,
            string eventId, IEnumerable<EventChoiceDefinition> choices,
            string titleKey = "", string bodyKey = "",
            IReadOnlyDictionary<string, string>? archivedVietnamese = null,
            IEnumerable<KeyValuePair<string, string>>? unavailableChoiceReasons = null)
        {
            RunId = runId; Revision = revision; TicketId = ticketId; EventId = eventId;
            TitleKey = titleKey; BodyKey = bodyKey;
            Choices = Array.AsReadOnly(choices.ToArray());
            ArchivedVietnamese = archivedVietnamese ??
                new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(
                    new Dictionary<string, string>(StringComparer.Ordinal));
            UnavailableChoiceReasons =
                new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(
                    (unavailableChoiceReasons ?? Array.Empty<KeyValuePair<string, string>>())
                        .ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal));
        }
    }

    // Stateless staged transaction entrypoint; NOT an independent gameplay engine.
    // Uses the same event journal planner and the existing ISaveStore CAS authority.
    // Gameplay commands and applied reward receipts still require GameSession routing.
    public sealed class EventV4CommitGateway : IEventV4AdvancePort
    {
        private readonly EventV4CompatibilitySerializer serializer;
        private readonly ISaveStore store;
        // All run-specific catalog decisions come from the SAME validated
        // serializer archive; no second active roster is kept in the gateway.
        private readonly EventV4CoreCommandRunner? coreRunner;
        private readonly bool automaticDailyChecks;
        // Validated in-memory genesis; never persisted or trusted after a file exists.
        private readonly byte[]? initialCheckpoint;

        public EventV4CommitGateway(EventV4CompatibilitySerializer serializer, ISaveStore store,
            IEnumerable<GameEventDefinition> definitions, IEnumerable<EventCategoryPolicy> policies,
            EventV4CoreCommandRunner? coreRunner = null,
            bool automaticDailyChecks = false, GameState? initialState = null)
        {
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.coreRunner = coreRunner;
            this.automaticDailyChecks = automaticDailyChecks;
            if (definitions == null || policies == null)
                throw new ArgumentNullException("event catalog");
            // Reject a gateway/serializer catalog mismatch before touching a
            // save. Historical snapshots are chosen per verified run below.
            var provided = new EventCatalogSnapshot(definitions, policies);
            if (provided.Fingerprint != serializer.CurrentCatalog.Fingerprint)
                throw new ArgumentException("Gateway and serializer current event catalogs differ.");
            if (initialState != null)
            {
                var journal = initialState.EventV4?.Journal;
                if (initialState.SaveVersion != 4 || initialState.Revision != 0 ||
                    initialState.Receipts.Count != 0 ||
                    !string.IsNullOrEmpty(initialState.Name) ||
                    journal == null || journal.Revision != 0 ||
                    journal.Tickets.Count != 0 || journal.AdvanceBatches.Count != 0 ||
                    journal.OutcomeReceipts.Count != 0 || journal.NoEligibleDays.Count != 0)
                    throw new ArgumentException(
                        "Fresh V4 genesis must be an empty verified zero-revision run.");
                // Validate via the same cold path used by all later CAS reads,
                // not by trusting the caller-supplied mutable GameState.
                var bytes = serializer.Serialize(initialState);
                var validated = serializer.DeserializeAndValidate(bytes);
                if (!Valid(validated) || validated.State!.Revision != 0)
                    throw new ArgumentException("Invalid V4 genesis checkpoint.");
                initialCheckpoint = bytes;
            }
        }

        public LoadResult Read()
        {
            var persisted = store.Read();
            if (persisted.Status != LoadStatus.Missing || initialCheckpoint == null)
                return persisted;
            // A verified projected state exists only in memory until the first
            // real V3 CreateCharacter receipt atomically promotes schema4.
            return serializer.DeserializeAndValidate(initialCheckpoint);
        }

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
            var catalog = serializer.ResolveCatalog(journal);
            var definition = catalog.Definitions.Single(x => x.Id == ticket.EventId);
            var unavailable = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var option in definition.Choices)
            {
                if (option.Reward == null) continue;
                var reason = EventDecisionJournal.RewardUnavailableReason(
                    state, option.Reward, coreRunner?.Content);
                if (reason.Length != 0) unavailable.Add(option.Id, reason);
            }
            return new EventPendingChoiceSnapshot(state.RunId, state.Revision,
                ticket.Id, definition.Id, definition.Choices, definition.TitleKey,
                definition.BodyKey, catalog.VietnameseText, unavailable);
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
            // Transient genesis cannot be made durable by an event offer before
            // the player's first authenticated CreateCharacter receipt.
            if (initialCheckpoint != null && current.Revision == 0 &&
                string.IsNullOrEmpty(current.Name))
                return Reject(current, "event.character_required");
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
            var catalog = serializer.ResolveCatalog(journal);
            var result = persistEmpty
                ? EventDecisionJournal.CheckDay(journal, catalog.Definitions, catalog.Policies,
                    context, journal.Revision, commandId, innerRevision)
                : EventDecisionJournal.Offer(journal, catalog.Definitions, catalog.Policies,
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
            var catalog = serializer.ResolveCatalog(payload.Journal!);
            var result = EventDecisionJournal.Resolve(payload.Journal!,
                catalog.Definitions, catalog.Policies,
                payload.Journal!.Revision, commandId, ticketId, choiceId);
            if (result.Status == EventJournalStatus.AlreadyCommitted)
                return FromPlanner(result, current.Revision);
            if (current.Revision != expectedRevision)
                return Reject(current, "event.stale_revision");
            if (result.Status != EventJournalStatus.Resolved)
            {
                if (result.Status == EventJournalStatus.Rejected &&
                    result.ReasonKey == "event.reward_processor_unavailable" &&
                    coreRunner != null)
                    return CommitReward(current, commandId, ticketId, choiceId);
                return FromPlanner(result, current.Revision);
            }
            return Commit(current, result);
        }

        // A reward has two logical receipts but only one physical write.
        // Neither journal nor wallet may publish before full composite cold
        // validation and the store's transition verifier succeed together.
        private EventCommitResult CommitReward(GameState current, string commandId,
            string ticketId, string choiceId)
        {
            try
            {
                var sourceBytes = serializer.Serialize(current);
                var catalog = serializer.ResolveCatalog(current.EventV4!.Journal!);
                var staged = EventRewardCompositeProof.Prepare(sourceBytes, serializer,
                    coreRunner!, catalog.Definitions, catalog.Policies,
                    commandId, ticketId, choiceId);
                EventRewardCompositeProof.Validate(sourceBytes, staged, serializer,
                    coreRunner!, catalog.Definitions, catalog.Policies);
                var payload = EventRewardCompositeProof.CreatePayload(staged, coreRunner!);
                return CommitCandidate(current, staged.Core, payload,
                    staged.Plan.TicketId, staged.Plan.OutcomeId, revisionDelta: 2);
            }
            catch (Exception error) when (error is ArgumentException ||
                error is InvalidOperationException || error is OverflowException ||
                error is FormatException)
            { return Reject(current, "event.reward_invalid_candidate"); }
        }

        public AdvanceResult AdvanceDay(CommandEnvelope request) =>
            EventV4AdvanceCoordinator.Execute(this, request, "day", automaticDailyChecks);

        public AdvanceResult AdvanceMonth(CommandEnvelope request) =>
            EventV4AdvanceCoordinator.Execute(this, request, "month", automaticDailyChecks);

        // The coordinator has no writer, so only this gateway can commit the
        // verified V4 intent and submit an authenticated V3 boundary receipt.
        EventCommitResult IEventV4AdvancePort.CommitBatchIntent(
            GameState current, EventV4Payload payload) =>
            CommitCandidate(current, EventV4CompatibilitySerializer.RestoreCoreProjection(current),
                payload);

        EventCommitResult IEventV4AdvancePort.ExecuteBatchChild(CommandEnvelope command) =>
            ExecuteCoreInternal(command, true);

        // Presentation-compatible single-command API: global revision belongs
        // to V4; operation, duration and outcome are the verified V3 evaluator's.
        // Multi-boundary day/month batches require their own durable intent and
        // are deliberately NOT synthesized here.
        public CommandResult ExecuteGameplay(CommandEnvelope command) =>
            EventV4CommandResultAdapter.ToGameplayResult(ExecuteCore(command));

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
            // Even a valid-looking encoded event reward is not a public V3
            // command. Only the future atomic V4 settlement path may submit a
            // receipt that has been bound to PlanReward and cold journal proof.
            if (command.Command.Kind == CommandKind.SettleEventReward)
                return Reject(current, "event.reward_requires_v4_settlement");
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
                    EventDecisionJournal.DailyCheckPrefix, StringComparison.Ordinal) ||
                command.CommandId.StartsWith(
                    EventRewardSettlementPlan.ReservedReceiptPrefix, StringComparison.Ordinal))
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
            if (initialCheckpoint != null && current.Revision == 0 &&
                string.IsNullOrEmpty(current.Name) &&
                command.Command.Kind != CommandKind.CreateCharacter)
                return Reject(current, "event.character_required");
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
            if (automaticDailyChecks && !batchChild &&
                command.Command.Kind == CommandKind.AdvanceBoundary &&
                !HasCheckedDay(journal, current.DateIso))
            {
                // WorkShiftPlaybackController legitimately drives one boundary
                // at a time, unlike the durable Day/Month batch. It must use
                // the same daily V4 ticket/negative-check gate before touching
                // the V3 clock. Stale callers have already been rejected.
                var check = CheckCurrentDay(current.RunId, current.Revision);
                if (check.Status == EventCommitStatus.Committed)
                    return new EventCommitResult(EventCommitStatus.Rejected,
                        "event.choice_required", check.Revision);
                if (check.Status != EventCommitStatus.NoEligibleEvent &&
                    check.Status != EventCommitStatus.AlreadyCommitted)
                    return check;
                var observed = Read();
                if (!Valid(observed) || observed.State!.Revision != check.Revision)
                    return LoadFailure(new LoadResult(LoadStatus.RecoveryRequired,
                        reason: "save.recovery_required"));
                if (observed.State.EventV4!.Journal!.PendingTicketId.Length > 0)
                    return Reject(observed.State, "event.choice_required");
                // The internal, authenticated daily check used one CAS
                // revision. Retry the *same* player command against that
                // newly verified frontier, not a second caller-supplied ID.
                return ExecuteCoreInternal(new CommandEnvelope(command.RunId,
                    command.CommandId, observed.State.Revision, command.Command), false);
            }
            try
            {
                var oldV3 = Convert.FromBase64String(current.EventV4.EmbeddedV3EnvelopeBase64);
                var catalog = serializer.ResolveCatalog(journal);
                var outcome = coreRunner.Evaluate(oldV3, command, out var nextV3,
                    journal, catalog.Definitions);
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
            CommandResult? coreCommand = null, int revisionDelta = 1)
        {
            byte[] candidateBytes;
            try
            {
                var candidate = EventV4CompatibilitySerializer.Project(nextCore, payload);
                if ((revisionDelta != 1 && revisionDelta != 2) ||
                    candidate.Revision != checked(current.Revision + revisionDelta))
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
            if (!Valid(verified) ||
                verified.State!.Revision != checked(current.Revision + revisionDelta) ||
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
