using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using StartupLife.Core;
using StartupLife.Simulation;
using StartupLife.Infrastructure;
using StartupLife.Application;

internal static class Program
{
    private static int passed;
    private static readonly List<object> results = new();
    private static readonly string scratch = Path.Combine(Path.GetTempPath(), "StartupLifeChecks", Guid.NewGuid().ToString("N"));
    private static readonly string[] skillIds = { "communication", "negotiation", "time-management", "problem-solving", "networking", "leadership" };
    private static int Main(string[] args)
    {
        Directory.CreateDirectory(scratch);
        Check("xorshift32-v1 reference vector", () =>
        {
            uint state = 1; var expected = new uint[] { 270369, 67634689, 2647435461, 307599695, 2398689233 };
            foreach (var value in expected) Equal(value, DeterministicRng.Next(ref state));
        });
        Check("independent event stream leaves scheduler unchanged", () =>
        {
            var catalog = Catalog(); var a = GameSession.NewState(catalog, "a", 123, new SimDate(2026, 9, 7));
            var b = GameSession.NewState(catalog, "b", 123, new SimDate(2026, 9, 7));
            a.Employment = b.Employment = new EmploymentState();
            for (var i = 0; i < 100; i++) { var rng = b.EventRng; DeterministicRng.Next(ref rng); b.EventRng = rng; }
            for (var i = 0; i < 100; i++) Equal(QuotaScheduler.Consume(a, catalog.Careers["developer"]), QuotaScheduler.Consume(b, catalog.Careers["developer"]));
        });
        Check("Developer 20-slot quota exact across cycles", () =>
        {
            var scenes = Catalog().Careers["developer"].Scenes; var expected = new[] { 8, 4, 3, 2, 2, 1 }; uint rng = 987;
            for (var cycle = 0; cycle < 100; cycle++)
            {
                var deck = QuotaScheduler.Order(QuotaScheduler.Apportion(scenes, 20), "", ref rng);
                Equal(20, deck.Count);
                for (var i = 0; i < 6; i++) Equal(expected[i], deck.Count(x => x == scenes[i].Id));
                True(deck.Zip(deck.Skip(1), (a, b) => a != b).All(x => x));
            }
        });
        Check("largest remainder stable ties and <1 slot error", () =>
        {
            var scenes = new[] { Scene("z", 33), Scene("a", 33), Scene("b", 34) };
            var counts = QuotaScheduler.Apportion(scenes, 7); Equal(2, counts["z"]); Equal(2, counts["a"]); Equal(3, counts["b"]);
            for (var slots = 1; slots < 100; slots++)
                foreach (var scene in scenes) True(Math.Abs(QuotaScheduler.Apportion(scenes, slots)[scene.Id] - (double)slots * scene.Weight / 100) < 1);
            var ties = QuotaScheduler.Apportion(new[] { Scene("b", 50), Scene("a", 50) }, 1); Equal(1, ties["a"]);
        });
        Check("content rejects invalid quota, duplicate IDs and missing references", () =>
        {
            Throws(() => Career(new[] { Scene("a", 99) }));
            Throws(() => new ContentCatalog("test.v1", new[] { Skill("communication"), Skill("communication") }, Array.Empty<CareerDefinition>(), Array.Empty<CourseDefinition>(), Array.Empty<CharacterStartDefinition>(), new EconomyBalanceDefinition(1, 1, 0), new DayScheduleDefinition(480, 1320)));
            Throws(() => new ContentCatalog("test.v1", new[] { Skill("communication") }, new[] { Career(new[] { new CareerSceneDefinition("a", "scene.a", 100, 1, "missing", 1) }) }, Array.Empty<CourseDefinition>(), Array.Empty<CharacterStartDefinition>(), new EconomyBalanceDefinition(1, 1, 0), new DayScheduleDefinition(480, 1320)));
        });
        Check("character boundaries and rejected commands are byte identical", () =>
        {
            var f = new Fixture(); var before = f.Session.ExportCheckpoint();
            var invalid = f.Send(new GameCommand(CommandKind.CreateCharacter, "fresh", 17, "An", "base.female"));
            Equal(CommandStatus.Rejected, invalid.Status); Bytes(before, f.Session.ExportCheckpoint());
            f.Create(18); Equal(18, f.State().StartingAge);
            var g = new Fixture(); g.Create(40); Equal(40, g.State().StartingAge);
        });
        Check("Vietnamese name and course save round trip", () =>
        {
            var f = new Fixture(); f.Create(); f.Accept(); f.Buy(); f.Evening(); f.SendOk(new GameCommand(CommandKind.Study, amount: 60));
            var copy = f.Serializer.DeserializeAndValidate(f.Session.ExportCheckpoint()); Equal(LoadStatus.Valid, copy.Status);
            Equal("Nguyễn Ánh", copy.State!.Name); Equal(600000L, copy.State.Course!.ProgressUnits); Equal(1080, copy.State.Minute);
        });
        Check("salary accrues without spendable cash and retry grants once", () =>
        {
            var f = new Fixture(); f.Create(); f.Accept(); var cash = f.Session.Snapshot().Cash;
            f.Evening(); var state = f.State(); Equal(cash, state.Cash); Equal(40L, state.Employment!.Xp); Equal(1, state.Claims.Count);
            Equal(new RationalAmount(10000000, 22), SimulationEngine.ClaimAmount(state.Claims[0]));
            var receipt = state.Receipts.Last(); var before = f.Session.ExportCheckpoint();
            var retry = f.Session.Execute(new CommandEnvelope(state.RunId, receipt.CommandId, 0, new GameCommand(CommandKind.AdvanceBoundary)));
            Equal(CommandStatus.AlreadyCommitted, retry.Status); Bytes(before, f.Session.ExportCheckpoint());
            var conflict = f.Session.Execute(new CommandEnvelope(state.RunId, receipt.CommandId, state.Revision, new GameCommand(CommandKind.Resign)));
            Equal("command.id_conflict", conflict.ReasonKey); Bytes(before, f.Session.ExportCheckpoint());
        });
        Check("course charge once, study time and completion floor", () =>
        {
            var f = new Fixture(); f.Create(); f.Accept(); var cash = f.Session.Snapshot().Cash; var buy = f.Buy("buy-once");
            Equal(cash - 200000, f.Session.Snapshot().Cash);
            Equal(CommandStatus.AlreadyCommitted, f.Session.Execute(new CommandEnvelope("run", "buy-once", 0, new GameCommand(CommandKind.PurchaseCourse, "communication-basics"))).Status);
            Equal(cash - 200000, f.Session.Snapshot().Cash); f.Evening();
            var result = f.SendOk(new GameCommand(CommandKind.Study, amount: 1000)); Equal(300, result.MinutesConsumed); Equal(1320, f.Session.Snapshot().Instant.Minute);
            f.Day(); f.Evening(); result = f.SendOk(new GameCommand(CommandKind.Study, amount: 300)); Equal(60, result.MinutesConsumed);
            Equal(1, f.Session.Snapshot().SkillLevels["communication"]); Equal(1080, f.Session.Snapshot().Instant.Minute); True(f.State().Course == null);
        });
        Check("course prerequisites and single-active-course gate reject without mutation", () =>
        {
            var source = Catalog();
            var gated = new CourseDefinition("negotiation-advanced", "course.negotiation", "negotiation", 2, 1, 120, 0);
            var catalog = new ContentCatalog("fixture.course-gates.v1", source.Skills.Values, source.Careers.Values,
                source.Courses.Values.Concat(new[] { gated }), source.Starts.Values, source.Economy, source.Schedule);
            var f = new Fixture(catalog); f.Create();
            var before = f.Session.ExportCheckpoint();
            Equal("course.prerequisite", f.Send(new GameCommand(CommandKind.PurchaseCourse, "negotiation-advanced")).ReasonKey);
            Bytes(before, f.Session.ExportCheckpoint());

            f.Buy(); before = f.Session.ExportCheckpoint();
            Equal("course.already_active", f.Send(new GameCommand(CommandKind.PurchaseCourse, "problem-course")).ReasonKey);
            Bytes(before, f.Session.ExportCheckpoint());
        });
        Check("active course snapshot survives cold restore without checkpoint inspection", () =>
        {
            var f = new Fixture(); f.Create(); f.Accept(); var buy = f.Buy("course-read-model");
            True(buy.Outcome?.CourseChange != null); var activated = buy.Outcome!.CourseChange!;
            Equal(CourseChangeKind.Activated, activated.Kind); Equal(CourseCompletionReason.None, activated.CompletionReason);
            Equal("communication-basics", activated.DefinitionId); Equal("communication", activated.SkillId);
            Equal(0L, activated.PriorProgressUnits); Equal(0L, activated.NewProgressUnits);
            Equal(3600000L, activated.TargetUnits); Equal(1, activated.TargetLevel);

            f.SendOk(new GameCommand(CommandKind.AdvanceBoundary));
            var store = new MemoryStore(f.Serializer, f.Session.ExportCheckpoint());
            True(GameSession.TryRestore(f.Content, f.Serializer, store, out var restored, out var load));
            Equal(LoadStatus.Valid, load.Status); True(restored != null);
            var snapshot = restored!.Snapshot(); True(snapshot.ActiveCourse != null); var active = snapshot.ActiveCourse!;
            Equal(activated.InstanceId, active.InstanceId); Equal("communication-basics", active.DefinitionId);
            Equal("communication", active.SkillId); Equal(0L, active.ProgressUnits);
            Equal(3600000L, active.TargetUnits); Equal(1, active.TargetLevel);
        });
        Check("course outcome distinguishes progress and study completion", () =>
        {
            var f = new Fixture(Catalog(speed: 20000)); f.Create(); f.Accept(); f.Buy();
            f.SendOk(new GameCommand(CommandKind.AdvanceBoundary));
            var progress = f.SendOk(new GameCommand(CommandKind.Study, amount: 60), "course-progress");
            True(progress.Outcome?.CourseChange != null); var progressed = progress.Outcome!.CourseChange!;
            Equal(CourseChangeKind.Progressed, progressed.Kind); Equal(CourseCompletionReason.None, progressed.CompletionReason);
            Equal(0L, progressed.PriorProgressUnits); Equal(1200000L, progressed.NewProgressUnits);
            Equal(3600000L, progressed.TargetUnits); Equal(1200000L, f.Session.Snapshot().ActiveCourse!.ProgressUnits);

            f.Evening();
            var complete = f.SendOk(new GameCommand(CommandKind.Study, amount: 500), "course-complete");
            True(complete.Outcome?.CourseChange != null); var completed = complete.Outcome!.CourseChange!;
            Equal(CourseChangeKind.Completed, completed.Kind); Equal(CourseCompletionReason.StudyTargetReached, completed.CompletionReason);
            Equal(1200000L, completed.PriorProgressUnits); Equal(3600000L, completed.NewProgressUnits);
            Equal("communication-basics", completed.DefinitionId); Equal("communication", completed.SkillId);
            True(f.Session.Snapshot().ActiveCourse == null); Equal(1, f.Session.Snapshot().SkillLevels["communication"]);
        });
        Check("career grant course completion has explicit target-already-met reason", () =>
        {
            var f = new Fixture(Catalog(promotionXp: 40, promotionDays: 0)); f.Create(); f.Accept();
            var purchase = f.SendOk(new GameCommand(CommandKind.PurchaseCourse, "problem-course"), "problem-read-model");
            True(purchase.Outcome?.CourseChange != null); var courseId = purchase.Outcome!.CourseChange!.InstanceId;
            f.SendOk(new GameCommand(CommandKind.AdvanceBoundary));
            f.SendOk(new GameCommand(CommandKind.AdvanceBoundary));
            CommandResult scene = null!;
            for (var i = 0; i < 4; i++) scene = f.SendOk(new GameCommand(CommandKind.AdvanceBoundary), "promotion-scene-" + i);
            True(scene.Outcome?.CourseChange != null); var completed = scene.Outcome!.CourseChange!;
            Equal(CourseChangeKind.Completed, completed.Kind);
            Equal(CourseCompletionReason.SkillTargetAlreadyMet, completed.CompletionReason);
            Equal(courseId, completed.InstanceId); Equal("problem-course", completed.DefinitionId); Equal("problem-solving", completed.SkillId);
            Equal(0L, completed.PriorProgressUnits); Equal(3600000L, completed.NewProgressUnits);
            True(f.Session.Snapshot().ActiveCourse == null); Equal(2, f.Session.Snapshot().SkillLevels["problem-solving"]);
        });
        Check("LearningSpeed changes duration and study never overlaps work/sleep", () =>
        {
            var f = new Fixture(Catalog(speed: 20000)); f.Create(); f.Accept(); f.Buy();
            var before = f.Session.ExportCheckpoint(); Equal("time.unavailable", f.Send(new GameCommand(CommandKind.Study, amount: 60)).ReasonKey); Bytes(before, f.Session.ExportCheckpoint());
            f.SendOk(new GameCommand(CommandKind.AdvanceBoundary));
            var morning = f.SendOk(new GameCommand(CommandKind.Study, amount: 180)); Equal(60, morning.MinutesConsumed); Equal(540, f.State().Minute);
            before = f.Session.ExportCheckpoint(); Equal("time.unavailable", f.Send(new GameCommand(CommandKind.Study, amount: 10)).ReasonKey); Bytes(before, f.Session.ExportCheckpoint());
            f.Evening(); var result = f.SendOk(new GameCommand(CommandKind.Study, amount: 500)); Equal(120, result.MinutesConsumed); Equal(1140, f.State().Minute);
            True(f.State().Course == null);
        });
        Check("career XP remains separate from skills; promotion milestone idempotent", () =>
        {
            var f = new Fixture(Catalog(promotionXp: 40, promotionDays: 0)); f.Create(); f.Accept(); f.Evening();
            Equal(40L, f.Session.Snapshot().CareerXp); Equal(1, f.Session.Snapshot().Rank); Equal(2, f.Session.Snapshot().SkillLevels["problem-solving"]);
            var grants = f.State().Grants.Count; f.Day(); f.Evening(); Equal(grants, f.State().Grants.Count);
        });
        Check("skill levels cover 0-5 boundaries and weaker grants cannot lower earned strength", () =>
        {
            var definition = Skill("communication");
            var state = new SkillState { Id = "communication" };
            Equal(0, GameSnapshot.Level(state, definition));
            state.Exposure = 99; Equal(0, GameSnapshot.Level(state, definition));
            state.Exposure = 100; Equal(1, GameSnapshot.Level(state, definition));
            state.Exposure = 299; Equal(1, GameSnapshot.Level(state, definition));
            state.Exposure = 300; Equal(2, GameSnapshot.Level(state, definition));
            state.Exposure = 599; Equal(2, GameSnapshot.Level(state, definition));
            state.Exposure = 600; state.GrantedLevel = 2; Equal(3, GameSnapshot.Level(state, definition));
            state.Exposure = 999; Equal(3, GameSnapshot.Level(state, definition));
            state.Exposure = 1000; Equal(4, GameSnapshot.Level(state, definition));
            state.Exposure = 1499; Equal(4, GameSnapshot.Level(state, definition));
            state.Exposure = 1500; state.GrantedLevel = 1; Equal(5, GameSnapshot.Level(state, definition));
            state.Exposure = 100; state.GrantedLevel = 4; Equal(4, GameSnapshot.Level(state, definition));
        });
        Check("course target met by career completes without refund/second grant", () =>
        {
            var f = new Fixture(Catalog(promotionXp: 40, promotionDays: 0)); f.Create(); f.Accept();
            f.SendOk(new GameCommand(CommandKind.PurchaseCourse, "problem-course")); var cash = f.Session.Snapshot().Cash;
            f.Evening(); True(f.State().Course == null); Equal(cash, f.Session.Snapshot().Cash); Equal(1, f.State().CompletedCourses.Count);
            True(!f.State().Grants.Any(x => x.StartsWith("course/", StringComparison.Ordinal)));
        });
        Check("mid-cycle/course restore and next outcomes deterministic", () =>
        {
            var a = new Fixture(); a.Create(); a.Accept(); a.Buy(); a.Evening(); a.SendOk(new GameCommand(CommandKind.Study, amount: 90));
            var b = new Fixture(a.Content, a.State());
            for (var i = 0; i < 30; i++)
            {
                var sa = a.Session.Snapshot(); var sb = b.Session.Snapshot(); Equal(sa.Revision, sb.Revision);
                var request = new CommandEnvelope("run", "continue-" + i, sa.Revision, new GameCommand(CommandKind.AdvanceBoundary));
                Equal(a.Session.Execute(request).Status, b.Session.Execute(request).Status); Bytes(a.Session.ExportCheckpoint(), b.Session.ExportCheckpoint());
            }
        });
        Check("suspension/real elapsed time never advances simulation", () =>
        {
            var f = new Fixture(); f.Create(); f.Accept(); f.Evening(); var saved = f.Session.ExportCheckpoint();
            var restored = new Fixture(f.Content, f.State()); Bytes(saved, restored.Session.ExportCheckpoint());
            for (var i = 0; i < 100; i++) _ = restored.Session.Snapshot(); Bytes(saved, restored.Session.ExportCheckpoint());
        });
        Check("employment-free planner covers founder-equivalent availability and clamps allocated minutes", () =>
        {
            var source = Catalog();
            var longCourse = new CourseDefinition("long-course", "course.long", "communication", 5, 0, 2000, 0);
            var catalog = new ContentCatalog("fixture.calendar.v1", source.Skills.Values, source.Careers.Values,
                source.Courses.Values.Concat(new[] { longCourse }), source.Starts.Values, source.Economy, source.Schedule);
            var f = new Fixture(catalog); f.Create();
            f.SendOk(new GameCommand(CommandKind.AdvanceBoundary));
            var engine = new SimulationEngine(catalog);
            Equal(840, engine.FreeMinutes(f.State()));
            f.SendOk(new GameCommand(CommandKind.PurchaseCourse, "long-course"));
            var study = f.SendOk(new GameCommand(CommandKind.Study, amount: 5000));
            Equal(840, study.MinutesConsumed); Equal(1320, f.State().Minute); Equal(0, engine.FreeMinutes(f.State()));

            var resumed = new Fixture(); resumed.Create(); resumed.Accept();
            resumed.SendOk(new GameCommand(CommandKind.AdvanceBoundary));
            resumed.SendOk(new GameCommand(CommandKind.AdvanceBoundary));
            resumed.SendOk(new GameCommand(CommandKind.AdvanceBoundary));
            resumed.SendOk(new GameCommand(CommandKind.Resign));
            var resignedState = resumed.State();
            True(resignedState.Employment == null);
            Equal(1320 - resignedState.Minute, new SimulationEngine(resumed.Content).FreeMinutes(resignedState));
        });
        Check("full month salary, expenses, payday and batch retry", () =>
        {
            var f = new Fixture(); f.Create(); f.Accept();
            var request = new CommandEnvelope("run", "month", f.Session.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary));
            Equal("TargetReached", f.Session.AdvanceMonth(request).StopReason);
            Equal(new SimDate(2026, 10, 1), f.State().Date); Equal(11000000L, f.State().Cash);
            Equal(880L, f.State().Employment!.Xp); Equal(0, f.State().Claims.Count);
            var before = f.Session.ExportCheckpoint(); Equal("TargetReached", f.Session.AdvanceMonth(request).StopReason); Bytes(before, f.Session.ExportCheckpoint());
            Equal(1, f.State().Ledger.Count(x => x.Category == "salary.payment"));
        });
        Check("partial-month resignation preserves exact earned pay and biography", () =>
        {
            var f = new Fixture(); f.Create(); f.Accept(); f.Evening(); f.SendOk(new GameCommand(CommandKind.Resign));
            Equal(1, f.State().PreviousEmployment.Count); True(f.State().Employment == null);
            f.Session.AdvanceMonth(new CommandEnvelope("run", "resigned-month", f.Session.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary)));
            Equal(1454545L, f.State().Cash); Equal(new RationalAmount(5, 11), SimulationEngine.ClaimAmount(f.State().Claims.Single()));
        });
        Check("oldest arrears settle before spending and employment recovers", () =>
        {
            var f = new Fixture(Catalog(cash: 0)); f.Create(); f.Accept(); Equal(1000000L, f.Session.Snapshot().Arrears);
            var before = f.Session.ExportCheckpoint(); Equal("economy.arrears", f.Send(new GameCommand(CommandKind.PurchaseCourse, "communication-basics")).ReasonKey); Bytes(before, f.Session.ExportCheckpoint());
            f.Session.AdvanceMonth(new CommandEnvelope("run", "recover-month", f.Session.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary)));
            Equal(0L, f.Session.Snapshot().Arrears); Equal(8000000L, f.Session.Snapshot().Cash); f.Buy();
        });
        Check("repeated zero-cash shortfalls accumulate arrears without negative cash", () =>
        {
            var f = new Fixture(Catalog(cash: 0)); f.Create();
            Equal(1000000L, f.Session.Snapshot().Arrears); Equal(0L, f.Session.Snapshot().Cash);
            Equal("TargetReached", f.Session.AdvanceMonth(new CommandEnvelope("run", "shortfall-1", f.Session.Snapshot().Revision,
                new GameCommand(CommandKind.AdvanceBoundary))).StopReason);
            Equal(2000000L, f.Session.Snapshot().Arrears); Equal(0L, f.Session.Snapshot().Cash);
            Equal("TargetReached", f.Session.AdvanceMonth(new CommandEnvelope("run", "shortfall-2", f.Session.Snapshot().Revision,
                new GameCommand(CommandKind.AdvanceBoundary))).StopReason);
            Equal(3000000L, f.Session.Snapshot().Arrears); Equal(0L, f.Session.Snapshot().Cash);
            True(f.State().Arrears.Select(x => x.DueIso).SequenceEqual(f.State().Arrears.Select(x => x.DueIso).OrderBy(x => x, StringComparer.Ordinal)));
        });
        Check("insufficient funds and stale revision reject without mutation", () =>
        {
            var f = new Fixture(Catalog(cash: 100)); f.Create(); var before = f.Session.ExportCheckpoint();
            Equal("economy.arrears", f.Send(new GameCommand(CommandKind.PurchaseCourse, "communication-basics")).ReasonKey); Bytes(before, f.Session.ExportCheckpoint());
            var g = new Fixture(Catalog(cash: 100, cost: 0)); g.Create(); before = g.Session.ExportCheckpoint();
            Equal("economy.insufficient_cash", g.Send(new GameCommand(CommandKind.PurchaseCourse, "communication-basics")).ReasonKey); Bytes(before, g.Session.ExportCheckpoint());
            Equal("command.stale_state", g.Session.Execute(new CommandEnvelope("run", "stale", 0, new GameCommand(CommandKind.AcceptJob, "developer"))).ReasonKey); Bytes(before, g.Session.ExportCheckpoint());
        });
        Check("overflow rejects entire boundary including RNG/rewards", () =>
        {
            var f = new Fixture(Catalog(sceneXp: long.MaxValue)); f.Create(); f.Accept();
            f.SendOk(new GameCommand(CommandKind.AdvanceBoundary)); f.SendOk(new GameCommand(CommandKind.AdvanceBoundary));
            f.SendOk(new GameCommand(CommandKind.AdvanceBoundary)); Equal(long.MaxValue, f.State().Employment!.Xp);
            var before = f.Session.ExportCheckpoint(); Equal("arithmetic.overflow", f.Send(new GameCommand(CommandKind.AdvanceBoundary)).ReasonKey); Bytes(before, f.Session.ExportCheckpoint());
        });
        Check("leap years, month end and hospitality weekends", () =>
        {
            Equal(new SimDate(2024, 2, 29), new SimDate(2024, 2, 28).AddDays(1)); Equal(new SimDate(2024, 3, 1), new SimDate(2024, 2, 29).AddDays(1));
            var office = Catalog().Careers["developer"]; Equal(21, SimulationEngine.ScheduledDays(office, new SimDate(2024, 2, 1)));
            var hospitality = Career(office.Scenes, days: new[] { DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday });
            True(hospitality.WorksOn(new SimDate(2026, 9, 12))); True(!office.WorksOn(new SimDate(2026, 9, 12)));
            var f = new Fixture(Catalog(), start: new SimDate(2026, 9, 12)); f.Create(); f.Accept(); f.Day(); Equal(0L, f.State().Employment!.Xp);
        });
        Check("payday clamp and payday work excluded from current payment", () =>
        {
            var f = new Fixture(Catalog(payday: 31, cost: 0), start: new SimDate(2024, 2, 28)); f.Create(); f.Accept(); f.Day();
            Equal(new SimDate(2024, 2, 29), f.State().Date); Equal(3476190L, f.State().Cash);
            f.Evening(); Equal(2, f.State().Claims.Count); Equal(3476190L, f.State().Cash);
        });
        Check("rank changes preserve committed deck prefix/history", () =>
        {
            var f = new Fixture(Catalog(promotionXp: 40, promotionDays: 0)); f.Create(); f.Accept(); f.Evening();
            var prefix = f.State().Scheduler.Deck.Take(4).ToArray(); var history = f.State().History.ToArray(); f.Day(); f.Evening();
            True(prefix.SequenceEqual(f.State().Scheduler.Deck.Take(4))); True(history.All(x => f.State().History.Contains(x))); Equal(2L, f.State().Scheduler.Generation);
        });
        Check("pending required choice stops month advancement", () =>
        {
            var f = new Fixture(); f.Create(); var state = f.State(); state.PendingChoiceId = "choice.test"; var g = new Fixture(f.Content, state);
            var before = g.Session.ExportCheckpoint(); Equal("PlayerChoice", g.Session.AdvanceMonth(new CommandEnvelope("run", "choice-month", state.Revision, new GameCommand(CommandKind.AdvanceBoundary))).StopReason); Bytes(before, g.Session.ExportCheckpoint());
        });
        Check("playback acknowledgement cannot add rewards", () =>
        {
            var f = new Fixture(); f.Create(); f.Accept(); f.Evening(); var before = f.State();
            f.SendOk(new GameCommand(CommandKind.AcknowledgePlayback, before.CurrentActivity, 10));
            Equal(before.Cash, f.State().Cash); Equal(before.Employment!.Xp, f.State().Employment!.Xp); Equal(before.Scheduler.Cursor, f.State().Scheduler.Cursor);
            Equal(before.CurrentCue, f.State().CurrentCue); Equal(10, f.State().PlaybackCursor);
        });
        Check("committed outcome exposes transactions and survives old retry after restore", () =>
        {
            var f = new Fixture(); f.Create(); f.Accept(); var buy = f.Buy("buy-outcome");
            True(buy.Outcome != null); var expected = buy.Outcome!;
            Equal("buy-outcome", expected.CommandId); Equal(buy.OperationId, expected.OperationId);
            Equal(-200000L, expected.CashDelta); Equal(expected.Start, expected.End);
            Equal(1, expected.LedgerEntries.Count); Equal("course.purchase", expected.LedgerEntries[0].Category);
            Equal(-200000L, expected.LedgerEntries[0].CashDelta); Equal(200000L, expected.LedgerEntries[0].Amount);
            Equal(1, expected.HistoryEntries.Count); Equal("course.purchased:communication-basics", expected.HistoryEntries[0]);
            ThrowsNotSupported(() => ((IList<OutcomeLedgerEntry>)expected.LedgerEntries).Clear());

            f.Evening();
            var retry = f.Session.Execute(new CommandEnvelope("run", "buy-outcome", 0,
                new GameCommand(CommandKind.PurchaseCourse, "communication-basics")));
            Equal(CommandStatus.AlreadyCommitted, retry.Status); True(retry.Outcome != null); SameOutcome(expected, retry.Outcome!);

            var restored = new Fixture(f.Content, f.State());
            var retryAfterReload = restored.Session.Execute(new CommandEnvelope("run", "buy-outcome", 0,
                new GameCommand(CommandKind.PurchaseCourse, "communication-basics")));
            Equal(CommandStatus.AlreadyCommitted, retryAfterReload.Status); True(retryAfterReload.Outcome != null);
            SameOutcome(expected, retryAfterReload.Outcome!);
        });
        Check("career boundary outcome exposes activity interval progression and reached instant", () =>
        {
            var f = new Fixture(); f.Create(); f.Accept();
            f.SendOk(new GameCommand(CommandKind.AdvanceBoundary));
            f.SendOk(new GameCommand(CommandKind.AdvanceBoundary));
            var scene = f.SendOk(new GameCommand(CommandKind.AdvanceBoundary), "scene-outcome");
            True(scene.Outcome != null); var outcome = scene.Outcome!;
            Equal(540, outcome.Start.Minute); Equal(660, outcome.End.Minute); Equal(120, outcome.MinutesConsumed);
            True(outcome.Cue.Length > 0); Equal("run/activity/2026-09-01/540", outcome.ActivityId);
            Equal(10L, outcome.CareerXpDelta); Equal(0, outcome.PriorRank); Equal(0, outcome.NewRank);
            var skill = outcome.SkillDeltas.Single(x => x.SkillId == "communication");
            Equal(1L, skill.ExposureDelta); Equal(0, skill.PriorLevel); Equal(0, skill.NewLevel);
            var snapshot = f.Session.Snapshot(); Equal(outcome.ActivityId, snapshot.CurrentActivityId); Equal(0, snapshot.PlaybackCursor);

            var advance = f.Session.AdvanceDay(new CommandEnvelope("run", "outcome-day", snapshot.Revision,
                new GameCommand(CommandKind.AdvanceBoundary)));
            Equal("TargetReached", advance.StopReason); Equal(f.Session.Snapshot().Instant, advance.ReachedInstant);
            True(advance.Boundaries.All(x => x.Outcome != null));
        });
        Check("playback outcome changes cursor only and snapshot publishes it", () =>
        {
            var f = new Fixture(); f.Create(); f.Accept(); f.Evening(); var before = f.State();
            var snapshot = f.Session.Snapshot(); True(snapshot.CurrentActivityId.Length > 0); Equal(0, snapshot.PlaybackCursor);
            var ack = f.SendOk(new GameCommand(CommandKind.AcknowledgePlayback, snapshot.CurrentActivityId, 10), "ack-outcome");
            True(ack.Outcome != null); var outcome = ack.Outcome!;
            Equal(snapshot.CurrentActivityId, outcome.ActivityId); Equal(10, outcome.PlaybackCursor); Equal(0L, outcome.CashDelta);
            Equal(0L, outcome.CareerXpDelta); Equal(0, outcome.LedgerEntries.Count); Equal(0, outcome.SkillDeltas.Count);
            Equal(0, outcome.GrantedIds.Count); Equal(0, outcome.HistoryEntries.Count); Equal(outcome.Start, outcome.End);
            var after = f.State(); Equal(before.Cash, after.Cash); Equal(before.Employment!.Xp, after.Employment!.Xp);
            Equal(before.Scheduler.Cursor, after.Scheduler.Cursor); Equal(before.CurrentCue, after.CurrentCue);
            Equal(10, f.Session.Snapshot().PlaybackCursor);
        });
        Check("root playback cue and cursor forgery are corrupt before restore publication", () =>
        {
            var f = new Fixture(); f.Create(); f.Accept(); f.Evening();
            var cueState = f.State(); cueState.CurrentCue = "forged.root.cue";
            var cueBytes = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(cueState), 1, cueState.Revision);
            Equal(LoadStatus.Corrupt, f.Serializer.DeserializeAndValidate(cueBytes).Status);
            var cueStore = new MemoryStore(f.Serializer, cueBytes);
            True(!GameSession.TryRestore(f.Content, f.Serializer, cueStore, out var cueSession, out var cueLoad));
            True(cueSession == null); Equal(LoadStatus.Corrupt, cueLoad.Status);

            var activity = f.Session.Snapshot().CurrentActivityId;
            f.SendOk(new GameCommand(CommandKind.AcknowledgePlayback, activity, 10), "root-cursor-source");
            var cursorState = f.State(); Equal(10, cursorState.PlaybackCursor); cursorState.PlaybackCursor = 0;
            var cursorBytes = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(cursorState), 1, cursorState.Revision);
            Equal(LoadStatus.Corrupt, f.Serializer.DeserializeAndValidate(cursorBytes).Status);
            var cursorStore = new MemoryStore(f.Serializer, cursorBytes);
            True(!GameSession.TryRestore(f.Content, f.Serializer, cursorStore, out var cursorSession, out var cursorLoad));
            True(cursorSession == null); Equal(LoadStatus.Corrupt, cursorLoad.Status);
        });
        Check("playback provenance does not mask unsupported content", () =>
        {
            var f = new Fixture(); f.Create(); f.Accept(); f.Buy(); var state = f.State();
            state.Course!.DefinitionId = "missing-course"; state.CurrentCue = "forged.root.cue";
            var bytes = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(state), SaveSchema.CurrentVersion, state.Revision);
            Equal(LoadStatus.UnsupportedContent, f.Serializer.DeserializeAndValidate(bytes).Status);
        });
        Check("candidate serialization validation is contained as state invalid", () =>
        {
            var content = Catalog();
            var inner = new JsonSaveSerializer(content, GameSession.CreateRestoreValidator(), new SyntheticV0Migration());
            var serializer = new NthSerializeFailureSerializer(inner);
            var initial = GameSession.NewState(content, "run", 12345, new SimDate(2026, 9, 1));
            var store = new MemoryStore(serializer, null);
            var session = new GameSession(initial, content, serializer, store);
            serializer.Arm(2);
            var result = session.Execute(new CommandEnvelope("run", "serialization-invalid", 0,
                new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "Nguyễn Ánh", "base.female")));
            Equal(CommandStatus.Rejected, result.Status); Equal("state.invalid", result.ReasonKey);
            Equal(0L, session.Snapshot().Revision); Equal(LoadStatus.Missing, store.Read().Status);
        });
        Check("committed non-boundary cue forgery is corrupt before outcome publication", () =>
        {
            var f = new Fixture(); f.Create(); f.Accept(); f.Buy("cue-source");
            var forged = f.State();
            var receipt = forged.Receipts.Single(x => x.CommandId == "cue-source");
            Equal("course.purchased", receipt.Cue);
            receipt.Cue = "forged.cue";
            var bytes = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(forged), 1, forged.Revision);
            Equal(LoadStatus.Corrupt, f.Serializer.DeserializeAndValidate(bytes).Status);
        });
        Check("synthetic v0 migration and current continuation", () =>
        {
            var f = new Fixture();
            var fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "synthetic-v0.json"));
            var migrated = f.Serializer.DeserializeAndValidate(fixture); Equal(LoadStatus.Valid, migrated.Status); Equal(SaveSchema.CurrentVersion, migrated.State!.SaveVersion);
            var current = f.Serializer.DeserializeAndValidate(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "current-v1.json")));
            Equal(LoadStatus.Valid, current.Status); Equal(current.State!.Name, migrated.State.Name); Equal(current.State.Cash, migrated.State.Cash);
            var g = new Fixture(f.Content, migrated.State); g.Evening(); Equal(40L, g.State().Employment!.Xp);
        });
        Check("future/content-incompatible saves preserved without backup fallback", () =>
        {
            var f = new Fixture(); f.Create(); var store = FileStore(f, "future"); var original = f.Session.ExportCheckpoint();
            var future = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(f.State()), 99, f.State().Revision);
            File.WriteAllBytes(PathFor("future"), future); File.WriteAllBytes(PathFor("future") + ".backup", original);
            Equal(LoadStatus.FutureVersion, store.Read().Status); Equal(WriteStatus.Failed, store.Commit(original, 0)); Bytes(future, File.ReadAllBytes(PathFor("future")));
            var incompatible = f.State(); incompatible.ContentVersion = "future.content";
            Equal(LoadStatus.UnsupportedContent, f.Serializer.DeserializeAndValidate(JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(incompatible), SaveSchema.CurrentVersion, incompatible.Revision)).Status);
        });
        Check("atomic storage before-replace failure retains state and backup", () =>
        {
            var f = new Fixture(); f.Create(); f.Accept(); var initial = f.State(); var path = PathFor("before"); File.WriteAllBytes(path, f.Session.ExportCheckpoint());
            var store = new AtomicFileSaveStore(path, f.Serializer, phase => phase == "before-replace"); var session = new GameSession(initial, f.Content, f.Serializer, store);
            var before = session.ExportCheckpoint(); Equal(CommandStatus.PersistenceFailed, session.Execute(new CommandEnvelope("run", "failed", initial.Revision, new GameCommand(CommandKind.AdvanceBoundary))).Status);
            Bytes(before, session.ExportCheckpoint()); Bytes(before, File.ReadAllBytes(path)); True(!File.Exists(path + ".tmp"));
        });
        Check("post-replace ambiguity blocks commands then reconciles retry", () =>
        {
            var f = new Fixture(); f.Create(); f.Accept(); var path = PathFor("after"); File.WriteAllBytes(path, f.Session.ExportCheckpoint()); var initial = f.State();
            var store = new AtomicFileSaveStore(path, f.Serializer, phase => phase == "after-replace"); var session = new GameSession(initial, f.Content, f.Serializer, store);
            var request = new CommandEnvelope("run", "ambiguous", initial.Revision, new GameCommand(CommandKind.AdvanceBoundary));
            Equal(CommandStatus.RecoveryRequired, session.Execute(request).Status); Equal(CommandStatus.RecoveryRequired, session.Execute(request).Status);
            Equal(LoadStatus.Valid, session.Recover().Status); Equal(CommandStatus.AlreadyCommitted, session.Execute(request).Status); Equal(initial.Revision + 1, session.Snapshot().Revision);
        });
        Check("corrupt primary recovers prior generation and preserves backup", () =>
        {
            var f = new Fixture(); f.Create(); var path = PathFor("backup"); File.WriteAllBytes(path, f.Session.ExportCheckpoint()); var original = f.Session.ExportCheckpoint();
            var store = new AtomicFileSaveStore(path, f.Serializer); var session = new GameSession(f.State(), f.Content, f.Serializer, store);
            Equal(CommandStatus.Committed, session.Execute(new CommandEnvelope("run", "job", f.State().Revision, new GameCommand(CommandKind.AcceptJob, "developer"))).Status);
            Bytes(original, File.ReadAllBytes(path + ".backup")); File.WriteAllText(path, "damaged");
            Equal(LoadStatus.RecoveredBackup, store.Read().Status); session.Recover();
            Equal(CommandStatus.Committed, session.Execute(new CommandEnvelope("run", "job-again", session.Snapshot().Revision, new GameCommand(CommandKind.AcceptJob, "developer"))).Status);
            Bytes(original, File.ReadAllBytes(path + ".backup")); True(Directory.GetFiles(scratch, "backup.json.corrupt-*").Length == 1);
        });
        Check("malformed save checksum, duplicate identity and invalid RNG rejected", () =>
        {
            var f = new Fixture(); f.Create(); Equal(LoadStatus.Corrupt, f.Serializer.DeserializeAndValidate(Encoding.UTF8.GetBytes("{}" )).Status);
            var state = f.State(); state.SchedulerRng = 0;
            Equal(LoadStatus.Corrupt, f.Serializer.DeserializeAndValidate(JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(state), SaveSchema.CurrentVersion, state.Revision)).Status);
            state = f.State(); state.Grants.Add("duplicate"); state.Grants.Add("duplicate");
            Equal(LoadStatus.Corrupt, f.Serializer.DeserializeAndValidate(JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(state), SaveSchema.CurrentVersion, state.Revision)).Status);
        });
        Check("transient read lock does not roll back to backup", () =>
        {
            var f = new Fixture(); f.Create(); var path = PathFor("locked"); File.WriteAllBytes(path, f.Session.ExportCheckpoint());
            var store = new AtomicFileSaveStore(path, f.Serializer); var session = new GameSession(f.State(), f.Content, f.Serializer, store);
            Equal(CommandStatus.Committed, session.Execute(new CommandEnvelope("run", "locked-job", session.Snapshot().Revision, new GameCommand(CommandKind.AcceptJob, "developer"))).Status);
            var before = session.ExportCheckpoint();
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            { Equal(LoadStatus.Unreadable, store.Read().Status); Equal(LoadStatus.Unreadable, session.Recover().Status); Bytes(before, session.ExportCheckpoint()); }
            Equal(LoadStatus.Valid, store.Read().Status);
        });
        Check("competing writer cannot delete active writer temp", () =>
        {
            var f = new Fixture(); f.Create(); f.Accept(); var initial = f.State(); var path = PathFor("competing"); File.WriteAllBytes(path, f.Session.ExportCheckpoint());
            AtomicFileSaveStore? second = null;
            var first = new AtomicFileSaveStore(path, f.Serializer, phase =>
            {
                if (phase != "before-replace") return false;
                var temp = Directory.GetFiles(scratch, "competing.json.tmp-*").Single();
                Equal(WriteStatus.Failed, second!.Commit(File.ReadAllBytes(temp), initial.Revision)); True(File.Exists(temp)); return false;
            });
            second = new AtomicFileSaveStore(path, f.Serializer);
            var session = new GameSession(initial, f.Content, f.Serializer, first);
            Equal(CommandStatus.Committed, session.Execute(new CommandEnvelope("run", "competing-command", initial.Revision, new GameCommand(CommandKind.AdvanceBoundary))).Status);
            Equal(0, Directory.GetFiles(scratch, "competing.json.tmp-*").Length);
        });
        Check("invalid restored work cursor and reward coherence rejected", () =>
        {
            var f = new Fixture(); f.Create(); f.Accept(); var state = f.State(); state.Minute = 600;
            Equal(LoadStatus.Corrupt, f.Serializer.DeserializeAndValidate(JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(state), SaveSchema.CurrentVersion, state.Revision)).Status);
            state.Minute = 660; state.Employment!.ScenesToday = 0;
            Equal(LoadStatus.Corrupt, f.Serializer.DeserializeAndValidate(JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(state), SaveSchema.CurrentVersion, state.Revision)).Status);
        });
        Check("snapshot collections cannot mutate authoritative state", () =>
        {
            var f = new Fixture(); f.Create(); var before = f.Session.ExportCheckpoint(); var snapshot = f.Session.Snapshot();
            True(snapshot.History is System.Collections.ObjectModel.ReadOnlyCollection<string>);
            True(snapshot.SkillLevels is System.Collections.ObjectModel.ReadOnlyDictionary<string, int>);
            Bytes(before, f.Session.ExportCheckpoint());
        });
        Check("ledger cash reconciliation over multi-month employed study scenario", () =>
        {
            var f = new Fixture(); f.Create(); f.Accept(); f.Buy();
            for (var i = 0; i < 3; i++) f.Session.AdvanceMonth(new CommandEnvelope("run", "balance-" + i, f.Session.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary)));
            Equal(checked(3000000L + f.State().Ledger.Sum(x => x.CashDelta)), f.State().Cash); True(f.State().Cash >= 0);
        });
        var reportPath = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(scratch, "report.json");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(new { suite = "Provisional first playable — .NET, not Unity", runtime = Environment.Version.ToString(), passed, failed = results.Count - passed, tests = results }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"{passed}/{results.Count} passed. Report: {reportPath}");
        Console.WriteLine($"Temporary fixtures retained for diagnosis: {scratch}");
        return passed == results.Count ? 0 : 1;
    }
    private static AtomicFileSaveStore FileStore(Fixture f, string name) => new(PathFor(name), f.Serializer);
    private static string PathFor(string name) => Path.Combine(scratch, name + ".json");
    private static void Check(string name, Action action)
    {
        try { action(); passed++; results.Add(new { name, status = "passed" }); Console.WriteLine("PASS " + name); }
        catch (Exception error) { results.Add(new { name, status = "failed", error = error.ToString() }); Console.WriteLine("FAIL " + name + ": " + error.Message); }
    }
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}; got {actual}"); }
    private static void True(bool value) { if (!value) throw new Exception("Assertion failed"); }
    private static void Bytes(byte[] expected, byte[] actual) => True(expected.SequenceEqual(actual));
    private static void Throws(Action action) { try { action(); } catch (ArgumentException) { return; } throw new Exception("Expected rejection"); }
    private static void ThrowsNotSupported(Action action) { try { action(); } catch (NotSupportedException) { return; } throw new Exception("Expected immutable collection"); }
    private static void SameOutcome(SimulationOutcome expected, SimulationOutcome actual)
    {
        Equal(expected.OperationId, actual.OperationId); Equal(expected.CommandId, actual.CommandId); Equal(expected.ActivityId, actual.ActivityId);
        Equal(expected.PriorRevision, actual.PriorRevision); Equal(expected.Revision, actual.Revision); Equal(expected.Start, actual.Start); Equal(expected.End, actual.End);
        Equal(expected.MinutesConsumed, actual.MinutesConsumed); Equal(expected.Cue, actual.Cue); Equal(expected.PlaybackCursor, actual.PlaybackCursor);
        Equal(expected.CashDelta, actual.CashDelta); Equal(expected.EmploymentId, actual.EmploymentId); Equal(expected.CareerXpDelta, actual.CareerXpDelta);
        Equal(expected.PriorRank, actual.PriorRank); Equal(expected.NewRank, actual.NewRank); SameCourseChange(expected.CourseChange, actual.CourseChange);
        True(expected.LedgerEntries.Select(x => (x.Id, x.Category, x.CashDelta, x.Amount, x.AttributionId))
            .SequenceEqual(actual.LedgerEntries.Select(x => (x.Id, x.Category, x.CashDelta, x.Amount, x.AttributionId))));
        True(expected.SkillDeltas.Select(x => (x.SkillId, x.ExposureDelta, x.PriorLevel, x.NewLevel, x.PriorGrantedLevel, x.NewGrantedLevel))
            .SequenceEqual(actual.SkillDeltas.Select(x => (x.SkillId, x.ExposureDelta, x.PriorLevel, x.NewLevel, x.PriorGrantedLevel, x.NewGrantedLevel))));
        True(expected.GrantedIds.SequenceEqual(actual.GrantedIds)); True(expected.HistoryEntries.SequenceEqual(actual.HistoryEntries));
    }
    private static void SameCourseChange(CourseChange? expected, CourseChange? actual)
    {
        if (expected == null || actual == null) { True(expected == actual); return; }
        Equal(expected.Kind, actual.Kind); Equal(expected.CompletionReason, actual.CompletionReason);
        Equal(expected.InstanceId, actual.InstanceId); Equal(expected.DefinitionId, actual.DefinitionId); Equal(expected.SkillId, actual.SkillId);
        Equal(expected.PriorProgressUnits, actual.PriorProgressUnits); Equal(expected.NewProgressUnits, actual.NewProgressUnits);
        Equal(expected.TargetUnits, actual.TargetUnits); Equal(expected.TargetLevel, actual.TargetLevel);
    }
    private static SkillDefinition Skill(string id) => new(id, "skill." + id, 100, 300, 600, 1000, 1500);
    private static CareerSceneDefinition Scene(string id, int weight, long xp = 10) => new(id, "scene." + id, weight, xp, "communication", 1);
    private static CareerDefinition Career(IEnumerable<CareerSceneDefinition> scenes, long promotionXp = 1000, int promotionDays = 30, DayOfWeek[]? days = null) =>
        new("developer", "v1", "career.developer", 540, 1020, 4, 20,
            days ?? new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, scenes,
            new[] { new CareerRankDefinition("junior", 0, 0, 10000000, "problem-solving", 0), new CareerRankDefinition("mid", promotionXp, promotionDays, 15000000, "problem-solving", 2) });
    private static ContentCatalog Catalog(long cash = 3000000, int speed = 10000, long promotionXp = 1000, int promotionDays = 30, int payday = 1, long cost = 1000000, long sceneXp = 10) =>
        new("fixture.v1", skillIds.Select(Skill),
            new[] { Career(new[] { Scene("coding", 40, sceneXp), Scene("meeting", 20, sceneXp), Scene("bug-fixing", 15, sceneXp), Scene("client-discussion", 10, sceneXp), Scene("demo", 10, sceneXp), Scene("documentation", 5, sceneXp) }, promotionXp, promotionDays) },
            new[] { new CourseDefinition("communication-basics", "course.communication", "communication", 1, 0, 360, 200000), new CourseDefinition("problem-course", "course.problem", "problem-solving", 2, 0, 360, 200000) },
            new[] { new CharacterStartDefinition("fresh", cash, speed, "base.female", "base.male") }, new EconomyBalanceDefinition(payday, 1, cost), new DayScheduleDefinition(480, 1320));
    private sealed class Fixture
    {
        public ContentCatalog Content { get; }
        public JsonSaveSerializer Serializer { get; }
        public GameSession Session { get; }
        private int sequence;
        public Fixture(ContentCatalog? content = null, GameState? state = null, SimDate? start = null)
        {
            Content = content ?? Catalog(); Serializer = new JsonSaveSerializer(Content, GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
            var initial = state ?? GameSession.NewState(Content, "run", 12345, start ?? new SimDate(2026, 9, 1));
            sequence = checked((int)initial.NextOperation);
            var store = new MemoryStore(Serializer, state != null ? Serializer.Serialize(initial) : null);
            Session = new GameSession(initial, Content, Serializer, store);
        }
        public GameState State() => Serializer.DeserializeAndValidate(Session.ExportCheckpoint()).State!;
        public CommandResult Send(GameCommand command, string? id = null) => Session.Execute(new CommandEnvelope("run", id ?? "cmd-" + (++sequence), Session.Snapshot().Revision, command));
        public CommandResult SendOk(GameCommand command, string? id = null) { var result = Send(command, id); Equal(CommandStatus.Committed, result.Status); return result; }
        public void Create(int age = 25) => SendOk(new GameCommand(CommandKind.CreateCharacter, "fresh", age, "Nguyễn Ánh", "base.female"));
        public void Accept() => SendOk(new GameCommand(CommandKind.AcceptJob, "developer"));
        public CommandResult Buy(string? id = null) => SendOk(new GameCommand(CommandKind.PurchaseCourse, "communication-basics"), id);
        public void Evening() { while (Session.Snapshot().Instant.Minute < 1020) SendOk(new GameCommand(CommandKind.AdvanceBoundary)); }
        public void Day() { var result = Session.AdvanceDay(new CommandEnvelope("run", "day-" + (++sequence), Session.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary))); Equal("TargetReached", result.StopReason); }
    }
    private sealed class NthSerializeFailureSerializer : ISaveSerializer
    {
        private readonly ISaveSerializer inner;
        private int failOn;
        private int calls;
        public NthSerializeFailureSerializer(ISaveSerializer innerSerializer) { inner = innerSerializer; }
        public void Arm(int serializeCall) { failOn = serializeCall; calls = 0; }
        public byte[] Serialize(GameState state)
        {
            calls++;
            if (failOn > 0 && calls == failOn) throw new ArgumentException("Injected candidate serialization validation failure.");
            return inner.Serialize(state);
        }
        public LoadResult DeserializeAndValidate(byte[] bytes) => inner.DeserializeAndValidate(bytes);
    }
    private sealed class MemoryStore : ISaveStore
    {
        private byte[]? bytes;
        private readonly ISaveSerializer serializer;
        public MemoryStore(ISaveSerializer serializer, byte[]? initial) { this.serializer = serializer; bytes = initial; }
        public LoadResult Read() => bytes == null ? new LoadResult(LoadStatus.Missing) : serializer.DeserializeAndValidate(bytes);
        public WriteStatus Commit(byte[] candidate, long expectedRevision)
        {
            var previous = Read(); var next = serializer.DeserializeAndValidate(candidate);
            if (next.Status != LoadStatus.Valid || next.State!.Revision != expectedRevision + 1 || (previous.State?.Revision ?? 0) != expectedRevision) return WriteStatus.Failed;
            bytes = (byte[])candidate.Clone(); return WriteStatus.Committed;
        }
    }
}
