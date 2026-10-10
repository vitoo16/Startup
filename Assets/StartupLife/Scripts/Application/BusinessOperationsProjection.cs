#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Core;
using StartupLife.Simulation;

namespace StartupLife.Application
{
    // Immutable, same-revision advisory UI projection. Commands still own CAS,
    // economic funding, and owner-time admission.
    public enum BusinessLaunchReadinessCode
    {
        Ready = 1,
        CharacterMissing,
        BusinessTypeAlreadyActive,
        PortfolioAtLimit,
        PersonalArrears,
        InsufficientCash,
        EmploymentIncompatible,
        InsufficientOwnerTime,
        UnsupportedMode,
        UnsupportedContent
    }

    public sealed class BusinessLaunchReadiness
    {
        public string DefinitionId { get; }
        public string Revision { get; }
        public string NameKey { get; }
        public long MinimumInvestmentVnd { get; }
        public long AvailableCashVnd { get; }
        public int? RequiredOwnerMinutes { get; }
        public int? AvailableOwnerMinutes { get; }
        public BusinessLaunchReadinessCode Code { get; }
        public bool CanRequestLaunch => Code == BusinessLaunchReadinessCode.Ready;

        internal BusinessLaunchReadiness(BusinessDefinition definition, GameState state,
            ContentCatalog content, IReadOnlyList<BusinessState> active)
        {
            DefinitionId = definition.Id;
            Revision = definition.Revision;
            NameKey = definition.NameKey;
            MinimumInvestmentVnd = definition.MinimumStartupInvestment;
            AvailableCashVnd = state.Cash;
            var eligibility = BusinessEligibilityEvaluator.Evaluate(
                content, state, definition.Id, definition.Revision);
            RequiredOwnerMinutes = eligibility.RequiredOwnerMinutes;
            AvailableOwnerMinutes = eligibility.AvailableOwnerMinutes;
            Code = string.IsNullOrEmpty(state.Name) ? BusinessLaunchReadinessCode.CharacterMissing :
                active.Any(x => content.Businesses[x.DefinitionId].Type == definition.Type)
                    ? BusinessLaunchReadinessCode.BusinessTypeAlreadyActive :
                active.Count >= BusinessPortfolioRules.MaxActiveBusinesses
                    ? BusinessLaunchReadinessCode.PortfolioAtLimit :
                state.Arrears.Any(x => x.Amount > 0)
                    ? BusinessLaunchReadinessCode.PersonalArrears :
                state.Cash < MinimumInvestmentVnd
                    ? BusinessLaunchReadinessCode.InsufficientCash :
                eligibility.Status == BusinessEligibilityStatus.EmploymentIncompatible
                    ? BusinessLaunchReadinessCode.EmploymentIncompatible :
                eligibility.Status == BusinessEligibilityStatus.InsufficientOwnerTime
                    ? BusinessLaunchReadinessCode.InsufficientOwnerTime :
                eligibility.Status == BusinessEligibilityStatus.UnsupportedOperationMode
                    ? BusinessLaunchReadinessCode.UnsupportedMode :
                eligibility.IsEligible ? BusinessLaunchReadinessCode.Ready :
                    BusinessLaunchReadinessCode.UnsupportedContent;
        }
    }

    public sealed class BusinessOperatingReadiness
    {
        public BusinessSnapshot Business { get; }
        public BusinessOperatingCapacity? OwnerTime { get; }
        // Null: forecast cannot be established; never present as zero or full.
        public int? OwnerTimeLimitedUnits { get; }
        public bool CanOperateToday { get; }

        internal BusinessOperatingReadiness(BusinessSnapshot business, OwnerDayAllocation day,
            BusinessEconomicProfile? economics)
        {
            Business = business ?? throw new ArgumentNullException(nameof(business));
            if (day == null) throw new ArgumentNullException(nameof(day));
            OwnerTime = day.Capacities.FirstOrDefault(x => x.BusinessInstanceId == business.InstanceId);
            OwnerTimeLimitedUnits = day.Status == OwnerDayPlanStatus.Ready &&
                OwnerTime != null && economics != null
                ? OwnerTime.EffectiveCapacity(economics.FullCapacityUnits) : (int?)null;
            CanOperateToday = business.IsActive &&
                business.OperationStatus == EconomicOperationStatusCode.Auto &&
                day.Status == OwnerDayPlanStatus.Ready &&
                OwnerTime != null && OwnerTime.AllocatedOwnerMinutes > 0;
        }
    }

    public sealed class BusinessOperationsSnapshot
    {
        public BusinessPortfolioSnapshot Portfolio { get; }
        public OwnerDayAllocation OwnerDay { get; }
        public IReadOnlyList<BusinessLaunchReadiness> LaunchOptions { get; }
        public IReadOnlyList<BusinessOperatingReadiness> OwnedBusinesses { get; }

        internal BusinessOperationsSnapshot(BusinessPortfolioSnapshot portfolio,
            OwnerDayAllocation day, IEnumerable<BusinessLaunchReadiness> launch,
            IEnumerable<BusinessOperatingReadiness> owned)
        {
            Portfolio = portfolio ?? throw new ArgumentNullException(nameof(portfolio));
            OwnerDay = day ?? throw new ArgumentNullException(nameof(day));
            LaunchOptions = Array.AsReadOnly((launch ?? throw new ArgumentNullException(nameof(launch))).ToArray());
            OwnedBusinesses = Array.AsReadOnly((owned ?? throw new ArgumentNullException(nameof(owned))).ToArray());
        }
    }

    public static class BusinessOperationsProjection
    {
        public static BusinessOperationsSnapshot FromCommittedState(GameState state,
            ContentCatalog content, BusinessEconomicCatalog? economics)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (content == null) throw new ArgumentNullException(nameof(content));
            var policies = state.EconomicV3?.EconomicRecords?.BusinessPolicies?
                .ToDictionary(x => x.InstanceId, StringComparer.Ordinal);
            var businesses = state.Businesses.Select(x =>
            {
                V3BusinessPolicyRecord? policy = null;
                if (policies != null) policies.TryGetValue(x.InstanceId, out policy);
                BusinessEconomicProfile? profile = null;
                if (economics != null) economics.Profiles.TryGetValue(x.DefinitionId, out profile);
                return new BusinessSnapshot(x, content.Businesses[x.DefinitionId],
                    policy, profile?.FixedHelper);
            }).ToArray();
            var portfolio = new BusinessPortfolioSnapshot(new GameSnapshot(state, content), businesses);
            var ownerDay = OwnerDayAllocator.Plan(state, content);
            var active = state.Businesses.Where(x => x.IsActive).ToArray();
            var launches = content.Businesses.Values.OrderBy(x => x.Id, StringComparer.Ordinal)
                .Select(x => new BusinessLaunchReadiness(x, state, content, active));
            var owned = businesses.Select(x =>
            {
                BusinessEconomicProfile? profile = null;
                if (economics != null) economics.Profiles.TryGetValue(x.DefinitionId, out profile);
                return new BusinessOperatingReadiness(x, ownerDay, profile);
            });
            return new BusinessOperationsSnapshot(portfolio, ownerDay, launches, owned);
        }
    }
}
