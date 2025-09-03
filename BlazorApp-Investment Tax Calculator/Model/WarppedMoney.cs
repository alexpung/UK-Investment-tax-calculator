using NMoneys;

using System.Text.Json.Serialization;

namespace InvestmentTaxCalculator.Model;

[System.Diagnostics.DebuggerDisplay("{ToString()}")]
public record WrappedMoney : IComparable<WrappedMoney>, IEquatable<WrappedMoney>
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

    [JsonConstructor]
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
        if (money1.Amount == 0) return money2;
        if (money2.Amount == 0) return money1;
        return new WrappedMoney(money1._nMoney + money2._nMoney);
    }

    public static WrappedMoney operator -(WrappedMoney money1, WrappedMoney money2)
    {
        if (money1.Amount == 0) return -money2;
        if (money2.Amount == 0) return money1;
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

    public static bool operator >=(WrappedMoney money1, WrappedMoney money2)
    {
        return money1._nMoney >= money2._nMoney;
    }

    public static bool operator <=(WrappedMoney money1, WrappedMoney money2)
    {
        return money1._nMoney <= money2._nMoney;
    }

    public static bool operator >(WrappedMoney money1, WrappedMoney money2)
    {
        return money1._nMoney > money2._nMoney;
    }

    public static bool operator <(WrappedMoney money1, WrappedMoney money2)
    {
        return money1._nMoney < money2._nMoney;
    }

    public WrappedMoney Floor()
    {
        return new WrappedMoney(_nMoney.Floor());
    }

    public WrappedMoney Ceiling()
    {
        return new WrappedMoney(_nMoney.Ceiling());
    }

    public WrappedMoney Convert(decimal fxRate, string currency)
    {
        return new WrappedMoney(fxRate * Amount, currency);
    }

    public int CompareTo(WrappedMoney? other)
    {
        if (other == null) return 0;
        if (Amount > other.Amount) return 1;
        if (Amount < other.Amount) return -1;
        return 0;
    }

    public virtual bool Equals(WrappedMoney? other)
    {
        if (other == null) return false;
        return Amount == other.Amount && Currency == other.Currency;
    }

    public static WrappedMoney Min(WrappedMoney wrappedMoney1, WrappedMoney wrappedMoney2)
    {
        if (wrappedMoney1.Currency != wrappedMoney2.Currency) throw new ArgumentException($"Cannot compare {wrappedMoney1.Currency} with {wrappedMoney2.Currency}");
        if (wrappedMoney1 < wrappedMoney2) return wrappedMoney1;
        return wrappedMoney2;
    }

    public static WrappedMoney Max(WrappedMoney wrappedMoney1, WrappedMoney wrappedMoney2)
    {
        if (wrappedMoney1.Currency != wrappedMoney2.Currency) throw new ArgumentException($"Cannot compare {wrappedMoney1.Currency} with {wrappedMoney2.Currency}");
        if (wrappedMoney1 > wrappedMoney2) return wrappedMoney1;
        return wrappedMoney2;
    }

    public override int GetHashCode()
    {
        return new
        {
            Amount,
            Currency
        }.GetHashCode();
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
