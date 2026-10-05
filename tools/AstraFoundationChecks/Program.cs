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
        Check("H1 unknown historical receipt ownership blocks writes without guessing", HistoricalOwnersRequireProvenance);
        Check("H1 PR6 and pre-PR6 completed day/month receipts replay with their original owner", HistoricalCompletedRetry);
        Check("H1 historical partial day/month continuation persists ownership atomically", HistoricalPartialContinuation);
        Check("H1 ordinary root/index day/month continuations need no provenance", OrdinaryLegacyContinuation);
        Check("H1 failed normalization preserves checkpoint and owner bindings are defensive", HistoricalOwnershipFailureSafety);
        Check("H1 internal IDs exclude historical callers including delimiter and maximum-length roots", InternalNamespaceIsDisjoint);
        Check("H1 trusted recovery persists completed/partial day/month owners before retry", DurableHistoricalRestore);
        Check("H1 compatibility recovery failures and replacement ambiguity preserve exactly-once results", HistoricalRestoreFaults);
        Check("H1 compatibility writes reject gameplay changes and competing owner resolutions", CompatibilityWriteRestrictions);
        Check("H1 malformed ownership groups fail closed without rewriting the checkpoint", InvalidHistoricalOwnershipFailsClosed);
        Check("H2 unsupported career revision does not fall back to backup or overwrite primary", UnsupportedCareerRevisionIsPreserved);
        Check("H2 missing saved skill is unsupported content", MissingSavedSkillIsUnsupported);
        Check("H2 missing scheduled scene is unsupported while wrong-career scene is corrupt", MissingSceneClassificationIsPrecise);
        Check("H2 malformed persisted fields remain corrupt before compatibility resolution", MalformedFieldsRemainCorrupt);
        Check("R2 truncated skill state is corrupt and recovers the valid backup", TruncatedSkillsRemainCorrupt);
        Check("R2 cross-field corruption wins over missing content and unavailable revisions", CrossFieldCorruptionRemainsCorrupt);

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

        var restored = new Fixture(legacyBatch.Catalog, legacyState, prePrCheckpoint, Owners(legacyRoot));
        var retry = restored.Session.AdvanceDay(originalRequest);
        Equal("TargetReached", retry.StopReason);
        Equal(legacyReceipts.Length, retry.Boundaries.Count);
        True(retry.Boundaries.All(x => x.Status == CommandStatus.AlreadyCommitted));
        SameStateExceptReceiptIds(prePrCheckpoint, restored.Session.ExportCheckpoint());

        var beforeAlias = restored.Session.Snapshot().Instant.Date;
        var alias = restored.Session.AdvanceDay(new CommandEnvelope("run", "abc", restored.Session.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary)));
        Equal("TargetReached", alias.StopReason);
        True(alias.Boundaries.Any(x => x.Status == CommandStatus.Committed));
        True(restored.Session.Snapshot().Instant.Date != beforeAlias);
    }

    private static Dictionary<string, string> Owners(string root) => new(StringComparer.Ordinal) { ["$batch/3:abc/"] = root };

    private static byte[] HistoricalCheckpoint(bool month, bool partial) => File.ReadAllBytes(Path.Combine(
        AppContext.BaseDirectory, "Fixtures", "pr6-" + (month ? "month" : "day") + "-" + (partial ? "partial" : "completed") + ".json"));

    private static AdvanceResult Advance(Fixture fixture, CommandEnvelope request, bool month) =>
        month ? fixture.Session.AdvanceMonth(request) : fixture.Session.AdvanceDay(request);

    private static void HistoricalOwnersRequireProvenance()
    {
        foreach (var month in new[] { false, true }) foreach (var partial in new[] { false, true })
        {
            var checkpoint = HistoricalCheckpoint(month, partial);
            var serializer = new JsonSaveSerializer(Catalog(), GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
            Equal(LoadStatus.Valid, serializer.DeserializeAndValidate(checkpoint).Status);
            var f = new Fixture(state: serializer.DeserializeAndValidate(checkpoint).State!, persisted: checkpoint);
            var inMemoryBefore = f.Session.ExportCheckpoint();
            foreach (var root in new[] { "abc", "$batch/3:abc", "fresh" })
                foreach (var revision in new[] { 2L, f.Session.Snapshot().Revision })
                    Equal("RecoveryRequired", Advance(f, new CommandEnvelope("run", root, revision, new GameCommand(CommandKind.AdvanceBoundary)), month).StopReason);
            var direct = f.Execute("abc", new GameCommand(CommandKind.Resign));
            Equal(CommandStatus.RecoveryRequired, direct.Status);
            Equal("save.batch_owner_required", direct.ReasonKey);
            Equal(CommandStatus.RecoveryRequired, f.Execute("fresh-direct", new GameCommand(CommandKind.Resign)).Status);
            Bytes(inMemoryBefore, f.Session.ExportCheckpoint());
            Bytes(checkpoint, f.Store.Bytes!);
            Equal(0, f.Store.CommitAttempts);
            Equal(0, f.Store.CompatibilityAttempts);
        }
    }

    private static void HistoricalCompletedRetry()
    {
        foreach (var owner in new[] { "abc", "$batch/3:abc" }) foreach (var month in new[] { false, true })
        {
            var checkpoint = HistoricalCheckpoint(month, false);
            var state = new JsonSaveSerializer(Catalog(), GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration()).DeserializeAndValidate(checkpoint).State!;
            var expected = state.Receipts.Where(x => x.ParentPayload.Length > 0).OrderBy(x => x.Revision).ToArray();
            var f = new Fixture(state: state, persisted: checkpoint, owners: Owners(owner));
            var request = new CommandEnvelope("run", owner, 2, new GameCommand(CommandKind.AdvanceBoundary));
            var retry = Advance(f, request, month);
            Equal("TargetReached", retry.StopReason);
            Equal(expected.Length, retry.Boundaries.Count);
            True(retry.Boundaries.All(x => x.Status == CommandStatus.AlreadyCommitted));
            True(expected.Select(x => (x.OperationId, x.Revision, x.MinutesConsumed)).SequenceEqual(
                retry.Boundaries.Select(x => (x.OperationId, x.Revision, x.MinutesConsumed))));
            Equal("command.id_conflict", f.Execute(owner, new GameCommand(CommandKind.Resign)).ReasonKey);
            Equal("InvalidState", Advance(f, request, !month).StopReason);
            Equal("InvalidState", Advance(f, new CommandEnvelope("run", owner, 2, new GameCommand(CommandKind.AdvanceBoundary, amount: 1)), month).StopReason);
            Equal("InvalidRequest", Advance(f, new CommandEnvelope("run", "stale", 2, new GameCommand(CommandKind.AdvanceBoundary)), month).StopReason);
            SameStateExceptReceiptIds(checkpoint, f.Session.ExportCheckpoint());
            Bytes(f.Session.ExportCheckpoint(), ProjectCurrent(f.Serializer, f.Store.Bytes!));
            Equal(0, f.Store.CommitAttempts);
            Equal(1, f.Store.CompatibilityAttempts);
        }
    }

    private static void HistoricalPartialContinuation()
    {
        // These bytes were produced independently by both PR6 abc and pre-PR6 $batch/3:abc.
        foreach (var owner in new[] { "abc", "$batch/3:abc" }) foreach (var month in new[] { false, true })
        {
            var checkpoint = HistoricalCheckpoint(month, true);
            var state = new JsonSaveSerializer(Catalog(), GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration()).DeserializeAndValidate(checkpoint).State!;
            var f = new Fixture(state: state, persisted: checkpoint, owners: Owners(owner));
            var request = new CommandEnvelope("run", owner, 2, new GameCommand(CommandKind.AdvanceBoundary));
            var resumed = Advance(f, request, month);
            Equal("TargetReached", resumed.StopReason);
            True(resumed.Boundaries.Take(2).All(x => x.Status == CommandStatus.AlreadyCommitted));
            True(resumed.Boundaries.Skip(2).All(x => x.Status == CommandStatus.Committed));

            var control = new Fixture();
            Equal(CommandStatus.Committed, control.Execute("create", new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female")).Status);
            Equal(CommandStatus.Committed, control.Execute("job", new GameCommand(CommandKind.AcceptJob, "developer")).Status);
            Equal("TargetReached", Advance(control, request, month).StopReason);
            // Compare the entire state: receipts, cash, salary, ledger, RNG, skills, time and operation counters.
            Bytes(control.Session.ExportCheckpoint(), f.Session.ExportCheckpoint());
            var durable = f.Store.Bytes!;
            var restored = new Fixture(state: f.Serializer.DeserializeAndValidate(durable).State!, persisted: durable);
            var retry = Advance(restored, request, month);
            Equal("TargetReached", retry.StopReason);
            Equal(resumed.Boundaries.Count, retry.Boundaries.Count);
            True(retry.Boundaries.All(x => x.Status == CommandStatus.AlreadyCommitted));
            Bytes(durable, restored.Session.ExportCheckpoint());
            Equal("command.id_conflict", restored.Execute(owner, new GameCommand(CommandKind.Resign)).ReasonKey);
            if (owner == "$batch/3:abc")
            {
                var independent = restored.Session.AdvanceDay(new CommandEnvelope("run", "abc", restored.Session.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary)));
                Equal("TargetReached", independent.StopReason);
                True(independent.Boundaries.All(x => x.Status == CommandStatus.Committed));
            }
        }
    }

    private static void HistoricalOwnershipFailureSafety()
    {
        var checkpoint = HistoricalCheckpoint(false, true);
        var state = new JsonSaveSerializer(Catalog(), GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration()).DeserializeAndValidate(checkpoint).State!;
        var bindings = Owners("abc");
        var f = new Fixture(state: state, persisted: checkpoint, owners: bindings);
        var inMemoryBefore = f.Session.ExportCheckpoint();
        bindings["$batch/3:abc/"] = "$batch/3:abc";
        f.Store.Allowed = 0;
        var request = new CommandEnvelope("run", "abc", 2, new GameCommand(CommandKind.AdvanceBoundary));
        var failed = f.Session.AdvanceDay(request);
        Equal("RecoveryRequired", failed.StopReason);
        Equal(0, failed.Boundaries.Count);
        Bytes(inMemoryBefore, f.Session.ExportCheckpoint());
        Bytes(checkpoint, f.Store.Bytes!);
        f.Store.Allowed = int.MaxValue;
        Equal("TargetReached", f.Session.AdvanceDay(request).StopReason);
        try
        {
            _ = new Fixture(state: state, persisted: checkpoint, owners: Owners("unrelated"));
            throw new Exception("Invalid binding accepted");
        }
        catch (ArgumentException) { }
    }

    private static void OrdinaryLegacyContinuation()
    {
        foreach (var month in new[] { false, true })
        {
            var state = new JsonSaveSerializer(Catalog(), GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration()).DeserializeAndValidate(HistoricalCheckpoint(month, true)).State!;
            foreach (var receipt in state.Receipts.Where(x => x.ParentPayload.Length > 0))
                receipt.CommandId = "legacy/" + receipt.CommandId.Substring(receipt.CommandId.LastIndexOf('/') + 1);
            var checkpoint = new JsonSaveSerializer(Catalog(), GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration()).Serialize(state);
            var f = new Fixture(state: state, persisted: checkpoint);
            var request = new CommandEnvelope("run", "legacy", 2, new GameCommand(CommandKind.AdvanceBoundary));
            var resumed = Advance(f, request, month);
            Equal("TargetReached", resumed.StopReason);
            True(resumed.Boundaries.Take(2).All(x => x.Status == CommandStatus.AlreadyCommitted));
            var durable = f.Session.ExportCheckpoint();
            var restored = new Fixture(state: f.Serializer.DeserializeAndValidate(durable).State!, persisted: durable);
            True(Advance(restored, request, month).Boundaries.All(x => x.Status == CommandStatus.AlreadyCommitted));
            Bytes(durable, restored.Session.ExportCheckpoint());
        }
    }

    private static void InternalNamespaceIsDisjoint()
    {
        var f = new Fixture();
        Equal(CommandStatus.Committed, f.Execute("create", new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female")).Status);
        foreach (var root in new[] { "a/0", "3:abc/", "Việt/1", new string('z', 256) })
        {
            var request = new CommandEnvelope("run", root, f.Session.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary));
            Equal("TargetReached", f.Session.AdvanceDay(request).StopReason);
            var bytes = f.Session.ExportCheckpoint();
            var restored = new Fixture(state: f.Serializer.DeserializeAndValidate(bytes).State!, persisted: bytes);
            True(restored.Session.AdvanceDay(request).Boundaries.All(x => x.Status == CommandStatus.AlreadyCommitted));
            Bytes(bytes, restored.Session.ExportCheckpoint());
            if (root.Length == 256)
            {
                var pr6State = f.Serializer.DeserializeAndValidate(bytes).State!;
                foreach (var receipt in pr6State.Receipts.Where(x => x.ParentPayload.Length > 0 &&
                    x.CommandId.StartsWith(new string('$', 257) + "batch/256:", StringComparison.Ordinal)))
                    receipt.CommandId = "$batch/256:" + root + "/" + receipt.CommandId.Substring(receipt.CommandId.LastIndexOf('/') + 1);
                var pr6Bytes = f.Serializer.Serialize(pr6State);
                var pr6Restored = new Fixture(state: pr6State, persisted: pr6Bytes);
                var pr6Retry = pr6Restored.Session.AdvanceDay(request);
                Equal("TargetReached", pr6Retry.StopReason);
                True(pr6Retry.Boundaries.Count > 0 && pr6Retry.Boundaries.All(x => x.Status == CommandStatus.AlreadyCommitted));
                SameStateExceptReceiptIds(pr6Bytes, pr6Restored.Session.ExportCheckpoint());
            }
        }
        var internalId = f.Serializer.DeserializeAndValidate(f.Session.ExportCheckpoint()).State!.Receipts.First(x => x.ParentPayload.Length > 0).CommandId;
        True(internalId.Length > 256);
        Equal("command.invalid_id", f.Execute(internalId, new GameCommand(CommandKind.Resign)).ReasonKey);
        Equal("InvalidRequest", f.Session.AdvanceDay(new CommandEnvelope("run", internalId, f.Session.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary))).StopReason);
    }

    private static void TruncatedSkillsRemainCorrupt()
    {
        var catalog = Catalog();
        var serializer = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
        var backup = CreateCharacterCheckpoint(catalog);
        var truncated = serializer.DeserializeAndValidate(backup).State!;
        truncated.Skills.Clear();
        var bytes = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(truncated), SaveSchema.CurrentVersion, truncated.Revision);
        Equal(LoadStatus.Corrupt, serializer.DeserializeAndValidate(bytes).Status);
        var directory = Path.Combine(Path.GetTempPath(), "StartupLifeAstraChecks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "save.json");
        File.WriteAllBytes(path, bytes);
        File.WriteAllBytes(path + ".backup", backup);
        var store = new AtomicFileSaveStore(path, serializer);
        Equal(LoadStatus.RecoveredBackup, store.Read().Status);
        var session = new GameSession(store.Read().State!, catalog, serializer, store);
        Equal(CommandStatus.Committed, session.Execute(new CommandEnvelope("run", "job", truncated.Revision, new GameCommand(CommandKind.AcceptJob, "developer"))).Status);
        Equal(LoadStatus.Valid, store.Read().Status);
    }

    private static string SavePath(byte[] checkpoint)
    {
        var directory = Path.Combine(Path.GetTempPath(), "StartupLifeAstraChecks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "save.json");
        File.WriteAllBytes(path, checkpoint);
        return path;
    }

    private static void DurableHistoricalRestore()
    {
        foreach (var owner in new[] { "abc", "$batch/3:abc" }) foreach (var month in new[] { false, true })
            foreach (var partial in new[] { false, true })
            {
                var catalog = Catalog();
                var serializer = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
                var checkpoint = HistoricalCheckpoint(month, partial);
                var path = SavePath(checkpoint);
                var original = serializer.DeserializeAndValidate(checkpoint).State!;
                var store = new AtomicFileSaveStore(path, serializer);
                True(!GameSession.TryRestore(catalog, serializer, store, out var unresolved, out var diagnosis));
                Equal<GameSession?>(null, unresolved);
                Equal(LoadStatus.RecoveryRequired, diagnosis.Status);
                Equal("save.batch_owner_required", diagnosis.Reason);
                Bytes(checkpoint, File.ReadAllBytes(path));

                True(GameSession.TryRestore(catalog, serializer, store, out var imported, out var resolved, Owners(owner)));
                Equal(LoadStatus.Valid, resolved.Status);
                Equal(original.Revision, imported!.Snapshot().Revision);
                var bound = File.ReadAllBytes(path);
                SameStateExceptReceiptIds(checkpoint, bound);
                Bytes(bound, File.ReadAllBytes(path + ".backup"));
                Equal(SaveSchema.HistoricalV1, JsonSaveSerializer.ReadObject<SaveEnvelope>(bound).SchemaVersion);
                Equal(SaveSchema.CurrentVersion, serializer.DeserializeAndValidate(bound).State!.SaveVersion);

                // New process/store/session, before ANY gameplay command or retry, with no in-memory bindings.
                True(GameSession.TryRestore(catalog, serializer, new AtomicFileSaveStore(path, serializer), out var cold, out var coldLoad));
                Equal(LoadStatus.Valid, coldLoad.Status);
                Bytes(ProjectCurrent(serializer, bound), cold!.ExportCheckpoint());
                // Ownership also survives validated-backup recovery.
                File.WriteAllText(path, "broken primary");
                True(GameSession.TryRestore(catalog, serializer, new AtomicFileSaveStore(path, serializer), out cold, out coldLoad));
                Equal(LoadStatus.RecoveredBackup, coldLoad.Status);
                Bytes(ProjectCurrent(serializer, bound), cold!.ExportCheckpoint());
                var request = new CommandEnvelope("run", owner, 2, new GameCommand(CommandKind.AdvanceBoundary));
                var retry = month ? cold.AdvanceMonth(request) : cold.AdvanceDay(request);
                Equal("TargetReached", retry.StopReason);
                if (!partial)
                {
                    True(retry.Boundaries.All(x => x.Status == CommandStatus.AlreadyCommitted));
                    Equal(original.Receipts.Count(x => x.ParentPayload.Length > 0), retry.Boundaries.Count);
                    True(original.Receipts.Where(x => x.ParentPayload.Length > 0).Select(x => (x.OperationId, x.Revision, x.MinutesConsumed)).SequenceEqual(
                        retry.Boundaries.Select(x => (x.OperationId, x.Revision, x.MinutesConsumed))));
                    Bytes(ProjectCurrent(serializer, bound), cold.ExportCheckpoint());
                }
                else
                {
                    True(retry.Boundaries.Take(2).All(x => x.Status == CommandStatus.AlreadyCommitted));
                    var control = new Fixture();
                    control.Execute("create", new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female"));
                    control.Execute("job", new GameCommand(CommandKind.AcceptJob, "developer"));
                    Equal("TargetReached", Advance(control, request, month).StopReason);
                    Bytes(control.Session.ExportCheckpoint(), cold.ExportCheckpoint());
                }
                Equal("command.id_conflict", cold.Execute(new CommandEnvelope("run", owner, cold.Snapshot().Revision, new GameCommand(CommandKind.Resign))).ReasonKey);
                Equal("InvalidState", (month ? cold.AdvanceDay(request) : cold.AdvanceMonth(request)).StopReason);
                if (owner == "$batch/3:abc")
                {
                    var separate = cold.AdvanceDay(new CommandEnvelope("run", "abc", cold.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary)));
                    Equal("TargetReached", separate.StopReason);
                    True(separate.Boundaries.Count > 0 && separate.Boundaries.All(x => x.Status == CommandStatus.Committed));
                }
            }
    }

    private static void HistoricalRestoreFaults()
    {
        foreach (var phase in new[] { "before-replace", "after-replace", "before-compatibility-backup" })
        {
            var catalog = Catalog();
            var serializer = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
            var checkpoint = HistoricalCheckpoint(false, false);
            var path = SavePath(checkpoint);
            File.WriteAllBytes(path + ".backup", checkpoint);
            var store = new AtomicFileSaveStore(path, serializer, current => current == phase);
            True(!GameSession.TryRestore(catalog, serializer, store, out var failed, out var result, Owners("abc")));
            Equal<GameSession?>(null, failed);
            Equal(LoadStatus.RecoveryRequired, result.Status);
            SameStateExceptReceiptIds(checkpoint, File.ReadAllBytes(path));
            if (phase == "before-replace")
            {
                Bytes(checkpoint, File.ReadAllBytes(path));
                Bytes(checkpoint, File.ReadAllBytes(path + ".backup"));
                Equal("save.compatibility_write_failed", result.Reason);
            }
            else Equal("save.recovery_required", result.Reason);
            var clean = new AtomicFileSaveStore(path, serializer);
            if (phase != "before-replace")
            {
                var primaryBeforeRead = File.ReadAllBytes(path);
                var backupBeforeRead = File.ReadAllBytes(path + ".backup");
                Equal(BatchReceiptIdentity.BackupRepairReason, clean.Read().Reason);
                Bytes(primaryBeforeRead, File.ReadAllBytes(path));
                Bytes(backupBeforeRead, File.ReadAllBytes(path + ".backup"));
            }
            True(GameSession.TryRestore(catalog, serializer, clean, out var recovered, out _, phase == "before-replace" ? Owners("abc") : null));
            var before = recovered!.ExportCheckpoint();
            Bytes(before, ProjectCurrent(serializer, File.ReadAllBytes(path + ".backup")));
            File.WriteAllText(path, "corrupt after reconciled import");
            True(GameSession.TryRestore(catalog, serializer, new AtomicFileSaveStore(path, serializer), out recovered, out var backupLoad));
            Equal(LoadStatus.RecoveredBackup, backupLoad.Status);
            var retry = recovered!.AdvanceDay(new CommandEnvelope("run", "abc", 2, new GameCommand(CommandKind.AdvanceBoundary)));
            Equal("TargetReached", retry.StopReason);
            True(retry.Boundaries.All(x => x.Status == CommandStatus.AlreadyCommitted));
            Bytes(before, recovered.ExportCheckpoint());
        }
    }

    private static void CompatibilityWriteRestrictions()
    {
        var catalog = Catalog();
        var serializer = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
        var expected = HistoricalCheckpoint(false, false);
        var path = SavePath(expected);
        var store = new AtomicFileSaveStore(path, serializer);
        var changed = serializer.DeserializeAndValidate(expected).State!;
        changed.Cash++;
        // The public serializer now rejects forged cash before storage. Build an adversarial envelope
        // directly so this H1 test still reaches the compatibility-store full-checkpoint guard.
        var forgedCash = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(changed), SaveSchema.CurrentVersion, changed.Revision);
        Equal(WriteStatus.Failed, store.CommitReceiptCompatibility(expected, forgedCash));
        Bytes(expected, File.ReadAllBytes(path));
        changed = serializer.DeserializeAndValidate(expected).State!;
        changed.Receipts.Single(x => x.CommandId == "job").CommandId = "renamed-direct";
        Equal(WriteStatus.Failed, store.CommitReceiptCompatibility(expected, serializer.Serialize(changed)));
        Bytes(expected, File.ReadAllBytes(path));

        True(GameSession.TryRestore(catalog, serializer, store, out var imported, out _, Owners("abc")));
        var durable = File.ReadAllBytes(path);
        var competitor = serializer.DeserializeAndValidate(expected).State!;
        var index = 0;
        foreach (var receipt in competitor.Receipts.Where(x => x.ParentPayload.Length > 0))
            receipt.CommandId = BatchReceiptIdentity.InternalPrefix + "12:$batch/3:abc/" + index++;
        // Same revision, different proposed owner: full-checkpoint comparison must reject the stale import.
        Equal(WriteStatus.Failed, store.CommitReceiptCompatibility(expected, serializer.Serialize(competitor)));
        Bytes(durable, File.ReadAllBytes(path));
        Equal(WriteStatus.Failed, store.Commit(serializer.Serialize(competitor), competitor.Revision));
        Bytes(durable, File.ReadAllBytes(path));
        var differentBackup = serializer.DeserializeAndValidate(expected).State!;
        differentBackup.Cash++;
        // F1 makes same-generation forged gameplay semantically invalid. Keep raw checksummed bytes here
        // to prove an invalid/different backup is never mistaken for a pending ownership mirror.
        var differentBytes = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(differentBackup), SaveSchema.CurrentVersion, differentBackup.Revision);
        File.WriteAllBytes(path + ".backup", differentBytes);
        Equal("", store.Read().Reason);
        True(GameSession.TryRestore(catalog, serializer, store, out _, out _));
        Bytes(differentBytes, File.ReadAllBytes(path + ".backup"));
    }

    private static void CrossFieldCorruptionRemainsCorrupt()
    {
        var catalog = Catalog();
        var original = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration()).DeserializeAndValidate(HistoricalCheckpoint(false, true)).State!;
        var mutations = new Action<GameState>[]
        {
            s => { s.Employment!.Xp = -1; s.Employment.DefinitionRevision = "unavailable"; },
            s => s.Employment!.CareerId = "",
            s => s.Employment!.CareerId = "INVALID ID",
            s => s.Employment!.DefinitionRevision = " ",
            s => s.Scheduler.Cursor = -1,
            s => s.Receipts[0].MinutesConsumed = -1,
            s => s.Ledger.Add(new LedgerEntry { Id = "invalid-ledger", OperationId = "old-operation", Amount = -1 }),
            s => s.PreviousEmployment.Add(new EmploymentState { CareerId = "developer", Xp = -1 }),
            s => s.CompletedCourses.Add(new CourseState { DefinitionId = "missing-course", InstanceId = "old", ProgressUnits = -1 })
        };
        foreach (var mutation in mutations)
            foreach (var serializer in new[] { new JsonSaveSerializer(CatalogWithoutSkills(), GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration()), new JsonSaveSerializer(Catalog("v2"), GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration()) })
            {
                var state = CloneState(original);
                mutation(state);
                Equal(LoadStatus.Corrupt, DeserializeRaw(serializer, state).Status);
                state.ContentVersion = "unavailable.catalog";
                Equal(LoadStatus.Corrupt, DeserializeRaw(serializer, state).Status);
            }
        // Missing scene content must not hide a malformed receipt later in the DTO.
        var sceneState = CloneState(original);
        sceneState.Scheduler.Deck.Add("missing-scene");
        sceneState.Receipts[0].MinutesConsumed = -1;
        Equal(LoadStatus.Corrupt, DeserializeRaw(new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration()), sceneState).Status);
        var corrupt = CloneState(original);
        corrupt.Employment!.Xp = -1;
        var missing = new JsonSaveSerializer(CatalogWithoutSkills(), GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
        var path = SavePath(JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(corrupt), SaveSchema.CurrentVersion, corrupt.Revision));
        var backup = GameSession.NewState(CatalogWithoutSkills(), "backup", 54321, new SimDate(2026, 9, 7));
        File.WriteAllBytes(path + ".backup", missing.Serialize(backup));
        Equal(LoadStatus.RecoveredBackup, new AtomicFileSaveStore(path, missing).Read().Status);
    }

    private static void InvalidHistoricalOwnershipFailsClosed()
    {
        var catalog = Catalog();
        var serializer = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
        var state = serializer.DeserializeAndValidate(HistoricalCheckpoint(false, true)).State!;
        state.Receipts.Last().CommandId = "abc/0";
        var checkpoint = serializer.Serialize(state);
        var path = SavePath(checkpoint);
        True(!GameSession.TryRestore(catalog, serializer, new AtomicFileSaveStore(path, serializer), out var failed, out var result, Owners("abc")));
        Equal<GameSession?>(null, failed);
        Equal(LoadStatus.RecoveryRequired, result.Status);
        Equal("save.batch_identity_invalid", result.Reason);
        Bytes(checkpoint, File.ReadAllBytes(path));
    }

    private static void UnsupportedCareerRevisionIsPreserved()
    {
        var oldCatalog = Catalog("v1");
        var oldSerializer = new JsonSaveSerializer(oldCatalog, GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
        var initial = GameSession.NewState(oldCatalog, "run", 12345, new SimDate(2026, 9, 7));
        var memory = new MemoryStore(oldSerializer, null);
        var session = new GameSession(initial, oldCatalog, oldSerializer, memory);
        Equal(CommandStatus.Committed, session.Execute(new CommandEnvelope("run", "create", 0, new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female"))).Status);
        Equal(CommandStatus.Committed, session.Execute(new CommandEnvelope("run", "job", session.Snapshot().Revision, new GameCommand(CommandKind.AcceptJob, "developer"))).Status);
        var unsupportedPrimary = session.ExportCheckpoint();

        var newCatalog = Catalog("v2");
        var newSerializer = new JsonSaveSerializer(newCatalog, GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
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
        var serializer = new JsonSaveSerializer(newCatalog, GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
        var result = serializer.DeserializeAndValidate(unsupportedPrimary);
        Equal(LoadStatus.UnsupportedContent, result.Status);
        Equal("save.content_id", result.Reason);
    }

    private static void MissingSceneClassificationIsPrecise()
    {
        var oldCatalog = Catalog(sceneId: "coding");
        var unsupportedPrimary = CreateCheckpointWithConsumedCareerScene(oldCatalog);

        var missingSceneCatalog = Catalog(sceneId: "meeting");
        var missingSerializer = new JsonSaveSerializer(missingSceneCatalog, GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
        var missing = missingSerializer.DeserializeAndValidate(unsupportedPrimary);
        Equal(LoadStatus.UnsupportedContent, missing.Status);
        Equal("save.content_id", missing.Reason);

        var validBackup = CreateCharacterCheckpoint(missingSceneCatalog);
        AssertUnsupportedPrimaryPreserved(missingSerializer, unsupportedPrimary, validBackup, "save.content_id");

        var foreignSceneSerializer = new JsonSaveSerializer(CatalogWithForeignCoding(), GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
        var wrongCareer = foreignSceneSerializer.DeserializeAndValidate(unsupportedPrimary);
        Equal(LoadStatus.Corrupt, wrongCareer.Status);
    }

    private static void MalformedFieldsRemainCorrupt()
    {
        var oldCatalog = Catalog("v1");
        var checkpoint = CreateEmployedCheckpoint(oldCatalog);
        var state = new JsonSaveSerializer(oldCatalog, GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration()).DeserializeAndValidate(checkpoint).State!;
        var newSerializer = new JsonSaveSerializer(Catalog("v2"), GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());

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
        var serializer = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
        var initial = GameSession.NewState(catalog, "run", 12345, new SimDate(2026, 9, 7));
        var session = new GameSession(initial, catalog, serializer, new MemoryStore(serializer, null));
        Equal(CommandStatus.Committed, session.Execute(new CommandEnvelope("run", "create", 0, new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female"))).Status);
        return session.ExportCheckpoint();
    }

    private static byte[] CreateEmployedCheckpoint(ContentCatalog catalog)
    {
        var serializer = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
        var initial = GameSession.NewState(catalog, "run", 12345, new SimDate(2026, 9, 7));
        var session = new GameSession(initial, catalog, serializer, new MemoryStore(serializer, null));
        Equal(CommandStatus.Committed, session.Execute(new CommandEnvelope("run", "create", 0, new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female"))).Status);
        Equal(CommandStatus.Committed, session.Execute(new CommandEnvelope("run", "job", session.Snapshot().Revision, new GameCommand(CommandKind.AcceptJob, "developer"))).Status);
        return session.ExportCheckpoint();
    }

    private static byte[] CreateCheckpointWithConsumedCareerScene(ContentCatalog catalog)
    {
        var serializer = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
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
        serializer.DeserializeAndValidate(JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(state), SaveSchema.CurrentVersion, state.Revision));

    private static GameState CloneState(GameState state) => JsonSaveSerializer.ReadObject<GameState>(JsonSaveSerializer.WriteObject(state));

    private static byte[] ProjectCurrent(JsonSaveSerializer serializer, byte[] checkpoint)
    {
        var loaded = serializer.DeserializeAndValidate(checkpoint);
        if (loaded.Status != LoadStatus.Valid && loaded.Status != LoadStatus.RecoveredBackup)
            throw new Exception("Checkpoint cannot be projected to current schema: " + loaded.Reason);
        return serializer.Serialize(loaded.State!);
    }

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
        True(!GameSession.TryRestore(Catalog(), serializer, store, out var session, out var blocked, Owners("abc")));
        Equal<GameSession?>(null, session);
        Equal(LoadStatus.UnsupportedContent, blocked.Status);
        Equal(WriteStatus.Failed, store.CommitReceiptCompatibility(validBackup, validBackup));
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
        public MemoryStore Store { get; }
        public Fixture(ContentCatalog? catalog = null, GameState? state = null, byte[]? persisted = null, IReadOnlyDictionary<string, string>? owners = null)
        {
            Catalog = catalog ?? Program.Catalog();
            Serializer = new JsonSaveSerializer(Catalog, GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
            var initial = state ?? GameSession.NewState(Catalog, "run", 12345, new SimDate(2026, 9, 7));
            Store = new MemoryStore(Serializer, persisted);
            Session = new GameSession(initial, Catalog, Serializer, Store, owners);
        }
        public CommandResult Execute(string id, GameCommand command) => Session.Execute(new CommandEnvelope("run", id, Session.Snapshot().Revision, command));
    }

    private sealed class MemoryStore : ISaveCompatibilityStore
    {
        private readonly ISaveSerializer serializer;
        private byte[]? bytes;
        public byte[]? Bytes => bytes == null ? null : (byte[])bytes.Clone();
        public int Allowed { get; set; } = int.MaxValue;
        public int CommitAttempts { get; private set; }
        public int CompatibilityAttempts { get; private set; }
        public MemoryStore(ISaveSerializer serializer, byte[]? initial) { this.serializer = serializer; bytes = initial == null ? null : (byte[])initial.Clone(); }
        public LoadResult Read() => bytes == null ? new LoadResult(LoadStatus.Missing) : serializer.DeserializeAndValidate(bytes);
        public WriteStatus Commit(byte[] candidate, long expectedRevision)
        {
            CommitAttempts++;
            if (Allowed == 0) return WriteStatus.Failed;
            Allowed--;
            var next = serializer.DeserializeAndValidate(candidate);
            var previous = Read();
            if (next.Status != LoadStatus.Valid || next.State!.Revision != expectedRevision + 1) return WriteStatus.Failed;
            if (previous.Status != LoadStatus.Missing && previous.Status != LoadStatus.Valid && previous.Status != LoadStatus.RecoveredBackup) return WriteStatus.Failed;
            if ((previous.State?.Revision ?? 0) != expectedRevision) return WriteStatus.Failed;
            bytes = (byte[])candidate.Clone();
            return WriteStatus.Committed;
        }
        public WriteStatus CommitReceiptCompatibility(byte[] expectedCheckpoint, byte[] normalizedCheckpoint)
        {
            CompatibilityAttempts++;
            if (Allowed == 0) return WriteStatus.Failed;
            Allowed--;
            var previous = Read();
            var expected = serializer.DeserializeAndValidate(expectedCheckpoint);
            var candidate = serializer.DeserializeAndValidate(normalizedCheckpoint);
            if (previous.Status != LoadStatus.Valid || expected.Status != LoadStatus.Valid || candidate.Status != LoadStatus.Valid ||
                !serializer.Serialize(previous.State!).SequenceEqual(serializer.Serialize(expected.State!)) ||
                candidate.State!.Revision != previous.State!.Revision) return WriteStatus.Failed;
            bytes = (byte[])normalizedCheckpoint.Clone();
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
    private static void SameStateExceptReceiptIds(byte[] expected, byte[] actual)
    {
        var serializer = new JsonSaveSerializer(Catalog(), GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
        var oldState = serializer.DeserializeAndValidate(expected).State!;
        var newState = serializer.DeserializeAndValidate(actual).State!;
        Equal(oldState.Receipts.Count, newState.Receipts.Count);
        for (var i = 0; i < oldState.Receipts.Count; i++) newState.Receipts[i].CommandId = oldState.Receipts[i].CommandId;
        Bytes(JsonSaveSerializer.WriteObject(oldState), JsonSaveSerializer.WriteObject(newState));
    }
}
