using InvestmentTaxCalculator.Enumerations;
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

    /// <summary>
    /// The last day of the fund reporting period the related income event covers. When set, the cost reduction is
    /// apportioned between units disposed of after this date but before the distribution date (adjusting those
    /// disposals) and the retained section 104 pool. When null (older saved files or non-reporting fund income)
    /// the whole amount reduces the pool on the distribution date, as before.
    /// </summary>
    public DateTime? ReportingPeriodEndDate { get; init; }

    public override string Reason => $"{AssetName} fund equalisation of {Amount.BaseCurrencyAmount} on {Date:d}" + (string.IsNullOrEmpty(RelatedEventDescription) ? "" : $" ({RelatedEventDescription})");

    public override AssetCategoryType AppliesToAssetCategoryType { get; } = AssetCategoryType.STOCK;

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
        if (ReportingPeriodEndDate is null)
        {
            section104.AdjustAcquisitionCost(-Amount.BaseCurrencyAmount, Date, explanation);
            return;
        }
        // Units disposed of between the reporting period end and the distribution date take their share of the
        // reduction at the disposal; only the retained units' share reduces the pool.
        ReportingFundCostAllocator.Apply(section104, DateOnly.FromDateTime(ReportingPeriodEndDate.Value), Date, -Amount.BaseCurrencyAmount, explanation);
    }
    public override string GetDuplicateSignature()
    {
        return $"EQUALISATION|{base.GetDuplicateSignature()}|{Amount.Amount.Amount}|{Amount.Amount.Currency}|{RelatedEventDescription}";
    }
}
