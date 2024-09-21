using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Model;

public record TaxEventLists : IDividendLists, ITradeAndCorporateActionList
{
    public List<Trade> Trades { get; set; } = [];
    public List<CorporateAction> CorporateActions { get; set; } = [];
    public List<Dividend> Dividends { get; set; } = [];
    public List<OptionTrade> OptionTrades { get; set; } = [];
    public List<FutureContractTrade> FutureContractTrades { get; set; } = [];

    public void AddData(TaxEventLists taxEventLists)
    {
        Trades.AddRange(taxEventLists.Trades);
        CorporateActions.AddRange(taxEventLists.CorporateActions);
        Dividends.AddRange(taxEventLists.Dividends);
        OptionTrades.AddRange(taxEventLists.OptionTrades);
        FutureContractTrades.AddRange(taxEventLists.FutureContractTrades);
    }

    public void AddData(IEnumerable<TaxEvent> taxEvents)
    {
        foreach (TaxEvent taxEvent in taxEvents)
        {
            if (taxEvent is Trade trade) Trades.Add(trade);
            if (taxEvent is CorporateAction corporateAction) CorporateActions.Add(corporateAction);
            if (taxEvent is Dividend dividend) Dividends.Add(dividend);
            if (taxEvent is OptionTrade optionTrade) OptionTrades.Add(optionTrade);
            if (taxEvent is FutureContractTrade futureContractTrade) FutureContractTrades.Add(futureContractTrade);
        }
    }

    public int GetTotalNumberOfEvents()
    {
        return Trades.Count + CorporateActions.Count + Dividends.Count;
    }
}
