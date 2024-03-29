﻿namespace UnitTest.Test.Model.UkTaxModel;
using Enumerations;

using global::Model;
using global::Model.Interfaces;
using global::Model.TaxEvents;
using global::Model.UkTaxModel;
using global::Model.UkTaxModel.Stocks;

using Moq;

using Shouldly;

using System;
using System.Collections.Generic;

using UnitTest.Helper;

using Xunit;

public class UkTradeCalculatorTests
{
    [Theory]
    [InlineData(TradeType.ACQUISITION, TradeType.ACQUISITION, 1, 2)]
    [InlineData(TradeType.DISPOSAL, TradeType.DISPOSAL, 1, 2)]
    [InlineData(TradeType.DISPOSAL, TradeType.ACQUISITION, 2, 1)]
    public void CalculateTax_GroupsTradeOnSameSideOnSameDay(TradeType tradeType1, TradeType tradeType2, int expectedITradeTaxCalculationCount, int expectedFirstTradeListCount)
    {
        // Arrange
        Mock<Trade> trade1Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 1, 12, 34, 56, DateTimeKind.Local), tradeType1, 100, 1000);
        Mock<Trade> trade2Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 1, 13, 34, 56, DateTimeKind.Local), tradeType2, 200, 2000);
        var tradeListMock = new Mock<ITradeAndCorporateActionList>();
        tradeListMock.Setup(t => t.Trades).Returns([trade1Mock.Object, trade2Mock.Object]);
        tradeListMock.Setup(t => t.CorporateActions).Returns([]);

        var section104PoolsMock = new Mock<UkSection104Pools>();
        section104PoolsMock.Setup(i => i.GetExistingOrInitialise(It.IsAny<string>())).Returns((string assetName) => new UkSection104(assetName));
        var calculator = new UkTradeCalculator(section104PoolsMock.Object, tradeListMock.Object);

        // Act
        var result = calculator.CalculateTax();

        // Assert
        section104PoolsMock.Verify(p => p.GetExistingOrInitialise("Asset1"), Times.Once);
        section104PoolsMock.Verify(p => p.GetExistingOrInitialise(It.IsAny<string>()), Times.Once);
        result.Count.ShouldBe(expectedITradeTaxCalculationCount);
        result[0].TradeList.Count.ShouldBe(expectedFirstTradeListCount);
        result[0].TradeList[0].ShouldBe(trade1Mock.Object);
    }

    [Fact]
    public void ApplySameDayMatchingRule_MatchesSameDayTrades()
    {
        // Arrange
        Mock<Trade> trade1Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 1, 12, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 100, 1000);
        Mock<Trade> trade2Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 2, 13, 34, 56, DateTimeKind.Local), TradeType.DISPOSAL, 80, 8000);
        Mock<Trade> trade3Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 2, 14, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 50, 2000);
        var tradeListMock = new Mock<ITradeAndCorporateActionList>();
        tradeListMock.Setup(t => t.Trades).Returns([trade1Mock.Object, trade2Mock.Object, trade3Mock.Object]);
        tradeListMock.Setup(t => t.CorporateActions).Returns([]);

        var section104PoolsMock = new Mock<UkSection104Pools>();
        UkSection104 section104 = new("Asset1");
        section104PoolsMock.Setup(i => i.GetExistingOrInitialise(It.IsAny<string>())).Returns(section104);
        var calculator = new UkTradeCalculator(section104PoolsMock.Object, tradeListMock.Object);

        // Act
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        // Assert
        result[1].Gain.ShouldBe(new WrappedMoney(5700));
        result[1].TotalAllowableCost.ShouldBe(new WrappedMoney(2300));
        result[1].TotalQty.ShouldBe(80m);
        section104.Quantity.ShouldBe(70);
        section104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(700));
    }


    [Fact]
    public void ApplyBedAndBreakfastRulesMatchBuyTradeWithin30Days()
    {
        // Arrange
        Mock<Trade> trade1Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 1, 12, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 100, 1000);
        Mock<Trade> trade2Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 2, 06, 34, 56, DateTimeKind.Local), TradeType.DISPOSAL, 80, 8000);
        Mock<Trade> trade3Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 2, 1, 12, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 50, 2500);
        Mock<Trade> trade4Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 2, 2, 02, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 20, 1500);

        var tradeListMock = new Mock<ITradeAndCorporateActionList>();
        tradeListMock.Setup(t => t.Trades).Returns([trade1Mock.Object, trade2Mock.Object, trade3Mock.Object, trade4Mock.Object]);
        tradeListMock.Setup(t => t.CorporateActions).Returns([]);

        var section104PoolsMock = new Mock<UkSection104Pools>();
        UkSection104 section104 = new("Asset1");
        section104PoolsMock.Setup(i => i.GetExistingOrInitialise(It.IsAny<string>())).Returns(section104);
        var calculator = new UkTradeCalculator(section104PoolsMock.Object, tradeListMock.Object);

        // Act
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        // Assert
        result[1].Gain.ShouldBe(new WrappedMoney(5200));
        result[1].TotalAllowableCost.ShouldBe(new WrappedMoney(2800));
        section104.Quantity.ShouldBe(90);
        section104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(2200));
    }

    [Fact]
    // Taxation of Chargeable Gains Act 1992, Section 105.2
    public void ShortSaleMatchWithMostRecentUnmatchedTrade()
    {
        // Arrange
        Mock<Trade> trade1Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 2, 12, 34, 56, DateTimeKind.Local), TradeType.DISPOSAL, 150, 1500);
        Mock<Trade> trade2Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 2, 1, 12, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 50, 2500); // Same day
        Mock<Trade> trade3Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 2, 1, 12, 34, 56, DateTimeKind.Local), TradeType.DISPOSAL, 50, 1500); // Same day
        Mock<Trade> trade4Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 2, 2, 12, 34, 56, DateTimeKind.Local), TradeType.DISPOSAL, 50, 2500); // bnb match
        Mock<Trade> trade5Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 2, 25, 12, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 50, 1000); // bnb match
        Mock<Trade> trade6Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 3, 3, 12, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 225, 7500); // should match this

        var tradeListMock = new Mock<ITradeAndCorporateActionList>();
        tradeListMock.Setup(t => t.Trades).Returns([trade1Mock.Object, trade2Mock.Object, trade3Mock.Object, trade4Mock.Object, trade5Mock.Object, trade6Mock.Object]);
        tradeListMock.Setup(t => t.CorporateActions).Returns([]);

        var section104PoolsMock = new Mock<UkSection104Pools>();
        UkSection104 section104 = new("Asset1");
        section104PoolsMock.Setup(i => i.GetExistingOrInitialise(It.IsAny<string>())).Returns(section104);
        var calculator = new UkTradeCalculator(section104PoolsMock.Object, tradeListMock.Object);

        // Act
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        // Assert
        result[0].Gain.ShouldBe(new WrappedMoney(-3500));
        result[0].TotalAllowableCost.ShouldBe(new WrappedMoney(5000));
        section104.Quantity.ShouldBe(75);
        section104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(2500));
    }

    [Fact]
    // If there are multiple disposals during the time window, earlier disposals have to be matched first TCGA92/S106A(5)(b)
    public void MultipleShortSaleFirstShortSaleMatchFirst()
    {
        // Arrange
        Mock<Trade> trade1Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 2, 12, 34, 56, DateTimeKind.Local), TradeType.DISPOSAL, 100, 1000);
        Mock<Trade> trade2Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 3, 12, 34, 56, DateTimeKind.Local), TradeType.DISPOSAL, 50, 800);
        Mock<Trade> trade3Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 4, 12, 34, 56, DateTimeKind.Local), TradeType.DISPOSAL, 30, 500);
        Mock<Trade> trade4Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 3, 1, 12, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 120, 1000);
        Mock<Trade> trade5Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 3, 2, 12, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 40, 300);
        Mock<Trade> trade6Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 3, 3, 12, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 20, 100);

        var tradeListMock = new Mock<ITradeAndCorporateActionList>();
        tradeListMock.Setup(t => t.Trades).Returns([trade1Mock.Object, trade2Mock.Object, trade3Mock.Object, trade4Mock.Object, trade5Mock.Object, trade6Mock.Object]);
        tradeListMock.Setup(t => t.CorporateActions).Returns([]);

        var section104PoolsMock = new Mock<UkSection104Pools>();
        UkSection104 section104 = new("Asset1");
        section104PoolsMock.Setup(i => i.GetExistingOrInitialise(It.IsAny<string>())).Returns(section104);
        var calculator = new UkTradeCalculator(section104PoolsMock.Object, tradeListMock.Object);

        // Act
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        // Assert
        result[0].Gain.Amount.ShouldBe(166.67m, 0.01m);
        result[1].Gain.Amount.ShouldBe(408.33m, 0.01m);
        result[2].Gain.Amount.ShouldBe(325, 0.01m);
    }



    // Add more unit tests for other methods in the UkTradeCalculator class

}
