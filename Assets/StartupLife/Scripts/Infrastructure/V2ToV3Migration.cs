#nullable enable
using System;
using System.Linq;
using System.Security.Cryptography;
using StartupLife.Core;

namespace StartupLife.Infrastructure
{
    // Strict adjacent 2->3 payload transform. Not activated by JsonSaveSerializer until
    // v3 intrinsic validation, historical replay, CAS repair and GameSession are wired.
    // The migration is zero-economic-effect and preserves the exact original v2 payload bytes.
    public sealed class V2ToV3Migration : ISaveMigration
    {
        public const string ArchivedRuleset = "first-playable.v1/v2-semantics@051a9314";
        private readonly IHistoricalEconomicRulesResolver archiveResolver;
        private readonly IVersionedReceiptReplay versionedReplay;
        public V2ToV3Migration(IHistoricalEconomicRulesResolver archiveResolver, IVersionedReceiptReplay versionedReplay)
        {
            this.archiveResolver = archiveResolver ?? throw new ArgumentNullException(nameof(archiveResolver));
            this.versionedReplay = versionedReplay ?? throw new ArgumentNullException(nameof(versionedReplay));
        }

        public int FromVersion => SaveSchema.HistoricalV2;
        public int ToVersion => 3;

        public byte[] Migrate(byte[] originalV2Payload) =>
            MigrateFromOriginalSource(originalV2Payload, SaveSchema.HistoricalV2, originalV2Payload);

        // An adjacent migration receives the exact source wire bytes and *original*
        // envelope schema from the serializer. Stage 2 remains a validated v2 DTO.
        public byte[] MigrateFromOriginalSource(byte[] originalV2Payload, int originalSchema,
            byte[] originalSourcePayload)
        {
            if (originalV2Payload == null || originalV2Payload.Length == 0)
                throw new ArgumentException("Empty historical v2 payload.");
            if (originalSchema < 0 || originalSchema > SaveSchema.HistoricalV2 ||
                originalSourcePayload == null || originalSourcePayload.Length == 0)
                throw new ArgumentException("Unsupported or missing original legacy save wire.");
            if (originalSchema == SaveSchema.HistoricalV2)
            {
                if (!originalSourcePayload.SequenceEqual(originalV2Payload))
                    throw new ArgumentException("Direct v2 source checkpoint cannot differ from stage-2 wire.");
            }
            else
            {
                HistoricalV1Codec.ValidatePayload(originalSourcePayload, originalSchema);
                var sourceV1 = originalSchema == 0 ?
                    new SyntheticV0Migration().Migrate(originalSourcePayload) : originalSourcePayload;
                var raisedV2 = new V1ToV2Migration().Migrate(sourceV1);
                if (!raisedV2.SequenceEqual(originalV2Payload))
                    throw new ArgumentException("Source 0/1 bytes do not reproduce the exact staged v2 checkpoint.");
            }
            var source = JsonSaveSerializer.ReadObject<GameState>(originalV2Payload);
            // The serializer's stage-2 restore/replay validator must run BEFORE this function.
            // These guards additionally reject direct misuse and unsupported source content.
            if (source.SaveVersion != SaveSchema.HistoricalV2 ||
                source.ContentVersion != "first-playable.v1" ||
                string.IsNullOrWhiteSpace(source.RunId) ||
                source.Revision < 0 || source.Receipts == null ||
                source.Receipts.Count != source.Revision ||
                source.Minute < 0 || source.Minute >= 1440)
                throw new ArgumentException("Invalid/unrecognized v2 checkpoint.");
            var last = source.Receipts.Count == 0 ? "" : source.Receipts[source.Receipts.Count - 1].OperationId;
            if (source.Receipts.Any(x => x == null) ||
                (source.Receipts.Count > 0 && string.IsNullOrWhiteSpace(last)))
                throw new ArgumentException("Invalid historical receipt frontier.");
            var anchor = EconomicActivationAnchor.FromHistoricalCheckpoint(
                source.Receipts.Count, last, source.ContentVersion,
                originalSchema, source.DateIso, source.Minute, ArchivedRuleset);
            // No money/time/ledger/history/operation IDs are created by migration.
            source.SaveVersion = 3;
            // A pure one-to-one projection of existing business price/closure;
            // this creates no receipt, payment, wallet mutation or retroactive operation.
            var policies = new EconomicV3Records();
            foreach (var owned in source.Businesses.OrderBy(x => x.InstanceId, StringComparer.Ordinal))
                policies.BusinessPolicies.Add(new V3BusinessPolicyRecord
                {
                    InstanceId = owned.InstanceId,
                    Status = owned.IsActive ? EconomicOperationStatusCode.Auto : EconomicOperationStatusCode.Closed,
                    EffectivePricing = owned.PricingPosture
                });
            var target = new EconomicV3Payload
            {
                Current = source,
                Activation = EconomicV3Activation.FromAnchor(anchor),
                EconomicRecords = policies,
                OriginalV2PayloadBase64 = Convert.ToBase64String(originalV2Payload),
                OriginalV2PayloadSha256 = Digest(originalV2Payload),
                OriginalV2ContentVersion = anchor.OriginalContentVersion,
                OriginalV2RulesetId = anchor.ArchivedRulesetId,
                OriginalSourceSchema = originalSchema,
                OriginalSourcePayloadBase64 = Convert.ToBase64String(originalSourcePayload),
                OriginalSourcePayloadSha256 = Digest(originalSourcePayload)
            };
            // The exact archived v2 evaluator and catalog must validate the checkpoint
            // before the migration may produce even an in-memory v3 payload.
            var originalV2 = JsonSaveSerializer.ReadObject<GameState>(originalV2Payload);
            versionedReplay.Validate(originalV2, target.Current!, anchor, archiveResolver);
            return EconomicV3PayloadCodec.SerializeChecked(target);
        }

        private static string Digest(byte[] payload)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(payload)).Replace("-", "").ToLowerInvariant();
        }

        public static string HashV2Payload(byte[] payload) => Digest(payload);
    }

    public static class EconomicV3PayloadCodec
    {
        public static byte[] SerializeChecked(EconomicV3Payload payload)
        {
            Validate(payload);
            return JsonSaveSerializer.WriteObject(payload);
        }

        public static EconomicV3Payload ReadChecked(byte[] payload)
        {
            if (payload == null || payload.Length == 0) throw new ArgumentException("Empty v3 payload.");
            var parsed = JsonSaveSerializer.ReadObject<EconomicV3Payload>(payload);
            Validate(parsed);
            return parsed;
        }

        public static void Validate(EconomicV3Payload target)
        {
            if (target == null || target.WireSchema != 3 ||
                target.Current == null || target.Current.SaveVersion != 3 ||
                target.Activation == null || target.EconomicRecords == null ||
                string.IsNullOrEmpty(target.OriginalV2PayloadBase64) ||
                string.IsNullOrWhiteSpace(target.OriginalV2PayloadSha256) ||
                target.OriginalV2ContentVersion != "first-playable.v1" ||
                target.OriginalV2RulesetId != V2ToV3Migration.ArchivedRuleset ||
                target.OriginalSourceSchema < 0 ||
                target.OriginalSourceSchema > SaveSchema.HistoricalV2 ||
                string.IsNullOrWhiteSpace(target.OriginalSourcePayloadBase64) ||
                string.IsNullOrWhiteSpace(target.OriginalSourcePayloadSha256))
                throw new ArgumentException("Incomplete/unsupported v3 migration envelope.");

            var originalBytes = Convert.FromBase64String(target.OriginalV2PayloadBase64);
            if (V2ToV3Migration.HashV2Payload(originalBytes) != target.OriginalV2PayloadSha256)
                throw new ArgumentException("Original v2 checkpoint bytes differ from immutable hash.");
            var sourceBytes = Convert.FromBase64String(target.OriginalSourcePayloadBase64);
            if (V2ToV3Migration.HashV2Payload(sourceBytes) != target.OriginalSourcePayloadSha256)
                throw new ArgumentException("Original source legacy payload checksum mismatch.");
            if (target.OriginalSourceSchema == SaveSchema.HistoricalV2)
            {
                if (!sourceBytes.SequenceEqual(originalBytes))
                    throw new ArgumentException("Source v2 and stage-2 checkpoint are not identical.");
            }
            else
            {
                HistoricalV1Codec.ValidatePayload(sourceBytes, target.OriginalSourceSchema);
                var stageOne = target.OriginalSourceSchema == 0 ?
                    new SyntheticV0Migration().Migrate(sourceBytes) : sourceBytes;
                if (!new V1ToV2Migration().Migrate(stageOne).SequenceEqual(originalBytes))
                    throw new ArgumentException("Original source v0/v1 could not reproduce stored v2 checkpoint.");
            }
            var v2 = JsonSaveSerializer.ReadObject<GameState>(originalBytes);
            if (v2.SaveVersion != SaveSchema.HistoricalV2 || v2.Receipts == null ||
                v2.Receipts.Count != v2.Revision || v2.ContentVersion != target.OriginalV2ContentVersion ||
                v2.RunId != target.Current.RunId || v2.Seed != target.Current.Seed ||
                v2.Revision < 0 || target.Current.Revision < v2.Revision ||
                target.Current.Receipts == null ||
                target.Current.Receipts.Count < v2.Receipts.Count)
                throw new ArgumentException("V3 checkpoint does not contain its complete v2 prefix.");
            var anchor = target.Activation.ToAnchor();
            var last = v2.Receipts.Count == 0 ? "" : v2.Receipts[v2.Receipts.Count - 1].OperationId;
            if (anchor.LegacyReceiptCount != v2.Receipts.Count ||
                anchor.LegacyLastOperationId != last ||
                anchor.OriginalContentVersion != v2.ContentVersion ||
                anchor.OriginalSourceSchema != target.OriginalSourceSchema ||
                anchor.MigratedAtDateIso != v2.DateIso ||
                anchor.MigratedAtMinute != v2.Minute ||
                anchor.ArchivedRulesetId != target.OriginalV2RulesetId)
                throw new ArgumentException("V3 activation anchor and source checkpoint differ.");
            for (var i = 0; i < v2.Receipts.Count; i++)
            {
                var old = v2.Receipts[i]; var current = target.Current.Receipts[i];
                if (old == null || current == null ||
                    old.CommandId != current.CommandId ||
                    old.Payload != current.Payload ||
                    old.OperationId != current.OperationId ||
                    old.Revision != current.Revision ||
                    old.Cue != current.Cue ||
                    old.MinutesConsumed != current.MinutesConsumed ||
                    old.AdvanceTargetIso != current.AdvanceTargetIso ||
                    old.ParentPayload != current.ParentPayload)
                    throw new ArgumentException("Historical receipt prefix must remain byte-semantically intact.");
            }
            target.EconomicRecords.Validate();
            if (target.Current.Revision == v2.Revision)
            {
                if (target.Current.DateIso != v2.DateIso || target.Current.Minute != v2.Minute ||
                    target.Current.Cash != v2.Cash || target.Current.NextEntity != v2.NextEntity ||
                    target.Current.NextOperation != v2.NextOperation ||
                    target.Current.Ledger.Count != v2.Ledger.Count ||
                    target.Current.Arrears.Count != v2.Arrears.Count ||
                    target.EconomicRecords.CommittedEpochIds.Count != 0 ||
                    target.EconomicRecords.CommittedFulfillmentIds.Count != 0 ||
                    target.EconomicRecords.CommittedObligationIds.Count != 0 ||
                    target.EconomicRecords.CommittedCostReservationIds.Count != 0 ||
                    target.EconomicRecords.CommittedSliceIds.Count != 0 ||
                    target.EconomicRecords.SettledDayIds.Count != 0 ||
                    target.EconomicRecords.V3RecognizedRevenueVnd != 0 ||
                    target.EconomicRecords.V3PaidVariableVnd != 0 ||
                    target.EconomicRecords.V3RecognizedFixedVnd != 0)
                    throw new ArgumentException("Migration-only checkpoint added unauthorized economic effects.");
            }
        }
    }
}
