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
            var migration = new V2ToV3Migration();
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
            var migration = new V2ToV3Migration();
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
                new V2ToV3Migration().Migrate(JsonSaveSerializer.WriteObject(legacy)));
            legacy.ContentVersion = "first-playable.v1";
            legacy.SaveVersion = 3;
            Throws<ArgumentException>(() =>
                new V2ToV3Migration().Migrate(JsonSaveSerializer.WriteObject(legacy)));
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
