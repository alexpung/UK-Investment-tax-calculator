using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Parser;

using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace InvestmentTaxCalculator.Parser.InteractiveBrokersXml;

public static class IBXmlStockSplitParser
{
    public static IList<StockSplit> ParseXml(XElement document)
    {
        IEnumerable<XElement> filteredElements = document.Descendants("CorporateAction").Where(row => row.GetAttribute("type") == "FS");
        return filteredElements.Select(StockSplitMaker).Where(dividend => dividend != null).ToList()!;
    }

    private static StockSplit StockSplitMaker(XElement element)
    {
        string matchExpression = @"SPLIT (\d*) FOR (\d*)";
        string description = element.GetAttribute("description");
        Regex regex = new(matchExpression, RegexOptions.Compiled);
        Match matchResult = regex.Match(description);
        return new StockSplit
        {
            AssetName = element.GetAttribute("symbol"),
            Date = DateTime.Parse(element.GetAttribute("dateTime"), CultureInfo.InvariantCulture),
            SplitFrom = ushort.Parse(matchResult.Groups[2].Value),
            SplitTo = ushort.Parse(matchResult.Groups[1].Value),
        };

    }
}
