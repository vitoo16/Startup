#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Core;
using StartupLife.Simulation;

namespace StartupLife.Application
{
    // Detached read model: employment history is already authoritative in GameState.
    // No extra biography DTO is persisted and no new save migration is needed.
    public sealed class CareerTenureSnapshot
    {
        public string EmploymentId { get; }
        public string EmployerId { get; }
        public string CareerId { get; }
        public string CareerRevision { get; }
        public string? CareerNameKey { get; }
        public string StartedIso { get; }
        public string FirstShiftIso { get; }
        public string? EndedIso { get; }
        public long CareerXp { get; }
        public int RankIndex { get; }
        public string? RankId { get; }
        public long? MonthlySalaryAtRecordedRankVnd { get; }
        public int UnpaidClaimCount { get; }
        public RationalAmount OutstandingSalary { get; }

        internal CareerTenureSnapshot(EmploymentState employment, ContentCatalog content,
            IReadOnlyDictionary<string, SalaryClaim[]> claims)
        {
            EmploymentId = employment.InstanceId;
            EmployerId = employment.EmployerId;
            CareerId = employment.CareerId;
            CareerRevision = employment.DefinitionRevision;
            StartedIso = employment.StartedIso;
            FirstShiftIso = employment.FirstShiftIso;
            EndedIso = employment.EndedIso.Length == 0 ? null : employment.EndedIso;
            CareerXp = employment.Xp;
            RankIndex = employment.Rank;

            // Never project a new revision's salary onto a historical job.
            if (content.Careers.TryGetValue(CareerId, out var definition) &&
                definition.Revision == CareerRevision &&
                RankIndex >= 0 && RankIndex < definition.Ranks.Count)
            {
                CareerNameKey = definition.NameKey;
                RankId = definition.Ranks[RankIndex].Id;
                MonthlySalaryAtRecordedRankVnd = definition.Ranks[RankIndex].MonthlySalary;
            }

            if (claims.TryGetValue(EmploymentId, out var outstanding))
            {
                UnpaidClaimCount = outstanding.Length;
                var exact = RationalAmount.Zero;
                foreach (var claim in outstanding)
                    exact += SimulationEngine.ClaimAmount(claim);
                OutstandingSalary = exact;
            }
            else OutstandingSalary = RationalAmount.Zero;
        }
    }

    public sealed class CareerBiographySnapshot
    {
        public bool HasCharacter { get; }
        public bool IsEmployed => CurrentEmployment != null;
        public bool IsFounderAfterResignation { get; }
        public CareerTenureSnapshot? CurrentEmployment { get; }
        public IReadOnlyList<CareerTenureSnapshot> PreviousEmployments { get; }
        public RationalAmount OutstandingSalary { get; }

        internal CareerBiographySnapshot(bool hasCharacter, CareerTenureSnapshot? current,
            CareerTenureSnapshot[] past, RationalAmount outstanding)
        {
            HasCharacter = hasCharacter;
            CurrentEmployment = current;
            IsFounderAfterResignation = hasCharacter && current == null && past.Length > 0;
            PreviousEmployments = Array.AsReadOnly(past);
            OutstandingSalary = outstanding;
        }
    }

    public static class CareerBiographyProjection
    {
        public static CareerBiographySnapshot FromCommittedState(GameState state, ContentCatalog content)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (content == null) throw new ArgumentNullException(nameof(content));

            // Claims retain their original employment identity after resignation.
            // A payday can leave a fractional-VND remainder, so never round here.
            var claims = state.Claims.GroupBy(x => x.EmploymentId, StringComparer.Ordinal)
                .ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.Ordinal);
            var current = state.Employment == null ? null :
                new CareerTenureSnapshot(state.Employment, content, claims);
            var past = state.PreviousEmployment.Select(x =>
                new CareerTenureSnapshot(x, content, claims)).ToArray();
            var total = RationalAmount.Zero;
            foreach (var claim in state.Claims)
                total += SimulationEngine.ClaimAmount(claim);
            return new CareerBiographySnapshot(!string.IsNullOrWhiteSpace(state.Name),
                current, past, total);
        }
    }
}
