#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // Authored snapshots are immutable content *references*, never copies of
    // the replay engine. Historical definitions must come from a retained
    // released roster, not be reconstructed from untrusted save metadata.
    public sealed class EventCatalogSnapshot
    {
        public IReadOnlyList<GameEventDefinition> Definitions { get; }
        public IReadOnlyList<EventCategoryPolicy> Policies { get; }
        public string Fingerprint { get; }
        // Empty for the current Unity StringTable-driven roster; bundled
        // historical releases carry immutable Vietnamese render copy.
        public IReadOnlyDictionary<string, string> VietnameseText { get; }

        public EventCatalogSnapshot(IEnumerable<GameEventDefinition> definitions,
            IEnumerable<EventCategoryPolicy> policies,
            IEnumerable<KeyValuePair<string, string>>? archivedVietnamese = null)
        {
            if (definitions == null || policies == null)
                throw new ArgumentNullException("An event catalog snapshot needs both rosters.");
            Definitions = Array.AsReadOnly(definitions.ToArray());
            Policies = Array.AsReadOnly(policies.ToArray());
            EventDecisionJournal.Validate(EventDecisionJournal.New("catalog-archive", 1),
                Definitions, Policies);
            Fingerprint = EventCatalogFingerprint.Compute(Definitions, Policies);

            var texts = new Dictionary<string, string>(StringComparer.Ordinal);
            if (archivedVietnamese != null)
            {
                foreach (var entry in archivedVietnamese)
                    if (string.IsNullOrWhiteSpace(entry.Key) ||
                        string.IsNullOrWhiteSpace(entry.Value) ||
                        !texts.TryAdd(entry.Key, entry.Value))
                        throw new ArgumentException("Duplicate or empty archived Vietnamese event text.");
                // A partial translation bundle must never appear to be an
                // authoritative frozen release snapshot.
                foreach (var definition in Definitions)
                {
                    if (!texts.ContainsKey(definition.TitleKey) ||
                        !texts.ContainsKey(definition.BodyKey) ||
                        definition.Choices.Any(choice => !texts.ContainsKey(choice.LabelKey)))
                        throw new ArgumentException("Missing archived Vietnamese event text.");
                }
            }
            VietnameseText = new ReadOnlyDictionary<string, string>(texts);
        }
    }

    public sealed class EventCatalogArchive
    {
        private readonly IReadOnlyDictionary<string, EventCatalogSnapshot> indexed;
        public EventCatalogSnapshot Current { get; }
        public EventCatalogSnapshot LegacyBaseline { get; }

        // If a new roster is introduced, callers MUST declare the baseline
        // used for previously unpinned V4 checkpoint selection. No guessing
        // based on the save date or matching an outcome ID is permitted.
        public EventCatalogArchive(EventCatalogSnapshot current,
            IEnumerable<EventCatalogSnapshot>? historical = null,
            EventCatalogSnapshot? legacyBaseline = null)
        {
            Current = current ?? throw new ArgumentNullException(nameof(current));
            var older = (historical ?? Array.Empty<EventCatalogSnapshot>()).ToArray();
            if (older.Any(x => x == null))
                throw new ArgumentException("Null historical event catalog.");
            if (older.Length > 0 && legacyBaseline == null)
                throw new ArgumentException("An explicit pre-fingerprint V4 baseline is required.");
            LegacyBaseline = legacyBaseline ?? current;
            var versions = new Dictionary<string, EventCatalogSnapshot>(StringComparer.Ordinal);
            foreach (var snapshot in new[] { Current, LegacyBaseline }.Concat(older))
            {
                // Recompute on admission: a purported fingerprint is never an
                // authority unless the bundled authored roster hashes to it.
                if (snapshot.Fingerprint != EventCatalogFingerprint.Compute(
                    snapshot.Definitions, snapshot.Policies))
                    throw new ArgumentException("An event archive snapshot changed after construction.");
                if (!versions.ContainsKey(snapshot.Fingerprint))
                    versions.Add(snapshot.Fingerprint, snapshot);
            }
            indexed = new ReadOnlyDictionary<string, EventCatalogSnapshot>(versions);
        }

        public EventCatalogSnapshot Resolve(EventJournalState journal)
        {
            if (journal == null || journal.CatalogFingerprint == null)
                throw new ArgumentException("Missing V4 journal catalog identity.");
            var identity = journal.CatalogFingerprint;
            if (identity.Length == 0) return LegacyBaseline;
            if (!EventCatalogFingerprint.IsWellFormed(identity) ||
                !indexed.TryGetValue(identity, out var snapshot))
                throw new ContentCompatibilityException("save.event_catalog_unavailable",
                    "The historical event catalog referenced by this save was not bundled.");
            return snapshot;
        }
    }
}
