using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Options;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations.Options;
public class UkTradeCalculatorOptionMixedTest
{

    [Fact]
    public void LongOptionMixedTest()
    {
        var buyOptionTrade = new OptionTrade
        {
            AssetName = "AAPL 200123C00100000",
            Date = DateTime.Parse("05-Dec-22 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(100),
            ExpiryDate = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 2,
            GrossProceed = new DescribedMoney(1000, "USD", 0.78m),
            Expenses = [new DescribedMoney(5, "USD", 0.78m)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Buy AAPL 125 Call Option"
        };

        var buyOptionTrade2 = new OptionTrade
        {
            AssetName = "AAPL 200123C00100000",
            Date = DateTime.Parse("05-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(100),
            ExpiryDate = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 4,
            GrossProceed = new DescribedMoney(1500, "USD", 0.85m),
            Expenses = [new DescribedMoney(4, "USD", 0.85m)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Buy AAPL 125 Call Option #2"
        };

        var sellOptionTrade = new OptionTrade
        {
            AssetName = "AAPL 200123C00100000",
            Date = DateTime.Parse("20-Jan-23 12:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(100),
            ExpiryDate = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(200, "USD", 0.8m),
            Expenses = [new DescribedMoney(2, "USD", 0.8m)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Sell AAPL 125 Call Option"
        };

        var exercisedOptionTrade = new OptionTrade
        {
            AssetName = "AAPL 200123C00100000",
            Date = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(100),
            ExpiryDate = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OwnerExerciseOption,
            Quantity = 2,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "AAPL 125 Call Option exercised",
        };

        var exerciseUnderlyingBuyTrade = new Trade
        {
            AssetName = "AAPL",
            Date = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            TradeReason = TradeReason.OwnerExerciseOption,
            Quantity = 200,
            GrossProceed = new DescribedMoney(20000, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "AAPL 125 Call Option exercised",
        };

        var expireOptionTrade = new OptionTrade
        {
            AssetName = "AAPL 200123C00100000",
            Date = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(100),
            ExpiryDate = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.Expired,
            Quantity = 3,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "AAPL 125 Call Option Expired",
        };

        var sellUnderlyingTrade = new Trade
        {
            AssetName = "AAPL",
            Date = DateTime.Parse("25-Jan-23 13:00:00", CultureInfo.InvariantCulture),
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 200,
            GrossProceed = new DescribedMoney(22000, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Sell AAPL",
        };

        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(
            [buyOptionTrade, buyOptionTrade2, sellOptionTrade, exercisedOptionTrade, expireOptionTrade, exerciseUnderlyingBuyTrade, sellUnderlyingTrade],
            out UkSection104Pools section104Pools);
        var disposeOptionTradeResult = result.Find(trade => trade is OptionTradeTaxCalculation { AcquisitionDisposal: TradeType.DISPOSAL, TotalQty: 6 });
        disposeOptionTradeResult!.TotalAllowableCost.Amount.ShouldBe(1374.87m, 0.01m); //  ( 1005 * 0.78 + 1504 * 0.85 ) * 4 / 6
        disposeOptionTradeResult.TotalProceeds.ShouldBe(new WrappedMoney(158.4m));  // ( 200 - 2 ) * 0.8
        var disposeTradeResult = result.Find(trade => trade is TradeTaxCalculation { AssetName: "AAPL", AcquisitionDisposal: TradeType.DISPOSAL, TotalQty: 200 });
        disposeTradeResult!.TotalAllowableCost.Amount.ShouldBe(20687.43m, 0.01m);
        disposeTradeResult.TotalProceeds.ShouldBe(new WrappedMoney(22000));
    }
}
