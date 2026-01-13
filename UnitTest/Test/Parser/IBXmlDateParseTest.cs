using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Parser;
using InvestmentTaxCalculator.Parser.InteractiveBrokersXml;

using System.Xml.Linq;

namespace UnitTest.Test.Parser;

public class IBXmlDateParseTest
{
    [Fact]
    public void IBXmlFxParser_ParsesValidDateCorrectly()
    {
        string xmlContent = @"
              <FlexStatement>
                <StmtFunds>
				  <!-- Foreign currencies -->
				  <StatementOfFundsLine currency=""DKK"" fxRateToBase=""0.11857"" reportDate=""03-Feb-21"" activityDescription=""RTE(DK0010267129) 
                   Cash Dividend DKK 2.50 per Share (Ordinary Dividend)"" amount=""4000"" levelOfDetail=""Currency"" />
			    </StmtFunds>
                <ConversionRates>
				  <ConversionRate reportDate=""03-Feb-21"" fromCurrency=""DKK"" toCurrency=""GBP"" rate=""0.11857"" />
			    </ConversionRates>
             </FlexStatement>";
        XElement element = XElement.Parse(xmlContent);
        IBXmlFxParser parser = new();
        IList<Trade> result = parser.ParseXml(element);

        result.ShouldNotBeNull();
        result[0].Date.ShouldBe(new DateTime(2021, 2, 3, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void IBXmlCashSettlementParser_ThrowsParseExceptionForInvalidDate()
    {
        string xmlContent = @"
           <StmtFunds>
			 <StatementOfFundsLine symbol=""Test"" currency=""DKK"" fxRateToBase=""0.11857"" assetCategory=""OPT"" date=""Invalid Date"" 
               activityDescription=""Option Cash Settlement for: Exercise"" amount=""4000"" levelOfDetail=""Currency"" />
		   </StmtFunds>";
        XElement element = XElement.Parse(xmlContent);
        Should.Throw<ParseException>(() => IBXmlCashSettlementParser.ParseXml(element));
    }

    [Theory]
    [InlineData("01-Feb-2023 12:34:56")]
    [InlineData("01-Feb-202312:34:56")]
    [InlineData("01-Feb-2023 123456")]
    [InlineData("01-Feb-2023123456")]
    public void TradeMaker_ParsesDateWithDifferentFormat(string dateString)
    {
        string xmlContent = @$"
             <Trades>
                <Order currency=""USD"" fxRateToBase=""0.8"" assetCategory=""STK"" symbol=""ABC"" isin=""""
                description=""ABC Example Stock"" dateTime=""{dateString}"" quantity=""200"" proceeds=""-2000"" taxes=""-20"" ibCommission=""-1.5"" 
                ibCommissionCurrency=""USD"" notes=""O"" buySell=""BUY"" levelOfDetail=""ORDER"" />
             </Trades>";
        XElement element = XElement.Parse(xmlContent);
        IList<Trade> result = IBXmlStockTradeParser.ParseXml(element);
        result.ShouldNotBeNull();
        result[0].Date.ShouldBe(new DateTime(2023, 2, 1, 12, 34, 56, DateTimeKind.Utc));
    }
}
