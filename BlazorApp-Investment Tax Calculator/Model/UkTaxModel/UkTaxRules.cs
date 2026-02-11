namespace InvestmentTaxCalculator.Model.UkTaxModel;

/// <summary>
/// Static helper class for common UK HMRC tax rules and thresholds.
/// </summary>
public static class UkTaxRules
{
    /// <summary>
    /// The absolute threshold for "small" distributions under TCGA 1992 s122.
    /// Distributions under this amount (currently £3,000) can typically be treated as cost basis reductions.
    /// </summary>
    public const decimal SmallDistributionAbsoluteThreshold = 3000m;

    /// <summary>
    /// The percentage threshold for "small" distributions under TCGA 1992 s122.
    /// Distributions under this percentage (currently 5%) of the value of the shareholding 
    /// can typically be treated as cost basis reductions.
    /// </summary>
    public const decimal SmallDistributionPercentageThreshold = 0.05m;

    /// <summary>
    /// Determines if a cash distribution (e.g. cash-in-lieu) qualifies as "small" under TCGA s122.
    /// It is small if it is <= £3000 OR <= 5% of the total market value of the shareholding before the distribution.
    /// </summary>
    /// <param name="cashAmountInBaseCurrency">The cash received in base currency.</param>
    /// <param name="totalValueBeforeDistribution">The total market value of the shareholding immediately before the distribution/event.</param>
    /// <returns>True if the cash is considered small under HMRC rules.</returns>
    public static bool IsSmallCash(decimal cashAmountInBaseCurrency, decimal totalValueBeforeDistribution)
    {
        if (cashAmountInBaseCurrency <= SmallDistributionAbsoluteThreshold)
            return true;

        if (totalValueBeforeDistribution > 0)
        {
            decimal fivePercentThreshold = totalValueBeforeDistribution * SmallDistributionPercentageThreshold;
            return cashAmountInBaseCurrency <= fivePercentThreshold;
        }

        return false;
    }

    /// <summary>
    /// Calculates the allowable cost used for a cash component in a corporate action.
    /// Implements both the "small distribution" (s122) deferral logic and the standard A/(A+B) part-disposal rule.
    /// </summary>
    /// <param name="cashAmount">The amount of cash received (in base currency).</param>
    /// <param name="relevantCostBasis">The cost basis to be proportioned (for mergers/spinoffs, the whole pool; for split rounding, the cost of the fractional part).</param>
    /// <param name="totalMarketValue">The total market value (Cash + Shares) relevant to the disposal.</param>
    /// <param name="electTaxDeferral">Whether to elect tax deferral if the cash is "small".</param>
    /// <returns>The calculated allowable cost.</returns>
    public static WrappedMoney CalculateAllowableCostForCash(WrappedMoney cashAmount, WrappedMoney relevantCostBasis, decimal totalMarketValue, bool electTaxDeferral)
    {
        if (IsSmallCash(cashAmount.Amount, totalMarketValue) && electTaxDeferral)
        {
            // Deferral: use cost to cover as much of the cash as possible, up to the relevant cost basis.
            return WrappedMoney.Min(cashAmount, relevantCostBasis);
        }

        // Standard part-disposal: A / (A + B)
        if (totalMarketValue > 0)
        {
            decimal cashProportion = cashAmount.Amount / totalMarketValue;
            return relevantCostBasis * cashProportion;
        }

        return WrappedMoney.GetBaseCurrencyZero();
    }
}
