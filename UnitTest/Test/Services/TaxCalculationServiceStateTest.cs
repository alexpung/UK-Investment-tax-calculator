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

    private static TaxCalculationService CreateService(TaxEventLists taxEventLists)
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
            [],
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
