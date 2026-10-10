#nullable enable
namespace StartupLife.Core
{
    // One replay authority used by restore, historical idempotent outcomes,
    // migration admission and financial provenance. No v3-evaluator fallback.
    public interface IVersionedReceiptReplay
    {
        void Validate(GameState originalV2, GameState currentV3,
            EconomicActivationAnchor anchor, IHistoricalEconomicRulesResolver archiveResolver);
    }
}
