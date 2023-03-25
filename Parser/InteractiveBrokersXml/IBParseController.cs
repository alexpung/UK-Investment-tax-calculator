using CapitalGainCalculator.Model;
using System.Linq;
using System.Xml.Linq;

namespace CapitalGainCalculator.Parser.InteractiveBrokersXml;

public class IBParseController : ITaxEventFileParser
{
    public TaxEventLists ParseFile(string fileUri)
    {
        TaxEventLists result = new TaxEventLists();
        IBXmlDividendParser dividendParser = new IBXmlDividendParser();
        IBXmlStockSplitParser stockSplitParser = new IBXmlStockSplitParser();
        IBXmlTradeParser tradeParser = new IBXmlTradeParser();
        XElement? xml = XDocument.Load(fileUri).Root;
        if (xml is not null)
        {
            result.Dividends.AddRange(dividendParser.ParseXml(xml));
            result.CorporateActions.AddRange(stockSplitParser.ParseXml(xml));
            result.Trades.AddRange(tradeParser.ParseXml(xml));
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
