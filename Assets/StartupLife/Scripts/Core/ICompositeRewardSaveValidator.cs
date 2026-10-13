#nullable enable
namespace StartupLife.Core
{
    // The serializer itself (never the command caller) must authorize any
    // nonstandard +2 generation inside AtomicFileSaveStore's existing lock.
    // Other schemas and ordinary gameplay remain strict +1 transactions.
    public interface ICompositeRewardSaveValidator
    {
        bool IsAuthenticatedRewardTransition(GameState previous, GameState candidate);
    }
}
