using InvestmentTaxCalculator.Model;

using Parser;

using System.Xml.Linq;

namespace InvestmentTaxCalculator.Parser;

public static class XmlParserHelper
{
    public static string GetAttribute(this XElement xElement, string attributeName)
    {
        XAttribute? xAttribute = xElement.Attribute(attributeName);
        if (xAttribute is not null)
        {
            return xAttribute.Value;
        }
        else
        {
            string attributeDump = string.Join(", ", xElement.Attributes().Select(i => i.Value));
            throw new ParseException(@$"The attribute ""{attributeName}"" is not found in ""{xElement.Name}"" in the line with data: {attributeDump}, 
            please include this attribute in your XML statement");
        }

    }

    public static WrappedMoney BuildMoney(this XElement xElement, string amountAttributeName, string currencyAttributeName)
    {
        return new WrappedMoney(decimal.Parse(xElement.GetAttribute(amountAttributeName)), xElement.GetAttribute(currencyAttributeName));
    }

    public static DescribedMoney BuildDescribedMoney(this XElement xElement, string amountAttributeName, string currencyAttributeName, string exchangeRateAttributeName, string description, bool revertSign = false)
    {
        WrappedMoney money = xElement.BuildMoney(amountAttributeName, currencyAttributeName);
        return new DescribedMoney
        {
            Amount = revertSign ? money * -1 : money,
            Description = description,
            FxRate = decimal.Parse(xElement.GetAttribute(exchangeRateAttributeName))
        };
    }

    public static T ParserExceptionManager<T>(Func<XElement, T> parseFunc, XElement xElement)
    {
        try
        {
            return parseFunc.Invoke(xElement);
        }
        catch (FormatException ex)
        {
            string attributeDump = string.Join(", ", xElement.Attributes().Select(i => i.Value));
            string exceptionMessage = $"Error processing date in the line with data: {attributeDump}, make sure timestamps is in the format of " +
                $"01-May-21 12:34:56";
            throw new ParseException(exceptionMessage, ex);
        }
    }
}
