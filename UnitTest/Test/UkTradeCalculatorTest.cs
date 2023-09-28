using Model;
using Model.Interfaces;
using Model.UkTaxModel;

namespace UnitTest.Test;
public class UkTradeCalculatorTest
{
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
            GrossProceed = new() { Description = "Commission", Amount = new(500m, "USD"), FxRate = 0.86m },
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
}
