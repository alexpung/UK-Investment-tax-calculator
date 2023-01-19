using CapitalGainCalculator.Enum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CapitalGainCalculator.Parser.InteractiveBrokersXml
{
    public class IBXmlParser
    {
        public IEnumerable<XElement> ParseDividend(XElement document)
        {
            return document.Descendants("CashTransaction").Where(row => GetDividendType(row.Attribute("type")?.Value) != DividendType.NOT_DIVIDEND);
        }

        private DividendType GetDividendType(string? dividendTypeString) => dividendTypeString switch
        {
            "Withholding Tax" => DividendType.WITHHOLDING,
            "Dividends" => DividendType.DIVIDEND,
            "Payment In Lieu Of Dividends" => DividendType.DIVIDEND_IN_LIEU,
            _ => DividendType.NOT_DIVIDEND
        };
    }
}
