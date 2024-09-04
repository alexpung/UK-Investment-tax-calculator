using InvestmentTaxCalculator.Model.TaxEvents;

namespace UnitTest.Test.Model;

public class StockSplitTests
{
    // TODO: write test
    [Fact]
    public void GetSharesAfterSplit_ReturnsCorrectShares()
    {
        // Arrange
        var stockSplit = new StockSplit
        {
            AssetName = "Test",
            Date = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Local),
            SplitFrom = 2,
            SplitTo = 3,
        };
        decimal quantity = 10;
    }
}