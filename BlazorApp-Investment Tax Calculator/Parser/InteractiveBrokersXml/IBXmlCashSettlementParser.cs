using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Xml.Linq;

namespace InvestmentTaxCalculator.Parser.InteractiveBrokersXml;

public static class IBXmlCashSettlementParser
{
    public static IList<CashSettlement> ParseXml(XElement document)
    {
        IEnumerable<XElement> filteredElements = document.Descendants("StatementOfFundsLine").Where(row => row.GetAttribute("activityDescription")
        .Contains("Option Cash Settlement") && row.GetAttribute("assetCategory") == "OPT");
        return filteredElements.Select(element => XmlParserHelper.ParserExceptionManager(OptionTradeMaker, element))
                                                                                          .Where(trade => trade != null).ToList()!;

    }

    private static CashSettlement? OptionTradeMaker(XElement element)
    {
        return new CashSettlement
        {
            AssetName = element.GetAttribute("symbol"),
            Description = element.GetAttribute("activityDescription"),
            Date = XmlParserHelper.ParseDate(element.GetAttribute("date")),
            Amount = element.BuildDescribedMoney("amount", "currency", "fxRateToBase", element.GetAttribute("activityDescription")),
            TradeReason = element.GetAttribute("activityDescription") switch
            {
                string s when s.Contains("Option Cash Settlement for: Exercise") => TradeReason.OwnerExerciseOption,
                string s when s.Contains("Option Cash Settlement for: Assignment") => TradeReason.OptionAssigned,
                _ => throw new ParseException($"Unknown cash settlement reason {element.GetAttribute("activityDescription")} for {element}")
            }
        };
    }
}


