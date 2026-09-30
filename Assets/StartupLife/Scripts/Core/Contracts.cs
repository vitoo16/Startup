#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;

namespace StartupLife.Core
{
    public enum CommandKind { CreateCharacter, AcceptJob, AdvanceBoundary, PurchaseCourse, Study, Resign, AcknowledgePlayback }
    public sealed class GameCommand
    {
        public CommandKind Kind { get; }
        public string ContentId { get; }
        public string Name { get; }
        public string AppearanceId { get; }
        public int Amount { get; }
        public GameCommand(CommandKind kind, string contentId = "", int amount = 0, string name = "", string appearanceId = "")
        { Kind = kind; ContentId = contentId; Amount = amount; Name = name; AppearanceId = appearanceId; }
        public string CanonicalPayload => ((int)Kind).ToString(CultureInfo.InvariantCulture) + ":" + Field(ContentId) + Field(Name) + Field(AppearanceId) + Amount.ToString(CultureInfo.InvariantCulture);
        private static string Field(string value) => value.Length.ToString(CultureInfo.InvariantCulture) + ":" + value;
    }
    public sealed class CommandEnvelope
    {
        public string RunId { get; }
        public string CommandId { get; }
        public long ExpectedRevision { get; }
        public GameCommand Command { get; }
        public CommandEnvelope(string runId, string commandId, long revision, GameCommand command)
        { RunId = runId; CommandId = commandId; ExpectedRevision = revision; Command = command; }
    }
    public enum CommandStatus { Committed, AlreadyCommitted, Rejected, PersistenceFailed, RecoveryRequired }
    public sealed class CommandResult
    {
        public CommandStatus Status { get; }
        public string ReasonKey { get; }
        public long Revision { get; }
        public string OperationId { get; }
        public int MinutesConsumed { get; }
        public CommandResult(CommandStatus status, string reason, long revision, string operationId = "", int minutes = 0)
        { Status = status; ReasonKey = reason; Revision = revision; OperationId = operationId; MinutesConsumed = minutes; }
    }
    public sealed class AdvanceResult
    {
        public IReadOnlyList<CommandResult> Boundaries { get; }
        public string StopReason { get; }
        public AdvanceResult(IReadOnlyList<CommandResult> boundaries, string reason) { Boundaries = boundaries; StopReason = reason; }
    }
    public interface IGameCommands
    {
        CommandResult Execute(CommandEnvelope command);
        AdvanceResult AdvanceDay(CommandEnvelope request);
        AdvanceResult AdvanceMonth(CommandEnvelope request);
        GameSnapshot Snapshot();
    }
    public interface ISaveSerializer
    {
        byte[] Serialize(GameState state);
        LoadResult DeserializeAndValidate(byte[] bytes);
    }
    public enum LoadStatus { Valid, Missing, Corrupt, Unreadable, FutureVersion, UnsupportedContent, RecoveredBackup }
    public sealed class LoadResult
    {
        public LoadStatus Status { get; }
        public GameState? State { get; }
        public string Reason { get; }
        public LoadResult(LoadStatus status, GameState? state = null, string reason = "") { Status = status; State = state; Reason = reason; }
    }
    public enum WriteStatus { Committed, Failed, Ambiguous }
    public interface ISaveStore
    {
        LoadResult Read();
        WriteStatus Commit(byte[] validatedBytes, long expectedRevision);
    }
    public interface ISaveMigration
    {
        int FromVersion { get; }
        int ToVersion { get; }
        byte[] Migrate(byte[] payload);
    }
    public sealed class RuleFailure : Exception
    {
        public string ReasonKey { get; }
        public RuleFailure(string key) : base(key) { ReasonKey = key; }
    }
}
