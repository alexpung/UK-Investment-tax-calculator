using CapitalGainCalculator.Model.Interfaces;
using System.Collections.Generic;

namespace CapitalGainCalculator.Model;
public record TaxEventLists : IDividendLists, ITradeAndCorporateActionList
{
    public List<Trade> Trades { get; set; } = new List<Trade>();
    public List<CorporateAction> CorporateActions { get; set; } = new List<CorporateAction>();
    public List<Dividend> Dividends { get; set; } = new List<Dividend>();

    public void AddData(TaxEventLists taxEventLists)
    {
        Trades.AddRange(taxEventLists.Trades);
        CorporateActions.AddRange(taxEventLists.CorporateActions);
        Dividends.AddRange(taxEventLists.Dividends);
    }

    public void AddData(IEnumerable<TaxEvent> taxEvents)
    {
        foreach (TaxEvent taxEvent in taxEvents)
        {
            if (taxEvent is Trade trade) Trades.Add(trade);
            if (taxEvent is CorporateAction corporateAction) CorporateActions.Add(corporateAction);
            if (taxEvent is Dividend dividend) Dividends.Add(dividend);
        }
    }

    public int GetTotalNumberOfEvents()
    {
        return Trades.Count + CorporateActions.Count + Dividends.Count;
    }
}
