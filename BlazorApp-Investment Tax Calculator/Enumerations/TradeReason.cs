using System.ComponentModel;

namespace InvestmentTaxCalculator.Enumerations;

public enum TradeReason
{
    [Description("Owner exercised the option")]
    OwnerExerciseOption,
    [Description("Option assigned to the owner")]
    OptionAssigned,
    [Description("Option expired")]
    Expired,
    [Description("Ordered trade")]
    OrderedTrade
}
