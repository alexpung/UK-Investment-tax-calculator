using NMoneys;

namespace Model;

[System.Diagnostics.DebuggerDisplay("{ToString()}")]
public record WrappedMoney : IComparable<WrappedMoney>
{
    private Money _nMoney;
    public const string BaseCurrency = "Gbp";
    public decimal Amount => _nMoney.Amount;
    public string Currency => _nMoney.CurrencyCode.ToString();

    private WrappedMoney(Money money)
    {
        _nMoney = money;
    }

    public WrappedMoney(decimal amount)
    {
        _nMoney = new Money(amount, BaseCurrency);
    }

    public WrappedMoney(decimal amount, string currency)
    {
        _nMoney = new Money(amount, currency);
    }

    public static WrappedMoney GetBaseCurrencyZero()
    {
        return new WrappedMoney(new Money(0, BaseCurrency));
    }

    public override string ToString()
    {
        return _nMoney.ToString();
    }

    public static WrappedMoney operator +(WrappedMoney money1, WrappedMoney money2)
    {
        return new WrappedMoney(money1._nMoney + money2._nMoney);
    }

    public static WrappedMoney operator -(WrappedMoney money1, WrappedMoney money2)
    {
        return new WrappedMoney(money1._nMoney - money2._nMoney);
    }

    public static WrappedMoney operator -(WrappedMoney money)
    {
        return new WrappedMoney(money._nMoney * -1);
    }

    public static WrappedMoney operator *(WrappedMoney money1, decimal multiplier)
    {
        Money newMoney = new(money1.Amount * multiplier, money1.Currency);
        return new WrappedMoney(newMoney);
    }

    public static WrappedMoney operator *(decimal multiplier, WrappedMoney money1)
    {
        Money newMoney = new(money1.Amount * multiplier, money1.Currency);
        return new WrappedMoney(newMoney);
    }

    public static WrappedMoney operator /(WrappedMoney money1, decimal divisor)
    {
        Money newMoney = new(money1.Amount / divisor, money1.Currency);
        return new WrappedMoney(newMoney);
    }

    public WrappedMoney Floor()
    {
        _nMoney = _nMoney.Floor();
        return this;
    }

    public WrappedMoney Ceiling()
    {
        _nMoney = _nMoney.Ceiling();
        return this;
    }

    public int CompareTo(WrappedMoney? other)
    {
        if (other == null) return 0;
        if (Amount > other.Amount) return 1;
        if (Amount < other.Amount) return -1;
        return 0;
    }
}

public static class MoneyExtension
{
    public static WrappedMoney Sum(this IEnumerable<WrappedMoney> moneys)
    {
        if (!moneys.Any()) return WrappedMoney.GetBaseCurrencyZero();
        return moneys.Aggregate((a, b) => a + b);
    }

    /// <summary>
    /// Sum up an IEnumerable of objects that contain Money object. Default return Money of Amount 0 in base currency.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="moneys"></param>
    /// <param name="selector">Delegate to select money object</param>
    /// <returns></returns>
    public static WrappedMoney Sum<T>(this IEnumerable<T> moneys, Func<T, WrappedMoney> selector)
    {
        if (!moneys.Any()) return WrappedMoney.GetBaseCurrencyZero();
        return moneys.Select(selector).Aggregate((a, b) => a + b);
    }
}
