using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;

using System.ComponentModel;

namespace InvestmentTaxCalculator.Model.TaxEvents;

/// <summary>
/// Represents Excess Reportable Income from ETFs/funds that adjusts the Section104 acquisition cost.
/// This is treated as either dividend or interest income depending on the nature of the distribution,
/// and increases the cost base of the holding to avoid double taxation on disposal.
/// </summary>
public record ExcessReportableIncome : CorporateAction, IChangeSection104
{
    /// <summary>
    /// The amount of excess reportable income received
    /// </summary>
    public required DescribedMoney Amount { get; init; }

    /// <summary>
    /// Whether this is treated as dividend or interest income for tax purposes
    /// </summary>
    public required ExcessReportableIncomeType IncomeType { get; init; }

    /// <summary>
    /// The country where the income is sourced from
    /// </summary>
    public CountryCode IncomeLocation { get; set; } = CountryCode.UnknownRegion;

    public override string Reason => $"{AssetName} excess reportable income ({IncomeType.GetDescription()}) of {Amount.BaseCurrencyAmount} on {Date:d}";

    public override MatchAdjustment TradeMatching(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, MatchAdjustment matchAdjustment)
    {
        // Excess reportable income doesn't affect trade matching
        return matchAdjustment;
    }

    public override void ChangeSection104(UkSection104 section104)
    {
        if (AssetName != section104.AssetName) return;
        string explanation = $"Excess reportable income ({IncomeType.GetDescription()}) of {Amount.BaseCurrencyAmount} on {Date:d}";
        section104.AdjustAcquisitionCost(Amount.BaseCurrencyAmount, Date, explanation);
    }
    public override string GetDuplicateSignature()
    {
        return $"ERI|{base.GetDuplicateSignature()}|{Amount.Amount.Amount}|{Amount.Amount.Currency}|{IncomeType}";
    }
}

public enum ExcessReportableIncomeType
{
    [Description("Dividend")]
    DIVIDEND,
    [Description("Interest")]
    INTEREST
}
