using System.ComponentModel;

namespace InvestmentTaxCalculator.Enumerations;

public enum AssetCatagoryType
{
    [Description("Stock")]
    STOCK,
    [Description("Future contract")]
    FUTURE,
    [Description("Foreign currency")]
    FX
}
