#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // One authoritative read-side index for event selection and cold replay.
    // Each ticket is parsed once. Category windows use ordered dates and a
    // binary lower bound rather than rescanning every ticket per event.
    internal sealed class EventSelectionHistory
    {
        private sealed class Occurrence
        {
            internal int Count;
            internal SimDate LastDate;
            internal Occurrence(SimDate first) { Count = 1; LastDate = first; }
        }

        private readonly Dictionary<string, Occurrence> byEvent =
            new Dictionary<string, Occurrence>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<DateTime>> byCategory =
            new Dictionary<string, List<DateTime>>(StringComparer.Ordinal);

        internal static EventSelectionHistory FromCommitted(IReadOnlyList<EventTicketRecord> tickets)
        {
            var index = new EventSelectionHistory();
            foreach (var ticket in tickets) index.Append(ticket);
            return index;
        }

        internal void Append(EventTicketRecord ticket)
        {
            var date = Parse(ticket.OpenedDateIso);
            if (byEvent.TryGetValue(ticket.EventId, out var occurrence))
            {
                occurrence.Count = checked(occurrence.Count + 1);
                occurrence.LastDate = date;
            }
            else byEvent.Add(ticket.EventId, new Occurrence(date));

            if (!byCategory.TryGetValue(ticket.CategoryId, out var dates))
            {
                dates = new List<DateTime>();
                byCategory.Add(ticket.CategoryId, dates);
            }
            var instant = DateTimeOf(date);
            if (dates.Count > 0 && dates[dates.Count - 1] >= instant)
                throw new ArgumentException("Event category history must be chronological.");
            dates.Add(instant);
        }

        internal GameEventDefinition? Select(IReadOnlyList<GameEventDefinition> definitions,
            IReadOnlyDictionary<string, EventCategoryPolicy> categories,
            GameEventContext requested, ref uint rng)
        {
            var now = DateTimeOf(requested.Date);
            var allowed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var policy in categories.Values)
                if (Eligible(policy, now)) allowed.Add(policy.Id);
            // Recompute the requested context with actual committed occurrence history.
            var context = new GameEventContext(requested.OriginDate, requested.Date, requested.Stage,
                requested.Tags, byEvent.Select(kv => new EventOccurrence(kv.Key, kv.Value.Count, kv.Value.LastDate)));
            var candidates = definitions.Where(def => allowed.Contains(def.CategoryId)).ToArray();
            return GameEventSelector.Select(candidates, context, ref rng);
        }

        private bool Eligible(EventCategoryPolicy policy, DateTime date)
        {
            if (!byCategory.TryGetValue(policy.Id, out var dates) || dates.Count == 0) return true;
            var elapsed = (date - dates[dates.Count - 1]).Days;
            if (elapsed < 0) throw new ArgumentException("Event journal contains a future ticket.");
            if (elapsed < policy.CooldownDays) return false;
            var earliest = date.AddDays(-(policy.WindowDays - 1));
            var first = dates.BinarySearch(earliest);
            if (first < 0) first = ~first;
            else
                while (first > 0 && dates[first - 1] == earliest) first--;
            return dates.Count - first < policy.MaxEventsPerWindow;
        }

        internal static SimDate Parse(string date)
        {
            var value = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None);
            return new SimDate(value.Year, value.Month, value.Day);
        }

        private static DateTime DateTimeOf(SimDate date) =>
            new DateTime(date.Year, date.Month, date.Day);
    }
}
