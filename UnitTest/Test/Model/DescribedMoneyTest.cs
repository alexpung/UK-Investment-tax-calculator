using Model;
using NMoneys;

namespace UnitTest.Test.Model;

public class DescribedMoneyTests
{
    [Fact]
    public void BaseCurrencyAmount_ReturnsCorrectValue()
    {
        // Arrange
        var amount = new Money(100, "USD");
        var describedMoney = new DescribedMoney { Amount = amount, FxRate = 1.5m };

        // Act
        Money baseCurrencyAmount = describedMoney.BaseCurrencyAmount;

        // Assert
        baseCurrencyAmount.ShouldBe(BaseCurrencyMoney.BaseCurrencyAmount(150));
    }
}

