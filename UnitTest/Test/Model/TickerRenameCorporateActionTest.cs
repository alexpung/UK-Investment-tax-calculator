using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using UnitTest.Helper;

namespace UnitTest.Test.Model;

public class TickerRenameCorporateActionTest
{
    [Fact]
    public void TickerRename_ShouldMoveEntireSection104PoolFromOldTickerToNewTicker()
    {
        TradeTaxCalculation buyTrade = MockTrade.CreateTradeTaxCalculation("FB", new DateTime(2021, 1, 1), 100, 1500m, TradeType.ACQUISITION);

        UkSection104 oldTickerSection104 = new("FB");
        UkSection104 newTickerSection104 = new("META");
        buyTrade.MatchWithSection104(oldTickerSection104);

        TickerRenameCorporateAction action = new()
        {
            AssetName = "FB",
            Date = new DateTime(2022, 6, 9),
            NewTicker = "META"
        };

        action.ChangeSection104(oldTickerSection104);
        action.ChangeSection104(newTickerSection104);

        oldTickerSection104.Quantity.ShouldBe(0m);
        oldTickerSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(0m));

        newTickerSection104.Quantity.ShouldBe(100m);
        newTickerSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(1500m));
        newTickerSection104.Section104HistoryList[^1].Explanation.ShouldContain("Ticker rename from FB");
    }

    [Fact]
    public void TickerRename_WhenNoOldTickerHolding_ShouldNotCreateNewPoolEntry()
    {
        UkSection104 oldTickerSection104 = new("AOL");
        UkSection104 newTickerSection104 = new("TWX");

        TickerRenameCorporateAction action = new()
        {
            AssetName = "AOL",
            Date = new DateTime(2003, 10, 16),
            NewTicker = "TWX"
        };

        action.ChangeSection104(oldTickerSection104);
        action.ChangeSection104(newTickerSection104);

        oldTickerSection104.Quantity.ShouldBe(0m);
        newTickerSection104.Quantity.ShouldBe(0m);
        newTickerSection104.Section104HistoryList.Count.ShouldBe(0);
    }
}
