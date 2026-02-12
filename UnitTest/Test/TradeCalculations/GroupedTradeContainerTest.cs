namespace UnitTest.Test.TradeCalculations;

using InvestmentTaxCalculator.Enumerations;
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

    [Fact]
    public void GetAllTaxEventsGroupedAndSorted_SameDay_OrdersAdjustmentThenTradeThenReorganisation()
    {
        DateTime sameDay = DateTime.Parse("10-Jan-24 00:00:00", CultureInfo.InvariantCulture);

        ITradeTaxCalculation trade = CreateTrade("SEQ", sameDay.AddHours(9), id: 100, assetCategoryType: AssetCategoryType.STOCK, TradeReason.OrderedTrade);
        StockSplit split = new()
        {
            AssetName = "SEQ",
            Date = sameDay.AddHours(16),
            SplitTo = 2,
            SplitFrom = 1
        };
        CorporateAction reorganisation = CreateCorporateAction("SEQ", sameDay.AddHours(8));

        GroupedTradeContainer<ITradeTaxCalculation> container = new([trade], [split, reorganisation]);

        var seqEvents = container.GetAllTaxEventsGroupedAndSorted().Single(e => e.AssetName == "SEQ").Events;

        seqEvents.Count.ShouldBe(3);
        seqEvents[0].ShouldBeOfType<StockSplit>();
        seqEvents[1].ShouldBe(trade);
        seqEvents[2].ShouldBe(reorganisation);
    }

    [Fact]
    public void GetAllTaxEventsGroupedAndSorted_SameDay_OrdersOptionExpiryAfterNormalTrade()
    {
        DateTime sameDay = DateTime.Parse("11-Jan-24 00:00:00", CultureInfo.InvariantCulture);

        ITradeTaxCalculation expiryOnlyOptionEvent = CreateTrade("OPTSEQ", sameDay.AddHours(9), id: 1, assetCategoryType: AssetCategoryType.OPTION, TradeReason.Expired);
        ITradeTaxCalculation orderedOptionTrade = CreateTrade("OPTSEQ", sameDay.AddHours(16), id: 2, assetCategoryType: AssetCategoryType.OPTION, TradeReason.OrderedTrade);

        GroupedTradeContainer<ITradeTaxCalculation> container = new([expiryOnlyOptionEvent, orderedOptionTrade], []);

        var seqEvents = container.GetAllTaxEventsGroupedAndSorted().Single(e => e.AssetName == "OPTSEQ").Events;

        seqEvents.Count.ShouldBe(2);
        seqEvents[0].ShouldBe(orderedOptionTrade);
        seqEvents[1].ShouldBe(expiryOnlyOptionEvent);
    }

    // Helper methods to create mocked trades and corporate actions
    private static ITradeTaxCalculation CreateTrade(
        string assetName,
        DateTime date,
        int id = 0,
        AssetCategoryType assetCategoryType = AssetCategoryType.STOCK,
        params TradeReason[] tradeReasons)
    {
        var trade = Substitute.For<ITradeTaxCalculation>();
        trade.AssetName.Returns(assetName);
        trade.Date.Returns(date);
        trade.Id.Returns(id);
        trade.AssetCategoryType.Returns(assetCategoryType);
        trade.TradeList.Returns(CreateTradeList(assetName, date, assetCategoryType, tradeReasons));
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

    private static List<Trade> CreateTradeList(string assetName, DateTime date, AssetCategoryType assetCategoryType, IReadOnlyCollection<TradeReason> tradeReasons)
    {
        IReadOnlyCollection<TradeReason> reasons = tradeReasons.Count == 0 ? [TradeReason.OrderedTrade] : tradeReasons;
        return reasons
            .Select(reason => assetCategoryType == AssetCategoryType.OPTION
                ? CreateOptionTrade(assetName, date, reason)
                : CreateRegularTrade(assetName, date, assetCategoryType, reason))
            .ToList();
    }

    private static Trade CreateRegularTrade(string assetName, DateTime date, AssetCategoryType assetCategoryType, TradeReason reason) => new()
    {
        AssetName = assetName,
        Date = date,
        AssetType = assetCategoryType,
        AcquisitionDisposal = TradeType.ACQUISITION,
        Quantity = 1,
        TradeReason = reason,
        GrossProceed = new() { Amount = new WrappedMoney(1m) },
    };

    private static Trade CreateOptionTrade(string assetName, DateTime date, TradeReason reason) => new OptionTrade()
    {
        AssetName = assetName,
        Date = date,
        AcquisitionDisposal = TradeType.ACQUISITION,
        Quantity = 1,
        TradeReason = reason,
        GrossProceed = new() { Amount = new WrappedMoney(1m) },
        Underlying = "UNDERLYING",
        StrikePrice = new WrappedMoney(100m),
        ExpiryDate = date.Date.AddMonths(1),
        PUTCALL = PUTCALL.CALL,
        Multiplier = 100,
    };
}
