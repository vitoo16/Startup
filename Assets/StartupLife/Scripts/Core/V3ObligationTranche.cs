#nullable enable
using System;
using System.Runtime.Serialization;

namespace StartupLife.Core
{
    // One persistent payable invoice. Outstanding = AmountDue - Paid; arrears are a VIEW,
    // not a second payable record. A due liability survives a business pause/close.
    [DataContract]
    public sealed class V3ObligationTranche
    {
        [DataMember(Order=0)] public string Id { get; set; } = "";
        [DataMember(Order=1)] public string BusinessInstanceId { get; set; } = "";
        [DataMember(Order=2)] public string ObligationDefinitionId { get; set; } = "";
        [DataMember(Order=3)] public string DefinitionRevision { get; set; } = "";
        [DataMember(Order=4)] public string PeriodKey { get; set; } = "";
        [DataMember(Order=5)] public string EventKey { get; set; } = "";
        [DataMember(Order=6)] public string DueDateIso { get; set; } = "";
        [DataMember(Order=7)] public int DueMinute { get; set; }
        [DataMember(Order=8)] public long CreationSequence { get; set; }
        [DataMember(Order=9)] public long AmountDueVnd { get; set; }
        [DataMember(Order=10)] public long PaidVnd { get; set; }
        [DataMember(Order=11)] public string SourceOperationId { get; set; } = "";
        public long OutstandingVnd => checked(AmountDueVnd - PaidVnd);
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Id) ||
                string.IsNullOrWhiteSpace(BusinessInstanceId) ||
                !ContentId.IsValid(ObligationDefinitionId) ||
                string.IsNullOrWhiteSpace(DefinitionRevision) ||
                string.IsNullOrWhiteSpace(PeriodKey) ||
                string.IsNullOrWhiteSpace(EventKey) ||
                string.IsNullOrWhiteSpace(SourceOperationId) ||
                DueMinute < 0 || DueMinute >= 1440 ||
                CreationSequence < 0 || AmountDueVnd < 0 ||
                PaidVnd < 0 || PaidVnd > AmountDueVnd)
                throw new ArgumentException("Invalid versioned contractual payable.");
            _ = DateTime.ParseExact(DueDateIso,"yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
