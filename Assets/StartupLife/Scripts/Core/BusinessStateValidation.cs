#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace StartupLife.Core
{
    public static class BusinessStateValidation
    {
        private static readonly string[] Actions =
        {
            "business.launched", "business.reinvested", "business.pricing_changed", "business.closed"
        };

        public static void ValidateStructure(GameState state)
        {
            if (state.Businesses == null) throw new ArgumentException("Businesses collection is required in save v2.");
            if (state.Businesses.Any(x => x == null)) throw new ArgumentException("Business collection contains null.");
            var businesses = state.Businesses;
            if (businesses.Select(x => x.InstanceId).Distinct(StringComparer.Ordinal).Count() != businesses.Count)
                throw new ArgumentException("Duplicate business identity.");
            var priorSequence = 0L;
            foreach (var business in businesses)
            {
                if (!TryParseRunSequence(state.RunId, "business", business.InstanceId, out var sequence) || sequence <= priorSequence)
                    throw new ArgumentException("Businesses are not in global creation order.");
                priorSequence = sequence;
                if (!ContentId.IsValid(business.DefinitionId) || string.IsNullOrWhiteSpace(business.DefinitionRevision) ||
                    !Enum.IsDefined(typeof(PricingPosture), business.PricingPosture) ||
                    business.InitialInvestment <= 0 || business.ReinvestedAmount < 0 ||
                    business.OpenedMinute < 0 || business.OpenedMinute >= 1440)
                    throw new ArgumentException("Invalid business state.");
                _ = checked(business.InitialInvestment + business.ReinvestedAmount);
                var opened = ParseInstant(business.OpenedIso, business.OpenedMinute);
                var hasClosedDate = business.ClosedIso != null;
                var hasClosedMinute = business.ClosedMinute.HasValue;
                if (hasClosedDate != hasClosedMinute) throw new ArgumentException("Partial business closure.");
                if (hasClosedDate)
                {
                    if (business.ClosedMinute!.Value < 0 || business.ClosedMinute.Value >= 1440)
                        throw new ArgumentException("Invalid business closure minute.");
                    var closedInstant = ParseInstant(business.ClosedIso!, business.ClosedMinute.Value);
                    if (Compare(opened, closedInstant) > 0 || Compare(closedInstant, new SimInstant(state.Date, state.Minute)) > 0)
                        throw new ArgumentException("Invalid business closure chronology.");
                }
                else if (Compare(opened, new SimInstant(state.Date, state.Minute)) > 0)
                    throw new ArgumentException("Business opening is ahead of the simulation cursor.");
            }
            if (businesses.Count(x => x.IsActive) > 4) throw new ArgumentException("Too many active businesses.");

            var records = new List<BusinessHistoryRecord>();
            foreach (var entry in state.History)
            {
                if (!Actions.Any(action => entry.StartsWith(action + ":", StringComparison.Ordinal))) continue;
                if (!BusinessHistory.TryParse(entry, out var record) || record == null)
                    throw new ArgumentException("Malformed business history.");
                records.Add(record);
            }
            var byOperation = records.GroupBy(x => x.OperationId, StringComparer.Ordinal)
                .ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.Ordinal);
            var launched = new HashSet<string>(StringComparer.Ordinal);
            var closed = new HashSet<string>(StringComparer.Ordinal);
            var reinvested = businesses.ToDictionary(x => x.InstanceId, _ => 0L, StringComparer.Ordinal);
            var pricing = new Dictionary<string, PricingPosture>(StringComparer.Ordinal);
            var businessReceiptOperations = new HashSet<string>(StringComparer.Ordinal);

            foreach (var receipt in state.Receipts)
            {
                var command = GameCommand.ParseCanonicalPayload(receipt.Payload);
                if ((int)command.Kind < (int)CommandKind.LaunchBusiness || (int)command.Kind > (int)CommandKind.CloseBusiness) continue;
                if (receipt.MinutesConsumed != 0) throw new ArgumentException("Business command advanced time.");
                if (!businessReceiptOperations.Add(receipt.OperationId) ||
                    !byOperation.TryGetValue(receipt.OperationId, out var operationRecords) || operationRecords.Length != 1)
                    throw new ArgumentException("Business receipt/history provenance mismatch.");
                var record = operationRecords[0];
                if (receipt.Cue != record.Action) throw new ArgumentException("Business cue/history mismatch.");

                switch (command.Kind)
                {
                    case CommandKind.LaunchBusiness:
                    {
                        if (record.Action != "business.launched" || command.Name.Length != 0 || string.IsNullOrEmpty(command.AppearanceId) ||
                            string.IsNullOrEmpty(command.ContentId) || record.Amount != command.Amount)
                            throw new ArgumentException("Invalid launch provenance.");
                        var ledgers = state.Ledger.Where(x => x.OperationId == receipt.OperationId).ToArray();
                        if (ledgers.Length != 1 || ledgers[0].Category != "business.launch" ||
                            ledgers[0].Amount != command.Amount || ledgers[0].CashDelta != -checked((long)command.Amount) ||
                            ledgers[0].AttributionId != record.InstanceId)
                            throw new ArgumentException("Invalid business launch ledger provenance.");
                        var business = businesses.SingleOrDefault(x => x.InstanceId == record.InstanceId);
                        if (business == null || !launched.Add(business.InstanceId) || closed.Contains(business.InstanceId) ||
                            business.DefinitionId != command.ContentId || business.DefinitionRevision != command.AppearanceId ||
                            business.InitialInvestment != command.Amount || business.OpenedIso != record.DateIso ||
                            business.OpenedMinute != record.Minute)
                            throw new ArgumentException("Invalid business launch state provenance.");
                        pricing[business.InstanceId] = record.Pricing;
                        break;
                    }
                    case CommandKind.ReinvestBusiness:
                    {
                        var business = businesses.SingleOrDefault(x => x.InstanceId == command.ContentId);
                        if (record.Action != "business.reinvested" || business == null || record.InstanceId != business.InstanceId ||
                            !launched.Contains(business.InstanceId) || closed.Contains(business.InstanceId) ||
                            record.Amount != command.Amount)
                            throw new ArgumentException("Invalid business reinvest provenance.");
                        var ledgers = state.Ledger.Where(x => x.OperationId == receipt.OperationId).ToArray();
                        if (ledgers.Length != 1 || ledgers[0].Category != "business.reinvest" ||
                            ledgers[0].Amount != command.Amount || ledgers[0].CashDelta != -checked((long)command.Amount) ||
                            ledgers[0].AttributionId != business.InstanceId)
                            throw new ArgumentException("Invalid business reinvest ledger provenance.");
                        reinvested[business.InstanceId] = checked(reinvested[business.InstanceId] + command.Amount);
                        break;
                    }
                    case CommandKind.SetBusinessPricing:
                    {
                        var business = businesses.SingleOrDefault(x => x.InstanceId == command.ContentId);
                        if (record.Action != "business.pricing_changed" || business == null || record.InstanceId != business.InstanceId ||
                            !launched.Contains(business.InstanceId) || closed.Contains(business.InstanceId) || record.Amount != 0 ||
                            !Enum.IsDefined(typeof(PricingPosture), command.Amount) || record.Pricing != (PricingPosture)command.Amount ||
                            state.Ledger.Any(x => x.OperationId == receipt.OperationId))
                            throw new ArgumentException("Invalid business pricing provenance.");
                        pricing[business.InstanceId] = record.Pricing;
                        break;
                    }
                    case CommandKind.CloseBusiness:
                    {
                        var business = businesses.SingleOrDefault(x => x.InstanceId == command.ContentId);
                        if (record.Action != "business.closed" || business == null || record.InstanceId != business.InstanceId ||
                            !launched.Contains(business.InstanceId) || !closed.Add(business.InstanceId) || record.Amount != 0 ||
                            state.Ledger.Any(x => x.OperationId == receipt.OperationId) ||
                            business.ClosedIso != record.DateIso || business.ClosedMinute != record.Minute)
                            throw new ArgumentException("Invalid business closure provenance.");
                        break;
                    }
                }
            }

            if (records.Any(x => !businessReceiptOperations.Contains(x.OperationId)))
                throw new ArgumentException("Business history has no receipt.");
            if (state.Ledger.Any(x => x.Category.StartsWith("business.", StringComparison.Ordinal) &&
                !businessReceiptOperations.Contains(x.OperationId)))
                throw new ArgumentException("Business ledger has no receipt.");
            foreach (var business in businesses)
            {
                if (!launched.Contains(business.InstanceId) || reinvested[business.InstanceId] != business.ReinvestedAmount ||
                    !pricing.TryGetValue(business.InstanceId, out var finalPricing) || finalPricing != business.PricingPosture ||
                    business.IsActive == closed.Contains(business.InstanceId))
                    throw new ArgumentException("Business state does not reconstruct from committed operations.");
            }
        }

        public static void ValidateContent(GameState state, ContentCatalog content)
        {
            var activeTypes = new HashSet<BusinessType>();
            foreach (var business in state.Businesses)
            {
                if (!content.Businesses.TryGetValue(business.DefinitionId, out var definition))
                    throw new ContentCompatibilityException("save.content_id", "Business definition is unavailable in the active catalog.");
                if (definition.Revision != business.DefinitionRevision)
                    throw new ContentCompatibilityException("save.content_revision", "Business revision is unavailable in the active catalog.");
                if (business.InitialInvestment < definition.MinimumStartupInvestment ||
                    business.InitialInvestment > definition.MaximumStartupInvestment ||
                    !definition.AllowedPricingPostures.Contains(business.PricingPosture))
                    throw new ArgumentException("Business state conflicts with its definition.");
                if (business.IsActive && !activeTypes.Add(definition.Type))
                    throw new ArgumentException("Multiple active businesses share one business type.");
            }

            foreach (var receipt in state.Receipts)
            {
                var command = GameCommand.ParseCanonicalPayload(receipt.Payload);
                if (command.Kind == CommandKind.LaunchBusiness)
                {
                    if (!content.Businesses.TryGetValue(command.ContentId, out var definition)) continue;
                    if (command.AppearanceId != definition.Revision)
                        throw new ContentCompatibilityException("save.content_revision", "Business launch revision is unavailable.");
                    var record = FindRecord(state, receipt.OperationId);
                    if (record.Pricing != definition.DefaultPricingPosture ||
                        command.Amount < definition.MinimumStartupInvestment || command.Amount > definition.MaximumStartupInvestment)
                        throw new ArgumentException("Business launch conflicts with its definition.");
                }
                else if (command.Kind == CommandKind.ReinvestBusiness)
                {
                    var business = state.Businesses.SingleOrDefault(x => x.InstanceId == command.ContentId);
                    if (business == null || !content.Businesses.TryGetValue(business.DefinitionId, out var definition)) continue;
                    if (business.DefinitionRevision != definition.Revision)
                        throw new ContentCompatibilityException("save.content_revision", "Business revision is unavailable.");
                    if (command.Amount < definition.MinimumReinvestment || command.Amount > definition.MaximumReinvestment)
                        throw new ArgumentException("Business reinvestment conflicts with its definition.");
                }
                else if (command.Kind == CommandKind.SetBusinessPricing)
                {
                    var business = state.Businesses.SingleOrDefault(x => x.InstanceId == command.ContentId);
                    if (business == null || !content.Businesses.TryGetValue(business.DefinitionId, out var definition)) continue;
                    if (business.DefinitionRevision != definition.Revision)
                        throw new ContentCompatibilityException("save.content_revision", "Business revision is unavailable.");
                    if (!Enum.IsDefined(typeof(PricingPosture), command.Amount) ||
                        !definition.AllowedPricingPostures.Contains((PricingPosture)command.Amount))
                        throw new ArgumentException("Business pricing conflicts with its definition.");
                }
            }
        }

        private static BusinessHistoryRecord FindRecord(GameState state, string operationId)
        {
            foreach (var entry in state.History)
                if (BusinessHistory.TryParse(entry, out var record) && record != null && record.OperationId == operationId) return record;
            throw new ArgumentException("Missing business history.");
        }

        private static SimInstant ParseInstant(string dateIso, int minute)
        {
            var date = DateTime.ParseExact(dateIso, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            return new SimInstant(new SimDate(date.Year, date.Month, date.Day), minute);
        }

        private static int Compare(SimInstant left, SimInstant right)
        {
            var date = left.Date.CompareTo(right.Date);
            return date != 0 ? date : left.Minute.CompareTo(right.Minute);
        }

        private static bool TryParseRunSequence(string runId, string kind, string value, out long sequence)
        {
            sequence = 0;
            var prefix = runId + "/" + kind + "/";
            if (!value.StartsWith(prefix, StringComparison.Ordinal)) return false;
            var suffix = value.Substring(prefix.Length);
            return suffix.Length > 0 && long.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out sequence) &&
                sequence > 0 && suffix == sequence.ToString(CultureInfo.InvariantCulture);
        }
    }
}
