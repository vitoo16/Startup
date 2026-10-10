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
        public int FromVersion => SaveSchema.HistoricalV2;
        public int ToVersion => 3;

        public byte[] Migrate(byte[] originalV2Payload)
        {
            if (originalV2Payload == null || originalV2Payload.Length == 0)
                throw new ArgumentException("Empty historical v2 payload.");
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
                SaveSchema.HistoricalV2, source.DateIso, source.Minute, ArchivedRuleset);
            // No money/time/ledger/history/operation IDs are created by migration.
            source.SaveVersion = 3;
            var target = new EconomicV3Payload
            {
                Current = source,
                Activation = EconomicV3Activation.FromAnchor(anchor),
                EconomicRecords = EconomicV3Records.Empty(),
                OriginalV2PayloadBase64 = Convert.ToBase64String(originalV2Payload),
                OriginalV2PayloadSha256 = Digest(originalV2Payload),
                OriginalV2ContentVersion = anchor.OriginalContentVersion,
                OriginalV2RulesetId = anchor.ArchivedRulesetId
            };
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
                target.OriginalV2RulesetId != V2ToV3Migration.ArchivedRuleset)
                throw new ArgumentException("Incomplete/unsupported v3 migration envelope.");

            var originalBytes = Convert.FromBase64String(target.OriginalV2PayloadBase64);
            if (V2ToV3Migration.HashV2Payload(originalBytes) != target.OriginalV2PayloadSha256)
                throw new ArgumentException("Original v2 checkpoint bytes differ from immutable hash.");
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
                anchor.OriginalSourceSchema != SaveSchema.HistoricalV2 ||
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
