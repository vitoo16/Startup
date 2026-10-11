#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // Receipt-backed V4 batch provenance. No alternative scheduler or economic
    // evaluator: every child is an ordinary V3 GameSession AdvanceBoundary receipt.
    public static class EventAdvanceBatchHistory
    {
        public const string ReservedChildPrefix = "v4batch/";
        private static readonly string BoundaryPayload =
            new GameCommand(CommandKind.AdvanceBoundary).CanonicalPayload;

        public static string Target(string openedDateIso, string mode)
        {
            var day = DateTime.ParseExact(openedDateIso, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None);
            if (mode == "day") return day.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (mode == "month") return new DateTime(day.Year, day.Month, 1)
                .AddMonths(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            throw new ArgumentException("Invalid advance batch mode.");
        }

        // Fixed-length internal namespace; external gameplay commands may never
        // use it. The root + run identity are included in the SHA256 preimage.
        public static string ChildPrefix(string runId, string rootCommandId)
        {
            if (string.IsNullOrWhiteSpace(runId) || string.IsNullOrWhiteSpace(rootCommandId))
                throw new ArgumentException("Missing batch identity.");
            using (var hash = SHA256.Create())
                return ReservedChildPrefix + BitConverter.ToString(hash.ComputeHash(
                    Encoding.UTF8.GetBytes(runId + "\n" + rootCommandId)))
                    .Replace("-", "").ToLowerInvariant() + "/";
        }

        public static string ChildId(string runId, string rootCommandId, int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            return ChildPrefix(runId, rootCommandId) +
                index.ToString(CultureInfo.InvariantCulture);
        }

        // Builds the authoritative count from the independently verified V3
        // command roster, never from untrusted persisted "next index" counters.
        public static IReadOnlyDictionary<string, int> Validate(EventJournalState journal,
            GameState core)
        {
            if (journal == null || core == null || core.EconomicV3?.Activation == null ||
                core.RunId != journal.RunId)
                throw new ArgumentException("Verified V3 core and V4 journal required.");
            var batches = journal.AdvanceBatches;
            if (batches == null) throw new ArgumentException("Batch roster missing.");
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            var prefixes = new Dictionary<string, EventAdvanceBatchRecord>(StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var previousTarget = "";
            var previousStart = -1L;
            foreach (var batch in batches)
            {
                if (batch == null || string.IsNullOrWhiteSpace(batch.RootCommandId) ||
                    batch.RootCommandId.Length > 256 ||
                    batch.RootCommandId.StartsWith(ReservedChildPrefix, StringComparison.Ordinal) ||
                    !seen.Add(batch.RootCommandId) ||
                    batch.Mode != "day" && batch.Mode != "month" ||
                    batch.CoreRevisionAtStart < core.EconomicV3.Activation.LegacyReceiptCount ||
                    batch.CoreRevisionAtStart > core.Revision ||
                    string.IsNullOrEmpty(batch.OpenedDateIso) ||
                    batch.TargetDateIso != Target(batch.OpenedDateIso, batch.Mode) ||
                    batch.OpenedDateIso.CompareTo(core.DateIso) > 0 ||
                    previousTarget.Length > 0 &&
                        (batch.OpenedDateIso.CompareTo(previousTarget) < 0 ||
                         batch.CoreRevisionAtStart <= previousStart))
                    throw new ArgumentException("Invalid advance batch anchor, date, or root.");
                if (core.Receipts.Any(x => x.CommandId == batch.RootCommandId) ||
                    journal.Tickets.Any(x => x.OfferCommandId == batch.RootCommandId ||
                        x.ResolutionCommandId == batch.RootCommandId))
                    throw new ArgumentException("Batch root collides with a core/event command.");
                var prefix = ChildPrefix(core.RunId, batch.RootCommandId);
                if (prefixes.ContainsKey(prefix))
                    throw new ArgumentException("Batch root digest collision.");
                prefixes.Add(prefix, batch);
                previousTarget = batch.TargetDateIso;
                previousStart = batch.CoreRevisionAtStart;
                result.Add(batch.RootCommandId, 0);
            }

            foreach (var receipt in core.Receipts)
            {
                if (!receipt.CommandId.StartsWith(ReservedChildPrefix, StringComparison.Ordinal))
                    continue;
                var slash = receipt.CommandId.LastIndexOf('/');
                if (slash <= 0 || !prefixes.TryGetValue(
                        receipt.CommandId.Substring(0, slash + 1), out var batch))
                    throw new ArgumentException("Orphaned or counterfeit V4 batch child receipt.");
                var count = result[batch.RootCommandId];
                if (receipt.CommandId != ChildId(core.RunId, batch.RootCommandId, count) ||
                    receipt.Revision <= batch.CoreRevisionAtStart ||
                    receipt.Payload != BoundaryPayload ||
                    receipt.ParentPayload.Length != 0 ||
                    receipt.AdvanceTargetIso.Length != 0)
                    throw new ArgumentException("Noncanonical V4 batch child provenance.");
                result[batch.RootCommandId] = checked(count + 1);
            }
            return result;
        }
    }
}
