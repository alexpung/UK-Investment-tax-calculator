namespace UnitTest.Test.Model.UkTaxModel;

using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;

using NSubstitute;

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
        Trade trade1Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 1, 12, 34, 56, DateTimeKind.Local), tradeType1, 100, 1000);
        Trade trade2Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 1, 13, 34, 56, DateTimeKind.Local), tradeType2, 200, 2000);
        var tradeListMock = Substitute.For<ITradeAndCorporateActionList>();
        tradeListMock.Trades.Returns([trade1Mock, trade2Mock]);
        tradeListMock.CorporateActions.Returns([]);

        var section104PoolsMock = Substitute.For<UkSection104Pools>(new UKTaxYear(), new ResidencyStatusRecord());
        section104PoolsMock.GetExistingOrInitialise(Arg.Any<string>()).ReturnsForAnyArgs(assetName => new UkSection104(assetName.Arg<string>()));
        var calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104PoolsMock, tradeListMock);

        // Act
        var result = calculator.CalculateTax();

        // Assert
        section104PoolsMock.Received().GetExistingOrInitialise("Asset1");
        section104PoolsMock.Received().GetExistingOrInitialise(Arg.Any<string>());
        result.Count.ShouldBe(expectedITradeTaxCalculationCount);
        result[0].TradeList.Count.ShouldBe(expectedFirstTradeListCount);
        result[0].TradeList[0].ShouldBe(trade1Mock);
    }

    [Fact]
    public void ApplySameDayMatchingRule_MatchesSameDayTrades()
    {
        // Arrange
        Trade trade1Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 1, 12, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 100, 1000);
        Trade trade2Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 2, 13, 34, 56, DateTimeKind.Local), TradeType.DISPOSAL, 80, 8000);
        Trade trade3Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 2, 14, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 50, 2000);
        var tradeListMock = Substitute.For<ITradeAndCorporateActionList>();
        tradeListMock.Trades.Returns([trade1Mock, trade2Mock, trade3Mock]);
        tradeListMock.CorporateActions.Returns([]);

        var section104PoolsMock = Substitute.For<UkSection104Pools>(new UKTaxYear(), new ResidencyStatusRecord());
        UkSection104 section104 = new("Asset1");
        section104PoolsMock.GetExistingOrInitialise(Arg.Any<string>()).Returns(section104);
        var calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104PoolsMock, tradeListMock);

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
        Trade trade1Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 1, 12, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 100, 1000);
        Trade trade2Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 2, 06, 34, 56, DateTimeKind.Local), TradeType.DISPOSAL, 80, 8000);
        Trade trade3Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 2, 1, 12, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 50, 2500);
        Trade trade4Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 2, 2, 02, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 20, 1500);

        var tradeListMock = Substitute.For<ITradeAndCorporateActionList>();
        tradeListMock.Trades.Returns([trade1Mock, trade2Mock, trade3Mock, trade4Mock]);
        tradeListMock.CorporateActions.Returns([]);

        var section104PoolsMock = Substitute.For<UkSection104Pools>(new UKTaxYear(), new ResidencyStatusRecord());
        UkSection104 section104 = new("Asset1");
        section104PoolsMock.GetExistingOrInitialise(Arg.Any<string>()).Returns(section104);
        var calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104PoolsMock, tradeListMock);

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
        Trade trade1Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 2, 12, 34, 56, DateTimeKind.Local), TradeType.DISPOSAL, 150, 1500);
        Trade trade2Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 2, 1, 12, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 50, 2500); // Same day
        Trade trade3Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 2, 1, 12, 34, 56, DateTimeKind.Local), TradeType.DISPOSAL, 50, 1500); // Same day
        Trade trade4Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 2, 2, 12, 34, 56, DateTimeKind.Local), TradeType.DISPOSAL, 50, 2500); // bnb match
        Trade trade5Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 2, 25, 12, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 50, 1000); // bnb match
        Trade trade6Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 3, 3, 12, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 225, 7500); // should match this

        var tradeListMock = Substitute.For<ITradeAndCorporateActionList>();
        tradeListMock.Trades.Returns([trade1Mock, trade2Mock, trade3Mock, trade4Mock, trade5Mock, trade6Mock]);
        tradeListMock.CorporateActions.Returns([]);

        var section104PoolsMock = Substitute.For<UkSection104Pools>(new UKTaxYear(), new ResidencyStatusRecord());
        UkSection104 section104 = new("Asset1");
        section104PoolsMock.GetExistingOrInitialise(Arg.Any<string>()).Returns(section104);
        var calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104PoolsMock, tradeListMock);

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
        Trade trade1Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 2, 12, 34, 56, DateTimeKind.Local), TradeType.DISPOSAL, 100, 1000);
        Trade trade2Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 3, 12, 34, 56, DateTimeKind.Local), TradeType.DISPOSAL, 50, 800);
        Trade trade3Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 1, 4, 12, 34, 56, DateTimeKind.Local), TradeType.DISPOSAL, 30, 500);
        Trade trade4Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 3, 1, 12, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 120, 1000);
        Trade trade5Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 3, 2, 12, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 40, 300);
        Trade trade6Mock = MockTrade.CreateMockTrade("Asset1", new DateTime(2023, 3, 3, 12, 34, 56, DateTimeKind.Local), TradeType.ACQUISITION, 20, 100);

        var tradeListMock = Substitute.For<ITradeAndCorporateActionList>();
        tradeListMock.Trades.Returns([trade1Mock, trade2Mock, trade3Mock, trade4Mock, trade5Mock, trade6Mock]);
        tradeListMock.CorporateActions.Returns([]);

        var section104PoolsMock = Substitute.For<UkSection104Pools>(new UKTaxYear(), new ResidencyStatusRecord());
        UkSection104 section104 = new("Asset1");
        section104PoolsMock.GetExistingOrInitialise(Arg.Any<string>()).Returns(section104);
        var calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104PoolsMock, tradeListMock);

        // Act
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        // Assert
        result[0].Gain.Amount.ShouldBe(166.67m, 0.01m);
        result[1].Gain.Amount.ShouldBe(408.33m, 0.01m);
        result[2].Gain.Amount.ShouldBe(325, 0.01m);
    }



    // Add more unit tests for other methods in the UkTradeCalculator class

}
