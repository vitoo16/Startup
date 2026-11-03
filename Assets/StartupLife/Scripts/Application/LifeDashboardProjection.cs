#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Application
{
    // Presentation-friendly immutable data. It never performs commands, forecasts
    // sales or rewrites gameplay/save state.
    public sealed class SkillDashboardItem
    {
        public string SkillId { get; }
        public string NameKey { get; }
        public int Level { get; }
        public int GrantedLevel { get; }
        public long Exposure { get; }
        public long? NextLevelExposure { get; }

        internal SkillDashboardItem(SkillDefinition definition, SkillState? value)
        {
            SkillId = definition.Id;
            NameKey = definition.NameKey;
            Exposure = value?.Exposure ?? 0;
            GrantedLevel = value?.GrantedLevel ?? 0;
            Level = GameSnapshot.Level(value ?? new SkillState { Id = definition.Id }, definition);
            NextLevelExposure = Level < definition.Thresholds.Count
                ? definition.Thresholds[Level] : (long?)null;
        }
    }

    // Advisory profession-to-business affinities from GDD §13.
    // Recommendations are derived only from immutable content tags; they are
    // not eligibility checks, unlocks, discounts or revenue modifiers.
    // Any future economic effect requires a separately versioned ruleset.
    public static class CareerBusinessAffinityPolicy
    {
        public static IReadOnlyList<string> ForCareer(string careerId, ContentCatalog content)
        {
            if (careerId == null) throw new ArgumentNullException(nameof(careerId));
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (!content.Careers.TryGetValue(careerId, out var career) ||
                career.BusinessAffinityTags.Count == 0)
                return Array.Empty<string>();

            var tags = new HashSet<string>(career.BusinessAffinityTags, StringComparer.Ordinal);
            return Array.AsReadOnly(content.Businesses.Values
                .Where(x => x.BusinessAffinityTags.Any(tags.Contains))
                .Select(x => x.Id)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        }

        public static bool IsPreferred(string careerId, string businessId, ContentCatalog content)
        {
            if (businessId == null) throw new ArgumentNullException(nameof(businessId));
            return ForCareer(careerId, content).Contains(businessId);
        }
    }

    // Read-only catalog offer. No employment data, rank change or career
    // prerequisites are invented by presentation; AcceptJob remains authoritative.
    public sealed class CareerDashboardItem
    {
        public string CareerId { get; }
        public string NameKey { get; }
        public IReadOnlyList<long> MonthlySalariesVnd { get; }
        public long StartingMonthlySalaryVnd => MonthlySalariesVnd[0];
        public int StartMinute { get; }
        public int EndMinute { get; }
        public IReadOnlyList<DayOfWeek> WorkDays { get; }
        public IReadOnlyList<string> PreferredBusinessIds { get; }

        internal CareerDashboardItem(CareerDefinition definition, ContentCatalog content)
        {
            CareerId = definition.Id;
            NameKey = definition.NameKey;
            MonthlySalariesVnd = Array.AsReadOnly(definition.Ranks.Select(x => x.MonthlySalary).ToArray());
            StartMinute = definition.StartMinute;
            EndMinute = definition.EndMinute;
            WorkDays = Array.AsReadOnly(definition.WorkDays.ToArray());
            PreferredBusinessIds = CareerBusinessAffinityPolicy.ForCareer(definition.Id, content);
        }
    }

    // Informational promotion progress only. The simulation decides when a
    // qualifying shift applies the next rank; calendar tenure is not a count
    // of completed work shifts.
    public sealed class CareerPromotionDashboardItem
    {
        public int NextRankNumber { get; }
        public long NextMonthlySalaryVnd { get; }
        public long MissingCareerXp { get; }
        public int MissingCalendarDays { get; }

        internal CareerPromotionDashboardItem(
            EmploymentState employment, CareerDefinition definition, SimDate today)
        {
            var next = definition.Ranks[employment.Rank + 1];
            NextRankNumber = employment.Rank + 2;
            NextMonthlySalaryVnd = next.MonthlySalary;
            MissingCareerXp = Math.Max(0L, next.RequiredXp - employment.Xp);
            var elapsed = (DateTime.ParseExact(today.ToString(), "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture) -
                DateTime.ParseExact(employment.StartedIso, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture)).Days;
            MissingCalendarDays = Math.Max(0, next.RequiredServiceDays - Math.Max(0, elapsed));
        }
    }

    public sealed class CourseDashboardItem
    {
        public string CourseId { get; }
        public string NameKey { get; }
        public string SkillId { get; }
        public int TargetLevel { get; }
        public int PrerequisiteLevel { get; }
        public long PriceVnd { get; }
        public int BaseStudyMinutes { get; }
        public bool IsActive { get; }
        public long? ProgressUnits { get; }
        public long? TargetUnits { get; }
        public long? StudyMinutesRemaining { get; }
        public long? Minimum60MinuteSessions { get; }

        internal CourseDashboardItem(CourseDefinition definition, ActiveCourseSnapshot? active, int learningSpeed)
        {
            CourseId = definition.Id;
            NameKey = definition.NameKey;
            SkillId = definition.SkillId;
            TargetLevel = definition.TargetLevel;
            PrerequisiteLevel = definition.PrerequisiteLevel;
            PriceVnd = definition.Price;
            BaseStudyMinutes = definition.BaseMinutes;
            IsActive = active?.DefinitionId == CourseId;
            ProgressUnits = IsActive ? active!.ProgressUnits : (long?)null;
            TargetUnits = IsActive ? active!.TargetUnits : (long?)null;
            if (IsActive && learningSpeed > 0 && TargetUnits.HasValue)
            {
                var unitsRemaining = Math.Max(0L, TargetUnits.Value - ProgressUnits!.Value);
                // Same integer ceiling as SimulationEngine.Study, expressed in
                // actual study minutes after the character's learning-speed modifier.
                StudyMinutesRemaining = unitsRemaining == 0 ? 0 :
                    1 + (unitsRemaining - 1) / learningSpeed;
                Minimum60MinuteSessions = StudyMinutesRemaining.Value == 0 ? 0 :
                    1 + (StudyMinutesRemaining.Value - 1) / 60;
            }
        }
    }

    public sealed class ArrearsDashboardItem
    {
        public string ArrearId { get; }
        public string DueIso { get; }
        public long OutstandingVnd { get; }

        internal ArrearsDashboardItem(ArrearState state)
        {
            ArrearId = state.Id;
            DueIso = state.DueIso;
            OutstandingVnd = state.Amount;
        }
    }

    public sealed class FinanceDashboardSnapshot
    {
        public long CashVnd { get; }
        public long OutstandingArrearsVnd { get; }
        public RationalAmount UnpaidSalaryExact { get; }
        public long CommittedBusinessGrossVnd { get; }
        public long CommittedBusinessProfitVnd { get; }
        public IReadOnlyList<ArrearsDashboardItem> Arrears { get; }
        public IReadOnlyList<BusinessFinanceDaySnapshot> SettledBusinessDays { get; }

        internal FinanceDashboardSnapshot(GameState state, CareerBiographySnapshot biography,
            IReadOnlyList<BusinessFinanceDaySnapshot> committed)
        {
            CashVnd = state.Cash;
            UnpaidSalaryExact = biography.OutstandingSalary;
            Arrears = Array.AsReadOnly(state.Arrears.OrderBy(x => x.DueIso, StringComparer.Ordinal)
                .ThenBy(x => x.Id, StringComparer.Ordinal)
                .Select(x => new ArrearsDashboardItem(x)).ToArray());
            OutstandingArrearsVnd = checked(Arrears.Sum(x => x.OutstandingVnd));
            SettledBusinessDays = Array.AsReadOnly(committed.ToArray());
            CommittedBusinessGrossVnd = checked(SettledBusinessDays.Sum(x => x.GrossRevenueVnd));
            CommittedBusinessProfitVnd = checked(SettledBusinessDays.Sum(x => x.ProfitVnd));
        }
    }

    public sealed class LifeDashboardSnapshot
    {
        public GameSnapshot Game { get; }
        public CareerBiographySnapshot Career { get; }
        public CareerPromotionDashboardItem? CareerPromotion { get; }
        public IReadOnlyList<CareerDashboardItem> Careers { get; }
        public IReadOnlyList<SkillDashboardItem> Skills { get; }
        public IReadOnlyList<CourseDashboardItem> Courses { get; }
        public FinanceDashboardSnapshot Finance { get; }

        internal LifeDashboardSnapshot(GameSnapshot game, CareerBiographySnapshot career,
            CareerPromotionDashboardItem? careerPromotion,
            IEnumerable<CareerDashboardItem> careers, IEnumerable<SkillDashboardItem> skills,
            IEnumerable<CourseDashboardItem> courses, FinanceDashboardSnapshot finance)
        {
            Game = game;
            Career = career;
            CareerPromotion = careerPromotion;
            Careers = Array.AsReadOnly(careers.ToArray());
            Skills = Array.AsReadOnly(skills.ToArray());
            Courses = Array.AsReadOnly(courses.ToArray());
            Finance = finance;
        }
    }

    public static class LifeDashboardProjection
    {
        public static LifeDashboardSnapshot FromCommittedState(GameState state, ContentCatalog content)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (content == null) throw new ArgumentNullException(nameof(content));
            var game = new GameSnapshot(state, content);
            var career = CareerBiographyProjection.FromCommittedState(state, content);
            var careers = content.Careers.Values.OrderBy(x => x.Id, StringComparer.Ordinal)
                .Select(x => new CareerDashboardItem(x, content));
            var skillMap = state.Skills.ToDictionary(x => x.Id, StringComparer.Ordinal);
            var skills = content.Skills.Values.OrderBy(x => x.Id, StringComparer.Ordinal).Select(x =>
            {
                skillMap.TryGetValue(x.Id, out var value);
                return new SkillDashboardItem(x, value);
            });
            var courses = content.Courses.Values.OrderBy(x => x.Id, StringComparer.Ordinal)
                .Select(x => new CourseDashboardItem(x, game.ActiveCourse, state.LearningSpeed));
            CareerPromotionDashboardItem? careerPromotion = null;
            var current = state.Employment;
            if (current != null && content.Careers.TryGetValue(current.CareerId, out var currentDefinition) &&
                current.DefinitionRevision == currentDefinition.Revision &&
                current.Rank >= 0 && current.Rank + 1 < currentDefinition.Ranks.Count)
                careerPromotion = new CareerPromotionDashboardItem(current, currentDefinition, state.Date);
            var settled = state.SaveVersion == 3 || (state.SaveVersion == 4 &&
                state.EventV4?.Journal != null)
                ? BusinessFinanceProjection.FromCommittedState(state)
                : Array.Empty<BusinessFinanceDaySnapshot>();
            return new LifeDashboardSnapshot(game, career, careerPromotion, careers, skills, courses,
                new FinanceDashboardSnapshot(state, career, settled));
        }
    }
}
