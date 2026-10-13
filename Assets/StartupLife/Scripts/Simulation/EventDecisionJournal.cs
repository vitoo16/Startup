#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // Prospective V3/V4 composite effect: an immutable binding between the
    // authoritative pending ticket, authored outcome and one V3 receipt identity.
    // Constructed only by EventDecisionJournal.PlanReward; this is NOT a grant
    // or a second wallet authority.
    public sealed class EventRewardSettlementPlan
    {
        public string RunId { get; }
        public string CheckedDateIso { get; }
        public long ExpectedCashVnd { get; }
        public long ExpectedCareerXp { get; }
        public string TicketId { get; }
        public string EventId { get; }
        public string ChoiceId { get; }
        public string OutcomeId { get; }
        public string ResolutionCommandId { get; }
        public long CoreRevision { get; }
        // One V3 receipt and one V4 resolution increment different ledgers.
        public long GlobalRevision { get; }
        public const string ReservedReceiptPrefix = "v4reward/";
        public string CoreReceiptCommandId { get; }
        public long CashDeltaVnd { get; }
        public long CareerXpDelta { get; }

        // This is a deterministic V3 *receipt intent*, NOT authority to post
        // cash. The V4 settlement caller must prove the canonical payload
        // against the pending authored choice and then publish one CAS.
        public GameCommand ToCoreCommand() =>
            new GameCommand(CommandKind.SettleEventReward, TicketId, 0,
                ChoiceId, EventId);

        public bool MatchesCoreCommand(GameCommand command) =>
            command != null && command.CanonicalPayload == ToCoreCommand().CanonicalPayload;

        // Canonical receipt identity and metadata are shared between the
        // prospective candidate, cold V3 replay and locked V4 CAS admission.
        // This checks the receipt ONLY; each trust boundary must still prove
        // the authored ticket, historical frontiers and state transitions.
        public bool MatchesCoreReceipt(CommandReceipt receipt) =>
            receipt != null &&
            receipt.CommandId == CoreReceiptCommandId &&
            receipt.Payload == ToCoreCommand().CanonicalPayload &&
            receipt.Revision == CoreRevision &&
            !string.IsNullOrWhiteSpace(receipt.OperationId) &&
            receipt.Cue == "event.resolved" &&
            receipt.MinutesConsumed == 0 &&
            receipt.AdvanceTargetIso == "" &&
            receipt.ParentPayload == "";

        internal EventRewardSettlementPlan(GameState verifiedState,
            EventTicketRecord ticket, EventChoiceDefinition choice, string commandId,
            long coreRevision, long globalRevision)
        {
            RunId = verifiedState.RunId;
            CheckedDateIso = verifiedState.DateIso;
            ExpectedCashVnd = verifiedState.Cash;
            ExpectedCareerXp = verifiedState.Employment?.Xp ?? 0;
            TicketId = ticket.Id;
            EventId = ticket.EventId;
            ChoiceId = choice.Id;
            OutcomeId = choice.OutcomeId;
            ResolutionCommandId = commandId;
            // Fixed-width internal ID; never reuse the public V4 command ID.
            using (var sha = SHA256.Create())
            {
                var identity = Encoding.UTF8.GetBytes(RunId + "\n" + TicketId + "\n" + commandId);
                CoreReceiptCommandId = ReservedReceiptPrefix +
                    BitConverter.ToString(sha.ComputeHash(identity)).Replace("-", "").ToLowerInvariant();
            }
            CoreRevision = coreRevision;
            GlobalRevision = globalRevision;
            CashDeltaVnd = choice.Reward!.CashDeltaVnd;
            CareerXpDelta = choice.Reward.CareerXpDelta;
        }
    }

    // Pure transition planner: caller is responsible for committing Next atomically
    // with the normal GameSession receipt/CAS + versioned migration/replay proof.
    public static class EventDecisionJournal
    {
        public const string DailyCheckPrefix = "v4day/";
        public static string DailyCommandId(string dateIso) => DailyCheckPrefix + dateIso;
        public static EventJournalState New(string runId, uint eventRng)
        {
            if (string.IsNullOrWhiteSpace(runId) || eventRng == 0)
                throw new ArgumentException("A valid run identity and seeded event RNG are required.");
            return new EventJournalState { RunId = runId, EventRng = eventRng, InitialEventRng = eventRng };
        }

        public static EventJournalResult Offer(EventJournalState source,
            IReadOnlyList<GameEventDefinition> definitions, IReadOnlyList<EventCategoryPolicy> policies,
            GameEventContext context, long expectedRevision, string offerCommandId = "",
            long coreRevisionAtOffer = -1) =>
            OfferInternal(source, definitions, policies, context, expectedRevision,
                offerCommandId, coreRevisionAtOffer, false);

        // The daily driver persists a negative check as a single V4 journal
        // transition. Legacy read-only Offer() retains NoEligibleEvent semantics.
        public static EventJournalResult CheckDay(EventJournalState source,
            IReadOnlyList<GameEventDefinition> definitions, IReadOnlyList<EventCategoryPolicy> policies,
            GameEventContext context, long expectedRevision, string commandId,
            long coreRevisionAtCheck) =>
            OfferInternal(source, definitions, policies, context, expectedRevision,
                commandId, coreRevisionAtCheck, true);

        private static EventJournalResult OfferInternal(EventJournalState source,
            IReadOnlyList<GameEventDefinition> definitions, IReadOnlyList<EventCategoryPolicy> policies,
            GameEventContext context, long expectedRevision, string offerCommandId,
            long coreRevisionAtOffer, bool persistEmpty)
        {
            var policyIndex = ValidateWithFinancialReceipts(source, definitions, policies);
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (offerCommandId == null || coreRevisionAtOffer < -1 ||
                (persistEmpty && (coreRevisionAtOffer < 0 ||
                    offerCommandId != DailyCommandId(context.Date.ToString()))))
                return Reject(source, "event.invalid_command");
            if (source.OriginDateIso.Length > 0 && source.OriginDateIso != context.OriginDate.ToString())
                return Reject(source, "event.origin_mismatch");
            // Explicit operation identities are replayable even after a successful
            // resolve or a stale retry. Unnamed legacy planner calls remain non-idempotent.
            if (offerCommandId.Length > 0)
            {
                if (string.IsNullOrWhiteSpace(offerCommandId))
                    return Reject(source, "event.invalid_command");
                if (ConflictsWithBatchIdentity(source, offerCommandId))
                    return Reject(source, "event.command_id_conflict");
                var prior = source.Tickets.FirstOrDefault(x =>
                    x.OfferCommandId == offerCommandId);
                if (prior != null)
                    return prior.OpenedDateIso == context.Date.ToString() &&
                           prior.OriginDateIso == context.OriginDate.ToString() &&
                           prior.Stage == context.Stage &&
                           prior.Tags.SequenceEqual(context.Tags)
                        ? new EventJournalResult(EventJournalStatus.AlreadyCommitted, "", source.Copy(), prior.Id)
                        : Reject(source, "event.command_id_conflict");
                if (source.Tickets.Any(x => x.ResolutionCommandId == offerCommandId))
                    return Reject(source, "event.command_id_conflict");
                var priorEmpty = source.NoEligibleDays.FirstOrDefault(x =>
                    x.CommandId == offerCommandId);
                if (priorEmpty != null)
                    return priorEmpty.DateIso == context.Date.ToString() &&
                           priorEmpty.OriginDateIso == context.OriginDate.ToString() &&
                           priorEmpty.Stage == context.Stage &&
                           priorEmpty.Tags.SequenceEqual(context.Tags)
                        ? new EventJournalResult(EventJournalStatus.AlreadyCommitted,
                            "", source.Copy())
                        : Reject(source, "event.command_id_conflict");
            }
            if (expectedRevision != source.Revision) return Reject(source, "event.stale_revision");
            if (source.PendingTicketId.Length > 0) return Reject(source, "event.choice_required");
            if (source.Tickets.Count > 0 &&
                source.Tickets[0].OriginDateIso != context.OriginDate.ToString())
                return Reject(source, "event.origin_mismatch");
            if (source.Tickets.Count > 0 &&
                string.CompareOrdinal(context.Date.ToString(), source.Tickets[source.Tickets.Count - 1].OpenedDateIso) <= 0 ||
                source.NoEligibleDays.Count > 0 &&
                string.CompareOrdinal(context.Date.ToString(), source.NoEligibleDays[source.NoEligibleDays.Count - 1].DateIso) <= 0)
                return Reject(source, "event.window_already_processed");

            // Build once from committed journal. The same selector powers cold replay.
            // No caller-supplied occurrence or category counters are ever trusted.
            var history = EventSelectionHistory.FromCommitted(source.Tickets);
            var nextRng = source.EventRng;
            var selected = history.Select(definitions, policyIndex, context, ref nextRng);
            if (selected == null)
            {
                if (!persistEmpty)
                    return new EventJournalResult(EventJournalStatus.NoEligibleEvent,
                        "event.none", source.Copy());
                var empty = source.Copy();
                empty.NoEligibleDays.Add(new EventNoEligibleDayRecord
                {
                    CommandId = offerCommandId,
                    DateIso = context.Date.ToString(),
                    OriginDateIso = context.OriginDate.ToString(),
                    Stage = context.Stage, Tags = context.Tags.ToList(),
                    CoreRevisionAtCheck = coreRevisionAtOffer
                });
                empty.Revision = checked(empty.Revision + 1);
                return new EventJournalResult(EventJournalStatus.NoEligibleEvent,
                    "event.none", empty);
            }

            var candidate = source.Copy();
            var ticketId = candidate.RunId + "/event/" +
                candidate.NextTicketSequence.ToString(CultureInfo.InvariantCulture);
            candidate.NextTicketSequence = checked(candidate.NextTicketSequence + 1);
            candidate.EventRng = nextRng;
            candidate.Tickets.Add(new EventTicketRecord
            {
                Id = ticketId, EventId = selected.Id, CategoryId = selected.CategoryId,
                OpenedDateIso = context.Date.ToString(),
                OriginDateIso = context.OriginDate.ToString(),
                Stage = context.Stage,
                Tags = context.Tags.ToList(),
                CoreRevisionAtOffer = coreRevisionAtOffer,
                OfferCommandId = offerCommandId.Length > 0
                    ? offerCommandId : source.RunId + "/event-offer/" + context.Date.ToString()
            });
            candidate.PendingTicketId = ticketId;
            candidate.Revision = checked(candidate.Revision + 1);
            return new EventJournalResult(EventJournalStatus.Offered, "", candidate, ticketId);
        }

        public static EventJournalResult Resolve(EventJournalState source,
            IReadOnlyList<GameEventDefinition> definitions, IReadOnlyList<EventCategoryPolicy> policies,
            long expectedRevision, string commandId, string ticketId, string choiceId) =>
            ResolveInternal(source, definitions, policies, expectedRevision,
                commandId, ticketId, choiceId, null);

        // Stage only: this trusted plan proves the intended V4 outcome, but the
        // returned journal CANNOT be saved until a matching V3 financial receipt
        // is cold-replayed within the same outer V4 CAS. Normal Resolve still
        // rejects rewarding choices.
        public static EventJournalResult StagePlannedRewardResolution(
            EventJournalState source, IReadOnlyList<GameEventDefinition> definitions,
            IReadOnlyList<EventCategoryPolicy> policies, EventRewardSettlementPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            return ResolveInternal(source, definitions, policies, source.Revision,
                plan.ResolutionCommandId, plan.TicketId, plan.ChoiceId, plan);
        }

        private static EventJournalResult ResolveInternal(EventJournalState source,
            IReadOnlyList<GameEventDefinition> definitions, IReadOnlyList<EventCategoryPolicy> policies,
            long expectedRevision, string commandId, string ticketId, string choiceId,
            EventRewardSettlementPlan? rewardPlan)
        {
            ValidateWithFinancialReceipts(source, definitions, policies);
            if (string.IsNullOrWhiteSpace(commandId) || string.IsNullOrWhiteSpace(ticketId) ||
                string.IsNullOrWhiteSpace(choiceId))
                return Reject(source, "event.invalid_command");
            if (ConflictsWithBatchIdentity(source, commandId))
                return Reject(source, "event.command_id_conflict");
            var previous = source.Tickets.FirstOrDefault(x =>
                string.Equals(x.ResolutionCommandId, commandId, StringComparison.Ordinal));
            if (previous != null)
                return previous.Id == ticketId && previous.ChoiceId == choiceId
                    ? new EventJournalResult(EventJournalStatus.AlreadyCommitted, "", source.Copy(),
                        previous.Id, previous.OutcomeId)
                    : Reject(source, "event.command_id_conflict");
            if (source.Tickets.Any(x => x.OfferCommandId == commandId))
                return Reject(source, "event.command_id_conflict");
            if (source.Revision != expectedRevision) return Reject(source, "event.stale_revision");
            if (source.PendingTicketId != ticketId)
                return Reject(source, "event.choice_not_pending");
            var ticket = source.Tickets[source.Tickets.Count - 1];
            var definition = definitions.Single(x => x.Id == ticket.EventId);
            var selectedChoice = definition.Choices.FirstOrDefault(x => x.Id == choiceId);
            if (selectedChoice == null) return Reject(source, "event.choice_invalid");
            // Only an immutable plan produced from a V4-verified pending choice
            // may stage a reward. This is *not* permission to publish the journal:
            // cold replay still rejects reward outcomes without a V3 receipt.
            if (selectedChoice.Reward != null)
            {
                if (rewardPlan == null)
                    return Reject(source, "event.reward_processor_unavailable");
                if (rewardPlan.RunId != source.RunId ||
                    rewardPlan.TicketId != ticket.Id ||
                    rewardPlan.EventId != ticket.EventId ||
                    rewardPlan.ChoiceId != selectedChoice.Id ||
                    rewardPlan.OutcomeId != selectedChoice.OutcomeId ||
                    rewardPlan.ResolutionCommandId != commandId ||
                    rewardPlan.CheckedDateIso != ticket.OpenedDateIso ||
                    rewardPlan.CoreRevision <= ticket.CoreRevisionAtOffer ||
                    rewardPlan.GlobalRevision != checked(rewardPlan.CoreRevision + source.Revision + 1L) ||
                    rewardPlan.CashDeltaVnd != selectedChoice.Reward.CashDeltaVnd ||
                    rewardPlan.CareerXpDelta != selectedChoice.Reward.CareerXpDelta)
                    return Reject(source, "event.reward_plan_mismatch");
            }
            else if (rewardPlan != null)
                return Reject(source, "event.reward_plan_mismatch");

            var candidate = source.Copy();
            var pending = candidate.Tickets[candidate.Tickets.Count - 1];
            pending.ChoiceId = selectedChoice.Id;
            pending.OutcomeId = selectedChoice.OutcomeId;
            pending.ResolutionCommandId = commandId;
            candidate.PendingTicketId = "";
            candidate.OutcomeReceipts.Add(new EventOutcomeReceipt
            {
                Id = ticket.Id + "/effect",
                TicketId = ticket.Id,
                ResolutionCommandId = commandId,
                OutcomeId = selectedChoice.OutcomeId,
                GrantedTags = selectedChoice.GrantedTags.ToList()
            });
            candidate.Revision = checked(candidate.Revision + 1);
            return new EventJournalResult(EventJournalStatus.Resolved, "", candidate,
                ticketId, selectedChoice.OutcomeId);
        }

        // Prepare the only admissible payout provenance before touching V3.
        // This function has no side effects: it cannot credit cash/XP and is
        // deliberately not a substitute for the future composite V3 receipt.
        // Always feed it a freshly verified V4 projection, never client state.
        public static EventRewardSettlementPlan PlanReward(GameState current,
            IReadOnlyList<GameEventDefinition> definitions,
            IReadOnlyList<EventCategoryPolicy> policies, string commandId,
            string ticketId, string choiceId)
        {
            if (current?.SaveVersion != 4 || current.EventV4?.Journal == null ||
                current.Receipts == null || definitions == null || policies == null)
                throw new ArgumentException("Verified V4 reward projection required.");
            var journal = current.EventV4.Journal;
            ValidateWithFinancialReceipts(journal, definitions, policies);
            if (journal.RunId != current.RunId ||
                current.Revision != checked(current.Receipts.Count + journal.Revision) ||
                journal.PendingTicketId != ticketId ||
                current.PendingChoiceId != journal.PendingTicketId ||
                current.EventRng != journal.EventRng ||
                journal.Tickets.Count == 0 ||
                current.Name.Length == 0 || current.DateIso.Length == 0)
                throw new ArgumentException("Reward requires a current pending event.");
            var ticket = journal.Tickets[journal.Tickets.Count - 1];
            if (ticket.Id != ticketId || ticket.OpenedDateIso != current.DateIso ||
                ticket.CoreRevisionAtOffer < 0 ||
                ticket.CoreRevisionAtOffer > current.Receipts.Count)
                throw new ArgumentException("Event offer frontier is not authoritative.");
            if (string.IsNullOrWhiteSpace(commandId) || commandId.Length > 256 ||
                commandId.StartsWith(DailyCheckPrefix, StringComparison.Ordinal) ||
                ConflictsWithBatchIdentity(journal, commandId) ||
                journal.Tickets.Any(x => x.OfferCommandId == commandId ||
                    x.ResolutionCommandId == commandId) ||
                journal.NoEligibleDays.Any(x => x.CommandId == commandId) ||
                current.Receipts.Any(x => x.CommandId == commandId))
                throw new ArgumentException("Reward command identity is already owned.");
            var definition = definitions.Single(x => x.Id == ticket.EventId);
            var choice = definition.Choices.SingleOrDefault(x => x.Id == choiceId);
            if (choice?.Reward == null)
                throw new ArgumentException("No financial effect on this authored choice.");
            var reward = choice.Reward;
            var unavailable = RewardUnavailableReason(current, reward);
            if (unavailable.Length != 0)
                throw new ArgumentException("Event reward is unavailable: " + unavailable);
            // Keep checked settlement arithmetic. Only authenticated V3/V4 CAS
            // applies the reward; this preflight never mutates the save.
            _ = checked(current.Cash + reward.CashDeltaVnd);
            if (reward.CareerXpDelta > 0)
                _ = checked(current.Employment!.Xp + reward.CareerXpDelta);
            return new EventRewardSettlementPlan(current, ticket, choice, commandId,
                checked(current.Receipts.Count + 1L), checked(current.Revision + 2L));
        }

        // Shared read-side preview and authoritative settlement preflight.
        // Returned localization keys describe blockers; no money or XP moves.
        public static string RewardUnavailableReason(GameState state, EventChoiceReward reward)
        {
            if (state == null || reward == null)
                throw new ArgumentNullException("A reward preview needs state and authored data.");
            if (reward.CashDeltaVnd < 0 && state.Cash < -reward.CashDeltaVnd)
                return "event.choice.insufficient_funds";
            if (reward.CashDeltaVnd > 0 &&
                state.Cash > long.MaxValue - reward.CashDeltaVnd)
                return "event.choice.reward_unavailable";
            if (reward.CareerXpDelta > 0 &&
                (state.Employment == null ||
                 state.Employment.EndedIso.Length > 0 ||
                 state.Employment.Xp > long.MaxValue - reward.CareerXpDelta))
                return "event.choice.reward_unavailable";
            return "";
        }

        // Structural and content validation for prospective cold restoration.
        // Not a substitute for main-save receipt replay once M15 is integrated.
        public static IReadOnlyDictionary<string, EventCategoryPolicy> Validate(EventJournalState state,
            IReadOnlyList<GameEventDefinition> definitions, IReadOnlyList<EventCategoryPolicy> policies) =>
            ValidateCore(state, definitions, policies, false);

        // Only V4's joint cold-replay path may admit authored financial outcomes.
        // The normal journal-only validator still rejects them.
        public static IReadOnlyDictionary<string, EventCategoryPolicy> ValidateWithFinancialReceipts(
            EventJournalState state, IReadOnlyList<GameEventDefinition> definitions,
            IReadOnlyList<EventCategoryPolicy> policies) =>
            ValidateCore(state, definitions, policies, true);

        private static IReadOnlyDictionary<string, EventCategoryPolicy> ValidateCore(
            EventJournalState state, IReadOnlyList<GameEventDefinition> definitions,
            IReadOnlyList<EventCategoryPolicy> policies, bool jointReplay)
        {
            if (state == null || definitions == null || policies == null ||
                string.IsNullOrWhiteSpace(state.RunId) || state.EventRng == 0 || state.InitialEventRng == 0 ||
                state.Tickets == null || state.PendingTicketId == null ||
                state.OutcomeReceipts == null || state.AdvanceBatches == null ||
                state.NoEligibleDays == null ||
                state.NextTicketSequence != state.Tickets.Count + 1L ||
                (state.OriginDateIso.Length > 0 &&
                    EventSelectionHistory.Parse(state.OriginDateIso).ToString() != state.OriginDateIso) ||
                state.Revision < 0 ||
                state.Revision > 2L * state.Tickets.Count + state.AdvanceBatches.Count +
                    state.NoEligibleDays.Count ||
                (state.Tickets.Count == 0 && state.EventRng != state.InitialEventRng))
                throw new ArgumentException("Invalid event journal or catalog.");
            if (definitions.Any(x => x == null) ||
                definitions.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != definitions.Count ||
                policies.Any(x => x == null) ||
                policies.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != policies.Count)
                throw new ArgumentException("Duplicate or null event/category content.");
            var events = definitions.ToDictionary(x => x.Id, StringComparer.Ordinal);
            var categories = policies.ToDictionary(x => x.Id, StringComparer.Ordinal);
            if (definitions.Any(x => !categories.ContainsKey(x.CategoryId)))
                throw new ArgumentException("An event has no category policy.");
            // Until V3-native reward receipts are implemented, a reward-only
            // event would trap the player in an unresolvable pending choice.
            // Every eligible event needs at least one safe tag-only option.
            if (definitions.Any(x => x.Choices.All(choice => choice.Reward != null)))
                throw new ArgumentException("Reward-only events cannot yet be resolved.");
            var completed = 0;
            var usedCommands = new HashSet<string>(StringComparer.Ordinal);
            string lastDay = "";
            string originDay = "";
            for (var i = 0; i < state.Tickets.Count; i++)
            {
                var ticket = state.Tickets[i];
                if (ticket == null || ticket.Id != state.RunId + "/event/" +
                        (i + 1).ToString(CultureInfo.InvariantCulture) ||
                    !events.TryGetValue(ticket.EventId, out var definition) ||
                    definition.CategoryId != ticket.CategoryId ||
                    string.IsNullOrWhiteSpace(ticket.OfferCommandId) ||
                    (ticket.OfferCommandId.StartsWith(DailyCheckPrefix, StringComparison.Ordinal) &&
                     ticket.OfferCommandId != DailyCommandId(ticket.OpenedDateIso)) ||
                    ConflictsWithBatchIdentity(state, ticket.OfferCommandId) ||
                    !usedCommands.Add(ticket.OfferCommandId) ||
                    ticket.CoreRevisionAtOffer < -1 ||
                    string.IsNullOrWhiteSpace(ticket.OpenedDateIso) ||
                    (lastDay.Length > 0 && string.CompareOrdinal(lastDay, ticket.OpenedDateIso) >= 0))
                    throw new ArgumentException("Invalid event ticket identity, content or chronology.");
                var date = Parse(ticket.OpenedDateIso);
                if (date.ToString() != ticket.OpenedDateIso ||
                    string.IsNullOrWhiteSpace(ticket.OriginDateIso) ||
                    Parse(ticket.OriginDateIso).ToString() != ticket.OriginDateIso ||
                    ticket.OriginDateIso.CompareTo(ticket.OpenedDateIso) > 0 ||
                    (originDay.Length > 0 && originDay != ticket.OriginDateIso) ||
                    (state.OriginDateIso.Length > 0 && state.OriginDateIso != ticket.OriginDateIso) ||
                    ticket.Stage < 0 || ticket.Tags == null ||
                    ticket.Tags.Any(x => !ContentId.IsValid(x)) ||
                    !ticket.Tags.SequenceEqual(ticket.Tags.Distinct(StringComparer.Ordinal)
                        .OrderBy(x => x, StringComparer.Ordinal)))
                    throw new ArgumentException("Noncanonical event offer context.");
                lastDay = ticket.OpenedDateIso;
                originDay = ticket.OriginDateIso;

                var resolved = ticket.ResolutionCommandId.Length > 0;
                if (resolved)
                {
                    var choice = definition.Choices.FirstOrDefault(x =>
                        x.Id == ticket.ChoiceId && x.OutcomeId == ticket.OutcomeId);
                    // A newly authored money/XP consequence cannot be
                    // retroactively attached to an older tag-only V4 receipt.
                    // Until V3-native financial receipts are coupled to this
                    // ticket, cold admission must fail instead of inventing credit.
                    if (string.IsNullOrWhiteSpace(ticket.ChoiceId) ||
                        (choice?.Reward != null && !jointReplay) ||
                        ConflictsWithBatchIdentity(state, ticket.ResolutionCommandId) ||
                        !usedCommands.Add(ticket.ResolutionCommandId) || choice == null ||
                        completed >= state.OutcomeReceipts.Count)
                        throw new ArgumentException("Resolved event choice/outcome does not match content.");
                    var effect = state.OutcomeReceipts[completed];
                    if (effect == null || effect.Id != ticket.Id + "/effect" ||
                        effect.TicketId != ticket.Id ||
                        effect.ResolutionCommandId != ticket.ResolutionCommandId ||
                        effect.OutcomeId != ticket.OutcomeId ||
                        effect.GrantedTags == null ||
                        !effect.GrantedTags.SequenceEqual(choice.GrantedTags))
                        throw new ArgumentException("Event choice effect receipt differs from authored outcome.");
                    completed++;
                }
                else if (i != state.Tickets.Count - 1 || ticket.ChoiceId.Length > 0 || ticket.OutcomeId.Length > 0 ||
                    state.PendingTicketId != ticket.Id)
                    throw new ArgumentException("Only the last event may have an unresolved choice.");
            }
            var checkedDates = new HashSet<string>(state.Tickets.Select(x => x.OpenedDateIso),
                StringComparer.Ordinal);
            var lastChecked = "";
            foreach (var day in state.NoEligibleDays)
            {
                if (day == null || day.CommandId != DailyCommandId(day.DateIso) ||
                    day.CommandId.Length > 256 ||
                    ConflictsWithBatchIdentity(state, day.CommandId) ||
                    !usedCommands.Add(day.CommandId) ||
                    day.CoreRevisionAtCheck < 0 ||
                    string.IsNullOrWhiteSpace(day.DateIso) ||
                    EventSelectionHistory.Parse(day.DateIso).ToString() != day.DateIso ||
                    !checkedDates.Add(day.DateIso) ||
                    (lastChecked.Length > 0 &&
                        string.CompareOrdinal(lastChecked, day.DateIso) >= 0) ||
                    string.IsNullOrWhiteSpace(day.OriginDateIso) ||
                    EventSelectionHistory.Parse(day.OriginDateIso).ToString() != day.OriginDateIso ||
                    day.OriginDateIso != state.OriginDateIso ||
                    string.CompareOrdinal(day.OriginDateIso, day.DateIso) > 0 ||
                    day.Stage < 0 || day.Tags == null ||
                    day.Tags.Any(x => !ContentId.IsValid(x)) ||
                    !day.Tags.SequenceEqual(day.Tags.Distinct(StringComparer.Ordinal)
                        .OrderBy(x => x, StringComparer.Ordinal)))
                    throw new ArgumentException("Invalid no-eligible event day checkpoint.");
                lastChecked = day.DateIso;
            }
            foreach (var batch in state.AdvanceBatches)
            {
                if (batch == null || string.IsNullOrWhiteSpace(batch.RootCommandId) ||
                    batch.RootCommandId.Length > 256 ||
                    batch.RootCommandId.StartsWith(EventAdvanceBatchHistory.ReservedChildPrefix,
                        StringComparison.Ordinal) ||
                    batch.RootCommandId.StartsWith(DailyCheckPrefix,
                        StringComparison.Ordinal) ||
                    !usedCommands.Add(batch.RootCommandId) ||
                    batch.Mode != "day" && batch.Mode != "month")
                    throw new ArgumentException("Invalid or conflicting event advance batch identity.");
                // Date, V3 receipt and target proofs run against verified V3 in
                // EventAdvanceBatchHistory.Validate, not against UI-provided dates.
            }
            if (state.OutcomeReceipts.Count != completed ||
                state.Revision != state.Tickets.Count + completed + state.AdvanceBatches.Count +
                    state.NoEligibleDays.Count ||
                (state.Tickets.Count == 0 || state.Tickets[state.Tickets.Count - 1].ResolutionCommandId.Length > 0) &&
                    state.PendingTicketId.Length != 0)
                throw new ArgumentException("Event choice commands or revision are inconsistent.");
            return categories;
        }

        // The same namespace rule protects standalone planners and cold journal
        // validation, not merely commands entering through the V4 save gateway.
        private static bool ConflictsWithBatchIdentity(EventJournalState state, string id) =>
            id.StartsWith(EventAdvanceBatchHistory.ReservedChildPrefix,
                StringComparison.Ordinal) ||
            id.StartsWith(EventRewardSettlementPlan.ReservedReceiptPrefix,
                StringComparison.Ordinal) ||
            state.AdvanceBatches.Any(x => x != null && x.RootCommandId == id);

        private static SimDate Parse(string date)
        {
            var d = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None);
            return new SimDate(d.Year, d.Month, d.Day);
        }

        private static EventJournalResult Reject(EventJournalState state, string reason) =>
            new EventJournalResult(EventJournalStatus.Rejected, reason, state.Copy());
    }
}
