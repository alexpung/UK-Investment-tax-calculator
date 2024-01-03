using Enum;

using Model;
using Model.Interfaces;
using Model.TaxEvents;
using Model.UkTaxModel;

using System.Globalization;

using TaxEvents;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations;
public class UkTradeCalculatorTest2FutureTrade
{
    [Fact]
    public void TestShortFutureContractIsAcqusition()
    {
        // Open short position
        FutureContractTrade trade1 = new()
        {
            AssetName = "DEF Future",
            BuySell = TradeType.SELL,
            Date = DateTime.Parse("05-May-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Future",
            Quantity = 100,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(100m, "JPY"), FxRate = 0.006m }],
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(1000000, "JPY"), FxRate = 0.006m }
        };
        // Close short position
        FutureContractTrade trade2 = new()
        {
            AssetName = "DEF Future",
            BuySell = TradeType.BUY,
            Date = DateTime.Parse("06-Dec-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Future",
            Quantity = 100,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(200m, "JPY"), FxRate = 0.007m }],
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(1200000, "JPY"), FxRate = 0.007m }
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { trade1, trade2 }, out UkSection104Pools section104Pools);
        result[1].Gain.ShouldBe(new WrappedMoney(-1402m)); // payment received: (1000000 - 1200000) * 0.007 = -1400. Gain = -1400 - 2 = -1402.
        result[1].TotalAllowableCost.ShouldBe(new WrappedMoney(1402m)); // same as the loss
        result[1].TotalProceeds.ShouldBe(new WrappedMoney(0m));
        result[1].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SECTION_104); // The sell trade is an OPEN trade, it is an acquisition of a short contract position
        section104Pools.GetExistingOrInitialise("DEF Future").AcquisitionCostInBaseCurrency.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        section104Pools.GetExistingOrInitialise("DEF Future").Quantity.ShouldBe(0);
    }


    [Fact]
    public void TestShortFutureContractSameDay()
    {
        // Open short position
        FutureContractTrade trade1 = new()
        {
            AssetName = "DEF Future",
            BuySell = TradeType.SELL,
            Date = DateTime.Parse("05-May-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Future",
            Quantity = 100,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(100m, "JPY"), FxRate = 0.006m }],
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(1000000, "JPY"), FxRate = 0.006m }
        };
        // Close short position
        FutureContractTrade trade2 = new()
        {
            AssetName = "DEF Future",
            BuySell = TradeType.BUY,
            Date = DateTime.Parse("05-May-21 13:34:56", CultureInfo.InvariantCulture),
            Description = "DEF Example Future",
            Quantity = 100,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(200m, "JPY"), FxRate = 0.007m }],
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(1200000, "JPY"), FxRate = 0.007m }
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { trade1, trade2 }, out UkSection104Pools section104Pools);
        result[1].Gain.ShouldBe(new WrappedMoney(-1402m)); // payment received: (1000000 - 1200000) * 0.007 = -1400. Gain = -1400 - 2 = -1402.
        result[1].TotalAllowableCost.ShouldBe(new WrappedMoney(1402m)); // same as the loss
        result[1].TotalProceeds.ShouldBe(new WrappedMoney(0m));
        result[1].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SAME_DAY); // The sell trade is an OPEN trade, it is an acquisition of a short contract position
        section104Pools.GetExistingOrInitialise("DEF Future").AcquisitionCostInBaseCurrency.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        section104Pools.GetExistingOrInitialise("DEF Future").Quantity.ShouldBe(0);
    }

    [Fact]
    public void TestPartialFutureContractSale()
    {
        // Open long position
        FutureContractTrade buyTrade = new()
        {
            AssetName = "DEF Future",
            BuySell = TradeType.BUY,
            Date = DateTime.Parse("05-May-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "Purchase of DEF Future",
            Quantity = 100,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(100m, "JPY"), FxRate = 0.006m }],
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(1000000m, "JPY"), FxRate = 0.006m }
        };

        // Partially close buy position
        FutureContractTrade sellTrade = new()
        {
            AssetName = "DEF Future",
            BuySell = TradeType.SELL,
            Date = DateTime.Parse("06-Dec-21 12:34:56", CultureInfo.InvariantCulture),
            Description = "Partial Sale of DEF Future",
            Quantity = 50,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(100m, "JPY"), FxRate = 0.007m }],
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(600000m, "JPY"), FxRate = 0.007m }
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { buyTrade, sellTrade }, out UkSection104Pools section104Pools);
        result[1].Gain.ShouldBe(new WrappedMoney(699m));  // Gain = (600000 - 500000) * 0.007 - (50 * 0.006 + 100 * 0.007)
        result[1].TotalAllowableCost.ShouldBe(new WrappedMoney(1m)); // TotalAllowableCost = 50 * 0.006 + 100 * 0.007
        result[1].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SECTION_104);
        section104Pools.GetExistingOrInitialise("DEF Future").AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(0.3m));  // 50 * 0.006
        section104Pools.GetExistingOrInitialise("DEF Future").TotalContractValue.Amount.ShouldBe(500000m); // 1000000 - 500000
        section104Pools.GetExistingOrInitialise("DEF Future").Quantity.ShouldBe(50); // 100 - 50
    }

    [Fact]
    public void TestSameDayMatching()
    {
        // Open long position
        FutureContractTrade buyTrade = new()
        {
            AssetName = "DEF Future",
            BuySell = TradeType.BUY,
            Date = DateTime.Parse("01-Jun-22 10:30:00", CultureInfo.InvariantCulture),
            Description = "Purchase of DEF Future",
            Quantity = 150,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(150m, "JPY"), FxRate = 0.007m }],
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(900000m, "JPY"), FxRate = 0.007m }
        };

        // Closing long position in same day
        FutureContractTrade sellTrade = new()
        {
            AssetName = "DEF Future",
            BuySell = TradeType.SELL,
            Date = DateTime.Parse("01-Jun-22 14:45:00", CultureInfo.InvariantCulture),
            Description = "Sale of DEF Future",
            Quantity = 150,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new(200m, "JPY"), FxRate = 0.007m }],
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(930000m, "JPY"), FxRate = 0.007m }
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { buyTrade, sellTrade }, out UkSection104Pools section104Pools);
        result[1].Gain.ShouldBe(new WrappedMoney(207.55m)); // (930000 - 900000) * 0.007 - (150 * 0.007 + 200 * 0.007)
        result[1].TotalAllowableCost.Amount.ShouldBe(2.45m, 0.01m); // 150 * 0.007 + 200 * 0.007
        result[1].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SAME_DAY);
        var section104Pool = section104Pools.GetExistingOrInitialise("DEF Future");
        section104Pool.AcquisitionCostInBaseCurrency.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        section104Pool.Quantity.ShouldBe(0);
    }
}
