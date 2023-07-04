using NMoneys;

namespace Model;
public static class MoneyExtension
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
