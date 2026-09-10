using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.ViewModel;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations.Stocks;

/// <summary>
/// An entry form editing a corporate action reads Section 104 pools that already have that action applied to them,
/// which previews a split as if it happened twice and a takeover as a holding of zero. The pool records the state
/// immediately before each movement, so the action's own entry carries the basis the form needs.
/// </summary>
public class UkSection104QuantityBeforeActionTest
{
    private const string AssetName = "SHARE";

    private static Trade CreateTrade(TradeType tradeType, string date, decimal quantity, string assetName = AssetName) => new()
    {
        AssetName = assetName,
        AcquisitionDisposal = tradeType,
        Date = DateTime.Parse(date, CultureInfo.InvariantCulture),
        Quantity = quantity,
        GrossProceed = new() { Amount = new(quantity * 10m) },
    };

    private static UkSection104Pools BuildPools(IEnumerable<TaxEvent> taxEvents)
    {
        TradeCalculationHelper.CalculateTrades(taxEvents, out UkSection104Pools section104Pools);
        return section104Pools;
    }

    [Fact]
    public void TestStockSplitReportsTheHoldingBeforeItRatherThanTheMultipliedPool()
    {
        StockSplit split = new()
        {
            AssetName = AssetName,
            Date = DateTime.Parse("01-Jun-23 00:00:00", CultureInfo.InvariantCulture),
            SplitTo = 2,
            SplitFrom = 1
        };
        UkSection104Pools pools = BuildPools([CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000), split]);
        UkSection104 pool = pools.GetExistingOrInitialise(AssetName);

        // What the form used to read: the pool on the split's own date, with the split already applied.
        pool.GetLastSection104History(new DateOnly(2023, 6, 1))!.NewQuantity.ShouldBe(2000m);
        // What it needs: 1000 before, 2000 after, rather than previewing 2000 -> 4000.
        pool.GetQuantityBefore(split).ShouldBe(1000m);
    }

    [Fact]
    public void TestSameDayTradeOrderedAfterTheActionIsNotPartOfItsBasis()
    {
        // The preview is "what you held going into the split". A purchase the matching rules order after the split
        // was never split, so it must not appear in the basis - even though it shares the split's date.
        StockSplit split = new()
        {
            AssetName = AssetName,
            Date = DateTime.Parse("01-Jun-23 00:00:00", CultureInfo.InvariantCulture),
            SplitTo = 2,
            SplitFrom = 1
        };
        Trade sameDayPurchase = CreateTrade(TradeType.ACQUISITION, "01-Jun-23 14:00:00", 100);
        UkSection104Pools pools = BuildPools([CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000), split, sameDayPurchase]);
        UkSection104 pool = pools.GetExistingOrInitialise(AssetName);

        pool.GetQuantityBefore(split).ShouldBe(1000m);
        // The end of day figure does include it, which is why the basis cannot be read from there.
        pool.GetLastSection104History(new DateOnly(2023, 6, 1))!.NewQuantity.ShouldBe(2100m);
    }

    [Fact]
    public void TestTakeoverReportsTheHoldingBeforeItRatherThanTheEmptiedPool()
    {
        TakeoverCorporateAction takeover = new()
        {
            AssetName = "OLDCO",
            Date = DateTime.Parse("01-Jun-23 00:00:00", CultureInfo.InvariantCulture),
            AcquiringCompanyTicker = "NEWCO",
            OldToNewRatio = 0.5m
        };
        UkSection104Pools pools = BuildPools([CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000, "OLDCO"), takeover]);
        UkSection104 pool = pools.GetExistingOrInitialise("OLDCO");

        // The takeover clears the pool, so reading its own date gives zero expected new shares.
        pool.GetLastSection104History(new DateOnly(2023, 6, 1))!.NewQuantity.ShouldBe(0m);
        pool.GetQuantityBefore(takeover).ShouldBe(1000m);
    }

    [Fact]
    public void TestGiftToPartnerReportsTheHoldingBeforeTheGiftWasDeducted()
    {
        PartnerTransferCorporateAction gift = new()
        {
            AssetName = AssetName,
            Date = DateTime.Parse("01-Jun-23 00:00:00", CultureInfo.InvariantCulture),
            Direction = PartnerTransferDirection.GiftToPartner,
            Quantity = 300
        };
        UkSection104Pools pools = BuildPools([CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000), gift]);
        UkSection104 pool = pools.GetExistingOrInitialise(AssetName);

        // Validating an increased gift against 700 is what rejected raising a 300 gift to 800.
        pool.GetLastSection104History(new DateOnly(2023, 6, 1))!.NewQuantity.ShouldBe(700m);
        pool.GetQuantityBefore(gift).ShouldBe(1000m);
    }

    [Fact]
    public void TestAnActionThatMovedNothingHasNoRecordedBasis()
    {
        // The split returns early on an empty pool, so nothing is stamped and there is no basis to report.
        StockSplit splitOnEmptyPool = new()
        {
            AssetName = AssetName,
            Date = DateTime.Parse("01-Jan-22 00:00:00", CultureInfo.InvariantCulture),
            SplitTo = 2,
            SplitFrom = 1
        };
        UkSection104Pools pools = BuildPools([splitOnEmptyPool, CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000)]);

        pools.GetExistingOrInitialise(AssetName).GetQuantityBefore(splitOnEmptyPool).ShouldBeNull();
    }

    [Fact]
    public void TestAnActionIsOnlyABasisForThePoolItMoved()
    {
        // A takeover stamps entries on both the old and the acquiring pool; each one answers for itself.
        TakeoverCorporateAction takeover = new()
        {
            AssetName = "OLDCO",
            Date = DateTime.Parse("01-Jun-23 00:00:00", CultureInfo.InvariantCulture),
            AcquiringCompanyTicker = "NEWCO",
            OldToNewRatio = 0.5m
        };
        UkSection104Pools pools = BuildPools([CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000, "OLDCO"), takeover]);

        pools.GetExistingOrInitialise("OLDCO").GetQuantityBefore(takeover).ShouldBe(1000m);
        // The acquiring pool held nothing before the takeover filled it.
        pools.GetExistingOrInitialise("NEWCO").GetQuantityBefore(takeover).ShouldBe(0m);
    }

    [Fact]
    public void TestPreviewUsesThePoolWhenRecordingAndThePriorStateWhenEditing()
    {
        StockSplit split = new()
        {
            AssetName = AssetName,
            Date = DateTime.Parse("01-Jun-23 00:00:00", CultureInfo.InvariantCulture),
            SplitTo = 2,
            SplitFrom = 1
        };
        UkSection104Pools pools = BuildPools([CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000), split]);
        DateTime splitDate = DateTime.Parse("01-Jun-23 00:00:00", CultureInfo.InvariantCulture);

        HoldingPreview.GetQuantity(pools, AssetName, splitDate, actionBeingEdited: null).ShouldBe(2000m);
        HoldingPreview.GetQuantity(pools, AssetName, splitDate, split).ShouldBe(1000m);
    }

    [Fact]
    public void TestPreviewHasNoAnswerWhenAnActionBeingEditedIsMovedToAnotherDate()
    {
        // Where the action would sort among the new date's trades is only settled by a calculation, so the form is
        // told there is nothing to show rather than being handed the basis from the old date.
        StockSplit split = new()
        {
            AssetName = AssetName,
            Date = DateTime.Parse("01-Jun-23 00:00:00", CultureInfo.InvariantCulture),
            SplitTo = 2,
            SplitFrom = 1
        };
        UkSection104Pools pools = BuildPools([CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000), split]);

        HoldingPreview.GetQuantity(pools, AssetName, DateTime.Parse("01-Jul-23 00:00:00", CultureInfo.InvariantCulture), split).ShouldBeNull();
    }

    [Fact]
    public void TestPreviewReportsZeroForATickerWithNoPool()
    {
        UkSection104Pools pools = BuildPools([CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000)]);

        HoldingPreview.GetQuantity(pools, "NOT_IMPORTED", DateTime.Parse("01-Jun-23 00:00:00", CultureInfo.InvariantCulture), null).ShouldBe(0m);
        HoldingPreview.GetQuantity(pools, assetName: null, DateTime.Parse("01-Jun-23 00:00:00", CultureInfo.InvariantCulture), null).ShouldBe(0m);
    }
}
