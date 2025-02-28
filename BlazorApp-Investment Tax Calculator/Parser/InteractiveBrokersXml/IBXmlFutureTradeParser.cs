using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Xml.Linq;

namespace InvestmentTaxCalculator.Parser.InteractiveBrokersXml;

public static class IBXmlFutureTradeParser
{
    public static IList<FutureContractTrade> ParseXml(XElement document)
    {
        IEnumerable<XElement> filteredElements = document.Descendants("Order").Where(row => row.GetAttribute("levelOfDetail") == "ORDER" &&
                                                row.GetAttribute("assetCategory") == "FUT");
        return filteredElements.Select(TradeMaker).Where(trade => trade != null).ToList()!;
    }

    private static FutureContractTrade? TradeMaker(XElement element)
    {
        return new FutureContractTrade
        {
            AssetType = AssetCategoryType.FUTURE,
            AcquisitionDisposal = element.GetTradeType(),
            AssetName = element.GetAttribute("symbol"),
            Description = element.GetAttribute("description"),
            Date = XmlParserHelper.ParseDate(element.GetAttribute("dateTime")),
            Quantity = element.GetQuantity(),
            GrossProceed = new DescribedMoney() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            Expenses = element.BuildExpenses(),
            ContractValue = element.GetContractValue()
        };
    }
}
