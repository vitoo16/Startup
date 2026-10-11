#nullable enable
using System;
using System.Linq;
using StartupLife.Core;
using StartupLife.Infrastructure;

namespace StartupLife.Integration
{
    // Version-4 projection onto the existing ISaveSerializer/ISaveStore contract.
    // AtomicFileSaveStore retains exclusive authority for physical CAS and backup.
    // This adapter never edits the archived V2 or nested V3 serialized DTO.
    public sealed class EventV4CompatibilitySerializer : ISaveSerializer, IActiveSaveSchema
    {
        private readonly StagedEventV4SaveSerializer staged;

        public int ActiveSchemaVersion => 4;

        public EventV4CompatibilitySerializer(StagedEventV4SaveSerializer staged)
        {
            this.staged = staged ?? throw new ArgumentNullException(nameof(staged));
        }

        public LoadResult DeserializeAndValidate(byte[] bytes)
        {
            if (bytes == null) return new LoadResult(LoadStatus.Corrupt, reason: "save.invalid");
            var loaded = staged.MigrateVerified(bytes);
            if (loaded.Status != LoadStatus.Valid || loaded.Payload == null || loaded.CoreState == null)
                return new LoadResult(loaded.Status, reason: loaded.ReasonKey);
            try
            {
                var envelope = JsonSaveSerializer.ReadObject<SaveEnvelope>(bytes);
                var projected = Project(loaded.CoreState, loaded.Payload);
                return new LoadResult(LoadStatus.Valid, projected,
                    sourceSchemaVersion: envelope.SchemaVersion);
            }
            catch (Exception)
            { return new LoadResult(LoadStatus.Corrupt, reason: "save.invalid"); }
        }

        public byte[] Serialize(GameState state)
        {
            if (state == null || state.SaveVersion != 4 || state.EventV4 == null)
                throw new ArgumentException("A V4 attached state is required.");
            var validatedWire = staged.SerializeVerified(state.EventV4, out var core);
            // Refuse any state mutation outside the projected EventJournal. That also
            // protects V3 salary, economics, receipts and scheduler RNG from silent writes.
            var expected = Project(core, state.EventV4);
            var claimed = JsonSaveSerializer.WriteObject(state);
            var canonical = JsonSaveSerializer.WriteObject(expected);
            if (!claimed.SequenceEqual(canonical))
                throw new ArgumentException("V4 candidate changed fields outside the event journal.");
            return validatedWire;
        }

        internal EventV4Payload ReplaceCore(EventV4Payload source, byte[] newV3Envelope,
            out GameState core) => staged.ReplaceCore(source, newV3Envelope, out core);

        internal static GameState RestoreCoreProjection(GameState current)
        {
            if (current.SaveVersion != 4 || current.EventV4?.Journal == null)
                throw new ArgumentException("Attached V4 state required.");
            var core = JsonSaveSerializer.ReadObject<GameState>(
                JsonSaveSerializer.WriteObject(current));
            core.SaveVersion = 3;
            core.Revision = checked(current.Revision - current.EventV4.Journal.Revision);
            core.EventRng = current.EventV4.Journal.InitialEventRng;
            core.PendingChoiceId = "";
            return core;
        }

        internal static GameState Project(GameState v3Core, EventV4Payload payload)
        {
            if (payload.Journal == null) throw new ArgumentException("Missing event journal.");
            var projected = JsonSaveSerializer.ReadObject<GameState>(
                JsonSaveSerializer.WriteObject(v3Core));
            projected.SaveVersion = 4;
            projected.Revision = checked(v3Core.Revision + payload.Journal.Revision);
            projected.EventRng = payload.Journal.EventRng;
            projected.PendingChoiceId = payload.Journal.PendingTicketId;
            projected.EventV4 = payload;
            // GameState.EconomicV3 is [IgnoreDataMember]: the ordinary V3
            // projection clone loses its *validated* business ledger view.
            // Attach a fresh non-wire wrapper over the already receipt-verified
            // records, with Current pointing to this exact projected state.
            // No economic values are recomputed, credited, or serialized by V4.
            var economic = v3Core.EconomicV3;
            if (economic?.Current != v3Core || economic.Activation == null ||
                economic.EconomicRecords == null)
                throw new ArgumentException("Verified V3 economic projection required.");
            projected.EconomicV3 = new EconomicV3Payload
            {
                WireSchema = economic.WireSchema,
                Current = projected,
                Activation = economic.Activation,
                EconomicRecords = economic.EconomicRecords,
                OriginalV2PayloadBase64 = economic.OriginalV2PayloadBase64,
                OriginalV2PayloadSha256 = economic.OriginalV2PayloadSha256,
                OriginalV2ContentVersion = economic.OriginalV2ContentVersion,
                OriginalV2RulesetId = economic.OriginalV2RulesetId,
                OriginalSourceSchema = economic.OriginalSourceSchema,
                OriginalSourcePayloadBase64 = economic.OriginalSourcePayloadBase64,
                OriginalSourcePayloadSha256 = economic.OriginalSourcePayloadSha256
            };
            return projected;
        }
    }
}
