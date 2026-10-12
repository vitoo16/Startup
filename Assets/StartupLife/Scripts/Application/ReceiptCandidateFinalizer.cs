#nullable enable
using System;
using StartupLife.Core;
using StartupLife.Simulation;

namespace StartupLife.Application
{
    // Shared candidate receipt assembly for both GameSession and staged V4
    // reward intents. No store, CAS, or independent gameplay evaluator here:
    // every caller still needs the authoritative serializer/replay admission.
    public static class ReceiptCandidateFinalizer
    {
        public static CommandReceipt Append(GameState candidate, GameCommand command,
            string commandId, string operationId, int minutesConsumed,
            V3EconomicReceiptProcessor? v3Processor = null,
            string advanceTargetIso = "", string parentPayload = "")
        {
            if (candidate == null || command == null ||
                candidate.Receipts == null || candidate.Revision != candidate.Receipts.Count ||
                string.IsNullOrWhiteSpace(commandId) || string.IsNullOrWhiteSpace(operationId) ||
                minutesConsumed < 0)
                throw new ArgumentException("Receipt requires an isolated canonical candidate frontier.");
            candidate.Revision = checked(candidate.Revision + 1L);
            if (command.Kind != CommandKind.AdvanceBoundary &&
                command.Kind != CommandKind.AcknowledgePlayback)
            {
                candidate.CurrentActivity = operationId;
                candidate.PlaybackCursor = 0;
            }
            var receipt = new CommandReceipt
            {
                CommandId = commandId,
                Payload = command.CanonicalPayload,
                OperationId = operationId,
                Revision = candidate.Revision,
                Cue = candidate.CurrentCue,
                MinutesConsumed = minutesConsumed,
                AdvanceTargetIso = advanceTargetIso,
                ParentPayload = parentPayload
            };
            candidate.Receipts.Add(receipt);
            v3Processor?.FinalizeReceipt(candidate.EconomicV3!, receipt);
            return receipt;
        }
    }
}
