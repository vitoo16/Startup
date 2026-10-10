#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Core;
using StartupLife.Simulation;

namespace StartupLife.Application
{
    // Detached view of committed finance records. Caller must supply *validated*
    // authoritative v3 records; this adapter never forecasts or posts money.
    public sealed class BusinessFinanceRow
    {
        public string InstanceId { get; }
        public int UnitsSold { get; }
        public long GrossRevenueVnd { get; }
        public long VariableExpenseVnd { get; }
        public long FixedExpenseVnd { get; }
        public long ProfitVnd { get; }
        public long ArrearsWithheldVnd { get; }
        public long NetCashCreditVnd { get; }

        internal BusinessFinanceRow(string instanceId, int units, long gross, long variable,
            long fixedVnd, long withheld, long credited)
        {
            InstanceId = instanceId; UnitsSold = units;
            GrossRevenueVnd = gross; VariableExpenseVnd = variable;
            FixedExpenseVnd = fixedVnd;
            ProfitVnd = checked(checked(gross - variable) - fixedVnd);
            ArrearsWithheldVnd = withheld; NetCashCreditVnd = credited;
            if (checked(withheld + credited) != gross)
                throw new ArgumentException("Detached business finance row does not conserve proceeds.");
        }

        internal BusinessFinanceRow(CommittedBusinessDayOutcome source)
        {
            InstanceId = source.BusinessInstanceId;
            UnitsSold = source.FulfilledUnits;
            GrossRevenueVnd = source.GrossRevenueVnd;
            VariableExpenseVnd = source.VariablePaidVnd;
            FixedExpenseVnd = source.FixedRecognizedVnd;
            ProfitVnd = source.ProfitVnd;
            ArrearsWithheldVnd = source.AppliedToArrearsVnd;
            NetCashCreditVnd = source.NetCashCreditVnd;
        }
    }

    public sealed class BusinessFinanceDaySnapshot
    {
        public string DateIso { get; }
        public string SettlementId { get; }
        public long GrossRevenueVnd { get; }
        public long VariableExpenseVnd { get; }
        public long FixedExpenseVnd { get; }
        public long ProfitVnd { get; }
        public long ArrearsWithheldVnd { get; }
        public long NetCashCreditVnd { get; }
        public IReadOnlyList<BusinessFinanceRow> Businesses { get; }

        internal BusinessFinanceDaySnapshot(V3BusinessDaySettlement source,
            IReadOnlyList<BusinessFinanceRow> rows)
        {
            DateIso = source.OldDateIso; SettlementId = source.Id;
            GrossRevenueVnd = source.GrossVnd; VariableExpenseVnd = source.VariablePaidVnd;
            FixedExpenseVnd = source.FixedRecognizedVnd; ProfitVnd = source.ProfitVnd;
            ArrearsWithheldVnd = source.AppliedToArrearsVnd;
            NetCashCreditVnd = source.NetCashCreditVnd;
            Businesses = Array.AsReadOnly(rows.OrderBy(x => x.InstanceId,
                StringComparer.Ordinal).ToArray());
            if (Businesses.Sum(x => x.GrossRevenueVnd) != GrossRevenueVnd ||
                Businesses.Sum(x => x.VariableExpenseVnd) != VariableExpenseVnd ||
                Businesses.Sum(x => x.FixedExpenseVnd) != FixedExpenseVnd ||
                Businesses.Sum(x => x.ProfitVnd) != ProfitVnd ||
                Businesses.Sum(x => x.ArrearsWithheldVnd) != ArrearsWithheldVnd ||
                Businesses.Sum(x => x.NetCashCreditVnd) != NetCashCreditVnd)
                throw new ArgumentException("Saved committed finance rows do not reconcile to settlement.");
        }

        internal BusinessFinanceDaySnapshot(FrozenBusinessDaySettlement source)
        {
            DateIso = source.OldDateIso;
            SettlementId = source.SettlementId;
            GrossRevenueVnd = source.GrossRevenueVnd;
            VariableExpenseVnd = source.VariablePaidDuringDayVnd;
            FixedExpenseVnd = source.FixedRecognizedDuringDayVnd;
            ProfitVnd = source.RecognizedProfitVnd;
            ArrearsWithheldVnd = source.AppliedToArrearsVnd;
            NetCashCreditVnd = source.NetCreditedCashVnd;
            Businesses = Array.AsReadOnly(source.Outcomes.OrderBy(x => x.BusinessInstanceId,
                StringComparer.Ordinal).Select(x => new BusinessFinanceRow(x)).ToArray());
        }
    }

    public static class BusinessFinanceProjection
    {
        // Distinguish historical committed facts from any provisional market estimate.
        // Never publish a mutable settlement object or expose ledger posting methods.
        // Safe for presentation: reads only completed source-backed settlement records,
        // no forecasts, no unit generation and no mutation of GameSession.
        public static IReadOnlyList<BusinessFinanceDaySnapshot> FromCommittedState(GameState state)
        {
            if (state == null || state.SaveVersion != 3 || state.EconomicV3 == null ||
                state.EconomicV3.Current != state || state.EconomicV3.EconomicRecords == null)
                throw new ArgumentException("A source-verified schema3 checkpoint is required.");
            var records = state.EconomicV3.EconomicRecords;
            records.Validate();
            var graph = records.Provenance;
            var snapshots = new List<BusinessFinanceDaySnapshot>();
            foreach (var day in graph.Settlements.OrderBy(x => x.OldDateIso, StringComparer.Ordinal))
            {
                var sold = graph.Fulfillments.Where(x => x.DateIso == day.OldDateIso).ToArray();
                var due = records.ObligationTranches.Where(x => x.DueDateIso == day.OldDateIso)
                    .ToArray();
                var businessIds = sold.Select(x => x.BusinessInstanceId)
                    .Concat(due.Select(x => x.BusinessInstanceId))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal).ToArray();
                var rows = new List<BusinessFinanceRow>();
                foreach (var id in businessIds)
                {
                    var owned = sold.Where(x => x.BusinessInstanceId == id).ToArray();
                    var gross = checked(owned.Sum(x => x.UnitPriceVnd));
                    var variable = checked(owned.Sum(x => x.VariablePaidVnd));
                    var fixedVnd = checked(due.Where(x => x.BusinessInstanceId == id)
                        .Sum(x => x.AmountDueVnd));
                    var revenues = state.Ledger.Where(x => x.Category == "business.revenue" &&
                        x.AttributionId == day.Id + "/" + id).ToArray();
                    var net = checked(revenues.Sum(x => x.CashDelta));
                    var withheld = checked(gross - net);
                    if (revenues.Length != (gross == 0 ? 0 : 1) ||
                        revenues.Any(x => x.Amount != gross))
                        throw new ArgumentException("Committed finance display needs attributed ledger revenue.");
                    rows.Add(new BusinessFinanceRow(id,owned.Length,gross,variable,fixedVnd,withheld,net));
                }
                snapshots.Add(new BusinessFinanceDaySnapshot(day,rows));
            }
            return Array.AsReadOnly(snapshots.ToArray());
        }

        public static BusinessFinanceDaySnapshot FromVerifiedSettlement(FrozenBusinessDaySettlement settlement)
        {
            if (settlement == null) throw new ArgumentNullException(nameof(settlement));
            return new BusinessFinanceDaySnapshot(settlement);
        }
    }
}
