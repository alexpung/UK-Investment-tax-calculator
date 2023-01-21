using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CapitalGainCalculator.Parser
{
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
    }
}
