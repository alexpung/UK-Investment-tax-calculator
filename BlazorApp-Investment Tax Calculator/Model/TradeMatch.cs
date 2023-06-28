using Model.Interfaces;
using NMoneys;

namespace Model;

/// <summary>
/// Data class to provide sufficient information to describe a matching of a trade pair and calculate taxable gain/loss
/// </summary>
public record TradeMatch
{
    public required TaxMatchType TradeMatchType { get; set; }
    public ITradeTaxCalculation? MatchedGroup { get; set; }
    public decimal MatchQuantity { get; set; } = 0m;
    public Money BaseCurrencyMatchDisposalValue { get; set; } = BaseCurrencyMoney.BaseCurrencyZero;
    public Money BaseCurrencyMatchAcquitionValue { get; set; } = BaseCurrencyMoney.BaseCurrencyZero;
    public string AdditionalInformation { get; set; } = string.Empty;
}
