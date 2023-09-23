using Model;

namespace UnitTest.Test.Model;

public class DescribedMoneyTests
{
    [Fact]
    public void BaseCurrencyAmount_ReturnsCorrectValue()
    {
        // Arrange
        var amount = new WrappedMoney(100, "USD");
        var describedMoney = new DescribedMoney { Amount = amount, FxRate = 1.5m };

        // Act
        WrappedMoney baseCurrencyAmount = describedMoney.BaseCurrencyAmount;

        // Assert
        baseCurrencyAmount.ShouldBe(new WrappedMoney(150));
    }
}

