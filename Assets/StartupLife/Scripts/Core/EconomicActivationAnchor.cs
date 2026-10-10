#nullable enable
using System;
using System.Globalization;

namespace StartupLife.Core
{
    // An immutable, non-receipt v2->v3 migration boundary. The serializer/runtime must
    // persist and validate this inside the v3 authoritative state; constructing it alone
    // does not migrate, mutate, charge, settle, or create any game receipt.
    public sealed class EconomicActivationAnchor
    {
        public long LegacyReceiptCount { get; }
        public string LegacyLastOperationId { get; }
        public string OriginalContentVersion { get; }
        public int OriginalSourceSchema { get; }
        public string MigratedAtDateIso { get; }
        public int MigratedAtMinute { get; }
        public string ActivationDateIso { get; }
        public string ArchivedRulesetId { get; }
        public string CutoverTransformVersion { get; }
        public bool SourceMidnightAlreadyProcessedLegacySettlement { get; }

        public EconomicActivationAnchor(long legacyReceiptCount, string legacyLastOperationId,
            string originalContentVersion, int originalSourceSchema, string migratedAtDateIso,
            int migratedAtMinute, string activationDateIso, string archivedRulesetId,
            string cutoverTransformVersion, bool sourceMidnightAlreadyProcessedLegacySettlement)
        {
            if (legacyReceiptCount < 0 ||
                (legacyReceiptCount == 0 && !string.IsNullOrEmpty(legacyLastOperationId)) ||
                (legacyReceiptCount > 0 && string.IsNullOrWhiteSpace(legacyLastOperationId)) ||
                string.IsNullOrWhiteSpace(originalContentVersion) ||
                originalSourceSchema < 0 || originalSourceSchema > 2 ||
                migratedAtMinute < 0 || migratedAtMinute >= 1440 ||
                string.IsNullOrWhiteSpace(archivedRulesetId) ||
                string.IsNullOrWhiteSpace(cutoverTransformVersion))
                throw new ArgumentException("Invalid historical migration anchor.");
            var sourceDate = ParseDate(migratedAtDateIso);
            var targetDate = ParseDate(activationDateIso);
            if (targetDate != sourceDate.AddDays(migratedAtMinute == 0 ? 0 : 1) ||
                sourceMidnightAlreadyProcessedLegacySettlement != (migratedAtMinute == 0))
                throw new ArgumentException("Economic activation must follow the exact source checkpoint.");
            LegacyReceiptCount = legacyReceiptCount;
            LegacyLastOperationId = legacyLastOperationId;
            OriginalContentVersion = originalContentVersion;
            OriginalSourceSchema = originalSourceSchema;
            MigratedAtDateIso = migratedAtDateIso; MigratedAtMinute = migratedAtMinute;
            ActivationDateIso = activationDateIso; ArchivedRulesetId = archivedRulesetId;
            CutoverTransformVersion = cutoverTransformVersion;
            SourceMidnightAlreadyProcessedLegacySettlement = sourceMidnightAlreadyProcessedLegacySettlement;
        }

        public static EconomicActivationAnchor FromHistoricalCheckpoint(long receiptCount,
            string lastOperationId, string originalContentVersion, int originalSourceSchema,
            string dateIso, int minute, string archivedRulesetId)
        {
            var date = ParseDate(dateIso);
            if (minute < 0 || minute >= 1440)
                throw new ArgumentOutOfRangeException(nameof(minute));
            var activation = (minute == 0 ? date : date.AddDays(1))
                .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return new EconomicActivationAnchor(receiptCount, lastOperationId,
                originalContentVersion, originalSourceSchema, dateIso, minute, activation,
                archivedRulesetId, "m9-t02.cutover-v1", minute == 0);
        }

        public bool IsLegacyReceiptIndex(long zeroBasedReceiptIndex) =>
            zeroBasedReceiptIndex >= 0 && zeroBasedReceiptIndex < LegacyReceiptCount;

        public bool ShouldOpenEconomicOperationsOnDate(string dateIso) =>
            ParseDate(dateIso) >= ParseDate(ActivationDateIso);

        private static DateTime ParseDate(string dateIso) =>
            DateTime.ParseExact(dateIso, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None);
    }
}
