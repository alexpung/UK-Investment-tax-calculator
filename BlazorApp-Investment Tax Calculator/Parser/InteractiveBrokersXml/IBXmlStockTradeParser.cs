using Model.TaxEvents;

using System.Globalization;
using System.Xml.Linq;

namespace Parser.InteractiveBrokersXml;

public static class IBXmlStockTradeParser
{
    public static IList<Trade> ParseXml(XElement document)
    {
        IEnumerable<XElement> filteredElements = document.Descendants("Order").Where(row => row.GetAttribute("levelOfDetail") == "ORDER" &&
                                                 row.GetAttribute("assetCategory") == "STK");
        return filteredElements.Select(element => XmlParserHelper.ParserExceptionManager(TradeMaker, element)).Where(trade => trade != null).ToList()!;
    }

    private static Trade? TradeMaker(XElement element)
    {
        return new Trade
        {
            AcquisitionDisposal = element.GetTradeType(),
            AssetName = element.GetAttribute("symbol"),
            Description = element.GetAttribute("description"),
            Date = DateTime.Parse(element.GetAttribute("dateTime"), CultureInfo.InvariantCulture),
            Quantity = element.GetQuantity(),
            GrossProceed = element.GetGrossProceed(),
            Expenses = element.BuildExpenses(),
        };
    }
}
