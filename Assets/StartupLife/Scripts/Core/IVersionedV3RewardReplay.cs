#nullable enable
using System;

namespace StartupLife.Core
{
    // Only an authenticated V4 journal may supply this callback. Normal V3
    // deserialize/serialize never accepts reward commands or this authorizer.
    public interface IVerifiedEventRewardReplay
    {
        // Return native consumed minutes derived from an authenticated V4
        // authored outcome, never from the untrusted receipt's claimed duration.
        int Apply(EconomicV3Payload candidate, CommandReceipt receipt,
            string operationId, ContentCatalog content);
    }

    public interface IVersionedV3RewardReplay : IVersionedV3FrontierReplay
    {
        void ValidateV3(EconomicV3Payload target,
            IHistoricalEconomicRulesResolver archives, Action<GameState> observeFrontier,
            IVerifiedEventRewardReplay rewardProof);
    }
}
