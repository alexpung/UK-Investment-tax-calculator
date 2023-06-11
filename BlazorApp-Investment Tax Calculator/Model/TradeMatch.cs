using Model.Interfaces;

namespace Model;

/// <summary>
/// Data class to provide sufficient information to describe a matching of a trade pair and calculate taxable gain/loss
/// </summary>
public record TradeMatch
{
    public required System.Enum TradeMatchType { get; set; }
    public ITradeTaxCalculation? MatchedGroup { get; set; }
    public decimal MatchQuantity { get; set; } = 0m;
    public decimal BaseCurrencyMatchDisposalValue { get; set; } = 0m;
    public decimal BaseCurrencyMatchAcquitionValue { get; set; } = 0m;
    public string AdditionalInformation { get; set; } = string.Empty;

}
