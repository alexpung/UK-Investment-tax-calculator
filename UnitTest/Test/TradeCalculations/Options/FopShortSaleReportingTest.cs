using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Options;
using System.Globalization;
using UnitTest.Helper;
using Shouldly;
using Xunit;
using InvestmentTaxCalculator.Services;

namespace UnitTest.Test.TradeCalculations.Options;

public class FopShortSaleReportingTest
{
    [Fact]
    public void UnmatchedShortOptionShouldShowInReportTest()
    {
        // 1. Sell option short in 2022-23
        var shortOptionTrade = new OptionTrade
        {
            AssetName = "AAPL 230120C00150000",
            Date = DateTime.Parse("10-Dec-22 10:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(1000, "USD", 0.8m), // 800 GBP
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Short AAPL Call"
        };

        var residencyStatusRecord = new ResidencyStatusRecord();
        List<ITradeTaxCalculation> trades = TradeCalculationHelper.CalculateTrades([shortOptionTrade], out _, residencyStatusRecord);
        
        TradeCalculationResult report = new(new UKTaxYear(), residencyStatusRecord);
        report.SetResult(trades);

        // Verify the report for 2022-23
        var year2022 = 2022;
        var proceeds = report.GetDisposalProceeds([year2022], AssetGroupType.OTHERASSETS);
        var gains = report.GetTotalGain([year2022], AssetGroupType.OTHERASSETS);

        // EXPECTATION: Even if unmatched, options should be reported as gains on grant.
        proceeds.Amount.ShouldBe(800m, "Unmatched short option proceeds should be reported in the year of grant");
        gains.Amount.ShouldBe(800m, "Unmatched short option gains should be reported in the year of grant");
    }

    [Fact]
    public void ShortOptionMatchingShouldBeOldestFirstTest()
    {
        // Sell 1 contract on Day 1
        var short1 = new OptionTrade
        {
            AssetName = "AAPL 230120C00150000",
            Date = DateTime.Parse("10-Dec-22 10:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(1000, "USD", 1m),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Day 1 Short"
        };

        // Sell 1 contract on Day 2
        var short2 = new OptionTrade
        {
            AssetName = "AAPL 230120C00150000",
            Date = DateTime.Parse("11-Dec-22 10:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(1200, "USD", 1m),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Day 2 Short"
        };

        // Buy back 1 contract on Day 3
        var buyBack = new OptionTrade
        {
            AssetName = "AAPL 230120C00150000",
            Date = DateTime.Parse("12-Dec-22 10:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(800, "USD", 1m),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Day 3 Buyback"
        };

        List<ITradeTaxCalculation> results = TradeCalculationHelper.CalculateTrades([short1, short2, buyBack], out _);

        // Verify that buyBack (Day 3) is matched with short1 (Day 1) - oldest first
        var short1Calc = results.OfType<OptionTradeTaxCalculation>().First(c => c.TradeList.First().Description == "Day 1 Short");
        var short2Calc = results.OfType<OptionTradeTaxCalculation>().First(c => c.TradeList.First().Description == "Day 2 Short");

        short1Calc.MatchHistory.Count.ShouldBe(1, "First short should be matched");
        short1Calc.MatchHistory[0].MatchedBuyTrade!.TradeList.First().Description.ShouldBe("Day 3 Buyback", "Matched with the buyback");
        short2Calc.MatchHistory.Count.ShouldBe(0, "Second short should NOT be matched manually here");
    }

    [Fact]
    public void ShortOptionCrossYearMatchingTest()
    {
        // 1. Sell option short in 2022-23
        var shortTrade = new OptionTrade
        {
            AssetName = "AAPL 230120C00150000",
            Date = DateTime.Parse("10-Dec-22 10:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = DateTime.Parse("20-Jan-24 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(1000, "USD", 1m),
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Year 1 Short"
        };

        // 2. Buy back in 2023-24
        var buyBack = new OptionTrade
        {
            AssetName = "AAPL 230120C00150000",
            Date = DateTime.Parse("10-May-23 10:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = DateTime.Parse("20-Jan-24 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(600, "USD", 1m),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Year 2 Buyback"
        };

        var residencyStatusRecord = new ResidencyStatusRecord();
        var taxYear = new UKTaxYear();
        List<ITradeTaxCalculation> results = TradeCalculationHelper.CalculateTrades([shortTrade, buyBack], out _, residencyStatusRecord);

        TradeCalculationResult report = new(taxYear, residencyStatusRecord);
        report.SetResult(results);

        // Year 1 (2022-23)
        var proceedsY1 = report.GetDisposalProceeds([2022], AssetGroupType.OTHERASSETS);
        var gainsY1 = report.GetTotalGain([2022], AssetGroupType.OTHERASSETS);
        
        // In Year 1, we only see the grant.
        proceedsY1.Amount.ShouldBe(1000m);
        gainsY1.Amount.ShouldBe(1000m);

        // Year 2 (2023-24)
        var proceedsY2 = report.GetDisposalProceeds([2023], AssetGroupType.OTHERASSETS);
        var gainsY2 = report.GetTotalGain([2023], AssetGroupType.OTHERASSETS);
        var costsY2 = report.GetAllowableCosts([2023], AssetGroupType.OTHERASSETS);

        // In Year 2, the buyback occurs. 
        // According to UkOptionTradeCalculator, a TaxRepay is added to the Year 1 trade, but only for the refund amount.
        // Wait, let's see what happens to the Gain/Loss in Year 2.
        
        // Current logic:
        // Matching across years:
        // allowableCost = WrappedMoney.GetBaseCurrencyZero();
        // TaxRepay added to disposal trade for Year 2.
        
        // This means in Year 2, there is no "disposal" reported in the main table, but there is a "tax refund"?
        // Let's verify what the report object says.
        
        var year2Trades = report.TradeByYear[(2023, AssetCategoryType.OPTION)];
        var disposalCalc = results.OfType<OptionTradeTaxCalculation>().First(c => c.AcquisitionDisposal == TradeType.DISPOSAL);
        
        disposalCalc.TaxRepayList.Count.ShouldBe(1);
        disposalCalc.TaxRepayList[0].TaxYear.ShouldBe(2023);
        disposalCalc.TaxRepayList[0].RefundAmount.Amount.ShouldBe(600m);
    }

    [Fact]
    public void NonResidentShortOptionShouldNotShowInReportTest()
    {
        // 1. Sell option short in 2022-23 when non-resident
        var shortOptionTrade = new OptionTrade
        {
            AssetName = "AAPL 230120C00150000",
            Date = DateTime.Parse("10-Dec-22 10:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(1000, "USD", 0.8m), // 800 GBP
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Short AAPL Call"
        };

        var residencyStatusRecord = new ResidencyStatusRecord();
        residencyStatusRecord.SetResidencyStatus(new DateOnly(2022, 4, 6), new DateOnly(2023, 4, 5), ResidencyStatus.NonResident);

        List<ITradeTaxCalculation> trades = TradeCalculationHelper.CalculateTrades([shortOptionTrade], out _, residencyStatusRecord);
        
        TradeCalculationResult report = new(new UKTaxYear(), residencyStatusRecord);
        report.SetResult(trades);

        var year2022 = 2022;
        var proceeds = report.GetDisposalProceeds([year2022], AssetGroupType.OTHERASSETS);
        var gains = report.GetTotalGain([year2022], AssetGroupType.OTHERASSETS);

        // EXPECTATION: Non-resident disposals should not be in the report proceeds/gains.
        proceeds.Amount.ShouldBe(0m);
        gains.Amount.ShouldBe(0m);
        
        // Check if it's in the disposal list (taxable)
        if (report.DisposalByYear.TryGetValue((year2022, AssetCategoryType.OPTION), out var disposalList))
        {
            disposalList.ShouldBeEmpty("Non-resident disposal should not be in the taxable disposal list");
        }
    }

    [Fact]
    public void TemporaryNonResidentShortOptionShouldShowInYearOfReturnTest()
    {
        // 1. Sell option short in 2022-23 when TNR
        var shortOptionTrade = new OptionTrade
        {
            AssetName = "AAPL 230120C00150000",
            Date = DateTime.Parse("10-Dec-22 10:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = DateTime.Parse("20-Jan-23 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(1000, "USD", 0.8m), // 800 GBP
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Short AAPL Call"
        };

        var residencyStatusRecord = new ResidencyStatusRecord();
        // TNR from 2022 to 2024
        residencyStatusRecord.SetResidencyStatus(new DateOnly(2022, 4, 6), new DateOnly(2024, 4, 5), ResidencyStatus.TemporaryNonResident);
        // Back to Resident from 2024
        residencyStatusRecord.SetResidencyStatus(new DateOnly(2024, 4, 6), DateOnly.MaxValue, ResidencyStatus.Resident);

        List<ITradeTaxCalculation> trades = TradeCalculationHelper.CalculateTrades([shortOptionTrade], out _, residencyStatusRecord);
        
        TradeCalculationResult report = new(new UKTaxYear(), residencyStatusRecord);
        report.SetResult(trades);

        // Should NOT be in 2022-23
        report.GetDisposalProceeds([2022], AssetGroupType.OTHERASSETS).Amount.ShouldBe(0m);

        // Should BE in 2024-25 (year of return)
        report.GetDisposalProceeds([2024], AssetGroupType.OTHERASSETS).Amount.ShouldBe(800m);
        report.GetTotalGain([2024], AssetGroupType.OTHERASSETS).Amount.ShouldBe(800m);
    }

    [Fact]
    public void TemporaryNonResidentShortOptionClosedInSamePeriodShouldNotBeTaxableTest()
    {
        // 1. Sell option short in 2022-23 when TNR
        var shortOptionTrade = new OptionTrade
        {
            AssetName = "AAPL 230120C00150000",
            Date = DateTime.Parse("10-Dec-22 10:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = DateTime.Parse("20-Jan-24 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(1000, "USD", 0.8m), // 800 GBP
            Expenses = [],
            AcquisitionDisposal = TradeType.DISPOSAL,
            Description = "Short AAPL Call"
        };

        // 2. Buy it back in Feb 2023 (same TNR period)
        var buyBackTrade = new OptionTrade
        {
            AssetName = "AAPL 230120C00150000",
            Date = DateTime.Parse("10-Feb-23 10:00:00", CultureInfo.InvariantCulture),
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = DateTime.Parse("20-Jan-24 16:00:00", CultureInfo.InvariantCulture),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100,
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(600, "USD", 0.8m), // 480 GBP
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Buy back AAPL Call"
        };

        var residencyStatusRecord = new ResidencyStatusRecord();
        // TNR from 2022 to 2024
        residencyStatusRecord.SetResidencyStatus(new DateOnly(2022, 4, 6), new DateOnly(2024, 4, 5), ResidencyStatus.TemporaryNonResident);
        // Back to Resident from 2024
        residencyStatusRecord.SetResidencyStatus(new DateOnly(2024, 4, 6), DateOnly.MaxValue, ResidencyStatus.Resident);

        List<ITradeTaxCalculation> trades = TradeCalculationHelper.CalculateTrades([shortOptionTrade, buyBackTrade], out _, residencyStatusRecord);
        
        TradeCalculationResult report = new(new UKTaxYear(), residencyStatusRecord);
        report.SetResult(trades);

        // EXPECTATION: Since it was closed in the same TNR period, it should NOT be taxable in 2024-25
        var proceedsY2024 = report.GetDisposalProceeds([2024], AssetGroupType.OTHERASSETS);
        var gainsY2024 = report.GetTotalGain([2024], AssetGroupType.OTHERASSETS);

        proceedsY2024.Amount.ShouldBe(0m, "TNR option closed in same period should not be taxable on return");
        gainsY2024.Amount.ShouldBe(0m, "TNR option closed in same period should not be taxable on return");
    }
}
