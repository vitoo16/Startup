#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Content
{
    // Provisional deterministic QA fixture. Does not alter or backfill the historical v2 asset.
    // The four business economic profiles are separate from old launch/investment definitions.
    public static class M9T02FunctionalEconomy
    {
        private static readonly string[] Segments =
        {
            "budget-sensitive", "convenience-focused", "quality-focused", "trend-aware",
            "loyal-relationship", "business-b2b"
        };

        public static BusinessEconomicCatalog Create()
        {
            var segmentDefinitions = Segments.Select((id, index) =>
                new CustomerSegmentDefinition(id, "v1", index < 4, 10000, 10000, 10000, 10000, 10000, 10000)).ToArray();
            var pools = new[]
            {
                Pool("mvp.market.online-retail", 18, 3500, 1500, 2500, 2500),
                Pool("mvp.market.home-food", 16, 2500, 3500, 2500, 1500),
                Pool("mvp.market.freelance", 6, 1500, 1500, 5500, 1500),
                Pool("mvp.market.coffee", 80, 2500, 3500, 2000, 2000)
            };
            var profiles = new[]
            {
                Profile("online-store", BusinessType.OnlineStore, "mvp.market.online-retail", 120, 8,
                    new long[]{75000,95000,125000}, 52000, 8000, new[]{11500,9000,10000,11000}),
                Profile("home-food-preorder", BusinessType.HomeFoodPreorder, "mvp.market.home-food", 120, 8,
                    new long[]{35000,45000,60000}, 23000, 12000, new[]{9500,12000,10500,9500}),
                Profile("freelance-service", BusinessType.FreelanceService, "mvp.market.freelance", 120, 3,
                    new long[]{160000,220000,300000}, 20000, 5000, new[]{8000,8500,12500,8500}),
                // Solo capacity 20 plus a contracted helper's 20 units = unchanged 40-unit
                // v3 economic capacity. The 90k wage is a COMPONENT of the existing
                // 150k mandatory daily charge, never an additional payable.
                Profile("coffee-kiosk", BusinessType.CoffeeKiosk, "mvp.market.coffee", 480, 20,
                    new long[]{20000,30000,40000}, 12000, 150000, new[]{11000,12500,9500,11500},
                    new FixedBusinessHelperDefinition("kiosk-attendant", "helper.kiosk-attendant",
                        90000, 20, EconomicDefinitionLimits.Scale))
            };
            return new BusinessEconomicCatalog("m9-t02.functional-fixture-v1", "v1", "m9-t02.rules-v1",
                segmentDefinitions, pools, profiles);
        }

        // Separate P04 preview. Existing M9T02FunctionalEconomy.Create() and its
        // archived v1 matrices stay exactly as authored for existing receipts.
        public static BusinessEconomicCatalog CreateBusinessBatchA()
        {
            var baseline = Create();
            var extraPools = new[]
            {
                Pool("p04.market.creative-agency", 9, 1000, 1000, 5500, 2500),
                Pool("p04.market.small-cafe", 95, 1500, 4000, 2500, 2000),
                Pool("p04.market.fashion-retail", 35, 2000, 1500, 2500, 4000)
            };
            var extraProfiles = new[]
            {
                Profile(FirstPlayableContentTemplate.MarketingAgencyId,
                    BusinessType.MarketingAgency, "p04.market.creative-agency", 180, 4,
                    new long[] { 420000, 550000, 750000 }, 115000, 60000,
                    new[] { 7000, 8500, 13500, 11000 },
                    profileRevision: "p04-biz-a.profile-v1"),
                // The 110k fixed assistant wage is included in the 250k
                // mandatory daily cost; there is no second staff invoice.
                Profile(FirstPlayableContentTemplate.SmallCafeId,
                    BusinessType.SmallCafe, "p04.market.small-cafe", 480, 36,
                    new long[] { 28000, 42000, 55000 }, 18000, 250000,
                    new[] { 8000, 13500, 10500, 11000 },
                    new FixedBusinessHelperDefinition("cafe-assistant",
                        "helper.cafe-assistant", 110000, 20, EconomicDefinitionLimits.Scale),
                    "p04-biz-a.profile-v1"),
                Profile(FirstPlayableContentTemplate.FashionStoreId,
                    BusinessType.FashionStore, "p04.market.fashion-retail", 420, 24,
                    new long[] { 120000, 170000, 250000 }, 80000, 125000,
                    new[] { 10000, 8500, 10000, 13500 },
                    profileRevision: "p04-biz-a.profile-v1")
            };
            return new BusinessEconomicCatalog("p04.biz-a.preview-v1",
                "biz-a.v1", "p04-biz-a.rules-v1", baseline.Segments.Values,
                baseline.Pools.Values.Concat(extraPools),
                baseline.Profiles.Values.Concat(extraProfiles));
        }

        private static MarketPoolDefinition Pool(string id, int daily, int budget, int convenience, int quality, int trend) =>
            new MarketPoolDefinition(id, "v1", "m9-t02.competition-v1", daily,
                SegmentFactors(new[]{budget, convenience, quality, trend}));

        private static IEnumerable<KeyValuePair<string, int>> SegmentFactors(int[] firstFour)
        {
            if (firstFour.Length != 4) throw new ArgumentException("Expected exactly four active values.");
            for (var i = 0; i < Segments.Length; i++)
                yield return new KeyValuePair<string, int>(Segments[i], i < 4 ? firstFour[i] : 0);
        }

        private static BusinessEconomicProfile Profile(string id, BusinessType type, string pool, int ownerMinutes,
            int baseCapacity, long[] prices, long variable, long daily, int[] productFit,
            FixedBusinessHelperDefinition? fixedHelper = null,
            string profileRevision = "m9-t02.profile-v1")
        {
            var posture = new[] { PricingPosture.Budget, PricingPosture.Standard, PricingPosture.Premium };
            var priceFit = new[] {
                new[]{11500,10500,9000,10500},
                new[]{10000,10000,10000,10000},
                new[]{7200,8500,11500,9500}
            };
            return new BusinessEconomicProfile(id, "v1", type, profileRevision, pool,
                ownerMinutes, baseCapacity, variable, daily, SegmentFactors(productFit),
                posture.Select((p,i) => new KeyValuePair<PricingPosture, IEnumerable<KeyValuePair<string,int>>>(
                    p, SegmentFactors(priceFit[i]))),
                posture.Select((p,i) => new KeyValuePair<PricingPosture,long>(p, prices[i])),
                fixedHelper);
        }
    }
}
