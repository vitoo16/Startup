#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Application;
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
        private readonly BusinessEconomicCatalog? economics;

        public EventV4GameCommands(EventV4CommitGateway gateway, ContentCatalog content,
            BusinessEconomicCatalog? economics = null)
        {
            this.gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            this.content = content ?? throw new ArgumentNullException(nameof(content));
            this.economics = economics;
        }

        public CommandResult Execute(CommandEnvelope command) =>
            gateway.ExecuteGameplay(command);

        public AdvanceResult AdvanceDay(CommandEnvelope request) =>
            gateway.AdvanceDay(ResumeInterruptedBatch(request, "day"));

        public AdvanceResult AdvanceMonth(CommandEnvelope request) =>
            gateway.AdvanceMonth(ResumeInterruptedBatch(request, "month"));

        // Presentation produces a new command ID for each tap. If a prior V4
        // batch stopped for a player choice or an app restart, reattach to its
        // durable root instead of registering a second AdvanceDay/Month. Direct
        // gateway callers still retain strict original-command idempotency.
        private CommandEnvelope ResumeInterruptedBatch(CommandEnvelope request, string mode)
        {
            if (request == null || request.Command == null ||
                request.Command.Kind != CommandKind.AdvanceBoundary)
                return request;
            var current = Verified();
            var active = current.EventV4!.Journal!.AdvanceBatches.FirstOrDefault(x =>
                string.CompareOrdinal(current.DateIso, x.TargetDateIso) < 0);
            if (active == null || active.Mode != mode || current.RunId != request.RunId)
                return request;
            return new CommandEnvelope(current.RunId, active.RootCommandId,
                current.Revision, request.Command);
        }

        public GameSnapshot Snapshot() => new GameSnapshot(Verified(), content);

        // Reuse the same immutable read projections as GameSession, always from
        // a fresh, independently validated V4 checkpoint. No UI-side forecast,
        // wallet mutation or shadow economic model.
        public IReadOnlyList<BusinessFinanceDaySnapshot> ReadCommittedFinance() =>
            BusinessFinanceProjection.FromCommittedState(Verified());

        public BusinessPortfolioSnapshot ReadBusinesses() =>
            BusinessOperationsProjection.ProjectPortfolio(Verified(), content, economics);

        public BusinessOperationsSnapshot ReadBusinessOperations() =>
            BusinessOperationsProjection.FromCommittedState(Verified(), content, economics);

        public LifeDashboardSnapshot ReadLifeDashboard() =>
            LifeDashboardProjection.FromCommittedState(Verified(), content);

        private GameState Verified()
        {
            var read = gateway.Read();
            if (read.Status != LoadStatus.Valid || read.State?.SaveVersion != 4 ||
                read.State.EventV4?.Journal == null)
                throw new InvalidOperationException(
                    "A validated V4 checkpoint is required for gameplay projection.");
            return read.State;
        }
    }
}
