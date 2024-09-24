using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel.Options;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations.Options;
public class UkTradeCalculatorOptionExerciseTest
{
    [Theory]
    [InlineData("05-Dec-22 09:30:00")]
    [InlineData("05-Jan-23 09:30:00")]
    [InlineData("25-Jan-23 09:30:00")]
    public void LongPutOptionBoughtAndExercised(string optionDate)
    {
        var buyStockTrade = new Trade
        {
            AssetName = "AAPL",
            Date = DateTime.Parse("01-Jan-23 09:30:00", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new DescribedMoney(15000, "USD", 1),
            Expenses = [new DescribedMoney(10, "USD", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Buy 100 shares of AAPL"
        };

        var buyPutOptionTrade = new OptionTrade
        {
            AssetName = "AAPL230125P00140000",
            Date = DateTime.Parse(optionDate, CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(140),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.PUT,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(500, "USD", 1),
            Expenses = [new DescribedMoney(5, "USD", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Buy AAPL Put Option (140 Strike Price)"
        };

        var exercisePutOptionTrade = new OptionTrade
        {
            AssetName = "AAPL230125P00140000",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(140),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.PUT,
            Multiplier = 100,
            TradeReason = TradeReason.OwnerExeciseOption,
            Quantity = 1,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "AAPL Put Option Exercised",
        };

        var sellStockTrade = new Trade
        {
            AssetName = "AAPL",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new DescribedMoney(14000, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Execise put to sell 100 shares of AAPL",
            TradeReason = TradeReason.OwnerExeciseOption
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(
            [buyStockTrade, buyPutOptionTrade, exercisePutOptionTrade, sellStockTrade],
            out _
        );
        var optionDisposalTrade = result.Find(trade => trade is OptionTradeTaxCalculation { AcquisitionDisposal: TradeType.DISPOSAL });
        optionDisposalTrade!.TotalAllowableCost.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        optionDisposalTrade.TotalProceeds.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        optionDisposalTrade.Gain.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        var stockDisposalTrade = result.Find(trade => trade is TradeTaxCalculation { AcquisitionDisposal: TradeType.DISPOSAL, TotalQty: 100 });
        stockDisposalTrade!.TotalAllowableCost.ShouldBe(new WrappedMoney(15010));
        stockDisposalTrade.TotalProceeds.ShouldBe(new WrappedMoney(13495)); // 14000 - 500 - 5
        stockDisposalTrade.Gain.ShouldBe(new WrappedMoney(-1515));
    }

    [Theory]
    [InlineData("05-Dec-22 09:30:00")]
    [InlineData("05-Jan-23 09:30:00")]
    [InlineData("25-Jan-23 09:30:00")]
    public void LongCallOptionBoughtExercisedAndStockDisposed(string optionDate)
    {
        var buyCallOptionTrade = new OptionTrade
        {
            AssetName = "AAPL230225C00160000",  // OCC Option Symbol for Call
            Date = DateTime.Parse(optionDate, CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(160),
            ExpiryDate = DateTime.Parse("25-Feb-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(700, "USD", 1),
            Expenses = [new DescribedMoney(5, "USD", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Buy AAPL Call Option (160 Strike Price)"
        };

        var exerciseCallOptionTrade = new OptionTrade
        {
            AssetName = "AAPL230225C00160000",
            Date = DateTime.Parse("25-Feb-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(160),
            ExpiryDate = DateTime.Parse("25-Feb-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OwnerExeciseOption,
            Quantity = 1,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "AAPL Call Option Exercised",
        };

        var execiseStockTrade = new Trade
        {
            AssetName = "AAPL",
            Date = DateTime.Parse("25-Feb-23 16:00:00", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new DescribedMoney(16000, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Execise call to buy 100 shares at $160",
            TradeReason = TradeReason.OwnerExeciseOption
        };

        var sellStockTrade = new Trade
        {
            AssetName = "AAPL",
            Date = DateTime.Parse("06-Apr-23 09:30:00", CultureInfo.InvariantCulture),  // 40 days later
            Quantity = 100,
            GrossProceed = new DescribedMoney(17000, "USD", 1),
            Expenses = [new DescribedMoney(10, "USD", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Sell 100 shares of AAPL after 40 days",
            TradeReason = TradeReason.OrderedTrade
        };

        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(
            [buyCallOptionTrade, exerciseCallOptionTrade, execiseStockTrade, sellStockTrade],
            out _
        );

        var optionDisposalTrade = result.Find(trade => trade is OptionTradeTaxCalculation { AcquisitionDisposal: TradeType.DISPOSAL });
        optionDisposalTrade!.TotalAllowableCost.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        optionDisposalTrade.TotalProceeds.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        optionDisposalTrade.Gain.ShouldBe(WrappedMoney.GetBaseCurrencyZero());

        var stockDisposalTrade = result.Find(trade => trade is TradeTaxCalculation { AcquisitionDisposal: TradeType.DISPOSAL, TotalQty: 100 });
        stockDisposalTrade!.TotalAllowableCost.ShouldBe(new WrappedMoney(16705));  // 16000 (strike price) + 705 (option expense)
        stockDisposalTrade.TotalProceeds.ShouldBe(new WrappedMoney(16990));  // 17000 - 10 (sale expense)
        stockDisposalTrade.Gain.ShouldBe(new WrappedMoney(285));  // Profit from stock sale after option exercise
    }
}
