namespace UnitTest.Test.Model;
using Enum;
using global::Model;
using global::Model.TaxEvents;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;


public class TradeTaxCalculationTests
{
    [Fact]
    public void TradeTaxCalculation_ThrowsException_WhenTradesHaveDifferentBuySellSides()
    {
        // Arrange
        Mock<Trade> trade1 = new();
        Mock<Trade> trade2 = new();
        Mock<Trade> trade3 = new();
        trade1.Setup(i => i.BuySell).Returns(TradeType.BUY);
        trade2.Setup(i => i.BuySell).Returns(TradeType.SELL);
        trade3.Setup(i => i.BuySell).Returns(TradeType.BUY);
        var trades = new List<Trade> { trade1.Object, trade2.Object, trade3.Object };
        // Act & Assert
        Should.Throw<ArgumentException>(() => new TradeTaxCalculation(trades));
    }

    [Theory]
    [InlineData(TradeType.BUY)]
    [InlineData(TradeType.SELL)]
    public void TradeTaxCalculation_SetCorrectBuySellSides(TradeType tradeType)
    {
        // Arrange
        Mock<Trade> trade1 = new();
        Mock<Trade> trade2 = new();
        Mock<Trade> trade3 = new();
        trade1.Setup(i => i.BuySell).Returns(tradeType);
        trade2.Setup(i => i.BuySell).Returns(tradeType);
        trade3.Setup(i => i.BuySell).Returns(tradeType);
        trade1.Setup(i => i.NetProceed).Returns(new WrappedMoney(100));
        trade2.Setup(i => i.NetProceed).Returns(new WrappedMoney(100));
        trade3.Setup(i => i.NetProceed).Returns(new WrappedMoney(100));
        var trades = new List<Trade> { trade1.Object, trade2.Object, trade3.Object };
        // Act & Assert
        TradeTaxCalculation calculation = new(trades);
        calculation.BuySell.ShouldBe(tradeType);
    }

    [Fact]
    public void TradeTaxCalculation_CalculatesTotalAllowableCost()
    {
        // Arrange
        Mock<Trade> trade1 = new();
        trade1.Setup(i => i.BuySell).Returns(TradeType.SELL);
        trade1.Setup(i => i.NetProceed).Returns(new WrappedMoney(100));
        var matchHistory = new List<TradeMatch> { TradeMatch.CreateTradeMatch(TaxMatchType.SECTION_104, 100, new WrappedMoney(100), new WrappedMoney(150)),
                                                  TradeMatch.CreateTradeMatch(TaxMatchType.SECTION_104, 100, new WrappedMoney(200), new WrappedMoney(250))
                                                };
        var calculation = new TradeTaxCalculation(new List<Trade>() { trade1.Object })
        {
            MatchHistory = matchHistory
        };

        // Act
        WrappedMoney totalAllowableCost = calculation.TotalAllowableCost;

        // Assert
        totalAllowableCost.ShouldBe(new WrappedMoney(300));
    }

    [Fact]
    public void TradeTaxCalculation_CalculatesTotalProceeds()
    {
        // Arrange
        Mock<Trade> trade1 = new();
        trade1.Setup(i => i.BuySell).Returns(TradeType.SELL);
        trade1.Setup(i => i.NetProceed).Returns(new WrappedMoney(100));
        var matchHistory = new List<TradeMatch> { TradeMatch.CreateTradeMatch(TaxMatchType.SECTION_104, 100, WrappedMoney.GetBaseCurrencyZero(), new WrappedMoney(100)),
                                                  TradeMatch.CreateTradeMatch(TaxMatchType.SECTION_104, 100, WrappedMoney.GetBaseCurrencyZero(), new WrappedMoney(200))
                                                };
        var calculation = new TradeTaxCalculation(new List<Trade>() { trade1.Object })
        {
            MatchHistory = matchHistory
        };

        // Act
        WrappedMoney totalProceeds = calculation.TotalProceeds;

        // Assert
        totalProceeds.ShouldBe(new WrappedMoney(300));
    }

    [Fact]
    public void TradeTaxCalculation_CalculatesTotalGain()
    {
        // Arrange
        Mock<Trade> trade1 = new();
        trade1.Setup(i => i.BuySell).Returns(TradeType.SELL);
        trade1.Setup(i => i.NetProceed).Returns(new WrappedMoney(100));
        var matchHistory = new List<TradeMatch> { TradeMatch.CreateTradeMatch(TaxMatchType.SECTION_104, 100, new WrappedMoney(70), new WrappedMoney(100)),
                                                  TradeMatch.CreateTradeMatch(TaxMatchType.SECTION_104, 100, new WrappedMoney(210), new WrappedMoney(200))
                                                };
        var calculation = new TradeTaxCalculation(new List<Trade>() { trade1.Object })
        {
            MatchHistory = matchHistory
        };

        // Act
        WrappedMoney gain = calculation.Gain;

        // Assert
        gain.ShouldBe(new WrappedMoney(20));
    }

    [Fact]
    public void TestAssetNameInitialised()
    {
        // Arrange
        Mock<Trade> trade1 = new();
        trade1.Setup(i => i.AssetName).Returns("IBM");
        trade1.Setup(i => i.BuySell).Returns(TradeType.SELL);
        trade1.Setup(i => i.NetProceed).Returns(new WrappedMoney(100));
        var calculation = new TradeTaxCalculation(new List<Trade>() { trade1.Object });
        // Assert
        calculation.AssetName.ShouldBe("IBM");
        calculation.CalculationCompleted.ShouldBeFalse();
    }

    [Fact]
    public void TestTradeDateInitialised()
    {
        // Arrange
        Mock<Trade> trade1 = new();
        trade1.Setup(i => i.Date).Returns(new DateTime(2023, 1, 1, 12, 34, 56));
        trade1.Setup(i => i.BuySell).Returns(TradeType.SELL);
        trade1.Setup(i => i.NetProceed).Returns(new WrappedMoney(100));
        var calculation = new TradeTaxCalculation(new List<Trade>() { trade1.Object });
        // Assert
        calculation.Date.ShouldBe(new DateTime(2023, 1, 1, 12, 34, 56));
    }

    [Fact]
    public void MatchQty_MatchesAndUpdatesUnmatchedQtyAndUnmatchedNetAmount()
    {
        // Arrange
        Mock<Trade> trade1 = new();
        Mock<Trade> trade2 = new();
        trade1.Setup(i => i.Quantity).Returns(10);
        trade1.Setup(i => i.NetProceed).Returns(new WrappedMoney(100));
        trade2.Setup(i => i.Quantity).Returns(20);
        trade2.Setup(i => i.NetProceed).Returns(new WrappedMoney(200));
        var trades = new List<Trade> { trade1.Object, trade2.Object };
        var calculation = new TradeTaxCalculation(trades);

        // Act
        (decimal matchedQty, WrappedMoney matchedValue) = calculation.MatchQty(12);

        // Assert
        matchedQty.ShouldBe(12);
        matchedValue.ShouldBe(new WrappedMoney(120));
        calculation.UnmatchedQty.ShouldBe(18);
        calculation.UnmatchedNetAmount.ShouldBe(new WrappedMoney(180));
        calculation.CalculationCompleted.ShouldBeFalse();
        calculation.TotalNetAmount.ShouldBe(new WrappedMoney(300));
        calculation.TotalQty.ShouldBe(30);
    }

    [Fact]
    public void MatchAll_MatchesAllAndResetsUnmatchedQtyAndUnmatchedNetAmount()
    {
        // Arrange
        Mock<Trade> trade1 = new();
        Mock<Trade> trade2 = new();
        trade1.Setup(i => i.Quantity).Returns(20);
        trade1.Setup(i => i.NetProceed).Returns(new WrappedMoney(300));
        trade2.Setup(i => i.Quantity).Returns(60);
        trade2.Setup(i => i.NetProceed).Returns(new WrappedMoney(500));
        var trades = new List<Trade> { trade1.Object, trade2.Object };
        var calculation = new TradeTaxCalculation(trades);

        // Act
        (decimal matchedQty, WrappedMoney matchedValue) = calculation.MatchAll();

        // Assert
        matchedQty.ShouldBe(80);
        matchedValue.ShouldBe(new WrappedMoney(800));
        calculation.UnmatchedQty.ShouldBe(0);
        calculation.UnmatchedNetAmount.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        calculation.CalculationCompleted.ShouldBeTrue();
        calculation.TotalNetAmount.ShouldBe(new WrappedMoney(800));
        calculation.TotalQty.ShouldBe(80);
    }
}
