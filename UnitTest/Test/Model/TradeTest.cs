using Enum;

using Model;
using Model.TaxEvents;

namespace UnitTest.Test.Model;

public class TradeTests
{
    [Fact]
    public void NetProceed_NoExpenses_ReturnsGrossProceed()
    {
        // Arrange
        var trade = new Trade
        {
            AssetName = "Test",
            Date = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Local),
            BuySell = TradeType.BUY,
            Quantity = 10,
            GrossProceed = new DescribedMoney
            {
                Amount = new WrappedMoney(100),
                Description = "Trade gross proceed"
            }
        };

        // Act
        var netProceed = trade.NetProceed;

        // Assert
        netProceed.Amount.ShouldBe(100);
    }

    [Fact]
    public void NetProceed_WithExpenses_ReturnsCorrectNetProceed()
    {
        // Arrange
        var trade = new Trade
        {
            AssetName = "Test",
            Date = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Local),
            BuySell = TradeType.SELL,
            Quantity = 5,
            GrossProceed = new DescribedMoney
            {
                Amount = new WrappedMoney(100),
                Description = "Trade gross proceed"
            },
            Expenses =
            [
                new DescribedMoney { Amount = new WrappedMoney(10), Description = "Expense 1" },
                new DescribedMoney { Amount = new WrappedMoney(20), Description = "Expense 2" }
            ],
        };

        // Act
        var netProceed = trade.NetProceed;

        // Assert
        netProceed.Amount.ShouldBe(70);
    }
}

