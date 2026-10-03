#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Presentation
{
    public interface ICommandIdSource
    {
        string Next(string action);
    }

    public sealed class GuidCommandIdSource : ICommandIdSource
    {
        public string Next(string action)
        {
            if (string.IsNullOrWhiteSpace(action)) throw new ArgumentException("Action is required.", nameof(action));
            return action + "/" + Guid.NewGuid().ToString("N");
        }
    }

    public enum FirstPlayableScreen
    {
        CharacterCreation,
        Life
    }

    public sealed class FirstPlayableState
    {
        public FirstPlayableScreen Screen { get; }
        public string Name { get; }
        public SimInstant Instant { get; }
        public long Revision { get; }
        public long Cash { get; }
        public long Arrears { get; }
        public string CareerId { get; }
        public long CareerXp { get; }
        public int Rank { get; }
        public string Cue { get; }
        public string CurrentActivityId { get; }
        public int PlaybackCursor { get; }
        public ActiveCourseSnapshot? ActiveCourse { get; }
        public IReadOnlyDictionary<string, int> SkillLevels { get; }
        public IReadOnlyList<string> History { get; }

        public bool HasCareer => CareerId.Length != 0;
        public bool CanStudy => ActiveCourse != null;
        public bool HasPendingPlayback => CurrentActivityId.Length != 0;

        public FirstPlayableState(GameSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            Screen = snapshot.Name.Length == 0 ? FirstPlayableScreen.CharacterCreation : FirstPlayableScreen.Life;
            Name = snapshot.Name;
            Instant = snapshot.Instant;
            Revision = snapshot.Revision;
            Cash = snapshot.Cash;
            Arrears = snapshot.Arrears;
            CareerId = snapshot.CareerId;
            CareerXp = snapshot.CareerXp;
            Rank = snapshot.Rank;
            Cue = snapshot.Cue;
            CurrentActivityId = snapshot.CurrentActivityId;
            PlaybackCursor = snapshot.PlaybackCursor;
            ActiveCourse = snapshot.ActiveCourse;
            SkillLevels = new ReadOnlyDictionary<string, int>(
                snapshot.SkillLevels.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal));
            History = Array.AsReadOnly(snapshot.History.ToArray());
        }
    }

    public sealed class FirstPlayableActionResult
    {
        public CommandResult Command { get; }
        public FirstPlayableState State { get; }

        public FirstPlayableActionResult(CommandResult command, FirstPlayableState state)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            State = state ?? throw new ArgumentNullException(nameof(state));
        }
    }

    public sealed class FirstPlayableAdvanceResult
    {
        public AdvanceResult Advance { get; }
        public FirstPlayableState State { get; }

        public FirstPlayableAdvanceResult(AdvanceResult advance, FirstPlayableState state)
        {
            Advance = advance ?? throw new ArgumentNullException(nameof(advance));
            State = state ?? throw new ArgumentNullException(nameof(state));
        }
    }

    public sealed class FirstPlayableFlow
    {
        private readonly IGameCommands commands;
        private readonly ICommandIdSource commandIds;

        public FirstPlayableFlow(IGameCommands gameCommands, ICommandIdSource commandIdSource)
        {
            commands = gameCommands ?? throw new ArgumentNullException(nameof(gameCommands));
            commandIds = commandIdSource ?? throw new ArgumentNullException(nameof(commandIdSource));
        }

        public FirstPlayableState Refresh() => Project(commands.Snapshot());

        public FirstPlayableActionResult CreateCharacter(string name, int age, string backgroundId, string appearanceId) =>
            Execute("create-character", new GameCommand(CommandKind.CreateCharacter, backgroundId, age, name, appearanceId));

        public FirstPlayableActionResult AcceptJob(string careerId) =>
            Execute("accept-job", new GameCommand(CommandKind.AcceptJob, careerId));

        public FirstPlayableActionResult AdvanceBoundary() =>
            Execute("advance-boundary", new GameCommand(CommandKind.AdvanceBoundary));

        public FirstPlayableActionResult PurchaseCourse(string courseId) =>
            Execute("purchase-course", new GameCommand(CommandKind.PurchaseCourse, courseId));

        public FirstPlayableActionResult Study(int minutes) =>
            Execute("study", new GameCommand(CommandKind.Study, amount: minutes));

        public FirstPlayableActionResult Resign() =>
            Execute("resign", new GameCommand(CommandKind.Resign));

        public FirstPlayableActionResult AcknowledgePlayback(int cursor)
        {
            var snapshot = commands.Snapshot();
            return Execute(snapshot, "acknowledge-playback",
                new GameCommand(CommandKind.AcknowledgePlayback, snapshot.CurrentActivityId, cursor));
        }

        public FirstPlayableAdvanceResult AdvanceDay() =>
            Advance("advance-day", (gateway, envelope) => gateway.AdvanceDay(envelope));

        public FirstPlayableAdvanceResult AdvanceMonth() =>
            Advance("advance-month", (gateway, envelope) => gateway.AdvanceMonth(envelope));

        private FirstPlayableActionResult Execute(string action, GameCommand command) =>
            Execute(commands.Snapshot(), action, command);

        private FirstPlayableActionResult Execute(GameSnapshot before, string action, GameCommand command)
        {
            var envelope = Envelope(before, action, command);
            var result = commands.Execute(envelope);
            return new FirstPlayableActionResult(result, Project(commands.Snapshot()));
        }

        private FirstPlayableAdvanceResult Advance(string action, Func<IGameCommands, CommandEnvelope, AdvanceResult> dispatch)
        {
            var before = commands.Snapshot();
            var envelope = Envelope(before, action, new GameCommand(CommandKind.AdvanceBoundary));
            var result = dispatch(commands, envelope);
            return new FirstPlayableAdvanceResult(result, Project(commands.Snapshot()));
        }

        private CommandEnvelope Envelope(GameSnapshot snapshot, string action, GameCommand command)
        {
            var commandId = commandIds.Next(action);
            if (string.IsNullOrWhiteSpace(commandId)) throw new InvalidOperationException("Command ID source returned an empty ID.");
            return new CommandEnvelope(snapshot.RunId, commandId, snapshot.Revision, command);
        }

        private static FirstPlayableState Project(GameSnapshot snapshot) => new FirstPlayableState(snapshot);
    }
}
