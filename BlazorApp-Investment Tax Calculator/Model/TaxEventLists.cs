using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Text.Json.Serialization;

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
    [JsonIgnore]
    public IEnumerable<TaxEvent> AllEvents => Trades.Cast<TaxEvent>()
        .Concat(CorporateActions.Cast<TaxEvent>())
        .Concat(Dividends.Cast<TaxEvent>())
        .Concat(OptionTrades.Cast<TaxEvent>())
        .Concat(FutureContractTrades.Cast<TaxEvent>())
        .Concat(CashSettlements.Cast<TaxEvent>())
        .Concat(InterestIncomes.Cast<TaxEvent>());

    public void AddData(TaxEventLists taxEventLists)
    {
        AddData(taxEventLists, false);
    }

    /// <summary>
    /// Every income a fund equalisation entry can be linked to.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<IEqualisableIncome> EqualisableIncomes => Dividends.Cast<IEqualisableIncome>()
        .Concat(InterestIncomes)
        .Concat(CorporateActions.OfType<ExcessReportableIncome>());

    /// <summary>
    /// Links <paramref name="equalisation"/> to <paramref name="income"/> and stores it. Throws
    /// <see cref="InvalidOperationException"/>, storing nothing, if the income is not in these lists, cannot be
    /// equalised, or would be exceeded by its equalisation.
    /// </summary>
    public void AddEqualisation(FundEqualisation equalisation, IEqualisableIncome income)
    {
        if (!EqualisableIncomes.Any(i => ReferenceEquals(i, income)))
        {
            throw new InvalidOperationException("The selected income no longer exists.");
        }
        equalisation.LinkTo(income);
        CorporateActions.Add(equalisation);
    }

    /// <summary>
    /// Removes the equalisation entry and detaches it from its income.
    /// </summary>
    public void RemoveEqualisation(FundEqualisation equalisation)
    {
        CorporateActions.RemoveAll(c => ReferenceEquals(c, equalisation));
        equalisation.Unlink();
    }

    /// <summary>
    /// Removes a dividend, interest income or ERI entry together with the fund equalisation entries linked to it,
    /// which cannot exist without their income. Returns those equalisation entries, for the caller to warn about.
    /// </summary>
    public List<FundEqualisation> RemoveIncome(IEqualisableIncome income)
    {
        int removed = income switch
        {
            Dividend dividend => Dividends.RemoveAll(d => ReferenceEquals(d, dividend)),
            InterestIncome interest => InterestIncomes.RemoveAll(i => ReferenceEquals(i, interest)),
            ExcessReportableIncome eri => CorporateActions.RemoveAll(c => ReferenceEquals(c, eri)),
            _ => 0
        };
        if (removed == 0) return [];
        List<FundEqualisation> equalisations = [.. income.EqualisationLinks.Equalisations];
        equalisations.ForEach(RemoveEqualisation);
        return equalisations;
    }

    /// <summary>
    /// Removes a corporate action. An ERI entry takes its equalisation entries with it (see <see cref="RemoveIncome"/>),
    /// and an equalisation entry is detached from its income. Returns the equalisation entries removed with an ERI.
    /// </summary>
    public List<FundEqualisation> RemoveCorporateAction(CorporateAction action)
    {
        switch (action)
        {
            case ExcessReportableIncome eri:
                return RemoveIncome(eri);
            case FundEqualisation equalisation:
                RemoveEqualisation(equalisation);
                return [];
            default:
                CorporateActions.RemoveAll(c => ReferenceEquals(c, action));
                return [];
        }
    }

    /// <summary>
    /// Links every equalisation entry not yet linked to an income in these lists to the income with its saved
    /// <see cref="FundEqualisation.IncomeId"/>, where there is one that can take it.
    /// </summary>
    public void LinkEqualisations()
    {
        Dictionary<string, IEqualisableIncome> incomeById = [];
        foreach (IEqualisableIncome income in EqualisableIncomes) incomeById.TryAdd(income.IncomeId, income);

        foreach (FundEqualisation equalisation in UnlinkedEqualisations())
        {
            if (equalisation.IncomeId is null || !incomeById.TryGetValue(equalisation.IncomeId, out IEqualisableIncome? income)) continue;
            try
            {
                equalisation.LinkTo(income);
            }
            catch (InvalidOperationException)
            {
                // The income refused it (it would be exceeded): left unlinked.
            }
        }
    }

    /// <summary>
    /// Links what can be linked (see <see cref="LinkEqualisations"/>), then removes the equalisation entries still
    /// without an income in these lists, so none is left stranded. Returns the entries removed, to warn about.
    /// Call after importing: an imported equalisation's income may be missing from the file, not loaded, or skipped
    /// as a duplicate of income already here (which it is then linked to).
    /// </summary>
    public List<FundEqualisation> RemoveUnlinkedEqualisations()
    {
        LinkEqualisations();
        List<FundEqualisation> unlinked = UnlinkedEqualisations();
        unlinked.ForEach(RemoveEqualisation);
        return unlinked;
    }

    private List<FundEqualisation> UnlinkedEqualisations()
    {
        HashSet<IEqualisableIncome> present = new(EqualisableIncomes, ReferenceEqualityComparer.Instance);
        return [.. CorporateActions.OfType<FundEqualisation>().Where(e => e.LinkedIncome is null || !present.Contains(e.LinkedIncome))];
    }

    /// <summary>
    /// Gives a new <see cref="IEqualisableIncome.IncomeId"/> to any income sharing one with an income before it
    /// (e.g. the same file imported twice without skipping duplicates), so a saved link identifies one income.
    /// Links are object references, so linked equalisations are unaffected.
    /// </summary>
    private void EnsureUniqueIncomeIds()
    {
        HashSet<string> seen = [];
        foreach (IEqualisableIncome income in EqualisableIncomes)
        {
            if (!seen.Add(income.IncomeId))
            {
                income.IncomeId = Guid.NewGuid().ToString("N");
                seen.Add(income.IncomeId);
            }
        }
    }

    public void AddData(TaxEventLists taxEventLists, bool skipDuplicates)
    {
        AddEvents(taxEventLists, skipDuplicates);
        EnsureUniqueIncomeIds();
    }

    private void AddEvents(TaxEventLists taxEventLists, bool skipDuplicates)
    {
        if (skipDuplicates)
        {
            var existingTradeSigs = Trades.Select(t => t.GetDuplicateSignature()).ToHashSet();
            var existingDivSigs = Dividends.Select(d => d.GetDuplicateSignature()).ToHashSet();
            var existingIntSigs = InterestIncomes.Select(i => i.GetDuplicateSignature()).ToHashSet();
            var existingCorpActionSigs = CorporateActions.Select(c => c.GetDuplicateSignature()).ToHashSet();
            var existingOptionSigs = OptionTrades.Select(o => o.GetDuplicateSignature()).ToHashSet();
            var existingFutureSigs = FutureContractTrades.Select(f => f.GetDuplicateSignature()).ToHashSet();
            var existingCashSigs = CashSettlements.Select(c => c.GetDuplicateSignature()).ToHashSet();

            Trades.AddRange(taxEventLists.Trades.Where(t => !existingTradeSigs.Contains(t.GetDuplicateSignature())));
            Dividends.AddRange(taxEventLists.Dividends.Where(d => !existingDivSigs.Contains(d.GetDuplicateSignature())));
            InterestIncomes.AddRange(taxEventLists.InterestIncomes.Where(i => !existingIntSigs.Contains(i.GetDuplicateSignature())));
            CorporateActions.AddRange(taxEventLists.CorporateActions.Where(c => !existingCorpActionSigs.Contains(c.GetDuplicateSignature())));
            OptionTrades.AddRange(taxEventLists.OptionTrades.Where(o => !existingOptionSigs.Contains(o.GetDuplicateSignature())));
            FutureContractTrades.AddRange(taxEventLists.FutureContractTrades.Where(f => !existingFutureSigs.Contains(f.GetDuplicateSignature())));
            CashSettlements.AddRange(taxEventLists.CashSettlements.Where(c => !existingCashSigs.Contains(c.GetDuplicateSignature())));
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
        var existingCorpActionSigs = CorporateActions.Select(c => c.GetDuplicateSignature()).ToHashSet();
        var existingOptionSigs = OptionTrades.Select(o => o.GetDuplicateSignature()).ToHashSet();
        var existingFutureSigs = FutureContractTrades.Select(f => f.GetDuplicateSignature()).ToHashSet();
        var existingCashSigs = CashSettlements.Select(c => c.GetDuplicateSignature()).ToHashSet();

        return new TaxEventLists
        {
            Trades = other.Trades.Where(t => existingTradeSigs.Contains(t.GetDuplicateSignature())).ToList(),
            Dividends = other.Dividends.Where(d => existingDivSigs.Contains(d.GetDuplicateSignature())).ToList(),
            InterestIncomes = other.InterestIncomes.Where(i => existingIntSigs.Contains(i.GetDuplicateSignature())).ToList(),
            CorporateActions = other.CorporateActions.Where(c => existingCorpActionSigs.Contains(c.GetDuplicateSignature())).ToList(),
            OptionTrades = other.OptionTrades.Where(o => existingOptionSigs.Contains(o.GetDuplicateSignature())).ToList(),
            FutureContractTrades = other.FutureContractTrades.Where(f => existingFutureSigs.Contains(f.GetDuplicateSignature())).ToList(),
            CashSettlements = other.CashSettlements.Where(c => existingCashSigs.Contains(c.GetDuplicateSignature())).ToList()
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
