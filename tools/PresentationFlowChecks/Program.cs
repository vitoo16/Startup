#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using StartupLife.Core;
using StartupLife.Presentation;

internal static class Program
{
    private static readonly List<object> Results = new();
    private static int passed;

    private static int Main(string[] args)
    {
        Check("create maps current run and revision without state access", CreateMapsEnvelope);
        Check("career course study resign map to commands", DecisionCommandsMap);
        Check("advance boundary dispatches direct command", AdvanceBoundaryMaps);
        Check("advance day delegates to application batch API", AdvanceDayUsesBatchApi);
        Check("playback acknowledge targets current activity", PlaybackMaps);
        Check("playback cursor cannot move backwards", PlaybackCannotRewind);
        Check("empty command id factory fails before dispatch", EmptyIdRejects);

        var reportPath = args.Length > 0 ? Path.GetFullPath(args[0]) :
            Path.Combine(Path.GetTempPath(), "StartupLifePresentationChecks", "report.json");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(new
        {
            suite = "Startup Life first playable presentation command seam — .NET, not Unity",
            runtime = Environment.Version.ToString(),
            passed,
            failed = Results.Count - passed,
            tests = Results
        }, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"{passed}/{Results.Count} passed. Report: {reportPath}");
        return passed == Results.Count ? 0 : 1;
    }

    private static void CreateMapsEnvelope()
    {
        var fake = new FakeCommands(Snapshot("run-a", 7));
        var flow = new FirstPlayableFlowCoordinator(fake, () => "ui-create");

        flow.CreateCharacter("Nguyễn Ánh", 25, "fresh", "base.female");

        var envelope = Required(fake.LastExecute);
        Equal("run-a", envelope.RunId);
        Equal("ui-create", envelope.CommandId);
        Equal(7L, envelope.ExpectedRevision);
        Equal(CommandKind.CreateCharacter, envelope.Command.Kind);
        Equal("fresh", envelope.Command.ContentId);
        Equal("Nguyễn Ánh", envelope.Command.Name);
        Equal(25, envelope.Command.Amount);
        Equal("base.female", envelope.Command.AppearanceId);
    }

    private static void DecisionCommandsMap()
    {
        var ids = new Queue<string>(new[] { "job", "course", "study", "resign" });
        var fake = new FakeCommands(Snapshot("run-b", 2));
        var flow = new FirstPlayableFlowCoordinator(fake, () => ids.Dequeue());

        flow.AcceptJob("developer");
        Equal(CommandKind.AcceptJob, Required(fake.LastExecute).Command.Kind);
        Equal("developer", Required(fake.LastExecute).Command.ContentId);

        flow.PurchaseCourse("communication-basics");
        Equal(CommandKind.PurchaseCourse, Required(fake.LastExecute).Command.Kind);
        Equal("communication-basics", Required(fake.LastExecute).Command.ContentId);

        flow.Study(60);
        Equal(CommandKind.Study, Required(fake.LastExecute).Command.Kind);
        Equal(60, Required(fake.LastExecute).Command.Amount);

        flow.Resign();
        Equal(CommandKind.Resign, Required(fake.LastExecute).Command.Kind);
    }

    private static void AdvanceBoundaryMaps()
    {
        var fake = new FakeCommands(Snapshot("run-c", 3));
        var flow = new FirstPlayableFlowCoordinator(fake, () => "boundary");
        flow.AdvanceBoundary();
        Equal(CommandKind.AdvanceBoundary, Required(fake.LastExecute).Command.Kind);
        Equal(0, fake.AdvanceDayCalls);
    }

    private static void AdvanceDayUsesBatchApi()
    {
        var fake = new FakeCommands(Snapshot("run-d", 9));
        var flow = new FirstPlayableFlowCoordinator(fake, () => "day");
        flow.AdvanceDay();

        Equal(1, fake.AdvanceDayCalls);
        True(fake.LastExecute == null);
        var envelope = Required(fake.LastAdvance);
        Equal("run-d", envelope.RunId);
        Equal("day", envelope.CommandId);
        Equal(9L, envelope.ExpectedRevision);
        Equal(CommandKind.AdvanceBoundary, envelope.Command.Kind);
    }

    private static void PlaybackMaps()
    {
        var fake = new FakeCommands(Snapshot("run-e", 12, "run-e/activity/2026-09-01/540", 2));
        var flow = new FirstPlayableFlowCoordinator(fake, () => "ack");
        flow.AcknowledgePlayback(3);

        var envelope = Required(fake.LastExecute);
        Equal(CommandKind.AcknowledgePlayback, envelope.Command.Kind);
        Equal("run-e/activity/2026-09-01/540", envelope.Command.ContentId);
        Equal(3, envelope.Command.Amount);
        Equal(12L, envelope.ExpectedRevision);
    }

    private static void PlaybackCannotRewind()
    {
        var fake = new FakeCommands(Snapshot("run-f", 4, "activity", 5));
        var flow = new FirstPlayableFlowCoordinator(fake, () => "should-not-dispatch");
        Throws<ArgumentOutOfRangeException>(() => flow.AcknowledgePlayback(4));
        True(fake.LastExecute == null);
    }

    private static void EmptyIdRejects()
    {
        var fake = new FakeCommands(Snapshot("run-g", 1));
        var flow = new FirstPlayableFlowCoordinator(fake, () => "");
        Throws<InvalidOperationException>(() => flow.AcceptJob("developer"));
        True(fake.LastExecute == null);
    }

    private static GameSnapshot Snapshot(string runId, long revision, string activity = "", int playback = 0)
    {
        var skill = new SkillDefinition("communication", "skill.communication", 100, 300, 600, 1000, 1500);
        var content = new ContentCatalog(
            "fixture.v1",
            new[] { skill },
            Array.Empty<CareerDefinition>(),
            Array.Empty<CourseDefinition>(),
            new[] { new CharacterStartDefinition("fresh", 0, 10000, "base.female") },
            new EconomyBalanceDefinition(1, 1, 0),
            new DayScheduleDefinition(480, 1320));
        var state = new GameState
        {
            ContentVersion = content.Version,
            RunId = runId,
            Revision = revision,
            DateIso = "2026-09-01",
            Minute = 480,
            CurrentActivity = activity,
            PlaybackCursor = playback
        };
        return new GameSnapshot(state, content);
    }

    private sealed class FakeCommands : IGameCommands
    {
        private readonly GameSnapshot snapshot;
        public CommandEnvelope? LastExecute { get; private set; }
        public CommandEnvelope? LastAdvance { get; private set; }
        public int AdvanceDayCalls { get; private set; }

        public FakeCommands(GameSnapshot value) { snapshot = value; }

        public CommandResult Execute(CommandEnvelope command)
        {
            LastExecute = command;
            return new CommandResult(CommandStatus.Committed, "", snapshot.Revision + 1);
        }

        public AdvanceResult AdvanceDay(CommandEnvelope request)
        {
            LastAdvance = request;
            AdvanceDayCalls++;
            return new AdvanceResult(Array.Empty<CommandResult>(), snapshot.Instant, "TargetReached");
        }

        public AdvanceResult AdvanceMonth(CommandEnvelope request) => throw new NotSupportedException();

        public GameSnapshot Snapshot() => snapshot;
    }

    private static T Required<T>(T? value) where T : class =>
        value ?? throw new Exception("Expected value.");

    private static void Check(string name, Action action)
    {
        try
        {
            action();
            passed++;
            Results.Add(new { name, status = "passed" });
            Console.WriteLine("PASS " + name);
        }
        catch (Exception error)
        {
            Results.Add(new { name, status = "failed", error = error.ToString() });
            Console.WriteLine("FAIL " + name + ": " + error.Message);
        }
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Expected {expected}; got {actual}");
    }

    private static void True(bool value)
    {
        if (!value) throw new Exception("Assertion failed");
    }

    private static void Throws<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new Exception("Expected " + typeof(TException).Name);
    }
}
