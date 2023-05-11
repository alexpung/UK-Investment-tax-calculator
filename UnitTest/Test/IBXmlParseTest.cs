using CapitalGainCalculator.Enum;
using CapitalGainCalculator.Model;
using CapitalGainCalculator.Parser.InteractiveBrokersXml;
using Shouldly;
using System.Xml.Linq;
using Xunit.Abstractions;

namespace CapitalGainCalculator.Test;

public class IBXmlParseTest
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IBXmlDividendParser _xmlDividendParser = new IBXmlDividendParser();
    private readonly IBXmlStockTradeParser _xmlTradeParser = new IBXmlStockTradeParser();
    private readonly IBXmlStockSplitParser _xmlStockSplitParser = new IBXmlStockSplitParser();
    private readonly XElement _xmlDoc = XElement.Load(@".\Test\Resource\TaxExample.xml");
    public IBXmlParseTest(ITestOutputHelper output)
    {
        _testOutputHelper = output;
    }

    [Fact]
    public void TestReadingIBXmlDividends()
    {
        IList<Dividend> parsedData = _xmlDividendParser.ParseXml(_xmlDoc);
        parsedData.Count().ShouldBe(47);
        IEnumerable<Dividend> witholdingTaxes = parsedData.Where(dataPoint => dataPoint.DividendType == DividendType.WITHHOLDING);
        witholdingTaxes.Count().ShouldBe(21);
        witholdingTaxes.Select(i => i.Proceed.Amount.Amount * i.Proceed.FxRate).Sum().ToString().ShouldBe("-1324.5492950");
        IEnumerable<Dividend> dividends = parsedData.Where(dataPoint => dataPoint.DividendType == DividendType.DIVIDEND);
        dividends.Count().ShouldBe(23);
        dividends.Select(i => i.Proceed.Amount.Amount * i.Proceed.FxRate).Sum().ToString().ShouldBe("8555.2289521");
        IEnumerable<Dividend> dividendsInLieu = parsedData.Where(dataPoint => dataPoint.DividendType == DividendType.DIVIDEND_IN_LIEU);
        dividendsInLieu.Count().ShouldBe(3);
        dividendsInLieu.Select(i => i.Proceed.Amount.Amount * i.Proceed.FxRate).Sum().ToString().ShouldBe("96.7693740");
    }

    [Fact]
    public void TestReadingIBXmlTrades()
    {
        IList<Trade> parsedData = _xmlTradeParser.ParseXml(_xmlDoc);
        parsedData.Count(trade => trade.BuySell == TradeType.BUY).ShouldBe(27);
        parsedData.Count(trade => trade.BuySell == TradeType.SELL).ShouldBe(31);

    }

    [Fact]
    public void TestMissingDividendTypeThrowException()
    {
        XElement xmlDoc = XElement.Parse(@"<CashTransactions><CashTransaction settleDate=""23-Dec-21"" symbol=""6050.T"" isin=""JP3130230000"" description=""6050.T(JP3130230000) CASH DIVIDEND JPY 14 PER SHARE - JP TAX"" 
            amount=""-3430"" listingExchange=""TSEJ"" currency=""JPY"" fxRateToBase=""0.0065181"" accountId=""TestAccount"" acctAlias=""TestAccount"" model="""" 
            assetCategory=""STK"" conid=""81540299"" securityID=""JP3130230000"" securityIDType=""ISIN"" cusip="""" underlyingConid="""" underlyingSymbol="""" underlyingSecurityID="""" 
            underlyingListingExchange="""" issuer="""" multiplier=""1"" strike="""" expiry="""" putCall="""" principalAdjustFactor="""" dateTime=""23-Dec-21 20:20:00"" tradeID="""" 
            code="""" transactionID="""" reportDate=""23-Dec-21"" clientReference="""" levelOfDetail=""DETAIL"" serialNumber="""" deliveryType="""" commodityType="""" fineness=""0.0"" weight=""0.0 ()"" /></CashTransactions>");
        IBXmlDividendParser iBXmlParser = new IBXmlDividendParser();
        Should.Throw<ArgumentException>(() => _xmlDividendParser.ParseXml(xmlDoc), @"The attribute ""type"" is not found in ""CashTransaction"", please include this attribute in your XML statement");
    }

    [Fact]
    public void TestReadingIBXmlCorporateActions()
    {
        IList<StockSplit> parsedData = _xmlStockSplitParser.ParseXml(_xmlDoc);
        parsedData.Count.ShouldBe(2);
        parsedData[0].AssetName.ShouldBe("4369.T");
        parsedData[0].Date.ShouldBe(DateTime.Parse("27/01/2021 20:25:00"));
        parsedData[0].NumberBeforeSplit.ShouldBe(1);
        parsedData[0].NumberAfterSplit.ShouldBe(4);
    }
}
