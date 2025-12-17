using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Xml.Linq;

namespace InvestmentTaxCalculator.Parser.InteractiveBrokersXml;

public static class IBXmlInterestIncomeParser
{
    public static List<InterestIncome> ParseXml(XElement document)
    {
        IEnumerable<XElement> filteredElements = document.Descendants("StatementOfFundsLine")
            .Where(row => row.GetAttribute("activityCode") is "INTR" or "INTP" or "CINT" && row.GetAttribute("levelOfDetail") == "Currency");
        return filteredElements.Select(InterestIncomeMaker).Where(interestIncome => interestIncome != null).ToList()!;
    }

    private static InterestIncome? InterestIncomeMaker(XElement element)
    {
        InterestType? interestType = GetInterestIncomeType(element);
        if (interestType is null) return null;
        CountryCode incomeLocation;
        try
        {
            incomeLocation = interestType == InterestType.SAVINGS
            ? CountryCode.GetRegionByTwoDigitCode("GB") : CountryCode.GetRegionByTwoDigitCode(element.GetAttribute("issuerCountryCode"));
        }
        catch (ParseException)
        {
            incomeLocation = CountryCode.UnknownRegion;
        }
        try
        {
            return new InterestIncome
            {
                InterestType = (InterestType)interestType,
                AssetName = interestType == InterestType.SAVINGS ? "Broker interest" : element.GetAttribute("symbol"),
                Date = XmlParserHelper.ParseDate(element.GetAttribute("settleDate")),
                IncomeLocation = incomeLocation,
                Amount = element.BuildDescribedMoney("amount", "currency", "fxRateToBase", element.GetAttribute("activityDescription"))
            };
        }
        catch (Exception ex)
        {
            string exceptionMessage = $"Exception occurred processing XElement: {element} - Original Exception: {ex.Message}";
            throw new ParseException(exceptionMessage, ex);
        }
    }

    private static InterestType? GetInterestIncomeType(XElement interestIncomeElement)
    {
        string description = interestIncomeElement.GetAttribute("activityDescription");
        if (description.Contains("Purchase Accrued Interest")) return InterestType.ACCURREDINCOMELOSS;
        if (description.Contains("Sold Accrued Interest")) return InterestType.ACCURREDINCOMEPROFIT;
        if (description.Contains("Bond Coupon Payment")) return InterestType.BOND;
        if (interestIncomeElement.GetAttribute("activityCode") == "CINT") return InterestType.SAVINGS;
        return null;
    }
}
