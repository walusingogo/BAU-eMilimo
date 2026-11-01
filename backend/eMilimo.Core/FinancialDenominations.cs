namespace eMilimo.Core;

/// <summary>
/// Canonical cash denominations used by capture screens for the base currency.
/// Keeping the definition in one place prevents contribution and collection
/// counting screens from drifting apart.
/// </summary>
public static class FinancialDenominations
{
    public static readonly decimal[] BaseCurrency =
        [500m, 200m, 100m, 50m, 20m, 10m, 5m, 2m, 1m, .50m];
}
