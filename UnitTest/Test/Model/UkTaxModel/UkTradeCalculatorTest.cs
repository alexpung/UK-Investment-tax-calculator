namespace UnitTest.Test.Model.UkTaxModel;
using Enum;
using global::Model;
using global::Model.Interfaces;
using global::Model.UkTaxModel;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;


public class UkTradeCalculatorTests
{
    [Fact]
    public void CalculateTax_ClearsSection104Pools()
    {
        // Arrange
        var section104PoolsMock = new Mock<UkSection104Pools>();
        var tradeListMock = new Mock<ITradeAndCorporateActionList>();
        tradeListMock.Setup(t => t.Trades).Returns(new List<Trade>());
        tradeListMock.Setup(t => t.CorporateActions).Returns(new List<CorporateAction>());
        var calculator = new UkTradeCalculator(section104PoolsMock.Object, tradeListMock.Object);

        // Act
        calculator.CalculateTax();

        // Assert
        section104PoolsMock.Verify(p => p.Clear(), Times.Once);
    }

    [Fact]
    public void CalculateTax_GroupsTradeOnSameSideOnSameDay()
    {
        // Arrange
        var trade1Mock = new Mock<Trade>();
        trade1Mock.SetupGet(t => t.AssetName).Returns("Asset1");
        trade1Mock.SetupGet(t => t.Date).Returns(new DateTime(2023, 1, 1));
        trade1Mock.SetupGet(t => t.BuySell).Returns(TradeType.BUY);
        trade1Mock.SetupGet(t => t.Quantity).Returns(100);
        trade1Mock.SetupGet(t => t.NetProceed).Returns(BaseCurrencyMoney.BaseCurrencyAmount(1000));

        var trade2Mock = new Mock<Trade>();
        trade2Mock.SetupGet(t => t.AssetName).Returns("Asset1");
        trade2Mock.SetupGet(t => t.Date).Returns(new DateTime(2023, 1, 1));
        trade2Mock.SetupGet(t => t.BuySell).Returns(TradeType.BUY);
        trade2Mock.SetupGet(t => t.Quantity).Returns(200);
        trade2Mock.SetupGet(t => t.NetProceed).Returns(BaseCurrencyMoney.BaseCurrencyAmount(2000));

        var tradeListMock = new Mock<ITradeAndCorporateActionList>();
        tradeListMock.Setup(t => t.Trades).Returns(new List<Trade> { trade1Mock.Object, trade2Mock.Object });
        tradeListMock.Setup(t => t.CorporateActions).Returns(new List<CorporateAction>());

        var section104PoolsMock = new Mock<UkSection104Pools>();
        section104PoolsMock.Setup(i => i.GetExistingOrInitialise(It.IsAny<string>())).Returns((string assetName) => new UkSection104(assetName));
        var calculator = new UkTradeCalculator(section104PoolsMock.Object, tradeListMock.Object);

        // Act
        var result = calculator.CalculateTax();

        // Assert
        section104PoolsMock.Verify(p => p.GetExistingOrInitialise("Asset1"), Times.Once);
        section104PoolsMock.Verify(p => p.GetExistingOrInitialise(It.IsAny<string>()), Times.Once);
        result.Count.ShouldBe(1);
        result[0].TradeList.Count.ShouldBe(2);
        result[0].TradeList[0].ShouldBe(trade1Mock.Object);
    }

    [Fact]
    public void ApplySameDayMatchingRule_MatchesSameDayTrades()
    {
        // Arrange
        var trade1Mock = new Mock<Trade>();
        trade1Mock.SetupGet(t => t.AssetName).Returns("Asset1");
        trade1Mock.SetupGet(t => t.Date).Returns(new DateTime(2023, 1, 1));
        trade1Mock.SetupGet(t => t.BuySell).Returns(TradeType.BUY);
        trade1Mock.SetupGet(t => t.Quantity).Returns(100);
        trade1Mock.SetupGet(t => t.NetProceed).Returns(BaseCurrencyMoney.BaseCurrencyAmount(1000));
        var trade2Mock = new Mock<Trade>();
        trade2Mock.SetupGet(t => t.AssetName).Returns("Asset1");
        trade2Mock.SetupGet(t => t.Date).Returns(new DateTime(2023, 1, 2));
        trade2Mock.SetupGet(t => t.BuySell).Returns(TradeType.SELL);
        trade2Mock.SetupGet(t => t.Quantity).Returns(80);
        trade2Mock.SetupGet(t => t.NetProceed).Returns(BaseCurrencyMoney.BaseCurrencyAmount(8000));
        var trade3Mock = new Mock<Trade>();
        trade3Mock.SetupGet(t => t.AssetName).Returns("Asset1");
        trade3Mock.SetupGet(t => t.Date).Returns(new DateTime(2023, 1, 2));
        trade3Mock.SetupGet(t => t.BuySell).Returns(TradeType.BUY);
        trade3Mock.SetupGet(t => t.Quantity).Returns(50);
        trade3Mock.SetupGet(t => t.NetProceed).Returns(BaseCurrencyMoney.BaseCurrencyAmount(2000));

        var tradeListMock = new Mock<ITradeAndCorporateActionList>();
        tradeListMock.Setup(t => t.Trades).Returns(new List<Trade> { trade1Mock.Object, trade2Mock.Object, trade3Mock.Object });
        tradeListMock.Setup(t => t.CorporateActions).Returns(new List<CorporateAction>());

        var section104PoolsMock = new Mock<UkSection104Pools>();
        UkSection104 section104 = new("Asset1");
        section104PoolsMock.Setup(i => i.GetExistingOrInitialise(It.IsAny<string>())).Returns(section104);
        var calculator = new UkTradeCalculator(section104PoolsMock.Object, tradeListMock.Object);

        // Act
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        // Assert
        result[1].Gain.ShouldBe(BaseCurrencyMoney.BaseCurrencyAmount(5700));
        result[1].TotalAllowableCost.ShouldBe(BaseCurrencyMoney.BaseCurrencyAmount(2300));
        result[1].TotalQty.ShouldBe(80m);
        section104.Quantity.ShouldBe(70);
        section104.ValueInBaseCurrency.ShouldBe(BaseCurrencyMoney.BaseCurrencyAmount(700));
    }

    // Add more unit tests for other methods in the UkTradeCalculator class

}
