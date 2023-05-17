using CapitalGainCalculator.Model;
using CapitalGainCalculator.Parser;
using CapitalGainCalculator.Parser.InteractiveBrokersXml;

namespace UnitTest.Test;
public class UkTradeCalculatorTest
{
    [Fact]
    public void TestTradeCalculator()
    {
        List<ITaxEventFileParser> taxEventFileParsers = new List<ITaxEventFileParser>
        {
            new IBParseController(new AssetTypeToLoadSetting())
        };
        FileParseController fileParseController = new(taxEventFileParsers);
        TaxEventLists taxEventLists = fileParseController.ParseFolder("C:\\Users\\Alex Pun\\Desktop\\IB statements\\IB xml cash");
        //UkTradeCalculator ukTradeCalculator = new();
        //ukTradeCalculator.AddTaxEvents(taxEventLists);
        //List<TradeTaxCalculation> results = ukTradeCalculator.CalculateTax();
    }
}
