using Enumerations;

using Model;
using Model.UkTaxModel;
using Model.UkTaxModel.Stocks;

using UnitTest.Helper;

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
        TradeTaxCalculation mockTrade = MockTrade.CreateTradeTaxCalculation("Asset1", new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Local), 100m, 2000, TradeType.ACQUISITION);
        mockTrade.MatchWithSection104(testSection104);

        // Act
        UkSection104 result = section104Pools.GetExistingOrInitialise("Asset1");

        // Assert
        result.Quantity.ShouldBe(100);
        result.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(2000));
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
}
