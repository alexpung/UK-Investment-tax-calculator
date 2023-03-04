using CapitalGainCalculator.Enum;
using CapitalGainCalculator.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CapitalGainCalculator.Parser.InteractiveBrokersXml
{
    public class IBXmlTradeParser
    {
        public IList<Trade> ParseXml(XElement document)
        {
            IEnumerable<XElement> filteredElements = document.Descendants("Order").Where(row => row.GetAttribute("levelOfDetail") == "ORDER");
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
                    Date = DateTime.Parse(element.GetAttribute("dateTime")),
                    Quantity = Decimal.Parse(element.GetAttribute("quantity")),
                    Proceed = element.BuildDescribedMoney("proceeds", "currency", "fxRateToBase", element.GetAttribute("description")),
                    Expenses = BuildExpenses(element),
                };
            }
            catch { return null; } // TODO Implement suitable catch clause and logging
        }

        private TradeType GetTradeType(XElement element) => element.GetAttribute("buySell") switch
        {
            "BUY" => TradeType.BUY,
            "SELL" => TradeType.SELL,
            _ => throw new ArgumentException($"Unrecognised trade type {element.GetAttribute("buySell")}")
        };

        private List<DescribedMoney> BuildExpenses(XElement element)
        {
            List<DescribedMoney> expenses = new List<DescribedMoney>();
            if (element.GetAttribute("ibCommission") != "0")
            {
                expenses.Add(element.BuildDescribedMoney("ibCommission", "ibCommissionCurrency", "fxRateToBase", "Commission"));
            }
            if (element.GetAttribute("taxes") != "0")
            {
                element.BuildDescribedMoney("taxes", "currency", "fxRateToBase", "Tax");
            }
            return expenses;
        }
    }
}
