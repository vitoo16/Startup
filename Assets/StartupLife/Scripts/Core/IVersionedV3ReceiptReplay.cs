#nullable enable
namespace StartupLife.Core
{
    public interface IVersionedV3ReceiptReplay
    {
        void ValidateV3(EconomicV3Payload target, IHistoricalEconomicRulesResolver archives);
    }
}
