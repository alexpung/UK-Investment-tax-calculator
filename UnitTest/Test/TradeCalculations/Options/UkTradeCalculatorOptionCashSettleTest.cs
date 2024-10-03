using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel.Options;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations.Options;
public class UkTradeCalculatorOptionCashSettleTest
{
    [Theory]
    [InlineData("05-Dec-22 09:30:00")]
    [InlineData("05-Jan-23 09:30:00")]
    [InlineData("25-Jan-23 09:30:00")]
    public void LongPutOptionBoughtAndCashSettleExercised(string optionDate)
    {
        var buyPutOptionTrade = new OptionTrade
        {
            AssetName = "SPX230125P04000000",
            Date = DateTime.Parse(optionDate, CultureInfo.InvariantCulture),
            Underlying = "SPX",
            StrikePrice = new WrappedMoney(4000),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.PUT,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(20000, "USD", 1),
            Expenses = [new DescribedMoney(5, "USD", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Buy SPX Put Option (4000 Strike Price)"
        };

        var exercisePutOptionTrade = new OptionTrade
        {
            AssetName = "SPX230125P04000000",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "SPX",
            StrikePrice = new WrappedMoney(4000),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.PUT,
            Multiplier = 100,
            TradeReason = TradeReason.OwnerExerciseOption,
            Quantity = 1,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "SPX Put Option Exercised",
        };

        var cashSettlement = new CashSettlement
        {
            AssetName = "SPX230125P04000000",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Description = "Option exercise cash settlement",
            TradeReason = TradeReason.OwnerExerciseOption,
            Amount = new WrappedMoney(30000)
        };

        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(
            [buyPutOptionTrade, exercisePutOptionTrade, cashSettlement],
            out _
        );

        var optionDisposalTrade = result.Find(trade => trade is OptionTradeTaxCalculation { AcquisitionDisposal: TradeType.DISPOSAL });
        optionDisposalTrade!.TotalAllowableCost.ShouldBe(new WrappedMoney(20005));
        optionDisposalTrade.TotalProceeds.ShouldBe(new WrappedMoney(30000));
    }

    [Theory]
    [InlineData("05-Apr-22 09:30:00", 0, true)]
    [InlineData("05-Dec-22 09:30:00", 30000, false)]
    [InlineData("05-Jan-23 09:30:00", 30000, false)]
    [InlineData("25-Jan-23 09:30:00", 30000, false)]
    public void ShortCallOptionAndCashSettleAssigned(string optionDate, decimal allowableCost, bool refunded)
    {
        var writeCallOptionTrade = new OptionTrade
        {
            AssetName = "SPX230125C04000000",
            Date = DateTime.Parse(optionDate, CultureInfo.InvariantCulture),
            Underlying = "SPX",
            StrikePrice = new WrappedMoney(4000),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(20000, "USD", 1),
            Expenses = [new DescribedMoney(5, "USD", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Write SPX call Option (4000 Strike Price)"
        };

        var assignCallOptionTrade = new OptionTrade
        {
            AssetName = "SPX230125C04000000",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "SPX",
            StrikePrice = new WrappedMoney(4000),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OptionAssigned,
            Quantity = 1,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "SPX Call Option assigned",
        };

        var cashSettlement = new CashSettlement
        {
            AssetName = "SPX230125C04000000",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Description = "Option exercise cash settlement",
            TradeReason = TradeReason.OptionAssigned,
            Amount = new WrappedMoney(-30000)
        };

        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(
            [writeCallOptionTrade, assignCallOptionTrade, cashSettlement],
            out _
        );

        OptionTradeTaxCalculation optionDisposalTrade = (OptionTradeTaxCalculation)result.Find(trade => trade is OptionTradeTaxCalculation { AcquisitionDisposal: TradeType.DISPOSAL })!;
        optionDisposalTrade!.TotalAllowableCost.ShouldBe(new WrappedMoney(allowableCost));
        optionDisposalTrade.TotalProceeds.ShouldBe(new WrappedMoney(19995));
        if (refunded)
        {
            optionDisposalTrade.TaxRepayList.Count.ShouldBe(1);
            optionDisposalTrade.TaxRepayList[0].RefundAmount.ShouldBe(new WrappedMoney(30000));
            optionDisposalTrade.TaxRepayList[0].TaxYear.ShouldBe(2022);
        }
        else
        {
            optionDisposalTrade.TaxRepayList.Count.ShouldBe(0);
        }
    }

    [Fact]
    public void ShortOptionMultipleTradeTest()
    {
        var writeCallOptionTrade = new OptionTrade
        {
            AssetName = "SPX230125C04000000",
            Date = DateTime.Parse("03-Apr-22 09:30:00", CultureInfo.InvariantCulture),
            Underlying = "SPX",
            StrikePrice = new WrappedMoney(4000),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 10,
            GrossProceed = new DescribedMoney(200000, "USD", 1),
            Expenses = [new DescribedMoney(30, "USD", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Write SPX call Option (4000 Strike Price)"
        };

        var closeCallOptionTrade1 = new OptionTrade
        {
            AssetName = "SPX230125C04000000",
            Date = DateTime.Parse("05-Apr-22 09:30:00", CultureInfo.InvariantCulture),
            Underlying = "SPX",
            StrikePrice = new WrappedMoney(4000),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 4,
            GrossProceed = new DescribedMoney(70000, "USD", 1),
            Expenses = [new DescribedMoney(50, "USD", 0.5m)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "SPX Call Option partly closed",
        };

        var closeCallOptionTrade2 = new OptionTrade
        {
            AssetName = "SPX230125C04000000",
            Date = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "SPX",
            StrikePrice = new WrappedMoney(4000),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(10000, "USD", 1),
            Expenses = [new DescribedMoney(50, "USD", 0.5m)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "SPX Call Option partly closed",
        };

        var closeCallOptionTrade3 = new OptionTrade
        {
            AssetName = "SPX230125C04000000",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "SPX",
            StrikePrice = new WrappedMoney(4000),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 2,
            GrossProceed = new DescribedMoney(30000, "USD", 1),
            Expenses = [new DescribedMoney(20, "USD", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "SPX Call Option closed",
        };

        var assignCallOptionTrade = new OptionTrade
        {
            AssetName = "SPX230125C04000000",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "SPX",
            StrikePrice = new WrappedMoney(4000),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OptionAssigned,
            Quantity = 3,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "SPX Put Option assigned",
        };

        var cashSettlement = new CashSettlement
        {
            AssetName = "SPX230125C04000000",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Description = "Option exercise cash settlement",
            TradeReason = TradeReason.OptionAssigned,
            Amount = new WrappedMoney(-90000)
        };

        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(
            [writeCallOptionTrade, closeCallOptionTrade1, closeCallOptionTrade2, closeCallOptionTrade3, assignCallOptionTrade, cashSettlement],
            out _
        );
        OptionTradeTaxCalculation optionDisposalTrade = (OptionTradeTaxCalculation)result.Find(trade => trade is OptionTradeTaxCalculation { AcquisitionDisposal: TradeType.DISPOSAL })!;
        optionDisposalTrade!.TotalAllowableCost.ShouldBe(new WrappedMoney(70000 + (50 / 2)));
        optionDisposalTrade.TotalProceeds.ShouldBe(new WrappedMoney(200000 - 30));
        optionDisposalTrade.TaxRepayList.Count.ShouldBe(3);
        optionDisposalTrade.TaxRepayList[0].RefundAmount.ShouldBe(new WrappedMoney(10000 + 25));
        optionDisposalTrade.TaxRepayList[0].TaxYear.ShouldBe(2022);
        optionDisposalTrade.TaxRepayList[1].RefundAmount.ShouldBe(new WrappedMoney(90000));
        optionDisposalTrade.TaxRepayList[1].TaxYear.ShouldBe(2022);
        optionDisposalTrade.TaxRepayList[2].RefundAmount.ShouldBe(new WrappedMoney(30000 + 20));
        optionDisposalTrade.TaxRepayList[2].TaxYear.ShouldBe(2022);
    }

    [Fact]
    public void LongOptionMultipleTradeTest()
    {
        var BuyCallOptionTrade = new OptionTrade
        {
            AssetName = "SPX230125C04000000",
            Date = DateTime.Parse("03-Apr-22 09:30:00", CultureInfo.InvariantCulture),
            Underlying = "SPX",
            StrikePrice = new WrappedMoney(4000),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 10,
            GrossProceed = new DescribedMoney(200000, "USD", 1),
            Expenses = [new DescribedMoney(200, "USD", 1)],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Write SPX call Option (4000 Strike Price)"
        };

        var closeCallOptionTrade1 = new OptionTrade
        {
            AssetName = "SPX230125C04000000",
            Date = DateTime.Parse("05-Apr-22 09:30:00", CultureInfo.InvariantCulture),
            Underlying = "SPX",
            StrikePrice = new WrappedMoney(4000),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 4,
            GrossProceed = new DescribedMoney(70000, "USD", 1),
            Expenses = [new DescribedMoney(50, "USD", 0.5m)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "SPX Call Option partly closed",
        };

        var closeCallOptionTrade2 = new OptionTrade
        {
            AssetName = "SPX230125C04000000",
            Date = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "SPX",
            StrikePrice = new WrappedMoney(4000),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(10000, "USD", 1),
            Expenses = [new DescribedMoney(50, "USD", 0.5m)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "SPX Call Option partly closed",
        };

        var closeCallOptionTrade3 = new OptionTrade
        {
            AssetName = "SPX230125C04000000",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "SPX",
            StrikePrice = new WrappedMoney(4000),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 2,
            GrossProceed = new DescribedMoney(30000, "USD", 1),
            Expenses = [new DescribedMoney(20, "USD", 1)],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "SPX Call Option closed",
        };

        var execiseCallOptionTrade = new OptionTrade
        {
            AssetName = "SPX230125C04000000",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Underlying = "SPX",
            StrikePrice = new WrappedMoney(4000),
            ExpiryDate = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OwnerExerciseOption,
            Quantity = 3,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "SPX Put Option execised",
        };

        var cashSettlement = new CashSettlement
        {
            AssetName = "SPX230125C04000000",
            Date = DateTime.Parse("25-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            Description = "Option exercise cash settlement",
            TradeReason = TradeReason.OwnerExerciseOption,
            Amount = new WrappedMoney(90000)
        };

        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(
            [BuyCallOptionTrade, closeCallOptionTrade1, closeCallOptionTrade2, closeCallOptionTrade3, execiseCallOptionTrade, cashSettlement],
            out _
        );
        OptionTradeTaxCalculation optionDisposalTrade = (OptionTradeTaxCalculation)result.Find(trade => trade is OptionTradeTaxCalculation
        { AcquisitionDisposal: TradeType.DISPOSAL, TotalQty: 4 })!;
        optionDisposalTrade!.TotalAllowableCost.ShouldBe(new WrappedMoney(80080));
        optionDisposalTrade.TotalProceeds.ShouldBe(new WrappedMoney(69975));
        OptionTradeTaxCalculation optionDisposalTrade2 = (OptionTradeTaxCalculation)result.Find(trade => trade is OptionTradeTaxCalculation
        { AcquisitionDisposal: TradeType.DISPOSAL, TotalQty: 1 })!;
        optionDisposalTrade2!.TotalAllowableCost.ShouldBe(new WrappedMoney(20020));
        optionDisposalTrade2.TotalProceeds.ShouldBe(new WrappedMoney(9975));
        OptionTradeTaxCalculation optionDisposalTrade3 = (OptionTradeTaxCalculation)result.Find(trade => trade is OptionTradeTaxCalculation
        { AcquisitionDisposal: TradeType.DISPOSAL, TotalQty: 5 })!;
        optionDisposalTrade3!.TotalAllowableCost.ShouldBe(new WrappedMoney(40040 + 60060));
        optionDisposalTrade3.TotalProceeds.ShouldBe(new WrappedMoney(29980 + 90000));
    }
}
