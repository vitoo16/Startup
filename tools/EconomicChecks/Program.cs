#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using StartupLife.Content;
using StartupLife.Core;
using StartupLife.Simulation;

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
            Assert(BusinessObligationScheduler.DueOnDate(Definition(ObligationCadence.Daily),"b1","2026-02-01",false)==null,
                "no service entitlement");
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
