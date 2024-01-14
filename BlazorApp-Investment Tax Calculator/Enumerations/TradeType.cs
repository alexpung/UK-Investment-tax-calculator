using System.ComponentModel;

namespace Enumerations;

public enum TradeType
{
    [Description("Acqusition")]
    BUY,
    [Description("Disposal")]
    SELL
}
