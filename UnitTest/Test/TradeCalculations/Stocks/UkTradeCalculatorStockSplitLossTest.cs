
using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;
using Shouldly;
using System.Globalization;
using UnitTest.Helper;
using System.Linq;

namespace UnitTest.Test.TradeCalculations.Stocks;

public class UkTradeCalculatorStockSplitLossTest
{
    [Fact]
    public void TestStockSplitCashInLieuLoss()
    {
        // Initial purchase: 95 shares @ £20 = £1900 total cost
        Trade trade1 = new()
        {
            AssetName = "LOSS_TEST",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jan-22", CultureInfo.InvariantCulture),
            Quantity = 95,
            GrossProceed = new() { Amount = new(1900m) }
        };

        // Reverse split 1-for-10. 
        // 95 shares -> 9.5 shares raw.
        // We get 9 shares and cash for 0.5 share.
        // Allocated Cost to 0.5 share = 1900 * (0.5 / 9.5) = £100.
        // Cash Received = £50.
        // Loss = £50 - £100 = -£50.

        StockSplit split = new()
        {
            AssetName = "LOSS_TEST",
            Date = DateTime.Parse("01-Feb-22", CultureInfo.InvariantCulture),
            SplitTo = 1,
            SplitFrom = 10,
            CashInLieu = new DescribedMoney { Amount = new(50m) }, 
            ElectTaxDeferral = false, // Explicitly choosing NOT to defer to force recognition
        };

        UkSection104Pools section104Pools = new(new UKTaxYear(), new ResidencyStatusRecord());
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([trade1, split]);

        UkTradeCalculator calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        // result[0] is Acquisition
        // result[1] should be CashDisposal (Loss)
        
        var cashCalc = result.OfType<CorporateActionTaxCalculation>().FirstOrDefault();
        cashCalc.ShouldNotBeNull();
        
        var stockSplitResult = (StockSplit)cashCalc.RelatedCorporateAction;
        
        stockSplitResult.CashDisposal.ShouldNotBeNull();
        stockSplitResult.CashDisposal.TotalProceeds.Amount.ShouldBe(50m);
        stockSplitResult.CashDisposal.TotalAllowableCost.Amount.ShouldBe(100m, 0.01m); // 1900 * (0.5/9.5)
        stockSplitResult.CashDisposal.Gain.Amount.ShouldBe(-50m, 0.01m); // 50 - 100
        
        // Also verify the final pool state
        // Remaining Quantity = 9.
        // Remaining Cost = 1900 - 100 = 1800.
        var pool = section104Pools.GetExistingOrInitialise("LOSS_TEST");
        pool.Quantity.ShouldBe(9);
        pool.AcquisitionCostInBaseCurrency.Amount.ShouldBe(1800m, 0.01m);
    }

    [Fact]
    public void TestStockSplitCashInLieuLoss_WithDeferralCheckFail()
    {
        // Initial purchase: 100 shares @ £100 = £10,000 total cost
        Trade trade1 = new()
        {
            AssetName = "LARGE_LOSS_TEST",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jan-22", CultureInfo.InvariantCulture),
            Quantity = 100,
            GrossProceed = new() { Amount = new(10000m) }
        };

        // Reverse split 1000-for-19. 
        // 100 shares -> 1.9 new shares.
        // Floor = 1 share. Fractional = 0.9 share.
        // Allocated Cost to fractional part = 10000 * (0.9 / 1.9) = £4736.842105...
        // Cash Received = £4000. 
        // £4000 > £3000 (Small limit). So Deferral should fail even if ElectTaxDeferral=true.
        // Loss = £4000 - £4736.84... = -£736.84...

        StockSplit split = new()
        {
            AssetName = "LARGE_LOSS_TEST",
            Date = DateTime.Parse("01-Feb-22", CultureInfo.InvariantCulture),
            SplitTo = 19,
            SplitFrom = 1000,
            CashInLieu = new DescribedMoney { Amount = new(4000m) }, 
            ElectTaxDeferral = true, // Trying to defer, but shouldn't be allowed due to size
        };

        UkSection104Pools section104Pools = new(new UKTaxYear(), new ResidencyStatusRecord());
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([trade1, split]);

        UkTradeCalculator calculator = TradeCalculationHelper.CreateUkTradeCalculator(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();
        
        var cashCalc = result.OfType<CorporateActionTaxCalculation>().FirstOrDefault();
        cashCalc.ShouldNotBeNull();
        
        var stockSplitResult = (StockSplit)cashCalc.RelatedCorporateAction;
        
        stockSplitResult.CashDisposal.ShouldNotBeNull();
        stockSplitResult.CashDisposal.TotalProceeds.Amount.ShouldBe(4000m);

        decimal expectedCost = 10000m * (0.9m / 1.9m); // 4736.8421
        decimal expectedLoss = 4000m - expectedCost;   // -736.8421

        stockSplitResult.CashDisposal.TotalAllowableCost.Amount.ShouldBe(expectedCost, 0.01m);
        stockSplitResult.CashDisposal.Gain.Amount.ShouldBe(expectedLoss, 0.01m);
        
        // Also verify the final pool state
        // Remaining Quantity = 1.
        // Remaining Cost = 10000 - 4736.84... = 5263.16...
        var pool = section104Pools.GetExistingOrInitialise("LARGE_LOSS_TEST");
        pool.Quantity.ShouldBe(1);
        pool.AcquisitionCostInBaseCurrency.Amount.ShouldBe(10000m - expectedCost, 0.01m);
    }
}
