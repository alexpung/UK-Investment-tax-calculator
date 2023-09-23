using Model;
using System.Xml.Linq;

namespace Parser;

public static class XmlParserHelper
{
    public static string GetAttribute(this XElement xElement, string attributeName)
    {
        XAttribute? xAttribute = xElement.Attribute(attributeName);
        if (xAttribute is not null)
        {
            return xAttribute.Value;
        }
        else throw new ArgumentException(@$"The attribute ""{attributeName}"" is not found in ""{xElement.Name}"", please include this attribute in your XML statement");
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
}
