using CapitalGainCalculator.Enum;
using CapitalGainCalculator.Model;
using CapitalGainCalculator.Parser.InteractiveBrokersXml;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Xunit;
using Xunit.Abstractions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CapitalGainCalculator.Test
{
    public class IBXmlParseTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly IBXmlDividendParser _xmlDividendParser = new IBXmlDividendParser();
        private readonly IBXmlTradeParser _xmlTradeParser = new IBXmlTradeParser();
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
            parsedData.Count().ShouldBe(93);
            IEnumerable<Dividend> witholdingTaxes = parsedData.Where(dataPoint => dataPoint.DividendType == DividendType.WITHHOLDING);
            witholdingTaxes.Count().ShouldBe(42);
            witholdingTaxes.Select(i => (i.Proceed.Amount * i.Proceed.FxRate).Amount).Sum().ToString().ShouldBe("-2648.12");
            IEnumerable<Dividend> dividends = parsedData.Where(dataPoint => dataPoint.DividendType == DividendType.DIVIDEND);
            dividends.Count().ShouldBe(46);
            dividends.Select(i => (i.Proceed.Amount * i.Proceed.FxRate).Amount).Sum().ToString().ShouldBe("17107.24");
            IEnumerable<Dividend> dividendsInLieu = parsedData.Where(dataPoint => dataPoint.DividendType == DividendType.DIVIDEND_IN_LIEU);
            dividendsInLieu.Count().ShouldBe(5);
            dividendsInLieu.Select(i => (i.Proceed.Amount * i.Proceed.FxRate).Amount).Sum().ToString().ShouldBe("97.84");
        }

        [Fact]
        public void TestReadingIBXmlTrades()
        {
            IList<Trade> parsedData = _xmlTradeParser.ParseXml(_xmlDoc);
            parsedData.Count(trade => trade.BuySell == TradeType.BUY).ShouldBe(41);
            parsedData.Count(trade => trade.BuySell == TradeType.SELL).ShouldBe(39);

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
            Should.Throw<NullReferenceException>(() => _xmlDividendParser.ParseXml(xmlDoc), @"The attribute ""type"" is not found in ""CashTransaction"", please include this attribute in your XML statement");
        }

        [Fact]
        public void TestReadingIBXmlCorporateActions()
        {
            IList<StockSplit> parsedData = _xmlStockSplitParser.ParseXml(_xmlDoc);
            parsedData.Count.ShouldBe(2);
            parsedData[0].AssetName.ShouldBe("4369.T");
            parsedData[0].Date.ShouldBe(DateTime.Parse("27/01/2021 20:25:00"));
            ((int)parsedData[0].NumberBeforeSplit).ShouldBe(1);
            ((int)parsedData[0].NumberAfterSplit).ShouldBe(4);
        }

        private void PrintXElements(IEnumerable<XElement> results)
        {
            foreach (XElement element in results)
            {
                _testOutputHelper.WriteLine(element.ToString());
            }
        }
    }
}
