using Enumerations;

using Model;
using Model.TaxEvents;

using System.Collections.Immutable;
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
            AcquisitionDisposal = GetTradeType(element),
            AssetName = element.GetAttribute("symbol"),
            Description = element.GetAttribute("description"),
            Date = DateTime.Parse(element.GetAttribute("dateTime"), CultureInfo.InvariantCulture),
            Quantity = GetQuantity(element),
            GrossProceed = GetGrossProceed(element),
            Expenses = BuildExpenses(element),
        };
    }

    private static decimal GetQuantity(XElement element) => element.GetAttribute("buySell") switch
    {
        "BUY" => decimal.Parse(element.GetAttribute("quantity")),
        "SELL" => decimal.Parse(element.GetAttribute("quantity")) * -1,
        _ => throw new NotImplementedException(),
    };

    private static DescribedMoney GetGrossProceed(XElement element) => element.GetAttribute("buySell") switch
    {
        "BUY" => element.BuildDescribedMoney("proceeds", "currency", "fxRateToBase", "", true),
        "SELL" => element.BuildDescribedMoney("proceeds", "currency", "fxRateToBase", ""),
        _ => throw new NotImplementedException(),
    };

    private static TradeType GetTradeType(XElement element) => element.GetAttribute("buySell") switch
    {
        "BUY" => TradeType.ACQUISITION,
        "SELL" => TradeType.DISPOSAL,
        _ => throw new NotImplementedException($"Unrecognised trade type {element.GetAttribute("buySell")}")
    };

    private static ImmutableList<DescribedMoney> BuildExpenses(XElement element)
    {
        List<DescribedMoney> expenses = [];
        if (element.GetAttribute("ibCommission") != "0")
        {
            expenses.Add(element.BuildDescribedMoney("ibCommission", "ibCommissionCurrency", "fxRateToBase", "Commission", true));
        }
        if (element.GetAttribute("taxes") != "0")
        {
            expenses.Add(element.BuildDescribedMoney("taxes", "currency", "fxRateToBase", "Tax", true));
        }
        return [.. expenses];
    }
}
