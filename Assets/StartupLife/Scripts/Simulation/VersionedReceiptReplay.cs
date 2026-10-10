#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // Central versioned replay authority. At the migration-only gate this proves the
    // entire v2 prefix against *frozen* source and rejects unaudited v3 suffixes.
    // Must be extended with a tested V3 evaluator BEFORE schema3 becomes live.
    public sealed class VersionedReceiptReplay : IVersionedReceiptReplay
    {
        public void Validate(GameState originalV2, GameState currentV3,
            EconomicActivationAnchor anchor, IHistoricalEconomicRulesResolver archiveResolver)
        {
            if (originalV2 == null || currentV3 == null || anchor == null || archiveResolver == null)
                throw new ArgumentNullException(nameof(originalV2));
            if (originalV2.SaveVersion != SaveSchema.HistoricalV2 ||
                currentV3.SaveVersion != 3 ||
                originalV2.RunId != currentV3.RunId ||
                originalV2.Seed != currentV3.Seed ||
                originalV2.Receipts == null || currentV3.Receipts == null ||
                originalV2.Receipts.Count != anchor.LegacyReceiptCount ||
                originalV2.Revision != originalV2.Receipts.Count ||
                currentV3.Receipts.Count < anchor.LegacyReceiptCount)
                throw new ArgumentException("V3 replay frontier fails historical v2 schema/identity.");
            if (!archiveResolver.TryResolve(anchor.ArchivedRulesetId, anchor.OriginalContentVersion, out var historical) ||
                historical == null)
                throw new ContentCompatibilityException("save.content_version",
                    "Required frozen v2 economic rules/content are not bundled in this build.");

            // This checks historical wallet/arrears/biz pricing, RNG, career scenes,
            // receipt/operation identities and other v2 authoritative fields.
            // Unlike a v3 evaluator it implements immediate historical price changes.
            StateValidation.Validate(originalV2, historical, new ArchivedV2RestoreValidator());

            var last = originalV2.Receipts.Count == 0 ? "" :
                originalV2.Receipts[originalV2.Receipts.Count - 1].OperationId;
            if (anchor.OriginalContentVersion != historical.Version ||
                anchor.OriginalSourceSchema != SaveSchema.HistoricalV2 ||
                anchor.LegacyLastOperationId != last ||
                anchor.MigratedAtDateIso != originalV2.DateIso ||
                anchor.MigratedAtMinute != originalV2.Minute)
                throw new ArgumentException("V2 replay checkpoint and versioned cutover disagree.");

            // Legacy receipts form an exact prefix, not a date-estimated subset.
            for (var i = 0; i < originalV2.Receipts.Count; i++)
                if (!Equals(originalV2.Receipts[i], currentV3.Receipts[i]))
                    throw new ArgumentException("V3 receipt history rewrote committed legacy prefix.");

            // Until a v3 evaluator is registered, do not accept any v3 suffix
            // or pretend to validate v3 money, identity/capacity or obligations.
            if (currentV3.Receipts.Count > originalV2.Receipts.Count)
                throw new NotSupportedException("V3 suffix replay evaluator has not been registered.");

            if (currentV3.Revision != originalV2.Revision ||
                currentV3.DateIso != originalV2.DateIso ||
                currentV3.Minute != originalV2.Minute ||
                currentV3.Cash != originalV2.Cash ||
                currentV3.NextEntity != originalV2.NextEntity ||
                currentV3.NextOperation != originalV2.NextOperation ||
                currentV3.Ledger.Count != originalV2.Ledger.Count ||
                currentV3.Arrears.Count != originalV2.Arrears.Count)
                throw new ArgumentException("Pure cutover changed frozen historical checkpoint state.");
        }

        private static bool Equals(CommandReceipt a, CommandReceipt b)
        {
            if (a == null || b == null) return a == b;
            return a.CommandId == b.CommandId && a.Payload == b.Payload &&
                a.OperationId == b.OperationId && a.Revision == b.Revision &&
                a.Cue == b.Cue && a.MinutesConsumed == b.MinutesConsumed &&
                a.AdvanceTargetIso == b.AdvanceTargetIso && a.ParentPayload == b.ParentPayload;
        }
    }
}
