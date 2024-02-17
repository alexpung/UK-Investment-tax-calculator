using System.ComponentModel;

namespace Enumerations;

public enum DividendType
{
    [Description("Withholding Tax")]
    WITHHOLDING,
    [Description("Dividend")]
    DIVIDEND,
    [Description("Payment in lieu of dividend")]
    DIVIDEND_IN_LIEU,
    [Description("Not a dividend")]
    NOT_DIVIDEND
}
