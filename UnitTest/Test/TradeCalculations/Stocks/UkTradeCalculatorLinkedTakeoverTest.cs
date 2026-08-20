using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;
using InvestmentTaxCalculator.Services;

using NSubstitute;

namespace UnitTest.Test.TradeCalculations.Stocks;

/// <summary>
/// A takeover names both the old and the acquiring company. When those two are also declared the same share by a
/// manual link, both tickers canonicalise to one name, and the action must not end up depending on itself.
/// </summary>
public class UkTradeCalculatorLinkedTakeoverTest
{
    [Fact]
    public void TestTakeoverBetweenManuallyLinkedTickersDoesNotReportCircularDependency()
    {
        Trade buyOld = new()
        {
            AssetName = "OLDCO",
            Isin = "GB00OLDCO001",
            Date = new DateTime(2023, 1, 1),
            AcquisitionDisposal = TradeType.ACQUISITION,
            Quantity = 100m,
            GrossProceed = new DescribedMoney(1000m, "GBP", 1m)
        };
        Trade buyNew = new()
        {
            AssetName = "NEWCO",
            Isin = "GB00NEWCO002",
            Date = new DateTime(2023, 7, 1),
            AcquisitionDisposal = TradeType.ACQUISITION,
            Quantity = 50m,
            GrossProceed = new DescribedMoney(500m, "GBP", 1m)
        };
        TakeoverCorporateAction takeover = new()
        {
            AssetName = "OLDCO",
            Date = new DateTime(2023, 6, 1),
            AcquiringCompanyTicker = "NEWCO",
            OldToNewRatio = 1.0m,
            CashComponent = null,
            ElectTaxDeferral = false,
            NewSharesMarketValue = null
        };

        // The Newco insertion changed both ticker and ISIN, so the user links them by hand: OLDCO and NEWCO now
        // canonicalise to the same name and the takeover lists that name twice.
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents([buyOld, buyNew, takeover]);
        registry.LinkShares("OLDCO", "NEWCO");
        registry.RegisterEvents([buyOld, buyNew, takeover]);
        buyOld.CanonicalAssetName.ShouldBe(buyNew.CanonicalAssetName);

        ITradeAndCorporateActionList tradeList = Substitute.For<ITradeAndCorporateActionList>();
        tradeList.Trades.Returns([buyOld, buyNew]);
        tradeList.CorporateActions.Returns([takeover]);
        UkSection104Pools section104Pools = new(Substitute.For<ITaxYear>(), new ResidencyStatusRecord(), registry);
        UkTradeCalculator calculator = new(section104Pools, tradeList,
            new TradeTaxCalculationFactory(new ResidencyStatusRecord()), registry);

        Should.NotThrow(() => calculator.CalculateTax());
    }
}
