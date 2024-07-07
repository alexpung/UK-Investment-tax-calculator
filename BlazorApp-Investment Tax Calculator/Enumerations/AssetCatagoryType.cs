using System.ComponentModel;

namespace Enumerations;

public enum AssetCatagoryType
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
