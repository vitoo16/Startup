using StartupLife.Application;
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using StartupLife.Content;
using StartupLife.Infrastructure;
using StartupLife.Core;
using StartupLife.Simulation;
using StartupLife.Integration;

internal static class Program
{
    private static int passed;
    private static readonly List<string> Failures = new List<string>();

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
    private static void Check(string name, Action run)
    {
        try { run(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception e) { Failures.Add(name + ": " + e); Console.Error.WriteLine("FAIL " + name + ": " + e.Message); }
    }

    private sealed class UnavailableArchive : IHistoricalEconomicRulesResolver
    {
        public bool TryResolve(string archiveId,string version,out ContentCatalog? content)
        {
            content=null;
            return false;
        }
    }

    private sealed class FailedWriteStore : ISaveStore
    {
        private readonly ISaveStore inner;
        public FailedWriteStore(ISaveStore inner) { this.inner = inner; }
        public LoadResult Read() => inner.Read();
        public WriteStatus Commit(byte[] validatedBytes, long expectedRevision) => WriteStatus.Failed;
    }

    private static int Main(string[] args)
    {
        var catalog = M9T02FunctionalEconomy.Create();
        Check("six ID-keyed segments with exactly four active", () =>
        {
            Assert(catalog.Segments.Count == 6, "six segments");
            Assert(catalog.Segments.Count(x => x.Value.Enabled) == 4, "four enabled");
            Assert(catalog.Segments["business-b2b"].Enabled == false, "B2B disabled");
            Assert(catalog.Segments["loyal-relationship"].Enabled == false, "loyal disabled");
            Assert(catalog.Pools.Count == 4 && catalog.Profiles.Count == 4, "four pools/profiles");
            foreach (var pool in catalog.Pools.Values)
            {
                Assert(pool.SegmentShareById.Where(x => catalog.Segments[x.Key].Enabled).Sum(x => x.Value) == 10000,
                    "normalized active weights");
                Assert(pool.SegmentShareById["business-b2b"] == 0 && pool.SegmentShareById["loyal-relationship"] == 0,
                    "zero inactive shares");
            }
        });
        Check("four exact functional fixture prices and charges", () =>
        {
            var fixtures = new[] {
                ("online-store", 95000L, 8000L, 8),
                ("home-food-preorder", 45000L, 12000L, 8),
                ("freelance-service", 220000L, 5000L, 3),
                ("coffee-kiosk", 30000L, 150000L, 40)
            };
            foreach (var x in fixtures)
            {
                var p = catalog.Profiles[x.Item1];
                Assert(p.UnitPriceByPosture[PricingPosture.Standard] == x.Item2, "price " + x.Item1);
                Assert(p.DailyMandatoryChargeVnd == x.Item3, "fixed " + x.Item1);
                Assert(p.FullCapacityUnits == x.Item4, "capacity " + x.Item1);
            }
        });
        Check("M10 kiosk fixed helper contributes capacity without duplicating the wage", () =>
        {
            var kiosk = catalog.Profiles["coffee-kiosk"];
            var helper = kiosk.FixedHelper;
            Assert(helper != null && helper.Id == "kiosk-attendant", "helper must be authored");
            Assert(helper!.ContractedDailyWageVnd == 90000L &&
                kiosk.NonHelperDailyFixedChargeVnd == 60000L,
                "fixed 150k/day must include helper wage once, not add another invoice");
            Assert(kiosk.BaseOwnerCapacityUnits == 20 &&
                helper.EffectiveAdditionalCapacityUnits == 20 &&
                kiosk.FullCapacityUnits == 40, "solo + helper capacity yields existing v3 effective 40");
            Assert(catalog.Profiles.Values.Count(x => x.FixedHelper != null) == 1,
                "MVP has exactly one contracted helper at the kiosk");
            Assert(catalog.Profiles.Where(x => x.Key != "coffee-kiosk")
                .All(x => x.Value.BaseOwnerCapacityUnits == x.Value.FullCapacityUnits),
                "other business capacity must not change");
            Throws<ArgumentException>(() => new FixedBusinessHelperDefinition(
                "invalid-helper", "helper.invalid", 0, 20, EconomicDefinitionLimits.Scale));
            Throws<ArgumentException>(() => new FixedBusinessHelperDefinition(
                "invalid-helper", "helper.invalid", 90000, 0, EconomicDefinitionLimits.Scale));
        });
        var supply = CustomerDemandCalculator.OpeningSupply("2026-10-09", catalog, "mvp.market.online-retail");
        Check("largest remainder pool supply is exact, tie by segment ID", () =>
        {
            var v = supply.OriginalUnitsBySegment;
            Assert(v.Values.Sum() == 18, "market supply must equal base demand");
            Assert(v["budget-sensitive"] == 6, "budget share");
            Assert(v["convenience-focused"] == 3, "convenience share");
            Assert(v["quality-focused"] == 5, "quality wins tie");
            Assert(v["trend-aware"] == 4, "trend loses tie");
            Assert(v["loyal-relationship"] == 0 && v["business-b2b"] == 0, "disabled supply");
        });
        var alpha = new BusinessDemandCandidate("alpha", "online-store", PricingPosture.Standard);
        var beta = new BusinessDemandCandidate("beta", "online-store", PricingPosture.Standard);
        Check("one business respects pool supply, no fake full-conversion", () =>
        {
            var offers = CustomerDemandCalculator.AllocateRemaining(catalog, supply,
                Array.Empty<MarketFulfillmentUnit>(), new[]{alpha});
            Assert(offers.Sum(x=>x.OfferedUnits) <= 18, "can't exceed finite pool");
            Assert(!offers.Any(x=>catalog.Segments[x.SegmentId].Enabled == false), "no inactive demand");
            Assert(offers.Single(x=>x.SegmentId=="convenience-focused").OfferedUnits == 2, "raw floor not full supply");
        });
        Check("competition caps shared pool and canonical ordering", () =>
        {
            var forward = CustomerDemandCalculator.AllocateRemaining(catalog, supply,
                Array.Empty<MarketFulfillmentUnit>(), new[]{alpha,beta});
            var reverse = CustomerDemandCalculator.AllocateRemaining(catalog, supply,
                Array.Empty<MarketFulfillmentUnit>(), new[]{beta,alpha});
            string Normalize(IEnumerable<BusinessSegmentDemand> items) =>
                string.Join("|",items.Select(x=>x.SegmentId + ":" + x.BusinessInstanceId + ":" + x.OfferedUnits));
            Assert(Normalize(forward)==Normalize(reverse), "business enumeration order changed result");
            Assert(forward.GroupBy(x=>x.SegmentId).All(g =>
                g.Sum(x=>x.OfferedUnits)<=supply.OriginalUnitsBySegment[g.Key]), "pool duplicated across competitors");
            Assert(forward.Single(x=>x.SegmentId=="budget-sensitive"&&x.BusinessInstanceId=="alpha").OfferedUnits == 3,
                "budget alpha 3/6");
            Assert(forward.Single(x=>x.SegmentId=="budget-sensitive"&&x.BusinessInstanceId=="beta").OfferedUnits == 3,
                "budget beta 3/6");
        });
        var sale=new MarketFulfillmentUnit("sale-1","2026-10-09","mvp.market.online-retail",
            "budget-sensitive",0,"alpha","slice-1","run/op/1");
        Check("sold units survive replanning and future pool exhaustion", () =>
        {
            var remaining=CustomerDemandCalculator.RemainingSupply(supply,new[]{sale});
            Assert(remaining["budget-sensitive"]==5, "committed supply was recreated");
            var offers=CustomerDemandCalculator.AllocateRemaining(catalog,supply,new[]{sale},new[]{alpha,beta});
            Assert(offers.Where(x=>x.SegmentId=="budget-sensitive").Sum(x=>x.OfferedUnits)<=5,
                "replan oversold budget segment");
        });
        Check("duplicate market unit and identity rejected", () =>
        {
            var duplicate = new MarketFulfillmentUnit("sale-2","2026-10-09","mvp.market.online-retail",
                "budget-sensitive",0,"beta","slice-2","run/op/2");
            Throws<ArgumentException>(() => CustomerDemandCalculator.RemainingSupply(supply,new[]{sale,duplicate}));
            Throws<ArgumentException>(() => CustomerDemandCalculator.RemainingSupply(supply,new[]{sale,sale}));
        });
        Check("zero weight cannot produce demand", () =>
        {
            var zero = new BusinessDemandCandidate("alpha","online-store",PricingPosture.Standard,reputation:0);
            Assert(CustomerDemandCalculator.AllocateRemaining(catalog,supply,
                Array.Empty<MarketFulfillmentUnit>(),new[]{zero}).Count==0,"zero attractiveness");
        });
        Check("version-1 integer bounds reject out of range", () =>
        {
            Throws<ArgumentOutOfRangeException>(()=>new MarketPoolDefinition("invalid.pool","v1","v1",100001,
                new[]{new KeyValuePair<string,int>("budget-sensitive",10000)}));
            Throws<ArgumentOutOfRangeException>(()=>new CustomerSegmentDefinition("invalid-segment","v1",true,
                40001,10000,10000,10000,10000,10000));
        });

        Check("eight obligation cadences use only authored deterministic triggers", () =>
        {
            BusinessObligationDefinition Definition(ObligationCadence cadence) =>
                new BusinessObligationDefinition("sample-due","v1","online-store","charge-v1",
                    cadence,ObligationProrationPolicy.FullContractual,
                    ObligationClosurePolicy.EnforceableThroughContractEnd,"2026-01-31",null,100,true);
            var one=BusinessObligationScheduler.DueOnDate(Definition(ObligationCadence.OneTime),"b1","2026-01-31",false);
            Assert(one!=null && one.AmountDueVnd==100,"one-time due");
            Assert(BusinessObligationScheduler.DueOnDate(Definition(ObligationCadence.OneTime),"b1","2026-02-01",true)==null,
                "one-time duplicate period");
            Assert(BusinessObligationScheduler.DueOnDate(Definition(ObligationCadence.PerOperation),"b1","2026-02-01",true,
                committedOperationId:"op7")!=null,"per-operation due");
            Assert(BusinessObligationScheduler.DueOnDate(Definition(ObligationCadence.PerOperation),"b1","2026-02-01",true)==null,
                "no fabricated per-operation");
            Assert(BusinessObligationScheduler.DueOnDate(Definition(ObligationCadence.PerOperatingDay),"b1",
                "2026-02-01",true,committedOperationId:"op8")!=null,"per-operating-day due");
            Assert(BusinessObligationScheduler.DueOnDate(Definition(ObligationCadence.Daily),"b1","2026-02-01",false)!=null,
                "contractual fixed daily charge survives pause");
            var dayMetered = new BusinessObligationDefinition("metered","v1","online-store","v1",
                ObligationCadence.Daily,ObligationProrationPolicy.DayMetered,
                ObligationClosurePolicy.CancellableAfterServiceDate,"2026-01-31",null,100,false);
            Assert(BusinessObligationScheduler.DueOnDate(dayMetered,"b1","2026-02-01",false)==null,
                "day-metered recurrence requires eligible service");
            Assert(BusinessObligationScheduler.DueOnDate(Definition(ObligationCadence.Daily),"b1","2026-02-01",true)!=null,
                "day-metered daily");
            Assert(BusinessObligationScheduler.DueOnDate(Definition(ObligationCadence.Weekly),"b1","2026-02-07",false)!=null,
                "weekly contractual due survives pause");
            Assert(BusinessObligationScheduler.DueOnDate(Definition(ObligationCadence.Weekly),"b1","2026-02-06",false)==null,
                "no early weekly due");
            Assert(BusinessObligationScheduler.DueOnDate(Definition(ObligationCadence.Monthly),"b1","2026-02-28",false)!=null,
                "monthly anniversary clamps");
            Assert(BusinessObligationScheduler.DueOnDate(Definition(ObligationCadence.Annual),"b1","2027-01-31",false)!=null,
                "annual anniversary");
            Assert(BusinessObligationScheduler.DueOnDate(Definition(ObligationCadence.OnClose),"b1","2026-02-07",false,
                closeOperationId:"close-op")!=null,"authored close fee");
            Assert(BusinessObligationScheduler.DueOnDate(Definition(ObligationCadence.OnClose),"b1","2026-02-07",false)==null,
                "no unauthored close event");
        });
        Check("cumulative day-metered proration 33+33+34=100", () =>
        {
            var increments=new[]{
                BusinessObligationScheduler.AccrualDelta(100,0,1,3),
                BusinessObligationScheduler.AccrualDelta(100,1,2,3),
                BusinessObligationScheduler.AccrualDelta(100,2,3,3)};
            Assert(increments.SequenceEqual(new long[]{33,33,34}),"cumulative floor");
            Assert(BusinessObligationScheduler.AccruedThroughEligibleDays(100,3,3)==100,"full period exact");
            Throws<ArgumentException>(()=>BusinessObligationScheduler.AccrualDelta(100,2,1,3));
            Throws<ArgumentException>(()=>BusinessObligationScheduler.AccruedThroughEligibleDays(100,1,0));
            Throws<ArgumentException>(()=>BusinessObligationScheduler.AccruedThroughEligibleDays(100,4,3));
        });
        Check("oldest due payment order and partial cash reserve", () =>
        {
            var old=new ObligationDueTranche("old","d:old","2026-01-01",600,1,100,40);
            var next=new ObligationDueTranche("new","d:new","2026-02-01",480,2,100,0);
            var allocations=BusinessObligationScheduler.PlanDuePayments(new[]{next,old},70);
            Assert(allocations.Count==2,"two attributed payments");
            Assert(allocations[0].TrancheId=="old" && allocations[0].PaidVnd==60,"old arrear first");
            Assert(allocations[1].TrancheId=="new" && allocations[1].PaidVnd==10,"remainder applied once");
            Assert(BusinessObligationScheduler.PlanDuePayments(new[]{next,old},0).Count==0,
                "zero available cash never funds expenses");
        });


        Check("future catalog revision permits fifth active stable segment", () =>
        {
            var expanded = catalog.Segments.Values.Select(s =>
                new CustomerSegmentDefinition(s.SegmentId, "v2",
                    s.Enabled || s.SegmentId == "loyal-relationship",
                    s.PriceSensitivity, s.Quality, s.Convenience, s.Trend, s.Trust, s.Repeat)).ToArray();
            var expandedCatalog = new BusinessEconomicCatalog(catalog.FixtureId, "v2",
                "m9-t02.rules-v2", expanded, catalog.Pools.Values, catalog.Profiles.Values);
            Assert(expandedCatalog.Segments.Values.Count(x => x.Enabled) == 5,
                "future explicit segment enablement");
            Assert(catalog.Segments.Values.Count(x => x.Enabled) == 4,
                "current fixture must retain four active segments");
        });


        Check("P-03 A/B/C exact 100-20-80 wallet time vector", () =>
        {
            var sameHours = Enumerable.Range(1080, 240).ToArray();
            var portfolio = new[] {
                new BusinessTimeWalletCandidate("a", "A", BusinessOperationMode.SideHustleCompatible,
                    120, 6, 0, 0, 6, 25, sameHours),
                new BusinessTimeWalletCandidate("b", "B", BusinessOperationMode.SideHustleCompatible,
                    120, 6, 0, 0, 6, 15, sameHours),
                new BusinessTimeWalletCandidate("c", "C", BusinessOperationMode.SideHustleCompatible,
                    120, 6, 0, 0, 6, 0, sameHours)
            };
            var plan = BusinessAffordabilityPlanner.Plan("epoch-abc", 80, portfolio);
            Assert(plan.ByInstance("A").ReservedUnits == 2, "A needs 2 units");
            Assert(plan.ByInstance("B").ReservedUnits == 2, "B needs 2 units");
            Assert(plan.ByInstance("C").ReservedUnits == 6, "C needs 6 zero-cost units");
            Assert(plan.ByInstance("A").ReservedOwnerMinutes.Count == 40, "A needs 40m");
            Assert(plan.ByInstance("B").ReservedOwnerMinutes.Count == 40, "B needs 40m");
            Assert(plan.ByInstance("C").ReservedOwnerMinutes.Count == 120, "C needs 120m");
            Assert(plan.TotalCostReservedVnd == 80 && plan.SpendableAfterReservationsVnd == 0,
                "variable wallet 80 only");
            Assert(plan.AllUnitReservations.Count == 10, "one reservation per accepted unit");
            Assert(plan.AllUnitReservations.Select(x => x.Id).Distinct().Count() == 10,
                "unique stable reservation IDs");
            var permuted = BusinessAffordabilityPlanner.Plan("epoch-abc", 80,
                new[]{portfolio[2],portfolio[0],portfolio[1]});
            foreach (var b in new[]{"A","B","C"})
            {
                Assert(plan.ByInstance(b).ReservedUnits == permuted.ByInstance(b).ReservedUnits,
                    "permutation changed units");
                Assert(plan.ByInstance(b).ReservedOwnerMinutes.SequenceEqual(
                    permuted.ByInstance(b).ReservedOwnerMinutes), "permutation changed time");
            }
        });

        Check("P-03 unequal R is normalized, not equal legacy minute shares", () =>
        {
            var slots = Enumerable.Range(0, 120);
            var a = new BusinessTimeWalletCandidate("a", "a1", BusinessOperationMode.SideHustleCompatible,
                60, 6, 0, 0, 6, 0, slots);
            var b = new BusinessTimeWalletCandidate("b", "b1", BusinessOperationMode.SideHustleCompatible,
                120, 6, 0, 0, 6, 0, slots);
            var p = BusinessAffordabilityPlanner.Plan("epoch-unequal", 0, new[]{b,a});
            Assert(p.ByInstance("a1").ReservedUnits == 4, "A normalized quota 4");
            Assert(p.ByInstance("b1").ReservedUnits == 4, "B normalized quota 4");
            Assert(p.ByInstance("a1").ReservedOwnerMinutes.Count == 40, "A 40m");
            Assert(p.ByInstance("b1").ReservedOwnerMinutes.Count == 80, "B 80m");
            Assert(p.TotalCostReservedVnd == 0, "zero cost is valid");
        });

        Check("P-03 zero wallet still funds zero-cost units without income", () =>
        {
            var expensive = new BusinessTimeWalletCandidate("a", "paid",
                BusinessOperationMode.SideHustleCompatible, 60, 6, 0, 0, 6, 10, Enumerable.Range(0,120));
            var free = new BusinessTimeWalletCandidate("b", "free",
                BusinessOperationMode.SideHustleCompatible, 60, 6, 0, 0, 6, 0, Enumerable.Range(0,120));
            var p = BusinessAffordabilityPlanner.Plan("epoch-free", 0, new[]{expensive,free});
            Assert(p.ByInstance("paid").ReservedUnits == 0, "unaffordable business");
            Assert(p.ByInstance("free").ReservedUnits == 6, "free business must still operate");
            Assert(p.ByInstance("free").ReservedOwnerMinutes.Count == 60, "free units use actual minutes");
            Assert(p.TotalCostReservedVnd == 0, "no same-day revenue financing");
        });

        Check("global matching reassigns occupied shared minute to feasible peer", () =>
        {
            var a = new BusinessTimeWalletCandidate("a","a",BusinessOperationMode.SideHustleCompatible,
                1,1,0,0,1,0,new[]{0,1});
            var b = new BusinessTimeWalletCandidate("b","b",BusinessOperationMode.SideHustleCompatible,
                1,1,0,0,1,0,new[]{0});
            var p = BusinessAffordabilityPlanner.Plan("match",0,new[]{a,b});
            Assert(p.ByInstance("a").ReservedOwnerMinutes.SequenceEqual(new[]{1}),"A must augment to minute 1");
            Assert(p.ByInstance("b").ReservedOwnerMinutes.SequenceEqual(new[]{0}),"B must get minute 0");
        });

        Check("C greater than R cannot mint unreserved units on one-minute floor jump", () =>
        {
            var paid = new BusinessTimeWalletCandidate("high-throughput","i1",
                BusinessOperationMode.SideHustleCompatible,1,4,0,0,4,1,new[]{42});
            var p = BusinessAffordabilityPlanner.Plan("floor-jump",2,new[]{paid});
            Assert(p.ByInstance("i1").ReferenceEligibleUnits==4,"one minute supports 4 potential capacity");
            Assert(p.ByInstance("i1").ReservedUnits==2,"wallet reserves only two units");
            Assert(p.ByInstance("i1").ReservedOwnerMinutes.Count==1,"one committed minute");
            Assert(p.AllUnitReservations.Count==2,"no unreserved third or fourth unit");
            var prior = new BusinessTimeWalletCandidate("high-throughput","i1",
                BusinessOperationMode.SideHustleCompatible,1,4,1,0,4,0,Array.Empty<int>());
            var after = BusinessAffordabilityPlanner.Plan("no-retro",0,new[]{prior});
            Assert(after.ByInstance("i1").ReservedUnits==0, "old unreserved minutes cannot earn later");
        });

        Check("full-time contiguous reservation excludes overlapping side minutes", () =>
        {
            var full = new BusinessTimeWalletCandidate("a","kiosk",
                BusinessOperationMode.FullTimeRequired,3,3,0,0,3,0,new[]{50,51,52});
            var side = new BusinessTimeWalletCandidate("b","side",
                BusinessOperationMode.SideHustleCompatible,1,1,0,0,4,0,new[]{50,51,52,53});
            var p = BusinessAffordabilityPlanner.Plan("full",0,new[]{side,full});
            Assert(p.ByInstance("kiosk").ReservedUnits==3,"full-time 3 capacity");
            Assert(p.ByInstance("kiosk").ReservedOwnerMinutes.SequenceEqual(new[]{50,51,52}),"contiguous full");
            Assert(p.ByInstance("side").ReservedUnits==1,"side only leftover minute");
            Assert(p.ByInstance("side").ReservedOwnerMinutes.SequenceEqual(new[]{53}),"side cannot steal kiosk");
            Throws<ArgumentException>(() => BusinessAffordabilityPlanner.Plan("invalid",0,new[]{full,full}));
        });

        Check("previously fulfilled and committed time preserved in new epoch", () =>
        {
            var partial = new BusinessTimeWalletCandidate("a","a1",
                BusinessOperationMode.SideHustleCompatible,120,6,60,3,3,0,
                Enumerable.Range(60,60));
            var p = BusinessAffordabilityPlanner.Plan("replan",0,new[]{partial},Enumerable.Range(0,60));
            Assert(p.ByInstance("a1").ReferenceEligibleUnits==3, "only remaining daily capacity");
            Assert(p.ByInstance("a1").ReservedUnits==3, "can earn remaining 3");
            Assert(p.ByInstance("a1").ReservedOwnerMinutes.Count==60, "60 new committed minutes");
            Assert(p.AllUnitReservations.Select(x=>x.AbsoluteBusinessUnitOrdinal).SequenceEqual(new[]{4,5,6}),
                "ordinals continue through a genuine replan");
        });


        Check("contractual due100 immediate40 later income80 pays same liability once", () =>
        {
            var obligation = new ObligationDueTranche("due-1","obligation-1","2026-10-09",
                0,1,100,40);
            var credited = ObligationIncomeDistributor.Plan("business-income/receipt-1",80,
                new[]{obligation});
            Assert(credited.GrossIncomeVnd==80 && credited.WithheldToOldestArrearsVnd==60,
                "revenue withheld 60 into linked obligation");
            Assert(credited.NetCreditedToCashVnd==20,"only 20 credited cash");
            var updated=credited.ApplyTo(new[]{obligation});
            Assert(updated.Single().PaidVnd==100 && updated.Single().OutstandingVnd==0,
                "linked debt paid exactly once");
            Throws<ArgumentException>(()=>credited.ApplyTo(updated));
            Assert(-40 + credited.NetCreditedToCashVnd == -20,
                "no second 60 cash debit on income withholding");
        });

        Check("salary and business income share stable oldest-first arrears graph", () =>
        {
            var debtEarly=new ObligationDueTranche("first","fixed/early","2026-02-01",0,1,100,0);
            var debtLate=new ObligationDueTranche("second","fixed/late","2026-03-01",0,2,100,0);
            var salary=ObligationIncomeDistributor.Plan("salary/receipt",130,new[]{debtLate,debtEarly});
            Assert(salary.ArrearAllocations.Count==2,"both arrears");
            Assert(salary.ArrearAllocations[0].TrancheId=="first" &&
                salary.ArrearAllocations[0].PaidFromIncomeVnd==100,"oldest due first");
            Assert(salary.ArrearAllocations[1].TrancheId=="second" &&
                salary.ArrearAllocations[1].PaidFromIncomeVnd==30,"remaining next due");
            var updated=salary.ApplyTo(new[]{debtLate,debtEarly});
            var business=ObligationIncomeDistributor.Plan("business/receipt",100,updated);
            Assert(business.ArrearAllocations.Single().TrancheId=="second" &&
                business.ArrearAllocations.Single().PaidFromIncomeVnd==70,"business uses same remaining debt");
            Assert(business.NetCreditedToCashVnd==30,"business sale net 30");
            Assert(business.ApplyTo(updated).All(x=>x.OutstandingVnd==0),"all liabilities cleared");
        });

        Check("full-time with zero accepted units releases future side slots", () =>
        {
            var paidSide=new BusinessTimeWalletCandidate("a","paid",
                BusinessOperationMode.SideHustleCompatible,1,1,0,0,1,60,new[]{0,1,2,3});
            var unpaidFull=new BusinessTimeWalletCandidate("b","full",
                BusinessOperationMode.FullTimeRequired,3,3,0,0,3,60,new[]{0,1,2});
            var freeSide=new BusinessTimeWalletCandidate("c","free",
                BusinessOperationMode.SideHustleCompatible,1,4,0,0,4,0,new[]{0,1,2,3});
            var p=BusinessAffordabilityPlanner.Plan("releasing-fulltime",60,
                new[]{unpaidFull,freeSide,paidSide});
            Assert(p.ByInstance("paid").ReservedUnits==1,"paid competitor uses cash");
            Assert(p.ByInstance("full").ReservedUnits==0 &&
                p.ByInstance("full").ReservedOwnerMinutes.Count==0,"unfunded full-time must release block");
            Assert(p.ByInstance("free").ReservedUnits>=1,"free business remains eligible");
            Assert(p.TotalCostReservedVnd==60,"wallet exactly reserved once");
        });


        Check("frozen epoch time crossing equals sum of split AdvanceBoundary children", () =>
        {
            var candidate=new BusinessTimeWalletCandidate("a","epoch-a",BusinessOperationMode.SideHustleCompatible,
                120,6,0,0,6,5,Enumerable.Range(1000,120));
            var plan=BusinessAffordabilityPlanner.Plan("epoch-immutable",30,new[]{candidate});
            var initial=FrozenBusinessEpochCursor.Begin(plan,1000);
            var first=initial.AdvanceTo(plan,1060);
            var second=first.Next.AdvanceTo(plan,1120);
            var whole=initial.AdvanceTo(plan,1120);
            Assert(first.Next.ByBusiness["epoch-a"].UnitsEarnedInEpoch==3,"first 60m 3 units");
            Assert(first.NewlyConsumedReservations.Count==3 && first.VariablePaymentVnd==15,
                "first 60m has three linked variable reservations");
            Assert(second.Next.ByBusiness["epoch-a"].UnitsEarnedInEpoch==6,"120m total 6 units");
            Assert(whole.Next.ByBusiness["epoch-a"].UnitsEarnedInEpoch==6,"one-shot total 6 units");
            var splitIds=first.NewlyConsumedReservations.Concat(second.NewlyConsumedReservations).Select(x=>x.Id);
            Assert(splitIds.SequenceEqual(whole.NewlyConsumedReservations.Select(x=>x.Id)),
                "receipt boundary splitting changed unit reservation IDs");
            Assert(first.VariablePaymentVnd+second.VariablePaymentVnd==whole.VariablePaymentVnd,
                "variable costs re-charged or lost by split");
        });

        Check("epoch consumes C greater than R only up to reserved two units", () =>
        {
            var item=new BusinessTimeWalletCandidate("a","floor-i",BusinessOperationMode.SideHustleCompatible,
                1,4,0,0,4,1,new[]{42});
            var plan=BusinessAffordabilityPlanner.Plan("floor-epoch",2,new[]{item});
            var result=FrozenBusinessEpochCursor.Begin(plan,42).AdvanceTo(plan,43);
            Assert(result.NewlyConsumedReservations.Count==2,"floor jump cannot mint more than reserved units");
            Assert(result.VariablePaymentVnd==2,"cost charged once per reserved fulfilled unit");
            Assert(result.Next.ByBusiness["floor-i"].OwnerMinutesWorkedInEpoch==1,"actual worked minute");
            Assert(result.Next.ByBusiness["floor-i"].UnitsEarnedInEpoch==2,"earned capped at reservation quota");
        });

        Check("Study never credits business work and invalidates frozen epoch", () =>
        {
            var item=new BusinessTimeWalletCandidate("a","study-business",BusinessOperationMode.SideHustleCompatible,
                2,2,0,0,2,0,new[]{50,51});
            var plan=BusinessAffordabilityPlanner.Plan("study-epoch",0,new[]{item});
            var initial=FrozenBusinessEpochCursor.Begin(plan,50);
            var result=initial.AdvanceTo(plan,51,isStudyCommand:true);
            Assert(result.NewlyConsumedReservations.Count==0 && result.VariablePaymentVnd==0,
                "study may not generate business revenue/variable charges");
            Assert(result.Next.RequiresGenuineReplan,"study must trigger prospective epoch recomputation");
            Throws<ArgumentException>(()=>result.Next.AdvanceTo(plan,52));
            Throws<ArgumentException>(()=>initial.AdvanceTo(plan,49));
        });



        Check("v3 price changes are next-day, overwritten, and can be cleared", () =>
        {
            var initial=new BusinessProspectivePolicy("i",EconomicOperationStatus.Auto,
                PricingPosture.Standard);
            var premium=BusinessPolicyTransitions.RequestPrice(initial,PricingPosture.Premium,
                "2026-10-10","price-1");
            Assert(premium.EffectivePricing==PricingPosture.Standard,"same day keeps old selling price");
            Assert(premium.PendingPricing==PricingPosture.Premium &&
                premium.PendingPricingEffectiveIso=="2026-10-11","price pending next simulation day");
            var budget=BusinessPolicyTransitions.RequestPrice(premium,PricingPosture.Budget,
                "2026-10-10","price-2");
            Assert(budget.PendingPricing==PricingPosture.Budget,"latest same-day pricing overwrites pending");
            var cancelled=BusinessPolicyTransitions.RequestPrice(budget,PricingPosture.Standard,
                "2026-10-10","price-3");
            Assert(cancelled.PendingPricing==null && cancelled.EffectivePricing==PricingPosture.Standard,
                "request effective price cancels pending");
            var beforeMidnight=BusinessPolicyTransitions.ActivateNewDate(budget,"2026-10-10","boundary-1");
            Assert(beforeMidnight.EffectivePricing==PricingPosture.Standard,"no same-day promotion");
            var nextDay=BusinessPolicyTransitions.ActivateNewDate(budget,"2026-10-11","boundary-2");
            Assert(nextDay.EffectivePricing==PricingPosture.Budget && nextDay.PendingPricing==null,
                "pending takes effect only next day");
        });

        Check("v3 Pause immediate, Resume next day, Close terminal with no fee fabricated", () =>
        {
            var initial=new BusinessProspectivePolicy("i",EconomicOperationStatus.Auto,
                PricingPosture.Standard);
            var paused=BusinessPolicyTransitions.Pause(initial,"pause-1");
            Assert(!paused.IsOperationEligible && paused.EffectiveStatus==EconomicOperationStatus.Paused,
                "pause immediately ends future operations");
            var resume=BusinessPolicyTransitions.RequestResume(paused,"2026-10-10","resume-1");
            Assert(!resume.IsOperationEligible && resume.PendingResumeEffectiveIso=="2026-10-11",
                "resume must wait for next date");
            var promoted=BusinessPolicyTransitions.ActivateNewDate(resume,"2026-10-11","day-change");
            Assert(promoted.IsOperationEligible && !promoted.PendingResume,"resume next day");
            var closed=BusinessPolicyTransitions.Close(resume,"close-1");
            Assert(closed.EffectiveStatus==EconomicOperationStatus.Closed &&
                !closed.PendingResume && closed.PendingPricing==null, "terminal close cancels pending");
            Throws<ArgumentException>(()=>BusinessPolicyTransitions.RequestPrice(closed,
                PricingPosture.Budget,"2026-10-11","illegal"));
            Throws<ArgumentException>(()=>BusinessPolicyTransitions.RequestResume(closed,
                "2026-10-11","illegal"));
        });


        Check("midnight A/B/C settlement conserves gross cost profit and cash", () =>
        {
            var slots=Enumerable.Range(1080,240);
            var candidates=new[]{
                new BusinessTimeWalletCandidate("a","A",BusinessOperationMode.SideHustleCompatible,
                    120,6,0,0,6,25,slots),
                new BusinessTimeWalletCandidate("b","B",BusinessOperationMode.SideHustleCompatible,
                    120,6,0,0,6,15,slots),
                new BusinessTimeWalletCandidate("c","C",BusinessOperationMode.SideHustleCompatible,
                    120,6,0,0,6,0,slots)
            };
            var plan=BusinessAffordabilityPlanner.Plan("settle-epoch",80,candidates);
            var prices=new Dictionary<string,long>{{"A",80},{"B",50},{"C",20}};
            var sold=new List<BusinessUnitFinancialLine>();
            var ordinal=0;
            foreach(var allocation in plan.Allocations)
                foreach(var reservation in allocation.UnitReservations)
                {
                    var unit=new MarketFulfillmentUnit("line-"+ordinal,"2026-10-09","test.pool",
                        "budget-sensitive",ordinal,allocation.InstanceId,
                        "slice-"+allocation.InstanceId,"advance/receipt");
                    sold.Add(new BusinessUnitFinancialLine(unit,reservation,prices[allocation.InstanceId],
                        "m9-t02.profile-v1",PricingPosture.Standard));
                    ordinal++;
                }
            var supply=new MarketPoolDaySupply("2026-10-09","test.pool","v1",
                new[]{new KeyValuePair<string,int>("budget-sensitive",10)});
            var settlement=BusinessDaySettlementEngine.Calculate("run","2026-10-09","midnight/operation",
                new[]{supply},sold,plan.AllUnitReservations,
                new[]{"slice-A","slice-B","slice-C"},
                new[]{new KeyValuePair<string,long>("A",20)},
                Array.Empty<ObligationDueTranche>(),Array.Empty<string>());
            var view=BusinessFinanceProjection.FromVerifiedSettlement(settlement);
            Assert(view.DateIso=="2026-10-09" && view.SettlementId==settlement.SettlementId,
                "finance projection must only represent committed date and settlement");
            Assert(view.GrossRevenueVnd==380 && view.VariableExpenseVnd==80 &&
                view.FixedExpenseVnd==20 && view.ProfitVnd==280 &&
                view.NetCashCreditVnd==380 && view.ArrearsWithheldVnd==0,
                "finance projection must conserve verified realized amounts");
            Assert(view.Businesses.Select(x=>x.InstanceId).SequenceEqual(new[]{"A","B","C"}),
                "detached finance rows must use canonical ordering");
            Assert(view.Businesses.Sum(x=>x.GrossRevenueVnd)==view.GrossRevenueVnd &&
                view.Businesses.Sum(x=>x.ProfitVnd)==view.ProfitVnd,
                "finance rows and day totals must match");
            Assert(settlement.SettlementId=="run/business-day/2026-10-09","receipt-independent identity");
            Assert(settlement.GrossRevenueVnd==380,"gross A160 + B100 + C120");
            Assert(settlement.VariablePaidDuringDayVnd==80,"variable paid earlier A50 B30");
            Assert(settlement.FixedRecognizedDuringDayVnd==20,"recognized fixed20");
            Assert(settlement.RecognizedProfitVnd==280,"profit=380-80-20");
            Assert(settlement.AppliedToArrearsVnd==0 && settlement.NetCreditedCashVnd==380,
                "no debt, all gross credited at midnight");
            Assert(100-20-80+settlement.NetCreditedCashVnd==380,"wallet after midnight=380");
            Throws<ArgumentException>(()=>BusinessDaySettlementEngine.Calculate("run","2026-10-09",
                "midnight/retry",new[]{supply},sold,plan.AllUnitReservations,
                new[]{"slice-A","slice-B","slice-C"},
                new[]{new KeyValuePair<string,long>("A",20)},
                Array.Empty<ObligationDueTranche>(),new[]{settlement.SettlementId}));
        });

        Check("midnight revenue pays exact original obligation arrear with no double debit", () =>
        {
            var item=new BusinessTimeWalletCandidate("a","A",BusinessOperationMode.SideHustleCompatible,
                1,1,0,0,1,0,new[]{100});
            var plan=BusinessAffordabilityPlanner.Plan("one-sale",0,new[]{item});
            var reservation=plan.AllUnitReservations.Single();
            var market=new MarketFulfillmentUnit("sale-1","2026-10-09","test.pool",
                "budget-sensitive",0,"A","slice-A","adv-1");
            var line=new BusinessUnitFinancialLine(market,reservation,80,"v1",PricingPosture.Standard);
            var supply=new MarketPoolDaySupply("2026-10-09","test.pool","v1",
                new[]{new KeyValuePair<string,int>("budget-sensitive",1)});
            var due=new ObligationDueTranche("due-1","obligation-1","2026-10-09",0,1,100,40);
            var settlement=BusinessDaySettlementEngine.Calculate("run","2026-10-09","midnight-op",
                new[]{supply},new[]{line},new[]{reservation},new[]{"slice-A"},
                Array.Empty<KeyValuePair<string,long>>(),new[]{due},Array.Empty<string>());
            Assert(settlement.GrossRevenueVnd==80,"income80");
            Assert(settlement.AppliedToArrearsVnd==60,"arrear settled60");
            Assert(settlement.NetCreditedCashVnd==20,"net credit20");
            Assert(settlement.UpdatedArrears.Single().PaidVnd==100 &&
                settlement.UpdatedArrears.Single().OutstandingVnd==0,"one payable debt, no duplicate");
            Assert(-40+settlement.NetCreditedCashVnd==-20,"only up-front payment cash debit40");
            Throws<ArgumentException>(()=>BusinessDaySettlementEngine.Calculate("run",
                "2026-10-09","midnight-op",new[]{supply},new[]{line},new[]{reservation},
                Array.Empty<string>(),Array.Empty<KeyValuePair<string,long>>(),
                new[]{due},Array.Empty<string>()));
        });

        Check("settlement rejects duplicated market unit and unmatched cost", () =>
        {
            var item=new BusinessTimeWalletCandidate("a","A",BusinessOperationMode.SideHustleCompatible,
                2,2,0,0,2,0,new[]{200,201});
            var plan=BusinessAffordabilityPlanner.Plan("duplicate-sale",0,new[]{item});
            var reserved=plan.AllUnitReservations.ToArray();
            var l1=new BusinessUnitFinancialLine(
                new MarketFulfillmentUnit("l1","2026-10-09","test.pool","budget-sensitive",
                    0,"A","s","adv"),reserved[0],100,"v1",PricingPosture.Standard);
            var l2=new BusinessUnitFinancialLine(
                new MarketFulfillmentUnit("l2","2026-10-09","test.pool","budget-sensitive",
                    0,"A","s","adv"),reserved[1],100,"v1",PricingPosture.Standard);
            var supply=new MarketPoolDaySupply("2026-10-09","test.pool","v1",
                new[]{new KeyValuePair<string,int>("budget-sensitive",2)});
            Throws<ArgumentException>(()=>BusinessDaySettlementEngine.Calculate("run","2026-10-09","op",
                new[]{supply},new[]{l1,l2},reserved,new[]{"s"},
                Array.Empty<KeyValuePair<string,long>>(),Array.Empty<ObligationDueTranche>(),
                Array.Empty<string>()));
            Throws<ArgumentException>(()=>BusinessDaySettlementEngine.Calculate("run","2026-10-09","op",
                new[]{supply},new[]{l1},Array.Empty<BusinessUnitCostReservation>(),new[]{"s"},
                Array.Empty<KeyValuePair<string,long>>(),Array.Empty<ObligationDueTranche>(),
                Array.Empty<string>()));
        });


        Check("four-pool epoch connects finite segment demand to wallet reservations", () =>
        {
            var d="2026-10-09";
            var catalog=M9T02FunctionalEconomy.Create();
            var supplies=catalog.Pools.Values.Select(x=>CustomerDemandCalculator.OpeningSupply(d,catalog,x.PoolId)).ToArray();
            BusinessMarketEpochParticipant Participant(string id, BusinessType type, string instance, int r, int c,
                long cost, BusinessOperationMode mode, IEnumerable<int> slots) =>
                new BusinessMarketEpochParticipant(
                    new BusinessTimeWalletCandidate(id,instance,mode,r,c,0,0,100,cost,slots),
                    new BusinessDemandCandidate(instance,id,PricingPosture.Standard),
                    new BusinessProspectivePolicy(instance,EconomicOperationStatus.Auto,PricingPosture.Standard),true);
            var portfolio=new[]{
                Participant("online-store",BusinessType.OnlineStore,"online",120,8,52000,
                    BusinessOperationMode.SideHustleCompatible,Enumerable.Range(1080,240)),
                Participant("home-food-preorder",BusinessType.HomeFoodPreorder,"food",120,8,23000,
                    BusinessOperationMode.SideHustleCompatible,Enumerable.Range(1080,240)),
                Participant("freelance-service",BusinessType.FreelanceService,"freelance",120,3,20000,
                    BusinessOperationMode.SideHustleCompatible,Enumerable.Range(1080,240)),
                Participant("coffee-kiosk",BusinessType.CoffeeKiosk,"kiosk",480,40,12000,
                    BusinessOperationMode.FullTimeRequired,Enumerable.Range(540,480))
            };
            var epoch=BusinessMarketEpochPlanner.Freeze(catalog,"market-epoch",d,800000,
                supplies,Array.Empty<MarketFulfillmentUnit>(),portfolio,Enumerable.Range(540,480));
            Assert(epoch.MarketReservations.Count==epoch.OwnerTimeAndWallet.AllUnitReservations.Count,
                "market units and variable wallet reservations are one-to-one");
            Assert(epoch.OwnerTimeAndWallet.TotalCostReservedVnd<=800000,"no borrowed cash");
            Assert(epoch.MarketReservations.All(x=>x.EffectiveUnitPriceVnd>0),"actual unit price attached");
            Assert(epoch.MarketReservations.All(x=>x.Cost.Id.Length>0),"cost reservation attached");
            Assert(epoch.MarketReservations.All(x=>x.MarketUnitOrdinal>=0),"ordinal attached");
            Assert(epoch.MarketReservations.Select(x=>x.MarketPoolId+"/"+x.SegmentId+"/"+x.MarketUnitOrdinal)
                .Distinct().Count()==epoch.MarketReservations.Count,"no market unit double allocation");
            Assert(epoch.OwnerTimeAndWallet.ByInstance("kiosk").ReservedUnits==0,
                "employment blocks kiosk full-time reserve");
            var reversed=BusinessMarketEpochPlanner.Freeze(catalog,"market-epoch",d,800000,
                supplies,Array.Empty<MarketFulfillmentUnit>(),portfolio.Reverse(),
                Enumerable.Range(540,480));
            Assert(epoch.MarketReservations.Select(x=>x.Cost.Id+"|"+x.MarketPoolId+"|"+x.SegmentId+"|"+
                x.MarketUnitOrdinal).SequenceEqual(
                reversed.MarketReservations.Select(x=>x.Cost.Id+"|"+x.MarketPoolId+"|"+x.SegmentId+"|"+
                x.MarketUnitOrdinal)),"portfolio permutation changed frozen market attribution");
        });

        Check("same-day market replan cannot reissue committed pool ordinal", () =>
        {
            var catalog=M9T02FunctionalEconomy.Create();
            var date="2026-10-09";
            var supply=catalog.Pools.Values.Select(x=>CustomerDemandCalculator.OpeningSupply(date,catalog,x.PoolId))
                .ToArray();
            BusinessMarketEpochParticipant Make(int alreadyWorked, int fulfilled, int start,
                string epoch)
            {
                var time=new BusinessTimeWalletCandidate("online-store","i",
                    BusinessOperationMode.SideHustleCompatible,120,8,alreadyWorked,fulfilled,100,52000,
                    Enumerable.Range(start,1320-start));
                var policy=new BusinessProspectivePolicy("i",EconomicOperationStatus.Auto,
                    PricingPosture.Standard,PricingPosture.Premium,"2026-10-10");
                return new BusinessMarketEpochParticipant(time,
                    new BusinessDemandCandidate("i","online-store",PricingPosture.Standard),policy,true);
            }
            var first=BusinessMarketEpochPlanner.Freeze(catalog,"epoch-original",date,500000,
                supply,Array.Empty<MarketFulfillmentUnit>(),new[]{Make(0,0,1080,"epoch-original")},
                Array.Empty<int>());
            Assert(first.MarketReservations.Count>0,"fixture should allocate at least one sale");
            var sold=first.MarketReservations[0];
            var committed=new MarketFulfillmentUnit("sold-1",date,sold.MarketPoolId,sold.SegmentId,
                sold.MarketUnitOrdinal,"i","slice-1","advance-1");
            var second=BusinessMarketEpochPlanner.Freeze(catalog,"epoch-after-sale",date,500000,
                supply,new[]{committed},new[]{Make(15,1,1095,"epoch-after-sale")},Enumerable.Range(1080,15));
            Assert(second.MarketReservations.All(x=>!(x.MarketPoolId==sold.MarketPoolId &&
                x.SegmentId==sold.SegmentId && x.MarketUnitOrdinal==sold.MarketUnitOrdinal)),
                "already sold market unit cannot be reissued by replan");
            Assert(second.MarketReservations.All(x=>x.EffectiveUnitPriceVnd==95000 &&
                x.EffectivePosture==PricingPosture.Standard),
                "pending next-day price cannot affect today's unit sale");
            Assert(supply.Single(x=>x.PoolId==sold.MarketPoolId).OriginalUnitsBySegment.Values.Sum()==18,
                "original opening supply remains immutable");
        });



        Check("migration activation is next date for v2 midday, without retroactive fees", () =>
        {
            var anchor=EconomicActivationAnchor.FromHistoricalCheckpoint(12,"run/op/12",
                "first-playable.v1",2,"2026-10-09",900,"v2-first-playable.v1");
            Assert(anchor.ActivationDateIso=="2026-10-10","midday checkpoint activates tomorrow");
            Assert(!anchor.SourceMidnightAlreadyProcessedLegacySettlement,"midday source not past new-day settlement");
            Assert(!anchor.ShouldOpenEconomicOperationsOnDate("2026-10-09"),"no invented migrated-day operations");
            Assert(anchor.ShouldOpenEconomicOperationsOnDate("2026-10-10"),"next-day activate");
            Assert(anchor.IsLegacyReceiptIndex(0) && anchor.IsLegacyReceiptIndex(11) &&
                !anchor.IsLegacyReceiptIndex(12),"exact receipt-count cutover");
        });

        Check("v2 midnight cutover is lazy current-day only, without salary/living replay", () =>
        {
            var anchor=EconomicActivationAnchor.FromHistoricalCheckpoint(10,"run/op/10",
                "first-playable.v1",2,"2026-10-10",0,"v2-first-playable.v1");
            Assert(anchor.ActivationDateIso=="2026-10-10","00:00 may start v3 economy same date");
            Assert(anchor.SourceMidnightAlreadyProcessedLegacySettlement,
                "midnight v2 already performed legacy salary/living");
            Assert(anchor.ShouldOpenEconomicOperationsOnDate("2026-10-10"),
                "same-day v3 economics can initialize on first committed command");
            Throws<ArgumentException>(()=>new EconomicActivationAnchor(10,"run/op/10",
                "first-playable.v1",2,"2026-10-10",900,"2026-10-10",
                "v2-first-playable.v1","m9-t02.cutover-v1",false));
        });



        Check("staged v2-to-v3 migration retains exact source checkpoint bytes", () =>
        {
            var historicalCatalog = FirstPlayableContentTemplate.BuildCatalog();
            var legacy = GameSession.NewState(historicalCatalog, "v2-stage-test", 98765UL,
                new SimDate(2026, 9, 1));
            StateValidation.Validate(legacy, historicalCatalog, GameSession.CreateRestoreValidator());
            var original = JsonSaveSerializer.WriteObject(legacy);
            var migration = new V2ToV3Migration(new BundledHistoricalV2ContentResolver(),new VersionedReceiptReplay());
            Assert(migration.FromVersion == 2 && migration.ToVersion == 3,
                "adjacent 2-to-3 schema contract");
            var migrated = EconomicV3PayloadCodec.ReadChecked(migration.Migrate(original));
            var recoveredSource = Convert.FromBase64String(migrated.OriginalV2PayloadBase64);
            Assert(original.SequenceEqual(recoveredSource), "v2 checkpoint must stay byte-identical");
            Assert(migrated.OriginalV2PayloadSha256 == V2ToV3Migration.HashV2Payload(original),
                "exact source payload checksum");
            Assert(migrated.Current != null && migrated.Current.SaveVersion == 3 &&
                migrated.Current.Revision == legacy.Revision &&
                migrated.Current.Cash == legacy.Cash &&
                migrated.Current.NextEntity == legacy.NextEntity &&
                migrated.Current.NextOperation == legacy.NextOperation,
                "v3 zero-economic-effect cutover");
            Assert(migrated.Activation != null &&
                migrated.Activation.OriginalSourceSchema == 2 &&
                migrated.Activation.LegacyReceiptCount == 0 &&
                migrated.Activation.ActivationDateIso == "2026-09-01",
                "00:00 cutover activates same date without old salary double-settlement");
            Assert(migrated.EconomicRecords != null &&
                migrated.EconomicRecords.CommittedEpochIds.Count == 0 &&
                migrated.EconomicRecords.CommittedSliceIds.Count == 0 &&
                migrated.EconomicRecords.CommittedFulfillmentIds.Count == 0 &&
                migrated.EconomicRecords.V3RecognizedRevenueVnd == 0,
                "no v3 economic records may be fabricated by migration");
            Assert(JsonSaveSerializer.ReadObject<GameState>(recoveredSource).SaveVersion == 2,
                "old wire schema must remain 2 in embedded original");
            var roundtrip = EconomicV3PayloadCodec.ReadChecked(EconomicV3PayloadCodec.SerializeChecked(migrated));
            Assert(roundtrip.OriginalV2PayloadBase64 == migrated.OriginalV2PayloadBase64,
                "v3 decode/encode retains source bytes exactly");
        });
        Check("staged v3 codec rejects fabricated revenue and post-cutover root mutation", () =>
        {
            var legacy = GameSession.NewState(FirstPlayableContentTemplate.BuildCatalog(),
                "v2-stage-tamper", 98766UL, new SimDate(2026, 9, 2));
            var migration = new V2ToV3Migration(new BundledHistoricalV2ContentResolver(),new VersionedReceiptReplay());
            var original = JsonSaveSerializer.WriteObject(legacy);
            var payload = EconomicV3PayloadCodec.ReadChecked(migration.Migrate(original));
            Assert(payload.EconomicRecords != null && payload.Current != null, "v3 initialized");
            payload.EconomicRecords!.V3RecognizedRevenueVnd = 1;
            Throws<ArgumentException>(() => EconomicV3PayloadCodec.SerializeChecked(payload));
            payload.EconomicRecords.V3RecognizedRevenueVnd = 0;
            payload.Current!.Cash++;
            Throws<ArgumentException>(() => EconomicV3PayloadCodec.SerializeChecked(payload));
            payload.Current.Cash--;
            payload.OriginalV2PayloadSha256 = new string('0', 64);
            Throws<ArgumentException>(() => EconomicV3PayloadCodec.SerializeChecked(payload));
        });
        Check("staged v2-to-v3 migration fails closed for unknown history ruleset and schema", () =>
        {
            var legacy = GameSession.NewState(FirstPlayableContentTemplate.BuildCatalog(),
                "v2-stage-unknown", 98767UL, new SimDate(2026, 9, 3));
            legacy.ContentVersion = "other-unknown-historical-catalog";
            Throws<ArgumentException>(() =>
                new V2ToV3Migration(new BundledHistoricalV2ContentResolver(),new VersionedReceiptReplay()).Migrate(JsonSaveSerializer.WriteObject(legacy)));
            legacy.ContentVersion = "first-playable.v1";
            legacy.SaveVersion = 3;
            Throws<ArgumentException>(() =>
                new V2ToV3Migration(new BundledHistoricalV2ContentResolver(),new VersionedReceiptReplay()).Migrate(JsonSaveSerializer.WriteObject(legacy)));
        });


        Check("frozen v2 catalog matches the original v2 authored fixture semantics", () =>
        {
            var resolver = new BundledHistoricalV2ContentResolver();
            Assert(!resolver.TryResolve("unknown-ruleset","first-playable.v1",out var absent1) &&
                absent1 == null,"unknown archived ruleset must fail closed");
            Assert(!resolver.TryResolve(BundledHistoricalV2ContentResolver.ArchiveId,
                "first-playable.v999",out var absent2) && absent2 == null,
                "unknown legacy content version must fail closed");
            Assert(resolver.TryResolve(BundledHistoricalV2ContentResolver.ArchiveId,
                BundledHistoricalV2ContentResolver.ContentVersion,out var original) &&
                original != null, "frozen authored resolver");
            var archived=original!;
            // The authored v3 template adds two modern businesses; compare only the
            // immutable v2-authored roster to the pinned historical catalog.
            var historicalAuthoring=FirstPlayableContentTemplate.Create();
            historicalAuthoring.Businesses = historicalAuthoring.Businesses.Where(x =>
                x.Id=="freelance-service" || x.Id=="coffee-kiosk").ToArray();
            var template=historicalAuthoring.Build();
            string Fingerprint(ContentCatalog c) => System.Text.Json.JsonSerializer.Serialize(new {
                c.Version,
                Schedule=new[]{c.Schedule.WakeMinute,c.Schedule.SleepMinute},
                Economy=new long[]{c.Economy.Payday,c.Economy.ExpenseDay,c.Economy.LivingCost},
                Skills=c.Skills.Values.OrderBy(x=>x.Id,StringComparer.Ordinal).Select(x=>new {x.Id,x.NameKey,Thresholds=x.Thresholds.ToArray()}).ToArray(),
                Careers=c.Careers.Values.OrderBy(x=>x.Id,StringComparer.Ordinal).Select(x=>new {
                    x.Id,x.Revision,x.NameKey,x.StartMinute,x.EndMinute,x.ScenesPerShift,x.QuotaSlots,
                    Days=x.WorkDays.Select(d=>(int)d).ToArray(),
                    Scenes=x.Scenes.Select(s=>new{s.Id,s.NameKey,s.SkillId,s.Weight,s.CareerXp,s.Exposure}).ToArray(),
                    Ranks=x.Ranks.Select(r=>new{r.Id,r.RequiredXp,r.RequiredServiceDays,r.MonthlySalary,r.GrantSkillId,r.GrantLevel}).ToArray()
                }).ToArray(),
                Courses=c.Courses.Values.OrderBy(x=>x.Id,StringComparer.Ordinal).Select(x=>new{
                    x.Id,x.NameKey,x.SkillId,x.TargetLevel,x.PrerequisiteLevel,x.BaseMinutes,x.Price}).ToArray(),
                Starts=c.Starts.Values.OrderBy(x=>x.Id,StringComparer.Ordinal).Select(x=>new{
                    x.Id,x.Cash,x.LearningSpeed,Appearances=x.AppearanceIds.ToArray()}).ToArray(),
                Businesses=c.Businesses.Values.OrderBy(x=>x.Id,StringComparer.Ordinal).Select(x=>new{
                    x.Id,x.Revision,x.NameKey,Type=(int)x.Type,Mode=(int)x.OperationMode,
                    x.MinimumStartupInvestment,x.MaximumStartupInvestment,x.MinimumReinvestment,x.MaximumReinvestment,
                    Pricing=x.AllowedPricingPostures.Select(z=>(int)z).ToArray(),
                    DefaultPricing=(int)x.DefaultPricingPosture,
                    OwnerMinutes=x.OperatingRequirements!.RequiredOwnerMinutes,
                    Windows=x.OperatingRequirements!.OperatingWindows.Select(w=>new{
                        Day=(int)w.DayOfWeek,w.StartMinute,w.EndMinute}).ToArray()
                }).ToArray()
            });
            Assert(Fingerprint(archived)==Fingerprint(template),"frozen content values differ from actual authored baseline/template");
            Assert(archived.Businesses.Count==2 &&
                archived.Businesses["freelance-service"].Revision=="v1" &&
                archived.Businesses["coffee-kiosk"].Revision=="v1",
                "legacy v2 businesses must not be overwritten by four modern MVP business profiles");
            Assert(archived.Businesses.Values.All(x=>x.MaximumStartupInvestment==1000),
                "frozen old capital bounds must remain v2");
        });


        Check("frozen v2 evaluator preserves immediate pricing and original command effects", () =>
        {
            var resolver = new BundledHistoricalV2ContentResolver();
            Assert(resolver.TryResolve(BundledHistoricalV2ContentResolver.ArchiveId,
                BundledHistoricalV2ContentResolver.ContentVersion,out var c) && c!=null,
                "missing historical engine content");
            var content = c!;
            var seed = GameSession.NewState(content,"archived-v2-parity",324154UL,new SimDate(2026,9,1));
            var bytes = JsonSaveSerializer.WriteObject(seed);
            var active = JsonSaveSerializer.ReadObject<GameState>(bytes);
            var historic = JsonSaveSerializer.ReadObject<GameState>(bytes);
            var normalEngine = new SimulationEngine(content);
            var frozenEngine = new ArchivedV2SimulationEngine(content);
            var create = new GameCommand(CommandKind.CreateCharacter,"fresh",25,
                "Archived Player","base.female");
            Assert(normalEngine.Evaluate(active,create,"archived-v2-parity/op/1") ==
                frozenEngine.Evaluate(historic,create,"archived-v2-parity/op/1"),
                "create differs");
            Assert(JsonSaveSerializer.WriteObject(active).SequenceEqual(JsonSaveSerializer.WriteObject(historic)),
                "v2 initial salary/living/character effects differ");
            var launch = new GameCommand(CommandKind.LaunchBusiness,"freelance-service",100,
                appearanceId:"v1");
            Assert(normalEngine.Evaluate(active,launch,"archived-v2-parity/op/2") ==
                frozenEngine.Evaluate(historic,launch,"archived-v2-parity/op/2"),
                "v2 launch differs");
            Assert(active.Businesses.Single().InstanceId==historic.Businesses.Single().InstanceId,
                "frozen v2 business identity differs");
            var price = new GameCommand(CommandKind.SetBusinessPricing,
                active.Businesses.Single().InstanceId,(int)PricingPosture.Premium);
            Assert(normalEngine.Evaluate(active,price,"archived-v2-parity/op/3") ==
                frozenEngine.Evaluate(historic,price,"archived-v2-parity/op/3"),
                "v2 pricing command differs");
            Assert(active.Businesses.Single().PricingPosture==PricingPosture.Premium &&
                historic.Businesses.Single().PricingPosture==PricingPosture.Premium,
                "historical v2 pricing MUST be immediate");
            Assert(JsonSaveSerializer.WriteObject(active).SequenceEqual(JsonSaveSerializer.WriteObject(historic)),
                "frozen evaluator and original evaluator results differ");
        });


        Check("versioned replay validates zero-effect v2 prefix against frozen evaluator", () =>
        {
            var historical = GameSession.NewState(FirstPlayableContentTemplate.BuildCatalog(),
                "archived-prefix-replay", 98768UL, new SimDate(2026,9,1));
            var oldBytes = JsonSaveSerializer.WriteObject(historical);
            var migrated = EconomicV3PayloadCodec.ReadChecked(
                new V2ToV3Migration(new BundledHistoricalV2ContentResolver(),new VersionedReceiptReplay()).Migrate(oldBytes));
            Assert(migrated.Current != null && migrated.Activation != null,"staged v3 body");
            var archive = new BundledHistoricalV2ContentResolver();
            var dispatcher = new VersionedReceiptReplay();
            dispatcher.Validate(JsonSaveSerializer.ReadObject<GameState>(oldBytes),
                migrated.Current!,migrated.Activation!.ToAnchor(),archive);
            var anchor = migrated.Activation.ToAnchor();
            var missing = new EconomicActivationAnchor(anchor.LegacyReceiptCount,
                anchor.LegacyLastOperationId,anchor.OriginalContentVersion,anchor.OriginalSourceSchema,
                anchor.MigratedAtDateIso,anchor.MigratedAtMinute,anchor.ActivationDateIso,
                "archive-not-installed",anchor.CutoverTransformVersion,
                anchor.SourceMidnightAlreadyProcessedLegacySettlement);
            Throws<ContentCompatibilityException>(()=>
                dispatcher.Validate(JsonSaveSerializer.ReadObject<GameState>(oldBytes),
                    migrated.Current!,missing,archive));
            // A fabricated v3 suffix must not be blessed without a v3 replay evaluator.
            migrated.Current!.Receipts.Add(new CommandReceipt {
                CommandId="fabricated",Payload="0:0:0:0:0",
                OperationId="archived-prefix-replay/op/1",Revision=1 });
            migrated.Current.Revision=1;
            Throws<NotSupportedException>(()=>
                dispatcher.Validate(JsonSaveSerializer.ReadObject<GameState>(oldBytes),
                    migrated.Current!,anchor,archive));
        });


        Check("typed v3 provenance preserves one sale / reservation / market unit / settlement", () =>
        {
            var graph = new V3FinancialProvenance();
            graph.MarketPools.Add(new V3MarketDayPool {
                DateIso="2026-10-09",PoolId="market-fixture",DefinitionRevision="v1",
                Segments=new List<V3PoolSegmentSupply>{new V3PoolSegmentSupply{
                    SegmentId="budget-sensitive",OriginalUnits=1
                }}
            });
            graph.CostReservations.Add(new V3CostReservation {
                Id="cost-1",DateIso="2026-10-09",EpochId="epoch-1",
                BusinessInstanceId="business-A",BusinessUnitOrdinal=0,AmountVnd=25,
                Status=V3CostReservationStatus.Consumed,SourceOperationId="operation-1"
            });
            graph.OperationSlices.Add(new V3BusinessOperationSlice {
                Id="slice-1",DateIso="2026-10-09",EpochId="epoch-1",
                BusinessInstanceId="business-A",StartMinute=1100,EndMinute=1200,
                CommittedOwnerMinuteDelta=100,UnitsDelta=1,VariablePaidVnd=25,
                SourceBoundaryOperationId="advance-1",
                FulfillmentIds=new List<string>{"sale-1"}
            });
            graph.Fulfillments.Add(new V3UnitFulfillment {
                Id="sale-1",DateIso="2026-10-09",SliceId="slice-1",
                EpochId="epoch-1",BusinessInstanceId="business-A",
                PoolId="market-fixture",SegmentId="budget-sensitive",
                MarketUnitOrdinal=0,BusinessUnitOrdinal=0,SourceCostReservationId="cost-1",
                UnitPriceVnd=80,VariablePaidVnd=25,ProfileRevision="v1",
                RulesetRevision="m9-t02.rules-v1",SourceBoundaryOperationId="advance-1"
            });
            graph.Settlements.Add(new V3BusinessDaySettlement {
                Id="run/business-day/2026-10-09",OldDateIso="2026-10-09",
                SourceBoundaryOperationId="advance-midnight",
                FulfillmentIds=new List<string>{"sale-1"},GrossVnd=80,
                VariablePaidVnd=25,FixedRecognizedVnd=10,ProfitVnd=45,
                AppliedToArrearsVnd=0,NetCashCreditVnd=80
            });
            graph.Validate();
            var record = new EconomicV3Records {
                CommittedEpochIds=new List<string>{"epoch-1"},
                CommittedSliceIds=new List<string>{"slice-1"},
                CommittedFulfillmentIds=new List<string>{"sale-1"},
                CommittedCostReservationIds=new List<string>{"cost-1"},
                SettledDayIds=new List<string>{"run/business-day/2026-10-09"},
                Provenance=graph,V3RecognizedRevenueVnd=80,V3PaidVariableVnd=25,
                V3RecognizedFixedVnd=10,V3NetBusinessCreditVnd=80
            };
            record.Validate();
            graph.Fulfillments[0].MarketUnitOrdinal=1;
            Throws<ArgumentException>(()=>graph.Validate());
            graph.Fulfillments[0].MarketUnitOrdinal=0;
            graph.CostReservations[0].Status=V3CostReservationStatus.Released;
            Throws<ArgumentException>(()=>graph.Validate());
            graph.CostReservations[0].Status=V3CostReservationStatus.Consumed;
            graph.Settlements[0].NetCashCreditVnd=79;
            Throws<ArgumentException>(()=>graph.Validate());
            graph.Settlements[0].NetCashCreditVnd=80;
            graph.OperationSlices[0].FulfillmentIds.Clear();
            Throws<ArgumentException>(()=>graph.Validate());
            graph.OperationSlices[0].FulfillmentIds.Add("sale-1");
            graph.Validate();
        });


        Check("v1 source checkpoint migrates through 1-to-2-to-3 with original schema retained", () =>
        {
            var catalog = FirstPlayableContentTemplate.BuildCatalog();
            var original = GameSession.NewState(catalog,"schema-one-origin",27182UL,
                new SimDate(2026,9,1));
            var serializer = new JsonSaveSerializer(catalog,GameSession.CreateRestoreValidator(),
                new SyntheticV0Migration(),new V1ToV2Migration());
            var v1Wire = serializer.SerializeForSchema(original, SaveSchema.HistoricalV1);
            var envelope = JsonSaveSerializer.ReadObject<SaveEnvelope>(v1Wire);
            var v1Bytes = Convert.FromBase64String(envelope.PayloadBase64);
            var stage2Bytes = new V1ToV2Migration().Migrate(v1Bytes);
            var migration = new V2ToV3Migration(
                new BundledHistoricalV2ContentResolver(),new VersionedReceiptReplay());
            var v3 = EconomicV3PayloadCodec.ReadChecked(
                migration.MigrateFromOriginalSource(stage2Bytes,1,v1Bytes));
            Assert(v3.Activation != null && v3.Activation.OriginalSourceSchema==1 &&
                v3.OriginalSourceSchema==1,
                "v1 original schema lost at version-3 anchor");
            Assert(Convert.FromBase64String(v3.OriginalSourcePayloadBase64).SequenceEqual(v1Bytes),
                "v1 bytes were not retained");
            Assert(Convert.FromBase64String(v3.OriginalV2PayloadBase64).SequenceEqual(stage2Bytes),
                "normalized stage2 bytes were not retained");
            var invalid = (byte[])stage2Bytes.Clone();
            invalid[invalid.Length-1] ^= 1;
            Throws<ArgumentException>(()=>migration.MigrateFromOriginalSource(invalid,1,v1Bytes));
        });
        Check("synthetic v0 source migrates through each adjacent stage with source0 anchor", () =>
        {
            var legacy = GameSession.NewState(FirstPlayableContentTemplate.BuildCatalog(),
                "schema-zero-origin",27183UL,new SimDate(2026,9,1));
            legacy.SaveVersion=0;
            var syntheticWire=JsonSaveSerializer.WriteObject(legacy);
            var v1=new SyntheticV0Migration().Migrate(syntheticWire);
            var v2=new V1ToV2Migration().Migrate(v1);
            var migration=new V2ToV3Migration(new BundledHistoricalV2ContentResolver(),
                new VersionedReceiptReplay());
            var payload=EconomicV3PayloadCodec.ReadChecked(
                migration.MigrateFromOriginalSource(v2,0,syntheticWire));
            Assert(payload.OriginalSourceSchema==0 &&
                payload.Activation!.OriginalSourceSchema==0,
                "synthetic original schema must be recorded as zero");
            Assert(Convert.FromBase64String(payload.OriginalSourcePayloadBase64).SequenceEqual(syntheticWire),
                "source-zero wire bytes lost");
            Throws<ArgumentException>(()=>migration.MigrateFromOriginalSource(v2,2,syntheticWire));
        });


        Check("M15 staged V4 migration retains verified V3/V2 bytes and restores pending choice", () =>
        {
            void Equal<T>(T expected, T actual) => Assert(EqualityComparer<T>.Default.Equals(expected, actual),
                "Expected " + expected + "; got " + actual);
            void True(bool condition) => Assert(condition, "M15 expected true");
            void Bytes(byte[] expected, byte[] actual) => Assert(expected.SequenceEqual(actual), "M15 checkpoint bytes differ");
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var resolver = new BundledHistoricalV2ContentResolver();
            var replay = new VersionedReceiptReplay();
            var v3 = new V3SaveCompatibilitySerializer(content, GameSession.CreateRestoreValidator(),
                new V2ToV3Migration(resolver, replay),
                new StagedV3SaveSerializer(resolver, replay));
            var definitions = new[] { new GameEventDefinition("career-intro", "career", 2026, 0, 1, 0, 5, 4, 0,
                Array.Empty<string>(), Array.Empty<string>(),
                new[] { new EventChoiceDefinition("continue", "event.continue", "outcome.continue") }) };
            var policies = new[] { new EventCategoryPolicy("career", 0, 7, 2) };
            var stage = new StagedEventV4SaveSerializer(v3, definitions, policies);
            var original = GameSession.NewState(content, "run-v4-migration", 17483UL,
                new SimDate(2026, 10, 1));
            var v2Bytes = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(original), 2, 0);
            var unchangedV2 = v2Bytes.ToArray();
            var migrated = stage.MigrateVerified(v2Bytes);
            Equal(LoadStatus.Valid, migrated.Status);
            True(migrated.Payload != null && migrated.CoreState != null);
            Equal(0L, migrated.Payload!.Journal!.Revision);
            var pinnedCatalog = EventCatalogFingerprint.Compute(definitions, policies);
            Equal(pinnedCatalog, migrated.Payload.Journal.CatalogFingerprint);
            Equal(original.EventRng, migrated.Payload.Journal.InitialEventRng);
            Bytes(unchangedV2, v2Bytes);
            var v3Bytes = Convert.FromBase64String(migrated.Payload.EmbeddedV3EnvelopeBase64);
            Equal(3, JsonSaveSerializer.ReadObject<SaveEnvelope>(v3Bytes).SchemaVersion);
            Equal(LoadStatus.Valid, v3.DeserializeAndValidate(v3Bytes).Status);
            Equal(LoadStatus.Valid, stage.MigrateVerified(v3Bytes).Status);
            var context = new GameEventContext(original.Date, original.Date, 0,
                Array.Empty<string>(), Array.Empty<EventOccurrence>());
            var offer = EventDecisionJournal.Offer(migrated.Payload.Journal, definitions, policies,
                context, 0, "migration-offer", migrated.CoreState!.Revision);
            Equal(EventJournalStatus.Offered, offer.Status);
            migrated.Payload.Journal = offer.Next;
            var v4Bytes = stage.SerializeVerified(migrated.Payload);
            // An updated authoring roster without the pinned revision must
            // fail as unsupported content, not silently select a new event.
            var rebalanced = new[] { new GameEventDefinition("career-intro", "career",
                2026, 0, 2, 0, 5, 4, 0, Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { new EventChoiceDefinition("continue",
                    "event.continue", "outcome.continue") }) };
            var newCatalog = new StagedEventV4SaveSerializer(v3, rebalanced, policies);
            Equal(LoadStatus.UnsupportedContent, newCatalog.DeserializeVerified(v4Bytes).Status);
            var persistedFingerprint = JsonSaveSerializer.ReadObject<EventV4Payload>(
                JsonSaveSerializer.WriteObject(migrated.Payload));
            persistedFingerprint.Journal!.CatalogFingerprint =
                EventCatalogFingerprint.Prefix + new string('0', EventCatalogFingerprint.HexLength);
            var mismatchedWire = JsonSaveSerializer.Wrap(
                JsonSaveSerializer.WriteObject(persistedFingerprint), 4, 1);
            Equal(LoadStatus.UnsupportedContent, stage.DeserializeVerified(mismatchedWire).Status);
            // Recreate a byte-authentic early V4 *wire shape* with the new
            // optional member omitted, without weakening current V4 replay.
            // Generate fresh envelope checksum instead of faking disk bytes.
            byte[] LegacyDailyShape(bool explicitNull)
            {
                var envelope = JsonSaveSerializer.ReadObject<SaveEnvelope>(v4Bytes);
                using var doc = JsonDocument.Parse(
                    Convert.FromBase64String(envelope.PayloadBase64));
                using var stream = new MemoryStream();
                var removed = false;
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();
                    foreach (var field in doc.RootElement.EnumerateObject())
                    {
                        if (field.Name != "Journal")
                        {
                            field.WriteTo(writer);
                            continue;
                        }
                        writer.WritePropertyName(field.Name);
                        writer.WriteStartObject();
                        foreach (var member in field.Value.EnumerateObject())
                        {
                            if (member.Name == "CatalogFingerprint")
                            {
                                // Earlier schema-4 checkpoints did not carry
                                // a catalog pin; preserve that exact old shape.
                                continue;
                            }
                            if (member.Name == "NoEligibleDays")
                            {
                                removed = true;
                                if (explicitNull)
                                    writer.WriteNull(member.Name);
                            }
                            else member.WriteTo(writer);
                        }
                        writer.WriteEndObject();
                    }
                    writer.WriteEndObject();
                }
                True(removed);
                return JsonSaveSerializer.Wrap(stream.ToArray(), 4, envelope.Generation);
            }
            var legacyV4Bytes = LegacyDailyShape(false);
            var legacyRead = stage.DeserializeVerified(legacyV4Bytes);
            Equal(LoadStatus.Valid, legacyRead.Status);
            Equal(0, legacyRead.Payload!.Journal!.NoEligibleDays.Count);
            Equal("", legacyRead.Payload.Journal.CatalogFingerprint);
            Equal(offer.TicketId, legacyRead.Payload.Journal.PendingTicketId);
            Equal(LoadStatus.Corrupt,
                stage.DeserializeVerified(LegacyDailyShape(true)).Status);
            // Cold read of the previous V4 layout must not eagerly rewrite a
            // valid primary save; the first real CAS commit promotes its shape.
            var folder = Path.Combine(Path.GetTempPath(),
                "m15-v4-legacy-daily-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var filename = Path.Combine(folder, "game.save");
            try
            {
                File.WriteAllBytes(filename, legacyV4Bytes);
                var activeSerializer = new EventV4CompatibilitySerializer(stage);
                EventV4CommitGateway Cold() => new EventV4CommitGateway(activeSerializer,
                    new AtomicFileSaveStore(filename, activeSerializer),
                    definitions, policies);
                var beforeRead = File.ReadAllBytes(filename);
                var pending = Cold().ReadPendingChoice();
                True(pending != null && pending.TicketId == offer.TicketId &&
                    pending.Revision == 1);
                Bytes(beforeRead, File.ReadAllBytes(filename));
                var accepted = Cold().Resolve(original.RunId, "legacy-daily-resolve",
                    pending!.Revision, pending.TicketId, "continue");
                Equal(EventCommitStatus.Committed, accepted.Status);
                var latest = Cold().Read().State!;
                Equal(2L, latest.Revision);
                Equal(1, latest.EventV4!.Journal!.OutcomeReceipts.Count);
                Equal(0, latest.EventV4.Journal.NoEligibleDays.Count);
                True(!File.ReadAllBytes(filename).SequenceEqual(beforeRead));
                var promoted = JsonSaveSerializer.ReadObject<SaveEnvelope>(
                    File.ReadAllBytes(filename));
                using var promotedJson = JsonDocument.Parse(
                    Convert.FromBase64String(promoted.PayloadBase64));
                True(promotedJson.RootElement.GetProperty("Journal")
                    .TryGetProperty("NoEligibleDays", out _));
                var promotedBytes = File.ReadAllBytes(filename);
                Equal(EventCommitStatus.AlreadyCommitted,
                    Cold().Resolve(original.RunId, "legacy-daily-resolve",
                        pending.Revision, pending.TicketId, "continue").Status);
                Bytes(promotedBytes, File.ReadAllBytes(filename));
            }
            finally
            {
                try { Directory.Delete(folder, true); }
                catch (IOException) { }
            }
            var restored = stage.DeserializeVerified(v4Bytes);
            Equal(LoadStatus.Valid, restored.Status);
            Equal(offer.TicketId, restored.Payload!.Journal!.PendingTicketId);
            Equal(4, JsonSaveSerializer.ReadObject<SaveEnvelope>(v4Bytes).SchemaVersion);
            var resume = EventDecisionJournal.Resolve(restored.Payload.Journal, definitions, policies,
                restored.Payload.Journal.Revision, "v4-choice", offer.TicketId, "continue");
            Equal(EventJournalStatus.Resolved, resume.Status);
            restored.Payload.Journal = resume.Next;
            var committedChoiceBytes = stage.SerializeVerified(restored.Payload);
            Equal(LoadStatus.Valid, stage.DeserializeVerified(committedChoiceBytes).Status);
            Equal(LoadStatus.Valid, stage.MigrateVerified(committedChoiceBytes).Status);
            var falseJournal = JsonSaveSerializer.ReadObject<EventV4Payload>(
                JsonSaveSerializer.WriteObject(restored.Payload));
            falseJournal.Journal!.EventRng++;
            Throws<ArgumentException>(() => stage.SerializeVerified(falseJournal));
            var falseV3 = JsonSaveSerializer.ReadObject<EventV4Payload>(
                JsonSaveSerializer.WriteObject(restored.Payload));
            falseV3.EmbeddedV3EnvelopeSha256 = "bad-sha";
            Throws<ArgumentException>(() => stage.SerializeVerified(falseV3));
            var wire = JsonSaveSerializer.ReadObject<SaveEnvelope>(committedChoiceBytes);
            wire.Generation++;
            Equal(LoadStatus.Corrupt, stage.DeserializeVerified(JsonSaveSerializer.WriteObject(wire)).Status);
            wire.SchemaVersion = 5;
            wire.PayloadBase64 = "!invalid!";
            Equal(LoadStatus.FutureVersion, stage.DeserializeVerified(JsonSaveSerializer.WriteObject(wire)).Status);
        });


        Check("M15 authored tag graph refuses mutually exclusive one-off prerequisites", () =>
        {
            GameEventDefinition Fork(int maxOccurrences) => new GameEventDefinition(
                "branch-choice", "life", 2026, 0, 1, 0, maxOccurrences, 1, 0,
                Array.Empty<string>(), Array.Empty<string>(),
                new[]
                {
                    new EventChoiceDefinition("left", "event.branch.left", "branch-left",
                        new[] { "event/branch-left" }),
                    new EventChoiceDefinition("right", "event.branch.right", "branch-right",
                        new[] { "event/branch-right" })
                });
            var impossibleJoin = new GameEventDefinition(
                "branch-join", "life", 2026, 0, 1, 0, 1, 1, 0,
                new[] { "event/branch-left", "event/branch-right" },
                Array.Empty<string>(),
                new[] { new EventChoiceDefinition("continue",
                    "event.join.continue", "join-continue") });
            Throws<InvalidOperationException>(() =>
                FirstPlayableEventContent.ValidateAuthoredPaths(
                    new[] { Fork(1), impossibleJoin }));
            // A repeatable source can grant the two tags across distinct
            // occurrences, so its authored dependency is not contradictory.
            FirstPlayableEventContent.ValidateAuthoredPaths(
                new[] { Fork(2), impossibleJoin });

            var forcedExpense = new GameEventDefinition(
                "forced-expense", "life", 2026, 0, 1, 0, 1, 1, 0,
                Array.Empty<string>(), Array.Empty<string>(),
                new[]
                {
                    new EventChoiceDefinition("first", "event.paid.first", "paid-first",
                        reward: new EventChoiceReward(-100, 0)),
                    new EventChoiceDefinition("second", "event.paid.second", "paid-second",
                        reward: new EventChoiceReward(-200, 0))
                });
            Throws<InvalidOperationException>(() =>
                FirstPlayableEventContent.ValidateAuthoredPaths(new[] { forcedExpense }));
            FirstPlayableEventContent.ValidateAuthoredPaths(
                FirstPlayableEventContent.Definitions);
        });
        Check("M15 active-course opportunity unlocks a free follow-up without fake study time", () =>
        {
            var courseStory = FirstPlayableEventContent.Definitions.Single(x =>
                x.Id == "course-study-questions");
            var followUp = FirstPlayableEventContent.Definitions.Single(x =>
                x.Id == "course-question-follow-up");
            Assert(courseStory.CategoryId == "study" &&
                courseStory.RequiredTags.SequenceEqual(new[] { "course-active" }) &&
                followUp.CategoryId == "study" &&
                followUp.RequiredTags.SequenceEqual(new[] { "event/course-questions-prepared" }),
                "a course event must use the V3 course eligibility fact and only real event-tag receipts");
            var prepare = courseStory.Choices.Single(x => x.Id == "prepare");
            var decline = courseStory.Choices.Single(x => x.Id == "review");
            Assert(prepare.Reward == null && decline.Reward == null &&
                prepare.GrantedTags.Contains("event/course-questions-prepared") &&
                !decline.GrantedTags.Contains("event/course-questions-prepared") &&
                followUp.Choices.All(choice => choice.Reward == null),
                "course narrative choices must not mint XP, unreceipted minutes, or course progress");
            var context = new GameEventContext(new SimDate(2026, 10, 1),
                new SimDate(2026, 10, 8), 1,
                new[] { "character-created", "course-active" },
                Array.Empty<EventOccurrence>());
            Assert(GameEventSelector.IsEligible(courseStory, context) &&
                !GameEventSelector.IsEligible(followUp, context),
                "the follow-up is gated on an accepted choice, not arbitrary active-course state");
            var nextContext = new GameEventContext(new SimDate(2026, 10, 1),
                new SimDate(2026, 10, 8), 1,
                new[] { "character-created", "event/course-questions-prepared" },
                Array.Empty<EventOccurrence>());
            Assert(GameEventSelector.IsEligible(followUp, nextContext),
                "a committed follow-up tag survives the V3 course completing later");
            FirstPlayableEventContent.ValidateAuthoredPaths(
                FirstPlayableEventContent.Definitions);
        });
        Check("M15 retained pinned development catalogs survive later authoring revisions", () =>
        {
            var baseline = ReleasedFirstPlayableEventCatalog.LegacyBaseline;
            var retained = RetainedM15DevelopmentEventCatalogs.Create(baseline);
            var expectedCounts = new[] { 22, 23, 26, 29, 33, 35 };
            Assert(retained.Count == expectedCounts.Length,
                "six pinned development fingerprints must remain independently resolvable");
            var archive = ReleasedFirstPlayableEventCatalog.CreateArchive(
                FirstPlayableEventContent.Definitions, FirstPlayableEventContent.Policies);
            var fingerprints = new HashSet<string>(StringComparer.Ordinal) { baseline.Fingerprint };
            for (var i = 0; i < retained.Count; i++)
            {
                var version = retained[i];
                Assert(version.Definitions.Count == expectedCounts[i] &&
                    version.Definitions.Sum(e => e.Choices.Count) == expectedCounts[i] * 2 &&
                    version.VietnameseText.Count == expectedCounts[i] * 4 &&
                    fingerprints.Add(version.Fingerprint),
                    "retained M15 snapshot must keep its original data and unique identity");
                var pinned = EventDecisionJournal.New("retained-development", 17);
                pinned.CatalogFingerprint = version.Fingerprint;
                var resolved = archive.Resolve(pinned);
                Assert(ReferenceEquals(resolved, version) == false &&
                    resolved.Fingerprint == version.Fingerprint &&
                    resolved.VietnameseText.Count == version.VietnameseText.Count &&
                    resolved.Definitions.Count == expectedCounts[i],
                    "bundled historical snapshot must resolve despite newer current roster");
                Assert(resolved.VietnameseText.TryGetValue(
                    "event.story.community-starter-grant.title", out var oldTitle) &&
                    oldTitle == "Khoản hỗ trợ thử ý tưởng",
                    "historical Vietnamese narrative must not use mutable live StringTables");
            }
            Assert(fingerprints.Add(archive.Current.Fingerprint),
                "36-story current roster must have a distinct content fingerprint");
            var oldCurrent = retained[1];
            var restored = new EventCatalogArchive(
                new EventCatalogSnapshot(oldCurrent.Definitions, oldCurrent.Policies),
                new[] { baseline, oldCurrent }, legacyBaseline: baseline);
            var journal = EventDecisionJournal.New("identical-rules-archive", 19);
            journal.CatalogFingerprint = oldCurrent.Fingerprint;
            Assert(ReferenceEquals(restored.Resolve(journal), oldCurrent) &&
                restored.Resolve(journal).VietnameseText.Count == 92,
                "a frozen roster must take precedence over mutable current texts with matching rules");
            journal.CatalogFingerprint = "";
            Assert(ReferenceEquals(restored.Resolve(journal), baseline),
                "unpinned legacy V4 runs must still resolve only to their declared first release");
        });
        Check("M15 bundled first-playable V4 roster remains immutable across catalog upgrades", () =>
        {
            var baseline = ReleasedFirstPlayableEventCatalog.LegacyBaseline;
            Assert(baseline.Definitions.Count == 20 &&
                baseline.Definitions.Sum(x => x.Choices.Count) == 40 &&
                baseline.Policies.Count == 4,
                "released V4 baseline must retain all 20 historical events and 40 choices");
            var current = ReleasedFirstPlayableEventCatalog.CreateArchive(
                FirstPlayableEventContent.Definitions, FirstPlayableEventContent.Policies);
            var original = EventCatalogFingerprint.Compute(
                baseline.Definitions, baseline.Policies);
            var currentFingerprint = EventCatalogFingerprint.Compute(
                FirstPlayableEventContent.Definitions, FirstPlayableEventContent.Policies);
            Assert(baseline.Fingerprint == original &&
                current.LegacyBaseline.Fingerprint == original &&
                current.Current.Fingerprint == currentFingerprint &&
                currentFingerprint != original,
                "current authored content may evolve but published first-playable V4 data must not");
            var rewarded = FirstPlayableEventContent.Definitions.SelectMany(x => x.Choices)
                .Where(x => x.Reward != null).ToArray();
            var compound = FirstPlayableEventContent.Definitions.Single(x =>
                x.Id == "mentor-workshop-materials").Choices.Single(x =>
                x.Id == "contribute").Reward;
            Assert(FirstPlayableEventContent.Definitions.Count == 36 &&
                rewarded.Length == 9 &&
                rewarded.Any(x => x.Reward!.CashDeltaVnd == 100000L &&
                    x.Reward.CareerXpDelta == 0) &&
                rewarded.Any(x => x.Reward!.CashDeltaVnd == 0 &&
                    x.Reward.CareerXpDelta == 60L) &&
                rewarded.Any(x => x.Reward!.CashDeltaVnd == -40000L &&
                    x.Reward.CareerXpDelta == 0) &&
                compound != null && compound.CashDeltaVnd == -25000L &&
                compound.CareerXpDelta == 45 &&
                rewarded.Any(x => x.Reward!.CashDeltaVnd == -70000L &&
                    x.Reward.CareerXpDelta == 0) &&
                rewarded.Any(x => x.Reward!.CashDeltaVnd == -55000L &&
                    x.Reward.CareerXpDelta == 0) &&
                rewarded.Any(x => x.Reward!.CashDeltaVnd == -90000L &&
                    x.Reward.CareerXpDelta == 0) &&
                rewarded.Any(x => x.Reward!.CashDeltaVnd == -120000L &&
                    x.Reward.CareerXpDelta == 0) &&
                rewarded.Any(x => x.Reward!.MinutesConsumed == 60 &&
                    x.Reward.CareerXpDelta == 30 && x.Reward.CashDeltaVnd == 0) &&
                FirstPlayableEventContent.Definitions.All(x =>
                    x.Choices.Any(choice => choice.Reward == null)),
                "all authored financial/XP choices retain a safe tag-only exit");
            var evolved = FirstPlayableEventContent.Definitions.ToArray();
            var first = evolved[0];
            evolved[0] = new GameEventDefinition(first.Id, first.CategoryId,
                first.MinimumYear, first.MinimumStage, first.Weight + 1,
                first.CooldownDays, first.MaxOccurrences, first.WindowPeriodDays,
                first.WindowOffsetDays, first.RequiredTags, first.ForbiddenTags,
                first.Choices, first.TitleKey, first.BodyKey);
            var archive = ReleasedFirstPlayableEventCatalog.CreateArchive(
                evolved, FirstPlayableEventContent.Policies);
            Assert(archive.Current.Fingerprint != baseline.Fingerprint &&
                archive.LegacyBaseline.Fingerprint == original,
                "a rebalanced catalog must not mutate the archived released roster");
            var oldPinned = EventDecisionJournal.New("first-playable-cold", 17);
            oldPinned.CatalogFingerprint = original;
            Assert(archive.Resolve(oldPinned).Fingerprint == original,
                "pinned old first-playable checkpoint must resolve from frozen snapshot");
            oldPinned.CatalogFingerprint = "";
            Assert(archive.Resolve(oldPinned).Fingerprint == original,
                "pre-fingerprint V4 checkpoint must resolve only declared frozen baseline");
            oldPinned.CatalogFingerprint = archive.Current.Fingerprint;
            Assert(archive.Resolve(oldPinned).Fingerprint == archive.Current.Fingerprint,
                "newly pinned V4 checkpoint must resolve to current catalog");
        });
        Check("M15 archived event catalog replays and continues pinned and legacy V4 saves", () =>
        {
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var history = new BundledHistoricalV2ContentResolver();
            var coreReplay = new VersionedReceiptReplay();
            var v3 = new V3SaveCompatibilitySerializer(content, GameSession.CreateRestoreValidator(),
                new V2ToV3Migration(history, coreReplay),
                new StagedV3SaveSerializer(history, coreReplay));
            var policy = new[] { new EventCategoryPolicy("life", 0, 7, 3) };
            var oldDefinitions = new[] { new GameEventDefinition("archived-story", "life",
                2026, 0, 1, 0, 4, 7, 0, Array.Empty<string>(), Array.Empty<string>(),
                new[] { new EventChoiceDefinition("accept", "event.old.accept", "outcome.accept") },
                "event.old.title", "event.old.body") };
            var currentDefinitions = new[] { new GameEventDefinition("archived-story", "life",
                2026, 0, 3, 0, 4, 7, 0, Array.Empty<string>(), Array.Empty<string>(),
                new[] { new EventChoiceDefinition("accept", "event.new.accept", "outcome.accept") },
                "event.new.title", "event.new.body") };
            var old = new EventCatalogSnapshot(oldDefinitions, policy);
            var currentCatalog = new EventCatalogSnapshot(currentDefinitions, policy);
            Assert(old.Fingerprint != currentCatalog.Fingerprint,
                "a different event weight or narrative must change the catalog identity");
            Throws<ArgumentException>(() => new EventCatalogArchive(currentCatalog,
                new[] { old }));
            var archive = new EventCatalogArchive(currentCatalog, new[] { old },
                legacyBaseline: old);
            var stage = new StagedEventV4SaveSerializer(v3, archive);
            var active = new EventV4CompatibilitySerializer(stage);
            var oldStage = new StagedEventV4SaveSerializer(v3, oldDefinitions, policy);
            var initial = GameSession.NewState(content, "m15-archive-run", 314159UL,
                new SimDate(2026, 10, 1));
            var v2Bytes = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(initial), 2, 0);
            var oldMigrated = oldStage.MigrateVerified(v2Bytes);
            Assert(oldMigrated.Status == LoadStatus.Valid && oldMigrated.Payload?.Journal != null,
                "old V3-to-V4 migration is required");
            var offer = EventDecisionJournal.Offer(oldMigrated.Payload!.Journal!,
                oldDefinitions, policy,
                new GameEventContext(initial.Date, initial.Date, 0,
                    Array.Empty<string>(), Array.Empty<EventOccurrence>()),
                0, "archive-offer", oldMigrated.CoreState!.Revision);
            Assert(offer.Status == EventJournalStatus.Offered,
                "old roster should have created a pending event");
            oldMigrated.Payload.Journal = offer.Next;
            var oldWire = oldStage.SerializeVerified(oldMigrated.Payload);
            Assert(stage.DeserializeVerified(oldWire).Status == LoadStatus.Valid &&
                new StagedEventV4SaveSerializer(v3, currentDefinitions, policy)
                    .DeserializeVerified(oldWire).Status == LoadStatus.UnsupportedContent,
                "archived catalog must restore a run refused by current content alone");

            var folder = Path.Combine(Path.GetTempPath(),
                "m15-event-archive-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, "game.save");
            try
            {
                File.WriteAllBytes(path, oldWire);
                EventV4CommitGateway Cold() => new EventV4CommitGateway(active,
                    new AtomicFileSaveStore(path, active), currentDefinitions, policy);
                var pending = Cold().ReadPendingChoice();
                Assert(pending != null && pending.TitleKey == "event.old.title" &&
                    pending.BodyKey == "event.old.body" &&
                    pending.Choices.Single().LabelKey == "event.old.accept",
                    "archived UI choice must not present newly authored option text");
                var bytes = File.ReadAllBytes(path);
                Assert(Cold().Read().Status == LoadStatus.Valid &&
                    File.ReadAllBytes(path).SequenceEqual(bytes),
                    "cold restore of pinned historical checkpoint must remain read-only");
                var result = Cold().Resolve(initial.RunId, "archive-resolve",
                    pending!.Revision, pending.TicketId, "accept");
                Assert(result.Status == EventCommitStatus.Committed &&
                    Cold().Read().Status == LoadStatus.Valid &&
                    Cold().Read().State!.EventV4!.Journal!.CatalogFingerprint == old.Fingerprint,
                    "historical resolution must commit with old catalog and retain its fingerprint");
                var committed = File.ReadAllBytes(path);
                Assert(Cold().Resolve(initial.RunId, "archive-resolve",
                    pending.Revision, pending.TicketId, "accept").Status ==
                    EventCommitStatus.AlreadyCommitted &&
                    committed.SequenceEqual(File.ReadAllBytes(path)),
                    "historical resolution retry may not rewrite the checkpoint");

                // Explicit legacy baseline applies ONLY to earlier V4 wires that
                // contain no fingerprint. Never guess based on saved event IDs.
                var envelope = JsonSaveSerializer.ReadObject<SaveEnvelope>(oldWire);
                using var document = JsonDocument.Parse(
                    Convert.FromBase64String(envelope.PayloadBase64));
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();
                    foreach (var field in document.RootElement.EnumerateObject())
                    {
                        if (field.Name != "Journal") { field.WriteTo(writer); continue; }
                        writer.WritePropertyName(field.Name);
                        writer.WriteStartObject();
                        foreach (var member in field.Value.EnumerateObject())
                            if (member.Name != "CatalogFingerprint") member.WriteTo(writer);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndObject();
                }
                var legacyWire = JsonSaveSerializer.Wrap(stream.ToArray(), 4,
                    envelope.Generation);
                File.WriteAllBytes(path, legacyWire);
                Assert(Cold().Read().Status == LoadStatus.Valid &&
                    Cold().Read().State!.EventV4!.Journal!.CatalogFingerprint == "" &&
                    File.ReadAllBytes(path).SequenceEqual(legacyWire),
                    "explicit baseline restores unpinned V4 without rewriting source");
                var legacyPending = Cold().ReadPendingChoice();
                Assert(legacyPending != null &&
                    legacyPending.TitleKey == "event.old.title",
                    "unpinned legacy choice must use declared historical baseline");
                var legacyResolved = Cold().Resolve(initial.RunId, "archive-legacy-resolve",
                    legacyPending!.Revision, legacyPending.TicketId, "accept");
                Assert(legacyResolved.Status == EventCommitStatus.Committed &&
                    Cold().Read().State!.EventV4!.Journal!.CatalogFingerprint == "",
                    "legacy CAS continuation must retain the unpinned roster designation");
                var promotedLegacy = File.ReadAllBytes(path);
                Assert(Cold().Resolve(initial.RunId, "archive-legacy-resolve",
                    legacyPending.Revision, legacyPending.TicketId, "accept").Status ==
                    EventCommitStatus.AlreadyCommitted &&
                    promotedLegacy.SequenceEqual(File.ReadAllBytes(path)),
                    "legacy retry must not change original outcome");

                // A brand-new run always pins CURRENT, never the archival baseline.
                var newMigration = stage.MigrateVerified(v2Bytes);
                Assert(newMigration.Status == LoadStatus.Valid &&
                    newMigration.Payload?.Journal?.CatalogFingerprint ==
                    currentCatalog.Fingerprint,
                    "new V4 runs must use current authored roster");
            }
            finally
            {
                try { Directory.Delete(folder, true); }
                catch (IOException) { }
            }
        });

        Check("M15 authored Vietnamese event stories survive verified V4 CAS and cold replay", () =>
        {
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var econ = M9T02FunctionalEconomy.Create();
            var archives = new BundledHistoricalV2ContentResolver();
            var replay = new VersionedReceiptReplay(econ, content);
            var v3 = new V3SaveCompatibilitySerializer(content,
                GameSession.CreateRestoreValidator(), new V2ToV3Migration(archives, replay),
                new StagedV3SaveSerializer(archives, replay));
            var definitions = FirstPlayableEventContent.Definitions;
            var policies = FirstPlayableEventContent.Policies;
            var serializer = new EventV4CompatibilitySerializer(
                new StagedEventV4SaveSerializer(v3, definitions, policies));
            var runner = new EventV4CoreCommandRunner(content, v3, econ, archives);
            var original = GameSession.NewState(content, "run-v4-authored", 1977UL,
                new SimDate(2026, 10, 12));
            var folder = Path.Combine(Path.GetTempPath(),
                "m15-authored-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var filename = Path.Combine(folder, "game.save");
            try
            {
                File.WriteAllBytes(filename, JsonSaveSerializer.Wrap(
                    JsonSaveSerializer.WriteObject(original), 2, 0));
                EventV4CommitGateway Cold() => new EventV4CommitGateway(serializer,
                    new AtomicFileSaveStore(filename, serializer),
                    definitions, policies, runner);
                var gateway = Cold();
                var created = gateway.ExecuteGameplay(new CommandEnvelope(
                    original.RunId, "authored-create", 0,
                    new GameCommand(CommandKind.CreateCharacter, "fresh", 22,
                        "Người chơi", "base.female")));
                Assert(created.Status == CommandStatus.Committed,
                    "actual V3 character receipt must commit in V4");
                var offer = gateway.Offer(original.RunId, "authored-offer", created.Revision);
                Assert(offer.Status == EventCommitStatus.Committed,
                    "authored event catalog should offer a deterministic eligible story");
                var pending = Cold().ReadPendingChoice();
                Assert(pending != null && pending.TitleKey.StartsWith("event.story.",
                    StringComparison.Ordinal) && pending.BodyKey.StartsWith("event.story.",
                    StringComparison.Ordinal) && pending.Choices.Count >= 2,
                    "cold UI read must expose actual authored title, body and choices");
                var translated = new HashSet<string>(
                    FirstPlayableEventContent.VietnameseEntries.Select(x => x.Key),
                    StringComparer.Ordinal);
                Assert(translated.Contains(pending!.TitleKey) &&
                    translated.Contains(pending.BodyKey) &&
                    pending.Choices.All(x => translated.Contains(x.LabelKey)),
                    "authored pending event requires every localized key");
                var chosen = pending.Choices[0];
                var resolved = Cold().Resolve(original.RunId, "authored-resolve",
                    pending.Revision, pending.TicketId, chosen.Id);
                Assert(resolved.Status == EventCommitStatus.Committed &&
                    Cold().ReadPendingChoice() == null,
                    "authored decision must commit once and clear pending state");
                var repeat = Cold().Resolve(original.RunId, "authored-resolve",
                    pending.Revision, pending.TicketId, chosen.Id);
                Assert(repeat.Status == EventCommitStatus.AlreadyCommitted &&
                    Cold().Read().Status == LoadStatus.Valid,
                    "cold replay and duplicate request must remain exactly-once");
            }
            finally { Directory.Delete(folder, true); }
        });

        Check("M15 V4 durable day/month batches resume across ambiguous CAS and event choices", () =>
        {
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var econ = M9T02FunctionalEconomy.Create();
            var archives = new BundledHistoricalV2ContentResolver();
            var replay = new VersionedReceiptReplay(econ, content);
            var v3 = new V3SaveCompatibilitySerializer(content,
                GameSession.CreateRestoreValidator(), new V2ToV3Migration(archives, replay),
                new StagedV3SaveSerializer(archives, replay));
            var definitions = new[] { new GameEventDefinition(
                "batch-interruption", "life", 2026, 1, 1, 0, 1, 1, 0,
                Array.Empty<string>(), Array.Empty<string>(),
                new[] { new EventChoiceDefinition("ack", "event.ack", "outcome.ack") }) };
            var policies = new[] { new EventCategoryPolicy("life", 0, 30, 1) };
            var strict = new StagedEventV4SaveSerializer(v3, definitions, policies);
            var serializer = new EventV4CompatibilitySerializer(strict);
            var runner = new EventV4CoreCommandRunner(content, v3, econ, archives);
            var original = GameSession.NewState(content, "run-v4-batch", 7219UL,
                new SimDate(2026, 10, 30));
            var folder = Path.Combine(Path.GetTempPath(),
                "m15-v4-batch-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var filename = Path.Combine(folder, "game.save");
            try
            {
                File.WriteAllBytes(filename, JsonSaveSerializer.Wrap(
                    JsonSaveSerializer.WriteObject(original), 2, 0));
                EventV4CommitGateway Gateway(AtomicFileSaveStore store) =>
                    new EventV4CommitGateway(serializer, store, definitions, policies, runner);
                EventV4CommitGateway Cold() =>
                    Gateway(new AtomicFileSaveStore(filename, serializer));
                var gateway = Cold();
                var beforeCreation = File.ReadAllBytes(filename);
                Assert(gateway.AdvanceDay(new CommandEnvelope(original.RunId,
                    "batch-before-character", 0,
                    new GameCommand(CommandKind.AdvanceBoundary))).StopReason ==
                    "InvalidRequest" &&
                    File.ReadAllBytes(filename).SequenceEqual(beforeCreation),
                    "pre-character advance cannot create an unfinishable durable batch");
                var uiCommands = new EventV4GameCommands(gateway, content);
                Assert(uiCommands.Snapshot().RunId == original.RunId &&
                    uiCommands.Snapshot().Revision == 0,
                    "presentation adapter projects validated migrated V4 state");
                var created = gateway.ExecuteGameplay(new CommandEnvelope(
                    original.RunId, "batch-create-character", 0,
                    new GameCommand(CommandKind.CreateCharacter, "fresh", 22,
                        "Batch Tester", "base.female")));
                Assert(created.Status == CommandStatus.Committed && created.Revision == 1,
                    "V4 batch fixture uses an actual V3 character receipt");
                Assert(uiCommands.Snapshot().Name == "Batch Tester" &&
                    uiCommands.Snapshot().Revision == created.Revision,
                    "UI IGameCommands adapter reads the same committed global revision");
                var dayRoot = new CommandEnvelope(original.RunId, "batch-day-root", 1,
                    new GameCommand(CommandKind.AdvanceBoundary));
                var replaces = 0;
                var faulted = Gateway(new AtomicFileSaveStore(filename, serializer,
                    point => point == "after-replace" && ++replaces == 2));
                var interrupted = faulted.AdvanceDay(dayRoot);
                Assert(interrupted.StopReason == "RecoveryRequired",
                    "one ambiguous child replacement must interrupt the advance transaction: " +
                    interrupted.StopReason + "; after-replace calls=" + replaces);
                var afterFault = Cold();
                var partial = afterFault.Read();
                var state = partial.State ??
                    throw new InvalidOperationException("Ambiguous batch has no persisted state.");
                var journal = state.EventV4!.Journal!;
                Assert(partial.Status == LoadStatus.Valid &&
                    journal.AdvanceBatches.Count == 1 &&
                    journal.AdvanceBatches[0].RootCommandId == "batch-day-root" &&
                    journal.AdvanceBatches[0].OpenedDateIso == "2026-10-30" &&
                    journal.AdvanceBatches[0].TargetDateIso == "2026-10-31" &&
                    EventAdvanceBatchHistory.CountSteps(journal, state.Receipts)["batch-day-root"] == 1,
                    "cold V4+V3 checkpoint restores original target and the one applied child");
                var unchanged = File.ReadAllBytes(filename);
                Assert(afterFault.AdvanceMonth(dayRoot).StopReason == "InvalidRequest" &&
                    File.ReadAllBytes(filename).SequenceEqual(unchanged),
                    "same root ID cannot switch from day to month on retry");
                Assert(afterFault.ExecuteCore(new CommandEnvelope(original.RunId,
                    "bypass-batch", state.Revision,
                    new GameCommand(CommandKind.AdvanceBoundary))).ReasonKey ==
                    "batch.in_progress",
                    "external boundary command cannot bypass unfinished batch owner");
                // The batch root and its internal child namespace are reserved
                // globally, including while the batch is still resumable.
                var beforeCollisions = File.ReadAllBytes(filename);
                Assert(afterFault.Offer(original.RunId, dayRoot.CommandId,
                    state.Revision).ReasonKey == "event.command_id_conflict" &&
                    afterFault.Offer(original.RunId, "v4batch/forged-offer",
                        state.Revision).ReasonKey == "event.command_id_conflict" &&
                    File.ReadAllBytes(filename).SequenceEqual(beforeCollisions),
                    "batch command identities cannot be recycled as event offers");
                var offer = afterFault.Offer(original.RunId, "batch-event-offer",
                    state.Revision);
                Assert(offer.Status == EventCommitStatus.Committed,
                    "event offer may interrupt unfinished batch at the same V3 frontier");
                var blocked = Cold();
                Assert(blocked.AdvanceDay(dayRoot).StopReason == "PlayerChoice",
                    "cold batch retry must stop at pending event choice");
                var pending = blocked.ReadPendingChoice();
                Assert(pending != null && pending.TicketId == offer.TicketId,
                    "interruption choice is backed by same validated journal");
                var beforeResolveCollision = File.ReadAllBytes(filename);
                Assert(blocked.Resolve(original.RunId, dayRoot.CommandId,
                    pending!.Revision, pending.TicketId, "ack").ReasonKey ==
                    "event.command_id_conflict" &&
                    blocked.Resolve(original.RunId, "v4batch/forged-resolve",
                        pending.Revision, pending.TicketId, "ack").ReasonKey ==
                    "event.command_id_conflict" &&
                    File.ReadAllBytes(filename).SequenceEqual(beforeResolveCollision),
                    "batch identities cannot resolve a pending event or mutate disk");
                var resolved = blocked.Resolve(original.RunId, "batch-choice-resolve",
                    pending.Revision, pending.TicketId, "ack");
                Assert(resolved.Status == EventCommitStatus.Committed,
                    "event choice resolves while batch intent survives");
                var resumed = Cold();
                var finished = resumed.AdvanceDay(dayRoot);
                Assert(finished.StopReason == "TargetReached" &&
                    finished.ReachedInstant.Date.ToString() == "2026-10-31",
                    "same stale root command resumes and reaches its original day target");
                var reached = Cold().Read().State!;
                var count = EventAdvanceBatchHistory.CountSteps(
                    reached.EventV4!.Journal!, reached.Receipts)["batch-day-root"];
                Assert(count > 1 && reached.DateIso == "2026-10-31" &&
                    reached.EventV4!.Journal!.AdvanceBatches.Count == 1,
                    "every resumed step uses authenticated V3 child receipts");
                var beforeRetry = File.ReadAllBytes(filename);
                Assert(Cold().Offer(original.RunId, dayRoot.CommandId, reached.Revision)
                        .ReasonKey == "event.command_id_conflict" &&
                    File.ReadAllBytes(filename).SequenceEqual(beforeRetry),
                    "completed batch roots remain reserved for the lifetime of the run");
                Assert(Cold().AdvanceDay(dayRoot).StopReason == "TargetReached" &&
                    File.ReadAllBytes(filename).SequenceEqual(beforeRetry),
                    "completed batch root retry is read-only, no extra time or cash settlement");
                var next = new CommandEnvelope(original.RunId, "batch-month-root",
                    reached.Revision, new GameCommand(CommandKind.AdvanceBoundary));
                var month = Cold().AdvanceMonth(next);
                Assert(month.StopReason == "TargetReached" &&
                    month.ReachedInstant.Date.ToString() == "2026-11-01",
                    "month batch retains next calendar-month target through normal V3 gameplay");
                var finalState = Cold().Read().State!;
                Assert(finalState.EventV4!.Journal!.AdvanceBatches.Count == 2 &&
                    finalState.EventV4.Journal.Tickets.Count == 1 &&
                    finalState.EventV4.Journal.OutcomeReceipts.Count == 1 &&
                    finalState.DateIso == "2026-11-01",
                    "two roots, one interrupting event and one effect survive cold replay");
                var beforeForged = File.ReadAllBytes(filename);
                EventV4Payload Copy() => JsonSaveSerializer.ReadObject<EventV4Payload>(
                    JsonSaveSerializer.WriteObject(finalState.EventV4!));
                var fakeTarget = Copy();
                fakeTarget.Journal!.AdvanceBatches[0].TargetDateIso = "2026-11-02";
                Throws<ArgumentException>(() => strict.SerializeVerified(fakeTarget));
                var movedFrontier = Copy();
                movedFrontier.Journal!.AdvanceBatches[0].CoreRevisionAtStart++;
                Throws<ArgumentException>(() => strict.SerializeVerified(movedFrontier));
                var missingBatch = Copy();
                missingBatch.Journal!.AdvanceBatches.RemoveAt(0);
                Throws<ArgumentException>(() => strict.SerializeVerified(missingBatch));
                var duplicatedBatch = Copy();
                duplicatedBatch.Journal!.AdvanceBatches.Add(
                    duplicatedBatch.Journal.AdvanceBatches[0].Copy());
                Throws<ArgumentException>(() => strict.SerializeVerified(duplicatedBatch));
                // The reserved child namespace must also be rejected by the
                // planner and on a forged cold checkpoint, not just the gateway.
                var authoritative = finalState.EventV4!.Journal!;
                var atFrontier = AuthoritativeEventContext.FromCore(finalState, authoritative);
                Assert(EventDecisionJournal.Offer(authoritative, definitions, policies,
                        atFrontier, authoritative.Revision, "v4batch/planner-offer",
                        finalState.Receipts.Count).ReasonKey == "event.command_id_conflict" &&
                    EventDecisionJournal.Resolve(authoritative, definitions, policies,
                        authoritative.Revision, "v4batch/planner-resolve",
                        authoritative.Tickets[0].Id, "ack").ReasonKey ==
                    "event.command_id_conflict",
                    "standalone event planner must refuse reserved internal batch identities");
                var forgedOfferId = Copy();
                forgedOfferId.Journal!.Tickets[0].OfferCommandId = "v4batch/forged-offer";
                Throws<ArgumentException>(() => strict.SerializeVerified(forgedOfferId));
                var forgedResolveId = Copy();
                forgedResolveId.Journal!.Tickets[0].ResolutionCommandId =
                    "v4batch/forged-resolution";
                forgedResolveId.Journal.OutcomeReceipts[0].ResolutionCommandId =
                    "v4batch/forged-resolution";
                Throws<ArgumentException>(() => strict.SerializeVerified(forgedResolveId));
                Assert(File.ReadAllBytes(filename).SequenceEqual(beforeForged),
                    "forged/missing duplicate batch histories cannot modify saved bytes");
            }
            finally
            {
                try { Directory.Delete(folder, true); }
                catch (IOException) { }
            }
        });

        Check("M15 outcome effect receipts unlock later events exactly once and reject forged replay", () =>
        {
            Throws<ArgumentException>(() => new EventChoiceDefinition(
                "fake-job", "event.fake", "outcome.fake", new[] { "employed" }));
            Throws<ArgumentException>(() => new EventChoiceDefinition(
                "duplicate", "event.duplicate", "outcome.duplicate",
                new[] { "event/contact", "event/contact" }));
            var definitions = new[]
            {
                new GameEventDefinition("meet-contact", "network", 2026, 1,
                    1, 0, 1, 2, 0, Array.Empty<string>(), Array.Empty<string>(),
                    new[] { new EventChoiceDefinition("exchange", "event.exchange",
                        "outcome.contact", new[] { "event/contact" }) }),
                new GameEventDefinition("contact-followup", "network", 2026, 1,
                    1, 0, 1, 2, 1, new[] { "event/contact" }, Array.Empty<string>(),
                    new[] { new EventChoiceDefinition("accept", "event.accept",
                        "outcome.followup") })
            };
            var policies = new[] { new EventCategoryPolicy("network", 0, 30, 5) };
            var baseDate = new SimDate(2026, 10, 1);
            var state = new GameState
            {
                RunId = "effect-run", Name = "Tester", DateIso = baseDate.ToString()
            };
            var journal = EventDecisionJournal.New(state.RunId, 42117);
            journal.OriginDateIso = state.DateIso;
            var firstContext = AuthoritativeEventContext.FromCore(state, journal);
            Assert(firstContext.Stage == 1 && !firstContext.Tags.Contains("event/contact"),
                "unearned effect tag cannot be present before choice");
            var offered = EventDecisionJournal.Offer(journal, definitions, policies,
                firstContext, journal.Revision, "offer-first", 0);
            Assert(offered.Status == EventJournalStatus.Offered &&
                offered.Next.Tickets.Single().EventId == "meet-contact",
                "first-day offer must select authoring window before effect exists");
            var resolved = EventDecisionJournal.Resolve(offered.Next, definitions, policies,
                offered.Next.Revision, "choose-contact", offered.TicketId, "exchange");
            Assert(resolved.Status == EventJournalStatus.Resolved &&
                resolved.Next.OutcomeReceipts.Count == 1 &&
                resolved.Next.OutcomeReceipts[0].Id == offered.TicketId + "/effect" &&
                resolved.Next.OutcomeReceipts[0].OutcomeId == "outcome.contact" &&
                resolved.Next.OutcomeReceipts[0].ResolutionCommandId == "choose-contact" &&
                resolved.Next.OutcomeReceipts[0].GrantedTags.SequenceEqual(
                    new[] { "event/contact" }),
                "choice yields one deterministic persisted receipt with the authored tag");
            var duplicated = EventDecisionJournal.Resolve(resolved.Next, definitions, policies,
                0, "choose-contact", offered.TicketId, "exchange");
            Assert(duplicated.Status == EventJournalStatus.AlreadyCommitted &&
                duplicated.Next.OutcomeReceipts.Count == 1 &&
                duplicated.Next.Revision == resolved.Next.Revision,
                "stale command retry never applies an effect twice");
            var currentTags = AuthoritativeEventContext.FromCore(state, resolved.Next);
            var atFirstOffer = AuthoritativeEventContext.FromCore(state, resolved.Next,
                offered.TicketId);
            Assert(currentTags.Tags.Contains("event/contact") &&
                !atFirstOffer.Tags.Contains("event/contact"),
                "later eligibility uses the receipt while past context excludes self-reward");
            state.DateIso = baseDate.AddDays(1).ToString();
            var secondContext = AuthoritativeEventContext.FromCore(state, resolved.Next);
            var secondOffer = EventDecisionJournal.Offer(resolved.Next, definitions, policies,
                secondContext, resolved.Next.Revision, "offer-second", 0);
            Assert(secondOffer.Status == EventJournalStatus.Offered &&
                secondOffer.Next.Tickets.Last().EventId == "contact-followup" &&
                secondOffer.Next.Tickets.Last().Tags.Contains("event/contact") &&
                secondOffer.Next.OutcomeReceipts.Count == 1,
                "earned contact unlocks the next-day event without duplicating receipt");
            EventJournalReplay.Validate(secondOffer.Next, definitions, policies);

            EventJournalState Clone() => JsonSaveSerializer.ReadObject<EventJournalState>(
                JsonSaveSerializer.WriteObject(secondOffer.Next));
            var forgedTags = Clone();
            forgedTags.OutcomeReceipts[0].GrantedTags.Clear();
            Throws<ArgumentException>(() =>
                EventJournalReplay.Validate(forgedTags, definitions, policies));
            var repeated = Clone();
            repeated.OutcomeReceipts.Add(repeated.OutcomeReceipts[0].Copy());
            Throws<ArgumentException>(() =>
                EventJournalReplay.Validate(repeated, definitions, policies));
            var forgedOutcome = Clone();
            forgedOutcome.OutcomeReceipts[0].OutcomeId = "outcome.fake";
            Throws<ArgumentException>(() =>
                EventJournalReplay.Validate(forgedOutcome, definitions, policies));
            var removed = Clone();
            removed.OutcomeReceipts.Clear();
            Throws<ArgumentException>(() =>
                EventJournalReplay.Validate(removed, definitions, policies));
            var forgedOffer = Clone();
            forgedOffer.Tickets[1].Tags.Clear();
            Throws<ArgumentException>(() =>
                EventJournalReplay.Validate(forgedOffer, definitions, policies));
        });

        Check("M15 authoritative event maturity and tags derive only from committed core fields", () =>
        {
            var source = new GameState
            {
                RunId = "event-tags",
                DateIso = "2026-10-11",
                Name = "Player",
                Employment = new EmploymentState { CareerId = "developer" }
            };
            var journal = EventDecisionJournal.New("event-tags", 1234);
            journal.OriginDateIso = "2026-10-01";
            var employed = AuthoritativeEventContext.FromCore(source, journal);
            Assert(employed.Stage == 1 && employed.OriginDate.ToString() == "2026-10-01" &&
                employed.Date.ToString() == source.DateIso &&
                employed.Tags.Contains("character-created") &&
                employed.Tags.Contains("employed") && employed.Tags.Contains("career/developer") &&
                !employed.Tags.Contains("business-active"),
                "employed-only tags are derived from actual current employment");
            source.Employment = null;
            source.Course = new CourseState { DefinitionId = "communication-basics" };
            var studying = AuthoritativeEventContext.FromCore(source, journal);
            Assert(studying.Stage == 1 && studying.Tags.Contains("unemployed") &&
                studying.Tags.Contains("course-active") &&
                studying.Tags.Contains("course/communication-basics") &&
                !studying.Tags.Contains("employed"),
                "resignation and an active course deterministically change event eligibility");
            source.Businesses.Add(new BusinessState
            {
                DefinitionId = "online-store", OpenedIso = source.DateIso
            });
            var founder = AuthoritativeEventContext.FromCore(source, journal);
            Assert(founder.Stage == 2 && founder.Tags.Contains("business-active") &&
                founder.Tags.Contains("business/online-store"),
                "only a real active business enables the side-project event tier");
            source.Businesses[0].ClosedIso = source.DateIso;
            source.Businesses[0].ClosedMinute = 800;
            var closed = AuthoritativeEventContext.FromCore(source, journal);
            Assert(closed.Stage == 1 && !closed.Tags.Contains("business/online-store") &&
                !closed.Tags.Contains("business-active"),
                "closed business must not retain the eligibility tag");
            var wrongRun = EventDecisionJournal.New("different-run", 4321);
            wrongRun.OriginDateIso = source.DateIso;
            Throws<ArgumentException>(() => AuthoritativeEventContext.FromCore(source, wrongRun));
        });
        Check("M15 V4 core GameSession receipts interleave with events and restore exactly once", () =>
        {
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var econ = M9T02FunctionalEconomy.Create();
            var archives = new BundledHistoricalV2ContentResolver();
            var replay = new VersionedReceiptReplay(econ, content);
            var v3 = new V3SaveCompatibilitySerializer(content, GameSession.CreateRestoreValidator(),
                new V2ToV3Migration(archives, replay),
                new StagedV3SaveSerializer(archives, replay));
            var definitions = new[]
            {
                new GameEventDefinition("career-moment", "career",
                    2026, 0, 1, 0, 1, 2, 0, Array.Empty<string>(), Array.Empty<string>(),
                    new[] { new EventChoiceDefinition("continue", "event.continue",
                        "outcome.continue", new[] { "event/networked" }) }),
                new GameEventDefinition("network-followup", "career",
                    2026, 1, 1, 0, 1, 2, 1, new[] { "event/networked" },
                    Array.Empty<string>(),
                    new[] { new EventChoiceDefinition("reply", "event.reply",
                        "outcome.reply") })
            };
            var policies = new[] { new EventCategoryPolicy("career", 0, 7, 4) };
            var serializer = new EventV4CompatibilitySerializer(
                new StagedEventV4SaveSerializer(v3, definitions, policies));
            var runtime = new EventV4CoreCommandRunner(content, v3, econ, archives);
            var origin = GameSession.NewState(content, "run-v4-core", 317749UL,
                new SimDate(2026, 10, 1));
            Assert(origin.Date.Day == content.Economy.ExpenseDay,
                "fixture must cross the authored living-expense settlement day");
            var expectedAfterCreate = checked(content.Starts["fresh"].Cash -
                content.Economy.LivingCost);
            var expectedAfterCourse = checked(expectedAfterCreate -
                content.Courses["communication-basics"].Price);
            var originalV2 = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(origin), 2, 0);
            var folder = Path.Combine(Path.GetTempPath(), "m15-interleaved-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var filename = Path.Combine(folder, "game.save");
            try
            {
                File.WriteAllBytes(filename, originalV2);
                var disk = new AtomicFileSaveStore(filename, serializer);
                var gateway = new EventV4CommitGateway(serializer, disk, definitions, policies, runtime);
                var created = new GameCommand(CommandKind.CreateCharacter, "fresh", 23,
                    "V4 Tester", "base.female");
                var initialResult = gateway.ExecuteCore(new CommandEnvelope(
                    origin.RunId, "create-v4", 0, created));
                Assert(initialResult.Status == EventCommitStatus.Committed &&
                    initialResult.Revision == 1 &&
                    initialResult.CoreCommand?.Status == CommandStatus.Committed &&
                    initialResult.CoreCommand.Revision == 1 &&
                    initialResult.CoreCommand.Outcome != null &&
                    initialResult.CoreCommand.Outcome.CashDelta == expectedAfterCreate,
                    "V4 commit preserves the genuine V3 outcome, cue and first-day cash delta");
                var first = gateway.Read().State ??
                    throw new InvalidOperationException("V4 first commit missing core projection.");
                var firstJournal = first.EventV4?.Journal ??
                    throw new InvalidOperationException("V4 first commit missing event journal.");
                Assert(first.Name == "V4 Tester" && first.Cash == expectedAfterCreate &&
                    first.Receipts.Count == 1 && firstJournal.Revision == 0,
                    "V4 projection includes authoritative V3 receipt and starting cash: name=" +
                    first.Name + "; cash=" + first.Cash + "; receipts=" + first.Receipts.Count +
                    "; journal=" + firstJournal.Revision);
                // V4's outer DTO ignores EconomicV3 on the wire, but the UI
                // must still receive the *same* authenticated ledger projection.
                var presentation = new EventV4GameCommands(gateway, content, econ);
                Assert(first.EconomicV3?.Current == first &&
                    first.EconomicV3.EconomicRecords != null &&
                    presentation.ReadCommittedFinance().Count == 0 &&
                    presentation.ReadBusinesses().Businesses.Count == 0 &&
                    presentation.ReadBusinessOperations().Portfolio.Game.Revision == 1 &&
                    presentation.ReadLifeDashboard().Finance.CashVnd == expectedAfterCreate &&
                    presentation.ReadLifeDashboard().Game.Revision == 1,
                    "V4 readouts must preserve authenticated V3 economic attachment");
                var afterCreate = File.ReadAllBytes(filename);
                Assert(gateway.ExecuteCore(new CommandEnvelope(
                    origin.RunId, "create-v4", 0, created)).Status ==
                    EventCommitStatus.AlreadyCommitted &&
                    File.ReadAllBytes(filename).SequenceEqual(afterCreate),
                    "cold retry of a V3 game command may not duplicate receipt or bytes");
                var context = new GameEventContext(origin.Date, origin.Date, 1,
                    new[] { "character-created", "unemployed" }, Array.Empty<EventOccurrence>());
                var beforeForged = File.ReadAllBytes(filename);
                Assert(gateway.Offer(origin.RunId, "event-offer-v4", 1,
                    new GameEventContext(origin.Date, origin.Date, 2,
                        new[] { "business-active", "employed" },
                        Array.Empty<EventOccurrence>())).ReasonKey ==
                    "event.context_not_authoritative" &&
                    File.ReadAllBytes(filename).SequenceEqual(beforeForged),
                    "client may not impersonate business or employment tags/stage");
                var offered = gateway.Offer(origin.RunId, "event-offer-v4", 1);
                Assert(gateway.Offer(origin.RunId, "event-offer-v4", 1, context).Status ==
                    EventCommitStatus.AlreadyCommitted,
                    "legacy context input is accepted only when it matches the committed offer");
                Assert(offered.Status == EventCommitStatus.Committed && offered.Revision == 2 &&
                    gateway.Read().State!.PendingChoiceId == offered.TicketId,
                    "V4 persisted event blocks pending transition");
                var pending = gateway.ReadPendingChoice() ??
                    throw new InvalidOperationException("Persisted choice has no read model.");
                Assert(pending.RunId == origin.RunId && pending.Revision == 2 &&
                    pending.TicketId == offered.TicketId && pending.EventId == "career-moment" &&
                    pending.Choices.Count == 1 && pending.Choices[0].Id == "continue" &&
                    pending.Choices[0].LabelKey == "event.continue",
                    "UI reads the real authored pending choice and CAS revision, not synthetic input");
                var afterOffer = File.ReadAllBytes(filename);
                var sourceWire = gateway.Read().State!.EventV4!;
                var strictV4 = new StagedEventV4SaveSerializer(v3, definitions, policies);
                EventV4Payload AlteredOffer() =>
                    JsonSaveSerializer.ReadObject<EventV4Payload>(
                        JsonSaveSerializer.WriteObject(sourceWire));
                var collidingOfferId = AlteredOffer();
                collidingOfferId.Journal!.Tickets[0].OfferCommandId = "create-v4";
                Throws<ArgumentException>(() => strictV4.SerializeVerified(collidingOfferId));
                var collisionWire = JsonSaveSerializer.Wrap(
                    JsonSaveSerializer.WriteObject(collidingOfferId), 4, offered.Revision);
                Assert(serializer.DeserializeAndValidate(collisionWire).Status ==
                        LoadStatus.Corrupt,
                    "cold replay rejects recomputed-checksum V4 offer claiming a V3 command ID");
                var forgedSelectedEvent = AlteredOffer();
                forgedSelectedEvent.Journal!.Tickets[0].EventId = "network-followup";
                Throws<ArgumentException>(() => strictV4.SerializeVerified(forgedSelectedEvent));
                var forgedStage = AlteredOffer();
                forgedStage.Journal!.Tickets[0].Stage = 2;
                Throws<ArgumentException>(() => strictV4.SerializeVerified(forgedStage));
                var forgedTags = AlteredOffer();
                forgedTags.Journal!.Tickets[0].Tags.Clear();
                Throws<ArgumentException>(() => strictV4.SerializeVerified(forgedTags));
                var forgedOrigin = AlteredOffer();
                forgedOrigin.Journal!.OriginDateIso = "2026-09-01";
                Throws<ArgumentException>(() => strictV4.SerializeVerified(forgedOrigin));
                var futureFrontier = AlteredOffer();
                futureFrontier.Journal!.Tickets[0].CoreRevisionAtOffer++;
                Throws<ArgumentException>(() => strictV4.SerializeVerified(futureFrontier));
                Assert(File.ReadAllBytes(filename).SequenceEqual(afterOffer),
                    "context/activation tampering must never rewrite committed file");
                Assert(gateway.ExecuteCore(new CommandEnvelope(origin.RunId, "clock-v4", 2,
                    new GameCommand(CommandKind.AdvanceBoundary))).ReasonKey ==
                    "event.choice_required" &&
                    File.ReadAllBytes(filename).SequenceEqual(afterOffer),
                    "inner V3 cannot bypass pending event choice with direct advance");
                var blockedCourse = new GameCommand(CommandKind.PurchaseCourse,
                    "communication-basics");
                Assert(gateway.ExecuteCore(new CommandEnvelope(origin.RunId,
                        "course-while-event", 2, blockedCourse)).ReasonKey ==
                        "event.choice_required" &&
                    gateway.ExecuteCore(new CommandEnvelope(origin.RunId,
                        "resign-while-event", 2,
                        new GameCommand(CommandKind.Resign))).ReasonKey ==
                        "event.choice_required" &&
                    gateway.Read().State!.Cash == expectedAfterCreate &&
                    gateway.Read().State!.Receipts.Count == 1 &&
                    File.ReadAllBytes(filename).SequenceEqual(afterOffer),
                    "pending event freezes economic commands and saves without spending money");
                Assert(gateway.ExecuteCore(new CommandEnvelope(origin.RunId, "event-offer-v4", 2,
                    new GameCommand(CommandKind.Resign))).ReasonKey == "command.id_conflict",
                    "event command IDs and V3 receipt IDs share a collision domain");
                var uncertainEventWriter = new EventV4CommitGateway(serializer,
                    new AtomicFileSaveStore(filename, serializer,
                        point => point == "after-replace"), definitions, policies, runtime);
                var uncertainResolution = uncertainEventWriter.Resolve(
                    origin.RunId, "event-resolve-v4", 2, offered.TicketId, "continue");
                Assert(uncertainResolution.Status == EventCommitStatus.RecoveryRequired,
                    "post-replace event effect commit must report ambiguous promotion");
                var resolved = gateway.Resolve(origin.RunId, "event-resolve-v4", 2,
                    offered.TicketId, "continue");
                Assert(resolved.Status == EventCommitStatus.AlreadyCommitted &&
                    resolved.Revision == 3 && resolved.OutcomeId == "outcome.continue" &&
                    gateway.ReadPendingChoice() == null,
                    "cold retry discovers the once-only receipt and clears the pending UI choice");
                var effectWire = gateway.Read().State!.EventV4!;
                Assert(effectWire.Journal!.OutcomeReceipts.Count == 1 &&
                    effectWire.Journal.OutcomeReceipts[0].GrantedTags.SequenceEqual(
                        new[] { "event/networked" }),
                    "a single authored gameplay effect is committed beside resolution");
                var forgedEffect = JsonSaveSerializer.ReadObject<EventV4Payload>(
                    JsonSaveSerializer.WriteObject(effectWire));
                forgedEffect.Journal!.OutcomeReceipts[0].GrantedTags.Clear();
                Throws<ArgumentException>(() => strictV4.SerializeVerified(forgedEffect));
                var collidingResolutionId = JsonSaveSerializer.ReadObject<EventV4Payload>(
                    JsonSaveSerializer.WriteObject(effectWire));
                collidingResolutionId.Journal!.Tickets[0].ResolutionCommandId = "create-v4";
                collidingResolutionId.Journal.OutcomeReceipts[0].ResolutionCommandId =
                    "create-v4";
                Throws<ArgumentException>(() =>
                    strictV4.SerializeVerified(collidingResolutionId));
                var collisionResolvedWire = JsonSaveSerializer.Wrap(
                    JsonSaveSerializer.WriteObject(collidingResolutionId), 4, resolved.Revision);
                var unchangedAfterResolution = File.ReadAllBytes(filename);
                Assert(serializer.DeserializeAndValidate(collisionResolvedWire).Status ==
                        LoadStatus.Corrupt &&
                    File.ReadAllBytes(filename).SequenceEqual(unchangedAfterResolution),
                    "recomputed-checksum V4 resolved outcome may not claim V3 receipt identity");
                var moneyCommand = new GameCommand(CommandKind.PurchaseCourse, "communication-basics");
                var uncertainWriter = new EventV4CommitGateway(serializer,
                    new AtomicFileSaveStore(filename, serializer,
                        point => point == "after-replace"), definitions, policies, runtime);
                var uncertain = uncertainWriter.ExecuteCore(new CommandEnvelope(
                    origin.RunId, "course-v4", 3, moneyCommand));
                Assert(uncertain.Status == EventCommitStatus.RecoveryRequired,
                    "post-replace economic command must report ambiguous promotion");
                var paid = gateway.ExecuteCore(new CommandEnvelope(
                    origin.RunId, "course-v4", 3, moneyCommand));
                Assert(paid.Status == EventCommitStatus.AlreadyCommitted &&
                    paid.Revision == 4,
                    "cold retry must discover the one V3 course receipt instead of paying twice");
                var saved = gateway.Read().State!;
                Assert(saved.Cash == expectedAfterCourse && saved.Receipts.Count == 2 &&
                    saved.EventV4!.Journal!.Revision == 2 &&
                    saved.Revision == 4 && saved.EventRng != origin.EventRng &&
                    saved.SchedulerRng == origin.SchedulerRng,
                    "authored course price is debited once; journal and RNG remain isolated");
                var courseBytes = File.ReadAllBytes(filename);
                var cold = new EventV4CommitGateway(serializer,
                    new AtomicFileSaveStore(filename, serializer), definitions, policies, runtime);
                Assert(cold.ExecuteCore(new CommandEnvelope(origin.RunId, "course-v4", 3,
                    new GameCommand(CommandKind.PurchaseCourse, "communication-basics"))).Status ==
                    EventCommitStatus.AlreadyCommitted &&
                    File.ReadAllBytes(filename).SequenceEqual(courseBytes),
                    "cold economic retry must not debit cash twice");
                Assert(cold.Offer(origin.RunId, "course-v4", 4, context).ReasonKey ==
                    "event.command_id_conflict", "economic command ID cannot open an event");
                Assert(cold.ExecuteCore(new CommandEnvelope(origin.RunId, "course-v4", 4,
                    new GameCommand(CommandKind.Resign))).ReasonKey == "command.id_conflict",
                    "same receipt ID with different payload rejected");
                Assert(cold.ExecuteCore(new CommandEnvelope(origin.RunId, "clock-v4", 3,
                    new GameCommand(CommandKind.AdvanceBoundary))).ReasonKey == "command.stale_state",
                    "stale global revision must be rejected even when inner V3 revision differs");
                var tick = cold.ExecuteGameplay(new CommandEnvelope(origin.RunId, "clock-v4", 4,
                    new GameCommand(CommandKind.AdvanceBoundary)));
                Assert(tick.Status == CommandStatus.Committed && tick.Revision == 5 &&
                    tick.Outcome != null && tick.Outcome.Cue == cold.Read().State!.CurrentCue &&
                    tick.OperationId.Length > 0 &&
                    tick.MinutesConsumed == tick.Outcome.MinutesConsumed &&
                    tick.Outcome.Revision == 3,
                    "UI adapter reports global V4 revision while retaining exact V3 operation/cue");
                var retryTick = cold.ExecuteGameplay(new CommandEnvelope(origin.RunId,
                    "clock-v4", 4, new GameCommand(CommandKind.AdvanceBoundary)));
                Assert(retryTick.Status == CommandStatus.AlreadyCommitted &&
                    retryTick.Revision == 5 && retryTick.Outcome == null,
                    "unacknowledged gameplay retry resolves globally without inventing an outcome");
                var restarted = cold.Read();
                var restoredState = restarted.State ??
                    throw new InvalidOperationException("V4 cold read has no validated projection.");
                Assert(restarted.Status == LoadStatus.Valid &&
                    restoredState.Receipts.Count == 3 &&
                    restoredState.EventV4!.Journal!.Tickets.Count == 1 &&
                    restoredState.EventV4.Journal.OutcomeReceipts.Count == 1 &&
                    restoredState.Cash == expectedAfterCourse &&
                    restoredState.PendingChoiceId.Length == 0,
                    "cold replay validates interleaved V3 receipts and event history");
                // The offer was made at V3 receipt revision 1. V3 has advanced
                // through course purchase and a day boundary by this point:
                // validation must reconstruct the *old* tags, not trust the ticket.
                EventV4Payload HistoricalCopy() =>
                    JsonSaveSerializer.ReadObject<EventV4Payload>(
                        JsonSaveSerializer.WriteObject(restoredState.EventV4!));
                var changedOldTags = HistoricalCopy();
                changedOldTags.Journal!.Tickets[0].Tags =
                    new List<string> { "character-created", "employed" };
                Throws<ArgumentException>(() => strictV4.SerializeVerified(changedOldTags));
                var changedOldStage = HistoricalCopy();
                changedOldStage.Journal!.Tickets[0].Stage = 2;
                Throws<ArgumentException>(() => strictV4.SerializeVerified(changedOldStage));
                var movedToStudyFrontier = HistoricalCopy();
                movedToStudyFrontier.Journal!.Tickets[0].CoreRevisionAtOffer++;
                Throws<ArgumentException>(() => strictV4.SerializeVerified(movedToStudyFrontier));
                var movedToLegacyFrontier = HistoricalCopy();
                movedToLegacyFrontier.Journal!.Tickets[0].CoreRevisionAtOffer = -1;
                Throws<ArgumentException>(() => strictV4.SerializeVerified(movedToLegacyFrontier));
                var identicalBytes = File.ReadAllBytes(filename);
                Assert(cold.Read().Status == LoadStatus.Valid &&
                    File.ReadAllBytes(filename).SequenceEqual(identicalBytes),
                    "historical prefix inspection must be read-only and preserve the checkpoint");
                Assert(Convert.FromBase64String(restoredState.EventV4!.EmbeddedV3EnvelopeBase64)
                    .Length > 0 && JsonSaveSerializer.ReadObject<SaveEnvelope>(
                        File.ReadAllBytes(filename)).SchemaVersion == 4,
                    "authoritative save remains single nested V3 within V4, not independent files");
                Assert(File.ReadAllBytes(filename).Length != originalV2.Length,
                    "legacy V2 was promoted only after authorized command");

                // Execute real receipt-backed game time, not a fabricated date
                // supplied by the event test. The persisted outcome's tag must
                // remain effective across an authenticated V3 midnight boundary.
                var boundaries = 0;
                while (cold.Read().State!.DateIso == origin.DateIso && boundaries < 16)
                {
                    var beforeBoundary = cold.Read().State!;
                    var transition = cold.ExecuteCore(new CommandEnvelope(origin.RunId,
                        "network-boundary-" + boundaries, beforeBoundary.Revision,
                        new GameCommand(CommandKind.AdvanceBoundary)));
                    Assert(transition.Status == EventCommitStatus.Committed,
                        "real V3 boundary must progress the event-enabled game: " +
                        transition.ReasonKey);
                    boundaries++;
                }
                var dayTwo = cold.Read().State!;
                Assert(dayTwo.DateIso == origin.Date.AddDays(1).ToString(),
                    "V3 receipt-backed time advance must reach the next day");
                var nextOffer = cold.Offer(origin.RunId, "network-followup-offer",
                    dayTwo.Revision);
                Assert(nextOffer.Status == EventCommitStatus.Committed,
                    "earned event/networked tag should authorize the next day's event: " +
                    nextOffer.ReasonKey);
                var nextCold = new EventV4CommitGateway(serializer,
                    new AtomicFileSaveStore(filename, serializer),
                    definitions, policies, runtime);
                var following = nextCold.Read();
                var followupJournal = following.State?.EventV4?.Journal ??
                    throw new InvalidOperationException("Follow-up event cold read has no journal.");
                Assert(following.Status == LoadStatus.Valid &&
                    followupJournal.Tickets.Count == 2 &&
                    followupJournal.Tickets[1].EventId == "network-followup" &&
                    followupJournal.Tickets[1].Tags.Contains("event/networked") &&
                    followupJournal.OutcomeReceipts.Count == 1 &&
                    following.State!.PendingChoiceId == nextOffer.TicketId,
                    "cold replay proves granted tag unlocks the next event exactly once");
                var beforeTamper = File.ReadAllBytes(filename);
                var forgedPriorEffect = JsonSaveSerializer.ReadObject<EventV4Payload>(
                    JsonSaveSerializer.WriteObject(following.State.EventV4!));
                forgedPriorEffect.Journal!.OutcomeReceipts[0].GrantedTags.Clear();
                Throws<ArgumentException>(() => strictV4.SerializeVerified(forgedPriorEffect));
                Assert(File.ReadAllBytes(filename).SequenceEqual(beforeTamper),
                    "forged old event effect may not change the saved follow-up");

            }
            finally
            {
                try { Directory.Delete(folder, true); }
                catch (IOException) { }
            }
        });

        Check("M15 empty event days persist once across ambiguous real-file V4 CAS", () =>
        {
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var economics = M9T02FunctionalEconomy.Create();
            var historical = new BundledHistoricalV2ContentResolver();
            var replay = new VersionedReceiptReplay(economics, content);
            var v3 = new V3SaveCompatibilitySerializer(content,
                GameSession.CreateRestoreValidator(), new V2ToV3Migration(historical, replay),
                new StagedV3SaveSerializer(historical, replay));
            var definitions = new[] { new GameEventDefinition(
                "only-employed", "career", 2026, 1, 1, 0, 1, 1, 0,
                new[] { "employed" }, Array.Empty<string>(),
                new[] { new EventChoiceDefinition("ack", "event.ack", "outcome.ack") }) };
            var policies = new[] { new EventCategoryPolicy("career", 0, 30, 2) };
            var strict = new StagedEventV4SaveSerializer(v3, definitions, policies);
            var serializer = new EventV4CompatibilitySerializer(strict);
            var runner = new EventV4CoreCommandRunner(content, v3, economics, historical);
            var first = GameSession.NewState(content, "run-v4-empty", 59137UL,
                new SimDate(2026, 10, 1));
            var folder = Path.Combine(Path.GetTempPath(),
                "m15-empty-day-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var filename = Path.Combine(folder, "game.save");
            try
            {
                File.WriteAllBytes(filename, JsonSaveSerializer.Wrap(
                    JsonSaveSerializer.WriteObject(first), 2, 0));
                EventV4CommitGateway Cold(Func<string, bool>? fault = null) =>
                    new EventV4CommitGateway(serializer,
                        new AtomicFileSaveStore(filename, serializer, fault),
                        definitions, policies, runner);
                Assert(Cold().CheckCurrentDay(first.RunId, 0).Status ==
                    EventCommitStatus.Rejected, "a character is required before daily checks");
                var create = Cold().ExecuteCore(new CommandEnvelope(first.RunId,
                    "empty-character", 0, new GameCommand(CommandKind.CreateCharacter,
                        "fresh", 25, "Empty Test", "base.female")));
                Assert(create.Status == EventCommitStatus.Committed && create.Revision == 1,
                    "daily check starts after real V3 GameSession receipt");
                var initial = Cold().Read().State!;
                var originalBytes = File.ReadAllBytes(filename);
                Assert(Cold(point => point == "during-temp-write").CheckCurrentDay(
                        first.RunId, initial.Revision).Status ==
                        EventCommitStatus.PersistenceFailed &&
                    File.ReadAllBytes(filename).SequenceEqual(originalBytes),
                    "failed empty-day write must preserve the committed checkpoint");
                Assert(Cold(point => point == "after-replace").CheckCurrentDay(
                        first.RunId, initial.Revision).Status ==
                        EventCommitStatus.RecoveryRequired,
                    "ambiguous empty-day promotion must not claim an unverified result");
                var cold = Cold();
                var after = cold.Read().State!;
                var committedBytes = File.ReadAllBytes(filename);
                Assert(cold.CheckCurrentDay(first.RunId, initial.Revision).Status ==
                        EventCommitStatus.AlreadyCommitted &&
                    after.EventV4!.Journal!.NoEligibleDays.Count == 1 &&
                    after.EventV4.Journal.NoEligibleDays[0].CommandId ==
                        EventDecisionJournal.DailyCommandId(after.DateIso) &&
                    after.EventV4.Journal.NoEligibleDays[0].CoreRevisionAtCheck ==
                        initial.Receipts.Count &&
                    after.Revision == initial.Revision + 1 &&
                    after.EventRng == initial.EventRng &&
                    after.SchedulerRng == initial.SchedulerRng &&
                    after.Cash == initial.Cash &&
                    after.Receipts.Count == initial.Receipts.Count &&
                    File.ReadAllBytes(filename).SequenceEqual(committedBytes),
                    "cold retry must not spend cash, advance RNG or duplicate negative day");
                Assert(cold.Offer(first.RunId, "another-offer", after.Revision).ReasonKey ==
                        "event.window_already_processed" &&
                    cold.Offer(first.RunId,
                        EventDecisionJournal.DailyCommandId(after.DateIso),
                        after.Revision).ReasonKey == "event.command_id_conflict" &&
                    File.ReadAllBytes(filename).SequenceEqual(committedBytes),
                    "daily window and its reserved command ID cannot be reused");
                EventV4Payload Copy() => JsonSaveSerializer.ReadObject<EventV4Payload>(
                    JsonSaveSerializer.WriteObject(after.EventV4!));
                var forgedTags = Copy();
                forgedTags.Journal!.NoEligibleDays[0].Tags.Add("employed");
                Throws<ArgumentException>(() => strict.SerializeVerified(forgedTags));
                var forgedDate = Copy();
                forgedDate.Journal!.NoEligibleDays[0].DateIso = "2026-10-02";
                Throws<ArgumentException>(() => strict.SerializeVerified(forgedDate));
                var forgedFrontier = Copy();
                forgedFrontier.Journal!.NoEligibleDays[0].CoreRevisionAtCheck++;
                Throws<ArgumentException>(() => strict.SerializeVerified(forgedFrontier));
                var forgedCommand = Copy();
                forgedCommand.Journal!.NoEligibleDays[0].CommandId = "forged-id";
                Throws<ArgumentException>(() => strict.SerializeVerified(forgedCommand));
                Assert(File.ReadAllBytes(filename).SequenceEqual(committedBytes),
                    "tampered negative-day evidence may not rewrite verified save bytes");
            }
            finally
            {
                try { Directory.Delete(folder, true); }
                catch (IOException) { }
            }
        });
        Check("M15 fresh V4 run persists its first character receipt atomically", () =>
        {
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var economic = M9T02FunctionalEconomy.Create();
            var archives = new BundledHistoricalV2ContentResolver();
            var replay = new VersionedReceiptReplay(economic, content);
            var v3 = new V3SaveCompatibilitySerializer(content,
                GameSession.CreateRestoreValidator(),
                new V2ToV3Migration(archives, replay),
                new StagedV3SaveSerializer(archives, replay));
            var defs = FirstPlayableEventContent.Definitions;
            var policies = FirstPlayableEventContent.Policies;
            var serializer = new EventV4CompatibilitySerializer(
                new StagedEventV4SaveSerializer(v3, defs, policies));
            var runner = new EventV4CoreCommandRunner(content, v3, economic, archives);
            var seed = GameSession.NewState(content, "v4-fresh-once", 51243UL,
                new SimDate(2026, 10, 1));
            var seeded = serializer.DeserializeAndValidate(JsonSaveSerializer.Wrap(
                JsonSaveSerializer.WriteObject(seed), 2, 0));
            Assert(seeded.Status == LoadStatus.Valid &&
                seeded.State?.Revision == 0 &&
                seeded.State.SaveVersion == 4, "V2 seed must migrate without disk writes");
            var dir = Path.Combine(Path.GetTempPath(),
                "v4-genesis-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "game.save");
            try
            {
                EventV4CommitGateway Cold(Func<string, bool>? fault = null,
                    bool withGenesis = true) => new EventV4CommitGateway(
                        serializer, new AtomicFileSaveStore(path, serializer, fault),
                        defs, policies, runner, automaticDailyChecks: true,
                        initialState: withGenesis ? seeded.State : null);
                var initial = Cold().Read().State!;
                var screen = new EventV4GameCommands(Cold(), content, economic);
                Assert(initial.Revision == 0 && initial.Name.Length == 0 &&
                    screen.Snapshot().Revision == 0 &&
                    !File.Exists(path), "reading first-run V4 cannot persist a seed");
                Assert(Cold().Offer(seed.RunId, "premature-genesis-offer", 0).ReasonKey ==
                        "event.character_required" &&
                    Cold().ExecuteCore(new CommandEnvelope(seed.RunId,
                        "premature-playback", 0,
                        new GameCommand(CommandKind.AcknowledgePlayback))).ReasonKey ==
                        "event.character_required" &&
                    !File.Exists(path),
                    "only a real character receipt may persist the fresh V4 seed");
                var create = new CommandEnvelope(seed.RunId, "fresh-v4-create", 0,
                    new GameCommand(CommandKind.CreateCharacter, "fresh", 25,
                        "V4 Founder", "base.female"));
                Assert(Cold(point => point == "during-temp-write").ExecuteCore(create).Status ==
                        EventCommitStatus.PersistenceFailed &&
                    !File.Exists(path) && Cold().Read().State!.Revision == 0,
                    "failure before first promotion preserves missing save and genesis");
                Assert(Cold(point => point == "after-replace").ExecuteCore(create).Status ==
                        EventCommitStatus.RecoveryRequired && File.Exists(path),
                    "ambiguous first promotion cannot claim an unverified receipt");
                var checkpoint = File.ReadAllBytes(path);
                var persisted = Cold(withGenesis: false).Read().State!;
                Assert(persisted.Name == "V4 Founder" &&
                    persisted.Revision == 1 && persisted.Receipts.Count == 1 &&
                    persisted.SaveVersion == 4 &&
                    persisted.EventV4!.Journal!.Revision == 0 &&
                    persisted.EventV4.Journal.Tickets.Count == 0 &&
                    Cold().ExecuteCore(create).Status == EventCommitStatus.AlreadyCommitted &&
                    File.ReadAllBytes(path).SequenceEqual(checkpoint),
                    "cold retry must recover exactly one first V3 receipt in schema4");
                // Corrupt/unreadable source is never interpreted as an empty run.
                File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
                Assert(Cold().Read().Status != LoadStatus.Valid &&
                    Cold().ExecuteCore(new CommandEnvelope(seed.RunId, "attempt-over-corrupt",
                        0, new GameCommand(CommandKind.CreateCharacter, "fresh", 25,
                            "Wrong", "base.female"))).Status != EventCommitStatus.Committed,
                    "genesis fallback must never erase a corrupt existing checkpoint");
            }
            finally
            {
                try { Directory.Delete(dir, true); }
                catch (IOException) { }
            }
        });
        Check("M15 direct V4 work on an ineligible day commits one negative check", () =>
        {
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var economics = M9T02FunctionalEconomy.Create();
            var archives = new BundledHistoricalV2ContentResolver();
            var replay = new VersionedReceiptReplay(economics, content);
            var v3 = new V3SaveCompatibilitySerializer(content,
                GameSession.CreateRestoreValidator(), new V2ToV3Migration(archives, replay),
                new StagedV3SaveSerializer(archives, replay));
            var definitions = new[] { new GameEventDefinition(
                "employment-only", "career", 2026, 0, 1, 0, 1, 1, 0,
                new[] { "employed" }, Array.Empty<string>(),
                new[] { new EventChoiceDefinition("continue", "event.continue",
                    "outcome.continue") }) };
            var policies = new[] { new EventCategoryPolicy("career", 0, 7, 1) };
            var serializer = new EventV4CompatibilitySerializer(
                new StagedEventV4SaveSerializer(v3, definitions, policies));
            var runner = new EventV4CoreCommandRunner(content, v3, economics, archives);
            var initial = GameSession.NewState(content, "m15-no-event-work", 37721UL,
                new SimDate(2026, 10, 1));
            var folder = Path.Combine(Path.GetTempPath(),
                "m15-work-no-event-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, "game.save");
            try
            {
                File.WriteAllBytes(file, JsonSaveSerializer.Wrap(
                    JsonSaveSerializer.WriteObject(initial), 2, 0));
                EventV4CommitGateway Cold(Func<string, bool>? fault = null,
                    bool withRunner = true) => new EventV4CommitGateway(
                        serializer, new AtomicFileSaveStore(file, serializer, fault),
                        definitions, policies, withRunner ? runner : null,
                        automaticDailyChecks: true);
                var create = Cold().ExecuteCore(new CommandEnvelope(initial.RunId,
                    "create-no-event-work", 0, new GameCommand(CommandKind.CreateCharacter,
                        "fresh", 25, "No Event Tester", "base.female")));
                Assert(create.Status == EventCommitStatus.Committed,
                    "real V3 character receipt precedes workday");
                var first = Cold().Read().State!;
                var boundary = new CommandEnvelope(initial.RunId, "direct-work-no-event",
                    first.Revision, new GameCommand(CommandKind.AdvanceBoundary));
                var initialBytes = File.ReadAllBytes(file);
                Assert(Cold(withRunner: false).ExecuteCore(boundary).ReasonKey ==
                        "save.v4_core_evaluator_unavailable" &&
                    File.ReadAllBytes(file).SequenceEqual(initialBytes),
                    "unavailable V3 evaluator may not commit even a negative daily check");
                Assert(Cold(x => x == "during-temp-write").ExecuteCore(boundary).Status ==
                        EventCommitStatus.PersistenceFailed &&
                    File.ReadAllBytes(file).SequenceEqual(initialBytes),
                    "failed first daily check cannot move clock or rewrite save");
                var advanced = Cold().ExecuteCore(boundary);
                var after = Cold().Read().State!;
                Assert(advanced.Status == EventCommitStatus.Committed &&
                    after.EventV4!.Journal!.NoEligibleDays.Count == 1 &&
                    after.EventV4.Journal.NoEligibleDays[0].DateIso == first.DateIso &&
                    after.EventV4.Journal.Tickets.Count == 0 &&
                    after.Receipts.Count == first.Receipts.Count + 1 &&
                    after.Minute > first.Minute && after.DateIso == first.DateIso &&
                    after.Revision == first.Revision + 2,
                    "one negative V4 day scan and one real V3 boundary are committed");
                var committed = File.ReadAllBytes(file);
                Assert(Cold().ExecuteCore(boundary).Status ==
                        EventCommitStatus.AlreadyCommitted &&
                    File.ReadAllBytes(file).SequenceEqual(committed),
                    "cold retry of direct work boundary never consumes additional minutes");
                var later = Cold().ExecuteCore(new CommandEnvelope(initial.RunId,
                    "same-day-next-boundary", after.Revision,
                    new GameCommand(CommandKind.AdvanceBoundary)));
                var next = Cold().Read().State!;
                Assert(later.Status == EventCommitStatus.Committed &&
                    next.EventV4!.Journal!.NoEligibleDays.Count == 1 &&
                    next.Receipts.Count == first.Receipts.Count + 2,
                    "next working step on the same checked date never rechecks the window");
            }
            finally
            {
                try { Directory.Delete(folder, true); }
                catch (IOException) { }
            }
        });
        Check("M15 work-shift boundary checks daily event and resumes after cold choice", () =>
        {
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var economics = M9T02FunctionalEconomy.Create();
            var archives = new BundledHistoricalV2ContentResolver();
            var replay = new VersionedReceiptReplay(economics, content);
            var v3 = new V3SaveCompatibilitySerializer(content,
                GameSession.CreateRestoreValidator(), new V2ToV3Migration(archives, replay),
                new StagedV3SaveSerializer(archives, replay));
            var definitions = new[] { new GameEventDefinition(
                "workday-prompt", "life", 2026, 0, 1, 0, 1, 1, 0,
                Array.Empty<string>(), Array.Empty<string>(),
                new[] { new EventChoiceDefinition("continue", "event.continue",
                    "outcome.continue") }) };
            var policies = new[] { new EventCategoryPolicy("life", 0, 10, 2) };
            var serializer = new EventV4CompatibilitySerializer(
                new StagedEventV4SaveSerializer(v3, definitions, policies));
            var runner = new EventV4CoreCommandRunner(content, v3, economics, archives);
            var initial = GameSession.NewState(content, "m15-workshift", 74011UL,
                new SimDate(2026, 10, 1));
            var folder = Path.Combine(Path.GetTempPath(),
                "m15-workshift-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, "game.save");
            try
            {
                File.WriteAllBytes(file, JsonSaveSerializer.Wrap(
                    JsonSaveSerializer.WriteObject(initial), 2, 0));
                EventV4CommitGateway Cold() => new EventV4CommitGateway(
                    serializer, new AtomicFileSaveStore(file, serializer),
                    definitions, policies, runner, automaticDailyChecks: true);
                var create = Cold().ExecuteCore(new CommandEnvelope(initial.RunId,
                    "create-workshift", 0, new GameCommand(CommandKind.CreateCharacter,
                        "fresh", 25, "Work Tester", "base.female")));
                Assert(create.Status == EventCommitStatus.Committed,
                    "real character is created by V3 receipt before working");
                var initialCore = Cold().Read().State!;
                var boundary = new CommandEnvelope(initial.RunId, "work-boundary",
                    initialCore.Revision, new GameCommand(CommandKind.AdvanceBoundary));
                var blocked = Cold().ExecuteCore(boundary);
                var pending = Cold().ReadPendingChoice() ??
                    throw new InvalidOperationException("work-triggered choice not persisted");
                var afterOffer = Cold().Read().State!;
                Assert(blocked.Status == EventCommitStatus.Rejected &&
                    blocked.ReasonKey == "event.choice_required" &&
                    pending.EventId == "workday-prompt" &&
                    afterOffer.Receipts.Count == initialCore.Receipts.Count &&
                    afterOffer.DateIso == initialCore.DateIso &&
                    afterOffer.Minute == initialCore.Minute,
                    "work action must open a durable event before consuming time");
                var offeredBytes = File.ReadAllBytes(file);
                Assert(Cold().ExecuteCore(new CommandEnvelope(initial.RunId,
                    "work-boundary-after-offer", afterOffer.Revision,
                    new GameCommand(CommandKind.AdvanceBoundary))).ReasonKey ==
                    "event.choice_required" &&
                    File.ReadAllBytes(file).SequenceEqual(offeredBytes),
                    "work cannot skip pending decision with a different command ID");
                Assert(Cold().Resolve(initial.RunId, "resolve-work-prompt",
                    afterOffer.Revision, pending.TicketId, "continue").Status ==
                    EventCommitStatus.Committed,
                    "event outcome is committed exactly once before work resumes");
                var resolved = Cold().Read().State!;
                var resume = new CommandEnvelope(initial.RunId, "work-after-choice",
                    resolved.Revision, new GameCommand(CommandKind.AdvanceBoundary));
                var advanced = Cold().ExecuteCore(resume);
                var completed = Cold().Read().State!;
                Assert(advanced.Status == EventCommitStatus.Committed &&
                    completed.Receipts.Count == initialCore.Receipts.Count + 1 &&
                    completed.Minute > initialCore.Minute &&
                    completed.EventV4!.Journal!.Tickets.Count == 1 &&
                    completed.EventV4.Journal.OutcomeReceipts.Count == 1 &&
                    completed.EventV4.Journal.NoEligibleDays.Count == 0,
                    "work resumes V3 clock on already-checked day with no new ticket");
                var bytes = File.ReadAllBytes(file);
                Assert(Cold().ExecuteCore(resume).Status ==
                        EventCommitStatus.AlreadyCommitted &&
                    File.ReadAllBytes(file).SequenceEqual(bytes),
                    "cold work boundary retry must not spend time or duplicate receipts");
            }
            finally
            {
                try { Directory.Delete(folder, true); }
                catch (IOException) { }
            }
        });
        Check("M15 opt-in daily event trigger stops batch and resumes after cold choice", () =>
        {
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var economics = M9T02FunctionalEconomy.Create();
            var historical = new BundledHistoricalV2ContentResolver();
            var replay = new VersionedReceiptReplay(economics, content);
            var v3 = new V3SaveCompatibilitySerializer(content,
                GameSession.CreateRestoreValidator(), new V2ToV3Migration(historical, replay),
                new StagedV3SaveSerializer(historical, replay));
            var definitions = new[] { new GameEventDefinition(
                "one-time-day", "life", 2026, 1, 1, 0, 1, 1, 0,
                new[] { "character-created" }, Array.Empty<string>(),
                new[] { new EventChoiceDefinition("ack", "event.ack", "outcome.ack") }) };
            var policies = new[] { new EventCategoryPolicy("life", 0, 30, 4) };
            var serializer = new EventV4CompatibilitySerializer(
                new StagedEventV4SaveSerializer(v3, definitions, policies));
            var runner = new EventV4CoreCommandRunner(content, v3, economics, historical);
            var source = GameSession.NewState(content, "run-v4-auto", 59137UL,
                new SimDate(2026, 10, 30));
            var folder = Path.Combine(Path.GetTempPath(),
                "m15-auto-day-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, "game.save");
            try
            {
                File.WriteAllBytes(file, JsonSaveSerializer.Wrap(
                    JsonSaveSerializer.WriteObject(source), 2, 0));
                var missing = new EventV4CommitGateway(serializer,
                    new AtomicFileSaveStore(file + ".missing", serializer),
                    definitions, policies, runner, automaticDailyChecks: true);
                var missingPort = new EventV4GameCommands(missing, content, economics);
                Assert(missingPort.AdvanceDay(new CommandEnvelope(source.RunId,
                    "missing-adapter-day", 0,
                    new GameCommand(CommandKind.AdvanceBoundary))).StopReason ==
                    "RecoveryRequired",
                    "missing V4 checkpoint is reported by gateway, not thrown by UI adapter");
                EventV4CommitGateway Cold() => new EventV4CommitGateway(
                    serializer, new AtomicFileSaveStore(file, serializer),
                    definitions, policies, runner, automaticDailyChecks: true);
                var created = Cold().ExecuteCore(new CommandEnvelope(source.RunId,
                    "auto-create", 0, new GameCommand(CommandKind.CreateCharacter,
                        "fresh", 25, "Auto Test", "base.female")));
                Assert(created.Status == EventCommitStatus.Committed && created.Revision == 1,
                    "auto event test creates a real V3 character receipt");
                var root = new CommandEnvelope(source.RunId, "auto-day-root",
                    created.Revision, new GameCommand(CommandKind.AdvanceBoundary));
                var blocked = Cold().AdvanceDay(root);
                Assert(blocked.StopReason == "PlayerChoice" && blocked.Boundaries.Count == 0,
                    "day batch must open the deterministic event before spending time");
                var pending = Cold().ReadPendingChoice() ??
                    throw new InvalidOperationException("auto offered ticket not cold-restored");
                var offered = Cold().Read().State!;
                Assert(pending.EventId == "one-time-day" &&
                    offered.EventV4!.Journal!.AdvanceBatches.Count == 1 &&
                    offered.EventV4.Journal.Tickets.Count == 1 &&
                    offered.DateIso == "2026-10-30" &&
                    offered.EventV4.Journal.NoEligibleDays.Count == 0,
                    "event offer and durable root survive independent cold validation");
                var bytesBeforeChoice = File.ReadAllBytes(file);
                Assert(Cold().AdvanceDay(root).StopReason == "PlayerChoice" &&
                    File.ReadAllBytes(file).SequenceEqual(bytesBeforeChoice) &&
                    Cold().ExecuteCore(new CommandEnvelope(source.RunId, "auto-bypass",
                        offered.Revision,
                        new GameCommand(CommandKind.AdvanceBoundary))).Status ==
                        EventCommitStatus.Rejected,
                    "pending choice and direct boundary bypass cannot double-advance");
                Assert(Cold().Resolve(source.RunId, "auto-resolve", pending.Revision,
                    pending.TicketId, "ack").Status == EventCommitStatus.Committed,
                    "pending event must resolve with its authenticated outcome");
                var presentation = new EventV4GameCommands(Cold(), content, economics);
                var newUiRequest = new CommandEnvelope(source.RunId,
                    "new-ui-id-after-cold-restart", Cold().Read().State!.Revision,
                    new GameCommand(CommandKind.AdvanceBoundary));
                var reached = presentation.AdvanceDay(newUiRequest);
                Assert(reached.StopReason == "TargetReached" &&
                    reached.ReachedInstant.Date.ToString() == "2026-10-31" &&
                    Cold().Read().State!.EventV4!.Journal!.AdvanceBatches.Count == 1,
                    "new UI command ID must resume cold-stored batch root exactly once");
                Assert(Cold().AdvanceDay(root).StopReason == "TargetReached",
                    "original root remains idempotent after UI-driven recovery");
                var onNewDate = Cold().Read().State!;
                Assert(onNewDate.EventV4!.Journal!.Tickets.Count == 1 &&
                    onNewDate.EventV4.Journal.OutcomeReceipts.Count == 1,
                    "completed event remains exactly once across batch child receipts");
                var secondRoot = new CommandEnvelope(source.RunId, "auto-second-root",
                    onNewDate.Revision, new GameCommand(CommandKind.AdvanceBoundary));
                Assert(Cold().AdvanceDay(secondRoot).StopReason == "TargetReached",
                    "subsequent day advances after an ineligible automatic window");
                var next = Cold().Read().State!;
                var savedBytes = File.ReadAllBytes(file);
                Assert(next.EventV4!.Journal!.NoEligibleDays.Count == 1 &&
                    next.EventV4.Journal.NoEligibleDays[0].DateIso == "2026-10-31" &&
                    next.EventV4.Journal.Tickets.Count == 1 &&
                    next.EventRng == onNewDate.EventRng &&
                    Cold().AdvanceDay(secondRoot).StopReason == "TargetReached" &&
                    File.ReadAllBytes(file).SequenceEqual(savedBytes),
                    "automatic empty day is cold replayable and original batch retries read-only");
                // Exercise the actual V3 economic ledger via the same V4 CAS
                // after an automatic event window, not an empty UI portfolio.
                var launchRequest = new CommandEnvelope(source.RunId,
                    "auto-business-launch", next.Revision,
                    new GameCommand(CommandKind.LaunchBusiness, "online-store", 100,
                        appearanceId: "v1"));
                var launched = Cold().ExecuteCore(launchRequest);
                Assert(launched.Status == EventCommitStatus.Committed,
                    "verified V4 must still launch a real V3 business: " + launched.ReasonKey);
                var view = new EventV4GameCommands(Cold(), content, economics);
                var portfolio = view.ReadBusinesses();
                var operations = view.ReadBusinessOperations();
                var dashboard = view.ReadLifeDashboard();
                var financial = view.ReadCommittedFinance();
                Assert(portfolio.Businesses.Count == 1 &&
                    portfolio.Businesses[0].DefinitionId == "online-store" &&
                    portfolio.Game.Revision == launched.Revision &&
                    operations.Portfolio.Game.Revision == launched.Revision &&
                    operations.OwnedBusinesses.Count == 1 &&
                    operations.OwnedBusinesses[0].Business.DefinitionId == "online-store" &&
                    dashboard.Game.Revision == launched.Revision &&
                    dashboard.Finance.CashVnd == portfolio.Game.Cash &&
                    financial != null,
                    "V4 UI must project the actual V3 business and wallet at one CAS frontier");
                var afterLaunchBytes = File.ReadAllBytes(file);
                Assert(Cold().ExecuteCore(launchRequest).Status ==
                        EventCommitStatus.AlreadyCommitted &&
                    File.ReadAllBytes(file).SequenceEqual(afterLaunchBytes),
                    "cold V4 business launch retry must never repeat an investment");
            }
            finally
            {
                try { Directory.Delete(folder, true); }
                catch (IOException) { }
            }
        });
        Check("M15 time-consuming event commits one verified V3 minute receipt and V4 choice", () =>
        {
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var rules = M9T02FunctionalEconomy.Create();
            var archives = new BundledHistoricalV2ContentResolver();
            var replay = new VersionedReceiptReplay(rules, content);
            var v3 = new V3SaveCompatibilitySerializer(content,
                GameSession.CreateRestoreValidator(), new V2ToV3Migration(archives, replay),
                new StagedV3SaveSerializer(archives, replay));
            var options = new[]
            {
                new EventChoiceDefinition("prepare", "event.prepare", "outcome.prepare",
                    reward: new EventChoiceReward(0, 0, 60)),
                new EventChoiceDefinition("decline", "event.decline", "outcome.decline")
            };
            var definitions = new[] { new GameEventDefinition("evening-preparation", "life",
                2026, 0, 1, 0, 1, 1, 0, Array.Empty<string>(),
                Array.Empty<string>(), options) };
            var policies = new[] { new EventCategoryPolicy("life", 0, 7, 1) };
            var serializer = new EventV4CompatibilitySerializer(
                new StagedEventV4SaveSerializer(v3, definitions, policies));
            var runner = new EventV4CoreCommandRunner(content, v3, rules, archives);
            var seed = GameSession.NewState(content, "event-time-cas", 66219UL,
                new SimDate(2026, 10, 1));
            var dir = Path.Combine(Path.GetTempPath(), "m15-event-time-" +
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "game.save");
            try
            {
                File.WriteAllBytes(path, JsonSaveSerializer.Wrap(
                    JsonSaveSerializer.WriteObject(seed), 2, 0));
                EventV4CommitGateway Cold() => new EventV4CommitGateway(serializer,
                    new AtomicFileSaveStore(path, serializer), definitions, policies, runner);
                var created = Cold().ExecuteCore(new CommandEnvelope(seed.RunId,
                    "time-create", 0, new GameCommand(CommandKind.CreateCharacter,
                        "fresh", 23, "Time Proof", "base.female")));
                Assert(created.Status == EventCommitStatus.Committed,
                    "character must commit before the time-consuming event");
                // The historical new-run clock starts at midnight. Use the
                // existing authenticated core boundary to reach waking hours
                // before checking or offering a real time-consuming choice.
                var wake = Cold().ExecuteCore(new CommandEnvelope(seed.RunId,
                    "time-wake", created.Revision,
                    new GameCommand(CommandKind.AdvanceBoundary)));
                Assert(wake.Status == EventCommitStatus.Committed &&
                    Cold().Read().State!.Minute == content.Schedule.WakeMinute,
                    "one committed V3 boundary must reach the waking free-time window");
                var offered = Cold().Offer(seed.RunId, "time-offer", wake.Revision);
                Assert(offered.Status == EventCommitStatus.Committed,
                    "one V4 ticket must be committed");
                var pending = Cold().ReadPendingChoice();
                Assert(pending != null && pending.UnavailableChoiceReasons.Count == 0,
                    "the verified 08:00 time window should admit 60 minutes");
                var before = Cold().Read().State!;
                var minute = before.Minute;
                var committedSource = File.ReadAllBytes(path);
                var tampered = EventRewardCompositeProof.Prepare(committedSource,
                    serializer, runner, definitions, policies, "time-confirm",
                    pending!.TicketId, "prepare");
                tampered.Core.Receipts.Last().MinutesConsumed = 59;
                Throws<ArgumentException>(() => EventRewardCompositeProof.Validate(
                    committedSource, tampered, serializer, runner,
                    definitions, policies));
                Assert(File.ReadAllBytes(path).SequenceEqual(committedSource),
                    "forged duration candidate must never publish any bytes");
                var finished = Cold().Resolve(seed.RunId, "time-confirm",
                    pending!.Revision, pending.TicketId, "prepare");
                Assert(finished.Status == EventCommitStatus.Committed,
                    "the time event must use one atomic composite CAS");
                var afterRead = Cold().Read();
                Assert(afterRead.Status == LoadStatus.Valid && afterRead.State != null,
                    "the time receipt and event outcome must cold-replay");
                var after = afterRead.State!;
                Assert(after.Minute == minute + 60 &&
                    after.Cash == before.Cash &&
                    after.Receipts.Count == before.Receipts.Count + 1 &&
                    after.Receipts.Last().MinutesConsumed == 60 &&
                    after.PendingChoiceId.Length == 0 &&
                    after.EventV4!.Journal!.PendingTicketId.Length == 0,
                    "the V3 clock and receipt must advance once without cash mutation");
                var savedBytes = File.ReadAllBytes(path);
                var retry = Cold().Resolve(seed.RunId, "time-confirm",
                    pending.Revision, pending.TicketId, "prepare");
                Assert(retry.Status == EventCommitStatus.AlreadyCommitted &&
                    File.ReadAllBytes(path).SequenceEqual(savedBytes),
                    "cold retry cannot consume another minute or rewrite the checkpoint");

                // Independent V3 replay must still reject the inner receipt:
                // V4 outcome provenance is required to authorize any time debit.
                var inner = Convert.FromBase64String(after.EventV4!.EmbeddedV3EnvelopeBase64);
                Assert(v3.DeserializeAndValidate(inner).Status != LoadStatus.Valid,
                    "time debit cannot be replayed in an unpaired V3 save");
            }
            finally
            {
                try { Directory.Delete(dir, true); }
                catch (IOException) { }
            }
        });
        Check("M15 reward settlement plan derives from cold-verified V3/V4 CAS state", () =>
        {
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var economics = M9T02FunctionalEconomy.Create();
            var archives = new BundledHistoricalV2ContentResolver();
            var replay = new VersionedReceiptReplay(economics, content);
            var v3 = new V3SaveCompatibilitySerializer(content,
                GameSession.CreateRestoreValidator(), new V2ToV3Migration(archives, replay),
                new StagedV3SaveSerializer(archives, replay));
            var defs = new[] { new GameEventDefinition("verified-reward", "life",
                2026, 0, 1, 0, 1, 1, 0, Array.Empty<string>(), Array.Empty<string>(),
                new[]
                {
                    new EventChoiceDefinition("receive", "event.receive", "outcome.receive",
                        reward: new EventChoiceReward(125000, 0)),
                    new EventChoiceDefinition("skip", "event.skip", "outcome.skip")
                }) };
            var policies = new[] { new EventCategoryPolicy("life", 0, 7, 1) };
            var serializer = new EventV4CompatibilitySerializer(
                new StagedEventV4SaveSerializer(v3, defs, policies));
            var runner = new EventV4CoreCommandRunner(content, v3, economics, archives);
            var seed = GameSession.NewState(content, "reward-real-frontier", 81097UL,
                new SimDate(2026, 10, 1));
            var folder = Path.Combine(Path.GetTempPath(),
                "m15-reward-frontier-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, "game.save");
            try
            {
                File.WriteAllBytes(path, JsonSaveSerializer.Wrap(
                    JsonSaveSerializer.WriteObject(seed), 2, 0));
                EventV4CommitGateway Cold() => new EventV4CommitGateway(
                    serializer, new AtomicFileSaveStore(path, serializer),
                    defs, policies, runner);
                var created = Cold().ExecuteCore(new CommandEnvelope(seed.RunId,
                    "reward-character", 0, new GameCommand(CommandKind.CreateCharacter,
                        "fresh", 25, "Reward Proof", "base.female")));
                Assert(created.Status == EventCommitStatus.Committed,
                    "real V3 receipt must precede reward planning");
                var offered = Cold().Offer(seed.RunId, "offer-reward-proof",
                    created.Revision);
                Assert(offered.Status == EventCommitStatus.Committed,
                    "pending reward ticket is persisted by V4 CAS");
                var read = Cold().Read();
                Assert(read.Status == LoadStatus.Valid && read.State != null,
                    "reward plan requires independently cold-validated state");
                var current = read.State!;
                var before = File.ReadAllBytes(path);
                var plan = EventDecisionJournal.PlanReward(current, defs, policies,
                    "settle-reward-proof", offered.TicketId, "receive");
                // Reconstruct BOTH proposed ledgers from the actual cold V4
                // checkpoint, then reject any independent money/XP/journal edits.
                var pair = EventRewardCompositeProof.Prepare(before, serializer, runner,
                    defs, policies, "settle-reward-proof", offered.TicketId, "receive");
                EventRewardCompositeProof.Validate(before, pair, serializer, runner,
                    defs, policies);
                // A genuine V4 wire now cold-replays BOTH the native V3 reward
                // receipt and the independently resolved V4 event outcome.
                EventV4Payload PairPayload(GameState core, EventJournalState outcome)
                {
                    var inner = JsonSaveSerializer.Wrap(
                        JsonSaveSerializer.WriteObject(core.EconomicV3!),
                        3, core.Revision);
                    using (var sha = System.Security.Cryptography.SHA256.Create())
                        return new EventV4Payload
                        {
                            EmbeddedV3EnvelopeBase64 = Convert.ToBase64String(inner),
                            EmbeddedV3EnvelopeSha256 = BitConverter.ToString(
                                sha.ComputeHash(inner)).Replace("-", "").ToLowerInvariant(),
                            Journal = outcome
                        };
                }
                var stagedWire = new StagedEventV4SaveSerializer(v3, defs, policies);
                var combinedWire = stagedWire.SerializeVerified(PairPayload(pair.Core, pair.Journal));
                var combinedRead = serializer.DeserializeAndValidate(combinedWire);
                Assert(combinedRead.Status == LoadStatus.Valid &&
                    combinedRead.State != null &&
                    combinedRead.State.Cash == checked(current.Cash + plan.CashDeltaVnd) &&
                    combinedRead.State.Revision == plan.GlobalRevision &&
                    combinedRead.State.PendingChoiceId.Length == 0 &&
                    combinedRead.State.Receipts.Last().CommandId == plan.CoreReceiptCommandId,
                    "joint cold replay must admit one authenticated cash reward and resolution");
                Assert(v3.DeserializeAndValidate(Convert.FromBase64String(
                        PairPayload(pair.Core, pair.Journal).EmbeddedV3EnvelopeBase64))
                        .Status != LoadStatus.Valid,
                    "standalone V3 replay must still refuse the same reward receipt");
                var absentCore = v3.DeserializeAndValidate(Convert.FromBase64String(
                    current.EventV4!.EmbeddedV3EnvelopeBase64)).State!;
                Throws<ArgumentException>(() => stagedWire.SerializeVerified(
                    PairPayload(absentCore, pair.Journal)));
                var mismatchedPair = EventRewardCompositeProof.Prepare(before, serializer,
                    runner, defs, policies, "settle-reward-proof", offered.TicketId, "receive");
                mismatchedPair.Core.Cash++;
                Throws<ArgumentException>(() => stagedWire.SerializeVerified(
                    PairPayload(mismatchedPair.Core, mismatchedPair.Journal)));
                var wrongOutcomePair = EventRewardCompositeProof.Prepare(before, serializer,
                    runner, defs, policies, "settle-reward-proof", offered.TicketId, "receive");
                wrongOutcomePair.Journal.Tickets.Last().OutcomeId = "outcome.forged";
                Throws<ArgumentException>(() => stagedWire.SerializeVerified(
                    PairPayload(wrongOutcomePair.Core, wrongOutcomePair.Journal)));
                var alteredFinancialChoice = new EventChoiceDefinition("receive",
                    "event.receive", "outcome.receive",
                    reward: new EventChoiceReward(125001, 0));
                var alteredCatalog = new[] { new GameEventDefinition("verified-reward",
                    "life", 2026, 0, 1, 0, 1, 1, 0,
                    Array.Empty<string>(), Array.Empty<string>(),
                    new[] { alteredFinancialChoice,
                        new EventChoiceDefinition("skip", "event.skip", "outcome.skip") }) };
                Assert(new EventV4CompatibilitySerializer(
                    new StagedEventV4SaveSerializer(v3, alteredCatalog, policies))
                    .DeserializeAndValidate(combinedWire).Status != LoadStatus.Valid,
                    "altered authored reward must not replay an existing ledger amount");
                Assert(File.ReadAllBytes(path).SequenceEqual(before),
                    "joint cold replay is read-only until a proven composite CAS is available");
                Assert(pair.Plan.CoreReceiptCommandId == plan.CoreReceiptCommandId &&
                    pair.Plan.GlobalRevision == current.Revision + 2 &&
                    pair.Core.Revision + pair.Journal.Revision == pair.Plan.GlobalRevision &&
                    pair.Journal.PendingTicketId.Length == 0 &&
                    pair.Journal.Tickets.Last().ChoiceId == "receive" &&
                    pair.Journal.OutcomeReceipts.Last().OutcomeId == "outcome.receive",
                    "paired reward proposal must bind exactly one V3 receipt and one V4 outcome");
                var forgedCash = EventRewardCompositeProof.Prepare(before, serializer,
                    runner, defs, policies, "settle-reward-proof", offered.TicketId, "receive");
                forgedCash.Core.Cash++;
                Throws<ArgumentException>(() => EventRewardCompositeProof.Validate(before,
                    forgedCash, serializer, runner, defs, policies));
                var forgedReceipt = EventRewardCompositeProof.Prepare(before, serializer,
                    runner, defs, policies, "settle-reward-proof", offered.TicketId, "receive");
                forgedReceipt.Core.Receipts.Last().Payload =
                    new GameCommand(CommandKind.AcknowledgePlayback).CanonicalPayload;
                Throws<ArgumentException>(() => EventRewardCompositeProof.Validate(before,
                    forgedReceipt, serializer, runner, defs, policies));
                var forgedLedger = EventRewardCompositeProof.Prepare(before, serializer,
                    runner, defs, policies, "settle-reward-proof", offered.TicketId, "receive");
                forgedLedger.Core.Ledger.Last().CashDelta++;
                Throws<ArgumentException>(() => EventRewardCompositeProof.Validate(before,
                    forgedLedger, serializer, runner, defs, policies));
                var forgedChoice = EventRewardCompositeProof.Prepare(before, serializer,
                    runner, defs, policies, "settle-reward-proof", offered.TicketId, "receive");
                forgedChoice.Journal.Tickets.Last().ChoiceId = "skip";
                Throws<ArgumentException>(() => EventRewardCompositeProof.Validate(before,
                    forgedChoice, serializer, runner, defs, policies));
                var forgedOutcome = EventRewardCompositeProof.Prepare(before, serializer,
                    runner, defs, policies, "settle-reward-proof", offered.TicketId, "receive");
                forgedOutcome.Journal.OutcomeReceipts.Last().OutcomeId = "outcome.forged";
                Throws<ArgumentException>(() => EventRewardCompositeProof.Validate(before,
                    forgedOutcome, serializer, runner, defs, policies));
                var forgedEconomic = EventRewardCompositeProof.Prepare(before, serializer,
                    runner, defs, policies, "settle-reward-proof", offered.TicketId, "receive");
                forgedEconomic.Core.EconomicV3!.EconomicRecords!.V3RecognizedRevenueVnd++;
                Throws<ArgumentException>(() => EventRewardCompositeProof.Validate(before,
                    forgedEconomic, serializer, runner, defs, policies));
                Assert(File.ReadAllBytes(path).SequenceEqual(before),
                    "all composite proof attempts must remain read-only");
                var candidateReward = runner.PrepareRewardReceiptCandidate(
                    Convert.FromBase64String(current.EventV4!.EmbeddedV3EnvelopeBase64), plan);
                var payoutReceipt = candidateReward.Receipts.Last();
                Assert(candidateReward.Revision == plan.CoreRevision &&
                    candidateReward.Cash == checked(current.Cash + plan.CashDeltaVnd) &&
                    payoutReceipt.CommandId == plan.CoreReceiptCommandId &&
                    payoutReceipt.CommandId != plan.ResolutionCommandId &&
                    payoutReceipt.Payload == plan.ToCoreCommand().CanonicalPayload &&
                    payoutReceipt.Revision == plan.CoreRevision &&
                    payoutReceipt.MinutesConsumed == 0 &&
                    candidateReward.CurrentActivity == payoutReceipt.OperationId &&
                    candidateReward.PlaybackCursor == 0 &&
                    payoutReceipt.Cue == candidateReward.CurrentCue &&
                    candidateReward.Receipts.Count == current.Receipts.Count + 1 &&
                    candidateReward.EconomicV3?.Current == candidateReward &&
                    current.Cash != candidateReward.Cash &&
                    File.ReadAllBytes(path).SequenceEqual(before),
                    "V3-native reward candidate must have one canonical operation/receipt without changing disk");
                var forgedInner = JsonSaveSerializer.Wrap(
                    JsonSaveSerializer.WriteObject(candidateReward.EconomicV3!),
                    3, candidateReward.Revision);
                Assert(v3.DeserializeAndValidate(forgedInner).Status != LoadStatus.Valid,
                    "ordinary V3 cold replay must reject a reward without V4 proof");
                Assert(Cold().ExecuteCore(new CommandEnvelope(seed.RunId,
                        plan.CoreReceiptCommandId, current.Revision,
                        new GameCommand(CommandKind.AcknowledgePlayback))).ReasonKey ==
                        "command.reserved_id",
                    "V3-reward receipt namespace must be unavailable to public V4 commands");
                var encodedReward = plan.ToCoreCommand();
                Assert(plan.MatchesCoreCommand(
                        GameCommand.ParseCanonicalPayload(encodedReward.CanonicalPayload)) &&
                    Cold().ExecuteCore(new CommandEnvelope(seed.RunId,
                        "forged-public-payout", current.Revision, encodedReward)).ReasonKey ==
                        "event.reward_requires_v4_settlement" &&
                    File.ReadAllBytes(path).SequenceEqual(before),
                    "canonical reward intent cannot be executed as a public V3 command");
                Assert(plan.CoreRevision == current.Receipts.Count + 1L &&
                    plan.GlobalRevision == current.Revision + 2 &&
                    plan.ResolutionCommandId == "settle-reward-proof" &&
                    plan.CashDeltaVnd == 125000 && plan.CareerXpDelta == 0 &&
                    plan.TicketId == offered.TicketId &&
                    current.EventV4!.Journal!.PendingTicketId == offered.TicketId &&
                    File.ReadAllBytes(path).SequenceEqual(before),
                    "authenticated settlement planning is read-only and binds both frontiers");
                Assert(File.ReadAllBytes(path).SequenceEqual(before),
                    "planning and composite proof alone must not credit cash or consume a choice");
                var modified = serializer.DeserializeAndValidate(before).State!;
                modified.Revision++;
                Throws<ArgumentException>(() => EventDecisionJournal.PlanReward(
                    modified, defs, policies, "fake-settle", offered.TicketId, "receive"));
                Assert(File.ReadAllBytes(path).SequenceEqual(before),
                    "forged revision never mutates stored V3 or V4 receipts");

                // A failed write must leave both economic and journal records
                // unchanged, including the pending event and retry identity.
                var writeFailure = new EventV4CommitGateway(serializer,
                    new AtomicFileSaveStore(path, serializer,
                        stage => stage == "during-temp-write"), defs, policies, runner);
                var rejectedWrite = writeFailure.Resolve(seed.RunId,
                    "settle-reward-proof", current.Revision, offered.TicketId, "receive");
                Assert(rejectedWrite.Status == EventCommitStatus.PersistenceFailed &&
                    File.ReadAllBytes(path).SequenceEqual(before) &&
                    Cold().ReadPendingChoice()!.TicketId == offered.TicketId,
                    "a failed composite temp write cannot partially post money or consume a ticket");
                var settled = Cold().Resolve(seed.RunId, "settle-reward-proof",
                    current.Revision, offered.TicketId, "receive");
                Assert(settled.Status == EventCommitStatus.Committed &&
                    settled.Revision == current.Revision + 2L &&
                    settled.TicketId == offered.TicketId &&
                    settled.OutcomeId == "outcome.receive",
                    "reward and outcome must commit as one verified +2 CAS");
                var paid = Cold().Read().State!;
                Assert(paid.Cash == checked(current.Cash + plan.CashDeltaVnd) &&
                    paid.Revision == settled.Revision &&
                    paid.EventV4!.Journal!.PendingTicketId.Length == 0 &&
                    paid.Receipts.Last().CommandId == plan.CoreReceiptCommandId,
                    "cold restore after CAS must expose exactly one spendable reward");
                var paidBytes = File.ReadAllBytes(path);
                Assert(Cold().Resolve(seed.RunId, "settle-reward-proof",
                        current.Revision, offered.TicketId, "receive").Status ==
                        EventCommitStatus.AlreadyCommitted &&
                    File.ReadAllBytes(path).SequenceEqual(paidBytes),
                    "cold retry must not pay or resolve again");
                Assert(Cold().Resolve(seed.RunId, "different-reward-id",
                        current.Revision, offered.TicketId, "receive").Status ==
                        EventCommitStatus.Rejected &&
                    File.ReadAllBytes(path).SequenceEqual(paidBytes),
                    "a different command ID cannot reuse a settled event");
                // Gameplay must remain fully functional after a reward receipt:
                // its V3 suffix is verified only in the enclosing V4 journal scope.
                var playback = Cold().ExecuteCore(new CommandEnvelope(seed.RunId,
                    "post-reward-playback", paid.Revision,
                    new GameCommand(CommandKind.AcknowledgePlayback,
                        paid.CurrentActivity, 0)));
                Assert(playback.Status == EventCommitStatus.Committed &&
                    Cold().Read().State!.Cash == paid.Cash &&
                    Cold().Read().State!.Revision == paid.Revision + 1 &&
                    Cold().Read().Status == LoadStatus.Valid,
                    "ordinary V3 gameplay receipt must cold replay after the V4 reward");

                // Crash after physical promotion: the caller cannot know
                // whether the payout committed, but a new cold session can.
                var ambiguousPath = Path.Combine(folder, "ambiguous.save");
                File.WriteAllBytes(ambiguousPath, before);
                var ambiguousGateway = new EventV4CommitGateway(serializer,
                    new AtomicFileSaveStore(ambiguousPath, serializer,
                        stage => stage == "after-replace"), defs, policies, runner);
                var ambiguous = ambiguousGateway.Resolve(seed.RunId,
                    "settle-reward-proof", current.Revision, offered.TicketId, "receive");
                Assert(ambiguous.Status == EventCommitStatus.RecoveryRequired,
                    "post-promotion fault must never return definite failure or success");
                EventV4CommitGateway ColdAmbiguous() => new EventV4CommitGateway(
                    serializer, new AtomicFileSaveStore(ambiguousPath, serializer),
                    defs, policies, runner);
                var recoveredReward = ColdAmbiguous().Read();
                Assert(recoveredReward.Status == LoadStatus.Valid &&
                    recoveredReward.State != null &&
                    recoveredReward.State.Cash == checked(current.Cash + plan.CashDeltaVnd) &&
                    recoveredReward.State.Revision == current.Revision + 2L &&
                    recoveredReward.State.EventV4!.Journal!.PendingTicketId.Length == 0,
                    "cold recovery must discover the single promoted composite");
                var promotedBytes = File.ReadAllBytes(ambiguousPath);
                Assert(ColdAmbiguous().Resolve(seed.RunId, "settle-reward-proof",
                        current.Revision, offered.TicketId, "receive").Status ==
                        EventCommitStatus.AlreadyCommitted &&
                    File.ReadAllBytes(ambiguousPath).SequenceEqual(promotedBytes),
                    "ambiguous retry must remain byte-stable and never credit twice");
            }
            finally
            {
                try { Directory.Delete(folder, true); }
                catch (IOException) { }
            }
        });
        Check("M15 financial reward choices reject without a V3 receipt and preserve pending save", () =>
        {
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var archives = new BundledHistoricalV2ContentResolver();
            var replay = new VersionedReceiptReplay();
            var v3 = new V3SaveCompatibilitySerializer(content,
                GameSession.CreateRestoreValidator(), new V2ToV3Migration(archives, replay),
                new StagedV3SaveSerializer(archives, replay));
            var definitions = new[] { new GameEventDefinition(
                "cash-test", "life", 2026, 0, 1, 0, 1, 1, 0,
                Array.Empty<string>(), Array.Empty<string>(), new[]
                {
                    new EventChoiceDefinition("reward", "event.reward", "outcome.reward",
                        reward: new EventChoiceReward(125000, 30)),
                    new EventChoiceDefinition("pass", "event.pass", "outcome.pass")
                }) };
            var policies = new[] { new EventCategoryPolicy("life", 0, 7, 1) };
            var strict = new StagedEventV4SaveSerializer(v3, definitions, policies);
            var serializer = new EventV4CompatibilitySerializer(strict);
            var initial = GameSession.NewState(content, "m15-reward-guard", 37919UL,
                new SimDate(2026, 10, 1));
            var dir = Path.Combine(Path.GetTempPath(),
                "m15-reward-guard-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "game.save");
            try
            {
                File.WriteAllBytes(path, JsonSaveSerializer.Wrap(
                    JsonSaveSerializer.WriteObject(initial), 2, 0));
                EventV4CommitGateway Cold() => new EventV4CommitGateway(
                    serializer, new AtomicFileSaveStore(path, serializer),
                    definitions, policies);
                var offered = Cold().Offer(initial.RunId, "reward-offer", 0);
                Assert(offered.Status == EventCommitStatus.Committed,
                    "reward-bearing choice must still offer without posting money");
                var before = Cold().Read().State!;
                var bytes = File.ReadAllBytes(path);
                var rejected = Cold().Resolve(initial.RunId, "reward-resolve",
                    offered.Revision, offered.TicketId, "reward");
                Assert(rejected.Status == EventCommitStatus.Rejected &&
                    rejected.ReasonKey == "event.reward_processor_unavailable" &&
                    Cold().Read().State!.Cash == before.Cash &&
                    Cold().Read().State!.Revision == before.Revision &&
                    Cold().ReadPendingChoice()!.TicketId == offered.TicketId &&
                    File.ReadAllBytes(path).SequenceEqual(bytes),
                    "unimplemented payout cannot consume choice, wallet or CAS revision");
                var safe = Cold().Resolve(initial.RunId, "reward-pass",
                    offered.Revision, offered.TicketId, "pass");
                Assert(safe.Status == EventCommitStatus.Committed &&
                    Cold().ReadPendingChoice() == null &&
                    Cold().Read().State!.Cash == before.Cash &&
                    Cold().Read().State!.EventV4!.Journal!.OutcomeReceipts.Count == 1,
                    "tag-only fallback must resolve once through existing V4 path");
                var afterBytes = File.ReadAllBytes(path);
                Assert(Cold().Resolve(initial.RunId, "reward-pass",
                    offered.Revision, offered.TicketId, "pass").Status ==
                    EventCommitStatus.AlreadyCommitted &&
                    File.ReadAllBytes(path).SequenceEqual(afterBytes),
                    "cold retry cannot resolve the fallback twice");
                var changed = new[] { new GameEventDefinition(
                    "cash-test", "life", 2026, 0, 1, 0, 1, 1, 0,
                    Array.Empty<string>(), Array.Empty<string>(), new[]
                    {
                        new EventChoiceDefinition("reward", "event.reward", "outcome.reward",
                            reward: new EventChoiceReward(125000, 30)),
                        new EventChoiceDefinition("pass", "event.pass", "outcome.pass",
                            reward: new EventChoiceReward(15000, 0)),
                        new EventChoiceDefinition("fallback", "event.fallback",
                            "outcome.fallback")
                    }) };
                // A catalog update must never retroactively credit a settled
                // tag-only outcome. The altered catalog is not cold-compatible.
                var incompatible = new StagedEventV4SaveSerializer(v3, changed, policies);
                var catalogMismatch = incompatible.DeserializeVerified(afterBytes);
                Assert(catalogMismatch.Status == LoadStatus.UnsupportedContent &&
                    catalogMismatch.ReasonKey == "save.event_catalog_unavailable" &&
                    File.ReadAllBytes(path).SequenceEqual(afterBytes),
                    "old resolved choices cannot acquire silent cash on catalog change");
            }
            finally
            {
                try { Directory.Delete(dir, true); }
                catch (IOException) { }
            }
        });
        Check("M15 V4 existing atomic store commits pending and resolve, with cold idempotent retry", () =>
        {
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var archived = new BundledHistoricalV2ContentResolver();
            var replay = new VersionedReceiptReplay();
            var v3 = new V3SaveCompatibilitySerializer(content, GameSession.CreateRestoreValidator(),
                new V2ToV3Migration(archived, replay),
                new StagedV3SaveSerializer(archived, replay));
            var eventDefinition = new GameEventDefinition("career-intro", "career", 2026, 0,
                1, 0, 5, 4, 0, Array.Empty<string>(), Array.Empty<string>(),
                new[] { new EventChoiceDefinition("continue", "event.continue", "outcome.continue") });
            var defs = new[] { eventDefinition };
            var policies = new[] { new EventCategoryPolicy("career", 0, 7, 3) };
            var active = new EventV4CompatibilitySerializer(
                new StagedEventV4SaveSerializer(v3, defs, policies));
            var initial = GameSession.NewState(content, "m15-cas", 47478UL,
                new SimDate(2026, 10, 1));
            var prior = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(initial), 2, 0);
            var root = Path.Combine(Path.GetTempPath(), "m15-cas-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var file = Path.Combine(root, "career.save");
            try
            {
                File.WriteAllBytes(file, prior);
                var store = new AtomicFileSaveStore(file, active);
                var gateway = new EventV4CommitGateway(active, store, defs, policies);
                Assert(gateway.Read().Status == LoadStatus.Valid &&
                    gateway.Read().SourceSchemaVersion == 2 && gateway.Read().State!.SaveVersion == 4,
                    "v2 read must migrate only in memory");
                Assert(File.ReadAllBytes(file).SequenceEqual(prior),
                    "V2 original must remain byte-identical before first command");
                var context = new GameEventContext(initial.Date, initial.Date, 0,
                    Array.Empty<string>(), Array.Empty<EventOccurrence>());
                Assert(gateway.Offer("m15-cas", "offer-a", 0, new GameEventContext(
                    initial.Date, initial.Date.AddDays(1), 0, Array.Empty<string>(),
                    Array.Empty<EventOccurrence>())).ReasonKey == "event.date_mismatch",
                    "event must not offer into a different V3 calendar day");
                var offered = gateway.Offer("m15-cas", "offer-a", 0);
                Assert(offered.Status == EventCommitStatus.Committed && offered.Revision == 1,
                    "first offer must commit through physical CAS");
                var persisted = File.ReadAllBytes(file);
                Assert(JsonSaveSerializer.ReadObject<SaveEnvelope>(persisted).SchemaVersion == 4,
                    "authorized offer must activate schema4");
                var loaded = gateway.Read();
                Assert(loaded.Status == LoadStatus.Valid &&
                    loaded.State!.PendingChoiceId == offered.TicketId &&
                    loaded.State!.EventRng != initial.EventRng,
                    "cold read must expose pending ticket and advanced event RNG");
                var payload = loaded.State!.EventV4!;
                Assert(Convert.FromBase64String(payload.EmbeddedV3EnvelopeBase64).Length > 0 &&
                    loaded.State!.SchedulerRng == initial.SchedulerRng,
                    "V3 economic checkpoint and career RNG must remain sealed");
                var coldGateway = new EventV4CommitGateway(active,
                    new AtomicFileSaveStore(file, active), defs, policies);
                Assert(coldGateway.Offer("m15-cas", "offer-a", 0, context).Status ==
                    EventCommitStatus.AlreadyCommitted, "cold offer retry must not apply twice");
                Assert(File.ReadAllBytes(file).SequenceEqual(persisted),
                    "cold retry must preserve every byte");
                var invalid = coldGateway.Resolve("m15-cas", "resolve-bad", 1, offered.TicketId, "not-a-choice");
                Assert(invalid.Status == EventCommitStatus.Rejected &&
                    File.ReadAllBytes(file).SequenceEqual(persisted),
                    "invalid choice cannot commit or change disk");
                var resolved = coldGateway.Resolve("m15-cas", "resolve-a", 1, offered.TicketId, "continue");
                Assert(resolved.Status == EventCommitStatus.Committed && resolved.Revision == 2 &&
                    resolved.OutcomeId == "outcome.continue", "resolve must persist once");
                var resolvedBytes = File.ReadAllBytes(file);
                Assert(coldGateway.Resolve("m15-cas", "resolve-a", 1, offered.TicketId, "continue").Status ==
                    EventCommitStatus.AlreadyCommitted, "resolved retry must remain idempotent");
                Assert(File.ReadAllBytes(file).SequenceEqual(resolvedBytes) &&
                    coldGateway.Read().State!.PendingChoiceId == "",
                    "resolved retry cannot change bytes or reopen a ticket");
                var forged = JsonSaveSerializer.ReadObject<GameState>(
                    JsonSaveSerializer.WriteObject(coldGateway.Read().State!));
                forged.SaveVersion = 4;
                forged.EventV4 = coldGateway.Read().State!.EventV4;
                forged.Cash++;
                Throws<ArgumentException>(() => active.Serialize(forged));
                Assert(File.ReadAllBytes(file).SequenceEqual(resolvedBytes),
                    "serializer must reject mutated cash even when journal is valid");
                var corrupt = JsonSaveSerializer.ReadObject<SaveEnvelope>(resolvedBytes);
                corrupt.Checksum = "not-authentic";
                File.WriteAllBytes(file, JsonSaveSerializer.WriteObject(corrupt));
                var recovered = coldGateway.Read();
                Assert(recovered.Status == LoadStatus.RecoveredBackup &&
                    recovered.State!.Revision == 1,
                    "corrupt primary should fall back to validated predecessor");
                Assert(File.ReadAllBytes(file).SequenceEqual(JsonSaveSerializer.WriteObject(corrupt)),
                    "cold recovery read must never rewrite corrupt primary");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch (System.IO.IOException) { }
            }
        });
        Check("M15 V4 CAS failed-write, ambiguous promotion and concurrent stale writer", () =>
        {
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var archived = new BundledHistoricalV2ContentResolver();
            var replay = new VersionedReceiptReplay();
            var v3 = new V3SaveCompatibilitySerializer(content, GameSession.CreateRestoreValidator(),
                new V2ToV3Migration(archived, replay),
                new StagedV3SaveSerializer(archived, replay));
            var defs = new[] { new GameEventDefinition("career-intro", "career", 2026, 0,
                1, 0, 5, 4, 0, Array.Empty<string>(), Array.Empty<string>(),
                new[] { new EventChoiceDefinition("continue", "event.continue", "outcome.continue") }) };
            var policies = new[] { new EventCategoryPolicy("career", 0, 7, 3) };
            var serializer = new EventV4CompatibilitySerializer(
                new StagedEventV4SaveSerializer(v3, defs, policies));
            var original = GameSession.NewState(content, "m15-fault", 3177UL,
                new SimDate(2026, 10, 1));
            var v2Bytes = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(original), 2, 0);
            var root = Path.Combine(Path.GetTempPath(), "m15-fault-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var file = Path.Combine(root, "event.save");
            try
            {
                File.WriteAllBytes(file, v2Bytes);
                var context = new GameEventContext(original.Date, original.Date, 0,
                    Array.Empty<string>(), Array.Empty<EventOccurrence>());
                var failed = new EventV4CommitGateway(serializer,
                    new AtomicFileSaveStore(file, serializer,
                        point => point == "during-temp-write"), defs, policies);
                Assert(failed.Offer("m15-fault", "offer-1", 0, context).Status ==
                    EventCommitStatus.PersistenceFailed &&
                    File.ReadAllBytes(file).SequenceEqual(v2Bytes),
                    "pre-promotion crash must not mutate committed V2 bytes");
                var ambiguous = new EventV4CommitGateway(serializer,
                    new AtomicFileSaveStore(file, serializer,
                        point => point == "after-replace"), defs, policies);
                Assert(ambiguous.Offer("m15-fault", "offer-1", 0, context).Status ==
                    EventCommitStatus.RecoveryRequired,
                    "post-promotion failure requires recovery, not retrying the write");
                var healthy = new EventV4CommitGateway(serializer,
                    new AtomicFileSaveStore(file, serializer), defs, policies);
                Assert(healthy.Read().State!.Revision == 1 &&
                    healthy.Offer("m15-fault", "offer-1", 0, context).Status ==
                        EventCommitStatus.AlreadyCommitted,
                    "post-promotion cold retry must detect committed offer");
                var committedBytes = File.ReadAllBytes(file);
                Assert(healthy.Offer("m15-fault", "offer-2", 0, context).Status ==
                    EventCommitStatus.Rejected &&
                    File.ReadAllBytes(file).SequenceEqual(committedBytes),
                    "stale writer must not replace committed choice");
                var resolver = new EventV4CommitGateway(serializer,
                    new AtomicFileSaveStore(file, serializer), defs, policies);
                var ticket = healthy.Read().State!.PendingChoiceId;
                Assert(resolver.Resolve("m15-fault", "resolve-1", 1, ticket, "continue").Status ==
                    EventCommitStatus.Committed, "follow-up writer must resolve after recovery");
                Assert(healthy.Resolve("m15-fault", "resolve-2", 1, ticket, "continue").Status ==
                    EventCommitStatus.Rejected, "second writer may not replay paid choice");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch (System.IO.IOException) { }
            }
        });
        Check("staged schema3 envelope validates checksum generation and future schema precedence", () =>
        {
            var legacy=GameSession.NewState(FirstPlayableContentTemplate.BuildCatalog(),
                "staged-wire-check",314159UL,new SimDate(2026,9,1));
            var staged=new V2ToV3Migration(new BundledHistoricalV2ContentResolver(),
                new VersionedReceiptReplay());
            var payload=EconomicV3PayloadCodec.ReadChecked(
                staged.Migrate(JsonSaveSerializer.WriteObject(legacy)));
            var serializer=new StagedV3SaveSerializer(
                new BundledHistoricalV2ContentResolver(),new VersionedReceiptReplay());
            var good=serializer.SerializeVerified(payload);
            var restored=serializer.DeserializeVerified(good);
            Assert(restored.Status==LoadStatus.Valid && restored.Payload!=null &&
                restored.Payload.OriginalV2PayloadSha256==payload.OriginalV2PayloadSha256,
                "v3 staged save did not round-trip");
            var envelope=JsonSaveSerializer.ReadObject<SaveEnvelope>(good);
            envelope.Generation++;
            Assert(serializer.DeserializeVerified(JsonSaveSerializer.WriteObject(envelope)).Status==
                LoadStatus.Corrupt,"different revision must fail even with matching checksum");
            envelope.Generation--;
            envelope.Checksum="0";
            Assert(serializer.DeserializeVerified(JsonSaveSerializer.WriteObject(envelope)).ReasonKey==
                "save.checksum","tampered checksum not identified");
            envelope.SchemaVersion=4;
            envelope.PayloadBase64="%% invalid %%";
            Assert(serializer.DeserializeVerified(JsonSaveSerializer.WriteObject(envelope)).Status==
                LoadStatus.FutureVersion,"future schema must take precedence over payload corruption");
            var unavailable=new StagedV3SaveSerializer(new UnavailableArchive(),new VersionedReceiptReplay());
            Assert(unavailable.DeserializeVerified(good).Status==LoadStatus.UnsupportedContent,
                "unknown archived ruleset must never silently fall back to current rules");
        });


        Check("v3 command wire appends Pause11 and Resume12 without changing v2 payload codes", () =>
        {
            Assert((int)CommandKind.SetBusinessPricing==9 && (int)CommandKind.CloseBusiness==10,
                "v2 command discriminants unchanged");
            Assert((int)CommandKind.PauseBusiness==11 && (int)CommandKind.ResumeBusiness==12,
                "new v3 command discriminants must be append-only");
            var pause=BusinessCommands.Pause("run/business/2");
            var resume=BusinessCommands.Resume("run/business/2");
            Assert(GameCommand.ParseCanonicalPayload(pause.CanonicalPayload).Kind==CommandKind.PauseBusiness,
                "pause roundtrip");
            Assert(GameCommand.ParseCanonicalPayload(resume.CanonicalPayload).Kind==CommandKind.ResumeBusiness,
                "resume roundtrip");
        });

        Check("v3 policy candidate mutates only prospective time and price", () =>
        {
            var candidate=new GameState
            {
                SaveVersion=3, DateIso="2026-10-10", Minute=1200,
                Businesses=new List<BusinessState>{
                    new BusinessState{InstanceId="run/business/1",DefinitionId="freelance-service",
                        DefinitionRevision="v1",OpenedIso="2026-10-08",OpenedMinute=0,
                        InitialInvestment=100,PricingPosture=PricingPosture.Standard}
                }
            };
            var records=EconomicV3Records.Empty();
            V3BusinessPolicyEngine.InitializeFromMigration(candidate,records);
            Assert(records.BusinessPolicies.Single().Status==EconomicOperationStatusCode.Auto,
                "historic active business starts Auto");
            V3BusinessPolicyEngine.ApplyCommand(candidate,records,
                BusinessCommands.SetPricing("run/business/1",PricingPosture.Premium),"op-price");
            Assert(candidate.Businesses.Single().PricingPosture==PricingPosture.Standard,
                "price cannot affect v3 current date");
            Assert(records.BusinessPolicies.Single().PendingPricing==PricingPosture.Premium &&
                records.BusinessPolicies.Single().PendingPricingEffectiveIso=="2026-10-11",
                "v3 pending price persisted");
            V3BusinessPolicyEngine.ApplyCommand(candidate,records,
                BusinessCommands.Pause("run/business/1"),"op-pause");
            Assert(records.BusinessPolicies.Single().Status==EconomicOperationStatusCode.Paused,
                "pause immediate");
            V3BusinessPolicyEngine.ApplyCommand(candidate,records,
                BusinessCommands.Resume("run/business/1"),"op-resume");
            Assert(records.BusinessPolicies.Single().HasPendingResume,
                "resume only on next date");
            candidate.DateIso="2026-10-11"; candidate.Minute=0;
            V3BusinessPolicyEngine.ActivateNewDate(candidate,records,"op-boundary");
            Assert(records.BusinessPolicies.Single().Status==EconomicOperationStatusCode.Auto,
                "auto activates at midnight");
            Assert(candidate.Businesses.Single().PricingPosture==PricingPosture.Premium,
                "effective price reflected in existing detached BusinessSnapshot");
            V3BusinessPolicyEngine.ApplyCommand(candidate,records,
                BusinessCommands.Close("run/business/1"),"op-close");
            Assert(records.BusinessPolicies.Single().Status==EconomicOperationStatusCode.Closed &&
                !candidate.Businesses.Single().IsActive,"close is terminal on existing instance");
            Throws<RuleFailure>(() => V3BusinessPolicyEngine.ApplyCommand(candidate,records,
                BusinessCommands.Resume("run/business/1"),"op-illegal"));
            V3BusinessPolicyEngine.ValidateAgainstBusinesses(candidate,records);
        });

        Check("v3 policy serialization retains pending fields and fails malformed flags", () =>
        {
            var record=new V3BusinessPolicyRecord {
                InstanceId="run/business/3",Status=EconomicOperationStatusCode.Paused,
                EffectivePricing=PricingPosture.Standard,
                HasPendingResume=true,PendingResumeEffectiveIso="2026-10-11",
                HasPendingPricing=true,PendingPricing=PricingPosture.Premium,
                PendingPricingEffectiveIso="2026-10-11",LastSourceOperationId="op/9"
            };
            var restored=JsonSaveSerializer.ReadObject<V3BusinessPolicyRecord>(
                JsonSaveSerializer.WriteObject(record));
            restored.Validate();
            Assert(restored.HasPendingResume && restored.PendingPricing==PricingPosture.Premium &&
                restored.LastSourceOperationId=="op/9","record survives JSON");
            restored.Status=EconomicOperationStatusCode.Closed;
            Throws<ArgumentException>(()=>restored.Validate());
        });


        Check("persisted v3 market epoch survives cold save with unspent wallet reservations", () =>
        {
            var catalog=M9T02FunctionalEconomy.Create();
            var date="2026-10-09";
            var pools=catalog.Pools.Values.Select(x=>CustomerDemandCalculator.OpeningSupply(date,catalog,x.PoolId)).ToArray();
            var available=Enumerable.Range(1080,120);
            var business=new BusinessMarketEpochParticipant(
                new BusinessTimeWalletCandidate("online-store","i",
                    BusinessOperationMode.SideHustleCompatible,120,8,0,0,18,52000,available),
                new BusinessDemandCandidate("i","online-store",PricingPosture.Standard),
                new BusinessProspectivePolicy("i",EconomicOperationStatus.Auto,PricingPosture.Standard),true);
            var planned=BusinessMarketEpochPlanner.Freeze(catalog,"epoch/cold","2026-10-09",160000,
                pools,Array.Empty<MarketFulfillmentUnit>(),new[]{business},Array.Empty<int>());
            var current=new GameState{SaveVersion=3,RunId="test-run",DateIso=date,Minute=1080,
                Cash=160000};
            var records=EconomicV3Records.Empty();
            records.RulesetRevision=catalog.RulesetRevision;
            V3EpochTransactions.Freeze(current,records,planned,"op/day-start",1080);
            Assert(current.Cash==160000,"reserving future unit costs must not debit cash");
            Assert(records.Provenance.ActiveEpoch!=null,"frozen epoch was not persisted");
            Assert(records.Provenance.ActiveEpoch!.PlannedUnits.Count==
                planned.OwnerTimeAndWallet.AllUnitReservations.Count,
                "market and wallet quotas remain one-to-one after persistence");
            var cold=JsonSaveSerializer.ReadObject<EconomicV3Records>(
                JsonSaveSerializer.WriteObject(records));
            cold.Validate();
            Assert(cold.Provenance.ActiveEpoch!=null &&
                cold.Provenance.ActiveEpoch.PlannedUnits.Count==
                    records.Provenance.ActiveEpoch!.PlannedUnits.Count,
                "cold restored exact market reservations");
            var originalIds=cold.Provenance.ActiveEpoch!.PlannedUnits.Select(x=>x.CostReservationId);
            Assert(originalIds.SequenceEqual(records.Provenance.ActiveEpoch.PlannedUnits.Select(x=>x.CostReservationId)),
                "cold restore reordered committed units");
            V3EpochTransactions.ConsumeInterval(current,cold,1080,1110,"op/boundary-1");
            Assert(cold.Provenance.Fulfillments.Count==2,"first 30 minutes yields two market units");
            Assert(current.Cash==56000,"two variable payments actually debited");
            V3EpochTransactions.ConsumeInterval(current,cold,1110,1125,"op/boundary-2");
            Assert(cold.Provenance.Fulfillments.Count==3,"third unit realized after remaining 15m");
            Assert(current.Cash==4000,"third variable reservation paid once");
            Assert(cold.Provenance.CostReservations.Count(x=>
                x.Status==V3CostReservationStatus.Consumed)==3,"all three realized reservations consumed");
            Assert(cold.Provenance.Fulfillments.Select(x=>x.Id).Distinct().Count()==3,
                "no duplicate sale identities");
            Assert(current.Ledger.Where(x=>x.Category=="business.variable.payment")
                .Sum(x=>x.CashDelta)==-156000,"actual ledger debits exactly three costs");
            cold.Validate();
            var again=JsonSaveSerializer.ReadObject<EconomicV3Records>(
                JsonSaveSerializer.WriteObject(cold));
            again.Validate();
            Throws<ArgumentException>(()=>V3EpochTransactions.ConsumeInterval(current,again,
                1080,1125,"op/replay-duplicated"));
        });

        Check("prospective replan releases only unconsumed cost and never refunds cash", () =>
        {
            var catalog=M9T02FunctionalEconomy.Create(); var date="2026-10-09";
            var pools=catalog.Pools.Values.Select(x=>CustomerDemandCalculator.OpeningSupply(date,catalog,x.PoolId));
            var b=new BusinessMarketEpochParticipant(
                new BusinessTimeWalletCandidate("online-store","i",BusinessOperationMode.SideHustleCompatible,
                    120,8,0,0,18,52000,Enumerable.Range(1080,120)),
                new BusinessDemandCandidate("i","online-store",PricingPosture.Standard),
                new BusinessProspectivePolicy("i",EconomicOperationStatus.Auto,PricingPosture.Standard),true);
            var plan=BusinessMarketEpochPlanner.Freeze(catalog,"epoch/release",date,160000,pools,
                Array.Empty<MarketFulfillmentUnit>(),new[]{b},Array.Empty<int>());
            var current=new GameState{SaveVersion=3,RunId="test-run",DateIso=date,Minute=1080,Cash=160000};
            var records=EconomicV3Records.Empty(); records.RulesetRevision=catalog.RulesetRevision;
            V3EpochTransactions.Freeze(current,records,plan,"op/start",1080);
            V3EpochTransactions.ConsumeInterval(current,records,1080,1095,"op/earned");
            Assert(records.Provenance.Fulfillments.Count==1,"first unit consumed");
            var beforeRelease=current.Cash;
            V3EpochTransactions.ReleaseFuture(records.Provenance);
            Assert(current.Cash==beforeRelease,"releasing a reservation does not reimburse past cost");
            Assert(records.Provenance.ActiveEpoch==null,"future plan removed");
            Assert(records.Provenance.CostReservations.Count(x=>
                x.Status==V3CostReservationStatus.Consumed)==1,"committed work survives pause");
            Assert(records.Provenance.CostReservations.Count(x=>
                x.Status==V3CostReservationStatus.Released)==2,"only uncommitted future reserves released");
            Assert(records.Provenance.Fulfillments.Count==1,"past sales remain immutable");
            records.Validate();
        });

        Check("authoritative epoch validator rejects tampered hours and market reservation mappings", () =>
        {
            var catalog=M9T02FunctionalEconomy.Create(); var date="2026-10-09";
            var pools=catalog.Pools.Values.Select(x=>CustomerDemandCalculator.OpeningSupply(date,catalog,x.PoolId));
            var b=new BusinessMarketEpochParticipant(
                new BusinessTimeWalletCandidate("online-store","i",BusinessOperationMode.SideHustleCompatible,
                    120,8,0,0,18,52000,Enumerable.Range(1080,120)),
                new BusinessDemandCandidate("i","online-store",PricingPosture.Standard),
                new BusinessProspectivePolicy("i",EconomicOperationStatus.Auto,PricingPosture.Standard),true);
            var plan=BusinessMarketEpochPlanner.Freeze(catalog,"epoch/tamper",date,160000,pools,
                Array.Empty<MarketFulfillmentUnit>(),new[]{b},Array.Empty<int>());
            var current=new GameState{SaveVersion=3,RunId="test-run",DateIso=date,Minute=1080,Cash=160000};
            var records=EconomicV3Records.Empty(); records.RulesetRevision=catalog.RulesetRevision;
            V3EpochTransactions.Freeze(current,records,plan,"op/start",1080);
            var bytes=JsonSaveSerializer.WriteObject(records);
            var hourTamper=JsonSaveSerializer.ReadObject<EconomicV3Records>(bytes);
            hourTamper.Provenance.ActiveEpoch!.Assignments.Single().FrozenFutureOwnerMinutes.Add(1080);
            Throws<ArgumentException>(()=>hourTamper.Validate());
            var costTamper=JsonSaveSerializer.ReadObject<EconomicV3Records>(bytes);
            costTamper.Provenance.ActiveEpoch!.PlannedUnits[0].BusinessUnitOrdinal=999;
            Throws<ArgumentException>(()=>costTamper.Validate());
            var soldTamper=JsonSaveSerializer.ReadObject<EconomicV3Records>(bytes);
            soldTamper.Provenance.CostReservations[0].Status=V3CostReservationStatus.Consumed;
            Throws<ArgumentException>(()=>soldTamper.Validate());
        });


        Check("v3 invoice persists one payable and income clears oldest without second debit", () =>
        {
            var candidate=new GameState{SaveVersion=3,RunId="run",DateIso="2026-10-10",
                Minute=0,Cash=40,Businesses=new List<BusinessState>{
                    new BusinessState{InstanceId="run/business/1",DefinitionId="online-store",
                        DefinitionRevision="v1",OpenedIso="2026-10-10",OpenedMinute=0,
                        InitialInvestment=100,PricingPosture=PricingPosture.Standard}
                }};
            var records=EconomicV3Records.Empty();
            var fixedDaily=new BusinessObligationDefinition("rent","v1","online-store","rent-v1",
                ObligationCadence.Daily,ObligationProrationPolicy.FullContractual,
                ObligationClosurePolicy.EnforceableThroughContractEnd,"2026-10-10",null,
                100,true);
            var opened=V3ObligationTransactions.OpenDue(candidate,records,new[]{fixedDaily},
                new Dictionary<string,bool>(), "run/op/open");
            Assert(opened.Count==1 && opened[0].AmountDueVnd==100,"contractual invoice due during pause");
            Assert(records.CommittedObligationIds.Single()==opened[0].Id,"invoice indexed once");
            Assert(candidate.Cash==40,"invoice accrual not a cash debit");
            Assert(V3ObligationTransactions.PayDue(candidate,records,"run/op/payment")==40,
                "pay only available 40 at due");
            Assert(candidate.Cash==0 && records.ObligationTranches.Single().OutstandingVnd==60,
                "source invoice and remaining debt persist once");
            var income=V3ObligationTransactions.CreditIncome(candidate,records,"run/op/sale",
                80,"business.revenue","run/business/1");
            Assert(income.WithheldToOldestArrearsVnd==60 && income.NetCreditedToCashVnd==20,
                "income80 pays60 and credits20");
            Assert(candidate.Cash==20 && records.ObligationTranches.Single().PaidVnd==100 &&
                records.ObligationTranches.Single().OutstandingVnd==0,
                "old fixed invoice cleared with no second cash debit");
            Assert(records.V3RecognizedRevenueVnd==80 &&
                records.V3AppliedToArrearsVnd==60 && records.V3NetBusinessCreditVnd==20,
                "revenue = arrears + cash");
            Assert(candidate.Ledger.Sum(x=>x.CashDelta)==-20,"wallet ledger conserved 40-40+20");
            records.Validate();
            Throws<ArgumentException>(()=>V3ObligationTransactions.OpenDue(candidate,records,
                new[]{fixedDaily},new Dictionary<string,bool>(),"run/op/duplicate"));
            candidate.DateIso="2026-10-11"; candidate.Minute=0;
            candidate.Businesses[0].ClosedIso="2026-10-10";
            candidate.Businesses[0].ClosedMinute=600;
            Assert(V3ObligationTransactions.OpenDue(candidate,records,new[]{fixedDaily},
                new Dictionary<string,bool>(),"run/op/next").Count==1,
                "next contractual liability survives closed status");
            Assert(records.ObligationTranches.Count==2,"one per contractual service date");
            records.Validate();
        });

        Check("persisted v3 old-day settlement conserves actual wallet and invoice profit", () =>
        {
            var catalog=M9T02FunctionalEconomy.Create();
            var date="2026-10-09";
            var candidate=new GameState{SaveVersion=3,RunId="run",DateIso=date,
                Minute=1080,Cash=160020,Businesses=new List<BusinessState>{
                    new BusinessState{InstanceId="i",DefinitionId="online-store",
                        DefinitionRevision="v1",OpenedIso=date,OpenedMinute=0,
                        InitialInvestment=100,PricingPosture=PricingPosture.Standard}
                }};
            var records=EconomicV3Records.Empty();
            records.RulesetRevision=catalog.RulesetRevision;
            var fixedDaily=new BusinessObligationDefinition("rent","v1","online-store","rent-v1",
                ObligationCadence.Daily,ObligationProrationPolicy.FullContractual,
                ObligationClosurePolicy.EnforceableThroughContractEnd,date,null,20,true);
            V3ObligationTransactions.OpenDue(candidate,records,new[]{fixedDaily},
                new Dictionary<string,bool>{{"i",true}},"op/open");
            Assert(V3ObligationTransactions.PayDue(candidate,records,"op/pay")==20,"pay fixed first");
            Assert(candidate.Cash==160000,"opening variable budget is exact preexisting cash");
            var pools=catalog.Pools.Values.Select(x=>
                CustomerDemandCalculator.OpeningSupply(date,catalog,x.PoolId));
            var business=new BusinessMarketEpochParticipant(
                new BusinessTimeWalletCandidate("online-store","i",
                    BusinessOperationMode.SideHustleCompatible,120,8,0,0,18,52000,
                    Enumerable.Range(1080,120)),
                new BusinessDemandCandidate("i","online-store",PricingPosture.Standard),
                new BusinessProspectivePolicy("i",EconomicOperationStatus.Auto,
                    PricingPosture.Standard),true);
            var plan=BusinessMarketEpochPlanner.Freeze(catalog,"epoch","2026-10-09",
                candidate.Cash,pools,Array.Empty<MarketFulfillmentUnit>(),
                new[]{business},Array.Empty<int>());
            V3EpochTransactions.Freeze(candidate,records,plan,"op/freeze",1080);
            V3EpochTransactions.ConsumeInterval(candidate,records,1080,1110,"op/advance-1");
            V3EpochTransactions.ConsumeInterval(candidate,records,1110,1125,"op/advance-2");
            candidate.Minute=1125;
            Assert(candidate.Cash==4000,"cash already paid three times 52000");
            Assert(records.Provenance.Fulfillments.Count==3,"three sale source lines earned");
            var settlement=V3MidnightTransactions.SettleOldDate(candidate,records,date,"op/midnight");
            Assert(settlement.GrossVnd==285000,"three units at effective price95000");
            Assert(settlement.VariablePaidVnd==156000 &&
                settlement.FixedRecognizedVnd==20,"variable and fixed source amounts");
            Assert(settlement.ProfitVnd==128980,"profit = gross - var - fixed");
            Assert(settlement.AppliedToArrearsVnd==0 &&
                settlement.NetCashCreditVnd==285000,"no debt all old-day revenue paid");
            Assert(candidate.Cash==289000,"160020-20-156000+285000=289000");
            Assert(records.V3RecognizedRevenueVnd==285000 &&
                records.V3PaidVariableVnd==156000 &&
                records.V3RecognizedFixedVnd==20,"stored economic totals conserve");
            Assert(records.Provenance.ActiveEpoch==null,"midnight releases future uncommitted units");
            Assert(records.SettledDayIds.Single()==settlement.Id,"settlement exactly once");
            records.Validate();
            var cold=JsonSaveSerializer.ReadObject<EconomicV3Records>(
                JsonSaveSerializer.WriteObject(records));
            cold.Validate();
            Throws<ArgumentException>(()=>V3MidnightTransactions.SettleOldDate(candidate,cold,
                date,"op/retry"));
        });


        Check("v3 economic receipt processing operates freelance and pays next-day fixed charges", () =>
        {
            var content=FirstPlayableContentTemplate.BuildCatalog();
            var economy=M9T02FunctionalEconomy.Create();
            var anchor=EconomicActivationAnchor.FromHistoricalCheckpoint(0,"",content.Version,2,
                "2026-10-09",0,V2ToV3Migration.ArchivedRuleset);
            var processor=new V3EconomicReceiptProcessor(content,economy,anchor);
            var state=new GameState{SaveVersion=3,RunId="run",ContentVersion=content.Version,
                DateIso="2026-10-09",Minute=480,Name="Tester",Cash=300000,
                History=new List<string>{"character.created:2026-10-09"},
                Receipts=new List<CommandReceipt>{
                    new CommandReceipt{CommandId="create",Payload=new GameCommand(
                        CommandKind.CreateCharacter,"fresh",18,"Tester","base.male").CanonicalPayload,
                        OperationId="run/op/1",Revision=1,MinutesConsumed=0},
                    new CommandReceipt{CommandId="start-day",Payload=new GameCommand(
                        CommandKind.AdvanceBoundary).CanonicalPayload,
                        OperationId="run/op/2",Revision=2,MinutesConsumed=480}
                },
                Businesses=new List<BusinessState>{
                    new BusinessState{InstanceId="run/business/1",DefinitionId="freelance-service",
                        DefinitionRevision="v1",OpenedIso="2026-10-09",
                        OpenedMinute=0,InitialInvestment=100,
                        PricingPosture=PricingPosture.Standard}
                }};
            var payload=new EconomicV3Payload{Current=state,Activation=EconomicV3Activation.FromAnchor(anchor),
                EconomicRecords=EconomicV3Records.Empty()};
            var before=state.Cash;
            var first=processor.Evaluate(payload,new GameCommand(CommandKind.AdvanceBoundary),"run/op/1");
            Assert(first==840 && state.Minute==1320 && state.DateIso=="2026-10-09",
                "gameplay advanced in-day to bedtime");
            Assert(payload.EconomicRecords!.Provenance.Fulfillments.Count==3,
                "committed slice earned three actual freelance units");
            Assert(state.Cash==235000,"opened fixed5000 and variable60000 paid from opening cash");
            state.Revision=3;
            state.Receipts.Add(new CommandReceipt{CommandId="evening",
                Payload=new GameCommand(CommandKind.AdvanceBoundary).CanonicalPayload,
                OperationId="run/op/3",Revision=3,MinutesConsumed=840});
            var second=processor.Evaluate(payload,new GameCommand(CommandKind.AdvanceBoundary),"run/op/4");
            Assert(second==120 && state.DateIso=="2026-10-10" && state.Minute==0,
                "midnight child consumes old date then advances simulation date");
            var prior=payload.EconomicRecords.Provenance.Settlements.Single();
            Assert(prior.OldDateIso=="2026-10-09" && prior.GrossVnd==660000,
                "old-date three sales realized only on midnight");
            Assert(prior.VariablePaidVnd==60000 && prior.FixedRecognizedVnd==5000 &&
                prior.ProfitVnd==595000,"revenue minus fixed and variable exactly once");
            Assert(state.Cash==895000,
                "opening300000-oldfixed5000-variable60000+660000=895000; new-day fee waits for next committed receipt");
            Assert(payload.EconomicRecords.Provenance.ActiveEpoch==null,
                "new-day epoch cannot freeze before its boundary receipt is committed");
            payload.EconomicRecords.Validate();
        });


        Check("real GameSession commits schema3 character and cold restores source-verified receipt", () =>
        {
            var content=FirstPlayableContentTemplate.BuildCatalog();
            var econ=M9T02FunctionalEconomy.Create();
            var archive=new BundledHistoricalV2ContentResolver();
            var versioned=new VersionedReceiptReplay(econ,content);
            var migration=new V2ToV3Migration(archive,versioned);
            var serializer=new V3SaveCompatibilitySerializer(content,
                GameSession.CreateRestoreValidator(),migration,
                new StagedV3SaveSerializer(archive,versioned));
            var source=GameSession.NewState(content,"run-m9-smoke",123456,
                new SimDate(2026,9,1));
            var promoted=EconomicV3PayloadCodec.ReadChecked(
                migration.Migrate(JsonSaveSerializer.WriteObject(source)));
            promoted.Current!.EconomicV3=promoted;
            var temp=Path.Combine(Path.GetTempPath(),
                "m9-t02-integration-"+Guid.NewGuid().ToString("N")+".json");
            try
            {
                var store=new AtomicFileSaveStore(temp,serializer);
                var session=new GameSession(promoted.Current,content,serializer,store,
                    economicRules:econ,archivedRules:archive);
                var cmd=new GameCommand(CommandKind.CreateCharacter,"fresh",18,"Tester","base.male");
                var result=session.Execute(new CommandEnvelope("run-m9-smoke","create-character",0,cmd));
                Assert(result.Status==CommandStatus.Committed,
                    "real v3 create command must commit to atomic store: "+result.ReasonKey);
                Assert(session.Snapshot().Revision==1,"revision one after character create");
                var freshReadiness = session.ReadBusinessOperations();
                Assert(freshReadiness.OwnerDay.StateRevision == freshReadiness.Portfolio.Game.Revision &&
                    freshReadiness.LaunchOptions.Count == 4 &&
                    freshReadiness.LaunchOptions.All(x => x.CanRequestLaunch),
                    "authoritative same-revision v3 launch projection exposes four openable businesses");
                var readOnlyBytes = File.ReadAllBytes(temp);
                _ = session.ReadBusinessOperations();
                Assert(File.ReadAllBytes(temp).SequenceEqual(readOnlyBytes),
                    "reading readiness never writes or replans the source checkpoint");
                Assert(store.Read().Status==LoadStatus.Valid &&
                    store.Read().SourceSchemaVersion==3,"primary is a validated schema3 save");
                Assert(GameSession.TryRestore(content,serializer,store,
                    out var restored,out var outcome,economicRules:econ,archivedRules:archive),
                    "cold restore v3 GameSession must be valid: "+outcome.Reason);
                Assert(restored!.Snapshot().Revision==1,"restored revision");
                var launch=new GameCommand(CommandKind.LaunchBusiness,"online-store",100,
                    appearanceId:"v1");
                var launched=restored.Execute(new CommandEnvelope("run-m9-smoke","launch-online",1,launch));
                Assert(launched.Status==CommandStatus.Committed,
                    "v3 live-only business must launch and replay through active content: "+launched.ReasonKey);
                Assert(restored.ReadBusinesses().Businesses.Any(x=>x.DefinitionId=="online-store"),
                    "newly authored online-store business is available to v3 gameplay");
                var launchedProjection = restored.ReadBusinessOperations();
                Assert(launchedProjection.LaunchOptions.Single(x=>x.DefinitionId=="online-store").Code ==
                    BusinessLaunchReadinessCode.BusinessTypeAlreadyActive,
                    "duplicate owner type disabled without UI hardcoding");
                Assert(launchedProjection.OwnedBusinesses.Single(x=>x.Business.DefinitionId=="online-store")
                    .OwnerTime != null, "owned business has authoritative time projection");
                Assert(store.Read().Status==LoadStatus.Valid &&
                    store.Read().State!.Businesses.Any(x=>x.DefinitionId=="online-store"),
                    "cold reloaded v3 source contains launch beyond frozen v2 business roster");
                var owned=restored.ReadBusinesses().Businesses.Single(x=>x.DefinitionId=="online-store");
                var price=restored.Execute(new CommandEnvelope("run-m9-smoke",
                    "price-premium",2,BusinessCommands.SetPricing(owned.InstanceId,PricingPosture.Premium)));
                Assert(price.Status==CommandStatus.Committed,"v3 next-day pricing must commit: "+price.ReasonKey);
                Assert(restored.ReadBusinesses().Businesses.Single(x=>x.InstanceId==owned.InstanceId)
                    .PricingPosture==PricingPosture.Standard,
                    "v3 price request does not change effective price same day");
                var pause=restored.Execute(new CommandEnvelope("run-m9-smoke",
                    "pause-online",3,BusinessCommands.Pause(owned.InstanceId)));
                Assert(pause.Status==CommandStatus.Committed,"v3 pause must commit: "+pause.ReasonKey);
                Assert(!restored.ReadBusinessOperations().OwnedBusinesses.Single().CanOperateToday,
                    "paused operation must not display eligible owner-time activity");
                var resume=restored.Execute(new CommandEnvelope("run-m9-smoke",
                    "resume-online",4,BusinessCommands.Resume(owned.InstanceId)));
                Assert(resume.Status==CommandStatus.Committed,"v3 next-day resume must commit: "+resume.ReasonKey);
                var coldPolicy=store.Read();
                Assert(coldPolicy.Status==LoadStatus.Valid &&
                    coldPolicy.SourceSchemaVersion==3,"cold restore of v3 pending policy");
                var savedPolicy=coldPolicy.State!.EconomicV3!.EconomicRecords!.BusinessPolicies.Single(
                    x=>x.InstanceId==owned.InstanceId);
                Assert(savedPolicy.Status==EconomicOperationStatusCode.Paused &&
                    savedPolicy.HasPendingResume && savedPolicy.HasPendingPricing,
                    "pending resume/price are canonical v3 persisted policy");
                var kioskLaunch=restored.Execute(new CommandEnvelope("run-m9-smoke",
                    "launch-kiosk",5,new GameCommand(CommandKind.LaunchBusiness,
                        "coffee-kiosk",100,appearanceId:"v1")));
                Assert(kioskLaunch.Status==CommandStatus.Committed,
                    "v3 founder must launch the full-time coffee kiosk: "+kioskLaunch.ReasonKey);
                var mixed=restored.ReadBusinessOperations();
                var kiosk=mixed.OwnedBusinesses.Single(x=>x.Business.DefinitionId=="coffee-kiosk");
                Assert(kiosk.Business.FixedHelper != null &&
                    kiosk.Business.FixedHelper.ContractedDailyWageVnd==90000L,
                    "read model projects fixed helper contract without a new salary transaction");
                Assert(kiosk.OwnerTime != null &&
                    kiosk.OwnerTime.Status==BusinessPlanStatus.FullyAllocated &&
                    kiosk.OwnerTime.AllocatedOwnerMinutes==480 &&
                    kiosk.OwnerTimeLimitedUnits==40,
                    "daytime coffee kiosk has a contiguous 480-minute founder block");
                var side=mixed.OwnedBusinesses.Single(x=>x.Business.DefinitionId=="online-store");
                Assert(side.OwnerTime != null && side.OwnerTime.AllocatedOwnerMinutes==120 &&
                    side.OwnerTimeLimitedUnits==8 && !side.CanOperateToday,
                    "side business shares evening time but remains paused until next-day resume");
                var kioskInterval=mixed.OwnerDay.Allocations.Single(x=>x.BusinessInstanceId==kiosk.Business.InstanceId).Interval;
                var sideInterval=mixed.OwnerDay.Allocations.Single(x=>x.BusinessInstanceId==side.Business.InstanceId).Interval;
                Assert(kioskInterval.EndMinute<=sideInterval.StartMinute &&
                    kioskInterval.Duration==480 && sideInterval.Duration==120,
                    "kiosk daytime and online evening owner allocations never overlap");
                var retry=restored.Execute(new CommandEnvelope("run-m9-smoke","create-character",0,cmd));
                Assert(retry.Status==CommandStatus.AlreadyCommitted,
                    "same canonical id must not duplicate the creation receipt");
            }
            finally
            {
                foreach (var filename in new[]{temp,temp+".backup",temp+".lock"})
                    if (File.Exists(filename)) File.Delete(filename);
            }
        });


        Check("v3 employment transitions commit with receipt-backed time and survive cold restore", () =>
        {
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var economics = M9T02FunctionalEconomy.Create();
            var archives = new BundledHistoricalV2ContentResolver();
            var replay = new VersionedReceiptReplay(economics, content);
            var migration = new V2ToV3Migration(archives, replay);
            var serializer = new V3SaveCompatibilitySerializer(content,
                GameSession.CreateRestoreValidator(), migration,
                new StagedV3SaveSerializer(archives, replay));
            var initial = GameSession.NewState(content, "run-employment-v3", 123456,
                new SimDate(2026, 9, 1));
            var promoted = EconomicV3PayloadCodec.ReadChecked(
                migration.Migrate(JsonSaveSerializer.WriteObject(initial)));
            promoted.Current!.EconomicV3 = promoted;
            var path = Path.Combine(Path.GetTempPath(),
                "m9-t02-employment-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var store = new AtomicFileSaveStore(path, serializer);
                var session = new GameSession(promoted.Current, content, serializer, store,
                    economicRules: economics, archivedRules: archives);
                CommandResult Commit(string id, GameCommand command)
                {
                    var result = session.Execute(new CommandEnvelope(initial.RunId, id,
                        session.Snapshot().Revision, command));
                    Assert(result.Status == CommandStatus.Committed,
                        id + " must commit through v3 transaction: " + result.ReasonKey);
                    Assert(GameSession.TryRestore(content, serializer, store,
                        out var restored, out var outcome,
                        economicRules: economics, archivedRules: archives),
                        id + " cold replay: " + outcome.Reason);
                    Assert(restored!.Snapshot().Revision == session.Snapshot().Revision &&
                        restored.Snapshot().CareerId == session.Snapshot().CareerId &&
                        restored.Snapshot().Cash == session.Snapshot().Cash,
                        id + " cold snapshot matches the continuously running session");
                    return result;
                }
                Commit("create", new GameCommand(CommandKind.CreateCharacter,
                    "fresh", 18, "Employment Tester", "base.male"));
                Assert(session.ReadCareerBiography().Stage == CareerJourneyStage.Unemployed,
                    "a newly created player without a business is unemployed, not a founder");
                Commit("launch", new GameCommand(CommandKind.LaunchBusiness,
                    "online-store", 100, appearanceId: "v1"));
                Assert(session.ReadCareerBiography().Stage == CareerJourneyStage.FullTimeFounder,
                    "operating an owned business without a job is a full-time founder");
                Commit("accept", new GameCommand(CommandKind.AcceptJob, "developer"));
                Assert(session.Snapshot().CareerId == "developer", "accepted career persists");
                var blockedKiosk = session.ReadBusinessOperations().LaunchOptions.Single(x =>
                    x.DefinitionId == "coffee-kiosk");
                Assert(blockedKiosk.Code == BusinessLaunchReadinessCode.EmploymentIncompatible &&
                    !blockedKiosk.CanRequestLaunch,
                    "employed worker is clearly blocked from daytime coffee kiosk ownership");
                Assert(session.ReadCareerBiography().Stage == CareerJourneyStage.SideBusinessOwner,
                    "employment with an active side business is not full-time founding");
                var acceptedBytes = File.ReadAllBytes(path);
                var revision = session.Snapshot().Revision;
                var retry = session.Execute(new CommandEnvelope(initial.RunId, "accept", revision - 1,
                    new GameCommand(CommandKind.AcceptJob, "developer")));
                Assert(retry.Status == CommandStatus.AlreadyCommitted &&
                    session.Snapshot().Revision == revision &&
                    File.ReadAllBytes(path).SequenceEqual(acceptedBytes),
                    "duplicate acceptance cannot replan, mint identities or rewrite the save");
                var rejected = session.Execute(new CommandEnvelope(initial.RunId, "accept-again", revision,
                    new GameCommand(CommandKind.AcceptJob, "developer")));
                Assert(rejected.Status == CommandStatus.Rejected &&
                    File.ReadAllBytes(path).SequenceEqual(acceptedBytes),
                    "rejected acceptance preserves the committed economic graph");
                Commit("wake", new GameCommand(CommandKind.AdvanceBoundary));
                Commit("work-start", new GameCommand(CommandKind.AdvanceBoundary));
                Commit("work-cue", new GameCommand(CommandKind.AdvanceBoundary));
                Assert(session.Snapshot().CareerXp > 0, "career work really executes before resignation");
                var beforeResign = store.Read().State!;
                var beforeBytes = File.ReadAllBytes(path);
                var failedSession = new GameSession(beforeResign, content, serializer,
                    new FailedWriteStore(store), economicRules: economics, archivedRules: archives);
                var failedResign = failedSession.Execute(new CommandEnvelope(initial.RunId,
                    "failed-resign", beforeResign.Revision, new GameCommand(CommandKind.Resign)));
                Assert(failedResign.Status == CommandStatus.PersistenceFailed &&
                    failedSession.Snapshot().CareerId == "developer" &&
                    failedSession.Snapshot().Revision == beforeResign.Revision &&
                    File.ReadAllBytes(path).SequenceEqual(beforeBytes),
                    "failed CAS cannot publish finalized resignation or change saved economics");
                var committedWorkEnd = beforeResign.Minute;
                Commit("resign", new GameCommand(CommandKind.Resign));
                Assert(session.Snapshot().CareerId == "", "resignation persists");
                var founder = session.ReadCareerBiography();
                Assert(founder.Stage == CareerJourneyStage.FullTimeFounder &&
                    founder.HasResignedEmployment && founder.PreviousEmployments.Count == 1,
                    "resignation with an active business transitions to founder without discarding biography");
                var availableKiosk = session.ReadBusinessOperations().LaunchOptions.Single(x =>
                    x.DefinitionId == "coffee-kiosk");
                Assert(availableKiosk.CanRequestLaunch &&
                    availableKiosk.RequiredOwnerMinutes == 480 &&
                    availableKiosk.MinimumInvestmentVnd ==
                        content.Businesses["coffee-kiosk"].MinimumStartupInvestment,
                    "resignation unlocks the authored daytime kiosk without inventing UI balance");
                var resignedState = store.Read().State!;
                var occupancy = CommittedTimeTrace.Project(resignedState, content);
                var workStart = content.Careers["developer"].StartMinute;
                Assert(Enumerable.Range(workStart, committedWorkEnd - workStart)
                    .All(t => occupancy[t] == CommittedTimeTrace.Employment),
                    "resignation preserves already committed work occupancy");
                Assert(occupancy[committedWorkEnd] == CommittedTimeTrace.Free,
                    "resignation releases future career time");
                Commit("reaccept", new GameCommand(CommandKind.AcceptJob, "developer"));
                Assert(session.Snapshot().CareerId == "developer", "reemployment persists");
                Assert(session.ReadCareerBiography().Stage == CareerJourneyStage.SideBusinessOwner,
                    "reemployment changes the stage without deleting existing business or history");
                var checkpoint = File.ReadAllBytes(path);
                Commit("continuation", new GameCommand(CommandKind.AdvanceBoundary));
                var uninterruptedBytes = File.ReadAllBytes(path);
                var otherPath = path + ".continuation";
                try
                {
                    File.WriteAllBytes(otherPath, checkpoint);
                    var otherStore = new AtomicFileSaveStore(otherPath, serializer);
                    Assert(GameSession.TryRestore(content, serializer, otherStore,
                        out var other, out var result, economicRules: economics, archivedRules: archives),
                        "independent cold continuation checkpoint: " + result.Reason);
                    var continued = other!.Execute(new CommandEnvelope(initial.RunId, "continuation",
                        other.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary)));
                    Assert(continued.Status == CommandStatus.Committed &&
                        File.ReadAllBytes(otherPath).SequenceEqual(uninterruptedBytes),
                        "cold and live continuation produce byte-identical save/receipt/economic graph");
                }
                finally
                {
                    foreach (var filename in new[] { otherPath, otherPath + ".backup", otherPath + ".lock" })
                        if (File.Exists(filename)) File.Delete(filename);
                }
                Commit("return-founder", new GameCommand(CommandKind.Resign));
                var businessId = session.ReadBusinesses().Businesses.Single(x =>
                    x.DefinitionId == "online-store").InstanceId;
                Commit("close-owned-business", new GameCommand(CommandKind.CloseBusiness, businessId));
                var unemployed = session.ReadCareerBiography();
                Assert(unemployed.Stage == CareerJourneyStage.Unemployed &&
                    unemployed.HasResignedEmployment && unemployed.ActiveBusinessCount == 0 &&
                    unemployed.PreviousEmployments.Count == 2,
                    "closing the last business returns player to unemployed without losing career history");
                var beforeDuplicate = File.ReadAllBytes(path);
                var repeatResign = session.Execute(new CommandEnvelope(initial.RunId,
                    "resign-with-no-career", session.Snapshot().Revision,
                    new GameCommand(CommandKind.Resign)));
                Assert(repeatResign.Status == CommandStatus.Rejected &&
                    File.ReadAllBytes(path).SequenceEqual(beforeDuplicate),
                    "invalid repeated resignation must not mutate persisted save");
            }
            finally
            {
                foreach (var filename in new[] { path, path + ".backup", path + ".lock" })
                    if (File.Exists(filename)) File.Delete(filename);
            }
        });

        Check("pre-fix v3 pending business policies retain byte-identical replay and serialization", () =>
        {
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var archives = new BundledHistoricalV2ContentResolver();
            var replay = new VersionedReceiptReplay(M9T02FunctionalEconomy.Create(), content);
            var serializer = new V3SaveCompatibilitySerializer(content,
                GameSession.CreateRestoreValidator(), new V2ToV3Migration(archives, replay),
                new StagedV3SaveSerializer(archives, replay));
            var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "pre-fix-v3-pending-business.json");
            var bytes = File.ReadAllBytes(path);
            var fixtureEnvelope = JsonSaveSerializer.ReadObject<SaveEnvelope>(bytes);
            var fixturePayload = EconomicV3PayloadCodec.ReadChecked(
                Convert.FromBase64String(fixtureEnvelope.PayloadBase64));
            replay.ValidateV3(fixturePayload, archives);
            var loaded = serializer.DeserializeAndValidate(bytes);
            Assert(loaded.Status == LoadStatus.Valid && loaded.SourceSchemaVersion == 3,
                "existing pre-fix save must pass unchanged strict replay: " + loaded.Reason);
            var policy = loaded.State!.EconomicV3!.EconomicRecords!.BusinessPolicies.Single();
            Assert(policy.HasPendingPricing && policy.HasPendingResume,
                "fixture exercises pricing/pause/resume, not only a fresh empty save");
            Assert(serializer.Serialize(loaded.State).SequenceEqual(bytes),
                "two-phase fix cannot rewrite previously valid v3 economics or wire bytes");
        });

        Check("v2 primary migrates in-memory and first v3 command preserves byte-exact v2 backup", () =>
        {
            var content=FirstPlayableContentTemplate.BuildCatalog();
            var archive=new BundledHistoricalV2ContentResolver();
            var economic=M9T02FunctionalEconomy.Create();
            var replay=new VersionedReceiptReplay(economic,content);
            var legacy=new JsonSaveSerializer(content,GameSession.CreateRestoreValidator(),
                new SyntheticV0Migration(),new V1ToV2Migration());
            var serializer=new V3SaveCompatibilitySerializer(content,
                GameSession.CreateRestoreValidator(),
                new V2ToV3Migration(archive,replay),
                new StagedV3SaveSerializer(archive,replay));
            var original=GameSession.NewState(content,"run-migration-smoke",9988,
                new SimDate(2026,9,1));
            var originalBytes=legacy.Serialize(original);
            var path=Path.Combine(Path.GetTempPath(),"m9-t02-migration-"+
                Guid.NewGuid().ToString("N")+".json");
            try
            {
                File.WriteAllBytes(path,originalBytes);
                var store=new AtomicFileSaveStore(path,serializer);
                var previous=store.Read();
                Assert(previous.Status==LoadStatus.Valid && previous.SourceSchemaVersion==2,
                    "source-v2 remains a tagged in-memory migrated checkpoint");
                Assert(File.ReadAllBytes(path).SequenceEqual(originalBytes),
                    "read and migration cannot rewrite primary bytes");
                Assert(GameSession.TryRestore(content,serializer,store,
                    out var session,out var restored,economicRules:economic,archivedRules:archive),
                    "restore historical v2 under active v3 serializer");
                Assert(restored.SourceSchemaVersion==2,"original source schema retained");
                var create=new GameCommand(CommandKind.CreateCharacter,"fresh",18,
                    "Migration","base.male");
                var cmd=session!.Execute(new CommandEnvelope("run-migration-smoke",
                    "create-migrated",0,create));
                Assert(cmd.Status==CommandStatus.Committed,
                    "first v3 gameplay command must commit atomically: "+cmd.ReasonKey);
                Assert(store.Read().Status==LoadStatus.Valid &&
                    store.Read().SourceSchemaVersion==3,"new primary promoted to version3");
                Assert(File.Exists(path+".backup") &&
                    File.ReadAllBytes(path+".backup").SequenceEqual(originalBytes),
                    "original v2 wire bytes remain unmodified in atomic backup");
                Assert(GameSession.TryRestore(content,serializer,store,
                    out var cold,out var saved,economicRules:economic,archivedRules:archive),
                    "cold restore committed schema3");
                Assert(cold!.Snapshot().Revision==1 && saved.SourceSchemaVersion==3,
                    "cold v3 replay preserves original receipt generation");
            }
            finally
            {
                foreach (var file in new[]{path,path+".backup",path+".lock"})
                    if (File.Exists(file)) File.Delete(file);
            }
        });


        Check("M11 released schemas v1 v2 v3 cold-restore and byte-identical continuation matrix", () =>
        {
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var economic = M9T02FunctionalEconomy.Create();
            var archives = new BundledHistoricalV2ContentResolver();
            var replay = new VersionedReceiptReplay(economic, content);
            var serializer = new V3SaveCompatibilitySerializer(content,
                GameSession.CreateRestoreValidator(), new V2ToV3Migration(archives, replay),
                new StagedV3SaveSerializer(archives, replay));
            var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures");
            var one = File.ReadAllBytes(Path.Combine(fixtureDir, "Legacy", "real-v1-employment.json"));
            var sourceOne = JsonSaveSerializer.ReadObject<SaveEnvelope>(one);
            var twoPayload = new V1ToV2Migration().Migrate(
                Convert.FromBase64String(sourceOne.PayloadBase64));
            var two = JsonSaveSerializer.Wrap(twoPayload, SaveSchema.HistoricalV2, sourceOne.Generation);
            var three = File.ReadAllBytes(Path.Combine(fixtureDir, "pre-fix-v3-pending-business.json"));
            var matrix = new (int Schema, byte[] Wire)[] {
                (1, one), (2, two), (3, three)
            };
            foreach (var source in matrix)
            {
                var initial = serializer.DeserializeAndValidate(source.Wire);
                Assert(initial.Status == LoadStatus.Valid &&
                    initial.SourceSchemaVersion == source.Schema &&
                    initial.State != null && initial.State.SaveVersion == 3,
                    "source schema " + source.Schema + " cannot migrate: " + initial.Reason);
                var baseState = initial.State!;
                var originalRevision = baseState.Revision;
                var originalEnvelope = JsonSaveSerializer.ReadObject<SaveEnvelope>(source.Wire);
                Assert(originalEnvelope.SchemaVersion == source.Schema,
                    "fixture schema is not source truth");
                if (source.Schema < 3)
                {
                    var provenance = baseState.EconomicV3!;
                    Assert(provenance.OriginalSourceSchema == source.Schema &&
                        Convert.FromBase64String(provenance.OriginalSourcePayloadBase64)
                            .SequenceEqual(Convert.FromBase64String(originalEnvelope.PayloadBase64)) &&
                        provenance.Activation!.LegacyReceiptCount == originalRevision,
                        "legacy source bytes, schema or identity frontier were lost");
                }
                else
                {
                    var policies = baseState.EconomicV3!.EconomicRecords!.BusinessPolicies;
                    Assert(policies.Any(x => x.HasPendingPricing && x.HasPendingResume),
                        "real pre-fix V3 fixture must still preserve pending actions");
                    Assert(serializer.Serialize(baseState).SequenceEqual(source.Wire),
                        "pre-fix v3 source must roundtrip without wire rewrite");
                }

                var root = Path.Combine(Path.GetTempPath(), "m11-compat-" +
                    source.Schema + "-" + Guid.NewGuid().ToString("N"));
                var leftPath = root + "-left.json";
                var rightPath = root + "-right.json";
                try
                {
                    File.WriteAllBytes(leftPath, source.Wire);
                    File.WriteAllBytes(rightPath, source.Wire);
                    var leftStore = new AtomicFileSaveStore(leftPath, serializer);
                    var rightStore = new AtomicFileSaveStore(rightPath, serializer);
                    var leftOk = GameSession.TryRestore(content, serializer, leftStore,
                        out var left, out var leftLoad, economicRules: economic, archivedRules: archives);
                    var rightOk = GameSession.TryRestore(content, serializer, rightStore,
                        out var right, out var rightLoad, economicRules: economic, archivedRules: archives);
                    Assert(leftOk && rightOk,
                        "two independent cold restores of schema " + source.Schema +
                        " failed: " + leftLoad.Reason + ", " + rightLoad.Reason);
                    Assert(leftLoad.SourceSchemaVersion == source.Schema &&
                        rightLoad.SourceSchemaVersion == source.Schema,
                        "cold restore discarded original schema identity");
                    Assert(left!.Snapshot().Revision == originalRevision &&
                        right!.Snapshot().Revision == originalRevision,
                        "cold reads must preserve original revision");
                    _ = left.Snapshot();
                    _ = right!.ReadBusinesses();
                    Assert(File.ReadAllBytes(leftPath).SequenceEqual(source.Wire) &&
                        File.ReadAllBytes(rightPath).SequenceEqual(source.Wire),
                        "cold-read migrated historical source must not rewrite original wire");

                    var envelope = new CommandEnvelope(baseState.RunId,
                        "m11-schema-" + source.Schema, originalRevision,
                        new GameCommand(CommandKind.AdvanceBoundary));
                    var first = left.Execute(envelope);
                    var second = right.Execute(envelope);
                    Assert(first.Status == CommandStatus.Committed &&
                        second.Status == CommandStatus.Committed,
                        "schema " + source.Schema + " cold continuation did not commit: " +
                        first.ReasonKey + ", " + second.ReasonKey);
                    var persisted = File.ReadAllBytes(leftPath);
                    Assert(persisted.SequenceEqual(File.ReadAllBytes(rightPath)),
                        "schema " + source.Schema + " continuation diverged between cold loads");
                    Assert(File.ReadAllBytes(leftPath + ".backup").SequenceEqual(source.Wire) &&
                        File.ReadAllBytes(rightPath + ".backup").SequenceEqual(source.Wire),
                        "first V3 write must preserve exact original schema " + source.Schema + " wire");
                    var validated = leftStore.Read();
                    Assert(validated.Status == LoadStatus.Valid &&
                        validated.SourceSchemaVersion == 3 &&
                        validated.State!.Revision == originalRevision + 1,
                        "schema " + source.Schema + " V3 promotion did not replay");
                    Assert(GameSession.TryRestore(content, serializer, leftStore,
                        out var resumed, out var resumedLoad,
                        economicRules: economic, archivedRules: archives) &&
                        resumedLoad.SourceSchemaVersion == 3 &&
                        resumed!.Execute(envelope).Status == CommandStatus.AlreadyCommitted,
                        "schema " + source.Schema + " retry after cold continuation was not idempotent");
                }
                finally
                {
                    foreach (var path in new[] { leftPath, rightPath })
                        foreach (var file in new[] { path, path + ".backup", path + ".lock" })
                            if (File.Exists(file)) File.Delete(file);
                }
            }
        });

        Check("M11 test-only V0 and fixture-v1 refuse unknown content without migration", () =>
        {
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var econ = M9T02FunctionalEconomy.Create();
            var archives = new BundledHistoricalV2ContentResolver();
            var replay = new VersionedReceiptReplay(econ, content);
            var serializer = new V3SaveCompatibilitySerializer(content,
                GameSession.CreateRestoreValidator(), new V2ToV3Migration(archives, replay),
                new StagedV3SaveSerializer(archives, replay));
            foreach (var legacyName in new[] { "synthetic-v0.json", "current-v1.json" })
            {
                var source = File.ReadAllBytes(Path.Combine(
                    AppContext.BaseDirectory, "Fixtures", "Legacy", legacyName));
                var outcome = serializer.DeserializeAndValidate(source);
                Assert(outcome.Status == LoadStatus.UnsupportedContent,
                    "unreleased fixture.v1 may not silently import into production: " +
                    legacyName + " / " + outcome.Status + " / " + outcome.Reason);
            }
        });

        Check("M11 independent scheduler and event RNG tampering is caught by V3 replay", () =>
        {
            var content = FirstPlayableContentTemplate.BuildCatalog();
            var economic = M9T02FunctionalEconomy.Create();
            var archives = new BundledHistoricalV2ContentResolver();
            var replay = new VersionedReceiptReplay(economic, content);
            var serializer = new V3SaveCompatibilitySerializer(content,
                GameSession.CreateRestoreValidator(), new V2ToV3Migration(archives, replay),
                new StagedV3SaveSerializer(archives, replay));
            var original = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
                "Fixtures", "pre-fix-v3-pending-business.json"));
            var envelope = JsonSaveSerializer.ReadObject<SaveEnvelope>(original);
            foreach (var stream in new[] { "scheduler", "event" })
            {
                var candidate = EconomicV3PayloadCodec.ReadChecked(
                    Convert.FromBase64String(envelope.PayloadBase64));
                if (stream == "scheduler")
                    candidate.Current!.SchedulerRng ^= 0x9e3779b9u;
                else
                    candidate.Current!.EventRng ^= 0x9e3779b9u;
                var alteredWire = JsonSaveSerializer.Wrap(
                    EconomicV3PayloadCodec.SerializeChecked(candidate),
                    3, envelope.Generation);
                var result = serializer.DeserializeAndValidate(alteredWire);
                Assert(result.Status == LoadStatus.Corrupt,
                    "valid-envelope tampering of " + stream + " RNG bypassed frozen/V3 replay");
            }
            Assert(serializer.DeserializeAndValidate(original).Status == LoadStatus.Valid,
                "immutable original fixture was modified by RNG test");
        });

        Check("same-generation v2 CAS projection rejects edited cash and latent v3 economy", () =>
        {
            var content=FirstPlayableContentTemplate.BuildCatalog();
            var archive=new BundledHistoricalV2ContentResolver();
            var economic=M9T02FunctionalEconomy.Create();
            var replay=new VersionedReceiptReplay(economic,content);
            var migrator=new V2ToV3Migration(archive,replay);
            var serializer=new V3SaveCompatibilitySerializer(content,GameSession.CreateRestoreValidator(),
                migrator,new StagedV3SaveSerializer(archive,replay));
            var original=GameSession.NewState(content,"run-cas-smoke",99891,new SimDate(2026,9,1));
            var originalBytes=JsonSaveSerializer.WriteObject(original);
            var migrated=EconomicV3PayloadCodec.ReadChecked(migrator.Migrate(originalBytes));
            migrated.Current!.EconomicV3=migrated;
            var unchanged=serializer.SerializeForSchema(migrated.Current,2);
            Assert(unchanged.Length>0,"unmodified v2 source may be projected to its exact version");
            migrated.Current.Cash++;
            Throws<ArgumentException>(()=>serializer.SerializeForSchema(migrated.Current!,2));
            migrated.Current.Cash--;
            migrated.EconomicRecords!.RulesetRevision="unauthorized-pending-rule";
            Throws<ArgumentException>(()=>serializer.SerializeForSchema(migrated.Current!,2));
        });

        Check("legacy business closed before v3 activation must never open a v3 contract", () =>
        {
            Assert(!V3EconomicReceiptProcessor.HasPostCutoverContractPeriod(
                "2026-09-20", "2026-10-10"),
                "historical-only closed business must not incur a post-cutover due");
            Assert(V3EconomicReceiptProcessor.HasPostCutoverContractPeriod(
                "2026-10-10", "2026-10-10"),
                "closure on the activation date may preserve an accrued contractual due");
            Assert(V3EconomicReceiptProcessor.HasPostCutoverContractPeriod(
                "2026-10-11", "2026-10-10"),
                "contract can remain enforceable through a later closure");
            Assert(V3EconomicReceiptProcessor.HasPostCutoverContractPeriod(
                null, "2026-10-10"),
                "still-active business remains eligible for v3 contract consideration");
            Throws<FormatException>(() =>
                V3EconomicReceiptProcessor.HasPostCutoverContractPeriod("garbage", "2026-10-10"));
        });

        var count=passed+Failures.Count;
        Console.WriteLine(passed+"/"+count+" M9-T02 economy checks passed.");
        if (args.Length>0)
        {
            var path=Path.GetFullPath(args[0]);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path,JsonSerializer.Serialize(new {passed,failed=Failures.Count,failures=Failures},new JsonSerializerOptions{WriteIndented=true}));
        }
        return Failures.Count==0?0:1;
    }
}
