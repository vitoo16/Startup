#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // Immutable, attributable application of gross salary/business proceeds to one
    // authoritative debt graph. These are NOT additional ledger cash debits.
    public sealed class IncomeToArrearAllocation
    {
        public string SourceOperationId { get; }
        public string TrancheId { get; }
        public string ObligationIdentity { get; }
        public long PreviouslyPaidVnd { get; }
        public long PaidFromIncomeVnd { get; }
        public long NewPaidVnd { get; }
        public long RemainingDueVnd { get; }

        internal IncomeToArrearAllocation(string operationId, ObligationDueTranche tranche, long withheld)
        {
            SourceOperationId = operationId; TrancheId = tranche.Id;
            ObligationIdentity = tranche.ObligationIdentity;
            PreviouslyPaidVnd = tranche.PaidVnd; PaidFromIncomeVnd = withheld;
            NewPaidVnd = checked(tranche.PaidVnd + withheld);
            RemainingDueVnd = checked(tranche.DueVnd - NewPaidVnd);
            if (withheld <= 0 || RemainingDueVnd < 0)
                throw new ArgumentException("Invalid single-source arrears allocation.");
        }
    }

    public sealed class IncomeArrearSettlementPlan
    {
        public string SourceOperationId { get; }
        public long GrossIncomeVnd { get; }
        public long WithheldToOldestArrearsVnd { get; }
        public long NetCreditedToCashVnd { get; }
        public IReadOnlyList<IncomeToArrearAllocation> ArrearAllocations { get; }

        internal IncomeArrearSettlementPlan(string source, long gross, IReadOnlyList<IncomeToArrearAllocation> payments)
        {
            SourceOperationId = source; GrossIncomeVnd = gross;
            ArrearAllocations = Array.AsReadOnly(payments.ToArray());
            WithheldToOldestArrearsVnd = checked(payments.Sum(x => x.PaidFromIncomeVnd));
            NetCreditedToCashVnd = checked(gross - WithheldToOldestArrearsVnd);
            if (NetCreditedToCashVnd < 0) throw new ArgumentException("Arrear withholding exceeds gross income.");
        }

        // Pure state transition for one atomic receipt. It rejects re-applying
        // the SAME plan to a debt graph whose PaidVnd already changed.
        // Source receipt deduplication in GameSession is still mandatory.
        public IReadOnlyList<ObligationDueTranche> ApplyTo(IEnumerable<ObligationDueTranche> original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            var entries = original.ToArray();
            if (entries.Any(x => x == null) || entries.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != entries.Length)
                throw new ArgumentException("Corrupt or repeated arrears tranche.");
            var mapping = ArrearAllocations.ToDictionary(x => x.TrancheId, x => x, StringComparer.Ordinal);
            var updated = new List<ObligationDueTranche>(entries.Length);
            foreach (var entry in entries)
            {
                if (!mapping.TryGetValue(entry.Id, out var payment))
                {
                    updated.Add(entry);
                    continue;
                }
                if (entry.ObligationIdentity != payment.ObligationIdentity ||
                    entry.PaidVnd != payment.PreviouslyPaidVnd ||
                    entry.DueVnd - entry.PaidVnd < payment.PaidFromIncomeVnd)
                    throw new ArgumentException("Stale, mismatched or previously applied revenue-to-arrears link.");
                updated.Add(new ObligationDueTranche(entry.Id, entry.ObligationIdentity,
                    entry.DueDateIso, entry.DueMinute, entry.CreationSequence,
                    entry.DueVnd, payment.NewPaidVnd));
                mapping.Remove(entry.Id);
            }
            if (mapping.Count > 0) throw new ArgumentException("Revenue settlement references missing tranche.");
            return updated.AsReadOnly();
        }
    }

    public static class ObligationIncomeDistributor
    {
        public static IncomeArrearSettlementPlan Plan(string sourceOperationId, long grossIncomeVnd,
            IEnumerable<ObligationDueTranche> outstandingDueTranches)
        {
            if (string.IsNullOrWhiteSpace(sourceOperationId) || grossIncomeVnd < 0)
                throw new ArgumentException("Invalid incoming salary/business revenue receipt.");
            if (outstandingDueTranches == null) throw new ArgumentNullException(nameof(outstandingDueTranches));
            var tranches = outstandingDueTranches.ToArray();
            if (tranches.Any(x => x == null) ||
                tranches.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != tranches.Length)
                throw new ArgumentException("Duplicate or malformed arrears provenance.");
            var remaining = grossIncomeVnd;
            var payments = new List<IncomeToArrearAllocation>();
            foreach (var tranche in tranches.Where(x => x.OutstandingVnd > 0)
                .OrderBy(x => x.DueDateIso, StringComparer.Ordinal)
                .ThenBy(x => x.DueMinute)
                .ThenBy(x => x.CreationSequence)
                .ThenBy(x => x.ObligationIdentity, StringComparer.Ordinal)
                .ThenBy(x => x.Id, StringComparer.Ordinal))
            {
                if (remaining == 0) break;
                var applied = Math.Min(remaining, tranche.OutstandingVnd);
                remaining = checked(remaining - applied);
                payments.Add(new IncomeToArrearAllocation(sourceOperationId, tranche, applied));
            }
            return new IncomeArrearSettlementPlan(sourceOperationId, grossIncomeVnd, payments);
        }
    }
}
