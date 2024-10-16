using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Services;

using System.Xml.Linq;

namespace InvestmentTaxCalculator.Parser.InteractiveBrokersXml;

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
                result.Dividends.AddRange(IBXmlDividendParser.ParseXml(xml));
                result.CorporateActions.AddRange(IBXmlStockSplitParser.ParseXml(xml));
                result.Trades.AddRange(IBXmlStockTradeParser.ParseXml(xml));
                result.FutureContractTrades.AddRange(IBXmlFutureTradeParser.ParseXml(xml));
                result.Trades.AddRange(_xmlFxParser.ParseXml(xml));
                result.OptionTrades.AddRange(IBXmlOptionTradeParser.ParseXml(xml));
                result.CashSettlements.AddRange(IBXmlCashSettlementParser.ParseXml(xml));
                result = assetTypeToLoadSetting.FilterTaxEvent(result);
            }
        }
        catch (ParseException ex)
        {
            toastService.ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            toastService.ShowError($"An unexpected error have occurred.\n {ex.Message}");
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
