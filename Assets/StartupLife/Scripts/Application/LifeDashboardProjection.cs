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

        internal CourseDashboardItem(CourseDefinition definition, ActiveCourseSnapshot? active)
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
        public IReadOnlyList<SkillDashboardItem> Skills { get; }
        public IReadOnlyList<CourseDashboardItem> Courses { get; }
        public FinanceDashboardSnapshot Finance { get; }

        internal LifeDashboardSnapshot(GameSnapshot game, CareerBiographySnapshot career,
            IEnumerable<SkillDashboardItem> skills, IEnumerable<CourseDashboardItem> courses,
            FinanceDashboardSnapshot finance)
        {
            Game = game;
            Career = career;
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
            var skillMap = state.Skills.ToDictionary(x => x.Id, StringComparer.Ordinal);
            var skills = content.Skills.Values.OrderBy(x => x.Id, StringComparer.Ordinal).Select(x =>
            {
                skillMap.TryGetValue(x.Id, out var value);
                return new SkillDashboardItem(x, value);
            });
            var courses = content.Courses.Values.OrderBy(x => x.Id, StringComparer.Ordinal)
                .Select(x => new CourseDashboardItem(x, game.ActiveCourse));
            var settled = state.SaveVersion == 3 || (state.SaveVersion == 4 &&
                state.EventV4?.Journal != null)
                ? BusinessFinanceProjection.FromCommittedState(state)
                : Array.Empty<BusinessFinanceDaySnapshot>();
            return new LifeDashboardSnapshot(game, career, skills, courses,
                new FinanceDashboardSnapshot(state, career, settled));
        }
    }
}
