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
        Check("M2 rewinding cursor and history cannot replay XP or salary", RewindCannotReplaySalary);
        Check("M2 same-career deck cannot violate quota", ImpossibleQuotaIsCorrupt);
        Check("M2 correct quota cannot hide impossible deck ordering", ImpossibleOrderIsCorrupt);
        Check("M2 generation overflow is rejected before publication", GenerationOverflowIsCorrupt);
        Check("M2 stale signature after an already-consumed promotion is corrupt", StaleSignatureIsCorrupt);
        Check("M2 non-career boundary history cannot be removed", NonCareerHistoryCannotBeRemoved);
        Check("M2 canonical past activities cannot be invented", PastActivityCannotBeForged);
        Check("M2 equal activity counts cannot hide forged provenance", EqualCountsCannotHideForgery);
        Check("M2 global entity sequence cannot be shared by different kinds", EntitySequenceIsGlobal);
        Check("M2 counter covers settled entity references retained in ledger", LedgerReferenceCannotExceedCounter);
        Check("M2 committed receipt history cannot be removed or reordered", ReceiptHistoryIsComplete);
        Check("M2 malformed canonical payloads remain structural corruption", MalformedPayloadIsCorrupt);
        Check("M2 timeline corruption wins over unavailable content", TimelineCorruptionBeforeCompatibility);
        Check("M2 known scheduler corruption wins over missing background", SchedulerCorruptionBeforeUnrelatedCompatibility);
        Check("M2 valid missing background remains unsupported content", ValidMissingBackgroundIsUnsupported);
        Check("M2 missing scheduler scenes remain unsupported content", MissingSchedulerSceneIsUnsupported);
        Check("M2 multi-scene promotion and cycle-seam continuation are deterministic", MultiSceneTransitionsRemainDeterministic);
        Check("M2 resignation and subsequent employment preserve scheduler provenance", SubsequentEmploymentRemainsValid);
        Check("M2 missing historical scheduler scene is unsupported", MissingHistoricalSceneIsUnsupported);
        Check("M2 historical scene belonging to another career is corrupt", ForeignHistoricalSceneIsCorrupt);
        Check("M2 serializer requires an explicit restore verifier", SerializerRequiresVerifier);
        Check("M2 coordinated receipt and cursor rewind cannot retain rewards", CoordinatedRewindIsCorrupt);
        Check("M2 committed reward and salary provenance cannot be forged", ForgedRewardsAreCorrupt);
        Check("M2 scheduler RNG and plausible generation must match its trace", SchedulerTraceFieldsAreCorrupt);
        Check("M2 creation receipt binds intrinsic character identity", CharacterCreationProvenanceIsBound);
        Check("M2 available start definition binds learning speed", LearningSpeedProvenanceIsBound);
        Check("M2 reconstructed cash must match authoritative checkpoint", CashProvenanceIsBound);
        Check("M2 unemployed boundary after mid-shift resignation is not career work", PostResignationBoundaryRemainsValid);

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
        operation.NextOperation = 1;
        Equal(LoadStatus.Corrupt, Raw(f.Serializer, operation).Status);

        var entity = f.State();
        entity.NextEntity = 1;
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

    private static void RewindCannotReplaySalary()
    {
        var f = Ready(); f.ToEndOfWork(); var state = f.State();
        Equal(40L, state.Employment!.Xp); Equal(1, state.Claims.Count); Equal(1, state.Ledger.Count(x => x.Category == "salary.accrual"));
        state.Minute = 900; state.Employment.ScenesToday = 3;
        True(state.ConsumedActivities.Remove("run/activity/2026-09-07/900"));
        state.CurrentActivity = state.Receipts.Last().OperationId;
        state.Scheduler.Cursor = 3; state.Scheduler.LastScene = state.Scheduler.Deck[2];
        RejectBeforePublication(f, state);
        var receipt = f.State().Receipts.Last(); var before = f.Session.ExportCheckpoint();
        Equal(CommandStatus.AlreadyCommitted, f.Session.Execute(new CommandEnvelope("run", receipt.CommandId, 0, new GameCommand(CommandKind.AdvanceBoundary))).Status);
        Bytes(before, f.Session.ExportCheckpoint());
    }
    private static void ImpossibleQuotaIsCorrupt()
    {
        var f = Ready(Catalog(multi: true)); f.FirstScene(); var state = f.State();
        state.Scheduler.Deck = Enumerable.Repeat(state.Scheduler.LastScene, 4).ToList(); RejectBeforePublication(f, state);
    }
    private static void ImpossibleOrderIsCorrupt()
    {
        var f = Ready(Catalog(multi: true)); f.FirstScene(); var state = f.State();
        var first = state.Scheduler.LastScene; var other = first == "coding" ? "meeting" : "coding";
        state.Scheduler.Deck = new List<string> { first, first, other, other }; RejectBeforePublication(f, state);
    }
    private static void GenerationOverflowIsCorrupt()
    {
        var f = Ready(); f.ToEndOfWork(); var state = f.State(); state.Scheduler.Generation = long.MaxValue;
        RejectBeforePublication(f, state);
    }
    private static void StaleSignatureIsCorrupt()
    {
        var f = Ready(Catalog(10, multi: true)); f.FirstScene(); Equal(CommandStatus.Committed, f.Step().Status);
        var state = f.State(); Equal("developer/v1/1", state.Scheduler.Signature);
        state.Scheduler.Signature = "developer/v1/0"; RejectBeforePublication(f, state);
    }
    private static void NonCareerHistoryCannotBeRemoved()
    {
        var f = Ready(); f.FirstScene(); var state = f.State();
        True(state.ConsumedActivities.Remove("run/activity/2026-09-07/0")); RejectBeforePublication(f, state);
    }
    private static void PastActivityCannotBeForged()
    {
        var f = Ready(); f.FirstScene(); var state = f.State();
        state.CurrentActivity = "run/activity/2026-09-07/479"; state.ConsumedActivities.Add(state.CurrentActivity);
        RejectBeforePublication(f, state);
    }
    private static void EqualCountsCannotHideForgery()
    {
        var f = Ready(); f.FirstScene(); var state = f.State();
        var index = state.ConsumedActivities.IndexOf("run/activity/2026-09-07/0"); True(index >= 0);
        state.ConsumedActivities[index] = "run/activity/2026-09-07/479"; state.CurrentActivity = state.ConsumedActivities[index];
        Equal(state.Receipts.Count(x => x.Payload == new GameCommand(CommandKind.AdvanceBoundary).CanonicalPayload), state.ConsumedActivities.Count);
        RejectBeforePublication(f, state);
    }
    private static void EntitySequenceIsGlobal()
    {
        var f = Ready(); var state = f.State();
        state.Employment!.EmployerId = "run/employer/" + state.Employment.InstanceId.Split('/').Last(); RejectBeforePublication(f, state);
    }
    private static void LedgerReferenceCannotExceedCounter()
    {
        var f = Ready(); f.ToEndOfWork();
        foreach (var category in new[] { "arrear.settlement", "external.label" })
        {
            var state = f.State(); state.Ledger[0].Category = category; state.Ledger[0].AttributionId = "run/arrear/9999";
            RejectBeforePublication(f, state);
        }
    }
    private static void ReceiptHistoryIsComplete()
    {
        var f = Ready(); f.FirstScene(); var missing = f.State(); missing.Receipts.RemoveAt(2); RejectBeforePublication(f, missing);
        var swapped = f.State(); (swapped.Receipts[2], swapped.Receipts[3]) = (swapped.Receipts[3], swapped.Receipts[2]);
        RejectBeforePublication(f, swapped);
    }
    private static void MalformedPayloadIsCorrupt()
    {
        var f = Ready(); f.FirstScene(); var state = f.State(); state.Receipts.Last().Payload = "2:0:0:0:00";
        RejectBeforePublication(f, state);
    }
    private static void TimelineCorruptionBeforeCompatibility()
    {
        var f = Ready(); f.FirstScene(); var state = f.State();
        True(state.ConsumedActivities.Remove("run/activity/2026-09-07/0")); state.Employment!.DefinitionRevision = "unavailable";
        RejectBeforePublication(f, state);
        var missingBackground = new JsonSaveSerializer(Catalog(missingStart: true), GameSession.CreateRestoreValidator());
        Equal(LoadStatus.Corrupt, Raw(missingBackground, state).Status);
    }
    private static void SchedulerCorruptionBeforeUnrelatedCompatibility()
    {
        var f = Ready(Catalog(multi: true)); f.FirstScene(); var state = f.State();
        state.Scheduler.Deck = Enumerable.Repeat(state.Scheduler.LastScene, 4).ToList();
        var serializer = new JsonSaveSerializer(Catalog(multi: true, missingStart: true), GameSession.CreateRestoreValidator());
        Equal(LoadStatus.Corrupt, Raw(serializer, state).Status);
    }
    private static void ValidMissingBackgroundIsUnsupported()
    {
        var f = Ready(Catalog(multi: true)); f.FirstScene();
        var serializer = new JsonSaveSerializer(Catalog(multi: true, missingStart: true), GameSession.CreateRestoreValidator());
        Equal(LoadStatus.UnsupportedContent, Raw(serializer, f.State()).Status);
    }
    private static void MissingSchedulerSceneIsUnsupported()
    {
        var f = Ready(); f.FirstScene(); var state = f.State();
        var serializer = new JsonSaveSerializer(Catalog(sceneId: "meeting"), GameSession.CreateRestoreValidator());
        Equal(LoadStatus.UnsupportedContent, Raw(serializer, state).Status);
    }
    private static void MultiSceneTransitionsRemainDeterministic()
    {
        var f = Ready(Catalog(10, multi: true)); f.FirstScene(); Equal("developer/v1/0", f.State().Scheduler.Signature);
        AssertContinuation(f); Equal(CommandStatus.Committed, f.Step().Status); Equal("developer/v1/1", f.State().Scheduler.Signature);
        AssertContinuation(f); f.ToEndOfWork(); AssertContinuation(f); f.ToNextWorkdayFirstScene(); AssertContinuation(f);
    }
    private static Fixture SwitchedCareer()
    {
        var f = Ready(TwoCareers()); f.FirstScene();
        Equal(CommandStatus.Committed, f.Execute("resign", new GameCommand(CommandKind.Resign)).Status);
        Equal(CommandStatus.Committed, f.Execute("new-job", new GameCommand(CommandKind.AcceptJob, "designer")).Status);
        AssertContinuation(f); f.ToNextWorkdayFirstScene(); return f;
    }
    private static void SubsequentEmploymentRemainsValid() { var f = SwitchedCareer(); AssertContinuation(f); }
    private static void MissingHistoricalSceneIsUnsupported()
    {
        var f = SwitchedCareer(); var state = f.State(); True(!state.Scheduler.Deck.Contains("coding"));
        var serializer = new JsonSaveSerializer(TwoCareers(removeCoding: true), GameSession.CreateRestoreValidator());
        Equal(LoadStatus.UnsupportedContent, Raw(serializer, state).Status);
    }
    private static void ForeignHistoricalSceneIsCorrupt()
    {
        var f = SwitchedCareer(); var state = f.State();
        var serializer = new JsonSaveSerializer(TwoCareers(removeCoding: true, foreignCoding: true), GameSession.CreateRestoreValidator());
        Equal(LoadStatus.Corrupt, Raw(serializer, state).Status);
    }
    private static void SerializerRequiresVerifier()
    {
        try { _ = new JsonSaveSerializer(Catalog(), null!); }
        catch (ArgumentNullException) { return; }
        throw new Exception("Serializer accepted no restore verifier");
    }
    private static void CoordinatedRewindIsCorrupt()
    {
        var f = Ready(); f.ToEndOfWork(); var state = f.State();
        state.Receipts.RemoveAt(state.Receipts.Count - 1); state.Revision--; state.NextOperation--;
        True(state.ConsumedActivities.Remove("run/activity/2026-09-07/900"));
        state.Minute = 900; state.Employment!.ScenesToday = 3;
        state.CurrentActivity = state.ConsumedActivities.Last(); state.Scheduler.Cursor = 3; state.Scheduler.LastScene = state.Scheduler.Deck[2];
        // The retained salary ledger points at a surviving operation, so structural receipt checks alone cannot catch this.
        state.Ledger.Last().OperationId = state.Receipts.Last().OperationId;
        RejectBeforePublication(f, state);
    }
    private static void ForgedRewardsAreCorrupt()
    {
        var f = Ready(); f.ToEndOfWork();
        foreach (Action<GameState> forge in new Action<GameState>[] {
            s => s.Employment!.Xp++, s => s.Skills[0].Exposure++,
            s => s.Claims[0].Numerator = "99999999", s => s.Ledger.Last().Amount++ })
        {
            var state = f.State(); forge(state); RejectBeforePublication(f, state);
        }
    }
    private static void SchedulerTraceFieldsAreCorrupt()
    {
        var f = Ready(Catalog(multi: true)); f.FirstScene();
        var rng = f.State(); rng.SchedulerRng = rng.SchedulerRng == 1 ? 2u : 1u; RejectBeforePublication(f, rng);
        var generation = f.State(); generation.Scheduler.Generation++;
        True(generation.Scheduler.Generation <= generation.ConsumedActivities.Count); RejectBeforePublication(f, generation);
    }
    private static void CharacterCreationProvenanceIsBound()
    {
        var f = new Fixture(CharacterCatalog());
        Equal(CommandStatus.Committed, f.Execute("create", new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female")).Status);
        var forgedReceipt = f.State();
        forgedReceipt.Receipts[0].Payload = new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "Forged", "base.female").CanonicalPayload;
        RejectBeforePublication(f, forgedReceipt);

        var missingName = f.State(); missingName.Name = "";
        RejectBeforePublication(f, missingName);

        var wrongAge = f.State(); wrongAge.StartingAge = 26;
        RejectBeforePublication(f, wrongAge);

        var wrongBirth = f.State(); wrongBirth.BirthDateIso = "2000-09-07";
        RejectBeforePublication(f, wrongBirth);
    }

    private static void LearningSpeedProvenanceIsBound()
    {
        var f = new Fixture(CharacterCatalog());
        Equal(CommandStatus.Committed, f.Execute("create", new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female")).Status);
        Equal(CommandStatus.Committed, f.Step().Status);
        Equal(480, f.State().Minute);
        Equal(CommandStatus.Committed, f.Execute("course", new GameCommand(CommandKind.PurchaseCourse, "communication-basics")).Status);

        var checkpoint = f.Session.ExportCheckpoint();
        var store = new MemoryStore(f.Serializer, checkpoint);
        True(GameSession.TryRestore(f.Catalog, f.Serializer, store, out var restored, out var result));
        Equal(LoadStatus.Valid, result.Status);
        Equal(CommandStatus.Committed, restored!.Execute(new CommandEnvelope("run", "study-valid", restored.Snapshot().Revision,
            new GameCommand(CommandKind.Study, amount: 60))).Status);

        var tampered = f.State(); tampered.LearningSpeed = 20000;
        RejectBeforePublication(f, tampered);
    }

    private static void CashProvenanceIsBound()
    {
        var f = new Fixture(CharacterCatalog(cash: 9500));
        Equal(CommandStatus.Committed, f.Execute("create", new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female")).Status);
        var checkpoint = f.Session.ExportCheckpoint();
        var store = new MemoryStore(f.Serializer, checkpoint);
        True(GameSession.TryRestore(f.Catalog, f.Serializer, store, out var restored, out var result));
        Equal(LoadStatus.Valid, result.Status);
        var rejected = restored!.Execute(new CommandEnvelope("run", "too-expensive", restored.Snapshot().Revision,
            new GameCommand(CommandKind.PurchaseCourse, "communication-basics")));
        Equal(CommandStatus.Rejected, rejected.Status); Equal("economy.insufficient_cash", rejected.ReasonKey);

        var tampered = f.State(); tampered.Cash = 100000;
        RejectBeforePublication(f, tampered);
    }

    private static void PostResignationBoundaryRemainsValid()
    {
        var f = new Fixture(MidShiftResignationCatalog());
        Equal(CommandStatus.Committed, f.Execute("create", new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female")).Status);
        Equal(CommandStatus.Committed, f.Execute("job", new GameCommand(CommandKind.AcceptJob, "developer")).Status);
        while (f.State().Minute < 780) Equal(CommandStatus.Committed, f.Step().Status);
        Equal(780, f.State().Minute); Equal(1, f.State().Scheduler.Cursor);
        Equal(CommandStatus.Committed, f.Execute("resign-mid-shift", new GameCommand(CommandKind.Resign)).Status);
        Equal(LoadStatus.Valid, Raw(f.Serializer, f.State()).Status);

        var advance = f.Execute("unemployed-evening", new GameCommand(CommandKind.AdvanceBoundary));
        Equal(CommandStatus.Committed, advance.Status);
        var state = f.State();
        Equal(1320, state.Minute); Equal(1, state.Scheduler.Cursor); Equal(1, state.PreviousEmployment.Count);
        Equal(LoadStatus.Valid, Raw(f.Serializer, state).Status);
        AssertContinuation(f);
    }

    private static void RejectBeforePublication(Fixture fixture, GameState state)
    {
        var before = fixture.Session.ExportCheckpoint();
        var bytes = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(state), 1, state.Revision);
        Equal(LoadStatus.Corrupt, fixture.Serializer.DeserializeAndValidate(bytes).Status);
        var store = new MemoryStore(fixture.Serializer, bytes);
        True(!GameSession.TryRestore(fixture.Catalog, fixture.Serializer, store, out var restored, out var result));
        True(restored == null); Equal(LoadStatus.Corrupt, result.Status); Bytes(before, fixture.Session.ExportCheckpoint());
    }
    private static void AssertContinuation(Fixture fixture)
    {
        var bytes = fixture.Session.ExportCheckpoint(); var state = fixture.State();
        var aStore = new MemoryStore(fixture.Serializer, bytes); var bStore = new MemoryStore(fixture.Serializer, bytes);
        True(GameSession.TryRestore(fixture.Catalog, fixture.Serializer, aStore, out var a, out _));
        True(GameSession.TryRestore(fixture.Catalog, fixture.Serializer, bStore, out var b, out _));
        var command = new CommandEnvelope("run", "restored-next", state.Revision, new GameCommand(CommandKind.AdvanceBoundary));
        Equal(CommandStatus.Committed, a!.Execute(command).Status); Equal(CommandStatus.Committed, b!.Execute(command).Status);
        Bytes(a.ExportCheckpoint(), b.ExportCheckpoint());
        var checkpoint = a.ExportCheckpoint(); Equal(CommandStatus.AlreadyCommitted, a.Execute(command).Status); Bytes(checkpoint, a.ExportCheckpoint());
    }

    private static LoadResult Raw(JsonSaveSerializer serializer, GameState state) =>
        serializer.DeserializeAndValidate(JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(state), SaveSchema.CurrentVersion, state.Revision));

    private static ContentCatalog Catalog(long promotionXp = 1000, bool multi = false, bool missingStart = false, string sceneId = "coding")
    {
        var skill = new SkillDefinition("communication", "skill.communication", 100, 300, 600, 1000, 1500);
        var scenes = multi ? new[] { new CareerSceneDefinition("coding", "scene.coding", 50, 10, "communication", 1),
            new CareerSceneDefinition("meeting", "scene.meeting", 50, 20, "communication", 4) } :
            new[] { new CareerSceneDefinition(sceneId, "scene." + sceneId, 100, 10, "communication", 1) };
        var ranks = new[]
        {
            new CareerRankDefinition("junior", 0, 0, 10000000, "communication", 0),
            new CareerRankDefinition("mid", promotionXp, 0, 15000000, "communication", 1)
        };
        var career = new CareerDefinition("developer", "v1", "career.developer", 540, 1020, 4, 4,
            new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            scenes, ranks);
        return new ContentCatalog("fixture.v1", new[] { skill }, new[] { career }, Array.Empty<CourseDefinition>(),
            missingStart ? Array.Empty<CharacterStartDefinition>() : new[] { new CharacterStartDefinition("fresh", 3000000, 10000, "base.female") },
            new EconomyBalanceDefinition(1, 1, 0), new DayScheduleDefinition(480, 1320));
    }
    private static ContentCatalog CharacterCatalog(long cash = 3000000, int speed = 10000)
    {
        var basis = Catalog();
        var course = new CourseDefinition("communication-basics", "course.communication", "communication", 1, 0, 180, 20000);
        return new ContentCatalog(basis.Version, basis.Skills.Values, basis.Careers.Values, new[] { course },
            new[] { new CharacterStartDefinition("fresh", cash, speed, "base.female") }, basis.Economy, basis.Schedule);
    }

    private static ContentCatalog MidShiftResignationCatalog()
    {
        var skill = new SkillDefinition("communication", "skill.communication", 100, 300, 600, 1000, 1500);
        var scene = new CareerSceneDefinition("coding", "scene.coding", 100, 10, "communication", 1);
        var rank = new CareerRankDefinition("junior", 0, 0, 10000000, "communication", 0);
        var career = new CareerDefinition("developer", "v1", "career.developer", 540, 1020, 2, 10,
            new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            new[] { scene }, new[] { rank });
        return new ContentCatalog("fixture.v1", new[] { skill }, new[] { career }, Array.Empty<CourseDefinition>(),
            new[] { new CharacterStartDefinition("fresh", 3000000, 10000, "base.female") },
            new EconomyBalanceDefinition(1, 1, 0), new DayScheduleDefinition(480, 1320));
    }

    private static ContentCatalog TwoCareers(bool removeCoding = false, bool foreignCoding = false)
    {
        var basis = Catalog(sceneId: removeCoding ? "meeting" : "coding"); var developer = basis.Careers["developer"];
        var designer = new CareerDefinition("designer", "v1", "career.designer", 540, 1020, 4, 4, developer.WorkDays,
            new[] { new CareerSceneDefinition("design", "scene.design", 100, 10, "communication", 1) }, developer.Ranks);
        var careers = new List<CareerDefinition> { developer, designer };
        if (foreignCoding) careers.Add(new CareerDefinition("foreign", "v1", "career.foreign", 540, 1020, 4, 4, developer.WorkDays,
            new[] { new CareerSceneDefinition("coding", "scene.coding", 100, 10, "communication", 1) }, developer.Ranks));
        return new ContentCatalog(basis.Version, basis.Skills.Values, careers, basis.Courses.Values, basis.Starts.Values, basis.Economy, basis.Schedule);
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
            Serializer = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator());
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
        public void FirstScene() { while (Session.Snapshot().Instant.Minute < 660) Equal(CommandStatus.Committed, Step().Status); }

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
