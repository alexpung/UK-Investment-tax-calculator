using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Xml.Linq;

namespace InvestmentTaxCalculator.Parser.InteractiveBrokersXml;

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
            Date = XmlParserHelper.ParseDate(element.GetAttribute("dateTime")),
            Quantity = element.GetQuantity(),
            GrossProceed = element.GetGrossProceed(),
            Expenses = element.BuildExpenses(),
            TradeReason = element.GetAttribute("notes") switch
            {
                string s when s.Split(";").Contains("Ex") => TradeReason.OwnerExerciseOption,
                string s when s.Split(";").Contains("A") => TradeReason.OptionAssigned,
                string s when s.Split(";").Contains("Ep") => TradeReason.Expired,
                _ => TradeReason.OrderedTrade
            }
        };
    }
}
