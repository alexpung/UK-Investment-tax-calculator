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
    private readonly XElement _xmlDoc = XElement.Load(@".\Test\Resource\TaxExample.xml");

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
}
