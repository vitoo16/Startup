#nullable enable
using System;
using StartupLife.Core;

namespace StartupLife.Infrastructure
{
    // Only independently verified checkpoints can select the live ruleset.
    // This policy performs NO migration, save IO, receipts or writes.
    public enum BusinessContentTrack
    {
        BaselineV1 = 1,
        BusinessBatchA = 2
    }

    public static class VerifiedBusinessContentSelector
    {
        public static BusinessContentTrack Select(byte[]? primary, byte[]? backup,
            ISaveSerializer baseline, ISaveSerializer batchA, bool enableBatchAForNewRuns)
        {
            if (baseline == null || batchA == null)
                throw new ArgumentNullException(nameof(baseline));
            if (primary == null && backup == null)
                return enableBatchAForNewRuns ?
                    BusinessContentTrack.BusinessBatchA : BusinessContentTrack.BaselineV1;

            // A verified primary always takes precedence over a backup.
            // If a legacy V2 checkpoint validates under both serializers,
            // choosing BaselineV1 prevents an implicit content upgrade.
            var chosen = Verify(primary, baseline, batchA) ??
                Verify(backup, baseline, batchA);
            if (chosen.HasValue) return chosen.Value;

            // An invalid or unsupported existing save is never a new game.
            // The caller must show the recovery error and preserve both files.
            throw new InvalidOperationException(
                "No verified business content ruleset can read the existing primary or backup.");
        }

        private static BusinessContentTrack? Verify(byte[]? bytes,
            ISaveSerializer baseline, ISaveSerializer batchA)
        {
            if (bytes == null) return null;
            var oldStatus = baseline.DeserializeAndValidate(bytes).Status;
            if (oldStatus == LoadStatus.Valid)
                return BusinessContentTrack.BaselineV1;
            var newStatus = batchA.DeserializeAndValidate(bytes).Status;
            return newStatus == LoadStatus.Valid
                ? BusinessContentTrack.BusinessBatchA : (BusinessContentTrack?)null;
        }
    }
}
