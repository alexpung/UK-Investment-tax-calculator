using Model;

namespace Services;

public static class ObjectDetailsToPrintedString
{
    public static string ToSignedNumberString(this decimal decimalNumber)
    {
        string sign = string.Empty;
        if (decimalNumber >= 0) sign = "+";
        return sign + decimalNumber.ToString("0.##");
    }

    public static string ToSignedNumberString(this WrappedMoney money)
    {
        string sign = string.Empty;
        if (money.Amount >= 0) sign = "+";
        return sign + money.ToString();
    }
}
