#nullable enable
using System;
using System.Linq;

namespace StartupLife.Application
{
    // Read-model guidance ONLY. SimulationEngine.Purchase remains the sole
    // authority that commits tuition, prerequisites and save-state changes.
    public enum CoursePurchaseAvailability
    {
        Available,
        AnotherCourseActive,
        PrerequisiteNotMet,
        TargetLevelReached,
        OutstandingArrears,
        InsufficientCash,
        MissingSkillDefinition
    }

    public static class CourseEnrollmentPolicy
    {
        public static CoursePurchaseAvailability Evaluate(
            LifeDashboardSnapshot dashboard, CourseDashboardItem course)
        {
            if (dashboard == null) throw new ArgumentNullException(nameof(dashboard));
            if (course == null) throw new ArgumentNullException(nameof(course));

            // Match the simulation Purchase command's rejection precedence.
            if (dashboard.Game.ActiveCourse != null)
                return CoursePurchaseAvailability.AnotherCourseActive;

            var skill = dashboard.Skills.FirstOrDefault(
                value => string.Equals(value.SkillId, course.SkillId, StringComparison.Ordinal));
            if (skill == null)
                return CoursePurchaseAvailability.MissingSkillDefinition;
            if (skill.Level < course.PrerequisiteLevel)
                return CoursePurchaseAvailability.PrerequisiteNotMet;
            if (skill.Level >= course.TargetLevel)
                return CoursePurchaseAvailability.TargetLevelReached;
            if (dashboard.Finance.Arrears.Count != 0)
                return CoursePurchaseAvailability.OutstandingArrears;
            if (dashboard.Finance.CashVnd < course.PriceVnd)
                return CoursePurchaseAvailability.InsufficientCash;

            return CoursePurchaseAvailability.Available;
        }

        // Selection is transient UI state. On restore the authoritative active
        // course wins; otherwise keep the user's existing choice where valid.
        // Never mutate or persist catalog IDs from presentation.
        public static string? ChooseCourseId(
            LifeDashboardSnapshot dashboard, string? selectedCourseId)
        {
            if (dashboard == null) throw new ArgumentNullException(nameof(dashboard));
            if (dashboard.Courses.Count == 0) return null;

            var activeId = dashboard.Game.ActiveCourse?.DefinitionId;
            if (activeId != null && dashboard.Courses.Any(x =>
                string.Equals(x.CourseId, activeId, StringComparison.Ordinal)))
                return activeId;

            if (selectedCourseId != null && dashboard.Courses.Any(x =>
                string.Equals(x.CourseId, selectedCourseId, StringComparison.Ordinal)))
                return selectedCourseId;

            var firstAvailable = dashboard.Courses.FirstOrDefault(x =>
                Evaluate(dashboard, x) == CoursePurchaseAvailability.Available);
            return (firstAvailable ?? dashboard.Courses[0]).CourseId;
        }
    }
}
