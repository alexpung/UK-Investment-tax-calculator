using CapitalGainCalculator.Model;
using NodaMoney;
using System;
using System.Xml.Linq;

namespace CapitalGainCalculator.Parser;

public static class XmlParserHelper
{
    public static string GetAttribute(this XElement xElement, string attributeName)
    {
        XAttribute? xAttribute = xElement.Attribute(attributeName);
        if (xAttribute is not null)
        {
            return xAttribute.Value;
        }
        else throw new NullReferenceException(@$"The attribute ""{attributeName}"" is not found in ""{xElement.Name}"", please include this attribute in your XML statement");
    }

    public static Money BuildMoney(this XElement xElement, string amountAttributeName, string currencyAttributeName)
    {
        return Money.Parse(xElement.GetAttribute(amountAttributeName), Currency.FromCode(xElement.GetAttribute(currencyAttributeName)));
    }

    public static DescribedMoney BuildDescribedMoney(this XElement xElement, string amountAttributeName, string currencyAttributeName, string exchangeRateAttributeName, string description, bool revertSign = false)
    {
        Money money = xElement.BuildMoney(amountAttributeName, currencyAttributeName);
        return new DescribedMoney
        {
            Amount = revertSign ? money * -1 : money,
            Description = description,
            FxRate = decimal.Parse(xElement.GetAttribute(exchangeRateAttributeName))
        };
    }
}
