#nullable enable
using System;
using StartupLife.Core;

namespace StartupLife.Integration
{
    // Adapts the verified V4 transaction gateway to the existing presentation
    // command port. No second GameSession, serializer, or physical save writer.
    // The Unity bootstrap must explicitly opt in only after the pending-choice
    // screen is wired; legacy V3 runs continue using GameSession.
    public sealed class EventV4GameCommands : IGameCommands
    {
        private readonly EventV4CommitGateway gateway;
        private readonly ContentCatalog content;

        public EventV4GameCommands(EventV4CommitGateway gateway, ContentCatalog content)
        {
            this.gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            this.content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public CommandResult Execute(CommandEnvelope command) =>
            gateway.ExecuteGameplay(command);

        public AdvanceResult AdvanceDay(CommandEnvelope request) =>
            gateway.AdvanceDay(request);

        public AdvanceResult AdvanceMonth(CommandEnvelope request) =>
            gateway.AdvanceMonth(request);

        public GameSnapshot Snapshot()
        {
            var read = gateway.Read();
            if (read.Status != LoadStatus.Valid || read.State?.SaveVersion != 4 ||
                read.State.EventV4?.Journal == null)
                throw new InvalidOperationException(
                    "A validated V4 checkpoint is required for gameplay projection.");
            return new GameSnapshot(read.State, content);
        }
    }
}
