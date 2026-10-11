#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // Reconstruct every persisted offer and resolution using the original event RNG.
    // An isolated journal does not prove GameSession time/tag authority; that requires
    // integrating these events as normal versioned GameSession receipts.
    public static class EventJournalReplay
    {
        public static void Validate(EventJournalState saved,
            IReadOnlyList<GameEventDefinition> definitions,
            IReadOnlyList<EventCategoryPolicy> policies)
        {
            EventDecisionJournal.Validate(saved, definitions, policies);
            var trace = EventDecisionJournal.New(saved.RunId, saved.InitialEventRng);
            foreach (var ticket in saved.Tickets)
            {
                var start = Parse(ticket.OriginDateIso);
                var date = Parse(ticket.OpenedDateIso);
                var context = new GameEventContext(start, date, ticket.Stage, ticket.Tags,
                    Array.Empty<EventOccurrence>());
                var offer = EventDecisionJournal.Offer(trace, definitions, policies, context, trace.Revision);
                if (offer.Status != EventJournalStatus.Offered || offer.TicketId != ticket.Id ||
                    offer.Next.Tickets[offer.Next.Tickets.Count - 1].EventId != ticket.EventId)
                    throw new ArgumentException("Event replay selection or occurrence differs from source history.");
                trace = offer.Next;
                if (ticket.ResolutionCommandId.Length > 0)
                {
                    var resolved = EventDecisionJournal.Resolve(trace, definitions, policies, trace.Revision,
                        ticket.ResolutionCommandId, ticket.Id, ticket.ChoiceId);
                    if (resolved.Status != EventJournalStatus.Resolved ||
                        resolved.OutcomeId != ticket.OutcomeId)
                        throw new ArgumentException("Event choice cannot be reproduced from catalog.");
                    trace = resolved.Next;
                }
            }
            if (trace.EventRng != saved.EventRng ||
                trace.InitialEventRng != saved.InitialEventRng ||
                trace.Revision != saved.Revision ||
                trace.NextTicketSequence != saved.NextTicketSequence ||
                trace.PendingTicketId != saved.PendingTicketId ||
                trace.Tickets.Count != saved.Tickets.Count)
                throw new ArgumentException("Event replay RNG, revision or pending choice drift.");
            for (var i = 0; i < trace.Tickets.Count; i++)
            {
                var actual = trace.Tickets[i];
                var claimed = saved.Tickets[i];
                if (actual.Id != claimed.Id || actual.EventId != claimed.EventId ||
                    actual.CategoryId != claimed.CategoryId ||
                    actual.OpenedDateIso != claimed.OpenedDateIso ||
                    actual.OriginDateIso != claimed.OriginDateIso || actual.Stage != claimed.Stage ||
                    !actual.Tags.SequenceEqual(claimed.Tags) ||
                    actual.ChoiceId != claimed.ChoiceId ||
                    actual.OutcomeId != claimed.OutcomeId ||
                    actual.ResolutionCommandId != claimed.ResolutionCommandId)
                    throw new ArgumentException("Event ticket cannot be reconstructed byte-semantically.");
            }
        }

        private static SimDate Parse(string date)
        {
            var parsed = DateTime.ParseExact(date, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture);
            return new SimDate(parsed.Year, parsed.Month, parsed.Day);
        }
    }
}
