using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;

namespace InvestmentTaxCalculator.Model.TaxEvents;

public record Dividend : TaxEvent, ITextFilePrintable
{
    public required DividendType DividendType { get; set; }
    public CountryCode CompanyLocation { get; set; } = CountryCode.UnknownRegion;
    public required DescribedMoney Proceed { get; set; }
    public WrappedMoney WithholdingTaxPaid => DividendType is DividendType.WITHHOLDING ? Proceed.BaseCurrencyAmount : WrappedMoney.GetBaseCurrencyZero();
    public WrappedMoney DividendReceived => DividendType is DividendType.DIVIDEND_IN_LIEU or DividendType.DIVIDEND or DividendType.EXCESS_REPORTABLE_INCOME ? Proceed.BaseCurrencyAmount : WrappedMoney.GetBaseCurrencyZero();

    public string PrintToTextFile()
    {
        return $"Asset Name: {AssetName}, " +
                $"Date: {Date.ToShortDateString()}, " +
                $"Type: {DividendType.GetDescription()}, " +
                $"Amount: {Proceed.Amount}, " +
                $"FxRate: {Proceed.FxRate}, " +
                $"Sterling Amount: {Proceed.BaseCurrencyAmount}, " +
                $"Description: {Proceed.Description}";
    }
    public override string GetDuplicateSignature()
    {
        return $"DIV|{base.GetDuplicateSignature()}|{DividendType}|{Proceed.Amount.Amount}|{Proceed.Amount.Currency}";
    }
}
