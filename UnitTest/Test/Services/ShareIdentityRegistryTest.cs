using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Services;

using System.Globalization;

namespace UnitTest.Test.Services;

public class ShareIdentityRegistryTest
{
    [Theory]
    [InlineData("CWR", "CWRl", "CWR")]
    [InlineData("DIS", "DISm", "DIS")]
    public void TestSameIsinWithLowercaseSuffixSymbolsResolveToBaseSymbol(string baseSymbol, string suffixedSymbol, string expectedSymbol)
    {
        ShareIdentityRegistry registry = new();
        List<TaxEvent> taxEvents =
        [
            CreateTrade(baseSymbol, "GB00B010V573"),
            CreateTrade(suffixedSymbol, "GB00B010V573"),
        ];
        registry.RegisterEvents(taxEvents);
        taxEvents.ShouldAllBe(taxEvent => taxEvent.CanonicalAssetName == expectedSymbol);
        registry.IsSameShare(baseSymbol, suffixedSymbol).ShouldBeTrue();
    }

    [Fact]
    public void TestDividendsAndCorporateActionsShareIdentityWithTrades()
    {
        Trade trade = CreateTrade("DISm", "US2546871060");
        Dividend dividend = CreateDividend("DIS", "US2546871060");
        StockSplit stockSplitWithoutIsin = CreateStockSplit("DISm");
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents([trade, dividend, stockSplitWithoutIsin]);
        trade.CanonicalAssetName.ShouldBe("DIS");
        dividend.CanonicalAssetName.ShouldBe("DIS");
        stockSplitWithoutIsin.CanonicalAssetName.ShouldBe("DIS");
        trade.ShareIdentity.ShouldBeSameAs(dividend.ShareIdentity);
        trade.ShareIdentity.ShouldBeSameAs(stockSplitWithoutIsin.ShareIdentity);
        stockSplitWithoutIsin.IsSameAsset("DIS").ShouldBeTrue();
    }

    [Fact]
    public void TestSymbolPairSplitAcrossStatementsIsUnifiedWhenRegisteredTogether()
    {
        // "DIS" appears in one statement and "DISm" only in another: registering the combined
        // events of both statements still unifies them.
        List<TaxEvent> statement1 = [CreateTrade("DIS", "US2546871060")];
        List<TaxEvent> statement2 = [CreateTrade("DISm", "US2546871060"), CreateDividend("DISm", "US2546871060")];
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents(statement1.Concat(statement2));
        statement1.ShouldAllBe(taxEvent => taxEvent.CanonicalAssetName == "DIS");
        statement2.ShouldAllBe(taxEvent => taxEvent.CanonicalAssetName == "DIS");
    }

    [Fact]
    public void TestDifferentIsinSymbolsAreSeparateShares()
    {
        List<TaxEvent> taxEvents =
        [
            CreateTrade("CWR", "GB00B010V573"),
            CreateTrade("CWRl", "GB00B010V574"),
        ];
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents(taxEvents);
        taxEvents[0].CanonicalAssetName.ShouldBe("CWR");
        taxEvents[1].CanonicalAssetName.ShouldBe("CWRl");
        registry.IsSameShare("CWR", "CWRl").ShouldBeFalse();
    }

    [Fact]
    public void TestRenamedTickerSharingIsinIsSameShare()
    {
        // A ticker rename keeps the ISIN: both symbols resolve to the same share and the
        // most recently traded ticker is used as the display name.
        Trade oldTrade = CreateTrade("DWAC", "US25400Q1058", "01-May-21 10:00:00");
        Trade newTrade = CreateTrade("DJT", "US25400Q1058", "01-Jun-24 10:00:00");
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents([oldTrade, newTrade]);
        registry.IsSameShare("DWAC", "DJT").ShouldBeTrue();
        oldTrade.CanonicalAssetName.ShouldBe("DJT");
        newTrade.CanonicalAssetName.ShouldBe("DJT");
    }

    [Fact]
    public void TestNewIsinKeepingTickerIsSameShare()
    {
        // A Newco insertion that keeps the ticker but issues a new ISIN: both ISINs are recorded on one share.
        Trade oldTrade = CreateTrade("ABC", "GB00B010V573", "01-May-21 10:00:00");
        Trade newTrade = CreateTrade("ABC", "GB00B010V574", "01-May-22 10:00:00");
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents([oldTrade, newTrade]);
        oldTrade.ShareIdentity.ShouldBeSameAs(newTrade.ShareIdentity);
        oldTrade.ShareIdentity!.Isins.ShouldBe(["GB00B010V573", "GB00B010V574"]);
    }

    [Fact]
    public void TestEventsWithoutIsinAreNotUnified()
    {
        List<TaxEvent> taxEvents =
        [
            CreateTrade("CWR", ""),
            CreateTrade("CWRl", ""),
        ];
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents(taxEvents);
        taxEvents[0].CanonicalAssetName.ShouldBe("CWR");
        taxEvents[1].CanonicalAssetName.ShouldBe("CWRl");
        registry.IsSameShare("CWR", "CWRl").ShouldBeFalse();
    }

    [Fact]
    public void TestManualLinkJoinsSharesWithDifferentTickerAndIsin()
    {
        // Newco insertion: ticker and ISIN both changed, so the shares can only be joined by a manual link.
        Trade oldTrade = CreateTrade("OLDCO", "GB00B010V573", "01-May-21 10:00:00");
        Trade newTrade = CreateTrade("NEWCO", "GB00B010V574", "01-May-22 10:00:00");
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents([oldTrade, newTrade]);
        registry.IsSameShare("OLDCO", "NEWCO").ShouldBeFalse();

        registry.LinkShares("OLDCO", "NEWCO");
        registry.RegisterEvents([oldTrade, newTrade]);
        registry.IsSameShare("OLDCO", "NEWCO").ShouldBeTrue();
        oldTrade.ShareIdentity.ShouldBeSameAs(newTrade.ShareIdentity);
        oldTrade.ShareIdentity!.Isins.Count.ShouldBe(2);
        oldTrade.CanonicalAssetName.ShouldBe("NEWCO");
    }

    [Fact]
    public void TestManualLinkByIsinIsApplied()
    {
        Trade oldTrade = CreateTrade("OLDCO", "GB00B010V573");
        Trade newTrade = CreateTrade("NEWCO", "GB00B010V574");
        ShareIdentityRegistry registry = new();
        registry.LinkShares("GB00B010V573", "GB00B010V574");
        registry.RegisterEvents([oldTrade, newTrade]);
        registry.IsSameShare("OLDCO", "NEWCO").ShouldBeTrue();
    }

    [Fact]
    public void TestFullNameIsRecordedFromTradeDescription()
    {
        Trade trade = CreateTrade("DIS", "US2546871060");
        trade.Description = "WALT DISNEY CO";
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents([trade]);
        trade.ShareIdentity!.FullNames.ShouldBe(["WALT DISNEY CO"]);
    }

    [Fact]
    public void TestDuplicateSignatureMatchesAcrossTickerVariations()
    {
        Trade trade1 = CreateTrade("DIS", "US2546871060");
        Trade trade2 = CreateTrade("DISm", "US2546871060");
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents([trade1, trade2]);
        trade1.GetDuplicateSignature().ShouldBe(trade2.GetDuplicateSignature());
    }

    [Fact]
    public void TestUnknownTickerResolvesToItself()
    {
        ShareIdentityRegistry registry = new();
        registry.GetCanonicalTicker("UNKNOWN").ShouldBe("UNKNOWN");
        registry.IsSameShare("UNKNOWN", "UNKNOWN").ShouldBeTrue();
        registry.IsSameShare("UNKNOWN", "OTHER").ShouldBeFalse();
    }

    private static Trade CreateTrade(string assetName, string isin, string date = "01-May-21 10:00:00")
    {
        return new Trade
        {
            AssetName = assetName,
            Isin = isin,
            Date = DateTime.Parse(date, CultureInfo.InvariantCulture),
            Quantity = 10,
            GrossProceed = new DescribedMoney(100, "GBP", 1),
            AcquisitionDisposal = TradeType.ACQUISITION
        };
    }

    private static Dividend CreateDividend(string assetName, string isin)
    {
        return new Dividend
        {
            AssetName = assetName,
            Isin = isin,
            Date = DateTime.Parse("01-Jun-21 10:00:00", CultureInfo.InvariantCulture),
            DividendType = DividendType.DIVIDEND,
            Proceed = new DescribedMoney(100, "GBP", 1)
        };
    }

    private static StockSplit CreateStockSplit(string assetName)
    {
        return new StockSplit
        {
            AssetName = assetName,
            Date = DateTime.Parse("01-Jul-21 10:00:00", CultureInfo.InvariantCulture),
            SplitTo = 2,
            SplitFrom = 1
        };
    }
}
