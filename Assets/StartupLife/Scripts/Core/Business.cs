#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StartupLife.Core
{
    public enum BusinessType
    {
        OnlineStore = 1,
        HomeFoodPreorder = 2,
        FreelanceService = 3,
        CoffeeKiosk = 4
    }

    public enum BusinessOperationMode
    {
        SideHustleCompatible = 1,
        FullTimeRequired = 2,
        ManagerOperable = 3
    }

    public enum PricingPosture
    {
        Budget = 1,
        Standard = 2,
        Premium = 3
    }

    public sealed class BusinessOperatingWindow
    {
        public DayOfWeek DayOfWeek { get; }
        public int StartMinute { get; }
        public int EndMinute { get; }
        public BusinessOperatingWindow(DayOfWeek dayOfWeek, int startMinute, int endMinute)
        {
            if (!Enum.IsDefined(typeof(DayOfWeek), dayOfWeek) || startMinute < 0 ||
                startMinute >= endMinute || endMinute > 1440)
                throw new ArgumentException("Invalid business operating window.");
            DayOfWeek = dayOfWeek; StartMinute = startMinute; EndMinute = endMinute;
        }
    }

    public sealed class BusinessOperatingRequirements
    {
        public IReadOnlyList<BusinessOperatingWindow> OperatingWindows { get; }
        public int RequiredOwnerMinutes { get; }
        public BusinessOperatingRequirements(IEnumerable<BusinessOperatingWindow> operatingWindows, int requiredOwnerMinutes)
        {
            var windows = operatingWindows?.ToArray() ?? throw new ArgumentNullException(nameof(operatingWindows));
            if (windows.Length == 0 || windows.Any(x => x == null) || requiredOwnerMinutes <= 0)
                throw new ArgumentException("Business operating requirements need windows and positive owner minutes.");
            windows = windows.OrderBy(x => (int)x.DayOfWeek).ThenBy(x => x.StartMinute).ThenBy(x => x.EndMinute).ToArray();
            for (var i = 1; i < windows.Length; i++)
                if (windows[i - 1].DayOfWeek == windows[i].DayOfWeek && windows[i].StartMinute < windows[i - 1].EndMinute)
                    throw new ArgumentException("Business operating windows overlap or repeat.");
            if (windows.GroupBy(x => x.DayOfWeek).Any(day => day.Sum(x => x.EndMinute - x.StartMinute) < requiredOwnerMinutes))
                throw new ArgumentException("A declared business weekday cannot meet required owner minutes.");
            OperatingWindows = Array.AsReadOnly(windows); RequiredOwnerMinutes = requiredOwnerMinutes;
        }
    }

    public sealed class BusinessDefinition
    {
        public string Id { get; }
        public string Revision { get; }
        public string NameKey { get; }
        public BusinessType Type { get; }
        public BusinessOperationMode OperationMode { get; }
        public long MinimumStartupInvestment { get; }
        public long MaximumStartupInvestment { get; }
        public IReadOnlyList<PricingPosture> AllowedPricingPostures { get; }
        public PricingPosture DefaultPricingPosture { get; }
        public long MinimumReinvestment { get; }
        public long MaximumReinvestment { get; }
        // Null explicitly retains the accepted M8-T01 mode-only policy for legacy revisions.
        public BusinessOperatingRequirements? OperatingRequirements { get; }

        public BusinessDefinition(string id, string revision, string nameKey, BusinessType type,
            BusinessOperationMode operationMode, long minimumStartupInvestment, long maximumStartupInvestment,
            IEnumerable<PricingPosture> allowedPricingPostures, PricingPosture defaultPricingPosture,
            long minimumReinvestment, long maximumReinvestment)
        {
            _ = new ContentId(id);
            var pricing = allowedPricingPostures?.ToArray() ?? throw new ArgumentNullException(nameof(allowedPricingPostures));
            if (string.IsNullOrWhiteSpace(revision) || string.IsNullOrWhiteSpace(nameKey) ||
                !Enum.IsDefined(typeof(BusinessType), type) || !Enum.IsDefined(typeof(BusinessOperationMode), operationMode) ||
                minimumStartupInvestment <= 0 || maximumStartupInvestment < minimumStartupInvestment ||
                minimumReinvestment <= 0 || maximumReinvestment < minimumReinvestment ||
                maximumStartupInvestment > int.MaxValue || maximumReinvestment > int.MaxValue ||
                pricing.Length == 0 || pricing.Distinct().Count() != pricing.Length ||
                pricing.Any(x => !Enum.IsDefined(typeof(PricingPosture), x)) || !pricing.Contains(defaultPricingPosture))
                throw new ArgumentException("Invalid business definition.");
            Id = id; Revision = revision; NameKey = nameKey; Type = type; OperationMode = operationMode;
            MinimumStartupInvestment = minimumStartupInvestment; MaximumStartupInvestment = maximumStartupInvestment;
            AllowedPricingPostures = Array.AsReadOnly((PricingPosture[])pricing.Clone());
            DefaultPricingPosture = defaultPricingPosture;
            MinimumReinvestment = minimumReinvestment; MaximumReinvestment = maximumReinvestment;
        }

        public BusinessDefinition(string id, string revision, string nameKey, BusinessType type,
            BusinessOperationMode operationMode, long minimumStartupInvestment, long maximumStartupInvestment,
            IEnumerable<PricingPosture> allowedPricingPostures, PricingPosture defaultPricingPosture,
            long minimumReinvestment, long maximumReinvestment, BusinessOperatingRequirements operatingRequirements)
            : this(id, revision, nameKey, type, operationMode, minimumStartupInvestment, maximumStartupInvestment,
                allowedPricingPostures, defaultPricingPosture, minimumReinvestment, maximumReinvestment)
        {
            OperatingRequirements = operatingRequirements ?? throw new ArgumentNullException(nameof(operatingRequirements));
        }
    }

    public enum BusinessEligibilityStatus
    {
        Eligible = 1,
        UnsupportedOperationMode = 2,
        EmploymentIncompatible = 3,
        InsufficientOwnerTime = 4,
        BusinessDefinitionUnavailable = 5,
        BusinessRevisionUnavailable = 6
    }

    public sealed class BusinessEligibilityResult
    {
        public BusinessEligibilityStatus Status { get; }
        public bool IsEligible => Status == BusinessEligibilityStatus.Eligible;
        public string ReasonKey => Status switch
        {
            BusinessEligibilityStatus.Eligible => "",
            BusinessEligibilityStatus.UnsupportedOperationMode => "business.manager_unsupported",
            BusinessEligibilityStatus.EmploymentIncompatible => "business.employment_incompatible",
            BusinessEligibilityStatus.InsufficientOwnerTime => "business.insufficient_owner_time",
            BusinessEligibilityStatus.BusinessDefinitionUnavailable => "content.missing",
            BusinessEligibilityStatus.BusinessRevisionUnavailable => "business.definition_revision",
            _ => throw new InvalidOperationException("Invalid business eligibility status.")
        };
        public int? RequiredOwnerMinutes { get; }
        public int? AvailableOwnerMinutes { get; }
        public BusinessEligibilityResult(BusinessEligibilityStatus status, int? requiredOwnerMinutes = null, int? availableOwnerMinutes = null)
        {
            if (!Enum.IsDefined(typeof(BusinessEligibilityStatus), status) || requiredOwnerMinutes <= 0 || availableOwnerMinutes < 0 ||
                (availableOwnerMinutes.HasValue && !requiredOwnerMinutes.HasValue) ||
                (status == BusinessEligibilityStatus.InsufficientOwnerTime && (!availableOwnerMinutes.HasValue || availableOwnerMinutes >= requiredOwnerMinutes)) ||
                (status == BusinessEligibilityStatus.Eligible && availableOwnerMinutes.HasValue && availableOwnerMinutes < requiredOwnerMinutes) ||
                (status != BusinessEligibilityStatus.Eligible && status != BusinessEligibilityStatus.InsufficientOwnerTime && availableOwnerMinutes.HasValue) ||
                ((status == BusinessEligibilityStatus.BusinessDefinitionUnavailable || status == BusinessEligibilityStatus.BusinessRevisionUnavailable) && requiredOwnerMinutes.HasValue))
                throw new ArgumentException("Invalid business eligibility result.");
            Status = status; RequiredOwnerMinutes = requiredOwnerMinutes; AvailableOwnerMinutes = availableOwnerMinutes;
        }
    }

    public sealed class BusinessEligibilitySnapshot
    {
        public string DefinitionId { get; }
        public string DefinitionRevision { get; }
        public SimDate Date { get; }
        public long StateRevision { get; }
        public BusinessEligibilityResult Eligibility { get; }
        public BusinessEligibilitySnapshot(string definitionId, string definitionRevision, SimDate date, long stateRevision,
            BusinessEligibilityResult eligibility)
        {
            DefinitionId = definitionId; DefinitionRevision = definitionRevision; Date = date; StateRevision = stateRevision;
            Eligibility = eligibility ?? throw new ArgumentNullException(nameof(eligibility));
        }
    }

    public interface IBusinessEligibilityReadModel
    {
        BusinessEligibilitySnapshot ReadEligibility(string definitionId, string definitionRevision);
    }

    public sealed class BusinessState
    {
        public string InstanceId { get; set; } = "";
        public string DefinitionId { get; set; } = "";
        public string DefinitionRevision { get; set; } = "";
        public string OpenedIso { get; set; } = "";
        public int OpenedMinute { get; set; }
        public string? ClosedIso { get; set; }
        public int? ClosedMinute { get; set; }
        public PricingPosture PricingPosture { get; set; }
        public long InitialInvestment { get; set; }
        public long ReinvestedAmount { get; set; }
        public bool IsActive => ClosedIso == null && ClosedMinute == null;
        public long TotalInvestment => checked(InitialInvestment + ReinvestedAmount);
    }

    public sealed class BusinessSnapshot
    {
        public string InstanceId { get; }
        public string DefinitionId { get; }
        public string DefinitionRevision { get; }
        public string NameKey { get; }
        public BusinessType Type { get; }
        public bool IsActive { get; }
        public SimInstant Opened { get; }
        public SimInstant? Closed { get; }
        public PricingPosture PricingPosture { get; }
        public long InitialInvestment { get; }
        public long ReinvestedAmount { get; }
        public long TotalInvestment { get; }

        public BusinessSnapshot(BusinessState state, BusinessDefinition definition)
        {
            InstanceId = state.InstanceId; DefinitionId = state.DefinitionId; DefinitionRevision = state.DefinitionRevision;
            NameKey = definition.NameKey; Type = definition.Type; IsActive = state.IsActive;
            Opened = new SimInstant(ParseDate(state.OpenedIso), state.OpenedMinute);
            Closed = state.IsActive ? (SimInstant?)null : new SimInstant(ParseDate(state.ClosedIso!), state.ClosedMinute!.Value);
            PricingPosture = state.PricingPosture; InitialInvestment = state.InitialInvestment;
            ReinvestedAmount = state.ReinvestedAmount; TotalInvestment = checked(state.InitialInvestment + state.ReinvestedAmount);
        }

        private static SimDate ParseDate(string value)
        {
            var date = DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            return new SimDate(date.Year, date.Month, date.Day);
        }
    }

    public sealed class BusinessPortfolioSnapshot
    {
        public GameSnapshot Game { get; }
        public IReadOnlyList<BusinessSnapshot> Businesses { get; }
        public BusinessPortfolioSnapshot(GameSnapshot game, IEnumerable<BusinessSnapshot> businesses)
        {
            Game = game ?? throw new ArgumentNullException(nameof(game));
            Businesses = Array.AsReadOnly((businesses ?? throw new ArgumentNullException(nameof(businesses))).ToArray());
        }
    }

    public interface IBusinessReadModel
    {
        BusinessPortfolioSnapshot ReadBusinesses();
    }

    public static class BusinessCommands
    {
        public static GameCommand Launch(string definitionId, string definitionRevision, int initialInvestment) =>
            new GameCommand(CommandKind.LaunchBusiness, definitionId, initialInvestment, "", definitionRevision);
        public static GameCommand Reinvest(string instanceId, int amount) =>
            new GameCommand(CommandKind.ReinvestBusiness, instanceId, amount);
        public static GameCommand SetPricing(string instanceId, PricingPosture pricing) =>
            new GameCommand(CommandKind.SetBusinessPricing, instanceId, (int)pricing);
        public static GameCommand Close(string instanceId) =>
            new GameCommand(CommandKind.CloseBusiness, instanceId);
        public static GameCommand Pause(string instanceId) =>
            new GameCommand(CommandKind.PauseBusiness, instanceId);
        public static GameCommand Resume(string instanceId) =>
            new GameCommand(CommandKind.ResumeBusiness, instanceId);
    }

    public sealed class BusinessHistoryRecord
    {
        public string Action { get; }
        public string OperationId { get; }
        public string InstanceId { get; }
        public string DateIso { get; }
        public int Minute { get; }
        public long Amount { get; }
        public PricingPosture Pricing { get; }
        public BusinessHistoryRecord(string action, string operationId, string instanceId, string dateIso,
            int minute, long amount, PricingPosture pricing)
        {
            Action = action; OperationId = operationId; InstanceId = instanceId; DateIso = dateIso;
            Minute = minute; Amount = amount; Pricing = pricing;
        }
    }

    public static class BusinessHistory
    {
        public static string Encode(string action, string operationId, string instanceId, string dateIso,
            int minute, long amount, PricingPosture pricing) =>
            action + ":" + Field(operationId) + Field(instanceId) + Field(dateIso) +
            Field(minute.ToString(CultureInfo.InvariantCulture)) + Field(amount.ToString(CultureInfo.InvariantCulture)) +
            Field(((int)pricing).ToString(CultureInfo.InvariantCulture));

        public static bool TryParse(string value, out BusinessHistoryRecord? record)
        {
            record = null;
            foreach (var action in new[] { "business.launched", "business.reinvested", "business.pricing_changed", "business.closed" })
            {
                var prefix = action + ":";
                if (!value.StartsWith(prefix, StringComparison.Ordinal)) continue;
                var position = prefix.Length;
                try
                {
                    var operation = ReadField(value, ref position);
                    var instance = ReadField(value, ref position);
                    var date = ReadField(value, ref position);
                    var minuteText = ReadField(value, ref position);
                    var amountText = ReadField(value, ref position);
                    var pricingText = ReadField(value, ref position);
                    if (position != value.Length ||
                        !int.TryParse(minuteText, NumberStyles.None, CultureInfo.InvariantCulture, out var minute) || minute < 0 || minute >= 1440 ||
                        minuteText != minute.ToString(CultureInfo.InvariantCulture) ||
                        !long.TryParse(amountText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var amount) ||
                        amountText != amount.ToString(CultureInfo.InvariantCulture) ||
                        !int.TryParse(pricingText, NumberStyles.None, CultureInfo.InvariantCulture, out var pricingCode) ||
                        !Enum.IsDefined(typeof(PricingPosture), pricingCode) ||
                        pricingText != pricingCode.ToString(CultureInfo.InvariantCulture))
                        return false;
                    _ = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    record = new BusinessHistoryRecord(action, operation, instance, date, minute, amount, (PricingPosture)pricingCode);
                    return true;
                }
                catch (ArgumentException) { return false; }
            }
            return false;
        }

        private static string Field(string value) => value.Length.ToString(CultureInfo.InvariantCulture) + ":" + value;
        private static string ReadField(string source, ref int position)
        {
            var separator = source.IndexOf(':', position);
            if (separator < position || !int.TryParse(source.Substring(position, separator - position), NumberStyles.None,
                CultureInfo.InvariantCulture, out var length) || length < 0 || separator + 1 + length > source.Length)
                throw new ArgumentException("Invalid business history field.");
            position = separator + 1;
            var value = source.Substring(position, length);
            position += length;
            return value;
        }
    }
}
