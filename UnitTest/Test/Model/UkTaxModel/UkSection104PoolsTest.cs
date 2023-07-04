using Enum;
using Model;
using Model.Interfaces;
using Model.UkTaxModel;
using Moq;

namespace UnitTest.Test.Model.UkTaxModel;

public class UkSection104PoolsTests
{
    [Fact]
    public void GetSection104s_ReturnsAllSection104s()
    {
        // Arrange
        var section104Pools = new UkSection104Pools();
        section104Pools.GetExistingOrInitialise("Test1");
        section104Pools.GetExistingOrInitialise("Test2");
        section104Pools.GetExistingOrInitialise("Test3");
        // Act
        List<UkSection104> result = section104Pools.GetSection104s();

        // Assert
        result.Count.ShouldBe(3);
        result.Select(i => i.AssetName).ShouldBe(new List<string>() { "Test1", "Test2", "Test3" }, ignoreOrder: true);
    }

    [Fact]
    public void GetExistingOrInitialise_ReturnsExistingSection104IfAvailable()
    {
        // Arrange
        var section104Pools = new UkSection104Pools();
        UkSection104 testSection104 = section104Pools.GetExistingOrInitialise("Asset1");
        testSection104.MatchTradeWithSection104(CreateMockTradeTaxCalculation("Asset1", new DateTime(2023, 1, 1), 100m, 2000, TradeType.BUY));

        // Act
        UkSection104 result = section104Pools.GetExistingOrInitialise("Asset1");

        // Assert
        result.Quantity.ShouldBe(100);
        result.ValueInBaseCurrency.ShouldBe(BaseCurrencyMoney.BaseCurrencyAmount(2000));
    }

    [Fact]
    public void GetExistingOrInitialise_CreatesNewSection104IfNotAvailable()
    {
        // Arrange
        var section104Pools = new UkSection104Pools();
        string assetName = "Asset1";

        // Act
        UkSection104 result = section104Pools.GetExistingOrInitialise(assetName);

        // Assert
        result.ShouldNotBeNull();
        result.AssetName.ShouldBe(assetName);
    }

    [Fact]
    public void Clear_RemovesAllSection104Pools()
    {
        // Arrange
        var section104Pools = new UkSection104Pools();
        section104Pools.GetExistingOrInitialise("Asset1");
        section104Pools.GetExistingOrInitialise("Asset2");
        section104Pools.GetExistingOrInitialise("Asset3");

        // Act
        section104Pools.Clear();
        List<UkSection104> result = section104Pools.GetSection104s();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetHistory_ReturnsSection104HistoryUpToGivenTradeCalculation()
    {
        // Arrange
        var section104Pools = new UkSection104Pools();
        var tradeTaxCalculation1 = CreateMockTradeTaxCalculation("Asset1", new DateTime(2023, 1, 1), 100, 1000, TradeType.BUY);
        var tradeTaxCalculation2 = CreateMockTradeTaxCalculation("Asset1", new DateTime(2023, 2, 1), 200, 1100, TradeType.BUY);
        var tradeTaxCalculation3 = CreateMockTradeTaxCalculation("Asset1", new DateTime(2023, 3, 1), 300, 1200, TradeType.SELL);
        var tradeTaxCalculation4 = CreateMockTradeTaxCalculation("Asset1", new DateTime(2023, 3, 2), 400, 1300, TradeType.BUY);
        var tradeTaxCalculation5 = CreateMockTradeTaxCalculation("Asset1", new DateTime(2023, 3, 3), 500, 1400, TradeType.BUY);
        var tradeTaxCalculation6 = CreateMockTradeTaxCalculation("Asset2", new DateTime(2023, 1, 1), 100, 2000, TradeType.BUY);
        var tradeTaxCalculation7 = CreateMockTradeTaxCalculation("Asset3", new DateTime(2023, 1, 1), 100, 3000, TradeType.BUY);
        section104Pools.GetExistingOrInitialise("Asset1").MatchTradeWithSection104(tradeTaxCalculation1);
        section104Pools.GetExistingOrInitialise("Asset1").MatchTradeWithSection104(tradeTaxCalculation2);
        section104Pools.GetExistingOrInitialise("Asset1").MatchTradeWithSection104(tradeTaxCalculation3);
        section104Pools.GetExistingOrInitialise("Asset1").MatchTradeWithSection104(tradeTaxCalculation4);
        section104Pools.GetExistingOrInitialise("Asset1").MatchTradeWithSection104(tradeTaxCalculation5);
        section104Pools.GetExistingOrInitialise("Asset2").MatchTradeWithSection104(tradeTaxCalculation6);
        section104Pools.GetExistingOrInitialise("Asset3").MatchTradeWithSection104(tradeTaxCalculation7);
        // Act
        List<Section104History> result = section104Pools.GetHistory(tradeTaxCalculation4);
        // Assert
        result.Count.ShouldBe(4);
        result[3].QuantityChange.ShouldBe(400);
        result[3].ValueChange.ShouldBe(BaseCurrencyMoney.BaseCurrencyAmount(1300));
    }

    private static ITradeTaxCalculation CreateMockTradeTaxCalculation(string assetName, DateTime dateTime, decimal qty, decimal value, TradeType tradeType)
    {
        var mock = new Mock<ITradeTaxCalculation>();
        mock.SetupGet(c => c.AssetName).Returns(assetName);
        mock.SetupGet(c => c.Date).Returns(dateTime);
        mock.SetupGet(c => c.UnmatchedQty).Returns(qty);
        mock.SetupGet(c => c.BuySell).Returns(tradeType);
        mock.SetupGet(c => c.MatchHistory).Returns(new List<TradeMatch>());
        mock.Setup(c => c.MatchAll()).Returns((qty, BaseCurrencyMoney.BaseCurrencyAmount(value)));
        return mock.Object;
    }
}
