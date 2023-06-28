using NMoneys;

namespace Model;

public record DescribedMoney
{
    public string Description { get; set; } = "";
    public required Money Amount { get; set; }
    public decimal FxRate { get; set; } = 1;

    public Money BaseCurrencyAmount => BaseCurrencyMoney.BaseCurrencyAmount(Amount.Amount * FxRate);
}

public static class DescribedMoneyExtension
{
    public static Money BaseCurrencySum(this IEnumerable<Money> moneys)
    {
        if (!moneys.Any()) return BaseCurrencyMoney.BaseCurrencyZero;
        return Money.Total(moneys);
    }

    /// <summary>
    /// Sum up an IEnumerable of objects that contain Money object. Default return Money of Amount 0 in base currency.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="moneys"></param>
    /// <param name="selector">Delegate to select money object</param>
    /// <returns></returns>
    public static Money BaseCurrencySum<T>(this IEnumerable<T> moneys, Func<T, Money> selector)
    {
        if (!moneys.Any()) return BaseCurrencyMoney.BaseCurrencyZero;
        return Money.Total(moneys.Select(selector));
    }
}