using Enumerations;

using Model;
using Model.Interfaces;
using Model.TaxEvents;
using Model.UkTaxModel;

using System.Globalization;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations;
public class UkTradeCalculatorTest2Trade
{
    [Fact]
    public void TestSimpleShortCover()
    {
        Trade trade1 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("05-May-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 100,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(1.5m, "USD"), FxRate = 0.86m }],
            GrossProceed = new() { Description = "Commission", Amount = new(1000m, "USD"), FxRate = 0.86m },
        };
        Trade trade2 = new()
        {
            AssetName = "DEF",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("06-Dec-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Stock",
            Quantity = 100,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(1.5m, "USD"), FxRate = 0.86m }],
            GrossProceed = new() { Description = "", Amount = new(500m, "USD"), FxRate = 0.86m },
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { trade1, trade2 }, out UkSection104Pools section104Pools);
        result[0].Gain.ShouldBe(new WrappedMoney(427.42m));
        result[0].TotalAllowableCost.ShouldBe(new WrappedMoney(431.29m));
        result[0].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SHORTCOVER);
        section104Pools.GetExistingOrInitialise("DEF").AcquisitionCostInBaseCurrency.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        section104Pools.GetExistingOrInitialise("DEF").Quantity.ShouldBe(0);
    }

    [Fact]
    public void TestSimpleSection104()
    {
        // Create a trade representing the purchase of an asset
        Trade buyTrade = new()
        {
            AssetName = "ABC",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("01-Jan-21 10:00:00", CultureInfo.InvariantCulture),
            Description = "Purchase of ABC Stock",
            Quantity = 200,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(2.0m, "USD"), FxRate = 0.85m }],
            GrossProceed = new() { Description = "", Amount = new(2000m, "USD"), FxRate = 0.85m },
        };

        // Create a trade representing the sale of the same asset
        Trade sellTrade = new()
        {
            AssetName = "ABC",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("15-Dec-21 15:30:00", CultureInfo.InvariantCulture),
            Description = "Sale of ABC Stock",
            Quantity = 150,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(1.5m, "USD"), FxRate = 0.85m }],
            GrossProceed = new() { Description = "", Amount = new(2250m, "USD"), FxRate = 0.85m },
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { buyTrade, sellTrade }, out UkSection104Pools section104Pools);
        result[1].Gain.ShouldBe(new WrappedMoney(634.95m)); // (2250 - 1.5 - (2000 + 2) * (150 / 200)) * 0.85
        result[1].TotalAllowableCost.ShouldBe(new WrappedMoney(1276.275m)); // (2000 + 2) * (150 / 200) * 0.85
        result[1].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SECTION_104);

        // Assert that Section 104 pool is updated correctly
        section104Pools.GetExistingOrInitialise("ABC").AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(425.425m)); // (2000 + 2) * (50 / 200) * 0.85
        section104Pools.GetExistingOrInitialise("ABC").Quantity.ShouldBe(50); // 200 - 150
    }

    [Fact]
    public void TestSimpleSameDayMatching()
    {
        // Create two trades representing a same-day purchase and sale of the same asset
        Trade purchaseTrade = new()
        {
            AssetName = "XYZ",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("02-Feb-22 09:30:00", CultureInfo.InvariantCulture),
            Description = "Purchase of XYZ Stock",
            Quantity = 100,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(1.0m, "USD"), FxRate = 0.88m }],
            GrossProceed = new() { Description = "", Amount = new(1000m, "USD"), FxRate = 0.88m },
        };

        Trade saleTrade = new()
        {
            AssetName = "XYZ",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("02-Feb-22 15:45:00", CultureInfo.InvariantCulture),
            Description = "Sale of XYZ Stock",
            Quantity = 100,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(1.0m, "USD"), FxRate = 0.85m }],
            GrossProceed = new() { Description = "", Amount = new(1100m, "USD"), FxRate = 0.85m },
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { purchaseTrade, saleTrade }, out UkSection104Pools section104Pools);
        // Assert the expected results
        result[1].Gain.ShouldBe(new WrappedMoney(53.27m)); // (1100 - 1) * 0.85 - (1000 + 1) * 0.88
        result[1].TotalAllowableCost.ShouldBe(new WrappedMoney(880.88m)); // (1000 + 1) * 0.88
        result[1].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SAME_DAY);

        // Assert that Section 104 pool is updated correctly
        section104Pools.GetExistingOrInitialise("XYZ").AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(0m));
        section104Pools.GetExistingOrInitialise("XYZ").Quantity.ShouldBe(0);
    }

    [Fact]
    public void TestShortBedAndBreakfast()
    {
        Trade saleTrade = new()
        {
            AssetName = "BBB",
            AcquisitionDisposal = TradeType.DISPOSAL,
            Date = DateTime.Parse("12-Apr-22 15:45:00", CultureInfo.InvariantCulture),
            Description = "Sale of BBB Stock",
            Quantity = 100,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(1.0m, "USD"), FxRate = 0.88m }], // Different FX rate
            GrossProceed = new() { Description = "", Amount = new(1100m, "USD"), FxRate = 0.88m }, // Different FX rate
        };

        Trade purchaseTrade1 = new()
        {
            AssetName = "BBB",
            AcquisitionDisposal = TradeType.ACQUISITION,
            Date = DateTime.Parse("12-May-22 09:30:00", CultureInfo.InvariantCulture),
            Description = "Purchase of BBB Stock",
            Quantity = 200,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(2.0m, "USD"), FxRate = 0.85m }],
            GrossProceed = new() { Description = "", Amount = new(2500m, "USD"), FxRate = 0.85m },
        };

        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { purchaseTrade1, saleTrade }, out UkSection104Pools section104Pools);
        // Assert the expected results for bed and breakfast matching with different FX rates
        result[1].Gain.ShouldBe(new WrappedMoney(-96.23m)); // (1100 - 1) * 0.88 - (2500 + 2) * 100 / 200 * 0.85
        result[1].TotalAllowableCost.ShouldBe(new WrappedMoney(1063.35m)); // (2500 + 2) * 100 / 200 * 0.85
        result[1].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.BED_AND_BREAKFAST);

        // Assert that Section 104 pool is updated correctly
        section104Pools.GetExistingOrInitialise("BBB").AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(1063.35m)); // (2500 + 2) * 100 / 200 * 0.85
        section104Pools.GetExistingOrInitialise("BBB").Quantity.ShouldBe(100);
    }
}
