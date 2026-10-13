#nullable enable
using System;
namespace StartupLife.Core
{
    // Optional extension for callers that need historical state at each *verified*
    // V3 receipt frontier. The observer may inspect but must never mutate the state.
    public interface IVersionedV3FrontierReplay : IVersionedV3ReceiptReplay
    {
        void ValidateV3(EconomicV3Payload target, IHistoricalEconomicRulesResolver archives,
            Action<GameState> observeFrontier);
    }
}
