#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace StartupLife.Core
{
    // Mutable candidate DTOs. Application owns them and publishes only detached immutable snapshots.
    public sealed class GameState
    {
        public int SaveVersion { get; set; } = SaveSchema.CurrentVersion;
        // Runtime-only, detached schema-3 wire authority. Never nested in GameState
        // serialization, which must preserve the byte-exact historical v2 DTO.
        [System.Runtime.Serialization.IgnoreDataMember]
        public EconomicV3Payload? EconomicV3 { get; set; }
        public string ContentVersion { get; set; } = "";
        public string RunId { get; set; } = "";
        public ulong Seed { get; set; }
        public long Revision { get; set; }
        public long NextEntity { get; set; } = 1;
        public long NextOperation { get; set; } = 1;
        public string DateIso { get; set; } = "2026-09-01";
        public int Minute { get; set; }
        public string Name { get; set; } = "";
        public int StartingAge { get; set; }
        public string BirthDateIso { get; set; } = "";
        public string AppearanceId { get; set; } = "";
        public string BackgroundId { get; set; } = "";
        public int LearningSpeed { get; set; }
        public long Cash { get; set; }
        public EmploymentState? Employment { get; set; }
        public List<EmploymentState> PreviousEmployment { get; set; } = new List<EmploymentState>();
        public List<SkillState> Skills { get; set; } = new List<SkillState>();
        public CourseState? Course { get; set; }
        public List<CourseState> CompletedCourses { get; set; } = new List<CourseState>();
        public List<string> Grants { get; set; } = new List<string>();
        public SchedulerState Scheduler { get; set; } = new SchedulerState();
        public uint SchedulerRng { get; set; }
        public uint EventRng { get; set; }
        public string RngVersion { get; set; } = "xorshift32-v1";
        public List<SalaryClaim> Claims { get; set; } = new List<SalaryClaim>();
        public List<ArrearState> Arrears { get; set; } = new List<ArrearState>();
        public List<LedgerEntry> Ledger { get; set; } = new List<LedgerEntry>();
        public List<BusinessState> Businesses { get; set; } = new List<BusinessState>();
        public List<string> History { get; set; } = new List<string>();
        public List<CommandReceipt> Receipts { get; set; } = new List<CommandReceipt>();
        public List<string> ConsumedActivities { get; set; } = new List<string>();
        public string CurrentActivity { get; set; } = "";
        public string CurrentCue { get; set; } = "";
        public int PlaybackCursor { get; set; }
        public string PendingChoiceId { get; set; } = "";
        public SimDate Date
        {
            get { var d = DateTime.ParseExact(DateIso, "yyyy-MM-dd", CultureInfo.InvariantCulture); return new SimDate(d.Year, d.Month, d.Day); }
        }
        public string NewEntity(string kind)
        { var sequence = NextEntity; NextEntity = checked(NextEntity + 1); return RunId + "/" + kind + "/" + sequence.ToString(CultureInfo.InvariantCulture); }
        public string NewOperation()
        { var sequence = NextOperation; NextOperation = checked(NextOperation + 1); return RunId + "/op/" + sequence.ToString(CultureInfo.InvariantCulture); }
    }
    public sealed class EmploymentState
    {
        public string InstanceId { get; set; } = "";
        public string EmployerId { get; set; } = "";
        public string CareerId { get; set; } = "";
        public string DefinitionRevision { get; set; } = "";
        public string StartedIso { get; set; } = "";
        public string EndedIso { get; set; } = "";
        public string FirstShiftIso { get; set; } = "";
        public long Xp { get; set; }
        public int Rank { get; set; }
        public int ScenesToday { get; set; }
        public string WorkDateIso { get; set; } = "";
    }
    public sealed class SkillState
    {
        public string Id { get; set; } = "";
        public long Exposure { get; set; }
        public int GrantedLevel { get; set; }
    }
    public sealed class CourseState
    {
        public string InstanceId { get; set; } = "";
        public string DefinitionId { get; set; } = "";
        public long ProgressUnits { get; set; }
        public bool Completed { get; set; }
    }
    public sealed class SchedulerState
    {
        public List<string> Deck { get; set; } = new List<string>();
        public int Cursor { get; set; }
        public long Cycle { get; set; }
        public long Generation { get; set; }
        public string Signature { get; set; } = "";
        public string LastScene { get; set; } = "";
    }
    public sealed class SalaryClaim
    {
        public string Id { get; set; } = "";
        public string EmploymentId { get; set; } = "";
        public string EarnedIso { get; set; } = "";
        public string Numerator { get; set; } = "0";
        public string Denominator { get; set; } = "1";
    }
    public sealed class ArrearState
    {
        public string Id { get; set; } = "";
        public string DueIso { get; set; } = "";
        public long Amount { get; set; }
    }
    public sealed class LedgerEntry
    {
        public string Id { get; set; } = "";
        public string OperationId { get; set; } = "";
        public string AttributionId { get; set; } = "";
        public string Category { get; set; } = "";
        public long CashDelta { get; set; }
        public long Amount { get; set; }
    }
    public sealed class CommandReceipt
    {
        public string CommandId { get; set; } = "";
        public string Payload { get; set; } = "";
        public string OperationId { get; set; } = "";
        public long Revision { get; set; }
        public string Cue { get; set; } = "";
        public int MinutesConsumed { get; set; }
        public string AdvanceTargetIso { get; set; } = "";
        public string ParentPayload { get; set; } = "";
    }
    public sealed class ActiveCourseSnapshot
    {
        public string InstanceId { get; }
        public string DefinitionId { get; }
        public string SkillId { get; }
        public long ProgressUnits { get; }
        public long TargetUnits { get; }
        public int TargetLevel { get; }
        public ActiveCourseSnapshot(CourseState state, CourseDefinition definition)
        {
            InstanceId = state.InstanceId; DefinitionId = state.DefinitionId; SkillId = definition.SkillId;
            ProgressUnits = state.ProgressUnits; TargetUnits = checked((long)definition.BaseMinutes * 10000);
            TargetLevel = definition.TargetLevel;
        }
    }
    public sealed class GameSnapshot
    {
        public string RunId { get; }
        public string Name { get; }
        public SimInstant Instant { get; }
        public long Revision { get; }
        public long Cash { get; }
        public long Arrears { get; }
        public long CareerXp { get; }
        public int Rank { get; }
        public string CareerId { get; }
        public string Cue { get; }
        public string CurrentActivityId { get; }
        public int PlaybackCursor { get; }
        public ActiveCourseSnapshot? ActiveCourse { get; }
        public IReadOnlyDictionary<string, int> SkillLevels { get; }
        public IReadOnlyList<string> History { get; }
        public GameSnapshot(GameState state, ContentCatalog content)
        {
            RunId = state.RunId; Name = state.Name; Instant = new SimInstant(state.Date, state.Minute);
            Revision = state.Revision; Cash = state.Cash; Arrears = state.Arrears.Sum(x => x.Amount);
            CareerXp = state.Employment?.Xp ?? 0; Rank = state.Employment?.Rank ?? 0;
            CareerId = state.Employment?.CareerId ?? ""; Cue = state.CurrentCue; CurrentActivityId = state.CurrentActivity;
            PlaybackCursor = state.PlaybackCursor;
            ActiveCourse = state.Course == null ? null : new ActiveCourseSnapshot(state.Course, content.Courses[state.Course.DefinitionId]);
            SkillLevels = new System.Collections.ObjectModel.ReadOnlyDictionary<string, int>(state.Skills.ToDictionary(x => x.Id, x => Level(x, content.Skills[x.Id]), StringComparer.Ordinal));
            History = Array.AsReadOnly(state.History.ToArray());
        }
        public static int Level(SkillState state, SkillDefinition definition) => Math.Max(state.GrantedLevel, definition.Thresholds.Count(t => state.Exposure >= t));
    }
}
