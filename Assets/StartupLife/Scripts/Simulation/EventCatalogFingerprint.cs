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
    // Hash the complete authored selection/effect policy, not the localization
    // text or Unity asset location. Stable ordering prevents harmless import
    // reorder from changing save compatibility, while choice order is retained
    // because presentation order is player-visible.
    public static class EventCatalogFingerprint
    {
        public const string Prefix = "sha256/";
        public const int HexLength = 64;
        private const string Domain = "startup-life/event-catalog/1";

        public static string Compute(IReadOnlyList<GameEventDefinition> definitions,
            IReadOnlyList<EventCategoryPolicy> policies)
        {
            if (definitions == null || policies == null ||
                definitions.Count == 0 || policies.Count == 0 ||
                definitions.Any(d => d == null) || policies.Any(p => p == null))
                throw new ArgumentException("A complete authored event catalog is required.");

            var builder = new StringBuilder(2048);
            void Field(string value)
            {
                if (value == null) throw new ArgumentException("Null event catalog field.");
                builder.Append(value.Length.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append(value);
            }
            void Number(long value) => Field(value.ToString(CultureInfo.InvariantCulture));
            void Tags(IEnumerable<string> values)
            {
                var ordered = values.OrderBy(x => x, StringComparer.Ordinal).ToArray();
                Number(ordered.Length);
                foreach (var value in ordered) Field(value);
            }

            Field(Domain);
            var sortedPolicies = policies.OrderBy(p => p.Id, StringComparer.Ordinal).ToArray();
            Number(sortedPolicies.Length);
            foreach (var policy in sortedPolicies)
            {
                Field(policy.Id);
                Number(policy.CooldownDays);
                Number(policy.WindowDays);
                Number(policy.MaxEventsPerWindow);
            }
            var sortedEvents = definitions.OrderBy(e => e.Id, StringComparer.Ordinal).ToArray();
            Number(sortedEvents.Length);
            foreach (var entry in sortedEvents)
            {
                Field(entry.Id); Field(entry.CategoryId);
                Number(entry.MinimumYear); Number(entry.MinimumStage);
                Number(entry.Weight); Number(entry.CooldownDays);
                Number(entry.MaxOccurrences); Number(entry.WindowPeriodDays);
                Number(entry.WindowOffsetDays);
                Tags(entry.RequiredTags); Tags(entry.ForbiddenTags);
                Field(entry.TitleKey); Field(entry.BodyKey);
                Number(entry.Choices.Count);
                foreach (var choice in entry.Choices)
                {
                    Field(choice.Id); Field(choice.LabelKey); Field(choice.OutcomeId);
                    Tags(choice.GrantedTags);
                    Number(choice.Reward == null ? 0 : 1);
                    if (choice.Reward != null)
                    {
                        Number(choice.Reward.CashDeltaVnd);
                        Number(choice.Reward.CareerXpDelta);
                    }
                }
            }
            using (var hash = SHA256.Create())
                return Prefix + BitConverter.ToString(hash.ComputeHash(
                    Encoding.UTF8.GetBytes(builder.ToString())))
                    .Replace("-", "").ToLowerInvariant();
        }

        public static bool IsWellFormed(string value) =>
            value != null && value.Length == Prefix.Length + HexLength &&
            value.StartsWith(Prefix, StringComparison.Ordinal) &&
            value.Skip(Prefix.Length).All(c =>
                c >= '0' && c <= '9' || c >= 'a' && c <= 'f');
    }
}
