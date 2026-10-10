#nullable enable
using System;
using System.Globalization;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // v3 prospective policy read model. The original BusinessState.PricingPosture
    // and frozen v2 SetBusinessPricing evaluator are intentionally not changed here.
    public enum EconomicOperationStatus { Auto = 1, Paused = 2, Closed = 3 }

    public sealed class BusinessProspectivePolicy
    {
        public string InstanceId { get; }
        public EconomicOperationStatus EffectiveStatus { get; }
        public PricingPosture EffectivePricing { get; }
        public PricingPosture? PendingPricing { get; }
        public string? PendingPricingEffectiveIso { get; }
        public bool PendingResume { get; }
        public string? PendingResumeEffectiveIso { get; }
        public string LastSourceOperationId { get; }
        public bool IsOperationEligible => EffectiveStatus == EconomicOperationStatus.Auto;

        public BusinessProspectivePolicy(string instanceId, EconomicOperationStatus status,
            PricingPosture pricing, PricingPosture? pendingPricing = null,
            string? pendingPricingDate = null, bool pendingResume = false,
            string? pendingResumeDate = null, string lastOperationId = "")
        {
            if (string.IsNullOrWhiteSpace(instanceId) ||
                !Enum.IsDefined(typeof(EconomicOperationStatus), status) ||
                !Enum.IsDefined(typeof(PricingPosture), pricing) ||
                (pendingPricing.HasValue && !Enum.IsDefined(typeof(PricingPosture), pendingPricing.Value)) ||
                pendingPricing.HasValue != (pendingPricingDate != null) ||
                pendingResume != (pendingResumeDate != null) ||
                (pendingResume && status != EconomicOperationStatus.Paused) ||
                (pendingPricing.HasValue && pendingPricing.Value == pricing) ||
                (status == EconomicOperationStatus.Closed &&
                 (pendingResume || pendingPricing.HasValue)))
                throw new ArgumentException("Invalid prospective operation/pricing policy.");
            if (pendingPricingDate != null) _ = Parse(pendingPricingDate);
            if (pendingResumeDate != null) _ = Parse(pendingResumeDate);
            InstanceId = instanceId; EffectiveStatus = status; EffectivePricing = pricing;
            PendingPricing = pendingPricing; PendingPricingEffectiveIso = pendingPricingDate;
            PendingResume = pendingResume; PendingResumeEffectiveIso = pendingResumeDate;
            LastSourceOperationId = lastOperationId;
        }

        internal static DateTime Parse(string iso) => DateTime.ParseExact(iso, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None);

        internal BusinessProspectivePolicy With(EconomicOperationStatus status, PricingPosture effective,
            PricingPosture? pendingPrice, string? priceDate, bool pendingResume, string? resumeDate,
            string operationId) =>
            new BusinessProspectivePolicy(InstanceId, status, effective, pendingPrice, priceDate,
                pendingResume, resumeDate, operationId);
    }

    // Every transition is requested by a committed v3 command. This class makes NO
    // GameState or wallet mutations; the atomic GameSession remains sole authority.
    public static class BusinessPolicyTransitions
    {
        public static BusinessProspectivePolicy RequestPrice(BusinessProspectivePolicy old,
            PricingPosture desired, string todayIso, string sourceOperationId)
        {
            ValidateChange(old, sourceOperationId);
            if (!Enum.IsDefined(typeof(PricingPosture), desired)) throw new ArgumentException("Unknown posture.");
            var nextDay = BusinessProspectivePolicy.Parse(todayIso).AddDays(1).ToString("yyyy-MM-dd",
                CultureInfo.InvariantCulture);
            return old.With(old.EffectiveStatus, old.EffectivePricing,
                desired == old.EffectivePricing ? (PricingPosture?)null : desired,
                desired == old.EffectivePricing ? null : nextDay,
                old.PendingResume, old.PendingResumeEffectiveIso, sourceOperationId);
        }

        public static BusinessProspectivePolicy Pause(BusinessProspectivePolicy old, string operationId)
        {
            ValidateChange(old, operationId);
            return old.With(EconomicOperationStatus.Paused, old.EffectivePricing,
                old.PendingPricing, old.PendingPricingEffectiveIso, false, null, operationId);
        }

        public static BusinessProspectivePolicy RequestResume(BusinessProspectivePolicy old,
            string todayIso, string operationId)
        {
            ValidateChange(old, operationId);
            if (old.EffectiveStatus != EconomicOperationStatus.Paused)
                throw new ArgumentException("Only paused operation can schedule resume.");
            var nextDay = BusinessProspectivePolicy.Parse(todayIso).AddDays(1).ToString("yyyy-MM-dd",
                CultureInfo.InvariantCulture);
            return old.With(EconomicOperationStatus.Paused, old.EffectivePricing,
                old.PendingPricing, old.PendingPricingEffectiveIso, true, nextDay, operationId);
        }

        public static BusinessProspectivePolicy Close(BusinessProspectivePolicy old, string operationId)
        {
            ValidateChange(old, operationId);
            return old.With(EconomicOperationStatus.Closed, old.EffectivePricing,
                null, null, false, null, operationId);
        }

        public static BusinessProspectivePolicy ActivateNewDate(BusinessProspectivePolicy old,
            string newDateIso, string sourceBoundaryOperationId)
        {
            if (old == null || string.IsNullOrWhiteSpace(sourceBoundaryOperationId))
                throw new ArgumentException("Missing current operation policy or boundary receipt.");
            var date = BusinessProspectivePolicy.Parse(newDateIso);
            var effective = old.EffectivePricing;
            var p = old.PendingPricing;
            var pendingDate = old.PendingPricingEffectiveIso;
            var status = old.EffectiveStatus;
            var resume = old.PendingResume;
            var resumeDate = old.PendingResumeEffectiveIso;
            if (pendingDate != null && date >= BusinessProspectivePolicy.Parse(pendingDate))
            {
                effective = p!.Value;
                p = null; pendingDate = null;
            }
            if (resumeDate != null && date >= BusinessProspectivePolicy.Parse(resumeDate))
            {
                status = EconomicOperationStatus.Auto;
                resume = false; resumeDate = null;
            }
            return old.With(status, effective, p, pendingDate, resume, resumeDate, sourceBoundaryOperationId);
        }

        private static void ValidateChange(BusinessProspectivePolicy old, string operation)
        {
            if (old == null || string.IsNullOrWhiteSpace(operation) ||
                old.EffectiveStatus == EconomicOperationStatus.Closed)
                throw new ArgumentException("Closed business or missing committed source cannot transition.");
        }
    }
}
