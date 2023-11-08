using Enum;

using Model;
using Model.Interfaces;
using Model.TaxEvents;

using System.Collections.Immutable;

using TaxEvents;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations;
public class UkTradeCalculatorTest3FutureTrade
{
    [Fact]
    public void TestFutureContractMatchingBuySellSell()
    {
        // Open long 150 future contracts
        FutureContractTrade buyTrade = new()
        {
            AssetName = "GHI Future",
            BuySell = TradeType.BUY,
            Date = DateTime.Parse("02-Jun-22 10:00:00"),
            Description = "Purchase of GHI Future",
            Quantity = 150,
            Expenses = ImmutableList.Create(new DescribedMoney() { Description = "Commission", Amount = new WrappedMoney(100m, "JPY"), FxRate = 0.007m }),
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(1000000m, "JPY"), FxRate = 0.007m }
        };

        // Close 100 contracts on the same day
        FutureContractTrade sellTradeSameDay = new()
        {
            AssetName = "GHI Future",
            BuySell = TradeType.SELL,
            Date = DateTime.Parse("02-Jun-22 15:00:00"),
            Description = "Sale of GHI Future",
            Quantity = 100,
            Expenses = ImmutableList.Create(new DescribedMoney() { Description = "Commission", Amount = new WrappedMoney(150m, "JPY"), FxRate = 0.0075m }),
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(1050000m, "JPY"), FxRate = 0.0075m }
        };

        // Close the remaining 50 future contracts on a different day
        FutureContractTrade sellTradeDifferentDay = new()
        {
            AssetName = "GHI Future",
            BuySell = TradeType.SELL,
            Date = DateTime.Parse("03-Jun-22 11:00:00"),
            Description = "Sale of GHI Future",
            Quantity = 50,
            Expenses = ImmutableList.Create(new DescribedMoney() { Description = "Commission", Amount = new WrappedMoney(50m, "JPY"), FxRate = 0.008m }),
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(300000m, "JPY"), FxRate = 0.008m }
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { buyTrade, sellTradeSameDay, sellTradeDifferentDay }, out _);
        result[1].Gain.Amount.ShouldBe(2873.41m, 0.01m); // (1,050,000 - 100/150 * 1,000,000) * 0.0075 - 150 * 0.0075 - (100/150) * 100 * 0.007 = 2873.408333
        result[1].TotalAllowableCost.ShouldBe(new WrappedMoney(1.59166667m)); // 150 * 0.0075 + (100/150) * 100 * 0.007 = 1.59166667
        result[1].TotalProceeds.Amount.ShouldBe(2875m, 0.01m); // (1,050,000 - 100/150 * 1,000,000) * 0.0075
        result[1].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SAME_DAY);
        result[2].Gain.ShouldBe(new WrappedMoney(-267.29995m)); // (300,000 - 333,333.33 * 0.008) - 0.23331 (Trade 1 commission) - 0.4 (Trade 3 commission)
        result[2].TotalAllowableCost.ShouldBe(new WrappedMoney(0.63331m)); // 0.23331 (Trade 1 commission) + 0.4 (Trade 3 commission)
        result[2].TotalProceeds.ShouldBe(new WrappedMoney(0m)); // No proceeds from a loss-making future contract sale
        result[2].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SECTION_104);
    }

    [Fact]
    public void TestFutureContractMatchingBuySellBuy()
    {
        // Trade 1: Buying 50 future contracts on day 1
        FutureContractTrade buyTradeDay1 = new()
        {
            AssetName = "JKL Future",
            BuySell = TradeType.BUY,
            Date = DateTime.Parse("01-Jun-22 10:00:00"),
            Description = "Purchase of JKL Future",
            Quantity = 50,
            Expenses = ImmutableList.Create(new DescribedMoney() { Description = "Commission", Amount = new WrappedMoney(100m, "JPY"), FxRate = 0.007m }),
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(500000m, "JPY"), FxRate = 0.007m }
        };

        // Trade 2: Selling 150 future contracts on day 2
        FutureContractTrade sellTradeDay2 = new()
        {
            AssetName = "JKL Future",
            BuySell = TradeType.SELL,
            Date = DateTime.Parse("02-Jun-22 15:00:00"),
            Description = "Sale of JKL Future",
            Quantity = 150,
            Expenses = ImmutableList.Create(new DescribedMoney() { Description = "Commission", Amount = new WrappedMoney(150m, "JPY"), FxRate = 0.0075m }),
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(1600000m, "JPY"), FxRate = 0.0075m }
        };

        // Trade 3: Buying 100 future contracts on day 3
        FutureContractTrade buyTradeDay3 = new()
        {
            AssetName = "JKL Future",
            BuySell = TradeType.BUY,
            Date = DateTime.Parse("03-Jun-22 11:00:00"),
            Description = "Purchase of JKL Future",
            Quantity = 100,
            Expenses = ImmutableList.Create(new DescribedMoney() { Description = "Commission", Amount = new WrappedMoney(50m, "JPY"), FxRate = 0.008m }),
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(1000000m, "JPY"), FxRate = 0.008m }
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { buyTradeDay1, sellTradeDay2, buyTradeDay3 }, out _);
        result[1].Gain.ShouldBe(new WrappedMoney(248.925m)); // (1600000 * 50/150 - 500,000) * 0.0075 - 150 * 0.0075 * (50/150) - 100 * 0.007 = 248.925
        result[1].TotalAllowableCost.ShouldBe(new WrappedMoney(1.075m)); // 150 * 0.0075 * (50/150) + 100 * 0.007 = 1.075
        result[1].TotalProceeds.Amount.ShouldBe(250m, 0.01m); // (1600000 * 50/150 - 500,000) * 0.0075 = 250
        result[1].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SAME_DAY);
        result[2].Gain.Amount.ShouldBe(-3040.67m, 0.01m); // (120,000 - 50/100 * 666,666.67) * 0.008 - 50 * 0.008 - (50/150) * 100 * 0.008
        result[2].TotalAllowableCost.Amount.ShouldBe(-3040.67m, 0.01m); // Same as loss above
        result[2].TotalProceeds.Amount.ShouldBe(0); // No proceeds from a loss-making future contract sale
        result[2].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SECTION_104);
    }
}
