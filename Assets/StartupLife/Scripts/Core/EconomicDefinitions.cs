#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StartupLife.Core
{
    // M9-T02 economic definitions are immutable, separately revised from legacy BusinessDefinition.
    // Stable string IDs rather than positional segment arrays are mandatory for v3 replay.
    public static class EconomicDefinitionLimits
    {
        public const int Scale = 10000;
        public const int MaxMultiplier = 40000;
        public static void Multiplier(int value, string name)
        {
            if (value < 0 || value > MaxMultiplier) throw new ArgumentOutOfRangeException(name);
        }
        public static void Id(string id, string name)
        {
            if (!ContentId.IsValid(id)) throw new ArgumentException("Invalid economic content ID.", name);
        }
        public static void Revision(string revision, string name)
        {
            if (string.IsNullOrWhiteSpace(revision)) throw new ArgumentException("Missing economic revision.", name);
        }
    }

    public sealed class CustomerSegmentDefinition
    {
        public string SegmentId { get; }
        public string Revision { get; }
        public bool Enabled { get; }
        public int PriceSensitivity { get; }
        public int Quality { get; }
        public int Convenience { get; }
        public int Trend { get; }
        public int Trust { get; }
        public int Repeat { get; }

        public CustomerSegmentDefinition(string segmentId, string revision, bool enabled,
            int priceSensitivity, int quality, int convenience, int trend, int trust, int repeat)
        {
            EconomicDefinitionLimits.Id(segmentId, nameof(segmentId));
            EconomicDefinitionLimits.Revision(revision, nameof(revision));
            EconomicDefinitionLimits.Multiplier(priceSensitivity, nameof(priceSensitivity));
            EconomicDefinitionLimits.Multiplier(quality, nameof(quality));
            EconomicDefinitionLimits.Multiplier(convenience, nameof(convenience));
            EconomicDefinitionLimits.Multiplier(trend, nameof(trend));
            EconomicDefinitionLimits.Multiplier(trust, nameof(trust));
            EconomicDefinitionLimits.Multiplier(repeat, nameof(repeat));
            SegmentId = segmentId; Revision = revision; Enabled = enabled;
            PriceSensitivity = priceSensitivity; Quality = quality; Convenience = convenience;
            Trend = trend; Trust = trust; Repeat = repeat;
        }
    }

    public sealed class MarketPoolDefinition
    {
        public string PoolId { get; }
        public string Revision { get; }
        public string CompetitionPolicyRevision { get; }
        public int BaseDailyUnits { get; }
        public IReadOnlyDictionary<string, int> SegmentShareById { get; }

        public MarketPoolDefinition(string poolId, string revision, string competitionPolicyRevision,
            int baseDailyUnits, IEnumerable<KeyValuePair<string, int>> segmentShares)
        {
            EconomicDefinitionLimits.Id(poolId, nameof(poolId));
            EconomicDefinitionLimits.Revision(revision, nameof(revision));
            EconomicDefinitionLimits.Revision(competitionPolicyRevision, nameof(competitionPolicyRevision));
            if (baseDailyUnits < 0 || baseDailyUnits > 100000) throw new ArgumentOutOfRangeException(nameof(baseDailyUnits));
            if (segmentShares == null) throw new ArgumentNullException(nameof(segmentShares));
            var shares = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in segmentShares)
            {
                EconomicDefinitionLimits.Id(pair.Key, nameof(segmentShares));
                if (pair.Value < 0 || pair.Value > EconomicDefinitionLimits.Scale ||
                    shares.ContainsKey(pair.Key)) throw new ArgumentException("Invalid or duplicate segment share.");
                shares.Add(pair.Key, pair.Value);
            }
            if (shares.Count == 0) throw new ArgumentException("Empty market pool.");
            PoolId = poolId; Revision = revision; CompetitionPolicyRevision = competitionPolicyRevision;
            BaseDailyUnits = baseDailyUnits;
            SegmentShareById = new ReadOnlyDictionary<string, int>(shares);
        }
    }

    public sealed class BusinessEconomicProfile
    {
        public string BusinessDefinitionId { get; }
        public string BusinessDefinitionRevision { get; }
        public BusinessType Type { get; }
        public string ProfileRevision { get; }
        public string MarketPoolId { get; }
        public int RequiredOwnerMinutes { get; }
        public int FullCapacityUnits { get; }
        public long VariableCostPerUnitVnd { get; }
        public long DailyMandatoryChargeVnd { get; }
        public IReadOnlyDictionary<string, int> ProductFitBySegmentId { get; }
        public IReadOnlyDictionary<PricingPosture, IReadOnlyDictionary<string, int>> PriceFitByPostureAndSegmentId { get; }
        public IReadOnlyDictionary<PricingPosture, long> UnitPriceByPosture { get; }

        public BusinessEconomicProfile(string definitionId, string definitionRevision, BusinessType type,
            string profileRevision, string poolId, int requiredOwnerMinutes, int fullCapacityUnits,
            long variableCostPerUnitVnd, long dailyMandatoryChargeVnd,
            IEnumerable<KeyValuePair<string, int>> productFit,
            IEnumerable<KeyValuePair<PricingPosture, IEnumerable<KeyValuePair<string, int>>>> priceFit,
            IEnumerable<KeyValuePair<PricingPosture, long>> unitPrices)
        {
            EconomicDefinitionLimits.Id(definitionId, nameof(definitionId));
            EconomicDefinitionLimits.Revision(definitionRevision, nameof(definitionRevision));
            EconomicDefinitionLimits.Revision(profileRevision, nameof(profileRevision));
            EconomicDefinitionLimits.Id(poolId, nameof(poolId));
            if (!Enum.IsDefined(typeof(BusinessType), type)) throw new ArgumentException("Unsupported business type.");
            if (requiredOwnerMinutes < 1 || requiredOwnerMinutes > 840 ||
                fullCapacityUnits < 0 || fullCapacityUnits > 1440)
                throw new ArgumentException("Invalid owner requirement/capacity.");
            if (variableCostPerUnitVnd < 0 || variableCostPerUnitVnd > 1000000000L ||
                dailyMandatoryChargeVnd < 0 || dailyMandatoryChargeVnd > 1000000000000L)
                throw new ArgumentException("Invalid economic costs.");
            if (productFit == null || priceFit == null || unitPrices == null)
                throw new ArgumentNullException(nameof(productFit), "All profile matrices are required.");

            var fits = ReadFactors(productFit);
            var matrix = new SortedDictionary<PricingPosture, IReadOnlyDictionary<string, int>>();
            foreach (var posture in priceFit)
            {
                if (!Enum.IsDefined(typeof(PricingPosture), posture.Key) || matrix.ContainsKey(posture.Key))
                    throw new ArgumentException("Invalid or repeated posture.");
                var factors = ReadFactors(posture.Value);
                if (!factors.Keys.SequenceEqual(fits.Keys, StringComparer.Ordinal))
                    throw new ArgumentException("PriceFit segment roster differs from ProductFit.");
                matrix.Add(posture.Key, factors);
            }
            var prices = new SortedDictionary<PricingPosture, long>();
            foreach (var price in unitPrices)
            {
                if (!Enum.IsDefined(typeof(PricingPosture), price.Key) || prices.ContainsKey(price.Key) ||
                    price.Value <= 0 || price.Value > 1000000000L)
                    throw new ArgumentException("Invalid unit price.");
                prices.Add(price.Key, price.Value);
            }
            var approved = new[] { PricingPosture.Budget, PricingPosture.Standard, PricingPosture.Premium };
            if (!matrix.Keys.SequenceEqual(approved) || !prices.Keys.SequenceEqual(approved))
                throw new ArgumentException("Exactly the three canonical pricing postures are required.");
            BusinessDefinitionId = definitionId; BusinessDefinitionRevision = definitionRevision;
            Type = type; ProfileRevision = profileRevision; MarketPoolId = poolId;
            RequiredOwnerMinutes = requiredOwnerMinutes; FullCapacityUnits = fullCapacityUnits;
            VariableCostPerUnitVnd = variableCostPerUnitVnd; DailyMandatoryChargeVnd = dailyMandatoryChargeVnd;
            ProductFitBySegmentId = fits;
            PriceFitByPostureAndSegmentId = new ReadOnlyDictionary<PricingPosture, IReadOnlyDictionary<string, int>>(matrix);
            UnitPriceByPosture = new ReadOnlyDictionary<PricingPosture, long>(prices);
        }

        private static IReadOnlyDictionary<string, int> ReadFactors(IEnumerable<KeyValuePair<string, int>> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var result = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in source)
            {
                EconomicDefinitionLimits.Id(pair.Key, nameof(source));
                EconomicDefinitionLimits.Multiplier(pair.Value, nameof(source));
                if (result.ContainsKey(pair.Key)) throw new ArgumentException("Duplicate segment factor.");
                result.Add(pair.Key, pair.Value);
            }
            if (result.Count == 0) throw new ArgumentException("Empty segment factor matrix.");
            return new ReadOnlyDictionary<string, int>(result);
        }
    }

    public sealed class BusinessEconomicCatalog
    {
        public string FixtureId { get; }
        public string CatalogRevision { get; }
        public string RulesetRevision { get; }
        public IReadOnlyDictionary<string, CustomerSegmentDefinition> Segments { get; }
        public IReadOnlyDictionary<string, MarketPoolDefinition> Pools { get; }
        public IReadOnlyDictionary<string, BusinessEconomicProfile> Profiles { get; }

        public BusinessEconomicCatalog(string fixtureId, string catalogRevision, string rulesetRevision,
            IEnumerable<CustomerSegmentDefinition> segments, IEnumerable<MarketPoolDefinition> pools,
            IEnumerable<BusinessEconomicProfile> profiles)
        {
            EconomicDefinitionLimits.Id(fixtureId, nameof(fixtureId));
            EconomicDefinitionLimits.Revision(catalogRevision, nameof(catalogRevision));
            EconomicDefinitionLimits.Revision(rulesetRevision, nameof(rulesetRevision));
            Segments = Index(segments, x => x.SegmentId);
            Pools = Index(pools, x => x.PoolId);
            Profiles = Index(profiles, x => x.BusinessDefinitionId);
            if (Segments.Count != 6 || Segments.Values.Count(x => x.Enabled) != 4 ||
                Pools.Count != 4 || Profiles.Count != 4)
                throw new ArgumentException("The M9-T02 functional fixture must define six segments, four active, four pools and four profiles.");
            foreach (var pool in Pools.Values)
            {
                if (!pool.SegmentShareById.Keys.SequenceEqual(Segments.Keys, StringComparer.Ordinal) ||
                    pool.SegmentShareById.Where(x => !Segments[x.Key].Enabled).Any(x => x.Value != 0) ||
                    pool.SegmentShareById.Where(x => Segments[x.Key].Enabled).Sum(x => x.Value) != EconomicDefinitionLimits.Scale)
                    throw new ArgumentException("Market pool segment shares must conserve 10000 permyriad.");
            }
            foreach (var profile in Profiles.Values)
            {
                if (!Pools.ContainsKey(profile.MarketPoolId) ||
                    !profile.ProductFitBySegmentId.Keys.SequenceEqual(Segments.Keys, StringComparer.Ordinal))
                    throw new ArgumentException("Unknown profile pool or segment.");
            }
            if (Profiles.Values.Select(x => x.Type).Distinct().Count() != 4)
                throw new ArgumentException("A fixture business type cannot appear more than once.");
            FixtureId = fixtureId; CatalogRevision = catalogRevision; RulesetRevision = rulesetRevision;
        }

        private static IReadOnlyDictionary<string, T> Index<T>(IEnumerable<T> values, Func<T, string> key) where T : class
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            var result = new SortedDictionary<string, T>(StringComparer.Ordinal);
            foreach (var value in values)
            {
                if (value == null) throw new ArgumentException("Null economic definition.");
                var id = key(value);
                if (result.ContainsKey(id)) throw new ArgumentException("Duplicate economic definition.");
                result.Add(id, value);
            }
            return new ReadOnlyDictionary<string, T>(result);
        }
    }
}
