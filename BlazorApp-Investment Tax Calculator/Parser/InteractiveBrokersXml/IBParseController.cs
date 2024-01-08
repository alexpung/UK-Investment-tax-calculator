using Model;

using Services;

using System.Xml.Linq;

namespace Parser.InteractiveBrokersXml;

public class IBParseController(AssetTypeToLoadSetting assetTypeToLoadSetting, ToastService toastService) : ITaxEventFileParser
{
    private readonly IBXmlFxParser _xmlFxParser = new();

    public TaxEventLists ParseFile(string data)
    {
        TaxEventLists result = new();
        XElement? xml = XDocument.Parse(data).Root;
        try
        {
            if (xml is not null)
            {
                if (assetTypeToLoadSetting.LoadDividends) result.Dividends.AddRange(IBXmlDividendParser.ParseXml(xml));
                if (assetTypeToLoadSetting.LoadStocks) result.CorporateActions.AddRange(IBXmlStockSplitParser.ParseXml(xml));
                if (assetTypeToLoadSetting.LoadStocks) result.Trades.AddRange(IBXmlStockTradeParser.ParseXml(xml));
                if (assetTypeToLoadSetting.LoadFutures) result.Trades.AddRange(IBXmlFutureTradeParser.ParseXml(xml));
                if (assetTypeToLoadSetting.LoadFx) result.Trades.AddRange(_xmlFxParser.ParseXml(xml));
            }
        }
        catch (ParseException ex)
        {
            toastService.ShowToast("Error", ex.Message, ToastOptionType.Error);
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
