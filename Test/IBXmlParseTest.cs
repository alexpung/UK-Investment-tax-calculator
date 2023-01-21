using CapitalGainCalculator.Enum;
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

namespace CapitalGainCalculator.Test
{
    public class IBXmlParseTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        public IBXmlParseTest(ITestOutputHelper output)
        {
            _testOutputHelper = output;
        }
        [Fact]
        public void TestReadingIBXmlDividends()
        {
            XElement xmlDoc = XElement.Load(@".\Test\Resource\TaxExample.xml");
            IBXmlParser iBXmlParser = new IBXmlParser();
            IEnumerable<XElement> parsedData = iBXmlParser.ParseDividend(xmlDoc);
            parsedData.Count().ShouldBe(93);
            IEnumerable<XElement> witholdingTaxes = parsedData.Where(row => row.Attribute("type")?.Value == "Withholding Tax");
            IEnumerable<XElement> dividends = parsedData.Where(row => row.Attribute("type")?.Value == "Dividends");
            IEnumerable<XElement> dividendsInLieu = parsedData.Where(row => row.Attribute("type")?.Value == "Payment In Lieu Of Dividends");
            witholdingTaxes.Sum(row => decimal.Parse(row.Attribute("amount")?.Value ?? "0")).ToString().ShouldBe("-363394");
            dividends.Sum(row => decimal.Parse(row.Attribute("amount")?.Value ?? "0")).ToString().ShouldBe("2367244.68");
            dividendsInLieu.Sum(row => decimal.Parse(row.Attribute("amount")?.Value ?? "0")).ToString().ShouldBe("933.84");
        }

        [Fact]
        public void TestMissingDividendTypeThrowException()
        {
            XElement xmlDoc = XElement.Parse(@"<CashTransactions><CashTransaction settleDate=""23-Dec-21"" symbol=""6050.T"" isin=""JP3130230000"" description=""6050.T(JP3130230000) CASH DIVIDEND JPY 14 PER SHARE - JP TAX"" 
            amount=""-3430"" listingExchange=""TSEJ"" currency=""JPY"" fxRateToBase=""0.0065181"" accountId=""TestAccount"" acctAlias=""TestAccount"" model="""" 
            assetCategory=""STK"" conid=""81540299"" securityID=""JP3130230000"" securityIDType=""ISIN"" cusip="""" underlyingConid="""" underlyingSymbol="""" underlyingSecurityID="""" 
            underlyingListingExchange="""" issuer="""" multiplier=""1"" strike="""" expiry="""" putCall="""" principalAdjustFactor="""" dateTime=""23-Dec-21 20:20:00"" tradeID="""" 
            code="""" transactionID="""" reportDate=""23-Dec-21"" clientReference="""" levelOfDetail=""DETAIL"" serialNumber="""" deliveryType="""" commodityType="""" fineness=""0.0"" weight=""0.0 ()"" /></CashTransactions>");
            IBXmlParser iBXmlParser = new IBXmlParser();
            IEnumerable<XElement> parsedData = iBXmlParser.ParseDividend(xmlDoc);
            Should.Throw<NullReferenceException>(() => parsedData.ToList(), @"The attribute ""type"" is not found in ""CashTransaction"", please include this attribute in your XML statement");
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
