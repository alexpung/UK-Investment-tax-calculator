using Model;
using NMoneys;
using System.Collections;

namespace UnitTest.Test.Model;

public class MoneyTest
{
    [Theory]
    [ClassData(typeof(MoneyTestTestData))]
    public void TestMoneyTextRepresentation(string currency, decimal amount, string textRepresentationOneHundred)
    {
        WrappedMoney money = new(amount, currency);
        money.ToString().ShouldBe(textRepresentationOneHundred);
    }

    [Fact]
    public void TestMoneyMultiplication()
    {
        WrappedMoney money = new(100.2m);
        (money * 2.5m).ShouldBeEquivalentTo(new WrappedMoney(250.5m));
    }

    [Fact]
    public void TestMoneySubtraction()
    {
        WrappedMoney money = new(100.2m, "Cad");
        WrappedMoney money2 = new(20.1m, "Cad");
        (money - money2).ShouldBeEquivalentTo(new WrappedMoney(80.1m, "Cad"));
    }

    [Fact]
    public void TestMoneyAddition()
    {
        WrappedMoney money = new(100.2m, "Cad");
        WrappedMoney money2 = new(20.1m, "Cad");
        (money + money2).ShouldBeEquivalentTo(new WrappedMoney(120.3m, "Cad"));
    }

    [Fact]
    public void TestMoneyDivision()
    {
        WrappedMoney money = new(100.2m, "Cad");
        (money / 2.5m).ShouldBeEquivalentTo(new WrappedMoney(40.08m, "Cad"));
    }

    [Fact]
    public void TestMoneySumEmptyEqualToBaseCurrencyZero()
    {
        List<WrappedMoney> moneys = new();
        moneys.Sum().ShouldBeEquivalentTo(WrappedMoney.GetBaseCurrencyZero());
    }

    [Fact]
    public void TestMoneySum()
    {
        List<WrappedMoney> moneys = new() { new WrappedMoney(10m, "Gbp"), new WrappedMoney(20.1m, "Gbp"), new WrappedMoney(0.2m, "Gbp") };
        moneys.Sum().ShouldBeEquivalentTo(new WrappedMoney(30.3m, "Gbp"));
    }

    [Fact]
    public void TestMoneySumDifferentCurrenciesThrowException()
    {
        List<WrappedMoney> moneys = new() { new WrappedMoney(10m, "Gbp"), new WrappedMoney(20.1m, "Cad"), new WrappedMoney(0.2m, "Gbp") };
        Should.Throw(() => moneys.Sum(), typeof(DifferentCurrencyException));
    }

    [Fact]
    public void TestObjectWithCurrencySum()
    {
        List<DescribedMoney> moneys = new()
        { new DescribedMoney() { Amount= new WrappedMoney(10m, "Gbp") },
          new DescribedMoney() { Amount= new WrappedMoney(20.1m, "Gbp") },
          new DescribedMoney() { Amount= new WrappedMoney(0.2m, "Gbp") },
        };
        moneys.Sum(i => i.Amount).ShouldBeEquivalentTo(new WrappedMoney(30.3m, "Gbp"));
    }

    [Fact]
    public void TestEmptyBaseCurrencySum()
    {
        List<DescribedMoney> moneys = new List<DescribedMoney>();
        moneys.Sum(i => i.Amount).ShouldBeEquivalentTo(WrappedMoney.GetBaseCurrencyZero());
    }
}

public class MoneyTestTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { "Jpy", 100, "¥100" };
        yield return new object[] { "Gbp", 10000, "£10,000.00" };
        yield return new object[] { "Usd", 10.25, "$10.25" };
        yield return new object[] { "Aud", 1234.56, "$1,234.56" };
        yield return new object[] { "Cad", 1234.56, "$1,234.56" };
        yield return new object[] { "Cny", 1234.56, "¥1,234.56" };
        yield return new object[] { "Sek", 1234.56, "1.234,56 kr" };
        yield return new object[] { "Eur", 1234.56, "1.234,56 €" };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
