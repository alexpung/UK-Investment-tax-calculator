using CapitalGainCalculator.Enum;
using CapitalGainCalculator.Model;
using CapitalGainCalculator.Model.Interfaces;
using CapitalGainCalculator.Model.UkTaxModel;
using Moq;
using Shouldly;

namespace CapitalGainCalculator.Test;

public class UkSection104Test
{
    [Theory]
    [InlineData(100, 1000, 50, 400)]
    [InlineData(100, 1000, 100, 5000)]
    [InlineData(100, 1000, 150, 4000)]
    public void TestAddandRemoveSection104(decimal buyQuantity, decimal buyValue, decimal sellQuantity, decimal sellValue)
    {
        Mock<ITradeTaxCalculation> mockBuyTrade = new();
        mockBuyTrade.Setup(f => f.MatchAll()).Returns((buyQuantity, buyValue));
        mockBuyTrade.Setup(f => f.BuySell).Returns(TradeType.BUY);
        mockBuyTrade.Setup(f => f.MatchHistory).Returns(new List<TradeMatch>());
        mockBuyTrade.Setup(f => f.UnmatchedQty).Returns(buyQuantity);
        UkSection104 ukSection104 = new("IBM");
        ukSection104.MatchTradeWithSection104(mockBuyTrade.Object);
        ukSection104.AssetName.ShouldBe("IBM");
        ukSection104.Quantity.ShouldBe(100m);
        ukSection104.ValueInBaseCurrency.ShouldBe(buyValue);
        mockBuyTrade.Object.MatchHistory[0].MatchQuantity.ShouldBe(buyQuantity);
        mockBuyTrade.Object.MatchHistory[0].BaseCurrencyMatchValue.ShouldBe(buyValue);
        mockBuyTrade.Object.MatchHistory[0].TradeMatchType.ShouldBe(UkMatchType.SECTION_104);

        Mock<ITradeTaxCalculation> mockSellTrade = new();
        mockSellTrade.Setup(f => f.MatchAll()).Returns((sellQuantity, sellValue));
        mockSellTrade.Setup(f => f.BuySell).Returns(TradeType.SELL);
        mockSellTrade.Setup(f => f.MatchHistory).Returns(new List<TradeMatch>());
        mockSellTrade.Setup(f => f.UnmatchedQty).Returns(sellQuantity);
        mockSellTrade.Setup(f => f.MatchQty(It.IsAny<decimal>())).Returns<decimal>(x => (x, 100));
        ukSection104.MatchTradeWithSection104(mockSellTrade.Object);
        ukSection104.Quantity.ShouldBe(decimal.Max(buyQuantity - sellQuantity, 0));
        ukSection104.ValueInBaseCurrency.ShouldBe(decimal.Max((buyQuantity - sellQuantity) / buyQuantity * buyValue, 0));
        mockSellTrade.Object.MatchHistory[0].MatchQuantity.ShouldBe(decimal.Min(sellQuantity, buyQuantity));
        mockSellTrade.Object.MatchHistory[0].BaseCurrencyMatchValue.ShouldBe(decimal.Min(buyValue / buyQuantity * sellQuantity, buyValue));
        mockSellTrade.Object.MatchHistory[0].TradeMatchType.ShouldBe(UkMatchType.SECTION_104);
    }
}
