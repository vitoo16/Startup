#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using StartupLife.Core;

namespace StartupLife.Infrastructure
{
    [DataContract]
    public sealed class SaveEnvelope
    {
        [DataMember(Order = 0)] public int SchemaVersion { get; set; }
        [DataMember(Order = 1)] public long Generation { get; set; }
        [DataMember(Order = 2)] public string Checksum { get; set; } = "";
        [DataMember(Order = 3)] public string PayloadBase64 { get; set; } = "";
    }

    // No reflection-derived Unity dependencies. IL2CPP preservation/device proof remains a separate gate.
    public sealed class JsonSaveSerializer : ISaveSerializer, ISaveCompatibilitySerializer
    {
        private readonly ContentCatalog content;
        private readonly Dictionary<int, ISaveMigration> migrations;
        private readonly IRestoreStateValidator restoreValidator;

        public JsonSaveSerializer(ContentCatalog catalog, IRestoreStateValidator validator, params ISaveMigration[] steps)
        {
            content = catalog;
            restoreValidator = validator ?? throw new ArgumentNullException(nameof(validator));
            migrations = new Dictionary<int, ISaveMigration>();
            foreach (var step in steps ?? Array.Empty<ISaveMigration>())
            {
                if (step == null || step.FromVersion < 0 || step.ToVersion != step.FromVersion + 1)
                    throw new ArgumentException("Migrations must be adjacent sequential steps.");
                if (migrations.ContainsKey(step.FromVersion))
                    throw new ArgumentException("Duplicate migration registration.");
                migrations.Add(step.FromVersion, step);
            }
        }

        public byte[] Serialize(GameState state)
        {
            if (state.SaveVersion != SaveSchema.CurrentVersion)
                throw new ArgumentException("Normal serialization accepts only the current save schema.");
            StateValidation.Validate(state, content, restoreValidator);
            return Wrap(WriteObject(state), SaveSchema.CurrentVersion, state.Revision);
        }

        public byte[] SerializeForSchema(GameState state, int schemaVersion)
        {
            StateValidation.Validate(state, content, restoreValidator);
            if (schemaVersion == SaveSchema.CurrentVersion) return Serialize(state);
            if (schemaVersion == SaveSchema.HistoricalV1)
                return Wrap(HistoricalV1Codec.EncodeFromCurrent(state), SaveSchema.HistoricalV1, state.Revision);
            throw new ArgumentException("Unsupported compatibility schema.");
        }

        public LoadResult DeserializeAndValidate(byte[] bytes)
        {
            try
            {
                var envelope = ReadObject<SaveEnvelope>(bytes);
                var sourceVersion = envelope.SchemaVersion;
                if (sourceVersion > SaveSchema.CurrentVersion)
                    return new LoadResult(LoadStatus.FutureVersion, reason: "save.future_version", sourceSchemaVersion: sourceVersion);
                if (sourceVersion < 0)
                    return new LoadResult(LoadStatus.Corrupt, reason: "save.checksum", sourceSchemaVersion: sourceVersion);

                var payload = Convert.FromBase64String(envelope.PayloadBase64);
                if (Digest(payload) != envelope.Checksum)
                    return new LoadResult(LoadStatus.Corrupt, reason: "save.checksum", sourceSchemaVersion: sourceVersion);

                var version = sourceVersion;
                GameState state;
                try
                {
                    state = ValidateStage(payload, version, envelope.Generation);
                }
                catch (SaveStageException e)
                {
                    return new LoadResult(LoadStatus.Corrupt, reason: e.ReasonKey, sourceSchemaVersion: sourceVersion);
                }
                catch (ContentCompatibilityException) { throw; }
                catch (Exception e) when (IsValidationFailure(e))
                {
                    return new LoadResult(LoadStatus.Corrupt, reason: "save.invalid", sourceSchemaVersion: sourceVersion);
                }

                while (version < SaveSchema.CurrentVersion)
                {
                    if (!migrations.TryGetValue(version, out var migration))
                        return new LoadResult(LoadStatus.Corrupt, reason: "save.migration_missing", sourceSchemaVersion: sourceVersion);
                    try
                    {
                        payload = migration.Migrate(payload);
                        version = migration.ToVersion;
                        state = ValidateStage(payload, version, envelope.Generation);
                    }
                    catch (ContentCompatibilityException) { throw; }
                    catch (Exception e) when (IsValidationFailure(e))
                    {
                        return new LoadResult(LoadStatus.Corrupt, reason: "save.migration_invalid", sourceSchemaVersion: sourceVersion);
                    }
                }

                return new LoadResult(LoadStatus.Valid, state, sourceSchemaVersion: sourceVersion);
            }
            catch (ContentCompatibilityException e)
            { return new LoadResult(LoadStatus.UnsupportedContent, reason: e.ReasonKey); }
            catch (Exception e) when (IsValidationFailure(e))
            { return new LoadResult(LoadStatus.Corrupt, reason: "save.invalid"); }
        }

        private GameState ValidateStage(byte[] payload, int version, long generation)
        {
            GameState state;
            if (version == 0)
            {
                HistoricalV1Codec.ValidatePayload(payload, 0);
                var historical = HistoricalV1Codec.PromoteSyntheticV0ToV1(payload);
                state = HistoricalV1Codec.DecodeToCurrent(historical);
            }
            else if (version == SaveSchema.HistoricalV1)
            {
                HistoricalV1Codec.ValidatePayload(payload, SaveSchema.HistoricalV1);
                state = HistoricalV1Codec.DecodeToCurrent(payload);
            }
            else if (version == SaveSchema.CurrentVersion)
            {
                state = ReadObject<GameState>(payload);
                if (state.SaveVersion != SaveSchema.CurrentVersion)
                    throw new SaveStageException("save.schema_mismatch", "Payload SaveVersion differs from its schema stage.");
                if (state.Businesses == null)
                    throw new ArgumentException("Current save stage requires Businesses.");
            }
            else throw new ArgumentException("Unsupported migration stage.");

            if (state.Revision != generation)
                throw new SaveStageException("save.generation", "Envelope generation differs from payload revision.");
            StateValidation.Validate(state, content, restoreValidator);
            return state;
        }

        private sealed class SaveStageException : ArgumentException
        {
            public string ReasonKey { get; }
            public SaveStageException(string reasonKey, string message) : base(message) { ReasonKey = reasonKey; }
        }

        private static bool IsValidationFailure(Exception e) =>
            e is SerializationException || e is ArgumentException || e is FormatException || e is OverflowException ||
            e is NullReferenceException || e is InvalidOperationException;

        public static byte[] Wrap(byte[] payload, int schema, long generation) => WriteObject(new SaveEnvelope
        { SchemaVersion = schema, Generation = generation, Checksum = Digest(payload), PayloadBase64 = Convert.ToBase64String(payload) });

        public static byte[] WriteObject<T>(T value)
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value);
                return stream.ToArray();
            }
        }

        public static T ReadObject<T>(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream)!;
        }

        private static string Digest(byte[] bytes)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }

    // Synthetic fixture only. There were no shipped v0 saves.
    public sealed class SyntheticV0Migration : ISaveMigration
    {
        public int FromVersion => 0;
        public int ToVersion => SaveSchema.HistoricalV1;
        public byte[] Migrate(byte[] payload) => HistoricalV1Codec.PromoteSyntheticV0ToV1(payload);
    }

    public sealed class V1ToV2Migration : ISaveMigration
    {
        public int FromVersion => SaveSchema.HistoricalV1;
        public int ToVersion => SaveSchema.CurrentVersion;
        public byte[] Migrate(byte[] payload)
        {
            var state = HistoricalV1Codec.DecodeToCurrent(payload);
            return JsonSaveSerializer.WriteObject(state);
        }
    }
}
