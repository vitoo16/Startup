#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // The only persistent payable is V3ObligationTranche. The remaining due is derived
    // from AmountDueVnd-PaidVnd, never a separately mutable business ArrearState.
    public static class V3ObligationTransactions
    {
        public static IReadOnlyList<V3ObligationTranche> OpenDue(
            GameState candidate, EconomicV3Records records,
            IEnumerable<BusinessObligationDefinition> authoredDefinitions,
            IReadOnlyDictionary<string, bool> eligibleServiceByInstance,
            string sourceOperationId,
            IReadOnlyDictionary<string, string>? committedPerOperationEvents = null,
            IReadOnlyDictionary<string, string>? committedCloseEvents = null)
        {
            Require(candidate, records, sourceOperationId);
            if (authoredDefinitions == null || eligibleServiceByInstance == null)
                throw new ArgumentNullException(nameof(authoredDefinitions));
            var newDue = new List<V3ObligationTranche>();
            foreach (var definition in authoredDefinitions.OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                if (definition == null) throw new ArgumentException("Null authored contractual obligation.");
                var businesses = candidate.Businesses
                    .Where(x => x.DefinitionId == definition.BusinessDefinitionId)
                    .OrderBy(x => x.InstanceId, StringComparer.Ordinal);
                foreach (var business in businesses)
                {
                    var eligible = eligibleServiceByInstance.TryGetValue(business.InstanceId, out var canOperate) && canOperate;
                    var eventId = committedPerOperationEvents != null &&
                        committedPerOperationEvents.TryGetValue(business.InstanceId, out var used) ? used : "";
                    var closeId = committedCloseEvents != null &&
                        committedCloseEvents.TryGetValue(business.InstanceId, out var closed) ? closed : "";
                    var due = BusinessObligationScheduler.DueOnDate(definition,
                        business.InstanceId, candidate.DateIso, eligible, eventId, closeId);
                    if (due == null) continue;
                    if (definition.Proration == ObligationProrationPolicy.DayMetered &&
                        definition.Cadence != ObligationCadence.Daily &&
                        definition.Cadence != ObligationCadence.PerOperatingDay)
                        throw new NotSupportedException("Recurring multi-day day-metered invoice requires period accumulator.");
                    var id = candidate.RunId + "/obligation/" + due.IdentityKey;
                    if (records.ObligationTranches.Any(x => x.Id == id))
                        throw new ArgumentException("Contractual due was opened twice in the same period.");
                    // This due is determined by immutable versioned obligation content,
                    // NOT arbitrary expense values from a UI or the caller's wallet.
                    var invoice = new V3ObligationTranche
                    {
                        Id = id, BusinessInstanceId = business.InstanceId,
                        ObligationDefinitionId = definition.Id,
                        DefinitionRevision = definition.Revision,
                        PeriodKey = due.PeriodKey, EventKey = due.DueEventKey,
                        DueDateIso = due.DueDateIso, DueMinute = candidate.Minute,
                        CreationSequence = candidate.Revision,
                        AmountDueVnd = due.AmountDueVnd,
                        PaidVnd = 0, SourceOperationId = sourceOperationId
                    };
                    invoice.Validate();
                    records.ObligationTranches.Add(invoice);
                    records.CommittedObligationIds.Add(invoice.Id);
                    candidate.Ledger.Add(new LedgerEntry
                    {
                        Id = candidate.NewEntity("transaction"),
                        OperationId = sourceOperationId, Category = "business.obligation.accrual",
                        CashDelta = 0, Amount = invoice.AmountDueVnd,
                        AttributionId = invoice.Id
                    });
                    newDue.Add(invoice);
                }
            }
            records.Validate();
            return newDue.AsReadOnly();
        }

        // Called on due events BEFORE reserving any new variable costs, or at a genuine
        // state-changing epoch replan. Never spends cash already committed to other units.
        public static long PayDue(GameState candidate, EconomicV3Records records,
            string sourceOperationId)
        {
            Require(candidate, records, sourceOperationId);
            var variableReserved = checked(records.Provenance.CostReservations
                .Where(x => x.Status == V3CostReservationStatus.Active)
                .Sum(x => x.AmountVnd));
            if (variableReserved > candidate.Cash)
                throw new ArgumentException("Existing cost reservations are not fully funded.");
            var spendable = checked(candidate.Cash - variableReserved);
            var dues = records.ObligationTranches.Select(x =>
                new ObligationDueTranche(x.Id,
                    x.BusinessInstanceId + "/" + x.ObligationDefinitionId,
                    x.DueDateIso, x.DueMinute, x.CreationSequence,
                    x.AmountDueVnd, x.PaidVnd)).ToArray();
            var allocations = BusinessObligationScheduler.PlanDuePayments(dues, spendable);
            long total = 0;
            foreach (var allocation in allocations)
            {
                var due = records.ObligationTranches.Single(x => x.Id == allocation.TrancheId);
                due.PaidVnd = checked(due.PaidVnd + allocation.PaidVnd);
                candidate.Cash = checked(candidate.Cash - allocation.PaidVnd);
                total = checked(total + allocation.PaidVnd);
                candidate.Ledger.Add(new LedgerEntry
                {
                    Id = candidate.NewEntity("transaction"),
                    OperationId = sourceOperationId,
                    Category = "business.obligation.payment",
                    CashDelta = -allocation.PaidVnd,
                    Amount = allocation.PaidVnd,
                    AttributionId = due.Id
                });
            }
            records.Validate();
            return total;
        }

        // This is the shared oldest-due income application for both personal legacy
        // arrears and source-linked v3 business invoices. Salary must call this in
        // its v3 evaluator instead of also running the legacy salary Income method.
        public static IncomeArrearSettlementPlan CreditIncome(
            GameState candidate, EconomicV3Records records,
            string sourceOperationId, long grossVnd,
            string incomeCategory, string attributionId)
        {
            Require(candidate, records, sourceOperationId);
            if (grossVnd < 0 || string.IsNullOrWhiteSpace(attributionId) ||
                (incomeCategory != "business.revenue" && incomeCategory != "salary.payment"))
                throw new ArgumentException("Unsupported authoritative income attribution.");
            var obligations = records.ObligationTranches.Select(x =>
                new ObligationDueTranche(x.Id,
                    x.BusinessInstanceId + "/" + x.ObligationDefinitionId,
                    x.DueDateIso, x.DueMinute, x.CreationSequence, x.AmountDueVnd, x.PaidVnd));
            var personal = candidate.Arrears.Select((x, index) =>
                new ObligationDueTranche(x.Id, "personal/" + x.Id,
                    x.DueIso, 0, index, x.Amount, 0));
            var debt = obligations.Concat(personal).ToArray();
            var plan = ObligationIncomeDistributor.Plan(sourceOperationId, grossVnd, debt);
            foreach (var allocation in plan.ArrearAllocations)
            {
                var invoice = records.ObligationTranches.SingleOrDefault(x => x.Id == allocation.TrancheId);
                if (invoice != null)
                {
                    if (invoice.PaidVnd != allocation.PreviouslyPaidVnd)
                        throw new ArgumentException("Historical business debt changed before income admission.");
                    invoice.PaidVnd = allocation.NewPaidVnd;
                }
                else
                {
                    var old = candidate.Arrears.SingleOrDefault(x => x.Id == allocation.TrancheId);
                    if (old == null || old.Amount < allocation.PaidFromIncomeVnd)
                        throw new ArgumentException("Historical personal arrears mismatch.");
                    old.Amount = checked(old.Amount - allocation.PaidFromIncomeVnd);
                }
                candidate.Ledger.Add(new LedgerEntry
                {
                    Id = candidate.NewEntity("transaction"),
                    OperationId = sourceOperationId, Category = "arrear.settlement",
                    CashDelta = 0, Amount = allocation.PaidFromIncomeVnd,
                    AttributionId = allocation.TrancheId
                });
            }
            candidate.Arrears.RemoveAll(x => x.Amount == 0);
            candidate.Cash = checked(candidate.Cash + plan.NetCreditedToCashVnd);
            candidate.Ledger.Add(new LedgerEntry
            {
                Id = candidate.NewEntity("transaction"),
                OperationId = sourceOperationId, Category = incomeCategory,
                CashDelta = plan.NetCreditedToCashVnd,
                Amount = grossVnd, AttributionId = attributionId
            });
            if (incomeCategory == "business.revenue")
            {
                records.V3RecognizedRevenueVnd = checked(records.V3RecognizedRevenueVnd + grossVnd);
                records.V3AppliedToArrearsVnd = checked(records.V3AppliedToArrearsVnd +
                    plan.WithheldToOldestArrearsVnd);
                records.V3NetBusinessCreditVnd = checked(records.V3NetBusinessCreditVnd +
                    plan.NetCreditedToCashVnd);
            }
            records.Validate();
            return plan;
        }

        private static void Require(GameState candidate, EconomicV3Records records, string operationId)
        {
            if (candidate == null || records == null || candidate.SaveVersion != 3 ||
                string.IsNullOrWhiteSpace(operationId))
                throw new ArgumentException("Contractual invoice requires a real schema-3 candidate operation.");
        }
    }
}
