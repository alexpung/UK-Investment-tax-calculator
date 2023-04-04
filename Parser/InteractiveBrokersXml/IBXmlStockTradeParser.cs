using CapitalGainCalculator.Enum;
using CapitalGainCalculator.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CapitalGainCalculator.Parser.InteractiveBrokersXml;

public class IBXmlStockTradeParser
{
    public IList<Trade> ParseXml(XElement document)
    {
        IEnumerable<XElement> filteredElements = document.Descendants("Order").Where(row => row.GetAttribute("levelOfDetail") == "ORDER" && row.GetAttribute("assetCategory") == "STK");
        return filteredElements.Select(TradeMaker).Where(trade => trade != null).ToList()!;
    }

    private Trade? TradeMaker(XElement element)
    {
        try
        {
            return new Trade
            {
                BuySell = GetTradeType(element),
                AssetName = element.GetAttribute("symbol"),
                Description = element.GetAttribute("description"),
                Date = DateTime.Parse(element.GetAttribute("dateTime")),
                Quantity = GetQuantity(element),
                GrossProceed = GetGrossProceed(element),
                Expenses = BuildExpenses(element),
            };
        }
        catch { return null; } // TODO Implement suitable catch clause and logging
    }

    private decimal GetQuantity(XElement element) => element.GetAttribute("buySell") switch
    {
        "BUY" => decimal.Parse(element.GetAttribute("quantity")),
        "SELL" => decimal.Parse(element.GetAttribute("quantity")) * -1,
        _ => throw new NotImplementedException(),
    };

    private DescribedMoney GetGrossProceed(XElement element) => element.GetAttribute("buySell") switch
    {
        "BUY" => element.BuildDescribedMoney("proceeds", "currency", "fxRateToBase", "", true),
        "SELL" => element.BuildDescribedMoney("proceeds", "currency", "fxRateToBase", ""),
        _ => throw new NotImplementedException(),
    };

    private TradeType GetTradeType(XElement element) => element.GetAttribute("buySell") switch
    {
        "BUY" => TradeType.BUY,
        "SELL" => TradeType.SELL,
        _ => throw new NotImplementedException($"Unrecognised trade type {element.GetAttribute("buySell")}")
    };

    private List<DescribedMoney> BuildExpenses(XElement element)
    {
        List<DescribedMoney> expenses = new List<DescribedMoney>();
        if (element.GetAttribute("ibCommission") != "0")
        {
            expenses.Add(element.BuildDescribedMoney("ibCommission", "ibCommissionCurrency", "fxRateToBase", "Commission", true));
        }
        if (element.GetAttribute("taxes") != "0")
        {
            element.BuildDescribedMoney("taxes", "currency", "fxRateToBase", "Tax", true);
        }
        return expenses;
    }
}
