#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // Mutates ONLY an isolated v3 candidate. GameSession must commit every returned
    // mutation with its receipt in ONE CAS transaction, never call this on live state.
    // The persisted plan, not a fresh planner call, drives AdvanceBoundary progress.
    public static class V3EpochTransactions
    {
        public static void Freeze(GameState candidate, EconomicV3Records records,
            FrozenBusinessMarketEpoch planned, string operationId, int startMinute)
        {
            Require(candidate, records);
            if (planned == null || string.IsNullOrWhiteSpace(operationId) ||
                startMinute != candidate.Minute || startMinute < 0 || startMinute >= 1440 ||
                records.CommittedEpochIds.Contains(planned.EpochId))
                throw new ArgumentException("Invalid or duplicated epoch source.");
            var graph = records.Provenance;
            ReleaseFuture(graph);
            var originalPools = planned.FrozenOriginalPools;
            foreach (var original in originalPools)
            {
                if (original.DateIso != candidate.DateIso)
                    throw new ArgumentException("Cannot reserve a market pool for another day.");
                var previous = graph.MarketPools.SingleOrDefault(x =>
                    x.DateIso == original.DateIso && x.PoolId == original.PoolId);
                var ordered = original.OriginalUnitsBySegment
                    .OrderBy(x => x.Key, StringComparer.Ordinal).ToArray();
                if (previous == null)
                    graph.MarketPools.Add(new V3MarketDayPool
                    {
                        DateIso = original.DateIso, PoolId = original.PoolId,
                        DefinitionRevision = original.PoolRevision,
                        Segments = ordered.Select(x => new V3PoolSegmentSupply
                        {
                            SegmentId = x.Key, OriginalUnits = x.Value
                        }).ToList()
                    });
                else if (previous.DefinitionRevision != original.PoolRevision ||
                    previous.Segments.Count != ordered.Length ||
                    ordered.Any(x => previous.Segments.SingleOrDefault(y =>
                        y.SegmentId == x.Key)?.OriginalUnits != x.Value))
                    throw new ArgumentException("Original day-opening demand changed during replanning.");
            }
            var epoch = new V3BusinessEpochPlan
            {
                Id = planned.EpochId,
                DateIso = candidate.DateIso,
                StartMinute = startMinute,
                ProcessedUntilMinute = startMinute,
                SourceOperationId = operationId,
                RulesetRevision = planned.MarketReservations.Count > 0 ?
                    planned.MarketReservations[0].RulesetRevision : records.RulesetRevision
            };
            if (string.IsNullOrWhiteSpace(epoch.RulesetRevision))
                throw new ArgumentException("Unversioned epoch cannot become authoritative.");
            foreach (var allocation in planned.OwnerTimeAndWallet.Allocations)
            {
                if (allocation.ReservedOwnerMinutes.Any(x => x < startMinute))
                    throw new ArgumentException("Retroactive business time reservation.");
                epoch.Assignments.Add(new V3BusinessEpochAssignment
                {
                    BusinessInstanceId = allocation.InstanceId,
                    RequiredOwnerMinutes = allocation.RequiredOwnerMinutes,
                    FullCapacityUnits = allocation.FullCapacityUnits,
                    AlreadyWorkedMinutes = allocation.AlreadyWorkedMinutes,
                    AlreadyFulfilledUnits = allocation.AlreadyFulfilledUnits,
                    FrozenFutureOwnerMinutes = allocation.ReservedOwnerMinutes.ToList(),
                    OrderedCostReservationIds = allocation.UnitReservations.Select(x => x.Id).ToList()
                });
                foreach (var cost in allocation.UnitReservations)
                {
                    if (graph.CostReservations.Any(x => x.Id == cost.Id))
                        throw new ArgumentException("A prior cost reservation was re-issued.");
                    graph.CostReservations.Add(new V3CostReservation
                    {
                        Id = cost.Id, DateIso = candidate.DateIso,
                        EpochId = epoch.Id, BusinessInstanceId = allocation.InstanceId,
                        BusinessUnitOrdinal = cost.AbsoluteBusinessUnitOrdinal,
                        AmountVnd = cost.AmountVnd,
                        Status = V3CostReservationStatus.Active,
                        SourceOperationId = operationId
                    });
                    records.CommittedCostReservationIds.Add(cost.Id);
                }
            }
            foreach (var market in planned.MarketReservations)
                epoch.PlannedUnits.Add(new V3PlannedMarketUnit
                {
                    CostReservationId = market.Cost.Id,
                    BusinessInstanceId = market.Cost.InstanceId,
                    BusinessUnitOrdinal = market.Cost.AbsoluteBusinessUnitOrdinal,
                    PoolId = market.MarketPoolId, SegmentId = market.SegmentId,
                    MarketUnitOrdinal = market.MarketUnitOrdinal,
                    EffectiveUnitPriceVnd = market.EffectiveUnitPriceVnd,
                    EffectivePosture = market.EffectivePosture,
                    ProfileRevision = market.ProfileRevision,
                    RulesetRevision = market.RulesetRevision
                });
            var remainingCost = graph.CostReservations.Where(x =>
                x.Status == V3CostReservationStatus.Active).Sum(x => x.AmountVnd);
            if (remainingCost > candidate.Cash || remainingCost < 0)
                throw new ArgumentException("Future variable costs not backed by existing cash.");
            records.CommittedEpochIds.Add(epoch.Id);
            graph.ActiveEpoch = epoch;
            records.Validate();
        }

        public static void ReleaseFuture(V3FinancialProvenance graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (graph.ActiveEpoch == null) return;
            var active = graph.ActiveEpoch;
            foreach (var unit in active.PlannedUnits)
            {
                var cost = graph.CostReservations.Single(x => x.Id == unit.CostReservationId);
                if (cost.Status == V3CostReservationStatus.Active)
                    cost.Status = V3CostReservationStatus.Released;
            }
            graph.ActiveEpoch = null;
            // Actual cash has not changed: releasing a reservation is never a refund.
        }

        public static void ConsumeInterval(GameState candidate, EconomicV3Records records,
            int startMinute, int endMinute, string boundaryOperationId)
        {
            Require(candidate, records);
            if (startMinute < 0 || endMinute <= startMinute || endMinute > 1440 ||
                string.IsNullOrWhiteSpace(boundaryOperationId))
                throw new ArgumentException("Invalid committed operating interval.");
            var graph = records.Provenance;
            var epoch = graph.ActiveEpoch;
            if (epoch == null) return; // future economy may be inactive before migration activation.
            if (epoch.DateIso != candidate.DateIso ||
                epoch.ProcessedUntilMinute != startMinute)
                throw new ArgumentException("Frozen epoch cursor does not match committed receipt.");
            foreach (var a in epoch.Assignments.OrderBy(x => x.BusinessInstanceId, StringComparer.Ordinal))
            {
                var newMinutes = a.FrozenFutureOwnerMinutes.Count(x =>
                    x >= startMinute && x < endMinute);
                var worked = checked(a.WorkedInEpoch + newMinutes);
                var passed = (long)a.AlreadyWorkedMinutes * a.FullCapacityUnits /
                    a.RequiredOwnerMinutes;
                var now = ((long)a.AlreadyWorkedMinutes + worked) *
                    a.FullCapacityUnits / a.RequiredOwnerMinutes;
                var available = Math.Max(0L,
                    now - Math.Max(passed, a.AlreadyFulfilledUnits));
                var credited = checked((int)Math.Min(a.OrderedCostReservationIds.Count, available));
                if (credited < a.FulfilledInEpoch)
                    throw new ArgumentException("Frozen epoch attempted to rewind committed units.");
                var newly = a.OrderedCostReservationIds
                    .Skip(a.FulfilledInEpoch).Take(credited - a.FulfilledInEpoch).ToArray();
                if (newMinutes == 0 && newly.Length == 0) continue;
                var sliceId = candidate.RunId + "/business-slice/" + epoch.Id.Length + ":" + epoch.Id +
                    "/" + a.BusinessInstanceId.Length + ":" + a.BusinessInstanceId +
                    "/" + startMinute.ToString(CultureInfo.InvariantCulture) +
                    "/" + endMinute.ToString(CultureInfo.InvariantCulture);
                if (graph.OperationSlices.Any(x => x.Id == sliceId))
                    throw new ArgumentException("Duplicate committed business slice.");
                var slice = new V3BusinessOperationSlice
                {
                    Id = sliceId, EpochId = epoch.Id, DateIso = epoch.DateIso,
                    BusinessInstanceId = a.BusinessInstanceId,
                    StartMinute = startMinute, EndMinute = endMinute,
                    CommittedOwnerMinuteDelta = newMinutes,
                    SourceBoundaryOperationId = boundaryOperationId
                };
                long paid = 0;
                foreach (var costId in newly)
                {
                    var cost = graph.CostReservations.Single(x => x.Id == costId);
                    var market = epoch.PlannedUnits.Single(x => x.CostReservationId == costId);
                    if (cost.Status != V3CostReservationStatus.Active)
                        throw new ArgumentException("Cost reservation already consumed or released.");
                    cost.Status = V3CostReservationStatus.Consumed;
                    paid = checked(paid + cost.AmountVnd);
                    var fulfillmentId = cost.Id + "/sale";
                    if (graph.Fulfillments.Any(x => x.Id == fulfillmentId))
                        throw new ArgumentException("Unit was previously sold.");
                    slice.FulfillmentIds.Add(fulfillmentId);
                    graph.Fulfillments.Add(new V3UnitFulfillment
                    {
                        Id = fulfillmentId, DateIso = epoch.DateIso,
                        SliceId = sliceId, EpochId = epoch.Id,
                        BusinessInstanceId = a.BusinessInstanceId,
                        PoolId = market.PoolId, SegmentId = market.SegmentId,
                        MarketUnitOrdinal = market.MarketUnitOrdinal,
                        BusinessUnitOrdinal = market.BusinessUnitOrdinal,
                        SourceCostReservationId = costId,
                        UnitPriceVnd = market.EffectiveUnitPriceVnd,
                        VariablePaidVnd = cost.AmountVnd,
                        ProfileRevision = market.ProfileRevision,
                        RulesetRevision = market.RulesetRevision,
                        SourceBoundaryOperationId = boundaryOperationId
                    });
                    records.CommittedFulfillmentIds.Add(fulfillmentId);
                }
                if (paid > candidate.Cash)
                    throw new ArgumentException("The authoritative wallet cannot fund realized operations.");
                candidate.Cash = checked(candidate.Cash - paid);
                slice.UnitsDelta = newly.Length;
                slice.VariablePaidVnd = paid;
                graph.OperationSlices.Add(slice);
                records.CommittedSliceIds.Add(slice.Id);
                records.V3PaidVariableVnd = checked(records.V3PaidVariableVnd + paid);
                if (paid > 0)
                    candidate.Ledger.Add(new LedgerEntry
                    {
                        Id = candidate.NewEntity("transaction"),
                        OperationId = boundaryOperationId,
                        Category = "business.variable.payment",
                        CashDelta = -paid, Amount = paid, AttributionId = slice.Id
                    });
                a.WorkedInEpoch = worked; a.FulfilledInEpoch = credited;
            }
            epoch.ProcessedUntilMinute = endMinute;
            var remaining = graph.CostReservations.Where(x =>
                x.Status == V3CostReservationStatus.Active).Sum(x => x.AmountVnd);
            if (remaining > candidate.Cash)
                throw new ArgumentException("Consumed unit cost depleted another reserved unit.");
            records.Validate();
        }

        private static void Require(GameState candidate, EconomicV3Records records)
        {
            if (candidate == null || records == null || candidate.SaveVersion != 3)
                throw new ArgumentException("Economic operation requires an isolated schema-3 candidate.");
        }
    }
}
