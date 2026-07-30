using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Xml.Linq;

namespace InvestmentTaxCalculator.Parser.InteractiveBrokersXml;

public static class IBXmlStockTradeParser
{
    public static IList<Trade> ParseXml(XElement document)
    {
        return ParseXml(document, "STK", AssetCategoryType.STOCK);
    }

    /// <summary>
    /// IBKR reports mutual funds and some ETFs with assetCategory="FUND".
    /// They are parsed like stock trades but tagged as FUND asset category.
    /// </summary>
    public static IList<Trade> ParseFundXml(XElement document)
    {
        return ParseXml(document, "FUND", AssetCategoryType.FUND);
    }

    private static IList<Trade> ParseXml(XElement document, string ibAssetCategory, AssetCategoryType assetCategoryType)
    {
        IEnumerable<XElement> filteredElements = document.Descendants("Order").Where(row => row.GetAttribute("levelOfDetail") == "ORDER" &&
                                                 row.GetAttribute("assetCategory") == ibAssetCategory);
        return filteredElements.Select(element => XmlParserHelper.ParserExceptionManager(e => TradeMaker(e, assetCategoryType), element)).Where(trade => trade != null).ToList()!;
    }

    private static Trade? TradeMaker(XElement element, AssetCategoryType assetCategoryType)
    {
        return new Trade
        {
            AssetType = assetCategoryType,
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
            },
            Isin = element.GetAttribute("isin")
        };
    }
}
