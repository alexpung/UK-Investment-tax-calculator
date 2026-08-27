using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Services;
using InvestmentTaxCalculator.ViewModel;

using System.Globalization;

namespace UnitTest.Test.ViewModel;

/// <summary>
/// The holding breakdown explains the quantity shown on the ERI and corporate action entry forms. Its visible rows
/// must reconcile to that figure even when the change list is truncated - a breakdown that does not add up would be
/// worse than no breakdown at all.
/// </summary>
public class HoldingBreakdownViewModelTest
{
    private const string AssetName = "FUND";

    private static Trade CreateTrade(TradeType tradeType, string date, decimal quantity) => new()
    {
        AssetName = AssetName,
        AcquisitionDisposal = tradeType,
        Date = DateTime.Parse(date, CultureInfo.InvariantCulture),
        Quantity = quantity,
        GrossProceed = new() { Amount = new(quantity * 10m) },
    };

    private static HoldingsService BuildService(IEnumerable<TaxEvent> taxEvents)
    {
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData(taxEvents);
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents(taxEventLists.AllEvents);
        return new HoldingsService(taxEventLists, registry);
    }

    [Fact]
    public void TestVisibleRowsReconcileToTheHeadlineQuantity()
    {
        HoldingsService service = BuildService([
            CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000),
            CreateTrade(TradeType.DISPOSAL, "01-Jun-23 10:00:00", 400),
            CreateTrade(TradeType.ACQUISITION, "01-Sep-23 10:00:00", 250)
        ]);

        HoldingBreakdownViewModel breakdown = HoldingBreakdownViewModel.Build(service, AssetName, new DateOnly(2023, 12, 31));

        breakdown.Quantity.ShouldBe(850m);
        breakdown.OmittedChangeCount.ShouldBe(0);
        breakdown.OpeningQuantity.ShouldBe(0m);
        (breakdown.OpeningQuantity + breakdown.Rows.Sum(row => row.Change)).ShouldBe(breakdown.Quantity);
        breakdown.Rows[^1].RunningTotal.ShouldBe(breakdown.Quantity);
    }

    [Fact]
    public void TestOnlyMovementsUpToTheGivenDateAreCounted()
    {
        HoldingsService service = BuildService([
            CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000),
            CreateTrade(TradeType.DISPOSAL, "01-Jun-23 10:00:00", 400)
        ]);

        HoldingBreakdownViewModel breakdown = HoldingBreakdownViewModel.Build(service, AssetName, new DateOnly(2023, 3, 31));

        breakdown.Quantity.ShouldBe(1000m);
        breakdown.Rows.Count.ShouldBe(1);
    }

    [Fact]
    public void TestChangeListIsCappedAndOmittedMovementsBecomeTheOpeningBalance()
    {
        // 15 acquisitions of 100 units on distinct days: more movements than the breakdown lists.
        List<TaxEvent> trades = [.. Enumerable.Range(1, 15)
            .Select(day => (TaxEvent)CreateTrade(TradeType.ACQUISITION, $"{day:00}-Jan-23 10:00:00", 100))];
        HoldingsService service = BuildService(trades);

        HoldingBreakdownViewModel breakdown = HoldingBreakdownViewModel.Build(service, AssetName, new DateOnly(2023, 12, 31));

        breakdown.Rows.Count.ShouldBe(HoldingBreakdownViewModel.MaxRows);
        breakdown.OmittedChangeCount.ShouldBe(5);
        breakdown.OpeningQuantity.ShouldBe(500m); // the 5 omitted acquisitions
        breakdown.Quantity.ShouldBe(1500m);
        // The point of the opening balance: the rows on screen still add up to the headline figure.
        (breakdown.OpeningQuantity + breakdown.Rows.Sum(row => row.Change)).ShouldBe(breakdown.Quantity);
    }

    [Fact]
    public void TestSellingMoreThanWasEverAcquiredIsFlagged()
    {
        // The acquisition is missing from the import, so the holding goes negative. Almost always a partial import
        // rather than a genuine short position.
        HoldingsService service = BuildService([CreateTrade(TradeType.DISPOSAL, "01-Jun-23 10:00:00", 400)]);

        HoldingBreakdownViewModel breakdown = HoldingBreakdownViewModel.Build(service, AssetName, new DateOnly(2023, 12, 31));

        breakdown.HasNegativeHolding.ShouldBeTrue();
        breakdown.Quantity.ShouldBe(-400m);
    }

    [Fact]
    public void TestUnknownTickerReportsNoHistory()
    {
        HoldingsService service = BuildService([CreateTrade(TradeType.ACQUISITION, "01-Jan-23 10:00:00", 1000)]);

        HoldingBreakdownViewModel breakdown = HoldingBreakdownViewModel.Build(service, "NOT_IMPORTED", new DateOnly(2023, 12, 31));

        breakdown.HasHistory.ShouldBeFalse();
        breakdown.Quantity.ShouldBe(0m);
        breakdown.Rows.ShouldBeEmpty();
        breakdown.OmittedChangeCount.ShouldBe(0);
        breakdown.HasNegativeHolding.ShouldBeFalse();
    }
}
