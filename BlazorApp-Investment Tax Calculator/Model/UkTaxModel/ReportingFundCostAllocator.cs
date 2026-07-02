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
        WrappedMoney adjustmentPerUnit = adjustmentAmount / quantityAtPeriodEnd;
        decimal remainingPeriodEndUnits = quantityAtPeriodEnd;
        List<Section104History> gapPeriodPoolDisposals = [.. section104.Section104HistoryList
            .Where(history => DateOnly.FromDateTime(history.Date) > reportingPeriodEnd
                              && history.QuantityChange < 0
                              && history.TradeTaxCalculation is { AcquisitionDisposal: TradeType.DISPOSAL })];
        foreach (Section104History history in gapPeriodPoolDisposals)
        {
            if (remainingPeriodEndUnits <= 0) break;
            // The section 104 pool is fungible, so a pool disposal is treated as disposing of period end units in
            // proportion to their share of the pool immediately before the disposal (just and reasonable apportionment).
            decimal disposedQuantity = -history.QuantityChange;
            decimal periodEndUnitsDisposed = history.OldQuantity > 0
                ? Math.Min(remainingPeriodEndUnits, disposedQuantity * remainingPeriodEndUnits / history.OldQuantity)
                : 0m;
            if (periodEndUnitsDisposed <= 0) continue;
            TradeMatch? match = history.TradeTaxCalculation!.MatchHistory.FirstOrDefault(m => m.Section104HistorySnapshot == history);
            // If the disposal match cannot be located the unallocated share stays with the pool remainder so no amount is lost.
            if (match is null) continue;
            WrappedMoney matchAdjustment = adjustmentPerUnit * periodEndUnitsDisposed;
            match.BaseCurrencyMatchAllowableCost += matchAdjustment;
            match.AdditionalInformation += $"{description}: allowable cost adjusted by {matchAdjustment} for {periodEndUnitsDisposed:0.####} unit(s) " +
                $"held at the reporting period end {reportingPeriodEnd:d} and disposed of before the fund distribution date " +
                $"(treated as received immediately before the disposal, SI 2009/3001 reg. 99(5)).\n";
            remainingPeriodEndUnits -= periodEndUnitsDisposed;
        }
        if (remainingPeriodEndUnits <= 0) return;
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
            // The pool is empty and no disposal can absorb the remainder (e.g. the units left the pool other than by
            // disposal). Applying it would distort the cost of future unrelated acquisitions, so only record it.
            section104.AdjustAcquisitionCost(WrappedMoney.GetBaseCurrencyZero(), adjustmentDate,
                $"{description} - {poolAdjustment} for {remainingPeriodEndUnits:0.####} unit(s) not applied: the section 104 pool is empty and no matching disposal was found.");
        }
    }
}
