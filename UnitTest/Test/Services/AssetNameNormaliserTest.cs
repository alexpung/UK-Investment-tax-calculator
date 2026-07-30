using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Services;

using System.Globalization;

namespace UnitTest.Test.Services;

public class AssetNameNormaliserTest
{
    [Theory]
    [InlineData("CWR", "CWRl", "CWR")]
    [InlineData("DIS", "DISm", "DIS")]
    public void TestSameIsinWithLowercaseSuffixSymbolIsRenamedToBaseSymbol(string baseSymbol, string suffixedSymbol, string expectedSymbol)
    {
        List<TaxEvent> taxEvents =
        [
            CreateTrade(baseSymbol, "GB00B010V573"),
            CreateTrade(suffixedSymbol, "GB00B010V573"),
        ];
        AssetNameNormaliser.NormaliseAssetNamesByIsin(taxEvents);
        taxEvents.ShouldAllBe(taxEvent => taxEvent.AssetName == expectedSymbol);
    }

    [Fact]
    public void TestDividendsAndCorporateActionsAreRenamedWithTrades()
    {
        Trade trade = CreateTrade("DISm", "US2546871060");
        Dividend dividend = CreateDividend("DIS", "US2546871060");
        StockSplit stockSplitWithoutIsin = CreateStockSplit("DISm");
        List<TaxEvent> taxEvents = [trade, dividend, stockSplitWithoutIsin];
        AssetNameNormaliser.NormaliseAssetNamesByIsin(taxEvents);
        trade.AssetName.ShouldBe("DIS");
        dividend.AssetName.ShouldBe("DIS");
        stockSplitWithoutIsin.AssetName.ShouldBe("DIS");
    }

    [Fact]
    public void TestSymbolPairSplitAcrossStatementsIsRenamedWhenNormalisedTogether()
    {
        // "DIS" appears in one statement and "DISm" only in another: normalising the combined
        // events of both statements still unifies them.
        List<TaxEvent> statement1 = [CreateTrade("DIS", "US2546871060")];
        List<TaxEvent> statement2 = [CreateTrade("DISm", "US2546871060"), CreateDividend("DISm", "US2546871060")];
        AssetNameNormaliser.NormaliseAssetNamesByIsin(statement1.Concat(statement2));
        statement1.ShouldAllBe(taxEvent => taxEvent.AssetName == "DIS");
        statement2.ShouldAllBe(taxEvent => taxEvent.AssetName == "DIS");
    }

    [Fact]
    public void TestDifferentIsinSymbolsAreNotRenamed()
    {
        List<TaxEvent> taxEvents =
        [
            CreateTrade("CWR", "GB00B010V573"),
            CreateTrade("CWRl", "GB00B010V574"),
        ];
        AssetNameNormaliser.NormaliseAssetNamesByIsin(taxEvents);
        taxEvents[0].AssetName.ShouldBe("CWR");
        taxEvents[1].AssetName.ShouldBe("CWRl");
    }

    [Fact]
    public void TestSameIsinButUnrelatedSymbolsAreNotRenamed()
    {
        List<TaxEvent> taxEvents =
        [
            CreateTrade("ABC", "GB00B010V573"),
            CreateTrade("XYZ", "GB00B010V573"),
        ];
        AssetNameNormaliser.NormaliseAssetNamesByIsin(taxEvents);
        taxEvents[0].AssetName.ShouldBe("ABC");
        taxEvents[1].AssetName.ShouldBe("XYZ");
    }

    [Fact]
    public void TestEventsWithoutIsinAloneAreNotRenamed()
    {
        List<TaxEvent> taxEvents =
        [
            CreateTrade("CWR", ""),
            CreateTrade("CWRl", ""),
        ];
        AssetNameNormaliser.NormaliseAssetNamesByIsin(taxEvents);
        taxEvents[0].AssetName.ShouldBe("CWR");
        taxEvents[1].AssetName.ShouldBe("CWRl");
    }

    private static Trade CreateTrade(string assetName, string isin)
    {
        return new Trade
        {
            AssetName = assetName,
            Isin = isin,
            Date = DateTime.Parse("01-May-21 10:00:00", CultureInfo.InvariantCulture),
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
