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
        public static GameCommand ParseCanonicalPayload(string payload)
        {
            var position = 0;
            var separator = payload.IndexOf(':');
            if (separator < 1 || !int.TryParse(payload.Substring(0, separator), NumberStyles.None, CultureInfo.InvariantCulture, out var kind) ||
                !Enum.IsDefined(typeof(CommandKind), kind)) throw new ArgumentException("Invalid command payload.");
            position = separator + 1;
            var content = ReadField(payload, ref position);
            var name = ReadField(payload, ref position);
            var appearance = ReadField(payload, ref position);
            if (!int.TryParse(payload.Substring(position), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var amount))
                throw new ArgumentException("Invalid command amount.");
            var command = new GameCommand((CommandKind)kind, content, amount, name, appearance);
            if (command.CanonicalPayload != payload) throw new ArgumentException("Noncanonical command payload.");
            return command;
        }
        private static string ReadField(string payload, ref int position)
        {
            var separator = payload.IndexOf(':', position);
            if (separator < position || !int.TryParse(payload.Substring(position, separator - position), NumberStyles.None,
                CultureInfo.InvariantCulture, out var length) || length < 0 || length > payload.Length - separator - 1)
                throw new ArgumentException("Invalid command field.");
            position = separator + 1;
            var value = payload.Substring(position, length); position += length; return value;
        }
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
    // Simulation supplies the verifier; storage depends only on this Core port.
    public interface IRestoreStateValidator
    {
        void Validate(GameState state, ContentCatalog content);
    }
    public enum LoadStatus { Valid, Missing, Corrupt, Unreadable, FutureVersion, UnsupportedContent, RecoveredBackup, RecoveryRequired }
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
    public interface ISaveCompatibilityStore : ISaveStore
    {
        // Compare the entire expected checkpoint and preserve its revision. Only historical child IDs may change.
        WriteStatus CommitReceiptCompatibility(byte[] expectedCheckpoint, byte[] normalizedCheckpoint);
    }
    public static class BatchReceiptIdentity
    {
        public static readonly string InternalPrefix = new string('$', 257) + "batch/";
        public const string BackupRepairReason = "save.receipt_compatibility_pending";
        public static bool IsLegacyReplacement(CommandReceipt previous, string nextId)
        {
            if (previous.ParentPayload.Length == 0 || previous.CommandId.StartsWith(InternalPrefix, StringComparison.Ordinal) ||
                !nextId.StartsWith(InternalPrefix, StringComparison.Ordinal)) return false;
            var colon = nextId.IndexOf(':', InternalPrefix.Length);
            if (colon < 0 || !int.TryParse(nextId.Substring(InternalPrefix.Length, colon - InternalPrefix.Length),
                System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var length) ||
                length < 1 || length > 256 || colon + 1 + length >= nextId.Length || nextId[colon + 1 + length] != '/') return false;
            var owner = nextId.Substring(colon + 1, length);
            var suffix = nextId.Substring(colon + 2 + length);
            if (string.IsNullOrWhiteSpace(owner) || !int.TryParse(suffix, System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var index) || index < 0 ||
                suffix != index.ToString(System.Globalization.CultureInfo.InvariantCulture)) return false;
            var encodedRoot = length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + owner + "/" + suffix;
            return nextId == InternalPrefix + encodedRoot &&
                (previous.CommandId == owner + "/" + suffix || previous.CommandId == "$batch/" + encodedRoot);
        }
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
