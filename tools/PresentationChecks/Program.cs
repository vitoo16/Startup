#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using StartupLife.Core;
using StartupLife.Presentation;

internal static class Program
{
    private static int passed;
    private static readonly List<string> failures = new List<string>();

    private static int Main(string[] args)
    {
        Check("initial snapshot selects character creation and copies read model", InitialProjection);
        Check("create character dispatches current run revision and injected command id", CreateDispatch);
        Check("life actions dispatch frozen commands from latest snapshot revision", ActionDispatch);
        Check("playback acknowledgement binds current activity", PlaybackDispatch);
        Check("day and month advancement use boundary intent and project reached state", AdvanceDispatch);
        Check("active course read model flows through unchanged", ActiveCourseProjection);
        Check("empty command id source fails before dispatch", EmptyCommandIdRejected);

        var report = new { suite = "Startup Life M7 presentation adapter — .NET, not Unity", passed, failed = failures.Count, failures };
        if (args.Length > 0)
        {
            var path = Path.GetFullPath(args[0]);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        }

        foreach (var failure in failures) Console.Error.WriteLine(failure);
        Console.WriteLine($"{passed}/{passed + failures.Count} passed." + (args.Length > 0 ? " Report: " + Path.GetFullPath(args[0]) : ""));
        return failures.Count == 0 ? 0 : 1;
    }

    private static void InitialProjection()
    {
        var gateway = new FakeGateway(Snapshot(name: "", revision: 3, history: new[] { "one" }));
        var flow = new FirstPlayableFlow(gateway, new SequenceIds());
        var state = flow.Refresh();
        Equal(FirstPlayableScreen.CharacterCreation, state.Screen);
        Equal(3L, state.Revision);
        Equal("run-a", gateway.Current.RunId);
        Equal(1, state.History.Count);
        Equal("one", state.History[0]);
        True(!state.HasCareer); True(!state.CanStudy); True(!state.HasPendingPlayback);
        ThrowsNotSupported(() => ((IList<string>)state.History).Clear());
        ThrowsNotSupported(() => ((IDictionary<string, int>)state.SkillLevels).Add("forged", 5));
    }

    private static void CreateDispatch()
    {
        var gateway = new FakeGateway(Snapshot(name: "", revision: 7));
        gateway.Next = Snapshot(name: "An", revision: 8);
        var flow = new FirstPlayableFlow(gateway, new SequenceIds());
        var result = flow.CreateCharacter("An", 25, "fresh", "base.female");
        Equal("run-a", gateway.LastEnvelope!.RunId);
        Equal("create-character/1", gateway.LastEnvelope.CommandId);
        Equal(7L, gateway.LastEnvelope.ExpectedRevision);
        Equal(CommandKind.CreateCharacter, gateway.LastEnvelope.Command.Kind);
        Equal("fresh", gateway.LastEnvelope.Command.ContentId);
        Equal("base.female", gateway.LastEnvelope.Command.AppearanceId);
        Equal("An", gateway.LastEnvelope.Command.Name);
        Equal(25, gateway.LastEnvelope.Command.Amount);
        Equal(FirstPlayableScreen.Life, result.State.Screen);
        Equal("An", result.State.Name);
        Equal(8L, result.State.Revision);
    }

    private static void ActionDispatch()
    {
        var gateway = new FakeGateway(Snapshot(name: "An", revision: 10));
        var ids = new SequenceIds();
        var flow = new FirstPlayableFlow(gateway, ids);

        gateway.Next = Snapshot(name: "An", revision: 11, careerId: "developer");
        flow.AcceptJob("developer");
        AssertLast(gateway, "accept-job/1", 10, CommandKind.AcceptJob, "developer", 0);

        gateway.Next = Snapshot(name: "An", revision: 12, careerId: "developer", activeCourse: true);
        flow.PurchaseCourse("communication-basics");
        AssertLast(gateway, "purchase-course/2", 11, CommandKind.PurchaseCourse, "communication-basics", 0);

        gateway.Next = Snapshot(name: "An", revision: 13, careerId: "developer", activeCourse: true, courseProgress: 600000);
        var study = flow.Study(60);
        AssertLast(gateway, "study/3", 12, CommandKind.Study, "", 60);
        Equal(600000L, study.State.ActiveCourse!.ProgressUnits);

        gateway.Next = Snapshot(name: "An", revision: 14, careerId: "");
        flow.Resign();
        AssertLast(gateway, "resign/4", 13, CommandKind.Resign, "", 0);

        gateway.Next = Snapshot(name: "An", revision: 15);
        flow.AdvanceBoundary();
        AssertLast(gateway, "advance-boundary/5", 14, CommandKind.AdvanceBoundary, "", 0);
    }

    private static void PlaybackDispatch()
    {
        var gateway = new FakeGateway(Snapshot(name: "An", revision: 20, activity: "run-a/activity/2026-09-01/540", playback: 0));
        gateway.Next = Snapshot(name: "An", revision: 21, activity: "run-a/activity/2026-09-01/540", playback: 10);
        var flow = new FirstPlayableFlow(gateway, new SequenceIds());
        var result = flow.AcknowledgePlayback(10);
        AssertLast(gateway, "acknowledge-playback/1", 20, CommandKind.AcknowledgePlayback, "run-a/activity/2026-09-01/540", 10);
        Equal(10, result.State.PlaybackCursor);
        True(result.State.HasPendingPlayback);
    }

    private static void AdvanceDispatch()
    {
        var gateway = new FakeGateway(Snapshot(name: "An", revision: 30, minute: 600));
        var flow = new FirstPlayableFlow(gateway, new SequenceIds());

        gateway.Next = Snapshot(name: "An", revision: 35, minute: 1320);
        gateway.NextAdvance = new AdvanceResult(Array.Empty<CommandResult>(), new SimInstant(new SimDate(2026, 9, 1), 1320), "TargetReached");
        var day = flow.AdvanceDay();
        Equal("AdvanceDay", gateway.LastDispatch);
        AssertLast(gateway, "advance-day/1", 30, CommandKind.AdvanceBoundary, "", 0);
        Equal(1320, day.State.Instant.Minute);
        Equal(1320, day.Advance.ReachedInstant.Minute);

        gateway.Next = Snapshot(name: "An", revision: 50, date: "2026-10-01", minute: 480);
        gateway.NextAdvance = new AdvanceResult(Array.Empty<CommandResult>(), new SimInstant(new SimDate(2026, 10, 1), 480), "TargetReached");
        var month = flow.AdvanceMonth();
        Equal("AdvanceMonth", gateway.LastDispatch);
        AssertLast(gateway, "advance-month/2", 35, CommandKind.AdvanceBoundary, "", 0);
        Equal(10, month.State.Instant.Date.Month);
    }

    private static void ActiveCourseProjection()
    {
        var gateway = new FakeGateway(Snapshot(name: "An", revision: 5, activeCourse: true, courseProgress: 1200000));
        var state = new FirstPlayableFlow(gateway, new SequenceIds()).Refresh();
        True(state.CanStudy); True(state.ActiveCourse != null);
        Equal("course/1", state.ActiveCourse!.InstanceId);
        Equal("communication-basics", state.ActiveCourse.DefinitionId);
        Equal("communication", state.ActiveCourse.SkillId);
        Equal(1200000L, state.ActiveCourse.ProgressUnits);
        Equal(3600000L, state.ActiveCourse.TargetUnits);
        Equal(1, state.ActiveCourse.TargetLevel);
    }

    private static void EmptyCommandIdRejected()
    {
        var gateway = new FakeGateway(Snapshot(name: "An", revision: 1));
        var flow = new FirstPlayableFlow(gateway, new EmptyIds());
        ThrowsInvalidOperation(() => flow.AdvanceBoundary());
        True(gateway.LastEnvelope == null);
    }

    private static void AssertLast(FakeGateway gateway, string id, long revision, CommandKind kind, string contentId, int amount)
    {
        True(gateway.LastEnvelope != null);
        Equal("run-a", gateway.LastEnvelope!.RunId);
        Equal(id, gateway.LastEnvelope.CommandId);
        Equal(revision, gateway.LastEnvelope.ExpectedRevision);
        Equal(kind, gateway.LastEnvelope.Command.Kind);
        Equal(contentId, gateway.LastEnvelope.Command.ContentId);
        Equal(amount, gateway.LastEnvelope.Command.Amount);
    }

    private static GameSnapshot Snapshot(string name, long revision, string date = "2026-09-01", int minute = 480,
        string careerId = "", bool activeCourse = false, long courseProgress = 0, string activity = "", int playback = 0,
        string[]? history = null)
    {
        var state = new GameState
        {
            ContentVersion = "fixture.v1",
            RunId = "run-a",
            Revision = revision,
            DateIso = date,
            Minute = minute,
            Name = name,
            StartingAge = name.Length == 0 ? 0 : 25,
            BirthDateIso = name.Length == 0 ? "" : "2001-09-01",
            BackgroundId = name.Length == 0 ? "" : "fresh",
            AppearanceId = name.Length == 0 ? "" : "base.female",
            LearningSpeed = name.Length == 0 ? 0 : 10000,
            Cash = name.Length == 0 ? 0 : 1000000,
            CurrentActivity = activity,
            CurrentCue = activity.Length == 0 ? "" : "scene.coding",
            PlaybackCursor = playback,
            History = history?.ToList() ?? new List<string>(),
            Skills = name.Length == 0 ? new List<SkillState>() : new List<SkillState> { new SkillState { Id = "communication" } }
        };
        if (careerId.Length != 0)
            state.Employment = new EmploymentState { InstanceId = "employment/1", CareerId = careerId, Xp = 40, Rank = 1 };
        if (activeCourse)
            state.Course = new CourseState { InstanceId = "course/1", DefinitionId = "communication-basics", ProgressUnits = courseProgress };

        return new GameSnapshot(state, Catalog());
    }

    private static ContentCatalog Catalog()
    {
        var skill = new SkillDefinition("communication", "skill.communication", 100, 300, 600, 1000, 1500);
        var course = new CourseDefinition("communication-basics", "course.communication", "communication", 1, 0, 360, 200000);
        return new ContentCatalog("fixture.v1", new[] { skill }, Array.Empty<CareerDefinition>(), new[] { course },
            new[] { new CharacterStartDefinition("fresh", 1000000, 10000, "base.female") },
            new EconomyBalanceDefinition(5, 1, 500000), new DayScheduleDefinition(480, 1320));
    }

    private sealed class SequenceIds : ICommandIdSource
    {
        private int value;
        public string Next(string action) { value++; return action + "/" + value; }
    }

    private sealed class EmptyIds : ICommandIdSource
    {
        public string Next(string action) => "";
    }

    private sealed class FakeGateway : IGameCommands
    {
        public GameSnapshot Current { get; private set; }
        public GameSnapshot? Next { private get; set; }
        public AdvanceResult? NextAdvance { private get; set; }
        public CommandEnvelope? LastEnvelope { get; private set; }
        public string LastDispatch { get; private set; } = "";

        public FakeGateway(GameSnapshot current) { Current = current; }

        public CommandResult Execute(CommandEnvelope command)
        {
            LastDispatch = "Execute"; LastEnvelope = command;
            if (Next != null) { Current = Next; Next = null; }
            return new CommandResult(CommandStatus.Committed, "", Current.Revision, "op");
        }

        public AdvanceResult AdvanceDay(CommandEnvelope request)
        {
            LastDispatch = "AdvanceDay"; LastEnvelope = request;
            if (Next != null) { Current = Next; Next = null; }
            var result = NextAdvance ?? new AdvanceResult(Array.Empty<CommandResult>(), Current.Instant, "TargetReached");
            NextAdvance = null; return result;
        }

        public AdvanceResult AdvanceMonth(CommandEnvelope request)
        {
            LastDispatch = "AdvanceMonth"; LastEnvelope = request;
            if (Next != null) { Current = Next; Next = null; }
            var result = NextAdvance ?? new AdvanceResult(Array.Empty<CommandResult>(), Current.Instant, "TargetReached");
            NextAdvance = null; return result;
        }

        public GameSnapshot Snapshot() => Current;
    }

    private static void Check(string name, Action action)
    {
        try { action(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failures.Add(name + ": " + error.Message); Console.WriteLine("FAIL " + name); }
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception("Expected " + expected + ", got " + actual);
    }

    private static void True(bool value)
    {
        if (!value) throw new Exception("Expected true.");
    }

    private static void ThrowsNotSupported(Action action)
    {
        try { action(); } catch (NotSupportedException) { return; }
        throw new Exception("Expected NotSupportedException.");
    }

    private static void ThrowsInvalidOperation(Action action)
    {
        try { action(); } catch (InvalidOperationException) { return; }
        throw new Exception("Expected InvalidOperationException.");
    }
}
