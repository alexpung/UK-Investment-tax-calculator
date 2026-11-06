using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations.Stocks;

/// <summary>
/// Keeps the worked examples on the "How it works" page (Pages/HowItWorksPage.razor) in step with the calculator.
/// If one of these fails, update the figures on that page as well as the test.
/// </summary>
public class HowItWorksPageExamplesTest
{
    private static Trade GbpTrade(TradeType side, string date, decimal quantity, decimal gross, decimal fee) => new()
    {
        AssetName = "ABC",
        AcquisitionDisposal = side,
        Date = DateTime.Parse(date, CultureInfo.InvariantCulture),
        Description = "ABC Example Stock",
        Quantity = quantity,
        Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(fee, "GBP"), FxRate = 1m }],
        GrossProceed = new() { Description = "", Amount = new(gross, "GBP"), FxRate = 1m },
    };

    [Fact]
    public void SameDayExample_CombinesTheDaysTradesAndSplitsNetProceedsByQuantity()
    {
        List<Trade> trades =
        [
            GbpTrade(TradeType.ACQUISITION, "01-Jan-24 10:00:00", 100, 990m, 10m),
            GbpTrade(TradeType.DISPOSAL, "10-Jun-24 09:00:00", 40, 600m, 5m),
            GbpTrade(TradeType.ACQUISITION, "10-Jun-24 11:00:00", 50, 700m, 10m),
            GbpTrade(TradeType.DISPOSAL, "10-Jun-24 15:00:00", 40, 600m, 5m),
        ];

        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(trades, out UkSection104Pools section104Pools);

        ITradeTaxCalculation disposal = result.Single(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL);
        disposal.TotalQty.ShouldBe(80);
        disposal.TotalCostOrProceed.ShouldBe(new WrappedMoney(1190m));

        TradeMatch sameDay = disposal.MatchHistory.Single(match => match.TradeMatchType == TaxMatchType.SAME_DAY);
        sameDay.MatchDisposalQty.ShouldBe(50);
        sameDay.BaseCurrencyMatchDisposalProceed.ShouldBe(new WrappedMoney(743.75m));
        sameDay.BaseCurrencyMatchAllowableCost.ShouldBe(new WrappedMoney(710m));

        TradeMatch pool = disposal.MatchHistory.Single(match => match.TradeMatchType == TaxMatchType.SECTION_104);
        pool.MatchDisposalQty.ShouldBe(30);
        pool.BaseCurrencyMatchDisposalProceed.ShouldBe(new WrappedMoney(446.25m));
        pool.BaseCurrencyMatchAllowableCost.ShouldBe(new WrappedMoney(300m));

        disposal.TotalProceeds.ShouldBe(new WrappedMoney(1190m));
        disposal.TotalAllowableCost.ShouldBe(new WrappedMoney(1010m));
        disposal.Gain.ShouldBe(new WrappedMoney(180m));
        section104Pools.GetExistingOrInitialise("ABC").Quantity.ShouldBe(70);
        section104Pools.GetExistingOrInitialise("ABC").AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(700m));
    }

    [Fact]
    public void ThirtyDayExample_MatchesTheRepurchaseAndLeavesThePoolUnchanged()
    {
        List<Trade> trades =
        [
            GbpTrade(TradeType.ACQUISITION, "01-May-24 10:00:00", 1000, 4990m, 10m),
            GbpTrade(TradeType.DISPOSAL, "03-Mar-25 10:00:00", 1000, 4000m, 10m),
            GbpTrade(TradeType.ACQUISITION, "20-Mar-25 10:00:00", 1000, 4100m, 10m),
        ];

        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(trades, out UkSection104Pools section104Pools);

        ITradeTaxCalculation disposal = result.Single(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL);
        disposal.MatchHistory.ShouldHaveSingleItem().TradeMatchType.ShouldBe(TaxMatchType.BED_AND_BREAKFAST);
        disposal.TotalProceeds.ShouldBe(new WrappedMoney(3990m));
        disposal.TotalAllowableCost.ShouldBe(new WrappedMoney(4110m));
        disposal.Gain.ShouldBe(new WrappedMoney(-120m));
        section104Pools.GetExistingOrInitialise("ABC").Quantity.ShouldBe(1000);
        section104Pools.GetExistingOrInitialise("ABC").AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(5000m));
    }
}
