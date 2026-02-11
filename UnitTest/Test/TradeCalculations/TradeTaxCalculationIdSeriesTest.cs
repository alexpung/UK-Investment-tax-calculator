using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel.Options;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;
using InvestmentTaxCalculator.Model.UkTaxModel.Futures;
using Xunit;
using Shouldly;

namespace UnitTest.Test.TradeCalculations;

[Collection("NonParallelTests")]
public class TradeTaxCalculationIdSeriesTest
{
    [Fact]
    public void TestCalculationClassesShareSameIdSeries()
    {
        // Reset the shared ID counter
        ITradeTaxCalculation.ResetID();

        // 1. Create a regular TradeTaxCalculation
        var stockTrade = new Trade
        {
            AssetName = "AAPL",
            Date = new DateTime(2023, 1, 1),
            AcquisitionDisposal = TradeType.ACQUISITION,
            Quantity = 100m,
            GrossProceed = new DescribedMoney(1000m, "USD", 1m)
        };
        var stockCalc = new TradeTaxCalculation([stockTrade]);

        // 2. Create a CorporateActionTaxCalculation
        var split = new StockSplit
        {
            AssetName = "AAPL",
            Date = new DateTime(2023, 1, 2),
            SplitFrom = 1,
            SplitTo = 2
        };
        var caCalc = new CorporateActionTaxCalculation(split, WrappedMoney.GetBaseCurrencyZero(), WrappedMoney.GetBaseCurrencyZero(), InvestmentTaxCalculator.Enumerations.ResidencyStatus.Resident, 1.0m);

        // 3. Create an OptionTradeTaxCalculation
        var optionTrade = new OptionTrade
        {
            AssetName = "AAPL 230120C150",
            Date = new DateTime(2023, 1, 3),
            AcquisitionDisposal = TradeType.DISPOSAL,
            Quantity = 1m,
            GrossProceed = new DescribedMoney(500m, "USD", 1m),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150m),
            ExpiryDate = new DateTime(2023, 1, 20),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100m
        };
        var optionCalc = new OptionTradeTaxCalculation([optionTrade]);

        // 4. Create a FutureTradeTaxCalculation
        var futureTrade = new FutureContractTrade
        {
            AssetName = "MESZ3",
            Date = new DateTime(2023, 1, 4),
            AcquisitionDisposal = TradeType.ACQUISITION,
            PositionType = PositionType.OPENLONG,
            Quantity = 1m,
            GrossProceed = new DescribedMoney(50m, "USD", 1m), // Commission/Fee
            ContractValue = new DescribedMoney(20000m, "USD", 1m)
        };
        var futureCalc = new FutureTradeTaxCalculation([futureTrade]);

        // Verify IDs are sequential across all types
        stockCalc.Id.ShouldBe(1);
        caCalc.Id.ShouldBe(2);
        optionCalc.Id.ShouldBe(3);
        futureCalc.Id.ShouldBe(4);
    }

    [Fact]
    public void TestResetIdClearsAllSeries()
    {
        // Increment IDs a bit
        var dummyTrade = new Trade
        {
            AssetName = "DUMMY",
            Date = DateTime.Now,
            AcquisitionDisposal = TradeType.ACQUISITION,
            Quantity = 1m,
            GrossProceed = new DescribedMoney(1m, "USD", 1m)
        };
        _ = new TradeTaxCalculation([dummyTrade]);

        // Reset
        ITradeTaxCalculation.ResetID();

        // New calculation should start at 1
        var nextCalc = new TradeTaxCalculation([dummyTrade]);
        nextCalc.Id.ShouldBe(1);
    }
}
