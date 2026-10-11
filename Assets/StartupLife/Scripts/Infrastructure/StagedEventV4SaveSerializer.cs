#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using StartupLife.Core;
using StartupLife.Simulation;

namespace StartupLife.Infrastructure
{
    public sealed class StagedEventV4LoadResult
    {
        public LoadStatus Status { get; }
        public EventV4Payload? Payload { get; }
        public GameState? CoreState { get; }
        public string ReasonKey { get; }
        internal StagedEventV4LoadResult(LoadStatus status, EventV4Payload? payload = null,
            GameState? core = null, string reason = "")
        {
            Status = status;
            Payload = payload;
            CoreState = core;
            ReasonKey = reason;
        }
    }

    // Source-only wire/migration admission. NOT an active ISaveSerializer and not
    // registered in GameSession/AtomicFileSaveStore until unified receipt CAS is built.
    public sealed class StagedEventV4SaveSerializer
    {
        private readonly V3SaveCompatibilitySerializer v3;
        private readonly IReadOnlyList<GameEventDefinition> definitions;
        private readonly IReadOnlyList<EventCategoryPolicy> policies;

        public StagedEventV4SaveSerializer(V3SaveCompatibilitySerializer v3,
            IEnumerable<GameEventDefinition> definitions, IEnumerable<EventCategoryPolicy> policies)
        {
            this.v3 = v3 ?? throw new ArgumentNullException(nameof(v3));
            if (definitions == null || policies == null) throw new ArgumentNullException("event catalog");
            this.definitions = Array.AsReadOnly(definitions.ToArray());
            this.policies = Array.AsReadOnly(policies.ToArray());
            // Validate the authoring roster before accepting any persisted checkpoint.
            EventDecisionJournal.Validate(EventDecisionJournal.New("catalog-validation", 1),
                this.definitions, this.policies);
        }

        // Historical migration is in-memory only; never rewrites the user's store.
        public StagedEventV4LoadResult MigrateVerified(byte[] envelopeBytes)
        {
            if (envelopeBytes == null) return new StagedEventV4LoadResult(LoadStatus.Corrupt, reason: "save.invalid");
            SaveEnvelope envelope;
            try { envelope = JsonSaveSerializer.ReadObject<SaveEnvelope>(envelopeBytes); }
            catch (Exception) { return new StagedEventV4LoadResult(LoadStatus.Corrupt, reason: "save.invalid"); }
            if (envelope.SchemaVersion > 4)
                return new StagedEventV4LoadResult(LoadStatus.FutureVersion, reason: "save.future_version");
            if (envelope.SchemaVersion == 4) return DeserializeVerified(envelopeBytes);
            if (envelope.SchemaVersion < 0)
                return new StagedEventV4LoadResult(LoadStatus.Corrupt, reason: "save.schema_mismatch");

            var loaded = v3.DeserializeAndValidate(envelopeBytes);
            if (loaded.Status != LoadStatus.Valid)
                return new StagedEventV4LoadResult(loaded.Status, reason: loaded.Reason);
            try
            {
                var verifiedV3 = envelope.SchemaVersion == 3 ?
                    envelopeBytes : v3.Serialize(loaded.State!);
                var payload = FromV3(verifiedV3, loaded.State!);
                // Full cold admission before presenting a migrated state.
                return DeserializeVerified(SerializeVerified(payload));
            }
            catch (ContentCompatibilityException error)
            { return new StagedEventV4LoadResult(LoadStatus.UnsupportedContent, reason: error.ReasonKey); }
            catch (Exception)
            { return new StagedEventV4LoadResult(LoadStatus.Corrupt, reason: "save.migration_invalid"); }
        }

        public EventV4Payload FromV3(byte[] verifiedV3Envelope, GameState core)
        {
            if (verifiedV3Envelope == null || core == null || core.SaveVersion != 3 ||
                core.EventRng == 0)
                throw new ArgumentException("Validated v3 checkpoint required for migration.");
            var incoming = v3.DeserializeAndValidate(verifiedV3Envelope);
            if (incoming.Status != LoadStatus.Valid || incoming.State == null ||
                incoming.State.RunId != core.RunId ||
                incoming.State.Revision != core.Revision ||
                incoming.State.EventRng != core.EventRng)
                throw new ArgumentException("Mismatched V3 migration source.");
            return new EventV4Payload
            {
                EmbeddedV3EnvelopeBase64 = Convert.ToBase64String(verifiedV3Envelope),
                EmbeddedV3EnvelopeSha256 = Digest(verifiedV3Envelope),
                Journal = EventDecisionJournal.New(core.RunId, core.EventRng)
            };
        }

        public byte[] SerializeVerified(EventV4Payload payload)
        {
            var core = ValidatePayload(payload);
            return JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(payload), 4,
                checked(core.Revision + payload.Journal!.Revision));
        }

        public StagedEventV4LoadResult DeserializeVerified(byte[] envelopeBytes)
        {
            SaveEnvelope envelope;
            try { envelope = JsonSaveSerializer.ReadObject<SaveEnvelope>(envelopeBytes); }
            catch (Exception) { return new StagedEventV4LoadResult(LoadStatus.Corrupt, reason: "save.invalid"); }
            if (envelope.SchemaVersion > 4)
                return new StagedEventV4LoadResult(LoadStatus.FutureVersion, reason: "save.future_version");
            if (envelope.SchemaVersion != 4 || envelope.Generation < 0)
                return new StagedEventV4LoadResult(LoadStatus.Corrupt, reason: "save.schema_mismatch");
            try
            {
                var bytes = Convert.FromBase64String(envelope.PayloadBase64);
                if (Digest(bytes) != envelope.Checksum)
                    return new StagedEventV4LoadResult(LoadStatus.Corrupt, reason: "save.checksum");
                var payload = JsonSaveSerializer.ReadObject<EventV4Payload>(bytes);
                var core = ValidatePayload(payload);
                if (checked(core.Revision + payload.Journal!.Revision) != envelope.Generation)
                    return new StagedEventV4LoadResult(LoadStatus.Corrupt, reason: "save.generation");
                return new StagedEventV4LoadResult(LoadStatus.Valid, payload, core);
            }
            catch (ContentCompatibilityException error)
            { return new StagedEventV4LoadResult(LoadStatus.UnsupportedContent, reason: error.ReasonKey); }
            catch (NotSupportedException)
            { return new StagedEventV4LoadResult(LoadStatus.UnsupportedContent, reason: "save.v3_replay_unavailable"); }
            catch (Exception)
            { return new StagedEventV4LoadResult(LoadStatus.Corrupt, reason: "save.invalid"); }
        }

        private GameState ValidatePayload(EventV4Payload payload)
        {
            if (payload == null || payload.WireSchema != 4 ||
                string.IsNullOrEmpty(payload.EmbeddedV3EnvelopeBase64) ||
                string.IsNullOrEmpty(payload.EmbeddedV3EnvelopeSha256) ||
                payload.Journal == null)
                throw new ArgumentException("Incomplete event v4 wire payload.");
            var v3Bytes = Convert.FromBase64String(payload.EmbeddedV3EnvelopeBase64);
            if (Digest(v3Bytes) != payload.EmbeddedV3EnvelopeSha256)
                throw new ArgumentException("Embedded V3 checkpoint digest mismatch.");
            var core = v3.DeserializeAndValidate(v3Bytes);
            if (core.Status == LoadStatus.UnsupportedContent)
                throw new ContentCompatibilityException(core.Reason, "Embedded V3 content is unavailable.");
            if (core.Status != LoadStatus.Valid || core.State == null)
                throw new ArgumentException("Embedded V3 checkpoint failed authoritative replay.");
            var state = core.State;
            if (state.RunId != payload.Journal.RunId ||
                state.EventRng != payload.Journal.InitialEventRng ||
                payload.Journal.Tickets.Any(t => string.CompareOrdinal(t.OpenedDateIso, state.DateIso) > 0) ||
                (payload.Journal.PendingTicketId.Length > 0 &&
                    payload.Journal.Tickets.Last().OpenedDateIso != state.DateIso))
                throw new ArgumentException("Event journal and V3 time/run/RNG anchor disagree.");
            EventJournalReplay.Validate(payload.Journal, definitions, policies);
            return state;
        }

        private static string Digest(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
}
