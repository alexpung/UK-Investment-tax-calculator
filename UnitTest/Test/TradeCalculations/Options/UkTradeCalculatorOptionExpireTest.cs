using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations.Options;
public class UkTradeCalculatorOptionExpireTest
{
    [Fact]
    public void LongOptionExpireAllowFullCost()
    {
        var buyOptionTrade = new OptionTrade
        {
            AssetName = "AAPL",
            Date = DateTime.Parse("10-Jan-23 09:30:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(100),
            ExpiryDate = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(500, "USD", 0.8m),
            Expenses = [new DescribedMoney(0.1m, "USD", 0.8m)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Buy AAPL 125 Call Option"
        };

        var expireOptionTrade = new OptionTrade
        {
            AssetName = "AAPL",
            Date = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(100),
            ExpiryDate = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.Expired,
            Quantity = 1,
            GrossProceed = new DescribedMoney(0, "USD", 0),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "AAPL 125 Call Option Expired",
            ExeciseOrExecisedTrade = buyOptionTrade
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { buyOptionTrade, expireOptionTrade }, out UkSection104Pools section104Pools);
        result[1].TotalAllowableCost.ShouldBe(new WrappedMoney(400.08m));
        result[1].TotalProceeds.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        result[1].Gain.ShouldBe(new WrappedMoney(-400.08m));
    }

    [Fact]
    public void ShortOptionExpireNoCostAllowed()
    {
        var shortOptionTrade = new OptionTrade
        {
            AssetName = "AAPL",
            Date = DateTime.Parse("10-Jan-23 09:30:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(100),
            ExpiryDate = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(500, "USD", 0.8m),
            Expenses = [new DescribedMoney(0.1m, "USD", 0.8m)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Short AAPL 125 Call Option"
        };

        var expireOptionTrade = new OptionTrade
        {
            AssetName = "AAPL",
            Date = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(100),
            ExpiryDate = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.Expired,
            Quantity = 1,
            GrossProceed = new DescribedMoney(0, "USD", 0),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "AAPL 125 Call Option Expired",
            ExeciseOrExecisedTrade = shortOptionTrade
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { shortOptionTrade, expireOptionTrade }, out UkSection104Pools section104Pools);
        result[0].TotalAllowableCost.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        result[0].TotalProceeds.ShouldBe(new WrappedMoney(399.92m));
        result[0].Gain.ShouldBe(new WrappedMoney(399.92m));
    }
}

