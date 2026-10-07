#nullable enable
using System;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // Individual whole-date compatibility only. No reservations or portfolio allocation.
    public static class BusinessEligibilityEvaluator
    {
        public static BusinessEligibilityResult Evaluate(ContentCatalog content, GameState state,
            string definitionId, string definitionRevision)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (!ContentId.IsValid(definitionId) || string.IsNullOrWhiteSpace(definitionRevision))
                throw new ArgumentException("A valid business ID and nonblank revision are required.");
            if (!content.Businesses.TryGetValue(definitionId, out var business))
                return new BusinessEligibilityResult(BusinessEligibilityStatus.BusinessDefinitionUnavailable);
            if (business.Revision != definitionRevision)
                return new BusinessEligibilityResult(BusinessEligibilityStatus.BusinessRevisionUnavailable);
            var requirements = business.OperatingRequirements;
            var required = requirements?.RequiredOwnerMinutes;
            if (business.OperationMode == BusinessOperationMode.ManagerOperable)
                return new BusinessEligibilityResult(BusinessEligibilityStatus.UnsupportedOperationMode, required);
            if (business.OperationMode == BusinessOperationMode.FullTimeRequired && state.Employment != null)
                return new BusinessEligibilityResult(BusinessEligibilityStatus.EmploymentIncompatible, required);
            if (requirements == null) return new BusinessEligibilityResult(BusinessEligibilityStatus.Eligible);

            CareerDefinition? mandatoryCareer = null;
            var date = state.Date;
            var employment = state.Employment;
            if (employment != null)
            {
                if (!content.Careers.TryGetValue(employment.CareerId, out var career))
                    throw new ContentCompatibilityException("save.content_id", "Required career definition is unavailable.");
                if (career.Revision != employment.DefinitionRevision)
                    throw new ContentCompatibilityException("save.content_revision", "Required career revision is unavailable.");
                if (string.CompareOrdinal(state.DateIso, employment.FirstShiftIso) >= 0 && career.WorksOn(date))
                    mandatoryCareer = career;
            }
            var available = 0;
            foreach (var window in requirements.OperatingWindows)
            {
                if (window.DayOfWeek != date.DayOfWeek) continue;
                var start = Math.Max(window.StartMinute, content.Schedule.WakeMinute);
                var end = Math.Min(window.EndMinute, content.Schedule.SleepMinute);
                if (start >= end) continue;
                var overlap = mandatoryCareer == null ? 0 : Math.Max(0,
                    Math.Min(end, mandatoryCareer.EndMinute) - Math.Max(start, mandatoryCareer.StartMinute));
                available = checked(available + end - start - overlap);
            }
            return new BusinessEligibilityResult(available >= requirements.RequiredOwnerMinutes
                ? BusinessEligibilityStatus.Eligible : BusinessEligibilityStatus.InsufficientOwnerTime,
                requirements.RequiredOwnerMinutes, available);
        }
    }

    internal static class EmploymentSchedule
    {
        public static SimDate FirstShift(CareerDefinition career, SimDate acceptanceDate, int acceptanceMinute)
        {
            var first = acceptanceMinute > career.StartMinute ? acceptanceDate.AddDays(1) : acceptanceDate;
            while (!career.WorksOn(first)) first = first.AddDays(1);
            return first;
        }
    }
}
