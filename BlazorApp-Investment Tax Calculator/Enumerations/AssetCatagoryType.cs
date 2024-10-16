using System.ComponentModel;

namespace InvestmentTaxCalculator.Enumerations;

public enum AssetCategoryType
{
    [Description("Stock")]
    STOCK,
    [Description("Future contract")]
    FUTURE,
    [Description("Foreign currency")]
    FX,
    [Description("Option")]
    OPTION
}
