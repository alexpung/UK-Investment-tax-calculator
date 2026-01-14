using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;

namespace InvestmentTaxCalculator.Model.TaxEvents;

/// <summary>
/// Represents Fund Equalisation from ETFs/funds that decreases the Section104 acquisition cost.
/// This is treated as a return of capital and reduces the cost base of the holding.
/// </summary>
public record FundEqualisation : CorporateAction, IChangeSection104
{
    /// <summary>
    /// The amount of equalisation received
    /// </summary>
    public required DescribedMoney Amount { get; init; }

    /// <summary>
    /// Optional reference to the income event (Dividend/ERI) that this equalisation was part of.
    /// </summary>
    public string? RelatedEventDescription { get; init; }

    public override string Reason => $"{AssetName} fund equalisation of {Amount.BaseCurrencyAmount} on {Date:d}" + (string.IsNullOrEmpty(RelatedEventDescription) ? "" : $" ({RelatedEventDescription})") + "\n";

    public override MatchAdjustment TradeMatching(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, MatchAdjustment matchAdjustment)
    {
        // Fund equalisation doesn't affect trade matching
        return matchAdjustment;
    }

    public override void ChangeSection104(UkSection104 section104)
    {
        if (AssetName != section104.AssetName) return;
        string explanation = $"Fund equalisation of {Amount.BaseCurrencyAmount} on {Date:d}";
        if (!string.IsNullOrEmpty(RelatedEventDescription))
        {
            explanation += $" ({RelatedEventDescription})";
        }
        // Equalisation reduces the cost base, so we pass a negative adjustment
        section104.AdjustAcquisitionCost(-Amount.BaseCurrencyAmount, Date, explanation);
    }
}
