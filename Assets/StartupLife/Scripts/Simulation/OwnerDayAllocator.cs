#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // Deterministic, nonsettling preview. M9-T02 owns monetary reservations and commit semantics.
    public static class OwnerDayAllocator
    {
        private sealed class Candidate
        {
            public BusinessState State = null!;
            public BusinessDefinition Definition = null!;
            public int Required;
            public BusinessPlanStatus Status = BusinessPlanStatus.NoAvailableTime;
            public int Minutes;
            public readonly List<int> Slots = new List<int>();
        }

        public static OwnerDayAllocation Plan(GameState state, ContentCatalog content)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (!string.Equals(state.ContentVersion, content.Version, StringComparison.Ordinal))
                return Unsupported(state, content,
                    Math.Min(content.Schedule.SleepMinute, Math.Max(content.Schedule.WakeMinute, state.Minute)));
            var wake = content.Schedule.WakeMinute;
            var sleep = content.Schedule.SleepMinute;
            var cutoff = Math.Min(sleep, Math.Max(wake, state.Minute));
            byte[] occupancy;
            try { occupancy = CommittedTimeTrace.Project(state, content); }
            catch (ContentCompatibilityException) { return Unsupported(state, content, cutoff); }

            var active = state.Businesses.Where(x => x.IsActive)
                .OrderBy(x => x.DefinitionId, StringComparer.Ordinal)
                .ThenBy(x => x.InstanceId, StringComparer.Ordinal).ToArray();
            var candidates = new List<Candidate>();
            foreach (var instance in active)
            {
                if (!content.Businesses.TryGetValue(instance.DefinitionId, out var definition) ||
                    definition.Revision != instance.DefinitionRevision)
                    return Unsupported(state, content, cutoff);
                var c = new Candidate { State = instance, Definition = definition,
                    Required = definition.OperatingRequirements?.RequiredOwnerMinutes ?? 0 };
                candidates.Add(c);
                if (definition.OperationMode == BusinessOperationMode.ManagerOperable)
                {
                    c.Status = BusinessPlanStatus.UnsupportedOperationMode;
                    continue;
                }
                if (definition.OperatingRequirements == null)
                {
                    c.Status = BusinessPlanStatus.UnscheduledDefinition;
                    continue;
                }
                BusinessEligibilityResult eligibility;
                try { eligibility = BusinessEligibilityEvaluator.Evaluate(content, state, definition.Id, definition.Revision); }
                catch (ContentCompatibilityException) { return Unsupported(state, content, cutoff); }
                if (!eligibility.IsEligible)
                {
                    c.Status = BusinessPlanStatus.IndividuallyIneligible;
                    continue;
                }
                foreach (var window in definition.OperatingRequirements.OperatingWindows)
                {
                    if (window.DayOfWeek != state.Date.DayOfWeek) continue;
                    var start = Math.Max(Math.Max(cutoff, window.StartMinute),
                        string.CompareOrdinal(instance.OpenedIso, state.DateIso) == 0 ? instance.OpenedMinute : wake);
                    var end = Math.Min(sleep, window.EndMinute);
                    for (var t = start; t < end; t++)
                        if (occupancy[t] == CommittedTimeTrace.Free) c.Slots.Add(t);
                }
            }

            var fullTime = candidates.Where(x => x.Definition.OperationMode == BusinessOperationMode.FullTimeRequired).ToArray();
            if (fullTime.Length > 1)
            {
                foreach (var c in candidates) c.Status = BusinessPlanStatus.MultipleFullTimeUnsupported;
                return Build(state, content, cutoff, occupancy, candidates,
                    new int[1440], OwnerDayPlanStatus.MultipleFullTimeUnsupported, false);
            }

            var owners = Enumerable.Repeat(-1, 1440).ToArray(); // -1 free, -2 unavailable/full-time, >=0 side candidate index
            for (var t = wake; t < sleep; t++)
                if (occupancy[t] != CommittedTimeTrace.Free) owners[t] = -2;

            if (fullTime.Length == 1)
            {
                var c = fullTime[0];
                if (c.Status != BusinessPlanStatus.IndividuallyIneligible && c.Status != BusinessPlanStatus.UnscheduledDefinition)
                {
                    var available = new bool[1440];
                    foreach (var slot in c.Slots) available[slot] = true;
                    var chosenStart = -1;
                    for (var t = cutoff; t + c.Required <= sleep; t++)
                    {
                        var found = true;
                        for (var m = t; m < t + c.Required; m++)
                            if (!available[m] || owners[m] != -1) { found = false; break; }
                        if (found) { chosenStart = t; break; }
                    }
                    if (chosenStart < 0) c.Status = BusinessPlanStatus.FullTimeBlockUnavailable;
                    else
                    {
                        c.Status = BusinessPlanStatus.FullyAllocated;
                        c.Minutes = c.Required;
                        for (var t = chosenStart; t < chosenStart + c.Required; t++) owners[t] = -2;
                    }
                    // Actual full-time interval is reconstructed from the selected start, not guessed from slots.
                    c.Slots.Clear();
                    if (chosenStart >= 0)
                        for (var t = chosenStart; t < chosenStart + c.Required; t++) c.Slots.Add(t);
                }
            }

            var sides = candidates.Where(x => x.Definition.OperationMode == BusinessOperationMode.SideHustleCompatible &&
                x.Status == BusinessPlanStatus.NoAvailableTime).ToArray();
            var sideOwnership = Enumerable.Repeat(-1, 1440).ToArray();
            for (var t = wake; t < sleep; t++)
                if (owners[t] != -1) sideOwnership[t] = -2;

            // Feasible progressive equal sharing. Each augmentation adds exactly one new assigned minute.
            // Recursive displacement can move existing minutes without changing another business's quota.
            bool Augment(int index, bool[] seenSlots, bool[] seenBusinesses)
            {
                if (seenBusinesses[index]) return false;
                seenBusinesses[index] = true;
                foreach (var t in sides[index].Slots)
                {
                    if (seenSlots[t] || sideOwnership[t] == -2) continue;
                    seenSlots[t] = true;
                    var previous = sideOwnership[t];
                    if (previous == -1 || (previous != index && Augment(previous, seenSlots, seenBusinesses)))
                    {
                        sideOwnership[t] = index;
                        return true;
                    }
                }
                return false;
            }

            bool progressed;
            do
            {
                progressed = false;
                for (var i = 0; i < sides.Length; i++)
                {
                    if (sides[i].Minutes >= sides[i].Required) continue;
                    if (!Augment(i, new bool[1440], new bool[sides.Length])) continue;
                    sides[i].Minutes++;
                    progressed = true;
                }
            } while (progressed);

            for (var i = 0; i < sides.Length; i++)
                sides[i].Status = sides[i].Minutes == 0 ? BusinessPlanStatus.NoAvailableTime :
                    sides[i].Minutes == sides[i].Required ? BusinessPlanStatus.FullyAllocated : BusinessPlanStatus.PartiallyAllocated;

            for (var t = wake; t < sleep; t++)
                if (sideOwnership[t] >= 0) owners[t] = sideOwnership[t];

            return Build(state, content, cutoff, occupancy, candidates, owners, OwnerDayPlanStatus.Ready, true, sides);
        }

        private static OwnerDayAllocation Unsupported(GameState state, ContentCatalog content, int cutoff)
        {
            // Unresolved content cannot establish trustworthy occupancy provenance.
            // Empty time classifications mean unavailable/unknown, not "all minutes free".
            // Never publish speculative free time or allocations from an unsupported plan.
            return new OwnerDayAllocation(state.Date, state.Revision, content.Version, cutoff,
                OwnerDayPlanStatus.UnsupportedContent,
                Array.Empty<OwnerTimeInterval>(), Array.Empty<OwnerTimeInterval>(),
                Array.Empty<OwnerTimeInterval>(), Array.Empty<OwnerTimeInterval>(),
                Array.Empty<OwnerTimeAllocation>(), Array.Empty<BusinessOperatingCapacity>());
        }

        private static OwnerDayAllocation Build(GameState state, ContentCatalog content, int cutoff, byte[] occupancy,
            IEnumerable<Candidate> candidates, int[] ownerSlots, OwnerDayPlanStatus status, bool includeAllocations,
            Candidate[]? sides = null)
        {
            var wake = content.Schedule.WakeMinute;
            var sleep = content.Schedule.SleepMinute;
            var list = candidates.ToArray();
            var allocations = new List<OwnerTimeAllocation>();
            if (includeAllocations)
            {
                foreach (var c in list.Where(x => x.Definition.OperationMode == BusinessOperationMode.FullTimeRequired &&
                    x.Status == BusinessPlanStatus.FullyAllocated))
                {
                    if (c.Slots.Count > 0)
                        allocations.Add(new OwnerTimeAllocation(c.State.InstanceId, c.Definition.Id, c.Definition.Revision,
                            new OwnerTimeInterval(c.Slots[0], c.Slots[c.Slots.Count - 1] + 1)));
                }
                if (sides != null)
                {
                    for (var i = 0; i < sides.Length; i++)
                    {
                        var start = -1;
                        for (var t = wake; t <= sleep; t++)
                        {
                            var owned = t < sleep && ownerSlots[t] == i;
                            if (owned && start < 0) start = t;
                            if (!owned && start >= 0)
                            {
                                allocations.Add(new OwnerTimeAllocation(sides[i].State.InstanceId,
                                    sides[i].Definition.Id, sides[i].Definition.Revision,
                                    new OwnerTimeInterval(start, t)));
                                start = -1;
                            }
                        }
                    }
                }
            }

            var capacities = list.Select(x => new BusinessOperatingCapacity(x.State.InstanceId,
                x.Required, includeAllocations ? x.Minutes : 0,
                includeAllocations ? x.Status : status == OwnerDayPlanStatus.MultipleFullTimeUnsupported
                    ? BusinessPlanStatus.MultipleFullTimeUnsupported : BusinessPlanStatus.UnsupportedContent)).ToArray();

            IReadOnlyList<OwnerTimeInterval> Ranges(Func<int, bool> predicate)
            {
                var result = new List<OwnerTimeInterval>();
                var start = -1;
                for (var t = wake; t <= sleep; t++)
                {
                    var matches = t < sleep && predicate(t);
                    if (matches && start < 0) start = t;
                    if (!matches && start >= 0) { result.Add(new OwnerTimeInterval(start, t)); start = -1; }
                }
                return result;
            }
            var allocated = new bool[1440];
            foreach (var allocation in allocations)
                for (var t = allocation.Interval.StartMinute; t < allocation.Interval.EndMinute; t++)
                {
                    if (allocated[t] || occupancy[t] != CommittedTimeTrace.Free)
                        throw new InvalidOperationException("Owner time is double-booked.");
                    allocated[t] = true;
                }
            return new OwnerDayAllocation(state.Date, state.Revision, content.Version, cutoff, status,
                Ranges(t => occupancy[t] == CommittedTimeTrace.Employment),
                Ranges(t => occupancy[t] == CommittedTimeTrace.Study),
                Ranges(t => occupancy[t] == CommittedTimeTrace.Elapsed),
                Ranges(t => occupancy[t] == CommittedTimeTrace.Free && !allocated[t]),
                allocations, capacities);
        }
    }
}
