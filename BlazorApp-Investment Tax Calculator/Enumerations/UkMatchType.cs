using System.ComponentModel;

namespace Enumerations;

public enum TaxMatchType
{
    [Description("Same day")]
    SAME_DAY,
    [Description("Bed and breakfast")]
    BED_AND_BREAKFAST,
    [Description("Section 104")]
    SECTION_104,
    [Description("Cover short selling")]
    SHORTCOVER
}
