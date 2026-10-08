#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StartupLife.Core;
using StartupLife.Simulation;

internal static partial class Program
{
    private static void OwnerDayAllocationChecks()
    {
        Check("M9-T01 interval and capacity validation", OwnerTimeContracts);
        Check("M9-T01 one business caps at owner requirement", OwnerTimeOne);
        Check("M9-T01 two different overlapping windows share feasible 90/90", OwnerTimeOverlap);
        Check("M9-T01 three side businesses receive 80/80/80", () => OwnerTimeEqual(3,80));
        Check("M9-T01 four side businesses receive 60/60/60/60", () => OwnerTimeEqual(4,60));
        Check("M9-T01 kiosk contiguous reservation precedes side business", OwnerTimeKiosk);
        Check("M9-T01 kiosk cannot operate with a fragmented or elapsed block", OwnerTimeKioskUnavailable);
        Check("M9-T01 Manager and legacy content cannot obtain operation minutes", OwnerTimeUnsupported);
        Check("M9-T01 unsupported content has no speculative free minutes", OwnerTimeMissingRevision);
        Check("M9-T01 unavailable career revision fails closed", OwnerTimeMissingCareerRevision);
        Check("M9-T01 closed businesses receive no allocation", OwnerTimeClosed);
        Check("M9-T01 real committed Study timeline and cold restore", OwnerTimeStudyTrace);
        Check("M9-T01 Developer weekday employment reservation and side business", OwnerTimeDeveloperShift);
        Check("M9-T01 F&B Saturday career shift reserves mandatory time", OwnerTimeWeekendShift);
        Check("M9-T01 off-day and pre-first-shift employment reserve no time", OwnerTimeOffDay);
        Check("M9-T01 241-minute canonical remainder apportions 81/80/80", OwnerTimeRemainder);
        Check("M9-T01 capped owner time redistributes remaining 120 minutes", OwnerTimeRedistribution);
        Check("M9-T01 three-business permutations preserve complete output", () => OwnerTimePermutations(3));
        Check("M9-T01 four-business permutations preserve complete output", () => OwnerTimePermutations(4));
        Check("M9-T01 source portfolio reordering is byte-deterministic", OwnerTimeReordering);
        Check("M9-T01 valid cold restore preserves complete output", OwnerTimeColdRestore);
        Check("M9-T01 read model returns pure revision-bound preview", OwnerTimeReadModel);
    }

    private static BusinessDefinition PlanBusiness(string id, BusinessType type, int first, int last,
        int required = 120, BusinessOperationMode mode = BusinessOperationMode.SideHustleCompatible,
        string revision = "r1") =>
        new BusinessDefinition(id,revision,id+".name",type,mode,100,1000,
            new[]{PricingPosture.Budget,PricingPosture.Standard,PricingPosture.Premium},PricingPosture.Standard,1,1000,
            new BusinessOperatingRequirements(new[]{new BusinessOperatingWindow(DayOfWeek.Wednesday,first,last)},required));

    private static BusinessState PlanInstance(BusinessDefinition definition, string id, bool closed = false) =>
        new BusinessState { InstanceId=id,DefinitionId=definition.Id,DefinitionRevision=definition.Revision,
            OpenedIso="2026-09-01",OpenedMinute=480,ClosedIso=closed?"2026-09-01":null,
            ClosedMinute=closed?480:(int?)null,PricingPosture=PricingPosture.Standard,InitialInvestment=100 };

    private static ContentCatalog PlanCatalog(params BusinessDefinition[] definitions) =>
        EligibilityCatalog(definitions,Array.Empty<CareerDefinition>());

    // Test fixtures carry an explicit receipt cursor; the planner must never infer past time.
    private static GameState PlanState(ContentCatalog catalog, int minute = 480)
    {
        var state = new GameState { ContentVersion=catalog.Version,DateIso=Wednesday.ToString(),Minute=minute };
        if (minute > 0)
        {
            state.History.Add("character.created:"+Wednesday);
            state.Receipts.Add(new CommandReceipt {CommandId="fixture-create",
                Payload=new GameCommand(CommandKind.CreateCharacter,"fresh",18,"Owner","base.female").CanonicalPayload,
                OperationId="run/op/1",Revision=1,MinutesConsumed=0});
            state.Receipts.Add(new CommandReceipt {CommandId="fixture-advance",
                Payload=new GameCommand(CommandKind.AdvanceBoundary).CanonicalPayload,
                OperationId="run/op/2",Revision=2,MinutesConsumed=minute});
            state.Revision=2;
        }
        return state;
    }

    private static void OwnerTimeContracts()
    {
        Throws<ArgumentException>(()=>new OwnerTimeInterval(0,0));
        Throws<ArgumentException>(()=>new OwnerTimeInterval(-1,5));
        Throws<ArgumentException>(()=>new OwnerTimeInterval(1,1441));
        var interval=new OwnerTimeInterval(600,660);
        Equal(60,interval.Duration);
        var allocated=new BusinessOperatingCapacity("id",120,90,BusinessPlanStatus.PartiallyAllocated);
        Equal(7500,allocated.CapacityPermyriad);
        Equal(150,allocated.EffectiveCapacity(200));
        // Avoid multiplying an already-rounded permyriad: floor(3 * 1 / 3) must equal 1.
        Equal(1,new BusinessOperatingCapacity("round",3,1,BusinessPlanStatus.PartiallyAllocated).EffectiveCapacity(3));
        Throws<ArgumentException>(()=>new BusinessOperatingCapacity("id",120,119,BusinessPlanStatus.FullyAllocated));
        Throws<ArgumentException>(()=>new BusinessOperatingCapacity("id",120,0,BusinessPlanStatus.PartiallyAllocated));
        Throws<ArgumentOutOfRangeException>(()=>allocated.EffectiveCapacity(-1));
        var source=new[]{interval}; var plan=new OwnerDayAllocation(Wednesday,1,"v1",480,OwnerDayPlanStatus.Ready,
            Array.Empty<OwnerTimeInterval>(),Array.Empty<OwnerTimeInterval>(),
            source,Array.Empty<OwnerTimeInterval>(),Array.Empty<OwnerTimeAllocation>(),new[]{allocated});
        source[0]=new OwnerTimeInterval(1000,1100);
        Equal(600,plan.ElapsedIntervals[0].StartMinute);
        Throws<NotSupportedException>(()=>((IList<OwnerTimeInterval>)plan.ElapsedIntervals).Clear());
    }

    private static void OwnerTimeOne()
    {
        var b=PlanBusiness("test.plan.a",BusinessType.FreelanceService,1080,1320);
        var c=PlanCatalog(b);var s=PlanState(c);s.Businesses.Add(PlanInstance(b,"run/business/1"));
        var before=System.Text.Json.JsonSerializer.Serialize(s);
        var plan=OwnerDayAllocator.Plan(s,c);
        Equal(OwnerDayPlanStatus.Ready,plan.Status);
        Equal(120,plan.Capacities.Single().AllocatedOwnerMinutes);
        Equal(BusinessPlanStatus.FullyAllocated,plan.Capacities.Single().Status);
        Equal(120,plan.Allocations.Sum(x=>x.Interval.Duration));
        True(plan.Allocations.All(x=>x.Interval.StartMinute>=1080 && x.Interval.EndMinute<=1320));
        Equal(before,System.Text.Json.JsonSerializer.Serialize(s));
    }

    private static void OwnerTimeOverlap()
    {
        var a=PlanBusiness("test.plan.a",BusinessType.OnlineStore,1080,1200);
        var b=PlanBusiness("test.plan.b",BusinessType.HomeFoodPreorder,1140,1260);
        var c=PlanCatalog(a,b);var s=PlanState(c);
        s.Businesses.Add(PlanInstance(a,"run/business/1"));s.Businesses.Add(PlanInstance(b,"run/business/2"));
        var p=OwnerDayAllocator.Plan(s,c);
        Equal(90,p.Capacities.Single(x=>x.BusinessInstanceId=="run/business/1").AllocatedOwnerMinutes);
        Equal(90,p.Capacities.Single(x=>x.BusinessInstanceId=="run/business/2").AllocatedOwnerMinutes);
        AssertNoOverlap(p);
        foreach(var allocation in p.Allocations)
        {
            if(allocation.DefinitionId==a.Id) True(allocation.Interval.StartMinute>=1080 && allocation.Interval.EndMinute<=1200);
            else True(allocation.Interval.StartMinute>=1140 && allocation.Interval.EndMinute<=1260);
        }
    }

    private static void OwnerTimeEqual(int count,int each)
    {
        var kinds=new[]{BusinessType.OnlineStore,BusinessType.HomeFoodPreorder,BusinessType.FreelanceService,BusinessType.CoffeeKiosk};
        var businesses=Enumerable.Range(0,count).Select(i=>PlanBusiness("test.plan."+((char)('a'+i)),kinds[i],1080,1320)).ToArray();
        var c=PlanCatalog(businesses);var s=PlanState(c);
        for(var i=0;i<count;i++)s.Businesses.Add(PlanInstance(businesses[i],"run/business/"+(i+1)));
        var p=OwnerDayAllocator.Plan(s,c);
        Equal(count,p.Capacities.Count);
        True(p.Capacities.All(x=>x.AllocatedOwnerMinutes==each));
        Equal(count*each,p.Allocations.Sum(x=>x.Interval.Duration));
        AssertNoOverlap(p);
    }

    private static void OwnerTimeKiosk()
    {
        var k=PlanBusiness("test.plan.kiosk",BusinessType.CoffeeKiosk,540,1020,480,BusinessOperationMode.FullTimeRequired);
        var f=PlanBusiness("test.plan.freelance",BusinessType.FreelanceService,1080,1320);
        var c=PlanCatalog(k,f);var s=PlanState(c);
        s.Businesses.Add(PlanInstance(k,"run/business/1"));s.Businesses.Add(PlanInstance(f,"run/business/2"));
        var p=OwnerDayAllocator.Plan(s,c);
        Equal(480,p.Capacities.Single(x=>x.BusinessInstanceId=="run/business/1").AllocatedOwnerMinutes);
        Equal(120,p.Capacities.Single(x=>x.BusinessInstanceId=="run/business/2").AllocatedOwnerMinutes);
        var kiosk=p.Allocations.Single(x=>x.DefinitionId==k.Id);
        Equal(540,kiosk.Interval.StartMinute);Equal(1020,kiosk.Interval.EndMinute);
        Equal(240,p.FreeIntervals.Sum(x=>x.Duration));
        AssertNoOverlap(p);
    }

    private static void OwnerTimeKioskUnavailable()
    {
        var k=PlanBusiness("test.plan.kiosk",BusinessType.CoffeeKiosk,540,1020,480,BusinessOperationMode.FullTimeRequired);
        var c=PlanCatalog(k);var s=PlanState(c,minute:600);
        s.Businesses.Add(PlanInstance(k,"run/business/1"));
        var p=OwnerDayAllocator.Plan(s,c);
        Equal(BusinessPlanStatus.FullTimeBlockUnavailable,p.Capacities.Single().Status);
        Equal(0,p.Allocations.Count);
    }

    private static void OwnerTimeUnsupported()
    {
        var manager=PlanBusiness("test.plan.manager",BusinessType.FreelanceService,1080,1320,120,BusinessOperationMode.ManagerOperable);
        var c=PlanCatalog(manager);var s=PlanState(c);
        s.Businesses.Add(PlanInstance(manager,"run/business/1"));
        var plan=OwnerDayAllocator.Plan(s,c);
        Equal(BusinessPlanStatus.UnsupportedOperationMode,plan.Capacities.Single().Status);
        Equal(0,plan.Allocations.Count);
        var legacy=Freelance;
        c=PlanCatalog(legacy);s=PlanState(c);s.Businesses.Add(PlanInstance(legacy,"run/business/2"));
        plan=OwnerDayAllocator.Plan(s,c);
        Equal(BusinessPlanStatus.UnscheduledDefinition,plan.Capacities.Single().Status);
    }

    private static void AssertUnsupportedNoFree(OwnerDayAllocation plan, int cutoff)
    {
        Equal(OwnerDayPlanStatus.UnsupportedContent,plan.Status);
        Equal(cutoff,plan.CutoffMinute);
        Equal(0,plan.Allocations.Count);
        Equal(0,plan.FreeIntervals.Count);
        Equal(0,plan.Capacities.Count);
        // Without resolvable content provenance, the read model must not invent
        // employment/study/elapsed time classifications either.
        Equal(0,plan.EmploymentIntervals.Count);
        Equal(0,plan.StudyIntervals.Count);
        Equal(0,plan.ElapsedIntervals.Count);
    }

    private static void OwnerTimeMissingRevision()
    {
        var b=PlanBusiness("test.plan.a",BusinessType.OnlineStore,1080,1320);
        var c=PlanCatalog(b);var s=PlanState(c,1200);var instance=PlanInstance(b,"run/business/1");
        instance.DefinitionRevision="old";s.Businesses.Add(instance);
        AssertUnsupportedNoFree(OwnerDayAllocator.Plan(s,c),1200);
        s.ContentVersion="incompatible.version";
        AssertUnsupportedNoFree(OwnerDayAllocator.Plan(s,c),1200);
    }

    private static void OwnerTimeMissingCareerRevision()
    {
        var b=PlanBusiness("test.plan.a",BusinessType.FreelanceService,1080,1320);
        var current=EligibilityCatalog(new[]{b},new[]{EligibilityCareer()});
        var f=EligibilityFixture(current);
        Employ(f);
        // Real, committed receipt sequence: midnight -> wake -> shift start -> shift end.
        for(var i=0;i<3;i++) Committed(f,"career-boundary-"+i,new GameCommand(CommandKind.AdvanceBoundary));
        Equal(1020,f.State().Minute);
        var missing=EligibilityCatalog(new[]{b},new[]{EligibilityCareer(revision:"unavailable-r2")});
        Equal(current.Version,missing.Version);
        AssertUnsupportedNoFree(OwnerDayAllocator.Plan(f.State(),missing),1020);
    }

    private static void OwnerTimeClosed()
    {
        var b=PlanBusiness("test.plan.a",BusinessType.FreelanceService,1080,1320);
        var c=PlanCatalog(b);var s=PlanState(c);s.Businesses.Add(PlanInstance(b,"run/business/1",true));
        var p=OwnerDayAllocator.Plan(s,c);
        Equal(0,p.Allocations.Count);Equal(0,p.Capacities.Count);
    }

    private static void OwnerTimeStudyTrace()
    {
        var b=PlanBusiness("test.plan.a",BusinessType.FreelanceService,1080,1320);
        // A one-scene shift ending at 18:00 creates a simulator-reachable
        // study boundary. Never manufacture receipt durations or purchase state.
        var baseCatalog=EligibilityCatalog(new[]{b},new[]{EligibilityCareer(start:540,end:1080)});
        var course=new CourseDefinition("test.plan.course","course.test.plan","communication",1,0,360,0);
        var c=new ContentCatalog(baseCatalog.Version,baseCatalog.Skills.Values,baseCatalog.Careers.Values,
            new[]{course},baseCatalog.Starts.Values,baseCatalog.Businesses.Values,
            baseCatalog.Economy,baseCatalog.Schedule);
        var f=EligibilityFixture(c);
        Employ(f);
        ScheduledLaunch(f,b);
        Committed(f,"purchase-owner-course",new GameCommand(CommandKind.PurchaseCourse,course.Id));
        foreach(var target in new[]{480,540,1080})
        {
            Committed(f,"owner-study-boundary-"+target,new GameCommand(CommandKind.AdvanceBoundary));
            Equal(target,f.State().Minute);
        }
        var result=f.Execute("owner-study-180",new GameCommand(CommandKind.Study,amount:180));
        Equal(CommandStatus.Committed,result.Status);
        Equal(180,result.MinutesConsumed);
        Equal(1260,f.State().Minute);
        True(f.State().Course!=null);
        var before=f.Checkpoint();
        var p=f.Session.ReadOwnerDayAllocation();
        Equal(OwnerDayPlanStatus.Ready,p.Status);
        Equal(180,p.StudyIntervals.Sum(x=>x.Duration));
        Equal(60,p.Capacities.Single().AllocatedOwnerMinutes);
        Equal(5000,p.Capacities.Single().CapacityPermyriad);
        True(p.Allocations.All(x=>x.Interval.StartMinute>=1260));
        AssertNoOverlap(p);
        var cold=f.Restore();
        Equal(NormalizedPlan(p),NormalizedPlan(cold.Session.ReadOwnerDayAllocation()));
        Bytes(before,cold.Checkpoint());
    }

    private static void OwnerTimeDeveloperShift()
    {
        var b=Scheduled();var c=EligibilityCatalog(new[]{b},new[]{EligibilityCareer()});
        var f=EligibilityFixture(c);Employ(f);ScheduledLaunch(f,b);
        var plan=f.Session.ReadOwnerDayAllocation();
        Equal(480,plan.EmploymentIntervals.Sum(x=>x.Duration));
        Equal(120,plan.Capacities.Single().AllocatedOwnerMinutes);
        AssertNoOverlap(plan);
    }

    private static void OwnerTimeWeekendShift()
    {
        var day=new SimDate(2026,9,5);
        var career=EligibilityCareer(start:480,end:960,days:new[]{DayOfWeek.Saturday});
        var b=Scheduled();var c=EligibilityCatalog(new[]{b},new[]{career});
        var f=EligibilityFixture(c,day);Employ(f);ScheduledLaunch(f,b);
        var plan=f.Session.ReadOwnerDayAllocation();
        Equal(480,plan.EmploymentIntervals.Sum(x=>x.Duration));
        Equal(120,plan.Capacities.Single().AllocatedOwnerMinutes);
        AssertNoOverlap(plan);
    }

    private static void OwnerTimeOffDay()
    {
        var day=new SimDate(2026,9,5);
        var b=Scheduled();var c=EligibilityCatalog(new[]{b},new[]{EligibilityCareer()});
        var f=EligibilityFixture(c,day);Employ(f);ScheduledLaunch(f,b);
        var plan=f.Session.ReadOwnerDayAllocation();
        Equal(0,plan.EmploymentIntervals.Sum(x=>x.Duration));
        Equal(120,plan.Capacities.Single().AllocatedOwnerMinutes);
        AssertNoOverlap(plan);
    }

    private static void OwnerTimeRemainder()
    {
        var kinds=new[]{BusinessType.OnlineStore,BusinessType.HomeFoodPreorder,BusinessType.FreelanceService};
        var businesses=Enumerable.Range(0,3).Select(i=>
            PlanBusiness("test.plan."+((char)('a'+i)),kinds[i],1079,1320)).ToArray();
        var c=PlanCatalog(businesses);var s=PlanState(c);
        for(var i=0;i<businesses.Length;i++)
            s.Businesses.Add(PlanInstance(businesses[i],"run/business/"+(i+1)));
        var p=OwnerDayAllocator.Plan(s,c);
        Equal(OwnerDayPlanStatus.Ready,p.Status);
        Equal(241,p.Allocations.Sum(x=>x.Interval.Duration));
        Equal(81,p.Capacities.Single(x=>x.BusinessInstanceId=="run/business/1").AllocatedOwnerMinutes);
        Equal(80,p.Capacities.Single(x=>x.BusinessInstanceId=="run/business/2").AllocatedOwnerMinutes);
        Equal(80,p.Capacities.Single(x=>x.BusinessInstanceId=="run/business/3").AllocatedOwnerMinutes);
        AssertNoOverlap(p);
    }

    private static void OwnerTimeRedistribution()
    {
        var a=PlanBusiness("test.plan.a",BusinessType.OnlineStore,1080,1230,required:30);
        var b=PlanBusiness("test.plan.b",BusinessType.HomeFoodPreorder,1080,1230,required:120);
        var c=PlanCatalog(a,b);var s=PlanState(c);
        s.Businesses.Add(PlanInstance(a,"run/business/1"));
        s.Businesses.Add(PlanInstance(b,"run/business/2"));
        var p=OwnerDayAllocator.Plan(s,c);
        Equal(OwnerDayPlanStatus.Ready,p.Status);
        Equal(30,p.Capacities.Single(x=>x.BusinessInstanceId=="run/business/1").AllocatedOwnerMinutes);
        Equal(120,p.Capacities.Single(x=>x.BusinessInstanceId=="run/business/2").AllocatedOwnerMinutes);
        True(p.Capacities.All(x=>x.Status==BusinessPlanStatus.FullyAllocated));
        Equal(150,p.Allocations.Sum(x=>x.Interval.Duration));
        AssertNoOverlap(p);
    }

    private static void OwnerTimePermutations(int count)
    {
        var kinds=new[]{BusinessType.OnlineStore,BusinessType.HomeFoodPreorder,BusinessType.FreelanceService,BusinessType.CoffeeKiosk};
        var businesses=Enumerable.Range(0,count).Select(i=>
            PlanBusiness("test.plan."+((char)('a'+i)),kinds[i],1079,1320)).ToArray();
        var c=PlanCatalog(businesses);var s=PlanState(c);
        for(var i=0;i<count;i++) s.Businesses.Add(PlanInstance(businesses[i],"run/business/"+(i+1)));
        var expected=NormalizedPlan(OwnerDayAllocator.Plan(s,c));
        s.Businesses.Reverse();
        Equal(expected,NormalizedPlan(OwnerDayAllocator.Plan(s,c)));
        for(var i=0;i<count;i++)
        {
            var last=s.Businesses[s.Businesses.Count-1];
            s.Businesses.RemoveAt(s.Businesses.Count-1);
            s.Businesses.Insert(0,last);
            Equal(expected,NormalizedPlan(OwnerDayAllocator.Plan(s,c)));
        }
    }

    private static void OwnerTimeColdRestore()
    {
        var b=Scheduled();
        var f=EligibilityFixture(EligibilityCatalog(new[]{b},new[]{EligibilityCareer()}));
        Employ(f);
        ScheduledLaunch(f,b);
        foreach(var target in new[]{480,540,1020})
        {
            Committed(f,"owner-restore-boundary-"+target,new GameCommand(CommandKind.AdvanceBoundary));
            Equal(target,f.State().Minute);
        }
        var original=f.Session.ReadOwnerDayAllocation();
        var checkpoint=f.Checkpoint();
        var restored=f.Restore();
        for(var i=0;i<3;i++)
        {
            var actual=restored.Session.ReadOwnerDayAllocation();
            Bytes(Encoding.UTF8.GetBytes(NormalizedPlan(original)),
                Encoding.UTF8.GetBytes(NormalizedPlan(actual)));
        }
        Bytes(checkpoint,restored.Checkpoint());
    }

    private static void OwnerTimeReordering()
    {
        var a=PlanBusiness("test.plan.a",BusinessType.OnlineStore,1080,1200);
        var b=PlanBusiness("test.plan.b",BusinessType.HomeFoodPreorder,1140,1260);
        var c=PlanCatalog(a,b);var s=PlanState(c);
        s.Businesses.Add(PlanInstance(a,"run/business/1"));s.Businesses.Add(PlanInstance(b,"run/business/2"));
        var first=NormalizedPlan(OwnerDayAllocator.Plan(s,c));
        s.Businesses.Reverse();
        Equal(first,NormalizedPlan(OwnerDayAllocator.Plan(s,c)));
        Equal(first,NormalizedPlan(OwnerDayAllocator.Plan(s,c)));
    }

    private static void OwnerTimeReadModel()
    {
        var b=Scheduled();var f=EligibilityFixture(EligibilityCatalog(new[]{b}));
        ScheduledLaunch(f,b);
        var before=f.Checkpoint();var commits=f.Store.CommitCalls;
        var p=((IOwnerDayAllocationReadModel)f.Session).ReadOwnerDayAllocation();
        Equal(f.Session.Snapshot().Revision,p.StateRevision);
        Equal(Wednesday,p.Date);
        True(p.Allocations.All(x=>x.Interval.StartMinute>=480));
        Equal(NormalizedPlan(p),NormalizedPlan(f.Session.ReadOwnerDayAllocation()));
        Bytes(before,f.Checkpoint());Equal(commits,f.Store.CommitCalls);
    }

    private static void AssertNoOverlap(OwnerDayAllocation plan)
    {
        var owners=new HashSet<int>();
        foreach(var allocation in plan.Allocations)
            for(var i=allocation.Interval.StartMinute;i<allocation.Interval.EndMinute;i++)
                True(owners.Add(i));
        foreach(var interval in plan.EmploymentIntervals.Concat(plan.StudyIntervals).Concat(plan.ElapsedIntervals))
            for(var i=interval.StartMinute;i<interval.EndMinute;i++)True(!owners.Contains(i));
    }

    private static string NormalizedPlan(OwnerDayAllocation plan)
    {
        // Canonical full read-model output; this is also compared byte-for-byte
        // across input permutations, repeated previews and cold restore.
        var result=new StringBuilder().Append("status=").Append(plan.Status)
            .Append("|date=").Append(plan.Date)
            .Append("|revision=").Append(plan.StateRevision)
            .Append("|content=").Append(plan.ContentVersion)
            .Append("|cutoff=").Append(plan.CutoffMinute);
        void AppendIntervals(string name,IReadOnlyList<OwnerTimeInterval> intervals)
        {
            result.Append('|').Append(name).Append('=').Append(intervals.Count);
            foreach(var interval in intervals)
                result.Append(':').Append(interval.StartMinute).Append('-').Append(interval.EndMinute);
        }
        AppendIntervals("employment",plan.EmploymentIntervals);
        AppendIntervals("study",plan.StudyIntervals);
        AppendIntervals("elapsed",plan.ElapsedIntervals);
        AppendIntervals("free",plan.FreeIntervals);
        result.Append("|capacities=").Append(plan.Capacities.Count);
        foreach(var capacity in plan.Capacities)
            result.Append('|').Append(capacity.BusinessInstanceId).Append(':').Append(capacity.Status)
                .Append(':').Append(capacity.RequiredOwnerMinutes)
                .Append(':').Append(capacity.AllocatedOwnerMinutes)
                .Append(':').Append(capacity.CapacityPermyriad);
        result.Append("|allocations=").Append(plan.Allocations.Count);
        foreach(var allocation in plan.Allocations)
            result.Append('|').Append(allocation.DefinitionId).Append(':')
                .Append(allocation.DefinitionRevision).Append(':')
                .Append(allocation.BusinessInstanceId).Append('@')
                .Append(allocation.Interval.StartMinute).Append('-').Append(allocation.Interval.EndMinute);
        return result.ToString();
    }
}
