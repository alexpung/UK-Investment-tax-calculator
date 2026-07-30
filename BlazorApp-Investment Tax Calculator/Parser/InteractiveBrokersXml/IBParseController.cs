using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

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
            if (assetTypeToLoadSetting.LoadDividends) result.Dividends.AddRange(IBXmlDividendParser.ParseXml(xml));
            if (assetTypeToLoadSetting.LoadDividends) result.CorporateActions.AddRange(IBXmlDividendParser.ParseReturnOfCapital(xml));
            result.CorporateActions.AddRange(IBXmlStockSplitParser.ParseXml(xml));
            List<Trade> stockAndFundTrades = [];
            if (assetTypeToLoadSetting.LoadStocks) stockAndFundTrades.AddRange(IBXmlStockTradeParser.ParseXml(xml));
            if (assetTypeToLoadSetting.LoadFunds) stockAndFundTrades.AddRange(IBXmlStockTradeParser.ParseFundXml(xml));
            IBXmlStockTradeParser.NormaliseAssetNamesByIsin(stockAndFundTrades);
            result.Trades.AddRange(stockAndFundTrades);
            if (assetTypeToLoadSetting.LoadFutures) result.FutureContractTrades.AddRange(IBXmlFutureTradeParser.ParseXml(xml));
            if (assetTypeToLoadSetting.LoadFx) result.Trades.AddRange(_xmlFxParser.ParseXml(xml));
            if (assetTypeToLoadSetting.LoadOptions) result.OptionTrades.AddRange(IBXmlOptionTradeParser.ParseXml(xml));
            if (assetTypeToLoadSetting.LoadOptions) result.CashSettlements.AddRange(IBXmlCashSettlementParser.ParseXml(xml));
            if (assetTypeToLoadSetting.LoadInterestIncome) result.InterestIncomes.AddRange(IBXmlInterestIncomeParser.ParseXml(xml));
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
