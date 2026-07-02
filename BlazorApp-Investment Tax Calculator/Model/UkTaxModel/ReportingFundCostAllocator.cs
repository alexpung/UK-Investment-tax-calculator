using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

namespace InvestmentTaxCalculator.Model.UkTaxModel;

/// <summary>
/// Splits a reporting fund cost adjustment (excess reportable income uplift or equalisation reduction) between
/// disposals made in the "gap period" (after the end of the fund reporting period but before the fund distribution
/// date) and the section 104 pool.
/// Liability to the income is fixed by the holding at the end of the reporting period (SI 2009/3001 reg. 94(3)),
/// so the adjustment is apportioned per unit held at the period end. The share belonging to units disposed of
/// before the fund distribution date is applied to the allowable cost of those disposals (reg. 99(5) treats the
/// amount as received immediately before the disposal), and only the retained units' share is applied to the
/// section 104 pool on the fund distribution date (reg. 99(4)).
/// </summary>
public static class ReportingFundCostAllocator
{
    /// <summary>
    /// Apply <paramref name="adjustmentAmount"/> (positive for an ERI base cost uplift, negative for an
    /// equalisation base cost reduction) to the disposals in the gap period and the section 104 pool.
    /// Must be called when the pool state reflects all events before <paramref name="adjustmentDate"/>,
    /// which holds when invoked from ChangeSection104 as events are processed in chronological order.
    /// </summary>
    public static void Apply(UkSection104 section104, DateOnly reportingPeriodEnd, DateTime adjustmentDate, WrappedMoney adjustmentAmount, string description)
    {
        decimal quantityAtPeriodEnd = section104.GetLastSection104History(reportingPeriodEnd)?.NewQuantity ?? 0m;
        if (quantityAtPeriodEnd <= 0)
        {
            // No holding recorded at the reporting period end - nothing to apportion, adjust the pool as a whole.
            section104.AdjustAcquisitionCost(adjustmentAmount, adjustmentDate, description);
            return;
        }
        // Both are tracked in the pool's current unit scale: quantity rescaling events (e.g. stock splits) in the
        // gap period update the pair together so remaining * perUnit stays equal to the unallocated amount.
        WrappedMoney adjustmentPerUnit = adjustmentAmount / quantityAtPeriodEnd;
        decimal remainingPeriodEndUnits = quantityAtPeriodEnd;
        WrappedMoney unappliedAmount = WrappedMoney.GetBaseCurrencyZero();

        List<Section104History> gapPeriodQuantityChanges = [.. section104.Section104HistoryList
            .Where(history => DateOnly.FromDateTime(history.Date) > reportingPeriodEnd && history.QuantityChange != 0)];
        foreach (Section104History history in gapPeriodQuantityChanges)
        {
            if (remainingPeriodEndUnits <= 0) break;
            if (history.TradeTaxCalculation is null && history.ValueChange.Amount == 0 && history.OldQuantity > 0 && history.NewQuantity > 0)
            {
                // Quantity rescaling (e.g. stock split): the same holding continues under a new unit count.
                decimal rescaleFactor = history.NewQuantity / history.OldQuantity;
                remainingPeriodEndUnits *= rescaleFactor;
                adjustmentPerUnit /= rescaleFactor;
                continue;
            }
            if (history.QuantityChange > 0) continue; // acquisitions dilute the pool but keep the period end units
            // The section 104 pool is fungible, so a removal is treated as removing period end units in proportion
            // to their share of the pool immediately before the removal (just and reasonable apportionment).
            decimal removedQuantity = -history.QuantityChange;
            decimal periodEndUnitsRemoved = history.OldQuantity > 0
                ? Math.Min(remainingPeriodEndUnits, removedQuantity * remainingPeriodEndUnits / history.OldQuantity)
                : 0m;
            if (periodEndUnitsRemoved <= 0) continue;
            WrappedMoney removedUnitsShare = adjustmentPerUnit * periodEndUnitsRemoved;
            TradeMatch? match = history.TradeTaxCalculation is { AcquisitionDisposal: TradeType.DISPOSAL } disposal
                ? disposal.MatchHistory.FirstOrDefault(m => m.Section104HistorySnapshot == history)
                : null;
            if (match is not null)
            {
                match.BaseCurrencyMatchAllowableCost += removedUnitsShare;
                match.AdditionalInformation += $"{description}: allowable cost adjusted by {removedUnitsShare} for {periodEndUnitsRemoved:0.####} unit(s) " +
                    $"held at the reporting period end {reportingPeriodEnd:d} and disposed of before the fund distribution date " +
                    $"(treated as received immediately before the disposal, SI 2009/3001 reg. 99(5)).\n";
            }
            else
            {
                // The units left the pool other than by a pool matched disposal (e.g. gift to partner, pool cleared
                // by a corporate action): their share cannot uplift a disposal computation and must not inflate the
                // cost of the retained units either, so it is only recorded.
                unappliedAmount += removedUnitsShare;
            }
            remainingPeriodEndUnits -= periodEndUnitsRemoved;
        }

        if (remainingPeriodEndUnits > 0)
        {
            WrappedMoney poolAdjustment = adjustmentPerUnit * remainingPeriodEndUnits;
            if (section104.Quantity > 0)
            {
                string poolExplanation = remainingPeriodEndUnits == quantityAtPeriodEnd
                    ? description
                    : $"{description} - {poolAdjustment} apportioned to the {remainingPeriodEndUnits:0.####} unit(s) retained after gap period disposal(s)";
                section104.AdjustAcquisitionCost(poolAdjustment, adjustmentDate, poolExplanation);
            }
            else
            {
                unappliedAmount += poolAdjustment;
            }
        }
        if (unappliedAmount.Amount != 0)
        {
            // Record without changing the pool cost: applying it would distort the cost of units that never carried
            // the entitlement (or of future unrelated acquisitions when the pool is empty).
            section104.AdjustAcquisitionCost(WrappedMoney.GetBaseCurrencyZero(), adjustmentDate,
                $"{description} - {unappliedAmount} attributable to unit(s) that left the section 104 pool other than by a matched disposal is not applied.");
        }
    }
}
