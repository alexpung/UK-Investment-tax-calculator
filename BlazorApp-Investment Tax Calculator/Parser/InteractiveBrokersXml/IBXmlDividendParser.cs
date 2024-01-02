using Enum;

using Model.TaxEvents;

using System.Globalization;
using System.Xml.Linq;

namespace Parser.InteractiveBrokersXml;


public static class IBXmlDividendParser
{
    public static IList<Dividend> ParseXml(XElement document)
    {
        IEnumerable<XElement> filteredElements = document.Descendants("CashTransaction").Where(row => GetDividendType(row) != DividendType.NOT_DIVIDEND && row.GetAttribute("levelOfDetail") == "DETAIL");
        return filteredElements.Select(DividendMaker).Where(dividend => dividend != null).ToList()!;
    }

    private static Dividend? DividendMaker(XElement element)
    {
        try
        {
            return new Dividend
            {
                DividendType = GetDividendType(element),
                AssetName = element.GetAttribute("symbol"),
                Date = DateTime.Parse(element.GetAttribute("settleDate")),
                CompanyLocation = GetCompanyLocation(element),
                Proceed = element.BuildDescribedMoney("amount", "currency", "fxRateToBase", element.GetAttribute("description"))
            };
        }
        catch { return null; } // TODO Implement suitable catch clause and logging */
    }

    private static RegionInfo GetCompanyLocation(XElement dividendElement)
    {
        try
        {
            return new RegionInfo(dividendElement.GetAttribute("isin")[..2]);
        }
        catch (ArgumentException) //CUSIP is shown
        {
            if (dividendElement.GetAttribute("description").Contains("US TAX"))
            {
                return new RegionInfo("US");
            }
            else if (dividendElement.GetAttribute("description").Contains("CA TAX"))
            {
                return new RegionInfo("CA");
            }
            else throw new ArgumentException($"Unable to determine Company Location with {dividendElement}");
        }
    }

    private static DividendType GetDividendType(XElement dividendElement) => dividendElement.GetAttribute("type") switch
    {
        "Withholding Tax" => DividendType.WITHHOLDING,
        "Dividends" => DividendType.DIVIDEND,
        "Payment In Lieu Of Dividends" => DividendType.DIVIDEND_IN_LIEU,
        _ => DividendType.NOT_DIVIDEND
    };
}
