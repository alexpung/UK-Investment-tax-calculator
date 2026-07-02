using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;

using System.ComponentModel;
using System.Text.Json.Serialization;

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

    /// <summary>
    /// The last day of the fund reporting period this ERI relates to. Liability is fixed by the holding at the end
    /// of this day (SI 2009/3001 reg. 94(3)). When absent (older saved files) it is assumed to be 6 months before
    /// the fund distribution date, mirroring reg. 94(4).
    /// </summary>
    public DateTime? ReportingPeriodEndDate { get; init; }

    [JsonIgnore]
    public DateOnly EffectiveReportingPeriodEndDate => DateOnly.FromDateTime(ReportingPeriodEndDate ?? AssumedReportingPeriodEnd());

    private DateTime AssumedReportingPeriodEnd()
    {
        DateTime assumed = Date.AddMonths(-6);
        // Reg. 94(4) maps a month end period end to a month end distribution date (e.g. 31 Dec -> 30 Jun), so a
        // month end distribution date is mapped back to the month end rather than AddMonths' clamped day.
        if (Date.Day == DateTime.DaysInMonth(Date.Year, Date.Month))
        {
            assumed = new DateTime(assumed.Year, assumed.Month, DateTime.DaysInMonth(assumed.Year, assumed.Month));
        }
        return assumed;
    }

    public override string Reason => $"{AssetName} excess reportable income ({IncomeType.GetDescription()}) of {Amount.BaseCurrencyAmount} on {Date:d}";
    public override AssetCategoryType AppliesToAssetCategoryType { get; } = AssetCategoryType.STOCK;

    public override MatchAdjustment TradeMatching(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, MatchAdjustment matchAdjustment)
    {
        // Excess reportable income doesn't affect trade matching
        return matchAdjustment;
    }

    public override void ChangeSection104(UkSection104 section104)
    {
        if (AssetName != section104.AssetName) return;
        string explanation = $"Excess reportable income ({IncomeType.GetDescription()}) of {Amount.BaseCurrencyAmount} on {Date:d}";
        // The uplift is apportioned per unit held at the reporting period end: units disposed of in the gap period
        // before the fund distribution date take their share at the disposal (reg. 99(5)), the rest goes to the pool.
        ReportingFundCostAllocator.Apply(section104, EffectiveReportingPeriodEndDate, Date, Amount.BaseCurrencyAmount, explanation);
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
