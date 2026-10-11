#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // Cold replay is linear in committed tickets (plus eligible catalog evaluation).
    // Reuse the production selector, not a second implementation of event rules.
    // GameSession time/tag authority still needs receipt integration.
    public static class EventJournalReplay
    {
        public static void Validate(EventJournalState saved,
            IReadOnlyList<GameEventDefinition> definitions,
            IReadOnlyList<EventCategoryPolicy> policies)
        {
            var catalog = EventDecisionJournal.Validate(saved, definitions, policies);
            var history = new EventSelectionHistory();
            var rng = saved.InitialEventRng;
            // Recheck positive and negative windows in chronological order with
            // the *same* production weighted selector and occurrence index.
            var ticketIndex = 0;
            var emptyIndex = 0;
            while (ticketIndex < saved.Tickets.Count ||
                   emptyIndex < saved.NoEligibleDays.Count)
            {
                var nextTicket = ticketIndex < saved.Tickets.Count
                    ? saved.Tickets[ticketIndex] : null;
                var nextEmpty = emptyIndex < saved.NoEligibleDays.Count
                    ? saved.NoEligibleDays[emptyIndex] : null;
                if (nextEmpty != null && (nextTicket == null ||
                    string.CompareOrdinal(nextEmpty.DateIso, nextTicket.OpenedDateIso) < 0))
                {
                    var context = new GameEventContext(
                        EventSelectionHistory.Parse(nextEmpty.OriginDateIso),
                        EventSelectionHistory.Parse(nextEmpty.DateIso), nextEmpty.Stage,
                        nextEmpty.Tags, Array.Empty<EventOccurrence>());
                    if (history.Select(definitions, catalog, context, ref rng) != null)
                        throw new ArgumentException(
                            "Previously empty event window is eligible on cold replay.");
                    emptyIndex++;
                    continue;
                }
                var ticket = nextTicket!;
                var origin = EventSelectionHistory.Parse(ticket.OriginDateIso);
                var date = EventSelectionHistory.Parse(ticket.OpenedDateIso);
                var offered = new GameEventContext(origin, date, ticket.Stage,
                    ticket.Tags, Array.Empty<EventOccurrence>());
                var selected = history.Select(definitions, catalog, offered, ref rng);
                if (selected == null || selected.Id != ticket.EventId ||
                    selected.CategoryId != ticket.CategoryId)
                    throw new ArgumentException(
                        "Event replay selection or occurrence differs from source history.");
                history.Append(ticket);
                ticketIndex++;
            }
            if (rng != saved.EventRng)
                throw new ArgumentException("Event replay RNG differs from committed journal.");
        }
    }
}
