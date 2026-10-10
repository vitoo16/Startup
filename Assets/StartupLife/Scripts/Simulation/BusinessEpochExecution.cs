#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StartupLife.Simulation
{
    public sealed class BusinessEpochBusinessProgress
    {
        public string InstanceId { get; }
        public int OwnerMinutesWorkedInEpoch { get; }
        public int UnitsEarnedInEpoch { get; }

        internal BusinessEpochBusinessProgress(string instanceId, int minutes, int earned)
        {
            InstanceId = instanceId; OwnerMinutesWorkedInEpoch = minutes; UnitsEarnedInEpoch = earned;
        }
    }

    // A single frozen epoch progresses monotonically across arbitrary AdvanceBoundary splits.
    // A Study command never creates business work, and invalidates subsequent use of this epoch.
    public sealed class FrozenBusinessEpochCursor
    {
        public string EpochId { get; }
        public int ProcessedUntilMinute { get; }
        public bool RequiresGenuineReplan { get; }
        public IReadOnlyDictionary<string, BusinessEpochBusinessProgress> ByBusiness { get; }

        private FrozenBusinessEpochCursor(string id, int minute, bool requiresReplan,
            IEnumerable<BusinessEpochBusinessProgress> items)
        {
            EpochId = id; ProcessedUntilMinute = minute; RequiresGenuineReplan = requiresReplan;
            var map = new SortedDictionary<string, BusinessEpochBusinessProgress>(StringComparer.Ordinal);
            foreach (var item in items) map.Add(item.InstanceId, item);
            ByBusiness = new ReadOnlyDictionary<string, BusinessEpochBusinessProgress>(map);
        }

        public static FrozenBusinessEpochCursor Begin(FrozenBusinessTimeWalletPlan plan, int startMinute)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (startMinute < 0 || startMinute >= 1440) throw new ArgumentOutOfRangeException(nameof(startMinute));
            if (plan.Allocations.Any(x => x.ReservedOwnerMinutes.Any(t => t < startMinute)))
                throw new ArgumentException("Cannot freeze an epoch with retroactively reserved owner minutes.");
            return new FrozenBusinessEpochCursor(plan.EpochId, startMinute, false,
                plan.Allocations.Select(x => new BusinessEpochBusinessProgress(x.InstanceId, 0, 0)));
        }

        public BusinessEpochAdvanceResult AdvanceTo(FrozenBusinessTimeWalletPlan plan, int endMinute,
            bool isStudyCommand = false)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (plan.EpochId != EpochId || RequiresGenuineReplan ||
                endMinute < ProcessedUntilMinute || endMinute > 1440 ||
                plan.Allocations.Count != ByBusiness.Count ||
                plan.Allocations.Any(x => !ByBusiness.ContainsKey(x.InstanceId)))
                throw new ArgumentException("Stale or rewound frozen financial epoch.");
            var newlyConsumed = new List<BusinessUnitCostReservation>();
            var newlyWorked = new SortedDictionary<string, int>(StringComparer.Ordinal);
            var next = new List<BusinessEpochBusinessProgress>();
            foreach (var allocation in plan.Allocations)
            {
                var old = ByBusiness[allocation.InstanceId];
                var deltaWorked = isStudyCommand ? 0 :
                    allocation.ReservedOwnerMinutes.Count(t =>
                        t >= ProcessedUntilMinute && t < endMinute);
                var cumulative = checked(old.OwnerMinutesWorkedInEpoch + deltaWorked);
                if (allocation.FullCapacityUnits < 0 || allocation.RequiredOwnerMinutes <= 0)
                    throw new ArgumentException("Invalid archived economic capacity.");
                var beforeEpochCapacity =
                    (long)allocation.AlreadyWorkedMinutes * allocation.FullCapacityUnits /
                    allocation.RequiredOwnerMinutes;
                var totalCapacity =
                    ((long)allocation.AlreadyWorkedMinutes + cumulative) *
                    allocation.FullCapacityUnits / allocation.RequiredOwnerMinutes;
                // Prior unreserved capacity is never a source of retroactive sales.
                var newPotentialUnits = Math.Max(0L,
                    totalCapacity - Math.Max(beforeEpochCapacity, allocation.AlreadyFulfilledUnits));
                var nowEarned = checked((int)Math.Min(allocation.ReservedUnits, newPotentialUnits));
                if (nowEarned < old.UnitsEarnedInEpoch)
                    throw new InvalidOperationException("Committed revenue cannot decrease.");
                var deltaUnits = nowEarned - old.UnitsEarnedInEpoch;
                if (deltaUnits > 0)
                    newlyConsumed.AddRange(allocation.UnitReservations.Skip(old.UnitsEarnedInEpoch).Take(deltaUnits));
                next.Add(new BusinessEpochBusinessProgress(allocation.InstanceId, cumulative, nowEarned));
                newlyWorked.Add(allocation.InstanceId, deltaWorked);
            }
            // Candidate state is a detached projection. Only the existing GameSession
            // transaction may consume reservations, debit variable cost and record sales.
            var nextCursor = new FrozenBusinessEpochCursor(EpochId, endMinute, isStudyCommand, next);
            return new BusinessEpochAdvanceResult(nextCursor, newlyConsumed, newlyWorked);
        }
    }

    public sealed class BusinessEpochAdvanceResult
    {
        public FrozenBusinessEpochCursor Next { get; }
        public IReadOnlyList<BusinessUnitCostReservation> NewlyConsumedReservations { get; }
        public IReadOnlyDictionary<string, int> NewlyWorkedMinutesByBusiness { get; }
        public long VariablePaymentVnd { get; }

        internal BusinessEpochAdvanceResult(FrozenBusinessEpochCursor next,
            IEnumerable<BusinessUnitCostReservation> consumed,
            IReadOnlyDictionary<string, int> minutesByBusiness)
        {
            Next = next;
            NewlyConsumedReservations = Array.AsReadOnly(consumed.ToArray());
            var worked = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in minutesByBusiness) worked.Add(pair.Key, pair.Value);
            NewlyWorkedMinutesByBusiness = new ReadOnlyDictionary<string, int>(worked);
            VariablePaymentVnd = checked(NewlyConsumedReservations.Sum(x => x.AmountVnd));
            if (NewlyConsumedReservations.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() !=
                NewlyConsumedReservations.Count)
                throw new ArgumentException("A variable cost reservation was consumed twice.");
        }
    }
}
