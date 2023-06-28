using NMoneys;

namespace Model;

public static class BaseCurrencyMoney
{
    public static Currency BaseCurrency { get; set; } = Currency.Gbp;
    public static Money BaseCurrencyZero => Money.Zero(BaseCurrency);
    public static Money BaseCurrencyAmount(decimal amount) => new(amount, BaseCurrency);

    public static Money Multiply(this Money money, decimal factor)
    {
        return new Money(money.Amount * factor, money.CurrencyCode);
    }

    public static Money Divide(this Money money, decimal factor)
    {
        return new Money(money.Amount / factor, money.CurrencyCode);
    }

    public static Money Sum(this IEnumerable<Money> money)
    {
        if (!money.Any()) return BaseCurrencyZero;
        else return Money.Total(money);
    }
}
