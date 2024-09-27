using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Options;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations.Options;

public class UkTradeCalculatorShortOptionRefundTest
{
    [Theory]
    [InlineData("05-Jan-22 09:30:00", 0, 8500)]
    [InlineData("05-Jan-23 09:30:00", 8500, 0)]
    [InlineData("25-Jan-23 09:30:00", 8500, 0)]
    public void ShortOptionClosingTest(string writeOptionDate, decimal allowableCostSameYear, decimal refundAmount)
    {
        var shortOptionTrade = new OptionTrade
        {
            AssetName = "AAPL 200123C00100000",
            Date = DateTime.Parse(writeOptionDate, CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(100),
            ExpiryDate = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 5,
            GrossProceed = new DescribedMoney(5000, "USD", 0.8m),
            Expenses = [new DescribedMoney(2m, "USD", 0.8m)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Short AAPL 125 Call Option"
        };

        var buyOptionTrade = new OptionTrade
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
            GrossProceed = new DescribedMoney(10000, "USD", 0.85m),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Close AAPL short option",
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades([shortOptionTrade, buyOptionTrade], out UkSection104Pools section104Pools);
        var disposeOptionTradeResult = result.Find(trade => trade is OptionTradeTaxCalculation { AcquisitionDisposal: TradeType.DISPOSAL, TotalQty: 5 });
        disposeOptionTradeResult!.TotalAllowableCost.ShouldBe(new WrappedMoney(allowableCostSameYear));
        ((OptionTradeTaxCalculation)disposeOptionTradeResult).TaxRepayList.Sum(taxrepay => taxrepay.RefundAmount.Amount).ShouldBe(refundAmount);
        ((OptionTradeTaxCalculation)disposeOptionTradeResult).TaxRepayList.Select(taxRepay => taxRepay.TaxYear).ShouldAllBe(x => x == 2022);
    }

    [Fact]
    public void WriteFourOptionsWithVariousClosingScenariosTest()
    {
        var writeOptionTrade = new OptionTrade
        {
            AssetName = "AAPL230125C00140000",
            Date = DateTime.Parse("01-Jan-22 09:30:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(140),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 4,
            GrossProceed = new DescribedMoney(4000, "USD", 0.8m),
            Expenses = [new DescribedMoney(40, "USD", 0.8m)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Write 4 AAPL 140 Call Options"
        };

        var closeOptionWithinSameYear = new OptionTrade
        {
            AssetName = "AAPL230125C00140000",
            Date = DateTime.Parse("20-Feb-22 12:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(140),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,  // Closing 1 option
            GrossProceed = new DescribedMoney(1000, "USD", 0.85m),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Close 1 AAPL 140 Call Option within same tax year"
        };

        var closeOptionNextYear = new OptionTrade
        {
            AssetName = "AAPL230125C00140000",
            Date = DateTime.Parse("10-Jan-23 12:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(140),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,  // Closing 1 option
            GrossProceed = new DescribedMoney(500, "USD", 0.85m),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Close 1 AAPL 140 Call Option next tax year"
        };

        var assignedOption = new OptionTrade
        {
            AssetName = "AAPL230125C00140000",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(140),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OptionAssigned,
            Quantity = 1,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Assigned 1 AAPL 140 Call Option at expiry"
        };

        var assignedTrade = new Trade
        {
            AssetName = "AAPL",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            TradeReason = TradeReason.OptionAssigned,
            Quantity = 100,
            GrossProceed = new DescribedMoney(14000, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Assigned 1 AAPL 140 Call Option at expiry"
        };

        var expiredOption = new OptionTrade
        {
            AssetName = "AAPL230125C00140000",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(140),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.Expired,
            Quantity = 1,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Expired 1 AAPL 140 Call Option"
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(
            new List<Trade>() { writeOptionTrade, closeOptionWithinSameYear, closeOptionNextYear, assignedOption, expiredOption, assignedTrade },
            out UkSection104Pools section104Pools
        );
        var writtenOptionTrade = result.Find(trade => trade is OptionTradeTaxCalculation { AcquisitionDisposal: TradeType.DISPOSAL, TotalQty: 4 });
        writtenOptionTrade!.TotalAllowableCost.ShouldBe(new WrappedMoney(850));
        ((OptionTradeTaxCalculation)writtenOptionTrade).TaxRepayList[0].RefundAmount.ShouldBe(new WrappedMoney(425));
        ((OptionTradeTaxCalculation)writtenOptionTrade).TaxRepayList[0].TaxYear.ShouldBe(2022);
        var execisedTradeResult = result.Find(trade => trade is ITradeTaxCalculation { AcquisitionDisposal: TradeType.DISPOSAL, TotalQty: 100 });
        execisedTradeResult!.UnmatchedCostOrProceed.ShouldBe(new WrappedMoney(14792));
    }
}
