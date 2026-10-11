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
            foreach (var ticket in saved.Tickets)
            {
                var origin = EventSelectionHistory.Parse(ticket.OriginDateIso);
                var date = EventSelectionHistory.Parse(ticket.OpenedDateIso);
                var context = new GameEventContext(origin, date, ticket.Stage,
                    ticket.Tags, Array.Empty<EventOccurrence>());
                var selected = history.Select(definitions, catalog, context, ref rng);
                if (selected == null || selected.Id != ticket.EventId ||
                    selected.CategoryId != ticket.CategoryId)
                    throw new ArgumentException("Event replay selection or occurrence differs from source history.");
                history.Append(ticket);
            }
            if (rng != saved.EventRng)
                throw new ArgumentException("Event replay RNG differs from committed journal.");
        }
    }
}
