namespace UnitTest.Test.Model;
using Enumerations;

using global::Model;
using global::Model.TaxEvents;
using global::Model.UkTaxModel.Stocks;

using NSubstitute;

using System;
using System.Collections.Generic;

using UnitTest.Helper;

using Xunit;

public class TradeTaxCalculationTests
{
    [Fact]
    public void TradeTaxCalculation_ThrowsException_WhenTradesHaveDifferentBuySellSides()
    {
        // Arrange
        Trade trade1 = Substitute.For<Trade>();
        Trade trade2 = Substitute.For<Trade>();
        Trade trade3 = Substitute.For<Trade>();
        trade1.AcquisitionDisposal.Returns(TradeType.ACQUISITION);
        trade2.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        trade3.AcquisitionDisposal.Returns(TradeType.ACQUISITION);
        var trades = new List<Trade> { trade1, trade2, trade3 };
        // Act & Assert
        Should.Throw<ArgumentException>(() => new TradeTaxCalculation(trades));
    }

    [Theory]
    [InlineData(TradeType.ACQUISITION)]
    [InlineData(TradeType.DISPOSAL)]
    public void TradeTaxCalculation_SetCorrectBuySellSides(TradeType tradeType)
    {
        // Arrange
        Trade trade1 = Substitute.For<Trade>();
        Trade trade2 = Substitute.For<Trade>();
        Trade trade3 = Substitute.For<Trade>();
        trade1.AcquisitionDisposal.Returns(tradeType);
        trade2.AcquisitionDisposal.Returns(tradeType);
        trade3.AcquisitionDisposal.Returns(tradeType);
        trade1.NetProceed.Returns(new WrappedMoney(100));
        trade2.NetProceed.Returns(new WrappedMoney(100));
        trade3.NetProceed.Returns(new WrappedMoney(100));
        var trades = new List<Trade> { trade1, trade2, trade3 };
        // Act & Assert
        TradeTaxCalculation calculation = new(trades);
        calculation.AcquisitionDisposal.ShouldBe(tradeType);
    }

    [Fact]
    public void TradeTaxCalculation_CalculatesTotalAllowableCost()
    {
        // Arrange
        Trade trade1 = Substitute.For<Trade>();
        trade1.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        trade1.NetProceed.Returns(new WrappedMoney(100));
        var matchHistory = new List<TradeMatch> { TradeCalculationHelper.CreateTradeMatch(TaxMatchType.SECTION_104, 100, new WrappedMoney(100), new WrappedMoney(150)),
                                                  TradeCalculationHelper.CreateTradeMatch(TaxMatchType.SECTION_104, 100, new WrappedMoney(200), new WrappedMoney(250))
                                                };
        var calculation = new TradeTaxCalculation([trade1])
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
        Trade trade1 = Substitute.For<Trade>();
        trade1.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        trade1.NetProceed.Returns(new WrappedMoney(100));
        var matchHistory = new List<TradeMatch> { TradeCalculationHelper.CreateTradeMatch(TaxMatchType.SECTION_104, 100, WrappedMoney.GetBaseCurrencyZero(), new WrappedMoney(100)),
                                                  TradeCalculationHelper.CreateTradeMatch(TaxMatchType.SECTION_104, 100, WrappedMoney.GetBaseCurrencyZero(), new WrappedMoney(200))
                                                };
        var calculation = new TradeTaxCalculation([trade1])
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
        Trade trade1 = Substitute.For<Trade>();
        trade1.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        trade1.NetProceed.Returns(new WrappedMoney(100));
        var matchHistory = new List<TradeMatch> { TradeCalculationHelper.CreateTradeMatch(TaxMatchType.SECTION_104, 100, new WrappedMoney(70), new WrappedMoney(100)),
                                                  TradeCalculationHelper.CreateTradeMatch(TaxMatchType.SECTION_104, 100, new WrappedMoney(210), new WrappedMoney(200))
                                                };
        var calculation = new TradeTaxCalculation([trade1])
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
        Trade trade1 = Substitute.For<Trade>();
        trade1.AssetName.Returns("IBM");
        trade1.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        trade1.NetProceed.Returns(new WrappedMoney(100));
        trade1.Quantity.Returns(10m);
        var calculation = new TradeTaxCalculation([trade1]);
        // Assert
        calculation.AssetName.ShouldBe("IBM");
        calculation.CalculationCompleted.ShouldBeFalse();
    }

    [Fact]
    public void TestTradeDateInitialised()
    {
        // Arrange
        Trade trade1 = Substitute.For<Trade>();
        trade1.Date.Returns(new DateTime(2023, 1, 1, 12, 34, 56, DateTimeKind.Local));
        trade1.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        trade1.NetProceed.Returns(new WrappedMoney(100));
        var calculation = new TradeTaxCalculation([trade1]);
        // Assert
        calculation.Date.ShouldBe(new DateTime(2023, 1, 1, 12, 34, 56, DateTimeKind.Local));
    }

    [Fact]
    public void MatchQty_MatchesAndUpdatesUnmatchedQtyAndUnmatchedNetAmount()
    {
        // Arrange
        Trade trade1 = Substitute.For<Trade>();
        Trade trade2 = Substitute.For<Trade>();
        trade1.Quantity.Returns(10);
        trade1.NetProceed.Returns(new WrappedMoney(100));
        trade2.Quantity.Returns(20);
        trade2.NetProceed.Returns(new WrappedMoney(200));
        var trades = new List<Trade> { trade1, trade2 };
        var calculation = new TradeTaxCalculation(trades);

        // Act
        calculation.MatchQty(12);

        // Assert
        calculation.UnmatchedQty.ShouldBe(18);
        calculation.UnmatchedCostOrProceed.ShouldBe(new WrappedMoney(180));
        calculation.CalculationCompleted.ShouldBeFalse();
        calculation.TotalCostOrProceed.ShouldBe(new WrappedMoney(300));
        calculation.TotalQty.ShouldBe(30);
    }

    [Fact]
    public void TestUnmatchedQtyAndValueReturnCorrectResult()
    {
        // Arrange
        Trade trade1 = Substitute.For<Trade>();
        Trade trade2 = Substitute.For<Trade>();
        trade1.Quantity.Returns(20);
        trade1.NetProceed.Returns(new WrappedMoney(300));
        trade2.Quantity.Returns(60);
        trade2.NetProceed.Returns(new WrappedMoney(500));
        var trades = new List<Trade> { trade1, trade2 };
        var calculation = new TradeTaxCalculation(trades);
        // Act
        decimal unmatchedQty = calculation.UnmatchedQty;
        WrappedMoney unmatchedValue = calculation.GetProportionedCostOrProceed(calculation.UnmatchedQty);
        // Assert
        unmatchedQty.ShouldBe(80);
        unmatchedValue.ShouldBe(new WrappedMoney(800));
    }
}
