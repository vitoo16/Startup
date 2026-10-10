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
    public sealed class VersionedReceiptReplay : IVersionedReceiptReplay, IVersionedV3ReceiptReplay
    {
        private readonly BusinessEconomicCatalog? economicRules;
        public VersionedReceiptReplay() { }
        public VersionedReceiptReplay(BusinessEconomicCatalog economicRules)
        {
            this.economicRules = economicRules ?? throw new ArgumentNullException(nameof(economicRules));
        }

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
                (anchor.OriginalSourceSchema < 0 || anchor.OriginalSourceSchema > SaveSchema.HistoricalV2) ||
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


        // One reproducible v3 suffix dispatcher, following exact frozen-v2 replay admission.
        public void ValidateV3(EconomicV3Payload target, IHistoricalEconomicRulesResolver archives)
        {
            if (economicRules == null) throw new NotSupportedException("V3 evaluator not registered.");
            if (target == null || target.Current == null || target.Activation == null ||
                target.EconomicRecords == null) throw new ArgumentException("Missing v3 replay inputs.");
            var anchor = target.Activation.ToAnchor();
            var original = Read<GameState>(Convert.FromBase64String(target.OriginalV2PayloadBase64));
            if (!archives.TryResolve(anchor.ArchivedRulesetId, anchor.OriginalContentVersion,
                    out var content) || content == null)
                throw new ContentCompatibilityException("save.content_version","Frozen v2 content unavailable.");
            var state = Read<GameState>(Write(original));
            state.SaveVersion = 3;
            Validate(original, state, anchor, archives);
            var financial = EconomicV3Records.Empty();
            if (state.Businesses.Count > 0)
                V3BusinessPolicyEngine.InitializeFromMigration(state, financial);
            var candidate = new EconomicV3Payload
            {
                Current = state, Activation = target.Activation, EconomicRecords = financial
            };
            var evaluator = new V3EconomicReceiptProcessor(content, economicRules, anchor);
            if (anchor.LegacyReceiptCount < 0 || anchor.LegacyReceiptCount > target.Current.Receipts.Count)
                throw new ArgumentException("Invalid historical receipt frontier.");
            for (var index = checked((int)anchor.LegacyReceiptCount);
                 index < target.Current.Receipts.Count; index++)
            {
                var receipt = target.Current.Receipts[index];
                if (receipt == null || receipt.Revision != index + 1L)
                    throw new ArgumentException("Missing/reordered v3 receipt.");
                var operation = state.NewOperation();
                if (operation != receipt.OperationId)
                    throw new ArgumentException("V3 receipt operation identity differs from replay.");
                var command = GameCommand.ParseCanonicalPayload(receipt.Payload);
                var duration = evaluator.Evaluate(candidate,command,operation);
                if (duration != receipt.MinutesConsumed || state.CurrentCue != receipt.Cue)
                    throw new ArgumentException("V3 suffix duration or cue differs from committed rules.");
                state.Revision = checked(state.Revision + 1);
                if (command.Kind != CommandKind.AdvanceBoundary &&
                    command.Kind != CommandKind.AcknowledgePlayback)
                {
                    state.CurrentActivity = operation;
                    state.PlaybackCursor = 0;
                }
                state.Receipts.Add(new CommandReceipt
                {
                    CommandId = receipt.CommandId, Payload = command.CanonicalPayload,
                    OperationId = operation, Revision = state.Revision, Cue = state.CurrentCue,
                    MinutesConsumed = duration, AdvanceTargetIso = receipt.AdvanceTargetIso,
                    ParentPayload = receipt.ParentPayload
                });
            }
            financial.Validate();
            if (!Write(state).SequenceEqual(Write(target.Current)) ||
                !Write(financial).SequenceEqual(Write(target.EconomicRecords)))
                throw new ArgumentException("Saved v3 cash, time, history or economic graph differs from replay.");
        }

        private static byte[] Write<T>(T source)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T))
                    .WriteObject(stream,source);
                return stream.ToArray();
            }
        }

        private static T Read<T>(byte[] bytes)
        {
            using (var stream = new System.IO.MemoryStream(bytes))
                return (T)new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T))
                    .ReadObject(stream)!;
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
