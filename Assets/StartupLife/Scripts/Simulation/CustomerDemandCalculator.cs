#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Numerics;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    public sealed class MarketPoolDaySupply
    {
        public string DateIso { get; }
        public string PoolId { get; }
        public string PoolRevision { get; }
        public IReadOnlyDictionary<string, int> OriginalUnitsBySegment { get; }

        public MarketPoolDaySupply(string dateIso, string poolId, string revision,
            IEnumerable<KeyValuePair<string, int>> supplies)
        {
            if (!DateTime.TryParseExact(dateIso, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out _) || string.IsNullOrWhiteSpace(poolId) ||
                string.IsNullOrWhiteSpace(revision) || supplies == null)
                throw new ArgumentException("Invalid immutable market day supply.");
            var map = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var s in supplies)
            {
                if (!ContentId.IsValid(s.Key) || s.Value < 0 || map.ContainsKey(s.Key))
                    throw new ArgumentException("Invalid market supply segment.");
                map.Add(s.Key, s.Value);
            }
            DateIso = dateIso; PoolId = poolId; PoolRevision = revision;
            OriginalUnitsBySegment = new ReadOnlyDictionary<string, int>(map);
        }
    }

    // Each committed market unit gets exactly one immutable line. No segment-blind sale is valid.
    public sealed class MarketFulfillmentUnit
    {
        public string Id { get; }
        public string DateIso { get; }
        public string PoolId { get; }
        public string SegmentId { get; }
        public int MarketUnitOrdinal { get; }
        public string BusinessInstanceId { get; }
        public string SliceId { get; }
        public string SourceOperationId { get; }

        public MarketFulfillmentUnit(string id, string dateIso, string poolId, string segmentId, int marketUnitOrdinal,
            string businessInstanceId, string sliceId, string sourceOperationId)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(dateIso) ||
                !ContentId.IsValid(poolId) || !ContentId.IsValid(segmentId) ||
                marketUnitOrdinal < 0 || string.IsNullOrWhiteSpace(businessInstanceId) ||
                string.IsNullOrWhiteSpace(sliceId) || string.IsNullOrWhiteSpace(sourceOperationId))
                throw new ArgumentException("Invalid fulfillment provenance.");
            Id = id; DateIso = dateIso; PoolId = poolId; SegmentId = segmentId;
            MarketUnitOrdinal = marketUnitOrdinal; BusinessInstanceId = businessInstanceId;
            SliceId = sliceId; SourceOperationId = sourceOperationId;
        }
    }

    public sealed class BusinessDemandCandidate
    {
        public string BusinessInstanceId { get; }
        public string DefinitionId { get; }
        public PricingPosture EffectivePosture { get; }
        public int Reputation { get; }
        public int Reach { get; }
        public int Seasonality { get; }
        public int Event { get; }

        public BusinessDemandCandidate(string instanceId, string definitionId, PricingPosture posture,
            int reputation = 10000, int reach = 10000, int seasonality = 10000, int eventFactor = 10000)
        {
            if (string.IsNullOrWhiteSpace(instanceId) || !ContentId.IsValid(definitionId) ||
                !Enum.IsDefined(typeof(PricingPosture), posture))
                throw new ArgumentException("Invalid business demand candidate.");
            EconomicDefinitionLimits.Multiplier(reputation, nameof(reputation));
            EconomicDefinitionLimits.Multiplier(reach, nameof(reach));
            EconomicDefinitionLimits.Multiplier(seasonality, nameof(seasonality));
            EconomicDefinitionLimits.Multiplier(eventFactor, nameof(eventFactor));
            BusinessInstanceId = instanceId; DefinitionId = definitionId; EffectivePosture = posture;
            Reputation = reputation; Reach = reach; Seasonality = seasonality; Event = eventFactor;
        }
    }

    public sealed class BusinessSegmentDemand
    {
        public string BusinessInstanceId { get; }
        public string DefinitionId { get; }
        public string SegmentId { get; }
        public int OfferedUnits { get; }
        public BusinessSegmentDemand(string instanceId, string definitionId, string segmentId, int units)
        {
            BusinessInstanceId = instanceId; DefinitionId = definitionId; SegmentId = segmentId; OfferedUnits = units;
        }
    }

    // Pure, deterministic, integer-only pool mathematics. The application must persist
    // supply snapshots and fulfillment lines; this calculator never commits operations.
    public static class CustomerDemandCalculator
    {
        private const int Scale = 10000;
        private static readonly BigInteger WeightDenominator = BigInteger.Pow(new BigInteger(Scale), 6);

        public static MarketPoolDaySupply OpeningSupply(string dateIso, BusinessEconomicCatalog catalog, string poolId)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (!catalog.Pools.TryGetValue(poolId, out var pool))
                throw new ContentCompatibilityException("save.content_id", "Market pool definition is unavailable.");
            var ordered = pool.SegmentShareById.OrderBy(x => x.Key, StringComparer.Ordinal).ToArray();
            var floor = new Dictionary<string, int>(StringComparer.Ordinal);
            var fractions = new List<Tuple<string, BigInteger>>();
            var allocated = 0;
            foreach (var segment in ordered)
            {
                var numerator = new BigInteger(pool.BaseDailyUnits) * segment.Value;
                var whole = checked((int)(numerator / Scale));
                floor.Add(segment.Key, whole);
                allocated = checked(allocated + whole);
                fractions.Add(Tuple.Create(segment.Key, numerator % Scale));
            }
            var leftovers = checked(pool.BaseDailyUnits - allocated);
            foreach (var item in fractions.OrderByDescending(x => x.Item2).ThenBy(x => x.Item1, StringComparer.Ordinal).Take(leftovers))
                floor[item.Item1] = checked(floor[item.Item1] + 1);
            return new MarketPoolDaySupply(dateIso, pool.PoolId, pool.Revision, floor);
        }

        public static IReadOnlyDictionary<string, int> RemainingSupply(MarketPoolDaySupply supply,
            IEnumerable<MarketFulfillmentUnit> alreadyCommitted)
        {
            if (supply == null || alreadyCommitted == null) throw new ArgumentNullException(nameof(supply));
            var remaining = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var item in supply.OriginalUnitsBySegment) remaining.Add(item.Key, item.Value);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var marketKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var unit in alreadyCommitted)
            {
                if (unit == null || !ids.Add(unit.Id) || unit.DateIso != supply.DateIso ||
                    unit.PoolId != supply.PoolId || !remaining.ContainsKey(unit.SegmentId))
                    throw new ArgumentException("Orphan or duplicate committed fulfillment.");
                var capacity = supply.OriginalUnitsBySegment[unit.SegmentId];
                var marketKey = unit.SegmentId + "/" + unit.MarketUnitOrdinal.ToString(CultureInfo.InvariantCulture);
                if (unit.MarketUnitOrdinal >= capacity || !marketKeys.Add(marketKey))
                    throw new ArgumentException("Market unit sold twice or outside frozen supply.");
                remaining[unit.SegmentId] = checked(remaining[unit.SegmentId] - 1);
                if (remaining[unit.SegmentId] < 0) throw new ArgumentException("Market pool oversold.");
            }
            return new ReadOnlyDictionary<string, int>(remaining);
        }

        public static IReadOnlyList<BusinessSegmentDemand> AllocateRemaining(
            BusinessEconomicCatalog catalog, MarketPoolDaySupply originalSupply,
            IEnumerable<MarketFulfillmentUnit> alreadyCommitted,
            IEnumerable<BusinessDemandCandidate> candidates)
        {
            if (catalog == null || originalSupply == null || candidates == null)
                throw new ArgumentNullException(nameof(catalog));
            if (!catalog.Pools.TryGetValue(originalSupply.PoolId, out var pool) ||
                pool.Revision != originalSupply.PoolRevision ||
                !originalSupply.OriginalUnitsBySegment.Keys.SequenceEqual(pool.SegmentShareById.Keys, StringComparer.Ordinal))
                throw new ContentCompatibilityException("save.content_revision", "Unavailable immutable pool revision.");
            var remaining = RemainingSupply(originalSupply, alreadyCommitted);
            var sourceCandidates = candidates.ToArray();
            if (sourceCandidates.Any(x => x == null)) throw new ArgumentException("Null business demand candidate.");
            var participating = sourceCandidates.OrderBy(x => x.DefinitionId, StringComparer.Ordinal)
                .ThenBy(x => x.BusinessInstanceId, StringComparer.Ordinal).ToArray();
            if (participating.Any(x => x == null) ||
                participating.Select(x => x.BusinessInstanceId).Distinct(StringComparer.Ordinal).Count() != participating.Length)
                throw new ArgumentException("Invalid/duplicate business demand candidates.");
            foreach (var candidate in participating)
                if (!catalog.Profiles.TryGetValue(candidate.DefinitionId, out var p) || p.MarketPoolId != pool.PoolId)
                    throw new ContentCompatibilityException("save.content_id", "Missing exact economic business/pool profile.");
            var answer = new List<BusinessSegmentDemand>();
            foreach (var segment in remaining.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                if (segment.Value == 0 || !catalog.Segments[segment.Key].Enabled) continue;
                var entries = participating.Select(x => new DemandWeight(x, Weight(catalog.Profiles[x.DefinitionId], x, segment.Key))).ToArray();
                var raw = entries.Select(x => (int)(new BigInteger(segment.Value) * x.Weight / WeightDenominator)).ToArray();
                var rawTotal = raw.Aggregate(0, (a,b) => checked(a + b));
                int[] units;
                if (rawTotal <= segment.Value) units = raw;
                else
                {
                    var totalWeight = entries.Aggregate(BigInteger.Zero, (sum, x) => sum + x.Weight);
                    if (totalWeight <= 0) throw new InvalidOperationException("Positive raw demand requires positive weight.");
                    units = new int[entries.Length];
                    var remainder = new List<Tuple<int, BigInteger>>();
                    var used = 0;
                    for (var i = 0; i < entries.Length; i++)
                    {
                        var numerator = new BigInteger(segment.Value) * entries[i].Weight;
                        units[i] = checked((int)(numerator / totalWeight));
                        used = checked(used + units[i]);
                        remainder.Add(Tuple.Create(i, numerator % totalWeight));
                    }
                    foreach (var fraction in remainder.OrderByDescending(x => x.Item2)
                        .ThenBy(x => entries[x.Item1].Candidate.DefinitionId, StringComparer.Ordinal)
                        .ThenBy(x => entries[x.Item1].Candidate.BusinessInstanceId, StringComparer.Ordinal)
                        .Take(segment.Value - used))
                        units[fraction.Item1] = checked(units[fraction.Item1] + 1);
                }
                for (var i = 0; i < entries.Length; i++)
                    if (units[i] > 0)
                        answer.Add(new BusinessSegmentDemand(entries[i].Candidate.BusinessInstanceId,
                            entries[i].Candidate.DefinitionId, segment.Key, units[i]));
            }
            return answer.AsReadOnly();
        }

        private static BigInteger Weight(BusinessEconomicProfile profile, BusinessDemandCandidate candidate, string segmentId)
        {
            var fit = profile.ProductFitBySegmentId[segmentId];
            var price = profile.PriceFitByPostureAndSegmentId[candidate.EffectivePosture][segmentId];
            return new BigInteger(fit) * price * candidate.Reputation * candidate.Reach *
                candidate.Seasonality * candidate.Event;
        }

        private sealed class DemandWeight
        {
            public BusinessDemandCandidate Candidate { get; }
            public BigInteger Weight { get; }
            public DemandWeight(BusinessDemandCandidate candidate, BigInteger weight)
            { Candidate = candidate; Weight = weight; }
        }
    }
}
