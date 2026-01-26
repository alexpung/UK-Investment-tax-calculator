using System.ComponentModel;

namespace InvestmentTaxCalculator.Enumerations;

public enum TaxMatchType
{
    [Description("Same day")]
    SAME_DAY,
    [Description("Bed and breakfast")]
    BED_AND_BREAKFAST,
    [Description("Section 104")]
    SECTION_104,
    [Description("Cover short selling")]
    SHORTCOVER,
    [Description("Corporate action")]
    CORPORATE_ACTION
}
