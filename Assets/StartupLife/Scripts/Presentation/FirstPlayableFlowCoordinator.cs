#nullable enable
using System;
using StartupLife.Core;

namespace StartupLife.Presentation
{
    /// <summary>
    /// Presentation command seam for the first playable.
    /// Views never own revisions, run IDs, receipts, save bytes, or mutable domain state.
    /// </summary>
    public sealed class FirstPlayableFlowCoordinator
    {
        private readonly IGameCommands commands;
        private readonly Func<string> commandIdFactory;

        public FirstPlayableFlowCoordinator(IGameCommands gameCommands, Func<string>? idFactory = null)
        {
            commands = gameCommands ?? throw new ArgumentNullException(nameof(gameCommands));
            commandIdFactory = idFactory ?? (() => Guid.NewGuid().ToString("N"));
        }

        public GameSnapshot Snapshot() => commands.Snapshot();

        public CommandResult CreateCharacter(string name, int age, string backgroundId, string appearanceId) =>
            Execute(new GameCommand(CommandKind.CreateCharacter, backgroundId, age, name, appearanceId));

        public CommandResult AcceptJob(string careerId) =>
            Execute(new GameCommand(CommandKind.AcceptJob, careerId));

        public CommandResult PurchaseCourse(string courseId) =>
            Execute(new GameCommand(CommandKind.PurchaseCourse, courseId));

        public CommandResult Study(int minutes) =>
            Execute(new GameCommand(CommandKind.Study, amount: minutes));

        public CommandResult AdvanceBoundary() =>
            Execute(new GameCommand(CommandKind.AdvanceBoundary));

        public AdvanceResult AdvanceDay()
        {
            var snapshot = commands.Snapshot();
            return commands.AdvanceDay(new CommandEnvelope(
                snapshot.RunId,
                NextCommandId(),
                snapshot.Revision,
                new GameCommand(CommandKind.AdvanceBoundary)));
        }

        public CommandResult Resign() =>
            Execute(new GameCommand(CommandKind.Resign));

        public CommandResult AcknowledgePlayback(int cursor)
        {
            var snapshot = commands.Snapshot();
            if (string.IsNullOrEmpty(snapshot.CurrentActivityId))
                throw new InvalidOperationException("Playback acknowledgement requires a committed current activity.");
            if (cursor < snapshot.PlaybackCursor)
                throw new ArgumentOutOfRangeException(nameof(cursor), "Playback cursor cannot move backwards.");

            return commands.Execute(new CommandEnvelope(
                snapshot.RunId,
                NextCommandId(),
                snapshot.Revision,
                new GameCommand(CommandKind.AcknowledgePlayback, snapshot.CurrentActivityId, cursor)));
        }

        private CommandResult Execute(GameCommand command)
        {
            var snapshot = commands.Snapshot();
            return commands.Execute(new CommandEnvelope(
                snapshot.RunId,
                NextCommandId(),
                snapshot.Revision,
                command));
        }

        private string NextCommandId()
        {
            var value = commandIdFactory();
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("Presentation command ID factory returned an empty ID.");
            return value;
        }
    }
}
