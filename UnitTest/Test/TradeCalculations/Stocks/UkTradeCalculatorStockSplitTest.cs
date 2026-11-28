using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations.Stocks;

public class UkTradeCalculatorStockSplitTest
{
    [Fact]
    public void TestStockSplitOnSameDay_DoesNotAdjustSameDayMatching()
    {
        Trade sameDayBuy = new()
        {
            AssetName = "SAME_DAY_SPLIT",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Feb-22 10:00:00", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new() { Amount = new(1000m) },
        };

        StockSplit stockSplit = new()
        {
            AssetName = "SAME_DAY_SPLIT",
            Date = DateTime.Parse("01-Feb-22 12:00:00", CultureInfo.InvariantCulture),
            SplitFrom = 1,
            SplitTo = 2
        };

        Trade sameDaySell = new()
        {
            AssetName = "SAME_DAY_SPLIT",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("01-Feb-22 16:00:00", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new() { Amount = new(1200m) },
        };

        UkSection104Pools section104Pools = new(new UKTaxYear(), new ResidencyStatusRecord());
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([sameDayBuy, stockSplit, sameDaySell]);

        UkTradeCalculator calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        ITradeTaxCalculation disposal = result.First(x => x.AcquisitionDisposal == TradeType.DISPOSAL && x is not CorporateActionTaxCalculation);
        disposal.MatchHistory.Count.ShouldBe(1);
        disposal.MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SAME_DAY);
        disposal.TotalAllowableCost.Amount.ShouldBe(1000m, 0.01m);
        disposal.Gain.Amount.ShouldBe(200m, 0.01m);

        UkSection104 pool = section104Pools.GetExistingOrInitialise("SAME_DAY_SPLIT");
        pool.Quantity.ShouldBe(0m);
        pool.AcquisitionCostInBaseCurrency.Amount.ShouldBe(0m, 0.01m);
    }

    [Fact]
    public void TestStockSplitIntraday_AppliesBeforeLaterDisposalInSection104Flow()
    {
        Trade initialPurchase = new()
        {
            AssetName = "INTRADAY_SPLIT",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jan-22 10:00:00", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new() { Amount = new(1000m) },
        };

        StockSplit stockSplit = new()
        {
            AssetName = "INTRADAY_SPLIT",
            Date = DateTime.Parse("01-Feb-22 09:00:00", CultureInfo.InvariantCulture),
            SplitFrom = 1,
            SplitTo = 2
        };

        Trade postSplitDisposal = new()
        {
            AssetName = "INTRADAY_SPLIT",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("01-Feb-22 16:00:00", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new() { Amount = new(1200m) },
        };

        UkSection104Pools section104Pools = new(new UKTaxYear(), new ResidencyStatusRecord());
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([initialPurchase, stockSplit, postSplitDisposal]);

        UkTradeCalculator calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        ITradeTaxCalculation disposal = result.First(x => x.AcquisitionDisposal == TradeType.DISPOSAL && x is not CorporateActionTaxCalculation);
        disposal.TotalAllowableCost.Amount.ShouldBe(500m, 0.01m);

        UkSection104 pool = section104Pools.GetExistingOrInitialise("INTRADAY_SPLIT");
        pool.Quantity.ShouldBe(100m);
        pool.AcquisitionCostInBaseCurrency.Amount.ShouldBe(500m, 0.01m);
    }

    [Fact]
    public void TestSection104WithStockSplit()
    {
        // First trade: Initial purchase before the split
        Trade initialPurchase = new()
        {
            AssetName = "XYZ",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jan-22 10:00:00", CultureInfo.InvariantCulture),
            Description = "Purchase of 1000 shares",
            Quantity = 1000,
            GrossProceed = new() { Description = "", Amount = new(1000.0m, "USD"), FxRate = 1.0m },
        };

        // Stock split event
        StockSplit stockSplit = new()
        {
            AssetName = "XYZ",
            Date = DateTime.Parse("01-Feb-22 10:00:00", CultureInfo.InvariantCulture),
            SplitFrom = 1,
            SplitTo = 2
        };

        // Second trade: Selling shares after the split
        Trade postSplitSale = new()
        {
            AssetName = "XYZ",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("01-Mar-22 10:00:00", CultureInfo.InvariantCulture),
            Description = "Sale of 1000 shares",
            Quantity = 1000,
            GrossProceed = new() { Description = "", Amount = new(1500.0m, "USD"), FxRate = 1.0m },
        };

        UkSection104Pools section104Pools = new(new UKTaxYear(), new ResidencyStatusRecord());
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([initialPurchase, stockSplit, postSplitSale]);

        UkTradeCalculator calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        // Assert the expected results
        // Section 104 Holding Matching (1000 shares)
        result[1].MatchHistory[0].BaseCurrencyMatchDisposalProceed.Amount.ShouldBe(1500.0m, 0.01m); // (1500 * 1000 / 2000)
        result[1].MatchHistory[0].BaseCurrencyMatchAllowableCost.Amount.ShouldBe(500.0m, 0.01m); // (1000 * 1000 / 2000)
        result[1].MatchHistory[0].MatchAcquisitionQty.ShouldBe(1000);
        result[1].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SECTION_104);

        // Gain Calculation
        result[1].Gain.Amount.ShouldBe(1000.0m, 0.01m); // 1500 - (1000 * 1000/2000)
        result[1].TotalAllowableCost.Amount.ShouldBe(500.0m, 0.01m);

        // Ensure the Section 104 pool is updated correctly
        section104Pools.GetExistingOrInitialise("XYZ").AcquisitionCostInBaseCurrency.Amount.ShouldBe(500.0m, 0.01m); // 1000 - 500
        section104Pools.GetExistingOrInitialise("XYZ").Quantity.ShouldBe(1000); // 2000 - 1000
    }

    [Fact]
    public void TestSection104WithStockSplit2()
    {
        // Trade1: Buying 500 shares pre-split
        Trade trade1 = new()
        {
            AssetName = "XYZ",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jan-22", CultureInfo.InvariantCulture),
            Quantity = 500,
            GrossProceed = new() { Amount = new(5000m) },  // £10 per share
        };

        // Stock Split: 1:2
        StockSplit stockSplit = new()
        {
            AssetName = "XYZ",
            Date = DateTime.Parse("01-Feb-22", CultureInfo.InvariantCulture),
            SplitFrom = 1,
            SplitTo = 2
        };

        // Trade2: Buying 600 shares post-split
        Trade trade2 = new()
        {
            AssetName = "XYZ",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("02-Feb-22", CultureInfo.InvariantCulture),
            Quantity = 600,
            GrossProceed = new() { Amount = new(3600m) },  // £6 per share
        };

        // Trade3: Selling 700 shares post-split
        Trade trade3 = new()
        {
            AssetName = "XYZ",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("03-Feb-22", CultureInfo.InvariantCulture),
            Quantity = 700,
            GrossProceed = new() { Amount = new(4900m) },  // £7 per share
        };

        // Section 104 pool
        UkSection104Pools section104Pools = new(new UKTaxYear(), new ResidencyStatusRecord());
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([trade1, stockSplit, trade2, trade3]);

        UkTradeCalculator calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        // Assert the expected results
        result[2].MatchHistory[0].BaseCurrencyMatchDisposalProceed.Amount.ShouldBe(4900m, 0.01m); // Selling 700 shares at £7
        result[2].MatchHistory[0].BaseCurrencyMatchAllowableCost.Amount.ShouldBe(3762.50m, 0.01m); // Acquisition cost (700 shares at £5.375)

        result[2].MatchHistory[0].MatchAcquisitionQty.ShouldBe(700);

        section104Pools.GetExistingOrInitialise("XYZ").AcquisitionCostInBaseCurrency.Amount.ShouldBe(4837.50m, 0.01m); // Remaining value in pool
        section104Pools.GetExistingOrInitialise("XYZ").Quantity.ShouldBe(900); // Remaining quantity in pool
    }

    [Fact]
    public void TestBedAndBreakfastWithStockSplit()
    {
        // Initial purchase of 100 shares
        Trade trade1 = new()
        {
            AssetName = "ABC",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Apr-21", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new() { Amount = new(2000m) } // £20 per share
        };

        // Stock split occurs, every share is now 2 shares
        StockSplit stockSplit = new()
        {
            AssetName = "ABC",
            Date = DateTime.Parse("01-May-21", CultureInfo.InvariantCulture),
            SplitTo = 2,
            SplitFrom = 1
        };

        // Sale of 100 shares (post-split, effectively 50 pre-split shares)
        Trade trade2 = new()
        {
            AssetName = "ABC",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("02-May-21", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new() { Amount = new(2500m) } // £25 per share post-split
        };

        // Repurchase of 50 shares (post-split, effectively 25 pre-split shares)
        Trade trade3 = new()
        {
            AssetName = "ABC",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("03-May-21", CultureInfo.InvariantCulture),
            Quantity = 50,
            GrossProceed = new() { Amount = new(1300m) } // £26 per share post-split
        };

        UkSection104Pools section104Pools = new(new UKTaxYear(), new ResidencyStatusRecord());
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([trade1, stockSplit, trade2, trade3]);
        UkTradeCalculator calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        result[1].Gain.Amount.ShouldBe(700m, 0.01m); // Gain is the disposal value minus acquisition cost: 2500 - 1300 + 2000 * 50 * (2000 / 200) = 700
        result[1].TotalAllowableCost.Amount.ShouldBe(1800m, 0.01m); // Acquisition cost of the sold shares: 1300 + 2000 * 50 * (2000 / 200)
        result[1].TotalProceeds.Amount.ShouldBe(2500m, 0.01m); // Total proceeds from the sale: 100 shares sold at £25 each

        // Verify the Section 104 pool after the sale
        section104Pools.GetExistingOrInitialise("ABC").AcquisitionCostInBaseCurrency.Amount.ShouldBe(1500m, 0.01m); // Remaining value in pool (2000 - 500)
        section104Pools.GetExistingOrInitialise("ABC").Quantity.ShouldBe(150); // Remaining quantity in pool (200 - 100)
    }

    [Fact]
    public void TestReverseStockSplitWithRoundingAndCash()
    {
        // Initial purchase of 95 shares @ £10
        Trade trade1 = new()
        {
            AssetName = "RS_TEST",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jan-22", CultureInfo.InvariantCulture),
            Quantity = 95,
            GrossProceed = new() { Amount = new(950m) }
        };

        // 1-for-10 reverse split. 
        // 95 shares should become 9.5 shares.
        // With rounding down, it becomes 9 shares.
        // Cash-in-lieu for 0.5 shares.
        StockSplit reverseSplit = new()
        {
            AssetName = "RS_TEST",
            Date = DateTime.Parse("01-Feb-22", CultureInfo.InvariantCulture),
            SplitTo = 1,
            SplitFrom = 10,
            CashInLieu = new DescribedMoney { Amount = new(6m) }, // £6 for the 0.5 shares
            ElectTaxDeferral = false // Normal part-disposal
        };

        UkSection104Pools section104Pools = new(new UKTaxYear(), new ResidencyStatusRecord());
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([trade1, reverseSplit]);

        UkTradeCalculator calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        // result[0] is the Acquisition (initialPurchase)
        // result[1] is the CashDisposal (CorporateActionTaxCalculation)
        
        var cashCalc = (CorporateActionTaxCalculation)result.First(r => r is CorporateActionTaxCalculation);
        var splitResult = (StockSplit)cashCalc.RelatedCorporateAction;
        
        splitResult.CashDisposal.ShouldNotBeNull();
        splitResult.CashDisposal.TotalProceeds.Amount.ShouldBe(6m);
        splitResult.CashDisposal.TotalAllowableCost.Amount.ShouldBe(50m, 0.01m);
        splitResult.CashDisposal.Gain.Amount.ShouldBe(-44m, 0.01m);

        // Final S104 state
        var pool = section104Pools.GetExistingOrInitialise("RS_TEST");
        pool.Quantity.ShouldBe(9);
        pool.AcquisitionCostInBaseCurrency.Amount.ShouldBe(900m, 0.01m); // 950 - 50
    }

    [Fact]
    public void TestSmallCashStockSplitDeferral()
    {
        // Initial purchase of 95 shares @ £10
        Trade trade1 = new()
        {
            AssetName = "DEFER_TEST",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jan-22", CultureInfo.InvariantCulture),
            Quantity = 95,
            GrossProceed = new() { Amount = new(950m) }
        };

        // 1-for-10 reverse split. 
        // Receive small cash £6.
        StockSplit reverseSplit = new()
        {
            AssetName = "DEFER_TEST",
            Date = DateTime.Parse("01-Feb-22", CultureInfo.InvariantCulture),
            SplitTo = 1,
            SplitFrom = 10,
            CashInLieu = new DescribedMoney { Amount = new(6m) },
            ElectTaxDeferral = true // Elect s122 deferral
        };

        UkSection104Pools section104Pools = new(new UKTaxYear(), new ResidencyStatusRecord());
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([trade1, reverseSplit]);

        UkTradeCalculator calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();
        
        result.Any(r => r is CorporateActionTaxCalculation).ShouldBeFalse();

        // Final S104 state
        var pool = section104Pools.GetExistingOrInitialise("DEFER_TEST");
        pool.Quantity.ShouldBe(9);

        // 950 - 6 = 944.
        pool.AcquisitionCostInBaseCurrency.Amount.ShouldBe(944m, 0.01m);
    }
    [Fact]
    public void TestStockSplitFractionalCost_QuantityProportion()
    {
        // Regression test for "Quantity Proportion vs Value Proportion" logic.
        // Verifies that cost allocated to a fractional share is based on the fraction of shares removed,
        // (Quantity Proportion), NOT the relative value of the cash vs the remaining shares (A/A+B).
        
        // Scenario:
        // Buy 100 shares @ £10 = £1000.
        // Reverse Split 200-for-101 (effectively 100 -> 50.5).
        // New Qty Raw = 50.5.
        // Floor = 50.
        // Fraction Removed = 0.5.
        // Fraction of Total = 0.5 / 50.5 = 0.00990099... (1/101).
        
        // Expected Cost Allocation = £1000 * (1/101) = £9.90099...
        
        // "Bad Rate" Scenario:
        // Assume the 0.5 share would be worth £10 per share = £5.
        // But we only get £1 cash-in-lieu (very low).
        // If we used Value Proportion (A/A+B):
        // Value of remaining 50 shares @ £20 = £1000.
        // Total Value = £1000 + £1 = £1001.
        // Cash Ratio = 1 / 1001 = 0.000999... (~0.1%).
        // Cost Allocation would be £1000 * 0.000999 = £0.99. -> INCORRECT.
        
        Trade trade1 = new()
        {
            AssetName = "QUANTITY_TEST",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jan-22", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new() { Amount = new(1000m) }
        };

        StockSplit split = new()
        {
            AssetName = "QUANTITY_TEST",
            Date = DateTime.Parse("01-Feb-22", CultureInfo.InvariantCulture),
            SplitTo = 101,
            SplitFrom = 200,
            CashInLieu = new DescribedMoney { Amount = new(1m) }, // Very low cash
            ElectTaxDeferral = false 
        };

        UkSection104Pools section104Pools = new(new UKTaxYear(), new ResidencyStatusRecord());
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([trade1, split]);

        UkTradeCalculator calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();
        
        var cashCalc = result.OfType<CorporateActionTaxCalculation>().FirstOrDefault();
        cashCalc.ShouldNotBeNull();
        
        var stockSplitResult = (StockSplit)cashCalc!.RelatedCorporateAction;
        stockSplitResult.CashDisposal.ShouldNotBeNull();
        
        // Assertions
        decimal expectedCost = 1000m * (0.5m / 50.5m); // ~9.90
        
        // Should match the Quantity Proportion cost (~9.90), NOT the Value Proportion cost (~0.99)
        stockSplitResult.CashDisposal.TotalAllowableCost.Amount.ShouldBe(expectedCost, 0.01m);
        
        // Gain = Proceeds (1.00) - Cost (9.90) = -8.90
        stockSplitResult.CashDisposal.Gain.Amount.ShouldBe(1m - expectedCost, 0.01m);
    }
    [Fact]
    public void TestStockSplitSmallCashLoss_DeferredIfElected()
    {
        // Verifies that a loss is deferred (no disposal created) if Deferral is elected for "small" cash.
        
        // Buy 101 shares @ £10 = £1010.
        // Reverse Split 1-for-10. 
        // 101 shares becomes 10.1 shares.
        // Fraction Removed = 0.1.
        // Proportional Cost = 1010 * (0.1/10.1) = £10.
        // CashReceived = £5 (Small cash).
        // Loss = £5.
        
        Trade trade1 = new()
        {
            AssetName = "SMALL_LOSS_DEFER",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jan-22", CultureInfo.InvariantCulture),
            Quantity = 101,
            GrossProceed = new() { Amount = new(1010m) }
        };

        StockSplit split = new()
        {
            AssetName = "SMALL_LOSS_DEFER",
            Date = DateTime.Parse("01-Feb-22", CultureInfo.InvariantCulture),
            SplitTo = 1,
            SplitFrom = 10,
            CashInLieu = new DescribedMoney { Amount = new(5m) }, 
            ElectTaxDeferral = true // Electing deferral SHOULD suppress a loss if it's small
        };

        UkSection104Pools section104Pools = new(new UKTaxYear(), new ResidencyStatusRecord());
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([trade1, split]);

        UkTradeCalculator calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();
        
        // Expected: No disposal created for the corporate action
        var cashCalc = result.OfType<CorporateActionTaxCalculation>().FirstOrDefault();
        cashCalc.ShouldBeNull("Small loss should be deferred when elected, resulting in no disposal record.");
        
        // Final S104 state: 10 shares, cost = 1010 - 5 = 1005
        var pool = section104Pools.GetExistingOrInitialise("SMALL_LOSS_DEFER");
        pool.Quantity.ShouldBe(10m);
        pool.AcquisitionCostInBaseCurrency.Amount.ShouldBe(1005m, 0.01m);
    }

    [Fact]
    public void TestStockSplitSmallCash_ExcessGainDeferral()
    {
        // Verifies TCGA 1992 s122 "Excess Gain" logic:
        // If cash is "small" and deferred, but EXCEEDS the total pool cost,
        // the excess must be recognized as a capital gain immediately.
        
        // Buy 10 shares @ £10 = £100.
        // Reverse Split 1-for-20.
        // 10 shares -> 0.5 shares.
        // Cash in lieu for 0.5 = £150.
        // Pool Cost = £100.
        // Total Value (Extrapolated) = £150.
        // IsSmall(150, 150) = True (<= £3000).
        
        // Expected:
        // CashCostUsed (to reduce pool) = £100 (max available).
        // Excess Gain = 150 - 100 = £50.
        // A disposal of £50 with £0 cost should be created.
        // Final Pool: 0 shares, £0 cost.

        Trade trade1 = new()
        {
            AssetName = "EXCESS_GAIN_TEST",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jan-22", CultureInfo.InvariantCulture),
            Quantity = 10,
            GrossProceed = new() { Amount = new(100m) }
        };

        StockSplit split = new()
        {
            AssetName = "EXCESS_GAIN_TEST",
            Date = DateTime.Parse("01-Feb-22", CultureInfo.InvariantCulture),
            SplitTo = 1,
            SplitFrom = 20,
            CashInLieu = new DescribedMoney { Amount = new(150m) }, 
            ElectTaxDeferral = true 
        };

        UkSection104Pools section104Pools = new(new UKTaxYear(), new ResidencyStatusRecord());
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([trade1, split]);

        UkTradeCalculator calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();
        
        // Check for the excess gain disposal
        var cashCalc = result.OfType<CorporateActionTaxCalculation>().FirstOrDefault();
        cashCalc.ShouldNotBeNull("Excess gain should be recognized even when deferring if cash > cost.");
        
        cashCalc.TotalProceeds.Amount.ShouldBe(50m);
        cashCalc.TotalAllowableCost.Amount.ShouldBe(0m);
        cashCalc.Gain.Amount.ShouldBe(50m);
        
        // Final S104 state: 0 shares, £0 cost (100 - 100)
        var pool = section104Pools.GetExistingOrInitialise("EXCESS_GAIN_TEST");
        pool.Quantity.ShouldBe(0m);
        pool.AcquisitionCostInBaseCurrency.Amount.ShouldBe(0m, 0.000001m);
    }

    [Fact]
    public void TestStockSplitCashInLieuWithoutFraction_Throws()
    {
        Trade trade1 = new()
        {
            AssetName = "NO_FRACTION_TEST",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jan-22", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new() { Amount = new(1000m) }
        };

        StockSplit split = new()
        {
            AssetName = "NO_FRACTION_TEST",
            Date = DateTime.Parse("01-Feb-22", CultureInfo.InvariantCulture),
            SplitTo = 2,
            SplitFrom = 1,
            CashInLieu = new DescribedMoney { Amount = new(10m) },
            ElectTaxDeferral = false
        };

        UkSection104Pools section104Pools = new(new UKTaxYear(), new ResidencyStatusRecord());
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([trade1, split]);

        UkTradeCalculator calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104Pools, taxEventLists);

        var ex = Should.Throw<InvalidOperationException>(() => calculator.CalculateTax());
        ex.Message.ShouldContain("no fractional shares were produced");
    }
}
