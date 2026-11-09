using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace InvestmentTaxCalculator.Model.TaxEvents;

public record InterestIncome : TaxEvent, ITextFilePrintable, IEqualisableIncome
{
    private DescribedMoney _amount = null!;
    private EqualisationLinks? _equalisationLinks;

    /// <summary>
    /// Negative for accurred income loss and interest expense. Cannot be set below the fund equalisation entered
    /// against the income.
    /// </summary>
    public required DescribedMoney Amount
    {
        get => _amount;
        set
        {
            EqualisationLinks.EnsureCovered(value);
            _amount = value;
        }
    }

    public required InterestType InterestType { get; set; }

    public string IncomeId { get; set; } = Guid.NewGuid().ToString("N");

    [JsonIgnore]
    public DescribedMoney EqualisableAmount => Amount;

    [JsonIgnore]
    public EqualisationLinks EqualisationLinks => EqualisationLinks.For(this, ref _equalisationLinks);
    public CountryCode IncomeLocation { get; set; } = CountryCode.UnknownRegion;
    // In case of accrued income profit/loss, tax is deferred if next payment is in different tax year
    public bool IsNextPaymentInSameTaxYear { get; set; } = true;
    /// <summary>
    /// Interest paid (e.g. margin interest). Recorded for reference only: it is not deductible and excluded from all income totals.
    /// </summary>
    public bool IsInterestExpense => InterestType == InterestType.INTERESTEXPENSE;
    public bool IsTaxDeferred => (InterestType == InterestType.ACCURREDINCOMEPROFIT || InterestType == InterestType.ACCURREDINCOMELOSS) && !IsNextPaymentInSameTaxYear;
    public string PrintToTextFile()
    {
        return $"Asset Name: {AssetName}, " +
                $"Date: {Date.ToShortDateString()}, " +
                $"Type: {InterestType.GetDescription()}, " +
                $"Amount: {Amount.Amount}, " +
                $"FxRate: {Amount.FxRate}, " +
                $"Sterling Amount: {Amount.BaseCurrencyAmount}, " +
                $"Description: {Amount.Description}";
    }
    public override string GetDuplicateSignature()
    {
        return $"INT|{base.GetDuplicateSignature()}|{InterestType}|{Amount.Amount.Amount}|{Amount.Amount.Currency}";
    }

    public override string ToSummaryString() => $"Income: {InterestType} ({Date.ToShortDateString()}) - {Amount.Amount}";
}

public enum InterestType
{
    [Description("Saving Interest Income")]
    SAVINGS,
    [Description("Bond Coupon")]
    BOND,
    [Description("Accrued Income Profit")]
    ACCURREDINCOMEPROFIT,
    [Description("Accrued Income Loss")]
    ACCURREDINCOMELOSS,
    [Description("ETF dividend income")]
    ETFDIVIDEND,
    [Description("Excess Reportable Income (Interest)")]
    EXCESSREPORTABLEINCOME,
    [Description("Interest Expense (not deductible)")]
    INTERESTEXPENSE
}
