#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // Scoped exclusively to one V4 cold-read. Authored amounts come from the
    // catalog, not the untrusted V3 payload. Each V3 reward receipt is verified
    // against the one resolved ticket in the same V4 envelope.
    public sealed class EventRewardReplayProof : IVerifiedEventRewardReplay
    {
        private readonly EventJournalState journal;
        private readonly IReadOnlyDictionary<string, GameEventDefinition> definitions;
        private readonly HashSet<string> applied = new HashSet<string>(StringComparer.Ordinal);

        public EventRewardReplayProof(EventJournalState journal,
            IReadOnlyList<GameEventDefinition> definitions)
        {
            this.journal = journal ?? throw new ArgumentNullException(nameof(journal));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            this.definitions = definitions.ToDictionary(x => x.Id, StringComparer.Ordinal);
        }

        public void Apply(EconomicV3Payload candidate, CommandReceipt receipt,
            string operationId, ContentCatalog content)
        {
            var core = candidate?.Current;
            if (core == null || candidate!.EconomicRecords == null ||
                receipt == null || content == null ||
                core.SaveVersion != 3 || core.RunId != journal.RunId ||
                core.Revision != core.Receipts.Count ||
                receipt.Revision != checked(core.Revision + 1L) ||
                receipt.OperationId != operationId ||
                receipt.MinutesConsumed != 0 || receipt.Cue != "event.resolved" ||
                receipt.AdvanceTargetIso.Length != 0 || receipt.ParentPayload.Length != 0)
                throw new ArgumentException("Invalid V3 financial reward receipt frontier.");

            var command = GameCommand.ParseCanonicalPayload(receipt.Payload);
            if (command.Kind != CommandKind.SettleEventReward || command.Amount != 0 ||
                !journal.Tickets.Any(x => x.Id == command.ContentId))
                throw new ArgumentException("Unknown financial event receipt.");
            var ticket = journal.Tickets.Single(x => x.Id == command.ContentId);
            if (ticket.ResolutionCommandId.Length == 0 ||
                ticket.ChoiceId != command.Name || ticket.EventId != command.AppearanceId ||
                ticket.OpenedDateIso != core.DateIso ||
                ticket.CoreRevisionAtOffer < 0 ||
                ticket.CoreRevisionAtOffer > core.Revision ||
                !definitions.TryGetValue(ticket.EventId, out var definition))
                throw new ArgumentException("Financial receipt is not anchored to a resolved ticket.");
            var choice = definition.Choices.SingleOrDefault(x =>
                x.Id == ticket.ChoiceId && x.OutcomeId == ticket.OutcomeId);
            if (choice?.Reward == null ||
                !journal.OutcomeReceipts.Any(x => x.TicketId == ticket.Id &&
                    x.OutcomeId == ticket.OutcomeId &&
                    x.ResolutionCommandId == ticket.ResolutionCommandId) ||
                !applied.Add(ticket.Id))
                throw new ArgumentException("Missing or duplicate V4 financial outcome.");

            // A blocking pending event allows only playback acknowledgement
            // between its offered V3 frontier and its native reward receipt.
            if (core.Receipts.Skip(checked((int)ticket.CoreRevisionAtOffer))
                .Any(x => GameCommand.ParseCanonicalPayload(x.Payload).Kind !=
                    CommandKind.AcknowledgePlayback))
                throw new ArgumentException("Event reward crossed an unrelated gameplay receipt.");

            var plan = new EventRewardSettlementPlan(core, ticket, choice,
                ticket.ResolutionCommandId, checked(core.Revision + 1L), 0);
            if (receipt.CommandId != plan.CoreReceiptCommandId ||
                !plan.MatchesCoreCommand(command))
                throw new ArgumentException("Financial receipt does not match V4 choice provenance.");
            new SimulationEngine(content, candidate.EconomicRecords)
                .ApplyPlannedEventReward(core, plan, operationId);
        }

        public void AssertComplete()
        {
            var expected = journal.Tickets.Count(ticket =>
                ticket.ResolutionCommandId.Length > 0 &&
                definitions[ticket.EventId].Choices.Single(x => x.Id == ticket.ChoiceId)
                    .Reward != null);
            if (applied.Count != expected)
                throw new ArgumentException("V4 financial outcomes lack verified V3 reward receipts.");
        }
    }
}
