#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StartupLife.Core;
using StartupLife.Simulation;

namespace StartupLife.Application
{
    // One deterministic v3 *candidate* evaluator. Never mutate a published state.
    // The GameSession receipt/CAS path must call this instead of invoking legacy
    // SimulationEngine directly after the historical v2 receipt frontier.
    public sealed class V3EconomicReceiptProcessor
    {
        private readonly ContentCatalog content;
        private readonly BusinessEconomicCatalog economics;
        private readonly EconomicActivationAnchor activation;

        public V3EconomicReceiptProcessor(ContentCatalog content,
            BusinessEconomicCatalog economics, EconomicActivationAnchor activation)
        {
            this.content = content ?? throw new ArgumentNullException(nameof(content));
            this.economics = economics ?? throw new ArgumentNullException(nameof(economics));
            this.activation = activation ?? throw new ArgumentNullException(nameof(activation));
        }

        public int Evaluate(EconomicV3Payload candidate, GameCommand command, string operationId)
        {
            if (candidate == null || candidate.Current == null ||
                candidate.EconomicRecords == null || command == null ||
                string.IsNullOrWhiteSpace(operationId) ||
                candidate.Current.SaveVersion != 3)
                throw new ArgumentException("Only an isolated schema3 command is supported.");
            var game = candidate.Current;
            var records = candidate.EconomicRecords;
            if (records.RulesetRevision.Length == 0)
                records.RulesetRevision = economics.RulesetRevision;
            if (records.RulesetRevision != economics.RulesetRevision)
                throw new ContentCompatibilityException("save.ruleset_revision",
                    "Unknown committed business ruleset.");
            // Keep historical businesses from migration; each new launch gets a new policy.
            if (records.BusinessPolicies.Count == 0 && game.Businesses.Count > 0)
                V3BusinessPolicyEngine.InitializeFromMigration(game, records);

            var oldDate = game.DateIso;
            var oldMinute = game.Minute;
            if (IsPolicy(command.Kind))
            {
                V3BusinessPolicyEngine.ApplyCommand(game, records, command, operationId);
                ReopenProspective(game, records, operationId);
                return 0;
            }

            if (command.Kind == CommandKind.AdvanceBoundary)
            {
                // Old date has priority over next-day salary and living costs.
                if (activation.ShouldOpenEconomicOperationsOnDate(oldDate))
                    EnsureOpened(game, records, operationId);
                var midnight = game.Minute >= content.Schedule.SleepMinute;
                if (midnight && activation.ShouldOpenEconomicOperationsOnDate(oldDate))
                {
                    if (records.Provenance.ActiveEpoch != null)
                        V3EpochTransactions.ConsumeInterval(game, records, oldMinute, 1440, operationId);
                    var instant = game.Minute;
                    game.Minute = 1440; // temporary local cursor for old-date settlement only
                    try { V3MidnightTransactions.SettleOldDate(game, records, oldDate, operationId); }
                    finally { game.Minute = instant; }
                }
                var minutes = new SimulationEngine(content, records).Evaluate(game, command, operationId);
                if (!midnight && game.DateIso == oldDate &&
                    activation.ShouldOpenEconomicOperationsOnDate(oldDate) &&
                    records.Provenance.ActiveEpoch != null)
                    V3EpochTransactions.ConsumeInterval(game, records, oldMinute, game.Minute, operationId);
                if (game.DateIso != oldDate)
                {
                    V3BusinessPolicyEngine.ActivateNewDate(game, records, operationId);
                    EnsureOpened(game, records, operationId);
                }
                return minutes;
            }

            // Study and cash-changing commands release ONLY uncommitted future
            // reservations. Their changes are applied from already-existing wallet,
            // never projected same-day revenue.
            var prospective = command.Kind == CommandKind.Study ||
                command.Kind == CommandKind.LaunchBusiness ||
                command.Kind == CommandKind.ReinvestBusiness ||
                command.Kind == CommandKind.PurchaseCourse ||
                command.Kind == CommandKind.Resign ||
                command.Kind == CommandKind.AcceptJob;
            if (prospective) V3EpochTransactions.ReleaseFuture(records.Provenance);
            var result = new SimulationEngine(content, records).Evaluate(game, command, operationId);
            if (command.Kind == CommandKind.LaunchBusiness)
            {
                var instance = game.Businesses.Last(x => x.IsActive);
                V3BusinessPolicyEngine.OnLaunched(game, records, instance.InstanceId);
            }
            if (prospective) EnsureOpened(game, records, operationId);
            return result;
        }

        private static bool IsPolicy(CommandKind kind) =>
            kind == CommandKind.SetBusinessPricing ||
            kind == CommandKind.PauseBusiness ||
            kind == CommandKind.ResumeBusiness ||
            kind == CommandKind.CloseBusiness;

        private void ReopenProspective(GameState game, EconomicV3Records records, string sourceOperation)
        {
            V3EpochTransactions.ReleaseFuture(records.Provenance);
            EnsureOpened(game, records, sourceOperation);
        }

        private void EnsureOpened(GameState game, EconomicV3Records records, string operation)
        {
            if (!activation.ShouldOpenEconomicOperationsOnDate(game.DateIso)) return;
            if (records.Provenance.ActiveEpoch != null &&
                records.Provenance.ActiveEpoch.DateIso == game.DateIso)
                return;
            var entries = game.Businesses.ToArray();
            var eligible = entries.ToDictionary(x => x.InstanceId,
                x => x.IsActive && records.BusinessPolicies.Single(y => y.InstanceId == x.InstanceId)
                    .Status == EconomicOperationStatusCode.Auto, StringComparer.Ordinal);
            var pending = new List<BusinessObligationDefinition>();
            foreach (var instance in entries)
            {
                if (!economics.Profiles.TryGetValue(instance.DefinitionId, out var profile))
                    throw new ContentCompatibilityException("save.content_id",
                        "Business economic profile is not archived or authored.");
                // Never back-bill economic time before the v2->v3 activation date.
                var inception = string.CompareOrdinal(instance.OpenedIso, activation.ActivationDateIso) > 0 ?
                    instance.OpenedIso : activation.ActivationDateIso;
                var definition = new BusinessObligationDefinition(
                    instance.DefinitionId + ".fixed.daily", profile.ProfileRevision,
                    instance.DefinitionId, profile.ProfileRevision,
                    ObligationCadence.Daily, ObligationProrationPolicy.FullContractual,
                    ObligationClosurePolicy.EnforceableThroughContractEnd,
                    inception, instance.ClosedIso, profile.DailyMandatoryChargeVnd, true);
                if (!records.ObligationTranches.Any(x =>
                    x.BusinessInstanceId == instance.InstanceId &&
                    x.ObligationDefinitionId == definition.Id && x.DueDateIso == game.DateIso))
                    pending.Add(definition);
            }
            if (pending.Count > 0)
                V3ObligationTransactions.OpenDue(game, records, pending, eligible, operation);
            V3ObligationTransactions.PayDue(game, records, operation);

            var snapshots = new List<MarketPoolDaySupply>();
            foreach (var pool in economics.Pools.Values)
            {
                var saved = records.Provenance.MarketPools.SingleOrDefault(x =>
                    x.DateIso == game.DateIso && x.PoolId == pool.PoolId);
                if (saved != null)
                    snapshots.Add(new MarketPoolDaySupply(game.DateIso, pool.PoolId,
                        saved.DefinitionRevision, saved.Segments.Select(x =>
                            new KeyValuePair<string,int>(x.SegmentId, x.OriginalUnits))));
                else snapshots.Add(CustomerDemandCalculator.OpeningSupply(game.DateIso, economics, pool.PoolId));
            }
            var prior = records.Provenance.Fulfillments.Where(x => x.DateIso == game.DateIso)
                .Select(x => new MarketFulfillmentUnit(x.Id, x.DateIso, x.PoolId,
                    x.SegmentId, x.MarketUnitOrdinal,
                    x.BusinessInstanceId, x.SliceId, x.SourceBoundaryOperationId)).ToArray();
            var occupied = CommittedTimeTrace.Project(game, content);
            var unavailable = new bool[1440];
            for (var t = 0; t < unavailable.Length; t++)
                unavailable[t] = t < game.Minute || occupied[t] != CommittedTimeTrace.Free;
            var employment = game.Employment;
            if (employment != null && content.Careers.TryGetValue(employment.CareerId, out var career) &&
                career.WorksOn(game.Date) &&
                string.CompareOrdinal(game.DateIso, employment.FirstShiftIso) >= 0)
                for (var t = career.StartMinute; t < career.EndMinute; t++)
                    unavailable[t] = true;

            var participants = new List<BusinessMarketEpochParticipant>();
            foreach (var owned in entries.Where(x => x.IsActive))
            {
                if (!content.Businesses.TryGetValue(owned.DefinitionId, out var definition) ||
                    !economics.Profiles.TryGetValue(owned.DefinitionId, out var profile))
                    throw new ContentCompatibilityException("save.content_id",
                        "An open business lacks v3 runtime definitions.");
                if (profile.RequiredOwnerMinutes != definition.OperatingRequirements?.RequiredOwnerMinutes)
                    throw new ArgumentException("Economic requirements differ from authored operating windows.");
                if (definition.OperationMode == BusinessOperationMode.ManagerOperable) continue;
                var windows = definition.OperatingRequirements!.OperatingWindows
                    .Where(x => x.DayOfWeek == game.Date.DayOfWeek).ToArray();
                var slots = windows.SelectMany(w => Enumerable.Range(w.StartMinute,
                    w.EndMinute - w.StartMinute))
                    .Where(t => t >= game.Minute && !unavailable[t])
                    .OrderBy(x => x).Distinct().ToArray();
                var worked = records.Provenance.OperationSlices.Where(x =>
                    x.DateIso == game.DateIso && x.BusinessInstanceId == owned.InstanceId)
                    .Sum(x => x.CommittedOwnerMinuteDelta);
                var fulfilled = prior.Count(x => x.BusinessInstanceId == owned.InstanceId);
                var policy = records.BusinessPolicies.Single(x => x.InstanceId == owned.InstanceId);
                var posture = policy.EffectivePricing;
                var effective = new BusinessProspectivePolicy(owned.InstanceId,
                    (EconomicOperationStatus)(int)policy.Status, posture,
                    policy.HasPendingPricing ? policy.PendingPricing : (PricingPosture?)null,
                    policy.HasPendingPricing ? policy.PendingPricingEffectiveIso : null,
                    policy.HasPendingResume,
                    policy.HasPendingResume ? policy.PendingResumeEffectiveIso : null,
                    policy.LastSourceOperationId);
                var unfunded = records.ObligationTranches.Any(x =>
                    x.BusinessInstanceId == owned.InstanceId &&
                    x.DueDateIso == game.DateIso && x.OutstandingVnd > 0);
                var time = new BusinessTimeWalletCandidate(owned.DefinitionId, owned.InstanceId,
                    definition.OperationMode, profile.RequiredOwnerMinutes,
                    profile.FullCapacityUnits, worked, fulfilled, profile.FullCapacityUnits,
                    profile.VariableCostPerUnitVnd, slots);
                participants.Add(new BusinessMarketEpochParticipant(time,
                    new BusinessDemandCandidate(owned.InstanceId, owned.DefinitionId, posture),
                    effective, !unfunded));
            }
            var epochId = game.RunId + "/economic-epoch/" +
                game.DateIso + "/" + operation.Length.ToString(CultureInfo.InvariantCulture) +
                ":" + operation;
            var planned = BusinessMarketEpochPlanner.Freeze(economics, epochId, game.DateIso,
                game.Cash, snapshots, prior, participants,
                Enumerable.Range(0, 1440).Where(t => unavailable[t]));
            V3EpochTransactions.Freeze(game, records, planned, operation, game.Minute);
        }
    }
}
