using Model;
using System.Xml.Linq;

namespace Parser.InteractiveBrokersXml;

public class IBParseController : ITaxEventFileParser
{
    private readonly AssetTypeToLoadSetting _assetTypeToLoadSetting;
    public IBParseController(AssetTypeToLoadSetting assetTypeToLoadSetting)
    {
        _assetTypeToLoadSetting = assetTypeToLoadSetting;
    }
    public TaxEventLists ParseFile(string data)
    {
        TaxEventLists result = new();
        IBXmlDividendParser dividendParser = new();
        IBXmlStockSplitParser stockSplitParser = new();
        IBXmlStockTradeParser stockTradeParser = new();
        XElement? xml = XDocument.Parse(data).Root;
        if (xml is not null)
        {
            if (_assetTypeToLoadSetting.LoadDividends) result.Dividends.AddRange(dividendParser.ParseXml(xml));
            if (_assetTypeToLoadSetting.LoadStocks) result.CorporateActions.AddRange(stockSplitParser.ParseXml(xml));
            if (_assetTypeToLoadSetting.LoadStocks) result.Trades.AddRange(stockTradeParser.ParseXml(xml));
        }
        return result;
    }

    public bool CheckFileValidity(string data, string contentType)
    {
        if (contentType != "text/xml") return false;
        XElement? xml = XDocument.Parse(data).Root;
        if (xml is not null)
        {
            return xml.DescendantsAndSelf("FlexQueryResponse").Any() && xml.Descendants("FlexStatements").Any();
        }
        return false;
    }
}
