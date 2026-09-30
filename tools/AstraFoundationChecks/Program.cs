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
        Check("H1 legacy reserved-prefix caller and batch identities remain disjoint", LegacyReservedPrefixCompatibility);
        Check("H2 unsupported career revision does not fall back to backup or overwrite primary", UnsupportedCareerRevisionIsPreserved);
        Check("H2 missing saved skill is unsupported content", MissingSavedSkillIsUnsupported);
        Check("H2 missing scheduled scene is unsupported while wrong-career scene is corrupt", MissingSceneClassificationIsPrecise);
        Check("H2 malformed persisted fields remain corrupt before compatibility resolution", MalformedFieldsRemainCorrupt);

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

    private static void LegacyReservedPrefixCompatibility()
    {
        const string legacyRoot = "$batch/3:abc";
        const string legacyDirectId = "$batch/3:abc/0";

        var direct = new Fixture();
        var createCommand = new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female");
        Equal(CommandStatus.Committed, direct.Execute(legacyDirectId, createCommand).Status);
        var directCheckpoint = direct.Session.ExportCheckpoint();
        var directRestored = new Fixture(direct.Catalog, direct.Serializer.DeserializeAndValidate(directCheckpoint).State!, directCheckpoint);
        var directRetry = directRestored.Session.Execute(new CommandEnvelope("run", legacyDirectId, 0, createCommand));
        Equal(CommandStatus.AlreadyCommitted, directRetry.Status);
        Bytes(directCheckpoint, directRestored.Session.ExportCheckpoint());
        var directThenBatch = directRestored.Session.AdvanceDay(new CommandEnvelope("run", "abc", directRestored.Session.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary)));
        Equal("TargetReached", directThenBatch.StopReason);
        True(directThenBatch.Boundaries.Any(x => x.Status == CommandStatus.Committed));

        var legacyBatch = new Fixture();
        Equal(CommandStatus.Committed, legacyBatch.Execute("create", createCommand).Status);
        var originalRequest = new CommandEnvelope("run", legacyRoot, legacyBatch.Session.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary));
        var generated = legacyBatch.Session.AdvanceDay(originalRequest);
        Equal("TargetReached", generated.StopReason);
        True(generated.Boundaries.Count > 0);

        var legacyState = legacyBatch.Serializer.DeserializeAndValidate(legacyBatch.Session.ExportCheckpoint()).State!;
        var legacyReceipts = legacyState.Receipts.Where(x => !string.IsNullOrEmpty(x.ParentPayload)).OrderBy(x => x.Revision).ToArray();
        Equal(generated.Boundaries.Count, legacyReceipts.Length);
        for (var index = 0; index < legacyReceipts.Length; index++) legacyReceipts[index].CommandId = legacyRoot + "/" + index;
        var prePrCheckpoint = legacyBatch.Serializer.Serialize(legacyState);

        var restored = new Fixture(legacyBatch.Catalog, legacyState, prePrCheckpoint);
        var retry = restored.Session.AdvanceDay(originalRequest);
        Equal("TargetReached", retry.StopReason);
        Equal(legacyReceipts.Length, retry.Boundaries.Count);
        True(retry.Boundaries.All(x => x.Status == CommandStatus.AlreadyCommitted));
        Bytes(prePrCheckpoint, restored.Session.ExportCheckpoint());

        var beforeAlias = restored.Session.Snapshot().Instant.Date;
        var alias = restored.Session.AdvanceDay(new CommandEnvelope("run", "abc", restored.Session.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary)));
        Equal("TargetReached", alias.StopReason);
        True(alias.Boundaries.Any(x => x.Status == CommandStatus.Committed));
        True(restored.Session.Snapshot().Instant.Date != beforeAlias);
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

        var validBackup = CreateCharacterCheckpoint(newCatalog);
        AssertUnsupportedPrimaryPreserved(newSerializer, unsupportedPrimary, validBackup, "save.content_revision");
    }

    private static void MissingSavedSkillIsUnsupported()
    {
        var oldCatalog = Catalog();
        var unsupportedPrimary = CreateCharacterCheckpoint(oldCatalog);
        var newCatalog = CatalogWithoutSkills();
        var serializer = new JsonSaveSerializer(newCatalog);
        var result = serializer.DeserializeAndValidate(unsupportedPrimary);
        Equal(LoadStatus.UnsupportedContent, result.Status);
        Equal("save.content_id", result.Reason);
    }

    private static void MissingSceneClassificationIsPrecise()
    {
        var oldCatalog = Catalog(sceneId: "coding");
        var unsupportedPrimary = CreateCheckpointWithConsumedCareerScene(oldCatalog);

        var missingSceneCatalog = Catalog(sceneId: "meeting");
        var missingSerializer = new JsonSaveSerializer(missingSceneCatalog);
        var missing = missingSerializer.DeserializeAndValidate(unsupportedPrimary);
        Equal(LoadStatus.UnsupportedContent, missing.Status);
        Equal("save.content_id", missing.Reason);

        var validBackup = CreateCharacterCheckpoint(missingSceneCatalog);
        AssertUnsupportedPrimaryPreserved(missingSerializer, unsupportedPrimary, validBackup, "save.content_id");

        var foreignSceneSerializer = new JsonSaveSerializer(CatalogWithForeignCoding());
        var wrongCareer = foreignSceneSerializer.DeserializeAndValidate(unsupportedPrimary);
        Equal(LoadStatus.Corrupt, wrongCareer.Status);
    }

    private static void MalformedFieldsRemainCorrupt()
    {
        var oldCatalog = Catalog("v1");
        var checkpoint = CreateEmployedCheckpoint(oldCatalog);
        var state = new JsonSaveSerializer(oldCatalog).DeserializeAndValidate(checkpoint).State!;
        var newSerializer = new JsonSaveSerializer(Catalog("v2"));

        var emptyCareer = CloneState(state);
        emptyCareer.Employment!.CareerId = "";
        Equal(LoadStatus.Corrupt, DeserializeRaw(newSerializer, emptyCareer).Status);

        var emptyRevision = CloneState(state);
        emptyRevision.Employment!.DefinitionRevision = "";
        Equal(LoadStatus.Corrupt, DeserializeRaw(newSerializer, emptyRevision).Status);

        var negativeXpUnsupportedRevision = CloneState(state);
        negativeXpUnsupportedRevision.Employment!.Xp = -1;
        negativeXpUnsupportedRevision.Employment.DefinitionRevision = "v0";
        Equal(LoadStatus.Corrupt, DeserializeRaw(newSerializer, negativeXpUnsupportedRevision).Status);

        var malformedSkill = CloneState(state);
        malformedSkill.Skills[0].Id = "";
        Equal(LoadStatus.Corrupt, DeserializeRaw(newSerializer, malformedSkill).Status);

        var malformedScene = CloneState(state);
        malformedScene.Scheduler.Deck.Add("");
        Equal(LoadStatus.Corrupt, DeserializeRaw(newSerializer, malformedScene).Status);
    }

    private static byte[] CreateCharacterCheckpoint(ContentCatalog catalog)
    {
        var serializer = new JsonSaveSerializer(catalog);
        var initial = GameSession.NewState(catalog, "run", 12345, new SimDate(2026, 9, 7));
        var session = new GameSession(initial, catalog, serializer, new MemoryStore(serializer, null));
        Equal(CommandStatus.Committed, session.Execute(new CommandEnvelope("run", "create", 0, new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female"))).Status);
        return session.ExportCheckpoint();
    }

    private static byte[] CreateEmployedCheckpoint(ContentCatalog catalog)
    {
        var serializer = new JsonSaveSerializer(catalog);
        var initial = GameSession.NewState(catalog, "run", 12345, new SimDate(2026, 9, 7));
        var session = new GameSession(initial, catalog, serializer, new MemoryStore(serializer, null));
        Equal(CommandStatus.Committed, session.Execute(new CommandEnvelope("run", "create", 0, new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female"))).Status);
        Equal(CommandStatus.Committed, session.Execute(new CommandEnvelope("run", "job", session.Snapshot().Revision, new GameCommand(CommandKind.AcceptJob, "developer"))).Status);
        return session.ExportCheckpoint();
    }

    private static byte[] CreateCheckpointWithConsumedCareerScene(ContentCatalog catalog)
    {
        var serializer = new JsonSaveSerializer(catalog);
        var initial = GameSession.NewState(catalog, "run", 12345, new SimDate(2026, 9, 7));
        var session = new GameSession(initial, catalog, serializer, new MemoryStore(serializer, null));
        Equal(CommandStatus.Committed, session.Execute(new CommandEnvelope("run", "create", 0, new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female"))).Status);
        Equal(CommandStatus.Committed, session.Execute(new CommandEnvelope("run", "job", session.Snapshot().Revision, new GameCommand(CommandKind.AcceptJob, "developer"))).Status);
        for (var i = 0; i < 3; i++)
            Equal(CommandStatus.Committed, session.Execute(new CommandEnvelope("run", "advance-" + i, session.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary))).Status);
        True(serializer.DeserializeAndValidate(session.ExportCheckpoint()).State!.Scheduler.Deck.Contains("coding"));
        return session.ExportCheckpoint();
    }

    private static LoadResult DeserializeRaw(JsonSaveSerializer serializer, GameState state) =>
        serializer.DeserializeAndValidate(JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(state), 1, state.Revision));

    private static GameState CloneState(GameState state) => JsonSaveSerializer.ReadObject<GameState>(JsonSaveSerializer.WriteObject(state));

    private static void AssertUnsupportedPrimaryPreserved(JsonSaveSerializer serializer, byte[] unsupportedPrimary, byte[] validBackup, string reason)
    {
        var directory = Path.Combine(Path.GetTempPath(), "StartupLifeAstraChecks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "save.json");
        File.WriteAllBytes(path, unsupportedPrimary);
        File.WriteAllBytes(path + ".backup", validBackup);
        var store = new AtomicFileSaveStore(path, serializer);
        var read = store.Read();
        Equal(LoadStatus.UnsupportedContent, read.Status);
        Equal(reason, read.Reason);
        Bytes(unsupportedPrimary, File.ReadAllBytes(path));
        Equal(WriteStatus.Failed, store.Commit(validBackup, 0));
        Bytes(unsupportedPrimary, File.ReadAllBytes(path));
    }

    private static ContentCatalog Catalog(string revision = "v1", string sceneId = "coding")
    {
        var skill = new SkillDefinition("communication", "skill.communication", 100, 300, 600, 1000, 1500);
        var scene = new CareerSceneDefinition(sceneId, "scene." + sceneId, 100, 10, "communication", 1);
        var rank = new CareerRankDefinition("junior", 0, 0, 10000000, "communication", 0);
        var career = new CareerDefinition("developer", revision, "career.developer", 540, 1020, 4, 20,
            new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            new[] { scene }, new[] { rank });
        return new ContentCatalog("fixture.v1", new[] { skill }, new[] { career }, Array.Empty<CourseDefinition>(),
            new[] { new CharacterStartDefinition("fresh", 3000000, 10000, "base.female") },
            new EconomyBalanceDefinition(1, 1, 0), new DayScheduleDefinition(480, 1320));
    }

    private static ContentCatalog CatalogWithoutSkills() =>
        new ContentCatalog("fixture.v1", Array.Empty<SkillDefinition>(), Array.Empty<CareerDefinition>(), Array.Empty<CourseDefinition>(),
            new[] { new CharacterStartDefinition("fresh", 3000000, 10000, "base.female") },
            new EconomyBalanceDefinition(1, 1, 0), new DayScheduleDefinition(480, 1320));

    private static ContentCatalog CatalogWithForeignCoding()
    {
        var skill = new SkillDefinition("communication", "skill.communication", 100, 300, 600, 1000, 1500);
        var rank = new CareerRankDefinition("junior", 0, 0, 10000000, "communication", 0);
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        var developer = new CareerDefinition("developer", "v1", "career.developer", 540, 1020, 4, 20, days,
            new[] { new CareerSceneDefinition("meeting", "scene.meeting", 100, 10, "communication", 1) }, new[] { rank });
        var designer = new CareerDefinition("designer", "v1", "career.designer", 540, 1020, 4, 20, days,
            new[] { new CareerSceneDefinition("coding", "scene.coding", 100, 10, "communication", 1) }, new[] { rank });
        return new ContentCatalog("fixture.v1", new[] { skill }, new[] { developer, designer }, Array.Empty<CourseDefinition>(),
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
