#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace StartupLife.Core
{
    // Append-only v3 content vocabulary; no edits to v2 GameState, ledger or command IDs.
    public enum ObligationCadence
    {
        OneTime = 1,
        PerOperation = 2,
        PerOperatingDay = 3,
        Daily = 4,
        Weekly = 5,
        Monthly = 6,
        Annual = 7,
        OnClose = 8
    }

    public enum ObligationProrationPolicy { FullContractual = 1, DayMetered = 2, PerEvent = 3 }
    public enum ObligationClosurePolicy { CancellableAfterServiceDate = 1, EnforceableThroughContractEnd = 2 }

    public sealed class BusinessObligationDefinition
    {
        public string Id { get; }
        public string Revision { get; }
        public string BusinessDefinitionId { get; }
        public string AmountRuleRevision { get; }
        public ObligationCadence Cadence { get; }
        public ObligationProrationPolicy Proration { get; }
        public ObligationClosurePolicy Closure { get; }
        public string ContractStartIso { get; }
        public string? ContractEndIso { get; }
        public long FullChargeVnd { get; }
        public bool BlockOperationWhenUnpaid { get; }

        public BusinessObligationDefinition(string id, string revision, string businessId, string amountRuleRevision,
            ObligationCadence cadence, ObligationProrationPolicy proration,
            ObligationClosurePolicy closure, string contractStartIso, string? contractEndIso,
            long fullChargeVnd, bool blockOperationWhenUnpaid)
        {
            EconomicDefinitionLimits.Id(id, nameof(id));
            EconomicDefinitionLimits.Id(businessId, nameof(businessId));
            EconomicDefinitionLimits.Revision(revision, nameof(revision));
            EconomicDefinitionLimits.Revision(amountRuleRevision, nameof(amountRuleRevision));
            if (!Enum.IsDefined(typeof(ObligationCadence), cadence) ||
                !Enum.IsDefined(typeof(ObligationProrationPolicy), proration) ||
                !Enum.IsDefined(typeof(ObligationClosurePolicy), closure))
                throw new ArgumentException("Invalid obligation policy.");
            if (fullChargeVnd < 0 || fullChargeVnd > 1000000000000L)
                throw new ArgumentOutOfRangeException(nameof(fullChargeVnd));
            var start = ParseDate(contractStartIso);
            if (contractEndIso != null && ParseDate(contractEndIso) < start)
                throw new ArgumentException("Contract ends before it starts.");
            if ((cadence == ObligationCadence.OneTime || cadence == ObligationCadence.PerOperation ||
                 cadence == ObligationCadence.OnClose) && proration == ObligationProrationPolicy.DayMetered)
                throw new ArgumentException("One-time, per-operation and termination fees cannot be prorated.");
            Id = id; Revision = revision; BusinessDefinitionId = businessId; AmountRuleRevision = amountRuleRevision;
            Cadence = cadence; Proration = proration; Closure = closure; ContractStartIso = contractStartIso;
            ContractEndIso = contractEndIso; FullChargeVnd = fullChargeVnd;
            BlockOperationWhenUnpaid = blockOperationWhenUnpaid;
        }

        public static DateTime ParseDate(string iso) => DateTime.ParseExact(iso, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None);
    }

    public sealed class BusinessObligationDue
    {
        public string IdentityKey { get; }
        public string BusinessInstanceId { get; }
        public string ObligationId { get; }
        public string ObligationRevision { get; }
        public string PeriodKey { get; }
        public string DueEventKey { get; }
        public string DueDateIso { get; }
        public long AmountDueVnd { get; }

        public BusinessObligationDue(string businessId, BusinessObligationDefinition definition,
            string periodKey, string eventKey, string dueDateIso)
        {
            if (string.IsNullOrWhiteSpace(businessId) || string.IsNullOrWhiteSpace(periodKey) ||
                string.IsNullOrWhiteSpace(eventKey)) throw new ArgumentException("Missing obligation event identity.");
            BusinessInstanceId = businessId; ObligationId = definition.Id; ObligationRevision = definition.Revision;
            PeriodKey = periodKey; DueEventKey = eventKey; DueDateIso = dueDateIso;
            AmountDueVnd = definition.FullChargeVnd;
            // Length-prefix all fields to prevent delimiter collisions with user-chosen operation IDs.
            IdentityKey = Join(businessId, definition.Id, definition.Revision, periodKey, eventKey);
        }

        private static string Join(params string[] values)
        {
            var result = "";
            foreach (var value in values)
                result += value.Length.ToString(CultureInfo.InvariantCulture) + ":" + value;
            return result;
        }
    }

    // One debt, two linked views: the due amount is not summed again as separate arrears.
    public sealed class ObligationDueTranche
    {
        public string Id { get; }
        public string ObligationIdentity { get; }
        public string DueDateIso { get; }
        public int DueMinute { get; }
        public long CreationSequence { get; }
        public long DueVnd { get; }
        public long PaidVnd { get; }
        public long OutstandingVnd => checked(DueVnd - PaidVnd);

        public ObligationDueTranche(string id, string obligationIdentity, string dateIso, int dueMinute,
            long sequence, long dueVnd, long paidVnd)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(obligationIdentity) ||
                dueMinute < 0 || dueMinute >= 1440 || sequence < 0 || dueVnd < 0 ||
                paidVnd < 0 || paidVnd > dueVnd)
                throw new ArgumentException("Invalid due tranche.");
            _ = BusinessObligationDefinition.ParseDate(dateIso);
            Id = id; ObligationIdentity = obligationIdentity; DueDateIso = dateIso;
            DueMinute = dueMinute; CreationSequence = sequence; DueVnd = dueVnd; PaidVnd = paidVnd;
        }
    }

    public sealed class ObligationPaymentAllocation
    {
        public string TrancheId { get; }
        public long PaidVnd { get; }
        public ObligationPaymentAllocation(string trancheId, long paidVnd)
        { TrancheId = trancheId; PaidVnd = paidVnd; }
    }
}
