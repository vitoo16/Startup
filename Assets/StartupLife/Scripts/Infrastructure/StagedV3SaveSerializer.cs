#nullable enable
using System;
using System.Security.Cryptography;
using StartupLife.Core;

namespace StartupLife.Infrastructure
{
    // Staged schema3 wire admission only; not the active ISaveSerializer and never
    // writes to disk. This will be adopted by JsonSaveSerializer/GameSession only
    // after a versioned v3 suffix evaluator, CAS and state validation are approved.
    public sealed class StagedV3LoadResult
    {
        public LoadStatus Status { get; }
        public EconomicV3Payload? Payload { get; }
        public string ReasonKey { get; }
        internal StagedV3LoadResult(LoadStatus status, EconomicV3Payload? payload=null, string reason="")
        {
            Status=status;Payload=payload;ReasonKey=reason;
        }
    }

    public sealed class StagedV3SaveSerializer
    {
        private readonly IHistoricalEconomicRulesResolver archives;
        private readonly IVersionedReceiptReplay replay;

        public StagedV3SaveSerializer(IHistoricalEconomicRulesResolver archives,
            IVersionedReceiptReplay replay)
        {
            this.archives=archives ?? throw new ArgumentNullException(nameof(archives));
            this.replay=replay ?? throw new ArgumentNullException(nameof(replay));
        }

        public byte[] SerializeVerified(EconomicV3Payload payload)
        {
            var data=EconomicV3PayloadCodec.SerializeChecked(payload);
            VerifySource(payload);
            return JsonSaveSerializer.Wrap(data,3,payload.Current!.Revision);
        }

        public StagedV3LoadResult DeserializeVerified(byte[] envelopeBytes)
        {
            SaveEnvelope envelope;
            try { envelope=JsonSaveSerializer.ReadObject<SaveEnvelope>(envelopeBytes); }
            catch (Exception) { return new StagedV3LoadResult(LoadStatus.Corrupt,reason:"save.invalid"); }
            if (envelope.SchemaVersion > 3)
                return new StagedV3LoadResult(LoadStatus.FutureVersion,reason:"save.future_version");
            if (envelope.SchemaVersion != 3 || envelope.Generation < 0)
                return new StagedV3LoadResult(LoadStatus.Corrupt,reason:"save.schema_mismatch");
            try
            {
                var bytes=Convert.FromBase64String(envelope.PayloadBase64);
                if (!string.Equals(Digest(bytes),envelope.Checksum,StringComparison.Ordinal))
                    return new StagedV3LoadResult(LoadStatus.Corrupt,reason:"save.checksum");
                var payload=EconomicV3PayloadCodec.ReadChecked(bytes);
                if (payload.Current!.Revision!=envelope.Generation)
                    return new StagedV3LoadResult(LoadStatus.Corrupt,reason:"save.generation");
                VerifySource(payload);
                return new StagedV3LoadResult(LoadStatus.Valid,payload);
            }
            catch (ContentCompatibilityException error)
            {
                return new StagedV3LoadResult(LoadStatus.UnsupportedContent,reason:error.ReasonKey);
            }
            catch (NotSupportedException)
            {
                return new StagedV3LoadResult(LoadStatus.UnsupportedContent,reason:"save.v3_replay_unavailable");
            }
            catch (Exception)
            {
                return new StagedV3LoadResult(LoadStatus.Corrupt,reason:"save.invalid");
            }
        }

        private void VerifySource(EconomicV3Payload body)
        {
            var legacy=JsonSaveSerializer.ReadObject<GameState>(
                Convert.FromBase64String(body.OriginalV2PayloadBase64));
            replay.Validate(legacy,body.Current!,body.Activation!.ToAnchor(),archives);
        }

        private static string Digest(byte[] payload)
        {
            using (var sha=SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(payload)).Replace("-","").ToLowerInvariant();
        }
    }
}
