using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;

using System.Text.Json.Serialization;

namespace InvestmentTaxCalculator.Model.TaxEvents;

public record Dividend : TaxEvent, ITextFilePrintable, IEqualisableIncome
{
    private DividendType _dividendType;
    private DescribedMoney _proceed = null!;

    /// <summary>
    /// A dividend with fund equalisation attached cannot be changed to a type that cannot be equalised.
    /// </summary>
    public required DividendType DividendType
    {
        get => _dividendType;
        set
        {
            if (!IsEqualisable(value)) EqualisationLinks.EnsureNone();
            _dividendType = value;
        }
    }

    public CountryCode CompanyLocation { get; set; } = CountryCode.UnknownRegion;

    /// <summary>
    /// The amount. Cannot be set below the fund equalisation entered against the dividend.
    /// </summary>
    public required DescribedMoney Proceed
    {
        get => _proceed;
        set
        {
            EqualisationLinks.EnsureCovered(value);
            _proceed = value;
        }
    }

    public string IncomeId { get; set; } = Guid.NewGuid().ToString("N");

    [JsonIgnore]
    public DescribedMoney EqualisableAmount => Proceed;

    [JsonIgnore]
    public bool CanBeEqualised => IsEqualisable(DividendType);

    [JsonIgnore]
    public EqualisationLinks EqualisationLinks => EqualisationLinks.Of(this);

    // Withholding tax is not income, so it has no equalisation.
    private static bool IsEqualisable(DividendType type) => type is not DividendType.WITHHOLDING;

    public WrappedMoney WithholdingTaxPaid => DividendType is DividendType.WITHHOLDING ? Proceed.BaseCurrencyAmount : WrappedMoney.GetBaseCurrencyZero();
    public WrappedMoney DividendReceived => DividendType is DividendType.DIVIDEND_IN_LIEU or DividendType.DIVIDEND or DividendType.EXCESS_REPORTABLE_INCOME or DividendType.RETURN_OF_CAPITAL ? Proceed.BaseCurrencyAmount : WrappedMoney.GetBaseCurrencyZero();

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
    public override string ToSummaryString() => $"Dividend: {AssetName} ({Date.ToShortDateString()}) - {Proceed.Amount}";
}
