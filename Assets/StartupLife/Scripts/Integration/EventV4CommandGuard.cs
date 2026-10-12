#nullable enable
using System;
using System.Linq;
using StartupLife.Core;
using StartupLife.Simulation;

namespace StartupLife.Integration
{
    // Shared guards for the V4 gateway and day/month coordinator. Command
    // identities always come from verified V3 receipt + V4 journal frontiers.
    internal static class EventV4CommandGuard
    {
        internal static bool Valid(LoadResult read) =>
            (read.Status == LoadStatus.Valid || read.Status == LoadStatus.RecoveredBackup) &&
            read.State != null && read.State.SaveVersion == 4 && read.State.EventV4?.Journal != null;

        // Event IDs share a command namespace with both V3 receipts and durable
        // V4 day/month roots. Reject collisions before planning or consuming RNG;
        // the V4 cold validator enforces the same rule on persisted checkpoints.
        internal static bool HasCheckedDay(EventJournalState journal, string dateIso) =>
            journal.Tickets.Any(ticket => ticket.OpenedDateIso == dateIso) ||
            journal.NoEligibleDays.Any(day => day.DateIso == dateIso);

        internal static bool ConflictsWithCoreOrBatch(GameState state, string commandId) =>
            commandId.StartsWith(EventAdvanceBatchHistory.ReservedChildPrefix,
                StringComparison.Ordinal) ||
            commandId.StartsWith(EventRewardSettlementPlan.ReservedReceiptPrefix,
                StringComparison.Ordinal) ||
            state.Receipts.Any(x => x.CommandId == commandId) ||
            state.EventV4!.Journal!.AdvanceBatches.Any(x => x.RootCommandId == commandId);

        internal static bool ValidIdentity(string runId, string commandId, GameState state) =>
            runId == state.RunId && !string.IsNullOrWhiteSpace(commandId) &&
            commandId.Length <= 256;

    }
}
