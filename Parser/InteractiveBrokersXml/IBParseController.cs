using CapitalGainCalculator.Model;
using System.Linq;
using System.Xml.Linq;

namespace CapitalGainCalculator.Parser.InteractiveBrokersXml;

public class IBParseController : ITaxEventFileParser
{
    private readonly AssetTypeToLoadSetting _assetTypeToLoadSetting;
    public IBParseController(AssetTypeToLoadSetting assetTypeToLoadSetting)
    {
        _assetTypeToLoadSetting = assetTypeToLoadSetting;
    }
    public TaxEventLists ParseFile(string fileUri)
    {
        TaxEventLists result = new TaxEventLists();
        IBXmlDividendParser dividendParser = new IBXmlDividendParser();
        IBXmlStockSplitParser stockSplitParser = new IBXmlStockSplitParser();
        IBXmlStockTradeParser stockTradeParser = new IBXmlStockTradeParser();
        XElement? xml = XDocument.Load(fileUri).Root;
        if (xml is not null)
        {
            if (_assetTypeToLoadSetting.LoadDividend) result.Dividends.AddRange(dividendParser.ParseXml(xml));
            if (_assetTypeToLoadSetting.LoadStocks) result.CorporateActions.AddRange(stockSplitParser.ParseXml(xml));
            if (_assetTypeToLoadSetting.LoadStocks) result.Trades.AddRange(stockTradeParser.ParseXml(xml));
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
