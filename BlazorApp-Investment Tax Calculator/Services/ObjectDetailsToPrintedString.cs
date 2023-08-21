using NMoneys;

namespace Services;

public static class ObjectDetailsToPrintedString
{
    public static string ToSignedNumberString(this decimal decimalNumber)
    {
        string sign = string.Empty;
        if (decimalNumber >= 0) sign = "+";
        return sign + decimalNumber.ToString();
    }

    public static string ToSignedNumberString(this Money money)
    {
        string sign = string.Empty;
        if (money.Amount >= 0) sign = "+";
        return sign + money.ToString();
    }
}
