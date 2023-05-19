using Enum;
using Model;
using Model.Interfaces;
using Model.UkTaxModel;
using Moq;
using Shouldly;
using UnitTest;

namespace CapitalGainCalculator.Test;

public class UkSection104Test
{
    [Theory]
    [InlineData(100, 1000, 50, 400)]
    [InlineData(100, 1000, 100, 5000)]
    [InlineData(100, 1000, 150, 4000)]
    public void TestAddandRemoveSection104(decimal buyQuantity, decimal buyValue, decimal sellQuantity, decimal sellValue)
    {
        Mock<ITradeTaxCalculation> mockBuyTrade = MockTrade.CreateMockTrade(buyQuantity, buyValue, TradeType.BUY);
        UkSection104 ukSection104 = new("IBM");
        ukSection104.MatchTradeWithSection104(mockBuyTrade.Object);
        ukSection104.AssetName.ShouldBe("IBM");
        ukSection104.Quantity.ShouldBe(100m);
        ukSection104.ValueInBaseCurrency.ShouldBe(buyValue);
        mockBuyTrade.Object.MatchHistory[0].MatchQuantity.ShouldBe(buyQuantity);
        mockBuyTrade.Object.MatchHistory[0].BaseCurrencyMatchAcquitionValue.ShouldBe(buyValue);
        mockBuyTrade.Object.MatchHistory[0].BaseCurrencyMatchDisposalValue.ShouldBe(0);
        mockBuyTrade.Object.MatchHistory[0].TradeMatchType.ShouldBe(UkMatchType.SECTION_104);
        Mock<ITradeTaxCalculation> mockSellTrade = MockTrade.CreateMockTrade(sellQuantity, sellValue, TradeType.SELL);
        ukSection104.MatchTradeWithSection104(mockSellTrade.Object);
        ukSection104.Quantity.ShouldBe(decimal.Max(buyQuantity - sellQuantity, 0));
        ukSection104.ValueInBaseCurrency.ShouldBe(decimal.Max((buyQuantity - sellQuantity) / buyQuantity * buyValue, 0));
        mockSellTrade.Object.MatchHistory[0].MatchQuantity.ShouldBe(decimal.Min(sellQuantity, buyQuantity));
        mockSellTrade.Object.MatchHistory[0].BaseCurrencyMatchAcquitionValue.ShouldBe(decimal.Min(buyValue / buyQuantity * sellQuantity, buyValue));
        mockSellTrade.Object.MatchHistory[0].BaseCurrencyMatchDisposalValue.ShouldBe(decimal.Min(sellQuantity, buyQuantity) * sellValue / sellQuantity);
        mockSellTrade.Object.MatchHistory[0].TradeMatchType.ShouldBe(UkMatchType.SECTION_104);
    }

    [Fact]
    public void TestSection104History()
    {
        Mock<ITradeTaxCalculation> mockTrade1 = MockTrade.CreateMockTrade(100, 1000, TradeType.BUY);
        Mock<ITradeTaxCalculation> mockTrade2 = MockTrade.CreateMockTrade(200, 2000, TradeType.BUY);
        Mock<ITradeTaxCalculation> mockTrade3 = MockTrade.CreateMockTrade(300, 3000, TradeType.BUY);
        Mock<ITradeTaxCalculation> mockTrade4 = MockTrade.CreateMockTrade(400, 8000, TradeType.SELL);
        UkSection104 ukSection104 = new("IBM");
        ukSection104.MatchTradeWithSection104(mockTrade1.Object);
        ukSection104.MatchTradeWithSection104(mockTrade2.Object);
        ukSection104.MatchTradeWithSection104(mockTrade3.Object);
        ukSection104.MatchTradeWithSection104(mockTrade4.Object);
        ukSection104.Section104HistoryList[0].OldQuantity.ShouldBe(0);
        ukSection104.Section104HistoryList[0].OldValue.ShouldBe(0);
        ukSection104.Section104HistoryList[0].QuantityChange.ShouldBe(100);
        ukSection104.Section104HistoryList[0].ValueChange.ShouldBe(1000);
        ukSection104.Section104HistoryList[1].OldQuantity.ShouldBe(100);
        ukSection104.Section104HistoryList[1].OldValue.ShouldBe(1000);
        ukSection104.Section104HistoryList[1].QuantityChange.ShouldBe(200);
        ukSection104.Section104HistoryList[1].ValueChange.ShouldBe(2000);
        ukSection104.Section104HistoryList[2].OldQuantity.ShouldBe(300);
        ukSection104.Section104HistoryList[2].OldValue.ShouldBe(3000);
        ukSection104.Section104HistoryList[2].QuantityChange.ShouldBe(300);
        ukSection104.Section104HistoryList[2].ValueChange.ShouldBe(3000);
        ukSection104.Section104HistoryList[3].OldQuantity.ShouldBe(600);
        ukSection104.Section104HistoryList[3].OldValue.ShouldBe(6000);
        ukSection104.Section104HistoryList[3].QuantityChange.ShouldBe(-400);
        ukSection104.Section104HistoryList[3].ValueChange.ShouldBe(-4000);
    }

    [Fact]
    public void TestSection104HandleShareSplit()
    {
        Mock<ITradeTaxCalculation> mockTrade1 = MockTrade.CreateMockTrade(100, 1000, TradeType.BUY);
        Mock<ITradeTaxCalculation> mockTrade2 = MockTrade.CreateMockTrade(120, 1500, TradeType.SELL);
        CorporateAction corporateAction = new StockSplit() { AssetName = "ABC", Date = new DateTime(), NumberAfterSplit = 3, NumberBeforeSplit = 2 };
        UkSection104 ukSection104 = new("IBM");
        ukSection104.MatchTradeWithSection104(mockTrade1.Object);
        ukSection104.PerformCorporateAction(corporateAction);
        ukSection104.MatchTradeWithSection104(mockTrade2.Object);
        ukSection104.Quantity.ShouldBe(30); // bought 100, 150 after split - 120 sold = 30
        ukSection104.ValueInBaseCurrency.ShouldBe(200); // bought shares worth 1000, remaining shares worth = 30*1000/150 = 200
    }
}
