#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Numerics;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // Pure v3 scheduling primitives, with explicit event inputs and no GameState mutation.
    // Calendar uses the simulation clock; no DateTime.Now, wall-clock or time-zone data.
    public static class BusinessObligationScheduler
    {
        public static BusinessObligationDue? DueOnDate(BusinessObligationDefinition definition,
            string businessInstanceId, string dateIso,
            bool eligibleServiceEntitlement, string committedOperationId = "", string closeOperationId = "")
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (string.IsNullOrWhiteSpace(businessInstanceId)) throw new ArgumentException("Missing business ID.");
            var date = BusinessObligationDefinition.ParseDate(dateIso);
            var start = BusinessObligationDefinition.ParseDate(definition.ContractStartIso);
            if (date < start || (definition.ContractEndIso != null &&
                date > BusinessObligationDefinition.ParseDate(definition.ContractEndIso)))
                return null;
            string eventKey;
            string periodKey;
            switch (definition.Cadence)
            {
                case ObligationCadence.OneTime:
                    if (date != start) return null;
                    eventKey = "one-time"; periodKey = definition.ContractStartIso; break;
                case ObligationCadence.PerOperation:
                    if (!eligibleServiceEntitlement || string.IsNullOrWhiteSpace(committedOperationId)) return null;
                    eventKey = "operation:" + committedOperationId;
                    periodKey = dateIso; break;
                case ObligationCadence.PerOperatingDay:
                    if (!eligibleServiceEntitlement || string.IsNullOrWhiteSpace(committedOperationId)) return null;
                    eventKey = "operating-day"; periodKey = dateIso; break;
                case ObligationCadence.Daily:
                    if (!eligibleServiceEntitlement) return null;
                    eventKey = "daily-service"; periodKey = dateIso; break;
                case ObligationCadence.Weekly:
                    var days = (date - start).Days;
                    if (days % 7 != 0) return null;
                    periodKey = dateIso; eventKey = "weekly"; break;
                case ObligationCadence.Monthly:
                    if (date.Day != Math.Min(start.Day, DateTime.DaysInMonth(date.Year, date.Month)))
                        return null;
                    periodKey = dateIso; eventKey = "monthly"; break;
                case ObligationCadence.Annual:
                    if (date.Month != start.Month ||
                        date.Day != Math.Min(start.Day, DateTime.DaysInMonth(date.Year, date.Month)))
                        return null;
                    periodKey = dateIso; eventKey = "annual"; break;
                case ObligationCadence.OnClose:
                    if (string.IsNullOrWhiteSpace(closeOperationId)) return null;
                    eventKey = "close:" + closeOperationId; periodKey = dateIso; break;
                default: throw new ArgumentException("Unsupported obligation cadence.");
            }
            return new BusinessObligationDue(businessInstanceId, definition, periodKey, eventKey, dateIso);
        }

        public static long AccruedThroughEligibleDays(long fullChargeVnd, int eligibleDaysThrough,
            int totalEligibleServiceDays)
        {
            if (fullChargeVnd < 0 || totalEligibleServiceDays <= 0 ||
                eligibleDaysThrough < 0 || eligibleDaysThrough > totalEligibleServiceDays)
                throw new ArgumentException("Invalid cumulative day-metered proration.");
            var amount = new BigInteger(fullChargeVnd) * eligibleDaysThrough / totalEligibleServiceDays;
            return checked((long)amount);
        }

        public static long AccrualDelta(long fullChargeVnd, int previousEligibleDays,
            int newEligibleDays, int totalEligibleServiceDays)
        {
            if (newEligibleDays < previousEligibleDays)
                throw new ArgumentException("Proration cannot run backward.");
            return checked(AccruedThroughEligibleDays(fullChargeVnd, newEligibleDays, totalEligibleServiceDays)
                - AccruedThroughEligibleDays(fullChargeVnd, previousEligibleDays, totalEligibleServiceDays));
        }

        // Plan payments oldest-due-first, without modifying a live GameState or charging twice.
        // The caller applies this plan and creates any arrear links inside one GameSession transaction.
        public static IReadOnlyList<ObligationPaymentAllocation> PlanDuePayments(
            IEnumerable<ObligationDueTranche> due, long availableCashVnd)
        {
            if (due == null) throw new ArgumentNullException(nameof(due));
            if (availableCashVnd < 0) throw new ArgumentException("Cash cannot be negative.");
            var entries = due.ToArray();
            if (entries.Any(x => x == null) ||
                entries.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != entries.Length)
                throw new ArgumentException("Invalid or duplicated obligation tranche.");
            var remaining = availableCashVnd;
            var result = new List<ObligationPaymentAllocation>();
            foreach (var item in entries.Where(x => x.OutstandingVnd > 0)
                .OrderBy(x => x.DueDateIso, StringComparer.Ordinal)
                .ThenBy(x => x.DueMinute)
                .ThenBy(x => x.CreationSequence)
                .ThenBy(x => x.ObligationIdentity, StringComparer.Ordinal)
                .ThenBy(x => x.Id, StringComparer.Ordinal))
            {
                if (remaining == 0) break;
                var pay = Math.Min(remaining, item.OutstandingVnd);
                remaining = checked(remaining - pay);
                if (pay > 0) result.Add(new ObligationPaymentAllocation(item.Id, pay));
            }
            return result.AsReadOnly();
        }
    }
}
