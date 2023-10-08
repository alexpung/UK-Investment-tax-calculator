using Model;
using Model.Interfaces;
using Model.UkTaxModel;

namespace UnitTest.Test.TradeCalculations;
public class UkTradeCalculatorTest
{
    [Fact]
    public void TestCaculateShortPartialCover()
    {
        Trade trade1 = new()
        {
            AssetName = "DEF",
            BuySell = Enum.TradeType.SELL,
            Date = DateTime.Parse("08-Apr-21 12:34:56"),
            Description = "DEF Example Stock",
            Quantity = 100,
            GrossProceed = new() { Description = "Commission", Amount = new(1000m) },
        };
        Trade trade2 = new()
        {
            AssetName = "DEF",
            BuySell = Enum.TradeType.SELL,
            Date = DateTime.Parse("06-May-21 13:34:56"),
            Description = "DEF Example Stock",
            Quantity = 100,
            GrossProceed = new() { Description = "Commission", Amount = new(1500m) },
        };
        Trade trade3 = new()
        {
            AssetName = "DEF",
            BuySell = Enum.TradeType.BUY,
            Date = DateTime.Parse("09-May-21 13:34:56"),
            Description = "DEF Example Stock",
            Quantity = 300,
            GrossProceed = new() { Description = "", Amount = new(5000m) },
        };
        UkSection104Pools section104Pools = new();
        TaxEventLists taxEventLists = new TaxEventLists();
        taxEventLists.AddData(new List<Trade>() { trade1, trade2, trade3 });
        UkTradeCalculator calculator = new(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();
        result[0].Gain.ShouldBe(new WrappedMoney(-666.6666666666666666666666667m));
        result[0].TotalAllowableCost.ShouldBe(new WrappedMoney(1666.6666666666666666666666667m));
        result[0].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SHORTCOVER);
        result[1].Gain.ShouldBe(new WrappedMoney(-166.6666666666666666666666667m));
        result[1].TotalAllowableCost.ShouldBe(new WrappedMoney(1666.6666666666666666666666667m));
        result[1].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.BED_AND_BREAKFAST);
        section104Pools.GetExistingOrInitialise("DEF").ValueInBaseCurrency.ShouldBe(new WrappedMoney(1666.6666666666666666666666666m));
        section104Pools.GetExistingOrInitialise("DEF").Quantity.ShouldBe(100);
    }

    [Fact]
    public void TestHMRCExample()
    {   // initial pool 9500 shares
        Trade initSection104 = new()
        {
            AssetName = "Mesopotamia plc",
            BuySell = Enum.TradeType.BUY,
            Date = DateTime.Parse("11-Sep-20 12:00:00"),
            Description = "Purchase of 9500 shares",
            Quantity = 9500,
            GrossProceed = new() { Description = "", Amount = new(1850.0m, "GBP"), FxRate = 1.0m },
        };

        // Create trades representing Mr. Schneider's transactions
        Trade purchaseTrade1 = new()
        {
            AssetName = "Mesopotamia plc",
            BuySell = Enum.TradeType.BUY,
            Date = DateTime.Parse("11-Sep-21 12:00:00"),
            Description = "Purchase of 500 shares",
            Quantity = 500,
            GrossProceed = new() { Description = "", Amount = new(850.0m, "GBP"), FxRate = 1.0m },
        };

        Trade saleTrade1 = new()
        {
            AssetName = "Mesopotamia plc",
            BuySell = Enum.TradeType.SELL,
            Date = DateTime.Parse("30-Aug-21 14:00:00"),
            Description = "Sale of 4,000 shares",
            Quantity = 4000,
            GrossProceed = new() { Description = "", Amount = new(6000.0m, "GBP"), FxRate = 1.0m },
        };
        // Create a Section104Pools instance
        UkSection104Pools section104Pools = new();

        // Create a TaxEventLists instance and add the trades to it
        TaxEventLists taxEventLists = new TaxEventLists();
        taxEventLists.AddData(new List<Trade>() { initSection104, purchaseTrade1, saleTrade1 });

        // Create the UkTradeCalculator instance
        UkTradeCalculator calculator = new(section104Pools, taxEventLists);

        // Calculate the tax
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        // Assert the expected results
        // Bed and Breakfast Matching (500 shares)
        result[2].MatchHistory[0].BaseCurrencyMatchDisposalValue.ShouldBe(new WrappedMoney(750m));
        result[2].MatchHistory[0].BaseCurrencyMatchAcquitionValue.ShouldBe(new WrappedMoney(850m));
        result[2].MatchHistory[0].MatchQuantity.ShouldBe(500);
        result[2].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.BED_AND_BREAKFAST);

        // Section 104 Holding Matching (3,500 shares)
        result[2].Gain.ShouldBe(new WrappedMoney(4468.4210526315789473684210526m)); // (6000 * 500 / 4000 - 850) + (6000 * 3500 / 4000) - (1850 * 3500 / 9500)
        result[2].TotalAllowableCost.ShouldBe(new WrappedMoney(1531.5789473684210526315789474m));
        result[2].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.BED_AND_BREAKFAST);

        // Ensure the Section 104 pool is updated correctly
        section104Pools.GetExistingOrInitialise("Mesopotamia plc").ValueInBaseCurrency.ShouldBe(new WrappedMoney(1168.4210526315789473684210526m)); // 1850 - (1850 * 3500 / 9500)
        section104Pools.GetExistingOrInitialise("Mesopotamia plc").Quantity.ShouldBe(6000);
    }


    [Fact]
    public void TestCaculateShortCover()
    {
        Trade trade1 = new()
        {
            AssetName = "DEF",
            BuySell = Enum.TradeType.SELL,
            Date = DateTime.Parse("05-May-21 12:34:56"),
            Description = "DEF Example Stock",
            Quantity = 100,
            Expenses = new() { new() { Description = "Commission", Amount = new(1.5m, "USD"), FxRate = 0.86m } },
            GrossProceed = new() { Description = "Commission", Amount = new(1000m, "USD"), FxRate = 0.86m },
        };
        Trade trade2 = new()
        {
            AssetName = "DEF",
            BuySell = Enum.TradeType.BUY,
            Date = DateTime.Parse("06-Dec-21 12:34:56"),
            Description = "DEF Example Stock",
            Quantity = 100,
            Expenses = new() { new() { Description = "Commission", Amount = new(1.5m, "USD"), FxRate = 0.86m } },
            GrossProceed = new() { Description = "", Amount = new(500m, "USD"), FxRate = 0.86m },
        };
        UkSection104Pools section104Pools = new();
        TaxEventLists taxEventLists = new TaxEventLists();
        taxEventLists.AddData(new List<Trade>() { trade1, trade2 });
        UkTradeCalculator calculator = new(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();
        result[0].Gain.ShouldBe(new WrappedMoney(427.42m));
        result[0].TotalAllowableCost.ShouldBe(new WrappedMoney(431.29m));
        result[0].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SHORTCOVER);
        section104Pools.GetExistingOrInitialise("DEF").ValueInBaseCurrency.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        section104Pools.GetExistingOrInitialise("DEF").Quantity.ShouldBe(0);
    }


    [Fact]
    public void TestCaculateExample1()
    {
        Trade trade1 = new()
        {
            AssetName = "ABC",
            BuySell = Enum.TradeType.BUY,
            Date = DateTime.Parse("01-May-21 12:34:56"),
            Description = "ABC Example Stock",
            Quantity = 200,
            Expenses = new() { new() { Description = "Commission", Amount = new(1.5m, "USD"), FxRate = 0.8m }, new() { Description = "Tax", Amount = new(20m, "USD"), FxRate = 0.8m } },
            GrossProceed = new() { Description = "", Amount = new(2000m, "USD"), FxRate = 0.8m },
        };
        Trade trade2 = new()
        {
            AssetName = "ABC",
            BuySell = Enum.TradeType.BUY,
            Date = DateTime.Parse("03-May-21 12:33:56"),
            Description = "ABC Example Stock",
            Quantity = 50,
            Expenses = new() { new() { Description = "Commission", Amount = new(1.5m, "USD"), FxRate = 0.85m }, new() { Description = "Tax", Amount = new(30m, "USD"), FxRate = 0.85m } },
            GrossProceed = new() { Description = "", Amount = new(510m, "USD"), FxRate = 0.85m },
        };
        Trade trade3 = new()
        {
            AssetName = "ABC",
            BuySell = Enum.TradeType.SELL,
            Date = DateTime.Parse("03-May-21 12:34:56"),
            Description = "ABC Example Stock",
            Quantity = 200,
            Expenses = new() { new() { Description = "Commission", Amount = new(1.5m, "USD"), FxRate = 0.85m }, new() { Description = "Tax", Amount = new(40m, "USD"), FxRate = 0.85m } },
            GrossProceed = new() { Description = "", Amount = new(2200m, "USD"), FxRate = 0.85m },
        };
        Trade trade4 = new()
        {
            AssetName = "ABC",
            BuySell = Enum.TradeType.BUY,
            Date = DateTime.Parse("04-May-21 12:34:56"),
            Description = "ABC Example Stock",
            Quantity = 100,
            Expenses = new() { new() { Description = "Commission", Amount = new(1.5m, "USD"), FxRate = 0.86m }, new() { Description = "Tax", Amount = new(50m, "USD"), FxRate = 0.86m } },
            GrossProceed = new() { Description = "", Amount = new(600m, "USD"), FxRate = 0.86m },
        };
        StockSplit stockSplit = new() { AssetName = "ABC", Date = DateTime.Parse("03-May-21 20:25:00"), NumberAfterSplit = 2, NumberBeforeSplit = 1 };
        UkSection104Pools section104Pools = new();
        TaxEventLists taxEventLists = new TaxEventLists();
        taxEventLists.AddData(new List<TaxEvent>() { trade1, trade2, trade3, trade4, stockSplit });
        UkTradeCalculator calculator = new(section104Pools, taxEventLists);
        List<ITradeTaxCalculation> result = calculator.CalculateTax();
        result[2].TotalProceeds.ShouldBe(new WrappedMoney(1834.725m));
        result[2].Gain.ShouldBe(new WrappedMoney(5.56m));
        result[2].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SAME_DAY);
        result[2].MatchHistory[0].MatchQuantity.ShouldBe(50);
        result[2].MatchHistory[0].BaseCurrencyMatchDisposalValue.ShouldBe(new WrappedMoney(458.68125m));
        result[2].MatchHistory[0].BaseCurrencyMatchAcquitionValue.ShouldBe(new WrappedMoney(460.275m));
        result[2].MatchHistory[1].TradeMatchType.ShouldBe(TaxMatchType.BED_AND_BREAKFAST);
        result[2].MatchHistory[1].MatchQuantity.ShouldBe(50);
        result[2].MatchHistory[1].BaseCurrencyMatchDisposalValue.ShouldBe(new WrappedMoney(458.68125m));
        result[2].MatchHistory[1].BaseCurrencyMatchAcquitionValue.ShouldBe(new WrappedMoney(560.29m));
        result[2].MatchHistory[2].TradeMatchType.ShouldBe(TaxMatchType.SECTION_104);
        result[2].MatchHistory[2].MatchQuantity.ShouldBe(100);
        result[2].MatchHistory[2].BaseCurrencyMatchDisposalValue.ShouldBe(new WrappedMoney(917.3625m));
        result[2].MatchHistory[2].BaseCurrencyMatchAcquitionValue.ShouldBe(new WrappedMoney(808.6m));
        section104Pools.GetExistingOrInitialise("ABC").ValueInBaseCurrency.ShouldBe(new WrappedMoney(808.6m));
        section104Pools.GetExistingOrInitialise("ABC").Quantity.ShouldBe(200);
    }

    [Fact]
    public void TestCalculateSection104Gain()
    {
        // Create a trade representing the purchase of an asset
        Trade buyTrade = new()
        {
            AssetName = "ABC",
            BuySell = Enum.TradeType.BUY,
            Date = DateTime.Parse("01-Jan-21 10:00:00"),
            Description = "Purchase of ABC Stock",
            Quantity = 200,
            Expenses = new() { new() { Description = "Commission", Amount = new(2.0m, "USD"), FxRate = 0.85m } },
            GrossProceed = new() { Description = "", Amount = new(2000m, "USD"), FxRate = 0.85m },
        };

        // Create a trade representing the sale of the same asset
        Trade sellTrade = new()
        {
            AssetName = "ABC",
            BuySell = Enum.TradeType.SELL,
            Date = DateTime.Parse("15-Dec-21 15:30:00"),
            Description = "Sale of ABC Stock",
            Quantity = 150,
            Expenses = new() { new() { Description = "Commission", Amount = new(1.5m, "USD"), FxRate = 0.85m } },
            GrossProceed = new() { Description = "", Amount = new(2250m, "USD"), FxRate = 0.85m },
        };

        // Create a Section104Pools instance
        UkSection104Pools section104Pools = new();

        // Create a TaxEventLists instance and add the trades to it
        TaxEventLists taxEventLists = new TaxEventLists();
        taxEventLists.AddData(new List<Trade>() { buyTrade, sellTrade });

        // Create the UkTradeCalculator instance
        UkTradeCalculator calculator = new(section104Pools, taxEventLists);

        // Calculate the tax
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        // Assert the expected results
        result[1].Gain.ShouldBe(new WrappedMoney(634.95m)); // (2250 - 1.5 - (2000 + 2) * (150 / 200)) * 0.85
        result[1].TotalAllowableCost.ShouldBe(new WrappedMoney(1276.275m)); // (2000 + 2) * (150 / 200) * 0.85
        result[1].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SECTION_104);

        // Assert that Section 104 pool is updated correctly
        section104Pools.GetExistingOrInitialise("ABC").ValueInBaseCurrency.ShouldBe(new WrappedMoney(425.425m)); // (2000 + 2) * (50 / 200) * 0.85
        section104Pools.GetExistingOrInitialise("ABC").Quantity.ShouldBe(50); // 200 - 150
    }

    [Fact]
    public void TestSameDayMatching()
    {
        // Create two trades representing a same-day purchase and sale of the same asset
        Trade purchaseTrade = new()
        {
            AssetName = "XYZ",
            BuySell = Enum.TradeType.BUY,
            Date = DateTime.Parse("02-Feb-22 09:30:00"),
            Description = "Purchase of XYZ Stock",
            Quantity = 100,
            Expenses = new() { new() { Description = "Commission", Amount = new(1.0m, "USD"), FxRate = 0.88m } },
            GrossProceed = new() { Description = "", Amount = new(1000m, "USD"), FxRate = 0.88m },
        };

        Trade saleTrade = new()
        {
            AssetName = "XYZ",
            BuySell = Enum.TradeType.SELL,
            Date = DateTime.Parse("02-Feb-22 15:45:00"),
            Description = "Sale of XYZ Stock",
            Quantity = 100,
            Expenses = new() { new() { Description = "Commission", Amount = new(1.0m, "USD"), FxRate = 0.85m } },
            GrossProceed = new() { Description = "", Amount = new(1100m, "USD"), FxRate = 0.85m },
        };

        // Create a Section104Pools instance
        UkSection104Pools section104Pools = new();

        // Create a TaxEventLists instance and add the trades to it
        TaxEventLists taxEventLists = new TaxEventLists();
        taxEventLists.AddData(new List<Trade>() { purchaseTrade, saleTrade });

        // Create the UkTradeCalculator instance
        UkTradeCalculator calculator = new(section104Pools, taxEventLists);

        // Calculate the tax
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        // Assert the expected results
        result[0].Gain.ShouldBe(new WrappedMoney(53.27m)); // (1100 - 1) * 0.85 - (1000 + 1) * 0.88
        result[0].TotalAllowableCost.ShouldBe(new WrappedMoney(880.88m)); // (1000 + 1) * 0.88
        result[0].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SAME_DAY);

        // Assert that Section 104 pool is updated correctly
        section104Pools.GetExistingOrInitialise("XYZ").ValueInBaseCurrency.ShouldBe(new WrappedMoney(0m));
        section104Pools.GetExistingOrInitialise("XYZ").Quantity.ShouldBe(0);
    }

    [Fact]
    public void TestBedAndBreakfastMatchingWithDifferentFXRates()
    {
        Trade purchaseTrade1 = new()
        {
            AssetName = "BBB",
            BuySell = Enum.TradeType.BUY,
            Date = DateTime.Parse("9-Apr-22 09:30:00"),
            Description = "Purchase of BBB Stock",
            Quantity = 200,
            Expenses = new() { new() { Description = "Commission", Amount = new(2.0m, "USD"), FxRate = 0.90m } },
            GrossProceed = new() { Description = "", Amount = new(2000m, "USD"), FxRate = 0.90m },
        };

        // Create two trades representing bed and breakfast transactions for the same asset

        Trade saleTrade = new()
        {
            AssetName = "BBB",
            BuySell = Enum.TradeType.SELL,
            Date = DateTime.Parse("12-Apr-22 15:45:00"),
            Description = "Sale of BBB Stock",
            Quantity = 100,
            Expenses = new() { new() { Description = "Commission", Amount = new(1.0m, "USD"), FxRate = 0.88m } }, // Different FX rate
            GrossProceed = new() { Description = "", Amount = new(1100m, "USD"), FxRate = 0.88m }, // Different FX rate
        };

        Trade purchaseTrade2 = new()
        {
            AssetName = "BBB",
            BuySell = Enum.TradeType.BUY,
            Date = DateTime.Parse("12-May-22 09:30:00"),
            Description = "Purchase of BBB Stock",
            Quantity = 200,
            Expenses = new() { new() { Description = "Commission", Amount = new(2.0m, "USD"), FxRate = 0.85m } },
            GrossProceed = new() { Description = "", Amount = new(2500m, "USD"), FxRate = 0.85m },
        };

        // Create a Section104Pools instance
        UkSection104Pools section104Pools = new();

        // Create a TaxEventLists instance and add the trades to it
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData(new List<Trade>() { purchaseTrade2, purchaseTrade1, saleTrade });

        // Create the UkTradeCalculator instance
        UkTradeCalculator calculator = new(section104Pools, taxEventLists);

        // Calculate the tax
        List<ITradeTaxCalculation> result = calculator.CalculateTax();

        // Assert the expected results for bed and breakfast matching with different FX rates
        result[2].Gain.ShouldBe(new WrappedMoney(-96.23m)); // (1100 - 1) * 0.88 - (2500 + 2) * 100 / 200 * 0.85
        result[2].TotalAllowableCost.ShouldBe(new WrappedMoney(1063.35m)); // (2500 + 2) * 100 / 200 * 0.85
        result[2].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.BED_AND_BREAKFAST);

        // Assert that Section 104 pool is updated correctly
        section104Pools.GetExistingOrInitialise("BBB").ValueInBaseCurrency.ShouldBe(new WrappedMoney(2865.15m)); // (2500 + 2) * 100 / 200 * 0.85 + (2000 + 2) * 0.9
        section104Pools.GetExistingOrInitialise("BBB").Quantity.ShouldBe(300);
    }
}
