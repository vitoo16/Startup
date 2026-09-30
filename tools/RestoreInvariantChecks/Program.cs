using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using StartupLife.Application;
using StartupLife.Core;
using StartupLife.Infrastructure;

internal static class Program
{
    private static readonly List<object> Results = new();
    private static int passed;

    private static int Main(string[] args)
    {
        Check("M2 operation and entity counters cannot reuse issued identities", CountersCannotReuseIssuedIds);
        Check("M2 scheduler cursor cannot rewind committed work", SchedulerCursorCannotRewind);
        Check("M2 scheduler cycle follows committed career activities", SchedulerCycleFollowsCommittedActivities);
        Check("M2 promoted rank transition remains valid until next scheduler consume", PromotionTransitionRemainsValid);
        Check("M2 committed activity history cannot be removed or forged", ActivityHistoryMustBeCommitted);
        Check("M2 resigned employment keeps its persisted scheduler valid", ResignedSchedulerRemainsValid);
        Check("M2 valid mid-cycle restore continues byte-identically", ValidRestoreContinuationIsDeterministic);

        var reportPath = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(Path.GetTempPath(), "startup-life-m2-restore-invariants.json");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(new
        {
            suite = "Astra M2 restore invariants — .NET, not Unity",
            passed,
            failed = Results.Count - passed,
            tests = Results
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"{passed}/{Results.Count} passed. Report: {reportPath}");
        return passed == Results.Count ? 0 : 1;
    }

    private static void CountersCannotReuseIssuedIds()
    {
        var f = Ready();
        var valid = f.State();
        Equal(LoadStatus.Valid, Raw(f.Serializer, valid).Status);

        var operation = f.State();
        operation.NextOperation = 2;
        Equal(LoadStatus.Corrupt, Raw(f.Serializer, operation).Status);

        var entity = f.State();
        entity.NextEntity = 3;
        Equal(LoadStatus.Corrupt, Raw(f.Serializer, entity).Status);

        var malformedOperation = f.State();
        malformedOperation.Receipts.Last().OperationId = "run/op/02";
        Equal(LoadStatus.Corrupt, Raw(f.Serializer, malformedOperation).Status);
    }

    private static void SchedulerCursorCannotRewind()
    {
        var f = Ready();
        f.ToEndOfWork();
        var valid = f.State();
        Equal(4, valid.Scheduler.Cursor);
        Equal(4, valid.Employment!.ScenesToday);
        Equal(LoadStatus.Valid, Raw(f.Serializer, valid).Status);

        var zero = f.State();
        zero.Scheduler.Cursor = 0;
        Equal(LoadStatus.Corrupt, Raw(f.Serializer, zero).Status);

        var plausiblePrefix = f.State();
        plausiblePrefix.Scheduler.Cursor = 2;
        plausiblePrefix.Scheduler.LastScene = plausiblePrefix.Scheduler.Deck[1];
        Equal(LoadStatus.Corrupt, Raw(f.Serializer, plausiblePrefix).Status);
    }

    private static void SchedulerCycleFollowsCommittedActivities()
    {
        var f = Ready();
        f.ToEndOfWork();
        f.ToNextWorkdayFirstScene();
        var valid = f.State();
        Equal(2L, valid.Scheduler.Cycle);
        Equal(1, valid.Scheduler.Cursor);
        Equal(LoadStatus.Valid, Raw(f.Serializer, valid).Status);

        var wrongCycle = f.State();
        wrongCycle.Scheduler.Cycle = 1;
        Equal(LoadStatus.Corrupt, Raw(f.Serializer, wrongCycle).Status);

        var wrongCursor = f.State();
        wrongCursor.Scheduler.Cursor = 2;
        wrongCursor.Scheduler.LastScene = wrongCursor.Scheduler.Deck[1];
        Equal(LoadStatus.Corrupt, Raw(f.Serializer, wrongCursor).Status);
    }

    private static void PromotionTransitionRemainsValid()
    {
        var f = Ready(Catalog(40));
        f.ToEndOfWork();
        var promoted = f.State();
        Equal(1, promoted.Employment!.Rank);
        Equal("developer/v1/0", promoted.Scheduler.Signature);
        Equal(LoadStatus.Valid, Raw(f.Serializer, promoted).Status);

        f.ToNextWorkdayFirstScene();
        var rebuilt = f.State();
        Equal(1, rebuilt.Employment!.Rank);
        Equal("developer/v1/1", rebuilt.Scheduler.Signature);
        Equal(LoadStatus.Valid, Raw(f.Serializer, rebuilt).Status);
    }

    private static void ActivityHistoryMustBeCommitted()
    {
        var f = Ready();
        f.ToEndOfWork();
        var missing = f.State();
        var workActivity = "run/activity/2026-09-07/900";
        True(missing.ConsumedActivities.Remove(workActivity));
        missing.CurrentActivity = missing.Receipts.Last().OperationId;
        Equal(LoadStatus.Corrupt, Raw(f.Serializer, missing).Status);

        var forged = f.State();
        forged.CurrentActivity = "run/activity/2026-09-07/777";
        Equal(LoadStatus.Corrupt, Raw(f.Serializer, forged).Status);

        var future = f.State();
        future.ConsumedActivities.Add("run/activity/2026-09-07/1200");
        Equal(LoadStatus.Corrupt, Raw(f.Serializer, future).Status);
    }

    private static void ResignedSchedulerRemainsValid()
    {
        var f = Ready();
        f.Step();
        f.Step();
        f.Step();
        f.Step();
        Equal(2, f.State().Employment!.ScenesToday);
        Equal(CommandStatus.Committed, f.Execute("resign", new GameCommand(CommandKind.Resign)).Status);
        var resigned = f.State();
        True(resigned.Employment == null);
        Equal(1, resigned.PreviousEmployment.Count);
        Equal(2, resigned.Scheduler.Cursor);
        Equal(LoadStatus.Valid, Raw(f.Serializer, resigned).Status);
    }

    private static void ValidRestoreContinuationIsDeterministic()
    {
        var f = Ready();
        f.Step();
        f.Step();
        f.Step();
        var checkpoint = f.Session.ExportCheckpoint();
        var state = f.Serializer.DeserializeAndValidate(checkpoint).State!;
        var a = new Fixture(f.Catalog, state, checkpoint);
        var b = new Fixture(f.Catalog, state, checkpoint);
        var revision = state.Revision;
        var command = new CommandEnvelope("run", "continue", revision, new GameCommand(CommandKind.AdvanceBoundary));
        Equal(CommandStatus.Committed, a.Session.Execute(command).Status);
        Equal(CommandStatus.Committed, b.Session.Execute(command).Status);
        Bytes(a.Session.ExportCheckpoint(), b.Session.ExportCheckpoint());
        Equal(LoadStatus.Valid, a.Serializer.DeserializeAndValidate(a.Session.ExportCheckpoint()).Status);
    }

    private static Fixture Ready(ContentCatalog? catalog = null)
    {
        var f = new Fixture(catalog ?? Catalog());
        Equal(CommandStatus.Committed, f.Execute("create", new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female")).Status);
        Equal(CommandStatus.Committed, f.Execute("job", new GameCommand(CommandKind.AcceptJob, "developer")).Status);
        return f;
    }

    private static LoadResult Raw(JsonSaveSerializer serializer, GameState state) =>
        serializer.DeserializeAndValidate(JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(state), 1, state.Revision));

    private static ContentCatalog Catalog(long promotionXp = 1000)
    {
        var skill = new SkillDefinition("communication", "skill.communication", 100, 300, 600, 1000, 1500);
        var scene = new CareerSceneDefinition("coding", "scene.coding", 100, 10, "communication", 1);
        var ranks = new[]
        {
            new CareerRankDefinition("junior", 0, 0, 10000000, "communication", 0),
            new CareerRankDefinition("mid", promotionXp, 0, 15000000, "communication", 1)
        };
        var career = new CareerDefinition("developer", "v1", "career.developer", 540, 1020, 4, 4,
            new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            new[] { scene }, ranks);
        return new ContentCatalog("fixture.v1", new[] { skill }, new[] { career }, Array.Empty<CourseDefinition>(),
            new[] { new CharacterStartDefinition("fresh", 3000000, 10000, "base.female") },
            new EconomyBalanceDefinition(1, 1, 0), new DayScheduleDefinition(480, 1320));
    }

    private sealed class Fixture
    {
        private int sequence;
        public ContentCatalog Catalog { get; }
        public JsonSaveSerializer Serializer { get; }
        public GameSession Session { get; }

        public Fixture(ContentCatalog catalog, GameState? state = null, byte[]? persisted = null)
        {
            Catalog = catalog;
            Serializer = new JsonSaveSerializer(catalog);
            var initial = state ?? GameSession.NewState(catalog, "run", 12345, new SimDate(2026, 9, 7));
            sequence = checked((int)initial.NextOperation);
            Session = new GameSession(initial, catalog, Serializer, new MemoryStore(Serializer, persisted));
        }

        public CommandResult Execute(string id, GameCommand command) =>
            Session.Execute(new CommandEnvelope("run", id, Session.Snapshot().Revision, command));

        public CommandResult Step() => Execute("step-" + (++sequence), new GameCommand(CommandKind.AdvanceBoundary));

        public GameState State() => Serializer.DeserializeAndValidate(Session.ExportCheckpoint()).State!;

        public void ToEndOfWork()
        {
            while (Session.Snapshot().Instant.Minute < 1020)
                Equal(CommandStatus.Committed, Step().Status);
        }

        public void ToNextWorkdayFirstScene()
        {
            var start = Session.Snapshot().Instant.Date;
            while (Session.Snapshot().Instant.Date == start)
                Equal(CommandStatus.Committed, Step().Status);
            while (Session.Snapshot().Instant.Minute < 660)
                Equal(CommandStatus.Committed, Step().Status);
        }
    }

    private sealed class MemoryStore : ISaveStore
    {
        private readonly ISaveSerializer serializer;
        private byte[]? bytes;
        public MemoryStore(ISaveSerializer serializer, byte[]? initial)
        {
            this.serializer = serializer;
            bytes = initial == null ? null : (byte[])initial.Clone();
        }
        public LoadResult Read() => bytes == null ? new LoadResult(LoadStatus.Missing) : serializer.DeserializeAndValidate(bytes);
        public WriteStatus Commit(byte[] candidate, long expectedRevision)
        {
            var next = serializer.DeserializeAndValidate(candidate);
            var previous = Read();
            if (next.Status != LoadStatus.Valid || next.State!.Revision != expectedRevision + 1) return WriteStatus.Failed;
            if (previous.Status != LoadStatus.Missing && previous.Status != LoadStatus.Valid && previous.Status != LoadStatus.RecoveredBackup) return WriteStatus.Failed;
            if ((previous.State?.Revision ?? 0) != expectedRevision) return WriteStatus.Failed;
            bytes = (byte[])candidate.Clone();
            return WriteStatus.Committed;
        }
    }

    private static void Check(string name, Action action)
    {
        try { action(); passed++; Results.Add(new { name, status = "passed" }); Console.WriteLine("PASS " + name); }
        catch (Exception error) { Results.Add(new { name, status = "failed", error = error.ToString() }); Console.WriteLine("FAIL " + name + ": " + error.Message); }
    }
    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}; got {actual}");
    }
    private static void True(bool value) { if (!value) throw new Exception("Assertion failed"); }
    private static void Bytes(byte[] expected, byte[] actual) => True(expected.SequenceEqual(actual));
}
