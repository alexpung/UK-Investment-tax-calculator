using Model;
using NMoneys;

namespace UnitTest;

public static class DecimalExtension
{
    public static Money ConvertToBaseCurrency(this decimal amount)
    {
        return BaseCurrencyMoney.BaseCurrencyAmount(amount);
    }
}
