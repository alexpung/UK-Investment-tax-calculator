using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;

namespace InvestmentTaxCalculator.Model.TaxEvents;

public record StockSplit : CorporateAction, IChangeSection104
{
    private const decimal FractionalTolerance = 0.00000001m;

    /// <summary>
    /// The number of shares after the split that are being given for the original number of shares.
    /// For example, in a 2:1 stock split, where 2 shares are given for every 1 share, this is 2.
    /// </summary>
    public required int SplitTo { get; init; }
    /// <summary>
    /// The number of shares prior to the split. 
    /// For example, in a 2:1 stock split, where 2 shares are given for every 1 share, this is 1.
    /// </summary>
    public required int SplitFrom { get; init; }

    /// <summary>
    /// Optional cash received in lieu of fractional shares (common in reverse splits)
    /// </summary>
    public DescribedMoney? CashInLieu { get; init; }

    /// <summary>
    /// If true and cash-in-lieu is "small" (under £3,000 or 5% of total value),
    /// elect to defer capital gains by reducing cost basis instead of recognizing a gain.
    /// This follows TCGA 1992 s122.
    /// </summary>
    public override bool ElectTaxDeferral { get; init; } = true;

    public override string Reason => CashInLieu != null
        ? $"{AssetName} undergoes a stock split {SplitTo} for {SplitFrom} with cash-in-lieu {CashInLieu.BaseCurrencyAmount} on {Date:d}"
        : $"{AssetName} undergoes a stock split {SplitTo} for {SplitFrom} on {Date:d}";

    public override AssetCategoryType AppliesToAssetCategoryType { get; } = AssetCategoryType.STOCK;

    public override MatchAdjustment TradeMatching(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, MatchAdjustment matchAdjustment)
    {
        ITradeTaxCalculation earlierTrade = trade1.Date <= trade2.Date ? trade1 : trade2;
        ITradeTaxCalculation laterTrade = trade1.Date > trade2.Date ? trade1 : trade2;

        DateOnly splitDate = EffectiveDate;
        DateOnly earlierTradeDate = DateOnly.FromDateTime(earlierTrade.Date);
        DateOnly laterTradeDate = DateOnly.FromDateTime(laterTrade.Date);

        if (AssetName != trade1.AssetName || AssetName != trade2.AssetName) return matchAdjustment;
        if (!(earlierTradeDate < splitDate && splitDate <= laterTradeDate)) return matchAdjustment;
        matchAdjustment.MatchAdjustmentFactor *= (decimal)SplitTo / SplitFrom;
        matchAdjustment.CorporateActions.Add(this);
        return matchAdjustment;
    }

    public override void ChangeSection104(UkSection104 section104)
    {
        if (AssetName != section104.AssetName) return;

        // Ensure per-run state is reset before processing.
        CashDisposal = null;

        decimal oldQuantity = section104.Quantity;
        WrappedMoney oldCost = section104.AcquisitionCostInBaseCurrency;

        if (oldQuantity == 0) return;

        decimal factor = SplitTo / (decimal)SplitFrom;
        decimal rawNewQuantity = oldQuantity * factor;

        // Apply basic split multiplier (decimal-preserving)
        string baseExplanation = $"Stock split {SplitTo} for {SplitFrom} on {Date:d}";
        section104.MultiplyQuantity(factor, Date, baseExplanation);

        if (CashInLieu != null)
        {
            // Process cash-in-lieu and rounding
            decimal roundedQuantity = Math.Floor(rawNewQuantity);
            decimal fractionalRemoved = rawNewQuantity - roundedQuantity;
            if (fractionalRemoved <= FractionalTolerance)
            {
                throw new InvalidOperationException(
                    $"Cash-in-lieu provided for {AssetName} stock split {SplitTo} for {SplitFrom} on {Date:d}, " +
                    $"but no fractional shares were produced from quantity {oldQuantity}.");
            }

            // This will setup the CashDisposal property if applicable (deferral not elected or gain exists)
            WrappedMoney cashCostUsed = ProcessCashInLieu(oldCost, rawNewQuantity, fractionalRemoved, section104);

            // Following user's advice: use AddAssets/RemoveAssets (or similar) to adjust diff.
            // We basically want to remove fractionalRemoved quantity and its corresponding cost bits.

            if (CashDisposal != null)
            {
                // 1. Remove the quantity (this also removes the proportion of cost based on rawNewQuantity)
                string roundingExplanation = $"Fractional shares ({fractionalRemoved:F4}) removed for cash-in-lieu";
                var removalResults = section104.RemoveAssets(CashDisposal!, fractionalRemoved);
                foreach (var removal in removalResults)
                {
                    string taxableStatus = removal.IsTaxable == TaxableStatus.TAXABLE
                        ? "Taxable cash disposal recognized."
                        : "Cash disposal not taxable due to residency status.";
                    removal.Section104HistoryResult.Explanation =
                        $"{roundingExplanation}. Cash-in-lieu received: {CashInLieu.BaseCurrencyAmount}. " +
                        $"Allowable cost used: {cashCostUsed}. {taxableStatus}";
                }

                // 2. Adjust the pool cost if we deferred some of the cost of the removed fractions
                // RemoveAssets removes proportional cost: Cost * (QtyRemoved / TotalQty)
                // We want to remove `cashCostUsed`.
                // So we add back (Proportional - ActualUsed).

                // Note: In StockSplit, we are removing from NEW quantity basis?
                // Actually, RemoveAssets removes from *current* pool state.
                // The pool currently has `rawNewQuantity`.
                WrappedMoney proportionalCostRemoved = oldCost * (fractionalRemoved / rawNewQuantity);
                WrappedMoney costToAddBack = proportionalCostRemoved - cashCostUsed;

                if (Math.Abs(costToAddBack.Amount) > 0.00000001m)
                {
                    string deferralExplanation = "Cost basis alignment adjustment after fractional-share cash disposal";
                    section104.AdjustAcquisitionCost(costToAddBack, Date, deferralExplanation);
                }
            }
            else
            {
                // Deferral Case (s122 small distribution, no excess gain).
                // No disposal created. Check s122 logic: "No disposal is treated as occurring"
                // But we must reduce the allowable cost by the amount of the distribution.
                // And we must reduce the quantity (the fractional shares are gone).

                string deferralExplanation = $"Small cash distribution (s122) deferral: {fractionalRemoved:F4} shares removed, cost reduced by {cashCostUsed.Amount}";

                // We use AddAssets with negative values to reduce quantity and cost without triggering a disposal calculation.
                // Check if cashCostUsed is > 0? It should be the cash amount (up to cost).
                section104.AddAssets(Date, -fractionalRemoved, -cashCostUsed, null, deferralExplanation);
            }
        }
    }

    private WrappedMoney ProcessCashInLieu(WrappedMoney oldPoolCost, decimal rawNewQuantity, decimal fractionalRemoved, UkSection104 section104)
    {
        if (CashInLieu == null || fractionalRemoved == 0) return WrappedMoney.GetBaseCurrencyZero();

        WrappedMoney cashAmount = CashInLieu.BaseCurrencyAmount;

        // Extrapolate the total market value of the holding based on the cash-in-lieu rate.
        decimal totalValue = (cashAmount.Amount / fractionalRemoved) * rawNewQuantity;

        // Strictly proportional cost for the fractional shares removed.
        WrappedMoney fractionalCost = oldPoolCost * (fractionalRemoved / rawNewQuantity);

        bool isSmall = UkTaxRules.IsSmallCash(cashAmount.Amount, totalValue);

        // Deferral logic applies if elected and small.
        // Losses are deferred (reducing pool cost) if elected, not recognized as disposals.
        if (ElectTaxDeferral && isSmall)
        {
            // Deferral path: Reduce pool cost by cash (capped at current pool cost).
            WrappedMoney allowableCostUsed = WrappedMoney.Min(cashAmount, oldPoolCost);

            // If cash exceeds the whole pool cost, the excess is a taxable gain (s122).
            WrappedMoney excessGain = cashAmount - allowableCostUsed;
            if (excessGain.Amount > 0)
            {
                string deferralGainDetail = $"Small cash treatment (deferred) from {AssetName}: \n" +
                                            $"\tCash Received: {cashAmount}\n" +
                                            $"\tPool Cost Available: {oldPoolCost}\n" +
                                            $"\tExcess Gain: {excessGain} (taxable)\n" +
                                            $"\tCost reduced by max available: {allowableCostUsed}";

                CreateCashDisposal(excessGain, WrappedMoney.GetBaseCurrencyZero(), fractionalRemoved, deferralGainDetail, section104);
            }

            return allowableCostUsed;
        }
        else
        {
            // Recognition path: Generate a disposal record (Gain or Loss).

            string calculationDetail = cashAmount.Amount < fractionalCost.Amount
                ? $"Recognized loss on fractional share from {AssetName}:\n" +
                  $"\tCash: {cashAmount}\n" +
                  $"\tAllocated Cost (Quantity Proportion): {oldPoolCost} * ({fractionalRemoved:F4} / {rawNewQuantity:F4}) = {fractionalCost}"
                : $"Part-disposal of fractional share from {AssetName}:\n" +
                  $"\tCash: {cashAmount}\n" +
                  $"\tAllocated Cost (Quantity Proportion): {oldPoolCost} * ({fractionalRemoved:F4} / {rawNewQuantity:F4}) = {fractionalCost}";

            CreateCashDisposal(cashAmount, fractionalCost, fractionalRemoved, calculationDetail, section104);
            return fractionalCost;
        }
    }

    public override string GetDuplicateSignature()
    {
        return $"STOCKSPLIT|{base.GetDuplicateSignature()}|{SplitTo}|{SplitFrom}";
    }
}
