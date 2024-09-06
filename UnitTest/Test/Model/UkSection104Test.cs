using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using UnitTest.Helper;

namespace UnitTest.Test.Model;

public class UkSection104Test
{
    [Theory]
    [InlineData(100, 1000, 50, 400)]
    [InlineData(100, 1000, 100, 5000)]
    [InlineData(100, 1000, 150, 4000)]
    public void TestAddandRemoveSection104(decimal buyQuantity, decimal buyValue, decimal sellQuantity, decimal sellValue)
    {
        TradeTaxCalculation buyTradeTaxCalculation = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Local), buyQuantity, buyValue, TradeType.ACQUISITION);
        UkSection104 ukSection104 = new("IBM");
        buyTradeTaxCalculation.MatchWithSection104(ukSection104);
        ukSection104.AssetName.ShouldBe("IBM");
        ukSection104.Quantity.ShouldBe(100m);
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(buyValue));
        buyTradeTaxCalculation.MatchHistory[0].MatchAcquisitionQty.ShouldBe(buyQuantity);
        buyTradeTaxCalculation.MatchHistory[0].BaseCurrencyMatchDisposalProceed.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        buyTradeTaxCalculation.MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SECTION_104);
        TradeTaxCalculation sellTradeTaxCalculation = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Local), sellQuantity, sellValue, TradeType.DISPOSAL);
        sellTradeTaxCalculation.MatchWithSection104(ukSection104);
        ukSection104.Quantity.ShouldBe(decimal.Max(buyQuantity - sellQuantity, 0));
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(decimal.Max((buyQuantity - sellQuantity) / buyQuantity * buyValue, 0)));
        sellTradeTaxCalculation.MatchHistory[0].MatchAcquisitionQty.ShouldBe(decimal.Min(sellQuantity, buyQuantity));
        sellTradeTaxCalculation.MatchHistory[0].BaseCurrencyMatchAllowableCost.ShouldBe(new WrappedMoney(decimal.Min(buyValue / buyQuantity * sellQuantity, buyValue)));
        sellTradeTaxCalculation.MatchHistory[0].BaseCurrencyMatchDisposalProceed.ShouldBe(new WrappedMoney(decimal.Min(sellQuantity, buyQuantity) * sellValue / sellQuantity));
        sellTradeTaxCalculation.MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SECTION_104);
    }

    [Fact]
    public void TestSection104History()
    {
        TradeTaxCalculation tradeTaxCalculation1 = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Local), 100, 1000m, TradeType.ACQUISITION);
        TradeTaxCalculation tradeTaxCalculation2 = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 3, 2, 0, 0, 0, DateTimeKind.Local), 200, 2000m, TradeType.ACQUISITION);
        TradeTaxCalculation tradeTaxCalculation3 = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 5, 3, 0, 0, 0, DateTimeKind.Local), 300, 3000m, TradeType.ACQUISITION);
        TradeTaxCalculation tradeTaxCalculation4 = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 7, 4, 0, 0, 0, DateTimeKind.Local), 400, 8000m, TradeType.DISPOSAL);
        UkSection104 ukSection104 = new("IBM");
        tradeTaxCalculation1.MatchWithSection104(ukSection104);
        tradeTaxCalculation2.MatchWithSection104(ukSection104);
        tradeTaxCalculation3.MatchWithSection104(ukSection104);
        tradeTaxCalculation4.MatchWithSection104(ukSection104);
        ukSection104.Section104HistoryList[0].OldQuantity.ShouldBe(0);
        ukSection104.Section104HistoryList[0].OldValue.ShouldBe(new WrappedMoney(0m));
        ukSection104.Section104HistoryList[0].QuantityChange.ShouldBe(100);
        ukSection104.Section104HistoryList[0].ValueChange.ShouldBe(new WrappedMoney(1000m));
        ukSection104.Section104HistoryList[1].OldQuantity.ShouldBe(100);
        ukSection104.Section104HistoryList[1].OldValue.ShouldBe(new WrappedMoney(1000m));
        ukSection104.Section104HistoryList[1].QuantityChange.ShouldBe(200);
        ukSection104.Section104HistoryList[1].ValueChange.ShouldBe(new WrappedMoney(2000m));
        ukSection104.Section104HistoryList[2].OldQuantity.ShouldBe(300);
        ukSection104.Section104HistoryList[2].OldValue.ShouldBe(new WrappedMoney(3000m));
        ukSection104.Section104HistoryList[2].QuantityChange.ShouldBe(300);
        ukSection104.Section104HistoryList[2].ValueChange.ShouldBe(new WrappedMoney(3000m));
        ukSection104.Section104HistoryList[3].OldQuantity.ShouldBe(600);
        ukSection104.Section104HistoryList[3].OldValue.ShouldBe(new WrappedMoney(6000m));
        ukSection104.Section104HistoryList[3].QuantityChange.ShouldBe(-400);
        ukSection104.Section104HistoryList[3].ValueChange.ShouldBe(new WrappedMoney(-4000m));
    }

    [Fact]
    public void TestSection104HandleShareSplit()
    {
        TradeTaxCalculation tradeTaxCalculation1 = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Local), 100, 1000m, TradeType.ACQUISITION);
        TradeTaxCalculation tradeTaxCalculation2 = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 3, 2, 0, 0, 0, DateTimeKind.Local), 120, 1500m, TradeType.DISPOSAL);
        StockSplit corporateAction = new() { AssetName = "IBM", Date = new DateTime(2020, 3, 1, 0, 0, 0, DateTimeKind.Local), SplitTo = 3, SplitFrom = 2 };
        // Also test wrong AssetName don't change S104
        StockSplit corporateAction2 = new() { AssetName = "ABC", Date = new DateTime(2020, 3, 1, 0, 0, 0, DateTimeKind.Local), SplitTo = 3, SplitFrom = 1 };
        UkSection104 ukSection104 = new("IBM");
        tradeTaxCalculation1.MatchWithSection104(ukSection104);
        corporateAction.ChangeSection104(ukSection104);
        corporateAction2.ChangeSection104(ukSection104);
        tradeTaxCalculation2.MatchWithSection104(ukSection104);
        ukSection104.Quantity.ShouldBe(30); // bought 100, 150 after split - 120 sold = 30
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(200m)); // bought shares worth 1000, remaining shares worth = 30*1000/150 = 200
    }
}
