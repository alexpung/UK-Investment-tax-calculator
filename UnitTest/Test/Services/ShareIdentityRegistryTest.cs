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
    public void TestNewIsinKeepingTickerRequiresManualLink()
    {
        // Same ticker, new ISIN: this pattern is produced both by a legitimate re-issue (same company, new ISIN)
        // and by an unrelated company later being assigned a ticker recycled from a delisted one. The two cannot
        // be told apart from the data alone, so they are NOT auto-joined; a manual link is required, same as any
        // other Newco-style insertion.
        Trade oldTrade = CreateTrade("ABC", "GB00B010V573", "01-May-21 10:00:00");
        Trade newTrade = CreateTrade("ABC", "GB00B010V574", "01-May-22 10:00:00");
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents([oldTrade, newTrade]);
        oldTrade.ShareIdentity.ShouldNotBeSameAs(newTrade.ShareIdentity);
        registry.IsSameShare("GB00B010V573", "GB00B010V574").ShouldBeFalse();

        registry.LinkShares("GB00B010V573", "GB00B010V574");
        registry.RegisterEvents([oldTrade, newTrade]);
        oldTrade.ShareIdentity.ShouldBeSameAs(newTrade.ShareIdentity);
        oldTrade.ShareIdentity!.Isins.ShouldBe(["GB00B010V573", "GB00B010V574"]);
    }

    [Fact]
    public void TestTickerRecycledByUnrelatedCompanyIsNotMerged()
    {
        // A ticker later reassigned by the exchange to a completely unrelated company (different ISIN) must not
        // be silently folded into the original company's share identity/Section 104 pool.
        Trade delistedCompanyTrade = CreateTrade("ABC", "GB00AAAAAAAA", "01-May-10 10:00:00");
        Trade unrelatedCompanyTrade = CreateTrade("ABC", "US00BBBBBBBB", "01-May-24 10:00:00");
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents([delistedCompanyTrade, unrelatedCompanyTrade]);
        delistedCompanyTrade.ShareIdentity.ShouldNotBeSameAs(unrelatedCompanyTrade.ShareIdentity);
        delistedCompanyTrade.ShareIdentity!.Isins.ShouldBe(["GB00AAAAAAAA"]);
        unrelatedCompanyTrade.ShareIdentity!.Isins.ShouldBe(["US00BBBBBBBB"]);
    }

    [Fact]
    public void TestRecycledTickerGetsDistinctCanonicalNames()
    {
        // Both identities have the primary ticker "ABC", so a canonical name built from the primary ticker alone
        // would group the two unrelated shares together and pool the delisted company's acquisition cost into the
        // new company's Section 104 holding. The canonical name must separate them.
        Trade delistedCompanyTrade = CreateTrade("ABC", "GB00AAAAAAAA", "01-May-10 10:00:00");
        Trade unrelatedCompanyTrade = CreateTrade("ABC", "US00BBBBBBBB", "01-May-24 10:00:00");
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents([delistedCompanyTrade, unrelatedCompanyTrade]);

        delistedCompanyTrade.CanonicalAssetName.ShouldBe("ABC (GB00AAAAAAAA)");
        unrelatedCompanyTrade.CanonicalAssetName.ShouldBe("ABC (US00BBBBBBBB)");
        delistedCompanyTrade.CanonicalAssetName.ShouldNotBe(unrelatedCompanyTrade.CanonicalAssetName);
        delistedCompanyTrade.GetDuplicateSignature().ShouldNotBe(unrelatedCompanyTrade.GetDuplicateSignature());

        // The disambiguated name must resolve back to its own identity, so pool lookups by reported name work.
        registry.ResolveByTicker("ABC (GB00AAAAAAAA)").ShouldBeSameAs(delistedCompanyTrade.ShareIdentity);
        registry.ResolveByTicker("ABC (US00BBBBBBBB)").ShouldBeSameAs(unrelatedCompanyTrade.ShareIdentity);
        registry.GetCanonicalTicker("ABC (US00BBBBBBBB)").ShouldBe("ABC (US00BBBBBBBB)");
        registry.IsSameShare("ABC (GB00AAAAAAAA)", "ABC (US00BBBBBBBB)").ShouldBeFalse();
    }

    [Fact]
    public void TestUnambiguousTickerKeepsPlainCanonicalName()
    {
        // The disambiguator is only added on an actual clash: ordinary shares keep their bare ticker.
        Trade trade = CreateTrade("ABC", "GB00AAAAAAAA");
        Trade otherTrade = CreateTrade("XYZ", "US00BBBBBBBB");
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents([trade, otherTrade]);
        trade.CanonicalAssetName.ShouldBe("ABC");
        otherTrade.CanonicalAssetName.ShouldBe("XYZ");
    }

    [Fact]
    public void TestManualLinkRemovesTheDisambiguator()
    {
        // Same ticker with a new ISIN starts out as two identities and so is disambiguated. Once the user declares
        // it a re-issue of the same share the identities merge and the plain ticker becomes unambiguous again.
        Trade oldTrade = CreateTrade("ABC", "GB00B010V573", "01-May-21 10:00:00");
        Trade newTrade = CreateTrade("ABC", "GB00B010V574", "01-May-22 10:00:00");
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents([oldTrade, newTrade]);
        oldTrade.CanonicalAssetName.ShouldBe("ABC (GB00B010V573)");
        newTrade.CanonicalAssetName.ShouldBe("ABC (GB00B010V574)");

        registry.LinkShares("GB00B010V573", "GB00B010V574");
        registry.RegisterEvents([oldTrade, newTrade]);
        oldTrade.CanonicalAssetName.ShouldBe("ABC");
        newTrade.CanonicalAssetName.ShouldBe("ABC");
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
    public void TestLastSeenDatesIdentifyOlderAndNewerIsin()
    {
        // Newco insertion keeping the ticker, confirmed via a manual link: the last seen dates show which ISIN
        // is the older one.
        Trade oldTrade = CreateTrade("ABC", "GB00B010V573", "01-May-21 10:00:00");
        Trade newTrade = CreateTrade("ABC", "GB00B010V574", "01-May-22 10:00:00");
        ShareIdentityRegistry registry = new();
        registry.LinkShares("GB00B010V573", "GB00B010V574");
        registry.RegisterEvents([oldTrade, newTrade]);
        ShareIdentity identity = oldTrade.ShareIdentity!;
        identity.GetIsinLastSeen("GB00B010V573").ShouldBe(oldTrade.Date);
        identity.GetIsinLastSeen("GB00B010V574").ShouldBe(newTrade.Date);
        identity.GetIsinLastSeen("GB00B010V573")!.Value.ShouldBeLessThan(identity.GetIsinLastSeen("GB00B010V574")!.Value);
    }

    [Fact]
    public void TestLastSeenDatesIdentifyOlderAndNewerTicker()
    {
        Trade oldTrade = CreateTrade("DWAC", "US25400Q1058", "01-May-21 10:00:00");
        Trade newTrade = CreateTrade("DJT", "US25400Q1058", "01-Jun-24 10:00:00");
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents([oldTrade, newTrade]);
        ShareIdentity identity = oldTrade.ShareIdentity!;
        identity.GetTickerLastSeen("DWAC").ShouldBe(oldTrade.Date);
        identity.GetTickerLastSeen("DJT").ShouldBe(newTrade.Date);
        identity.GetTickerLastSeen("DWAC")!.Value.ShouldBeLessThan(identity.GetTickerLastSeen("DJT")!.Value);
    }

    [Fact]
    public void TestObservationsListTickerIsinCombosOrderedOldestFirst()
    {
        Trade oldTrade = CreateTrade("DWAC", "US25400Q1058", "01-May-21 10:00:00");
        Trade laterOldTrade = CreateTrade("DWAC", "US25400Q1058", "01-Jun-21 10:00:00");
        Trade newTrade = CreateTrade("DJT", "US25400Q9999", "01-Jun-24 10:00:00");
        ShareIdentityRegistry registry = new();
        registry.LinkShares("DWAC", "DJT");
        registry.RegisterEvents([oldTrade, laterOldTrade, newTrade]);
        IReadOnlyList<ShareIdentityObservation> observations = oldTrade.ShareIdentity!.Observations;
        observations.ShouldBe([
            new ShareIdentityObservation("DWAC", "US25400Q1058", laterOldTrade.Date),
            new ShareIdentityObservation("DJT", "US25400Q9999", newTrade.Date)]);
    }

    [Fact]
    public void TestObservationLastSeenSurvivesIdentityMerge()
    {
        // The ticker only stock split is seen before the ISIN carrying trades that cause the identities to merge.
        StockSplit stockSplit = CreateStockSplit("DISm");
        Trade trade = CreateTrade("DISm", "US2546871060", "01-May-21 10:00:00");
        Trade laterTrade = CreateTrade("DIS", "US2546871060", "01-May-22 10:00:00");
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents([stockSplit, trade, laterTrade]);
        ShareIdentity identity = trade.ShareIdentity!;
        identity.GetTickerLastSeen("DISm").ShouldBe(stockSplit.Date); // split is dated 01-Jul-21, after the trade
        identity.GetTickerLastSeen("DIS").ShouldBe(laterTrade.Date);
        identity.Observations.Select(observation => (observation.Ticker, observation.Isin)).ShouldBe([
            ("DISm", ""),
            ("DISm", "US2546871060"),
            ("DIS", "US2546871060")], ignoreOrder: true);
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
