using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Services;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using System.Globalization;

namespace UnitTest.Test.Services;

/// <summary>
/// The entry forms show a quantity as pending until a calculation has run, and prompt for an update once the
/// imported data has moved on, so both signals have to be right.
/// </summary>
public class TaxCalculationServiceStateTest
{
    private static Trade CreateTrade(string date, decimal quantity) => new()
    {
        AssetName = "FUND",
        AcquisitionDisposal = TradeType.ACQUISITION,
        Date = DateTime.Parse(date, CultureInfo.InvariantCulture),
        Quantity = quantity,
        GrossProceed = new() { Amount = new(quantity * 10m) },
    };

    private static TaxCalculationService CreateService(TaxEventLists taxEventLists, IEnumerable<ITradeCalculator>? tradeCalculators = null)
    {
        UKTaxYear taxYear = new();
        ResidencyStatusRecord residencyStatusRecord = new();
        ShareIdentityRegistry shareIdentityRegistry = new();
        IDividendCalculator dividendCalculator = Substitute.For<IDividendCalculator>();
        dividendCalculator.CalculateTax().Returns([]);
        return new TaxCalculationService(
            new UkSection104Pools(taxYear, residencyStatusRecord, shareIdentityRegistry),
            dividendCalculator,
            new DividendCalculationResult(),
            new TradeCalculationResult(taxYear, residencyStatusRecord),
            tradeCalculators ?? [],
            new YearOptions(),
            taxYear,
            new ToastService(NullLogger<ToastService>.Instance),
            taxEventLists,
            shareIdentityRegistry);
    }

    [Fact]
    public async Task TestQuantitiesArePendingUntilACalculationHasRun()
    {
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([CreateTrade("01-Jan-23 10:00:00", 1000)]);
        TaxCalculationService service = CreateService(taxEventLists);

        service.HasCalculated.ShouldBeFalse();
        service.IsResultStale.ShouldBeFalse(); // nothing calculated yet, so nothing to be stale

        await service.CalculateAsync();

        service.HasCalculated.ShouldBeTrue();
        service.IsResultStale.ShouldBeFalse();
    }

    [Fact]
    public async Task TestAddingAnEventAfterCalculatingMakesTheResultStale()
    {
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([CreateTrade("01-Jan-23 10:00:00", 1000)]);
        TaxCalculationService service = CreateService(taxEventLists);
        await service.CalculateAsync();

        taxEventLists.AddData([CreateTrade("01-Jun-23 10:00:00", 500)]);
        service.IsResultStale.ShouldBeTrue();

        await service.CalculateAsync();
        service.IsResultStale.ShouldBeFalse();
    }

    [Fact]
    public async Task TestEditingAnEventMakesTheResultStaleEvenThoughTheCountIsUnchanged()
    {
        // Editing a corporate action removes the old entry and adds a replacement, so a count alone would miss it.
        TaxEventLists taxEventLists = new();
        StockSplit original = new()
        {
            AssetName = "FUND",
            Date = DateTime.Parse("01-Jun-23 00:00:00", CultureInfo.InvariantCulture),
            SplitTo = 2,
            SplitFrom = 1
        };
        taxEventLists.AddData([CreateTrade("01-Jan-23 10:00:00", 1000), original]);
        TaxCalculationService service = CreateService(taxEventLists);
        await service.CalculateAsync();
        service.IsResultStale.ShouldBeFalse();

        taxEventLists.CorporateActions.Remove(original);
        taxEventLists.CorporateActions.Add(original with { SplitTo = 3 });

        taxEventLists.GetTotalNumberOfEvents().ShouldBe(2);
        service.IsResultStale.ShouldBeTrue();
    }

    [Fact]
    public async Task TestAnEventAddedWhileCalculatingLeavesTheResultStale()
    {
        // The calculators read the event lists when they run, so an entry the user submits mid calculation is not in
        // the results. Standing in for that timing: a calculator that adds an event while the calculation is running.
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([CreateTrade("01-Jan-23 10:00:00", 1000)]);

        ITradeCalculator lateAddingCalculator = Substitute.For<ITradeCalculator>();
        lateAddingCalculator.CalculateTax().Returns(_ =>
        {
            taxEventLists.AddData([CreateTrade("01-Jun-23 10:00:00", 500)]);
            return [];
        });

        TaxCalculationService service = CreateService(taxEventLists, [lateAddingCalculator]);
        await service.CalculateAsync();

        service.HasCalculated.ShouldBeTrue();
        service.IsResultStale.ShouldBeTrue();
    }

    [Fact]
    public async Task TestAFailedRecalculationDoesNotLeaveTheEarlierResultReportedAsCurrent()
    {
        // The pools are cleared before the calculators run, so a failed recalculation leaves nothing to read even
        // though the events are unchanged and the earlier run succeeded.
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([CreateTrade("01-Jan-23 10:00:00", 1000)]);

        bool shouldThrow = false;
        ITradeCalculator calculator = Substitute.For<ITradeCalculator>();
        calculator.CalculateTax().Returns(_ => shouldThrow ? throw new InvalidOperationException("boom") : []);

        TaxCalculationService service = CreateService(taxEventLists, [calculator]);
        await service.CalculateAsync();
        service.HasCurrentResult.ShouldBeTrue();

        shouldThrow = true;
        await service.CalculateAsync();

        service.HasCalculated.ShouldBeFalse();
        service.IsResultStale.ShouldBeFalse(); // the events never changed, so staleness alone would not catch this
        service.HasCurrentResult.ShouldBeFalse();
    }

    [Fact]
    public async Task TestAStaleResultIsNotReportedAsCurrent()
    {
        // The pools still hold the previous run until the next one, so anything reading a quantity out of them has
        // to treat a stale result as no result.
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([CreateTrade("01-Jan-23 10:00:00", 1000)]);
        TaxCalculationService service = CreateService(taxEventLists);
        await service.CalculateAsync();
        service.HasCurrentResult.ShouldBeTrue();

        taxEventLists.AddData([CreateTrade("01-Jun-23 10:00:00", 500)]);

        service.HasCalculated.ShouldBeTrue();
        service.HasCurrentResult.ShouldBeFalse();

        await service.CalculateAsync();
        service.HasCurrentResult.ShouldBeTrue();
    }

    [Fact]
    public async Task TestRemovingAnEventAfterCalculatingMakesTheResultStale()
    {
        TaxEventLists taxEventLists = new();
        Trade trade = CreateTrade("01-Jan-23 10:00:00", 1000);
        taxEventLists.AddData([trade, CreateTrade("01-Jun-23 10:00:00", 500)]);
        TaxCalculationService service = CreateService(taxEventLists);
        await service.CalculateAsync();

        taxEventLists.Trades.Remove(trade);

        service.IsResultStale.ShouldBeTrue();
    }
}
