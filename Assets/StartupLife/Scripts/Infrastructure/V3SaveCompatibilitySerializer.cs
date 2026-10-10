#nullable enable
using System;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Infrastructure
{
    // Active schema3 compatibility adapter for the preexisting AtomicFileSaveStore
    // and GameSession CAS contract. Merely constructing it does not rewrite a v2 save.
    // Cold reads migrate in-memory; the FIRST authorized gameplay command performs
    // the normal state.Revision+1 atomic commit with a v2 backup intact.
    public sealed class V3SaveCompatibilitySerializer :
        ISaveSerializer, ISaveCompatibilitySerializer, IActiveSaveSchema
    {
        private readonly JsonSaveSerializer legacy;
        private readonly V2ToV3Migration migration;
        private readonly StagedV3SaveSerializer staged;

        public int ActiveSchemaVersion => 3;

        public V3SaveCompatibilitySerializer(ContentCatalog historicalContent,
            IRestoreStateValidator originalRestoreValidator, V2ToV3Migration migration,
            StagedV3SaveSerializer staged)
        {
            if (historicalContent == null || originalRestoreValidator == null)
                throw new ArgumentNullException(nameof(historicalContent));
            legacy = new JsonSaveSerializer(historicalContent, originalRestoreValidator,
                new SyntheticV0Migration(), new V1ToV2Migration());
            this.migration = migration ?? throw new ArgumentNullException(nameof(migration));
            this.staged = staged ?? throw new ArgumentNullException(nameof(staged));
        }

        public byte[] Serialize(GameState state)
        {
            if (state == null || state.SaveVersion != 3 ||
                state.EconomicV3 == null || !ReferenceEquals(state.EconomicV3.Current,state))
                throw new ArgumentException("Only an attached schema3 authoritative candidate may be written.");
            return staged.SerializeVerified(state.EconomicV3);
        }

        public LoadResult DeserializeAndValidate(byte[] bytes)
        {
            if (bytes == null) return new LoadResult(LoadStatus.Corrupt,reason:"save.invalid");
            SaveEnvelope envelope;
            try { envelope = JsonSaveSerializer.ReadObject<SaveEnvelope>(bytes); }
            catch (Exception) { return new LoadResult(LoadStatus.Corrupt,reason:"save.invalid"); }
            if (envelope.SchemaVersion > ActiveSchemaVersion)
                return new LoadResult(LoadStatus.FutureVersion,reason:"save.future_version",
                    sourceSchemaVersion:envelope.SchemaVersion);
            if (envelope.SchemaVersion == ActiveSchemaVersion)
            {
                var loaded = staged.DeserializeVerified(bytes);
                if (loaded.Status != LoadStatus.Valid)
                    return new LoadResult(loaded.Status,reason:loaded.ReasonKey,sourceSchemaVersion:3);
                var state = loaded.Payload!.Current!;
                state.EconomicV3 = loaded.Payload;
                return new LoadResult(LoadStatus.Valid,state,sourceSchemaVersion:3);
            }
            var baseline = legacy.DeserializeAndValidate(bytes);
            if (baseline.Status != LoadStatus.Valid)
                return baseline;
            if (envelope.SchemaVersion < 0 || envelope.SchemaVersion > SaveSchema.HistoricalV2)
                return new LoadResult(LoadStatus.Corrupt,reason:"save.schema_mismatch");
            try
            {
                var original = Convert.FromBase64String(envelope.PayloadBase64);
                byte[] exactStage2;
                if (envelope.SchemaVersion == SaveSchema.HistoricalV2) exactStage2 = original;
                else
                {
                    var stageOne = envelope.SchemaVersion == 0 ?
                        new SyntheticV0Migration().Migrate(original) : original;
                    exactStage2 = new V1ToV2Migration().Migrate(stageOne);
                }
                var v3 = migration.MigrateFromOriginalSource(exactStage2,
                    envelope.SchemaVersion, original);
                var readback = staged.DeserializeVerified(JsonSaveSerializer.Wrap(
                    v3,3,baseline.State!.Revision));
                if (readback.Status != LoadStatus.Valid)
                    return new LoadResult(readback.Status,reason:readback.ReasonKey,
                        sourceSchemaVersion:envelope.SchemaVersion);
                var state = readback.Payload!.Current!;
                state.EconomicV3 = readback.Payload;
                return new LoadResult(LoadStatus.Valid,state,
                    sourceSchemaVersion:envelope.SchemaVersion);
            }
            catch (ContentCompatibilityException error)
            {
                return new LoadResult(LoadStatus.UnsupportedContent,reason:error.ReasonKey,
                    sourceSchemaVersion:envelope.SchemaVersion);
            }
            catch (Exception error) when (error is ArgumentException ||
                error is FormatException || error is InvalidOperationException)
            {
                return new LoadResult(LoadStatus.Corrupt,reason:"save.migration_invalid",
                    sourceSchemaVersion:envelope.SchemaVersion);
            }
        }

        public byte[] SerializeForSchema(GameState state, int schemaVersion)
        {
            if (state == null || state.EconomicV3 == null || state.EconomicV3.Current != state)
                throw new ArgumentException("Compatibility projection requires immutable archive association.");
            if (schemaVersion == 3) return Serialize(state);
            var archive = state.EconomicV3;
            if (schemaVersion != archive.OriginalSourceSchema)
                throw new ArgumentException("Cannot invent unsupported original source schema.");
            if (state.Revision != archive.Activation!.LegacyReceiptCount ||
                state.Receipts.Count != archive.Activation.LegacyReceiptCount)
                throw new ArgumentException("Compatibility repair allowed only before first v3 receipt.");
            if (schemaVersion != 2)
                throw new ArgumentException("Synthetic v0/v1 historical child normalization must precede cutover.");
            var before = Convert.FromBase64String(archive.OriginalV2PayloadBase64);
            var frozen = JsonSaveSerializer.ReadObject<GameState>(before);
            if (frozen.Receipts.Count != state.Receipts.Count)
                throw new ArgumentException("Historical receipt roster changed during compatibility repair.");
            for (var index = 0; index < frozen.Receipts.Count; index++)
                frozen.Receipts[index].CommandId = state.Receipts[index].CommandId;
            return JsonSaveSerializer.Wrap(JsonSaveSerializer.WriteObject(frozen),
                SaveSchema.HistoricalV2,state.Revision);
        }
    }
}
