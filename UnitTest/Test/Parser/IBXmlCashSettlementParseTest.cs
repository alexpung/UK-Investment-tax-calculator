using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Parser;
using InvestmentTaxCalculator.Parser.InteractiveBrokersXml;

using System.Xml.Linq;

namespace UnitTest.Test.Parser;

public class IBXmlCashSettlementParserTests
{
    [Fact]
    public void ParseXml_ValidOptionCashSettlement_ReturnsParsedCashSettlements()
    {
        // Arrange
        var xml = @"
            <root>
                <StatementOfFundsLine assetCategory='OPT' symbol='SPX 180316C01000000' 
                    activityDescription='Option Cash Settlement for: Assignment' 
                    reportDate='16-Mar-18' amount='-700732' currency='USD' />
                <StatementOfFundsLine assetCategory='STK' symbol='AAPL' 
                    activityDescription='Option Cash Settlement for: Exercise' 
                    reportDate='16-Mar-18' amount='5000' currency='USD' />
                <StatementOfFundsLine assetCategory='OPT' symbol='SPX 180316P01000000' 
                    activityDescription='Option Cash Settlement for: Exercise' 
                    reportDate='16-Mar-18' amount='-12345' currency='USD' />
            </root>";

        var document = XElement.Parse(xml);

        // Act
        var result = IBXmlCashSettlementParser.ParseXml(document);

        // Assert
        result.Count.ShouldBe(2); // Only the "OPT" category elements should be parsed

        var firstSettlement = result[0];
        firstSettlement.AssetName.ShouldBe("SPX 180316C01000000");
        firstSettlement.Description.ShouldBe("Option Cash Settlement for: Assignment");
        firstSettlement.Date.ShouldBe(new DateTime(2018, 3, 16));
        firstSettlement.Amount.ShouldBe(new WrappedMoney(-700732, "USD"));
        firstSettlement.TradeReason.ShouldBe(TradeReason.OptionAssigned);

        var secondSettlement = result[1];
        secondSettlement.AssetName.ShouldBe("SPX 180316P01000000");
        secondSettlement.Description.ShouldBe("Option Cash Settlement for: Exercise");
        secondSettlement.Date.ShouldBe(new DateTime(2018, 3, 16));
        secondSettlement.Amount.ShouldBe(new WrappedMoney(-12345, "USD"));
        secondSettlement.TradeReason.ShouldBe(TradeReason.OwnerExerciseOption);
    }

    [Fact]
    public void ParseXml_InvalidActivityDescription_ThrowsParseException()
    {
        var xml = @"
            <root>
                <StatementOfFundsLine assetCategory='OPT' symbol='SPX 180316C01000000' 
                    activityDescription='Option Cash Settlement for: UnknownActivity' 
                    reportDate='16-Mar-18' amount='-700732' currency='USD' />
            </root>";
        var document = XElement.Parse(xml);
        var ex = Should.Throw<ParseException>(() => IBXmlCashSettlementParser.ParseXml(document));
        ex.Message.ShouldContain("Unknown cash settlement reason");
    }

    [Fact]
    public void ParseXml_NonOptionSettlement_IgnoresNonOptionTrades()
    {
        var xml = @"
            <root>
                <StatementOfFundsLine assetCategory='STK' symbol='AAPL' 
                    activityDescription='Option Cash Settlement for: Assignment' 
                    reportDate='16-Mar-18' amount='5000' currency='USD' />
            </root>";
        var document = XElement.Parse(xml);
        var result = IBXmlCashSettlementParser.ParseXml(document);
        result.ShouldBeEmpty();
    }
}
