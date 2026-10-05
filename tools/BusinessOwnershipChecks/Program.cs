#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using StartupLife.Application;
using StartupLife.Core;
using StartupLife.Infrastructure;
using StartupLife.Simulation;

internal static class Program
{
    private static int passed;
    private static readonly List<object> results = new List<object>();

    private static int Main(string[] args)
    {
        Check("M8 definitions, enums, defensive copies, and catalog compatibility", DefinitionContracts);
        Check("M8 command ordinals and historical canonical payloads are frozen", CommandWireCompatibility);
        Check("M8 launch acceptance matrix", LaunchCases);
        Check("M8 reinvest acceptance matrix", ReinvestCases);
        Check("M8 pricing acceptance matrix", PricingCases);
        Check("M8 closure acceptance matrix", ClosureCases);
        Check("M8 retry, stale, identity conflict, and rejected-byte invariants", RetryAndHostileCases);
        Check("M8 conservation, ledger attribution, entity order, and read model", ConservationAndReadModel);
        Check("M8 save v1/v0 migration and v2 roundtrip matrix", MigrationCases);
        Check("M8 H1 v1 schema-preserving normalization and first v2 commit", H1CompatibilityCases);
        Check("F1 frozen v1 rejects whitespace and escaped Businesses members", FrozenV1MemberDetection);
        Check("F1 H1 restore preserves bytes when frozen v1 wire is impossible", FrozenV1H1BytesPreserved);
        Check("F2 unavailable business content cannot mask known business corruption", BusinessContentCompatibilityPrecedence);
        Check("F3 intrinsic business history provenance survives unrelated missing content", BusinessHistoryProvenanceWithoutUnrelatedContent);
        Check("M8 restore intrinsically validates business command payloads before content compatibility", IntrinsicBusinessCommandPayloadPrecedesContentCompatibility);
        Check("M8 intrinsically corrupt business primary recovers valid backup", IntrinsicBusinessCorruptionRecoversBackup);
        Check("M8 malformed frozen v1 is contained and recovers valid backup", MalformedFrozenV1IsContainedAndRecoversBackup);
        Check("F4 ordinary commits require physical current schema", OrdinaryCommitSchemaBoundary);
        Check("F5 migration source and intermediate stages validate before the next step", MigrationStageValidation);
        Check("F8 LaunchBusiness retry matrix returns original committed semantics", LaunchRetryMatrix);
        Check("F8 ReinvestBusiness retry matrix returns original committed semantics", ReinvestRetryMatrix);
        Check("F8 SetBusinessPricing retry matrix returns original committed semantics", PricingRetryMatrix);
        Check("F8 CloseBusiness retry matrix returns original committed semantics", CloseRetryMatrix);
        Check("F8 business command IDs preserve hostile ownership rules", BusinessCommandOwnershipHostility);
        Check("M8 restore rejects corrupt business state before content compatibility", RestoreBusinessCorruption);
        Check("M8 unavailable business revision is unsupported content", UnsupportedBusinessRevision);

        var reportPath = args.Length > 0 ? Path.GetFullPath(args[0]) :
            Path.Combine(Path.GetTempPath(), "startup-life-m8-business-ownership.json");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(new
        {
            suite = "Startup Life M8-T01 business ownership and Save v2 — .NET, not Unity",
            passed,
            failed = results.Count - passed,
            tests = results
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"{passed}/{results.Count} passed. Report: {reportPath}");
        return passed == results.Count ? 0 : 1;
    }

    private static void DefinitionContracts()
    {
        Equal(1, (int)BusinessType.OnlineStore); Equal(2, (int)BusinessType.HomeFoodPreorder);
        Equal(3, (int)BusinessType.FreelanceService); Equal(4, (int)BusinessType.CoffeeKiosk);
        Equal(1, (int)BusinessOperationMode.SideHustleCompatible); Equal(2, (int)BusinessOperationMode.FullTimeRequired);
        Equal(3, (int)BusinessOperationMode.ManagerOperable);
        Equal(1, (int)PricingPosture.Budget); Equal(2, (int)PricingPosture.Standard); Equal(3, (int)PricingPosture.Premium);

        var source = new[] { PricingPosture.Budget, PricingPosture.Standard, PricingPosture.Premium };
        var definition = new BusinessDefinition("test.business.freelance", "fixture-r1", "test.business.freelance.name",
            BusinessType.FreelanceService, BusinessOperationMode.SideHustleCompatible, 100, 1000, source,
            PricingPosture.Standard, 1, 1000);
        source[0] = PricingPosture.Premium;
        Equal(PricingPosture.Budget, definition.AllowedPricingPostures[0]);
        Throws<NotSupportedException>(() => ((IList<PricingPosture>)definition.AllowedPricingPostures).Add(PricingPosture.Budget));
        Throws<ArgumentException>(() => new BusinessDefinition("test.business.bad", "r", "key", BusinessType.OnlineStore,
            BusinessOperationMode.SideHustleCompatible, 1, (long)int.MaxValue + 1, new[] { PricingPosture.Standard },
            PricingPosture.Standard, 1, 1));
        Throws<ArgumentException>(() => new BusinessDefinition("test.business.bad", "r", "key", BusinessType.OnlineStore,
            BusinessOperationMode.SideHustleCompatible, 1, 1, new[] { PricingPosture.Standard, PricingPosture.Standard },
            PricingPosture.Standard, 1, 1));

        var legacy = new ContentCatalog("legacy", Array.Empty<SkillDefinition>(), Array.Empty<CareerDefinition>(),
            Array.Empty<CourseDefinition>(), new[] { new CharacterStartDefinition("fresh", 1000, 10000, "base.female") },
            new EconomyBalanceDefinition(1, 1, 0), new DayScheduleDefinition(480, 1320));
        Equal(0, legacy.Businesses.Count);
    }

    private static void CommandWireCompatibility()
    {
        Equal(0, (int)CommandKind.CreateCharacter); Equal(1, (int)CommandKind.AcceptJob);
        Equal(2, (int)CommandKind.AdvanceBoundary); Equal(3, (int)CommandKind.PurchaseCourse);
        Equal(4, (int)CommandKind.Study); Equal(5, (int)CommandKind.Resign);
        Equal(6, (int)CommandKind.AcknowledgePlayback); Equal(7, (int)CommandKind.LaunchBusiness);
        Equal(8, (int)CommandKind.ReinvestBusiness); Equal(9, (int)CommandKind.SetBusinessPricing);
        Equal(10, (int)CommandKind.CloseBusiness);

        Equal("0:5:fresh2:An11:base.female25", new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female").CanonicalPayload);
        Equal("1:9:developer0:0:0", new GameCommand(CommandKind.AcceptJob, "developer").CanonicalPayload);
        Equal("2:0:0:0:0", new GameCommand(CommandKind.AdvanceBoundary).CanonicalPayload);
        Equal("3:6:course0:0:0", new GameCommand(CommandKind.PurchaseCourse, "course").CanonicalPayload);
        Equal("4:0:0:0:60", new GameCommand(CommandKind.Study, amount: 60).CanonicalPayload);
        Equal("5:0:0:0:0", new GameCommand(CommandKind.Resign).CanonicalPayload);
        Equal("6:8:activity0:0:12", new GameCommand(CommandKind.AcknowledgePlayback, "activity", 12).CanonicalPayload);
        Equal("7:23:test.business.freelance0:10:fixture-r1100",
            BusinessCommands.Launch("test.business.freelance", "fixture-r1", 100).CanonicalPayload);
        Equal("", new GameCommand(CommandKind.CloseBusiness, null!, 0, null!, null!).ContentId);
    }

    private static void LaunchCases()
    {
        var f = Ready(cash: 1000);
        var valid = f.Execute("launch", BusinessCommands.Launch(Freelance.Id, Freelance.Revision, 100));
        Equal(CommandStatus.Committed, valid.Status);
        Equal(900L, f.State().Cash);
        Equal(1, f.State().Businesses.Count);
        Equal(PricingPosture.Standard, f.State().Businesses[0].PricingPosture);

        var exact = Ready(cash: 100);
        Equal(CommandStatus.Committed, exact.Execute("launch", BusinessCommands.Launch(Freelance.Id, Freelance.Revision, 100)).Status);
        Equal(0L, exact.State().Cash);

        ExpectRejectedUnchanged(Ready(cash: 99), BusinessCommands.Launch(Freelance.Id, Freelance.Revision, 100), "economy.insufficient_cash");
        ExpectRejectedUnchanged(Ready(cash: 1000), BusinessCommands.Launch(Freelance.Id, Freelance.Revision, 99), "business.invalid_investment");
        ExpectRejectedUnchanged(Ready(cash: 2000), BusinessCommands.Launch(Freelance.Id, Freelance.Revision, 1001), "business.invalid_investment");
        ExpectRejectedUnchanged(Ready(cash: 1000), new GameCommand(CommandKind.LaunchBusiness, Freelance.Id, 100, "unused", Freelance.Revision), "business.invalid_payload");
        ExpectRejectedUnchanged(Ready(cash: 1000), BusinessCommands.Launch(Freelance.Id, "wrong-r", 100), "business.definition_revision");
        ExpectRejectedUnchanged(Ready(cash: 1000), BusinessCommands.Launch("test.business.missing", "r1", 100), "content.missing");

        var duplicate = Ready(cash: 3000, businesses: new[] { Freelance, FreelanceAlt });
        Equal(CommandStatus.Committed, duplicate.Execute("one", BusinessCommands.Launch(Freelance.Id, Freelance.Revision, 100)).Status);
        ExpectRejectedUnchanged(duplicate, BusinessCommands.Launch(Freelance.Id, Freelance.Revision, 100), "business.type_active");
        ExpectRejectedUnchanged(duplicate, BusinessCommands.Launch(FreelanceAlt.Id, FreelanceAlt.Revision, 100), "business.type_active");

        var four = Ready(cash: 5000, businesses: FourSideHustles.Concat(new[] { FifthDifferentDefinition }));
        for (var i = 0; i < 4; i++)
            Equal(CommandStatus.Committed, four.Execute("launch-" + i, BusinessCommands.Launch(FourSideHustles[i].Id, FourSideHustles[i].Revision, 100)).Status);
        ExpectRejectedUnchanged(four, BusinessCommands.Launch(FifthDifferentDefinition.Id, FifthDifferentDefinition.Revision, 100), "business.active_limit");

        var manager = Ready(cash: 1000, businesses: new[] { ManagerDefinition });
        ExpectRejectedUnchanged(manager, BusinessCommands.Launch(ManagerDefinition.Id, ManagerDefinition.Revision, 100), "business.manager_unsupported");

        var employed = Ready(cash: 1000, businesses: new[] { FullTimeDefinition }, includeCareer: true);
        Equal(CommandStatus.Committed, employed.Execute("job", new GameCommand(CommandKind.AcceptJob, "developer")).Status);
        ExpectRejectedUnchanged(employed, BusinessCommands.Launch(FullTimeDefinition.Id, FullTimeDefinition.Revision, 100), "business.employment_incompatible");

        var arrearsState = ReadyArrearsState(Freelance);
        var engine = new SimulationEngine(arrearsState.catalog);
        // SimulationEngine clears the detached candidate cue before rule evaluation; Application
        // never publishes that candidate on rejection. Normalize the cue here so this direct-engine
        // assertion measures the M8 state/counter preflight rather than generic cue preparation.
        arrearsState.state.CurrentCue = "";
        var before = JsonSaveSerializer.WriteObject(arrearsState.state);
        ThrowsRule("economy.arrears", () => engine.Evaluate(arrearsState.state, BusinessCommands.Launch(Freelance.Id, Freelance.Revision, 100), "run/op/2"));
        Bytes(before, JsonSaveSerializer.WriteObject(arrearsState.state));
    }

    private static void ReinvestCases()
    {
        var f = Ready(cash: 2500); Launch(f, 100);
        var id = f.State().Businesses.Single().InstanceId;
        Equal(CommandStatus.Committed, f.Execute("reinvest", BusinessCommands.Reinvest(id, 10)).Status);
        Equal(10L, f.State().Businesses.Single().ReinvestedAmount);
        Equal(2390L, f.State().Cash);

        var lowCash = Ready(cash: 100); Launch(lowCash, 100);
        ExpectRejectedUnchanged(lowCash, BusinessCommands.Reinvest(lowCash.State().Businesses.Single().InstanceId, 1), "economy.insufficient_cash");

        var closed = Ready(cash: 500); Launch(closed, 100); var closedId = closed.State().Businesses.Single().InstanceId;
        Equal(CommandStatus.Committed, closed.Execute("close", BusinessCommands.Close(closedId)).Status);
        ExpectRejectedUnchanged(closed, BusinessCommands.Reinvest(closedId, 1), "business.closed");

        ExpectRejectedUnchanged(Ready(cash: 1000), BusinessCommands.Reinvest("run/business/999", 1), "business.not_found");
        var range = Ready(cash: 2000); Launch(range, 100); var rangeId = range.State().Businesses.Single().InstanceId;
        foreach (var amount in new[] { 0, -1 })
            ExpectRejectedUnchanged(range, BusinessCommands.Reinvest(rangeId, amount), "business.invalid_reinvestment");

        var narrow = Definition("test.business.narrow", BusinessType.FreelanceService, BusinessOperationMode.SideHustleCompatible, 100, 1000, 10, 20);
        var nf = Ready(cash: 3000, businesses: new[] { narrow }); Launch(nf, 100, narrow);
        ExpectRejectedUnchanged(nf, BusinessCommands.Reinvest(nf.State().Businesses.Single().InstanceId, 9), "business.invalid_reinvestment");
        ExpectRejectedUnchanged(nf, BusinessCommands.Reinvest(nf.State().Businesses.Single().InstanceId, 21), "business.invalid_reinvestment");

        var arrears = ReadyArrearsState(Freelance, includeBusiness: true);
        var engine = new SimulationEngine(arrears.catalog);
        ThrowsRule("economy.arrears", () => engine.Evaluate(arrears.state, BusinessCommands.Reinvest(arrears.state.Businesses.Single().InstanceId, 1), "run/op/3"));

        var overflowCatalog = BusinessCatalog(long.MaxValue, new[] { Freelance });
        var cumulative = CharacterState(overflowCatalog, long.MaxValue);
        cumulative.Businesses.Add(new BusinessState
        {
            InstanceId = cumulative.NewEntity("business"), DefinitionId = Freelance.Id, DefinitionRevision = Freelance.Revision,
            OpenedIso = cumulative.DateIso, OpenedMinute = cumulative.Minute, PricingPosture = PricingPosture.Standard,
            InitialInvestment = 100, ReinvestedAmount = long.MaxValue - 500
        });
        cumulative.CurrentCue = "";
        var cumulativeBefore = JsonSaveSerializer.WriteObject(cumulative);
        Throws<OverflowException>(() => new SimulationEngine(overflowCatalog).Evaluate(cumulative,
            BusinessCommands.Reinvest(cumulative.Businesses.Single().InstanceId, 1000), "run/op/2"));
        Bytes(cumulativeBefore, JsonSaveSerializer.WriteObject(cumulative));

        var total = CharacterState(overflowCatalog, long.MaxValue);
        total.Businesses.Add(new BusinessState
        {
            InstanceId = total.NewEntity("business"), DefinitionId = Freelance.Id, DefinitionRevision = Freelance.Revision,
            OpenedIso = total.DateIso, OpenedMinute = total.Minute, PricingPosture = PricingPosture.Standard,
            InitialInvestment = 1000, ReinvestedAmount = long.MaxValue - 1000
        });
        total.CurrentCue = "";
        var totalBefore = JsonSaveSerializer.WriteObject(total);
        Throws<OverflowException>(() => new SimulationEngine(overflowCatalog).Evaluate(total,
            BusinessCommands.Reinvest(total.Businesses.Single().InstanceId, 1), "run/op/2"));
        Bytes(totalBefore, JsonSaveSerializer.WriteObject(total));
    }

    private static void PricingCases()
    {
        var f = Ready(cash: 1000); Launch(f, 100); var id = f.State().Businesses.Single().InstanceId;
        var cash = f.State().Cash; var ledger = f.State().Ledger.Count;
        Equal(CommandStatus.Committed, f.Execute("premium", BusinessCommands.SetPricing(id, PricingPosture.Premium)).Status);
        Equal(PricingPosture.Premium, f.State().Businesses.Single().PricingPosture);
        Equal(cash, f.State().Cash); Equal(ledger, f.State().Ledger.Count);

        ExpectRejectedUnchanged(f, new GameCommand(CommandKind.SetBusinessPricing, id, 99), "business.invalid_payload");
        ExpectRejectedUnchanged(f, BusinessCommands.SetPricing(id, PricingPosture.Premium), "business.pricing_unchanged");
        ExpectRejectedUnchanged(Ready(cash: 1000), BusinessCommands.SetPricing("run/business/999", PricingPosture.Budget), "business.not_found");

        var onlyStandard = Definition("test.business.standard", BusinessType.FreelanceService,
            BusinessOperationMode.SideHustleCompatible, 100, 1000, 1, 1000, new[] { PricingPosture.Standard });
        var disallowed = Ready(cash: 1000, businesses: new[] { onlyStandard }); Launch(disallowed, 100, onlyStandard);
        ExpectRejectedUnchanged(disallowed, BusinessCommands.SetPricing(disallowed.State().Businesses.Single().InstanceId, PricingPosture.Premium), "business.invalid_payload");

        var closed = Ready(cash: 1000); Launch(closed, 100); var closedId = closed.State().Businesses.Single().InstanceId;
        closed.Execute("close", BusinessCommands.Close(closedId));
        ExpectRejectedUnchanged(closed, BusinessCommands.SetPricing(closedId, PricingPosture.Budget), "business.closed");
    }

    private static void ClosureCases()
    {
        var f = Ready(cash: 1000); Launch(f, 100); var id = f.State().Businesses.Single().InstanceId;
        var ledger = f.State().Ledger.Select(x => x.Id).ToArray();
        var cash = f.State().Cash;
        Equal(CommandStatus.Committed, f.Execute("close", BusinessCommands.Close(id)).Status);
        True(!f.State().Businesses.Single().IsActive);
        Equal(cash, f.State().Cash);
        True(ledger.SequenceEqual(f.State().Ledger.Select(x => x.Id)));
        Equal("business.closed", f.State().CurrentCue);
        ExpectRejectedUnchanged(f, BusinessCommands.Close(id), "business.closed");
        ExpectRejectedUnchanged(Ready(cash: 1000), BusinessCommands.Close("run/business/999"), "business.not_found");

        Equal(CommandStatus.Committed, f.Execute("reopen", BusinessCommands.Launch(Freelance.Id, Freelance.Revision, 100)).Status);
        Equal(2, f.State().Businesses.Count);
        True(f.State().Businesses[0].InstanceId != f.State().Businesses[1].InstanceId);
        True(!f.State().Businesses[0].IsActive && f.State().Businesses[1].IsActive);

        var arrears = ReadyArrearsState(Freelance, includeBusiness: true);
        var engine = new SimulationEngine(arrears.catalog);
        var beforeCash = arrears.state.Cash; var beforeArrears = arrears.state.Arrears.Sum(x => x.Amount);
        Equal(0, engine.Evaluate(arrears.state, BusinessCommands.Close(arrears.state.Businesses.Single().InstanceId), "run/op/3"));
        Equal(beforeCash, arrears.state.Cash); Equal(beforeArrears, arrears.state.Arrears.Sum(x => x.Amount));
        True(!arrears.state.Businesses.Single().IsActive);
    }

    private static void RetryAndHostileCases()
    {
        var f = Ready(cash: 3000);
        var launchCommand = BusinessCommands.Launch(Freelance.Id, Freelance.Revision, 100);
        var launch = f.Execute("launch", launchCommand); Equal(CommandStatus.Committed, launch.Status);
        var afterLaunch = f.Checkpoint();
        Equal(CommandStatus.AlreadyCommitted, f.ExecuteAt("launch", launchCommand, 0).Status);
        Bytes(afterLaunch, f.Checkpoint());

        var restored = f.Restore();
        Equal(CommandStatus.AlreadyCommitted, restored.ExecuteAt("launch", launchCommand, 0).Status);
        var id = restored.State().Businesses.Single().InstanceId;
        var reinvestCommand = BusinessCommands.Reinvest(id, 10);
        Equal(CommandStatus.Committed, restored.Execute("reinvest", reinvestCommand).Status);
        var afterReinvest = restored.Checkpoint();
        Equal(CommandStatus.AlreadyCommitted, restored.ExecuteAt("reinvest", reinvestCommand, 1).Status);
        Bytes(afterReinvest, restored.Checkpoint());

        var priceCommand = BusinessCommands.SetPricing(id, PricingPosture.Premium);
        Equal(CommandStatus.Committed, restored.Execute("price", priceCommand).Status);
        var closeCommand = BusinessCommands.Close(id);
        Equal(CommandStatus.Committed, restored.Execute("close", closeCommand).Status);
        var later = restored.Execute("reopen", BusinessCommands.Launch(Freelance.Id, Freelance.Revision, 100));
        Equal(CommandStatus.Committed, later.Status);
        var afterLater = restored.Checkpoint();
        Equal(CommandStatus.AlreadyCommitted, restored.ExecuteAt("reinvest", reinvestCommand, 1).Status);
        Equal(CommandStatus.AlreadyCommitted, restored.ExecuteAt("price", priceCommand, 2).Status);
        Equal(CommandStatus.AlreadyCommitted, restored.ExecuteAt("close", closeCommand, 3).Status);
        Bytes(afterLater, restored.Checkpoint());

        var conflict = restored.Session.Execute(new CommandEnvelope("run", "launch", restored.Session.Snapshot().Revision,
            BusinessCommands.Launch(Freelance.Id, Freelance.Revision, 101)));
        Equal(CommandStatus.Rejected, conflict.Status); Equal("command.id_conflict", conflict.ReasonKey);
        var stale = restored.Session.Execute(new CommandEnvelope("run", "fresh-stale", 0,
            BusinessCommands.Launch(FreelanceAlt.Id, FreelanceAlt.Revision, 100)));
        Equal(CommandStatus.Rejected, stale.Status); Equal("command.stale_state", stale.ReasonKey);
        var wrongRun = restored.Session.Execute(new CommandEnvelope("other", "wrong-run", restored.Session.Snapshot().Revision,
            BusinessCommands.Close(restored.State().Businesses.Last().InstanceId)));
        Equal(CommandStatus.Rejected, wrongRun.Status); Equal("command.invalid_id", wrongRun.ReasonKey);
    }

    private static void ConservationAndReadModel()
    {
        var f = Ready(cash: 1000); Launch(f, 100);
        var state = f.State(); var business = state.Businesses.Single();
        var launchLedger = state.Ledger.Single(x => x.Category == "business.launch");
        Equal(-100L, launchLedger.CashDelta); Equal(100L, launchLedger.Amount); Equal(business.InstanceId, launchLedger.AttributionId);
        True(business.InstanceId.EndsWith("/business/1", StringComparison.Ordinal));
        True(launchLedger.Id.EndsWith("/transaction/2", StringComparison.Ordinal));
        Equal(900L, state.Cash);

        Equal(CommandStatus.Committed, f.Execute("reinvest", BusinessCommands.Reinvest(business.InstanceId, 25)).Status);
        state = f.State();
        var reinvest = state.Ledger.Single(x => x.Category == "business.reinvest");
        Equal(-25L, reinvest.CashDelta); Equal(25L, reinvest.Amount); Equal(business.InstanceId, reinvest.AttributionId);
        True(reinvest.Id.EndsWith("/transaction/3", StringComparison.Ordinal));
        Equal(875L, state.Cash); True(state.Cash >= 0);
        Equal(1000L, state.Cash - state.Ledger.Where(x => x.Category.StartsWith("business.", StringComparison.Ordinal)).Sum(x => x.CashDelta));

        var read = f.Session.ReadBusinesses();
        Equal(f.Session.Snapshot().Revision, read.Game.Revision);
        Equal(1, read.Businesses.Count); Equal(125L, read.Businesses[0].TotalInvestment);
        Equal("test.business.freelance.name", read.Businesses[0].NameKey);
        Throws<NotSupportedException>(() => ((IList<BusinessSnapshot>)read.Businesses).Clear());

        var before = f.Checkpoint(); var beforeEntity = state.NextEntity; var beforeOperation = state.NextOperation;
        var bad = f.Execute("bad-invest", BusinessCommands.Reinvest(business.InstanceId, 1001));
        Equal(CommandStatus.Rejected, bad.Status);
        Bytes(before, f.Checkpoint());
        state = f.State(); Equal(beforeEntity, state.NextEntity); Equal(beforeOperation, state.NextOperation);
    }

    private static void MigrationCases()
    {
        var catalog = MigrationCatalog();
        var serializer = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(),
            new SyntheticV0Migration(), new V1ToV2Migration());
        var v1 = FixtureBytes("current-v1.json");
        var v1Copy = (byte[])v1.Clone();
        var loadedV1 = serializer.DeserializeAndValidate(v1);
        Equal(LoadStatus.Valid, loadedV1.Status); Equal(SaveSchema.HistoricalV1, loadedV1.SourceSchemaVersion);
        Equal(SaveSchema.CurrentVersion, loadedV1.State!.SaveVersion); Equal(0, loadedV1.State.Businesses.Count);
        Bytes(v1Copy, v1);

        var v0 = FixtureBytes("synthetic-v0.json");
        var envelope = JsonSaveSerializer.ReadObject<SaveEnvelope>(v0);
        var raw = Convert.FromBase64String(envelope.PayloadBase64);
        var intermediate = new SyntheticV0Migration().Migrate(raw);
        var text = Encoding.UTF8.GetString(intermediate);
        True(text.Contains("\"SaveVersion\":1", StringComparison.Ordinal));
        True(!text.Contains("\"Businesses\"", StringComparison.Ordinal));
        var loadedV0 = serializer.DeserializeAndValidate(v0);
        Equal(LoadStatus.Valid, loadedV0.Status); Equal(0, loadedV0.SourceSchemaVersion);
        Equal(SaveSchema.CurrentVersion, loadedV0.State!.SaveVersion);
        Equal(loadedV1.State.Revision, loadedV0.State.Revision);
        Equal(loadedV1.State.Receipts.Count, loadedV0.State.Receipts.Count);

        var current = serializer.Serialize(loadedV1.State);
        var currentEnv = JsonSaveSerializer.ReadObject<SaveEnvelope>(current);
        Equal(SaveSchema.CurrentVersion, currentEnv.SchemaVersion);
        Equal(LoadStatus.Valid, serializer.DeserializeAndValidate(current).Status);

        var future = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(loadedV1.State), 3, loadedV1.State.Revision);
        var futureLoad = serializer.DeserializeAndValidate(future);
        Equal(LoadStatus.FutureVersion, futureLoad.Status); Equal("save.future_version", futureLoad.Reason);

        var mismatchState = Clone(loadedV1.State); mismatchState.SaveVersion = SaveSchema.HistoricalV1;
        var mismatch = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(mismatchState), SaveSchema.CurrentVersion, mismatchState.Revision);
        Equal("save.schema_mismatch", serializer.DeserializeAndValidate(mismatch).Reason);
        var generation = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(loadedV1.State), SaveSchema.CurrentVersion, loadedV1.State.Revision + 1);
        Equal("save.generation", serializer.DeserializeAndValidate(generation).Reason);

        var missing = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator());
        Equal("save.migration_missing", missing.DeserializeAndValidate(v1).Reason);
        Throws<ArgumentException>(() => new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(),
            new SyntheticV0Migration(), new SyntheticV0Migration()));
        Throws<ArgumentException>(() => new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(), new NonAdjacentMigration()));

        var nullBusinesses = Clone(loadedV1.State); nullBusinesses.Businesses = null!;
        var nullBytes = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(nullBusinesses), SaveSchema.CurrentVersion, nullBusinesses.Revision);
        Equal(LoadStatus.Corrupt, serializer.DeserializeAndValidate(nullBytes).Status);

        var invalid = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(),
            new SyntheticV0Migration(), new InvalidV1ToV2Migration());
        Equal("save.migration_invalid", invalid.DeserializeAndValidate(v1).Reason);
    }

    private static void H1CompatibilityCases()
    {
        var catalog = LegacyCatalog();
        var serializer = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(),
            new SyntheticV0Migration(), new V1ToV2Migration());
        var historical = FixtureBytes("pr6-day-completed.json");
        var originalEnv = JsonSaveSerializer.ReadObject<SaveEnvelope>(historical);
        var directory = Path.Combine(Path.GetTempPath(), "StartupLifeM8H1", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "save.json");
        File.WriteAllBytes(path, historical);
        var owners = new Dictionary<string, string>(StringComparer.Ordinal) { ["$batch/3:abc/"] = "abc" };
        True(GameSession.TryRestore(catalog, serializer, new AtomicFileSaveStore(path, serializer), out var session, out var load, owners));
        Equal(LoadStatus.Valid, load.Status);
        var physical = File.ReadAllBytes(path);
        var physicalEnv = JsonSaveSerializer.ReadObject<SaveEnvelope>(physical);
        Equal(SaveSchema.HistoricalV1, physicalEnv.SchemaVersion);
        Equal(originalEnv.Generation, physicalEnv.Generation);
        Equal(SaveSchema.HistoricalV1, JsonSaveSerializer.ReadObject<SaveEnvelope>(File.ReadAllBytes(path + ".backup")).SchemaVersion);
        Equal(SaveSchema.CurrentVersion, session!.ReadBusinesses().Game.Revision >= 0 ? serializer.DeserializeAndValidate(physical).State!.SaveVersion : -1);
        Equal(0, session.ReadBusinesses().Businesses.Count);

        True(GameSession.TryRestore(catalog, serializer, new AtomicFileSaveStore(path, serializer), out var cold, out var coldLoad));
        Equal(SaveSchema.HistoricalV1, coldLoad.SourceSchemaVersion);
        Equal(SaveSchema.CurrentVersion, coldLoad.State!.SaveVersion);
        var beforeRevision = cold!.Snapshot().Revision;
        var fresh = cold.Execute(new CommandEnvelope("run", "post-migration-resign", beforeRevision, new GameCommand(CommandKind.Resign)));
        Equal(CommandStatus.Committed, fresh.Status);
        var primaryAfter = JsonSaveSerializer.ReadObject<SaveEnvelope>(File.ReadAllBytes(path));
        var backupAfter = JsonSaveSerializer.ReadObject<SaveEnvelope>(File.ReadAllBytes(path + ".backup"));
        Equal(SaveSchema.CurrentVersion, primaryAfter.SchemaVersion);
        Equal(beforeRevision + 1, primaryAfter.Generation);
        Equal(SaveSchema.HistoricalV1, backupAfter.SchemaVersion);

        var mixedPath = Path.Combine(directory, "mixed.json");
        File.WriteAllBytes(mixedPath, physical);
        var logical = serializer.DeserializeAndValidate(physical).State!;
        File.WriteAllBytes(mixedPath + ".backup", serializer.Serialize(logical));
        var mixed = new AtomicFileSaveStore(mixedPath, serializer).Read();
        Equal(LoadStatus.Valid, mixed.Status);
        Equal("", mixed.Reason);

        var businessCatalog = BusinessCatalog(1000, new[] { Freelance });
        var businessSerializer = new JsonSaveSerializer(businessCatalog, GameSession.CreateRestoreValidator(),
            new SyntheticV0Migration(), new V1ToV2Migration());
        var businessFixture = Ready(cash: 1000); Launch(businessFixture, 100);
        var businessState = businessSerializer.DeserializeAndValidate(businessFixture.Checkpoint()).State!;
        Throws<ArgumentException>(() => ((ISaveCompatibilitySerializer)businessSerializer).SerializeForSchema(businessState, SaveSchema.HistoricalV1));
    }


    private static void FrozenV1MemberDetection()
    {
        var serializer = new JsonSaveSerializer(MigrationCatalog(), GameSession.CreateRestoreValidator(),
            new SyntheticV0Migration(), new V1ToV2Migration());
        var historical = FixtureBytes("current-v1.json");
        foreach (var member in new[]
        {
            "\"Businesses\" : [{\"DefinitionId\":\"future\"}]",
            "\"\\u0042usinesses\":[{\"DefinitionId\":\"future\"}]"
        })
        {
            var candidate = InjectRootMember(historical, member);
            var load = serializer.DeserializeAndValidate(candidate);
            Equal(LoadStatus.Corrupt, load.Status);
        }
    }

    private static void FrozenV1H1BytesPreserved()
    {
        var catalog = LegacyCatalog();
        var serializer = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(),
            new SyntheticV0Migration(), new V1ToV2Migration());
        foreach (var member in new[]
        {
            "\"Businesses\" : [{\"DefinitionId\":\"future\"}]",
            "\"\\u0042usinesses\":[{\"DefinitionId\":\"future\"}]"
        })
        {
            var impossible = InjectRootMember(FixtureBytes("pr6-day-completed.json"), member);
            var directory = Path.Combine(Path.GetTempPath(), "StartupLifeM8F1", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "save.json");
            File.WriteAllBytes(path, impossible);
            File.WriteAllBytes(path + ".backup", impossible);
            var store = new AtomicFileSaveStore(path, serializer);
            True(!GameSession.TryRestore(catalog, serializer, store, out var session, out var load,
                new Dictionary<string, string>(StringComparer.Ordinal) { ["$batch/3:abc/"] = "abc" }));
            Equal<GameSession?>(null, session);
            Equal(LoadStatus.Corrupt, load.Status);
            Bytes(impossible, File.ReadAllBytes(path));
            Bytes(impossible, File.ReadAllBytes(path + ".backup"));
        }
    }

    private static void BusinessContentCompatibilityPrecedence()
    {
        var a = Definition("test.business.a", BusinessType.FreelanceService);
        var b = Definition("test.business.b", BusinessType.OnlineStore);
        var strictB = Definition(b.Id, b.Type, startMin: 200, startMax: 1000);

        var active = Ready(cash: 5000, businesses: new[] { a, b });
        Launch(active, 100, a);
        Launch(active, 100, b);
        var checkpoint = active.Checkpoint();

        var missingA = new JsonSaveSerializer(BusinessCatalog(5000, new[] { strictB }), GameSession.CreateRestoreValidator(),
            new SyntheticV0Migration(), new V1ToV2Migration());
        Equal(LoadStatus.Corrupt, missingA.DeserializeAndValidate(checkpoint).Status);

        var incompatibleA = new BusinessDefinition(a.Id, "fixture-r2", a.NameKey, a.Type, a.OperationMode,
            a.MinimumStartupInvestment, a.MaximumStartupInvestment, a.AllowedPricingPostures,
            a.DefaultPricingPosture, a.MinimumReinvestment, a.MaximumReinvestment);
        var incompatible = new JsonSaveSerializer(BusinessCatalog(5000, new[] { incompatibleA, strictB }), GameSession.CreateRestoreValidator(),
            new SyntheticV0Migration(), new V1ToV2Migration());
        Equal(LoadStatus.Corrupt, incompatible.DeserializeAndValidate(checkpoint).Status);

        var missingAValidB = new JsonSaveSerializer(BusinessCatalog(5000, new[] { b }), GameSession.CreateRestoreValidator(),
            new SyntheticV0Migration(), new V1ToV2Migration());
        Equal(LoadStatus.UnsupportedContent, missingAValidB.DeserializeAndValidate(checkpoint).Status);

        var closed = Ready(cash: 5000, businesses: new[] { a, b });
        Launch(closed, 100, a);
        var aId = closed.State().Businesses.Single().InstanceId;
        Equal(CommandStatus.Committed, closed.Execute("close-a", BusinessCommands.Close(aId)).Status);
        Launch(closed, 100, b);
        Equal(LoadStatus.Corrupt, missingA.DeserializeAndValidate(closed.Checkpoint()).Status);
    }

    private static void BusinessHistoryProvenanceWithoutUnrelatedContent()
    {
        var missingStartCatalog = BusinessCatalogWithoutStart(new[] { Freelance });
        var missingStart = new JsonSaveSerializer(missingStartCatalog, GameSession.CreateRestoreValidator(),
            new SyntheticV0Migration(), new V1ToV2Migration());

        var reinvest = Ready(cash: 3000);
        Launch(reinvest, 100);
        var id = reinvest.State().Businesses.Single().InstanceId;
        Equal(CommandStatus.Committed, reinvest.Execute("reinvest-provenance", BusinessCommands.Reinvest(id, 10)).Status);
        var timestamp = reinvest.State();
        RewriteBusinessHistory(timestamp, "business.reinvested", record =>
            BusinessHistory.Encode(record.Action, record.OperationId, record.InstanceId, "2099-01-01",
                record.Minute, record.Amount, record.Pricing));
        Equal(LoadStatus.Corrupt, Raw(reinvest.Serializer, timestamp).Status);
        Equal(LoadStatus.Corrupt, Raw(missingStart, timestamp).Status);

        var pricing = Ready(cash: 3000);
        Launch(pricing, 100);
        id = pricing.State().Businesses.Single().InstanceId;
        Equal(CommandStatus.Committed, pricing.Execute("price-provenance",
            BusinessCommands.SetPricing(id, PricingPosture.Premium)).Status);
        var noOp = pricing.State();
        noOp.Businesses.Single().PricingPosture = PricingPosture.Standard;
        var priceReceipt = noOp.Receipts.Single(x =>
            GameCommand.ParseCanonicalPayload(x.Payload).Kind == CommandKind.SetBusinessPricing);
        priceReceipt.Payload = BusinessCommands.SetPricing(id, PricingPosture.Standard).CanonicalPayload;
        RewriteBusinessHistory(noOp, "business.pricing_changed", record =>
            BusinessHistory.Encode(record.Action, record.OperationId, record.InstanceId, record.DateIso,
                record.Minute, record.Amount, PricingPosture.Standard));
        Equal(LoadStatus.Corrupt, Raw(pricing.Serializer, noOp).Status);
        Equal(LoadStatus.Corrupt, Raw(missingStart, noOp).Status);

        var closure = Ready(cash: 3000);
        Launch(closure, 100);
        id = closure.State().Businesses.Single().InstanceId;
        Equal(CommandStatus.Committed, closure.Execute("advance-before-close", new GameCommand(CommandKind.AdvanceBoundary)).Status);
        Equal(480, closure.State().Minute);
        Equal(CommandStatus.Committed, closure.Execute("close-provenance", BusinessCommands.Close(id)).Status);
        var wrongClosureTime = closure.State();
        wrongClosureTime.Businesses.Single().ClosedMinute = 0;
        RewriteBusinessHistory(wrongClosureTime, "business.closed", record =>
            BusinessHistory.Encode(record.Action, record.OperationId, record.InstanceId, record.DateIso,
                0, record.Amount, record.Pricing));
        Equal(LoadStatus.Corrupt, Raw(closure.Serializer, wrongClosureTime).Status);
        Equal(LoadStatus.Corrupt, Raw(missingStart, wrongClosureTime).Status);
    }

    private static void IntrinsicBusinessCommandPayloadPrecedesContentCompatibility()
    {
        var missingStartCatalog = BusinessCatalogWithoutStart(new[] { Freelance });
        var missingStart = new JsonSaveSerializer(missingStartCatalog, GameSession.CreateRestoreValidator(),
            new SyntheticV0Migration(), new V1ToV2Migration());

        var closure = Ready(cash: 3000);
        Launch(closure, 100);
        var id = closure.State().Businesses.Single().InstanceId;
        Equal(CommandStatus.Committed, closure.Execute("close-intrinsic", BusinessCommands.Close(id)).Status);
        var closed = closure.State();
        foreach (var command in new[]
        {
            new GameCommand(CommandKind.CloseBusiness, id, 1),
            new GameCommand(CommandKind.CloseBusiness, id, 0, "unused"),
            new GameCommand(CommandKind.CloseBusiness, id, 0, "", "unused")
        })
            AssertIntrinsicBusinessPayloadCorrupt(closure.Serializer, missingStart, closed, CommandKind.CloseBusiness, command);

        var reinvest = Ready(cash: 3000);
        Launch(reinvest, 100);
        id = reinvest.State().Businesses.Single().InstanceId;
        Equal(CommandStatus.Committed, reinvest.Execute("reinvest-intrinsic", BusinessCommands.Reinvest(id, 10)).Status);
        var reinvested = reinvest.State();
        foreach (var command in new[]
        {
            new GameCommand(CommandKind.ReinvestBusiness, id, 10, "unused"),
            new GameCommand(CommandKind.ReinvestBusiness, id, 10, "", "unused")
        })
            AssertIntrinsicBusinessPayloadCorrupt(reinvest.Serializer, missingStart, reinvested, CommandKind.ReinvestBusiness, command);

        var pricing = Ready(cash: 3000);
        Launch(pricing, 100);
        id = pricing.State().Businesses.Single().InstanceId;
        Equal(CommandStatus.Committed, pricing.Execute("pricing-intrinsic",
            BusinessCommands.SetPricing(id, PricingPosture.Premium)).Status);
        var priced = pricing.State();
        foreach (var command in new[]
        {
            new GameCommand(CommandKind.SetBusinessPricing, id, (int)PricingPosture.Premium, "unused"),
            new GameCommand(CommandKind.SetBusinessPricing, id, (int)PricingPosture.Premium, "", "unused")
        })
            AssertIntrinsicBusinessPayloadCorrupt(pricing.Serializer, missingStart, priced, CommandKind.SetBusinessPricing, command);
    }

    private static void IntrinsicBusinessCorruptionRecoversBackup()
    {
        var missingStartCatalog = BusinessCatalogWithoutStart(new[] { Freelance });
        var serializer = new JsonSaveSerializer(missingStartCatalog, GameSession.CreateRestoreValidator(),
            new SyntheticV0Migration(), new V1ToV2Migration());

        var closure = Ready(cash: 3000);
        Launch(closure, 100);
        var id = closure.State().Businesses.Single().InstanceId;
        Equal(CommandStatus.Committed, closure.Execute("close-recovery", BusinessCommands.Close(id)).Status);
        var corrupt = WithReceiptCommand(closure.State(), CommandKind.CloseBusiness,
            new GameCommand(CommandKind.CloseBusiness, id, 1));
        var primary = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(corrupt),
            SaveSchema.CurrentVersion, corrupt.Revision);
        Equal(LoadStatus.Corrupt, serializer.DeserializeAndValidate(primary).Status);

        var backupState = GameSession.NewState(missingStartCatalog, "backup-run", 98765, new SimDate(2026, 9, 2));
        var backup = serializer.Serialize(backupState);
        Equal(LoadStatus.Valid, serializer.DeserializeAndValidate(backup).Status);

        var path = NewSavePath("intrinsic-corrupt-recovery");
        File.WriteAllBytes(path, primary);
        File.WriteAllBytes(path + ".backup", backup);
        var primaryBefore = File.ReadAllBytes(path);
        var backupBefore = File.ReadAllBytes(path + ".backup");
        var recovered = new AtomicFileSaveStore(path, serializer).Read();
        Equal(LoadStatus.RecoveredBackup, recovered.Status);
        Equal("backup-run", recovered.State!.RunId);
        Bytes(primaryBefore, File.ReadAllBytes(path));
        Bytes(backupBefore, File.ReadAllBytes(path + ".backup"));
    }

    private static void MalformedFrozenV1IsContainedAndRecoversBackup()
    {
        var catalog = LegacyCatalog();
        var serializer = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(),
            new SyntheticV0Migration(), new V1ToV2Migration());
        var malformedPayload = Encoding.UTF8.GetBytes("{\"SaveVersion\":1,\"Receipts\":[");
        var primary = JsonSaveSerializer.Wrap(malformedPayload, SaveSchema.HistoricalV1, 0);
        Equal(LoadStatus.Corrupt, serializer.DeserializeAndValidate(primary).Status);

        var backupState = GameSession.NewState(catalog, "v1-backup-run", 45678, new SimDate(2026, 9, 2));
        var backup = serializer.Serialize(backupState);
        Equal(LoadStatus.Valid, serializer.DeserializeAndValidate(backup).Status);

        var path = NewSavePath("malformed-v1-recovery");
        File.WriteAllBytes(path, primary);
        File.WriteAllBytes(path + ".backup", backup);
        var primaryBefore = File.ReadAllBytes(path);
        var backupBefore = File.ReadAllBytes(path + ".backup");
        var recovered = new AtomicFileSaveStore(path, serializer).Read();
        Equal(LoadStatus.RecoveredBackup, recovered.Status);
        Equal("v1-backup-run", recovered.State!.RunId);
        Bytes(primaryBefore, File.ReadAllBytes(path));
        Bytes(backupBefore, File.ReadAllBytes(path + ".backup"));
    }

    private static void AssertIntrinsicBusinessPayloadCorrupt(JsonSaveSerializer full, JsonSaveSerializer missingStart,
        GameState source, CommandKind kind, GameCommand command)
    {
        var corrupt = WithReceiptCommand(source, kind, command);
        Equal(LoadStatus.Corrupt, Raw(full, corrupt).Status);
        Equal(LoadStatus.Corrupt, Raw(missingStart, corrupt).Status);
    }

    private static GameState WithReceiptCommand(GameState source, CommandKind kind, GameCommand command)
    {
        var corrupt = Clone(source);
        var receipt = corrupt.Receipts.Single(x => GameCommand.ParseCanonicalPayload(x.Payload).Kind == kind);
        receipt.Payload = command.CanonicalPayload;
        return corrupt;
    }

    private static void OrdinaryCommitSchemaBoundary()
    {
        var catalog = LegacyCatalog();
        var serializer = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(),
            new SyntheticV0Migration(), new V1ToV2Migration());
        var fixture = new Fixture(catalog);
        Equal(CommandStatus.Committed, fixture.Execute("create",
            new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female")).Status);
        var stateV1Generation = fixture.State();
        Equal(1L, stateV1Generation.Revision);
        var currentV1Generation = serializer.Serialize(stateV1Generation);
        var historicalV1Generation = ((ISaveCompatibilitySerializer)serializer)
            .SerializeForSchema(stateV1Generation, SaveSchema.HistoricalV1);
        var syntheticState = Clone(stateV1Generation);
        syntheticState.SaveVersion = 0;
        var syntheticV0Generation = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(syntheticState), 0, syntheticState.Revision);
        Equal(LoadStatus.Valid, serializer.DeserializeAndValidate(syntheticV0Generation).Status);

        var rejectedV1Path = NewSavePath("ordinary-v1");
        Equal(WriteStatus.Failed, new AtomicFileSaveStore(rejectedV1Path, serializer).Commit(historicalV1Generation, 0));
        True(!File.Exists(rejectedV1Path));

        var rejectedV0Path = NewSavePath("ordinary-v0");
        Equal(WriteStatus.Failed, new AtomicFileSaveStore(rejectedV0Path, serializer).Commit(syntheticV0Generation, 0));
        True(!File.Exists(rejectedV0Path));

        var currentPath = NewSavePath("ordinary-v2");
        Equal(WriteStatus.Committed, new AtomicFileSaveStore(currentPath, serializer).Commit(currentV1Generation, 0));
        Equal(SaveSchema.CurrentVersion, JsonSaveSerializer.ReadObject<SaveEnvelope>(File.ReadAllBytes(currentPath)).SchemaVersion);

        Equal(CommandStatus.Committed, fixture.Execute("job", new GameCommand(CommandKind.AcceptJob, "developer")).Status);
        var stateGeneration2 = fixture.State();
        var historicalGeneration2 = ((ISaveCompatibilitySerializer)serializer)
            .SerializeForSchema(stateGeneration2, SaveSchema.HistoricalV1);
        var before = File.ReadAllBytes(currentPath);
        Equal(WriteStatus.Failed, new AtomicFileSaveStore(currentPath, serializer).Commit(historicalGeneration2, 1));
        Bytes(before, File.ReadAllBytes(currentPath));

        var syntheticGeneration2 = Clone(stateGeneration2);
        syntheticGeneration2.SaveVersion = 0;
        var v0Generation2 = JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(syntheticGeneration2), 0, syntheticGeneration2.Revision);
        Equal(LoadStatus.Valid, serializer.DeserializeAndValidate(v0Generation2).Status);
        Equal(WriteStatus.Failed, new AtomicFileSaveStore(currentPath, serializer).Commit(v0Generation2, 1));
        Bytes(before, File.ReadAllBytes(currentPath));

        var compatibilityV1Path = NewSavePath("compat-v1");
        File.WriteAllBytes(compatibilityV1Path, historicalV1Generation);
        var compatibilityV1 = new AtomicFileSaveStore(compatibilityV1Path, serializer);
        Equal(WriteStatus.Committed, compatibilityV1.CommitReceiptCompatibility(historicalV1Generation, historicalV1Generation));
        Equal(SaveSchema.HistoricalV1, JsonSaveSerializer.ReadObject<SaveEnvelope>(File.ReadAllBytes(compatibilityV1Path)).SchemaVersion);
        Equal(SaveSchema.HistoricalV1, JsonSaveSerializer.ReadObject<SaveEnvelope>(File.ReadAllBytes(compatibilityV1Path + ".backup")).SchemaVersion);

        var compatibilityV2Path = NewSavePath("compat-v2");
        File.WriteAllBytes(compatibilityV2Path, currentV1Generation);
        Equal(WriteStatus.Committed, new AtomicFileSaveStore(compatibilityV2Path, serializer)
            .CommitReceiptCompatibility(currentV1Generation, currentV1Generation));
        Equal(SaveSchema.CurrentVersion, JsonSaveSerializer.ReadObject<SaveEnvelope>(File.ReadAllBytes(compatibilityV2Path)).SchemaVersion);
    }

    private static void MigrationStageValidation()
    {
        var catalog = MigrationCatalog();
        var source = FixtureBytes("current-v1.json");
        var envelope = JsonSaveSerializer.ReadObject<SaveEnvelope>(source);
        var payload = Convert.FromBase64String(envelope.PayloadBase64);
        var invalidSourcePayload = RewriteRootRevisionPayload(payload, envelope.Generation + 1);
        var invalidSource = JsonSaveSerializer.Wrap(invalidSourcePayload, SaveSchema.HistoricalV1, envelope.Generation);
        var sourceRepair = new RepairingV1ToV2Migration(envelope.Generation);
        var sourceSerializer = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(), sourceRepair);
        Equal(LoadStatus.Corrupt, sourceSerializer.DeserializeAndValidate(invalidSource).Status);
        Equal(0, sourceRepair.Calls);

        var v0 = FixtureBytes("synthetic-v0.json");
        var v0Envelope = JsonSaveSerializer.ReadObject<SaveEnvelope>(v0);
        var invalidIntermediate = new InvalidIntermediateV0ToV1Migration();
        var intermediateRepair = new RepairingV1ToV2Migration(v0Envelope.Generation);
        var intermediateSerializer = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(),
            invalidIntermediate, intermediateRepair);
        Equal(LoadStatus.Corrupt, intermediateSerializer.DeserializeAndValidate(v0).Status);
        Equal(1, invalidIntermediate.Calls);
        Equal(0, intermediateRepair.Calls);

        var builtIn = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(),
            new SyntheticV0Migration(), new V1ToV2Migration());
        var builtInLoad = builtIn.DeserializeAndValidate(v0);
        Equal(LoadStatus.Valid, builtInLoad.Status);
        Equal(SaveSchema.CurrentVersion, builtInLoad.State!.SaveVersion);
    }

    private static void LaunchRetryMatrix()
    {
        var fixture = Ready(cash: 3000);
        AssertBusinessRetryMatrix(fixture, "retry-launch",
            BusinessCommands.Launch(Freelance.Id, Freelance.Revision, 100),
            current => current.Execute("later-launch-price",
                BusinessCommands.SetPricing(current.State().Businesses.Single().InstanceId, PricingPosture.Premium)));
    }

    private static void ReinvestRetryMatrix()
    {
        var fixture = Ready(cash: 3000);
        Launch(fixture, 100);
        var id = fixture.State().Businesses.Single().InstanceId;
        AssertBusinessRetryMatrix(fixture, "retry-reinvest", BusinessCommands.Reinvest(id, 10),
            current => current.Execute("later-reinvest-price", BusinessCommands.SetPricing(id, PricingPosture.Premium)));
    }

    private static void PricingRetryMatrix()
    {
        var fixture = Ready(cash: 3000);
        Launch(fixture, 100);
        var id = fixture.State().Businesses.Single().InstanceId;
        AssertBusinessRetryMatrix(fixture, "retry-pricing", BusinessCommands.SetPricing(id, PricingPosture.Premium),
            current => current.Execute("later-pricing-reinvest", BusinessCommands.Reinvest(id, 10)));
    }

    private static void CloseRetryMatrix()
    {
        var fixture = Ready(cash: 3000);
        Launch(fixture, 100);
        var id = fixture.State().Businesses.Single().InstanceId;
        AssertBusinessRetryMatrix(fixture, "retry-close", BusinessCommands.Close(id),
            current => current.Execute("later-close-launch", BusinessCommands.Launch(Freelance.Id, Freelance.Revision, 100)));
    }

    private static void BusinessCommandOwnershipHostility()
    {
        var launch = Ready(cash: 3000);
        Equal(CommandStatus.Committed, launch.Execute("same-launch",
            BusinessCommands.Launch(Freelance.Id, Freelance.Revision, 100)).Status);
        AssertIdConflictUnchanged(launch, "same-launch", BusinessCommands.Launch(Freelance.Id, Freelance.Revision, 101));

        var reinvest = Ready(cash: 3000); Launch(reinvest, 100);
        var id = reinvest.State().Businesses.Single().InstanceId;
        Equal(CommandStatus.Committed, reinvest.Execute("same-reinvest", BusinessCommands.Reinvest(id, 10)).Status);
        AssertIdConflictUnchanged(reinvest, "same-reinvest", BusinessCommands.Reinvest(id, 11));

        var pricing = Ready(cash: 3000); Launch(pricing, 100);
        id = pricing.State().Businesses.Single().InstanceId;
        Equal(CommandStatus.Committed, pricing.Execute("same-pricing", BusinessCommands.SetPricing(id, PricingPosture.Premium)).Status);
        AssertIdConflictUnchanged(pricing, "same-pricing", BusinessCommands.SetPricing(id, PricingPosture.Budget));

        var close = Ready(cash: 3000); Launch(close, 100);
        id = close.State().Businesses.Single().InstanceId;
        Equal(CommandStatus.Committed, close.Execute("same-close", BusinessCommands.Close(id)).Status);
        AssertIdConflictUnchanged(close, "same-close", new GameCommand(CommandKind.CloseBusiness, id, 1));

        var directRoot = Ready(cash: 3000);
        Equal(CommandStatus.Committed, directRoot.Execute("business-root",
            BusinessCommands.Launch(Freelance.Id, Freelance.Revision, 100)).Status);
        var directBytes = directRoot.Checkpoint();
        var blockedBatch = directRoot.Session.AdvanceDay(new CommandEnvelope("run", "business-root",
            directRoot.Session.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary)));
        Equal("InvalidRequest", blockedBatch.StopReason);
        Bytes(directBytes, directRoot.Checkpoint());

        var batchRoot = Ready(cash: 3000);
        var batch = batchRoot.Session.AdvanceDay(new CommandEnvelope("run", "business-batch",
            batchRoot.Session.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary)));
        Equal("TargetReached", batch.StopReason);
        var batchBytes = batchRoot.Checkpoint();
        var blockedDirect = batchRoot.Session.Execute(new CommandEnvelope("run", "business-batch",
            batchRoot.Session.Snapshot().Revision, BusinessCommands.Launch(Freelance.Id, Freelance.Revision, 100)));
        Equal(CommandStatus.Rejected, blockedDirect.Status);
        Equal("command.id_conflict", blockedDirect.ReasonKey);
        Bytes(batchBytes, batchRoot.Checkpoint());

        var disjoint = Ready(cash: 3000);
        Equal(CommandStatus.Committed, disjoint.Execute("business-child/0",
            BusinessCommands.Launch(Freelance.Id, Freelance.Revision, 100)).Status);
        var rootBatch = disjoint.Session.AdvanceDay(new CommandEnvelope("run", "business-child",
            disjoint.Session.Snapshot().Revision, new GameCommand(CommandKind.AdvanceBoundary)));
        Equal("TargetReached", rootBatch.StopReason);
    }

    private static void AssertBusinessRetryMatrix(Fixture fixture, string commandId, GameCommand command,
        Func<Fixture, CommandResult> laterCommit)
    {
        var original = fixture.Execute(commandId, command);
        Equal(CommandStatus.Committed, original.Status);
        True(original.Outcome != null);
        var committed = fixture.Checkpoint();
        var priorRevision = original.Outcome!.PriorRevision;

        var immediate = fixture.ExecuteAt(commandId, command, priorRevision);
        AssertEquivalentOutcome(original, immediate);
        Bytes(committed, fixture.Checkpoint());

        var cold = fixture.Restore();
        Bytes(committed, cold.Checkpoint());
        var coldRetry = cold.ExecuteAt(commandId, command, priorRevision);
        AssertEquivalentOutcome(original, coldRetry);
        Bytes(committed, cold.Checkpoint());

        var later = laterCommit(cold);
        Equal(CommandStatus.Committed, later.Status);
        var afterLater = cold.Checkpoint();
        var lateRetry = cold.ExecuteAt(commandId, command, priorRevision);
        AssertEquivalentOutcome(original, lateRetry);
        // Byte identity covers cash, ledger, history, businesses, entity/operation counters,
        // receipts, and revision, so a retry cannot duplicate any committed side effect.
        Bytes(afterLater, cold.Checkpoint());
    }

    private static void AssertEquivalentOutcome(CommandResult original, CommandResult retry)
    {
        Equal(CommandStatus.AlreadyCommitted, retry.Status);
        Equal(original.Revision, retry.Revision);
        Equal(original.OperationId, retry.OperationId);
        Equal(original.MinutesConsumed, retry.MinutesConsumed);
        True(original.Outcome != null && retry.Outcome != null);
        var expected = original.Outcome!;
        var actual = retry.Outcome!;
        Equal(expected.OperationId, actual.OperationId);
        Equal(expected.CommandId, actual.CommandId);
        Equal(expected.ActivityId, actual.ActivityId);
        Equal(expected.PriorRevision, actual.PriorRevision);
        Equal(expected.Revision, actual.Revision);
        Equal(expected.Start, actual.Start);
        Equal(expected.End, actual.End);
        Equal(expected.MinutesConsumed, actual.MinutesConsumed);
        Equal(expected.Cue, actual.Cue);
        Equal(expected.PlaybackCursor, actual.PlaybackCursor);
        Equal(expected.CashDelta, actual.CashDelta);
        Equal(expected.EmploymentId, actual.EmploymentId);
        Equal(expected.CareerXpDelta, actual.CareerXpDelta);
        Equal(expected.PriorRank, actual.PriorRank);
        Equal(expected.NewRank, actual.NewRank);
        True(expected.HistoryEntries.SequenceEqual(actual.HistoryEntries));
        True(expected.GrantedIds.SequenceEqual(actual.GrantedIds));
        True(expected.LedgerEntries.Select(x => (x.Id, x.Category, x.CashDelta, x.Amount, x.AttributionId))
            .SequenceEqual(actual.LedgerEntries.Select(x => (x.Id, x.Category, x.CashDelta, x.Amount, x.AttributionId))));
        True(expected.SkillDeltas.Select(x => (x.SkillId, x.ExposureDelta, x.PriorLevel, x.NewLevel, x.PriorGrantedLevel, x.NewGrantedLevel))
            .SequenceEqual(actual.SkillDeltas.Select(x => (x.SkillId, x.ExposureDelta, x.PriorLevel, x.NewLevel, x.PriorGrantedLevel, x.NewGrantedLevel))));
    }

    private static void AssertIdConflictUnchanged(Fixture fixture, string commandId, GameCommand changed)
    {
        var before = fixture.Checkpoint();
        var result = fixture.Session.Execute(new CommandEnvelope("run", commandId, fixture.Session.Snapshot().Revision, changed));
        Equal(CommandStatus.Rejected, result.Status);
        Equal("command.id_conflict", result.ReasonKey);
        Bytes(before, fixture.Checkpoint());
    }

    private static byte[] InjectRootMember(byte[] checkpoint, string member)
    {
        var envelope = JsonSaveSerializer.ReadObject<SaveEnvelope>(checkpoint);
        var payload = Encoding.UTF8.GetString(Convert.FromBase64String(envelope.PayloadBase64));
        var close = payload.LastIndexOf('}');
        True(close >= 0);
        var injected = payload.Insert(close, "," + member);
        return JsonSaveSerializer.Wrap(Encoding.UTF8.GetBytes(injected), envelope.SchemaVersion, envelope.Generation);
    }

    private static byte[] RewriteRootRevisionPayload(byte[] payload, long revision)
    {
        using var document = JsonDocument.Parse(payload);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                writer.WritePropertyName(property.Name);
                if (property.NameEquals("Revision")) writer.WriteNumberValue(revision);
                else property.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static void RewriteBusinessHistory(GameState state, string action, Func<BusinessHistoryRecord, string> rewrite)
    {
        var index = state.History.FindIndex(x => x.StartsWith(action + ":", StringComparison.Ordinal));
        True(index >= 0);
        True(BusinessHistory.TryParse(state.History[index], out var record) && record != null);
        state.History[index] = rewrite(record!);
    }

    private static string NewSavePath(string name)
    {
        var directory = Path.Combine(Path.GetTempPath(), "StartupLifeM8Audit", name + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "save.json");
    }

    private static void RestoreBusinessCorruption()
    {
        var f = Ready(cash: 1000); Launch(f, 100); var valid = f.State();
        var corrupt = Clone(valid); corrupt.Businesses[0].ClosedIso = corrupt.DateIso; corrupt.Businesses[0].ClosedMinute = null;
        Equal(LoadStatus.Corrupt, Raw(f.Serializer, corrupt).Status);

        corrupt = Clone(valid); corrupt.Businesses[0].PricingPosture = (PricingPosture)99;
        Equal(LoadStatus.Corrupt, Raw(f.Serializer, corrupt).Status);

        corrupt = Clone(valid); corrupt.Businesses[0].ReinvestedAmount = -1;
        Equal(LoadStatus.Corrupt, Raw(f.Serializer, corrupt).Status);

        corrupt = Clone(valid); corrupt.Businesses[0].DefinitionId = "missing-business";
        corrupt.Businesses[0].ReinvestedAmount = -1;
        Equal(LoadStatus.Corrupt, Raw(f.Serializer, corrupt).Status);
    }

    private static void UnsupportedBusinessRevision()
    {
        var f = Ready(cash: 1000); Launch(f, 100);
        var checkpoint = f.Checkpoint();
        var replacement = new BusinessDefinition(Freelance.Id, "fixture-r2", Freelance.NameKey, Freelance.Type,
            Freelance.OperationMode, 100, 1000, Freelance.AllowedPricingPostures, PricingPosture.Standard, 1, 1000);
        var catalog = BusinessCatalog(1000, new[] { replacement });
        var serializer = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
        var load = serializer.DeserializeAndValidate(checkpoint);
        Equal(LoadStatus.UnsupportedContent, load.Status); Equal("save.content_revision", load.Reason);
    }

    private static readonly BusinessDefinition Freelance = Definition("test.business.freelance", BusinessType.FreelanceService);
    private static readonly BusinessDefinition FreelanceAlt = Definition("test.business.freelance-alt", BusinessType.FreelanceService);
    private static readonly BusinessDefinition ManagerDefinition = Definition("test.business.manager", BusinessType.OnlineStore, BusinessOperationMode.ManagerOperable);
    private static readonly BusinessDefinition FullTimeDefinition = Definition("test.business.fulltime", BusinessType.OnlineStore, BusinessOperationMode.FullTimeRequired);
    private static readonly BusinessDefinition FifthDifferentDefinition = Definition("test.business.fifth", BusinessType.OnlineStore);
    private static readonly BusinessDefinition[] FourSideHustles =
    {
        Definition("test.business.online", BusinessType.OnlineStore),
        Definition("test.business.food", BusinessType.HomeFoodPreorder),
        Definition("test.business.freelance-four", BusinessType.FreelanceService),
        Definition("test.business.kiosk-fixture", BusinessType.CoffeeKiosk)
    };

    private static BusinessDefinition Definition(string id, BusinessType type,
        BusinessOperationMode mode = BusinessOperationMode.SideHustleCompatible, long startMin = 100, long startMax = 1000,
        long reinvestMin = 1, long reinvestMax = 1000, IEnumerable<PricingPosture>? pricing = null) =>
        new BusinessDefinition(id, "fixture-r1", id + ".name", type, mode, startMin, startMax,
            pricing ?? new[] { PricingPosture.Budget, PricingPosture.Standard, PricingPosture.Premium },
            PricingPosture.Standard, reinvestMin, reinvestMax);

    private static ContentCatalog BusinessCatalog(long cash, IEnumerable<BusinessDefinition> businesses, bool includeCareer = false)
    {
        var skills = includeCareer ? new[] { new SkillDefinition("communication", "skill.communication", 100, 300, 600, 1000, 1500) } : Array.Empty<SkillDefinition>();
        var careers = Array.Empty<CareerDefinition>();
        if (includeCareer)
        {
            var scene = new CareerSceneDefinition("coding", "scene.coding", 100, 10, "communication", 1);
            var rank = new CareerRankDefinition("junior", 0, 0, 1000, "communication", 0);
            careers = new[] { new CareerDefinition("developer", "v1", "career.developer", 540, 1020, 4, 20,
                new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                new[] { scene }, new[] { rank }) };
        }
        return new ContentCatalog("business.fixture.v1", skills, careers, Array.Empty<CourseDefinition>(),
            new[] { new CharacterStartDefinition("fresh", cash, 10000, "base.female") }, businesses,
            new EconomyBalanceDefinition(1, 1, 0), new DayScheduleDefinition(480, 1320));
    }

    private static ContentCatalog BusinessCatalogWithoutStart(IEnumerable<BusinessDefinition> businesses) =>
        new ContentCatalog("business.fixture.v1", Array.Empty<SkillDefinition>(), Array.Empty<CareerDefinition>(),
            Array.Empty<CourseDefinition>(), Array.Empty<CharacterStartDefinition>(), businesses,
            new EconomyBalanceDefinition(1, 1, 0), new DayScheduleDefinition(480, 1320));

    private static ContentCatalog MigrationCatalog()
    {
        var skillIds = new[] { "communication", "negotiation", "time-management", "problem-solving", "networking", "leadership" };
        var skills = skillIds.Select(id => new SkillDefinition(id, "skill." + id, 100, 300, 600, 1000, 1500)).ToArray();
        CareerSceneDefinition Scene(string id, int weight) => new CareerSceneDefinition(id, "scene." + id, weight, 10, "communication", 1);
        var career = new CareerDefinition("developer", "v1", "career.developer", 540, 1020, 4, 20,
            new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            new[] { Scene("coding", 40), Scene("meeting", 20), Scene("bug-fixing", 15), Scene("client-discussion", 10), Scene("demo", 10), Scene("documentation", 5) },
            new[]
            {
                new CareerRankDefinition("junior", 0, 0, 10000000, "problem-solving", 0),
                new CareerRankDefinition("mid", 1000, 30, 15000000, "problem-solving", 2)
            });
        var courses = new[]
        {
            new CourseDefinition("communication-basics", "course.communication", "communication", 1, 0, 360, 200000),
            new CourseDefinition("problem-course", "course.problem", "problem-solving", 2, 0, 360, 200000)
        };
        return new ContentCatalog("fixture.v1", skills, new[] { career }, courses,
            new[] { new CharacterStartDefinition("fresh", 3000000, 10000, "base.female", "base.male") },
            new EconomyBalanceDefinition(1, 1, 1000000), new DayScheduleDefinition(480, 1320));
    }

    private static ContentCatalog LegacyCatalog()
    {
        var skill = new SkillDefinition("communication", "skill.communication", 100, 300, 600, 1000, 1500);
        var scene = new CareerSceneDefinition("coding", "scene.coding", 100, 10, "communication", 1);
        var rank = new CareerRankDefinition("junior", 0, 0, 10000000, "communication", 0);
        var career = new CareerDefinition("developer", "v1", "career.developer", 540, 1020, 4, 20,
            new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            new[] { scene }, new[] { rank });
        return new ContentCatalog("fixture.v1", new[] { skill }, new[] { career }, Array.Empty<CourseDefinition>(),
            new[] { new CharacterStartDefinition("fresh", 3000000, 10000, "base.female") },
            new EconomyBalanceDefinition(1, 1, 0), new DayScheduleDefinition(480, 1320));
    }

    private static Fixture Ready(long cash, IEnumerable<BusinessDefinition>? businesses = null, bool includeCareer = false)
    {
        var catalog = BusinessCatalog(cash, businesses ?? new[] { Freelance }, includeCareer);
        var f = new Fixture(catalog);
        Equal(CommandStatus.Committed, f.Execute("create", new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female")).Status);
        return f;
    }

    private static GameState CharacterState(ContentCatalog catalog, long cash)
    {
        var state = GameSession.NewState(catalog, "run", 12345, new SimDate(2026, 9, 2));
        state.Name = "An"; state.StartingAge = 25; state.BirthDateIso = "2001-09-02";
        state.AppearanceId = "base.female"; state.BackgroundId = "fresh"; state.LearningSpeed = 10000; state.Cash = cash;
        state.Skills = catalog.Skills.Keys.Select(x => new SkillState { Id = x }).ToList();
        state.Revision = 1; state.NextOperation = 2;
        state.History.Add("character.created:2026-09-02");
        state.Receipts.Add(new CommandReceipt
        {
            CommandId = "create", Payload = new GameCommand(CommandKind.CreateCharacter, "fresh", 25, "An", "base.female").CanonicalPayload,
            OperationId = "run/op/1", Revision = 1, Cue = "character.created"
        });
        state.CurrentActivity = "run/op/1"; state.CurrentCue = "character.created";
        return state;
    }

    private static (GameState state, ContentCatalog catalog) ReadyArrearsState(BusinessDefinition definition, bool includeBusiness = false)
    {
        var catalog = BusinessCatalog(1000, new[] { definition });
        var state = CharacterState(catalog, 1000);
        state.Arrears.Add(new ArrearState { Id = state.NewEntity("arrear"), DueIso = state.DateIso, Amount = 1 });
        if (includeBusiness)
        {
            state.Businesses.Add(new BusinessState
            {
                InstanceId = state.NewEntity("business"), DefinitionId = definition.Id, DefinitionRevision = definition.Revision,
                OpenedIso = state.DateIso, OpenedMinute = state.Minute, PricingPosture = definition.DefaultPricingPosture,
                InitialInvestment = 100
            });
        }
        return (state, catalog);
    }

    private static void Launch(Fixture f, int amount, BusinessDefinition? definition = null)
    {
        definition ??= Freelance;
        Equal(CommandStatus.Committed, f.Execute("launch-" + f.Session.Snapshot().Revision,
            BusinessCommands.Launch(definition.Id, definition.Revision, amount)).Status);
    }

    private static void ExpectRejectedUnchanged(Fixture f, GameCommand command, string reason)
    {
        var before = f.Checkpoint();
        var revision = f.Session.Snapshot().Revision;
        var result = f.Session.Execute(new CommandEnvelope("run", "reject-" + Guid.NewGuid().ToString("N"), revision, command));
        Equal(CommandStatus.Rejected, result.Status); Equal(reason, result.ReasonKey); Equal(revision, f.Session.Snapshot().Revision);
        Bytes(before, f.Checkpoint());
    }

    private static LoadResult Raw(JsonSaveSerializer serializer, GameState state) =>
        serializer.DeserializeAndValidate(JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(state), SaveSchema.CurrentVersion, state.Revision));

    private static byte[] FixtureBytes(string name) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
    private static GameState Clone(GameState state) => JsonSaveSerializer.ReadObject<GameState>(JsonSaveSerializer.WriteObject(state));

    private sealed class Fixture
    {
        public ContentCatalog Catalog { get; }
        public JsonSaveSerializer Serializer { get; }
        public MemoryStore Store { get; }
        public GameSession Session { get; }
        public Fixture(ContentCatalog catalog, byte[]? persisted = null, GameState? state = null)
        {
            Catalog = catalog;
            Serializer = new JsonSaveSerializer(catalog, GameSession.CreateRestoreValidator(), new SyntheticV0Migration(), new V1ToV2Migration());
            var initial = state ?? GameSession.NewState(catalog, "run", 12345, new SimDate(2026, 9, 2));
            Store = new MemoryStore(Serializer, persisted);
            Session = new GameSession(initial, catalog, Serializer, Store);
        }
        public CommandResult Execute(string id, GameCommand command) =>
            Session.Execute(new CommandEnvelope("run", id, Session.Snapshot().Revision, command));
        public CommandResult ExecuteAt(string id, GameCommand command, long revision) =>
            Session.Execute(new CommandEnvelope("run", id, revision, command));
        public GameState State() => Serializer.DeserializeAndValidate(Session.ExportCheckpoint()).State!;
        public byte[] Checkpoint() => Session.ExportCheckpoint();
        public Fixture Restore()
        {
            var bytes = Store.Bytes!;
            var state = Serializer.DeserializeAndValidate(bytes).State!;
            return new Fixture(Catalog, bytes, state);
        }
    }

    private sealed class MemoryStore : ISaveStore
    {
        private readonly ISaveSerializer serializer;
        private byte[]? bytes;
        public byte[]? Bytes => bytes == null ? null : (byte[])bytes.Clone();
        public MemoryStore(ISaveSerializer serializer, byte[]? initial) { this.serializer = serializer; bytes = initial == null ? null : (byte[])initial.Clone(); }
        public LoadResult Read() => bytes == null ? new LoadResult(LoadStatus.Missing) : serializer.DeserializeAndValidate(bytes);
        public WriteStatus Commit(byte[] candidate, long expectedRevision)
        {
            var next = serializer.DeserializeAndValidate(candidate);
            var previous = Read();
            if (next.Status != LoadStatus.Valid || next.State!.Revision != expectedRevision + 1) return WriteStatus.Failed;
            if (previous.Status == LoadStatus.Missing)
            {
                if (expectedRevision != 0) return WriteStatus.Failed;
            }
            else if (previous.Status != LoadStatus.Valid || previous.State!.Revision != expectedRevision) return WriteStatus.Failed;
            bytes = (byte[])candidate.Clone(); return WriteStatus.Committed;
        }
    }

    private sealed class RepairingV1ToV2Migration : ISaveMigration
    {
        private readonly long targetRevision;
        public int Calls { get; private set; }
        public int FromVersion => SaveSchema.HistoricalV1;
        public int ToVersion => SaveSchema.CurrentVersion;
        public RepairingV1ToV2Migration(long targetRevision) { this.targetRevision = targetRevision; }
        public byte[] Migrate(byte[] payload)
        {
            Calls++;
            var current = JsonSaveSerializer.ReadObject<GameState>(new V1ToV2Migration().Migrate(payload));
            current.Revision = targetRevision;
            return JsonSaveSerializer.WriteObject(current);
        }
    }

    private sealed class InvalidIntermediateV0ToV1Migration : ISaveMigration
    {
        public int Calls { get; private set; }
        public int FromVersion => 0;
        public int ToVersion => SaveSchema.HistoricalV1;
        public byte[] Migrate(byte[] payload)
        {
            Calls++;
            var historical = new SyntheticV0Migration().Migrate(payload);
            var projected = HistoricalPayloadRevision(historical);
            return RewriteRootRevisionPayload(historical, checked(projected + 1));
        }

        private static long HistoricalPayloadRevision(byte[] payload)
        {
            using var document = JsonDocument.Parse(payload);
            return document.RootElement.GetProperty("Revision").GetInt64();
        }
    }

    private sealed class NonAdjacentMigration : ISaveMigration
    {
        public int FromVersion => 0; public int ToVersion => 2; public byte[] Migrate(byte[] payload) => payload;
    }
    private sealed class InvalidV1ToV2Migration : ISaveMigration
    {
        public int FromVersion => 1; public int ToVersion => 2;
        public byte[] Migrate(byte[] payload) => Encoding.UTF8.GetBytes("{\\\"SaveVersion\\\":1}");
    }

    private static void Check(string name, Action action)
    {
        try { action(); passed++; results.Add(new { name, status = "passed" }); Console.WriteLine("PASS " + name); }
        catch (Exception e) { results.Add(new { name, status = "failed", error = e.ToString() }); Console.WriteLine("FAIL " + name + ": " + e.Message); }
    }
    private static void ThrowsRule(string reason, Action action)
    {
        try { action(); throw new Exception("Expected RuleFailure " + reason); }
        catch (RuleFailure e) { Equal(reason, e.ReasonKey); }
    }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); throw new Exception("Expected " + typeof(T).Name); }
        catch (T) { }
    }
    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}; got {actual}");
    }
    private static void True(bool value) { if (!value) throw new Exception("Assertion failed"); }
    private static void Bytes(byte[] expected, byte[] actual) { if (!expected.SequenceEqual(actual)) throw new Exception("Byte arrays differ."); }
}
