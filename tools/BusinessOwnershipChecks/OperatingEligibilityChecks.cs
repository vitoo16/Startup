#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StartupLife.Application;
using StartupLife.Core;
using StartupLife.Infrastructure;
using StartupLife.Simulation;

internal static partial class Program
{
    private static readonly SimDate Wednesday = new SimDate(2026, 9, 2);
    private static readonly DayOfWeek[] Weekdays = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

    private static void OperatingEligibilityChecks()
    {
        Check("M8-T02 immutable windows, copies, canonical sort and adjacency", OperatingDefinitionContracts);
        Check("M8-T02 malformed operating windows reject", InvalidOperatingWindows);
        Check("M8-T02 malformed and impossible owner requirements reject", InvalidOperatingRequirements);
        Check("M8-T02 catalog rejects windows outside global waking day", OperatingCatalogBounds);
        Check("M8-T02 result statuses and metric invariants", EligibilityResultContracts);
        Check("M8-T02 unemployed Freelance 240/120 eligible", () => AvailabilityCase(null, Wednesday, Wednesday, 240, true));
        Check("M8-T02 Developer workday evening 240/120 eligible", () => AvailabilityCase(EligibilityCareer(), Wednesday, Wednesday, 240, true));
        Check("M8-T02 Developer off-day eligible", () => AvailabilityCase(EligibilityCareer(), new SimDate(2026, 9, 5), Wednesday, 240, true));
        Check("M8-T02 early F&B 08-16 leaves 240", () => AvailabilityCase(EligibilityCareer(start:480,end:960), Wednesday, Wednesday, 240, true));
        Check("M8-T02 late F&B 14-21 leaves 60 and rejects", () => AvailabilityCase(EligibilityCareer(start:840,end:1260), Wednesday, Wednesday, 60, false));
        Check("M8-T02 threshold F&B 14-20 leaves exactly 120", () => AvailabilityCase(EligibilityCareer(start:840,end:1200), Wednesday, Wednesday, 120, true));
        Check("M8-T02 threshold minus one minute rejects", () => AvailabilityCase(EligibilityCareer(start:840,end:1201), Wednesday, Wednesday, 119, false));
        Check("M8-T02 late F&B off-day leaves 240", () => AvailabilityCase(EligibilityCareer(start:840,end:1260), new SimDate(2026,9,5), Wednesday, 240, true));
        Check("M8-T02 late F&B before first shift leaves 240", () => AvailabilityCase(EligibilityCareer(start:840,end:1260), Wednesday, Wednesday.AddDays(1), 240, true));
        Check("M8-T02 weekend configured shift subtracts career interval", () => AvailabilityCase(EligibilityCareer(start:840,end:1260,days:new[]{DayOfWeek.Saturday}), new SimDate(2026,9,5), new SimDate(2026,9,5), 60, false));
        Check("M8-T02 multiple windows add and half-open boundaries exclude overlap", MultipleOperatingWindows);
        Check("M8-T02 omitted business weekday measures zero", OmittedOperatingWeekday);
        Check("M8-T02 whole-date calculation ignores clock, study and portfolio", EligibilityClockAndPurity);
        Check("M8-T02 modes preserve hard rules and legacy policy", OperatingModes);
        Check("M8-T02 content resolution is exact and follows hard mode gates", EligibilityContentResolution);
        Check("M8-T02 read model is pure with date/revision and stale metadata", EligibilityReadModel);
        Check("M8-T02 command-query parity and rejected-byte atomicity", EligibilityCommandParity);
        Check("M8-T02 launch rejection precedence stays frozen", EligibilityLaunchPrecedence);
        Check("M8-T02 scheduled v2 roundtrip and cold retry", ScheduledRoundtrip);
        Check("M8-T02 historical launch then employment stays valid", HistoricalLaunchThenEmployment);
        Check("M8-T02 historical resign then same-day launch stays valid", HistoricalResignThenLaunch);
        Check("M8-T02 historical pre-first-shift ignores future shift", HistoricalBeforeFirstShift);
        Check("M8-T02 first shift exact start and one minute after preserve parity", FirstShiftBoundary);
        Check("M8-T02 employed illegal historical launch then resign remains corrupt", HistoricalIllegalThenResign);
        Check("M8-T02 known scheduled corruption wins over unrelated missing content", HistoricalMissingUnrelatedContent);
        Check("M8-T02 required career revision absent remains unsupported", HistoricalMissingCareer);
        Check("M8-T02 changed business revision never substitutes new balance", HistoricalChangedBusinessRevision);
        Check("M8-T02 unavailable earlier career cannot hide later unemployed corruption", HistoricalContinueAfterMissingCareer);
        Check("M8-T02 unavailable earlier career cannot hide later known-career schedule corruption", HistoricalLaterKnownCareer);
        Check("M8-T02 unavailable earlier career cannot hide later Manager corruption", HistoricalManagerAfterMissingCareer);
        Check("M8-T02 receipt date rather than current weekday decides historical eligibility", HistoricalReceiptDate);
        Check("M8-T02 forged first shift cannot make a historical launch eligible", HistoricalForgedFirstShift);
    }

    private static BusinessOperatingWindow Window(DayOfWeek day, int start, int end) => new BusinessOperatingWindow(day,start,end);
    private static BusinessDefinition Scheduled(string id = "test.scheduled.freelance", int required = 120,
        BusinessOperationMode mode = BusinessOperationMode.SideHustleCompatible, string revision = "scheduled-r1",
        IEnumerable<BusinessOperatingWindow>? windows = null, BusinessType type = BusinessType.FreelanceService) =>
        new BusinessDefinition(id, revision, id+".name", type, mode, 100,1000,
            new[]{PricingPosture.Budget,PricingPosture.Standard,PricingPosture.Premium},PricingPosture.Standard,1,1000,
            new BusinessOperatingRequirements(windows ?? Enumerable.Range(0,7).Select(day=>Window((DayOfWeek)day,1080,1320)),required));

    private static CareerDefinition EligibilityCareer(string id = "developer", int start = 540, int end = 1020,
        DayOfWeek[]? days = null, string revision = "schedule-r1") =>
        new CareerDefinition(id,revision,"career."+id,start,end,1,20,days??Weekdays,
            new[]{new CareerSceneDefinition(id+".scene","scene."+id,100,0,"communication",0)},
            new[]{new CareerRankDefinition(id+".rank",0,0,0,"communication",0)});

    private static ContentCatalog EligibilityCatalog(IEnumerable<BusinessDefinition>? businesses = null,
        IEnumerable<CareerDefinition>? careers = null, bool includeStart = true) =>
        new ContentCatalog("eligibility.fixture.v1",new[]{new SkillDefinition("communication","skill.communication",100,300,600,1000,1500)},
            careers??new[]{EligibilityCareer()},Array.Empty<CourseDefinition>(),
            includeStart?new[]{new CharacterStartDefinition("fresh",10000,10000,"base.female")}:Array.Empty<CharacterStartDefinition>(),
            businesses??new[]{Scheduled()},new EconomyBalanceDefinition(1,1,0),new DayScheduleDefinition(480,1320));

    private static Fixture EligibilityFixture(ContentCatalog? catalog = null, SimDate? date = null)
    {
        catalog ??= EligibilityCatalog();
        var f = new Fixture(catalog,state:GameSession.NewState(catalog,"run",12345,date??Wednesday));
        Committed(f,"create",new GameCommand(CommandKind.CreateCharacter,"fresh",18,"Owner","base.female"));
        return f;
    }
    private static void Committed(Fixture f,string id,GameCommand command) => Equal(CommandStatus.Committed,f.Execute(id,command).Status);
    private static void Employ(Fixture f,string id="developer") => Committed(f,"accept-"+f.Session.Snapshot().Revision,new GameCommand(CommandKind.AcceptJob,id));
    private static void ScheduledLaunch(Fixture f,BusinessDefinition? business=null)
    { business??=f.Catalog.Businesses.Values.First(); Committed(f,"launch-"+f.Session.Snapshot().Revision,BusinessCommands.Launch(business.Id,business.Revision,100)); }
    private static BusinessEligibilityResult Query(Fixture f,BusinessDefinition? business=null)
    { business??=f.Catalog.Businesses.Values.First(); return f.Session.ReadEligibility(business.Id,business.Revision).Eligibility; }
    private static JsonSaveSerializer EligibilitySerializer(ContentCatalog content) => new JsonSaveSerializer(content,GameSession.CreateRestoreValidator(),new SyntheticV0Migration(),new V1ToV2Migration());

    private static void OperatingDefinitionContracts()
    {
        var source=new[]{Window(DayOfWeek.Wednesday,1200,1320),Window(DayOfWeek.Monday,1080,1200),Window(DayOfWeek.Wednesday,1080,1200)};
        var requirements=new BusinessOperatingRequirements(source,120);
        source[0]=Window(DayOfWeek.Sunday,480,600);
        Equal(DayOfWeek.Monday,requirements.OperatingWindows[0].DayOfWeek);
        Equal(1080,requirements.OperatingWindows[1].StartMinute); Equal(1200,requirements.OperatingWindows[2].StartMinute);
        Throws<NotSupportedException>(()=>((IList<BusinessOperatingWindow>)requirements.OperatingWindows).Clear());
        var b=Scheduled(); True(b.OperatingRequirements!=null); Equal<BusinessOperatingRequirements?>(null,Freelance.OperatingRequirements);
        Throws<ArgumentNullException>(()=>new BusinessDefinition("test.null","r","key",BusinessType.FreelanceService,
            BusinessOperationMode.SideHustleCompatible,100,1000,new[]{PricingPosture.Standard},PricingPosture.Standard,1,1000,null!));
    }
    private static void InvalidOperatingWindows()
    {
        foreach(var x in new[]{(-1,60),(60,60),(70,60),(0,1441),(1320,480)})
            Throws<ArgumentException>(()=>Window(DayOfWeek.Monday,x.Item1,x.Item2));
        Throws<ArgumentException>(()=>Window((DayOfWeek)7,480,600));
        _=Window(DayOfWeek.Sunday,0,1440);
    }
    private static void InvalidOperatingRequirements()
    {
        var w=Window(DayOfWeek.Monday,1080,1200);
        Throws<ArgumentNullException>(()=>new BusinessOperatingRequirements(null!,1));
        Throws<ArgumentException>(()=>new BusinessOperatingRequirements(Array.Empty<BusinessOperatingWindow>(),1));
        Throws<ArgumentException>(()=>new BusinessOperatingRequirements(new BusinessOperatingWindow[]{null!},1));
        foreach(var required in new[]{0,-1,121,int.MaxValue}) Throws<ArgumentException>(()=>new BusinessOperatingRequirements(new[]{w},required));
        Throws<ArgumentException>(()=>new BusinessOperatingRequirements(new[]{w,w},1));
        Throws<ArgumentException>(()=>new BusinessOperatingRequirements(new[]{w,Window(DayOfWeek.Monday,1199,1320)},1));
        Throws<ArgumentException>(()=>new BusinessOperatingRequirements(new[]{w,Window(DayOfWeek.Tuesday,1080,1199)},120));
    }
    private static void OperatingCatalogBounds()
    {
        foreach(var x in new[]{(479,600),(1200,1321)})
            Throws<ArgumentException>(()=>EligibilityCatalog(new[]{Scheduled(required:1,windows:new[]{Window(DayOfWeek.Wednesday,x.Item1,x.Item2)})}));
        _=EligibilityCatalog(new[]{Scheduled(required:840,windows:new[]{Window(DayOfWeek.Wednesday,480,1320)})});
    }
    private static void EligibilityResultContracts()
    {
        foreach(BusinessEligibilityStatus status in Enum.GetValues(typeof(BusinessEligibilityStatus)))
        {
            var r=status==BusinessEligibilityStatus.InsufficientOwnerTime?new BusinessEligibilityResult(status,120,119):new BusinessEligibilityResult(status);
            Equal(status==BusinessEligibilityStatus.Eligible,r.IsEligible);
            True(r.IsEligible== (r.ReasonKey.Length==0));
        }
        Throws<ArgumentException>(()=>new BusinessEligibilityResult((BusinessEligibilityStatus)99));
        Throws<ArgumentException>(()=>new BusinessEligibilityResult(BusinessEligibilityStatus.Eligible,120,119));
        Throws<ArgumentException>(()=>new BusinessEligibilityResult(BusinessEligibilityStatus.InsufficientOwnerTime,120,120));
        Throws<ArgumentException>(()=>new BusinessEligibilityResult(BusinessEligibilityStatus.InsufficientOwnerTime));
        Throws<ArgumentException>(()=>new BusinessEligibilityResult(BusinessEligibilityStatus.UnsupportedOperationMode,120,0));
    }
    private static GameState AvailabilityState(CareerDefinition? career,SimDate date,SimDate first) => new GameState
    { DateIso=date.ToString(),Minute=480,Employment=career==null?null:new EmploymentState{CareerId=career.Id,DefinitionRevision=career.Revision,FirstShiftIso=first.ToString()} };
    private static void AvailabilityCase(CareerDefinition? career,SimDate date,SimDate first,int available,bool eligible)
    {
        var content=EligibilityCatalog(careers:career==null?Array.Empty<CareerDefinition>():new[]{career});
        var state=AvailabilityState(career,date,first);
        var bytes=JsonSaveSerializer.WriteObject(state);
        var r=BusinessEligibilityEvaluator.Evaluate(content,state,"test.scheduled.freelance","scheduled-r1");
        Equal<int?>(120,r.RequiredOwnerMinutes); Equal<int?>(available,r.AvailableOwnerMinutes); Equal(eligible,r.IsEligible);
        Equal(eligible?"":"business.insufficient_owner_time",r.ReasonKey);
        Bytes(bytes,JsonSaveSerializer.WriteObject(state));
    }
    private static void MultipleOperatingWindows()
    {
        var b=Scheduled(required:120,windows:new[]{Window(DayOfWeek.Wednesday,480,540),Window(DayOfWeek.Wednesday,1020,1080)});
        var c=EligibilityCatalog(new[]{b}); var state=AvailabilityState(c.Careers["developer"],Wednesday,Wednesday);
        var r=BusinessEligibilityEvaluator.Evaluate(c,state,b.Id,b.Revision); Equal<int?>(120,r.AvailableOwnerMinutes); True(r.IsEligible);
        b=Scheduled(required:120,windows:new[]{Window(DayOfWeek.Wednesday,480,600),Window(DayOfWeek.Wednesday,960,1080)});
        c=EligibilityCatalog(new[]{b}); r=BusinessEligibilityEvaluator.Evaluate(c,state,b.Id,b.Revision);
        Equal<int?>(120,r.AvailableOwnerMinutes); True(r.IsEligible);
    }
    private static void OmittedOperatingWeekday()
    {
        var b=Scheduled(windows:new[]{Window(DayOfWeek.Monday,1080,1320)}); var c=EligibilityCatalog(new[]{b});
        var r=BusinessEligibilityEvaluator.Evaluate(c,new GameState{DateIso=Wednesday.ToString()},b.Id,b.Revision);
        Equal<int?>(0,r.AvailableOwnerMinutes); Equal(BusinessEligibilityStatus.InsufficientOwnerTime,r.Status);
    }
    private static void EligibilityClockAndPurity()
    {
        var career=EligibilityCareer(start:840,end:1260); var c=EligibilityCatalog(careers:new[]{career});
        var state=AvailabilityState(career,Wednesday,Wednesday);
        state.Course=new CourseState{ProgressUnits=123}; state.Businesses.Add(new BusinessState{DefinitionId="other"});
        var before=JsonSaveSerializer.WriteObject(state);
        var a=BusinessEligibilityEvaluator.Evaluate(c,state,"test.scheduled.freelance","scheduled-r1"); Bytes(before,JsonSaveSerializer.WriteObject(state));
        state.Minute=1260; var b=BusinessEligibilityEvaluator.Evaluate(c,state,"test.scheduled.freelance","scheduled-r1");
        Equal(a.Status,b.Status); Equal(a.AvailableOwnerMinutes,b.AvailableOwnerMinutes);
    }
    private static void OperatingModes()
    {
        var kiosk=Scheduled("test.kiosk",480,BusinessOperationMode.FullTimeRequired,
            windows:Enumerable.Range(0,7).Select(day=>Window((DayOfWeek)day,540,1020)),type:BusinessType.CoffeeKiosk);
        var manager=Scheduled("test.manager",mode:BusinessOperationMode.ManagerOperable);
        var c=EligibilityCatalog(new[]{kiosk,manager,Freelance});
        foreach(var date in new[]{Wednesday,new SimDate(2026,9,5)})
        {
            var state=AvailabilityState(c.Careers["developer"],date,date.AddDays(1));
            var r=BusinessEligibilityEvaluator.Evaluate(c,state,kiosk.Id,kiosk.Revision);
            Equal(BusinessEligibilityStatus.EmploymentIncompatible,r.Status); Equal<int?>(480,r.RequiredOwnerMinutes); Equal<int?>(null,r.AvailableOwnerMinutes);
        }
        var unemployed=new GameState{DateIso=Wednesday.ToString()};
        var k=BusinessEligibilityEvaluator.Evaluate(c,unemployed,kiosk.Id,kiosk.Revision); True(k.IsEligible); Equal<int?>(480,k.AvailableOwnerMinutes);
        var m=BusinessEligibilityEvaluator.Evaluate(c,unemployed,manager.Id,manager.Revision); Equal(BusinessEligibilityStatus.UnsupportedOperationMode,m.Status); Equal<int?>(null,m.AvailableOwnerMinutes);
        var legacy=BusinessEligibilityEvaluator.Evaluate(c,unemployed,Freelance.Id,Freelance.Revision); True(legacy.IsEligible); Equal<int?>(null,legacy.RequiredOwnerMinutes);
    }
    private static void EligibilityContentResolution()
    {
        var full=Scheduled("test.full",mode:BusinessOperationMode.FullTimeRequired); var manager=Scheduled("test.manager",mode:BusinessOperationMode.ManagerOperable);
        var c=EligibilityCatalog(new[]{Scheduled(),full,manager,Freelance},careers:Array.Empty<CareerDefinition>());
        var state=AvailabilityState(EligibilityCareer(),Wednesday,Wednesday.AddDays(1));
        Equal(BusinessEligibilityStatus.BusinessDefinitionUnavailable,BusinessEligibilityEvaluator.Evaluate(c,state,"missing.business","r").Status);
        Equal(BusinessEligibilityStatus.BusinessRevisionUnavailable,BusinessEligibilityEvaluator.Evaluate(c,state,"test.scheduled.freelance","other").Status);
        Equal(BusinessEligibilityStatus.EmploymentIncompatible,BusinessEligibilityEvaluator.Evaluate(c,state,full.Id,full.Revision).Status);
        Equal(BusinessEligibilityStatus.UnsupportedOperationMode,BusinessEligibilityEvaluator.Evaluate(c,state,manager.Id,manager.Revision).Status);
        True(BusinessEligibilityEvaluator.Evaluate(c,state,Freelance.Id,Freelance.Revision).IsEligible);
        Throws<ContentCompatibilityException>(()=>BusinessEligibilityEvaluator.Evaluate(c,state,"test.scheduled.freelance","scheduled-r1"));
        c=EligibilityCatalog(careers:new[]{EligibilityCareer(revision:"new")});
        Throws<ContentCompatibilityException>(()=>BusinessEligibilityEvaluator.Evaluate(c,state,"test.scheduled.freelance","scheduled-r1"));
    }
    private static void EligibilityReadModel()
    {
        var f=EligibilityFixture(); Employ(f); var before=f.Checkpoint(); var storeBytes=f.Store.Bytes!; var commits=f.Store.CommitCalls; var state=f.State();
        var snapshot=f.Session.ReadEligibility("test.scheduled.freelance","scheduled-r1");
        Equal("test.scheduled.freelance",snapshot.DefinitionId); Equal("scheduled-r1",snapshot.DefinitionRevision);
        Equal(Wednesday,snapshot.Date); Equal(state.Revision,snapshot.StateRevision); True(snapshot.Eligibility.IsEligible);
        for(var i=0;i<3;i++) _=f.Session.ReadEligibility(snapshot.DefinitionId,snapshot.DefinitionRevision);
        Bytes(before,f.Checkpoint()); Bytes(storeBytes,f.Store.Bytes!); Equal(commits,f.Store.CommitCalls);
        Throws<ArgumentException>(()=>f.Session.ReadEligibility("Bad ID","r")); Throws<ArgumentException>(()=>f.Session.ReadEligibility(snapshot.DefinitionId," "));
        Committed(f,"resign",new GameCommand(CommandKind.Resign)); True(snapshot.StateRevision<f.Session.Snapshot().Revision);
    }
    private static void EligibilityCommandParity()
    {
        var manager=Scheduled(mode:BusinessOperationMode.ManagerOperable); var full=Scheduled(mode:BusinessOperationMode.FullTimeRequired);
        foreach(var b in new[]{Scheduled(),manager,full})
        {
            var f=EligibilityFixture(EligibilityCatalog(new[]{b})); Employ(f); var q=Query(f,b);
            if(q.IsEligible) ScheduledLaunch(f,b); else ExpectRejectedUnchanged(f,BusinessCommands.Launch(b.Id,b.Revision,100),q.ReasonKey);
        }
        var late=EligibilityCareer(start:840,end:1260); var insufficient=EligibilityFixture(EligibilityCatalog(careers:new[]{late})); Employ(insufficient);
        var result=Query(insufficient); Equal("business.insufficient_owner_time",result.ReasonKey);
        ExpectRejectedUnchanged(insufficient,BusinessCommands.Launch("test.scheduled.freelance","scheduled-r1",100),result.ReasonKey);
    }
    private static void EligibilityLaunchPrecedence()
    {
        var b=Scheduled(mode:BusinessOperationMode.ManagerOperable); var c=EligibilityCatalog(new[]{b}); var f=EligibilityFixture(c); Employ(f);
        ExpectRejectedUnchanged(f,new GameCommand(CommandKind.LaunchBusiness,b.Id,0,"bad",b.Revision),"business.invalid_payload");
        ExpectRejectedUnchanged(f,BusinessCommands.Launch("missing.business","r",0),"content.missing");
        ExpectRejectedUnchanged(f,BusinessCommands.Launch(b.Id,"missing",0),"business.definition_revision");
        ExpectRejectedUnchanged(f,BusinessCommands.Launch(b.Id,b.Revision,0),"business.invalid_investment");
        var state=f.State(); state.Arrears.Add(new ArrearState{Id="arrear",Amount=1});
        ThrowsRule("economy.arrears",()=>new SimulationEngine(c).Evaluate(state,BusinessCommands.Launch(b.Id,b.Revision,0),"op"));
        state.Arrears.Clear(); state.Cash=0;
        ThrowsRule("economy.insufficient_cash",()=>new SimulationEngine(c).Evaluate(state,BusinessCommands.Launch(b.Id,b.Revision,100),"op"));
        state.Cash=1000; state.Businesses.Add(new BusinessState{DefinitionId=b.Id,DefinitionRevision=b.Revision});
        ThrowsRule("business.type_active",()=>new SimulationEngine(c).Evaluate(state,BusinessCommands.Launch(b.Id,b.Revision,0),"op"));
        while(state.Businesses.Count<4) state.Businesses.Add(new BusinessState());
        ThrowsRule("business.active_limit",()=>new SimulationEngine(c).Evaluate(state,BusinessCommands.Launch(b.Id,b.Revision,0),"op"));
    }
    private static void ScheduledRoundtrip()
    {
        var f=EligibilityFixture(); Employ(f); ScheduledLaunch(f); var bytes=f.Checkpoint();
        Equal(2,JsonSaveSerializer.ReadObject<SaveEnvelope>(bytes).SchemaVersion); Equal(2,f.State().SaveVersion);
        var text=Encoding.UTF8.GetString(JsonSaveSerializer.WriteObject(f.State()));
        foreach(var field in new[]{"OperatingRequirements","OperatingWindows","AvailableOwnerMinutes","RequiredOwnerMinutes","Eligibility"}) True(!text.Contains(field));
        var restored=f.Restore(); var receipt=f.State().Receipts.Last();
        var retry=restored.ExecuteAt(receipt.CommandId,BusinessCommands.Launch("test.scheduled.freelance","scheduled-r1",100),0);
        Equal(CommandStatus.AlreadyCommitted,retry.Status); Equal(receipt.OperationId,retry.OperationId); Bytes(bytes,restored.Checkpoint());
        var business=restored.State().Businesses.Single(); Committed(restored,"reinvest",BusinessCommands.Reinvest(business.InstanceId,1));
    }
    private static void HistoricalLaunchThenEmployment()
    {
        var f=EligibilityFixture(EligibilityCatalog(careers:new[]{EligibilityCareer(start:840,end:1260)}));
        ScheduledLaunch(f); Employ(f); True(!Query(f).IsEligible);
        Equal(LoadStatus.Valid,f.Serializer.DeserializeAndValidate(f.Checkpoint()).Status);
        _=f.Restore();
    }
    private static void HistoricalResignThenLaunch()
    {
        var f=EligibilityFixture(EligibilityCatalog(careers:new[]{EligibilityCareer(start:840,end:1260)}));
        Employ(f); Committed(f,"resign",new GameCommand(CommandKind.Resign)); ScheduledLaunch(f);
        Equal<int?>(240,Query(f).AvailableOwnerMinutes); Equal(LoadStatus.Valid,f.Serializer.DeserializeAndValidate(f.Checkpoint()).Status);
    }
    private static void HistoricalBeforeFirstShift()
    {
        var f=EligibilityFixture(EligibilityCatalog(careers:new[]{EligibilityCareer(start:840,end:1260)}));
        Committed(f,"wake",new GameCommand(CommandKind.AdvanceBoundary));
        Committed(f,"sleep",new GameCommand(CommandKind.AdvanceBoundary)); Employ(f);
        True(f.State().Employment!.FirstShiftIso!=f.State().DateIso); ScheduledLaunch(f);
        Equal<int?>(240,Query(f).AvailableOwnerMinutes); Equal(LoadStatus.Valid,f.Serializer.DeserializeAndValidate(f.Checkpoint()).Status);
    }
    private static void FirstShiftBoundary()
    {
        var c=EligibilityCatalog(careers:new[]{EligibilityCareer(start:840,end:1260)});
        foreach(var minute in new[]{840,841})
        {
            // Detached runtime probe: exactly at start versus one minute after.
            var state=new GameState{Name="Owner",DateIso=Wednesday.ToString(),Minute=minute,RunId="run",
                Skills=new List<SkillState>{new SkillState{Id="communication"}}};
            _=new SimulationEngine(c).Evaluate(state,new GameCommand(CommandKind.AcceptJob,"developer"),"op");
            Equal(minute==840?Wednesday.ToString():Wednesday.AddDays(1).ToString(),state.Employment!.FirstShiftIso);
            var r=BusinessEligibilityEvaluator.Evaluate(c,state,"test.scheduled.freelance","scheduled-r1"); Equal<int?>(minute==840?60:240,r.AvailableOwnerMinutes);
        }
    }
    private static Fixture HostileScheduledHistory(bool resign = false)
    {
        // Deliberately permissive producer is only an adversarial-byte fixture. It is NOT
        // an example of shipping different behavior under a persisted revision.
        var f=EligibilityFixture(EligibilityCatalog(new[]{Scheduled(required:60)},new[]{EligibilityCareer(start:840,end:1260)}));
        Employ(f); ScheduledLaunch(f); if(resign) Committed(f,"resign",new GameCommand(CommandKind.Resign)); return f;
    }
    private static void HistoricalIllegalThenResign()
    {
        var f=HostileScheduledHistory(resign:true);
        var c=EligibilityCatalog(careers:new[]{EligibilityCareer(start:840,end:1260)});
        Equal(LoadStatus.Corrupt,EligibilitySerializer(c).DeserializeAndValidate(f.Checkpoint()).Status);
    }
    private static void HistoricalMissingUnrelatedContent()
    {
        var f=HostileScheduledHistory(resign:true);
        var c=EligibilityCatalog(careers:new[]{EligibilityCareer(start:840,end:1260)},includeStart:false);
        Equal(LoadStatus.Corrupt,EligibilitySerializer(c).DeserializeAndValidate(f.Checkpoint()).Status);
        // Missing saved business revision removes the proof, so it must not use new balance.
        c=EligibilityCatalog(new[]{Scheduled(revision:"scheduled-r2")},new[]{EligibilityCareer(start:840,end:1260)},false);
        Equal(LoadStatus.UnsupportedContent,EligibilitySerializer(c).DeserializeAndValidate(f.Checkpoint()).Status);
    }
    private static void HistoricalMissingCareer()
    {
        var f=HostileScheduledHistory(resign:true);
        foreach(var careers in new[]{Array.Empty<CareerDefinition>(),new[]{EligibilityCareer(start:840,end:1260,revision:"schedule-r2")}})
            Equal(LoadStatus.UnsupportedContent,EligibilitySerializer(EligibilityCatalog(careers:careers)).DeserializeAndValidate(f.Checkpoint()).Status);
    }
    private static void HistoricalChangedBusinessRevision()
    {
        var f=EligibilityFixture(); ScheduledLaunch(f);
        var c=EligibilityCatalog(new[]{Scheduled(required:240,revision:"scheduled-r2")});
        var load=EligibilitySerializer(c).DeserializeAndValidate(f.Checkpoint()); Equal(LoadStatus.UnsupportedContent,load.Status); Equal("save.content_revision",load.Reason);
    }
    private static void HistoricalContinueAfterMissingCareer()
    {
        var monday=Scheduled(windows:new[]{Window(DayOfWeek.Monday,1080,1320)});
        var f=EligibilityFixture(); Employ(f); Committed(f,"resign",new GameCommand(CommandKind.Resign)); ScheduledLaunch(f);
        var c=EligibilityCatalog(new[]{monday},Array.Empty<CareerDefinition>());
        Equal(LoadStatus.Corrupt,EligibilitySerializer(c).DeserializeAndValidate(f.Checkpoint()).Status);
    }
    private static void HistoricalLaterKnownCareer()
    {
        var late = EligibilityCareer("late",840,1260);
        var f = EligibilityFixture(EligibilityCatalog(new[]{Scheduled(required:60)},new[]{EligibilityCareer(),late}));
        Employ(f); Committed(f,"resign",new GameCommand(CommandKind.Resign)); Employ(f,"late"); ScheduledLaunch(f);
        var c = EligibilityCatalog(careers:new[]{late});
        Equal(LoadStatus.Corrupt,EligibilitySerializer(c).DeserializeAndValidate(f.Checkpoint()).Status);
    }
    private static void HistoricalManagerAfterMissingCareer()
    {
        var f=EligibilityFixture(); Employ(f); Committed(f,"resign",new GameCommand(CommandKind.Resign)); ScheduledLaunch(f);
        var c=EligibilityCatalog(new[]{Scheduled(mode:BusinessOperationMode.ManagerOperable)},Array.Empty<CareerDefinition>());
        Equal(LoadStatus.Corrupt,EligibilitySerializer(c).DeserializeAndValidate(f.Checkpoint()).Status);
    }
    private static void HistoricalReceiptDate()
    {
        var b=Scheduled(windows:new[]{Window(DayOfWeek.Wednesday,1080,1320)});
        var f=EligibilityFixture(EligibilityCatalog(new[]{b})); ScheduledLaunch(f,b);
        for(var i=0;i<3;i++) Committed(f,"advance-"+i,new GameCommand(CommandKind.AdvanceBoundary));
        Equal(Wednesday.AddDays(1),f.State().Date); True(!Query(f,b).IsEligible);
        Equal(LoadStatus.Valid,f.Serializer.DeserializeAndValidate(f.Checkpoint()).Status);
        Equal(LoadStatus.UnsupportedContent,EligibilitySerializer(EligibilityCatalog(new[]{b},includeStart:false)).DeserializeAndValidate(f.Checkpoint()).Status);
    }
    private static void HistoricalForgedFirstShift()
    {
        var f=HostileScheduledHistory(); var state=f.State(); state.Employment!.FirstShiftIso=Wednesday.AddDays(1).ToString();
        var c=EligibilityCatalog(careers:new[]{EligibilityCareer(start:840,end:1260)},includeStart:false);
        Equal(LoadStatus.Corrupt,Raw(EligibilitySerializer(c),state).Status);
    }
}
