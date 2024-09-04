using System.ComponentModel;

namespace InvestmentTaxCalculator.Enumerations;

/// <summary>
/// Should not add additional type
/// </summary>
public enum TradeType
{
    [Description("Acquisition")]
    ACQUISITION,
    [Description("Disposal")]
    DISPOSAL
}
