#nullable enable
using System;
using System.Runtime.Serialization;

namespace StartupLife.Core
{
    // v3-only persisted operating policy; v2's BusinessState and pricing history remain frozen.
    [DataContract]
    public sealed class V3BusinessPolicyRecord
    {
        [DataMember(Order=0)] public string InstanceId { get; set; } = "";
        [DataMember(Order=1)] public EconomicOperationStatusCode Status { get; set; }
        [DataMember(Order=2)] public PricingPosture EffectivePricing { get; set; }
        [DataMember(Order=3)] public bool HasPendingPricing { get; set; }
        [DataMember(Order=4)] public PricingPosture PendingPricing { get; set; }
        [DataMember(Order=5)] public string PendingPricingEffectiveIso { get; set; } = "";
        [DataMember(Order=6)] public bool HasPendingResume { get; set; }
        [DataMember(Order=7)] public string PendingResumeEffectiveIso { get; set; } = "";
        [DataMember(Order=8)] public string LastSourceOperationId { get; set; } = "";

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(InstanceId) ||
                !Enum.IsDefined(typeof(EconomicOperationStatusCode), Status) ||
                !Enum.IsDefined(typeof(PricingPosture), EffectivePricing) ||
                (HasPendingPricing != (PendingPricingEffectiveIso.Length > 0)) ||
                (HasPendingResume != (PendingResumeEffectiveIso.Length > 0)) ||
                (HasPendingPricing && !Enum.IsDefined(typeof(PricingPosture), PendingPricing)) ||
                (Status == EconomicOperationStatusCode.Closed && (HasPendingPricing || HasPendingResume)) ||
                (HasPendingResume && Status != EconomicOperationStatusCode.Paused))
                throw new ArgumentException("Invalid persisted v3 pricing/mode state.");
            if (HasPendingPricing) _ = DateTime.ParseExact(PendingPricingEffectiveIso,
                "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            if (HasPendingResume) _ = DateTime.ParseExact(PendingResumeEffectiveIso,
                "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    // Keep the serialized enum in Core; the simulation's policy object can convert it.
    public enum EconomicOperationStatusCode
    {
        Auto = 1,
        Paused = 2,
        Closed = 3
    }
}
