using InvestmentTaxCalculator.Model.TaxEvents;

using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace InvestmentTaxCalculator.Parser.InteractiveBrokersXml;

public static class IBXmlStockSplitParser
{
    public static IList<StockSplit> ParseXml(XElement document)
    {
        // IB uses FS for forward split and RS for reverse split
        IEnumerable<XElement> filteredElements = document.Descendants("CorporateAction")
            .Where(row => row.GetAttribute("type") == "FS" || row.GetAttribute("type") == "RS");
        return filteredElements.Select(StockSplitMaker).Where(s => s != null).ToList()!;
    }

    private static StockSplit? StockSplitMaker(XElement element)
    {
        // Matches "SPLIT 1 FOR 10", "SPLIT 10 TO 1", "SPLIT 2:1" etc.
        string matchExpression = @"SPLIT\s*(\d+)\s*(?:FOR|TO|:)\s*(\d+)";
        string description = element.GetAttribute("description");
        
        Regex regex = new(matchExpression, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        Match matchResult = regex.Match(description);
        
        if (!matchResult.Success)
        {
            return null; // Or handle unparsed split descriptions as needed
        }

        return new StockSplit
        {
            AssetName = element.GetAttribute("symbol"),
            Date = XmlParserHelper.ParseDate(element.GetAttribute("dateTime")),
            SplitTo = int.Parse(matchResult.Groups[1].Value),
            SplitFrom = int.Parse(matchResult.Groups[2].Value),
        };
    }
}
