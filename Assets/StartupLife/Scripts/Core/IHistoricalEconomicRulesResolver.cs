#nullable enable
namespace StartupLife.Core
{
    // No network lookups or implicit substitution. Unknown archive is UnsupportedContent.
    public interface IHistoricalEconomicRulesResolver
    {
        bool TryResolve(string archivedRulesetId, string originalContentVersion,
            out ContentCatalog? content);
    }
}
