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
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Command == null ||
                request.Command.Kind != CommandKind.AdvanceBoundary)
                return request;
            var read = gateway.Read();
            if (!EventV4CommandGuard.Valid(read))
                return request; // Gateway owns recovery/error classification.
            var current = read.State!;
            // Valid() checks the nested journal at this checkpoint, but the
            // compiler does not propagate nullability through that method.
            // A missing journal must remain a recovery case for the gateway,
            // never a UI-side NullReferenceException.
            var journal = current.EventV4?.Journal;
            if (journal == null)
                return request;
            var active = journal.AdvanceBatches.FirstOrDefault(x =>
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
            if (!EventV4CommandGuard.Valid(read))
                throw new InvalidOperationException(
                    "A validated V4 checkpoint is required for gameplay projection.");
            // A previously recovered backup is verified by exactly the same
            // active schema validator; rejecting it only in read-only UI
            // projections would strand an otherwise recoverable player run.
            return read.State!;
        }
    }
}
