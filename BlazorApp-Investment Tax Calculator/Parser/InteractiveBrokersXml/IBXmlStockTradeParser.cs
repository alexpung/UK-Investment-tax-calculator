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

    /// <summary>
    /// IBKR appends a lowercase exchange suffix to a symbol when the same instrument is traded on another exchange,
    /// e.g. "CWRl" vs "CWR" or "DISm" vs "DIS". When trades sharing the same ISIN only differ by such a suffix they
    /// are the same instrument, so all of them are renamed to the common base symbol with the suffix discarded.
    /// </summary>
    public static void NormaliseAssetNamesByIsin(IEnumerable<Trade> trades)
    {
        IEnumerable<IGrouping<string, Trade>> tradesGroupedByIsin = trades.Where(trade => !string.IsNullOrEmpty(trade.Isin)).GroupBy(trade => trade.Isin);
        foreach (IGrouping<string, Trade> isinGroup in tradesGroupedByIsin)
        {
            List<string> symbols = isinGroup.Select(trade => trade.AssetName).Distinct().ToList();
            if (symbols.Count < 2) continue;
            List<string> baseSymbols = symbols.Select(StripLowercaseSuffix).Distinct().ToList();
            if (baseSymbols.Count != 1 || string.IsNullOrEmpty(baseSymbols[0])) continue;
            foreach (Trade trade in isinGroup)
            {
                trade.AssetName = baseSymbols[0];
            }
        }
    }

    private static string StripLowercaseSuffix(string symbol)
    {
        int end = symbol.Length;
        while (end > 0 && char.IsLower(symbol[end - 1]))
        {
            end--;
        }
        return symbol[..end];
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
