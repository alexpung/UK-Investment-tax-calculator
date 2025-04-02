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
            TradeReason = TradeReason.OwnerExerciseOption,
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
            Description = "Exercise put to sell 100 shares of AAPL",
            TradeReason = TradeReason.OwnerExerciseOption
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
            TradeReason = TradeReason.OwnerExerciseOption,
            Quantity = 1,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [new DescribedMoney(10, "USD", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "AAPL Call Option Exercised",
        };

        var exerciseStockTrade = new Trade
        {
            AssetName = "AAPL",
            Date = DateTime.Parse("25-Feb-23 16:00:00", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new DescribedMoney(16000, "USD", 1),
            Expenses = [new DescribedMoney(5, "USD", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Exercise call to buy 100 shares at $160",
            TradeReason = TradeReason.OwnerExerciseOption
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
            [buyCallOptionTrade, exerciseCallOptionTrade, exerciseStockTrade, sellStockTrade],
            out _
        );

        var optionDisposalTrade = result.Find(trade => trade is OptionTradeTaxCalculation { AcquisitionDisposal: TradeType.DISPOSAL });
        optionDisposalTrade!.TotalAllowableCost.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        optionDisposalTrade.TotalProceeds.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        optionDisposalTrade.Gain.ShouldBe(WrappedMoney.GetBaseCurrencyZero());

        var stockDisposalTrade = result.Find(trade => trade is TradeTaxCalculation { AcquisitionDisposal: TradeType.DISPOSAL, TotalQty: 100 });
        stockDisposalTrade!.TotalAllowableCost.ShouldBe(new WrappedMoney(16720));
        // 16000 (strike price) + 705 (option expense) + 10 (option execise expense) + 5 (stock acquisition expense)
        stockDisposalTrade.TotalProceeds.ShouldBe(new WrappedMoney(16990));  // 17000 - 10 (sale expense)
        stockDisposalTrade.Gain.ShouldBe(new WrappedMoney(270));  // Profit from stock sale after option exercise
    }

    [Fact]
    public void LongCallOptionBoughtAndExercisedWithExeciseCost()
    {
        var buyCallOptionTrade = new OptionTrade
        {
            AssetName = "GOOG180119C00800000",
            Date = DateTime.Parse("01-Jan-18 09:30:00", CultureInfo.InvariantCulture),
            Underlying = "GOOG",
            StrikePrice = new WrappedMoney(800),
            ExpiryDate = DateTime.Parse("19-Jan-18 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(502, "GBP", 1),
            Expenses = [new DescribedMoney(1, "GBP", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Buy GOOG Call Option (800 Strike Price)"
        };

        var exerciseCallOptionTrade = new OptionTrade
        {
            AssetName = "GOOG180119C00800000",
            Date = DateTime.Parse("19-Jan-18 16:20:00", CultureInfo.InvariantCulture),
            Underlying = "GOOG",
            StrikePrice = new WrappedMoney(800),
            ExpiryDate = DateTime.Parse("19-Jan-18 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OwnerExerciseOption,
            Quantity = 1,
            GrossProceed = new DescribedMoney(0, "GBP", 1),
            Expenses = [new DescribedMoney(1, "GBP", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "GOOG Call Option Exercised",
        };

        var exerciseStockTrade = new Trade
        {
            AssetName = "GOOG",
            Date = DateTime.Parse("19-Jan-18 16:20:00", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new DescribedMoney(80000, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Exercise call to buy 100 shares at $800",
            TradeReason = TradeReason.OwnerExerciseOption
        };

        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(
           [buyCallOptionTrade, exerciseCallOptionTrade, exerciseStockTrade],
           out _
       );

        var optionDisposalTrade = result.Find(trade => trade is OptionTradeTaxCalculation { AcquisitionDisposal: TradeType.DISPOSAL });
        optionDisposalTrade!.TotalAllowableCost.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        optionDisposalTrade.TotalProceeds.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        optionDisposalTrade.Gain.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        var tradeAcquisitionTrade = result.Find(trade => trade.AssetCategoryType is AssetCategoryType.STOCK);
        tradeAcquisitionTrade!.TotalCostOrProceed.Amount.ShouldBe(80504);
    }

    [Theory]
    [InlineData("01-Jan-17 09:30:00", 501, 502)]
    [InlineData("01-Dec-17 09:30:00", 0, 0)]
    [InlineData("01-Jan-18 09:30:00", 0, 0)]
    public void ShortCallOptionWrittenAndAssignedWithExerciseCost(string writeCallOptionDateString, decimal expectedTotalProceedAmount, decimal refundAmount)
    {
        var BuyStockTrade = new Trade
        {
            AssetName = "GOOG",
            Date = DateTime.Parse("1-Jan-18 16:20:00", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new DescribedMoney(70000, "USD", 1),
            Expenses = [new DescribedMoney(2, "GBP", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Buy 100 shares of GOOG at $700",
            TradeReason = TradeReason.OptionAssigned
        };

        var writeCallOptionTrade = new OptionTrade
        {
            AssetName = "GOOG180119C00800000",
            Date = DateTime.Parse(writeCallOptionDateString, CultureInfo.InvariantCulture),
            Underlying = "GOOG",
            StrikePrice = new WrappedMoney(800),
            ExpiryDate = DateTime.Parse("19-Jan-18 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(502, "GBP", 1),
            Expenses = [new DescribedMoney(1, "GBP", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Write GOOG Call Option (800 Strike Price)"
        };

        var assignCallOptionTrade = new OptionTrade
        {
            AssetName = "GOOG180119C00800000",
            Date = DateTime.Parse("19-Jan-18 16:20:00", CultureInfo.InvariantCulture),
            Underlying = "GOOG",
            StrikePrice = new WrappedMoney(800),
            ExpiryDate = DateTime.Parse("19-Jan-18 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OptionAssigned,
            Quantity = 1,
            GrossProceed = new DescribedMoney(0, "GBP", 1),
            Expenses = [new DescribedMoney(1, "GBP", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "GOOG Call Option Assigned",
        };

        var sellStockTrade = new Trade
        {
            AssetName = "GOOG",
            Date = DateTime.Parse("19-Jan-18 16:20:00", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new DescribedMoney(80000, "USD", 1),
            Expenses = [new DescribedMoney(4, "GBP", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Sell 100 shares of GOOG at $800",
            TradeReason = TradeReason.OptionAssigned
        };

        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(
            [BuyStockTrade, writeCallOptionTrade, assignCallOptionTrade, sellStockTrade],
            out _
        );

        var optionDisposalTrade = result.Find(trade => trade is OptionTradeTaxCalculation { AcquisitionDisposal: TradeType.DISPOSAL });
        optionDisposalTrade!.TotalAllowableCost.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        optionDisposalTrade.TotalProceeds.Amount.ShouldBe(expectedTotalProceedAmount);
        optionDisposalTrade.Gain.Amount.ShouldBe(expectedTotalProceedAmount);
        ((OptionTradeTaxCalculation)optionDisposalTrade).TaxRepayList.Sum(taxRepay => taxRepay.RefundAmount.Amount).ShouldBe(refundAmount);

        var stockDisposalTrade = result.Find(trade => trade is TradeTaxCalculation { AcquisitionDisposal: TradeType.DISPOSAL, TotalQty: 100 });
        stockDisposalTrade!.TotalAllowableCost.ShouldBe(new WrappedMoney(70002));
        stockDisposalTrade.TotalProceeds.ShouldBe(new WrappedMoney(80000 + 503 - 1 - 4));
        stockDisposalTrade.Gain.ShouldBe(new WrappedMoney(10496));
    }


}
