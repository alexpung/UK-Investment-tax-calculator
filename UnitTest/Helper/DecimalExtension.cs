using Model;
using NMoneys;

namespace UnitTest.Helper;

public static class DecimalExtension
{
    public static Money ConvertToBaseCurrency(this decimal amount)
    {
        return BaseCurrencyMoney.BaseCurrencyAmount(amount);
    }
}
