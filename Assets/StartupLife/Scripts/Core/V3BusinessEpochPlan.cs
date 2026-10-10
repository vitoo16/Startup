#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace StartupLife.Core
{
    // Exact future-time reservation snapshot. It MUST be serialized to survive cold restore;
    // a UI preview or a recomputed plan cannot substitute for committed epoch identity.
    [DataContract]
    public sealed class V3PlannedMarketUnit
    {
        [DataMember(Order=0)] public string CostReservationId { get; set; } = "";
        [DataMember(Order=1)] public string BusinessInstanceId { get; set; } = "";
        [DataMember(Order=2)] public int BusinessUnitOrdinal { get; set; }
        [DataMember(Order=3)] public string PoolId { get; set; } = "";
        [DataMember(Order=4)] public string SegmentId { get; set; } = "";
        [DataMember(Order=5)] public int MarketUnitOrdinal { get; set; }
        [DataMember(Order=6)] public long EffectiveUnitPriceVnd { get; set; }
        [DataMember(Order=7)] public PricingPosture EffectivePosture { get; set; }
        [DataMember(Order=8)] public string ProfileRevision { get; set; } = "";
        [DataMember(Order=9)] public string RulesetRevision { get; set; } = "";
    }

    [DataContract]
    public sealed class V3BusinessEpochAssignment
    {
        [DataMember(Order=0)] public string BusinessInstanceId { get; set; } = "";
        [DataMember(Order=1)] public int RequiredOwnerMinutes { get; set; }
        [DataMember(Order=2)] public int FullCapacityUnits { get; set; }
        [DataMember(Order=3)] public int AlreadyWorkedMinutes { get; set; }
        [DataMember(Order=4)] public int AlreadyFulfilledUnits { get; set; }
        [DataMember(Order=5)] public List<int> FrozenFutureOwnerMinutes { get; set; } = new List<int>();
        [DataMember(Order=6)] public List<string> OrderedCostReservationIds { get; set; } = new List<string>();
        [DataMember(Order=7)] public int WorkedInEpoch { get; set; }
        [DataMember(Order=8)] public int FulfilledInEpoch { get; set; }
    }

    [DataContract]
    public sealed class V3BusinessEpochPlan
    {
        [DataMember(Order=0)] public string Id { get; set; } = "";
        [DataMember(Order=1)] public string DateIso { get; set; } = "";
        [DataMember(Order=2)] public int StartMinute { get; set; }
        [DataMember(Order=3)] public int ProcessedUntilMinute { get; set; }
        [DataMember(Order=4)] public string SourceOperationId { get; set; } = "";
        [DataMember(Order=5)] public string RulesetRevision { get; set; } = "";
        [DataMember(Order=6)] public List<V3BusinessEpochAssignment> Assignments { get; set; } =
            new List<V3BusinessEpochAssignment>();
        [DataMember(Order=7)] public List<V3PlannedMarketUnit> PlannedUnits { get; set; } =
            new List<V3PlannedMarketUnit>();

        public void Validate(V3FinancialProvenance provenance)
        {
            if (provenance == null) throw new ArgumentNullException(nameof(provenance));
            _ = DateTime.ParseExact(DateIso, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(SourceOperationId) ||
                string.IsNullOrWhiteSpace(RulesetRevision) ||
                StartMinute < 0 || StartMinute >= 1440 ||
                ProcessedUntilMinute < StartMinute || ProcessedUntilMinute > 1440 ||
                Assignments == null || PlannedUnits == null ||
                Assignments.Any(x => x == null) || PlannedUnits.Any(x => x == null))
                throw new ArgumentException("Malformed persisted v3 economic epoch.");
            var registered = new HashSet<string>(StringComparer.Ordinal);
            var reservedMinutes = new HashSet<int>();
            var orderedCosts = new HashSet<string>(StringComparer.Ordinal);
            var allCosts = provenance.CostReservations
                .ToDictionary(x => x.Id, x => x, StringComparer.Ordinal);
            foreach (var a in Assignments)
            {
                if (string.IsNullOrWhiteSpace(a.BusinessInstanceId) ||
                    !registered.Add(a.BusinessInstanceId) ||
                    a.RequiredOwnerMinutes < 1 || a.RequiredOwnerMinutes > 840 ||
                    a.FullCapacityUnits < 0 || a.FullCapacityUnits > 1440 ||
                    a.AlreadyWorkedMinutes < 0 || a.AlreadyWorkedMinutes > 1440 ||
                    a.AlreadyFulfilledUnits < 0 || a.AlreadyFulfilledUnits > a.FullCapacityUnits ||
                    a.FrozenFutureOwnerMinutes == null || a.OrderedCostReservationIds == null ||
                    a.FrozenFutureOwnerMinutes.Count > 1440 ||
                    a.WorkedInEpoch < 0 || a.WorkedInEpoch > a.FrozenFutureOwnerMinutes.Count ||
                    a.FulfilledInEpoch < 0 ||
                    a.FulfilledInEpoch > a.OrderedCostReservationIds.Count)
                    throw new ArgumentException("Invalid saved owner-time capacity assignment.");
                var last = -1;
                foreach (var minute in a.FrozenFutureOwnerMinutes)
                {
                    if (minute < StartMinute || minute >= 1440 || minute <= last ||
                        !reservedMinutes.Add(minute))
                        throw new ArgumentException("Overlapping, backward or duplicated owner minute.");
                    last = minute;
                }
                foreach (var reservationId in a.OrderedCostReservationIds)
                    if (string.IsNullOrWhiteSpace(reservationId) ||
                        !orderedCosts.Add(reservationId) ||
                        !allCosts.TryGetValue(reservationId, out var cost) ||
                        cost.EpochId != Id || cost.DateIso != DateIso ||
                        cost.BusinessInstanceId != a.BusinessInstanceId)
                        throw new ArgumentException("Unfunded or orphaned epoch quota.");
                var processedMinutes = a.FrozenFutureOwnerMinutes.Count(x => x < ProcessedUntilMinute);
                if (processedMinutes != a.WorkedInEpoch)
                    throw new ArgumentException("Persisted epoch progress differs from processed minute frontier.");
                var priorUnits = a.OrderedCostReservationIds.Take(a.FulfilledInEpoch).ToArray();
                var futureUnits = a.OrderedCostReservationIds.Skip(a.FulfilledInEpoch).ToArray();
                if (priorUnits.Any(x => allCosts[x].Status != V3CostReservationStatus.Consumed) ||
                    futureUnits.Any(x => allCosts[x].Status == V3CostReservationStatus.Consumed))
                    throw new ArgumentException("Persisted cost consumption diverges from accepted units.");
            }
            if (PlannedUnits.Count != orderedCosts.Count ||
                PlannedUnits.Select(x => x.CostReservationId)
                    .Distinct(StringComparer.Ordinal).Count() != PlannedUnits.Count)
                throw new ArgumentException("Market reservation count differs from wallet reservations.");
            var unitKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var unit in PlannedUnits)
            {
                if (!orderedCosts.Contains(unit.CostReservationId) ||
                    !allCosts.TryGetValue(unit.CostReservationId, out var cost) ||
                    unit.BusinessInstanceId != cost.BusinessInstanceId ||
                    unit.BusinessUnitOrdinal != cost.BusinessUnitOrdinal ||
                    !ContentId.IsValid(unit.PoolId) || !ContentId.IsValid(unit.SegmentId) ||
                    unit.MarketUnitOrdinal < 0 || unit.EffectiveUnitPriceVnd <= 0 ||
                    !Enum.IsDefined(typeof(PricingPosture), unit.EffectivePosture) ||
                    string.IsNullOrWhiteSpace(unit.ProfileRevision) ||
                    string.IsNullOrWhiteSpace(unit.RulesetRevision) ||
                    !unitKeys.Add(unit.PoolId.Length + ":" + unit.PoolId +
                        unit.SegmentId.Length + ":" + unit.SegmentId + ":" + unit.MarketUnitOrdinal))
                    throw new ArgumentException("Market-unit plan lacks immutable price/segment/cost linkage.");
            }
        }
    }
}
