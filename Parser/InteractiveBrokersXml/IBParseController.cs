using CapitalGainCalculator.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CapitalGainCalculator.Parser.InteractiveBrokersXml
{
    public class IBParseController : ITaxEventFileParser
    {
        public IList<TaxEvent> ParseFile(string fileUri)
        {
            List<TaxEvent> result = new List<TaxEvent>();
            IBXmlDividendParser dividendParser = new IBXmlDividendParser();
            IBXmlStockSplitParser stockSplitParser = new IBXmlStockSplitParser();
            IBXmlTradeParser tradeParser = new IBXmlTradeParser();
            XElement? xml = XDocument.Load(fileUri).Root;
            if (xml is not null)
            {
                result.AddRange(dividendParser.ParseXml(xml));
                result.AddRange(stockSplitParser.ParseXml(xml));
                result.AddRange(tradeParser.ParseXml(xml));
            }
            return result;
        }

        public bool CheckFileValidity(string fileUri)
        {
            if (System.IO.Path.GetExtension(fileUri) == ".xml")
            {
                XElement? xml = XDocument.Load(fileUri).Root;
                if (xml is not null)
                {
                    return xml.DescendantsAndSelf("FlexQueryResponse").Any() && xml.Descendants("FlexStatements").Any();
                }
            }
            return false;
        }
    }
}
