using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Xml.Linq;

namespace InvestmentTaxCalculator.Parser.InteractiveBrokersXml;


public static class IBXmlDividendParser
{
    public static IList<Dividend> ParseXml(XElement document)
    {
        IEnumerable<XElement> filteredElements = document.Descendants("CashTransaction").Where(row => GetDividendType(row) != DividendType.NOT_DIVIDEND && row.GetAttribute("levelOfDetail") == "DETAIL");
        return filteredElements.Select(DividendMaker).Where(dividend => dividend != null).ToList()!;
    }

    public static IList<CorporateAction> ParseReturnOfCapital(XElement document)
    {
        IEnumerable<XElement> filteredElements = document.Descendants("CashTransaction")
            .Where(row => GetDividendType(row) == DividendType.RETURN_OF_CAPITAL && row.GetAttribute("levelOfDetail") == "DETAIL");

        return [.. filteredElements.Select(element =>
        {
            try
            {
                return (CorporateAction)new ReturnOfCapitalCorporateAction
                {
                    AssetName = element.GetAttribute("symbol"),
                    Date = XmlParserHelper.ParseDate(element.GetAttribute("settleDate")),
                    Amount = element.BuildDescribedMoney("amount", "currency", "fxRateToBase", element.GetAttribute("description")),
                    Isin = element.GetAttribute("isin")
                };
            }
            catch (Exception ex)
            {
                string exceptionMessage = $"Exception occurred processing XElement: {element} - Original Exception: {ex.Message}";
                throw new ParseException(exceptionMessage, ex);
            }
        })];
    }

    private static Dividend? DividendMaker(XElement element)
    {
        try
        {

            return new Dividend
            {
                DividendType = GetDividendType(element),
                AssetName = element.GetAttribute("symbol"),
                Date = XmlParserHelper.ParseDate(element.GetAttribute("settleDate")),
                CompanyLocation = GetCompanyLocation(element),
                Proceed = element.BuildDescribedMoney("amount", "currency", "fxRateToBase", element.GetAttribute("description")),
                Isin = element.GetAttribute("isin")
            };
        }
        catch (Exception ex)
        {
            string exceptionMessage = $"Exception occurred processing XElement: {element} - Original Exception: {ex.Message}";
            throw new ParseException(exceptionMessage, ex);
        }
    }

    private static CountryCode GetCompanyLocation(XElement dividendElement)
    {
        if (dividendElement.GetAttribute("description").Contains("US TAX"))
        {
            return CountryCode.GetRegionByTwoDigitCode("US");
        }
        if (dividendElement.GetAttribute("description").Contains("CA TAX"))
        {
            return CountryCode.GetRegionByTwoDigitCode("CA");
        }
        return CountryCode.GetRegionByTwoDigitCode(dividendElement.GetAttribute("isin")[..2]);
    }

    private static DividendType GetDividendType(XElement dividendElement)
    {
        if (dividendElement.GetAttribute("description").Contains("Return of Capital")) return DividendType.RETURN_OF_CAPITAL;
        else return dividendElement.GetAttribute("type") switch
        {
            "Withholding Tax" => DividendType.WITHHOLDING,
            "Dividends" => DividendType.DIVIDEND,
            "Payment In Lieu Of Dividends" => DividendType.DIVIDEND_IN_LIEU,
            _ => DividendType.NOT_DIVIDEND
        };
    }
}
