﻿using Enumerations;

using Model;
using Model.TaxEvents;

using Parser;
using Parser.InteractiveBrokersXml;

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
        parsedData.Count.ShouldBe(47);
        IEnumerable<Dividend> witholdingTaxes = parsedData.Where(dataPoint => dataPoint.DividendType == DividendType.WITHHOLDING);
        witholdingTaxes.Count().ShouldBe(21);
        witholdingTaxes.Select(i => i.Proceed.Amount.Amount * i.Proceed.FxRate).Sum().ShouldBe(-1324.5492950m);
        IEnumerable<Dividend> dividends = parsedData.Where(dataPoint => dataPoint.DividendType == DividendType.DIVIDEND);
        dividends.Count().ShouldBe(23);
        dividends.Select(i => i.Proceed.Amount.Amount * i.Proceed.FxRate).Sum().ShouldBe(8555.2289521m);
        IEnumerable<Dividend> dividendsInLieu = parsedData.Where(dataPoint => dataPoint.DividendType == DividendType.DIVIDEND_IN_LIEU);
        dividendsInLieu.Count().ShouldBe(3);
        dividendsInLieu.Select(i => i.Proceed.Amount.Amount * i.Proceed.FxRate).Sum().ShouldBe(96.7693740m);
    }

    [Fact]
    public void TestReadingIBXmlTrades()
    {
        IList<Trade> parsedData = IBXmlStockTradeParser.ParseXml(_xmlDoc);
        parsedData.Count(trade => trade.AcquisitionDisposal == TradeType.ACQUISITION).ShouldBe(27);
        parsedData.Count(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL).ShouldBe(31);

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
        parsedData.Count.ShouldBe(2);
        parsedData[0].AssetName.ShouldBe("4369.T");
        parsedData[0].Date.ShouldBe(DateTime.Parse("27-Jan-21 20:25:00", CultureInfo.InvariantCulture));
        parsedData[0].NumberBeforeSplit.ShouldBe(1);
        parsedData[0].NumberAfterSplit.ShouldBe(4);
    }
}
