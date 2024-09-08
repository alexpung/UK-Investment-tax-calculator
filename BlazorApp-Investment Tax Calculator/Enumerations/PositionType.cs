using System.ComponentModel;

namespace InvestmentTaxCalculator.Enumerations;

public enum PositionType
{
    [Description("Open long position")]
    OPENLONG,
    [Description("Open short position")]
    OPENSHORT,
    [Description("Close long position")]
    CLOSELONG,
    [Description("Close short position")]
    CLOSESHORT
}
