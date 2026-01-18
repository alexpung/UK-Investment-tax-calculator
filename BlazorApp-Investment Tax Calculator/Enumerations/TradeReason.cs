using System.ComponentModel;

namespace InvestmentTaxCalculator.Enumerations;

public enum TradeReason
{
    [Description("Option exercised")]
    OwnerExerciseOption,
    [Description("Option assigned")]
    OptionAssigned,
    [Description("Option expired")]
    Expired,
    [Description("Ordered trade")]
    OrderedTrade
}
