using CapitalGainCalculator.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CapitalGainCalculator.Parser.InteractiveBrokersXml;

public class IBXmlStockSplitParser
{
    public IList<StockSplit> ParseXml(XElement document)
    {
        IEnumerable<XElement> filteredElements = document.Descendants("CorporateAction").Where(row => row.GetAttribute("description").Contains("SPLIT"));
        return filteredElements.Select(StockSplitMaker).Where(dividend => dividend != null).ToList()!;
    }

    private StockSplit StockSplitMaker(XElement element)
    {
        string matchExpression = @"SPLIT (\d*) FOR (\d*)";
        string description = element.GetAttribute("description");
        Regex regex = new(matchExpression, RegexOptions.Compiled);
        Match matchResult = regex.Match(description);
        return new StockSplit
        {
            AssetName = element.GetAttribute("symbol"),
            Date = DateTime.Parse(element.GetAttribute("dateTime")),
            NumberBeforeSplit = ushort.Parse(matchResult.Groups[2].Value),
            NumberAfterSplit = ushort.Parse(matchResult.Groups[1].Value),
        };

    }
}
