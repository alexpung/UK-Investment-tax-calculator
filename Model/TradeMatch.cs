using CapitalGainCalculator.Model.Interfaces;
using System.Collections.Generic;

namespace CapitalGainCalculator.Model;

/// <summary>
/// Data class to provide sufficient information to describe a matching of a trade pair and calculate taxable gain/loss
/// </summary>
public record TradeMatch
{
    public System.Enum? TradeMatchType { get; set; }
    public List<ITradeTaxCalculation> MatchedGroups { get; set; } = new();

    public decimal MatchQuantity { get; set; } = 0m;

    public decimal BaseCurrencyMatchValue { get; set; } = 0m;

}
