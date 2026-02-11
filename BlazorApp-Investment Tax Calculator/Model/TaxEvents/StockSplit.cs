using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using System.Text.Json.Serialization;

namespace InvestmentTaxCalculator.Model.TaxEvents;

public record StockSplit : CorporateAction, IChangeSection104
{
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
        if (AssetName != trade1.AssetName || AssetName != trade2.AssetName) return matchAdjustment;
        if (earlierTrade.Date > Date || Date > laterTrade.Date) return matchAdjustment;
        matchAdjustment.MatchAdjustmentFactor *= (decimal)SplitTo / SplitFrom;
        matchAdjustment.CorporateActions.Add(this);
        return matchAdjustment;
    }

    public override void ChangeSection104(UkSection104 section104)
    {
        if (AssetName != section104.AssetName) return;

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

            if (fractionalRemoved > 0)
            {
                // Process the cash-in-lieu tax effect (part-disposal)
                WrappedMoney cashCostUsed = ProcessCashInLieu(oldCost, rawNewQuantity, fractionalRemoved, section104);

                // Adjust S104 for the removal of fractional shares and the cost allocation
                // Following user's advice: use AddAssets/RemoveAssets (or similar) to adjust diff.
                // We basically want to remove fractionalRemoved quantity and its corresponding cost bits.
                
                // 1. Remove the quantity (this also removes the proportion of cost based on rawNewQuantity)
                string roundingExplanation = $"Fractional shares ({fractionalRemoved:F4}) removed for cash-in-lieu";
                section104.RemoveAssets(CashDisposal!, fractionalRemoved);
                
                // 2. Adjust the pool cost if we deferred some of the cost of the removed fractions
                WrappedMoney fractionalCost = oldCost * (fractionalRemoved / rawNewQuantity);
                WrappedMoney costToAddBack = fractionalCost - cashCostUsed;
                
                if (costToAddBack.Amount > 0.00000001m)
                {
                    string deferralExplanation = $"Cost basis adjustment for deferred Gain/Loss on split rounding";
                    section104.AdjustAcquisitionCost(costToAddBack, Date, deferralExplanation);
                }
            }
        }
    }

    private WrappedMoney ProcessCashInLieu(WrappedMoney oldPoolCost, decimal rawNewQuantity, decimal fractionalRemoved, UkSection104 section104)
    {
        if (CashInLieu == null) return WrappedMoney.GetBaseCurrencyZero();

        WrappedMoney cashAmount = CashInLieu.BaseCurrencyAmount;
        // The cost basis relevant to the fractional removal
        WrappedMoney allocatedFractionalCost = oldPoolCost * (fractionalRemoved / rawNewQuantity);
        
        return ProcessCashResult(cashAmount, allocatedFractionalCost, cashAmount.Amount, fractionalRemoved, AssetName, section104);
    }

    public override string GetDuplicateSignature()
    {
        return $"STOCKSPLIT|{base.GetDuplicateSignature()}|{SplitTo}|{SplitFrom}";
    }
}
