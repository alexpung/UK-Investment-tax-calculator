using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace InvestmentTaxCalculator.Model.TaxEvents;

/// <summary>
/// Represents Fund Equalisation from ETFs/funds that decreases the Section104 acquisition cost.
/// This is treated as a return of capital and reduces the cost base of the holding, and the same amount is excluded
/// from the taxable income it was paid with.
/// <para>
/// The entry itself is never applied to other stored events: the income reduction is worked out at calculation time
/// by <see cref="ReportingFundIncomeResolver"/>, which finds the income by asset, date and <see cref="IncomeSource"/>.
/// </para>
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
    /// Which income the equalisation reduces: an ERI entry on <see cref="TaxEvent.Date"/> or an ordinary fund
    /// distribution (dividend or interest) paid on that date. Null only for entries saved before this was recorded;
    /// those are converted on import by <see cref="ReportingFundLegacyMigration"/>.
    /// </summary>
    public EqualisationIncomeSource? IncomeSource { get; init; }

    /// <summary>
    /// The ERI entry this equalisation reduces, resolved at the start of each calculation. Not saved.
    /// </summary>
    [JsonIgnore]
    public ExcessReportableIncome? LinkedExcessReportableIncome { get; set; }

    /// <summary>
    /// Only set on entries saved by older versions. Newer ERI equalisations take the reporting period end from the
    /// linked ERI entry, so editing the ERI keeps them in step.
    /// The last day of the fund reporting period the related income event covers. When set, the cost reduction is
    /// apportioned between units disposed of after this date but before the distribution date (adjusting those
    /// disposals) and the retained section 104 pool. When null (older saved files or non-reporting fund income)
    /// the whole amount reduces the pool on the distribution date, as before.
    /// </summary>
    public DateTime? ReportingPeriodEndDate { get; init; }

    /// <summary>
    /// The reporting period end used to apportion the cost reduction, or null to reduce the pool as a whole.
    /// </summary>
    [JsonIgnore]
    public DateOnly? EffectiveReportingPeriodEndDate
    {
        get
        {
            if (LinkedExcessReportableIncome is not null) return LinkedExcessReportableIncome.EffectiveReportingPeriodEndDate;
            if (ReportingPeriodEndDate is not null) return DateOnly.FromDateTime(ReportingPeriodEndDate.Value);
            if (IncomeSource == EqualisationIncomeSource.ExcessReportableIncome) return DateOnly.FromDateTime(ExcessReportableIncome.AssumedReportingPeriodEnd(Date));
            return null;
        }
    }

    public override string Reason => $"{AssetName} fund equalisation of {Amount.BaseCurrencyAmount} on {Date:d}" + (string.IsNullOrEmpty(RelatedEventDescription) ? "" : $" ({RelatedEventDescription})");

    public override AssetCategoryType AppliesToAssetCategoryType { get; } = AssetCategoryType.STOCK;

    public override MatchAdjustment TradeMatching(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, MatchAdjustment matchAdjustment)
    {
        // Fund equalisation doesn't affect trade matching
        return matchAdjustment;
    }

    public override void ChangeSection104(UkSection104 section104)
    {
        if (!section104.MatchesAsset(AssetName)) return;
        string explanation = $"Fund equalisation of {Amount.BaseCurrencyAmount} on {Date:d}";
        if (!string.IsNullOrEmpty(RelatedEventDescription))
        {
            explanation += $" ({RelatedEventDescription})";
        }
        // Equalisation reduces the cost base, so we pass a negative adjustment
        DateOnly? reportingPeriodEnd = EffectiveReportingPeriodEndDate;
        if (reportingPeriodEnd is null)
        {
            section104.AdjustAcquisitionCost(-Amount.BaseCurrencyAmount, Date, explanation);
            return;
        }
        // Units disposed of between the reporting period end and the distribution date take their share of the
        // reduction at the disposal; only the retained units' share reduces the pool.
        ReportingFundCostAllocator.Apply(section104, reportingPeriodEnd.Value, Date, -Amount.BaseCurrencyAmount, explanation);
    }
    /// <summary>
    /// Hand-written to leave out <see cref="LinkedExcessReportableIncome"/>, a reference re-resolved on every
    /// calculation, so equality (and removal from lists) does not depend on whether a calculation has run.
    /// </summary>
    public virtual bool Equals(FundEqualisation? other) =>
        base.Equals(other) &&
        Amount == other.Amount &&
        RelatedEventDescription == other.RelatedEventDescription &&
        IncomeSource == other.IncomeSource &&
        ReportingPeriodEndDate == other.ReportingPeriodEndDate;

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Amount, RelatedEventDescription, IncomeSource, ReportingPeriodEndDate);

    public override string GetDuplicateSignature()
    {
        return $"EQUALISATION|{base.GetDuplicateSignature()}|{Amount.Amount.Amount}|{Amount.Amount.Currency}|{RelatedEventDescription}";
    }
}

public enum EqualisationIncomeSource
{
    [Description("Fund distribution")]
    Distribution,
    [Description("Excess Reportable Income")]
    ExcessReportableIncome
}
