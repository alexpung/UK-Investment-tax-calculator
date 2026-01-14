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
    public List<CashSettlement> CashSettlements { get; set; } = [];
    public List<InterestIncome> InterestIncomes { get; set; } = [];

    public void AddData(TaxEventLists taxEventLists)
    {
        AddData(taxEventLists, false);
    }

    public void AddData(TaxEventLists taxEventLists, bool skipDuplicates)
    {
        if (skipDuplicates)
        {
            var existingTradeSigs = Trades.Select(t => t.GetDuplicateSignature()).ToHashSet();
            var existingDivSigs = Dividends.Select(d => d.GetDuplicateSignature()).ToHashSet();
            var existingIntSigs = InterestIncomes.Select(i => i.GetDuplicateSignature()).ToHashSet();

            Trades.AddRange(taxEventLists.Trades.Where(t => !existingTradeSigs.Contains(t.GetDuplicateSignature())));
            Dividends.AddRange(taxEventLists.Dividends.Where(d => !existingDivSigs.Contains(d.GetDuplicateSignature())));
            InterestIncomes.AddRange(taxEventLists.InterestIncomes.Where(i => !existingIntSigs.Contains(i.GetDuplicateSignature())));
            
            // For other types, see initial implementation
            CorporateActions.AddRange(taxEventLists.CorporateActions);
            OptionTrades.AddRange(taxEventLists.OptionTrades);
            FutureContractTrades.AddRange(taxEventLists.FutureContractTrades);
            CashSettlements.AddRange(taxEventLists.CashSettlements);
        }
        else
        {
            Trades.AddRange(taxEventLists.Trades);
            CorporateActions.AddRange(taxEventLists.CorporateActions);
            Dividends.AddRange(taxEventLists.Dividends);
            OptionTrades.AddRange(taxEventLists.OptionTrades);
            FutureContractTrades.AddRange(taxEventLists.FutureContractTrades);
            CashSettlements.AddRange(taxEventLists.CashSettlements);
            InterestIncomes.AddRange(taxEventLists.InterestIncomes);
        }
    }

    public TaxEventLists GetDuplicates(TaxEventLists other)
    {
        var existingTradeSigs = Trades.Select(t => t.GetDuplicateSignature()).ToHashSet();
        var existingDivSigs = Dividends.Select(d => d.GetDuplicateSignature()).ToHashSet();
        var existingIntSigs = InterestIncomes.Select(i => i.GetDuplicateSignature()).ToHashSet();

        return new TaxEventLists
        {
            Trades = other.Trades.Where(t => existingTradeSigs.Contains(t.GetDuplicateSignature())).ToList(),
            Dividends = other.Dividends.Where(d => existingDivSigs.Contains(d.GetDuplicateSignature())).ToList(),
            InterestIncomes = other.InterestIncomes.Where(i => existingIntSigs.Contains(i.GetDuplicateSignature())).ToList()
        };
    }

    public void AddData(IEnumerable<TaxEvent> taxEvents)
    {
        foreach (TaxEvent taxEvent in taxEvents)
        {
            if (taxEvent is Trade and not OptionTrade and not FutureContractTrade) Trades.Add((Trade)taxEvent);
            if (taxEvent is CorporateAction corporateAction) CorporateActions.Add(corporateAction);
            if (taxEvent is Dividend dividend) Dividends.Add(dividend);
            if (taxEvent is OptionTrade optionTrade) OptionTrades.Add(optionTrade);
            if (taxEvent is FutureContractTrade futureContractTrade) FutureContractTrades.Add(futureContractTrade);
            if (taxEvent is CashSettlement cashSettlements) CashSettlements.Add(cashSettlements);
            if (taxEvent is InterestIncome interestIncome) InterestIncomes.Add(interestIncome);
        }
    }

    public int GetTotalNumberOfEvents()
    {
        return Trades.Count + CorporateActions.Count + Dividends.Count + OptionTrades.Count + FutureContractTrades.Count + CashSettlements.Count + InterestIncomes.Count;
    }
}
