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
            return projected;
        }
    }
}
