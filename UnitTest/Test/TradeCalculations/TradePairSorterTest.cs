namespace UnitTest.Test.TradeCalculations;
using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;

using NSubstitute;

using Shouldly;

using System;
using System.Globalization;

using Xunit;

public class TradePairSorterTests
{
    private readonly ITradeTaxCalculation _trade1;
    private readonly ITradeTaxCalculation _trade2;

    public TradePairSorterTests()
    {
        _trade1 = Substitute.For<ITradeTaxCalculation>();
        _trade2 = Substitute.For<ITradeTaxCalculation>();
    }

    [Fact]
    public void Constructor_ValidBuySellTrade_SortsCorrectly()
    {
        _trade1.AcquisitionDisposal.Returns(TradeType.ACQUISITION);
        _trade1.Date.Returns(DateTime.Parse("02-Jun-22 10:00:00", CultureInfo.InvariantCulture));
        _trade2.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        _trade2.Date.Returns(DateTime.Parse("02-Jun-21 10:00:00", CultureInfo.InvariantCulture));
        var sorter = new TradePairSorter<ITradeTaxCalculation>(_trade1, _trade2);
        sorter.AcquisitionTrade.ShouldBe(_trade1);
        sorter.DisposalTrade.ShouldBe(_trade2);
        sorter.EarlierTrade.ShouldBe(_trade2);
        sorter.LatterTrade.ShouldBe(_trade1);
    }

    [Fact]
    public void Constructor_TradesWithInvalidCombination_ThrowsArgumentException()
    {
        _trade1.AcquisitionDisposal.Returns(TradeType.ACQUISITION);
        _trade2.AcquisitionDisposal.Returns(TradeType.ACQUISITION);
        Should.Throw<ArgumentException>(() => new TradePairSorter<ITradeTaxCalculation>(_trade1, _trade2));
    }

    [Fact]
    public void Constructor_SellBeforeBuy_SortsCorrectly()
    {
        _trade1.AcquisitionDisposal.Returns(TradeType.DISPOSAL);
        _trade1.Date.Returns(DateTime.Parse("02-Jun-22 10:00:00", CultureInfo.InvariantCulture));
        _trade2.AcquisitionDisposal.Returns(TradeType.ACQUISITION);
        _trade2.Date.Returns(DateTime.Parse("03-Jun-22 10:00:00", CultureInfo.InvariantCulture));
        var sorter = new TradePairSorter<ITradeTaxCalculation>(_trade1, _trade2);
        sorter.AcquisitionTrade.ShouldBe(_trade2);
        sorter.DisposalTrade.ShouldBe(_trade1);
        sorter.EarlierTrade.ShouldBe(_trade1);
        sorter.LatterTrade.ShouldBe(_trade2);
    }
}

