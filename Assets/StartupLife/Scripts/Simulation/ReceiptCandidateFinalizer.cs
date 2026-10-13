#nullable enable
using System;
using StartupLife.Core;
namespace StartupLife.Simulation
{
    // Single canonical receipt append/finalize path for GameSession, V3 cold
    // replay and staged V4 rewards. This never evaluates gameplay or writes a
    // checkpoint; every caller still requires full versioned replay and CAS.
    public static class ReceiptCandidateFinalizer
    {
        public static CommandReceipt Append(GameState candidate, GameCommand command,
            string commandId, string operationId, int minutesConsumed,
            V3EconomicReceiptProcessor? v3Processor = null,
            string advanceTargetIso = "", string parentPayload = "",
            EconomicV3Payload? economicReplayCandidate = null)
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
            if (v3Processor != null)
            {
                // Live candidates attach the V3 payload to GameState. During
                // cold replay the authoritative payload is constructed
                // separately: never assume this state owns that attachment.
                var economic = economicReplayCandidate ?? candidate.EconomicV3;
                if (economic?.Current != candidate)
                    throw new ArgumentException("V3 receipt finalization needs its exact replay candidate.");
                v3Processor.FinalizeReceipt(economic, receipt);
            }
            return receipt;
        }
    }
}
