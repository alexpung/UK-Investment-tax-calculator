namespace UnitTest.Test.TradeCalculations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;

using NSubstitute;

using Shouldly;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Xunit;

public class GroupedTradeContainerTests
{
    private readonly List<ITradeTaxCalculation> _tradeList;
    private readonly List<CorporateAction> _corporateActionList;
    private readonly GroupedTradeContainer<ITradeTaxCalculation> _groupedTradeContainer;

    public GroupedTradeContainerTests()
    {
        // Arrange - set up trades and corporate actions
        _tradeList =
        [
            CreateTrade("ABC", DateTime.Parse("03-Jan-23 10:00:00", CultureInfo.InvariantCulture)),
            CreateTrade("ABC", DateTime.Parse("01-Jan-23 10:00:00", CultureInfo.InvariantCulture)),
            CreateTrade("XYZ", DateTime.Parse("05-Jan-23 10:00:00", CultureInfo.InvariantCulture)),
            CreateTrade("XYZ", DateTime.Parse("01-Jan-23 10:00:00", CultureInfo.InvariantCulture))
        ];

        _corporateActionList =
        [
            CreateCorporateAction("ABC", DateTime.Parse("02-Jan-23 10:00:00", CultureInfo.InvariantCulture)),
            CreateCorporateAction("XYZ", DateTime.Parse("04-Jan-23 10:00:00", CultureInfo.InvariantCulture))
        ];

        // Instantiate the GroupedTradeContainer
        _groupedTradeContainer = new GroupedTradeContainer<ITradeTaxCalculation>(_tradeList, _corporateActionList);
    }

    [Fact]
    public void Indexer_WithExistingAssetName_ShouldReturnSortedTradeList()
    {
        // Act
        var result = _groupedTradeContainer["ABC"];

        // Assert
        result.Count.ShouldBe(2);
        result[0].Date.ShouldBe(DateTime.Parse("01-Jan-23 10:00:00", CultureInfo.InvariantCulture)); // First trade by date
        result[1].Date.ShouldBe(DateTime.Parse("03-Jan-23 10:00:00", CultureInfo.InvariantCulture)); // Second trade by date
    }

    [Fact]
    public void Indexer_WithNonExistentAssetName_ShouldReturnEmptyList()
    {
        // Act
        var result = _groupedTradeContainer["DEF"];

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetAllTradesGroupedAndSorted_ShouldReturnAllGroupedAndSortedTrades()
    {
        // Act
        var allTrades = _groupedTradeContainer.GetAllTradesGroupedAndSorted().ToList();

        // Assert
        allTrades.Count.ShouldBe(2); // Two distinct asset names: "ABC" and "XYZ"

        // Assert for ABC trades
        var abcTrades = allTrades[0];
        abcTrades.Count.ShouldBe(2);
        abcTrades[0].Date.ShouldBe(DateTime.Parse("01-Jan-23 10:00:00", CultureInfo.InvariantCulture)); // First by date
        abcTrades[1].Date.ShouldBe(DateTime.Parse("03-Jan-23 10:00:00", CultureInfo.InvariantCulture)); // Second by date

        // Assert for XYZ trades
        var xyzTrades = allTrades[1];
        xyzTrades.Count.ShouldBe(2);
        xyzTrades[0].Date.ShouldBe(DateTime.Parse("01-Jan-23 10:00:00", CultureInfo.InvariantCulture)); // First by date
        xyzTrades[1].Date.ShouldBe(DateTime.Parse("05-Jan-23 10:00:00", CultureInfo.InvariantCulture)); // Second by date
    }

    [Fact]
    public void GetAllTaxEventsGroupedAndSorted_ShouldReturnAllGroupedAndSortedEvents()
    {
        // Act
        var allEvents = _groupedTradeContainer.GetAllTaxEventsGroupedAndSorted().ToList();

        // Assert
        allEvents.Count.ShouldBe(2); // Two distinct asset names: "ABC" and "XYZ"

        // Assert for ABC events
        var abcEvents = allEvents.Single(e => e.AssetName == "ABC");
        abcEvents.Events.Count.ShouldBe(3); // Two trades + one corporate action
        abcEvents.Events[0].Date.ShouldBe(DateTime.Parse("01-Jan-23 10:00:00", CultureInfo.InvariantCulture)); // First trade
        abcEvents.Events[1].Date.ShouldBe(DateTime.Parse("02-Jan-23 10:00:00", CultureInfo.InvariantCulture)); // Corporate action
        abcEvents.Events[2].Date.ShouldBe(DateTime.Parse("03-Jan-23 10:00:00", CultureInfo.InvariantCulture)); // Second trade

        // Assert for XYZ events
        var xyzEvents = allEvents.Single(e => e.AssetName == "XYZ");
        xyzEvents.Events.Count.ShouldBe(3); // Two trades + one corporate action
        xyzEvents.Events[0].Date.ShouldBe(DateTime.Parse("01-Jan-23 10:00:00", CultureInfo.InvariantCulture)); // First trade
        xyzEvents.Events[1].Date.ShouldBe(DateTime.Parse("04-Jan-23 10:00:00", CultureInfo.InvariantCulture)); // Corporate action
        xyzEvents.Events[2].Date.ShouldBe(DateTime.Parse("05-Jan-23 10:00:00", CultureInfo.InvariantCulture)); // Second trade
    }

    // Helper methods to create mocked trades and corporate actions
    private static ITradeTaxCalculation CreateTrade(string assetName, DateTime date)
    {
        var trade = Substitute.For<ITradeTaxCalculation>();
        trade.AssetName.Returns(assetName);
        trade.Date.Returns(date);
        return trade;
    }

    private static CorporateAction CreateCorporateAction(string assetName, DateTime date)
    {
        var corporateAction = Substitute.For<CorporateAction>();
        corporateAction.AssetName.Returns(assetName);
        corporateAction.CompanyTickersInProcessingOrder.Returns([assetName]);
        corporateAction.Date.Returns(date);
        return corporateAction;
    }
}

