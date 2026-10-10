#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // This mutates one DETACHED old-date candidate and is intended to execute
    // before the existing employment salary/living start-of-new-day settlement.
    // No settlement command is exposed to players.
    public static class V3MidnightTransactions
    {
        public static V3BusinessDaySettlement SettleOldDate(
            GameState candidate, EconomicV3Records records,
            string oldDateIso, string boundaryOperationId)
        {
            if (candidate == null || records == null || candidate.SaveVersion != 3 ||
                string.IsNullOrWhiteSpace(boundaryOperationId) || candidate.DateIso != oldDateIso)
                throw new ArgumentException("A midnight settlement requires a valid old-date candidate.");
            _ = DateTime.ParseExact(oldDateIso, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var provenance = records.Provenance;
            var settlementId = candidate.RunId + "/business-day/" + oldDateIso;
            if (records.SettledDayIds.Contains(settlementId) ||
                provenance.Settlements.Any(x => x.Id == settlementId ||
                    x.OldDateIso == oldDateIso))
                throw new ArgumentException("The old simulation date was already settled.");
            if (provenance.ActiveEpoch != null &&
                (provenance.ActiveEpoch.DateIso != oldDateIso ||
                 provenance.ActiveEpoch.ProcessedUntilMinute != candidate.Minute))
                throw new ArgumentException("Old-day financial epoch does not reach the committed cursor.");
            var sold = provenance.Fulfillments
                .Where(x => x.DateIso == oldDateIso)
                .OrderBy(x => x.BusinessInstanceId, StringComparer.Ordinal)
                .ThenBy(x => x.BusinessUnitOrdinal)
                .ToArray();
            var gross = checked(sold.Sum(x => x.UnitPriceVnd));
            var variable = checked(sold.Sum(x => x.VariablePaidVnd));
            // Fixed cost is recognized exactly once from the source-linked
            // contractual invoice graph, independently of how much was paid.
            var recognized = checked(records.ObligationTranches
                .Where(x => x.DueDateIso == oldDateIso)
                .Sum(x => x.AmountDueVnd));
            V3EpochTransactions.ReleaseFuture(provenance);
            long appliedArrears = 0;
            long credited = 0;
            foreach (var businessSales in sold.GroupBy(x => x.BusinessInstanceId, StringComparer.Ordinal)
                .OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                var businessGross = checked(businessSales.Sum(x => x.UnitPriceVnd));
                var income = V3ObligationTransactions.CreditIncome(candidate, records,
                    boundaryOperationId, businessGross, "business.revenue",
                    settlementId + "/" + businessSales.Key);
                appliedArrears = checked(appliedArrears + income.WithheldToOldestArrearsVnd);
                credited = checked(credited + income.NetCreditedToCashVnd);
            }
            if (checked(appliedArrears + credited) != gross)
                throw new ArgumentException("Gross old-day proceeds do not conserve net and due payments.");
            var settlement = new V3BusinessDaySettlement
            {
                Id = settlementId, OldDateIso = oldDateIso,
                SourceBoundaryOperationId = boundaryOperationId,
                FulfillmentIds = sold.Select(x => x.Id).ToList(),
                GrossVnd = gross, VariablePaidVnd = variable,
                FixedRecognizedVnd = recognized,
                ProfitVnd = checked(checked(gross - variable) - recognized),
                AppliedToArrearsVnd = appliedArrears,
                NetCashCreditVnd = credited
            };
            provenance.Settlements.Add(settlement);
            records.SettledDayIds.Add(settlementId);
            records.V3RecognizedFixedVnd = checked(records.V3RecognizedFixedVnd + recognized);
            records.Validate();
            return settlement;
        }
    }
}
