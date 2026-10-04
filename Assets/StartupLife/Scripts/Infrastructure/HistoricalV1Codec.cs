#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using StartupLife.Core;

namespace StartupLife.Infrastructure
{
    internal static class HistoricalV1Codec
    {
        public static byte[] PromoteSyntheticV0ToV1(byte[] payload)
        {
            var dto = Decode(payload, 0);
            dto.SaveVersion = SaveSchema.HistoricalV1;
            return JsonSaveSerializer.WriteObject(dto);
        }

        public static GameState DecodeToCurrent(byte[] payload)
        {
            var dto = Decode(payload, SaveSchema.HistoricalV1);
            return ToCurrent(dto);
        }

        public static byte[] EncodeFromCurrent(GameState state)
        {
            if (state.SaveVersion != SaveSchema.CurrentVersion || state.Businesses == null || state.Businesses.Count != 0)
                throw new ArgumentException("Current state is not representable by frozen v1.");
            if (state.Receipts.Any(x => (int)GameCommand.ParseCanonicalPayload(x.Payload).Kind > (int)CommandKind.AcknowledgePlayback))
                throw new ArgumentException("Business command history is not representable by frozen v1.");
            var dto = FromCurrent(state);
            Validate(dto, SaveSchema.HistoricalV1);
            return JsonSaveSerializer.WriteObject(dto);
        }

        public static void ValidatePayload(byte[] payload, int expectedVersion) => _ = Decode(payload, expectedVersion);

        private static V1GameState Decode(byte[] payload, int expectedVersion)
        {
            var dto = JsonSaveSerializer.ReadObject<V1GameState>(payload);
            Validate(dto, expectedVersion);
            return dto;
        }

        private static void Validate(V1GameState dto, int expectedVersion)
        {
            if (dto == null) throw new ArgumentException("Invalid frozen v1 root: null.");
            if (dto.SaveVersion != expectedVersion) throw new ArgumentException("Invalid frozen v1 root: version.");
            if (dto.ExtensionData != null) throw new ArgumentException("Invalid frozen v1 root: extension data.");
            if (dto.Arrears == null) throw new ArgumentException("Invalid frozen v1 root: Arrears.");
            if (dto.Claims == null) throw new ArgumentException("Invalid frozen v1 root: Claims.");
            if (dto.CompletedCourses == null) throw new ArgumentException("Invalid frozen v1 root: CompletedCourses.");
            if (dto.ConsumedActivities == null) throw new ArgumentException("Invalid frozen v1 root: ConsumedActivities.");
            if (dto.Grants == null) throw new ArgumentException("Invalid frozen v1 root: Grants.");
            if (dto.History == null) throw new ArgumentException("Invalid frozen v1 root: History.");
            if (dto.Ledger == null) throw new ArgumentException("Invalid frozen v1 root: Ledger.");
            if (dto.PreviousEmployment == null) throw new ArgumentException("Invalid frozen v1 root: PreviousEmployment.");
            if (dto.Receipts == null) throw new ArgumentException("Invalid frozen v1 root: Receipts.");
            if (dto.Scheduler == null) throw new ArgumentException("Invalid frozen v1 root: Scheduler.");
            if (dto.Skills == null) throw new ArgumentException("Invalid frozen v1 root: Skills.");
            RequireNoExtension(dto.Employment);
            RequireNoExtension(dto.Course);
            RequireNoExtension(dto.Scheduler);
            foreach (var value in dto.Arrears) RequireNoExtension(value);
            foreach (var value in dto.Claims) RequireNoExtension(value);
            foreach (var value in dto.CompletedCourses) RequireNoExtension(value);
            foreach (var value in dto.Ledger) RequireNoExtension(value);
            foreach (var value in dto.PreviousEmployment) RequireNoExtension(value);
            foreach (var value in dto.Receipts)
            {
                RequireNoExtension(value);
                var command = GameCommand.ParseCanonicalPayload(value.Payload);
                if ((int)command.Kind > (int)CommandKind.AcknowledgePlayback)
                    throw new ArgumentException("Frozen v1 cannot carry future command semantics.");
            }
            foreach (var value in dto.Skills) RequireNoExtension(value);
        }

        private static void RequireNoExtension(V1Extensible? value)
        {
            if (value == null) return;
            if (value.ExtensionData != null) throw new ArgumentException("Frozen v1 contains unknown members.");
        }

        private static GameState ToCurrent(V1GameState s) => new GameState
        {
            SaveVersion = SaveSchema.CurrentVersion,
            ContentVersion = s.ContentVersion,
            RunId = s.RunId,
            Seed = s.Seed,
            Revision = s.Revision,
            NextEntity = s.NextEntity,
            NextOperation = s.NextOperation,
            DateIso = s.DateIso,
            Minute = s.Minute,
            Name = s.Name,
            StartingAge = s.StartingAge,
            BirthDateIso = s.BirthDateIso,
            AppearanceId = s.AppearanceId,
            BackgroundId = s.BackgroundId,
            LearningSpeed = s.LearningSpeed,
            Cash = s.Cash,
            Employment = Map(s.Employment),
            PreviousEmployment = s.PreviousEmployment.Select(MapRequired).ToList(),
            Skills = s.Skills.Select(x => new SkillState { Id = x.Id, Exposure = x.Exposure, GrantedLevel = x.GrantedLevel }).ToList(),
            Course = Map(s.Course),
            CompletedCourses = s.CompletedCourses.Select(MapRequired).ToList(),
            Grants = new List<string>(s.Grants),
            Scheduler = new SchedulerState
            {
                Deck = new List<string>(s.Scheduler.Deck), Cursor = s.Scheduler.Cursor, Cycle = s.Scheduler.Cycle,
                Generation = s.Scheduler.Generation, Signature = s.Scheduler.Signature, LastScene = s.Scheduler.LastScene
            },
            SchedulerRng = s.SchedulerRng,
            EventRng = s.EventRng,
            RngVersion = s.RngVersion,
            Claims = s.Claims.Select(x => new SalaryClaim { Id = x.Id, EmploymentId = x.EmploymentId, EarnedIso = x.EarnedIso, Numerator = x.Numerator, Denominator = x.Denominator }).ToList(),
            Arrears = s.Arrears.Select(x => new ArrearState { Id = x.Id, DueIso = x.DueIso, Amount = x.Amount }).ToList(),
            Ledger = s.Ledger.Select(x => new LedgerEntry { Id = x.Id, OperationId = x.OperationId, AttributionId = x.AttributionId, Category = x.Category, CashDelta = x.CashDelta, Amount = x.Amount }).ToList(),
            Businesses = new List<BusinessState>(),
            History = new List<string>(s.History),
            Receipts = s.Receipts.Select(x => new CommandReceipt
            {
                CommandId = x.CommandId, Payload = x.Payload, OperationId = x.OperationId, Revision = x.Revision,
                Cue = x.Cue, MinutesConsumed = x.MinutesConsumed, AdvanceTargetIso = x.AdvanceTargetIso, ParentPayload = x.ParentPayload
            }).ToList(),
            ConsumedActivities = new List<string>(s.ConsumedActivities),
            CurrentActivity = s.CurrentActivity,
            CurrentCue = s.CurrentCue,
            PlaybackCursor = s.PlaybackCursor,
            PendingChoiceId = s.PendingChoiceId
        };

        private static V1GameState FromCurrent(GameState s) => new V1GameState
        {
            SaveVersion = SaveSchema.HistoricalV1,
            ContentVersion = s.ContentVersion,
            RunId = s.RunId,
            Seed = s.Seed,
            Revision = s.Revision,
            NextEntity = s.NextEntity,
            NextOperation = s.NextOperation,
            DateIso = s.DateIso,
            Minute = s.Minute,
            Name = s.Name,
            StartingAge = s.StartingAge,
            BirthDateIso = s.BirthDateIso,
            AppearanceId = s.AppearanceId,
            BackgroundId = s.BackgroundId,
            LearningSpeed = s.LearningSpeed,
            Cash = s.Cash,
            Employment = Map(s.Employment),
            PreviousEmployment = s.PreviousEmployment.Select(MapRequired).ToList(),
            Skills = s.Skills.Select(x => new V1SkillState { Id = x.Id, Exposure = x.Exposure, GrantedLevel = x.GrantedLevel }).ToList(),
            Course = Map(s.Course),
            CompletedCourses = s.CompletedCourses.Select(MapRequired).ToList(),
            Grants = new List<string>(s.Grants),
            Scheduler = new V1SchedulerState
            {
                Deck = new List<string>(s.Scheduler.Deck), Cursor = s.Scheduler.Cursor, Cycle = s.Scheduler.Cycle,
                Generation = s.Scheduler.Generation, Signature = s.Scheduler.Signature, LastScene = s.Scheduler.LastScene
            },
            SchedulerRng = s.SchedulerRng,
            EventRng = s.EventRng,
            RngVersion = s.RngVersion,
            Claims = s.Claims.Select(x => new V1SalaryClaim { Id = x.Id, EmploymentId = x.EmploymentId, EarnedIso = x.EarnedIso, Numerator = x.Numerator, Denominator = x.Denominator }).ToList(),
            Arrears = s.Arrears.Select(x => new V1ArrearState { Id = x.Id, DueIso = x.DueIso, Amount = x.Amount }).ToList(),
            Ledger = s.Ledger.Select(x => new V1LedgerEntry { Id = x.Id, OperationId = x.OperationId, AttributionId = x.AttributionId, Category = x.Category, CashDelta = x.CashDelta, Amount = x.Amount }).ToList(),
            History = new List<string>(s.History),
            Receipts = s.Receipts.Select(x => new V1CommandReceipt
            {
                CommandId = x.CommandId, Payload = x.Payload, OperationId = x.OperationId, Revision = x.Revision,
                Cue = x.Cue, MinutesConsumed = x.MinutesConsumed, AdvanceTargetIso = x.AdvanceTargetIso, ParentPayload = x.ParentPayload
            }).ToList(),
            ConsumedActivities = new List<string>(s.ConsumedActivities),
            CurrentActivity = s.CurrentActivity,
            CurrentCue = s.CurrentCue,
            PlaybackCursor = s.PlaybackCursor,
            PendingChoiceId = s.PendingChoiceId
        };

        private static EmploymentState? Map(V1EmploymentState? e) => e == null ? null : new EmploymentState
        {
            InstanceId = e.InstanceId, EmployerId = e.EmployerId, CareerId = e.CareerId, DefinitionRevision = e.DefinitionRevision,
            StartedIso = e.StartedIso, EndedIso = e.EndedIso, FirstShiftIso = e.FirstShiftIso, Xp = e.Xp, Rank = e.Rank,
            ScenesToday = e.ScenesToday, WorkDateIso = e.WorkDateIso
        };
        private static EmploymentState MapRequired(V1EmploymentState e) => Map(e)!;
        private static V1EmploymentState? Map(EmploymentState? e) => e == null ? null : new V1EmploymentState
        {
            InstanceId = e.InstanceId, EmployerId = e.EmployerId, CareerId = e.CareerId, DefinitionRevision = e.DefinitionRevision,
            StartedIso = e.StartedIso, EndedIso = e.EndedIso, FirstShiftIso = e.FirstShiftIso, Xp = e.Xp, Rank = e.Rank,
            ScenesToday = e.ScenesToday, WorkDateIso = e.WorkDateIso
        };
        private static V1EmploymentState MapRequired(EmploymentState e) => Map(e)!;
        private static CourseState? Map(V1CourseState? c) => c == null ? null : new CourseState
        { InstanceId = c.InstanceId, DefinitionId = c.DefinitionId, ProgressUnits = c.ProgressUnits, Completed = c.Completed };
        private static CourseState MapRequired(V1CourseState c) => Map(c)!;
        private static V1CourseState? Map(CourseState? c) => c == null ? null : new V1CourseState
        { InstanceId = c.InstanceId, DefinitionId = c.DefinitionId, ProgressUnits = c.ProgressUnits, Completed = c.Completed };
        private static V1CourseState MapRequired(CourseState c) => Map(c)!;
    }

    [DataContract]
    internal abstract class V1Extensible : IExtensibleDataObject
    {
        public ExtensionDataObject? ExtensionData { get; set; }
    }

    [DataContract]
    internal sealed class V1GameState : V1Extensible
    {
        [DataMember] public string AppearanceId { get; set; } = "";
        [DataMember] public List<V1ArrearState> Arrears { get; set; } = new List<V1ArrearState>();
        [DataMember] public string BackgroundId { get; set; } = "";
        [DataMember] public string BirthDateIso { get; set; } = "";
        [DataMember] public long Cash { get; set; }
        [DataMember] public List<V1SalaryClaim> Claims { get; set; } = new List<V1SalaryClaim>();
        [DataMember] public List<V1CourseState> CompletedCourses { get; set; } = new List<V1CourseState>();
        [DataMember] public List<string> ConsumedActivities { get; set; } = new List<string>();
        [DataMember] public string ContentVersion { get; set; } = "";
        [DataMember] public V1CourseState? Course { get; set; }
        [DataMember] public string CurrentActivity { get; set; } = "";
        [DataMember] public string CurrentCue { get; set; } = "";
        [DataMember] public string DateIso { get; set; } = "";
        [DataMember] public V1EmploymentState? Employment { get; set; }
        [DataMember] public uint EventRng { get; set; }
        [DataMember] public List<string> Grants { get; set; } = new List<string>();
        [DataMember] public List<string> History { get; set; } = new List<string>();
        [DataMember] public int LearningSpeed { get; set; }
        [DataMember] public List<V1LedgerEntry> Ledger { get; set; } = new List<V1LedgerEntry>();
        [DataMember] public int Minute { get; set; }
        [DataMember] public string Name { get; set; } = "";
        [DataMember] public long NextEntity { get; set; }
        [DataMember] public long NextOperation { get; set; }
        [DataMember] public string PendingChoiceId { get; set; } = "";
        [DataMember] public int PlaybackCursor { get; set; }
        [DataMember] public List<V1EmploymentState> PreviousEmployment { get; set; } = new List<V1EmploymentState>();
        [DataMember] public List<V1CommandReceipt> Receipts { get; set; } = new List<V1CommandReceipt>();
        [DataMember] public long Revision { get; set; }
        [DataMember] public string RngVersion { get; set; } = "";
        [DataMember] public string RunId { get; set; } = "";
        [DataMember] public int SaveVersion { get; set; }
        [DataMember] public V1SchedulerState Scheduler { get; set; } = new V1SchedulerState();
        [DataMember] public uint SchedulerRng { get; set; }
        [DataMember] public ulong Seed { get; set; }
        [DataMember] public List<V1SkillState> Skills { get; set; } = new List<V1SkillState>();
        [DataMember] public int StartingAge { get; set; }
    }

    [DataContract] internal sealed class V1EmploymentState : V1Extensible
    {
        [DataMember] public string CareerId { get; set; } = "";
        [DataMember] public string DefinitionRevision { get; set; } = "";
        [DataMember] public string EmployerId { get; set; } = "";
        [DataMember] public string EndedIso { get; set; } = "";
        [DataMember] public string FirstShiftIso { get; set; } = "";
        [DataMember] public string InstanceId { get; set; } = "";
        [DataMember] public int Rank { get; set; }
        [DataMember] public int ScenesToday { get; set; }
        [DataMember] public string StartedIso { get; set; } = "";
        [DataMember] public string WorkDateIso { get; set; } = "";
        [DataMember] public long Xp { get; set; }
    }
    [DataContract] internal sealed class V1SkillState : V1Extensible
    {
        [DataMember] public long Exposure { get; set; }
        [DataMember] public int GrantedLevel { get; set; }
        [DataMember] public string Id { get; set; } = "";
    }
    [DataContract] internal sealed class V1CourseState : V1Extensible
    {
        [DataMember] public bool Completed { get; set; }
        [DataMember] public string DefinitionId { get; set; } = "";
        [DataMember] public string InstanceId { get; set; } = "";
        [DataMember] public long ProgressUnits { get; set; }
    }
    [DataContract] internal sealed class V1SchedulerState : V1Extensible
    {
        [DataMember] public int Cursor { get; set; }
        [DataMember] public long Cycle { get; set; }
        [DataMember] public List<string> Deck { get; set; } = new List<string>();
        [DataMember] public long Generation { get; set; }
        [DataMember] public string LastScene { get; set; } = "";
        [DataMember] public string Signature { get; set; } = "";
    }
    [DataContract] internal sealed class V1SalaryClaim : V1Extensible
    {
        [DataMember] public string Denominator { get; set; } = "1";
        [DataMember] public string EarnedIso { get; set; } = "";
        [DataMember] public string EmploymentId { get; set; } = "";
        [DataMember] public string Id { get; set; } = "";
        [DataMember] public string Numerator { get; set; } = "0";
    }
    [DataContract] internal sealed class V1ArrearState : V1Extensible
    {
        [DataMember] public long Amount { get; set; }
        [DataMember] public string DueIso { get; set; } = "";
        [DataMember] public string Id { get; set; } = "";
    }
    [DataContract] internal sealed class V1LedgerEntry : V1Extensible
    {
        [DataMember] public long Amount { get; set; }
        [DataMember] public string AttributionId { get; set; } = "";
        [DataMember] public long CashDelta { get; set; }
        [DataMember] public string Category { get; set; } = "";
        [DataMember] public string Id { get; set; } = "";
        [DataMember] public string OperationId { get; set; } = "";
    }
    [DataContract] internal sealed class V1CommandReceipt : V1Extensible
    {
        [DataMember] public string AdvanceTargetIso { get; set; } = "";
        [DataMember] public string CommandId { get; set; } = "";
        [DataMember] public string Cue { get; set; } = "";
        [DataMember] public int MinutesConsumed { get; set; }
        [DataMember] public string OperationId { get; set; } = "";
        [DataMember] public string ParentPayload { get; set; } = "";
        [DataMember] public string Payload { get; set; } = "";
        [DataMember] public long Revision { get; set; }
    }
}
