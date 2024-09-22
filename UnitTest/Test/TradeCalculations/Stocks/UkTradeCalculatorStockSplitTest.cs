using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using System.Globalization;

namespace UnitTest.Test.TradeCalculations.Stocks;
public class UkTradeCalculatorStockSplitTest
{
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

        UkSection104Pools section104Pools = new();
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([initialPurchase, stockSplit, postSplitSale]);

        UkTradeCalculator calculator = new(section104Pools, taxEventLists);
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
        UkSection104Pools section104Pools = new();
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([trade1, stockSplit, trade2, trade3]);

        UkTradeCalculator calculator = new(section104Pools, taxEventLists);
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

        UkSection104Pools section104Pools = new();
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([trade1, stockSplit, trade2, trade3]);
        UkTradeCalculator calculator = new(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        result[1].Gain.Amount.ShouldBe(700m, 0.01m); // Gain is the disposal value minus acquisition cost: 2500 - 1300 + 2000 * 50 * (2000 / 200) = 700
        result[1].TotalAllowableCost.Amount.ShouldBe(1800m, 0.01m); // Acquisition cost of the sold shares: 1300 + 2000 * 50 * (2000 / 200)
        result[1].TotalProceeds.Amount.ShouldBe(2500m, 0.01m); // Total proceeds from the sale: 100 shares sold at £25 each

        // Verify the Section 104 pool after the sale
        section104Pools.GetExistingOrInitialise("ABC").AcquisitionCostInBaseCurrency.Amount.ShouldBe(1500m, 0.01m); // Remaining value in pool (2000 - 500)
        section104Pools.GetExistingOrInitialise("ABC").Quantity.ShouldBe(150); // Remaining quantity in pool (200 - 100)
    }

}
