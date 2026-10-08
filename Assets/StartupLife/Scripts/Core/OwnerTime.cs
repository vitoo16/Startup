#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace StartupLife.Core
{
    // Pure, immutable planning values. A preview does not reserve or consume simulation time.
    public sealed class OwnerTimeInterval
    {
        public int StartMinute { get; }
        public int EndMinute { get; }
        public int Duration => EndMinute - StartMinute;

        public OwnerTimeInterval(int startMinute, int endMinute)
        {
            if (startMinute < 0 || startMinute >= endMinute || endMinute > 1440)
                throw new ArgumentException("Owner-time intervals must be nonempty, within a day.");
            StartMinute = startMinute;
            EndMinute = endMinute;
        }
    }

    public sealed class OwnerTimeAllocation
    {
        public string BusinessInstanceId { get; }
        public string DefinitionId { get; }
        public string DefinitionRevision { get; }
        public OwnerTimeInterval Interval { get; }

        public OwnerTimeAllocation(string instanceId, string definitionId, string definitionRevision, OwnerTimeInterval interval)
        {
            if (string.IsNullOrWhiteSpace(instanceId) || !ContentId.IsValid(definitionId) ||
                string.IsNullOrWhiteSpace(definitionRevision) || interval == null)
                throw new ArgumentException("Invalid owner-time allocation.");
            BusinessInstanceId = instanceId;
            DefinitionId = definitionId;
            DefinitionRevision = definitionRevision;
            Interval = interval;
        }
    }

    public enum BusinessPlanStatus
    {
        FullyAllocated = 1,
        PartiallyAllocated = 2,
        NoAvailableTime = 3,
        IndividuallyIneligible = 4,
        FullTimeBlockUnavailable = 5,
        MultipleFullTimeUnsupported = 6,
        UnsupportedOperationMode = 7,
        UnsupportedContent = 8,
        UnscheduledDefinition = 9
    }

    public sealed class BusinessOperatingCapacity
    {
        public string BusinessInstanceId { get; }
        public int RequiredOwnerMinutes { get; }
        public int AllocatedOwnerMinutes { get; }
        public int CapacityPermyriad { get; }
        public BusinessPlanStatus Status { get; }

        public BusinessOperatingCapacity(string instanceId, int requiredMinutes, int allocatedMinutes, BusinessPlanStatus status)
        {
            if (string.IsNullOrWhiteSpace(instanceId) || requiredMinutes < 0 || allocatedMinutes < 0 ||
                (requiredMinutes == 0 && allocatedMinutes != 0) || allocatedMinutes > requiredMinutes ||
                !Enum.IsDefined(typeof(BusinessPlanStatus), status))
                throw new ArgumentException("Invalid business capacity.");
            if (status == BusinessPlanStatus.FullyAllocated && (requiredMinutes == 0 || allocatedMinutes != requiredMinutes))
                throw new ArgumentException("Full allocation requires the exact reference minutes.");
            if (status == BusinessPlanStatus.PartiallyAllocated &&
                (allocatedMinutes == 0 || allocatedMinutes >= requiredMinutes))
                throw new ArgumentException("Partial allocation must be positive but below reference.");
            if (status != BusinessPlanStatus.FullyAllocated && status != BusinessPlanStatus.PartiallyAllocated && allocatedMinutes != 0)
                throw new ArgumentException("Unavailable businesses cannot retain owner-time allocation.");
            BusinessInstanceId = instanceId;
            RequiredOwnerMinutes = requiredMinutes;
            AllocatedOwnerMinutes = allocatedMinutes;
            CapacityPermyriad = requiredMinutes == 0 ? 0 :
                checked((int)Math.Min(10000L, checked(10000L * allocatedMinutes) / requiredMinutes));
            Status = status;
        }

        // A capacity cap is an explicit consumer/test input; demand and settlement belong to M9-T02.
        public int EffectiveCapacity(int fullCapacity)
        {
            if (fullCapacity < 0) throw new ArgumentOutOfRangeException(nameof(fullCapacity));
            return RequiredOwnerMinutes == 0 ? 0 : checked((int)(checked((long)fullCapacity * AllocatedOwnerMinutes) / RequiredOwnerMinutes));
        }
    }

    public enum OwnerDayPlanStatus
    {
        Ready = 1,
        UnsupportedContent = 2,
        MultipleFullTimeUnsupported = 3
    }

    public sealed class OwnerDayAllocation
    {
        public SimDate Date { get; }
        public long StateRevision { get; }
        public string ContentVersion { get; }
        public int CutoffMinute { get; }
        public OwnerDayPlanStatus Status { get; }
        public IReadOnlyList<OwnerTimeInterval> EmploymentIntervals { get; }
        public IReadOnlyList<OwnerTimeInterval> StudyIntervals { get; }
        public IReadOnlyList<OwnerTimeInterval> ElapsedIntervals { get; }
        public IReadOnlyList<OwnerTimeInterval> FreeIntervals { get; }
        public IReadOnlyList<OwnerTimeAllocation> Allocations { get; }
        public IReadOnlyList<BusinessOperatingCapacity> Capacities { get; }

        public OwnerDayAllocation(SimDate date, long revision, string contentVersion, int cutoffMinute,
            OwnerDayPlanStatus status, IEnumerable<OwnerTimeInterval> employment,
            IEnumerable<OwnerTimeInterval> study, IEnumerable<OwnerTimeInterval> elapsed,
            IEnumerable<OwnerTimeInterval> free, IEnumerable<OwnerTimeAllocation> allocations,
            IEnumerable<BusinessOperatingCapacity> capacities)
        {
            if (revision < 0 || string.IsNullOrWhiteSpace(contentVersion) || cutoffMinute < 0 || cutoffMinute > 1440 ||
                !Enum.IsDefined(typeof(OwnerDayPlanStatus), status))
                throw new ArgumentException("Invalid owner-day planning identity.");
            Date = date;
            StateRevision = revision;
            ContentVersion = contentVersion;
            CutoffMinute = cutoffMinute;
            Status = status;
            EmploymentIntervals = CopyIntervals(employment);
            StudyIntervals = CopyIntervals(study);
            ElapsedIntervals = CopyIntervals(elapsed);
            FreeIntervals = CopyIntervals(free);
            Allocations = Array.AsReadOnly((allocations ?? throw new ArgumentNullException(nameof(allocations)))
                .OrderBy(x => x.Interval.StartMinute).ThenBy(x => x.Interval.EndMinute)
                .ThenBy(x => x.DefinitionId, StringComparer.Ordinal)
                .ThenBy(x => x.BusinessInstanceId, StringComparer.Ordinal).ToArray());
            Capacities = Array.AsReadOnly((capacities ?? throw new ArgumentNullException(nameof(capacities)))
                .OrderBy(x => x.BusinessInstanceId, StringComparer.Ordinal).ToArray());
            for (var i = 1; i < Allocations.Count; i++)
                if (Allocations[i].Interval.StartMinute < Allocations[i - 1].Interval.EndMinute)
                    throw new ArgumentException("Business allocations overlap.");
        }

        private static IReadOnlyList<OwnerTimeInterval> CopyIntervals(IEnumerable<OwnerTimeInterval> items)
        {
            var input = (items ?? throw new ArgumentNullException(nameof(items))).ToArray();
            if (input.Any(x => x == null)) throw new ArgumentException("Null time interval.");
            var copy = input.OrderBy(x => x.StartMinute).ThenBy(x => x.EndMinute).ToArray();
            for (var i = 1; i < copy.Length; i++)
                if (copy[i].StartMinute < copy[i - 1].EndMinute)
                    throw new ArgumentException("Overlapping intervals in one classification.");
            return Array.AsReadOnly(copy);
        }
    }

    public interface IOwnerDayAllocationReadModel
    {
        OwnerDayAllocation ReadOwnerDayAllocation();
    }
}
