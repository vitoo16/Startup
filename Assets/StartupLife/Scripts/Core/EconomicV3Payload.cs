#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace StartupLife.Core
{
    // Save v3 wire contracts: mutable only for the serializer; NEVER publish as read models.
    // The original v2 checkpoint is embedded verbatim and independently checksummed
    // so historical byte-for-byte identity survives later v3 commands and catalog changes.
    [DataContract]
    public sealed class EconomicV3Payload
    {
        [DataMember(Order = 0)] public int WireSchema { get; set; } = 3;
        [DataMember(Order = 1)] public GameState? Current { get; set; }
        [DataMember(Order = 2)] public EconomicV3Activation? Activation { get; set; }
        [DataMember(Order = 3)] public EconomicV3Records? EconomicRecords { get; set; }
        [DataMember(Order = 4)] public string OriginalV2PayloadBase64 { get; set; } = "";
        [DataMember(Order = 5)] public string OriginalV2PayloadSha256 { get; set; } = "";
        [DataMember(Order = 6)] public string OriginalV2ContentVersion { get; set; } = "";
        [DataMember(Order = 7)] public string OriginalV2RulesetId { get; set; } = "";
        [DataMember(Order = 8)] public int OriginalSourceSchema { get; set; } = 2;
        [DataMember(Order = 9)] public string OriginalSourcePayloadBase64 { get; set; } = "";
        [DataMember(Order = 10)] public string OriginalSourcePayloadSha256 { get; set; } = "";
    }

    [DataContract]
    public sealed class EconomicV3Activation
    {
        [DataMember(Order = 0)] public long LegacyReceiptCount { get; set; }
        [DataMember(Order = 1)] public string LegacyLastOperationId { get; set; } = "";
        [DataMember(Order = 2)] public string OriginalContentVersion { get; set; } = "";
        [DataMember(Order = 3)] public int OriginalSourceSchema { get; set; }
        [DataMember(Order = 4)] public string MigratedAtDateIso { get; set; } = "";
        [DataMember(Order = 5)] public int MigratedAtMinute { get; set; }
        [DataMember(Order = 6)] public string ActivationDateIso { get; set; } = "";
        [DataMember(Order = 7)] public string ArchivedRulesetId { get; set; } = "";
        [DataMember(Order = 8)] public string CutoverTransformVersion { get; set; } = "";
        [DataMember(Order = 9)] public bool SourceMidnightAlreadyProcessedLegacySettlement { get; set; }

        public EconomicActivationAnchor ToAnchor() => new EconomicActivationAnchor(
            LegacyReceiptCount, LegacyLastOperationId, OriginalContentVersion,
            OriginalSourceSchema, MigratedAtDateIso, MigratedAtMinute,
            ActivationDateIso, ArchivedRulesetId, CutoverTransformVersion,
            SourceMidnightAlreadyProcessedLegacySettlement);

        public static EconomicV3Activation FromAnchor(EconomicActivationAnchor anchor)
        {
            if (anchor == null) throw new ArgumentNullException(nameof(anchor));
            return new EconomicV3Activation
            {
                LegacyReceiptCount = anchor.LegacyReceiptCount,
                LegacyLastOperationId = anchor.LegacyLastOperationId,
                OriginalContentVersion = anchor.OriginalContentVersion,
                OriginalSourceSchema = anchor.OriginalSourceSchema,
                MigratedAtDateIso = anchor.MigratedAtDateIso,
                MigratedAtMinute = anchor.MigratedAtMinute,
                ActivationDateIso = anchor.ActivationDateIso,
                ArchivedRulesetId = anchor.ArchivedRulesetId,
                CutoverTransformVersion = anchor.CutoverTransformVersion,
                SourceMidnightAlreadyProcessedLegacySettlement = anchor.SourceMidnightAlreadyProcessedLegacySettlement
            };
        }
    }

    // Explicit future-tracked collections. No financial backfill on 2->3 migration.
    // Persistent operation/fulfillment DTOs will populate these under GameSession authority only.
    [DataContract]
    public sealed class EconomicV3Records
    {
        [DataMember(Order = 0)] public string RulesetRevision { get; set; } = "";
        [DataMember(Order = 1)] public List<string> CommittedEpochIds { get; set; } = new List<string>();
        [DataMember(Order = 2)] public List<string> CommittedSliceIds { get; set; } = new List<string>();
        [DataMember(Order = 3)] public List<string> CommittedFulfillmentIds { get; set; } = new List<string>();
        [DataMember(Order = 4)] public List<string> CommittedObligationIds { get; set; } = new List<string>();
        [DataMember(Order = 5)] public List<string> CommittedCostReservationIds { get; set; } = new List<string>();
        [DataMember(Order = 6)] public List<string> SettledDayIds { get; set; } = new List<string>();
        [DataMember(Order = 7)] public long V3RecognizedRevenueVnd { get; set; }
        [DataMember(Order = 8)] public long V3PaidVariableVnd { get; set; }
        [DataMember(Order = 9)] public long V3RecognizedFixedVnd { get; set; }
        [DataMember(Order = 10)] public long V3AppliedToArrearsVnd { get; set; }
        [DataMember(Order = 11)] public long V3NetBusinessCreditVnd { get; set; }
        [DataMember(Order = 12)] public V3FinancialProvenance Provenance { get; set; } = new V3FinancialProvenance();
        // Prospective v3 business state is the authority, never v2's immediate price evaluator.
        [DataMember(Order = 13)] public List<V3BusinessPolicyRecord> BusinessPolicies { get; set; } = new List<V3BusinessPolicyRecord>();

        public void Validate()
        {
            if (RulesetRevision == null || BusinessPolicies == null || CommittedEpochIds == null ||
                CommittedSliceIds == null || CommittedFulfillmentIds == null ||
                CommittedObligationIds == null || CommittedCostReservationIds == null ||
                SettledDayIds == null || V3RecognizedRevenueVnd < 0 ||
                V3PaidVariableVnd < 0 || V3RecognizedFixedVnd < 0 ||
                V3AppliedToArrearsVnd < 0 || V3NetBusinessCreditVnd < 0)
                throw new ArgumentException("Invalid v3 financial record graph.");
            if (checked(V3AppliedToArrearsVnd + V3NetBusinessCreditVnd) != V3RecognizedRevenueVnd)
                throw new ArgumentException("V3 gross/arrears/net credit conservation.");
            if (Provenance == null) throw new ArgumentException("Missing authoritative v3 provenance.");
            Provenance.Validate();
            RequireRoster(CommittedSliceIds, Provenance.OperationSlices.Select(x=>x.Id));
            RequireRoster(CommittedFulfillmentIds, Provenance.Fulfillments.Select(x=>x.Id));
            RequireRoster(CommittedCostReservationIds, Provenance.CostReservations.Select(x=>x.Id));
            RequireRoster(SettledDayIds, Provenance.Settlements.Select(x=>x.Id));
            if (Provenance.OperationSlices.Any(x=>!CommittedEpochIds.Contains(x.EpochId)) ||
                Provenance.CostReservations.Any(x=>!CommittedEpochIds.Contains(x.EpochId)))
                throw new ArgumentException("Economic graph uses an uncommitted epoch.");
            foreach (var policy in BusinessPolicies) policy.Validate();
            Unique(BusinessPolicies.Select(x => x.InstanceId).ToList());
            Unique(CommittedEpochIds); Unique(CommittedSliceIds); Unique(CommittedFulfillmentIds);
            Unique(CommittedObligationIds); Unique(CommittedCostReservationIds); Unique(SettledDayIds);
        }

        private static void RequireRoster(IEnumerable<string> declared, IEnumerable<string> observed)
        {
            if (!declared.OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(
                observed.OrderBy(x=>x,StringComparer.Ordinal)))
                throw new ArgumentException("V3 financial record index is inconsistent with saved provenance.");
        }

        private static void Unique(List<string> values)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in values)
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id))
                    throw new ArgumentException("Duplicate or empty committed v3 financial record identity.");
        }

        public static EconomicV3Records Empty() => new EconomicV3Records();
    }
}
