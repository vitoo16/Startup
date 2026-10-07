#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Content
{
    [Serializable]
    public sealed class SkillContentSource
    {
        public string Id = "";
        public string NameKey = "";
        public long[] Thresholds = Array.Empty<long>();

        public SkillDefinition Build() => new SkillDefinition(Id, NameKey, Required(Thresholds, nameof(Thresholds)));
        private static long[] Required(long[]? values, string field) =>
            values ?? throw new ArgumentException(field + " is required.");
    }

    [Serializable]
    public sealed class CareerSceneContentSource
    {
        public string Id = "";
        public string NameKey = "";
        public int Weight;
        public long CareerXp;
        public string SkillId = "";
        public long Exposure;

        public CareerSceneDefinition Build() =>
            new CareerSceneDefinition(Id, NameKey, Weight, CareerXp, SkillId, Exposure);
    }

    [Serializable]
    public sealed class CareerRankContentSource
    {
        public string Id = "";
        public long RequiredXp;
        public int RequiredServiceDays;
        public long MonthlySalary;
        public string GrantSkillId = "";
        public int GrantLevel;

        public CareerRankDefinition Build() =>
            new CareerRankDefinition(Id, RequiredXp, RequiredServiceDays, MonthlySalary, GrantSkillId, GrantLevel);
    }

    [Serializable]
    public sealed class CareerContentSource
    {
        public string Id = "";
        public string Revision = "";
        public string NameKey = "";
        public int StartMinute;
        public int EndMinute;
        public int ScenesPerShift;
        public int QuotaSlots;
        public DayOfWeek[] WorkDays = Array.Empty<DayOfWeek>();
        public CareerSceneContentSource[] Scenes = Array.Empty<CareerSceneContentSource>();
        public CareerRankContentSource[] Ranks = Array.Empty<CareerRankContentSource>();

        public CareerDefinition Build() =>
            new CareerDefinition(
                Id,
                Revision,
                NameKey,
                StartMinute,
                EndMinute,
                ScenesPerShift,
                QuotaSlots,
                Required(WorkDays, nameof(WorkDays)),
                Required(Scenes, nameof(Scenes)).Select(Require).Select(x => x.Build()),
                Required(Ranks, nameof(Ranks)).Select(Require).Select(x => x.Build()));

        private static T[] Required<T>(T[]? values, string field) =>
            values ?? throw new ArgumentException(field + " is required.");
        private static T Require<T>(T? value) where T : class =>
            value ?? throw new ArgumentException("Content source entry cannot be null.");
    }

    [Serializable]
    public sealed class CourseContentSource
    {
        public string Id = "";
        public string NameKey = "";
        public string SkillId = "";
        public int TargetLevel;
        public int PrerequisiteLevel;
        public int BaseMinutes;
        public long Price;

        public CourseDefinition Build() =>
            new CourseDefinition(Id, NameKey, SkillId, TargetLevel, PrerequisiteLevel, BaseMinutes, Price);
    }

    [Serializable]
    public sealed class CharacterStartContentSource
    {
        public string Id = "";
        public long Cash;
        public int LearningSpeed;
        public string[] AppearanceIds = Array.Empty<string>();

        public CharacterStartDefinition Build() =>
            new CharacterStartDefinition(
                Id,
                Cash,
                LearningSpeed,
                AppearanceIds ?? throw new ArgumentException(nameof(AppearanceIds) + " is required."));
    }

    [Serializable]
    public sealed class EconomyContentSource
    {
        public int Payday = 1;
        public int ExpenseDay = 1;
        public long LivingCost;

        public EconomyBalanceDefinition Build() =>
            new EconomyBalanceDefinition(Payday, ExpenseDay, LivingCost);
    }

    [Serializable]
    public sealed class DayScheduleContentSource
    {
        public int WakeMinute = 480;
        public int SleepMinute = 1320;

        public DayScheduleDefinition Build() => new DayScheduleDefinition(WakeMinute, SleepMinute);
    }

    [Serializable]
    public sealed class BusinessOperatingWindowContentSource
    {
        public DayOfWeek DayOfWeek;
        public int StartMinute;
        public int EndMinute;
        public BusinessOperatingWindow Build() => new BusinessOperatingWindow(DayOfWeek, StartMinute, EndMinute);
    }

    [Serializable]
    public sealed class BusinessContentSource
    {
        public string Id = "";
        public string Revision = "";
        public string NameKey = "";
        public BusinessType Type;
        public BusinessOperationMode OperationMode;
        public long MinimumStartupInvestment;
        public long MaximumStartupInvestment;
        public PricingPosture[] AllowedPricingPostures = Array.Empty<PricingPosture>();
        public PricingPosture DefaultPricingPosture;
        public long MinimumReinvestment;
        public long MaximumReinvestment;
        public BusinessOperatingWindowContentSource[] OperatingWindows = Array.Empty<BusinessOperatingWindowContentSource>();
        public int RequiredOwnerMinutes;

        public BusinessDefinition Build()
        {
            if (OperatingWindows == null) throw new ArgumentException("Business operating windows are required.");
            var windows = OperatingWindows.Select(x => (x ?? throw new ArgumentException("Business window entry cannot be null.")).Build());
            return new BusinessDefinition(Id, Revision, NameKey, Type, OperationMode,
                MinimumStartupInvestment, MaximumStartupInvestment, AllowedPricingPostures, DefaultPricingPosture,
                MinimumReinvestment, MaximumReinvestment, new BusinessOperatingRequirements(windows, RequiredOwnerMinutes));
        }
    }

    [Serializable]
    public sealed class ContentCatalogSource
    {
        public string Version = "";
        public SkillContentSource[] Skills = Array.Empty<SkillContentSource>();
        public CareerContentSource[] Careers = Array.Empty<CareerContentSource>();
        public CourseContentSource[] Courses = Array.Empty<CourseContentSource>();
        public CharacterStartContentSource[] Starts = Array.Empty<CharacterStartContentSource>();
        public BusinessContentSource[] Businesses = Array.Empty<BusinessContentSource>();
        public EconomyContentSource Economy = new EconomyContentSource();
        public DayScheduleContentSource Schedule = new DayScheduleContentSource();

        public ContentCatalog Build()
        {
            if (Economy == null) throw new ArgumentException("Economy content is required.");
            if (Schedule == null) throw new ArgumentException("Schedule content is required.");
            return new ContentCatalog(
                Version,
                BuildAll(Skills, x => x.Build(), nameof(Skills)),
                BuildAll(Careers, x => x.Build(), nameof(Careers)),
                BuildAll(Courses, x => x.Build(), nameof(Courses)),
                BuildAll(Starts, x => x.Build(), nameof(Starts)),
                BuildAll(Businesses, x => x.Build(), nameof(Businesses)),
                Economy.Build(),
                Schedule.Build());
        }

        private static IEnumerable<TResult> BuildAll<TSource, TResult>(
            TSource[]? source,
            Func<TSource, TResult> build,
            string field)
            where TSource : class
        {
            if (source == null) throw new ArgumentException(field + " is required.");
            for (var i = 0; i < source.Length; i++)
            {
                var item = source[i] ?? throw new ArgumentException(field + " contains a null entry.");
                yield return build(item);
            }
        }
    }
}
