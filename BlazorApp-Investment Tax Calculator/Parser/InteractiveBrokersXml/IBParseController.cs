using Model;

using System.Xml.Linq;

namespace Parser.InteractiveBrokersXml;

public class IBParseController : ITaxEventFileParser
{
    private readonly AssetTypeToLoadSetting _assetTypeToLoadSetting;
    private readonly IBXmlFxParser _xmlFxParser = new();

    public IBParseController(AssetTypeToLoadSetting assetTypeToLoadSetting)
    {
        _assetTypeToLoadSetting = assetTypeToLoadSetting;
    }
    public TaxEventLists ParseFile(string data)
    {
        TaxEventLists result = new();
        XElement? xml = XDocument.Parse(data).Root;
        if (xml is not null)
        {
            if (_assetTypeToLoadSetting.LoadDividends) result.Dividends.AddRange(IBXmlDividendParser.ParseXml(xml));
            if (_assetTypeToLoadSetting.LoadStocks) result.CorporateActions.AddRange(IBXmlStockSplitParser.ParseXml(xml));
            if (_assetTypeToLoadSetting.LoadStocks) result.Trades.AddRange(IBXmlStockTradeParser.ParseXml(xml));
            if (_assetTypeToLoadSetting.LoadFutures) result.Trades.AddRange(IBXmlFutureTradeParser.ParseXml(xml));
            if (_assetTypeToLoadSetting.LoadFx) result.Trades.AddRange(_xmlFxParser.ParseXml(xml));
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
