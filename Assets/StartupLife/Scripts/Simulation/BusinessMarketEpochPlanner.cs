#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    public sealed class BusinessMarketEpochParticipant
    {
        public BusinessTimeWalletCandidate Time { get; }
        public BusinessDemandCandidate Demand { get; }
        public BusinessProspectivePolicy Policy { get; }
        public bool MandatoryOperatingDuePaid { get; }

        public BusinessMarketEpochParticipant(BusinessTimeWalletCandidate time,
            BusinessDemandCandidate demand, BusinessProspectivePolicy policy, bool mandatoryOperatingDuePaid)
        {
            if (time == null || demand == null || policy == null ||
                time.InstanceId != demand.BusinessInstanceId ||
                time.InstanceId != policy.InstanceId ||
                time.DefinitionId != demand.DefinitionId ||
                policy.EffectivePricing != demand.EffectivePosture)
                throw new ArgumentException("Time, demand and effective pricing must refer to one canonical business.");
            Time = time; Demand = demand; Policy = policy;
            MandatoryOperatingDuePaid = mandatoryOperatingDuePaid;
        }
    }

    public sealed class BusinessMarketUnitReservation
    {
        public BusinessUnitCostReservation Cost { get; }
        public string MarketPoolId { get; }
        public string SegmentId { get; }
        public int MarketUnitOrdinal { get; }
        public long EffectiveUnitPriceVnd { get; }
        public PricingPosture EffectivePosture { get; }
        public string ProfileRevision { get; }
        public string RulesetRevision { get; }

        internal BusinessMarketUnitReservation(BusinessUnitCostReservation cost,
            BusinessEconomicProfile profile, string ruleset, string segment, int ordinal,
            PricingPosture pricing)
        {
            Cost = cost; MarketPoolId = profile.MarketPoolId; SegmentId = segment;
            MarketUnitOrdinal = ordinal; EffectiveUnitPriceVnd = profile.UnitPriceByPosture[pricing];
            EffectivePosture = pricing; ProfileRevision = profile.ProfileRevision;
            RulesetRevision = ruleset;
        }
    }

    public sealed class FrozenBusinessMarketEpoch
    {
        public string EpochId { get; }
        public IReadOnlyList<MarketPoolDaySupply> FrozenOriginalPools { get; }
        public FrozenBusinessTimeWalletPlan OwnerTimeAndWallet { get; }
        public IReadOnlyList<BusinessMarketUnitReservation> MarketReservations { get; }
        internal FrozenBusinessMarketEpoch(string epochId, IReadOnlyList<MarketPoolDaySupply> original,
            FrozenBusinessTimeWalletPlan plan, IReadOnlyList<BusinessMarketUnitReservation> reservations)
        {
            EpochId = epochId;
            FrozenOriginalPools = Array.AsReadOnly(original.ToArray());
            OwnerTimeAndWallet = plan;
            MarketReservations = Array.AsReadOnly(reservations.ToArray());
            if (MarketReservations.Count != plan.AllUnitReservations.Count ||
                MarketReservations.Select(x => x.Cost.Id).Distinct(StringComparer.Ordinal).Count() != MarketReservations.Count ||
                MarketReservations.Select(x => x.MarketPoolId + "/" + x.SegmentId + "/" + x.MarketUnitOrdinal)
                    .Distinct(StringComparer.Ordinal).Count() != MarketReservations.Count)
                throw new ArgumentException("Epoch reservations fail one-to-one money/market conservation.");
        }
    }

    // Reconciles the SAME finite original pool snapshots with the normalized
    // time/wallet planner. Generates only FUTURE reservations, never revenue.
    public static class BusinessMarketEpochPlanner
    {
        public static FrozenBusinessMarketEpoch Freeze(
            BusinessEconomicCatalog catalog, string epochId, string dateIso, long availableWalletVnd,
            IEnumerable<MarketPoolDaySupply> originalPoolSnapshots,
            IEnumerable<MarketFulfillmentUnit> committedOldDaySales,
            IEnumerable<BusinessMarketEpochParticipant> allBusinesses,
            IEnumerable<int> globallyUnavailableMinutes)
        {
            if (catalog == null || string.IsNullOrWhiteSpace(epochId) ||
                availableWalletVnd < 0 || originalPoolSnapshots == null ||
                committedOldDaySales == null || allBusinesses == null ||
                globallyUnavailableMinutes == null)
                throw new ArgumentException("Invalid epoch inputs.");
            _ = BusinessObligationDefinition.ParseDate(dateIso);
            var pools = originalPoolSnapshots.OrderBy(x => x.PoolId, StringComparer.Ordinal).ToArray();
            if (pools.Any(x => x == null || x.DateIso != dateIso ||
                !catalog.Pools.TryGetValue(x.PoolId, out var definition) ||
                definition.Revision != x.PoolRevision) ||
                pools.Select(x => x.PoolId).Distinct(StringComparer.Ordinal).Count() != pools.Length ||
                !pools.Select(x => x.PoolId).SequenceEqual(catalog.Pools.Keys, StringComparer.Ordinal))
                throw new ArgumentException("Missing or replaced immutable day-opening market pool.");
            var sales = committedOldDaySales.ToArray();
            if (sales.Any(x => x == null || x.DateIso != dateIso))
                throw new ArgumentException("Foreign-date committed fulfillment cannot affect current supply.");
            foreach (var pool in pools)
                _ = CustomerDemandCalculator.RemainingSupply(pool,
                    sales.Where(x => x.PoolId == pool.PoolId));
            var participants = allBusinesses.OrderBy(x => x.Time.DefinitionId, StringComparer.Ordinal)
                .ThenBy(x => x.Time.InstanceId, StringComparer.Ordinal).ToArray();
            if (participants.Any(x => x == null) || participants.Length > 4 ||
                participants.Select(x => x.Time.InstanceId).Distinct(StringComparer.Ordinal).Count() != participants.Length)
                throw new ArgumentException("Duplicate or oversized portfolio.");

            var enabled = new List<BusinessMarketEpochParticipant>();
            foreach (var p in participants)
            {
                if (!catalog.Profiles.TryGetValue(p.Time.DefinitionId, out var profile) ||
                    profile.RequiredOwnerMinutes != p.Time.RequiredOwnerMinutes ||
                    profile.FullCapacityUnits != p.Time.FullCapacityUnits ||
                    profile.VariableCostPerUnitVnd != p.Time.VariableCostPerUnitVnd)
                    throw new ArgumentException("Economic content revision and owner-time inputs differ.");
                if (!p.Policy.IsOperationEligible || !p.MandatoryOperatingDuePaid) continue;
                if (sales.Count(x => x.BusinessInstanceId == p.Time.InstanceId) != p.Time.AlreadyFulfilledUnits)
                    throw new ArgumentException("Economic candidate and committed old-date units diverge.");
                enabled.Add(p);
            }
            var offers = new List<BusinessSegmentDemand>();
            foreach (var pool in pools)
            {
                var competing = enabled.Where(x =>
                    catalog.Profiles[x.Time.DefinitionId].MarketPoolId == pool.PoolId)
                    .Select(x => x.Demand);
                offers.AddRange(CustomerDemandCalculator.AllocateRemaining(catalog, pool,
                    sales.Where(x => x.PoolId == pool.PoolId), competing));
            }
            var timeCandidates = enabled.Select(x =>
                new BusinessTimeWalletCandidate(x.Time.DefinitionId, x.Time.InstanceId,
                    x.Time.OperationMode, x.Time.RequiredOwnerMinutes, x.Time.FullCapacityUnits,
                    x.Time.AlreadyWorkedMinutes, x.Time.AlreadyFulfilledUnits,
                    offers.Where(o => o.BusinessInstanceId == x.Time.InstanceId).Sum(o => o.OfferedUnits),
                    x.Time.VariableCostPerUnitVnd, x.Time.EligibleFutureMinutes,
                    x.Time.LockedFullTimeBlock)).ToArray();
            var wallet = BusinessAffordabilityPlanner.Plan(epochId, availableWalletVnd,
                timeCandidates, globallyUnavailableMinutes);
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (var line in sales)
                if (!used.Add(Key(line.PoolId, line.SegmentId, line.MarketUnitOrdinal)))
                    throw new ArgumentException("Duplicate committed market unit in source day.");
            var market = new List<BusinessMarketUnitReservation>();
            foreach (var allocation in wallet.Allocations)
            {
                var participant = enabled.Single(x => x.Time.InstanceId == allocation.InstanceId);
                var profile = catalog.Profiles[participant.Time.DefinitionId];
                var ownerOffers = offers.Where(x => x.BusinessInstanceId == allocation.InstanceId)
                    .OrderBy(x => x.SegmentId, StringComparer.Ordinal).ToArray();
                var index = 0;
                foreach (var offer in ownerOffers)
                {
                    var supply = pools.Single(x => x.PoolId == profile.MarketPoolId);
                    for (var q = 0; q < offer.OfferedUnits && index < allocation.ReservedUnits; q++)
                    {
                        var ordinal = -1;
                        for (var v = 0; v < supply.OriginalUnitsBySegment[offer.SegmentId]; v++)
                            if (!used.Contains(Key(profile.MarketPoolId, offer.SegmentId, v)))
                            {
                                ordinal = v;
                                break;
                            }
                        if (ordinal < 0) throw new InvalidOperationException("Finite market pool exhausted during reservation.");
                        used.Add(Key(profile.MarketPoolId, offer.SegmentId, ordinal));
                        market.Add(new BusinessMarketUnitReservation(allocation.UnitReservations[index],
                            profile, catalog.RulesetRevision, offer.SegmentId,
                            ordinal, participant.Policy.EffectivePricing));
                        index++;
                    }
                }
                if (index != allocation.ReservedUnits)
                    throw new InvalidOperationException("Time/wallet quota lacks segment-level prospective demand.");
            }
            return new FrozenBusinessMarketEpoch(epochId, pools, wallet, market);
        }

        private static string Key(string pool, string segment, int ordinal) =>
            pool.Length + ":" + pool + segment.Length + ":" + segment + ":" + ordinal;
    }
}
