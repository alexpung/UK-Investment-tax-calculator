using Model.TaxEvents;

namespace UnitTest.Test.Model;

public class StockSplitTests
{
    [Fact]
    public void GetSharesAfterSplit_ReturnsCorrectShares()
    {
        // Arrange
        var stockSplit = new StockSplit
        {
            AssetName = "Test",
            Date = new DateTime(2023, 1, 1),
            NumberBeforeSplit = 2,
            NumberAfterSplit = 3,
            Rounding = true
        };
        decimal quantity = 10;

        // Act
        decimal sharesAfterSplit = stockSplit.GetSharesAfterSplit(quantity);

        // Assert
        sharesAfterSplit.ShouldBe(15);
    }
}