using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Parser;
using InvestmentTaxCalculator.Parser.InteractiveBrokersXml;

using System.Globalization;
using System.Xml.Linq;
namespace UnitTest.Test.Parser;

public class IBXmlParseTest
{
    private readonly XElement _xmlDoc = XElement.Load(Path.Combine(AppContext.BaseDirectory, "Test", "resource", "TaxExample.xml"));

    [Fact]
    public void TestReadingIBXmlDividends()
    {
        IList<Dividend> parsedData = IBXmlDividendParser.ParseXml(_xmlDoc);
        parsedData.Count.ShouldBe(3);
        IEnumerable<Dividend> witholdingTaxes = parsedData.Where(dataPoint => dataPoint.DividendType == DividendType.WITHHOLDING);
        witholdingTaxes.Count().ShouldBe(1);
        witholdingTaxes.Select(i => i.Proceed.Amount.Amount * i.Proceed.FxRate).Sum().ShouldBe(-166.5m);
        IEnumerable<Dividend> dividends = parsedData.Where(dataPoint => dataPoint.DividendType == DividendType.DIVIDEND);
        dividends.Count().ShouldBe(1);
        dividends.Select(i => i.Proceed.Amount.Amount * i.Proceed.FxRate).Sum().ShouldBe(499.5m);
        dividends.First().Isin.ShouldBe("JP12345");
        IEnumerable<Dividend> dividendsInLieu = parsedData.Where(dataPoint => dataPoint.DividendType == DividendType.DIVIDEND_IN_LIEU);
        dividendsInLieu.Count().ShouldBe(1);
        dividendsInLieu.Select(i => i.Proceed.Amount.Amount * i.Proceed.FxRate).Sum().ShouldBe(55.5m);
    }

    [Fact]
    public void TestReadingIBXmlTrades()
    {
        IList<Trade> parsedData = IBXmlStockTradeParser.ParseXml(_xmlDoc);
        parsedData.Count(trade => trade.AcquisitionDisposal == TradeType.ACQUISITION).ShouldBe(6);
        parsedData.Count(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL).ShouldBe(3);
        parsedData.First(t => t.AssetName == "ABC").Isin.ShouldBe("US9999999999");
        parsedData.First(t => t.AssetName == "DEF").Isin.ShouldBe("GB0000000000");
        parsedData.ShouldAllBe(trade => trade.AssetType == AssetCategoryType.STOCK);
    }

    [Fact]
    public void TestReadingIBXmlFundTrades()
    {
        IList<Trade> parsedData = IBXmlStockTradeParser.ParseFundXml(_xmlDoc);
        parsedData.Count.ShouldBe(2);
        parsedData.ShouldAllBe(trade => trade.AssetType == AssetCategoryType.FUND);
        parsedData.Count(trade => trade.AcquisitionDisposal == TradeType.ACQUISITION).ShouldBe(1);
        parsedData.Count(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL).ShouldBe(1);
        parsedData.ShouldAllBe(trade => trade.AssetName == "FUNDA");
        parsedData.First().Isin.ShouldBe("IE0000000001");
    }

    [Theory]
    [InlineData("CWR", "CWRl", "CWR")]
    [InlineData("DIS", "DISm", "DIS")]
    public void TestSameIsinWithLowercaseSuffixSymbolIsRenamedToBaseSymbol(string baseSymbol, string suffixedSymbol, string expectedSymbol)
    {
        List<Trade> trades =
        [
            CreateTrade(baseSymbol, "GB00B010V573"),
            CreateTrade(suffixedSymbol, "GB00B010V573"),
        ];
        IBXmlStockTradeParser.NormaliseAssetNamesByIsin(trades);
        trades.ShouldAllBe(trade => trade.AssetName == expectedSymbol);
    }

    [Fact]
    public void TestDifferentIsinSymbolsAreNotRenamed()
    {
        List<Trade> trades =
        [
            CreateTrade("CWR", "GB00B010V573"),
            CreateTrade("CWRl", "GB00B010V574"),
        ];
        IBXmlStockTradeParser.NormaliseAssetNamesByIsin(trades);
        trades[0].AssetName.ShouldBe("CWR");
        trades[1].AssetName.ShouldBe("CWRl");
    }

    [Fact]
    public void TestSameIsinButUnrelatedSymbolsAreNotRenamed()
    {
        List<Trade> trades =
        [
            CreateTrade("ABC", "GB00B010V573"),
            CreateTrade("XYZ", "GB00B010V573"),
        ];
        IBXmlStockTradeParser.NormaliseAssetNamesByIsin(trades);
        trades[0].AssetName.ShouldBe("ABC");
        trades[1].AssetName.ShouldBe("XYZ");
    }

    [Fact]
    public void TestTradesWithoutIsinAreNotRenamed()
    {
        List<Trade> trades =
        [
            CreateTrade("CWR", ""),
            CreateTrade("CWRl", ""),
        ];
        IBXmlStockTradeParser.NormaliseAssetNamesByIsin(trades);
        trades[0].AssetName.ShouldBe("CWR");
        trades[1].AssetName.ShouldBe("CWRl");
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

    [Fact]
    public void TestMissingDividendTypeThrowException()
    {
        XElement xmlDoc = XElement.Parse(@"<CashTransactions><CashTransaction settleDate=""23-Dec-21"" symbol=""6050.T"" isin=""JP3130230000"" description=""6050.T(JP3130230000) CASH DIVIDEND JPY 14 PER SHARE - JP TAX"" 
            amount=""-3430"" listingExchange=""TSEJ"" currency=""JPY"" fxRateToBase=""0.0065181"" accountId=""TestAccount"" acctAlias=""TestAccount"" model="""" 
            assetCategory=""STK"" conid=""81540299"" securityID=""JP3130230000"" securityIDType=""ISIN"" cusip="""" underlyingConid="""" underlyingSymbol="""" underlyingSecurityID="""" 
            underlyingListingExchange="""" issuer="""" multiplier=""1"" strike="""" expiry="""" putCall="""" principalAdjustFactor="""" dateTime=""23-Dec-21 20:20:00"" tradeID="""" 
            code="""" transactionID="""" reportDate=""23-Dec-21"" clientReference="""" levelOfDetail=""DETAIL"" serialNumber="""" deliveryType="""" commodityType="""" fineness=""0.0"" weight=""0.0 ()"" /></CashTransactions>");
        Should.Throw<ParseException>(() => IBXmlDividendParser.ParseXml(xmlDoc), @"The attribute ""type"" is not found in ""CashTransaction"", please include this attribute in your XML statement");
    }

    [Fact]
    public void TestReadingIBXmlCorporateActions()
    {
        IList<StockSplit> parsedData = IBXmlStockSplitParser.ParseXml(_xmlDoc);
        parsedData.Count.ShouldBe(1);
        parsedData[0].AssetName.ShouldBe("ABC");
        parsedData[0].Date.ShouldBe(DateTime.Parse("03-May-21 20:25:00", CultureInfo.InvariantCulture));
        parsedData[0].SplitFrom.ShouldBe(1);
        parsedData[0].SplitTo.ShouldBe(2);
    }

    [Fact]
    public void TestUnknownCountryCodeInDividend()
    {
        XElement xmlDoc = XElement.Parse(@"<CashTransactions><CashTransaction settleDate=""02-Feb-21"" symbol=""ABCD"" isin=""AA12345"" description=""ABC CASH DIVIDEND - JP TAX"" amount=""-30000"" type=""Withholding Tax"" currency=""JPY"" fxRateToBase=""0.00555"" levelOfDetail=""DETAIL""/></CashTransactions>");
        var result = IBXmlDividendParser.ParseXml(xmlDoc);
        result[0].CompanyLocation.ShouldBe(CountryCode.UnknownRegion);
    }
    [Fact]
    public void TestReadingIBXmlReturnOfCapital()
    {
        XElement xmlDoc = XElement.Parse(@"<CashTransactions>
            <CashTransaction settleDate=""20-Oct-21"" symbol=""VGS"" isin=""AU000000VGS7"" 
                description=""VGS(AU000000VGS7) CASH DIVIDEND USD 0.123 - Return of Capital"" 
                amount=""123.45"" type=""Dividends"" currency=""USD"" fxRateToBase=""0.75"" levelOfDetail=""DETAIL""/>
        </CashTransactions>");

        IList<Dividend> dividends = IBXmlDividendParser.ParseXml(xmlDoc);
        IList<CorporateAction> corporateActions = IBXmlDividendParser.ParseReturnOfCapital(xmlDoc);

        dividends.Count.ShouldBe(1);
        dividends[0].DividendType.ShouldBe(DividendType.RETURN_OF_CAPITAL);
        dividends[0].DividendReceived.Amount.ShouldBe(123.45m * 0.75m);
        dividends[0].Proceed.BaseCurrencyAmount.Amount.ShouldBe(123.45m * 0.75m);

        corporateActions.Count.ShouldBe(1);
        var roc = corporateActions[0].ShouldBeOfType<ReturnOfCapitalCorporateAction>();
        roc.AssetName.ShouldBe("VGS");
        roc.Amount.Amount.Amount.ShouldBe(123.45m);
        roc.Amount.FxRate.ShouldBe(0.75m);
    }
}
