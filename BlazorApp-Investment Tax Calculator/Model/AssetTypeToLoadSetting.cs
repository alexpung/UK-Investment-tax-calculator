using InvestmentTaxCalculator.Enumerations;

namespace InvestmentTaxCalculator.Model;
public class AssetTypeToLoadSetting
{
    public bool LoadStocks { get; set; } = true;
    public bool LoadOptions { get; set; } = true;
    public bool LoadFutures { get; set; } = true;
    public bool LoadFx { get; set; } = true;
    public bool LoadDividends { get; set; } = true;

    public TaxEventLists FilterTaxEvent(TaxEventLists taxEventLists)
    {
        TaxEventLists resultFiltered = new();
        if (LoadDividends) resultFiltered.Dividends.AddRange(taxEventLists.Dividends);
        if (LoadStocks) resultFiltered.CorporateActions.AddRange(taxEventLists.CorporateActions);
        if (LoadStocks) resultFiltered.Trades.AddRange(taxEventLists.Trades.Where(trade => trade.AssetType == AssetCatagoryType.STOCK));
        if (LoadFutures) resultFiltered.FutureContractTrades.AddRange(taxEventLists.FutureContractTrades);
        if (LoadFx) resultFiltered.Trades.AddRange(taxEventLists.Trades.Where(trade => trade.AssetType == AssetCatagoryType.FX));
        if (LoadOptions) resultFiltered.OptionTrades.AddRange(taxEventLists.OptionTrades);
        if (LoadOptions) resultFiltered.CashSettlements.AddRange(taxEventLists.CashSettlements);
        return resultFiltered;
    }
}
