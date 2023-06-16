using Model;
using Parser;
using Parser.InteractiveBrokersXml;

namespace UnitTest.Test;
public class UkTradeCalculatorTest
{
    [Fact]
    public void TestTradeCalculator()
    {
        // TODO: Implement tests
        List<ITaxEventFileParser> taxEventFileParsers = new List<ITaxEventFileParser>
        {
            new IBParseController(new AssetTypeToLoadSetting(), new IBXmlDividendParser(), new IBXmlStockTradeParser(), new IBXmlStockSplitParser())
        };
        FileParseController fileParseController = new(taxEventFileParsers);
        //TaxEventLists taxEventLists = fileParseController.ParseFolder("C:\\Users\\Alex Pun\\Desktop\\IB statements\\IB xml cash");
        //UkTradeCalculator ukTradeCalculator = new();
        //ukTradeCalculator.AddTaxEvents(taxEventLists);
        //List<TradeTaxCalculation> results = ukTradeCalculator.CalculateTax();
    }
}
