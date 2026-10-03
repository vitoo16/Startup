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
    public sealed class JsonSaveSerializer : ISaveSerializer
    {
        private readonly ContentCatalog content;
        private readonly Dictionary<int, ISaveMigration> migrations;
        private readonly IRestoreStateValidator restoreValidator;
        public JsonSaveSerializer(ContentCatalog catalog, IRestoreStateValidator validator, params ISaveMigration[] steps)
        {
            content = catalog; restoreValidator = validator ?? throw new ArgumentNullException(nameof(validator));
            migrations = steps.ToDictionary(x => x.FromVersion);
            if (steps.Any(x => x.ToVersion != x.FromVersion + 1 || x.FromVersion < 0)) throw new ArgumentException("Migrations must be sequential.");
        }
        public byte[] Serialize(GameState state)
        {
            StateValidation.Validate(state, content, restoreValidator);
            return Wrap(WriteObject(state), state.SaveVersion, state.Revision);
        }
        public LoadResult DeserializeAndValidate(byte[] bytes)
        {
            try
            {
                var envelope = ReadObject<SaveEnvelope>(bytes);
                if (envelope.SchemaVersion > 1) return new LoadResult(LoadStatus.FutureVersion, reason: "save.future_version");
                var payload = Convert.FromBase64String(envelope.PayloadBase64);
                if (Digest(payload) != envelope.Checksum || envelope.SchemaVersion < 0) return new LoadResult(LoadStatus.Corrupt, reason: "save.checksum");
                var version = envelope.SchemaVersion;
                while (version < 1)
                {
                    if (!migrations.TryGetValue(version, out var migration)) return new LoadResult(LoadStatus.Corrupt, reason: "save.migration_missing");
                    payload = migration.Migrate(payload); version = migration.ToVersion;
                    if (ReadObject<GameState>(payload).SaveVersion != version) return new LoadResult(LoadStatus.Corrupt, reason: "save.migration_invalid");
                }
                var state = ReadObject<GameState>(payload);
                if (state.SaveVersion != version || state.Revision != envelope.Generation) return new LoadResult(LoadStatus.Corrupt, reason: "save.generation");
                StateValidation.Validate(state, content, restoreValidator);
                return new LoadResult(LoadStatus.Valid, state);
            }
            catch (ContentCompatibilityException e)
            { return new LoadResult(LoadStatus.UnsupportedContent, reason: e.ReasonKey); }
            catch (Exception e) when (e is SerializationException || e is ArgumentException || e is FormatException || e is OverflowException || e is NullReferenceException || e is InvalidOperationException)
            { return new LoadResult(LoadStatus.Corrupt, reason: "save.invalid"); }
        }
        public static byte[] Wrap(byte[] payload, int schema, long generation) => WriteObject(new SaveEnvelope
        { SchemaVersion = schema, Generation = generation, Checksum = Digest(payload), PayloadBase64 = Convert.ToBase64String(payload) });
        public static byte[] WriteObject<T>(T value)
        {
            using (var stream = new MemoryStream()) { new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value); return stream.ToArray(); }
        }
        public static T ReadObject<T>(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes)) return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream)!;
        }
        private static string Digest(byte[] bytes)
        { using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    }
    // Synthetic fixture only. There were no shipped v0 saves; no schema shape is fabricated.
    public sealed class SyntheticV0Migration : ISaveMigration
    {
        public int FromVersion => 0;
        public int ToVersion => 1;
        public byte[] Migrate(byte[] payload)
        {
            var state = JsonSaveSerializer.ReadObject<GameState>(payload);
            if (state.SaveVersion != 0) throw new ArgumentException("Not synthetic v0.");
            state.SaveVersion = 1; return JsonSaveSerializer.WriteObject(state);
        }
    }
}
