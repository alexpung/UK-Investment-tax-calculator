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
            PrintXElements(dividends);
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
