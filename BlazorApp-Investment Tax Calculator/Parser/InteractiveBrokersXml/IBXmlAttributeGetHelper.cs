using Enumerations;

using Model;

using System.Collections.Immutable;
using System.Xml.Linq;

namespace Parser.InteractiveBrokersXml;

public static class IBXmlAttributeGetHelper
{
    public static ImmutableList<DescribedMoney> BuildExpenses(this XElement element)
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

    public static decimal GetQuantity(this XElement element) => element.GetAttribute("buySell") switch
    {
        "BUY" => decimal.Parse(element.GetAttribute("quantity")),
        "SELL" => decimal.Parse(element.GetAttribute("quantity")) * -1,
        _ => throw new NotImplementedException(),
    };

    public static DescribedMoney GetGrossProceed(this XElement element) => element.GetAttribute("buySell") switch
    {
        "BUY" => element.BuildDescribedMoney("proceeds", "currency", "fxRateToBase", "", true),
        "SELL" => element.BuildDescribedMoney("proceeds", "currency", "fxRateToBase", ""),
        _ => throw new NotImplementedException(),
    };

    public static TradeType GetTradeType(this XElement element) => element.GetAttribute("buySell") switch
    {
        "BUY" => TradeType.ACQUISITION,
        "SELL" => TradeType.DISPOSAL,
        _ => throw new NotImplementedException($"Unrecognised trade type {element.GetAttribute("buySell")}")
    };

    public static DescribedMoney GetContractValue(this XElement element) => element.GetAttribute("buySell") switch
    {
        "BUY" => element.BuildDescribedMoney("proceeds", "currency", "fxRateToBase", "", true),
        "SELL" => element.BuildDescribedMoney("proceeds", "currency", "fxRateToBase", ""),
        _ => throw new NotImplementedException(),
    };
}