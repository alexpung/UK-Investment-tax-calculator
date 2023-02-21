using CapitalGainCalculator.Model;
using CapitalGainCalculator.Parser.InteractiveBrokersXml;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CapitalGainCalculator.Test
{
    public class IBXmlParseControllerTest
    {
        private readonly IBParseController _parseController = new IBParseController();

        [Fact]
        public void TestCheckingInvalidIBXml() 
        {
            string testFilePath = @".\Test\Resource\InvalidFile.xml";
            _parseController.CheckFileValidity(testFilePath).ShouldBeFalse();
        }

        [Fact]
        public void TestCheckingValidIBXml()
        {
            string testFilePath = @".\Test\Resource\TaxExample.xml";
            _parseController.CheckFileValidity(testFilePath).ShouldBeTrue();
        }

        [Fact]
        public void TestParseValidIBXml()
        {
            string testFilePath = @".\Test\Resource\TaxExample.xml";
            IList<TaxEvent> results = _parseController.ParseFile(testFilePath);
            results.Count(i => i is Dividend).ShouldBe(93);
            results.Count(i => i is StockSplit).ShouldBe(2);
            results.Count(i => i is Trade).ShouldBe(80);
        }
    }
}
