#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace StartupLife.Core
{
    [DataContract]
    public sealed class V3PoolSegmentSupply
    {
        [DataMember(Order=0)] public string SegmentId { get; set; } = "";
        [DataMember(Order=1)] public int OriginalUnits { get; set; }
    }
    [DataContract]
    public sealed class V3MarketDayPool
    {
        [DataMember(Order=0)] public string DateIso { get; set; } = "";
        [DataMember(Order=1)] public string PoolId { get; set; } = "";
        [DataMember(Order=2)] public string DefinitionRevision { get; set; } = "";
        [DataMember(Order=3)] public List<V3PoolSegmentSupply> Segments { get; set; } = new List<V3PoolSegmentSupply>();
    }
    public enum V3CostReservationStatus { Active=1, Consumed=2, Released=3 }
    [DataContract]
    public sealed class V3CostReservation
    {
        [DataMember(Order=0)] public string Id { get; set; } = "";
        [DataMember(Order=1)] public string DateIso { get; set; } = "";
        [DataMember(Order=2)] public string EpochId { get; set; } = "";
        [DataMember(Order=3)] public string BusinessInstanceId { get; set; } = "";
        [DataMember(Order=4)] public int BusinessUnitOrdinal { get; set; }
        [DataMember(Order=5)] public long AmountVnd { get; set; }
        [DataMember(Order=6)] public V3CostReservationStatus Status { get; set; }
        [DataMember(Order=7)] public string SourceOperationId { get; set; } = "";
    }
    [DataContract]
    public sealed class V3BusinessOperationSlice
    {
        [DataMember(Order=0)] public string Id { get; set; } = "";
        [DataMember(Order=1)] public string DateIso { get; set; } = "";
        [DataMember(Order=2)] public string EpochId { get; set; } = "";
        [DataMember(Order=3)] public string BusinessInstanceId { get; set; } = "";
        [DataMember(Order=4)] public int StartMinute { get; set; }
        [DataMember(Order=5)] public int EndMinute { get; set; }
        [DataMember(Order=6)] public int CommittedOwnerMinuteDelta { get; set; }
        [DataMember(Order=7)] public int UnitsDelta { get; set; }
        [DataMember(Order=8)] public long VariablePaidVnd { get; set; }
        [DataMember(Order=9)] public string SourceBoundaryOperationId { get; set; } = "";
        [DataMember(Order=10)] public List<string> FulfillmentIds { get; set; } = new List<string>();
    }
    [DataContract]
    public sealed class V3UnitFulfillment
    {
        [DataMember(Order=0)] public string Id { get; set; } = "";
        [DataMember(Order=1)] public string DateIso { get; set; } = "";
        [DataMember(Order=2)] public string SliceId { get; set; } = "";
        [DataMember(Order=3)] public string EpochId { get; set; } = "";
        [DataMember(Order=4)] public string BusinessInstanceId { get; set; } = "";
        [DataMember(Order=5)] public string PoolId { get; set; } = "";
        [DataMember(Order=6)] public string SegmentId { get; set; } = "";
        [DataMember(Order=7)] public int MarketUnitOrdinal { get; set; }
        [DataMember(Order=8)] public int BusinessUnitOrdinal { get; set; }
        [DataMember(Order=9)] public string SourceCostReservationId { get; set; } = "";
        [DataMember(Order=10)] public long UnitPriceVnd { get; set; }
        [DataMember(Order=11)] public long VariablePaidVnd { get; set; }
        [DataMember(Order=12)] public string ProfileRevision { get; set; } = "";
        [DataMember(Order=13)] public string RulesetRevision { get; set; } = "";
        [DataMember(Order=14)] public string SourceBoundaryOperationId { get; set; } = "";
    }
    [DataContract]
    public sealed class V3BusinessDaySettlement
    {
        [DataMember(Order=0)] public string Id { get; set; } = "";
        [DataMember(Order=1)] public string OldDateIso { get; set; } = "";
        [DataMember(Order=2)] public string SourceBoundaryOperationId { get; set; } = "";
        [DataMember(Order=3)] public List<string> FulfillmentIds { get; set; } = new List<string>();
        [DataMember(Order=4)] public long GrossVnd { get; set; }
        [DataMember(Order=5)] public long VariablePaidVnd { get; set; }
        [DataMember(Order=6)] public long FixedRecognizedVnd { get; set; }
        [DataMember(Order=7)] public long ProfitVnd { get; set; }
        [DataMember(Order=8)] public long AppliedToArrearsVnd { get; set; }
        [DataMember(Order=9)] public long NetCashCreditVnd { get; set; }
    }

    // Root intrinsic validator: only immutable source IDs can create money,
    // one market ordinal is sold once and each sold unit consumes one cost reservation.
    // This does not replace GameSession transaction CAS or deterministic replay.
    [DataContract]
    public sealed class V3FinancialProvenance
    {
        [DataMember(Order=0)] public List<V3MarketDayPool> MarketPools { get; set; } = new List<V3MarketDayPool>();
        [DataMember(Order=1)] public List<V3CostReservation> CostReservations { get; set; } = new List<V3CostReservation>();
        [DataMember(Order=2)] public List<V3BusinessOperationSlice> OperationSlices { get; set; } = new List<V3BusinessOperationSlice>();
        [DataMember(Order=3)] public List<V3UnitFulfillment> Fulfillments { get; set; } = new List<V3UnitFulfillment>();
        [DataMember(Order=4)] public List<V3BusinessDaySettlement> Settlements { get; set; } = new List<V3BusinessDaySettlement>();

        public void Validate()
        {
            if (MarketPools==null || CostReservations==null || OperationSlices==null ||
                Fulfillments==null || Settlements==null ||
                MarketPools.Any(x=>x==null) || CostReservations.Any(x=>x==null) ||
                OperationSlices.Any(x=>x==null) || Fulfillments.Any(x=>x==null) ||
                Settlements.Any(x=>x==null))
                throw new ArgumentException("Missing financial provenance graph.");
            var pools = new Dictionary<string,V3MarketDayPool>(StringComparer.Ordinal);
            foreach (var pool in MarketPools)
            {
                Date(pool.DateIso);
                if (!ContentId.IsValid(pool.PoolId) || string.IsNullOrWhiteSpace(pool.DefinitionRevision) ||
                    pool.Segments==null || pool.Segments.Count==0 ||
                    pool.Segments.Any(x=>x==null || !ContentId.IsValid(x.SegmentId) || x.OriginalUnits<0) ||
                    pool.Segments.Select(x=>x.SegmentId).Distinct(StringComparer.Ordinal).Count()!=pool.Segments.Count ||
                    !pools.TryAdd(Key(pool.DateIso,pool.PoolId),pool))
                    throw new ArgumentException("Invalid or duplicate day-opening market pool.");
            }
            var costs = UniqueMap(CostReservations,x=>x.Id);
            foreach (var cost in costs.Values)
            {
                Date(cost.DateIso);
                if (string.IsNullOrWhiteSpace(cost.EpochId) || string.IsNullOrWhiteSpace(cost.BusinessInstanceId) ||
                    string.IsNullOrWhiteSpace(cost.SourceOperationId) || cost.BusinessUnitOrdinal<0 ||
                    cost.AmountVnd<0 || !Enum.IsDefined(typeof(V3CostReservationStatus),cost.Status))
                    throw new ArgumentException("Invalid cost reservation.");
            }
            var slices = UniqueMap(OperationSlices,x=>x.Id);
            foreach (var slice in slices.Values)
            {
                Date(slice.DateIso);
                if (string.IsNullOrWhiteSpace(slice.EpochId) ||
                    string.IsNullOrWhiteSpace(slice.BusinessInstanceId) ||
                    string.IsNullOrWhiteSpace(slice.SourceBoundaryOperationId) ||
                    slice.StartMinute<0 || slice.EndMinute>1440 || slice.EndMinute<=slice.StartMinute ||
                    slice.CommittedOwnerMinuteDelta<0 ||
                    slice.CommittedOwnerMinuteDelta>slice.EndMinute-slice.StartMinute ||
                    slice.UnitsDelta<0 || slice.VariablePaidVnd<0 ||
                    slice.FulfillmentIds==null || slice.FulfillmentIds.Count!=slice.UnitsDelta ||
                    slice.FulfillmentIds.Any(string.IsNullOrWhiteSpace) ||
                    slice.FulfillmentIds.Distinct(StringComparer.Ordinal).Count()!=slice.FulfillmentIds.Count)
                    throw new ArgumentException("Invalid committed slice.");
            }
            var sales = UniqueMap(Fulfillments,x=>x.Id);
            var marketKeys = new HashSet<string>(StringComparer.Ordinal);
            var businessOrdinals = new HashSet<string>(StringComparer.Ordinal);
            var consumed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sale in sales.Values)
            {
                Date(sale.DateIso);
                if (!slices.TryGetValue(sale.SliceId,out var slice) ||
                    slice.DateIso!=sale.DateIso || slice.EpochId!=sale.EpochId ||
                    slice.BusinessInstanceId!=sale.BusinessInstanceId ||
                    slice.SourceBoundaryOperationId!=sale.SourceBoundaryOperationId ||
                    !slice.FulfillmentIds.Contains(sale.Id) ||
                    !costs.TryGetValue(sale.SourceCostReservationId,out var cost) ||
                    cost.Status!=V3CostReservationStatus.Consumed ||
                    cost.DateIso!=sale.DateIso || cost.EpochId!=sale.EpochId ||
                    cost.BusinessInstanceId!=sale.BusinessInstanceId ||
                    cost.BusinessUnitOrdinal!=sale.BusinessUnitOrdinal ||
                    cost.AmountVnd!=sale.VariablePaidVnd ||
                    !consumed.Add(sale.SourceCostReservationId) ||
                    sale.MarketUnitOrdinal<0 || sale.BusinessUnitOrdinal<0 ||
                    sale.UnitPriceVnd<=0 || sale.VariablePaidVnd<0 ||
                    string.IsNullOrWhiteSpace(sale.ProfileRevision) ||
                    string.IsNullOrWhiteSpace(sale.RulesetRevision) ||
                    !pools.TryGetValue(Key(sale.DateIso,sale.PoolId),out var pool) ||
                    !ContentId.IsValid(sale.SegmentId) ||
                    !pool.Segments.Any(x=>x.SegmentId==sale.SegmentId &&
                        sale.MarketUnitOrdinal<x.OriginalUnits) ||
                    !marketKeys.Add(Key(sale.DateIso,sale.PoolId,sale.SegmentId,
                        sale.MarketUnitOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture))) ||
                    !businessOrdinals.Add(Key(sale.DateIso,sale.BusinessInstanceId,
                        sale.BusinessUnitOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture))))
                    throw new ArgumentException("Orphan, double-sold or unfunded committed market unit.");
            }
            foreach (var slice in slices.Values)
            {
                var owned = sales.Values.Where(x=>x.SliceId==slice.Id).ToArray();
                if (owned.Length!=slice.UnitsDelta || checked(owned.Sum(x=>x.VariablePaidVnd))!=slice.VariablePaidVnd)
                    throw new ArgumentException("Slice units and variable payments do not match fulfillment.");
            }
            foreach (var cost in costs.Values)
                if (cost.Status==V3CostReservationStatus.Consumed && !consumed.Contains(cost.Id))
                    throw new ArgumentException("Consumed cost reservation lacks exactly one market sale.");

            var settlements = UniqueMap(Settlements,x=>x.Id);
            var settledSales = new HashSet<string>(StringComparer.Ordinal);
            var dates = new HashSet<string>(StringComparer.Ordinal);
            foreach (var settlement in settlements.Values)
            {
                Date(settlement.OldDateIso);
                if (string.IsNullOrWhiteSpace(settlement.SourceBoundaryOperationId) ||
                    !dates.Add(settlement.OldDateIso) || settlement.FulfillmentIds==null ||
                    settlement.FulfillmentIds.Any(x=>!sales.ContainsKey(x) || !settledSales.Add(x) ||
                        sales[x].DateIso!=settlement.OldDateIso) ||
                    settlement.FulfillmentIds.Distinct(StringComparer.Ordinal).Count()!=settlement.FulfillmentIds.Count ||
                    settlement.GrossVnd<0 || settlement.VariablePaidVnd<0 ||
                    settlement.FixedRecognizedVnd<0 || settlement.AppliedToArrearsVnd<0 ||
                    settlement.NetCashCreditVnd<0)
                    throw new ArgumentException("Invalid settlement provenance.");
                var day = sales.Values.Where(x=>x.DateIso==settlement.OldDateIso).ToArray();
                if (day.Length!=settlement.FulfillmentIds.Count ||
                    checked(day.Sum(x=>x.UnitPriceVnd))!=settlement.GrossVnd ||
                    checked(day.Sum(x=>x.VariablePaidVnd))!=settlement.VariablePaidVnd ||
                    checked(checked(settlement.GrossVnd-settlement.VariablePaidVnd)-
                        settlement.FixedRecognizedVnd)!=settlement.ProfitVnd ||
                    checked(settlement.AppliedToArrearsVnd+settlement.NetCashCreditVnd)!=settlement.GrossVnd)
                    throw new ArgumentException("Settlement gross/expense/profit/cash conservation failed.");
            }
        }

        private static Dictionary<string,T> UniqueMap<T>(IEnumerable<T> items,Func<T,string> key) where T:class
        {
            var map = new Dictionary<string,T>(StringComparer.Ordinal);
            foreach (var item in items)
            {
                var id = key(item);
                if (string.IsNullOrWhiteSpace(id) || !map.TryAdd(id,item))
                    throw new ArgumentException("Duplicate or missing financial record ID.");
            }
            return map;
        }
        private static void Date(string iso) => _ = DateTime.ParseExact(iso,"yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None);
        private static string Key(params string[] parts) =>
            string.Concat(parts.Select(x=>x.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)+":"+x));
    }
}
