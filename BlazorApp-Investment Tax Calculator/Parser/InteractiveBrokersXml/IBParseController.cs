using InvestmentTaxCalculator.Model;

using System.Xml.Linq;

namespace InvestmentTaxCalculator.Parser.InteractiveBrokersXml;

public class IBParseController(AssetTypeToLoadSetting assetTypeToLoadSetting) : ITaxEventFileParser
{
    private readonly IBXmlFxParser _xmlFxParser = new();

    public TaxEventLists ParseFile(string data)
    {
        TaxEventLists result = new();
        XElement? xml = XDocument.Parse(data).Root;
        if (xml is not null)
        {
            result.Dividends.AddRange(IBXmlDividendParser.ParseXml(xml));
            result.CorporateActions.AddRange(IBXmlStockSplitParser.ParseXml(xml));
            result.Trades.AddRange(IBXmlStockTradeParser.ParseXml(xml));
            result.FutureContractTrades.AddRange(IBXmlFutureTradeParser.ParseXml(xml));
            result.Trades.AddRange(_xmlFxParser.ParseXml(xml));
            result.OptionTrades.AddRange(IBXmlOptionTradeParser.ParseXml(xml));
            result.CashSettlements.AddRange(IBXmlCashSettlementParser.ParseXml(xml));
            result.InterestIncomes.AddRange(IBXmlInterestIncomeParser.ParseXml(xml));
            result = assetTypeToLoadSetting.FilterTaxEvent(result);
        }
        return result;
    }

    public bool CheckFileValidity(string data, string contentType)
    {
        if (contentType != "text/xml") return false;
        XElement? xml = XDocument.Parse(data).Root;
        if (xml is null) return false;
        return xml.DescendantsAndSelf("FlexQueryResponse").Any() && xml.Descendants("FlexStatements").Any();
    }
}
