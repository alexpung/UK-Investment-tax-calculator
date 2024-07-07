using Enumerations;

using Model;
using Model.TaxEvents;

using System.Globalization;
using System.Xml.Linq;

using TaxEvents;

namespace Parser.InteractiveBrokersXml;

public static class IBXmlFutureTradeParser
{
    public static IList<Trade> ParseXml(XElement document)
    {
        IEnumerable<XElement> filteredElements = document.Descendants("Order").Where(row => row.GetAttribute("levelOfDetail") == "ORDER" &&
                                                row.GetAttribute("assetCategory") == "FUT");
        return filteredElements.Select(TradeMaker).Where(trade => trade != null).ToList()!;
    }

    private static Trade? TradeMaker(XElement element)
    {
        return new FutureContractTrade
        {
            AssetType = AssetCatagoryType.FUTURE,
            AcquisitionDisposal = element.GetTradeType(),
            AssetName = element.GetAttribute("symbol"),
            Description = element.GetAttribute("description"),
            Date = DateTime.Parse(element.GetAttribute("dateTime"), CultureInfo.InvariantCulture),
            Quantity = element.GetQuantity(),
            GrossProceed = new DescribedMoney() { Amount = WrappedMoney.GetBaseCurrencyZero() },
            Expenses = element.BuildExpenses(),
            ContractValue = element.GetContractValue()
        };
    }
}
