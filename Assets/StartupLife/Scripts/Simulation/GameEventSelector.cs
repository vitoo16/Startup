#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // Pure event-selection calculation. The caller must commit an occurrence and RNG state
    // atomically through an authoritative command before presenting any choice to the player.
    public static class GameEventSelector
    {
        public static GameEventDefinition? Select(IReadOnlyList<GameEventDefinition> definitions,
            GameEventContext context, ref uint eventRng)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (eventRng == 0) throw new ArgumentException("RNG state must be nonzero.", nameof(eventRng));
            if (definitions.Any(x => x == null) ||
                definitions.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != definitions.Count)
                throw new ArgumentException("Event catalog contains null or duplicate IDs.");

            // Canonical ID order makes selection stable across content authoring/reordering.
            var eligible = definitions.Where(x => IsEligible(x, context))
                .OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            if (eligible.Length == 0) return null; // no RNG consumed for an empty window

            long sum = 0;
            foreach (var definition in eligible)
                sum = checked(sum + definition.Weight);
            if (sum > int.MaxValue)
                throw new ArgumentException("Eligible event weights exceed the supported RNG bound.");

            var draw = DeterministicRng.Below(ref eventRng, (int)sum);
            foreach (var definition in eligible)
            {
                if (draw < definition.Weight) return definition;
                draw -= definition.Weight;
            }
            throw new InvalidOperationException("Event weight accounting is inconsistent.");
        }

        public static bool IsEligible(GameEventDefinition definition, GameEventContext context)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (context.Date.Year < definition.MinimumYear || context.Stage < definition.MinimumStage ||
                context.ElapsedDays < definition.WindowOffsetDays ||
                (context.ElapsedDays - definition.WindowOffsetDays) % definition.WindowPeriodDays != 0)
                return false;

            var tags = new HashSet<string>(context.Tags, StringComparer.Ordinal);
            if (definition.RequiredTags.Any(x => !tags.Contains(x)) ||
                definition.ForbiddenTags.Any(tags.Contains))
                return false;

            if (!context.Occurrences.TryGetValue(definition.Id, out var previous)) return true;
            if (previous.Count >= definition.MaxOccurrences) return false;

            var elapsed = (new DateTime(context.Date.Year, context.Date.Month, context.Date.Day) -
                new DateTime(previous.LastDate.Year, previous.LastDate.Month, previous.LastDate.Day)).Days;
            return elapsed >= definition.CooldownDays;
        }
    }
}
