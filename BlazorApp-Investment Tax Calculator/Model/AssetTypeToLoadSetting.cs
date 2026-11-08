using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Model;

public class AssetTypeToLoadSetting
{
    public bool LoadStocks { get; set; } = true;
    public bool LoadFunds { get; set; } = true;
    public bool LoadOptions { get; set; } = true;
    public bool LoadFutures { get; set; } = true;
    public bool LoadFx { get; set; } = true;
    public bool LoadDividends { get; set; } = true;
    public bool LoadInterestIncome { get; set; } = true;

    /// <summary>
    /// The events of an imported session file restricted to the asset and income types chosen for loading.
    /// </summary>
    public TaxEventLists FilterTaxEvent(TaxEventLists taxEventLists)
    {
        TaxEventLists resultFiltered = new();
        if (LoadDividends) resultFiltered.Dividends.AddRange(taxEventLists.Dividends);
        // An ERI entry is kept for its Section 104 cost uplift, but its income is excluded with the income type it is
        // taxed as, as the stored dividend/interest it used to generate was.
        resultFiltered.CorporateActions.AddRange(taxEventLists.CorporateActions.Select(action =>
            action is ExcessReportableIncome eri && !IsIncomeLoaded(eri) ? eri with { IsIncomeExcluded = true } : action));
        if (LoadStocks) resultFiltered.Trades.AddRange(taxEventLists.Trades.Where(trade => trade.AssetType == AssetCategoryType.STOCK));
        if (LoadFunds) resultFiltered.Trades.AddRange(taxEventLists.Trades.Where(trade => trade.AssetType == AssetCategoryType.FUND));
        if (LoadFutures) resultFiltered.FutureContractTrades.AddRange(taxEventLists.FutureContractTrades);
        if (LoadFx) resultFiltered.Trades.AddRange(taxEventLists.Trades.Where(trade => trade.AssetType == AssetCategoryType.FX));
        if (LoadOptions) resultFiltered.OptionTrades.AddRange(taxEventLists.OptionTrades);
        if (LoadOptions) resultFiltered.CashSettlements.AddRange(taxEventLists.CashSettlements);
        if (LoadInterestIncome) resultFiltered.InterestIncomes.AddRange(taxEventLists.InterestIncomes);
        return resultFiltered;
    }

    /// <summary>
    /// Whether the income type the ERI entry is taxed as was chosen for loading.
    /// </summary>
    private bool IsIncomeLoaded(ExcessReportableIncome eri) =>
        eri.IncomeType == ExcessReportableIncomeType.DIVIDEND ? LoadDividends : LoadInterestIncome;
}
