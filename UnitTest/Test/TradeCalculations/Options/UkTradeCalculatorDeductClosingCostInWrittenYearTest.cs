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

public class UkTradeCalculatorDeductClosingCostInWrittenYearTest
{
    [Theory]
    [InlineData(false, 2000, 0, 2000, 600)]
    [InlineData(true, 2000, 600, 1400, 0)]
    public void WrittenOptionPartlyClosedInLaterTaxYear(bool deductClosingCostInWrittenTaxYear, decimal expectedProceeds, decimal expectedAllowableCost,
        decimal expectedGainInWrittenYear, decimal expectedRefund)
    {
        var writeOptionTrade = new OptionTrade
        {
            AssetName = "AAPL 240119C00150000",
            Date = DateTime.Parse("10-Dec-22 10:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = DateTime.Parse("19-Jan-24 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 2,
            GrossProceed = new DescribedMoney(2000, "GBP", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Write 2 AAPL call options"
        };

        var buyBackTrade = new OptionTrade
        {
            AssetName = "AAPL 240119C00150000",
            Date = DateTime.Parse("10-May-23 10:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = DateTime.Parse("19-Jan-24 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(600, "GBP", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Buy back 1 AAPL call option in the next tax year"
        };

        ResidencyStatusRecord residencyStatusRecord = new();
        OptionCalculationSetting setting = new() { DeductClosingCostInWrittenTaxYear = deductClosingCostInWrittenTaxYear };
        List<ITradeTaxCalculation> results = TradeCalculationHelper.CalculateTrades([writeOptionTrade, buyBackTrade], out _, residencyStatusRecord, setting);

        OptionTradeTaxCalculation writtenOption = results.OfType<OptionTradeTaxCalculation>().Single(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL);
        // The option that is not closed is still taxed on the full premium.
        writtenOption.TotalProceeds.ShouldBe(new WrappedMoney(expectedProceeds));
        writtenOption.TotalAllowableCost.ShouldBe(new WrappedMoney(expectedAllowableCost));
        writtenOption.TaxRepayList.Sum(taxRepay => taxRepay.RefundAmount.Amount).ShouldBe(expectedRefund);
        writtenOption.TaxRepayList.ShouldAllBe(taxRepay => taxRepay.WrittenTaxYear == 2022 && taxRepay.ClosingTaxYear == 2023);

        TradeCalculationResult report = new(new UKTaxYear(), residencyStatusRecord);
        report.SetResult(results);
        report.GetTotalGain([2022], AssetGroupType.OTHERASSETS).Amount.ShouldBe(expectedGainInWrittenYear);
        report.GetTotalGain([2023], AssetGroupType.OTHERASSETS).Amount.ShouldBe(0);
    }

    [Fact]
    public void WrittenOptionCashSettledInLaterTaxYear_DeductedInWrittenYear()
    {
        var writeCallOptionTrade = new OptionTrade
        {
            AssetName = "SPX230125C04000000",
            Date = DateTime.Parse("05-Apr-22 09:30:00", CultureInfo.InvariantCulture),
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
            Amount = new DescribedMoney(-30000, "GBP", 1)
        };

        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(
            [writeCallOptionTrade, assignCallOptionTrade, cashSettlement],
            out _,
            optionCalculationSetting: new OptionCalculationSetting { DeductClosingCostInWrittenTaxYear = true }
        );

        OptionTradeTaxCalculation optionDisposalTrade = result.OfType<OptionTradeTaxCalculation>().Single(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL);
        optionDisposalTrade.TotalAllowableCost.ShouldBe(new WrappedMoney(30000));
        optionDisposalTrade.TotalProceeds.ShouldBe(new WrappedMoney(19995));
        optionDisposalTrade.TaxRepayList.ShouldBeEmpty();
    }

    [Fact]
    public void WrittenOptionAssignedByDeliveryInLaterTaxYear_PremiumRemovedFromWrittenYear()
    {
        var buyStockTrade = new Trade
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
            Date = DateTime.Parse("01-Jan-17 09:30:00", CultureInfo.InvariantCulture),
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
            [buyStockTrade, writeCallOptionTrade, assignCallOptionTrade, sellStockTrade],
            out _,
            optionCalculationSetting: new OptionCalculationSetting { DeductClosingCostInWrittenTaxYear = true }
        );

        OptionTradeTaxCalculation optionDisposalTrade = result.OfType<OptionTradeTaxCalculation>().Single(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL);
        // The premium is rolled into the underlying disposal, so the written option no longer has taxable proceeds in the tax year it was written.
        optionDisposalTrade.TotalProceeds.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        optionDisposalTrade.Gain.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        optionDisposalTrade.TaxRepayList.ShouldBeEmpty();

        var stockDisposalTrade = result.Find(trade => trade is TradeTaxCalculation { AcquisitionDisposal: TradeType.DISPOSAL, TotalQty: 100 });
        stockDisposalTrade!.TotalProceeds.ShouldBe(new WrappedMoney(80000 + 502 - 1 - 1 - 4));
    }
}
