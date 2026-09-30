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
        Check("H1 direct command and advance batch cannot share caller CommandId", DirectAndBatchIdentityConflict);
        Check("H1 completed batch retry replays persisted receipts after restore", BatchRetryReplaysPersistedReceiptsAfterRestore);
        Check("H2 unsupported career revision does not fall back to backup or overwrite primary", UnsupportedCareerRevisionIsPreserved);

        var reportPath = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(Path.GetTempPath(), "startup-life-astra-foundation-highs.json");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(new
        {
            suite = "Astra foundation HIGH regression — .NET, not Unity",
            passed,
            failed = Results.Count - passed,
            tests = Results
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"{passed}/{Results.Count} passed. Report: {reportPath}");
        return passed == Results.Count ? 0 : 1;
    }

    private static void DirectAndBatchIdentityConflict()
    {
        var f = new Fixture();
        var create = f.Execute("shared", new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female"));
        Equal(CommandStatus.Committed, create.Status);
        var before = f.Session.ExportCheckpoint();
        var batch = f.Session.AdvanceDay(new CommandEnvelope("run", "shared", f.Session.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary)));
        Equal("InvalidRequest", batch.StopReason);
        Equal(0, batch.Boundaries.Count);
        Bytes(before, f.Session.ExportCheckpoint());

        var g = new Fixture();
        Equal(CommandStatus.Committed, g.Execute("create", new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female")).Status);
        var request = new CommandEnvelope("run", "advance-shared", g.Session.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary));
        var first = g.Session.AdvanceDay(request);
        Equal("TargetReached", first.StopReason);
        True(first.Boundaries.Count > 0);
        var checkpoint = g.Session.ExportCheckpoint();
        var conflict = g.Session.Execute(new CommandEnvelope("run", "advance-shared", g.Session.Snapshot().Revision, new GameCommand(CommandKind.AcceptJob, "developer")));
        Equal(CommandStatus.Rejected, conflict.Status);
        Equal("command.id_conflict", conflict.ReasonKey);
        Bytes(checkpoint, g.Session.ExportCheckpoint());
    }

    private static void BatchRetryReplaysPersistedReceiptsAfterRestore()
    {
        var f = new Fixture();
        Equal(CommandStatus.Committed, f.Execute("create", new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female")).Status);
        var request = new CommandEnvelope("run", "batch-retry", f.Session.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary));
        var first = f.Session.AdvanceDay(request);
        Equal("TargetReached", first.StopReason);
        True(first.Boundaries.Count > 0);
        var checkpoint = f.Session.ExportCheckpoint();
        var firstOperations = first.Boundaries.Select(x => x.OperationId).ToArray();

        var restored = new Fixture(f.Catalog, f.Serializer.DeserializeAndValidate(checkpoint).State!, checkpoint);
        var retry = restored.Session.AdvanceDay(request);
        Equal("TargetReached", retry.StopReason);
        Equal(first.Boundaries.Count, retry.Boundaries.Count);
        True(retry.Boundaries.All(x => x.Status == CommandStatus.AlreadyCommitted));
        True(firstOperations.SequenceEqual(retry.Boundaries.Select(x => x.OperationId)));
        Bytes(checkpoint, restored.Session.ExportCheckpoint());
    }

    private static void UnsupportedCareerRevisionIsPreserved()
    {
        var oldCatalog = Catalog("v1");
        var oldSerializer = new JsonSaveSerializer(oldCatalog);
        var initial = GameSession.NewState(oldCatalog, "run", 12345, new SimDate(2026, 9, 7));
        var memory = new MemoryStore(oldSerializer, null);
        var session = new GameSession(initial, oldCatalog, oldSerializer, memory);
        Equal(CommandStatus.Committed, session.Execute(new CommandEnvelope("run", "create", 0, new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female"))).Status);
        Equal(CommandStatus.Committed, session.Execute(new CommandEnvelope("run", "job", session.Snapshot().Revision, new GameCommand(CommandKind.AcceptJob, "developer"))).Status);
        var unsupportedPrimary = session.ExportCheckpoint();

        var newCatalog = Catalog("v2");
        var newSerializer = new JsonSaveSerializer(newCatalog);
        var direct = newSerializer.DeserializeAndValidate(unsupportedPrimary);
        Equal(LoadStatus.UnsupportedContent, direct.Status);
        Equal("save.content_revision", direct.Reason);

        var backupState = GameSession.NewState(newCatalog, "run", 12345, new SimDate(2026, 9, 7));
        var backupStore = new MemoryStore(newSerializer, null);
        var backupSession = new GameSession(backupState, newCatalog, newSerializer, backupStore);
        Equal(CommandStatus.Committed, backupSession.Execute(new CommandEnvelope("run", "create-backup", 0, new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female"))).Status);
        var validBackup = backupSession.ExportCheckpoint();

        var directory = Path.Combine(Path.GetTempPath(), "StartupLifeAstraChecks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "save.json");
        File.WriteAllBytes(path, unsupportedPrimary);
        File.WriteAllBytes(path + ".backup", validBackup);
        var store = new AtomicFileSaveStore(path, newSerializer);

        var read = store.Read();
        Equal(LoadStatus.UnsupportedContent, read.Status);
        Equal("save.content_revision", read.Reason);
        Bytes(unsupportedPrimary, File.ReadAllBytes(path));
        Equal(WriteStatus.Failed, store.Commit(validBackup, 0));
        Bytes(unsupportedPrimary, File.ReadAllBytes(path));
    }

    private static ContentCatalog Catalog(string revision = "v1")
    {
        var skill = new SkillDefinition("communication", "skill.communication", 100, 300, 600, 1000, 1500);
        var scene = new CareerSceneDefinition("coding", "scene.coding", 100, 10, "communication", 1);
        var rank = new CareerRankDefinition("junior", 0, 0, 10000000, "communication", 0);
        var career = new CareerDefinition("developer", revision, "career.developer", 540, 1020, 4, 20,
            new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            new[] { scene }, new[] { rank });
        return new ContentCatalog("fixture.v1", new[] { skill }, new[] { career }, Array.Empty<CourseDefinition>(),
            new[] { new CharacterStartDefinition("fresh", 3000000, 10000, "base.female") },
            new EconomyBalanceDefinition(1, 1, 0), new DayScheduleDefinition(480, 1320));
    }

    private sealed class Fixture
    {
        public ContentCatalog Catalog { get; }
        public JsonSaveSerializer Serializer { get; }
        public GameSession Session { get; }
        public Fixture(ContentCatalog? catalog = null, GameState? state = null, byte[]? persisted = null)
        {
            Catalog = catalog ?? Program.Catalog();
            Serializer = new JsonSaveSerializer(Catalog);
            var initial = state ?? GameSession.NewState(Catalog, "run", 12345, new SimDate(2026, 9, 7));
            Session = new GameSession(initial, Catalog, Serializer, new MemoryStore(Serializer, persisted));
        }
        public CommandResult Execute(string id, GameCommand command) => Session.Execute(new CommandEnvelope("run", id, Session.Snapshot().Revision, command));
    }

    private sealed class MemoryStore : ISaveStore
    {
        private readonly ISaveSerializer serializer;
        private byte[]? bytes;
        public MemoryStore(ISaveSerializer serializer, byte[]? initial) { this.serializer = serializer; bytes = initial == null ? null : (byte[])initial.Clone(); }
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
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}; got {actual}"); }
    private static void True(bool value) { if (!value) throw new Exception("Assertion failed"); }
    private static void Bytes(byte[] expected, byte[] actual) => True(expected.SequenceEqual(actual));
}
