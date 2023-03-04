using CapitalGainCalculator.Model;
using CapitalGainCalculator.Model.UkTaxModel;
using NodaMoney;
using Shouldly;

namespace CapitalGainCalculator.Test
{
    public class UKTradeGrouperTest
    {
        [Fact]
        public void TestTradeGrouping()
        {
            Trade Trade1 = new()
            {
                AssetName = "abc",
                BuySell = Enum.TradeType.BUY,
                Date = new DateTime(2022, 4, 5),
                Quantity = 10,
                Proceed = new DescribedMoney { Amount = new Money(1000, "HKD"), FxRate = 0.11m }
            };
            Trade Trade2 = new()
            {
                AssetName = "HSBC bank",
                BuySell = Enum.TradeType.BUY,
                Date = new DateTime(2022, 4, 5),
                Quantity = 10,
                Proceed = new DescribedMoney { Amount = new Money(500, "HKD"), FxRate = 0.11m }
            };
            Trade Trade3 = new Trade
            {
                AssetName = "abc",
                BuySell = Enum.TradeType.SELL,
                Date = new DateTime(2022, 4, 5),
                Quantity = 10,
                Proceed = new DescribedMoney { Amount = new Money(2000, "GBP"), FxRate = 1m }
            };
            Trade Trade4 = new()
            {
                AssetName = "abc",
                BuySell = Enum.TradeType.BUY,
                Date = new DateTime(2022, 4, 5),
                Quantity = 10,
                Proceed = new DescribedMoney { Amount = new Money(100, "GBP"), FxRate = 1m }
            };
            Trade Trade5 = new()
            {
                AssetName = "abc",
                BuySell = Enum.TradeType.BUY,
                Date = new DateTime(2022, 4, 6),
                Quantity = 10,
                Proceed = new DescribedMoney { Amount = new Money(20000, "JPY"), FxRate = 0.0063m }
            };
            IList<TaxEvent> data = new List<TaxEvent> { Trade1, Trade2, Trade3, Trade4, Trade5 };
            var result = UkTradeGrouper.GroupTrade(data).ToList();
            result.Count.ShouldBe(4);
            result.ShouldContain(trade => trade.UnmatchedNetAmount == 210m && trade.UnmatchedQty == 20m);
        }
    }
}
