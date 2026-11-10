using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;

using System.Text.Json.Serialization;

namespace InvestmentTaxCalculator.Model.TaxEvents;

/// <summary>
/// Represents Fund Equalisation from ETFs/funds that decreases the Section104 acquisition cost.
/// This is treated as a return of capital and reduces the cost base of the holding, and the same amount is excluded
/// from the taxable income it was paid with.
/// <para>
/// Each entry is linked both ways to the income it belongs to (<see cref="LinkedIncome"/> and the income's
/// <see cref="IEqualisableIncome.EqualisationLinks"/>), so its asset and date always follow that income. The income
/// itself is never reduced in storage: <see cref="ReportingFundIncomeResolver"/> deducts the equalisation from a copy
/// at calculation time.
/// </para>
/// </summary>
public record FundEqualisation : CorporateAction, IChangeSection104
{
    private string? _incomeId;

    /// <summary>
    /// The amount of equalisation received, in the linked income's currency and FX rate.
    /// </summary>
    public required DescribedMoney Amount { get; init; }

    /// <summary>
    /// Description of the income event (Dividend/ERI) this equalisation was part of, for display.
    /// </summary>
    public string? RelatedEventDescription { get; init; }

    /// <summary>
    /// The <see cref="IEqualisableIncome.IncomeId"/> of the linked income. Saved so the link can be restored on
    /// import (see <see cref="TaxEventLists.LinkEqualisations"/>).
    /// </summary>
    public string? IncomeId
    {
        get => LinkedIncome?.IncomeId ?? _incomeId;
        set => _incomeId = value;
    }

    /// <summary>
    /// The income this equalisation belongs to. Set through <see cref="LinkTo"/> and <see cref="Unlink"/>, which
    /// keep the income's side of the link in step.
    /// </summary>
    [JsonIgnore]
    public IEqualisableIncome? LinkedIncome { get; private set; }

    /// <summary>
    /// The asset of the linked income: an equalisation always belongs to the share its income was paid on.
    /// </summary>
    public override required string AssetName
    {
        get => (LinkedIncome as TaxEvent)?.AssetName ?? base.AssetName;
        set => base.AssetName = value;
    }

    /// <summary>
    /// The date of the linked income: the equalisation is received with the distribution.
    /// </summary>
    public override required DateTime Date
    {
        get => (LinkedIncome as TaxEvent)?.Date ?? base.Date;
        set => base.Date = value;
    }

    /// <summary>
    /// Attaches this equalisation to <paramref name="income"/> (moving it from any previous income). Throws
    /// <see cref="InvalidOperationException"/>, leaving the links unchanged, if the income's equalisation would
    /// then exceed it.
    /// </summary>
    public void LinkTo(IEqualisableIncome income)
    {
        if (ReferenceEquals(LinkedIncome, income)) return;
        income.EqualisationLinks.Attach(this);
        LinkedIncome?.EqualisationLinks.Detach(this);
        LinkedIncome = income;
    }

    /// <summary>
    /// Detaches this equalisation from its income, keeping the income's id, asset and date as they were.
    /// </summary>
    public void Unlink()
    {
        if (LinkedIncome is null) return;
        _incomeId = LinkedIncome.IncomeId;
        base.AssetName = AssetName;
        base.Date = Date;
        LinkedIncome.EqualisationLinks.Detach(this);
        LinkedIncome = null;
    }

    /// <summary>
    /// The reporting period end used to apportion the cost reduction: that of the linked ERI entry, or null (a
    /// distribution) to reduce the pool as a whole.
    /// </summary>
    [JsonIgnore]
    public DateOnly? ReportingPeriodEndDate => (LinkedIncome as ExcessReportableIncome)?.EffectiveReportingPeriodEndDate;

    public override string Reason => $"{AssetName} fund equalisation of {Amount.BaseCurrencyAmount} on {Date:d}" + (string.IsNullOrEmpty(RelatedEventDescription) ? "" : $" ({RelatedEventDescription})");

    public override AssetCategoryType AppliesToAssetCategoryType { get; } = AssetCategoryType.STOCK;

    public override MatchAdjustment TradeMatching(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, MatchAdjustment matchAdjustment)
    {
        // Fund equalisation doesn't affect trade matching
        return matchAdjustment;
    }

    /// <summary>
    /// Reduces the cost of the holding by the equalisation amount: apportioned between gap period disposals and the
    /// pool for ERI equalisation, otherwise taken off the pool on the date.
    /// </summary>
    public override void ChangeSection104(UkSection104 section104)
    {
        if (!section104.MatchesAsset(AssetName)) return;
        string explanation = $"Fund equalisation of {Amount.BaseCurrencyAmount} on {Date:d}";
        if (!string.IsNullOrEmpty(RelatedEventDescription))
        {
            explanation += $" ({RelatedEventDescription})";
        }
        // Equalisation reduces the cost base, so we pass a negative adjustment
        if (ReportingPeriodEndDate is not { } reportingPeriodEnd)
        {
            section104.AdjustAcquisitionCost(-Amount.BaseCurrencyAmount, Date, explanation);
            return;
        }
        // Units disposed of between the reporting period end and the distribution date take their share of the
        // reduction at the disposal; only the retained units' share reduces the pool.
        ReportingFundCostAllocator.Apply(section104, reportingPeriodEnd, Date, -Amount.BaseCurrencyAmount, explanation);
    }

    /// <summary>
    /// The warning to show when deleting an income also deleted these equalisation entries, or null if there were none.
    /// </summary>
    public static string? DescribeRemovedWithIncome(IReadOnlyList<FundEqualisation> removed) => removed.Count == 0
        ? null
        : $"Also deleted {removed.Count} fund equalisation entr{(removed.Count == 1 ? "y" : "ies")} that reduced the deleted income: " +
          string.Join("; ", removed.Select(e => e.Reason)) + ".";

    public override string GetDuplicateSignature()
    {
        return $"EQUALISATION|{base.GetDuplicateSignature()}|{Amount.Amount.Amount}|{Amount.Amount.Currency}|{RelatedEventDescription}";
    }
}
