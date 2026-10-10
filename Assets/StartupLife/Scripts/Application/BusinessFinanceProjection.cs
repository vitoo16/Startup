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
        public static BusinessFinanceDaySnapshot FromVerifiedSettlement(FrozenBusinessDaySettlement settlement)
        {
            if (settlement == null) throw new ArgumentNullException(nameof(settlement));
            return new BusinessFinanceDaySnapshot(settlement);
        }
    }
}
