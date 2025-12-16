using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;

using System.ComponentModel;

namespace InvestmentTaxCalculator.Model.TaxEvents;

public record InterestIncome : TaxEvent, ITextFilePrintable
{
    /// <summary>
    /// Negative for accurred income loss
    /// </summary>
    public required DescribedMoney Amount { get; init; }
    public required InterestType InterestType { get; init; }
    public CountryCode IncomeLocation { get; set; } = CountryCode.UnknownRegion;
    // In case of accrued income profit/loss, tax is deferred if next payment is in different tax year
    public bool IsNextPaymentInSameTaxYear { get; set; } = true;
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
    ETFDIVIDEND
}
