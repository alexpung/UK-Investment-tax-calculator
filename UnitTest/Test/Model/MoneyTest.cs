using Model;
using NMoneys;
using NMoneys.Extensions;
using System.Collections;

namespace UnitTest.Test.Model;

public class MoneyTest
{
    [Theory]
    [ClassData(typeof(MoneyTestTestData))]
    public void TestMoneyTextRepresentation(Currency currency, decimal amount, string textRepresentationOneHundred)
    {
        Money money = new(amount, currency);
        money.ToString().ShouldBe(textRepresentationOneHundred);
    }

    [Fact]
    public void TestMoneyMultiplication()
    {
        Money money = new(100.2m);
        money.Multiply(2.5m).ShouldBeEquivalentTo(new Money(250.5m));
    }

    [Fact]
    public void TestMoneySubtraction()
    {
        Money money = new(100.2m, Currency.Cad);
        Money money2 = new(20.1m, Currency.Cad);
        (money - money2).ShouldBeEquivalentTo(new Money(80.1m, Currency.Cad));
    }

    [Fact]
    public void TestMoneyAddition()
    {
        Money money = new(100.2m, Currency.Cad);
        Money money2 = new(20.1m, Currency.Cad);
        (money + money2).ShouldBeEquivalentTo(new Money(120.3m, Currency.Cad));
    }

    [Fact]
    public void TestMoneyDivision()
    {
        Money money = new(100.2m, Currency.Cad);
        money.Divide(2.5m).ShouldBeEquivalentTo(new Money(40.08m, Currency.Cad));
    }

    [Fact]
    public void TestMoneySumEmptyEqualToBaseCurrencyZero()
    {
        List<Money> moneys = new();
        moneys.Sum().ShouldBeEquivalentTo(BaseCurrencyMoney.BaseCurrencyZero);
    }

    [Fact]
    public void TestMoneySum()
    {
        List<Money> moneys = new() { new Money(10m, Currency.Gbp), new Money(20.1m, Currency.Gbp), new Money(0.2m, Currency.Gbp) };
        moneys.Sum().ShouldBeEquivalentTo(new Money(30.3m, Currency.Gbp));
    }

    [Fact]
    public void TestMoneySumDifferentCurrenciesThrowException()
    {
        List<Money> moneys = new() { new Money(10m, Currency.Gbp), new Money(20.1m, Currency.Cad), new Money(0.2m, Currency.Gbp) };
        Should.Throw(() => moneys.Sum(), typeof(DifferentCurrencyException));
    }

    [Fact]
    public void TestObjectWithCurrencySum()
    {
        List<DescribedMoney> moneys = new List<DescribedMoney>
        { new DescribedMoney() { Amount= new Money(10m, Currency.Gbp) },
          new DescribedMoney() { Amount= new Money(20.1m, Currency.Gbp) },
          new DescribedMoney() { Amount= new Money(0.2m, Currency.Gbp) },
        };
        moneys.BaseCurrencySum(i => i.Amount).ShouldBeEquivalentTo(new Money(30.3m, Currency.Gbp));
    }

    [Fact]
    public void TestEmptyCurrencySum()
    {
        List<DescribedMoney> moneys = new List<DescribedMoney>();
        moneys.BaseCurrencySum(i => i.Amount).ShouldBeEquivalentTo(BaseCurrencyMoney.BaseCurrencyZero);
    }

    [Fact]
    public void TestChangeBaseCurrencyZero()
    {
        BaseCurrencyMoney.BaseCurrency = Currency.Jpy;
        Money money = BaseCurrencyMoney.BaseCurrencyZero;
        money.Amount.ShouldBe(0);
        money.GetCurrency().ShouldBe(Currency.Jpy);
    }

    [Fact]
    public void TestChangeBaseCurrencyAmount()
    {
        BaseCurrencyMoney.BaseCurrency = Currency.Jpy;
        Money money = BaseCurrencyMoney.BaseCurrencyAmount(100);
        money.Amount.ShouldBe(100);
        money.GetCurrency().ShouldBe(Currency.Jpy);
    }
}

public class MoneyTestTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { Currency.Jpy, 100, "¥100" };
        yield return new object[] { Currency.Gbp, 10000, "£10,000.00" };
        yield return new object[] { Currency.Usd, 10.25, "$10.25" };
        yield return new object[] { Currency.Aud, 1234.56, "$1,234.56" };
        yield return new object[] { Currency.Cad, 1234.56, "$1,234.56" };
        yield return new object[] { Currency.Cny, 1234.56, "¥1,234.56" };
        yield return new object[] { Currency.Sek, 1234.56, "1.234,56 kr" };
        yield return new object[] { Currency.Eur, 1234.56, "1.234,56 €" };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
