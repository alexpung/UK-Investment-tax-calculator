namespace UnitTest.Test.Model;
using Enum;
using global::Model;
using Moq;
using NMoneys;
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
        var trades = new List<Trade> { trade1.Object, trade2.Object, trade3.Object };
        // Act & Assert
        TradeTaxCalculation calculation = new TradeTaxCalculation(trades);
        calculation.BuySell.ShouldBe(tradeType);
    }

    [Fact]
    public void TradeTaxCalculation_CalculatesTotalAllowableCost()
    {
        // Arrange
        Mock<TradeMatch> match1 = new();
        Mock<TradeMatch> match2 = new();
        match1.Setup(i => i.BaseCurrencyMatchAcquitionValue).Returns(BaseCurrencyMoney.BaseCurrencyAmount(100));
        match2.Setup(i => i.BaseCurrencyMatchAcquitionValue).Returns(BaseCurrencyMoney.BaseCurrencyAmount(200));
        Mock<Trade> trade1 = new();
        trade1.Setup(i => i.BuySell).Returns(TradeType.SELL);
        var matchHistory = new List<TradeMatch> { match1.Object, match2.Object };
        var calculation = new TradeTaxCalculation(new List<Trade>() { trade1.Object })
        {
            MatchHistory = matchHistory
        };

        // Act
        Money totalAllowableCost = calculation.TotalAllowableCost;

        // Assert
        totalAllowableCost.ShouldBe(BaseCurrencyMoney.BaseCurrencyAmount(300));
    }

    [Fact]
    public void TradeTaxCalculation_CalculatesTotalProceeds()
    {
        // Arrange
        Mock<TradeMatch> match1 = new();
        Mock<TradeMatch> match2 = new();
        match1.Setup(i => i.BaseCurrencyMatchDisposalValue).Returns(BaseCurrencyMoney.BaseCurrencyAmount(100));
        match2.Setup(i => i.BaseCurrencyMatchDisposalValue).Returns(BaseCurrencyMoney.BaseCurrencyAmount(200));
        Mock<Trade> trade1 = new();
        trade1.Setup(i => i.BuySell).Returns(TradeType.SELL);
        var matchHistory = new List<TradeMatch> { match1.Object, match2.Object };
        var calculation = new TradeTaxCalculation(new List<Trade>() { trade1.Object })
        {
            MatchHistory = matchHistory
        };

        // Act
        Money totalProceeds = calculation.TotalProceeds;

        // Assert
        totalProceeds.ShouldBe(BaseCurrencyMoney.BaseCurrencyAmount(300));
    }

    [Fact]
    public void TradeTaxCalculation_CalculatesTotalGain()
    {
        // Arrange
        Mock<TradeMatch> match1 = new();
        Mock<TradeMatch> match2 = new();
        match1.Setup(i => i.BaseCurrencyMatchDisposalValue).Returns(BaseCurrencyMoney.BaseCurrencyAmount(100));
        match2.Setup(i => i.BaseCurrencyMatchDisposalValue).Returns(BaseCurrencyMoney.BaseCurrencyAmount(200));
        match1.Setup(i => i.BaseCurrencyMatchAcquitionValue).Returns(BaseCurrencyMoney.BaseCurrencyAmount(70));
        match2.Setup(i => i.BaseCurrencyMatchAcquitionValue).Returns(BaseCurrencyMoney.BaseCurrencyAmount(210));
        Mock<Trade> trade1 = new();
        trade1.Setup(i => i.BuySell).Returns(TradeType.SELL);
        var matchHistory = new List<TradeMatch> { match1.Object, match2.Object };
        var calculation = new TradeTaxCalculation(new List<Trade>() { trade1.Object })
        {
            MatchHistory = matchHistory
        };

        // Act
        Money gain = calculation.Gain;

        // Assert
        gain.ShouldBe(BaseCurrencyMoney.BaseCurrencyAmount(20));
    }

    [Fact]
    public void TestAssetNameInitialised()
    {
        // Arrange
        Mock<Trade> trade1 = new();
        trade1.Setup(i => i.AssetName).Returns("IBM");
        trade1.Setup(i => i.BuySell).Returns(TradeType.SELL);
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
        trade1.Setup(i => i.Date).Returns(new DateTime(2023, 1, 1));
        trade1.Setup(i => i.BuySell).Returns(TradeType.SELL);
        var calculation = new TradeTaxCalculation(new List<Trade>() { trade1.Object });
        // Assert
        calculation.Date.ShouldBe(new DateTime(2023, 1, 1));
    }

    [Fact]
    public void MatchQty_MatchesAndUpdatesUnmatchedQtyAndUnmatchedNetAmount()
    {
        // Arrange
        Mock<Trade> trade1 = new();
        Mock<Trade> trade2 = new();
        trade1.Setup(i => i.Quantity).Returns(10);
        trade1.Setup(i => i.NetProceed).Returns(BaseCurrencyMoney.BaseCurrencyAmount(100));
        trade2.Setup(i => i.Quantity).Returns(20);
        trade2.Setup(i => i.NetProceed).Returns(BaseCurrencyMoney.BaseCurrencyAmount(200));
        var trades = new List<Trade> { trade1.Object, trade2.Object };
        var calculation = new TradeTaxCalculation(trades);

        // Act
        (decimal matchedQty, Money matchedValue) = calculation.MatchQty(12);

        // Assert
        matchedQty.ShouldBe(12);
        matchedValue.ShouldBe(BaseCurrencyMoney.BaseCurrencyAmount(120));
        calculation.UnmatchedQty.ShouldBe(18);
        calculation.UnmatchedNetAmount.ShouldBe(BaseCurrencyMoney.BaseCurrencyAmount(180));
        calculation.CalculationCompleted.ShouldBeFalse();
        calculation.TotalNetAmount.ShouldBe(BaseCurrencyMoney.BaseCurrencyAmount(300));
        calculation.TotalQty.ShouldBe(30);
    }

    [Fact]
    public void MatchAll_MatchesAllAndResetsUnmatchedQtyAndUnmatchedNetAmount()
    {
        // Arrange
        Mock<Trade> trade1 = new();
        Mock<Trade> trade2 = new();
        trade1.Setup(i => i.Quantity).Returns(20);
        trade1.Setup(i => i.NetProceed).Returns(BaseCurrencyMoney.BaseCurrencyAmount(300));
        trade2.Setup(i => i.Quantity).Returns(60);
        trade2.Setup(i => i.NetProceed).Returns(BaseCurrencyMoney.BaseCurrencyAmount(500));
        var trades = new List<Trade> { trade1.Object, trade2.Object };
        var calculation = new TradeTaxCalculation(trades);

        // Act
        (decimal matchedQty, Money matchedValue) = calculation.MatchAll();

        // Assert
        matchedQty.ShouldBe(80);
        matchedValue.ShouldBe(BaseCurrencyMoney.BaseCurrencyAmount(800));
        calculation.UnmatchedQty.ShouldBe(0);
        calculation.UnmatchedNetAmount.ShouldBe(BaseCurrencyMoney.BaseCurrencyZero);
        calculation.CalculationCompleted.ShouldBeTrue();
        calculation.TotalNetAmount.ShouldBe(BaseCurrencyMoney.BaseCurrencyAmount(800));
        calculation.TotalQty.ShouldBe(80);
    }
}
