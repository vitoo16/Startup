#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // Internal S4 planner inputs are already reduced to future ELIGIBLE minutes.
    // The runtime must derive these from committed receipt occupancy, business operating
    // windows, effective mode, unpaid mandatory obligations and market demand.
    public sealed class BusinessTimeWalletCandidate
    {
        public string DefinitionId { get; }
        public string InstanceId { get; }
        public BusinessOperationMode OperationMode { get; }
        public int RequiredOwnerMinutes { get; }
        public int FullCapacityUnits { get; }
        public int AlreadyWorkedMinutes { get; }
        public int AlreadyFulfilledUnits { get; }
        public int RemainingDemandUnits { get; }
        public long VariableCostPerUnitVnd { get; }
        public IReadOnlyList<int> EligibleFutureMinutes { get; }
        public IReadOnlyList<int> LockedFullTimeBlock { get; }

        public BusinessTimeWalletCandidate(string definitionId, string instanceId,
            BusinessOperationMode operationMode, int requiredOwnerMinutes, int fullCapacityUnits,
            int alreadyWorkedMinutes, int alreadyFulfilledUnits, int remainingDemandUnits,
            long variableCostPerUnitVnd, IEnumerable<int> eligibleFutureMinutes,
            IEnumerable<int>? lockedFullTimeBlock = null)
        {
            if (!ContentId.IsValid(definitionId) || string.IsNullOrWhiteSpace(instanceId) ||
                (operationMode != BusinessOperationMode.SideHustleCompatible &&
                 operationMode != BusinessOperationMode.FullTimeRequired) ||
                requiredOwnerMinutes < 1 || requiredOwnerMinutes > 840 ||
                fullCapacityUnits < 0 || fullCapacityUnits > 1440 ||
                alreadyWorkedMinutes < 0 || alreadyWorkedMinutes > 1440 ||
                alreadyFulfilledUnits < 0 || alreadyFulfilledUnits > fullCapacityUnits ||
                remainingDemandUnits < 0 || remainingDemandUnits > 100000 ||
                variableCostPerUnitVnd < 0 || variableCostPerUnitVnd > 1000000000L)
                throw new ArgumentException("Invalid economic time/wallet candidate.");
            if ((long)alreadyFulfilledUnits * requiredOwnerMinutes >
                (long)fullCapacityUnits * alreadyWorkedMinutes)
                throw new ArgumentException("Previously fulfilled units lack worked-minute capacity.");
            if (eligibleFutureMinutes == null) throw new ArgumentNullException(nameof(eligibleFutureMinutes));
            var slots = eligibleFutureMinutes.ToArray();
            if (slots.Any(t => t < 0 || t >= 1440) || slots.Distinct().Count() != slots.Length)
                throw new ArgumentException("Future business time contains duplicates or invalid minutes.");
            Array.Sort(slots);
            var locked = lockedFullTimeBlock == null ? Array.Empty<int>() : lockedFullTimeBlock.ToArray();
            if (locked.Any(t => t < 0 || t >= 1440) || locked.Distinct().Count() != locked.Length)
                throw new ArgumentException("Locked full-time time contains invalid minutes.");
            Array.Sort(locked);
            if (locked.Length > 0 && (operationMode != BusinessOperationMode.FullTimeRequired ||
                locked.Length != requiredOwnerMinutes ||
                locked[locked.Length - 1] - locked[0] + 1 != locked.Length ||
                locked.Count(slots.Contains) + alreadyWorkedMinutes != locked.Length))
                throw new ArgumentException("A locked full-time block must be contiguous and preserve committed progress.");
            DefinitionId = definitionId; InstanceId = instanceId; OperationMode = operationMode;
            RequiredOwnerMinutes = requiredOwnerMinutes; FullCapacityUnits = fullCapacityUnits;
            AlreadyWorkedMinutes = alreadyWorkedMinutes; AlreadyFulfilledUnits = alreadyFulfilledUnits;
            RemainingDemandUnits = remainingDemandUnits; VariableCostPerUnitVnd = variableCostPerUnitVnd;
            EligibleFutureMinutes = Array.AsReadOnly(slots);
            LockedFullTimeBlock = Array.AsReadOnly(locked);
        }
    }

    public sealed class BusinessUnitCostReservation
    {
        public string EpochId { get; }
        public string InstanceId { get; }
        public int AbsoluteBusinessUnitOrdinal { get; }
        public long AmountVnd { get; }
        public string Id { get; }

        internal BusinessUnitCostReservation(string epochId, BusinessTimeWalletCandidate business, int ordinal)
        {
            EpochId = epochId; InstanceId = business.InstanceId; AbsoluteBusinessUnitOrdinal = ordinal;
            AmountVnd = business.VariableCostPerUnitVnd;
            Id = LengthPrefixed(epochId) + LengthPrefixed(business.InstanceId) + ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string LengthPrefixed(string value) =>
            value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + value + ":";
    }

    public sealed class BusinessTimeWalletAllocation
    {
        public string DefinitionId { get; }
        public string InstanceId { get; }
        public int ReferenceEligibleUnits { get; }
        public int ReservedUnits { get; }
        public long ReservedVariableVnd { get; }
        public IReadOnlyList<int> ReservedOwnerMinutes { get; }
        public IReadOnlyList<BusinessUnitCostReservation> UnitReservations { get; }

        internal BusinessTimeWalletAllocation(BusinessTimeWalletCandidate candidate, int reference, int accepted,
            IReadOnlyList<int> slots, IReadOnlyList<BusinessUnitCostReservation> reservations)
        {
            DefinitionId = candidate.DefinitionId; InstanceId = candidate.InstanceId;
            ReferenceEligibleUnits = reference; ReservedUnits = accepted;
            ReservedVariableVnd = checked(candidate.VariableCostPerUnitVnd * accepted);
            ReservedOwnerMinutes = Array.AsReadOnly(slots.ToArray());
            UnitReservations = Array.AsReadOnly(reservations.ToArray());
        }
    }

    public sealed class FrozenBusinessTimeWalletPlan
    {
        public string EpochId { get; }
        public long CashAvailableAfterDueObligationsVnd { get; }
        public long TotalCostReservedVnd { get; }
        public long SpendableAfterReservationsVnd => checked(CashAvailableAfterDueObligationsVnd - TotalCostReservedVnd);
        public IReadOnlyList<BusinessTimeWalletAllocation> Allocations { get; }
        public IReadOnlyList<BusinessUnitCostReservation> AllUnitReservations { get; }

        internal FrozenBusinessTimeWalletPlan(string epochId, long cashAvailable,
            IReadOnlyList<BusinessTimeWalletAllocation> allocations)
        {
            EpochId = epochId; CashAvailableAfterDueObligationsVnd = cashAvailable;
            Allocations = Array.AsReadOnly(allocations.ToArray());
            AllUnitReservations = Array.AsReadOnly(allocations.SelectMany(x => x.UnitReservations).ToArray());
            TotalCostReservedVnd = checked(allocations.Sum(x => x.ReservedVariableVnd));
            if (TotalCostReservedVnd > cashAvailable || AllUnitReservations.Count != allocations.Sum(x => x.ReservedUnits))
                throw new ArgumentException("Variable cost reservations exceed cash or accepted quotas.");
            var allMinutes = allocations.SelectMany(x => x.ReservedOwnerMinutes).ToArray();
            if (allMinutes.Distinct().Count() != allMinutes.Length)
                throw new ArgumentException("Overlapping owner-minute reservations.");
        }

        public BusinessTimeWalletAllocation ByInstance(string instanceId) =>
            Allocations.Single(x => string.Equals(x.InstanceId, instanceId, StringComparison.Ordinal));
    }

    // Version-1 authoritative P-03 implementation. This is a PURE planner:
    // - never mutates GameState, wallet, receipts or legacy M9-T01 OwnerDayAllocator.Plan()
    // - never consumes forecast revenue; only the caller's already-spendable cash
    // - produces a frozen plan for a genuine epoch, NOT for each AdvanceBoundary cursor step.
    public static class BusinessAffordabilityPlanner
    {
        private const int MinuteCount = 1440;

        public static FrozenBusinessTimeWalletPlan Plan(
            string epochId, long spendableCashAfterDueFixedVnd,
            IEnumerable<BusinessTimeWalletCandidate> inputCandidates,
            IEnumerable<int>? globallyUnavailableFutureMinutes = null)
        {
            if (string.IsNullOrWhiteSpace(epochId) || spendableCashAfterDueFixedVnd < 0)
                throw new ArgumentException("Invalid epoch/cash input.");
            if (inputCandidates == null) throw new ArgumentNullException(nameof(inputCandidates));
            var candidates = inputCandidates.ToArray();
            if (candidates.Any(x => x == null) || candidates.Length > 4 ||
                candidates.Select(x => x.InstanceId).Distinct(StringComparer.Ordinal).Count() != candidates.Length ||
                candidates.Select(x => x.DefinitionId).Distinct(StringComparer.Ordinal).Count() != candidates.Length)
                throw new ArgumentException("Invalid/duplicate business portfolio.");
            candidates = candidates.OrderBy(x => x.DefinitionId, StringComparer.Ordinal)
                .ThenBy(x => x.InstanceId, StringComparer.Ordinal).ToArray();
            if (candidates.Count(x => x.OperationMode == BusinessOperationMode.FullTimeRequired) > 1)
                throw new ArgumentException("Multiple full-time businesses unsupported by this ruleset.");

            var unavailable = new bool[MinuteCount];
            foreach (var slot in globallyUnavailableFutureMinutes ?? Array.Empty<int>())
            {
                if (slot < 0 || slot >= MinuteCount || unavailable[slot])
                    throw new ArgumentException("Invalid/duplicate owner-time exclusion.");
                unavailable[slot] = true;
            }
            var eligibilities = candidates.Select(x =>
                x.EligibleFutureMinutes.Where(t => !unavailable[t]).ToArray()).ToArray();
            var fullTimeCandidate = Array.FindIndex(candidates,
                x => x.OperationMode == BusinessOperationMode.FullTimeRequired);
            var chosenFullTime = Array.Empty<int>();
            if (fullTimeCandidate >= 0)
            {
                var b = candidates[fullTimeCandidate];
                var future = eligibilities[fullTimeCandidate];
                int possible = b.FullCapacityUnits == 0 ? 0 : Math.Max(0,
                    (int)Math.Min(b.FullCapacityUnits - b.AlreadyFulfilledUnits,
                        ((long)(b.AlreadyWorkedMinutes + future.Length) * b.FullCapacityUnits / b.RequiredOwnerMinutes)
                        - Math.Max(b.AlreadyFulfilledUnits, (long)b.AlreadyWorkedMinutes * b.FullCapacityUnits / b.RequiredOwnerMinutes)));
                if (b.RemainingDemandUnits > 0 && possible > 0 &&
                    b.VariableCostPerUnitVnd <= spendableCashAfterDueFixedVnd)
                {
                    if (b.LockedFullTimeBlock.Count > 0)
                    {
                        var lockedFuture = b.LockedFullTimeBlock.Where(t => !unavailable[t]).ToArray();
                        // No release/re-acquire of an already committed full-time reservation.
                        if (lockedFuture.Length != b.LockedFullTimeBlock.Count - b.AlreadyWorkedMinutes ||
                            lockedFuture.Any(t => Array.BinarySearch(future, t) < 0))
                            throw new ArgumentException("Existing full-time block has been displaced.");
                        chosenFullTime = lockedFuture;
                    }
                    else if (b.AlreadyWorkedMinutes == 0)
                    {
                        for (var k = 0; k + b.RequiredOwnerMinutes <= future.Length; k++)
                        {
                            if (future[k + b.RequiredOwnerMinutes - 1] - future[k] + 1 != b.RequiredOwnerMinutes)
                                continue;
                            chosenFullTime = future.Skip(k).Take(b.RequiredOwnerMinutes).ToArray();
                            break;
                        }
                    }
                }
                foreach (var t in chosenFullTime) unavailable[t] = true;
            }

            // Side candidate references are computed after the full-time reservation
            // so every denominator reflects solo achievable capacity with this policy.
            var refs = new int[candidates.Length];
            var allowed = new int[candidates.Length][];
            for (var i = 0; i < candidates.Length; i++)
            {
                var b = candidates[i];
                allowed[i] = i == fullTimeCandidate ? chosenFullTime :
                    eligibilities[i].Where(t => !unavailable[t]).ToArray();
                if (b.FullCapacityUnits == 0 || b.RemainingDemandUnits == 0) continue;
                if (i == fullTimeCandidate && chosenFullTime.Length == 0) continue;
                var soloCapacity = (long)(b.AlreadyWorkedMinutes + allowed[i].Length)
                    * b.FullCapacityUnits / b.RequiredOwnerMinutes;
                var alreadyPassedCapacity = (long)b.AlreadyWorkedMinutes * b.FullCapacityUnits / b.RequiredOwnerMinutes;
                // Capacity already passed by committed work cannot generate retrospective sales.
                var additional = Math.Max(0L, soloCapacity - Math.Max(b.AlreadyFulfilledUnits, alreadyPassedCapacity));
                refs[i] = (int)Math.Min(b.RemainingDemandUnits,
                    Math.Min(b.FullCapacityUnits - b.AlreadyFulfilledUnits, additional));
            }

            var accepted = new int[candidates.Length];
            var retired = new bool[candidates.Length];
            var matchedOwner = Enumerable.Repeat(-1, MinuteCount).ToArray();
            for (var t = 0; t < MinuteCount; t++) if (unavailable[t]) matchedOwner[t] = -2;
            long reservedTotal = 0;
            // Progression is bounded by 4 * 1440 accepted quota attempts plus at most
            // one retired increment per candidate, never by untrusted demand magnitudes.
            while (true)
            {
                var chosen = -1;
                for (var i = 0; i < candidates.Length; i++)
                {
                    if (retired[i] || accepted[i] >= refs[i]) continue;
                    if (chosen == -1 ||
                        (long)accepted[i] * refs[chosen] < (long)accepted[chosen] * refs[i])
                        chosen = i;
                }
                if (chosen < 0) break;
                var c = candidates[chosen];
                var cost = c.VariableCostPerUnitVnd;
                if (cost > spendableCashAfterDueFixedVnd - reservedTotal)
                {
                    retired[chosen] = true;
                    continue;
                }
                if (chosen != fullTimeCandidate)
                {
                    var required = RequiredFutureOwnerMinutes(c, accepted[chosen] + 1);
                    var previousRequired = RequiredFutureOwnerMinutes(c, accepted[chosen]);
                    var next = (int[])matchedOwner.Clone();
                    var feasible = true;
                    for (var count = previousRequired; count < required; count++)
                    {
                        var visited = new bool[MinuteCount];
                        if (!TryAugment(chosen, next, allowed, visited))
                        {
                            feasible = false;
                            break;
                        }
                    }
                    if (!feasible)
                    {
                        retired[chosen] = true;
                        continue;
                    }
                    matchedOwner = next;
                }
                reservedTotal = checked(reservedTotal + cost);
                accepted[chosen]++;
            }

            // Full-time is never allowed to consume prospective owner minutes when
            // the normalized wallet progression could not fund even one unit.
            // Drop its uncommitted block and recompute side references from a NEW,
            // equivalently grounded epoch without granting any retroactive revenue.
            if (fullTimeCandidate >= 0 && chosenFullTime.Length > 0 &&
                accepted[fullTimeCandidate] == 0)
            {
                var sideOnly = Plan(epochId, spendableCashAfterDueFixedVnd,
                    candidates.Where((candidate, index) => index != fullTimeCandidate),
                    unavailable.Select((blocked, minute) => new { blocked, minute })
                        .Where(x => x.blocked && !chosenFullTime.Contains(x.minute))
                        .Select(x => x.minute));
                var adjusted = new List<BusinessTimeWalletAllocation>(sideOnly.Allocations)
                {
                    new BusinessTimeWalletAllocation(candidates[fullTimeCandidate], 0, 0,
                        Array.Empty<int>(), Array.Empty<BusinessUnitCostReservation>())
                };
                return new FrozenBusinessTimeWalletPlan(epochId, spendableCashAfterDueFixedVnd,
                    adjusted.OrderBy(x => x.DefinitionId, StringComparer.Ordinal)
                        .ThenBy(x => x.InstanceId, StringComparer.Ordinal).ToArray());
            }

            var allocations = new List<BusinessTimeWalletAllocation>();
            for (var i = 0; i < candidates.Length; i++)
            {
                var b = candidates[i];
                var slots = i == fullTimeCandidate ? chosenFullTime :
                    Enumerable.Range(0, MinuteCount).Where(t => matchedOwner[t] == i).ToArray();
                // A full-time block is released if no business unit could be reserved.
                if (i == fullTimeCandidate && accepted[i] == 0) slots = Array.Empty<int>();
                if (i != fullTimeCandidate && slots.Length != RequiredFutureOwnerMinutes(b, accepted[i]))
                    throw new InvalidOperationException("Matching did not conserve future-minute quotas.");
                var reservations = Enumerable.Range(1, accepted[i])
                    .Select(u => new BusinessUnitCostReservation(epochId, b, b.AlreadyFulfilledUnits + u)).ToArray();
                allocations.Add(new BusinessTimeWalletAllocation(b, refs[i], accepted[i], slots, reservations));
            }
            return new FrozenBusinessTimeWalletPlan(epochId, spendableCashAfterDueFixedVnd, allocations);
        }

        private static int RequiredFutureOwnerMinutes(BusinessTimeWalletCandidate c, int additionalUnits)
        {
            if (additionalUnits == 0) return 0;
            if (c.FullCapacityUnits == 0) throw new ArgumentException("Zero-capacity business cannot reserve units.");
            var passedCapacity = (long)c.AlreadyWorkedMinutes * c.FullCapacityUnits / c.RequiredOwnerMinutes;
            var futureTarget = Math.Max((long)c.AlreadyFulfilledUnits, passedCapacity) + additionalUnits;
            var totalNeeded = (futureTarget * c.RequiredOwnerMinutes + c.FullCapacityUnits - 1) /
                c.FullCapacityUnits;
            return checked((int)Math.Max(0, totalNeeded - c.AlreadyWorkedMinutes));
        }

        private static bool TryAugment(int candidate, int[] owner, int[][] allowed, bool[] visited)
        {
            foreach (var minute in allowed[candidate])
            {
                if (visited[minute]) continue;
                visited[minute] = true;
                var incumbent = owner[minute];
                if (incumbent == -1 || (incumbent >= 0 && incumbent != candidate &&
                    TryAugment(incumbent, owner, allowed, visited)))
                {
                    owner[minute] = candidate;
                    return true;
                }
            }
            return false;
        }
    }
}
