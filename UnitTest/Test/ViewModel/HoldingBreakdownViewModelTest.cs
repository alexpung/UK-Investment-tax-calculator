using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.ViewModel;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.ViewModel;

/// <summary>
/// The holding breakdown explains the quantity shown on the ERI and corporate action entry forms. Its headline
/// figure must agree with what those forms display, and its visible rows must reconcile to that figure even when
/// the change list is truncated - a breakdown that does not add up is worse than no breakdown at all.
/// </summary>
public class HoldingBreakdownViewModelTest
{
    private const string AssetName = "FUND";

    private static Trade CreateTrade(TradeType tradeType, string date, decimal quantity, decimal amount) => new()
    {
        AssetName = AssetName,
        AcquisitionDisposal = tradeType,
        Date = DateTime.Parse(date, CultureInfo.InvariantCulture),
        Quantity = quantity,
        GrossProceed = new() { Amount = new(amount) },
    };

    private static UkSection104Pools BuildPools(IEnumerable<TaxEvent> taxEvents)
    {
        TradeCalculationHelper.CalculateTrades(taxEvents, out UkSection104Pools section104Pools);
        return section104Pools;
    }

    [Fact]
    public void TestHeadlineQuantityMatchesTheSection104PoolAtTheDate()
    {
        UkSection104Pools pools = BuildPools([
            CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000, 10000m),
            CreateTrade(TradeType.DISPOSAL, "01-Jun-23 10:00:00", 400, 5000m)
        ]);

        HoldingBreakdownViewModel breakdown = HoldingBreakdownViewModel.Build(pools, AssetName, new DateOnly(2023, 12, 31));

        decimal formQuantity = pools.GetExistingOrInitialise(AssetName).GetLastSection104History(new DateOnly(2023, 12, 31))!.NewQuantity;
        breakdown.Quantity.ShouldBe(formQuantity);
        breakdown.Quantity.ShouldBe(600m);
    }

    [Fact]
    public void TestQuantityOnlyCountsMovementsUpToTheGivenDate()
    {
        UkSection104Pools pools = BuildPools([
            CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000, 10000m),
            CreateTrade(TradeType.DISPOSAL, "01-Jun-23 10:00:00", 400, 5000m)
        ]);

        HoldingBreakdownViewModel breakdown = HoldingBreakdownViewModel.Build(pools, AssetName, new DateOnly(2023, 3, 31));

        breakdown.Quantity.ShouldBe(1000m);
        breakdown.Rows.Count.ShouldBe(1);
    }

    [Fact]
    public void TestVisibleRowsReconcileToTheHeadlineQuantity()
    {
        UkSection104Pools pools = BuildPools([
            CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000, 10000m),
            CreateTrade(TradeType.DISPOSAL, "01-Jun-23 10:00:00", 400, 5000m),
            CreateTrade(TradeType.ACQUISITION, "01-Sep-23 10:00:00", 250, 3000m)
        ]);

        HoldingBreakdownViewModel breakdown = HoldingBreakdownViewModel.Build(pools, AssetName, new DateOnly(2023, 12, 31));

        breakdown.OmittedChangeCount.ShouldBe(0);
        breakdown.OpeningQuantity.ShouldBe(0m);
        (breakdown.OpeningQuantity + breakdown.Rows.Sum(row => row.Change)).ShouldBe(breakdown.Quantity);
        breakdown.Rows[^1].RunningTotal.ShouldBe(breakdown.Quantity);
    }

    [Fact]
    public void TestChangeListIsCappedAndOmittedMovementsBecomeTheOpeningBalance()
    {
        // 15 acquisitions of 100 units on distinct days: more movements than the breakdown lists.
        List<TaxEvent> trades = [.. Enumerable.Range(1, 15)
            .Select(day => (TaxEvent)CreateTrade(TradeType.ACQUISITION, $"{day:00}-Jan-23 10:00:00", 100, 1000m))];
        UkSection104Pools pools = BuildPools(trades);

        HoldingBreakdownViewModel breakdown = HoldingBreakdownViewModel.Build(pools, AssetName, new DateOnly(2023, 12, 31));

        breakdown.Rows.Count.ShouldBe(HoldingBreakdownViewModel.MaxRows);
        breakdown.OmittedChangeCount.ShouldBe(5);
        breakdown.OpeningQuantity.ShouldBe(500m); // the 5 omitted acquisitions
        breakdown.Quantity.ShouldBe(1500m);
        // The point of the opening balance: the rows on screen still add up to the headline figure.
        (breakdown.OpeningQuantity + breakdown.Rows.Sum(row => row.Change)).ShouldBe(breakdown.Quantity);
    }

    [Fact]
    public void TestCorporateActionMovementIsDescribedByItsReason()
    {
        StockSplit split = new()
        {
            AssetName = AssetName,
            Date = DateTime.Parse("01-Jun-23 00:00:00", CultureInfo.InvariantCulture),
            SplitTo = 2,
            SplitFrom = 1
        };
        UkSection104Pools pools = BuildPools([CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000, 10000m), split]);

        HoldingBreakdownViewModel breakdown = HoldingBreakdownViewModel.Build(pools, AssetName, new DateOnly(2023, 12, 31));

        breakdown.Quantity.ShouldBe(2000m);
        HoldingChangeRow splitRow = breakdown.Rows[^1];
        splitRow.Change.ShouldBe(1000m);
        splitRow.RunningTotal.ShouldBe(2000m);
        splitRow.Description.ShouldContain("Stock split 2 for 1");
        splitRow.Description.ShouldNotContain("\n");
    }

    [Fact]
    public void TestTradeMovementsAreDescribedWithoutTheMultiLineCostBreakdown()
    {
        UkSection104Pools pools = BuildPools([
            CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000, 10000m),
            CreateTrade(TradeType.DISPOSAL, "01-Jun-23 10:00:00", 400, 5000m)
        ]);

        HoldingBreakdownViewModel breakdown = HoldingBreakdownViewModel.Build(pools, AssetName, new DateOnly(2023, 12, 31));

        breakdown.Rows[0].Description.ShouldBe("Acquisition");
        breakdown.Rows[1].Description.ShouldBe("Disposal");
    }

    [Fact]
    public void TestUnknownTickerReportsNoHistoryAndDoesNotCreateAPool()
    {
        UkSection104Pools pools = BuildPools([CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000, 10000m)]);
        int poolCountBefore = pools.GetSection104s().Count;

        HoldingBreakdownViewModel breakdown = HoldingBreakdownViewModel.Build(pools, "NOT_IMPORTED", new DateOnly(2023, 12, 31));

        breakdown.HasHistory.ShouldBeFalse();
        breakdown.Quantity.ShouldBe(0m);
        breakdown.Rows.ShouldBeEmpty();
        breakdown.OmittedChangeCount.ShouldBe(0);
        // Reading a holding for a ticker the user is still typing must not leave an empty pool behind.
        pools.GetSection104s().Count.ShouldBe(poolCountBefore);
    }
}
