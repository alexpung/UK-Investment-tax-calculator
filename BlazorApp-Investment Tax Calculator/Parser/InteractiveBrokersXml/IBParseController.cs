﻿using InvestmentTaxCalculator.Model;
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
                if (assetTypeToLoadSetting.LoadDividends) result.Dividends.AddRange(IBXmlDividendParser.ParseXml(xml));
                if (assetTypeToLoadSetting.LoadStocks) result.CorporateActions.AddRange(IBXmlStockSplitParser.ParseXml(xml));
                if (assetTypeToLoadSetting.LoadStocks) result.Trades.AddRange(IBXmlStockTradeParser.ParseXml(xml));
                if (assetTypeToLoadSetting.LoadFutures) result.FutureContractTrades.AddRange(IBXmlFutureTradeParser.ParseXml(xml));
                if (assetTypeToLoadSetting.LoadFx) result.Trades.AddRange(_xmlFxParser.ParseXml(xml));
                if (assetTypeToLoadSetting.LoadOptions) result.OptionTrades.AddRange(IBXmlOptionTradeParser.ParseXml(xml));
                if (assetTypeToLoadSetting.LoadOptions) result.CashSettlements.AddRange(IBXmlCashSettlementParser.ParseXml(xml));
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
