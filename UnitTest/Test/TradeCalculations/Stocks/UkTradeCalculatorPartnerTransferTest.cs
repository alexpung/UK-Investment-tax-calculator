using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using NSubstitute;

namespace UnitTest.Test.TradeCalculations.Stocks;

public class UkTradeCalculatorPartnerTransferTest
{
    private readonly UkSection104Pools _section104Pools;
    private readonly TradeTaxCalculationFactory _tradeTaxCalculationFactory;

    public UkTradeCalculatorPartnerTransferTest()
    {
        _section104Pools = new UkSection104Pools(new UKTaxYear(), new ResidencyStatusRecord());
        _tradeTaxCalculationFactory = new TradeTaxCalculationFactory(new ResidencyStatusRecord());
    }

    private static ITradeAndCorporateActionList CreateMockTradeList(List<Trade> trades, List<CorporateAction> actions)
    {
        var list = Substitute.For<ITradeAndCorporateActionList>();
        list.Trades.Returns(trades);
        list.CorporateActions.Returns(actions);
        return list;
    }

    [Fact]
    public void GiftToPartner_BeforeDisposal_ShouldReduceAllowableCost()
    {
        var buy = new Trade
        {
            AssetName = "ABC",
            Date = new DateTime(2024, 1, 1),
            AcquisitionDisposal = TradeType.ACQUISITION,
            Quantity = 100m,
            GrossProceed = new DescribedMoney(1000m, "GBP", 1m)
        };
        var gift = new PartnerTransferCorporateAction
        {
            AssetName = "ABC",
            Date = new DateTime(2024, 1, 2),
            Direction = PartnerTransferDirection.GiftToPartner,
            Quantity = 20m
        };
        var sell = new Trade
        {
            AssetName = "ABC",
            Date = new DateTime(2024, 1, 3),
            AcquisitionDisposal = TradeType.DISPOSAL,
            Quantity = 40m,
            GrossProceed = new DescribedMoney(600m, "GBP", 1m)
        };

        var tradeList = CreateMockTradeList([buy, sell], [gift]);
        var calculator = new UkTradeCalculator(_section104Pools, tradeList, _tradeTaxCalculationFactory);

        List<ITradeTaxCalculation> results = calculator.CalculateTax();
        ITradeTaxCalculation disposal = results.First(x => x.AcquisitionDisposal == TradeType.DISPOSAL && x is not CorporateActionTaxCalculation);

        disposal.TotalAllowableCost.Amount.ShouldBe(400m, 0.01m);
        disposal.Gain.Amount.ShouldBe(200m, 0.01m);

        UkSection104 pool = _section104Pools.GetExistingOrInitialise("ABC");
        pool.Quantity.ShouldBe(40m);
        pool.AcquisitionCostInBaseCurrency.Amount.ShouldBe(400m, 0.01m);
    }

    [Fact]
    public void ReceiveFromPartner_BeforeDisposal_ShouldIncreaseAllowableCost()
    {
        var buy = new Trade
        {
            AssetName = "ABC",
            Date = new DateTime(2024, 1, 1),
            AcquisitionDisposal = TradeType.ACQUISITION,
            Quantity = 100m,
            GrossProceed = new DescribedMoney(1000m, "GBP", 1m)
        };
        var receive = new PartnerTransferCorporateAction
        {
            AssetName = "ABC",
            Date = new DateTime(2024, 1, 2),
            Direction = PartnerTransferDirection.ReceiveFromPartner,
            Quantity = 50m,
            TransferredCost = new DescribedMoney(600m, "GBP", 1m, "Transferred from spouse")
        };
        var sell = new Trade
        {
            AssetName = "ABC",
            Date = new DateTime(2024, 1, 3),
            AcquisitionDisposal = TradeType.DISPOSAL,
            Quantity = 75m,
            GrossProceed = new DescribedMoney(1200m, "GBP", 1m)
        };

        var tradeList = CreateMockTradeList([buy, sell], [receive]);
        var calculator = new UkTradeCalculator(_section104Pools, tradeList, _tradeTaxCalculationFactory);

        List<ITradeTaxCalculation> results = calculator.CalculateTax();
        ITradeTaxCalculation disposal = results.First(x => x.AcquisitionDisposal == TradeType.DISPOSAL && x is not CorporateActionTaxCalculation);

        disposal.TotalAllowableCost.Amount.ShouldBe(800m, 0.01m);
        disposal.Gain.Amount.ShouldBe(400m, 0.01m);

        UkSection104 pool = _section104Pools.GetExistingOrInitialise("ABC");
        pool.Quantity.ShouldBe(75m);
        pool.AcquisitionCostInBaseCurrency.Amount.ShouldBe(800m, 0.01m);
    }
}
