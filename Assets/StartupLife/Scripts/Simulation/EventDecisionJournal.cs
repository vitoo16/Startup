#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // Pure transition planner: caller is responsible for committing Next atomically
    // with the normal GameSession receipt/CAS + versioned migration/replay proof.
    public static class EventDecisionJournal
    {
        public static EventJournalState New(string runId, uint eventRng)
        {
            if (string.IsNullOrWhiteSpace(runId) || eventRng == 0)
                throw new ArgumentException("A valid run identity and seeded event RNG are required.");
            return new EventJournalState { RunId = runId, EventRng = eventRng, InitialEventRng = eventRng };
        }

        public static EventJournalResult Offer(EventJournalState source,
            IReadOnlyList<GameEventDefinition> definitions, IReadOnlyList<EventCategoryPolicy> policies,
            GameEventContext context, long expectedRevision)
        {
            var policyIndex = Validate(source, definitions, policies);
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (expectedRevision != source.Revision) return Reject(source, "event.stale_revision");
            if (source.PendingTicketId.Length > 0) return Reject(source, "event.choice_required");
            if (source.Tickets.Count > 0 &&
                string.CompareOrdinal(context.Date.ToString(), source.Tickets[source.Tickets.Count - 1].OpenedDateIso) <= 0)
                return Reject(source, "event.window_already_processed");

            var occurrences = source.Tickets.GroupBy(x => x.EventId, StringComparer.Ordinal)
                .Select(g => new EventOccurrence(g.Key, g.Count(), Parse(g.Last().OpenedDateIso)));
            // Build from committed journal, never trust client-supplied occurrence counts.
            var verifiedContext = new GameEventContext(context.OriginDate, context.Date,
                context.Stage, context.Tags, occurrences);
            var candidates = definitions.Where(x => CategoryEligible(x.CategoryId,
                policyIndex[x.CategoryId], source.Tickets, context.Date)).ToArray();
            var nextRng = source.EventRng;
            var selected = GameEventSelector.Select(candidates, verifiedContext, ref nextRng);
            if (selected == null)
                return new EventJournalResult(EventJournalStatus.NoEligibleEvent, "event.none", source.Copy());

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
                Tags = context.Tags.ToList()
            });
            candidate.PendingTicketId = ticketId;
            candidate.Revision = checked(candidate.Revision + 1);
            return new EventJournalResult(EventJournalStatus.Offered, "", candidate, ticketId);
        }

        public static EventJournalResult Resolve(EventJournalState source,
            IReadOnlyList<GameEventDefinition> definitions, IReadOnlyList<EventCategoryPolicy> policies,
            long expectedRevision, string commandId, string ticketId, string choiceId)
        {
            Validate(source, definitions, policies);
            if (string.IsNullOrWhiteSpace(commandId) || string.IsNullOrWhiteSpace(ticketId) ||
                string.IsNullOrWhiteSpace(choiceId))
                return Reject(source, "event.invalid_command");
            var previous = source.Tickets.FirstOrDefault(x =>
                string.Equals(x.ResolutionCommandId, commandId, StringComparison.Ordinal));
            if (previous != null)
                return previous.Id == ticketId && previous.ChoiceId == choiceId
                    ? new EventJournalResult(EventJournalStatus.AlreadyCommitted, "", source.Copy(),
                        previous.Id, previous.OutcomeId)
                    : Reject(source, "event.command_id_conflict");
            if (source.Revision != expectedRevision) return Reject(source, "event.stale_revision");
            if (source.PendingTicketId != ticketId)
                return Reject(source, "event.choice_not_pending");
            var ticket = source.Tickets[source.Tickets.Count - 1];
            var definition = definitions.Single(x => x.Id == ticket.EventId);
            var selectedChoice = definition.Choices.FirstOrDefault(x => x.Id == choiceId);
            if (selectedChoice == null) return Reject(source, "event.choice_invalid");

            var candidate = source.Copy();
            var pending = candidate.Tickets[candidate.Tickets.Count - 1];
            pending.ChoiceId = selectedChoice.Id;
            pending.OutcomeId = selectedChoice.OutcomeId;
            pending.ResolutionCommandId = commandId;
            candidate.PendingTicketId = "";
            candidate.Revision = checked(candidate.Revision + 1);
            return new EventJournalResult(EventJournalStatus.Resolved, "", candidate,
                ticketId, selectedChoice.OutcomeId);
        }

        // Structural and content validation for prospective cold restoration.
        // Not a substitute for main-save receipt replay once M15 is integrated.
        public static IReadOnlyDictionary<string, EventCategoryPolicy> Validate(EventJournalState state,
            IReadOnlyList<GameEventDefinition> definitions, IReadOnlyList<EventCategoryPolicy> policies)
        {
            if (state == null || definitions == null || policies == null ||
                string.IsNullOrWhiteSpace(state.RunId) || state.EventRng == 0 || state.InitialEventRng == 0 ||
                state.Tickets == null || state.PendingTicketId == null ||
                state.NextTicketSequence != state.Tickets.Count + 1L ||
                state.Revision < 0 || state.Revision > 2L * state.Tickets.Count ||
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
            var completed = 0;
            string lastDay = "";
            for (var i = 0; i < state.Tickets.Count; i++)
            {
                var ticket = state.Tickets[i];
                if (ticket == null || ticket.Id != state.RunId + "/event/" +
                        (i + 1).ToString(CultureInfo.InvariantCulture) ||
                    !events.TryGetValue(ticket.EventId, out var definition) ||
                    definition.CategoryId != ticket.CategoryId ||
                    string.IsNullOrWhiteSpace(ticket.OpenedDateIso) ||
                    (lastDay.Length > 0 && string.CompareOrdinal(lastDay, ticket.OpenedDateIso) >= 0))
                    throw new ArgumentException("Invalid event ticket identity, content or chronology.");
                var date = Parse(ticket.OpenedDateIso);
                if (date.ToString() != ticket.OpenedDateIso ||
                    string.IsNullOrWhiteSpace(ticket.OriginDateIso) ||
                    Parse(ticket.OriginDateIso).ToString() != ticket.OriginDateIso ||
                    ticket.OriginDateIso.CompareTo(ticket.OpenedDateIso) > 0 ||
                    ticket.Stage < 0 || ticket.Tags == null ||
                    ticket.Tags.Any(x => !ContentId.IsValid(x)) ||
                    !ticket.Tags.SequenceEqual(ticket.Tags.Distinct(StringComparer.Ordinal)
                        .OrderBy(x => x, StringComparer.Ordinal)))
                    throw new ArgumentException("Noncanonical event offer context.");
                lastDay = ticket.OpenedDateIso;

                var resolved = ticket.ResolutionCommandId.Length > 0;
                if (resolved)
                {
                    completed++;
                    if (string.IsNullOrWhiteSpace(ticket.ChoiceId) ||
                        !definition.Choices.Any(x => x.Id == ticket.ChoiceId && x.OutcomeId == ticket.OutcomeId))
                        throw new ArgumentException("Resolved event choice/outcome does not match content.");
                }
                else if (i != state.Tickets.Count - 1 || ticket.ChoiceId.Length > 0 || ticket.OutcomeId.Length > 0 ||
                    state.PendingTicketId != ticket.Id)
                    throw new ArgumentException("Only the last event may have an unresolved choice.");
            }
            if (state.Tickets.Select(x => x.ResolutionCommandId)
                    .Where(x => x.Length > 0).Distinct(StringComparer.Ordinal).Count() != completed ||
                state.Revision != state.Tickets.Count + completed ||
                (state.Tickets.Count == 0 || state.Tickets[state.Tickets.Count - 1].ResolutionCommandId.Length > 0) &&
                    state.PendingTicketId.Length != 0)
                throw new ArgumentException("Event choice commands or revision are inconsistent.");
            return categories;
        }

        private static bool CategoryEligible(string categoryId, EventCategoryPolicy policy,
            IReadOnlyList<EventTicketRecord> history, SimDate date)
        {
            var windowCount = 0;
            for (var i = history.Count - 1; i >= 0; i--)
            {
                var ticket = history[i];
                var elapsed = (new DateTime(date.Year, date.Month, date.Day) -
                    new DateTime(Parse(ticket.OpenedDateIso).Year,
                        Parse(ticket.OpenedDateIso).Month, Parse(ticket.OpenedDateIso).Day)).Days;
                if (elapsed < 0) throw new ArgumentException("Event journal contains a future event.");
                if (ticket.CategoryId != categoryId) continue;
                if (elapsed < policy.CooldownDays) return false;
                if (elapsed < policy.WindowDays) windowCount++;
            }
            return windowCount < policy.MaxEventsPerWindow;
        }

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
