using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;
using InvestmentTaxCalculator.Services;

using NSubstitute;

namespace UnitTest.Test.Reproduction;

/// <summary>
/// Issue #257 follow-up: a rename observed only as an exchange suffixed ticker ("PHNX" renamed and imported as
/// "SDLFl"). Linking with the base symbol the user knows ("SDLF") used to be silently ignored, leaving two
/// Section 104 pools and two End of Tax Year Section 104 Status rows; and once linked, the merged share was
/// displayed as "SDLFl" instead of "SDLF".
/// </summary>
public class Issue257RenamedTickerRepro
{
    [Theory]
    [InlineData("PHNX", "SDLF")]
    [InlineData("PHNX", "SDLFl")]
    [InlineData("GB00PHNX0001", "GB00SDLF0001")]
    public void TestLinkedRenameConsolidatesEndOfYearSection104UnderBaseSymbol(string linkReference, string linkedReference)
    {
        Trade buyOld = CreateBuy("PHNX", "GB00PHNX0001", new DateTime(2020, 6, 1), 100m, 1000m);
        Trade buyNew = CreateBuy("SDLFl", "GB00SDLF0001", new DateTime(2025, 7, 1), 50m, 500m);

        ShareIdentityRegistry registry = new();
        registry.RegisterEvents([buyOld, buyNew]);
        registry.LinkShares(linkReference, linkedReference);
        registry.RegisterEvents([buyOld, buyNew]);

        registry.Identities.Count.ShouldBe(1);
        buyOld.CanonicalAssetName.ShouldBe("SDLF");
        buyNew.CanonicalAssetName.ShouldBe("SDLF");

        ITradeAndCorporateActionList tradeList = Substitute.For<ITradeAndCorporateActionList>();
        tradeList.Trades.Returns([buyOld, buyNew]);
        tradeList.CorporateActions.Returns([]);
        UkSection104Pools section104Pools = new(new UKTaxYear(), new ResidencyStatusRecord(), registry);
        UkTradeCalculator calculator = new(section104Pools, tradeList,
            new TradeTaxCalculationFactory(new ResidencyStatusRecord()), registry);
        calculator.CalculateTax();

        Dictionary<string, Section104History> endOfYear = section104Pools.GetEndOfYearSection104s(2025);
        endOfYear.Keys.ShouldBe(["SDLF"]);
        endOfYear["SDLF"].NewQuantity.ShouldBe(150m);
        endOfYear["SDLF"].NewValue.ShouldBe(new WrappedMoney(1500m, "GBP"));
    }

    [Fact]
    public void TestLinkByBaseSymbolIsNotAppliedWhenAmbiguous()
    {
        // Two unrelated shares both observed with suffixed variations of "ABC": a link typed as "ABC" cannot
        // tell which one is meant, so it must not merge either of them.
        Trade buyTarget = CreateBuy("XYZ", "GB00XYZ00001", new DateTime(2020, 6, 1), 100m, 1000m);
        Trade buySuffixed = CreateBuy("ABCl", "GB00ABC00001", new DateTime(2021, 6, 1), 100m, 1000m);
        Trade buyOtherSuffixed = CreateBuy("ABCm", "GB00ABC00002", new DateTime(2022, 6, 1), 100m, 1000m);

        ShareIdentityRegistry registry = new();
        registry.RegisterEvents([buyTarget, buySuffixed, buyOtherSuffixed]);
        registry.LinkShares("XYZ", "ABC");
        registry.RegisterEvents([buyTarget, buySuffixed, buyOtherSuffixed]);

        registry.Identities.Count.ShouldBe(3);
        registry.IsSameShare("XYZ", "ABCl").ShouldBeFalse();
        registry.IsSameShare("XYZ", "ABCm").ShouldBeFalse();
    }

    private static Trade CreateBuy(string assetName, string isin, DateTime date, decimal quantity, decimal proceed)
    {
        return new Trade
        {
            AssetName = assetName,
            Isin = isin,
            Date = date,
            AcquisitionDisposal = TradeType.ACQUISITION,
            Quantity = quantity,
            GrossProceed = new DescribedMoney(proceed, "GBP", 1m)
        };
    }
}
