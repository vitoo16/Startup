#nullable enable
using System;
using System.Linq;

namespace StartupLife.Application
{
    // Advisory display state only. SimulationEngine.Accept alone can commit
    // employment. In particular it does NOT require tuition, cash or a skill.
    public enum CareerAcceptanceAvailability
    {
        Available,
        CharacterRequired,
        AlreadyEmployed
    }

    public static class CareerSelectionPolicy
    {
        public static CareerAcceptanceAvailability Evaluate(
            LifeDashboardSnapshot dashboard, CareerDashboardItem offer)
        {
            if (dashboard == null) throw new ArgumentNullException(nameof(dashboard));
            if (offer == null) throw new ArgumentNullException(nameof(offer));
            if (!dashboard.Career.HasCharacter)
                return CareerAcceptanceAvailability.CharacterRequired;
            if (dashboard.Career.IsEmployed)
                return CareerAcceptanceAvailability.AlreadyEmployed;
            return CareerAcceptanceAvailability.Available;
        }

        public static string? ChooseCareerId(
            LifeDashboardSnapshot dashboard, string? preferredId)
        {
            if (dashboard == null) throw new ArgumentNullException(nameof(dashboard));
            if (dashboard.Careers.Count == 0) return null;

            // Restore the *actual* job on a save or after hiring. The manual
            // UI selection does not become an additional persisted game state.
            var employedId = dashboard.Career.CurrentEmployment?.CareerId;
            if (employedId != null && dashboard.Careers.Any(x =>
                string.Equals(x.CareerId, employedId, StringComparison.Ordinal)))
                return employedId;

            if (preferredId != null && dashboard.Careers.Any(x =>
                string.Equals(x.CareerId, preferredId, StringComparison.Ordinal)))
                return preferredId;

            return dashboard.Careers[0].CareerId;
        }
    }
}
