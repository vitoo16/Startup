#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // v3 candidate mutator invoked inside the future GameSession clone/validate/CAS
    // transaction. No wall clock, and never used by the archived v2 evaluator.
    public static class V3BusinessPolicyEngine
    {
        public static void InitializeFromMigration(GameState candidate, EconomicV3Records financial)
        {
            RequireV3(candidate, financial);
            if (financial.BusinessPolicies.Count != 0)
                throw new ArgumentException("Policy migration is not idempotently additive.");
            foreach (var owned in candidate.Businesses.OrderBy(x => x.InstanceId, StringComparer.Ordinal))
            {
                financial.BusinessPolicies.Add(new V3BusinessPolicyRecord
                {
                    InstanceId = owned.InstanceId,
                    Status = owned.IsActive ? EconomicOperationStatusCode.Auto : EconomicOperationStatusCode.Closed,
                    EffectivePricing = owned.PricingPosture
                });
            }
            financial.Validate();
        }

        public static void OnLaunched(GameState candidate, EconomicV3Records financial, string instanceId)
        {
            RequireV3(candidate, financial);
            var owned = candidate.Businesses.SingleOrDefault(x => x.InstanceId == instanceId);
            if (owned == null || !owned.IsActive || financial.BusinessPolicies.Any(x => x.InstanceId == instanceId))
                throw new ArgumentException("The launched instance already has a policy or is not active.");
            financial.BusinessPolicies.Add(new V3BusinessPolicyRecord
            {
                InstanceId = owned.InstanceId, Status = EconomicOperationStatusCode.Auto,
                EffectivePricing = owned.PricingPosture
            });
            financial.Validate();
        }

        public static void ApplyCommand(GameState candidate, EconomicV3Records financial,
            GameCommand command, string operationId)
        {
            RequireV3(candidate, financial);
            if (command == null || string.IsNullOrWhiteSpace(operationId) ||
                !IsPolicyCommand(command.Kind) || string.IsNullOrWhiteSpace(command.ContentId) ||
                command.Name.Length != 0 || command.AppearanceId.Length != 0 ||
                (command.Kind != CommandKind.SetBusinessPricing && command.Amount != 0))
                throw new RuleFailure("business.invalid_payload");
            var owned = candidate.Businesses.SingleOrDefault(x => x.InstanceId == command.ContentId);
            if (owned == null) throw new RuleFailure("business.not_found");
            if (!owned.IsActive) throw new RuleFailure("business.closed");
            var saved = financial.BusinessPolicies.SingleOrDefault(x => x.InstanceId == command.ContentId);
            if (saved == null) throw new RuleFailure("business.policy_missing");
            saved.Validate();
            if (saved.Status == EconomicOperationStatusCode.Closed) throw new RuleFailure("business.closed");
            var policy = From(saved);
            BusinessProspectivePolicy next;
            switch (command.Kind)
            {
                case CommandKind.SetBusinessPricing:
                    if (!Enum.IsDefined(typeof(PricingPosture), command.Amount))
                        throw new RuleFailure("business.invalid_payload");
                    next = BusinessPolicyTransitions.RequestPrice(policy,
                        (PricingPosture)command.Amount, candidate.DateIso, operationId);
                    candidate.CurrentCue = "business.pricing_scheduled";
                    break;
                case CommandKind.PauseBusiness:
                    if (saved.Status != EconomicOperationStatusCode.Auto)
                        throw new RuleFailure("business.already_paused");
                    next = BusinessPolicyTransitions.Pause(policy, operationId);
                    candidate.CurrentCue = "business.paused";
                    break;
                case CommandKind.ResumeBusiness:
                    if (saved.Status != EconomicOperationStatusCode.Paused ||
                        saved.HasPendingResume)
                        throw new RuleFailure("business.not_paused");
                    next = BusinessPolicyTransitions.RequestResume(policy, candidate.DateIso, operationId);
                    candidate.CurrentCue = "business.resume_scheduled";
                    break;
                case CommandKind.CloseBusiness:
                    next = BusinessPolicyTransitions.Close(policy, operationId);
                    owned.ClosedIso = candidate.DateIso;
                    owned.ClosedMinute = candidate.Minute;
                    candidate.CurrentCue = "business.closed";
                    break;
                default: throw new RuleFailure("command.unsupported");
            }
            CopyBack(saved, next);
            candidate.History.Add(BusinessHistory.Encode(candidate.CurrentCue,
                operationId, owned.InstanceId, candidate.DateIso,
                candidate.Minute, 0, next.EffectivePricing));
            financial.Validate();
        }

        public static void ActivateNewDate(GameState candidate, EconomicV3Records financial,
            string sourceBoundaryOperationId)
        {
            RequireV3(candidate, financial);
            if (string.IsNullOrWhiteSpace(sourceBoundaryOperationId))
                throw new ArgumentException("A real boundary operation must activate pending pricing.");
            foreach (var record in financial.BusinessPolicies.OrderBy(x => x.InstanceId, StringComparer.Ordinal))
            {
                var next = BusinessPolicyTransitions.ActivateNewDate(From(record),
                    candidate.DateIso, sourceBoundaryOperationId);
                CopyBack(record, next);
                var business = candidate.Businesses.Single(x => x.InstanceId == record.InstanceId);
                // Existing presentation reads the effective price, not the pending one.
                business.PricingPosture = next.EffectivePricing;
                if (record.Status == EconomicOperationStatusCode.Closed && business.IsActive)
                    throw new ArgumentException("Closed business cannot reactivate on new day.");
            }
            financial.Validate();
        }

        public static void ValidateAgainstBusinesses(GameState candidate, EconomicV3Records financial)
        {
            RequireV3(candidate, financial);
            if (candidate.Businesses.Count != financial.BusinessPolicies.Count)
                throw new ArgumentException("Each historical/current business needs exactly one v3 policy.");
            foreach (var business in candidate.Businesses)
            {
                var policy = financial.BusinessPolicies.SingleOrDefault(x => x.InstanceId == business.InstanceId);
                if (policy == null || policy.EffectivePricing != business.PricingPosture ||
                    business.IsActive == (policy.Status == EconomicOperationStatusCode.Closed))
                    throw new ArgumentException("Business effective v3 pricing/closure differs from policy.");
            }
        }

        private static bool IsPolicyCommand(CommandKind kind) =>
            kind == CommandKind.SetBusinessPricing || kind == CommandKind.CloseBusiness ||
            kind == CommandKind.PauseBusiness || kind == CommandKind.ResumeBusiness;

        private static void RequireV3(GameState candidate, EconomicV3Records financial)
        {
            if (candidate == null || financial == null || candidate.SaveVersion != 3)
                throw new ArgumentException("The prospective financial policy evaluator requires v3.");
        }

        private static BusinessProspectivePolicy From(V3BusinessPolicyRecord policy)
        {
            policy.Validate();
            return new BusinessProspectivePolicy(policy.InstanceId,
                (EconomicOperationStatus)(int)policy.Status,
                policy.EffectivePricing,
                policy.HasPendingPricing ? policy.PendingPricing : (PricingPosture?)null,
                policy.HasPendingPricing ? policy.PendingPricingEffectiveIso : null,
                policy.HasPendingResume,
                policy.HasPendingResume ? policy.PendingResumeEffectiveIso : null,
                policy.LastSourceOperationId);
        }

        private static void CopyBack(V3BusinessPolicyRecord saved, BusinessProspectivePolicy policy)
        {
            saved.Status = (EconomicOperationStatusCode)(int)policy.EffectiveStatus;
            saved.EffectivePricing = policy.EffectivePricing;
            saved.HasPendingPricing = policy.PendingPricing.HasValue;
            saved.PendingPricing = policy.PendingPricing ?? PricingPosture.Standard;
            saved.PendingPricingEffectiveIso = policy.PendingPricingEffectiveIso ?? "";
            saved.HasPendingResume = policy.PendingResume;
            saved.PendingResumeEffectiveIso = policy.PendingResumeEffectiveIso ?? "";
            saved.LastSourceOperationId = policy.LastSourceOperationId;
        }
    }
}
