using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using UnitTest.Helper;

namespace UnitTest.Test.Model;

public class PartnerTransferCorporateActionTest
{
    [Fact]
    public void GiftToPartner_ShouldRemoveQuantityAndProportionalCostFromSection104()
    {
        TradeTaxCalculation buyTrade = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 1, 1), 100, 1000m, TradeType.ACQUISITION);
        UkSection104 section104 = new("IBM");
        buyTrade.MatchWithSection104(section104);

        PartnerTransferCorporateAction gift = new()
        {
            AssetName = "IBM",
            Date = new DateTime(2020, 1, 2),
            Direction = PartnerTransferDirection.GiftToPartner,
            Quantity = 25m
        };

        gift.ChangeSection104(section104);

        section104.Quantity.ShouldBe(75m);
        section104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(750m));
        section104.Section104HistoryList.Count.ShouldBe(2);
        section104.Section104HistoryList[1].Explanation.ShouldContain("Gift to partner");
    }

    [Fact]
    public void ReceiveFromPartner_ShouldAddQuantityAndTransferredCostToSection104()
    {
        TradeTaxCalculation buyTrade = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 1, 1), 100, 1000m, TradeType.ACQUISITION);
        UkSection104 section104 = new("IBM");
        buyTrade.MatchWithSection104(section104);

        PartnerTransferCorporateAction receive = new()
        {
            AssetName = "IBM",
            Date = new DateTime(2020, 1, 2),
            Direction = PartnerTransferDirection.ReceiveFromPartner,
            Quantity = 10m,
            TransferredCost = new DescribedMoney(120m, "GBP", 1m, "Transferred from spouse")
        };

        receive.ChangeSection104(section104);

        section104.Quantity.ShouldBe(110m);
        section104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(1120m));
        section104.Section104HistoryList.Count.ShouldBe(2);
        section104.Section104HistoryList[1].Explanation.ShouldContain("Received from partner");
    }

    [Fact]
    public void GiftToPartner_MoreThanHolding_ShouldThrow()
    {
        TradeTaxCalculation buyTrade = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 1, 1), 10, 100m, TradeType.ACQUISITION);
        UkSection104 section104 = new("IBM");
        buyTrade.MatchWithSection104(section104);

        PartnerTransferCorporateAction gift = new()
        {
            AssetName = "IBM",
            Date = new DateTime(2020, 1, 2),
            Direction = PartnerTransferDirection.GiftToPartner,
            Quantity = 20m
        };

        var ex = Should.Throw<InvalidOperationException>(() => gift.ChangeSection104(section104));
        ex.Message.ShouldContain("Cannot gift");
    }

    [Fact]
    public void ReceiveFromPartner_WithoutTransferredCost_ShouldThrow()
    {
        UkSection104 section104 = new("IBM");

        PartnerTransferCorporateAction receive = new()
        {
            AssetName = "IBM",
            Date = new DateTime(2020, 1, 2),
            Direction = PartnerTransferDirection.ReceiveFromPartner,
            Quantity = 10m,
            TransferredCost = null
        };

        var ex = Should.Throw<InvalidOperationException>(() => receive.ChangeSection104(section104));
        ex.Message.ShouldContain("Transferred cost must be provided");
    }
}
