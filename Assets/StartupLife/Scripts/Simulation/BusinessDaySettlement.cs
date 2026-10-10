#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    public sealed class BusinessUnitFinancialLine
    {
        public MarketFulfillmentUnit Fulfillment { get; }
        public int BusinessUnitOrdinal { get; }
        public string SourceCostReservationId { get; }
        public long UnitPriceVnd { get; }
        public long VariablePaidVnd { get; }
        public string EconomicProfileRevision { get; }
        public PricingPosture EffectivePosture { get; }

        public BusinessUnitFinancialLine(MarketFulfillmentUnit fulfillment,
            BusinessUnitCostReservation consumedReservation, long realizedUnitPriceVnd,
            string profileRevision, PricingPosture effectivePosture)
        {
            if (fulfillment == null || consumedReservation == null ||
                fulfillment.BusinessInstanceId != consumedReservation.InstanceId ||
                realizedUnitPriceVnd <= 0 || realizedUnitPriceVnd > 1000000000L ||
                string.IsNullOrWhiteSpace(profileRevision) ||
                !Enum.IsDefined(typeof(PricingPosture), effectivePosture))
                throw new ArgumentException("Unattributed sale or invalid v3 sale price.");
            Fulfillment = fulfillment;
            BusinessUnitOrdinal = consumedReservation.AbsoluteBusinessUnitOrdinal;
            SourceCostReservationId = consumedReservation.Id;
            UnitPriceVnd = realizedUnitPriceVnd;
            VariablePaidVnd = consumedReservation.AmountVnd;
            EconomicProfileRevision = profileRevision;
            EffectivePosture = effectivePosture;
        }
    }

    public sealed class CommittedBusinessDayOutcome
    {
        public string BusinessInstanceId { get; }
        public int FulfilledUnits { get; }
        public long GrossRevenueVnd { get; }
        public long FixedRecognizedVnd { get; }
        public long VariablePaidVnd { get; }
        public long ProfitVnd { get; }
        public long AppliedToArrearsVnd { get; }
        public long NetCashCreditVnd { get; }
        public IReadOnlyList<string> FulfillmentIds { get; }

        internal CommittedBusinessDayOutcome(string instanceId, IReadOnlyList<BusinessUnitFinancialLine> lines,
            long fixedRecognized, IncomeArrearSettlementPlan income)
        {
            if (fixedRecognized < 0) throw new ArgumentException("Negative fixed recognition.");
            BusinessInstanceId = instanceId; FulfilledUnits = lines.Count;
            GrossRevenueVnd = checked(lines.Sum(x => x.UnitPriceVnd));
            VariablePaidVnd = checked(lines.Sum(x => x.VariablePaidVnd));
            FixedRecognizedVnd = fixedRecognized;
            ProfitVnd = checked(checked(GrossRevenueVnd - VariablePaidVnd) - FixedRecognizedVnd);
            AppliedToArrearsVnd = income.WithheldToOldestArrearsVnd;
            NetCashCreditVnd = income.NetCreditedToCashVnd;
            FulfillmentIds = Array.AsReadOnly(lines.Select(x => x.Fulfillment.Id).ToArray());
            if (GrossRevenueVnd != checked(AppliedToArrearsVnd + NetCashCreditVnd))
                throw new ArgumentException("Business sales and income-to-arrears conservation failed.");
        }
    }

    public sealed class FrozenBusinessDaySettlement
    {
        public string SettlementId { get; }
        public string OldDateIso { get; }
        public string SourceBoundaryOperationId { get; }
        public IReadOnlyList<CommittedBusinessDayOutcome> Outcomes { get; }
        public IReadOnlyList<ObligationDueTranche> UpdatedArrears { get; }
        public IReadOnlyList<IncomeArrearSettlementPlan> IncomeApplications { get; }
        public long GrossRevenueVnd { get; }
        public long VariablePaidDuringDayVnd { get; }
        public long FixedRecognizedDuringDayVnd { get; }
        public long RecognizedProfitVnd { get; }
        public long AppliedToArrearsVnd { get; }
        public long NetCreditedCashVnd { get; }

        internal FrozenBusinessDaySettlement(string id, string oldDate, string boundary,
            IEnumerable<CommittedBusinessDayOutcome> outcomes, IEnumerable<ObligationDueTranche> remainingDue,
            IEnumerable<IncomeArrearSettlementPlan> applications)
        {
            SettlementId = id; OldDateIso = oldDate; SourceBoundaryOperationId = boundary;
            Outcomes = Array.AsReadOnly(outcomes.ToArray());
            UpdatedArrears = Array.AsReadOnly(remainingDue.ToArray());
            IncomeApplications = Array.AsReadOnly(applications.ToArray());
            GrossRevenueVnd = checked(Outcomes.Sum(x => x.GrossRevenueVnd));
            VariablePaidDuringDayVnd = checked(Outcomes.Sum(x => x.VariablePaidVnd));
            FixedRecognizedDuringDayVnd = checked(Outcomes.Sum(x => x.FixedRecognizedVnd));
            RecognizedProfitVnd = checked(Outcomes.Sum(x => x.ProfitVnd));
            AppliedToArrearsVnd = checked(Outcomes.Sum(x => x.AppliedToArrearsVnd));
            NetCreditedCashVnd = checked(Outcomes.Sum(x => x.NetCashCreditVnd));
            if (GrossRevenueVnd != checked(AppliedToArrearsVnd + NetCreditedCashVnd) ||
                RecognizedProfitVnd != checked(
                    checked(GrossRevenueVnd - VariablePaidDuringDayVnd) - FixedRecognizedDuringDayVnd))
                throw new ArgumentException("Day settlement failed monetary conservation.");
        }
    }

    // Pure midnight calculator. It produces exactly one unique, attributable old-date result
    // but NEVER posts player Cash/Ledger/Receipt itself. GameSession owns atomic commit.
    public static class BusinessDaySettlementEngine
    {
        public static FrozenBusinessDaySettlement Calculate(
            string runId, string oldDateIso, string boundaryOperationId,
            IEnumerable<MarketPoolDaySupply> originalDayPools,
            IEnumerable<BusinessUnitFinancialLine> realizedLines,
            IEnumerable<BusinessUnitCostReservation> actuallyConsumedReservations,
            IEnumerable<string> committedSliceIds,
            IEnumerable<KeyValuePair<string, long>> fixedRecognizedByBusiness,
            IEnumerable<ObligationDueTranche> outstandingDue,
            IEnumerable<string> alreadySettledIds)
        {
            if (string.IsNullOrWhiteSpace(runId) || string.IsNullOrWhiteSpace(boundaryOperationId))
                throw new ArgumentException("Midnight requires one committed source operation.");
            _ = BusinessObligationDefinition.ParseDate(oldDateIso);
            var settlementId = runId + "/business-day/" + oldDateIso;
            if (originalDayPools == null || realizedLines == null ||
                actuallyConsumedReservations == null || committedSliceIds == null ||
                fixedRecognizedByBusiness == null || outstandingDue == null || alreadySettledIds == null)
                throw new ArgumentNullException(nameof(realizedLines));
            var priorSettlements = alreadySettledIds.ToArray();
            if (priorSettlements.Any(x => x == settlementId) ||
                priorSettlements.Distinct(StringComparer.Ordinal).Count() != priorSettlements.Length)
                throw new ArgumentException("Old date has already been settled or settlement index is corrupt.");
            var pools = originalDayPools.ToArray();
            if (pools.Any(x => x == null || x.DateIso != oldDateIso) ||
                pools.Select(x => x.PoolId).Distinct(StringComparer.Ordinal).Count() != pools.Length)
                throw new ArgumentException("Missing or duplicated immutable old-day pools.");
            var poolMap = pools.ToDictionary(x => x.PoolId, x => x, StringComparer.Ordinal);
            var sales = realizedLines.ToArray();
            if (sales.Any(x => x == null || x.Fulfillment.DateIso != oldDateIso) ||
                sales.Select(x => x.Fulfillment.Id).Distinct(StringComparer.Ordinal).Count() != sales.Length ||
                sales.Select(x => x.SourceCostReservationId).Distinct(StringComparer.Ordinal).Count() != sales.Length ||
                sales.Select(x => x.Fulfillment.BusinessInstanceId + "/" + x.BusinessUnitOrdinal)
                    .Distinct(StringComparer.Ordinal).Count() != sales.Length)
                throw new ArgumentException("Duplicate or wrong-date realized business line.");
            var validSlices = new HashSet<string>(committedSliceIds, StringComparer.Ordinal);
            if (validSlices.Count != committedSliceIds.Count() ||
                validSlices.Any(string.IsNullOrWhiteSpace) ||
                sales.Any(x => !validSlices.Contains(x.Fulfillment.SliceId)))
                throw new ArgumentException("Business line lacks a committed operating slice.");
            var consumed = actuallyConsumedReservations.ToArray();
            if (consumed.Any(x => x == null) ||
                consumed.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != consumed.Length ||
                consumed.Length != sales.Length)
                throw new ArgumentException("Each cost reservation must be consumed once by exactly one sale.");
            var consumedMap = consumed.ToDictionary(x => x.Id, x => x, StringComparer.Ordinal);
            foreach (var sale in sales)
            {
                if (!consumedMap.TryGetValue(sale.SourceCostReservationId, out var cost) ||
                    cost.InstanceId != sale.Fulfillment.BusinessInstanceId ||
                    cost.AbsoluteBusinessUnitOrdinal != sale.BusinessUnitOrdinal ||
                    cost.AmountVnd != sale.VariablePaidVnd)
                    throw new ArgumentException("Fulfilled unit lacks exact cost reservation provenance.");
            }
            foreach (var byPool in sales.GroupBy(x => x.Fulfillment.PoolId, StringComparer.Ordinal))
            {
                if (!poolMap.TryGetValue(byPool.Key, out var pool))
                    throw new ArgumentException("Fulfillment references missing old-day pool.");
                _ = CustomerDemandCalculator.RemainingSupply(pool, byPool.Select(x => x.Fulfillment));
            }
            var fixedRows = new SortedDictionary<string, long>(StringComparer.Ordinal);
            foreach (var row in fixedRecognizedByBusiness)
            {
                if (string.IsNullOrWhiteSpace(row.Key) || row.Value < 0 || fixedRows.ContainsKey(row.Key))
                    throw new ArgumentException("Invalid or duplicated accrued fixed expense.");
                fixedRows.Add(row.Key, row.Value);
            }
            var debts = outstandingDue.ToArray();
            var groupIds = sales.Select(x => x.Fulfillment.BusinessInstanceId).Concat(fixedRows.Keys)
                .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var results = new List<CommittedBusinessDayOutcome>();
            var settlements = new List<IncomeArrearSettlementPlan>();
            foreach (var instance in groupIds)
            {
                var owned = sales.Where(x => x.Fulfillment.BusinessInstanceId == instance)
                    .OrderBy(x => x.BusinessUnitOrdinal)
                    .ThenBy(x => x.Fulfillment.Id, StringComparer.Ordinal).ToArray();
                var gross = checked(owned.Sum(x => x.UnitPriceVnd));
                // The old-date settlement is still valid for businesses closed earlier that day.
                var income = ObligationIncomeDistributor.Plan(boundaryOperationId + "/business/" + instance, gross, debts);
                debts = income.ApplyTo(debts).ToArray();
                settlements.Add(income);
                results.Add(new CommittedBusinessDayOutcome(instance, owned,
                    fixedRows.TryGetValue(instance, out var fixedVnd) ? fixedVnd : 0, income));
            }
            return new FrozenBusinessDaySettlement(settlementId, oldDateIso,
                boundaryOperationId, results, debts, settlements);
        }
    }
}
