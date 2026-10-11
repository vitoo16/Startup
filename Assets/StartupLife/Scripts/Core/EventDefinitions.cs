#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StartupLife.Core
{
    // Immutable authored content. Event execution and state persistence belong to later layers.
    public sealed class EventChoiceDefinition
    {
        public string Id { get; }
        public string LabelKey { get; }
        public string OutcomeId { get; }
        // Only event-scoped gameplay tags. Never impersonate payroll, business
        // ownership, courses or any other verified V3 core fact.
        public IReadOnlyList<string> GrantedTags { get; }

        public EventChoiceDefinition(string id, string labelKey, string outcomeId,
            IEnumerable<string>? grantedTags = null)
        {
            _ = new ContentId(id);
            _ = new ContentId(outcomeId);
            if (string.IsNullOrWhiteSpace(labelKey))
                throw new ArgumentException("Event choice requires a localization key.");
            var tags = (grantedTags ?? Array.Empty<string>()).ToArray();
            if (tags.Any(x => !ContentId.IsValid(x) ||
                !x.StartsWith("event/", StringComparison.Ordinal)) ||
                tags.Distinct(StringComparer.Ordinal).Count() != tags.Length)
                throw new ArgumentException("An event choice may grant only unique event-scoped tags.");
            Id = id;
            LabelKey = labelKey;
            OutcomeId = outcomeId;
            GrantedTags = Array.AsReadOnly(tags.OrderBy(x => x, StringComparer.Ordinal).ToArray());
        }
    }

    public sealed class GameEventDefinition
    {
        public string Id { get; }
        public string CategoryId { get; }
        public int MinimumYear { get; }
        public int MinimumStage { get; }
        public int Weight { get; }
        public int CooldownDays { get; }
        public int MaxOccurrences { get; }
        public int WindowPeriodDays { get; }
        public int WindowOffsetDays { get; }
        public IReadOnlyList<string> RequiredTags { get; }
        public IReadOnlyList<string> ForbiddenTags { get; }
        public IReadOnlyList<EventChoiceDefinition> Choices { get; }
        // Optional authored narrative keys. Empty keys retain compatibility with
        // pre-narrative deterministic test catalogs and historical V4 journal fixtures.
        public string TitleKey { get; }
        public string BodyKey { get; }

        public GameEventDefinition(string id, string categoryId, int minimumYear, int minimumStage,
            int weight, int cooldownDays, int maxOccurrences, int windowPeriodDays, int windowOffsetDays,
            IEnumerable<string> requiredTags, IEnumerable<string> forbiddenTags,
            IEnumerable<EventChoiceDefinition> choices, string titleKey = "", string bodyKey = "")
        {
            _ = new ContentId(id);
            _ = new ContentId(categoryId);
            var required = (requiredTags ?? throw new ArgumentNullException(nameof(requiredTags))).ToArray();
            var forbidden = (forbiddenTags ?? throw new ArgumentNullException(nameof(forbiddenTags))).ToArray();
            var options = (choices ?? throw new ArgumentNullException(nameof(choices))).ToArray();
            if (minimumYear < 1 || minimumYear > 9999 || minimumStage < 0 || weight <= 0 ||
                cooldownDays < 0 || maxOccurrences <= 0 || windowPeriodDays <= 0 ||
                windowOffsetDays < 0 || windowOffsetDays >= windowPeriodDays)
                throw new ArgumentException("Invalid event timing, rarity or frequency.");
            if (required.Any(x => !ContentId.IsValid(x)) || forbidden.Any(x => !ContentId.IsValid(x)) ||
                required.Distinct(StringComparer.Ordinal).Count() != required.Length ||
                forbidden.Distinct(StringComparer.Ordinal).Count() != forbidden.Length ||
                required.Intersect(forbidden, StringComparer.Ordinal).Any() ||
                options.Length == 0 || options.Any(x => x == null) ||
                options.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != options.Length)
                throw new ArgumentException("Invalid event tag or choice configuration.");

            if (titleKey == null || bodyKey == null ||
                (titleKey.Length == 0) != (bodyKey.Length == 0) ||
                (titleKey.Length > 0 && (string.IsNullOrWhiteSpace(titleKey) ||
                    string.IsNullOrWhiteSpace(bodyKey))))
                throw new ArgumentException("Event narrative title and body keys must be paired.");

            TitleKey = titleKey;
            BodyKey = bodyKey;
            Id = id;
            CategoryId = categoryId;
            MinimumYear = minimumYear;
            MinimumStage = minimumStage;
            Weight = weight;
            CooldownDays = cooldownDays;
            MaxOccurrences = maxOccurrences;
            WindowPeriodDays = windowPeriodDays;
            WindowOffsetDays = windowOffsetDays;
            RequiredTags = Array.AsReadOnly(required);
            ForbiddenTags = Array.AsReadOnly(forbidden);
            Choices = Array.AsReadOnly(options);
        }
    }

    // Detached read-side history; authoritatively persisted event state is deliberately not introduced here.
    public sealed class EventOccurrence
    {
        public string EventId { get; }
        public int Count { get; }
        public SimDate LastDate { get; }

        public EventOccurrence(string eventId, int count, SimDate lastDate)
        {
            _ = new ContentId(eventId);
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            EventId = eventId;
            Count = count;
            LastDate = lastDate;
        }
    }

    public sealed class GameEventContext
    {
        public SimDate OriginDate { get; }
        public SimDate Date { get; }
        public int Stage { get; }
        public int ElapsedDays { get; }
        public IReadOnlyCollection<string> Tags { get; }
        public IReadOnlyDictionary<string, EventOccurrence> Occurrences { get; }

        public GameEventContext(SimDate originDate, SimDate date, int stage, IEnumerable<string> tags,
            IEnumerable<EventOccurrence> occurrences)
        {
            if (date < originDate || stage < 0) throw new ArgumentException("Invalid event context date/stage.");
            var tagSet = new HashSet<string>(tags ?? throw new ArgumentNullException(nameof(tags)), StringComparer.Ordinal);
            if (tagSet.Any(x => !ContentId.IsValid(x)))
                throw new ArgumentException("Invalid event context tag.");
            var history = (occurrences ?? throw new ArgumentNullException(nameof(occurrences))).ToArray();
            if (history.Any(x => x == null || x.LastDate < originDate || x.LastDate > date) ||
                history.Select(x => x.EventId).Distinct(StringComparer.Ordinal).Count() != history.Length)
                throw new ArgumentException("Invalid event occurrence history.");
            OriginDate = originDate;
            Date = date;
            Stage = stage;
            ElapsedDays = (new DateTime(date.Year, date.Month, date.Day) -
                new DateTime(originDate.Year, originDate.Month, originDate.Day)).Days;
            Tags = Array.AsReadOnly(tagSet.OrderBy(x => x, StringComparer.Ordinal).ToArray());
            Occurrences = new ReadOnlyDictionary<string, EventOccurrence>(
                history.ToDictionary(x => x.EventId, StringComparer.Ordinal));
        }
    }
}
