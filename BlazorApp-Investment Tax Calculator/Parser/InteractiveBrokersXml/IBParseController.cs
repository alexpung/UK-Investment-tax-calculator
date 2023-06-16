using Model;
using System.Xml.Linq;

namespace Parser.InteractiveBrokersXml;

public class IBParseController : ITaxEventFileParser
{
    private readonly AssetTypeToLoadSetting _assetTypeToLoadSetting;
    private readonly IBXmlDividendParser _dividendParser;
    private readonly IBXmlStockTradeParser _stockTradeParser;
    private readonly IBXmlStockSplitParser _stockSplitParser;

    public IBParseController(AssetTypeToLoadSetting assetTypeToLoadSetting, IBXmlDividendParser iBXmlDividendParser, IBXmlStockTradeParser iBXmlStockTradeParser, IBXmlStockSplitParser iBXmlStockSplitParser)
    {
        _assetTypeToLoadSetting = assetTypeToLoadSetting;
        _dividendParser = iBXmlDividendParser;
        _stockSplitParser = iBXmlStockSplitParser;
        _stockTradeParser = iBXmlStockTradeParser;

    }
    public TaxEventLists ParseFile(string data)
    {
        TaxEventLists result = new();
        XElement? xml = XDocument.Parse(data).Root;
        if (xml is not null)
        {
            if (_assetTypeToLoadSetting.LoadDividends) result.Dividends.AddRange(_dividendParser.ParseXml(xml));
            if (_assetTypeToLoadSetting.LoadStocks) result.CorporateActions.AddRange(_stockSplitParser.ParseXml(xml));
            if (_assetTypeToLoadSetting.LoadStocks) result.Trades.AddRange(_stockTradeParser.ParseXml(xml));
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
