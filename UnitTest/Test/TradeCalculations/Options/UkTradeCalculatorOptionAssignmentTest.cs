using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel.Options;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations.Options;
public class UkTradeCalculatorOptionAssignmentTest
{
    [Theory]
    [InlineData("05-Dec-22 09:30:00")]
    [InlineData("05-Jan-23 09:30:00")]
    [InlineData("25-Jan-23 09:30:00")]
    public void ShortPutOptionWrittenAndAssigned(string optionDate)
    {
        var shortPutOptionTrade = new OptionTrade
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
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Short AAPL Put Option (140 Strike Price)"
        };

        var assignedPutOptionTrade = new OptionTrade
        {
            AssetName = "AAPL230125P00140000",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(140),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.PUT,
            Multiplier = 100,
            TradeReason = TradeReason.OptionAssigned,
            Quantity = 1,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "AAPL Put Option Assigned",
        };

        var buyStockTrade = new Trade
        {
            AssetName = "AAPL",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new DescribedMoney(14000, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Assigned put to buy 100 shares of AAPL",
            TradeReason = TradeReason.OptionAssigned
        };

        var sellStockTrade = new Trade
        {
            AssetName = "AAPL",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new DescribedMoney(18000, "USD", 1),
            Expenses = [new DescribedMoney(5, "USD", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Sell 100 shares of AAPL",
            TradeReason = TradeReason.OptionAssigned
        };

        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(
            new List<Trade>() { buyStockTrade, shortPutOptionTrade, assignedPutOptionTrade, sellStockTrade },
            out _
        );
        var optionDisposalTrade = result.Find(trade => trade is OptionTradeTaxCalculation { AcquisitionDisposal: TradeType.DISPOSAL });
        optionDisposalTrade!.TotalAllowableCost.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        optionDisposalTrade.TotalProceeds.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        optionDisposalTrade.Gain.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        var stockDisposalTrade = result.Find(trade => trade is TradeTaxCalculation { AcquisitionDisposal: TradeType.DISPOSAL, TotalQty: 100 });
        stockDisposalTrade!.TotalAllowableCost.ShouldBe(new WrappedMoney(13505)); // 14000 - (500 - 5)
        stockDisposalTrade.TotalProceeds.ShouldBe(new WrappedMoney(17995)); // 18000 - 5
        stockDisposalTrade.Gain.ShouldBe(new WrappedMoney(4490));
    }
}
