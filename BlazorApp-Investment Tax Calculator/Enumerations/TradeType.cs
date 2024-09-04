using System.ComponentModel;

namespace Enumerations;

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
