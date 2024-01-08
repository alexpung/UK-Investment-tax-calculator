using Enumerations;

using Model;
using Model.Interfaces;
using Model.TaxEvents;

using System.Globalization;

using TaxEvents;

using UnitTest.Helper;

namespace UnitTest.Test.TradeCalculations;
public class UkTradeCalculatorTestBedAndBreakfast
{
    [Fact]
    public void TestFutureContractBreadAndBreadFastOnlyMatchIfUnmatchedRemaining()
    {
        FutureContractTrade buyTrade = new()
        {
            AssetName = "GHI Future",
            BuySell = TradeType.BUY,
            Date = DateTime.Parse("02-Jun-22 10:00:00", CultureInfo.InvariantCulture),
            Description = "Purchase of GHI Future",
            Quantity = 5000,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new WrappedMoney(100m, "JPY"), FxRate = 0.007m }],
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(1000000m, "JPY"), FxRate = 0.007m }
        };

        FutureContractTrade disposeLongPosition = new()
        {
            AssetName = "GHI Future",
            BuySell = TradeType.SELL,
            Date = DateTime.Parse("03-Jun-22 15:00:00", CultureInfo.InvariantCulture),
            Description = "Sale of GHI Future",
            Quantity = 100,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new WrappedMoney(150m, "JPY"), FxRate = 0.0075m }],
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(1050000m, "JPY"), FxRate = 0.0075m }
        };

        FutureContractTrade bedAndBreakfastBuy1 = new()
        {
            AssetName = "GHI Future",
            BuySell = TradeType.BUY,
            Date = DateTime.Parse("04-Jun-22 11:00:00", CultureInfo.InvariantCulture),
            Description = "Bed and breakfast purchase of GHI Future",
            Quantity = 100,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new WrappedMoney(50m, "JPY"), FxRate = 0.008m }],
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(300000m, "JPY"), FxRate = 0.008m }
        };

        FutureContractTrade bedAndBreakfastBuy2 = new()
        {
            AssetName = "GHI Future",
            BuySell = TradeType.BUY,
            Date = DateTime.Parse("05-Jun-22 11:00:00", CultureInfo.InvariantCulture),
            Description = "Bed and breakfast purchase of GHI Future 2",
            Quantity = 100,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new WrappedMoney(50m, "JPY"), FxRate = 0.008m }],
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(300000m, "JPY"), FxRate = 0.008m }
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { buyTrade, disposeLongPosition, bedAndBreakfastBuy1, bedAndBreakfastBuy2 }, out _);
        result[1].MatchHistory.Count.ShouldBe(1);
        result[1].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.BED_AND_BREAKFAST);
    }

    [Fact]
    public void TestShortFutureContractBreadAndBreadFastOnlyMatchIfUnmatchedRemaining()
    {
        FutureContractTrade buyTrade = new()
        {
            AssetName = "GHI Future",
            BuySell = TradeType.SELL,
            Date = DateTime.Parse("02-Jun-22 10:00:00", CultureInfo.InvariantCulture),
            Description = "Purchase of GHI Future",
            Quantity = 5000,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new WrappedMoney(100m, "JPY"), FxRate = 0.007m }],
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(1000000m, "JPY"), FxRate = 0.007m }
        };

        FutureContractTrade disposeLongPosition = new()
        {
            AssetName = "GHI Future",
            BuySell = TradeType.BUY,
            Date = DateTime.Parse("03-Jun-22 15:00:00", CultureInfo.InvariantCulture),
            Description = "Sale of GHI Future",
            Quantity = 100,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new WrappedMoney(150m, "JPY"), FxRate = 0.0075m }],
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(1050000m, "JPY"), FxRate = 0.0075m }
        };

        FutureContractTrade bedAndBreakfastBuy1 = new()
        {
            AssetName = "GHI Future",
            BuySell = TradeType.SELL,
            Date = DateTime.Parse("04-Jun-22 11:00:00", CultureInfo.InvariantCulture),
            Description = "Bed and breakfast purchase of GHI Future",
            Quantity = 100,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new WrappedMoney(50m, "JPY"), FxRate = 0.008m }],
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(300000m, "JPY"), FxRate = 0.008m }
        };

        FutureContractTrade bedAndBreakfastBuy2 = new()
        {
            AssetName = "GHI Future",
            BuySell = TradeType.SELL,
            Date = DateTime.Parse("05-Jun-22 11:00:00", CultureInfo.InvariantCulture),
            Description = "Bed and breakfast purchase of GHI Future 2",
            Quantity = 100,
            Expenses = [new DescribedMoney() { Description = "Commission", Amount = new WrappedMoney(50m, "JPY"), FxRate = 0.008m }],
            GrossProceed = new() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            ContractValue = new() { Amount = new WrappedMoney(300000m, "JPY"), FxRate = 0.008m }
        };
        List<ITradeTaxCalculation> result = TradeCalculationHelper.CalculateTrades(new List<Trade>() { buyTrade, disposeLongPosition, bedAndBreakfastBuy1, bedAndBreakfastBuy2 }, out _);
        result[1].MatchHistory.Count.ShouldBe(1);
        result[1].MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.BED_AND_BREAKFAST);
    }
}
